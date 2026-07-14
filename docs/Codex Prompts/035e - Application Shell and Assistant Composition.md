# Codex Prompt 035e – Application Shell and Assistant Composition

## Source Prompt

Implement **Step 5 only** from:

```text
docs/AI Playbook/035 - Dashboard and Navigation.md
```

Steps 1–4 have already been implemented.

Do not implement Steps 6 or 7.

---

## Goal

Compose the completed navigation foundation, shell ViewModel, Dashboard view and Skill details view into the Avalonia application.

This task should:

- turn `MainWindow` into the persistent application shell
- add left navigation
- show Dashboard by default
- host the generic Skill details page
- preserve the complete existing Assistant workspace
- extract the Assistant workspace into a focused UserControl
- register `ShellViewModel` through dependency injection
- update startup composition
- add focused navigation and dashboard styling
- ensure shell resolution and navigation perform no network request

Do not add tests or final Prompt 035 verification in this task.

---

## Allowed Project

Make implementation changes only inside:

```text
KrytenAssist.Avalonia
```

Allowed files are limited to:

```text
KrytenAssist.Avalonia/App.axaml
KrytenAssist.Avalonia/MainWindow.axaml
KrytenAssist.Avalonia/MainWindow.axaml.cs
KrytenAssist.Avalonia/Program.cs
KrytenAssist.Avalonia/DependencyInjection/ShellServiceCollectionExtensions.cs
KrytenAssist.Avalonia/Views/AssistantWorkspaceView.axaml
KrytenAssist.Avalonia/Views/AssistantWorkspaceView.axaml.cs
```

The solution may be built from the repository root.

Do not modify:

```text
KrytenAssist.Avalonia/ViewModels/
KrytenAssist.Avalonia/Navigation/
KrytenAssist.Avalonia/Skills/
KrytenAssist.Avalonia/Views/DashboardView.axaml
KrytenAssist.Avalonia/Views/DashboardView.axaml.cs
KrytenAssist.Avalonia/Views/SkillDetailsView.axaml
KrytenAssist.Avalonia/Views/SkillDetailsView.axaml.cs
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

## Existing Shell Contract

Use:

```text
KrytenAssist.Avalonia/ViewModels/ShellViewModel.cs
```

The shell exposes:

```csharp
MainWindowViewModel AssistantWorkspace
IReadOnlyList<NavigationItem> NavigationItems
IReadOnlyList<DashboardSkillCard> DashboardCards
NavigationItem SelectedNavigationItem
SkillManifest? SelectedSkillManifest
bool IsDashboardSelected
bool IsAssistantSelected
bool IsSkillSelected
ICommand NavigateCommand
```

Use the existing views:

```text
KrytenAssist.Avalonia/Views/DashboardView.axaml
KrytenAssist.Avalonia/Views/SkillDetailsView.axaml
```

Do not modify these contracts.

Do not inspect concrete Skill types.

---

## Existing Assistant Workspace

The current Assistant experience is implemented directly in:

```text
KrytenAssist.Avalonia/MainWindow.axaml
KrytenAssist.Avalonia/MainWindow.axaml.cs
```

It includes:

- Prompt Library
- prompt search
- prompt selection
- create/edit overlay
- delete confirmation overlay
- AI Conversation
- send/cancel/clear commands
- keyboard Enter handling
- conversation scrolling and focus
- embedding status
- prompt double-tap handling
- initial `MainWindowViewModel.LoadAsync()`

Move this behavior into `AssistantWorkspaceView` without redesigning it.

The extracted workspace must preserve the existing bindings, names, commands, overlays and view-only event forwarding.

---

## AssistantWorkspaceView XAML

Create:

```text
KrytenAssist.Avalonia/Views/AssistantWorkspaceView.axaml
```

Use:

```xml
x:Class="KrytenAssist.Avalonia.Views.AssistantWorkspaceView"
x:DataType="viewModels:MainWindowViewModel"
```

The root should be a `UserControl`.

Move the existing Assistant workspace content from `MainWindow.axaml` into this view.

### Content to Preserve

Preserve the existing:

- two-column Prompt Library and AI Conversation layout
- `PromptList`
- `ConversationList`
- `ConversationInputBox`
- Category suggestions
- prompt editor overlay
- delete confirmation overlay
- all commands
- all visibility bindings
- all validation/error messages
- all style classes
- compiled DataTemplate types
- scrolling behavior
- minimum input sizing
- button labels

Do not change user-facing behavior or command semantics.

### Header Status

The application-level Kryten header remains in `MainWindow`.

Move only the embedding-status surface needed by the Assistant workspace if keeping it in the root header would create fragile nested bindings.

Preferred behavior:

- MainWindow header remains application identity only
- embedding status appears at the top of the Assistant workspace

Do not remove the status message.

### Workspace Root

Use a root `Grid` that can contain:

1. the existing workspace content
2. prompt editor overlay
3. delete confirmation overlay

The overlays must cover the Assistant workspace when open.

Do not make them cover the persistent left navigation.

---

## AssistantWorkspaceView Code-Behind

Create:

```text
KrytenAssist.Avalonia/Views/AssistantWorkspaceView.axaml.cs
```

Move the existing view-only behavior from `MainWindow.axaml.cs`:

- Enter-to-send handling
- Use Prompt focus forwarding
- prompt-card double-tap forwarding
- conversation-history subscription
- scroll newest conversation message into view
- focus conversation input
- initial `LoadAsync()`

Code-behind must continue to delegate commands and state to `MainWindowViewModel`.

Do not move business logic from the ViewModel.

### DataContext Lifecycle

The control receives `MainWindowViewModel` through inherited/bound DataContext.

Do not resolve it from `Program.Services`.

Handle DataContext/visual attachment safely:

- subscribe to the current ViewModel's `ConversationHistory.CollectionChanged` once
- unsubscribe when the control detaches or DataContext changes
- call `LoadAsync()` once per Assistant workspace control instance
- do not call Load repeatedly each time navigation returns to Assistant
- avoid memory leaks
- tolerate a temporarily null DataContext during initialization

A small private `MainWindowViewModel?` field and a private load guard are permitted.

Use Avalonia lifecycle overrides or events appropriate to the existing version.

Do not introduce an external behavior package.

### View-Only Event Handlers

The following event forwarding remains permitted:

```text
ConversationInput_OnKeyDown
UsePrompt_OnClick
PromptCard_OnDoubleTapped
ConversationHistory_OnCollectionChanged
```

Do not add navigation decisions here.

---

## MainWindow XAML

Replace the current Assistant-specific root with the application shell.

Use:

```xml
x:DataType="viewModels:ShellViewModel"
```

Declare namespaces for:

- shell ViewModel
- navigation models
- `KrytenAssist.Avalonia.Views`

### Root Layout

Use:

```text
Window
└── Grid
    ├── Header
    └── Main content
        ├── Left navigation
        └── Selected page
```

Keep:

- title `Kryten Assist`
- subtitle `Making Future Robin's Life Easier`
- current Width, Height, MinWidth and MinHeight unless layout requires a narrowly justified adjustment
- existing outer margin and spacing conventions

### Header

Retain the existing application identity header.

It should not contain Skill execution state.

Do not add provider-specific content.

---

## Left Navigation

Display `ShellViewModel.NavigationItems` in registry order.

Use a `ListBox` named:

```text
NavigationList
```

Bind:

```text
ItemsSource = NavigationItems
SelectedItem = SelectedNavigationItem
```

Use a one-way selected-item binding from the ViewModel and forward user selection to `NavigateCommand`.

Each item should display:

- `NavigationItem.Title`

Do not display:

- internal navigation id
- Skill id
- concrete type
- version in the navigation rail

### Selection Forwarding

Use a small `SelectionChanged` event handler in `MainWindow.axaml.cs`.

The handler should:

1. obtain the selected `NavigationItem`
2. obtain the `ShellViewModel`
3. call `NavigateCommand` if executable

This is view-only control-event forwarding.

Navigation decisions remain in `ShellViewModel`.

Do not duplicate selection state in code-behind.

### Navigation Grouping

Dashboard, Assistant and Skill items may appear in one ordered list for this prompt.

Do not add hard-coded Echo or Cruise entries.

Do not add Settings or future destinations.

---

## Selected Content

Host all three destinations in the main content area:

```xml
<views:DashboardView IsVisible="{Binding IsDashboardSelected}" />
<views:AssistantWorkspaceView
    DataContext="{Binding AssistantWorkspace}"
    IsVisible="{Binding IsAssistantSelected}" />
<views:SkillDetailsView IsVisible="{Binding IsSkillSelected}" />
```

Requirements:

- Dashboard is visible initially
- Assistant receives the exact composed `MainWindowViewModel`
- Skill details inherits the shell DataContext
- only one destination is visible at a time
- views are created without executing Skills
- navigation does not retrieve live data

Do not use reflection-based view location.

Do not dynamically instantiate views in the ViewModel.

---

## MainWindow Code-Behind

Simplify:

```text
KrytenAssist.Avalonia/MainWindow.axaml.cs
```

Responsibilities should be limited to:

- `InitializeComponent()`
- resolving `ShellViewModel` from the existing composition root
- assigning the window DataContext
- forwarding `NavigationList.SelectionChanged` to `NavigateCommand`

Remove Assistant-specific handlers after they are moved to `AssistantWorkspaceView`.

Do not:

- resolve `MainWindowViewModel` separately
- call Assistant `LoadAsync()`
- subscribe to conversation history
- execute Skills
- navigate based on concrete Skill types
- access Infrastructure
- call HTTP

The current `Program.Services` mechanism may be retained.

Do not redesign application hosting.

---

## Shell Dependency-Injection Extension

Create:

```text
KrytenAssist.Avalonia/DependencyInjection/ShellServiceCollectionExtensions.cs
```

Use:

```csharp
namespace KrytenAssist.Avalonia.DependencyInjection;
```

Implement:

```csharp
public static IServiceCollection AddShell(this IServiceCollection services)
```

Requirements:

- reject null services
- register `MainWindowViewModel` as transient
- register `ShellViewModel` as transient
- return the original service collection
- use constructor injection
- do not resolve services during registration

Do not register views or `MainWindow`.

Do not register navigation models individually.

---

## Program Composition

Update:

```text
KrytenAssist.Avalonia/Program.cs
```

Remove the existing direct registration:

```csharp
services.AddTransient<MainWindowViewModel>();
```

Call:

```csharp
services.AddShell();
```

Call order must ensure:

1. existing Assistant dependencies are registered
2. Marella provider registration is configured
3. `AddSkills()` registers Skills and registry
4. `AddShell()` registers the ViewModels

Place `AddShell()` after `AddSkills()`.

Registration itself must not resolve `ISkillRegistry`, construct `ShellViewModel` or execute a Skill.

Do not alter provider configuration.

Do not call broad Infrastructure registration.

---

## Styling

Update only:

```text
KrytenAssist.Avalonia/App.axaml
```

Add focused styles using existing DynamicResources.

Suggested classes:

```text
Border.navigation-surface
ListBox.navigation-list
ListBox.navigation-list ListBoxItem
ListBox.navigation-list ListBoxItem:selected
Border.dashboard-card
```

Use the smallest set required by the final XAML.

### Navigation Surface

Use existing resources such as:

- `KrytenSurfaceBrush`
- `KrytenSubtleSurfaceBrush`
- `KrytenSelectedSurfaceBrush`
- `KrytenAccentBrush`
- `KrytenBorderBrush`

Provide:

- clear surface boundary
- comfortable padding
- selected item background
- selected item accent/border
- keyboard-focus visibility
- stretched item content
- transparent default ListBox background

### Dashboard Card

The existing Dashboard view currently uses neutral existing styles.

Add a `dashboard-card` class only if MainWindow composition demonstrates a clear need and the existing view can consume it without modification.

Because `DashboardView.axaml` is not allowed to change in this task, do not add an unused global style.

Do not modify the theme dictionaries unless a genuinely missing semantic brush is required.

Do not hard-code light-only colors.

Do not create a new theme system.

---

## Layout and Responsiveness

The shell should remain usable at the existing minimum window size.

Use a left navigation width around:

```text
190–230 pixels
```

Allow the selected content area to use remaining width.

The Assistant workspace should retain enough width for its existing two-column layout.

Use scrolling inside destination views rather than allowing content to force the window larger.

Do not introduce a collapsible navigation rail in this prompt.

---

## Offline and Lazy Behavior

Resolving `ShellViewModel` will resolve `ISkillRegistry` and registered Skills.

This must remain safe because:

- `CruiseOfTheWeekSkill` receives an already-configured provider
- provider construction retains a configured client and parser
- no retrieval occurs until explicit Skill execution

Confirm through source inspection that:

- shell construction reads manifests only
- Dashboard construction reads cards only
- navigation selects presentation state only
- no `GetCurrentAsync` call occurs
- no HTTP request occurs

Do not add startup retrieval or status probes.

---

## Architecture Requirements

Preserve:

- `ShellViewModel` owns navigation state
- `MainWindowViewModel` owns Assistant behavior
- `ISkillRegistry` owns Skill discovery
- views remain passive
- MainWindow code-behind forwards only control events
- provider-specific behavior remains in Infrastructure

The shell UI must not reference:

- concrete Skills
- Core Cruise models
- Application Cruise contracts
- Marella Infrastructure
- AngleSharp
- `HttpClient`
- OpenAI SDK types

The Assistant workspace may continue using its existing provider-independent conversation services.

---

## Design Constraints

Prefer:

- focused UserControl extraction
- compiled bindings
- inherited/bound DataContexts
- constructor injection
- one shell DI extension
- view-only event forwarding
- existing theme resources
- registry-derived navigation

Avoid:

- service location inside UserControls
- duplicate ViewModels
- copying Assistant state
- concrete Skill checks
- automatic Skill execution
- dynamic view locators
- reflection
- assembly scanning
- routing frameworks
- unrelated UI redesign
- new packages

Do not reformat unrelated files.

---

## Explicitly Out of Scope

Do not implement:

- tests
- Prompt 035 final verification
- Skill operation discovery
- Skill execution buttons
- Cruise result presentation
- live Cruise retrieval
- loading/error states for Skills
- dynamic Skill refresh
- navigation history
- collapsible navigation
- dashboard customization
- persistence
- Cruise history
- alerts
- scheduling
- notifications
- Prompt 036 or later work

---

## Verification

Before building, inspect the final diff and confirm changes are limited to the allowed files.

Run:

```bash
dotnet build
```

Do not make a live Marella request.

Do not start external services.

Application startup is not required in this step.

The task succeeds when:

- MainWindow binds to `ShellViewModel`
- persistent left navigation renders registry-derived destinations
- Dashboard is the default content
- Assistant workspace is preserved in its own UserControl
- generic Skill details can be selected
- selected navigation has a clear visual state
- Assistant view-only behavior was moved without business-logic changes
- Assistant loads once per view instance
- shell and Assistant ViewModels resolve through DI
- `AddShell()` is called after `AddSkills()`
- shell construction and navigation perform no network request
- compiled XAML builds
- the solution builds
- no unrelated file changes occur

Report existing SQLite vulnerability and Avalonia unused-event warnings separately from warnings introduced here.

---

## Completion Report

### Files Created

Expected:

```text
KrytenAssist.Avalonia/DependencyInjection/ShellServiceCollectionExtensions.cs
KrytenAssist.Avalonia/Views/AssistantWorkspaceView.axaml
KrytenAssist.Avalonia/Views/AssistantWorkspaceView.axaml.cs
```

### Files Modified

Expected:

```text
KrytenAssist.Avalonia/App.axaml
KrytenAssist.Avalonia/MainWindow.axaml
KrytenAssist.Avalonia/MainWindow.axaml.cs
KrytenAssist.Avalonia/Program.cs
```

### Implementation Summary

Describe:

- shell layout
- registry-driven navigation
- selected destination hosting
- Assistant extraction
- lifecycle handling
- dependency-injection composition
- navigation styling
- lazy network behavior

### Build

Report command, status, warning count and error count.

Separate existing warnings from warnings introduced by this task.

### Tests

Confirm no tests were added or run because tests belong to Step 6.

### Manual Verification

State whether the application was started.

If not, state that compiled XAML and source inspection were used.

Do not perform live provider retrieval.

### Scope Check

Confirm:

- only Step 5 was implemented
- no tests or final verification were added
- no Skill execution or live retrieval was added
- no concrete Skill/provider coupling was introduced
- no persistence, caching or scheduling was added
- no documentation was modified

