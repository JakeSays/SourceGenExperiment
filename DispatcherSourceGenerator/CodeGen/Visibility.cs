using System;


namespace WayCoolStuff.CodeGen;

public enum Visibility
{
    None,
    Private,
    Protected,
    ProtectedInternal,
    Internal,
    Public
}

public static class VisibilityHelpers
{
    public static string Format(this Visibility visibility)
    {
        switch (visibility)
        {
            case Visibility.None:
                return "";
            case Visibility.Public:
                return "public";
            case Visibility.Private:
                return "private";
            case Visibility.Internal:
                return "internal";
            case Visibility.Protected:
                return "protected";
            case Visibility.ProtectedInternal:
                return "protected internal";
            default:
                throw new ArgumentOutOfRangeException(nameof(visibility), visibility, null);
        }
    }

    public static Visibility MostVisible(Visibility lhs, Visibility rhs) => (Visibility)Math.Max((int)lhs, (int)rhs);

    public static Visibility LeastVisible(Visibility lhs, Visibility rhs) => (Visibility)Math.Min((int)lhs, (int)rhs);
}