# Codex Prompt 035f – Dashboard and Navigation Tests

## Source Prompt

Implement **Step 6 only** from:

```text
docs/AI Playbook/035 - Dashboard and Navigation.md
```

Steps 1–5 have already been implemented.

Do not implement Step 7 verification and do not begin Prompt 036.

---

## Goal

Add comprehensive deterministic tests for the Dashboard and Navigation capability implemented in Prompts 035a–035e.

Cover:

- navigation model validation and immutability
- built-in shell navigation
- registry-driven Skill discovery
- dashboard-card mapping
- registry-order preservation
- empty-registry behavior
- Skill selection from navigation items
- Skill selection from dashboard cards
- unknown and stale navigation inputs
- property-change notifications
- shell dependency-injection registration and resolution
- non-execution during discovery and navigation
- existing Assistant ViewModel regression behavior

All tests must run offline and must not execute a live Cruise request.

Do not perform final Prompt 035 verification, launch the application, add UI automation or update project documentation.

---

## Allowed Changes

Create or modify files only inside:

```text
KrytenAssist.Avalonia.Tests/
```

Expected test locations:

```text
KrytenAssist.Avalonia.Tests/Navigation/
KrytenAssist.Avalonia.Tests/ViewModels/
KrytenAssist.Avalonia.Tests/DependencyInjection/
```

Suggested files:

```text
KrytenAssist.Avalonia.Tests/Navigation/NavigationItemTests.cs
KrytenAssist.Avalonia.Tests/Navigation/DashboardSkillCardTests.cs
KrytenAssist.Avalonia.Tests/ViewModels/ShellViewModelTests.cs
KrytenAssist.Avalonia.Tests/DependencyInjection/ShellDependencyInjectionTests.cs
```

Names may follow existing test conventions if equivalent focused coverage is clearer.

The existing test project already references the required production projects and test packages. Do not add a new test project or NuGet package.

Production files may be modified only if a required deterministic test exposes a genuine Prompt 035 defect.

Any production correction must:

- be minimal
- preserve the existing architecture and public contracts where practical
- fix only the verified defect
- include a focused regression test
- be reported explicitly

Do not modify documentation, including the AI Playbook, Roadmap, Backlog, Session Handover or Codex prompts. Robin will update documentation after reviewing the implementation.

Do not stage, commit, push, discard or overwrite existing work.

---

## Production Types Under Test

Exercise the existing implementation directly:

```text
KrytenAssist.Avalonia/Navigation/Models/NavigationDestinationKind.cs
KrytenAssist.Avalonia/Navigation/Models/NavigationItem.cs
KrytenAssist.Avalonia/Navigation/Models/DashboardSkillCard.cs
KrytenAssist.Avalonia/ViewModels/ShellViewModel.cs
KrytenAssist.Avalonia/ViewModels/MainWindowViewModel.cs
KrytenAssist.Avalonia/Skills/Models/SkillManifest.cs
KrytenAssist.Avalonia/Skills/Services/ISkill.cs
KrytenAssist.Avalonia/Skills/Services/ISkillRegistry.cs
KrytenAssist.Avalonia/Skills/Services/SkillRegistry.cs
KrytenAssist.Avalonia/DependencyInjection/ShellServiceCollectionExtensions.cs
```

Do not duplicate production navigation or mapping logic in tests.

Do not test private implementation details through reflection.

---

## Test Conventions

Follow the existing Avalonia test-project conventions.

Use:

- xUnit
- FluentAssertions where it improves clarity
- Arrange, Act and Assert
- descriptive test names
- small hand-written fakes
- fixed values and timestamps
- direct public-contract assertions
- isolated `ServiceCollection` instances

Do not use:

- mocking libraries
- reflection
- snapshot or approval tests
- Avalonia UI automation
- browser automation
- live HTTP
- live Marella content
- OpenAI calls
- file-system fixtures
- sleeps, retries or test ordering
- shared mutable state
- service location inside production types

Keep each test focused on one observable requirement.

---

## Deterministic Test Fixtures

Create small test-only Skills with manifests such as:

```text
Id: test.first
Name: First Skill
Description: First deterministic test Skill.
Version: 1.0.0

Id: test.second
Name: Second Skill
Description: Second deterministic test Skill.
Version: 2.0.0
```

Implement a hand-written `ISkill` fake that:

- returns its supplied manifest
- increments an execution counter inside `ExecuteAsync`
- returns a deterministic `SkillResult`

Use this fake to prove that shell construction, discovery and navigation do not call `ExecuteAsync`.

Populate the existing `SkillRegistry` through its public registration contract where practical. A small test-only `ISkillRegistry` is permitted only when a test requires precise observation of registry access.

Do not use `EchoSkill` or `CruiseOfTheWeekSkill` when a small fake better isolates shell behavior.

---

## Assistant Workspace Test Fixture

`ShellViewModel` requires an existing `MainWindowViewModel` instance.

Construct it with the same style of deterministic fakes already used by:

```text
KrytenAssist.Avalonia.Tests/ViewModels/MainWindowViewModelTests.cs
```

Use test implementations of:

- `IPromptCardStore`
- `IEmbeddingService`
- `IConversationService`
- `IConversationMemory`
- `ConversationOptions`

The shell tests should retain and compare the exact supplied Assistant ViewModel. They must not call provider services or depend on application startup.

Reuse a focused test helper inside the new shell test file or a clearly named shared test fixture if this removes meaningful duplication.

Do not add production constructors or factories solely for tests.

---

## NavigationItem Tests

Create focused tests for the existing `NavigationItem` contract.

Cover successful construction for:

- Dashboard with no Skill id
- Assistant with no Skill id
- Skill with a nonblank Skill id

Assert exact preservation of:

- id
- title
- destination kind
- Skill id

Cover constructor rejection for every invalid state already required by Prompt 035a:

- null, empty or whitespace navigation id
- null, empty or whitespace title
- Skill destination with null, empty or whitespace Skill id
- Dashboard with a supplied Skill id
- Assistant with a supplied Skill id
- unsupported destination-kind values if the public constructor can receive them

Assert the exact exception type and relevant parameter name where the implementation exposes one.

Do not weaken model validation to make a test pass.

---

## DashboardSkillCard Tests

Test successful construction and exact preservation of:

- Skill id
- name
- description
- version

Cover rejection of null, empty or whitespace values for every required field according to the existing Prompt 035a contract.

Confirm the model exposes presentation data only and can be consumed without constructing or executing an `ISkill`.

Do not add operations, parameters, provider data or execution state to the card.

---

## Shell Construction Tests

Create `ShellViewModel` with a deterministic Assistant ViewModel and registry.

Verify:

- null Assistant workspace is rejected
- null registry is rejected
- `AssistantWorkspace` is the exact supplied instance
- Dashboard is selected initially
- `SelectedNavigationItem` is the canonical Dashboard item
- `SelectedSkillManifest` is null initially
- `IsDashboardSelected` is true initially
- `IsAssistantSelected` and `IsSkillSelected` are false initially
- `NavigateCommand` is available

Verify built-in navigation entries exactly:

```text
navigation.dashboard | Dashboard | Dashboard | null
navigation.assistant | Assistant | Assistant | null
```

Confirm the built-ins appear first and are not recreated on property access.

---

## Skill Discovery and Mapping Tests

Register two deterministic Skills in a deliberate order.

Verify final navigation order:

1. Dashboard
2. Assistant
3. First Skill
4. Second Skill

For each Skill navigation item assert:

```text
Id      = navigation.skill:{manifest.Id}
Title   = manifest.Name
Kind    = Skill
SkillId = manifest.Id
```

Verify dashboard cards:

- contain one card per registered Skill
- preserve registry order
- preserve manifest id, name, description and version exactly
- do not include Dashboard or Assistant
- are constructed without Skill execution

Verify:

- `NavigationItems` cannot be mutated successfully by a caller
- `DashboardCards` cannot be mutated successfully by a caller
- an empty registry produces only Dashboard and Assistant navigation
- an empty registry produces no dashboard cards
- `HasDashboardCards` is false for an empty registry
- `HasDashboardCards` is true when at least one Skill is present

Do not sort the expected data before asserting order.

---

## Navigation Behavior Tests

Exercise `NavigateCommand` through its public `ICommand` contract.

### Built-In Selection

Verify:

- selecting Assistant selects the canonical Assistant item
- Assistant selection makes only `IsAssistantSelected` true
- selecting Dashboard returns to the canonical Dashboard item
- Dashboard selection makes only `IsDashboardSelected` true
- built-in selection leaves `SelectedSkillManifest` null

### Skill Navigation Item Selection

Verify:

- selecting a Skill navigation item selects the canonical registry-derived item
- `IsSkillSelected` becomes true
- Dashboard and Assistant flags become false
- `SelectedSkillManifest` is the exact corresponding manifest
- selecting a second Skill changes both canonical selection and manifest

Pass an equivalent noncanonical `NavigationItem` with a known navigation id and verify the shell resolves it to the canonical item rather than retaining caller-owned state.

### Dashboard Card Selection

Verify:

- passing a known `DashboardSkillCard` navigates to the matching Skill item
- the selected item is the same canonical item exposed by `NavigationItems`
- the selected manifest matches that Skill
- a dashboard card and its corresponding left-navigation item produce the same destination state

### Safe Inputs

Verify each of these leaves state unchanged and does not throw:

- null
- unsupported parameter type
- unknown navigation id
- stale Skill navigation item
- unknown dashboard-card Skill id

Verify selecting the currently selected canonical item is stable.

Do not test private lookup methods directly.

---

## Property-Change Notification Tests

Subscribe to `ShellViewModel.PropertyChanged` and record property names.

For a real destination transition, verify notifications for the affected public state:

- `SelectedNavigationItem`
- `IsDashboardSelected`
- `IsAssistantSelected`
- `IsSkillSelected`
- `SelectedSkillManifest` when the manifest changes

Cover transitions:

- Dashboard to Assistant
- Assistant to Skill
- one Skill to another Skill
- Skill to Dashboard

Verify:

- selecting the current item emits no notifications
- unknown input emits no notifications
- `SelectedSkillManifest` is not redundantly notified when its reference does not change

Assert the contract-required names and avoid coupling tests to unrelated implementation order unless Prompt 035 explicitly requires that order.

---

## Non-Execution Tests

Use counter-based fake Skills to prove the execution count remains zero after:

- registry population
- `ShellViewModel` construction
- reading `NavigationItems`
- reading `DashboardCards`
- selecting Dashboard
- selecting Assistant
- selecting every Skill navigation item
- selecting every dashboard card
- reading `SelectedSkillManifest`

The fake's `ExecuteAsync` may return a deterministic result if called, but the tests must assert it was never called.

Do not instantiate Marella infrastructure and do not make an HTTP request.

---

## Shell Dependency-Injection Tests

Test:

```csharp
services.AddShell()
```

Cover:

- null service collection is rejected
- the extension returns the original collection
- `MainWindowViewModel` is registered as transient
- `ShellViewModel` is registered as transient
- repeated resolution returns distinct shell instances
- each shell receives a DI-created Assistant workspace
- a resolved shell receives the registered `ISkillRegistry`
- resolving the shell produces Dashboard as the initial destination
- resolving the shell discovers deterministic registered Skills
- resolving the shell does not execute any Skill

Build an isolated `ServiceCollection` with deterministic registrations for all `MainWindowViewModel` dependencies and a deterministic Skill registry.

Do not call `AddSkills()` when that would introduce the production Cruise Skill and its provider dependency into a focused shell-registration test. Register the exact deterministic `ISkillRegistry` needed by the test instead.

Add one focused composition test using `AddSkills()` only if all required dependencies are supplied with offline fakes and it materially verifies the production call sequence.

Do not register a real OpenAI service or live Marella provider.

Do not change `AddShell()` lifetimes merely to simplify tests.

---

## Existing Assistant Regression Tests

Run and preserve:

```text
KrytenAssist.Avalonia.Tests/ViewModels/MainWindowViewModelTests.cs
```

Confirm existing tests continue to cover completed Assistant behavior, including prompt creation, editing, deletion, search, Use Prompt and conversation behavior already present in the suite.

Add a focused `MainWindowViewModel` regression test only if the shell extraction exposed a genuine behavior gap that can be tested through the ViewModel's public contract.

Do not instantiate `MainWindow` or `AssistantWorkspaceView` in ordinary unit tests. The current test project does not require a headless Avalonia environment, and Prompt 035f must not add one.

Do not test XAML layout, control focus, scrolling or visual attachment lifecycle through brittle text-file assertions.

Those view-only behaviors will be checked through build-time XAML compilation and manual review in Prompt 035g.

---

## Required Test Commands

First run the focused Prompt 035 and Assistant regression tests using the actual namespaces and class names created:

```bash
dotnet test KrytenAssist.Avalonia.Tests/KrytenAssist.Avalonia.Tests.csproj --filter "FullyQualifiedName~Navigation|FullyQualifiedName~Shell|FullyQualifiedName~MainWindowViewModel"
```

Adjust the filter if necessary so every Prompt 035 test and the existing Assistant ViewModel tests run.

Then run the complete Avalonia test project:

```bash
dotnet test KrytenAssist.Avalonia.Tests/KrytenAssist.Avalonia.Tests.csproj
```

Finally build the solution:

```bash
dotnet build KrytenAssist.sln
```

Report exact commands and results.

Do not run the full solution regression suite in this step; that belongs to Prompt 035g verification.

---

## Acceptance Criteria

Prompt 035f is complete when:

- navigation model invariants have focused deterministic tests
- Dashboard is proven to be the initial shell destination
- built-in navigation behavior is covered
- registry-driven Skill mapping is covered
- registry order is proven for navigation and dashboard cards
- empty registry behavior is covered
- dashboard-card and left-navigation selection converge on the same Skill destination
- safe handling of stale and unknown inputs is covered
- property-change behavior is covered
- shell collections are proven externally read-only
- `AddShell()` registration and transient resolution are covered
- shell resolution is proven not to execute Skills
- discovery and every navigation path are proven not to execute Skills
- existing Assistant ViewModel tests still pass
- no test accesses an external service
- no UI automation or headless framework was introduced
- the Avalonia test project passes
- the solution builds
- no Step 7 verification or documentation update was performed

---

## Completion Report

Provide:

### Summary

A concise description of the test coverage added.

### Files Modified

List every created or modified file.

### Focused Tests

Report the exact command, passed count, failed count and skipped count.

### Avalonia Test Suite

Report the exact command, passed count, failed count and skipped count.

### Build

Report the exact command, result, errors and warnings.

### Production Corrections

State `None` unless a deterministic test exposed a genuine defect. If corrected, describe the defect, minimal production change and regression test.

### Notes

List anything the reviewer should inspect manually before running Prompt 035g.

