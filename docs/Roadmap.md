# Kryten Assist Roadmap
 
Current Version
---------------
v0.1.0

Current Phase
-------------
Phase 3 – Persistence

Current Prompt
--------------
Prompt 018 React Client

### Phase 1 – API Foundation (Current)

✔ Prompt 001 – Domain
✔ Prompt 002 – Application
✔ Prompt 003 – Infrastructure
✔ Prompt 004 – Dependency Injection
✔ Prompt 005 – POST Endpoint
✔ Prompt 006 – GET Endpoint
### Phase 2 – API Maturity
✔ Prompt 007 – API Organisation & GET by Id
✔ Prompt 008 – Update PromptCard (PUT)
✔ Prompt 009 – Delete PromptCard (DELETE)
✔ Prompt 010 - Engineering Review
✔ Prompt 011 - Validation
- FluentValidation
- ProblemDetails
- HTTP 400 responses

Prompt 012 – Application Dependency Injection Cleanup ✅
- Added AddApplication()
- Moved Application service registrations into Application
- Registered validators using assembly scanning
- Simplified Infrastructure DI
- Program.cs now calls AddApplication() and AddInfrastructure()

Prompt 013 – Logging & Error Handling  ✅ 
- Global exception handling
- Structured logging


Prompt 014 – Integration Tests ✅
- Added API integration test project
- Added reusable WebApplicationFactory
- Organised tests by feature
- Added shared test data helpers
- Added CRUD endpoint integration tests
- Established automated API regression testing

Phase 3 – Persistence

Prompt 015 – SQLite Repository ✅

Prompt 016 – Entity Framework Core ✅
Prompt 017 – Database Migrations ✅

## Phase 4 – User Interfaces
Prompt 018
React Client

Prompt 019
Prompt Browser

Prompt 020
Prompt Editor

Prompt 021
Avalonia Client

Prompt 022
Offline Support

## Phase 5 – AI Features
Prompt 023
Prompt Templates

Prompt 024
Prompt Categories

Prompt 025
Prompt Search

Prompt 026
Embedding Service

Prompt 027
Semantic Search

Prompt 028
OpenAI Integration

Prompt 029
AI Conversations

Prompt 030
Memory

Prompt 031
Plugins / Tools


Milestone Achieved

✔ Complete CRUD API
✔ Clean Architecture established
✔ Swagger verified
✔ AI Playbook established
✔ Session Handovers established
✔ Entity Framework Core persistence
✔ Database migrations established







