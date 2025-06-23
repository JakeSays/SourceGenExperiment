using System;

namespace WayCoolStuff;

internal class TypeInfo
{
    public string Namespace { get; }
    public string Name { get; }

    public TypeInfo(string ns, string name)
    {
        Namespace = ns;
        Name = name;
    }
}
