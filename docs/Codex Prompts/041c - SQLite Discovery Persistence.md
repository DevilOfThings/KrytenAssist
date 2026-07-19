# Codex Prompt 041c – SQLite Discovery Persistence

## Implementation Prompt

Implement **Step 041c only** from:

```text
docs/AI Playbook/041 - New Itinerary Detection.md
```

Prompts 041a and 041b are complete. This step implements the Application-owned
discovery repository in normalized local SQLite storage, generates the migration
and completes dependency-injection composition. Do not implement TUI mapping,
New Itinerary alerts, recording triggers or Avalonia presentation yet.

---

## Required Reading

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/037 - Cruise History and Price Tracking.md`
5. `docs/AI Playbook/038 - Saved Cruises and Preferences.md`
6. `docs/AI Playbook/039 - Price Drop Alerts.md`
7. `docs/AI Playbook/040 - Cabin Availability.md`
8. `docs/AI Playbook/041 - New Itinerary Detection.md`
9. `docs/Codex Prompts/041a - New Itinerary Experience and Evidence Contract.md`
10. `docs/Codex Prompts/041b - Itinerary Domain and Application Contracts.md`
11. existing History/cabin/alert entities, configurations, repositories,
    migrations, DI and isolated SQLite test infrastructure

---

## Persistence Boundary

Infrastructure implements the existing Application-owned
`ICruiseDiscoveryRepository`. EF Core and SQLite types stay entirely in
Infrastructure. Do not add persistence annotations, numeric database ids,
navigation properties or SQLite concepts to Core/Application models.

Discovery evidence is an independent factual aggregate. Do not add foreign keys
to:

- Cruise History or price observations
- Saved Cruises, evaluations or favourites
- preferences
- cabin series/history
- alerts or Saved Criteria state

Deleting or changing any of those features must not remove discovery scopes,
checks, occurrences or catalogue entries. Discovery-internal child rows may
cascade only from their owning discovery aggregate. There is no public delete
operation in 041c.

All tests use isolated temporary SQLite databases. Never open, inspect, copy or
migrate Robin's production database.

---

## Required 041b Contract Correction

The first accepted check seeds a baseline without creating first-observed
events. Its itinerary identities still belong in the catalogue. Therefore
`CruiseItineraryCatalogueEntry.FirstObservedEventKey` must be nullable.

Make this focused correction in Application and its tests:

- baseline catalogue entries have `FirstObservedEventKey == null`
- later newly observed entries have a validated 64-character event key
- `ListFirstObservedAsync` returns only entries with a non-null event key
- `GetAsync` may return either baseline or first-observed catalogue entries

Do not manufacture an event key for baseline data. No other 041b contract
redesign is permitted.

---

## Normalized Discovery Schema

Add Infrastructure-owned entities, configurations and DbSets for the following
logical tables. Exact CLR names may follow repository conventions.

### `CruiseDiscoveryScopes`

Store one row per normalized comparable scope:

- internal numeric primary key
- unique 64-character lowercase `ScopeFingerprint`
- normalized retail source id and latest bounded display name
- normalized operator id
- discovery surface enum
- positive capture-contract version
- first accepted check time plus UTC ticks
- last accepted check time plus UTC ticks

The existence of a committed scope row means its baseline has been seeded. A
scope row must be created only in the same transaction as its first accepted
check and occurrences.

Reconstruction recomputes and verifies the scope fingerprint. Do not trust the
stored hash blindly.

### `CruiseDiscoveryScopeCriteria`

Store each semantic material criterion as an internal child of its scope:

- scope foreign key
- normalized criterion name
- known/unknown state

Enforce unique scope/name and valid enum state. Unknown criteria have no values.

### `CruiseDiscoveryScopeCriterionValues`

Store ordered normalized values for known criteria:

- criterion foreign key
- zero-based canonical order
- bounded normalized value

Enforce unique criterion/order and criterion/value. Child values cascade only
with their owning criterion. Repository reconstruction validates contiguous
ordering, distinct values and known/unknown invariants through Core models.

### `CruiseDiscoveryChecks`

Store each accepted explicit check:

- internal numeric primary key
- scope foreign key
- unique 64-character lowercase check/evidence key
- observed time preserving original offset plus UTC ticks
- `WasTruncated` boolean
- accepted occurrence count
- rejected candidate count

Counts are redundant integrity evidence and must match loaded children.
Observed ordering uses UTC ticks, then check evidence key.

### `CruiseDiscoveryCheckOccurrences`

Store positive occurrence evidence as children of one check:

- internal numeric primary key
- check foreign key
- unique-within-check catalogue key
- 64-character occurrence fingerprint
- normalized operator id and provider itinerary id
- normalized retail source id and bounded display name
- optional bounded title, ship, departure date, duration, departure port,
  itinerary summary and provider offer id
- required observed time preserving offset plus UTC ticks
- bounded provider evidence key
- optional bounded trusted source reference

Enforce one occurrence per catalogue identity per check. Each occurrence time
must equal its check time when reconstructed. Recompute and verify itinerary,
catalogue and occurrence identities through Core constructors.

Do not persist prices, promotions or cabin evidence in discovery tables.

### `CruiseDiscoveryCheckRejections`

Store bounded review diagnostics as check children:

- check foreign key
- deterministic zero-based order
- bounded candidate key
- bounded reason

Enforce unique check/order and reconstruct canonical order. Do not store raw DOM,
provider exceptions or unbounded payload fragments.

### `CruiseItineraryCatalogueEntries`

Store one row per retail-source catalogue identity:

- internal numeric primary key
- unique 64-character lowercase catalogue key
- normalized retail source id and latest bounded display name
- normalized operator id and provider itinerary id
- first occurrence foreign key
- latest occurrence foreign key
- first-seen time preserving offset plus UTC ticks
- last-seen time preserving offset plus UTC ticks
- nullable 64-character first-observed event key

The first/latest occurrence foreign keys point to retained discovery occurrence
rows and use Restrict/NoAction, not cascade. Checks are not deletable in 041c,
so these references cannot become orphaned.

Baseline catalogue rows have a null event key. A later unseen itinerary receives
the exact deterministic event key reconstructed from its occurrence, scope and
check. Once assigned, the event key never changes.

Index first-observed UTC/event ordering and latest-seen ordering. Enforce first
time <= last time and paired identity/hash constraints where SQLite can do so.

---

## Atomic Repository Recording

Implement `SqliteCruiseDiscoveryRepository` using the existing explicit
transaction, cancellation and bounded concurrency conventions.

Validate all Core identities and bounds before database access. In one
transaction:

1. detect an existing check by exact check evidence key; if found, reconstruct
   and return `AlreadyRecorded`
2. load the complete scope by scope fingerprint
3. if absent, create the scope and all criteria, marking this check as the
   baseline
4. insert the check, accepted occurrences and rejection diagnostics
5. for each occurrence in deterministic catalogue-key order, load its catalogue
   entry for the same retail source
6. if absent during baseline seeding, create a catalogue entry with a null
   first-observed event key
7. if absent after a prior scope baseline, create the catalogue entry and exact
   `CruiseItineraryFirstObservedEvent`
8. if present, retain first occurrence/time and update last-seen/latest
   occurrence only when the incoming evidence wins the ordering rule
9. update scope last-check metadata monotonically
10. commit and return a reconstructed confirmed result

Latest evidence ordering is:

```text
observed UTC instant
then occurrence fingerprint ordinal
then check evidence key ordinal
```

An earlier out-of-order check is retained but cannot regress catalogue or scope
latest metadata. Original timestamp offsets round-trip; UTC ticks control
ordering.

Repository state mapping:

```text
new scope/check                         -> BaselineSeeded
existing scope, no unseen identity      -> RecordedNoNewItineraries
existing scope, one or more unseen ids  -> RecordedWithFirstObserved
existing exact check key                -> AlreadyRecorded
```

`AlreadyRecorded` returns the originally confirmed check and the same persisted
first-observed events associated with it, if any; it must not create or claim
new data.

Do not call the pure detector optimistically outside the transaction. The
repository owns the atomic baseline/catalogue decision promised by the
Application contract. Reuse the Core event construction to derive and verify
event identities.

---

## Concurrency and Retry

Use the demonstrated SQLite policy:

- explicit transaction per attempt
- at most three bounded attempts
- clear the EF change tracker before retry
- retry only SQLite busy/locked and demonstrated primary/unique conflicts
- use a small cancellation-aware bounded delay
- propagate cancellation
- never broadly retry validation, mapping or arbitrary failures

Required concurrent outcomes:

```text
same first check
  -> BaselineSeeded + AlreadyRecorded

two different first checks for one new scope
  -> one seeds baseline; the committed second check evaluates against it

same unseen identity in concurrent later checks
  -> one catalogue first-observed event; other result cannot claim it as new

different unseen identities in concurrent checks
  -> both retained once with deterministic event ownership
```

After every outcome there is one scope per fingerprint, one check per evidence
key, one occurrence per check/catalogue identity, one catalogue row per key and
no orphaned criteria, values, rejections or occurrence references.

---

## Queries and Reconstruction

### `ListFirstObservedAsync`

- filter out baseline catalogue rows whose event key is null
- load referenced first/latest occurrences without tracking
- order by first-seen UTC descending, then event key and catalogue key ordinal
- return exact validated `CruiseItineraryCatalogueEntry` values

### `GetAsync`

- validate the complete 64-character lowercase catalogue key before querying
- return null when absent
- return baseline or first-observed entries
- reconstruct and verify itinerary/catalogue/occurrence keys and first/latest
  time consistency

### `ListChecksAsync`

- load scopes, ordered criteria/values, occurrences and rejections without
  tracking
- order checks by observed UTC descending, then evidence key
- reconstruct exact Core checks including timestamp offsets and truncation
- verify redundant counts, hashes, scope relationships and child ordering

Every persisted enum, boolean, hash, timestamp companion, relationship and Core
invariant must be validated. Corrupt rows fail explicitly with controlled data
errors; do not silently repair or reinterpret them.

No query may require History, Saved Cruise, cabin or alert rows.

---

## Migration Requirements

Generate and check in one EF Core migration named consistently with:

```text
AddCruiseDiscoveryPersistence
```

Update the model snapshot. The migration must:

- create all normalized discovery tables, constraints and indexes
- configure only discovery-internal cascades and the catalogue occurrence
  references described above
- preserve all PromptCards, History, Saved Cruises/preferences, alerts/settings,
  criteria state and cabin evidence
- migrate cleanly from the current latest migration
- migrate a new empty database through every checked-in migration
- support close/reopen and exact reconstruction

Use the established EF tooling/runtime. Do not hand-edit generated designer or
model-snapshot metadata except where the established workflow requires it.

---

## Dependency Injection

Register in Infrastructure:

```text
ICruiseDiscoveryRepository -> SqliteCruiseDiscoveryRepository
```

Now that the repository exists, register the repository-dependent 041b use
cases through the appropriate service-collection extension:

- `RecordCruiseDiscoveryCheck`
- `ListFirstObservedCruiseItineraries`
- `GetCruiseItineraryDiscovery`
- `ListCruiseDiscoveryChecks`

Keep `CruiseNewItineraryDetector` registered once. API/container validation and
persistence-enabled desktop composition must succeed. Do not register a TUI
capture adapter and do not invoke recording from a ViewModel in 041c.

---

## Required Tests

### Migration and Schema

- empty database migrates through all migrations
- upgrade from the previous latest migration preserves representative existing
  History, Saved Cruise, alert/settings and cabin rows
- expected tables, indexes, foreign keys and check constraints exist
- no discovery foreign key targets an existing cruise feature
- database constraints reject duplicate scope/check/catalogue keys,
  criterion/value/rejection order and check/catalogue occurrence identity

### Record and Round Trip

- first check seeds baseline with null event keys
- later known-only check records no event and advances last seen
- later one/multiple unseen identities return deterministic confirmed events
- exact retry returns `AlreadyRecorded` and the original confirmed events
- complete round trip for scope criteria known/unknown values, occurrences,
  optional fields, rejections, truncation and timestamp offsets
- changed sailing/offer evidence for known itinerary updates latest occurrence
  without changing first occurrence or producing an event
- earlier out-of-order evidence cannot regress latest state
- different source and scope identities remain separate
- duplicate identity in one check is rejected before persistence
- first-observed listing excludes baseline rows and orders deterministically
- get returns baseline and first-observed entries; missing returns null
- close/reopen preserves exact checks, catalogue and event identities

### Cancellation, Corruption and Concurrency

- pre-cancelled and mid-operation cancellation
- invalid hash/enum/timestamp/count/criterion/occurrence/catalogue relationship
  is rejected during reconstruction
- concurrent identical first check
- concurrent different first checks for one scope
- concurrent later checks containing the same unseen itinerary
- concurrent later checks containing different unseen itineraries
- no orphaned or duplicate rows after retries

### Independence and Composition

- discovery evidence survives deletion of price History, Saved Cruises, cabin
  evidence and alerts
- removing discovery rows in an isolated fixture leaves those features intact
- Infrastructure DI resolves repository and all discovery use cases
- generic API composition remains valid
- no TUI adapter, alert materialization or UI behavior is introduced

All tests remain offline and use disposable databases only.

---

## Required Documentation Updates

After implementation and verification:

- complete Results below
- update `docs/AI Playbook/041 - New Itinerary Detection.md`
- update `docs/Roadmap.md`
- create a session handover
- identify Prompt 041d as next without implementing it

---

## Exclusions

- TUI script/payload or provider adapter implementation
- `NewItinerary` alert type, settings, persistence or materialization
- production Record Discovery Check trigger
- Avalonia ViewModels, views or mode changes
- scheduled/background browsing or network polling
- publication, disappearance or withdrawal inference
- modifications to unrelated persistence aggregates
- Prompt 042 Dashboard work

---

## Results

### Status

Complete on 19 July 2026. Normalized discovery persistence, atomic baseline and
first-observed recording, reconstruction, concurrency handling and DI
composition are implemented. No TUI, alert or UI integration was added.

### Files Modified

- `KrytenAssist.Application/Cruises/CruiseDiscoveryContracts.cs`
- `KrytenAssist.Application/DependencyInjection.cs`
- `KrytenAssist.Infrastructure/DependencyInjection.cs`
- `KrytenAssist.Infrastructure/Persistence/CruisePersistenceConversions.cs`
- `KrytenAssist.Infrastructure/Persistence/CruiseDiscoveryEntities.cs`
- `KrytenAssist.Infrastructure/Persistence/CruiseDiscoveryEntityConfigurations.cs`
- `KrytenAssist.Infrastructure/Persistence/SqliteCruiseDiscoveryRepository.cs`
- `KrytenAssist.Infrastructure/Persistence/KrytenAssistDbContext.cs`
- migration and model snapshot files listed below
- `KrytenAssist.Avalonia.Tests/Application/Cruises/CruiseDiscoveryApplicationTests.cs`
- `KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/SqliteCruiseDiscoveryRepositoryTests.cs`
- `KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/CruiseDiscoveryConcurrencyTests.cs`
- `KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/CruisePersistenceDependencyInjectionTests.cs`
- `KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/CruisePersistenceSchemaAndMigrationTests.cs`
- this prompt, Prompt 041 playbook, Roadmap and Session Handover 034

### Migration

- `20260719214241_AddCruiseDiscoveryPersistence.cs`
- `20260719214241_AddCruiseDiscoveryPersistence.Designer.cs`
- `KrytenAssistDbContextModelSnapshot.cs`
- EF reports no pending model changes

### Build and Tests

- focused discovery tests: 20 passed
- solution build: passed with 0 errors and five existing `SQLitePCLRaw`
  advisory warnings
- Core: 155 passed
- Avalonia/Application/Infrastructure: 570 passed
- API: 9 passed
- total: 734 passed, 0 failed, 0 skipped
- `git diff --check`: passed

### Implementation Notes

- Seven normalized tables retain scopes, semantic criteria/values, checks,
  occurrences, rejection diagnostics and source-partitioned catalogue state.
- Baseline catalogue entries correctly persist a null first-observed event key;
  first-observed queries exclude them.
- Recording is transactional and retry-bounded. Exact retries return the
  originally confirmed events, while concurrent later checks can claim an
  unseen itinerary only once.
- First/latest occurrence references preserve factual evidence and exact time
  offsets; UTC tick companions drive ordering.
- Reconstruction recomputes scope, itinerary, catalogue, occurrence, check and
  event identities through Core contracts.
- Discovery tables have no foreign keys to existing cruise features.

### Next

Prompt 041d – Trusted TUI Itinerary Capture.
