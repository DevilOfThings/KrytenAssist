# Codex Prompt 037h-d – Batch Observation Recording

## Implementation Prompt

Implement **Step 4 only** from:

```text
docs/AI Playbook/037h - Multiple Cruise Deals Handling.md
```

Prompt 037h-c is complete and committed as `f78a426`.

The verified solution baseline is:

```text
Core: 105 passed
Avalonia: 409 passed
API: 9 passed
Total: 523 passed, 0 failed, 0 skipped
```

This step adds explicit sequential recording of reviewed multi-Cruise
observations and one controlled Cruise History refresh.

Do not perform the final live Voyager verification, Roadmap update or Playbook
completion. Those belong to 037h-e.

Do not begin Prompt 038.

---

## Important Naming

This is:

```text
037h-d – Batch Observation Recording
```

It is not the already completed:

```text
037d – Observation Recording and History Queries
```

The completed 037d single-observation Application use case and History behavior
are foundations for this step and must be reused rather than redesigned.

---

## Required Reading

Read these files in order before changing code:

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/037 - Cruise History and Price Tracking.md`
5. `docs/AI Playbook/037h - Multiple Cruise Deals Handling.md`
6. `docs/Codex Prompts/037d - Observation Recording and History Queries.md`,
   including Results and Lessons Learned
7. `docs/Codex Prompts/037e - Cruise History Presentation.md`, including Results
   and Lessons Learned
8. `docs/Codex Prompts/037h-c - Multi-Cruise Capture Review.md`, including
   Results and Lessons Learned
9. `KrytenAssist.Application/Cruises/RecordCruiseObservation.cs`
10. the Application observation-record result and status types
11. `KrytenAssist.Avalonia/ViewModels/CruiseCaptureCandidateReviewItemViewModel.cs`
12. `KrytenAssist.Avalonia/ViewModels/CruiseBrowserFeasibilityViewModel.cs`
13. `KrytenAssist.Avalonia/ViewModels/CruiseHistoryViewModel.cs`
14. `KrytenAssist.Avalonia/ViewModels/CruiseOfTheWeekViewModel.cs`
15. `KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml`
16. the existing single recording, batch review, History and persistence tests
17. current Cruise Application and desktop-persistence dependency injection

Do not begin implementation until the existing single-observation outcome
semantics, batch selection behavior, history refresh generation boundary and
repository partial-success requirements are understood.

---

## Goal

Allow Robin to explicitly record selected or all valid observations from the
current multi-Cruise review while preserving one controlled outcome per Cruise.

The workflow becomes:

```text
Capture Loaded Cruises
        ↓
Review and select Ready candidates
        ↓
Record Selected / Record All Observations
        ↓
Sequential per-Cruise outcomes
        ↓
One Recorded Cruise History refresh
```

The implementation must:

- reuse `RecordCruiseObservation` for each valid reviewed observation
- keep every Cruise an independent durable aggregate
- process a stable ordered candidate snapshot sequentially
- retain First, Changed, Already Current, Cancelled and Failed outcomes per row
- allow failed, cancelled and unprocessed candidates to be retried
- never retry successful candidates in the same review
- stop cleanly on cancellation without fabricating failures
- refresh History exactly once after useful outcomes
- preserve the verified single-clean-candidate Record Observation workflow
- remain offline-first and provider independent

Recording factual observations does not mean Robin likes, saves, recommends or
rates those Cruises. Prompt 038 owns those concepts.

---

## Scope

This step owns:

- explicit Record Selected and Record All Observations commands
- explicit Cancel Recording for the active batch
- stable ordered batch recording orchestration in Avalonia
- per-candidate recording state and safe messages
- partial completion and retryability
- cancellation and stale-recording generations
- exact single-use-case invocation per attempted observation
- one post-batch History refresh after useful outcomes
- deterministic preferred affected-history selection
- batch progress and completion summaries
- passive recording presentation in the existing batch review
- dependency composition of the existing record use case
- deterministic offline orchestration and presentation tests
- this prompt's Results and Lessons Learned

This step does **not** own:

- changing `RecordCruiseObservation` behavior without a test-proven defect
- a new repository or persistence abstraction
- a batch database transaction
- changing Cruise History domain identity or fingerprint semantics
- changing Core Cruise models
- changing capture contracts or the TUI adapter
- changing the fixed browser script
- ratings, notes, favourites, interest or preferences
- deleting or editing recorded observations
- automatic recording after capture
- parallel recording
- live TUI verification
- Roadmap or Playbook Results updates
- Session Handover updates
- Prompt 038 behavior

Do not add final verification behavior from 037h-e.

---

## Allowed Changes

Production changes should be limited to:

```text
KrytenAssist.Avalonia/ViewModels/
KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml
```

Tests may be created or updated under:

```text
KrytenAssist.Avalonia.Tests/ViewModels/
KrytenAssist.Avalonia.Tests/DependencyInjection/
KrytenAssist.Avalonia.Tests/Application/Cruises/
```

Update this prompt after implementation:

```text
docs/Codex Prompts/037h-d - Batch Observation Recording.md
```

Do not modify:

- Core production types
- Infrastructure
- API
- TUI capture adapter or browser script
- browser code-behind
- persistence entities, configuration or migrations
- existing database schema
- Roadmap
- Playbook 037h Results
- Session Handovers

Application production changes are not expected. If a deterministic test proves
a genuine defect in the existing single record use case, follow the Production
Correction Policy and report it explicitly.

Do not stage, commit, push, discard or overwrite Robin's work.

---

## Architecture Boundary

Preserve:

```text
Avalonia reviewed candidate snapshot
        ↓ one at a time
Application RecordCruiseObservation
        ↓
Application-owned repository abstraction
        ↓
Existing SQLite implementation
```

The batch orchestrator may use:

- the existing Application `RecordCruiseObservation` use case
- provider-independent `CruiseObservation`
- Application record result/status types
- Avalonia candidate review items and commands
- the existing `CruiseHistoryViewModel` refresh behavior
- ordinary cancellation tokens and immutable/read-only snapshots

It must not use or expose:

- TUI payload types
- HTML, DOM, JavaScript or `NativeWebView`
- Infrastructure repository implementations
- EF Core, SQLite connections or entities
- browser cookies or storage
- parallel database calls

Do not call the repository directly from Avalonia. Always use the verified
Application use case.

---

## Reuse the Existing Single-Observation Use Case

For every attempted batch observation, call:

```text
RecordCruiseObservation.ExecuteAsync(observation, cancellationToken)
```

Do not copy its sailing-key, fingerprint, repository or price-history logic into
Avalonia.

Retain these exact Application outcomes:

```text
FirstObservationRecorded
ChangedObservationRecorded
AlreadyCurrent
Cancelled
Failed
```

The use case already converts repository cancellation and failures into
controlled results. The batch orchestrator should still contain a safe final
exception boundary so an unexpected exception cannot escape an async command.

Do not place multiple sailings in one transaction. A failure for one Cruise
must not roll back successfully recorded observations for unrelated sailings.

---

## Dependency Composition

Pass the existing Application use case into the Cruise workspace:

```text
RecordCruiseObservation
```

Prefer passing it through `CruiseOfTheWeekViewModel` into
`CruiseBrowserFeasibilityViewModel` as an optional final constructor dependency
where that preserves existing direct callers and design-time construction.

The existing desktop-persistence composition should already register this use
case. Do not add manual `Program.cs` registration or construct repositories in a
ViewModel.

When the record use case is absent:

- batch review remains usable
- recording commands remain disabled
- no service location or runtime resolution is attempted

The existing single recording dependency inside `CruiseHistoryViewModel`
remains unchanged.

---

## Per-Candidate Recording State

Extend the candidate review presentation with a focused Avalonia-owned status,
for example:

```text
NotAttempted
Recording
FirstObservationRecorded
ChangedObservationRecorded
AlreadyCurrent
Cancelled
Failed
```

Exact type and property names may follow existing conventions.

Each candidate item should expose the useful equivalent of:

```text
RecordingStatus
RecordingStatusText
RecordingMessage
HasRecordingMessage
IsRecording
IsRecordingComplete
IsRetryable
CanRecord
```

### Capture-Incomplete and Capture-Failed Rows

Rows without a Ready observation:

- remain visible as review evidence
- are never recordable
- retain their original capture status and message
- do not acquire a fabricated batch-record outcome

### Not Attempted

A Ready observation that has not been processed in the current review is
eligible for recording.

### Recording

Only the currently processed row should show Recording. Processing remains
sequential.

### First Observation Recorded

The first factual observation for the sailing/source was recorded. It is
complete and not retryable in this review.

### Changed Observation Recorded

A meaningful changed observation was recorded. It is complete and not
retryable in this review.

### Already Current

The identical observation was already current. It is a useful completed outcome
but must not be described as newly inserted. It is not retryable in this review.

### Cancelled

The current attempt was cancelled. It remains retryable.

### Failed

The current attempt could not be recorded. It remains retryable.

Use safe controlled row messages. Do not expose exception, repository, SQL or
database details.

---

## Recording Commands

Add parent commands:

```text
RecordSelectedCommand
RecordAllObservationsCommand
CancelBatchRecordingCommand
```

Use the exact user-facing labels:

```text
Record Selected
Record All Observations
Cancel Recording
```

Do not use Save All Cruises. Prompt 038 owns saved/interesting Cruises.

### Record Selected

Enable only when:

- a genuine batch review exists
- the record use case is available
- capture is not active
- batch recording is not active
- at least one selected Ready candidate is currently recordable

At command start, create a stable ordered snapshot of selected recordable items.

Selection changes after start must not alter the active enumeration.

### Record All Observations

Enable under the same lifecycle rules when at least one Ready candidate is
recordable.

At command start, create a stable ordered snapshot of every recordable Ready
candidate in current review order, regardless of selection.

It must not include:

- Incomplete candidates
- capture-Failed candidates
- successfully completed record outcomes
- Already Current outcomes

### Cancel Recording

Enable only while batch recording is active.

Cancel the exact token used by the current observation and stop before beginning
the next candidate.

Do not clear completed outcomes when cancellation is requested.

---

## Sequential Execution

Process the stable snapshot one item at a time in review order.

For each item:

1. check cancellation before beginning
2. mark only that item Recording
3. call `RecordCruiseObservation.ExecuteAsync`
4. map the controlled Application result into the item outcome
5. retain the outcome before considering the next item
6. stop if cancellation was returned or requested

Do not use `Task.WhenAll`, parallel loops or fire-and-forget calls.

Duplicate command execution must not create a second active batch.

Capture should be disabled while recording. Selection controls should be
disabled while recording for clarity, but the internal stable snapshot remains
required regardless of UI enablement.

Do not automatically change the selected set after completion. Recording
eligibility, not hidden selection mutation, should prevent successful items
from being processed twice.

---

## Partial Success and Retry

Every Cruise is independent.

A Failed result should:

- be retained on its row
- not stop later unrelated candidates
- remain retryable

The following outcomes are complete and excluded from retry:

```text
FirstObservationRecorded
ChangedObservationRecorded
AlreadyCurrent
```

The following remain eligible for retry:

```text
NotAttempted
Cancelled
Failed
```

Retry through Record Selected or Record All should process only currently
eligible items. It must not call the use case again for completed items.

Do not roll back earlier successes when a later item fails or is cancelled.

---

## Cancellation Semantics

Use a separate deterministic batch-recording generation and
`CancellationTokenSource` from the capture lifecycle.

Cancellation should:

- cancel the exact current use-case token
- preserve outcomes already completed
- mark the current candidate Cancelled when the use case returns Cancelled
- leave candidates not yet started as Not Attempted
- stop before the next candidate
- retain Cancelled and Not Attempted candidates as retryable
- never turn unprocessed candidates into Failed

If cancellation occurs between candidates, no arbitrary next candidate should
be marked Cancelled.

The following should cancel and invalidate an active recording generation:

- explicit Cancel Recording
- a new capture
- navigation to another address
- Back
- Forward
- Refresh
- browser close
- source change
- untrusted navigation

A late obsolete result must not:

- overwrite a newer review
- update a replacement candidate row
- restore cleared state
- alter a later recording summary

Observations already persisted before invalidation remain durable. Do not attempt
compensating deletes or rollback.

---

## Batch Progress and Summary

Expose useful parent state such as:

```text
IsBatchRecording
CanRecordSelected
CanRecordAllObservations
BatchRecordingProgressText
BatchRecordingSummary
HasBatchRecordingSummary
FirstRecordedCount
ChangedRecordedCount
AlreadyCurrentCount
RecordingFailedCount
RecordingCancelledCount
NotAttemptedCount
```

During processing use honest text such as:

```text
Recording observation 4 of 10…
```

After completion use exact counts, for example:

```text
10 observations checked against local history.

6 first observations
2 changed observations
1 already current
1 failed — you can retry it
```

After cancellation use controlled language such as:

```text
Recording cancelled.

3 observations completed
1 cancelled
6 not attempted
```

Do not describe Already Current as newly recorded. Do not claim Failed,
Cancelled or Not Attempted observations were checked successfully.

The summary may describe the stable attempted batch or current review totals,
but the meaning and counts must be deterministic and tested.

---

## One History Refresh

After processing stops, refresh Recorded Cruise History exactly once when at
least one attempted result is:

```text
FirstObservationRecorded
ChangedObservationRecorded
AlreadyCurrent
```

Do not refresh once per candidate.

Do not refresh when:

- no candidate was attempted
- every attempt Failed
- cancellation occurred before any useful outcome

Add a small awaitable method to `CruiseHistoryViewModel` if needed, such as an
explicit batch-completion refresh accepting a preferred affected observation.

That method must reuse the existing private `LoadHistoryAsync` behavior rather
than duplicating list, selection or message logic.

Preferred history selection should use the first useful affected observation in
stable batch order:

- `CruiseSailingKey.From(observation)`
- that observation's retail source

If the preferred history is not returned, preserve or choose the existing safe
fallback selection behavior.

A History refresh failure must not erase successful per-candidate record
outcomes. Surface the existing controlled History error independently.

If recording is cancelled after useful outcomes, still perform the one useful
History refresh unless the entire review lifecycle has been invalidated by
navigation/close/replacement. Document and test the chosen safe behavior.

---

## Preserve Single Recording

Do not change the verified single-clean-candidate experience:

- one clean Ready capture uses the existing single review
- `CruiseHistoryViewModel.SetCapturedObservation` receives it
- the existing Record Observation command calls the existing use case once
- existing single outcome messages and one-history refresh remain unchanged

The new batch commands should appear only for a genuine batch review.

Do not make the single review pass through the batch coordinator.

---

## Passive View

Update the existing batch review panel in
`CruiseBrowserFeasibilityView.axaml`.

Add parent actions bound to:

```text
Record Selected
Record All Observations
Cancel Recording
```

Each Ready row should show its recording status and safe message.

Show:

- active progress while recording
- final/partial batch summary
- retryable Failed or Cancelled outcomes honestly

Disable selection while batch recording where supported by the item ViewModel.

The view must not:

- call the record use case or repository
- loop through candidates in code-behind
- decide retry eligibility
- compute outcome counts
- refresh History directly
- add browser or JavaScript behavior

Use bindings and commands only. Browser code-behind remains unchanged.

---

## Test Requirements

Use xUnit and existing hand-written ViewModel and Application-test conventions.

### Command and Snapshot Tests

Prove:

- Record Selected is enabled only for selected recordable Ready candidates
- Record All is enabled only when at least one recordable Ready candidate exists
- commands remain disabled without the record use case
- both commands are disabled during capture and active batch recording
- Cancel Recording is enabled only during batch recording
- duplicate command execution starts one batch only
- Record Selected uses a stable ordered selection snapshot
- Record All uses a stable ordered snapshot of every eligible Ready candidate
- Incomplete and capture-Failed candidates are never passed to the use case
- selection controls cannot affect the active enumeration

### Exact Invocation Tests

Use a deterministic fake or the existing Application use case with a controlled
repository and prove:

- each attempted item passes its exact observation
- every attempted item receives the exact active cancellation token
- execution order matches review order
- processing is sequential, with no next call before the prior result completes
- completed candidates are not invoked again on retry

Do not mock the concrete non-virtual use case. Prefer the existing fake
repository beneath the real `RecordCruiseObservation`, or introduce only a
small Application-owned abstraction if deterministic tests prove it is required
and the architecture remains consistent. Do not use service location.

### Outcome Tests

Prove independently:

- First Observation Recorded maps to a completed non-retryable row
- Changed Observation Recorded maps to a completed non-retryable row
- Already Current is useful, complete and not described as newly inserted
- Failed remains retryable
- one Failed result does not stop later unrelated observations
- safe row messages contain no exception or persistence details
- exact summary counts and wording distinguish every outcome

### Cancellation Tests

Use controlled incomplete tasks and prove:

- explicit cancellation cancels the exact current token
- completed earlier outcomes remain unchanged
- the current returned Cancelled result remains retryable
- later candidates stay Not Attempted and retryable
- no later use-case call begins after cancellation
- cancellation between candidates does not fabricate a Cancelled next item
- retry processes only failed/cancelled/not-attempted eligible candidates
- navigation, Back, Forward, Refresh, close and replacement capture invalidate
  late recording results
- a stale result cannot alter a new batch review

Do not use sleeps or timing races.

### History Refresh Tests

Prove:

- several useful outcomes cause exactly one History list/query refresh
- First, Changed and Already Current each count as useful
- mixed useful/failed/cancelled outcomes still refresh exactly once
- all-Failed does not refresh
- cancellation before a useful outcome does not refresh
- the first useful affected observation supplies the preferred key/source
- preferred affected History is selected when returned
- History refresh failure preserves row outcomes and uses controlled History
  error presentation
- History is never refreshed once per candidate

### Single Compatibility Tests

Preserve and rerun tests proving:

- the existing single Record Observation command remains available
- single recording calls the use case once
- single messages and History refresh remain unchanged
- batch commands do not appear for the single-clean-candidate review

### Composition and Passive-View Tests

Prove:

- the existing record use case is supplied to the Cruise workspace through DI
- direct/design-time construction without it remains controlled
- XAML binds Record Selected, Record All Observations and Cancel Recording
- XAML binds per-row recording state
- code-behind contains no batch-recording or selection orchestration

### Test Data

Use:

- fictional provider-independent observations
- fixed timestamps with a non-zero offset
- existing in-memory fake Cruise repository patterns
- controlled incomplete repository tasks
- isolated temporary SQLite only when an integration test materially requires it
- exact assertions

Do not use:

- live HTTP or DNS
- live TUI pages or payloads
- browser automation or `NativeWebView`
- OS URL launching
- Robin's production SQLite database
- `DateTimeOffset.Now` or `UtcNow`
- mocking libraries
- reflection
- sleeps or timing-dependent tests
- mutable shared fixtures

---

## Expected Files

Expected production updates:

```text
KrytenAssist.Avalonia/ViewModels/CruiseCaptureCandidateReviewItemViewModel.cs
KrytenAssist.Avalonia/ViewModels/CruiseBrowserFeasibilityViewModel.cs
KrytenAssist.Avalonia/ViewModels/CruiseHistoryViewModel.cs
KrytenAssist.Avalonia/ViewModels/CruiseOfTheWeekViewModel.cs
KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml
```

A small Avalonia status type may be created, for example:

```text
KrytenAssist.Avalonia/ViewModels/CruiseBatchRecordingStatus.cs
```

Expected test changes may include:

```text
KrytenAssist.Avalonia.Tests/ViewModels/CruiseBatchObservationRecordingViewModelTests.cs
KrytenAssist.Avalonia.Tests/ViewModels/CruiseBatchCaptureReviewViewModelTests.cs
KrytenAssist.Avalonia.Tests/ViewModels/CruiseHistoryViewModelTests.cs
KrytenAssist.Avalonia.Tests/DependencyInjection/CruiseHistoryDesktopCompositionTests.cs
```

No Core, Infrastructure, API, migration or schema file is expected to change.

---

## Production Correction Policy

The expected production changes are limited to Avalonia orchestration,
presentation and History refresh exposure.

If a required deterministic test exposes a genuine existing Application defect:

1. retain the focused failing regression test
2. make the smallest Application-only correction
3. preserve every verified single-observation behavior
4. rerun focused single and batch recording tests
5. report the correction explicitly

Do not redesign the repository, schema or transaction boundary to simplify the
batch UI.

---

## Required Commands

Inspect and preserve the initial worktree.

Run focused tests for:

- batch recording commands and stable snapshots
- per-candidate outcomes and retries
- cancellation and stale-result protection
- one History refresh and preferred selection
- existing single observation recording
- batch capture review compatibility
- Cruise workspace composition
- passive XAML boundaries

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

Prompt 037h-d is complete when:

- Record Selected processes a stable ordered selected Ready snapshot
- Record All Observations processes every eligible Ready candidate
- Incomplete and capture-Failed rows are never recorded
- recording calls the existing single Application use case once per attempt
- execution is sequential and duplicate batches are prevented
- every attempted row retains its controlled outcome
- useful completed outcomes are not retried
- Failed, Cancelled and Not Attempted rows remain retryable
- one failure does not stop later unrelated candidates
- cancellation preserves completed outcomes and leaves later rows unprocessed
- late invalidated results cannot alter a replacement review
- exact progress and outcome summaries are presented
- History refreshes exactly once after useful outcomes
- History does not refresh for wholly unsuccessful attempts
- the first useful affected History is preferred deterministically
- the existing single Record Observation workflow remains unchanged
- the view remains passive and MVVM-driven
- no batch transaction, persistence redesign or Prompt 038 behavior was added
- focused offline tests pass
- the complete solution builds
- the complete regression suite passes
- this prompt's Results and Lessons Learned are complete

Do not begin 037h-e.

Stop after 037h-d.

---

## Completion Report

Provide:

### Summary

Describe Record Selected, Record All, sequential outcomes and retry behavior.

### Cancellation and Partial Success

Report stable snapshots, cancellation preservation and stale-result protection.

### History Refresh

Report exact one-refresh behavior and deterministic preferred selection.

### Single-Recording Compatibility

Confirm the existing single Record Observation workflow remains unchanged.

### Files Modified

List every created and updated file.

### Production Corrections

Use `None` or report every test-proven correction outside expected files.

### Build and Tests

Report exact commands, totals, failures, skipped tests, errors and warnings.

### Architecture and Scope

Confirm no Core/Infrastructure/schema change, batch transaction, live automation,
final 037h-e verification or Prompt 038 behavior was added.

---

## Results

> Complete during implementation.

### Status

Complete.

### Batch Recording Commands

Added Record Selected, Record All Observations and Cancel Recording commands to
the genuine batch review. Commands require the existing Application record use
case, reject duplicate execution, take stable ordered snapshots and remain
disabled during capture or active batch recording.

### Per-Candidate Outcomes

Ready rows now retain Not Attempted, Recording, First Observation Recorded,
Changed Observation Recorded, Already Current, Cancelled and Failed states with
safe messages. Capture-Incomplete and capture-Failed rows remain review-only and
never reach the record use case.

### Partial Success and Retry

Recording is sequential and each Cruise remains independent. Failed outcomes do
not stop later candidates and remain retryable. First, Changed and Already
Current outcomes are complete and excluded from retry; later retry commands
process only Failed, Cancelled or Not Attempted Ready rows.

### Cancellation and Stale Results

Batch recording uses its own generation and cancellation source. Explicit
cancellation preserves completed rows, maps the active cancelled use-case result
to a retryable Cancelled row and leaves later rows Not Attempted. Capture,
navigation, Back, Forward, Refresh, close and replacement review invalidate
obsolete recording generations.

### History Refresh

Added an awaitable `CruiseHistoryViewModel` batch-completion refresh that reuses
the existing history loader and prefers the first useful affected observation.
The orchestrator refreshes exactly once after any First, Changed or Already
Current outcome, including partial cancellation, and does not refresh wholly
failed attempts.

### Single-Recording Compatibility

The verified single-clean-candidate review still delegates to the existing
`CruiseHistoryViewModel` Record Observation command. The new batch coordinator
is used only when genuine batch review rows exist; no single recording messages,
fingerprints or refresh behavior were changed.

### Files Created

- `KrytenAssist.Avalonia/ViewModels/CruiseBatchRecordingStatus.cs`
- `KrytenAssist.Avalonia.Tests/ViewModels/CruiseBatchObservationRecordingViewModelTests.cs`
- `docs/Codex Prompts/037h-d - Batch Observation Recording.md`

### Files Updated

- `KrytenAssist.Avalonia/ViewModels/CruiseCaptureCandidateReviewItemViewModel.cs`
- `KrytenAssist.Avalonia/ViewModels/CruiseBrowserFeasibilityViewModel.cs`
- `KrytenAssist.Avalonia/ViewModels/CruiseHistoryViewModel.cs`
- `KrytenAssist.Avalonia/ViewModels/CruiseOfTheWeekViewModel.cs`
- `KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml`

### Production Corrections

None.

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
dotnet test KrytenAssist.Avalonia.Tests/KrytenAssist.Avalonia.Tests.csproj --no-restore --disable-build-servers -m:1 --filter "FullyQualifiedName~CruiseBatchObservationRecordingViewModelTests|FullyQualifiedName~CruiseBatchCaptureReviewViewModelTests|FullyQualifiedName~CruiseHistoryViewModelTests|FullyQualifiedName~RecordCruiseObservationTests|FullyQualifiedName~CruiseCaptureLifecycleViewModelTests"
```

Result: 68 passed, 0 failed, 0 skipped. The single-worker/build-server options
were used after the sandbox denied an MSBuild named-pipe worker; this did not
change test behavior.

### Complete Regression Suite

Passed:

```text
dotnet test KrytenAssist.sln --no-build --no-restore
```

Results:

- Core: 105 passed, 0 failed, 0 skipped
- Avalonia: 415 passed, 0 failed, 0 skipped
- API: 9 passed, 0 failed, 0 skipped
- Total: 529 passed, 0 failed, 0 skipped

### Offline Check

All new tests use fictional provider-independent observations, fixed timestamps,
the real Application record use case, the existing in-memory fake repository and
controlled incomplete tasks. No HTTP, DNS, browser, live TUI page, OS launcher,
SQLite production database or personal data was used.

### Architecture and Scope Check

Changes are limited to Avalonia ViewModels, passive XAML, focused tests and this
prompt. No Core, Application production, Infrastructure, API, schema, migration,
batch transaction, live automation, final 037h-e verification or Prompt 038
behavior was added.

### Notes

The first parallel complete-suite attempt exposed the existing timing-sensitive
`MainWindowViewModelTests.DeletePrompt_RefreshesPromptsCategoriesSearchAndSelection`
test once. The isolated complete Avalonia project passed 415 tests, and the
immediate required complete-suite rerun passed all 529 tests without production
changes. Manual review should confirm the buttons and per-row outcome text remain
comfortable with ten displayed cards.

---

## Lessons Learned

> Complete after implementation.

- Reusing the real single-observation use case keeps sailing identity,
  fingerprinting, repository errors and price-history analysis out of Avalonia.
- A stable sequential snapshot makes cancellation and retry deterministic while
  avoiding concurrent SQLite writes or an inappropriate cross-sailing
  transaction.
- Recording eligibility can prevent duplicate successful calls without silently
  mutating Robin's review selection.
- Treating Already Current as useful but not newly inserted provides honest row
  and batch summaries and still justifies one History refresh.
- Exposing one small awaitable History refresh method allowed deterministic
  preferred selection without duplicating History loading logic or refreshing
  once per candidate.
