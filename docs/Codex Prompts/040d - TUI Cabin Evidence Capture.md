# Codex Prompt 040d – TUI Cabin Evidence Capture

## Implementation Prompt

Implement **Step 040d only** from:

```text
docs/AI Playbook/040 - Cabin Availability.md
```

Prompts 040a–040c are complete. Capture honest partial cabin evidence from the
explicitly demonstrated modern TUI Cruise Packages result cards. Preserve the
fixed, bounded, trusted and read-only browser boundary. Do not record evidence,
evaluate preferences/alerts or add Cabin Availability UI.

No richer TUI cabin-selection page has been demonstrated. Do not guess its
selectors, category meanings or unavailable states.

---

## Required Reading

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/037 - Cruise History and Price Tracking.md`
5. `docs/AI Playbook/037h - Multiple Cruise Deals Handling.md`
6. `docs/AI Playbook/040 - Cabin Availability.md`
7. `docs/Codex Prompts/037k - Modern TUI Cruise Results Capture Compatibility.md`
8. `docs/Codex Prompts/040a - Cabin Availability Experience and Evidence Contract.md`
9. `docs/Codex Prompts/040b - Cabin Domain and Application Contracts.md`
10. `docs/Codex Prompts/040c - SQLite Cabin Persistence.md`
11. existing cabin capture contracts, TUI script/adapter/registration, fixtures
    and script-safety tests

---

## Demonstrated Evidence and Exact Meaning

On 19 July 2026, a modern TUI Cruise Packages result card demonstrated:

```text
1 x Inside Cabin
(Cheapest available)
```

The demonstrated search address contained explicit values including:

```text
from[]=STN:Airport
when=01-10-2026
addAStay=0
noOfAdults=2
noOfChildren=0
childrenAge=
room=
```

For that card and exact search context this may produce only:

```text
Inside  = Available
Outside = Unknown
Balcony = Unknown
Suite   = Unknown
Solo    = Unknown
Coverage = Partial
```

It does not prove omitted categories unavailable, expose an inventory count or
apply to another occupancy, airport or package context. The `when` value is the
search window, not the candidate sailing date. Sailing identity continues to
come from the trusted itinerary reference.

---

## Application Capture Contract Correction

The unused 040b single-observation capture request contains no page payload and
cannot represent the demonstrated bounded result-card batch. Amend that seam
rather than parsing TUI JSON or query parameters in Application/Avalonia.

Add Application-owned transport-neutral contracts following existing Cruise
page capture conventions:

- request: source identifier, `CruiseSource`, current trusted HTTPS page
  reference, evidence time and bounded opaque page payload
- batch result: deterministic candidates, truncation, controlled status/message
- candidate result: safe display label, trusted itinerary reference, status,
  optional `CruiseCabinObservation`, missing fields and controlled message
- Application-owned page/batch cabin capture interface

Reuse `CruiseCabinCaptureStatus` where appropriate. Exact names may follow
existing conventions. Enforce ten candidates, 65,536 payload characters, 4,000
reference characters and 16 missing fields. TUI DTOs, JSON/DOM types and
provider enums stay in Infrastructure/Avalonia.

The old single interface may be removed or replaced because it has no
production consumer; update its focused tests. Do not preserve a misleading
contract solely for test compatibility. Capture returns review evidence only
and never calls repositories, recording, Saved Cruises or alert evaluation.

---

## Fixed Browser Payload

Extend `TuiCruiseCaptureScript` instead of adding a second DOM pass. Preserve
all Prompt 037k price capture and its exact two card structures:

```text
[data-testid="product-card"]
section.ResultListItem__cruiseResultItem
```

For each recognized card return a small structured cabin-evidence value only
when card-local visible text contains the demonstrated combination. Matching
may tolerate case and normal whitespace but must require quantity `1`, exact
`Inside Cabin` and `Cheapest available` in that card. Do not match a bare word
`inside`, marketing copy or text outside the card. Return normalized label,
quantity and qualifier, not whole-card text.

Do not map Outside, Balcony, Suite or Solo until exact current retailer wording
is demonstrated. Conflicting/unfamiliar labels remain controlled non-evidence.

Create an explicitly versioned additive payload shape. Prefer retaining
version-1 price fixture support while allowing the price adapter to accept the
new additive version. Cabin capture supports only the new version. Unknown
versions are Unsupported. Added cabin fields must never make an otherwise
valid price candidate fail.

---

## TUI Infrastructure Mapping

Implement and register the provider adapter through the existing TUI service
extension.

### Trust

Require the established TUI source identifier/retail source and validate:

- current page: absolute HTTPS, `www.tui.co.uk`, `/cruise/packages`
- candidate: absolute HTTPS, `www.tui.co.uk`,
  `/cruise/bookitineraries/...`
- `itineraryCodeOne` or `itineraryCode` is present

Reject foreign hosts, HTTP, lookalike paths, missing identity and oversized
values. Never replace an untrusted candidate reference with the page URL.

### Sailing identity

Create `CruiseSailingKey` using operator id `marella`, demonstrated normalized
Marella ship name, `sailingDate` and positive `cruiseDuration` from the trusted
candidate link. Missing/malformed values are Incomplete. Never use search
`when` as sailing date. A cabin observation does not require a price.

### Search context

Parse only explicit recognized values from the validated current page:

- `noOfAdults` and `noOfChildren`: known only with one valid in-range value
- `childrenAge`: known only with a complete valid ordered set matching known
  child count; zero known children means known empty ages
- `from[]`: airport id before `:Airport`, only with one recognized value
- package mode: `FlyCruise` only for the packages page with recognized airport;
  otherwise `Unknown`
- cabin quantity: explicit supported card quantity (`1`); empty `room` is not
  evidence

Do not infer two adults, Cruise Only, Cruise and Stay, child ages, quantity or a
default airport. `addAStay=0`, `searchType` and similar control values do not
independently prove a Core mode. Ambiguous duplicate/conflicting values remain
unknown where representable; reject only invalid combinations. Different
context fingerprints create independent series.

### Observation

Map supported evidence to Inside Available, all other existing cabin types
Unknown and Partial coverage. Use the request evidence time and trusted
itinerary reference. Build a deterministic bounded versioned evidence key from
stable normalized retailer evidence and candidate identity, never observation
time, raw payload, whole-card text or session data.

Return no observation for absent cabin wording, `All gone`, ambiguous labels,
missing sailing identity or failed trust. Use Incomplete for missing required
demonstrated fields, Unsupported for well-formed unmapped evidence, Failed for
malformed/unsafe payload and Cancelled when requested. One non-ready candidate
must not erase valid siblings; mixed batches remain reviewable.

Never manufacture category-specific Unavailable states from omission, `All
gone` or an unvisited chooser.

---

## Bounds, Determinism and Safety

Preserve three open shadow roots, 100 scanned recognized links, ten unique
candidates, deterministic page order, URL deduplication before the candidate
limit, 512-character fields and 4,000-character references. Reject an
over-limit serialized payload/collection safely. `WasTruncated` means unique
supported loaded cards beyond ten, not TUI's headline count. Honor cancellation
before parsing, between candidates and before return.

The script must not click, focus, submit, scroll, mutate, select cabins, progress
booking, call network/private endpoints, read cookies/storage/history/referrer,
return HTML/full body text, inspect closed roots/frames, execute page script or
transmit data. Do not add generic article/list/nearest-parent/page fallbacks.

---

## Required Offline Tests

Use frozen fixtures only. Cover:

- exact modern Inside phrase -> Inside Available, four Unknown, Partial
- card quantity one, explicit adults/zero children/known-empty ages/STN/Fly
  Cruise context
- absent, bare `inside`, missing qualifier, conflicting/unfamiliar evidence
- `All gone` creates no Unavailable state
- malformed, duplicate and out-of-range context values
- search `when` never replaces itinerary `sailingDate`
- missing/invalid ship, sailing date or duration remains Incomplete
- trusted page/candidate host, path, scheme and identity enforcement
- deterministic evidence key; repeated evidence identity
- different occupancy/airport context creates a different series
- mixed outcomes, duplicate candidates, deterministic order and truncation
- malformed, oversized, unknown-version payload and cancellation
- version-1 and additive-version price capture compatibility
- shadow-DOM and modern light-DOM price regressions
- cabin adapter dependency registration
- script selectors, bounds and prohibited-operation audit

If the harness cannot execute DOM extraction, use exact script audits plus
representative new-version JSON through the real adapter. Do not add a second
production C# DOM parser or any live browser/network test. Never use Robin's
database.

---

## Manual Acceptance

1. Load the supplied or an equivalent current TUI Cruise Packages page.
2. Confirm a card visibly contains the supported Inside wording.
3. Run fixed capture without navigating or changing the page.
4. Verify Inside Available, four Unknown and Partial for that card.
5. Verify sailing date comes from the itinerary link and context matches the
   visible search, including cabin quantity one.
6. Verify a card without supported wording produces no observation.
7. Verify existing price Capture Current Page behavior is unchanged.
8. Verify no recording, navigation, clicking or booking action occurs.

Manual acceptance may wait for 040e/040f review/record UI, but focused tests
must make the adapter payload inspectable now.

---

## Allowed Changes

```text
KrytenAssist.Application/Cruises/*CabinCapture*.cs
KrytenAssist.Infrastructure/Cruises/Tui/*
KrytenAssist.Avalonia/Cruises/Discovery/TuiCruiseCaptureScript.cs
KrytenAssist.Application.Tests/Cruises/*Cabin*.cs
KrytenAssist.Avalonia.Tests/Cruises/Tui/*
KrytenAssist.Avalonia.Tests/ViewModels/CruiseCaptureReviewViewModelTests.cs
KrytenAssist.Avalonia.Tests/Fixtures/Cruises/Tui/*
docs/Codex Prompts/040d - TUI Cabin Evidence Capture.md
docs/AI Playbook/040 - Cabin Availability.md
docs/Roadmap.md
```

UI files may change only to preserve existing price payload compatibility. Do
not add cabin buttons/history/alert wiring. Do not change SQLite or migrate.

---

## Exclusions

- cabin-selection/booking-page capture
- undemonstrated category mappings or category-specific Unavailable mapping
- automated navigation, passenger submission or booking progression
- private API reverse engineering, generic scraping or another retailer
- background monitoring or automatic recording
- preference/Saved Criteria/alert evaluation or materialization
- Cabin Availability review/history UI
- schema changes, live browser/network tests or production database access

---

## Verification

Run focused Application/TUI cabin and price-capture tests, then:

```text
dotnet build KrytenAssist.sln --no-restore
dotnet test KrytenAssist.sln --no-build --no-restore
git diff --check
```

Use the established single-worker runner where required. Resolve warnings
introduced by this step; existing SQLitePCLRaw advisories may remain documented.

---

## Results

Implementation and automated verification completed on 19 July 2026.

### Status

Complete.

### Implementation

- Added an Application-owned bounded cabin page/batch capture contract with
  controlled per-candidate outcomes.
- Extended the fixed TUI script to payload version 2 with exact card-local
  `1 x Inside Cabin` plus `Cheapest available` evidence.
- Preserved version-1 and version-2 price adapter compatibility.
- Added a trusted TUI cabin adapter that maps Inside Available, four Unknown and
  Partial coverage for explicit occupancy/airport/package/cabin context.
- Kept missing or unfamiliar wording as controlled non-evidence and retained
  `All gone` outside category availability mapping.
- Registered the stateless adapter through the existing TUI extension.
- Added offline mapping, context, trust, deduplication, cancellation, DI and
  fixed-script regression tests.

No cabin observation is recorded by this step. Prompt 040e owns the explicit
recording and preference-evaluation boundary.

### Build and Tests

- Solution build passed with 0 errors.
- Core: 147 passed.
- Avalonia/Application/Infrastructure: 542 passed.
- API: 9 passed.
- Total: 698 passed, 0 failed, 0 skipped.
- Existing SQLitePCLRaw advisory warnings remain unchanged.
