# Codex Prompt 037i-c – Compact Cruise Workspace Panels

## Implementation Prompt

Implement **Step 3 only** from:

```text
docs/AI Playbook/037i - Cruise Discovery Workspace Layout.md
```

Prompt 037i-a – Two-Panel Cruise Workspace is complete and committed as
`93a3b30`. Prompt 037i-b – Trusted Address Navigation is complete and
committed as `3ebe0a3`.

The verified automated baseline is:

```text
Core: 105 passed
Avalonia: 426 passed
API: 9 passed
Total: 540 passed, 0 failed, 0 skipped
```

This step makes the established left-hand Cruise workspace compact and easy to
scan while the interactive TUI browser remains available in its separate
right-hand panel. It is a presentation and display-state refinement only.

Do not change trusted navigation, capture extraction, recording semantics,
Cruise History persistence, price modelling, source support or Prompt 038.

---

## Required Reading

Read these files in order before changing code:

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/036 - Cruise Discovery and Capture.md`
5. `docs/AI Playbook/037 - Cruise History and Price Tracking.md`
6. `docs/AI Playbook/037h - Multiple Cruise Deals Handling.md`
7. `docs/AI Playbook/037i - Cruise Discovery Workspace Layout.md`
8. `docs/Codex Prompts/037i-a - Two-Panel Cruise Workspace.md`, including
   Results and Lessons Learned
9. `docs/Codex Prompts/037i-b - Trusted Address Navigation.md`, including
   Results and Lessons Learned
10. `KrytenAssist.Avalonia/ViewModels/CruiseBrowserFeasibilityViewModel.cs`
11. `KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml`
12. the Cruise browser, capture lifecycle, batch review, batch recording and
    Cruise History ViewModel tests

Understand the existing bounded `_navigationHistory`, candidate selection,
batch-recording state and History selection before implementing this prompt.

---

## Goal

Make the active Cruise Discovery workspace comfortable at the normal desktop
window size when Robin has visited several TUI pages and captured up to ten
deal cards.

The desired arrangement remains:

```text
┌──────────── compact left workspace ────────────┬──── interactive TUI ────┐
│ Address and short scrollable navigation history │                         │
│ Capture review / captured deals, bounded        │  Existing browser       │
│ Recorded History, bounded                       │  remains usable         │
└─────────────────────────────────────────────────┴─────────────────────────┘
```

The existing browser panel, trusted-address Go workflow and all capture and
recording commands are already working. Refine only the way compact diagnostic
and source-reference information is displayed around them.

---

## Required Behaviour

### Compact Navigation History

Replace the active-workspace multiline navigation-history diagnostics box with
a bounded vertical list.

- Display exactly one trusted history address per visual row.
- Keep the existing history ordering and maximum-entry behaviour unless a
  focused test proves a small presentation-only change is needed.
- Each row must be readable at a glance, prioritising host and path, for
  example:

  ```text
  www.tui.co.uk/cruise/deals/voyager-cruises
  ```

- Long queries must not make a row wrap or increase its height. Use truncation
  where needed.
- Preserve the complete trusted address through a tooltip or similarly
  non-intrusive detail mechanism.
- The list must have a modest bounded height and standard vertical scrolling,
  so Robin can inspect older entries one row at a time without the history
  dominating the workspace.
- History rows are diagnostic only. Selecting, focusing or scrolling them must
  not request navigation, alter the editable address draft, mutate history or
  affect browser Back/Forward state.

The ViewModel may expose display-ready entry objects or an equivalent
collection. Keep the raw trusted address available for deterministic tests and
for the full-detail tooltip. Do not move URL formatting or trust decisions into
XAML or browser code-behind.

### Compact Long Source References

Long trusted source references must no longer consume several visible lines in
the following existing regions:

- the single captured-Cruise review
- each captured multi-deal candidate card
- the selected Recorded Cruise History detail

Present each source reference as one compact, non-wrapping truncated line with
the exact value available by tooltip or equivalent passive detail. The exact
reference must remain unchanged for existing **Open at TUI**, capture,
recording, identity and History behaviour.

Do not hide any required Cruise title, ship, date, price, readiness, validation
message, recording outcome, retry, selection or History information.

### Bounded Left-Hand Panels

Refine the existing left-hand presentation so that, at the normal desktop
window size:

- the single-capture review remains independently vertically scrollable when
  needed
- genuine batch captured-deal cards remain independently vertically scrollable
- Recorded Cruise History list and selected price-history detail remain usable
  in their existing independent scroll regions
- long diagnostic or reference text cannot expand a panel enough to obscure
  the section below it or interfere with the right-hand browser
- the outer left workspace can still scroll normally when all sections are
  present

Use the current styles and restrained Avalonia layout. Do not introduce a new
application-wide visual system, a collapse/expand feature, a new navigation
workflow or code-behind layout logic.

---

## Preserve Existing Behaviour

All of the following must remain unchanged:

- source selection remains the explicit prerequisite to browser loading
- address draft, Go, Enter, validation and `LoadRequested` lifecycle from
  037i-b
- trusted-host policy and browser code-behind bridge
- capture cancellation, stale-result handling and capture review state
- single-Cruise recording
- batch candidate selection, Record Selected, Record All, retry, cancellation,
  per-row outcomes and one useful History refresh
- local History selection, observation display and browser-closed offline use
- TUI adapter/card selector/script behaviour, capture contracts and all Core,
  Application, Infrastructure and API behaviour

No test may contact TUI, launch a browser, execute browser code-behind or use
Robin's real SQLite database.

---

## Allowed Changes

Production changes should be limited to:

```text
KrytenAssist.Avalonia/ViewModels/CruiseBrowserFeasibilityViewModel.cs
KrytenAssist.Avalonia/ViewModels/<new compact navigation-history display type, if needed>.cs
KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml
```

Tests may be created or updated only under:

```text
KrytenAssist.Avalonia.Tests/ViewModels/
```

Update this prompt after implementation:

```text
docs/Codex Prompts/037i-c - Compact Cruise Workspace Panels.md
```

Do not modify:

- `CruiseTrustedHostPolicy`, source catalogue/source definitions or browser
  `.axaml.cs` bridge
- Core, Application, Infrastructure or API projects
- capture contracts, TUI adapter, fixed script or supported page templates
- persistence, migrations, History domain behaviour, identity/fingerprints or
  price model
- Roadmap, Playbook Results or Session Handovers

Do not stage, commit, push, discard or overwrite Robin's work.

---

## Required Tests

Add focused deterministic ViewModel tests proving:

- trusted observed navigation produces display-ready history entries in the
  same order as the existing raw bounded history
- a compact entry retains the complete trusted address while exposing a
  one-line readable display value that does not require the full query string
- history still respects the existing maximum-entry boundary and clears on
  browser close
- a history display entry has no command or state-changing navigation effect
- existing navigation, trusted Go, single capture, batch candidate selection,
  batch recording and History-selection tests continue to pass

XAML-only compact source-reference changes do not need a browser or internet
test. Where a test needs URI values, use fixed `www.tui.co.uk` addresses and
hand-written ViewModel dependencies.

---

## Acceptance Criteria

037i-c is complete only when:

- navigation history has one compact, non-wrapping row per entry and a bounded
  vertical scroll area
- each history row presents useful host/path context while preserving its exact
  trusted URL passively
- compact history remains diagnostic and cannot navigate or mutate state
- long source references in capture review, batch cards and History detail do
  not dominate panel height, while their exact values remain available and
  unchanged to existing behaviour
- capture review, batch cards, History list and History detail remain bounded
  and independently scrollable at normal desktop size
- the right-hand interactive browser remains unobscured and usable while the
  left workspace scrolls
- all trusted navigation, capture, recording and History semantics remain
  unchanged
- no production code accesses TUI or a browser during automated tests
- relevant focused tests, complete build and complete offline suite pass
- Results and Lessons Learned are complete
- 037i-d and Prompt 038 remain unstarted

---

## Verification

Run focused Cruise browser, capture lifecycle, batch review, batch recording
and History ViewModel tests, then:

```text
dotnet build KrytenAssist.sln --no-restore
dotnet test KrytenAssist.sln --no-build --no-restore
```

If the complete suite is affected by the known environmental parallel-runner
instability, record the symptom and run:

```text
dotnet test KrytenAssist.sln --no-build --no-restore --disable-build-servers -m:1
```

Manual verification with Robin:

1. Open Cruise of the Week and select the TUI source.
2. Visit several trusted TUI pages using normal browsing and paste-and-Go.
3. Confirm navigation history shows compact one-line entries, can scroll, and
   does not navigate when interacted with.
4. Hover a compact history item and confirm the exact trusted address is
   available.
5. Capture a single deal and confirm its source reference is compact while
   Record Observation and Open at TUI still work as before.
6. On a supported multi-deal page, capture several cards; confirm the cards
   scroll within their bounded panel and retain selection/recording behaviour.
7. Select recorded history and confirm the price-history detail is independently
   scrollable below the compact single-line cruise grid, and its source
   reference is compact.
8. Confirm the TUI browser remains visible and interactive on the right
   throughout.

Do not perform final 037i Roadmap, Playbook or Session Handover work here.
Those belong to 037i-d.

---

## Results

> Complete after implementation and verification.

### Status

Implementation and automated verification complete. Awaiting Robin's manual
desktop verification.

### Compact Navigation History

`CruiseNavigationHistoryEntryViewModel` now exposes each existing trusted
history address as a compact host/path display value and its unchanged full
address. The active workspace binds a bounded, one-row-per-entry scroll list;
the full address is available by tooltip. The raw history and its twelve-entry
boundary remain unchanged.

### Compact Source References and Panel Bounds

Single capture review, batch candidate cards and selected History detail now
show source references on one truncated line with the full existing value in a
tooltip. The single review and batch cards retain separate vertical scrolling.
Recorded Cruise History is now a compact, single-line-per-cruise grid with
column headings; its selected Price History appears below the grid in its own
bounded scroll region. It fills the remaining vertical space in the left
workspace after the independently scrollable controls/review area; within that
available space, the cruise grid receives two-thirds of the height and Price
History receives one-third.

### Files Created

- `KrytenAssist.Avalonia/ViewModels/CruiseNavigationHistoryEntryViewModel.cs`

### Files Updated

- `KrytenAssist.Avalonia/ViewModels/CruiseBrowserFeasibilityViewModel.cs`
- `KrytenAssist.Avalonia/MainWindow.axaml`
- `KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml`
- `KrytenAssist.Avalonia.Tests/ViewModels/CruiseBrowserFeasibilityViewModelTests.cs`
- this prompt

### Production Corrections

None. The existing browser bridge, trusted-host policy, capture flow,
batch-recording flow and Cruise History behaviour remain unchanged.

### Build

Passed:

```text
dotnet build KrytenAssist.sln --no-restore
```

Result: 0 errors. Existing SQLite package advisory and unused command-event
warnings remain.

### Tests

Focused Cruise workspace regression: 100 passed, 0 failed, 0 skipped.

Complete offline regression using the stable single-worker runner:

```text
Core: 105 passed
Avalonia: 427 passed
API: 9 passed
Total: 541 passed, 0 failed, 0 skipped
```

### Manual Verification

Pending Robin's desktop verification using the eight checks above.

---

## Lessons Learned

> Complete manual-verification observations before beginning 037i-d.

- A small immutable display model keeps address shortening out of XAML while
  retaining the exact trusted address for diagnostics and passive detail.
- Compact source-reference presentation can be achieved without changing the
  values used by the established capture, external-open or History workflows.
- The existing independently scrolling regions required only bounded-height
  refinement; no new navigation, browser or recording behaviour was needed.
