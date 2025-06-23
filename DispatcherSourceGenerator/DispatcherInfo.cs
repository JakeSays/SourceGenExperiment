using System.Collections.Generic;
using WayCoolStuff.CodeGen;


namespace WayCoolStuff;

internal readonly struct DispatcherInfo
{
    public readonly bool Present;
    public readonly Visibility Visibility;
    public readonly Modifiers Modifiers;
    public readonly TypeInfo Interface;
    public readonly TypeInfo Implementation;

    public IEnumerable<string> Namespaces
    {
        get
        {
            yield return Interface.Namespace;
            yield return Implementation.Namespace;
        }
    }

    public DispatcherInfo(
        TypeInfo @interface,
        TypeInfo implementation,
        Visibility visibility,
        Modifiers modifiers,
        bool present = true)
    {
        Interface = @interface;
        Present = present;
        Implementation = implementation;
        Visibility = visibility;
        Modifiers = modifiers;
    }
}
