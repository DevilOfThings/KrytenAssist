# Codex Prompt 036b – Cruise Source Navigation

## Implementation Prompt

Implement Step 2 of:

`docs/AI Playbook/036 - Cruise Discovery and Capture.md`

This prompt follows the successful feasibility gate in:

`docs/Codex Prompts/036a - Embedded Cruise Browser Feasibility.md`

---

## Required Reading

Before changing code, read these files in order:

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/036 - Cruise Discovery and Capture.md`
5. `docs/Codex Prompts/036a - Embedded Cruise Browser Feasibility.md`
6. the current Cruise of the Week View, ViewModel and tests
7. the browser feasibility View, ViewModel, code-behind and tests
8. the shell navigation and dependency-injection files directly involved

Do not begin implementation until the existing navigation, MVVM and browser
boundaries are understood.

---

## Scope

Implement only Prompt 036b.

Turn the successful 036a browser spike into the first controlled Cruise
Discovery navigation surface.

This step owns:

- trusted cruise-source definitions
- explicit source selection
- the empty discovery state before selection
- browser navigation state
- Back, Forward, Refresh, Stop and Close controls where supported
- HTTPS and trusted-host policy
- controlled unsupported-host and navigation-failure states
- deterministic offline unit tests

This step does **not** own:

- a Cruise capture application contract
- TUI page extraction or Cruise model mapping
- a capture/review panel
- persistence or history across application sessions
- saved cruises, ratings or preferences
- price tracking or comparisons
- external booking actions
- automatic or background browsing

Do not implement Prompts 036c–036g.

---

## Proven 036a Evidence

Treat these as established facts rather than repeating the spike:

- `Avalonia.Controls.WebView` 12.0.1 restores and builds with Kryten's Avalonia
  12.0.5 packages.
- `NativeWebView` renders TUI interactively on Robin's macOS environment.
- the WebView resizes with the parent, scrolls and supports cookie interaction.
- Robin can navigate from Marella Cruise of the Week to Canarian Flavours.
- a hard-coded read-only script can inspect the displayed page.
- a detailed TUI `bookitineraries` anchor can be recovered before TUI replaces
  it with `/cruise/book/flow/cruiseoptions`.
- the package's macOS navigation-completed signal is not reliable enough to be
  the sole definition of a usable page; the visible page may work while the
  status remains `Loading`.
- `WebViewNavigationStartingEventArgs` does not expose the requested address in
  the package API used by Kryten. Do not pretend it supplies a target URI.

Preserve the useful 036a diagnostic behavior unless a small refactor is required
to create the production navigation surface.

---

## Goal

When Robin opens Cruise Discovery, no external page should load.

The surface should initially explain the workflow and display a trusted source
choice similar to:

```text
Cruise Discovery

Choose a trusted cruise source. Nothing loads until you select one.

[ Marella Cruise of the Week ]
```

After explicit source selection, the surface should show:

```text
Source: Marella Cruise of the Week
Host: www.tui.co.uk

[ Back ] [ Forward ] [ Refresh ] [ Stop ] [ Close ]

┌──────────────────────────────────────────────┐
│ Interactive trusted source page              │
└──────────────────────────────────────────────┘
```

The UI may follow existing Kryten styling rather than reproducing this sketch
literally.

---

## Trusted Source Definitions

Introduce a small immutable source definition and catalogue using names that fit
the existing solution.

A definition should contain only demonstrated navigation metadata such as:

- a stable string identifier
- display name
- short description
- trusted HTTPS host
- HTTPS starting address
- whether read-only link diagnostics are currently supported

Initial catalogue contents:

- `Marella Cruise of the Week`
- starting address:
  `https://www.tui.co.uk/cruise/deals/marella-cruise-of-the-week`
- trusted host: `www.tui.co.uk`

Do not add unproven sources merely to create more buttons.

Do not model source identity as an enum. Future providers must be addable by
catalogue registration rather than a shared switch statement.

Do not call this catalogue the Skill registry. Cruise discovery sources and
Kryten Skills are separate concepts.

Keep browser-navigation metadata out of Core. Prefer a focused presentation
boundary unless inspection of the existing architecture demonstrates a clear
application-layer home that introduces no browser concerns.

Validate catalogue definitions when they are constructed:

- identifier and display name are required
- starting address must be absolute HTTPS
- trusted host is required
- starting-address host must match the trusted host
- fragments and query strings are allowed
- invalid definitions fail immediately with a clear developer-facing exception

Do not accept wildcard hosts or substring host matching.

---

## Trusted-Host Policy

Create one focused, independently testable policy for addresses observed or
requested by Cruise Discovery.

An address is trusted only when:

- it is absolute
- it uses HTTPS
- its host equals the selected source's trusted host using case-insensitive exact
  comparison

Examples:

- `https://www.tui.co.uk/cruise/...` → trusted
- `http://www.tui.co.uk/cruise/...` → rejected
- `https://www.tui.co.uk.evil.example/...` → rejected
- `https://tui.co.uk/...` → rejected unless separately registered later
- `about:blank` → browser-internal, not a trusted cruise destination

The policy should distinguish a harmless browser-internal transition such as
`about:blank` from an approved cruise page. It must never present `about:blank`
as a trusted source.

Because the current WebView navigation-start event does not expose the target
URI, enforce the strongest honest boundary supported by the package:

- validate every application-requested address before navigation
- validate addresses exposed by source definitions
- observe the WebView source during lifecycle events
- stop or hide an observed untrusted top-level destination as soon as it can be
  identified
- display a controlled unsupported-host state
- never claim that pre-navigation cancellation was achieved if the API cannot
  provide the requested address

If the WebView exposes a reliable additional event during implementation, use it
only after confirming its behavior and covering it with an isolated adapter test.
Do not introduce a broad request interceptor or log every subresource.

---

## ViewModel and Browser Boundary

Refactor the feasibility state into a focused Cruise Discovery navigation
ViewModel where this reduces duplication. Preserve MVVM:

- source catalogue and selected source belong in ViewModel state
- commands and user-visible state transitions belong in the ViewModel
- the native browser control remains in the Avalonia View/code-behind boundary
- code-behind may forward native events and execute fixed browser operations
- code-behind must not decide which source is trusted

Expected ViewModel state includes equivalents of:

- available sources
- selected source
- whether a browsing session is open
- current observed address
- current observed host
- loading/navigation state
- controlled status and error messages
- Back and Forward availability when the native control can report them
- the existing bounded navigation history and read-only link diagnostics where
  they remain useful

Expected commands include equivalents of:

- select/open source
- Back
- Forward
- Refresh
- Stop
- Close browser

Commands should expose events or an application-owned presentation abstraction;
they must not expose `NativeWebView` through the ViewModel.

Do not add service location or browser-control references to constructors.

---

## Explicit Navigation Rules

No TUI request may occur during:

- dependency injection registration
- ViewModel construction
- View construction
- application startup
- Dashboard display
- shell navigation to Cruise Discovery

The first request occurs only after Robin explicitly selects a source.

Selecting a source should:

1. validate the source definition
2. set the selected source
3. open the browser session
4. request the source's starting address once
5. expose controlled loading state

Selecting the already-open source must not create duplicate controls, duplicate
subscriptions or an accidental navigation loop.

Closing the browser should stop current navigation, release the visible browser
session state and return to the source-selection surface. It should not close the
Kryten application.

---

## Navigation Controls

Support these operations when `NativeWebView` supports them:

- Back
- Forward
- Refresh
- Stop
- Close Browser

The ViewModel must own command availability. The View/code-behind may report
native Back/Forward capability after operations and lifecycle events.

Operations should fail safely:

- no raw platform exception in the UI
- no crash when Back or Forward is unavailable
- no permanent busy state after a failed browser operation
- no fabricated current address

Do not use the unreliable navigation-completed event as the only route out of a
loading state. Use an honest state such as `Page displayed; background activity
may still be continuing` when the control cannot prove completion. A bounded,
user-triggered verification may mark the current page readable.

Do not implement arbitrary address entry in this prompt.

---

## Existing Cruise of the Week Behavior

Preserve the existing Skill-based Cruise of the Week retrieval surface and its
controlled failure result unless a minimal presentation integration is required.

Do not:

- remove the existing Skill
- rewrite the old HTTP provider
- add browser cookies or impersonation to `HttpClient`
- make the browser start automatically to hide the old provider failure

The new discovery navigation surface should be the useful interactive route,
while the old retrieval implementation remains architecturally intact for now.

---

## Error and Offline States

Provide controlled user-facing outcomes for:

- invalid source definition
- offline or page load failure
- unsupported or untrusted observed host
- browser operation failure
- stopped navigation
- closed browsing session

Messages should explain the next useful action without exposing exception,
JavaScript or native-control details.

The application, shell and empty Cruise Discovery view must remain usable
offline. Automated tests must not access external pages.

---

## Tests

Add focused deterministic tests for at least:

### Source Definitions and Catalogue

- the initial catalogue contains exactly the proven Marella source
- identifiers are stable and unique
- source construction rejects missing values
- non-HTTPS starting addresses are rejected
- starting-address and trusted-host mismatch is rejected

### Trusted-Host Policy

- exact trusted HTTPS host is accepted
- HTTP is rejected
- deceptive suffix/prefix hosts are rejected
- unrelated HTTPS hosts are rejected
- relative addresses are rejected
- `about:blank` is treated as internal rather than trusted

### ViewModel Navigation

- construction causes no navigation
- opening the Cruise view causes no navigation
- explicit source selection requests exactly one validated starting address
- Back, Forward, Refresh, Stop and Close requests are explicit
- command availability follows browser state
- observed trusted addresses update presentation state
- observed untrusted destinations produce a controlled state
- closing clears session-only browsing state
- read-only diagnostics remain bounded
- browser failures do not expose raw exception details

Use fakes or event assertions. No automated test may instantiate a native WebView
or contact TUI or another external service.

Update existing 036a tests rather than duplicating obsolete feasibility-only
expectations where the production navigation ViewModel replaces them.

Do not delete tests merely because names or ownership change.

---

## Verification Commands

After implementation, run:

```bash
dotnet restore KrytenAssist.sln
dotnet build KrytenAssist.sln --no-restore
dotnet test KrytenAssist.Avalonia.Tests/KrytenAssist.Avalonia.Tests.csproj --no-build --no-restore
dotnet test KrytenAssist.sln --no-build --no-restore
```

Report exact totals, failures, skipped tests, errors and warnings.

Confirm by test design and code review that no automated test requested an
external website.

Do not suppress package compatibility, vulnerability or compiler warnings.

---

## Documentation

After implementation and automated verification:

- complete this prompt's `Results` section
- record files created and updated
- record exact build and test outcomes
- record trusted-host behavior and any NativeWebView limitation honestly
- leave the main Prompt 036 Playbook and Roadmap unchanged until Robin reviews
  the 036b implementation

Do not mark all of Prompt 036 complete.

---

## Explicitly Out of Scope

Do not implement:

- `ICruisePageCaptureService` or an equivalent capture contract
- parsing the detailed itinerary URL into Cruise domain data
- DOM/HTML Cruise extraction
- capture success, incomplete or ambiguous result models
- a Cruise review panel
- persistence of URLs, cookies, history or Cruise observations
- ratings, notes or preferences
- price history or trends
- source comparison
- new third-party retailers
- external booking automation
- authentication or payment handling
- unattended scraping or background jobs

The detailed Canarian Flavours link proven in 036a is test evidence, not a value
to hard-code into production or fixtures with its live package identifier.

---

## Completion Criteria

Prompt 036b is complete when:

- trusted source definitions and exact-host validation exist
- the catalogue contains only the proven initial Marella source
- Cruise Discovery loads no external page until explicit source selection
- source selection opens the embedded page through a controlled ViewModel event
- Back, Forward, Refresh, Stop and Close are supported where available
- unsupported observed hosts produce a controlled state
- internal `about:blank` transitions are not presented as trusted sources
- the WebView remains isolated from Core and application contracts
- the existing Cruise Skill remains present
- offline deterministic tests cover source, policy and navigation behavior
- the solution builds and all tests pass
- no automated test performs external network access
- this prompt's Results section is complete

Stop after Prompt 036b. Mention future capture work separately rather than
implementing it.

---

## Completion Report

When finished, provide:

### Summary

Describe the trusted-source navigation surface and explicit-navigation behavior.

### Architecture

State where source definitions, trust policy, ViewModel state and native browser
operations live.

### Files Modified

List every created and updated file.

### Automatic-Navigation Check

Confirm that startup, construction, Dashboard display and opening Cruise
Discovery perform no external request.

### Trusted-Host Check

Report exact-host, HTTPS, deceptive-host and `about:blank` behavior, including any
native-control limitation.

### Build and Tests

Report exact commands, totals, failures, skipped tests, errors and warnings.

### Network Check

Confirm no automated test accessed an external website.

### Manual Verification

Report what Robin should verify manually on macOS. Do not claim manual TUI
behavior was tested by the coding agent.

### Scope Check

Confirm no capture contract, Cruise extraction, review, persistence, rating,
price tracking or booking behavior was added.

---

## Results

> Complete during implementation and after automated verification.

### Status

Complete. Robin confirmed the source selection, trusted browsing, navigation
controls, page verification, Stop and Close behavior on macOS on 16 July 2026.
Late native lifecycle events no longer reopen a closed session or overwrite a
verified or explicitly stopped state.

### Architecture

Trusted source definitions, the source catalogue and exact-host policy live in
the Avalonia Cruise Discovery boundary because they describe presentation
navigation rather than Cruise domain data. The existing feasibility ViewModel
and native bridge were evolved in place: the ViewModel owns source selection,
trust decisions, commands and user-visible state, while code-behind performs only
fixed `NativeWebView` operations and forwards native lifecycle/capability events.

No browser, DOM, JavaScript or Avalonia type was introduced into Core or
Application.

### Files Created

- `KrytenAssist.Avalonia/Cruises/Discovery/CruiseDiscoverySource.cs`
- `KrytenAssist.Avalonia/Cruises/Discovery/CruiseDiscoverySourceCatalog.cs`
- `KrytenAssist.Avalonia/Cruises/Discovery/CruiseTrustedHostPolicy.cs`
- `KrytenAssist.Avalonia.Tests/Cruises/Discovery/CruiseDiscoverySourceTests.cs`
- `KrytenAssist.Avalonia.Tests/Cruises/Discovery/CruiseTrustedHostPolicyTests.cs`

### Files Updated

- `KrytenAssist.Avalonia/DependencyInjection/ShellServiceCollectionExtensions.cs`
- `KrytenAssist.Avalonia/ViewModels/CruiseOfTheWeekViewModel.cs`
- `KrytenAssist.Avalonia/ViewModels/CruiseBrowserFeasibilityViewModel.cs`
- `KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml`
- `KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml.cs`
- `KrytenAssist.Avalonia.Tests/DependencyInjection/ShellDependencyInjectionTests.cs`
- `KrytenAssist.Avalonia.Tests/ViewModels/CruiseBrowserFeasibilityViewModelTests.cs`
- `docs/Codex Prompts/036b - Cruise Source Navigation.md`

### Automatic-Navigation Check

Verified through constructor, source-command and dependency-injection tests.
Catalogue construction, ViewModel construction, shell resolution, Dashboard
display and opening the Cruise capability issue no navigation request. The first
TUI address is requested exactly once only after the Marella source command is
executed explicitly.

### Trusted-Host Check

The policy accepts only absolute HTTPS addresses whose host exactly matches
`www.tui.co.uk` case-insensitively. HTTP, relative, unrelated, missing and
deceptive suffix/prefix hosts are rejected. `about:blank` is classified as an
internal browser transition and is neither displayed nor recorded as a trusted
cruise address.

Every application-requested address is validated before navigation. The current
WebView package does not expose the requested URI in its navigation-start event,
so observed top-level sources are checked as soon as `NativeWebView.Source`
reports them; an observed untrusted address stops and hides the browser with a
controlled message. This is not represented as pre-navigation cancellation.

### Build

`dotnet build KrytenAssist.sln --no-restore --disable-build-servers -m:1`

- succeeded
- 0 errors
- 7 existing warnings: 5 `NU1903` SQLite package vulnerability warnings and 2
  `CS0067` unused command-event warnings in `MainWindowViewModel`
- no warning introduced by Prompt 036b

### Tests

- Focused source, trust, navigation and shell tests: 41 passed, 0 failed, 0
  skipped.
- Avalonia tests: 197 passed, 0 failed, 0 skipped.
- Core tests: 62 passed, 0 failed, 0 skipped.
- API tests: 9 passed, 0 failed, 0 skipped.
- Full solution: 268 passed, 0 failed, 0 skipped.

### Network Check

Verified by test design and code review. Tests use source objects, ViewModel event
assertions and dependency-injection resolution only. No automated test creates a
native WebView or requests TUI or another external website.

### Manual Verification

Completed on 16 July 2026. Robin confirmed that source selection, embedded
browsing, page verification and the navigation controls work. Close returns to
source selection and remains closed. Stop retains its stopped state. Delayed
same-address lifecycle events do not replace verified or stopped status.

### Notes

The existing Skill-based Cruise of the Week retrieval remains present and
unchanged. No capture contract, Cruise extraction/mapping, review, persistence,
rating, price tracking, comparison, external booking or unattended browsing was
added.

---

## Lessons Learned

### Implementation

- The successful feasibility bridge could be evolved without duplicating a
  second native browser boundary; browser operations remain isolated in one
  Avalonia View.
- A source catalogue is distinct from the Skill registry: it describes approved
  browsing destinations and can grow without an enum or shared switch.
- Exact host equality is essential. Suffix checks would incorrectly trust hosts
  such as `www.tui.co.uk.evil.example`.
- macOS WebView lifecycle signals remain advisory. User-triggered read
  verification provides a more honest readiness signal than leaving the
  interface permanently busy while a visible page is usable.
- The package cannot expose a navigation target through
  `WebViewNavigationStartingEventArgs`; 036b therefore validates explicit
  requests before navigation and stops an untrusted top-level source as soon as
  it becomes observable, while documenting that limitation honestly.
- Native lifecycle events may arrive after `Stop`; session ownership must be
  checked before applying them so a closed browser cannot reopen itself.
- TUI may also emit delayed same-address navigation-start events after read
  verification. Once an address is verified, identical start events must not
  reset the usable page to a misleading loading state; a genuinely different
  trusted address still begins a new navigation lifecycle.
- Stop has the same delayed-event behavior. A same-address start cannot overwrite
  the explicit stopped state, while a different trusted address or explicit
  browser command can begin a new lifecycle.
- Successful read verification must itself mark the page ready and clear the
  navigation state because the native completion event is not reliable on the
  proven macOS path.

### Review

Robin's manual macOS review passed on 16 July 2026. Prompt 036b is complete.
