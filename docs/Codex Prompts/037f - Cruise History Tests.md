# Codex Prompt 037f – Cruise History Tests

## Implementation Prompt

Implement **Step 6 only** from:

```text
docs/AI Playbook/037 - Cruise History and Price Tracking.md
```

Prompts 037a–037e are complete and committed. Robin has manually confirmed that
a captured Cruise price can be recorded and that the history remains available
after restarting Kryten.

The current verified baseline is:

```text
Core: 105 passed
Avalonia: 325 passed
API: 9 passed
Total: 439 passed, 0 failed, 0 skipped
```

This is a deterministic test-completion step. Do not implement Prompt 037g or
begin Prompt 038.

---

## Required Reading

Read these files in order before changing tests:

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/037 - Cruise History and Price Tracking.md`
5. Prompts 037a–037e, including their Results and Lessons Learned
6. all existing Core Cruise History tests
7. all existing Application Cruise History tests
8. all existing Infrastructure Cruise persistence, migration, concurrency,
   cancellation and restart tests
9. all existing Cruise History presentation and desktop-composition tests
10. the production types directly exercised by any proposed new test

Do not start by adding a broad duplicate test matrix. First map each Prompt 037
acceptance criterion to existing deterministic coverage, then add only the
missing high-value cases described below.

---

## Goal

Complete deterministic automated coverage for Cruise History and Price Tracking.

Prove across the existing architecture that:

- explicitly recorded observations retain their exact provider-independent
  evidence
- stable sailing identity and retail-source separation remain intact
- meaningful changes create snapshots and identical evidence does not
- last-seen evidence can advance without inventing another snapshot
- comparable prices never mix currency or basis
- headline summaries and trends remain deterministic
- SQLite migrations, transactions and concurrency behavior remain correct
- a recorded history survives complete disposal and recreation of the desktop
  persistence graph
- local history loads without TUI, HTTP, browser navigation or external work
- recording and loading cancellation cannot publish stale presentation state
- browser capture-review clearing never clears persisted or loaded history
- list selection remains stable across refresh and falls back safely when needed
- display formatting is honest for missing values and non-GBP prices
- the complete solution remains green

This prompt should normally modify tests and its own Results section only.

---

## Scope

This prompt owns:

- auditing existing Prompt 037 acceptance coverage
- missing Core and Application edge cases only where the audit demonstrates a
  gap
- an isolated cross-layer desktop record/dispose/recreate/load workflow test
- remaining Cruise History ViewModel cancellation and stale-result tests
- remaining refresh, selection and controlled-state presentation tests
- focused formatting coverage for all existing trend and price states
- rerunning migration, rollback, concurrency and restart regressions
- structural confirmation that all new tests are offline and isolated
- minimal production corrections proved necessary by deterministic tests
- updating this prompt's Results and Lessons Learned

This prompt does **not** own:

- live TUI requests or browser automation
- repeating the manual TUI workflow
- new capture selectors or parsing behavior
- changing the Cruise History architecture
- original-price versus discounted-price redesign
- ratings, notes, favourites or preferences
- deletion, export, charts, search or filtering
- monitoring, alerts or booking behavior
- final architecture verification
- updating the Playbook Results
- updating the Roadmap
- creating a Session Handover
- Prompt 038 behavior

Those final documentation and verification tasks belong to 037g. The richer
price model remains explicitly deferred.

---

## Allowed Changes

Prefer changes only within:

```text
KrytenAssist.Core.Tests/
KrytenAssist.Avalonia.Tests/
docs/Codex Prompts/037f - Cruise History Tests.md
```

The Application and Infrastructure tests currently live in the existing
Avalonia test project because of the established project-reference arrangement.
Do not create another test project merely to reorganise them.

Do not add a mocking library, coverage package, database package or other NuGet
dependency.

Production files may be changed only if a new deterministic failing test proves
a genuine Prompt 037 defect. Every correction must be:

- minimal
- architecture-consistent
- limited to existing Prompt 037 behavior
- covered by a focused regression test
- reported explicitly under Production Corrections

Do not stage, commit, push, discard or overwrite Robin's work.

---

## Existing Coverage to Preserve

### Core

Review the existing tests for:

```text
CruiseSailingKey
CruiseObservationFingerprint
CruiseObservationFingerprintOrdering
CruisePrice
CruisePriceHistoryAnalyzer
CruiseObservation
CruiseSnapshot
```

They already cover stable identity, canonicalization, meaningful-change
fingerprints, deterministic ordering, compatible-price selection, summary
calculation, mixed-series exclusion, missing retail source and defensive copies.

Do not reproduce these matrices. Add a Core test only if an acceptance rule has
no existing direct assertion.

### Application

Preserve and audit:

```text
KrytenAssist.Avalonia.Tests/Application/Cruises/RecordCruiseObservationTests.cs
KrytenAssist.Avalonia.Tests/Application/Cruises/CruiseHistoryQueryUseCaseTests.cs
KrytenAssist.Avalonia.Tests/Application/Cruises/CruiseRecordedHistoryTests.cs
KrytenAssist.Avalonia.Tests/Application/Cruises/FakeCruiseObservationRepository.cs
KrytenAssist.Avalonia.Tests/Application/Cruises/CruiseHistoryApplicationTestData.cs
```

These already cover use-case mapping, exact evidence forwarding, cancellation,
safe failure, deterministic list ordering and result invariants.

### Infrastructure

Preserve and rerun:

```text
KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/SqliteCruiseObservationRepositoryTests.cs
KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/CruisePersistenceSchemaAndMigrationTests.cs
KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/CruiseHistoryHardeningMigrationTests.cs
KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/CruiseHistoryRestartPersistenceTests.cs
KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/CruiseObservationConcurrencyTests.cs
KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/CruiseObservationCancellationTests.cs
KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/CruisePersistenceDependencyInjectionTests.cs
```

These already cover normalized SQLite round trips, checked-in migrations,
upgrade preservation, constraints, rollback, multiple sources, source-less
history, concurrent recording and repository restart persistence.

### Avalonia Presentation and Composition

Preserve and extend:

```text
KrytenAssist.Avalonia.Tests/ViewModels/CruiseHistoryViewModelTests.cs
KrytenAssist.Avalonia.Tests/DependencyInjection/CruiseHistoryDesktopCompositionTests.cs
KrytenAssist.Avalonia.Tests/ViewModels/CruiseCaptureLifecycleViewModelTests.cs
KrytenAssist.Avalonia.Tests/ViewModels/CruiseBrowserFeasibilityViewModelTests.cs
KrytenAssist.Avalonia.Tests/ViewModels/ShellViewModelTests.cs
```

037e already covers record availability, first/lower/already-current outcomes,
failure retry, initial activation, refresh selection, browser-close separation,
basic formatting and offline shell activation.

037f should concentrate on the missing asynchronous and cross-layer behavior.

---

## Required Cross-Layer Restart Test

Add one focused integration test using the real desktop composition and a unique
temporary file database.

The test must:

1. create a unique directory beneath `Path.GetTempPath()`
2. construct an isolated database path inside it
3. build the desktop persistence graph with that explicit path
4. resolve the existing record use case or Cruise History ViewModel through DI
5. record a provider-independent observation with:
   - stable sailing identity
   - retail source
   - known price and price basis
   - offer identifier
   - source reference
   - timestamp with a non-zero offset
6. confirm the record succeeds
7. fully dispose scopes and the first service provider
8. build a new service provider against the same file
9. load history without resolving or invoking browser/TUI services
10. confirm exactly one history is present
11. assert the exact sailing identity, retail source, price, evidence,
    observation timestamp and summary values
12. confirm the database file exists only at the supplied test path
13. dispose all services before deleting the temporary directory in `finally`

This must prove the full composition/restart path, not merely call the same
repository instance twice.

Do not read, copy, overwrite or assert against the production path returned by:

```text
DesktopPersistenceServiceCollectionExtensions.GetDefaultDatabasePath()
```

---

## Required Presentation Lifecycle Coverage

Use public commands, properties and lifecycle methods. Use controlled tasks or
hand-written repositories; do not use timing delays.

### Recording

Add coverage for the important missing cases:

- Cancel Recording is enabled only while recording
- cancellation is forwarded to the Application operation
- cancelled recording retains the captured review and permits retry
- changing or clearing the capture while recording cancels the prior operation
- a late result for a prior capture cannot update the new review's message,
  completion state, history selection or command availability
- duplicate command execution while recording invokes the use case once
- higher comparable price produces an accurate controlled message
- unchanged comparable price with other changed evidence is described accurately
- changed evidence with unavailable comparable price remains honest
- source-less first observation uses neutral wording

Do not test private command types or internal generation counters. Prove behavior
through the public presentation contract.

### Loading and Refresh

Add coverage for:

- Cancel History Loading is enabled only while loading
- cancellation is forwarded to the list operation
- deactivation cancels an active load
- a cancelled or stale load cannot overwrite a later successful result
- a failed refresh retains the last successful list and selection
- retry after failure can publish a successful result
- empty history can become populated on refresh
- selection is retained by sailing key and retail source
- the same sailing from two retail sources remains two selectable histories
- if the selected history disappears, selection falls back deterministically
- browser source change, refresh and close clear only capture-review state

Do not require real UI dispatching or Avalonia controls. The existing ViewModel
boundary is the unit under test.

---

## Required Formatting Coverage

Complete the existing `CruiseHistoryItemViewModel` assertions for the states
already supported by production code:

- GBP prices use `£`
- non-GBP prices retain the currency code
- per-person and total basis remain visible
- unavailable price is explicit
- First, Up, Down, Unchanged and Unavailable trend states are distinct
- one-night and multiple-night duration wording are correct
- past, upcoming and departure-day status uses an injected fixed clock
- first observed, last observed and last seen remain distinct
- absent retail source, offer identifier and source reference are safe and honest
- formatting never calls the single known captured price a discounted price

Use exact assertions where text is part of the user-facing contract. Avoid
asserting insignificant whitespace or AXAML layout details.

---

## Migration, Transaction and Concurrency Regression

Run all existing persistence tests that prove:

- an empty database migrates through every checked-in migration
- the pre-Cruise database upgrades without losing Prompt Cards
- the 037c schema upgrades to the hardened 037d schema
- disposal and recreation retain histories, snapshots and latest evidence
- first and changed recordings are atomic
- cancellation and constraint failure roll back the entire aggregate
- concurrent identical writes create one snapshot
- concurrent distinct meaningful changes are retained without orphaned rows
- duplicate observations update last-seen evidence without increasing snapshot
  count
- return to a prior advertised state creates a new chronological snapshot
- different retail sources never share a price series

If all required cases already have direct assertions, do not add duplicate tests.
Report the existing files and focused command used to verify them.

---

## Offline and Isolation Rules

Every new test must be deterministic and offline.

Use:

- xUnit
- Arrange, Act and Assert
- descriptive test names
- fixed clocks with explicit offsets
- hand-written fakes
- `TaskCompletionSource` with asynchronous continuations for controlled in-flight
  operations
- unique temporary SQLite paths
- explicit provider and connection disposal
- `try/finally` cleanup
- exact assertions for stored and presented evidence

Do not use:

- mocking libraries
- reflection
- sleeps, polling or timing retries
- the system clock
- shared mutable databases
- Robin's Local Application Data database
- HTTP, DNS or `HttpClient`
- `NativeWebView`
- browser automation
- OS launchers
- TUI HTML, JavaScript execution or live page content
- test ordering
- cookies, credentials or personal booking information

No test may rely on network availability or an installed browser.

---

## Production Correction Policy

Do not change production code speculatively.

When a required deterministic test exposes a defect:

1. retain the focused failing regression test
2. identify which existing Prompt 037 contract is violated
3. make the smallest architecture-consistent production correction
4. rerun the affected focused suite
5. rerun the complete suite
6. document the correction and evidence in Results

Do not use 037f to refactor production classes, rename public contracts or add
future behavior merely because the tests make an improvement convenient.

---

## Suggested Test Organisation

Prefer extending:

```text
KrytenAssist.Avalonia.Tests/ViewModels/CruiseHistoryViewModelTests.cs
```

Create a narrowly named file for the cross-layer workflow, for example:

```text
KrytenAssist.Avalonia.Tests/Integration/CruiseHistoryDesktopRestartWorkflowTests.cs
```

If controlled asynchronous fakes would make the existing ViewModel file too
large, place them in a focused test-support type under the existing test project.
Do not turn general test data into production code.

Prefer adding no Core, Application or repository tests unless the coverage audit
finds a concrete missing acceptance assertion.

---

## Required Commands

Before implementation, inspect the worktree and preserve unrelated changes.

Run the directly affected ViewModel and integration tests with an explicit
filter. Record the exact filter and totals in Results.

Run the complete existing Cruise History test group, including Core,
Application, persistence, migration, concurrency, restart, composition and
presentation tests. Record the exact commands and totals.

Build the solution:

```text
dotnet build KrytenAssist.sln --no-restore
```

Run the complete regression suite:

```text
dotnet test KrytenAssist.sln --no-build --no-restore
```

Report exact totals, failures, skipped tests, errors and warnings.

---

## Definition of Done

Prompt 037f is complete when:

- the existing Prompt 037 coverage map has been audited
- duplicate domain and persistence matrices were avoided
- a real isolated desktop composition record/restart/load test passes
- that test proves exact evidence and summary persistence across provider
  recreation
- recording cancellation, stale capture and duplicate execution are covered
- loading cancellation, stale result, retry and selection fallback are covered
- multi-source presentation separation is covered
- all existing trend, price and missing-value presentation states are covered
- all new databases use explicit unique temporary paths
- no automated test performs browser, TUI, HTTP or other external work
- migration, rollback, concurrency and restart regressions pass
- any production correction is minimal, covered and documented
- the solution builds
- the complete regression suite passes
- this prompt's Results and Lessons Learned are complete

Do not begin Prompt 037g.

Stop after Prompt 037f.

---

## Completion Report

Provide:

### Summary

Describe the test gaps closed without merely reporting a test count.

### Coverage Audit

Explain which Core, Application and Infrastructure areas were already complete
and where new coverage was added.

### Cross-Layer Restart Workflow

Report the exact record/dispose/recreate/load behavior proved and database
isolation strategy.

### Presentation Lifecycle

Report recording/loading cancellation, stale-result, retry and selection tests.

### Files Modified

List every created and updated file.

### Production Corrections

Use `None` or report each verified correction and its regression test.

### Build and Tests

Report exact commands, project totals, failures, skipped tests, warnings and
errors.

### Offline and Isolation Check

Confirm no real database, network, browser or OS-launcher work occurred.

### Architecture and Scope

Confirm no persistence moved into presentation and no richer price model or
Prompt 038 behavior was introduced.

---

## Results

> Complete during implementation.

### Status

Complete. Deterministic coverage was extended without external work, both
test-proven presentation defects were corrected, and the complete regression
suite passes.

### Coverage Audit

The audit found the Core identity, fingerprint, comparable-price and summary
matrices already comprehensive. Application use-case mapping, result invariants,
query ordering and safe cancellation/failure behavior were also directly
covered. Infrastructure already had real migration, normalized round-trip,
constraint, rollback, concurrency, multi-source and repository-restart tests.

No duplicate Core, Application or repository matrices were added. New coverage
was limited to the missing desktop composition restart workflow and Avalonia
recording/loading lifecycle and formatting edges.

### Cross-Layer Restart Workflow

Added a real desktop-composition integration test using a unique database beneath
`Path.GetTempPath()`. It records through the DI-resolved Application use case,
fully disposes the scope and service provider, creates a second provider against
the same file and loads history through a newly resolved query use case.

The test proves the exact sailing key, retail source, offer, prices, promotion,
timestamp offset, source reference, latest evidence and headline summary survive
recreation. It resolves no browser, TUI, HTTP or OS-launcher service and deletes
the isolated directory after every run.

### Presentation Lifecycle Coverage

Added deterministic coverage for:

- recording cancellation and cancellation-token forwarding
- duplicate execution prevention while recording
- retry after cancellation
- capture replacement cancelling and rejecting a late result
- immediate recording availability for the replacement capture
- higher, unchanged, unavailable and source-less controlled messages
- load cancellation and token forwarding
- deactivation cancellation
- stale load rejection after a later successful refresh
- failed refresh retaining the last good list and selection
- successful retry with deterministic fallback selection
- same-sailing multi-source selection retention

Controlled incomplete tasks and command/property events are used rather than
sleeps, polling or timing assumptions.

### Formatting Coverage

Completed presentation assertions for GBP and non-GBP prices, total and
per-person bases, First/Up/Down/Unchanged/Unavailable trends, one-night wording,
departure-day status, unavailable retail source, absent source reference and
neutral price terminology. The earlier tests continue to prove distinct first,
last-observed and last-seen timestamps and past/upcoming histories.

### Existing Persistence Regressions

Reran the existing migration, hardening upgrade, repository round-trip,
cancellation rollback, constraint rollback, concurrency, source separation and
restart-persistence coverage. No new persistence defect or missing acceptance
case was found, so those test matrices were preserved unchanged.

### Files Created

- `KrytenAssist.Avalonia.Tests/Integration/CruiseHistoryDesktopRestartWorkflowTests.cs`
- `docs/Codex Prompts/037f - Cruise History Tests.md`

### Files Updated

- `KrytenAssist.Avalonia.Tests/Application/Cruises/FakeCruiseObservationRepository.cs`
- `KrytenAssist.Avalonia.Tests/ViewModels/CruiseHistoryViewModelTests.cs`
- `KrytenAssist.Avalonia/ViewModels/CruiseHistoryItemViewModel.cs`
- `KrytenAssist.Avalonia/ViewModels/CruiseHistoryViewModel.cs`

### Production Corrections

Two focused regression tests exposed presentation defects:

1. A one-night sailing displayed `1 nights`. Duration formatting now uses the
   singular `night` only when the duration is one.
2. Cancelling history loading updated visible state but the asynchronous command
   remained internally busy until a cancellation-ignoring operation returned.
   The existing generation-based stale-result boundary now also resets command
   execution safely, permitting an immediate retry without allowing the prior
   completion to alter the new operation. Capture replacement uses the same
   boundary so a new review is immediately recordable while late prior work is
   ignored.

No domain, Application or persistence production correction was required.

### Build

Passed:

```text
dotnet build KrytenAssist.sln --no-restore
```

Result: 0 errors and 5 warnings. All five are the existing NU1903 advisory for
`SQLitePCLRaw.lib.e_sqlite3` 2.1.11.

### Focused Tests

Passed the new and extended presentation/restart set:

```text
dotnet test KrytenAssist.Avalonia.Tests/KrytenAssist.Avalonia.Tests.csproj --no-restore --filter 'FullyQualifiedName~CruiseHistoryViewModelTests|FullyQualifiedName~CruiseHistoryDesktopRestartWorkflowTests'
```

19 passed, 0 failed, 0 skipped.

Passed the focused Core Cruise History regression group:

```text
dotnet test KrytenAssist.Core.Tests/KrytenAssist.Core.Tests.csproj --no-restore --filter 'FullyQualifiedName~CruiseSailingKey|FullyQualifiedName~CruiseObservation|FullyQualifiedName~CruisePriceHistory|FullyQualifiedName~CruisePriceTests'
```

58 passed, 0 failed, 0 skipped.

Passed the broader Application, persistence, migration, concurrency, restart,
composition and presentation group:

```text
dotnet test KrytenAssist.Avalonia.Tests/KrytenAssist.Avalonia.Tests.csproj --no-restore --filter 'FullyQualifiedName~CruiseHistory|FullyQualifiedName~CruiseObservation'
```

54 passed, 0 failed, 0 skipped.

### Complete Regression Suite

Passed:

```text
dotnet test KrytenAssist.sln --no-build --no-restore
```

- Core: 105 passed, 0 failed, 0 skipped
- Avalonia: 336 passed, 0 failed, 0 skipped
- API: 9 passed, 0 failed, 0 skipped
- Total: 450 passed, 0 failed, 0 skipped

This adds 11 Avalonia tests to the verified 439-test baseline.

### Offline and Isolation Check

Confirmed by construction. New lifecycle tests use hand-written fakes, fixed
timestamps and controlled tasks. The integration test supplies an explicit
unique temporary SQLite path and disposes both providers before cleanup. No test
uses the production Local Application Data path, network, DNS, `HttpClient`,
`NativeWebView`, TUI content, OS launchers or the system clock.

### Architecture and Scope Check

Confirmed. Persistence remains in Infrastructure, use cases and abstractions
remain in Application, domain analysis remains in Core and Avalonia retains only
composition/presentation responsibilities. The two production corrections are
local presentation fixes. No richer price model, ratings, preferences, alerts,
booking behavior, Prompt 037g work or Prompt 038 behavior was introduced.

### Notes

Robin's successful manual record/restart workflow remains documented in 037e and
was not repeated as live automation. 037f adds the deterministic isolated
equivalent. Final Playbook, Roadmap and Session Handover updates remain for 037g.

The known SQLite package advisory predates this prompt and was not expanded into
an unrelated dependency upgrade.

---

## Lessons Learned

> Complete after implementation.

- The highest-value remaining persistence test was not another repository round
  trip but recreation of the complete desktop DI graph around the same isolated
  file.
- Cancellation has two independent concerns: cancelling the underlying token and
  releasing presentation command availability. Generation checks are required
  so the released command cannot be corrupted by a late prior completion.
- Capture generations provide a clean boundary between session review state and
  persisted history while allowing a replacement capture to be used immediately.
- Field-by-field persistence assertions are more precise than relying on record
  equality when immutable models contain separately reconstructed read-only
  collections.
- A coverage audit prevented duplication: existing Core and SQLite tests already
  proved the difficult identity, comparison, migration, transaction and
  concurrency rules.
- Small language cases such as `1 night` belong in deterministic presentation
  tests because they are visible product behavior even when the underlying value
  is correct.
