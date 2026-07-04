using FluentAssertions;
using System;

namespace EnumClassSourceGenerator.UnitTests.Tests;

public class SwitchTests
{
    [Fact]
    public void When_SwitchWithActions_Then_ShouldInvokeOnlyMatchedAction()
    {
        var invoked = false;

        BasicEnum.One.Switch(
            onOne: () => invoked = true,
            onTwo: () => throw new InvalidOperationException(),
            onThree: null,
            onFour: null);

        invoked.Should().BeTrue();
    }

    [Fact]
    public void When_SwitchWithFunc_Then_ShouldReturnMatchedResultOrNull()
    {
        var result = BasicEnum.Two.Switch(
            onOne: () => "one",
            onTwo: () => "two",
            onThree: null,
            onFour: null);

        result.Should().Be("two");

        var none = BasicEnum.Four.Switch<object>(
            onOne: null,
            onTwo: null,
            onThree: null,
            onFour: null);

        none.Should().BeNull();
    }
}
