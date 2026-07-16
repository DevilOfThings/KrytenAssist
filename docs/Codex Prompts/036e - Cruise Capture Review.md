# Codex Prompt 036e – Cruise Capture Review

## Implementation Prompt

Implement Step 5 of:

`docs/AI Playbook/036 - Cruise Discovery and Capture.md`

This prompt follows:

- `docs/Codex Prompts/036a - Embedded Cruise Browser Feasibility.md`
- `docs/Codex Prompts/036b - Cruise Source Navigation.md`
- `docs/Codex Prompts/036c - Cruise Capture Contract.md`
- `docs/Codex Prompts/036d - TUI Cruise Capture Adapter.md`

---

## Required Reading

Before changing code, read these files in order:

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/036 - Cruise Discovery and Capture.md`
5. Prompts 036a–036d, including Results and Lessons Learned
6. the current Cruise Discovery View, code-behind and ViewModel
7. `CruisePageCaptureRequest`, `CruiseCaptureResult`, status and interface
8. `TuiCruisePageCaptureService` and its version-1 payload fixture/tests
9. existing Cruise domain models
10. shell, dependency-injection and clock conventions

Do not begin implementation until the native-browser boundary, capture contract
and private TUI payload schema are understood.

---

## Scope

Implement only Prompt 036e.

Connect the live embedded TUI page to the 036d capture adapter through an
explicit user action, then present the resulting observation for review without
saving it.

This step owns:

- `Capture Displayed Cruise`
- a fixed source-specific read-only browser script
- bounded payload transfer from the View to the ViewModel
- capture running, success, incomplete, ambiguous, unsupported, failed and
  cancelled presentation states
- a non-persistent Cruise review panel
- explicit external opening of the current trusted source page
- dependency-injection composition of the 036d adapter
- ViewModel, browser-bridge and presentation tests
- manual live TUI verification with Robin

This step does **not** own:

- saving or accepting a captured cruise into storage
- price history or trend calculations
- ratings, notes or preferences
- source comparison
- background capture or monitoring
- booking automation

Do not implement Prompts 036f or 036g beyond tests directly required for this
step.

---

## Goal

The live workflow should be:

```text
Select Marella Cruise of the Week
        ↓
Browse TUI interactively
        ↓
Open or display one specific offer
        ↓
Click Capture Displayed Cruise
        ↓
Read a bounded source-specific payload
        ↓
Pass it to ICruisePageCaptureService
        ↓
Display success or an honest controlled outcome
        ↓
Review the captured Cruise values
        ↓
Optionally open the trusted page externally
```

No capture occurs automatically.

---

## User Experience

When no source is selected, preserve the 036b empty Cruise Discovery state.

When browsing, preserve:

- source name and trusted host
- Back, Forward, Refresh, Stop and Close
- current address and bounded diagnostics
- responsive embedded page behavior
- cookie consent as Robin's manual interaction

After the page is readable, show:

```text
[ Capture Displayed Cruise ] [ Open at TUI ]
```

During capture:

- disable duplicate capture
- display `Capturing the displayed cruise...`
- show a Cancel action
- keep the application responsive
- do not navigate or modify the TUI page

On successful capture, show a review panel containing every available value.

On non-success, show the controlled result message and an appropriate next step,
such as opening one specific itinerary and trying again.

Closing the browser must clear the session-only capture review.

Starting a genuinely different navigation must clear an earlier review so it is
never presented as belonging to the new page.

---

## Fixed Browser Extraction Boundary

The native browser View/code-behind may execute one fixed, hard-coded,
source-specific read-only extraction script.

The ViewModel must never provide arbitrary script text.

Prefer a focused presentation type such as:

```text
TuiCruiseCaptureScript
```

placed under an Avalonia Cruise Discovery/TUI presentation namespace. It may
contain the fixed script as a constant or build it entirely from application
owned constants. It must not accept user input, page text or AI output as code.

The script must:

- read only the currently displayed top-level page
- never click, submit, focus, scroll, accept cookies or alter the DOM
- never access local files or Kryten services
- never read cookies, local storage, session storage or credentials
- never return full page HTML
- never return arbitrary visible text beyond tightly bounded candidate fields
- return JSON matching the 036d version-1 payload
- return at most a small bounded number of candidates
- truncate every extracted string to a documented maximum
- keep the complete serialized payload within
  `CruisePageCaptureRequest.MaximumPagePayloadLength`

If the script result is empty, invalid or over the Application bound, report a
safe controlled capture failure. Do not pass it to the adapter.

Do not reuse the earlier page-diagnostic JSON as if it were the capture payload;
the 036d adapter requires `{ version, candidates }`.

---

## TUI Extraction Strategy

Implement only page shapes demonstrated by the embedded Marella workflow.

Prefer data sources in this order:

1. stable structured data already published in the page, such as relevant JSON-LD
   or explicit data attributes
2. semantic/accessibility attributes and labelled content
3. the detailed `bookitineraries` anchor and its query parameters
4. small TUI-specific selectors and bounded text in the nearest semantic offer
   container

Do not scrape the entire document text and guess.

The detailed itinerary anchor may provide:

- itinerary identity/code
- title from the `/bookitineraries/<title>-<code>` path
- sailing date
- duration
- departure airport or port code
- ship code
- package identifier

An opaque ship code is not a ship name. Do not map it to `shipName` unless a
demonstrated page value provides the human-readable ship name.

A package identifier may be used as the provider offer identifier but must not be
shown in logs or documentation. If a smaller stable itinerary/package identity
is available, prefer it.

Price extraction must retain its visible basis. Do not treat a total as per
person or infer a price absent from the displayed page.

The script may return incomplete candidates. The 036d adapter owns required-field
validation and will report missing fields honestly.

Do not add broad fallback heuristics merely to force the Canarian Flavours page
to succeed. A controlled Incomplete result is preferable to fabricated data.

---

## Candidate Rules

The fixed script should scope candidates to the displayed or selected offer.

Acceptable strategies include:

- one specific itinerary page → one candidate
- one Cruise of the Week promotion with one qualifying detailed anchor → one
  candidate
- several qualifying offer anchors → several candidates, allowing 036d to return
  Ambiguous

Do not silently choose the first link when several offers qualify.

Limit candidate count to a documented small maximum, no greater than ten.

Do not send unrelated navigation, footer, recommendation or advertising links as
Cruise candidates.

---

## Browser-to-ViewModel Flow

Preserve MVVM responsibilities:

### ViewModel

Owns:

- Capture command availability
- capture cancellation
- busy and result state
- creation of `CruisePageCaptureRequest`
- calling `ICruisePageCaptureService`
- mapping the result to review presentation properties
- clearing stale capture state on navigation/close
- requesting explicit external opening

### View/code-behind

Owns only:

- invoking the fixed TUI script on `NativeWebView`
- returning its serialized payload and current address to the ViewModel
- cancelling/ignoring a script result when navigation or session changes
- asking Avalonia's platform launcher to open a validated external URI

The code-behind must not parse Cruise fields or construct domain models.

Use a small event or presentation-owned bridge to connect the command to the
native control. Do not pass `NativeWebView` into the ViewModel.

---

## Capture Request Creation

The ViewModel should construct `CruisePageCaptureRequest` using:

- selected source identifier from the 036b source definition
- retail source: `CruiseSource("tui", "TUI")` supplied through a source-specific
  mapping owned by the presentation capture bridge, not inferred from
  `CruiseProvider`
- current verified HTTPS source reference returned by the script/browser
- current timestamp from the existing `IClock`
- bounded version-1 payload

Do not use `DateTimeOffset.Now` directly when an existing clock is available.

Validate that the source session and address are still current before applying a
capture result.

If navigation, Close or a new capture invalidates an in-flight operation, ignore
the stale result and cancel where possible.

---

## Capture ViewModel State

Add focused state, using a child ViewModel if that keeps the navigation ViewModel
manageable.

Expected state includes equivalents of:

- `IsCapturing`
- `CanCapture`
- `CaptureStatus`
- `CaptureMessage`
- `HasCaptureError`
- `CapturedObservation`
- `HasCapturedObservation`
- `MissingFields`
- review display properties

Expected commands include equivalents of:

- Capture Displayed Cruise
- Cancel Capture
- Clear Review, if useful
- Open at TUI

Capture should be available only when:

- a supported source session is open
- the current address is trusted
- the page has been read/verified or otherwise explicitly established as usable
- no capture is already running
- no unsupported-host state is active

Do not rely solely on the unreliable native navigation-completed event.

---

## Cancellation and Stale Results

Use one `CancellationTokenSource` per capture attempt.

Requirements:

- Cancel requests cancellation once
- cancellation is not shown as a failure
- a stale result cannot replace a newer capture
- navigation to a different address cancels/invalidates capture
- Close cancels/invalidates capture and clears review
- disposal/detachment cannot update an unrelated new session
- command availability refreshes in `finally`

The fixed script API may not support true cancellation. If so, cancel the
ViewModel operation and ignore the eventual script result using a capture/session
generation or address check.

Do not block the UI thread.

---

## Review Panel

On `Success`, present available values from `CruiseObservation`:

- cruise title
- operator/provider
- retail source
- ship
- departure date
- duration
- departure port, when present
- itinerary summary, when present
- every captured price with currency and basis
- promotion summary, when present
- source reference
- observation timestamp

Use existing UK-oriented date and price display conventions where possible.

Clearly label operator and source separately.

Do not show raw payload, package identifiers, JSON, selectors or JavaScript.

Do not add Save, Accept, Rate, Watch or Compare actions. Prompt 037 owns
persistence.

---

## Controlled Outcome Presentation

Map the 036c/036d result without replacing its meaning:

- Success → show review
- Incomplete → show safe message and missing field names in user-friendly form
- Ambiguous → ask Robin to open one specific itinerary and retry
- Unsupported → explain that the displayed TUI page cannot currently be captured
- Failed → suggest refresh and retry
- Cancelled → return to a neutral non-error state

Do not expose raw exception messages.

Do not manufacture a partial `CruiseObservation` from an incomplete result.

---

## Open at TUI

Add an explicit action that opens the current trusted source reference using the
platform's external launcher.

Requirements:

- action is user initiated
- URI must be absolute HTTPS
- host must pass the selected source's exact trusted-host policy
- unsupported or internal addresses are rejected safely
- launcher failure displays a controlled message
- no login, basket, passenger, payment or booking action is automated

The ViewModel requests external opening; the Avalonia View/platform boundary
performs it.

Do not add arbitrary URL entry.

---

## Dependency Injection

Compose the 036d adapter through an existing desktop composition extension.

Requirements:

- call `AddTuiCruiseCapture` from an appropriate Avalonia composition extension,
  not by adding another manual concrete registration to `Program.cs`
- `CruiseOfTheWeekViewModel` or a focused capture child receives
  `ICruisePageCaptureService` through constructor injection
- pass the existing clock through the composition/ViewModel boundary
- no service location
- resolving the shell performs no capture, script invocation, browser navigation
  or external opening

Do not change the adapter lifetime unless tests demonstrate a need.

---

## Tests

Add deterministic offline tests covering at least:

### Fixed Script Contract

- script is fixed and source-specific
- no request, ViewModel or user value is interpolated as executable code
- script contains no cookie/local-storage/session-storage access
- result schema is version 1 with bounded candidates and fields
- representative local fictional DOM/page data produces the 036d fixture shape,
  or an isolated script-result fixture proves the bridge contract
- empty, invalid and over-limit script results fail safely

Do not instantiate a live WebView in automated tests if the package cannot do so
deterministically. Test the bridge/parser boundary with fakes.

### Capture ViewModel

- construction and source selection do not capture
- command is unavailable without a supported readable page
- explicit capture requests exactly one payload
- duplicate capture is disabled
- request retains source identity, trusted address and clock timestamp
- every result status maps to the correct presentation state
- success exposes every review value
- incomplete never fabricates an observation
- cancellation is neutral
- stale results after navigation, Close or a newer capture are ignored
- navigation clears previous review
- Close clears review and capture state

### External Opening

- command is available only for a trusted HTTPS current address
- explicit command requests one external open
- deceptive, unrelated, HTTP and `about:blank` addresses are rejected
- launcher failure is controlled

### Dependency Injection

- shell graph resolves with `ICruisePageCaptureService`
- implementation is the 036d TUI adapter
- construction causes no script, capture, WebView navigation or external opening

### Regression

- 036b navigation and delayed-event behavior still passes
- 036d adapter tests remain unchanged
- old Skill-based retrieval remains present
- no test accesses TUI or another external website

Use fakes/events for browser payload and launcher operations.

---

## Manual Verification

After automated verification, ask Robin to test on macOS:

1. open Cruise Discovery and confirm no automatic page load
2. select Marella Cruise of the Week
3. accept cookies manually if required
4. verify/read the page
5. click Capture Displayed Cruise on the promotion page
6. if ambiguous/incomplete, open the specific Canarian Flavours itinerary and
   retry
7. compare every successful review value with the visible TUI page
8. verify Cancel does not show an error
9. navigate elsewhere and confirm the old review clears
10. verify Open at TUI opens only the current trusted page
11. close the embedded browser and confirm capture state remains cleared

Record the page shape and outcome, but do not record cookies, tracking values,
personal data or the live package identifier in fixtures or documentation.

If live extraction is incomplete, record exactly which demonstrated fields are
unavailable. Do not expand the script into broad scraping during verification.

---

## Verification Commands

After implementation:

```bash
dotnet restore KrytenAssist.sln
dotnet build KrytenAssist.sln --no-restore
dotnet test KrytenAssist.Avalonia.Tests/KrytenAssist.Avalonia.Tests.csproj --no-build --no-restore
dotnet test KrytenAssist.Core.Tests/KrytenAssist.Core.Tests.csproj --no-build --no-restore
dotnet test KrytenAssist.sln --no-build --no-restore
```

Report exact totals, failures, skipped tests, errors and warnings.

Confirm no automated test performed external work.

Do not suppress compiler, package or vulnerability warnings.

---

## Documentation

After implementation and automated verification:

- complete this prompt's `Results` section
- list every created and updated file
- document the fixed script boundary and payload limits
- record outcome and review behavior
- record exact build and test totals
- record the manual workflow as awaiting Robin
- leave the main Prompt 036 Playbook and Roadmap unchanged until Robin reviews
  the live 036e workflow

Do not mark Prompt 036 complete.

---

## Explicitly Out of Scope

Do not implement:

- persistence of captured observations
- database migrations
- price history or trends
- ratings, notes or preferences
- source comparison
- AI extraction
- background capture, scraping or monitoring
- scheduled jobs or alerts
- automatic cookie acceptance
- automatic external opening
- login, basket, passenger, payment or booking automation
- the complete Prompt 042 Cruise Dashboard

Do not add a temporary Save button.

---

## Completion Criteria

Prompt 036e is implementation-complete when:

- capture occurs only after explicit user action
- one fixed TUI read-only script produces the bounded version-1 payload
- no arbitrary script enters from the ViewModel
- the ViewModel calls `ICruisePageCaptureService` with the current trusted source,
  address and clock time
- capture cancellation and stale-result protection exist
- every controlled result has honest presentation
- a successful observation is displayed in a non-persistent review panel
- operator and retail source are labelled separately
- navigation and Close clear stale review state
- Open at TUI is explicit and exact-host validated
- the 036d adapter is composed through DI
- shell resolution performs no external work
- all automated tests pass offline
- this prompt's Results section is complete

Prompt 036e is fully complete after Robin performs the manual live workflow and
the result is recorded.

Stop after Prompt 036e.

---

## Completion Report

When implementation finishes, provide:

### Summary

Describe the explicit live capture and non-persistent review workflow.

### Architecture

Explain View, ViewModel, Application and Infrastructure responsibilities.

### Fixed Script Boundary

Report candidate/field bounds, prohibited data access and stale-result handling.

### Review and Outcomes

Report displayed values and every controlled status.

### External Opening

Report exact-host validation and launcher behavior.

### Files Modified

List every created and updated file.

### Build and Tests

Report exact commands, totals, failures, skipped tests, errors and warnings.

### Network Check

Confirm no automated test performed external work.

### Manual Gate

Use one:

```text
Awaiting Robin's manual live TUI verification.
```

```text
Passed on <date> – <concise verified workflow and page shape>.
```

```text
Partial on <date> – <honest limitation and next action>.
```

### Scope Check

Confirm no persistence, history, rating, comparison, monitoring or booking
behavior was added.

---

## Results

> Complete during implementation and after Robin's manual verification.

### Status

Complete. Automated verification and Robin's manual live TUI workflow passed.

### Architecture

The Avalonia View owns only the fixed native-browser script invocation and OS
launcher. The ViewModel owns commands, trusted-address validation, cancellation,
stale-result rejection and review state. The Application capture contract remains
the boundary, with the TUI adapter isolated in Infrastructure.

### Files Created

- `KrytenAssist.Avalonia/Cruises/Discovery/TuiCruiseCaptureScript.cs`
- `KrytenAssist.Avalonia.Tests/ViewModels/CruiseCaptureReviewViewModelTests.cs`

### Files Updated

- `KrytenAssist.Avalonia/DependencyInjection/ShellServiceCollectionExtensions.cs`
- `KrytenAssist.Avalonia/ViewModels/CruiseBrowserFeasibilityViewModel.cs`
- `KrytenAssist.Avalonia/ViewModels/CruiseOfTheWeekViewModel.cs`
- `KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml`
- `KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml.cs`
- `docs/Codex Prompts/036e - Cruise Capture Review.md`

### Fixed Script Boundary

The hard-coded TUI script reads at most 10 detailed itinerary candidates and
truncates every candidate field to 512 characters. It does not click, navigate,
alter the DOM, return page HTML, or access cookies, local storage or session
storage. The bridge rejects blank and over-limit results before the adapter.
Capture generations and cancellation prevent results from an earlier page from
appearing after navigation or Close. It reads only the top-level document and up
to three open roots of TUI's demonstrated `tui-product-cards` component.

### Capture Outcomes and Review

Explicit capture presents Success, Incomplete, Ambiguous, Unsupported, Failed
and Cancelled results supplied by the adapter. A successful observation displays
all available offer, source, itinerary, price, promotion and observation values
in a review-only panel. No result is saved. Incomplete results include their
missing fields and the adapter's next-step message.

### External Opening

`Open at TUI` emits only the current absolute address after exact trusted-host
classification. The View uses Avalonia's platform launcher and reports a
controlled error if the OS refuses the request.

### Dependency Injection

`AddShell` composes the 036d TUI capture adapter. The service and clock are
constructor-injected into the Cruise of the Week and browser ViewModels; shell
resolution performs no browser or network work.

### Build

Passed: `dotnet build KrytenAssist.sln --no-restore`.

0 errors. 7 warnings: five existing NU1903 SQLite package-advisory warnings and
two existing unused-event warnings in `MainWindowViewModel`.

### Tests

Passed: `dotnet test KrytenAssist.sln --no-build --no-restore`.

327 passed, 0 failed, 0 skipped:

- Core: 71
- Avalonia: 247
- API: 9

### Network Check

Verified. All tests use fakes or fixed payloads; no automated test opens a browser
or contacts TUI.

### Manual Verification

Passed on 16 July 2026. Robin opened the embedded Marella Cruise of the Week
landing page and selected `Capture Displayed Cruise`. Kryten entered TUI's open
`tui-product-cards` shadow root, deduplicated the repeated Canarian Flavours
itinerary link, captured the displayed offer successfully and presented the
session-only review.

### Notes

No persistence, price history, ratings, preferences, comparison, monitoring or
booking behavior was added. The review is cleared on genuine navigation and
when the embedded browser closes.

---

## Lessons Learned

> Complete after implementation and manual review.

- Keeping extraction fixed in the View preserves the native-browser security
  boundary while allowing the ViewModel and adapter to remain independently
  testable.
- A detailed itinerary link is useful structured evidence, but opaque TUI codes
  must not be presented as human-readable ship or port names.
- Session-only review state needs the same lifecycle discipline as browser state;
  navigation generations prevent a valid result from being attached to the
  wrong page.
- Final extraction quality remains dependent on the live TUI page shape and must
  be recorded after manual verification rather than inferred from unit tests.
- Visible web-component content is not necessarily reachable through document
  selectors. TUI's offer card uses an open shadow root, so the fixed boundary
  must explicitly include that demonstrated component without becoming a broad
  recursive DOM crawler.
