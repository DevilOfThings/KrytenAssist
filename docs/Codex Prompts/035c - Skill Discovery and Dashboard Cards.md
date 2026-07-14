# Codex Prompt 035c – Skill Discovery and Dashboard Cards

## Source Prompt

Implement **Step 3 only** from:

```text
docs/AI Playbook/035 - Dashboard and Navigation.md
```

Steps 1–2 have already been implemented.

Do not implement Steps 4–7.

---

## Goal

Extend the existing `ShellViewModel` with registry-driven Skill discovery.

The shell should:

- receive `ISkillRegistry` through constructor injection
- enumerate registered Skills once
- preserve registry order
- create one Skill navigation item per registered Skill
- create one dashboard discovery card per registered Skill
- expose the selected `SkillManifest`
- allow dashboard cards to reuse the existing navigation command
- support an empty registry
- execute no Skill during discovery or navigation

Do not add views, dependency-injection registration, styling or tests in this task.

---

## Allowed Project and File

Make implementation changes only inside:

```text
KrytenAssist.Avalonia
```

Modify only:

```text
KrytenAssist.Avalonia/ViewModels/ShellViewModel.cs
```

The solution may be built from the repository root.

Do not modify:

```text
KrytenAssist.Avalonia/Navigation/Models/
KrytenAssist.Avalonia/Skills/
KrytenAssist.Avalonia/MainWindow.axaml
KrytenAssist.Avalonia/App.axaml.cs
KrytenAssist.Avalonia/Program.cs
KrytenAssist.Avalonia/DependencyInjection/
KrytenAssist.Core
KrytenAssist.Core.Tests
KrytenAssist.Application
KrytenAssist.Infrastructure
KrytenAssist.Api
KrytenAssist.Api.Tests
KrytenAssist.Avalonia.Tests
KrytenAssist.Client
KrytenAssist.sln
```

Do not modify documentation, including the AI Playbook, Roadmap, Backlog, handovers or Codex prompts.

Robin will update documentation after reviewing implementation.

---

## Existing Types

Use:

```text
KrytenAssist.Avalonia/ViewModels/ShellViewModel.cs
KrytenAssist.Avalonia/Navigation/Models/NavigationItem.cs
KrytenAssist.Avalonia/Navigation/Models/DashboardSkillCard.cs
KrytenAssist.Avalonia/Navigation/Models/NavigationDestinationKind.cs
KrytenAssist.Avalonia/Skills/Models/SkillManifest.cs
KrytenAssist.Avalonia/Skills/Services/ISkill.cs
KrytenAssist.Avalonia/Skills/Services/ISkillRegistry.cs
```

Do not change the registry, manifest, Skill contracts or navigation models.

Do not inspect concrete Skill types.

---

## Constructor

Change the constructor to:

```csharp
public ShellViewModel(
    MainWindowViewModel assistantWorkspace,
    ISkillRegistry skillRegistry)
```

Requirements:

- reject null `assistantWorkspace`
- reject null `skillRegistry`
- retain the exact Assistant workspace
- create Dashboard and Assistant first
- enumerate `skillRegistry.Skills` exactly once
- create navigation items and cards during construction
- select Dashboard initially
- create the navigation command once

Do not retain `IServiceProvider`.

The shell may retain manifest lookup state required for selection, but it should not retain concrete `ISkill` instances merely for display.

Do not call `ISkill.ExecuteAsync`.

---

## Registry Enumeration

Read:

```csharp
skillRegistry.Skills
```

once during shell construction.

Iterate the returned collection once.

For each `ISkill`:

1. read `skill.Manifest` once
2. create one Skill `NavigationItem`
3. create one `DashboardSkillCard`
4. retain the manifest by its Skill identifier for later selection

Do not:

- sort Skills
- group by concrete type
- use reflection
- scan assemblies
- call the registry's `Find` method for every item
- execute a Skill
- access Infrastructure
- infer operations

The existing registry already rejects duplicate identifiers. Do not duplicate that responsibility.

---

## Navigation Item Mapping

For each manifest, create:

```text
Id: navigation.skill:{manifest.Id}
Title: manifest.Name
Kind: Skill
SkillId: manifest.Id
```

Use the literal prefix:

```text
navigation.skill:
```

Examples:

```text
navigation.skill:sample.echo
navigation.skill:cruise.of-the-week
```

Requirements:

- preserve manifest id and name exactly
- do not normalize or trim values
- do not use a concrete Skill class name
- do not include provider names beyond what the manifest already contains

Final navigation order must be:

1. Dashboard
2. Assistant
3. every registered Skill in registry order

Expose the complete collection through the existing `NavigationItems` property.

The collection must remain externally read-only.

---

## Dashboard Card Mapping

For each manifest, create:

```csharp
new DashboardSkillCard(
    manifest.Id,
    manifest.Name,
    manifest.Description,
    manifest.Version)
```

Expose:

```csharp
public IReadOnlyList<DashboardSkillCard> DashboardCards { get; }
```

Requirements:

- preserve registry order
- preserve all manifest values exactly
- expose an externally read-only collection
- create the collection once
- do not retain concrete Skills inside cards
- do not add operation, loading, result or provider state

Also expose:

```csharp
public bool HasDashboardCards { get; }
```

Derive it from `DashboardCards.Count > 0`.

Do not store a duplicate boolean field.

An empty registry should produce:

- two navigation items: Dashboard and Assistant
- zero dashboard cards
- `HasDashboardCards == false`
- Dashboard selected initially

Do not add an empty-state message property; the later Dashboard view owns fixed presentation copy.

---

## Selected Skill Manifest

Expose:

```csharp
public SkillManifest? SelectedSkillManifest { get; }
```

Behavior:

- null when Dashboard is selected
- null when Assistant is selected
- the exact manifest instance associated with the selected Skill destination when a Skill is selected

Do not copy or reconstruct the manifest for selection.

Do not expose the concrete `ISkill`.

Do not expose provider output or operation metadata.

### Lookup

Maintain a private manifest lookup keyed by Skill id.

Use:

```text
StringComparer.Ordinal
```

The lookup is shell-owned construction state.

Do not expose it publicly.

---

## Navigation Command Extension

Retain the existing public command:

```csharp
public ICommand NavigateCommand { get; }
```

Extend its accepted parameters.

### NavigationItem Parameter

Existing behavior remains:

- resolve by stable navigation id
- use ordinal comparison
- select the canonical shell-owned item
- ignore unknown ids
- ignore already-selected items

### DashboardSkillCard Parameter

When the parameter is a `DashboardSkillCard`:

1. read its `SkillId`
2. find the canonical Skill navigation item with the same `SkillId`
3. compare Skill ids using `StringComparison.Ordinal`
4. select that canonical navigation item
5. ignore an unknown or stale card safely

Both parameter types must flow through one shared private selection method.

Do not duplicate property-notification logic.

### Unsupported Parameter

Continue to ignore null and unsupported parameter types.

Do not throw.

---

## Selection State

The existing properties remain:

```csharp
SelectedNavigationItem
IsDashboardSelected
IsAssistantSelected
IsSkillSelected
```

Update selection so that `IsSkillSelected` becomes true for discovered Skill destinations.

When selection changes:

1. determine the new selected manifest
2. update the canonical selected navigation item
3. update selected-manifest state
4. raise required notifications

Raise `PropertyChanged` for:

```text
SelectedNavigationItem
IsDashboardSelected
IsAssistantSelected
IsSkillSelected
```

Raise `PropertyChanged` for `SelectedSkillManifest` only when its value changes.

Examples:

- Dashboard to Assistant: manifest remains null, so no selected-manifest notification is required
- Dashboard to Skill: notify selected manifest
- Skill A to Skill B: notify selected manifest
- Skill to Assistant: notify selected manifest
- selecting the current destination: no notifications

Use reference comparison for retained manifest instances where practical.

Do not add duplicate selected booleans.

---

## Exact Manifest Ownership

The registry owns registered Skills.

The shell owns:

- navigation presentation objects
- dashboard presentation objects
- its read-only collections
- selected navigation state
- manifest lookup references

The shell must not mutate manifests or Skills.

It must not dispose Skills or the registry.

---

## No Skill Execution

Construction and navigation must not call:

```csharp
ISkill.ExecuteAsync(...)
```

Do not:

- retrieve Cruise of the Week
- call HTTP
- access Marella
- start background work
- infer default operations
- create cancellation tokens
- introduce loading or error states

Dashboard cards remain discovery metadata only.

---

## Architecture Requirements

`ShellViewModel` may now depend on:

- Base Class Library types
- Prompt 035 navigation models
- `MainWindowViewModel`
- `ISkillRegistry`
- `ISkill` only through registry enumeration
- `SkillManifest`

It must not depend on:

- concrete Skills
- Core Cruise models
- Application Cruise contracts
- Infrastructure
- Marella
- AngleSharp
- `HttpClient`
- OpenAI SDK types
- Avalonia controls, views or windows
- persistence
- service providers

The generic shell must remain provider independent.

---

## Collection Construction

Use mutable local lists during construction if helpful.

After mapping completes, expose read-only collections.

Do not expose `List<T>` or arrays that callers can mutate.

Do not use `ObservableCollection<T>`; discovery is a construction-time snapshot in this prompt.

Dynamic Skill installation or removal is out of scope.

---

## Design Constraints

Prefer:

- one registry enumeration
- one manifest read per Skill
- exact manifest values
- registry order
- immutable presentation models
- canonical navigation items
- a shared selection path
- ordinal identifiers
- read-only collections

Avoid:

- concrete Skill branching
- reflection
- assembly scanning
- sorting
- dynamic refresh
- service location
- Skill execution
- async construction
- multiple competing navigation commands
- provider-specific display logic
- unrelated shell refactoring

Do not reformat unrelated code.

---

## Explicitly Out of Scope

Do not implement:

- Dashboard view
- Skill details view
- Assistant workspace extraction
- MainWindow changes
- App startup changes
- dependency-injection registration
- styling
- dynamic Skill refresh
- Skill execution
- operation metadata
- live Cruise retrieval
- result presentation
- loading or error state
- tests
- Prompt 035 verification
- persistence
- Prompt 036 or later work

---

## Verification

Inspect the final diff and confirm only this file changed:

```text
KrytenAssist.Avalonia/ViewModels/ShellViewModel.cs
```

Run:

```bash
dotnet build
```

Do not start the application.

Do not add tests in this step.

The task succeeds when:

- constructor requires `ISkillRegistry`
- registry Skills are enumerated once
- each manifest creates one navigation item and one dashboard card
- registry order is preserved
- built-in destinations remain first
- empty registry is supported
- collections are externally read-only
- selected manifest is exact and null for built-ins
- navigation items and cards use one shared selection path
- unknown parameters and stale cards are ignored safely
- no Skill is executed
- no provider-specific dependency is added
- the solution builds
- no unrelated file changes are made

Report existing SQLite vulnerability and Avalonia unused-event warnings separately from warnings introduced here.

---

## Completion Report

### Files Created

Expected:

```text
None
```

### Files Modified

Expected:

```text
KrytenAssist.Avalonia/ViewModels/ShellViewModel.cs
```

### Implementation Summary

Describe:

- registry enumeration
- Skill navigation mapping
- dashboard card mapping
- selected manifest behavior
- shared navigation behavior
- empty registry support

### Build

Report command, status, warning count and error count.

Separate existing warnings from warnings introduced by this task.

### Tests

Confirm no tests were added or run because tests belong to Step 6.

### Scope Check

Confirm:

- only Step 3 was implemented
- no views, styling, DI or startup changes were made
- no concrete Skill checks were added
- no Skill execution or network behavior was added
- no provider-specific dependency was introduced
- no documentation was modified

