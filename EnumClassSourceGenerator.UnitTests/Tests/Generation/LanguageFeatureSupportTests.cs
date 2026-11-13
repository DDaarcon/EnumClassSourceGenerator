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
        generatedSource.Should().Contain(
            "System.Collections.Immutable.ImmutableArray.Create<RuntimeIndexedEnum>(");
        generatedSource.Should().NotContain("_allValues = [");
        GetErrors(outputCompilation).Should().NotContain(diagnostic =>
            diagnostic.GetMessage().Contains("required members", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void When_RequiredKeywordIsSupported_Then_ShouldIncludeItInEnumIndexProperty()
    {
        var (result, _) = RunGenerator(LanguageVersion.CSharp11);

        GetGeneratedSource(result).Should().Contain("public required int EnumIndex");
    }

    [Fact]
    public void When_InitKeywordIsUnsupported_Then_ShouldUseSetAndReportWarningOnEnumClassAttribute()
    {
        var (result, _) = RunGenerator(LanguageVersion.CSharp8);

        var generatedSource = GetGeneratedSource(result);
        generatedSource.Should().Contain("\n    set\n");
        generatedSource.Should().NotContain("\n    init\n");

        var warning = result.Diagnostics.Should().ContainSingle(diagnostic =>
                diagnostic.Id == "ENUMCLGEN018"
                && diagnostic.Severity == DiagnosticSeverity.Warning)
            .Which;
        warning.GetMessage().Should().Contain("using a public set accessor instead");
        warning.GetMessage().Should().Contain("may make index lookups inconsistent");
        warning.Location.SourceTree.Should().NotBeNull();
        warning.Location.SourceTree!.GetText().ToString(warning.Location.SourceSpan)
            .Should().StartWith("EnumClass(");
    }

    [Fact]
    public void When_InitKeywordIsSupported_Then_ShouldUseInitWithoutFallbackWarning()
    {
        var (result, _) = RunGenerator(LanguageVersion.CSharp9);

        GetGeneratedSource(result).Should().Contain("\n    init\n");
        result.Diagnostics.Should().NotContain(diagnostic => diagnostic.Id == "ENUMCLGEN018");
    }

    [Fact]
    public void When_NotPatternIsUnsupported_Then_ShouldUseBasicNullComparison()
    {
        var (result, _) = RunGenerator(LanguageVersion.CSharp8);

        var generatedSource = GetGeneratedSource(result);
        generatedSource.Should().Contain("return result != null;");
        generatedSource.Should().Contain("=> GetByEnumIndex(index) != null;");
        generatedSource.Should().NotContain(" is not null");
    }

    [Fact]
    public void When_NotPatternIsSupported_Then_ShouldUseNotNullPattern()
    {
        var (result, _) = RunGenerator(LanguageVersion.CSharp9);

        var generatedSource = GetGeneratedSource(result);
        generatedSource.Should().Contain("return result is not null;");
        generatedSource.Should().Contain("=> GetByEnumIndex(index) is not null;");
    }

    [Fact]
    public void When_Utf8StringLiteralIsUnsupported_Then_ShouldUseStringComparison()
    {
        var (result, _) = RunGenerator(LanguageVersion.CSharp10, generateJsonConverter: true);

        var generatedSource = GetGeneratedJsonConverterSource(result);
        generatedSource.Should().Contain("reader.ValueTextEquals(\"One\")");
        generatedSource.Should().NotContain("reader.ValueTextEquals(\"One\"u8)");
    }

    [Fact]
    public void When_Utf8StringLiteralIsSupported_Then_ShouldUseUtf8Comparison()
    {
        var (result, _) = RunGenerator(LanguageVersion.CSharp11, generateJsonConverter: true);

        GetGeneratedJsonConverterSource(result)
            .Should().Contain("reader.ValueTextEquals(\"One\"u8)");
    }

    private static (GeneratorRunResult Result, Compilation OutputCompilation) RunGenerator(
        LanguageVersion languageVersion,
        bool generateJsonConverter = false)
    {
        var source = """
            using EnumClasses;

            namespace Consumer;

            [EnumClass(
                GenerateJsonConverter = GENERATE_JSON_CONVERTER,
                IndexAssignmentMode = IndexAssignmentMode.RuntimeAtValueInit)]
            internal partial class RuntimeIndexedEnum
            {
                public static RuntimeIndexedEnum One { get; } = new RuntimeIndexedEnum() { EnumIndex = 1 };
            }
            """.Replace(
                "GENERATE_JSON_CONVERTER",
                generateJsonConverter ? "true" : "false");

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

    private static string GetGeneratedJsonConverterSource(GeneratorRunResult result)
        => result.GeneratedSources
            .Single(x => x.HintName == "Consumer.RuntimeIndexedEnumJsonConverter.g.cs")
            .SourceText
            .ToString();

    private static Diagnostic[] GetErrors(Compilation compilation)
        => compilation.GetDiagnostics()
            .Where(x => x.Severity == DiagnosticSeverity.Error)
            .ToArray();
}
