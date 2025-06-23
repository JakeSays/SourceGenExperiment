using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using WayCoolStuff.CodeGen;


namespace WayCoolStuff;

[Generator]
public class DispatcherSourceGenerator : IIncrementalGenerator
{
    private const string HandlerInterfaceName = "IMessageHandler";
    private const string DispatcherInterfaceName = "IMessageDispatcher";
    private const string DefaultDispatcherErrorClassName = "DispatcherError";
    private const string DispatcherErrorClassOption = "build_property.DispatcherSourceGenerator_DispatcherErrorClassName";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (s, _) => s is ClassDeclarationSyntax,
                transform: (ctx, _) => AnalyzeClass(ctx))
            .Where(t => t.Type != InfoType.None);

        var options = GetGeneratorOptions(context);

        context.RegisterSourceOutput(
            context.CompilationProvider
                .Combine(provider.Collect())
                .Combine(options),
            (ctx, t) => GenerateCode(ctx, t.Left.Right, t.Right));
    }

    private static IncrementalValueProvider<DispatcherGeneratorOptions> GetGeneratorOptions(
        IncrementalGeneratorInitializationContext context)
    {
        return context.AnalyzerConfigOptionsProvider
            .Select((optionsProvider, _) => new DispatcherGeneratorOptions(GetOption(
                    optionsProvider, DispatcherErrorClassOption, DefaultDispatcherErrorClassName)));

        static string GetOption(AnalyzerConfigOptionsProvider provider, string name, string defaultValue)
        {
            return provider.GlobalOptions.TryGetValue(name, out var optionText)
                ? optionText
                : defaultValue;
        }
    }

    private static ClassInfo AnalyzeClass(GeneratorSyntaxContext context)
    {
        var syntax = (ClassDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(syntax)
            is not INamedTypeSymbol symbol)
        {
            return new ClassInfo(InfoType.None);
        }

        var interfaces = symbol.Interfaces;

        if (interfaces.Any(i => i.Name.StartsWith(HandlerInterfaceName) && i.Arity == 1))
        {
            return new ClassInfo(InfoType.Handler, syntax, symbol);
        }

        return interfaces.Any(i => i.Name == DispatcherInterfaceName)
            ? new ClassInfo(InfoType.Dispatcher, syntax, symbol)
            : new ClassInfo(InfoType.None);
    }

    private static string MakeNamespace(INamedTypeSymbol symbol)
    {
        //there's probably a better way..

        var nss = symbol.ContainingNamespace;

        var ns = "";
        do
        {
            if (nss.Name.Length > 0)
            {
                ns = $"{nss.Name}{(ns.Length > 0 ? "." : "")}{ns}";
            }

            nss = nss.ContainingNamespace;
        } while (nss != null);

        return ns;
    }

    private static List<HandlerInfo> PrepareHandlers(
        CancellationToken canceller,
        IEnumerable<ClassInfo> handlerClasses,
        DispatcherGeneratorOptions options)
    {
        var handlers = new List<HandlerInfo>();

        foreach (var handlerClass in handlerClasses)
        {
            canceller.ThrowIfCancellationRequested();

            var intfModel = handlerClass.Model!.Interfaces.First(i => i.Name.StartsWith(HandlerInterfaceName) && i.Arity == 1);
            var intfArg = intfModel.TypeArguments[0] as INamedTypeSymbol;

            var impl = new TypeInfo(MakeNamespace(handlerClass.Model!), handlerClass.Model!.Name);
            var intf = new TypeInfo(MakeNamespace(intfModel), intfModel.Name);
            var parm = new TypeInfo(MakeNamespace(intfArg!), intfArg!.Name);

            handlers.Add(new HandlerInfo(handlerClass.Class!, handlerClass.Model!, impl, intf, parm));
        }

        return handlers;
    }

    private static void PrepareDispatcher(ImmutableArray<ClassInfo> handlerClasses, out DispatcherInfo info)
    {
        var dispatcherInfo = handlerClasses.FirstOrDefault(c => c.Type == InfoType.Dispatcher);

        if (dispatcherInfo.Type == InfoType.Dispatcher)
        {
            var intf = dispatcherInfo.Model!.Interfaces.First(i => i.Name.StartsWith(DispatcherInterfaceName) && i.Arity == 0);

            info = new DispatcherInfo(
                new TypeInfo(MakeNamespace(intf), intf.Name),
                new TypeInfo(MakeNamespace(dispatcherInfo.Model!), dispatcherInfo.Model!.Name),
                dispatcherInfo.Class!.Modifiers.Any(m => m.ValueText == "public")
                    ? Visibility.Public
                    : Visibility.Internal,
                Modifiers.Partial);
        }
        else
        {
            info = new DispatcherInfo(
                new TypeInfo("WayCoolStuff", DispatcherInterfaceName),
                new TypeInfo("WayCoolStuff", "ThingWithBigSwitch"),
                Visibility.Public,
                Modifiers.Sealed,
                false);
        }
    }

    private static void GenerateCode(
        SourceProductionContext context,
        ImmutableArray<ClassInfo> handlerClasses,
        DispatcherGeneratorOptions options)
    {
        var handlers = PrepareHandlers(
            context.CancellationToken,
            handlerClasses.Where(c => c.Type == InfoType.Handler),
            options);

        if (handlers.Count == 0)
        {
            return;
        }

        PrepareDispatcher(handlerClasses, out var dispatcher);

        var output = new CodeWriter();
        output.Comment($" <auto-generated when=\"{DateTime.Now:R}\"/>");
        output.BlankLine();

        output.Using("System");
        output.Using("System.Threading.Tasks");
        output.Using("Microsoft.Extensions.DependencyInjection");

        output.Block("\n#nullable enable\n\n");

        foreach (var ns in handlers
            .SelectMany(h => h.Namespaces)
            .Concat(dispatcher.Namespaces)
            .Where(h => h.NotNullOrEmpty())
            .Distinct())
        {
            output.Using(ns);
        }

        output.BlankLine();
        output.WriteLine($"namespace {dispatcher.Implementation.Namespace};");
        output.BlankLine();

        context.CancellationToken.ThrowIfCancellationRequested();

        using (output.Class(
            dispatcher.Implementation.Name,
            visibility: dispatcher.Visibility,
            modifiers: dispatcher.Modifiers))
        {
            if (!dispatcher.Present)
            {
                output.WriteLine("private readonly IServiceProvider _services;");
                output.BlankLine();
                using (output.Constructor(dispatcher.Implementation.Name, Visibility.Public, ("IServiceProvider", "services")))
                {
                    output.WriteLine("_services = services;");
                }

                output.BlankLine();
            }

            using (output.AsyncMethod("DispatchAsync", args: new Parameter("data", "object")))
            {
                output.WriteLine("ArgumentNullException.ThrowIfNull(data);");
                output.BlankLine();

                using (output.Switch("data"))
                {
                    var caseCounter = 0;
                    foreach (var handler in handlers)
                    {
                        context.CancellationToken.ThrowIfCancellationRequested();

                        var typeName = $"type{caseCounter++}";
                        using (output.Case($"{handler.Parameter.Name} {typeName}"))
                        {
                            output.WriteLine(
                                $"var handler = _services.GetRequiredService<{HandlerInterfaceName}<{handler.Parameter.Name}>>();");
                            output.WriteLine($"await handler.HandleMessage({typeName});");
                        }
                    }

                    output.Block($"default:\a\nthrow new {options.ErrorClassName}",
                        "($\"Cannot handle messages of type '{{data.GetType().FullName}}'\");\b\n");
                }
            }
        }

        context.AddSource($"{dispatcher.Implementation.Namespace}{dispatcher.Implementation.Name}.g.cs", SourceText.From(output.CodeOutput, Encoding.UTF8));
    }
}
