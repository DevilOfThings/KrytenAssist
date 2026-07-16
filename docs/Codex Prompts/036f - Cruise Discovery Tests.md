# Codex Prompt 036f – Cruise Discovery Tests

## Implementation Prompt

Implement **Step 6 only** from:

```text
docs/AI Playbook/036 - Cruise Discovery and Capture.md
```

Prompts 036a–036e are implemented, and Robin has manually verified that the
embedded TUI Cruise of the Week landing page can be captured successfully.

Do not implement Step 7 verification and do not begin Prompt 037.

---

## Required Reading

Read these files in order before changing tests:

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/036 - Cruise Discovery and Capture.md`
5. Prompts 036a–036e, including Results and Lessons Learned
6. all existing Cruise Discovery, capture-contract and TUI capture tests
7. the production types listed below

Understand the trusted-browser boundary, explicit capture lifecycle, open shadow
root support and existing deterministic coverage before adding tests.

---

## Goal

Complete the deterministic automated test coverage for Cruise Discovery and
Capture.

Prove that:

- startup and ordinary shell resolution perform no external work
- source navigation occurs only after an explicit user command
- only trusted HTTPS TUI addresses remain usable
- browser-internal and untrusted addresses are handled safely
- capture occurs only after an explicit command
- duplicate capture is prevented while capture is active
- the fixed TUI extraction boundary remains bounded and read-only
- the demonstrated `tui-product-cards` open shadow root is supported narrowly
- capture requests retain the exact trusted address, source and clock time
- all controlled capture results are presented honestly
- cancellation and navigation prevent stale results from appearing
- successful observations are displayed for review without persistence
- external opening is explicit and trusted-host validated
- Close clears browser and review state
- dependency-injection composition resolves offline
- the entire regression suite remains green

This is a test-completion step. Add only minimal production corrections when a
new deterministic regression test proves a genuine Prompt 036 defect.

---

## Scope

This prompt owns:

- missing Cruise Discovery ViewModel tests
- capture lifecycle and review-state tests
- trusted navigation and external-opening tests
- fixed TUI script-boundary contract tests
- capture adapter composition tests where coverage is missing
- cancellation and stale-result tests
- offline/no-external-work assertions
- focused and complete regression test execution
- updating this prompt's Results section

This prompt does **not** own:

- browser or Avalonia UI automation
- a live TUI request
- repeating the manual TUI workflow
- persistent Cruise history
- saving or accepting a captured cruise
- price trend calculations
- ratings, notes or preferences
- source comparison
- background capture or monitoring
- booking automation
- final architecture verification
- Playbook, Roadmap or Session Handover updates

Those documentation and final verification tasks belong to 036g. Persistence and
price history belong to Prompt 037.

---

## Allowed Changes

Prefer changes only within:

```text
KrytenAssist.Avalonia.Tests/
```

Expected locations include:

```text
KrytenAssist.Avalonia.Tests/Cruises/Discovery/
KrytenAssist.Avalonia.Tests/Cruises/Tui/
KrytenAssist.Avalonia.Tests/ViewModels/
KrytenAssist.Avalonia.Tests/DependencyInjection/
```

Update this file when implementation is complete:

```text
docs/Codex Prompts/036f - Cruise Discovery Tests.md
```

Do not add a new test project or NuGet package.

Production files may be modified only when a required deterministic test exposes
a genuine defect. Each correction must be:

- minimal
- limited to Prompt 036 behavior
- covered by a focused regression test
- reported explicitly in Results

Do not stage, commit, push, discard or overwrite Robin's existing work.

---

## Production Types Under Test

Exercise the public behavior of the existing implementation directly.

### Discovery and Trust

```text
KrytenAssist.Avalonia/Cruises/Discovery/CruiseDiscoverySource.cs
KrytenAssist.Avalonia/Cruises/Discovery/CruiseDiscoverySourceCatalog.cs
KrytenAssist.Avalonia/Cruises/Discovery/CruiseTrustedHostPolicy.cs
KrytenAssist.Avalonia/Cruises/Discovery/TuiCruiseCaptureScript.cs
```

### Browser and Review State

```text
KrytenAssist.Avalonia/ViewModels/CruiseBrowserFeasibilityViewModel.cs
KrytenAssist.Avalonia/ViewModels/CruiseOfTheWeekViewModel.cs
KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml.cs
```

Do not instantiate `NativeWebView` or test code-behind through reflection. Test
the browser bridge indirectly through its ViewModel events and public result
methods. The fixed-script contract may be tested as an application-owned string.

### Capture Contract and Adapter

```text
KrytenAssist.Application/Cruises/CruisePageCaptureRequest.cs
KrytenAssist.Application/Cruises/CruiseCaptureResult.cs
KrytenAssist.Application/Cruises/CruiseCaptureStatus.cs
KrytenAssist.Application/Cruises/ICruisePageCaptureService.cs
KrytenAssist.Infrastructure/Cruises/Tui/TuiCruisePageCaptureService.cs
KrytenAssist.Infrastructure/Cruises/Tui/TuiCruiseCaptureServiceCollectionExtensions.cs
```

The 036c and 036d tests already cover these contracts extensively. Add only
missing integration or regression coverage; do not duplicate their complete
matrices.

### Composition

```text
KrytenAssist.Avalonia/DependencyInjection/ShellServiceCollectionExtensions.cs
KrytenAssist.Avalonia/ViewModels/ShellViewModel.cs
```

---

## Existing Tests to Preserve and Extend

Review and preserve:

```text
KrytenAssist.Avalonia.Tests/Application/Cruises/CruisePageCaptureRequestTests.cs
KrytenAssist.Avalonia.Tests/Application/Cruises/CruiseCaptureResultTests.cs
KrytenAssist.Avalonia.Tests/Application/Cruises/CruisePageCaptureServiceContractTests.cs
KrytenAssist.Avalonia.Tests/Cruises/Discovery/CruiseDiscoverySourceTests.cs
KrytenAssist.Avalonia.Tests/Cruises/Discovery/CruiseTrustedHostPolicyTests.cs
KrytenAssist.Avalonia.Tests/Cruises/Tui/TuiCruisePageCaptureServiceTests.cs
KrytenAssist.Avalonia.Tests/Cruises/Tui/TuiCruiseCaptureDependencyInjectionTests.cs
KrytenAssist.Avalonia.Tests/ViewModels/CruiseBrowserFeasibilityViewModelTests.cs
KrytenAssist.Avalonia.Tests/ViewModels/CruiseCaptureReviewViewModelTests.cs
KrytenAssist.Avalonia.Tests/DependencyInjection/ShellDependencyInjectionTests.cs
```

Prefer extending these focused files or splitting the capture lifecycle into a
clearly named file when that improves readability.

Do not replace useful existing assertions merely to reorganise tests.

---

## Test Conventions

Use:

- xUnit
- Arrange, Act and Assert
- descriptive test names
- public commands, properties, events and methods
- small hand-written fakes
- fixed timestamps with a non-zero offset
- exact trusted and untrusted addresses
- deterministic `TaskCompletionSource` coordination for in-flight work
- isolated `ServiceCollection` instances
- exact assertions for observations and request values

Do not use:

- mocking libraries
- reflection
- live HTTP or DNS
- browser automation
- `NativeWebView`
- JavaScript engines
- system clock values
- sleeps, polling or retries
- test ordering
- shared mutable state
- cookies, credentials or personal browsing data
- snapshot or approval tests
- full copied TUI HTML

All tests must run offline.

---

## Deterministic Capture Fixtures

Use fictional capture data for adapter and ViewModel tests. Do not place Robin's
live TUI package identifier in test source, output or documentation.

Representative values may include:

```text
Source identifier: marella-cruise-of-the-week
Retail source: TUI
Offer id: fictional-offer-101
Title: Atlantic Discovery
Ship: Marella Example
Departure: 18 December 2026
Duration: 7 nights
Per-person price: GBP 988
Total price: GBP 1,975, total based on 2 sharing
Promotion: GBP 380 per person discount
Source reference: https://www.tui.co.uk/cruise/bookitineraries/fictional-101
Observed at: 16 July 2026 10:30 +01:00
```

Use a hand-written `ICruisePageCaptureService` fake that can:

- record the exact request and cancellation token
- return a supplied controlled result
- throw a deterministic exception
- remain incomplete until a test-controlled task is completed
- observe cancellation without sleeping

Use a fixed `IClock` and assert the exact timestamp passed through the request.

---

## Discovery and Navigation Coverage

Retain existing source-model and policy coverage. Add missing tests for these
observable behaviors:

- constructing the browser ViewModel performs no navigation
- the source catalogue exposes only configured trusted sources
- selecting a source raises exactly one load request with its exact address
- selecting the same active source again does not reload it
- Back and Forward raise only when their reported capability permits
- Refresh and Stop maintain honest ready/navigating state
- Close clears source, address, navigation history, diagnostics and review state
- genuine navigation clears an earlier capture review
- a duplicate navigation-start event for the same ready page does not clear the
  review or restart loading
- `about:blank` is treated only as browser-internal preparation
- HTTP, deceptive subdomains, user-info URLs and unrelated HTTPS hosts are
  rejected according to the existing policy
- an untrusted address stops usable browser presentation and raises the existing
  untrusted-address signal once
- diagnostic history and cruise-link collections retain their documented bounds

Do not assert private fields or reproduce the policy implementation in tests.

---

## Explicit Capture Lifecycle Coverage

Complete the ViewModel capture matrix.

Cover:

- Capture is unavailable before a source is selected and the page is ready
- Capture becomes available only for a ready trusted page with an injected
  service and clock
- executing Capture raises exactly one payload request
- Capture becomes unavailable while a request is active
- Cancel becomes available only while capture is active
- a second Capture command cannot start duplicate work
- blank, invalid bridge output is reported through the controlled bridge failure
- an over-limit payload is rejected before invoking the service
- an untrusted source reference is rejected before invoking the service
- the service receives the exact source identifier, retail source, address,
  fixed observation time and payload
- Success displays the complete observation and every relevant formatted review
  value
- Incomplete displays its message and exact missing-field names
- Ambiguous, Unsupported and Failed preserve their controlled messages
- Cancelled is presented according to the existing capture-result contract
- an unexpected service exception becomes the safe failed state
- Cancel cancels the active token, clears transient result state and ignores a
  later completion
- navigation, Refresh and Close cancel in-flight capture and ignore stale results
- completion from an earlier generation cannot overwrite a newer capture
- result state never persists beyond the ViewModel session

Do not force every controlled result through a `[Theory]` if focused tests make
state and intent clearer.

---

## Review Presentation Coverage

Construct a complete provider-independent `CruiseObservation` and verify the
public review properties exactly.

Cover:

- title
- operator/provider name
- retail source name
- ship
- departure date formatting
- duration formatting
- optional departure port
- optional itinerary
- one and multiple prices with preserved currency and basis
- promotion
- exact source reference
- observation timestamp and offset
- optional-value visibility flags
- `HasCapturedObservation`
- the explicit review-only/no-save lifecycle

Also cover a successful observation with absent optional values. Blank optional
values must not appear as invented content.

Do not add persistence merely to test that persistence is absent.

---

## Fixed TUI Script Boundary Coverage

The fixed script is production-owned JavaScript hosted as a constant. Do not run
it in a browser or third-party JavaScript engine during this step.

Assert stable security and bounding invariants without coupling tests to every
selector or whitespace character.

Cover that the script:

- is fixed and accepts no script input
- returns the version-1 `{ version, candidates }` shape
- bounds candidates to 10
- bounds each extracted string to 512 characters
- uses detailed itinerary identity rather than arbitrary links
- supports relative URL resolution
- deduplicates repeated detailed itinerary links
- explicitly supports only the demonstrated `tui-product-cards` open shadow root
- bounds the number of those roots
- does not recursively traverse arbitrary shadow roots
- retains the confirmed `150013` to `Marella Discovery 2` fallback
- preserves per-person price, total-price basis and promotion as separate data
- does not read `document.cookie`, local storage or session storage
- does not click, submit, focus, scroll or mutate the DOM
- does not return `innerHTML`, `outerHTML` or full body text

Prefer assertion groups around meaningful invariants. Avoid a brittle assertion
for the complete script string.

---

## External Opening Coverage

Cover ViewModel behavior without launching an operating-system browser:

- Open at TUI is unavailable before a trusted current address exists
- it becomes available for the exact trusted TUI host
- executing it raises one event containing the exact current address
- it is disabled for browser-internal, malformed and untrusted addresses
- no event is raised when the command cannot execute
- a launcher failure maps to the existing controlled error message
- external opening does not navigate the embedded browser or alter the capture
  observation

Do not invoke Avalonia's platform launcher in tests.

---

## Dependency Injection and Offline Safety

Complete composition coverage for `AddShell()` and the 036d adapter.

Prove that:

- `AddShell()` retains its null guard and fluent return value
- `ICruisePageCaptureService` resolves to the TUI adapter
- `CruiseOfTheWeekViewModel` receives a capture-capable browser ViewModel
- transient shell/ViewModel lifetimes remain intentional
- singleton catalogue and policy lifetimes remain intentional
- resolving the shell performs no navigation, capture or Skill execution
- resolving the shell does not read the clock
- no fake production service is introduced
- no `HttpClient` or live TUI dependency is required by the capture workflow

Use service descriptors, hand-written fakes and counters. Do not infer
no-network behavior merely from a quick test duration.

---

## Production Corrections

If a deterministic test exposes a defect:

1. write the failing regression test
2. confirm it fails for the intended reason
3. make the smallest production correction
4. rerun the focused test
5. run the complete suite
6. record the correction in Results

Do not redesign the browser boundary, capture contract, Cruise models or DI
architecture during this step.

Potential future improvements belong in Notes, not production code.

---

## Required Commands

Run the focused suite:

```text
dotnet test KrytenAssist.Avalonia.Tests/KrytenAssist.Avalonia.Tests.csproj --no-restore
```

Build the complete solution:

```text
dotnet build KrytenAssist.sln --no-restore
```

Run the complete regression suite:

```text
dotnet test KrytenAssist.sln --no-build --no-restore
```

Report exact totals, failures, skipped tests, errors and warnings.

If this environment requires local test-runner socket permission, request it;
do not replace execution with an assumption.

---

## Definition of Done

Prompt 036f is complete when:

- missing deterministic coverage described above has been added
- existing 036a–036e tests remain intact
- all new tests run without live browser or network access
- capture lifecycle, cancellation and stale-result protection are covered
- trusted navigation and external-open event behavior are covered
- successful and controlled non-success reviews are covered
- the fixed TUI script security/bounding invariants are covered
- the shadow-root regression has a focused test
- shell and capture DI resolve offline
- any production correction is minimal and documented
- the complete solution builds
- focused and complete test suites pass
- this prompt's Results and Lessons Learned are completed

Do not perform the 036g manual or architecture verification in this prompt.

Stop after Prompt 036f.

---

## Completion Report

When implementation finishes, provide:

### Summary

Describe the added deterministic coverage and any production correction.

### Coverage

Report navigation, trust, capture lifecycle, review, script boundary, external
opening, cancellation, stale-result and DI coverage.

### Files Modified

List every created and updated file.

### Production Corrections

Use one:

```text
None.
```

or list each defect, regression test and minimal correction.

### Build and Tests

Report exact commands and totals.

### Network Check

Confirm how the suite proves no browser or external network work occurred.

### Scope Check

Confirm no persistence, history, rating, comparison, monitoring, booking or
Prompt 036g work was added.

---

## Results

> Complete during implementation.

### Status

Complete.

### Coverage Added

Added deterministic coverage for:

- capture availability and explicit bridge requests
- duplicate capture prevention
- exact source, retail source, trusted address, payload and fixed timestamp
- complete and optional-value review presentation
- Incomplete, Ambiguous, Unsupported, Failed and Cancelled outcomes
- blank, oversized and untrusted bridge inputs
- unexpected adapter exceptions
- cancellation and late completion
- navigation, Refresh and Close stale-result protection
- exact external-open requests and controlled launcher failure
- bounded navigation history and trusted cruise-link diagnostics
- fixed-script candidate, field, root and semantic-text bounds
- detailed-link deduplication and relative URL handling
- the demonstrated `tui-product-cards` open shadow-root regression
- prohibited browser storage, DOM mutation and full-page extraction
- offline TUI adapter composition through `AddShell()`

### Files Created

- `KrytenAssist.Avalonia.Tests/ViewModels/CruiseCaptureLifecycleViewModelTests.cs`
- `KrytenAssist.Avalonia.Tests/DependencyInjection/CruiseDiscoveryDependencyInjectionTests.cs`

### Files Updated

- `KrytenAssist.Avalonia.Tests/ViewModels/CruiseBrowserFeasibilityViewModelTests.cs`
- `KrytenAssist.Avalonia.Tests/ViewModels/CruiseCaptureReviewViewModelTests.cs`
- `docs/Codex Prompts/036f - Cruise Discovery Tests.md`

### Production Corrections

None. The deterministic tests did not expose a production defect.

### Build

Passed:

```text
dotnet build KrytenAssist.sln --no-restore
```

0 errors. 5 existing NU1903 warnings for the known
`SQLitePCLRaw.lib.e_sqlite3` package advisory.

### Focused Tests

Passed:

```text
dotnet test KrytenAssist.Avalonia.Tests/KrytenAssist.Avalonia.Tests.csproj --no-restore
```

270 passed, 0 failed, 0 skipped. This adds 23 Avalonia tests to the previous
247-test baseline.

### Complete Regression Suite

Passed:

```text
dotnet test KrytenAssist.sln --no-build --no-restore
```

350 passed, 0 failed, 0 skipped:

- Core: 71
- Avalonia: 270
- API: 9

### Network Check

Verified by construction. Capture services, clocks and in-flight operations use
hand-written fakes and controlled tasks. Script tests inspect the fixed string
without a JavaScript engine or browser. DI resolution does not register or use
`HttpClient`. No test performs DNS, HTTP, browser or OS-launcher work.

### Scope Check

No persistence, history storage, rating, comparison, monitoring, booking,
Playbook verification, Roadmap update or Prompt 037 behavior was added.

### Notes

The successful manual TUI workflow remains recorded in 036e. It was not repeated
because 036f is the deterministic automated-test step; final verification belongs
to 036g.

---

## Lessons Learned

> Complete after implementation.

- The most valuable presentation tests assert the exact provider-independent
  request and review values rather than duplicating source-specific parsing.
- A controllable incomplete task proves cancellation and stale-result behavior
  without sleeps, polling or timing assumptions.
- The TUI shadow-root regression can be protected offline through narrow script
  boundary assertions without introducing browser automation into the suite.
- No-network behavior is strongest when demonstrated structurally: hand-written
  services, fixed clocks, no launcher calls and no `HttpClient` dependency.
- Existing 036a–036d tests already covered source models, trust policy, capture
  contracts and adapter parsing thoroughly; 036f added lifecycle integration
  coverage instead of duplicating those matrices.
