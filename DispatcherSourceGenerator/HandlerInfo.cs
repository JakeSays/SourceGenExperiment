using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace WayCoolStuff;

internal class HandlerInfo
{
    public ClassDeclarationSyntax Syntax { get; }
    public INamedTypeSymbol Symbol { get; }

    public TypeInfo Implementation { get; }
    public TypeInfo Interface { get; }
    public TypeInfo Parameter { get; }

    public IEnumerable<string> Namespaces
    {
        get
        {
            yield return Implementation.Namespace;
            yield return Interface.Namespace;
            yield return Parameter.Namespace;
        }
    }

    public HandlerInfo(
        ClassDeclarationSyntax syntax,
        INamedTypeSymbol symbol,
        TypeInfo implementation,
        TypeInfo @interface,
        TypeInfo parameter)
    {
        Syntax = syntax;
        Symbol = symbol;
        Implementation = implementation;
        Interface = @interface;
        Parameter = parameter;
    }
}