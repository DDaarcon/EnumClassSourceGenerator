using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;

namespace EnumClassSourceGenerator.UnitTests.Tests;

public class UnsupportedDeclarationTests
{
    [Fact]
    public void When_EnumClassIsNested_Then_ShouldReportDedicatedDiagnosticAndSkipGeneration()
    {
        const string source = """
            using GenEnumClass;

            namespace Consumer;

            internal partial class Container
            {
                [EnumClass]
                internal partial class NestedEnum
                {
                }
            }
            """;

        var result = RunGenerator(source);

        result.Diagnostics.Should().ContainSingle(x => x.Id == "ENUMCLGEN006");
        result.GeneratedSources.Should().NotContain(x => x.HintName == "NestedEnum.g.cs");
    }

    [Fact]
    public void When_EnumClassIsGeneric_Then_ShouldReportDedicatedDiagnosticAndSkipGeneration()
    {
        const string source = """
            using GenEnumClass;

            namespace Consumer;

            [EnumClass]
            internal partial class GenericEnum<T>
            {
            }
            """;

        var result = RunGenerator(source);

        result.Diagnostics.Should().ContainSingle(x => x.Id == "ENUMCLGEN007");
        result.GeneratedSources.Should().NotContain(x => x.HintName == "GenericEnum.g.cs");
    }

    private static GeneratorRunResult RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(
            source,
            new CSharpParseOptions(LanguageVersion.Preview));

        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));

        var compilation = CSharpCompilation.Create(
            assemblyName: "GeneratorTests",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new global::EnumClassSourceGenerator.EnumClassSourceGenerator().AsSourceGenerator());

        driver = driver.RunGenerators(compilation);

        return driver.GetRunResult().Results.Single();
    }
}
