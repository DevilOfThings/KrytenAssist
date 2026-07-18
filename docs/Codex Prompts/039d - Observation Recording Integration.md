# Codex Prompt 039d – Observation Recording Integration

## Implementation Prompt

Implement **Step 4 only** from:

```text
docs/AI Playbook/039 - Price Drop Alerts.md
```

Prompts 039a–039c are complete. The alert domain, pure recorded-observation
detector, Application evaluation contracts and normalized SQLite persistence
already exist.

This step integrates Price Drop and Promotion evaluation with the existing
explicit single and batch **Record Observation** workflows. Do not implement
Saved Criteria triggers, the Alert Centre or alert settings presentation yet.

---

## Required Reading

Read these files in order before changing code:

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/037 - Cruise History and Price Tracking.md`
5. `docs/AI Playbook/037h - Multiple Cruise Deals Handling.md`
6. `docs/AI Playbook/039 - Price Drop Alerts.md`
7. `docs/Codex Prompts/039a - Price Drop Alert Experience and Contract.md`
8. `docs/Codex Prompts/039b - Alert Domain and Application Contracts.md`,
   including Results
9. `docs/Codex Prompts/039c - SQLite Alert Persistence.md`, including Results
10. `docs/Codex Prompts/037d - Observation Recording and History Queries.md`,
    including Results and Lessons Learned
11. `docs/Codex Prompts/037h-d - Batch Observation Recording.md`, including
    Results and Lessons Learned
12. the existing observation recorder, History query, alert evaluator,
    composite result and dependency-injection registrations
13. `CruiseHistoryViewModel`, `CruiseBrowserFeasibilityViewModel` and
    `CruiseCaptureCandidateReviewItemViewModel`
14. existing single-recording, batch-recording, History, alert use-case and
    persistence tests

Do not begin implementation until the distinction between a committed factual
recording and its later alert-evaluation outcome is understood.

---

## Goal

After Robin explicitly records a changed Cruise observation, evaluate the
newly committed current evidence for Price Drop and Promotion alerts.

The single workflow becomes:

```text
Record Observation
        ↓
commit factual Cruise History
        ↓
if a changed snapshot became current, load its committed same-source History
        ↓
evaluate previous current → new current evidence
        ↓
materialize zero or more deduplicated alerts
        ↓
return recording and alert outcomes independently
```

The batch workflow applies the same composite operation sequentially to each
attempted candidate. Every Cruise remains an independent recording and alert
evaluation boundary.

The implementation must:

- reuse `RecordCruiseObservation` for factual persistence
- reuse `EvaluateRecordedCruiseAlerts` for pure detection and materialisation
- add a focused Application record-then-evaluate orchestrator
- evaluate only after a successful changed observation becomes the committed
  current observation
- preserve first-observation and `AlreadyCurrent` behavior without change
  evaluation
- preserve successful recording when later History lookup or alert evaluation
  is cancelled or fails
- return and surface created-alert counts separately from recording status
- use the existing deterministic event keys and SQLite uniqueness for retry and
  concurrency convergence
- wire both single and batch recording through the same composite use case
- preserve all existing capture, review, selection, sequential processing and
  History-refresh behavior
- remain provider-independent, deterministic and offline

---

## Scope

This step owns:

- a focused Application `RecordCruiseObservationAndEvaluateAlerts`-style use
  case; naming may follow established conventions
- orchestration of existing factual recording, committed History lookup and
  recorded-observation alert evaluation
- deterministic selection of the previous and current committed observations
  from the exact sailing and retail-source History
- explicit caller-supplied alert creation time
- composite recording/evaluation outcomes for all branches
- Application dependency-injection registration
- replacement of direct single and batch recorder calls at the Avalonia
  composition boundary
- controlled single-recording feedback for created alerts and evaluation
  cancellation/failure
- controlled per-row and aggregate batch feedback for created alerts and
  evaluation cancellation/failure
- preservation of one post-batch Cruise History refresh
- focused Application, ViewModel and dependency-injection tests
- this prompt's Results and Lessons Learned
- Roadmap status after implementation

This step does **not** own:

- Saved Criteria evaluation or triggers from recording (039e)
- Save Cruise, Restore, Save Preferences or saved-state integration (039e)
- Alert Centre, unread badge, filters, detail, lifecycle commands or settings
  editor (039f)
- a general notification service or operating-system notification
- changing alert settings from the recording UI
- a scheduler, background capture, scraping or network work
- cross-retailer comparison, currency conversion or price-basis inference
- cabin availability matching or Prompt 040 work
- changing Cruise History identity, fingerprint or persistence semantics
- changing alert schema or migrations unless a focused failing test proves a
  concrete 039c persistence defect
- parallel batch recording or a cross-Cruise transaction
- redesigning capture review, selection or History presentation

Do not add future 039e–039g behavior.

---

## Architecture Boundary

Preserve:

```text
Avalonia explicit Record action
        ↓
Application record-then-evaluate orchestrator
        ├── existing RecordCruiseObservation
        │       ↓
        │   ICruiseObservationRepository
        │
        ├── existing committed History query
        │       ↓
        │   ICruiseObservationRepository
        │
        └── existing EvaluateRecordedCruiseAlerts
                ↓
            alert settings + alert repository
```

Avalonia may consume only Application-owned models and controlled results. It
must not select alert candidates, compare prices/promotions, query alert
repositories directly or use Infrastructure types.

The Application orchestrator must compose existing use cases/contracts. It
must not:

- call EF Core or SQLite directly
- create one transaction spanning History and alerts
- copy detector logic
- make the observation repository depend on alerts
- make alert persistence depend on Cruise History rows
- leak browser, DOM, TUI or provider payload types

---

## Required Application Orchestration

Add one focused use case accepting:

```text
CruiseObservation observation
DateTimeOffset alertCreatedAt
CancellationToken cancellationToken
```

It returns the existing:

```text
CruiseRecordAndAlertResult
```

Prefer retaining the existing composite result. Change a 039b contract only if
a focused test proves it cannot represent the required independent outcomes;
document any correction.

### Required Branches

#### Recording Cancelled or Failed

- return the exact controlled recording result
- do not load History for evaluation
- do not invoke `EvaluateRecordedCruiseAlerts`
- set alert evaluation to not attempted (`Alerts` remains null)

#### First Observation Recorded

- preserve the successful recording result
- do not invoke change detection because there is no prior evidence
- set alert evaluation to not attempted

#### Already Current

- preserve the successful `AlreadyCurrent` result
- do not invoke change detection or materialisation
- set alert evaluation to not attempted
- re-recording current evidence must not manufacture or duplicate an alert

#### Changed Observation Recorded

Only this branch may attempt recorded-observation alert evaluation.

After the factual recorder returns success:

1. load the exact committed History for the observation's sailing key and
   retail source through an existing Application query/use-case boundary
2. deterministically order evidence by the established domain chronology:
   `ObservedAt`, then canonical observation fingerprint
3. confirm the newly supplied observation is the committed current observation
4. if it is current and a previous observation exists, pass exactly the
   immediately previous and current observations to
   `EvaluateRecordedCruiseAlerts`
5. pass the caller-supplied `alertCreatedAt` unchanged
6. return the recording result plus the exact controlled evaluation result

If an older distinct observation was retained historically but did not become
current, preserve `ChangedObservationRecorded` and return a successful
zero-candidate evaluation or another explicit non-failure representation
consistent with the existing contract. Do not compare the older insertion
against arbitrary neighbours and do not alert from a change that is not the
new committed current evidence.

If committed History is unexpectedly unavailable, malformed or cannot be
loaded, return the successful recording result plus controlled alert failure.
Never rewrite the recording as failed.

Do not add raw observations to `CruiseObservationRecordResult` merely for UI
convenience. If the smallest correct Application composition requires a
focused committed-History query, keep it inside the orchestrator.

---

## Cancellation and Failure Independence

Recording and alert evaluation are consecutive durability boundaries.

- cancellation before or during an uncommitted recording retains existing
  recording cancellation semantics
- once History commits, cancellation during History lookup or alert evaluation
  must not roll back or falsely cancel the recording
- an alert repository/settings/query failure must not escape the Application
  boundary or change the successful recording status
- the composite result must distinguish alert `Cancelled` from alert `Failed`
- an unexpected exception at the Avalonia async-command boundary must remain
  controlled

A failed/cancelled alert outcome remains independently retryable by the
Application contract and deterministic event-key persistence. This prompt does
not add a dedicated retry button or reinterpret an `AlreadyCurrent` re-record
as a new alert event; Alert Centre retry presentation belongs to 039f unless a
later agreed prompt explicitly changes that boundary.

---

## Single Record Observation Integration

Update `CruiseHistoryViewModel` to use the composite orchestrator instead of
calling `RecordCruiseObservation` directly.

Preserve:

- captured-observation generation and stale-result protection
- Can Record and Cancel Recording behavior
- all existing recording statuses and History refresh/selection behavior
- existing factual recording messages
- use of the injected Avalonia `IClock` for the explicit alert creation time

Extend controlled feedback without introducing the Alert Centre:

- zero created alerts: retain the existing factual message
- one created alert: append a concise statement that one alert was created
- multiple created alerts: append the exact count
- alert evaluation cancelled after commit: state that the observation was
  recorded but alerts were not evaluated because evaluation was cancelled
- alert evaluation failed after commit: state that the observation was
  recorded but alerts could not be evaluated locally

Do not claim live monitoring, automatic browsing or notification delivery.
Do not expose alert detail/filter/lifecycle UI in this step.

---

## Batch Record Observation Integration

Update the existing sequential batch loop to invoke the same composite use case
once for each attempted observation.

Preserve:

- the stable ordered candidate snapshot
- selected/all behavior
- sequential execution
- one independent durable observation per candidate
- per-row First, Changed, Already Current, Cancelled and Failed status
- stopping cleanly on recording cancellation
- retryability of failed, cancelled and unprocessed observations
- successful-candidate locking in the current review
- exactly one History refresh after useful recording outcomes
- preferred affected-History selection
- stale-generation suppression

Extend the candidate/result presentation with the smallest state required to
report alert evaluation independently. Do not replace the factual per-row
recording status with an alert status.

Required batch feedback:

- retain each row's existing recording outcome
- include its created-alert count when non-zero
- distinguish a committed recording followed by alert cancellation/failure
- aggregate total newly created alerts in the completion summary
- aggregate alert-evaluation cancellation/failure counts separately from
  factual recording failures
- `AlreadyCurrent` contributes no alert evaluation and no created alerts

If alert evaluation is cancelled after a row has committed, retain that row's
successful recording outcome and stop further work when the shared batch token
is cancelled. Do not fabricate failures for unprocessed rows.

Do not refresh Cruise History once per alert or once per row. Preserve the one
controlled post-batch refresh.

---

## Dependency Composition

Register the new Application orchestrator through `AddApplication()` using the
established lifetime conventions.

Pass it through existing ViewModel composition in place of the direct recorder.
Avoid manual service location and avoid adding repository dependencies to
ViewModels.

Update composition tests so the complete desktop service provider resolves the
new graph with SQLite alert persistence.

---

## Required Tests

### Application Orchestrator

Add focused tests proving:

- cancelled recording does not query History or evaluate alerts
- failed recording does not query History or evaluate alerts
- first observation does not query/evaluate a change
- `AlreadyCurrent` does not query/evaluate a change
- changed current observation loads the exact sailing/source History
- the immediately previous and current committed observations are passed to
  the evaluator in deterministic order
- a retained older non-current observation creates no alert candidate
- created-alert count and deduplicated existing count are preserved
- alert cancellation preserves successful recording
- alert failure preserves successful recording
- missing/malformed/unloadable committed History becomes only alert failure
- caller-supplied creation time is passed unchanged
- pre-cancellation and cancellation between commit and evaluation are honored
- unexpected dependency exceptions remain controlled

Use focused fakes; do not replace the 039b pure detector or 039c SQLite tests.

### Single Recording ViewModel

Verify:

- existing First, Changed, Already Current, Cancelled and Failed behavior
  remains intact
- zero, one and multiple created-alert messages are deterministic
- post-commit alert cancellation/failure messages still report recording
  success
- History refresh and preferred selection remain unchanged
- cancel and stale-result generations ignore late composite outcomes
- the Avalonia clock value is supplied as alert creation time

### Batch Recording ViewModel

Verify:

- the composite use case is called exactly once per attempted candidate and
  remains sequential
- per-row factual status remains independent from alert outcome
- created alerts are counted per row and in the aggregate summary
- alert cancellation/failure counts do not become recording-failure counts
- `AlreadyCurrent` produces no alert evaluation/count
- shared cancellation after a committed row preserves that row and leaves
  unprocessed rows retryable
- one History refresh still occurs after useful outcomes
- partial completion, selection and stale-generation behavior remain intact

### Composition and Regression

Verify:

- `AddApplication()` resolves the new orchestrator
- desktop composition supplies it to single and batch workflows
- existing History, alert repository and capture selection tests continue to
  pass

---

## Allowed Changes

Production changes should remain focused inside:

```text
KrytenAssist.Application/Cruises/
KrytenAssist.Application/DependencyInjection.cs
KrytenAssist.Avalonia/ViewModels/
```

Minimal desktop composition changes are permitted where constructor wiring
requires them.

Tests may be created or updated under:

```text
KrytenAssist.Avalonia.Tests/Application/Cruises/
KrytenAssist.Avalonia.Tests/ViewModels/
KrytenAssist.Avalonia.Tests/DependencyInjection/
```

Update after implementation:

```text
docs/Codex Prompts/039d - Observation Recording Integration.md
docs/AI Playbook/039 - Price Drop Alerts.md
docs/Roadmap.md
```

Do not modify Core, Infrastructure schema/migrations, API, capture adapters,
browser scripts or XAML unless a focused failing test demonstrates that the
smallest in-scope correction requires it. Document and test any such exception.

Do not stage, commit, push, discard or overwrite unrelated work.

---

## Exclusions

- saved-criteria evaluation and transition state integration (039e)
- Alert Centre and alert settings presentation (039f)
- final cross-boundary verification and manual acceptance (039g)
- alert lifecycle mutations from the recording UI
- external or OS notifications
- background monitoring or acquisition
- History/alert cross-aggregate transaction or foreign keys
- schema or migration redesign
- new retailer, parser, capture or price interpretation behavior
- Prompt 040 implementation

---

## Verification

Run focused Application, ViewModel and composition tests, then:

```text
dotnet build KrytenAssist.sln --no-restore
dotnet test KrytenAssist.sln --no-build --no-restore
git diff --check
```

Use the established single-worker runner only where existing SQLite contention
requires it. Do not hide or suppress warnings introduced by this change.

---

## Results

Implemented on 18 July 2026.

### Status

Complete.

### Implementation

- Added a focused Application record-then-evaluate orchestrator around the
  existing factual recorder, committed History query and recorded-observation
  alert evaluator.
- First observations, failed/cancelled recordings and `AlreadyCurrent`
  evidence do not trigger change evaluation.
- Changed observations are evaluated only when the supplied evidence is the
  deterministic committed current observation. Valid older historical inserts
  create no false alert.
- History lookup and alert cancellation/failure remain separate from the
  successful factual recording result.
- Single and sequential batch recording now use the same composite use case and
  caller-supplied Avalonia clock value.
- Single feedback reports created-alert counts and controlled post-commit alert
  failures. Batch rows preserve factual status while reporting alert outcomes,
  and the completion summary counts created, failed and cancelled evaluations
  separately.
- Existing capture selection, retryability, stale-generation handling and one
  post-batch History refresh remain intact.

### Verification

- `dotnet build KrytenAssist.sln --no-restore --verbosity minimal -m:1`:
  passed with 0 errors.
- `dotnet test KrytenAssist.sln --no-build --no-restore --verbosity minimal
  -m:1`: 643 passed, 0 failed, 0 skipped.
- Test totals: Core 139, Avalonia 495, API 9.
- `git diff --check`: passed.
- Existing SQLitePCLRaw vulnerability advisories and unused command-event
  warnings remain unchanged.

### Lessons Learned

- A changed repository outcome does not necessarily mean the supplied evidence
  became the latest domain observation: an older distinct capture may be
  retained historically. Re-reading committed same-source History and checking
  timestamp plus canonical fingerprint prevents false alerts.
- Alert state belongs beside, not inside, the factual recording result. This
  lets the UI report a durable recording honestly when later alert work fails.
- Batch presentation needs separate alert counters rather than another
  recording status; otherwise post-commit alert failure would incorrectly make
  a successful row retryable as a factual recording.
