using FluentAssertions;
using System;

namespace EnumClassSourceGenerator.UnitTests.Tests;

public class OfTypeTests
{
    [Fact]
    public void When_TryGetOfTypeWithDerived_Then_ShouldReturnTrueAndTypedValue()
    {
        var ok = CustomTypedEnum.TryGetOfType<DerivedTypedEnum>(CustomTypedEnum.Derived, out var typed);
        ok.Should().BeTrue();
        typed.Should().NotBeNull();
        typed!.Extra.Should().Be(5);
    }

    [Fact]
    public void When_TryGetOfTypeWithBase_Then_ShouldReturnFalse()
    {
        var ok = CustomTypedEnum.TryGetOfType<DerivedTypedEnum>(CustomTypedEnum.Base, out var typed);
        ok.Should().BeFalse();
        typed.Should().BeNull();
    }
}
