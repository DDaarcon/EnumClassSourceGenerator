using FluentAssertions;

namespace EnumClassSourceGenerator.UnitTests.Tests;

public class RawEnumTests
{
    [Fact]
    public void When_ConvertingFromRaw_Then_ShouldReturnMatchingEnumClassValue()
    {
        var convertedOne = RawEnum.FromRaw(RawEnum.Raw.One);
        var convertedTwo = RawEnum.FromRaw(RawEnum.Raw.Two);

        convertedOne.Should().BeSameAs(RawEnum.One);
        convertedTwo.Should().BeSameAs(RawEnum.Two);
    }

    [Fact]
    public void When_ImplicitlyConvertingFromRaw_Then_ShouldReturnMatchingEnumClassValue()
    {
        RawEnum converted = RawEnum.Raw.Two;

        converted.Should().BeSameAs(RawEnum.Two);
    }

    [Fact]
    public void When_ConvertingToRaw_Then_ShouldReturnMatchingRawValue()
    {
        RawEnum.Two.ToRaw().Should().Be(RawEnum.Raw.Two);
        RawEnum.ToRaw(RawEnum.One).Should().Be(RawEnum.Raw.One);
        ((RawEnum.Raw)RawEnum.Two).Should().Be(RawEnum.Raw.Two);
    }

    [Fact]
    public void When_TryingConversions_Then_ShouldReturnMatchingFlagsAndValues()
    {
        RawEnum.TryFromRaw(RawEnum.Raw.One, out var enumClassValue).Should().BeTrue();
        enumClassValue.Should().BeSameAs(RawEnum.One);

        RawEnum.TryToRaw(RawEnum.Two, out var rawValue).Should().BeTrue();
        rawValue.Should().Be(RawEnum.Raw.Two);
    }

    [Fact]
    public void When_ConvertingUndefinedRawValue_Then_ShouldRejectIt()
    {
        var undefined = (RawEnum.Raw)int.MaxValue;

        RawEnum.TryFromRaw(undefined, out var result).Should().BeFalse();
        result.Should().BeNull();
        var act = () => RawEnum.FromRaw(undefined);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void When_ConvertingUndeclaredEnumClassValue_Then_ShouldRejectIt()
    {
        var undeclared = new RawEnum();

        RawEnum.TryToRaw(undeclared, out _).Should().BeFalse();
        var act = () => undeclared.ToRaw();
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void When_ConvertingNullEnumClassValue_Then_ShouldRejectIt()
    {
        RawEnum.TryToRaw(null, out _).Should().BeFalse();
        var act = () => RawEnum.ToRaw(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void When_ConvertingNumberedRawValue_Then_ShouldMapByMemberRatherThanEnumIndex()
    {
        NumberedRawEnum.FromRaw(NumberedRawEnum.Raw.Ten).Should().BeSameAs(NumberedRawEnum.Ten);
        NumberedRawEnum.Twenty.ToRaw().Should().Be(NumberedRawEnum.Raw.Twenty);

        ((int)NumberedRawEnum.Raw.Ten).Should().Be(0);
        NumberedRawEnum.Ten.EnumIndex.Should().Be(10);
    }
}
