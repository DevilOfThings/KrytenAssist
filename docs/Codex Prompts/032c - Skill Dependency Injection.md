# Codex Prompt 032c – Skill Dependency Injection

## Source Prompt

Implement **Step 4 only** from:

```text
docs/AI Playbook/032 - Skills Framework.md
```

Do not implement any later steps from Prompt 032.

---

## Goal

Register the Skill Registry with the Avalonia application's dependency-injection container.

This task introduces a single dependency-injection extension point for the Skills framework and wires it into application startup.

No concrete Skills, automatic discovery, tests or UI integration should be added.

---

## Allowed Project

Make implementation changes only inside:

```text
KrytenAssist.Avalonia
```

The solution may be built and tested from the repository root for verification.

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

Do not modify documentation files.

---

## Expected Files

Create:

```text
KrytenAssist.Avalonia/DependencyInjection/SkillServiceCollectionExtensions.cs
```

Modify:

```text
KrytenAssist.Avalonia/Program.cs
```

Do not create or modify any other files unless compilation requires a namespace import adjustment directly related to this task.

---

## SkillServiceCollectionExtensions

Create:

```text
KrytenAssist.Avalonia/DependencyInjection/SkillServiceCollectionExtensions.cs
```

Use the namespace:

```csharp
KrytenAssist.Avalonia.DependencyInjection
```

Create a public static extension class named:

```csharp
SkillServiceCollectionExtensions
```

Expose this extension method:

```csharp
public static IServiceCollection AddSkills(
    this IServiceCollection services)
```

Requirements:

- Guard against a null `services` argument using the existing project convention.
- Register `ISkillRegistry` with `SkillRegistry` as a singleton.
- Return the supplied `IServiceCollection` so registration calls can be chained.
- Keep the method synchronous.
- Do not instantiate `SkillRegistry` manually.
- Do not resolve services while configuring the container.
- Do not register any concrete `ISkill` implementations.
- Do not populate the registry.
- Do not add comments listing speculative future Skills.

The essential registration should be equivalent to:

```csharp
services.AddSingleton<ISkillRegistry, SkillRegistry>();
```

Use the required `Microsoft.Extensions.DependencyInjection` and Skills service namespaces.

---

## Program Startup

Modify:

```text
KrytenAssist.Avalonia/Program.cs
```

Add the Skills registration to the existing dependency-injection setup using:

```csharp
services.AddSkills();
```

or the equivalent call using the existing service-collection variable name in `Program.cs`.

Requirements:

- Follow the existing startup style and registration ordering.
- Place `AddSkills()` alongside the existing dependency-injection extension calls.
- Add the required namespace import only if it is not already present.
- Do not move or refactor unrelated registrations.
- Do not change application startup behaviour beyond registering the Skill Registry.

---

## Lifetime Requirement

Register the Skill Registry as a singleton:

```text
ISkillRegistry -> SkillRegistry
```

A singleton is required because the registry represents the application-wide collection of available Skills.

Do not register it as scoped or transient.

---

## Design Constraints

The implementation must remain:

- provider independent
- UI independent
- explicit
- minimal
- consistent with the existing dependency-injection extension pattern

Prefer:

- one extension method
- explicit registration
- existing project conventions

Avoid:

- reflection
- assembly scanning
- automatic discovery
- factories
- service locators
- hosted services
- startup hooks
- configuration binding
- logging
- registry population
- speculative abstractions

Do not add NuGet packages.

Do not rename or move unrelated files.

Do not reformat unrelated code.

---

## Explicitly Out of Scope

Do not implement:

- concrete Skills
- `ISkill` registrations
- Cruise Skills
- Home Energy Skills
- Finance Skills
- Interview Skills
- automatic Skill discovery
- reflection-based loading
- assembly scanning
- registry population
- Skill execution
- Skill persistence
- Skill configuration
- Skill options
- Skill settings
- Skill permissions
- Avalonia views
- Avalonia view models
- dashboards
- navigation
- menus
- Tool-to-Skill integration
- AI-provider integration
- unit tests
- Steps 5 or 6 from Prompt 032

---

## Verification

From the repository root, run:

```bash
dotnet build
dotnet test
```

Do not make unrelated changes to remove pre-existing warnings.

The task is complete when:

- `SkillServiceCollectionExtensions` exists
- `AddSkills()` registers `ISkillRegistry` with `SkillRegistry`
- the registration uses singleton lifetime
- `Program.cs` calls `AddSkills()`
- no concrete Skills are registered
- no automatic discovery is added
- the solution builds successfully
- all existing tests pass

---

## Completion Report

After implementation, report:

### Files Created

List every file created.

### Files Modified

List every existing file modified.

### Implementation Summary

Briefly describe:

- the dependency-injection extension created
- the service lifetime used
- the startup wiring added

### Build

Report:

- command executed
- success or failure
- warning count
- error count

Clearly distinguish pre-existing warnings from warnings introduced by this task.

### Tests

Report:

- command executed
- total tests
- passed
- failed
- skipped

### Scope Check

Confirm that:

- only Step 4 was implemented
- no concrete Skills were added
- no automatic discovery was added
- no tests were added or modified
- no documentation files were modified