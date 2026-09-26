using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace EnumClassSourceGenerator.UnitTests.Tests;

public class GeneratedSourceFileNameTests
{
    [Fact]
    public void When_GeneratingEnumClass_Then_ShouldUseFullyQualifiedNameForGeneratedFiles()
    {
        const string source = """
            using EnumClasses;

            namespace Consumer.Payments;

            [EnumClass]
            internal partial class PaymentMethod
            {
                public static PaymentMethod Card { get; } = new();
            }
            """;

        var result = RunGenerator(source);

        result.GeneratedSources
            .Select(x => x.HintName)
            .Should()
            .BeEquivalentTo(
                "Consumer.Payments.PaymentMethod.g.cs",
                "Consumer.Payments.PaymentMethodJsonConverter.g.cs");
    }

    [Fact]
    public void When_EnumClassesHaveSameNameInDifferentNamespaces_Then_ShouldGenerateDistinctFileNames()
    {
        const string source = """
            using EnumClasses;

            namespace Consumer.Payments
            {
                [EnumClass]
                internal partial class Status
                {
                    public static Status Active { get; } = new();
                }
            }

            namespace Consumer.Orders
            {
                [EnumClass]
                internal partial class Status
                {
                    public static Status Pending { get; } = new();
                }
            }
            """;

        var result = RunGenerator(source);
        var hintNames = result.GeneratedSources.Select(x => x.HintName).ToArray();

        hintNames.Should().OnlyHaveUniqueItems();
        hintNames.Should().BeEquivalentTo(
            "Consumer.Payments.Status.g.cs",
            "Consumer.Payments.StatusJsonConverter.g.cs",
            "Consumer.Orders.Status.g.cs",
            "Consumer.Orders.StatusJsonConverter.g.cs");
        result.Diagnostics.Should().NotContain(x => x.Id == "ENUMCLGEN005");
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
            assemblyName: "GeneratedSourceFileNameTests",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new global::EnumClasses.SourceGenerators.EnumClassSourceGenerator().AsSourceGenerator());

        driver = driver.RunGenerators(compilation);

        return driver.GetRunResult().Results.Single();
    }
}
