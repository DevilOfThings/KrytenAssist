# Prompt 038 – Saved Cruises and Preferences

## Goal

Allow Robin to deliberately save, organise and evaluate interesting cruises,
and record a small set of explicit personal preferences for later comparison.

Prompt 038 must keep Robin's mutable opinions separate from the factual cruise
observations and price evidence established by Prompt 037.

---

## Why This Prompt Exists

Cruise Discovery can capture currently displayed offers and Recorded Cruise
History can retain factual evidence across restarts. Neither capability answers
the personal questions that follow:

- Is this sailing interesting to us?
- Why did we save or reject it?
- How strongly does it appeal?
- Which itinerary, ship or price looks best?
- Which months, cabin types and budgets should later comparisons consider?

Prompt 038 introduces that personal decision layer. It does not reinterpret,
delete or manufacture provider observations.

---

## Proposed User Experience

### Save Without Recording

After a successful capture, Robin may choose:

```text
[ Record Observation ]   [ Save Cruise ]
```

The actions are independent:

- **Record Observation** preserves factual source and price evidence.
- **Save Cruise** adds the sailing to Robin's personal shortlist.

Saving must not silently record an observation. Recording must not silently
save a cruise.

The same sailing can also be saved from Recorded Cruise History. Saving from a
capture must not require the observation to have been recorded first.

### Saved Cruise Evaluation

A saved sailing may contain:

- interest level: **Maybe** or **Strong candidate**
- optional overall rating
- optional itinerary rating
- optional ship rating
- optional value rating
- optional personal notes
- favourite sailing marker

Ratings use a simple 1–5 scale and remain optional. Saving is useful without
completing every field.

### Not For Us

**Not for us** is useful preference evidence but is not a saved shortlist item.
It is a deliberate dismissed evaluation. Dismissed sailings should be retained
locally and available through an explicit filter, without crowding the default
Saved Cruises list.

Changing a saved sailing to **Not for us** removes it from the default shortlist
but does not delete its evaluation or any Recorded Cruise History.

### Favourite Ships

A favourite ship applies to the operator and ship, not to one dated sailing.
Marking a ship as favourite should be reflected consistently for every loaded
sailing on that ship.

This is separate from favouriting one specific saved sailing.

### Cruise Preferences

Prompt 038 records an intentionally small preference profile:

- one or more preferred departure months
- optional maximum budget
- explicit budget basis: **per person** or **total booking**
- one or more preferred cabin types

Preferences are guidance, not automatic exclusion rules. Prompt 038 stores and
edits them; it does not score cruises, recommend bookings or generate alerts.

### Saved Cruises Workspace

Provide a distinct Saved Cruises area inside the Cruise experience. It should
show personal state without merging it into factual Recorded Cruise History.

Default presentation:

```text
Saved Cruises
Filter: [ Shortlist | Strong candidates | Favourites | Not for us ]

Cruise | Ship | Departure | Interest | Overall | Current known price
```

Selecting an item shows its editable personal evaluation and any available
local price-history summary. Absence of price history must be presented
honestly when a captured sailing was saved without recording it.

---

## Identity and Data Boundaries

### Saved Sailing Identity

A saved cruise represents a specific sailing:

```text
Operator + Ship + Departure date + Duration
```

Retailer, offer id and source URL are evidence about that sailing, not its
personal saved identity. The saved record may retain a bounded snapshot of the
captured sailing so it remains useful when no observation was recorded.

That saved snapshot contains only the useful facts Robin saw when saving:

- cruise title
- operator and ship
- departure date, departure port and duration
- itinerary summary
- latest displayed price
- retail source
- trusted source reference
- time saved

This is personal shortlist context. It must not appear in Recorded Cruise
History, count as a price observation or participate in price trends unless
Robin separately chooses **Record Observation**.

The implementation must define how a saved capture later joins to Recorded
Cruise History without duplicating personal evaluations.

### Favourite Ship Identity

```text
Operator + Ship
```

Ship favourites must not be represented as unrelated booleans repeated across
individual sailing evaluations.

### Personal State Versus Evidence

```text
Provider observation / price history     Robin's personal state
------------------------------------     ----------------------
Source and offer id                      Saved or dismissed
Observed title and itinerary             Interest level
Observed price and promotion             Ratings
First/last observed                      Notes
Price movement                           Favourite sailing/ship
                                         General preferences
```

Personal changes must never delete, rewrite or add provider observations.

---

## Architecture Principles

- Core owns saved-sailing, evaluation, favourite-ship and preference models.
- Application owns save, update, remove, query and organisation contracts.
- Infrastructure persists personal state locally in SQLite.
- Avalonia owns editing state, commands, filters and presentation.
- Provider and browser types must not enter personal-state contracts.
- Saving and preference editing remain fully offline.
- Mutating a personal record must use explicit user actions.
- Removing a saved item must not remove factual History.

The classes and migration currently present in the worktree are provisional.
Step 038b must compare them with this agreed contract and amend or replace them
where necessary; their current shapes are not architectural decisions.

---

## Scope

### In Scope

- save a captured or recorded specific sailing
- optional personal evaluation and notes
- shortlist, strong-candidate, favourite and dismissed organisation
- sailing favourites and operator/ship favourites
- preferred months, budget with basis and preferred cabin types
- local SQLite persistence and restart behaviour
- independent association with available Recorded Cruise History
- deterministic offline tests and desktop manual verification

### Out of Scope

- recommendations, rankings or automatic preference inference
- price-drop alerts or notifications
- cabin availability monitoring
- background browsing, scraping or unattended capture
- bookings, payments or purchase decisions
- deleting or rewriting provider observations
- new retailer support, capture selectors or price modelling
- multiple named traveller profiles
- cloud synchronisation

---

## Implementation Steps

### Step 1 – 038a: Saved Cruise Experience and Contract

- agree the save, dismiss, restore, edit and remove workflows
- define sailing identity and the minimum snapshot required when saving an
  unrecorded capture
- define interest states, optional ratings and notes limits
- define favourite sailing versus favourite ship behaviour
- define month, cabin and budget preference semantics
- document independence from Record Observation and Cruise History
- produce contract tests or test cases before accepting persistence shapes

No further production implementation belongs to this step.

### Step 2 – 038b: Personal Cruise Domain and Application Contracts

- audit the provisional Prompt 038 classes against Step 1
- model saved/dismissed sailing state independently from observations
- model favourite ships by operator and ship identity
- model multiple months and cabin types plus an explicit budget basis
- introduce application-owned repositories and result types
- define save, update, dismiss/restore, remove and query use cases
- add comprehensive Core and Application tests

The agreed analysis separates shortlist lifecycle from optional interest:
**Not for us** is Dismissed, while **Maybe** and **Strong candidate** are
optional interest strengths. It also requires a distinct operator/ship key,
multiple normalized months and cabin types, and a currency-aware maximum budget
with explicit per-person or total-booking basis. See the 038b Codex prompt for
the complete implementation contract.

### Step 3 – 038c: SQLite Personal-State Persistence

- audit or replace the provisional migration before it is accepted
- persist saved sailing snapshots, evaluations, favourite ships and general
  preferences in normalized local tables
- enforce stable identity, field limits and valid rating/preference ranges
- support update and removal without cascading into Cruise History
- verify migrations from the current Prompt 037j database and restart
  persistence using isolated test databases

The agreed analysis uses five independent tables: saved sailings, favourite
ships, a singleton preference profile, preferred months and preferred cabins.
There is deliberately no database foreign key between personal state and
Cruise History; their normalized sailing values are associated only through
Application identity. See the 038c Codex prompt for the complete schema,
transaction, concurrency, migration and composition contract.

### Step 4 – 038d: Save Actions and Evaluation Editing

- add explicit Save Cruise actions to capture review and Recorded History
- keep Save Cruise and Record Observation independent
- provide editable interest, rating, note and favourite controls
- provide honest save/update/cancel/failure feedback
- prevent stale capture or selection changes from updating the wrong sailing
- add focused ViewModel and command-lifecycle tests

The agreed analysis uses one shared workspace child ViewModel for explicit
single-capture, per-candidate and selected-History targets. It maps observations
through a provider-independent Application factory, keeps save and recording
independent, provides separate evaluation/sailing-favourite/ship-favourite
operations and rejects stale asynchronous results by target generation. See the
038d Codex prompt for the complete interaction and test contract.

### Step 5 – 038e: Saved Cruises Organisation

- add the distinct Saved Cruises presentation
- support shortlist, strong-candidate, favourite and Not for us filters
- show available local current-price context without requiring History
- provide selection, edit, remove, dismiss and restore workflows
- surface favourite-ship state consistently across relevant sailings
- preserve useful empty, loading and failure states

### Step 6 – 038f: Cruise Preference Profile

- add editing for preferred departure months, cabin types, maximum budget and
  budget basis
- make optional/unset preferences explicit
- persist only after an explicit Save Preferences action
- do not filter, score, alert or recommend automatically
- add deterministic ViewModel and persistence tests

### Step 7 – 038g: Tests and Verification

- audit separation between personal state and provider evidence
- verify saving from capture without recording and saving from History
- verify update, dismiss, restore, remove and restart behaviour
- verify removing personal state leaves all Cruise History untouched
- run the complete offline suite and build
- manually verify the desktop workflow
- update Results, Lessons Learned, Roadmap and Session Handover
- leave Prompt 039 unstarted

---

## Acceptance Criteria

Prompt 038 is complete only when:

- Robin can save a captured sailing without recording an observation
- Robin can save a sailing already present in Recorded Cruise History
- saving and recording remain visibly and technically independent
- saved state is attached to stable sailing identity, not a retailer offer
- optional ratings, notes and interest can be edited later
- Not for us is retained but excluded from the default shortlist
- favourite sailing and favourite ship have distinct identities
- preferred months, cabins and budget basis are explicit and locally persisted
- Saved Cruises remains useful when no matching price history exists
- personal updates/removal never alter factual observations or price history
- no provider-specific types leak into Core or Application
- automated tests remain offline and use isolated databases
- build and full regression tests pass
- Robin manually verifies the workflow
- results documentation is complete

---

## Results

> Complete after implementation and verification.

### Status

In progress. Steps 038a–038d are complete. Robin can deliberately save a single
capture, an individual Ready batch candidate or a selected recorded History
sailing without recording another observation. One shared workspace editor now
supports optional personal evaluation plus independent sailing and ship
favourites, with controlled outcomes and stale-result protection. Prompt 038e
– Saved Cruises Organisation is next.
