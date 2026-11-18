using FluentAssertions;

namespace EnumClassSourceGenerator.UnitTests.Tests;

public class RecordEnumTests
{
    [Fact]
    public void When_GeneratingRecordEnum_Then_ShouldExposeDeclaredValues()
    {
        RecordEnum.AllValues.Should().Equal(RecordEnum.One, RecordEnum.Two);
    }

    [Fact]
    public void When_ComparingRecordEnumValues_Then_ShouldUseReferenceIdentity()
    {
        var value = RecordEnum.One;
        var sameValue = value;
        object sameReference = value;
        var clonedValue = RecordEnum.One with { };

        value.Equals(sameReference).Should().BeTrue();
        (value == sameValue).Should().BeTrue();
        RecordEnum.One.Equals(RecordEnum.Two).Should().BeFalse();
        (RecordEnum.One == RecordEnum.Two).Should().BeFalse();
        RecordEnum.One.Equals(clonedValue).Should().BeFalse();
        (RecordEnum.One == clonedValue).Should().BeFalse();
        RecordEnum.One.GetHashCode().Should().Be(
            System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(RecordEnum.One));
    }
}
