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

        public const string Category = "EnumClassGenerator";
}
