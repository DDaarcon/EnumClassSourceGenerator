# EnumClasses

EnumClasses is a Roslyn source generator for defining type-safe, class-based enum values while retaining generated lookup, serialization, matching, and optional raw-enum APIs.

The consumer-facing guide—including installation, examples, generated API, configuration, indexing rules, JSON behavior, diagnostics, requirements, and limitations—is maintained in the [NuGet package README](EnumClasses.Package/README.md).

## Repository layout

- `EnumClasses.Package` builds the `DDaarcon.EnumClasses` NuGet package and contains its README.
- `EnumClasses.SourceGenerators` contains the incremental source generator.
- `EnumClasses.SharedSources` contains the public attributes and option types shared by the package and generator.
- `EnumClassSourceGenerator.UnitTests` and `EnumClassSourceGenerator.UnitTestsLite` exercise runtime and generator behavior.
- `EnumClasses.CompatibilityTesting.Net8` verifies the minimum supported consumer target.
- `EnumClassSourceGenerator.Benchmarks` contains lookup benchmarks.

## Build and test

The repository requires a .NET SDK capable of building the solution's .NET 10 test projects.

```console
dotnet build EnumClassSourceGenerator.slnx
dotnet test EnumClassSourceGenerator.slnx
dotnet pack EnumClasses.Package/EnumClasses.Package.csproj
```

The package itself targets .NET 8 and supports C# 12 consumers; see the [package requirements](EnumClasses.Package/README.md#requirements) for details.
