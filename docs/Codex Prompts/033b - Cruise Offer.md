# Codex Prompt 033b – Cruise Offer

## Source Prompt

Implement **Step 3 only** from:

```text
docs/AI Playbook/033 - Cruise Domain Models.md
```

Steps 1 and 2 have already been implemented.

Do not implement Steps 4–7.

---

## Goal

Implement the immutable, provider-independent `CruiseOffer` domain model in `KrytenAssist.Core`.

`CruiseOffer` represents the stable identity and descriptive details of a cruise published by a provider.

It must compose the existing `CruiseProvider` value model and represent optional provider-supplied information honestly.

No prices, snapshots, observations, Skills, web access, parsing, persistence, dependency injection, user interface or tests should be added.

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

Use the existing type:

```text
KrytenAssist.Core/Cruises/CruiseProvider.cs
```

Do not duplicate, replace or redesign `CruiseProvider`.

`CruisePrice` also exists, but it must not be added to `CruiseOffer` in this task. Pricing represents observable offer state and will be composed by `CruiseSnapshot` in Step 4.

---

## Step 3 – Implement CruiseOffer

Create:

```text
KrytenAssist.Core/Cruises/CruiseOffer.cs
```

Use the namespace:

```csharp
KrytenAssist.Core.Cruises
```

Implement `CruiseOffer` as a `sealed record` with these public values:

```csharp
CruiseProvider Provider
string ProviderOfferId
string Title
string ShipName
DateOnly DepartureDate
int DurationNights
string? DeparturePort
string? ItinerarySummary
```

Provide a public constructor accepting parameters in this order:

```csharp
CruiseOffer(
    CruiseProvider provider,
    string providerOfferId,
    string title,
    string shipName,
    DateOnly departureDate,
    int durationNights,
    string? departurePort = null,
    string? itinerarySummary = null)
```

Use explicit constructor validation and get-only properties.

---

## Property Semantics

### Provider

`Provider` identifies the organisation or source publishing the offer.

Requirements:

- reject `null` with `ArgumentNullException`
- retain the supplied `CruiseProvider` instance
- use composition rather than inheritance

Do not create provider-specific subclasses or known-provider constants.

---

### ProviderOfferId

`ProviderOfferId` is the stable identifier assigned to the offer by its provider.

Requirements:

- reject `null`
- reject empty or whitespace-only values
- preserve the supplied value without trimming or case conversion

Do not generate an identifier inside the domain model.

Do not add a Kryten database identifier in this task.

---

### Title

`Title` is the provider-independent display title or name of the cruise.

Requirements:

- reject `null`
- reject empty or whitespace-only values
- preserve the supplied value

Do not format or derive the title from other properties.

---

### ShipName

`ShipName` identifies the ship associated with the offer.

Requirements:

- reject `null`
- reject empty or whitespace-only values
- preserve the supplied value

Do not introduce a separate Ship domain model in this task.

---

### DepartureDate

Use `DateOnly` because a cruise departure is represented as a calendar date in this domain foundation.

Requirements:

- retain the exact caller-supplied value
- do not read the current system clock
- do not reject historical dates based on the current date
- do not perform time-zone conversion

Do not replace `DateOnly` with `DateTime` or `DateTimeOffset`.

---

### DurationNights

`DurationNights` represents the cruise duration as a count of nights.

Requirements:

- require a value greater than zero
- reject zero and negative values with `ArgumentOutOfRangeException`

Do not derive duration from dates.

---

### DeparturePort

`DeparturePort` is optional because not every source is guaranteed to supply it.

Requirements:

- accept `null`
- reject empty or whitespace-only values when supplied
- preserve a valid supplied value

Do not introduce a Port domain model in this task.

---

### ItinerarySummary

`ItinerarySummary` is an optional, source-neutral description of the itinerary or destination.

Requirements:

- accept `null`
- reject empty or whitespace-only values when supplied
- preserve a valid supplied value

Do not parse the summary into ports, countries, regions or itinerary legs.

---

## Construction Style

Use a sealed record with an explicit constructor and get-only properties.

The intended shape is conceptually:

```csharp
public sealed record CruiseOffer
{
    public CruiseOffer(
        CruiseProvider provider,
        string providerOfferId,
        string title,
        string shipName,
        DateOnly departureDate,
        int durationNights,
        string? departurePort = null,
        string? itinerarySummary = null)
    {
        // Validate and assign.
    }

    // Get-only properties.
}
```

This example describes the required public shape. Implement the guards clearly with normal .NET exception types.

Do not introduce:

- a builder
- a factory
- a shared guard utility
- a base class
- an interface for `CruiseOffer`
- mutable setters

Record equality should provide value semantics based on all public values.

---

## Architecture Requirements

`CruiseOffer` must reference only:

- Base Class Library types
- `CruiseProvider` from `KrytenAssist.Core.Cruises`

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

- constructor injection of values
- explicit required and optional values
- standard .NET exception types
- `DateOnly` for the departure date
- composition with `CruiseProvider`

Avoid:

- mutable state
- inheritance
- speculative abstractions
- custom exception types
- implicit conversions
- parsing methods
- formatting methods
- provider-specific rules
- comments that merely repeat the code

Do not rename, move or reformat unrelated files.

---

## Explicitly Out of Scope

Do not implement:

- `CruiseSnapshot`
- `CruiseObservation`
- additional Cruise value models
- prices on `CruiseOffer`
- cabin or occupancy models
- itinerary-leg models
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
- price comparison
- price history
- watch lists
- alerts
- background processing
- dashboards
- Avalonia or React UI
- unit tests
- new test projects
- Steps 4–7 from Prompt 033

---

## Verification

Before building, inspect the final diff and confirm that the only implementation file created or changed by this task is:

```text
KrytenAssist.Core/Cruises/CruiseOffer.cs
```

From the repository root, run:

```bash
dotnet build
```

Do not make unrelated changes merely to remove pre-existing warnings.

The existing SQLite package vulnerability warnings and unrelated Avalonia command-event warnings should be reported but not addressed.

The task is successful when:

- `CruiseOffer.cs` exists in the expected folder
- it uses `KrytenAssist.Core.Cruises`
- it is an immutable sealed record
- it composes `CruiseProvider`
- all required values are guarded
- optional values accept `null` and reject blank supplied values
- duration must be greater than zero
- departure uses `DateOnly`
- no price, snapshot or observation behaviour was added
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

- the `CruiseOffer` contract
- required and optional values
- constructor invariants
- composition with `CruiseProvider`
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

- only Step 3 was implemented
- only `CruiseOffer.cs` was added by this task
- existing foundational Cruise models were not redesigned
- no snapshot or observation model was added
- no Cruise Skill was added
- no web or provider integration was added
- no persistence was added
- no UI changes were made
- no dependency-injection changes were made
- no tests were added or modified
- no NuGet packages or project references were added
- no documentation files were modified

