# Prompt 007 – API Organisation and GET by Id

You are an experienced Senior .NET software engineer helping build a long-term project called Kryten Assist.

## Project Vision

Kryten Assist is a personal digital assistant whose purpose is:

> "Making Future Robin's Life Easier."

The project follows Clean Architecture principles and is being built as both a learning exercise and a portfolio project.

---

## Current Solution

The solution currently contains:

- KrytenAssist.Core
- KrytenAssist.Application
- KrytenAssist.Infrastructure
- KrytenAssist.Api

---

## Current Architecture

### Core

Contains:

- PromptCard domain entity

### Application

Contains:

- CreatePromptCard
- CreatePromptCardRequest
- CreatePromptCardResponse
- IPromptCardRepository

### Infrastructure

Contains:

- InMemoryPromptCardRepository
- DependencyInjection.cs

### API

Currently contains:

- Minimal API endpoints in Program.cs
- Infrastructure dependency registration

---

## Namespaces

CreatePromptCard

```csharp
namespace KrytenAssist.Application.PromptCards;
```

CreatePromptCardRequest

```csharp
namespace KrytenAssist.Application.PromptCards;
```

CreatePromptCardResponse

```csharp
namespace KrytenAssist.Application.PromptCards;
```

IPromptCardRepository

```csharp
namespace KrytenAssist.Application.Abstractions.Persistence;
```

InMemoryPromptCardRepository

```csharp
namespace KrytenAssist.Infrastructure.Persistence;
```

---

## Existing Use Case Signature

```csharp
public async Task<CreatePromptCardResponse> ExecuteAsync(
    CreatePromptCardRequest request,
    CancellationToken cancellationToken = default)
```

---

## Existing Repository Interface

```csharp
Task AddAsync(PromptCard promptCard, CancellationToken cancellationToken = default);

Task<IReadOnlyCollection<PromptCard>> GetAllAsync(
    CancellationToken cancellationToken = default);

Task<PromptCard?> GetByIdAsync(
    Guid id,
    CancellationToken cancellationToken = default);

Task<bool> UpdateAsync(
    PromptCard promptCard,
    CancellationToken cancellationToken = default);

Task<bool> DeleteAsync(
    Guid id,
    CancellationToken cancellationToken = default);
```

---

## Completed Prompts

### Prompt 001

- Created PromptCard domain entity.

### Prompt 002

- Created first application use case.
- Added CreatePromptCard.
- Added CreatePromptCardRequest.
- Added CreatePromptCardResponse.
- Added IPromptCardRepository.

### Prompt 003

- Implemented InMemoryPromptCardRepository.
- Expanded repository interface with CRUD methods.

### Prompt 004

- Added Infrastructure.DependencyInjection.
- Registered IPromptCardRepository.
- Registered CreatePromptCard.
- Updated Program.cs.
- Solution builds successfully.

### Prompt 005

- Added POST /api/promptcards.
- Endpoint accepts CreatePromptCardRequest.
- Calls ExecuteAsync().
- Returns 201 Created.
- Successfully tested in Swagger.

### Prompt 006

- Added GET /api/promptcards.
- Resolves IPromptCardRepository.
- Calls GetAllAsync().
- Returns 200 OK.
- Successfully tested in Swagger.

---

## Existing API Endpoints

### POST /api/promptcards

- Accepts CreatePromptCardRequest.
- Resolves CreatePromptCard from dependency injection.
- Calls ExecuteAsync().
- Returns 201 Created.

### GET /api/promptcards

- Resolves IPromptCardRepository.
- Calls GetAllAsync().
- Returns 200 OK.

---

## Task

Organise the PromptCard API endpoints and add a GET by id endpoint.

Create:

```text
KrytenAssist.Api/
    Endpoints/
        PromptCardEndpoints.cs
```

Update:

```text
KrytenAssist.Api/
    Program.cs
```

---

## Requirements

- Keep the task small and focused.
- Move the existing POST endpoint from Program.cs into PromptCardEndpoints.cs.
- Move the existing GET endpoint from Program.cs into PromptCardEndpoints.cs.
- Add GET /api/promptcards/{id}.
- Use a Minimal API route group for `/api/promptcards`.
- Create an extension method:

```csharp
app.MapPromptCardEndpoints();
```

- Program.cs should only call the extension method.
- Continue using dependency injection through Minimal API handler parameters.
- Continue passing CancellationToken into all repository and use case calls.
- Use lightweight Swagger/OpenAPI metadata including:
    - Tags
    - Endpoint names
    - Summaries
- GET by id returns:
    - 200 OK when found.
    - 404 Not Found when the PromptCard does not exist.
- Follow Clean Architecture principles.
- Use modern C# conventions.
- Explain any non-obvious design decisions.

---

## Do Not Do

- Do not create controllers.
- Do not add SQLite.
- Do not add Entity Framework Core.
- Do not add PUT endpoints.
- Do not add DELETE endpoints.
- Do not add validation.
- Do not add FluentValidation.
- Do not introduce MediatR.
- Do not modify Core, Application or Infrastructure unless absolutely necessary.

---

## Success Criteria

- PromptCardEndpoints.cs is created.
- Existing POST endpoint is moved.
- Existing GET endpoint is moved.
- GET /api/promptcards/{id} is implemented.
- Program.cs calls app.MapPromptCardEndpoints().
- Program.cs no longer contains PromptCard endpoint mappings.
- PromptCard endpoints are grouped under `/api/promptcards`.
- Swagger displays the endpoints clearly.
- GET by id returns:
    - 200 OK when found.
    - 404 Not Found when missing.
- Solution builds successfully.
- Existing POST and GET endpoints continue to function after the refactor.

---

## Before generating any code

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
- KrytenAssist.Api/Endpoints/PromptCardEndpoints.cs

### Files Updated
- KrytenAssist.Api/Program.cs

### Build
✅ Successful

### Testing
✅ Verified in Swagger

- POST `/api/promptcards`
- GET `/api/promptcards`
- GET `/api/promptcards/{id}`

All endpoints function correctly following the refactor.

### Git Commit
Prompt 007 - Organise PromptCard endpoints

### Lessons Learnt
- Minimal API endpoint extension methods keep `Program.cs` focused on application composition rather than endpoint implementation.
- Route groups provide a natural way to organise related endpoints while sharing a common route prefix and Swagger metadata.
- Refactoring after establishing working functionality reduces risk and keeps changes easy to verify.
- Endpoint metadata such as names, summaries, and tags improves Swagger documentation with minimal effort.
- Keeping endpoint organisation separate from feature development results in a cleaner and more maintainable API structure as additional endpoints are introduced.
