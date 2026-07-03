# Prompt 013 – Logging & Error Handling

## Goal

Introduce global exception handling and structured logging to the Kryten Assist API.

The API should return consistent error responses for unexpected failures without changing existing business behaviour.

## Context

Kryten Assist currently has:

- Clean Architecture
- Minimal APIs
- CRUD endpoints for PromptCards
- FluentValidation for request validation
- Dependency Injection split by Application and Infrastructure layers
- Swagger/OpenAPI
- In-memory repository

Validation errors already return HTTP 400 responses using `Results.ValidationProblem(...)`.

This prompt focuses only on unexpected/unhandled exceptions.

## Requirements

1. Add global exception handling to the API.
2. Return a consistent `ProblemDetails` response for unhandled exceptions.
3. Log unhandled exceptions using structured logging.
4. Keep the Application and Core layers free from HTTP concerns.
5. Do not change existing endpoint routes.
6. Do not change existing business logic.
7. Do not add temporary test endpoints.
8. Keep `Program.cs` clean by using an extension method where appropriate.

## Suggested Implementation

Create a new API extension file:

```text
KrytenAssist.Api/Extensions/ExceptionHandlingExtensions.cs
```

Add an extension method such as:

```csharp
UseGlobalExceptionHandling()
```

The handler should:

- catch unhandled exceptions
- log the exception
- return HTTP 500
- return a safe `ProblemDetails` response

Example response intent:

```json
{
  "title": "An unexpected error occurred.",
  "status": 500,
  "detail": "Please try again later.",
  "instance": "/api/promptcards"
}
```

## Expected Files Updated

```text
KrytenAssist.Api/Program.cs
docs/Roadmap.md
```

## Expected Files Created

```text
KrytenAssist.Api/Extensions/ExceptionHandlingExtensions.cs
```

## Build Requirement

Run:

```bash
dotnet build
```

The solution must build successfully.

## Roadmap Update

Update Prompt 013 to show it as complete once implemented:

```text
Prompt 013 – Logging & Error Handling ✅
- Added global exception handling
- Added consistent ProblemDetails responses for unhandled exceptions
- Added structured exception logging
```

## Git Commit

After implementation and successful build, commit using:

```bash
git add .
git commit -m "Add global exception handling and logging"
```

## Constraints

- Do not modify Core.
- Do not modify Application use cases.
- Do not modify Infrastructure unless required.
- Do not introduce persistence.
- Do not introduce integration tests yet.
- Do not create a session handover until the user requests it.