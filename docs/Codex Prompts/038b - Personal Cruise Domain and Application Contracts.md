# Codex Prompt 038b – Personal Cruise Domain and Application Contracts

## Implementation Prompt

Implement **Step 2 only** from:

```text
docs/AI Playbook/038 - Saved Cruises and Preferences.md
```

Prompt 038a is complete. This step replaces the provisional personal-preference
prototype with the agreed provider-independent Core model and Application
contracts. Do not implement SQLite persistence or Avalonia behaviour.

---

## Required Reading

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/037 - Cruise History and Price Tracking.md`
5. `docs/AI Playbook/038 - Saved Cruises and Preferences.md`
6. `docs/Codex Prompts/038a - Saved Cruise Experience and Contract.md`
7. existing Core Cruise models and tests
8. existing Application Cruise contracts, use cases and tests
9. all provisional Prompt 038 Core/Application/Infrastructure files

---

## Required Domain Model

Names may be refined for consistency, but the responsibilities and boundaries
below are required.

### Saved Cruise

Introduce a saved personal record for one `CruiseSailingKey`.

It contains:

- stable sailing key
- bounded `SavedCruiseSnapshot`
- shortlist/dismissed status
- personal evaluation
- favourite-sailing marker

Saving a new sailing should require no rating or notes. Use a deliberate
default shortlist state without inventing evaluation answers.

### Saved Cruise Snapshot

The immutable snapshot contains:

- title
- operator display name
- ship, departure date and duration through the sailing key
- optional departure port
- optional itinerary summary
- displayed price context
- optional retail source
- optional trusted source reference
- saved timestamp

Prefer reusing provider-independent value objects such as `CruisePrice`,
`CruiseSource` and `CruiseSailingKey`. Do not reuse `CruiseObservation` or
`CruiseSnapshot` as the saved aggregate: that would falsely make personal
shortlist context look like price-history evidence.

Snapshot construction may accept a provider-independent `CruiseObservation` at
the application boundary, but the resulting saved model must not retain an
observation or observation fingerprint.

Normalize and bound all user/source text consistently with existing Cruise
models. Define explicit limits suitable for later SQLite constraints.

### Lifecycle and Interest

Separate these concepts:

```text
SavedCruiseStatus: Shortlisted | Dismissed
CruiseInterestLevel: Maybe | StrongCandidate
```

Interest is optional. **Not for us** is the user-facing meaning of Dismissed,
not a third interest-strength value.

### Evaluation

`CruiseEvaluation` contains only mutable personal evaluation data:

- optional interest level
- optional overall rating
- optional itinerary rating
- optional ship rating
- optional value rating
- optional notes

It does not own sailing identity, shortlist lifecycle, favourite-ship state or
provider evidence. Ratings are integers from 1–5. Notes are trimmed, optional
and limited to 4,000 characters.

### Favourite Sailing and Favourite Ship

- Favourite sailing is a boolean property of one saved sailing.
- Introduce `CruiseShipKey` using normalized operator id and ship name.
- A favourite ship is stored/queryable by `CruiseShipKey`, independently of
  dated sailings.
- Do not repeat favourite-ship booleans across saved-cruise records.

### Cruise Preferences

Model:

- a normalized, duplicate-free collection of departure months 1–12
- a normalized, duplicate-free collection of preferred cabin types
- optional maximum budget

Use an explicit cabin enum covering the current intended categories:

```text
Inside | Outside | Balcony | Suite | Solo
```

Model maximum budget as a value object containing non-negative amount,
three-letter currency and explicit basis:

```text
PerPerson | TotalBooking
```

Collections must be immutable/read-only from consumers and have deterministic
ordering and value equality. Empty collections and no budget represent an
unset preference profile.

---

## Required Application Contracts

Application owns its persistence abstractions. Prefer focused interfaces over
one broad provisional preference repository.

Define contracts supporting:

- get one saved cruise by sailing key
- list saved cruises
- create or update a saved cruise
- remove personal saved state
- get/list and set/unset favourite ships
- get and save the preference profile

Repository methods must remain asynchronous and accept cancellation tokens.
They must not expose EF Core, SQLite, Avalonia or provider SDK types.

Define focused use cases for:

- `SaveCruise`
- `UpdateCruiseEvaluation`
- `DismissCruise`
- `RestoreCruise`
- `RemoveSavedCruise`
- `GetSavedCruise`
- `ListSavedCruises`
- set/unset and list favourite ships
- get/save cruise preferences

Exact class grouping may remain small and cohesive, but UI code must not need
to manipulate repositories or aggregate lifecycle directly.

### Result Semantics

Do not return a bare `bool`. Result types must distinguish the useful outcomes
for later ViewModels, including where applicable:

```text
Created | Updated | Unchanged | Found | NotFound | Removed
Dismissed | Restored | Cancelled | Failed
```

Controlled failures may contain safe user-facing messages. Cancellation must
remain distinct from failure. Unexpected repository exceptions must not escape
the use-case boundary.

### Save Semantics

- first save creates a Shortlisted item with empty evaluation
- saving the same sailing again updates its bounded source snapshot rather than
  creating a duplicate
- repeat save does not overwrite existing ratings, notes, lifecycle or
  favourite-sailing state
- source/retailer differences do not alter sailing identity
- saving never calls `ICruiseObservationRepository`

### Mutation Semantics

- evaluation update changes only evaluation fields
- dismiss changes only lifecycle to Dismissed
- restore changes only lifecycle to Shortlisted
- remove deletes only personal saved state
- favourite-ship changes use ship identity and do not rewrite each sailing
- preference changes do not filter, rank, alert or mutate saved cruises

---

## Provisional Prototype Handling

Audit and replace the provisional:

```text
CruiseEvaluation
CruiseInterestLevel
CruisePreferences
ICruisePreferenceRepository
SaveCruiseEvaluation
SaveCruisePreferences
```

The provisional Infrastructure implementation and migration encode the wrong
contract. Prompt 038b must not extend or validate them as the final schema.

If Core/Application changes would leave the solution uncompilable, remove only
the provisional Prompt 038 Infrastructure registration, entities, repository,
migration and snapshot changes, restoring the verified Prompt 037j persistence
baseline. Prompt 038c will implement and generate the accepted schema. Do not
alter any Prompt 037 observation/history persistence behaviour.

---

## Required Tests

Add comprehensive deterministic tests proving:

- saved snapshot validation, normalization and equality
- saved identity ignores retail source and source reference
- lifecycle is separate from optional interest
- ratings accept 1–5 and reject other values
- notes trim, clear and enforce their limit
- favourite ship identity is operator plus ship
- preference collections deduplicate and order deterministically
- invalid months, budgets, currency and enum values are rejected
- first save creates one Shortlisted record with empty evaluation
- repeat save refreshes snapshot but preserves evaluation and personal state
- source changes do not duplicate a sailing
- evaluation, dismiss, restore and remove use cases change only their owned state
- favourite-ship use cases do not rewrite sailings
- all not-found, unchanged, cancellation and exception paths return controlled
  results
- no use case records or deletes Cruise History

Use in-memory fakes at the Application boundary. Do not use SQLite, Robin's
database, TUI, a browser or network access in Prompt 038b tests.

---

## Allowed Changes

```text
KrytenAssist.Core/Cruises/
KrytenAssist.Core.Tests/Cruises/
KrytenAssist.Application/Abstractions/Persistence/
KrytenAssist.Application/Cruises/
KrytenAssist.Application/DependencyInjection.cs
KrytenAssist.Avalonia.Tests/Application/Cruises/
docs/Codex Prompts/038b - Personal Cruise Domain and Application Contracts.md
docs/AI Playbook/038 - Saved Cruises and Preferences.md
docs/Roadmap.md
```

Only the provisional Prompt 038 Infrastructure files/registrations/migration
and their model-snapshot additions may also be removed when required to restore
a compiling Prompt 037j persistence baseline. No other Infrastructure changes
are allowed.

Do not stage, commit, push, discard or overwrite unrelated work.

---

## Exclusions

- accepted SQLite schema or persistence implementation (Prompt 038c)
- Avalonia commands, ViewModels or UI (Prompts 038d–038f)
- joining saved cruises to price-history query results
- recommendation, ranking, alerts or automatic inference
- new provider, source, browser, capture or price model behaviour
- Prompt 039 or later work

---

## Verification

Run focused Core and Application tests, then:

```text
dotnet build KrytenAssist.sln --no-restore
dotnet test KrytenAssist.sln --no-build --no-restore
```

Use the established single-worker runner when sandboxed test-host sockets or
MSBuild nodes require it. Tests must remain deterministic and offline.

---

## Results

> Complete after implementation and verification.

### Status

Complete.

### Summary

- Replaced the provisional evaluation-first model with an explicit saved-cruise
  aggregate, bounded snapshot, shortlist/dismissed lifecycle and optional
  evaluation.
- Separated favourite sailing from normalized operator/ship favourites.
- Added deterministic multi-month and multi-cabin preferences with a
  currency-aware per-person/total-booking budget.
- Replaced the broad provisional repository with focused Application-owned
  saved-cruise, favourite-ship and preference contracts.
- Added controlled save, query, update, dismiss, restore, remove and favourite
  outcomes, including cancellation and safe failure paths.
- Removed the provisional Prompt 038 SQLite implementation and migration,
  restoring the Prompt 037j persistence baseline for Prompt 038c.
- Deferred use-case DI registration until Prompt 038c registers the matching
  repositories, preserving valid API and desktop composition.

### Files Added or Replaced

- Core saved-cruise, snapshot, evaluation, lifecycle, ship-key, cabin, budget
  and preference models under `KrytenAssist.Core/Cruises/`
- focused repository contracts under
  `KrytenAssist.Application/Abstractions/Persistence/`
- saved-cruise and personal-preference use cases/results under
  `KrytenAssist.Application/Cruises/`
- Core tests in `KrytenAssist.Core.Tests/Cruises/`
- Application fakes and use-case tests in
  `KrytenAssist.Avalonia.Tests/Application/Cruises/`
- this prompt, the Prompt 038 playbook and Roadmap

The provisional Prompt 038 Infrastructure entities, repository, migration and
schema-test changes were removed. Existing Prompt 037 persistence files are
unchanged relative to their verified baseline.

### Build

Passed:

```text
dotnet build KrytenAssist.sln --no-restore -m:1
```

Result: 0 errors. Existing SQLite package advisory warnings remain.

### Tests

Passed offline:

```text
Core:      120 passed
Avalonia:  439 passed
API:         9 passed
Total:     568 passed, 0 failed, 0 skipped
```

### Notes

- A repeat save refreshes bounded source context while retaining evaluation,
  lifecycle and favourite-sailing state.
- `Not for us` is represented by Dismissed status and no longer appears in the
  interest enum.
- Prompt 038c owns persistence implementation, DI composition, migration and
  restart/concurrency verification.

### Next

Prompt 038c – SQLite Personal-State Persistence.
