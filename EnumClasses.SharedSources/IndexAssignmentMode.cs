namespace EnumClasses
{
    /// <summary>
    /// TODO
    /// </summary>
#if ENUMCLASSES_SOURCE_GENERATOR
    internal enum IndexAssignmentMode
#else
    public enum IndexAssignmentMode
#endif
    {
        /// <summary>
        /// TOOD
        /// </summary>
        CompileTime = 0,

        /// <summary>
        /// TODO
        /// </summary>
        Runtime = 1,
    }
}
