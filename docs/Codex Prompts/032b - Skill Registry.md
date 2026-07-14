# Codex Prompt 032b – Skill Registry

## Source Prompt

Implement **Step 3 only** from:

```text
docs/AI Playbook/032 - Skills Framework.md
```

Do not implement any later steps from Prompt 032.

---

## Goal

Introduce the provider-independent Skill Registry.

The registry provides a simple mechanism for storing and locating Skills during application startup.

This task introduces:

- `ISkillRegistry`
- `SkillRegistry`

No dependency injection, automatic discovery, concrete Skills or UI integration should be added.

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

Do not modify any documentation files.

---

## Files

Create:

```text
KrytenAssist.Avalonia/
└── Skills/
    └── Services/
        ├── ISkillRegistry.cs
        └── SkillRegistry.cs
```

Use the namespace:

```csharp
KrytenAssist.Avalonia.Skills.Services
```

---

## ISkillRegistry

Create:

```text
KrytenAssist.Avalonia/Skills/Services/ISkillRegistry.cs
```

Expose the following members:

```csharp
IReadOnlyCollection<ISkill> Skills { get; }

void Register(ISkill skill);

ISkill? Find(string id);
```

Requirements:

- `Skills` exposes the registered Skills as a read-only collection.
- `Find()` performs a lookup using `SkillManifest.Id`.
- Lookup must be case-insensitive.
- Return `null` if no Skill exists.
- Do not expose mutable collections.
- Do not add asynchronous methods.
- Do not add events.
- Do not add lifecycle methods.

---

## SkillRegistry

Create:

```text
KrytenAssist.Avalonia/Skills/Services/SkillRegistry.cs
```

Implement an in-memory registry.

Requirements:

- Store Skills in registration order.
- Preserve insertion order.
- Prevent duplicate Skill IDs.
- Duplicate IDs should throw:

```csharp
InvalidOperationException
```

- `Find()` must perform a case-insensitive lookup.
- `Skills` must expose a read-only collection.
- Use `SkillManifest.Id` as the unique identifier.

---

## Design Constraints

The registry must remain:

- provider independent
- UI independent
- lightweight
- deterministic
- testable

Avoid:

- reflection
- assembly scanning
- plugin discovery
- dependency injection
- configuration loading
- persistence
- logging
- caching
- service locators
- generic registry abstractions
- factories

---

## Explicitly Out of Scope

Do not implement:

- dependency injection registration
- automatic Skill discovery
- reflection-based loading
- assembly scanning
- concrete Skills
- Cruise Skills
- Home Energy Skills
- Finance Skills
- Interview Skills
- dashboards
- Avalonia views
- Avalonia view models
- Skill execution changes
- Tool integration
- tests
- Steps 4–6 from Prompt 032

---

## Verification

From the repository root, run:

```bash
dotnet build
```

Do not make unrelated changes to remove existing warnings.

The task is complete when:

- `ISkillRegistry` exists
- `SkillRegistry` exists
- registration order is preserved
- duplicate IDs are rejected
- `Find()` performs case-insensitive lookup
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

Briefly describe the registry implementation.

### Build

Report:

- command executed
- success or failure
- warning count
- error count

Identify any warnings that existed before this task.

### Scope Check

Confirm that:

- only Step 3 was implemented
- no dependency injection was added
- no automatic discovery was added
- no concrete Skills were added
- no tests were added
- no documentation files were modified