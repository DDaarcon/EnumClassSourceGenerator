using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace EnumClassSourceGenerator.UnitTests.Tests;

public class UnsupportedDeclarationTests
{
    [Fact]
    public void When_EnumClassIsNested_Then_ShouldReportDedicatedDiagnosticAndSkipGeneration()
    {
        const string source = """
            using EnumClasses;

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
            using EnumClasses;

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

    [Fact]
    public void When_EnumClassValueUsesEscapedReservedName_Then_ShouldReportCollisionAndSkipValue()
    {
        const string source = """
            using EnumClasses;

            namespace Consumer;

            [EnumClass(GenerateJsonConverter = false)]
            internal partial class EscapedIdentifierEnum
            {
                public static readonly EscapedIdentifierEnum @Switch = new();
            }
            """;

        var result = RunGenerator(source);

        result.Diagnostics.Should().ContainSingle(x => x.Id == "ENUMCLGEN008");

        var generatedSource = result.GeneratedSources
            .Single(x => x.HintName == "Consumer.EscapedIdentifierEnum.g.cs")
            .SourceText
            .ToString();

        generatedSource.Should().NotContain("@Switch._serializedName");
    }

    [Fact]
    public void When_EnumClassValueUsesEscapedKeyword_Then_ShouldPreserveEscapingInGeneratedSource()
    {
        const string source = """
            using EnumClasses;

            namespace Consumer;

            [EnumClass(GenerateJsonConverter = false)]
            internal partial class EscapedIdentifierEnum
            {
                public static readonly EscapedIdentifierEnum @class = new();
            }
            """;

        var syntaxTree = CSharpSyntaxTree.ParseText(
            source,
            new CSharpParseOptions(LanguageVersion.Preview));

        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));

        var compilation = CSharpCompilation.Create(
            assemblyName: "EscapedIdentifierTests",
            syntaxTrees: [syntaxTree],
            references: references,
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

        generatedSource.Should().Contain("@class._serializedName = nameof(@class);");
    }

    [Theory]
    [InlineData("public static InvalidAccessorEnum Value => new();")]
    [InlineData("public static InvalidAccessorEnum Value { get { return new(); } }")]
    [InlineData("public static InvalidAccessorEnum Value { get; set; } = new();")]
    [InlineData("public static InvalidAccessorEnum Value { get; init; } = new();")]
    [InlineData("public static InvalidAccessorEnum Value { get; }")]
    public void When_EnumClassValueIsNotInitializedGetOnlyAutoProperty_Then_ShouldReportInvalidAccessors(
        string propertyDeclaration)
    {
        var source = $$"""
            using EnumClasses;

            namespace Consumer;

            [EnumClass]
            internal partial class InvalidAccessorEnum
            {
                {{propertyDeclaration}}
            }
            """;

        var result = RunGenerator(source);

        var diagnostic = result.Diagnostics.Should()
            .ContainSingle(x => x.Id == "ENUMCLGEN002")
            .Subject;

        diagnostic.GetMessage().Should().Contain("'Value'");
        diagnostic.GetMessage().Should().Contain("'InvalidAccessorEnum'");

        var generatedSource = string.Join(
            Environment.NewLine,
            result.GeneratedSources.Select(x => x.SourceText.ToString()));

        generatedSource.Should().NotContain("Value._serializedName");
        result.Diagnostics.Should().NotContain(x => x.Id == "ENUMCLGEN005");
    }

    [Fact]
    public void When_EnumClassValueFieldIsUninitialized_Then_ShouldReportDedicatedDiagnosticAndSkipValue()
    {
        const string source = """
            using EnumClasses;

            namespace Consumer;

            [EnumClass(GenerateJsonConverter = false)]
            internal partial class UninitializedFieldEnum
            {
                public static readonly UninitializedFieldEnum Broken;
            }
            """;

        var result = RunGenerator(source);

        var diagnostic = result.Diagnostics.Should()
            .ContainSingle(x => x.Id == "ENUMCLGEN010")
            .Subject;

        diagnostic.GetMessage().Should().Contain("'Broken'");
        diagnostic.GetMessage().Should().Contain("'UninitializedFieldEnum'");

        var generatedSource = result.GeneratedSources
            .Single(x => x.HintName == "Consumer.UninitializedFieldEnum.g.cs")
            .SourceText
            .ToString();

        generatedSource.Should().NotContain("Broken._serializedName");
        result.Diagnostics.Should().NotContain(x => x.Id == "ENUMCLGEN005");
    }

    [Fact]
    public void When_FieldDeclarationMixesInitializedAndUninitializedValues_Then_ShouldRejectOnlyUninitializedValue()
    {
        const string source = """
            using EnumClasses;

            namespace Consumer;

            [EnumClass(GenerateJsonConverter = false)]
            internal partial class MixedFieldEnum
            {
                public static readonly MixedFieldEnum Initialized = new(), Broken;
            }
            """;

        var result = RunGenerator(source);

        result.Diagnostics.Should()
            .ContainSingle(x => x.Id == "ENUMCLGEN010")
            .Which.GetMessage().Should().Contain("'Broken'");

        var generatedSource = result.GeneratedSources
            .Single(x => x.HintName == "Consumer.MixedFieldEnum.g.cs")
            .SourceText
            .ToString();

        generatedSource.Should().Contain("Initialized._serializedName");
        generatedSource.Should().NotContain("Broken._serializedName");
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
            new global::EnumClasses.SourceGenerators.EnumClassSourceGenerator().AsSourceGenerator());

        driver = driver.RunGenerators(compilation);

        return driver.GetRunResult().Results.Single();
    }
}
