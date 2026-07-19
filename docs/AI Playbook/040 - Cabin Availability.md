# Prompt 040 – Cabin Availability

## Goal

Record and revisit honest, source-specific cabin availability evidence for saved
Marella sailings, then identify meaningful changes affecting Robin's preferred
cabin types.

Prompt 040 must not imply live or unattended monitoring. Kryten knows only what
the currently displayed trusted page explicitly proves when Robin captures or
records it.

---

## Why This Prompt Exists

Prompt 037 records price/promotion observations. Prompt 038 stores preferred
cabin types. Prompt 039 deliberately excludes cabin matching because current
Cruise observations contain no trustworthy cabin availability set.

TUI search cards may show text such as:

```text
1 x Inside Cabin
(Cheapest available)
```

That is useful positive evidence for the exact search, but it is not evidence
that other cabin categories are unavailable. TUI may expose richer category
evidence later in an itinerary/cabin-selection flow. Prompt 040 must preserve
that distinction rather than infer inventory from absent text.

---

## Evidence Boundary

Cabin availability belongs to this identity:

```text
Sailing + Retail Source + Search/Occupancy Context + Evidence Time
```

At minimum, context must retain the facts that can materially change the result
and are explicitly known, including:

- adult count
- child count and supplied child ages where present
- fly-cruise versus cruise-only/package mode where demonstrated
- departure airport when the result is a flight package
- cabin quantity where visible

Unknown context stays unknown. Do not assume two adults merely because that is
Robin's common search, and do not compare incompatible contexts as one series.

Cabin categories remain the existing Core-owned set:

```text
Inside | Outside | Balcony | Suite | Solo
```

Solo is retained as Robin's existing category even though a retailer may model
occupancy and physical cabin class separately. Adapter mappings must use only
explicitly demonstrated retailer wording; ambiguous labels remain unmapped.

For each category, evidence is:

```text
Available | Unavailable | Unknown
```

- `Available` requires explicit quoteable/available/selected evidence.
- `Unavailable` requires explicit category-specific unavailable/sold-out
  evidence.
- omission, an unvisited chooser, or an expired package means `Unknown`.
- an itinerary-level `All gone` page may describe the whole searched package,
  but does not prove category-specific unavailability and must not manufacture
  five Unavailable values.

Evidence also records whether the source presented a complete category set or
only partial positive evidence.

---

## Product Experience

The intended journey is explicit:

```text
Browse trusted TUI evidence
        ↓
Capture currently displayed cabin evidence
        ↓
Review source, sailing, context and category states
        ↓
Record Cabin Observation
        ↓
Revisit local Cabin Availability history
        ↓
Evaluate preferred-cabin transitions for Shortlisted cruises
```

Capture and recording remain separate actions. No cabin evidence is recorded
merely because a Cruise price card was captured or an itinerary page was opened.

The UI must show:

- source and evidence time
- sailing identity
- search/occupancy context
- each explicitly evidenced category and state
- whether evidence is partial or complete
- Unknown categories honestly
- latest versus previous meaningful availability change
- a trusted source reference

Avoid `available now`, `live availability`, `we monitor`, or inventory-count
language unless the evidence explicitly supports it. Prefer `available when
recorded for this search`.

---

## Ownership and Architecture

Cabin evidence is a new factual timeline. It is not:

- a field on `SavedCruise`
- part of personal `CruisePreferences`
- a mutation of price `CruiseObservation`
- provider inventory owned by Core
- inferred from an alert

Use a provider-independent Core observation and Application-owned repository.
TUI DOM/JavaScript/payload types remain at the Avalonia/Infrastructure boundary.

Cabin observations use value identity to the sailing/source/context and have no
aggregate ownership or cascade relationship to Cruise History, Saved Cruises,
preferences or alerts. Removing personal state must not delete factual cabin
evidence. Removing Cabin History must not change saved or price history.

---

## Meaningful Change and Deduplication

Build a stable fingerprint from:

- version
- sailing identity
- normalized retail source id
- normalized search/occupancy context
- ordered explicit category states
- partial/complete coverage marker
- bounded source evidence identity

Observation time is not part of meaningful-state equality. Recording equivalent
evidence advances last-seen/latest-reference metadata without adding a duplicate
snapshot, following the established Cruise History pattern.

Compare only one sailing/source/compatible-context series. A transition is
meaningful when an explicitly known category changes state or evidence coverage
changes in a way that adds/removes a supported fact. Unknown must not be treated
as Unavailable.

---

## Preferences and Alerts

Preferred cabins are an OR set: any explicitly Available preferred category
satisfies the cabin preference. Cabin matching then combines with other
configured saved criteria using AND:

```text
month matches (when configured)
AND budget matches (when configured)
AND any preferred cabin is explicitly Available (when configured)
```

If cabin preferences exist but compatible current cabin evidence is absent or
only Unknown, saved criteria remain not known/met. Prompt 040 versions the
criteria fingerprint and removes the current `CabinPreferencesUnavailable`
limitation only when real compatible evidence is supplied.

Do not backfill historic Saved Criteria alerts from newly introduced cabin
logic. Evaluate future explicitly recorded evidence and explicit preference/save
boundaries.

Prompt 040 extends the in-app alert model with a provider-independent Cabin
Availability change type as a separate event from Saved Criteria. The useful
notifications are:

- a preferred cabin changes explicitly from Unavailable to Available for a
  Shortlisted saved cruise and compatible context
- a previously Available preferred cabin becomes explicitly Unavailable

First-seen Available evidence is displayed in Cabin History but is not phrased
as `became available`. Unknown-to-Available is new knowledge, not proof of an
inventory transition, and creates Cabin History only.

Prompt 040 adds a separate typed `CabinAvailability` alert for explicit
preferred-category Unavailable-to-Available and Available-to-Unavailable
transitions on Shortlisted saved cruises. Saved Criteria remains an independent
alert meaning and may also transition when all configured month, budget and
cabin groups become Met.

---

## TUI Acquisition Strategy

Start only with page shapes demonstrated through Robin's explicit browser use.

### Search Result Card

The current modern results card can provide partial positive evidence such as
one selected/cheapest cabin. It may create:

```text
Inside = Available
Outside/Balcony/Suite/Solo = Unknown
Coverage = Partial
```

only when the card wording and search context are explicit.

### Cabin Selection Page

A richer adapter may capture multiple categories only after a real page is
demonstrated. Disabled, sold-out and selectable states must be inspected and
mapped exactly. Do not guess selectors or automate progress through booking.

Navigation remains manual. The fixed script may read the current page but must
not click a cabin, submit passengers, advance booking, fetch private endpoints,
or expose cookies/session state.

### Expired or Sold-Out Package

During analysis on 19 July 2026, an incomplete direct itinerary reconstruction
showed `All gone`. This proves only that the reconstructed searched package was
not available. It is not valid category evidence and must produce a controlled
unsupported/incomplete capture rather than category states.

---

## Persistence

Use normalized local SQLite storage for:

- cabin series keyed by sailing/source/context
- chronological meaningful cabin observations
- ordered category-state children
- latest evidence/last-seen metadata

Enforce uniqueness for series identity, sequence and observation fingerprint.
Preserve exact timestamp offsets and deterministic UTC ordering. Use
transactions, cancellation and bounded retry for demonstrated SQLite
busy/unique conflicts. No production or test access to Robin's database.

---

## Implementation Sequence

### 040a – Cabin Availability Experience and Evidence Contract

- agree exact evidence semantics and context identity
- agree partial versus complete coverage and Unknown rules
- agree explicit capture/record/revisit workflow
- agree preferred-cabin matching and alert transition language
- inspect demonstrable TUI evidence without implementing production code

### 040b – Cabin Domain and Application Contracts

- add immutable provider-independent observation/context/state models
- add fingerprints, compatible-series and meaningful-change rules
- add Application repositories, results and record/query use cases
- extend saved-criteria evaluation contract with optional cabin evidence
- define any Cabin Availability alert contract without provider leakage

The agreed analysis uses immutable observations with exactly one state for every
existing cabin type, explicit Partial/Complete coverage and a normalized search
context that preserves known versus unknown occupancy/package facts. A stable
series key identifies sailing/source/context; a separate state fingerprint
contains only coverage and ordered category states, deliberately excluding
time, retailer evidence key and reference so refreshed equivalent evidence does
not create false history.

Application owns cabin capture and persistence abstractions plus controlled
record/get/list/orchestration results. Pure Core policies distinguish explicit
Available/Unavailable transitions from Unknown knowledge changes. Prompt 040b
also versions Saved Criteria for tri-state preferred-cabin matching and extends
alerts/settings with a default-enabled typed Cabin Availability transition.
SQLite reconstruction compatibility is preserved until 040c. See the 040b
Codex prompt for the complete model, identity, policy and test contract.

### 040c – SQLite Cabin Persistence

- add normalized series/observation/category tables and migration
- implement repository, ordering, deduplication and concurrency
- verify restart and independence from History/Saved Cruises/alerts

### 040d – TUI Cabin Evidence Capture

- extend fixed bounded capture for explicitly demonstrated result-card evidence
- add a richer cabin-selection adapter only if its page is demonstrated
- preserve trusted source, read-only behavior and controlled incomplete results
- add offline fixtures and safety tests

### 040e – Recording and Preference Evaluation

- add explicit Record Cabin Observation action
- evaluate preferred-cabin transitions only after committed evidence
- preserve recording success if alert/criteria evaluation fails
- trigger supported saved-criteria reevaluation at explicit boundaries

### 040f – Cabin Availability Presentation

- add local latest/history presentation to the Cruise workspace
- show context, partial/complete evidence and Unknown states
- show preferred-cabin matches without overstating inventory
- add controlled loading/empty/retry/stale-result states

### 040g – Tests and Verification

- audit architecture, schema ownership and independence
- verify compatible-context comparison, restart and concurrency
- verify capture safety and preference/alert transitions
- run complete offline suite and manual desktop checklist
- update Results, Lessons Learned, Roadmap and session handover

---

## Deterministic Acceptance Scenarios

1. A card explicitly showing Inside as cheapest available records Inside
   Available and all other categories Unknown with Partial coverage.
2. Missing cabin text records no cabin observation.
3. An `All gone` itinerary does not manufacture category unavailability.
4. Equivalent evidence advances last-seen without a duplicate snapshot.
5. An explicit category Available-to-Unavailable transition creates history.
6. Different retailer or occupancy/search context forms an independent series.
7. Preferred cabins use OR; month/budget/cabin groups combine with AND.
8. Cabin-only preferences require explicit compatible Available evidence.
9. Removing a Saved Cruise or price History does not remove cabin observations.
10. No automated test browses TUI, invokes booking or uses Robin's database.

---

## Results

> Complete after implementation and verification.

### Status

In progress. Step 040a is complete. The evidence/context identity,
Available/Unavailable/Unknown states, Partial/Complete coverage, explicit
capture/record workflow, preference tri-state rules, separate Cabin Availability
alerts, retention and initial TUI boundary were agreed on 19 July 2026. Prompt
040b – Cabin Domain and Application Contracts is complete. Provider-independent
contextual observations, deterministic history/transition policies, typed Cabin
Availability alert candidates, Saved Criteria v2 and Application-owned
capture/persistence/use-case contracts are implemented with 672 offline tests
passing. Cabin alert materialization remains intentionally unwired because the
current SQLite schema supports only the Prompt 039 alert types. Prompt 040c –
SQLite Cabin Persistence is next.
