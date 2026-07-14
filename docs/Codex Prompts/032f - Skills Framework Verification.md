# Codex Prompt 032f – Skills Framework Verification

## Source Prompt

Implement **Step 7 only** from:

```text
docs/AI Playbook/032 - Skills Framework.md
```

Steps 1–6 have already been implemented.

Do not begin Prompt 033.

---

## Goal

Verify that the completed Skills framework can register, discover and execute the sample `EchoSkill` without requiring a user interface, AI provider, external service or application-window startup.

This is a verification task, not a feature-development task.

The verification must demonstrate that:

- Skills services are registered through dependency injection
- `ISkillRegistry` resolves successfully
- `EchoSkill` is registered as an `ISkill`
- the registry exposes the DI-managed `EchoSkill` instance
- the Skill is discoverable using its manifest identifier
- the Skill can be executed through the `ISkill` contract
- execution uses `SkillRequest`, `SkillContext` and `SkillResult`
- the framework remains independent of UI and AI providers
- the solution builds successfully
- all tests pass

Do not add new framework behaviour merely to create additional verification mechanisms.

---

## Repository Context

The Skills framework is implemented in:

```text
KrytenAssist.Avalonia/Skills
```

Its dependency-injection registration is implemented in:

```text
KrytenAssist.Avalonia/DependencyInjection/SkillServiceCollectionExtensions.cs
```

Application startup calls:

```csharp
services.AddSkills();
```

The framework tests are located in:

```text
KrytenAssist.Avalonia.Tests/Skills
```

The existing test classes are:

```text
EchoSkillTests.cs
SkillDependencyInjectionTests.cs
SkillRegistryTests.cs
```

Use the current production contracts and existing tests as the source of truth. Do not duplicate the framework in verification-only code.

---

## Existing Framework Contract

The completed framework contains:

```text
KrytenAssist.Avalonia/Skills/Models/SkillManifest.cs
KrytenAssist.Avalonia/Skills/Models/SkillContext.cs
KrytenAssist.Avalonia/Skills/Models/SkillRequest.cs
KrytenAssist.Avalonia/Skills/Models/SkillResult.cs
KrytenAssist.Avalonia/Skills/Samples/EchoSkill.cs
KrytenAssist.Avalonia/Skills/Services/ISkill.cs
KrytenAssist.Avalonia/Skills/Services/ISkillRegistry.cs
KrytenAssist.Avalonia/Skills/Services/SkillRegistry.cs
KrytenAssist.Avalonia/DependencyInjection/SkillServiceCollectionExtensions.cs
```

The sample Skill has the identifier:

```text
sample.echo
```

and supports the operation:

```text
echo
```

with a required string parameter:

```text
message
```

---

## Allowed Changes

The expected outcome is **no source-code changes**.

Production or test files may be modified only if verification exposes a genuine defect against the requirements already defined by Prompt 032.

Any correction must:

- be minimal
- fix only the verified defect
- preserve the existing public Skill contracts
- preserve provider independence
- preserve UI independence
- include or update a focused unit test demonstrating the defect
- be described explicitly in the completion report

Do not modify documentation files as part of the verification run.

Do not update the AI Playbook, Roadmap, Backlog or Session Handover in this task. Those project-management updates will be completed separately after the verification result has been reviewed.

---

## Verification Process

### 1. Inspect the Working Tree

Run:

```bash
git status --short
```

Record any pre-existing changes before starting.

Do not modify, discard or overwrite changes that are unrelated to this task.

---

### 2. Review the Skills Architecture

Inspect the existing implementation and confirm:

- `ISkill` exposes a manifest and asynchronous execution contract
- the public contract uses only application-owned Skill models
- no OpenAI or other provider SDK types appear in the Skills contracts
- no Avalonia control, view or window types appear in the Skills contracts
- `SkillRegistry` supports registration and discovery
- duplicate identifiers are rejected case-insensitively
- registration order is preserved
- `AddSkills()` is the single dependency-injection extension point
- the registry and sample Skill use singleton lifetimes
- registry population uses DI-managed `ISkill` instances
- startup invokes `AddSkills()`

Report the evidence found. Do not refactor code that already satisfies these requirements.

---

### 3. Verify Dependency-Injection Resolution

Use the existing `SkillDependencyInjectionTests` as executable verification.

Confirm that:

- a service collection can call `AddSkills()`
- `ISkillRegistry` resolves
- `ISkill` resolves to `EchoSkill`
- the registry contains exactly the registered sample Skill expected by Prompt 032
- the registry contains the same `EchoSkill` instance supplied by dependency injection
- the registry is a singleton
- `EchoSkill` is a singleton

Do not create a separate console application, debug endpoint or UI solely for this verification.

---

### 4. Verify Skill Discovery

Confirm through the existing registry and tests that:

- `sample.echo` appears in `ISkillRegistry.Skills`
- `Find("sample.echo")` returns the registered Skill
- lookup is case-insensitive
- an unknown identifier returns `null`
- the returned manifest contains the expected identifier, name, description and version

Discovery must occur through `ISkillRegistry`. Do not access `EchoSkill` directly for this check.

---

### 5. Verify End-to-End Skill Execution

Confirm through the existing dependency-injection test that the Skill can be executed using this path:

```text
ServiceCollection
    -> AddSkills()
    -> ServiceProvider
    -> ISkillRegistry
    -> Find("sample.echo")
    -> ISkill.ExecuteAsync(...)
    -> SkillResult
```

The execution must:

- resolve the Skill through `ISkillRegistry`
- use a `SkillRequest` with operation `echo`
- supply a non-empty string `message` parameter
- supply a deterministic `SkillContext`
- call `ExecuteAsync` through the `ISkill` contract
- return a successful `SkillResult`
- return the original message without alteration

Do not call external services, access storage, start Avalonia or use an AI provider.

---

### 6. Run the Focused Skills Tests

Run the Skills test classes using the test runner's supported filter syntax.

For example:

```bash
dotnet test KrytenAssist.Avalonia.Tests/KrytenAssist.Avalonia.Tests.csproj --filter FullyQualifiedName~KrytenAssist.Avalonia.Tests.Skills
```

Report:

- total tests
- passed tests
- failed tests
- skipped tests

If the command's output does not provide a reliable total, report the result exactly as emitted rather than estimating.

---

### 7. Build and Run the Full Test Suite

From the repository root, run:

```bash
dotnet build
dotnet test
```

Report:

- each command executed
- success or failure
- warning count
- error count
- total, passed, failed and skipped tests

Clearly distinguish:

- pre-existing warnings
- warnings introduced by this task

The existing SQLite package vulnerability warnings and unrelated Avalonia command-event warnings must be reported but must not be addressed in this task.

---

### 8. Confirm the Final Working Tree

Run:

```bash
git status --short
```

Compare the result with the initial working-tree state.

The preferred result is that verification introduced no file changes.

Do not create a commit, push changes or modify documentation.

---

## Acceptance Criteria

Step 7 is complete when:

- the Skills architecture has been inspected against Prompt 032
- dependency-injection registration resolves successfully
- `EchoSkill` is registered and discoverable by `sample.echo`
- the registry exposes the DI-managed Skill instance
- `EchoSkill` executes successfully through `ISkillRegistry` and `ISkill`
- execution demonstrates `SkillRequest`, `SkillContext` and `SkillResult`
- no UI is required
- no AI provider is required
- no external service is required
- focused Skills tests pass
- the solution builds successfully
- the full test suite passes
- no new business Skill has been introduced
- no Prompt 033 work has been started
- any source correction is minimal, tested and fully reported

---

## Design Constraints

Verification must remain:

- provider independent
- UI independent
- deterministic
- offline
- isolated
- repeatable
- non-destructive

Do not introduce:

- reflection
- assembly scanning
- automatic Skill discovery
- application-window startup
- manual service location in production code
- external API calls
- file-system persistence
- network access
- sleeps or retries
- new NuGet packages
- new abstractions

---

## Explicitly Out of Scope

Do not implement:

- new Skill framework features
- new sample Skills
- Cruise functionality
- Prompt 033
- dashboards
- Avalonia UI
- AI-provider integration
- Tool integration changes
- background processing
- notifications
- persistence
- web requests
- performance benchmarks
- architecture redesign
- documentation completion
- Roadmap updates
- Backlog updates
- Session Handover updates
- Git commits or pushes

---

## Completion Report

After verification, provide:

### Framework Verification

Report whether each of the following passed or failed, with concise evidence:

- provider-independent contracts
- UI-independent contracts
- dependency-injection resolution
- singleton lifetimes
- registry population
- Skill discovery
- manifest exposure
- end-to-end execution through the registry
- successful `SkillResult`

### Files Created

List every file created.

If none were created, state:

```text
None
```

### Files Modified

List every existing file modified.

If none were modified, state:

```text
None
```

### Production Corrections

State either:

```text
None
```

or describe every correction, the verified defect that required it and the focused test covering it.

### Focused Tests

Report:

- command executed
- total
- passed
- failed
- skipped

### Build

Report:

- command executed
- success or failure
- warning count
- error count
- pre-existing warnings
- warnings introduced by this task

### Full Test Suite

Report:

- command executed
- total
- passed
- failed
- skipped

### Working Tree

Report:

- pre-verification status
- post-verification status
- whether verification introduced any changes

### Scope Check

Confirm that:

- only Step 7 was verified
- no new production feature was added
- no new Skill was added
- no UI changes were made
- no AI-provider integration was added
- no reflection or assembly scanning was added
- no NuGet packages were added
- no documentation files were modified
- Prompt 033 was not started

