# Codex Prompt 040f – Cabin Availability Presentation

## Implementation Prompt

Implement **Step 040f only** from:

```text
docs/AI Playbook/040 - Cabin Availability.md
```

Prompts 040a–040e are complete. This step adds the local Cabin Availability
latest/history presentation to the Cruise workspace using the existing
provider-independent query and preference contracts. Do not change capture,
recording, evaluation, persistence or alert meaning.

---

## Required Reading

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/037 - Cruise History and Price Tracking.md`
5. `docs/AI Playbook/038 - Saved Cruises and Preferences.md`
6. `docs/AI Playbook/039 - Price Drop Alerts.md`
7. `docs/AI Playbook/040 - Cabin Availability.md`
8. `docs/Codex Prompts/040a - Cabin Availability Experience and Evidence Contract.md`
9. `docs/Codex Prompts/040b - Cabin Domain and Application Contracts.md`
10. `docs/Codex Prompts/040c - SQLite Cabin Persistence.md`
11. `docs/Codex Prompts/040d - TUI Cabin Evidence Capture.md`
12. `docs/Codex Prompts/040e - Recording and Preference Evaluation.md`
13. existing Cruise workspace mode/navigation, Cruise History, Saved Cruises,
    Alert Centre, cabin query results/use cases, preferences and desktop DI tests

---

## Workspace Placement

Add a fourth top-level Cruise workspace mode:

```text
Discovery | Saved Cruises | Alerts | Cabin Availability
```

Extend the existing `CruiseWorkspaceMode` and `CruiseOfTheWeekViewModel`
activation/deactivation behavior. Add a focused
`CruiseCabinAvailabilityViewModel` and view, registered through the existing
desktop extension and injected normally.

Cabin Availability is a factual local history workspace. It is not nested under
Saved Cruises because cabin evidence survives removal/dismissal of personal
state and can exist before a cruise is saved. It must not duplicate the 040e
capture review or recording action.

On entering the mode, load local cabin histories. On leaving it, cancel active
loads and prevent stale completion from publishing. Returning to the mode
refreshes so observations recorded in Discovery become visible without an app
restart.

---

## Master/Detail Experience

Use a desktop-friendly master/detail layout consistent with existing Cruise
workspace surfaces.

### History list

Show one row per persisted cabin **series**, not one row per sailing. Different
retail source or search-context fingerprints remain visibly separate and must
never be collapsed or merged.

Each row shows:

- ship, departure date and duration
- retail source
- concise search-context summary
- latest evidence time and last checked time
- latest known category summary
- Partial or Complete evidence
- observation count
- a concise preferred-cabin match status

Use deterministic ordering:

1. sailing departure date ascending
2. operator id ordinal
3. ship name ordinal
4. retail source id ordinal
5. context fingerprint ordinal
6. series key ordinal

Retain selection by complete series key after refresh. If it no longer exists,
select the first row. Do not retain by sailing alone because another context is
not interchangeable.

### Selected series detail

Show:

- sailing identity and retail source
- full explicit search/occupancy context
- current state for all five cabin categories
- coverage (`Partial evidence` or `Complete evidence`)
- observation time, last checked time and first observed time
- trusted latest source reference when present
- observation count
- current preferred-cabin annotation
- latest versus previous meaningful state differences
- chronological observation timeline, newest first

The timeline must show each retained snapshot's evidence time, coverage,
category states, evidence key/reference where useful and a clear visual selected
state. It is read-only. Do not add delete/edit operations.

---

## Search Context Presentation

Present known facts and unknown facts explicitly. At minimum cover:

- adults
- children
- ordered child ages when known
- package mode
- departure airport
- cabin quantity

Examples:

```text
2 adults · 0 children · child ages known (none)
Fly cruise · departing STN · 1 cabin
```

Unknown values must read `Unknown`, not disappear in a way that suggests a
default. For example:

```text
Adults: Unknown
Package mode: Unknown
Departure airport: Unknown
```

Do not infer two adults, cruise-only, an airport, room count or child ages in
the presentation layer. Child count known with ages unknown must say so. A
known zero-child context has known empty ages, not unknown ages.

Use friendly package labels (`Fly cruise`, `Cruise only`, `Cruise and stay`,
`Unknown`) while preserving the underlying enum meaning.

---

## Availability Language

For every category show exactly one current state:

```text
Available when recorded for this search
Unavailable when recorded for this search
Unknown
```

Do not use `available now`, `live`, `inventory`, `we monitor`, `sold out` or
numeric stock language. Unknown categories remain first-class visible rows.

Partial evidence must include a short explanation:

> This source showed only some cabin facts for this search. Unknown categories
> were not proven unavailable.

Complete evidence may say the source explicitly represented every supported
category at that evidence time. Do not imply the evidence remains current.

An itinerary/package-level `All gone` concept is not a category state and must
not be introduced in this view.

---

## Latest and Previous Change Presentation

Use the persisted chronological series and existing analyzer semantics. Compare
only the latest and immediately previous meaningful observations in the same
series.

Show every differing category honestly:

- `Unavailable → Available`: `Became available when recorded`
- `Available → Unavailable`: `Became unavailable when recorded`
- `Unknown → Available/Unavailable`: `New evidence recorded`
- `Available/Unavailable → Unknown`: `The latest evidence no longer confirms
  the previous state`

Only the first two are explicit inventory transitions. Knowledge changes must
not be phrased as stock movement or alert transitions.

When there is one observation say `No previous recorded cabin evidence for this
search`. When the latest two snapshots have no category difference, say `No
category-state change in the retained latest evidence`; this should be unusual
because equivalent-current evidence is deduplicated, but the presentation must
remain safe for reconstructed data.

Distinguish:

- `Latest meaningful evidence`: time of the latest retained observation
- `Last checked`: series `LastSeenAt`, which may be later after equivalent
  evidence was recorded

Do not invent a timeline snapshot for an equivalent-current recheck.

---

## Preferred-Cabin Annotation

Load current `CruisePreferences` alongside cabin histories and annotate each
series without mutating it.

Preferred cabin types remain an OR set:

- any preferred category explicitly Available:
  `Matches your cabin preferences for this search: Inside, Balcony`
- none Available and at least one preferred category Unknown:
  `Preference match unknown — some preferred cabin states were not shown`
- every preferred category explicitly Unavailable:
  `No preferred cabin type was available when recorded for this search`
- no preferred cabin configured:
  `No preferred cabin types configured`

These annotations describe only the selected source/context evidence. Do not
combine categories from other series or reuse Saved Criteria Met/NotMet state.
Do not claim the cruise as a whole matches all preferences; month and budget are
outside this workspace annotation.

If preferences fail to load, cabin history remains usable. Show a controlled
message that preference matching is temporarily unavailable and omit match
claims. A preference failure must not turn the entire history load into an
error.

---

## Loading, Empty, Error and Stale State

Follow the established local-workspace pattern:

- activate: start one refresh using `ListCruiseCabinHistories`
- refresh: cancel/replace the prior request and increment a generation
- deactivate: cancel and invalidate the current generation
- stale/cancelled completions never replace newer state or selection
- success: atomically replace list projections and set loaded state
- refresh failure after prior success: retain the last successful list and
  selection, show a retryable error
- initial failure: show an error surface and Try Again
- cancellation: neutral message, not an error
- empty success: show an intentional empty state

Empty copy:

> No cabin observations have been recorded yet. Capture a supported TUI result
> in Discovery, review its cabin evidence, then choose Record Cabin Observation.
> Kryten does not monitor cabin availability in the background.

Provide Refresh, Cancel while loading and Try Again after failure. Disable
duplicate refresh while the current load is active.

Preference loading uses the same cancellation token/generation. A late
preference result must not annotate a newer history snapshot.

---

## Projection Ownership

Keep formatting and selection state in Avalonia presentation projections, for
example:

- `CruiseCabinAvailabilityViewModel`
- one series/list item ViewModel
- category-state row ViewModel
- observation/timeline item ViewModel
- latest-change row ViewModel

Exact names may follow project conventions. Projections receive existing
immutable Core/Application values and do not query repositories directly.
ViewModels call Application use cases only.

Do not add provider conditionals, persistence identifiers, EF entities or
database access to Avalonia. Do not change Core fingerprints or Application
history ordering merely for display.

---

## Responsive Layout and Accessibility

Preserve the existing desktop-first visual language and avoid horizontal
scrolling for ordinary content:

- master list and detail may use columns on wide layouts
- narrow layouts stack list above detail
- category states use text, not color alone
- selected/focus states remain keyboard-visible
- commands have unambiguous labels
- long source references trim visually but retain full tooltip/text access
- status/error text wraps

Do not introduce icons whose meaning is unavailable without color or tooltip.

---

## Dependency Injection and Mode Lifecycle

Register the new presentation ViewModel through
`DesktopPersistenceServiceCollectionExtensions`. Inject it into
`CruiseOfTheWeekViewModel`, expose the new workspace mode and wire:

```text
activate mode   -> cabin ViewModel ActivateAsync/Refresh
leave mode      -> Deactivate/cancel
shell deactivate -> Deactivate/cancel
```

Other modes retain their existing lifecycle behavior. The mode must be absent
or disabled only when its required Application query dependencies genuinely
cannot be composed; normal desktop composition must resolve it.

---

## Required Offline Tests

### Projection tests

- all five category rows in canonical order
- exact Available/Unavailable/Unknown wording
- Partial/Complete explanation
- fully known context including known zero-child ages
- child count known but ages unknown
- all unknown context values remain visible as Unknown
- package-mode and airport labels
- preferred OR match, unknown match, explicit no-match and no-preference states
- preference-load failure leaves history usable
- latest/previous explicit transition wording
- Unknown knowledge-change wording is not an inventory transition
- first observation and no-change messages
- latest meaningful evidence differs from later LastSeenAt
- timeline newest-first and source/reference formatting

### Workspace behavior

- deterministic series ordering and no merging across source/context
- exact series-key selection retention after refresh
- first-row fallback when selection disappears
- activate refreshes; return to mode refreshes newly recorded evidence
- refresh cancellation and generation guards ignore stale completion
- deactivate/shell deactivation cancels and prevents publication
- initial loading, loaded, empty, failure, retry and cancelled states
- refresh failure retains prior successful data
- commands correctly enable/disable while loading
- switching among all four Cruise modes activates/deactivates the correct child
- no recording/mutation command appears in Cabin Availability history

### Composition and regressions

- desktop DI resolves the new ViewModel and complete Cruise workspace
- existing Discovery, Saved Cruises and Alerts modes remain unchanged
- 040e cabin capture review/record action remains in Discovery only
- price History remains separate
- no Core/Application/Infrastructure dependency points outward to Avalonia
- no database migration/schema change
- no browser/network/production-database access

Use immutable fixtures, fakes and isolated local test infrastructure only.

---

## Manual Acceptance

1. Record a supported cabin observation in Discovery.
2. Open Cabin Availability and confirm the new series appears without restart.
3. Verify sailing, TUI source, evidence time and every category against the
   recorded capture.
4. Verify two adults, zero children/known empty ages, Fly cruise, STN and one
   cabin display correctly for the demonstrated search.
5. Verify Partial wording states that Unknown is not Unavailable.
6. Record equivalent evidence again and confirm observation count does not
   increase while Last checked advances.
7. With multiple contexts, confirm separate rows and no merged states.
8. Verify current preferred-cabin annotation changes after editing preferences
   and returning to the mode.
9. Exercise Refresh, Cancel and a controlled local failure/retry if practical.
10. Switch rapidly between workspace modes and confirm stale loads do not
    replace current state.
11. Confirm no background monitoring, automatic capture, recording or booking
    action occurs.

---

## Allowed Changes

```text
KrytenAssist.Avalonia/ViewModels/CruiseAlertPresentation.cs
KrytenAssist.Avalonia/ViewModels/CruiseOfTheWeekViewModel.cs
KrytenAssist.Avalonia/ViewModels/CruiseCabinAvailability*.cs
KrytenAssist.Avalonia/Views/CruiseOfTheWeekView.axaml
KrytenAssist.Avalonia/Views/CruiseCabinAvailability*.axaml
KrytenAssist.Avalonia/Views/CruiseCabinAvailability*.axaml.cs
KrytenAssist.Avalonia/DependencyInjection/DesktopPersistenceServiceCollectionExtensions.cs
relevant focused ViewModel, view, lifecycle and DI tests/fakes
docs/Codex Prompts/040f - Cabin Availability Presentation.md
docs/AI Playbook/040 - Cabin Availability.md
docs/Roadmap.md
docs/Session Handovers/*
```

Broaden only for a directly required existing presentation registration or test
contract and document why. No Core, Application, Infrastructure or migration
change should be necessary.

---

## Exclusions

- cabin capture, extraction, recording or alert-evaluation changes
- new cabin/category/context domain concepts
- category-state inference or merging across series
- Saved Criteria logic changes
- editing/deleting cabin history
- automatic refresh timers or background monitoring
- richer retailer adapters or booking interaction
- charting/forecasting/inventory counts
- database schema or migration changes
- cross-device sync or remote notifications
- live browser/network automated tests

---

## Verification

Run focused Cabin Availability projection, lifecycle, workspace and composition
tests, then:

```text
dotnet build KrytenAssist.sln --no-restore -m:1
dotnet test KrytenAssist.sln --no-build --no-restore
git diff --check
```

Use `--disable-build-servers` where required. Resolve warnings introduced by
this step; existing SQLitePCLRaw advisory and pre-existing unused-event warnings
may remain documented.

---

## Results

Implementation and automated verification completed on 19 July 2026.

### Status

Complete.

### Implementation

- Added Cabin Availability as a fourth top-level Cruise workspace mode with
  enter/leave/shell lifecycle integration.
- Added a local master/detail history surface that retains every retailer and
  search-context series independently.
- Presented all five cabin states, complete explicit context, coverage,
  evidence times, source references, observation count and newest-first
  timeline without implying current or monitored availability.
- Added honest latest-versus-previous explicit and knowledge-change wording.
- Added current preferred-cabin OR annotations with controlled degradation when
  preferences cannot be loaded.
- Added atomic refresh, exact-series selection retention, cancellation,
  generation guards, empty/error/retry states and prior-data retention.
- Registered the ViewModel through desktop dependency injection and added
  focused projection, ordering, selection, failure and composition tests.

### Build and Tests

- Single-worker solution build passed with 0 errors.
- Core: 147 passed.
- Avalonia/Application/Infrastructure: 556 passed.
- API: 9 passed.
- Total: 712 passed, 0 failed, 0 skipped.
- Existing SQLitePCLRaw advisory and pre-existing unused-event warnings remain
  unchanged.
