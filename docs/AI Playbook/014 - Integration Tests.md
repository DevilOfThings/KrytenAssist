# Prompt 014 – Integration Tests

## Goal

Introduce API integration tests for Kryten Assist.

The tests should verify the PromptCard API endpoints through the complete HTTP pipeline using `WebApplicationFactory`.

---

## Context

Kryten Assist currently has:

- Clean Architecture
- Minimal APIs
- CRUD endpoints for PromptCards
- FluentValidation request validation
- Application-layer dependency injection
- Infrastructure-layer dependency injection
- In-memory repository
- Global exception handling
- Structured exception logging
- Swagger/OpenAPI

Manual testing has been performed using Swagger. This prompt introduces automated integration tests to provide regression protection and increase confidence when refactoring.

---

## Requirements

1. Create a new test project:

```text
KrytenAssist.Api.Tests
```

2. Use xUnit.

3. Add the required NuGet packages:

```text
Microsoft.NET.Test.Sdk
xunit
xunit.runner.visualstudio
Microsoft.AspNetCore.Mvc.Testing
```

4. Add the test project to the solution.

5. Add a project reference to:

```text
KrytenAssist.Api
```

6. Update `Program.cs` by adding:

```csharp
public partial class Program;
```

at the bottom of the file so the application entry point can be discovered by `WebApplicationFactory`.

7. Create a reusable custom test host by deriving from:

```csharp
WebApplicationFactory<Program>
```

This should live in the test project under an `Infrastructure` folder.

8. Add integration tests for the PromptCard endpoints.

9. Organise tests by API feature rather than placing every endpoint test in one large test class.

10. Add reusable test request helpers under a dedicated `TestData` folder.

---

## Test Design Principles

- Tests must interact with the API **only through `HttpClient`**.
- Do not call repositories or use cases directly.
- Every test must be independent.
- Tests must not depend on execution order.
- Each test should create its own data and verify only its own behaviour.
- Group tests by API resource or feature.
- Prefer small focused test classes over one large endpoint test class.
- Extract duplicated test data creation into reusable helpers when duplication becomes clear.

---

## Project Structure

```text
KrytenAssist.Api.Tests
│
├── Infrastructure
│   └── CustomWebApplicationFactory.cs
│
├── TestData
│   └── PromptCardRequests.cs
│
└── PromptCards
    ├── CreatePromptCardTests.cs
    ├── GetPromptCardTests.cs
    ├── UpdatePromptCardTests.cs
    └── DeletePromptCardTests.cs
```

---

## Test Data Helpers

Shared test request creation should live in:

```text
KrytenAssist.Api.Tests/TestData/PromptCardRequests.cs
```

Example helper:

```csharp
public static class PromptCardRequests
{
    public static CreatePromptCardRequest CreateValidRequest() => new(
        Title: "Test Prompt Card",
        Category: "Testing",
        Description: "A prompt card created during an integration test.",
        PromptText: "Write a test for this endpoint.",
        Tags: ["tests", "api"]);
}
```

Tests can then use:

```csharp
var request = PromptCardRequests.CreateValidRequest();
```

For invalid request scenarios, use the valid request as a base and override the specific field under test:

```csharp
var request = PromptCardRequests.CreateValidRequest() with
{
    Title = string.Empty
};
```

---

## Required Test Coverage

### Create PromptCard

```http
POST /api/promptcards
```

Verify:

- valid request returns `201 Created`
- response includes a `Location` header
- response body includes a non-empty created ID
- invalid request returns `400 Bad Request`

Suggested tests:

```text
PostPromptCard_WithValidRequest_ReturnsCreated
PostPromptCard_WithInvalidRequest_ReturnsBadRequest
```

---

### Get PromptCards

```http
GET /api/promptcards
GET /api/promptcards/{id}
```

Verify:

- get all returns `200 OK`
- existing prompt card returns `200 OK`
- returned prompt card contains the expected ID and key values
- unknown ID returns `404 Not Found`

Suggested tests:

```text
GetPromptCards_ReturnsOk
GetPromptCardById_WithExistingId_ReturnsOk
GetPromptCardById_WithUnknownId_ReturnsNotFound
```

---

### Update PromptCard

```http
PUT /api/promptcards/{id}
```

Verify:

- existing prompt card returns `200 OK`
- updated values are persisted and can be retrieved
- unknown ID returns `404 Not Found`

Suggested tests:

```text
UpdatePromptCard_WithExistingId_ReturnsOk
UpdatePromptCard_WithUnknownId_ReturnsNotFound
```

---

### Delete PromptCard

```http
DELETE /api/promptcards/{id}
```

Verify:

- existing prompt card returns `204 No Content`
- unknown ID returns `404 Not Found`

Suggested tests:

```text
DeletePromptCard_WithExistingId_ReturnsNoContent
DeletePromptCard_WithUnknownId_ReturnsNotFound
```

---

## Expected Files Created

```text
KrytenAssist.Api.Tests/
    Infrastructure/
        CustomWebApplicationFactory.cs
    TestData/
        PromptCardRequests.cs
    PromptCards/
        CreatePromptCardTests.cs
        GetPromptCardTests.cs
        UpdatePromptCardTests.cs
        DeletePromptCardTests.cs
```

---

## Expected Files Updated

```text
KrytenAssist.sln
KrytenAssist.Api/Program.cs
docs/Roadmap.md
```

---

## Build and Test Requirement

Run:

```bash
dotnet build
dotnet test
```

Both commands must complete successfully.

Expected result:

```text
9 integration tests passing
```

---

## Constraints

- Do not add database persistence.
- Do not introduce Entity Framework.
- Do not add Testcontainers.
- Do not add mocking frameworks.
- Do not introduce authentication.
- Do not create Application or Core unit test projects yet.
- Do not change existing endpoint behaviour.
- Do not create a session handover until requested.

---

## Roadmap Update

After implementation:

```text
Prompt 014 – Integration Tests ✅

- Added API integration test project
- Added reusable WebApplicationFactory
- Organised tests by feature
- Added shared test data helpers
- Added CRUD endpoint integration tests
- Established automated API regression testing
```

---

## Git Commit

After a successful build and test run:

```bash
git add .
git commit -m "Add PromptCard API integration tests"
```