# Codex Prompt 035b – Shell ViewModel

## Source Prompt

Implement **Step 2 only** from:

```text
docs/AI Playbook/035 - Dashboard and Navigation.md
```

Step 1 has already been implemented.

Do not implement Steps 3–7.

---

## Goal

Introduce a focused shell ViewModel that owns application-level navigation state for the built-in Dashboard and Assistant destinations.

The shell should:

- compose the existing `MainWindowViewModel` as the Assistant workspace
- expose Dashboard and Assistant navigation items
- select Dashboard initially
- expose selected-destination state
- provide one navigation command
- raise deterministic property-change notifications
- perform no Skill execution or external work

Registry-driven Skill discovery and dashboard cards belong to Prompt 035c and must not be implemented here.

---

## Allowed Project

Make implementation changes only inside:

```text
KrytenAssist.Avalonia
```

Create:

```text
KrytenAssist.Avalonia/ViewModels/ShellViewModel.cs
```

Do not modify existing production files.

The solution may be built from the repository root.

Do not modify:

```text
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

Use the existing Prompt 035a models:

```text
KrytenAssist.Avalonia/Navigation/Models/NavigationDestinationKind.cs
KrytenAssist.Avalonia/Navigation/Models/NavigationItem.cs
KrytenAssist.Avalonia/Navigation/Models/DashboardSkillCard.cs
```

Compose the existing Assistant ViewModel:

```text
KrytenAssist.Avalonia/ViewModels/MainWindowViewModel.cs
```

Do not modify or redesign these types.

Do not duplicate the navigation models.

---

## Namespace and Type

Create:

```text
KrytenAssist.Avalonia/ViewModels/ShellViewModel.cs
```

Use:

```csharp
namespace KrytenAssist.Avalonia.ViewModels;
```

Implement:

```csharp
public sealed class ShellViewModel : INotifyPropertyChanged
```

The ViewModel must remain independent of Avalonia control and view types.

---

## Constructor

Use constructor injection:

```csharp
public ShellViewModel(MainWindowViewModel assistantWorkspace)
```

Requirements:

- reject null `assistantWorkspace`
- retain the exact supplied instance
- create the built-in navigation items
- select Dashboard initially
- create the navigation command once

Do not manually construct `MainWindowViewModel`.

Do not accept `IServiceProvider`.

Do not resolve services inside the ViewModel.

### Skill Registry Timing

Do not inject `ISkillRegistry` in this step.

Prompt 035c will extend the shell with registry-driven Skill discovery.

This incremental boundary keeps Prompt 035b focused on built-in shell state and prevents an unused dependency.

Do not create a temporary registry abstraction or fake Skill list.

---

## Built-In Navigation

Create exactly two built-in navigation items in this order:

### Dashboard

```text
Id: navigation.dashboard
Title: Dashboard
Kind: Dashboard
SkillId: null
```

### Assistant

```text
Id: navigation.assistant
Title: Assistant
Kind: Assistant
SkillId: null
```

Use the existing `NavigationItem` constructor.

Do not add static global navigation catalogues.

Private constants for the two stable ids inside `ShellViewModel` are permitted.

Do not add Skills, Settings or future destinations.

---

## Exposed Properties

Expose:

```csharp
public MainWindowViewModel AssistantWorkspace { get; }

public IReadOnlyList<NavigationItem> NavigationItems { get; }

public NavigationItem SelectedNavigationItem { get; }

public bool IsDashboardSelected { get; }

public bool IsAssistantSelected { get; }

public bool IsSkillSelected { get; }

public ICommand NavigateCommand { get; }
```

### AssistantWorkspace

Return the exact injected `MainWindowViewModel`.

Do not proxy or duplicate its prompt/conversation properties.

### NavigationItems

Expose Dashboard followed by Assistant.

Requirements:

- callers must not be able to mutate the collection successfully
- preserve the stable order
- do not recreate the collection on every property access
- do not expose a mutable array or list directly

Use a small read-only collection created during construction.

### SelectedNavigationItem

The selected item is never null.

Dashboard is selected initially.

The property should have a private setter or equivalent encapsulation.

External callers navigate through `NavigateCommand`.

### Selected-State Properties

Implement:

```text
IsDashboardSelected
IsAssistantSelected
IsSkillSelected
```

Derive them from `SelectedNavigationItem.Kind`.

Do not store duplicate boolean fields.

In this step `IsSkillSelected` will be false because Skill destinations are not yet discovered. It exists as stable shell state for later steps.

---

## Navigation Command

Implement one `ICommand`:

```text
NavigateCommand
```

The command parameter must be a `NavigationItem`.

Navigation behavior:

1. if the parameter is not a `NavigationItem`, do nothing
2. resolve the supplied item by its stable `Id` against `NavigationItems`
3. compare ids using `StringComparison.Ordinal`
4. if no matching item exists, do nothing
5. select the canonical item from `NavigationItems`
6. if that canonical item is already selected, do nothing
7. otherwise update selected state and notify bindings

Resolving by stable id allows a logically equivalent item to navigate while ensuring selected state always retains a shell-owned canonical instance.

Do not:

- execute a Skill
- use reflection
- inspect concrete types
- resolve services
- throw for a non-navigation command parameter
- create a new destination
- accept arbitrary unknown Skill items
- perform asynchronous work

### CanExecute

`CanExecute` may remain true.

The command should safely ignore unsupported parameters in `Execute`.

Do not add complex command-state management.

---

## Property Change Notifications

Implement `INotifyPropertyChanged` using the existing project style.

When selection changes, raise `PropertyChanged` for:

```text
SelectedNavigationItem
IsDashboardSelected
IsAssistantSelected
IsSkillSelected
```

Do not raise those notifications when:

- the command parameter is invalid
- the destination id is unknown
- the already-selected destination is requested

Use `CallerMemberName` only if it keeps the implementation focused and consistent.

Do not add an external MVVM package.

---

## Command Implementation

A small private nested command implementation is permitted inside `ShellViewModel`.

It should:

- implement `ICommand`
- accept an `Action<object?>`
- reject a null action in its constructor
- execute synchronously
- keep `CanExecute` true
- declare `CanExecuteChanged` consistently with existing project conventions

Do not create a public general-purpose command abstraction in this step.

Do not modify the private command implementations inside `MainWindowViewModel`.

---

## Immutability and Ownership

The shell owns:

- its built-in navigation item instances
- the read-only navigation collection
- selected destination state
- the navigation command

The shell does not own or dispose:

- `MainWindowViewModel`
- services used by the Assistant workspace
- Skills
- the Skill Registry

Do not implement `IDisposable` merely because the composed ViewModel has services.

---

## Architecture Requirements

`ShellViewModel` may depend on:

- Base Class Library interfaces
- Prompt 035 navigation models
- the existing `MainWindowViewModel`

It must not depend on:

- `ISkillRegistry` yet
- `ISkill`
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

No external request or Skill execution may occur during construction or navigation.

---

## Design Constraints

Prefer:

- constructor injection
- one canonical navigation collection
- derived selected-state properties
- stable ordinal identifiers
- one small command
- explicit property notifications

Avoid:

- service location
- static mutable state
- multiple navigation commands
- view switching in code-behind
- string-to-view maps
- concrete Skill checks
- dynamic view creation
- automatic Skill execution
- speculative navigation history
- back/forward stacks
- routing frameworks
- new packages

Do not reformat unrelated files.

---

## Explicitly Out of Scope

Do not implement:

- Skill Registry injection
- Skill discovery
- Skill navigation items
- dashboard cards
- selected Skill manifest
- Skill details
- Skill execution
- Dashboard view
- Skill details view
- Assistant workspace view extraction
- MainWindow changes
- App startup changes
- dependency-injection registration
- styling
- tests
- Prompt 035 verification
- Cruise retrieval
- persistence
- navigation history
- Prompt 036 or later work

---

## Verification

Inspect the final diff and confirm only this file was added:

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

- `ShellViewModel` compiles
- it retains the injected Assistant workspace
- Dashboard and Assistant exist in the required order
- navigation collection is externally read-only
- Dashboard is initially selected
- selection booleans derive from the selected kind
- navigation uses stable ids and canonical items
- invalid and unknown parameters are ignored safely
- required property notifications are raised only on changes
- construction and navigation execute no Skill or external work
- the solution builds
- no unrelated files change

Report existing SQLite vulnerability and Avalonia unused-event warnings separately from warnings introduced here.

---

## Completion Report

### Files Created

Expected:

```text
KrytenAssist.Avalonia/ViewModels/ShellViewModel.cs
```

### Files Modified

Expected:

```text
None
```

### Implementation Summary

Describe:

- Assistant workspace composition
- built-in navigation
- initial selection
- navigation command behavior
- property notifications

### Build

Report command, status, warning count and error count.

Separate existing warnings from warnings introduced by this task.

### Tests

Confirm no tests were added or run because tests belong to Step 6.

### Scope Check

Confirm:

- only Step 2 was implemented
- no Skill Registry dependency was added
- no Skill discovery or dashboard cards were added
- no views, DI or startup changes were made
- no Skill execution or network work was added
- no documentation was modified

