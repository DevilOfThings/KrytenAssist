

# Prompt 030 – AI Memory

## Goal

Introduce provider-independent, bounded, in-memory conversation memory to KrytenAssist. This enables the AI to remember the recent context of a conversation, improving its ability to answer follow-up questions naturally.

## Why This Prompt Exists

Prompt 029 introduced provider-independent conversation transport, allowing KrytenAssist to send and receive chat messages via a pluggable AI provider. However, each conversation turn was stateless: only the latest user message was sent to the provider. This prompt adds short-term, in-memory conversation memory, so the assistant can maintain context across multiple turns.

## Scope

### In Scope
- Bounded, in-memory short-term conversation memory for AI chat in KrytenAssist.
- Memory is provider-independent and managed by a new `IConversationMemory` abstraction.
- Only successful (completed) user/assistant message pairs are committed to memory.
- Previous conversation turns are included in subsequent requests, up to a configurable maximum.
- User can clear the conversation memory via a command.

### Out of Scope
- Long-term or persistent memory (no disk, database, or cloud storage).
- Semantic memory, vector search, or retrieval-augmented generation (RAG).
- Token counting, summarisation, or memory compression.
- Conversation titles, search, or multi-conversation UI features.
- UI redesign (beyond adding a "Clear Conversation" command/button).

## Functional Requirements

### Conversation Memory
- All successful user/assistant turns are stored in a bounded, in-memory conversation memory.
- Memory is provider-independent and does not persist across application restarts.

### Successful Turns Only
- Only after a successful assistant response is received should the user/assistant turn be committed to memory.
- Failed or cancelled requests do not affect memory.

### Conversation Request
- When sending a new user message, the conversation request should include all previous messages in memory (up to the configured maximum) in the correct order.
- The memory should include both user and assistant messages.

### Provider Mapping
- The memory abstraction should not depend on the specifics of the underlying AI provider.
- The conversation memory is injected into the conversation service and used to construct requests.

### Configurable Memory Size
- The maximum number of previous conversation messages to include is configurable via `ConversationOptions.MaxContextMessages`.
- When the limit is reached, the oldest messages are discarded (FIFO).

### Clear Conversation
- The user can clear the conversation memory at any time, resetting the context.
- This action should be available via a command in the UI.

## Architecture

Prompt 029 separated conversation transport from the UI and provider. Prompt 030 introduces a new memory layer, further decoupling state from transport and provider logic.

### Architecture Diagram

```
MainWindowViewModel
      │
      ▼
IConversationMemory
      │
      ▼
ConversationRequest
      │
      ▼
IConversationService
      │
      ▼
OpenAIConversationService
      │
      ▼
OpenAI SDK
```

- `MainWindowViewModel` orchestrates user input and commands.
- `IConversationMemory` manages short-term conversation memory.
- `ConversationRequest` is constructed using the current memory.
- `IConversationService` sends the request to the provider.
- `OpenAIConversationService` implements the provider-specific logic.

## Suggested Files

### New files
- `Services/IConversationMemory.cs` – Interface for conversation memory.
- `Services/InMemoryConversationMemory.cs` – Bounded in-memory implementation.

### Updated files
- `Options/ConversationOptions.cs` – Add `MaxContextMessages` option.
- `Models/ConversationRequest.cs` – Accept memory context.
- `Services/OpenAIConversationService.cs` – Use memory to build requests.
- `ViewModels/MainWindowViewModel.cs` – Integrate memory, add "Clear Conversation" command.
- `MainWindow.axaml` – Add "Clear Conversation" button.
- `Program.cs` – Register memory implementation in DI.
- `appsettings.json` – Add default for `MaxContextMessages`.

## Acceptance Criteria

- KrytenAssist remembers the last N (configurable) conversation turns in memory, even after multiple follow-up questions.
- Only successful user/assistant turns are remembered.
- Previous turns are included in new requests, improving follow-up accuracy.
- User can clear conversation memory at any time.
- Memory is not persisted beyond application lifetime.
- No semantic/RAG/long-term/persistent memory is introduced.

## Expected Result

### Example Conversation

**User:** Who is Alan Turing?  
**Assistant:** Alan Turing was a British mathematician, logician, and computer scientist, widely considered to be the father of theoretical computer science and artificial intelligence.  
**User:** Where was he born?  
**Assistant:** Alan Turing was born in Maida Vale, London, England, on 23 June 1912.  
**User:** What is he famous for?  
**Assistant:** Alan Turing is famous for his work on breaking the Enigma code during World War II, formulating the concept of the Turing machine, and laying the foundations of modern computing and artificial intelligence.

In this example, the assistant correctly answers follow-up questions ("Where was he born?", "What is he famous for?") by maintaining short-term memory of the conversation context. Clearing the conversation memory would reset the assistant's context to "stateless" mode.

## Result

Status: Completed.

### Implementation Results

- Added `IConversationMemory` as a provider-independent conversation memory abstraction.
- Implemented `InMemoryConversationMemory` with configurable bounded conversation history.
- Added `MaxContextMessages` to `ConversationOptions`.
- Updated `ConversationRequest` to carry provider-independent conversation messages.
- Updated `OpenAIConversationService` to translate provider-independent conversation messages into OpenAI SDK message types.
- Integrated conversation memory into `MainWindowViewModel`.
- Included previous successful conversation turns in subsequent AI requests.
- Ensured failed and cancelled requests are not committed to memory.
- Added `ClearConversationCommand` together with a Clear button in the conversation UI.
- Verified conversational memory using a live OpenAI API account.
- Verified `dotnet build` completed successfully throughout implementation.

### Notes

Prompt 030 intentionally introduces short-term conversational memory only. Runtime context (current date, time, environment and similar dynamic information) has been deferred to Prompt 031b, while desktop usability improvements remain in Prompt 031a.

## Lessons Learnt

- Conversation transport and conversation memory are separate architectural concerns. Prompt 029 established the transport layer while Prompt 030 introduced conversational memory.
- Provider-independent conversation models allowed memory to be implemented without introducing OpenAI-specific types into the ViewModel.
- Only successful user/assistant turns should be committed to memory. Failed and cancelled requests must not influence future conversations.
- Bounded memory provides predictable behaviour and avoids unbounded context growth while remaining simple to understand and configure.
- Testing against the live OpenAI API verified conversational behaviour that could not be fully validated through compilation or mocked services alone.
- Separating runtime context (current date, time and environment) from conversational memory keeps responsibilities clear and prepares the architecture for Prompt 031b.
- The `IConversationMemory` abstraction provides a clean extension point for future implementations such as persistent, semantic or hybrid conversation memory without changing the ViewModel.
- Incremental implementation with a successful `dotnet build` after each step continued to reduce debugging effort and simplified architectural evolution.