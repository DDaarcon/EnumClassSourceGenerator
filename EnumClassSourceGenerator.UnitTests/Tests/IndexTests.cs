using FluentAssertions;
using System;

namespace EnumClassSourceGenerator.UnitTests.Tests;

public class IndexTests
{
    [Fact]
    public void When_GettingByEnumIndex_Then_ShouldReturnExpectedValueOrNull()
    {
        var byIndexOne = BasicEnum.GetByEnumIndex(1);
        byIndexOne.Should().Be(BasicEnum.Two);

        var byIndexNegative = BasicEnum.GetByEnumIndex(-1);
        byIndexNegative.Should().BeNull();
    }

    [Fact]
    public void When_TryingGetByEnumIndex_Then_ShouldReturnFlagAndOutValue()
    {
        var ok = BasicEnum.TryGetByEnumIndex(2, out var value);
        ok.Should().BeTrue();
        value.Should().Be(BasicEnum.Three);

        var notOk = BasicEnum.TryGetByEnumIndex(99, out var missing);
        notOk.Should().BeFalse();
        missing.Should().BeNull();
    }

    [Fact]
    public void When_CheckingContainsEnumIndex_Then_ShouldReflectPresence()
    {
        BasicEnum.ContainsEnumIndex(0).Should().BeTrue();
        BasicEnum.ContainsEnumIndex(999).Should().BeFalse();
    }
}
