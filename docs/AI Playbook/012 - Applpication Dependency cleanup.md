# Prompt 012 – Application Dependency Injection Cleanup

## Goal

Improve the solution architecture by moving dependency injection registration to the project that owns each service.

This prompt performs an architectural cleanup only. It must not change application behaviour.

After completion, each project should register only the services it owns.

---

## Why

Following Prompt 011, the project contains:

- Domain entities
- Application use cases
- FluentValidation validators
- Repository abstractions
- Infrastructure implementations

The current dependency injection configuration works correctly, but the responsibilities are split incorrectly.

The Application project should register Application services.

The Infrastructure project should register Infrastructure implementations.

This aligns the solution more closely with Clean Architecture principles before introducing persistence and additional cross-cutting concerns.

---

## Current State

```text
KrytenAssist.Core
KrytenAssist.Application
KrytenAssist.Infrastructure
KrytenAssist.Api
```

Current registration:

```text
Program.cs
    ↓
AddInfrastructure()

Infrastructure
    ├── Repository implementation
    ├── Use cases
    └── Validators
```

---

## Target State

```text
Program.cs
    ↓
AddApplication()
AddInfrastructure()

Application
    ├── Use cases
    └── Validators

Infrastructure
    └── Repository implementations
```

---

## Architecture Rules

- Api remains the composition root.
- Core remains dependency-free.
- Application owns Application services.
- Infrastructure owns Infrastructure services.
- Application must not depend on Infrastructure.
- Infrastructure may depend on Application abstractions.
- No API behaviour should change.

---

## Objectives

### 1. Create Application Dependency Injection

Create:

```text
KrytenAssist.Application/DependencyInjection.cs
```

Add an extension method:

```csharp
services.AddApplication();
```

---

### 2. Register Application Use Cases

Move registration of these use cases into the Application project:

```text
CreatePromptCard
UpdatePromptCard
DeletePromptCard
```

---

### 3. Register Validators

Register all FluentValidation validators from the Application assembly using assembly scanning.

This should allow future validators to be discovered automatically without needing to update dependency injection manually.

---

### 4. Simplify Infrastructure Registration

`AddInfrastructure()` should only register infrastructure implementations.

Expected registration:

```text
IPromptCardRepository -> InMemoryPromptCardRepository
```

No Application use cases or validators should be registered in Infrastructure.

---

### 5. Update Program.cs

Program.cs should call both:

```csharp
builder.Services.AddApplication();
builder.Services.AddInfrastructure();
```

Program.cs remains the application composition root.

---

## Dependency Direction

The intended dependency flow is:

```text
Api
 ↓
Application
 ↓
Core
```

and:

```text
Api
 ↓
Infrastructure
 ↓
Application
 ↓
Core
```

Application must not depend on Infrastructure.

Infrastructure may depend on Application abstractions.

Core remains independent of all other projects.

---

## Implementation Strategy

Complete one step at a time.

1. Create `KrytenAssist.Application/DependencyInjection.cs`.
2. Register PromptCard use cases.
3. Register FluentValidation validators using assembly scanning.
4. Build.
5. Remove Application registrations from `KrytenAssist.Infrastructure/DependencyInjection.cs`.
6. Build.
7. Update `Program.cs` to call both:
    - `AddApplication()`
    - `AddInfrastructure()`
8. Build.
9. Smoke test Swagger:
    - POST valid request
    - POST invalid request
    - GET all
    - PUT valid request
    - DELETE request
10. Update AI Playbook.
11. Update Roadmap.
12. Update Session Handover.
13. Commit changes.

Build after every significant change.

---

## Success Criteria

Prompt 012 is complete when:

- `AddApplication()` exists.
- Use cases are registered by the Application project.
- Validators are registered automatically from the Application assembly.
- Infrastructure registers only infrastructure implementations.
- `Program.cs` calls both `AddApplication()` and `AddInfrastructure()`.
- Existing API behaviour is unchanged.
- The solution builds successfully.
- Swagger smoke tests pass.
- AI Playbook has been updated.
- Roadmap has been updated.
- Session Handover has been updated.
- Changes are committed to Git.

After completion, each project is responsible only for registering the services it owns.

---

## Do Not Do

Do not introduce:

- SQLite
- Entity Framework
- Logging
- Global exception handling
- ProblemDetails
- New endpoints
- Validation rule changes
- Domain model changes
- Repository changes unrelated to dependency injection

---

## Result

Status: Application DI cleanup successful.

## Files Created

- KrytenAssist.Application/DependencyInjection.cs

## Files Updated

- KrytenAssist.Infrastructure/DependencyInjection.cs
- KrytenAssist.Api/Program.cs

## Build

Successful.

## Swagger

Smoke test passed.