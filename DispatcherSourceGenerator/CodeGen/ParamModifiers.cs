using System.Linq;


namespace WayCoolStuff.CodeGen;

public enum ParamModifiers
{
    None,
    In,
    Out,
    Ref
}

public static class ParameterModifiersExtensions
{
    public static string Format(this ParamModifiers[] values, bool prependSpace = false, bool appendSpace = false)
    {
        if (values.Length == 0)
        {
            return "";
        }

        var prefix = prependSpace
            ? " "
            : "";
        var suffix = appendSpace
            ? " "
            : "";

        var hasIn = values.Contains(ParamModifiers.In);
        var hasOut = values.Contains(ParamModifiers.Out);
        var hasRef = values.Contains(ParamModifiers.Ref);

        if (hasOut)
        {
            return $"{prefix}out{suffix}";
        }

        if (hasIn)
        {
            return hasRef
                ? $"{prefix}ref in{suffix}"
                : $"{prefix}in{suffix}";
        }

        if (hasRef)
        {
            return $"{prefix}ref{suffix}";
        }

        return "";
    }
}
