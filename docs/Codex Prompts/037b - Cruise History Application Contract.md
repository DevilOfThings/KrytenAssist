# Codex Prompt 037b – Cruise History Application Contract

## Implementation Prompt

Implement **Step 2 only** from:

```text
docs/AI Playbook/037 - Cruise History and Price Tracking.md
```

Prompt 037a is complete and committed as `a868133`.

Do not implement Steps 3–7.

---

## Required Reading

Read these files in order before changing code:

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/037 - Cruise History and Price Tracking.md`
5. `docs/Codex Prompts/037a - Cruise History Domain.md`, including Results and
   Lessons Learned
6. all files under `KrytenAssist.Core/Cruises/`
7. all existing Application persistence abstractions and use-case conventions
8. existing Application contract tests under
   `KrytenAssist.Avalonia.Tests/Application/`

Do not begin implementation until the 037a sailing key, observation fingerprint,
comparable-price and summary contracts are understood.

---

## Goal

Create the provider-independent Application boundary and use cases for recording
and querying Cruise history.

This step owns:

- an Application-owned observation-history repository abstraction
- an immutable repository history record
- an atomic record-operation contract
- explicit first, changed and already-current repository outcomes
- `RecordCruiseObservation` orchestration
- `GetCruiseHistory` orchestration
- `ListCruiseHistories` orchestration
- controlled success, not-found, cancelled and failed results
- exact use of 037a identity, fingerprint and history analysis
- deterministic Application tests with hand-written repository fakes

This step does **not** own:

- EF Core or SQLite
- persistence entities or configurations
- migrations
- repository implementation
- transactions or database constraints
- dependency injection
- ViewModels or views
- `Record Observation` UI
- application startup composition
- ratings, notes or preferences
- monitoring, alerts or booking

Do not create Infrastructure or Avalonia production placeholders.

---

## Allowed Changes

Create or modify production files only inside:

```text
KrytenAssist.Application/Cruises/
KrytenAssist.Application/Abstractions/Persistence/
```

Place the repository interface under the existing Application persistence
abstractions unless the repository conventions demonstrate a clearer equivalent
location.

Create or modify tests only inside:

```text
KrytenAssist.Avalonia.Tests/Application/Cruises/
```

The existing Avalonia test project already references Application through the
`KrytenApplication` alias and Core directly. Use those references; do not add a
new test project or package.

Update this prompt after implementation:

```text
docs/Codex Prompts/037b - Cruise History Application Contract.md
```

Do not modify:

- Core production types
- Infrastructure
- Avalonia production code
- API
- solution/project references
- Playbook, Roadmap, Backlog or Session Handovers

Production changes to Core are permitted only if a required deterministic test
exposes a genuine 037a contract defect. Any correction must be minimal, tested
and reported.

Do not stage, commit, push, discard or overwrite Robin's work.

---

## Architecture Boundary

Preserve this dependency direction:

```text
KrytenAssist.Core
        ↑
KrytenAssist.Application
```

Application may consume:

- `CruiseObservation`
- `CruiseSailingKey`
- `CruiseObservationFingerprint`
- `CruisePriceHistoryAnalyzer`
- `CruisePriceHistorySummary`
- other Core/BCL types

Application must not consume or expose:

- EF Core
- SQLite
- `DbContext`
- persistence entities
- database-generated identifiers as product identity
- migrations
- Avalonia
- browser, DOM or JavaScript types
- TUI payload types
- HTTP
- connection strings

The repository abstraction belongs to Application. Its later SQLite
implementation belongs to Infrastructure.

---

## Use-Case-Shaped Repository Contract

Create an abstraction such as:

```text
ICruiseObservationRepository
```

Do not copy an EF Core CRUD surface.

The repository exists to support exactly these operations:

```text
record one captured observation atomically
get one sailing/source history
list all recorded sailing/source histories
```

Avoid generic methods such as:

```text
Add
Update
Delete
GetAllEntities
SaveChanges
IQueryable
```

Do not expose transaction or tracking APIs.

### Required Repository Operations

The exact method names may follow project conventions, but the contract must
support:

```text
RecordAsync(
    CruiseSailingKey sailingKey,
    CruiseObservationFingerprint fingerprint,
    CruiseObservation observation,
    CancellationToken)

GetAsync(
    CruiseSailingKey sailingKey,
    string? normalizedRetailSourceId,
    CancellationToken)

ListAsync(CancellationToken)
```

Returning `Task` or `Task<T>` is required. Use async throughout.

### Atomic Recording Contract

`RecordAsync` must represent one atomic repository operation.

Its future Infrastructure implementation will be responsible for transaction and
concurrency behavior. Do not define a read-then-write sequence that requires the
Application service to:

1. fetch latest observation
2. compare it
3. insert separately

That sequence would allow concurrent duplicate histories or snapshots.

The repository must receive the already-defined Core sailing key and meaningful
fingerprint and return one of these semantic states:

```text
FirstObservationRecorded
ChangedObservationRecorded
AlreadyCurrent
```

Use a small Application-owned enum with equivalent names.

The repository result must include the complete updated history record so the
Application service can calculate and return an exact summary without another
database round trip.

### Repository Responsibility Boundary

Application defines what the supplied key and fingerprint mean.

The repository implementation later owns:

- finding or creating the canonical sailing/source history atomically
- comparing the latest stored fingerprint with the supplied fingerprint
- inserting a first or changed snapshot
- suppressing an identical snapshot
- advancing `LastSeenAt` for an already-current observation
- returning the resulting complete history

Infrastructure must not recreate 037a normalization rules from raw strings.

---

## Immutable Repository History Record

Create a provider-independent immutable Application model representing one
canonical sailing and one retail-source history loaded from persistence.

A name such as:

```text
CruiseRecordedHistory
```

is preferred.

It should contain:

- `CruiseSailingKey`
- `CruiseSource?`
- exact `LastSeenAt`
- nonempty ordered `IReadOnlyList<CruiseObservation>`

### Invariants

The constructor must:

- reject null sailing key
- reject null observations collection
- reject an empty observations collection
- reject null observation entries
- defensively copy the supplied collection
- order observations deterministically using the 037a history semantics, or
  require/document an ordering that is validated
- require every observation to match the supplied sailing key
- require every observation to match the supplied retail-source id, including
  consistently absent source
- require `LastSeenAt` not to precede the latest stored `ObservedAt`

Source display-name changes may be meaningful observation changes while source
identity remains based on normalized source id.

Do not include:

- database row ids
- EF entities
- mutable collections
- UI strings
- ratings or notes

### Summary Calculation

Provide a focused way to calculate `CruisePriceHistorySummary` from the record's
observations using `CruisePriceHistoryAnalyzer`.

The summary's `LastObservedAt` remains the latest stored snapshot timestamp.
`LastSeenAt` remains separate Application history metadata because an identical
recapture may advance it without adding a snapshot.

Do not alter Core summary semantics to disguise this distinction.

---

## Repository Record Result

Create a small immutable Application result returned by the repository's atomic
record operation.

A name such as:

```text
CruiseObservationRepositoryRecordResult
```

is acceptable.

It must contain:

- repository record state
- complete updated `CruiseRecordedHistory`

It must reject invalid enum values or null history.

Do not put user-facing error messages or persistence exceptions in this result.

---

## RecordCruiseObservation Use Case

Create an Application service such as:

```text
RecordCruiseObservation
```

Inject through its constructor:

- `ICruiseObservationRepository`
- `CruisePriceHistoryAnalyzer`

Do not instantiate Infrastructure or resolve services internally.

### Execute Contract

Expose one async method accepting:

- a complete `CruiseObservation`
- optional `CancellationToken`

The method must:

1. reject a null observation
2. honor pre-cancelled input without calling the repository
3. derive `CruiseSailingKey` through the 037a contract
4. derive `CruiseObservationFingerprint` through the 037a contract
5. call the repository exactly once
6. verify/analyze the returned complete history
7. map the repository state to a controlled record result
8. return the history summary and exact `LastSeenAt`

It must not:

- use the system clock
- replace the observation timestamp
- inspect TUI URLs or package ids
- perform its own read-before-write repository sequence
- retry automatically
- contact the source URL
- mutate the supplied observation

### Record Status

Create an Application-owned result status with:

```text
FirstObservationRecorded
ChangedObservationRecorded
AlreadyCurrent
Cancelled
Failed
```

A result type such as:

```text
CruiseObservationRecordResult
```

should expose:

- status
- whether a snapshot was inserted
- `CruisePriceHistorySummary?`
- `DateTimeOffset? LastSeenAt`
- safe controlled message for Cancelled/Failed when useful

The exact public shape may be refined, but contradictory combinations must not
be constructible.

Examples:

- First/Changed require summary and `SnapshotInserted == true`
- AlreadyCurrent requires summary and `SnapshotInserted == false`
- Cancelled/Failed contain no fabricated summary
- Failed never exposes exception text

Prefer static factories or a private validated constructor.

### Price Movement

Do not calculate price delta separately in Application.

Return the 037a summary/movement produced by `CruisePriceHistoryAnalyzer`.

An observation can be `ChangedObservationRecorded` while movement is
`Unchanged` when promotion or itinerary changed but comparable price did not.

### Cancellation and Failure

- Pre-cancellation returns Cancelled without repository access.
- Caller cancellation raised by the repository maps to Cancelled.
- Unexpected repository failure maps to Failed with a stable safe message.
- Do not expose exception type, message, SQL, path or connection details.
- Do not catch domain/programming validation exceptions caused before repository
  access and pretend they are persistence failures.

Do not use exceptions for `AlreadyCurrent`.

---

## GetCruiseHistory Use Case

Create an Application service such as:

```text
GetCruiseHistory
```

Inject:

- `ICruiseObservationRepository`
- `CruisePriceHistoryAnalyzer`

Expose an async method accepting:

- `CruiseSailingKey`
- optional retail source id
- optional `CancellationToken`

Normalize supplied retail source id using the same 037a rules. Do not create a
second incompatible normalization implementation in Application. If Core's
normalization is not publicly reusable, use a small Core value/factory already
present or make only the smallest justified 037a correction with regression
coverage.

### Query Result

Return a controlled result distinguishing:

```text
Found
NotFound
Cancelled
Failed
```

For Found, expose immutable details containing:

- complete `CruiseRecordedHistory`
- calculated `CruisePriceHistorySummary`
- exact `LastSeenAt`

NotFound must be an ordinary result, not an exception.

Cancelled and Failed must not fabricate history.

The repository must be called at most once.

---

## ListCruiseHistories Use Case

Create an Application service such as:

```text
ListCruiseHistories
```

It should load all local histories once and map them into immutable details with
037a summaries.

Return a controlled result distinguishing:

```text
Success
Cancelled
Failed
```

An empty repository is a successful empty result.

### Deterministic Ordering

Order returned histories by:

1. departure date ascending
2. latest stored observation timestamp descending for equal departure dates
3. canonical operator id ordinal
4. canonical ship name ordinal
5. duration ascending
6. normalized retail source id ordinal, with absent source handled consistently

Do not use the system clock to remove or reorder past sailings.

Return a read-only defensive collection.

One malformed repository history should become a controlled Failed result rather
than returning a partially fabricated list.

---

## Public Text Normalization Boundary

037a intentionally owns identity/fingerprint normalization inside Core.

037b must not duplicate whitespace/casing algorithms in multiple Application
services.

For retail-source query identity, prefer one of these incremental options:

1. accept a `CruiseSource?` rather than raw source id
2. introduce a small public Core factory/value for normalized retail-source id
3. expose the smallest focused normalization behavior from an existing 037a
   value type

Choose the smallest option consistent with existing contracts.

Do not make the internal general-purpose `CruiseHistoryText` class public merely
for convenience.

Document the choice in Results.

---

## Test Project and Aliases

Use the existing test project:

```text
KrytenAssist.Avalonia.Tests/KrytenAssist.Avalonia.Tests.csproj
```

Application is referenced through:

```csharp
extern alias KrytenApplication;
```

Use focused aliases for new Application types. Core Cruise types can use the
existing direct Core reference.

Do not remove or weaken aliases.

Do not add Infrastructure to these tests beyond its existing project reference;
037b tests must not resolve or instantiate Infrastructure types.

---

## Hand-Written Repository Fake

Create a focused test-only fake implementing `ICruiseObservationRepository`.

It should support:

- recording exact call count
- recording exact key, fingerprint, observation and token
- returning a supplied repository result
- returning a supplied history from Get
- returning supplied histories from List
- returning empty results
- throwing deterministic exceptions
- throwing `OperationCanceledException` for the caller token

Do not implement an in-memory database or duplicate the future SQLite repository.

Keep fake behavior explicit per test.

Do not use a mocking library.

---

## Record Use-Case Tests

Cover:

- null observation guard
- null repository/analyzer constructor guards
- pre-cancelled token returns Cancelled and never calls repository
- exact sailing key supplied to repository
- exact fingerprint supplied to repository
- exact observation instance supplied to repository
- exact cancellation token supplied
- repository called exactly once
- first observation maps to First and snapshot inserted
- changed observation maps to Changed and snapshot inserted
- identical observation maps to AlreadyCurrent without snapshot inserted
- summary and `LastSeenAt` come from returned updated history
- changed promotion with unchanged price produces Changed plus Unchanged trend
- repository caller cancellation maps to Cancelled
- unexpected repository exception maps to safe Failed
- failure message does not expose exception details
- no system clock or source access occurs

Use exact fixed non-zero-offset timestamps.

---

## Recorded History Model Tests

Cover:

- valid construction
- exact sailing key/source/last-seen preservation
- defensive observation copy
- deterministic observation ordering
- null and empty guards
- null observation entry rejection
- mixed sailing rejection
- mixed retail-source rejection
- consistently absent source support
- `LastSeenAt` before latest observation rejection
- `LastSeenAt` after an unchanged recapture acceptance
- exact 037a summary calculation

Do not test EF tracking or serialization.

---

## Get Use-Case Tests

Cover:

- null key guard
- exact key and source identity passed to repository
- Found returns exact history, summary and `LastSeenAt`
- NotFound is controlled and contains no history
- pre-cancellation avoids repository access
- repository cancellation maps to Cancelled
- repository failure maps to safe Failed
- repository called at most once

Include source-present and source-absent histories.

---

## List Use-Case Tests

Cover:

- empty repository returns successful empty read-only collection
- multiple histories are summarized exactly
- deterministic departure/latest/key/source ordering
- past sailings are retained
- input repository collection mutation cannot alter returned result
- pre-cancellation avoids repository access
- repository cancellation maps to Cancelled
- repository failure maps to safe Failed
- malformed returned history cannot produce a partial success
- repository called exactly once

No test may use the current date to decide whether a sailing is included.

---

## Repository Contract Tests

Add focused tests for Application result/model factories and invariants:

- repository state enum contains only the three atomic storage states
- repository result rejects null history
- record result cannot combine failure with a summary
- snapshot-inserted values match status
- query Found requires details
- query NotFound/Cancelled/Failed cannot carry details
- list Success exposes a defensive read-only collection
- list Cancelled/Failed exposes no fabricated items
- unknown enum values are rejected by public factories/constructors

Prefer unrepresentable invalid states over a large validation test surface.

---

## Offline and Deterministic Requirements

All 037b behavior must be pure orchestration over Core and a repository
abstraction.

The following are prohibited:

- network access
- browser invocation
- source URL retrieval
- file access
- SQLite
- EF Core
- system clock
- random ids
- delays, retries or polling
- static mutable state
- service location

Tests must prove inputs and returned repository records determine every output.

---

## Error Messages

Application may provide stable safe messages for later presentation.

Suggested meanings:

```text
Cancelled: Recording the cruise observation was cancelled.
Failed:    The cruise observation could not be recorded locally.
```

Query failures should similarly mention local history without exposing technical
details.

Do not mention:

- SQL
- EF Core
- SQLite
- connection strings
- file paths
- exception types

Exact wording may follow current application conventions, but tests should prove
technical exception details are not leaked.

---

## Production Corrections

The expected work is additive inside Application.

If 037b reveals a genuine 037a limitation:

1. add a failing Core regression test
2. make the smallest Core correction
3. rerun Core tests
4. record the correction explicitly

Do not redesign 037a types or move history orchestration into Core for
convenience.

---

## Required Commands

Run focused Application tests through the existing test project. A filter is
optional if the chosen namespaces allow it; otherwise run the full project:

```text
dotnet test KrytenAssist.Avalonia.Tests/KrytenAssist.Avalonia.Tests.csproj --no-restore
```

Build the complete solution:

```text
dotnet build KrytenAssist.sln --no-restore
```

Run the complete regression suite:

```text
dotnet test KrytenAssist.sln --no-build --no-restore
```

Report exact totals, failures, skipped tests, errors and warnings.

All tests must remain offline.

---

## Definition of Done

Prompt 037b is complete when:

- Application owns a use-case-shaped repository abstraction
- atomic record semantics are explicit in the repository contract
- repository results distinguish first, changed and already-current
- immutable recorded histories validate sailing/source consistency
- `LastSeenAt` remains distinct from latest stored observation time
- Record derives the exact 037a key and fingerprint
- Record calls the repository exactly once
- first/changed/already-current/cancelled/failed results are controlled
- Get returns Found/NotFound/Cancelled/Failed without exceptions for normal flow
- List returns deterministic read-only summaries and retains past sailings
- Core analyzer supplies all price summary and trend calculations
- cancellation and unexpected repository failures are safe
- no system clock, external work or source retrieval exists
- deterministic hand-written-fake tests pass
- complete solution builds
- complete regression suite passes
- no SQLite, migration, DI or UI work was added
- this prompt's Results and Lessons Learned are complete

Do not begin Prompt 037c.

Stop after Prompt 037b.

---

## Completion Report

Provide:

### Summary

Describe the Application history boundary and use cases.

### Atomic Repository Contract

Explain first/changed/already-current semantics and `LastSeenAt`.

### Use Cases and Results

Report Record, Get and List behavior, cancellation and safe failures.

### Files Modified

List every created and updated file.

### Production Corrections

Use `None` or report each verified correction.

### Build and Tests

Report exact commands and totals.

### Architecture and Scope

Confirm Application ownership and no Infrastructure/UI work.

---

## Results

> Complete during implementation.

### Status

Complete.

### Repository Contract

Added the Application-owned `ICruiseObservationRepository` with atomic Record,
Get and List operations. Record accepts the exact 037a sailing key, fingerprint
and observation evidence and returns the complete immutable history together
with FirstObservationRecorded, ChangedObservationRecorded or AlreadyCurrent.

### Record Use Case

`RecordCruiseObservation` derives the 037a identity and fingerprint, calls the
repository once, and returns controlled first, changed, already-current,
cancelled or failed results. Successful results expose the analyzer-owned price
history summary and repository-owned `LastSeenAt`; only first and changed states
report that a snapshot was inserted.

### Query Use Cases

`GetCruiseHistory` returns Found, NotFound, Cancelled or Failed. Found results
contain the immutable history and its Core analyzer summary. `ListCruiseHistories`
returns a defensive read-only collection, retains past and future sailings, and
orders them deterministically by departure date followed by stable tie-breakers.

### Retail Source Normalization Choice

Chose the typed-source boundary: Get accepts `CruiseSource?`, while repositories
persist and compare the canonical `RetailSourceId` supplied by the 037a
fingerprint. This keeps generic public text normalization out of Application and
avoids duplicating Core's source normalization rules.

### Files Created

- `KrytenAssist.Application/Abstractions/Persistence/ICruiseObservationRepository.cs`
- `KrytenAssist.Application/Cruises/CruiseHistoryDetails.cs`
- `KrytenAssist.Application/Cruises/CruiseHistoryListResult.cs`
- `KrytenAssist.Application/Cruises/CruiseHistoryListStatus.cs`
- `KrytenAssist.Application/Cruises/CruiseHistoryQueryResult.cs`
- `KrytenAssist.Application/Cruises/CruiseHistoryQueryStatus.cs`
- `KrytenAssist.Application/Cruises/CruiseObservationRecordResult.cs`
- `KrytenAssist.Application/Cruises/CruiseObservationRecordStatus.cs`
- `KrytenAssist.Application/Cruises/CruiseObservationRepositoryRecordResult.cs`
- `KrytenAssist.Application/Cruises/CruiseObservationRepositoryRecordState.cs`
- `KrytenAssist.Application/Cruises/CruiseRecordedHistory.cs`
- `KrytenAssist.Application/Cruises/GetCruiseHistory.cs`
- `KrytenAssist.Application/Cruises/ListCruiseHistories.cs`
- `KrytenAssist.Application/Cruises/RecordCruiseObservation.cs`
- `KrytenAssist.Avalonia.Tests/Application/Cruises/CruiseHistoryApplicationTestData.cs`
- `KrytenAssist.Avalonia.Tests/Application/Cruises/CruiseHistoryQueryUseCaseTests.cs`
- `KrytenAssist.Avalonia.Tests/Application/Cruises/CruiseRecordedHistoryTests.cs`
- `KrytenAssist.Avalonia.Tests/Application/Cruises/FakeCruiseObservationRepository.cs`
- `KrytenAssist.Avalonia.Tests/Application/Cruises/RecordCruiseObservationTests.cs`
- `KrytenAssist.Core.Tests/Cruises/CruiseObservationFingerprintOrderingTests.cs`
- `docs/Codex Prompts/037b - Cruise History Application Contract.md`

### Files Updated

- `KrytenAssist.Core/Cruises/CruiseObservationFingerprint.cs`

### Production Corrections

Added public deterministic comparison to `CruiseObservationFingerprint`. The
037a type already owned the canonical comparison key, but Application could not
use it to provide stable ordering when two observations have the same timestamp.
The correction exposes that existing ordering without changing fingerprint
identity or normalization and is covered by focused Core regression tests.

### Build

Passed: `dotnet build KrytenAssist.sln --no-restore`.

Build produced 0 errors and 5 existing NU1903 warnings for
`SQLitePCLRaw.lib.e_sqlite3` 2.1.11.

### Focused Tests

Passed:

- Application cruise-history filter: 13 passed, 0 failed, 0 skipped
- Core fingerprint filter: 7 passed, 0 failed, 0 skipped

### Complete Regression Suite

Passed: `dotnet test KrytenAssist.sln --no-build --no-restore`.

- Core: 104 passed, 0 failed, 0 skipped
- Avalonia: 288 passed, 0 failed, 0 skipped
- API: 9 passed, 0 failed, 0 skipped
- Total: 401 passed, 0 failed, 0 skipped

### Architecture and Scope Check

Application owns the use cases and persistence abstraction; Core remains the
owner of sailing identity, fingerprinting and price analysis. No Infrastructure,
SQLite, migration, dependency-injection, Avalonia UI, Roadmap, Backlog or session
handover changes were made.

### Notes

All tests are deterministic and offline. Repository behavior is represented by
a hand-written fake; no system clock, source retrieval or external I/O is used.

---

## Lessons Learned

> Complete after implementation.

- Atomic first/changed/already-current classification belongs in the future
  repository implementation so concurrent captures cannot split the decision
  from persistence.
- `LastSeenAt` must be stored separately from observation timestamps: an
  already-current capture advances recency without adding duplicate evidence.
- Returning complete immutable history from Record lets Application calculate a
  trustworthy summary without a second repository call.
- Stable equal-time ordering needs a domain-owned comparison because the
  fingerprint's canonical normalized values are intentionally encapsulated.
- Accepting `CruiseSource?` at the query boundary keeps raw source-text
  normalization out of Application while retaining provider independence.
