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

Awaiting agreement.

### Build and Tests

Not run. This prompt is documentation-only.

