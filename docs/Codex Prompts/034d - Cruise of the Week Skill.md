# Codex Prompt 034d – Cruise of the Week Skill

## Source Prompt

Implement **Step 4 only** from:

```text
docs/AI Playbook/034 - Cruise of the Week Skill.md
```

Steps 1–3 have already been implemented.

Do not implement Steps 5–7.

---

## Goal

Implement the concrete `CruiseOfTheWeekSkill` using the existing Prompt 032 Skills framework and the provider-independent Application retrieval contract introduced by Prompt 034a.

The Skill should:

- expose stable manifest metadata
- support the `get-current` operation
- pass `SkillContext.RequestedAt` to `ICruiseOfTheWeekProvider`
- return the retrieved `CruiseObservation` through `SkillResult`
- translate expected `CruiseOfTheWeekException` failures into controlled failed results
- preserve cancellation behavior

Do not register the Skill with dependency injection or the Skill Registry in this task. Registration belongs to Step 5.

Do not add HTTP, Marella parsing, configuration, persistence, UI or tests.

---

## Allowed Project

Make implementation changes only inside:

```text
KrytenAssist.Avalonia
```

The solution may be built from the repository root for verification.

Allowed files are limited to:

```text
KrytenAssist.Avalonia/KrytenAssist.Avalonia.csproj
KrytenAssist.Avalonia/Skills/Cruises/CruiseOfTheWeekSkill.cs
```

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

## Project Reference

Add a project reference from:

```text
KrytenAssist.Avalonia
```

to:

```text
KrytenAssist.Application
```

Use:

```xml
<ProjectReference Include="..\KrytenAssist.Application\KrytenAssist.Application.csproj" />
```

This is an inward dependency on the application-owned abstraction.

Do not add a project reference to Infrastructure in this task. Infrastructure access belongs only at the composition root in Step 5.

Do not add a duplicate explicit Core project reference unless compilation genuinely requires it. The Skill should consume the observation through the Application interface and return it as `object` data in `SkillResult`.

Do not add NuGet packages.

---

## Existing Contracts

Use:

```text
KrytenAssist.Application/Cruises/ICruiseOfTheWeekProvider.cs
KrytenAssist.Application/Cruises/CruiseOfTheWeekException.cs
KrytenAssist.Avalonia/Skills/Models/SkillContext.cs
KrytenAssist.Avalonia/Skills/Models/SkillManifest.cs
KrytenAssist.Avalonia/Skills/Models/SkillRequest.cs
KrytenAssist.Avalonia/Skills/Models/SkillResult.cs
KrytenAssist.Avalonia/Skills/Services/ISkill.cs
```

Do not duplicate, move or redesign these contracts.

Do not modify `SkillRegistry` or `ISkillRegistry`.

---

## Skill Location

Create:

```text
KrytenAssist.Avalonia/Skills/Cruises/CruiseOfTheWeekSkill.cs
```

Use the namespace:

```csharp
KrytenAssist.Avalonia.Skills.Cruises
```

Implement:

```csharp
public sealed class CruiseOfTheWeekSkill : ISkill
```

Do not place the Skill in Infrastructure or Core.

---

## Constructor

Use constructor injection:

```csharp
public CruiseOfTheWeekSkill(ICruiseOfTheWeekProvider provider)
```

Requirements:

- reject a null provider with `ArgumentNullException`
- retain the supplied provider
- depend only on `ICruiseOfTheWeekProvider`

Do not:

- depend on `MarellaCruiseOfTheWeekProvider`
- depend on the parser
- depend on `HttpClient`
- accept `IServiceProvider`
- manually construct the provider
- use service location

The manifest may identify Marella in the user-facing description, but the executable dependency must remain provider independent.

---

## Manifest

Expose this exact manifest:

```text
Id: cruise.of-the-week
Name: Cruise of the Week
Description: Retrieves Marella Cruises' current Cruise of the Week.
Version: 1.0.0
```

Use an immutable manifest instance.

The identifier must remain provider neutral so a future provider-selection strategy can evolve without changing the Skill identifier.

Do not add additional manifests or operations.

---

## Supported Operation

Support exactly:

```text
get-current
```

Compare the operation using:

```text
StringComparison.OrdinalIgnoreCase
```

Examples that should be accepted:

```text
get-current
GET-CURRENT
Get-Current
```

Do not trim, rewrite or alias operation names.

---

## Request and Context Guards

At the start of execution:

- reject null `SkillRequest` with `ArgumentNullException`
- reject null `SkillContext` with `ArgumentNullException`
- honor an already-cancelled token before provider execution

Use standard .NET guard clauses.

Do not replace invalid programming inputs with failed `SkillResult` values.

---

## Unsupported Operation

For any operation other than `get-current`, return:

```csharp
SkillResult.Failure(
    $"Operation '{request.Operation}' is not supported by the Cruise of the Week Skill.")
```

Do not invoke the provider for unsupported operations.

Do not throw for an ordinary unsupported operation.

---

## Parameters

The `get-current` operation accepts no parameters.

When `request.Parameters` contains one or more entries, return:

```csharp
SkillResult.Failure(
    "The Cruise of the Week Skill does not accept parameters.")
```

Do not invoke the provider when parameters are supplied.

This keeps the initial operation deterministic and avoids silently ignoring caller mistakes.

---

## Provider Invocation

For a valid `get-current` request:

```csharp
var observation = await _provider.GetCurrentAsync(
    context.RequestedAt,
    cancellationToken);
```

Requirements:

- pass the exact `SkillContext.RequestedAt` value
- pass the exact caller cancellation token
- invoke the provider exactly once
- do not modify the returned observation
- do not read the system clock

The Skill should not know which URL was requested or how HTML was parsed.

---

## Successful Result

Return:

```csharp
SkillResult.Success(
    data: observation,
    message: "Cruise of the Week retrieved successfully.")
```

Requirements:

- result is successful
- `Data` is the exact provider-returned `CruiseObservation` instance
- the message is stable and provider neutral

Do not convert the observation into:

- a provider DTO
- a view model
- formatted text
- JSON
- a dictionary

---

## Expected Retrieval Failure

Catch only:

```text
CruiseOfTheWeekException
```

Return:

```csharp
SkillResult.Failure(exception.Message)
```

Requirements:

- preserve the safe application-owned message
- do not expose the inner exception
- do not log raw HTML
- do not add provider-specific error handling

Do not catch:

- `OperationCanceledException`
- `TaskCanceledException`
- arbitrary `Exception`

Cancellation and unexpected programming failures must propagate normally.

---

## Cancellation Behavior

Cancellation must be honored:

- before operation validation invokes external work
- by forwarding the token to the provider
- during provider execution

An already-cancelled valid request should throw `OperationCanceledException` without invoking the provider.

If the provider throws `OperationCanceledException`, allow it to propagate unchanged.

Do not convert cancellation into a failed `SkillResult`.

---

## Architecture Requirements

The Skill may reference only:

- Base Class Library types
- `KrytenAssist.Application.Cruises`
- existing Avalonia Skills contracts and models

It must not reference:

- `KrytenAssist.Infrastructure`
- Marella provider or parser types
- `HttpClient`
- AngleSharp
- HTML
- URLs
- Entity Framework
- persistence
- OpenAI or another AI provider
- Avalonia controls, views or view models

No provider-specific SDK type may appear in the Skill contract.

---

## Design Constraints

Keep the Skill:

- small
- asynchronous
- cancellation aware
- provider independent in its dependency
- UI independent
- persistence independent
- independently testable with a fake provider in Step 6

Prefer:

- constructor injection
- one supported operation
- direct domain-result forwarding
- controlled expected failure handling

Avoid:

- service location
- static mutable state
- parsing
- transport logic
- formatting
- caching
- retries
- history
- comments that merely repeat the code

Do not rename, move or reformat unrelated files.

---

## Explicitly Out of Scope

Do not implement:

- Skill registration
- changes to `AddSkills()`
- Skill Registry changes
- Infrastructure references
- Marella dependency injection
- provider options
- configuration
- startup changes
- HTTP retrieval
- HTML parsing
- retries
- caching
- persistence
- history
- comparison
- alerts
- background scheduling
- dashboards
- Avalonia views or view models
- conversation-provider integration
- tests
- fake providers
- new test projects
- live Marella verification
- Steps 5–7 from Prompt 034

---

## Verification

Before building, inspect the final diff and confirm implementation changes are limited to:

```text
KrytenAssist.Avalonia/KrytenAssist.Avalonia.csproj
KrytenAssist.Avalonia/Skills/Cruises/CruiseOfTheWeekSkill.cs
```

From the repository root, run:

```bash
dotnet build
```

Do not call the live Marella website.

Do not make unrelated changes merely to remove pre-existing warnings.

The existing SQLite package vulnerability warnings and unrelated Avalonia command-event warnings should be reported but not addressed.

The task is successful when:

- Avalonia references Application
- `CruiseOfTheWeekSkill` implements `ISkill`
- manifest metadata is exact
- only `get-current` is supported
- operation comparison is case-insensitive
- parameters are rejected
- `SkillContext.RequestedAt` is passed unchanged
- cancellation is forwarded and propagated
- provider result is returned unchanged in `SkillResult.Data`
- expected application failures become failed results
- no registration or later Prompt 034 behavior was added
- the solution builds successfully

---

## Completion Report

After implementation, report:

### Files Created

List every file created.

### Files Modified

List every existing file modified.

### Implementation Summary

Briefly describe:

- manifest metadata
- supported operation
- parameter validation
- timestamp and cancellation forwarding
- successful result behavior
- expected failure handling

### Project Reference

Report the project reference added and explain its inward dependency direction.

### Build

Report:

- command executed
- success or failure
- warning count
- error count

Distinguish pre-existing warnings from warnings introduced by this task.

### Tests

Confirm that no tests were added or modified and that Skill tests are reserved for Prompt 034 Step 6.

### Scope Check

Confirm that:

- only Step 4 was implemented
- only the allowed Avalonia files were changed
- no Infrastructure reference was added
- no Skill registration was added
- no HTTP, parsing or provider-specific dependency was added
- no dependency-injection or configuration changes were made
- no persistence or UI behavior was added
- no tests were added or modified
- no NuGet packages were added
- no documentation files were modified

