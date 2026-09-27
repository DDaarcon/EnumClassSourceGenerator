using FluentAssertions;
using System;

namespace EnumClassSourceGenerator.UnitTests.Tests;

public class SwitchTests
{
    [Fact]
    public void When_SwitchHasCallbackForValue_Then_ShouldInvokeOnlyMatchedCallback()
    {
        var invoked = false;

        BasicEnum.Two.Switch(
            onOne: () => throw new InvalidOperationException(),
            onTwo: () => invoked = true,
            onThree: () => throw new InvalidOperationException());

        invoked.Should().BeTrue();
    }

    [Fact]
    public void When_SwitchHasNoCallbackForValue_Then_ShouldNotInvokeAnyCallback()
    {
        var invoked = false;

        BasicEnum.Four.Switch(
            onOne: () => invoked = true,
            onTwo: () => invoked = true,
            onThree: () => invoked = true);

        invoked.Should().BeFalse();
    }

    [Fact]
    public void When_SwitchExIsCalled_Then_ShouldInvokeOnlyMatchedCallback()
    {
        var invoked = false;

        BasicEnum.Three.SwitchEx(
            onOne: () => throw new InvalidOperationException(),
            onTwo: () => throw new InvalidOperationException(),
            onThree: () => invoked = true,
            onFour: () => throw new InvalidOperationException());

        invoked.Should().BeTrue();
    }

    [Fact]
    public void When_SwitchExIsCalledForUndeclaredValue_Then_ShouldThrow()
    {
        var undeclared = new BasicEnum { Value = "Undeclared" };

        var act = () => undeclared.SwitchEx(
            onOne: () => { },
            onTwo: () => { },
            onThree: () => { },
            onFour: () => { });

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Invalid value of BasicEnum.");
    }

    [Fact]
    public void When_MatchHasCallbackForValue_Then_ShouldReturnMatchedResult()
    {
        var result = BasicEnum.Two.Match(
            onOne: () => throw new InvalidOperationException(),
            onTwo: () => "two",
            onThree: () => throw new InvalidOperationException());

        result.Should().Be("two");
    }

    [Fact]
    public void When_MatchHasNoCallbackForValue_Then_ShouldReturnNull()
    {
        var result = BasicEnum.Four.Match<string>(
            onOne: () => "one",
            onTwo: () => "two",
            onThree: () => "three");

        result.Should().BeNull();
    }

    [Fact]
    public void When_MatchExIsCalled_Then_ShouldReturnMatchedResult()
    {
        var result = BasicEnum.Three.MatchEx(
            onOne: () => throw new InvalidOperationException(),
            onTwo: () => throw new InvalidOperationException(),
            onThree: () => "three",
            onFour: () => throw new InvalidOperationException());

        result.Should().Be("three");
    }

    [Fact]
    public void When_MatchExIsCalledForUndeclaredValue_Then_ShouldThrow()
    {
        var undeclared = new BasicEnum { Value = "Undeclared" };

        var act = () => undeclared.MatchEx(
            onOne: () => "one",
            onTwo: () => "two",
            onThree: () => "three",
            onFour: () => "four");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Invalid value of BasicEnum.");
    }

    [Fact]
    public void When_SwitchWithStateHasCallbackForValue_Then_ShouldPassStateToOnlyMatchedCallback()
    {
        var state = new List<string>();

        BasicEnum.Two.Switch(
            state,
            onOne: static _ => throw new InvalidOperationException(),
            onTwo: static state => state.Add("two"),
            onThree: static _ => throw new InvalidOperationException());

        state.Should().Equal("two");
    }

    [Fact]
    public void When_SwitchWithStateHasNoCallbackForValue_Then_ShouldNotInvokeAnyCallback()
    {
        var state = new List<string>();

        BasicEnum.Four.Switch(
            state,
            onOne: static state => state.Add("one"),
            onTwo: static state => state.Add("two"),
            onThree: static state => state.Add("three"));

        state.Should().BeEmpty();
    }

    [Fact]
    public void When_SwitchExWithStateIsCalled_Then_ShouldPassStateToOnlyMatchedCallback()
    {
        var state = new List<string>();

        BasicEnum.Three.SwitchEx(
            state,
            onOne: static _ => throw new InvalidOperationException(),
            onTwo: static _ => throw new InvalidOperationException(),
            onThree: static state => state.Add("three"),
            onFour: static _ => throw new InvalidOperationException());

        state.Should().Equal("three");
    }

    [Fact]
    public void When_SwitchExWithStateIsCalledForUndeclaredValue_Then_ShouldThrow()
    {
        var undeclared = new BasicEnum { Value = "Undeclared" };

        var act = () => undeclared.SwitchEx(
            state: "state",
            onOne: static _ => { },
            onTwo: static _ => { },
            onThree: static _ => { },
            onFour: static _ => { });

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Invalid value of BasicEnum.");
    }

    [Fact]
    public void When_MatchWithStateHasCallbackForValue_Then_ShouldPassStateAndReturnMatchedResult()
    {
        var result = BasicEnum.Two.Match(
            state: "value",
            onOne: static _ => throw new InvalidOperationException(),
            onTwo: static state => state + "-two",
            onThree: static _ => throw new InvalidOperationException());

        result.Should().Be("value-two");
    }

    [Fact]
    public void When_MatchWithStateHasNoCallbackForValue_Then_ShouldReturnNull()
    {
        var result = BasicEnum.Four.Match<string, string>(
            state: "value",
            onOne: static state => state + "-one",
            onTwo: static state => state + "-two",
            onThree: static state => state + "-three");

        result.Should().BeNull();
    }

    [Fact]
    public void When_MatchExWithStateIsCalled_Then_ShouldPassStateAndReturnMatchedResult()
    {
        var result = BasicEnum.Three.MatchEx(
            state: "value",
            onOne: static _ => throw new InvalidOperationException(),
            onTwo: static _ => throw new InvalidOperationException(),
            onThree: static state => state + "-three",
            onFour: static _ => throw new InvalidOperationException());

        result.Should().Be("value-three");
    }

    [Fact]
    public void When_MatchExWithStateIsCalledForUndeclaredValue_Then_ShouldThrow()
    {
        var undeclared = new BasicEnum { Value = "Undeclared" };

        var act = () => undeclared.MatchEx(
            state: "value",
            onOne: static state => state + "-one",
            onTwo: static state => state + "-two",
            onThree: static state => state + "-three",
            onFour: static state => state + "-four");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Invalid value of BasicEnum.");
    }
}
