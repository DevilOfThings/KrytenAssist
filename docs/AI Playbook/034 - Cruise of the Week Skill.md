# Prompt 034 – Cruise of the Week Skill

## Goal

Implement the first real Kryten Assist Skill: **Cruise of the Week**.

The Skill should retrieve Marella Cruises' current Cruise of the Week, parse the published sailing into the provider-independent Cruise domain introduced by Prompt 033, and return a `CruiseObservation` through the Skills framework introduced by Prompt 032.

This prompt validates that the Skills platform can deliver a real user-facing capability while preserving Clean Architecture and provider independence.

No historical storage, comparison, alerts, background scheduling, dashboard or user interface should be introduced.

---

## Why This Prompt Exists

Prompt 032 established the Skills framework.

Prompt 033 established the shared Cruise domain:

- `CruiseProvider`
- `CruisePrice`
- `CruiseOffer`
- `CruiseSnapshot`
- `CruiseObservation`

Those prompts proved the technical contracts but did not deliver a real business capability.

Prompt 034 connects the two foundations.

It introduces a provider-independent retrieval boundary, a Marella-specific infrastructure adapter, and a Cruise Skill that users and future dashboards can invoke without knowing how Marella publishes its data.

This becomes the reference pattern for later provider-backed Skills.

---

## User Experience

There is no user interface in this prompt.

The framework should support a future request such as:

> Show me Marella's Cruise of the Week.

Executing the Skill should return structured data describing the currently advertised sailing, including the information Marella publishes and that maps honestly into the Prompt 033 domain.

The caller should receive a successful `SkillResult` containing a `CruiseObservation` when retrieval and parsing succeed.

If Marella is unavailable or its page no longer contains the expected required information, the Skill should return a controlled failure result rather than fabricated or partially invented cruise data.

Cancellation should remain cancellation and must not be converted into an ordinary failure.

---

## Marella Source

The initial provider is Marella Cruises, published through TUI UK.

The dedicated source page is:

```text
https://www.tui.co.uk/cruise/deals/marella-cruise-of-the-week
```

At the time this prompt was designed, the page exposed a dedicated weekly deal plus one matching cruise result containing information such as:

- weekly saving and promotional code
- itinerary title
- ship name
- departure port
- departure date
- return date
- duration in nights
- per-person price
- total price

The current example must not be hard-coded into production code or tests.

Marella controls the page and may change:

- the selected sailing
- promotional wording
- prices
- HTML classes
- nesting
- attributes
- client-rendered components

All Marella page knowledge must remain isolated inside the Marella infrastructure implementation.

The rest of Kryten Assist must consume only application-owned interfaces and Core domain models.

---

## Architecture Overview

```text
CruiseOfTheWeekSkill
        │
        ▼
ICruiseOfTheWeekProvider
        │
        ▼
MarellaCruiseOfTheWeekProvider
        │
        ├── HttpClient
        │
        ▼
MarellaCruiseOfTheWeekParser
        │
        ▼
CruiseObservation
└── CruiseSnapshot
    ├── CruiseOffer
    │   └── CruiseProvider
    └── CruisePrice
```

Dependencies must point inward:

```text
KrytenAssist.Core
        ▲
        │
KrytenAssist.Application
        ▲
        │
KrytenAssist.Infrastructure

KrytenAssist.Avalonia ──► Application/Core
KrytenAssist.Avalonia ──► Infrastructure only at composition root
```

The completed Skills framework currently belongs to the Avalonia application. This prompt should integrate with that existing architecture rather than relocate or redesign Prompt 032.

---

## Responsibility Boundaries

### KrytenAssist.Core

Owns only the provider-independent Cruise domain.

Prompt 034 should consume the existing models without adding provider-specific fields, web concepts or persistence behavior.

### KrytenAssist.Application

Owns the provider-independent retrieval abstraction used by the Skill.

Introduce:

```text
ICruiseOfTheWeekProvider
```

The interface should return a `CruiseObservation` and must not expose:

- `HttpClient`
- AngleSharp types
- HTML
- Marella DTOs
- transport response types
- Avalonia types
- OpenAI types

### KrytenAssist.Infrastructure

Owns all Marella-specific retrieval and parsing behavior.

Introduce:

- `MarellaCruiseOfTheWeekProvider`
- `MarellaCruiseOfTheWeekParser`
- Marella-specific configuration and dependency-injection registration

Only Infrastructure should know:

- the Marella URL
- the page structure
- the provider identifier and display name
- how text and prices are extracted
- how a stable provider offer identifier is obtained or derived

### KrytenAssist.Avalonia

Owns the concrete `CruiseOfTheWeekSkill` because the established Prompt 032 Skill framework currently belongs to the desktop application.

The Skill should orchestrate the application abstraction and translate the outcome into `SkillResult`.

It must not contain:

- URL constants
- HTTP code
- HTML selectors
- price parsing
- date parsing
- Marella page knowledge

---

## Provider-Independent Retrieval Contract

Introduce the application-owned abstraction:

```csharp
public interface ICruiseOfTheWeekProvider
{
    Task<CruiseObservation> GetCurrentAsync(
        DateTimeOffset observedAt,
        CancellationToken cancellationToken = default);
}
```

The caller supplies `observedAt`.

This preserves the Prompt 033 time semantics and prevents Infrastructure from reading the system clock.

The `CruiseOfTheWeekSkill` should pass `SkillContext.RequestedAt` to the provider.

The interface must remain provider independent even though Prompt 034 supplies only a Marella implementation.

Future implementations should be able to support another cruise provider without changing the Skill contract or Core domain.

---

## Retrieval Failure Contract

Introduce a small application-owned exception representing failure to retrieve or interpret the current Cruise of the Week.

For example:

```text
CruiseOfTheWeekException
```

The exception should:

- represent an unavailable or unusable provider result
- support an inner exception for infrastructure diagnostics
- avoid exposing HTML or provider SDK types
- contain a safe user-facing message

Infrastructure should wrap relevant transport and parsing failures in this application-owned exception.

The Skill should catch this expected exception and return `SkillResult.Failure(...)`.

The Skill must not catch:

- `OperationCanceledException`
- arbitrary programmer errors
- every possible `Exception`

Cancellation should propagate normally.

---

## Marella Provider

`MarellaCruiseOfTheWeekProvider` should implement `ICruiseOfTheWeekProvider`.

It should:

1. accept a configured `HttpClient`
2. accept the Marella parser through constructor injection
3. request the configured Cruise of the Week page
4. require a successful HTTP response
5. read the returned HTML asynchronously
6. pass HTML, source reference and caller-supplied observation time to the parser
7. return the resulting `CruiseObservation`

It should support `CancellationToken` throughout.

It should not:

- use static `HttpClient`
- instantiate dependencies manually
- retry indefinitely
- use browser automation
- execute JavaScript
- persist results
- compare with previous observations
- cache the weekly result in this prompt
- read the system clock

Use a typed or named `HttpClient` registered through dependency injection.

Configure a finite timeout and a clear user agent appropriate for the application.

Do not send authentication cookies or attempt to log in to TUI.

---

## Marella Parser

`MarellaCruiseOfTheWeekParser` should parse an HTML string without performing network access.

Use a standards-aware HTML parser rather than regular expressions for document traversal.

The approved initial parser package is:

```text
AngleSharp 1.5.2
```

Keep all AngleSharp types inside Infrastructure.

The parser should accept:

- the HTML content
- the caller-supplied observation timestamp
- the source reference

and return:

```text
CruiseObservation
```

The parser must be deterministic and independently testable using small local HTML fixtures or inline representative HTML.

---

## Parsing Strategy

The parser should first identify the Cruise of the Week result region and then extract values relative to that region.

Avoid global queries that could accidentally select:

- another deal card
- navigation text
- footer content
- a recommended cruise
- the first unrelated price on the page

Prefer a combination of:

- stable semantic headings
- accessible labels
- structured attributes when present
- relative DOM traversal within the selected result

CSS class names may be used only where no more stable semantic marker exists. Centralize selectors and keep them provider-specific.

Do not use one large regular expression over the complete page.

Small, focused regular expressions may be used to interpret already-extracted text values such as:

- `27 Oct 2026`
- `7 nights`
- `£903 pp`

Parsing should be culture-explicit and deterministic.

---

## Required Marella Mapping

### CruiseProvider

Create:

```text
Id: marella
Name: Marella Cruises
```

These values belong only to the Marella adapter.

### CruiseOffer

Map:

- itinerary title
- ship name
- departure date
- duration in nights
- departure port when available
- itinerary summary when a distinct source-neutral value is available

Do not fabricate an itinerary summary by copying unrelated promotional text.

### Provider Offer Identifier

Prefer a stable provider-supplied identifier from the selected deal link or structured page data when one is available.

If the page does not expose a stable identifier, derive a deterministic provider identifier from stable offer values such as:

```text
marella:{normalized-title}:{yyyy-MM-dd}
```

The derivation must:

- be isolated in the Marella parser
- be deterministic
- use invariant rules
- avoid randomized values
- avoid the current system clock
- be covered by unit tests

Do not add a Kryten database identifier.

### CruisePrice

Parse only prices whose meaning is clear from the selected result.

At minimum, the current per-person price should be represented when published.

When the page clearly supplies a total price and basis, it may be represented as an additional `CruisePrice` while preserving source order.

Use:

```text
Currency: GBP
```

Use explicit source-neutral basis text such as:

```text
per person
total based on 2 sharing
```

Do not include an unlabeled or ambiguous numeric value merely because it resembles a price.

Do not calculate one price from another.

### Promotion Summary

Capture the weekly offer summary when present, including useful source wording such as the saving and promotion code.

Do not interpret terms and conditions or calculate the discount.

### CruiseObservation

Use:

- the parsed snapshot
- the exact caller-supplied `DateTimeOffset`
- the configured Marella source URL as the source reference

Do not replace the supplied timestamp with the parsing time.

---

## Required and Optional Source Data

The parser must require enough information to create valid Prompt 033 models.

Required:

- cruise title
- ship name
- departure date
- duration in nights
- at least one unambiguous price

Optional:

- departure port
- itinerary summary
- additional clearly labeled prices
- promotion summary
- stable provider-supplied offer identifier

If required data is missing, invalid or ambiguous, parsing should fail with a controlled application-owned exception.

Do not:

- invent placeholder values
- use empty strings
- substitute today's date
- substitute zero prices
- silently select unrelated page content

Optional values should remain `null` when the source does not publish them.

---

## CruiseOfTheWeekSkill

Create a concrete Skill implementing the existing `ISkill` contract.

Use this manifest:

```text
Id: cruise.of-the-week
Name: Cruise of the Week
Description: Retrieves Marella Cruises' current Cruise of the Week.
Version: 1.0.0
```

Support the operation:

```text
get-current
```

The operation should require no parameters.

Execution should:

1. validate `SkillRequest` and `SkillContext`
2. honor cancellation
3. reject unsupported operations with `SkillResult.Failure`
4. pass `SkillContext.RequestedAt` to `ICruiseOfTheWeekProvider`
5. return the retrieved `CruiseObservation` as successful result data
6. translate expected `CruiseOfTheWeekException` failures into a controlled failed `SkillResult`
7. allow cancellation to propagate

Do not add conversation-provider or Tool integration in this prompt.

---

## Dependency Injection

Register services through extension methods.

### Application

The application layer owns only the abstraction and exception. It should not register a concrete provider.

### Infrastructure

Provide a focused extension such as:

```text
AddMarellaCruiseOfTheWeek(...)
```

It should register:

- the configured `HttpClient`
- `MarellaCruiseOfTheWeekParser`
- `ICruiseOfTheWeekProvider` using the Marella implementation

Do not make the Avalonia composition root manually construct these services.

Do not force the desktop client to initialize unrelated API persistence services merely to use the Cruise provider.

### Avalonia Skills

Update the existing `AddSkills()` extension to register `CruiseOfTheWeekSkill` as an `ISkill`.

The existing registry population mechanism should discover it from registered `ISkill` instances without changes to registry behavior.

Preserve the existing `EchoSkill` unless a later prompt explicitly retires it.

---

## Configuration

The Marella source URL and timeout should be configuration-driven.

Introduce a small provider-specific options model owned by Infrastructure or the composition layer.

Suggested configuration shape:

```json
"CruiseOfTheWeek": {
  "Marella": {
    "SourceUrl": "https://www.tui.co.uk/cruise/deals/marella-cruise-of-the-week",
    "TimeoutSeconds": 30
  }
}
```

Validate:

- source URL is absolute HTTPS
- timeout is finite and positive

Do not add credentials, cookies or secrets.

The default checked-in configuration may contain the public Marella URL.

---

## Offline-First Behavior

Prompt 034 introduces an online provider because current Cruise of the Week data is inherently remote.

Offline-first principles still apply:

- the parser must be testable without network access
- automated tests must use deterministic local content
- the application should fail cleanly when offline
- no startup request should be required to launch Kryten Assist
- retrieving the offer should occur only when the Skill is executed
- existing offline prompt, search and embedding behavior must remain unchanged

Do not add a fake production offer or stale bundled response as an automatic fallback.

Historical/offline cruise storage belongs to Prompt 036.

---

## Testing Strategy

### Application Contract Tests

Tests should confirm that the provider contract remains expressed only in Core/application-owned types.

Do not test implementation details of an interface.

### Parser Tests

Create deterministic tests covering:

- complete valid Marella result
- title extraction
- ship extraction
- departure port extraction
- `DateOnly` departure parsing
- duration parsing
- per-person price parsing
- optional total price parsing
- price order
- promotion summary
- stable identifier extraction or deterministic derivation
- source reference
- exact observation timestamp and offset
- missing required fields
- invalid date
- invalid duration
- missing or ambiguous price
- optional fields being absent
- unrelated page prices not being selected

Use small, purpose-built HTML fixtures containing only the minimum representative structure needed for the parser contract.

Do not copy and commit the complete live TUI page.

### Provider Tests

Use a deterministic fake `HttpMessageHandler` or equivalent test transport to verify:

- configured URL is requested
- successful HTML reaches the parser
- cancellation is honored
- non-success HTTP responses become controlled retrieval failures
- transport failures are wrapped appropriately
- no real network call occurs

### Skill Tests

Use a simple fake `ICruiseOfTheWeekProvider` to verify:

- manifest metadata
- `get-current` succeeds
- operation comparison is case-insensitive
- no parameters are required
- unsupported operations fail
- context timestamp is passed to the provider
- successful result data is the returned `CruiseObservation`
- expected retrieval failures become failed `SkillResult`
- cancellation propagates

### Dependency-Injection Tests

Verify:

- Marella provider resolves through `ICruiseOfTheWeekProvider`
- the parser resolves
- `CruiseOfTheWeekSkill` resolves as `ISkill`
- the Skill Registry contains `cruise.of-the-week`
- registry execution succeeds with deterministic fake provider wiring
- lifetimes are intentional and stable

No automated test may call the live Marella website.

---

## Scope

### In Scope

- provider-independent `ICruiseOfTheWeekProvider`
- controlled application-owned retrieval exception
- Marella HTTP retrieval implementation
- Marella HTML parser
- mapping into Prompt 033 Core models
- `CruiseOfTheWeekSkill`
- dependency-injection registration
- public URL and timeout configuration
- comprehensive deterministic tests
- optional manual live verification

### Out of Scope

- historical storage
- repositories
- Entity Framework
- migrations
- snapshot comparison
- meaningful-change detection
- price history
- watch lists
- alerts
- cabin availability tracking
- itinerary-change detection
- background scheduling
- caching
- notifications
- dashboards
- navigation
- Avalonia views
- React UI
- conversation-provider integration
- automatic Skill execution
- multiple cruise providers
- TUI authentication
- myTUI login
- browser automation
- JavaScript execution
- booking automation
- payment flows

---

## Implementation Steps

### Step 1 – Application Retrieval Contract

Introduce:

- `ICruiseOfTheWeekProvider`
- `CruiseOfTheWeekException`

Use only Core and Base Class Library types.

---

### Step 2 – Marella Parser

Add AngleSharp to Infrastructure and implement `MarellaCruiseOfTheWeekParser`.

Map representative Marella HTML into a complete `CruiseObservation`.

Keep parsing fully deterministic and independent of network access.

---

### Step 3 – Marella HTTP Provider

Implement `MarellaCruiseOfTheWeekProvider` using injected `HttpClient` and parser.

Propagate cancellation and translate expected transport/parsing failures through the application-owned failure contract.

---

### Step 4 – Cruise of the Week Skill

Implement `CruiseOfTheWeekSkill` using the existing Prompt 032 contracts.

Return `CruiseObservation` through `SkillResult`.

---

### Step 5 – Configuration and Dependency Injection

Add focused provider configuration and service registration.

Register the new Skill with the existing Skill registry without changing registry semantics.

---

### Step 6 – Automated Tests

Add deterministic parser, provider, Skill and dependency-injection tests.

No live network calls are permitted in automated tests.

---

### Step 7 – End-to-End Verification

Verify:

- application and provider boundaries
- Marella types remain isolated in Infrastructure
- Skill discovery through `ISkillRegistry`
- successful execution using deterministic test input
- graceful failure behavior
- build and full regression suite

An optional manual live request may be performed separately to confirm the current Marella page remains compatible.

If live verification is performed, report the observed result and time without turning changing live content into a test assertion.

---

## Acceptance Criteria

Prompt 034 is complete when:

- a provider-independent Cruise of the Week abstraction exists
- Marella retrieval is isolated in Infrastructure
- Marella HTML parsing is isolated and independently testable
- the current result maps into Prompt 033 models
- required missing data produces a controlled failure
- no cruise data is fabricated
- `CruiseOfTheWeekSkill` implements `ISkill`
- the Skill is registered and discoverable as `cruise.of-the-week`
- successful execution returns `CruiseObservation`
- expected retrieval failures return a failed `SkillResult`
- cancellation propagates
- all automated tests remain offline and deterministic
- the solution builds successfully
- all tests pass
- no storage, history, scheduling or UI has been introduced
- the capability is ready for Prompt 035 dashboard discovery and Prompt 036 historical storage

---

## Results

> Complete this section after implementation.

### Status

_Not Started_

### Files Created

_To be completed._

### Files Updated

_To be completed._

### Build

_To be completed._

### Tests

_To be completed._

### Manual Verification

_To be completed if performed._

### Git Commit

_Not Created_

---

# Lessons Learned

> Complete this section after implementation.

