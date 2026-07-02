# Prompt 010 – Engineering Review

## Goal

Perform a senior engineering review of Kryten Assist v0.1.0.

The goal is not to add a new feature. The goal is to review the current foundation before introducing validation, persistence, and client applications.

Focus on architecture, consistency, API design, validation readiness, and persistence readiness.

Do not make functional changes unless they are clearly beneficial.

Important: Do not generate implementation code. Produce the review first and wait for approval before suggesting file changes.


---

## Current Project Context

Kryten Assist is a personal digital assistant project built as both a learning exercise and portfolio project.

Project vision:

“Making Future Robin’s Life Easier.”

The solution follows Clean Architecture principles and currently contains:

```text
KrytenAssist.Core
KrytenAssist.Application
KrytenAssist.Infrastructure
KrytenAssist.Api
```

The API currently supports full CRUD for PromptCards.

Current endpoints:

```http
POST   /api/promptcards
GET    /api/promptcards
GET    /api/promptcards/{id}
PUT    /api/promptcards/{id}
DELETE /api/promptcards/{id}
```

The project builds successfully and all endpoints have been tested through Swagger.

---

## Review Areas

### 1. Architecture Review

Review whether responsibilities are correctly separated across:

- Core
- Application
- Infrastructure
- Api

Check whether any layer depends on something it should not.

Confirm whether the Clean Architecture structure is still appropriate for the current stage of the project.

---

### 2. Consistency Review

Review consistency across:

- Use case names
- Request and response names
- Repository method names
- Endpoint names
- Folder structure
- Namespaces
- Async method patterns
- Return types

Identify anything that feels inconsistent, unclear, or accidental.

---

### 3. API Review

Review the current API shape:

```http
POST   /api/promptcards
GET    /api/promptcards
GET    /api/promptcards/{id}
PUT    /api/promptcards/{id}
DELETE /api/promptcards/{id}
```

Check:

- Route naming
- HTTP status codes
- Swagger/OpenAPI metadata
- Minimal API organisation
- Route group structure
- Typed results
- Error responses

Confirm whether the API is predictable and suitable for a portfolio project.

---

### 4. Validation Readiness

Prompt 011 will introduce validation.

Review whether the current request models and endpoint structure are ready for validation.

Consider:

- Where validation should live
- Whether FluentValidation is appropriate
- Whether validation should happen in the API layer or Application layer
- How bad request responses should be returned
- Whether request models are shaped correctly

Do not implement validation in this prompt.

---

### 5. Persistence Readiness

SQLite persistence will be introduced in a future prompt.

Review whether the repository abstraction is ready for a real database implementation.

Consider:

- Whether repository method names are clear
- Whether async methods are correctly shaped
- Whether CancellationToken support should be introduced
- Whether update/delete semantics are clear
- Whether the current in-memory implementation hides any future problems

Do not implement SQLite in this prompt.

---

### 6. Improvement Classification

Classify all findings into one of these categories:

```text
Must fix now
Should fix soon
Can defer
No change needed
```

Avoid unnecessary refactoring.

Only recommend changes that improve maintainability, clarity, or readiness for the next phase.

---

## Expected Output

Produce a written engineering review.

The review should include:

1. Overall assessment
2. Architecture findings
3. Consistency findings
4. API findings
5. Validation readiness findings
6. Persistence readiness findings
7. Recommended changes
8. Deferred technical debt
9. Suggested next prompt

Do not write code unless a change is explicitly agreed after the review.

---

## Success Criteria

The prompt is complete when:

- The current architecture has been reviewed.
- Any inconsistencies have been identified.
- The API shape has been assessed.
- Validation readiness has been assessed.
- Persistence readiness has been assessed.
- Findings have been classified by priority.
- No unnecessary functional changes have been introduced.
- The review produces a clear path into Prompt 011.