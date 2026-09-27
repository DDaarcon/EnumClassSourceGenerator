using System;

namespace EnumClasses
{
    /// <summary>
    /// Excludes an otherwise eligible field or property from the generated enum-class values.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
#if ENUMCLASSES_SOURCE_GENERATOR
    internal class EnumClassIgnoreAttribute : Attribute
#else
    public class EnumClassIgnoreAttribute : Attribute
#endif
    {
    }
}
