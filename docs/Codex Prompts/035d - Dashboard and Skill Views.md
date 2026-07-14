# Codex Prompt 035d – Dashboard and Skill Views

## Source Prompt

Implement **Step 4 only** from:

```text
docs/AI Playbook/035 - Dashboard and Navigation.md
```

Steps 1–3 have already been implemented.

Do not implement Steps 5–7.

---

## Goal

Create the first two provider-independent shell content views:

- Dashboard
- generic Skill details

Both views should bind to the existing `ShellViewModel`.

The Dashboard should render registered Skills as discovery cards and navigate through the existing `NavigateCommand`.

The Skill details view should display the selected `SkillManifest`.

Neither view may execute a Skill, retrieve Cruise data or know about concrete Skill implementations.

Do not modify the root `MainWindow`, extract the Assistant workspace, register services or add tests in this task.

---

## Allowed Project and Files

Make implementation changes only inside:

```text
KrytenAssist.Avalonia
```

Create exactly:

```text
KrytenAssist.Avalonia/Views/DashboardView.axaml
KrytenAssist.Avalonia/Views/DashboardView.axaml.cs
KrytenAssist.Avalonia/Views/SkillDetailsView.axaml
KrytenAssist.Avalonia/Views/SkillDetailsView.axaml.cs
```

The solution may be built from the repository root.

Do not modify existing production files.

Do not modify:

```text
KrytenAssist.Avalonia/MainWindow.axaml
KrytenAssist.Avalonia/MainWindow.axaml.cs
KrytenAssist.Avalonia/App.axaml
KrytenAssist.Avalonia/App.axaml.cs
KrytenAssist.Avalonia/Program.cs
KrytenAssist.Avalonia/ViewModels/
KrytenAssist.Avalonia/Navigation/
KrytenAssist.Avalonia/Skills/
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

## Existing Binding Contract

Bind to:

```text
KrytenAssist.Avalonia/ViewModels/ShellViewModel.cs
```

The required properties are:

```csharp
IReadOnlyList<DashboardSkillCard> DashboardCards
bool HasDashboardCards
SkillManifest? SelectedSkillManifest
ICommand NavigateCommand
```

Use:

```csharp
KrytenAssist.Avalonia.Navigation.Models.DashboardSkillCard
```

inside the Dashboard item template.

Do not modify these contracts.

Do not access `ISkillRegistry` directly from a view.

---

## View Namespace

Use:

```csharp
namespace KrytenAssist.Avalonia.Views;
```

Each XAML file should declare the matching `x:Class`.

Each code-behind should contain only:

- the partial `UserControl` class
- a parameterless constructor
- `InitializeComponent()`

Do not add navigation or business logic to code-behind.

---

## DashboardView

Create:

```text
KrytenAssist.Avalonia/Views/DashboardView.axaml
```

Root control:

```text
UserControl
```

Use compiled bindings:

```xml
x:DataType="viewModels:ShellViewModel"
```

### Layout

Use a scrollable page suitable for tablet-sized windows.

Recommended structure:

```text
UserControl
└── ScrollViewer
    └── StackPanel
        ├── Page heading
        ├── Welcome/purpose text
        ├── Empty state
        └── Skill card ItemsControl
```

Use padding and spacing consistent with the current application.

The page should not assume a specific root-window size.

### Page Copy

Use:

```text
Heading: Dashboard
Introduction: Discover the capabilities available in Kryten Assist.
Empty state: No Skills are currently available.
```

Keep copy concise.

Do not mention Marella, Cruise results or online status.

### Dashboard Cards

Bind the `ItemsControl.ItemsSource` to:

```text
DashboardCards
```

Use a wrapping panel so cards flow at narrower widths.

Each card should display exactly the card's:

- `Name`
- `Description`
- `Version`

Present the version with a fixed neutral label such as:

```text
Version {0}
```

Do not display the internal Skill id on the Dashboard card.

Use text wrapping for descriptions.

Give cards a practical minimum or fixed width appropriate for the existing 1100-pixel desktop shell, while allowing wrapping.

### Open Action

Each card should include a button:

```text
Open
```

Bind:

```text
Command: ShellViewModel.NavigateCommand
CommandParameter: the current DashboardSkillCard
```

Because the button is inside a `DataTemplate`, expose the parent command through a named `ItemsControl` or another existing compiled-binding pattern.

The project already uses the named-control `Tag` pattern for commands inside item templates. Reuse that pattern if it compiles cleanly:

```xml
<ItemsControl x:Name="SkillCards"
              ItemsSource="{Binding DashboardCards}"
              Tag="{Binding NavigateCommand}">
```

Then bind the card button command to the parent control's `Tag`.

Do not add a click handler.

Do not call `ISkill.ExecuteAsync`.

### Empty State Visibility

Show the ItemsControl only when:

```text
HasDashboardCards == true
```

Show the empty-state message only when:

```text
HasDashboardCards == false
```

Use Avalonia compiled-binding negation if supported by the existing target version:

```xml
IsVisible="{Binding !HasDashboardCards}"
```

Do not add a converter or ViewModel property solely for inversion.

---

## Dashboard Card Presentation

Reuse existing theme resources and style classes.

Prefer:

- `workspace-surface`
- `message-surface` or another existing neutral surface
- `section-heading`
- `secondary-text`
- `accent-text`
- `primary` or `secondary` button classes

Do not modify `App.axaml` in this step.

Do not introduce a second theme system.

Do not hard-code light-only colors.

Use DynamicResources if a local property genuinely needs an existing theme brush.

Do not create custom dashboard styles yet; focused shell styling belongs to Prompt 035e.

---

## SkillDetailsView

Create:

```text
KrytenAssist.Avalonia/Views/SkillDetailsView.axaml
```

Root control:

```text
UserControl
```

Use compiled bindings:

```xml
x:DataType="viewModels:ShellViewModel"
```

### Layout

Use a scrollable, provider-independent details page.

Display from `SelectedSkillManifest`:

- `Name` as the primary page heading
- `Description`
- `Version`
- `Id`

Use clear fixed labels for identifier and version.

Suggested copy:

```text
Version
Identifier
Capability-specific controls will appear here when supported.
```

Do not display:

- concrete class names
- assembly names
- provider implementation details
- URLs
- operation guesses
- Cruise data
- loading/error states

### Nullable Selection

`SelectedSkillManifest` is nullable because built-in destinations have no selected Skill.

The shell will show this view only when `IsSkillSelected` is true in Prompt 035e.

Bindings should remain safe when the manifest is null.

Do not add fallback fake manifest data.

Do not add code-behind guards.

---

## Accessibility and Usability

Both views should provide:

- readable headings
- wrapped descriptions
- keyboard-focusable Open buttons
- sensible tab order
- sufficient spacing
- scrolling for smaller content areas
- theme-compatible foreground/background behavior

Do not rely on color alone to communicate meaning.

Do not add icons without accessible labels.

---

## Compiled Binding Requirements

Use explicit namespace declarations for:

```text
KrytenAssist.Avalonia.ViewModels
KrytenAssist.Avalonia.Navigation.Models
```

The Dashboard card `DataTemplate` must declare:

```xml
x:DataType="navigationModels:DashboardSkillCard"
```

Do not disable compiled bindings.

Do not use reflection-based binding as a shortcut.

Resolve all XAML compiler errors introduced by the new views.

---

## Code-Behind Requirements

Each code-behind should follow this shape:

```csharp
using Avalonia.Controls;

namespace KrytenAssist.Avalonia.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
    }
}
```

Use the corresponding type name for `SkillDetailsView`.

Do not add:

- event handlers
- service resolution
- DataContext assignment
- navigation
- Skill execution
- async startup
- HTTP access

The parent shell will supply the inherited DataContext later.

---

## Architecture Requirements

The views may reference:

- Avalonia presentation controls
- `ShellViewModel` for compiled bindings
- `DashboardSkillCard` for item-template bindings
- existing theme resources

They must not reference:

- `ISkillRegistry`
- `ISkill`
- concrete Skills
- Core Cruise models
- Application Cruise contracts
- Infrastructure
- Marella
- AngleSharp
- `HttpClient`
- OpenAI SDK types
- persistence services
- service providers

The views are passive presentation.

---

## Design Constraints

Prefer:

- passive UserControls
- compiled bindings
- inherited DataContext
- wrapping card layout
- existing theme resources
- commands over event handlers
- provider-independent copy

Avoid:

- view-owned state
- service location
- code-behind navigation
- concrete Skill checks
- operation inference
- automatic Skill execution
- live-data placeholders
- duplicated manifest data
- new packages
- unrelated XAML reformatting

---

## Explicitly Out of Scope

Do not implement:

- root application shell
- persistent left navigation
- Assistant workspace extraction
- MainWindow DataContext changes
- App startup changes
- dependency-injection registration
- navigation styling
- dashboard-specific global styles
- Skill execution
- Cruise retrieval
- Cruise result presentation
- loading/error state
- operation metadata
- tests
- Prompt 035 verification
- persistence
- Prompt 036 or later work

---

## Verification

Inspect the final diff and confirm only the four new view files were added.

Run:

```bash
dotnet build
```

The build must compile the new XAML even though the views are not yet hosted by `MainWindow`.

Do not start the application.

Do not add tests in this step.

The task succeeds when:

- both UserControls exist
- both use compiled `ShellViewModel` bindings
- Dashboard cards derive only from `DashboardCards`
- empty state is deterministic
- Open uses `NavigateCommand` with the current card
- Skill details use only `SelectedSkillManifest`
- code-behind contains initialization only
- no Skill executes
- no provider-specific dependency is introduced
- the solution builds
- no unrelated files change

Report existing SQLite vulnerability and Avalonia unused-event warnings separately from warnings introduced here.

---

## Completion Report

### Files Created

List all four view files.

### Files Modified

Expected:

```text
None
```

### Implementation Summary

Describe:

- Dashboard layout
- discovery cards
- empty state
- command binding
- generic Skill details
- passive code-behind

### Build

Report command, status, warning count and error count.

Separate existing warnings from warnings introduced by this task.

### Tests

Confirm no tests were added or run because tests belong to Step 6.

### Scope Check

Confirm:

- only Step 4 was implemented
- MainWindow and Assistant workspace were not changed
- no DI, startup or global styling changes were made
- no Skill execution or live data was added
- no concrete Skill or provider dependency was introduced
- no documentation was modified

