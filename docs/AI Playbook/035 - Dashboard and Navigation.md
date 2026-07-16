# Prompt 035 – Dashboard and Navigation

## Goal

Introduce the first dashboard and navigation experience for Kryten Assist.

Prompt 035 should transform the existing single-workspace Avalonia window into a reusable application shell with:

- persistent left navigation
- a Dashboard destination
- the existing Assistant workspace
- automatic discovery of registered Skills
- dashboard cards for discovered Skills
- navigation to a generic Skill details page
- clear selected-navigation state

The implementation must use the existing `ISkillRegistry` as the source of Skill discovery.

The dashboard and navigation layers must remain independent of Marella, HTTP, HTML parsing and concrete Skill implementations.

Opening the application, dashboard or a Skill details page must not automatically execute an online Skill.

This prompt establishes the presentation shell that later capabilities can extend. It does not implement the full Cruise Dashboard planned for Prompt 042.

---

## Why This Prompt Exists

Prompts 032–034 established the first complete capability path:

```text
Skill Framework
    ↓
Cruise Domain
    ↓
Cruise of the Week Skill
```

The application can now register, discover and execute Skills, but the desktop client has no visual mechanism for users to discover them.

The current `MainWindow` directly presents two existing areas:

- Prompt Library
- AI Conversation

This layout works as an assistant workspace, but it does not provide an application-level shell for future capabilities such as:

- Cruise Assistant
- Home Assistant
- dashboards
- watch lists
- alerts
- settings
- additional Skills

Prompt 035 introduces that shell.

The first dashboard should prove that registered Skills can appear in the UI through provider-independent metadata without hard-coding Marella or inspecting concrete Skill types.

---

## User Experience

When Kryten Assist starts, the user should see a persistent application shell.

The left navigation should contain:

```text
Dashboard
Assistant
Skills
  Echo
  Cruise of the Week
```

The exact visual treatment may use grouped navigation rather than literal nested controls, but the hierarchy must be clear.

### Dashboard

The Dashboard is the default destination.

It should display:

- a clear page title
- a short welcome or purpose statement
- one card for every registered Skill
- Skill name
- Skill description
- Skill version where useful
- an action to open the Skill details page

Dashboard cards are discovery cards in this prompt.

They must not:

- execute the Skill automatically
- retrieve live Cruise data
- display fabricated or bundled Cruise values
- know about Marella infrastructure
- infer Skill operations

### Assistant

Selecting Assistant should display the existing Prompt Library and AI Conversation workspace with its current behavior preserved.

Prompt creation, editing, deletion, search, conversation memory, tool execution and runtime context should continue to work as before.

### Skill Details

Selecting a Skill from navigation or a dashboard card should display a generic Skill details page.

The page should show provider-independent manifest information:

- name
- description
- version
- identifier

Prompt 035 does not yet define a universal operation-discovery contract.

Therefore the generic details page should not guess operation names or parameters and should not execute the Skill.

Future capability-specific pages can replace or extend this generic destination.

### Navigation Behavior

- Dashboard is selected initially.
- Only one destination is selected at a time.
- Selecting Dashboard shows the Dashboard.
- Selecting Assistant shows the existing assistant workspace.
- Selecting a Skill shows that Skill's details.
- Selecting a dashboard card navigates to the same Skill destination as the left navigation.
- Unknown or stale Skill identifiers are handled safely.
- Navigation does not access external services.

---

## Design Principles

### Registry-Driven Discovery

`ISkillRegistry.Skills` is the source of truth for registered Skills.

Do not maintain a second hard-coded Skill catalogue.

Built-in application destinations such as Dashboard and Assistant may be explicit.

Skill destinations and dashboard cards must be derived from Skill manifests.

### Provider Independence

Presentation code may consume:

- `ISkillRegistry`
- `ISkill`
- `SkillManifest`
- application-owned dashboard/navigation models

Presentation code must not consume:

- `MarellaCruiseOfTheWeekProvider`
- `MarellaCruiseOfTheWeekParser`
- Marella options
- AngleSharp
- `HttpClient`
- OpenAI SDK types

The generic shell must not branch on concrete Skill types.

### No Automatic Online Work

Application startup and navigation must remain offline-safe.

Do not execute Skills when:

- building the service provider
- resolving the shell ViewModel
- opening Dashboard
- creating dashboard cards
- selecting a Skill
- rendering Skill details

Cruise retrieval remains explicit behavior and is not added by this prompt.

### MVVM

Navigation state and behavior belong in ViewModels.

Do not place navigation decisions in XAML code-behind.

Code-behind may retain view-only behavior already required by Avalonia controls, such as focus, scrolling or double-click forwarding.

### Preserve Existing Functionality

The existing assistant workspace is complete functionality.

Prompt 035 should relocate or compose it without redesigning its behavior.

Do not rewrite prompt management, search, embeddings, conversations, memory, tools or runtime context.

---

## Architecture Overview

```text
MainWindow
    ↓
ShellViewModel
    ├── NavigationItems
    ├── DashboardCards
    ├── SelectedDestination
    ├── NavigateCommand
    └── AssistantWorkspace
            ↓
        MainWindowViewModel
```

Skill discovery path:

```text
ISkillRegistry
    ↓
registered ISkill instances
    ↓
SkillManifest
    ├── NavigationItem
    └── DashboardSkillCard
```

Selection path:

```text
Dashboard card or Navigation item
    ↓
ShellViewModel.Navigate(...)
    ↓
Selected destination
    ↓
Dashboard / Assistant / Skill details visibility
```

No provider-specific dependency should appear in this path.

---

## Ownership Boundaries

### Skills Framework

Continues to own:

- `ISkill`
- `ISkillRegistry`
- `SkillManifest`
- registration and discovery semantics

Do not redesign the registry.

### Shell Presentation

Owns:

- navigation destination models
- dashboard card presentation models
- selected destination state
- navigation commands
- conversion from Skill manifests into display models

### Existing Assistant Workspace

`MainWindowViewModel` continues to own:

- prompt library
- prompt editing
- prompt search
- prompt selection
- conversations
- memory commands
- conversation busy/error state

It should not gain Skill discovery or shell navigation responsibilities.

### Views

Own only layout and bindings.

The root `MainWindow` should host the shell.

The existing assistant content may be extracted into a focused user control so it can be shown as one shell destination.

---

## Navigation Models

Introduce small immutable presentation models.

Suggested concepts:

```text
NavigationDestinationKind
NavigationItem
DashboardSkillCard
```

Names may follow existing project conventions, but responsibilities must remain focused.

### NavigationDestinationKind

Represent only the destination categories required now:

```text
Dashboard
Assistant
Skill
```

Do not add future destinations pre-emptively.

### NavigationItem

A navigation item should expose enough provider-independent state for rendering and selection, such as:

- stable id
- display title
- destination kind
- optional Skill id

Requirements:

- Dashboard and Assistant have stable application-owned identifiers.
- Skill items use their manifest identifier as the stable Skill reference.
- required strings reject invalid construction where models enforce invariants.
- the model contains no command, service or view reference.

### DashboardSkillCard

A dashboard card should expose:

- Skill id
- name
- description
- version

It should be created from a `SkillManifest`.

It must not expose:

- concrete `ISkill`
- execution delegates
- operation names
- Marella details
- HTTP state
- arbitrary provider output

---

## Shell ViewModel

Create a focused shell ViewModel, for example:

```text
ShellViewModel
```

It should receive through constructor injection:

- `ISkillRegistry`
- the existing `MainWindowViewModel` used as the Assistant workspace

Do not manually instantiate either dependency.

### Initialization

During construction:

1. create Dashboard navigation
2. create Assistant navigation
3. enumerate the registered Skills once
4. create Skill navigation items in registry order
5. create Dashboard cards in the same Skill order
6. select Dashboard

Enumeration must not execute Skills.

### Exposed State

Expose provider-independent bindable state such as:

- navigation items or grouped built-in/Skill collections
- dashboard cards
- selected navigation item
- selected Skill manifest/details
- Assistant workspace ViewModel
- `IsDashboardSelected`
- `IsAssistantSelected`
- `IsSkillSelected`
- navigation command

Use the smallest coherent API needed by the views.

### Navigation Command

The navigation command should accept a navigation item or stable destination identifier.

It must:

- reject or safely ignore invalid input according to existing command conventions
- update selected state
- update selected Skill details
- raise all required property-change notifications
- avoid executing the selected Skill
- avoid resolving services dynamically

### Dashboard Card Navigation

A dashboard-card action should navigate using the card's Skill identifier.

It should reuse the same selection logic used by left navigation.

Do not create a separate navigation path with different behavior.

---

## Skill Discovery

Skill discovery must use:

```csharp
ISkillRegistry.Skills
```

Preserve registry order.

The existing order is expected to be:

1. Echo
2. Cruise of the Week

The shell should not sort Skills unless a clear existing application convention requires it.

This preserves intentional registration order and makes behavior deterministic.

### Duplicate Skills

Duplicate manifest identifiers are already rejected by `SkillRegistry`.

Do not add duplicate handling to the shell.

### Empty Registry

The shell should still display Dashboard and Assistant if no Skills exist.

The Dashboard should display a clear empty state such as:

```text
No Skills are currently available.
```

Do not add a fake Skill solely to avoid the empty state.

### Manifest Data

Use manifest values exactly for display.

Do not reinterpret descriptions or create provider-specific labels.

---

## View Structure

Prefer a small set of focused Avalonia views.

Suggested structure:

```text
KrytenAssist.Avalonia/
├── MainWindow.axaml
├── ViewModels/
│   ├── ShellViewModel.cs
│   └── MainWindowViewModel.cs
└── Views/
    ├── DashboardView.axaml
    ├── AssistantWorkspaceView.axaml
    └── SkillDetailsView.axaml
```

Code-behind files generated for Avalonia views are permitted for `InitializeComponent` and view-only event forwarding.

Do not place business or navigation logic in them.

### MainWindow

The root window should contain:

- existing application header
- left navigation
- selected content area
- existing overlays in the appropriate workspace/view

It should bind to `ShellViewModel`.

### AssistantWorkspaceView

Move or compose the existing Prompt Library and AI Conversation workspace without changing behavior.

Preserve:

- compiled bindings
- commands
- scrolling
- selection
- prompt editor overlay
- delete confirmation
- conversation input
- status messaging

If extracting the workspace creates disproportionate risk, an equivalent composition may be used, but the root shell and assistant state must still be separated cleanly.

### DashboardView

Display dashboard cards using an `ItemsControl` or equivalent.

Cards should be usable at tablet-oriented window sizes and support wrapping.

Each card should expose an Open action bound to shell navigation behavior.

Display a deterministic empty state when no cards exist.

### SkillDetailsView

Display the selected manifest without concrete Skill type checks.

Include a clear message that capability-specific controls will appear here when supported.

Do not expose internal implementation names or provider types.

---

## Visual Design

Reuse the established Kryten theme resources and class conventions.

The shell should feel consistent with Prompt 031c.

Prefer:

- existing surface brushes
- existing accent treatment
- clear selected navigation state
- restrained card layout
- readable spacing
- tablet-friendly touch targets
- scrollable content where required

Do not introduce a new theme system.

Do not hard-code a large collection of colors directly into views.

Add focused theme resources or classes only where necessary for:

- navigation rail
- selected navigation item
- dashboard cards
- page headings

Preserve light and dark theme compatibility.

---

## Dependency Injection

Register the shell and presentation ViewModels through an extension method where practical.

Expected lifetimes:

- `MainWindowViewModel`: retain an intentional lifetime compatible with existing behavior
- `ShellViewModel`: transient unless application shell ownership clearly requires singleton
- navigation/card models: created by the shell, not registered individually

Update application startup so `MainWindow` receives or resolves `ShellViewModel`.

Do not manually populate Skill navigation in `Program.cs`.

Do not resolve concrete Skills in the composition root.

Do not change Infrastructure registration.

---

## App and Main Window Composition

Inspect the existing startup path in:

```text
KrytenAssist.Avalonia/App.axaml.cs
KrytenAssist.Avalonia/Program.cs
KrytenAssist.Avalonia/MainWindow.axaml.cs
```

Update only the minimum required composition so the main window uses `ShellViewModel`.

Do not introduce a service locator into ViewModels.

The existing static `Program.Services` composition mechanism may be retained unless the current startup structure supports a cleaner incremental injection without unrelated redesign.

Do not redesign application hosting in this prompt.

---

## Offline-First Behavior

The dashboard must work when the machine is offline.

Offline behavior includes:

- application starts
- Dashboard renders
- navigation renders
- registered Skills are discoverable
- Skill details render
- Assistant workspace remains usable
- no Cruise retrieval occurs automatically

Do not add bundled stale Cruise data as a fallback.

Do not display an online failure merely because the dashboard opened.

Online behavior belongs to explicit capability actions introduced by later prompts.

---

## Accessibility and Usability

Provide:

- readable navigation labels
- visible selected state
- keyboard-focusable buttons
- sensible tab order
- tooltips only where labels are insufficient
- text wrapping for long Skill descriptions
- scroll support for smaller windows
- empty-state text
- no color-only selection indication where practical

Maintain the existing minimum window constraints unless testing shows a justified adjustment.

---

## Testing Strategy

All tests must remain deterministic and offline.

### Navigation Model Tests

Cover:

- stable Dashboard and Assistant identifiers
- Skill destination identity
- destination kind
- optional Skill id semantics
- required-value guards where implemented

### Shell ViewModel Tests

Use a real `SkillRegistry` with simple deterministic Skills or existing registered test Skills.

Verify:

- Dashboard is selected initially
- Dashboard and Assistant navigation always exist
- registered Skills become navigation items
- registered Skills become dashboard cards
- manifest values are preserved exactly
- registry order is preserved
- empty registry behavior
- navigation selection changes
- selected-state boolean properties
- selected Skill details
- dashboard card navigation reuses Skill selection
- property-change notifications
- navigation never executes a Skill

Avoid constructing the full production assistant service graph where a focused test double for the assistant workspace dependency is practical. If `ShellViewModel` requires the concrete existing ViewModel, introduce only the minimum testable composition consistent with the architecture; do not add a service-locator workaround.

### ViewModel DI Tests

Verify:

- `ShellViewModel` resolves through DI
- `ISkillRegistry` supplies registered Skills
- the shell exposes Echo and Cruise Skill cards
- resolving the shell does not call the Cruise provider
- lifetimes are intentional

Use a deterministic fake `ICruiseOfTheWeekProvider`.

### Existing Behavior Tests

Update existing `MainWindowViewModel` tests only where constructor or composition changes require it.

Existing prompt, search and conversation tests must continue to pass.

Do not rewrite unrelated tests.

### View Verification

Build-time compiled XAML is required.

Automated visual snapshot testing is not required.

Perform focused source/binding inspection for:

- selected navigation bindings
- content visibility
- dashboard cards
- assistant workspace composition
- Skill details
- empty state

Do not start external services.

---

## Scope

### In Scope

- application shell
- persistent left navigation
- Dashboard destination
- Assistant destination
- Skill-derived navigation destinations
- registry-driven Skill discovery
- dashboard discovery cards
- generic Skill details
- selected navigation state
- reuse of existing assistant workspace
- dependency injection
- deterministic unit tests
- build and regression verification

### Out of Scope

- automatic Skill execution
- universal Skill operation metadata
- Cruise result presentation
- Cruise history
- price charts
- watch lists
- alerts
- cabin availability
- itinerary detection
- background refresh
- scheduling
- notifications
- persistence changes
- new databases or migrations
- dashboard customization
- drag-and-drop cards
- user-defined navigation
- permissions
- authentication
- multiple windows
- deep linking
- browser navigation
- React dashboard
- mobile application
- conversation-provider changes
- OpenAI changes
- Marella parser or provider changes

---

## Implementation Steps

### Step 1 – Navigation Foundation

Introduce provider-independent navigation and dashboard presentation models.

Define only Dashboard, Assistant and Skill destination concepts.

Do not add views or modify the existing workspace.

---

### Step 2 – Shell ViewModel

Implement the shell state and navigation behavior.

Inject `ISkillRegistry` and compose the existing assistant workspace.

Select Dashboard initially.

Do not execute Skills during discovery or navigation.

---

### Step 3 – Skill Discovery and Dashboard Cards

Map registered Skill manifests into:

- Skill navigation items
- dashboard cards
- generic Skill details

Preserve registry order and support an empty registry.

Do not inspect concrete Skill types.

---

### Step 4 – Dashboard and Skill Views

Create focused Dashboard and generic Skill details views.

Use manifest-based presentation only.

Do not add live Cruise retrieval or capability-specific controls.

---

### Step 5 – Application Shell, Assistant Workspace and Composition

Refactor the root `MainWindow` into the persistent shell.

Preserve the existing Prompt Library, conversation workspace and overlays as the Assistant destination.

Keep navigation behavior in ViewModels.

Register the shell composition and update startup.

Add focused navigation/dashboard theme resources while preserving existing light/dark styling.

Confirm resolving the shell performs no network request.

---

### Step 6 – Automated Tests

Add deterministic tests for:

- navigation models
- shell ViewModel
- Skill discovery
- dashboard cards
- selection behavior
- DI resolution
- non-execution during discovery/navigation
- existing assistant behavior

No test may call an external service.

---

### Step 7 – Verification

Verify:

- architecture boundaries
- registry-driven discovery
- dashboard and navigation bindings
- existing Assistant functionality
- offline startup behavior
- focused tests
- full build
- complete regression suite

No live Cruise request is required.

---

## Acceptance Criteria

Prompt 035 is complete when:

- a persistent application shell exists
- Dashboard is the default destination
- Assistant preserves the existing workspace
- registered Skills appear in left navigation
- registered Skills appear as dashboard cards
- Skill order matches registry order
- dashboard cards navigate to generic Skill details
- selected navigation state is clear
- empty Skill discovery is handled
- UI code does not inspect concrete Skill types
- opening Dashboard or Skill details executes no Skill
- no provider-specific Infrastructure type enters shell presentation
- existing prompt and conversation behavior remains intact
- the shell resolves through dependency injection
- all automated tests are offline and deterministic
- the solution builds
- all tests pass
- no Cruise history, scheduling or full Cruise dashboard is introduced
- the shell is ready for future capability-specific pages

---

## Results

### Status

✅ Complete. Dashboard and navigation were implemented through Prompts 035a–035g,
then 035h added the dedicated Cruise of the Week capability page.

### Files Created

- navigation models and `ShellViewModel`
- `DashboardView`, `SkillDetailsView` and `AssistantWorkspaceView`
- shell dependency-injection registration
- dashboard, navigation, shell and DI tests
- `CruiseOfTheWeekViewModel` and `CruiseOfTheWeekView`
- Cruise of the Week ViewModel and shell-routing tests

### Files Updated

- `KrytenAssist.Avalonia/App.axaml`
- `KrytenAssist.Avalonia/MainWindow.axaml`
- `KrytenAssist.Avalonia/MainWindow.axaml.cs`
- `KrytenAssist.Avalonia/Program.cs`
- related shell and test-composition files
- `docs/Roadmap.md`

### Build

✅ `dotnet build KrytenAssist.sln --no-restore`

Verified again on 16 July 2026: succeeded with 0 errors. Seven pre-existing
warnings remain and none were introduced by this documentation backfill.

### Tests

✅ `dotnet test KrytenAssist.sln --no-build --no-restore`

Verified again on 16 July 2026: 231 passed, 0 failed and 0 skipped.

### Manual Verification

Dashboard navigation, Assistant composition and the dedicated Cruise of the Week
view were exercised. The presentation workflow works, including controlled error
handling, but live TUI retrieval is unavailable through the current HTTP provider.

### Git Commits

- `6a9f85b` through `1480d75` – navigation foundation through verification
- `97f3b64` – Cruise of the Week View

---

## Lessons Learned

- Registry-driven Skill discovery keeps the shell independent of concrete Skill
  implementations.
- The Assistant workspace must be composed as a single shell destination; placing
  overlapping views directly in the window creates layering and visibility bugs.
- Capability-specific presentation can remain MVVM-driven while executing through
  the generic `ISkill` contract.
- Navigation should never execute a network-backed Skill. Retrieval belongs behind
  an explicit user action with loading, cancellation, error and retry states.
- A polished view cannot make an inaccessible external source usable; acquisition
  feasibility must be treated separately from presentation completeness.
- The failed TUI route clarified the next product goal: cruise discovery, explicit
  browser-assisted capture, saved candidates, ratings and later price comparison.
