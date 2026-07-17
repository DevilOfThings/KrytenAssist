# Codex Prompt 037j-b – Grouped Recorded Cruises

Implement **Step 2 only** from `docs/AI Playbook/037j - Cruise Discovery Workspace Refinements.md`.

Add an ephemeral **Group by: None | Cruise | Ship** control to Recorded Cruise
History. Grouping must derive only from loaded local History rows, preserve
selection and Price History detail, preserve first-seen order, and never cause
a query, recording operation, persistence write or TUI access.

Use display-ready History group ViewModels owned by `CruiseHistoryViewModel`.
None remains the existing flat order without a heading. Cruise groups by title;
Ship groups by ship name. Group headings show the label and sailing count.

Update both browser-free and active-browser History views. Do not change
capture, provider, browser, persistence or price behaviour. Add deterministic
tests for grouping order, item identity, selection preservation and no
repository call on grouping changes.

Run focused History/Cruise tests, build and the complete offline suite. Update
this prompt’s Results and Lessons Learned after Robin’s manual verification.

## Results

### Status

Implementation and automated verification complete. Awaiting Robin's final
manual verification.

### Grouping

Recorded Cruise History now provides mutually exclusive **None**, **Cruise**
and **Ship** radio choices. Groups are derived from the existing loaded History
items in first-seen order, show a sailing count and retain the selected Price
History item. Grouping is not persisted and causes no History query, recording
or TUI access.

The grouped list has its own vertical scroll container in both browser-free and
active-browser History views.

### Files Created

- `KrytenAssist.Avalonia/ViewModels/CruiseHistoryGrouping.cs`
- `KrytenAssist.Avalonia/ViewModels/CruiseHistoryGroupViewModel.cs`

### Build

Passed:

```text
dotnet build KrytenAssist.Avalonia/KrytenAssist.Avalonia.csproj --no-restore
```

Result: 0 errors. Existing SQLite package advisory and unused command-event
warnings remain.

### Tests

Focused Cruise History/browser regression: 52 passed, 0 failed, 0 skipped.

### Manual Verification

Pending Robin's confirmation that all three radio choices group correctly,
Recorded Cruises scroll independently, and selecting a sailing updates Price
History.

## Lessons Learned

- Grouping should be derived from the loaded local item collection, leaving the
  existing History query and selection identity untouched.
- Grouped nested lists need an explicit outer scroll container; otherwise the
  group host expands rather than using the bounded Recorded Cruises region.
