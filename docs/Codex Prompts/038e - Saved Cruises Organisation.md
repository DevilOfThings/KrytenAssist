# Codex Prompt 038e – Saved Cruises Organisation

## Implementation Prompt

Implement **Step 5 only** from:

```text
docs/AI Playbook/038 - Saved Cruises and Preferences.md
```

Prompts 038a–038d are complete. This step adds the distinct Saved Cruises
workspace, organisation filters and saved-item lifecycle actions. Reuse the
accepted personal-state contracts, SQLite persistence and shared evaluation
editor. Do not implement the general Cruise preference profile yet.

---

## Required Reading

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/037 - Cruise History and Price Tracking.md`
5. `docs/AI Playbook/037i - Cruise Discovery Workspace Refinement.md`
6. `docs/AI Playbook/038 - Saved Cruises and Preferences.md`
7. `docs/Codex Prompts/038a - Saved Cruise Experience and Contract.md`
8. `docs/Codex Prompts/038b - Personal Cruise Domain and Application Contracts.md`
9. `docs/Codex Prompts/038c - SQLite Personal-State Persistence.md`
10. `docs/Codex Prompts/038d - Save Actions and Evaluation Editing.md`
11. existing Saved Cruise, favourite-ship and Cruise History Application
    queries, repositories and tests
12. existing Cruise workspace, History and shared evaluation ViewModels,
    views, composition and tests

---

## Agreed User Experience

### Cruise Workspace Modes

Add an explicit top-level choice inside the existing Cruise experience:

```text
[ Discovery ]   [ Saved Cruises ]
```

- Discovery remains the existing browser, capture and Recorded History
  workspace.
- Saved Cruises is a distinct personal organisation workspace; it must not be
  embedded inside or presented as another Recorded History grouping.
- Discovery remains the default when the Cruise workspace is first created.
- switching modes must not close or navigate the embedded browser
- switching invalidates editor work tied to the hidden mode and cancels Saved
  Cruises loading, but does not alter saved state or factual History
- entering Saved Cruises loads current local personal state; provide an
  explicit Refresh action as well

Do not add a new application-shell section or duplicate Cruise navigation in
the global sidebar.

### Filters

Present four mutually exclusive filters with useful counts:

```text
[ Shortlist ] [ Strong candidates ] [ Favourites ] [ Not for us ]
```

Their exact semantics are:

- **Shortlist**: every `Shortlisted` sailing, including Unrated, Maybe and
  Strong candidate; this is the default filter.
- **Strong candidates**: Shortlisted sailings whose interest is
  `StrongCandidate`.
- **Favourites**: Shortlisted sailings that are either a favourite sailing or
  sail on an operator/ship currently marked as a favourite ship.
- **Not for us**: every `Dismissed` sailing, regardless of retained interest,
  ratings or favourite state.

Dismissed items appear only under Not for us. Counts and membership update
after evaluation, sailing-favourite, ship-favourite, dismiss, restore and
remove operations. Filtering is deterministic and local; it is not a
recommendation or ranking system.

Within every filter order by departure date, then normalized operator, ship,
duration and title. Keep past dated saved records visible; Prompt 038e must not
invent an expiry or archive policy.

### List and Selection

Use a bounded, scrollable master/detail presentation suitable for the existing
desktop Cruise workspace. Each row shows enough identity and personal state to
distinguish it without becoming a full card:

```text
Cruise | Ship | Departure | Interest | Overall | Price context | Favourite
```

- use the saved snapshot when no factual History exists
- clearly distinguish favourite sailing from favourite ship
- do not use colour alone to communicate status or favourite state
- selecting a row shows its saved snapshot, evaluation editor and available
  recorded price context
- preserve selection by `CruiseSailingKey` across refresh when the item remains
  in the active filter
- when an operation moves the selected item out of the active filter, select
  the next deterministic item or clear selection if none remains
- changing filters selects the previously selected matching sailing when
  possible, otherwise the first item, without mutating personal state

### Price and History Context

Saved snapshot context and factual History must remain visibly distinct:

- label `SavedCruiseSnapshot.DisplayedPrice` as **Price when saved** and show
  `SavedAt`
- when matching Recorded History exists, show **Latest recorded price**, its
  observation time and source, plus a compact observation/source count
- match saved state to History only by `CruiseSailingKey`
- retailer/source identity must not affect the join
- when several retailer-specific histories match, choose the latest
  observation by `ObservedAt`, then use the existing observation fingerprint as
  a stable tie-break; retain all matching histories for counts
- do not relabel either value as a live or current market price
- when no match exists, say **No recorded price history for this saved cruise.**
- never create, copy or update an observation while loading this context

The selected detail may show the bounded itinerary, departure port, retail
source and trusted reference from the saved snapshot. Opening or navigating a
saved reference is not required in this step.

### Evaluation Editing

Reuse the one `CruiseSaveAndEvaluationViewModel` introduced by 038d. Do not
create a second Saved-Cruises-specific evaluation implementation.

Extend the shared editor so it can open an existing `SavedCruise` directly,
without manufacturing a `CruiseObservation` and without re-running Save Cruise.
Its target identity/display state should work from the saved snapshot as well
as from the existing capture and History observation paths.

Confirmed evaluation, favourite-sailing and favourite-ship changes must notify
the organiser so rows, counts and filter membership refresh immediately. A
failed or cancelled edit retains the existing 038d retry behaviour and must not
optimistically change organiser state.

### Dismiss, Restore and Remove

For a Shortlisted item provide:

```text
[ Edit Evaluation ] [ Not for us ] [ Remove Saved Cruise ]
```

For a Dismissed item provide:

```text
[ Edit Evaluation ] [ Restore to Shortlist ] [ Remove Saved Cruise ]
```

- Not for us calls `DismissCruise` and changes only lifecycle state.
- Restore calls `RestoreCruise` and changes only lifecycle state.
- Both retain snapshot, evaluation and favourite-sailing state.
- Remove permanently deletes only the personal saved-sailing aggregate.
- Remove requires an inline confirmation naming the selected cruise and offers
  **Remove** and **Keep Saved Cruise** actions.
- removing a sailing does not unset its operator/ship favourite; that is an
  independent aggregate
- none of these operations records, changes or removes Cruise History

If an item is no longer present, remove it from the local presentation and show
an honest stale-item message. Do not claim success for Failed or Cancelled
results.

---

## Architecture

### Application Organisation Query

Add one provider-independent Application query/result model that composes the
existing local data needed by the organiser. A suitable shape is:

```text
ListSavedCruiseDetails
SavedCruiseDetails
SavedCruiseDetailsListResult
```

Names may be refined, but each detail contains:

- the `SavedCruise` aggregate
- whether its `CruiseShipKey` is a favourite ship
- all matching `CruiseHistoryDetails`, possibly empty and possibly covering
  multiple retail sources
- deterministic latest recorded observation/context when History exists

The query may compose the existing Application-owned repositories/analyzer or
focused query services. It must:

- load saved cruises, favourite ships and recorded histories asynchronously
- join only through `CruiseSailingKey`
- preserve the source-specific boundaries of each `CruiseRecordedHistory`
- return Success, Cancelled and Failed controlled outcomes
- never expose EF Core, SQLite, Avalonia, browser or provider SDK types
- never mutate personal state or observation data

Do not add a persistence table, database view, foreign key or denormalized
History columns for this join.

### Saved Cruises ViewModels

Introduce focused presentation types, for example:

```text
SavedCruisesViewModel
SavedCruiseItemViewModel
SavedCruiseFilter
```

The organiser ViewModel owns:

- lazy activation and explicit refresh
- one load generation and cancellation source
- the full loaded detail set and four derived filters/counts
- stable selection by sailing key
- dismiss, restore and remove command lifecycles
- inline remove confirmation
- controlled loading, empty, cancellation and error messages
- coordination with the shared 038d editor

Keep filtering, selection, commands and lifecycle out of XAML code-behind.
Item ViewModels are presentation projections and must not call repositories.

`CruiseOfTheWeekViewModel` remains the Cruise workspace owner. It coordinates
Discovery/Saved Cruises mode selection and owns/injects the Saved Cruises child
alongside the existing browser and shared editor. Register new services through
the existing extension methods, not manually in `Program.cs`.

### Shared Editor Synchronisation

Extend the shared editor with a small presentation-level notification contract
for confirmed changes, for example events carrying the updated saved aggregate
or favourite ship key/state. The organiser may update its projection or run a
generation-safe refresh.

- emit notifications only after confirmed Updated or synchronized Unchanged
  outcomes
- do not emit on validation failure, Failed, Cancelled or stale completion
- opening/cancelling the editor does not reload or mutate the list
- dismiss, restore or remove must update/clear the editor target consistently
- selection or workspace mode changes invalidate late editor results

Avoid circular ViewModel ownership: the Cruise workspace owns both children;
the organiser coordinates with the injected shared editor but does not create
it.

---

## Loading, Lifecycle and Controlled Outcomes

Every list load and selected-item mutation has cancellation and generation
protection. Invalidate relevant work when:

- Refresh starts
- the Saved Cruises workspace is left or deactivated
- selection changes during a selected-item operation
- the selected item is dismissed, restored or removed
- a newer editor change supersedes a pending refresh

Late results must not replace a newer list, restore an old selection, reopen an
editor or show a message against the wrong sailing.

While list loading:

- retain the last successful list during an ordinary Refresh
- show a loading state on first activation
- allow cancellation where useful
- disable conflicting lifecycle actions

Map existing Application statuses honestly:

- Dismissed: `Cruise moved to Not for us.`
- Restored: `Cruise restored to your shortlist.`
- Removed: `Saved cruise removed. Recorded History was not changed.`
- Unchanged: synchronize local state without claiming a new change
- NotFound: remove stale local item and explain it is no longer saved
- Cancelled: retain prior confirmed state and allow retry
- Failed: retain prior confirmed state and show a retryable local failure

Unexpected presentation exceptions remain contained at command boundaries.

---

## Empty, Loading and Failure States

Provide distinct messages for:

- no personal saved cruises at all: explain that cruises can be saved from
  Discovery capture or Recorded History
- no items in the active filter: explain the filter without implying no saved
  data exists
- first load in progress
- refresh in progress with existing content retained
- cancelled load
- failed local load with **Try Again**
- selected saved cruise with no Recorded History

Do not display Not for us items in the general empty-state count and do not
describe saved snapshot price as tracked History.

---

## Required Tests

Add deterministic offline tests covering:

### Application query

- saved cruises join to History only by sailing identity
- a source change does not prevent a match or duplicate personal state
- multiple source histories remain distinct and all contribute to counts
- latest observation selection is chronological with a stable tie-break
- no History returns valid saved snapshot context with an empty match set
- favourite ship state is derived by `CruiseShipKey`
- cancellation and repository failures return controlled results
- the query performs no repository mutations

### Filters and list projection

- Shortlist includes all and only Shortlisted items
- Strong candidates requires Shortlisted plus StrongCandidate interest
- Favourites includes Shortlisted favourite sailings or favourite ships
- Not for us includes all and only Dismissed items
- deterministic ordering and filter counts
- clear distinction between Price when saved and Latest recorded price
- honest no-History state

### Selection and lifecycle

- first activation loads once and Refresh reloads
- refresh retains selection by sailing key
- filter changes preserve/reselect deterministically
- dismiss moves an item out of shortlist filters and retains evaluation
- restore moves an item out of Not for us and retains evaluation
- remove requires confirmation and clears only personal selection
- cancelling remove performs no mutation
- remove does not call observation or favourite-ship mutation contracts
- NotFound removes stale presentation state
- Cancelled/Failed retains prior confirmed state
- selection/mode/deactivation changes ignore stale completions

### Shared editor and composition

- an existing SavedCruise opens directly without SaveCruise or an invented
  observation
- confirmed evaluation and favourite changes update counts/membership
- failed/cancelled editor operations do not optimistically update rows
- one shared editor instance is used across Discovery, History and Saved
  Cruises for a resolved Cruise workspace
- switching modes does not close or navigate the embedded browser
- all new ViewModels and Application services resolve through DI

Use fakes at Application boundaries. Persistence restart and schema behaviour
remain covered by 038c. No automated test may contact TUI, launch a browser,
use Robin's database or access the network.

---

## Allowed Changes

```text
KrytenAssist.Application/Cruises/ (organisation query/results only)
KrytenAssist.Application/DependencyInjection.cs
KrytenAssist.Avalonia/DependencyInjection/
KrytenAssist.Avalonia/ViewModels/CruiseOfTheWeekViewModel.cs
KrytenAssist.Avalonia/ViewModels/CruiseSaveAndEvaluationViewModel.cs
KrytenAssist.Avalonia/ViewModels/SavedCruise*.cs
KrytenAssist.Avalonia/Views/CruiseOfTheWeekView.axaml
KrytenAssist.Avalonia/Views/SavedCruise*.axaml
KrytenAssist.Avalonia/Views/SavedCruise*.axaml.cs
KrytenAssist.Avalonia.Tests/Application/Cruises/
KrytenAssist.Avalonia.Tests/ViewModels/
KrytenAssist.Avalonia.Tests/DependencyInjection/
docs/Codex Prompts/038e - Saved Cruises Organisation.md
docs/AI Playbook/038 - Saved Cruises and Preferences.md
docs/Roadmap.md
```

Prompt 038b/c domain, repository and schema contracts may change only for a
concrete defect blocking this workflow. Document and test any such correction.
Do not stage, commit, push, discard or overwrite unrelated work.

---

## Exclusions

- general departure month, cabin and budget preference editor (Prompt 038f)
- recommendation, ranking, scoring, alerts or automatic inference
- automatic dismissal, expiry or archiving of past sailings
- Save All, bulk dismiss, bulk restore or bulk remove
- opening, navigating or refreshing saved retail references
- new persistence schema, migration, foreign key or database view
- new retailer, provider, capture, browser or price-history behaviour
- deleting or rewriting factual Cruise History
- Prompt 039 or later work

---

## Verification

Run focused organisation-query, filter, lifecycle, shared-editor, workspace and
composition tests, then:

```text
dotnet build KrytenAssist.sln --no-restore
dotnet test KrytenAssist.sln --no-build --no-restore
```

Use the established single-worker runner when required. Tests remain offline.
After automated verification, manually confirm the four filters, a no-History
saved capture, edit, Not for us, restore and confirmed remove workflows while
verifying that Recorded History remains unchanged.

---

## Results

### Status

Complete.

### Implementation Summary

- Added a distinct Saved Cruises workspace mode beside Discovery without
  closing or navigating the existing browser.
- Added Shortlist, Strong candidates, Favourites and Not for us filters with
  exact lifecycle/favourite semantics, deterministic ordering and live counts.
- Added an Application organisation query that joins personal records to all
  matching source-specific History through `CruiseSailingKey`, derives
  favourite-ship state and selects the latest recorded observation
  deterministically.
- Added bounded master/detail presentation that distinguishes Price when saved
  from Latest recorded price and states honestly when no History exists.
- Reused and extended the shared 038d editor so existing saved aggregates open
  without re-saving or manufacturing observations; confirmed evaluation and
  favourite changes update organiser membership.
- Added Not for us, Restore and confirmed Remove workflows with controlled
  outcomes, cancellation/generation protection and strict preservation of
  Recorded History and independent ship favourites.
- Added loading, refresh, empty-filter, no-saved-data and retryable local-error
  presentation states.

### Files Added

- `KrytenAssist.Application/Cruises/SavedCruiseDetails.cs`
- `KrytenAssist.Application/Cruises/ListSavedCruiseDetails.cs`
- `KrytenAssist.Avalonia/ViewModels/SavedCruiseFilter.cs`
- `KrytenAssist.Avalonia/ViewModels/SavedCruiseItemViewModel.cs`
- `KrytenAssist.Avalonia/ViewModels/SavedCruisesViewModel.cs`
- `KrytenAssist.Avalonia/Views/SavedCruisesView.axaml`
- `KrytenAssist.Avalonia/Views/SavedCruisesView.axaml.cs`
- `KrytenAssist.Avalonia.Tests/Application/Cruises/ListSavedCruiseDetailsTests.cs`
- `KrytenAssist.Avalonia.Tests/ViewModels/SavedCruisesViewModelTests.cs`
- `docs/Session Handovers/2026-07-18 Session 022.md`

### Files Updated

- `KrytenAssist.Application/DependencyInjection.cs`
- `KrytenAssist.Avalonia/DependencyInjection/DesktopPersistenceServiceCollectionExtensions.cs`
- `KrytenAssist.Avalonia/ViewModels/CruiseOfTheWeekViewModel.cs`
- `KrytenAssist.Avalonia/ViewModels/CruiseSaveAndEvaluationViewModel.cs`
- `KrytenAssist.Avalonia/Views/CruiseOfTheWeekView.axaml`
- `KrytenAssist.Avalonia.Tests/DependencyInjection/CruiseHistoryDesktopCompositionTests.cs`
- this prompt, the Prompt 038 playbook and Roadmap

### Build and Tests

- `dotnet build KrytenAssist.sln --no-restore --disable-build-servers -m:1`:
  passed with 0 errors. Five SQLitePCLRaw advisory warnings remain from the
  existing dependency baseline.
- focused 038d/038e tests: 10 passed
- Core: 120 passed
- Avalonia: 458 passed
- API: 9 passed
- total: 587 passed, 0 failed, 0 skipped
- `git diff --check`: passed

Automated tests remained offline and did not contact TUI, launch a browser or
use Robin's database. Manual desktop verification of filters, no-History price
context, edit, dismiss, restore and confirmed removal remains for Robin.

### Next

Prompt 038f – Cruise Preference Profile.
