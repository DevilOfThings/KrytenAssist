# Codex Prompt 034f – Cruise of the Week Tests

## Source Prompt

Implement **Step 6 only** from:

```text
docs/AI Playbook/034 - Cruise of the Week Skill.md
```

Steps 1–5 have already been implemented. Do not implement Step 7.

---

## Goal

Add comprehensive deterministic tests for the Cruise of the Week capability implemented in Prompts 034a–034e.

Cover:

- the Marella HTML parser
- the Marella HTTP provider
- the Cruise of the Week Skill
- Marella configuration and dependency injection
- Skill registration and registry population
- the existing `AddSkills()` regression caused by the Cruise Skill's required provider dependency

All tests must run without accessing the live Marella website.

Do not perform final Prompt 034 verification, live retrieval, persistence, UI work or roadmap updates.

---

## Existing Regression

The existing tests in:

```text
KrytenAssist.Avalonia.Tests/Skills/SkillDependencyInjectionTests.cs
```

call `services.AddSkills()` without registering `ICruiseOfTheWeekProvider`.

`AddSkills()` now correctly registers `CruiseOfTheWeekSkill`, whose constructor requires that provider. Resolving all `ISkill` instances or `ISkillRegistry` therefore fails with:

```text
Unable to resolve service for type
'KrytenAssist.Application.Cruises.ICruiseOfTheWeekProvider'
while attempting to activate
'KrytenAssist.Avalonia.Skills.Cruises.CruiseOfTheWeekSkill'.
```

Fix the tests, not production registration.

Every test that resolves `ISkill` instances or `ISkillRegistry` after calling `AddSkills()` must first register a deterministic fake `ICruiseOfTheWeekProvider`.

Update assertions that currently expect exactly one Skill so they intentionally cover:

1. `EchoSkill`
2. `CruiseOfTheWeekSkill`

Do not make Cruise Skill registration conditional, add a production fake, weaken DI validation or hide missing dependencies.

---

## Allowed Changes

Create or modify files only inside:

```text
KrytenAssist.Avalonia.Tests/
```

Expected project change:

```text
KrytenAssist.Avalonia.Tests/KrytenAssist.Avalonia.Tests.csproj
```

Expected test locations:

```text
KrytenAssist.Avalonia.Tests/Cruises/Marella/
KrytenAssist.Avalonia.Tests/Skills/
```

Production files may be modified only if a required deterministic test exposes a genuine Prompt 034 defect.

Any production correction must be minimal, preserve public contracts, include a focused regression test and be reported explicitly.

Do not modify documentation, including the AI Playbook, Roadmap, Backlog, handovers or Codex prompts. Robin will update documentation after review.

---

## Existing Production Types

Exercise these implementations directly:

```text
KrytenAssist.Application/Cruises/ICruiseOfTheWeekProvider.cs
KrytenAssist.Application/Cruises/CruiseOfTheWeekException.cs
KrytenAssist.Infrastructure/Cruises/Marella/MarellaCruiseOfTheWeekParser.cs
KrytenAssist.Infrastructure/Cruises/Marella/MarellaCruiseOfTheWeekProvider.cs
KrytenAssist.Infrastructure/Cruises/Marella/MarellaCruiseOfTheWeekOptions.cs
KrytenAssist.Infrastructure/Cruises/Marella/MarellaCruiseServiceCollectionExtensions.cs
KrytenAssist.Avalonia/Skills/Cruises/CruiseOfTheWeekSkill.cs
KrytenAssist.Avalonia/DependencyInjection/SkillServiceCollectionExtensions.cs
```

Do not duplicate production parsing or validation logic in tests.

---

## Test Project References

Update the existing Avalonia test project and preserve its Avalonia reference.

Add direct references needed for Application and Infrastructure tests. Use compile-time aliases to preserve namespace isolation:

```xml
<ProjectReference Include="..\KrytenAssist.Application\KrytenAssist.Application.csproj">
  <Aliases>KrytenApplication</Aliases>
</ProjectReference>
<ProjectReference Include="..\KrytenAssist.Infrastructure\KrytenAssist.Infrastructure.csproj">
  <Aliases>KrytenInfrastructure</Aliases>
</ProjectReference>
```

Add a direct Core reference only if required for clean compilation.

Do not add a new test project or any NuGet package. Use the existing xUnit, FluentAssertions and .NET test SDK packages.

Test files that consume aliased assemblies should use `extern alias` and focused type aliases rather than exposing provider-specific namespaces broadly.

---

## Test Conventions

Use:

- Arrange, Act and Assert
- descriptive test names
- fixed timestamps
- small fictional HTML documents
- simple hand-written fakes
- an in-memory `HttpMessageHandler`
- exact assertions for domain mapping and cancellation

Do not use:

- live Marella content or external network access
- browser automation or JavaScript
- the system clock
- sleeps, retries or test ordering
- shared mutable fixtures
- secrets, cookies or authentication
- snapshot or approval tests
- mocking libraries

---

## Shared Deterministic Data

Use fictional Marella-shaped content, not the complete TUI page. Representative values may include:

```text
Promotion: This week's deal: Save £300 per booking with code WEEK300
Title: Mediterranean Medley
Ship: Marella Explorer
Departure: From Palma on 27 Oct 2026
Duration: 27 Oct 2026 - 7 nights
Per-person price: £903 pp
Total price: £1,806 Total price based on 2 sharing
```

Use a fixed non-zero-offset timestamp:

```csharp
new DateTimeOffset(2026, 7, 14, 10, 30, 0, TimeSpan.FromHours(1))
```

Use a non-live source URL for directly constructed providers:

```text
https://example.test/cruise-of-the-week
```

The checked-in Marella URL may be asserted as configuration but must never be requested.

---

## Marella Parser Tests

Create:

```text
KrytenAssist.Avalonia.Tests/Cruises/Marella/MarellaCruiseOfTheWeekParserTests.cs
```

Test `MarellaCruiseOfTheWeekParser.Parse(...)` directly.

Cover successful mapping of:

- provider id `marella`
- provider name `Marella Cruises`
- cruise title and ship
- departure port and `DateOnly` departure
- duration
- per-person GBP price and basis
- optional total GBP price and basis
- source price order
- promotion summary
- exact source reference
- exact timestamp and non-zero offset

Cover identifiers:

- provider-supplied identifier is preferred when present
- deterministic fallback is stable for identical input
- fallback changes when an identity-defining input changes
- fallback does not depend on the system clock

Cover optional data:

- departure port absent
- itinerary summary absent
- total price absent
- promotion behavior matches the implementation contract

Cover controlled `CruiseOfTheWeekException` failures for:

- missing title
- missing ship
- missing or invalid departure date
- missing, zero or invalid duration
- missing or ambiguous price
- ambiguous weekly result headings

Cover scoping:

- unrelated prices outside the selected weekly result are ignored
- another cruise card is not selected

Use small inline HTML builders or private fixture methods. Do not commit downloaded TUI HTML.

---

## Marella HTTP Provider Tests

Create:

```text
KrytenAssist.Avalonia.Tests/Cruises/Marella/MarellaCruiseOfTheWeekProviderTests.cs
```

Construct the provider with an `HttpClient` backed by a hand-written fake `HttpMessageHandler`, a configured HTTPS base address and a real parser.

Verify:

- exactly one GET request is made
- the configured absolute URL is requested
- successful HTML returns the expected observation
- exact caller timestamp reaches the observation
- configured URL becomes the source reference
- non-success HTTP responses become `CruiseOfTheWeekException`
- `HttpRequestException` is wrapped
- timeout-style cancellation not caused by the caller is wrapped
- caller cancellation propagates as `OperationCanceledException`
- empty responses become controlled failures

Verify constructor guards for null client, null parser, missing base address, relative address where constructible, and non-HTTPS address.

Do not use a local HTTP server or external request.

---

## Cruise Skill Tests

Create:

```text
KrytenAssist.Avalonia.Tests/Skills/CruiseOfTheWeekSkillTests.cs
```

Use a small fake `ICruiseOfTheWeekProvider`.

Verify manifest metadata exactly:

```text
Id: cruise.of-the-week
Name: Cruise of the Week
Description: Retrieves Marella Cruises' current Cruise of the Week.
Version: 1.0.0
```

Verify:

- `get-current` succeeds
- matching is case-insensitive
- no parameters are required
- parameters are rejected
- unsupported operations fail
- exact `SkillContext.RequestedAt` reaches the provider
- provider is called exactly once for valid execution
- result data is the exact returned observation
- success message matches the implementation
- `CruiseOfTheWeekException` becomes a controlled failed result
- safe exception message becomes the failure message
- caller cancellation propagates
- unexpected exceptions are not swallowed
- constructor, request and context null guards

Do not involve Infrastructure in Skill behavior tests.

---

## Dependency Injection and Options Tests

Create:

```text
KrytenAssist.Avalonia.Tests/Cruises/Marella/MarellaCruiseDependencyInjectionTests.cs
```

Build configuration with `ConfigurationBuilder` and an in-memory collection.

Verify the Infrastructure extension:

- rejects null services and configuration
- returns the original `IServiceCollection`
- binds the supplied configuration object directly
- preserves configured URL and timeout
- resolves the options, parser and provider abstraction
- resolves the Marella implementation
- parser and provider are singletons
- resolving the graph performs no HTTP request

Verify named-client configuration without making a request:

- base address matches configuration
- timeout matches configuration
- user agent contains `KrytenAssist/0.1`

Verify validation rejects:

- missing, empty and whitespace-only source URL
- relative, HTTP and malformed source URL
- timeout below 1
- timeout above 300

Verify timeout boundaries 1 and 300 are accepted.

Assert `OptionsValidationException` and useful messages without over-coupling to complete framework-generated text.

---

## Existing Skill DI Test Updates

Modify:

```text
KrytenAssist.Avalonia.Tests/Skills/SkillDependencyInjectionTests.cs
```

Register a deterministic fake provider before `AddSkills()` in every test that resolves Skills or the registry.

Verify:

- registry resolves
- Echo remains registered
- Cruise Skill is registered
- both Skills are singleton
- Echo is first and Cruise is second
- registry contains `sample.echo`
- registry contains `cruise.of-the-week`
- registry retains the exact DI-created instances
- Echo executes through the registry
- Cruise executes through the registry using the fake provider

Do not use `.Single()` on the complete `IEnumerable<ISkill>` now that two Skills intentionally exist. Filter by type or assert the ordered collection.

Do not change the production registry factory.

---

## Test Fake Design

Test fakes may be private nested types or small internal test types.

The fake Cruise provider should support deterministic control over:

- returned observation
- received timestamp and cancellation token
- invocation count
- controlled `CruiseOfTheWeekException`
- cancellation
- unexpected exception where needed

Keep fakes purpose-built. Do not add a general mocking framework or production fake.

---

## Production Corrections

Do not change production merely to preserve obsolete test assumptions.

Specifically, do not:

- conditionally register the Cruise Skill
- remove it from `AddSkills()`
- add a default production provider
- allow a null provider
- catch dependency-resolution failures
- make the provider optional

A production change is permitted only when a deterministic test proves a Prompt 034 violation. Make the narrowest correction and report it.

---

## Explicitly Out of Scope

Do not implement:

- Step 7 final verification
- live retrieval
- full TUI fixtures
- parser/provider redesign except a proven defect
- production fakes
- retries, caching or scheduling
- persistence, history, comparisons or alerts
- dashboards, navigation or other UI
- conversation-provider integration
- multiple providers
- browser automation
- documentation updates

---

## Verification

Run:

```bash
dotnet test KrytenAssist.Avalonia.Tests/KrytenAssist.Avalonia.Tests.csproj
dotnet build
dotnet test
```

No command or test may call the live Marella website.

The task is complete when:

- parser tests cover success, optional, invalid and scoped content
- provider tests cover success, transport/HTTP failures and cancellation
- Skill tests cover metadata, execution, failures and cancellation
- DI tests cover configuration, validation, lifetimes and lazy resolution
- existing Skill DI tests register a fake provider
- both Skills resolve and populate the registry in order
- focused and full test suites pass
- the solution builds

Report existing SQLite vulnerability and Avalonia unused-event warnings separately from warnings introduced here.

---

## Completion Report

### Files Created

List every file created.

### Files Modified

List every existing file modified.

### Tests Added

List each test class and summarize its coverage.

### Existing Regression

Confirm the legacy Skill DI tests now register a deterministic provider and intentionally cover both Skills.

### Production Corrections

State `None`, or report every correction, failing test and Prompt 034 requirement.

### Focused Tests

Report command, total, passed, failed and skipped.

### Build

Report command, status, warning count and error count. Separate pre-existing warnings.

### Full Test Suite

Report command, total, passed, failed and skipped.

### Network Check

Confirm no test called Marella or another external service.

### Scope Check

Confirm:

- only Step 6 was implemented
- Step 7 was not performed
- tests are deterministic and offline
- no production fake or weakened registration was added
- no persistence, UI, retries, caching or scheduling was added
- no documentation was modified

