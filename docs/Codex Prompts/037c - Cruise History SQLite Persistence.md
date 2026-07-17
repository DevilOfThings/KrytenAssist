# Codex Prompt 037c – Cruise History SQLite Persistence

## Implementation Prompt

Implement **Step 3 only** from:

```text
docs/AI Playbook/037 - Cruise History and Price Tracking.md
```

Prompts 037a and 037b are complete.

Use the agreed boundary:

- 037c establishes the normalized EF Core model, SQLite mapping, real migration,
  repository implementation, Infrastructure registration and basic transactional
  round trips
- 037d will harden simultaneous recording, cancellation/rollback, restart
  persistence and complete multi-source edge cases

Do not implement Prompt 037d or any presentation work.

---

## Required Reading

Read these files in order before changing code:

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/037 - Cruise History and Price Tracking.md`
5. `docs/Codex Prompts/037a - Cruise History Domain.md`, including Results and
   Lessons Learned
6. `docs/Codex Prompts/037b - Cruise History Application Contract.md`, including
   Results and Lessons Learned
7. all files under `KrytenAssist.Core/Cruises/`
8. all files under `KrytenAssist.Application/Cruises/`
9. `KrytenAssist.Application/Abstractions/Persistence/ICruiseObservationRepository.cs`
10. all existing files under `KrytenAssist.Infrastructure/Persistence/`
11. `KrytenAssist.Infrastructure/DependencyInjection.cs`
12. existing persistence and Infrastructure tests and project alias conventions

Do not begin implementation until the 037a identity/fingerprint rules and the
037b atomic repository contract are understood.

---

## Goal

Persist Cruise observation histories in Kryten's existing EF Core SQLite
database without leaking persistence concerns outside Infrastructure.

This step owns:

- normalized Infrastructure persistence entities
- focused EF Core configurations
- deterministic SQLite conversions
- `KrytenAssistDbContext` extension
- a real additive EF Core migration
- an Infrastructure implementation of `ICruiseObservationRepository`
- mapping between persistence entities and provider-independent Cruise models
- a basic transactional implementation of Record, Get and List
- first, changed and already-current storage outcomes
- `LastSeenAt` updates without duplicate snapshots
- Infrastructure dependency-injection registration
- isolated SQLite schema, migration, mapping and round-trip tests

This step does **not** own:

- ViewModels, views or commands
- Record Observation UI
- history list/detail UI
- changes to Cruise capture or TUI parsing
- choosing original versus discounted prices
- ratings, notes, favourites or preferences
- monitoring, alerts or booking
- deleting or recreating Robin's database
- full concurrency stress testing
- complete cancellation/rollback and restart-persistence verification
- advanced multi-retailer workflows

The final four persistence concerns are hardened in 037d. Do not add placeholders
for UI or Prompt 038.

---

## Allowed Changes

Production changes should be limited to:

```text
KrytenAssist.Infrastructure/Persistence/
KrytenAssist.Infrastructure/DependencyInjection.cs
```

Create isolated persistence tests in the existing appropriate test project and
folder. Prefer the established Avalonia test project if it already owns local
Infrastructure integration tests and references; do not add a new test project
merely for this prompt.

Create a real migration under:

```text
KrytenAssist.Infrastructure/Persistence/Migrations/
```

Update this prompt after implementation:

```text
docs/Codex Prompts/037c - Cruise History SQLite Persistence.md
```

A minimal Core production correction is permitted only if Infrastructure cannot
persist the canonical 037a fingerprint without duplicating its private
normalization algorithm. Such a correction must:

- expose existing deterministic fingerprint evidence, not define new equality
- remain provider-independent
- be immutable and culture-independent
- receive focused Core regression tests
- be reported under Production Corrections

Do not otherwise modify Core or Application contracts.

Do not modify Avalonia production code, API behavior, browser code, TUI adapters,
the Roadmap, Playbook, Backlog or session handovers.

Do not stage, commit, push, discard or overwrite Robin's work.

---

## Architecture Boundary

Preserve:

```text
Core
  ↑
Application
  ↑
Infrastructure
```

Infrastructure may consume Core domain models and implement Application
abstractions. Core and Application must not reference:

- EF Core
- SQLite
- persistence entities
- database identifiers
- migrations
- connection strings

EF entities must not be returned through `ICruiseObservationRepository`.

Browser, DOM, JavaScript and TUI-specific types must not enter persistence.

---

## Existing Database

Use the existing `KrytenAssistDbContext` and the existing configured SQLite
connection. Do not introduce:

- a second database
- a second connection-string requirement
- JSON persistence
- an in-memory-only production repository
- direct SQL from Avalonia
- `EnsureCreated()` in production

Preserve the existing startup migration behavior through `Database.Migrate()`.
Migration must be additive and retain every existing PromptCard row.

Never silently delete, recreate or fall back from a failed database migration.

---

## Relational Model

Use three normalized Infrastructure-owned entity types, with names following
existing conventions:

```text
CruiseHistories
CruiseObservations
CruiseObservationPrices
```

Do not serialize a complete observation or its prices into an opaque JSON value.

### Cruise History Entity

Store at least:

- database id
- canonical operator id
- canonical normalized ship name
- departure date
- duration in nights
- canonical retail-source id, including a deterministic representation for no
  source
- retail-source display name when present
- first observed timestamp
- last seen timestamp

The product identity remains the provider-independent `CruiseSailingKey` plus
retail-source identity. The database id is persistence-only.

Enforce one history for:

```text
operator id
+ normalized ship name
+ departure date
+ duration in nights
+ normalized retail-source id
```

Retail sources must not share one price series. A source-less observation must
round-trip as `Source == null` and must have one stable database identity.

### Cruise Observation Entity

Store all facts necessary to reconstruct the original provider-independent
`CruiseObservation`, including:

- database id and history foreign key
- deterministic meaningful fingerprint evidence
- provider offer id
- operator display name
- title
- original ship display name
- departure date and duration
- departure port
- itinerary summary
- promotion summary
- source reference
- observed timestamp

The source id/name may live on the history entity, observation entity or both,
provided mapping is lossless and consistency is constrained.

Do not treat provider offer id or source reference as sailing identity.

### Cruise Observation Price Entity

Store each existing `CruisePrice` separately:

- database id and observation foreign key
- exact decimal amount
- uppercase ISO currency
- optional basis
- deterministic display/order value

All prices captured by the existing domain model must survive a round trip.

Do not reinterpret the current captured price as original or discounted. The
future pricing model may separately represent original price, discounted price,
per-person discount, booking-level discount and final booking total, but 037c
must not invent those distinctions.

---

## Constraints and Indexes

Configure explicitly:

- primary keys
- foreign keys
- cascade behavior for owned history data
- required versus optional columns
- sensible maximum lengths for persisted text
- positive duration constraint
- non-negative price constraint
- canonical sailing/source uniqueness
- observation fingerprint uniqueness within one history
- deterministic observation-ordering index
- unique price order within one observation

Use database constraints as the final integrity boundary. Do not depend only on
repository queries or a disabled future UI button.

Do not truncate domain values silently. Invalid or overlong values should fail
in a controlled, testable way rather than corrupt evidence.

Document chosen maximum lengths in Results.

---

## Deterministic SQLite Conversions

Configure culture-independent, round-trip-safe persistence for:

- `DateOnly`
- `DateTimeOffset`, preserving the original offset
- `decimal`
- nullable text and source values

Prefer explicit invariant representations where SQLite provider defaults do not
prove exact round trips.

Tests must use timestamps with a non-zero offset and decimal values that would
expose lossy conversion.

Do not use the system clock, current culture, random semantic values or external
data to determine stored results.

Database ids may use conventional generated values because they are not domain
identity; tests must not depend on their exact value.

---

## Fingerprint Persistence Boundary

The repository must compare and constrain the exact meaningful fingerprint
defined by 037a. Do not recreate its normalization or assemble a second
Infrastructure-owned fingerprint algorithm.

If the existing public Core API is insufficient, make the smallest tested Core
correction described under Allowed Changes, such as exposing the already-owned
canonical fingerprint evidence through an appropriately named read-only member.

Do not use `GetHashCode()` as persisted evidence. It is not a durable database
identity and collision behavior is unsuitable for duplicate protection.

The persisted fingerprint evidence must be stable across process restarts.

---

## DbContext and Configuration

Extend `KrytenAssistDbContext` with focused `DbSet` properties for the new
entities. Continue using assembly configuration discovery.

Create one or more small `IEntityTypeConfiguration<T>` classes rather than
building the schema inline in `OnModelCreating`.

Keep navigation properties and constructors compatible with EF Core while
preventing unrelated code from treating persistence entities as domain models.

Do not add data access logic to the DbContext.

---

## Repository Implementation

Create an Infrastructure class such as:

```text
SqliteCruiseObservationRepository
```

It must implement the existing Application-owned:

```text
ICruiseObservationRepository
```

### RecordAsync

Implement the existing atomic contract in one database transaction:

1. honour pre-cancelled input before mutation
2. identify the history by exact canonical sailing key and retail source
3. create the history if absent
4. compare the supplied canonical fingerprint with the latest stored snapshot
5. insert the observation and every price when the meaningful fingerprint
   changed
6. do not insert a snapshot when it is already current
7. advance `LastSeenAt` to the supplied observation's timestamp when later
8. never move `LastSeenAt` backwards
9. save atomically
10. return the complete mapped history with the correct repository state

The result state must be exactly one of:

- `FirstObservationRecorded`
- `ChangedObservationRecorded`
- `AlreadyCurrent`

No read-before-write orchestration may be moved into the Application use case.

For 037c, implement a correct transaction and uniqueness constraints. Prompt
037d will add adversarial simultaneous-writer testing and any retry strategy
proven necessary by that testing.

### GetAsync

Load exactly one sailing/source history, including observations and prices.

Return `null` when absent. Order observations and prices deterministically before
mapping. Do not mix source histories.

### ListAsync

Load all recorded sailing/source histories, including past sailings, without
network access. Map complete histories because Application owns summary
calculation.

Return an empty read-only result for an empty database. Apply deterministic
database ordering, even though Application also owns final presentation order.

### Mapping

Reconstruct valid existing Core objects rather than bypassing their invariants.
The mapped `CruiseRecordedHistory` must preserve:

- canonical sailing identity
- exact observed display facts
- prices
- source evidence
- observation timestamps and offsets
- `LastSeenAt`
- deterministic observation ordering

A malformed persisted row should fail clearly; do not silently invent missing
domain facts or drop invalid prices.

---

## Migration

Create a real EF Core migration from the current schema.

The migration must:

- leave `PromptCards` and its data intact
- add the normalized Cruise tables
- add all intended foreign keys, checks, uniqueness constraints and indexes
- have a valid Down operation limited to the new Cruise schema
- update `KrytenAssistDbContextModelSnapshot`

Generate the migration using the repository's established EF tooling and design
factory. Review generated code; do not hand-wave missing constraints.

Do not edit or replace the existing `InitialCreate` migration.

---

## Dependency Injection

Register the repository in the existing Infrastructure extension method:

```text
ICruiseObservationRepository -> SqliteCruiseObservationRepository
```

Follow the DbContext-compatible lifetime already used by Infrastructure.

Do not manually resolve it in Program.cs and do not add a second database
initializer.

Add a focused test proving the Application interface resolves to the SQLite
implementation from Infrastructure registration without opening Robin's
production database.

---

## Tests

Use SQLite in-memory databases or isolated temporary database files. Never use
the configured production database path.

Keep every test deterministic and offline.

### Schema and Configuration Tests

Cover:

- expected tables, foreign keys, indexes and check constraints
- canonical sailing/source uniqueness
- observation fingerprint uniqueness per history
- price ordering uniqueness
- required values and cascade behavior
- maximum-length behavior where SQLite requires explicit check constraints

### Mapping and Round-Trip Tests

Cover:

- first observation creates one history, one observation and all prices
- the returned state is FirstObservationRecorded
- exact sailing key reconstruction
- exact source and source-less reconstruction
- optional departure port, itinerary, promotion and source reference
- multiple prices retain amount, currency, basis and deterministic order
- a non-zero `DateTimeOffset` offset survives
- `DateOnly` and exact decimal amounts survive
- Get returns the same complete history through a fresh DbContext
- List returns all persisted histories and includes past sailings
- an empty database returns an empty collection

### Basic Record Semantics Tests

Cover:

- changed fingerprint inserts one new snapshot and returns
  ChangedObservationRecorded
- already-current fingerprint does not insert a snapshot and returns
  AlreadyCurrent
- already-current recording can advance `LastSeenAt`
- an older repeat never moves `LastSeenAt` backwards
- provider offer id or source reference alone does not split the sailing or make
  a meaningful snapshot
- different retail sources create separate histories
- a failed save leaves no partial history, observation or prices
- a pre-cancelled operation performs no mutation

The most adversarial concurrency, cancellation during active database work,
restart and multi-context scenarios belong to 037d. Do not weaken 037c behavior
to defer ordinary correctness.

### Migration Tests

Cover migration from the checked-in previous schema:

- migrate an empty database through all migrations
- migrate a database at `InitialCreate` to the new migration
- preserve an existing PromptCard row
- confirm the new Cruise tables and constraints exist after migration
- reopen through a new DbContext and read migrated data

Do not use `EnsureCreated()` for migration tests.

### Test Hygiene

- dispose connections and contexts
- use unique isolated temporary files only when an in-memory connection cannot
  prove restart or migration behavior
- delete temporary files in cleanup
- do not depend on test execution order
- do not make HTTP, browser or TUI requests
- do not weaken production constraints solely to simplify tests

---

## Error and Cancellation Behavior

Infrastructure should propagate genuine persistence and cancellation exceptions
to the 037b Application boundary, which already maps them to controlled results.

Do not:

- expose connection strings through custom exception messages
- swallow migration errors
- translate constraint failures into ordinary AlreadyCurrent results unless the
  repository has verified that the stored fingerprint truly matches
- catch all exceptions and return fabricated empty histories

Pre-cancelled repository calls must avoid mutation.

---

## Production Corrections

Do not redesign 037a or 037b.

If a verified contract defect blocks correct persistence:

1. prove it with a focused failing test
2. make the smallest correction
3. run the affected regression suite
4. report the correction in Results

Expected possible correction: exposing existing canonical fingerprint evidence
for durable persistence. This is not permission to change what the fingerprint
means.

---

## Required Commands

Before implementation, inspect the current worktree and preserve unrelated
changes.

Run focused Core tests if Core changes:

```text
dotnet test KrytenAssist.Core.Tests/KrytenAssist.Core.Tests.csproj --no-restore
```

Run the focused persistence tests using the exact project and filter selected
during implementation.

Build the solution:

```text
dotnet build KrytenAssist.sln --no-restore
```

Run the complete regression suite:

```text
dotnet test KrytenAssist.sln --no-build --no-restore
```

Report exact totals, failures, skipped tests, errors and warnings.

All tests must remain offline and isolated from Robin's production database.

---

## Definition of Done

Prompt 037c is complete when:

- normalized history, observation and price entities exist in Infrastructure
- canonical sailing plus retail source has a database uniqueness boundary
- complete observations and prices round-trip without opaque serialization
- date, timestamp offset and decimal values round-trip deterministically
- fingerprint evidence is durable and does not duplicate Core normalization
- `KrytenAssistDbContext` includes focused configurations
- Record implements basic transactional first/changed/already-current behavior
- unchanged capture advances but never reverses `LastSeenAt`
- Get and List map complete immutable histories
- a real additive migration upgrades the existing schema
- existing PromptCard data is preserved by migration tests
- Infrastructure DI resolves `ICruiseObservationRepository`
- isolated SQLite schema, constraint, mapping and migration tests pass
- complete solution builds
- complete regression suite passes
- no production database, network, browser or TUI work occurs in tests
- no UI or Prompt 037d hardening is implemented early
- this prompt's Results and Lessons Learned are complete

Do not begin Prompt 037d.

Stop after Prompt 037c.

---

## Completion Report

Provide:

### Summary

Describe the normalized persistence model and repository.

### Schema and Migration

Report tables, identity, constraints, conversion decisions and migration path.

### Repository Behavior

Report Record, Get, List, mapping, transaction and `LastSeenAt` behavior.

### Files Modified

List every created and updated file.

### Production Corrections

Use `None` or report each verified correction.

### Build and Tests

Report exact commands and totals.

### Architecture and Scope

Confirm Infrastructure ownership, existing database use and no presentation or
production-data work.

---

## Results

> Complete during implementation.

### Status

Complete.

### Persistence Model

Added normalized `CruiseHistories`, `CruiseObservations` and
`CruiseObservationPrices` Infrastructure entities. One history represents one
canonical sailing and retail source; observations retain complete advertised
evidence and each existing `CruisePrice` is stored as a separate ordered row.
Database-generated ids remain persistence-only.

### Constraints and Conversions

Added explicit primary/foreign keys, cascading aggregate ownership, positive
duration and non-negative price checks, required/optional length checks, unique
sailing/source identity, unique history/fingerprint evidence and unique price
order. Dates use invariant `yyyy-MM-dd`; timestamps use round-trip `O` strings
that preserve offsets; decimals use invariant `G29` strings for exact values.

Maximum lengths are: operator/source ids 200, ship/source/operator names 500,
offer ids and titles 1000, price basis and departure port 500, itinerary,
promotion and source reference 4000, and canonical fingerprint evidence 16000.

### Repository Behavior

Added `SqliteCruiseObservationRepository` implementing the 037b Record, Get and
List contract. Record validates the supplied identity/evidence, uses one SQLite
transaction, distinguishes first/changed/already-current, stores all prices and
returns complete immutable history. Identical evidence does not add a snapshot;
`LastSeenAt` advances to later supplied evidence and never moves backwards.
Queries reconstruct valid Core models, keep retail sources separate and return
deterministically ordered complete histories.

### Fingerprint Persistence Choice

Core now exposes the existing canonical fingerprint evidence as immutable
`PersistenceKey`, plus `RetailSourceKey` for the typed query boundary.
Infrastructure persists and queries those Core-owned normalized values and does
not duplicate normalization. A blank Infrastructure-only source id represents
`Source == null`; it maps back to null and cannot collide with a valid Core
source id.

### Migration

Generated real additive migration
`20260717082520_AddCruiseHistoryPersistence`. It adds only the three Cruise
tables, their checks, foreign keys and indexes, updates the model snapshot and
has a scoped Down operation. Tests migrate from `InitialCreate`, retain an
existing PromptCard and reopen the upgraded database successfully.

### Dependency Injection

Registered scoped
`ICruiseObservationRepository -> SqliteCruiseObservationRepository` through the
existing `AddInfrastructure` extension. An isolated temporary-file test proves
resolution without touching the production database.

### Files Created

- `KrytenAssist.Infrastructure/Persistence/CruiseHistoryEntity.cs`
- `KrytenAssist.Infrastructure/Persistence/CruiseHistoryEntityConfiguration.cs`
- `KrytenAssist.Infrastructure/Persistence/CruiseObservationEntity.cs`
- `KrytenAssist.Infrastructure/Persistence/CruiseObservationEntityConfiguration.cs`
- `KrytenAssist.Infrastructure/Persistence/CruiseObservationPriceEntity.cs`
- `KrytenAssist.Infrastructure/Persistence/CruiseObservationPriceEntityConfiguration.cs`
- `KrytenAssist.Infrastructure/Persistence/CruisePersistenceConversions.cs`
- `KrytenAssist.Infrastructure/Persistence/SqliteCruiseObservationRepository.cs`
- `KrytenAssist.Infrastructure/Persistence/Migrations/20260717082520_AddCruiseHistoryPersistence.cs`
- `KrytenAssist.Infrastructure/Persistence/Migrations/20260717082520_AddCruiseHistoryPersistence.Designer.cs`
- `KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/CruisePersistenceTestDatabase.cs`
- `KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/CruisePersistenceTestData.cs`
- `KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/SqliteCruiseObservationRepositoryTests.cs`
- `KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/CruisePersistenceSchemaAndMigrationTests.cs`
- `KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/CruisePersistenceDependencyInjectionTests.cs`
- `docs/Codex Prompts/037c - Cruise History SQLite Persistence.md`

### Files Updated

- `KrytenAssist.Core/Cruises/CruiseObservationFingerprint.cs`
- `KrytenAssist.Core.Tests/Cruises/CruiseObservationFingerprintOrderingTests.cs`
- `KrytenAssist.Infrastructure/Persistence/KrytenAssistDbContext.cs`
- `KrytenAssist.Infrastructure/Persistence/Migrations/KrytenAssistDbContextModelSnapshot.cs`
- `KrytenAssist.Infrastructure/DependencyInjection.cs`

### Production Corrections

Exposed the fingerprint's already-owned canonical comparison evidence through
read-only `PersistenceKey` and its existing source-id normalization through
`RetailSourceKey`. This was required for durable duplicate constraints and typed
Get queries without recreating 037a normalization in Infrastructure. Fingerprint
meaning, equality and comparison were not changed. Focused Core regression tests
cover stability, equal evidence and source normalization.

### Build

Passed: `dotnet build KrytenAssist.sln --no-restore`.

0 errors and 5 existing NU1903 warnings for
`SQLitePCLRaw.lib.e_sqlite3` 2.1.11.

### Focused Tests

Passed:

- Infrastructure persistence filter: 13 passed, 0 failed, 0 skipped
- Core fingerprint filter: 8 passed, 0 failed, 0 skipped

### Complete Regression Suite

Passed: `dotnet test KrytenAssist.sln --no-build --no-restore`.

- Core: 105 passed, 0 failed, 0 skipped
- Avalonia: 301 passed, 0 failed, 0 skipped
- API: 9 passed, 0 failed, 0 skipped
- Total: 415 passed, 0 failed, 0 skipped

### Architecture and Scope Check

Verified that EF entities, configurations, repository implementation,
transactions, migration and DI remain in Infrastructure. Core exposes only
provider-independent canonical evidence and Application contracts are
unchanged. No Avalonia production code, browser, TUI adapter, API behavior,
second database, JSON store, Roadmap or Playbook was changed.

### Notes

All persistence tests use an open in-memory SQLite connection or one uniquely
named temporary database deleted during cleanup. They migrate through the real
checked-in migrations and perform no network, browser, TUI or production-file
access. Original-versus-discounted price semantics remain deliberately deferred;
037c losslessly stores the prices represented by the current domain model.

---

## Lessons Learned

> Complete after implementation.

- Canonical identity and display evidence need separate columns: normalized ids
  protect lookup and uniqueness while original source/operator names preserve
  what Robin saw.
- Persisting a domain hash would be unsafe; durable duplicate protection needs
  the exact Core-owned canonical evidence whose equality semantics are tested.
- SQLite text conversions make `DateOnly`, timestamp offsets and decimal values
  deterministic, but scientific decimal notation must also be accepted during
  reconstruction.
- `LastSeenAt` is repository metadata rather than the latest snapshot timestamp;
  an identical later capture can advance it without falsifying price history.
- Migration tests are materially stronger when they begin at the checked-in
  previous migration with existing data, rather than using `EnsureCreated()`.
- Constraint tests should exercise the real SQLite schema as well as inspect it;
  EF model configuration alone does not prove database enforcement.
