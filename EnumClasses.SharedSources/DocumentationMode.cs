namespace EnumClasses
{
    /// <summary>
    /// Specifies whether additional XML documentation is generated for an enum class.
    /// </summary>
#if ENUMCLASSES_SOURCE_GENERATOR
    internal enum DocumentationMode
#else
    public enum DocumentationMode
#endif
    {
        /// <summary>
        /// Lists the declared enum-class values and their resolved indexes in the generated class documentation
        /// when indexes are assigned at compile time.
        /// </summary>
        ListCompileTimeIndexes = 0,

        /// <summary>
        /// Disables additional generated XML documentation.
        /// </summary>
        Disabled = 1
    }
}
