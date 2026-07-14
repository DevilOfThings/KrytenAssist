# Codex Prompt 035a – Navigation Foundation

## Source Prompt

Implement **Step 1 only** from:

```text
docs/AI Playbook/035 - Dashboard and Navigation.md
```

Do not implement Steps 2–7.

---

## Goal

Introduce the small provider-independent presentation models required by the future Dashboard and application shell.

Create:

- `NavigationDestinationKind`
- `NavigationItem`
- `DashboardSkillCard`

These types should represent navigation and Skill discovery data only.

Do not add ViewModels, commands, Avalonia views, dependency-injection registrations, Skill discovery behavior or tests in this task.

---

## Allowed Project

Make implementation changes only inside:

```text
KrytenAssist.Avalonia
```

Create files only under:

```text
KrytenAssist.Avalonia/Navigation/Models/
```

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

Do not modify existing Avalonia files in this step.

Do not modify documentation, including the AI Playbook, Roadmap, Backlog, handovers or Codex prompts.

Robin will update documentation after reviewing implementation.

---

## Namespace and Structure

Use:

```csharp
namespace KrytenAssist.Avalonia.Navigation.Models;
```

Create:

```text
KrytenAssist.Avalonia/Navigation/Models/
├── DashboardSkillCard.cs
├── NavigationDestinationKind.cs
└── NavigationItem.cs
```

Keep each public type in its own file.

Do not place the types in the existing general `Models` directory.

---

## NavigationDestinationKind

Create:

```text
KrytenAssist.Avalonia/Navigation/Models/NavigationDestinationKind.cs
```

Implement:

```csharp
public enum NavigationDestinationKind
{
    Dashboard,
    Assistant,
    Skill
}
```

Requirements:

- include exactly these three values
- preserve this order
- do not assign explicit numeric values
- do not add future destinations
- do not add display formatting or behavior to the enum

This is presentation state, not a domain model.

---

## NavigationItem

Create:

```text
KrytenAssist.Avalonia/Navigation/Models/NavigationItem.cs
```

Implement an immutable sealed record representing one navigation destination.

Expose:

```csharp
public string Id { get; }
public string Title { get; }
public NavigationDestinationKind Kind { get; }
public string? SkillId { get; }
```

Use an explicit constructor:

```csharp
public NavigationItem(
    string id,
    string title,
    NavigationDestinationKind kind,
    string? skillId = null)
```

### Required Values

`Id` and `Title` must reject:

- null
- empty
- whitespace-only values

Use standard argument exceptions and preserve the supplied values exactly.

Do not trim, normalize or change case.

### Kind Validation

Reject enum values outside the defined `NavigationDestinationKind` range.

Use:

```text
ArgumentOutOfRangeException
```

Do not silently map unknown values.

### Skill Identity Rules

When `Kind` is `Skill`:

- `SkillId` is required
- null, empty and whitespace-only Skill ids must be rejected
- preserve the supplied Skill id exactly

When `Kind` is `Dashboard` or `Assistant`:

- `SkillId` must be null
- a supplied Skill id must be rejected with `ArgumentException`

These invariants prevent an ambiguous destination.

### Responsibilities

`NavigationItem` may contain only presentation identity.

It must not contain:

- `ICommand`
- delegates
- view types
- service references
- `ISkill`
- concrete Skills
- URLs
- icons tied to provider assets
- selected-state mutation
- navigation behavior

Selection belongs to the later shell ViewModel.

---

## Stable Built-In Identifiers

Do not add static catalogues or singleton instances in this step.

The later shell should use these identifiers:

```text
navigation.dashboard
navigation.assistant
```

The models must allow those values but should not own a global navigation collection.

Do not create a static helper class for identifiers.

If constants are judged necessary, stop and report the need rather than expanding this task.

---

## DashboardSkillCard

Create:

```text
KrytenAssist.Avalonia/Navigation/Models/DashboardSkillCard.cs
```

Implement an immutable sealed record representing provider-independent Skill manifest data displayed on the Dashboard.

Expose:

```csharp
public string SkillId { get; }
public string Name { get; }
public string Description { get; }
public string Version { get; }
```

Use an explicit constructor:

```csharp
public DashboardSkillCard(
    string skillId,
    string name,
    string description,
    string version)
```

All four values are required.

Reject null, empty and whitespace-only input using standard argument exceptions.

Preserve supplied values exactly.

Do not:

- trim
- normalize case
- parse or compare versions
- infer provider names
- append presentation copy
- execute a Skill

### Responsibilities

The card is discovery metadata only.

It must not contain:

- an `ISkill` instance
- `SkillManifest`
- an execution delegate
- `ICommand`
- operation names
- parameters
- result state
- loading state
- error state
- HTTP state
- Cruise data
- Marella types
- provider SDK types
- Avalonia controls or brushes

The later shell ViewModel will map `SkillManifest` into this card.

---

## Immutability and Equality

Use immutable sealed records with get-only properties.

Value equality should use the exposed values.

Do not add mutable setters, collection state or custom equality.

Do not introduce inheritance.

---

## Architecture Requirements

These models belong to the Avalonia presentation layer because they describe desktop navigation and dashboard presentation.

They must remain independent of:

- Core Cruise domain types
- Application provider contracts
- Infrastructure
- Marella
- AngleSharp
- HTTP
- OpenAI
- persistence
- Skill execution
- Avalonia control types

Using Base Class Library types is sufficient.

Do not move these models to Core or Application.

---

## Design Constraints

Prefer:

- explicit small constructors
- standard argument guards
- immutable records
- one responsibility per type
- exact value preservation

Avoid:

- factories
- static helpers
- global navigation collections
- view references
- commands
- service location
- provider-specific metadata
- speculative fields
- premature icon systems
- localization abstractions

Do not reformat unrelated files.

---

## Explicitly Out of Scope

Do not implement:

- `ShellViewModel`
- navigation commands
- selected destination state
- property-change notifications
- Skill Registry access
- Skill discovery
- dashboard mapping
- Dashboard view
- Skill details view
- Assistant workspace extraction
- MainWindow changes
- App startup changes
- dependency injection
- theme resources
- tests
- Prompt 035 verification
- Cruise retrieval
- persistence
- dashboards with live data
- Prompt 036 or later work

---

## Verification

Inspect the final diff and confirm only the three new model files were added.

Run:

```bash
dotnet build
```

Do not run or start the application.

Do not add tests in this step.

The task succeeds when:

- all three types exist in the expected namespace
- the enum contains exactly Dashboard, Assistant and Skill
- both records are immutable and sealed
- constructor invariants prevent ambiguous navigation state
- values are preserved exactly
- no provider, Skill execution or view dependency is introduced
- the solution builds
- no unrelated file changes are made

Report existing SQLite vulnerability and Avalonia unused-event warnings separately from warnings introduced by this task.

---

## Completion Report

### Files Created

List all three files.

### Files Modified

Expected:

```text
None
```

### Implementation Summary

Describe:

- destination kinds
- navigation invariants
- dashboard-card metadata
- immutability

### Build

Report command, status, warning count and error count.

Separate existing warnings from warnings introduced by this task.

### Tests

Confirm no tests were added or run because tests belong to Step 6.

### Scope Check

Confirm:

- only Step 1 was implemented
- no ViewModel or command was added
- no Skill discovery was added
- no views or styling were added
- no DI or startup change was added
- no provider-specific dependency was introduced
- no documentation was modified

