using FluentAssertions;
using System;

namespace EnumClassSourceGenerator.UnitTests.Tests;

public class TryDeserializeTests
{
    [Fact]
    public void When_TryDeserializeWithValidValue_Then_ShouldReturnTrueAndValue()
    {
        var ok = BasicEnum.TryDeserialize(nameof(BasicEnum.Four), out var result);
        ok.Should().BeTrue();
        result.Should().Be(BasicEnum.Four);
    }

    [Fact]
    public void When_TryDeserializeWithInvalidValue_Then_ShouldReturnFalseAndNull()
    {
        var ok = BasicEnum.TryDeserialize("NotAValue", out var result);
        ok.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void When_ContainsSerializedValue_Then_ShouldMatchDeserialize()
    {
        BasicEnum.ContainsSerializedValue(nameof(BasicEnum.One)).Should().BeTrue();
        BasicEnum.ContainsSerializedValue(null).Should().BeFalse();
    }
}
