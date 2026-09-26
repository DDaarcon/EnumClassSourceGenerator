using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace EnumClassSourceGenerator.UnitTests.Tests;

public class ConstructionRestrictionTests
{
    [Fact]
    public void When_UnrestrictedConstructionIsNotSpecified_Then_ShouldNotAllowPublicConstructor()
    {
        const string source = """
            using EnumClasses;

            namespace Consumer;

            [EnumClass(GenerateJsonConverter = false)]
            internal partial class OpenEnum
            {
                public OpenEnum()
                {
                }

                public static OpenEnum One { get; } = new();
            }
            """;

        var (result, outputCompilation) = RunGenerator(source);

        result.Diagnostics.Should().Contain(x => x.Id == "ENUMCLGEN009");
        GetErrors(outputCompilation).Should().BeEmpty();

        var constructor = outputCompilation
            .GetTypeByMetadataName("Consumer.OpenEnum")!
            .InstanceConstructors
            .Should()
            .ContainSingle()
            .Subject;

        constructor.DeclaredAccessibility.Should().Be(Accessibility.Public);
    }

    [Fact]
    public void When_ConstructionIsRestrictedAndNoConstructorIsDeclared_Then_ShouldGenerateProtectedConstructor()
    {
        const string source = """
            using EnumClasses;

            namespace Consumer;

            [EnumClass(UnrestrictedConstruction = false, GenerateJsonConverter = false)]
            internal partial class RestrictedEnum
            {
                public static RestrictedEnum One { get; } = new();
            }
            """;

        var (result, outputCompilation) = RunGenerator(source);

        result.Diagnostics.Should().NotContain(x => x.Severity == DiagnosticSeverity.Error);
        GetErrors(outputCompilation).Should().BeEmpty();

        var constructor = outputCompilation
            .GetTypeByMetadataName("Consumer.RestrictedEnum")!
            .InstanceConstructors
            .Should()
            .ContainSingle()
            .Subject;

        constructor.DeclaredAccessibility.Should().Be(Accessibility.Protected);

        result.GeneratedSources.Single().SourceText.ToString()
            .Should().Contain("protected RestrictedEnum() { }");
    }

    [Theory]
    [InlineData("public")]
    [InlineData("internal")]
    [InlineData("protected internal")]
    [InlineData("private protected")]
    public void When_ConstructionIsRestrictedAndConstructorIsNotPrivateNorProtected_Then_ShouldReportDiagnostic(
        string accessibility)
    {
        var source = $$"""
            using EnumClasses;

            namespace Consumer;

            [EnumClass(UnrestrictedConstruction = false, GenerateJsonConverter = false)]
            internal partial class RestrictedEnum
            {
                {{accessibility}} RestrictedEnum(int value)
                {
                }

                public static RestrictedEnum One { get; } = new(1);
            }
            """;

        var (result, _) = RunGenerator(source);

        var diagnostic = result.Diagnostics.Should()
            .ContainSingle(x => x.Id == "ENUMCLGEN009")
            .Subject;

        diagnostic.Severity.Should().Be(DiagnosticSeverity.Error);
        diagnostic.GetMessage().Should().Contain("'RestrictedEnum'");
        result.GeneratedSources.Should().BeEmpty();
    }

    [Fact]
    public void When_ConstructionIsRestrictedAndPrivateConstructorIsDeclared_Then_ShouldUseUserConstructor()
    {
        const string source = """
            using EnumClasses;

            namespace Consumer;

            [EnumClass(UnrestrictedConstruction = false, GenerateJsonConverter = false)]
            internal partial class RestrictedEnum
            {
                private RestrictedEnum(int value)
                {
                }

                public static RestrictedEnum One { get; } = new(1);
            }
            """;

        var (result, outputCompilation) = RunGenerator(source);

        result.Diagnostics.Should().NotContain(x => x.Severity == DiagnosticSeverity.Error);
        GetErrors(outputCompilation).Should().BeEmpty();

        var constructor = outputCompilation
            .GetTypeByMetadataName("Consumer.RestrictedEnum")!
            .InstanceConstructors
            .Should()
            .ContainSingle()
            .Subject;

        constructor.DeclaredAccessibility.Should().Be(Accessibility.Private);
        constructor.Parameters.Should().ContainSingle();
        result.GeneratedSources.Single().SourceText.ToString()
            .Should().NotContain("protected RestrictedEnum() { }");
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
            assemblyName: "ConstructionRestrictionTests",
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

    private static Diagnostic[] GetErrors(Compilation compilation)
        => compilation.GetDiagnostics()
            .Where(x => x.Severity == DiagnosticSeverity.Error)
            .ToArray();
}
