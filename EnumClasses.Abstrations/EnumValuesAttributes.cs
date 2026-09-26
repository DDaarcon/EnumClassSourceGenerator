using System;

namespace EnumClasses
{
    /// <summary>
    /// Marks a field or property that should be ignored when generating Enum Class values.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class EnumClassIgnoreAttribute : Attribute
    {
    }
}
