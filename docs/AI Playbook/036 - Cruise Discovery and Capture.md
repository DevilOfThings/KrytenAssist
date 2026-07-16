# Prompt 036 – Cruise Discovery and Capture

## Goal

Create the first usable cruise-research workflow in Kryten Assist.

Prompt 036 should allow Robin to browse trusted cruise offer pages from within
the desktop application, beginning with TUI's Marella Cruise of the Week, and
explicitly capture an interesting sailing as provider-independent Cruise data.

The workflow should make it easy to:

- choose a trusted cruise source
- browse that source without leaving Kryten
- inspect offers interactively
- capture the currently displayed cruise through an explicit action
- review the captured details
- continue to the source company when further investigation or booking is wanted

This prompt establishes discovery and capture only.

Persistence, historical price calculations, ratings, preferences, alerts,
scheduled monitoring and booking automation belong to later roadmap prompts.

---

## Why This Prompt Exists

Prompts 032–035 established:

- the Skills framework
- provider-independent Cruise models
- the Cruise of the Week Skill
- a Marella HTML parser and HTTP provider
- the dashboard and navigation shell
- a focused Cruise of the Week view

The application architecture and presentation workflow are complete, but the
original live data route is not operational. TUI's website protection rejects
the application's direct `HttpClient` request with HTTP 403 even though the page
works in an ordinary interactive browser.

Repeatedly disguising an unattended HTTP client is not an appropriate foundation
for Kryten.

Kryten is a personal desktop assistant, so the next design should use the user's
explicit browsing activity:

```text
Choose Source
    ↓
Browse Interactively
    ↓
Inspect an Offer
    ↓
Capture Explicitly
    ↓
Review Structured Cruise Data
```

This supports the longer-term product goal:

> Help Robin discover, remember and compare cruises that may be worth booking.

---

## Product Vision

Prompt 036 is the first part of a larger journey:

```text
Discover → Inspect → Capture → Save → Rate → Revisit → Compare → Book externally
```

Prompt 036 owns:

```text
Discover → Inspect → Capture → Review
```

Prompt 037 will own persistence and price history.

Prompt 038 will own saved-cruise evaluation, ratings, notes and preferences.

Later prompts will use those observations and preferences to help identify the
best cruises for Robin rather than merely reproducing a retailer's featured deal.

---

## User Experience

Selecting the Cruise capability should show a discovery workspace.

A first version may resemble:

```text
Cruise Discovery

[ TUI Cruise of the Week ] [ TUI Cruise Deals ] [ Future sources... ]

┌──────────────────────────────────────────────┐
│ Trusted source page displayed interactively   │
│                                              │
│ Robin can browse and choose an offer          │
│                                              │
└──────────────────────────────────────────────┘

[ Capture Displayed Cruise ] [ Open at TUI ]
```

### Before a Source Is Selected

Display:

- a short explanation of Cruise Discovery
- trusted-source buttons or chips
- no external page
- no network activity
- no fabricated Cruise result

### Browsing

After Robin selects a source:

- navigate only because of that explicit action
- show loading and navigation status
- display the current source and address
- support ordinary interactive browsing
- provide Back, Forward, Refresh and Stop where supported
- handle navigation failures visibly
- do not capture automatically

### Capture

`Capture Displayed Cruise` should:

- run only after an explicit click
- inspect the currently displayed page
- return structured data only when required fields can be identified honestly
- report missing or unsupported content as a controlled result
- never invent a title, ship, date, duration or price
- never submit a form or change the source page

### Review

After successful capture, display a review panel containing available values:

- cruise title
- cruise operator or provider
- ship
- departure date
- duration
- departure port
- itinerary summary
- cabin or fare basis where supplied
- current advertised price
- promotion
- advertising or retail source
- source reference
- observation timestamp

The review panel must distinguish captured values from optional missing values.

Prompt 036 should not persist the review. A later Prompt 037 action will save an
accepted observation and build history.

### Continue at Source

Provide a clear action to open the current trusted page externally when Robin
wants to continue investigating or book.

Opening an external browser must remain explicit. Kryten must not:

- log in on Robin's behalf
- add a cruise to a basket
- submit passenger details
- accept a final price
- store payment details
- complete a booking

---

## Trusted Sources

Prompt 036 begins with TUI because Robin knows Marella Cruises and considers TUI
offers useful.

Initial destinations should be deliberately small, for example:

- Marella Cruise of the Week
- a relevant TUI cruise deals or search page proven during implementation

Do not add speculative providers merely to populate the UI.

Source definitions should include only the information necessary for safe
navigation and presentation, such as:

- stable identifier
- display name
- trusted HTTPS host
- starting address
- optional description
- capture support state

Do not use an enum that requires shared code changes for every future source.

The source catalogue must not duplicate the Skill registry. Skills describe
Kryten capabilities; cruise sources describe destinations used by this specific
capability.

---

## Cruise Operator and Retail Source

Future discovery may show the same Marella sailing through both TUI and a
third-party retailer such as Iglu.

The design must therefore recognise that these concepts can differ:

```text
Cruise operator/provider: Marella Cruises
Advertising or retail source: TUI or Iglu
Cruise offer: the particular ship, itinerary and sailing date
Observation: what that source advertised at a particular time
```

The existing `CruiseProvider` must not be renamed or redesigned casually.

During the capture-contract step, review the existing Core models and introduce
the smallest provider-independent representation needed to retain the retail or
acquisition source. A small immutable `CruiseSource` value model is acceptable if
the existing models cannot represent the distinction honestly.

Do not place browser, HTML, URL-navigation or Avalonia types in Core.

---

## Architecture Principles

### Clean Architecture

Dependencies must continue to point inward.

```text
KrytenAssist.Core
        ↑
KrytenAssist.Application
        ↑
KrytenAssist.Infrastructure

KrytenAssist.Avalonia → Application/Core
KrytenAssist.Avalonia → Infrastructure at composition only
```

### Browser Isolation

The embedded-browser control is a presentation-platform concern.

Browser objects must not enter:

- Core models
- application interfaces
- Skill requests or results
- stored observations

Page extraction knowledge must be isolated by source. Only the TUI capture
adapter should understand TUI selectors, attributes or page structure.

### MVVM

Navigation, capture state and review state belong in focused ViewModels and
services.

Minimal code-behind may host a native browser control and forward lifecycle or
navigation events when Avalonia requires it. Business decisions, parsing,
capture validation and state transitions must not move into code-behind.

### Explicit External Work

No external page should load during:

- application startup
- service registration
- shell construction
- Dashboard display
- navigation to the empty Cruise Discovery page

Network activity begins only after Robin selects a source or requests navigation.

### Offline First

The application must still start and navigate when offline.

Offline tests must use fakes, fixtures or a local deterministic page. Automated
tests must not request TUI, Iglu or any other external service.

---

## NativeWebView Feasibility Gate

Avalonia 12 exposes `NativeWebView` through the separate
`Avalonia.Controls.WebView` package. Kryten currently uses Avalonia 12.0.5 while
the available WebView package version may not exactly match the main package.

Do not assume compatibility.

The first implementation step must prove all of the following on Robin's macOS
development environment:

1. the WebView package restores alongside the current Avalonia packages
2. the solution builds without incompatible package downgrades
3. a `NativeWebView` can be hosted in the existing shell layout
4. the TUI Cruise of the Week page loads interactively
5. JavaScript and required page resources execute
6. normal scrolling and links work
7. navigation completion can be observed
8. a harmless script such as `document.title` can be invoked
9. visible page content can be read without modifying the page
10. the application remains stable when the page fails or the machine is offline

This is a go/no-go gate.

If the embedded page or script invocation cannot be made reliable without
weakening security or redesigning the application, stop after documenting the
evidence. Do not implement the remaining stages by pretending feasibility was
proven.

---

## Capture Boundary

The capture workflow should expose an application-owned contract conceptually
similar to:

```csharp
public interface ICruisePageCaptureService
{
    Task<CruiseCaptureResult> CaptureAsync(
        CruisePageCaptureRequest request,
        CancellationToken cancellationToken = default);
}
```

The exact names and shapes should follow existing project conventions.

The contract must not expose:

- `NativeWebView`
- DOM nodes
- JavaScript engine objects
- AngleSharp types
- raw provider SDK types
- Avalonia controls

A transport-neutral request may contain controlled page content, the current
trusted source identity and source reference. The capture result may contain a
`CruiseObservation` plus source-neutral validation information.

Do not pass an unrestricted script from the ViewModel into a browser control.
Scripts and selectors belong to a trusted source-specific adapter.

---

## TUI Capture

The initial capture adapter should support only the specific TUI page shapes
proven during implementation.

It should capture values from structured page state where practical and fall
back to visible semantic content only when necessary.

Prefer, in order:

1. stable structured data published by the page
2. semantic attributes and accessible content
3. small, source-specific selectors

Avoid:

- positional selectors dependent on the complete page layout
- parsing visual coordinates
- OCR when DOM content exists
- copying the entire page into the shared application layer
- interpreting unrelated search results as the selected cruise
- treating a generic promotion banner as a complete Cruise observation

Extraction must validate required fields and detect ambiguity. Zero matches and
multiple conflicting matches are controlled failures.

Captured prices must retain their basis. `£999 per person`, `from £999` and
`£1,998 total` are not interchangeable.

---

## Security and Privacy

Embedded web content is untrusted external content.

Requirements:

- allow only HTTPS starting addresses
- identify trusted hosts explicitly
- show the current host to Robin
- require confirmation or external opening for navigation outside trusted hosts
- never treat page text as instructions to Kryten or the AI provider
- do not expose local files, prompts, memory or application services to page script
- do not inject secrets, API keys or personal application data
- do not persist browser cookies as Cruise data
- do not intercept credentials or payment information
- keep capture scripts read-only

Cookie consent remains an interaction between Robin and the displayed website.
Prompt 036 must not silently accept marketing consent.

---

## Error Handling

Represent these states clearly:

- no source selected
- loading
- page ready
- navigation failed
- offline
- unsupported host
- capture running
- capture succeeded
- capture incomplete
- capture ambiguous
- capture failed
- capture cancelled

Failures must be controlled and actionable.

Examples:

```text
The TUI page could not be loaded. Check your connection and try again.
```

```text
Kryten could not identify one complete cruise on this page. Open a specific
itinerary and try Capture Displayed Cruise again.
```

Do not expose raw JavaScript, DOM or platform exception messages directly to the
user.

---

## Testing Strategy

### Unit Tests

Cover:

- trusted-source definitions and host validation
- browser-state ViewModel transitions
- explicit navigation behavior
- no automatic navigation during construction
- capture command availability
- successful capture mapping
- missing required values
- ambiguous values
- unsupported pages and hosts
- cancellation
- review-state presentation
- external-open command delegation

### Capture Fixtures

Use small fictional TUI-shaped fixtures that contain only the structures needed
by the adapter.

Do not check in a complete live TUI page containing personal, tracking or
copyrighted marketing content.

### Browser Adapter Tests

Use a fake browser boundary or deterministic local page for automated tests.

No automated test may access an external website.

### Manual Verification

After automated verification, Robin should explicitly test:

1. open Cruise Discovery
2. confirm no page loads automatically
3. select TUI Cruise of the Week
4. interact with the page normally
5. navigate to a specific offer where available
6. capture the displayed cruise
7. compare captured fields with the visible page
8. verify Back, Forward, Refresh and Stop
9. verify offline and page-failure behavior
10. verify external opening without submitting any booking action

Record the date, source address and concise result. Do not record cookies,
tracking identifiers or personal browsing data.

---

## Explicitly Out of Scope

Prompt 036 must not implement:

- persistent Cruise history
- price trend calculations
- saved-cruise ratings
- preference learning
- watch lists
- alerts or notifications
- background jobs
- scheduled page access
- unattended scraping
- cabin availability monitoring
- comparison across retailers
- AI-generated extraction
- automatic booking
- authentication management
- payment handling
- the complete Cruise Dashboard from Prompt 042

Do not repair the old `HttpClient` provider by adding browser impersonation,
cookie replay or escalating anti-bot workarounds.

---

## Implementation Steps

### Step 1 – 036a: Embedded Cruise Browser Feasibility

- evaluate `Avalonia.Controls.WebView` compatibility
- host a minimal `NativeWebView`
- load TUI only after explicit action
- verify navigation and harmless script invocation
- document a clear go/no-go result
- stop if feasibility is not proven

### Step 2 – 036b: Cruise Source Navigation

- introduce trusted source definitions
- add Cruise Discovery navigation and ViewModel state
- add source chips or buttons
- add browser navigation controls
- enforce HTTPS and trusted-host rules
- preserve offline startup and non-loading navigation

### Step 3 – 036c: Cruise Capture Contract

- review the existing Cruise domain
- define the provider-independent capture boundary
- represent retail/acquisition source only as far as demonstrated requirements need
- define controlled capture outcomes
- keep browser and page types outside shared contracts

### Step 4 – 036d: TUI Cruise Capture Adapter

- inspect the proven TUI page shape
- implement source-specific read-only extraction
- map valid values into Cruise models
- validate required, missing and ambiguous data
- add small offline fixtures and focused tests

### Step 5 – 036e: Cruise Capture Review

- add the explicit capture action
- show running, success, incomplete, cancelled and failed states
- display captured fields in a review panel
- add explicit external opening
- do not persist or rate the cruise

### Step 6 – 036f: Cruise Discovery Tests

- complete ViewModel, navigation, security-boundary and capture tests
- confirm no test performs external network access
- run focused tests and the complete regression suite
- make only minimal production corrections exposed by deterministic tests

### Step 7 – 036g: Cruise Discovery Verification

- verify architecture and dependency direction
- verify browser isolation and trusted-host behavior
- verify no automatic external work
- verify capture accuracy and controlled failures
- build the complete solution
- run all tests
- perform the explicit manual TUI workflow with Robin
- update this Playbook's Results and Lessons Learned sections
- update the Roadmap and create a Session Handover where appropriate

---

## Acceptance Criteria

Prompt 036 is complete only when:

- the embedded-browser feasibility gate has passed
- TUI loads only after an explicit user action
- Robin can browse the supported TUI source interactively
- trusted sources are represented without a hard-coded enum
- navigation outside trusted hosts is controlled
- capture runs only after an explicit action
- one proven TUI page shape maps into provider-independent Cruise data
- incomplete or ambiguous pages fail honestly
- captured values can be reviewed before any later persistence
- the source company and source reference are retained
- browser, DOM and JavaScript types do not leak inward
- application startup and ordinary navigation remain offline-safe
- automated tests are deterministic and make no external requests
- the solution builds and all tests pass
- manual verification is recorded
- Results and Lessons Learned below are completed

---

## Results

> Complete after implementation and verification.

### Status

Not started.

### Feasibility Outcome

To be completed after 036a.

### Files Created

To be completed.

### Files Updated

To be completed.

### Build

Not run.

### Tests

Not run.

### Manual Verification

Not performed.

### Git Commits

Not created.

---

## Lessons Learned

> Complete after implementation and verification. Do not begin Prompt 037 until
> this section and Results have been updated.
