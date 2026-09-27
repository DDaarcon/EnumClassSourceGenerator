using FluentAssertions;

namespace EnumClassSourceGenerator.UnitTests.Tests;

public class AlternateLookupTests
{
    [Fact]
    public void When_DeserializingSpanWithAlternateLookup_Then_ShouldReturnMatchingValueOrNull()
    {
        var found = AlternateLookupEnum.Deserialize(nameof(AlternateLookupEnum.Two).AsSpan());
        var missing = AlternateLookupEnum.Deserialize("Missing".AsSpan());

        found.Should().BeSameAs(AlternateLookupEnum.Two);
        missing.Should().BeNull();
    }

    [Fact]
    public void When_DeserializingSpanWithAlternateLookup_Then_ShouldNotAllocate()
    {
        var serializedValue = nameof(AlternateLookupEnum.Two).AsSpan();

        // Initialize the generated type and JIT the lookup path before measuring it.
        _ = AlternateLookupEnum.Deserialize(serializedValue);

        var allocatedBytesBefore = GC.GetAllocatedBytesForCurrentThread();
        var result = AlternateLookupEnum.Deserialize(serializedValue);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBytesBefore;

        result.Should().BeSameAs(AlternateLookupEnum.Two);
        allocatedBytes.Should().Be(0);
    }
}
