# Codex Prompt 037j-c – Mobile Browser Presentation

## Implementation Prompt

Implement **Step 3 only** from:

```text
docs/AI Playbook/037j - Cruise Discovery Workspace Refinements.md
```

Prompts 037j-a and 037j-b refine the visible Cruise Discovery workspace and
local History presentation. This step adds a deliberate, temporary choice of
how the already-trusted TUI page is presented in the embedded browser.

Do not add a second browser, a new source, mobile-specific TUI extraction,
DOM selectors, scripts, URL rewriting, persistence or preference storage.

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
9. `docs/Codex Prompts/037j-a - Cruise Discovery Identity and History Actions.md`
10. `docs/Codex Prompts/037j-b - Grouped Recorded Cruises.md`
11. `KrytenAssist.Avalonia/ViewModels/CruiseBrowserFeasibilityViewModel.cs`
12. `KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml`
13. `KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml.cs`
14. `KrytenAssist.Avalonia.Tests/ViewModels/CruiseBrowserFeasibilityViewModelTests.cs`

---

## Goal

While a trusted Cruise Discovery browser session is active, Robin can choose:

```text
Browser presentation:  [ Desktop ] [ Mobile ]
```

Mobile is the initial mode. Selecting Desktop restores the native/default
desktop user-agent; selecting Mobile requests TUI's mobile
presentation through the installed native web-view's supported user-agent
capability and displays the native browser at a phone-oriented width. It must
not merely make the desktop page narrower.

Changing either mode deliberately reloads the same current trusted address.
It must use the existing navigation lifecycle so that navigation state, capture
clearing, address observation, trust validation and Back/Forward behaviour
remain coherent.

The actual page returned is controlled by TUI. Kryten must not claim that a
particular page has a supported mobile capture layout.

---

## Required Behaviour

### Presentation State and Controls

- Add a small, explicit Desktop/Mobile mode choice to the active Cruise
  browser workspace.
- Mobile is selected by default for a new Cruise browser ViewModel/session.
- The selected mode is ephemeral ViewModel display state. Do not persist it.
- The controls are visible only with the active embedded browser workspace and
  disabled whenever a presentation reload cannot safely begin: no selected
  source/current trusted page, navigation in progress, read verification in
  progress, capture in progress or batch recording in progress.
- Selecting the already-active mode is a no-op: it does not reload, clear a
  review or modify navigation history.

### Trusted Reload Lifecycle

On a valid mode change:

1. Validate the observed current address using the existing trusted-host policy.
2. Change the selected presentation state.
3. Begin the existing new-navigation boundary for that same address, including
   cancelling/clearing capture review and pending candidate state as the normal
   navigation path does.
4. Request one existing browser load for the unchanged trusted address.

The ViewModel must not write a URL supplied by the UI, create a hidden
navigation path, or update observed address/navigation history before native
browser callbacks report it. An unsafe, malformed or unavailable address must
leave the current mode and browser state unchanged, with controlled feedback.

### Native Browser Bridge

Keep Avalonia native web-view implementation details in the existing narrow
code-behind bridge.

- The ViewModel may expose a provider-neutral presentation enum/value and a
  presentation-change event/request; it must not reference `NativeWebView` or
  web-view SDK types.
- The bridge maps the requested mode to its user-agent behaviour and applies it
  **before** navigating the existing `NativeWebView` to the trusted address.
- Desktop restores the native/default desktop user-agent behaviour; Mobile uses
  one stable phone browser user-agent value. Keep this mapping local to the
  Avalonia browser bridge, documented and easy to replace if the native package
  changes.
- Continue using the installed `Avalonia.Controls.WebView` API. Do not add a
  browser package, JavaScript workaround, remote service or HTTP retrieval
  path.
- Preserve the existing native NavigationStarted/Completed, stop, close,
  read-access verification, capture payload and external Open at TUI bridges.

### Phone-Oriented Layout

- In Desktop mode, retain the existing resizable desktop browser panel and its
  usable desktop minimum size.
- In Mobile mode, retain the right-hand browser workspace but present the
  native browser in a centred phone-oriented width with an appropriate smaller
  minimum width. It must remain fully visible, vertically stretch to the
  available workspace and continue to resize with the parent window.
- Do not duplicate `NativeWebView`, overlay it over local Cruise controls, or
  change the left workspace/capture/History layout.
- Returning to Desktop restores the current desktop panel constraints.

### Preserve Existing Behaviour

- TUI remains the same sole trusted source and trusted-host policy remains
  unchanged.
- Go/Enter, Back, Forward, Refresh, Stop, Close Browser, Verify Page Access,
  Capture, batch review/recording and external Open at TUI retain their
  existing behaviour.
- Existing TUI capture scripts and adapters are not changed. A page that is
  unsupported in either presentation remains honestly unsupported.
- Browser-free local Recorded Cruise History remains browser-free and does not
  gain presentation controls.

---

## Allowed Changes

```text
KrytenAssist.Avalonia/ViewModels/CruiseBrowserFeasibilityViewModel.cs
KrytenAssist.Avalonia/ViewModels/          (small presentation state type only)
KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml
KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml.cs
KrytenAssist.Avalonia.Tests/ViewModels/CruiseBrowserFeasibilityViewModelTests.cs
docs/Codex Prompts/037j-c - Mobile Browser Presentation.md
```

Do not modify Core, Application, Infrastructure, TUI capture adapters,
persistence, source catalogue or price models. Do not stage, commit, push,
discard or overwrite Robin's work.

---

## Automated Verification

Add deterministic ViewModel tests; do not instantiate the native browser.
At minimum prove that:

- Mobile is the default presentation.
- a ready trusted current address can change between Mobile and Desktop and
  emits exactly one presentation request and one existing load request for the
  same address.
- the bridge-facing request carries the intended presentation mode before the
  associated load request is handled.
- a successful mode change starts the normal navigation/capture-clearing
  boundary and clears existing single and multi-candidate review state.
- choosing the current mode is a no-op.
- a presentation change is unavailable during navigation, verification,
  capture and batch recording, and before any source/current page exists.
- an unsafe or malformed observed address cannot be used to reload or change
  mode.
- existing trusted navigation, close and capture tests remain valid.

Run focused Cruise browser tests, then:

```text
dotnet build KrytenAssist.sln --no-restore
dotnet test KrytenAssist.sln --no-build --no-restore
```

Use the stable single-worker runner if necessary. All automated tests must
remain offline: no TUI connection, native browser launch or Robin database.

---

## Manual Verification with Robin

After automated checks pass:

1. Start Kryten, select Cruise Discovery and open the trusted TUI source.
2. Confirm Mobile is initially selected and TUI displays in the
   phone-oriented right panel. Confirm the page can be scrolled, clicked and
   resized with the Kryten window without covering left-side controls.
3. Navigate to a trusted TUI page, optionally capture a displayed deal, then
   select Desktop. Confirm the review clears as a normal reload and the same
   trusted address reloads in the desktop panel.
4. Switch back to Mobile and confirm the same address reloads in the mobile
   panel.
5. Confirm Go/Enter, Back/Forward, Refresh, Verify Page Access, capture and
   Open at TUI still operate normally in both modes where TUI permits.
6. Confirm browser-free Recorded Cruise History has no presentation controls
   and remains accessible without opening TUI.

Record the actual TUI result faithfully. Do not claim a mobile TUI page is
capture-compatible unless Robin has demonstrated it.

---

## Results

> Complete after implementation and verification.

### Status

Complete.

### Files Created

- `KrytenAssist.Avalonia/ViewModels/CruiseBrowserPresentation.cs`

### Files Updated

- `KrytenAssist.Avalonia/ViewModels/CruiseBrowserFeasibilityViewModel.cs`
- `KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml`
- `KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml.cs`
- `KrytenAssist.Avalonia.Tests/ViewModels/CruiseBrowserFeasibilityViewModelTests.cs`
- this prompt

### Implementation

Cruise Discovery now has explicit Desktop and Mobile presentation choices.
Mobile is the default. A valid mode change applies the requested native
web-view user-agent through the existing Avalonia bridge, then requests the
existing trusted-address load. Mobile uses a bounded phone-oriented browser
panel; the active workspace gives the left controls and History three-fifths
of the available width and reserves the remaining two-fifths for the compact
browser. Desktop restores the native default user-agent and desktop panel
width.

The mode is not persisted. Mode controls are unavailable until a trusted page
is ready and while navigation, read verification, capture or batch recording
is in progress. Changing mode clears capture review through the established
navigation boundary; selecting the current mode does nothing.

### Build

Passed:

```text
dotnet build KrytenAssist.sln --no-restore
```

Result: 0 errors. Existing SQLite package advisory warnings remain.

### Tests

Focused Cruise browser ViewModel regression: 38 passed, 0 failed, 0 skipped.

Complete offline regression using the stable single-worker runner:

```text
Core: 105 passed
Avalonia: 431 passed
API: 9 passed
Total: 545 passed, 0 failed, 0 skipped
```

The tests use deterministic fakes and do not contact TUI, launch a native
browser or access Robin's database.

### Manual Verification

Passed. Robin confirmed the mobile default, compact right-hand mobile browser,
wider Cruise controls/History panel and the completed Cruise Discovery
workspace refinement flow.

## Lessons Learned

- Presentation mode can remain provider-neutral in the ViewModel by emitting a
  simple bridge request before the existing navigation event; native user-agent
  details stay confined to the Avalonia view bridge.
- A presentation change must use the same capture-clearing navigation boundary
  as Go and Refresh, otherwise a review could describe a previous page layout.
- TUI ultimately chooses its returned layout. The user-agent and bounded panel
  request a mobile experience but do not imply mobile capture compatibility.
