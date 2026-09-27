using FluentAssertions;
using System.Buffers;
using System.Text;
using System.Text.Json;

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

    [Fact]
    public void When_DeserializingContiguousUtf8ViaJson_Then_ShouldUseDictionaryLookup()
    {
        var result = JsonSerializer.Deserialize<AlternateLookupEnum>("\"Two\"");

        result.Should().BeSameAs(AlternateLookupEnum.Two);
    }

    [Fact]
    public void When_DeserializingEscapedUtf8ViaJson_Then_ShouldUseDecodedStackBuffer()
    {
        var result = JsonSerializer.Deserialize<AlternateLookupEnum>("\"T\\u0077o\"");

        result.Should().BeSameAs(AlternateLookupEnum.Two);
    }

    [Fact]
    public void When_DeserializingMultiSegmentUtf8ViaConverter_Then_ShouldUseDecodedStackBuffer()
    {
        var sequence = CreateSequence("\"T", "w", "o\"");
        var reader = new Utf8JsonReader(sequence, isFinalBlock: true, state: default);
        reader.Read().Should().BeTrue();
        reader.HasValueSequence.Should().BeTrue();

        var result = new AlternateLookupEnum.JsonConverter().Read(
            ref reader,
            typeof(AlternateLookupEnum),
            new JsonSerializerOptions());

        result.Should().BeSameAs(AlternateLookupEnum.Two);
    }

    [Fact]
    public void When_DeserializingLargeEscapedUtf8ViaJson_Then_ShouldUseDecodedPooledBuffer()
    {
        var serializedName = nameof(AlternateLookupEnum.ValueWithNameLongEnoughToExerciseThePooledUtf8BufferPath);
        var escapedValue = string.Concat(serializedName.Select(character => $"\\u{(int)character:x4}"));
        var json = $"\"{escapedValue}\"";

        var result = JsonSerializer.Deserialize<AlternateLookupEnum>(json);

        escapedValue.Length.Should().BeGreaterThan(256);
        result.Should().BeSameAs(AlternateLookupEnum.ValueWithNameLongEnoughToExerciseThePooledUtf8BufferPath);
    }

    [Fact]
    public void When_DeserializingUnknownValueViaDictionaryConverter_Then_ShouldThrowJsonException()
    {
        var act = () => JsonSerializer.Deserialize<AlternateLookupEnum>("\"Unknown\"");

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void When_DeserializingNonStringViaDictionaryConverter_Then_ShouldThrowJsonException()
    {
        var act = () => JsonSerializer.Deserialize<AlternateLookupEnum>("123");

        act.Should().Throw<JsonException>();
    }

    private static ReadOnlySequence<byte> CreateSequence(params string[] segments)
    {
        var first = new ByteSequenceSegment(Encoding.UTF8.GetBytes(segments[0]));
        var last = first;

        foreach (var segment in segments.Skip(1))
        {
            last = last.Append(Encoding.UTF8.GetBytes(segment));
        }

        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    private sealed class ByteSequenceSegment : ReadOnlySequenceSegment<byte>
    {
        public ByteSequenceSegment(ReadOnlyMemory<byte> memory)
        {
            Memory = memory;
        }

        public ByteSequenceSegment Append(ReadOnlyMemory<byte> memory)
        {
            var next = new ByteSequenceSegment(memory)
            {
                RunningIndex = RunningIndex + Memory.Length
            };

            Next = next;
            return next;
        }
    }
}
