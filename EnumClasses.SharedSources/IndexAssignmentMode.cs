namespace EnumClasses
{
    /// <summary>
    /// Specifies how generated enum-class values receive their <c>EnumIndex</c> values.
    /// </summary>
#if ENUMCLASSES_SOURCE_GENERATOR
    internal enum IndexAssignmentMode
#else
    public enum IndexAssignmentMode
#endif
    {
        /// <summary>
        /// Resolves indexes during source generation. Values are numbered in declaration order, and an
        /// <see cref="EnumClassValueAttribute"/> index restarts the sequence for that value.
        /// </summary>
        CompileTime = 0,

        /// <summary>
        /// Requires each declared value to assign <c>EnumIndex</c> in its initializer and validates the
        /// completed declared set when the enum class is initialized.
        /// </summary>
        Runtime = 1,
    }
}
