using System;

namespace EnumClasses
{
    /// <summary>
    /// Base class for an attribute defining Enum Class. Do not use directly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
#if ENUMCLASSES_SOURCE_GENERATOR
    internal abstract class BaseEnumClassAttribute : Attribute
#else
    public abstract class BaseEnumClassAttribute : Attribute
#endif
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
        /// Specifies the method for deserializing values. Defaults to <see cref="DefaultDeserializationMode"/>.
        /// </summary>
        public DeserializationMode DeserializationMode { get; set; } = DefaultDeserializationMode;
        public const DeserializationMode DefaultDeserializationMode = DeserializationMode.Optimized;

        /// <summary>
        /// Controls the constructors restrictions. Defaults to <see cref="DefaultConstructionRestrictionMode" />.
        /// </summary>
        public ConstructionRestrictionMode ConstructionRestrictionMode { get; set; } = DefaultConstructionRestrictionMode;
        public const ConstructionRestrictionMode DefaultConstructionRestrictionMode = ConstructionRestrictionMode.WithProtectedDefaultConstructor;
    }


    /// <summary>
    /// Defines an Enum Class. <br />
    /// Enumerable values should be defined as either fields with `public static readonly` modifiers or properties with `public static` modifiers and only a getter (no setter).
    /// Enumerable values can be of a containing type type or one inheriting from it.
    /// </summary>
    /// 
#if ENUMCLASSES_SOURCE_GENERATOR
    internal sealed class EnumClassAttribute : BaseEnumClassAttribute
#else
    public sealed class EnumClassAttribute : BaseEnumClassAttribute
#endif
    {
    }

    /// <summary>
    /// Defines a Numbered Enum Class. <br />
    /// Enumerable values should be defined as either fields with `public static readonly` modifiers or properties with `public static` modifiers and only a getter (no setter).
    /// Enumerable values can be of a containing type type or one inheriting from it.<br />
    /// Numbered Enum Values has to have a value provided for `EnumIndex` property.
    /// </summary>
#if ENUMCLASSES_SOURCE_GENERATOR
    internal sealed class NumberedEnumClassAttribute : BaseEnumClassAttribute
#else
    public sealed class NumberedEnumClassAttribute : BaseEnumClassAttribute
#endif
    {
        /// <summary>
        /// Flag controlling presence of `require` keyword for `EnumIndex` property.
        /// </summary>
        public bool RequireIndexAssignmentInInitializer { get; set; } = true;
    }
}
