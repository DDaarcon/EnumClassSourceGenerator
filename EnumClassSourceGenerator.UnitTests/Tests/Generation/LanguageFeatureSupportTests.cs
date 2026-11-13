using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace EnumClassSourceGenerator.UnitTests.Tests;

public class LanguageFeatureSupportTests
{
    [Fact]
    public void When_RequiredKeywordIsUnsupported_Then_ShouldExcludeItFromEnumIndexProperty()
    {
        var (result, outputCompilation) = RunGenerator(LanguageVersion.CSharp10);

        var generatedSource = GetGeneratedSource(result);
        generatedSource.Should().Contain("public int EnumIndex");
        generatedSource.Should().NotContain("public required int EnumIndex");
        GetErrors(outputCompilation).Should().NotContain(diagnostic =>
            diagnostic.GetMessage().Contains("required members", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void When_RequiredKeywordIsSupported_Then_ShouldIncludeItInEnumIndexProperty()
    {
        var (result, _) = RunGenerator(LanguageVersion.CSharp11);

        GetGeneratedSource(result).Should().Contain("public required int EnumIndex");
    }

    private static (GeneratorRunResult Result, Compilation OutputCompilation) RunGenerator(
        LanguageVersion languageVersion)
    {
        const string source = """
            using EnumClasses;

            namespace Consumer;

            [EnumClass(
                GenerateJsonConverter = false,
                IndexAssignmentMode = IndexAssignmentMode.RuntimeAtValueInit)]
            internal partial class RuntimeIndexedEnum
            {
                public static RuntimeIndexedEnum One { get; } = new() { EnumIndex = 1 };
            }
            """;

        var parseOptions = new CSharpParseOptions(languageVersion);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        var referencePaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Append(typeof(global::EnumClasses.EnumClassAttribute).Assembly.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var compilation = CSharpCompilation.Create(
            assemblyName: "LanguageFeatureSupportTests",
            syntaxTrees: [syntaxTree],
            references: referencePaths.Select(path => MetadataReference.CreateFromFile(path)),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [new global::EnumClasses.SourceGenerators.EnumClassSourceGenerator().AsSourceGenerator()],
            parseOptions: parseOptions);

        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out _);

        return (driver.GetRunResult().Results.Single(), outputCompilation);
    }

    private static string GetGeneratedSource(GeneratorRunResult result)
        => result.GeneratedSources
            .Single(x => x.HintName == "Consumer.RuntimeIndexedEnum.g.cs")
            .SourceText
            .ToString();

    private static Diagnostic[] GetErrors(Compilation compilation)
        => compilation.GetDiagnostics()
            .Where(x => x.Severity == DiagnosticSeverity.Error)
            .ToArray();
}
