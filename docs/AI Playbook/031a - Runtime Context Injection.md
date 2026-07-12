# Prompt 031a – Runtime Context Injection

## Goal

Introduce provider-independent runtime context injection into the AI conversation pipeline.

This prompt allows Kryten Assist to automatically provide contextual information about the current execution environment to the AI provider without relying on conversation memory or tool invocation.

The implementation must preserve the existing provider-independent architecture introduced in Prompts 029–031 while preparing the application for future context providers such as Git, calendar integration, open documents, Home Assistant, weather and system information.

This is a foundation prompt only.

No Git integration, calendar integration, Home Assistant integration, Rider integration, weather services, MCP servers, open document discovery or operating system automation should be implemented in this prompt.

---

## Why This Prompt Exists

Prompt 029 introduced provider-independent AI conversations.

Prompt 030 introduced conversational memory.

Prompt 031 introduced provider-independent tool execution.

The AI can now remember previous conversation turns and invoke deterministic application tools.

However, the AI still has no awareness of the environment in which it is running.

For example, if asked:

- What day is it?
- What time is it?
- Which time zone am I in?

the AI must currently infer the answer or rely on model knowledge.

This information should instead be supplied automatically by the application.

Runtime context differs fundamentally from conversation memory.

Conversation memory records what has happened during the discussion.

Runtime context describes facts that are true at the moment a request is made.

These facts should never become part of long-term conversation memory.

Prompt 031a introduces a dedicated runtime context abstraction that keeps these responsibilities separate.

---

## Scope

Implement only the provider-independent runtime context foundation.

### In Scope

- Introduce an IRuntimeContextProvider abstraction
- Implement the default runtime context provider
- Supply the current date
- Supply the current time
- Supply the local time zone
- Register the provider using dependency injection
- Inject runtime context into every conversation request
- Update the OpenAI provider to include runtime context within the generated system message
- Preserve Prompt 030 conversation memory behaviour
- Preserve Prompt 031 tool execution behaviour
- Add comprehensive unit tests

### Out of Scope

- Git integration
- Current solution discovery
- Current project discovery
- Open document discovery
- Calendar integration
- Weather integration
- Home Assistant integration
- MCP servers
- Operating system automation
- User profile context
- Runtime context persistence
- Context editing UI

---

## Architecture Overview

The conversation pipeline should now consist of four distinct information sources.

```
System Prompt
        │
        ▼
Runtime Context
        │
        ▼
Conversation Memory
        │
        ▼
Current User Message
```

Each layer has a unique responsibility.

### System Prompt

Permanent behavioural instructions.

Examples:

- Assistant identity
- Tone
- Engineering guidance

### Runtime Context

Facts that are true now.

Examples:

- Current date
- Current time
- Time zone

Future prompts will extend this layer with additional context providers.

### Conversation Memory

Historical conversation.

Contains only successful user and assistant exchanges.

### Current User Message

The user's latest request.

---

## Design Principles

The provider must not generate runtime context.

Instead, runtime context should be supplied through a provider-independent abstraction.

Conversation providers consume runtime context without understanding where it originated.

The implementation must preserve provider independence.

No OpenAI SDK types may appear outside the OpenAI provider.

Runtime context must never be stored in conversation memory.

Future context providers must be addable without changing conversation providers.

The implementation must remain compatible with offline-first development.

Runtime context should be generated on demand for every conversation request and should never be cached or persisted.


---

## Implementation

### Step 1 – Create the Runtime Context Provider Abstraction

Create a provider-independent runtime context abstraction.

Introduce:

```
IRuntimeContextProvider
```

The interface should expose a single method returning a formatted runtime context string.

The returned value should contain the complete runtime context ready to be appended to the system prompt.

Conversation providers must depend only upon this abstraction.

No provider-specific implementation should be referenced.

### Step 2 – Create the Runtime Context Provider

Create the default implementation.

It should gather:

- Current local date
- Current local time
- Local time zone

The implementation should rely on the existing application clock abstraction wherever possible to ensure deterministic unit testing.

The provider should return a formatted runtime context string suitable for appending directly to the conversation system prompt.

No formatting logic should exist outside the runtime context provider.

### Step 3 – Register the Runtime Context Provider

Register the default runtime context provider using the application's dependency injection container.

The runtime context provider should have a singleton lifetime.

Conversation services should receive the provider through constructor injection.

No service should manually instantiate the provider.

The registration should follow the same dependency injection conventions established in previous prompts.

---

### Step 4 – Inject Runtime Context into the Conversation Pipeline

Update the conversation provider to retrieve runtime context before constructing the system prompt for each AI conversation.

The runtime context should be retrieved once for each conversation request.

The conversation provider must obtain runtime context exclusively through the `IRuntimeContextProvider` abstraction.

---

### Step 5 – Update the OpenAI Conversation Provider

Update the OpenAI provider to include runtime context within the generated system prompt.

The provider should retrieve the formatted runtime context from `IRuntimeContextProvider` and append it to the existing system prompt before constructing the provider-specific message list.

For example:

```
Current Runtime Context

CurrentDate: 12 July 2026
CurrentTime: 09:15
TimeZone: Europe/London
```

Formatting should remain encapsulated within the runtime context provider.

The runtime context should appear before conversation memory so that the AI always receives current environmental information first.

No tool invocation should be required.

---

### Step 6 – Add Unit Tests

Create comprehensive unit tests covering:

- IRuntimeContextProvider
- DefaultRuntimeContextProvider
- Dependency injection registration
- Runtime context injection into conversation requests
-Runtime context injection into the OpenAI provider


Verify that:

- CurrentDate is supplied.
- CurrentTime is supplied.
- TimeZone is supplied.
- Runtime context appears before conversation memory.
- Runtime context is generated for every conversation request.
- Runtime context is not persisted by the memory implementation.

Existing Prompt 029–031 tests should continue to pass unchanged.

---

### Step 7 – Verify End-to-End Behaviour

Run the application and verify:

- AI conversations continue to function normally.
- Existing conversation memory behaves exactly as before.
- Tool execution continues to work correctly.
- Runtime context is automatically supplied to every conversation.
- Runtime context is regenerated for each conversation request.
- No provider-specific types leak outside the OpenAI implementation.
- Dependency injection resolves successfully.
- All existing tests continue to pass.

No user interface changes should be required.

---

## Testing

Verify the following:

- IRuntimeContextProvider resolves correctly.
- The default runtime context provider returns the expected formatted context.
- Runtime context includes the current date, current time and local time zone.
- Runtime context is retrieved once per conversation request.
- Runtime context is included in every AI request.
- Runtime context appears before conversation memory.
- Conversation memory remains unchanged.
- Tool execution remains unchanged.
- Existing integration tests continue to pass.

---

## Acceptance Criteria

Prompt 031a is complete when:

- A provider-independent IRuntimeContextProvider abstraction exists.
- A default runtime context provider has been implemented.
- CurrentDate, CurrentTime and TimeZone are automatically supplied.
- Runtime context is injected into every conversation request.
- Runtime context remains separate from conversation memory.
- The OpenAI provider consumes runtime context without exposing provider-specific types.
- Dependency injection has been updated.
- Comprehensive unit tests have been added.
- Existing Prompt 029–031 functionality continues to operate without regression.

---
# Results

> Completed after implementation.

### Status

✅ Completed

### Files Created

- `KrytenAssist.Avalonia/Services/IRuntimeContextProvider.cs`
- `KrytenAssist.Avalonia/Services/DefaultRuntimeContextProvider.cs`
- `KrytenAssist.Avalonia/DependencyInjection/RuntimeContextServiceCollectionExtensions.cs`
- `KrytenAssist.Avalonia.Tests/Services/DefaultRuntimeContextProviderTests.cs`

### Files Updated

- `KrytenAssist.Avalonia/OpenAIConversationService.cs`
- `KrytenAssist.Avalonia/Program.cs` *(or the application's composition root, depending on your implementation)*
- `docs/AI Playbook/031a - Runtime Context Injection.md`

### Build

✅ Successful

### Git Commit

`Add provider-independent runtime context injection`---

# Lessons Learned

- Existing infrastructure should always be reviewed before introducing new abstractions.
- Reusing the existing IClock abstraction simplified the implementation and improved testability.
- Runtime context belongs alongside conversation composition rather than conversation memory.
- Keeping runtime context as a formatted provider-independent string avoided introducing unnecessary models.
- Separate dependency injection extension classes help maintain clear architectural boundaries.