using FluentAssertions;
using System;
using System.Reflection;

namespace EnumClassSourceGenerator.UnitTests.Tests;

public class RuntimeIndexTests
{
    [Fact]
    public void When_GetByRuntimeAssignedEnumIndex_Then_ShouldReturnExpectedValue()
    {
        var got = RuntimeIndexedEnum.GetByEnumIndex(10);
        got.Should().Be(RuntimeIndexedEnum.Ten);

        RuntimeIndexedEnum.ContainsEnumIndex(20).Should().BeTrue();
        RuntimeIndexedEnum.ContainsEnumIndex(15).Should().BeFalse();
    }

    [Fact]
    public void When_RuntimeIndexesAreDuplicated_Then_TypeInitializationShouldFailWithMeaningfulError()
    {
        var action = () => DuplicateRuntimeIndexedEnum.GetByEnumIndex(7);

        var exception = action.Should()
            .Throw<TypeInitializationException>()
            .Which;
        exception.InnerException.Should()
            .BeOfType<InvalidOperationException>()
            .Which.Message.Should().Be("EnumIndex 7 is assigned more than once.");
    }

    [Fact]
    public void When_RequiredAssignmentIsBypassed_Then_TypeInitializationShouldFailWithMeaningfulError()
    {
        var action = () => UnassignedRuntimeIndexedEnum.GetByEnumIndex(0);

        var exception = action.Should()
            .Throw<TypeInitializationException>()
            .Which;
        exception.InnerException.Should()
            .BeOfType<InvalidOperationException>()
            .Which.Message.Should().Be(
                "Every declared UnassignedRuntimeIndexedEnum value must assign EnumIndex.");
    }

    [Fact]
    public void When_RuntimeIndexesUseNegativeAndExtremeValues_Then_LookupShouldSupportFullIntRange()
    {
        ExtremeRuntimeIndexedEnum.GetByEnumIndex(int.MinValue)
            .Should().BeSameAs(ExtremeRuntimeIndexedEnum.Minimum);
        ExtremeRuntimeIndexedEnum.GetByEnumIndex(-1)
            .Should().BeSameAs(ExtremeRuntimeIndexedEnum.Negative);
        ExtremeRuntimeIndexedEnum.GetByEnumIndex(int.MaxValue)
            .Should().BeSameAs(ExtremeRuntimeIndexedEnum.Maximum);
        ExtremeRuntimeIndexedEnum.GetByEnumIndex(0).Should().BeNull();
    }

    [Fact]
    public void When_DictionaryLookupIsForced_Then_RuntimeIndexesShouldUseInitializedDictionary()
    {
        DictionaryRuntimeIndexedEnum.GetByEnumIndex(-100)
            .Should().BeSameAs(DictionaryRuntimeIndexedEnum.Negative);
        DictionaryRuntimeIndexedEnum.GetByEnumIndex(100)
            .Should().BeSameAs(DictionaryRuntimeIndexedEnum.Positive);
        DictionaryRuntimeIndexedEnum.GetByEnumIndex(0).Should().BeNull();

        var lookupDictionary = typeof(DictionaryRuntimeIndexedEnum).GetField(
            "_valuesByEnumIndex",
            BindingFlags.NonPublic | BindingFlags.Static);

        lookupDictionary.Should().NotBeNull();
        lookupDictionary!.GetValue(null).Should().NotBeNull();
    }

    [Fact]
    public void When_RuntimeAtValueInitUsesConstructorAndInitializerAssignments_Then_ShouldResolveBothIndexes()
    {
        RuntimeAtValueInitAssignmentEnum.GetByEnumIndex(30)
            .Should().BeSameAs(RuntimeAtValueInitAssignmentEnum.FromConstructor);
        RuntimeAtValueInitAssignmentEnum.GetByEnumIndex(40)
            .Should().BeSameAs(RuntimeAtValueInitAssignmentEnum.FromInitializer);
    }

    [Fact]
    public void When_RuntimeAtValueCreationUsesConstructorAndInitializerAssignments_Then_ShouldResolveBothIndexes()
    {
        RuntimeAtValueCreationAssignmentEnum.GetByEnumIndex(50)
            .Should().BeSameAs(RuntimeAtValueCreationAssignmentEnum.FromConstructor);
        RuntimeAtValueCreationAssignmentEnum.GetByEnumIndex(60)
            .Should().BeSameAs(RuntimeAtValueCreationAssignmentEnum.FromInitializer);
    }

    [Fact]
    public void When_RuntimeAtValueCreationAssignmentIsOmitted_Then_TypeInitializationShouldFailWithMeaningfulError()
    {
        var action = () => UnassignedRuntimeAtValueCreationEnum.GetByEnumIndex(0);

        var exception = action.Should()
            .Throw<TypeInitializationException>()
            .Which;
        exception.InnerException.Should()
            .BeOfType<InvalidOperationException>()
            .Which.Message.Should().Be(
                "Every declared UnassignedRuntimeAtValueCreationEnum value must assign EnumIndex.");
    }
}
