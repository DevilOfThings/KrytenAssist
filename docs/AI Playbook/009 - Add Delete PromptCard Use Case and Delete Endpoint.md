# Prompt 009 – Add Delete PromptCard Use Case and DELETE Endpoint

You are an experienced Senior .NET software engineer helping build a long-term project called Kryten Assist.

## Project Vision

Kryten Assist is a personal digital assistant whose purpose is:

> "Making Future Robin's Life Easier."

The project follows Clean Architecture principles and is being built as both a learning exercise and a portfolio project.

---

## Task

Add an application use case and API endpoint for deleting an existing PromptCard.

Create:

```text
KrytenAssist.Application/
    PromptCards/
        DeletePromptCard.cs
        DeletePromptCardResponse.cs
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

- Keep the task small and focused.
- Add a `DeletePromptCard` application use case.
- Add a `DeletePromptCardResponse`.
- Do not add `DeletePromptCardRequest`; the route `id` is enough.
- Add a Minimal API endpoint:

```http
DELETE /api/promptcards/{id}
```

- The endpoint should resolve `DeletePromptCard` from dependency injection.
- The use case should call `IPromptCardRepository.DeleteAsync`.
- Return `204 No Content` when the PromptCard is deleted.
- Return `404 Not Found` when the PromptCard does not exist.
- Continue passing `CancellationToken`.
- Register `DeletePromptCard` in dependency injection.
- Keep the API thin.
- Keep business/application flow inside the Application layer.
- Follow Clean Architecture principles.
- Use modern C# conventions.

---

## Do Not Do

- Do not create controllers.
- Do not add SQLite.
- Do not add Entity Framework Core.
- Do not add validation.
- Do not add FluentValidation.
- Do not introduce MediatR.
- Do not modify Core unless absolutely necessary.
- Do not bypass the Application layer from the API endpoint.

---

## Success Criteria

- DeletePromptCard.cs is created.
- DeletePromptCardResponse.cs is created.
- DeletePromptCard is registered in DI.
- DELETE `/api/promptcards/{id}` is added.
- Endpoint uses DeletePromptCard rather than directly calling the repository.
- DELETE returns `204 No Content` when deleted.
- DELETE returns `404 Not Found` when missing.
- Existing POST, GET, GET by id, and PUT endpoints continue to work.
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
- KrytenAssist.Application/PromptCards/DeletePromptCard.cs
- KrytenAssist.Application/PromptCards/DeletePromptCardResponse.cs

### Files Updated
- KrytenAssist.Infrastructure/DependencyInjection.cs
- KrytenAssist.Api/Endpoints/PromptCardEndpoints.cs

### Build
✅ Successful

### Testing
✅ Verified in Swagger

Successfully tested:

- POST `/api/promptcards`
- GET `/api/promptcards`
- GET `/api/promptcards/{id}`
- PUT `/api/promptcards/{id}`
- DELETE `/api/promptcards/{id}`

Verified that:

- A PromptCard can be created.
- All PromptCards can be retrieved.
- A PromptCard can be retrieved by id.
- A PromptCard can be updated.
- A PromptCard can be deleted.
- A deleted PromptCard returns `404 Not Found` when requested by id.

### Git Commit
Prompt 009 - Add Delete PromptCard use case and DELETE endpoint

### Lessons Learnt
- A dedicated use case should encapsulate delete operations, keeping the API layer independent of repository implementation details.
- Not every use case requires a Request object. When all required information is supplied by the route (such as an identifier), passing the value directly keeps the API and Application layers simple.
- Returning a lightweight response object from the use case allows the API layer to translate application outcomes into appropriate HTTP responses without introducing HTTP concepts into the Application layer.
- Completing CRUD operations before introducing persistence validates the architecture and repository abstraction, making the transition to a database implementation significantly easier.
