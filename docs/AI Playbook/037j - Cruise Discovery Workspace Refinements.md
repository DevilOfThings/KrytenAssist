# Prompt 037j – Cruise Discovery Workspace Refinements

## Goal

Refine the completed Cruise Discovery workspace so its visible identity is
clearer, local Recorded Cruise History is easier to scan, and Robin can
deliberately request TUI's mobile presentation when useful.

Prompt 037j builds only on the verified workspace from Prompt 037i. It must
preserve the explicit trusted browsing, capture, recording and local-history
workflow.

---

## Why This Prompt Exists

Prompt 037i made Cruise Discovery practical: it has a resizable embedded TUI
browser, trusted paste-and-Go navigation, capture review and local History.

During manual verification, several small, coherent presentation improvements
were identified:

- the visible route still says **Cruise of the Week**, although its practical
  purpose is now Cruise Discovery
- the selected History **Open at TUI** action belongs beside the Price History
  heading rather than at the end of the detail
- Trend and Status are useful in browser-free History, but consume scarce width
  when the embedded browser is open
- recorded sailings need optional grouping by Cruise or Ship
- TUI's mobile layout may be more convenient for some pages within the
  embedded browser

These are workspace refinements, not a change to source extraction, Cruise
History persistence or the price model.

---

## User Experience

### Clear Cruise Discovery Identity

The visible navigation route and Dashboard representation should say:

```text
Cruise Discovery
```

The existing `cruise.of-the-week` skill identifier and its provider-facing
registration must remain unchanged. This is a presentation projection, not a
provider or assistant-tool rename.

### Selected History Actions

When a selected History has a valid trusted latest TUI source reference,
display:

```text
Price History                                      [ Open at TUI ]
```

The action retains the established trusted external-browser behaviour. It is
hidden for a missing, malformed or untrusted reference.

### Adaptive Recorded Cruise Columns

When the embedded browser is visible, the compact History grid prioritises:

```text
Cruise | Ship | Departure | Current
```

Trend and Status remain available in the selected detail and are retained in
the wider browser-free History screen.

### Grouped Recorded Cruises

Provide a simple local display control:

```text
Group by: [ None | Cruise | Ship ]
```

- **None** preserves the current flat list and order.
- **Cruise** groups by the displayed Cruise title.
- **Ship** groups by ship name.
- Each group presents a readable heading and its number of sailings.
- Selecting an individual sailing still updates the same Price History detail.
- Grouping is presentation-only, ephemeral and is not persisted.
- Grouping must not change History queries, selection identity, price trend or
  recording behaviour.

### Explicit Mobile Browser Presentation

When a trusted TUI browser session is active, provide an explicit mode choice:

```text
[ Desktop ] [ Mobile ]
```

Desktop is the default. Mobile presentation must use the native web-view's
supported user-agent capability and a phone-oriented browser panel width; it
must not merely shrink a desktop page.

Changing mode is deliberate and reloads only the current trusted page through
the existing navigation lifecycle. It must:

- retain trusted-host validation
- clear capture review through the existing new-navigation boundary
- avoid automatic mode changes, hidden navigation or address rewriting
- preserve external Open at TUI and browser Back/Forward semantics

No mobile extraction template, mobile DOM selector or new capture support is
introduced. If a TUI mobile page is unsupported, Kryten must remain honest.

---

## Architecture Principles

### Preserve Existing Boundaries

```text
Avalonia ViewModel
        ↓ existing navigation event
NativeWebView bridge (including explicit user agent)
        ↓
Interactive trusted TUI page

Avalonia History display state
        ↓ existing History ViewModel
Application/Cruise persistence
```

Keep TUI and native-web-view types out of Core, Application, Infrastructure
and History persistence. The ViewModel owns selected display mode, grouping
state, command availability and trusted reload requests. The View owns passive
layout and bindings. The browser code-behind stays a narrow bridge.

---

## Scope

### In Scope

- visible Cruise Discovery route/Dashboard label projection
- placement of selected History Open at TUI action
- browser-active compact History columns
- ephemeral None/Cruise/Ship local grouping
- explicit Desktop/Mobile native-browser presentation mode
- deterministic ViewModel and display-state tests
- manual desktop and mobile TUI presentation verification

### Out of Scope

- changing the `cruise.of-the-week` skill id, HTTP provider, parser,
  registration or assistant-tool contract
- changing trusted hosts, source catalogue or URL policy
- new TUI card templates, DOM selectors, scripts or automatic capture
- changes to capture contracts, batch recording or History query semantics
- SQLite schema, migrations, sailing identity, fingerprints or price model
- saving cruises, ratings, notes, recommendations or Prompt 038
- browser bookmarks, autocomplete, persisted display preferences or cross-host
  browsing
- a redesign of Dashboard, application shell or navigation framework

---

## Implementation Steps

### Step 1 – 037j-a: Cruise Discovery Identity and History Actions

- project `cruise.of-the-week` as **Cruise Discovery** in visible navigation
  and Dashboard surfaces without changing its internal skill identity
- move the selected History Open at TUI action beside Price History heading
- hide Trend and Status columns only in the active browser workspace
- preserve browser-free History columns and all trusted external-open behaviour
- add focused shell/history display tests

### Step 2 – 037j-b: Grouped Recorded Cruises

- add local None/Cruise/Ship grouping state and explicit selection control
- expose display-ready groups while preserving existing flat History items and
  their ordering for the None option
- retain individual item selection and Price History detail behavior
- add deterministic grouping and selection regression tests

### Step 3 – 037j-c: Mobile Browser Presentation

- introduce explicit Desktop/Mobile display mode in the Cruise browser
- use the installed native web-view's user-agent support via the existing
  narrow browser bridge
- reload only the current trusted page through existing lifecycle boundaries
- add deterministic ViewModel tests for command availability, trusted reload,
  capture clearing and no-op/unavailable cases
- manually verify TUI's desktop and mobile presentations

### Step 4 – 037j-d: Refinement Tests and Verification

- audit 037j against Prompts 036, 037, 037h and 037i
- run focused workspace, grouping, navigation, capture, recording and History
  tests plus the complete offline suite
- manually verify all refinements at normal desktop size
- update Results, Lessons Learned, Roadmap, Codex prompts and Session Handover
- leave Prompt 038 unstarted

---

## Acceptance Criteria

Prompt 037j is complete only when:

- visible navigation and Dashboard use Cruise Discovery while the underlying
  skill identity remains unchanged
- selected History Open at TUI is beside its heading and remains trusted-only
- active browser History shows only its essential columns
- browser-free History retains Trend and Status context
- None/Cruise/Ship grouping is deterministic, local and does not alter History
  records or selected-detail behaviour
- Desktop/Mobile is explicit, defaults to Desktop and uses the native user
  agent rather than width alone
- mode changes reload only a trusted current address through established
  lifecycle and capture-clearing behaviour
- no capture adapter/script, provider, persistence or price-model change occurs
- no automated test contacts TUI, launches a browser or uses Robin's database
- complete build and offline tests pass
- Robin manually verifies desktop/mobile browser presentation and History UI
- Results and Lessons Learned are complete
- Prompt 038 remains unstarted

---

## Results

> Complete after implementation and verification.

### Status

Not started.

### Build

Not run.

### Tests

Not run.

### Manual Verification

Not performed.

### Files Created

To be completed.

### Files Updated

To be completed.

### Production Corrections

To be completed.

---

## Lessons Learned

> Complete after implementation and verification. Do not begin Prompt 038
> until this section and Results have been updated.
