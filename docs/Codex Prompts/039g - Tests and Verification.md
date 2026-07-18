# Codex Prompt 039g – Tests and Verification

## Verification Prompt

Complete **Step 7 only** from:

```text
docs/AI Playbook/039 - Price Drop Alerts.md
```

Prompts 039a–039f are implemented. This final step audits their combined
boundaries, fills only material verification gaps, runs the complete offline
suite and records the manual desktop acceptance outcome. Do not start Prompt
040 or add new Alert features.

---

## Required Reading

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/037 - Cruise History and Price Tracking.md`
5. `docs/AI Playbook/038 - Saved Cruises and Preferences.md`
6. `docs/AI Playbook/039 - Price Drop Alerts.md`
7. every `docs/Codex Prompts/039a` through `039f` prompt and Results section
8. Prompt 039 Core/Application contracts, orchestration and tests
9. alert/settings/criteria-state SQLite adapters, migration and tests
10. Record, Save, Restore, Preferences and Alert Centre ViewModels/composition
11. latest Prompt 039 session handovers

---

## Scope

This is an audit and verification prompt, not another feature prompt.

Allowed production changes are limited to concrete defects discovered while
verifying the accepted 039a–f contract. Any correction must be minimal,
documented and covered by a failing-then-passing focused test.

Do not redesign accepted detection, identity, lifecycle, settings, inbox or
workspace semantics.

---

## Required Cross-Boundary Audit

### Evidence, Personal State and Alerts

Prove as coherent workflows that:

1. first explicit Record commits History and creates no Price Drop/Promotion
   change alert
2. a changed current same-source observation may create typed alerts only after
   History commits
3. alert evaluation cancellation/failure never rolls back or falsely fails the
   primary factual/personal mutation
4. Alert lifecycle changes modify only alert status
5. Alert Settings changes modify only future evaluation configuration
6. Saved Criteria triggers after explicit Record, Save, Restore and Save
   Preferences without changing the primary result
7. removing History or Saved Cruises does not cascade to retained alerts
8. alert operations never rewrite History, Saved Cruises, preferences or
   criteria evidence

Use isolated SQLite plus Application use cases where practical. Never access
Robin's database.

### Detection, Identity and Deduplication

Audit deterministic coverage proving:

- same-source comparable Price Drop calculation and inclusive threshold
- no alert for first, unchanged, higher, incomparable or below-threshold price
- new/changed Promotion detection and no first/disappearing promotion claim
- source-specific observation alerts remain distinct across retailers
- Saved Criteria requires every supported configured month/budget criterion
- dismissed, wrong-month, above-budget, ambiguous-basis and cabin-only cases do
  not create a criteria alert
- first/re-entered Met may alert while repeated Met does not
- stable event keys and database uniqueness converge retries/concurrency to one
  retained alert
- settings/alerts/criteria transition state round-trip exactly across restart

### Alert Centre and Settings

Audit deterministic coverage proving:

- Discovery remains the default and all three Cruise modes are exclusive
- activation loads one complete ordered snapshot and durable unread count
- exact type and Active/Unread/Read/Dismissed filters compose locally
- selection is stable and never auto-marks read
- lifecycle actions update the snapshot and reload the durable unread count
- Dismissed alerts remain discoverable and restorable as Unread
- typed details remain based on stored immutable evidence and hide opaque keys
- count/list/settings loads reject stale results and preserve last good state
- Alert Settings retain confirmed baseline versus draft; invalid values never
  call persistence; failed saves retain the draft
- created-alert outcomes from single/batch Record, Save, Restore and Save
  Preferences request count refresh without optimistic arithmetic
- there is no polling, timer, background browsing or external notification

### Architecture, Schema and Composition

Confirm:

- Core and Application expose no Avalonia, EF Core, SQLite, browser or provider
  SDK types
- Application owns alert repository/use-case contracts and orchestration
- Infrastructure adapters remain scoped and registered through extension
  methods
- no alert/settings/criteria table has a foreign key/navigation/cascade to
  History or Saved Cruises
- the persistence-enabled desktop resolves one shared coordinator, Alert
  Centre, Settings editor and Cruise workspace without service location
- the generic shell remains resolvable without persistence
- provider/capture types do not enter alert presentation contracts
- no Prompt 039 path performs network work except Robin's pre-existing explicit
  browser/capture workflow

Record findings in Results. Do not silently leave a known defect.

---

## Required Automated Verification

Add only missing tests needed to make the combined boundary explicit. Prefer a
focused integration/audit fixture rather than duplicating 039b–f unit tests.

Run:

```text
dotnet build KrytenAssist.sln --no-restore --verbosity minimal -m:1
dotnet test KrytenAssist.Core.Tests/KrytenAssist.Core.Tests.csproj --no-build --no-restore --verbosity minimal -m:1
dotnet test KrytenAssist.Avalonia.Tests/KrytenAssist.Avalonia.Tests.csproj --no-build --no-restore --verbosity minimal -m:1
dotnet test KrytenAssist.Api.Tests/KrytenAssist.Api.Tests.csproj --no-build --no-restore --verbosity minimal -m:1
git diff --check
```

All tests remain deterministic and offline. Inspect migration/model metadata and
repository use sites in addition to executing tests.

---

## Manual Desktop Acceptance Checklist

Robin verifies against a disposable or accepted local desktop profile:

### Alert creation and independence

- Record a first observation and confirm History changes with no change alert
- Record a lower comparable same-source price and confirm one explainable Price
  Drop alert appears without changing saved evaluation
- record a new/changed promotion and confirm its previous/current evidence
- cause a supported saved month/budget match through an explicit agreed action
- confirm primary Record/Save/Restore/Preferences feedback remains independent

### Alert Centre lifecycle

- switch among Discovery, Saved Cruises and Alerts without browser navigation
- confirm the unread badge matches the retained local inbox
- inspect All, Price drops, Promotions and Saved criteria filters
- inspect Active, Unread, Read and Dismissed filters
- select an Unread alert and confirm selection alone does not mark it Read
- Mark read, Mark unread and Dismiss; confirm badge/filter changes
- find the Dismissed alert and Restore as unread
- restart the desktop and confirm alerts/lifecycle survive

### Alert Settings

- switch Inbox/Settings and edit each enabled flag
- verify exact 0 and 100 thresholds plus one representative decimal
- verify blank, non-numeric, negative and above-100 validation without losing
  the draft
- verify Cancel Changes restores the confirmed baseline
- save, restart and confirm settings reload
- confirm saving settings does not backfill, delete or recalculate alerts

### General

- confirm honest no-background-monitoring language and no email/push claims
- confirm typed details wrap/scroll and lifecycle controls remain reachable
- confirm existing Discovery, History, Saved Cruises and Preferences workflows
  remain usable at the intended desktop/tablet window size

Record Robin's date and outcome in Results. If any item fails, keep Prompt 039g
and Prompt 039 open, document the failure and implement only an in-scope fix.

---

## Documentation Completion

When automated verification passes:

- complete this prompt's automated Results and audit findings
- update Prompt 039 playbook Results and Lessons Learned
- update Roadmap with the exact automated status
- create the final Prompt 039 session handover
- keep Prompt 040 unstarted

Only after Robin confirms the manual checklist:

- mark Prompt 039g and Prompt 039 complete
- record the manual verification date and accepted limitations
- identify Prompt 040 as next without implementing it

---

## Allowed Changes

```text
KrytenAssist.Core.Tests/Cruises/ (audit coverage only)
KrytenAssist.Avalonia.Tests/Application/Cruises/ (audit coverage only)
KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/ (audit coverage only)
KrytenAssist.Avalonia.Tests/ViewModels/ (audit coverage only)
KrytenAssist.Avalonia.Tests/DependencyInjection/ (audit coverage only)
docs/Codex Prompts/039g - Tests and Verification.md
docs/AI Playbook/039 - Price Drop Alerts.md
docs/Roadmap.md
docs/Session Handovers/
```

Production changes require a concrete audited defect and focused regression
test. Do not stage, commit, push, discard or overwrite unrelated work.

---

## Exclusions

- Prompt 040 cabin availability or any later roadmap work
- background acquisition, scheduling, polling or monitoring
- email, SMS, push, OS notifications or sounds
- cross-retailer comparison, currency conversion or recommendation
- alert deletion, expiry, retention pruning or bulk actions
- new retailer, capture, browser, price or identity behaviour
- visual redesign or unrelated cleanup

---

## Results

### Status

Complete. All 656 offline tests pass, no production defect was found and Robin
manually confirmed the desktop acceptance checklist on 19 July 2026. Prompt 039
is complete; Prompt 040 is next and remains unstarted.

### Automated Audit Summary

- Added an isolated SQLite boundary test proving that a retained Dismissed alert
  and exact Alert Settings survive removal of both factual History and Saved
  Cruise personal state, including database reopen.
- Added desktop composition assertions for the scoped unread coordinator, Alert
  Settings editor and Alert Centre, including the shared Settings instance.
- Existing deterministic tests cover first/AlreadyCurrent suppression, exact
  current-evidence selection, record-before-evaluate failure independence,
  Price Drop threshold/comparability, Promotion transitions, source identity,
  Saved Criteria month/budget/cabin boundaries and transition retries.
- Existing persistence tests cover all typed payload round trips, ordering,
  filters, unread lifecycle, exact settings, criteria-state ordering,
  database-enforced event deduplication, cancellation and concurrent insert
  convergence.
- Existing Alert Centre tests cover typed evidence projection, Active filtering,
  dismissed recovery, no-auto-read selection and invalid/failed settings saves.
- No automated test contacted TUI, launched a browser, used Robin's database or
  performed external notification work.

### Architecture and Schema Findings

- Core/Application Cruise alert code contains no Avalonia, EF Core, SQLite,
  WebView, AngleSharp, OpenAI or provider SDK dependency.
- Alert, settings and criteria-state repository interfaces remain
  Application-owned; SQLite implementations are scoped through Infrastructure
  dependency-injection extensions.
- Schema metadata proves alert/settings/criteria-state headers have no foreign
  key to History or Saved Cruises. Typed detail tables relate only to their
  owning `CruiseAlerts` header.
- Alert lifecycle/status and settings persistence remain physically independent
  from observations, Saved Cruises and preferences in both directions.
- The persistence-enabled desktop resolves the complete 039f graph without
  service location; the generic shell's no-persistence resolution tests remain
  green.
- Alert presentation contains no timers, polling, HTTP/WebView access, toast or
  notification delivery path.
- No production correction, schema change or migration was required.

### Files Added

- this prompt
- `KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/Prompt039BoundaryTests.cs`
- `docs/Session Handovers/2026-07-19 Session 027.md`

### Build and Automated Tests

- focused Prompt 039 boundary/composition audit: 3 passed
- `dotnet build KrytenAssist.sln --no-restore --verbosity minimal -m:1`:
  passed with 0 errors and five existing `SQLitePCLRaw` advisory warnings
- Core: 139 passed
- Avalonia/Application/Infrastructure: 508 passed
- API: 9 passed
- total: 656 passed, 0 failed, 0 skipped
- `git diff --check`: passed

All verification used isolated/local test data and remained offline.

### Manual Acceptance

Robin manually confirmed the complete checklist on 19 July 2026, including
explicit alert creation and primary-action independence, unread badge and inbox
filters, selection without automatic lifecycle mutation, read/unread/dismiss/
restore behaviour, restart persistence, Alert Settings validation/save/cancel,
honest no-monitoring language and existing Cruise workspace usability.

Prompt 039g and Prompt 039 are complete. Prompt 040 is next and remains
unstarted.
