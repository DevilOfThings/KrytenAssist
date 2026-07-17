# Codex Prompt 037h-e – Multiple Cruise Deals Tests and Verification

## Implementation Prompt

Implement **Step 5 only** from:

```text
docs/AI Playbook/037h - Multiple Cruise Deals Handling.md
```

Prompts 037h-a through 037h-d are complete and committed. Prompt 037h-d is
committed as `c3fece1`.

The latest verified automated baseline is:

```text
Core: 105 passed
Avalonia: 415 passed
API: 9 passed
Total: 529 passed, 0 failed, 0 skipped
```

This is a final verification and documentation step. Do not redesign the
multi-Cruise workflow, begin Prompt 038, or add unrelated presentation work.

---

## Important Naming

This is:

```text
037h-e – Multiple Cruise Deals Tests and Verification
```

It is not the already completed:

```text
037e – Cruise History Presentation
```

The `h` identifies the multiple-deals extension to Prompt 037.

---

## Required Reading

Read these files in order before changing code:

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/036 - Cruise Discovery and Capture.md`
5. `docs/AI Playbook/037 - Cruise History and Price Tracking.md`
6. `docs/AI Playbook/037h - Multiple Cruise Deals Handling.md`
7. `docs/Codex Prompts/037h-a - Multi-Cruise Capture Contract.md`, including
   Results and Lessons Learned
8. `docs/Codex Prompts/037h-b - TUI Multi-Card Capture Adapter.md`, including
   Results and Lessons Learned
9. `docs/Codex Prompts/037h-c - Multi-Cruise Capture Review.md`, including
   Results and Lessons Learned
10. `docs/Codex Prompts/037h-d - Batch Observation Recording.md`, including
    Results and Lessons Learned
11. the current batch capture, TUI adapter, capture review, batch recording and
    Cruise History tests
12. `docs/Session Handovers/2026-07-17 Session 018.md`

Treat the committed implementation and passing tests as evidence. Do not begin
production changes until the existing behavior and its test coverage are
understood.

---

## Goal

Verify and close Prompt 037h – Multiple Cruise Deals Handling.

Demonstrate that Kryten can, through an explicit user action, capture the
currently loaded TUI deal cards, independently review valid and incomplete
results, explicitly record selected or all valid observations, and refresh
local Cruise History once after useful recording outcomes.

The supported live page is:

```text
https://www.tui.co.uk/cruise/deals/voyager-cruises
```

The final evidence must prove:

- product-card evidence cannot leak between cards
- duplicate itinerary links become one ordered candidate per exact address
- the ten-card boundary, truncation and partial results are honest
- the original single-Cruise capture and recording workflow remains compatible
- capture and recording reject stale late results and respect cancellation
- batch recording retains independent per-row outcomes and does not roll back
  unrelated successes
- History refreshes once, not once per recorded observation
- automated tests remain wholly offline and isolated from Robin's real database
- Robin manually verifies a supported live multi-deal workflow
- the Playbook, Roadmap, prompt Results and Session Handover consistently
  describe the completed capability and leave Prompt 038 unstarted

---

## Scope

This step owns:

- auditing the existing 037h-a through 037h-d implementation and tests
- adding only focused deterministic regression tests for verified coverage gaps
- correcting only a genuine defect exposed by verification
- focused, full solution and build verification
- guided manual verification with Robin on the live Voyager page
- completion of the 037h Playbook Results and Lessons Learned
- Roadmap status correction and completion summary
- this prompt's Results and Lessons Learned
- a new session handover for the completed 037h extension

This step does **not** own:

- automatic capture or recording
- clicking Load More, changing filters or capturing unloaded cards
- a live-page fixture, browser automation or TUI network tests
- reworking the multi-Cruise screen layout
- ratings, favourites, notes, preferences, recommendations or Prompt 038 work
- original/discounted/booking-level discount model changes
- persistence schema, migration or Cruise identity redesign unless a proven
  Prompt 037h defect requires a minimal correction

The known multi-Cruise review layout could be improved later. Record it as a
follow-up only; it must not expand this verification prompt.

---

## Allowed Changes

The expected outcome is documentation-only plus any narrowly justified tests.

Verification may update:

```text
docs/AI Playbook/037h - Multiple Cruise Deals Handling.md
docs/Roadmap.md
docs/Codex Prompts/037h-e - Multiple Cruise Deals Tests and Verification.md
docs/Session Handovers/
```

Tests may be created or updated only under:

```text
KrytenAssist.Avalonia.Tests/Application/Cruises/
KrytenAssist.Avalonia.Tests/Cruises/Tui/
KrytenAssist.Avalonia.Tests/ViewModels/
```

Production code may change only when verification proves a real defect. Any
such correction must be minimal, include a focused deterministic regression
test, preserve existing architecture and be explicitly reported.

Do not stage, commit, push, discard or overwrite Robin's work.

---

## Verification Process

### 1. Establish the Starting State

Run:

```text
git status --short
git log --oneline --max-count=10
```

Preserve any existing work. Confirm the four prerequisite 037h commits and
their Results/Lessons Learned are present before continuing.

### 2. Verify Capture Contract and TUI Card Isolation

Inspect the provider-independent batch contracts, TUI adapter and their tests.

Confirm:

- every result comes from a demonstrated `[data-testid="product-card"]`
  boundary
- no page-wide ship, price or promotion fallback can fill another card
- each candidate has its own trusted exact itinerary reference
- repeated links within one card and duplicate cards deduplicate by exact
  address without changing first-seen order
- at most ten loaded candidates cross the Application boundary
- truncation states exactly that only the first ten currently loaded deals were
  captured
- ready, incomplete, failed, unsupported and cancelled outcomes remain honest
- no browser, DOM, HTML, JavaScript or TUI type leaks into Core or Application

Use fictional fixed payloads and fixed-script assertions. Do not contact TUI.

### 3. Verify Review and Single-Cruise Compatibility

Inspect the Cruise browser/review ViewModels and tests.

Confirm:

- multi-candidate results retain deterministic source order
- incomplete candidates remain visible but cannot be selected or recorded
- Select All Ready and Clear Selection affect only eligible rows
- Open at TUI uses only each candidate's validated trusted address
- an exactly one clean, non-truncated candidate retains the established
  single-Cruise review and Record Observation flow
- a truncated single candidate remains a batch result rather than silently
  becoming the single-Cruise path
- capture cancellation, navigation, refresh, close and source changes clear
  stale review state without allowing late completion to restore it

### 4. Verify Batch Recording and History Refresh

Inspect the batch recording ViewModel, `RecordCruiseObservation` use case,
Cruise History refresh behavior and tests.

Confirm:

- Record Selected snapshots eligible selected rows in stable review order
- Record All Observations snapshots all eligible Ready rows in stable order
- the existing Application use case is invoked once per attempted observation
- execution is sequential, never parallel or a batch database transaction
- First, Changed, Already Current, Failed and Cancelled outcomes remain on the
  corresponding row
- failure does not roll back or prevent later unrelated observations
- cancelled and unattempted candidates remain retryable; completed candidates
  do not run again in the same review
- navigation/capture replacement and explicit cancellation reject stale late
  results without overwriting replacement review state
- History refreshes exactly once when any result is First, Changed or Already
  Current, and does not refresh for all-failed or pre-attempt cancellation
- the preferred History selection is deterministic

### 5. Add Only Missing Deterministic Tests

Do not duplicate existing tests merely to increase a count. Add tests only if
the audit finds a real acceptance-criterion gap.

All tests must be offline and deterministic. They must use fixed payloads,
controlled incomplete tasks, explicit cancellation tokens, fixed clocks where
needed, and in-memory or temporary SQLite only where recording integration is
being tested.

No test may:

- contact TUI or any network endpoint
- launch a browser or NativeWebView
- rely on live page structure beyond the fixed script contract
- access Robin's configured desktop database

### 6. Build and Automated Verification

Run the relevant focused tests first, including batch contract, TUI adapter,
capture review, batch recording, single-capture lifecycle and Cruise History
tests.

Then run:

```text
dotnet build KrytenAssist.sln --no-restore
dotnet test KrytenAssist.sln --no-build --no-restore
```

If the standard full run is affected by an environmental parallel-runner issue,
record the exact symptom and additionally run the stable equivalent:

```text
dotnet test KrytenAssist.sln --no-build --no-restore --disable-build-servers -m:1
```

Do not conceal actual test failures. Resolve only failures caused by this
prompt's verified scope; report unrelated pre-existing failures with evidence.

### 7. Guided Manual Verification with Robin

Ask Robin to perform and report the following live workflow; do not claim it
passed until Robin confirms the observed result.

1. Start Kryten and select **Cruise of the Week**.
2. Open the embedded TUI browser and navigate to the supported Voyager deals
   page.
3. Choose **Capture Loaded Cruises**.
4. Confirm multiple distinct cards appear in page order, with no repeated
   first-card ship/price/promotion details, and that any truncation wording is
   honest.
5. Select one or more Ready cards. Confirm incomplete cards cannot be recorded.
6. Use **Open at TUI** for one Ready card and confirm it opens that card's own
   itinerary.
7. Use **Record Selected** or **Record All Observations** intentionally.
8. Confirm each attempted row receives an understandable outcome and Recorded
   Cruise History refreshes once with the affected observation available.
9. Confirm the original single Cruise of the Week page can still be captured
   and recorded through its existing single-Cruise workflow.

The manual run may write observations to Robin's local history only when Robin
chooses a Record command. Never automate this action or fabricate the outcome.

### 8. Complete Documentation and Handover

Only after automated and manual evidence is complete:

- update all 037h Playbook Results sections, including actual test totals,
  live evidence and any production corrections
- complete this prompt's Results and Lessons Learned with factual evidence
- update `docs/Roadmap.md` to mark the 037h multi-deals extension complete;
  leave Prompt 038 as the next unstarted task
- create the next dated Session Handover describing the completed workflow,
  verified test results, manual evidence, known limits and Prompt 038 as next
- record the deferred multi-Cruise review layout refinement as a later
  presentation follow-up, without adding it to Prompt 038 by implication

Do not mark 037h complete if manual verification remains outstanding.

---

## Acceptance Criteria

Prompt 037h is complete only when:

- all Playbook acceptance criteria for 037h are verified
- no candidate receives cross-card evidence
- loaded-card bounds, deduplication, ordering, truncation and partial results
  are proven by deterministic tests
- the existing single-Cruise workflow remains compatible
- recording is explicit, sequential and independently controlled per candidate
- cancellation and stale-result behavior are deterministic and safe
- one useful batch produces no more than one History refresh
- focused tests, build and complete offline regression suite pass
- Robin confirms a supported live multi-deal workflow, or the documentation
  honestly records that the original volatile Voyager URL changed and names the
  replacement verified page
- Results, Lessons Learned, Roadmap and Session Handover are complete and
  consistent
- Prompt 038 has not started

---

## Results

> Complete after implementation and verification.

### Status

Complete.

### Verification Findings

The audit found no production defect. Existing deterministic tests cover exact
product-card isolation, candidate order, deduplication, bounds, truncation,
partial results, the single-Cruise route, capture cancellation/stale completion,
sequential partial recording, retry behavior and one History refresh after
useful outcomes.

### Production Corrections

None.

### Build

Passed: `dotnet build KrytenAssist.sln --no-restore` with 0 errors and the five
existing NU1903 warnings for `SQLitePCLRaw.lib.e_sqlite3`.

### Tests

Focused multi-Cruise suite: 164 passed, 0 failed, 0 skipped.

Complete offline suite passed with the stable single-worker runner:

```text
Core: 105 passed
Avalonia: 415 passed
API: 9 passed
Total: 529 passed, 0 failed, 0 skipped
```

### Manual Verification

Passed on 17 July 2026. The volatile Voyager page was no longer discoverable,
so Robin verified the equivalent multi-deal capture and explicit recording
workflow at:

```text
https://www.tui.co.uk/destinations/deals
```

Robin successfully captured multiple offers and recorded observations.

The Italy destination page was honestly rejected because it uses TUI's separate
`small-product-card` template, not the verified `product-card` template. It is
a future tested source-template extension, not a verification defect.

### Files Updated

- `docs/AI Playbook/037h - Multiple Cruise Deals Handling.md`
- `docs/Roadmap.md`
- this prompt
- `docs/Session Handovers/2026-07-17 Session 019.md`

### Session Handover

Session 019 records completion, test evidence, manual evidence and the known
TUI template/layout follow-ups.

---

## Lessons Learned

- The existing tests were already targeted at the 037h acceptance criteria, so
  verification required no duplicate tests or production changes.
- TUI's visible itinerary cards are not one universal template. Exact,
  independently tested card boundaries are safer than broad selector fallbacks.
- The live Voyager address is volatile; a successful equivalent manual workflow
  plus deterministic fixtures gives honest evidence without claiming the page
  remained unchanged.
- Review layout and navigation ergonomics should be improved separately from
  capture correctness.
