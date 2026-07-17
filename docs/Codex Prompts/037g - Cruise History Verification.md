# Codex Prompt 037g – Cruise History Verification

## Implementation Prompt

Implement **Step 7 only** from:

```text
docs/AI Playbook/037 - Cruise History and Price Tracking.md
```

Prompts 037a–037f must already be complete and committed. Prompt 037f is
committed as `5bb399a` and records a verified baseline of:

```text
Core: 105 passed
Avalonia: 336 passed
API: 9 passed
Total: 450 passed, 0 failed, 0 skipped
```

If the 037f Results, deterministic restart workflow or complete regression suite
are absent or failing, stop and report the prerequisite rather than recreating
037f here.

Do not begin Prompt 038.

---

## Required Reading

Read these files in order:

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/036 - Cruise Discovery and Capture.md`
5. `docs/AI Playbook/037 - Cruise History and Price Tracking.md`
6. Prompts 037a–037f, including Results and Lessons Learned
7. all Core Cruise History production types
8. all Application Cruise History contracts, results and use cases
9. all Infrastructure Cruise persistence entities, configurations, repository,
   migrations and dependency injection
10. all Avalonia Cruise History composition, ViewModels and views
11. the complete Prompt 037 test suite
12. `docs/Session Handovers/2026-07-16 Session 017.md`

Treat committed implementation and passing tests as evidence. This is a
verification and documentation task, not a redesign or feature-development task.

---

## Goal

Verify Cruise History and Price Tracking end to end and close Prompt 037.

Demonstrate that:

- recording occurs only after Robin explicitly chooses Record Observation
- sailing identity is stable, provider-independent and independent of volatile
  retail package evidence
- retail source separates histories for the same physical sailing
- meaningful advertised changes create snapshots
- identical evidence does not create duplicate snapshots
- last-seen evidence can advance without increasing snapshot count
- unlike currency or price basis is never compared
- current, lowest, highest, observation count and trend are correct
- exact original observations and source evidence remain retained
- SQLite persistence uses a real checked-in migration and normalized schema
- record operations are atomic, cancellation-safe and concurrency-safe
- desktop startup uses the existing Infrastructure migration path and a writable
  per-user database location
- recorded history survives complete application/service-provider recreation
- local history can load and be revisited without opening TUI or a browser
- capture-review clearing cannot delete or hide persisted history
- Core, Application, Infrastructure and Avalonia boundaries remain clean
- no cookies, HTML, credentials or personal booking data are persisted
- no ratings, preferences, recommendations, alerts or Prompt 038 behavior were
  introduced
- the complete solution builds and all tests pass
- Robin's manual record/restart/revisit evidence is accurately recorded
- the Playbook, Roadmap, verification prompt and Session Handover describe the
  completed capability and next step consistently

---

## Existing Manual Evidence

Robin manually verified the 037e workflow on **17 July 2026**:

```text
Capture a displayed Cruise
        ↓
Choose Record Observation
        ↓
Confirm the captured price appears in Recorded Cruise History
        ↓
Close and restart Kryten
        ↓
Confirm the recorded history remains available
```

This proved the desktop presentation was using durable SQLite persistence rather
than session-only ViewModel state.

Prompt 037f subsequently changed only:

- singular `1 night` presentation wording
- asynchronous command-generation handling so cancellation/replacement permits
  immediate retry while rejecting late results

It did not change sailing identity, record orchestration, SQLite mapping,
migrations, normal successful recording or history loading.

The existing manual workflow may satisfy the 037g gate if source inspection and
tests confirm those 037f corrections did not invalidate the successful path. If
verification finds a production change affecting normal record or restart
behavior, ask Robin to repeat the manual workflow and record only the result
actually observed.

Do not invent another manual run.

---

## Expected Architecture

Verify this dependency direction:

```text
KrytenAssist.Core
        ↑
KrytenAssist.Application
        ↑
KrytenAssist.Infrastructure

KrytenAssist.Avalonia → Core/Application
KrytenAssist.Avalonia → Infrastructure at composition only
```

Verify this record path:

```text
Successful captured CruiseObservation review
        ↓ Robin chooses Record Observation
CruiseHistoryViewModel
        ↓
RecordCruiseObservation
        ↓
ICruiseObservationRepository
        ↓
SqliteCruiseObservationRepository
        ↓ atomic transaction
Cruise history + observation snapshot + ordered prices + latest evidence
        ↓
CruisePriceHistoryAnalyzer
        ↓
controlled record outcome and refreshed local history
```

Verify this revisit path:

```text
Robin selects Cruise of the Week
        ↓
CruiseHistoryViewModel.Activate
        ↓ no browser navigation or TUI work
ListCruiseHistories
        ↓
local ICruiseObservationRepository
        ↓
deterministic history list, summary and selected detail
```

No SQL, EF entities or database context may enter Core, Application use cases,
Avalonia ViewModels or views. No Avalonia, TUI, browser, DOM, HTML or JavaScript
type may enter the persisted Cruise contracts.

---

## Allowed Changes

The expected outcome is no production or test-code change.

Verification may update:

```text
docs/AI Playbook/037 - Cruise History and Price Tracking.md
docs/Roadmap.md
docs/Codex Prompts/037g - Cruise History Verification.md
docs/Session Handovers/2026-07-17 Session 018.md
```

Create Session Handover 018 because Prompt 037 closes a roadmap capability and
Prompt 038 begins the separate Saved Cruises and Preferences capability.

Production or test files may be modified only if verification exposes a genuine
Prompt 037 defect. Any correction must:

- be minimal
- fix only the verified defect
- preserve existing contracts and architecture
- include a focused deterministic regression test
- rerun focused and complete test suites
- be reported explicitly
- trigger a repeat manual workflow when normal record/restart behavior changes

Do not stage, commit, push, discard or overwrite Robin's work.

---

## Verification Process

### 1. Record the Initial Working Tree

Run:

```text
git status --short
git log --oneline --max-count=10
```

Record all staged, unstaged and untracked files and preserve them. Use actual
commit hashes only. Do not use destructive Git commands.

### 2. Verify Prompt Prerequisites

Confirm from Results and committed code that:

- 037a defines stable domain identity, fingerprints and price analysis
- 037b defines Application-owned repository and use-case contracts
- 037c introduces normalized SQLite persistence and a real migration
- 037d hardens atomic recording, latest evidence, queries, cancellation,
  concurrency and restart behavior
- 037e composes desktop persistence and exposes Record Observation plus local
  history list/detail presentation
- 037f completes deterministic cross-layer, lifecycle and formatting coverage
- every prompt's Results and Lessons Learned are populated
- 037f focused and complete suites pass
- Robin's manual record/restart/revisit evidence is documented

Stop and report any contradiction before marking Prompt 037 complete.

### 3. Verify Project and Dependency Boundaries

Inspect project references, aliases and source placement.

Confirm:

- Core has no EF Core, SQLite, Avalonia, browser, TUI, HTML, HTTP or provider SDK
  dependency
- Application references Core and owns `ICruiseObservationRepository`, result
  types and use cases
- Application does not reference Infrastructure or Avalonia
- Infrastructure references inward and owns EF entities, configurations,
  migrations and repository mapping
- Avalonia references Application and Infrastructure through the established
  aliases and reaches Infrastructure only from composition
- ViewModels depend on Application use cases and provider-independent Core data
- views contain bindings only and browser code-behind has no history persistence
  logic

Search for forbidden dependency leakage rather than relying only on directory
names.

### 4. Verify Sailing Identity

Inspect `CruiseSailingKey` and its tests.

Confirm identity uses stable physical sailing facts such as:

- operator identity
- ship
- departure date
- duration

Confirm identity does not rely solely on or incorrectly include volatile facts
such as:

- retailer package identifier
- source reference URL
- promotion text
- advertised price
- observation timestamp

Verify canonical case/whitespace behavior and that changing any genuine sailing
component changes identity.

### 5. Verify Retail Source Separation

Confirm operator and retail source remain distinct concepts.

For the same sailing:

- TUI and another retail source must create separate histories
- prices from different retail sources must not share one series
- a consistently source-less observation remains supported
- source normalization is deterministic

Provider offer/package identifiers must remain evidence, not the sole sailing
identity.

### 6. Verify Meaningful-Change Semantics

Inspect `CruiseObservationFingerprint`, ordering and persistence usage.

Confirm:

- normalized equivalent observations share a fingerprint
- price ordering and equivalent duplicate prices do not fabricate change
- meaningful advertised offer, price or promotion changes produce a different
  fingerprint
- timestamp and ordinary source-reference changes are evidence rather than a
  reason to create a duplicate snapshot
- returning to a previously seen advertised state later creates a new
  chronological snapshot when it follows a different current state
- equal timestamps use deterministic ordering/evidence rules
- identical current evidence can advance last-seen/latest evidence without
  increasing snapshot count

Corroborate each rule with direct tests.

### 7. Verify Comparable Price and Trend Rules

Inspect `CruisePriceHistoryAnalyzer` and result models.

Confirm:

- a clear GBP per-person price is preferred where defined by the existing rules
- ambiguous candidate prices remain unavailable rather than guessed
- unlike currency or basis is excluded from a comparison series
- current, lowest and highest come from one compatible series
- trend compares the current observation with the immediately previous
  observation in that series
- First, Higher, Lower, Unchanged and Unavailable remain distinct
- latest unavailable price does not silently reuse an earlier headline price
- original observation prices remain retained even when not comparable
- analysis is deterministic and does not mutate input collections

Do not introduce original-versus-discounted interpretation. Prompt 037 stores the
captured known price neutrally; the richer pricing model remains future work.

### 8. Verify SQLite Schema and Migration

Inspect the DbContext, entities, configurations and checked-in migrations.

Confirm:

- the existing database is extended rather than replaced
- Prompt Card persistence remains present
- Cruise histories, observations and prices use normalized tables
- required keys, foreign keys, uniqueness and ordered-price constraints exist
- history uniqueness reflects sailing identity plus retail source
- observations retain sequence/fingerprint, timestamp and evidence
- prices retain amount, currency, optional basis and stable order
- cascade behavior is intentional and tested
- migration from the pre-Cruise schema preserves existing data
- the 037c schema hardening migration preserves rows and initializes added
  evidence/sequence values safely
- an empty database migrates through every checked-in migration
- no `EnsureCreated` shortcut replaces migrations

Report actual migration names and schema evidence found in the repository.

### 9. Verify Transaction, Cancellation and Concurrency Behavior

Confirm repository recording:

- performs aggregate changes atomically
- rolls back on cancellation or constraint failure
- creates one history/snapshot for concurrent identical first observations
- creates one changed snapshot for concurrent identical changes
- retains distinct concurrent meaningful changes without orphaned rows
- uses bounded retry only for demonstrated SQLite concurrency conflicts
- maps cancellation and failures through controlled Application results

Do not weaken tests or transaction boundaries during verification.

### 10. Verify Desktop Composition and Offline Startup

Confirm:

- Application DI registers the analyzer and all Cruise History use cases
- Avalonia composes the existing Application and Infrastructure extensions
- the desktop database path is deterministic and writable beneath the user's
  Local Application Data directory
- tests can override that path and never touch Robin's real database
- migrations run through the existing Infrastructure initializer
- the JSON offline Prompt Card store remains unchanged
- selecting Cruise of the Week loads local history without starting browser
  navigation, reading TUI, invoking HTTP or executing the Cruise skill
- ordinary service resolution performs no external work

### 11. Verify Presentation and Lifecycle

Confirm:

- Record Observation appears only for a successful captured review
- duplicate action is disabled during recording and after the same completed
  review
- busy, cancel, first, changed, already-current, failure and retry states are
  controlled
- successful recording refreshes and selects the affected sailing/source
- loading, empty, error, cancellation and retry states remain distinct
- a failed refresh retains the last good local list
- stale record/load completions cannot overwrite newer state
- replacing a capture or cancelling a load permits immediate safe retry
- changing, refreshing or closing the browser clears only capture-review state
- persisted history and its loaded presentation remain intact
- one-night, price, trend, missing-evidence and source-less formatting is honest
- the known captured price is never falsely labelled discounted
- views remain passive

### 12. Verify Stored Data Boundary

Confirm Prompt 037 stores only provider-independent factual evidence needed for
history:

- sailing and offer facts
- retail source
- source reference
- observation time
- advertised prices and basis
- promotion summary
- latest evidence and last-seen metadata

Confirm it does not store:

- cookies or browser storage
- raw HTML, DOM or extraction payloads
- credentials or payment details
- opaque personal tracking/session identifiers
- personal booking details
- ratings, interest, notes, favourites or preferences

### 13. Verify Scope Exclusions

Search production and tests for accidental Prompt 038 or later behavior.

Confirm Prompt 037 introduced no:

- saved-cruise evaluation state
- interest level or rating
- personal notes
- favourite ships or cruises
- preference profile
- recommendation or best-cruise ranking
- automatic comparison across retailers
- deletion/editing UI
- background monitoring or price-drop alert
- notification, booking or payment behavior

Do not implement any missing future feature.

### 14. Run Focused Verification

Run the Core Cruise History group and the Application/Infrastructure/Avalonia
Cruise History group with explicit filters. Include:

- identity and fingerprint tests
- price analysis tests
- Application record/query tests
- schema and migration tests
- repository transaction/concurrency/cancellation tests
- restart persistence tests
- desktop composition and cross-layer restart tests
- presentation lifecycle and formatting tests

Record exact commands and totals.

### 15. Build and Run Complete Regression Suite

Run:

```text
dotnet build KrytenAssist.sln --no-restore
dotnet test KrytenAssist.sln --no-build --no-restore
```

Report exact totals, failures, skipped tests, errors and warnings.

### 16. Confirm Manual Evidence

Compare the relevant production path with the changes made after Robin's 037e
manual workflow.

If the successful record/restart path remains unchanged, record the existing
17 July 2026 evidence and explain why it remains valid. If it changed materially,
ask Robin to repeat:

1. capture a displayed Cruise
2. choose Record Observation
3. confirm its price/history appears
4. restart Kryten
5. confirm the history remains
6. revisit it without opening TUI

Record only an actual result. Never claim a live check performed by automation.

---

## Documentation Updates

### Playbook 037

Complete the Results and Lessons Learned in:

```text
docs/AI Playbook/037 - Cruise History and Price Tracking.md
```

Record:

- final completion status
- identity and meaningful-change rules
- database schema/migrations and transaction behavior
- local presentation and offline lifecycle
- files created and updated across 037a–037g
- production corrections from the individual prompts
- exact build/test totals
- manual evidence
- actual commits through 037f and 037g left uncommitted for review
- deferred richer price model and Prompt 038 boundary

Do not invent commit hashes.

### Roadmap

Update `docs/Roadmap.md` consistently:

- mark Prompt 037 complete
- summarize explicit recording, deduplicated snapshots, SQLite persistence,
  historical price summary and offline revisit behavior
- record the verified test baseline
- identify Prompt 038 – Saved Cruises and Preferences as next
- remove or correct stale statements that say Prompt 037 has not started or that
  Prompt 036 is still current
- do not mark Prompt 038 started or complete

Preserve historical phase descriptions unless they directly contradict current
status.

### Session Handover

Create:

```text
docs/Session Handovers/2026-07-17 Session 018.md
```

Follow the established format and record:

- Prompt 037 completion
- current clean architecture and composition
- sailing identity and source-separation rules
- meaningful-change and last-seen behavior
- SQLite schema/migration/restart behavior
- Record Observation and local history workflow
- Robin's actual manual record/restart evidence
- final build and test totals
- existing warnings
- known limitation: captured price is not yet separated into original,
  discounted, per-person discount and booking-level discount
- no ratings or preferences exist yet
- Prompt 038 is the recommended next task
- actual commits and current working-tree state only

Do not overwrite another handover.

---

## Definition of Done

Prompt 037g is complete when:

- all 037a–037f prerequisites are verified
- dependency direction and type placement are verified
- sailing identity and source separation are verified
- meaningful-change, duplicate and last-seen rules are verified
- comparable-price and trend semantics are verified
- SQLite schema, migrations and constraints are verified
- transactions, cancellation and concurrency are verified
- desktop composition and offline local loading are verified
- presentation lifecycle and browser/history separation are verified
- stored-data and security boundaries are verified
- Prompt 038 exclusions are verified
- focused suites pass
- complete solution builds
- complete regression suite passes
- manual evidence is accurately recorded
- Playbook 037 Results and Lessons Learned are complete
- Roadmap Prompt 037 is marked complete with Prompt 038 next
- Session Handover 018 is created
- this prompt's Results and Lessons Learned are complete

Do not implement Prompt 038.

Stop after Prompt 037g.

---

## Completion Report

Provide:

### Summary

State whether Prompt 037 is complete.

### Architecture Verification

Report concrete evidence for dependency direction and responsibility boundaries.

### Identity, Change and Price Rules

Report sailing identity, source separation, fingerprint, duplicate, last-seen,
comparison and trend evidence.

### Persistence Verification

Report schema, migration, transaction, concurrency, cancellation and restart
evidence.

### Desktop and Presentation Verification

Report offline activation, explicit recording, controlled lifecycle, local
history and browser/capture separation.

### Stored Data and Scope

Report security boundaries and Prompt 038 exclusions.

### Files Modified

List every file changed by verification.

### Production Corrections

Use `None` or report every verified correction and regression test.

### Build and Tests

Report exact commands and totals.

### Manual Verification

Record only evidence Robin actually performed.

### Documentation

Report Playbook, Roadmap and Session Handover updates.

---

## Results

> Complete during verification.

### Status

Complete. Prompt 037 is verified and closed. Prompt 038 is next and has not
started.

### Prerequisites

Verified. Prompts 037a–037f are committed as `a868133`, `b776b9a`, `a3e3038`,
`394f5b3`, `74078c1` and `5bb399a`. Every prompt has completed Results and
Lessons Learned. 037f records the isolated desktop restart workflow and a green
450-test baseline. Robin's successful manual record/restart workflow is recorded
in 037e.

### Architecture Verification

Verified from project references and source placement. Core has no package or
project dependency and contains only provider-independent Cruise identity and
analysis. Application references Core, owns the repository abstraction, results
and use cases, and contains no Infrastructure or Avalonia reference.
Infrastructure references inward and owns EF Core, SQLite, entities,
configurations, migrations, mapping and transaction behavior. Avalonia uses the
established Application/Infrastructure aliases and reaches Infrastructure from
desktop composition.

Searches found no EF/SQLite/database types in Core, Application Cruise contracts,
Avalonia Cruise History ViewModels or views. Views remain binding-only and the
native-browser code-behind contains no Cruise History persistence logic.

### Identity and Source Separation

Verified. `CruiseSailingKey` contains normalized operator id, ship name,
departure date and duration only. Offer/package id, title, source reference,
price, promotion and timestamp do not define physical sailing identity. Tests
prove canonical equality and that every genuine key component changes identity.

Retail source id is a separate history key component. Repository uniqueness is
operator/ship/departure/duration/source, and Core/Application validation rejects
mixed-source history. Tests prove TUI and another retailer remain separate and a
consistently source-less history round-trips as null. Provider offer id remains
latest/historical evidence rather than the sole identity.

### Meaningful Change and Price Analysis

Verified. The fingerprint canonicalizes offer facts, source, prices and
promotion; it ignores timestamp and source reference. Case/whitespace, price
order and equivalent duplicates do not create false changes. Meaningful offer,
price or promotion changes do. Sequence rather than fingerprint uniqueness
allows a later return to a prior advertised state.

Identical current evidence updates last-seen/latest evidence without increasing
snapshot count. Equal timestamps and latest evidence use deterministic ordinal
rules.

The analyzer prefers one unambiguous GBP per-person price, otherwise accepts one
explicit-basis fallback and reports ambiguity as unavailable. Current, lowest
and highest use the current compatible currency/basis series; trend compares
with the immediately previous observation. Tests cover First, Lower, Higher,
Unchanged and Unavailable, mixed series, latest unavailable evidence and
input-order independence.

### Database and Migration Verification

Verified. The existing DbContext retains Prompt Cards and adds normalized
`CruiseHistories`, `CruiseObservations` and `CruiseObservationPrices` tables.
`20260717082520_AddCruiseHistoryPersistence` creates sailing/source uniqueness,
foreign keys, cascades, length/value constraints and ordered-price uniqueness.
`20260717090431_HardenCruiseHistoryRecording` adds positive per-history sequence
uniqueness and latest offer/reference/time evidence while preserving and
initializing existing 037c rows.

Tests prove initial-schema upgrade preserves Prompt Cards, empty databases run
all migrations, 037c hardening preserves Cruise rows, constraints reject invalid
duplicates/order and cascade removes aggregate children. `DatabaseInitializer`
uses `Database.Migrate()`; no `EnsureCreated` shortcut was found in production.

### Transaction, Cancellation and Concurrency

Verified. Recording validates key/fingerprint/observation agreement, wraps the
aggregate mutation and result mapping in an explicit transaction, checks
cancellation before commit and rolls back failures. Bounded retry is limited to
three attempts for demonstrated SQLite busy/locked and uniqueness conflict
codes.

Tests prove cancellation after save rolls back, query cancellation does not
mutate, constraint failure rolls back the entire aggregate, concurrent first and
identical changed observations create one snapshot, and concurrent distinct
changes are retained without orphan rows.

### Desktop and Presentation Verification

Verified. Application DI registers the analyzer and record/get/list use cases.
Avalonia composes the existing layer extensions and supplies a deterministic
Local Application Data path under `KrytenAssist/krytenassist.db`; tests override
it with isolated paths. The existing Infrastructure initializer applies
migrations, while the JSON Prompt Card store remains unchanged.

Selecting Cruise of the Week activates a local list query without starting the
browser or executing TUI work. Record Observation is available only after a
successful capture review. Tests cover first/changed/already-current/failure,
cancellation, immediate retry, stale-result rejection, refresh, selection,
multi-source lists, empty/error states and honest price/trend formatting.
Browser source change, refresh and close clear capture review through
`SetCapturedObservation(null)` but leave persisted and loaded history intact.

### Stored Data and Scope Verification

Verified. Stored data is limited to sailing/offer facts, retail source,
observation time, source reference, advertised prices/basis, promotion,
fingerprint/sequence and latest/last-seen evidence. Searches found no cookies,
browser storage, raw HTML/DOM payload, credentials, payment details or personal
booking data in the history model or schema.

No saved-cruise interest, ratings, notes, favourites, preferences,
recommendations, cross-retailer ranking, alerts, notifications, booking or
payment behavior was introduced. Original/discounted/extra-discount semantics
remain explicitly deferred. Prompt 038 was not implemented.

### Production Corrections

None. Verification exposed no production or test defect.

The earlier prompt-level corrections remain accurately documented: domain-owned
deterministic fingerprint evidence was exposed for Application/Infrastructure;
037d replaced history-wide fingerprint uniqueness, added separate latest
evidence and bounded SQLite conflict handling; 037e completed desktop
composition; 037f corrected one-night wording and cancellation command release.

### Files Created

- `docs/Codex Prompts/037g - Cruise History Verification.md`
- `docs/Session Handovers/2026-07-17 Session 018.md`

### Files Updated

- `docs/AI Playbook/037 - Cruise History and Price Tracking.md`
- `docs/Roadmap.md`

### Build

Passed:

```text
dotnet build KrytenAssist.sln --no-restore
```

0 errors and 5 existing NU1903 warnings for
`SQLitePCLRaw.lib.e_sqlite3` 2.1.11.

### Focused Tests

Passed Core Cruise History verification:

```text
dotnet test KrytenAssist.Core.Tests/KrytenAssist.Core.Tests.csproj --no-restore --filter 'FullyQualifiedName~CruiseSailingKey|FullyQualifiedName~CruiseObservation|FullyQualifiedName~CruisePriceHistory|FullyQualifiedName~CruisePriceTests'
```

58 passed, 0 failed, 0 skipped.

Passed Application, Infrastructure, migration, restart, composition and
presentation verification:

```text
dotnet test KrytenAssist.Avalonia.Tests/KrytenAssist.Avalonia.Tests.csproj --no-restore --filter 'FullyQualifiedName~CruiseHistory|FullyQualifiedName~CruiseObservation'
```

54 passed, 0 failed, 0 skipped.

### Complete Regression Suite

Passed:

```text
dotnet test KrytenAssist.sln --no-build --no-restore
```

- Core: 105 passed, 0 failed, 0 skipped
- Avalonia: 336 passed, 0 failed, 0 skipped
- API: 9 passed, 0 failed, 0 skipped
- Total: 450 passed, 0 failed, 0 skipped

### Manual Verification

Passed on 17 July 2026. Robin captured a displayed Cruise, chose Record
Observation, confirmed its price appeared in history, restarted Kryten and
confirmed the history remained available.

037f subsequently changed only singular duration text and generation-based
command release for cancellation/replacement. Source inspection and regression
tests confirm the normal successful record, SQLite transaction and restart-load
path did not change, so the existing manual evidence remains valid and was not
invented or repeated by automation.

### Documentation Updates

Completed Playbook 037 Results and Lessons Learned, updated the Roadmap to mark
Prompt 037 complete with Prompt 038 next, and created Session Handover 018.

### Notes

The 037g documentation remains uncommitted for Robin to review. The current
captured known price is intentionally neutral. A future prompt should model
original price, discounted price, per-person discount and booking-level discount
explicitly rather than inferring them from promotion text.

The SQLite package advisory predates Prompt 037 and remains a separate dependency
maintenance task.

---

## Lessons Learned

> Complete after verification.

- Source placement, project references and focused behavioral tests together
  provide stronger architectural evidence than any one of them alone.
- Sailing identity, change fingerprint and latest evidence answer three different
  questions and must remain separate to avoid duplicate snapshots or lost retail
  evidence.
- Removing fingerprint uniqueness in favor of chronological sequence is what
  makes return-to-prior-state history truthful without weakening lookup or
  concurrency guarantees.
- A complete desktop provider recreation against one isolated SQLite file is the
  clearest automated proof of the same durability Robin observed manually.
- Offline-first verification is structural: local activation resolves only
  Application/SQLite services, while all automated clocks, repositories,
  databases and in-flight work are controlled.
- Prompt 037's factual observation boundary leaves Prompt 038 free to add
  subjective ratings and preferences without contaminating historical source
  evidence.
