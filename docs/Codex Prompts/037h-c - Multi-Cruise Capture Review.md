# Codex Prompt 037h-c – Multi-Cruise Capture Review

## Implementation Prompt

Implement **Step 3 only** from:

```text
docs/AI Playbook/037h - Multiple Cruise Deals Handling.md
```

Prompt 037h-b is complete and committed as `b974ff5`.

The verified solution baseline is:

```text
Core: 105 passed
Avalonia: 392 passed
API: 9 passed
Total: 506 passed, 0 failed, 0 skipped
```

This step adds the Avalonia multi-candidate capture lifecycle, review and
selection presentation on top of the verified Application batch contract and
TUI batch adapter.

Do not implement batch observation recording. `Record Selected` and
`Record All Observations` belong to 037h-d.

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
7. `docs/Codex Prompts/036e - Cruise Capture Review.md`, including Results and
   Lessons Learned
8. `docs/Codex Prompts/037h-a - Multi-Cruise Capture Contract.md`, including
   Results and Lessons Learned
9. `docs/Codex Prompts/037h-b - TUI Multi-Card Capture Adapter.md`, including
   Results and Lessons Learned
10. the Application batch result, candidate result and service interface
11. `KrytenAssist.Avalonia/ViewModels/CruiseBrowserFeasibilityViewModel.cs`
12. `KrytenAssist.Avalonia/ViewModels/CruiseOfTheWeekViewModel.cs`
13. `KrytenAssist.Avalonia/ViewModels/CruiseHistoryViewModel.cs`, only to
    preserve the current single-observation hand-off
14. `KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml`
15. `KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml.cs`
16. the current capture lifecycle, review, navigation and Cruise History tests
17. shell and TUI capture dependency-injection composition

Do not begin implementation until the existing capture generation boundary,
browser bridge, trusted-address policy, single-review hand-off and 037h-b batch
semantics are understood.

---

## Goal

Allow Robin to explicitly capture and review the loaded Cruise cards from a
supported TUI page as an ordered selectable collection.

The presentation must:

- invoke the provider-independent batch capture service through the existing
  bounded browser bridge
- show Ready, Incomplete and Failed candidates honestly in adapter order
- display candidate-specific Cruise evidence without cross-row reuse
- allow individual selection of Ready candidates only
- provide Select All Ready and Clear Selection actions
- allow explicit trusted Open at TUI for each published candidate
- retain honest loaded-card and truncation language
- preserve cancellation and stale-result protection
- preserve the current single Cruise of the Week review and recording path
- perform no batch recording or persistence

This is a review step. Selection means only "chosen for a possible later record
action". It does not record, save, like, rate or recommend a Cruise.

---

## Scope

This step owns:

- Avalonia consumption of `ICruisePageBatchCaptureService`
- batch capture lifecycle and safe result presentation
- a focused per-candidate review item ViewModel
- ordered candidate review rows
- individual Ready selection
- Select All Ready and Clear Selection commands
- selected and status counts
- candidate-specific trusted external open commands
- honest batch and truncation summaries
- single-clean-candidate compatibility routing
- generation-based cancellation and stale-result tests for batch capture
- passive XAML presentation
- dependency composition through the existing Cruise workspace
- deterministic offline ViewModel and presentation tests
- this prompt's Results and Lessons Learned

This step does **not** own:

- changing the Application batch contract
- changing the TUI adapter or fixed extraction script
- changing Core Cruise models
- Record Selected
- Record All Observations
- batch recording orchestration or per-record outcomes
- Cruise History persistence changes
- SQLite schema or migrations
- ratings, notes, favourites or preferences
- automatic selection based on recommendation or ranking
- automatic recording after capture
- live TUI verification
- Roadmap or Playbook Results updates
- Session Handover updates
- Prompt 038 behavior

Do not add disabled recording placeholders. Stop at useful review and selection.

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
```

Update this prompt after implementation:

```text
docs/Codex Prompts/037h-c - Multi-Cruise Capture Review.md
```

Do not modify:

- Application production contracts
- Core production types
- Infrastructure
- the fixed TUI browser script
- Avalonia browser code-behind unless a deterministic test proves the existing
  provider-independent payload bridge cannot support batch review
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
Application batch contract
        ↓
Avalonia parent lifecycle ViewModel
        ↓
Avalonia candidate review item ViewModels
        ↓ bindings
Passive Avalonia view
```

The ViewModels may use:

- `ICruisePageBatchCaptureService`
- provider-independent batch and candidate results
- existing Core Cruise observations
- the existing source catalogue and trusted-host policy
- commands, immutable/read-only collections and presentation strings

They must not use or expose:

- TUI payload DTOs
- JSON parsing
- HTML, DOM or JavaScript
- `NativeWebView`
- Infrastructure adapter types
- HTTP response types
- EF Core, SQLite or persistence entities

Keep browser operations in the existing view bridge. Keep review, selection,
trust decisions and lifecycle state in ViewModels.

---

## Preserve the Existing Browser Bridge

The view code-behind already:

1. receives the explicit capture event
2. records the browser address at operation start
3. invokes the fixed bounded capture script once
4. rejects stale navigation/address changes
5. parses the returned script string
6. enforces `CruisePageCaptureRequest.MaximumPagePayloadLength`
7. calls `ProcessCapturePayloadAsync`

That bridge is sufficient for single and batch payloads because both use the
same existing request.

Prefer no code-behind change. Do not add candidate selection or presentation
logic to code-behind.

The fixed script remains unchanged in this step.

---

## Batch Dependency Composition

Add the Application-owned batch dependency to the Cruise workspace:

```text
ICruisePageBatchCaptureService
```

Pass it through `CruiseOfTheWeekViewModel` into
`CruiseBrowserFeasibilityViewModel`.

037h-b already registers the single and batch interfaces against the same
Infrastructure singleton. Do not add a new Infrastructure registration.

Keep the batch dependency optional where that preserves design-time construction
and existing direct tests. Add optional constructor parameters at the end where
possible to avoid breaking verified positional callers.

When the batch service is available, the explicit capture action should use it.
When it is absent, preserve the existing legacy single-service behavior rather
than making design-time or isolated tests unusable.

Do not use service location.

---

## Capture Action and User Language

When batch capture is available, use:

```text
Capture Loaded Cruises
```

Do not claim to capture all Cruise deals. The browser script captures only the
currently loaded bounded product cards.

Expose presentation text through a ViewModel property such as:

```text
CaptureButtonText
```

When only the legacy single service is available, retaining:

```text
Capture Displayed Cruise
```

is acceptable and should be deterministic.

Reuse the existing explicit bridge event. Do not add a second browser-script
event for batch capture.

---

## Candidate Review Item ViewModel

Create a small focused presentation type such as:

```text
CruiseCaptureCandidateReviewItemViewModel
```

It should wrap one immutable provider-independent candidate result and expose
only review and selection presentation.

Expected properties include the useful equivalent of:

```text
Status
StatusText
DisplayLabel
Observation
Operator
RetailSource
Ship
DepartureDateText
DurationText
DeparturePort
PricesText
PromotionSummary
SourceReference
MissingFields
MissingFieldsText
IsReady
IsIncomplete
IsFailed
IsSelected
CanSelect
CanOpenAtTui
OpenAtTuiCommand
```

Exact property names may follow existing conventions.

### Ready Row

A Ready row should display from its own observation:

- itinerary title/display label
- operator
- retail source
- ship
- departure date
- duration
- departure port when present
- current neutral price evidence
- promotion summary when present
- exact candidate source reference

It is selectable.

### Incomplete Row

An Incomplete row should:

- remain visible in original order
- show its safe display label
- show Incomplete state
- show its controlled message
- show its candidate-specific missing-field names
- contain no fabricated Cruise evidence
- not be selectable

### Failed Row

A Failed row should:

- remain visible in original order
- show its safe display label
- show Failed state and safe message
- contain no fabricated observation or missing fields
- not be selectable

### Optional Presentation

Do not invent placeholder values such as an unknown ship or zero price. Hide or
omit optional observation fields when absent.

Use the same neutral price display semantics as the existing single review. Do
not redesign original/discounted/extra-discount pricing in this step.

---

## Selection Semantics

Selection begins clear after every new batch capture.

Only Ready candidates may be selected.

Expose:

```text
SelectAllReadyCommand
ClearSelectionCommand
```

The parent should retain useful computed state such as:

```text
SelectedCandidateCount
HasSelectedCandidates
CanSelectAllReady
CanClearSelection
```

Requirements:

- an individual Ready item can be selected or cleared
- an Incomplete or Failed item can never become selected
- Select All Ready selects every and only Ready item
- Clear Selection clears every item
- selection counts and command enablement update immediately
- a later capture replaces the old collection and resets selection
- clearing/navigation/cancellation never leaves hidden selected observations

Selection is presentation state. Do not mutate Application results or Core
observations.

Do not default all Ready candidates to selected. Recording remains an explicit
later action.

---

## Batch Review State

The parent ViewModel should expose a defensive ordered review collection and
useful equivalent properties such as:

```text
CapturedCandidates
HasCapturedCandidates
ReadyCandidateCount
IncompleteCandidateCount
FailedCandidateCount
WasCaptureTruncated
BatchCaptureSummary
```

Use the exact immutable order returned by the Application result.

Never sort candidates alphabetically or by price in this step.

Do not expose a mutable collection that external callers can add to or remove
from.

The parent should unsubscribe from replaced item events if subscriptions are
used to maintain selection counts.

---

## Honest Summary Language

Use loaded/displayed language, for example:

```text
Captured 9 of 10 loaded cruise deals.

9 ready to review
1 incomplete
```

The exact grammar should remain readable for singular and plural counts.

When Failed candidates exist, report that count without exposing internal
details.

When `WasTruncated` is true, include controlled guidance such as:

```text
Kryten captured the first 10 loaded cruise deals.
Refine the TUI results or capture another page to review more.
```

Do not state or infer the total number of TUI search results from the bounded
payload.

Batch-wide Incomplete, Unsupported, Failed and Cancelled results should show the
safe Application message and no review candidates.

---

## Single-Candidate Compatibility Rule

Preserve the existing single Cruise of the Week workflow.

When a Completed batch contains exactly:

- one candidate
- that candidate is Ready
- no Incomplete or Failed candidates
- `WasTruncated` is false

route the observation into the existing single captured-cruise review.

That means:

- `CapturedObservation` is populated
- the existing detailed single review remains visible
- `History.SetCapturedObservation` receives that observation
- the existing single `Record Observation` workflow remains available
- no duplicate batch row is required

For every genuine batch—including one candidate plus truncation or any
Incomplete/Failed candidate:

- show the batch review
- clear the single `CapturedObservation`
- call `History.SetCapturedObservation(null)` through the existing state setter
- do not choose one arbitrary Ready candidate for the single recorder
- do not display the single Record Observation action for the batch

This compatibility routing is presentation behavior only. Do not change the
Application or Infrastructure contracts.

---

## Candidate-Specific Open at TUI

Every published candidate has a source reference, but Avalonia must revalidate
it before requesting an OS launch.

For each row:

1. parse the exact source reference as an absolute URI
2. require the currently selected Cruise source
3. classify it through `CruiseTrustedHostPolicy`
4. enable Open at TUI only for `Trusted`
5. raise the existing `ExternalOpenRequested` event with that exact URI

Do not reconstruct, normalize or shorten the address.

Ready, Incomplete and Failed rows may expose Open at TUI when their published
reference remains trusted. This is useful when Robin wants to inspect why a
candidate was incomplete. Selection and external-open eligibility are separate.

The existing page-level Open at TUI action may remain for the currently browsed
page.

Reuse the existing controlled `ReportExternalOpenFailed` behavior when the OS
launcher fails.

Do not launch an external address directly from a ViewModel.

---

## Capture Lifecycle and Stale Results

Extend the existing capture generation and cancellation boundary to batch
capture.

The following must cancel the active capture, increment or invalidate the
generation and clear both single and batch review state:

- explicit Cancel Capture
- navigation to another address
- Back
- Forward
- Refresh
- browser close
- source change
- an untrusted navigation outcome
- a later capture request

A late earlier batch result must never:

- replace a newer review
- repopulate a cleared review
- restore old selection
- restore a single captured observation
- set Cruise History's captured observation
- re-enable commands for an obsolete operation

Capture should remain disabled while one capture is active.

Use one stable operation generation and one cancellation token per capture. Do
not use sleeps, polling or UI-thread blocking.

If the batch service throws unexpectedly, show a safe controlled failure and no
candidates. Do not expose exception details.

---

## Passive View

Update `CruiseBrowserFeasibilityView.axaml` to present the batch review through
bindings.

Add a bounded scrollable review area containing one concise row/card per
candidate.

A row should visibly distinguish:

- Ready
- Incomplete
- Failed

Suggested controls:

- a checkbox bound two-way to `IsSelected`, enabled only when `CanSelect`
- status and display label
- observation details when Ready
- message and missing fields when Incomplete/Failed
- exact candidate source reference
- Open at TUI button bound to the item command

Add parent actions:

```text
Select All Ready
Clear Selection
```

Show selected and status counts where useful.

Retain the existing single captured-cruise review panel for the compatibility
case.

The XAML must not:

- call services
- parse payloads
- decide trust
- perform selection logic
- record observations
- add event handlers for batch selection

Prefer bindings and commands. Keep the existing browser code-behind limited to
browser and OS-launch operations.

---

## History Boundary

The current single captured observation is handed to `CruiseHistoryViewModel`
so Robin can use the verified single Record Observation action.

Preserve that only for the single-clean-candidate compatibility rule.

For a batch:

- do not call the record use case
- do not set one selected or first candidate as History's captured observation
- do not refresh History
- do not add per-candidate recording status
- do not alter Cruise History data

037h-d will add explicit batch recording orchestration.

---

## Test Requirements

Use xUnit and existing hand-written ViewModel-test conventions.

### Batch Capture Request Tests

Add deterministic tests proving:

- capture is available only when the trusted page is ready and the required
  batch dependencies exist
- the existing explicit bridge event is raised exactly once
- duplicate capture requests are prevented while active
- the batch service receives the exact existing `CruisePageCaptureRequest`
- source identifier, retail source, page reference, payload and fixed clock
  timestamp are exact
- the exact cancellation token is forwarded
- blank, oversized and untrusted bridge values fail before service invocation
- unexpected service exceptions become a safe failed presentation

### Candidate Presentation Tests

Prove:

- Ready, Incomplete and Failed candidates remain in exact returned order
- every Ready row presents only its own observation details
- prices and promotions do not leak between rows
- absent optional values remain absent
- Incomplete rows show their own controlled message and missing fields
- Failed rows show their own safe message and no fabricated fields
- candidate status counts are exact
- a Completed batch with no Ready candidates is still reviewable
- the candidate collection cannot be structurally mutated externally
- truncation produces honest first-ten guidance
- batch-wide non-completed results show safe messages and no rows

### Selection Tests

Prove:

- selection begins clear
- an individual Ready candidate can be selected and cleared
- Incomplete and Failed candidates cannot become selected
- Select All Ready selects only Ready candidates
- Clear Selection clears all candidates
- selected count and command states update after individual and bulk changes
- a replacement capture resets selection

### Candidate Open Tests

Prove:

- a trusted candidate command raises the existing external-open event once
- the exact candidate URI is forwarded
- Ready and Incomplete candidates can be inspected when trusted
- an untrusted or malformed candidate URI is disabled defensively
- page-level Open at TUI behavior remains compatible
- an external-launch failure remains controlled

Use provider-independent test candidates. Do not construct Infrastructure DTOs.

### Single Compatibility Tests

Prove:

- exactly one Ready non-truncated candidate uses the existing single review
- it does not produce a duplicate batch row
- it is handed to the existing History ViewModel
- the existing single record action remains available under its current rules
- one Ready candidate with truncation uses batch review instead
- a mixed batch never gives History an arbitrary observation
- existing legacy single-service-only tests remain compatible

### Cancellation and Stale-Result Tests

Use controlled incomplete tasks and prove:

- explicit cancellation cancels the exact token and ignores late completion
- navigation ignores late completion and clears rows and selection
- Back, Forward, Refresh and Close clear batch state
- source change clears batch state
- a later capture supersedes an earlier pending capture
- a late earlier result cannot restore History's captured observation
- cancellation is a neutral action and does not publish partial candidates

### Composition and Passive-View Tests

Prove:

- shell composition resolves `CruiseOfTheWeekViewModel` with the registered batch
  service
- design-time/default construction remains controlled
- the XAML binds the batch ItemsSource and selection commands
- the XAML contains no batch-recording actions
- browser code-behind contains no selection or recording logic

### Test Data

Use:

- fictional provider-independent observations and batch results
- fixed timestamps with a non-zero offset
- fictional HTTPS references
- hand-written fake single and batch services
- controlled incomplete tasks
- exact assertions

Do not use:

- live HTTP or DNS
- live TUI pages or payloads
- `NativeWebView` in tests
- JavaScript execution
- browser automation
- OS URL launching
- SQLite or Robin's production database
- `DateTimeOffset.Now` or `UtcNow`
- mocking libraries
- reflection
- sleeps or timing-dependent tests
- mutable shared fixtures

---

## Expected Files

Expected production changes:

```text
KrytenAssist.Avalonia/ViewModels/CruiseBrowserFeasibilityViewModel.cs
KrytenAssist.Avalonia/ViewModels/CruiseCaptureCandidateReviewItemViewModel.cs
KrytenAssist.Avalonia/ViewModels/CruiseOfTheWeekViewModel.cs
KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml
```

Expected test changes may include:

```text
KrytenAssist.Avalonia.Tests/ViewModels/CruiseBatchCaptureReviewViewModelTests.cs
KrytenAssist.Avalonia.Tests/ViewModels/CruiseCaptureLifecycleViewModelTests.cs
KrytenAssist.Avalonia.Tests/ViewModels/CruiseCaptureReviewViewModelTests.cs
KrytenAssist.Avalonia.Tests/ViewModels/CruiseOfTheWeekViewModelTests.cs
KrytenAssist.Avalonia.Tests/DependencyInjection/CruiseDiscoveryDependencyInjectionTests.cs
```

Prefer one focused new batch-review test file rather than expanding an existing
single-capture test file into an unrelated oversized suite.

No Infrastructure, Application, Core, persistence or migration file is
expected to change.

---

## Production Correction Policy

The expected production changes are limited to Avalonia ViewModels and the
passive Cruise browser view.

If a required deterministic test exposes a genuine defect elsewhere:

1. retain the focused failing regression test
2. make the smallest correction
3. preserve every verified existing behavior
4. rerun focused single and batch capture tests
5. report the correction explicitly

Do not change the Application batch contract or TUI adapter merely to simplify
presentation code.

---

## Required Commands

Inspect and preserve the initial worktree.

Run focused tests for:

- the new batch capture review lifecycle
- candidate presentation and selection
- candidate external-open trust
- existing single capture lifecycle and review
- Cruise History single-observation compatibility
- Cruise of the Week and shell composition
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

Prompt 037h-c is complete when:

- the Cruise workspace consumes `ICruisePageBatchCaptureService`
- Capture Loaded Cruises uses the existing bounded explicit browser bridge
- completed batch candidates appear in exact adapter order
- Ready, Incomplete and Failed rows are honest and independently presented
- selection begins clear
- only Ready candidates can be selected
- Select All Ready and Clear Selection behave deterministically
- selection counts and command states remain correct
- each candidate can request an exact trusted Open at TUI action
- untrusted candidate references cannot be launched
- honest truncation and loaded-card summaries are shown
- one clean Ready candidate retains the existing single review and record path
- genuine batches never choose one arbitrary observation for History
- navigation, cancellation and later capture invalidate stale results
- the view remains passive and MVVM-driven
- no batch recording or persistence behavior was added
- focused offline tests pass
- the complete solution builds
- the complete regression suite passes
- this prompt's Results and Lessons Learned are complete

Do not begin 037h-d.

Stop after 037h-c.

---

## Completion Report

Provide:

### Summary

Describe the batch lifecycle, candidate review and selection experience.

### Single-Capture Compatibility

Confirm the single-clean-candidate routing and existing single record path.

### Selection and Trust

Report Ready-only selection, bulk actions and candidate-specific Open at TUI
validation.

### Lifecycle

Report cancellation, navigation clearing and stale-result protection.

### Files Modified

List every created and updated file.

### Production Corrections

Use `None` or report every test-proven correction outside expected files.

### Build and Tests

Report exact commands, totals, failures, skipped tests, errors and warnings.

### Architecture and Scope

Confirm no Application/Core/Infrastructure changes, recording, persistence,
live automation or Prompt 038 behavior was added.

---

## Results

> Complete during implementation.

### Status

Complete.

### Batch Capture Lifecycle

`CruiseBrowserFeasibilityViewModel` now consumes the optional Application batch
capture service through the existing bounded payload bridge. When composed, the
capture action uses "Capture Loaded Cruises", forwards the exact existing
request and cancellation token, and presents controlled batch-wide outcomes.
The legacy single service remains the fallback when the batch dependency is
absent.

### Candidate Review

Added an immutable ordered review collection backed by focused candidate item
ViewModels. Ready rows present only their own observation, while Incomplete and
Failed rows retain their candidate-local safe messages and missing-field
evidence without fabricated Cruise details. Counts, neutral prices, promotions
and honest truncation guidance are exposed through the parent ViewModel.

### Selection

Selection begins clear. Individual selection is restricted to Ready candidates;
Select All Ready selects only Ready rows and Clear Selection removes every
selection. Selected counts and command availability update immediately. The
collection is structurally read-only while item selection remains controlled
presentation state.

### Candidate Open at TUI

Every candidate row has an Open at TUI command only when its exact reference is
revalidated as trusted by the current Avalonia source policy. The command raises
the existing external-open event with the exact URI. Defensive tests prove an
untrusted provider-independent candidate reference cannot be launched.

### Single-Capture Compatibility

A Completed batch containing exactly one Ready candidate with no truncation is
routed into the existing single captured-cruise review and Cruise History
hand-off. The existing Record Observation path therefore remains available.
Every genuine batch clears the single observation and never chooses an
arbitrary candidate for History.

### Stale-Result Protection

The existing capture generation and cancellation token now cover both single
and batch services. Cancel, navigation, Back, Forward, Refresh and browser close
clear batch rows and selection. Late cancelled completions are ignored and
unexpected batch exceptions become safe failures without candidate data.

### Files Created

- `KrytenAssist.Avalonia/ViewModels/CruiseCaptureCandidateReviewItemViewModel.cs`
- `KrytenAssist.Avalonia.Tests/ViewModels/CruiseBatchCaptureReviewViewModelTests.cs`
- `docs/Codex Prompts/037h-c - Multi-Cruise Capture Review.md`

### Files Updated

- `KrytenAssist.Avalonia/ViewModels/CruiseBrowserFeasibilityViewModel.cs`
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
dotnet test KrytenAssist.Avalonia.Tests/KrytenAssist.Avalonia.Tests.csproj --no-restore --filter "FullyQualifiedName~CruiseBatchCaptureReviewViewModelTests|FullyQualifiedName~CruiseCaptureLifecycleViewModelTests|FullyQualifiedName~CruiseCaptureReviewViewModelTests|FullyQualifiedName~CruiseHistoryViewModelTests|FullyQualifiedName~CruiseOfTheWeekViewModelTests|FullyQualifiedName~CruiseDiscoveryDependencyInjectionTests|FullyQualifiedName~ShellDependencyInjectionTests"
```

Result: 75 passed, 0 failed, 0 skipped. The command reported the existing
NU1903 advisory and two existing unused command-event compiler warnings.

### Complete Regression Suite

Passed:

```text
dotnet test KrytenAssist.sln --no-build --no-restore
```

Results:

- Core: 105 passed, 0 failed, 0 skipped
- Avalonia: 409 passed, 0 failed, 0 skipped
- API: 9 passed, 0 failed, 0 skipped
- Total: 523 passed, 0 failed, 0 skipped

### Offline Check

All new tests use fictional provider-independent observations and batch results,
fixed timestamps, hand-written fake services and controlled incomplete tasks.
No HTTP, DNS, live TUI page, browser, JavaScript, OS launcher, SQLite or personal
data was used.

### Architecture and Scope Check

Changes are limited to Avalonia ViewModels, passive XAML, focused tests and this
prompt. The existing browser code-behind and fixed script were unchanged. No
Application, Core, Infrastructure, persistence, migration, batch-recording,
Roadmap, Playbook Results, Session Handover or Prompt 038 behavior was added.

### Notes

Manual review should confirm the batch panel remains readable with ten loaded
cards at the target desktop window size and that candidate Open at TUI launches
the expected itinerary. Automated XAML compilation passed; no live page was
opened during this step.

---

## Lessons Learned

> Complete after implementation.

- Routing one clean Ready result back into the proven single review preserves
  the existing Cruise of the Week workflow without making the batch model
  pretend to be a single result.
- Keeping batch candidates out of `CruiseHistoryViewModel` until an explicit
  later record action prevents the first or selected item from being persisted
  accidentally.
- A structurally read-only collection with mutable Ready-only item selection
  gives the next step a stable reviewed input without mutating Application or
  Core models.
- Revalidating candidate references in Avalonia provides useful defence in
  depth while retaining the existing browser/OS-launch bridge.
- Reusing the existing generation boundary made single and batch stale-result
  protection consistent and avoided a parallel capture lifecycle.
