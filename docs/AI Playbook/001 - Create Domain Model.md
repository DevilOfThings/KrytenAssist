# Prompt 001 - Create Domain Model

**Date:** 30 June 2026  
**Project:** Kryten Assist  
**Motto:** Making Future Robin's Life Easier  
**Purpose:** Create the first domain model for Kryten Assist.

---

## Goal

Create the first domain entity called `PromptCard`.

This entity represents a reusable AI thinking or prompt card.

---

## Prompt

```text
You are an experienced Senior .NET software engineer helping build a long-term project called Kryten Assist.

Project Vision
--------------
Kryten Assist is a personal digital assistant whose purpose is:

"Making Future Robin's Life Easier."

The project follows Clean Architecture principles and is being built as a learning exercise as well as a portfolio project.

Current Solution
----------------
The solution currently contains:

- KrytenAssist.Core
- KrytenAssist.Application
- KrytenAssist.Infrastructure
- KrytenAssist.Api

The Core project contains the domain model and should have no dependencies on Infrastructure, API or UI projects.

Task
----
Create the first domain entity called PromptCard.

The entity should represent a reusable AI thinking or prompt card.

It should contain the following properties:

- Id
- Title
- Category
- Description
- PromptText
- Tags
- CreatedAt
- UpdatedAt

Requirements
------------
- Use modern C# conventions.
- Make sensible decisions about property types.
- Assume this project will eventually be stored in a database.
- Keep the class focused on the domain.
- Do not add persistence attributes or Entity Framework code.
- Do not create repositories or services yet.
- Explain any design decisions that aren't obvious.

Success Criteria
----------------
- A single PromptCard entity is created in KrytenAssist.Core.
- The entity compiles successfully.
- The entity has no dependencies on Infrastructure, API or UI projects.
- No persistence framework attributes are used.
- The code follows modern C# and .NET conventions.
- The AI explains the reasoning behind any important design decisions.

Before generating any code:

1. Review the task.
2. Identify any assumptions you are making.
3. Explain those assumptions.
4. If there are multiple valid approaches, recommend one and explain why.
5. Then generate the code.---
