# Prompt 037h – Multiple Cruise Deals Handling

## Goal

Extend Kryten's proven single-Cruise capture and history workflow so Robin can
capture, review and explicitly record multiple Cruise deals displayed on one
supported TUI results page.

Prompt 037h should allow Robin to:

- browse a supported TUI Cruise deals page interactively
- capture the currently loaded Cruise cards through one explicit action
- receive one independently validated provider-independent observation per card
- review successful and incomplete candidates before recording anything
- select individual observations for recording
- explicitly record all successfully captured observations when wanted
- see a controlled outcome for every attempted record
- refresh Recorded Cruise History once after the batch completes
- preserve the existing single-Cruise Cruise of the Week workflow

Prompt 037h owns bounded multi-card capture and explicit batch recording of
factual observations.

It does not own ratings, notes, favourites or preferences. Those remain Prompt
038.

---

## Why This Prompt Exists

Prompts 036 and 037 established a safe complete workflow:

```text
Browse → Capture one Cruise → Review → Record Observation → Revisit History
```

On the Marella Cruise of the Week landing page, the demonstrated TUI component
contains one unique Cruise card, so the Application capture result and Avalonia
review deliberately accept one `CruiseObservation`.

Robin then demonstrated a broader path:

```text
TUI Cruise of the Week
        ↓ View Deals
TUI Voyager Cruise deals page
        ↓ Capture Displayed Cruise
More than one candidate is returned
```

The current Infrastructure adapter treats more than one candidate as
`Ambiguous` and asks Robin to open one itinerary. That was the correct safety
boundary before multi-card behavior was inspected, but it now prevents a useful
workflow that the page can support honestly.

Kryten should be able to remember multiple offers Robin deliberately inspected
without requiring each itinerary to be opened and captured individually.

---

## Demonstrated Live Page

The page inspected on **17 July 2026** was:

```text
https://www.tui.co.uk/cruise/deals/voyager-cruises
```

Observed facts:

- the page title was `Marella Voyager Deals`
- TUI reported 25 Cruise holidays
- 10 cards were currently loaded into the page
- one open `tui-product-cards` shadow root contained the results
- the loaded cards used `[data-testid="product-card"]`
- each card contained two links to the same itinerary, which reduce to one exact
  unique itinerary URL per card
- 20 raw itinerary links therefore represented 10 unique loaded candidates
- each inspected card independently contained:
  - itinerary title
  - ship name
  - outbound/departure date
  - return date
  - departure port
  - displayed original price
  - current per-person price
  - total price based on two sharing
  - per-person discount
  - a trusted TUI itinerary URL with itinerary, ship, sailing and package
    evidence

Examples observed during inspection included:

```text
Iconic Islands     Marella Voyager   24 Jul 2026   £1,136pp
Aegean Shores      Marella Voyager   31 Jul 2026   £1,088pp
Adriatic Explorer  Marella Voyager   07 Aug 2026   £1,377pp
```

These values are volatile live evidence, not permanent fixtures or expected test
values. Do not copy the live opaque package identifiers into documentation or
tests.

---

## Important Existing Extraction Defect

The current fixed script finds each itinerary link and then chooses a candidate
container using:

```text
closest('article,[data-testid],[data-cruise-card],li')
```

On the demonstrated Voyager page, the nearest matching ancestor is the heading:

```text
<h3 data-testid="resort-name">Iconic Islands</h3>
```

It is not the full product card.

Consequences on a multi-card page:

- local container text contains only the title
- ship may fall back to the first page-wide ship evidence
- price may fall back to the first page-wide price evidence
- multiple candidates could therefore receive evidence from the wrong card

The adapter currently returns `Ambiguous` before mapping those candidates, so
incorrect observations are not persisted. Preserve that safety until exact card
scoping is implemented and tested.

Prompt 037h must select the demonstrated card first:

```text
closest('[data-testid="product-card"]')
```

All title, ship, date, price, total and promotion evidence must be read from that
specific card. Page-wide fallbacks must not copy evidence between cards.

A candidate-specific URL parameter, such as the trusted `shipCode`, may support
a narrowly demonstrated map because it belongs to that candidate. A page-wide
ship or price fallback is not acceptable for batch capture.

---

## Product Journey

The broader journey becomes:

```text
Discover → Inspect results → Capture loaded deals → Review batch
         → Record selected observations → Revisit history
         → Rate and organise later
```

Prompt 037h owns:

```text
Capture loaded deals → Review batch → Record selected factual observations
```

Prompt 038 will own:

```text
Save interesting Cruise → Rate → Add notes → Express preferences
```

Recording every displayed observation must not imply Robin likes or recommends
those cruises. It records only what TUI advertised at that time.

---

## User Language

When a supported page contains more than one independently scoped Cruise card,
prefer:

```text
[ Capture Loaded Cruises ]
```

The existing single-card action may remain:

```text
[ Capture Displayed Cruise ]
```

Review actions should use:

```text
[ Record Selected ] [ Record All Observations ]
```

Do not use `Save All Cruises`. Prompt 038 owns the separate concept of a saved
Cruise that Robin has chosen to organise or evaluate.

Do not claim to capture all 25 TUI deals when only 10 cards are loaded in the
current DOM. Use `loaded`, `displayed` or the exact captured count.

---

## User Experience

### Capture Multiple Loaded Cards

After Robin explicitly selects Capture Loaded Cruises:

- run the fixed bounded read-only script once
- inspect only the currently loaded supported product cards
- deduplicate repeated exact itinerary links
- validate every candidate independently
- return successful and incomplete candidates in deterministic card order
- do not navigate, expand cards, click Load More or alter filters
- do not automatically record any observation

Example controlled result:

```text
Captured 9 of 10 loaded Cruise deals.

9 ready to review
1 incomplete — price was unavailable
```

If no supported product card is found:

```text
Kryten could not identify supported Cruise deal cards on this TUI page.
```

If more cards exist than the bounded maximum:

```text
Kryten captured the first 10 loaded Cruise deals.
Refine the TUI results or capture another page to review more.
```

Never silently truncate without telling Robin.

### Batch Review

Show a concise row/card per candidate containing:

- capture state: Ready or Incomplete
- itinerary title
- operator
- retail source
- ship
- departure date
- duration
- departure port where available
- current advertised price(s)
- promotion summary where available
- candidate-specific trusted source reference
- missing fields for incomplete candidates
- selection state for valid candidates

Incomplete candidates must remain visible so Robin understands why they cannot
be recorded. They must not produce a fabricated observation.

Allow:

- Select All Ready
- Clear Selection
- individual selection
- Open at TUI for one selected/reviewed candidate where its exact address remains
  trusted
- Record Selected
- Record All Observations

`Record All Observations` means every valid reviewed observation in the current
batch. It does not include incomplete candidates.

### Batch Recording

Recording remains an explicit second action after review.

For each selected observation, reuse the existing provider-independent
`RecordCruiseObservation` behavior and retain one controlled outcome:

- first observation recorded
- changed observation recorded
- already current
- cancelled
- failed

Example completion:

```text
10 observations checked against local history.

6 first observations
2 changed observations
1 already current
1 failed — you can retry it
```

Each Cruise is an independent durable aggregate. A failure for one candidate
should not roll back successful records for unrelated sailings.

Cancellation should:

- stop before beginning the next unprocessed candidate
- preserve outcomes already completed
- leave remaining candidates clearly unprocessed and retryable
- never convert cancelled/unprocessed candidates into failures

After at least one successful or already-current outcome:

- refresh Recorded Cruise History once
- preserve or select a useful affected history deterministically
- do not refresh once per candidate

---

## Provider-Independent Batch Contract

The Application layer must own the multi-capture contract.

Do not expose TUI payload types, card selectors, HTML, DOM or JavaScript outside
the Infrastructure/Avalonia browser boundary.

The contract should represent:

- bounded ordered candidate results
- a successful candidate containing one `CruiseObservation`
- an incomplete candidate containing controlled missing-field names
- safe unsupported/failed/cancelled batch-level outcomes
- whether the source contained more candidates than the returned bound
- a controlled summary message

A batch may be partially successful. Do not reuse the existing single
`CruiseCaptureResult` in a way that forces one global success or failure.

Prefer focused types such as:

```text
CruiseCaptureBatchResult
CruiseCaptureCandidateResult
CruiseCaptureCandidateStatus
```

Exact names should follow existing conventions after implementation inspection.

### Candidate Source Reference

Every successful observation should retain its own exact trusted itinerary URL
where demonstrated.

The listing page URL identifies where capture began, but it is not sufficient as
the only source reference for ten distinct offers. Do not store one shared
listing URL as if it were the individual itinerary reference for every card.

Validate each candidate URL as:

- absolute HTTPS
- exact supported TUI host
- supported Cruise itinerary path/query shape
- bounded before crossing the capture boundary

Do not persist cookies, tracking storage or browser session state. The existing
TUI URL may contain an opaque package id needed as retail evidence; do not make
it the physical sailing identity.

---

## TUI Adapter Rules

The TUI adapter must:

- accept the supported versioned bounded batch payload
- validate the source and payload before mapping
- enforce the maximum candidate count
- preserve candidate order
- validate each candidate independently
- map each valid candidate to one provider-independent `CruiseObservation`
- return incomplete items rather than fail the whole batch for one missing field
- treat duplicate exact itinerary references deterministically
- reject a candidate whose URL is untrusted or inconsistent
- preserve Marella Cruises as operator and TUI as retail source
- remain free of Avalonia and browser types

Do not infer one candidate's values from another candidate.

The existing single-candidate Cruise of the Week mapping must remain compatible.
Where practical, one shared private candidate validator/mapper should serve both
single and batch paths without weakening either contract.

---

## Fixed Script Boundary

The browser script remains:

- fixed application-owned code
- bounded
- read-only
- user-triggered
- limited to the demonstrated open `tui-product-cards` shadow root

It must not:

- accept arbitrary JavaScript input
- click View Deal
- click Load More
- change sort or filters
- scroll automatically
- submit forms
- mutate the DOM
- read cookies or browser storage
- return full HTML
- recursively crawl arbitrary shadow roots
- perform an external request

Retain or tighten the existing limits:

- at most 3 demonstrated `tui-product-cards` roots
- at most 10 unique candidate results
- bounded string fields
- bounded semantic scans

Candidate extraction should begin from exact unique itinerary links and locate
the nearest demonstrated product card. The resulting JSON must include only the
fields needed by the Application-owned capture contract.

---

## Existing Cruise History Compatibility

The Prompt 037 domain and SQLite schema already support multiple different
sailings and multiple retail sources.

Do not redesign:

- `CruiseSailingKey`
- meaningful-change fingerprint semantics
- price-history analysis
- SQLite history/observation/price schema
- existing migrations
- transaction/concurrency behavior

Batch orchestration should call the existing single-observation record use case
for each independently validated observation unless a demonstrated atomicity or
performance requirement proves another Application abstraction necessary.

Do not wrap unrelated sailings in one database transaction. Partial success must
remain observable and retryable.

---

## Price Boundary

The demonstrated cards show several monetary facts:

- crossed-out/original price
- current per-person price
- total price based on two sharing
- per-person discount

The current Cruise model records a collection of prices plus a promotion summary
but does not explicitly distinguish original price, discounted price,
per-person discount and booking-level discount.

Prompt 037h should capture the same neutral price evidence already supported by
the current single-card workflow. It must not silently introduce or infer the
richer pricing model while implementing batch handling.

If exact product-card scoping demonstrates a current price or total price, retain
it with an honest basis. Do not label a price discounted unless the domain later
models that meaning explicitly.

The richer price model remains a separately analyzed future correction.

---

## Cancellation and Stale Results

Use deterministic operation generations for both capture and batch recording.

If Robin:

- cancels capture
- navigates to another address
- changes source
- refreshes
- closes the browser
- starts a later capture

then a late earlier batch result must not replace the current review.

If Robin changes selection during recording, the in-flight batch should retain a
stable copy of the selected observations it began with. UI selection changes
must not alter the active enumeration unexpectedly.

Do not use sleeps, polling or UI-thread blocking.

---

## Offline and Security Principles

All automated tests must remain offline.

Do not:

- call the live TUI page from a test
- run `NativeWebView` in tests
- use a JavaScript engine
- launch a browser or OS URL handler
- store live HTML fixtures
- store cookies, credentials or personal booking data
- use Robin's production SQLite database

Use:

- small fictional bounded payload fixtures
- hand-written fakes
- fixed clocks
- controlled incomplete tasks
- explicit trusted/untrusted addresses
- isolated in-memory or temporary SQLite only where recording integration is
  tested

Live page behavior should be recorded as manual evidence, not converted into a
brittle full-page fixture.

---

## Explicitly Out of Scope

Prompt 037h must not implement:

- automatic capture on navigation
- automatic recording after capture
- capturing unloaded results or all 25 deals through hidden pagination
- clicking Load More
- changing TUI filters or sort
- unattended scraping or scheduled capture
- cross-retailer comparison or ranking
- best-Cruise recommendations
- ratings, interest levels or preferences
- personal notes or favourites
- deletion or editing of recorded history
- price-drop alerts or notifications
- original/discounted/extra-discount domain redesign
- authentication, booking or payment automation
- Prompt 038 behavior

---

## Implementation Steps

### Step 1 – 037h-a: Multi-Cruise Capture Contract

- define provider-independent bounded batch and per-candidate results
- distinguish successful, incomplete, unsupported, failed and cancelled states
- support ordered partial success without one global Ambiguous outcome
- retain candidate-specific trusted source reference
- preserve the existing single-capture contract
- add focused Application contract tests

### Step 2 – 037h-b: TUI Multi-Card Capture Adapter

- change the fixed script to scope every candidate to
  `[data-testid="product-card"]`
- remove unsafe page-wide ship/price reuse across candidates
- retain exact-link deduplication and the maximum of 10 candidates
- map each card independently through Infrastructure
- preserve candidate order and candidate-specific itinerary reference
- report truncation and incomplete candidates honestly
- keep the single Cruise of the Week path working
- add fictional bounded payload, adapter and fixed-script regression tests

### Step 3 – 037h-c: Multi-Cruise Capture Review

- add a multi-candidate capture lifecycle to the Cruise workspace
- show ready and incomplete candidates in deterministic order
- add individual selection, Select All Ready and Clear Selection
- support explicit trusted Open at TUI per valid candidate
- preserve browser navigation/cancellation stale-result boundaries
- retain the existing single-card review where appropriate
- keep views passive and MVVM-driven

### Step 4 – 037h-d: Batch Observation Recording

- add explicit Record Selected and Record All Observations
- reuse the existing single-observation record use case per candidate
- retain first/changed/already-current/cancelled/failed outcome per observation
- support partial completion and retryable remaining items
- stop cleanly on cancellation before the next candidate
- refresh Cruise History once after useful outcomes
- add deterministic orchestration and presentation tests

### Step 5 – 037h-e: Multiple Cruise Deals Tests and Verification

- verify exact product-card isolation and no cross-card evidence leakage
- verify bounds, deduplication, order, truncation and partial results
- verify single-card Cruise of the Week compatibility
- verify capture and recording cancellation/stale-result behavior
- verify partial recording outcomes and one history refresh
- run focused and complete offline suites
- perform the live Voyager capture/review/record workflow with Robin
- update this Playbook's Results and Lessons Learned
- update the Roadmap and create a Session Handover where appropriate
- leave Prompt 038 unstarted

---

## Acceptance Criteria

Prompt 037h is complete only when:

- capture remains explicitly user-triggered
- the demonstrated Voyager results page is supported through exact product-card
  scoping
- no candidate receives ship, price or promotion evidence from another card
- duplicate itinerary links reduce to one candidate per exact address
- at most 10 currently loaded candidates cross the boundary
- truncation is visible and honest
- every candidate is validated independently
- partial success retains ready and incomplete candidates
- each successful observation has its own trusted itinerary source reference
- single Cruise of the Week capture still works
- batch review shows all ready/incomplete outcomes clearly
- recording remains a separate explicit action
- Record Selected and Record All operate only on valid reviewed observations
- every attempted record has a controlled outcome
- cancellation preserves completed outcomes and leaves remaining items retryable
- one candidate failure does not roll back unrelated successful records
- history refreshes once after useful batch results
- existing sailing identity, history schema and migrations remain unchanged
- views remain passive and provider-specific payloads remain in Infrastructure
- no browser/DOM/TUI type leaks into Application/Core
- no test contacts TUI, launches a browser or accesses Robin's database
- complete solution builds and all tests pass
- Robin manually verifies the supported live multi-deal workflow; if the
  originally demonstrated retailer URL has changed, record the replacement
  supported page and the external volatility honestly
- Results and Lessons Learned are complete
- Prompt 038 remains unstarted

---

## Results

> Completed after implementation and verification on 17 July 2026.

### Status

Complete.

### Live Page Shape

The demonstrated Voyager page used `tui-product-cards` with ten loaded
`[data-testid="product-card"]` cards. The fixed extraction script scopes all
evidence to that exact card boundary.

During final manual verification the original Voyager page was no longer
discoverable. Robin instead verified the equivalent multi-deal workflow on:

```text
https://www.tui.co.uk/destinations/deals
```

Robin successfully captured multiple deals and explicitly recorded
observations. This is live functional evidence, while the volatile Voyager page
shape remains covered by fictional deterministic tests.

### Batch Contract

Application owns bounded ordered batch and candidate result contracts. Ready,
Incomplete, Failed, Unsupported and Cancelled states remain distinct; each
Ready candidate requires its own exact HTTPS itinerary reference.

### TUI Card Isolation

The TUI adapter extracts only demonstrated `product-card` containers within the
open `tui-product-cards` shadow root. It removes duplicate exact itinerary
references, preserves first-seen card order, limits capture to ten cards and
retains truncation evidence. Fixed fictional-payload tests prove a card cannot
receive another card's ship, price or promotion evidence.

### Review and Selection

Avalonia displays deterministic Ready, Incomplete and Failed review rows. Only
Ready rows can be selected or opened at their independently validated trusted
TUI address. One clean untruncated candidate preserves the existing single
Cruise of the Week workflow; a genuine batch remains a batch.

### Batch Recording

Record Selected and Record All Observations process a stable sequential snapshot
through the existing Application record use case. Each row retains its own
First, Changed, Already Current, Failed or Cancelled outcome. Useful outcomes
refresh Cruise History exactly once; failed, cancelled and unattempted rows
remain retryable.

### Single-Cruise Compatibility

Single-Cruise capture, review and Record Observation behavior remains covered
by focused lifecycle and History tests. Robin also confirmed the broader
multi-deal capture-and-record workflow remains usable in the desktop app.

### Files Created

- Application batch contracts and batch capture service interface
- TUI batch adapter tests
- Avalonia candidate review and batch recording presentation types/tests
- Codex prompts 037h-a through 037h-e
- `docs/Session Handovers/2026-07-17 Session 019.md`

### Files Updated

- TUI fixed capture script, adapter and dependency registration
- Cruise browser/review/History ViewModels and passive review view
- Roadmap and this Playbook

### Production Corrections

None during final verification.

### Build

Passed:

```text
dotnet build KrytenAssist.sln --no-restore
```

Result: 0 errors. The five existing NU1903 warnings concern the known
`SQLitePCLRaw.lib.e_sqlite3` advisory.

### Tests

Focused multi-Cruise verification passed: 164 passed, 0 failed, 0 skipped.

Complete offline regression passed using the stable single-worker runner:

```text
dotnet test KrytenAssist.sln --no-build --no-restore --disable-build-servers -m:1
```

- Core: 105 passed
- Avalonia: 415 passed
- API: 9 passed
- Total: 529 passed, 0 failed, 0 skipped

Tests use fictional bounded payloads, fakes and isolated persistence only. No
test contacts TUI, launches a browser or accesses Robin's local database.

### Manual Verification

Passed on 17 July 2026. Robin could not locate the volatile Voyager page but
used `https://www.tui.co.uk/destinations/deals` to capture multiple offers and
record observations successfully.

An Italy destination page subsequently demonstrated a different TUI template:
`[data-testid="small-product-card"]`. Kryten reported that page as unsupported
instead of guessing across card boundaries, which is the intended safety
behavior. Supporting that template requires a later tested extension.

### Git Commits

Implementation commits:

- `726804d` – 037h-a Multi Cruise Capture Contract
- `b974ff5` – 037h-b TUI Multi card capture Adapter
- `f78a426` – 037h-c Multi Cruise Capture Review
- `c3fece1` – 037h-d Batch Observation Recording

---

## Lessons Learned

- TUI pages can use separate card templates inside the same component. Support
  must be added per demonstrated template with fixed fixtures; a generic
  fallback would risk cross-card evidence leakage.
- Live retailer pages are volatile. Their manual results are useful evidence,
  but durable behavioral guarantees belong in offline fictional-payload tests.
- Explicit sequential batch recording gives Robin controlled outcomes and
  preserves the existing single-observation history model without pretending
  unrelated sailings form one transaction.
- The multi-Cruise review needs a future layout refinement so source navigation
  remains comfortable while comparing supported and unsupported pages.
