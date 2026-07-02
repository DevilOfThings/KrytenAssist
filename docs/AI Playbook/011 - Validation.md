# Prompt 011 – Validation

## Goal

Introduce consistent request validation for the PromptCard API using FluentValidation.

The objective is to ensure invalid requests are rejected before reaching the application use cases, returning clear and consistent HTTP 400 responses.

Validation should improve API quality without changing existing business functionality.

Do not introduce persistence, logging, or exception handling changes as part of this prompt.

---

# Current Project Context

Kryten Assist is a personal digital assistant project built using Clean Architecture.

Project vision:

> **Making Future Robin's Life Easier.**

The solution currently contains:

```text
KrytenAssist.Core
KrytenAssist.Application
KrytenAssist.Infrastructure
KrytenAssist.Api
```

Current API:

```http
POST   /api/promptcards
GET    /api/promptcards
GET    /api/promptcards/{id}
PUT    /api/promptcards/{id}
DELETE /api/promptcards/{id}
```

Prompt 010 completed a full engineering review and confirmed the architecture is ready for validation.

---

# Objectives

Implement consistent request validation for:

- CreatePromptCardRequest
- UpdatePromptCardRequest

Validation should be performed before the corresponding use case is executed.

---

# Validation Rules

## Title

- Required
- Must not be empty or whitespace
- Maximum length: 100 characters

## Category

- Required
- Must not be empty or whitespace
- Maximum length: 50 characters

## Description

- Optional
- Maximum length: 500 characters

## PromptText

- Required
- Must not be empty or whitespace
- Maximum length: 4000 characters

## Tags

- Optional
- Empty collections are allowed
- Individual tags must not be empty or whitespace
- Maximum length per tag: 50 characters

---

# Architecture

Validation belongs in the **Application** layer.

Create dedicated validators for each request model.

Suggested files:

```text
PromptCards/
    CreatePromptCardRequestValidator.cs
    UpdatePromptCardRequestValidator.cs
```

Do not place validation logic inside the Domain or Infrastructure projects.

---

# Technology

Use FluentValidation.

Register validators using dependency injection.

---

# Endpoint Behaviour

Endpoints should validate requests before executing the application use case.

If validation fails:

- Return HTTP 400 Bad Request.
- Return validation errors using `Results.ValidationProblem(...)`.

If validation succeeds:

- Continue existing behaviour.

Expected responses:

```text
POST
201 Created

PUT
200 OK

Invalid request
400 Bad Request

Unknown resource
404 Not Found
```

---

# Use Cases

Following this prompt, application use cases should no longer perform manual validation of user input.

Validation should have already occurred before the use case is called.

Business logic remains inside the use case.

---

# Implementation Approach

Complete the work incrementally.

Suggested order:

1. Add FluentValidation package.
2. Create CreatePromptCardRequestValidator.
3. Build.
4. Create UpdatePromptCardRequestValidator.
5. Build.
6. Register validators with dependency injection.
7. Update POST endpoint.
8. Build.
9. Test POST in Swagger.
10. Update PUT endpoint.
11. Build.
12. Test PUT in Swagger.
13. Remove obsolete manual validation from CreatePromptCard.
14. Final build.
15. Final Swagger verification.

Build after each completed step.

---

# Success Criteria

Prompt 011 is complete when:

- FluentValidation has been added.
- Create and Update requests are validated consistently.
- Invalid requests return HTTP 400.
- Valid requests retain existing behaviour.
- Manual request validation has been removed from CreatePromptCard.
- The solution builds successfully.
- POST and PUT endpoints have been verified using Swagger.
- AI Playbook has been updated.
- Changes are committed to Git.

Do not implement logging, exception handling, persistence, or additional features as part of this prompt.