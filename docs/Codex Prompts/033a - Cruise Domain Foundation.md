# Codex Prompt 033a – Cruise Domain Foundation

## Source Prompt

Implement **Steps 1 and 2 only** from:

```text
docs/AI Playbook/033 - Cruise Domain Models.md
```

Do not implement Steps 3–7.

---

## Goal

Create the initial provider-independent Cruise domain foundation in `KrytenAssist.Core`.

This task introduces:

- the shared Cruise domain folder and namespace
- `CruiseProvider`
- `CruisePrice`

Both models must be immutable value models with small, local domain invariants.

No cruise offer, snapshot, observation, Skill, web access, parsing, persistence, dependency injection, user interface or tests should be added.

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

## Step 1 – Cruise Domain Structure

Create this structure inside `KrytenAssist.Core`:

```text
Cruises/
├── CruisePrice.cs
└── CruiseProvider.cs
```

Use the namespace:

```csharp
KrytenAssist.Core.Cruises
```

Do not create empty placeholder files for later models.

Do not move the existing `PromptCard` entity.

---

## Step 2 – Foundational Value Models

### CruiseProvider

Create:

```text
KrytenAssist.Core/Cruises/CruiseProvider.cs
```

Implement `CruiseProvider` as a `sealed record` with these public values:

```csharp
string Id
string Name
```

Provide a public constructor accepting:

```csharp
CruiseProvider(string id, string name)
```

The constructor must enforce:

- `id` is not null, empty or whitespace-only
- `name` is not null, empty or whitespace-only

Throw `ArgumentException` for an empty or whitespace-only value and an appropriate null argument exception for `null`.

Store the supplied values without provider-specific interpretation.

Do not:

- use an enum for providers
- create known-provider constants
- create a provider registry
- introduce provider subclasses
- introduce Marella-specific behaviour
- perform case conversion
- perform external lookup

Record equality should provide value semantics based on `Id` and `Name`.

---

### CruisePrice

Create:

```text
KrytenAssist.Core/Cruises/CruisePrice.cs
```

Implement `CruisePrice` as a `sealed record` with these public values:

```csharp
decimal Amount
string Currency
string? Basis
```

Provide a public constructor accepting:

```csharp
CruisePrice(decimal amount, string currency, string? basis = null)
```

#### Amount

The constructor must:

- accept zero
- accept positive decimal values
- reject negative values with `ArgumentOutOfRangeException`

Do not round the amount.

Do not use `double` or `float`.

#### Currency

The constructor must:

- reject `null`
- require exactly three alphabetic characters
- reject empty, whitespace-only, numeric or incorrectly sized codes
- store the accepted code in uppercase using invariant casing

Examples:

```text
gbp -> GBP
GBP -> GBP
usd -> USD
```

This is structural ISO-style validation only.

Do not add a currency catalogue or verify that the code belongs to a list of known currencies.

#### Basis

`Basis` describes the meaning of the quoted amount when the source supplies one.

Examples might include:

```text
per person
total
from
```

The constructor must:

- accept `null`
- reject an empty or whitespace-only value when a value is supplied
- preserve a valid supplied value without interpretation or case conversion

Do not create a price-basis enum in this task.

Record equality should provide value semantics based on `Amount`, `Currency` and `Basis`.

---

## Construction Style

Use explicit constructors and get-only properties so validation occurs at construction time.

The intended shape is conceptually:

```csharp
public sealed record CruiseProvider
{
    public CruiseProvider(string id, string name)
    {
        // Validate and assign.
    }

    public string Id { get; }

    public string Name { get; }
}
```

and equivalently for `CruisePrice`.

This example describes the required public shape. Implement validation clearly using normal .NET guard clauses.

Do not introduce a shared guard utility, base record, factory or builder.

---

## Architecture Requirements

The new models must reference only Base Class Library types.

They must not reference:

- Avalonia
- OpenAI or another AI provider
- Skills framework types
- application services
- infrastructure services
- Entity Framework
- database attributes
- HTTP clients
- browser types
- provider response types

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

- meaningful property names
- constructor validation
- standard .NET exception types
- `decimal` for money
- invariant culture for currency casing

Avoid:

- mutable setters
- static mutable state
- inheritance
- interfaces for value models
- speculative abstractions
- custom exception types
- implicit conversion operators
- parsing or formatting methods
- comments that merely repeat the code

Do not rename, move or reformat unrelated files.

---

## Explicitly Out of Scope

Do not implement:

- `CruiseOffer`
- `CruiseSnapshot`
- `CruiseObservation`
- any additional Cruise model
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
- price formatting
- currency conversion
- cabin availability
- watch lists
- alerts
- background processing
- dashboards
- Avalonia or React UI
- unit tests
- new test projects
- Steps 3–7 from Prompt 033

---

## Verification

Before building, inspect the final diff and confirm that only the two required Core files were created.

From the repository root, run:

```bash
dotnet build
```

Do not make unrelated changes merely to remove pre-existing warnings.

The existing SQLite package vulnerability warnings should be reported but not addressed in this task.

The task is successful when:

- `KrytenAssist.Core/Cruises/CruiseProvider.cs` exists
- `KrytenAssist.Core/Cruises/CruisePrice.cs` exists
- both use `KrytenAssist.Core.Cruises`
- both are immutable sealed records
- constructor invariants are enforced
- `CruisePrice` uses `decimal`
- valid currency codes are stored in invariant uppercase
- no later Cruise models or behaviours were added
- no project references or NuGet packages were added
- the solution builds successfully

Do not run provider, web or UI verification.

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

- `CruiseProvider`
- `CruisePrice`
- their invariants
- their value and immutability semantics

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

- only Steps 1 and 2 were implemented
- only `KrytenAssist.Core` was changed
- no later Cruise models were added
- no Cruise Skill was added
- no web or provider integration was added
- no persistence was added
- no UI changes were made
- no dependency-injection changes were made
- no tests were added or modified
- no NuGet packages or project references were added
- no documentation files were modified

