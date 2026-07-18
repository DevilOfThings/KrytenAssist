# Codex Prompt 038c – SQLite Personal-State Persistence

## Implementation Prompt

Implement **Step 3 only** from:

```text
docs/AI Playbook/038 - Saved Cruises and Preferences.md
```

Prompts 038a and 038b are complete. This step persists the accepted personal
Cruise contracts locally, registers their composition and verifies migration,
restart, cancellation and concurrency behaviour. Do not add Avalonia UI.

---

## Required Reading

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/037 - Cruise History and Price Tracking.md`
5. `docs/AI Playbook/038 - Saved Cruises and Preferences.md`
6. `docs/Codex Prompts/038a - Saved Cruise Experience and Contract.md`
7. `docs/Codex Prompts/038b - Personal Cruise Domain and Application Contracts.md`
8. accepted Prompt 038 Core and Application contracts
9. existing DbContext, entity configurations, migrations and Prompt 037 SQLite
   repository/tests

---

## Persistence Boundary

Personal state and provider evidence share normalized sailing values but have
no database relationship:

```text
SavedCruises                 CruiseHistories
-------------                ---------------
OperatorId                   OperatorId
ShipName                     NormalizedShipName
DepartureDate                DepartureDate
DurationNights               DurationNights

          application-level identity association only
```

Do not add foreign keys, navigation properties or cascades between these
tables. Saving, dismissing or removing a cruise must be physically incapable of
adding, updating or deleting observations. Deleting History must not remove a
saved cruise.

---

## Required Normalized Schema

Names may follow established conventions, but the following responsibilities
and constraints are required.

### `SavedCruises`

One row per provider-independent sailing:

- surrogate primary key
- normalized operator id
- normalized ship name
- departure date
- positive duration nights
- snapshot title and operator display name
- optional departure port and itinerary summary
- displayed price amount, currency and optional basis
- optional retail source id and name
- optional trusted source reference
- saved timestamp
- shortlist/dismissed status
- optional interest
- optional overall, itinerary, ship and value ratings
- optional notes
- favourite-sailing flag

Required unique index:

```text
OperatorId + ShipName + DepartureDate + DurationNights
```

Retail source, price and source reference must not participate in identity.

Enforce the Core limits and valid enum/rating ranges with SQLite check
constraints. Ratings are null or 1–5. Status is Shortlisted or Dismissed.
Interest is null, Maybe or Strong candidate. Currency is exactly three uppercase
ASCII letters. Price is non-negative. Optional strings are null or non-empty
within their documented maximum length.

The 038b snapshot currently has no explicit retail-source id/name constants,
while existing Cruise persistence consistently uses 200 characters for source
ids and 500 for source names. Treat this as a concrete persistence-blocking
contract gap: add matching snapshot validation/constants or an equally explicit
Core-owned limit, with tests, rather than relying on a late `DbUpdateException`.
Do not change the general `CruiseSource` contract or Prompt 037 behaviour.

### `FavouriteCruiseShips`

One row per normalized operator and ship:

- surrogate primary key
- operator id
- ship name

Add a unique operator/ship index and matching length constraints. This table is
independent of `SavedCruises`; favouriting a ship does not create or rewrite
sailing rows.

### `CruisePreferenceProfiles`

Use a deliberate singleton row with id `1` containing the optional maximum
budget amount, currency and basis. Enforce singleton id, non-negative amount,
three-letter uppercase currency and valid basis.

Budget fields must be all null or all populated. An absent row maps to empty
`CruisePreferences` and is not an error.

### `CruisePreferenceMonths`

Child rows contain profile id and month. Enforce:

- foreign key to profile with cascade only within the preference aggregate
- month from 1–12
- unique profile/month
- deterministic query ordering

### `CruisePreferenceCabins`

Child rows contain profile id and cabin enum value. Enforce:

- foreign key to profile with cascade only within the preference aggregate
- valid current cabin enum values
- unique profile/cabin
- deterministic query ordering

---

## Repository Implementations

Implement the three accepted Application-owned contracts with focused SQLite
adapters using the existing scoped `KrytenAssistDbContext`:

```text
ISavedCruiseRepository
IFavouriteCruiseShipRepository
ICruisePreferencesRepository
```

Provider SDK, browser and Avalonia types must not appear.

### Saved Cruise Repository

- `GetAsync` loads one sailing using all four identity components.
- `ListAsync` uses no tracking and deterministic departure/operator/ship/
  duration ordering.
- `UpsertAsync` inserts or updates the complete accepted aggregate.
- repeated upsert updates snapshot and personal fields without adding rows.
- retail-source changes update snapshot context without changing identity.
- `RemoveAsync` returns true only when personal state was removed.
- all methods honour pre-cancellation and in-flight cancellation.
- mapping reconstructs Core models so database corruption or invalid enum data
  cannot silently bypass domain validation.

### Favourite Ship Repository

- `ListAsync` is no-tracking and deterministically ordered.
- `SetAsync(key, true)` inserts only when absent.
- `SetAsync(key, false)` removes only when present.
- return value reports whether state changed.
- duplicate concurrent favourite requests converge to one row.

### Preference Repository

- missing profile returns empty preferences.
- `SaveAsync` atomically replaces the complete profile and both child
  collections in one transaction.
- save of empty preferences remains a valid explicit profile or removes the
  singleton consistently; choose one representation and test it.
- cancellation/failure before commit leaves the previous complete profile
  intact, never a partial month/cabin set.

### Transactions and Concurrency

Use explicit transactions for multi-step writes. Follow the bounded retry and
change-tracker clearing pattern proven by Prompt 037 for transient SQLite
locking or unique-key races.

Concurrent operations must satisfy:

- one saved row for one sailing
- one favourite row for one ship
- no partially replaced preference profile
- no effect on Cruise History
- bounded retries; no infinite loops

Do not introduce a global lock or service-wide static state.

---

## Migration

Create one checked-in EF Core migration based on the verified Prompt 037j
snapshot. The premature prototype migration removed by 038b must not reappear.

The migration must:

- add only the five personal-state tables, their constraints and indexes
- preserve existing PromptCards and all Cruise History rows
- migrate cleanly from the latest Prompt 037j database
- migrate an empty database through all checked-in migrations
- roll back only the five new tables in dependency-safe order
- update `KrytenAssistDbContextModelSnapshot`

Do not use `EnsureCreated`, raw startup schema creation or destructive data
rewrites.

Tests must never migrate Robin's real database.

---

## Dependency Injection

Register repositories through the existing Infrastructure extension method.
Register the Prompt 038b use cases through the existing Application extension
method now that their repository dependencies are available in the complete
application composition.

Add deterministic composition tests proving:

- all three contracts resolve to Infrastructure adapters
- Prompt 038 use cases resolve in a scoped provider
- repository and DbContext lifetimes are scoped
- existing Prompt 037 observation repository still resolves unchanged
- API and desktop service-provider validation succeeds

Do not add manual registrations to `Program.cs` or Avalonia startup code.

---

## Required Tests

Add isolated SQLite tests covering:

### Schema and Migration

- all five tables exist with required constraints and indexes
- latest Prompt 037j database migrates without losing PromptCards or Cruise
  History
- empty database applies every checked-in migration
- invalid status, interest, rating, month, cabin, price, currency, budget basis,
  incomplete budget tuple and duplicate identities are rejected by SQLite
- Down migration removes personal tables without altering earlier tables

### Saved Cruises

- insert, get, deterministic list and restart persistence
- full optional evaluation round-trip
- source/price snapshot refresh without duplicate sailing
- update preserves exact accepted values
- remove personal state while History remains unchanged
- deleting History while saved state remains unchanged
- cancellation before and during save/remove
- concurrent first saves converge to one row

### Favourite Ships

- set, unset, unchanged result and deterministic list
- identity normalization and restart persistence
- concurrent duplicate favourite requests converge to one row
- no saved sailing is required and no sailing row is rewritten

### Preferences

- empty defaults when no row exists
- multiple deduplicated months and cabins plus budget round-trip
- update and clear profile
- cancellation/failure rolls back the complete previous profile
- restart persistence and deterministic ordering

All tests must use temporary or in-memory isolated SQLite databases. No test may
contact TUI, launch a browser or access Robin's database.

---

## Allowed Changes

```text
KrytenAssist.Application/DependencyInjection.cs
KrytenAssist.Infrastructure/DependencyInjection.cs
KrytenAssist.Infrastructure/Persistence/KrytenAssistDbContext.cs
KrytenAssist.Infrastructure/Persistence/*SavedCruise*
KrytenAssist.Infrastructure/Persistence/*FavouriteCruiseShip*
KrytenAssist.Infrastructure/Persistence/*CruisePreference*
KrytenAssist.Infrastructure/Persistence/Migrations/
KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/
KrytenAssist.Avalonia.Tests/DependencyInjection/
docs/Codex Prompts/038c - SQLite Personal-State Persistence.md
docs/AI Playbook/038 - Saved Cruises and Preferences.md
docs/Roadmap.md
```

Prompt 038b Core/Application contracts may be changed only if implementation
reveals a concrete persistence-blocking defect. Document and test any such
correction. Do not stage, commit, push, discard or overwrite unrelated work.

---

## Exclusions

- Avalonia commands, ViewModels or UI
- saving from capture or History presentation
- joining Saved Cruises to price-history query results
- recommendations, rankings, alerts or automatic inference
- provider, source, browser, capture or price-model changes
- Prompt 038d or later implementation

---

## Verification

Run focused persistence, migration, concurrency, restart and DI tests, then:

```text
dotnet build KrytenAssist.sln --no-restore
dotnet test KrytenAssist.sln --no-build --no-restore
```

Use the stable single-worker runner where required. The complete provider must
validate and all tests must remain offline.

---

## Results

> Complete after implementation and verification.

### Status

Complete.

### Summary

- Added normalized SQLite entities and constraints for saved sailings,
  favourite ships, the singleton preference profile, months and cabins.
- Kept personal state physically independent from Cruise History; no personal
  table has a History foreign key or cascade.
- Implemented focused saved-cruise, favourite-ship and preference repositories.
- Added transactional saved-cruise upsert and complete preference replacement,
  deterministic queries, cancellation and bounded conflict handling.
- Added the missing Core-owned saved-snapshot retail-source limits identified
  during analysis.
- Registered repositories through Infrastructure and Prompt 038 use cases
  through Application composition.
- Added migration `20260718091822_AddPersonalCruiseState` from the verified
  Prompt 037j model snapshot.

### Files Added

- personal-state entities and configurations under
  `KrytenAssist.Infrastructure/Persistence/`
- `SqliteSavedCruiseRepository.cs`
- `SqliteFavouriteCruiseShipRepository.cs`
- `SqliteCruisePreferencesRepository.cs`
- migration `20260718091822_AddPersonalCruiseState`
- `PersonalCruisePersistenceTests.cs`
- this prompt

### Files Updated

- `SavedCruiseSnapshot.cs`
- Application and Infrastructure dependency-injection extensions
- `KrytenAssistDbContext.cs` and its model snapshot
- persistence schema, dependency-injection and desktop-composition tests
- `SavedCruiseTests.cs`
- Prompt 038 playbook and Roadmap

### Build

Passed:

```text
dotnet build KrytenAssist.sln --no-restore -m:1
```

Result: 0 errors. Existing SQLite package advisory and unused command-event
warnings remain.

### Tests

Focused persistence, migration, concurrency and composition: 16 passed.

Complete offline regression:

```text
Core:      120 passed
Avalonia:  447 passed
API:         9 passed
Total:     576 passed, 0 failed, 0 skipped
```

### Notes

- Saving the same sailing refreshes its snapshot without creating another row.
- Missing preference profile maps to empty preferences; saving empty preferences
  keeps an explicit valid singleton with empty child collections.
- Favourite ships require no saved sailing and never rewrite sailing rows.
- Prompt 038d owns capture/History save actions and evaluation editing.

### Next

Prompt 038d – Save Actions and Evaluation Editing.
