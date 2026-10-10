using FluentAssertions;
using System.Text.Json;

namespace EnumClassSourceGenerator.UnitTests.Tests;

public class ExplicitSerializationNameTests
{
    public static TheoryData<ExplicitSerializationNameEnum, string> ExplicitNames => new()
    {
        { ExplicitSerializationNameEnum.FromProperty, "property-name" },
        { ExplicitSerializationNameEnum.FromConstructor, "constructor-name" },
        { ExplicitSerializationNameEnum.FromNamedConstructor, "named-constructor-name" },
        { ExplicitSerializationNameEnum.WithEscapedCharacters, "quote\" slash\\ line\n" }
    };

    [Theory]
    [MemberData(nameof(ExplicitNames))]
    public void When_ValueHasExplicitSerializedName_Then_MethodsShouldRoundTrip(
        ExplicitSerializationNameEnum value,
        string serializedName)
    {
        value.Serialize().Should().Be(serializedName);
        value.ToString().Should().Be(serializedName);
        ExplicitSerializationNameEnum.Serialize(value).Should().Be(serializedName);
        ExplicitSerializationNameEnum.Deserialize(serializedName).Should().BeSameAs(value);
        ExplicitSerializationNameEnum.Deserialize(serializedName.AsSpan()).Should().BeSameAs(value);
        ExplicitSerializationNameEnum.TryDeserialize(serializedName, out var fromString).Should().BeTrue();
        fromString.Should().BeSameAs(value);
        ExplicitSerializationNameEnum.TryDeserialize(serializedName.AsSpan(), out var fromSpan).Should().BeTrue();
        fromSpan.Should().BeSameAs(value);
    }

    [Theory]
    [MemberData(nameof(ExplicitNames))]
    public void When_ValueHasExplicitSerializedName_Then_JsonShouldRoundTrip(
        ExplicitSerializationNameEnum value,
        string serializedName)
    {
        var json = JsonSerializer.Serialize(value);

        json.Should().Be(JsonSerializer.Serialize(serializedName));
        JsonSerializer.Deserialize<ExplicitSerializationNameEnum>(json).Should().BeSameAs(value);
    }

    [Fact]
    public void When_ValueHasNoExplicitSerializedName_Then_ShouldUseMemberName()
    {
        ExplicitSerializationNameEnum.DefaultName.Serialize()
            .Should().Be(nameof(ExplicitSerializationNameEnum.DefaultName));
        ExplicitSerializationNameEnum.Deserialize(nameof(ExplicitSerializationNameEnum.DefaultName))
            .Should().BeSameAs(ExplicitSerializationNameEnum.DefaultName);
    }

    [Fact]
    public void When_ValueHasExplicitSerializedName_Then_MemberNameShouldNotDeserialize()
    {
        ExplicitSerializationNameEnum.Deserialize(nameof(ExplicitSerializationNameEnum.FromProperty))
            .Should().BeNull();
    }

    [Fact]
    public void When_CombinedConstructorFormsAreUsed_Then_IndexesShouldStillBeCollected()
    {
        ExplicitSerializationNameEnum.FromProperty.EnumIndex.Should().Be(0);
        ExplicitSerializationNameEnum.FromConstructor.EnumIndex.Should().Be(7);
        ExplicitSerializationNameEnum.FromNamedConstructor.EnumIndex.Should().Be(9);
        ExplicitSerializationNameEnum.WithEscapedCharacters.EnumIndex.Should().Be(10);
        ExplicitSerializationNameEnum.DefaultName.EnumIndex.Should().Be(11);
    }

    [Fact]
    public void When_DictionaryLookupIsUsed_Then_ExplicitNameShouldRoundTripThroughMethodsAndJson()
    {
        const string serializedName = "dictionary-name";
        var value = DictionaryExplicitSerializationNameEnum.Value;

        value.Serialize().Should().Be(serializedName);
        DictionaryExplicitSerializationNameEnum.Deserialize(serializedName).Should().BeSameAs(value);
        DictionaryExplicitSerializationNameEnum.Deserialize(serializedName.AsSpan()).Should().BeSameAs(value);

        var json = JsonSerializer.Serialize(value);
        json.Should().Be(JsonSerializer.Serialize(serializedName));
        JsonSerializer.Deserialize<DictionaryExplicitSerializationNameEnum>(json).Should().BeSameAs(value);
    }
}
