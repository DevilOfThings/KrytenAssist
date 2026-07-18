# Prompt 039 – Price Drop Alerts

## Goal

Turn newly recorded Cruise evidence into durable, explainable in-app alerts for
meaningful price reductions, new promotions and saved cruises that newly meet
the subset of Robin's explicit criteria that current data can evaluate.

Prompt 039 must not imply unattended monitoring. Kryten can evaluate only the
evidence Robin explicitly captures and records until a later roadmap step adds
a supported background acquisition mechanism.

---

## Why This Prompt Exists

Prompt 037 records factual source-specific price History. Prompt 038 stores
Robin's saved sailings, evaluations and guidance preferences. Robin can inspect
both manually, but Kryten does not yet retain notable changes or surface them
as an actionable inbox.

Prompt 039 adds that decision layer without rewriting History, manufacturing
observations or coupling personal alerts to one provider.

---

## Proposed User Experience

### Detection Boundary

Alerts are evaluated after an explicit successful **Record Observation** action
for single or batch capture.

- no scheduled browsing, scraping or background capture
- no alert from preview/capture alone
- re-recording already-current evidence creates no duplicate alert
- recording remains successful even if later alert evaluation fails; the UI
  reports the alert failure separately

### Alert Types

#### Price Drop

Create an alert when the newest comparable price is lower than the immediately
previous comparable price in the same sailing and retail-source History.

The alert records:

- previous and current price
- absolute reduction
- percentage reduction
- source and observation time
- the triggering factual evidence identity

Do not compare different currencies, price bases, retailers or ambiguous price
sets. Existing History remains the source of truth.

Price-drop sensitivity is explicit:

- optional minimum percentage from 0 through 100
- zero means every exact comparable reduction is meaningful
- a higher value suppresses smaller reductions
- settings changes affect future evaluations only; do not silently rewrite or
  backfill prior alerts

The initial settings profile enables all three alert types and uses a zero
minimum percentage, making every exact comparable reduction eligible until
Robin deliberately changes it.

#### New Promotion

Create an alert when a sailing with prior same-source evidence gains a non-empty
promotion or its normalized promotion text changes.

- the first observation alone does not claim a newly appeared promotion
- promotion disappearance is retained as History but is not an alert
- a later promotion after no promotion, or a materially changed promotion,
  may alert
- alerts quote only the bounded promotion summary already in the observation

#### Saved Cruise Criteria Met

For a Shortlisted saved sailing, evaluate only criteria supported by existing
provider-independent data:

- departure month, when one or more preferred months are set
- maximum budget, when set and when one unambiguous observed price matches its
  currency and per-person/total-booking basis

Create an alert only on a transition from not-known/not-met to met, including
the first supported evaluation after the sailing becomes saved. All explicitly
set evaluable criteria must match.

Run this criteria evaluation only after explicit actions that can change its
answer: successful Record Observation, Save Cruise/refresh, Restore to
Shortlist, or Save Preferences. Saving preferences may evaluate all currently
Shortlisted sailings against the newly confirmed criteria fingerprint.

Cabin preferences are not evaluable because Prompt 037 observations contain no
cabin availability/category evidence. Do not treat missing cabin data as a
match. Prompt 040 owns cabin availability and may extend criteria evaluation.

Dismissed/Not for us sailings do not create criteria alerts. Favourite and
rating state may be displayed as context but do not change detection rules.

### In-App Alert Centre

Add an Alerts mode inside the Cruise experience with:

- unread count
- newest-first alert list
- filters for All, Price drops, Promotions and Saved criteria
- selected alert evidence/detail
- Mark read/unread
- Dismiss alert
- alert settings for enabled types and minimum price-drop percentage

Alerts are local and durable across restart. Dismissal changes alert lifecycle
only; it never changes History, Saved Cruises or preferences.

Show a controlled in-workspace notification after recording creates alerts.
Do not add email, SMS, push, operating-system notifications or sounds.

---

## Identity and Deduplication

Each alert has its own stable identity plus a deterministic event key built
from:

```text
Alert type + sailing identity + retail source + triggering evidence identity
```

Criteria alerts additionally include the applicable preference/settings
version or deterministic criteria fingerprint.

- one factual event produces at most one alert of each type
- retry after partial failure converges without duplicates
- source-specific price/promotion alerts remain source-specific
- alert identity never becomes Cruise History or Saved Cruise identity

Alert lifecycle is separate:

```text
Unread | Read | Dismissed
```

Dismissed alerts remain locally retained behind an explicit filter; they are
not evidence deletion. For Prompt 039, retain them without automatic expiry. Manual alert
deletion and retention pruning are excluded unless later evidence shows a
concrete storage need.

---

## Architecture Principles

- Core owns alert event, type, lifecycle, settings and deterministic detection
  policy models.
- Application owns evaluation orchestration, repositories, queries, lifecycle
  mutations and controlled outcomes.
- Infrastructure persists alerts, settings and any required criteria state in
  SQLite without foreign keys/cascades to History or personal-state tables.
- Avalonia owns Alert Centre filters, commands, settings drafts and feedback.
- detection consumes provider-independent observations, History summaries,
  saved aggregates and preference values only
- provider/browser SDK types do not enter alert contracts
- observation persistence completes independently before alert evaluation
- alert failure never rolls back or falsely fails a recorded observation
- detection is deterministic and offline

---

## Scope

### In Scope

- deterministic price-drop detection from newly recorded comparable evidence
- new/changed promotion detection
- supported saved month/budget criteria transition detection
- explicit alert settings and percentage threshold
- durable alert inbox, unread/read/dismissed lifecycle and restart persistence
- single and batch Record Observation integration
- controlled partial-failure and retry/deduplication behaviour
- offline tests and manual desktop verification

### Out of Scope

- unattended browsing, scheduling or background monitoring
- email, SMS, push or OS desktop notifications
- cross-retailer price comparisons
- currency conversion or passenger-count assumptions
- cabin preference/availability matching before Prompt 040
- recommendations, booking advice or automatic dismissal/saving
- editing or deleting provider observations
- new retailer, parser, capture or price-basis behaviour
- Prompt 040 or later implementation

---

## Implementation Steps

### Step 1 – 039a: Alert Experience and Contract

- agree explicit-recording trigger and honest monitoring language
- define price, promotion and saved-criteria rules
- define settings, threshold, lifecycle, retention and deduplication semantics
- define failure independence from Record Observation
- enumerate deterministic acceptance scenarios
- audit current price basis and preference-data limitations

No production implementation belongs to this step.

### Step 2 – 039b: Alert Domain and Application Contracts

- add provider-independent alert/settings/lifecycle models
- add deterministic price, promotion and criteria evaluators
- add Application-owned repositories, queries and mutation results
- add record-then-evaluate orchestration contracts without changing factual
  recording semantics
- add comprehensive Core/Application tests

The agreed analysis uses an immutable alert aggregate with typed Price Drop,
Promotion and Saved Criteria details, separate Unread/Read/Dismissed lifecycle,
default-enabled settings and versioned deterministic event keys. Pure Core
detectors compare the latest same-source observations and supported saved
month/budget criteria. Application owns alert/settings/criteria-state
repositories, controlled queries/mutations/evaluation results and a composite
record-then-evaluate contract that cannot roll back committed History. Saved
criteria evidence explicitly distinguishes latest Recorded Observation from
the bounded Price when saved snapshot; cabin preferences remain unavailable
context until Prompt 040. See the 039b Codex prompt for the complete model,
detection, orchestration and test contract.

### Step 3 – 039c: SQLite Alert Persistence

- add normalized alert, settings and criteria-transition persistence
- enforce deterministic event-key uniqueness
- add read/unread/dismiss lifecycle mutations and deterministic queries
- verify migration, restart, cancellation, concurrency and independence from
  History/Saved Cruises

The agreed analysis uses a common alert header with one owned one-to-one typed
detail table for each Price Drop, Promotion and Saved Criteria payload; JSON and
a nullable detail property bag are excluded. Event-key uniqueness is enforced
by SQLite and concurrent inserts return the existing aggregate. A singleton
settings row and independent sailing/criteria transition state preserve exact
decimal and offset-bearing values. UTC ordering columns make list/state
concurrency deterministic across offsets. There are no relationships or
cascades to History, Saved Cruises or preferences. See the 039c Codex prompt
for the complete schema, repository, migration, concurrency and test contract.

### Step 4 – 039d: Observation Recording Integration

- integrate alert evaluation after successful single and batch recording
- preserve independent recording and alert cancellation/failure outcomes
- prevent AlreadyCurrent evidence from duplicating alerts
- surface created-alert counts and controlled evaluation failures
- preserve all existing capture selection and recording behaviour

The implemented Application orchestrator records first, then reloads the exact
committed same-source History for changed outcomes. It evaluates only when the
supplied observation is the deterministic current observation, so an older
historical insertion cannot create a false alert. First observations and
`AlreadyCurrent` evidence do not invoke change evaluation. Single and
sequential batch workflows preserve factual success while surfacing created
alert counts and controlled post-commit cancellation/failure independently.
See the 039d Codex prompt for the complete orchestration and test results.

### Step 5 – 039e: Saved Criteria Evaluation

- evaluate Shortlisted saved sailings against supported month/budget criteria
- persist transition state and criteria fingerprints
- trigger evaluation at explicit, agreed save/preference/evidence boundaries
- exclude Dismissed items and unavailable/ambiguous basis data honestly
- leave cabin matching deferred to Prompt 040

The implemented orchestration evaluates Saved Criteria after successful Record
Observation, Save Cruise/refresh, Restore to Shortlist and Save Preferences
actions. Primary factual/personal mutations commit independently. Evidence
selection prefers the latest recorded observation across sources and otherwise
uses the bounded saved snapshot explicitly. Preference saves evaluate one
stable, deterministic Shortlisted snapshot sequentially. Observation-change
and criteria outcomes remain independently reportable, and failed alert
materialisation no longer advances Met transition state before a safe retry.
See the 039e Codex prompt for complete results and tests.

### Step 6 – 039f: Alert Centre and Settings

- add in-app Alerts mode, unread badge, filters and selected detail
- add read/unread/dismiss controls
- add enabled-type and minimum-percentage settings editor
- show controlled post-recording alert feedback
- add loading, empty, retry and stale-result states

### Step 7 – 039g: Tests and Verification

- audit evidence, personal-state and alert independence
- verify deduplication, partial failure, restart and concurrency
- verify single/batch recording integration
- verify threshold, promotion and supported criteria edge cases
- run full offline suite/build and manual desktop checklist
- update Results, Lessons Learned, Roadmap and Session Handover
- leave Prompt 040 unstarted

---

## Deterministic Acceptance Scenarios

1. First observation records History and creates no price/promotion change alert.
2. A same-source comparable lower price creates one Price Drop alert.
3. An unchanged, higher, incomparable or below-threshold change creates none.
4. A retry/AlreadyCurrent record never duplicates an existing alert.
5. A newly appearing/changed promotion after prior evidence creates one alert.
6. Promotion disappearance and first-observation promotion create none.
7. Same sailing at another retailer has independent History and alert events.
8. A Shortlisted sailing crossing into its matching currency/basis budget and
   preferred month creates one criteria alert.
9. Dismissed, wrong-month, above-budget, ambiguous-basis and cabin-only cases
   create no criteria alert.
10. Read/unread/dismiss changes only alert state.
11. Removing an alert or saved sailing does not change observations.
12. Removing History does not cascade-delete alerts.
13. Alert evaluation failure leaves successful recording committed and retryable.
14. Alerts/settings survive restart and concurrent evaluation deduplicates.
15. No automated test browses TUI, uses Robin's database or accesses network.

---

## Results

> Complete after implementation and verification.

### Status

In progress. Steps 039a–039e are complete. Robin agreed the explicit-recording trigger,
same-source comparable price and promotion rules, supported month/budget
criteria, cabin deferral, in-app lifecycle, settings defaults, retained
dismissal, deterministic deduplication and record-before-alert failure boundary
on 18 July 2026. Prompt 039b implemented the immutable alert/settings model,
typed evidence payloads, stable event identity, pure detection policies,
Application persistence abstractions and controlled evaluation/lifecycle
contracts. Prompt 039c added the normalized alert header and typed-detail
schema, singleton settings, independent criteria state, exact value round trips,
database-enforced event deduplication, deterministic concurrency and complete
repository/DI wiring. Prompt 039d added shared single/batch record-then-evaluate
orchestration, deterministic committed-current evidence selection and
independent alert feedback without changing factual recording outcomes. Prompt
039e added explicit Record/Save/Restore/Preferences criteria triggers, stable
bulk evaluation, honest recorded-versus-saved evidence and independent primary
mutation outcomes. Prompt 039f – Alert Centre and Settings is next.
