using System;


namespace WayCoolStuff;

public sealed class DispatcherGeneratorOptions : IEquatable<DispatcherGeneratorOptions>
{
    public string ErrorClassName { get; }

    public DispatcherGeneratorOptions(string errorClassName)
    {
        ErrorClassName = errorClassName;
    }

    public bool Equals(DispatcherGeneratorOptions? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return ErrorClassName == other.ErrorClassName;
    }

    public override bool Equals(
        object? obj) =>
        ReferenceEquals(this, obj) ||
        obj is DispatcherGeneratorOptions other && Equals(other);

    public override int GetHashCode() => ErrorClassName.GetHashCode();
}
