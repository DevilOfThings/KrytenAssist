# Codex Prompt 040b – Cabin Domain and Application Contracts

## Implementation Prompt

Implement **Step 040b only** from:

```text
docs/AI Playbook/040 - Cabin Availability.md
```

Prompt 040a is complete. This step introduces provider-independent cabin
evidence/history, pure transition and preference policies, capture/record/query
contracts and the typed Cabin Availability alert extension. Do not implement
SQLite, TUI extraction, recording presentation or UI yet.

---

## Required Reading

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/036 - Cruise Discovery and Capture.md`
5. `docs/AI Playbook/037 - Cruise History and Price Tracking.md`
6. `docs/AI Playbook/038 - Saved Cruises and Preferences.md`
7. `docs/AI Playbook/039 - Price Drop Alerts.md`
8. `docs/AI Playbook/040 - Cabin Availability.md`
9. `docs/Codex Prompts/040a - Cabin Availability Experience and Evidence Contract.md`
10. existing observation/fingerprint/history, preferences, saved-criteria,
    alert, capture-result and repository/use-case conventions

---

## Core Search Context

Introduce immutable provider-independent context, for example
`CruiseCabinSearchContext`, containing:

- optional adult count
- optional child count
- child ages as either explicitly unknown or a complete ordered set
- package mode
- optional normalized departure-airport identity
- optional cabin quantity

Use a Core-owned mode enum such as:

```text
Unknown | FlyCruise | CruiseOnly | CruiseAndStay
```

Names may follow existing conventions, but unknown must be a real canonical
value rather than null/string magic.

Validation:

- supplied adult/child counts and cabin quantity are non-negative/positive as
  appropriate and safely bounded
- child ages are 0–17
- supplied ages are accepted only when child count is known and their count
  exactly matches it
- zero children has an explicitly known empty age collection
- unknown ages remain distinguishable from known empty ages
- departure airport is bounded and normalized using Core-owned rules
- do not require an airport merely because mode is FlyCruise; unknown context is
  valid evidence and remains an independent series

Context has deterministic value equality and a versioned stable fingerprint
using invariant/ordinal canonical formatting. Unknown markers must be explicit.
Different known values and known-versus-unknown values produce different keys.

---

## Core Cabin Observation

Use the existing `CruiseCabinType` exactly:

```text
Inside | Outside | Balcony | Suite | Solo
```

Introduce:

```text
CruiseCabinAvailabilityState: Unknown | Available | Unavailable
CruiseCabinEvidenceCoverage: Partial | Complete
```

An immutable `CruiseCabinObservation` contains:

- `CruiseSailingKey`
- required retail `CruiseSource`
- `CruiseCabinSearchContext`
- coverage
- exactly one ordered state for every defined `CruiseCabinType`
- evidence/observed time
- bounded deterministic retailer evidence key
- optional bounded trusted source reference as opaque evidence text

Do not put provider names, DOM, URLs as types, browser objects or TUI concepts in
Core. A source reference remains a bounded string; Infrastructure validates
trust before construction.

Observation invariants:

- all cabin types occur exactly once
- all enums are defined
- at least one category is explicitly Available or Unavailable
- Partial has at least one Unknown category
- Complete has no Unknown category
- no quantity, cabin number, deck or price is inferred
- collections are copied and exposed read-only

Provide focused lookup helpers such as state-by-type without exposing mutable
dictionaries.

---

## Deterministic Identity and Meaningful Change

Define two separate stable versioned identities.

### Series Key

Canonical input:

```text
sailing identity
normalized retail source id
search-context fingerprint
```

This is the compatible comparison/repository series. Retail source display name
is excluded. Cross-sailing, cross-source and cross-context evidence is rejected
by comparison policies rather than silently compared.

### State Fingerprint

Canonical input:

```text
series key/version
coverage
ordered cabin-type/state pairs
```

Exclude observed time, source display name, evidence key and source reference.
Equivalent availability with refreshed retailer/package evidence remains the
same meaningful state and should advance last-seen metadata rather than add a
snapshot.

Use lowercase SHA-256 hexadecimal keys or the established equivalent. Never use
`GetHashCode`, current culture or process-random identity.

---

## Pure History and Transition Policy

Introduce a pure analyzer/detector that accepts observations only from one
series and orders deterministically by evidence instant, then fingerprint or
another stable tie-breaker.

Represent per-category changes explicitly, for example:

```text
CruiseCabinAvailabilityChange
CabinType
PreviousState
CurrentState
```

Rules:

- first evidence has no transition
- equal state fingerprint has no meaningful transition
- Available ↔ Unavailable is an explicit transition
- Unknown → known is newly observed knowledge, not became-available/unavailable
- known → Unknown is lost evidence, not proof of inventory change
- coverage change is retained in history when the ordered state set changes
- never collapse Unknown into Unavailable

History summary/application projection should support first observed, last
seen, latest meaningful observation, observation count and latest explicit
category changes without requiring UI wording in Core.

---

## Cabin Availability Alert Extension

Extend `CruiseAlertType` with exactly:

```text
CabinAvailability
```

Add a closed typed `CruiseCabinAvailabilityAlertDetails` payload containing:

- cabin type
- previous explicit state
- current explicit state
- change direction (`BecameAvailable` or `BecameUnavailable`) or an equally
  strict derivation
- context fingerprint
- evidence coverage
- triggering cabin state fingerprint/evidence key
- evidence time

Only Unavailable → Available and Available → Unavailable are valid payloads.
The alert is source-specific and requires a retail source. Update candidate type
validation, status cloning, event-key evidence selection and tests without
weakening existing Price Drop, Promotion or Saved Criteria rules.

Add a default-enabled Cabin Availability setting to `CruiseAlertSettings` and
its stable fingerprint. This is a contract change only; 040c updates persisted
settings/schema. Existing construction call sites must retain default-enabled
behaviour through a compatible optional parameter or focused migration.

Pure alert detection input:

- previous and current compatible cabin observations
- Shortlisted `SavedCruise`, when one exists
- confirmed preferences
- confirmed settings

Create one candidate per preferred cabin with an explicit opposite-state
transition. No candidate for first evidence, Unknown transitions, non-preferred
categories, Dismissed/absent saved cruise, disabled setting or incompatible
series. One observation may produce multiple independently keyed cabin alerts.

The triggering evidence identity must distinguish category transitions from one
observation while remaining deterministic across retries, for example state
fingerprint plus cabin type. Do not use alert-created time.

---

## Saved Criteria Version 2

Extend saved-criteria evaluation with optional compatible cabin evidence.

Prefer a focused composite evidence contract rather than adding more unrelated
nullable arguments indefinitely. It must retain existing price origin/key/time
and optionally one selected cabin observation/state projection.

Cabin group result:

- no preferred cabins: criterion not configured
- any preferred cabin Available: Met
- every preferred cabin explicitly Unavailable: NotMet
- otherwise (no Available and at least one Unknown/no compatible evidence):
  Unknown

Combined result:

- no month/budget/cabin criterion configured: Unknown/ineligible
- any configured criterion explicitly fails: NotMet
- all configured criteria Met: Met
- otherwise: Unknown

Version the criteria fingerprint (for example `criteria:v2`) using ordered
months, budget, ordered preferred cabins, relevant settings version and selected
compatible cabin context fingerprint. Do not reuse `cabins-unavailable` wording.

Update Saved Criteria alert details so they truthfully report:

- configured/matched month state
- configured/matched budget evidence
- configured preferred cabins
- matched preferred cabins
- cabin criterion result
- optional cabin context/evidence identity/time

Retain a compatible reconstruction path until 040c migrates existing alert
persistence. Do not make old persisted Saved Criteria alerts invalid merely
because they predate cabin evidence. No historic re-evaluation/backfill occurs.

Criteria transition state/event evidence must be deterministic when both price
and cabin evidence contribute. Use a versioned composite evidence key and the
latest contributing evidence instant; never concatenate unbounded source text.

---

## Application Persistence Contract

Application owns `ICruiseCabinObservationRepository` (or a consistently named
equivalent). It supports:

- record one observation using series key and state fingerprint
- get one compatible recorded series/history
- list recorded cabin histories deterministically

Repository record result distinguishes:

```text
FirstObservationRecorded
ChangedObservationRecorded
AlreadyCurrent
```

It returns confirmed history/summary metadata needed by orchestration. On
AlreadyCurrent, latest evidence/last-seen metadata advances without adding a
meaningful snapshot. No delete belongs in 040b.

Repository abstractions expose only Core/Application types and asynchronous
operations. No EF Core, SQLite, Avalonia, browser or provider type crosses the
boundary.

---

## Application Capture Contract

Define provider-independent transport-neutral capture request/results for one or
multiple bounded cabin candidates as needed by 040d. They represent:

- sailing/source/context
- coverage and ordered category states
- evidence key/time/reference
- controlled `Ready`, `Incomplete`, `Unsupported`, `Cancelled` and `Failed`
  outcomes
- missing field names without raw DOM/provider exceptions

Do not place TUI selectors/payload DTOs in Application. Capture does not call the
repository and never implies recording.

---

## Application Use Cases and Results

Add focused controlled use cases/results for:

- record one cabin observation
- get one cabin history by complete series identity
- list cabin histories
- evaluate explicit cabin transition alert candidates
- evaluate Saved Criteria with optional cabin evidence
- compose record-then-evaluate without rolling back committed cabin evidence

Use cases accept caller-supplied evaluated/created timestamps where required and
contain repository exceptions. Distinguish useful statuses such as:

```text
Success | Found | NotFound | FirstObservationRecorded
ChangedObservationRecorded | AlreadyCurrent | Cancelled | Failed
```

Record-then-evaluate rules:

```text
record cancelled/failed -> no evaluation
AlreadyCurrent -> no new cabin transition evaluation
first observation -> no Cabin Availability transition alert
changed committed + alert/criteria success -> return both outcomes
changed committed + evaluation cancelled/failed -> recording remains successful,
                                                   evaluation is retryable
```

Do not wire these use cases into existing Cruise capture/record ViewModels in
040b. Register only repository-independent pure services. Repository-dependent
registrations wait until 040c supplies implementations so API validation remains
complete.

---

## Required Tests

### Domain and Identity

- all enum/constructor validation
- every category exactly once; duplicate/missing/null rejected
- Partial/Complete and all-Unknown invariants
- context known/unknown ages, counts, modes, airport and cabin quantity
- stable series/context/state fingerprints across equivalent normalized input
- fingerprints differ by sailing, source, material context, coverage and state
- time/evidence reference changes do not change meaningful state fingerprint
- input collections are defensively copied

### History and Transitions

- first observation, equivalent evidence and changed evidence
- explicit Available/Unavailable changes
- Unknown-to-known and known-to-Unknown are not inventory transitions
- multiple category changes retain deterministic order
- cross-sailing/source/context input is rejected
- equal-time input ordering is deterministic

### Alerts

- new enum/detail/type validation and lifecycle preservation
- event keys differ per cabin category/evidence/source/context
- preferred Unavailable-to-Available and reverse create typed candidates
- first/Unknown/non-preferred/disabled/Dismissed/incompatible cases create none
- multiple preferred transitions create independent candidates
- all existing alert behaviours remain unchanged

### Saved Criteria

- cabin-only Met/NotMet/Unknown
- preferred cabins use OR
- month/budget/cabin groups use AND with explicit failure precedence
- absent/incompatible cabin evidence is Unknown
- version-2 fingerprint stability and meaningful differences
- composite evidence identity/time is stable
- no candidate for repeated Met; future NotMet/Unknown-to-Met follows the
  agreed transition contract without historic backfill
- old Saved Criteria alert reconstruction remains valid

### Application Contracts

- record result mappings and explicit timestamp preservation
- get/list deterministic projections
- cancellation and every repository exception are controlled
- capture and record remain separate
- AlreadyCurrent/first evidence skip cabin transition alerts
- committed changed record remains successful when evaluation fails/cancels
- evaluation never mutates price observations or Saved Cruises

Use hand-written in-memory fakes. All tests remain offline and deterministic.

---

## Allowed Changes

```text
KrytenAssist.Core/Cruises/*Cabin*.cs
KrytenAssist.Core/Cruises/*Alert*.cs
KrytenAssist.Core/Cruises/SavedCruiseCriteriaAlertDetector.cs
KrytenAssist.Core.Tests/Cruises/*Cabin*.cs
KrytenAssist.Core.Tests/Cruises/*Alert*.cs
KrytenAssist.Application/Abstractions/Persistence/*Cabin*.cs
KrytenAssist.Application/Cruises/*Cabin*.cs
KrytenAssist.Application/Cruises/*Alert*.cs
KrytenAssist.Application/DependencyInjection.cs
KrytenAssist.Avalonia.Tests/Application/Cruises/*Cabin*.cs
KrytenAssist.Avalonia.Tests/Application/Cruises/*Alert*.cs
docs/Codex Prompts/040b - Cabin Domain and Application Contracts.md
docs/AI Playbook/040 - Cabin Availability.md
docs/Roadmap.md
```

Existing alert/settings/criteria contracts may change only as required for this
agreed extension. Preserve backward reconstruction of current persisted alerts
until 040c updates the schema/repositories. Do not stage, commit, push, discard
or overwrite unrelated work.

---

## Exclusions

- EF Core entities, SQLite repositories or migration (040c)
- TUI/Avalonia browser script or extraction (040d)
- production recording/preference trigger wiring (040e)
- Cabin Availability UI (040f)
- background browsing, scheduling or external notifications
- cabin quantity/inventory count, cabin numbers, decks or price redesign
- Prompt 041/042 work

---

## Verification

Run focused Core/Application cabin tests, then:

```text
dotnet build KrytenAssist.sln --no-restore
dotnet test KrytenAssist.sln --no-build --no-restore
git diff --check
```

Use the established single-worker runner where required.

---

## Results

Implemented on 19 July 2026.

- added normalized immutable cabin search context, observation, series and state
  identities with explicit Partial/Complete and Unknown/Available/Unavailable
  semantics
- added deterministic cabin history/change analysis that treats only explicit
  Available/Unavailable reversals as inventory transitions
- added the typed Cabin Availability alert, default-enabled setting and pure
  preferred-cabin candidate detector without enabling SQLite materialization
- versioned Saved Criteria evaluation for optional compatible cabin evidence,
  preferred-cabin OR semantics and configured-group AND semantics
- added Application-owned capture, repository, recorded-history, query, record,
  candidate-evaluation and record-then-evaluate contracts with controlled
  cancellation/failure results
- registered only repository-independent cabin policies; SQLite repository and
  production trigger registrations remain deferred to 040c
- added offline Core and Application coverage; the full solution passes 672
  tests

### Status

Complete.
