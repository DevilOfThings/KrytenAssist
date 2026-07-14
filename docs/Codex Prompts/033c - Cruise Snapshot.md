# Codex Prompt 033c – Cruise Snapshot

## Source Prompt

Implement **Step 4 only** from:

```text
docs/AI Playbook/033 - Cruise Domain Models.md
```

Steps 1–3 have already been implemented.

Do not implement Steps 5–7.

---

## Goal

Implement the immutable, provider-independent `CruiseSnapshot` domain model in `KrytenAssist.Core`.

`CruiseSnapshot` represents the observable state of a `CruiseOffer` at the point it is captured by a future retrieval service.

It must compose:

- one existing `CruiseOffer`
- one or more existing `CruisePrice` values
- an optional source-neutral promotion summary

The snapshot must protect its price collection from external mutation.

Do not add observation timestamps to the snapshot. `CruiseObservation` will associate a snapshot with a timestamp in Step 5.

No observation model, Skill, web access, parsing, comparison, persistence, dependency injection, user interface or tests should be added.

---

## Allowed Project

Make implementation changes only inside:

```text
KrytenAssist.Core
```

The solution may be built from the repository root for verification.

Do not modify:

```text
KrytenAssist.Application
KrytenAssist.Infrastructure
KrytenAssist.Api
KrytenAssist.Api.Tests
KrytenAssist.Avalonia
KrytenAssist.Avalonia.Tests
KrytenAssist.Client
```

Do not add or modify test projects in this task.

Do not modify:

```text
docs/AI Playbook/033 - Cruise Domain Models.md
docs/Roadmap.md
docs/Backlog.md
docs/Session Handovers
```

Do not modify other Codex prompts or documentation files.

Robin will update project documentation after reviewing the implementation.

---

## Existing Domain Types

Use the existing types:

```text
KrytenAssist.Core/Cruises/CruiseOffer.cs
KrytenAssist.Core/Cruises/CruisePrice.cs
KrytenAssist.Core/Cruises/CruiseProvider.cs
```

Do not duplicate, replace or redesign these models.

`CruiseSnapshot` must compose `CruiseOffer` and `CruisePrice` rather than copying their properties.

---

## Step 4 – Implement CruiseSnapshot

Create:

```text
KrytenAssist.Core/Cruises/CruiseSnapshot.cs
```

Use the namespace:

```csharp
KrytenAssist.Core.Cruises
```

Implement `CruiseSnapshot` as a `sealed record` with these public values:

```csharp
CruiseOffer Offer
IReadOnlyList<CruisePrice> Prices
string? PromotionSummary
```

Provide a public constructor accepting parameters in this order:

```csharp
CruiseSnapshot(
    CruiseOffer offer,
    IEnumerable<CruisePrice> prices,
    string? promotionSummary = null)
```

Use explicit constructor validation and get-only properties.

---

## Property Semantics

### Offer

`Offer` supplies the stable identity and descriptive details associated with the captured state.

Requirements:

- reject `null` with `ArgumentNullException`
- retain the supplied `CruiseOffer` instance
- use composition rather than copying offer properties

Do not modify or enrich the offer inside the snapshot.

---

### Prices

`Prices` contains the currently observed source-neutral price quotes for the offer.

Requirements:

- accept an `IEnumerable<CruisePrice>`
- reject a `null` collection with `ArgumentNullException`
- enumerate the supplied sequence exactly once during construction
- require at least one price
- reject an empty sequence with `ArgumentException`
- reject any `null` element with `ArgumentException`
- preserve the supplied order
- copy the supplied values during construction
- expose the copy as `IReadOnlyList<CruisePrice>`
- prevent later mutation of the source collection from changing the snapshot
- prevent callers from mutating the snapshot's exposed collection

Use a normal BCL read-only collection representation.

Do not expose the internal mutable list as its runtime value.

Do not:

- sort prices
- remove duplicates
- select a preferred price
- calculate lowest or highest prices
- compare currencies
- convert currencies
- format prices
- add cabin-specific interpretation

The snapshot records observed values. Later application logic may interpret them.

---

### PromotionSummary

`PromotionSummary` is an optional source-neutral description of the currently advertised promotion or offer.

Requirements:

- accept `null`
- reject empty or whitespace-only values when supplied
- preserve a valid supplied value without trimming, parsing or case conversion

Do not introduce a promotion hierarchy or provider-specific promotion model.

---

## Time Semantics

Do not add a timestamp to `CruiseSnapshot`.

Step 5 will implement `CruiseObservation`, which owns:

- the observation timestamp
- the association between time and snapshot
- an optional source reference

Keeping time on `CruiseObservation` prevents two competing timestamp concepts and keeps `CruiseSnapshot` focused on captured state.

Do not read the current system clock anywhere in this task.

---

## Collection Immutability

The constructor must materialise the `prices` sequence once and create a read-only copy.

The following mutation must not affect the snapshot:

```csharp
var source = new List<CruisePrice> { firstPrice };
var snapshot = new CruiseSnapshot(offer, source);

source.Add(secondPrice);
```

After the source list changes, `snapshot.Prices` must still contain only `firstPrice`.

The collection exposed by `snapshot.Prices` must not provide a successful mutation path when cast through common collection interfaces.

Do not rely only on the `IReadOnlyList<T>` compile-time interface while storing and exposing the original mutable list.

---

## Construction Style

Use a sealed record with an explicit constructor and get-only properties.

The intended shape is conceptually:

```csharp
public sealed record CruiseSnapshot
{
    public CruiseSnapshot(
        CruiseOffer offer,
        IEnumerable<CruisePrice> prices,
        string? promotionSummary = null)
    {
        // Validate, materialise once, copy and assign.
    }

    public CruiseOffer Offer { get; }

    public IReadOnlyList<CruisePrice> Prices { get; }

    public string? PromotionSummary { get; }
}
```

This example describes the required public shape.

Do not introduce:

- a builder
- a factory
- a shared guard utility
- a base class
- an interface for `CruiseSnapshot`
- mutable setters
- a custom collection type

Record equality should continue to provide value semantics for scalar and composed values. Do not add custom collection equality in this task; collection-content equality policy may be considered later if a real use case requires it.

---

## Architecture Requirements

`CruiseSnapshot` must reference only:

- Base Class Library types
- existing models from `KrytenAssist.Core.Cruises`

It must not reference:

- Avalonia
- OpenAI or another AI provider
- Skills framework types
- application services
- infrastructure services
- Entity Framework
- database attributes
- HTTP clients
- browser types
- provider DTOs or response types

Do not add project references or NuGet packages to `KrytenAssist.Core`.

---

## Design Constraints

Keep the implementation:

- small
- immutable
- provider independent
- UI independent
- persistence independent
- deterministic
- independently testable by a later prompt

Prefer:

- composition
- constructor validation
- standard .NET exception types
- a defensive collection copy
- a BCL read-only collection wrapper

Avoid:

- mutable state
- inheritance
- speculative abstractions
- custom exception types
- implicit conversions
- comparison logic
- formatting logic
- provider-specific rules
- comments that merely repeat the code

Do not rename, move or reformat unrelated files.

---

## Explicitly Out of Scope

Do not implement:

- `CruiseObservation`
- additional Cruise models
- observation timestamps
- source references
- snapshot comparison
- change detection
- deduplication
- price trends
- lowest or highest price calculations
- currency conversion
- cabin or occupancy models
- a Cruise Skill
- `ISkill` integration
- Cruise of the Week
- Marella integration
- web requests
- HTTP clients
- HTML parsing
- JSON serialization configuration
- provider DTOs
- repositories
- Entity Framework configuration
- migrations
- local storage
- dependency-injection registration
- watch lists
- alerts
- background processing
- dashboards
- Avalonia or React UI
- unit tests
- new test projects
- Steps 5–7 from Prompt 033

---

## Verification

Before building, inspect the final diff and confirm that the only implementation file created or changed by this task is:

```text
KrytenAssist.Core/Cruises/CruiseSnapshot.cs
```

From the repository root, run:

```bash
dotnet build
```

Do not make unrelated changes merely to remove pre-existing warnings.

The existing SQLite package vulnerability warnings and unrelated Avalonia command-event warnings should be reported but not addressed.

The task is successful when:

- `CruiseSnapshot.cs` exists in the expected folder
- it uses `KrytenAssist.Core.Cruises`
- it is an immutable sealed record
- it composes the existing `CruiseOffer` and `CruisePrice` models
- it requires at least one non-null price
- price order is preserved
- the source sequence is enumerated once
- the source collection cannot mutate the snapshot
- the exposed price collection cannot be mutated successfully
- optional promotion text accepts `null` and rejects blank supplied values
- no timestamp or observation behaviour was added
- no packages or project references were added
- the solution builds successfully

---

## Completion Report

After implementation, report:

### Files Created

List every file created.

### Files Modified

List every existing file modified.

If none were modified, state:

```text
None
```

### Implementation Summary

Briefly describe:

- the `CruiseSnapshot` contract
- composition with `CruiseOffer` and `CruisePrice`
- price collection validation
- defensive copying and immutability
- optional promotion handling

### Build

Report:

- command executed
- success or failure
- warning count
- error count

Distinguish pre-existing warnings from warnings introduced by this task.

### Tests

Confirm that no tests were added or modified and that test coverage is reserved for Prompt 033 Step 6.

### Scope Check

Confirm that:

- only Step 4 was implemented
- only `CruiseSnapshot.cs` was added by this task
- existing Cruise models were not redesigned
- no observation model or timestamp was added
- no Cruise Skill was added
- no web or provider integration was added
- no comparison or persistence behaviour was added
- no UI changes were made
- no dependency-injection changes were made
- no tests were added or modified
- no NuGet packages or project references were added
- no documentation files were modified

