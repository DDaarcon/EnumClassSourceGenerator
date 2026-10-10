using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnumClasses;
using FluentAssertions;

[assembly: EnumClassDefaults(
    GenerateJsonConverter = false,
    GenerateRawEnum = true,
    LookupMode = LookupMode.ForceIfChain,
    ConstructionRestrictionMode = ConstructionRestrictionMode.WithPrivateDefaultConstructor,
    IndexAssignmentMode = IndexAssignmentMode.CompileTime,
    DocumentationMode = DocumentationMode.Disabled)]

namespace EnumClasses.CompatibilityTesting.Net8;

[EnumClass]
internal partial class DefaultConfiguredStatus
{
    [EnumClassValue(10, "ready")]
    public static readonly DefaultConfiguredStatus Ready = new();

    public static DefaultConfiguredStatus Running { get; } = new();

    [EnumClassIgnore]
    public static readonly DefaultConfiguredStatus Ignored = new();
}

[EnumClass(
    GenerateJsonConverter = true,
    GenerateRawEnum = false,
    LookupMode = LookupMode.ForceDictionary,
    ConstructionRestrictionMode = ConstructionRestrictionMode.Off,
    IndexAssignmentMode = IndexAssignmentMode.RuntimeAtValueInit,
    DocumentationMode = DocumentationMode.ListCompileTimeIndexes)]
internal partial class LocallyConfiguredStatus
{
    public LocallyConfiguredStatus()
    {
    }

    public static readonly LocallyConfiguredStatus Started = new()
    {
        EnumIndex = -5
    };

    [EnumClassValue(SerializedName = "completed-successfully")]
    public static readonly LocallyConfiguredStatus Completed = new()
    {
        EnumIndex = 42
    };
}

public class CompatibilityTests
{
    [Fact]
    public void AssemblyDefaults_GenerateExpectedNet8Api()
    {
        DefaultConfiguredStatus.AllValues.Should().Equal(
            DefaultConfiguredStatus.Ready,
            DefaultConfiguredStatus.Running);
        DefaultConfiguredStatus.Ready.EnumIndex.Should().Be(10);
        DefaultConfiguredStatus.Running.EnumIndex.Should().Be(11);
        DefaultConfiguredStatus.Deserialize("ready").Should().BeSameAs(DefaultConfiguredStatus.Ready);
        DefaultConfiguredStatus.GetByEnumIndex(11).Should().BeSameAs(DefaultConfiguredStatus.Running);

        DefaultConfiguredStatus.Ready.ToRaw().Should().Be(DefaultConfiguredStatus.Raw.Ready);
        DefaultConfiguredStatus.FromRaw(DefaultConfiguredStatus.Raw.Running)
            .Should().BeSameAs(DefaultConfiguredStatus.Running);

        typeof(DefaultConfiguredStatus).GetCustomAttribute<JsonConverterAttribute>().Should().BeNull();
        var generatedConstructor = typeof(DefaultConfiguredStatus).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            Type.EmptyTypes,
            modifiers: null);
        generatedConstructor.Should().NotBeNull();
        generatedConstructor!.IsPrivate.Should().BeTrue();
    }

    [Fact]
    public void LocalOptions_OverrideEveryAssemblyDefault()
    {
        LocallyConfiguredStatus.GetByEnumIndex(-5).Should().BeSameAs(LocallyConfiguredStatus.Started);
        LocallyConfiguredStatus.GetByEnumIndex(42).Should().BeSameAs(LocallyConfiguredStatus.Completed);
        LocallyConfiguredStatus.Deserialize("completed-successfully")
            .Should().BeSameAs(LocallyConfiguredStatus.Completed);

        var json = JsonSerializer.Serialize(LocallyConfiguredStatus.Completed);
        json.Should().Be("\"completed-successfully\"");
        JsonSerializer.Deserialize<LocallyConfiguredStatus>(json)
            .Should().BeSameAs(LocallyConfiguredStatus.Completed);

        typeof(LocallyConfiguredStatus).GetNestedType("Raw").Should().BeNull();
        new LocallyConfiguredStatus { EnumIndex = 100 }.Should().NotBeNull();
    }

    [Fact]
    public void GeneratedControlFlow_WorksWithCSharp12Consumer()
    {
        var match = DefaultConfiguredStatus.Running.MatchEx(
            onReady: static () => "ready",
            onRunning: static () => "running");

        match.Should().Be("running");

        var visited = new List<string>();
        LocallyConfiguredStatus.Completed.Switch(
            visited,
            onStarted: static values => values.Add("started"),
            onCompleted: static values => values.Add("completed"));

        visited.Should().Equal("completed");
    }
}
