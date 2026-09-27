using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace EnumClassSourceGenerator.UnitTests.Tests;

public class FullyQualifiedTypeNameTests
{
    [Fact]
    public void When_DerivedValuesUseImportedTypes_Then_ShouldGenerateFullyQualifiedTypeNames()
    {
        const string source = """
            using EnumClasses;
            using ExternalValues;

            namespace Consumer
            {
                [EnumClass(GenerateJsonConverter = false)]
                internal partial class Status
                {
                    public static readonly FieldValue Field = new();
                    public static PropertyValue Property { get; } = new();
                }
            }

            namespace ExternalValues
            {
                internal sealed class FieldValue : Consumer.Status
                {
                }

                internal sealed class PropertyValue : Consumer.Status
                {
                }
            }
            """;

        var syntaxTree = CSharpSyntaxTree.ParseText(
            source,
            new CSharpParseOptions(LanguageVersion.Preview));

        var referencePaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Append(typeof(global::EnumClasses.EnumClassAttribute).Assembly.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var compilation = CSharpCompilation.Create(
            assemblyName: "FullyQualifiedTypeNameTests",
            syntaxTrees: [syntaxTree],
            references: referencePaths.Select(path => MetadataReference.CreateFromFile(path)),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [new global::EnumClasses.SourceGenerators.EnumClassSourceGenerator().AsSourceGenerator()],
            parseOptions: (CSharpParseOptions)syntaxTree.Options);

        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out var generatorDiagnostics);

        generatorDiagnostics.Should().NotContain(x => x.Severity == DiagnosticSeverity.Error);
        outputCompilation.GetDiagnostics()
            .Where(x => x.Severity == DiagnosticSeverity.Error)
            .Should()
            .BeEmpty();

        var generatedSource = driver.GetRunResult().Results
            .Single()
            .GeneratedSources
            .Single()
            .SourceText
            .ToString();

        generatedSource.Should().Contain("typeof(global::ExternalValues.FieldValue)");
        generatedSource.Should().Contain("typeof(global::ExternalValues.PropertyValue)");
    }
}
