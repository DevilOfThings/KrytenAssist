# Codex Prompt 041e – Recording and Alert Integration

## Implementation Prompt

Implement **Step 041e only** from:

```text
docs/AI Playbook/041 - New Itinerary Detection.md
```

Prompts 041a–041d are complete. Add the Application orchestration that records
an explicitly reviewed discovery check before independently materializing typed
New Itinerary alerts. Extend the existing alert domain and SQLite persistence
to support a stable route subject without pretending that an itinerary is a
dated sailing. Do not add the Avalonia New Itineraries presentation or wire a
production ViewModel trigger yet.

---

## Required Reading

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/037 - Cruise History and Price Tracking.md`
5. `docs/AI Playbook/039 - Price Drop Alerts.md`
6. `docs/AI Playbook/040 - Cabin Availability.md`
7. `docs/AI Playbook/041 - New Itinerary Detection.md`
8. Codex Prompts 041a–041d
9. existing alert domain, settings, evaluation/materialization use cases,
   SQLite entities/configurations/repositories/migrations and tests
10. existing discovery recording contracts, SQLite repository and tests

---

## Architectural Boundary

Discovery recording is the primary factual operation. Alert materialization is
a derived, best-effort second operation:

```text
explicit accepted CruiseDiscoveryCheck
        ↓
atomic ICruiseDiscoveryRepository.RecordAsync
        ↓ commit succeeds
confirmed first-observed events (possibly none)
        ↓
read current New Itinerary setting
        ↓
idempotent alert materialization
```

Never place alert persistence in the discovery transaction. Alert settings or
alert persistence failure, cancellation or retry must not roll back, delete or
report failure for a successfully committed factual check.

Keep discovery tables physically independent from alert tables. Do not add
foreign keys between them. The typed alert retains copied bounded evidence
identifiers; the discovery catalogue remains authoritative factual evidence.

---

## Route-Based Alert Subject

The current alert aggregate requires a `CruiseSailingKey`. That is correct for
Price Drop, Promotion, Saved Criteria and Cabin Availability, but incorrect for
New Itinerary. Do not invent a ship, date, duration or representative sailing.

Introduce an immutable closed provider-independent alert subject model with two
valid variants (exact naming may follow existing conventions):

```text
CruiseSailingAlertSubject
  CruiseSailingKey

CruiseItineraryAlertSubject
  CruiseItineraryCatalogueKey
```

`CruiseAlertCandidate` and `CruiseAlert` expose the subject and validate this
strict matrix:

| Alert type | Required subject | Source rule | Required details |
|---|---|---|---|
| PriceDrop | sailing | retail source required | price drop |
| Promotion | sailing | retail source required | promotion |
| SavedCriteria | sailing | source absent | saved criteria |
| CabinAvailability | sailing | retail source required | cabin availability |
| NewItinerary | itinerary catalogue | source must equal catalogue source | new itinerary |

Preserve a convenient sailing-key access path for existing callers if useful,
but it must be impossible to construct a New Itinerary alert with a sailing
subject or an existing alert with an itinerary subject. Do not weaken the
contract into unrelated nullable identity properties.

Add `NewItinerary` as the next stable `CruiseAlertType` numeric value. Existing
numeric values and public behavior remain unchanged.

---

## Typed New Itinerary Details

Add `CruiseNewItineraryAlertDetails` containing bounded, immutable factual
evidence sufficient for later local presentation and strict reconstruction:

- exact `CruiseItineraryKey`
- exact discovery scope fingerprint
- exact discovery check evidence key
- exact occurrence fingerprint/provider evidence key
- first-observed application time
- optional bounded title, ship, departure date, duration, departure port and
  itinerary summary from the confirmed occurrence
- optional bounded trusted source reference

The detail itinerary identity must equal the itinerary alert subject identity.
The detail evidence time must equal the confirmed event/check time. Use existing
Core bounds and normalized values; do not store raw payload, raw query values,
DOM text, price, promotion or cabin state.

The detail describes `First observed by Kryten`. It does not contain or infer a
retailer publication time, availability state or disappearance status.

---

## Deterministic Alert Identity and Compatibility

Keep `CruiseAlertEventKey.Create(...)` for the four sailing-based alert types
byte-for-byte compatible. Existing persisted event keys must reconstruct
unchanged after migration.

Add a separate route-based creation path for New Itinerary using a versioned,
culture-independent canonical value containing:

- New Itinerary type
- catalogue source id
- operator id
- provider itinerary id
- confirmed `CruiseItineraryFirstObservedEvent.EventKey`

Do not include mutable display text, source display name, alert creation time or
settings. Replaying the same discovery event, including an `AlreadyRecorded`
check, must produce exactly the same alert event key. The existing unique event
key and `AddIfAbsentAsync` behavior remain the final concurrency boundary.

---

## Detection and Application Orchestration

Add a small pure Core mapper/detector that converts confirmed
`CruiseItineraryFirstObservedEvent` values to New Itinerary alert candidates
only when `CruiseAlertSettings.NewItineraryEnabled` is true. It must preserve
deterministic event ordering and produce one candidate per distinct confirmed
event.

Add an Application-owned orchestration result that reports the two outcomes
separately, for example:

```text
Recording
  BaselineSeeded | RecordedNoNewItineraries |
  RecordedWithFirstObserved | AlreadyRecorded | Cancelled | Failed

AlertEvaluation
  NotRequired | Disabled | Success | Cancelled | Failed
```

The exact names may follow existing result conventions, but callers must be
able to distinguish a committed check from derived alert failure.

The orchestration must:

1. accept only an already constructed, explicitly reviewed
   `CruiseDiscoveryCheck`
2. call the existing factual recorder first
3. stop with no alert work when factual recording is Cancelled or Failed
4. create no alerts for `BaselineSeeded` or `RecordedNoNewItineraries`
5. evaluate the confirmed events for `RecordedWithFirstObserved`
6. also evaluate events returned by `AlreadyRecorded`, allowing a prior alert
   failure to be repaired safely
7. read current settings only after factual commit and materialize through the
   existing idempotent alert path
8. preserve the successful recording result if settings read or alert
   materialization fails or is cancelled

`AlreadyRecorded` with no confirmed events is `NotRequired`. Disabled settings
create no alert and perform no backfill. Re-enabling affects future explicit
record attempts; replaying an exact recorded check may safely materialize its
persisted confirmed event because event-key deduplication prevents duplicates.

Register the new detector and orchestration through Application dependency
injection. Do not invoke TUI code, browser scripts or repositories directly
from Core.

---

## Alert Settings

Extend `CruiseAlertSettings` with `NewItineraryEnabled`, defaulting to `true`.
Advance its fingerprint version while retaining all existing values and
meaning. Update SQLite settings storage, defaults, constraints and repository
round-trip behavior.

The migration must preserve existing user choices for Price Drop, Promotion,
Saved Criteria, minimum price-drop percentage and Cabin Availability. Existing
settings rows receive New Itinerary enabled by default. Do not reinterpret or
reset prior settings.

Do not add the settings control to Avalonia in 041e; Prompt 041f owns that
presentation wiring.

---

## SQLite Alert Persistence

Generate one EF Core migration for the alert-subject/details/settings extension.
All existing alert rows must survive and reconstruct with identical event keys,
status, source, details and times.

Persist a strict discriminated subject rather than fabricated sailing fields:

- make the existing sailing identity columns nullable as a complete group
- add nullable itinerary operator/provider-id columns as a complete group
- retain retail source id/name and require them for retail-source alert types
- enforce, as far as SQLite permits, exactly one complete subject shape
- sailing types use only sailing columns
- New Itinerary uses only itinerary columns
- Saved Criteria remains the only source-less type

Add a one-to-one normalized New Itinerary detail table. Enforce bounded required
identity, scope/check/occurrence/event evidence and paired optional fields.
Configure cascade only from its owning alert. Do not add a foreign key to the
discovery schema.

Update alert repository queries, mapping and strict reconstruction so exactly
one detail subtype is present and it matches the type and subject. Include the
new detail in `CompleteQuery`. Invalid persisted combinations throw controlled
data-integrity exceptions rather than silently coercing identity.

Update the model snapshot and verify that EF reports no pending model changes.

---

## Required Offline Tests

### Core

- closed sailing/itinerary subject construction and invalid matrices
- all four existing alert candidates retain their exact event keys
- one confirmed discovery event maps to one typed route alert
- subject/detail/source identity mismatch is rejected
- settings enabled/disabled behavior
- deterministic route event key across culture, time zone and mutable display
  changes
- duplicate/reordered confirmed events remain deterministic

### Application

- first scope check seeds baseline and performs no alert work
- known-only later check creates no alert
- later unseen route records first, then creates one alert
- several unseen routes create one alert each in deterministic order
- `AlreadyRecorded` replays confirmed events and deduplicates existing alerts
- `AlreadyRecorded` repairs a prior alert failure without changing discovery
- disabled setting creates no alert
- discovery Cancelled/Failed prevents alert evaluation
- settings/materialization Cancelled/Failed preserves committed factual result
- DI resolves the new detector and orchestration

### SQLite

- migration upgrades a representative pre-041e database without changing
  existing alert event keys or settings choices
- New Itinerary subject/details/settings round-trip after restart
- existing four typed alert subtypes still round-trip
- duplicate/concurrent New Itinerary materialization creates one alert
- malformed mixed/null subject and mismatched detail rows are rejected
- discovery evidence remains after alert deletion and alerts remain independent
  if unrelated History/Saved/Cabin data is removed
- EF model has no pending changes

Use isolated temporary SQLite databases only. Never inspect, migrate or copy
Robin's production database.

---

## Documentation

Complete this prompt's Results, update the Prompt 041 playbook and Roadmap, and
create the next session handover. Identify Prompt 041f as next without
implementing it.

---

## Exclusions

- Avalonia capture/review/recording trigger wiring
- Alert Centre/New Itineraries UI, filters, labels or settings controls
- trusted revisit action or browser navigation
- scheduled/background browsing, polling or notifications
- publication, withdrawal, cancellation or sold-out inference
- discovery schema redesign or foreign keys between discovery and alerts
- price, promotion, Saved Criteria or cabin detection redesign
- Prompt 041f presentation, Prompt 041g audit or Prompt 042 Dashboard

---

## Results

### Status

Complete on 20 July 2026. Explicit factual recording now precedes independent,
retry-safe New Itinerary alert evaluation. Route alerts use stable itinerary
catalogue identity and never synthesize a sailing.

### Files Modified

- Core alert subject, details, event-key, settings and detector files
- Application discovery results, orchestration and dependency injection
- Infrastructure alert entities, configuration, repositories, DbContext,
  migration `20260720185813_AddNewItineraryAlertIntegration` and snapshot
- focused Core, Application and SQLite migration/persistence tests
- minimal safe existing Alert Centre item/settings preservation changes
- this prompt, Prompt 041 playbook, Roadmap and Session Handover 036

### Build and Tests

- solution build: passed with 0 errors and seven existing warnings (five
  `SQLitePCLRaw` advisories and two unused command-event warnings)
- Core: 157 passed
- Avalonia/Application/Infrastructure: 582 passed
- API: 9 passed
- total: 748 passed, 0 failed, 0 skipped
- EF pending model changes: none
- `git diff --check`: passed

### Implementation Notes

- Existing sailing alert event keys remain on the original v1 canonical path.
- New Itinerary uses a distinct route-based canonical key derived from the
  confirmed discovery event.
- Factual discovery and alert transactions remain physically independent.
- Exact recorded-check replay is the supported repair path after derived alert
  failure; unique alert identity makes it idempotent.
- Existing settings saves preserve hidden Cabin Availability and New Itinerary
  flags until Prompt 041f exposes their presentation controls.

### Next

Prompt 041f – New Itineraries Presentation.
