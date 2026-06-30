# Prompt 002 - Create First Application Use Case

**Date:** 30 June 2026  
**Project:** Kryten Assist  
**Motto:** Making Future Robin's Life Easier  
**Purpose:** Create the first application use case following the project's Clean Architecture principles.

---

## Goal

Create the first application use case responsible for creating a new `PromptCard`.

The objective is to establish the pattern that all future use cases will follow.

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

The Core project already contains the PromptCard domain entity.

Task
----
Design and implement the first application use case for creating a PromptCard.

The implementation should establish the architectural pattern that future use cases will follow.

Create the following files:

PromptCards/
- CreatePromptCard.cs
- CreatePromptCardRequest.cs
- CreatePromptCardResponse.cs

Abstractions/Persistence/
- IPromptCardRepository.cs

Requirements
------------
- Use the existing KrytenAssist.Core.Entities.PromptCard entity.
- Follow Clean Architecture principles.
- The Application layer must not depend on Infrastructure, API or UI projects.
- The use case should depend only on abstractions.
- The repository should be defined as an interface.
- Do not implement the repository.
- The request should contain all information required to create a PromptCard.
- The response should return the identifier of the newly created PromptCard.
- Use modern C# conventions.
- Explain any design decisions that are not obvious.

Success Criteria
----------------
- The use case creates a PromptCard with Id, CreatedAt and UpdatedAt populated.
- Four files are created.
- Each file has a single responsibility.
- The Application project has no dependency on Infrastructure.
- The repository is an abstraction only.
- The code compiles successfully.
- The AI explains why this structure supports Clean Architecture.

Before generating any code:

1. Review the task.
2. Identify any assumptions you are making.
3. Explain those assumptions.
4. Recommend the best architectural approach.
5. Explain why that approach has been chosen.
6. Generate the code.
```

---

## Result

_To be completed after the code has been generated._

---

## Notes

_To be completed with lessons learnt, design decisions, and any improvements that should be carried forward into future prompts._