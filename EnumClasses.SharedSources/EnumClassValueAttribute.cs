using System;

namespace EnumClasses;

/// <summary>
/// Marks an enum-class value and optionally assigns its compile-time <c>EnumIndex</c>.
/// </summary>
/// <remarks>
/// The attribute is optional. Use <see cref="EnumClassValueAttribute(int)"/> when
/// <see cref="EnumClassAttribute.IndexAssignmentMode"/> is <see cref="IndexAssignmentMode.CompileTime"/>.
/// </remarks>
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
    /// <summary>Initializes the attribute without assigning an explicit index.</summary>
    public EnumClassValueAttribute()
    {
    }

    /// <summary>Initializes the attribute with an explicit compile-time index.</summary>
    /// <param name="enumIndex">The index assigned to this value.</param>
    public EnumClassValueAttribute(int enumIndex)
    {
        EnumIndex = enumIndex;
    }

    /// <summary>Gets the explicit compile-time index.</summary>
    public int EnumIndex { get; }
}
