# Codex Prompt 035h – Cruise of the Week View

## Source

Implement the roadmap item:

```text
Prompt 035h – Cruise of the Week View
```

Prompt 035 has been implemented and verified.

Do not begin Prompt 036 or implement the complete Cruise Dashboard from Prompt 042.

---

## Goal

Make the existing Cruise of the Week Skill usable from the Avalonia desktop application.

When Robin selects **Cruise of the Week**, show a focused capability page where the current Marella Cruise of the Week can be retrieved explicitly and displayed as a simple readable result.

The page should:

- remain idle when opened
- retrieve only when Robin presses `Get Cruise of the Week`
- show loading state while retrieval is running
- allow an in-progress request to be cancelled
- show a controlled error with a retry action
- display the returned provider-independent Cruise data
- support refreshing the result through another explicit request
- remain deterministic and offline in tests

Do not retrieve automatically during startup, shell resolution, Dashboard display or navigation.

---

## User Experience

Selecting the Cruise of the Week navigation item or Dashboard card should show a dedicated page rather than the generic Skill details page.

Before retrieval, display:

```text
Cruise of the Week
Retrieve Marella Cruises' currently advertised Cruise of the Week.

[ Get Cruise of the Week ]
```

After a successful retrieval, display a readable summary such as:

```text
Cruise of the Week is Mediterranean Medley on Marella Explorer,
departing Palma on 27 October 2026 for 7 nights from £903 per person.
```

Also display the structured fields where available:

- cruise title
- ship
- departure date
- departure port
- duration in nights
- current price
- price basis
- promotion summary
- provider name
- source reference
- observation timestamp

Optional values should be omitted or represented honestly. Do not fabricate placeholders that look like retrieved data.

The source reference may be displayed as text. Do not add browser launching in this prompt.

---

## Existing Contracts

Reuse the existing Skill:

```text
KrytenAssist.Avalonia/Skills/Cruises/CruiseOfTheWeekSkill.cs
```

Its manifest identifier is:

```text
cruise.of-the-week
```

Its supported operation is:

```text
get-current
```

It accepts no parameters and returns:

```text
SkillResult.Data = CruiseObservation
```

Use the existing contracts:

```text
KrytenAssist.Avalonia/Skills/Services/ISkillRegistry.cs
KrytenAssist.Avalonia/Skills/Services/ISkill.cs
KrytenAssist.Avalonia/Skills/Models/SkillRequest.cs
KrytenAssist.Avalonia/Skills/Models/SkillContext.cs
KrytenAssist.Avalonia/Skills/Models/SkillResult.cs
KrytenAssist.Avalonia/Tools/IClock
KrytenAssist.Core/Cruises/CruiseObservation.cs
KrytenAssist.Core/Cruises/CruiseSnapshot.cs
KrytenAssist.Core/Cruises/CruiseOffer.cs
KrytenAssist.Core/Cruises/CruisePrice.cs
```

Do not call `ICruiseOfTheWeekProvider` directly from the ViewModel or view.

The UI must execute the capability through the existing `ISkill` contract.

---

## Allowed Projects

Production changes are limited to:

```text
KrytenAssist.Avalonia/
```

Tests are limited to:

```text
KrytenAssist.Avalonia.Tests/
```

Expected production files:

```text
KrytenAssist.Avalonia/ViewModels/CruiseOfTheWeekViewModel.cs
KrytenAssist.Avalonia/ViewModels/ShellViewModel.cs
KrytenAssist.Avalonia/Views/CruiseOfTheWeekView.axaml
KrytenAssist.Avalonia/Views/CruiseOfTheWeekView.axaml.cs
KrytenAssist.Avalonia/MainWindow.axaml
KrytenAssist.Avalonia/DependencyInjection/ShellServiceCollectionExtensions.cs
```

Expected test files:

```text
KrytenAssist.Avalonia.Tests/ViewModels/CruiseOfTheWeekViewModelTests.cs
KrytenAssist.Avalonia.Tests/ViewModels/ShellViewModelTests.cs
KrytenAssist.Avalonia.Tests/DependencyInjection/ShellDependencyInjectionTests.cs
```

A small test-only fake may be added under the existing test project.

Do not modify:

```text
KrytenAssist.Core
KrytenAssist.Core.Tests
KrytenAssist.Application
KrytenAssist.Infrastructure
KrytenAssist.Api
KrytenAssist.Api.Tests
KrytenAssist.Client
KrytenAssist.sln
```

Do not modify documentation, including the AI Playbook, Roadmap, Backlog, Session Handover or Codex prompts. Robin will update documentation after review.

Preserve existing working-tree changes. In particular, do not revert the MainWindow visibility-wrapper correction made after Prompt 035 verification.

---

## Architecture

Use this execution path:

```text
CruiseOfTheWeekView
    ↓ binding
CruiseOfTheWeekViewModel
    ↓ Find("cruise.of-the-week")
ISkillRegistry
    ↓ ISkill.ExecuteAsync
CruiseOfTheWeekSkill
    ↓
ICruiseOfTheWeekProvider
    ↓
CruiseObservation
```

The ViewModel may know the stable capability identifier and operation name because it is the capability-specific presentation adapter.

The generic shell must not inspect concrete Skill classes or Infrastructure types.

Provider-specific HTTP, parsing and configuration must remain in Infrastructure.

---

## CruiseOfTheWeekViewModel

Create:

```text
KrytenAssist.Avalonia/ViewModels/CruiseOfTheWeekViewModel.cs
```

Use:

```csharp
namespace KrytenAssist.Avalonia.ViewModels;
```

Implement a sealed `INotifyPropertyChanged` ViewModel.

### Constructor

Use constructor injection:

```csharp
public CruiseOfTheWeekViewModel(
    ISkillRegistry skillRegistry,
    IClock clock)
```

Requirements:

- reject null dependencies
- retain no `IServiceProvider`
- do not resolve Infrastructure services
- do not execute the Skill in the constructor
- do not read the clock until an explicit retrieval begins

The ViewModel may resolve and retain the `ISkill` with identifier `cruise.of-the-week` during construction, provided this performs no execution or network request.

If the Skill is unavailable, retain a controlled unavailable state rather than throwing during shell construction.

### Commands

Expose:

```text
RetrieveCommand
CancelCommand
```

`RetrieveCommand` should:

1. reject or ignore re-entry while already busy
2. clear the previous error
3. create a fresh `CancellationTokenSource`
4. read `IClock.Now` once
5. construct `new SkillRequest("get-current")`
6. construct `new SkillContext(requestedAt)`
7. execute the resolved `ISkill`
8. validate the returned `SkillResult`
9. require successful data to be a `CruiseObservation`
10. publish presentation state on success
11. convert controlled failure into an error message
12. always clear busy state and dispose request cancellation state

`CancelCommand` should cancel only the active request.

Do not use `async void` except within an internal `ICommand` event boundary where unavoidable.

Do not use `Task.Run`.

### State

Expose focused bindable state such as:

```text
CruiseObservation? Observation
bool HasObservation
bool IsBusy
bool HasError
string? ErrorMessage
bool CanRetrieve
string RetrieveButtonText
string? Summary
```

Additional read-only presentation properties for the structured fields are permitted where they keep formatting out of XAML.

Do not expose mutable domain state.

### Success Validation

Treat these as controlled failures:

- Cruise Skill is not registered
- `SkillResult.IsSuccess` is false
- successful result contains null data
- successful result data is not `CruiseObservation`

For a failed `SkillResult`, prefer its nonblank message.

Use a stable user-facing fallback message when no safe message exists.

Do not expose exception stack traces or provider internals in the UI.

Unexpected exceptions may be converted to a concise controlled retrieval error, while caller cancellation must remain distinct from failure.

### Cancellation

When Robin cancels:

- pass cancellation to `ISkill.ExecuteAsync`
- leave any previously successful observation visible
- clear the busy state
- do not show cancellation as a retrieval failure
- allow a later retry

If a new retrieval starts after a previous success, keep or clear the prior observation consistently. Prefer keeping it visible while refreshing so the page does not become empty.

### Formatting

Create the simple summary in the ViewModel from the returned domain model.

Use:

- `CruiseObservation.Snapshot.Offer`
- the first source-ordered `CruisePrice` as the current advertised price
- invariant domain values
- an explicit UK display culture for GBP and English date presentation where needed

Example:

```text
Cruise of the Week is Mediterranean Medley on Marella Explorer,
departing Palma on 27 October 2026 for 7 nights from £903 per person.
```

Formatting must:

- preserve the actual currency rather than always assuming GBP
- avoid trailing punctuation or broken grammar when departure port is absent
- use singular `night` for one night and `nights` otherwise
- preserve the price basis separately where useful
- never fabricate promotion, port, source or itinerary values

Do not add a general localization framework in this prompt.

---

## Shell Integration

Update:

```text
KrytenAssist.Avalonia/ViewModels/ShellViewModel.cs
```

Inject the focused capability ViewModel:

```csharp
CruiseOfTheWeekViewModel cruiseOfTheWeek
```

Reject null and expose the exact instance:

```csharp
public CruiseOfTheWeekViewModel CruiseOfTheWeek { get; }
```

Add focused selected-state properties:

```text
IsCruiseOfTheWeekSelected
IsGenericSkillSelected
```

Requirements:

- `IsCruiseOfTheWeekSelected` is true only when the selected Skill id is `cruise.of-the-week`
- `IsGenericSkillSelected` is true for every other selected Skill
- keep the existing `IsSkillSelected` contract intact
- raise property-change notifications for both new properties whenever selection may affect them
- selecting the Cruise navigation item and its Dashboard card reaches the same capability page
- selecting Echo still reaches generic Skill details
- selection performs no retrieval

Using the stable Skill id in these two capability-routing properties is permitted.

Do not inspect `CruiseOfTheWeekSkill` by concrete type.

Do not add Marella, provider or HTTP dependencies to `ShellViewModel`.

---

## CruiseOfTheWeekView

Create:

```text
KrytenAssist.Avalonia/Views/CruiseOfTheWeekView.axaml
KrytenAssist.Avalonia/Views/CruiseOfTheWeekView.axaml.cs
```

Use:

```xml
x:Class="KrytenAssist.Avalonia.Views.CruiseOfTheWeekView"
x:DataType="viewModels:CruiseOfTheWeekViewModel"
```

The code-behind should contain only `InitializeComponent()`.

Use existing Kryten theme resources and style classes.

The view should include:

- title and short explanation
- `Get Cruise of the Week` or `Refresh` button
- `Cancel` action visible or enabled while busy
- busy status
- controlled error surface
- retry through the retrieval command
- readable summary
- structured result details
- promotion when present
- observation timestamp and source when present

Use scrolling so the page remains usable at the existing minimum window size.

Do not:

- place retrieval logic in code-behind
- resolve services from the view
- execute automatically on attach or DataContext change
- add browser launching
- add raw JSON output
- add provider-specific DTOs
- add history charts, watch-list controls or alerts

---

## MainWindow Composition

Update:

```text
KrytenAssist.Avalonia/MainWindow.axaml
```

Preserve the corrected destination layering, where shell-owned visibility is placed on wrapper elements and a child capability view receives its own DataContext.

Compose destinations conceptually as:

```xml
<views:DashboardView IsVisible="{Binding IsDashboardSelected}" />

<Grid IsVisible="{Binding IsAssistantSelected}">
    <views:AssistantWorkspaceView DataContext="{Binding AssistantWorkspace}" />
</Grid>

<Grid IsVisible="{Binding IsCruiseOfTheWeekSelected}">
    <views:CruiseOfTheWeekView DataContext="{Binding CruiseOfTheWeek}" />
</Grid>

<views:SkillDetailsView IsVisible="{Binding IsGenericSkillSelected}" />
```

The visibility binding must remain on the shell-DataContext wrapper. Do not place `IsCruiseOfTheWeekSelected` on the same element whose DataContext is changed to `CruiseOfTheWeekViewModel`.

Only one destination should be visible at a time.

Do not add capability selection logic to `MainWindow.axaml.cs`.

---

## Dependency Injection

Update:

```text
KrytenAssist.Avalonia/DependencyInjection/ShellServiceCollectionExtensions.cs
```

Register:

```csharp
services.AddTransient<CruiseOfTheWeekViewModel>();
```

Keep:

```text
MainWindowViewModel = transient
ShellViewModel = transient
```

Requirements:

- `AddShell()` still rejects null
- registration resolves nothing eagerly
- use the existing `IClock` registration from `AddKrytenTools()`
- do not register a second clock
- do not register views
- do not register the Cruise provider here
- preserve `Program` registration order

No `Program.cs` change is expected unless compilation proves one is required. Do not alter provider configuration.

---

## Tests

All tests must be deterministic and offline.

Do not use the live Marella provider, a real `HttpClient`, OpenAI, browser automation, Avalonia UI automation, sleeps or mocking libraries.

### ViewModel Tests

Create focused tests for `CruiseOfTheWeekViewModel` using:

- a fake `ISkillRegistry`
- a fake or existing `SkillRegistry`
- a hand-written fake `ISkill`
- a fixed `IClock`
- fixed `CruiseObservation` test data

Cover:

- null dependencies rejected
- construction performs no Skill execution
- construction does not read the clock
- missing Skill produces controlled unavailable/error behavior
- retrieval sends operation `get-current`
- retrieval sends no parameters
- clock is read exactly once per request
- exact clock value reaches `SkillContext.RequestedAt`
- successful `CruiseObservation` is retained
- summary maps title, ship, date, duration, port and current price
- missing optional port and promotion are handled honestly
- one-night grammar
- non-GBP currency is not formatted as GBP
- `HasObservation`, busy, error and command state transitions
- failed `SkillResult` displays its controlled message
- null successful data is rejected
- wrong successful data type is rejected
- unexpected exception becomes a controlled error
- cancellation reaches the Skill
- cancellation is not displayed as an error
- previous result remains visible during refresh and cancellation
- re-entry does not start a second request
- retry succeeds after failure
- property-change notifications cover affected public state

Use a controllable `TaskCompletionSource<SkillResult>` fake for busy, cancellation and re-entry tests. Do not use timing delays.

### Shell Tests

Update existing `ShellViewModelTests` for the new constructor dependency.

Cover:

- exact `CruiseOfTheWeekViewModel` instance is retained
- null capability ViewModel is rejected
- Cruise navigation item sets `IsCruiseOfTheWeekSelected`
- Cruise dashboard card sets the same state
- Cruise selection makes `IsGenericSkillSelected` false
- Echo or another Skill sets `IsGenericSkillSelected` true
- built-in destinations make both capability flags false
- required property-change notifications are raised
- selecting Cruise performs no Skill execution and no clock read

Preserve all existing Prompt 035 shell tests.

### Dependency-Injection Tests

Update existing shell DI tests.

Cover:

- `CruiseOfTheWeekViewModel` is registered transient
- `ShellViewModel` resolves with the focused capability ViewModel
- repeated shell resolution receives transient capability ViewModels
- DI resolution performs no Skill execution
- DI resolution performs no clock read

Use only deterministic test registrations.

---

## Offline and Lazy Requirements

Confirm through tests and source inspection that none of these perform retrieval:

- service registration
- service-provider construction
- `ISkillRegistry` resolution
- `CruiseOfTheWeekViewModel` construction
- `ShellViewModel` construction
- application startup
- Dashboard display
- navigation-item selection
- dashboard-card selection
- view construction

Only `RetrieveCommand` may execute the Cruise Skill.

Do not make a live request as part of implementation or verification.

---

## Out of Scope

Do not implement:

- automatic retrieval on navigation
- background polling
- scheduled retrieval
- history storage
- price comparison or trends
- watch lists
- price alerts
- cabin availability
- itinerary detection
- notifications
- a complete Cruise Dashboard
- multi-provider selection
- Marella authentication or cookies
- source-page browser launching
- persistence or caching

These remain assigned to later roadmap prompts.

---

## Required Commands

Run focused tests first:

```bash
dotnet test KrytenAssist.Avalonia.Tests/KrytenAssist.Avalonia.Tests.csproj --filter "FullyQualifiedName~CruiseOfTheWeekViewModel|FullyQualifiedName~Shell"
```

Then run the complete Avalonia test project:

```bash
dotnet test KrytenAssist.Avalonia.Tests/KrytenAssist.Avalonia.Tests.csproj
```

Finally build the solution:

```bash
dotnet build KrytenAssist.sln
```

Do not run a live Cruise request.

---

## Acceptance Criteria

Prompt 035h is complete when:

- selecting Cruise of the Week shows a dedicated capability page
- Dashboard cards and left navigation reach the same page
- generic Skills still use generic Skill details
- the page remains idle until explicit retrieval
- explicit retrieval executes the existing `CruiseOfTheWeekSkill` through `ISkill`
- the current `CruiseObservation` is displayed readably
- structured cruise details are shown honestly
- loading, cancellation, failure, retry and refresh states work
- navigation and construction perform no retrieval
- the ViewModel uses the existing deterministic clock abstraction
- provider and HTTP dependencies remain outside presentation
- all new tests are deterministic and offline
- existing Prompt 035 tests continue to pass
- the complete Avalonia test project passes
- the solution builds
- no Prompt 036 or Prompt 042 functionality is introduced

---

## Completion Report

Provide:

### Summary

A concise description of the usable Cruise of the Week capability.

### Files Modified

List every created or modified file.

### Architecture

Confirm Skill-contract execution, MVVM ownership, provider isolation and lazy retrieval.

### Focused Tests

Report exact command and passed, failed and skipped counts.

### Avalonia Test Suite

Report exact command and passed, failed and skipped counts.

### Build

Report exact command, result, errors and warnings.

### Notes

List any manual checks, including the explicit live retrieval workflow Robin should perform after reviewing the implementation.

