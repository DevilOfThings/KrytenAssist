# Prompt 037i – Cruise Discovery Workspace Layout

## Goal

Make Cruise Discovery comfortable to use while Robin browses TUI pages,
reviews multiple captured deals and revisits local Cruise History.

Prompt 037i improves the desktop workspace only. It preserves the proven
capture and recording workflow from Prompts 036, 037 and 037h.

The priority is that the embedded TUI page has a useful, persistent area on the
right while the Cruise controls and local information remain usable on the
left.

---

## Why This Prompt Exists

Prompt 037h made it possible to capture and record several independently
validated TUI deal cards. In practice, the current single vertical layout puts:

- navigation controls and diagnostics
- captured Cruise review cards
- the embedded browser
- Recorded Cruise History

in one stacking column.

Ten captured cards, their full source references and local history can push the
interactive TUI page beneath the fold or squeeze it into an unusable space.
This makes it hard to paste or find a known page, compare a displayed deal, or
move between supported and unsupported pages safely.

The observed workspace needs to support this practical journey:

```text
Paste known trusted TUI address
        ↓ explicit Go
Browse in a useful right-hand panel
        ↓
Capture loaded deals
        ↓
Review / record in bounded left-hand panels
        ↓
Revisit local history without losing browser context
```

This is not a request to make browsing unattended or to widen TUI extraction.
It makes Robin's existing explicit workflow usable.

---

## User Experience

### Before a Source Is Selected

Keep the existing simple Cruise Discovery introduction and trusted source
buttons. No browser must load until Robin explicitly chooses a source.

### Active Cruise Workspace

After Robin selects a source, show a durable two-panel workspace:

```text
┌──────────────────── Cruise workspace ────────────────────┬──── TUI browser ────┐
│ Navigation, trusted address and status                    │                     │
│                                                           │  Interactive TUI    │
│ Captured Cruise review / Captured cruise deals            │  page at useful     │
│ (independently scrollable)                                │  width and height   │
│                                                           │                     │
│ Recorded Cruise History (independently scrollable)        │                     │
└──────────────────────────────────────────────────────────┴─────────────────────┘
```

The exact visual treatment may follow existing Avalonia styles, but these rules
are required:

- the browser is a separate right-hand panel, not a later vertical row
- the split can be resized by Robin within sensible minimum sizes
- the browser retains a useful minimum width and height at the normal desktop
  window size
- the left workspace may scroll without moving, hiding or shrinking the
  browser
- review and history content must not overlay the browser
- the browser remains interactive while a capture review or local history is
  visible

Do not make the full window horizontally scroll merely to preserve a layout.
At a constrained size, retain minimum browser usability and allow the left
workspace to use its own scrolling region.

### Trusted Address Navigation

Show an editable address draft with a clearly labelled:

```text
[ Go ]
```

action. Pressing Enter in the address field should have the same effect.

Typing or pasting an address must not navigate automatically.

On explicit Go:

- trim and parse one absolute HTTPS address
- require the current selected source's existing trusted-host policy to classify
  it as Trusted
- keep the existing browser page untouched when the address is malformed,
  untrusted or navigation is currently unavailable
- show a controlled message without exposing implementation detail
- on success, use the established browser navigation event and lifecycle
- clear capture-review state through the existing new-navigation boundary
- retain existing cancellation and stale-result protection

The displayed address must remain truthful. Browser navigation, redirects and
diagnostics update the draft only when the existing policy accepts the observed
address. Do not let the editable draft become a way to display or launch an
untrusted address.

No address autocomplete, address persistence, cross-host browsing, query
rewriting, hidden navigation or source-catalog change is required.

### Compact Navigation History

Replace the large multiline diagnostics box with a bounded scrollable list:

- one address per visual row
- retain the existing bounded history behavior unless a test demonstrates a
  small presentation-only adjustment is needed
- show a short readable address/path on each row; expose the complete trusted
  address through a tooltip or equivalent non-intrusive detail
- do not make a history row navigate merely when it is selected
- history is diagnostic context, not a second browser-history implementation

Existing Back and Forward browser commands remain the source of truth for
actual browser navigation.

### Bounded Capture and History Panels

The left workspace must contain separately bounded scroll regions for:

- a single captured-Cruise review when applicable
- Captured cruise deals when a genuine batch exists
- Recorded Cruise History list and selected price-history detail

Keep their existing text, selection, record, retry, cancellation and external
open behavior. Improve placement and scrolling only.

Long source references should not dominate the visible card layout. Preserve
the exact trusted address for Open at TUI and capture/record behavior, while
presenting it compactly with a tooltip or appropriate detail affordance.

---

## Architecture Principles

### MVVM and Browser Isolation

The ViewModel owns:

- address draft state
- explicit Go command availability and controlled validation result
- trusted-address validation through the existing `CruiseTrustedHostPolicy`
- navigation lifecycle state
- display-ready bounded navigation history entries

The View owns:

- passive bindings, layout, split sizing and scroll containers
- forwarding existing browser navigation events to the embedded browser bridge
- Enter-to-command binding if needed by Avalonia's existing command patterns

The browser code-behind remains a narrow platform bridge. It may navigate only
in response to the existing ViewModel navigation-request event. It must not
parse, normalize, trust or persist addresses itself.

### Preserve Existing Boundaries

```text
Avalonia workspace ViewModel
        ↓ existing navigation request event
Avalonia NativeWebView bridge
        ↓
Interactive TUI page

Avalonia review / history ViewModels
        ↓ existing Application use cases
Application and Core Cruise contracts
```

No browser, DOM, HTML, TUI payload, JavaScript, SQLite or persistence types may
be introduced into Core or Application because of this prompt.

---

## Scope

### In Scope

- active Cruise Discovery two-panel resizable desktop layout
- useful independent browser panel sizing
- editable trusted address draft with explicit Go and Enter behavior
- controlled trusted-address validation and navigation requests
- compact, one-line scrollable navigation-history presentation
- bounded independently scrollable capture-review, batch-review and History
  regions
- compact presentation of long trusted source references
- focused ViewModel, XAML/layout and lifecycle tests
- manual desktop verification at normal window size

### Out of Scope

- changing the `CruisePageCaptureRequest`, single or batch capture contracts
- changing TUI card selectors, script payloads or adapter mapping
- supporting `small-product-card` or any other new TUI page template
- automatic capture, recording, browsing, history navigation or page loading
- changing trusted hosts or adding retailers
- browser address persistence, bookmarks, autocomplete or search
- changing Cruise History schema, migrations, identity, fingerprints or price
  model
- ratings, notes, saved cruises, preferences, recommendations or Prompt 038
- changing Record Observation or batch-recording semantics
- redesigning the overall application shell or Dashboard

---

## Implementation Steps

### Step 1 – 037i-a: Two-Panel Cruise Workspace

- refactor only the active Cruise Discovery presentation into left workspace
  and right browser panels
- use an Avalonia splitter/resizable layout with sensible minimum dimensions
- keep the browser out of the left content flow and preserve the inactive source
  chooser state
- make capture review, batch review and recorded History individually bounded
  and scrollable
- retain existing commands, bindings and browser bridge behavior
- add focused passive-layout and ViewModel regression tests as appropriate

### Step 2 – 037i-b: Trusted Address Navigation

- add Avalonia-owned editable address-draft state and explicit Go command
- support Enter as the same explicit action
- validate only through the existing selected-source trusted-host policy
- reuse existing navigation request, cancellation and stale-result boundaries
- keep browser/current address unchanged on malformed or untrusted input
- add deterministic ViewModel tests for accepted, malformed, untrusted,
  unavailable and redirect/replacement cases

### Step 3 – 037i-c: Compact Cruise Workspace Panels

- replace multiline navigation history with one-line bounded entry presentation
- compact long address/source-reference display without losing exact trusted
  behavior
- refine capture-deal and History panel bounds at the normal desktop size
- preserve candidate selection, recording states, History selection and browser
  interaction while panels scroll independently
- add focused presentation/state tests

### Step 4 – 037i-d: Workspace Tests and Verification

- audit 037i behavior against Prompts 036, 037 and 037h
- run focused Cruise workspace, lifecycle, capture, recording and History tests
- build and run the complete offline suite
- manually verify source selection, paste-and-Go navigation, browser usability,
  capture, record and History visibility at the normal desktop window size
- update this Playbook Results and Lessons Learned, the Roadmap, Codex prompts
  and a Session Handover where appropriate
- leave Prompt 038 unstarted

---

## Acceptance Criteria

Prompt 037i is complete only when:

- selecting a source still remains an explicit prerequisite to browser loading
- active Cruise Discovery has a separate useful right-hand browser panel
- review/history content cannot cover or push the browser beneath the fold
- the split is resizable and preserves sensible minimum working areas
- Robin can paste a trusted absolute HTTPS TUI address and explicitly navigate
  with Go or Enter
- typing alone causes no navigation
- malformed or untrusted addresses do not navigate, do not change the current
  browser page and show controlled feedback
- trusted navigation preserves existing capture cancellation and stale-result
  behavior
- navigation history is compact, bounded, scrollable and one address per row
- captured Cruise deals and Recorded Cruise History remain independently
  scrollable and usable with a batch of ten deals
- long trusted source references do not dominate the workspace layout
- single-Cruise capture and Record Observation remain unchanged
- batch capture, selection, recording outcomes, retry and one History refresh
  remain unchanged
- no capture contract, TUI adapter/script, persistence schema or price model is
  changed
- no test contacts TUI, launches a real browser or accesses Robin's database
- complete build and offline tests pass
- Robin manually verifies the redesigned workspace
- Results and Lessons Learned are complete
- Prompt 038 remains unstarted

---

## Results

> Complete after implementation and verification.

### Status

Not started.

### Workspace Layout

To be completed.

### Trusted Address Navigation

To be completed.

### Compact Panels

To be completed.

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

> Complete after implementation and verification. Do not begin Prompt 038 until
> this section and Results have been updated.
