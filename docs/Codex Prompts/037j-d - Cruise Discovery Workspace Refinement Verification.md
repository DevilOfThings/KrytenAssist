# Codex Prompt 037j-d – Cruise Discovery Workspace Refinement Verification

## Implementation Prompt

Implement **Step 4 only** from:

```text
docs/AI Playbook/037j - Cruise Discovery Workspace Refinements.md
```

This is the close-out step for Prompt 037j. Prompts 037j-a, 037j-b and 037j-c
must be implemented and have their focused automated checks passing before
this prompt begins.

Audit and verify the finished Cruise Discovery refinements. Do not introduce
new workspace features while verifying them.

---

## Required Reading

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/036 - Cruise Discovery and Capture.md`
5. `docs/AI Playbook/037 - Cruise History and Price Tracking.md`
6. `docs/AI Playbook/037h - Multiple Cruise Deals Handling.md`
7. `docs/AI Playbook/037i - Cruise Discovery Workspace Layout.md`
8. `docs/AI Playbook/037j - Cruise Discovery Workspace Refinements.md`
9. `docs/Codex Prompts/037j-a - Cruise Discovery Identity and History Actions.md`,
   including Results and Lessons Learned
10. `docs/Codex Prompts/037j-b - Grouped Recorded Cruises.md`, including
    Results and Lessons Learned
11. `docs/Codex Prompts/037j-c - Mobile Browser Presentation.md`, including
    Results and Lessons Learned
12. the current Cruise browser, capture, recording, History and Shell
    ViewModels, views and related tests
13. the most recent Session Handover under `docs/Session Handovers/`

Treat completed code, committed history and existing tests as evidence. Do not
redesign the workspace before identifying a concrete regression against this
prompt's scope.

---

## Goal

Close Prompt 037j with evidence that its refinements improve presentation
without changing trusted browsing, TUI capture, local Cruise History or
provider behaviour.

The verified journey is:

```text
Cruise Discovery
        ↓
Open trusted TUI source or explicitly Go to a trusted address
        ↓
Choose Desktop or Mobile presentation when a browser page is ready
        ↓
Capture and record supported displayed deals
        ↓
Scan, group and select local Recorded Cruise History
        ↓
Open a trusted selected offer externally when required
```

---

## Verification Scope

### Automated Regression

Run focused deterministic tests covering:

- Shell visible identity projection while retaining the `cruise.of-the-week`
  internal identifier and Cruise workspace route
- trusted source selection, Go/Enter address validation, navigation lifecycle,
  close/reopen and compact diagnostic history
- Desktop/Mobile presentation state, trusted reload request ordering,
  no-op/disabled cases and capture-clearing boundary
- single capture, multi-deal review, batch recording and retry/cancellation
- Recorded Cruise History load, grouping, selection, Price History and trusted
  external TUI links

Then run:

```text
dotnet build KrytenAssist.sln --no-restore
dotnet test KrytenAssist.sln --no-build --no-restore
```

If the known environmental parallel-runner instability occurs, record it and
run the stable alternative:

```text
dotnet test KrytenAssist.sln --no-build --no-restore --disable-build-servers -m:1
```

All tests must remain offline. They must not contact TUI, launch a native
browser or access Robin's actual SQLite database.

### Manual Verification with Robin

At the normal desktop window size:

1. Confirm Navigation and Dashboard visibly say **Cruise Discovery**, while
   the application still opens the established Cruise workspace.
2. Open the source and confirm the active browser History grid contains only
   Cruise, Ship, Departure and Current; confirm browser-free History retains
   Trend and Status.
3. Select a History item with a valid latest TUI reference. Confirm **Open at
   TUI** appears beside Price History and opens only a trusted external link.
4. Exercise None, Cruise and Ship grouping. Confirm each group is readable,
   has its sailing count, scrolls within Recorded Cruises and selecting a
   sailing updates Price History. Confirm grouping changes no stored data.
5. With a trusted TUI page ready, confirm Mobile is default and the same
   trusted page is displayed in a phone-oriented panel. Change to Desktop and
   confirm the page reloads, the capture review clears and the page remains
   interactive and resizable.
6. Return to Mobile. Confirm the same page reloads in its mobile panel.
7. Confirm Go/Enter, Back/Forward, Refresh, Verify Page Access, single and
   multi-deal capture, recording, Close Browser and external Open at TUI
   retain their established behaviour.
8. Restart Kryten. Confirm local Recorded Cruise History is still available
   without opening TUI and the presentation mode is not persisted as a user
   preference.

Record Robin's pass/fail observations accurately. A defect may be corrected
only when necessary to meet this existing 037j scope. Repeat affected tests
and the relevant manual check after any correction. Do not treat further UI
ideas, new retailer support, mobile capture selectors or price-model changes as
verification fixes.

---

## Documentation Close-Out

After automated and manual verification pass, update all of the following:

1. This prompt's Results and Lessons Learned.
2. Results and Lessons Learned in 037j-a, 037j-b and 037j-c, accurately
   recording their final manual verification.
3. `docs/AI Playbook/037j - Cruise Discovery Workspace Refinements.md`,
   marking all four steps complete and completing Results and Lessons Learned.
4. `docs/Roadmap.md`, marking Prompt 037j complete and leaving Prompt 038
   unstarted.
5. Create the next sequential Session Handover under `docs/Session Handovers/`
   summarising the completed 037j baseline, automated results, manual evidence
   and any consciously deferred refinements.

Do not claim a manual check passed unless Robin confirmed it. Do not stage,
commit, push, discard or overwrite Robin's work.

---

## Acceptance Criteria

037j-d is complete only when:

- all 037j Playbook acceptance criteria are evidenced
- visible identity is Cruise Discovery without changing its underlying skill
  identifier or assistant-tool route
- History actions and adaptive columns remain correctly placed and trusted
- grouping is deterministic, scrollable, local-only and selection-safe
- Desktop/Mobile is explicit, defaults to Mobile and changes the native
  browser user agent as well as layout width
- a mode change reloads only the existing trusted address through established
  navigation and capture-clearing behaviour
- no provider, TUI capture adapter, source policy, persistence or price model
  change has been introduced
- all automated tests are offline and the complete suite passes
- Robin has manually verified the workspace at normal desktop size
- all Results, Lessons Learned, Roadmap and Session Handover documentation is
  complete and truthful
- Prompt 038 remains unstarted

---

## Results

> Complete after implementation and verification.

### Status

Complete.

### Automated Verification

Focused Cruise Discovery/Shell regression passed:

```text
120 passed, 0 failed, 0 skipped
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
Avalonia: 431 passed
API: 9 passed
Total: 545 passed, 0 failed, 0 skipped
```

The tests are deterministic and offline; they do not contact TUI, launch a
native browser or access Robin's actual database.

### Manual Verification

Passed. Robin confirmed the Cruise Discovery visible identity, compact History
layout, local grouping and mobile-default compact browser workspace.

### Documentation

Roadmap, Playbook, 037j prompt Results/Lessons and Session Handover 021 have
been updated. Prompt 038 remains unstarted.

## Lessons Learned

- Verification should preserve the established browser, capture and History
  boundaries instead of treating presentation refinements as authority to
  redesign them.
- Explicit truncation feedback is important: multi-deal capture currently
  keeps a safe maximum of ten cards, so users know to refine or paginate TUI
  results rather than assuming every visible deal was recorded.
