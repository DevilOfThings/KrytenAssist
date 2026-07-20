# Prompt 041 – New Itinerary Detection

## Goal

Allow Robin to compare deliberately captured Marella discovery evidence and
identify itineraries that Kryten has not observed in an earlier comparable
capture.

Prompt 041 must not claim that TUI has just published an itinerary unless the
trusted source explicitly supplies publication evidence. With the evidence
currently demonstrated by Prompt 036, the honest product meaning is:

> First observed by Kryten in a later explicit capture of the same discovery
> scope.

The architecture must remain provider-independent and support future cruise
operators and trusted retail sources.

---

## Why This Prompt Exists

Prompts 036 and 037 established deliberate browser-assisted discovery,
provider-independent cruise observations and durable price History. Prompt 039
added durable in-app alerts. Prompt 040 added contextual cabin evidence.

Those features preserve individual sailings and changes to them, but they do
not preserve a comparable catalogue view or distinguish a newly encountered
itinerary from a changed offer for an itinerary already known to Kryten.

Prompt 041 adds that missing positive-evidence boundary. It does not add a
scheduler, crawler, background browser or claim of complete retailer inventory.

---

## Evidence Contract

### Detection Meaning

Initial detection is `FirstObserved`, not proven publication.

An itinerary is FirstObserved only when:

1. Robin explicitly captures a supported discovery page;
2. a prior accepted baseline exists for the same compatible discovery scope;
3. the current candidate has a stable provider itinerary identity; and
4. that identity has never previously been accepted in that provider/source
   catalogue.

The first accepted capture seeds the baseline and creates no New Itinerary
event. This avoids presenting every existing itinerary as newly published.

If a future provider supplies trustworthy publication time or an explicit
published-since feed, the provider adapter may map that separate evidence
without changing the application contract. It must not be inferred from first
observation time.

### Itinerary Versus Sailing

An itinerary is the operator's stable voyage/route definition. A sailing is a
dated departure of that itinerary.

Initial identity is:

```text
CruiseItineraryKey
  operator id
  provider itinerary id
```

For the demonstrated TUI source, the provider itinerary id comes from the
trusted `itineraryCodeOne` or `itineraryCode` URL parameter. `packageId`, price,
promotion, departure date and retailer URL are occurrence evidence, not
itinerary identity.

Do not synthesize identity from title, ship, ports or itinerary-summary text.
A candidate without a trusted stable itinerary id may remain usable by existing
price capture, but is ineligible for New Itinerary detection and must say why.

Different dated sailings with the same provider itinerary id describe one
itinerary. A reused provider id with contradictory operator evidence is rejected
rather than silently merged.

### Discovery Scope

Captures compare only inside a normalized provider-independent discovery scope.
The scope contains:

- trusted retail source
- cruise operator/provider
- source surface or adapter kind
- every material search filter explicitly evidenced by the page/address
- explicit unknown markers for material filters that cannot be recovered
- capture contract/version where compatibility requires it

Adapters own provider URL parsing. Raw URLs and provider query names must not
leak into Core/Application identity types.

Different known filters, or known versus unknown material filters, form
different scopes. Cosmetic, tracking and ordering query values do not.

### Bounded and Partial Evidence

The TUI capture script is currently bounded to ten candidates and can report
truncation. Therefore a capture is positive evidence only:

- a present, valid candidate may be recorded as observed;
- absence never proves withdrawal, cancellation or unpublication;
- disappearance creates no event and deletes nothing;
- a truncated capture may discover a genuinely unseen identity, but the UI
  must say `first observed by Kryten`, never `newly published by TUI`;
- malformed, unsupported or wholly failed capture does not advance a baseline;
- mixed captures accept valid candidates and report rejected candidates
  explicitly.

Changing sort order can expose an older itinerary that was outside a previous
bounded result. This is still first observed by Kryten, not publication
evidence.

### Time

`ObservedAt` comes from the application clock/runtime context at explicit
capture time. It is not retailer publication time and must not be presented as
such.

---

## Workflow

```text
Open trusted discovery page manually
        ↓
Capture supported itinerary candidates
        ↓
Review scope, bounds and eligible/rejected candidates
        ↓
Record Discovery Check explicitly
        ↓
Seed baseline or identify first-observed itineraries
        ↓
Review local New Itineraries history / in-app alerts
```

Capture and review never mutate the itinerary catalogue. Only `Record Discovery
Check` may persist the accepted check and materialize detections.

Recording the factual discovery check commits independently before derived
alerts. Alert failure must not roll back or falsely fail the recorded check.

---

## Retention and Deduplication

- Discovery checks and accepted itinerary identities are durable local factual
  evidence.
- First-seen and last-seen times are application observation times.
- Repeated captures update last-seen evidence without creating another event.
- An itinerary disappearing and later reappearing does not create another
  FirstObserved event in Prompt 041.
- A stable deterministic event key prevents retry and concurrency duplicates.
- No permanent-delete UI or automatic expiry is introduced.
- Discovery evidence is physically independent from price History, Saved
  Cruises, cabin evidence and alerts.
- Removing or changing personal state never removes discovery evidence.

---

## Alerts and Presentation

Prompt 041 extends the provider-independent in-app alert model with a distinct
`NewItinerary` type. It is enabled through the existing Alert Settings pattern
and uses typed details containing the itinerary key, first-observed evidence,
scope identity and trusted source reference.

The user-facing title should prefer `New itinerary observed`. Supporting text
must explain `First observed by Kryten on …` and identify the captured source
and scope. Do not say `published`, `released today`, `live`, or `currently
available` without explicit evidence for that statement.

A local New Itineraries presentation should provide:

- newest-first first-observed entries
- operator itinerary id and bounded display details
- first-seen and last-seen times
- source/scope explanation
- trusted revisit action
- clear seeded-baseline, empty, partial, truncated and failure states

Prompt 042 owns the combined Cruise Dashboard. Prompt 041 must not implement
that dashboard.

---

## Provider Boundary

Core owns itinerary identity, discovery-scope value models, accepted-check
semantics and pure first-observed detection.

Application owns capture/check/repository contracts, controlled results,
record-then-alert orchestration and queries.

Infrastructure owns SQLite persistence and TUI/provider-specific mapping.

Avalonia owns the existing fixed read-only browser script and MVVM workflow.
The script may read the currently displayed trusted page only. It must not
navigate automatically, click, paginate, scroll, submit, call private
endpoints, inspect cookies/storage or run in the background.

Future providers add adapters behind application-owned contracts. Provider SDK,
DOM and URL types never cross into Core/Application.

---

## Implementation Plan

### 041a – Experience and Evidence Contract

- agree FirstObserved versus published language
- agree stable itinerary identity and discovery-scope compatibility
- agree baseline, truncation, partial-failure and reappearance behaviour
- agree alert, retention and presentation boundaries
- make documentation changes only

### 041b – Itinerary Domain and Application Contracts

- add provider-independent itinerary/scope/check/evidence models
- add pure first-observed detector and deterministic identities
- add Application-owned repositories, queries and controlled results
- add comprehensive deterministic Core/Application tests

### 041c – SQLite Discovery Persistence

- add normalized independent catalogue, check and occurrence persistence
- add transactional first/last-seen and idempotent/concurrent recording
- add migration, restart and deletion-boundary coverage
- register adapters through Infrastructure dependency injection

### 041d – Trusted TUI Itinerary Capture

- extend the existing fixed payload with demonstrated itinerary-code and scope
  evidence without breaking price/cabin capture
- map TUI-specific evidence behind the application capture contract
- preserve strict trust, bounds and read-only behaviour
- test only offline fixtures

### 041e – Recording and Alert Integration

- add explicit Record Discovery Check orchestration
- seed the first scope baseline without events
- detect and materialize later first-observed events after commit
- add typed New Itinerary alerts, settings and retry-safe deduplication
- preserve successful factual recording if alert evaluation fails

### 041f – New Itineraries Presentation

- add the local MVVM review/history experience
- present honest scope, evidence time, baseline and truncation language
- add trusted revisit and alert presentation/settings
- preserve existing Cruise modes and defer the combined dashboard

### 041g – Tests and Verification

- audit architecture, script safety, schema independence and composition
- run the full offline solution suite
- complete manual desktop verification
- update this playbook, Roadmap and session handover

---

## Required Acceptance Scenarios

1. First accepted complete capture seeds a baseline and creates no event.
2. First accepted truncated capture seeds a bounded baseline and says so.
3. A later comparable capture contains one never-seen stable itinerary id.
4. A later capture contains a new dated sailing for an already-known itinerary.
5. Price, promotion or source-reference changes for a known itinerary.
6. Same itinerary appears twice in one capture.
7. Same itinerary appears in two compatible discovery scopes.
8. Different known filters or known-versus-unknown filters are captured.
9. Candidate has no trusted itinerary code.
10. Capture contains valid and invalid candidates.
11. Capture is unsupported, cancelled or wholly failed.
12. Previously seen itinerary disappears, then later reappears.
13. Retry and concurrent recording see the same new identity.
14. Discovery check commits but alert persistence fails.
15. Saved Cruise, price History or cabin history is removed.
16. Application restarts before the next comparable capture.

---

## Exclusions

- unattended, scheduled or background browsing
- retailer/API polling, feeds or private endpoint inspection
- claims of publication time without explicit source evidence
- withdrawal, cancellation or sold-out detection from absence
- automatic pagination, scrolling or booking navigation
- fuzzy identity derived from titles, ports or route text
- itinerary recommendations, ranking or AI inference
- email, SMS, push or OS notifications
- cloud sync
- the combined Prompt 042 Cruise Dashboard

---

## Results

### Status

Prompt 041a is complete. The accepted contract treats an itinerary as the
operator's stable route definition and distinguishes it from dated sailings and
mutable retail offers. Detection means first observed by Kryten after a
compatible explicit baseline, not proven retailer publication. Prompt 041b is
complete: provider-independent stable route/catalogue identities, semantic
discovery scopes, positive occurrence/check evidence, pure first-observed
detection and Application capture/repository/query/use-case contracts are
implemented. Prompt 041c is also complete. Migration
`20260719214241_AddCruiseDiscoveryPersistence` adds normalized independent
scope, criterion/value, check, occurrence, rejection and catalogue storage.
Atomic recording seeds baselines, confirms later first-observed events,
preserves exact retries and prevents concurrent double-detection. Baseline
catalogue entries correctly have no event key. Strict reconstruction verifies
all stable identities and timestamp companions. Prompt 041d is also complete.
The bounded read-only script now emits payload v3 with
an explicit trusted provider itinerary id. A stateless TUI adapter maps the
demonstrated package-page query to semantic scope criteria, validates route ids
against trusted itinerary URLs and deduplicates multiple offers for one stable
route. Price v1–v3 and cabin v2–v3 compatibility is preserved. Prompt 041e is
also complete. Alerts now use a closed sailing-or-itinerary subject contract,
so a confirmed route is never represented by a fabricated sailing. Explicit
discovery recording commits first, then independently evaluates deterministic
typed New Itinerary alerts; retries repair missing derived alerts without
duplicating them. Migration `20260720185813_AddNewItineraryAlertIntegration`
preserves existing alerts/settings and adds route subjects, normalized typed
details and the default-enabled setting. All 748 offline tests pass; Prompt
041f is next.

### Existing-System Findings

- The current trusted TUI capture already extracts `itineraryCodeOne` or
  `itineraryCode` from bounded itinerary URLs, but folds it into the more
  general offer id and does not preserve discovery checks.
- Existing Cruise History is keyed by dated sailing plus retail source. It is
  suitable price evidence but cannot prove an itinerary was newly published.
- Current capture is explicitly invoked, bounded to ten candidates and reports
  truncation. This supports positive first-observed evidence only.
- Existing alerts provide the correct durable in-app lifecycle and independent
  post-recording materialization pattern for a new typed alert.
- No demonstrated source field currently proves retailer publication time.

### 041b Analysis

- Stable itinerary identity remains smaller than sailing/offer identity:
  normalized operator id plus opaque trusted provider itinerary id.
- A separate catalogue partition combines that key with retail source id, so
  future retailers do not collide while mutable source display names remain
  outside identity.
- Discovery scope needs provider-independent semantic known/unknown criteria;
  raw TUI query names and addresses remain adapter concerns.
- Accepted checks contain positive occurrence evidence only. The repository
  contract must atomically seed baselines, update first/last seen catalogue
  state and confirm first-observed events so concurrency cannot double-detect.
- Pure detection produces a route-based first-observed domain event. The current
  alert aggregate is sailing-based, so 041b must not invent a representative
  sailing or prematurely change persistence. Prompt 041e owns the typed alert
  subject/materialization extension.
- Application capture and repository contracts can be implemented and tested
  offline before SQLite and TUI adapters exist.

### 041b Results

- Stable route identity and mutable occurrence/check identity are separate,
  versioned and culture-independent.
- Discovery criteria preserve known/unknown values and normalize input ordering
  without exposing provider query names.
- Baseline seeding, known-route offer changes, source partitioning, truncation
  and deterministic new-event ordering are covered by pure tests.
- Application owns the future atomic record contract and controlled recording,
  list, detail and recent-check results.
- The pure detector is registered immediately. Repository-dependent use cases
  wait for Prompt 041c's adapter so generic API composition remains valid.
- The solution builds with zero errors and all 729 offline tests pass.

### 041c Analysis

- Normalized persistence needs separate scope, semantic criterion/value, check,
  occurrence, rejection and source-catalogue tables. Checks retain only positive
  evidence and bounded review diagnostics.
- Scope-row creation and the first accepted check form one transaction; a
  committed scope row is the durable baseline marker.
- Catalogue rows reference retained first/latest occurrences. UTC companions
  provide deterministic ordering while original timestamp offsets round-trip.
- The 041b catalogue projection needs one focused correction: baseline entries
  have no first-observed event, so `FirstObservedEventKey` must be nullable and
  the first-observed list filters null values.
- Atomic repository recording, rather than optimistic Application detection,
  must own baseline seeding, catalogue insertion and event confirmation so
  retries/concurrency cannot double-detect.
- Discovery storage remains physically independent from History, personal
  state, alerts and cabin evidence. No TUI or alert integration belongs in
  041c.

### 041c Results

- The generated migration creates seven discovery-only normalized tables with
  internal cascades and restricted catalogue occurrence references.
- Repository recording is one transaction with bounded SQLite contention and
  uniqueness retries.
- First/latest evidence survives restart with original offsets; deterministic
  UTC/fingerprint ordering prevents regression from older checks.
- Baseline, known-only, later-discovery, retry, migration/schema and concurrent
  same-identity behavior are covered by isolated SQLite tests.
- Infrastructure supplies the repository and Application now registers the
  dependent recording/query use cases without breaking API composition.
- EF reports no pending model changes; the solution builds and all 734 offline
  tests pass.

### 041d Analysis

- Payload version 3 should expose the itinerary-code parameter explicitly
  instead of treating package/offer identity as stable route identity.
- Scope mapping belongs in the trusted adapter. The demonstrated Prompt 037k
  URL supplies a concrete material-query allowlist and semantic known/unknown
  values; unknown keys remain potentially material and make capture incomplete.
- Sort/view and explicit tracking keys are non-material and do not affect scope.
- Multiple sailing/package cards for one itinerary deduplicate by stable
  catalogue identity without changing price capture's offer behavior.
- Price stays compatible with payload v1–v3, cabin with v2–v3, while itinerary
  discovery requires v3.
- The adapter remains read-only, bounded, offline-tested and separate from 041e
  recording/alerts and 041f presentation.

### 041d Results

- Existing shadow/light DOM structures and capture bounds are unchanged.
- Payload v3 adds only explicit itinerary identity; no publication or hidden
  page evidence is inferred.
- The exact demonstrated search parameters map to 16 semantic known/unknown
  criteria. Unknown query keys cannot silently merge scopes.
- Stable-route deduplication is isolated from existing price offer handling.
- TUI DI resolves price, cabin and itinerary adapters, and the complete suite
  passes with 743 offline tests.

### 041e Analysis

- Existing alerts are strictly sailing-based, but a New Itinerary event is a
  stable route/source-catalogue fact. The alert aggregate therefore needs a
  closed sailing-or-itinerary subject model; manufacturing a representative
  ship, date or duration would contradict the accepted 041a contract.
- The four existing sailing alert event keys must remain byte-for-byte stable.
  New Itinerary receives a separate route-based canonical event-key path using
  the confirmed discovery event key, so retries and concurrent materialization
  remain safe without rewriting existing identities.
- Discovery recording is the primary factual mutation and already returns the
  events atomically confirmed by SQLite. Alert evaluation must happen only
  after that commit and expose a separate outcome; alert/settings failure must
  not make a committed check appear to have failed.
- `AlreadyRecorded` must return and reevaluate its originally confirmed events.
  This repairs a prior derived-alert failure safely while the alert repository's
  unique event key prevents duplicates.
- Alert SQLite storage needs one compatibility migration: preserve legacy rows,
  represent exactly one complete sailing or itinerary subject, add typed New
  Itinerary details and extend settings with a default-enabled future-evaluation
  switch. No discovery-to-alert foreign key is appropriate.
- Prompt 041e stops at Core/Application/Infrastructure integration. Avalonia
  recording controls, New Itineraries/Alert Centre presentation and the visible
  settings switch belong to 041f.

### 041e Results

- A closed alert subject hierarchy preserves sailing identity for the four
  existing alert types and adds source-catalogue route identity for New
  Itinerary without changing legacy event-key calculation.
- Confirmed first-observed events map to bounded typed alert details and a
  separate deterministic route event key. Exact-check replay safely repairs a
  prior alert failure through existing unique-key materialization.
- `RecordCruiseDiscoveryCheckAndEvaluateAlerts` exposes factual recording and
  derived alert outcomes independently. Baseline/known-only checks do no alert
  work, disabled settings are explicit, and alert failure cannot falsify a
  committed discovery result.
- SQLite stores exactly one sailing or itinerary subject, one matching typed
  detail, and the new setting while retaining schema independence from factual
  discovery evidence. Legacy migration coverage confirms alert event keys and
  prior setting choices survive.
- The current Alert Centre renders a safe minimal New Itinerary item and hidden
  alert flags survive existing settings saves. Full controls, filters and New
  Itineraries presentation remain 041f work.
- EF reports no pending model changes. The solution builds with zero errors;
  157 Core, 582 Avalonia/Application/Infrastructure and 9 API tests pass.

### Agreed 041a Decisions

- Stable identity is operator id plus trusted provider itinerary id. For TUI,
  this is `itineraryCodeOne`/`itineraryCode` from the validated source URL.
- Package id, sailing date, ship, price, promotion and source-reference changes
  never create a new itinerary identity.
- First accepted capture seeds a scope baseline without alerts. Later accepted
  comparable captures may detect only identities never accepted previously in
  that provider/source catalogue.
- Scope contains source, operator, surface and explicitly known material search
  filters. Different or unknown material filters remain separate.
- Bounded/truncated capture is positive evidence only. Absence, disappearance
  and reappearance never imply publication, withdrawal or another first-seen
  event.
- Explicit Record Discovery Check commits factual evidence before a separate
  typed in-app New Itinerary alert is evaluated.
- Discovery evidence remains durable and independent from price History, Saved
  Cruises, cabin evidence and alerts.
- Presentation uses `New itinerary observed` and `First observed by Kryten`,
  with explicit baseline, partial and truncation states.

### Build and Tests

Not run. This analysis changed documentation only.
