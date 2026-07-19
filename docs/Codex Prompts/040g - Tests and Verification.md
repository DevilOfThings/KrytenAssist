# Codex Prompt 040g – Tests and Verification

## Verification Prompt

Complete **Step 040g only** from:

```text
docs/AI Playbook/040 - Cabin Availability.md
```

Prompts 040a–040f are implemented. This final step audits the complete cabin
evidence boundary, fills only material verification gaps, runs the complete
offline suite and records Robin's manual desktop acceptance. Do not start
Prompt 041 or add new cabin features.

---

## Required Reading

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/037 - Cruise History and Price Tracking.md`
5. `docs/AI Playbook/038 - Saved Cruises and Preferences.md`
6. `docs/AI Playbook/039 - Price Drop Alerts.md`
7. `docs/AI Playbook/040 - Cabin Availability.md`
8. every `docs/Codex Prompts/040a` through `040f` prompt and Results section
9. cabin Core/Application contracts, orchestration and tests
10. cabin/alert/criteria SQLite adapters, migration and tests
11. TUI fixed-script/adapter safety tests and offline fixtures
12. Discovery cabin review, Cabin Availability workspace and desktop DI tests
13. latest Prompt 040 session handovers

---

## Scope

This is an audit and verification prompt, not another feature prompt.

Production changes are permitted only for a concrete defect discovered while
verifying the accepted 040a–f contract. Any correction must be minimal,
documented and covered by a failing-then-passing focused test.

Do not redesign cabin identity, evidence states, context compatibility,
capture, recording, preferences, alerts, persistence or presentation.

---

## Required Cross-Boundary Audit

### Factual and Personal-State Independence

Prove as coherent isolated workflows that:

1. cabin capture reads the currently displayed supported page but records
   nothing
2. Record Cabin Observation commits only the cabin factual timeline before
   evaluating derived alerts/criteria
3. post-commit evaluation cancellation/failure never rolls back cabin evidence
4. cabin recording does not create or mutate price History or personal state
5. changing/dismissing/removing Saved Cruises or preferences does not rewrite or
   remove cabin evidence
6. deleting price History does not remove cabin observations
7. deleting cabin History does not change price History, Saved Cruises,
   preferences or retained alerts
8. Cabin Availability presentation remains read-only

Use isolated SQLite and Application use cases where practical. Never access
Robin's database.

### Evidence, Context and Meaningful Change

Audit deterministic coverage proving:

- all five existing cabin categories are present in canonical order
- Available, Unavailable and Unknown remain distinct
- Partial evidence never turns omitted categories into Unavailable
- Complete evidence requires one explicit state for every supported category
- series identity includes sailing, retailer and complete known/unknown search
  context
- incompatible retailer, occupancy, package, airport or cabin-quantity context
  never merges or compares
- equivalent-current evidence advances Last checked/latest reference without a
  duplicate snapshot
- meaningful state/coverage changes append; a recurring earlier fingerprint is
  retained after an intervening change
- explicit Available↔Unavailable is an inventory transition while any
  Unknown edge remains a knowledge change
- first-seen Available creates history but no `became available` alert

### Capture Safety

Confirm the demonstrated TUI boundary only:

- `/cruise/packages` is the accepted page shape
- `1 x Inside Cabin (Cheapest available)` maps Inside Available, every other
  category Unknown and Partial coverage
- missing cabin text, unfamiliar wording and `All gone` create no observation
- candidate itinerary references require trusted HTTPS TUI host and a stable
  itinerary code
- the fixed script is bounded and read-only
- no click, form submission, booking advance, private endpoint, cookie/session
  access or network call exists in automated tests
- existing price capture remains independently compatible

### Preferences, Alerts and Presentation

Audit deterministic coverage proving:

- preferred cabins are OR; month/budget/cabin groups are AND
- cabin-only preference requires explicit compatible Available evidence
- latest exact-sailing/same-retailer cabin evidence is selected without merging
  contexts
- only Shortlisted explicit preferred-category Available↔Unavailable
  transitions create Cabin Availability alerts
- Saved Criteria and Cabin Availability alerts remain separate typed events
- retry after post-commit failure converges without duplicate alerts
- all agreed explicit Record/Save/Restore/Preferences boundaries supply cabin
  evidence when compatible
- each retailer/context series remains a separate presentation row
- all context Unknown values remain visibly Unknown
- Partial/Complete wording and preferred-cabin annotations are honest
- latest meaningful evidence and later Last checked remain distinct
- refresh/cancellation/generation guards preserve last good local state
- there is no polling, monitoring, automatic recording or booking action

### Architecture, Schema and Composition

Confirm:

- Core/Application expose no Avalonia, EF Core, SQLite, browser or provider SDK
  types
- Application owns cabin capture/persistence/orchestration abstractions
- TUI page/payload interpretation remains in Infrastructure/Avalonia boundaries
- cabin series/observation/state tables have no relationship or cascade to
  History, Saved Cruises, preferences or alerts
- cabin alert detail ownership is only through its alert header
- persistence-enabled desktop resolves cabin capture, recording, Cabin
  Availability and shared alert coordination without service location
- generic shell remains resolvable without persistence
- no Prompt 040 path implies live/unattended availability

Record findings in Results. Do not silently leave a known defect.

---

## Required Automated Verification

Add only missing tests required to make the combined boundary explicit. Prefer
one focused SQLite boundary fixture rather than duplicating 040b–f tests.

Run:

```text
dotnet build KrytenAssist.sln --no-restore --verbosity minimal -m:1
dotnet test KrytenAssist.Core.Tests/KrytenAssist.Core.Tests.csproj --no-build --no-restore --verbosity minimal -m:1
dotnet test KrytenAssist.Avalonia.Tests/KrytenAssist.Avalonia.Tests.csproj --no-build --no-restore --verbosity minimal -m:1
dotnet test KrytenAssist.Api.Tests/KrytenAssist.Api.Tests.csproj --no-build --no-restore --verbosity minimal -m:1
git diff --check
```

All tests remain deterministic and offline. Inspect migration/model metadata,
fixed-script content and repository use sites in addition to executing tests.

---

## Manual Desktop Acceptance Checklist

Robin verifies against a disposable or accepted local desktop profile:

### Capture and recording

- load a live supported TUI `/cruise/packages` result page
- Capture and confirm a card showing `1 x Inside Cabin (Cheapest available)`
  produces Inside Available, four Unknown states and Partial evidence
- confirm capture alone changes no local cabin history
- choose Record Cabin Observation and confirm successful independent feedback
- open Cabin Availability and confirm the new series appears without restart

### Evidence and history

- verify sailing, TUI source, evidence time, all five states and search context
- confirm adults/children/ages/package/airport/cabin quantity match the search
- confirm Partial wording says Unknown is not Unavailable
- record equivalent evidence and confirm count is unchanged while Last checked
  advances
- confirm different search contexts remain separate rows
- restart and confirm cabin evidence/history persists

### Preferences and alerts

- set one or more preferred cabin types and verify the current-series OR
  annotation
- exercise one explicitly supported preferred-category transition fixture or
  controlled workflow if available
- confirm Cabin Availability and Saved Criteria alerts remain independently
  described
- confirm Save/Restore/Preferences actions do not record cabin evidence

### Lifecycle and safety

- exercise Refresh, Cancel and retry where practical
- switch rapidly among all Cruise modes and confirm stale loads do not publish
- confirm Cabin Availability has no edit/delete/record controls
- confirm no background navigation, capture, monitoring or booking action occurs
- confirm existing Discovery, History, Saved Cruises and Alerts remain usable

Record Robin's date and outcome in Results. A partial check is recorded as
partial, not complete. If any item fails, keep Prompt 040g/040 open and correct
only an in-scope defect.

---

## Documentation Completion

When automated verification passes:

- complete automated Results and audit findings here
- update Prompt 040 Results and Lessons Learned
- update Roadmap with exact automated status
- create the final Prompt 040 session handover
- leave Prompt 041 unstarted

Only after Robin confirms the complete manual checklist:

- mark Prompt 040g and Prompt 040 complete
- record the manual verification date and accepted limitations
- identify Prompt 041 as next without implementing it

---

## Allowed Changes

```text
KrytenAssist.Core.Tests/Cruises/ (audit coverage only)
KrytenAssist.Avalonia.Tests/Application/Cruises/ (audit coverage only)
KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/ (audit coverage only)
KrytenAssist.Avalonia.Tests/Cruises/Tui/ (audit coverage only)
KrytenAssist.Avalonia.Tests/ViewModels/ (audit coverage only)
KrytenAssist.Avalonia.Tests/DependencyInjection/ (audit coverage only)
docs/Codex Prompts/040g - Tests and Verification.md
docs/AI Playbook/040 - Cabin Availability.md
docs/Roadmap.md
docs/Session Handovers/
```

Production changes require a concrete audited defect and focused regression
test. Do not stage, commit, push, discard or overwrite unrelated work.

---

## Exclusions

- richer cabin-selection adapters or inferred category states
- automatic navigation, capture, recording, booking or monitoring
- inventory counts, forecasts or recommendations
- new retailer/context/category identity behavior
- manual deletion/editing of cabin evidence
- cloud sync or external notifications
- Prompt 041 or later roadmap implementation
- visual redesign or unrelated cleanup

---

## Results

### Status

Complete. All 713 offline tests pass and no production defect was found. Robin
confirmed the complete manual desktop acceptance checklist on 19 July 2026.
Prompt 040g and Prompt 040 are complete; Prompt 041 remains unstarted.

### Automated Audit Summary

- Extended the committed SQLite boundary fixture with the missing reverse
  deletion case. Its two tests now prove independence in both directions:
  cabin history survives price-History/Saved-Cruise/alert removal, while
  cabin-history removal leaves price evidence, saved evaluation and a retained
  alert unchanged after restart.
- Existing deterministic tests cover the canonical states, context identity,
  Partial/Complete invariants, inventory versus knowledge changes, preferred
  alert detection, committed-current orchestration, retry safety, restart,
  recurrence, ordering, cancellation and concurrent recording.
- Existing TUI tests cover demonstrated Inside Cabin wording, missing/
  unfamiliar/All-gone rejection, trusted references, bounded candidates and
  price-payload compatibility.
- Existing presentation tests cover explicit context/state wording, preferred
  annotations, separate ordered series, exact selection retention and
  degraded/failure state preservation.
- No automated test contacted TUI, launched a booking flow or accessed Robin's
  database.

### Architecture and Schema Findings

- Core/Application cabin contracts contain no Avalonia, EF Core, SQLite,
  WebView, AngleSharp, OpenAI or provider SDK dependency.
- Cabin repository/orchestration abstractions remain Application-owned; the
  SQLite adapter is scoped through Infrastructure dependency injection.
- Schema metadata proves cabin storage has only internal child relationships,
  with no foreign key to History, Saved Cruises, preferences or alerts. Cabin
  alert detail belongs only to its alert header.
- The fixed TUI script performs bounded DOM reads only: no click, submission,
  fetch/XHR, cookie, storage or booking action.
- Desktop composition resolves the cabin use cases, recording orchestration and
  Cabin Availability ViewModel without service location.
- No production correction, schema change or migration was required.

### Files Added or Updated

- this prompt
- updated `KrytenAssist.Avalonia.Tests/Infrastructure/Persistence/Prompt040BoundaryTests.cs`
- `docs/Session Handovers/2026-07-19 Session 032.md`

### Build and Automated Tests

- focused Prompt 040 boundary/composition audit: 4 passed
- solution build: passed with 0 errors and five existing `SQLitePCLRaw`
  advisory warnings
- Core: 147 passed
- Avalonia/Application/Infrastructure: 557 passed
- API: 9 passed
- total: 713 passed, 0 failed, 0 skipped
- `git diff --check`: passed

All automated verification remained offline and used isolated test databases.

### Manual Acceptance

Complete on 19 July 2026. Robin confirmed the full desktop checklist, including
live TUI package loading, capture review, explicit recording, Cabin Availability
history and context separation, preference presentation, restart persistence
and mode/lifecycle safety.
