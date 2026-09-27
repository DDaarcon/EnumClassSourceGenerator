namespace EnumClasses
{
#if ENUMCLASSES_SOURCE_GENERATOR
    internal enum SearchMode
#else
    public enum SearchMode
#endif
    {
        /// <summary>
        /// The matching method is decided based on the amount of values.
        /// </summary>
        Optimized = 0,
        /// <summary>
        /// Forces matching with if chain.
        /// </summary>
        ForceIfChain = 1,
        /// <summary>
        /// Forces matching with a dictionary.
        /// </summary>
        ForceDictionary = 2
    }
}
