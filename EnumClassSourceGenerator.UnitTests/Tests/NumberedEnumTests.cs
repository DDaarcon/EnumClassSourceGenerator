using FluentAssertions;
using System;

namespace EnumClassSourceGenerator.UnitTests.Tests;

public class NumberedEnumTests
{
    [Fact]
    public void When_GetByEnumIndexOnNumberedEnum_Then_ShouldReturnExpectedValue()
    {
        var got = NumberedEnum.GetByEnumIndex(10);
        got.Should().Be(NumberedEnum.Ten);

        NumberedEnum.ContainsEnumIndex(20).Should().BeTrue();
        NumberedEnum.ContainsEnumIndex(15).Should().BeFalse();
    }
}
