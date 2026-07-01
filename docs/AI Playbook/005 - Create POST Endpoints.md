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
- DependencyInjection.cs

The API project currently registers infrastructure services using dependency injection.

Prompt 004 completed:

- Created Infrastructure.DependencyInjection.
- Registered IPromptCardRepository using InMemoryPromptCardRepository.
- Registered CreatePromptCard for dependency injection.
- Updated Program.cs to call AddInfrastructure().
- Solution builds successfully.

Task
----
Create the first API endpoint for PromptCards.

Add a Minimal API endpoint:

POST /api/promptcards

Requirements
------------
- Keep the task small and focused.
- Update only KrytenAssist.Api/Program.cs for this prompt.
- Do not create controllers.
- Do not create endpoint extension classes yet.
- Do not add GET, PUT, DELETE, or search endpoints.
- Do not add SQLite or database persistence.
- Do not add FluentValidation or advanced validation yet.
- The endpoint should accept CreatePromptCardRequest from the request body.
- The endpoint should resolve CreatePromptCard from dependency injection.
- The endpoint should call the CreatePromptCard use case.
- The endpoint should return 201 Created when successful.
- Follow Clean Architecture principles.
- The API layer may depend on Application and Infrastructure.
- The Application layer must not depend on API or Infrastructure.
- Use modern C# Minimal API conventions.
- Explain any design decisions that are not obvious.

Success Criteria
----------------
- POST /api/promptcards endpoint is added.
- Endpoint accepts CreatePromptCardRequest.
- Endpoint calls CreatePromptCard.
- Endpoint returns 201 Created.
- No controllers are created.
- No additional endpoints are created.
- The solution builds successfully.
- The AI explains why Minimal API is appropriate for this first vertical slice.

Before generating any code:

1. Review the task.
2. Identify any assumptions you are making.
3. Explain those assumptions.
4. Recommend the best implementation approach.
5. Explain why that approach has been chosen.
6. Generate the code.

Status
------
✅ Successful

Files Created
-------------
None

Files Updated
-------------
- KrytenAssist.Api/Program.cs

Build
-----
✅ Successful

Git Commit
----------
Prompt 005 - Add POST prompt card endpoint

Lessons Learnt
--------------
- Minimal APIs provide an excellent starting point for vertical slices without introducing unnecessary controller complexity.
- ASP.NET Core automatically resolves services and binds request models in Minimal API handlers.
- Passing the request CancellationToken into the use case follows ASP.NET Core best practices and prepares the application for long-running operations.
- Returning 201 Created clearly communicates successful resource creation and aligns with REST conventions.