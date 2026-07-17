# Codex Prompt 037i-a – Two-Panel Cruise Workspace

## Implementation Prompt

Implement **Step 1 only** from:

```text
docs/AI Playbook/037i - Cruise Discovery Workspace Layout.md
```

Prompt 037h – Multiple Cruise Deals Handling is complete through `1c419fc`.
The verified solution baseline is:

```text
Core: 105 passed
Avalonia: 415 passed
API: 9 passed
Total: 529 passed, 0 failed, 0 skipped
```

This step solves the immediate workspace problem: the embedded TUI browser must
have a useful, persistent right-hand panel while Cruise controls, review and
local History remain usable on the left.

Do not add editable address navigation, compact history entries, new TUI card
templates or Prompt 038 behavior. Those belong to later 037i steps.

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
8. `docs/Codex Prompts/037h-c - Multi-Cruise Capture Review.md`, including
   Results and Lessons Learned
9. `docs/Codex Prompts/037h-d - Batch Observation Recording.md`, including
   Results and Lessons Learned
10. `docs/Codex Prompts/037h-e - Multiple Cruise Deals Tests and Verification.md`,
    including Results and Lessons Learned
11. `KrytenAssist.Avalonia/Views/CruiseOfTheWeekView.axaml`
12. `KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml`
13. `KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml.cs`
14. `KrytenAssist.Avalonia/ViewModels/CruiseBrowserFeasibilityViewModel.cs`
15. existing Cruise browser, capture-review, batch-review, batch-recording and
    History tests

Do not begin implementation until the current browser bridge, capture lifecycle
and left-side review/history presentation are understood.

---

## Goal

Replace the active Cruise Discovery workspace's vertical stacking layout with a
durable resizable two-panel layout.

When a source is active, Robin must be able to browse TUI in a useful right-hand
panel while seeing and scrolling Cruise controls, review panels and Recorded
Cruise History on the left.

```text
┌──────────────── Left Cruise workspace ───────────────┬──── Right browser ────┐
│ Navigation controls, status and diagnostics            │                       │
│ Capture review or captured cruise deals                │  NativeWebView stays  │
│ Recorded Cruise History                                │  visible, useful and  │
│ Left-side content may scroll                           │  interactive          │
└───────────────────────────────────────────────────────┴───────────────────────┘
```

The existing inactive source chooser remains simple and full-width. No source
must load until Robin explicitly chooses it.

---

## Scope

This step owns:

- active-state two-panel Grid layout in Cruise Discovery
- a visible, resizable splitter between workspace and browser
- sensible minimum useful dimensions for both panels
- a left-side scrolling workspace that does not move or cover the browser
- retaining the existing bounded review, captured-deal and History regions
- preserving all existing bindings, commands and browser event wiring
- focused regression execution and this prompt's Results/Lessons Learned

This step does **not** own:

- an editable address, Go button or Enter navigation
- any ViewModel navigation-state change
- compact navigation-history row presentation
- changing source-reference text presentation
- changes to capture, selection, recording, retry or History behavior
- changing browser code-behind behavior or `NativeWebView` bridge events
- TUI script, adapter, selectors or `small-product-card` support
- Core/Application/Infrastructure/persistence/schema changes
- redesigning the Dashboard, application shell or inactive source chooser
- Prompt 038 behavior

---

## Allowed Changes

Production changes should be limited to:

```text
KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml
```

Update this prompt after implementation:

```text
docs/Codex Prompts/037i-a - Two-Panel Cruise Workspace.md
```

Tests should normally remain unchanged: this is passive XAML layout and the
existing ViewModel suites already protect the behavior it must preserve.

Add or update tests only if a deterministic, behavior-level regression is
introduced or uncovered. Do not add brittle pixel, window-size or live-browser
tests merely to test XAML structure.

Do not modify:

- any `.axaml.cs` browser bridge
- Cruise ViewModels
- Core, Application, Infrastructure or API code
- capture contracts, TUI script/adapter or trusted-host policy
- persistence or migrations
- Roadmap, Playbook Results or Session Handovers

Do not stage, commit, push, discard or overwrite Robin's work.

---

## Layout Requirements

### Inactive State

Before a source is selected, retain the current full-width heading, explanation
and trusted source buttons. The browser remains absent and no navigation occurs.

### Active State

After a source is selected:

- retain the Cruise Discovery heading and the existing command controls
- place active workspace content in a left panel
- place the existing `NativeWebView` in a separate right panel
- put a visible `GridSplitter` between the panels so Robin can resize them
- use a browser-favouring default width, while retaining a useful left working
  width
- give both sides explicit sensible minimum widths; retain the existing browser
  minimum height or improve it only when needed for a useful desktop panel
- ensure the browser fills the available right panel vertically
- ensure scrolling the left workspace cannot scroll, hide, overlap or reduce
  the browser
- retain all existing `NativeWebView` name, visibility binding and navigation
  event handlers exactly

Do not introduce a window-level horizontal scrollbar. At constrained widths,
the panel minimums and splitter should preserve usable work areas rather than
allowing either panel to collapse.

### Left Workspace Content

Move the current active-only content into the left workspace in the same
functional order:

1. navigation controls
2. status, source and diagnostics
3. errors/capture state
4. single captured-Cruise review or batch captured-deals review
5. Recorded Cruise History

The left panel may have one outer vertical `ScrollViewer`.

Keep existing inner bounds and scroll behavior for:

- single captured-Cruise review
- captured deal cards
- History list and History detail

Do not attempt the later compact navigation-history or source-reference design
in this step. Preserve the current content and bindings as-is.

### Browser Bridge Preservation

`CruiseBrowserFeasibilityView.axaml.cs` must remain unchanged. Its existing
event subscriptions and `EmbeddedBrowser` operations own the narrow platform
bridge.

The layout must not:

- remove or rename `EmbeddedBrowser`
- add browser navigation in XAML/code-behind
- alter `LoadRequested`, Back, Forward, Stop, Refresh, Close, capture,
  verification or external-open events
- detach/recreate the browser because review/history content changes

---

## Acceptance Criteria

037i-a is complete only when:

- inactive source selection remains full-width and performs no automatic load
- active Cruise Discovery has a left workspace and separate right browser panel
- a visible splitter allows the two panels to be resized
- both panels retain sensible useful minimum dimensions
- the browser has useful vertical space at the normal desktop window size
- capture review, ten captured deals and Recorded Cruise History cannot push
  the browser beneath the fold or overlay it
- left-side scrolling leaves the browser visible and interactive
- existing browser command buttons and browser bridge event wiring remain
  unchanged
- single capture, batch review, batch recording and History behavior remain
  unchanged
- no Core/Application/Infrastructure/adapter/script/persistence changes occur
- XAML compiles, relevant tests pass and Robin manually verifies the layout
- Results and Lessons Learned are complete
- 037i-b, 037i-c, 037i-d and Prompt 038 remain unstarted

---

## Verification

Run the existing focused desktop tests covering:

- `CruiseBrowserFeasibilityViewModelTests`
- `CruiseCaptureLifecycleViewModelTests`
- `CruiseCaptureReviewViewModelTests`
- `CruiseBatchCaptureReviewViewModelTests`
- `CruiseBatchObservationRecordingViewModelTests`
- `CruiseHistoryViewModelTests`

Then run:

```text
dotnet build KrytenAssist.sln --no-restore
dotnet test KrytenAssist.sln --no-build --no-restore
```

If the complete test runner is affected by the known environmental parallel
runner instability, record the exact symptom and run the stable equivalent:

```text
dotnet test KrytenAssist.sln --no-build --no-restore --disable-build-servers -m:1
```

Manual verification with Robin:

1. Start Kryten and open Cruise of the Week.
2. Select the TUI source and confirm the browser appears in the right panel.
3. Resize the splitter in both directions; confirm the browser and left
   workspace remain useful.
4. Browse TUI, capture one Cruise and then capture loaded deals where supported.
5. Scroll the left workspace through captured deals and Recorded Cruise History.
6. Confirm the TUI page stays visible, interactive and does not move beneath
   review/history content.
7. Close the browser and confirm the existing inactive source-selection state
   remains controlled.

Do not perform final 037i documentation, Roadmap updates or Session Handover
work here. Those belong to 037i-d.

---

## Results

> Complete after implementation and verification.

### Status

Complete.

### Layout

The active Cruise Discovery workspace now uses a browser-favouring resizable
two-panel Grid. Existing controls, diagnostics, capture review, batch review
and Recorded Cruise History remain in a scrollable left workspace. The existing
`EmbeddedBrowser` remains unchanged in a separate right panel with useful
minimum dimensions.

### Files Updated

- `KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml`
- this prompt

### Production Corrections

None. This step changed passive XAML layout only; the browser bridge and all
Cruise ViewModels remain unchanged.

### Build

Passed:

```text
dotnet build KrytenAssist.sln --no-restore
```

Result: 0 errors. The existing NU1903 SQLite advisory and two existing unused
command-event warnings remain.

### Tests

Focused Cruise workspace regression:

```text
88 passed, 0 failed, 0 skipped
```

Complete offline regression using the stable single-worker runner:

```text
Core: 105 passed
Avalonia: 415 passed
API: 9 passed
Total: 529 passed, 0 failed, 0 skipped
```

### Manual Verification

Passed. Robin confirmed the two-panel Cruise workspace, splitter resizing and
browser usability work as intended.

---

## Lessons Learned

- The existing Cruise ViewModel tests remained sufficient because the change is
  passive layout only; the full build also compiled the revised Avalonia XAML.
- Separating the browser from vertically stacked review and History content
  gives browsing a durable working area without changing capture or recording
  behavior.
- Keeping the existing `NativeWebView` instance and code-behind bridge in place
  avoided lifecycle risk while materially improving the workspace.
