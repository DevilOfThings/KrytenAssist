# Codex Prompt 039a – Price Drop Alert Experience and Contract

## Analysis Prompt

Define **Step 1 only** from:

```text
docs/AI Playbook/039 - Price Drop Alerts.md
```

Prompt 038 is complete. This step agrees the Price Drop Alert experience,
detection boundaries, identities, lifecycle and data limitations before any
production implementation.

---

## Required Decisions

- explicit Record Observation versus unattended monitoring trigger
- same-source comparable price-drop definition
- configurable minimum percentage and default behaviour
- first-observation and incomparable-price behaviour
- new/changed/disappeared promotion semantics
- supported saved month/budget criteria and transition boundaries
- honest deferral of cabin matching to Prompt 040
- in-app Alert Centre versus external/OS notifications
- unread/read/dismissed lifecycle and retention
- deterministic evidence-based deduplication
- single/batch recording partial-failure semantics
- retry, cancellation and stale-result behaviour
- strict preservation of Cruise History and Saved Cruises

---

## Proposed Contract for Agreement

- Prompt 039 evaluates only explicitly recorded evidence; it adds no scheduled
  browsing or background monitoring.
- Price drops compare the latest two unambiguous comparable prices in one
  sailing/retailer History.
- an optional 0–100 minimum percentage controls future Price Drop alerts; zero
  means every exact reduction
- the initial settings enable all three alert types with a zero threshold
- first observation, unchanged/higher/incomparable price and changes below the
  configured threshold do not alert
- a non-empty promotion appearing or changing after prior same-source evidence
  alerts; first-observation promotions and disappearance do not
- Shortlisted saved cruises may alert on transition into all currently
  evaluable month/budget criteria
- criteria evaluation follows explicit Record Observation, Save Cruise,
  Restore and Save Preferences actions only
- cabin criteria remain unavailable until Prompt 040 supplies cabin evidence
- alerts are durable and in-app only, with Unread, Read and Dismissed lifecycle
- dismissed alerts are retained without automatic expiry in Prompt 039
- deterministic event keys prevent retry, AlreadyCurrent and concurrency
  duplicates
- recording commits independently before alert evaluation; alert failure never
  rolls back or falsely fails Record Observation
- alert lifecycle/settings changes never edit History, saved evaluations or
  preferences

---

## Required Output

- update the Prompt 039 playbook with Robin's agreed decisions
- refine the deterministic acceptance scenarios
- identify any current price-basis or preference-data limitation explicitly
- define the exact scope of 039b–039g
- update Roadmap status

---

## Exclusions

- no production code, schema or migration changes
- no Avalonia implementation
- no scheduler, background browser or network work
- no email, SMS, push or OS notifications
- no cabin availability matching
- no cross-retailer comparisons or currency conversion
- no Prompt 040 implementation

---

## Results

### Status

Complete. Robin agreed the Prompt 039 contract on 18 July 2026.

### Agreed Contract

- Prompt 039 evaluates alerts only from explicit actions and recorded local
  evidence. It does not schedule browsing, scrape in the background or imply
  live monitoring.
- Price Drop alerts compare the latest two unambiguous, like-for-like prices
  within one sailing and retail-source History.
- Alert settings initially enable Price Drop, Promotion and Saved Criteria
  alerts with a zero minimum percentage, so every exact comparable reduction
  is eligible until Robin deliberately changes the threshold.
- The threshold accepts 0–100 and affects future evaluations only. It does not
  rewrite or backfill previous alerts.
- First observations, unchanged/higher/incomparable prices and reductions below
  the configured threshold do not create Price Drop alerts.
- A non-empty promotion appearing or materially changing after prior
  same-source evidence creates a Promotion alert. First-observation promotions
  and promotion disappearance do not.
- Shortlisted saved cruises may create a Saved Criteria alert when they
  transition into all explicitly set criteria that existing evidence can
  evaluate: departure month and an unambiguous matching-currency/basis budget.
- Criteria evaluation follows successful explicit Record Observation, Save
  Cruise/refresh, Restore to Shortlist and Save Preferences actions.
- Dismissed sailings are excluded. Favourite and evaluation state are context,
  not detection criteria.
- Cabin preferences are not treated as matched or unmatched because current
  observations contain no cabin evidence. Prompt 040 owns that extension.
- Alerts are durable and in-app only. External email, SMS, push, sounds and OS
  notifications are excluded.
- Alert lifecycle is Unread, Read or Dismissed. Dismissed alerts remain retained
  without automatic expiry in Prompt 039.
- Deterministic event keys prevent duplicates across retry, AlreadyCurrent
  evidence and concurrent evaluation.
- Observation recording commits independently before alert evaluation. Alert
  failure never rolls back or falsely fails a successful Record Observation.
- Alert lifecycle/settings changes never edit or delete Cruise History, Saved
  Cruises, evaluations, favourites or preferences.

### Accepted Data Limitations

- Existing History deliberately partitions observations by retail source, so
  price and promotion changes are never compared across retailers.
- Existing price bases are bounded text. Budget evaluation recognizes only one
  unambiguous price with the requested currency and accepted per-person or
  total-booking basis; ambiguous or unavailable data cannot alert.
- No passenger count or currency conversion is inferred.
- Departure month is available from sailing identity. Cabin category and
  availability are not available until Prompt 040.

### Implementation Scope

- 039b: provider-independent alert domain, settings, evaluators, Application
  contracts and controlled results
- 039c: normalized SQLite alerts/settings/criteria state and migration
- 039d: single/batch record-then-evaluate integration with independent outcomes
- 039e: saved month/budget criteria transition evaluation
- 039f: in-app Alert Centre, lifecycle actions, unread badge and settings
- 039g: cross-boundary audit, complete offline verification and manual acceptance

### Build and Tests

Not run. Prompt 039a changed documentation only and introduced no production
behaviour.

### Next

Prompt 039b – Alert Domain and Application Contracts.
