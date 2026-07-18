# Codex Prompt 039e – Saved Criteria Evaluation

## Implementation Prompt

Implement **Step 5 only** from:

```text
docs/AI Playbook/039 - Price Drop Alerts.md
```

Prompts 039a–039d are complete. Alert domain and persistence, the pure Saved
Criteria detector, criteria transition state and recorded-observation
integration already exist.

This step integrates Saved Criteria evaluation with the explicit actions that
can change its answer: successful Record Observation, Save Cruise/refresh,
Restore to Shortlist and Save Preferences.

Do not build the Alert Centre or alert settings presentation yet.

---

## Required Reading

Read these files in order before changing code:

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/037 - Cruise History and Price Tracking.md`
5. `docs/AI Playbook/038 - Saved Cruises and Preferences.md`
6. `docs/AI Playbook/039 - Price Drop Alerts.md`
7. `docs/Codex Prompts/038b - Personal Cruise Domain and Application Contracts.md`
8. `docs/Codex Prompts/038d - Save Actions and Evaluation Editing.md`
9. `docs/Codex Prompts/038e - Saved Cruises Organisation.md`
10. `docs/Codex Prompts/038f - Cruise Preference Profile.md`
11. `docs/Codex Prompts/038g - Tests and Verification.md`
12. `docs/Codex Prompts/039a - Price Drop Alert Experience and Contract.md`
13. `docs/Codex Prompts/039b - Alert Domain and Application Contracts.md`,
    including Results
14. `docs/Codex Prompts/039c - SQLite Alert Persistence.md`, including Results
15. `docs/Codex Prompts/039d - Observation Recording Integration.md`, including
    Results and Lessons Learned
16. existing saved-cruise, preference, History and alert Application use cases,
    result types and dependency-injection registrations
17. `CruiseCriteriaEvidenceSelector`, `EvaluateSavedCruiseCriteriaAlerts` and
    `SavedCruiseCriteriaAlertDetector`
18. `CruiseSaveAndEvaluationViewModel`, `SavedCruisesViewModel`,
    `CruisePreferencesViewModel`, `CruiseHistoryViewModel` and batch recording
    ViewModels/tests

Do not begin implementation until the distinction between a committed primary
action and its later criteria-evaluation outcome is understood.

---

## Goal

Create a durable Saved Criteria alert when a Shortlisted sailing transitions
into all currently evaluable explicit criteria after one of the agreed user
actions.

The single-sailing workflow becomes:

```text
explicit Record / Save / Restore action
        ↓
commit factual or personal primary state
        ↓
resolve the committed Saved Cruise, confirmed preferences and best evidence
        ↓
evaluate supported month/budget criteria
        ↓
persist transition state and materialize a deduplicated alert when entering Met
        ↓
return primary and criteria-alert outcomes independently
```

The Save Preferences workflow becomes:

```text
explicit Save Preferences
        ↓
commit the confirmed preference profile
        ↓
take one stable deterministic snapshot of Shortlisted sailings and History
        ↓
evaluate each sailing sequentially against the same confirmed preferences
        ↓
retain controlled per-sailing and aggregate outcomes
```

The implementation must:

- reuse `SavedCruiseCriteriaAlertDetector`, `CruiseCriteriaEvidenceSelector`
  and `EvaluateSavedCruiseCriteriaAlerts`
- evaluate only after the triggering primary operation succeeds
- keep recording, saving, restoring and preference persistence successful when
  later criteria evaluation is cancelled or fails
- evaluate only Shortlisted saved sailings
- use preferred departure months and one unambiguous matching budget price
  only
- preserve cabin preferences as unavailable context, never a match
- prefer the latest recorded observation across retail sources; use the saved
  snapshot only when no recorded History exists
- preserve exact evidence origin in alert details
- persist criteria transition state even when no alert is created
- create an alert only on a transition from Unknown/NotMet to Met for the
  current criteria fingerprint
- rely on deterministic event keys and SQLite uniqueness for retry/concurrency
  convergence
- evaluate all currently Shortlisted sailings after successful Save
  Preferences using a stable sequential snapshot
- surface created-alert counts and controlled evaluation failures without
  adding the Alert Centre
- remain provider-independent, deterministic and offline

---

## Agreed Trigger Contract

### Successful Record Observation

Evaluate Saved Criteria for the recorded sailing after these successful
factual outcomes:

```text
FirstObservationRecorded
ChangedObservationRecorded
AlreadyCurrent
```

Unlike Price Drop/Promotion detection, Saved Criteria may be evaluated after a
first or already-current recording because the criteria transition may not
previously have been evaluated for the current saved/preference state.

Rules:

- if the sailing is not saved, criteria evaluation is not applicable
- if it is Dismissed, the detector records/retains no Met alert
- failed or cancelled recording triggers no criteria work
- Price Drop/Promotion evaluation from 039d remains independent
- one alert type failing must not erase the controlled outcome of another
- a successful recording remains successful if saved-state, preference,
  History, transition-state or alert persistence later fails

### Successful Save Cruise or Snapshot Refresh

After `SaveCruise` returns:

```text
Created | Updated | Unchanged
```

evaluate the returned Shortlisted aggregate.

This includes saving an unrecorded capture. When no History exists, the bounded
saved snapshot is honest `SavedSnapshot` evidence. Saving must not create a
Cruise observation or call the observation recorder.

An explicit Unchanged save may retry criteria evaluation safely. Event-key
deduplication and transition state prevent duplicates.

### Successful Restore to Shortlist

After `RestoreCruise` returns:

```text
Restored | Unchanged
```

evaluate the returned aggregate only when it is Shortlisted.

Dismiss does not trigger criteria evaluation and never creates a Saved
Criteria alert. Removing personal state does not delete alerts or transition
state.

### Successful Save Preferences

After `SaveCruisePreferences` returns:

```text
Updated | Unchanged
```

evaluate every currently Shortlisted sailing against the exact confirmed
profile returned/accepted by that save action.

An explicit Unchanged save may retry a prior partial evaluation and converges
through persisted state/event keys.

Do not evaluate on:

- draft edits before Save Preferences
- Cancel Changes or Clear Draft
- loading the Preferences view
- changing Saved Cruises filters or selection
- opening an editor
- evaluation/rating/note/favourite changes
- dismiss or remove
- application startup
- capture/preview without Record or Save

Alert-settings-trigger behavior belongs to 039f. Changing settings affects
future agreed evaluations and must not cause a hidden 039e backfill.

---

## Supported Criteria and Honest Limitations

Evaluate only:

- departure month when one or more preferred months are set
- maximum budget when set and exactly one observed price matches currency and
  the accepted per-person or total-booking basis

All explicitly set evaluable criteria must match.

Required behavior:

- no month and no budget means Unknown and no alert
- wrong month means NotMet
- matching month with no budget means Met
- a matching budget requires exact currency and accepted normalized basis
- zero or multiple matching budget prices means NotMet
- no currency conversion, passenger count, division or basis inference
- Dismissed saved cruises are Unknown and never alert
- disabled Saved Criteria settings produce Unknown and no alert
- favourite, rating, interest and notes do not affect detection
- cabin preferences remain recorded as unavailable context only

Do not reinterpret or broaden the pure detector policy unless a focused failing
test proves a concrete 039b defect. Document and test any correction.

---

## Evidence Selection

Reuse `CruiseCriteriaEvidenceSelector`.

For each saved sailing:

1. select only History with the exact `CruiseSailingKey`
2. choose the latest recorded observation across all retail-source histories
   by `ObservedAt`, then canonical fingerprint
3. use that observation's complete price collection as
   `RecordedObservation` evidence
4. when no recorded History exists for the sailing, use the bounded saved
   snapshot price and mark the origin `SavedSnapshot`

Do not:

- merge prices from multiple observations or retailers
- choose the cheapest retailer
- compare retailers
- prefer a newer saved snapshot over any existing recorded History
- call saved snapshot evidence “recorded”
- add the saved snapshot to Cruise History

For single-sailing triggers, loading all local History is acceptable only
through an Application query boundary and only when required for exact
cross-source selection. For Save Preferences bulk evaluation, load one stable
History snapshot and reuse it for every sailing; do not issue one full History
query per sailing.

---

## Required Application Orchestration

Introduce focused Application orchestration around existing mutation and
evaluation use cases. Names may follow established conventions.

### Evaluate One Saved Sailing

Add a reusable coordinator accepting the already committed:

```text
SavedCruise savedCruise
CruisePreferences confirmedPreferences
IReadOnlyList<CruiseRecordedHistory> histories
DateTimeOffset alertCreatedAt
CancellationToken cancellationToken
```

It must:

- select evidence through `CruiseCriteriaEvidenceSelector`
- call `EvaluateSavedCruiseCriteriaAlerts` exactly once
- pass `alertCreatedAt` unchanged
- return its controlled result without throwing dependency details

Keep data loading outside this pure orchestration seam so single and bulk
triggers can reuse stable snapshots efficiently.

### Resolve One Sailing for Evaluation

Add the smallest coordinator required to load:

- the committed saved aggregate when the trigger supplies only a sailing key
- confirmed preferences
- local History

It then delegates to the one-sailing coordinator.

Use existing Application queries/contracts. Do not query repositories directly
from Avalonia and do not introduce Infrastructure types.

Not-saved is a successful NotApplicable/no-evaluation outcome, not an alert
failure. Dismissed may be passed to the detector or short-circuited with an
equivalent controlled result, but must never alert.

### Save Cruise Then Evaluate

Add a composite use case around existing `SaveCruise`.

It returns both:

```text
SavedCruiseMutationResult primary save result
CruiseAlertEvaluationResult? criteria result
```

Rules:

- Cancelled/Failed save: no criteria evaluation
- Created/Updated/Unchanged save: evaluate returned saved aggregate
- criteria cancellation/failure never changes the save status
- snapshot save remains independent from Cruise History persistence

### Restore Then Evaluate

Add a composite use case around existing `RestoreCruise`.

Rules:

- NotFound/Cancelled/Failed restore: no criteria evaluation
- Restored/Unchanged Shortlisted result: evaluate
- criteria cancellation/failure never changes restore success

### Save Preferences Then Evaluate All

Add a composite use case around existing `SaveCruisePreferences`.

After Updated/Unchanged:

1. use the exact successfully saved preferences object as the confirmed profile
2. load Saved Cruises once
3. retain only Shortlisted aggregates
4. order deterministically by departure date, operator id, ship name and
   duration
5. load History once
6. evaluate each stable saved-sailing snapshot sequentially
7. retain one controlled outcome per attempted sailing
8. stop cleanly on cancellation without fabricating failures for unprocessed
   sailings

The bulk result reports at least:

- primary preference mutation result
- eligible/attempted/completed counts
- total candidates
- created alerts
- existing/deduplicated alerts
- failed and cancelled evaluation counts
- unprocessed count after cancellation
- controlled setup/list/History failure separately

Do not place all sailings in one database transaction. A failure for one
sailing must not roll back prior alert/state work or the saved preferences.

---

## Record Observation Composition Refinement

Extend the completed 039d record-then-evaluate orchestration to include Saved
Criteria for the exact recorded sailing.

The current `CruiseRecordAndAlertResult.Alerts` property represents only one
evaluation outcome. That is insufficient once observation-change and criteria
evaluation can succeed/fail independently.

Refine the Application-owned composite result explicitly, for example:

```text
Recording
ObservationAlerts
SavedCriteriaAlerts
```

and provide aggregate convenience properties for created-alert totals where
useful.

Required branches:

- recording Cancelled/Failed: neither alert path is attempted
- First: no observation-change evaluation; criteria evaluation may run
- Changed current: observation-change evaluation and criteria evaluation may
  both run independently
- Changed older historical insertion: no observation-change alert; criteria
  evaluation uses the committed best evidence selected across History
- AlreadyCurrent: no observation-change evaluation; criteria evaluation may
  run
- not saved: criteria result is explicitly not attempted/not applicable
- one alert path Cancelled/Failed: preserve recording and the other alert path's
  outcome

Do not collapse one failure over created alerts from the other path. Preserve
the 039d ViewModel's existing factual message and total created-alert feedback.

If the existing result contract requires correction, update all focused 039b,
039d and composition tests and document the reason. Do not redesign unrelated
alert contracts.

---

## Cancellation, Failure and Retry Semantics

The primary mutation always commits before criteria evaluation.

- primary cancellation/failure performs no criteria work
- cancellation after primary commit leaves that primary result successful
- preference save remains successful after bulk setup or per-sailing failure
- Save Cruise and Restore remain successful after criteria failure
- successful Record Observation remains successful after criteria failure
- repository/query exceptions remain controlled inside Application
- UI messages never expose SQL, paths, exception text or internal ids

Retry rules:

- explicit successful Unchanged Save Cruise/Save Preferences may retry
- explicit AlreadyCurrent Record Observation may retry criteria evaluation
- retry after alert creation but before state persistence converges through the
  deterministic event key, then persists state
- retry after state persistence creates no duplicate alert
- equal/newer criteria state conflict behavior remains owned by 039c

No dedicated Retry Alerts button is added in 039e. Prompt 039f owns Alert Centre
loading/retry presentation.

---

## Avalonia Integration

Keep views passive and reuse existing commands. Do not add a new criteria
editor or Alert Centre.

### Record Observation Feedback

Preserve 039d single/batch factual outcomes and report the combined number of
new Price Drop, Promotion and Saved Criteria alerts.

When one alert path fails after recording:

- retain any created count from the other path
- state concisely that some alerts could not be evaluated
- never change the factual recording status

Batch recording remains sequential and refreshes History exactly once.

### Save Cruise Feedback

Route single capture, Ready candidate and History Save/Edit through the
composite save use case.

Preserve Created/Updated/Already saved text and editor opening. Append:

- one/multiple created-alert count when non-zero
- controlled criteria cancellation/failure after successful save

Saving one candidate remains independent from batch selection/recording.

### Restore Feedback

Route Restore to Shortlist through the composite restore use case.

Preserve organiser selection, filtering and list replacement. Append created
alert count or controlled criteria failure without presenting restore as
failed.

Dismiss behavior remains unchanged and does not evaluate criteria.

### Save Preferences Feedback

Route explicit Save Preferences through the bulk composite use case.

Preserve confirmed baseline/draft behavior. After successful preference save,
show:

- number of Shortlisted sailings evaluated
- number of newly created alerts when non-zero
- controlled partial-failure/cancellation summary

Do not open alerts automatically, reorder Saved Cruises or turn criteria into
filtering/recommendation behavior.

Use injected `IClock.Now` once per explicit action and pass that value unchanged
for every alert created by that action. Do not call the clock separately per
sailing in one bulk evaluation.

Preserve every existing target/mutation generation and stale-result boundary.

---

## Dependency Composition

Register new Application coordinators and composite use cases through
`AddApplication()` with established lifetimes.

Replace direct mutation use-case dependencies in the relevant ViewModels with
the composite use cases. Do not add repository dependencies or service
location to Avalonia.

Update complete desktop composition tests for:

- record + observation alerts + criteria alerts
- save + criteria evaluation
- restore + criteria evaluation
- save preferences + bulk criteria evaluation

---

## Required Tests

### Single-Sailing Criteria Orchestration

Verify:

- latest recorded evidence is preferred across retail sources
- saved snapshot is used only when no recorded History exists
- exact evidence origin/key/time reaches the evaluator
- caller timestamp is passed unchanged
- not-saved and Dismissed cases create no alert
- month-only, budget-only and combined criteria use existing detector policy
- cabin preferences remain unavailable context
- loading/evaluation cancellation and failure are controlled

### Record Observation Integration

Verify:

- First and AlreadyCurrent may evaluate criteria but not observation changes
- Changed current evaluates both paths independently
- older historical insertion creates no observation-change alert and uses
  committed best criteria evidence
- unsaved sailing skips criteria evaluation
- recording cancellation/failure skips both alert paths
- created totals include both paths
- either alert-path failure preserves recording and the other outcome
- single/batch feedback and one History refresh remain correct

### Save and Restore Integration

Verify:

- Created/Updated/Unchanged Save Cruise evaluates returned Shortlisted state
- saving without History uses SavedSnapshot evidence and never records History
- save cancellation/failure performs no criteria work
- criteria failure preserves save success and editor state
- Restored/Unchanged Shortlisted state evaluates
- NotFound/cancelled/failed restore does not evaluate
- dismiss and remove do not evaluate or delete alerts/state
- generation changes ignore late results

### Save Preferences Bulk Evaluation

Verify:

- Updated and Unchanged saves evaluate one stable Shortlisted snapshot
- failed/cancelled preference save performs no listing/evaluation
- Dismissed sailings are excluded before evaluation
- deterministic order is independent of repository return order
- preferences and History are loaded/reused once
- evaluations are sequential
- per-sailing failure does not roll back preferences or prior outcomes
- cancellation preserves completed outcomes and reports unprocessed sailings
- rerun deduplicates alerts and persists transition state
- one clock value is used for the complete action
- draft/confirmed/stale-generation behavior remains intact

### Composition and Cross-Aggregate Independence

Verify:

- all new use cases resolve from complete desktop composition
- evaluation never calls observation record mutation
- Save Cruise evaluation does not add History
- criteria state and alerts remain independent from Saved Cruise deletion
- alert/state failures do not change saved preferences or lifecycle state
- existing 039b pure detector and 039c persistence/concurrency tests remain
  intact

---

## Allowed Changes

Production changes should remain focused inside:

```text
KrytenAssist.Application/Cruises/
KrytenAssist.Application/DependencyInjection.cs
KrytenAssist.Avalonia/ViewModels/
```

Minimal passive XAML changes are permitted only if existing feedback bindings
cannot display the controlled results.

Tests may be created or updated under:

```text
KrytenAssist.Avalonia.Tests/Application/Cruises/
KrytenAssist.Avalonia.Tests/ViewModels/
KrytenAssist.Avalonia.Tests/DependencyInjection/
KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/
```

Update after implementation:

```text
docs/Codex Prompts/039e - Saved Criteria Evaluation.md
docs/AI Playbook/039 - Price Drop Alerts.md
docs/Roadmap.md
```

Core or Infrastructure production changes are permitted only for a focused,
test-proven defect in the completed 039b/039c contracts. Do not add or edit a
migration without a concrete schema defect. Document and test any correction.

Do not stage, commit, push, discard or overwrite unrelated work.

---

## Exclusions

- Alert Centre, unread badge, filters and selected detail (039f)
- alert lifecycle/settings presentation (039f)
- final cross-boundary audit and manual acceptance (039g)
- evaluating criteria on draft edits, startup, load or selection
- alert-settings-triggered backfill
- cabin availability/category matching
- scoring, ranking, recommendations or automatic filtering
- cross-retailer price comparison or cheapest-price selection
- currency conversion, passenger assumptions or price-basis inference
- background monitoring, capture, scheduling or network work
- external or operating-system notifications
- History/Saved Cruise/alert foreign keys or cross-aggregate transactions
- Prompt 040 implementation

---

## Verification

Run focused Application, ViewModel, composition and persistence regression
tests, then:

```text
dotnet build KrytenAssist.sln --no-restore --verbosity minimal -m:1
dotnet test KrytenAssist.sln --no-build --no-restore --verbosity minimal -m:1
git diff --check
```

Use the established single-worker runner where SQLite contention requires it.
Do not hide or suppress warnings introduced by this change.

---

## Results

Implemented on 18 July 2026.

### Status

Complete.

### Implementation

- Added reusable one-sailing Saved Criteria orchestration with exact
  RecordedObservation/SavedSnapshot evidence selection and controlled loading
  of preferences and History.
- Added composite Save Cruise, Restore and Save Preferences use cases that
  preserve committed primary outcomes when later criteria work is cancelled or
  fails.
- Save Preferences evaluates one deterministic Shortlisted snapshot
  sequentially, reuses one History snapshot and reports completed, failed,
  cancelled and unprocessed outcomes.
- Extended Record Observation composition so observation-change and Saved
  Criteria alert results remain independent. First and AlreadyCurrent evidence
  may evaluate criteria; unsaved and Dismissed sailings do not.
- Updated single/batch Record, Save Cruise, Restore and Save Preferences
  ViewModels with combined created-alert counts and controlled post-commit
  feedback while preserving existing generation and refresh behavior.
- Corrected a focused retry defect in the 039b evaluator: a Met transition is
  no longer persisted when alert materialisation fails, allowing a later retry
  to create/deduplicate the alert before committing transition state.
- Registered all orchestration through Application dependency injection. No
  Core, schema, migration, API, browser, XAML or network changes were required.

### Verification

- `dotnet build KrytenAssist.sln --no-restore --verbosity minimal -m:1`:
  passed with 0 errors.
- `dotnet test KrytenAssist.sln --no-build --no-restore --verbosity minimal
  -m:1`: 650 passed, 0 failed, 0 skipped.
- Test totals: Core 139, Avalonia 502, API 9.
- `git diff --check`: passed.
- Existing SQLitePCLRaw vulnerability advisories and unused command-event
  warnings remain unchanged.

### Lessons Learned

- Transition state must not advance to Met after candidate materialisation
  fails; otherwise the persisted state suppresses the exact retry that
  deterministic event keys were intended to support.
- Bulk preference evaluation should accept the already confirmed preference
  object and one stable History snapshot. Reloading either per sailing could
  mix evidence generations inside one explicit action.
- Observation-change and Saved Criteria evaluations need separate result
  properties. Collapsing them into one status would lose created alerts when
  the other path fails and could falsely change factual recording feedback.
