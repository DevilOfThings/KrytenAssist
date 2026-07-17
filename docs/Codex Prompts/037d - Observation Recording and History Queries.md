# Codex Prompt 037d – Observation Recording and History Queries

## Implementation Prompt

Implement **Step 4 only** from:

```text
docs/AI Playbook/037 - Cruise History and Price Tracking.md
```

Prompts 037a–037c are complete and committed.

Use the agreed boundary:

- 037c established the normalized SQLite schema, basic transactional repository,
  migration, mapping and dependency-injection registration
- 037d hardens recording and queries for concurrency, cancellation, historical
  state recurrence, latest evidence and genuine application restart
- 037e will add the first Cruise History presentation and explicit Record
  Observation action

Do not implement Prompt 037e or any UI work.

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
7. `docs/Codex Prompts/037c - Cruise History SQLite Persistence.md`, including
   Results and Lessons Learned
8. all existing Cruise domain and Application history contracts
9. all existing Cruise persistence entities, configurations, repository and
   migrations
10. all tests under
    `KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/`

Inspect the committed 037c migration and current production database path
behavior before creating another migration. Do not begin implementation until
the latest-snapshot, `LastSeenAt` and retail-source boundaries are understood.

---

## Goal

Make Cruise observation recording and local history queries reliable under the
conditions that occur in a long-running desktop application.

This step owns:

- correcting history-wide fingerprint uniqueness so a prior advertised state
  may recur later
- stable per-history observation sequencing
- retaining latest provider-offer and source-reference evidence without adding
  a price snapshot
- simultaneous first, changed and duplicate recording behavior
- bounded handling of verified transient SQLite contention
- cancellation and transactional rollback during database work
- complete source-separated Get and List behavior
- genuine temporary-file restart-persistence verification
- a real additive corrective migration from the committed 037c schema
- focused Infrastructure and minimal Application contract tests where required

This step does **not** own:

- Record Observation buttons or commands
- history list/detail views
- changes to Cruise capture, parsing or browser navigation
- deciding whether the currently captured amount is original or discounted
- ratings, notes, favourites or preferences
- price monitoring, notifications or booking
- deletion, export, charts or advanced filtering
- changing the production database path
- editing previously committed migrations

No visible application workflow is expected until 037e.

---

## Allowed Changes

Production changes should remain focused inside:

```text
KrytenAssist.Infrastructure/Persistence/
```

Minimal changes are permitted inside:

```text
KrytenAssist.Application/Cruises/
```

only when required to expose provider-independent latest booking evidence in a
recorded history. Do not expose EF entities, database ids or SQLite types.

Create or update tests under:

```text
KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/
KrytenAssist.Avalonia.Tests/Application/Cruises/
```

Create a new additive migration under:

```text
KrytenAssist.Infrastructure/Persistence/Migrations/
```

Update this prompt after implementation:

```text
docs/Codex Prompts/037d - Observation Recording and History Queries.md
```

Do not modify Core unless a focused failing test proves an actual 037a domain
defect. Do not modify Avalonia production code, API behavior, TUI adapters,
browser code, the Roadmap, Playbook, Backlog or session handovers.

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

Application may describe latest evidence through immutable provider-independent
values such as:

```text
provider offer id
source reference
evidence observed timestamp
```

Application must not expose:

- EF entities
- database sequence or generated ids
- SQLite error codes
- transactions
- connection strings

Infrastructure owns concurrency, retry, transaction, schema and mapping details.

Browser, DOM, JavaScript, HTML and TUI-specific payload types must not enter the
repository or persisted models.

---

## Required Correction: Historical State Recurrence

037c added a unique constraint over:

```text
CruiseHistoryId + Fingerprint
```

That constraint prevents a valid history such as:

```text
£988 → £949 → £988
```

The final £988 is meaningfully different from the current £949 even though an
equivalent fingerprint existed earlier.

Correct the schema and repository so duplicate suppression compares only with
the **current/latest snapshot**.

Required behavior:

- A → A records one snapshot and returns AlreadyCurrent for the repeat
- A → B records two snapshots
- A → B → A records three snapshots
- every stored observation remains available in chronological deterministic
  history
- current price and trend use the actual latest snapshot

Remove the history-wide fingerprint uniqueness constraint through the new
migration. Do not edit the committed 037c migration.

Add an Infrastructure-only positive observation sequence such as:

```text
Sequence
```

Enforce:

```text
unique CruiseHistoryId + Sequence
```

Retain a non-unique fingerprint index suitable for latest-evidence comparison.
Sequence is persistence ordering/concurrency evidence and must not become the
domain sailing identity or leak into UI contracts.

Existing 037c rows must receive deterministic sequence values during migration,
ordered by persisted observation time with stable tie-breakers.

---

## Latest Booking Evidence

The following values are evidence but do not define a meaningful advertised
snapshot:

- provider offer id
- source reference
- observation/check timestamp

When an otherwise identical observation is recorded with newer evidence:

- do not insert another observation snapshot
- advance `LastSeenAt` when appropriate
- retain the exact latest provider offer id
- retain the exact latest source reference, including null
- retain when that latest evidence was observed

Do not rewrite an older observation row to pretend its provider offer/reference
was captured at the original snapshot time.

Prefer history-level persisted fields such as:

```text
LatestProviderOfferId
LatestSourceReference
LatestEvidenceObservedAt
```

Names may follow existing conventions.

Rules:

- first recording initializes latest evidence
- strictly newer evidence replaces latest evidence
- older evidence never replaces newer evidence
- equal timestamps use one documented deterministic rule and must not depend on
  writer completion order
- `LastSeenAt` never moves backwards
- latest evidence must survive restart

If future 037e presentation needs this evidence, add the smallest immutable
Application-owned latest-evidence value and expose it from
`CruiseRecordedHistory`. Preserve source and constructor invariants and update
all existing contract tests. Do not expose persistence sequence or database ids.

---

## Atomic Recording Semantics

`RecordAsync` remains one logical atomic operation.

For each attempt:

1. validate sailing key, fingerprint and observation before database mutation
2. honor pre-cancelled input
3. locate or create the exact sailing/source history
4. compare meaningful evidence with the deterministic current snapshot
5. allocate the next sequence only when a snapshot is required
6. add all prices with the snapshot atomically
7. update `FirstObservedAt`, `LastSeenAt` and latest evidence correctly
8. save and commit atomically
9. return the complete committed history and exact storage state

The outcomes remain:

- FirstObservationRecorded
- ChangedObservationRecorded
- AlreadyCurrent

Do not perform read-before-write orchestration in Application.

If an observation timestamp precedes the current snapshot but contains distinct
meaningful evidence, retain it as historical evidence and return a complete
deterministically ordered history. Do not allow an older capture to replace the
current snapshot or latest booking evidence.

Define “current” consistently with the 037a/037b deterministic ordering:

```text
ObservedAt
then canonical fingerprint tie-breaker
```

Sequence must not silently redefine domain chronology.

---

## Concurrency Hardening

Tests must use separate repository instances and separate DbContexts against the
same isolated temporary SQLite file. EF DbContext is not thread-safe; do not
invoke concurrent operations on one context.

### Concurrent First Observation

Two callers recording the same first observation must result in:

- one history
- one observation
- complete prices with no orphans
- one FirstObservationRecorded result
- one AlreadyCurrent result
- equivalent complete returned histories after conflict resolution

### Concurrent Identical Change

When an existing A history receives B from two callers:

- only one B snapshot is stored
- one caller returns ChangedObservationRecorded
- the other returns AlreadyCurrent
- sequence values remain contiguous and unique

### Concurrent Distinct Changes

When two distinct meaningful observations are recorded concurrently:

- neither observation is lost
- both complete price collections remain attached
- no duplicate history or orphan is created
- returned/final history remains deterministic
- latest/current selection follows observed evidence, not task completion order

### Conflict Resolution

Use database constraints and transactions as the final correctness boundary.

Handle only recognised transient SQLite contention or verified uniqueness races.
A bounded retry policy may be introduced inside Infrastructure. It must:

- have a small documented maximum attempt count
- honor cancellation between attempts
- clear or recreate invalid EF tracking state before retry
- re-read stored history and verify evidence before returning AlreadyCurrent
- never translate an unrelated constraint failure into AlreadyCurrent
- never retry validation failures, mapping failures or arbitrary exceptions
- avoid unbounded loops

Do not expose SQLite error codes through Application results.

Tests must be deterministic and use synchronization gates/barriers where needed;
do not depend on lucky task timing or long sleeps.

---

## Cancellation and Rollback

Cover cancellation at these boundaries:

- before repository access
- while transactional SaveChanges is pending
- before commit
- during Get
- during List

After cancelled Record:

- no partial new history remains
- no partial observation remains
- no orphaned prices remain
- an existing history and latest evidence remain unchanged
- a fresh context can retry successfully

Use a deterministic test-only EF interceptor or equivalent synchronization
mechanism to pause the database operation and cancel it. Do not simulate
mid-save cancellation with timing guesses.

Production repository code must propagate `OperationCanceledException`; the
037b Application use case owns conversion to the controlled Cancelled result.

Do not retry cancellation.

---

## Restart Persistence

Use a uniquely named isolated temporary SQLite file and the real migrations.

The restart test must perform:

```text
create provider/context A
migrate
record multiple histories and observations
dispose provider/context/connection A completely
create provider/context B using the same file
read histories
```

Verify after restart:

- exact sailing identities
- source and source-less separation
- every snapshot and complete price collection
- A → B → A recurrence
- deterministic observation order
- non-zero timestamp offsets
- `LastSeenAt`
- latest provider offer id and source reference
- past and future sailings
- correct Get and List results

Delete the temporary database and SQLite sidecar files during cleanup.

Do not keep an in-memory connection alive and call that an application restart.

---

## Get History Hardening

`GetAsync` must:

- honor cancellation
- query exact canonical sailing identity
- normalize the typed source through the existing Core-owned boundary
- keep different retail sources separate
- round-trip source-less history
- return null only for a genuinely absent history
- load complete observations and prices
- return deterministic chronology independent of insertion/query plan order
- include latest booking evidence when added to the Application contract
- perform no mutation

Test source id casing and whitespace at the query boundary.

Do not query by database-generated id as product identity.

---

## List History Hardening

`ListAsync` must:

- honor cancellation
- return an empty read-only result for an empty database
- retain past and future sailings
- return one entry per sailing/source series
- include source-less histories
- load complete snapshots, prices and latest evidence
- apply deterministic database ordering with stable tie-breakers
- return results unchanged across application restart
- perform no mutation

Application remains responsible for analyzer summaries and final presentation
ordering.

Do not add search, deletion, filtering, charts or paging in this prompt.

---

## Corrective Migration

Create a new real additive/corrective migration from the committed 037c model.

The migration must:

- preserve PromptCards
- preserve every existing Cruise history, observation and price
- remove history-wide fingerprint uniqueness
- add deterministic positive observation sequence values
- enforce unique history/sequence
- retain a non-unique fingerprint lookup index
- add latest provider-offer/source-reference evidence fields
- initialize latest evidence deterministically from existing observations
- preserve all foreign keys, checks and bounded-string constraints
- update the model snapshot
- provide a scoped Down operation where practical

SQLite may require table rebuilding for constraint changes. Review generated
SQL/migration operations and prove the upgrade with real migration tests.

Do not edit:

```text
20260703152527_InitialCreate
20260717082520_AddCruiseHistoryPersistence
```

Do not use `EnsureCreated()` in migration or restart tests.

---

## Migration Tests

Test at least:

- empty database through every migration
- database stopped at the committed 037c migration, then upgraded
- existing PromptCard remains unchanged
- existing Cruise history, observations and prices remain unchanged
- deterministic sequences are assigned to existing observations
- latest evidence is initialized correctly
- A → B → A can be stored after migration
- intended indexes and constraints exist
- removed history-wide fingerprint uniqueness no longer exists
- no pending model changes remain after migration generation

Use fixed fictional evidence. Never use Robin's production database.

---

## Test Organization

Extend the existing persistence test support rather than duplicating it.

Suggested focused files may include:

```text
CruiseObservationConcurrencyTests
CruiseObservationCancellationTests
CruiseHistoryRestartPersistenceTests
CruiseHistoryQueryPersistenceTests
CruiseHistoryHardeningMigrationTests
```

Names may follow existing conventions.

Keep tests:

- offline
- isolated
- independently runnable
- free from production paths
- deterministic under repeated execution
- explicit about separate contexts/providers

Do not remove or weaken 037c tests to make new behavior pass. Update only an
assertion that is intentionally superseded by the documented corrective model.

---

## Error Handling

Repository validation and mapping defects should remain exceptions rather than
being mistaken for ordinary persistence outcomes.

Do not:

- expose SQL, connection strings or file paths through custom Application errors
- swallow migration failure
- catch every `DbUpdateException` as a duplicate
- return fabricated empty histories
- delete/recreate a database after failure
- retry forever

After an exhausted verified transient retry, propagate the persistence failure
to the 037b Application boundary for controlled Failed handling.

---

## Pricing Boundary

037d must retain every existing `CruisePrice` exactly, but must not redesign the
pricing model.

The later correction remains deferred for separately representing:

- original price
- discounted price
- per-person discount
- additional booking-level discount
- final booking total

Do not infer those meanings from the current captured values during persistence
hardening.

---

## Production Corrections

Expected corrections in this prompt are:

- replacing history-wide fingerprint uniqueness with latest-only duplicate
  comparison plus sequence uniqueness
- retaining latest non-meaningful booking evidence separately
- verified concurrency conflict handling

Do not redesign completed domain price analysis or Application orchestration.

For any additional correction:

1. prove the issue with a focused failing test
2. make the smallest architecture-consistent correction
3. run the affected regression suite
4. report it under Results

---

## Required Commands

Before implementation, inspect the worktree and preserve unrelated changes.

Run focused Application tests if Application contracts change.

Run all focused persistence tests using the exact project/filter selected during
implementation.

Verify the EF model has no pending migration changes:

```text
dotnet ef migrations has-pending-model-changes \
  --project KrytenAssist.Infrastructure/KrytenAssist.Infrastructure.csproj \
  --context KrytenAssistDbContext \
  --no-build
```

Build the solution:

```text
dotnet build KrytenAssist.sln --no-restore
```

Run the complete regression suite:

```text
dotnet test KrytenAssist.sln --no-build --no-restore
```

Report exact totals, failures, skipped tests, errors and warnings.

---

## Definition of Done

Prompt 037d is complete when:

- A → B → A produces three valid chronological snapshots
- duplicate suppression compares only with the deterministic current snapshot
- observation sequence is positive, unique per history and persistence-only
- latest provider offer/reference evidence is retained without duplicate snapshot
- older evidence cannot overwrite newer latest evidence
- concurrent identical first recording produces one history and one snapshot
- concurrent identical changed recording produces one changed snapshot
- concurrent distinct changes are retained without corruption or orphans
- transient conflict handling is bounded, cancellation-aware and verified
- cancellation during transactional recording leaves no partial mutation
- cancelled work is retryable through a fresh context
- Get and List remain source-separated, complete and deterministic
- real temporary-file restart persistence passes after full disposal/recreation
- a new migration upgrades the committed 037c schema without data loss
- PromptCards and existing Cruise rows survive migration
- no previous migration was edited
- no pending EF model changes remain
- focused tests pass
- complete solution builds
- complete regression suite passes
- no UI, browser, TUI, production-database or pricing redesign work was added
- this prompt's Results and Lessons Learned are complete

Do not begin Prompt 037e.

Stop after Prompt 037d.

---

## Completion Report

Provide:

### Summary

Describe the recording/query hardening and corrected historical semantics.

### Concurrency and Cancellation

Report simultaneous-writer outcomes, retry limits and rollback behavior.

### Latest Evidence and Queries

Report latest booking evidence, source separation, Get/List and restart behavior.

### Migration

Report schema correction, upgrade path and preserved data.

### Files Modified

List every created and updated file.

### Production Corrections

Report each verified correction.

### Build and Tests

Report exact commands and totals.

### Architecture and Scope

Confirm Infrastructure ownership and no presentation or production-data work.

---

## Results

> Complete during implementation.

### Status

Complete.

### Historical State Recurrence

Removed history-wide fingerprint uniqueness and added positive per-history
observation sequence. Duplicate suppression now compares only with the
deterministic current observation (`ObservedAt`, then fingerprint). A → B → A
therefore stores three complete snapshots, while A → A remains one snapshot.
Sequence is unique per history and remains Infrastructure-only.

### Latest Booking Evidence

Added immutable Application-owned `CruiseLatestEvidence` containing exact
provider offer id, optional source reference and evidence timestamp.
`CruiseRecordedHistory` exposes it separately from historical snapshots.
Infrastructure persists latest evidence on the history row: strictly newer
evidence replaces it, older evidence cannot, and equal timestamps use an ordinal
provider-offer/source-reference tie-breaker independent of completion order.
`LastSeenAt` remains monotonic and separate.

### Concurrency

Record uses at most three attempts. It retries only SQLite busy/locked errors and
primary/unique constraint races, clears invalid tracked state, observes
cancellation between attempts and reloads the committed history before deciding
the outcome. Separate-context file-database tests prove concurrent identical
first recordings produce First plus AlreadyCurrent, identical changes produce
Changed plus AlreadyCurrent, and distinct changes are both retained with
contiguous sequences and no orphaned prices.

### Cancellation and Rollback

Added deterministic EF interceptors that cancel after SQL writes but before the
outer transaction commits, and while Get/List readers are pending. Cancellation
propagates, rolls back all changed observation/price/latest-evidence state, and a
fresh context can retry successfully. Pre-cancellation remains covered by 037c.

### Get and List Queries

Get continues to use canonical sailing/source identity, including normalized
source casing/whitespace and source-less history, with no tracked mutation. List
retains past/future and one entry per source series. Both load complete ordered
observations, prices and latest evidence and propagate cancellation.

### Restart Persistence

Added a genuine temporary-file test that disposes the first context and every
connection, then creates a new context against the same migrated file. It proves
source/source-less separation, past/future histories, A → B → A, exact prices,
non-zero timestamp offsets, `LastSeenAt` and latest booking evidence all survive.

### Migration

Generated and reviewed
`20260717090431_HardenCruiseHistoryRecording`. It preserves existing tables and
rows, assigns deterministic sequence using observed time/fingerprint/id, removes
the unique fingerprint index, adds a non-unique lookup index and unique sequence
index, and initializes latest evidence from the deterministic current 037c
observation. Real migration tests stop at 037c, seed histories/prices, upgrade,
verify preservation/indexes and then store a recurring prior state. Neither
previous migration was edited; EF reports no pending model changes.

### Files Created

- `KrytenAssist.Application/Cruises/CruiseLatestEvidence.cs`
- `KrytenAssist.Infrastructure/Persistence/Migrations/20260717090431_HardenCruiseHistoryRecording.cs`
- `KrytenAssist.Infrastructure/Persistence/Migrations/20260717090431_HardenCruiseHistoryRecording.Designer.cs`
- `KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/CruisePersistenceFileDatabase.cs`
- `KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/CruiseObservationConcurrencyTests.cs`
- `KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/CruiseObservationCancellationTests.cs`
- `KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/CruiseHistoryRestartPersistenceTests.cs`
- `KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/CruiseHistoryHardeningMigrationTests.cs`
- `docs/Codex Prompts/037d - Observation Recording and History Queries.md`

### Files Updated

- `KrytenAssist.Application/Cruises/CruiseRecordedHistory.cs`
- `KrytenAssist.Infrastructure/Persistence/CruiseHistoryEntity.cs`
- `KrytenAssist.Infrastructure/Persistence/CruiseHistoryEntityConfiguration.cs`
- `KrytenAssist.Infrastructure/Persistence/CruiseObservationEntity.cs`
- `KrytenAssist.Infrastructure/Persistence/CruiseObservationEntityConfiguration.cs`
- `KrytenAssist.Infrastructure/Persistence/SqliteCruiseObservationRepository.cs`
- `KrytenAssist.Infrastructure/Persistence/Migrations/KrytenAssistDbContextModelSnapshot.cs`
- `KrytenAssist.Avalonia.Tests/Application/Cruises/CruiseRecordedHistoryTests.cs`
- `KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/CruisePersistenceTestDatabase.cs`
- `KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/SqliteCruiseObservationRepositoryTests.cs`
- `KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/CruisePersistenceSchemaAndMigrationTests.cs`

### Production Corrections

- Corrected history-wide fingerprint uniqueness, which prevented a price or
  promotion from returning to an earlier meaningful state.
- Added separate latest booking evidence so identical captures can retain a new
  offer/reference without rewriting historical snapshots.
- Added bounded verified SQLite conflict handling so simultaneous writers return
  correct storage outcomes rather than leaking ordinary uniqueness races.

### Build

Passed: `dotnet build KrytenAssist.sln --no-restore`.

0 errors and 5 existing NU1903 warnings for
`SQLitePCLRaw.lib.e_sqlite3` 2.1.11.

### Focused Tests

Passed:

- Infrastructure persistence filter: 26 passed, 0 failed, 0 skipped
- Application recorded-history contract tests: 6 passed, 0 failed, 0 skipped

### Complete Regression Suite

Passed: `dotnet test KrytenAssist.sln --no-build --no-restore`.

- Core: 105 passed, 0 failed, 0 skipped
- Avalonia: 315 passed, 0 failed, 0 skipped
- API: 9 passed, 0 failed, 0 skipped
- Total: 429 passed, 0 failed, 0 skipped

### Architecture and Scope Check

Verified. Latest evidence is an immutable provider-independent Application
value; sequence, SQLite retry classification, transactions, entities and
migrations remain in Infrastructure. No Avalonia production code, UI, browser,
TUI adapter, API behavior, pricing redesign, production database, Roadmap or
Playbook was changed.

### Notes

All new database tests use migrated in-memory SQLite or uniquely named temporary
files with pooling disabled and sidecar cleanup. Concurrency uses separate
DbContexts/repositories. Cancellation uses synchronization interceptors rather
than timing sleeps. Original/discounted price distinctions remain deferred and
all currently modeled prices still round-trip exactly.

---

## Lessons Learned

> Complete after implementation.

- A fingerprint identifies meaningful state, not a globally unique historical
  event; recurrence requires comparing it with the current snapshot only.
- A persistence sequence solves concurrent allocation and row uniqueness without
  replacing domain chronology based on observed evidence.
- Latest booking evidence belongs beside history metadata because overwriting an
  old snapshot would make its original capture evidence inaccurate.
- Equal-time evidence needs an explicit value-derived tie-breaker; task completion
  order is not durable factual evidence.
- SQLite writer races can surface as busy/locked or uniqueness errors. Bounded
  retry is safe only when error codes are narrowly classified and stored state is
  re-read before selecting AlreadyCurrent.
- Cancellation after SaveChanges is still before the outer transaction commit;
  deterministic interception proves disposal rolls back every aggregate change.
- Restart persistence is only proven after disposing all contexts/connections and
  reopening a real file, not by recycling a context over one in-memory connection.
- Corrective migration backfill must establish valid sequence/latest-evidence
  values before new indexes and check constraints are applied.
