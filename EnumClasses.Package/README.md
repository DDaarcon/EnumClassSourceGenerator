# DDaarcon.EnumClasses

`DDaarcon.EnumClasses` generates class-based enum types at compile time. Values can carry data, use derived types, participate in exhaustive-looking match helpers, serialize by stable names, and still be discovered and indexed without reflection.

> This package is currently a preview. Review generated-source and persistence implications before using it as a long-lived public contract.

## Installation

Install the package from NuGet:

```console
dotnet add package DDaarcon.EnumClasses --version 0.1.0-preview.1
```

or add a package reference:

```xml
<PackageReference Include="DDaarcon.EnumClasses" Version="0.1.0-preview.1" />
```

The package supplies both the public attributes and the analyzer/source generator; no separate analyzer reference is required.

## Minimal example

```csharp
using EnumClasses;
using System.Text.Json;

namespace Shipping;

[EnumClass]
public partial class OrderStatus
{
    public static readonly OrderStatus Pending = new();

    [EnumClassValue(10, "in-transit")]
    public static readonly OrderStatus InTransit = new();

    public static readonly OrderStatus Delivered = new();
}
```

The generator completes the partial class:

```csharp
OrderStatus.Pending.EnumIndex;                 // 0
OrderStatus.InTransit.EnumIndex;               // 10
OrderStatus.Delivered.EnumIndex;               // 11

OrderStatus.AllValues;                         // declaration order
OrderStatus.Deserialize("in-transit");        // OrderStatus.InTransit
OrderStatus.InTransit.Serialize();             // "in-transit"
OrderStatus.GetByEnumIndex(11);                 // OrderStatus.Delivered

JsonSerializer.Serialize(OrderStatus.Pending); // "Pending"
```

An eligible value is an initialized `public static readonly` field or an initialized `public static` get-only auto-property. Its declared type may be the enum class or a derived type. Add `[EnumClassIgnore]` to exclude an otherwise eligible member.

## Generated API

Every valid enum class implements `IEquatable<T>` using reference identity and receives:

- `ImmutableArray<T> AllValues` in source declaration order.
- `int EnumIndex`, plus `GetByEnumIndex`, `TryGetByEnumIndex`, and `ContainsEnumIndex`.
- `Serialize`, `Deserialize`, `TryDeserialize`, `ContainsSerializedValue`, and `ToString`.
- `Switch`/`SwitchEx` and `Match`/`MatchEx` overloads, with optional caller state. The `Ex` variants throw when invoked for an instance that is not one of the declared values; the non-`Ex` variants do nothing or return `null`.
- `TryGetOfType<TValue>` when at least one declared value uses a derived type.
- Reference-based `Equals` and `GetHashCode` implementations.

Optional features add:

- A nested `JsonConverter` registered on the class when `GenerateJsonConverter` is enabled.
- A nested `Raw` enum, conversion methods, and conversion operators when `GenerateRawEnum` is enabled.

Generated method parameters correspond to the value member names. Adding, removing, or renaming values can therefore change the signatures of generated `Switch` and `Match` methods.

## Configuration

Options may be set per class on `[EnumClass(...)]` or assembly-wide with `[assembly: EnumClassDefaults(...)]`. A class option overrides the assembly default.

```csharp
using EnumClasses;

[assembly: EnumClassDefaults(
    GenerateJsonConverter = true,
    GenerateRawEnum = false,
    LookupMode = LookupMode.Automatic,
    ConstructionRestrictionMode = ConstructionRestrictionMode.WithProtectedDefaultConstructor,
    IndexAssignmentMode = IndexAssignmentMode.CompileTime,
    DocumentationMode = DocumentationMode.ListCompileTimeIndexes)]
```

| Option | Default | Behavior |
| --- | --- | --- |
| `GenerateJsonConverter` | `true` | Generates and registers a `System.Text.Json` converter. |
| `GenerateRawEnum` | `false` | Generates the nested `Raw` enum and conversions. |
| `LookupMode` | `Automatic` | Chooses lookup code automatically, or forces an if-chain or dictionary with `ForceIfChain` / `ForceDictionary`. This is an implementation choice, not a semantic one. |
| `ConstructionRestrictionMode` | `WithProtectedDefaultConstructor` | Requires user constructors to be private or protected and generates a protected parameterless constructor if none exists. `WithPrivateDefaultConstructor` generates a private constructor; `Off` performs no constructor validation or generation. |
| `IndexAssignmentMode` | `CompileTime` | Selects compile-time or runtime index assignment as described below. |
| `DocumentationMode` | `ListCompileTimeIndexes` | Adds resolved compile-time indexes to generated XML documentation. `Disabled` omits that extra documentation. |

`[EnumClassValue]` marks a value explicitly and supports `SerializedName`. In compile-time index mode its constructors also accept an index. `[EnumClassIgnore]` excludes a member.

## Index semantics

### Compile-time indexes

`IndexAssignmentMode.CompileTime` assigns indexes in declaration order, starting at `0`. An explicit `[EnumClassValue(index)]` sets that value's index and restarts the sequence, so the following implicit value receives `index + 1`.

```csharp
[EnumClass]
public partial class Priority
{
    public static readonly Priority Low = new();       // 0

    [EnumClassValue(20)]
    public static readonly Priority High = new();      // 20

    public static readonly Priority Urgent = new();    // 21
}
```

Indexes must be unique. Negative values and the full `int` range are accepted, but automatic numbering cannot advance beyond `int.MaxValue`; a following value then collides and produces `ENUMCLGEN012`. Treat persisted indexes as an explicit compatibility contract: inserting or reordering implicitly indexed members can change them.

### Runtime indexes

Runtime modes take each value's index from its initializer or constructor and validate the complete declared set during type initialization:

```csharp
[EnumClass(IndexAssignmentMode = IndexAssignmentMode.RuntimeAtValueInit)]
public partial class HttpStatus
{
    public static readonly HttpStatus Ok = new() { EnumIndex = 200 };
    public static readonly HttpStatus NotFound = new() { EnumIndex = 404 };
}
```

- `RuntimeAtValueInit` generates `required int EnumIndex { get; init; }`.
- `RuntimeAtValueCreation` generates the same init-only assignment mechanism without `required`, allowing a constructor to enforce assignment instead.
- Every declared value must actually assign an index, and indexes must be unique. Missing or duplicate assignments throw `InvalidOperationException` from the generated type initializer (observed by callers as `TypeInitializationException`).
- `[EnumClassValue(index)]` produces warning `ENUMCLGEN013` in either runtime mode, and its index is ignored. The value assigned to the generated `EnumIndex` property by the initializer or constructor remains authoritative. Use parameterless `[EnumClassValue]` when only `SerializedName` or explicit marking is needed.

## Serialization and JSON

The serialized value defaults to the source member name and is case-sensitive. Override it without changing the C# member name:

```csharp
[EnumClassValue(SerializedName = "awaiting-payment")]
public static readonly OrderStatus AwaitingPayment = new();
```

`Serialize()` and `ToString()` return that name. `Deserialize` returns the matching declared instance or `null`; `TryDeserialize` and `ContainsSerializedValue` provide non-null-check alternatives. Duplicate serialized names are not supported and may fail during generated lookup initialization.

When enabled, the generated `System.Text.Json` converter writes a JSON string and restores the declared singleton instance. Unknown names and non-string tokens cause `JsonException`. Serialized names are therefore part of the wire contract and should be changed deliberately.

## Raw enum behavior

With `GenerateRawEnum = true`, the generator adds a conventional nested `Raw` enum and the following APIs:

- `FromRaw` / `TryFromRaw`
- instance and static `ToRaw` / `TryToRaw`
- implicit conversion from `Raw` to the enum class
- explicit conversion from the enum class to `Raw`

Raw numbering depends on the index-assignment mode:

- With `CompileTime`, each raw member uses the same resolved numeric value as `EnumIndex`, including explicit indexes and restarted sequences.
- With either runtime mode, raw members are numbered `0, 1, 2, ...` in source declaration order because runtime `EnumIndex` values are unavailable to the generator.

The distinction in runtime mode is intentional:

```csharp
HttpStatus.Ok.EnumIndex;          // 200
(int)HttpStatus.Raw.Ok;           // 0
HttpStatus.NotFound.EnumIndex;    // 404
(int)HttpStatus.Raw.NotFound;     // 1
```

Runtime indexing combined with raw generation produces warning `ENUMCLGEN015` to make this independent numbering explicit. Raw conversion maps declared instances by identity; invalid raw values or undeclared instances make the throwing conversion APIs throw `ArgumentOutOfRangeException`.

## Diagnostics

| ID | Severity | Meaning |
| --- | --- | --- |
| `ENUMCLGEN001` | Error | A candidate value's type is not assignable to the enum class. |
| `ENUMCLGEN002` | Error | A candidate property is not an initialized get-only auto-property. |
| `ENUMCLGEN003` | Error | The enum class is in the global namespace. |
| `ENUMCLGEN004` | Error | The enum class has no explicit accessibility. |
| `ENUMCLGEN005` | Error | Generation failed with an unexpected exception. |
| `ENUMCLGEN006` | Error | Nested enum classes are unsupported. |
| `ENUMCLGEN007` | Error | Generic enum classes are unsupported. |
| `ENUMCLGEN008` | Error | A value name collides with a generated member name. |
| `ENUMCLGEN009` | Error | A constructor other than private or protected is used while constructor restrictions are enabled. |
| `ENUMCLGEN010` | Error | A candidate value field has no initializer. |
| `ENUMCLGEN011` | Error | Values are spread across multiple partial declarations. |
| `ENUMCLGEN012` | Error | Two compile-time indexes collide. |
| `ENUMCLGEN013` | Warning | An ignored attribute index is specified while runtime index assignment is active. |
| `ENUMCLGEN014` | Error | One explicit index attribute is shared by multiple comma-separated fields. |
| `ENUMCLGEN015` | Warning | Runtime indexes and generated raw-enum numeric values are independent. |
| `ENUMCLGEN016` | Error | An unsupported `IndexAssignmentMode` numeric value was configured. |

Generator errors intentionally prevent the affected values from participating in the generated contract; do not suppress them as a compatibility strategy.

## Requirements

- Consumer target: .NET 8 or later. The package targets `net8.0` and generated code uses .NET 8 APIs including frozen collections.
- C# language version: C# 12 or later. Runtime-at-init indexing emits `required` and all modes emit modern collection syntax.
- `System.Text.Json` is required when JSON converter generation is enabled (the default).
- .NET 9 / C# 13 consumers may receive more efficient alternate-key dictionary lookups; behavior is unchanged on .NET 8 / C# 12.

## Limitations and compatibility notes

- The target must be a `partial`, non-generic, non-nested class in a named namespace and must declare its accessibility explicitly.
- All generated values must be declared in one partial declaration, even when the class has other partial declarations.
- Eligible fields must be initialized `public static readonly`; eligible properties must be initialized `public static` get-only auto-properties.
- Value and serialized names must be unique in practice. Names reserved by the generated API are rejected.
- Equality is singleton/reference identity, not structural equality. `ConstructionRestrictionMode.Off`, derived types, and accessible protected constructors can permit undeclared instances; lookup and exhaustive helpers only know the declared static values.
- Declaration order affects `AllValues`, implicit compile-time indexes, raw-enum numbers, and generated matching method signatures.
- Runtime index validity is checked when the generated type initializes, not at source-generation time.
- The package is in preview; generated API shape and defaults may change before a stable release.

Source and issue tracking: [github.com/DDaarcon/EnumClassSourceGenerator](https://github.com/DDaarcon/EnumClassSourceGenerator)
