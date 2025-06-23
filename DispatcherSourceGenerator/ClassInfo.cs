using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace WayCoolStuff;

internal enum InfoType
{
    None,
    Dispatcher,
    Handler
}

internal readonly struct ClassInfo
{
    public readonly InfoType Type;
    public readonly ClassDeclarationSyntax? Class;
    public readonly INamedTypeSymbol? Model;

    public ClassInfo(
        InfoType type,
        ClassDeclarationSyntax? @class = null,
        INamedTypeSymbol? model = null)
    {
        Type = type;
        Class = @class;
        Model = model;
    }
}
