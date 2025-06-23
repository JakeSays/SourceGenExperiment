using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;


namespace WayCoolStuff.Tests;

internal class TestConfigOptions : AnalyzerConfigOptions
{
    private readonly Dictionary<string, string> _options;

    public TestConfigOptions(Dictionary<string, string> analyzerOptions)
    {
        _options = analyzerOptions;
    }
    public override bool TryGetValue(string key, out string value)
        => _options.TryGetValue(key, out value!);
}

internal sealed class TestConfigOptionsProvider : AnalyzerConfigOptionsProvider
{
    private const string DispatcherErrorClassOption = "build_property.DispatcherSourceGenerator_DispatcherErrorClassName";

    private readonly Dictionary<string, string> _analyzerOptions;

    public TestConfigOptionsProvider(string errorClassName)
    {
        _analyzerOptions = new Dictionary<string, string>
        {
            { DispatcherErrorClassOption, errorClassName }
        };
    }

    public override AnalyzerConfigOptions GlobalOptions => new TestConfigOptions(_analyzerOptions);

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => throw new System.NotSupportedException();
    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => throw new System.NotSupportedException();
}
