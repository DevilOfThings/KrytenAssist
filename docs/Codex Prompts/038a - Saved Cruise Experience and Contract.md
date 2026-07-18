# Codex Prompt 038a – Saved Cruise Experience and Contract

## Analysis Prompt

Define **Step 1 only** from:

```text
docs/AI Playbook/038 - Saved Cruises and Preferences.md
```

This step establishes the agreed user experience, identity and boundary for
Prompt 038 before further production implementation.

The provisional Prompt 038 classes, repository, migration and tests already in
the worktree may be inspected as a disposable prototype. Do not treat their
current shapes as accepted, extend them, stage them, commit them or remove them
in this step.

---

## Required Decisions

- save directly from a successful capture without Record Observation
- save from Recorded Cruise History
- stable identity for a specific saved sailing
- minimum local snapshot for a saved but unrecorded sailing
- shortlist versus Not for us behaviour
- optional rating scale and personal notes
- favourite sailing versus favourite operator/ship identity
- preferred month collection
- preferred cabin collection
- maximum budget and per-person/total basis
- removal semantics and strict preservation of Cruise History
- Saved Cruises placement and default organisation

---

## Required Output

- update the Prompt 038 playbook with the agreed contract
- enumerate deterministic acceptance scenarios for later steps
- identify every provisional class or migration assumption that must change
- define the scope of 038b–038g without implementing those steps

---

## Exclusions

- no production code changes
- no schema or migration changes
- no Avalonia implementation
- no recommendation or scoring rules
- no Prompt 039 alerts
- no new provider, browser or capture behaviour

---

## Results

> Complete after Robin agrees the Prompt 038 contract.

### Status

Complete.

### Agreed Contract

- Save Cruise is independent from Record Observation and is available from a
  successful capture or Recorded Cruise History.
- A saved item represents one specific operator/ship/departure/duration
  sailing, independently of retailer offer identity.
- A saved but unrecorded sailing retains a bounded snapshot containing title,
  operator, ship, departure date and port, duration, itinerary summary, latest
  displayed price, retail source, trusted reference and time saved.
- That snapshot never counts as an observation or price-history evidence.
- Maybe and Strong candidate form the default shortlist. Not for us is retained
  as dismissed personal evidence behind an explicit filter.
- Overall, itinerary, ship and value ratings are optional integers from 1–5.
- Favourite sailing and favourite operator/ship are separate identities.
- Preferences support multiple months and cabin types plus an optional maximum
  budget with an explicit per-person or total-booking basis.
- Removing or changing personal state never removes or rewrites Cruise History.
- Saved Cruises is a distinct area within the Cruise experience and may show
  associated History context when it exists.

### Deterministic Acceptance Scenarios for Later Steps

1. Saving a captured sailing creates one shortlist item and zero observations.
2. Recording that same capture creates History evidence without duplicating or
   changing its saved evaluation.
3. Saving the same sailing later from History resolves to the existing saved
   item even when the retail source differs.
4. A saved capture remains recognisable after restart when no History exists.
5. Changing Maybe to Strong candidate updates the same item.
6. Changing an item to Not for us removes it from the default shortlist but
   retains it in the dismissed filter.
7. Restoring a dismissed sailing returns it to the shortlist without changing
   History.
8. Removing personal state leaves every observation and price snapshot intact.
9. Favouriting a ship is reflected for other loaded sailings from the same
   operator and ship; favouriting one sailing does not favourite its siblings.
10. Optional ratings and notes can be added, changed and cleared independently.
11. Multiple months and cabins survive restart with their budget and basis.
12. Missing History is shown honestly rather than manufacturing a current
    tracked price from the saved snapshot.

### Provisional Prototype Audit for 038b

The retained prototype must change before acceptance:

- `CruiseEvaluation` currently makes interest mandatory and conflates the
  saved/dismissed lifecycle with evaluation fields.
- `IsFavouriteShip` is incorrectly repeated on each sailing evaluation instead
  of using operator/ship identity.
- `CruisePreferences` supports only one month and one cabin.
- maximum budget has no per-person/total-booking basis.
- there is no saved-sailing record or bounded saved snapshot, so an unrecorded
  capture cannot be saved usefully.
- repository operations persist evaluations only and do not express save,
  dismiss, restore or saved-item removal semantics.
- save use cases return only `bool`, which is insufficient for controlled
  created, updated, unchanged, cancelled and failed outcomes.
- the provisional tables and migration encode those incomplete assumptions and
  must be audited or replaced before the migration is accepted.

### Files Updated

- `docs/AI Playbook/038 - Saved Cruises and Preferences.md`
- this prompt
- `docs/Roadmap.md`

### Build and Tests

Not run. Prompt 038a changed documentation only; the retained prototype remains
subject to implementation verification in later steps.

### Next

Prompt 038b – Personal Cruise Domain and Application Contracts.
