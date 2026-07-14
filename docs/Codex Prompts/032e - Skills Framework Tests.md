# Codex Prompt 032e – Skills Framework Tests

## Source Prompt

Implement **Step 6 only** from:

```text
docs/AI Playbook/032 - Skills Framework.md
```

Do not implement Step 7.

---

## Goal

Add comprehensive unit tests for the Skills framework and the sample `EchoSkill`.

The tests should establish the testing pattern that future Kryten Assist Skills can follow.

This task should verify:

- Skill registration
- Skill discovery
- Duplicate detection
- Unknown Skill lookup
- Manifest retrieval
- Registration order
- `EchoSkill` behaviour
- Skills dependency-injection registration
- Singleton lifetimes
- Registry population from dependency injection

Do not add new production behaviour unless a minimal correction is required to make the existing implementation conform to Prompt 032.

---

## Allowed Test Project

Make test changes only inside:

```text
KrytenAssist.Avalonia.Tests
```

Production files inside:

```text
KrytenAssist.Avalonia
```

may only be modified if a test exposes a genuine defect against the existing Prompt 032 requirements.

Any production correction must:

- be minimal
- be directly related to the failing test
- be described clearly in the completion report
- avoid refactoring unrelated code

Do not modify:

```text
KrytenAssist.Core
KrytenAssist.Application
KrytenAssist.Infrastructure
KrytenAssist.Api
KrytenAssist.Api.Tests
KrytenAssist.Client
```

Do not modify documentation files.

---

## Expected Test Structure

Create the following test files:

```text
KrytenAssist.Avalonia.Tests/
└── Skills/
    ├── EchoSkillTests.cs
    ├── SkillDependencyInjectionTests.cs
    └── SkillRegistryTests.cs
```

Use namespaces matching the test folder structure:

```csharp
KrytenAssist.Avalonia.Tests.Skills
```

Do not create one large combined test file.

---

## Existing Production Types

The tests should exercise the existing implementations:

```text
KrytenAssist.Avalonia/Skills/Models/SkillContext.cs
KrytenAssist.Avalonia/Skills/Models/SkillManifest.cs
KrytenAssist.Avalonia/Skills/Models/SkillRequest.cs
KrytenAssist.Avalonia/Skills/Models/SkillResult.cs
KrytenAssist.Avalonia/Skills/Samples/EchoSkill.cs
KrytenAssist.Avalonia/Skills/Services/ISkill.cs
KrytenAssist.Avalonia/Skills/Services/ISkillRegistry.cs
KrytenAssist.Avalonia/Skills/Services/SkillRegistry.cs
KrytenAssist.Avalonia/DependencyInjection/SkillServiceCollectionExtensions.cs
```

Use the current public contracts rather than duplicating production behaviour inside the tests.

---

## Testing Conventions

Follow the existing `KrytenAssist.Avalonia.Tests` conventions.

Use:

- xUnit
- FluentAssertions, if already used by the test project
- clear Arrange, Act and Assert structure
- descriptive test method names

Do not add any NuGet packages.

Prefer one behavioural assertion group per test.

Avoid implementation-detail assertions unless required to verify the contract.

---

# SkillRegistryTests

Create:

```text
KrytenAssist.Avalonia.Tests/Skills/SkillRegistryTests.cs
```

Test the `SkillRegistry` directly without using dependency injection.

A small private test double implementing `ISkill` may be created inside the test file.

Do not add a production fake Skill.

---

## Required Registry Tests

### Register exposes the Skill

Verify that:

- a Skill can be registered
- the registered Skill appears in `Skills`
- the same Skill instance is returned

Suggested test name:

```csharp
Register_ShouldExposeRegisteredSkill()
```

---

### Find returns the registered Skill

Verify that:

- a registered Skill can be located using its manifest identifier
- the returned instance is the same registered object

Suggested test name:

```csharp
Find_ShouldReturnRegisteredSkill()
```

---

### Find is case-insensitive

Register a Skill with an identifier such as:

```text
sample.test
```

Then locate it using different casing.

Suggested test name:

```csharp
Find_ShouldBeCaseInsensitive()
```

---

### Unknown identifier returns null

Verify that an unknown identifier does not throw and returns `null`.

Suggested test name:

```csharp
Find_ShouldReturnNull_WhenSkillIsUnknown()
```

---

### Duplicate identifier is rejected

Register two different Skill instances with the same manifest identifier.

Verify that the second registration throws:

```csharp
InvalidOperationException
```

Suggested test name:

```csharp
Register_ShouldThrow_WhenSkillIdIsDuplicated()
```

---

### Duplicate identifier casing is rejected

Register identifiers that differ only by casing.

For example:

```text
sample.test
SAMPLE.TEST
```

Verify that the second registration throws:

```csharp
InvalidOperationException
```

Suggested test name:

```csharp
Register_ShouldThrow_WhenSkillIdDiffersOnlyByCase()
```

---

### Registration order is preserved

Register at least three Skills.

Verify that `Skills` returns them in the same order.

Suggested test name:

```csharp
Skills_ShouldPreserveRegistrationOrder()
```

---

### Manifest is exposed

Register a Skill with known manifest values.

Verify that the registry exposes the Skill and its unchanged manifest.

Suggested test name:

```csharp
Skills_ShouldExposeRegisteredSkillManifest()
```

---

### Null Skill is rejected

Verify that registering a null Skill throws:

```csharp
ArgumentNullException
```

Suggested test name:

```csharp
Register_ShouldThrow_WhenSkillIsNull()
```

---

### Null identifier lookup is rejected

Verify that calling `Find(null!)` throws:

```csharp
ArgumentNullException
```

Suggested test name:

```csharp
Find_ShouldThrow_WhenIdIsNull()
```

This test should reflect the explicit null guard previously added to the registry.

---

# EchoSkillTests

Create:

```text
KrytenAssist.Avalonia.Tests/Skills/EchoSkillTests.cs
```

Instantiate `EchoSkill` directly.

Do not use dependency injection in these tests.

---

## Required Manifest Tests

Verify the manifest contains exactly:

```text
Id: sample.echo
Name: Echo
Description: Returns a supplied message to validate the Skills framework.
Version: 1.0.0
```

Suggested test name:

```csharp
Manifest_ShouldContainExpectedMetadata()
```

---

## Required Successful Execution Tests

### Valid echo succeeds

Create:

```csharp
SkillRequest(
    "echo",
    new Dictionary<string, object?>
    {
        ["message"] = "Hello, Kryten."
    })
```

Use a valid `SkillContext`.

Verify that:

- `IsSuccess` is `true`
- `Data` contains the exact original string
- `Message` is:

```text
Echo completed successfully.
```

Suggested test name:

```csharp
ExecuteAsync_ShouldReturnSuccessfulResult_ForValidEchoRequest()
```

---

### Operation comparison is case-insensitive

Execute using an operation such as:

```text
ECHO
```

Verify that execution succeeds.

Suggested test name:

```csharp
ExecuteAsync_ShouldMatchOperationCaseInsensitively()
```

---

### Returned message is not altered

Use a message with leading or trailing whitespace but containing non-whitespace text.

Verify the original value is returned unchanged.

For example:

```text
  Hello, Kryten.  
```

The Skill may inspect the value for validation, but it must not trim or transform the returned data.

Suggested test name:

```csharp
ExecuteAsync_ShouldReturnOriginalMessageWithoutTransformation()
```

---

## Required Failure Tests

### Missing message fails

Execute an `echo` request without a `message` parameter.

Verify that:

- `IsSuccess` is `false`
- `Message` is:

```text
A non-empty 'message' parameter is required.
```

Suggested test name:

```csharp
ExecuteAsync_ShouldFail_WhenMessageIsMissing()
```

---

### Null message fails

Provide:

```csharp
["message"] = null
```

Verify the same failed result.

Suggested test name:

```csharp
ExecuteAsync_ShouldFail_WhenMessageIsNull()
```

---

### Non-string message fails

Provide a numeric or other non-string value.

Verify the same failed result.

Suggested test name:

```csharp
ExecuteAsync_ShouldFail_WhenMessageIsNotAString()
```

---

### Empty message fails

Provide:

```text
""
```

Verify the same failed result.

Suggested test name:

```csharp
ExecuteAsync_ShouldFail_WhenMessageIsEmpty()
```

---

### Whitespace-only message fails

Provide a string containing only spaces.

Verify the same failed result.

Suggested test name:

```csharp
ExecuteAsync_ShouldFail_WhenMessageContainsOnlyWhitespace()
```

---

### Unsupported operation fails

Execute an operation such as:

```text
unknown
```

Verify that:

- `IsSuccess` is `false`
- the failure message identifies the unsupported operation
- no exception is thrown

Suggested test name:

```csharp
ExecuteAsync_ShouldFail_WhenOperationIsUnsupported()
```

---

## Required Guard and Cancellation Tests

### Null request is rejected

Verify that passing `null!` for `SkillRequest` throws:

```csharp
ArgumentNullException
```

Suggested test name:

```csharp
ExecuteAsync_ShouldThrow_WhenRequestIsNull()
```

---

### Null context is rejected

Verify that passing `null!` for `SkillContext` throws:

```csharp
ArgumentNullException
```

Suggested test name:

```csharp
ExecuteAsync_ShouldThrow_WhenContextIsNull()
```

---

### Cancellation is honoured

Use an already-cancelled `CancellationToken`.

Verify execution throws:

```csharp
OperationCanceledException
```

Suggested test name:

```csharp
ExecuteAsync_ShouldThrow_WhenCancellationIsRequested()
```

---

# SkillDependencyInjectionTests

Create:

```text
KrytenAssist.Avalonia.Tests/Skills/SkillDependencyInjectionTests.cs
```

Use:

```csharp
ServiceCollection
```

Call:

```csharp
services.AddSkills();
```

Then build the service provider.

Dispose of the provider appropriately.

---

## Required Dependency-Injection Tests

### Registry resolves

Verify that:

```csharp
ISkillRegistry
```

can be resolved from the service provider.

Suggested test name:

```csharp
AddSkills_ShouldRegisterSkillRegistry()
```

---

### EchoSkill resolves through ISkill

Resolve:

```csharp
IEnumerable<ISkill>
```

Verify that it contains exactly one `EchoSkill`, unless existing production registrations intentionally contain additional sample Skills.

The current expected framework contains only `EchoSkill`.

Suggested test name:

```csharp
AddSkills_ShouldRegisterEchoSkill()
```

---

### Registry contains EchoSkill

Resolve the registry and verify:

```csharp
registry.Find("sample.echo")
```

returns an `EchoSkill`.

Suggested test name:

```csharp
AddSkills_ShouldPopulateRegistryWithEchoSkill()
```

---

### Skill and registry are singletons

Resolve `ISkillRegistry` twice and verify both references are the same.

Resolve the `EchoSkill` through `IEnumerable<ISkill>` twice and verify the same Skill instance is returned.

Suggested test names:

```csharp
AddSkills_ShouldRegisterRegistryAsSingleton()
```

```csharp
AddSkills_ShouldRegisterEchoSkillAsSingleton()
```

---

### Registry contains the DI-managed Skill instance

Resolve:

- `IEnumerable<ISkill>`
- `ISkillRegistry`

Locate the `EchoSkill` in both.

Verify that the object stored in the registry is the same object resolved through dependency injection.

Suggested test name:

```csharp
AddSkills_ShouldPopulateRegistryWithTheDependencyInjectionSkillInstance()
```

This test is important because it verifies that the registry does not create a second `EchoSkill`.

---

### Registry execution works end to end

Resolve `ISkillRegistry`.

Locate:

```text
sample.echo
```

Execute it through the `ISkill` contract with a valid request and context.

Verify that the returned result succeeds and contains the original message.

Suggested test name:

```csharp
RegisteredEchoSkill_ShouldExecuteSuccessfullyThroughRegistry()
```

This is still a unit-level dependency-injection verification test.

Do not add UI or application startup tests.

---

## Test Data

Use simple inline dictionaries where practical.

For an empty parameters or context dictionary, use the existing model constructors or defaults rather than introducing test-only helpers unless repeated setup materially harms readability.

Use a fixed timestamp for `SkillContext`, for example:

```csharp
new DateTimeOffset(2026, 7, 14, 10, 0, 0, TimeSpan.Zero)
```

Do not use the current system clock in tests.

---

## Production Changes

Do not change production code merely to satisfy a preferred test style.

A production change is permitted only when:

- the existing implementation violates Prompt 032
- a required test demonstrates the defect
- the change is minimal and directly related

Do not change:

- public Skill contracts
- manifest values
- registry semantics
- dependency-injection lifetimes
- the required Echo behaviour

without reporting the conflict before proceeding.

---

## Design Constraints

The tests must remain:

- deterministic
- isolated
- readable
- fast
- independent of UI
- independent of AI providers
- independent of external services
- independent of system time
- independent of file storage
- independent of network access

Avoid:

- sleeps
- retries
- test ordering dependencies
- reflection
- mocks where a simple test double is sufficient
- shared mutable test fixtures
- application-window creation
- real API calls
- real file-system writes

---

## Explicitly Out of Scope

Do not implement:

- Prompt 032 Step 7 documentation or verification updates
- new sample Skills
- business Skills
- Cruise functionality
- dashboards
- Avalonia UI tests
- AI-provider tests
- Tool integration tests
- persistence tests
- performance benchmarks
- automatic Skill discovery
- reflection-based discovery
- assembly scanning
- new NuGet packages
- documentation edits

---

## Verification

From the repository root, run:

```bash
dotnet build
dotnet test
```

The task is complete when:

- all required registry behaviours are covered
- all required `EchoSkill` behaviours are covered
- Skills dependency-injection registration is covered
- singleton lifetimes are verified
- the registry contains the DI-managed `EchoSkill`
- `EchoSkill` executes successfully through the registry
- the solution builds successfully
- all tests pass

Pre-existing warnings must be reported separately from warnings introduced by this task.

The existing SQLite package vulnerability warnings and unrelated Avalonia command-event warnings should not be addressed in this task.

---

## Completion Report

After implementation, report:

### Files Created

List every file created.

### Files Modified

List every existing file modified.

If no production files were modified, state that explicitly.

### Tests Added

List the test classes and summarise the behaviours covered.

### Production Corrections

State either:

```text
None
```

or describe every production correction and the failing requirement that justified it.

### Build

Report:

- command executed
- success or failure
- warning count
- error count

Distinguish:

- pre-existing warnings
- warnings introduced by this task

### Tests

Report:

- command executed
- total tests
- passed
- failed
- skipped

### Scope Check

Confirm that:

- only Step 6 was implemented
- no new production feature was added
- no new Skill was added
- no UI changes were made
- no reflection or assembly scanning was added
- no NuGet packages were added
- no documentation files were modified
- Step 7 was not implemented