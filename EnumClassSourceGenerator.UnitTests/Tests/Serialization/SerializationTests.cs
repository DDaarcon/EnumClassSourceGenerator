using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace EnumClassSourceGenerator.UnitTests.Tests;

public class SerializationTests
{
    [Fact]
    public void When_SerializingEnumViaMethod_Then_ShouldGetSerializedValue()
    {
        var serializedOne = BasicEnum.One.Serialize();
        serializedOne.Should().Be(nameof(BasicEnum.One));
    }

    [Fact]
    public void When_DeserializingEnumViaMethod_Then_ShouldProperEnum()
    {
        var deserializedEnum = BasicEnum.Deserialize(nameof(BasicEnum.Two));
        deserializedEnum.Should().Be(BasicEnum.Two);
    }

    [Fact]
    public void When_SerializingEnumViaJson_Then_ShouldGetSerializedValue()
    {
        var serializedOne = JsonSerializer.Serialize(BasicEnum.One);
        serializedOne.Should().Be($"\"{nameof(BasicEnum.One)}\"");
    }

    [Fact]
    public void When_DeserializingEnumViaJson_Then_ShouldGetProperEnum()
    {
        var deserializedEnum = JsonSerializer.Deserialize<BasicEnum>($"\"{nameof(BasicEnum.Two)}\"");
        deserializedEnum.Should().Be(BasicEnum.Two);
    }

    [Fact]
    public void When_UsingDeserializedEnumWithSet_Then_ShouldBehaveNormally()
    {
        var set = new HashSet<BasicEnum>();

        set.Add(BasicEnum.One);

        var deserializedOne = BasicEnum.Deserialize(nameof(BasicEnum.One))!;

        set.Add(deserializedOne);

        var jsonDeserializedOne = JsonSerializer.Deserialize<BasicEnum>($"\"{nameof(BasicEnum.One)}\"")!;

        set.Add(jsonDeserializedOne);
        
        set.Should().HaveCount(1);
    }
}
