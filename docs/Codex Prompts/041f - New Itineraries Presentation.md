# Codex Prompt 041f – New Itineraries Presentation

## Implementation Prompt

Implement **Step 041f only** from:

```text
docs/AI Playbook/041 - New Itinerary Detection.md
```

Prompts 041a–041e are complete. Wire the existing trusted itinerary capture
adapter and post-commit recording/alert orchestration into the Avalonia Cruise
Discovery workflow, then add a durable local New Itineraries workspace and the
remaining Alert Centre presentation. Do not change capture identity,
persistence schema or browser safety rules.

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
9. Codex Prompts 041a–041e
10. existing Cruise workspace, browser bridge, capture review, History, Cabin
    Availability, Alert Centre and settings ViewModels/views/composition tests

---

## Experience Boundary

The visible workflow is deliberate and local:

```text
manually load trusted TUI Cruise Packages page
        ↓
Capture Loaded Cruises
        ↓
review itinerary scope, bounds, Ready and rejected candidates
        ↓
Record Discovery Check
        ↓
factual result: baseline / no unseen routes / first observed / already recorded
        ↓ independently
New Itinerary alert result
        ↓
review durable New Itineraries workspace or Alert Centre
```

Capture and review must not mutate the catalogue. Only the explicit
`Record Discovery Check` command may call
`RecordCruiseDiscoveryCheckAndEvaluateAlerts`.

Do not add automatic recording after capture, navigation or startup. Do not add
scheduled/background capture, page refresh, scrolling, pagination, polling,
notifications or private endpoint access.

---

## Shared Capture Integration

Extend the current browser presentation composition with:

```text
ICruiseItineraryPageCaptureService
RecordCruiseDiscoveryCheckAndEvaluateAlerts
```

The existing fixed payload is already passed independently to price and cabin
adapters. Pass that same bounded payload, trusted source reference and one
shared capture-time value to the itinerary adapter. Do not execute another
script or change payload v3.

Price/cabin/itinerary mapping outcomes remain independent:

- itinerary failure must not erase a successful price or cabin capture
- price/cabin failure must not erase a successful itinerary review
- cancellation and generation checks prevent stale results appearing after a
  later capture, navigation, close or mode deactivation
- one adapter exception becomes a controlled message for that evidence type
- all capture state clears consistently on a new capture, navigation, close or
  source change

The itinerary request continues to use the current selected source identifier,
provider-independent `CruiseSource`, current trusted HTTPS address and
application clock. No TUI URL parsing belongs in a ViewModel.

---

## Discovery Check Review

Add focused presentation models for the completed
`CruiseItineraryCaptureBatchResult`. Keep Application/Core objects immutable;
presentation state belongs in Avalonia.

The Discovery review must show:

- source and operator
- source surface
- observation time
- scope summary containing every semantic criterion and whether it is Known or
  Unknown
- bounded-result wording and explicit truncation state
- total, Ready, Ineligible, Incomplete and Failed candidate counts
- each candidate label, state, reason/missing fields and stable provider
  itinerary id where Ready
- optional Ready evidence such as title, ship, departure date, duration,
  departure port and itinerary summary
- an explanation that routes are identified by operator plus provider
  itinerary id; dated sailings and offers do not create separate itineraries

Known multi-values display in canonical order. Unknown means the filter could
not be recovered; it must not be presented as “any” or a provider default. Raw
TUI query names, raw JSON and payload text are never displayed.

Use these exact concepts in user-facing copy:

- `First observed by Kryten`
- `This does not prove when TUI published the itinerary.`
- `Only routes present in this bounded capture are evidence.`
- `Absence does not mean withdrawn, cancelled or sold out.`

When truncated, say that only the bounded displayed results were captured and
that sort/order may expose a previously unseen older route. Do not use `newly
published`, `released today`, `just added`, `live` or `currently available`.

### Whole-Check Recording

A discovery check is a comparable catalogue observation, not a shortlist.
Unlike price observations, itinerary candidates are not individually
selectable for recording.

Build exactly one `CruiseDiscoveryCheck` from:

- the captured scope
- the exact shared capture time
- every Ready occurrence
- every non-Ready candidate converted to one bounded deterministic rejection
- the exact truncation flag

Do not silently omit a rejected candidate. Use stable bounded candidate keys
and reasons derived from the controlled capture result, never raw exceptions.
Preserve deterministic ordering expected by Core.

Enable `Record Discovery Check` only when:

- capture status is Completed
- scope is present
- at least one candidate is Ready
- no capture or recording operation is active
- the review still belongs to the current trusted page/generation

Do not permit selecting a subset, editing scope/identity evidence or recording
unsupported/incomplete/wholly failed capture. A mixed capture may record its
Ready routes plus explicit rejections.

---

## Recording Result Presentation

Keep recording cancellation separate from capture cancellation. Provide
progress, a Cancel Recording action, command locking and generation safety.
Prevent double invocation while active; exact retry after completion remains
allowed because 041e is idempotent.

Map factual recording states honestly:

- `BaselineSeeded`: “Discovery baseline recorded. No New Itinerary alerts are
  created from the first accepted check for this scope.”
- `RecordedNoNewItineraries`: “Discovery check recorded. Every eligible route
  was already known to Kryten.”
- `RecordedWithFirstObserved`: “Discovery check recorded. Kryten first observed
  N itinerary/itineraries in this check.”
- `AlreadyRecorded`: “This exact discovery check was already recorded.”
- `Cancelled`: recording was cancelled and must not claim a commit
- `Failed`: factual evidence could not be recorded locally

Then present the independent alert outcome without replacing the factual
message:

- NotRequired
- Disabled
- Success with created/existing counts
- Cancelled
- Failed with “The discovery check remains recorded.”

After any committed factual state, refresh New Itineraries and unread alert
coordination. Alert refresh failure must not change the recording result.

---

## New Itineraries Workspace

Add `NewItineraries` as a fifth `CruiseWorkspaceMode`, alongside Discovery,
Saved Cruises, Alerts and Cabin Availability. Add a `New Itineraries` selector
to the existing Cruise header, using a wrapping/responsive layout so the
tablet-sized workspace does not require horizontal clipping.

Create a focused `CruiseNewItinerariesViewModel` and view. Register it through
desktop DI and inject it into the parent Cruise workspace; do not instantiate
repository/use-case dependencies in XAML or code-behind.

Lifecycle follows existing modes:

- first activation loads local data
- later activation refreshes when stale/explicitly invalidated
- explicit Refresh and Cancel Loading commands
- deactivation cancels only presentation loading
- deterministic selection is preserved across refresh when the same catalogue
  key remains
- restart loads solely from local SQLite without opening TUI

### Application Read Model

The current catalogue query contains first/latest occurrence evidence, while
scope/check explanation lives in retained checks. Add one focused
Application-owned query/read projection if needed. It may compose the existing
`ICruiseDiscoveryRepository` catalogue and check queries, but must not add a new
table or expose EF entities.

For every first-observed catalogue entry, resolve and validate the exact
retained check/event that produced its non-null first-observed event key. Return
the entry plus its discovery scope/check evidence, truncation and rejection
context. Missing or contradictory retained evidence is a controlled local data
failure, not silently guessed scope information.

Do not join discovery to alerts, History, Saved Cruises or cabin persistence.

### List and Detail

Present first-observed entries newest first, using UTC instant ordering with the
existing deterministic tie-breakers. Each item should show:

- preferred title, falling back to provider itinerary id
- operator and provider itinerary id
- first seen and last seen application times
- source name
- optional ship/date/duration summary clearly labelled as captured occurrence
  evidence, not route identity

The selected detail shows:

- `New itinerary observed`
- `First observed by Kryten on …`
- first/latest captured evidence and times
- trusted source and semantic scope explanation
- whether the confirming check was truncated or contained rejected candidates
- the publication/absence disclaimer
- exact provider itinerary id
- optional bounded route/ship/port/summary fields

Provide distinct loading, cancellation, local failure, never-recorded empty and
selection-empty states. Empty copy should explain how to manually capture and
record a baseline, then a later comparable check. A baseline itself does not
appear in the New Itineraries list.

---

## Trusted Revisit

If the selected entry's latest occurrence has a non-empty trusted source
reference, show `Open in Discovery`.

The action must:

1. validate the address through the existing source catalogue and
   `CruiseTrustedHostPolicy`
2. switch the parent workspace to Discovery
3. navigate through the existing explicit trusted browser navigation path

Hide/disable the action for absent, malformed, unsupported or untrusted
references. Never call a shell browser directly from the item ViewModel, bypass
the host policy or navigate automatically on selection/activation.

The retained reference revisits captured evidence only. Do not label it as a
guarantee that the offer or itinerary is still available.

---

## Alert Centre and Settings Completion

Complete the existing typed Alert Centre presentation without changing alert
domain/persistence:

- add a `New itineraries` type filter
- add a `Cabin availability` type filter so every already-supported alert type
  has an explicit filter
- New Itinerary items use `New itinerary observed`, provider itinerary id,
  source and `First observed by Kryten on …`
- show semantic scope fingerprint/evidence information from typed alert details
  without claiming publication
- expose `New Itinerary alerts` in Alert Settings
- expose the already persisted `Cabin Availability alerts` setting alongside
  it, rather than allowing visible settings saves to hide/reset either flag
- preserve unsaved-change, cancellation, validation and future-evaluations-only
  behavior

Saving any visible setting must preserve every other setting exactly. Existing
alerts are not backfilled, recalculated or removed. The unread badge and current
lifecycle controls continue to work for New Itinerary alerts.

---

## MVVM and Composition

- all commands and state live in ViewModels
- code-behind remains limited to existing browser/view plumbing
- use constructor injection and existing DI extensions
- avoid service location and manual `Program.cs` registration
- retain cancellation/generation conventions used by current Cruise ViewModels
- keep provider URL/JSON/DOM logic in Infrastructure
- do not add database access to Avalonia
- use bounded collections and scroll viewers inside the existing resizable
  Cruise workspace

Do not redesign completed Discovery, History, Saved Cruises, Alerts or Cabin
Availability experiences beyond the integrations explicitly required above.

---

## Required Offline Tests

### Discovery Capture and Review

- same payload independently maps price, cabin and itinerary evidence
- one shared capture time is used for itinerary scope/check occurrences
- completed Ready, mixed, truncated, duplicate-route and rejected review states
- unsupported/incomplete/failed itinerary mapping does not erase price/cabin
- new capture/navigation/close/cancellation prevents stale review mutation
- whole-check construction includes all Ready and every rejection
- subset selection/editing is unavailable
- command enablement and operation locking

### Recording Presentation

- baseline, no-new, first-observed and already-recorded factual messages
- separate Disabled/Success/Cancelled/Failed alert messages
- committed factual success remains visible after alert failure
- exact retry does not duplicate visible result counts
- successful commit refreshes local New Itineraries and unread alerts
- cancellation/generation/disposal behavior

### New Itineraries

- Application read projection resolves exact scope/check/event evidence
- contradictory/missing event evidence fails in a controlled way
- newest-first/tie ordering and selection preservation
- first/latest evidence and honest labels
- baseline-only/empty/loading/cancelled/failure states
- local restart behavior does not invoke browser/network capture
- trusted revisit visible only for policy-approved addresses
- revisit switches to Discovery and uses existing navigation route

### Alert Centre and Settings

- New Itinerary and Cabin Availability filters
- typed route item/detail formatting never dereferences a sailing subject
- publication/availability language is absent
- New Itinerary and Cabin Availability settings load, edit, cancel and persist
- saving one setting preserves all other values
- unread/lifecycle coordination includes New Itinerary alerts

### Composition and Regression

- desktop DI resolves the fifth workspace and all required use cases
- parent mode activation/deactivation and responsive selector behavior
- fixed script/safety assertions remain unchanged
- existing price, cabin, History, Saved Cruises and alert tests remain green

Use deterministic offline fakes/fixtures and isolated temporary SQLite only.
Never load TUI or Robin's production database in automated tests.

---

## Manual Verification

Document, but do not perform automatically, a desktop check covering:

1. load a supported trusted TUI Cruise Packages URL manually
2. capture and review scope, Ready/rejected counts and truncation wording
3. record the first comparable check and see baseline/no-alert wording
4. record a later fixture/page state containing an unseen itinerary
5. verify the factual result remains clear independently of its alert outcome
6. open New Itineraries, inspect first/last evidence and scope explanation
7. use a trusted `Open in Discovery` action
8. filter/read/dismiss/restore a New Itinerary alert
9. disable/re-enable the New Itinerary setting and confirm future-only wording
10. restart and verify local New Itineraries/history without loading TUI

Do not mark manual verification complete unless Robin explicitly confirms it.

---

## Required Documentation Updates

Complete Results, update the Prompt 041 playbook and Roadmap, and create a
session handover. Identify Prompt 041g as next without implementing its audit.

---

## Exclusions

- schema migration or discovery/alert persistence redesign
- automatic candidate selection, recording or navigation
- scheduled/background browsing, polling or notifications
- new scripts, payload versions, DOM selectors or TUI URL parsing
- pagination, scrolling, clicking, form submission or booking flow
- publication, withdrawal, cancellation, sold-out or current-availability
  inference
- fuzzy route identity or merging different scopes
- edit/delete discovery evidence
- email, SMS, push or OS notification delivery
- Prompt 041g final audit or Prompt 042 combined Dashboard

---

## Results

### Status

Complete on 20 July 2026. The Avalonia Cruise workspace now supports explicit
whole-check itinerary recording, durable local New Itineraries review, trusted
revisit and complete typed alert filters/settings.

### Files Modified

- Application discovery presentation contracts/query and DI
- itinerary capture review/item ViewModels and Discovery XAML integration
- New Itineraries item/workspace ViewModels and view
- Cruise parent mode, lifecycle, trusted revisit and desktop composition
- Alert Centre item/filter/settings ViewModels and XAML
- focused Application, presentation, settings and composition tests
- this prompt, Prompt 041 playbook, Roadmap and Session Handover 037

### Build and Tests

- solution build: passed with 0 errors and seven existing warnings
- Core: 157 passed
- Avalonia/Application/Infrastructure: 585 passed
- API: 9 passed
- total: 751 passed, 0 failed, 0 skipped
- `git diff --check`: passed

### Implementation Notes

- The same fixed payload and capture time feed independent price, cabin and
  itinerary adapters; no script or provider mapping changed.
- Discovery checks include every Ready occurrence and every controlled
  rejection. There is intentionally no itinerary subset selection.
- The fifth workspace reads retained local discovery evidence and validates the
  exact confirming event/check through an Application projection.
- Trusted revisit is policy-checked twice: before presentation command
  enablement and again at the existing browser navigation boundary.
- Manual desktop verification is documented but not marked complete.

### Next

Prompt 041g – Tests and Verification.
