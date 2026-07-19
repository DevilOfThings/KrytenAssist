# Codex Prompt 041b – Itinerary Domain and Application Contracts

## Implementation Prompt

Implement **Step 041b only** from:

```text
docs/AI Playbook/041 - New Itinerary Detection.md
```

Prompt 041a is complete. This step introduces provider-independent itinerary,
discovery-scope/check/evidence models, pure first-observed policy and
Application-owned capture, recording and query contracts. Do not implement
SQLite, TUI mapping, alert persistence/materialization or Avalonia UI yet.

---

## Required Reading

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/036 - Cruise Discovery and Capture.md`
5. `docs/AI Playbook/037 - Cruise History and Price Tracking.md`
6. `docs/AI Playbook/039 - Price Drop Alerts.md`
7. `docs/AI Playbook/040 - Cabin Availability.md`
8. `docs/AI Playbook/041 - New Itinerary Detection.md`
9. `docs/Codex Prompts/041a - New Itinerary Experience and Evidence Contract.md`
10. existing Cruise identity/fingerprint, capture result, repository result,
    clock and alert conventions and tests

---

## Stable Itinerary Identity

Introduce an immutable Core-owned `CruiseItineraryKey` or consistently named
equivalent containing exactly:

```text
normalized operator id
normalized trusted provider itinerary id
```

Requirements:

- values are required, trimmed, bounded and normalized using Core-owned rules
- equality is ordinal and deterministic
- provide a stable versioned fingerprint/persistence key using invariant
  canonical formatting and lowercase SHA-256 (or the established equivalent)
- do not use `GetHashCode`, current culture or provider display names
- do not include retail source, package/offer id, ship, sailing date, title,
  route text, price, promotion or URL
- no fuzzy identity factory belongs in Core/Application

The provider itinerary id is opaque to Core. Prompt 041d owns extracting and
validating TUI `itineraryCodeOne`/`itineraryCode` before construction.

Introduce a separate catalogue partition identity combining normalized retail
source id with `CruiseItineraryKey`. This allows provider-independent storage
to distinguish evidence supplied by different retailers without corrupting the
stable operator itinerary identity. Retail source display name is excluded.

---

## Discovery Scope

Introduce an immutable Core-owned discovery scope containing:

- required `CruiseSource`
- required normalized operator id
- a provider-independent discovery surface, initially `CruisePackages`
- an ordered immutable set of material criteria
- an explicit capture-contract version

Represent each material criterion with a provider-independent semantic name and
an explicit known/unknown state. A known criterion contains one or more bounded,
normalized values; unknown contains no invented default.

Provider query names and raw URLs must not enter Core. Prompt 041d maps trusted
provider query data to semantic criteria. Tracking, sort, analytics and cosmetic
values are not criteria.

Scope invariants:

- source/operator/surface/version are required and bounded
- semantic criterion names are required, normalized and unique
- known criteria contain a non-empty ordered distinct value set
- unknown criteria contain no values
- collections are defensively copied and exposed read-only
- ordering of input criteria/values does not affect identity
- different known values and known-versus-unknown values produce different
  scope fingerprints
- source display-name changes do not affect identity

Create a stable versioned scope fingerprint from the canonical values. Do not
persist or hash an entire raw address as the scope identity.

---

## Itinerary Occurrence Evidence

Introduce an immutable provider-independent occurrence representing positive
evidence that one itinerary was present in one explicit discovery capture.

It contains:

- `CruiseItineraryKey`
- required retail `CruiseSource`
- optional bounded display title
- optional bounded ship name
- optional sailing date and positive bounded duration
- optional bounded departure port and itinerary summary
- optional bounded provider offer/package id as evidence only
- required observation time
- required bounded deterministic provider evidence key
- optional bounded trusted source reference as opaque evidence text

Do not include prices, promotions or cabin states in itinerary identity.
Existing observation features continue to own those meanings.

Validation:

- occurrence operator and source must agree with its eventual scope
- optional strings cannot be empty/whitespace and use existing appropriate
  bounds where possible
- duration, when present, is positive and safely bounded
- evidence/source collections are immutable
- provider evidence key and source reference never become identity substitutes

Define a deterministic occurrence fingerprint for retry/check identity. It may
include bounded display/sailing evidence but must exclude capture time and must
remain distinct from the stable itinerary key.

---

## Accepted Discovery Check

Introduce an immutable `CruiseDiscoveryCheck` containing:

- one `CruiseDiscoveryScope`
- application-supplied observed/captured time
- ordered accepted itinerary occurrences
- rejected candidate summaries/count without raw provider exceptions or DOM
- `WasTruncated`
- a deterministic check/evidence key

The check is accepted factual evidence, not an instruction to browse.

Invariants:

- at least one accepted occurrence is required for an accepted check
- every accepted occurrence matches the scope's normalized source/operator
- duplicate catalogue identities within one check are collapsed or rejected
  deterministically; never double-detected
- accepted occurrences have the same evidence time as the check, unless an
  explicitly justified capture contract preserves one authoritative batch time
- rejected summaries are bounded transport-neutral values
- `WasTruncated` is retained but does not change positive occurrence meaning
- check key uses scope fingerprint, observed time, truncation state and sorted
  accepted/rejected bounded identities
- input order does not affect check identity

Do not model disappearance or a complete retailer catalogue. A check contains
only what was positively and safely observed.

---

## Pure First-Observed Policy

Introduce a pure Core policy/detector with no I/O or clock lookup.

Input represents:

- whether the current scope already has an accepted baseline
- catalogue identities already accepted for the same retail source
- one current accepted check

Output distinguishes:

```text
BaselineSeeded
NoNewItineraries
FirstObserved
```

and returns the ordered occurrences whose catalogue identities are newly
observed.

Rules:

- no prior scope baseline: `BaselineSeeded`, zero first-observed events
- prior compatible baseline: each identity absent from the source catalogue is
  FirstObserved exactly once
- a new sailing or changed occurrence fingerprint for a known itinerary is not
  a new itinerary
- duplicates in the check never produce duplicates
- a known itinerary appearing through another compatible scope is not new for
  that retail-source catalogue
- a different source uses a different catalogue partition
- truncation does not suppress positive newly observed identities
- absence, disappearance and reappearance have no transition meaning
- output ordering is deterministic by stable catalogue/itinerary identity

Reject mismatched source/operator data instead of silently comparing it.

Create a provider-independent `CruiseItineraryFirstObservedEvent` (or equivalent
domain fact) containing the catalogue identity, occurrence, scope fingerprint,
check evidence key and observed time. Give it a deterministic event fingerprint
for later alert materialization. Do not add `NewItinerary` to `CruiseAlertType`
or change the sailing-based alert aggregate in 041b; Prompt 041e owns that
separate integration after the persistence contract exists.

---

## Application Capture Contracts

Application owns a transport-neutral itinerary discovery capture abstraction.
Define a request with the trusted current source/address context and an
application-supplied observation time, following existing capture conventions.

Results support a bounded batch and controlled statuses such as:

```text
Ready | Ineligible | Incomplete | Unsupported | Cancelled | Failed
```

The batch reports:

- proposed normalized scope
- accepted occurrence candidates
- per-candidate bounded reasons/missing semantic fields
- truncation
- a safe user-facing message

Requirements:

- cancellation is a result, not a provider exception leak
- provider exceptions, DOM objects, browser objects and provider DTOs do not
  cross the interface
- capture results are review-only and do not persist automatically
- enforce explicit candidate/field/reference bounds consistent with existing
  capture contracts
- no network, browser automation or TUI implementation is added in 041b

---

## Application Persistence Contract

Application owns `ICruiseDiscoveryRepository` or a consistently named
equivalent. Its record operation accepts one reviewed `CruiseDiscoveryCheck`
and promises an atomic confirmed result suitable for concurrent SQLite
implementation in 041c.

The confirmed record result distinguishes:

```text
BaselineSeeded
RecordedNoNewItineraries
RecordedWithFirstObserved
AlreadyRecorded
```

It returns:

- the committed check identity/time/scope
- ordered confirmed first-observed events, if any
- accepted and rejected counts
- truncation

Repository semantics:

- one check key is idempotent across retry
- baseline existence, catalogue insertion, first/last-seen updates and confirmed
  new-event selection form one atomic semantic operation
- concurrency cannot return the same first-observed identity as new twice
- known identities advance last-seen/occurrence evidence without another event
- no deletion operation belongs in 041b
- interfaces expose only Core/Application types and async cancellation

Also define focused deterministic queries for:

- list first-observed itinerary entries newest-first
- get one itinerary catalogue/history entry
- list recent accepted discovery checks when needed for presentation/diagnosis

Queries must carry controlled success/empty/cancelled/failed results rather than
provider exceptions.

---

## Application Use Cases

Introduce focused use cases around the contracts, with constructor injection:

- record one reviewed discovery check
- list first-observed itineraries
- get itinerary details/evidence
- optionally list recent checks if required by the agreed presentation

Recording calls only the repository semantic operation in 041b. It does not
materialize alerts, navigate the browser or mutate price History, Saved Cruises
or cabin evidence. Prompt 041e will wrap confirmed recording with independent
post-commit alert evaluation.

Use cases map cancellation and repository failure to explicit controlled
results. Do not catch programming/validation errors and misreport them as an
ordinary empty result.

Register pure/Application services through the existing Application dependency
injection extension where repository-independent registration is appropriate.
Do not add Infrastructure registrations or service location.

---

## Required Tests

Add deterministic offline tests covering at least:

### Identity and Validation

- itinerary normalization, bounds, equality and stable fingerprint
- package/sailing/display changes excluded from itinerary identity
- source-partitioned catalogue identity
- discovery criteria known/unknown and input-order independence
- scope source/operator/surface/version differences
- occurrence/check bounds, immutability and deterministic fingerprints
- duplicate accepted identity handling

### Pure Detection

- first scope check seeds baseline with no event
- later unseen itinerary returns one stable event
- multiple unseen identities have deterministic order
- new sailing or changed offer evidence for known itinerary returns none
- duplicate identity produces at most one event
- known identity in another compatible scope returns none
- same itinerary under a different retail source uses a separate catalogue
- truncated check still detects present unseen identities
- disappearance/reappearance has no new-event meaning
- source/operator mismatch is rejected

### Application Contracts and Use Cases

- every confirmed repository record state maps correctly
- retry/AlreadyRecorded does not claim a new event
- list/detail/check ordering and empty states
- cancellation and repository failure are controlled
- dependency injection resolves repository-independent services
- no mutation or dependency on History, Saved Cruises, cabin evidence or alerts

Use fakes/in-memory test doubles. Do not add SQLite fixtures, browser tests or
network calls in 041b.

---

## Architectural Constraints

- Core contains no Application, Infrastructure, EF Core, SQLite, Avalonia,
  WebView, AngleSharp, OpenAI or provider dependencies.
- Application contains no Infrastructure, EF Core, SQLite, Avalonia, browser,
  DOM or provider DTO dependencies.
- provider/source-specific mapping remains deferred to 041d
- persistence remains deferred to 041c
- alert aggregate/settings/persistence changes remain deferred to 041e
- UI remains deferred to 041f
- all identities are stable across restart and culture
- runtime context is not persisted as conversation memory; use the injected
  application clock for observation time

---

## Required Documentation Updates

After implementation and verification:

- complete Results below
- update `docs/AI Playbook/041 - New Itinerary Detection.md`
- update `docs/Roadmap.md`
- create a session handover if appropriate
- identify Prompt 041c as next without implementing it

---

## Exclusions

- SQLite entities/configuration/migration
- TUI script or provider adapter implementation
- `NewItinerary` alert type/settings/materialization
- Avalonia ViewModels, views or mode changes
- scheduled/background browsing or network polling
- publication-time inference
- disappearance/withdrawal detection
- fuzzy identity
- Prompt 042 Dashboard work

---

## Results

### Status

Complete on 19 July 2026.

Implemented the provider-independent itinerary discovery domain and Application
contracts without adding SQLite, TUI mapping, alert changes or UI.

### Files Modified

- `KrytenAssist.Core/Cruises/CruiseItineraryDiscovery.cs`
- `KrytenAssist.Application/Abstractions/Persistence/ICruiseDiscoveryRepository.cs`
- `KrytenAssist.Application/Cruises/CruiseDiscoveryContracts.cs`
- `KrytenAssist.Application/Cruises/CruiseDiscoveryUseCases.cs`
- `KrytenAssist.Application/DependencyInjection.cs`
- `KrytenAssist.Core.Tests/Cruises/CruiseItineraryDiscoveryTests.cs`
- `KrytenAssist.Avalonia.Tests/Application/Cruises/CruiseDiscoveryApplicationTests.cs`
- this prompt
- `docs/AI Playbook/041 - New Itinerary Detection.md`
- `docs/Roadmap.md`
- `docs/Session Handovers/2026-07-19 Session 033.md`

### Build and Tests

- focused Core discovery tests: 8 passed
- focused Application discovery tests: 8 passed
- solution build: passed with 0 errors and five existing `SQLitePCLRaw`
  advisory warnings
- Core: 155 passed
- Avalonia/Application/Infrastructure: 565 passed
- API: 9 passed
- total: 729 passed, 0 failed, 0 skipped
- `git diff --check`: passed

### Implementation Notes

- Stable route identity contains only normalized operator id and trusted opaque
  provider itinerary id. Mutable sailing/offer evidence has a separate
  occurrence fingerprint.
- Source-partitioned catalogue and semantic scope fingerprints are versioned,
  invariant SHA-256 identities.
- Checks accept positive bounded evidence only, reject duplicate itinerary
  identities and preserve truncation/rejection diagnostics.
- Pure detection seeds first baselines, returns only globally unseen identities
  for that source catalogue and never treats changed offers as new routes.
- The repository contract owns the future atomic baseline/catalogue/first-seen
  transaction required for concurrency-safe SQLite implementation.
- Only the repository-independent detector is registered in Application DI.
  Repository-dependent use cases remain unregistered until Prompt 041c supplies
  the Infrastructure adapter, preserving API composition.

### Next

Prompt 041c – SQLite Discovery Persistence.
