# Codex Prompt 038f – Cruise Preference Profile

## Implementation Prompt

Implement **Step 6 only** from:

```text
docs/AI Playbook/038 - Saved Cruises and Preferences.md
```

Prompts 038a–038e are complete. This step exposes the accepted, locally
persisted `CruisePreferences` aggregate through an explicit desktop editor. Do
not use those preferences to filter, score, rank, recommend or alert yet.

---

## Required Reading

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/038 - Saved Cruises and Preferences.md`
5. `docs/Codex Prompts/038a - Saved Cruise Experience and Contract.md`
6. `docs/Codex Prompts/038b - Personal Cruise Domain and Application Contracts.md`
7. `docs/Codex Prompts/038c - SQLite Personal-State Persistence.md`
8. `docs/Codex Prompts/038e - Saved Cruises Organisation.md`
9. accepted `CruisePreferences`, `CruiseBudget`, `CruiseCabinType` and
   `CruiseBudgetBasis` Core models and tests
10. existing `GetCruisePreferences`, `SaveCruisePreferences`, repository,
    SQLite adapter, composition and persistence tests
11. existing Saved Cruises workspace ViewModels, view and tests

---

## Agreed User Experience

### Placement

Keep the top-level Cruise modes accepted in 038e:

```text
[ Discovery ]   [ Saved Cruises ]
```

Inside Saved Cruises add a secondary choice:

```text
[ Saved list ]   [ Preferences ]
```

- Saved list remains the default and retains all 038e filters and behaviour.
- Preferences opens a distinct bounded editor within the Saved Cruises
  workspace rather than appearing against one selected sailing.
- do not add another global shell destination or a third top-level Cruise mode
- switching between Saved list and Preferences must not reload, mutate or
  re-filter saved cruises
- an unsaved preference draft survives switching between these two secondary
  modes during the current workspace lifetime
- leaving/deactivating the Cruise workspace cancels in-flight preference work
  but does not silently persist or discard an already loaded draft

The editor states prominently:

> Preferences are guidance only. They do not filter, rank or recommend cruises.

### Preferred Departure Months

Show all twelve named months as independent multi-select checkboxes in calendar
order January–December.

- zero, one or many months may be selected
- zero means **No preferred months set**; it does not mean no months are allowed
- selection order never affects stored equality or display order
- use full month names and do not infer a year or season

### Preferred Cabin Types

Show all accepted cabin categories as independent multi-select checkboxes in
the Core enum's deterministic order:

```text
Inside | Outside | Balcony | Suite | Solo
```

- zero, one or many cabin types may be selected
- zero means **No preferred cabin types set**
- preferences are broad cabin categories, not availability or booking claims

### Optional Maximum Budget

Use one explicit **Set a maximum budget** checkbox.

When unchecked:

- amount, currency and basis controls are disabled
- the draft maps to `MaximumBudget = null`
- no default budget is persisted

When checked, require all three fields:

```text
Amount:   [ 2500 ]
Currency: [ GBP ]
Basis:    [ Per person | Total booking ]
```

- amount is a non-negative decimal; zero remains valid because the accepted
  Core contract permits it
- accept the current desktop culture's normal decimal format, with invariant
  decimal input as a deterministic fallback
- currency is trimmed, converted to uppercase and must be exactly three ASCII
  letters
- basis is always explicit and maps only to `PerPerson` or `TotalBooking`
- a newly enabled empty budget may offer draft conveniences of `GBP` and
  `Per person`, but these are not persisted until Save Preferences
- never assume a passenger count or convert between per-person and total

### Explicit Draft Actions

Provide:

```text
[ Save Preferences ] [ Cancel Changes ] [ Clear Draft ]
```

- Save Preferences is the only persistence action.
- Cancel Changes restores the last successfully loaded or saved profile.
- Clear Draft selects no months, no cabins and no maximum budget, but remains
  an unsaved draft until Save Preferences is chosen.
- show an explicit Unsaved changes state whenever the draft differs from the
  last confirmed profile
- switching controls, modes or selected saved cruises never auto-saves
- an unchanged save reports honestly and performs no repository write through
  the existing Application outcome

Do not add Reset to Defaults: there are no inferred/default preference values.

---

## Validation and Controlled Feedback

Validate the complete draft at Save Preferences time before constructing Core
value objects or calling the use case.

When maximum budget is enabled:

- missing amount: `Enter a maximum budget amount.`
- invalid or negative amount: `Maximum budget must be a non-negative number.`
- missing/invalid currency: `Currency must contain exactly three letters.`
- invalid basis: `Choose whether the budget is per person or total booking.`

Validation errors retain every draft selection and do not call
`SaveCruisePreferences`. Display field-level errors where practical plus one
accessible summary. Do not throw presentation exceptions for ordinary invalid
input.

Map Application outcomes:

- Updated: `Cruise preferences saved.` and replace the confirmed baseline
- Unchanged: `These cruise preferences are already saved.` and synchronize the
  confirmed baseline
- Cancelled: `Saving cruise preferences was cancelled. You can try again.`
- Failed: `Cruise preferences could not be saved locally. Please try again.`

Cancelled and Failed saves retain the draft and Unsaved changes state.

Loading outcomes:

- Success: populate every month, cabin and budget field and establish a clean
  confirmed baseline
- Cancelled: controlled retryable message without replacing a previous draft
- Failed: controlled local error with **Try Again**, retaining previous loaded
  content when available

Provide a visible **Cancel Operation** action while loading or saving. It
cancels only preference work and does not affect Saved Cruises loading,
evaluation editing, browser navigation, capture or Recorded History.

---

## ViewModel and Lifecycle Architecture

Introduce one focused child ViewModel, for example:

```text
CruisePreferencesViewModel
CruisePreferenceMonthOptionViewModel
CruisePreferenceCabinOptionViewModel
```

The preference ViewModel owns:

- lazy first load through `GetCruisePreferences`
- one confirmed `CruisePreferences` baseline
- editable month, cabin and raw budget draft state
- deterministic dirty comparison
- validation and controlled messages
- Save, Cancel Changes, Clear Draft, Retry and Cancel Operation commands
- independent load/save cancellation and monotonically increasing generation

`SavedCruisesViewModel` owns the secondary Saved list/Preferences mode and the
injected preference child. It must not manipulate repositories or reconstruct
preference persistence itself.

Register the child through the existing desktop service extension. Do not add
manual registrations to `Program.cs` and do not use service location.

### Draft and Equality Rules

- construct the draft's `CruisePreferences` only after raw budget validation
- rely on the accepted Core aggregate for normalized deterministic value
  equality
- checkbox option ViewModels expose immutable identity/label plus mutable
  `IsSelected`; they do not persist themselves
- after successful load/save, normalize UI state from the confirmed Core value
- Cancel Changes likewise repopulates from the confirmed Core value
- Clear Draft does not change the confirmed baseline
- budget fields may retain their typed draft while temporarily disabled, but
  disabled state maps to null; Cancel/load restores confirmed display values
- empty confirmed preferences are a legitimate loaded profile, not an error

### Activation

- selecting Preferences lazily loads once when no successful baseline exists
- returning to Preferences after a successful load preserves its current draft,
  including unsaved changes
- Try Again explicitly retries a cancelled/failed first load
- do not automatically refresh and overwrite a dirty draft
- deactivation increments generation, cancels active work and ignores late
  results without clearing the confirmed baseline or draft

Late load/save results must not overwrite a newer draft, clear validation,
report success after cancellation or update a later workspace activation.

---

## Presentation

- keep the editor bounded and vertically scrollable at tablet/desktop sizes
- group Months, Cabin types and Maximum budget into clear visual sections
- use existing theme resources and control styles
- show selected-value summaries such as `3 preferred months` and
  `2 preferred cabin types`, while retaining the explicit checkboxes
- use text as well as selection state; do not communicate meaning by colour
- keep Save/Cancel/Clear actions visible without displacing the embedded
  browser, which remains hidden but undisturbed in Saved Cruises mode
- no code-behind logic beyond `InitializeComponent`

---

## Required Tests

Add deterministic offline tests covering:

### Loading and population

- first Preferences activation loads exactly once
- all persisted months and cabins populate in deterministic accepted order
- a persisted budget populates amount, uppercase currency and exact basis
- an empty profile shows explicit unset states and a clean draft
- returning to the editor preserves unsaved draft state without reloading
- cancelled/failed load is controlled and retry succeeds
- deactivation ignores late load completion

### Draft and validation

- selecting multiple months/cabins constructs the exact Core profile
- option selection order does not affect dirty comparison
- no months/cabins is valid and explicitly displayed as unset
- disabled budget maps to null and does not persist draft convenience values
- enabled budget requires amount, currency and basis
- negative/non-numeric amount and invalid currency prevent use-case invocation
- valid amount is parsed deterministically, currency is normalized uppercase
- zero budget remains valid
- Clear Draft changes only draft state
- Cancel Changes restores every confirmed field and clears validation

### Saving and lifecycle

- only Save Preferences calls the save use case
- Updated establishes a clean confirmed baseline
- Unchanged reports honestly and becomes/remains clean
- Cancelled/Failed retains the dirty draft for retry
- Cancel Operation cancels only preference work
- a draft change during an in-flight operation invalidates/ignores stale result
- secondary mode and workspace deactivation preserve unsaved draft but cancel
  active work
- preferences never change Saved Cruises filter membership, observations,
  favourite ships or browser state

### Persistence and composition

- isolated SQLite save through the Application use case survives a fresh
  DbContext/repository and loads through `GetCruisePreferences`
- clearing all fields persists the accepted empty profile across restart
- the preference ViewModel and its use cases resolve through desktop DI
- the Saved Cruises workspace receives the same registered preference child
- no test contacts TUI, launches a browser, accesses Robin's database or uses
  the network

Retain the 038c atomic repository tests. Do not weaken or duplicate schema
coverage merely to test bindings.

---

## Allowed Changes

```text
KrytenAssist.Avalonia/DependencyInjection/
KrytenAssist.Avalonia/ViewModels/SavedCruisesViewModel.cs
KrytenAssist.Avalonia/ViewModels/CruisePreference*.cs
KrytenAssist.Avalonia/Views/SavedCruisesView.axaml
KrytenAssist.Avalonia/Views/CruisePreference*.axaml
KrytenAssist.Avalonia/Views/CruisePreference*.axaml.cs
KrytenAssist.Avalonia.Tests/ViewModels/
KrytenAssist.Avalonia.Tests/DependencyInjection/
KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/
docs/Codex Prompts/038f - Cruise Preference Profile.md
docs/AI Playbook/038 - Saved Cruises and Preferences.md
docs/Roadmap.md
```

Core, Application and Infrastructure production contracts may change only for
a concrete defect that blocks the accepted editor workflow. Document and test
any such correction. Do not stage, commit, push, discard or overwrite unrelated
work.

---

## Exclusions

- filtering or sorting Saved Cruises using preferences
- recommendation, ranking, scoring or automatic preference inference
- alerts, notifications or price-drop behaviour
- passenger counts, party composition or budget conversion
- cabin availability, specific cabin numbers or booking flows
- seasonal presets or inferred/default months and cabins
- multiple named preference profiles
- automatic save, reset-to-defaults or cloud synchronization
- new provider, retailer, capture, browser, History or persistence schema work
- Prompt 039 or later work

---

## Verification

Run focused preference ViewModel, lifecycle, persistence and composition tests,
then:

```text
dotnet build KrytenAssist.sln --no-restore
dotnet test KrytenAssist.sln --no-build --no-restore
```

Use the established single-worker runner when required. Tests remain offline.
After automated verification, manually confirm multi-month/multi-cabin editing,
budget enable/disable and validation, Cancel Changes, Clear Draft, save/restart
and that Saved Cruises filters remain unchanged.

---

## Results

### Status

Complete.

### Implementation Summary

- Added Preferences as a secondary mode inside Saved Cruises while preserving
  the accepted Discovery/Saved Cruises top-level workspace.
- Added deterministic January–December and Inside/Outside/Balcony/Suite/Solo
  multi-select option ViewModels with explicit unset summaries.
- Added an optional maximum-budget editor with amount, normalized three-letter
  currency and explicit per-person/total-booking basis.
- Added confirmed-baseline and raw-draft state, dirty tracking, field-level
  validation, explicit Save Preferences, Cancel Changes and Clear Draft
  actions.
- Added independent load/save cancellation and generation protection. Failed,
  cancelled and invalid saves retain the draft for retry; switching secondary
  modes preserves unsaved state without automatically reloading or saving.
- Kept preferences informational only: Saved Cruises filters, observations,
  favourite ships and browser state are not read or changed by the editor.
- Added desktop composition and isolated SQLite use-case restart/clear coverage
  without changing Core, Application, Infrastructure or schema contracts.

### Files Added

- `KrytenAssist.Avalonia/ViewModels/CruisePreferenceMonthOptionViewModel.cs`
- `KrytenAssist.Avalonia/ViewModels/CruisePreferenceCabinOptionViewModel.cs`
- `KrytenAssist.Avalonia/ViewModels/CruisePreferencesViewModel.cs`
- `KrytenAssist.Avalonia/Views/CruisePreferencesView.axaml`
- `KrytenAssist.Avalonia/Views/CruisePreferencesView.axaml.cs`
- `KrytenAssist.Avalonia.Tests/ViewModels/CruisePreferencesViewModelTests.cs`
- `docs/Session Handovers/2026-07-18 Session 023.md`

### Files Updated

- `KrytenAssist.Avalonia/DependencyInjection/DesktopPersistenceServiceCollectionExtensions.cs`
- `KrytenAssist.Avalonia/ViewModels/SavedCruisesViewModel.cs`
- `KrytenAssist.Avalonia/Views/SavedCruisesView.axaml`
- `KrytenAssist.Avalonia.Tests/Application/Cruises/FakePersonalCruiseRepositories.cs`
- `KrytenAssist.Avalonia.Tests/ViewModels/SavedCruisesViewModelTests.cs`
- `KrytenAssist.Avalonia.Tests/DependencyInjection/CruiseHistoryDesktopCompositionTests.cs`
- `KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/PersonalCruisePersistenceTests.cs`
- this prompt, the Prompt 038 playbook and Roadmap

### Build and Tests

- `dotnet build KrytenAssist.sln --no-restore --disable-build-servers -m:1`:
  passed with 0 errors. Five existing SQLitePCLRaw advisory warnings remain.
- focused preference, Saved Cruises, composition and persistence scenarios: 23
  covered by the passing Avalonia suite
- Core: 120 passed
- Avalonia: 469 passed
- API: 9 passed
- total: 598 passed, 0 failed, 0 skipped
- `git diff --check`: passed

Automated tests remained offline and did not contact TUI, launch a browser or
use Robin's database. Manual verification of the rendered editor, validation,
save/restart and unchanged Saved Cruises filters remains for Robin.

### Next

Prompt 038g – Tests and Verification.
