# Codex Prompt 037h-b – TUI Multi-Card Capture Adapter

## Implementation Prompt

Implement **Step 2 only** from:

```text
docs/AI Playbook/037h - Multiple Cruise Deals Handling.md
```

Prompt 037h-a is complete and committed as `726804d`.

The verified solution baseline is:

```text
Core: 105 passed
Avalonia: 370 passed
API: 9 passed
Total: 484 passed, 0 failed, 0 skipped
```

This step implements the fixed bounded TUI multi-card extraction boundary and
the Infrastructure adapter behind the Application batch contract.

Do not implement multi-candidate presentation, selection, batch recording or
final live verification.

Do not begin Prompt 038.

---

## Required Reading

Read these files in order before changing code:

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/036 - Cruise Discovery and Capture.md`
5. `docs/AI Playbook/037 - Cruise History and Price Tracking.md`
6. `docs/AI Playbook/037h - Multiple Cruise Deals Handling.md`
7. `docs/Codex Prompts/036d - TUI Cruise Capture Adapter.md`, including Results
   and Lessons Learned
8. `docs/Codex Prompts/037h-a - Multi-Cruise Capture Contract.md`, including
   Results and Lessons Learned
9. the new Application batch status, result and service contract files
10. `KrytenAssist.Infrastructure/Cruises/Tui/TuiCruisePageCaptureService.cs`
11. `KrytenAssist.Infrastructure/Cruises/Tui/TuiCruiseCaptureServiceCollectionExtensions.cs`
12. `KrytenAssist.Avalonia/Cruises/Discovery/TuiCruiseCaptureScript.cs`
13. the existing TUI capture, script and dependency-injection tests
14. the existing single-capture Application contract and tests

Do not begin implementation until the current single-card behavior, the
037h-a batch invariants, the browser-script security boundary and the known
product-card scoping defect are understood.

---

## Goal

Implement a bounded TUI adapter that converts one versioned multi-card browser
payload into an ordered provider-independent `CruiseCaptureBatchResult`.

The implementation must:

- scope every extracted candidate to its own demonstrated TUI product card
- prevent page-wide ship, price or promotion evidence leaking between cards
- preserve exact loaded-card order
- retain one candidate-specific trusted itinerary reference per card
- deduplicate repeated exact itinerary links
- return no more than ten candidates
- report when more loaded candidates existed than were returned
- map complete candidates independently to Ready observations
- retain identifiable incomplete candidates with controlled missing fields
- isolate candidate-local mapping failures
- preserve the existing single Cruise of the Week capture path
- remain offline, bounded, fixed and read-only

This step creates the adapter foundation only. It does not expose the batch in
the Avalonia presentation yet.

---

## Scope

This step owns:

- exact `[data-testid="product-card"]` scoping in the fixed browser script
- bounded versioned multi-card payload output
- candidate-specific itinerary references in that payload
- explicit payload truncation evidence
- TUI batch payload validation and mapping in Infrastructure
- implementation of `ICruisePageBatchCaptureService`
- independent Ready, Incomplete and Failed candidate mapping
- trusted TUI candidate-address validation
- preservation of the existing `ICruisePageCaptureService` behavior
- dependency-injection registration for both capture interfaces
- deterministic offline adapter, script and DI tests
- this prompt's Results and Lessons Learned

This step does **not** own:

- changing the Application batch contract
- changing Core Cruise models
- ViewModels or views
- multi-candidate review presentation
- selection state or selection commands
- Open at TUI presentation behavior
- Record Selected or Record All Observations
- Cruise History orchestration or persistence
- SQLite schema or migrations
- live TUI verification
- Roadmap or Playbook Results updates
- Session Handover updates
- Prompt 038 behavior

Do not create Avalonia presentation placeholders.

---

## Allowed Changes

Production changes should be limited to:

```text
KrytenAssist.Infrastructure/Cruises/Tui/
KrytenAssist.Avalonia/Cruises/Discovery/TuiCruiseCaptureScript.cs
```

Tests may be created or updated under:

```text
KrytenAssist.Avalonia.Tests/Cruises/Tui/
KrytenAssist.Avalonia.Tests/ViewModels/
KrytenAssist.Avalonia.Tests/Fixtures/Cruises/Tui/
```

Update this prompt after implementation:

```text
docs/Codex Prompts/037h-b - TUI Multi-Card Capture Adapter.md
```

Do not modify:

- Application production contracts
- Core production types
- Avalonia ViewModels or views
- API
- persistence code, migrations or database schema
- Roadmap
- Playbook 037h Results
- Session Handovers

Production changes outside the expected files are permitted only when a
required deterministic test proves they are necessary for this step. Report
every such correction explicitly.

Do not stage, commit, push, discard or overwrite Robin's work.

---

## Architecture Boundary

Preserve:

```text
KrytenAssist.Core
        ↑
KrytenAssist.Application
        ↑
KrytenAssist.Infrastructure

KrytenAssist.Avalonia browser boundary
        ↓ fixed bounded JSON payload
KrytenAssist.Infrastructure adapter
```

Provider-specific payload types, TUI hosts, path validation and JSON mapping
belong in Infrastructure. Fixed JavaScript belongs at the existing Avalonia
browser boundary.

Do not expose outside those boundaries:

- TUI payload types
- selectors
- HTML or DOM types
- JavaScript
- `NativeWebView`
- browser cookies or storage
- HTTP response types
- opaque package identifiers as domain identity

The adapter must not depend on Avalonia or browser types. The browser script
must not depend on Infrastructure types.

---

## Preserve the Existing Single-Capture Path

The current `TuiCruisePageCaptureService` implements:

```text
ICruisePageCaptureService
```

Preserve that interface and its verified behavior.

The same Infrastructure implementation may implement both:

```text
ICruisePageCaptureService
ICruisePageBatchCaptureService
```

Prefer one shared private payload validator and candidate mapper where that
reduces duplication without weakening either contract.

The existing single interface must continue to:

- map one complete candidate successfully
- return controlled Incomplete, Unsupported, Failed and Cancelled results
- return Ambiguous when a valid payload contains multiple candidates
- use the existing bounded `CruisePageCaptureRequest`
- preserve all existing test behavior

Do not replace the single interface with the batch interface. Prompt 037h-c
will adopt the batch interface explicitly.

---

## Fixed Script Correction

The current script can choose this generic ancestor:

```javascript
closest('article,[data-testid],[data-cruise-card],li')
```

On the demonstrated Voyager page, that can select:

```text
<h3 data-testid="resort-name">
```

rather than the complete Cruise card.

Correct candidate scoping so extraction begins from a trusted itinerary link
and locates its nearest demonstrated card:

```javascript
closest('[data-testid="product-card"]')
```

Only links associated with a supported product card should form listing-page
batch candidates.

All candidate evidence must be read from that exact card:

- itinerary title
- ship name
- departure date
- duration
- departure port when present
- displayed price evidence
- total price evidence
- promotion evidence
- exact itinerary reference

Remove page-wide ship, price and promotion fallbacks that could copy the first
card's evidence into later candidates.

A narrowly demonstrated candidate-specific URL field such as `shipCode` may
support a controlled ship map because it belongs to that candidate. Do not use
page-wide text as a substitute.

If retaining compatibility for a supported single itinerary page requires a
separate exact demonstrated container path, keep it explicit and test it. Do not
restore a generic page-wide fallback.

---

## Script Security and Bounds

The script must remain:

- fixed application-owned code
- read-only
- explicitly user-triggered
- limited to the demonstrated `tui-product-cards` roots
- bounded to at most three open product-card shadow roots
- bounded to at most ten published unique candidates
- bounded to existing safe field lengths
- deterministic in loaded-card order

It must not:

- accept arbitrary JavaScript
- click links, View Deal or Load More
- navigate
- submit forms
- focus controls
- scroll automatically
- change filters or sort
- mutate the DOM
- recursively crawl arbitrary shadow roots
- read cookies, local storage or session storage
- return full HTML
- perform external requests

Retain exact-link deduplication. Two links inside one card that resolve to the
same exact itinerary URL must produce one candidate.

Inspect enough bounded unique links to determine whether the published ten were
truncated. Do not silently discard truncation evidence.

---

## Payload Shape

Keep the payload versioned and bounded. Extend the existing private payload with
only the evidence required by the Application batch contract, including:

```text
version
wasTruncated
candidates[]
    sourceReference
    providerOfferId
    title
    shipName
    departureDate
    durationNights
    departurePort
    itinerarySummary
    prices[]
    promotionSummary
```

Keeping version 1 with additive fields is preferred if it preserves the proven
single path cleanly. Increase the version only if deterministic compatibility
tests demonstrate that accepting both shapes is safer. Report that decision.

The script should emit the exact trusted itinerary URL as `sourceReference` for
each candidate. The shared listing-page reference remains on
`CruisePageCaptureRequest`.

Do not emit DOM nodes, HTML, cookies, storage, personal data or unrelated page
content.

---

## TUI Source and Candidate Trust

Retain validation of the request source:

- supported source capability identifier
- TUI retail source identity
- absolute HTTPS request address
- exact supported TUI host

Validate every candidate source reference independently:

- non-null and nonblank
- bounded before mapping
- absolute HTTPS
- exact `www.tui.co.uk` host
- supported Cruise itinerary path
- required itinerary identity evidence in the path or query
- no trust by suffix or substring matching

The Application contract deliberately accepts any absolute HTTPS reference.
Exact TUI host and path trust belongs here in Infrastructure.

An untrusted or malformed candidate address must never become a Ready
observation. Return an honest candidate-local Failed or Incomplete result,
depending on whether the candidate can still be identified safely.

Do not include rejected address details in user-facing failure messages.

---

## Batch Mapping Rules

Implement:

```text
ICruisePageBatchCaptureService.CaptureAsync
```

The batch path must:

1. reject or cancel before publishing partial candidates when pre-cancelled
2. validate the request source
3. deserialize the bounded versioned payload safely
4. validate payload version and candidate collection
5. return batch Incomplete when no supported candidate exists
6. enforce the maximum of ten published candidates independently of the script
7. retain deterministic payload order
8. process each candidate independently
9. retain exact candidate itinerary references
10. compute `WasTruncated` from controlled payload evidence
11. return a Completed batch containing the candidate results

A valid multi-card payload is expected and must not become Ambiguous.

### Ready Candidate

A complete trusted candidate should map to one `CruiseObservation` with:

- Marella Cruises as operator
- TUI as retail source
- the request's fixed observed timestamp
- the candidate's exact trusted itinerary source reference
- the existing neutral Cruise offer, snapshot, price and promotion mapping

The observation and candidate source references must agree exactly so the
Application Ready factory accepts them.

### Incomplete Candidate

When a candidate can be identified safely but required Cruise evidence is
missing or invalid, return an Incomplete candidate with:

- a safe display label
- its trusted candidate reference
- a bounded safe message
- the existing stable controlled missing-field names
- no observation

One incomplete card must not prevent neighbouring complete cards from becoming
Ready.

### Failed Candidate

When one identifiable candidate cannot be mapped safely for a candidate-local
reason, return a Failed candidate with a safe message and no observation.

Do not leak JSON, exception details, query parameters or internal parsing
information into messages.

### Batch-Wide Results

Use batch-wide states only for batch-wide conditions:

- Incomplete: no supported candidates were supplied
- Unsupported: request source, page type or payload version is unsupported
- Failed: malformed payload or unexpected batch-wide read failure
- Cancelled: capture was cancelled and no partial candidates are published

A Completed batch may contain any mixture of Ready, Incomplete and Failed
candidates, including no Ready candidates.

---

## Duplicate and Bound Behavior

The script should collapse repeated exact itinerary links while preserving the
first card occurrence.

Infrastructure must not trust the script blindly.

Define and test deterministic behavior for payload duplicates:

- exact duplicate candidate references must not produce two observations
- prefer retaining the first occurrence in payload order when safely possible
- otherwise return a controlled batch-wide failure rather than violating the
  Application batch invariant

The adapter must never return more than:

```text
CruiseCaptureBatchResult.MaximumCandidateCount
```

If a payload itself contains more than the allowed published maximum, reject it
as invalid rather than silently trusting or trimming hostile input.

`WasTruncated` describes browser evidence that additional loaded unique cards
existed beyond the returned bound. It does not permit an oversized payload.

---

## Cancellation

Honor cancellation:

- before deserialization where practical
- before candidate processing
- between candidates
- before publishing the completed batch

Return the controlled Cancelled batch result with no candidates when
cancellation is observed.

Do not publish partially processed candidates on cancellation in this step.
Do not use sleeps, polling or UI-thread blocking.

---

## Dependency Injection

Update the existing TUI capture registration so both interfaces resolve:

```text
ICruisePageCaptureService
ICruisePageBatchCaptureService
```

Both should resolve to the same singleton `TuiCruisePageCaptureService`
instance. Do not register two independently created singleton instances.

Keep registration inside the existing Infrastructure extension method. Do not
add manual registrations to `Program.cs`.

---

## Price Boundary

Retain the same neutral price evidence supported by the current single-card
workflow:

- current per-person price where exactly demonstrated
- total price where exactly demonstrated
- existing basis labels
- promotion summary where exactly demonstrated

Do not redesign the Cruise price domain in this step.

Do not infer or add explicit original-price, discounted-price,
per-person-discount or booking-level-discount semantics. That remains a future
model correction.

Most importantly, never use one card's price evidence for another card.

---

## Test Requirements

Use xUnit and existing TUI adapter-test conventions.

### Batch Adapter Tests

Add deterministic tests proving:

- two complete fictional candidates become two Ready results in exact order
- each Ready observation retains its own exact itinerary reference
- operator remains Marella Cruises and retail source remains TUI
- fixed observation timestamp and existing neutral price mapping are retained
- a Ready candidate beside an Incomplete candidate produces partial success
- a Ready candidate beside a Failed candidate remains available
- an all-Incomplete/all-Failed set still produces a Completed batch
- stable missing-field names are retained independently per candidate
- `WasTruncated` true and false are preserved
- an empty candidate collection returns batch Incomplete
- malformed JSON returns a safe batch Failed result
- missing or unsupported payload versions return Unsupported
- unsupported request source, retailer or host returns Unsupported
- candidate references reject null, blank, relative, HTTP, foreign-host and
  unsupported-path values
- an invalid candidate address never becomes Ready
- an oversized payload is rejected safely and never silently trimmed
- exact duplicate payload references are handled deterministically without
  duplicate observations
- null candidate elements are controlled
- pre-cancelled and cancellation observed during processing return Cancelled
  with no candidates
- messages do not expose hostile payload fragments or address details

### Single-Capture Regression Tests

Preserve and rerun tests proving:

- one complete fictional payload maps successfully
- existing incomplete/malformed/unsupported/cancelled behavior remains stable
- a multi-candidate payload remains Ambiguous through
  `ICruisePageCaptureService`
- the single observation source reference behavior remains explicitly tested

If the additive candidate source reference legitimately changes the single
observation from the listing reference to the exact itinerary reference, add a
focused regression test and document the compatibility decision. Do not change
this silently.

### Fixed Script Regression Tests

Test the fixed script as fixed text without executing JavaScript.

Prove that it:

- contains exact `[data-testid="product-card"]` scoping
- no longer contains the unsafe generic closest selector
- emits candidate `sourceReference`
- emits `wasTruncated`
- deduplicates exact itinerary URLs
- preserves a ten-candidate publication bound
- inspects no more than three demonstrated product-card roots
- retains bounded fields
- contains no page-wide ship, price or promotion reuse
- contains no cookie or browser-storage reads
- contains no full HTML reads
- contains no click, navigation, submission, focus, scroll or external request
- still returns versioned JSON

Avoid brittle assertions against formatting alone. Assert meaningful security,
scope and payload fragments.

### Dependency-Injection Tests

Prove:

- both Application capture interfaces resolve
- both resolve to the same singleton adapter instance
- repeated resolutions remain stable

### Test Data

Use:

- small fictional bounded JSON payloads
- fictional TUI itinerary paths and query values
- fixed timestamps with a non-zero offset
- exact assertions
- hand-written inputs and services

Do not use:

- live HTTP or DNS
- the live TUI page
- `NativeWebView`
- a JavaScript engine
- browser automation
- full captured HTML
- live opaque package identifiers
- cookies, credentials or personal booking information
- `DateTimeOffset.Now` or `UtcNow`
- mocking libraries
- reflection
- mutable shared fixtures

---

## Expected Files

Expected production updates:

```text
KrytenAssist.Infrastructure/Cruises/Tui/TuiCruisePageCaptureService.cs
KrytenAssist.Infrastructure/Cruises/Tui/TuiCruiseCaptureServiceCollectionExtensions.cs
KrytenAssist.Avalonia/Cruises/Discovery/TuiCruiseCaptureScript.cs
```

Expected test changes may include:

```text
KrytenAssist.Avalonia.Tests/Cruises/Tui/TuiCruisePageBatchCaptureServiceTests.cs
KrytenAssist.Avalonia.Tests/Cruises/Tui/TuiCruisePageCaptureServiceTests.cs
KrytenAssist.Avalonia.Tests/Cruises/Tui/TuiCruiseCaptureDependencyInjectionTests.cs
KrytenAssist.Avalonia.Tests/Cruises/Tui/TuiCruiseCaptureScriptTests.cs
KrytenAssist.Avalonia.Tests/Fixtures/Cruises/Tui/*.json
```

Updating the existing script test in
`CruiseCaptureReviewViewModelTests.cs` is acceptable if moving those assertions
would create unrelated churn. Prefer a focused script test file if the expanded
coverage is clearer.

Do not create presentation or recording production files.

---

## Production Correction Policy

The expected production changes are limited to the TUI adapter, its DI
registration and the fixed script.

If a required deterministic test exposes a genuine defect elsewhere:

1. retain the focused failing regression test
2. make the smallest in-scope correction
3. preserve every verified existing behavior
4. rerun focused single and batch capture tests
5. report the correction explicitly

Do not modify Application or Core types merely to share validation code.

---

## Required Commands

Inspect and preserve the initial worktree.

Run focused tests for:

- new TUI batch capture
- existing TUI single capture
- fixed script security and bounds
- TUI dependency injection
- Application single and batch capture contracts

Use an explicit test filter and record the exact command and totals.

Build the solution:

```text
dotnet build KrytenAssist.sln --no-restore
```

Run the complete regression suite:

```text
dotnet test KrytenAssist.sln --no-build --no-restore
```

Report exact totals, failures, skipped tests, errors and warnings.

All automated tests must remain offline.

---

## Definition of Done

Prompt 037h-b is complete when:

- the fixed script scopes every batch candidate to its exact product card
- unsafe page-wide ship, price and promotion reuse is removed
- exact itinerary links are deduplicated in deterministic card order
- the script publishes at most ten candidates and reports truncation
- each payload candidate contains its own itinerary reference
- the TUI adapter implements the separate batch service
- request source and every candidate address are trusted independently
- complete candidates map to Ready observations
- incomplete and failed candidates do not invalidate neighbouring candidates
- Completed batches may contain no Ready candidates
- observation and candidate references agree exactly
- oversized and malformed payloads are controlled
- cancellation publishes no partial candidates
- the existing single-capture behavior remains compatible
- both capture interfaces resolve to the same singleton adapter
- focused offline tests pass
- the complete solution builds
- the complete regression suite passes
- this prompt's Results and Lessons Learned are complete

Do not begin 037h-c.

Stop after 037h-b.

---

## Completion Report

Provide:

### Summary

Describe the product-card script correction and batch adapter behavior.

### Single-Capture Compatibility

Confirm the existing single service remains available and report any deliberate
source-reference compatibility decision.

### Security and Bounds

Report exact card scoping, trusted-address validation, deduplication, candidate
maximum and truncation behavior.

### Files Modified

List every created and updated file.

### Production Corrections

Use `None` or report every test-proven correction outside the expected files.

### Build and Tests

Report exact commands, totals, failures, skipped tests, errors and warnings.

### Architecture and Scope

Confirm no Application/Core redesign, presentation, selection, recording,
persistence, live automation or Prompt 038 behavior was added.

---

## Results

> Complete during implementation.

### Status

Complete.

### Fixed Script Correction

The fixed script now begins from itinerary links inside at most three open
`tui-product-cards` shadow roots and requires the exact nearest
`[data-testid="product-card"]`. It publishes the first ten unique itinerary
links in loaded-card order, reports `wasTruncated`, emits each exact bounded
source reference and reads ship, price and promotion evidence only from that
candidate card. The unsafe generic closest selector and page-wide evidence
fallbacks were removed.

### Batch Adapter

`TuiCruisePageCaptureService` now implements both the existing single capture
interface and the new batch capture interface. The batch path validates the
versioned payload, enforces the ten-candidate maximum, deduplicates exact
references by first occurrence and independently maps Ready, Incomplete and
Failed candidates into a Completed batch. Batch-wide malformed, unsupported,
empty, oversized and cancelled conditions remain controlled and publish no
partial candidates.

### Candidate Trust and Isolation

Every batch candidate requires an absolute HTTPS reference on the exact
`www.tui.co.uk` host, under `/cruise/bookitineraries/`, with nonblank itinerary
identity query evidence. Ready observations retain that exact candidate
reference. Missing required fields remain candidate-local Incomplete evidence;
oversized optional evidence becomes a candidate-local Failed result, preventing
one invalid card from contaminating its neighbours.

### Single-Capture Compatibility

The existing `ICruisePageCaptureService` remains available and retains its
verified one-candidate mapping, controlled failures and multi-candidate
Ambiguous behavior. Payload version 1 was retained with additive
`sourceReference` and `wasTruncated` fields. The legacy single observation
deliberately continues to use the request/page source reference; only the new
batch path requires candidate itinerary references.

### Dependency Injection

The existing extension now registers one concrete singleton and maps both
`ICruisePageCaptureService` and `ICruisePageBatchCaptureService` to that exact
instance. Focused tests prove repeated single resolutions, the batch resolution
and the concrete resolution are identical.

### Files Created

- `KrytenAssist.Avalonia.Tests/Cruises/Tui/TuiCruisePageBatchCaptureServiceTests.cs`
- `docs/Codex Prompts/037h-b - TUI Multi-Card Capture Adapter.md`

### Files Updated

- `KrytenAssist.Infrastructure/Cruises/Tui/TuiCruisePageCaptureService.cs`
- `KrytenAssist.Infrastructure/Cruises/Tui/TuiCruiseCaptureServiceCollectionExtensions.cs`
- `KrytenAssist.Avalonia/Cruises/Discovery/TuiCruiseCaptureScript.cs`
- `KrytenAssist.Avalonia.Tests/Cruises/Tui/TuiCruiseCaptureDependencyInjectionTests.cs`
- `KrytenAssist.Avalonia.Tests/ViewModels/CruiseCaptureReviewViewModelTests.cs`
- `KrytenAssist.Avalonia.Tests/Fixtures/Cruises/Tui/complete-capture.json`

### Production Corrections

None outside the expected adapter, script, DI and test scope.

### Build

Passed:

```text
dotnet build KrytenAssist.sln --no-restore
```

Result: 0 errors and 5 warnings. All warnings are the existing NU1903 advisory
for `SQLitePCLRaw.lib.e_sqlite3` 2.1.11; no compiler warning was introduced.

### Focused Tests

Passed:

```text
dotnet test KrytenAssist.Avalonia.Tests/KrytenAssist.Avalonia.Tests.csproj --no-restore --filter "FullyQualifiedName~TuiCruisePageBatchCaptureServiceTests|FullyQualifiedName~TuiCruisePageCaptureServiceTests|FullyQualifiedName~TuiCruiseCaptureDependencyInjectionTests|FullyQualifiedName~CruiseCaptureReviewViewModelTests|FullyQualifiedName~CruiseCaptureCandidateResultTests|FullyQualifiedName~CruiseCaptureBatchResultTests|FullyQualifiedName~CruisePageBatchCaptureServiceContractTests|FullyQualifiedName~CruisePageCaptureRequestTests|FullyQualifiedName~CruiseCaptureResultTests|FullyQualifiedName~CruisePageCaptureServiceContractTests"
```

Result: 106 passed, 0 failed, 0 skipped. The command reported the existing
NU1903 advisory and two existing unused command-event compiler warnings.

### Complete Regression Suite

Passed:

```text
dotnet test KrytenAssist.sln --no-build --no-restore
```

Results:

- Core: 105 passed, 0 failed, 0 skipped
- Avalonia: 392 passed, 0 failed, 0 skipped
- API: 9 passed, 0 failed, 0 skipped
- Total: 506 passed, 0 failed, 0 skipped

### Offline Check

All tests use fictional bounded JSON, fictional itinerary addresses, fixed
timestamps and direct in-memory adapter calls. No live HTTP, DNS, TUI page,
browser, JavaScript engine, cookies, storage or personal data was used.

### Architecture and Scope Check

Changes are limited to the existing Avalonia fixed-script boundary, the TUI
Infrastructure adapter and registration, focused tests, a fictional fixture and
this prompt. No Application/Core redesign, ViewModel production behavior,
views, selection, recording, persistence, live automation, Roadmap, Playbook
Results, Session Handover or Prompt 038 behavior was added.

### Notes

The browser payload remains version 1 because the new fields are additive and
the legacy single path remains compatible. Exact duplicate payload references
retain the first occurrence deterministically. Structurally untrusted candidate
references fail the batch safely because the Application candidate contract
cannot honestly publish a malformed itinerary reference.

---

## Lessons Learned

> Complete after implementation.

- Exact product-card scoping is the essential safety boundary: once every value
  comes from the card containing its itinerary link, valid and incomplete cards
  can coexist without cross-card evidence leakage.
- One adapter can safely serve the legacy single and new batch contracts when
  their orchestration is kept separate and candidate mapping is shared only
  below those result boundaries.
- Additive payload evidence avoided an unnecessary version change while still
  making candidate itinerary identity and truncation explicit.
- Infrastructure must repeat script bounds and trust checks because fixed
  browser code is a convenience boundary, not a reason to trust incoming JSON.
- A malformed candidate reference cannot be represented honestly as an
  Application candidate, so it is safer to reject that structurally invalid
  batch than fabricate a trusted itinerary address.
