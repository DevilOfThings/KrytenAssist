# Prompt 032 – Skills Framework

## Goal

Introduce the provider-independent Skills architecture that enables Kryten Assist to register, discover and execute reusable user-facing capabilities.

Validate the architecture by implementing a simple sample Skill before introducing real business capabilities.

This prompt establishes the foundation for all future Skills, including Cruise, Home, Finance, Health, Developer and Career.

No cruise functionality, dashboard implementation or user interface should be introduced in this prompt.

---

## Why This Prompt Exists

Prompts 001–031d established the technical platform for Kryten Assist.

The application now supports:

- AI conversations
- Conversation memory
- Runtime context
- Provider-independent tools

These provide powerful technical capabilities, but they do not define end-user features.

From this point onwards, **Skills** become the primary unit of functionality within Kryten Assist.

Each Skill represents a complete user-facing capability that may expose conversational behaviour, dashboard content, background processing and local data.

This marks the transition from **platform engineering** to **capability engineering**.

---

## Vision

Skills become the fundamental building blocks of Kryten Assist.

Rather than implementing isolated technical features, future development will focus on delivering complete user-facing capabilities.

Each Skill should feel like an independent application that can:

- expose conversational behaviour
- provide dashboard content
- maintain its own data
- perform background processing
- interact with other Skills where appropriate

The long-term vision is for Kryten Assist to become a modular personal assistant platform where new capabilities can be added simply by introducing additional Skills.

This prompt establishes that architectural direction.

---

## Skills and Tools

Although Skills and Tools are closely related, they serve different purposes within the architecture.

### Tool

A Tool performs a focused technical operation.

Examples include:

- Getting the current date and time
- Performing a calculation
- Calling an external service
- Reading a file
- Retrieving data from an API

Tools should be:

- Small
- Focused
- Reusable
- Independently testable

A Tool should do one thing well.

### Skill

A Skill delivers a complete user-facing capability.

A Skill may orchestrate one or more Tools together with business logic, storage and presentation.

Examples include:

- Cruise of the Week
- Home Energy
- Premium Bonds
- Git Assistant
- Interview Coach

Skills are what users interact with.

Tools are implementation building blocks.

In other words:

> **Tools perform operations. Skills deliver capabilities.**

---

## Design Principles

Every Skill should:

- be provider independent
- have a single responsibility
- be independently testable
- be discoverable
- support dependency injection
- expose descriptive metadata
- remain independent of any user interface
- remain independent of any AI provider
- favour composition over inheritance
- be capable of evolving independently from other Skills

---

## User Experience

Although this prompt introduces no user interface, it enables future scenarios such as:

- "Show me my Cruise Dashboard."
- "Track this cruise."
- "Show today's electricity costs."
- "Review my Premium Bonds."
- "Run my Git status."

without requiring changes to the core application architecture.

---

## Proposed Architecture

```text
Application
└── Skills
    ├── ISkill
    ├── SkillManifest
    ├── SkillContext
    ├── SkillRequest
    ├── SkillResult
    ├── ISkillRegistry
    └── SkillRegistry
```

Infrastructure implementations will be introduced by later prompts.

---

## Skill Contract

Every Skill should answer the following questions:

| Question | Component |
|----------|-----------|
| Who am I? | SkillManifest |
| What do I do? | ISkill |
| What information do I need? | SkillContext |
| What input do I accept? | SkillRequest |
| What do I return? | SkillResult |
| How am I discovered? | ISkillRegistry |
| How am I executed? | SkillRegistry |

This contract should remain stable as the Skills platform evolves.

---

## Scope

### In Scope

- Create the Skills namespace.
- Introduce the core Skill abstractions.
- Create Skill request and result models.
- Create Skill metadata.
- Create Skill registry interfaces.
- Implement the Skill registry.
- Configure dependency injection.
- Implement a sample Skill.
- Add comprehensive unit tests.

### Out of Scope

- Cruise functionality
- Dashboard implementation
- Background scheduling
- Notifications
- Local storage
- Web requests
- Avalonia user interface
- AI provider integration
- Tool execution changes

---

## Implementation Steps

### Step 1

Create the Skills namespace and folder structure.

---

### Step 2

Implement:

- ISkill
- SkillManifest
- SkillContext
- SkillRequest
- SkillResult

---

### Step 3

Implement:

- ISkillRegistry
- SkillRegistry

Support Skill registration and discovery.

---

### Step 4

Configure dependency injection.

Register the Skill Registry with the application's dependency injection container.

Provide a single extension point where future prompts can register concrete Skills.

Automatic discovery may be introduced by a future prompt if it provides clear benefits over explicit registration.

---

### Step 5

Implement a simple sample Skill to validate the framework.

The sample Skill should:

- implement `ISkill`
- register with the Skill Registry
- expose a valid `SkillManifest`
- execute successfully
- demonstrate `SkillRequest`, `SkillContext` and `SkillResult`

The sample Skill should be intentionally simple and exist only to prove the framework.

No business functionality should be introduced.

---

### Step 6

Add unit tests covering:

- registration
- discovery
- duplicate detection
- unknown Skill lookup
- manifest retrieval
- sample Skill registration
- sample Skill execution

---

### Step 7

Verify the framework can register and execute the sample Skill without requiring any user interface.

The sample Skill should:

- be successfully registered with the `SkillRegistry`
- be discoverable by its identifier
- execute successfully through the `ISkill` contract
- demonstrate the use of `SkillRequest`, `SkillContext` and `SkillResult`

No concrete business Skills should exist at the end of this prompt.

---

## Completion Criteria

At the end of this prompt it should be possible to introduce a new Skill simply by implementing `ISkill`.

The registry should successfully register the Skill and expose its metadata.

The sample Skill should demonstrate that the Skills framework is ready for real business capabilities.

No Cruise, Home, Finance or other business functionality should yet be implemented.

The Skills platform should now be ready for Prompt 033.

---

## Results

#### Status

_In Progress_

Completed:

- Step 1 – Skills namespace and folder structure
- Step 2 – Core Skill abstractions
- Step 3 – Skill registry

Remaining:

- Step 4 – Dependency Injection 
- Step 5 – Sample Skill
- Step 6 – Unit Tests
- Step 7 – Framework Verification

### Files Created

- Skills/Models/SkillManifest.cs
- Skills/Models/SkillContext.cs
- Skills/Models/SkillRequest.cs
- Skills/Models/SkillResult.cs
- Skills/Services/ISkill.cs
- Skills/Services/ISkillRegistry.cs
- Skills/Services/SkillRegistry.cs

### Files Updated

- None

### Build

- ✅ dotnet build successful
- ✅ dotnet test successful

### Git Commit

_Not Created_

---

# Lessons Learned

- Skills are intentionally a higher-level abstraction than Tools. Tools perform individual operations; Skills deliver complete user-facing capabilities.
- Keeping the initial contracts small has avoided speculative architecture and makes future expansion easier.
- The Skill Registry should remain a lightweight in-memory component responsible only for registration and lookup.
- Case-insensitive Skill identifiers improve usability while still enforcing uniqueness.
- Registration order should be preserved so future user interfaces can present Skills consistently.
- Automatic discovery and dependency injection are best introduced after the core contracts have stabilised.
