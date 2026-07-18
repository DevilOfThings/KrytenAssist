# Codex Prompt 039c – SQLite Alert Persistence

## Implementation Prompt

Implement **Step 3 only** from:

```text
docs/AI Playbook/039 - Price Drop Alerts.md
```

Prompts 039a–039b are complete. This step persists the agreed alert aggregate,
settings profile and saved-criteria transition state in local SQLite. Do not
integrate alert evaluation with observation recording, saved-cruise actions or
the Avalonia UI yet.

---

## Required Reading

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/037 - Cruise History and Price Tracking.md`
5. `docs/AI Playbook/038 - Saved Cruises and Preferences.md`
6. `docs/AI Playbook/039 - Price Drop Alerts.md`
7. `docs/Codex Prompts/039a - Price Drop Alert Experience and Contract.md`
8. `docs/Codex Prompts/039b - Alert Domain and Application Contracts.md`
9. the 039b Core alert models, Application repository contracts and use cases
10. existing Cruise History and personal-state entities, configurations,
    migrations, repositories, conversions, DI and isolated-SQLite tests

---

## Agreed Persistence Shape

Keep persistence models internal to Infrastructure. Do not annotate Core models
or expose EF Core/SQLite types through Application contracts.

### Alert Header

Add a `CruiseAlerts` table containing the fields common to every alert:

- stable alert `Guid` id
- 64-character lowercase hexadecimal event key
- alert type and lifecycle status
- normalized sailing identity: operator, ship, departure date and duration
- optional retail source id/name pair
- event/evidence timestamp and alert-created timestamp
- numeric UTC ordering values for both timestamps

The displayed `DateTimeOffset` values must round-trip with their original
offsets. Ordering must use the corresponding UTC values so equivalent instants
and different offsets cannot sort incorrectly as strings.

Constraints must enforce valid type/status ranges, positive duration, bounded
identity/source text, paired source fields and source presence rules:

- Price Drop and Promotion require a retail source
- Saved Criteria requires no retail source

Create a unique index on `EventKey`. This database constraint is the
authoritative deduplication boundary. Add indexes supporting deterministic
newest-first listing, unread count and type/status filters where useful.

### Typed Detail Tables

Do not persist the closed detail hierarchy as JSON or one large nullable
property bag. Add three one-to-one tables whose primary key is also a foreign
key to `CruiseAlerts`:

```text
CruisePriceDropAlertDetails
CruisePromotionAlertDetails
CruiseSavedCriteriaAlertDetails
```

Use cascade delete only from an alert header to its owned detail row. No other
table owns or cascades to an alert.

Price Drop detail stores:

- previous/current amount, currency and optional basis
- positive reduction and percentage reduction
- triggering evidence key

Promotion detail stores:

- optional previous bounded summary
- required current bounded summary
- triggering evidence key

Saved Criteria detail stores:

- month-configured-and-matched flag
- optional configured budget amount/currency/basis
- optional matched price amount/currency/basis
- criteria fingerprint
- evidence origin, key and offset-preserving time
- cabin-preferences-unavailable flag

Database constraints should mirror Core invariants where SQLite can express
them safely: valid enum/boolean ranges, non-negative amounts, uppercase
three-letter currencies, paired optional budget/matched-price groups, positive
Price Drop reduction, percentage 0–100 and bounded required/optional text.
Core reconstruction remains the final invariant check.

Store decimals with the existing invariant decimal conversion, not SQLite
binary floating point. Do not silently reduce the Core model's four-decimal
Price Drop percentage precision or arbitrary valid monetary precision.

### Alert Settings

Add a singleton `CruiseAlertSettings` table with a fixed key and complete
profile:

- Price Drop enabled
- Promotion enabled
- Saved Criteria enabled
- minimum Price Drop percentage

Missing storage returns `new CruiseAlertSettings()`. Saving replaces the whole
profile atomically. Enforce singleton identity, boolean ranges and percentage
0–100. Settings are independent personal configuration and have no foreign key
to alerts, History or Saved Cruises.

### Saved Criteria Evaluation State

Add `SavedCruiseCriteriaEvaluationStates` containing:

- normalized sailing identity
- criteria fingerprint
- evidence key
- offset-preserving evidence time plus UTC ordering value
- `Unknown`, `NotMet` or `Met` result

Use sailing identity plus criteria fingerprint as the unique logical key. This
table is transition/deduplication state only; it is not Saved Cruise data and
must have no foreign key to `SavedCruises` or Cruise History.

An upsert replaces the state for that logical key. Concurrent writes must
converge deterministically on the newest evidence instant; for equal instants,
use ordinal evidence key as the stable tie-breaker. An older completion must not
overwrite newer state.

---

## Repository Implementations

Implement all three 039b Application abstractions:

```text
ICruiseAlertRepository
ICruiseAlertSettingsRepository
ISavedCruiseCriteriaStateRepository
```

### Alert Repository Behaviour

- `GetAsync` reconstructs exactly one validated typed aggregate or returns null.
- `ListAsync` applies optional type/status filters in the database.
- ordering is event instant descending, created instant descending, then event
  key and stable id ordinal for a fully deterministic result
- `CountUnreadAsync` counts in SQLite without materialising alerts
- `UpdateStatusAsync` performs only the lifecycle change and reports whether an
  alert existed; invalid enum input is rejected before database access
- `AddIfAbsentAsync` inserts header and exactly one matching detail atomically
- an existing event key returns `Created == false` and the already persisted
  aggregate, regardless of the incoming display id or created timestamp
- concurrent first inserts for one event key converge on one row and one detail
  without leaking a uniqueness exception

Do not add permanent deletion. Do not mutate History, Saved Cruises, criteria
state or settings as a side effect of alert operations.

### Settings Repository Behaviour

- missing row returns Core defaults
- save atomically replaces every setting
- exact decimal and boolean values survive restart
- concurrent saves leave one valid complete profile, never a partially mixed
  profile

### Criteria State Repository Behaviour

- get uses the complete sailing/fingerprint key
- upsert is atomic and concurrency-safe
- newest evidence wins, with evidence key breaking equal-time ties
- exact timestamp offset survives restart
- upsert never requires a Saved Cruise or History row to exist

All methods must honour pre-cancelled and in-flight cancellation. Do not catch
and translate repository failures into Application results here; the 039b use
cases already own that boundary.

---

## Migration and Composition

Add one checked-in EF Core migration, suggested name:

```text
AddCruiseAlertPersistence
```

Update the model snapshot. The migration must:

- create all alert/settings/state tables, constraints and indexes
- migrate cleanly from every currently supported checked-in migration
- preserve Prompt Cards, Cruise History and personal Cruise state
- leave all new tables empty/default-on-read after upgrade
- migrate a brand-new empty database through the complete chain
- downgrade by removing only Prompt 039c objects

Register the three SQLite repositories in Infrastructure DI. Once those
registrations exist, register the repository-dependent 039b alert use cases in
Application DI. Verify both API and desktop service-provider validation; 039c
must not reintroduce unresolved alert dependencies.

Do not alter connection-string ownership, startup initialization or existing
database location.

---

## Independence and Ownership

The schema must deliberately contain no foreign keys between alerts or criteria
state and:

- `CruiseHistories`
- `CruiseObservations`
- `SavedCruises`
- Cruise preference tables

Deleting History must leave alerts and criteria state unchanged. Removing a
Saved Cruise must also leave them unchanged. Alert lifecycle changes must not
touch either factual or personal Cruise state.

The only Prompt 039c cascade is alert header to its own typed detail.

---

## Required Tests

Use isolated temporary SQLite databases only. Never use Robin's application
database or network access.

### Schema and Migration

- all six new tables are created
- event-key and criteria logical-key uniqueness exist
- header/detail ownership and cascade are correct
- no foreign keys connect alerts/state to History or Saved Cruises
- constraints reject invalid enums, booleans, percentages, currencies,
  incomplete source pairs and malformed typed detail groups
- migration from the previous latest migration preserves representative Prompt
  Card, History, Saved Cruise, favourite and preference data
- empty database migrates through all checked-in migrations and reopens

### Alert Round Trip and Query

- each of the three typed detail payloads round-trips exactly
- ids, keys, offsets, decimal precision and optional fields round-trip
- get missing returns null
- list ordering is deterministic across different timestamp offsets and ties
- type/status filters and unread count are database-backed and correct
- Unread/Read/Dismissed updates preserve every non-lifecycle field
- lifecycle missing and invalid paths are correct
- cancellation is honoured

### Deduplication and Concurrency

- sequential duplicate event keys return Created then AlreadyExists
- duplicate input with another id/created time returns the stored aggregate
- concurrent first inserts from separate contexts produce one header/detail and
  exactly one Created result
- different event keys remain independent
- failed header/detail persistence cannot leave an orphan or partial alert

### Settings and Criteria State

- missing settings return defaults
- complete changed settings round-trip and survive restart
- settings replacement cannot retain stale fields
- criteria states for different sailing/fingerprint keys remain independent
- state upsert survives restart and preserves offset/result
- older concurrent state cannot overwrite newer evidence
- equal-time evidence-key tie-break is deterministic
- settings/state cancellation is honoured

### Cross-Aggregate Independence and Composition

- deleting History leaves alerts and criteria state intact
- removing Saved Cruise leaves alerts and criteria state intact
- alert status changes leave History and Saved Cruise rows unchanged
- Infrastructure resolves all three repository contracts
- complete API and desktop composition validates after Application use-case
  registration

Retain the focused in-memory 039b tests. Do not replace them with SQLite tests.

---

## Allowed Changes

```text
KrytenAssist.Infrastructure/Persistence/*CruiseAlert*.cs
KrytenAssist.Infrastructure/Persistence/*Criteria*State*.cs
KrytenAssist.Infrastructure/Persistence/KrytenAssistDbContext.cs
KrytenAssist.Infrastructure/Persistence/CruisePersistenceConversions.cs
KrytenAssist.Infrastructure/Persistence/Migrations/*AddCruiseAlertPersistence*.cs
KrytenAssist.Infrastructure/Persistence/Migrations/KrytenAssistDbContextModelSnapshot.cs
KrytenAssist.Infrastructure/DependencyInjection.cs
KrytenAssist.Application/DependencyInjection.cs
KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/*CruiseAlert*.cs
KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/*Criteria*State*.cs
KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/CruisePersistenceSchemaAndMigrationTests.cs
KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/CruisePersistenceDependencyInjectionTests.cs
KrytenAssist.Avalonia.Tests/DependencyInjection/*CruiseAlert*.cs
docs/Codex Prompts/039c - SQLite Alert Persistence.md
docs/AI Playbook/039 - Price Drop Alerts.md
docs/Roadmap.md
```

Existing 039b contracts may change only for a concrete defect blocking correct
persistence. Document and test any such correction. Do not stage, commit, push,
discard or overwrite unrelated work.

---

## Exclusions

- observation record-then-evaluate integration (039d)
- saved-cruise/preference trigger integration (039e)
- Alert Centre, settings editor or other Avalonia presentation (039f)
- permanent alert deletion or automatic retention expiry
- JSON detail payload storage
- foreign keys or cascades to History/Saved Cruises/preferences
- background browsing, scheduling, network or external notifications
- cabin availability matching or Prompt 040 work

---

## Verification

Run focused persistence/migration/composition tests, then:

```text
dotnet build KrytenAssist.sln --no-restore
dotnet test KrytenAssist.sln --no-build --no-restore
git diff --check
```

Use the established single-worker runner where SQLite contention requires it.

---

## Results

Implemented on 18 July 2026.

### Status

Complete.

### Implementation

- Added a normalized alert header with one owned typed detail table for each
  Price Drop, Promotion and Saved Criteria payload.
- Added exact invariant-decimal and offset-preserving timestamp persistence,
  with UTC ticks for deterministic ordering.
- Added singleton alert settings and independent saved-criteria state with
  newest-evidence/evidence-key conflict resolution.
- Implemented all three Application repository contracts, database-backed
  filtering/count/lifecycle operations and event-key deduplication.
- Added the `AddCruiseAlertPersistence` migration, model snapshot, Infrastructure
  registrations and repository-dependent Application registrations.
- Verified the alert/state schema has no relationships to History, Saved
  Cruises or preferences; only alert-owned detail rows cascade.

### Verification

- `dotnet build KrytenAssist.sln --no-restore`: passed with 0 errors.
- `dotnet test KrytenAssist.sln --no-build --no-restore`: 634 passed, 0 failed,
  0 skipped.
- Existing SQLitePCLRaw advisory warnings remain unchanged.
