# Codex Prompt 037k – Modern TUI Cruise Results Capture Compatibility

## Implementation Prompt

Fix the demonstrated Cruise Discovery capture failure on TUI's current Cruise
Packages results layout without weakening the existing trusted, bounded and
read-only capture boundary.

This is a focused Prompt 037 compatibility correction. Prompt 040 remains next
after this fix. Do not implement Cabin Availability, alerts, new Cruise domain
fields or generic retailer scraping here.

---

## Demonstrated Failure

On 19 July 2026, Cruise Discovery loaded but could not capture:

```text
https://www.tui.co.uk/cruise/packages?from%5B%5D=STN%3AAirport&to%5B%5D=&when=01-10-2026&flexibility=false&flexibleDays=1&duration=1-7&addAStay=0&until=&choiceSearch=true&noOfAdults=2&noOfChildren=0&childrenAge=&searchRequestType=ins&searchType=search&sp=true&room=
```

Read-only live inspection found:

- TUI reported 16 cruises
- itinerary links used trusted `/cruise/bookitineraries/` paths
- cards were light-DOM
  `section.component.ResultListItem__cruiseResultItem` elements
- title and Continue links commonly duplicated the same itinerary URL
- the page had no `tui-product-cards` elements or open shadow roots

The existing script searches only open shadow roots under `tui-product-cards`
and requires `[data-testid="product-card"]`, so it returns zero candidates.

The first demonstrated card exposed:

```text
Iconic Islands
Sailing on Marella Voyager
Departs 02 Oct 2026
7 Night cruise
£1439pp
Total Price £2877
Includes £451pp discount
```

The URL and values are volatile diagnostic evidence, never an automated network
test or permanent price assertion.

---

## Required Reading

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/036 - Cruise of the Week.md`
5. `docs/AI Playbook/037 - Cruise History and Price Tracking.md`
6. `docs/AI Playbook/037h - Multiple Cruise Deals Handling.md`
7. `docs/Codex Prompts/037h-b - TUI Multi-Card Capture Adapter.md`
8. `KrytenAssist.Avalonia/Cruises/Discovery/TuiCruiseCaptureScript.cs`
9. `KrytenAssist.Infrastructure/Cruises/Tui/TuiCruisePageCaptureService.cs`
10. existing TUI capture, batch, trust, review and script-safety tests

---

## Required Behaviour

### Two Explicitly Supported Page Structures

Retain capture from up to three existing open `tui-product-cards` shadow roots
and add the current page's light DOM. Recognized card containers are exactly:

```text
[data-testid="product-card"]
section.ResultListItem__cruiseResultItem
```

Do not introduce a generic article/list-item/nearest-ancestor fallback. Search
only link/data-link elements within recognized cards. Preserve the current
bounded limits: roots, 100 scanned links, 512-character fields, 4,000-character
references and at most ten returned candidates.

### Identity and Deduplication

Continue to resolve references with `new URL(value, document.location.href)`
and require `/cruise/bookitineraries/` plus `itineraryCodeOne` or
`itineraryCode`. Existing Infrastructure HTTPS/TUI-host validation remains
unchanged.

Deduplicate title/Continue links by canonical absolute itinerary URL before the
ten-candidate limit. `WasTruncated` describes unique currently loaded supported
cards, not raw links or TUI's headline result count.

### Existing Candidate Contract

Without changing Core/Application models, extract:

- title from itinerary path/code
- offer id from `packageId`, then itinerary code/path fallback
- known Marella ship from bounded card text or existing semantic fallback
- departure date from `sailingDate`
- duration from `cruiseDuration`
- canonical itinerary source reference
- Marella Cruises as operator and TUI as retailer

Missing required evidence remains Incomplete; never invent it.

### Price Variants

Support both existing and demonstrated ordering:

```text
£1439pp
£1,439 per person
Total Price £2877
Total Price £2,877
£2877 Total Price
```

Per-person remains `GBP / per person`. A present total remains
`GBP / total based on 2 sharing`. Never infer total by multiplication. Ignore
unrecognized price/currency phrases instead of guessing.

### Promotion Variants

Capture bounded meaningful visible phrases including:

```text
Includes £451pp discount
Includes £38pp online discount
```

Absence remains null. Do not introduce original/discounted price concepts.

### Loaded Results Only

Capture only cards present when Robin explicitly chooses Capture. Do not scroll,
click Load More, fetch endpoints or claim the headline result count was
captured. Preserve existing Ready/Incomplete/Failed and truncation review.

---

## Safety Boundary

The script remains fixed, bounded and read-only. It must not:

- use `fetch`, `XMLHttpRequest` or other network calls
- read cookies, local/session storage or browser history
- read/return `innerHTML`, `outerHTML` or bulk page markup
- click, focus, submit, scroll or mutate the page
- inspect closed shadow roots or arbitrary frames
- execute page-provided script or transmit captured data

Do not loosen trusted host/path/query checks.

---

## Required Offline Tests

Retain all existing shadow-DOM coverage and add representative modern-layout
fixtures/evidence for:

- zero `tui-product-cards` with recognized light-DOM cards
- exact `ResultListItem__cruiseResultItem` recognition
- duplicate title/Continue URLs producing one candidate
- distinct URLs producing distinct candidates
- itinerary-like links outside recognized cards being ignored
- relative links becoming canonical absolute TUI references
- offer id, sailing date, duration and known Marella ship mapping
- compact/comma per-person prices and both total-price orderings
- online/non-online discount phrases and absent promotion
- missing price remaining Incomplete
- more than ten unique cards reporting truncation
- non-TUI, HTTP, wrong-path and missing-code references remaining rejected
- all field/link/root bounds and script safety prohibitions
- existing capture, review, navigation, recording and History regressions

If the current harness cannot execute DOM extraction, keep the script
selector/safety audit and pass representative version-1 payload fixtures through
the real batch adapter. Do not add a second production C# DOM parser merely for
tests, and do not add live browser/network tests.

---

## Manual Acceptance

1. Load the supplied URL, or an equivalent current light-DOM results URL if it
   expires.
2. Wait for visible results and choose Capture Current Page.
3. Confirm currently loaded supported cards enter review.
4. Confirm title/Continue duplicates do not duplicate cruises.
5. Check title, ship, date, duration, per-person price, available total and
   promotion against one visible card.
6. Confirm more than ten unique loaded cards uses existing truncation feedback.
7. Recheck an existing shadow-DOM page if TUI still serves one; otherwise record
   that the frozen offline regression is the available evidence.
8. Confirm no automatic scrolling, clicking, loading or recording occurs.

---

## Allowed Changes

```text
KrytenAssist.Avalonia/Cruises/Discovery/TuiCruiseCaptureScript.cs
KrytenAssist.Avalonia.Tests/ViewModels/CruiseCaptureReviewViewModelTests.cs
KrytenAssist.Avalonia.Tests/Cruises/Tui/*Capture*.cs
KrytenAssist.Avalonia.Tests/Fixtures/Cruises/Tui/*
docs/Codex Prompts/037k - Modern TUI Cruise Results Capture Compatibility.md
docs/AI Playbook/037 - Cruise History and Price Tracking.md
docs/Roadmap.md
```

The Infrastructure adapter may change only for a concrete demonstrated mapping
defect. Document and test it. Do not change shared Cruise models, SQLite schema,
alerts, Prompt 040 or unrelated UI. Do not stage, commit, push or discard other
work.

---

## Exclusions

- Prompt 040 Cabin Availability
- background monitoring or unattended capture
- automatic scrolling/loading of all TUI results
- private API/endpoint reverse engineering
- generic page extraction or new retailers/templates
- destination `small-product-card` support without separate agreement
- new price/discount domain concepts
- automatic observation recording
- browser/network access in automated tests

---

## Verification

Run focused TUI capture/script/review tests, then:

```text
dotnet build KrytenAssist.sln --no-restore
dotnet test KrytenAssist.sln --no-build --no-restore
git diff --check
```

Use the established single-worker runner where required.

---

## Results

Implementation and automated verification completed on 19 July 2026. Robin's
in-app manual confirmation remains.

### Status

Implemented; awaiting manual confirmation.

### Implementation

- Preserved the existing bounded open-shadow-root product-card path.
- Added exact light-DOM `ResultListItem__cruiseResultItem` card discovery.
- Deduplicated title/Continue links before the ten-candidate boundary.
- Added current compact/comma per-person price and both total-price orderings.
- Preserved meaningful normal and online discount phrases.
- Added demonstrated Marella Explorer/Voyager ship-code fallbacks while
  retaining card-local semantic matching.
- Kept the fixed script read-only with existing field, reference, link and
  candidate bounds.

### Live Read-Only Verification

The exact production script was executed against Robin's supplied page on 19
July 2026. It returned ten currently loaded unique candidates. Samples included:

- Iconic Islands — Marella Voyager — 2 October 2026 — 7 nights — £1,439pp —
  £2,877 total — £451pp discount
- Magic of Spain — Marella Explorer — 3 October 2026 — 7 nights — £1,245pp —
  £2,489 total — £38pp online discount
- Cosmopolitan Charms — Marella Discovery 2 — 6 October 2026 — 7 nights —
  £1,245pp — £2,491 total — £39pp online discount

This verified extraction only; Robin must still confirm the complete in-app
Capture Current Page and review workflow.

### Build and Tests

- Solution build passed with 0 errors.
- Core: 139 passed.
- Avalonia/Application/Infrastructure: 509 passed.
- API: 9 passed.
- Total: 657 passed, 0 failed, 0 skipped.
- Existing SQLitePCLRaw advisory warnings remain unchanged.
