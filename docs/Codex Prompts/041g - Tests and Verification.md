# Codex Prompt 041g – Tests and Verification

## Verification Prompt

Complete **Step 041g only** from:

```text
docs/AI Playbook/041 - New Itinerary Detection.md
```

Prompts 041a–041f are implemented. Audit the complete New Itinerary evidence
boundary, add only missing deterministic verification, run the full offline
suite and record Robin's manual desktop acceptance. Do not start Prompt 042.

---

## Required Reading

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/037 - Cruise History and Price Tracking.md`
5. `docs/AI Playbook/038 - Saved Cruises and Preferences.md`
6. `docs/AI Playbook/039 - Price Drop Alerts.md`
7. `docs/AI Playbook/040 - Cabin Availability.md`
8. `docs/AI Playbook/041 - New Itinerary Detection.md`
9. every Prompt 041a–041f prompt and Results section
10. discovery Core/Application contracts, SQLite mappings, TUI adapter, fixed
    script, presentation and composition tests

---

## Scope

This is a verification prompt, not another feature prompt. Production changes
are allowed only for a concrete defect found during the audit, and require a
focused regression test. Do not redesign identity, scope, capture, recording,
alerts, persistence or presentation.

## Required Audit

### Evidence and lifecycle

Confirm deterministically that:

- the first accepted check for a compatible scope seeds a baseline and creates
  no first-observed event or alert
- only a later previously unseen stable route identity creates an event
- a changed sailing, price, promotion, offer reference or reappearance does not
  make an already-known route new
- known and unknown criteria are semantic parts of scope identity
- truncated evidence remains visibly bounded and proves no absence
- recording is explicit; capture and local presentation are read-only
- the factual check commits before alert evaluation, and retry repairs a
  missing alert without duplicating factual evidence or alerts
- cancellation or alert failure after commit does not roll evidence back
- restart and concurrent recording preserve the same result

### Capture safety and meaning

Confirm the demonstrated TUI boundary only:

- the supported source starts on an HTTPS `/cruise/packages` result route
- the fixed script is bounded, read-only and emits payload v3
- no click, submission, booking advance, automatic navigation, network call,
  cookie or browser-storage access exists
- only a trusted TUI itinerary URL with a stable itinerary code is eligible
- price v1–v3 and cabin v2–v3 compatibility remains intact
- wording says first observed by Kryten and never claims retailer publication,
  current availability, withdrawal or novelty from absence

### Architecture, schema and composition

Confirm:

- Core/Application contain no Avalonia, EF, SQLite, browser or TUI dependency
- provider query names and payload interpretation remain outside Core/Application
- discovery tables relate only within their factual aggregate
- deleting History, Saved Cruises, cabin evidence or alerts leaves discovery
  evidence intact, and deleting discovery evidence leaves those features intact
- existing migrations fully describe the model; 041f/041g require no migration
- persistence-enabled desktop DI resolves capture, record/alert orchestration,
  local queries, review and New Itineraries presentation
- generic shell composition remains usable without opening Robin's database
- the fifth Cruise mode preserves the existing Discovery, Saved Cruises,
  Alerts and Cabin Availability modes

Record every finding in Results. Do not silently leave a known defect.

---

## Required Automated Verification

Prefer one focused cross-feature SQLite boundary test and explicit composition
assertions rather than duplicating the detailed 041b–041f suites. Tests must be
deterministic, offline and use isolated temporary storage.

Run:

```text
dotnet build KrytenAssist.sln --no-restore --verbosity minimal -m:1
dotnet test KrytenAssist.Core.Tests/KrytenAssist.Core.Tests.csproj --no-build --no-restore --verbosity minimal -m:1
dotnet test KrytenAssist.Avalonia.Tests/KrytenAssist.Avalonia.Tests.csproj --no-build --no-restore --verbosity minimal -m:1
dotnet test KrytenAssist.Api.Tests/KrytenAssist.Api.Tests.csproj --no-build --no-restore --verbosity minimal -m:1
dotnet ef migrations has-pending-model-changes --project KrytenAssist.Infrastructure/KrytenAssist.Infrastructure.csproj --context KrytenAssistDbContext --no-build
git diff --check
```

## Manual Desktop Acceptance

Robin verifies with an accepted local desktop profile:

- open Cruise Discovery and confirm the supported package-results page loads
- capture itinerary evidence and confirm capture alone records nothing
- choose Record Discovery Check and confirm the first comparable check is
  described as a baseline with no New Itinerary alert
- record a later controlled fixture or live result containing a genuinely
  unseen stable route and confirm exactly one New Itinerary entry and alert
- confirm repeat capture, mutable offer changes and reappearance do not create
  another event
- open New Itineraries, inspect scope/evidence/truncation wording, revisit the
  trusted route, then restart and confirm it persists
- exercise Refresh, Cancel and rapid mode switching; confirm last good local
  state is preserved and no background capture/navigation occurs
- confirm existing History, Saved Cruises, Alerts and Cabin Availability still
  work and can be removed independently

Record Robin's date and outcome. A partial check remains partial. Live TUI data
may legitimately contain no unseen itinerary; use a controlled deterministic
fixture for that acceptance case when necessary.

## Documentation Completion

After automated verification, complete Results here, update the Prompt 041
playbook and Roadmap, and create a session handover. Mark manual acceptance
pending until Robin explicitly confirms the complete checklist. Do not start
Prompt 042.

## Exclusions

- new discovery features or source mappings
- polling, scheduling, pagination, scrolling or booking automation
- publication/withdrawal/availability inference
- fuzzy route identity, ranking or recommendations
- cloud sync or external notification delivery
- Prompt 042 implementation

---

## Results

### Status

Automated verification is complete. The complete manual desktop acceptance
checklist remains pending Robin's explicit confirmation, so Prompt 041 remains
open and Prompt 042 has not started.

### Audit Findings

- The existing 041b–041f tests cover stable route identity, scope
  compatibility, baseline behavior, positive-only detection,
  retry/concurrency, post-commit alerts, trusted capture, honest presentation
  and cancellation/generation safety.
- New isolated SQLite checks prove discovery evidence and History, Saved
  Cruises, cabin evidence and alerts are independent in both directions.
  Discovery catalogue-to-occurrence relationships are internal to the factual
  aggregate; valid aggregate removal deletes its catalogue headers before the
  scope cascade.
- Desktop persistence composition resolves the provider-independent itinerary
  capture interface, explicit record/alert orchestration, first-observed detail
  projection, review and New Itineraries presentation together.
- Application references only Core. No Core/Application source contains an
  Avalonia, EF, SQLite, browser, TUI or provider SDK dependency.
- The source catalog uses trusted HTTPS `/cruise/packages`; fixed payload v3
  remains bounded/read-only and compatibility tests retain price v1–v3 and
  cabin v2–v3 support.
- The EF model has no pending changes. No production code, fixed script,
  provider mapping or migration was required; eight migrations remain.

### Automated Verification

- solution build: passed, 0 errors
- Core: 157 passed
- Avalonia/Application/Infrastructure: 587 passed
- API: 9 passed
- total: 753 passed, 0 failed, 0 skipped
- EF pending-model check: passed, no changes
- `git diff --check`: passed
- warnings: five existing `NU1903` advisories for
  `SQLitePCLRaw.lib.e_sqlite3` 2.1.11; no new compiler warning

### Manual Verification

Partial only. Robin confirmed the supported package page loads and understands
that New Itineraries can legitimately remain empty after baseline seeding when
no later comparable capture contains an unseen stable route. The controlled
first-observed/alert, restart, lifecycle and cross-feature desktop checks listed
above have not yet been claimed complete.
