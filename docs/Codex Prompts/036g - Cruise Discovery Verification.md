# Codex Prompt 036g – Cruise Discovery Verification

## Implementation Prompt

Implement **Step 7 only** from:

```text
docs/AI Playbook/036 - Cruise Discovery and Capture.md
```

Prompts 036a–036f must already be complete.

If the 036f deterministic test suite is absent or failing, stop and report the
prerequisite rather than recreating it here.

Do not begin Prompt 037.

---

## Required Reading

Read these files in order:

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/036 - Cruise Discovery and Capture.md`
5. Prompts 036a–036f, including Results and Lessons Learned
6. all production files introduced or updated by Prompt 036
7. the complete Prompt 036 test suite
8. the latest Session Handover

Treat the implemented code and passing tests as evidence. Do not redesign the
capability during verification.

---

## Goal

Verify Cruise Discovery and Capture end to end and close Prompt 036.

Demonstrate that:

- TUI loads only after Robin explicitly selects a trusted source
- interactive browsing remains isolated to Avalonia's native-browser boundary
- trusted-host navigation is exact, HTTPS-only and controlled
- capture occurs only after an explicit action
- the fixed TUI script is bounded, read-only and source-specific
- the demonstrated `tui-product-cards` open shadow root is supported narrowly
- Application owns the provider-independent capture contract
- Infrastructure owns TUI payload validation and mapping
- Core contains only provider-independent Cruise data
- operator and retail source remain distinct
- successful capture produces a review-only `CruiseObservation`
- incomplete, ambiguous, unsupported, cancelled and failed results remain honest
- cancellation and navigation reject stale capture results
- external opening is explicit and trusted-address validated
- shell resolution and ordinary startup remain offline-safe
- no Cruise persistence, ratings, comparison, monitoring or booking behavior was
  introduced
- the complete solution builds and all automated tests pass
- the manual TUI workflow is recorded
- the Playbook, Roadmap and Session Handover accurately describe the completed
  capability and next step

This is a verification and documentation task, not a feature-development task.

---

## Existing Manual Evidence

Robin successfully performed the live workflow on **16 July 2026** after the
fixed script was updated to inspect TUI's open `tui-product-cards` shadow root.

The demonstrated page was:

```text
https://www.tui.co.uk/cruise/deals/marella-cruise-of-the-week
```

The verified workflow was:

```text
Open Cruise of the Week
        ↓
Accept TUI cookies manually
        ↓
View the Canarian Flavours offer
        ↓
Click Capture Displayed Cruise
        ↓
Kryten identifies one deduplicated offer
        ↓
Kryten displays the captured review successfully
```

The page exposed its offer card inside an open `tui-product-cards` shadow root
and repeated the detailed itinerary link twice. The fixed reader entered only
that demonstrated component, deduplicated the identical links and captured the
visible Cruise data.

This recorded evidence may satisfy the 036g manual gate if verification confirms
that the relevant production files have not changed since Robin's successful
check. If capture or browser-boundary production code has changed, ask Robin to
repeat the live workflow and record the new result.

Never copy cookies, tracking identifiers, the opaque live package identifier or
personal browsing data into documentation.

---

## Expected Architecture

Verify this dependency direction:

```text
KrytenAssist.Core
        ↑
KrytenAssist.Application
        ↑
KrytenAssist.Infrastructure

KrytenAssist.Avalonia → Core/Application
KrytenAssist.Avalonia → Infrastructure at composition only
```

Verify this live capture path:

```text
Robin clicks Capture Displayed Cruise
        ↓
CruiseBrowserFeasibilityView
        ↓ fixed read-only TUI script
NativeWebView current trusted page
        ↓ bounded version-1 JSON
CruiseBrowserFeasibilityViewModel
        ↓ CruisePageCaptureRequest
ICruisePageCaptureService
        ↓
TuiCruisePageCaptureService
        ↓
CruiseCaptureResult
        ↓
session-only CruiseObservation review
```

Confirm no browser, DOM, JavaScript, Avalonia, HTML or TUI payload type enters
Core or the Application contract.

---

## Allowed Changes

The expected outcome is no production or test-code change.

Verification may update:

```text
docs/AI Playbook/036 - Cruise Discovery and Capture.md
docs/Roadmap.md
docs/Codex Prompts/036g - Cruise Discovery Verification.md
docs/Session Handovers/<next session handover>.md
```

Create a Session Handover because Prompt 036 closes a roadmap capability and
Prompt 037 begins persistence and price history.

Production or test files may be modified only if verification exposes a genuine
Prompt 036 defect. Any correction must:

- be minimal
- fix only the verified defect
- preserve existing contracts and architecture
- include a focused deterministic regression test
- rerun focused and complete test suites
- be reported explicitly
- trigger a repeat manual check if it changes browser extraction or capture
  presentation behavior

Do not stage, commit, push, discard or overwrite Robin's work.

---

## Verification Process

### 1. Record the Initial Working Tree

Run:

```text
git status --short
```

Record all staged, unstaged and untracked files. Preserve them. Do not use
destructive Git commands.

### 2. Verify Prompt Prerequisites

Confirm:

- 036a records a `Go` feasibility decision
- 036b records successful trusted interactive navigation
- 036c defines the capture contract and controlled results
- 036d contains the isolated TUI adapter and offline fixtures
- 036e records the successful live capture and review
- 036f Results and Lessons Learned are complete
- 036f focused and complete suites passed

Report any missing or contradictory result before continuing.

### 3. Verify Project and Dependency Boundaries

Inspect project references and source placement.

Confirm:

- Core has no Avalonia, browser, HTML, TUI, HTTP or provider SDK dependency
- Application references Core but not Infrastructure or Avalonia
- Infrastructure references Application/Core and owns TUI mapping
- Avalonia aliases Application and Infrastructure references
- Infrastructure is used from Avalonia only at composition or existing provider
  boundaries
- the test project preserves alias isolation
- no package was added solely for browser extraction or test execution
- no new project was introduced

Search for `NativeWebView`, `WebView`, `shadowRoot`, `document.`, TUI payload
schema names and provider selectors outside their intended Avalonia presentation
or Infrastructure adapter locations. Report concrete file evidence.

### 4. Verify Cruise Domain and Capture Contract

Inspect:

```text
KrytenAssist.Core/Cruises/
KrytenAssist.Application/Cruises/CruisePageCaptureRequest.cs
KrytenAssist.Application/Cruises/CruiseCaptureResult.cs
KrytenAssist.Application/Cruises/CruiseCaptureStatus.cs
KrytenAssist.Application/Cruises/ICruisePageCaptureService.cs
```

Confirm:

- Cruise models remain immutable and provider independent
- `CruiseProvider` represents the operator
- `CruiseSource` represents the advertising or retail source
- `CruiseObservation` retains exact observation time and source reference
- the request accepts only bounded nonblank payload and absolute HTTPS reference
- the capture interface accepts cancellation
- controlled outcomes cover Success, Incomplete, Ambiguous, Unsupported, Failed
  and Cancelled
- results cannot claim success without a valid observation
- browser and source-specific payload types do not leak inward

Use the 036c contract tests as executable evidence.

### 5. Verify Trusted Source Navigation

Inspect:

```text
KrytenAssist.Avalonia/Cruises/Discovery/CruiseDiscoverySource.cs
KrytenAssist.Avalonia/Cruises/Discovery/CruiseDiscoverySourceCatalog.cs
KrytenAssist.Avalonia/Cruises/Discovery/CruiseTrustedHostPolicy.cs
KrytenAssist.Avalonia/ViewModels/CruiseBrowserFeasibilityViewModel.cs
```

Confirm:

- source definitions are data-driven and do not use a shared enum
- nothing loads during construction
- navigation starts only from an explicit source command
- only HTTPS and the exact configured host are trusted
- deceptive suffixes, user-info URLs and unrelated hosts are rejected
- `about:blank` is treated as internal preparation, never a trusted source
- untrusted navigation stops usable presentation without displaying the address
- Back, Forward, Stop, Refresh and Close maintain honest state
- navigation history and diagnostic cruise links are bounded
- Close clears the browser session and captured review

Use the 036b and 036f tests as evidence. Do not launch the application merely to
re-prove deterministic state.

### 6. Verify Native Browser Isolation

Inspect:

```text
KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml
KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml.cs
KrytenAssist.Avalonia/Cruises/Discovery/TuiCruiseCaptureScript.cs
```

Confirm:

- `NativeWebView` remains a presentation-platform detail
- code-behind only hosts browser operations and forwards lifecycle events
- the ViewModel never supplies arbitrary JavaScript
- capture uses one fixed application-owned TUI script
- the script does not click, submit, focus, scroll or alter the DOM
- the script does not read cookies, local storage, session storage or credentials
- it does not return HTML or unbounded body text
- strings, candidates, semantic scans and supported shadow roots are bounded
- only the demonstrated open `tui-product-cards` roots are entered
- no recursive arbitrary shadow-root traversal exists
- blank, invalid and over-limit bridge results fail safely
- navigation changes during script execution cause the result to be ignored

Do not broaden extraction during verification.

### 7. Verify TUI Adapter Isolation and Accuracy

Inspect:

```text
KrytenAssist.Infrastructure/Cruises/Tui/
KrytenAssist.Avalonia.Tests/Cruises/Tui/
```

Confirm:

- the adapter accepts only the supported source identifier, TUI retail source,
  exact HTTPS host and payload version
- payload deserialization is bounded by the Application request
- zero candidates are Incomplete
- multiple candidates are Ambiguous
- missing title, ship, departure, duration or price remains Incomplete
- valid values map into provider-independent Cruise models
- price amounts, currencies and bases retain their source meaning
- the operator is Marella Cruises while the retail source is TUI
- optional data remains optional
- no live network, browser, filesystem or system clock is used
- cancellation is honored
- no opaque package identifier appears in logs or documentation

Use only fictional offline fixtures and existing adapter tests.

### 8. Verify Capture and Review Lifecycle

Inspect the ViewModel, View and 036f lifecycle tests.

Confirm:

- Capture is unavailable before a trusted page is ready
- explicit Capture raises one bridge request
- duplicate capture is disabled while active
- the exact trusted address, source and clock time enter the request
- every controlled result has an honest presentation
- successful observations display all available fields
- optional missing fields are not invented
- the review is explicitly non-persistent
- Cancel, navigation, Refresh and Close reject late results
- a result from an earlier generation cannot overwrite current state
- genuine navigation clears an earlier review
- same-page duplicate events do not destroy valid ready state
- no acceptance, save or history operation exists

### 9. Verify Explicit External Opening

Confirm:

- Open at TUI is unavailable without a current trusted address
- the ViewModel raises only the exact validated address
- the View owns the platform launcher
- launcher failure is controlled
- no automatic opening occurs
- opening externally does not log in, submit, add to basket or book

Do not invoke the operating-system launcher during automated verification.

### 10. Verify Dependency Injection and Offline Startup

Inspect:

```text
KrytenAssist.Avalonia/DependencyInjection/ShellServiceCollectionExtensions.cs
KrytenAssist.Infrastructure/Cruises/Tui/TuiCruiseCaptureServiceCollectionExtensions.cs
KrytenAssist.Avalonia/Program.cs
```

Confirm:

- the TUI capture adapter is composed through an extension method
- shell/ViewModel resolution succeeds with the application clock
- source catalogue and trusted-host policy lifetimes are intentional
- resolving the shell does not navigate, capture, execute a Skill or read the
  clock
- no `HttpClient` is required by the explicit capture path
- the older direct Marella provider remains isolated and is not disguised to
  bypass TUI's protection
- ordinary application startup remains offline-safe

### 11. Verify Scope Exclusions

Search for Prompt 036 additions involving:

- persistent Cruise repositories or database tables
- observation history
- price trends
- saved cruises
- ratings or notes
- preferences
- alerts, schedules or background jobs
- retailer comparison
- automated booking or payment
- authentication or cookie storage
- AI-generated extraction

Confirm none were introduced. Do not implement them. Prompt 037 owns local
history and price tracking.

---

## Automated Verification

Run focused tests:

```text
dotnet test KrytenAssist.Avalonia.Tests/KrytenAssist.Avalonia.Tests.csproj --no-restore
```

Build the complete solution:

```text
dotnet build KrytenAssist.sln --no-restore
```

Run the complete suite:

```text
dotnet test KrytenAssist.sln --no-build --no-restore
```

Report exact totals, failures, skipped tests, errors and warnings.

No automated verification may contact TUI, launch a browser, invoke the OS
launcher or use the system clock for capture observations.

---

## Manual Gate

Use one:

```text
Passed on 16 July 2026 – Robin opened the embedded TUI Cruise of the Week page,
accepted cookies manually, viewed Canarian Flavours, selected Capture Displayed
Cruise and received the successful session-only review from the demonstrated
tui-product-cards shadow-root page shape.
```

```text
Awaiting Robin's repeat manual live TUI verification because capture or browser
production code changed after the recorded 036e check.
```

```text
Partial on <date> – <honest limitation and next action>.
```

Do not claim a new manual check that Robin did not perform.

---

## Documentation Updates

### AI Playbook 036

Complete:

```text
docs/AI Playbook/036 - Cruise Discovery and Capture.md
```

Update:

- Status
- Feasibility Outcome
- Files Created
- Files Updated
- Build
- Tests
- Manual Verification
- Git Commits using only commits that actually exist
- Lessons Learned

Record the final TUI shadow-root page shape and the controlled browser boundary.
Do not include the opaque live package identifier.

### Roadmap

Update Prompt 036 in:

```text
docs/Roadmap.md
```

Mark it complete and concisely record:

- trusted embedded browsing
- explicit capture and review
- TUI open shadow-root support
- provider-independent observation mapping
- offline-safe deterministic verification
- the next step: Prompt 037 Cruise History and Price Tracking

Do not mark Prompt 037 started.

### Session Handover

Create the next sequential file under:

```text
docs/Session Handovers/
```

Follow the established handover format. Record:

- Prompt 036 completion
- current architecture and user workflow
- successful manual TUI evidence
- build and test totals
- existing warnings
- important source-specific limitation: the supported TUI page shape may change
- no persistence exists yet
- Prompt 037 is the recommended next task
- current working-tree state and actual commits only

Do not overwrite an existing handover or invent a commit hash.

---

## Definition of Done

Prompt 036g is complete when:

- all 036a–036f prerequisites are verified
- architecture and dependency direction are verified
- browser isolation and trusted-host behavior are verified
- no automatic external work is verified
- capture accuracy and controlled failures are verified
- cancellation and stale-result behavior are verified
- scope exclusions are verified
- the focused suite passes
- the complete solution builds
- the complete regression suite passes
- manual evidence is accurately recorded
- Playbook 036 Results and Lessons Learned are complete
- Roadmap Prompt 036 is marked complete
- the next Session Handover is created
- this prompt's Results and Lessons Learned are complete

Do not implement Prompt 037.

Stop after Prompt 036g.

---

## Completion Report

Provide:

### Summary

State whether Prompt 036 is complete.

### Architecture Verification

Report concrete evidence for dependency direction, browser isolation, TUI
adapter isolation and operator/source distinction.

### Security and External Work

Report trusted-host enforcement, fixed-script constraints, explicit external
actions and offline startup/test evidence.

### Capture Verification

Report successful and controlled non-success paths, cancellation and review-only
state.

### Files Modified

List every file changed by verification.

### Production Corrections

Use `None` or report every verified correction and regression test.

### Build and Tests

Report exact commands and totals.

### Manual Verification

Record only evidence Robin actually performed.

### Documentation

Report Playbook, Roadmap and handover updates.

### Scope Check

Confirm Prompt 037 and later features were not implemented.

---

## Results

> Complete during verification.

### Status

Complete. Prompt 036 is verified and closed.

### Prerequisites

Prompts 036a–036f are complete. Their Results and Lessons Learned are populated,
036a records a `Go` decision, 036e records Robin's successful live TUI workflow,
and 036f records 350 passing deterministic tests. Commits exist for every step
through 036f.

### Architecture Verification

Verified. Core remains dependency-free and provider independent; Application
references Core and owns the capture contract; Infrastructure references inward
and privately owns TUI payload validation/mapping; Avalonia owns browser and
presentation concerns and reaches Infrastructure through composition aliases.

Searches found browser/DOM code only in the Avalonia Cruise browser View and
fixed script, plus unrelated HTML parsing inside the existing Infrastructure
Marella parser. TUI candidate payload types are private nested Infrastructure
types and do not leak inward.

### Browser and Security Boundary

Verified. Source selection is explicit, exact HTTPS host comparison rejects
deceptive and unrelated hosts, and `about:blank` is browser-internal only. The
fixed script accepts no input, bounds strings to 512 characters, candidates to
10, supported `tui-product-cards` roots to 3 and semantic scans to finite limits.
It does not click, submit, focus, scroll, mutate the DOM, read cookies/storage or
return page HTML. External opening is explicit and exact-address validated.

### Capture and Review Verification

Verified. The Application request retains exact source, retail source, HTTPS
reference, fixed observation time and bounded payload. The TUI adapter maps one
valid candidate into provider-independent Cruise models and returns controlled
Incomplete, Ambiguous, Unsupported, Failed or Cancelled results otherwise.
Marella Cruises is the operator and TUI is the retail source. The successful
review is session-only; cancellation generations reject stale results after
Cancel, navigation, Refresh or Close.

### Scope Verification

Verified. Prompt 036 introduced no persistence, Cruise history, price trends,
ratings, notes, preferences, comparison, monitoring, scheduling, authentication,
cookie storage, automated booking or payment behavior. Prompt 037 was not
started.

### Production Corrections

None. Verification exposed no production or test defect.

### Files Created

- `docs/Codex Prompts/036g - Cruise Discovery Verification.md`
- `docs/Session Handovers/2026-07-16 Session 017.md`

### Files Updated

- `docs/AI Playbook/036 - Cruise Discovery and Capture.md`
- `docs/Roadmap.md`

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

270 passed, 0 failed, 0 skipped.

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

Verified. Automated checks use fictional fixtures, hand-written services,
controlled tasks and fixed clocks. No test performs DNS/HTTP work, contacts TUI,
launches a browser or invokes the operating-system launcher. Shell resolution
performs no navigation, capture, Skill execution or clock read.

### Manual Verification

Passed on 16 July 2026. Robin opened the embedded TUI Cruise of the Week page,
accepted cookies manually, viewed Canarian Flavours and selected
`Capture Displayed Cruise`. Kryten read the demonstrated open
`tui-product-cards` shadow root, deduplicated the repeated itinerary link and
displayed the successful session-only review. Relevant production code did not
change during 036f or 036g, so a repeat was not required.

### Documentation Updates

Completed Playbook 036 Results and Lessons Learned, marked Roadmap Prompt 036
complete with Prompt 037 identified as next, and created Session Handover 017.

### Notes

Commits through 036f are recorded exactly. The 036g documentation remains
uncommitted for Robin to review. Prompt 037 has not started.

---

## Lessons Learned

> Complete after verification.

- Browser-assisted capture provides a compliant path around unattended HTTP
  blocking without disguising a background client or automating user actions.
- Architecture verification is clearest when source searches and project
  references corroborate the behavioral tests: browser code stays in Avalonia,
  TUI mapping stays in Infrastructure and shared models remain clean.
- The open shadow-root behavior is a source-specific compatibility fact, not a
  domain concern; documenting that limitation makes future TUI changes easier to
  diagnose honestly.
- Existing successful manual evidence remains valid when later steps add only
  deterministic tests and documentation. A new live check is necessary whenever
  extraction or browser presentation code changes.
- Prompt 036's review-only boundary gives Prompt 037 a clear starting point:
  explicit acceptance and local observation history can be added without
  redesigning discovery or capture.
