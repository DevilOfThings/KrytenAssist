# Codex Prompt 040c – SQLite Cabin Persistence

## Implementation Prompt

Implement **Step 040c only** from:

```text
docs/AI Playbook/040 - Cabin Availability.md
```

Prompts 040a and 040b are complete. This step persists the provider-independent
cabin contracts in normalized local SQLite storage, completes persistence for
the Prompt 040 alert/settings/Saved Criteria extensions and registers the
repository-dependent Application services. Do not implement TUI extraction,
production recording triggers or Cabin Availability UI yet.

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
10. existing Cruise History, personal-state and alert EF entities,
    configurations, migrations, repositories, DI and SQLite tests

---

## Persistence Boundary

Infrastructure implements the existing Application-owned
`ICruiseCabinObservationRepository`. EF Core and SQLite types remain entirely in
Infrastructure. Core/Application contracts must not acquire persistence
annotations, entity identifiers, navigation properties or SQLite concepts.

Cabin data is an independent factual aggregate. Do not add foreign keys from
cabin storage to:

- Cruise History or price observations
- Saved Cruises or favourites
- preference profiles
- alerts or Saved Criteria evaluation state

Deleting or changing any of those features must not delete cabin evidence.
Internal cabin children cascade only with their owning cabin aggregate. There
is no public cabin-delete operation in this step.

All tests use isolated temporary SQLite databases. Never open, copy, migrate or
inspect Robin's production database.

---

## Normalized Cabin Schema

Add Infrastructure-owned entities, configurations and DbSets representing the
following logical tables. Exact CLR names may follow existing conventions.

### `CruiseCabinSeries`

Store one row per compatible series:

- numeric internal primary key
- 64-character lowercase SHA-256 `SeriesKey`
- sailing identity: operator id, normalized ship name, departure date and
  duration nights
- normalized retail source id and latest display name
- 64-character search-context fingerprint
- nullable adult count and child count
- explicit child-ages-known flag
- package mode enum value
- nullable normalized departure-airport id
- nullable cabin quantity
- first-observed time
- last-seen time
- latest retailer evidence key
- latest optional source reference
- latest evidence time
- UTC tick companions required for deterministic comparison/order where useful

Use the Core bounds and enum ranges in column lengths/check constraints. Source
identity follows the established retail-source bounds. The series key is unique
and reconstruction must recompute and verify both context fingerprint and
series key; persisted hashes are not trusted blindly.

### `CruiseCabinContextChildAges`

Store known child ages as ordered children of the series:

- series foreign key
- zero-based display/order index
- age constrained to 0–17

Enforce unique series/order and cascade internally. Unknown ages have no rows
with `ChildAgesKnown = false`; known zero-child ages have no rows with
`ChildAgesKnown = true`. Repository reconstruction and Core construction verify
that known age count equals the stored child count.

### `CruiseCabinObservations`

Store each meaningful chronological snapshot:

- internal primary key and series foreign key
- positive per-series persistence sequence
- 64-character lowercase state fingerprint
- Partial/Complete coverage value
- observed/evidence time plus UTC ticks
- bounded retailer evidence key
- optional bounded source reference

Enforce unique series/sequence. Index series/state fingerprint and
series/observed-UTC/state fingerprint for deduplication and deterministic
loading, but **do not make state fingerprint unique within a series**. A real
series may return to an earlier state and that recurrence must remain visible:

```text
Available → Unavailable → Available
```

Equivalent evidence is deduplicated only when its fingerprint equals the
current meaningful observation at record time.

### `CruiseCabinObservationStates`

Store exactly one child row for every existing `CruiseCabinType`:

- observation foreign key
- cabin type
- availability state

Constrain cabin and availability enum ranges and enforce unique
observation/cabin type. Cascade internally. The repository creates all five
rows atomically and reconstruction through the Core model rejects missing,
duplicate or inconsistent category data.

---

## Repository Behaviour

Implement `SqliteCruiseCabinObservationRepository` using the existing
transaction/cancellation/concurrency conventions.

### Record

Validate the supplied observation identity before database access. In one
transaction:

1. load the complete series by `SeriesKey`
2. if absent, create series/context/ages plus sequence 1 observation/states and
   return `FirstObservationRecorded`
3. otherwise reconstruct/verify the stored series identity
4. determine the current meaningful observation using evidence instant in UTC,
   then state fingerprint as the stable equal-time tie-breaker
5. if the incoming state fingerprint equals current, add no observation and
   return `AlreadyCurrent`
6. otherwise append one observation with the next persistence sequence and
   return `ChangedObservationRecorded`
7. advance `LastSeenAt` monotonically
8. replace latest evidence metadata only for a later evidence instant, or at an
   equal instant using a deterministic ordinal evidence-key/reference
   tie-breaker

Equivalent refreshed evidence therefore updates last-seen/latest evidence but
does not overwrite the retained meaningful snapshot's original evidence.
Earlier out-of-order evidence must not regress last-seen or latest metadata.
Preserve exact timestamp offsets on round trip while comparing/order by UTC.

Return the committed, reconstructed `CruiseCabinRecordedHistory`. Do not return
an optimistic pre-commit projection.

### Get and List

- `GetAsync` uses the complete 64-character series identity and returns null
  when absent
- `ListAsync` loads all children without tracking and returns deterministic
  sailing/source/context/series order
- observations reconstruct in evidence UTC order, then state fingerprint
- category states and child ages reconstruct in their canonical enum/order
- every persisted enum, hash, invariant, relationship and redundant identity is
  checked through reconstruction; corrupt rows fail explicitly rather than
  being silently repaired or interpreted

No query may require Cruise History, Saved Cruise or alert rows to exist.

---

## Concurrency and Atomicity

Follow the demonstrated Cruise History policy:

- explicit transaction per record attempt
- bounded maximum of three attempts
- clear the EF change tracker before retry
- retry only SQLite busy/locked and demonstrated primary/unique conflicts
- small cancellation-aware bounded delay
- propagate cancellation
- do not broadly retry validation, mapping or arbitrary database failures

Required concurrent outcomes:

```text
same first state       -> FirstObservationRecorded + AlreadyCurrent
same current change    -> ChangedObservationRecorded + AlreadyCurrent
two distinct changes   -> both retained with unique contiguous sequences
```

After every outcome there is one series, no duplicate child order/category and
no orphaned observation/state/age row.

---

## Complete Prompt 040 Alert Persistence

The current Prompt 039 schema accepts alert types 0–2 only. Extend it safely for
`CruiseAlertType.CabinAvailability` without changing existing event keys or
historic alert meaning.

### Cabin Availability details

Add a one-to-one `CruiseCabinAvailabilityAlertDetails` table containing:

- alert id foreign key with internal cascade
- cabin type
- previous and current explicit state
- direction
- context fingerprint
- evidence coverage
- state fingerprint
- retailer evidence key
- evidence time

Add enum, hash, length and valid-transition constraints. The database must allow
only:

```text
Unavailable → Available / BecameAvailable
Available → Unavailable / BecameUnavailable
```

Update alert type/source constraints so Price Drop, Promotion and Cabin
Availability require a retail source while Saved Criteria forbids one. Update
strict typed-detail mapping so exactly the matching detail payload is present.
All four alert types must round-trip, filter, deduplicate and preserve lifecycle
status after restart.

Cabin alert event-key reconstruction must use the same triggering identity as
040b (`state fingerprint + cabin type` in its canonical format), not the
retailer evidence key stored in the details. Recompute and verify the persisted
event key exactly, as for the existing alert types.

Do not wire cabin candidate materialization to a production trigger in 040c.
The repository merely becomes capable of storing an explicitly materialized
candidate; Prompt 040e owns post-record evaluation wiring.

### Saved Criteria v2 details

Extend Saved Criteria detail persistence to round-trip the 040b fields:

- configured preferred cabins
- matched preferred cabins
- cabin criterion result
- optional cabin context fingerprint
- optional cabin evidence key/time

Use one normalized child table with one row per configured cabin and an
`IsMatched` flag. Enforce unique alert/cabin type, enum bounds and boolean
constraints. This structure makes every matched cabin necessarily configured.

Preserve all pre-040c Saved Criteria rows. Legacy rows reconstruct with empty
configured/matched cabin collections, Unknown cabin result and no cabin
evidence while retaining their existing `CabinPreferencesUnavailable` value.
Do not backfill, re-evaluate or manufacture cabin evidence for historic alerts.

### Alert settings

Add the non-null `CabinAvailabilityEnabled` setting with migration default
`true`. Existing settings rows must retain their three prior values and price
threshold while receiving the enabled default. Update repository read/upsert and
boolean constraints so the complete `CruiseAlertSettings` round-trips exactly.

Saved Criteria evaluation-state storage already accepts versioned fingerprints
and requires no historic state rewrite.

---

## Migration Requirements

Generate one checked-in EF Core migration and update the model snapshot. The
migration must:

- create all normalized cabin tables, constraints, indexes and internal
  cascades
- extend alert type/source constraints and typed detail storage
- extend Saved Criteria detail storage and add its normalized cabin children
- add the default-enabled cabin alert setting
- preserve PromptCards, Cruise History, personal cruise state, all Prompt 039
  alerts/settings and Saved Criteria states
- migrate cleanly from the previous latest migration
- migrate a new empty database through every checked-in migration
- support close/reopen/reconstruction after migration

Use the established EF tooling/runtime. Do not hand-edit generated designer or
model-snapshot metadata except where the repository's established migration
workflow explicitly requires it.

---

## Dependency Injection

Register:

```text
ICruiseCabinObservationRepository -> SqliteCruiseCabinObservationRepository
```

Register the repository-dependent 040b Application use cases now that their
dependencies exist:

- `RecordCruiseCabinObservation`
- `GetCruiseCabinHistory`
- `ListCruiseCabinHistories`
- `EvaluateCruiseCabinAvailabilityAlerts`
- `RecordCruiseCabinObservationAndEvaluateAlerts`

Do not register a TUI capture adapter and do not invoke these services from a
ViewModel, page or existing Cruise recording flow in this step. API/container
validation must remain complete.

---

## Required Tests

### Cabin repository

- first record, meaningful change and already-current result mappings
- exact round trip for every context field, known/unknown child ages, source,
  coverage, five category states, evidence key/reference and timestamp offset
- equivalent evidence adds no snapshot but advances monotonic last-seen/latest
  evidence metadata
- changed evidence appends one snapshot and five state children
- return to an earlier fingerprint is retained as a new transition snapshot
- distinct sailing, source and each material context field create independent
  series
- get missing, deterministic get/list/observation/category ordering
- close/reopen retains exact aggregates
- pre-cancelled and mid-operation cancellation
- invalid/corrupt persisted enum/hash/context/category data is rejected
- database constraints reject duplicate series, sequence, child-age order and
  observation/category

### Concurrency

- concurrent identical first observation
- concurrent identical change
- concurrent distinct changes
- bounded busy/locked/unique retry behaviour
- no duplicate series/snapshot, gaps caused by failed attempts or orphan rows

### Alerts/settings/Saved Criteria

- all four typed alerts round-trip and filter after restart
- Cabin Availability details and lifecycle round-trip exactly
- duplicate cabin event key creates one alert/detail
- Saved Criteria v2 configured/matched cabins and optional evidence round-trip
- old Saved Criteria rows remain reconstructable
- existing settings migration preserves values and defaults cabin alerts on
- complete settings replace/reopen round-trip including disabled cabin alerts
- malformed typed detail combinations/transition tuples are rejected

### Migration and boundaries

- previous latest migration upgrades without data loss
- empty database applies every migration
- expected tables, columns, constraints, indexes and cascades exist
- cabin tables have no foreign keys to History, Saved Cruises, preferences,
  alerts or criteria state
- deleting History/Saved Cruise/alert leaves cabin aggregates intact
- deleting an internal cabin series in a schema test cascades only its ages,
  observations and category states
- DI resolves all newly registered services
- architecture tests confirm no EF/SQLite leakage into Core/Application

All tests remain offline and use temporary databases only.

---

## Allowed Changes

```text
KrytenAssist.Application/DependencyInjection.cs
KrytenAssist.Infrastructure/DependencyInjection.cs
KrytenAssist.Infrastructure/Persistence/*CruiseCabin*.cs
KrytenAssist.Infrastructure/Persistence/CruiseAlertEntities.cs
KrytenAssist.Infrastructure/Persistence/CruiseAlertEntityConfigurations.cs
KrytenAssist.Infrastructure/Persistence/SqliteCruiseAlertRepository.cs
KrytenAssist.Infrastructure/Persistence/SqliteCruiseAlertSettingsRepository.cs
KrytenAssist.Infrastructure/Persistence/KrytenAssistDbContext.cs
KrytenAssist.Infrastructure/Persistence/CruisePersistenceConversions.cs
KrytenAssist.Infrastructure/Persistence/Migrations/*AddCruiseCabinPersistence*.cs
KrytenAssist.Infrastructure/Persistence/Migrations/KrytenAssistDbContextModelSnapshot.cs
KrytenAssist.Avalonia.Tests/Application/DependencyInjectionTests.cs
KrytenAssist.Avalonia.Tests/DependencyInjection/CruiseHistoryDesktopCompositionTests.cs
KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/CruisePersistenceDependencyInjectionTests.cs
KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/*CruiseCabin*.cs
KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/CruiseAlertPersistenceTests.cs
KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/CruisePersistenceSchemaAndMigrationTests.cs
KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/Prompt040BoundaryTests.cs
docs/Codex Prompts/040c - SQLite Cabin Persistence.md
docs/AI Playbook/040 - Cabin Availability.md
docs/Roadmap.md
```

Small changes to existing persistence test helpers are allowed only when needed
to create isolated migrated databases or repositories. Do not stage, commit,
push, discard or overwrite unrelated work.

---

## Exclusions

- TUI/browser extraction or fixtures (040d)
- production Record Cabin action or post-record trigger wiring (040e)
- Cabin Availability UI or alert presentation changes (040f)
- background browsing, scheduling or external notifications
- cabin inventory quantities, cabin numbers, decks or pricing redesign
- deleting/backfilling/re-evaluating historic cabin, criteria or alert data
- Prompt 041/042 work

---

## Verification

Run focused SQLite cabin/alert/migration tests, then:

```text
dotnet build KrytenAssist.sln --no-restore
dotnet test KrytenAssist.sln --no-build --no-restore
git diff --check
```

Use the established single-worker runner when required. No automated test may
access the network or Robin's production database.

---

## Results

Implemented on 19 July 2026.

- added independent normalized cabin series, ordered child-age, meaningful
  observation and per-category state tables
- implemented transactional SQLite record/get/list behaviour with current-state
  deduplication, recurrence retention, monotonic latest evidence, corruption
  checks and bounded concurrency retry
- generated migration `20260719174221_AddCruiseCabinPersistence`, preserving the
  previous Prompt 039 schema and defaulting existing settings to Cabin
  Availability alerts enabled
- completed SQLite round-trip support for Cabin Availability alerts, Saved
  Criteria v2 cabin evidence/details and the full alert settings value
- registered the cabin repository and repository-dependent 040b Application
  services without adding capture, production triggers or UI
- verified migration/restart, exact timestamp offsets, legacy alert/settings
  reconstruction, recurrence, concurrency, internal cascades and independence
  from History/Saved Cruises/alerts using isolated temporary databases
- full solution build succeeds and all 685 offline tests pass

### Status

Complete.
