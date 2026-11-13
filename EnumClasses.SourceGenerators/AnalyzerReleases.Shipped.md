; Shipped analyzer releases

## Release 0.1.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
ENUMCLGEN001 | EnumClassGenerator | Error | Invalid enum value type
ENUMCLGEN002 | EnumClassGenerator | Error | Invalid enum value property accessors
ENUMCLGEN003 | EnumClassGenerator | Error | Enum class must be defined in a namespace
ENUMCLGEN004 | EnumClassGenerator | Error | Enum class must have valid accessibility
ENUMCLGEN005 | EnumClassGenerator | Error | Unhandled source-generation exception
ENUMCLGEN006 | EnumClassGenerator | Error | Nested enum classes are not supported
ENUMCLGEN007 | EnumClassGenerator | Error | Generic enum classes are not supported
ENUMCLGEN008 | EnumClassGenerator | Error | Enum class value name collides with generated code
ENUMCLGEN009 | EnumClassGenerator | Error | Restricted enum class constructors must be private
ENUMCLGEN010 | EnumClassGenerator | Error | Enum class value fields must be initialized
ENUMCLGEN011 | EnumClassGenerator | Error | Enum class values must be declared in one partial declaration
ENUMCLGEN012 | EnumClassGenerator | Error | Enum class value indexes must be unique
ENUMCLGEN013 | EnumClassGenerator | Warning | Index is ignored when runtime assignment is used
ENUMCLGEN014 | EnumClassGenerator | Error | Explicit index cannot be shared by multiple fields
ENUMCLGEN015 | EnumClassGenerator | Warning | Runtime indexes may differ from generated raw enum values
ENUMCLGEN016 | EnumClassGenerator | Error | Unsupported index assignment mode
ENUMCLGEN017 | EnumClassGenerator | Error | Compile-time EnumIndex must not be assigned in a value initializer

## Release 0.1.2

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
ENUMCLGEN018 | EnumClassGenerator | Warning | Runtime EnumIndex falls back to a mutable setter when init is unavailable
ENUMCLGEN019 | EnumClassGenerator | Error | JSON converter generation requires C# 7.2 and compatible framework APIs
