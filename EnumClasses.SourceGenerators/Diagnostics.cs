using Microsoft.CodeAnalysis;

namespace EnumClasses.SourceGenerators;

internal static class Diagnostics
{
    public static Diagnostic InvalidEnumValueType(Location? valueLocation, string valueName, string declarationaName, string valueTypeName)
        => Diagnostic.Create(InvalidEnumValueTypeDescriptor, valueLocation, valueName, declarationaName, valueTypeName);
    public static readonly DiagnosticDescriptor InvalidEnumValueTypeDescriptor =
        new(
            id: "ENUMCLGEN001",
            title: "Invalid type of a Enum Class Value field/property",
            messageFormat: "Type '{2}' of '{0}' is not assignable to '{1}'",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);


    public static Diagnostic InvalidEnumValueAccessors(Location? valueLocation, string valueName, string declarationaName)
        => Diagnostic.Create(InvalidEnumValueAccessorsDescriptor, valueLocation, valueName, declarationaName);
    public static readonly DiagnosticDescriptor InvalidEnumValueAccessorsDescriptor =
        new(
            id: "ENUMCLGEN002",
            title: "Invalid accessors for a Enum Class Value property",
            messageFormat: "'{0}' on '{1}' has invalid property accessors, it has to have a getter and must not have an 'init' nor 'set' setter",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);


    public static Diagnostic NamespaceNotFound(Location? declarationLocation, string declarationName)
        => Diagnostic.Create(NamespaceNotFoundDescriptor, declarationLocation, declarationName);
    public static readonly DiagnosticDescriptor NamespaceNotFoundDescriptor =
        new(
            id: "ENUMCLGEN003",
            title: "Enum Class has to be defined in a namespace",
            messageFormat: "Enum Class '{0}' is not defined in a namespace",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);


    public static Diagnostic InvalidModifiers(Location? declarationLocation, string declarationName)
        => Diagnostic.Create(InvalidModifiersDescriptor, declarationLocation, declarationName);
    public static readonly DiagnosticDescriptor InvalidModifiersDescriptor =
        new(
            id: "ENUMCLGEN004",
            title: "Enum Class has to be either public, internal, protected or private",
            messageFormat: "Enum Class '{0}' has invalid access modifiers",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);


    public static Diagnostic UnexpectedException(Location? location, string message, string declarationName)
        => Diagnostic.Create(UnexpectedExceptionDescriptor, location, message, declarationName);
    public static readonly DiagnosticDescriptor UnexpectedExceptionDescriptor =
        new(
            id: "ENUMCLGEN005",
            title: "Unhandled exception",
            messageFormat: "Unhandled exception when generating Enum Class '{1}': {0}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);


    public static Diagnostic NestedTypeNotSupported(Location? declarationLocation, string declarationaName)
        => Diagnostic.Create(NestedTypeNotSupportedDescriptor, declarationLocation, declarationaName);
    public static readonly DiagnosticDescriptor NestedTypeNotSupportedDescriptor =
        new(
            id: "ENUMCLGEN006",
            title: "Nested Enum Classes are not supported",
            messageFormat: "Enum Class '{0}' is nested inside another type. Move it to namespace scope.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);


    public static Diagnostic GenericTypeNotSupported(Location? declarationLocation, string declarationaName)
        => Diagnostic.Create(GenericTypeNotSupportedDescriptor, declarationLocation, declarationaName);
    public static readonly DiagnosticDescriptor GenericTypeNotSupportedDescriptor =
        new(
            id: "ENUMCLGEN007",
            title: "Generic Enum Classes are not supported",
            messageFormat: "Enum Class '{0}' has type parameters. Generic enum classes are not currently supported.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);


    public static Diagnostic ReservedKeywordUsedForValue(Location? valueLocation, string valueName, string declarationaName)
        => Diagnostic.Create(ReservedKeywordUsedForValueDescriptor, valueLocation, valueName, declarationaName);
    public static readonly DiagnosticDescriptor ReservedKeywordUsedForValueDescriptor =
        new(
            id: "ENUMCLGEN008",
            title: "The name of an Enum Class value collides with generated code",
            messageFormat: "The name '{0}' on Enum Class '{1}' collides with generated code and can not be used for a value name",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);


    public static Diagnostic NonPrivateConstructorOnRestrictedEnumClass(Location? constructorLocation, string declarationName)
        => Diagnostic.Create(NonPrivateConstructorOnRestrictedEnumClassDescriptor, constructorLocation, declarationName);
    public static readonly DiagnosticDescriptor NonPrivateConstructorOnRestrictedEnumClassDescriptor =
        new(
            id: "ENUMCLGEN009",
            title: "Restricted Enum Class constructors must be private",
            messageFormat: "Constructor on Enum Class '{0}' must be private when ConstructionRestrictionMode is not Off",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);


    public static Diagnostic UninitializedEnumValueField(Location? valueLocation, string valueName, string declarationName)
        => Diagnostic.Create(UninitializedEnumValueFieldDescriptor, valueLocation, valueName, declarationName);
    public static readonly DiagnosticDescriptor UninitializedEnumValueFieldDescriptor =
        new(
            id: "ENUMCLGEN010",
            title: "Enum Class value fields must be initialized",
            messageFormat: "Enum Class value field '{0}' on '{1}' must have an initializer",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static Diagnostic ValuesDeclaredInMultiplePartialDeclarations(Location? declarationLocation, string declarationName)
        => Diagnostic.Create(ValuesDeclaredInMultiplePartialDeclarationsDescriptor, declarationLocation, declarationName);
    public static readonly DiagnosticDescriptor ValuesDeclaredInMultiplePartialDeclarationsDescriptor =
        new(
            id: "ENUMCLGEN011",
            title: "Enum Class values must be declared in one partial declaration",
            messageFormat: "Enum Class '{0}' declares values in multiple partial declarations; move all values into a single declaration",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static Diagnostic CollidingEnumIndexes(Location? valueLocation, string declarationName, string valueName, string collidingValuesList)
        => Diagnostic.Create(CollidingEnumIndexesDescriptor, valueLocation, declarationName, valueName, collidingValuesList);
    public static readonly DiagnosticDescriptor CollidingEnumIndexesDescriptor =
        new(
            id: "ENUMCLGEN012",
            title: "Enum Class values' indexes must be unique",
            messageFormat: "Enum Class '{0}' value '{1}' has the same index as '{2}'",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    // TODO separate errors from warnings
    public static Diagnostic IndexProvidedToTheAttributeWillBeIgnoredForRuntimeIndexes(Location? attributeLocation)
        => Diagnostic.Create(IndexProvidedToTheAttributeWillBeIgnoredForRuntimeIndexesDescriptor, attributeLocation);
    public static readonly DiagnosticDescriptor IndexProvidedToTheAttributeWillBeIgnoredForRuntimeIndexesDescriptor =
        new(
            id: "ENUMCLGEN013",
            title: "Redundant index specification",
            messageFormat: "Index specified in EnumClassValueAttribute is ignored when IndexAssignmentMode is set to Runtime",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

    public static Diagnostic ExplicitIndexCannotBeSharedByMultipleFields(Location? attributeLocation)
        => Diagnostic.Create(ExplicitIndexCannotBeSharedByMultipleFieldsDescriptor, attributeLocation);
    public static readonly DiagnosticDescriptor ExplicitIndexCannotBeSharedByMultipleFieldsDescriptor =
        new(
            id: "ENUMCLGEN014",
            title: "An explicit Enum Class index applies to exactly one value",
            messageFormat: "EnumClassValueAttribute with an index cannot be applied to a field declaration containing multiple values; split the values into separate declarations",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static Diagnostic RuntimeIndexesMayDifferFromRawEnumValues(Location? attributeLocation)
        => Diagnostic.Create(RuntimeIndexesMayDifferFromRawEnumValuesDescriptor, attributeLocation);
    public static readonly DiagnosticDescriptor RuntimeIndexesMayDifferFromRawEnumValuesDescriptor =
        new(
            id: "ENUMCLGEN015",
            title: "Runtime indexes may differ from generated Raw enum values",
            messageFormat: "Runtime EnumIndex values may not match the numeric values of the generated Raw enum, whose values follow declaration order",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

    public static Diagnostic UnsupportedIndexAssignmentMode(Location? valueLocation, int value)
        => Diagnostic.Create(UnsupportedIndexAssignmentModeDescriptor, valueLocation, value);
    public static readonly DiagnosticDescriptor UnsupportedIndexAssignmentModeDescriptor =
        new(
            id: "ENUMCLGEN016",
            title: "Unsupported index assignment mode",
            messageFormat: "IndexAssignmentMode value '{0}' is not supported; use CompileTime or Runtime",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public const string Category = "EnumClassGenerator";
}
