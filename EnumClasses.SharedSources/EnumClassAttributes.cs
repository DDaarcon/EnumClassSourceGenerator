using System;

namespace EnumClasses
{
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
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
#if ENUMCLASSES_SOURCE_GENERATOR
    internal sealed class EnumClassAttribute : Attribute
#else
    public sealed class EnumClassAttribute : Attribute
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

        /// <summary>
        /// Gets or sets whether indexes are resolved by the generator or assigned by value initializers at runtime.
        /// </summary>
        /// <remarks>
        /// When omitted, the value configured by <see cref="EnumClassDefaultsAttribute.IndexAssignmentMode"/> is used.
        /// The built-in default is <see cref="EnumClasses.IndexAssignmentMode.CompileTime"/>.
        /// </remarks>
        public IndexAssignmentMode IndexAssignmentMode { get; set; } = EnumClassDefaultsAttribute.DefaultIndexAssignmentMode;

        /// <summary>
        /// Gets or sets whether additional XML documentation is generated for the enum class.
        /// </summary>
        /// <remarks>
        /// When omitted, the value configured by <see cref="EnumClassDefaultsAttribute.DocumentationMode"/> is used.
        /// The built-in default is <see cref="EnumClasses.DocumentationMode.ListCompileTimeIndexes"/>.
        /// </remarks>
        public DocumentationMode DocumentationMode { get; set; } = EnumClassDefaultsAttribute.DefaultDocumentationMode;
    }
}
