using System;

namespace EnumClasses
{
    /// <summary>
    /// Provides configuration shared by attributes that generate an enum class.
    /// </summary>
    /// <remarks>Apply <see cref="EnumClassAttribute"/> or <see cref="NumberedEnumClassAttribute"/> instead.</remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
#if ENUMCLASSES_SOURCE_GENERATOR
    internal abstract class BaseEnumClassAttribute : Attribute
#else
    public abstract class BaseEnumClassAttribute : Attribute
#endif
    {
        /// <summary>
        /// Gets or sets whether a <c>System.Text.Json.Serialization.JsonConverter&lt;T&gt;</c> is generated for the enum class.
        /// </summary>
        /// <remarks>
        /// When omitted, the value configured by <see cref="EnumClassDefaultsAttribute.GenerateJsonConverter"/> is used.
        /// The built-in default is <see langword="true"/>.
        /// </remarks>
        public bool GenerateJsonConverter { get; set; } = true;

        /// <summary>
        /// Gets or sets whether a nested <c>Raw</c> enum and conversions to and from it are generated.
        /// </summary>
        /// <remarks>
        /// When omitted, the value configured by <see cref="EnumClassDefaultsAttribute.GenerateRawEnum"/> is used.
        /// The built-in default is <see langword="false"/>.
        /// </remarks>
        public bool GenerateRawEnum { get; set; } = false;

        /// <summary>
        /// Gets or sets how the generator chooses lookup implementations for operations such as deserialization.
        /// </summary>
        /// <remarks>When omitted, <see cref="EnumClassDefaultsAttribute.LookupMode"/> is used.</remarks>
        public LookupMode LookupMode { get; set; } = EnumClassDefaultsAttribute.DefaultLookupMode;

        /// <summary>
        /// Gets or sets the restrictions applied to instance constructors.
        /// </summary>
        /// <remarks>When omitted, <see cref="EnumClassDefaultsAttribute.ConstructionRestrictionMode"/> is used.</remarks>
        public ConstructionRestrictionMode ConstructionRestrictionMode { get; set; } = EnumClassDefaultsAttribute.DefaultConstructionRestrictionMode;
    }


    /// <summary>
    /// Marks a class for enum-class member generation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The target must be a partial, non-generic, non-nested class in a named namespace and must declare
    /// its accessibility explicitly.
    /// </para>
    /// <para>
    /// Values are declared as initialized <c>public static readonly</c> fields or initialized
    /// <c>public static</c> get-only auto-properties. A value may have the target class type or a type
    /// derived from it. Apply <see cref="EnumClassIgnoreAttribute"/> to an otherwise eligible member to
    /// exclude it from generation.
    /// </para>
    /// <para>
    /// Apply <see cref="EnumClassDefaultsAttribute"/> to the assembly to configure defaults for options
    /// not specified on this attribute.
    /// </para>
    /// </remarks>
#if ENUMCLASSES_SOURCE_GENERATOR
    internal sealed class EnumClassAttribute : BaseEnumClassAttribute
#else
    public sealed class EnumClassAttribute : BaseEnumClassAttribute
#endif
    {
    }

    /// <summary>
    /// Marks a class for numbered enum-class member generation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The target and its values must have the same shape required by <see cref="EnumClassAttribute"/>.
    /// Each declared value must have a unique <c>EnumIndex</c> value.
    /// </para>
    /// <para>
    /// Apply <see cref="EnumClassDefaultsAttribute"/> to the assembly to configure defaults for options
    /// not specified on this attribute.
    /// </para>
    /// </remarks>
#if ENUMCLASSES_SOURCE_GENERATOR
    internal sealed class NumberedEnumClassAttribute : BaseEnumClassAttribute
#else
    public sealed class NumberedEnumClassAttribute : BaseEnumClassAttribute
#endif
    {
        /// <summary>
        /// Gets or sets whether the generated <c>EnumIndex</c> property uses the <see langword="required"/> modifier.
        /// </summary>
        /// <remarks>
        /// When omitted, the value configured by <see cref="EnumClassDefaultsAttribute.RequireIndexAssignmentInInitializer"/> is used.
        /// The built-in default is <see langword="true"/>.
        /// </remarks>
        public bool RequireIndexAssignmentInInitializer { get; set; } = true;
    }
}
