# Codex Prompt 037j-a – Cruise Discovery Identity and History Actions

## Implementation Prompt

Implement **Step 1 only** from:

```text
docs/AI Playbook/037j - Cruise Discovery Workspace Refinements.md
```

Prompt 037i is complete and verified. This step is presentation-only: make
the visible Cruise route accurately say **Cruise Discovery**, place the
selected History action beside its heading, and reclaim browser-workspace
width by hiding secondary History columns.

Do not change the existing `cruise.of-the-week` skill id, HTTP provider,
assistant-tool contract, browser bridge, capture flow, History persistence,
grouping or mobile presentation.

---

## Required Reading

1. `AGENTS.md`
2. `docs/Roadmap.md`
3. `docs/AI Playbook/031a - Runtime Context Injection.md`
4. `docs/AI Playbook/037i - Cruise Discovery Workspace Layout.md`
5. `docs/AI Playbook/037j - Cruise Discovery Workspace Refinements.md`
6. `docs/Codex Prompts/037i-d - Cruise Discovery Workspace Verification.md`
7. `KrytenAssist.Avalonia/ViewModels/ShellViewModel.cs`
8. `KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml`
9. `KrytenAssist.Avalonia/Views/CruiseHistoryPanelView.axaml`
10. Shell and Cruise History ViewModel tests

---

## Required Behaviour

### Visible Label Projection

For the existing internal skill id:

```text
cruise.of-the-week
```

show the following user-facing identity in navigation and Dashboard:

```text
Cruise Discovery
Browse trusted TUI cruise pages, capture displayed deals and revisit local price history.
```

Keep the original manifest, id, registration, assistant-tool behaviour and
special Cruise workspace route unchanged. This is a Shell-owned visible
projection only.

### Selected History Action

Move **Open at TUI** beside the `Price History` heading in both the
browser-free and active-browser History detail views.

It retains existing command, trusted-reference validation, visibility and
external-launch behaviour. Do not create a new command or URL path.

### Browser-Active Grid

Only in the active browser workspace, display:

```text
Cruise | Ship | Departure | Current
```

Remove Trend and Status columns there. Retain them in browser-free History and
the selected Price History detail.

---

## Allowed Changes

```text
KrytenAssist.Avalonia/ViewModels/ShellViewModel.cs
KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml
KrytenAssist.Avalonia/Views/CruiseHistoryPanelView.axaml
KrytenAssist.Avalonia.Tests/ViewModels/
docs/Codex Prompts/037j-a - Cruise Discovery Identity and History Actions.md
```

Do not stage, commit, push, discard or overwrite Robin's work.

---

## Verification

Add or update deterministic tests proving:

- navigation and Dashboard display **Cruise Discovery** for the existing
  `cruise.of-the-week` id
- navigation and Dashboard still select the existing Cruise workspace
- the internal manifest identity and generic Skill routing remain unchanged
- existing History trusted Open at TUI command behaviour remains covered

Run focused Shell, Cruise browser and History tests, then:

```text
dotnet build KrytenAssist.sln --no-restore
dotnet test KrytenAssist.sln --no-build --no-restore
```

Use the stable single-worker runner if required. Tests must not contact TUI,
launch a browser or access Robin's database.

---

## Results

> Complete after implementation and verification.

### Status

Complete.

### Files Updated

- `KrytenAssist.Avalonia/ViewModels/ShellViewModel.cs`
- `KrytenAssist.Avalonia/Views/CruiseBrowserFeasibilityView.axaml`
- `KrytenAssist.Avalonia/Views/CruiseHistoryPanelView.axaml`
- `KrytenAssist.Avalonia.Tests/ViewModels/ShellViewModelTests.cs`
- `KrytenAssist.Avalonia.Tests/ViewModels/CruiseHistoryViewModelTests.cs`
- `KrytenAssist.Avalonia/ViewModels/CruiseHistoryItemViewModel.cs`
- this prompt

### Build

Passed:

```text
dotnet build KrytenAssist.sln --no-restore
```

Result: 0 errors. Existing SQLite package advisory and unused command-event
warnings remain.

### Tests

Focused Shell and Cruise workspace regression: 116 passed, 0 failed, 0 skipped.

Complete offline regression using the stable single-worker runner:

```text
Core: 105 passed
Avalonia: 427 passed
API: 9 passed
Total: 541 passed, 0 failed, 0 skipped
```

### Manual Verification

Passed. Robin confirmed the visible Cruise Discovery identity, History action
placement, compact active-browser grid and the abbreviated `pp` grid price
display.

---

## Lessons Learned

- Shell-owned display projection allows a visible workspace identity to evolve
  without changing a provider-facing skill id or assistant-tool contract.
- Removing secondary columns only from the browser-active view improves width
  without losing their context from browser-free History or selected detail.
- Compact grid formatting can use `pp` while the full per-person wording
  remains available in Price History and by hover.
