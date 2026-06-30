# Prompt 003 - Create In-Memory PromptCard Repository

**Date:** 30 June 2026  
**Project:** Kryten Assist  
**Motto:** Making Future Robin's Life Easier  
**Purpose:** Create the first Infrastructure repository implementation using in-memory storage.

---

## Goal

Create an in-memory implementation of `IPromptCardRepository`.

This gives Kryten Assist a temporary persistence mechanism so the Application layer can be tested before adding a real database.

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

Task
----
Create an in-memory repository implementation for PromptCard.

Also update IPromptCardRepository so it supports the basic CRUD operations required by the next API milestone.

Update the following file:

KrytenAssist.Application/Abstractions/Persistence/
- IPromptCardRepository.cs

Create the following file:

KrytenAssist.Infrastructure/Persistence/
- InMemoryPromptCardRepository.cs

Repository Methods
------------------
The repository should support:

- AddAsync
- GetAllAsync
- GetByIdAsync
- UpdateAsync
- DeleteAsync

Requirements
------------
- Follow Clean Architecture principles.
- The Infrastructure layer may depend on Application and Core.
- The Application layer must not depend on Infrastructure.
- Do not add SQLite, Entity Framework, or database-specific code yet.
- Use an in-memory collection.
- Keep the implementation simple but sensible.
- Use asynchronous method signatures to match future database implementations.
- Use CancellationToken parameters.
- Handle missing records sensibly.
- Use modern C# conventions.
- Explain any design decisions that are not obvious.

Success Criteria
----------------
- IPromptCardRepository contains the required CRUD methods.
- InMemoryPromptCardRepository implements IPromptCardRepository.
- The implementation stores PromptCard instances in memory.
- The code compiles successfully.
- No database or persistence framework is introduced.
- The Application project still has no dependency on Infrastructure.
- The AI explains why this is a temporary Infrastructure implementation.

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

Successfully implemented the first Infrastructure repository for Kryten Assist.

Completed:

- Updated `IPromptCardRepository` to support the initial CRUD operations:
    - `AddAsync`
    - `GetAllAsync`
    - `GetByIdAsync`
    - `UpdateAsync`
    - `DeleteAsync`
- Created `InMemoryPromptCardRepository`.
- Implemented an in-memory storage mechanism for `PromptCard` entities.
- Maintained the Clean Architecture dependency flow:
    - Core ← Application ← Infrastructure
- Verified that the solution builds successfully.

This implementation provides a temporary persistence mechanism that will allow the API layer to be developed before introducing a real database such as SQLite.

---

## Notes

### Design Decisions

- Repository interfaces remain in the Application layer under `Abstractions/Persistence`.
- Infrastructure contains the concrete implementation.
- Repository methods are asynchronous even though the current implementation is in-memory, ensuring the API remains unchanged when a real database is introduced.
- Missing records are represented by `null` or `false` rather than exceptions.

### Lessons Learnt

- One AI prompt can describe an entire feature, but implementation should be completed one file at a time.
- Building after each completed vertical slice makes debugging significantly easier.
- Recording architectural decisions immediately after implementation prevents knowledge from being lost.

### Future Improvements

- Replace the in-memory repository with a SQLite implementation.
- Introduce unit tests for repository behaviour.
- Register the repository with Dependency Injection in the API project.
- Begin exposing repository functionality through REST endpoints.