using FluentAssertions;

namespace EnumClassSourceGenerator.UnitTests.Tests;

public class EnumClassIgnoreAttributeTests
{
    [Fact]
    public void When_FieldOrPropertyIsIgnored_Then_ShouldExcludeItFromAllValues()
    {
        EnumClassIgnoreEnum.AllValues.Should().Equal(
            EnumClassIgnoreEnum.FirstIncluded,
            EnumClassIgnoreEnum.SecondIncluded);

        EnumClassIgnoreEnum.AllValues.Select(value => value.EnumIndex).Should().Equal(0, 1);
    }

    [Theory]
    [InlineData(nameof(EnumClassIgnoreEnum.IgnoredField))]
    [InlineData(nameof(EnumClassIgnoreEnum.IgnoredProperty))]
    [InlineData(nameof(EnumClassIgnoreEnum.IgnoredInvalidField))]
    [InlineData(nameof(EnumClassIgnoreEnum.IgnoredInvalidProperty))]
    public void When_DeserializingIgnoredMemberName_Then_ShouldNotFindAValue(string serializedName)
    {
        EnumClassIgnoreEnum.ContainsSerializedValue(serializedName).Should().BeFalse();
        EnumClassIgnoreEnum.TryDeserialize(serializedName, out var value).Should().BeFalse();
        value.Should().BeNull();
    }
}
