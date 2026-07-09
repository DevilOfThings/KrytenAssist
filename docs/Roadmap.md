# Kryten Assist Roadmap
 
Current Version
---------------
v0.1.0

Current Phase
-------------
Phase 4 – User Interfaces

Current Prompt
--------------
Prompt 026 – Avalonia Embedding Service ✅
- Added EmbeddingVector model
- Added IEmbeddingService abstraction
- Added deterministic offline embedding implementation
- Registered embedding service with dependency injection
- Introduced stable deterministic hashing for embedding generation
- Verified `dotnet build` and `dotnet test`

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
Prompt 018 – React Client ✅
- Created the first React client
- Connected the React client to the PromptCards API
- Displayed PromptCards returned from the backend
- Verified empty-state handling when no PromptCards exist
- Confirmed build and API tests still pass

Prompt 019 – Prompt Browser ✅
- Improved Prompt Browser layout
- Added loading, error and empty states
- Displayed prompt count
- Enhanced Prompt Card display
- Added client-side search
- Added client-side category filtering
- Added no-results messaging
- Verified npm build, dotnet build and tests

Prompt 020 – Prompt Editor ✅
- Added PromptCard editor form
- Integrated React form with the PromptCards API
- Cleared the form after successful create
- Added success and error feedback
- Refreshed the Prompt Browser automatically after create
- Improved Prompt Editor styling
- Verified npm build, dotnet build and tests

Prompt 021 – Avalonia Client ✅
- Created the first Avalonia desktop client project
- Added the Avalonia project to the solution
- Displayed a basic Kryten Assist shell window
- Verified dotnet build and API tests still pass

Prompt 022 – Avalonia Offline Prompt Store ✅
- Added an offline PromptCard model
- Introduced IPromptCardStore and JsonPromptCardStore
- Implemented MVVM with MainWindowViewModel
- Registered desktop services with dependency injection
- Bound the Avalonia UI to offline prompt cards
- Verified build, application startup and offline storage

## Phase 5 – Avalonia AI Features
Prompt 023 – Avalonia Prompt Template Editor ✅
- Added offline prompt template editor
- Added MVVM command-based save
- Persisted templates to JSON storage
- Refreshed prompt list automatically
- Verified build and tests

Prompt 024 – Prompt Categories ✅
- Added automatic category discovery
- Added reusable category suggestion chips
- Supported both category selection and manual entry
- Refreshed categories after saving prompt templates
- Verified build and tests

Prompt 025 – Avalonia Offline Prompt Search ✅
- Added live offline prompt search
- Added filtered prompt collection
- Implemented case-insensitive search across title, category, description, prompt text and tags
- Added no-results messaging
- Verified build and tests

Prompt 026 – Avalonia Embedding Service ✅
- Added EmbeddingVector model
- Added IEmbeddingService abstraction
- Added deterministic offline embedding implementation
- Registered embedding service with dependency injection
- Introduced stable deterministic hashing for embedding generation
- Verified build and tests

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

## Phase 6 – Career Assistant

Prompt 032
Job Opportunity Domain

Prompt 033
Job Opportunity API

Prompt 034
Interview Prep Notes

Prompt 035
Interview Question Bank

Prompt 036
Mock Interview Sessions

Prompt 037
React Interview Prep UI


Milestone Achieved

✔ Complete CRUD API
✔ Clean Architecture established
✔ Swagger verified
✔ AI Playbook established
✔ Session Handovers established
✔ Entity Framework Core persistence
✔ Database migrations established
✔ Interview Prep / Career Assistant roadmap added
✔ React client established
✔ Prompt Browser completed
✔ Prompt Editor completed
✔ Avalonia desktop client foundation established
✔ Avalonia offline architecture established
✔ MVVM introduced into the desktop client
✔ Offline JSON prompt store implemented
✔ Offline prompt template editor completed
✔ Prompt category discovery and reuse implemented
✔ Offline prompt search implemented
✔ Offline embedding service foundation established
