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
    public void When_ExplicitIndexIsSharedByCommaSeparatedFields_Then_ShouldRequireSeparateDeclarations()
    {
        const string source = """
            using EnumClasses;

            namespace Consumer;

            [EnumClass(GenerateJsonConverter = false)]
            internal partial class SharedAttributeEnum
            {
                [EnumClassValue(7)]
                public static readonly SharedAttributeEnum First = new(), Second = new();

                public static readonly SharedAttributeEnum Following = new();
            }
            """;

        var (result, _) = RunGenerator(source);

        var diagnostic = result.Diagnostics.Should()
            .ContainSingle(x => x.Id == "ENUMCLGEN014")
            .Which;
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Error);
        diagnostic.GetMessage().Should().Be(
            "EnumClassValueAttribute with an index cannot be applied to a field declaration containing multiple values; split the values into separate declarations");
        diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan)
            .Should().Be("EnumClassValue(7)");
        result.Diagnostics.Should().NotContain(x => x.Id == "ENUMCLGEN012");

        var generatedSource = GetGeneratedSource(result, "Consumer.SharedAttributeEnum.g.cs");
        generatedSource.Should().NotContain("First.EnumIndex");
        generatedSource.Should().NotContain("Second.EnumIndex");
        generatedSource.Should().Contain("Following.EnumIndex = 8;");
    }

    [Fact]
    public void When_ParameterlessAttributeIsSharedByCommaSeparatedFields_Then_ShouldAllowAutomaticIndexes()
    {
        const string source = """
            using EnumClasses;

            namespace Consumer;

            [EnumClass(GenerateJsonConverter = false)]
            internal partial class SharedMarkerAttributeEnum
            {
                [EnumClassValue]
                public static readonly SharedMarkerAttributeEnum First = new(), Second = new();
            }
            """;

        var (result, outputCompilation) = RunGenerator(source);

        result.Diagnostics.Should().NotContain(x => x.Id == "ENUMCLGEN014");
        GetErrors(outputCompilation).Should().BeEmpty();

        var generatedSource = GetGeneratedSource(result, "Consumer.SharedMarkerAttributeEnum.g.cs");
        generatedSource.Should().Contain("First.EnumIndex = 0;");
        generatedSource.Should().Contain("Second.EnumIndex = 1;");
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

    [Fact]
    public void When_ExplicitAttributeIndexesAreUsedWithRuntimeAssignment_Then_ShouldReportWarningForEachAttribute()
    {
        const string source = """
            using EnumClasses;

            namespace Consumer;

            [EnumClass(
                GenerateJsonConverter = false,
                IndexAssignmentMode = IndexAssignmentMode.Runtime)]
            internal partial class RuntimeIndexedEnum
            {
                [EnumClassValue(100)]
                public static RuntimeIndexedEnum First { get; } = new() { EnumIndex = 10 };

                [global::EnumClasses.EnumClassValue(200)]
                public static readonly RuntimeIndexedEnum Second = new() { EnumIndex = 20 };
            }
            """;

        var (result, outputCompilation) = RunGenerator(source);

        var diagnostics = result.Diagnostics
            .Where(x => x.Id == "ENUMCLGEN013")
            .ToArray();

        diagnostics.Should().HaveCount(2);
        diagnostics.Should().OnlyContain(x =>
            x.Severity == DiagnosticSeverity.Warning
            && x.GetMessage() == "Index specified in EnumClassValueAttribute is ignored when IndexAssignmentMode is set to Runtime");
        diagnostics.Select(x => x.Location.SourceTree!.GetText().ToString(x.Location.SourceSpan))
            .Should().BeEquivalentTo(
                "EnumClassValue(100)",
                "global::EnumClasses.EnumClassValue(200)");

        GetErrors(outputCompilation).Should().BeEmpty();

        var generatedSource = GetGeneratedSource(result, "Consumer.RuntimeIndexedEnum.g.cs");
        generatedSource.Should().Contain("public required int EnumIndex");
        generatedSource.Should().NotContain("First.EnumIndex = 100;");
        generatedSource.Should().NotContain("Second.EnumIndex = 200;");
    }

    [Fact]
    public void When_AttributeDoesNotProvideRedundantRuntimeIndex_Then_ShouldNotReportWarning()
    {
        const string source = """
            using EnumClasses;

            namespace Consumer;

            [EnumClass(
                GenerateJsonConverter = false,
                IndexAssignmentMode = IndexAssignmentMode.Runtime)]
            internal partial class RuntimeIndexedEnum
            {
                [EnumClassValue]
                public static RuntimeIndexedEnum First { get; } = new() { EnumIndex = 10 };
            }

            [EnumClass(GenerateJsonConverter = false)]
            internal partial class CompileTimeIndexedEnum
            {
                [EnumClassValue(100)]
                public static CompileTimeIndexedEnum First { get; } = new();
            }
            """;

        var (result, outputCompilation) = RunGenerator(source);

        result.Diagnostics.Should().NotContain(x => x.Id == "ENUMCLGEN013");
        GetErrors(outputCompilation).Should().BeEmpty();

        GetGeneratedSource(result, "Consumer.CompileTimeIndexedEnum.g.cs")
            .Should().Contain("First.EnumIndex = 100;");
    }

    [Fact]
    public void When_RuntimeIndexDictionaryIsForced_Then_ShouldValidateAndCollectInOneDictionary()
    {
        const string source = """
            using EnumClasses;

            namespace Consumer;

            [EnumClass(
                GenerateJsonConverter = false,
                IndexAssignmentMode = IndexAssignmentMode.Runtime,
                LookupMode = LookupMode.ForceDictionary)]
            internal partial class DictionaryRuntimeIndexedEnum
            {
                public static DictionaryRuntimeIndexedEnum Ten { get; } = new() { EnumIndex = 10 };
                public static DictionaryRuntimeIndexedEnum Twenty { get; } = new() { EnumIndex = 20 };
            }
            """;

        var (result, outputCompilation) = RunGenerator(source);

        GetErrors(outputCompilation).Should().BeEmpty();
        var generatedSource = GetGeneratedSource(result, "Consumer.DictionaryRuntimeIndexedEnum.g.cs");
        generatedSource.Should().Contain("_valuesByEnumIndex = CreateRuntimeIndexDictionary();");
        generatedSource.Should().Contain(
            "new System.Collections.Generic.Dictionary<int, DictionaryRuntimeIndexedEnum>(_allValues.Length)");
        generatedSource.Should().Contain("valuesByEnumIndex.TryAdd(value._enumIndex, value)");
        generatedSource.Should().Contain(
            "System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary(valuesByEnumIndex)");
        generatedSource.Should().NotContain("new System.Collections.Generic.HashSet<int>()");
        generatedSource.Should().NotContain("ValidateRuntimeIndexes();");
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
