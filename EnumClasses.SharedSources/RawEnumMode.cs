namespace EnumClasses
{

#if ENUMCLASSES_SOURCE_GENERATOR
    internal enum RawEnumMode
#else
    public enum RawEnumMode
#endif
    {
        Off = 0,
        On = 1
    }
}
