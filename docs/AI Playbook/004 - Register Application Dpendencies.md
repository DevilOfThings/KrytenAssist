# Prompt 004 - Register Application Dependencies

**Date:** 30 June 2026  
**Project:** Kryten Assist  
**Motto:** Making Future Robin's Life Easier  
**Purpose:** Register the first application and infrastructure services with dependency injection.

---

## Goal

Wire the existing Application and Infrastructure layers into the API project using dependency injection.

This prompt should keep the task deliberately small and focused. No controllers or endpoints should be created yet.

---

## Prompt

```text
You are an experienced Senior .NET software engineer helping build a long-term project called Kryten Assist.

Project Vision
--------------
Kryten Assist is a personal digital assistant whose purpose is:

"Making Future Robin's Life Easier."

The project follows Clean Architecture principles and is being built as both a learning exercise and a portfolio project.

Current Solution
----------------
The solution currently contains:

- KrytenAssist.Core
- KrytenAssist.Application
- KrytenAssist.Infrastructure
- KrytenAssist.Api

The Core project contains the PromptCard domain entity.

The Application project contains:
- CreatePromptCard
- CreatePromptCardRequest
- CreatePromptCardResponse
- IPromptCardRepository

The Infrastructure project contains:
- InMemoryPromptCardRepository

Task
----
Register the existing application and infrastructure services using dependency injection.

Create the following file:

KrytenAssist.Infrastructure/
- DependencyInjection.cs

Update the following file:

KrytenAssist.Api/
- Program.cs

Requirements
------------
- Keep the task small and focused.
- Do not create controllers or API endpoints yet.
- Add an extension method for registering Infrastructure services.
- Register IPromptCardRepository using InMemoryPromptCardRepository.
- Register CreatePromptCard so it can be resolved by the API layer.
- Use appropriate service lifetimes.
- The in-memory repository should preserve data during the lifetime of the running API.
- Follow Clean Architecture principles.
- The API project may reference Infrastructure.
- The Application project must not depend on Infrastructure.
- Use modern C# conventions.
- Explain any design decisions that are not obvious.

Success Criteria
----------------
- DependencyInjection.cs is created in KrytenAssist.Infrastructure.
- Program.cs calls the dependency registration method.
- IPromptCardRepository is registered with InMemoryPromptCardRepository.
- CreatePromptCard is registered for dependency injection.
- The solution builds successfully.
- No controllers or endpoints are created in this prompt.
- The AI explains why the chosen service lifetimes are appropriate.

Before generating any code:

1. Review the task.
2. Identify any assumptions you are making.
3. Explain those assumptions.
4. Recommend the best implementation approach.
5. Explain why that approach has been chosen.
6. Generate the code.
```

---

## Result

✅ Successful

Created Infrastructure.DependencyInjection.
Registered IPromptCardRepository using InMemoryPromptCardRepository.
Registered CreatePromptCard for dependency injection.
Updated Program.cs to register infrastructure services.
Solution builds successfully.

---

## Notes

- Infrastructure owns registration of infrastructure services via an extension method.
- The in-memory repository is registered as a Singleton so data persists for the lifetime of the running API.
- Application use cases are registered as Scoped to align with the standard ASP.NET Core request lifetime.
- Keeping dependency registration in extension methods keeps Program.cs clean and scalable as the application grows.