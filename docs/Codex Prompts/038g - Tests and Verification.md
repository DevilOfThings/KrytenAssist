# Codex Prompt 038g – Tests and Verification

## Verification Prompt

Complete **Step 7 only** from:

```text
docs/AI Playbook/038 - Saved Cruises and Preferences.md
```

Prompts 038a–038f are implemented. This final step audits their combined
boundaries, fills only material verification gaps, runs the complete offline
suite and records the manual desktop acceptance outcome. Do not start Prompt
039 or add new Saved Cruise features.

---

## Required Reading

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/037 - Cruise History and Price Tracking.md`
5. `docs/AI Playbook/038 - Saved Cruises and Preferences.md`
6. every `docs/Codex Prompts/038a` through `038f` prompt and Results section
7. Prompt 038 Core/Application contracts and tests
8. personal-state and History SQLite adapters, migration and tests
9. Cruise capture, History, Saved Cruises, evaluation and preference
   ViewModels/views/composition/tests
10. latest Prompt 038 session handovers

---

## Scope

This is an audit and verification prompt, not another feature prompt.

Allowed production changes are limited to concrete defects discovered while
verifying the accepted 038a–f contract. Any correction must be minimal,
documented and covered by a failing-then-passing focused test.

Do not redesign accepted UI, contracts, schema, filters or preference semantics.

---

## Required Cross-Boundary Audit

### Personal State Versus Factual Evidence

Prove as one coherent workflow that:

1. Save Cruise creates one personal saved aggregate and zero observations.
2. Record Observation for the same sailing creates factual History without
   changing its evaluation, lifecycle or favourites.
3. re-saving the sailing from a different retail source refreshes only the
   bounded saved snapshot and does not duplicate personal identity
4. evaluation, dismiss and restore mutations do not add, rewrite or remove
   observations
5. removing the saved aggregate leaves every recorded observation intact
6. deleting History leaves the personal saved aggregate intact

Use isolated SQLite plus Application use cases where practical so the test
exercises the physical database boundary as well as contracts. Never use
Robin's database.

### Identity and Organisation

Audit deterministic coverage proving:

- saved sailing identity is operator/ship/departure/duration, not retailer
- favourite sailing and favourite operator/ship remain distinct
- Favourites includes Shortlisted favourite sailings or favourite ships only
- Not for us is Dismissed lifecycle, not an interest value
- source-specific histories remain distinct while Saved Cruises joins all
  matching histories by sailing identity
- Price when saved and Latest recorded price remain separately labelled
- a saved cruise without History remains useful and honest
- dismiss/restore/remove update selection and filters without stale results

### Preference Profile

Audit deterministic coverage proving:

- months and cabins are multi-value, normalized and restart-persistent
- maximum budget remains optional and requires amount/currency/basis together
- invalid budget drafts never call persistence
- Save Preferences is the only write path
- Cancel Changes and Clear Draft remain distinct
- failed/cancelled saves retain unsaved drafts
- preferences do not filter, sort, score or otherwise mutate Saved Cruises,
  History, favourites, capture or browser state

### Architecture and Composition

Confirm:

- Core and Application expose no Avalonia, EF Core, SQLite, browser or provider
  SDK types
- Application owns all personal-state repository interfaces and use cases
- Infrastructure adapters remain scoped and registered through extension
  methods
- the desktop resolves the shared evaluation editor, Saved Cruises organiser
  and preference editor without service location
- one resolved Cruise workspace shares editor instances as designed
- no Prompt 038 code writes through `ICruiseObservationRepository` except the
  existing explicit Record Observation path; the organisation query is
  read-only
- no personal-state table has a foreign key/navigation/cascade to Cruise
  History

Record findings in this prompt's Results. Do not silently leave a known defect.

---

## Required Automated Verification

Add only missing tests needed to make the combined boundary explicit. Prefer
one focused integration test fixture rather than duplicating all 038b–f unit
tests.

Run:

```text
dotnet build KrytenAssist.sln --no-restore
dotnet test KrytenAssist.Core.Tests/KrytenAssist.Core.Tests.csproj --no-build --no-restore
dotnet test KrytenAssist.Avalonia.Tests/KrytenAssist.Avalonia.Tests.csproj --no-build --no-restore
dotnet test KrytenAssist.Api.Tests/KrytenAssist.Api.Tests.csproj --no-build --no-restore
git diff --check
```

Use the established single-worker/disabled-build-server options when required.
All tests stay deterministic and offline.

Also inspect the generated migration/model relationship metadata or existing
schema tests to confirm there is no personal-state-to-History relationship.

---

## Manual Desktop Acceptance Checklist

After automated verification, Robin verifies against a disposable or accepted
local desktop profile:

### Save and History independence

- capture one supported sailing and choose Save Cruise only
- confirm it appears in Saved Cruises while Recorded History does not change
- then Record Observation and confirm History changes while saved evaluation
  does not

### Saved Cruises organisation

- inspect Shortlist, Strong candidates, Favourites and Not for us filters
- edit evaluation and independently toggle cruise and ship favourites
- confirm a no-History item says no recorded price history
- confirm Price when saved is distinct from Latest recorded price
- move one item to Not for us and restore it
- remove one saved item through confirmation and verify History remains

### Preferences

- select multiple months and cabin types
- enable and save a valid maximum budget
- verify invalid amount/currency feedback without losing the draft
- verify Cancel Changes and Clear Draft semantics
- restart the desktop and confirm saved preferences reload
- confirm changing preferences does not change Saved Cruises filter membership

### General

- switch Discovery/Saved Cruises and Saved list/Preferences without unwanted
  browser navigation or lost unsaved preference draft
- confirm bounded scrolling and control labels are usable at the intended
  desktop/tablet window size

Record Robin's date and outcome in Results. If any item fails, keep Prompt 038g
and Prompt 038 open, document the failure and implement only an in-scope fix.

---

## Documentation Completion

When automated verification passes:

- complete this prompt's automated Results and audit findings
- update Prompt 038 playbook Results and Lessons Learned
- update Roadmap with the exact automated status
- create the final Prompt 038 session handover
- keep Prompt 039 unstarted

Only after Robin confirms the manual checklist:

- mark Prompt 038g complete
- mark Prompt 038 complete in the playbook and Roadmap
- record the manual verification date and any accepted limitations
- identify Prompt 039 as next without implementing it

---

## Allowed Changes

```text
KrytenAssist.Core.Tests/Cruises/ (audit coverage only)
KrytenAssist.Avalonia.Tests/Application/Cruises/ (audit coverage only)
KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/ (audit coverage only)
KrytenAssist.Avalonia.Tests/ViewModels/ (audit coverage only)
KrytenAssist.Avalonia.Tests/DependencyInjection/ (audit coverage only)
docs/Codex Prompts/038g - Tests and Verification.md
docs/AI Playbook/038 - Saved Cruises and Preferences.md
docs/Roadmap.md
docs/Session Handovers/
```

Production changes require a concrete audited defect and a focused regression
test. Do not stage, commit, push, discard or overwrite unrelated work.

---

## Exclusions

- preference-based filtering, ranking or recommendations
- price-drop alerts or notifications
- any Prompt 039 implementation
- new retailer, capture, browser, price or identity behaviour
- new persistence schema or migration without a proven Prompt 038 defect
- visual redesign or unrelated cleanup
- cloud sync, bookings, monitoring or background work

---

## Results

> Complete after automated and Robin's manual verification.

### Status

Complete. Robin manually confirmed the desktop acceptance checklist on 18 July
2026.

### Automated Audit Summary

- Added two isolated SQLite cross-boundary tests covering the complete personal
  sailing workflow and reverse History-deletion boundary.
- Save Cruise created personal state with zero observations.
- explicit Record Observation created factual History without changing
  evaluation, lifecycle or favourite-sailing state.
- re-saving the same sailing from another retailer refreshed its bounded
  snapshot without duplicating identity or changing personal state.
- evaluation, favourite, dismiss and restore mutations left observations
  unchanged.
- removing personal saved state left History and observations intact.
- deleting History and its observations left the saved aggregate intact.
- Existing deterministic tests cover exact organiser filters, favourite-ship
  identity, multi-source History association, separate saved/recorded price
  labels, no-History presentation, preference draft/validation semantics,
  restart persistence and stale-result protection.

### Architecture and Schema Findings

- No Avalonia, EF Core, SQLite, browser, WebView, OpenAI or provider SDK types
  were found in Prompt 038 Core/Application contracts.
- Personal repository interfaces remain Application-owned; SQLite adapters and
  DbContext registrations remain scoped through extension methods.
- Personal-state use cases do not write through
  `ICruiseObservationRepository`. The Saved Cruises organisation query uses its
  list operation read-only; explicit `RecordCruiseObservation` remains the only
  Prompt 038 workflow that records evidence.
- The personal-state migration contains foreign keys only from preference
  month/cabin children to their singleton preference profile. There is no
  foreign key, navigation or cascade between Saved Cruises/favourites/
  preferences and Cruise History.
- Desktop composition resolves and shares the evaluation and preference child
  ViewModels through dependency injection without service location.
- No production correction, schema change or migration was required by the
  audit.

### Files Added

- `KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/Prompt038BoundaryTests.cs`
- this prompt
- `docs/Session Handovers/2026-07-18 Session 024.md`

### Build and Automated Tests

- `dotnet build KrytenAssist.sln --no-restore --disable-build-servers -m:1`:
  passed with 0 errors and five existing SQLitePCLRaw advisory warnings.
- focused Prompt 038 boundary audit: 2 passed
- Core: 120 passed
- Avalonia: 471 passed
- API: 9 passed
- total: 600 passed, 0 failed, 0 skipped
- `git diff --check`: passed

All verification remained offline and used isolated databases. It did not
contact TUI, launch a browser or access Robin's database.

### Manual Acceptance

Robin manually confirmed the complete checklist on 18 July 2026, including
Save Cruise/Record Observation independence, Saved Cruises organisation,
evaluation and favourite controls, saved-versus-recorded price presentation,
dismiss/restore/removal, preference editing and persistence, workspace
switching and layout usability.

Prompt 038 is complete. Prompt 039 remains unstarted and is next.
