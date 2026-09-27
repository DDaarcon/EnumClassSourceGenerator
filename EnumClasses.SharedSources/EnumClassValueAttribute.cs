using System;

namespace EnumClasses;

/// <summary>
/// TODO rephrase description
/// Attribute defining values of an Enum Class. The attribute is optional, it's main function is defining complie-time constants.
/// </summary>
[AttributeUsage(
    AttributeTargets.Field | AttributeTargets.Property,
    AllowMultiple = false,
    Inherited = false)]

#if ENUMCLASSES_SOURCE_GENERATOR
internal sealed class EnumClassValueAttribute : Attribute
#else
public sealed class EnumClassValueAttribute : Attribute
#endif
{
    public EnumClassValueAttribute()
    {
    }

    public EnumClassValueAttribute(int enumIndex)
    {
        EnumIndex = enumIndex;
    }

    public int EnumIndex { get; }

    // TODO Potential future addition:
    // public string? SerializedName { get; set; }
}
