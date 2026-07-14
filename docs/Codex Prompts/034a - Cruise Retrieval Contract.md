# Codex Prompt 034a – Cruise Retrieval Contract

## Source Prompt

Implement **Step 1 only** from:

```text
docs/AI Playbook/034 - Cruise of the Week Skill.md
```

Do not implement Steps 2–7.

---

## Goal

Create the provider-independent Application contract for retrieving the current Cruise of the Week.

This task introduces:

- `ICruiseOfTheWeekProvider`
- `CruiseOfTheWeekException`

The contract must use only Base Class Library types and the provider-independent `CruiseObservation` model from `KrytenAssist.Core`.

No Marella implementation, HTTP access, HTML parsing, Skill, dependency injection, configuration or tests should be added.

---

## Allowed Project

Make implementation changes only inside:

```text
KrytenAssist.Application
```

The solution may be built from the repository root for verification.

Do not modify:

```text
KrytenAssist.Core
KrytenAssist.Core.Tests
KrytenAssist.Infrastructure
KrytenAssist.Api
KrytenAssist.Api.Tests
KrytenAssist.Avalonia
KrytenAssist.Avalonia.Tests
KrytenAssist.Client
```

Do not add or modify test projects in this task.

Do not modify:

```text
docs/AI Playbook/034 - Cruise of the Week Skill.md
docs/Roadmap.md
docs/Backlog.md
docs/Session Handovers
```

Do not modify other Codex prompts or documentation files.

Robin will update project documentation after reviewing the implementation.

---

## Folder and Namespace

Create:

```text
KrytenAssist.Application/Cruises/
├── CruiseOfTheWeekException.cs
└── ICruiseOfTheWeekProvider.cs
```

Use the namespace:

```csharp
KrytenAssist.Application.Cruises
```

Do not create empty placeholder files for later Prompt 034 steps.

---

## ICruiseOfTheWeekProvider

Create:

```text
KrytenAssist.Application/Cruises/ICruiseOfTheWeekProvider.cs
```

Define this public contract:

```csharp
public interface ICruiseOfTheWeekProvider
{
    Task<CruiseObservation> GetCurrentAsync(
        DateTimeOffset observedAt,
        CancellationToken cancellationToken = default);
}
```

Requirements:

- return `Task<CruiseObservation>`
- use `CruiseObservation` from `KrytenAssist.Core.Cruises`
- require the caller to supply `DateTimeOffset observedAt`
- accept an optional `CancellationToken`
- have no default interface implementation
- expose no provider-specific members
- expose no transport-specific members

The caller-supplied timestamp is intentional.

It allows a future Skill to pass `SkillContext.RequestedAt` and prevents provider implementations from reading the system clock.

Do not:

- rename the method
- add synchronous methods
- add provider identifiers to the interface
- add URL properties
- add HTML parameters
- add retry or cache methods
- return infrastructure DTOs
- return `SkillResult`
- reference `ISkill`

---

## CruiseOfTheWeekException

Create:

```text
KrytenAssist.Application/Cruises/CruiseOfTheWeekException.cs
```

Implement a public sealed exception:

```csharp
public sealed class CruiseOfTheWeekException : Exception
```

Provide these constructors:

```csharp
public CruiseOfTheWeekException(string message)
    : base(message)
{
}

public CruiseOfTheWeekException(string message, Exception innerException)
    : base(message, innerException)
{
}
```

Requirements:

- represent an expected inability to retrieve or interpret the current Cruise of the Week
- support a safe message for the calling Skill
- preserve an optional underlying exception for diagnostics
- remain provider independent
- remain transport independent

Do not add:

- HTTP status properties
- response bodies
- HTML content
- Marella fields
- URLs
- retry metadata
- serialization attributes
- custom error codes
- logging behavior

Do not create separate retrieval and parsing exception hierarchies in this task.

---

## Architecture Requirements

The new Application contract may reference only:

- Base Class Library types
- `KrytenAssist.Core.Cruises.CruiseObservation`

It must not reference:

- Marella
- TUI
- Avalonia
- OpenAI or another AI provider
- Skills framework types
- Infrastructure types
- Entity Framework
- `HttpClient`
- HTTP response types
- AngleSharp
- browser types
- provider DTOs

Do not add project references or NuGet packages.

`KrytenAssist.Application` already references `KrytenAssist.Core`; use that existing inward dependency.

---

## Design Constraints

Keep the implementation:

- small
- provider independent
- transport independent
- UI independent
- persistence independent
- asynchronous
- cancellation aware
- independently testable by a later prompt

Prefer:

- one interface
- one focused exception
- application-owned public contracts
- standard .NET exception behavior

Avoid:

- speculative abstractions
- result wrappers duplicating `CruiseObservation`
- static helper classes
- factories
- service location
- comments that merely repeat the code

Do not rename, move or reformat unrelated files.

---

## Explicitly Out of Scope

Do not implement:

- Marella provider
- Marella parser
- provider options
- provider DTOs
- HTTP clients
- web requests
- HTML parsing
- AngleSharp
- retries
- caching
- persistence
- history
- comparison
- alerts
- background scheduling
- a Cruise Skill
- `ISkill` integration
- dependency-injection registration
- application startup changes
- configuration
- UI
- tests
- new test projects
- Steps 2–7 from Prompt 034

---

## Verification

Before building, inspect the final diff and confirm that only these implementation files were created:

```text
KrytenAssist.Application/Cruises/ICruiseOfTheWeekProvider.cs
KrytenAssist.Application/Cruises/CruiseOfTheWeekException.cs
```

From the repository root, run:

```bash
dotnet build
```

Do not make unrelated changes merely to remove pre-existing warnings.

The existing SQLite package vulnerability warnings and unrelated Avalonia command-event warnings should be reported but not addressed.

The task is successful when:

- both required files exist
- both use `KrytenAssist.Application.Cruises`
- the interface returns `Task<CruiseObservation>`
- the caller must supply `DateTimeOffset observedAt`
- cancellation is part of the contract
- the exception supports message-only and inner-exception construction
- no provider or transport types leak into Application
- no project references or packages were added
- the solution builds successfully

---

## Completion Report

After implementation, report:

### Files Created

List every file created.

### Files Modified

List every existing file modified.

If none were modified, state:

```text
None
```

### Implementation Summary

Briefly describe:

- the retrieval contract
- caller-supplied observation time
- cancellation support
- the controlled failure contract
- provider and transport independence

### Build

Report:

- command executed
- success or failure
- warning count
- error count

Distinguish pre-existing warnings from warnings introduced by this task.

### Tests

Confirm that no tests were added or modified and that Prompt 034 testing is reserved for Step 6.

### Scope Check

Confirm that:

- only Step 1 was implemented
- only `KrytenAssist.Application` was changed
- no Marella or TUI types were added
- no HTTP or HTML parsing was added
- no Cruise Skill was added
- no dependency-injection or configuration changes were made
- no persistence or UI behavior was added
- no tests were added or modified
- no NuGet packages or project references were added
- no documentation files were modified

