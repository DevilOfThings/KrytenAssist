# Codex Prompt 039f – Alert Centre and Settings

## Implementation Prompt

Implement **Step 6 only** from:

```text
docs/AI Playbook/039 - Price Drop Alerts.md
```

Prompts 039a–039e are complete. Alert domain, deterministic detection,
normalized SQLite persistence, explicit Record integration and Saved Criteria
triggers already exist.

This step exposes the durable local Alert Centre, lifecycle actions, unread
badge and alert settings inside the Cruise workspace.

Do not perform the final Prompt 039 audit or manual verification yet. Those
belong to 039g.

---

## Required Reading

Read these files in order before changing code:

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/035 - Dashboard and Navigation.md`
5. `docs/AI Playbook/037 - Cruise History and Price Tracking.md`
6. `docs/AI Playbook/038 - Saved Cruises and Preferences.md`
7. `docs/AI Playbook/039 - Price Drop Alerts.md`
8. `docs/Codex Prompts/039a - Price Drop Alert Experience and Contract.md`
9. `docs/Codex Prompts/039b - Alert Domain and Application Contracts.md`,
   including Results
10. `docs/Codex Prompts/039c - SQLite Alert Persistence.md`, including Results
11. `docs/Codex Prompts/039d - Observation Recording Integration.md`, including
    Results and Lessons Learned
12. `docs/Codex Prompts/039e - Saved Criteria Evaluation.md`, including Results
    and Lessons Learned
13. existing alert query, count, lifecycle and settings use cases/results
14. `CruiseAlert`, all typed detail models and `CruiseAlertSettings`
15. current Cruise workspace, Saved Cruises/Preferences and History
    ViewModels/views/tests
16. current theme resources, reusable control styles and desktop composition
    conventions

Do not begin implementation until alert lifecycle, settings and factual/personal
state independence are understood.

---

## Goal

Give Robin one durable, explainable, fully local place to review and manage
alerts created by explicit Cruise actions.

The Cruise workspace becomes:

```text
Cruises
[ Discovery ] [ Saved Cruises ] [ Alerts · unread count ]
```

The Alerts workspace contains two internal modes:

```text
[ Inbox ] [ Settings ]
```

Inbox provides:

- newest-first durable alert list
- type and lifecycle filters
- selected alert evidence/detail
- explicit Mark read, Mark unread, Dismiss and Restore as unread actions
- refresh, loading, empty, cancellation and failure states

Settings provides:

- enable/disable Price Drop alerts
- enable/disable Promotion alerts
- enable/disable Saved Criteria alerts
- minimum Price Drop percentage from 0 through 100
- explicit Save Settings and Cancel Changes actions
- confirmed baseline, unsaved draft and controlled validation/failure behavior

The implementation must remain offline and must not imply unattended
monitoring.

---

## User Language and Honesty

Use:

- **Alerts**
- **Inbox**
- **Price drops**
- **Promotions**
- **Saved criteria**
- **Unread**, **Read**, **Dismissed**
- **Mark read**, **Mark unread**, **Dismiss**, **Restore as unread**
- **Alert Settings**

Include a short bounded explanation such as:

```text
Alerts are created only from Cruise evidence you explicitly record or save.
Kryten is not monitoring retailer sites in the background.
```

Do not use:

- live monitoring
- watched continuously
- real-time alerts
- notification delivery
- inbox claims implying email, push or external delivery

No alert action may imply a booking recommendation or guarantee that an offer
is still available.

---

## Cruise Workspace Navigation

Replace the current two-mode boolean inversion with the smallest clear
three-mode representation, for example:

```text
Discovery | SavedCruises | Alerts
```

Boolean binding helpers are acceptable, but one authoritative mode must own
the state.

Required behavior:

- Discovery remains the default
- switching modes does not recreate or discard existing child ViewModels
- leaving Discovery cancels/clears only the state already owned by its existing
  deactivation boundaries
- leaving Saved Cruises preserves its established confirmed/draft behavior
- entering Alerts activates the Alert Centre and refreshes local data
- leaving Alerts cancels active alert/settings loads and ignores late results
- returning to Alerts retains filters and preferred selection where possible,
  then refreshes durable data
- no mode switch performs network access, capture or alert evaluation

Add an Alerts selector with an unread badge/count:

```text
Alerts · 3
```

When zero, show `Alerts` without a noisy zero badge or use an equivalent compact
zero state. The exact visual treatment should follow existing theme resources.

Do not add a second application-level navigation destination. Alerts belong
inside the existing Cruise experience.

---

## Alert Centre Inbox

### Local Snapshot

On activation or explicit Refresh:

1. call `ListCruiseAlerts` with the provider-independent all-alert query
2. call `CountUnreadCruiseAlerts`
3. retain one complete newest-first local snapshot
4. project immutable alert item ViewModels
5. apply presentation filters without another database round trip

The existing Application list use case and repository already provide
deterministic ordering:

```text
EventTime descending
CreatedAt descending
EventKey
```

Do not reorder by display text or current wall-clock time.

The list is local and bounded by existing retained alert rows. Do not add
pagination, retention pruning or virtualized server queries in this prompt.

### Type Filter

Provide exactly:

```text
All | Price drops | Promotions | Saved criteria
```

`All` means all alert types in the selected lifecycle scope. It does not mean
all lifecycle states.

### Lifecycle Filter

Provide exactly:

```text
Active | Unread | Read | Dismissed
```

Definitions:

- Active = Unread + Read, excluding Dismissed
- Unread = Unread only
- Read = Read only
- Dismissed = Dismissed only

Default:

```text
Type: All
Lifecycle: Active
```

Dismissed alerts therefore remain retained and discoverable behind an explicit
filter without crowding the normal inbox.

Filters are Avalonia presentation state over the loaded provider-independent
snapshot. Do not change repository contracts or schema merely to combine
Unread and Read.

### Selection

- preserve the selected alert by stable alert `Id` after refresh/filter changes
  when it remains visible
- otherwise select the first visible newest alert
- an empty filtered list has no selection and shows a filter-specific empty
  message
- selecting an alert never silently changes its lifecycle
- selection does not load History, Saved Cruises, TUI or network data
- stale selection cannot mutate a previously selected alert

### Empty States

Distinguish:

- no alerts have ever been created
- no active alerts
- no unread alerts
- no read alerts
- no dismissed alerts
- no alerts matching the selected type/lifecycle filters

The no-alert state should explain that alerts appear only after explicit
supported actions create them. Do not suggest that background monitoring needs
time to run.

---

## Alert List Items

Each row should show only concise, stable information:

- unread/read/dismissed visual state
- alert type label
- ship and departure date
- short type-specific summary
- event/evidence time
- retail source for Price Drop/Promotion when available

Suggested bounded summaries:

### Price Drop

```text
Price dropped from £988 to £949 (3.9474%).
```

### Promotion

```text
New promotion: £100 off per person
```

### Saved Criteria

```text
This shortlisted sailing met your supported saved criteria.
```

Do not display opaque event keys, criteria fingerprints or persistence ids in
normal list rows.

Use existing provider-independent price values. Do not convert currencies,
rewrite bases or round persisted percentages into a different semantic value.
Display formatting may use a concise culture-consistent number of decimal
places while the selected detail retains the exact persisted value.

---

## Selected Alert Detail

Show common detail:

- alert type and lifecycle
- operator id, ship, departure date and duration
- event/evidence time
- alert-created time
- retail source for source-specific alerts

Do not manufacture a cruise title: the alert aggregate intentionally stores
stable sailing identity, not a title snapshot.

### Price Drop Detail

Show:

- previous price, currency and basis
- current price, currency and basis
- exact absolute reduction
- exact percentage reduction
- source
- evidence/event time

### Promotion Detail

Show:

- previous promotion when present
- current promotion
- source
- evidence/event time

When there was no previous promotion, say `No previous promotion recorded`
rather than showing null/blank.

### Saved Criteria Detail

Show only supported explainable values:

- whether a configured departure month matched
- configured budget and matched price when budget was used
- evidence origin: `Recorded observation` or `Price when saved`
- evidence time
- a clear note when cabin preferences existed but were unavailable to evaluate

Do not expose the criteria fingerprint or evidence key as user-facing detail.
Do not claim cabin match/availability.

The selected detail uses only the immutable typed payload already stored in the
alert. It must not reinterpret current History, preferences or saved state,
because those may have changed after the alert event.

---

## Explicit Lifecycle Actions

Reuse `ChangeCruiseAlertStatus`.

Available actions depend on selected status:

### Unread

```text
[ Mark read ] [ Dismiss ]
```

### Read

```text
[ Mark unread ] [ Dismiss ]
```

### Dismissed

```text
[ Restore as unread ]
```

`Restore as unread` is an explicit use of the existing Unread lifecycle state;
it does not recreate, re-evaluate or duplicate the alert.

Rules:

- one mutation command runs at a time
- command target is the stable selected alert Id captured before awaiting
- Updated and Unchanged are successful controlled outcomes
- NotFound removes the stale item from the local snapshot or triggers one safe
  refresh with an honest message
- Cancelled/Failed retains the item and selection unchanged
- successful mutation replaces only that alert in the local snapshot
- reapply active filters after mutation
- preserve selection if still visible; otherwise choose deterministic fallback
- refresh unread count after every successful lifecycle mutation
- no mutation edits History, saved state, preferences, criteria state or alert
  typed details
- no SQL/path/exception detail reaches the UI

Do not add:

- Mark all read
- Dismiss all
- permanent delete
- retention expiry
- bulk selection/actions

Selecting an unread alert does not automatically mark it read. Lifecycle
changes remain deliberate and testable.

---

## Unread Count and Cross-Workspace Refresh

The unread count is durable repository state, not a count derived only from the
currently filtered list.

Refresh it:

- during Cruise workspace activation
- when the Alert Centre activates
- after explicit Refresh
- after successful read/unread/dismiss/restore mutation
- after Record, Save Cruise, Restore or Save Preferences reports one or more
  newly created alerts

Use one focused Avalonia-owned coordination mechanism, such as scoped child
ViewModel events or a small injected workspace alert-state coordinator.

Requirements for that mechanism:

- no static/global event bus
- no service location
- no repository dependency in producer ViewModels
- producers report only controlled created-alert outcomes
- the unread count is confirmed through `CountUnreadCruiseAlerts`; do not assume
  `old count + created count` is correct across deduplication/concurrency
- coalesce or cancel overlapping count refreshes
- ignore stale late count results
- if count refresh fails, retain the last known good count and expose controlled
  retry state without blocking Record/Save/Restore/Preferences success
- when Alerts is active and new alerts are reported, refresh the inbox snapshot
  once after the producing action completes

The existing post-action text from 039d/039e remains. Do not replace it with an
OS toast, popup or sound.

---

## Loading, Refresh, Cancellation and Stale Results

The Alert Centre ViewModel owns separate generations/cancellation for:

- inbox list/unread loading
- selected lifecycle mutation
- settings loading/saving

Required behavior:

- activation starts at most one current load
- explicit Refresh replaces/cancels the previous inbox load
- Cancel Loading is available while inbox/settings loading is active where
  consistent with existing workspace conventions
- a late previous load cannot overwrite a newer snapshot/filter/selection
- switching away cancels active work and ignores late results
- lifecycle mutation results are rejected if the selected target generation
  changed
- settings save results are rejected if settings mode/generation changed
- last good inbox and last confirmed settings remain visible after refresh
  failure
- failed initial load shows Try Again
- cancellation is not presented as failure

No automated refresh timer, polling loop or background monitor belongs here.

---

## Alert Settings

Create a focused settings section inside the Alert Centre, not inside general
Cruise Preferences.

Settings are separate from Robin's month/cabin/budget preference profile.

### Fields

Expose:

```text
[x] Price Drop alerts
[x] Promotion alerts
[x] Saved Criteria alerts
Minimum Price Drop: [ 0 ] %
```

The initial persisted/default settings already enable all types with zero
minimum percentage.

### Draft and Confirmed Baseline

Follow the established explicit-save preference pattern:

- load through `GetCruiseAlertSettings`
- retain a confirmed immutable baseline
- edit a separate draft
- expose `HasUnsavedChanges`
- Save Settings calls `SaveCruiseAlertSettings` only for a valid changed draft
- Updated replaces the confirmed baseline
- Unchanged is successful and replaces/retains the confirmed baseline
- Cancel Changes restores the confirmed baseline
- switching Inbox/Settings preserves a dirty draft within the current Alert
  Centre lifetime
- deactivation/re-entry must not silently persist the draft
- a failed save retains the valid dirty draft for retry
- a late save cannot overwrite a newer settings generation

Do not add Reset Defaults unless an existing reusable pattern requires it; it
is not needed for Prompt 039.

### Percentage Validation

Minimum Price Drop percentage:

- is required while editing the settings object
- accepts decimal values from 0 through 100 inclusive
- zero means every exact comparable reduction is eligible
- rejects blank, non-numeric, negative, NaN/infinity-like text and values above
  100 without calling Application
- uses deterministic culture-aware parsing/display consistent with the desktop
  client
- preserves the exact accepted decimal value represented by the domain
- shows a bounded inline validation message

Disabling Price Drop alerts does not erase or silently reset the threshold.

### Settings Effects

Saving settings affects future agreed evaluations only.

It must not:

- backfill alerts
- delete or dismiss existing alerts
- recalculate existing alert details
- trigger Record, Save Cruise or preference evaluation
- alter criteria transition state immediately
- change History, saved state or general Cruise preferences

The Saved Criteria settings fingerprint already ensures a later explicit
criteria trigger evaluates the new configuration correctly.

---

## MVVM and Presentation Architecture

Add focused ViewModels such as:

```text
CruiseAlertCentreViewModel
CruiseAlertItemViewModel
CruiseAlertSettingsViewModel
```

Exact names may follow existing conventions.

Responsibilities:

### Alert Centre ViewModel

- activation/deactivation
- inbox/settings internal mode
- complete loaded alert snapshot
- type/lifecycle filters
- deterministic selection
- lifecycle commands
- refresh/cancel/retry
- unread count coordination
- controlled status/error messages

### Alert Item ViewModel

- immutable display projection
- common/type-specific list summary
- exact typed detail projection
- no persistence or use-case calls

### Alert Settings ViewModel

- confirmed settings baseline
- editable draft
- validation
- save/cancel/retry lifecycle
- no alert detection or backfill

Views remain passive. Do not place:

- repository calls
- filtering algorithms
- lifecycle mutations
- settings construction/validation
- typed-detail branching

in code-behind.

Prefer a dedicated `CruiseAlertCentreView.axaml` rather than making
`CruiseOfTheWeekView.axaml` carry the complete inbox and settings layout.

Use existing theme resources and established responsive/scrolling conventions.
The alert list and selected detail require independent bounded scrolling so a
long promotion cannot make lifecycle controls unreachable.

---

## Dependency Composition

Register Alert Centre/settings ViewModels through existing desktop extension
methods with appropriate scoped/transient lifetimes.

Pass the shared Alert Centre into `CruiseOfTheWeekViewModel` through constructor
injection. Do not manually instantiate it inside the workspace ViewModel.

Application alert use cases already exist and are registered. Change
Application contracts only for a focused test-proven defect blocking correct
presentation.

Update composition tests so the complete SQLite-backed desktop graph resolves:

- Cruise workspace
- Alert Centre
- Alert Settings
- list/get/count/lifecycle/settings use cases

No provider-specific SDK type may cross into the new presentation.

---

## Required Tests

### Alert Item Projection

Verify:

- Price Drop summary/detail uses exact previous/current/reduction/percentage
- Promotion handles missing previous summary honestly
- Saved Criteria shows month/budget/evidence origin/cabin-unavailable context
- source appears only for source-specific alerts
- sailing identity and timestamps are deterministic
- opaque keys/fingerprints are not exposed in normal presentation
- long bounded promotion text remains present and wrap-ready

### Inbox Loading and Filtering

Verify:

- activation loads all alerts and count once
- repository ordering is preserved
- default All + Active excludes Dismissed
- every type filter has exact membership
- Active/Unread/Read/Dismissed filters have exact membership
- combined type/lifecycle filters compose correctly
- selected Id survives refresh/re-filter when visible
- deterministic fallback selection is used otherwise
- initial/filtered empty messages are distinct and honest
- refresh failure retains last good snapshot
- cancellation and stale loads cannot overwrite current state

### Lifecycle Mutations

Verify:

- Unread → Read
- Read → Unread
- Unread/Read → Dismissed
- Dismissed → Unread through Restore as unread
- selection alone performs no mutation
- Updated/Unchanged/NotFound/Cancelled/Failed remain controlled
- successful mutation updates local snapshot/filter/selection
- failed/cancelled mutation retains prior state
- unread count refreshes from the count use case
- no History, Saved Cruise, preferences or criteria-state mutation is invoked
- stale target result cannot change a replacement selection

### Unread Coordination

Verify:

- workspace activation refreshes count
- created alerts from single/batch Record, Save, Restore and Save Preferences
  request a count refresh
- zero-created and failed primary actions do not claim a new unread alert
- overlapping refreshes are cancelled/coalesced
- failed count retains last good value
- inactive inbox refreshes count without loading list
- active inbox refreshes list once after a producing action
- no polling/timer exists

### Settings

Verify:

- defaults load enabled with zero threshold
- all enabled flags round-trip
- exact 0 and 100 boundaries save
- representative decimal threshold round-trips exactly
- blank/non-numeric/negative/above-100 inputs do not call save
- dirty state is exact and Cancel restores baseline
- Updated/Unchanged replace/retain confirmed baseline
- failed save retains draft
- cancellation/stale results cannot overwrite newer draft/baseline
- disabling Price Drop preserves threshold
- saving settings creates no alerts and triggers no criteria evaluation

### Workspace, Layout and Composition

Verify:

- Discovery remains default
- three modes are mutually exclusive
- mode activation/deactivation calls the correct child only
- unread badge formatting handles zero/non-zero
- Alerts workspace contains independently scrollable list/detail/settings areas
- complete desktop composition resolves all ViewModels/use cases
- all existing Discovery, History, Saved Cruises and Preferences tests remain
  intact

### Persistence Regression

Retain and run existing 039c tests proving:

- alerts/settings survive restart
- deterministic ordering remains correct
- lifecycle mutation changes only alert status
- Dismissed alerts remain retained
- History/Saved Cruise deletion does not cascade to alerts

Do not replace Infrastructure tests with ViewModel fakes.

---

## Allowed Changes

Production changes should remain focused inside:

```text
KrytenAssist.Avalonia/ViewModels/
KrytenAssist.Avalonia/Views/
KrytenAssist.Avalonia/DependencyInjection/
```

Minimal changes are permitted inside:

```text
KrytenAssist.Application/Cruises/
KrytenAssist.Application/DependencyInjection.cs
```

only when a focused failing test proves an existing alert presentation contract
cannot represent the required controlled behavior.

Tests may be created or updated under:

```text
KrytenAssist.Avalonia.Tests/ViewModels/
KrytenAssist.Avalonia.Tests/DependencyInjection/
KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/
```

Update after implementation:

```text
docs/Codex Prompts/039f - Alert Centre and Settings.md
docs/AI Playbook/039 - Price Drop Alerts.md
docs/Roadmap.md
```

Do not modify Core, Infrastructure schema/migrations, API, capture adapters or
browser scripts unless a focused failing test proves a concrete defect. Do not
stage, commit, push, discard or overwrite unrelated work.

---

## Exclusions

- final Prompt 039 cross-boundary audit/manual verification (039g)
- background browsing, monitoring, polling or scheduling
- email, SMS, push, OS notifications or sounds
- permanent alert deletion, retention pruning or expiry
- bulk lifecycle actions
- alert editing or detail recalculation
- settings-triggered alert backfill or criteria evaluation
- opening retailer pages directly from an alert
- adding/removing Saved Cruises from the Alert Centre
- recommendations, booking advice or availability claims
- cross-retailer comparison or currency conversion
- cabin matching or Prompt 040 implementation
- new provider/capture behavior

---

## Verification

Run focused Alert Centre, settings, workspace and composition tests, then:

```text
dotnet build KrytenAssist.sln --no-restore --verbosity minimal -m:1
dotnet test KrytenAssist.sln --no-build --no-restore --verbosity minimal -m:1
git diff --check
```

Use the established single-worker runner where SQLite contention requires it.
Do not hide or suppress warnings introduced by this change.

---

## Results

Implemented on 18 July 2026.

### Status

Complete. The durable local Alert Centre, explicit lifecycle controls, unread
coordination, Alert Settings editor and three-mode Cruise workspace are
implemented. Prompt 039g remains the final audit/manual verification step.

### Implementation

- Added a dedicated Alert Centre ViewModel/view with one newest-first local
  snapshot, exact type/lifecycle filters, stable selection and honest empty,
  loading, cancellation, retry and failure states.
- Added immutable typed alert projections for Price Drop, Promotion and Saved
  Criteria evidence without exposing event keys, criteria fingerprints or
  persistence ids.
- Added explicit Mark read, Mark unread, Dismiss and Restore as unread commands
  over the existing Application lifecycle use case. Selection alone never
  changes lifecycle state.
- Added an Alert Settings ViewModel with confirmed baseline, unsaved draft,
  explicit Save/Cancel, exact decimal 0–100 validation and controlled
  cancellation/stale-result handling.
- Replaced the two-mode Cruise boolean inversion with one authoritative
  Discovery/Saved Cruises/Alerts mode and added a durable unread badge.
- Added a scoped Avalonia alert coordinator. Single/batch Record, Save Cruise,
  Restore and Save Preferences created-alert outcomes request a repository
  count refresh; an active Alert Centre refreshes its snapshot once.
- Registered alert presentation services at the desktop-persistence boundary so
  the generic shell remains resolvable without persistence.
- Added focused projection, filtering, lifecycle, no-auto-read and settings
  validation/failure tests.

### Verification

- `dotnet build KrytenAssist.sln --no-restore --verbosity minimal -m:1` — passed.
- `dotnet test KrytenAssist.Avalonia.Tests/KrytenAssist.Avalonia.Tests.csproj
  --no-restore --verbosity minimal -m:1` — passed, 507 tests.
- `dotnet test KrytenAssist.sln --no-build --no-restore --verbosity minimal
  -m:1` — passed, 655 tests (9 API, 507 Avalonia, 139 Core).
- `git diff --check` — passed.

The build reports the repository's existing `NU1903` advisory for
`SQLitePCLRaw.lib.e_sqlite3` 2.1.11. No new compiler warnings were introduced.

### Lessons Learned

- Alert presentation belongs at the persistence-enabled desktop composition
  boundary; keeping it out of the generic shell preserves offline shell tests
  that intentionally register no repositories.
- A complete in-memory snapshot keeps Active (Unread + Read) filtering in the
  presentation without expanding repository query contracts.
- Created-alert counts are coordination signals, not badge arithmetic. Reloading
  the durable count remains correct across deduplication and concurrent actions.
- Explicit restore of a retained Dismissed alert provides recovery without
  adding permanent deletion or re-evaluation semantics.
