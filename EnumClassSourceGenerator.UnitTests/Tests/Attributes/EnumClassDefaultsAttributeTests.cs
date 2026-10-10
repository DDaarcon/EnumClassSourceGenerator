using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace EnumClassSourceGenerator.UnitTests.Tests;

public class EnumClassDefaultsAttributeTests
{
    [Fact]
    public void When_AssemblyDefaultsAreSpecified_Then_ShouldApplyThemToEveryEnumClass()
    {
        const string source = """
            using EnumClasses;

            [assembly: EnumClassDefaults(
                GenerateJsonConverter = false,
                GenerateRawEnum = true,
                LookupMode = LookupMode.ForceDictionary,
                ConstructionRestrictionMode = ConstructionRestrictionMode.WithPrivateDefaultConstructor,
                IndexAssignmentMode = IndexAssignmentMode.RuntimeAtValueInit)]

            namespace Consumer;

            [EnumClass]
            internal partial class BasicEnum
            {
                public static BasicEnum One { get; } = new() { EnumIndex = 1 };
            }

            [EnumClass]
            internal partial class RuntimeIndexedEnum
            {
                public static RuntimeIndexedEnum Ten { get; } = new() { EnumIndex = 10 };
            }
            """;

        var (_, result, outputCompilation) = RunGenerator(source);

        result.Diagnostics.Should().NotContain(x => x.Severity == DiagnosticSeverity.Error);
        var rawIndexMismatchWarnings = result.Diagnostics
            .Where(x => x.Id == "ENUMCLGEN015")
            .ToArray();
        rawIndexMismatchWarnings.Should().HaveCount(2);
        rawIndexMismatchWarnings.Should().OnlyContain(x =>
            x.Severity == DiagnosticSeverity.Warning
            && x.GetMessage() == "Runtime EnumIndex values may not match the numeric values of the generated Raw enum, whose values follow declaration order");
        rawIndexMismatchWarnings.Select(x => x.Location.SourceTree!.GetText().ToString(x.Location.SourceSpan))
            .Should().OnlyContain(attributeText => attributeText == "EnumClass");
        GetErrors(outputCompilation).Should().BeEmpty();
        result.GeneratedSources.Select(x => x.HintName).Should().BeEquivalentTo(
            "Consumer.BasicEnum.g.cs",
            "Consumer.RuntimeIndexedEnum.g.cs");

        var basicSource = GetGeneratedSource(result, "Consumer.BasicEnum.g.cs");
        basicSource.Should().Contain("private BasicEnum() { }");
        basicSource.Should().Contain("public enum Raw");
        basicSource.Should().Contain("FrozenDictionary.ToFrozenDictionary");
        basicSource.Should().Contain("public required int EnumIndex");

        var runtimeIndexedSource = GetGeneratedSource(result, "Consumer.RuntimeIndexedEnum.g.cs");
        runtimeIndexedSource.Should().Contain("private RuntimeIndexedEnum() { }");
        runtimeIndexedSource.Should().Contain("public enum Raw");
        runtimeIndexedSource.Should().Contain("FrozenDictionary.ToFrozenDictionary");
        runtimeIndexedSource.Should().Contain("public required int EnumIndex");
    }

    [Fact]
    public void When_EnumClassOverridesAssemblyDefaults_Then_ShouldUseEnumClassValues()
    {
        const string source = """
            using EnumClasses;

            [assembly: EnumClassDefaults(
                GenerateJsonConverter = false,
                GenerateRawEnum = true,
                LookupMode = LookupMode.ForceIfChain,
                ConstructionRestrictionMode = ConstructionRestrictionMode.WithPrivateDefaultConstructor,
                IndexAssignmentMode = IndexAssignmentMode.CompileTime)]

            namespace Consumer;

            [EnumClass(
                GenerateJsonConverter = true,
                GenerateRawEnum = false,
                LookupMode = LookupMode.ForceDictionary,
                ConstructionRestrictionMode = ConstructionRestrictionMode.Off,
                IndexAssignmentMode = IndexAssignmentMode.RuntimeAtValueInit)]
            internal partial class LocallyConfiguredEnum
            {
                public static LocallyConfiguredEnum One { get; } = new() { EnumIndex = 1 };
            }
            """;

        var (_, result, _) = RunGenerator(source);

        result.Diagnostics.Should().NotContain(x => x.Severity == DiagnosticSeverity.Error);
        result.Diagnostics.Should().NotContain(x => x.Id == "ENUMCLGEN015");
        result.GeneratedSources.Select(x => x.HintName).Should().BeEquivalentTo(
            "Consumer.LocallyConfiguredEnum.g.cs",
            "Consumer.LocallyConfiguredEnumJsonConverter.g.cs");

        var generatedSource = GetGeneratedSource(result, "Consumer.LocallyConfiguredEnum.g.cs");
        generatedSource.Should().NotContain("LocallyConfiguredEnum() { }");
        generatedSource.Should().Contain("public required int EnumIndex");
        generatedSource.Should().NotContain("public enum Raw");
        generatedSource.Should().Contain("FrozenDictionary.ToFrozenDictionary");
    }

    [Fact]
    public void When_RuntimeIndexesAndRawEnumAreEnabledLocally_Then_ShouldWarnOnEnumClassAttribute()
    {
        const string source = """
            using EnumClasses;

            namespace Consumer;

            [EnumClass(
                GenerateJsonConverter = false,
                GenerateRawEnum = true,
                IndexAssignmentMode = IndexAssignmentMode.RuntimeAtValueInit)]
            internal partial class RuntimeRawEnum
            {
                public static RuntimeRawEnum Ten { get; } = new() { EnumIndex = 10 };
            }
            """;

        var (_, result, outputCompilation) = RunGenerator(source);

        var warning = result.Diagnostics.Should()
            .ContainSingle(x => x.Id == "ENUMCLGEN015")
            .Which;
        warning.Severity.Should().Be(DiagnosticSeverity.Warning);
        warning.GetMessage().Should().Be(
            "Runtime EnumIndex values may not match the numeric values of the generated Raw enum, whose values follow declaration order");
        warning.Location.SourceTree!.GetText().ToString(warning.Location.SourceSpan)
            .Should().StartWith("EnumClass(");
        GetErrors(outputCompilation).Should().BeEmpty();
    }

    [Fact]
    public void When_EnumClassUsesUnsupportedIndexAssignmentMode_Then_ShouldReportErrorAtConfiguredValue()
    {
        const string source = """
            using EnumClasses;

            namespace Consumer;

            [EnumClass(
                GenerateJsonConverter = false,
                IndexAssignmentMode = (IndexAssignmentMode)99)]
            internal partial class InvalidModeEnum
            {
                public static InvalidModeEnum One { get; } = new();
            }
            """;

        var (_, result, _) = RunGenerator(source);

        var diagnostic = result.Diagnostics.Should()
            .ContainSingle(x => x.Id == "ENUMCLGEN016")
            .Which;
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Error);
        diagnostic.GetMessage().Should().Be(
            "IndexAssignmentMode value '99' is not supported; use CompileTime, RuntimeAtValueInit, or RuntimeAtValueCreation");
        diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan)
            .Should().Be("(IndexAssignmentMode)99");
        result.GeneratedSources.Should().BeEmpty();
    }

    [Fact]
    public void When_AssemblyDefaultUsesUnsupportedIndexAssignmentMode_Then_ShouldReportErrorAndUseBuiltInDefault()
    {
        const string source = """
            using EnumClasses;

            [assembly: EnumClassDefaults(IndexAssignmentMode = (IndexAssignmentMode)99)]

            namespace Consumer;

            [EnumClass(GenerateJsonConverter = false)]
            internal partial class InvalidDefaultModeEnum
            {
                public static InvalidDefaultModeEnum One { get; } = new();
            }
            """;

        var (_, result, outputCompilation) = RunGenerator(source);

        var diagnostic = result.Diagnostics.Should()
            .ContainSingle(x => x.Id == "ENUMCLGEN016")
            .Which;
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Error);
        diagnostic.GetMessage().Should().Be(
            "IndexAssignmentMode value '99' is not supported; use CompileTime, RuntimeAtValueInit, or RuntimeAtValueCreation");
        diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan)
            .Should().Be("(IndexAssignmentMode)99");

        GetErrors(outputCompilation).Should().BeEmpty();
        GetGeneratedSource(result, "Consumer.InvalidDefaultModeEnum.g.cs")
            .Should().Contain("One.EnumIndex = 0;");
    }

    [Fact]
    public void When_AssemblyDefaultsChange_Then_ShouldRegenerateEveryEnumClass()
    {
        const string initialSource = """
            using EnumClasses;

            [assembly: EnumClassDefaults(GenerateJsonConverter = false, GenerateRawEnum = false)]

            namespace Consumer;

            [EnumClass]
            internal partial class FirstEnum
            {
                public static FirstEnum One { get; } = new();
            }

            [EnumClass]
            internal partial class SecondEnum
            {
                public static SecondEnum One { get; } = new();
            }
            """;

        const string updatedSource = """
            using EnumClasses;

            [assembly: EnumClassDefaults(GenerateJsonConverter = false, GenerateRawEnum = true)]

            namespace Consumer;

            [EnumClass]
            internal partial class FirstEnum
            {
                public static FirstEnum One { get; } = new();
            }

            [EnumClass]
            internal partial class SecondEnum
            {
                public static SecondEnum One { get; } = new();
            }
            """;

        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var initialCompilation = CreateCompilation(initialSource, parseOptions);
        var driver = CreateDriver(parseOptions);

        driver = driver.RunGeneratorsAndUpdateCompilation(
            initialCompilation,
            out var initialOutputCompilation,
            out _);

        var initialResult = driver.GetRunResult().Results.Single();
        GetErrors(initialOutputCompilation).Should().BeEmpty();
        GetGeneratedSource(initialResult, "Consumer.FirstEnum.g.cs").Should().NotContain("public enum Raw");
        GetGeneratedSource(initialResult, "Consumer.SecondEnum.g.cs").Should().NotContain("public enum Raw");

        var updatedSyntaxTree = CSharpSyntaxTree.ParseText(updatedSource, parseOptions);
        var updatedCompilation = initialCompilation.ReplaceSyntaxTree(
            initialCompilation.SyntaxTrees.Single(),
            updatedSyntaxTree);

        driver = driver.RunGeneratorsAndUpdateCompilation(
            updatedCompilation,
            out var updatedOutputCompilation,
            out _);

        var updatedResult = driver.GetRunResult().Results.Single();
        GetErrors(updatedOutputCompilation).Should().BeEmpty();
        GetGeneratedSource(updatedResult, "Consumer.FirstEnum.g.cs").Should().Contain("public enum Raw");
        GetGeneratedSource(updatedResult, "Consumer.SecondEnum.g.cs").Should().Contain("public enum Raw");
    }

    [Fact]
    public void When_IndexAssignmentDefaultChangesWithoutChangingEnumDeclaration_Then_ShouldResolveIndexesAgain()
    {
        const string enumSource = """
            using EnumClasses;
            using System.Diagnostics.CodeAnalysis;

            namespace Consumer;

            [EnumClass(GenerateJsonConverter = false)]
            internal partial class ReconfiguredEnum
            {
                [SetsRequiredMembers]
                private ReconfiguredEnum()
                {
                }

                [EnumClassValue(10)]
                public static ReconfiguredEnum Ten { get; } = new();

                public static ReconfiguredEnum Eleven { get; } = new();
            }
            """;

        const string runtimeDefaultsSource = """
            using EnumClasses;

            [assembly: EnumClassDefaults(IndexAssignmentMode = IndexAssignmentMode.RuntimeAtValueInit)]
            """;

        const string compileTimeDefaultsSource = """
            using EnumClasses;

            [assembly: EnumClassDefaults(IndexAssignmentMode = IndexAssignmentMode.CompileTime)]
            """;

        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var enumSyntaxTree = CSharpSyntaxTree.ParseText(enumSource, parseOptions, path: "ReconfiguredEnum.cs");
        var runtimeDefaultsSyntaxTree = CSharpSyntaxTree.ParseText(runtimeDefaultsSource, parseOptions, path: "Defaults.cs");
        var initialCompilation = CreateCompilation([enumSyntaxTree, runtimeDefaultsSyntaxTree]);
        var driver = CreateDriver(parseOptions);

        driver = driver.RunGeneratorsAndUpdateCompilation(
            initialCompilation,
            out var runtimeOutputCompilation,
            out _);

        var runtimeResult = driver.GetRunResult().Results.Single();
        GetErrors(runtimeOutputCompilation).Should().BeEmpty();
        var runtimeGeneratedSource = GetGeneratedSource(runtimeResult, "Consumer.ReconfiguredEnum.g.cs");
        runtimeGeneratedSource.Should().Contain("public required int EnumIndex");
        runtimeGeneratedSource.Should().NotContain("Ten.EnumIndex = 10;");

        var compileTimeDefaultsSyntaxTree = CSharpSyntaxTree.ParseText(
            compileTimeDefaultsSource,
            parseOptions,
            path: "Defaults.cs");
        var updatedCompilation = initialCompilation.ReplaceSyntaxTree(
            runtimeDefaultsSyntaxTree,
            compileTimeDefaultsSyntaxTree);

        driver = driver.RunGeneratorsAndUpdateCompilation(
            updatedCompilation,
            out var compileTimeOutputCompilation,
            out _);

        var compileTimeResult = driver.GetRunResult().Results.Single();
        GetErrors(compileTimeOutputCompilation).Should().BeEmpty();
        var compileTimeGeneratedSource = GetGeneratedSource(compileTimeResult, "Consumer.ReconfiguredEnum.g.cs");
        compileTimeGeneratedSource.Should().Contain("public int EnumIndex { get; private set; }");
        compileTimeGeneratedSource.Should().Contain("Ten.EnumIndex = 10;");
        compileTimeGeneratedSource.Should().Contain("Eleven.EnumIndex = 11;");
    }

    [Fact]
    public void When_AssemblyDisablesConstructionRestrictions_Then_ShouldAllowPublicConstructor()
    {
        const string source = """
            using EnumClasses;

            [assembly: EnumClassDefaults(
                GenerateJsonConverter = false,
                ConstructionRestrictionMode = ConstructionRestrictionMode.Off)]

            namespace Consumer;

            [EnumClass]
            internal partial class OpenEnum
            {
                public OpenEnum()
                {
                }

                public static OpenEnum One { get; } = new();
            }
            """;

        var (_, result, outputCompilation) = RunGenerator(source);

        result.Diagnostics.Should().NotContain(x => x.Id == "ENUMCLGEN009");
        GetErrors(outputCompilation).Should().BeEmpty();
        GetGeneratedSource(result, "Consumer.OpenEnum.g.cs")
            .Should().NotContain("protected OpenEnum() { }");
    }

    private static (GeneratorDriver Driver, GeneratorRunResult Result, Compilation OutputCompilation) RunGenerator(
        string source)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var compilation = CreateCompilation(source, parseOptions);
        var driver = CreateDriver(parseOptions);

        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out _,
            out _);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out _);

        return (driver, driver.GetRunResult().Results.Single(), outputCompilation);
    }

    private static CSharpCompilation CreateCompilation(string source, CSharpParseOptions parseOptions)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        return CreateCompilation([syntaxTree]);
    }

    private static CSharpCompilation CreateCompilation(IEnumerable<SyntaxTree> syntaxTrees)
    {
        var referencePaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Append(typeof(global::EnumClasses.EnumClassAttribute).Assembly.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return CSharpCompilation.Create(
            assemblyName: "EnumClassDefaultsAttributeTests",
            syntaxTrees: syntaxTrees,
            references: referencePaths.Select(path => MetadataReference.CreateFromFile(path)),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static GeneratorDriver CreateDriver(CSharpParseOptions parseOptions)
        => CSharpGeneratorDriver.Create(
            generators: [new global::EnumClasses.SourceGenerators.EnumClassSourceGenerator().AsSourceGenerator()],
            parseOptions: parseOptions);

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
