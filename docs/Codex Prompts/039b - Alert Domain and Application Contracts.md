# Codex Prompt 039b – Alert Domain and Application Contracts

## Implementation Prompt

Implement **Step 2 only** from:

```text
docs/AI Playbook/039 - Price Drop Alerts.md
```

Prompt 039a is complete. This step introduces the provider-independent alert
domain, pure detection policies and Application contracts. Do not implement
SQLite persistence, recording integration or Avalonia presentation yet.

---

## Required Reading

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/037 - Cruise History and Price Tracking.md`
5. `docs/AI Playbook/038 - Saved Cruises and Preferences.md`
6. `docs/AI Playbook/039 - Price Drop Alerts.md`
7. `docs/Codex Prompts/039a - Price Drop Alert Experience and Contract.md`
8. existing Cruise observation, fingerprint, History analyzer, saved-sailing,
   preference and result models/tests
9. existing Application repository/use-case/result conventions

---

## Required Core Model

Names may be refined for consistency, but preserve these responsibilities and
boundaries.

### Alert Aggregate

Introduce an immutable `CruiseAlert` containing:

- stable alert id
- deterministic event key
- `CruiseAlertType`
- `CruiseAlertStatus`
- `CruiseSailingKey`
- optional retail `CruiseSource` for source-specific alerts
- bounded provider-independent detail payload
- event/evidence time
- local alert-created time

Types are exactly:

```text
PriceDrop | Promotion | SavedCriteria
```

Lifecycle is exactly:

```text
Unread | Read | Dismissed
```

New alerts start Unread. Read/unread/dismiss mutations change only lifecycle.
There is no Alert-to-History or Alert-to-SavedCruise object ownership.

Use an explicit stable identifier value or `Guid`; do not use process-random
hash codes. The deterministic event key, not the display id, is the
deduplication identity.

### Typed Detail Payloads

Avoid one nullable property bag whose fields can form invalid combinations.
Use a closed provider-independent detail hierarchy or equally strict
discriminated model.

#### Price Drop details

- previous `CruisePrice`
- current `CruisePrice`
- positive absolute reduction
- percentage reduction in the inclusive range 0–100
- triggering observation fingerprint/evidence key

Previous/current currency and normalized basis must be comparable. Reduction
must equal previous minus current and percentage must be calculated from the
previous amount. A lower price from zero is impossible because prices are
non-negative.

#### Promotion details

- optional previous bounded promotion summary
- current non-empty bounded promotion summary
- triggering observation fingerprint/evidence key

Reuse the existing promotion/source text limit or introduce an explicit
matching Core-owned limit. Normalize for comparison using existing Cruise
History whitespace/case rules while retaining safe display text.

#### Saved Criteria details

- whether departure month was configured and matched
- optional configured `CruiseBudget`
- optional matched `CruisePrice`
- criteria fingerprint
- evidence origin: `RecordedObservation` or `SavedSnapshot`
- deterministic evidence key and evidence time
- whether cabin preferences existed but were not evaluable

The detail must never claim cabin matching. If the only configured preference
is cabin type, there are zero evaluable criteria and no candidate.

### Alert Settings

Introduce immutable `CruiseAlertSettings`:

- Price Drop enabled, default true
- Promotion enabled, default true
- Saved Criteria enabled, default true
- minimum Price Drop percentage, default 0

Percentage is decimal from 0 through 100 inclusive. Settings have deterministic
value equality and a stable canonical fingerprint/version used by evaluation.
Settings changes affect future evidence only and are not an instruction to
backfill old price/promotion alerts.

### Event Key

Introduce a bounded deterministic `CruiseAlertEventKey` or factory that produces
a stable canonical key, preferably a versioned lowercase SHA-256 hexadecimal
value rather than storing unbounded concatenated evidence.

Canonical input contains:

```text
schema version
alert type
sailing key
retail source id or explicit no-source marker
triggering evidence key
criteria fingerprint for SavedCriteria only
```

- use ordinal normalized components and invariant date/number formatting
- do not use `GetHashCode`, current culture, alert-created time or retailer
  display name
- same input must produce the same key across process restart
- different type/source/evidence/criteria must produce a different key

### Detection Candidates

Pure detection should return validated `CruiseAlertCandidate` values rather
than pretending an alert was persisted. A candidate contains the event identity,
sailing/source, typed detail, event time and type. Application orchestration
adds alert id, created time and Unread lifecycle when persisting.

---

## Pure Detection Policies

Detection belongs in Core and performs no I/O, clock lookup or repository work.

### Observation Change Detector

Input:

- immediately previous same-source `CruiseObservation`
- newly recorded current observation
- confirmed `CruiseAlertSettings`

Validate that both observations have the same `CruiseSailingKey` and normalized
retail-source identity. Reject mixed histories rather than comparing them.

#### Price Drop

- use the existing `CruisePriceHistoryAnalyzer.SelectComparablePrice` contract
  so Prompt 039 does not invent a second History comparison model
- require one comparable previous and current price
- require current amount lower than previous amount
- compute percentage as `(previous - current) / previous * 100`
- create a candidate when percentage is greater than or equal to the configured
  minimum
- settings-disabled, unchanged, higher, incomparable and below-threshold cases
  return no Price Drop candidate

The percentage is stored at a deliberate precision agreed for persistence and
display, for example four decimal places using `MidpointRounding.AwayFromZero`.
Define and test the exact rule; never use floating point.

#### Promotion

- require Promotion alerts enabled
- normalize prior/current summaries for comparison
- current must be non-empty
- no candidate when prior and current normalize equally
- prior null plus current value creates a candidate only because a previous
  observation exists
- current null (promotion disappearance) creates no candidate

A first observation is not passed to the change detector and creates neither a
Price Drop nor Promotion candidate.

One observation change may create both candidate types.

### Saved Criteria Detector

Input:

- one Shortlisted `SavedCruise`
- confirmed `CruisePreferences`
- confirmed `CruiseAlertSettings`
- selected current price evidence, possibly absent
- previous persisted criteria evaluation state, possibly absent

The Application layer selects evidence and supplies one strict
provider-independent value containing:

- origin (`RecordedObservation` or `SavedSnapshot`)
- evidence key/time
- all displayed prices available from that origin

Detection rules:

- settings must enable Saved Criteria
- saved lifecycle must be Shortlisted
- departure-month criterion exists only when preferred months are non-empty
- budget criterion exists only when MaximumBudget is set
- cabin preferences are recorded as unavailable context and are not evaluable
- at least one month/budget criterion must be configured
- every configured evaluable criterion must match
- month matches `SavedCruise.SailingKey.DepartureDate.Month`
- budget requires exactly one distinct price matching budget currency and
  recognized basis, with amount less than or equal to the maximum
- recognized normalized bases are exactly `per person` for `PerPerson`, and
  `total` or `total booking` for `TotalBooking`
- ambiguous/missing price, currency mismatch or unrecognized basis means budget
  not known/met

Prefer the latest recorded observation selected deterministically across source
histories. When no recorded History exists, use only the bounded saved snapshot
and mark the origin `SavedSnapshot`; do not call it recorded evidence.

Create a candidate only when the current result is met and the prior state for
the same criteria fingerprint/evidence was absent, unknown or not met. Repeating
the same met evaluation creates no candidate. A new confirmed preference
fingerprint may create a new transition candidate. Detection returns the new
evaluation state even when it creates no candidate so Application can persist
the transition.

### Criteria Fingerprint

Build a stable fingerprint from only evaluable inputs:

- ordered preferred months
- optional budget amount/currency/basis
- alert settings' Saved Criteria enabled state/version

Cabins may be included as an explicit unavailable-context component so a later
Prompt 040 extension versions safely, but must not change a current met result
or imply matching. Use invariant canonical formatting and a version prefix.

---

## Required Application Contracts

Application owns every persistence abstraction. No repository exposes EF Core,
SQLite, Avalonia, browser or provider SDK types.

### Alert Repository

Define focused asynchronous operations supporting:

- get alert by id
- list alerts deterministically with lifecycle/type filters
- count unread alerts
- add candidate/materialized alert if event key is absent
- change lifecycle to Unread, Read or Dismissed

The add operation must distinguish Created from AlreadyExists and return the
confirmed aggregate. Event-key uniqueness is authoritative. List ordering is
newest event time first, then created time and stable id/event key.

Do not add permanent delete in Prompt 039.

### Settings Repository

Support get and save of one alert settings profile. Missing persistence maps to
the Core defaults. Save replaces the complete settings atomically.

### Criteria State Repository

Persist/query the latest `SavedCruiseCriteriaEvaluationState` by sailing key and
criteria fingerprint (or an equally deterministic key). It contains only the
minimum transition/deduplication state:

- sailing identity
- criteria fingerprint
- last evidence key/time
- result such as Unknown/NotMet/Met

It is alert evaluation state, not a SavedCruise field and not History evidence.

### Application Use Cases

Define focused use cases/results for:

- list/get/count alerts
- mark read, mark unread and dismiss alert
- get/save alert settings
- evaluate newly recorded observation change candidates
- evaluate one/all supported saved criteria transitions
- materialize candidates with caller-supplied current timestamp

Use cases contain repository exceptions and distinguish useful statuses:

```text
Success | Found | NotFound | Created | AlreadyExists
Updated | Unchanged | Cancelled | Failed
```

Evaluation result reports:

- candidate count
- created alerts
- existing/deduplicated alerts
- updated criteria state where applicable
- Cancelled/Failed separately

Unexpected repository exceptions do not escape Application boundaries.

### Record-Then-Evaluate Contract

Define—but do not yet wire into Avalonia—the composite result required by 039d.
It preserves the existing `CruiseObservationRecordResult` and separately reports
alert evaluation:

```text
record cancelled/failed -> no alert evaluation
record AlreadyCurrent -> no new change evaluation
record committed + alert success -> recording result plus created alerts
record committed + alert cancelled/failed -> recording remains successful,
                                             alert outcome is retryable
```

Do not change `RecordCruiseObservation` to return false failure after its
repository has committed. Prefer a focused orchestration use case around the
existing recorder and evaluator rather than coupling repositories or creating
one cross-aggregate transaction.

The orchestration contract accepts an explicit evaluated/created timestamp,
consistent with `SavedCruiseSnapshotFactory`; do not introduce an Avalonia clock
into Core/Application.

---

## Required Tests

Add comprehensive deterministic tests covering:

### Domain validation and identity

- all alert type/lifecycle enum validation
- typed detail invariants and bounded promotion text
- price delta and percentage consistency
- default/settings equality, inclusive 0/100 and invalid percentage rejection
- stable event/criteria fingerprints across equivalent normalized input
- event keys differ by type, source, evidence and criteria
- alert lifecycle mutation preserves every other field

### Observation detection

- first observation produces no change candidates
- exact lower price at/above threshold creates Price Drop
- equality below threshold, unchanged, higher and incomparable create none
- decimal percentage precision/rounding including boundary values
- cross-sailing/source input is rejected
- new promotion, changed normalized promotion and case/whitespace equivalence
- disappearance produces none
- one change may produce both Price Drop and Promotion candidates
- disabled types produce none

### Saved criteria detection

- month-only, budget-only and combined criteria
- all configured evaluable criteria must match
- shortlist required; Dismissed excluded
- exact budget boundary passes
- currency, basis, amount, ambiguity and missing evidence failures
- exact recognized per-person/total aliases only
- saved snapshot versus recorded evidence origin remains explicit
- latest recorded evidence selection is deterministic
- cabin-only preferences create no candidate
- cabins alongside evaluable criteria are marked unavailable without blocking or
  falsely matching
- first met and not-met-to-met transition create; repeated met does not
- new criteria fingerprint can create a new transition

### Application contracts

- add Created/AlreadyExists semantics
- deterministic list/filter/unread count
- lifecycle NotFound/Updated/Unchanged paths
- default and changed settings
- candidate materialization uses explicit supplied timestamp
- criteria state persists after evaluation even with no alert
- cancellation and every repository exception return controlled outcomes
- evaluation never calls observation-record mutation
- composite record result preserves successful recording when alert evaluation
  is Cancelled/Failed

Use in-memory fakes. Do not add SQLite, migration, Avalonia, browser, TUI or
network tests in 039b.

---

## Allowed Changes

```text
KrytenAssist.Core/Cruises/*Alert*.cs
KrytenAssist.Core.Tests/Cruises/*Alert*.cs
KrytenAssist.Application/Abstractions/Persistence/*Alert*.cs
KrytenAssist.Application/Cruises/*Alert*.cs
KrytenAssist.Application/DependencyInjection.cs
KrytenAssist.Avalonia.Tests/Application/Cruises/*Alert*.cs
docs/Codex Prompts/039b - Alert Domain and Application Contracts.md
docs/AI Playbook/039 - Price Drop Alerts.md
docs/Roadmap.md
```

Existing History, Saved Cruise and preference contracts may change only for a
concrete defect blocking the agreed alert model. Document and test any such
correction. Do not stage, commit, push, discard or overwrite unrelated work.

---

## Exclusions

- SQLite entities, repositories, migration or schema (039c)
- wiring single/batch Record Observation presentation (039d)
- production saved-criteria trigger integration (039e)
- Alert Centre, settings editor or Avalonia changes (039f)
- scheduling, background browser, network or external notifications
- cross-retailer comparisons or currency conversion
- cabin matching or Prompt 040 implementation

---

## Verification

Run focused Core/Application alert tests, then:

```text
dotnet build KrytenAssist.sln --no-restore
dotnet test KrytenAssist.sln --no-build --no-restore
```

Use the established single-worker runner where required. All tests remain
deterministic and offline.

---

## Results

Implemented on 18 July 2026.

### Status

Complete.

### Implementation

- Added immutable Price Drop, Promotion and Saved Criteria alert aggregates,
  typed details, lifecycle, settings and versioned SHA-256 event/criteria keys.
- Added pure same-source observation and supported saved-criteria detectors,
  including explicit Recorded Observation/Saved Snapshot evidence selection.
- Added Application-owned alert, settings and criteria-state repositories plus
  controlled query, mutation, materialisation and evaluation use cases.
- Added the record-then-evaluate composite result without wiring recording or
  changing committed Cruise History semantics.
- Registered the repository-independent detection/evidence services. Use cases
  requiring repositories remain deliberately unregistered until 039c supplies
  their SQLite implementations, preserving complete service-provider startup.

### Verification

- `dotnet build KrytenAssist.sln --no-restore`: passed with 0 errors.
- `dotnet test KrytenAssist.sln --no-build --no-restore`: 626 passed, 0 failed,
  0 skipped.
- Existing SQLitePCLRaw advisory and unused-command-event warnings remain
  unchanged.
