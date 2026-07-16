# Codex Prompt 036d – TUI Cruise Capture Adapter

## Implementation Prompt

Implement Step 4 of:

`docs/AI Playbook/036 - Cruise Discovery and Capture.md`

This prompt follows:

- `docs/Codex Prompts/036a - Embedded Cruise Browser Feasibility.md`
- `docs/Codex Prompts/036b - Cruise Source Navigation.md`
- `docs/Codex Prompts/036c - Cruise Capture Contract.md`

---

## Required Reading

Before changing code, read these files in order:

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/036 - Cruise Discovery and Capture.md`
5. Prompts 036a, 036b and 036c
6. all models under `KrytenAssist.Core/Cruises`
7. all contracts under `KrytenAssist.Application/Cruises`
8. the 036b source catalogue and trusted-host policy
9. the existing Marella Cruise of the Week parser, provider, options, dependency
   injection and tests
10. existing Infrastructure registration conventions

Do not begin implementation until the old HTTP parser and the new browser
capture boundary are clearly distinguished.

---

## Scope

Implement only Prompt 036d.

Create the first source-specific implementation of `ICruisePageCaptureService`
for the proven TUI/Marella Cruise Discovery source.

This step owns:

- a TUI-specific bounded payload schema private to Infrastructure
- deterministic parsing and validation of that payload
- mapping one valid candidate into existing Cruise domain models
- honest incomplete, ambiguous, unsupported, failed and cancelled results
- TUI source, HTTPS host and page-shape validation
- dependency-injection registration for the first capture implementation
- small fictional offline fixtures and focused tests

This step does **not** own:

- invoking JavaScript in `NativeWebView`
- constructing a payload from the live page
- a Capture button or capture orchestration ViewModel
- the review panel
- external browser opening
- persistence, price history, ratings or comparisons

Those browser/UI responsibilities belong to Prompt 036e.

---

## Proven Evidence and Constraints

Treat the following as established:

- TUI works interactively in Kryten's embedded macOS WebView.
- a read-only script can inspect the displayed page.
- the detailed itinerary anchor contains useful itinerary, ship-code, sailing-date
  and package values before TUI redirects to a generic booking-flow address.
- the WebView navigation-completed event is not a reliable readiness signal.
- Prompt 036c bounds `PagePayload` at 65,536 characters.
- `CruiseProvider` represents the operator; `CruiseSource` represents the retail
  or advertising source.

The existing `MarellaCruiseOfTheWeekParser` parses a different, old
server-rendered weekly-deal HTML shape used by the direct HTTP provider. Do not
silently treat it as the proven WebView capture format.

Preserve the old parser and provider unchanged unless a very small shared helper
can be extracted without changing their behavior. Do not redesign Prompt 034.

---

## Adapter Placement

Place the implementation in Infrastructure under a focused TUI capture namespace
or folder, for example:

```text
KrytenAssist.Infrastructure/Cruises/Tui
```

Prefer names such as:

```csharp
TuiCruisePageCaptureService
```

The implementation should depend only on:

- `CruisePageCaptureRequest`
- `CruiseCaptureResult`
- existing Core Cruise models
- .NET JSON/date/number parsing facilities

Do not add a browser or Avalonia reference to Infrastructure.

Do not add a new HTML parsing dependency. AngleSharp already exists for the old
Marella parser but is not required for the structured browser payload.

---

## Source Support Rules

The initial adapter supports exactly:

- discovery source identifier: `marella-cruise-of-the-week`
- retail source identifier: `tui`
- retail source name: `TUI`
- source-reference scheme: HTTPS
- source-reference host: exact case-insensitive `www.tui.co.uk`

Do not accept:

- HTTP
- relative references
- deceptive hosts such as `www.tui.co.uk.evil.example`
- another retailer
- another Cruise Discovery source identifier
- arbitrary TUI subdomains

An unsupported source, retailer or trusted page should return a controlled
`Unsupported` result. It should not throw for a request that already satisfies
the 036c constructor invariants.

Do not duplicate 036b's UI navigation decisions. This check determines capture
support, not whether the user may browse a page.

---

## Private TUI Payload Schema

Define a small JSON schema private to the TUI Infrastructure adapter. Internal
DTOs are acceptable.

The top-level payload should carry a version and candidate collection, similar
to:

```json
{
  "version": 1,
  "candidates": [
    {
      "providerOfferId": "fictional-offer-001",
      "title": "Island Discovery",
      "shipName": "Marella Example",
      "departureDate": "2027-01-15",
      "durationNights": 7,
      "departurePort": "Santa Cruz",
      "itinerarySummary": "Santa Cruz, Madeira and Gran Canaria",
      "prices": [
        {
          "amount": 999.00,
          "currency": "GBP",
          "basis": "per person"
        }
      ],
      "promotionSummary": "Fictional test promotion"
    }
  ]
}
```

This example is fictional. Do not check in the live Canarian Flavours payload,
package identifier, tracking data, cookies or copied marketing content.

The payload represents values already read by a later fixed, source-specific
browser script. Prompt 036d parses the payload only; it must not contain or
generate an executable script.

Version requirements:

- version `1` is supported
- missing, zero, negative or unknown versions return `Unsupported`
- do not add speculative version-conversion infrastructure

Use case-insensitive JSON property matching only if needed for normal serializer
behavior. Do not accept arbitrary aliases for fields.

---

## Candidate Selection

The adapter must produce at most one observation.

Candidate rules:

- zero candidates → `Incomplete`
- one candidate → validate and map it
- more than one candidate → `Ambiguous`
- a null candidate entry → controlled `Failed` or `Incomplete`, never a crash

Do not choose the first of multiple candidates.

Do not rank candidates or ask AI to decide.

Do not interpret a page-wide promotional banner as a candidate.

---

## Required and Optional Values

Required for successful capture:

- provider offer identifier
- title
- ship name
- ISO departure date (`yyyy-MM-dd`)
- duration of at least one night
- at least one price
- for each price: non-negative amount and valid three-letter currency

Optional:

- departure port
- itinerary summary
- price basis
- promotion summary

Blank optional strings should be treated as absent, not forwarded to Core
constructors as invalid whitespace.

If required values are missing or blank, return `Incomplete` with stable field
names such as:

- `providerOfferId`
- `title`
- `shipName`
- `departureDate`
- `durationNights`
- `prices`
- `prices.amount`
- `prices.currency`

Return distinct field names and stay within the 036c maximum.

Invalid dates, invalid durations, invalid currencies and negative prices are
controlled incomplete results. Do not expose raw serializer or domain exception
messages.

---

## Mapping

For one complete candidate, map to:

```text
CruiseProvider
    Id: marella
    Name: Marella Cruises

CruiseOffer
    values from the candidate

CruiseSnapshot
    offer, prices and optional promotion

CruiseObservation
    snapshot
    request.ObservedAt
    request.SourceReference
    request.Source
```

The retail source must come from the validated request rather than being inferred
from the operator.

The source reference and timestamp must be retained exactly.

Price basis must be retained. Do not convert `per person` into a total or infer a
basis when it is missing.

Do not fabricate a port, itinerary, promotion, ship name or price.

---

## Controlled Outcomes

Use the existing 036c factories.

### Success

Exactly one complete candidate maps into one `CruiseObservation`.

### Incomplete

Use when no complete cruise can be formed because candidates or required values
are missing or invalid.

### Ambiguous

Use when more than one candidate is supplied or conflicting values prevent an
honest choice.

### Unsupported

Use for unsupported source identity, retailer, host, payload version or payload
shape that is clearly not the TUI capture schema.

### Failed

Use for malformed JSON or an unexpected controlled parsing/mapping failure.

The message must be safe and actionable. Do not include raw JSON, exceptions,
selectors or stack traces.

### Cancelled

Check the supplied cancellation token before parsing and during candidate/price
mapping where practical. Return the existing controlled `Cancelled` result, or
follow the documented 036c cancellation semantics consistently. Choose one
behavior and test it.

Do not swallow an unrelated exception as cancellation.

---

## JSON Safety

Use `System.Text.Json` with deliberate options.

Requirements:

- do not enable polymorphic type handling
- do not deserialize types from payload metadata
- do not permit comments or trailing commas unless a demonstrated fixture needs
  them
- do not log or return the raw payload
- malformed JSON returns a safe controlled result
- extra unknown properties may be ignored for forward-compatible page metadata
- the 036c request length bound remains the outer payload limit

Do not write a general-purpose JSON-to-domain reflection mapper.

---

## Dependency Injection

Register the first implementation through an Infrastructure extension method.

Requirements:

- register `ICruisePageCaptureService` to the TUI implementation
- follow existing Infrastructure DI conventions
- use the smallest appropriate lifetime; the stateless adapter may be singleton
- do not register in `Program.cs`
- do not manually instantiate the adapter in a ViewModel
- resolution must not parse payloads, open a browser or perform network work

If a dedicated `AddTuiCruiseCapture` extension keeps ownership clearer, call it
from the established Infrastructure composition extension.

Do not inject the service into the UI yet; Prompt 036e owns orchestration.

---

## Fixtures

Create small fictional JSON fixtures under an appropriate test fixture folder.

Include only the minimum structures required for:

- one successful candidate
- missing required values
- multiple candidates
- unsupported version
- malformed JSON if a file fixture adds value

Prefer inline JSON for very small invalid cases and files for the representative
successful shape.

Do not copy a complete live TUI page or real package identifier.

---

## Tests

Add focused offline tests covering at least:

### Source Support

- supported source identifier, TUI retail source and exact host proceed
- wrong source identifier is unsupported
- wrong retailer is unsupported
- HTTP cannot enter through the request constructor
- deceptive or unrelated HTTPS host is unsupported

### Payload Shape

- supported version succeeds
- missing or unknown version is unsupported
- malformed JSON fails safely
- zero candidates is incomplete
- multiple candidates is ambiguous
- null candidate is controlled
- unknown JSON properties do not break a valid candidate

### Required Fields

- every required string is validated
- missing or invalid departure date is incomplete
- zero/negative duration is incomplete
- no prices is incomplete
- negative amount is incomplete
- invalid currency is incomplete
- missing fields are distinct and use stable names

### Mapping

- operator is Marella Cruises
- retail source is the request's TUI source
- source reference and timestamp are retained exactly
- offer identity, title, ship, date and duration map correctly
- optional port, itinerary, basis and promotion map when present
- blank optional strings become null
- multiple prices and their bases remain distinct

### Cancellation and Failures

- pre-cancelled token produces the chosen cancellation behavior
- raw JSON and exception details never appear in result messages

### Dependency Injection

- interface resolves to the TUI adapter
- lifetime matches the chosen registration
- resolving services performs no parsing, browsing or network access

No automated test may access TUI or another external service.

Existing Prompt 034 parser/provider tests and all 036a–036c tests must continue to
pass.

---

## Verification Commands

After implementation, run:

```bash
dotnet restore KrytenAssist.sln
dotnet build KrytenAssist.sln --no-restore
dotnet test KrytenAssist.Avalonia.Tests/KrytenAssist.Avalonia.Tests.csproj --no-build --no-restore
dotnet test KrytenAssist.Core.Tests/KrytenAssist.Core.Tests.csproj --no-build --no-restore
dotnet test KrytenAssist.sln --no-build --no-restore
```

Report exact totals, failures, skipped tests, errors and warnings.

Confirm no test or service resolution performed external work.

Do not suppress compiler, package or vulnerability warnings.

---

## Documentation

After implementation and verification:

- complete this prompt's `Results` section
- list every created and updated file
- document the private payload version and required fields
- record controlled-outcome behavior
- record exact build and test totals
- confirm no external network work occurred
- leave the main Prompt 036 Playbook and Roadmap unchanged until Robin reviews
  the 036d implementation

Do not mark Prompt 036 complete.

---

## Explicitly Out of Scope

Do not implement:

- WebView script invocation
- a payload-producing JavaScript bridge
- Capture Displayed Cruise command or ViewModel state
- capture review UI
- external browser opening
- persistence or migrations
- price history
- ratings or preferences
- source comparison
- AI extraction
- live TUI requests in tests
- background or scheduled capture
- booking, authentication or payment handling

Do not modify the 036b browser UI merely to demonstrate the adapter.

---

## Completion Criteria

Prompt 036d is complete when:

- Infrastructure implements `ICruisePageCaptureService` for the proven TUI source
- the private versioned payload is bounded by the 036c request
- source, retailer and exact host are validated
- one complete candidate maps to one provider-independent observation
- operator and retail source remain distinct
- missing, ambiguous, unsupported, failed and cancelled outcomes are controlled
- malformed or incomplete data never fabricates a Cruise
- the adapter is registered through Infrastructure DI
- resolution performs no browser or network work
- fixtures are fictional, minimal and offline
- all focused and regression tests pass
- this prompt's Results section is complete

Stop after Prompt 036d.

---

## Completion Report

When complete, provide:

### Summary

Describe the TUI adapter, private payload and domain mapping.

### Architecture

Confirm source-specific knowledge remains in Infrastructure and browser types do
not cross the contract.

### Files Modified

List every created and updated file.

### Payload Contract

Report version, required values and candidate-selection rules.

### Controlled Outcomes

Report how success, incomplete, ambiguous, unsupported, failed and cancelled are
produced.

### Dependency Injection

Report registration, lifetime and resolution behavior.

### Build and Tests

Report exact commands, totals, failures, skipped tests, errors and warnings.

### Network and Scope Check

Confirm no automated external work and no Prompt 036e UI/orchestration behavior
was added.

---

## Results

> Complete during implementation and automated verification.

### Status

Complete. Implementation and automated verification passed, and Robin confirmed
the adapter changes on 16 July 2026.

### Architecture

Infrastructure now owns a stateless `TuiCruisePageCaptureService` implementing
the Application-owned `ICruisePageCaptureService`. Its private JSON DTOs and TUI
support rules remain inside `KrytenAssist.Infrastructure/Cruises/Tui`.

The adapter depends only on Application capture contracts, Core Cruise models
and `System.Text.Json`. No browser, Avalonia, DOM, JavaScript, HTTP or provider
SDK type crosses the capture boundary. The old Prompt 034 HTTP parser/provider
remain unchanged.

### Files Created

- `KrytenAssist.Infrastructure/Cruises/Tui/TuiCruisePageCaptureService.cs`
- `KrytenAssist.Infrastructure/Cruises/Tui/TuiCruiseCaptureServiceCollectionExtensions.cs`
- `KrytenAssist.Avalonia.Tests/Cruises/Tui/TuiCruisePageCaptureServiceTests.cs`
- `KrytenAssist.Avalonia.Tests/Cruises/Tui/TuiCruiseCaptureDependencyInjectionTests.cs`
- `KrytenAssist.Avalonia.Tests/Fixtures/Cruises/Tui/complete-capture.json`

### Files Updated

- `KrytenAssist.Avalonia.Tests/KrytenAssist.Avalonia.Tests.csproj`
- `docs/Codex Prompts/036d - TUI Cruise Capture Adapter.md`

### Payload Contract

The private payload uses version `1` with one `candidates` collection. Missing,
zero, negative or unknown versions are unsupported. Zero candidates are
incomplete, exactly one candidate is validated, and multiple candidates are
ambiguous rather than selecting the first.

Required candidate values are provider offer ID, title, ship name, ISO
`yyyy-MM-dd` departure date, duration of at least one night and at least one
non-negative price with a three-letter currency. Port, itinerary, price basis and
promotion are optional; blank optional strings become null.

The outer payload remains bounded by the 036c 65,536-character request limit.
Unknown JSON properties are ignored, while comments, trailing commas and
polymorphic metadata are not enabled.

### Controlled Outcomes

- Success maps one candidate to a Marella `CruiseProvider`, retaining the
  request's TUI retail source, source reference and timestamp exactly.
- Incomplete reports stable, distinct missing field names for absent or invalid
  required data.
- Ambiguous is returned for multiple candidates.
- Unsupported is returned for the wrong discovery source, retailer, exact host
  or payload version.
- Failed is returned safely for malformed JSON, a null candidate or controlled
  mapping failure without returning raw payload or exception details.
- A pre-cancelled token returns the controlled Cancelled result. Cancellation is
  rechecked before parsing completion and during price validation.

### Dependency Injection

`AddTuiCruiseCapture` registers `ICruisePageCaptureService` to the stateless TUI
adapter as a singleton. Repeated resolutions return the same instance and perform
no parsing, browser creation or network access. The adapter is not injected into
the UI in this prompt; Prompt 036e owns browser orchestration.

### Build

`dotnet build KrytenAssist.sln --no-restore --disable-build-servers -m:1`

- succeeded
- 0 errors
- 5 existing `NU1903` warnings for `SQLitePCLRaw.lib.e_sqlite3` 2.1.11
- no warning introduced by Prompt 036d

### Tests

- Focused TUI adapter and dependency-injection tests: 25 passed, 0 failed, 0
  skipped.
- Avalonia/Application tests: 244 passed, 0 failed, 0 skipped.
- Core tests: 71 passed, 0 failed, 0 skipped.
- API tests: 9 passed, 0 failed, 0 skipped.
- Full solution: 324 passed, 0 failed, 0 skipped.

### Network Check

Verified by implementation scope and test design. The adapter consumes only
in-memory request payloads. The representative fixture is fictional and minimal.
No test or service resolution opened a browser, created an HTTP client or
accessed TUI or another external service.

### Notes

No WebView script, payload-producing browser bridge, capture command, review UI,
external opening, persistence, price history, rating, comparison, AI extraction,
background capture or booking behavior was added. Manual live capture is not
possible until Prompt 036e connects the browser bridge to this adapter.

---

## Lessons Learned

### Implementation

- The old server-rendered Marella HTML parser and the embedded-browser capture
  payload are different source shapes. Keeping separate adapters avoids forcing
  live WebView data through assumptions proven only for Prompt 034 fixtures.
- A small versioned JSON payload allows the browser layer to return bounded
  values without leaking DOM nodes, scripts or complete page HTML inward.
- Candidate ambiguity must be resolved by Robin opening a specific itinerary;
  the adapter must never choose the first cruise silently.
- Source support validation and browsing trust are related but distinct. The
  adapter repeats exact source/retailer/host checks to decide whether it can
  interpret the payload, not to control WebView navigation.
- Mapping optional whitespace to null before invoking Core constructors keeps
  provider payload tolerance outside the domain while preserving Core
  invariants.

### Review

Robin's review passed on 16 July 2026. Prompt 036d is complete.
