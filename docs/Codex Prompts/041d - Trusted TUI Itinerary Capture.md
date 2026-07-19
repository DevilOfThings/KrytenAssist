# Codex Prompt 041d – Trusted TUI Itinerary Capture

## Implementation Prompt

Implement **Step 041d only** from:

```text
docs/AI Playbook/041 - New Itinerary Detection.md
```

Prompts 041a–041c are complete. Extend the existing fixed read-only TUI payload
and add a trusted adapter mapping displayed itinerary and discovery-scope
evidence to the provider-independent 041b capture contracts. Do not add a
production recording trigger, alerts or UI.

---

## Required Reading

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/036 - Cruise Discovery and Capture.md`
5. `docs/AI Playbook/037 - Cruise History and Price Tracking.md`
6. `docs/AI Playbook/040 - Cabin Availability.md`
7. `docs/AI Playbook/041 - New Itinerary Detection.md`
8. Codex Prompts 041a–041c
9. current fixed TUI script, price/cabin adapters, service registration,
   fixtures and safety/compatibility tests

---

## Acquisition Boundary

Support only the manually loaded trusted page:

```text
https://www.tui.co.uk/cruise/packages
```

with the exact HTTPS host checks used by existing TUI capture. Read only the
currently displayed bounded cards. Do not navigate, click, submit, scroll,
paginate, progress booking, call fetch/XHR/private APIs, read cookies/storage,
extract HTML/body blobs or schedule/background the script. Retain the ten-card
and existing DOM-root/link bounds. Evidence is positive only; absence has no
meaning and truncation remains explicit.

---

## Fixed Payload Version 3

Advance the shared payload from version 2 to 3. Preserve every candidate field
and add:

```text
providerItineraryId
```

Set it from non-empty `itineraryCodeOne`, falling back to non-empty
`itineraryCode`, on the already validated itinerary URL. Never fall back to
`packageId`, path/title text or other fields.

Retain both demonstrated shadow-root and modern light-DOM cards, 3-root,
100-link, 10-candidate, 512-character field and 4,000-character reference
bounds, exact-URL deduplication and `wasTruncated`. Do not add page-query data or
raw card text to the payload.

Compatibility:

- price single/batch capture accepts payload versions 1, 2 and 3
- cabin capture accepts versions 2 and 3
- itinerary capture accepts version 3 only
- equivalent v2/v3 input produces identical price and cabin observations
- unknown future versions remain controlled Unsupported

Do not reinterpret old provider offer ids as itinerary ids.

---

## Trusted TUI Itinerary Adapter

Implement `TuiCruiseItineraryPageCaptureService` behind:

```text
ICruiseItineraryPageCaptureService
```

It consumes only the Application request/result contracts. Provider DTOs, JSON
and URL/query names remain in Infrastructure.

Page validation requires the existing supported source identifier, TUI retail
source, absolute HTTPS, exact `www.tui.co.uk` host and normalized path exactly
`/cruise/packages`. Other schemes, hosts, paths and lookalike prefixes are
Unsupported.

Payload behavior:

- malformed JSON: Failed
- non-v3: Unsupported
- null/empty candidates: Incomplete
- more than ten: Failed
- cancellation before/during mapping: Cancelled
- preserve truncation and bounded safe messages

---

## Discovery Scope Mapping

Build one scope using the request source, operator `marella`, surface
`CruisePackages` and capture-contract version 3.

Map the demonstrated package-page query keys to provider-independent semantic
criteria:

| TUI key | Semantic criterion |
|---|---|
| `from[]` | `departure-airport` |
| `to[]` | `destination` |
| `when` | `departure-date` |
| `flexibility` | `date-flexibility` |
| `flexibleDays` | `flexible-days` |
| `duration` | `duration` |
| `addAStay` | `add-a-stay` |
| `until` | `end-date` |
| `noOfAdults` | `adult-count` |
| `noOfChildren` | `child-count` |
| `childrenAge` | `child-ages` |
| `room` | `room` |
| `choiceSearch` | `choice-search` |
| `searchRequestType` | `search-request-type` |
| `searchType` | `search-type` |
| `sp` | `single-package` |

Percent-decode safely, trim, normalize and enforce Core bounds. Repeated
material keys become ordered distinct Known values. One non-empty value is
Known. Missing or empty values are explicit Unknown. Never invent defaults.
Input key/value ordering must not affect scope identity. Remove `:Airport` only
by the already demonstrated exact suffix rule.

Ignore only explicit non-material keys: exact `sort`, `view`, `gclid`,
`msclkid`, and names beginning `utm_`. Sort/display order is not scope identity.
Any other non-empty query key may be material: return controlled Incomplete with
missing field `discoveryScope`, naming only the bounded key—not its value.

The exact Prompt 037k demonstrated URL must map successfully.

---

## Candidate Mapping

For each candidate:

1. validate its bounded reference as a trusted TUI itinerary URL
2. extract exactly one non-empty itinerary-code parameter
3. require payload `providerItineraryId` to normalize equal to the URL value
4. create `CruiseItineraryKey("marella", providerItineraryId)`
5. map bounded optional display/sailing evidence without making it identity
6. derive a deterministic versioned SHA-256 provider evidence key
7. return a controlled candidate result

Hash only bounded canonical trusted reference, itinerary id and optional
display/sailing fields. Exclude time, prices, promotions, cabin state and raw
card text.

- missing id: Ineligible with `providerItineraryId`
- payload/URL mismatch or untrusted URL: Failed
- invalid explicit date/duration or oversized field: controlled
  Incomplete/Failed, never silently altered
- absent optional title/ship/date/duration/port/summary/offer id is allowed

### Duplicate Stable Itineraries

Several dated/package cards may represent one route. Deduplicate by normalized
source catalogue identity, not package URL. Choose the representative
deterministically by trusted source reference ordinal, then occurrence
fingerprint. Preserve a bounded Ineligible diagnostic for discarded duplicates
with reason `duplicate itinerary identity` (or an equally explicit contract-
compatible result). Duplicates do not fail the batch.

Do not change the price adapter's exact-URL behavior; price capture still works
with offers/sailings rather than stable routes.

---

## Dependency Injection

Register the stateless adapter through the existing TUI extension:

```text
ICruiseItineraryPageCaptureService -> TuiCruiseItineraryPageCaptureService
```

Existing price and cabin registrations remain unchanged. Do not invoke the
adapter from a ViewModel or call `RecordCruiseDiscoveryCheck` in 041d.

---

## Required Offline Tests

### Fixed Script

- v3 marker and explicit bounded provider itinerary id
- itinerary-code precedence and no package/path/title fallback
- existing DOM structures/bounds retained
- exact no-click/submit/focus/scroll/fetch/XHR/cookie/storage/HTML/navigation
  safety assertions

### Scope

- exact Prompt 037k URL maps every semantic criterion
- missing/empty becomes Unknown
- query/repeated-value ordering is stable
- material value changes fingerprint
- tracking and sort/view do not
- unknown non-empty key is Incomplete
- malformed encoding, oversized/ambiguous values are controlled
- wrong source/scheme/host/path is Unsupported

### Candidates

- both itinerary-code parameter names
- stable operator+itinerary identity despite package/date/price changes
- exact optional evidence/time/reference and deterministic evidence key
- missing/mismatched/oversized id and untrusted URL
- duplicate route across multiple sailing/package cards
- mixed candidate states, truncation, null/empty/too-many/malformed/future
  version and cancellation

### Compatibility and Composition

- price v1/v2/v3 and cabin v2/v3 equivalence
- future versions rejected
- DI resolves price, cabin and itinerary capture abstractions
- no repository mutation, database access or network request

Use deterministic serialized fixtures only. Never load TUI in tests.

---

## Required Documentation Updates

Complete Results, update the Prompt 041 playbook and Roadmap, create a handover
and identify Prompt 041e as next without implementing it.

---

## Exclusions

- production review/recording trigger or repository integration
- New Itinerary alerts/settings/persistence
- Avalonia New Itineraries presentation
- scheduled/background browsing or polling
- navigation, scrolling, pagination or booking actions
- publication/disappearance/withdrawal inference
- new operators, retailers or TUI surfaces
- Prompt 042 Dashboard

---

## Results

### Status

Complete on 19 July 2026. Payload version 3, trusted TUI itinerary/scope mapping,
stable-route deduplication and DI composition are implemented without adding a
recording trigger, alert or UI.

### Files Modified

- `KrytenAssist.Avalonia/Cruises/Discovery/TuiCruiseCaptureScript.cs`
- `KrytenAssist.Infrastructure/Cruises/Tui/TuiCruiseItineraryPageCaptureService.cs`
- `KrytenAssist.Infrastructure/Cruises/Tui/TuiCruisePageCaptureService.cs`
- `KrytenAssist.Infrastructure/Cruises/Tui/TuiCruiseCabinPageCaptureService.cs`
- `KrytenAssist.Infrastructure/Cruises/Tui/TuiCruiseCaptureServiceCollectionExtensions.cs`
- `KrytenAssist.Avalonia.Tests/Cruises/Tui/TuiCruiseItineraryPageCaptureServiceTests.cs`
- `KrytenAssist.Avalonia.Tests/Cruises/Tui/TuiCruisePageBatchCaptureServiceTests.cs`
- `KrytenAssist.Avalonia.Tests/Cruises/Tui/TuiCruisePageCaptureServiceTests.cs`
- `KrytenAssist.Avalonia.Tests/Cruises/Tui/TuiCruiseCaptureDependencyInjectionTests.cs`
- `KrytenAssist.Avalonia.Tests/ViewModels/CruiseCaptureReviewViewModelTests.cs`
- this prompt, Prompt 041 playbook, Roadmap and Session Handover 035

### Build and Tests

- focused TUI itinerary/compatibility/safety tests: 50 passed
- solution build: passed with 0 errors and five existing `SQLitePCLRaw`
  advisory warnings
- Core: 155 passed
- Avalonia/Application/Infrastructure: 579 passed
- API: 9 passed
- total: 743 passed, 0 failed, 0 skipped
- `git diff --check`: passed

### Implementation Notes

- The fixed script emits payload v3 with `providerItineraryId` taken only from
  trusted itinerary-code URL parameters; all previous bounds and read-only
  safety assertions remain intact.
- Price capture accepts v1–v3 and cabin capture accepts v2–v3.
- The itinerary adapter requires v3 and maps the demonstrated 16 material query
  fields to semantic known/unknown criteria.
- Sort/view and explicit tracking keys are ignored; an unknown non-empty query
  key returns controlled Incomplete scope capture.
- Candidate identity is verified against its trusted URL. Multiple package or
  sailing cards for one stable route produce one Ready occurrence plus explicit
  duplicate diagnostics.

### Next

Prompt 041e – Recording and Alert Integration.
