# Codex Prompt 037h-a – Multi-Cruise Capture Contract

## Implementation Prompt

Implement **Step 1 only** from:

```text
docs/AI Playbook/037h - Multiple Cruise Deals Handling.md
```

Prompt 037 is complete and committed through verification as `0edcac4`.

The verified solution baseline is:

```text
Core: 105 passed
Avalonia: 336 passed
API: 9 passed
Total: 450 passed, 0 failed, 0 skipped
```

This step defines the provider-independent Application contract only.

Do not implement the TUI multi-card adapter, fixed-script correction,
presentation, batch recording or final verification.

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
7. `docs/Codex Prompts/036c - Cruise Capture Contract.md`, including Results and
   Lessons Learned
8. `KrytenAssist.Application/Cruises/CruisePageCaptureRequest.cs`
9. `KrytenAssist.Application/Cruises/CruiseCaptureStatus.cs`
10. `KrytenAssist.Application/Cruises/CruiseCaptureResult.cs`
11. `KrytenAssist.Application/Cruises/ICruisePageCaptureService.cs`
12. the matching Application contract tests
13. Core Cruise models used by the existing result
14. the current TUI capture adapter only to understand the later consumer; do
    not modify it in this step

Do not begin implementation until the existing request bounds, single-result
invariants, provider-independent boundary and single-capture compatibility
requirements are understood.

---

## Goal

Create a bounded provider-independent Application contract for capturing an
ordered collection of independently validated Cruise candidates from one page.

The contract must support:

- reuse of the existing transport-neutral bounded page request
- a separate batch capture service
- a completed batch containing one to ten candidate results
- ordered partial success
- Ready, Incomplete and Failed candidate states
- candidate-specific absolute HTTPS source references
- one complete `CruiseObservation` for every Ready candidate
- controlled missing-field evidence for Incomplete candidates
- safe messages for candidate-local and batch-level failures
- explicit batch truncation evidence
- computed candidate counts
- defensive immutable collections
- cancellation-token forwarding
- complete preservation of the existing single-capture contract

This is a contract-foundation step. It performs no parsing, mapping, recording,
navigation or persistence.

---

## Scope

This step owns:

- `CruiseCaptureBatchStatus`
- `CruiseCaptureBatchResult`
- `CruiseCaptureCandidateStatus`
- `CruiseCaptureCandidateResult`
- `ICruisePageBatchCaptureService`
- validation and immutable contract invariants
- deterministic Application contract tests
- regression execution for the existing single-capture contract
- this prompt's Results and Lessons Learned

This step does **not** own:

- changing `CruisePageCaptureRequest`
- changing `CruiseCaptureResult`
- changing `CruiseCaptureStatus`
- changing `ICruisePageCaptureService`
- TUI JSON or payload types
- multi-card parsing or mapping
- fixed JavaScript changes
- `[data-testid="product-card"]` selectors
- dependency injection
- source catalogue changes
- ViewModels or views
- candidate selection
- Record Selected or Record All
- batch recording orchestration
- Cruise History, SQLite or migrations
- live TUI verification
- Roadmap, Playbook Results or Session Handover updates
- Prompt 038 behavior

Do not create Infrastructure or Avalonia production placeholders.

---

## Allowed Changes

Create production files only under:

```text
KrytenAssist.Application/Cruises/
```

Create or update tests only under:

```text
KrytenAssist.Avalonia.Tests/Application/Cruises/
```

Update this prompt after implementation:

```text
docs/Codex Prompts/037h-a - Multi-Cruise Capture Contract.md
```

Do not modify:

- Core production types
- Infrastructure
- Avalonia production code
- API
- dependency injection
- project or solution references
- existing migrations or database schema
- Roadmap
- Playbook 037h Results
- Session Handovers

Production changes to an existing capture contract are permitted only if a
required deterministic regression test proves a genuine earlier contract defect.
Prefer no such correction. Report any correction explicitly.

Do not stage, commit, push, discard or overwrite Robin's work.

---

## Architecture Boundary

Preserve:

```text
KrytenAssist.Core
        ↑
KrytenAssist.Application
```

The new Application contract may use:

- `CruiseObservation`
- BCL collection types
- `Task`
- `CancellationToken`
- ordinary strings, booleans and counts

It must not use or expose:

- TUI payload types
- TUI source identifiers or host constants
- HTML, DOM or JavaScript
- `NativeWebView`
- Avalonia
- Infrastructure
- HTTP client or response types
- EF Core, SQLite or entities
- browser cookies or storage
- persistence identifiers

Application should require an absolute HTTPS candidate reference. The later TUI
adapter owns exact host/path trust validation.

---

## Preserve the Existing Single-Capture Contract

Do not replace or generalize these verified types:

```text
CruisePageCaptureRequest
CruiseCaptureStatus
CruiseCaptureResult
ICruisePageCaptureService
```

The single Cruise of the Week path depends on their exact behavior.

`CruisePageCaptureRequest` already supplies:

- source capability identifier
- retail source
- current page HTTPS reference
- observation timestamp
- bounded page payload up to 65,536 characters

Reuse that request at the new batch service boundary. Do not create a duplicate
request type or increase its payload limit without a deterministic failing test.

The listing-page `SourceReference` in the request describes the page on which
capture occurred. Every candidate result separately owns its itinerary reference.

---

## Batch Service Contract

Add a separate Application-owned interface:

```text
ICruisePageBatchCaptureService
```

It should expose one operation shaped consistently with the existing service:

```text
Task<CruiseCaptureBatchResult> CaptureAsync(
    CruisePageCaptureRequest request,
    CancellationToken cancellationToken = default)
```

The XML documentation should state:

- the request is validated and transport neutral
- candidates are returned in deterministic page order
- the implementation should honour cancellation
- an orchestration boundary may translate `OperationCanceledException` into a
  Cancelled result
- no external work is implied by the Application interface

Do not make the new interface inherit from or replace the single-capture
interface. A later Infrastructure adapter may implement both interfaces.

---

## Batch Status

Add:

```text
CruiseCaptureBatchStatus
```

Use these states unless existing naming conventions prove a clearer equivalent:

```text
Completed
Incomplete
Unsupported
Failed
Cancelled
```

Meanings:

### Completed

The supported payload was processed and contains between one and ten ordered
candidate results. A Completed batch may contain any mixture of Ready,
Incomplete and Failed candidates.

Multiplicity is expected. Do not include an `Ambiguous` batch state.

### Incomplete

The supported page/payload could not provide any candidate result—for example,
no supported product cards were found. It contains a safe message and no
candidates.

### Unsupported

The source, payload version or page type is unsupported. It contains a safe
message and no candidates.

### Failed

The payload could not be read or an unexpected batch-wide failure occurred. It
contains a safe message and no candidates.

### Cancelled

Capture was cancelled. It contains a safe message and no published partial
candidates.

Candidate-local invalidity belongs to an Incomplete or Failed candidate inside a
Completed batch, not a batch-wide failure.

---

## Candidate Status

Add:

```text
CruiseCaptureCandidateStatus
```

Use:

```text
Ready
Incomplete
Failed
```

### Ready

The candidate contains exactly one complete provider-independent
`CruiseObservation` and can later be selected for recording.

### Incomplete

The candidate can be identified safely but required Cruise fields are absent or
invalid. It contains controlled missing-field names and cannot be recorded.

### Failed

The individual candidate could not be mapped safely for a reason that does not
invalidate other candidates. It contains a safe message and cannot be recorded.

Do not use Ready to mean recorded. This contract ends at capture review.

---

## Candidate Result

Add an immutable result such as:

```text
CruiseCaptureCandidateResult
```

Expose:

```text
Status
DisplayLabel
SourceReference
Observation
Message
MissingFields
IsReady
```

Use focused factories such as:

```text
Ready(displayLabel, sourceReference, observation)
Incomplete(displayLabel, sourceReference, message, missingFields)
Failed(displayLabel, sourceReference, message)
```

Exact factory names may follow the existing `CruiseCaptureResult` convention.

### Display Label

Every candidate needs a safe identity for review, including candidates that
cannot form a complete domain observation.

Require:

- non-null
- nonblank
- maximum 512 characters
- exact retained value after validation unless existing normalization conventions
  clearly require trimming

The later adapter will normally use the available itinerary title. Do not add a
large partial-Cruise model in this step.

### Candidate Source Reference

Every candidate result requires its own:

- non-null
- nonblank
- absolute HTTPS source reference
- maximum length consistent with existing source-reference boundaries

This is the candidate itinerary reference, not merely the shared listing page.

Application must not hard-code the TUI host. Infrastructure will validate that
later.

### Ready Invariants

A Ready result must:

- require a non-null observation
- contain no message
- contain no missing fields
- set `IsReady` true
- require `observation.SourceReference` to be non-null
- require the observation source reference to equal the candidate source
  reference using ordinal equality

This prevents the review/open link from disagreeing with the observation that
would later be recorded.

Do not attempt URI canonicalization inside Application.

### Incomplete Invariants

An Incomplete result must:

- contain no observation
- require a safe nonblank message
- require between 1 and 16 missing-field names
- reject blank field names
- reject case-insensitive duplicate field names
- retain an immutable defensive copy
- set `IsReady` false

### Failed Invariants

A Failed result must:

- contain no observation
- require a safe nonblank message
- contain no missing fields
- set `IsReady` false

Use a shared private validation method where that reduces duplication without
creating a generic abstraction.

---

## Batch Result

Add an immutable result such as:

```text
CruiseCaptureBatchResult
```

Expose:

```text
Status
Candidates
Message
WasTruncated
IsCompleted
ReadyCount
IncompleteCount
FailedCount
```

Use focused factories such as:

```text
Completed(candidates, wasTruncated)
Incomplete(message)
Unsupported(message)
Failed(message)
Cancelled(message)
```

### Completed Invariants

A Completed result must:

- require a non-null candidate sequence
- enumerate the supplied sequence exactly once
- contain between 1 and 10 candidates
- reject null elements
- retain input order
- retain an immutable defensive copy
- reject exact duplicate candidate source references using ordinal comparison
- allow mixed Ready, Incomplete and Failed candidates
- allow a batch in which every candidate is Incomplete or Failed
- contain no batch-level failure message
- allow `WasTruncated` true or false
- set `IsCompleted` true
- compute Ready/Incomplete/Failed counts from the immutable candidates

Do not require at least one Ready candidate. A page with ten identifiable cards
whose prices are all missing is still a useful completed review containing ten
honest Incomplete candidates.

### Non-Completed Invariants

Incomplete, Unsupported, Failed and Cancelled batch results must:

- require a safe nonblank message
- contain no candidates
- set `WasTruncated` false
- set all candidate counts to zero
- set `IsCompleted` false

Do not publish partially processed candidates on batch cancellation in this
contract. The later ViewModel generation boundary can reject stale operations
cleanly.

### Bounds

Define a public or internally testable maximum:

```text
MaximumCandidateCount = 10
```

Keep it aligned with the existing fixed-script bound. Do not increase it in this
step.

Candidate messages and missing fields should use the existing capture-result
style and bounds rather than unbounded arbitrary content.

---

## Partial Success Semantics

Examples that must be valid:

```text
Completed
  Ready       Iconic Islands
  Ready       Aegean Shores
  Incomplete  Adriatic Explorer — missing prices
```

```text
Completed
  Incomplete  Candidate 1 — missing shipName
  Failed      Candidate 2 — could not be mapped safely
```

Examples that must be invalid:

```text
Completed with zero candidates
Completed with eleven candidates
Ready without an observation
Ready whose observation points to another source reference
Incomplete without missing fields
Failed with an observation
Cancelled with candidates
```

The contract does not determine which candidates are selected or recorded.

---

## Test Requirements

Use xUnit and existing Application contract-test conventions.

Add focused tests for the candidate contract:

- Ready retains exact values and exposes only the observation
- Ready requires observation and matching non-null source reference
- Ready rejects mismatched observation/reference
- display label validation and maximum length
- candidate reference rejects null, blank, relative and non-HTTPS values
- candidate reference maximum length
- Incomplete retains ordered distinct missing fields defensively
- Incomplete rejects empty, too many, blank and duplicate fields
- Incomplete requires a message and contains no observation
- Failed requires a message and contains no observation/fields
- status and `IsReady` invariants

Add focused tests for the batch contract:

- Completed retains one candidate
- Completed retains mixed candidates in exact input order
- Completed allows all-incomplete/all-failed candidates
- Completed computes exact counts
- Completed retains `WasTruncated`
- Completed enumerates its source exactly once
- Completed defensively copies candidate input
- candidate collection cannot be mutated through a mutable interface
- Completed rejects null, empty, null-element and over-maximum inputs
- Completed rejects exact duplicate candidate references
- non-completed factories retain safe messages and no candidates
- non-completed results have zero counts and are never truncated
- all message factories reject null/blank messages
- unknown/manual invalid status combinations cannot be constructed

Add a service-contract test proving:

- `ICruisePageBatchCaptureService` accepts the existing validated
  `CruisePageCaptureRequest`
- it receives the exact request instance
- it receives the exact cancellation token
- the interface itself performs no external work

Preserve and rerun the existing tests for:

```text
CruisePageCaptureRequest
CruiseCaptureResult
ICruisePageCaptureService
```

Use:

- fictional Cruise observations
- fixed timestamps with a non-zero offset
- fictional HTTPS references
- hand-written fake services
- exact assertions

Do not use:

- `DateTimeOffset.Now` or `UtcNow`
- mocking libraries
- reflection
- live HTTP, DNS or TUI
- browser automation
- JSON parsing
- test ordering
- mutable shared fixtures

---

## Expected Files

Expected new production files:

```text
KrytenAssist.Application/Cruises/CruiseCaptureBatchStatus.cs
KrytenAssist.Application/Cruises/CruiseCaptureBatchResult.cs
KrytenAssist.Application/Cruises/CruiseCaptureCandidateStatus.cs
KrytenAssist.Application/Cruises/CruiseCaptureCandidateResult.cs
KrytenAssist.Application/Cruises/ICruisePageBatchCaptureService.cs
```

Expected tests may be placed in focused files such as:

```text
KrytenAssist.Avalonia.Tests/Application/Cruises/CruiseCaptureBatchResultTests.cs
KrytenAssist.Avalonia.Tests/Application/Cruises/CruiseCaptureCandidateResultTests.cs
KrytenAssist.Avalonia.Tests/Application/Cruises/CruisePageBatchCaptureServiceContractTests.cs
```

Combining small related status/result tests is acceptable when readability is
better. Do not create one oversized unrelated test file.

---

## Production Correction Policy

The expected result is no change to existing production files.

If a required deterministic test exposes a genuine existing single-capture
contract defect:

1. retain the focused failing regression test
2. make the smallest Application-only correction
3. preserve every verified existing behavior
4. rerun the single and batch contract tests
5. report the correction explicitly

Do not modify existing types merely to share private validation code with the new
types. Small duplication is preferable to destabilizing the proven single path.

---

## Required Commands

Inspect and preserve the initial worktree.

Run the focused new and existing Application capture-contract tests with an
explicit filter. Record the exact command and totals.

Build the solution:

```text
dotnet build KrytenAssist.sln --no-restore
```

Run the complete regression suite:

```text
dotnet test KrytenAssist.sln --no-build --no-restore
```

Report exact totals, failures, skipped tests, errors and warnings.

All tests must remain offline.

---

## Definition of Done

Prompt 037h-a is complete when:

- the existing single request/result/service remain compatible
- a separate Application-owned batch capture service exists
- Completed replaces ambiguity as the expected multi-candidate state
- batch-level and candidate-level statuses are distinct
- Ready candidates contain one complete observation
- Incomplete candidates retain controlled missing fields
- Failed candidates retain safe messages
- every candidate has a bounded display label and absolute HTTPS reference
- Ready observation/reference agreement is enforced
- completed batches retain one to ten ordered immutable candidates
- exact duplicate candidate references are rejected
- partial success and all-incomplete batches are supported
- truncation is explicit
- counts are computed and correct
- non-completed batches contain no candidate data
- focused new tests pass
- existing single-capture contract tests pass
- complete solution builds
- complete regression suite passes
- this prompt's Results and Lessons Learned are complete

Do not begin 037h-b.

Stop after 037h-a.

---

## Completion Report

Provide:

### Summary

Describe the batch and per-candidate contract.

### Compatibility

Confirm the existing request/result/interface remain unchanged and tested.

### Invariants

Report bounds, immutability, partial success, source-reference and state
invariants.

### Files Modified

List every created and updated file.

### Production Corrections

Use `None` or report every test-proven correction.

### Build and Tests

Report exact commands and totals.

### Architecture and Scope

Confirm no TUI, Infrastructure, Avalonia, persistence, batch-recording or Prompt
038 behavior was added.

---

## Results

> Complete during implementation.

### Status

Complete.

### Batch Contract

Added a separate provider-independent batch result with Completed, Incomplete,
Unsupported, Failed and Cancelled states. Completed batches retain one to ten
ordered candidates, exact computed status counts and explicit truncation
evidence. Non-completed batches contain only a bounded safe message.

### Candidate Contract

Added immutable Ready, Incomplete and Failed candidate results. Every candidate
has a bounded display label and absolute HTTPS itinerary reference. Ready
candidates require an observation with an ordinally equal source reference;
Incomplete candidates retain bounded distinct missing fields; Failed candidates
retain only a bounded safe message.

### Single-Capture Compatibility

`CruisePageCaptureRequest`, `CruiseCaptureStatus`, `CruiseCaptureResult` and
`ICruisePageCaptureService` remain unchanged. Their existing contract tests were
included in the 56-test focused run.

### Files Created

Production:

- `KrytenAssist.Application/Cruises/CruiseCaptureBatchStatus.cs`
- `KrytenAssist.Application/Cruises/CruiseCaptureBatchResult.cs`
- `KrytenAssist.Application/Cruises/CruiseCaptureCandidateStatus.cs`
- `KrytenAssist.Application/Cruises/CruiseCaptureCandidateResult.cs`
- `KrytenAssist.Application/Cruises/ICruisePageBatchCaptureService.cs`

Tests:

- `KrytenAssist.Avalonia.Tests/Application/Cruises/CruiseCaptureBatchResultTests.cs`
- `KrytenAssist.Avalonia.Tests/Application/Cruises/CruiseCaptureCandidateResultTests.cs`
- `KrytenAssist.Avalonia.Tests/Application/Cruises/CruisePageBatchCaptureServiceContractTests.cs`

### Files Updated

- `docs/Codex Prompts/037h-a - Multi-Cruise Capture Contract.md`

### Production Corrections

None. The existing single-capture production contract required no correction.

### Build

Passed:

```text
dotnet build KrytenAssist.sln --no-restore
```

Result: 0 errors and 5 warnings. All warnings are the existing NU1903 advisory
for `SQLitePCLRaw.lib.e_sqlite3` 2.1.11; no compiler warning was introduced by
this step.

### Focused Tests

Passed:

```text
dotnet test KrytenAssist.Avalonia.Tests/KrytenAssist.Avalonia.Tests.csproj --no-restore --filter "FullyQualifiedName~CruiseCaptureCandidateResultTests|FullyQualifiedName~CruiseCaptureBatchResultTests|FullyQualifiedName~CruisePageBatchCaptureServiceContractTests|FullyQualifiedName~CruisePageCaptureRequestTests|FullyQualifiedName~CruiseCaptureResultTests|FullyQualifiedName~CruisePageCaptureServiceContractTests"
```

Result: 56 passed, 0 failed, 0 skipped. The command reported the existing
NU1903 package advisory and two existing unused-command-event compiler warnings.

### Complete Regression Suite

Passed:

```text
dotnet test KrytenAssist.sln --no-build --no-restore
```

Results:

- Core: 105 passed, 0 failed, 0 skipped
- Avalonia: 370 passed, 0 failed, 0 skipped
- API: 9 passed, 0 failed, 0 skipped
- Total: 484 passed, 0 failed, 0 skipped

### Offline Check

All tests use fictional observations, fixed timestamps, fictional HTTPS
references and hand-written in-memory fakes. No HTTP, DNS, browser automation or
external source access was used.

### Architecture and Scope Check

Changes are limited to the provider-independent Application cruise contract,
Application contract tests in the Avalonia test project and this prompt. No TUI
types, parsing, Infrastructure, Avalonia production code, dependency injection,
persistence, recording, Roadmap, Playbook Results, Session Handover or Prompt
038 behavior was added.

### Notes

Completed batches deliberately permit all-Incomplete/all-Failed candidate sets:
identifiable but unusable cards remain useful review evidence. Result
collections are defensive read-only copies, and the batch source is enumerated
exactly once.

---

## Lessons Learned

> Complete after implementation.

- Multi-card discovery is not an ambiguous single-result failure. A separate
  batch boundary preserves the proven single-capture path while expressing
  ordered partial success honestly.
- Keeping batch-wide failure states separate from candidate-local states makes
  it possible to review valid cruises even when neighbouring cards are missing
  fields or cannot be mapped.
- Requiring exact agreement between each candidate reference and its Ready
  observation prevents a later review or recording action from targeting a
  different itinerary.
- Bounded defensive copies make the contract safe for later adapters and
  presentation code without introducing provider or UI concerns prematurely.
