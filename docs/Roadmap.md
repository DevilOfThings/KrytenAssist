## Kryten Assist Roadmap

Current Version
---------------
v0.1.0

Current Phase
-------------
Phase 7 – Cruise Assistant

Current Prompt
--------------
Prompt 038 – Saved Cruises and Preferences (next)

Prompt 037 – Cruise History and Price Tracking is complete. Kryten can explicitly
record captured Cruise observations, preserve meaningful price changes in local
SQLite history and revisit that history after restart without loading TUI.

Prompt 038 is the recommended next task. It has not started.

### Phase 1 – API Foundation

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

Prompt 027 – Avalonia Offline Semantic Search ✅
- Added offline semantic ranking
- Added cosine similarity service
- Preserved keyword search
- Ranked keyword matches by semantic similarity
- Kept the implementation fully offline
- Verified build and tests

Prompt 028 – OpenAI Embedding Provider ✅
- Added configuration-driven embedding provider selection
- Added OpenAI embedding provider implementation
- Added startup configuration validation for API keys
- Added debounced semantic search
- Added cancellation support throughout the search pipeline
- Added in-memory embedding cache
- Added resilient runtime fallback to deterministic embeddings
- Added provider status reporting in the UI
- Preserved offline-first development
- Verified build and tests

Prompt 029 – AI Conversations ✅
- Added provider-independent conversation abstractions
- Added OpenAI conversation provider
- Added configurable system prompt support
- Added conversation UI with send, cancel, busy and error states
- Added conversation history display
- Added cancellation support throughout the conversation pipeline
- Preserved stateless conversations ready for Prompt 030 memory
- Verified build and manual conversation workflow

Prompt 030 – Memory ✅
- Added provider-independent conversation memory abstraction
- Added bounded in-memory conversation memory
- Added configurable conversation context size
- Added provider-independent conversation request model
- Included previous successful conversation turns in subsequent AI requests
- Added Clear Conversation command and UI
- Ensured failed and cancelled requests are not committed to memory
- Verified conversational memory using the live OpenAI API


Prompt 031 – Tools ✅
- Introduced a provider-independent tools architecture
- Added application-owned tool models, contracts and registry
- Registered deterministic built-in tools through dependency injection
- Integrated OpenAI tool calling without leaking provider SDK types
- Executed tool requests through a provider-independent registry
- Preserved Prompt 030 conversation memory behaviour
- Added configurable maximum tool iterations
- Introduced controlled tool execution, validation and error handling
- Added comprehensive unit tests for ToolRegistry and all built-in tools
- Verified live tool calling using the OpenAI API
- Preserved the offline-first architecture

Prompt 031a – Runtime Context Injection ✅
- Inject current runtime context into every AI conversation
- Supply the current date, time and local time zone to the AI provider
- Introduce a provider-independent runtime context abstraction
- Separate runtime context from conversational memory
- Prepare the architecture for future context providers such as calendar, current project, open documents, weather and system information
- Preserve provider independence and the offline-first architecture

Prompt 031b – Avalonia Desktop UX Refinements ✅
- Introduce a two-pane desktop workspace optimised for tablet-sized displays
- Move prompt creation into an overlay/dialog launched from a 'New Prompt' action
- Maximise space for prompt browsing with independent scrolling
- Refine conversation and prompt layouts for long-running sessions
- Improve desktop usability and visual polish without changing core AI functionality

Prompt 031c – Avalonia Desktop Visual Polish ✅
- Added centralized light and dark Kryten theme resources
- Introduced a restrained professional accent colour
- Refined header, buttons, prompt cards, conversation messages and statuses
- Preserved the Prompt 031b layout and application behaviour

Prompt 031d – Avalonia Prompt Management
- Added single prompt selection and selected-state presentation
- Added Use Prompt without automatic sending
- Reused the prompt editor for create and edit modes
- Added confirmed deletion and immediate prompt/category/search refresh
- Added prompt-management ViewModel tests

---

# 🏁 Platform Foundation Complete

Prompt 031d completes the first generation of the Kryten Assist platform.

The platform now provides:

- Clean Architecture
- REST API
- SQLite and Entity Framework persistence
- React web client
- Avalonia desktop client
- Offline prompt storage
- Semantic search
- AI conversations
- Conversation memory
- Runtime context
- Provider-independent tools
- OpenAI integration

The technical platform is now considered complete.

From this point onwards, Kryten Assist transitions from platform engineering to capability development through reusable Skills.

Future prompts should continue to follow the established structure:

- Goal
- Why This Prompt Exists
- User Experience
- Architecture
- Scope
- Implementation Steps
- Results
- Lessons Learned

---

# Phase 6 – Skills Platform

Introduce the reusable Skills architecture that will power every future capability within Kryten Assist.

Skills represent complete user-facing capabilities rather than technical components.

Each Skill may expose:

- Conversational actions
- Dashboard cards
- Background processing
- Notifications
- Local data storage
- Settings

## Prompt 032 – Skills Framework ✅

Created and verified the provider-independent Skills architecture.

Introduce:

- ISkill
- SkillDefinition
- SkillManifest
- SkillContext
- SkillResult
- SkillRegistry
- Dependency Injection
- Unit tests

No user interface should be introduced.

Completed with comprehensive registry, sample Skill and dependency-injection tests. The framework was verified end to end with all solution tests passing.

---

## Prompt 033 – Cruise Domain Models ✅

Create the shared Cruise domain.

Introduce models such as:

- CruiseOffer
- CruiseSnapshot
- CruisePrice
- CruiseProvider
- CruiseObservation

No web access or dashboard implementation should be added.

---

## Prompt 034 – Cruise of the Week Skill ✅

Implement the first real Skill.

The Skill should:

- Retrieve the current Cruise of the Week
- Parse structured cruise information
- Return provider-independent models
- Remain independently testable

No historical storage should be implemented.

---

## Prompt 035 – Dashboard & Navigation ✅

Introduce the first dashboard experience.

Implement:

- Dashboard page
- Left navigation
- Skill discovery
- Dashboard cards
- Navigation between Skills

This establishes the visual shell that future Skills will plug into.

---

## Prompt 035h – Cruise of the Week View ✅

Make the existing Cruise of the Week Skill usable from the Avalonia application
before the complete Cruise Dashboard is introduced in Prompt 042.

Selecting Cruise of the Week should display a focused capability page with:

- an explicit `Get Cruise of the Week` action
- loading, error and retry states
- the current cruise title
- ship
- departure date and port where available
- duration
- current per-person price
- promotion summary where available
- source and observation timestamp where useful

The first version may present the result as a simple readable statement, for example:

```text
Cruise of the Week is Mediterranean Medley on Marella Explorer,
departing Palma on 27 October 2026 for 7 nights from £903 per person.
```

Requirements:

- reuse the existing `CruiseOfTheWeekSkill` and provider-independent Cruise models
- retrieve only after explicit user action
- do not make a network request during startup or navigation
- keep retrieval and presentation state in a focused ViewModel
- preserve cancellation and controlled failure behavior
- support deterministic offline tests with a fake Skill or provider
- do not add history, watch lists, alerts, scheduling or persistence

Prompt 042 remains responsible for the complete Cruise Dashboard.

The focused view, Skill execution path and deterministic UI tests are complete.
Live retrieval through the original `HttpClient` provider is currently blocked
by TUI's website protection. Prompt 036 replaces direct unattended retrieval as
the primary user workflow with browser-assisted discovery and explicit capture.

---

# Phase 7 – Cruise Assistant

The Cruise Assistant becomes the first complete end-user capability built on the Skills platform.

---

## Prompt 036 – Cruise Discovery and Capture

Create the first usable cruise-research workflow.

Current status: Complete. TUI can be browsed interactively in an embedded,
responsive macOS WebView only after an explicit source action. Robin can capture
the displayed Marella Cruise of the Week into provider-independent Cruise data,
review the observation without saving it and explicitly continue at TUI.

The verified TUI page renders its offer inside an open `tui-product-cards`
shadow root. Kryten reads only that demonstrated component through a fixed,
bounded, read-only script, deduplicates repeated itinerary links and preserves
Marella Cruises as operator separately from TUI as retail source. Trusted-host
navigation, cancellation, stale-result rejection and controlled failure states
are covered by deterministic offline tests.

Prompt 037 – Cruise History and Price Tracking is complete. Prompt 038 – Saved
Cruises and Preferences is next and has not started.

Allow Robin to:

- browse trusted cruise offer pages from within Kryten
- begin with TUI and Marella Cruise of the Week
- move between supported cruise sources using clear buttons or chips
- open the relevant provider page for further investigation or booking
- explicitly capture a displayed cruise as provider-independent Cruise data
- review captured details before saving them
- preserve the source company and source reference
- handle unsupported pages or incomplete extraction honestly

The initial implementation should investigate Avalonia `NativeWebView` as the
browser surface. Browser-specific types, HTML and extraction selectors must not
leak into the shared Cruise domain or application contracts.

Browsing must remain useful when automatic capture is unavailable. Do not make
booking decisions, submit purchases, store payment details or introduce
unattended background scraping.

Architecture must support later cruise operators and retailers without assuming
that the company operating a cruise is always the company advertising or selling
it.

---

## Prompt 037 – Cruise History and Price Tracking

Current status: Complete. Robin can explicitly choose `Record Observation` after
a successful capture, and Kryten persists the provider-independent evidence in
the existing EF Core SQLite database.

Stable sailing identity uses operator, ship, departure date and duration, while
retail source remains a separate history dimension. Meaningful advertised
changes create chronological snapshots; identical evidence advances last-seen
and latest evidence without creating duplicates. Transactions, cancellation,
concurrent writers, migrations and full restart persistence are covered by
isolated deterministic tests.

Recorded Cruise History loads locally without opening TUI and displays:

- First observed
- Current price
- Lowest price
- Highest price
- Observation count
- Trend

Preserve observations even when Robin never books the cruise so that later
offers can be compared with prices seen previously.

Robin manually confirmed that a captured price could be recorded and remained
available after restarting Kryten. The verified solution baseline is 450 tests:
105 Core, 336 Avalonia and 9 API, with 0 failures and 0 skipped.

The current captured price remains neutral; original price, discounted price,
per-person discount and booking-level discount require a later explicit model.

---

## Prompt 038 – Saved Cruises and Preferences

Allow Robin to save, organise and evaluate interesting cruises.

Support:

- interest level such as Not for us, Maybe or Strong candidate
- overall rating
- itinerary, ship and value ratings where useful
- personal notes
- favourite cruises and ships
- departure month
- maximum budget
- preferred cabin

Keep Robin's evaluation separate from provider observations. Use saved ratings
and choices as the first explicit preference data for later cruise comparison.

---

## Prompt 039 – Price Drop Alerts

Detect meaningful price reductions.

Notify users when:

- Prices fall
- New promotions appear
- Saved cruises meet budget or preference criteria

---

## Prompt 040 – Cabin Availability

Monitor cabin availability.

Track:

- Inside
- Outside
- Balcony
- Suites
- Solo cabins

---

## Prompt 041 – New Itinerary Detection

Detect newly published itineraries.

Initially support Marella and the trusted sources proven by Prompt 036.

Architecture should support future providers.

---

## Prompt 042 – Cruise Dashboard

Complete the Cruise Assistant.

Display:

- Cruise discovery sources
- Cruise of the Week
- Saved cruises and ratings
- Price History
- Alerts
- Recent Changes
- Cabin Availability

---

# 🚢 Milestone – Cruise Assistant Complete

The first end-user Skill is complete.

Kryten can now:

- Browse trusted cruise offer sources
- Capture and save interesting cruises
- Rate cruises and record personal preferences
- Monitor Cruise of the Week where the source permits
- Track historical prices
- Compare current offers with previously observed prices
- Detect price changes
- Detect itinerary changes
- Monitor cabin availability
- Present a dedicated Cruise Dashboard

---

# Phase 8 – Home Assistant

Provide insight into Robin's smart home and energy systems.

---

## Prompt 043 – Solar Skill

Monitor:

- Solar generation
- Daily production
- Monthly production
- Historical trends

---

## Prompt 044 – Battery Skill

Monitor:

- Battery charge
- Charge/discharge
- Reserve levels
- Daily cycling

---

## Prompt 045 – Octopus Skill

Provide:

- Current tariff
- Cheapest periods
- Historical comparison
- Tariff recommendations

---

## Prompt 046 – Heat Pump Skill

Display:

- COP
- Consumption
- Flow temperatures
- Daily efficiency

---

## Prompt 047 – Weather Skill

Provide weather context for:

- Solar
- Heat pump
- Energy predictions

---

## Prompt 048 – Home Dashboard

Bring all Home Skills together.

---

# 🏠 Milestone – Home Assistant Complete

---

# Phase 9 – Finance Assistant

Support long-term financial planning.

---

## Prompt 049 – Pension Skill

---

## Prompt 050 – Mortgage Skill

---

## Prompt 051 – Premium Bonds Skill

---

## Prompt 052 – Spending Skill

---

## Prompt 053 – Finance Dashboard

---

# 💷 Milestone – Finance Assistant Complete

---

# Phase 10 – Health Assistant

Support health and wellbeing.

---

## Prompt 054 – Glucose Dashboard

---

## Prompt 055 – Exercise Skill

---

## Prompt 056 – Weight Skill

---

## Prompt 057 – Medication Skill

---

## Prompt 058 – Health Dashboard

---

# ❤️ Milestone – Health Assistant Complete

---

# Phase 11 – Developer Assistant

Use Kryten to improve Kryten.

---

## Prompt 059 – Git Skill

---

## Prompt 060 – Build Skill

---

## Prompt 061 – Test Runner

---

## Prompt 062 – Prompt Generator

---

## Prompt 063 – Architecture Review

---

## Prompt 064 – Development Dashboard

---

# 💻 Milestone – Developer Assistant Complete

---

# Phase 12 – Career Assistant

Support ongoing professional development.

---

## Prompt 065 – Job Skill

---

## Prompt 066 – CV Assistant

---

## Prompt 067 – Interview Coach

---

## Prompt 068 – Mock Interviews

---

## Prompt 069 – Career Dashboard

---

# 👔 Milestone – Career Assistant Complete

At this point, Kryten Assist has evolved into a modular personal assistant platform powered by reusable Skills, shared infrastructure and a unified dashboard experience.

# Platform Foundation Achievements

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
✔ Offline semantic search implemented
✔ Hybrid keyword and semantic search architecture established
✔ OpenAI embedding provider integrated
✔ Runtime AI provider resilience implemented
✔ Semantic search optimised with debouncing, cancellation and caching
✔ Offline-first AI architecture preserved
✔ Provider-independent AI tool architecture established
✔ Built-in desktop tools implemented
✔ Tool registry and dependency injection framework established
✔ OpenAI function calling integrated
✔ Deterministic tool unit testing established
