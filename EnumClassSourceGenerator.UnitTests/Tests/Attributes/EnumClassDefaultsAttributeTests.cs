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
                RequireIndexAssignmentInInitializer = false)]

            namespace Consumer;

            [EnumClass]
            internal partial class BasicEnum
            {
                public static BasicEnum One { get; } = new();
            }

            [NumberedEnumClass]
            internal partial class NumberedEnum
            {
                public static NumberedEnum Ten { get; } = new() { EnumIndex = 10 };
            }
            """;

        var (_, result, outputCompilation) = RunGenerator(source);

        result.Diagnostics.Should().NotContain(x => x.Severity == DiagnosticSeverity.Error);
        GetErrors(outputCompilation).Should().BeEmpty();
        result.GeneratedSources.Select(x => x.HintName).Should().BeEquivalentTo(
            "Consumer.BasicEnum.g.cs",
            "Consumer.NumberedEnum.g.cs");

        var basicSource = GetGeneratedSource(result, "Consumer.BasicEnum.g.cs");
        basicSource.Should().Contain("private BasicEnum() { }");
        basicSource.Should().Contain("public enum Raw");
        basicSource.Should().Contain("FrozenDictionary.ToFrozenDictionary");

        var numberedSource = GetGeneratedSource(result, "Consumer.NumberedEnum.g.cs");
        numberedSource.Should().Contain("private NumberedEnum() { }");
        numberedSource.Should().Contain("public enum Raw");
        numberedSource.Should().Contain("FrozenDictionary.ToFrozenDictionary");
        numberedSource.Should().Contain("public int EnumIndex");
        numberedSource.Should().NotContain("public required int EnumIndex");
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
                RequireIndexAssignmentInInitializer = false)]

            namespace Consumer;

            [NumberedEnumClass(
                GenerateJsonConverter = true,
                GenerateRawEnum = false,
                LookupMode = LookupMode.ForceDictionary,
                ConstructionRestrictionMode = ConstructionRestrictionMode.Off,
                RequireIndexAssignmentInInitializer = true)]
            internal partial class LocallyConfiguredEnum
            {
                public static LocallyConfiguredEnum One { get; } = new() { EnumIndex = 1 };
            }
            """;

        var (_, result, _) = RunGenerator(source);

        result.Diagnostics.Should().NotContain(x => x.Severity == DiagnosticSeverity.Error);
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
        var referencePaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Append(typeof(global::EnumClasses.EnumClassAttribute).Assembly.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return CSharpCompilation.Create(
            assemblyName: "EnumClassDefaultsAttributeTests",
            syntaxTrees: [syntaxTree],
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
