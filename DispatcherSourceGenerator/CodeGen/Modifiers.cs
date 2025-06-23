using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace WayCoolStuff.CodeGen;

[Flags]
public enum Modifiers
{
    None = 0x00,
    Virtual = 0x01,
    Abstract = 0x02,
    Override = 0x04,
    Sealed = 0x08,
    Partial = 0x10,
    Static = 0x20,
    SequentialLayout = 0x40,
    Unsafe = 0x80,
    Pack8 = 0x100,
    Size = 0x200,
    Offset = 0x400,
    Ansi = 0x800,
    ReadOnly = 0x1000,
    Async = 0x2000
}

public static class ModifiersExtensions
{
    public static bool IsSet(this Modifiers value, Modifiers mask) => (value & mask) == mask;

    public static IEnumerable<Modifiers> Expand(this IEnumerable<Modifiers> values)
    {
        if (values == null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        return values.SelectMany(v => v.Expand());
    }

    public static IEnumerable<Modifiers> Expand(this Modifiers value)
    {
        var used = Modifiers.None;

        bool Contains(Modifiers v, Modifiers target)
        {
            if (!v.IsSet(target) ||
                used.IsSet(target))
            {
                return false;
            }

            used |= target;
            return true;
        }

        if (Contains(value, Modifiers.None))
        {
            yield return Modifiers.None;
        }

        if (Contains(value, Modifiers.Virtual))
        {
            yield return Modifiers.Virtual;
        }

        if (Contains(value, Modifiers.Abstract))
        {
            yield return Modifiers.Abstract;
        }

        if (Contains(value, Modifiers.Override))
        {
            yield return Modifiers.Override;
        }

        if (Contains(value, Modifiers.Sealed))
        {
            yield return Modifiers.Sealed;
        }

        if (Contains(value, Modifiers.Partial))
        {
            yield return Modifiers.Partial;
        }

        if (Contains(value, Modifiers.Static))
        {
            yield return Modifiers.Static;
        }

        if (Contains(value, Modifiers.SequentialLayout))
        {
            yield return Modifiers.SequentialLayout;
        }

        if (Contains(value, Modifiers.Unsafe))
        {
            yield return Modifiers.Unsafe;
        }

        if (Contains(value, Modifiers.Pack8))
        {
            yield return Modifiers.Pack8;
        }

        if (Contains(value, Modifiers.Size))
        {
            yield return Modifiers.Size;
        }

        if (Contains(value, Modifiers.Offset))
        {
            yield return Modifiers.Offset;
        }

        if (Contains(value, Modifiers.Ansi))
        {
            yield return Modifiers.Ansi;
        }

        if (Contains(value, Modifiers.ReadOnly))
        {
            yield return Modifiers.ReadOnly;
        }

        if (Contains(value, Modifiers.Async))
        {
            yield return Modifiers.Async;
        }

        if (used == Modifiers.None)
        {
            yield return Modifiers.None;
        }
    }

    public static string Format(this IEnumerable<Modifiers> values, bool prependSpace = false, bool appendSpace = false)
    {
        if (values == null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        var prefix = prependSpace
            ? " "
            : "";
        var suffix = appendSpace
            ? " "
            : "";

        var sb = new StringBuilder();

        void Append(string text)
        {
            if (sb.Length > 0)
            {
                sb.Append(' ');
            }

            sb.Append(text);
        }

        foreach (var value in values.Flatten()
            .Expand())
        {
            if (value == Modifiers.None)
            {
                continue;
            }

            if (value == Modifiers.Async)
            {
                Append("async");
            }

            if (value == Modifiers.Virtual)
            {
                Append("virtual");
                continue;
            }

            if (value == Modifiers.Abstract)
            {
                Append("abstract");
                continue;
            }

            if (value == Modifiers.Override)
            {
                Append("override");
                continue;
            }

            if (value == Modifiers.Sealed)
            {
                Append("sealed");
                continue;
            }

            if (value == Modifiers.Partial)
            {
                Append("partial");
                continue;
            }

            if (value == Modifiers.Static)
            {
                Append("static");
                continue;
            }

            if (value == Modifiers.SequentialLayout)
            {
                continue;
            }

            if (value == Modifiers.Unsafe)
            {
                Append("unsafe");
                continue;
            }

            if (value == Modifiers.Pack8)
            {
                Append("pack8");
                continue;
            }

            if (value == Modifiers.Size)
            {
                Append("size");
                continue;
            }

            if (value == Modifiers.Offset)
            {
                Append("offset");
                continue;
            }

            if (value == Modifiers.Ansi)
            {
                Append("ansi");
                continue;
            }

            if (value == Modifiers.ReadOnly)
            {
                Append("readonly");
            }
        }

        return $"{prefix}{sb}{suffix}";
    }

    public static string Format(this Modifiers value, bool prependSpace = false, bool appendSpace = false) =>
        value.Expand()
            .Format(prependSpace, appendSpace);

    public static Modifiers Flatten(this IEnumerable<Modifiers> values)
    {
        //using a local method eliminates the lambda overhead
        Modifiers Do(Modifiers agg, Modifiers next) => agg | next;

        var result = values.Aggregate(Modifiers.None, Do);
        return result;
    }

    public static string Format(this Modifiers value, params Modifiers[] masks)
    {
        var sb = new StringBuilder();

        void Append(string text)
        {
            if (sb.Length > 0)
            {
                sb.Append(' ');
            }

            sb.Append(text);
        }

        foreach (var mask in masks.Flatten()
            .Expand())
        {
            if (mask == Modifiers.None)
            {
                continue;
            }

            if (mask.IsSet(Modifiers.Abstract))
            {
                Append("abstract");
                continue;
            }

            if (mask.IsSet(Modifiers.Override))
            {
                Append("override");
                continue;
            }

            if (mask.IsSet(Modifiers.Partial))
            {
                Append("partial");
                continue;
            }

            if (mask.IsSet(Modifiers.Sealed))
            {
                Append("sealed");
                continue;
            }

            if (mask.IsSet(Modifiers.Virtual))
            {
                Append("virtual");
                continue;
            }

            if (mask.IsSet(Modifiers.Static))
            {
                Append("static");
            }
        }

        return sb.ToString();
    }
}