# Prompt 008 – Add Update PromptCard Use Case and PUT Endpoint

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

Contains:

- Program.cs
- Endpoints/PromptCardEndpoints.cs

---

## Namespaces

CreatePromptCard and related request/response types:

```csharp
namespace KrytenAssist.Application.PromptCards;
```

IPromptCardRepository:

```csharp
namespace KrytenAssist.Application.Abstractions.Persistence;
```

PromptCardEndpoints:

```csharp
namespace KrytenAssist.Api.Endpoints;
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
- Created CreatePromptCard use case.
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
- Successfully tested in Swagger.

### Prompt 006
- Added GET /api/promptcards.
- Successfully tested in Swagger.

### Prompt 007
- Created PromptCardEndpoints.cs.
- Moved PromptCard endpoints out of Program.cs.
- Added GET /api/promptcards/{id}.
- Added route group and lightweight Swagger metadata.
- Successfully tested in Swagger.

---

## Current API Endpoints

```http
POST /api/promptcards
GET  /api/promptcards
GET  /api/promptcards/{id}
```

---

## Task

Add an application use case and API endpoint for updating an existing PromptCard.

Because PromptCard uses init-only properties, the UpdatePromptCard use case should create a new PromptCard instance with the same Id and CreatedAt values, updated editable fields, and a new UpdatedAt value.

``` csharp
var updatedPromptCard = new PromptCard
{
    Id = existingPromptCard.Id,
    Title = request.Title,
    Category = request.Category,
    Description = request.Description,
    PromptText = request.PromptText,
    Tags = request.Tags,
    CreatedAt = existingPromptCard.CreatedAt,
    UpdatedAt = DateTimeOffset.UtcNow
};
```
Create:

```text
KrytenAssist.Application/
    PromptCards/
        UpdatePromptCard.cs
        UpdatePromptCardRequest.cs
        UpdatePromptCardResponse.cs
```

Update:

```text
KrytenAssist.Infrastructure/
    DependencyInjection.cs

KrytenAssist.Api/
    Endpoints/
        PromptCardEndpoints.cs
```

---

## Requirements

- Keep the task focused.
- Add an `UpdatePromptCard` application use case.
- Add an `UpdatePromptCardRequest`.
- Add an `UpdatePromptCardResponse`.
- Add a Minimal API endpoint:

```http
PUT /api/promptcards/{id}
```

- The endpoint should resolve `UpdatePromptCard` from dependency injection.
- The endpoint should pass the route `id` and request body into the use case.
- The use case should call `IPromptCardRepository.GetByIdAsync`.
- If the PromptCard does not exist, return a result that allows the API to return `404 Not Found`.
- If the PromptCard exists, update it using `IPromptCardRepository.UpdateAsync`.
- Return `200 OK` with an update response when successful.
- Continue passing `CancellationToken`.
- Register `UpdatePromptCard` in dependency injection.
- Keep business/application flow inside the Application layer.
- Keep the API thin.
- Follow Clean Architecture principles.
- Use modern C# conventions.
- Explain any non-obvious design decisions.

---

## Suggested Update Fields

The update request should support:

- Title
- Category
- Description
- PromptText
- Tags

Use property names consistent with the existing `CreatePromptCardRequest`.

---

## Do Not Do

- Do not create controllers.
- Do not add SQLite.
- Do not add Entity Framework Core.
- Do not add DELETE endpoints.
- Do not add FluentValidation.
- Do not add advanced validation.
- Do not introduce MediatR.
- Do not modify Infrastructure persistence unless required.
- Do not bypass the Application layer from the API endpoint.

---

## Success Criteria

- UpdatePromptCard.cs is created.
- UpdatePromptCardRequest.cs is created.
- UpdatePromptCardResponse.cs is created.
- UpdatePromptCard is registered in DI.
- PUT /api/promptcards/{id} is added.
- Endpoint uses UpdatePromptCard rather than directly updating the repository.
- PUT returns 200 OK when the PromptCard exists.
- PUT returns 404 Not Found when the PromptCard does not exist.
- POST, GET, and GET by id continue to work.
- Solution builds successfully.
- Endpoint is successfully tested in Swagger.

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
- KrytenAssist.Application/PromptCards/UpdatePromptCard.cs
- KrytenAssist.Application/PromptCards/UpdatePromptCardRequest.cs
- KrytenAssist.Application/PromptCards/UpdatePromptCardResponse.cs

### Files Updated
- KrytenAssist.Infrastructure/DependencyInjection.cs
- KrytenAssist.Api/Endpoints/PromptCardEndpoints.cs

### Build
✅ Successful

### Testing
✅ Verified in Swagger

- POST `/api/promptcards`
- GET `/api/promptcards`
- GET `/api/promptcards/{id}`
- PUT `/api/promptcards/{id}`

Verified that an existing PromptCard can be updated and the changes are returned correctly.

### Git Commit
Prompt 008 - Add Update PromptCard use case and PUT endpoint

### Lessons Learnt
- Application use cases should encapsulate business workflows rather than allowing the API layer to interact directly with repositories.
- Using immutable entities with `init` properties encourages creating a new instance during updates rather than mutating an existing object.
- Returning `null` from the use case for a missing PromptCard allows the API layer to translate that outcome into a `404 Not Found` response while keeping HTTP concerns out of the Application layer.
- Registering each use case independently keeps dependency injection simple and explicit as the application grows.