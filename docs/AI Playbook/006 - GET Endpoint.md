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

Prompt 005 completed:

- Added POST /api/promptcards.
- Endpoint accepts CreatePromptCardRequest.
- Endpoint resolves CreatePromptCard from dependency injection.
- Endpoint calls ExecuteAsync.
- Endpoint returns 201 Created.
- Endpoint works in Swagger.
- Solution builds successfully.

Task
----
Create the second API endpoint for PromptCards.

Add a Minimal API endpoint:

GET /api/promptcards

Requirements
------------
- Keep the task small and focused.
- Update only KrytenAssist.Api/Program.cs for this prompt.
- Do not create controllers.
- Do not create endpoint extension classes yet.
- Do not add GET /api/promptcards/{id}.
- Do not add PUT, DELETE, or search endpoints.
- Do not add SQLite or database persistence.
- Do not add FluentValidation or advanced validation yet.
- The endpoint should resolve IPromptCardRepository from dependency injection.
- The endpoint should call GetAllAsync.
- The endpoint should return 200 OK when successful.
- The endpoint should return the prompt cards currently stored in the in-memory repository.
- Follow Clean Architecture principles.
- The API layer may depend on Application and Infrastructure.
- The Application layer must not depend on API or Infrastructure.
- Use modern C# Minimal API conventions.
- Explain any design decisions that are not obvious.

Success Criteria
----------------
- GET /api/promptcards endpoint is added.
- Endpoint resolves IPromptCardRepository.
- Endpoint calls GetAllAsync.
- Endpoint returns 200 OK.
- No controllers are created.
- No endpoint extension classes are created.
- No additional endpoints are created.
- The solution builds successfully.
- The endpoint works in Swagger.
- The AI explains why this prompt does not yet refactor endpoints out of Program.cs.

Before generating any code:

1. Review the task.
2. Identify any assumptions you are making.
3. Explain those assumptions.
4. Recommend the best implementation approach.
5. Explain why that approach has been chosen.
6. Generate the code.

## Result

### Status
✅ Successful

### Files Created
None

### Files Updated
- KrytenAssist.Api/Program.cs

### Build
✅ Successful

### Testing
✅ Verified in Swagger

- POST `/api/promptcards` successfully creates a PromptCard.
- GET `/api/promptcards` successfully returns all PromptCards currently stored in the in-memory repository.
- Data persists across requests while the API is running due to the singleton repository registration.

### Git Commit
Prompt 006 - Add GET prompt cards endpoint

### Lessons Learnt
- Minimal APIs allow new endpoints to be added quickly while maintaining a clean vertical slice through the application.
- ASP.NET Core automatically resolves dependencies and binds parameters in Minimal API handlers, reducing boilerplate.
- Injecting `IPromptCardRepository` into the GET endpoint keeps the API dependent on the application abstraction rather than the infrastructure implementation, preserving Clean Architecture boundaries.
- Passing the `CancellationToken` through to the repository follows ASP.NET Core best practices and prepares the application for future database providers.
- Keeping the POST and GET endpoints together in `Program.cs` is appropriate while the API surface is small. Refactoring endpoint mappings into dedicated endpoint classes will provide a cleaner and more scalable structure once additional endpoints are introduced.