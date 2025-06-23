namespace WayCoolStuff.Tests;

internal class TestCase
{
    public string SourceCode { get; }
    public string GeneratedCode { get; }
    public TestConfigOptionsProvider Options { get; }

    public TestCase(string sourceCode, string generatedCode, string errorClassName)
    {
        SourceCode = sourceCode;
        GeneratedCode = generatedCode;
        Options = new TestConfigOptionsProvider(errorClassName);
    }
}
