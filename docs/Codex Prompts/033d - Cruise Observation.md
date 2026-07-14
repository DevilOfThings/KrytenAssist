# Codex Prompt 033d – Cruise Observation

## Source Prompt

Implement **Step 5 only** from:

```text
docs/AI Playbook/033 - Cruise Domain Models.md
```

Steps 1–4 have already been implemented.

Do not implement Steps 6 or 7.

---

## Goal

Implement the immutable, provider-independent `CruiseObservation` domain model in `KrytenAssist.Core`.

`CruiseObservation` represents the fact that Kryten observed a particular `CruiseSnapshot` at a caller-supplied point in time.

It must compose:

- one existing `CruiseSnapshot`
- one explicit `DateTimeOffset` observation timestamp
- one optional transport-neutral source reference

The model must not read the system clock, retrieve data, persist itself, compare snapshots or contain scheduling state.

No tests, Skill, web access, parsing, persistence, dependency injection or user interface should be added.

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
KrytenAssist.Core/Cruises/CruiseSnapshot.cs
```

Do not duplicate, replace or redesign these models.

`CruiseObservation` must compose `CruiseSnapshot` rather than copying offer, provider or price properties.

---

## Step 5 – Implement CruiseObservation

Create:

```text
KrytenAssist.Core/Cruises/CruiseObservation.cs
```

Use the namespace:

```csharp
KrytenAssist.Core.Cruises
```

Implement `CruiseObservation` as a `sealed record` with these public values:

```csharp
CruiseSnapshot Snapshot
DateTimeOffset ObservedAt
string? SourceReference
```

Provide a public constructor accepting parameters in this order:

```csharp
CruiseObservation(
    CruiseSnapshot snapshot,
    DateTimeOffset observedAt,
    string? sourceReference = null)
```

Use explicit constructor validation and get-only properties.

---

## Property Semantics

### Snapshot

`Snapshot` contains the captured state associated with the observation.

Requirements:

- reject `null` with `ArgumentNullException`
- retain the supplied `CruiseSnapshot` instance
- use composition rather than copying nested values

Do not modify, enrich or compare the snapshot during construction.

---

### ObservedAt

`ObservedAt` records when the caller observed the snapshot.

Requirements:

- use `DateTimeOffset`
- require the caller to supply the value
- preserve the exact supplied date, time and offset
- do not replace the supplied value with UTC
- do not convert the offset
- do not read `DateTimeOffset.Now`, `DateTimeOffset.UtcNow`, `DateTime.Now` or another system clock
- do not reject historical or future timestamps based on the current time

No default timestamp parameter should be provided.

The application clock abstraction may be used by a future retrieval service when it creates an observation. It does not belong inside this domain record.

---

### SourceReference

`SourceReference` optionally records where the observation originated.

It may later contain a provider reference, external identifier or textual URL, but the Core domain must not interpret it as a transport object.

Requirements:

- accept `null`
- reject empty or whitespace-only values when supplied
- preserve a valid supplied value without trimming, parsing, normalization or case conversion
- expose the value as `string?`

Do not:

- require a valid URI
- use `HttpRequestMessage`, browser or SDK types
- perform DNS, HTTP or file access
- add provider-specific source-reference classes

Keeping this property transport-neutral allows later infrastructure to map its source information without coupling Core to a retrieval mechanism.

---

## Responsibility Boundary

`CruiseObservation` is a domain record only.

It answers:

> Which snapshot was observed, when was it observed, and what optional source reference accompanied it?

It must not answer:

- whether the snapshot differs from a previous snapshot
- whether a price changed
- whether the observation should be stored
- whether an alert should be raised
- when another observation should run
- whether retrieval succeeded or failed

Those behaviours belong to later application services and Skills.

Do not add:

- database identifiers
- created or updated timestamps
- retry counters
- job identifiers
- change flags
- comparison results
- persistence status
- error state

---

## Construction Style

Use a sealed record with an explicit constructor and get-only properties.

The intended shape is conceptually:

```csharp
public sealed record CruiseObservation
{
    public CruiseObservation(
        CruiseSnapshot snapshot,
        DateTimeOffset observedAt,
        string? sourceReference = null)
    {
        // Validate and assign.
    }

    public CruiseSnapshot Snapshot { get; }

    public DateTimeOffset ObservedAt { get; }

    public string? SourceReference { get; }
}
```

This example describes the required public shape.

Do not introduce:

- a builder
- a factory
- a shared guard utility
- a base class
- an interface for `CruiseObservation`
- mutable setters
- static creation methods that read the clock

Record equality should provide value semantics based on `Snapshot`, `ObservedAt` and `SourceReference`.

---

## Architecture Requirements

`CruiseObservation` must reference only:

- Base Class Library types
- `CruiseSnapshot` from `KrytenAssist.Core.Cruises`

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
- independently testable by Step 6

Prefer:

- composition
- constructor validation
- standard .NET exception types
- caller-supplied time
- a transport-neutral source reference

Avoid:

- mutable state
- inheritance
- speculative abstractions
- custom exception types
- implicit conversions
- clock access
- comparison logic
- persistence logic
- provider-specific rules
- comments that merely repeat the code

Do not rename, move or reformat unrelated files.

---

## Explicitly Out of Scope

Do not implement:

- additional Cruise models
- snapshot comparison
- observation comparison
- change detection
- deduplication
- price trends
- lowest or highest price calculations
- currency conversion
- persistence
- repositories
- Entity Framework configuration
- migrations
- local storage
- scheduling
- retry logic
- background processing
- notifications
- watch lists
- alerts
- a Cruise Skill
- `ISkill` integration
- Cruise of the Week
- Marella integration
- web requests
- HTTP clients
- HTML parsing
- JSON serialization configuration
- provider DTOs
- dependency-injection registration
- dashboards
- Avalonia or React UI
- unit tests
- new test projects
- Steps 6 or 7 from Prompt 033

---

## Verification

Before building, inspect the final diff and confirm that the only implementation file created or changed by this task is:

```text
KrytenAssist.Core/Cruises/CruiseObservation.cs
```

From the repository root, run:

```bash
dotnet build
```

Do not make unrelated changes merely to remove pre-existing warnings.

The existing SQLite package vulnerability warnings and unrelated Avalonia command-event warnings should be reported but not addressed.

The task is successful when:

- `CruiseObservation.cs` exists in the expected folder
- it uses `KrytenAssist.Core.Cruises`
- it is an immutable sealed record
- it composes the existing `CruiseSnapshot`
- it requires an explicit `DateTimeOffset`
- it preserves the exact supplied timestamp and offset
- it does not access the system clock
- optional source reference accepts `null` and rejects blank supplied values
- it contains no comparison, persistence or scheduling behaviour
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

- the `CruiseObservation` contract
- composition with `CruiseSnapshot`
- explicit timestamp semantics
- optional source-reference handling
- immutability and value semantics

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

- only Step 5 was implemented
- only `CruiseObservation.cs` was added by this task
- existing Cruise models were not redesigned
- no additional domain models were added
- no system clock access was added
- no Cruise Skill was added
- no web or provider integration was added
- no comparison, persistence or scheduling behaviour was added
- no UI changes were made
- no dependency-injection changes were made
- no tests were added or modified
- no NuGet packages or project references were added
- no documentation files were modified

