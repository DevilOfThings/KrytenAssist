# Codex Prompt 037i-b – Trusted Address Navigation

## Implementation Prompt

Implement **Step 2 only** from:

```text
docs/AI Playbook/037i - Cruise Discovery Workspace Layout.md
```

Prompt 037i-a – Two-Panel Cruise Workspace is complete and committed as
`93a3b30`.

The verified solution baseline is:

```text
Core: 105 passed
Avalonia: 415 passed
API: 9 passed
Total: 529 passed, 0 failed, 0 skipped
```

This step lets Robin paste a known trusted TUI address and explicitly navigate
to it. It must reuse the existing ViewModel-to-browser navigation event and
the existing trusted-host policy.

Do not compact navigation history or source-reference display; that belongs to
037i-c. Do not change capture, recording, TUI extraction or Prompt 038.

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
9. `KrytenAssist.Avalonia/Cruises/Discovery/CruiseTrustedHostPolicy.cs`
10. `KrytenAssist.Avalonia/Cruises/Discovery/CruiseDiscoverySource.cs`
11. `KrytenAssist.Avalonia/ViewModels/CruiseBrowserFeasibilityViewModel.cs`
12. `KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml`
13. `KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml.cs`
14. `KrytenAssist.Avalonia.Tests/ViewModels/CruiseBrowserFeasibilityViewModelTests.cs`
15. capture lifecycle, batch review and batch recording ViewModel tests

Do not begin implementation until the current `LoadRequested` bridge,
`CurrentAddress` lifecycle, `ClearCaptureState` boundary and trusted-address
classification are understood.

---

## Goal

Allow Robin to paste a known trusted TUI page into the active Cruise workspace
and deliberately navigate using:

```text
[ Go ]
```

or Enter in the address field.

The workflow must be:

```text
Robin types or pastes address
        ↓ no navigation
Robin chooses Go or presses Enter
        ↓
ViewModel validates through existing trusted-host policy
        ↓ Trusted only
existing LoadRequested event
        ↓
existing NativeWebView bridge navigates
```

No input may bypass the selected source's configured exact HTTPS host.

---

## Address State Rules

Introduce separate Avalonia-owned state with equivalent responsibilities to:

```text
CurrentAddress  = last trusted address actually observed from the browser
AddressDraft    = text Robin is currently editing
```

### AddressDraft

- is editable only while a Cruise source is active
- can temporarily contain any text while Robin types or pastes
- does not navigate, update `CurrentAddress`, add history, clear capture state
  or change browser status merely because it changes
- clears when the browser session closes
- updates to the current trusted browser address after a trusted navigation,
  redirect or read-access diagnostic reports a trusted address

### CurrentAddress

Keep the existing meaning: it is the last trusted address reported by the
browser lifecycle. It must not become user-editable and must never show an
untrusted input value.

Do not persist either value, add autocomplete, bookmarks or address history.

---

## Explicit Go Navigation

Add an Avalonia ViewModel command with an equivalent name to:

```text
GoCommand
```

Expose equivalent command availability such as:

```text
CanGo
```

The command is available only when:

- a source is selected and active
- the draft is nonblank
- browser navigation is not already in progress
- read-access verification is not active
- capture is not active
- batch recording is not active

On invocation:

1. trim the draft
2. require one absolute URI
3. classify it with the existing `CruiseTrustedHostPolicy` and selected source
4. accept only `CruiseAddressTrust.Trusted`
5. leave the existing page and `CurrentAddress` untouched for every rejected
   value
6. for a trusted value, begin the same lifecycle as a deliberate new navigation:
   cancel/clear capture review through the existing boundary, reset relevant
   navigation state and raise the existing `LoadRequested` event once

Do not create another browser-navigation event, instantiate a browser, call a
browser API from the ViewModel, or duplicate URI host checks outside the policy.

### Rejected Values

Reject with controlled user-facing feedback and no navigation request:

- blank after trimming
- malformed or relative value
- non-HTTPS value
- `about:blank` / browser-internal value
- a different host
- a lookalike host such as `www.tui.co.uk.evil.example`
- any value that policy classifies as Untrusted or BrowserInternal

Messages must not echo an unsafe supplied address or expose exception details.
Use calm controlled language, for example:

```text
Enter a valid HTTPS address for the trusted cruise source.
Kryten only allows browsing on www.tui.co.uk.
```

### Trusted Navigation Lifecycle

On a valid trusted Go:

- retain the draft (normalized to the accepted trusted address where useful)
- set controlled loading state and status
- invalidate/clear prior capture review exactly as an ordinary new page
  navigation does
- do not record navigation history or replace `CurrentAddress` until an
  existing trusted browser navigation callback observes the address
- retain current browser event handling for redirects and untrusted observed
  addresses

This prompt must preserve the existing capture stale-result behavior. It does
not create a new browser automation or attempt to infer unidentifiable native
browser callback generations.

---

## XAML Presentation

In the active left workspace only:

- replace the read-only current-address display with an editable address draft
  field bound two-way
- add a visible **Go** button bound to the command
- add an Enter key binding invoking the same command
- keep a suitable placeholder when no address is available
- do not create a second address display
- retain existing styles and avoid code-behind input handling

The current compact-history/source-reference presentation remains unchanged in
this step.

---

## Allowed Changes

Production changes should be limited to:

```text
KrytenAssist.Avalonia/ViewModels/CruiseBrowserFeasibilityViewModel.cs
KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml
```

Tests may be created or updated only under:

```text
KrytenAssist.Avalonia.Tests/ViewModels/
```

Update this prompt after implementation:

```text
docs/Codex Prompts/037i-b - Trusted Address Navigation.md
```

Do not modify:

- `CruiseTrustedHostPolicy`
- source catalogue/source definitions
- browser `.axaml.cs` bridge
- Core, Application, Infrastructure or API code
- capture contracts, TUI adapter or fixed script
- persistence, migrations or History domain behavior
- Roadmap, Playbook Results or Session Handovers

Do not stage, commit, push, discard or overwrite Robin's work.

---

## Required Tests

Add deterministic ViewModel tests proving:

- editing the draft causes no navigation request or capture/history mutation
- Go is unavailable before source activation and while navigation, verification,
  capture or batch recording is active
- a trusted trimmed HTTPS TUI address raises exactly one `LoadRequested` event
- a trusted Go begins controlled navigation and clears current capture review
- malformed, relative, HTTP, `about:blank`, wrong-host and lookalike-host input
  raise no request and retain the previous trusted `CurrentAddress`
- rejected input does not expose the unsafe supplied value in controlled status
  or error messages
- trusted browser navigation/redirect and trusted diagnostic address updates
  synchronise the draft
- browser close clears the draft
- existing single and batch capture lifecycle tests still pass

Tests must use fixed addresses, hand-written services and ViewModel events.
They must not contact TUI, create `NativeWebView`, call browser code-behind or
access Robin's database.

---

## Acceptance Criteria

037i-b is complete only when:

- Robin can type or paste an address after source activation
- typing alone causes no browser request or state mutation beyond the draft
- Go and Enter use the same explicit command
- only the existing selected-source trusted HTTPS host can be navigated to
- malformed, browser-internal and untrusted inputs retain the current browser
  page and trusted observed address
- every rejection is controlled and does not echo unsafe input
- valid navigation uses only the existing ViewModel `LoadRequested` event and
  existing browser bridge
- capture review is cleared through the established new-navigation boundary
- observed trusted redirects keep draft and current address truthful
- close clears draft and existing browser close behavior remains unchanged
- all relevant deterministic tests pass without network/browser/database work
- no trust policy, browser bridge, capture adapter/script or persistence change
  occurs
- Results and Lessons Learned are complete
- 037i-c, 037i-d and Prompt 038 remain unstarted

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
2. Paste a known trusted TUI cruise address and choose Go.
3. Paste another known trusted address and press Enter.
4. Confirm both pages load in the existing right-hand browser panel.
5. Try `https://example.com`, `http://www.tui.co.uk`, and a malformed value;
   confirm the displayed trusted page remains unchanged.
6. Browse to a trusted TUI page that redirects; confirm the address field
   reflects the trusted observed address.
7. Capture a page, then Go to another trusted page; confirm the prior capture
   review clears and no stale result returns.
8. Close the browser and confirm the address draft clears with the existing
   inactive source-selection state.

Do not perform final 037i Roadmap, Playbook or Session Handover work here.
Those belong to 037i-d.

---

## Results

> Complete after implementation and verification.

### Status

Complete.

### Trusted Navigation

`AddressDraft` is separate from the last browser-observed `CurrentAddress`.
Typing changes only the draft. Go validates through the existing selected-source
trusted-host policy and raises the existing `LoadRequested` event only for a
trusted absolute HTTPS address. Rejected input leaves the observed page intact
and produces controlled feedback.

### Files Updated

- `KrytenAssist.Avalonia/ViewModels/CruiseBrowserFeasibilityViewModel.cs`
- `KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml`
- `KrytenAssist.Avalonia.Tests/ViewModels/CruiseBrowserFeasibilityViewModelTests.cs`
- `KrytenAssist.Avalonia.Tests/ViewModels/CruiseCaptureLifecycleViewModelTests.cs`
- this prompt

### Production Corrections

None. The existing trusted-host policy and browser code-behind bridge are reused
unchanged.

### Build

Passed:

```text
dotnet build KrytenAssist.sln --no-restore
```

Result: 0 errors. Existing SQLite advisory and unused command-event warnings
remain.

### Tests

Focused Cruise workspace regression: 99 passed, 0 failed, 0 skipped.

Complete offline regression using the stable single-worker runner:

```text
Core: 105 passed
Avalonia: 426 passed
API: 9 passed
Total: 540 passed, 0 failed, 0 skipped
```

### Manual Verification

Passed. Robin confirmed trusted address navigation works through both Go and
Enter, rejected inputs leave the displayed page unchanged, capture review clears
on trusted navigation, and browser close clears the address field.

---

## Lessons Learned

- Separating the editable draft from the browser-observed address lets Robin
  paste a known page without turning every keystroke into navigation or making
  an unsafe input appear trusted.
- Reusing the existing trusted-host policy and `LoadRequested` event preserved
  the established browser safety boundary without adding a second navigation
  path.
- Explicitly disabling Go during capture and batch recording avoids replacing
  review state while an observation is being processed.
