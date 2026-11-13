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
    public void When_NullableDirectiveIsUnsupported_Then_ShouldExcludeIt()
    {
        var (result, _) = RunGenerator(LanguageVersion.CSharp7, generateJsonConverter: true);

        GetGeneratedSource(result).Should().NotContain("#nullable");
        GetGeneratedJsonConverterSource(result).Should().NotContain("#nullable");
    }

    [Fact]
    public void When_NullableDirectiveIsSupported_Then_ShouldIncludeIt()
    {
        var (result, _) = RunGenerator(LanguageVersion.CSharp8, generateJsonConverter: true);

        GetGeneratedSource(result).Should().Contain("#nullable enable").And.Contain("#nullable disable");
        GetGeneratedJsonConverterSource(result)
            .Should().Contain("#nullable enable").And.Contain("#nullable disable");
    }

    [Fact]
    public void When_NullableReferenceAnnotationsAreUnsupported_Then_ShouldExcludeThem()
    {
        var (result, _) = RunGenerator(
            LanguageVersion.CSharp7,
            generateJsonConverter: true,
            generateRawEnum: true);

        var generatedSource = GetGeneratedSource(result);
        generatedSource.Should().NotContain("RuntimeIndexedEnum?");
        generatedSource.Should().NotContain("string?");
        generatedSource.Should().NotContain("object?");
        generatedSource.Should().NotContain("System.Action?");
        generatedSource.Should().NotContain("System.Func<TResult>?");
        generatedSource.Should().NotContain("TResult?");
        generatedSource.Should().NotContain("TValue?");
        generatedSource.Should().NotContain("null!");
        generatedSource.Should().Contain("private Raw? _rawValue;");

        GetGeneratedJsonConverterSource(result).Should().NotContain("RuntimeIndexedEnum?");
    }

    [Fact]
    public void When_NullableReferenceAnnotationsAreSupported_Then_ShouldIncludeThem()
    {
        var (result, _) = RunGenerator(LanguageVersion.CSharp8);

        var generatedSource = GetGeneratedSource(result);
        generatedSource.Should().Contain("RuntimeIndexedEnum?");
        generatedSource.Should().Contain("string?");
        generatedSource.Should().Contain("object?");
        generatedSource.Should().Contain("System.Action?");
        generatedSource.Should().Contain("TValue?");
        generatedSource.Should().NotContain("public TResult? Match<TResult>(");
    }

    [Fact]
    public void When_UnconstrainedNullableTypeParameterAnnotationIsSupported_Then_ShouldIncludeIt()
    {
        var (result, _) = RunGenerator(LanguageVersion.CSharp9);

        GetGeneratedSource(result).Should().Contain("public TResult? Match<TResult>(");
    }

    [Fact]
    public void When_SwitchExpressionIsUnsupported_Then_ShouldUseSwitchStatements()
    {
        var (result, _) = RunGenerator(
            LanguageVersion.CSharp7,
            generateRawEnum: true,
            useRuntimeIndexes: false);

        var generatedSource = GetGeneratedSource(result);
        generatedSource.Should().Contain("switch (index)");
        generatedSource.Should().Contain("case 0: return One;");
        generatedSource.Should().Contain("switch (value)");
        generatedSource.Should().Contain("case Raw.One:");
        generatedSource.Should().Contain("result = default(Raw);");
        generatedSource.Should().NotContain("return index switch");
        generatedSource.Should().NotContain("result = value switch");
        generatedSource.Should().NotContain("result = default;");
    }

    [Fact]
    public void When_SwitchExpressionIsSupported_Then_ShouldUseSwitchExpressions()
    {
        var (result, _) = RunGenerator(
            LanguageVersion.CSharp8,
            generateRawEnum: true,
            useRuntimeIndexes: false);

        var generatedSource = GetGeneratedSource(result);
        generatedSource.Should().Contain("return index switch");
        generatedSource.Should().Contain("result = value switch");
    }

    [Fact]
    public void When_SpanBasedApisAreUnsupported_Then_ShouldUseStringOnlySerialization()
    {
        var (result, _) = RunGenerator(LanguageVersion.CSharp7);

        var generatedSource = GetGeneratedSource(result);
        generatedSource.Should().NotContain("System.ReadOnlySpan<char>");
        generatedSource.Should().NotContain("System.MemoryExtensions.AsSpan");
        generatedSource.Should().Contain("System.String.Equals(serializedValue");
    }

    [Fact]
    public void When_SpanBasedApisAreSupported_Then_ShouldIncludeSpanSerializationOverloads()
    {
        var (result, _) = RunGenerator(LanguageVersion.CSharp7_2);

        var generatedSource = GetGeneratedSource(result);
        generatedSource.Should().Contain(
            "Deserialize(System.ReadOnlySpan<char> serializedValue)");
        generatedSource.Should().Contain(
            "TryDeserialize(System.ReadOnlySpan<char> serializedValue");
        generatedSource.Should().Contain(
            "ContainsSerializedValue(System.ReadOnlySpan<char> serializedValue)");
        generatedSource.Should().Contain("System.MemoryExtensions.AsSpan");
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
        bool generateJsonConverter = false,
        bool generateRawEnum = false,
        bool useRuntimeIndexes = true)
    {
        var source = """
            using EnumClasses;

            namespace Consumer
            {
                [EnumClass(
                    GenerateJsonConverter = GENERATE_JSON_CONVERTER,
                    GenerateRawEnum = GENERATE_RAW_ENUM,
                    IndexAssignmentMode = INDEX_ASSIGNMENT_MODE)]
                internal partial class RuntimeIndexedEnum
                {
                    public static SpecializedRuntimeIndexedEnum One { get; }
                        = new SpecializedRuntimeIndexedEnum() ENUM_INDEX_ASSIGNMENT;
                }

                internal sealed class SpecializedRuntimeIndexedEnum : RuntimeIndexedEnum { }
            }
            """.Replace(
                "GENERATE_JSON_CONVERTER",
                generateJsonConverter ? "true" : "false")
            .Replace(
                "GENERATE_RAW_ENUM",
                generateRawEnum ? "true" : "false")
            .Replace(
                "INDEX_ASSIGNMENT_MODE",
                useRuntimeIndexes
                    ? "IndexAssignmentMode.RuntimeAtValueInit"
                    : "IndexAssignmentMode.CompileTime")
            .Replace(
                "ENUM_INDEX_ASSIGNMENT",
                useRuntimeIndexes ? "{ EnumIndex = 1 }" : "");

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
