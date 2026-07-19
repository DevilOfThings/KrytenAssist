# Codex Prompt 041a – New Itinerary Experience and Evidence Contract

## Analysis Prompt

Complete **Step 041a only** from:

```text
docs/AI Playbook/041 - New Itinerary Detection.md
```

This is an analysis and agreement step. Do not change production code, tests,
schema, migrations or UI.

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
9. current cruise offer/sailing/fingerprint models, capture contracts, fixed TUI
   script, SQLite History and alert contracts

---

## Decisions to Confirm

### Honest Detection Language

Confirm that current source evidence supports `First observed by Kryten`, not a
claim that TUI just published the itinerary.

Confirm that the first accepted capture of a discovery scope seeds its baseline
without producing alerts. Later accepted captures may detect identities never
previously accepted in the provider/source catalogue.

### Itinerary Identity

Confirm initial identity:

```text
operator id + trusted provider itinerary id
```

For TUI, use the demonstrated `itineraryCodeOne`/`itineraryCode`, not
`packageId`, title, ship, departure date, price or fuzzy route text.

Confirm that multiple dated sailings of the same itinerary are occurrences of
one itinerary and do not create repeated New Itinerary events.

Confirm that candidates without a stable trusted itinerary id are ineligible
and reported honestly rather than guessed.

### Comparable Discovery Scope

Agree a normalized provider-independent scope containing source, operator,
surface kind and all explicitly known material search filters. Known versus
unknown values, and different known values, remain separate. Tracking, display
order and cosmetic URL values do not define scope.

Provider-specific URL parsing stays inside the adapter.

### Bounded Evidence

Confirm that the ten-candidate limit and truncation marker make absence
non-evidence. No disappearance, withdrawal, cancellation or sold-out event may
be inferred.

Confirm that a newly exposed older result may still be labelled first observed
by Kryten, but never newly published by TUI.

Agree mixed-candidate, unsupported, cancelled and failed-capture behaviour.

### Recording, Retention and Alerts

Confirm explicit:

```text
Capture → Review → Record Discovery Check → Detect → Revisit
```

The factual check commits before derived alerts. Confirm lifetime first-seen
deduplication, reappearance behaviour, retention and physical independence from
price History, Saved Cruises, cabin evidence and alerts.

Confirm a distinct durable in-app `NewItinerary` alert type, enabled through the
existing settings pattern. No external notifications or background capture.

### Presentation

Confirm a local New Itineraries history/review experience with newest-first
entries, first/last seen evidence, scope explanation, trusted revisit and
explicit baseline/partial/truncated/failure states.

Prompt 042 remains responsible for the combined Cruise Dashboard.

---

## Required Analysis Tests

Walk through and record the outcome of every acceptance scenario in the Prompt
041 playbook, including:

- first baseline versus later unseen identity
- new sailing of a known itinerary
- known itinerary with changed offer details
- duplicate identity within/across compatible scopes
- different or incomplete scope filters
- missing itinerary code
- truncated and mixed-result capture
- disappearance and reappearance
- retry, concurrency and post-commit alert failure
- restart and deletion-boundary independence

---

## Required Output

- record Robin's agreed decisions in this prompt
- update the Prompt 041 playbook with the accepted contract
- refine the 041b–041g implementation sequence if required
- update Roadmap status precisely
- identify data limitations and accepted wording explicitly

---

## Exclusions

- production code, schema, migration or UI changes
- scheduled/background browsing or network polling
- automatic navigation, pagination, scrolling or booking actions
- publication-time inference
- disappearance/withdrawal detection
- fuzzy itinerary identity
- new providers or unsupported source surfaces
- Prompt 042 implementation

---

## Results

### Status

Complete on 19 July 2026. The safe evidence, identity, workflow and retention
defaults were accepted as the implementation contract for Prompts 041b–041g.

### Agreed Product Meaning

- An itinerary is the operator's stable route definition. It is not expected to
  change during the lifecycle of a cruise.
- A sailing is a dated departure using an itinerary. An offer is a retailer's
  current package, price, promotion and source reference for a sailing.
- New dates, packages, prices, promotions, cabin states or retailer links for a
  known itinerary do not make it a new itinerary.
- Current trusted TUI evidence supports `First observed by Kryten`, not proof
  that TUI has just published an itinerary.
- User-facing text uses `New itinerary observed` and `First observed by Kryten
  on …`. It does not use `published`, `released today`, `live` or `currently
  available` without separate explicit evidence.

### Agreed Identity

The stable identity is:

```text
operator id + trusted provider itinerary id
```

- For TUI, the provider itinerary id is the demonstrated
  `itineraryCodeOne`/`itineraryCode` from a validated itinerary URL.
- `packageId`, sailing date, ship, title, route text, price, promotion and URL
  are occurrence/display evidence and are not itinerary identity.
- Multiple dated sailings with the same provider itinerary id are occurrences
  of one itinerary.
- Identity is never synthesized from fuzzy text. A candidate without a trusted
  stable itinerary id remains ineligible for detection and reports that reason.
- Contradictory operator evidence for a reused identity is rejected rather than
  silently merged.

### Agreed Scope and Baseline

- Discovery checks compare only within a normalized provider-independent scope
  containing retail source, operator, source surface and explicitly evidenced
  material search filters.
- Different known filters, and known versus unknown material filters, form
  separate scopes. Tracking parameters, result order and cosmetic URL values do
  not.
- TUI URL/query interpretation remains inside the provider adapter.
- The first accepted capture for a scope seeds its baseline without producing
  New Itinerary events, including when that accepted capture is bounded or
  truncated.
- A later comparable accepted capture may create an event only for a stable
  itinerary identity never previously accepted in the provider/source
  catalogue.
- Seeing an already-known itinerary through another compatible scope records
  occurrence evidence but does not create another first-observed event.

### Agreed Bounded-Evidence Behaviour

- Present valid candidates are positive evidence. Absence is never evidence of
  withdrawal, cancellation, sold-out state or unpublication.
- The existing ten-candidate bound and truncation marker are preserved and
  presented explicitly.
- A changed result order may expose an older itinerary that Kryten has not seen.
  It may be recorded as first observed by Kryten, never newly published by TUI.
- Valid candidates in a mixed capture may be accepted while rejected candidates
  retain explicit reasons.
- Unsupported, cancelled, malformed or wholly failed captures do not seed or
  advance a baseline.
- Disappearance creates no event and deletes nothing. Later reappearance of a
  known identity updates last-seen evidence without another event.

### Agreed Workflow, Retention and Alerts

```text
Capture → Review → Record Discovery Check → Detect → Revisit
```

- Capture/review is read-only. Only explicit `Record Discovery Check` persists
  evidence.
- The application clock supplies observation time. This is not retailer
  publication time.
- The factual check commits before alert evaluation. Alert failure cannot roll
  it back or falsely report recording failure.
- Checks, catalogue identities and occurrences are durable local evidence with
  first-seen and last-seen times.
- Deterministic identities prevent retry and concurrent duplicates.
- Discovery evidence has no ownership/cascade relationship with price History,
  Saved Cruises, cabin evidence or alerts.
- Prompt 041 adds a separate durable in-app `NewItinerary` alert type, enabled
  by default through the existing settings pattern. Setting changes affect
  future evaluation only and do not backfill history.
- No background browsing or external notifications are introduced.

### Agreed Presentation

- A local New Itineraries experience presents newest-first first-observed
  entries, stable itinerary identity, bounded display evidence, first/last seen
  times, source/scope explanation and trusted revisit.
- Baseline seeded, no-new-results, partial, truncated, cancelled and failure
  states are explicit.
- Existing Cruise modes remain usable. Prompt 042 retains ownership of the
  combined Cruise Dashboard.

### Scenario Outcomes

1. First accepted complete capture: seed the scope baseline; no event.
2. First accepted truncated capture: seed a bounded baseline, show truncation;
   no event.
3. Later comparable capture with one never-seen stable id: record it and create
   one New Itinerary event/alert when enabled.
4. New dated sailing for a known itinerary: update occurrence/last-seen only.
5. Changed price, promotion, package or source reference: not a new itinerary;
   existing price/promotion processing remains independent.
6. Duplicate identity in one capture: accept once and report/deduplicate the
   duplicate deterministically.
7. Known identity in another compatible scope: record occurrence evidence, no
   additional first-observed event.
8. Different known filters or known-versus-unknown filters: separate scope
   baselines; never compare them as one check.
9. Missing trusted itinerary code: candidate is ineligible with an explicit
   reason; no guessed identity.
10. Mixed valid/invalid capture: record accepted candidates and report rejected
    candidates without converting the whole check to success without detail.
11. Unsupported, cancelled or wholly failed capture: persist no check/baseline
    mutation and create no event.
12. Disappearance then reappearance: absence does nothing; reappearance updates
    last seen and creates no repeat event.
13. Retry/concurrent record of the same identity: one factual first-seen state
    and at most one deterministic alert.
14. Alert persistence failure: discovery check remains committed and the result
    reports the later evaluation failure separately.
15. Saved Cruise, price History or cabin history removal: discovery catalogue
    and checks remain intact; reverse removal also leaves those features intact.
16. Restart before the next capture: persisted baseline/catalogue produces the
    same detection and deduplication outcome.

### Implementation Sequence

- 041b: itinerary domain, pure detection and Application contracts
- 041c: independent normalized SQLite discovery persistence
- 041d: trusted TUI itinerary/scope capture mapping
- 041e: explicit record-then-detect orchestration and New Itinerary alerts
- 041f: local New Itineraries presentation and settings integration
- 041g: architecture, safety, persistence and regression verification

### Build and Tests

Not run. This prompt is documentation-only.

### Next

Prompt 041b – Itinerary Domain and Application Contracts.
