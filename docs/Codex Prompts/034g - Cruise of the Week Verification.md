# Codex Prompt 034g – Cruise of the Week Verification

## Source Prompt

Implement **Step 7 only** from:

```text
docs/AI Playbook/034 - Cruise of the Week Skill.md
```

Steps 1–6 have already been implemented.

Do not begin Prompt 035.

---

## Goal

Verify the completed Cruise of the Week capability end to end while preserving Clean Architecture, provider independence and offline-first behavior.

This is a verification task, not a feature-development task.

Demonstrate that:

- Application owns the provider-independent retrieval contract
- Infrastructure owns all Marella parsing, HTTP and configuration behavior
- Avalonia owns the Skill and composition root
- Core remains provider independent
- the Marella parser is deterministic and network independent
- HTTP retrieval is lazy and cancellation aware
- expected failures remain controlled
- the Skill is registered and discoverable through `ISkillRegistry`
- deterministic execution returns a `CruiseObservation`
- configuration and DI resolve without network access
- focused Cruise tests pass
- the solution builds
- all solution tests pass
- Prompt 034 is ready for later dashboard discovery and historical storage

Do not add behavior merely to create a verification mechanism.

---

## Repository Context

### Core Domain

```text
KrytenAssist.Core/Cruises/
├── CruiseProvider.cs
├── CruisePrice.cs
├── CruiseOffer.cs
├── CruiseSnapshot.cs
└── CruiseObservation.cs
```

### Application Contract

```text
KrytenAssist.Application/Cruises/
├── ICruiseOfTheWeekProvider.cs
└── CruiseOfTheWeekException.cs
```

### Marella Infrastructure

```text
KrytenAssist.Infrastructure/Cruises/Marella/
├── MarellaCruiseOfTheWeekOptions.cs
├── MarellaCruiseOfTheWeekParser.cs
├── MarellaCruiseOfTheWeekProvider.cs
└── MarellaCruiseServiceCollectionExtensions.cs
```

### Avalonia Skill and Composition

```text
KrytenAssist.Avalonia/Skills/Cruises/CruiseOfTheWeekSkill.cs
KrytenAssist.Avalonia/DependencyInjection/SkillServiceCollectionExtensions.cs
KrytenAssist.Avalonia/Program.cs
KrytenAssist.Avalonia/appsettings.json
```

### Tests

```text
KrytenAssist.Avalonia.Tests/Cruises/CruiseTestData.cs
KrytenAssist.Avalonia.Tests/Cruises/Marella/MarellaCruiseOfTheWeekParserTests.cs
KrytenAssist.Avalonia.Tests/Cruises/Marella/MarellaCruiseOfTheWeekProviderTests.cs
KrytenAssist.Avalonia.Tests/Cruises/Marella/MarellaCruiseDependencyInjectionTests.cs
KrytenAssist.Avalonia.Tests/Skills/CruiseOfTheWeekSkillTests.cs
KrytenAssist.Avalonia.Tests/Skills/FakeCruiseOfTheWeekProvider.cs
KrytenAssist.Avalonia.Tests/Skills/SkillDependencyInjectionTests.cs
```

Use the current contracts and tests as the source of truth. Do not duplicate the capability in verification-only code.

---

## Expected Architecture

Confirm this dependency and execution path:

```text
ISkillRegistry
└── CruiseOfTheWeekSkill
    └── ICruiseOfTheWeekProvider
        └── MarellaCruiseOfTheWeekProvider
            ├── configured HttpClient
            └── MarellaCruiseOfTheWeekParser
                └── CruiseObservation
                    └── CruiseSnapshot
                        ├── CruiseOffer
                        │   └── CruiseProvider
                        └── CruisePrice
```

Confirm project dependencies point inward:

```text
Core
  ▲
Application
  ▲
Infrastructure

Avalonia ──► Application
Avalonia composition root only ──► Infrastructure
```

The Skill must not know how Marella publishes HTML. Infrastructure must not know about Skills.

---

## Allowed Changes

The expected outcome is **no source-code changes**.

Production or test files may be modified only if verification exposes a genuine defect against Prompt 034.

Any correction must:

- be minimal
- fix only the verified defect
- preserve existing public contracts
- preserve architecture boundaries
- include or update a focused deterministic test
- be described explicitly in the completion report

Do not modify documentation during verification.

Do not update the AI Playbook, Roadmap, Backlog or Session Handover. Those updates will be completed separately after review.

Do not stage, commit, push, discard or overwrite existing work.

---

## Verification Process

### 1. Record the Initial Working Tree

Run:

```bash
git status --short
```

Record all staged, unstaged and untracked files.

The Prompt 034 implementation may still be present in the working tree. Treat it as pre-existing work and preserve it exactly.

Do not use destructive Git commands.

---

### 2. Verify Project References and Package Placement

Inspect:

```text
KrytenAssist.Core/KrytenAssist.Core.csproj
KrytenAssist.Application/KrytenAssist.Application.csproj
KrytenAssist.Infrastructure/KrytenAssist.Infrastructure.csproj
KrytenAssist.Avalonia/KrytenAssist.Avalonia.csproj
KrytenAssist.Avalonia.Tests/KrytenAssist.Avalonia.Tests.csproj
```

Confirm:

- Core has no provider, HTTP, AngleSharp, Avalonia or OpenAI dependency
- Application references Core but not Infrastructure or Avalonia
- Infrastructure references Application and Core
- AngleSharp 1.5.2 exists only where provider parsing is implemented
- `Microsoft.Extensions.Http` 10.0.9 is added to Infrastructure
- `Microsoft.Extensions.Options.ConfigurationExtensions` 10.0.9 is added to Infrastructure
- Avalonia references Application through `KrytenApplication`
- Avalonia references Infrastructure through `KrytenInfrastructure`
- the Infrastructure reference is used only by the composition root
- test aliases preserve the same namespace isolation
- no unauthorized package or project reference was introduced

Report concrete evidence from the project files.

---

### 3. Verify the Application Boundary

Inspect:

```text
KrytenAssist.Application/Cruises/ICruiseOfTheWeekProvider.cs
KrytenAssist.Application/Cruises/CruiseOfTheWeekException.cs
```

Confirm the contract:

- returns `Task<CruiseObservation>`
- accepts caller-supplied `DateTimeOffset observedAt`
- accepts optional `CancellationToken`
- uses only Core/Application-owned and Base Class Library types
- exposes no HTML, HTTP, AngleSharp, Marella, Avalonia or provider SDK type
- contains no source URL or configuration
- supports a controlled application-owned exception with an inner exception

Do not move or redesign the contract.

---

### 4. Verify Marella Isolation

Inspect all files under:

```text
KrytenAssist.Infrastructure/Cruises/Marella
```

Confirm that Marella-specific knowledge remains isolated there, including:

- provider id and display name
- semantic headings and selectors
- HTML traversal
- date, duration and price interpretation
- provider-offer identifier extraction or derivation
- HTTP transport
- source options
- named-client registration

Search outside Infrastructure for Marella-specific parsing and transport behavior.

The Avalonia composition-root alias and user-facing Skill description may mention Marella. Core models and Application contracts must not contain Marella-specific behavior.

---

### 5. Verify Parser Behavior

Inspect the parser and its tests.

Confirm that parsing:

- accepts HTML, exact caller timestamp and source reference
- performs no network or file access
- uses AngleSharp for document traversal
- scopes extraction to the selected weekly result
- maps provider identity, offer details, prices and promotion
- uses invariant date and decimal parsing
- preserves price meaning and order
- prefers a provider-supplied identifier
- derives a deterministic fallback when necessary
- does not use the system clock or random values
- preserves optional values as null
- throws `CruiseOfTheWeekException` for missing, invalid or ambiguous required data
- ignores unrelated page prices
- never fabricates required data

Use existing deterministic tests as executable evidence. Do not download or commit the live TUI page.

---

### 6. Verify HTTP Provider Behavior

Inspect the provider and tests.

Confirm that the provider:

- accepts an injected `HttpClient` and parser
- requires an absolute HTTPS base address
- performs one cancellable GET when invoked
- requires a successful response
- rejects an empty response
- forwards HTML, source URL and exact caller timestamp to the parser
- wraps HTTP and timeout failures using `CruiseOfTheWeekException`
- propagates caller cancellation
- does not retry, cache, persist or read the system clock
- makes no request during construction or DI resolution

Confirm provider tests use only an in-memory `HttpMessageHandler`.

---

### 7. Verify Configuration and Dependency Injection

Inspect:

```text
KrytenAssist.Infrastructure/Cruises/Marella/MarellaCruiseOfTheWeekOptions.cs
KrytenAssist.Infrastructure/Cruises/Marella/MarellaCruiseServiceCollectionExtensions.cs
KrytenAssist.Avalonia/Program.cs
KrytenAssist.Avalonia/appsettings.json
```

Confirm:

- the caller supplies the exact Marella configuration section
- options bind directly from that section
- source URL must be nonblank, absolute and HTTPS
- timeout must be between 1 and 300 seconds inclusive
- options validate on start
- the named client uses the configured base address and timeout
- user agent is `KrytenAssist/0.1`
- no credentials, cookies or authentication are configured
- parser and provider lifetimes are singleton
- provider construction uses `IHttpClientFactory`
- focused provider registration occurs before `AddSkills()`
- the broad API persistence registration is not invoked
- registration and graph resolution remain lazy with respect to retrieval

Do not resolve or execute the live provider merely to prove registration.

---

### 8. Verify Skill Behavior and Discovery

Inspect:

```text
KrytenAssist.Avalonia/Skills/Cruises/CruiseOfTheWeekSkill.cs
KrytenAssist.Avalonia/DependencyInjection/SkillServiceCollectionExtensions.cs
KrytenAssist.Avalonia/Skills/Services/SkillRegistry.cs
```

Confirm manifest metadata:

```text
Id: cruise.of-the-week
Name: Cruise of the Week
Description: Retrieves Marella Cruises' current Cruise of the Week.
Version: 1.0.0
```

Confirm execution:

- supports `get-current` case-insensitively
- accepts no parameters
- passes `SkillContext.RequestedAt` to the provider
- returns the exact observation through successful `SkillResult.Data`
- converts expected retrieval failures to failed `SkillResult`
- propagates cancellation
- does not catch arbitrary exceptions
- contains no URL, HTTP, HTML or parsing behavior

Confirm registration:

- Echo remains first
- Cruise Skill is second
- both are singleton `ISkill` registrations
- the registry factory remains unchanged
- registry population uses exact DI-managed instances
- `Find("cruise.of-the-week")` discovers the Skill

---

### 9. Verify Deterministic End-to-End Execution

Use the existing Skill DI test as executable verification of this path:

```text
ServiceCollection
    -> deterministic ICruiseOfTheWeekProvider
    -> AddSkills()
    -> ServiceProvider
    -> ISkillRegistry
    -> Find("cruise.of-the-week")
    -> ISkill.ExecuteAsync("get-current")
    -> SkillResult
    -> CruiseObservation
```

Confirm:

- no production fake is used
- no Infrastructure HTTP call occurs
- deterministic test data supplies the observation
- execution occurs through `ISkillRegistry` and `ISkill`
- the result is successful
- the result contains `CruiseObservation`

Do not create a console project, debug endpoint or application-startup harness.

---

### 10. Verify Controlled Failure and Cancellation

Use the existing provider and Skill tests to confirm:

- missing required page data becomes a controlled parse failure
- HTTP non-success becomes a controlled retrieval failure
- transport failure is wrapped with its inner exception
- timeout becomes a controlled retrieval failure
- provider caller cancellation propagates
- Skill expected failure becomes a failed result
- Skill caller cancellation propagates
- unexpected Skill dependency failures are not swallowed

Report which test classes provide the evidence.

---

### 11. Run Focused Cruise Tests

Run the Cruise parser, provider, Skill and DI tests with supported filters.

A suitable command is:

```bash
dotnet test KrytenAssist.Avalonia.Tests/KrytenAssist.Avalonia.Tests.csproj --filter "FullyQualifiedName~CruiseOfTheWeek|FullyQualifiedName~MarellaCruise"
```

If the filter omits the updated general Skill DI class, also run:

```bash
dotnet test KrytenAssist.Avalonia.Tests/KrytenAssist.Avalonia.Tests.csproj --filter FullyQualifiedName~SkillDependencyInjectionTests
```

Report totals exactly as emitted for each command.

All focused tests must remain offline.

---

### 12. Build and Run the Full Test Suite

From the repository root, run:

```bash
dotnet build
dotnet test
```

Report:

- each command
- build success or failure
- warning and error counts
- test totals by project
- combined totals
- passed, failed and skipped

Distinguish pre-existing warnings from warnings introduced by verification.

The known SQLite package vulnerability warnings and unrelated Avalonia unused-command-event warnings must be reported but not addressed.

---

### 13. Optional Manual Live Verification

Do **not** perform live verification unless the user separately and explicitly authorizes it.

Automated acceptance does not require a live request.

If explicitly authorized:

- perform at most one retrieval of the configured public Marella page
- use the production provider path or a narrowly scoped existing application path
- do not add a permanent endpoint, console project or test
- do not authenticate
- do not send cookies or secrets
- do not book or navigate beyond retrieval
- record the verification time and outcome
- avoid reproducing full live page content
- do not turn changing live values into automated assertions

A live failure may indicate provider unavailability or source markup drift. Report it separately from deterministic test results.

---

### 14. Confirm the Final Working Tree

Run:

```bash
git status --short
```

Compare it with the initial state.

The preferred result is that verification introduced no changes.

Do not stage, commit, push or modify documentation.

---

## Acceptance Criteria

Step 7 is complete when:

- project dependency directions have been inspected
- Application contract remains provider independent
- Marella behavior remains isolated in Infrastructure
- parser behavior is deterministic and independently tested
- HTTP retrieval is lazy, cancellable and controlled
- configuration and DI validation are correct
- Skill metadata and orchestration are correct
- Skill is registered and discoverable through the registry
- deterministic end-to-end Skill execution succeeds
- controlled failure and cancellation paths are verified
- focused Cruise tests pass
- the solution builds
- all solution tests pass
- no automated external request occurs
- verification introduces no unintended changes
- Prompt 035 has not begun

Live verification is optional and requires separate authorization.

---

## Design Constraints

Verification must remain:

- read-only unless a genuine defect is proven
- deterministic
- offline by default
- provider independent outside Infrastructure
- UI independent
- persistence independent
- repeatable
- non-destructive

Do not introduce:

- new production abstractions
- new Skills or providers
- reflection or assembly scanning
- new packages or project references
- browser automation
- application startup solely for verification
- persistence, repositories or migrations
- retries, caching or scheduling
- dashboards or navigation
- debug endpoints
- network access without explicit authorization

---

## Explicitly Out of Scope

Do not implement:

- Prompt 035
- dashboards or navigation
- Prompt 036 history storage
- repositories, Entity Framework or migrations
- snapshot comparison
- price history
- watch lists or alerts
- background scheduling
- caching or resilience policies
- notifications
- cabin availability
- itinerary-change detection
- multiple providers
- conversation-provider integration
- booking or payment behavior
- UI changes
- documentation completion
- Roadmap, Backlog or handover updates
- Git staging, commits or pushes

---

## Completion Report

### Capability Verification

Report pass or fail with concise evidence for:

- project dependency direction
- provider-independent Application contract
- Marella isolation
- parser determinism and mapping
- provider transport and cancellation
- options validation
- named-client configuration
- singleton lifetimes
- lazy network behavior
- Skill metadata
- Skill discovery
- deterministic registry execution
- controlled failure behavior
- cancellation propagation
- absence of persistence, UI, caching and scheduling

### Files Created

State `None` unless a verified correction required a test file.

### Files Modified

State `None` unless a verified correction was required.

### Production Corrections

State `None`, or describe every correction, verified defect and focused test.

### Focused Tests

For each command report total, passed, failed and skipped.

### Build

Report command, status, warning count, error count, pre-existing warnings and warnings introduced by verification.

### Full Test Suite

Report totals by project and combined total, passed, failed and skipped.

### Live Verification

State:

```text
Not performed — separate explicit authorization was not provided.
```

or report the authorized time and concise outcome.

### Working Tree

Report initial status, final status and whether verification introduced changes.

### Scope Check

Confirm:

- only Step 7 was verified
- Prompt 035 was not started
- no new behavior was added
- no production fake was added
- no UI or persistence was added
- no retries, caching or scheduling was added
- no package or project reference was added
- no automated external request occurred
- no documentation was modified
- no Git state was changed

