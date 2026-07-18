# Codex Prompt 038d – Save Actions and Evaluation Editing

## Implementation Prompt

Implement **Step 4 only** from:

```text
docs/AI Playbook/038 - Saved Cruises and Preferences.md
```

Prompts 038a–038c are complete. This step exposes deliberate Save Cruise
actions from existing capture and History workflows and provides one shared
personal-evaluation editor. Do not build the Saved Cruises organiser or general
preference profile yet.

---

## Required Reading

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/037h - Multiple Cruise Deals Handling.md`
5. `docs/AI Playbook/038 - Saved Cruises and Preferences.md`
6. `docs/Codex Prompts/038a - Saved Cruise Experience and Contract.md`
7. `docs/Codex Prompts/038b - Personal Cruise Domain and Application Contracts.md`
8. `docs/Codex Prompts/038c - SQLite Personal-State Persistence.md`
9. existing Cruise browser, capture-candidate, History and Cruise workspace
   ViewModels/views/tests

---

## User Experience

### Single Capture Review

For a successful single capture, present independent actions:

```text
[ Record Observation ]   [ Save Cruise ]
```

- Record Observation retains its existing factual History behaviour.
- Save Cruise creates or refreshes personal shortlist state only.
- either action is available without performing the other
- saving never changes record-completion text or History counts
- recording never changes saved-state text

The existing single-capture compatibility path must remain supported even
though current TUI composition normally uses batch capture.

### Multi-Cruise Capture Review

Each Ready candidate receives its own explicit **Save Cruise** action.

- incomplete or failed candidates cannot be saved
- one candidate can be saved without selecting or recording it
- candidate selection for batch recording remains unchanged
- saving does not lock or alter record-selection checkboxes
- show per-candidate Saving, Saved/Updated, Cancelled and Failed feedback
- a newly captured replacement with the same display position must not inherit
  the previous candidate's saved feedback

Do not add **Save All**, **Save Selected** or automatic saving. A shortlist is a
personal decision for each sailing, unlike factual batch observation capture.

### Recorded History

For the selected History sailing, place a personal-state action beside the
existing detail actions:

```text
[ Save Cruise ]
```

When the sailing is already saved, display:

```text
[ Edit Evaluation ]
```

This works without opening TUI and uses the latest recorded provider-independent
observation as saved snapshot context. It does not create another observation.

### Shared Evaluation Editor

After a successful save, or when Edit Evaluation is chosen, show one bounded
editor for the explicitly selected sailing:

```text
Personal Evaluation
Cruise / Ship / Departure

Interest:  [ Unrated | Maybe | Strong candidate ]
Overall:   [ Unrated | 1 | 2 | 3 | 4 | 5 ]
Itinerary: [ Unrated | 1 | 2 | 3 | 4 | 5 ]
Ship:      [ Unrated | 1 | 2 | 3 | 4 | 5 ]
Value:     [ Unrated | 1 | 2 | 3 | 4 | 5 ]
Notes:     [ ... ]

[ Save Evaluation ] [ Cancel Changes ]
[ Favourite Cruise / Remove Cruise Favourite ]
[ Favourite Ship / Remove Ship Favourite ]
```

All evaluation fields are optional. `Unrated` maps to null. Notes show the
4,000-character limit and reject excess input without calling the use case.

`Not for us`, restore and removal belong to the Saved Cruises organiser in
Prompt 038e and are not editor controls in this step.

Saving evaluation, changing the sailing favourite and changing the ship
favourite are separate explicit operations with separate controlled feedback.
Do not imply an all-or-nothing transaction across those distinct aggregates.

---

## Architecture

### Shared Workspace ViewModel

Introduce one focused child ViewModel, for example:

```text
CruiseSaveAndEvaluationViewModel
```

It is owned by the Cruise workspace and shared by:

- single capture review
- individual Ready batch candidates
- selected Recorded History

Do not copy save/evaluation state into three independent implementations and do
not move personal-state logic into XAML code-behind.

The ViewModel owns:

- current explicit target and its origin
- load/save/update cancellation
- target generation
- saved aggregate and editable draft
- commands and availability
- validation and controlled messages
- favourite sailing/ship display state

The existing browser ViewModel may coordinate target selection and candidate
callbacks, but persistence use cases remain encapsulated by the focused child.

### Snapshot Mapping

Add a provider-independent Application mapper/factory rather than constructing
saved snapshots in Avalonia bindings or code-behind.

It maps a `CruiseObservation` plus the explicit current save timestamp to
`SavedCruiseSnapshot`:

- sailing key from operator, ship, date and duration
- title and operator display name
- departure port and itinerary
- the first advertised price in existing preserved display order
- retail source and source reference
- saved timestamp from injected `IClock.Now`

Capture currently guarantees at least one price. The mapper must still validate
its inputs and must never create/record a `CruiseObservation`.

Saving from History uses the latest chronologically recorded observation and
its latest evidence/source reference. Define this selection deterministically
and test it; do not use current wall-clock ordering or retailer identity as the
saved key.

The mapper contains no OpenAI, TUI, browser, EF Core or Avalonia types.

### Existing Saved State

Use `GetSavedCruise` before presenting History Save/Edit state or opening the
editor. Do not infer saved state from Record Observation completion.

After `SaveCruise` returns Created, Updated or Unchanged:

- retain the returned saved aggregate
- populate the editor from its existing evaluation and favourite-sailing state
- query favourite ships and derive ship favourite by `CruiseShipKey`
- show honest Created/Updated/Already saved feedback

Do not overwrite an existing evaluation when a source snapshot is refreshed;
the Application use case already preserves personal state.

---

## Lifecycle and Stale-Result Protection

Every explicit editor target has a monotonically increasing generation and a
cancellation source.

Invalidate the active target when:

- capture result is cleared or replaced
- new browser navigation clears capture review
- a batch result is replaced
- the associated History selection changes
- the Cruise workspace deactivates
- the user explicitly closes/cancels the editor

Late load/save/evaluation/favourite results for an invalid generation must be
ignored. They must not reopen the editor, update a new candidate, change a new
History selection or display a stale success message.

While an operation is active:

- disable conflicting commands for that target
- expose an explicit Cancel action where useful
- preserve unrelated Record Observation and browser cancellation lifecycles
- do not share cancellation sources with capture, navigation or recording

Unexpected presentation exceptions must remain contained at command boundaries.

---

## Commands and Controlled Outcomes

Use existing Prompt 038 Application result statuses directly and map every
outcome to honest presentation state.

### Save Cruise

- Created: `Cruise saved to your shortlist.`
- Updated: `Saved cruise details refreshed; your evaluation was preserved.`
- Unchanged: `This cruise is already saved.`
- Cancelled: retryable cancelled message
- Failed: retryable local-save failure

### Save Evaluation

- Updated: evaluation saved
- Unchanged: no changes needed
- NotFound: saved item is no longer available; close or reset editor honestly
- Cancelled/Failed: retain draft so Robin can retry

### Favourite Sailing and Ship

- Updated: reflect the new state
- Unchanged: synchronize to the existing state without false success
- NotFound for sailing: reset stale saved target
- Cancelled/Failed: preserve the previous confirmed state

Messages must not claim price recording, History changes, recommendation or
alert behaviour.

---

## Presentation

- keep capture review and History areas bounded and scrollable
- place Save Cruise beside the relevant existing action rather than in a remote
  global toolbar
- use a compact editor that does not displace or hide the embedded browser
- use existing theme resources and control styles
- show the target identity prominently so Robin knows which sailing is edited
- do not introduce a modal window requiring code-behind logic
- preserve mobile/desktop browser presentation and the Prompt 037j layout

The editor may be a new reusable Avalonia view bound to the child ViewModel.
Code-behind must remain initialization-only.

---

## Required Tests

Add deterministic offline tests proving:

### Snapshot Mapping

- capture observation maps every bounded snapshot field
- first advertised price is selected deterministically
- saved timestamp comes from the supplied clock value
- source/reference are optional and sailing identity ignores retailer
- mapping does not call the observation repository

### Single and Batch Capture

- successful capture can save without recording
- recording can occur without saving
- Ready candidate has an independent save command
- incomplete/failed candidate cannot save
- one saved candidate does not change batch selection or recording state
- Created, Updated, Unchanged, Cancelled and Failed messages are controlled
- replacement capture/candidate ignores stale completion and resets feedback

### History

- selected History queries saved state by sailing identity
- unsaved History exposes Save Cruise; saved History exposes Edit Evaluation
- save uses latest chronological observation
- source changes do not create another personal item
- History selection change cancels/ignores stale operations
- personal actions never call RecordCruiseObservation

### Editor

- existing evaluation populates every optional draft field
- Unrated maps to null and ratings map only 1–5
- notes validation prevents an over-limit save
- Cancel Changes restores confirmed values without persistence
- unchanged evaluation avoids a write through the Application outcome
- failed/cancelled evaluation retains the draft for retry
- favourite sailing and favourite ship update independently
- ship favourite is derived by operator/ship, not sailing date
- close/deactivate cancels work and ignores late results

### Regression

- existing capture, batch recording, History selection, trusted Open at TUI,
  grouping and browser-mode tests remain unchanged in behaviour
- desktop composition resolves the shared editor and all use cases
- no automated test contacts TUI, launches a browser or accesses Robin's data

Use fakes at the Application boundary. Persistence itself was verified in
038c; UI tests should not require SQLite except existing composition tests.

---

## Allowed Changes

```text
KrytenAssist.Application/Cruises/ (snapshot mapper only)
KrytenAssist.Application/DependencyInjection.cs
KrytenAssist.Avalonia/DependencyInjection/
KrytenAssist.Avalonia/ViewModels/CruiseSaveAndEvaluationViewModel.cs
KrytenAssist.Avalonia/ViewModels/CruiseCaptureCandidateReviewItemViewModel.cs
KrytenAssist.Avalonia/ViewModels/CruiseBrowserFeasibilityViewModel.cs
KrytenAssist.Avalonia/ViewModels/CruiseOfTheWeekViewModel.cs
KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml
KrytenAssist.Avalonia/Views/CruiseHistoryPanelView.axaml
KrytenAssist.Avalonia/Views/*Evaluation*.axaml
KrytenAssist.Avalonia/Views/*Evaluation*.axaml.cs
KrytenAssist.Avalonia.Tests/Application/Cruises/
KrytenAssist.Avalonia.Tests/ViewModels/
KrytenAssist.Avalonia.Tests/DependencyInjection/
docs/Codex Prompts/038d - Save Actions and Evaluation Editing.md
docs/AI Playbook/038 - Saved Cruises and Preferences.md
docs/Roadmap.md
```

Prompt 038b/c contracts or persistence may change only for a concrete defect
that blocks this workflow. Document and test any such correction. Do not stage,
commit, push, discard or overwrite unrelated work.

---

## Exclusions

- Saved Cruises list, filters, dismissal, restore or removal (Prompt 038e)
- general month, cabin and budget preference editor (Prompt 038f)
- recommendations, rankings, alerts or automatic inference
- new persistence schema or migration
- new provider, source, capture template or browser behaviour
- bulk Save All/Save Selected actions
- Prompt 039 or later work

---

## Verification

Run focused snapshot, save/editor, capture, batch, History and composition tests,
then:

```text
dotnet build KrytenAssist.sln --no-restore
dotnet test KrytenAssist.sln --no-build --no-restore
```

Use the stable single-worker runner when required. Tests must remain offline.
Manually verify saving one live captured TUI sailing and editing its evaluation
without Record Observation only after automated verification passes.

---

## Results

### Status

Complete.

### Implementation Summary

- Added explicit, independent Save Cruise actions for single capture, each
  Ready batch candidate and the selected recorded History sailing.
- Added an Application snapshot factory that deterministically maps the first
  displayed observation price and current injected clock time into personal
  saved state without recording History.
- Added one shared Cruise workspace evaluation editor with optional interest,
  ratings and notes plus independent sailing- and ship-favourite operations.
- Added cancellation and target-generation protection so cleared/replaced
  captures, History selection changes and workspace deactivation ignore stale
  asynchronous results.
- Added controlled Created, Updated, Unchanged, Cancelled, Failed and NotFound
  presentation outcomes while preserving draft values when retry is useful.

### Verification

- `dotnet build KrytenAssist.sln --no-restore`: passed with 0 errors. The seven
  reported warnings pre-date 038d (SQLitePCLRaw advisory and existing command
  event warnings).
- `KrytenAssist.Core.Tests`: 120 passed.
- `KrytenAssist.Avalonia.Tests`: 451 passed.
- `KrytenAssist.Api.Tests`: 9 passed.
- Total: 580 passed, 0 failed, 0 skipped.
- `git diff --check`: passed.

Live TUI saving and visual evaluation editing remain a manual reviewer check;
automated tests do not contact TUI or Robin's data.
