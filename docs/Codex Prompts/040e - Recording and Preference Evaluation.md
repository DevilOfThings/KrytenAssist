# Codex Prompt 040e – Recording and Preference Evaluation

## Implementation Prompt

Implement **Step 040e only** from:

```text
docs/AI Playbook/040 - Cabin Availability.md
```

Prompts 040a–040d are complete. This step connects captured cabin candidates to
an explicit recording action and completes post-commit Cabin Availability and
Saved Criteria evaluation. Preserve recording success when later evaluation or
alert materialization fails. Do not implement the full Cabin Availability
history workspace; Prompt 040f owns that presentation.

---

## Required Reading

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/037 - Cruise History and Price Tracking.md`
5. `docs/AI Playbook/038 - Saved Cruises and Preferences.md`
6. `docs/AI Playbook/039 - Price Drop Alerts.md`
7. `docs/AI Playbook/040 - Cabin Availability.md`
8. `docs/Codex Prompts/040a - Cabin Availability Experience and Evidence Contract.md`
9. `docs/Codex Prompts/040b - Cabin Domain and Application Contracts.md`
10. `docs/Codex Prompts/040c - SQLite Cabin Persistence.md`
11. `docs/Codex Prompts/040d - TUI Cabin Evidence Capture.md`
12. existing price recording/alert orchestration, Saved Criteria orchestration,
    cabin use cases/results, Cruise Discovery capture review, alert coordinator
    and dependency-composition tests

---

## Product Boundary

The explicit journey in this step is:

```text
Capture loaded TUI cards
        ↓
Review cabin evidence independently from price evidence
        ↓
Choose Record Cabin Observation for one ready candidate
        ↓
Commit cabin evidence locally
        ↓
Evaluate Cabin Availability and Saved Criteria from committed evidence
```

Capture and recording remain separate. Existing `Record Observation`, `Record
Selected` and `Record All Observations` actions record price/promotion history
only. They must not start recording cabin evidence. Likewise, recording cabin
evidence must not create or replace a price observation.

Add a small cabin review/action surface to Cruise Discovery sufficient to make
the recording decision honest. Prompt 040f owns the fuller latest/history
presentation and workspace organization.

---

## Post-Commit Application Orchestration

Refine `RecordCruiseCabinObservationAndEvaluateAlerts` into the single
Application-owned coordinator for this workflow. Its public request should
require only the provider-independent `CruiseCabinObservation`, alert creation
time and cancellation token. The caller must not load or pass `SavedCruise`,
preferences, history or alert settings.

The coordinator owns these steps:

1. inspect the compatible series before recording only as needed for transition
   comparison
2. call `RecordCruiseCabinObservation`
3. stop if recording failed or was cancelled
4. reload/use the committed cabin history and verify which observation is the
   current meaningful snapshot
5. when the verified committed current snapshot has an immediately previous
   compatible snapshot, evaluate/materialize that latest transition; this also
   permits an Already Current retry after an earlier materialization failure
6. independently reevaluate Saved Criteria for the matching sailing using the
   committed cabin snapshot
7. return recording plus both controlled evaluation outcomes

Never evaluate from an optimistic pre-commit projection. A concurrent or
out-of-order incoming observation that is not the committed current snapshot
must not produce a false transition alert. Comparison remains within the same
sailing/source/context series.

First observation cannot create a Cabin Availability transition alert. An
Already Current retry may reevaluate the latest genuine committed transition
only when the supplied observation is still the verified current snapshot;
existing event-key deduplication prevents duplicates. Every successful outcome
also forms a Saved Criteria reevaluation boundary: first evidence may resolve a
cabin criterion, and retries may recover from earlier post-record failures.

---

## Cabin Availability Alert Materialization

The existing `EvaluateCruiseCabinAvailabilityAlerts` currently returns pure
candidates. Add or refactor an Application use case analogous to
`EvaluateRecordedCruiseAlerts` that:

- loads current alert settings
- calls `CruiseCabinAvailabilityAlertDetector`
- materializes candidates through `MaterializeCruiseAlertCandidates`
- returns `CruiseAlertEvaluationResult`
- contains cancellation and infrastructure failures as controlled results

Use the committed previous/current pair, currently saved cruise and current
preferences loaded inside Application. Existing Core rules remain authoritative:

- only Shortlisted saved cruises are eligible
- only configured preferred cabin categories are eligible
- only explicit Unavailable → Available and Available → Unavailable transitions
  create Cabin Availability alerts
- first-seen, Unknown → Available, Available → Unknown, coverage-only and
  non-preferred-category changes remain history-only
- the alert setting must be enabled

Materialization uses existing event-key deduplication. Retrying post-record
evaluation may return Already Exists but must not duplicate an alert.

If the sailing is not saved, is Dismissed, preferences have no cabin types, or
settings disable the alert, evaluation succeeds with zero candidates rather
than failing recording.

---

## Saved Criteria Evidence Selection

Extend Saved Criteria orchestration so it can supply real compatible cabin
evidence instead of always passing null.

### Compatibility

A selected cabin observation must:

- have the exact saved sailing identity
- have the same normalized retail source id as the Saved Cruise snapshot
- remain one complete cabin series; never merge category states across search
  contexts, sources or evidence times

There is no configured preferred occupancy context yet. When an explicit cabin
observation triggered evaluation, use that committed observation. At save,
restore, price-record and preference-save boundaries, select the latest
compatible cabin observation deterministically by:

1. evidence instant in UTC descending
2. state fingerprint descending ordinal
3. series key descending ordinal

The selected context fingerprint becomes part of the existing criteria-v2
fingerprint. A different context is a distinct criteria state; never reinterpret
an older context as the current one. When no compatible evidence exists, cabin
criteria remain Unknown exactly as agreed.

### Combined evidence

Continue selecting price evidence from the latest recorded price observation,
falling back to the Saved Cruise snapshot. Combine that price evidence with the
selected cabin observation through `CruiseCriteriaEvidence`; do not require the
two evidence instants to be equal and do not manufacture a price from cabin
capture.

Preferred cabins remain an OR group. Month, budget and cabin groups combine
with AND. An explicitly Available preferred category can make the cabin group
Met. Unknown is never Unavailable. Persist criteria state before/after alert
materialization using the established success rules and composite evidence key.

### Explicit reevaluation boundaries

Ensure compatible cabin evidence participates when Saved Criteria is evaluated
after:

- successful cabin recording
- successful price observation recording
- save/update of a Shortlisted cruise
- restoration to Shortlisted
- successful preference save across Shortlisted cruises

Do not add background reevaluation, startup backfill, timers or evaluation on
unrelated navigation. Migration alone must not generate historic alerts.

Preference-save bulk evaluation must load price histories and cabin histories
once, then select deterministically per sailing; do not issue an avoidable
per-sailing cabin-history query. Preserve existing partial bulk result and
cancellation behavior.

---

## Failure and Result Semantics

Evolve `CruiseCabinRecordAndAlertResult` so callers can distinguish:

- recording outcome
- Cabin Availability evaluation/materialization outcome
- Saved Criteria evaluation/materialization outcome
- total created alert count
- whether either evaluation failed or was cancelled after recording
- whether post-record evaluation is safely retryable

Recording is authoritative once committed:

```text
record succeeds + evaluation fails
    => report recorded, retain evidence, expose retryable evaluation failure

record fails
    => no evaluation and no success claim
```

Attempt the two evaluation families independently after a successful record.
A non-cancellation failure in one must not prevent a best-effort attempt of the
other. Honor cancellation between stages. Never roll back or delete a committed
cabin observation because preference lookup, settings lookup, criteria-state
storage, alert storage or notification failed.

Alert notification is a UI concern after the coordinator returns. Notify the
existing alert coordinator once with the combined number actually created.
Notification failure must not alter recording/evaluation results.

---

## Cruise Discovery Integration

Use the same fixed payload and trusted current-page reference for both existing
price capture and `ICruiseCabinPageCaptureService`. Keep the two capture results
independent:

- price capture success survives cabin capture failure
- cabin capture success survives an incomplete price candidate
- correlate only by exact trusted candidate source reference where a combined
  card is useful
- do not infer cabin evidence from the price observation

Prefer a distinct bounded cabin candidate review collection so ready cabin
evidence is not hidden merely because price extraction was incomplete. Preserve
deterministic page order and the ten-candidate bound.

For every cabin candidate show enough review detail before recording:

- safe sailing label, ship, departure date and duration
- retail source and trusted reference/open-at-TUI action
- explicit search context facts and honest Unknown labels
- `Inside — Available when captured for this search`
- `Outside/Balcony/Suite/Solo — Unknown` for current partial evidence
- `Partial evidence`
- evidence time
- controlled Incomplete/Unsupported/Failed message

Add an explicit per-candidate **Record Cabin Observation** command only for
Ready evidence. While recording, disable duplicate invocation. Afterwards show
First observation, Changed observation or Already current independently from
price recording. If evaluation fails after recording, say the cabin evidence
was recorded and alert/criteria evaluation can be retried; do not show
`Recording failed`.

Cancellation, navigation, recapture and disposal must use generation/cancellation
guards so stale completion cannot replace the current page's review. Do not
automatically record all cabin candidates and do not preselect them for price
batch recording.

Full cabin-history browsing, latest/previous comparison cards and workspace
filters remain Prompt 040f.

---

## Dependency Injection

Register all amended/new Application coordinators through `AddApplication` and
inject the cabin capture/recording seams into Cruise Discovery through existing
composition roots. Preserve scoped repository-dependent services and stateless
singleton Core policies. Avoid service location or manual provider-specific
construction in ViewModels.

Do not change the SQLite schema or add a migration.

---

## Required Offline Tests

### Application orchestration

- first observation commits, creates no Cabin Availability transition, and
  reevaluates Saved Criteria
- changed committed Unavailable → Available preferred cabin materializes one
  Cabin Availability alert for a Shortlisted cruise
- Available → Unavailable materializes the corresponding typed alert
- first-seen/Unknown transitions, non-preferred categories, Dismissed/not-saved
  cruises and disabled settings create none
- Already Current retries the verified latest transition and Saved Criteria
  without duplicating alerts
- out-of-order/non-current incoming evidence creates no false transition alert
- concurrent committed history is compared only through repository-reloaded
  compatible snapshots
- alert candidate materialization deduplicates on retry/restart
- recording remains successful when saved cruise, preference, settings,
  criteria-state or alert repository evaluation fails
- one evaluation failure does not suppress the other unless cancellation stops
  processing
- pre-cancellation records nothing; post-commit cancellation never reports the
  committed evidence as unrecorded
- result created counts and retryable flags include both evaluation families

### Saved Criteria boundaries

- exact sailing and normalized retail source compatibility
- latest compatible cabin observation selection with UTC/fingerprint/series
  tie-breakers
- different search contexts never merge states
- incompatible retailer/sailing evidence leaves cabin criterion Unknown
- explicit cabin record can resolve Unknown → Met using latest price/snapshot
- price record reevaluation includes latest compatible cabin evidence
- save, restore and preference save include cabin evidence
- preferred cabins use OR; month/budget/cabin groups use AND
- missing/Unknown cabin evidence cannot satisfy cabin-only preferences
- bulk preference evaluation loads bounded histories once, is deterministic and
  preserves partial cancellation/failure outcomes
- no startup or migration backfill

### Desktop interaction

- the same payload is offered independently to price and cabin adapters
- cabin failure does not erase price review and vice versa
- ready cabin evidence remains recordable when price candidate is incomplete
- exact-reference correlation only
- explicit command records one cabin candidate; price record commands do not
  record cabin evidence
- command busy/retry/result messages distinguish recording from evaluation
- created alerts notify the coordinator once with combined count
- capture/navigation cancellation and stale-result guards
- read-only browser/script safety remains unchanged
- DI resolves the complete workflow in a fresh scope

All tests use fakes or isolated temporary SQLite. No test browses TUI, invokes
booking or accesses Robin's database.

---

## Manual Acceptance

1. Open a supported modern TUI Cruise Packages result page and capture it.
2. Confirm price review remains unchanged and supported cabin evidence appears
   as a separate explicit review/action.
3. Confirm the displayed sailing, TUI source, occupancy/airport context, Inside
   Available, four Unknown and Partial evidence match the visible card/search.
4. Choose **Record Cabin Observation** for one candidate.
5. Confirm the first record says first evidence and does not claim a cabin
   transition.
6. Repeat the same capture/record and confirm Already current with no duplicate
   observation or alert.
7. Where frozen/manual explicit transition evidence is available, confirm only
   a preferred-category explicit Unavailable ↔ Available change on a
   Shortlisted cruise creates a Cabin Availability alert.
8. Confirm a newly satisfied Saved Criteria combination can create its separate
   alert.
9. Confirm price Record Selected/All does not record cabin evidence.
10. Confirm navigation/cancellation cannot publish stale results and no browser
    automation or background monitoring occurs.

---

## Allowed Changes

```text
KrytenAssist.Application/Cruises/CruiseCabin*.cs
KrytenAssist.Application/Cruises/CruiseCriteriaEvidenceSelector.cs
KrytenAssist.Application/Cruises/SavedCruiseCriteriaOrchestration.cs
KrytenAssist.Application/Cruises/RecordCruiseObservationAndEvaluateAlerts.cs
KrytenAssist.Application/Cruises/CruiseAlertEvaluationUseCases.cs
KrytenAssist.Application/DependencyInjection.cs
KrytenAssist.Avalonia/ViewModels/CruiseBrowserFeasibilityViewModel.cs
KrytenAssist.Avalonia/ViewModels/*Cabin*Review*.cs
KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml
relevant focused Application/ViewModel/DI tests and test fakes
docs/Codex Prompts/040e - Recording and Preference Evaluation.md
docs/AI Playbook/040 - Cabin Availability.md
docs/Roadmap.md
docs/Session Handovers/*
```

Broaden this list only for a directly required existing result/composition
contract and document why. Do not change capture selectors, Core evidence rules,
SQLite schema or unrelated Cruise UI.

---

## Exclusions

- richer TUI cabin-selection capture or new retailer mappings
- category-specific Unavailable extraction not already supplied as explicit
  provider-independent test evidence
- automatic/background recording or monitoring
- recording cabin evidence through price batch commands
- merging cabin states across contexts, retailers or evidence instants
- new preference fields for occupancy/context selection
- database migration or schema changes
- full Cabin Availability latest/history workspace (040f)
- cross-device sync, push/email notifications or booking actions
- live browser/network automated tests

---

## Verification

Run focused cabin orchestration, Saved Criteria, Cruise Discovery and DI tests,
then:

```text
dotnet build KrytenAssist.sln --no-restore
dotnet test KrytenAssist.sln --no-build --no-restore
git diff --check
```

Use `--disable-build-servers` if the local shared build server stalls, and use
the established single-worker runner where required. Resolve warnings introduced
by this step; existing SQLitePCLRaw advisories may remain documented.

---

## Results

Implementation and automated verification completed on 19 July 2026.

### Status

Complete.

### Implementation

- Added post-commit cabin recording orchestration that reloads and verifies the
  committed current series before evaluating its latest transition.
- Materialized typed Cabin Availability candidates through the existing alert
  repository and event-key deduplication path.
- Preserved committed recording results when either Cabin Availability or
  Saved Criteria evaluation fails or is cancelled.
- Extended Saved Criteria evidence selection with deterministic exact-sailing,
  same-retailer cabin observations while keeping each search context intact.
- Included cabin evidence at cabin record, price record, save, restore and
  preference-save evaluation boundaries.
- Added a separate Cruise Discovery cabin review collection and explicit
  per-candidate Record Cabin Observation command; price record commands remain
  independent.
- Added honest context/category/coverage presentation, retry messaging, alert
  coordinator notification and recapture stale-state clearing.
- Added focused orchestration, evidence-selection and review-action tests.

Prompt 040f remains responsible for the complete local Cabin Availability
latest/history workspace.

### Build and Tests

- Single-worker solution build passed with 0 errors.
- Core: 147 passed.
- Avalonia/Application/Infrastructure: 550 passed.
- API: 9 passed.
- Total: 706 passed, 0 failed, 0 skipped.
- Existing SQLitePCLRaw advisory and pre-existing unused-event warnings remain
  unchanged.
