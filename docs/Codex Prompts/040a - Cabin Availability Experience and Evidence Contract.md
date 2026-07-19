# Codex Prompt 040a – Cabin Availability Experience and Evidence Contract

## Analysis Prompt

Complete **Step 040a only** from:

```text
docs/AI Playbook/040 - Cabin Availability.md
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
6. `docs/AI Playbook/038 - Saved Cruises and Preferences.md`
7. `docs/AI Playbook/039 - Price Drop Alerts.md`
8. `docs/AI Playbook/040 - Cabin Availability.md`
9. existing cabin enum/preferences, Cruise observations, saved-criteria detector,
   alerts and current TUI capture scripts

---

## Decisions to Confirm

### Evidence Meaning

Confirm that cabin evidence is tied to sailing, retailer, compatible
search/occupancy context and time—not the sailing alone.

Confirm three states:

```text
Available | Unavailable | Unknown
```

Confirm that omission is Unknown, `All gone` is not category-specific evidence,
and Unavailable requires explicit category wording/state.

Confirm Partial versus Complete coverage. A current result card showing
`1 x Inside Cabin (Cheapest available)` proves only Inside Available for that
search; other categories remain Unknown.

### Search Context

Agree the minimum comparison identity:

- adults
- children and known ages
- package/cruise-only mode
- departure airport when relevant
- cabin quantity when known

Decide how incomplete context is displayed and whether observations with
different or unknown material context may ever compare. The safe default is
separate/incompatible.

### Workflow and Language

Confirm explicit:

```text
Capture Cabin Evidence → Review → Record Cabin Observation → Revisit History
```

Agree UI wording such as `available when recorded for this search`, not live
inventory or unattended monitoring language.

### Preferences

Confirm:

- preferred cabin types are OR
- month, budget and cabin criterion groups are AND
- cabin-only preferences can match only explicit compatible Available evidence
- Unknown cannot satisfy or fail as explicit Unavailable
- saved criteria fingerprint/evaluation becomes a new version
- historic criteria alerts are not backfilled

### Alerts

Choose whether Prompt 040 adds a separate Cabin Availability alert type.

Recommended contract:

- Unavailable → Available for a preferred cabin on a Shortlisted cruise:
  positive availability alert
- Available → Unavailable: loss-of-availability alert
- first observation: history only
- Unknown → Available: neutral newly-observed evidence, not `became available`;
  decide whether this is history only or a separately worded discovery alert
- repeated identical evidence: no alert

Alerts remain in-app and evidence-linked. They do not trigger capture or
background browsing.

### Acquisition Boundary

Confirm initial 040d support:

- current search-card partial positive evidence
- richer multi-category evidence only after Robin demonstrates the actual
  cabin-selection page
- no automatic clicks, booking progress, endpoint calls or hidden inventory
  inspection

If richer TUI evidence cannot be demonstrated, Prompt 040 must still deliver an
honest partial-evidence history rather than guessing complete availability.

### Retention and Independence

Confirm cabin observations are retained factual evidence with no automatic
expiry, permanent delete UI or cascade from Saved Cruises/price History.

---

## Required Analysis Tests

Before agreeing implementation, walk through at least:

1. Inside explicitly available; all others absent.
2. Complete chooser with Balcony available and Suite explicitly sold out.
3. Whole package `All gone` with no category evidence.
4. Same cabin evidence for two adults versus a family search.
5. First preferred cabin observed Available.
6. Preferred cabin explicitly Unavailable then Available.
7. Preferred cabin Available then explicitly Unavailable.
8. Two preferred categories where one is Available.
9. Month and budget match but cabin is Unknown.
10. Cabin-only preference with compatible Available evidence.
11. Saved Cruise removed while cabin history remains.
12. Capture/record succeeds but later alert evaluation fails.

Record the agreed outcomes in this prompt and the playbook.

---

## Exclusions

- production implementation
- schema or migration
- background/scheduled browsing
- booking automation or cabin selection
- inventory counts or cabin numbers unless explicitly evidenced and separately
  agreed
- new retailers
- Prompt 041/042 work

---

## Results

Completed on 19 July 2026.

### Status

Complete. The proposed safe defaults were accepted as the implementation
contract for Prompts 040b–040g.

### Agreed Evidence Contract

- Cabin evidence belongs to sailing, retail source, compatible search/occupancy
  context and evidence time.
- Category states are exactly `Available`, `Unavailable` and `Unknown`.
- Available and Unavailable require explicit category evidence. Omission is
  always Unknown.
- Whole-package `All gone` evidence does not manufacture category states.
- Evidence coverage is `Partial` or `Complete`. Complete requires the source to
  explicitly enumerate the supported category set and expose a state for every
  category; a search card is Partial.
- `1 x Inside Cabin (Cheapest available)` proves Inside Available only. Outside,
  Balcony, Suite and Solo remain Unknown.
- Availability wording is historical and contextual: `available when recorded
  for this search`. The product does not claim live inventory or monitoring.

### Agreed Context Identity

Compatible-series identity contains all explicitly known material context:

- adult count
- child count and ordered ages when supplied
- package/fly-cruise/cruise-only mode
- departure airport for flight packages
- cabin quantity when known

Unknown values are retained as explicit unknown markers in the fingerprint.
They are not filled with Robin's usual defaults. A series with unknown material
context does not compare with a series where that value is known, and different
known values always form independent series.

### Agreed Workflow and Retention

```text
Capture Cabin Evidence
        ↓
Review source, context, coverage and category states
        ↓
Record Cabin Observation
        ↓
Revisit local Cabin Availability history
```

Capture never records automatically. Cabin evidence is retained factual history
without automatic expiry or permanent-delete UI in Prompt 040. It has no
ownership/cascade relationship with Price History, Saved Cruises, preferences
or alerts.

### Agreed Preference Semantics

- Multiple preferred cabin categories are OR: one explicitly Available
  preferred category satisfies the cabin group.
- Configured month, budget and cabin groups combine with AND.
- Cabin-only preferences may become Met when compatible evidence explicitly
  shows a preferred cabin Available.
- Explicit Unavailable for every preferred category makes the cabin group
  NotMet.
- When no preferred category is Available and at least one preferred category
  is Unknown, the cabin group is Unknown rather than NotMet.
- A known failing month/budget criterion makes the combined result NotMet even
  when cabin evidence is Unknown. Otherwise unresolved cabin evidence makes the
  combined result Unknown.
- Criteria identity receives a new version including normalized preferred cabins
  and compatible cabin-evidence context. No historic Saved Criteria alerts are
  backfilled.

### Agreed Alert Semantics

Prompt 040 will add a separate provider-independent `CabinAvailability` alert
type with typed transition details. It remains distinct from the existing Saved
Criteria alert.

For a Shortlisted saved cruise and compatible context:

- first observation: Cabin History only
- Unknown → Available: Cabin History only; this is new knowledge, not proof that
  inventory became available
- Unavailable → Available for a preferred category: positive Cabin Availability
  alert
- Available → Unavailable for a preferred category: loss-of-availability alert
- repeated equivalent evidence: no alert
- transitions affecting only non-preferred categories: history only

The alert records previous/current explicit state, category, context
fingerprint, retail source and triggering evidence identity/time. It does not
trigger capture, imply background monitoring or roll back a committed cabin
observation if alert persistence fails.

Saved Criteria evaluation remains separate: a newly satisfied cabin group may
also cause the existing Saved Criteria transition when every configured group is
Met. Event keys keep those two alert meanings independently deduplicated.

### Agreed Initial TUI Boundary

- 040d initially supports demonstrated partial positive search-card evidence.
- Multi-category Available/Unavailable mapping is added only after Robin
  demonstrates the actual cabin-selection page and its semantic states.
- An expired/sold-out package without category evidence returns controlled
  incomplete/unsupported capture.
- The fixed script never clicks cabins, progresses booking, submits passenger
  data, scrolls automatically, calls private endpoints or reads browser storage.

### Scenario Outcomes

1. Inside explicitly available and all others absent: record Partial evidence;
   Inside Available, others Unknown.
2. Complete chooser with Balcony Available and Suite sold out: record both
   explicit states and the remaining enumerated category states.
3. Whole package All gone: no category observation.
4. Two-adult and family searches: independent context series.
5. First preferred cabin observed Available: history only; criteria may become
   Met, but no Cabin Availability transition alert.
6. Preferred cabin Unavailable then Available: history plus positive transition
   alert; Saved Criteria may independently transition to Met.
7. Preferred cabin Available then Unavailable: history plus loss alert.
8. Two preferred categories with one Available: cabin group Met.
9. Month/budget pass and preferred cabin Unknown: combined criteria Unknown.
10. Cabin-only preference with compatible Available evidence: criteria Met.
11. Removing Saved Cruise: cabin history remains; later preference alerts cease
    until the sailing is Shortlisted again.
12. Cabin record commits and alert evaluation fails: recording remains
    successful and the alert outcome is retryable.

### Next Step

Prompt 040b – Cabin Domain and Application Contracts.
