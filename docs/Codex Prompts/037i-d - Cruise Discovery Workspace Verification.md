# Codex Prompt 037i-d – Cruise Discovery Workspace Verification

## Implementation Prompt

Implement **Step 4 only** from:

```text
docs/AI Playbook/037i - Cruise Discovery Workspace Layout.md
```

Prompts 037i-a and 037i-b are committed as `93a3b30` and `3ebe0a3`.
Prompt 037i-c is implemented and awaiting final verification, including its
latest responsive Recorded Cruise History layout and larger default desktop
window.

This is the close-out step for Prompt 037i. Audit and verify the completed
workspace against the proven behaviour from Prompts 036, 037 and 037h, collect
Robin's manual desktop evidence, and complete the project documentation.

Do not add mobile browser emulation, grouping of Recorded Cruises, new TUI
templates, new retailers, saved cruises, preferences or Prompt 038. Mobile
view and Cruise/Ship grouping are consciously deferred ideas, not verification
failures.

---

## Required Reading

Read these files in order before making changes:

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
10. `docs/Codex Prompts/037i-c - Compact Cruise Workspace Panels.md`,
    including Results and Lessons Learned
11. `KrytenAssist.Avalonia/MainWindow.axaml`
12. `KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml`
13. `KrytenAssist.Avalonia/ViewModels/CruiseBrowserFeasibilityViewModel.cs`
14. the Cruise browser, capture lifecycle, batch review, batch recording and
    Cruise History ViewModel tests
15. `docs/Session Handovers/2026-07-17 Session 019.md`

Treat committed code and existing tests as evidence. Do not redesign the
workspace before identifying a concrete verification failure.

---

## Goal

Close Prompt 037i with evidence that the redesigned Cruise Discovery workspace
is usable at its normal desktop size and preserves all established Cruise
Discovery, capture, batch recording and local History behaviour.

The completed workspace must support this explicit journey:

```text
Choose TUI source
        ↓
Paste a trusted TUI address, then explicitly Go or press Enter
        ↓
Browse in the persistent right-hand browser
        ↓
Capture one or several displayed deals
        ↓
Review and record local observations
        ↓
Use Recorded Cruise History without losing browser context
```

---

## Verification Scope

### Automated Regression

Run focused deterministic tests covering:

- Cruise browser source activation, lifecycle, trusted address handling and
  compact navigation history
- single capture lifecycle and review
- multi-deal capture review, selection, batch recording, cancellation, retry
  and outcomes
- Cruise History recording, refresh, selection and price-history display

Then run:

```text
dotnet build KrytenAssist.sln --no-restore
dotnet test KrytenAssist.sln --no-build --no-restore
```

If the known environmental parallel-runner instability occurs, record it and
use the stable runner:

```text
dotnet test KrytenAssist.sln --no-build --no-restore --disable-build-servers -m:1
```

All tests must remain offline. They must not contact TUI, open a native browser
or use Robin's real database.

### Manual Desktop Verification with Robin

Start Kryten from a clean launch and verify at the larger default window size:

1. Confirm the default window has enough room for Navigation, the left Cruise
   workspace and the right embedded TUI browser without initial clipping.
2. Select Cruise of the Week and explicitly choose the TUI source. Confirm no
   browser loads before source selection.
3. Resize the centre splitter. Confirm both panels retain usable minimum sizes
   and the browser remains visible and interactive.
4. Paste a trusted TUI address, choose Go, then repeat using Enter. Confirm
   typing alone does not navigate.
5. Try malformed, HTTP and non-TUI addresses. Confirm the current trusted page
   remains displayed and controlled feedback appears.
6. Visit several trusted pages. Confirm compact navigation-history rows scroll,
   show useful host/path text and expose the exact URL by hover without acting
   as navigation controls.
7. Capture a single supported deal. Confirm compact source-reference display,
   Record Observation and Open at TUI still work.
8. Capture a supported multi-deal page. Confirm candidate selection, record
   actions, outcomes and bounded scrolling remain usable.
9. Confirm Recorded Cruise History fills the available left-workspace height;
   its compact one-line cruise grid takes roughly two-thirds and the selected
   Price History below takes roughly one-third, with independent scrolling.
10. Confirm the embedded browser stays on the right and interactive while
    capture review, batch deals and History are visible or scrolled.
11. Restart Kryten and confirm local Recorded Cruise History remains available
    without opening TUI.

Record Robin's pass/fail observations accurately. A real regression may be
fixed only when it is necessary to satisfy this existing 037i scope; then
repeat affected tests and verification. Do not use this as authority for a
general redesign.

---

## Documentation Close-Out

When automated and manual verification pass, update all of the following:

1. This prompt’s Results and Lessons Learned.
2. `docs/Codex Prompts/037i-c - Compact Cruise Workspace Panels.md` Results
   and Lessons Learned to record final manual verification and the latest
   responsive History/default-window refinement.
3. `docs/AI Playbook/037i - Cruise Discovery Workspace Layout.md` Results and
   Lessons Learned, marking all four steps complete.
4. `docs/Roadmap.md`, marking Prompt 037i complete and leaving Prompt 038
   unstarted.
5. Create the next sequential Session Handover under `docs/Session Handovers/`
   summarising the completed 037i baseline, passing tests, manual checks and
   the deferred mobile-view and Cruise/Ship grouping ideas.

Do not state that a manual check passed unless Robin has confirmed it. Do not
stage, commit, push, discard or overwrite Robin's work.

---

## Acceptance Criteria

037i-d is complete only when:

- the complete 037i acceptance criteria in the Playbook are evidenced
- build and complete offline suite pass
- no test uses TUI, a real browser or Robin's database
- Robin has manually confirmed the active workspace at default desktop size
- the right-hand browser is usable, resizable and not covered by local panels
- trusted Go/Enter navigation and rejection behaviour remain correct
- compact diagnostic history remains passive and bounded
- single and batch capture/recording workflows remain unchanged
- responsive Recorded Cruise History and its two-thirds/one-third internal
  layout are confirmed
- all Results and Lessons Learned are complete and truthful
- Roadmap and Session Handover are updated
- Prompt 038 remains unstarted

---

## Results

> Complete after implementation and verification.

### Status

Complete.

### Automated Verification

Focused Cruise workspace regression passed:

```text
100 passed, 0 failed, 0 skipped
```

Build passed:

```text
dotnet build KrytenAssist.sln --no-restore
```

Result: 0 errors. Existing SQLite package advisory and unused command-event
warnings remain.

Complete offline regression passed using the stable single-worker runner:

```text
Core: 105 passed
Avalonia: 427 passed
API: 9 passed
Total: 541 passed, 0 failed, 0 skipped
```

The tests use deterministic fixtures and fakes; they do not contact TUI, open
a native browser or use Robin's real database.

After the local-History correction, focused Cruise browser and History
regression passed again: 52 passed, 0 failed, 0 skipped.

### Manual Verification

Passed. Robin confirmed the larger default window, two-panel browser layout,
trusted navigation, compact history, capture/recording workflow, browser-free
local History, trusted recorded-offer links and compact grid indicators.

### Documentation

Roadmap, Playbook, 037i-c Results/Lessons and Session Handover updated.

### Production Corrections

Recorded Cruise History is now available in the inactive Cruise of the Week
screen, beneath the source chooser. Robin can inspect local History after a
restart without selecting a source or opening TUI. The active browser workspace
retains its existing History presentation.

The obsolete HTTP-client retrieval controls and result card are no longer
visible in the Cruise screen, so Cruise Discovery occupies the available area.
The underlying provider and skill remain registered for now and are not part of
this UI correction.

Selected Recorded Cruise History detail now exposes **Open at TUI** when its
latest stored source reference is a trusted TUI HTTPS address. The action uses
the existing external browser bridge; missing, malformed and untrusted values
remain unavailable.

The compact History grid now uses hoverable emoji indicators for price trend
and sailing status while retaining the full controlled text in the ViewModel
and Price History detail.

---

## Lessons Learned

- Manual verification exposed that local History must be reachable before a
  live source is selected; offline-first presentation needs explicit testing,
  not just service-level coverage.
- Reusing the existing trusted external-open bridge lets recorded History link
  to its evidence without adding a second browser or URL-trust path.
- Remaining presentation ideas are better handled as a focused follow-up than
  extending a completed verification prompt.
