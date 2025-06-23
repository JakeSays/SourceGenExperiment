using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;


namespace WayCoolStuff.CodeGen;

public class Parameter
{
    public Parameter(string name, string type, bool isOut = false)
    {
        Name = name;
        Type = type;
        IsOut = isOut;
    }

    public string Name { get; }
    public string Type { get; }
    public bool IsOut { get; }
}

public class CodeWriter
{
    private readonly HashSet<string> _actionMethods = [];

    //public virtual void WriteBeginClass(string className, bool isstatic = false, Access access = Access.Public)
    //{
    //	WriteBeginClass(className, null, isstatic, access: access);
    //}

    private readonly StringBuilder _builder = new();

    private readonly StringBuilder _codeBuilder = new();
    private ScopeContext? _currentScope;

    private bool _endsWithNewline;

    private bool _getWritten;

    public bool GeneratePartialClasses { get; set; }

    public List<string> ActionMethods => _actionMethods.ToList();

    public string CodeOutput => _codeBuilder.ToString();

    private List<int> IndentLengths { get; } = [];

    public string CurrentIndent { get; private set; } = "";

    //const int MaxSummaryLineLength = 40;

    //public virtual void Summary(string s)
    //{
    //    Summary(s, MaxSummaryLineLength);
    //}

    //public virtual void Summary(string s, int lineLength)
    //{
    //    if (string.IsNullOrWhiteSpace(s))
    //    {
    //        return;
    //    }

    //    var lines = new List<string>();
    //    var sourceLines = s.Split('\n');
    //    foreach (var line in sourceLines)
    //    {
    //        if (line.Length <= lineLength)
    //        {
    //            lines.Add(line);
    //            continue;
    //        }
    //        var brokeLines = StringExtensions.BreakIntoLines(line, lineLength);
    //        lines.AddRange(brokeLines);
    //    }

    //    var text = "\n" + string.Join("\n", lines) + "\n";

    //    var xml = new XElement("summary", text);

    //    using (var reader = new StringReader(xml.ToString()))
    //    {
    //        string line;
    //        while ((line = reader.ReadLine()) != null)
    //        {
    //            WriteLine("/// " + line);
    //        }
    //    }
    //}

    public int UsingCount { get; private set; }

    public void AddAction(string methodName) => _actionMethods.Add(methodName);

    public void Reset()
    {
        _builder.Clear();
        _codeBuilder.Clear();
        _endsWithNewline = false;
        IndentLengths.Clear();
        CurrentIndent = "";
        _actionMethods.Clear();
    }

    public void Write(char ch) => Write(ch.ToString(CultureInfo.InvariantCulture));

    public void RawWrite(string text) => _codeBuilder.Append(text);

    public void RawWriteLine(string text = "") => _codeBuilder.AppendLine(text);

    public void BlankLine(int count = 1)
    {
        while (count-- > 0)
        {
            _codeBuilder.AppendLine();
        }
    }

    public void Write(string? textToAppend)
    {
        if (string.IsNullOrEmpty(textToAppend))
        {
            return;
        }

        // If we're starting off, or if the previous text ended with a newline,
        // we have to append the current indent first.
        if (CodeOutput.Length == 0 || _endsWithNewline)
        {
            _codeBuilder.Append(CurrentIndent);
            _endsWithNewline = false;
        }

        // Check if the current text ends with a newline
        if (textToAppend!.EndsWith("\n") ||
            textToAppend.EndsWith("\r\n"))
        {
            _endsWithNewline = true;
        }

        // This is an optimization. If the current indent is "", then we don't have to do any
        // of the more complex stuff further down.
        if (CurrentIndent.Length == 0)
        {
            _codeBuilder.Append(textToAppend);
            return;
        }

        // Everywhere there is a newline in the text, add an indent after it
        textToAppend = textToAppend
            .Replace("\r\n", $"\r\n{CurrentIndent}")
            .Replace("\n", $"\n{CurrentIndent}");
        // If the text ends with a newline, then we should strip off the indent added at the very end
        // because the appropriate indent will be added when the next time Write() is called
        if (_endsWithNewline)
        {
            _codeBuilder.Append(textToAppend, 0, textToAppend.Length - CurrentIndent.Length);
        }
        else
        {
            _codeBuilder.Append(textToAppend);
        }
    }

    public IDisposable Scope(
        bool openScope = false,
        bool addTrailingLine = false,
        bool isStatement = false,
        Action? cleanup = null)
    {
        if (openScope)
        {
            OpenScope();
        }

        return new ScopeContext(this, addTrailingLine, isStatement, cleanup);
    }

    public IDisposable PropertyScope(
        bool openScope = false,
        bool addTrailingLine = false,
        bool isStatement = false,
        Action? cleanup = null,
        Visibility propertyVisibility = Visibility.Public)
    {
        if (openScope)
        {
            OpenScope();
        }

        _currentScope = new ScopeContext(this, addTrailingLine, isStatement, cleanup);
        return _currentScope;
    }

    public void OpenScope(string scopeTag = "{")
    {
        WriteLine(scopeTag);
        PushIndent();
    }

    public void CloseScope(string scopeTag = "}", bool addTrailingLine = false)
    {
        PopIndent();
        WriteLine(scopeTag);
        if (addTrailingLine)
        {
            RawWriteLine();
        }
    }

    public void WriteLine(string? textToAppend = null)
    {
        if (!textToAppend.IsNullOrEmpty())
        {
            Write(textToAppend);
        }

        _codeBuilder.AppendLine();
        _endsWithNewline = true;
    }

    public void Write(string format, params object[] args) => Write(string.Format(format, args));

    public void WriteLine(string format, params object[] args) => WriteLine(string.Format(format, args));

    public void PushIndent(string indent = "    ")
    {
        if (indent == null)
        {
            throw new ArgumentNullException(nameof(indent));
        }

        CurrentIndent = CurrentIndent + indent;
        IndentLengths.Add(indent.Length);
    }

    public void Block(params string[] lines)
    {
        foreach (var line in lines)
        {
            if (line == "")
            {
                WriteLine();
                continue;
            }

            for (var index = 0; index < line.Length; index++)
            {
                var ch = line[index];
                if (ch == '\a')
                {
                    PushIndent();
                    continue;
                }

                if (ch == '\b')
                {
                    PopIndent();
                    continue;
                }

                if (ch == '\n')
                {
                    WriteLine();
                    continue;
                }

                if (ch == '{')
                {
                    if (index + 1 < line.Length &&
                        line[index + 1] == '{')
                    {
                        index++;
                        Write(ch);
                    }
                    else
                    {
                        if (!_endsWithNewline)
                        {
                            WriteLine();
                        }

                        OpenScope();
                    }
                    continue;
                }

                if (ch == '}')
                {
                    if (index + 1 < line.Length &&
                        line[index + 1] == '}')
                    {
                        index++;
                        Write(ch);
                    }
                    else
                    {
                        if (!_endsWithNewline)
                        {
                            WriteLine();
                        }

                        CloseScope();
                    }

                    continue;
                }

                Write(ch);
            }
        }
    }

    public string PopIndent()
    {
        var returnValue = "";
        if (IndentLengths.Count > 0)
        {
            var indentLength = IndentLengths[IndentLengths.Count - 1];
            IndentLengths.RemoveAt(IndentLengths.Count - 1);
            if (indentLength > 0)
            {
                returnValue = CurrentIndent.Substring(CurrentIndent.Length - indentLength);
                CurrentIndent = CurrentIndent.Remove(CurrentIndent.Length - indentLength);
            }
        }

        return returnValue;
    }

    public void ClearIndent()
    {
        IndentLengths.Clear();
        CurrentIndent = "";
    }


    public virtual void Comment(string s) => WriteLine($"//{s}");

    public IDisposable Try()
    {
        WriteLine("try");
        return Scope(true);
    }

    public void Catch()
    {
        CloseScope();
        WriteLine("catch");
        OpenScope();
    }

    public void Catch(string exception, string? variable = null)
    {
        CloseScope();
        WriteLine($"catch ({exception}{(variable != null ? $" {variable}" : "")})");
        OpenScope();
    }

    public void Finally()
    {
        CloseScope();
        WriteLine("finally");
        OpenScope();
    }

    public IDisposable Switch(string switchOn)
    {
        WriteLine($"switch ({switchOn})");
        return Scope(openScope: true, addTrailingLine: true);
    }

    private class CaseScope : ScopeContext
    {
        private readonly bool _breakOnexit;

        public CaseScope(
            CodeWriter writer,
            bool breakOnexit = true,
            bool addTrailingLine = false,
            bool isStatement = false,
            Action? cleanup = null,
            Visibility propertyVisibility = Visibility.Public)
            : base(writer, addTrailingLine, isStatement, cleanup, propertyVisibility)
        {
            _breakOnexit = breakOnexit;
        }

        public override void Dispose()
        {
            Writer.WriteLine(
                _breakOnexit
                    ? "break;"
                    : "return;");
            base.Dispose();
        }
    }

    public IDisposable Case(string expr, bool breakOnExit = true)
    {
        WriteLine($"case {expr}:");
        OpenScope();
        return new CaseScope(this, breakOnExit);
    }

    public virtual void Using(string s)
    {
        WriteLine($"using {s};");
        UsingCount += 1;
    }

    public virtual IDisposable Namespace(string ns)
    {
        BeginNamespace(ns);
        return new ScopeContext(this, false);
    }

    public virtual void BeginNamespace(string ns)
    {
        WriteLine($"namespace {ns}");
        WriteLine("{");
        PushIndent();
    }

    public virtual void EndNamespace(bool addTrailingLine = false)
    {
        PopIndent();
        WriteLine("}");

        if (addTrailingLine)
        {
            RawWriteLine();
        }
    }

    public virtual void Enum(
        string enumName,
        string baseType,
        Visibility visibility = Visibility.Public,
        params string[] tags)
    {
        BeginEnum(enumName, visibility, baseType);

        var lastTag = tags.Last();
        foreach (var tag in tags)
        {
            WriteLine(
                "{0}{1}",
                tag,
                tag == lastTag
                    ? ""
                    : ",");
        }

        CloseScope();
    }

    public virtual void Enum(
        string enumName,
        string baseType,
        Visibility visibility,
        IEnumerable<(string Name, string Value)> tags)
    {
        BeginEnum(enumName, visibility, baseType);

        var tagsa = tags.ToArray();
        var lastTag = tagsa.Last()
            .Name;
        foreach (var tag in tagsa)
        {
            WriteLine($"{tag.Name} = {tag.Value}{(tag.Name == lastTag ? "" : ",")}");
        }

        CloseScope();
    }

    public virtual void Enumerant(string name, string value, bool isLast)
    {
        Write(name);
        if (value.NotNullOrEmpty())
        {
            Write($" = {value}");
        }

        if (!isLast)
        {
            WriteLine(",");
        }
        else
        {
            WriteLine();
        }
    }

    public virtual void BeginEnum(string enumName, Visibility visibility, string baseType)
    {
        baseType = baseType.NotNullOrEmpty()
            ? $" : {baseType}"
            : "";

        WriteLine($"{visibility.Format()} enum {enumName}{baseType}");
        OpenScope();
    }

    public virtual void EndEnum(bool addTrailingLine = true) => CloseScope(addTrailingLine: addTrailingLine);

    public virtual IDisposable Struct(
        string className,
        Visibility visibility = Visibility.Public,
        string? size = null,
        params Modifiers[] modifiers)
    {
        BeginStruct(className, visibility, size, modifiers);
        return new ScopeContext(this, false);
    }

    public virtual void BeginStruct(
        string className,
        Visibility visibility = Visibility.Public,
        string? size = null,
        params Modifiers[] modifiers)
    {
        _builder.Clear();

        if (modifiers.Contains(Modifiers.SequentialLayout))
        {
            var attr = "";
            if (modifiers.Contains(Modifiers.Pack8))
            {
                attr += ", Pack = 8";
            }

            if (modifiers.Contains(Modifiers.Size))
            {
                attr += $", Size = {size}";
            }

            if (modifiers.Contains(Modifiers.Ansi))
            {
                attr += ", CharSet = CharSet.Ansi";
            }

            Attribute($"StructLayout(LayoutKind.Explicit{attr})");
        }

        var modtext = modifiers.Format();
        if (modtext.Length > 0)
        {
            modtext = " " + modtext;
        }

        _builder.Append(visibility.Format());
        _builder.Append($"{modtext} struct {className}");

        WriteLine(_builder.ToString());
        OpenScope();
    }

    public virtual void EndStruct(bool addTrailingLine = false)
    {
        PopIndent();
        WriteLine("}");

        if (addTrailingLine)
        {
            RawWriteLine();
        }
    }

    public virtual IDisposable Class(
        string className,
        string? baseClassName = null,
        Visibility visibility = Visibility.Public,
        params Modifiers[] modifiers)
    {
        BeginClass(className, baseClassName, visibility, modifiers);
        return new ScopeContext(this, false);
    }

    public virtual void BeginClass(
        string className,
        string? baseClassName = null,
        Visibility visibility = Visibility.Public,
        params Modifiers[] modifiers)
    {
        _builder.Clear();

        var modtext = modifiers.Format();
        if (modtext.Length > 0)
        {
            modtext = " " + modtext;
        }

        _builder.Append(visibility.Format());
        _builder.Append($"{modtext} class {className}");

        Write(_builder.ToString());
        if (!string.IsNullOrEmpty(baseClassName))
        {
            Write($" : {baseClassName}");
        }

        WriteLine();
        OpenScope();
    }

    public virtual void EndClass(bool addTrailingLine = false)
    {
        PopIndent();
        WriteLine("}");

        if (addTrailingLine)
        {
            RawWriteLine();
        }
    }

    public virtual IDisposable Method(string name, params IEnumerable<Parameter> args)
    {
        BeginMethod(name, "void", Visibility.Public, Modifiers.None, args);
        return Scope();
    }

    public virtual IDisposable Method(
        string name,
        string returnType,
        params IEnumerable<Parameter> args)
    {
        BeginMethod(name, returnType, Visibility.Public, Modifiers.None, args);
        return Scope();
    }

    public virtual IDisposable AsyncMethod(
        string name,
        string? returnType = null,
        Visibility visibility = Visibility.Public,
        Modifiers modifiers = Modifiers.None,
        params IEnumerable<Parameter> args)
    {
        modifiers |= Modifiers.Async;

        returnType = returnType == null
            ? "Task"
            : $"Task<{returnType}>";

        BeginMethod(name, returnType, visibility, modifiers, args);
        return Scope();
    }

    public virtual IDisposable Method(
        string name,
        string returnType,
        Visibility visibility,
        params IEnumerable<Parameter> args)
    {
        BeginMethod(name, returnType, visibility, Modifiers.None, args);
        return Scope();
    }

    public virtual IDisposable Method(
        string name,
        string returnType,
        Visibility visibility,
        Modifiers modifiers,
        params IEnumerable<Parameter> args)
    {
        BeginMethod(name, returnType, visibility, modifiers, args);
        return Scope();
    }

    public virtual void EmptyMethod(
        string name,
        string returnType = "void",
        Visibility visibility = Visibility.Public,
        Modifiers modifiers = Modifiers.None,
        params IEnumerable<Parameter> args)
    {
        BeginMethod(name, returnType, visibility, modifiers, args);
        EndMethod();
    }

    public virtual void BeginMethod(
        string name,
        string returnType = "void",
        Visibility visibility = Visibility.Public,
        Modifiers modifiers = Modifiers.None,
        params IEnumerable<Parameter> args)
    {
        var modtext = modifiers.Format(appendSpace: true);

        Write($"{visibility.Format()} {modtext}{returnType} {name}(");
        var argCounter = 0;
        foreach (var arg in args)
        {
            if (argCounter++ > 0)
            {
                Write(", ");
            }

            if (arg.IsOut)
            {
                Write($"out {arg.Type} {arg.Name}");
                continue;
            }

            Write($"{arg.Type} {arg.Name}");
        }

        WriteLine(")");
        OpenScope();
    }

    public virtual void EndMethod(bool addTrailingLine = true) => CloseScope(addTrailingLine: addTrailingLine);

    public virtual void EmptyConstructor(
        string name,
        string? baseName = null,
        Visibility visibility = Visibility.Public,
        Modifiers modifiers = Modifiers.None,
        params (string Type, string Name)[] args)
    {
        BeginConstructor(name, baseName, visibility, modifiers, args);
        CloseScope();
    }

    public virtual void PublicConstructor(string name, params (string Type, string Name)[] args) =>
        BeginConstructor(name, null, Visibility.Public, Modifiers.None, args);

    public virtual void BeginConstructor(
        string name,
        string? baseName = null,
        Visibility visibility = Visibility.Public,
        Modifiers modifiers = Modifiers.None,
        params (string Type, string Name)[] args)
    {
        var modtext = modifiers.Format(appendSpace: true);

        var forwardArgs = false;
        if (baseName?.StartsWith(">") ?? false)
        {
            baseName = baseName.Substring(1);
            forwardArgs = true;
        }

        Write($"{visibility.Format()}{modtext}{name}(");
        var argCounter = 0;
        foreach (var arg in args)
        {
            if (argCounter++ > 0)
            {
                Write(", ");
            }

            Write($"{arg.Type} {arg.Name}");
        }

        WriteLine(")");

        if (baseName.NotNullOrEmpty())
        {
            PushIndent();
            if (forwardArgs)
            {
                Write(": base(");
                argCounter = 0;
                foreach (var arg in args)
                {
                    if (argCounter++ > 0)
                    {
                        Write(", ");
                    }

                    Write($"{arg.Name}");
                }

                WriteLine(")");
            }
            else
            {
                WriteLine(
                    baseName!.StartsWith("this")
                        ? baseName == "this"
                            ? ": this()"
                            : ": " + baseName
                        : baseName == "base"
                            ? ": base()"
                            : ": " + baseName);
            }

            PopIndent();
        }

        OpenScope();
    }

    public virtual IDisposable Constructor(string name, params (string Type, string Name)[] args)
    {
        BeginConstructor(name, null, Visibility.Public, Modifiers.None, args);
        return new ScopeContext(this, false);
    }

    public virtual IDisposable Constructor(
        string name,
        string baseName,
        params (string Type, string Name)[] args)
    {
        BeginConstructor(name, baseName, Visibility.Public, Modifiers.None, args);
        return new ScopeContext(this, false);
    }

    public virtual IDisposable Constructor(
        string name,
        Visibility visibility,
        params (string Type, string Name)[] args)
    {
        BeginConstructor(name, null, visibility, Modifiers.None, args);
        return new ScopeContext(this, false);
    }

    public virtual IDisposable Constructor(
        string name,
        Visibility visibility,
        Modifiers modifiers,
        params (string Type, string Name)[] args)
    {
        BeginConstructor(name, null, visibility, modifiers, args);
        return new ScopeContext(this, false);
    }

    public virtual IDisposable Constructor(
        string name,
        string? baseName = null,
        Visibility visibility = Visibility.Public,
        Modifiers modifiers = Modifiers.None,
        params (string Type, string Name)[] args)
    {
        BeginConstructor(name, baseName, visibility, modifiers, args);
        return new ScopeContext(this, false);
    }

    public virtual IDisposable PragmaIf(string condition)
    {
        BeginPragmaIf(condition);
        return new ScopeContext(this, false);
    }

    public virtual void BeginPragmaIf(string condition)
    {
        WriteLine($"#if {condition}");
        PushIndent();
    }

    public virtual void PragmaElse()
    {
        PopIndent();
        WriteLine("#else");
        PushIndent();
    }

    public virtual void EndPragmaIf(bool addTrailingLine = true)
    {
        PopIndent();
        WriteLine("#endif");
        if (addTrailingLine)
        {
            RawWriteLine();
        }
    }

    public virtual void Attribute(string a) => WriteLine("[{0}]", a);

    public virtual void AttributeLine() => WriteLine();

    public virtual IDisposable If(string condition)
    {
        WriteLine($"if ({condition})");
        return Scope(true);
    }

    public virtual void Else()
    {
        CloseScope(addTrailingLine: true);
        WriteLine("else");
        OpenScope();
    }

    public virtual void Statement(string text) => WriteLine($"{text};");

    public virtual void Return(string? value = null)
    {
        if (value.NotNullOrEmpty())
        {
            WriteLine($"return {value};");
            return;
        }

        WriteLine("return;");
    }

    public virtual void Field(
        string type,
        string name,
        string? initializer = null,
        Visibility visibility = Visibility.Private,
        params Modifiers[] modifiers)
    {
        var modtext = modifiers.Format();
        if (modtext.Length > 0)
        {
            modtext = " " + modtext;
        }

        initializer = initializer.NotNullOrEmpty()
            ? " = " + initializer
            : "";

        WriteLine($"{visibility.Format()}{modtext} {type} {name}{initializer};");
    }

    public virtual void BeginInterface(string interfaceName, Visibility visibility = Visibility.Public) =>
        BeginInterface(interfaceName, null, visibility);

    public void ReadonlyProperty(
        string type,
        string name,
        string value,
        Visibility visibility = Visibility.Public,
        params Modifiers[] modifiers)
    {
        var modtext = modifiers.Format();
        if (modtext.Length > 0)
        {
            modtext = " " + modtext;
        }

        WriteLine($"{visibility.Format()}{modtext} {type} {name}");
        OpenScope();
        WriteLine($"get => {value};");
        CloseScope();
    }

    public virtual IDisposable Property(
        string type,
        string name,
        Visibility visibility = Visibility.Public,
        params Modifiers[] modifiers)
    {
        var modtext = modifiers.Format();
        if (modtext.Length > 0)
        {
            modtext = " " + modtext;
        }

        WriteLine($"{visibility.Format()}{modtext} {type} {name}");
        return PropertyScope(true, cleanup: () => _getWritten = false, propertyVisibility: visibility);
    }

    public virtual void GetExpression(
        string expression,
        Visibility visibility = Visibility.Public,
        params Modifiers[] modifiers)
    {
        var modtext = modifiers.Format();
        if (modtext.Length > 0)
        {
            modtext = " " + modtext;
        }

        var accessText = _currentScope?.PropertyVisibility == visibility
            ? ""
            : visibility.Format();

        WriteLine($"{accessText}{modtext} get => {expression};");
    }

    public virtual void SetExpression(
        string expression,
        Visibility visibility = Visibility.Public,
        params Modifiers[] modifiers)
    {
        var modtext = modifiers.Format();
        if (modtext.Length > 0)
        {
            modtext = " " + modtext;
        }

        var accessText = _currentScope?.PropertyVisibility == visibility
            ? ""
            : visibility.Format();

        WriteLine($"{accessText}{modtext} set => {expression};");
    }

    public virtual IDisposable Get(Visibility visibility = Visibility.Public, params Modifiers[] modifiers)
    {
        _getWritten = true;

        var modtext = modifiers.Format();
        if (modtext.Length > 0)
        {
            modtext = " " + modtext;
        }

        var accessText = _currentScope?.PropertyVisibility == visibility
            ? ""
            : visibility.Format();

        WriteLine($"{accessText}{modtext} get");

        return Scope(true);
    }

    public virtual IDisposable Set(Visibility visibility = Visibility.Public, params Modifiers[] modifiers)
    {
        if (_getWritten)
        {
            WriteLine();
        }

        var modtext = modifiers.Format();
        if (modtext.Length > 0)
        {
            modtext = " " + modtext;
        }

        var accessText = _currentScope?.PropertyVisibility == visibility
            ? ""
            : visibility.Format();

        WriteLine($"{accessText}{modtext} set");

        return Scope(true);
    }

    public void PropertyExpression(
        string type,
        string name,
        string value,
        Visibility visibility = Visibility.Public,
        params Modifiers[] modifiers)
    {
        var modtext = modifiers.Format();
        if (modtext.Length > 0)
        {
            modtext = " " + modtext;
        }

        WriteLine($"{visibility.Format()}{modtext} {type} {name} => {value};");
    }

    public void AutoProperty(
        string type,
        string name,
        Visibility visibility = Visibility.Public,
        Visibility setterVisibility = Visibility.Public,
        params Modifiers[] modifiers)
    {
        var modtext = modifiers.Format();
        if (modtext.Length > 0)
        {
            modtext = " " + modtext;
        }

        var setterAccessText = setterVisibility == visibility
            ? ""
            : setterVisibility.Format() + " ";

        WriteLine($"{visibility.Format()}{modtext} {type} {name} {{ get; {setterAccessText}set; }}");
    }

    public virtual IDisposable Interface(
        string interfaceName,
        string? baseInterfaceName = null,
        Visibility visibility = Visibility.Public)
    {
        BeginInterface(interfaceName, baseInterfaceName, visibility);
        return Scope();
    }

    public virtual void BeginInterface(
        string interfaceName,
        string? baseInterfaceName,
        Visibility visibility = Visibility.Public)
    {
        Write($"{visibility.Format()} interface {interfaceName}");

        if (!string.IsNullOrEmpty(baseInterfaceName))
        {
            Write($" : {baseInterfaceName}");
        }

        OpenScope();
    }

    public virtual void EndInterface(bool addTrailingLine = true) => CloseScope(addTrailingLine: addTrailingLine);

    private class ScopeContext : IDisposable
    {
        private readonly bool _addTrailingLine;
        private readonly Action? _cleanup;
        private readonly bool _isStatement;
        public CodeWriter Writer { get; }

        public ScopeContext(
            CodeWriter writer,
            bool addTrailingLine,
            bool isStatement = false,
            Action? cleanup = null,
            Visibility propertyVisibility = Visibility.Public)
        {
            PropertyVisibility = propertyVisibility;
            Writer = writer;
            _addTrailingLine = addTrailingLine;
            _isStatement = isStatement;
            _cleanup = cleanup;
        }

        public Visibility PropertyVisibility { get; }

        public virtual void Dispose()
        {
            Writer.CloseScope(
                _isStatement
                    ? "};"
                    : "}",
                _addTrailingLine);
            _cleanup?.Invoke();
        }
    }
}
