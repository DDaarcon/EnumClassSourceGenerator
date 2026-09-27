using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace EnumClassSourceGenerator.UnitTests.Tests;

public class EnumClassValueAttributeTests
{
    [Fact]
    public void When_ExplicitIndexesAreMixedWithAutomaticIndexes_Then_ShouldContinueFromPreviousValue()
    {
        const string source = """
            using EnumClasses;
            using ValueDefinition = EnumClasses.EnumClassValueAttribute;

            namespace Consumer;

            [EnumClass(GenerateJsonConverter = false)]
            internal partial class MixedIndexesEnum
            {
                private const int IndexAnchor = 10;

                public static MixedIndexesEnum Zero { get; } = new();

                [ValueDefinition(IndexAnchor + 2)]
                public static MixedIndexesEnum Twelve { get; } = new();

                [EnumClassValue]
                public static readonly MixedIndexesEnum Thirteen = new();

                [global::EnumClasses.EnumClassValue(20)]
                public static MixedIndexesEnum Twenty { get; } = new();

                public static readonly MixedIndexesEnum TwentyOne = new();
            }
            """;

        var (result, outputCompilation) = RunGenerator(source);

        result.Diagnostics.Should().NotContain(x => x.Severity == DiagnosticSeverity.Error);
        GetErrors(outputCompilation).Should().BeEmpty();

        var generatedSource = GetGeneratedSource(result, "Consumer.MixedIndexesEnum.g.cs");
        generatedSource.Should().Contain("Zero.EnumIndex = 0;");
        generatedSource.Should().Contain("Twelve.EnumIndex = 12;");
        generatedSource.Should().Contain("Thirteen.EnumIndex = 13;");
        generatedSource.Should().Contain("Twenty.EnumIndex = 20;");
        generatedSource.Should().Contain("TwentyOne.EnumIndex = 21;");

        generatedSource.Contains("0 => Zero,").Should().BeTrue();
        generatedSource.Contains("12 => Twelve,").Should().BeTrue();
        generatedSource.Contains("13 => Thirteen,").Should().BeTrue();
        generatedSource.Contains("20 => Twenty,").Should().BeTrue();
        generatedSource.Contains("21 => TwentyOne,").Should().BeTrue();
    }

    [Fact]
    public void When_ExplicitIndexIsNegative_Then_ShouldContinueTowardZero()
    {
        const string source = """
            using EnumClasses;

            namespace Consumer;

            [EnumClass(GenerateJsonConverter = false)]
            internal partial class NegativeIndexesEnum
            {
                [EnumClassValue(-2)]
                public static NegativeIndexesEnum NegativeTwo { get; } = new();

                public static NegativeIndexesEnum NegativeOne { get; } = new();
            }
            """;

        var (result, outputCompilation) = RunGenerator(source);

        result.Diagnostics.Should().NotContain(x => x.Severity == DiagnosticSeverity.Error);
        GetErrors(outputCompilation).Should().BeEmpty();

        var generatedSource = GetGeneratedSource(result, "Consumer.NegativeIndexesEnum.g.cs");
        generatedSource.Should().Contain("NegativeTwo.EnumIndex = -2;");
        generatedSource.Should().Contain("NegativeOne.EnumIndex = -1;");
        generatedSource.Contains("-2 => NegativeTwo,").Should().BeTrue();
        generatedSource.Contains("-1 => NegativeOne,").Should().BeTrue();
    }

    [Fact]
    public void When_TwoValuesResolveToSameIndex_Then_ShouldReportBothValues()
    {
        const string source = """
            using EnumClasses;

            namespace Consumer;

            [EnumClass(GenerateJsonConverter = false)]
            internal partial class DuplicateIndexesEnum
            {
                [EnumClassValue(4)]
                public static DuplicateIndexesEnum First { get; } = new();

                [EnumClassValue(2 + 2)]
                public static DuplicateIndexesEnum Second { get; } = new();
            }
            """;

        var (result, _) = RunGenerator(source);

        var diagnostics = result.Diagnostics.Where(x => x.Id == "ENUMCLGEN012").ToArray();
        diagnostics.Should().HaveCount(2);
        diagnostics.Should().Contain(x => x.GetMessage().Contains("'First'") && x.GetMessage().Contains("'Second'"));
        diagnostics.Should().Contain(x => x.GetMessage().Contains("'Second'") && x.GetMessage().Contains("'First'"));

        var generatedSource = GetGeneratedSource(result, "Consumer.DuplicateIndexesEnum.g.cs");
        generatedSource.Should().NotContain("First.EnumIndex");
        generatedSource.Should().NotContain("Second.EnumIndex");
    }

    [Fact]
    public void When_AttributedCommaSeparatedFieldsShareAnIndex_Then_ShouldReportCollision()
    {
        const string source = """
            using EnumClasses;

            namespace Consumer;

            [EnumClass(GenerateJsonConverter = false)]
            internal partial class SharedAttributeEnum
            {
                [EnumClassValue(7)]
                public static readonly SharedAttributeEnum First = new(), Second = new();
            }
            """;

        var (result, _) = RunGenerator(source);

        result.Diagnostics.Where(x => x.Id == "ENUMCLGEN012")
            .Select(x => x.GetMessage())
            .Should().BeEquivalentTo(
                "Enum Class 'SharedAttributeEnum' value 'First' has the same index as 'Second'",
                "Enum Class 'SharedAttributeEnum' value 'Second' has the same index as 'First'");
    }

    [Fact]
    public void When_AutomaticIndexWouldExceedIntMaxValue_Then_ShouldReportCollision()
    {
        const string source = """
            using EnumClasses;

            namespace Consumer;

            [EnumClass(GenerateJsonConverter = false)]
            internal partial class MaximumIndexEnum
            {
                [EnumClassValue(int.MaxValue)]
                public static MaximumIndexEnum Maximum { get; } = new();

                public static MaximumIndexEnum Following { get; } = new();
            }
            """;

        var (result, _) = RunGenerator(source);

        result.Diagnostics.Count(x => x.Id == "ENUMCLGEN012").Should().Be(2);

        var generatedSource = GetGeneratedSource(result, "Consumer.MaximumIndexEnum.g.cs");
        generatedSource.Should().NotContain("Maximum.EnumIndex");
        generatedSource.Should().NotContain("Following.EnumIndex");
    }

    private static (GeneratorRunResult Result, Compilation OutputCompilation) RunGenerator(string source)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        var referencePaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Append(typeof(global::EnumClasses.EnumClassAttribute).Assembly.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var compilation = CSharpCompilation.Create(
            assemblyName: "EnumClassValueAttributeTests",
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

    private static string GetGeneratedSource(GeneratorRunResult result, string hintName)
        => result.GeneratedSources
            .Single(x => x.HintName == hintName)
            .SourceText
            .ToString();

    private static Diagnostic[] GetErrors(Compilation compilation)
        => compilation.GetDiagnostics()
            .Where(x => x.Severity == DiagnosticSeverity.Error)
            .ToArray();
}
