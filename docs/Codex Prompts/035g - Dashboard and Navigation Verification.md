# Codex Prompt 035g – Dashboard and Navigation Verification

## Source Prompt

Implement **Step 7 only** from:

```text
docs/AI Playbook/035 - Dashboard and Navigation.md
```

This prompt is intended to run only after Prompts 035a–035f have been implemented.

If Prompt 035f automated tests are not present and complete, stop and report that prerequisite rather than implementing them here.

Do not begin Prompt 036.

---

## Goal

Verify the completed Dashboard and Navigation capability end to end while preserving MVVM, registry-driven Skill discovery, provider independence and offline-safe startup.

This is a verification task, not a feature-development task.

Demonstrate that:

- `MainWindow` is the persistent application shell
- Dashboard is the default destination
- Assistant preserves the existing Prompt Library and conversation workspace
- registered Skills drive navigation items and dashboard cards
- registry order is preserved
- dashboard cards and left navigation select the same generic Skill destination
- shell navigation state remains in `ShellViewModel`
- views remain passive and provider independent
- shell construction and navigation do not execute Skills
- shell construction and navigation do not retrieve live Cruise data
- dependency-injection composition resolves successfully
- focused Prompt 035 tests pass
- the solution builds
- the complete regression suite passes

No live Cruise request, browser automation or external service is required.

Do not add behavior merely to create a verification mechanism.

---

## Repository Context

### Navigation Models

```text
KrytenAssist.Avalonia/Navigation/Models/
├── NavigationDestinationKind.cs
├── NavigationItem.cs
└── DashboardSkillCard.cs
```

### Shell State

```text
KrytenAssist.Avalonia/ViewModels/ShellViewModel.cs
KrytenAssist.Avalonia/ViewModels/MainWindowViewModel.cs
```

### Views and Composition

```text
KrytenAssist.Avalonia/MainWindow.axaml
KrytenAssist.Avalonia/MainWindow.axaml.cs
KrytenAssist.Avalonia/Views/DashboardView.axaml
KrytenAssist.Avalonia/Views/DashboardView.axaml.cs
KrytenAssist.Avalonia/Views/SkillDetailsView.axaml
KrytenAssist.Avalonia/Views/SkillDetailsView.axaml.cs
KrytenAssist.Avalonia/Views/AssistantWorkspaceView.axaml
KrytenAssist.Avalonia/Views/AssistantWorkspaceView.axaml.cs
KrytenAssist.Avalonia/App.axaml
```

### Discovery and Dependency Injection

```text
KrytenAssist.Avalonia/Skills/Services/ISkill.cs
KrytenAssist.Avalonia/Skills/Services/ISkillRegistry.cs
KrytenAssist.Avalonia/Skills/Services/SkillRegistry.cs
KrytenAssist.Avalonia/DependencyInjection/SkillServiceCollectionExtensions.cs
KrytenAssist.Avalonia/DependencyInjection/ShellServiceCollectionExtensions.cs
KrytenAssist.Avalonia/Program.cs
```

### Tests

Use the Prompt 035 tests created by Prompt 035f under:

```text
KrytenAssist.Avalonia.Tests/
```

Use the current contracts and tests as the source of truth. Do not duplicate the shell or navigation behavior in verification-only code.

---

## Expected Architecture

Confirm this composition path:

```text
MainWindow
    ↓
ShellViewModel
    ├── NavigationItems
    ├── DashboardCards
    ├── SelectedNavigationItem
    ├── SelectedSkillManifest
    └── AssistantWorkspace
            ↓
        MainWindowViewModel
```

Confirm Skill discovery remains registry driven:

```text
ISkillRegistry.Skills
    ↓
SkillManifest
    ├── NavigationItem
    └── DashboardSkillCard
```

Confirm selection changes presentation state only:

```text
Navigation item or Dashboard card
    ↓
ShellViewModel.NavigateCommand
    ↓
Dashboard / Assistant / generic Skill details
```

No path above should execute `ISkill.ExecuteAsync`, call `ICruiseOfTheWeekProvider.GetCurrentAsync` or perform HTTP work.

---

## Allowed Changes

The expected outcome is **no source-code changes**.

Production or test files may be modified only if verification exposes a genuine defect against Prompt 035.

Any correction must:

- be minimal
- fix only the verified defect
- preserve existing public contracts where practical
- preserve MVVM and registry-driven discovery
- preserve the existing Assistant behavior
- preserve offline-safe construction and navigation
- include or update a focused deterministic test
- be described explicitly in the completion report

Do not modify documentation during verification.

Do not update the AI Playbook, Roadmap, Backlog, Session Handover or Codex prompts. Those updates will be completed separately after review.

Do not stage, commit, push, discard or overwrite existing work.

---

## Verification Process

### 1. Record the Initial Working Tree

Run:

```bash
git status --short
```

Record all staged, unstaged and untracked files.

Treat all existing changes as pre-existing work. Do not use destructive Git commands.

---

### 2. Verify Scope and Project Boundaries

Inspect the Prompt 035 implementation and project references.

Confirm:

- navigation models, shell ViewModel, views and DI composition remain in `KrytenAssist.Avalonia`
- no Prompt 035 presentation type was added to Core, Application or Infrastructure
- no new project or package reference was required for navigation
- no reflection-based view locator or external behavior package was introduced
- no Marella, AngleSharp, `HttpClient`, OpenAI SDK or provider DTO type enters the navigation models, shell ViewModel or generic views
- no Cruise history, scheduling, persistence or full Cruise Dashboard work was introduced

Report concrete file evidence.

---

### 3. Verify Navigation Models

Inspect:

```text
KrytenAssist.Avalonia/Navigation/Models
```

Confirm:

- `NavigationDestinationKind` defines only Dashboard, Assistant and Skill
- `NavigationItem` is an immutable presentation model
- built-in destinations do not require a Skill identifier
- Skill destinations require and retain a Skill identifier
- invalid or blank required values are rejected
- `DashboardSkillCard` contains only provider-independent manifest presentation data
- models contain no service resolution, execution, network or UI-control behavior

Use the focused model tests from Prompt 035f as executable evidence.

---

### 4. Verify Registry-Driven Discovery

Inspect `ShellViewModel` and the Skill registry contracts.

Confirm:

- `ISkillRegistry.Skills` is the only Skill catalogue used by the shell
- Dashboard and Assistant are the only explicit built-in destinations
- Skill navigation items are derived from `SkillManifest`
- dashboard cards are derived from the same manifests
- registry order is preserved in both collections
- an empty registry produces valid built-in navigation and an empty dashboard
- the shell does not inspect `EchoSkill`, `CruiseOfTheWeekSkill` or any concrete Skill type
- no hard-coded Echo or Cruise navigation entry exists
- duplicate catalogue or provider-specific mapping logic was not introduced

Search the presentation layer for concrete Skill and Marella type checks and report the result.

---

### 5. Verify Shell State and Navigation

Inspect `ShellViewModel` and its tests.

Confirm:

- the exact composed `MainWindowViewModel` is exposed as `AssistantWorkspace`
- Dashboard is selected initially
- only one destination-kind flag is true at a time
- selecting Dashboard clears the selected Skill manifest
- selecting Assistant clears the selected Skill manifest
- selecting a Skill exposes its manifest
- dashboard cards navigate to the matching canonical Skill navigation item
- left-navigation items and dashboard cards reach the same Skill destination
- unknown, stale, null and unsupported navigation parameters are ignored safely
- repeated selection is stable
- property-change notifications cover every affected property
- navigation decisions are not duplicated in code-behind

Use deterministic tests rather than launching the UI to establish state behavior.

---

### 6. Verify Dashboard and Skill Views

Inspect:

```text
KrytenAssist.Avalonia/Views/DashboardView.axaml
KrytenAssist.Avalonia/Views/SkillDetailsView.axaml
```

Confirm:

- both views use compiled bindings against `ShellViewModel`
- Dashboard binds to `DashboardCards`
- the empty-dashboard message is controlled by shell state
- each card displays manifest-derived name, description and version
- card actions forward the card to `NavigateCommand`
- Skill details displays only the selected manifest's name, description, version and identifier
- neither view guesses operations or parameters
- neither view resolves services, executes a Skill or retrieves data
- no provider-specific or concrete-Skill branch appears in XAML or code-behind

Do not add capability-specific controls during verification.

---

### 7. Verify Application Shell Composition

Inspect `MainWindow.axaml` and `MainWindow.axaml.cs`.

Confirm:

- `MainWindow` uses `ShellViewModel` as its compiled DataContext contract
- the application identity header remains persistent
- `NavigationList` binds to `NavigationItems`
- its selected item is a one-way binding from `SelectedNavigationItem`
- each navigation entry displays only its title
- the small selection handler forwards the selected `NavigationItem` to `NavigateCommand`
- code-behind does not duplicate navigation state or inspect destination types beyond the presentation model
- Dashboard, Assistant and Skill details are hosted as the three destinations
- Dashboard is initially visible
- Assistant receives the exact `AssistantWorkspace` ViewModel
- Skill details inherits the shell DataContext
- the left navigation remains outside Assistant overlays
- `MainWindow` does not resolve `MainWindowViewModel` separately
- `MainWindow` performs no Assistant loading, Skill execution, HTTP or Infrastructure work

---

### 8. Verify Existing Assistant Preservation

Inspect `AssistantWorkspaceView.axaml`, its code-behind and the existing `MainWindowViewModel` tests.

Confirm the extracted view preserves:

- Prompt Library layout and search
- prompt selection, creation, editing and deletion
- category suggestions
- editor and deletion overlays
- AI Conversation history and input
- send, cancel and clear commands
- Enter-to-send and Shift+Enter behavior
- Use Prompt focus forwarding
- prompt-card double-tap forwarding
- newest-message scrolling and input focus
- embedding status
- existing error, busy and validation bindings
- existing control names, commands and style classes

Confirm lifecycle behavior:

- the ViewModel comes from DataContext rather than service location
- the active conversation collection is subscribed once
- the previous collection is unsubscribed on detach or DataContext change
- `LoadAsync()` runs once per Assistant workspace control instance
- temporary null DataContext values are tolerated
- navigation away and back does not repeatedly load the workspace

Do not redesign the Assistant during verification.

---

### 9. Verify Dependency Injection and Startup Composition

Inspect:

```text
KrytenAssist.Avalonia/DependencyInjection/ShellServiceCollectionExtensions.cs
KrytenAssist.Avalonia/Program.cs
```

Confirm:

- `AddShell()` rejects null
- `MainWindowViewModel` is registered as transient
- `ShellViewModel` is registered as transient
- the extension returns the original service collection
- registration uses constructor injection and resolves nothing eagerly
- `Program` no longer registers `MainWindowViewModel` directly
- Marella composition remains unchanged
- `AddSkills()` occurs before `AddShell()`
- the shell and its Assistant workspace resolve successfully from DI
- no view or window is registered unnecessarily

Use Prompt 035f DI tests as executable evidence.

---

### 10. Verify Offline and Non-Execution Behavior

Inspect constructors, commands and tests across:

```text
ShellViewModel
DashboardView
SkillDetailsView
MainWindow
SkillRegistry
CruiseOfTheWeekSkill
MarellaCruiseOfTheWeekProvider
```

Confirm:

- shell construction reads Skill manifests only
- dashboard-card construction reads presentation metadata only
- navigation changes selection state only
- resolving `ISkillRegistry` constructs Skills but does not execute them
- resolving `ShellViewModel` does not call `ISkill.ExecuteAsync`
- no navigation path calls `ICruiseOfTheWeekProvider.GetCurrentAsync`
- the Marella provider performs HTTP only inside explicit `GetCurrentAsync()` invocation
- focused non-execution tests use fakes and do not access the network
- application startup adds no Cruise retrieval, status probe or preload

Do not make a live Marella request to prove this behavior.

---

### 11. Verify Styling and Layout Contracts

Inspect `App.axaml` and the composed views.

Confirm:

- navigation styles use existing dynamic theme resources
- selected navigation has a clear background and accent boundary
- keyboard focus remains visible
- default navigation background is transparent
- item content stretches correctly
- the navigation width remains suitable for the existing minimum window size
- light and dark theme resources remain intact
- no unused dashboard-card style was added
- no light-only hard-coded navigation color was introduced

Visual appearance may be noted for manual review, but browser automation or application launch is not required by this verification prompt.

---

### 12. Run Focused Prompt 035 Tests

Run the focused offline tests created by Prompt 035f.

Prefer the narrowest available filter covering Prompt 035 navigation, shell, composition and Assistant regression tests, for example:

```bash
dotnet test KrytenAssist.Avalonia.Tests/KrytenAssist.Avalonia.Tests.csproj --filter "FullyQualifiedName~Navigation|FullyQualifiedName~Shell|FullyQualifiedName~MainWindowViewModel"
```

Adjust the filter to the actual Prompt 035 test namespaces and class names.

Report:

- exact command
- passed count
- failed count
- skipped count

All tests must remain deterministic and offline.

---

### 13. Build the Solution

Run:

```bash
dotnet build KrytenAssist.sln
```

Report:

- build result
- errors
- warnings
- whether any warning was introduced by Prompt 035

Do not hide or suppress warnings merely to obtain a clean report.

---

### 14. Run the Complete Regression Suite

Run:

```bash
dotnet test KrytenAssist.sln --no-build
```

Report results per test project where available.

Confirm:

- Core tests pass
- API tests pass
- Avalonia tests pass
- Prompt 032 Skill tests still pass
- Prompt 034 Cruise tests still pass without network access
- existing Assistant ViewModel tests still pass
- Prompt 035 tests pass

Do not weaken, skip or remove an existing test to make verification pass.

---

### 15. Recheck the Working Tree

Run:

```bash
git status --short
```

Compare it with the initial status.

If verification required no correction, confirm that no source or test file changed.

If a genuine correction was necessary, list every changed file and connect it to the failing requirement and focused regression test.

---

## Verification Boundaries

Do not:

- execute a live Cruise request
- launch browser automation
- redesign the shell or Assistant
- add Skill execution controls
- add operation discovery
- add a Cruise-specific dashboard
- add history, persistence, scheduling, watch lists or alerts
- add Settings or future navigation destinations
- hard-code registered Skills in the UI
- introduce provider-specific presentation dependencies
- update documentation or roadmap status
- stage, commit or push changes

---

## Completion Report

Provide:

### Summary

A concise verification outcome and whether Prompt 035 satisfies its acceptance criteria.

### Architecture

Report evidence for:

- registry-driven discovery
- MVVM ownership
- provider-independent presentation
- preserved Assistant composition
- offline and non-executing navigation

### Focused Tests

Include the exact command and result counts.

### Build

Include the exact command, result, errors and warnings.

### Regression Tests

Include the exact command and results per test project where available.

### Files Modified

State `None` when verification required no correction. Otherwise list every file and explain why it changed.

### Notes

List any manual UI checks recommended after verification and any pre-existing warnings or working-tree changes.

### Final Status

Use exactly one:

```text
Prompt 035 verified and complete.
```

or:

```text
Prompt 035 verification failed: <concise reason>.
```

