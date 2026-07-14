# Codex Prompt 032a – Skills Framework Foundation

## Source Prompt

Implement **Steps 1 and 2 only** from:

```text
docs/AI Playbook/032 - Skills Framework.md
```

Do not implement any later steps from Prompt 032.

---

## Goal

Create the initial provider-independent Skills foundation in the Avalonia project.

This task introduces:

- the Skills folder structure
- the core `ISkill` contract
- the initial immutable Skill models

No registry, dependency injection, concrete Skills, user interface, persistence or AI-provider integration should be added.

---

## Allowed Project

Make implementation changes only inside:

```text
KrytenAssist.Avalonia
```

The solution may be built from the repository root for verification.

Do not modify:

```text
KrytenAssist.Core
KrytenAssist.Application
KrytenAssist.Infrastructure
KrytenAssist.Api
KrytenAssist.Api.Tests
KrytenAssist.Avalonia.Tests
KrytenAssist.Client
```

Do not modify the roadmap, backlog, session handovers or other AI Playbook prompts.

Do not modify:

```text
docs/AI Playbook/032 - Skills Framework.md
```

Robin will update the documentation after reviewing the implementation.

---

## Step 1 – Skills Folder Structure

Create the following structure inside `KrytenAssist.Avalonia`:

```text
Skills/
├── Models/
│   ├── SkillContext.cs
│   ├── SkillManifest.cs
│   ├── SkillRequest.cs
│   └── SkillResult.cs
└── Services/
    └── ISkill.cs
```

Use namespaces that match the folder structure:

```csharp
KrytenAssist.Avalonia.Skills.Models
```

and:

```csharp
KrytenAssist.Avalonia.Skills.Services
```

Do not create empty placeholder files or folders beyond those required by this task.

---

## Step 2 – Core Skill Abstractions

### ISkill

Create:

```text
KrytenAssist.Avalonia/Skills/Services/ISkill.cs
```

The interface must expose:

```csharp
SkillManifest Manifest { get; }
```

and:

```csharp
Task<SkillResult> ExecuteAsync(
    SkillRequest request,
    SkillContext context,
    CancellationToken cancellationToken = default);
```

Requirements:

- Reference the models from `KrytenAssist.Avalonia.Skills.Models`.
- Use `Task<SkillResult>`.
- Accept a `CancellationToken`.
- Do not add default interface implementations.
- Do not add registration, discovery or lifecycle methods.
- Do not reference Avalonia controls, windows, views or view models.
- Do not reference OpenAI or any other AI provider.
- Do not reference the existing Tool interfaces.

---

### SkillManifest

Create:

```text
KrytenAssist.Avalonia/Skills/Models/SkillManifest.cs
```

Implement `SkillManifest` as a sealed immutable record containing:

```csharp
string Id
string Name
string Description
string Version
```

The properties represent:

- `Id`: a stable machine-readable Skill identifier
- `Name`: the user-facing Skill name
- `Description`: a description of the Skill capability
- `Version`: the Skill version

Requirements:

- Keep the model immutable.
- Do not add validation behaviour.
- Do not add icons, colours, routes, menu positions or dashboard metadata.
- Do not add provider-specific metadata.
- Do not add categories, tags or permissions in this step.

---

### SkillContext

Create:

```text
KrytenAssist.Avalonia/Skills/Models/SkillContext.cs
```

Implement `SkillContext` as a sealed immutable record containing:

```csharp
DateTimeOffset RequestedAt
IReadOnlyDictionary<string, object?> Values
```

Requirements:

- `Values` must support optional provider-independent contextual values.
- Supply an empty read-only dictionary when no values are provided.
- Do not expose a mutable dictionary as the default.
- Do not use `IServiceProvider`.
- Do not add database connections or repositories.
- Do not add user identity.
- Do not reference Avalonia UI types.
- Do not integrate `IRuntimeContextProvider` in this task.

A simple secondary constructor or static factory may be used to provide the empty dictionary, but do not introduce a builder or inheritance hierarchy.

---

### SkillRequest

Create:

```text
KrytenAssist.Avalonia/Skills/Models/SkillRequest.cs
```

Implement `SkillRequest` as a sealed immutable record containing:

```csharp
string Operation
IReadOnlyDictionary<string, object?> Parameters
```

Requirements:

- `Operation` identifies the action requested from the Skill.
- `Parameters` contains optional provider-independent inputs.
- Supply an empty read-only dictionary when no parameters are provided.
- Do not expose a mutable dictionary as the default.
- Do not use `JsonElement`, `JsonDocument` or other JSON-specific types.
- Do not use AI-provider request types.
- Do not add parsing or validation behaviour.
- Do not add conversation-message types.

---

### SkillResult

Create:

```text
KrytenAssist.Avalonia/Skills/Models/SkillResult.cs
```

Implement `SkillResult` as an immutable model containing:

```csharp
bool IsSuccess
string? Message
object? Data
```

Provide these static factory methods:

```csharp
public static SkillResult Success(
    object? data = null,
    string? message = null);
```

```csharp
public static SkillResult Failure(string message);
```

Requirements:

- A private constructor may be used.
- `Success` must return a result with `IsSuccess` set to `true`.
- `Failure` must return a result with `IsSuccess` set to `false`.
- A failure must contain the supplied message.
- Do not throw an exception during normal failure-result creation.
- Do not add UI presentation properties.
- Do not add HTTP status codes.
- Do not add AI-provider response types.
- Do not add error collections or validation frameworks in this step.

---

## Design Constraints

The implementation must remain:

- provider independent
- UI independent
- minimal
- immutable where practical
- compatible with the existing .NET project conventions

Prefer:

- sealed records for simple immutable data
- composition over inheritance
- explicit contracts
- read-only collection interfaces

Avoid:

- speculative abstractions
- base Skill classes
- generic Skill interfaces
- reflection
- dynamic loading
- plugin discovery
- persistence
- configuration
- logging
- dependency injection registration
- concrete business Skills
- changes to the existing Tools framework

Do not add NuGet packages.

Do not rename or move unrelated existing files.

Do not reformat unrelated code.

---

## Explicitly Out of Scope

Do not implement:

- `ISkillRegistry`
- `SkillRegistry`
- Skill registration
- dependency-injection extensions
- automatic Skill discovery
- reflection-based discovery
- assembly scanning
- concrete Cruise Skills
- Home Energy Skills
- Finance Skills
- Interview Skills
- dashboards
- navigation
- menus
- Avalonia views
- Avalonia view models
- Skill persistence
- Skill settings
- Skill permissions
- tests
- AI-provider translation
- Tool-to-Skill integration
- runtime-context integration
- Steps 3 or later from Prompt 032

---

## Verification

From the repository root, run:

```bash
dotnet build
```

Do not make unrelated changes merely to remove pre-existing warnings.

The task is successful when:

- all five required files exist
- namespaces match the folder structure
- `ISkill` exposes the required contract
- all models remain provider independent and UI independent
- no later Prompt 032 features have been implemented
- the solution builds successfully

---

## Completion Report

After implementation, report:

### Files Created

List every file created.

### Files Modified

List every existing file modified.

If no existing files were modified, state:

```text
None
```

### Implementation Summary

Briefly describe the abstractions created.

### Build

Report:

- command run
- success or failure
- warning count
- error count

Clearly distinguish pre-existing warnings from warnings introduced by this task.

### Scope Check

Confirm that:

- only Steps 1 and 2 were implemented
- no tests were added
- no dependency-injection registration was added
- no concrete Skills were added
- no documentation files were modified