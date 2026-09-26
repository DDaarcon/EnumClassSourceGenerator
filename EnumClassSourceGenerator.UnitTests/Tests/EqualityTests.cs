using FluentAssertions;
using System;

namespace EnumClassSourceGenerator.UnitTests.Tests;

public class EqualityTests
{
    [Fact]
    public void When_ComparingSameReference_Then_EqualsIsTrue()
    {
        var value = BasicEnum.One;
        var sameReference = value;

        value.Equals(sameReference).Should().BeTrue();
        (value == sameReference).Should().BeTrue();
    }

    [Fact]
    public void When_ComparingDifferentReferences_Then_EqualsIsFalse()
    {
        BasicEnum.One.Equals(BasicEnum.Two).Should().BeFalse();
    }

    [Fact]
    public void When_GetHashCode_Then_ShouldUseReferenceIdentity()
    {
        BasicEnum.One.GetHashCode().Should().Be(
            System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(BasicEnum.One));
    }
}
