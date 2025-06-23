using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;


namespace WayCoolStuff.Tests;

public class DispatcherSourceGeneratorTests
{
    [Fact]
    public void TestOne() => ExecuteTest(TestCases.TestOne);

    [Fact]
    public void TestTwo() => ExecuteTest(TestCases.TestTwo);

    private void ExecuteTest(TestCase test)
    {
        var generator = new DispatcherSourceGenerator();

        var driver = CSharpGeneratorDriver.Create(generator)
            .WithUpdatedAnalyzerConfigOptions(test.Options);

        var sourceTree = CSharpSyntaxTree.ParseText(test.SourceCode);
        var compilation = CSharpCompilation.Create(
            "FooAssembly",
            [sourceTree],
            [
                // Add reference to 'System.Private.CoreLib'.
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            ]);

        var runResult = driver.RunGenerators(compilation)
            .GetRunResult();

        var expectedTree = CSharpSyntaxTree.ParseText(test.GeneratedCode);
        var actualTree = runResult.GeneratedTrees.Single(t => t.FilePath.EndsWith(".g.cs"));

        Assert.True(actualTree.IsEquivalentTo(expectedTree));
    }
}
