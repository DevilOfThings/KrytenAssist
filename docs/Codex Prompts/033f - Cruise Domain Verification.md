# Codex Prompt 033f – Cruise Domain Verification

## Source Prompt

Implement **Step 7 only** from:

```text
docs/AI Playbook/033 - Cruise Domain Models.md
```

Steps 1–6 have already been implemented.

Do not begin Prompt 034.

---

## Goal

Verify that the completed shared Cruise domain is provider independent, UI independent, persistence independent and ready for use by Prompt 034.

This is a verification task, not a feature-development task.

The verification must demonstrate that:

- all five required Cruise domain models exist in `KrytenAssist.Core`
- the models use only application-owned Cruise types and Base Class Library types
- model relationships use composition
- required invariants are enforced
- money uses `decimal`
- departure dates use `DateOnly`
- observations use caller-supplied `DateTimeOffset`
- snapshot collections are protected from external mutation
- the dedicated Core test project references only Core
- focused Cruise domain tests pass
- the solution builds successfully
- all solution tests pass
- a future provider implementation can map source data into the domain without changing Core

Do not add new domain behaviour merely to create another verification mechanism.

---

## Repository Context

The Cruise domain is implemented in:

```text
KrytenAssist.Core/Cruises
```

The required production files are:

```text
CruiseProvider.cs
CruisePrice.cs
CruiseOffer.cs
CruiseSnapshot.cs
CruiseObservation.cs
```

The dedicated test project is:

```text
KrytenAssist.Core.Tests
```

The required test files are:

```text
KrytenAssist.Core.Tests/Cruises/CruiseProviderTests.cs
KrytenAssist.Core.Tests/Cruises/CruisePriceTests.cs
KrytenAssist.Core.Tests/Cruises/CruiseOfferTests.cs
KrytenAssist.Core.Tests/Cruises/CruiseSnapshotTests.cs
KrytenAssist.Core.Tests/Cruises/CruiseObservationTests.cs
```

Use the current production contracts and tests as the source of truth. Do not duplicate the domain in verification-only code.

---

## Allowed Changes

The expected outcome is **no source-code changes**.

Production or test files may be modified only if verification exposes a genuine defect against Prompt 033.

Any correction must:

- be minimal
- fix only the verified defect
- preserve Clean Architecture
- preserve the existing public Cruise contracts unless the contract itself violates Prompt 033
- include or update a focused Core unit test
- be described explicitly in the completion report

Do not modify documentation files as part of the verification run.

Do not update the AI Playbook, Roadmap, Backlog or Session Handover in this task. Those updates will be completed separately after verification has been reviewed.

Do not create a commit or push changes.

---

## Verification Process

### 1. Record the Initial Working Tree

Run:

```bash
git status --short
```

Record all pre-existing changes before verification.

Do not modify, discard, stage or overwrite unrelated work.

---

### 2. Verify Project Placement and Dependencies

Inspect:

```text
KrytenAssist.Core/KrytenAssist.Core.csproj
KrytenAssist.Core.Tests/KrytenAssist.Core.Tests.csproj
KrytenAssist.sln
```

Confirm that:

- all Cruise production models are in `KrytenAssist.Core`
- all use `KrytenAssist.Core.Cruises`
- `KrytenAssist.Core` has no project references
- `KrytenAssist.Core` has no NuGet package references
- `KrytenAssist.Core.Tests` references only `KrytenAssist.Core`
- the test project uses only the approved existing test packages
- `KrytenAssist.Core.Tests` is included in `KrytenAssist.sln`

Report concrete evidence from the project files.

Do not add an architecture-testing package or reflection-based test library.

---

### 3. Verify Forbidden Dependencies Are Absent

Inspect all production files under:

```text
KrytenAssist.Core/Cruises
```

Confirm that they do not reference:

- Avalonia
- OpenAI or another AI provider
- the Skills framework
- application services
- infrastructure services
- Entity Framework
- database attributes
- HTTP clients
- browser types
- provider DTOs or response types
- serialization-specific contracts

Also confirm that the models do not perform:

- web access
- file access
- database access
- system-clock access
- dependency resolution
- background scheduling
- UI operations

This may be verified through source inspection and targeted repository searches.

Do not add source annotations solely to prove the absence of dependencies.

---

### 4. Verify the Domain Model Set

Confirm that Prompt 033 introduced exactly these Cruise domain concepts:

```text
CruiseProvider
CruisePrice
CruiseOffer
CruiseSnapshot
CruiseObservation
```

Confirm that no provider-specific or speculative domain types were introduced, including:

- Marella-specific models
- provider subclasses
- provider enums
- ship models
- port models
- itinerary-leg models
- cabin models
- persistence entities
- retrieval result models

Existing unrelated Core models are outside this check.

---

### 5. Verify Model Contracts and Invariants

Inspect the models and use the existing Core tests as executable evidence.

#### CruiseProvider

Confirm that:

- it is an immutable sealed record
- it contains stable `Id` and display `Name` values
- required values reject null or blank input
- values are not interpreted as provider-specific data
- equality uses its values

#### CruisePrice

Confirm that:

- it is an immutable sealed record
- amount uses `decimal`
- negative amounts are rejected
- currency requires exactly three ASCII alphabetic characters
- currency is stored using invariant uppercase
- optional basis is validated when supplied
- no currency conversion, formatting or lookup exists

#### CruiseOffer

Confirm that:

- it is an immutable sealed record
- it composes `CruiseProvider`
- it retains stable source-neutral offer details
- departure uses `DateOnly`
- duration is a positive number of nights
- optional departure port and itinerary summary are honest nullable values
- it contains no price, retrieval or persistence behaviour

#### CruiseSnapshot

Confirm that:

- it is an immutable sealed record
- it composes `CruiseOffer` and `CruisePrice`
- at least one non-null price is required
- source order is preserved
- the source sequence is enumerated once
- the price collection is defensively copied
- callers cannot mutate the exposed collection successfully
- optional promotion summary is validated
- it owns no observation timestamp
- it contains no comparison or change-detection logic

#### CruiseObservation

Confirm that:

- it is an immutable sealed record
- it composes `CruiseSnapshot`
- it requires an explicit caller-supplied `DateTimeOffset`
- the exact timestamp and offset are preserved
- it does not access the system clock
- optional source reference is transport neutral
- it contains no persistence, comparison or scheduling state

Do not refactor models that already satisfy these requirements.

---

### 6. Verify Composition Boundaries

Confirm this relationship:

```text
CruiseObservation
└── CruiseSnapshot
    ├── CruiseOffer
    │   └── CruiseProvider
    └── CruisePrice (one or more)
```

Verify that:

- composition is used instead of inheritance
- nested properties are not duplicated across models unnecessarily
- provider identity remains data rather than provider behaviour
- snapshot state is separate from observation time
- no model reaches outward to retrieve or store data

The goal is a source-neutral domain that future infrastructure can map into.

---

### 7. Verify Future Provider Mapping

By inspecting constructors and public properties, confirm that a future provider implementation can:

1. map provider identity into `CruiseProvider`
2. map stable cruise details into `CruiseOffer`
3. map source prices into one or more `CruisePrice` values
4. combine current state into `CruiseSnapshot`
5. attach retrieval time and source reference through `CruiseObservation`

This is an architectural inspection only.

Do not implement:

- a provider mapper
- a DTO
- a fake provider
- a Cruise Skill
- a console verification application
- a debug API endpoint

Report whether the mapping path is possible without modifying Core.

---

### 8. Run Focused Core Tests

Run:

```bash
dotnet test KrytenAssist.Core.Tests/KrytenAssist.Core.Tests.csproj
```

Report:

- total tests
- passed tests
- failed tests
- skipped tests

The focused test project must execute without starting an API, Avalonia application, external service or database.

---

### 9. Build and Run the Full Test Suite

From the repository root, run:

```bash
dotnet build
dotnet test
```

Report:

- each command executed
- success or failure
- build warning count
- build error count
- test totals across all test projects

Clearly distinguish:

- pre-existing warnings
- warnings introduced by verification

The existing SQLite package vulnerability warnings and unrelated Avalonia command-event warnings must be reported but must not be addressed in this task.

---

### 10. Confirm the Final Working Tree

Run:

```bash
git status --short
```

Compare the result with the initial working-tree state.

The preferred result is that verification introduced no file changes.

Do not stage, commit, push or modify documentation.

---

## Acceptance Criteria

Step 7 is complete when:

- all five Cruise domain models have been inspected
- Core contains no outward project or package dependency
- Cruise models contain no forbidden provider, UI, persistence, Skills or infrastructure references
- model invariants are covered by executable tests
- model composition matches the Prompt 033 architecture
- money, date and observation-time semantics are correct
- snapshot price immutability is verified
- future provider mapping is possible without modifying Core
- focused Core tests pass
- the solution builds successfully
- all solution tests pass
- verification introduces no unintended changes
- Prompt 034 has not been started

---

## Design Constraints

Verification must remain:

- read-only unless a genuine defect is found
- provider independent
- UI independent
- persistence independent
- deterministic
- offline
- isolated
- repeatable
- non-destructive

Do not introduce:

- new abstractions
- new models
- reflection or assembly scanning
- architecture-testing packages
- network access
- file persistence
- database access
- application startup
- sleeps or retries
- new NuGet packages
- new project references

---

## Explicitly Out of Scope

Do not implement:

- Prompt 034
- Cruise of the Week
- a Cruise Skill
- provider implementations
- Marella integration
- provider DTOs or mappers
- web access
- HTML parsing
- JSON transport contracts
- repositories
- Entity Framework
- migrations
- storage
- snapshot comparison
- change detection
- price history
- watch lists
- alerts
- cabin availability
- itinerary detection
- scheduling
- notifications
- dashboards
- navigation
- Avalonia or React UI
- dependency injection
- documentation completion
- Roadmap updates
- Backlog updates
- Session Handover updates
- Git commits or pushes

---

## Completion Report

After verification, provide:

### Domain Verification

Report whether each item passed or failed, with concise evidence:

- Core project independence
- Core test-project isolation
- forbidden-dependency inspection
- required model set
- model immutability
- constructor invariants
- money semantics
- departure-date semantics
- snapshot collection immutability
- observation-time semantics
- composition boundaries
- future provider mapping

### Files Created

List every file created during verification.

If none were created, state:

```text
None
```

### Files Modified

List every existing file modified during verification.

If none were modified, state:

```text
None
```

### Production Corrections

State either:

```text
None
```

or describe every correction, the verified defect that required it and the focused test covering it.

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
- pre-existing warnings
- warnings introduced by verification

### Full Test Suite

Report:

- command executed
- test totals by project
- combined total
- passed
- failed
- skipped

### Working Tree

Report:

- pre-verification status
- post-verification status
- whether verification introduced any changes

### Scope Check

Confirm that:

- only Step 7 was verified
- no new production behaviour was added
- no new Cruise model was added
- no Cruise Skill was added
- no provider or web integration was added
- no persistence or UI behaviour was added
- no dependency-injection changes were made
- no packages or project references were added
- no documentation files were modified
- Prompt 034 was not started

