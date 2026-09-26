using System;

namespace EnumClasses
{
    /// <summary>
    /// Base class for an attribute defining Enum Class. Do not use directly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public abstract class BaseEnumClassAttribute : Attribute
    {
        /// <summary>
        /// Flag enabling generation of a custom <see cref="Text.Json.Serialization.JsonConverter{T}"/> for the enum class. Defaults to <c>true</c>.
        /// </summary>
        public bool GenerateJsonConverter { get; set; } = true;

        /// <summary>
        /// Flag enabling generation of a nested <c>Raw</c> enum and conversions between it and the enum class. Defaults to <c>false</c>.
        /// </summary>
        public bool GenerateRawEnum { get; set; } = false;

        /// <summary>
        /// Flag changing if-based matching into one based on a cached dictionary. 
        /// </summary>
        public bool UseDictionaryForDeserialization { get; set; } = false;
    }


    /// <summary>
    /// Defines an Enum Class. <br />
    /// Enumerable values should be defined as either fields with `public static readonly` modifiers or properties with `public static` modifiers and only a getter (no setter).
    /// Enumerable values can be of a containing type type or one inheriting from it.
    /// </summary>
    public sealed class EnumClassAttribute : BaseEnumClassAttribute
    {
    }

    /// <summary>
    /// Defines a Numbered Enum Class. <br />
    /// Enumerable values should be defined as either fields with `public static readonly` modifiers or properties with `public static` modifiers and only a getter (no setter).
    /// Enumerable values can be of a containing type type or one inheriting from it.<br />
    /// Numbered Enum Values has to have a value provided for `EnumIndex` property.
    /// </summary>
    public sealed class NumberedEnumClassAttribute : BaseEnumClassAttribute
    {
        /// <summary>
        /// Flag controlling presence of `require` keyword for `EnumIndex` property.
        /// </summary>
        public bool RequireIndexAssignmentInInitializer { get; set; } = true;
    }
}
