# Codex Prompt 036a – Embedded Cruise Browser Feasibility

## Source

Implement Step 1 from:

```text
docs/AI Playbook/036 - Cruise Discovery and Capture.md
```

The current roadmap item is:

```text
Prompt 036 – Cruise Discovery and Capture
```

Prompts 032–035h are implemented and verified.

Read before making changes:

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/036 - Cruise Discovery and Capture.md`
5. this prompt
6. every production or test file directly referenced below

Implement only Prompt 036a.

Do not begin source navigation, capture contracts, TUI extraction, review,
persistence, ratings, history or later Prompt 036 steps.

---

## Goal

Determine whether Avalonia's native embedded WebView is a viable browser surface
for Kryten's Cruise Discovery workflow on Robin's macOS development environment.

Create the smallest reviewable application spike that can prove:

- the WebView package is compatible with the current project
- the application can host the native browser without destabilising the shell
- the TUI Cruise of the Week page loads after an explicit user action
- the page remains interactive
- navigation lifecycle events can be observed
- Kryten can invoke a harmless read-only script such as `document.title`
- visible page text can be sampled without modifying the page
- failures and offline behavior are controlled

This is a feasibility gate, not the production Cruise Discovery design.

The implementation must produce an honest outcome:

```text
Go – embedded browsing and read-only script invocation were proven.
```

or:

```text
No-go – <concise verified reason>.
```

Automated build and tests alone cannot produce a `Go` outcome. Robin must perform
the explicit manual workflow before the final feasibility decision is recorded.

---

## Background

The existing Marella provider requests:

```text
https://www.tui.co.uk/cruise/deals/marella-cruise-of-the-week
```

through `HttpClient`.

TUI currently returns HTTP 403 to that direct request. The same page is usable in
an ordinary interactive browser.

Do not modify the existing provider to imitate a browser. Do not add headers,
cookie replay, proxying, CAPTCHA workarounds or another scraping library.

The purpose of this prompt is to test a genuinely interactive native browser
surface controlled by Robin.

---

## Current Application

Kryten currently targets:

```text
net10.0
Avalonia 12.0.5
Avalonia.Desktop 12.0.5
```

The focused Cruise UI is:

```text
KrytenAssist.Avalonia/Views/CruiseOfTheWeekView.axaml
KrytenAssist.Avalonia/Views/CruiseOfTheWeekView.axaml.cs
KrytenAssist.Avalonia/ViewModels/CruiseOfTheWeekViewModel.cs
```

The existing retrieval workflow must remain intact. Prompt 036a may add a clearly
labelled feasibility area, but it must not remove, disguise or rewrite Prompt
035h's Skill-based retrieval behavior.

The current package project is:

```text
KrytenAssist.Avalonia/KrytenAssist.Avalonia.csproj
```

Avalonia's WebView API is supplied separately through:

```text
Avalonia.Controls.WebView
```

The available WebView package version may not exactly equal the existing
Avalonia package version. Verify compatibility through package metadata, restore
and build. Do not downgrade or broadly upgrade the existing Avalonia packages to
force the experiment to compile.

If no compatible stable package can be used with the current application, stop
and report `No-go` with the restore or compatibility evidence.

---

## Required User Experience

Add a clearly temporary feasibility surface reachable from the existing Cruise
of the Week page.

Before Robin explicitly starts the experiment, show a compact area such as:

```text
Embedded TUI Browser Feasibility

Test whether TUI's Cruise of the Week can be displayed safely inside Kryten.

[ Load TUI in Embedded Browser ]
```

Requirements before the button is pressed:

- do not create an external request
- do not set the WebView source to the TUI address in XAML
- do not navigate in a constructor, loaded event or ViewModel initialization
- do not invoke page script
- show no fabricated success state

After Robin presses the button:

- display or activate the embedded WebView
- navigate to the exact configured HTTPS TUI address
- show a concise loading or navigation status
- preserve ordinary page interaction
- provide a `Stop` action while navigation is active where the API supports it
- provide `Refresh` after the browser is ready
- display the current address or trusted host
- surface navigation failure without crashing the application

After navigation completes, provide:

```text
[ Verify Read Access ]
```

That action should invoke trusted, hard-coded, read-only JavaScript to obtain:

- `document.title`
- the current document address if supported safely
- one bounded sample of visible page text

The sample must be small enough for diagnostics. Do not copy or display the full
page source, full DOM or all body text.

Display a concise result such as:

```text
Page title: Marella Cruise of the Week | TUI.co.uk
Visible text sample received: Yes
```

Do not attempt to identify or parse a cruise in this prompt.

---

## Feasibility UI Placement

Keep the spike visually and structurally separate from the existing Cruise of
the Week result panel.

The embedded browser must receive a bounded usable area and must not accidentally
cover the dashboard, navigation rail or Assistant workspace.

Native browser controls can have clipping, z-order and scrolling constraints.
Do not place the WebView inside the existing vertically scrolling content if that
causes native-host rendering or input problems.

A focused child view or bounded panel is preferred over adding browser logic to
`MainWindow`.

Do not introduce a new application-wide navigation architecture in this spike.

---

## Architecture

### Presentation Boundary

`NativeWebView` is an Avalonia presentation concern.

It must not appear in:

- `KrytenAssist.Core`
- `KrytenAssist.Application`
- `KrytenAssist.Infrastructure`
- Skill models or interfaces
- the existing Cruise domain

Production changes for this feasibility spike should remain in:

```text
KrytenAssist.Avalonia/
```

Tests should remain in:

```text
KrytenAssist.Avalonia.Tests/
```

### MVVM and Native Control Bridge

Keep observable state and commands in a focused ViewModel where practical.

A small view-owned bridge or minimal code-behind is acceptable for operations
that require a concrete native control instance, including:

- `Navigate`
- `Refresh`
- `Stop`
- `InvokeScript`
- forwarding navigation events

The code-behind must not:

- parse Cruise data
- contain business rules
- access dependency injection through service location
- perform persistence
- call the existing Marella provider
- execute arbitrary script supplied by the ViewModel or user

The script used for verification must be hard-coded in the trusted bridge or a
small dedicated feasibility service.

### Suggested State

Use only state needed by the spike, for example:

- `HasStarted`
- `IsNavigating`
- `IsPageReady`
- `StatusMessage`
- `ErrorMessage`
- `CurrentAddress`
- `PageTitle`
- `HasVisibleTextSample`

Do not introduce general-purpose browser tab, history, source catalogue or
capture abstractions. Those belong to later Prompt 036 steps after feasibility is
proven.

---

## Address and Safety Rules

Use exactly this starting address:

```text
https://www.tui.co.uk/cruise/deals/marella-cruise-of-the-week
```

Requirements:

- the address must be an absolute HTTPS URI
- keep it in one clearly named feasibility constant or narrowly scoped option
- do not read it from the existing `CruiseOfTheWeek:Marella` HTTP-provider option
- do not share cookies with Safari manually
- do not import browser history, passwords or profile data
- do not inject application state into the page
- do not accept cookie consent automatically
- do not intercept login, passenger or payment information
- do not expose arbitrary local navigation

Ordinary links needed to prove interactive browsing may work inside the embedded
page. Prompt 036b will introduce the durable trusted-host and external-navigation
policy. Do not claim that policy is complete in this spike.

If navigation leaves `tui.co.uk`, display the resulting host visibly and record
the limitation for Prompt 036b. Do not silently broaden a trusted-host list.

---

## Package and Initialization Rules

Inspect the official package API and current project before editing.

Requirements:

- add only the package or initialization required for native WebView support
- use a stable version compatible with the existing Avalonia 12 application
- do not downgrade existing Avalonia packages
- do not upgrade all Avalonia packages as an incidental change
- do not add Cef, Playwright, Selenium or a second browser framework
- do not add an HTTP scraping fallback
- keep platform initialization changes minimal and documented

If a macOS entitlement, application setting or native dependency is required,
add only what the official WebView package requires and explain it in the
completion report.

Do not invent undocumented startup calls. Use the package's actual public API.

---

## Error and Cancellation Behavior

Handle:

- package or adapter unavailable
- navigation start
- navigation completion
- navigation failure
- user stop
- refresh
- script invocation before readiness
- script invocation failure
- application offline
- view disposal during navigation

Cancellation or stopping navigation must not be reported as a successful page
load.

Dispose or detach native-control event handlers when the view is detached or
disposed according to the established Avalonia lifecycle.

Do not show raw stack traces, JavaScript or native exception details in the UI.
Retain concise diagnostic detail in the completion report where useful.

---

## Allowed Changes

Expected production changes are limited to a small subset of:

```text
KrytenAssist.Avalonia/KrytenAssist.Avalonia.csproj
KrytenAssist.Avalonia/Program.cs
KrytenAssist.Avalonia/Views/CruiseOfTheWeekView.axaml
KrytenAssist.Avalonia/Views/CruiseOfTheWeekView.axaml.cs
KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml
KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml.cs
KrytenAssist.Avalonia/ViewModels/CruiseBrowserFeasibilityViewModel.cs
KrytenAssist.Avalonia/Services/ICruiseBrowserFeasibilityBridge.cs
KrytenAssist.Avalonia/DependencyInjection/ShellServiceCollectionExtensions.cs
```

Use the smallest coherent set. The exact bridge name may be adjusted to match
the implementation, but do not create a reusable production browser framework
in this step.

Expected test changes are limited to:

```text
KrytenAssist.Avalonia.Tests/ViewModels/CruiseBrowserFeasibilityViewModelTests.cs
KrytenAssist.Avalonia.Tests/DependencyInjection/ShellDependencyInjectionTests.cs
```

Update this prompt's `Results` section after implementation and automated
verification. Record the final `Go` or `No-go` only after Robin completes the
manual workflow.

Do not modify:

```text
KrytenAssist.Core/
KrytenAssist.Core.Tests/
KrytenAssist.Application/
KrytenAssist.Infrastructure/
KrytenAssist.Api/
KrytenAssist.Api.Tests/
KrytenAssist.Client/
```

Do not update the Roadmap or the main Prompt 036 Playbook until Robin has reviewed
the feasibility result.

---

## Automated Tests

Automated tests must remain offline and deterministic.

Test focused ViewModel or bridge-facing behavior for:

- construction causes no navigation
- explicit start requests the one configured TUI address
- invalid state cannot verify read access before page readiness
- navigation-start state
- successful navigation-completion state
- failed-navigation state
- stop state
- refresh availability
- successful title and bounded-text diagnostic mapping
- script failure becomes a controlled error
- stale navigation or script completions do not overwrite newer state where
  applicable
- null dependency guards

Do not instantiate a live native WebView in ordinary unit tests.

Use a fake bridge or a small state adapter where needed.

No automated test may access TUI or another external service.

Preserve all existing Prompt 035h ViewModel and shell tests.

---

## Manual Verification Gate

After the build and automated tests pass, report that manual verification is
required. Do not claim `Go` before Robin performs it.

Robin should verify on macOS:

1. start Kryten normally
2. confirm the Dashboard and Assistant still render correctly
3. open Cruise of the Week
4. confirm the TUI page has not loaded automatically
5. press `Load TUI in Embedded Browser`
6. confirm the real TUI page renders inside Kryten
7. accept or reject cookie controls manually if shown
8. scroll and use a harmless link within the TUI cruise area
9. confirm navigation status remains accurate
10. press `Verify Read Access`
11. confirm a plausible page title is displayed
12. confirm a bounded visible-text sample was detected
13. test Refresh
14. test Stop during a navigation where practical
15. temporarily test offline or a controlled navigation failure
16. return to Dashboard and Assistant and confirm no WebView overlaps them

Record:

- macOS test date
- WebView package version
- whether the page rendered
- whether interaction worked
- whether `document.title` returned successfully
- whether bounded visible text could be read
- any cookie, layout, z-order or navigation limitations
- final `Go` or `No-go`

Do not record cookies, personal identifiers, tracking query strings or page
content beyond the small diagnostic needed to prove access.

---

## Explicitly Out of Scope

Do not implement:

- general Cruise Discovery source chips
- a reusable source catalogue
- trusted-host navigation policy beyond the single starting-address safeguards
- cruise capture contracts
- TUI Cruise parsing or extraction
- `CruiseObservation` creation
- capture review
- saved cruises
- persistence
- price history
- ratings or preferences
- alerts
- background browsing
- scheduled browsing
- Iglu support
- external booking integration
- browser tabs
- browser history persistence
- authentication management
- payment handling
- CAPTCHA bypass
- modifications to the original Marella HTTP parser/provider

Do not remove existing functionality to make the feasibility spike easier.

---

## Verification Commands

After implementation:

```bash
dotnet restore KrytenAssist.sln
dotnet build KrytenAssist.sln --no-restore
dotnet test KrytenAssist.Avalonia.Tests/KrytenAssist.Avalonia.Tests.csproj --no-build --no-restore
dotnet test KrytenAssist.sln --no-build --no-restore
```

Report exact totals, failures, skipped tests, errors and warnings.

Confirm through test doubles or command review that no automated test requested an
external site.

Do not suppress package compatibility, vulnerability or compiler warnings.

---

## Completion Criteria

Prompt 036a implementation is ready for Robin's manual gate when:

- package compatibility has been verified without broad Avalonia version changes
- the solution builds
- all automated tests pass offline
- the existing Cruise of the Week workflow remains present
- no TUI request occurs before explicit user action
- the embedded page has a bounded non-overlapping host area
- navigation status and failures are controlled
- read access uses only hard-coded harmless diagnostics
- no Cruise extraction or later Prompt 036 behavior was added
- the Results section below contains the automated outcome
- the manual workflow and required decision are clearly reported

Prompt 036a is fully complete only after Robin records the manual result and a
final `Go` or `No-go` decision.

---

## Completion Report

When implementation work finishes, provide:

### Summary

Describe the minimal feasibility surface and what it proves.

### Package and Platform

Report:

- WebView package and version
- current Avalonia versions
- initialization changes
- native macOS browser engine or adapter reported by the control, if available

### Files Modified

List every created or modified file.

### Automatic-Navigation Check

Confirm that construction, startup, Dashboard navigation and opening the Cruise
page do not request TUI before the explicit load action.

### Focused Tests

Report command, total, passed, failed and skipped.

### Full Tests

Report totals by project and combined totals.

### Build

Report command, result, errors and warnings. Separate new warnings from existing
warnings.

### Network Check

Confirm no automated test accessed an external website.

### Manual Gate

Use one:

```text
Awaiting Robin's manual macOS verification – no feasibility decision recorded.
```

```text
Go – embedded TUI rendering, interaction and read-only script access were proven
on <date>.
```

```text
No-go – <concise verified reason>.
```

### Scope Check

Confirm no capture, parsing, persistence, ratings, alerts, scheduling, booking or
HTTP-provider workaround was introduced.

---

## Results

> Complete during implementation and after Robin's manual verification.

### Status

Complete. Robin's manual verification succeeded on 16 July 2026. Embedded TUI
rendering, resizing, scrolling, cookie interaction, offer navigation and bounded
read-only page access are proven.

### Feasibility Decision

Go – embedded TUI rendering, interaction and read-only script access were proven
on 16 July 2026. The read-only diagnostic recovered the detailed Canarian
Flavours itinerary address, including itinerary, ship, sailing date and package
query values, before TUI redirected into its generic booking flow.

### Package and Platform

- Added `Avalonia.Controls.WebView` 12.0.1.
- Existing `Avalonia` and `Avalonia.Desktop` packages remain at 12.0.5.
- The WebView package targets .NET 10 and depends on Avalonia 12.0.0 or later;
  restore and build succeeded without a downgrade or broad package upgrade.
- No additional application initialization was required by the documented API.
- The package uses native WebKit on macOS; the actual adapter remains to be
  confirmed during Robin's manual run.

### Files Created

- `KrytenAssist.Avalonia/ViewModels/CruiseBrowserFeasibilityViewModel.cs`
- `KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml`
- `KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml.cs`
- `KrytenAssist.Avalonia.Tests/ViewModels/CruiseBrowserFeasibilityViewModelTests.cs`

### Files Updated

- `KrytenAssist.Avalonia/KrytenAssist.Avalonia.csproj`
- `KrytenAssist.Avalonia/ViewModels/CruiseOfTheWeekViewModel.cs`
- `KrytenAssist.Avalonia/Views/CruiseOfTheWeekView.axaml`
- `KrytenAssist.Avalonia.Tests/ViewModels/CruiseOfTheWeekViewModelTests.cs`
- `KrytenAssist.Avalonia.Tests/DependencyInjection/ShellDependencyInjectionTests.cs`
- `docs/Codex Prompts/036a - Embedded Cruise Browser Feasibility.md`

### Build

`dotnet build KrytenAssist.sln --no-restore`

- succeeded
- 0 errors
- 5 existing `NU1903` warnings for `SQLitePCLRaw.lib.e_sqlite3` 2.1.11
- 2 existing `CS0067` unused command-event warnings in `MainWindowViewModel`
- no warnings introduced by Prompt 036a

### Tests

- Focused feasibility tests: 14 passed, 0 failed, 0 skipped.
- Avalonia tests: 174 passed, 0 failed, 0 skipped.
- Core tests: 62 passed, 0 failed, 0 skipped.
- API tests: 9 passed, 0 failed, 0 skipped.
- Full solution: 245 passed, 0 failed, 0 skipped.
- All tests used ViewModel state and event doubles; no automated test created a
  native WebView or accessed TUI or another external website.

### Manual Verification

Performed on 16 July 2026. The initial fixed-height host rendered TUI but did not
resize with its parent and did not expose enough usable page area to reach the
cookie action.

After the responsive host correction, Robin confirmed that:

- the cookie action could be pressed
- the embedded page resized with the parent window
- navigation into Marella Cruise of the Week worked
- the Canarian Flavours offer was visible
- Verify Page Access executed successfully
- the diagnostic exposed the detailed `bookitineraries/Canarian-Flavours-101842`
  link with its itinerary, ship, sailing date and package query values

The final feasibility decision is `Go`.

### Lessons Learned

- NativeWebView is a viable foundation for interactive TUI browsing on Robin's
  macOS environment even though unattended HTTP retrieval is blocked.
- A WebView must occupy a star-sized layout row to provide native page scrolling
  and responsive resizing; a fixed-height browser inside the surrounding content
  layout made the cookie flow unusable.
- Interactive browsing remains valuable independently of automatic extraction,
  so later Prompt 036 work should preserve a useful manual path when capture is
  unavailable.
- TUI intercepts offer navigation and replaces the detailed itinerary URL with a
  generic booking-flow address. Reading qualifying anchor destinations before
  the click preserves the useful source reference without unattended scraping.

### Notes

- The existing Skill-based Cruise of the Week retrieval remains present and
  unchanged.
- Construction, service resolution, Dashboard navigation and opening the Cruise
  page do not navigate the WebView. The TUI address is supplied only when Robin
  executes `Load TUI in Embedded Browser`.
- The feasibility bridge uses only hard-coded, read-only diagnostics. It returns
  the page title, current address, a Boolean indicating whether a bounded
  512-character visible-text sample exists, and at most ten bounded trusted TUI
  anchor destinations containing cruise identifiers. It does not return the
  text sample or parse Cruise data.
- No capture, persistence, ratings, alerts, scheduling, booking or HTTP-provider
  workaround was introduced.
