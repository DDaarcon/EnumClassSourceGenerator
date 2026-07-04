using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Text;

namespace EnumClassSourceGenerator.UnitTests.Tests;

public class AllValuesTests
{
    [Fact]
    public void When_AccessingAllValues_Then_ShouldGetAllPossibleValuesInDeclarationOrder()
    {
        var allValues = BasicEnum.AllValues;

        allValues.Should().HaveCount(4);
        allValues.ElementAt(0).Should().Be(BasicEnum.One);
        allValues.ElementAt(1).Should().Be(BasicEnum.Two);
        allValues.ElementAt(2).Should().Be(BasicEnum.Three);
        allValues.ElementAt(3).Should().Be(BasicEnum.Four);
    }
}
