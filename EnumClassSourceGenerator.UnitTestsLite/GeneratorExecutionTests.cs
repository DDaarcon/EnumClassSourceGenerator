using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace EnumClassSourceGenerator.UnitTestsLite;

public class GeneratorExecutionTests
{
    [Fact]
    public void Execute()
    {
        const string source = """
            using EnumClasses;

            namespace Consumer;

            [EnumClass(ConstructionRestrictionMode = ConstructionRestrictionMode.WithProtectedDefaultConstructor, GenerateJsonConverter = false)]
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
