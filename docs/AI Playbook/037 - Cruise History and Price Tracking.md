# Prompt 037 – Cruise History and Price Tracking

## Goal

Persist explicitly accepted Cruise observations locally and turn repeated
captures of the same sailing into useful price history.

Prompt 037 should allow Robin to:

- record a successfully captured Cruise observation deliberately
- recognise the same sailing when it is captured again
- avoid storing duplicate snapshots when nothing meaningful changed
- retain changed advertised values as historical evidence
- revisit previously observed cruises after restarting Kryten
- see first observed, last observed, current, lowest and highest comparable price
- see the number of recorded changes
- see whether the comparable price moved up, down or stayed unchanged
- retain operator, retail source, source reference and observation time

Prompt 037 owns recorded observations and historical facts.

It does not own Robin's interest, rating, notes, favourites or preferences. Those
belong to Prompt 038.

---

## Why This Prompt Exists

Prompt 036 established the first usable Cruise research workflow:

```text
Discover → Inspect → Capture → Review
```

The review is intentionally session-only. Closing the browser or navigating to a
different page clears it, and restarting Kryten loses it completely.

That is the correct boundary for safe capture, but it does not yet support the
longer-term product goal:

> Help Robin remember what a cruise cost before, even when it was never booked.

Retailer promotions and prices change. A future offer is useful only when Kryten
can answer questions such as:

- Have we seen this sailing before?
- What did it cost the first time?
- Is today's price lower or higher?
- What is the lowest price we have recorded?
- Did only the promotion change, or did the actual price change?
- Which company advertised the observed price?

Prompt 037 introduces that durable factual memory.

---

## Product Journey

The broader Cruise journey remains:

```text
Discover → Inspect → Capture → Record → Revisit → Rate → Compare → Book externally
```

Prompt 037 owns:

```text
Record → Revisit factual history
```

Prompt 038 will own:

```text
Rate → Add notes → Express preferences
```

Later prompts may compare retailers, recommend cruises or monitor changes, but
Prompt 037 must not implement those behaviors early.

---

## User Language

Use **Record Observation** for the explicit Prompt 037 action.

Do not label it `Save Cruise` because Prompt 038 owns saved cruises and Robin's
evaluation of them.

The distinction should remain clear:

```text
Recorded observation = what a source advertised at a point in time
Saved cruise          = a cruise Robin has chosen to organise or evaluate
```

Recording a factual observation must not imply that Robin likes the cruise or
intends to book it.

---

## User Experience

### Record the Captured Observation

After a successful Prompt 036 capture review, display:

```text
[ Record Observation ]
```

Recording must occur only after Robin clicks the action.

During recording:

- disable duplicate actions
- show `Recording this observation...`
- keep the application responsive
- support cancellation where practical
- do not navigate or alter the TUI page
- do not perform any external request

On the first recording, show a controlled confirmation such as:

```text
Observation recorded.
This is the first price seen for this sailing from TUI.
```

When meaningful values changed, show:

```text
New observation recorded.
The comparable price is £39 lower than the previous recorded price.
```

When nothing meaningful changed, show:

```text
No new snapshot was needed.
This advertised observation matches the latest recorded values.
```

An unchanged recording may update `Last observed` or `Last seen` metadata, but it
must not increase the recorded snapshot count.

### Price History Summary

For a recorded sailing and retail source, display:

```text
Price History

First observed     16 July 2026
Last observed      23 July 2026
Current price      £949 per person
Lowest price       £949 per person
Highest price      £988 per person
Recorded changes   2
Trend              Down £39
Retail source      TUI
```

Use `Recorded observations` or `Recorded changes`, not `times checked`, because
identical recaptures do not create snapshots.

If no comparable headline price exists, retain the observation and show:

```text
Comparable price history is unavailable for this observation.
```

Do not compare unlike price bases.

### Revisit Recorded History

Cruise Discovery should expose a small local `Recorded Cruise History` area.

It should:

- load only local persisted data
- work without selecting or loading a cruise source
- perform no network access
- list previously observed sailings using concise provider-independent details
- allow selection of a sailing/source history summary
- survive application restart
- remain separate from the Prompt 038 saved-cruise experience

The first version does not require advanced search, filtering, deletion, export
or charts.

---

## Sailing Identity

Reliable history requires a stable answer to:

> Is this observation about the same real sailing?

Do not use the TUI package identifier as the sole history identity.

Package identifiers and retailer query parameters may change with:

- price
- airport
- passenger count
- cabin
- promotion
- package construction
- tracking or booking-flow state

Using those values as the history key could split one sailing whenever the offer
changes—the opposite of price tracking.

### Canonical Sailing Key

Introduce the smallest immutable provider-independent sailing identity required
for demonstrated captures.

The first canonical key should use:

```text
operator/provider id
+ normalized ship name
+ departure date
+ duration in nights
```

This is conservative and stable for a physical sailing: one ship cannot operate
two different cruises beginning on the same date for the same duration.

Title, departure port and itinerary should remain meaningful observed values but
must not be required identity components because retailers may describe the same
sailing differently or omit optional fields.

The provider offer identifier must still be retained in each observation as
source evidence, but a change to that identifier alone must not create a new
sailing history or snapshot.

### Normalisation

Identity normalisation should be deterministic and culture independent.

At minimum:

- trim leading and trailing whitespace
- collapse repeated internal whitespace
- compare operator ids case-insensitively
- compare ship names case-insensitively
- preserve the exact original display values in the observation
- never use the current culture, random values or system clock

Do not introduce fuzzy matching, AI matching or manual merge behavior.

When identity cannot be matched confidently, create a separate history rather
than combining unrelated sailings.

---

## Retail Source and Price Series

A physical sailing may later be advertised by more than one retailer.

The data model should therefore support:

```text
one sailing history
    ├── observations from TUI
    └── future observations from another retail source
```

Prompt 037 initially presents price history for the captured observation's retail
source. It must not compare one retailer's headline price directly with another
retailer's price yet.

A comparable price series is scoped by:

```text
canonical sailing
+ retail source id
+ currency
+ normalized price basis
```

This allows later cross-retailer comparison without corrupting today's history.

---

## Comparable Price Policy

Cruise snapshots can contain multiple prices, for example:

```text
GBP 988 per person
GBP 1,975 total based on 2 sharing
```

These prices are not interchangeable.

For Prompt 037 headline history:

1. prefer a GBP per-person price when one exists
2. otherwise use a single unambiguous price only when its currency and basis can
   be retained honestly
3. compare only prices with the same currency and normalized basis
4. never compare a total price with a per-person price
5. never infer division by passenger count
6. retain every captured price even when it is not selected as headline price
7. report unavailable comparable history rather than inventing a comparison

Basis normalisation may trim whitespace, collapse repeated whitespace and compare
case-insensitively. Do not silently reinterpret the meaning.

---

## Trend Semantics

Use a small provider-independent trend representation such as:

```text
FirstObservation
Lower
Higher
Unchanged
Unavailable
```

Trend compares the current comparable price with the previous recorded snapshot
in the same sailing/source/currency/basis series.

Examples:

```text
£988 → £949       Lower by £39
£949 → £1,020     Higher by £71
£949 → £949       Unchanged
one observation   First observation
unlike bases      Unavailable
```

`Unchanged` is still possible when another meaningful field—such as the
promotion—changed and caused a new snapshot while the comparable price remained
the same.

Do not implement prediction, percentage forecasting or claims that a price will
continue moving.

---

## Meaningful Change Detection

Store a new snapshot only when the advertised facts changed meaningfully.

Meaningful values include:

- title
- ship
- departure date
- duration
- departure port
- itinerary summary
- retail source
- complete price collection, including amount, currency and basis
- promotion summary

The following values alone must not create a new snapshot:

- observation timestamp
- `Last seen` timestamp
- provider offer identifier
- source-reference tracking parameters
- query-string ordering
- casing or whitespace differences with no semantic change
- the order of equivalent prices

The exact source reference and provider offer identifier should be retained with
the latest evidence even when a duplicate does not create a new snapshot, if the
chosen persistence design can update them without falsifying historical data.

### Normalised Snapshot Signature

Prefer an explicit provider-independent comparison/signature component rather
than comparing:

- serialized JSON
- database entity equality
- TUI payload text
- object reference identity

The signature must be:

- deterministic
- culture independent
- insensitive to irrelevant order and formatting
- based only on documented meaningful values
- testable without persistence or Infrastructure

Do not use a cryptographic hash as a substitute for defining the comparison
rules. A hash may be stored as an optimization only after canonical values and
collision-safe equality are defined.

---

## Recording Outcomes

Define a provider-independent result for the record operation.

It should distinguish at least:

```text
FirstObservationRecorded
ChangedObservationRecorded
AlreadyCurrent
Cancelled
Failed
```

The result may include:

- the updated history summary
- whether a snapshot was inserted
- the previous and current comparable price
- the price delta when comparable
- a controlled user-facing message or presentation state

Do not use exceptions as ordinary duplicate-flow control.

Persistence exceptions must become controlled Application or presentation
failures without exposing connection strings, SQL or file paths.

---

## Domain Model Direction

Prefer small immutable provider-independent types under Core for concepts such
as:

- canonical sailing identity
- price trend direction
- price-history summary values
- meaningful observation comparison

Do not put in Core:

- EF Core entities or attributes
- database ids required only by SQLite
- migrations
- SQL
- Avalonia state
- TUI selectors or package identifiers
- repository interfaces
- `DbContext`

Do not redesign the existing `CruiseProvider`, `CruiseOffer`, `CruiseSnapshot`,
`CruiseObservation` or `CruiseSource` unless a demonstrated Prompt 037
requirement cannot be represented incrementally.

---

## Application Boundary

Application should own:

- the observation-history repository abstraction
- record-observation use case
- query-history use case
- record outcomes
- provider-independent history query/result contracts
- orchestration of identity, change detection and persistence
- cancellation contracts

A repository abstraction may resemble:

```text
ICruiseObservationRepository
```

but its methods should be designed from the use cases rather than copying an EF
Core CRUD surface.

Required use cases are:

```text
RecordCruiseObservation
GetCruiseHistory
ListRecordedCruiseHistories
```

The exact class names may follow established Application conventions.

Application contracts must not expose:

- EF entities
- `DbContext`
- SQLite types
- database-generated keys as product identity
- TUI payloads
- browser or Avalonia types

---

## Persistence Architecture

Use the existing EF Core SQLite database and migration infrastructure.

Do not introduce:

- a second database
- a JSON history file
- an in-memory-only production store
- direct SQL from Avalonia
- a repository implemented in Core

Infrastructure should own:

- persistence entities or persistence-specific records where required
- EF Core configurations
- repository implementation
- transaction and concurrency behavior
- migration
- mapping between persisted values and provider-independent models
- DI registration

Extend:

```text
KrytenAssist.Infrastructure/Persistence/KrytenAssistDbContext.cs
```

through focused configurations and a real migration. Do not use
`EnsureCreated()` in production.

### Suggested Relational Shape

Prefer a normalized shape capable of retaining multiple observations and prices:

```text
CruiseHistories
    Id
    OperatorId
    NormalizedShipName
    DepartureDate
    DurationNights
    FirstObservedAt
    LastSeenAt

CruiseObservations
    Id
    CruiseHistoryId
    RetailSourceId
    RetailSourceName
    ProviderOfferId
    Title
    ShipName
    DepartureDate
    DurationNights
    DeparturePort
    ItinerarySummary
    PromotionSummary
    SourceReference
    ObservedAt
    NormalizedSignature or equivalent comparison evidence

CruiseObservationPrices
    Id
    CruiseObservationId
    Amount
    Currency
    Basis
```

Names may differ if the resulting model is clearer, but do not serialize the
entire observation into one opaque database column.

Use constraints and indexes for:

- canonical sailing uniqueness
- observation ordering
- foreign keys
- required values
- bounded strings
- duplicate/concurrency protection where appropriate

Store `DateTimeOffset` and `DateOnly` deterministically and preserve offsets where
the existing SQLite conventions require explicit conversion.

### Atomic Recording

Recording must be atomic.

Concurrent attempts to record the same first observation must not create:

- duplicate sailing histories
- duplicate snapshots
- orphaned prices

Use a transaction and database constraints. Do not rely only on a ViewModel
button being disabled.

---

## Local Startup and Migration

Prompt 037 may read and migrate the local SQLite database during application
composition according to the existing Infrastructure pattern.

It must not:

- contact an external service
- load TUI
- execute a Skill
- capture a page
- record an observation automatically

Migration failure should remain visible and controlled according to the existing
application startup approach. Do not silently delete or recreate Robin's
database.

---

## Presentation and MVVM

Extend the focused Cruise Discovery presentation rather than adding persistence
logic to code-behind.

ViewModel state should cover:

- whether a captured observation can be recorded
- recording in progress
- cancellation
- first/changed/unchanged outcome
- controlled persistence failure
- selected local history
- locally loaded history summaries
- empty-history state
- price trend presentation

The View should bind to commands and state only.

Code-behind must not:

- resolve the repository
- call EF Core
- compare observations
- calculate trends
- construct persistence entities

The existing native-browser code-behind should remain concerned only with the
browser boundary.

### Capture Review Lifecycle

A successful captured review may be recorded once or checked against history.

After successful recording:

- retain the review so Robin can see what was recorded
- disable or relabel the action while the observation remains current
- allow a newly captured changed observation to be recorded
- clear transient recording state when the capture review is cleared
- retain persisted history independently of browser Close or navigation

Closing the browser must clear the session review but must not delete recorded
history.

---

## History Query Behavior

History queries should return provider-independent read models suitable for
presentation.

For each sailing/source history provide enough data for:

- title
- operator
- ship
- departure date
- duration
- retail source
- first observed
- last observed or last seen
- current comparable price
- lowest comparable price
- highest comparable price
- observation count
- trend and delta
- latest source reference where useful

Ordering should be deterministic. A sensible default is:

1. future departure date ascending
2. most recently observed descending for equal dates
3. stable identity as a final tie-breaker

Do not silently discard past sailings; price evidence remains useful after the
departure date for later comparison. Presentation may distinguish past dates but
must preserve them.

---

## Deletion and Retention

Prompt 037 should not add deletion UI unless implementation reveals a compelling
data-safety requirement.

Historical observations are intended to remain even when Robin never books or
later decides the cruise is unsuitable.

Prompt 038 preference state must not delete factual price evidence.

Future data-management/export prompts may add deliberate deletion or retention
controls.

---

## Privacy and Security

Persist only the captured provider-independent Cruise evidence required for
history.

Never store:

- cookies
- session or local storage
- authentication tokens
- passenger names
- email addresses
- payment details
- browser history
- full HTML
- arbitrary visible page text
- tracking identifiers retained only for analytics

Source references should be normalized or sanitized so unnecessary tracking
parameters do not become historical identity or change signals.

Do not log opaque live package identifiers, connection strings, SQL statements
containing private data or Robin's database path in user-facing errors.

---

## Offline-First Requirements

Every Prompt 037 operation is local.

The following must perform no external work:

- resolving history services
- opening Cruise Discovery
- listing recorded histories
- selecting a recorded history
- calculating summaries and trends
- recording an already captured observation
- duplicate detection
- application and repository tests

Prompt 037 must not revisit the source URL to verify a stored price.

New price evidence still enters through Robin's explicit Prompt 036 browse and
capture workflow.

---

## Error and Cancellation Behavior

Handle:

- cancelled recording
- database unavailable
- migration or schema failure
- unique/concurrency conflict
- corrupt persisted values
- unsupported price comparison
- missing history

Use controlled messages and retain the captured review when recording fails so
Robin can retry.

Do not expose:

- exception stack traces
- SQL
- connection strings
- database paths
- EF Core type names

Cancellation must not leave a partially inserted history, observation or price
collection.

---

## Testing Strategy

All automated tests must be deterministic and offline.

### Core Tests

Cover:

- canonical sailing identity and normalization
- equality and non-equality boundaries
- meaningful observation comparison
- price collection order independence
- optional-value normalization
- comparable price selection
- first, lower, higher, unchanged and unavailable trends
- exact decimal delta behavior

### Application Tests

Use hand-written repository fakes to cover:

- first observation
- changed observation
- identical observation
- latest-evidence update without new snapshot
- exact history queries
- missing history
- cancellation
- controlled repository failure
- no system clock or external work

### Infrastructure Tests

Use SQLite in-memory or isolated temporary databases.

Cover:

- EF mappings and constraints
- real repository round trips
- observation and price ordering
- duplicate suppression
- atomic first insert
- changed snapshot insertion
- identical snapshot behavior
- multiple retail sources
- migration from the previous schema
- application restart through a new DbContext
- cancellation and rollback

Do not use the production database file.

### Avalonia Tests

Use ViewModels and deterministic services.

Cover:

- explicit Record Observation
- disabled and busy command state
- first/changed/already-current presentation
- cancellation and retry
- controlled persistence failure
- retained review on failure
- persisted history survives browser review clearing
- local empty and populated history states
- summary formatting
- no browser, Skill or network work during local history loading

Do not use UI automation, live TUI, sleeps or a real OS launcher.

---

## Explicitly Out of Scope

Prompt 037 must not implement:

- interest levels
- ratings
- personal notes
- favourites
- budgets
- preferred cabins
- preference learning
- retailer recommendations
- cross-retailer value ranking
- watch lists
- alerts or notifications
- scheduled capture
- background browsing
- unattended scraping
- automatic source refresh
- booking automation
- login or authentication management
- payment handling
- charts requiring a new visualization framework
- the complete Cruise Dashboard from Prompt 042

Do not redesign Prompt 036 extraction while implementing history.

---

## Implementation Steps

### Step 1 – 037a: Cruise History Domain

- define canonical sailing identity
- implement normalization rules
- define meaningful observation equality/signature
- define comparable price selection
- define price trend and history summary models
- add focused Core tests

### Step 2 – 037b: Cruise History Application Contract

- define repository abstraction from use cases
- define record and query contracts
- implement record-observation orchestration
- distinguish first, changed, already-current, cancelled and failed outcomes
- add deterministic Application tests with hand-written fakes

### Step 3 – 037c: Cruise History SQLite Persistence

- add normalized EF persistence entities and configurations
- extend `KrytenAssistDbContext`
- implement repository mapping and transactions
- add a real migration from the current schema
- register persistence through Infrastructure DI
- add SQLite round-trip, constraint and migration tests

### Step 4 – 037d: Observation Recording and History Queries

- complete atomic first/changed/duplicate recording behavior
- update last-seen evidence without adding duplicate snapshots
- implement list and detail history queries
- support multiple retail sources without mixing price series
- add concurrency, cancellation and restart-persistence tests

### Step 5 – 037e: Cruise History Presentation

- add explicit `Record Observation`
- show busy, cancellation, outcome and retry states
- display current history summary after recording
- add local Recorded Cruise History list/detail presentation
- preserve history when browser session review clears
- keep views passive and MVVM-driven

### Step 6 – 037f: Cruise History Tests

- complete domain, Application, Infrastructure and Avalonia coverage
- confirm all tests use isolated local persistence and no external work
- run migration and restart-persistence regression tests
- make only minimal corrections exposed by deterministic tests

### Step 7 – 037g: Cruise History Verification

- verify architecture and dependency direction
- verify identity and meaningful-change semantics
- verify SQLite schema, migration and transaction behavior
- verify offline startup and local history loading
- verify no Prompt 038 preference behavior was introduced
- build the complete solution
- run focused and complete test suites
- perform a manual record/restart/revisit workflow with Robin
- update this Playbook's Results and Lessons Learned
- update the Roadmap and create a Session Handover

---

## Acceptance Criteria

Prompt 037 is complete only when:

- recording occurs only after explicit user action
- a stable provider-independent sailing identity exists
- retailer package identifiers are retained as evidence but not used as the sole
  history identity
- first observation creates one sailing history and one snapshot
- meaningful changes create a new snapshot
- identical observations do not create duplicate snapshots
- last-seen metadata can advance without increasing the snapshot count
- prices of unlike currency or basis are never compared
- current, lowest, highest, count and trend are correct
- all original prices remain retained
- operator and retail source remain distinct
- source reference and observation timestamp are preserved
- recorded history survives application restart
- local history can be revisited without loading TUI
- persistence uses the existing EF Core SQLite database and a real migration
- recording is transactional and concurrency safe
- browser and TUI types do not enter persistence or shared contracts
- no cookies, HTML, credentials or personal booking data are stored
- all automated tests are deterministic and offline
- the complete solution builds and all tests pass
- manual record/restart/revisit verification is recorded
- Results and Lessons Learned below are complete

---

## Results

> Complete after implementation and verification.

### Status

Complete and verified on 17 July 2026.

Kryten now records explicitly accepted Cruise observations into durable local
history, creates snapshots only for meaningful advertised changes, calculates a
compatible price-history summary and revisits recorded cruises after restart
without loading TUI.

### Identity and Change Rules

`CruiseSailingKey` identifies a physical sailing by normalized operator id, ship
name, departure date and duration. Retail source is deliberately separate, so
the same sailing advertised by TUI and another retailer has independent history
and price series. Retail package/offer id, source reference, price, promotion and
observation timestamp are evidence rather than sailing identity.

`CruiseObservationFingerprint` canonicalizes advertised offer facts, prices,
promotion and source identity. Equivalent case/whitespace, price order and
duplicate price representations do not fabricate change. Meaningful offer,
price or promotion changes create a new chronological snapshot. An identical
current observation adds no snapshot but can advance last-seen and latest offer
evidence. Returning to a previously seen meaningful state after an intervening
state creates a new chronological snapshot.

Comparable price selection prefers one unambiguous GBP per-person price and
otherwise accepts only one price with an explicit basis. Unlike currency or
basis is never mixed. Current, lowest and highest use the current compatible
series, and trend compares with the immediately previous observation. First,
Higher, Lower, Unchanged and Unavailable remain distinct.

### Files Created

Core production and tests:

- `KrytenAssist.Core/Cruises/CruiseHistoryText.cs`
- `KrytenAssist.Core/Cruises/CruiseSailingKey.cs`
- `KrytenAssist.Core/Cruises/CruiseObservationFingerprint.cs`
- `KrytenAssist.Core/Cruises/CruisePriceTrendDirection.cs`
- `KrytenAssist.Core/Cruises/CruisePriceMovement.cs`
- `KrytenAssist.Core/Cruises/CruisePriceHistorySummary.cs`
- `KrytenAssist.Core/Cruises/CruisePriceHistoryAnalyzer.cs`
- the matching sailing-key, fingerprint, ordering and price-history Core tests

Application production and tests:

- `KrytenAssist.Application/Abstractions/Persistence/ICruiseObservationRepository.cs`
- Cruise History details, list/query/record results and statuses under
  `KrytenAssist.Application/Cruises/`
- `CruiseRecordedHistory`, `CruiseLatestEvidence`, `RecordCruiseObservation`,
  `GetCruiseHistory` and `ListCruiseHistories`
- the matching Application contract, use-case and hand-written repository tests

Infrastructure production and tests:

- normalized Cruise history, observation and price entities/configurations
- `KrytenAssist.Infrastructure/Persistence/CruisePersistenceConversions.cs`
- `KrytenAssist.Infrastructure/Persistence/SqliteCruiseObservationRepository.cs`
- migrations `20260717082520_AddCruiseHistoryPersistence` and
  `20260717090431_HardenCruiseHistoryRecording`, including designers
- schema/migration, round-trip, cancellation, concurrency, restart and hardening
  test fixtures under `KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/`

Avalonia production and tests:

- `KrytenAssist.Avalonia/DependencyInjection/DesktopPersistenceServiceCollectionExtensions.cs`
- `KrytenAssist.Avalonia/ViewModels/CruiseHistoryViewModel.cs`
- `KrytenAssist.Avalonia/ViewModels/CruiseHistoryItemViewModel.cs`
- desktop composition, ViewModel and cross-layer restart workflow tests

Documentation:

- `docs/AI Playbook/037 - Cruise History and Price Tracking.md`
- Codex prompts 037a–037g
- `docs/Session Handovers/2026-07-17 Session 018.md`

### Files Updated

- `KrytenAssist.Application/DependencyInjection.cs`
- `KrytenAssist.Infrastructure/DependencyInjection.cs`
- `KrytenAssist.Infrastructure/Persistence/KrytenAssistDbContext.cs`
- `KrytenAssist.Infrastructure/Persistence/Migrations/KrytenAssistDbContextModelSnapshot.cs`
- Cruise persistence entities, configurations and repository during 037d
  hardening
- `KrytenAssist.Avalonia/Program.cs`
- `KrytenAssist.Avalonia/ViewModels/CruiseBrowserFeasibilityViewModel.cs`
- `KrytenAssist.Avalonia/ViewModels/CruiseOfTheWeekViewModel.cs`
- `KrytenAssist.Avalonia/ViewModels/ShellViewModel.cs`
- `KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml`
- focused Core, Application, Infrastructure and Avalonia tests as recorded in
  prompts 037a–037f
- `docs/Roadmap.md`

### Database and Migration

The existing `KrytenAssistDbContext` now includes normalized `CruiseHistories`,
`CruiseObservations` and `CruiseObservationPrices` tables while retaining Prompt
Cards. `AddCruiseHistoryPersistence` introduced sailing/source uniqueness,
ordered prices, constraints and cascading aggregate ownership.
`HardenCruiseHistoryRecording` replaced history-wide fingerprint uniqueness with
positive per-history sequence uniqueness, retained a fingerprint lookup index
and added latest provider offer/reference/time evidence.

Tests prove migration from the initial Prompt Card schema, migration through
every checked-in version, 037c-to-037d data preservation, constraints, cascade,
transaction rollback, concurrent writers and complete provider/database
recreation. Recording uses an explicit transaction and at most three attempts
for demonstrated SQLite busy/locked or uniqueness concurrency conflicts.

Avalonia composes the existing Application and Infrastructure extensions and
uses a deterministic writable database at the platform Local Application Data
path under `KrytenAssist/krytenassist.db`. Tests always override this with
in-memory or uniquely named temporary databases.

### Build

Passed:

```text
dotnet build KrytenAssist.sln --no-restore
```

0 errors and 5 existing NU1903 warnings for the known
`SQLitePCLRaw.lib.e_sqlite3` 2.1.11 advisory.

### Tests

Focused verification passed:

- Core Cruise History group: 58 passed, 0 failed, 0 skipped
- Application/Infrastructure/Avalonia Cruise History group: 54 passed, 0
  failed, 0 skipped

Complete regression suite passed:

```text
dotnet test KrytenAssist.sln --no-build --no-restore
```

- Core: 105 passed, 0 failed, 0 skipped
- Avalonia: 336 passed, 0 failed, 0 skipped
- API: 9 passed, 0 failed, 0 skipped
- Total: 450 passed, 0 failed, 0 skipped

All Cruise History tests are offline and use fixed clocks, hand-written fakes,
in-memory SQLite or unique temporary file databases. They do not contact TUI,
open a browser or access Robin's desktop database.

### Manual Verification

Passed on 17 July 2026. Robin captured a Cruise, chose Record Observation,
confirmed that its price appeared in Recorded Cruise History, restarted Kryten
and confirmed that the history remained available.

037f later changed only one-night wording and cancellation/replacement command
generation. Normal successful record, persistence and restart loading did not
change, so the existing manual evidence remains valid.

### Git Commits

- `a868133` – 037a Cruise History Domain
- `b776b9a` – 037b Cruise History Application Contract
- `a3e3038` – 037c Cruise History SQLite Persistence
- `394f5b3` – 037d Observation Recording and History Queries
- `74078c1` – 037e Cruise History Presentation
- `5bb399a` – 037f Cruise History Tests

037g verification documentation remains uncommitted for Robin to review.

---

## Lessons Learned

> Complete after implementation and verification. Do not begin Prompt 038 until
> this section and Results have been updated.

- Physical sailing identity must remain smaller and more stable than retailer
  evidence; retail source then provides the correct boundary for independent
  advertised price series.
- Fingerprints answer whether advertised facts changed, while observation time,
  latest offer reference and last-seen metadata answer when and where equivalent
  evidence was seen. Combining those concerns would either lose evidence or
  create duplicate snapshots.
- Price history is trustworthy only when ambiguity is represented explicitly and
  unlike currency or basis is never compared.
- A first persistence migration can establish normalized storage, but realistic
  chronological return-to-prior-state and latest-evidence rules may require a
  focused hardening migration before presentation is exposed.
- SQLite concurrency correctness needs database constraints, transactions,
  separate contexts in tests and bounded retry for demonstrated conflict codes;
  in-memory orchestration alone cannot prove it.
- The strongest restart proof recreates the complete desktop service provider
  against an isolated file, not merely a repository instance.
- Capture review and persisted history are separate lifecycle states. Browser
  navigation may clear the former but must never clear the latter.
- The neutral current price model is sufficient for factual history but not for
  explaining original price, discounted price, per-person discount and an extra
  booking-level discount. That richer model should be designed explicitly later.
- Prompt 038 can now add Robin's ratings, notes and preferences without mixing
  subjective evaluation into provider observations.
