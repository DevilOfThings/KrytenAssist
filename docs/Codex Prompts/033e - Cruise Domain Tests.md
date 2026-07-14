# Codex Prompt 033e – Cruise Domain Tests

## Source Prompt

Implement **Step 6 only** from:

```text
docs/AI Playbook/033 - Cruise Domain Models.md
```

Steps 1–5 have already been implemented.

Do not implement Step 7.

---

## Goal

Add comprehensive, deterministic unit tests for the shared Cruise domain models.

Create a dedicated Core test project so the domain can be tested directly without depending on API, Avalonia, application or infrastructure projects.

The tests must cover:

- successful model construction
- required-field guards
- optional-field behaviour
- money and currency invariants
- duration invariants
- immutable composition
- defensive collection copying
- collection ordering
- exact timestamp preservation
- value equality where it is an intentional record contract

Do not add new production behaviour unless a test exposes a genuine defect against Prompt 033.

---

## Allowed Changes

Create and modify only:

```text
KrytenAssist.Core.Tests/
KrytenAssist.sln
```

Production files inside:

```text
KrytenAssist.Core/Cruises/
```

may be modified only if a required test exposes a genuine defect against the existing Prompt 033 contract.

Any production correction must:

- be minimal
- fix only the failing requirement
- preserve the existing public model contracts
- include a focused test demonstrating the defect
- be described explicitly in the completion report

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

Do not modify documentation files, including:

```text
docs/AI Playbook/033 - Cruise Domain Models.md
docs/Roadmap.md
docs/Backlog.md
docs/Session Handovers
docs/Codex Prompts
```

Robin will update documentation after reviewing the implementation.

---

## Test Project

Create:

```text
KrytenAssist.Core.Tests/KrytenAssist.Core.Tests.csproj
```

Use the existing solution's test conventions and package versions:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.4" />
    <PackageReference Include="FluentAssertions" Version="8.10.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\KrytenAssist.Core\KrytenAssist.Core.csproj" />
  </ItemGroup>

</Project>
```

Do not add any other package or project reference.

Add `KrytenAssist.Core.Tests` to `KrytenAssist.sln` using the normal .NET solution command so all required project configuration entries are generated correctly.

The test project must reference only `KrytenAssist.Core`.

---

## Expected Test Structure

Create:

```text
KrytenAssist.Core.Tests/
└── Cruises/
    ├── CruiseObservationTests.cs
    ├── CruiseOfferTests.cs
    ├── CruisePriceTests.cs
    ├── CruiseProviderTests.cs
    └── CruiseSnapshotTests.cs
```

Use the namespace:

```csharp
KrytenAssist.Core.Tests.Cruises
```

Do not create one large combined test file.

Do not add generated placeholder tests such as `UnitTest1.cs`.

---

## Existing Production Models

Exercise the existing implementations directly:

```text
KrytenAssist.Core/Cruises/CruiseProvider.cs
KrytenAssist.Core/Cruises/CruisePrice.cs
KrytenAssist.Core/Cruises/CruiseOffer.cs
KrytenAssist.Core/Cruises/CruiseSnapshot.cs
KrytenAssist.Core/Cruises/CruiseObservation.cs
```

Use the current public constructors and properties.

Do not duplicate production validation logic inside test helper classes.

---

## Testing Conventions

Use:

- xUnit
- FluentAssertions
- clear Arrange, Act and Assert sections
- descriptive method names
- fixed deterministic test data

Prefer:

- one behaviour per test
- small private factory methods only where repeated setup would obscure the test
- `Action` plus FluentAssertions for synchronous exceptions
- reference assertions where composition requires retaining the supplied instance

Do not use:

- mocks for simple domain values
- shared mutable fixtures
- test ordering
- sleeps
- retries
- the current system clock
- network access
- file-system access
- dependency injection
- application startup
- UI startup

When checking exceptions, verify the exception type and parameter name where practical.

---

# CruiseProviderTests

Create:

```text
KrytenAssist.Core.Tests/Cruises/CruiseProviderTests.cs
```

Verify:

### Valid construction

- `Id` is retained exactly
- `Name` is retained exactly
- no trimming or case conversion occurs

### Required values

- null `id` throws `ArgumentNullException`
- empty `id` throws `ArgumentException`
- whitespace-only `id` throws `ArgumentException`
- null `name` throws `ArgumentNullException`
- empty `name` throws `ArgumentException`
- whitespace-only `name` throws `ArgumentException`

### Value equality

- two providers with equal `Id` and `Name` are equal
- a changed `Id` or `Name` produces inequality

Do not test provider catalogues or known-provider constants; none should exist.

---

# CruisePriceTests

Create:

```text
KrytenAssist.Core.Tests/Cruises/CruisePriceTests.cs
```

Verify:

### Valid construction

- zero amount is accepted
- positive decimal amount is retained without rounding
- uppercase currency is retained
- lowercase currency is normalized using invariant uppercase
- `null` basis is accepted
- a valid basis is retained exactly

### Amount validation

- a negative amount throws `ArgumentOutOfRangeException`

### Currency validation

- null currency throws `ArgumentNullException`
- empty currency throws `ArgumentException`
- whitespace-only currency throws `ArgumentException`
- codes shorter than three characters throw `ArgumentException`
- codes longer than three characters throw `ArgumentException`
- numeric characters throw `ArgumentException`
- punctuation throws `ArgumentException`
- non-ASCII alphabetic characters throw `ArgumentException`

### Basis validation

- empty basis throws `ArgumentException`
- whitespace-only basis throws `ArgumentException`

### Value equality

- equal normalized values produce equal records
- a changed amount, currency or basis produces inequality

Do not test currency conversion, formatting or known-currency lookup.

---

# CruiseOfferTests

Create:

```text
KrytenAssist.Core.Tests/Cruises/CruiseOfferTests.cs
```

Verify:

### Valid construction

- all required values are retained
- the supplied `CruiseProvider` instance is retained
- `DateOnly` departure is retained exactly
- positive duration is retained
- both optional values may be `null`
- valid departure port is retained exactly
- valid itinerary summary is retained exactly

### Required-value validation

- null provider throws `ArgumentNullException`
- null, empty and whitespace-only provider-offer identifiers are rejected with the appropriate argument exception
- null, empty and whitespace-only titles are rejected
- null, empty and whitespace-only ship names are rejected

### Duration validation

- zero duration throws `ArgumentOutOfRangeException`
- negative duration throws `ArgumentOutOfRangeException`

### Optional-value validation

- empty departure port throws `ArgumentException`
- whitespace-only departure port throws `ArgumentException`
- empty itinerary summary throws `ArgumentException`
- whitespace-only itinerary summary throws `ArgumentException`

### Value equality

- offers with equal composed values are equal
- changing an intentional value produces inequality

Do not test pricing, parsing, ships, ports or itinerary legs as separate models.

---

# CruiseSnapshotTests

Create:

```text
KrytenAssist.Core.Tests/Cruises/CruiseSnapshotTests.cs
```

Verify:

### Valid construction and composition

- the supplied `CruiseOffer` instance is retained
- one price is accepted
- multiple prices preserve their supplied order
- `null` promotion summary is accepted
- a valid promotion summary is retained exactly

### Required-value validation

- null offer throws `ArgumentNullException`
- null prices collection throws `ArgumentNullException`
- empty prices sequence throws `ArgumentException`
- a sequence containing a null price throws `ArgumentException`

Use `null!` only at the test boundary required to exercise runtime guards.

### Collection behaviour

- mutating the source list after construction does not change `Prices`
- the exposed collection cannot be mutated successfully through `IList<CruisePrice>` or another applicable mutable collection interface
- a custom single-use enumerable confirms the source sequence is enumerated exactly once

The single-use enumerable may be a small private test type inside `CruiseSnapshotTests`. It should throw if a second enumeration is attempted.

Do not add a production test helper.

### Promotion validation

- empty promotion summary throws `ArgumentException`
- whitespace-only promotion summary throws `ArgumentException`

Do not assert structural collection-content equality between two different snapshots. Prompt 033 explicitly does not define custom collection equality semantics.

---

# CruiseObservationTests

Create:

```text
KrytenAssist.Core.Tests/Cruises/CruiseObservationTests.cs
```

Verify:

### Valid construction and composition

- the supplied `CruiseSnapshot` instance is retained
- the exact caller-supplied `DateTimeOffset` is retained
- a non-zero offset is preserved without UTC conversion
- `null` source reference is accepted
- a valid source reference is retained exactly
- a source reference is not required to be a valid URI

Use a fixed timestamp, for example:

```csharp
new DateTimeOffset(2026, 7, 14, 10, 30, 0, TimeSpan.FromHours(1))
```

Do not use the current system clock.

### Required-value validation

- null snapshot throws `ArgumentNullException`

### Source-reference validation

- empty source reference throws `ArgumentException`
- whitespace-only source reference throws `ArgumentException`

### Value equality

- observations with equal values are equal when they share equivalent composed values
- changing the timestamp or source reference produces inequality

Do not test persistence, comparison, scheduling or change detection.

---

## Test Data

Use clearly fictional deterministic data.

Suggested examples:

```text
Provider Id: sample.cruises
Provider Name: Sample Cruises
Offer Id: sample-offer-001
Title: Mediterranean Escape
Ship: Sample Voyager
Departure: 14 July 2027
Duration: 7 nights
Departure Port: Palma
Itinerary: Western Mediterranean
Price: 799.50 GBP per person
Promotion: Summer saving
Source Reference: sample://cruises/offer-001
```

Do not introduce Marella-specific expectations into the shared domain tests.

---

## Production Changes

Do not change production code merely to satisfy a preferred test style.

A production change is permitted only when:

- the implementation violates Prompt 033
- a required test demonstrates the defect
- the correction is minimal and directly related

Do not change:

- public constructor signatures
- public property names or types
- required/optional semantics
- currency normalization
- collection immutability policy
- timestamp ownership

without reporting the conflict before proceeding.

---

## Design Constraints

The tests must remain:

- deterministic
- isolated
- readable
- fast
- provider independent
- UI independent
- independent of persistence
- independent of Skills
- independent of external services

Do not add:

- snapshots or approval tests
- reflection-based architecture libraries
- mocking packages
- property-based testing packages
- new production abstractions
- provider fixtures
- network fixtures
- database fixtures

---

## Explicitly Out of Scope

Do not implement:

- Prompt 033 Step 7 verification
- additional Cruise models
- a Cruise Skill
- Cruise of the Week
- Marella integration
- web access
- parsing
- persistence
- repositories
- Entity Framework
- comparison or change detection
- price history
- watch lists
- alerts
- scheduling
- dashboards
- UI
- dependency injection
- production serialization configuration
- architecture redesign
- documentation updates

---

## Verification

Run the focused Core test project:

```bash
dotnet test KrytenAssist.Core.Tests/KrytenAssist.Core.Tests.csproj
```

Then, from the repository root, run:

```bash
dotnet build
dotnet test
```

The task is complete when:

- `KrytenAssist.Core.Tests` exists
- it references only `KrytenAssist.Core`
- it is included in `KrytenAssist.sln`
- all five domain models have focused test classes
- all required invariants are covered
- collection immutability and single enumeration are covered
- exact caller-supplied time and offset are covered
- focused Core tests pass
- the solution builds successfully
- all solution tests pass

Pre-existing SQLite package vulnerability warnings and unrelated Avalonia command-event warnings must be reported separately from warnings introduced by this task.

---

## Completion Report

After implementation, report:

### Files Created

List every file created.

### Files Modified

List every existing file modified, including the solution file.

### Tests Added

List each test class and summarise its covered behaviours.

### Production Corrections

State either:

```text
None
```

or describe every production correction and the failing Prompt 033 requirement that justified it.

### Focused Tests

Report:

- command executed
- total
- passed
- failed
- skipped

### Build

Report:

- command executed
- success or failure
- warning count
- error count

Distinguish pre-existing warnings from warnings introduced by this task.

### Full Test Suite

Report:

- command executed
- total tests across all test projects
- passed
- failed
- skipped

### Scope Check

Confirm that:

- only Step 6 was implemented
- the new test project references only Core
- no new production feature was added
- no new Cruise model was added
- no Cruise Skill was added
- no integration, persistence or UI behaviour was added
- no dependency-injection changes were made
- only the approved existing package versions were used
- no documentation files were modified

