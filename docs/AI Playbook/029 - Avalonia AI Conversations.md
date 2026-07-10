

# Prompt 029 – Avalonia AI Conversations

## Goal

Introduce the first AI conversation capability to the Avalonia desktop client.

This prompt allows the user to enter a message, send it to an AI conversation provider, and display the assistant response in the desktop application.

This is a foundation prompt only.

No conversation memory, persistence, streaming responses, markdown rendering, tool calling, plugins, voice, image generation, or multi-provider support should be added in this prompt.

---

## Why This Prompt Exists

Prompts 026–028 established the AI infrastructure required by the Avalonia client:

- Prompt 026 introduced the embedding abstraction.
- Prompt 027 introduced offline semantic ranking.
- Prompt 028 introduced OpenAI integration, configuration, resilience, cancellation and provider-status reporting.

The application can now organise and search prompt templates intelligently, but it cannot yet send a user message to an AI model and display the response.

This prompt introduces that first conversation capability while keeping conversation memory, persistence and advanced interaction features for later prompts.

---

## Scope

Implement only the first one-shot AI conversation workflow.

## Architecture

The implementation should preserve the existing Avalonia MVVM and dependency-injection architecture:

- The View owns conversation layout and bindings.
- The ViewModel owns conversation state and commands.
- `IConversationService` defines the provider-independent conversation abstraction.
- `OpenAIConversationService` implements the abstraction.
- OpenAI-specific types remain inside the service implementation.
- Configuration selects the conversation model and system prompt.
- The ViewModel should not depend directly on OpenAI SDK types.

The first implementation should be stateless from the provider's perspective. Each request should contain only:

- the configured system prompt;
- the current user message.

Previous conversation messages should be displayed in the UI but should not yet be sent back to the model as context. Conversation memory belongs in Prompt 030.

## Design Principles

- Introduce the abstraction before advanced conversation features.
- Keep embeddings and conversations as separate AI capabilities.
- Keep OpenAI-specific implementation details behind `IConversationService`.
- Preserve the existing offline prompt-management features.
- Fail clearly when conversation configuration is invalid.
- Surface runtime provider errors rather than failing silently.
- Prevent duplicate sends while a request is in progress.
- Support cancellation of the active conversation request.
- Keep the first UI simple and functional.

### In Scope

- Add provider-independent conversation models.
- Add `IConversationService`.
- Add `OpenAIConversationService`.
- Add conversation configuration for model and system prompt.
- Read the existing `OPENAI_API_KEY` environment variable.
- Add conversation state to `MainWindowViewModel`.
- Add a send command.
- Add a cancel command or cancellation behaviour for the active request.
- Add a simple conversation history collection for display only.
- Display user and assistant messages.
- Display a busy state while waiting for a response.
- Display a clear non-fatal error message when a conversation request fails.
- Disable or prevent duplicate sends while a request is active.
- Keep the implementation inside the Avalonia desktop client.

### Out of Scope

- Sending previous messages back to the model.
- Conversation memory.
- Conversation persistence.
- Conversation titles.
- Multiple conversations or tabs.
- Streaming responses.
- Markdown rendering.
- Syntax highlighting.
- Tool calling.
- Plugins.
- Function calling.
- RAG.
- Prompt-template injection into conversations.
- Automatic semantic-search context.
- Voice input or output.
- Image generation.
- Multiple AI providers.
- Offline deterministic conversation responses.
- API or cloud synchronisation.

---

## Expected Outcome

After completing this prompt:

- The user can type a message in the Avalonia client.
- The user can send the message through a command.
- The user message appears in the conversation history.
- The assistant response appears in the conversation history.
- A busy state is visible while the request is running.
- Duplicate sends are prevented while waiting for a response.
- The active request can be cancelled safely.
- Provider errors are displayed clearly without crashing the application.
- Previous messages remain visible but are not yet sent as model context.
- Existing prompt browsing, editing, categories and search continue to work.

---

## Conversation Models

Typical provider-independent models may include:

### `ConversationRole`

Values:

- `System`
- `User`
- `Assistant`

### `ConversationMessage`

Typical properties:

- `ConversationRole Role`
- `string Content`

### `ConversationRequest`

Typical properties:

- `string SystemPrompt`
- `string UserMessage`

### `ConversationResponse`

Typical properties:

- `string Content`

The exact shape may be refined during implementation, but the models should remain independent from the OpenAI SDK.

---

## Configuration

Add a conversation configuration section that contains no secrets.

Example:

```json
{
  "Conversation": {
    "Model": "gpt-4.1-mini",
    "SystemPrompt": "You are Kryten Assist. Provide concise, accurate and practical help."
  }
}
```

The API key must continue to come from:

```text
OPENAI_API_KEY
```

The key must not be stored in `appsettings.json` or committed to source control.

The model name should remain configurable rather than hard-coded in the service.

---

## Implementation Notes

- Add a `ConversationOptions` class.
- Add provider-independent conversation models.
- Add an asynchronous `IConversationService` method accepting a cancellation token.
- Use the official OpenAI .NET SDK already added in Prompt 028.
- Keep the configured system prompt outside the ViewModel.
- Add an observable conversation-history collection to the ViewModel.
- Add user messages to the history before sending the request.
- Add assistant messages only after a successful response.
- Preserve the user's message or expose a retry-friendly state when a request fails.
- Clear the input only when the send operation starts successfully.
- Prevent empty or whitespace-only messages from being sent.
- Prevent duplicate sends while `IsConversationBusy` is true.
- Re-throw or separately handle `OperationCanceledException` so cancellation is not displayed as a provider failure.
- Display runtime errors through a ViewModel property bound to the UI.
- Do not include previous messages in the provider request yet.
- Do not change the embedding or semantic-search services.

---

## Expected Files

### New Files

Typical new files may include:

- `Options/ConversationOptions.cs`
- `Models/ConversationRole.cs`
- `Models/ConversationMessage.cs`
- `Models/ConversationRequest.cs`
- `Models/ConversationResponse.cs`
- `Services/IConversationService.cs`
- `Services/OpenAIConversationService.cs`

### Updated Files

Typical files likely to change include:

- `Program.cs`
- `ViewModels/MainWindowViewModel.cs`
- `MainWindow.axaml`
- `appsettings.json`

The existing OpenAI NuGet package should be reused unless implementation requires a package update.

---

## Implementation Steps

1. Add `ConversationOptions`.
2. Add the provider-independent conversation models.
3. Add `IConversationService` with cancellation support.
4. Add `OpenAIConversationService`.
5. Map the OpenAI response into `ConversationResponse`.
6. Add non-secret conversation configuration.
7. Register conversation options and services with Dependency Injection.
8. Add conversation state to `MainWindowViewModel`.
9. Add an observable conversation-history collection.
10. Add `SendMessageCommand`.
11. Add cancellation support for the active request.
12. Add busy-state and error-state properties.
13. Prevent empty and duplicate sends.
14. Add the conversation UI to `MainWindow.axaml`.
15. Display user and assistant messages.
16. Display busy and error states.
17. Confirm previous messages are not yet sent as provider context.
18. Run `dotnet build`.
19. Run `dotnet test`.
20. Manually verify a successful request, cancellation and runtime-error handling.

---

## Success Criteria

- `ConversationOptions` exists.
- Provider-independent conversation models exist.
- `IConversationService` exists.
- `OpenAIConversationService` implements `IConversationService`.
- Conversation model and system prompt are configuration-driven.
- No API key is stored in source control.
- The user can send a non-empty message.
- User and assistant messages are displayed.
- Busy state is displayed while waiting.
- Duplicate sends are prevented.
- The active request can be cancelled safely.
- Runtime provider failures are visible and non-fatal.
- Previous messages are displayed but not sent as context.
- Existing embedding and semantic-search behaviour remains unchanged.
- `dotnet build` succeeds.
- `dotnet test` succeeds.

---

## Definition of Done

This prompt is considered complete when:

- The Avalonia client can resolve `IConversationService` through DI.
- A user message can be sent to the configured OpenAI conversation model.
- The assistant response is displayed in the conversation history.
- The ViewModel remains independent from OpenAI-specific types.
- Busy, cancellation and error states behave correctly.
- No conversation memory or persistence has been introduced.
- Existing prompt-management and search features continue to work.
- The solution builds successfully.
- All automated tests continue to pass.

---

## Verification

Verify the implementation by confirming:

- The application starts with valid conversation configuration.
- Empty or whitespace-only messages cannot be sent.
- Sending a message immediately adds the user message to the history.
- A busy indicator appears while awaiting the response.
- A second send cannot start while the first is active.
- A successful assistant response is added to the history.
- Cancelling a request does not show a provider-error message.
- An API, billing, quota, authentication or network failure displays a clear non-fatal message.
- Previous displayed messages are not included in the next provider request.
- Prompt loading still works.
- Prompt creation still works.
- Category chips still work.
- Keyword and semantic search still work.
- `dotnet build` succeeds.
- `dotnet test` succeeds.

---

## Result

Status: Completed.

### Implementation Results

- Added `ConversationOptions` for model and system prompt configuration.
- Added provider-independent conversation models (`ConversationRole`, `ConversationMessage`, `ConversationRequest` and `ConversationResponse`).
- Added `IConversationService` abstraction.
- Implemented `OpenAIConversationService` using the official OpenAI .NET SDK.
- Added conversation configuration to `appsettings.json` while continuing to read `OPENAI_API_KEY` from the environment.
- Registered conversation services and options with Dependency Injection.
- Extended `MainWindowViewModel` with conversation state, busy state, error state, cancellation support and conversation history.
- Added Send and Cancel commands.
- Added a simple conversation UI to the Avalonia client.
- Displayed user and assistant messages in the conversation history.
- Prevented duplicate sends while a request is active.
- Preserved the user's message after runtime failures to allow retry.
- Verified OpenAI connectivity by successfully reaching the API and handling quota errors gracefully.
- Confirmed that previous conversation messages are displayed but are not yet sent back to the provider.
- Verified `dotnet build` completed successfully.
- Verified the conversation workflow manually.

### Notes

The current conversation UI is intentionally functional rather than polished. Desktop usability improvements identified during implementation have been deferred to Prompt 031a to keep Prompt 029 focused on conversation architecture.

---

## Lessons Learnt

- Conversation transport and conversation memory are separate architectural concerns. Prompt 029 establishes the transport layer while Prompt 030 will introduce conversational memory.
- Keeping OpenAI-specific code behind `IConversationService` maintained a clean MVVM architecture and allows additional providers to be introduced later.
- Runtime provider failures should be surfaced to the user rather than failing silently. The application remained usable while displaying OpenAI quota errors.
- A retry-friendly user experience is improved by restoring the user's message after failed requests.
- Separating UX refinements into Prompt 031a prevented architectural work from becoming coupled with desktop interface polish.
- Incremental implementation with a successful `dotnet build` after each step significantly reduced debugging effort.
- Startup validation of required configuration provides earlier and clearer feedback than discovering configuration problems during the first conversation request.