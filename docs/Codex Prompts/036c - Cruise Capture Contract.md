# Codex Prompt 036c – Cruise Capture Contract

## Implementation Prompt

Implement Step 3 of:

`docs/AI Playbook/036 - Cruise Discovery and Capture.md`

This prompt follows:

- `docs/Codex Prompts/036a - Embedded Cruise Browser Feasibility.md`
- `docs/Codex Prompts/036b - Cruise Source Navigation.md`

---

## Required Reading

Before changing code, read these files in order:

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/036 - Cruise Discovery and Capture.md`
5. `docs/Codex Prompts/036a - Embedded Cruise Browser Feasibility.md`
6. `docs/Codex Prompts/036b - Cruise Source Navigation.md`
7. all models and tests under `KrytenAssist.Core/Cruises`
8. all existing contracts under `KrytenAssist.Application/Cruises`
9. the 036b Cruise Discovery source definitions and trust policy

Do not begin implementation until the existing Cruise domain invariants and
layer boundaries are understood.

---

## Scope

Implement only Prompt 036c.

Define the provider-independent contract that a later source-specific adapter
will use to transform controlled page data into a Cruise observation.

This step owns:

- the minimal domain representation of the advertising/retail source
- the distinction between cruise operator and retail source
- an application-owned capture request
- an application-owned capture service interface
- explicit controlled capture outcomes
- validation and invariant tests for the new models
- compatibility tests for existing Cruise behavior

This step does **not** own:

- TUI selectors or extraction logic
- JavaScript or DOM inspection
- an implementation of the capture service
- dependency-injection registration for an adapter
- a Capture button or review UI
- persistence or price history
- ratings, preferences or comparisons
- external opening or booking behavior

Do not implement Prompts 036d–036g.

---

## Goal

Create a clean boundary conceptually similar to:

```csharp
public interface ICruisePageCaptureService
{
    Task<CruiseCaptureResult> CaptureAsync(
        CruisePageCaptureRequest request,
        CancellationToken cancellationToken = default);
}
```

The exact type names may follow established project conventions, but the
responsibilities and dependency direction must remain the same.

The contract must allow a later TUI adapter to receive bounded, controlled page
data and return either:

- one valid provider-independent `CruiseObservation`, or
- a precise controlled outcome explaining why no observation was produced

No browser or provider implementation should exist in this step.

---

## Existing Domain Evidence

The existing Core Cruise domain already provides:

- `CruiseProvider` – the cruise operator/provider identity
- `CruiseOffer` – provider offer identity, title, ship, departure and itinerary
- `CruisePrice` – amount, currency and price basis
- `CruiseSnapshot` – offer, prices and optional promotion
- `CruiseObservation` – snapshot, observation time and optional source reference

Preserve these models and their existing semantics.

Do not rename `CruiseProvider`. In the proven Marella/TUI workflow:

```text
CruiseProvider: Marella Cruises
Retail/advertising source: TUI
Source reference: detailed TUI itinerary URL
```

Future workflows may advertise the same Marella sailing through a different
retailer, so the retailer must not be inferred from `CruiseProvider`.

---

## Retail Source Domain Model

Introduce the smallest immutable Core value model needed to identify the
advertising or acquisition source independently of the cruise operator.

Prefer a name such as:

```csharp
CruiseSource
```

It should contain only:

- a stable string identifier
- a display name

Examples are explanatory only:

```text
Id: tui
Name: TUI
```

Do not include:

- a URL or `Uri`
- a trusted host
- browser state
- HTML or selectors
- cookies
- capture-support flags
- Avalonia types

Those remain presentation or adapter concerns.

Apply the same validation conventions as `CruiseProvider`:

- identifier is required and cannot be whitespace
- name is required and cannot be whitespace
- retain the supplied meaningful value rather than silently inventing one

Do not create a source enum.

---

## CruiseObservation Compatibility

Extend `CruiseObservation` only as far as needed to retain the demonstrated
retail source.

The preferred compatible shape is an optional `CruiseSource` property added to
the existing constructor after existing optional parameters, or an equivalent
overload that preserves every current call site.

Requirements:

- existing observations created without a retail source remain valid
- captured observations can carry a non-null retail source
- `SourceReference` continues to represent the page/reference where the values
  were observed
- do not move the source reference into the browser layer
- do not require historic Prompt 033/034 callers to fabricate a retail source
- record equality must continue to include the new source naturally

Do not redesign `CruiseOffer`, `CruiseSnapshot`, `CruisePrice` or
`CruiseProvider`.

---

## Capture Request

Create an immutable request model in `KrytenAssist.Application/Cruises`.

It should carry only transport-neutral data needed by a future adapter, such as:

- the stable Cruise Discovery source identifier
- the retail `CruiseSource`
- the current trusted source reference/address as a string
- the observation timestamp
- a bounded controlled page payload supplied by the browser bridge

The payload may be named `PageContent`, `PagePayload` or similar. It is an opaque
string owned by the source-specific adapter; the shared contract must not claim
that it is always HTML.

The request must not contain:

- `NativeWebView`
- DOM nodes
- JavaScript engine values
- Avalonia controls
- AngleSharp objects
- provider SDK types
- an executable script
- cookies, credentials or request headers

Validation requirements:

- source identifier is required
- retail source is required
- source reference is required and cannot be whitespace
- source reference must be an absolute HTTPS address
- page payload is required and cannot be whitespace
- page payload has a documented finite maximum length
- an over-limit payload is rejected before reaching an adapter
- timestamp is retained exactly

Choose a conservative limit large enough for a controlled structured payload but
small enough to prevent accidentally passing an entire unbounded browsing
session. Define the limit once as a named constant and test its boundary.

Do not validate TUI hostnames in the application contract. The 036b presentation
policy owns navigation trust; the future TUI adapter owns whether a trusted
source and payload are supported.

---

## Capture Outcomes

Represent capture outcomes explicitly. Do not use `null`, exceptions or free-form
messages as the only outcome model.

The result must distinguish at least:

- `Success` – exactly one complete observation was produced
- `Incomplete` – required fields were missing
- `Ambiguous` – multiple conflicting candidates or values were found
- `Unsupported` – source or page shape is not supported
- `Failed` – a controlled adapter failure occurred
- `Cancelled` – capture was cancelled without fabricating a result

An enum plus an immutable result model is acceptable.

The result should expose:

- status/outcome
- `CruiseObservation` only for success
- a safe user-facing message for non-success outcomes
- a bounded list of missing field names for `Incomplete`, if useful

Enforce invariants through constructors or named factories:

- success requires a non-null observation
- success cannot contain a failure message or missing fields
- non-success cannot contain an observation
- incomplete may contain a distinct, non-empty bounded list of missing fields
- statuses other than incomplete cannot contain missing fields
- non-success requires a safe non-empty message
- missing field names cannot be null, empty or whitespace
- raw exceptions, HTML and JavaScript must not be stored in the result

Prefer named factories such as `Succeeded`, `Incomplete`, `Ambiguous`,
`Unsupported`, `Failed` and `Cancelled` when they make invalid combinations
impossible.

Do not expose exception objects in the application result.

---

## Capture Service Interface

Add the provider-independent interface to `KrytenAssist.Application/Cruises`.

The interface should:

- accept the validated capture request
- return the controlled capture result asynchronously
- accept an optional `CancellationToken`
- contain no provider or browser types

Do not implement the interface in this prompt.

Do not register it with dependency injection until Prompt 036d supplies the
first implementation.

Do not modify the existing `ICruiseOfTheWeekProvider` contract.

---

## Cancellation Semantics

The result model must be capable of representing a controlled cancelled outcome
because the later user workflow needs a distinct state.

The future adapter may still honour `CancellationToken` by throwing
`OperationCanceledException`; orchestration can translate that into the
cancelled result at the appropriate boundary. Prompt 036c should document the
semantics in XML documentation or tests without implementing orchestration.

Do not catch cancellation in an interface or add a concrete service solely to
exercise it.

---

## Layering

Expected dependency direction:

```text
KrytenAssist.Core
    CruiseSource
    CruiseObservation extension
        ↑
KrytenAssist.Application
    CruisePageCaptureRequest
    CruiseCaptureStatus / CruiseCaptureResult
    ICruisePageCaptureService
```

Core must not reference Application, Infrastructure or Avalonia.

Application may reference Core and must remain independent of Infrastructure and
Avalonia.

Do not add package references for HTML parsing, WebView access or TUI.

---

## Tests

Add deterministic tests in the appropriate existing test projects.

### Core Tests

Cover:

- valid `CruiseSource` construction
- required identifier and name validation
- value equality
- an existing `CruiseObservation` remains constructible without a source
- a captured observation retains its retail source and source reference
- equality reflects different retail sources

### Application Contract Tests

There is currently no dedicated Application test project. Follow the existing
solution testing convention rather than creating a new project solely for this
prompt. Place provider-independent contract tests in the smallest existing test
project that already references Application and Core, and keep their namespace
and folder clearly application-oriented.

Cover:

- a valid request retains every value
- missing source identifier, source, source reference or payload is rejected
- a non-HTTPS or relative source reference is rejected
- payload exactly at the maximum is accepted
- payload above the maximum is rejected
- success requires and exposes one observation
- every non-success status rejects an observation
- incomplete retains distinct validated missing fields
- invalid or excessive missing-field collections are rejected
- other failures cannot carry missing fields
- messages are required for non-success outcomes
- raw exception details are not needed by the contract

Interface tests should use a tiny fake only when needed to prove the signature
and cancellation token can be consumed. Do not create a production adapter.

Existing Prompt 033 and 034 Cruise tests must continue to pass unchanged unless
the compatible `CruiseObservation` constructor addition requires an intentional
assertion update.

No test may access TUI or another external website.

---

## Verification Commands

After implementation, run:

```bash
dotnet restore KrytenAssist.sln
dotnet build KrytenAssist.sln --no-restore
dotnet test KrytenAssist.Core.Tests/KrytenAssist.Core.Tests.csproj --no-build --no-restore
dotnet test KrytenAssist.Avalonia.Tests/KrytenAssist.Avalonia.Tests.csproj --no-build --no-restore
dotnet test KrytenAssist.sln --no-build --no-restore
```

Report exact totals, failures, skipped tests, errors and warnings.

Do not suppress compiler, compatibility or vulnerability warnings.

---

## Documentation

After implementation and verification:

- complete this prompt's `Results` section
- list every created and updated file
- describe the operator/source distinction
- record the exact request payload bound
- record exact build and test totals
- confirm no external network access occurred
- leave the main Prompt 036 Playbook and Roadmap unchanged until Robin reviews
  the 036c implementation

Do not mark Prompt 036 complete.

---

## Explicitly Out of Scope

Do not implement:

- TUI capture selectors
- parsing the Canarian Flavours URL or payload
- HTML fixtures or a TUI adapter
- browser-to-request orchestration
- Capture Displayed Cruise UI
- capture review presentation
- dependency-injection registration for capture
- persistence or migrations
- price history
- ratings or preferences
- source comparison
- external browser opening
- booking, authentication or payment handling
- unattended or scheduled browsing

Do not add a convenience implementation that silently performs Prompt 036d.

---

## Completion Criteria

Prompt 036c is complete when:

- operator and advertising/retail source can be represented independently
- existing Cruise observations remain compatible
- captured observations retain retail source and source reference
- the application owns a browser-neutral capture request
- the request payload is validated and bounded
- the application owns explicit capture statuses and an invariant-safe result
- the application owns an asynchronous cancellable capture interface
- no capture implementation, TUI parsing or UI is added
- no browser, DOM, JavaScript, provider SDK or Avalonia type leaks inward
- deterministic tests cover all new invariants
- the solution builds and all tests pass
- this prompt's Results section is complete

Stop after Prompt 036c.

---

## Completion Report

When complete, provide:

### Summary

Describe the provider-independent boundary and the operator/source distinction.

### Architecture

List the Core and Application responsibilities and confirm dependency direction.

### Files Modified

List every created and updated file.

### Contract Invariants

Report request validation, maximum payload length and result invariants.

### Compatibility

Confirm existing Cruise models, providers and observations remain usable.

### Build and Tests

Report commands, totals, failures, skipped tests, errors and warnings.

### Network Check

Confirm no test or implementation accessed an external website.

### Scope Check

Confirm no TUI adapter, extraction, UI, persistence or later Prompt 036 behavior
was added.

---

## Results

> Complete during implementation and automated verification.

### Status

Complete. Implementation and automated verification passed, and Robin confirmed
the contract changes on 16 July 2026.

### Architecture

Core now distinguishes the cruise operator (`CruiseProvider`) from the
advertising/retail source (`CruiseSource`). `CruiseObservation` can retain that
source independently of its source reference while remaining compatible with
existing callers.

Application owns the transport-neutral capture request, explicit result status
and invariant-safe result factories, plus the asynchronous cancellable capture
interface. Core and Application contain no browser, DOM, JavaScript, Avalonia,
HTML-parser or provider SDK types.

### Files Created

- `KrytenAssist.Core/Cruises/CruiseSource.cs`
- `KrytenAssist.Application/Cruises/CruisePageCaptureRequest.cs`
- `KrytenAssist.Application/Cruises/CruiseCaptureStatus.cs`
- `KrytenAssist.Application/Cruises/CruiseCaptureResult.cs`
- `KrytenAssist.Application/Cruises/ICruisePageCaptureService.cs`
- `KrytenAssist.Core.Tests/Cruises/CruiseSourceTests.cs`
- `KrytenAssist.Avalonia.Tests/Application/Cruises/CruisePageCaptureRequestTests.cs`
- `KrytenAssist.Avalonia.Tests/Application/Cruises/CruiseCaptureResultTests.cs`
- `KrytenAssist.Avalonia.Tests/Application/Cruises/CruisePageCaptureServiceContractTests.cs`

### Files Updated

- `KrytenAssist.Core/Cruises/CruiseObservation.cs`
- `KrytenAssist.Core.Tests/Cruises/CruiseObservationTests.cs`
- `docs/Codex Prompts/036c - Cruise Capture Contract.md`

### Contract Invariants

- A request requires a source identifier, retail `CruiseSource`, absolute HTTPS
  source reference with a host, observation timestamp and non-blank page payload.
- Page payloads are bounded at exactly 65,536 characters; the boundary is
  accepted and an additional character is rejected.
- Results distinguish Success, Incomplete, Ambiguous, Unsupported, Failed and
  Cancelled.
- Success requires exactly one observation and contains no message or missing
  fields.
- Non-success results contain no observation and require a safe non-blank
  message.
- Incomplete results require 1–16 distinct, non-blank field names. Other failure
  statuses cannot carry missing fields.
- No exception, HTML, JavaScript or browser object is exposed by the result.

### Compatibility

The existing `CruiseObservation` constructor remains source-compatible because
the new `CruiseSource` parameter is optional and follows the existing optional
source reference. Existing Prompt 033/034 observations, providers, parsers and
Skills need not fabricate a retailer. Record equality naturally includes a
supplied retail source.

### Build

`dotnet build KrytenAssist.sln --no-restore --disable-build-servers -m:1`

- succeeded
- 0 errors
- 7 existing warnings: 5 `NU1903` SQLite package vulnerability warnings and 2
  `CS0067` unused command-event warnings in `MainWindowViewModel`
- no warning introduced by Prompt 036c

### Tests

- Focused Core source/observation tests: 16 passed, 0 failed, 0 skipped.
- Focused Application request/result/interface tests: 22 passed, 0 failed, 0
  skipped.
- Core tests: 71 passed, 0 failed, 0 skipped.
- Avalonia/Application tests: 219 passed, 0 failed, 0 skipped.
- API tests: 9 passed, 0 failed, 0 skipped.
- Full solution: 299 passed, 0 failed, 0 skipped.

### Network Check

Verified through implementation scope and test design. No production capture
implementation exists, and all tests construct models or use one in-memory test
fake. No test opened a browser or accessed an external website.

### Notes

No TUI adapter, selector, parser, HTML fixture, browser orchestration, capture UI,
review UI, dependency-injection registration, persistence, price history,
rating, comparison or booking behavior was added.

---

## Lessons Learned

### Implementation

- `CruiseProvider` and `CruiseSource` represent different business facts and
  should remain separate even when one company currently appears to fill both
  roles.
- Adding the optional retail source at the end of `CruiseObservation` preserved
  all existing callers while allowing captured observations to retain their
  acquisition context.
- Named result factories make invalid success/failure combinations impossible
  without exposing constructors that later adapters could misuse.
- Bounding the transport-neutral payload at the Application boundary prevents a
  future browser bridge from accidentally passing an unlimited page or browsing
  session inward.
- Navigation trust and capture support are distinct checks: 036b decides whether
  a page may be browsed, while a later source adapter decides whether its bounded
  payload can be interpreted.

### Review

Robin's review passed on 16 July 2026. Prompt 036c is complete.
