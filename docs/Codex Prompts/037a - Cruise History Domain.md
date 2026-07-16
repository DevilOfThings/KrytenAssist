# Codex Prompt 037a – Cruise History Domain

## Implementation Prompt

Implement **Step 1 only** from:

```text
docs/AI Playbook/037 - Cruise History and Price Tracking.md
```

Prompt 036 is complete. Prompt 037 has not otherwise started.

Do not implement Steps 2–7.

---

## Required Reading

Read these files in order before changing code:

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/037 - Cruise History and Price Tracking.md`
5. `docs/AI Playbook/036 - Cruise Discovery and Capture.md`
6. `docs/Session Handovers/2026-07-16 Session 017.md`
7. all existing types under `KrytenAssist.Core/Cruises/`
8. all existing tests under `KrytenAssist.Core.Tests/Cruises/`

Do not begin implementation until the operator/source distinction, sailing
identity, meaningful-change rules and comparable-price policy are understood.

---

## Goal

Create the provider-independent Core domain foundation required for durable
Cruise history and price tracking.

This step owns:

- canonical sailing identity
- deterministic identity normalization
- meaningful observation equality/fingerprinting
- strict comparable-price selection
- price-trend direction and exact delta
- immutable price-history summary values
- deterministic history analysis from recorded observations
- focused Core tests

This step does **not** own:

- repository interfaces
- record-observation use cases
- SQLite or Entity Framework
- migrations
- dependency injection
- ViewModels or views
- `Record Observation`
- persisted history lists
- ratings, notes or preferences
- alerts, monitoring or booking

Do not create placeholders for later steps.

---

## Allowed Changes

Create or modify files only inside:

```text
KrytenAssist.Core/Cruises/
KrytenAssist.Core.Tests/Cruises/
docs/Codex Prompts/037a - Cruise History Domain.md
```

Expected new production files may include:

```text
KrytenAssist.Core/Cruises/CruiseSailingKey.cs
KrytenAssist.Core/Cruises/CruiseObservationFingerprint.cs
KrytenAssist.Core/Cruises/CruisePriceTrendDirection.cs
KrytenAssist.Core/Cruises/CruisePriceMovement.cs
KrytenAssist.Core/Cruises/CruisePriceHistorySummary.cs
KrytenAssist.Core/Cruises/CruisePriceHistoryAnalyzer.cs
```

Equivalent names are acceptable when they describe the same focused concepts
more clearly.

Expected test files should mirror the production concepts under:

```text
KrytenAssist.Core.Tests/Cruises/
```

Do not add a project, project reference or NuGet package.

Do not modify Application, Infrastructure, Avalonia, API, Roadmap, Playbook,
Backlog or Session Handover files.

Do not stage, commit, push, discard or overwrite Robin's work.

---

## Existing Models to Preserve

Build incrementally on:

```text
CruiseProvider
CruiseSource
CruisePrice
CruiseOffer
CruiseSnapshot
CruiseObservation
```

Do not redesign their constructors or current invariants.

The new history types should consume these models through public properties.

Do not add persistence ids, EF attributes, mutable navigation collections or
database concerns to the existing models.

---

## Canonical Sailing Identity

Create an immutable value model representing one physical sailing.

A name such as:

```text
CruiseSailingKey
```

is preferred.

### Identity Components

The key must contain exactly the demonstrated stable identity values:

```text
operator/provider id
normalized ship name
departure date
duration in nights
```

It must not contain:

- provider offer id
- TUI package id
- title
- departure port
- itinerary summary
- retail source
- source reference
- observation timestamp
- price
- promotion

Those are observations about a sailing, not the physical sailing identity.

### Construction

Support direct construction from the four identity components and a focused
factory from `CruiseObservation` or its composed offer.

The public API should make the normalized identity values observable and
testable.

The factory must not mutate the supplied observation.

### Normalization

Normalize operator id and ship name deterministically:

- reject null, empty and whitespace-only values
- trim leading and trailing whitespace
- collapse every run of internal whitespace to one ordinary space
- use invariant lowercase for identity comparison/storage
- treat tabs, line breaks and repeated spaces equivalently
- preserve `DateOnly` exactly
- require duration of at least one night

Examples:

```text
" Marella "                   → "marella"
"Marella   Discovery 2"       → "marella discovery 2"
"MARELLA\tDISCOVERY\n2"      → "marella discovery 2"
```

Use ordinal value equality after normalization.

Do not use:

- `CurrentCulture`
- fuzzy matching
- edit distance
- AI matching
- aliases for known ships
- provider-specific constants
- random or database-generated identity

### Equality Boundaries

Two keys must be equal when only casing or whitespace differs.

They must be unequal when any canonical component changes:

- operator id
- ship
- departure date
- duration

Do not merge uncertain sailings.

---

## Meaningful Observation Fingerprint

Create an immutable provider-independent value representing the advertised facts
that determine whether a new historical snapshot is needed.

A name such as:

```text
CruiseObservationFingerprint
```

is preferred.

It should be created from a complete `CruiseObservation`.

### Values Included

Meaningful equality must include normalized forms of:

- canonical sailing key
- operator/provider display name
- title
- displayed ship name
- departure date
- duration
- optional departure port
- optional itinerary summary
- retail source id
- retail source display name
- complete prices as amount, currency and basis
- optional promotion summary

`CruiseObservation.Source` may be absent in existing domain construction. The
fingerprint must represent absence honestly and deterministically; do not invent
an `unknown` source.

### Values Excluded

The fingerprint must ignore:

- `ProviderOfferId`
- `SourceReference`
- `ObservedAt`
- object identity
- collection instance identity
- source price order

A change to an opaque retailer package identifier, tracking URL or observation
time alone must not create a changed fingerprint.

### Text Normalization

For comparison only:

- trim text
- collapse repeated whitespace
- compare case-insensitively using invariant/ordinal rules
- treat optional null consistently

Existing domain constructors already reject supplied blank optional values. Do
not weaken those models.

The original observation retains exact display text; the fingerprint is only
comparison evidence.

### Price Normalization

Meaningful price equality must:

- retain exact decimal amount
- retain normalized uppercase currency
- normalize basis using the same trim, whitespace-collapse and invariant case
  rules
- treat `null` basis distinctly from a supplied basis
- ignore price collection order
- ignore repeated equivalent price entries
- detect any changed amount, currency or basis

Do not compare serialized JSON or concatenate values without unambiguous
structure.

If a canonical string is used internally, it must use collision-safe component
boundaries. Prefer explicit structural equality.

### Equality Boundaries

Add focused tests proving:

- timestamp-only changes are equal
- provider-offer-id-only changes are equal
- source-reference-only changes are equal
- casing/whitespace-only changes are equal
- reordered prices are equal
- repeated equivalent prices do not create inequality
- changed title is unequal
- changed optional itinerary or promotion is unequal
- changed retail source is unequal
- changed price amount, currency or basis is unequal
- a changed sailing identity is unequal

---

## Comparable Price Selection

Historical price movement must compare prices with equivalent meaning.

Implement the policy inside one focused domain analyzer/service rather than
scattering string rules across models.

A name such as:

```text
CruisePriceHistoryAnalyzer
```

is preferred.

### Headline Price Policy

For one `CruiseSnapshot`:

1. Find prices whose currency is `GBP` and whose normalized basis is exactly
   `per person`.
2. If exactly one distinct matching price exists, select it.
3. If more than one distinct matching amount exists, selection is unavailable;
   do not guess.
4. If no GBP per-person price exists, select the only price in the snapshot only
   when it has a nonblank basis and is therefore honestly comparable.
5. If multiple non-preferred prices exist, selection is unavailable.
6. A price with `Basis == null` is retained in the snapshot but is unavailable
   as a comparable headline price.

Repeated equivalent prices count as one distinct candidate for selection.

Do not:

- divide total price by passenger count
- infer passenger count
- convert currencies
- treat `from`, `total` or `displayed price` as `per person`
- choose the cheapest among ambiguous bases
- round decimal values

The selected comparable value may reuse `CruisePrice` or use a small immutable
wrapper, but currency and normalized basis must remain available.

### Selection Tests

Cover:

- one GBP per-person price
- casing and whitespace variation in `per person`
- GBP per-person preferred over a total
- identical repeated GBP per-person entries
- conflicting GBP per-person amounts are unavailable
- one non-GBP price with an explicit basis
- one total price with an explicit basis
- one price with null basis is unavailable
- multiple prices without a GBP per-person candidate are unavailable

---

## Trend Direction and Movement

Create a provider-independent enum:

```text
FirstObservation
Lower
Higher
Unchanged
Unavailable
```

A name such as:

```text
CruisePriceTrendDirection
```

is preferred.

Create an immutable movement value that retains:

- direction
- previous comparable price when available
- current comparable price when available
- exact non-negative decimal delta when comparable

### Movement Rules

- no previous observation plus a comparable current price → `FirstObservation`
- current lower than comparable previous → `Lower`
- current higher than comparable previous → `Higher`
- equal comparable amounts → `Unchanged`
- missing price, different currency or different normalized basis → `Unavailable`

Delta is the absolute difference:

```text
988 → 949   delta 39, Lower
949 → 1020  delta 71, Higher
949 → 949   delta 0, Unchanged
```

For `FirstObservation`, delta should be absent rather than zero.

For `Unavailable`, delta must be absent.

Do not use negative deltas to encode direction.

Validate that movement objects cannot represent contradictory states.

---

## Price History Summary

Create an immutable summary value for one canonical sailing and one retail
source price series.

A name such as:

```text
CruisePriceHistorySummary
```

is preferred.

It should retain:

- canonical sailing key
- retail source when supplied
- first observed timestamp
- last observed timestamp
- observation count
- current comparable price when available
- lowest comparable price when available
- highest comparable price when available
- current price movement

The summary is a domain result, not an EF entity or UI-formatted string.

Do not add:

- currency symbols
- localized dates
- display colors or arrows
- Avalonia visibility flags
- database ids

### History Analysis Input

The analyzer should build a summary from a nonempty collection of recorded
`CruiseObservation` values.

It must:

- defensively enumerate/copy input as needed
- order observations by `ObservedAt` ascending using exact `DateTimeOffset`
- use deterministic tie-breaking when timestamps are equal
- require every observation to have the same canonical sailing key
- require every observation to have the same retail source identity, including
  consistently absent source
- reject mixed sailing/source input rather than silently filtering it
- preserve the earliest and latest exact timestamps
- count all supplied recorded snapshots

### Headline Series

Use the latest observation's comparable price to choose the active series:

```text
currency + normalized basis
```

For current, lowest and highest:

- current is the latest selected comparable price
- lowest/highest use earlier prices only when currency and normalized basis match
  the active series
- incompatible historical prices remain outside the calculation
- if the latest observation has no comparable price, all headline price values
  are absent and trend is `Unavailable`

For trend:

- one recorded observation with a comparable price is `FirstObservation`
- otherwise compare the latest observation to the immediately previous recorded
  observation
- if that previous observation has no matching comparable price, trend is
  `Unavailable`
- do not skip backwards across an incompatible previous observation to manufacture
  a trend

An unchanged comparable price may accompany a new snapshot because another
meaningful field changed.

### Summary Tests

Cover:

- one observation
- lower second observation
- higher second observation
- unchanged price with changed promotion
- three observations with correct current/lowest/highest
- input order independence
- exact first/last timestamp preservation including non-zero offsets
- observation count includes snapshots with unavailable comparable price
- latest unavailable price produces unavailable headline history
- incompatible immediately previous basis produces unavailable trend
- historical incompatible prices are excluded from min/max
- mixed sailing keys are rejected
- mixed retail sources are rejected
- all-absent retail source is supported consistently
- input collection mutation after construction does not alter the summary

---

## Equal Timestamp Ordering

Recorded observation timestamps should normally differ, but the analyzer must
remain deterministic when two timestamps are equal.

Use a documented provider-independent tie-breaker derived from meaningful
observation values, such as fingerprint ordering, rather than:

- input collection order
- object hash codes
- random values
- provider offer ids
- source-reference tracking parameters

Add a test proving the same summary is produced when equal-timestamp input order
is reversed.

Do not silently treat equal timestamps as duplicates; duplicate persistence
behavior belongs to later Application/Infrastructure steps.

---

## Validation and Immutability

All new public types must:

- reject null required arguments
- reject empty or whitespace-only required text
- reject invalid duration/count values
- use get-only properties
- expose collections as read-only defensive copies
- have deterministic value equality where equality is part of the contract
- use standard .NET exception types

Do not introduce:

- mutable setters
- static mutable state
- inheritance hierarchies
- provider subclasses
- service interfaces in Core
- custom exceptions without a demonstrated need
- implicit conversions

Small private normalization functions inside the owning domain type are
acceptable. Do not create a broad generic text-helper utility.

---

## Architecture Requirements

The new production code must reference only:

- Base Class Library types
- existing `KrytenAssist.Core.Cruises` types

It must not reference:

- Application
- Infrastructure
- Avalonia
- Entity Framework
- SQLite
- browser or JavaScript types
- TUI or Marella constants
- Skills
- HTTP
- OpenAI
- file-system APIs
- system clock

Do not modify `KrytenAssist.Core.csproj` unless required solely for normal file
inclusion, which SDK-style projects should not need.

---

## Test Conventions

Use the existing Core test project and conventions.

Use:

- xUnit
- FluentAssertions where it improves clarity
- Arrange, Act and Assert
- fixed dates and non-zero-offset timestamps
- small private factory methods
- exact decimal assertions
- parameter-name assertions for guards where practical

Do not use:

- mocking libraries
- dependency injection
- file or database access
- current date/time
- sleeps or retries
- shared mutable fixtures
- reflection
- snapshot tests
- random data
- Application, Infrastructure or Avalonia references

Representative fictional values may include:

```text
Operator: Marella Cruises / marella
Ship: Marella Example
Title: Atlantic Discovery
Departure: 18 December 2026
Duration: 7 nights
Source: TUI / tui
Prices: GBP 988 per person; GBP 1,975 total based on 2 sharing
Promotion: GBP 380 per person discount
Observed at: 16 July 2026 10:30 +01:00
```

Do not copy a live TUI package identifier or tracking URL into tests.

---

## Production Corrections

The expected work is additive.

If a focused test exposes a genuine defect in an existing Cruise model:

1. write the failing regression test
2. confirm the failure represents the 037a contract
3. make the smallest compatible correction
4. preserve existing public behavior where possible
5. report it explicitly in Results

Do not redesign existing models for convenience.

---

## Required Commands

Run focused Core tests:

```text
dotnet test KrytenAssist.Core.Tests/KrytenAssist.Core.Tests.csproj --no-restore
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

Prompt 037a is complete when:

- canonical sailing identity is immutable and deterministic
- identity ignores retailer package, source, price and tracking values
- meaningful observation fingerprints follow the documented inclusion/exclusion
  rules
- price order and irrelevant formatting do not create false changes
- meaningful advertised changes do create different fingerprints
- comparable-price selection never mixes unlike meaning
- trend direction and exact delta are correct
- history summary current/lowest/highest/count/timestamps are correct
- mixed sailing/source histories are rejected
- all new Core tests pass
- the complete solution builds
- the complete regression suite passes
- no repository, persistence, migration, DI or UI work was added
- this prompt's Results and Lessons Learned are complete

Do not begin Prompt 037b.

Stop after Prompt 037a.

---

## Completion Report

Provide:

### Summary

Describe the domain foundation added.

### Identity Rules

Report canonical components, normalization and excluded unstable values.

### Meaningful Change Rules

Report fingerprint inclusions, exclusions and price order behavior.

### Price Analysis

Report comparable-price selection, trend and summary semantics.

### Files Modified

List every created and updated file.

### Production Corrections

Use `None` or report each verified correction.

### Build and Tests

Report exact commands and totals.

### Architecture and Scope

Confirm Core-only dependencies and no later Prompt 037 work.

---

## Results

> Complete during implementation.

### Status

Complete.

### Identity Rules

`CruiseSailingKey` uses normalized operator id, normalized ship name, exact
departure date and duration. Required text is trimmed, internal whitespace is
collapsed and invariant lowercase is used. Provider offer id, title, source,
URL, observation time, price and promotion are deliberately excluded.

### Meaningful Change Rules

`CruiseObservationFingerprint` structurally includes normalized sailing,
operator display name, title, ship, optional port/itinerary, retail source,
distinct order-independent prices and promotion. It deliberately ignores
provider offer id, source reference, observation timestamp, input price order
and repeated equivalent price entries.

### Comparable Price and Trend

`CruisePriceHistoryAnalyzer` prefers one distinct GBP `per person` price, permits
one unambiguous fallback with an explicit basis and returns unavailable for
conflicting or basis-free prices. Summaries retain exact first/last timestamps,
count, current/lowest/highest matching-series prices and deterministic
First/Lower/Higher/Unchanged/Unavailable movement with non-negative decimal
delta.

### Files Created

- `KrytenAssist.Core/Cruises/CruiseHistoryText.cs`
- `KrytenAssist.Core/Cruises/CruiseSailingKey.cs`
- `KrytenAssist.Core/Cruises/CruiseObservationFingerprint.cs`
- `KrytenAssist.Core/Cruises/CruisePriceTrendDirection.cs`
- `KrytenAssist.Core/Cruises/CruisePriceMovement.cs`
- `KrytenAssist.Core/Cruises/CruisePriceHistorySummary.cs`
- `KrytenAssist.Core/Cruises/CruisePriceHistoryAnalyzer.cs`
- `KrytenAssist.Core.Tests/Cruises/CruiseHistoryTestData.cs`
- `KrytenAssist.Core.Tests/Cruises/CruiseSailingKeyTests.cs`
- `KrytenAssist.Core.Tests/Cruises/CruiseObservationFingerprintTests.cs`
- `KrytenAssist.Core.Tests/Cruises/CruisePriceHistoryAnalyzerTests.cs`
- `docs/Codex Prompts/037a - Cruise History Domain.md`

### Files Updated

None beyond this prompt's Results and Lessons Learned.

### Production Corrections

None. Existing Cruise model behavior did not require correction.

### Build

Passed:

```text
dotnet build KrytenAssist.sln --no-restore
```

0 errors. 7 existing warnings: five NU1903 warnings for the known SQLite package
advisory and two unused-event warnings in `MainWindowViewModel`.

### Focused Tests

Passed:

```text
dotnet test KrytenAssist.Core.Tests/KrytenAssist.Core.Tests.csproj --no-restore
```

103 passed, 0 failed, 0 skipped. This adds 32 focused Core tests to the previous
71-test baseline.

### Complete Regression Suite

Passed:

```text
dotnet test KrytenAssist.sln --no-build --no-restore
```

382 passed, 0 failed, 0 skipped:

- Core: 103
- Avalonia: 270
- API: 9

### Architecture and Scope Check

Verified. New production types reference only BCL and existing Core Cruise types.
No project/package reference changed. No Application contract, repository,
persistence, migration, dependency injection, browser or presentation behavior
was added. Tests use fixed fictional values and perform no external work.

### Notes

Equal timestamps use the fingerprint's deterministic meaningful comparison key
as a tie-breaker, so input collection order cannot change the selected current
snapshot. Mixed sailing keys and mixed retail-source ids are rejected rather
than silently filtered or merged.

---

## Lessons Learned

> Complete after implementation.

- A stable physical sailing key must exclude retailer package evidence; otherwise
  a price or booking-flow change can fragment one sailing's history.
- Meaningful equality is clearer and safer as explicit structural normalization
  than serialized-object equality or a hash whose semantics are undocumented.
- Price collection order and repeated equivalent entries are acquisition noise,
  while amount, currency and basis remain meaningful facts.
- Comparable history must be strict about basis. Returning unavailable is better
  than comparing a total with a per-person price or guessing passenger count.
- Selecting the active series from the latest observation keeps current, lowest
  and highest values internally comparable while preserving incompatible prices
  in their original snapshots for later use.
- Deterministic equal-timestamp ordering needs a meaningful value-derived
  tie-breaker; input order and object hash codes are not stable domain evidence.
