using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace EnumClassSourceGenerator.UnitTests.Tests;

public class EnumClassTypeDeclarationTests
{
    [Fact]
    public void When_EnumClassIsARecord_Then_ShouldGenerateEnumClassMembers()
    {
        const string source = """
            using EnumClasses;

            namespace Consumer;

            [EnumClass(ConstructionRestrictionMode = ConstructionRestrictionMode.Off)]
            internal partial record RecordEnum
            {
                public static readonly RecordEnum One = new();
            }
            """;

        var (result, outputCompilation) = RunGenerator(source);

        result.Diagnostics.Should().NotContain(x => x.Severity == DiagnosticSeverity.Error);
        outputCompilation.GetDiagnostics()
            .Where(x => x.Severity == DiagnosticSeverity.Error)
            .Should()
            .BeEmpty();

        var enumClass = outputCompilation.GetTypeByMetadataName("Consumer.RecordEnum");
        enumClass.Should().NotBeNull();
        enumClass!.IsRecord.Should().BeTrue();
        enumClass.GetMembers("AllValues").Should().ContainSingle();
    }

    [Theory]
    [InlineData("struct")]
    [InlineData("record struct")]
    public void When_EnumClassIsAValueType_Then_ShouldRejectIt(string typeKeyword)
    {
        var source = $$"""
            using EnumClasses;

            namespace Consumer;

            [EnumClass]
            internal partial {{typeKeyword}} ValueEnum
            {
            }
            """;

        var (result, outputCompilation) = RunGenerator(source);

        result.GeneratedSources.Should().BeEmpty();
        outputCompilation.GetDiagnostics().Should().Contain(x =>
            x.Id == "CS0592"
            && x.Severity == DiagnosticSeverity.Error);
    }

    [Theory]
    [InlineData(1, "Consumer.Container+NestedEnum")]
    [InlineData(2, "Consumer.Outer+Inner+NestedEnum")]
    public void When_EnumClassIsNestedInPartialClasses_Then_ShouldGenerateEnumClassMembers(
        int nestingLevel,
        string metadataName)
    {
        var source = nestingLevel switch
        {
            1 => """
                using EnumClasses;

                namespace Consumer;

                internal partial class Container
                {
                    [EnumClass]
                    internal partial class NestedEnum
                    {
                        public static readonly NestedEnum One = new();
                    }
                }
                """,
            2 => """
                using EnumClasses;

                namespace Consumer;

                internal partial class Outer
                {
                    internal partial class Inner
                    {
                        [EnumClass]
                        internal partial class NestedEnum
                        {
                            public static readonly NestedEnum One = new();
                        }
                    }
                }
                """,
            _ => throw new ArgumentOutOfRangeException(nameof(nestingLevel))
        };

        var (result, outputCompilation) = RunGenerator(source);

        result.Diagnostics.Should().NotContain(x => x.Severity == DiagnosticSeverity.Error);
        outputCompilation.GetDiagnostics()
            .Where(x => x.Severity == DiagnosticSeverity.Error)
            .Should()
            .BeEmpty();

        var enumClass = outputCompilation.GetTypeByMetadataName(metadataName);
        enumClass.Should().NotBeNull();
        enumClass!.GetMembers("AllValues").Should().ContainSingle();
    }

    [Theory]
    [InlineData(1, "GenericContainer")]
    [InlineData(2, "GenericOuter")]
    public void When_EnumClassIsContainedInAGenericClass_Then_ShouldReportDiagnosticAndSkipGeneration(
        int nestingLevel,
        string genericContainerName)
    {
        var source = nestingLevel switch
        {
            1 => """
                using EnumClasses;

                namespace Consumer;

                internal partial class GenericContainer<T>
                {
                    [EnumClass(GenerateJsonConverter = false)]
                    internal partial class NestedEnum
                    {
                    }
                }
                """,
            2 => """
                using EnumClasses;

                namespace Consumer;

                internal partial class GenericOuter<T>
                {
                    internal partial class Inner
                    {
                        [EnumClass(GenerateJsonConverter = false)]
                        internal partial class NestedEnum
                        {
                        }
                    }
                }
                """,
            _ => throw new ArgumentOutOfRangeException(nameof(nestingLevel))
        };

        var (result, _) = RunGenerator(source);

        var diagnostic = result.Diagnostics.Should()
            .ContainSingle(x => x.Id == "ENUMCLGEN023")
            .Subject;

        diagnostic.Severity.Should().Be(DiagnosticSeverity.Error);
        diagnostic.GetMessage().Should().Contain($"'{genericContainerName}'");
        diagnostic.GetMessage().Should().Contain("'NestedEnum'");
        diagnostic.Location.SourceSpan.Should().Be(
            new TextSpan(
                source.IndexOf(genericContainerName, StringComparison.Ordinal),
                genericContainerName.Length));
        result.GeneratedSources.Should().BeEmpty();
    }

    [Fact]
    public void When_NestedEnumClassesHaveTheSameName_Then_ShouldGenerateDistinctSources()
    {
        const string source = """
            using EnumClasses;

            namespace Consumer;

            internal partial class FirstContainer
            {
                [EnumClass(GenerateJsonConverter = false)]
                internal partial class Status
                {
                    public static readonly Status One = new();
                }
            }

            internal partial class SecondContainer
            {
                [EnumClass(GenerateJsonConverter = false)]
                internal partial class Status
                {
                    public static readonly Status One = new();
                }
            }
            """;

        var (result, outputCompilation) = RunGenerator(source);

        result.Diagnostics.Should().NotContain(x => x.Severity == DiagnosticSeverity.Error);
        outputCompilation.GetDiagnostics()
            .Where(x => x.Severity == DiagnosticSeverity.Error)
            .Should()
            .BeEmpty();
        result.GeneratedSources.Should().Contain(x =>
            x.HintName == "Consumer.FirstContainer.Status.g.cs");
        result.GeneratedSources.Should().Contain(x =>
            x.HintName == "Consumer.SecondContainer.Status.g.cs");
    }

    private static (GeneratorRunResult Result, Compilation OutputCompilation) RunGenerator(string source)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions, path: "Source.cs");
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create(
            assemblyName: "EnumClassTypeDeclarationTests",
            syntaxTrees: [syntaxTree],
            references: references,
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
}
