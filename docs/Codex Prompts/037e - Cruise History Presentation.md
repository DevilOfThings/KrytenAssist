# Codex Prompt 037e – Cruise History Presentation

## Implementation Prompt

Implement **Step 5 only** from:

```text
docs/AI Playbook/037 - Cruise History and Price Tracking.md
```

Prompts 037a–037d are complete and committed.

This is the first Prompt 037 step that exposes Cruise History to Robin.

Use the agreed boundary:

- 037a–037d own domain identity/analysis, Application use cases and durable
  concurrent SQLite persistence
- 037e owns desktop composition, Record Observation interaction and local
  history list/detail presentation
- 037f will complete remaining cross-layer and Avalonia edge-case coverage
- 037g will perform final verification

Do not implement Prompt 037f or 037g early.

---

## Required Reading

Read these files in order before changing code:

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/037 - Cruise History and Price Tracking.md`
5. `docs/Codex Prompts/037a - Cruise History Domain.md`, including Results and
   Lessons Learned
6. `docs/Codex Prompts/037b - Cruise History Application Contract.md`, including
   Results and Lessons Learned
7. `docs/Codex Prompts/037c - Cruise History SQLite Persistence.md`, including
   Results and Lessons Learned
8. `docs/Codex Prompts/037d - Observation Recording and History Queries.md`,
   including Results and Lessons Learned
9. all existing Cruise Application use cases and results
10. `KrytenAssist.Avalonia/Program.cs`
11. `KrytenAssist.Avalonia/appsettings.json`
12. `KrytenAssist.Avalonia/DependencyInjection/ShellServiceCollectionExtensions.cs`
13. `KrytenAssist.Avalonia/ViewModels/CruiseOfTheWeekViewModel.cs`
14. `KrytenAssist.Avalonia/ViewModels/CruiseBrowserFeasibilityViewModel.cs`
15. `KrytenAssist.Avalonia/ViewModels/ShellViewModel.cs`
16. `KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml`
17. its code-behind, only to preserve the native-browser boundary
18. all existing Cruise capture lifecycle/review and shell tests

Do not begin implementation until the capture-review lifecycle, 037b result
states, 037d latest-evidence contract and current desktop composition gap are
understood.

---

## Important Existing Composition Gap

The Avalonia desktop currently does **not** call:

```text
Application.AddApplication
Infrastructure.AddInfrastructure
```

Starting Kryten after 037c/037d therefore did not resolve the Cruise repository
or apply its migrations through the desktop composition root. The app remained
healthy because Cruise History was not yet used by Avalonia.

037e must close this expected gap safely.

Do not report desktop migration/startup verification until the Avalonia process
actually resolves the history services against its configured local database.

---

## Goal

Allow Robin to deliberately record a successfully captured Cruise observation,
review its price-history outcome and revisit all locally recorded Cruise
histories without loading a browser or contacting TUI.

This step owns:

- registering the 037b use cases and analyzer through Application DI
- composing Application and Infrastructure in Avalonia
- a deterministic writable desktop database location
- automatic migration through the existing Infrastructure path
- a focused Cruise History presentation ViewModel
- explicit Record Observation and Cancel Recording commands
- controlled first/changed/already-current/cancelled/failed presentation
- automatic local history loading when Cruise of the Week is selected
- Refresh History, loading, empty, failure and retry states
- deterministic recorded-history list and selection
- price summary/trend and latest-evidence detail presentation
- capture-review versus persisted-history lifecycle separation
- focused ViewModel, composition and binding tests

This step does **not** own:

- changing TUI capture selectors or page parsing
- changing browser navigation behavior
- reinterpreting original versus discounted price
- a new repository or second database
- deletion, export, charts, advanced search or filtering
- ratings, notes, favourites or preferences
- monitoring, notifications or booking
- Prompt 038 behavior

---

## Allowed Changes

Production changes should be limited to the smallest relevant files under:

```text
KrytenAssist.Application/DependencyInjection.cs
KrytenAssist.Avalonia/Program.cs
KrytenAssist.Avalonia/appsettings.json
KrytenAssist.Avalonia/DependencyInjection/
KrytenAssist.Avalonia/ViewModels/
KrytenAssist.Avalonia/Views/
```

Create focused presentation models/ViewModels in a cohesive Avalonia folder if
that keeps the existing browser ViewModel small.

Create or update tests under:

```text
KrytenAssist.Avalonia.Tests/ViewModels/
KrytenAssist.Avalonia.Tests/DependencyInjection/
```

Reuse 037b hand-written fakes or add focused presentation fakes. Do not use real
SQLite in pure ViewModel tests.

Update this prompt after implementation:

```text
docs/Codex Prompts/037e - Cruise History Presentation.md
```

Do not modify Core or Infrastructure persistence behavior unless a focused test
proves a genuine contract defect. Any correction must be minimal, tested and
reported.

Do not modify the Roadmap, Playbook, Backlog or session handovers.

Do not stage, commit, push, discard or overwrite Robin's work.

---

## Architecture Boundary

Preserve:

```text
Core
  ↑
Application
  ↑
Infrastructure
  ↑
Avalonia composition and presentation
```

Avalonia presentation may consume:

- `RecordCruiseObservation`
- `GetCruiseHistory`
- `ListCruiseHistories`
- controlled Application result types
- Core summary values returned through Application

Avalonia ViewModels must not consume:

- `ICruiseObservationRepository`
- `KrytenAssistDbContext`
- EF entities
- migrations
- SQLite types or error codes
- connection strings
- TUI HTML/DOM payloads for history

Views bind commands and presentation state only.

Code-behind must not:

- resolve Application use cases or repositories
- record observations
- query history
- calculate price trends
- construct persistence entities

The native-browser code-behind remains concerned only with the browser,
JavaScript capture bridge and trusted external-navigation boundary.

---

## Application Dependency Injection

Extend the existing Application DI extension to register:

```text
CruisePriceHistoryAnalyzer
RecordCruiseObservation
GetCruiseHistory
ListCruiseHistories
```

Use architecture-appropriate lifetimes compatible with the scoped repository.
Do not manually construct these use cases in Avalonia.

Keep the existing PromptCard Application registrations and validators intact.

Add a focused Application/Avalonia composition test proving all three history
use cases resolve with the expected dependencies.

---

## Avalonia Database Composition

Update the Avalonia composition root to call the existing Application and
Infrastructure registration extensions.

The desktop database must use a deterministic writable per-user location, not an
accidental current working directory or `AppContext.BaseDirectory` database.

Prefer a location based on:

```text
Environment.SpecialFolder.LocalApplicationData
+ KrytenAssist
+ krytenassist.db
```

Create the application-data directory before Infrastructure migration.

Build or override the existing connection setting at composition time so
`AddInfrastructure` receives the desktop path through configuration. Continue
using the existing connection-string key expected by Infrastructure unless a
verified design constraint requires a minimal rename.

Do not:

- place the database beside the application executable
- use Robin's test/project working directory by accident
- introduce a second Cruise database
- replace the existing JSON `IPromptCardStore`
- switch the Avalonia prompt library to the API `IPromptCardRepository`
- use `EnsureCreated()`
- silently delete/recreate a failed database

The Infrastructure migration initializer must remain the only production schema
initialization path.

Add an isolated composition test with a temporary path. The test must not touch
the real per-user desktop database.

---

## Recommended ViewModel Composition

Keep Cruise History responsibilities out of the already-large browser ViewModel.

Prefer a focused child such as:

```text
CruiseHistoryViewModel
```

owned or exposed by `CruiseBrowserFeasibilityViewModel` so the existing Cruise
Discovery view can bind to it.

The child should own:

- current captured observation supplied by the capture lifecycle
- record/cancel commands and recording generation
- record outcome state/message
- local history load/refresh/cancel state
- read-only history presentation items
- selected history item/detail
- summary and trend formatting

It must depend on Application use cases, not persistence.

Small immutable presentation items such as
`CruiseHistoryItemViewModel` are permitted. Do not reproduce domain analysis in
them; format values already calculated by Core/Application.

Existing ViewModel constructors may retain optional dependencies only where
needed by the Avalonia designer and existing isolated tests. Production DI must
provide the complete history child and use cases.

---

## Activation and Local Loading

When Cruise of the Week is selected in Shell navigation, trigger a safe
ViewModel-owned history activation/load.

Do not start unobserved persistence work from a constructor.

A suitable flow is:

```text
Shell selects Cruise of the Week
→ CruiseOfTheWeekViewModel activates
→ CruiseHistoryViewModel executes EnsureLoaded/Load
```

Use the existing async-command style or an equivalent observed async boundary.

On navigation away:

- cancel an active history load where practical
- do not clear loaded history
- do not delete or mutate persistence

Returning to Cruise of the Week may retain the list and offer explicit refresh.
The first selection in a process must load local history automatically.

History loading must not:

- open or select a Cruise source
- load TUI
- start the embedded browser
- execute a Skill
- revisit stored source references

---

## Capture Review Integration

When capture succeeds, provide the exact captured `CruiseObservation` to the
history presentation state.

When capture state clears because of:

- Back
- Forward
- Refresh
- Close Browser
- changing/opening a source
- a new capture attempt

clear the transient record state associated with that review and cancel any
active record operation for that review.

Do **not** clear:

- loaded history items
- selected persisted history
- persisted records

Use a generation/version guard so a late record completion for an older capture
cannot overwrite state belonging to a newer capture.

The existing capture review must remain visible after successful recording so
Robin can see what was recorded.

Do not rename the factual action to `Save Cruise`; use:

```text
Record Observation
```

---

## Record Observation Command

Expose:

```text
RecordObservationCommand
CancelRecordingCommand
```

Record is enabled only when:

- a successful captured observation exists
- no record operation is active
- that exact captured meaningful observation has not already completed a record
  action in the current review lifecycle

During recording:

- disable duplicate Record actions
- show `Recording this observation...`
- keep the UI responsive
- enable Cancel Recording
- perform no browser or network work

Call `RecordCruiseObservation.ExecuteAsync` exactly once per command execution.

After a controlled success:

- retain the capture review
- refresh local history
- select the matching sailing/source entry
- show the exact controlled outcome
- disable or relabel Record while this captured review remains current

A newly captured observation resets the completed-record state and may be
recorded.

---

## Record Outcome Presentation

### First Observation

Use clear language such as:

```text
Observation recorded.
This is the first price seen for this sailing from TUI.
```

### Changed Observation

When comparable and lower:

```text
New observation recorded.
The comparable price is £39 lower than the previous recorded price.
```

Provide equivalent controlled language for:

- higher
- unchanged comparable price with another meaningful advertised change
- comparison unavailable

Do not claim that a price changed when only promotion/title/itinerary evidence
changed and comparable price remained equal.

### Already Current

```text
No new snapshot was needed.
This advertised observation matches the latest recorded values.
```

The repository may still have advanced latest booking evidence/LastSeenAt.

### Cancelled

Use a neutral controlled message. Keep the review available and allow retry.

### Failed

```text
The observation could not be recorded. Please try again.
```

Keep the review and enable retry. Do not expose exception, SQL, connection
string, path, EF or SQLite details.

---

## Recorded Cruise History Area

Add a clearly separate local area inside Cruise Discovery:

```text
Recorded Cruise History
```

It must remain available when the embedded browser is closed.

Provide:

- automatic first load on feature activation
- Refresh History command
- Cancel Loading where useful
- loading state
- successful empty state
- loaded list state
- controlled cancelled state
- controlled failed state with retry

Empty text should explain:

```text
No cruise observations have been recorded yet.
Capture a cruise and choose Record Observation to begin its price history.
```

Do not treat empty history as an error.

Retain the last successfully loaded collection if a later refresh fails, while
showing a controlled refresh error.

---

## History List Presentation

Each history item should expose concise provider-independent display values:

- cruise title from the deterministic current observation
- operator
- ship
- departure date
- duration
- retail source or a controlled source-unavailable label
- current comparable price or unavailable label
- observation count
- last seen
- price trend
- whether the sailing departure date is in the past

Do not use the system clock inside pure formatting. If past/future labeling is
shown, use the existing injected `IClock` and its date/time-zone conventions.

Preserve Application ordering. Do not silently remove past sailings.

Multiple retail sources for the same sailing must appear as separate items.

Selection must be stable by sailing/source identity, not by list index or
database id.

---

## Selected History Detail

Selecting a history item should show at least:

```text
Price History

First observed          16 July 2026
Last observed           23 July 2026
Last seen               25 July 2026
Current price           £949 per person
Lowest price            £949 per person
Highest price           £988 per person
Recorded observations   2
Trend                    Down £39
Retail source            TUI
```

Also display where useful:

- title
- operator
- ship
- departure date
- duration
- latest provider offer id
- latest source reference as read-only/copyable text

Keep `Last observed` and `Last seen` distinct.

Do not add deletion or editing controls.

Do not automatically open the latest source reference. A later prompt may add a
trusted explicit revisit action.

---

## Price and Trend Formatting

Use one consistent `en-GB` presentation policy.

Format GBP as `£`; retain other ISO currencies visibly. Preserve basis, for
example:

```text
£949 per person
£1,975 total
```

Trend labels should cover:

- First observation
- Down £39
- Up £39
- Unchanged
- Comparable price unavailable

When the Core summary has no comparable price, show:

```text
Comparable price history is unavailable for this observation.
```

Do not calculate lowest/highest/trend in Avalonia. Format the
`CruisePriceHistorySummary` values supplied by Application.

Known limitation: the current TUI capture may represent the original advertised
price rather than the discounted price. Do not label it discounted or payable.
The later pricing-model correction remains deferred for separately representing:

- original price
- discounted price
- per-person discount
- booking-level extra discount
- final booking total

---

## View Layout

Extend `CruiseBrowserFeasibilityView.axaml` without obscuring or shrinking the
embedded page into an unusable area.

Keep the layout scrollable and usable at the existing tablet-sized desktop
window.

Recommended visual hierarchy:

```text
Cruise Discovery controls/status
Embedded browser when open
Captured cruise review + Record Observation
Recorded Cruise History list
Selected Price History detail
```

The history area must remain visible/accessible with the browser closed.

Use existing theme classes and restrained surfaces. Do not introduce a new theme
or unrelated shell redesign.

Prefer bindings, `ItemsControl`/selection controls and DataTemplates. Do not add
persistence event handlers to code-behind.

---

## Busy, Cancellation and Generation Rules

Recording and history loading are separate operations but must not race the same
presentation state.

At minimum:

- prevent duplicate record execution
- prevent duplicate refresh execution
- cancel/dispose replaced cancellation sources
- ignore late completion after cancellation or new capture generation
- raise command `CanExecuteChanged` and all dependent property notifications
- never leave busy state stuck after exception
- keep record failure separate from history-refresh failure

Navigation/browser actions must remain responsive while local persistence work
is running.

---

## Desktop Composition Tests

Add deterministic tests proving:

- Application DI resolves analyzer plus Record/Get/List use cases
- Avalonia history presentation resolves through the shell composition graph
- Infrastructure resolves the SQLite repository
- migrations run against a supplied isolated temporary database
- the production desktop path builder is deterministic and uses per-user local
  application data
- tests can override the path and never touch Robin's real database
- resolving history performs no TUI/HTTP/browser work
- existing JSON `IPromptCardStore` remains registered and usable

If `Program.BuildServices` is currently private and untestable, extract the
smallest composition helper rather than using reflection or launching the real
desktop process in a unit test.

Do not duplicate Infrastructure registrations inside tests.

---

## ViewModel Tests

Use hand-written fake use cases only if the existing concrete use cases cannot be
controlled cleanly; prefer their Application-owned abstractions and established
test conventions. Do not mock EF or use real SQLite in pure ViewModel tests.

Cover at least:

### Record Lifecycle

- disabled without a successful captured observation
- exact observation passed once
- busy state prevents duplicate recording
- Cancel Recording cancels the token
- late completion after cancellation/new capture is ignored
- FirstObservationRecorded message/state
- ChangedObservationRecorded lower message/state
- ChangedObservationRecorded higher message/state
- meaningful change with unchanged comparable price
- comparison unavailable
- AlreadyCurrent message/state
- controlled Cancelled and Failed allow retry
- captured review remains after success/failure
- successful record refreshes history and selects matching sailing/source
- newly captured observation resets completed record state
- capture clear cancels recording but leaves persisted list/selection

### History Loading

- first Cruise navigation activation loads once
- returning without refresh does not duplicate load unnecessarily
- explicit Refresh loads again
- loading prevents duplicate refresh
- successful empty state
- successful past/future histories
- multiple retail sources stay separate
- source-less history is controlled
- list result mutation cannot affect presentation collection
- deterministic selection by sailing/source identity
- selected detail exposes all required summary/latest-evidence values
- refresh retains/reselects matching item where possible
- cancellation is neutral and clears busy state
- failure is safe, retryable and retains the last successful list
- no system clock controls repository output
- no network/browser/Skill work occurs

### Formatting

- GBP and non-GBP prices
- per-person and total basis
- first/lower/higher/unchanged/unavailable trend
- first observed, last observed and last seen remain distinct
- observation count
- optional latest source reference
- known captured price is never labelled discounted

### Shell/Capture Integration

- selecting Cruise of the Week activates history loading
- selecting another destination cancels active loading where implemented
- successful capture supplies the exact observation to history state
- Back/Forward/Refresh/Close/new source clears record-review state only
- browser close leaves local history list and selected history intact

---

## XAML and Binding Verification

Add focused tests or build-time compiled-binding coverage proving the new AXAML
bindings target real properties/commands.

At minimum verify:

- Record Observation visibility and command
- recording/cancel states
- record outcome message
- history loading/empty/error states
- history items and selection
- selected summary fields
- latest source reference

Do not introduce value converters containing history business logic. Small
presentation-only converters should be avoided when a typed ViewModel property is
clearer and easier to test.

---

## Manual Verification

After automated tests pass, report these exact manual checks for Robin:

1. Start Kryten and select Cruise of the Week.
2. Confirm empty or previously persisted Recorded Cruise History loads without
   opening TUI.
3. Open the TUI source, navigate to the current offer and capture it.
4. Confirm Record Observation appears only after successful capture.
5. Record it and confirm first/changed/already-current controlled message.
6. Confirm the matching history is selected and summary values appear.
7. Close the browser and confirm captured review clears while history remains.
8. Leave and return to the Cruise feature; confirm history remains available.
9. Restart Kryten and confirm history reloads from the desktop database.
10. Capture/record the same advertised observation and confirm no snapshot count
    increase.
11. Confirm no price is described as discounted unless the model explicitly
    represents that fact.

Do not claim a changed-price manual check unless the live source actually
provides changed evidence. Automated tests own that deterministic proof.

---

## Error and Privacy Requirements

All history operations are local and offline.

Do not expose in UI or logs intended for Robin:

- connection strings
- database paths
- SQL
- EF/SQLite type names
- stack traces
- cookies or browser storage
- full page HTML
- passenger or booking personal data

Do not revisit stored URLs to validate a price.

Migration failure must remain visible through startup failure behavior; never
silently delete or recreate the database.

---

## Production Corrections

Do not redesign 037a–037d.

Expected composition correction:

- Avalonia must now compose Application and Infrastructure because 037e is the
  first desktop consumer of Cruise History

For any additional verified defect:

1. prove it with a focused failing test
2. make the smallest architecture-consistent correction
3. run the affected regression suite
4. report it under Results

---

## Required Commands

Before implementation, inspect the worktree and preserve unrelated changes.

Run focused ViewModel and composition tests using the exact project/filter
selected during implementation.

Build the solution:

```text
dotnet build KrytenAssist.sln --no-restore
```

Run the complete regression suite:

```text
dotnet test KrytenAssist.sln --no-build --no-restore
```

Report exact totals, failures, skipped tests, errors and warnings.

All automated tests must remain offline and isolated from Robin's desktop
database.

---

## Definition of Done

Prompt 037e is complete when:

- Application DI registers analyzer and all Cruise History use cases
- Avalonia composes Application and Infrastructure
- desktop SQLite uses a deterministic writable per-user path
- tests can override the database path and never touch the real file
- the existing migration path runs from desktop composition
- Record Observation appears only for successful capture review
- record busy/cancel/outcome/retry states are controlled
- first/changed/already-current outcomes are clear and accurate
- successful record refreshes and selects the affected history
- browser/capture review lifecycle cannot delete persisted history
- first Cruise activation loads local history without TUI/browser work
- empty/loading/error/retry list states are present
- past, future, multi-source and source-less histories are retained
- selected history displays all required summary and latest-evidence values
- price/trend formatting uses Application/Core results
- original price is not falsely labelled discounted
- views remain passive and browser code-behind contains no persistence logic
- focused ViewModel/composition/binding tests pass
- complete solution builds
- complete regression suite passes
- manual verification instructions are reported
- this prompt's Results and Lessons Learned are complete

Do not begin Prompt 037f.

Stop after Prompt 037e.

---

## Completion Report

Provide:

### Summary

Describe Record Observation, local history and selected detail behavior.

### Desktop Composition

Report Application/Infrastructure registration, database location strategy and
migration startup behavior.

### ViewModel and Lifecycle

Report activation, recording, cancellation, refresh, selection and capture-clear
behavior.

### View

Report history layout, states and formatted values.

### Files Modified

List every created and updated file.

### Production Corrections

Use `None` or report each verified correction.

### Build and Tests

Report exact commands and totals.

### Manual Verification

List the checks Robin should perform and any live-data limitation.

### Architecture and Scope

Confirm no persistence logic entered ViewModels/views and no future pricing or
Prompt 038 behavior was added.

---

## Results

> Complete during implementation.

### Status

Complete. Automated verification passed and Robin confirmed the required live
desktop persistence behavior.

### Desktop Composition

Added a focused Avalonia desktop persistence composition extension. It registers
the existing Application Cruise History analyzer and use cases, composes the
existing Infrastructure SQLite repository and migration runner, and registers
the Cruise History presentation ViewModel.

The production database is stored at the platform Local Application Data path
under `KrytenAssist/krytenassist.db`. Tests can supply an isolated database path,
so they never use Robin's desktop database. Existing migrations run through the
normal Infrastructure composition path when the desktop service provider starts.

### Record Observation

Successful capture review now offers Record Observation. Recording passes the
captured observation unchanged to the Application use case, supports cancellation,
prevents repeat recording of the same completed review and reports controlled
first, lower, higher, unchanged, unavailable, already-current, cancelled and
failure outcomes. A successful record refreshes the list and selects the affected
history.

### History Loading and Selection

Cruise History loads from local storage on the first Cruise of the Week
activation without opening or navigating the browser. Explicit refresh and
cancellation are supported, loading/empty/error states are distinct, and a
failed refresh retains the last good list. Selection is retained by canonical
cruise key and source when possible.

### Price History Presentation

The selected history displays identity, operator, ship, departure, duration,
source, first/last observation times, last-seen time, current/lowest/highest known
price, observation count, trend, latest offer id and latest source reference.
GBP and non-GBP formatting is culture-aware, unavailable prices remain explicit,
and the known captured price is not labelled as a discounted price.

### Capture and Browser Lifecycle

Changing source, refreshing, closing or navigating the browser clears only the
current capture review and cancels stale recording work. Persisted histories and
the last successfully loaded list remain intact. The existing browser code-behind
was not given persistence or history responsibilities.

### Files Created

- `KrytenAssist.Avalonia/DependencyInjection/DesktopPersistenceServiceCollectionExtensions.cs`
- `KrytenAssist.Avalonia/ViewModels/CruiseHistoryItemViewModel.cs`
- `KrytenAssist.Avalonia/ViewModels/CruiseHistoryViewModel.cs`
- `KrytenAssist.Avalonia.Tests/DependencyInjection/CruiseHistoryDesktopCompositionTests.cs`
- `KrytenAssist.Avalonia.Tests/ViewModels/CruiseHistoryViewModelTests.cs`
- `docs/Codex Prompts/037e - Cruise History Presentation.md`

### Files Updated

- `KrytenAssist.Application/DependencyInjection.cs`
- `KrytenAssist.Avalonia/Program.cs`
- `KrytenAssist.Avalonia/ViewModels/CruiseBrowserFeasibilityViewModel.cs`
- `KrytenAssist.Avalonia/ViewModels/CruiseOfTheWeekViewModel.cs`
- `KrytenAssist.Avalonia/ViewModels/ShellViewModel.cs`
- `KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml`

### Production Corrections

The desktop application had not yet composed the Application and Infrastructure
Cruise History registrations, so its SQLite migration and repository path were
not available to the presentation layer. Desktop composition now uses the
existing layer extension methods and a deterministic writable per-user path.
This is the expected 037e integration correction; no persistence implementation
was duplicated or moved into Avalonia.

### Build

Passed:

```text
dotnet build KrytenAssist.sln --no-restore
```

Result: 0 errors and 5 warnings. All five warnings are the existing NU1903
advisory for `SQLitePCLRaw.lib.e_sqlite3` 2.1.11.

### Focused Tests

Passed 10 focused Cruise History presentation and desktop-composition tests with
0 failures and 0 skipped tests. They cover recording outcomes, retry/cancellation,
refresh/selection, formatting, browser lifecycle, shell activation and isolated
SQLite composition.

### Complete Regression Suite

Passed:

```text
dotnet test KrytenAssist.sln --no-build --no-restore
```

- Core: 105 passed, 0 failed, 0 skipped
- Avalonia: 325 passed, 0 failed, 0 skipped
- API: 9 passed, 0 failed, 0 skipped
- Total: 439 passed, 0 failed, 0 skipped

### Manual Verification

Passed. Robin manually confirmed that a captured cruise price can be recorded and
appears in Cruise History. Robin then restarted Kryten and confirmed that the
recorded history remained available, proving that the desktop presentation is
using the persistent SQLite store rather than session-only state.

The automated presentation tests additionally cover first activation without
browser navigation, empty/loading/error states, refresh and selection, changed
and already-current outcomes, browser capture clearing, unavailable prices and
source-less records. Live capture remains dependent on the current TUI page and
embedded-browser behavior; automated tests remain offline.

### Architecture and Scope Check

Confirmed. Application owns the use cases and analysis, Infrastructure owns
SQLite, and Avalonia owns only composition and presentation. Views are passive,
ViewModels do not use SQL or repository service location, and browser code-behind
contains no history logic. No discounted/original price redesign, ranking,
recommendations, Prompt 037f or Prompt 038 behavior was introduced.

### Notes

The desktop database is deliberately separate from source-controlled project
files and is created beneath the current user's Local Application Data directory.
The existing JSON prompt-card store remains unchanged. The known SQLite package
advisory predates this prompt and should be handled separately rather than by
expanding 037e.

---

## Lessons Learned

> Complete after implementation.

- Desktop presentation needs an explicit composition root even when Application
  and Infrastructure registrations already exist for another host such as the
  API.
- Treating capture review and persisted history as separate lifecycle state keeps
  browser navigation from accidentally hiding or deleting useful local data.
- Stable selection requires both the canonical cruise key and source because the
  same itinerary can be observed through multiple providers.
- A single known captured price must remain neutral until the domain explicitly
  models original price, discounted price, per-person discount and booking-level
  discount.
- Injecting the clock and database path makes time-sensitive presentation and
  startup migration behavior deterministic without touching Robin's real data.
