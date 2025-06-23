namespace WayCoolStuff.CodeGen;

internal static class VariousExtensions
{
    public static bool IsNullOrEmpty(this string? value) => string.IsNullOrEmpty(value);

    public static bool NotNullOrEmpty(this string? value) => !string.IsNullOrEmpty(value);
}
