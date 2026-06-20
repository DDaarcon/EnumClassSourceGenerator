using Microsoft.CodeAnalysis;

namespace EnumClassSourceGenerator;

internal static class Diagnostics
{
    public static readonly DiagnosticDescriptor InvalidEnumValueType =
        new(
            id: "ENUMCLGEN001",
            title: "Invalid type of a Enum Class Value field/property",
            messageFormat: "Type '{0}' is not assignable to '{1}'",
            category: "EnumClassGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidEnumValueAccessors =
        new(
            id: "ENUMCLGEN002",
            title: "Invalid accessors for a Enum Class Value property",
            messageFormat: "Enum Class Value property has to have a getter and must not have an 'init' nor 'set' setter",
            category: "EnumClassGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NamespaceNotFound =
        new(
            id: "ENUMCLGEN003",
            title: "Unexpectedly not found a namespace for Enum Class",
            messageFormat: "That's suprising",
            category: "EnumClassGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidModifiers =
        new(
            id: "ENUMCLGEN004",
            title: "Enum Class has to be either public, internal, protected or private",
            messageFormat: "Please correct accessibility modifiers",
            category: "EnumClassGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnexpectedException =
        new(
            id: "ENUMCLGEN005",
            title: "Unhandled exception",
            messageFormat: "Unhandled exception: {0}",
            category: "EnumClassGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
}
