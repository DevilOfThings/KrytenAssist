# Codex Prompt 032d – Sample Skill

## Source Prompt

Implement **Step 5 only** from:

```text
docs/AI Playbook/032 - Skills Framework.md
```

Do not implement Steps 6 or 7.

---

## Goal

Create a deliberately simple sample Skill that proves the Skills framework can support a concrete implementation.

The sample Skill should demonstrate:

- `ISkill`
- `SkillManifest`
- `SkillRequest`
- `SkillContext`
- `SkillResult`
- explicit Skill registration
- Skill lookup through `ISkillRegistry`
- successful asynchronous execution

The sample Skill exists only to validate the framework.

It must not introduce real business functionality.

---

## Allowed Projects

Make implementation changes only inside:

```text
KrytenAssist.Avalonia
```

The solution may be built and tested from the repository root.

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
KrytenAssist.Avalonia/
└── Skills/
    └── Samples/
        └── EchoSkill.cs
```

Modify the existing Skills dependency-injection extension:

```text
KrytenAssist.Avalonia/DependencyInjection/SkillServiceCollectionExtensions.cs
```

Do not modify `Program.cs` unless compilation requires a directly related namespace correction.

Do not create or modify any other files unless required for compilation.

---

## Sample Skill Choice

Create a sample Skill named:

```csharp
EchoSkill
```

Use the namespace:

```csharp
KrytenAssist.Avalonia.Skills.Samples
```

The Skill should echo a supplied message back to the caller.

This behaviour is intentionally trivial so that the task validates the framework rather than introducing business logic.

---

## EchoSkill Contract

`EchoSkill` must:

- be a public sealed class
- implement `ISkill`
- remain provider independent
- remain UI independent
- contain no external service dependencies
- contain no mutable application state

Expose a manifest through:

```csharp
public SkillManifest Manifest { get; }
```

Use the following manifest values:

```text
Id: sample.echo
Name: Echo
Description: Returns a supplied message to validate the Skills framework.
Version: 1.0.0
```

The manifest should be immutable and reused for every execution.

---

## ExecuteAsync

Implement:

```csharp
public Task<SkillResult> ExecuteAsync(
    SkillRequest request,
    SkillContext context,
    CancellationToken cancellationToken = default)
```

Requirements:

- Guard against null `request`.
- Guard against null `context`.
- Honour cancellation by calling:

```csharp
cancellationToken.ThrowIfCancellationRequested();
```

- Support only the operation:

```text
echo
```

- Compare the operation using a case-insensitive comparison.
- Return a failed `SkillResult` for unsupported operations.
- Do not throw for an unsupported operation.
- Do not use `Task.Run`.
- Do not add artificial delays.
- Return completed results using `Task.FromResult(...)`.

---

## Echo Parameters

For the `echo` operation, read a parameter named:

```text
message
```

from:

```csharp
request.Parameters
```

Requirements:

- The parameter must exist.
- The value must be a non-empty string.
- Whitespace-only strings must be rejected.
- Missing or invalid values must return a failed `SkillResult`.
- Do not throw for normal invalid input.

Use a clear failure message such as:

```text
A non-empty 'message' parameter is required.
```

---

## Successful Result

For valid input, return:

```csharp
SkillResult.Success(
    data: message,
    message: "Echo completed successfully.")
```

The returned `Data` value must contain the original message string.

Do not transform, trim or alter the returned message except for validation purposes.

---

## Unsupported Operation Result

For an unsupported operation, return a failed result with a clear message containing the requested operation.

For example:

```text
Operation 'unknown' is not supported by the Echo Skill.
```

Do not throw an exception for unsupported operations.

---

## SkillContext Usage

The method must accept and validate `SkillContext`, demonstrating that concrete Skills receive execution context.

Do not add extra behaviour using `RequestedAt` or `Values`.

Do not place context values into the result.

Do not introduce speculative context handling.

---

## Dependency-Injection Registration

Update:

```text
KrytenAssist.Avalonia/DependencyInjection/SkillServiceCollectionExtensions.cs
```

Register `EchoSkill` as a singleton implementation of `ISkill`.

The registration should be equivalent to:

```csharp
services.AddSingleton<ISkill, EchoSkill>();
```

The registry must also contain all registered `ISkill` implementations.

Because the current `SkillRegistry` does not automatically populate itself from dependency injection, extend the existing `AddSkills()` registration so that the singleton `ISkillRegistry` is constructed from the registered `IEnumerable<ISkill>` and each Skill is explicitly registered.

Use a factory registration equivalent in behaviour to:

```csharp
services.AddSingleton<ISkillRegistry>(serviceProvider =>
{
    var registry = new SkillRegistry();

    foreach (var skill in serviceProvider.GetServices<ISkill>())
    {
        registry.Register(skill);
    }

    return registry;
});
```

Requirements:

- Register `EchoSkill` before registering `ISkillRegistry`.
- Preserve singleton lifetime for the Skill and registry.
- Use `GetServices<ISkill>()`.
- Do not use reflection.
- Do not use assembly scanning.
- Do not create a second registry instance elsewhere.
- Do not populate the registry in `Program.cs`.
- Do not add a hosted service.
- Do not add startup callbacks.
- Do not change `ISkillRegistry` or `SkillRegistry` contracts in this task.

---

## Important Architecture Check

The final dependency-injection flow should be:

```text
AddSkills()
    ├── registers EchoSkill as ISkill
    └── creates one SkillRegistry
            └── registers all DI-provided ISkill implementations
```

Resolving:

```csharp
ISkillRegistry
```

must return a registry containing the `EchoSkill`.

Resolving:

```csharp
IEnumerable<ISkill>
```

must include the same singleton `EchoSkill` instance.

---

## Design Constraints

The implementation must remain:

- provider independent
- UI independent
- deterministic
- minimal
- asynchronous by contract
- easy to test

Prefer:

- explicit registration
- immutable metadata
- simple validation
- clear failure results

Avoid:

- base Skill classes
- generic Skill abstractions
- reflection
- assembly scanning
- configuration
- persistence
- logging
- external services
- AI integration
- Tool integration
- Avalonia controls
- view models
- dashboards
- business functionality

Do not add NuGet packages.

Do not rename or move unrelated files.

Do not reformat unrelated code.

---

## Explicitly Out of Scope

Do not implement:

- unit tests
- framework verification code
- Cruise functionality
- Home Energy functionality
- Finance functionality
- Interview functionality
- dashboards
- menus
- navigation
- background processing
- notifications
- persistence
- configuration
- automatic assembly discovery
- reflection-based loading
- AI-provider integration
- Tool-to-Skill integration
- Steps 6 or 7 from Prompt 032

---

## Verification

From the repository root, run:

```bash
dotnet build
dotnet test
```

Do not add tests during this task.

Do not make unrelated changes to remove pre-existing warnings.

The task is complete when:

- `EchoSkill` implements `ISkill`
- the manifest contains the required values
- the `echo` operation accepts a valid `message`
- valid execution returns a successful `SkillResult`
- invalid input returns a failed `SkillResult`
- unsupported operations return a failed `SkillResult`
- cancellation is honoured
- `EchoSkill` is registered through dependency injection
- resolving `ISkillRegistry` returns a registry containing `sample.echo`
- no business functionality has been introduced
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

- the sample Skill
- its manifest
- supported operation
- input validation
- result behaviour
- dependency-injection registration
- registry population approach

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

Confirm that no tests were created or modified.

### Scope Check

Confirm that:

- only Step 5 was implemented
- no business Skills were added
- no UI changes were made
- no reflection or assembly scanning was added
- no tests were added or modified
- no documentation files were modified
- Steps 6 and 7 were not implemented