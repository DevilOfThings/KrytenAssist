# Prompt 031 – Tools

## Goal

Add the first provider-independent tools framework to the Avalonia desktop client.

This prompt allows the AI conversation service to expose a small set of safe application tools, receive tool requests from the configured AI provider, execute those tools through application-owned abstractions, and continue the conversation using the tool results.

Unlike previous prompts, this introduces the first capability that allows the AI to interact with the application rather than simply generating text.

The purpose of this prompt is not to build desktop automation.

The purpose is to establish a provider-independent architecture that future desktop capabilities can safely build upon.

The implementation must preserve the existing separation between:

- Avalonia user interface
- ViewModels
- Conversation services
- AI provider implementations
- Application-owned tool execution

This is a foundation prompt only.

No filesystem access, shell execution, desktop automation, project indexing, Home Assistant integration, MCP support, dynamic plugin loading, approval workflows or destructive tools should be introduced in this prompt.

---

## Why This Prompt Exists

Prompt 029 introduced AI conversations.

Prompt 030 introduced conversational memory.

The desktop client can now hold natural multi-turn conversations while preserving successful user and assistant messages between requests.

However, the AI currently has no deterministic way of interacting with the application.

Everything it produces is generated text.

It cannot reliably:

- retrieve the current date and time
- perform trusted calculations
- identify the running application
- inspect local projects
- search prompt templates
- execute desktop functionality

The first three examples are suitable for this prompt.

The remaining examples demonstrate why a reusable tools framework is required.

Without a provider-independent design, tool execution would quickly become coupled directly to the OpenAI SDK.

That coupling would:

- make future providers difficult to support
- leak provider concepts into ViewModels
- reduce testability
- make future desktop capabilities harder to evolve

Prompt 031 introduces the first provider-independent tools framework.

The application—not the AI provider—owns:

- tool definitions
- tool registration
- tool discovery
- tool invocation
- tool execution
- tool results

The provider is responsible only for translating between its own protocol and the application abstractions.

This keeps Kryten Assist independent of any individual AI provider and provides the architectural foundation for future capabilities such as project awareness, prompt-library search, build execution, Home Assistant integration and desktop automation.

---

## Scope

Implement only the first provider-independent tools framework.

### In Scope

- provider-independent tool models
- provider-independent tool contracts
- tool registry
- dependency injection
- deterministic built-in tools
- provider translation
- tool execution
- conversation continuation
- configurable maximum tool iterations
- compatibility with Prompt 030 memory
- focused automated tests
- live OpenAI verification

### Out of Scope

Do not add:

- filesystem access
- shell execution
- process launching
- Rider automation
- project indexing
- semantic search
- Home Assistant
- runtime context injection
- MCP
- plugin loading
- permissions
- approval dialogs
- background execution
- desktop automation
- React changes
- API changes

---

## Architectural Requirements

The tools framework must remain completely provider-independent.

The application must own the complete tool model and execution pipeline.

The AI provider must only translate between its own protocol and the application abstractions.

Future providers should be able to replace the OpenAI implementation without requiring changes to the ViewModels, tool implementations or registry.

### The Application Owns the Tool Model

The application must define and own:

- Tool definitions.
- Tool parameter schemas.
- Tool invocations.
- Tool results.
- Tool registration.
- Tool discovery.
- Tool execution.
- Tool failure behaviour.

The following application abstractions should remain provider-independent:

- `ToolDefinition`
- `ToolInvocation`
- `ToolResult`
- `ITool`
- `IToolRegistry`

Provider-specific SDK types must never appear in:

- Tool models.
- Tool contracts.
- Tool implementations.
- ViewModels.
- Conversation memory.
- Dependency injection registrations.

### The Provider Only Translates

The OpenAI provider is responsible only for translation.

Its responsibilities are limited to:

- Converting application tool definitions into OpenAI tool definitions.
- Translating provider tool calls into application tool invocations.
- Executing tools through the registry.
- Translating tool results back into the provider message format.
- Continuing the conversation after tool execution.

The provider must not:

- Execute concrete tools directly.
- Contain application business logic.
- Know about individual tool implementations.
- Become responsible for tool registration.

### ViewModels Never Execute Tools

`MainWindowViewModel` should continue to depend only upon:

```text
IConversationService
```

The ViewModel must never:

- Discover tools.
- Execute tools.
- Parse tool arguments.
- Handle provider tool calls.
- Reference OpenAI SDK types.

This preserves the MVVM architecture introduced in earlier prompts.

### Tools and Runtime Context Are Different

Tools represent active capabilities that the AI chooses to invoke while generating a response.

Examples include:

- retrieving the current date and time
- performing a calculation
- searching the prompt library
- querying Home Assistant

Runtime context represents passive information supplied to the AI before the conversation begins.

Examples include:

- current application version
- selected project
- selected prompt
- current operating system
- active environment

Runtime context injection remains out of scope for this prompt and will be introduced separately in Prompt 031a.

---

## High-Level Architecture

The architecture after completing Prompt 031 should resemble the following.

```text
                    Avalonia UI
                         │
                         ▼
              MainWindowViewModel
                         │
                         ▼
             IConversationService
                         │
                         ▼
        OpenAIConversationService
                         │
                         ▼
                  IToolRegistry
                         │
        ┌────────────────┼────────────────┐
        ▼                ▼                ▼
CurrentDateTimeTool  CalculatorTool  ApplicationInfoTool
```

The ViewModel remains unaware of individual tools.

The OpenAI provider remains unaware of individual tool implementations.

The registry becomes the single entry point for application-owned capabilities.

Future prompts will extend the registry by registering additional `ITool` implementations rather than modifying the conversation service.

This keeps the architecture open for extension while remaining closed for modification.

---

## Provider-Independent Tool Models

Prompt 031 introduces three core models.

### ToolDefinition

Represents metadata describing a tool that can be exposed to an AI provider.

Suggested properties:

```csharp
public sealed class ToolDefinition
{
    public required string Name { get; init; }

    public required string Description { get; init; }

    public required string ParametersJsonSchema { get; init; }
}
```

The parameter schema should remain provider-independent.

JSON Schema should be treated as application data rather than an OpenAI-specific construct.

### ToolInvocation

Represents a request from the AI provider to execute a tool.

Suggested properties:

```csharp
public sealed class ToolInvocation
{
    public required string CallId { get; init; }

    public required string ToolName { get; init; }

    public required string ArgumentsJson { get; init; }
}
```

The OpenAI provider should translate its own SDK types into this model before execution.

### ToolResult

Represents the outcome of a tool execution.

Suggested properties:

```csharp
public sealed class ToolResult
{
    public required string CallId { get; init; }

    public required string ToolName { get; init; }

    public required string Content { get; init; }

    public bool IsSuccess { get; init; }
}
```

Tool implementations should return controlled success or failure results rather than throwing exceptions wherever practical.

---

## Tool Contracts

### ITool

Each tool should expose its definition and execute a provider-independent invocation.

Suggested contract:

```csharp
public interface ITool
{
    ToolDefinition Definition { get; }

    Task<ToolResult> ExecuteAsync(
        ToolInvocation invocation,
        CancellationToken cancellationToken);
}
```

Each concrete tool is responsible for:

- exposing its own metadata
- validating its own arguments
- executing its own behaviour
- returning a controlled result

### IToolRegistry

The registry provides discovery and execution.

Suggested contract:

```csharp
public interface IToolRegistry
{
    IReadOnlyCollection<ToolDefinition> GetDefinitions();

    Task<ToolResult> ExecuteAsync(
        ToolInvocation invocation,
        CancellationToken cancellationToken);
}
```

The registry should never contain tool-specific business logic.

Instead, it should resolve registered tools through dependency injection and delegate execution to the selected implementation.

---

## Tool Registry Behaviour

The tool registry is responsible for discovering and executing all application-owned tools.

It provides a single provider-independent entry point for the AI conversation service.

The registry should:

- Receive registered `ITool` implementations through dependency injection.
- Discover all available tools during application startup.
- Expose the available tool definitions.
- Resolve tools using a stable, case-sensitive tool name.
- Execute the requested tool.
- Pass cancellation tokens to the executing tool.
- Return controlled success or failure results.
- Remain completely independent of any AI provider.

The registry must not:

- Reference OpenAI SDK types.
- Contain application business logic.
- Parse provider-specific messages.
- Contain hard-coded `if` or `switch` statements for individual tools.

Tool discovery should be entirely data-driven.

Duplicate tool names should be detected during registry construction and treated as a configuration error.

Unknown tools should return a controlled failure result rather than throwing an unhandled exception.

Stable tool names should use lowercase with underscores.

Examples:

```text
get_current_date_time
calculate
get_application_info
```

---

## Initial Built-In Tools

Prompt 031 intentionally introduces only a very small set of deterministic tools.

These tools are designed to validate the architecture rather than provide significant desktop functionality.

Future prompts will build upon this foundation.

### Current Date and Time Tool

Suggested tool name:

```text
get_current_date_time
```

Purpose:

- Return the current local date.
- Return the current local time.
- Return the current UTC offset.
- Require no input parameters.

Suggested JSON result:

```json
{
  "localDateTime": "2026-07-10T19:30:00+01:00",
  "utcOffset": "+01:00"
}
```

If practical, the implementation should use an injectable clock abstraction to improve unit testability.

---

### Calculator Tool

Suggested tool name:

```text
calculate
```

Purpose:

- Demonstrate structured tool arguments.
- Demonstrate provider-independent execution.
- Demonstrate controlled validation failures.

Supported operations:

- add
- subtract
- multiply
- divide

Suggested parameter schema:

```json
{
  "type": "object",
  "properties": {
    "left": {
      "type": "number"
    },
    "right": {
      "type": "number"
    },
    "operation": {
      "type": "string",
      "enum": [
        "add",
        "subtract",
        "multiply",
        "divide"
      ]
    }
  },
  "required": [
    "left",
    "right",
    "operation"
  ],
  "additionalProperties": false
}
```

The calculator tool should:

- Reject invalid JSON.
- Reject missing parameters.
- Reject unsupported operations.
- Return a controlled failure for division by zero.
- Never evaluate arbitrary mathematical expressions.
- Never compile or execute user-supplied code.

---

### Application Information Tool

Suggested tool name:

```text
get_application_info
```

Purpose:

Return stable information describing the running Avalonia application.

Suggested JSON result:

```json
{
  "name": "Kryten Assist",
  "client": "Avalonia",
  "mode": "Desktop"
}
```

The implementation may include an application version if it can be obtained consistently.

It must not inspect:

- the local machine
- installed software
- environment variables
- the filesystem
- active projects

Those capabilities belong in future prompts.

---

## Dependency Injection

Register the tool framework through the Avalonia composition root.

Suggested registrations:

```csharp
services.AddSingleton<IToolRegistry, ToolRegistry>();

services.AddSingleton<ITool, CurrentDateTimeTool>();

services.AddSingleton<ITool, CalculatorTool>();

services.AddSingleton<ITool, ApplicationInfoTool>();
```

`OpenAIConversationService` should depend only upon `IToolRegistry`.

The ViewModel should remain completely unaware of individual tools.

This preserves the provider-independent architecture introduced throughout the AI integration prompts.


---

## Conversation Flow

The OpenAI conversation service must support a tool-call loop.

A normal conversation currently follows this flow:

```text
User Message
      │
      ▼
OpenAI Request
      │
      ▼
Assistant Response
```

After Prompt 031, the conversation may instead follow this flow:

```text
User Message
      │
      ▼
OpenAI Request
      │
      ▼
Tool Call Requested
      │
      ▼
Translate to ToolInvocation
      │
      ▼
IToolRegistry.ExecuteAsync
      │
      ▼
ToolResult
      │
      ▼
Translate to Provider Tool Message
      │
      ▼
Continue OpenAI Conversation
      │
      ▼
Final Assistant Response
```

The provider service should continue this loop until one of the following occurs:

- A normal assistant response is produced.
- The operation is cancelled.
- A provider error occurs.
- The maximum tool-iteration limit is reached.

---

## Normal Response Behaviour

When OpenAI returns a normal assistant response:

- Return the response through the existing conversation service flow.
- Preserve streaming behaviour where compatible.
- Commit the successful user and assistant messages to conversation memory.
- Do not expose provider-specific response objects to the ViewModel.

The existing Prompt 030 behaviour should remain unchanged when no tool is requested.

---

## Tool Call Behaviour

When OpenAI returns one or more tool calls:

1. Preserve the assistant tool-call message in the provider conversation history.
2. Translate each provider tool call into a provider-independent `ToolInvocation`.
3. Execute each invocation through `IToolRegistry`.
4. Translate each `ToolResult` into the provider's tool-result message format.
5. Add the tool-result messages to the provider conversation history.
6. Send the updated conversation back to OpenAI.
7. Continue until a normal assistant response is produced.

The OpenAI provider must not execute concrete tools directly.

It should only communicate with the tool system through:

```csharp
IToolRegistry
```

The first implementation may execute multiple tool calls sequentially.

Parallel tool execution is out of scope for this prompt.

---

## Tool Execution Loop

The provider implementation should use a bounded loop.

Suggested behaviour:

```text
Build provider request
        │
        ▼
Send request
        │
        ▼
Tool calls returned?
   │                │
   No               Yes
   │                │
   ▼                ▼
Return final     Execute tools
response             │
                     ▼
              Add tool results
                     │
                     ▼
              Send next request
```

The loop should not modify application conversation memory during intermediate tool calls.

Only the completed interaction should be committed.

The provider request history may temporarily contain:

- User messages.
- Assistant messages.
- Assistant tool-call messages.
- Tool-result messages.

Application conversation memory should remain simpler and provider-independent.

---

## Maximum Tool Iterations

Add a configurable maximum tool-iteration limit.

Suggested default:

```text
5
```

Suggested configuration:

```json
{
  "Conversation": {
    "MaxContextMessages": 20,
    "MaxToolIterations": 5
  }
}
```

The existing conversation-options class should be extended where practical.

Validate that:

```text
MaxToolIterations > 0
```

The iteration count should increase whenever the provider returns a new tool-call response.

If the maximum is exceeded:

- Stop the conversation loop.
- Return a controlled failure.
- Do not continue making provider requests.
- Do not commit the failed conversation to memory.
- Do not expose provider internals or stack traces to the user.

This protects the application from accidental infinite loops caused by repeated tool requests.

---

## Conversation Memory Behaviour

Prompt 030 memory rules must remain intact.

### Successful Tool-Assisted Conversation

When the complete interaction succeeds, store only:

```text
User Message

Final Assistant Response
```

Do not store:

- Provider tool-call messages.
- Raw tool arguments.
- Raw tool results.
- Provider-specific message objects.
- Intermediate assistant messages used only for tool execution.

This keeps conversation memory concise and provider-independent.

### Failed Conversation

If the interaction fails before producing a final successful assistant response:

- Do not commit the user message.
- Do not commit partial assistant output.
- Do not commit tool-call messages.
- Do not commit tool results.

The conversation should behave like the failed requests introduced in Prompt 030.

### Controlled Tool Failure

A tool may return:

```text
IsSuccess = false
```

The failure result may still be returned to the AI provider.

This allows the provider to produce a natural response such as:

```text
I could not complete that calculation because division by zero is not allowed.
```

If the provider then returns a valid final assistant response, the completed user and assistant messages may be committed to memory.

If the provider conversation fails completely, nothing should be committed.

---

## Cancellation Behaviour

Cancellation must propagate through the complete tool-call pipeline.

The cancellation token should be passed through:

- `MainWindowViewModel`
- `IConversationService`
- `OpenAIConversationService`
- Provider requests
- `IToolRegistry`
- `ITool`
- Tool implementations

When cancellation is requested:

- Stop issuing provider requests.
- Stop the tool-execution loop.
- Stop tool execution where possible.
- Do not commit the cancelled conversation to memory.
- Preserve the existing UI cancellation behaviour.

`OperationCanceledException` should continue to represent cancellation.

It should not be converted into an ordinary failed `ToolResult`.

---

## Streaming Behaviour

Prompt 029 introduced streaming assistant responses.

Tool calling adds a new complication because a provider may need to complete a tool call before a final natural-language response can be streamed.

The implementation should preserve streaming where supported, but correctness is more important than forcing every intermediate provider response through the streaming UI.

Acceptable foundation behaviour:

- Do not display raw tool-call fragments.
- Do not display raw JSON arguments.
- Do not display raw tool-result JSON as the final response.
- Resume normal assistant streaming once the provider produces final text.
- Keep the ViewModel unaware of provider-specific tool events.

Streaming tool-activity indicators are deferred to Prompt 031b.

---

## Configuration

Extend the existing conversation configuration.

Suggested shape:

```csharp
public sealed class ConversationOptions
{
    public int MaxContextMessages { get; init; } = 20;

    public int MaxToolIterations { get; init; } = 5;
}
```

The exact implementation may differ if the existing options class uses another pattern.

Configuration validation should ensure:

- `MaxContextMessages` remains valid.
- `MaxToolIterations` is greater than zero.

Do not add:

- Per-tool configuration.
- Per-tool permissions.
- Per-tool timeouts.
- Tool enable or disable settings.

Those capabilities belong in later prompts.

---

## Error Handling

Tool errors must be controlled and must not crash the Avalonia client.

Handle at least:

- Unknown tool name.
- Duplicate registered tool name.
- Invalid argument JSON.
- Missing required arguments.
- Unsupported calculator operation.
- Division by zero.
- Tool execution exception.
- Provider request failure.
- Malformed provider tool-call data.
- Empty tool-call identifier.
- Empty tool name.
- Maximum tool-iteration limit exceeded.
- Cancellation.

### Unknown Tool

When the registry cannot find a requested tool:

- Return a controlled failed `ToolResult`.
- Preserve the original call identifier.
- Include a readable failure message.
- Do not throw an unhandled exception.

### Invalid Arguments

When a tool receives invalid arguments:

- Return a controlled failed `ToolResult`.
- Explain the validation problem clearly.
- Do not expose parser stack traces.
- Do not allow the tool to continue with partial or unsafe input.

### Unexpected Tool Exception

When a tool throws an unexpected exception:

- Catch it at an appropriate boundary.
- Log the exception using the existing logging approach.
- Return a controlled failure where practical.
- Do not expose stack traces or internal details to the provider or user.

### Provider Failure

When OpenAI fails during a tool-assisted conversation:

- Preserve the existing provider error handling.
- Do not commit partial memory.
- Keep the user-facing message readable.
- Do not expose API keys, request headers or raw provider payloads.

### User-Facing Error Safety

User-facing errors must not expose:

- API keys.
- Secrets.
- Stack traces.
- Internal file paths.
- Full provider payloads.
- Sensitive configuration.
- Environment variables.

The application should provide enough information to understand the failure without leaking implementation details.


---

## Suggested File Structure

Adapt the exact folders to the current Avalonia project where needed.

A likely structure is:

```text
KrytenAssist.Avalonia/
├── Models/
│   └── Tools/
│       ├── ToolDefinition.cs
│       ├── ToolInvocation.cs
│       └── ToolResult.cs
├── Options/
│   └── ConversationOptions.cs
├── Services/
│   ├── IConversationService.cs
│   ├── ITool.cs
│   ├── IToolRegistry.cs
│   ├── OpenAIConversationService.cs
│   └── ToolRegistry.cs
└── Tools/
    ├── ApplicationInfoTool.cs
    ├── CalculatorTool.cs
    └── CurrentDateTimeTool.cs
```

The exact namespace arrangement may differ if the existing project already has a clearer structure.

The important requirement is architectural separation:

- Provider-independent models remain outside the OpenAI implementation.
- Tool contracts remain provider-independent.
- Concrete tools remain isolated from provider SDK types.
- The provider owns translation only.
- The ViewModel remains unaware of tools.

---

## Implementation Steps

### Step 1 of 14 – Add Tool Models

Create the provider-independent tool models:

- `ToolDefinition`
- `ToolInvocation`
- `ToolResult`

Requirements:

- Use application-owned types only.
- Do not reference the OpenAI SDK.
- Use required properties where appropriate.
- Keep the models immutable after construction where practical.
- Follow the existing project naming and namespace conventions.

Suggested responsibilities:

#### ToolDefinition

Stores:

- Stable tool name.
- Clear description.
- Provider-independent parameter schema.

#### ToolInvocation

Stores:

- Provider call identifier.
- Requested tool name.
- Raw JSON arguments.

#### ToolResult

Stores:

- Original call identifier.
- Tool name.
- Result content.
- Success or failure state.

Validate with:

```bash
dotnet build
```

---

### Step 2 of 14 – Add Tool Contracts

Create:

- `ITool`
- `IToolRegistry`

Requirements:

- Use only provider-independent models.
- Accept a `CancellationToken`.
- Return controlled `ToolResult` values.
- Avoid references to concrete tool implementations.
- Avoid references to OpenAI SDK types.

Suggested `ITool` shape:

```csharp
public interface ITool
{
    ToolDefinition Definition { get; }

    Task<ToolResult> ExecuteAsync(
        ToolInvocation invocation,
        CancellationToken cancellationToken);
}
```

Suggested `IToolRegistry` shape:

```csharp
public interface IToolRegistry
{
    IReadOnlyCollection<ToolDefinition> GetDefinitions();

    Task<ToolResult> ExecuteAsync(
        ToolInvocation invocation,
        CancellationToken cancellationToken);
}
```

Validate with:

```bash
dotnet build
```

---

### Step 3 of 14 – Implement ToolRegistry

Create a concrete `ToolRegistry`.

The registry should receive:

```csharp
IEnumerable<ITool>
```

through dependency injection.

Requirements:

- Build a lookup from registered tools.
- Use stable, case-sensitive tool names.
- Reject duplicate tool names.
- Expose all registered definitions.
- Execute tools by name.
- Return a controlled failed result for unknown tools.
- Pass cancellation tokens to tool implementations.
- Remain completely provider-independent.

Duplicate names should be treated as a startup or construction error.

The registry must not contain tool-specific logic.

Avoid patterns such as:

```csharp
if (toolName == "calculate")
{
    // calculator logic
}
```

Validate with:

```bash
dotnet build
```

---

### Step 4 of 14 – Add CurrentDateTimeTool

Create `CurrentDateTimeTool`.

Suggested tool name:

```text
get_current_date_time
```

Requirements:

- Require no arguments.
- Return local date and time.
- Return the current UTC offset.
- Return structured JSON content.
- Use a clear tool description.
- Use a valid no-argument JSON schema.
- Support cancellation consistently.
- Use an injectable clock abstraction if practical.

Suggested result shape:

```json
{
  "localDateTime": "2026-07-10T19:30:00+01:00",
  "utcOffset": "+01:00"
}
```

Do not add:

- Geolocation.
- Timezone lookup services.
- Network calls.
- User-profile inspection.

Validate with:

```bash
dotnet build
```

---

### Step 5 of 14 – Add CalculatorTool

Create `CalculatorTool`.

Suggested tool name:

```text
calculate
```

Supported operations:

- `add`
- `subtract`
- `multiply`
- `divide`

Requirements:

- Parse JSON arguments safely.
- Validate all required properties.
- Reject unsupported operations.
- Return a controlled failure for division by zero.
- Return structured content.
- Avoid arbitrary expression evaluation.
- Avoid dynamic compilation.
- Avoid executing user-supplied code.

Suggested argument model:

```csharp
public sealed class CalculatorArguments
{
    public decimal Left { get; init; }

    public decimal Right { get; init; }

    public required string Operation { get; init; }
}
```

A separate private or internal argument type is acceptable if it keeps parsing clear.

Return controlled failures for:

- Invalid JSON.
- Missing operation.
- Missing operands.
- Unknown operation.
- Division by zero.

Validate with:

```bash
dotnet build
```

---

### Step 6 of 14 – Add ApplicationInfoTool

Create `ApplicationInfoTool`.

Suggested tool name:

```text
get_application_info
```

Requirements:

- Return stable information only.
- Include the application name.
- Identify the Avalonia desktop client.
- Identify desktop mode.
- Include the application version only if it can be retrieved safely and consistently.
- Avoid machine inspection.
- Avoid filesystem discovery.
- Avoid project discovery.
- Avoid environment-variable enumeration.

Suggested result shape:

```json
{
  "name": "Kryten Assist",
  "client": "Avalonia",
  "mode": "Desktop"
}
```

Validate with:

```bash
dotnet build
```

---

### Step 7 of 14 – Register the Tool Framework

Update the Avalonia composition root.

Register:

```csharp
services.AddSingleton<IToolRegistry, ToolRegistry>();

services.AddSingleton<ITool, CurrentDateTimeTool>();
services.AddSingleton<ITool, CalculatorTool>();
services.AddSingleton<ITool, ApplicationInfoTool>();
```

Requirements:

- Keep registration in the existing composition root.
- Avoid manual service-location.
- Ensure all tools are discovered automatically.
- Avoid registering concrete tools directly into the provider.
- Preserve the existing conversation-service registration.

`OpenAIConversationService` should depend on:

```csharp
IToolRegistry
```

It should not depend on:

```csharp
CurrentDateTimeTool
CalculatorTool
ApplicationInfoTool
```

Validate with:

```bash
dotnet build
```

---

### Step 8 of 14 – Add Tool Iteration Configuration

Extend the existing conversation configuration.

Add:

```csharp
public int MaxToolIterations { get; init; } = 5;
```

Requirements:

- Reuse the existing options class where practical.
- Keep the default small and safe.
- Validate that the value is greater than zero.
- Preserve the existing `MaxContextMessages` behaviour.
- Keep configuration provider-independent.

Suggested configuration:

```json
{
  "Conversation": {
    "MaxContextMessages": 20,
    "MaxToolIterations": 5
  }
}
```

Do not add:

- Per-tool timeouts.
- Per-tool enable or disable settings.
- Per-tool permissions.
- Tool categories.

Validate with:

```bash
dotnet build
```

---

### Step 9 of 14 – Expose Tool Definitions to OpenAI

Update `OpenAIConversationService`.

The provider should:

- Request all available definitions from `IToolRegistry`.
- Translate each `ToolDefinition` into the equivalent OpenAI SDK type.
- Include those definitions in the provider request.
- Preserve the existing conversation-message translation.
- Keep all OpenAI SDK types inside the provider implementation.

Requirements:

- Do not modify tool models to match OpenAI types.
- Do not expose OpenAI types outside the provider service.
- Preserve ordinary text-only conversations.
- Preserve existing streaming behaviour where compatible.

The OpenAI service should remain able to work when the registry contains no tools.

Validate with:

```bash
dotnet build
```

---

### Step 10 of 14 – Execute Provider Tool Calls

Extend `OpenAIConversationService` to detect tool-call responses.

For each provider tool call:

1. Read the provider call identifier.
2. Read the requested tool name.
3. Read the raw argument JSON.
4. Create a provider-independent `ToolInvocation`.
5. Execute it through `IToolRegistry`.
6. Convert the `ToolResult` into an OpenAI tool-result message.
7. Continue the provider conversation.

Requirements:

- Preserve the provider assistant tool-call message.
- Preserve call identifiers correctly.
- Execute multiple tool calls sequentially.
- Enforce `MaxToolIterations`.
- Stop when a normal assistant response is produced.
- Avoid exposing raw tool-call content to the ViewModel.
- Avoid committing intermediate tool messages to application memory.

Validate with:

```bash
dotnet build
```

---

### Step 11 of 14 – Preserve Memory and Cancellation Behaviour

Confirm that Prompt 030 behaviour remains correct.

Successful tool-assisted conversations should commit only:

```text
User Message

Final Assistant Response
```

Do not commit:

- Assistant tool-call messages.
- Raw tool arguments.
- Tool-result messages.
- Partial assistant output.
- Provider-specific message objects.

Cancellation must propagate through:

- `MainWindowViewModel`
- `IConversationService`
- `OpenAIConversationService`
- Provider requests
- `IToolRegistry`
- Concrete tools

Cancelled interactions must not be committed to memory.

`OperationCanceledException` should remain cancellation and should not become an ordinary failed tool result.

Validate with:

```bash
dotnet build
```

---

### Step 12 of 14 – Add Focused Tests

Add focused automated tests where practical.

Test at least:

#### Tool Registry

- Returns registered definitions.
- Executes a known tool.
- Returns a controlled failure for an unknown tool.
- Rejects duplicate tool names.
- Passes cancellation tokens through.

#### Calculator Tool

- Adds values correctly.
- Subtracts values correctly.
- Multiplies values correctly.
- Divides values correctly.
- Rejects invalid JSON.
- Rejects missing arguments.
- Rejects unsupported operations.
- Rejects division by zero.

#### Other Tools

- Current date and time returns structured content.
- Application information returns expected stable values.

#### Conversation Service

Where practical, verify:

- Tool definitions are added to provider requests.
- Tool calls are executed through the registry.
- Tool results are returned to the provider.
- Maximum tool iterations are enforced.
- Final responses are returned after tool execution.
- Failed or cancelled interactions do not commit memory.

Do not introduce a large new testing architecture solely for this prompt.

Validate with:

```bash
dotnet test
```

---

### Step 13 of 14 – Live Verification

Run the Avalonia client with the configured OpenAI API.

Verify the following prompts.

#### Current Date and Time

```text
What is the current local date and time?
```

Expected:

- OpenAI requests `get_current_date_time`.
- The registry executes the tool.
- The final assistant response is natural language.

#### Calculator

```text
What is 125 multiplied by 48?
```

Expected:

- OpenAI requests `calculate`.
- Structured arguments are supplied.
- The correct result is returned.
- Raw JSON is not shown as the final response.

#### Application Information

```text
What application am I currently using?
```

Expected:

- OpenAI requests `get_application_info`.
- The assistant identifies Kryten Assist and the Avalonia desktop client.

#### Multi-Turn Memory

Ask:

```text
What is 12 multiplied by 12?
```

Then ask:

```text
Add 10 to that result.
```

Expected:

- Prompt 030 memory provides the previous conversational context.
- The calculator tool is invoked again.
- The final result is correct.

#### Cancellation

Start a request and cancel it while the provider request or tool loop is active.

Expected:

- The operation stops cleanly.
- The application remains responsive.
- No partial conversation is committed.

---

### Step 14 of 14 – Final Validation

Run:

```bash
dotnet build
```

Run:

```bash
dotnet test
```

Review:

```bash
git status
```

Confirm:

- Only intended files changed.
- Existing warnings remain understood.
- No OpenAI SDK types leaked into provider-independent layers.
- The ViewModel remains unaware of tools.
- Existing non-tool conversations still work.
- Tool-assisted conversations work.
- Memory and cancellation behaviour remain correct.

---

## Acceptance Criteria

Prompt 031 is complete when:

- Provider-independent tool models exist.
- `ITool` exists.
- `IToolRegistry` exists.
- A concrete `ToolRegistry` is registered through dependency injection.
- Registered tool definitions can be discovered through the registry.
- Duplicate tool names are rejected.
- Unknown tool names return controlled failure results.
- `CurrentDateTimeTool` works.
- `CalculatorTool` works.
- `ApplicationInfoTool` works.
- OpenAI receives the registered tool definitions.
- OpenAI tool calls are translated into provider-independent `ToolInvocation` objects.
- Tools execute through `IToolRegistry`.
- Tool results are translated back into provider tool-result messages.
- The provider conversation continues until a final assistant response is produced.
- Maximum tool iterations prevent infinite loops.
- Cancellation propagates through provider requests and tool execution.
- Failed conversations are not committed to memory.
- Cancelled conversations are not committed to memory.
- Successful tool-assisted conversations preserve Prompt 030 memory behaviour.
- Tool-call messages are not stored in application conversation memory.
- Tool-result messages are not stored in application conversation memory.
- No OpenAI SDK types leak into tool models, contracts, implementations or ViewModels.
- Existing text-only conversations continue to work.
- Existing streaming behaviour remains usable.
- The solution builds successfully.
- Existing tests continue to pass.
- New focused tests pass.
- Live OpenAI verification succeeds.
- Results and lessons learnt are documented.

---

## Manual Test Scenarios

### Scenario 1 – Current Date and Time

Ask:

```text
What is the current local date and time?
```

Expected:

- OpenAI requests `get_current_date_time`.
- The request is translated into a `ToolInvocation`.
- `IToolRegistry` resolves the correct tool.
- The tool returns structured JSON.
- OpenAI produces a natural-language final response.
- Raw tool JSON is not displayed as the final assistant response.

---

### Scenario 2 – Calculator Multiplication

Ask:

```text
What is 125 multiplied by 48?
```

Expected:

- OpenAI requests `calculate`.
- The arguments contain:
    - `left = 125`
    - `right = 48`
    - `operation = multiply`
- The tool returns the correct result.
- OpenAI presents the result naturally.

---

### Scenario 3 – Calculator Division

Ask:

```text
What is 875 divided by 7?
```

Expected:

- OpenAI requests `calculate`.
- The result is correct.
- No arbitrary expression evaluation is used.
- The final response is natural language.

---

### Scenario 4 – Application Information

Ask:

```text
What application am I currently using?
```

Expected:

- OpenAI requests `get_application_info`.
- The result identifies:
    - Kryten Assist
    - Avalonia
    - Desktop mode
- The assistant explains the result naturally.

---

### Scenario 5 – Multi-Turn Memory

Ask:

```text
What is 12 multiplied by 12?
```

Then ask:

```text
Add 10 to that result.
```

Expected:

- Prompt 030 memory provides the previous conversational context.
- The second request resolves the earlier result correctly.
- The calculator tool is invoked again.
- The final answer is correct.
- Only the user messages and final assistant messages are stored in application memory.

---

### Scenario 6 – Division by Zero

Ask or simulate:

```text
Divide 25 by zero.
```

Expected:

- `CalculatorTool` returns a controlled failure.
- The application does not crash.
- No stack trace is shown.
- OpenAI may explain that division by zero is not allowed.
- The final assistant response remains readable.

---

### Scenario 7 – Invalid Calculator Arguments

Use a focused test or simulated provider call with:

- Invalid JSON.
- Missing operands.
- Missing operation.
- Unsupported operation.

Expected:

- The calculator returns a controlled failed `ToolResult`.
- The tool does not execute with partial input.
- Parser details are not exposed to the user.
- The application remains stable.

---

### Scenario 8 – Unknown Tool

Use a focused test or simulated provider call requesting:

```text
unknown_tool
```

Expected:

- `ToolRegistry` returns a controlled failure.
- The original call identifier is preserved.
- No unhandled exception is thrown.
- The provider may explain that the capability is unavailable.

---

### Scenario 9 – Duplicate Tool Registration

Register two tools with the same stable name in a focused test.

Expected:

- Registry construction fails clearly.
- The duplicate name is identified as a configuration error.
- Tool execution does not continue with ambiguous registration.

---

### Scenario 10 – Cancellation

Start a conversation and cancel it while:

- The provider request is active.
- The tool loop is active.
- A tool is executing.

Expected:

- Cancellation propagates.
- The operation stops cleanly.
- The application remains responsive.
- `OperationCanceledException` remains cancellation.
- No partial conversation is committed to memory.

---

### Scenario 11 – Maximum Tool Iterations

Simulate repeated tool-call responses beyond:

```text
MaxToolIterations
```

Expected:

- The provider service stops after the configured limit.
- No further provider request is issued.
- A controlled failure is returned.
- The application does not loop indefinitely.
- The failed conversation is not committed to memory.

---

### Scenario 12 – Clear Conversation

Complete a successful tool-assisted conversation.

Then clear the conversation and ask a follow-up question that depends on the earlier result.

Expected:

- The previous context is no longer available.
- Prompt 030 clear-conversation behaviour remains correct.
- Tool registration remains unaffected.

---

### Scenario 13 – Ordinary Text Conversation

Ask a question that does not require a tool.

Expected:

- The provider returns a normal assistant response.
- No tool is invoked.
- Existing conversation behaviour remains unchanged.
- Memory is committed as before.

---

### Scenario 14 – Streaming Final Response

Ask a question that requires a tool and then a natural-language explanation.

Expected:

- Raw tool-call fragments are not shown.
- Raw argument JSON is not shown.
- Raw tool-result JSON is not shown as the final response.
- Final assistant text streams where supported.
- The ViewModel remains unaware of provider-specific tool events.

---

## Security and Safety Notes

All tools introduced in this prompt must be:

- Deterministic.
- Read-only.
- Local.
- Non-destructive.
- Narrowly scoped.
- Explicitly registered.
- Provider-independent.

Do not introduce generic execution capabilities.

No tool in this prompt may accept arbitrary:

- Shell commands.
- Source code.
- File paths.
- URLs.
- SQL.
- Process names.
- Application names.
- System instructions.
- Environment-variable names.
- Network targets.

The calculator must not evaluate arbitrary expressions.

The application-information tool must not inspect the machine.

The current date and time tool must not perform network or location lookup.

Future capabilities involving files, processes, networks, desktop control or external systems require separate prompts with explicit:

- Permissions.
- Validation.
- Limits.
- User confirmation.
- Logging.
- Error handling.
- Security review.

---

## Deferred Work

### Prompt 031a – Runtime Context Injection

Future work may inject passive structured context such as:

- Current operating system.
- Application version.
- Selected project.
- Selected prompt.
- Current working directory.
- Active environment.
- User-configured context.

Passive runtime context must remain separate from active tools.

The model should receive passive context automatically.

The model should request active capabilities through tools.

---

### Prompt 031b – Avalonia Desktop UX Refinements

Future work may improve:

- Conversation layout.
- Message presentation.
- Tool-activity indicators.
- Cancellation feedback.
- Clear-conversation confirmation.
- Empty states.
- Keyboard behaviour.
- Accessibility.
- Tool-status presentation.
- Streaming transitions around tool calls.

Prompt 031 should not add UI specifically for tool execution.

---

### Future Tool Prompts

Future prompts may add narrowly scoped tools for:

- Prompt-library search.
- Semantic search.
- Project awareness.
- Controlled local-file reading.
- Controlled file editing.
- Build execution.
- Test execution.
- Home Assistant queries.
- Desktop automation.
- MCP integration.
- External plugin discovery.
- External plugin loading.

Each future capability should be introduced separately with explicit scope, validation, permissions and safety rules.

---

## Expected Result

After completing Prompt 031, Kryten Assist will be able to hold a conversation in which the AI can request a small set of registered application tools, receive deterministic results, and use those results to produce a final natural-language response.

The tools framework will remain independent of OpenAI.

The ViewModel will remain unaware of individual tools.

The registry will become the single application-owned entry point for tool discovery and execution.

The initial tools will prove:

- No-argument tool execution.
- Structured argument handling.
- Controlled tool failures.
- Provider translation.
- Conversation continuation.
- Memory compatibility.
- Cancellation propagation.

Kryten Assist will have moved from a text-only AI conversation client to the first safe foundation of an actionable desktop assistant.

---

## Results

### Status

✅ Complete. The provider-independent Tool framework, three deterministic built-in
Tools and OpenAI tool-calling integration were implemented and committed.

---

### Files Created

- `KrytenAssist.Avalonia/Models/ToolDefinition.cs`
- `KrytenAssist.Avalonia/Models/ToolInvocation.cs`
- `KrytenAssist.Avalonia/Models/ToolResult.cs`
- `KrytenAssist.Avalonia/Services/ITool.cs`
- `KrytenAssist.Avalonia/Services/IToolRegistry.cs`
- `KrytenAssist.Avalonia/Services/ToolRegistry.cs`
- `KrytenAssist.Avalonia/Tools/ApplicationInfoTool.cs`
- `KrytenAssist.Avalonia/Tools/CalculatorTool.cs`
- `KrytenAssist.Avalonia/Tools/CurrentDateTimeTool.cs`
- `KrytenAssist.Avalonia/DependencyInjection/ToolServiceCollectionExtensions.cs`
- Tool tests under `KrytenAssist.Avalonia.Tests/Services/`

---

### Files Updated

- `KrytenAssist.Avalonia/Services/OpenAIConversationService.cs`
- `KrytenAssist.Avalonia/Options/ConversationOptions.cs`
- `KrytenAssist.Avalonia/Program.cs`
- `KrytenAssist.Avalonia/appsettings.json`
- project and solution files

---

### Build

✅ `dotnet build KrytenAssist.sln --no-restore`

Verified again on 16 July 2026: succeeded with 0 errors. Seven pre-existing
warnings remain: five `NU1903` SQLite package vulnerability warnings and two
unused command-event warnings.

---

### Tests

✅ `dotnet test KrytenAssist.sln --no-build --no-restore`

Verified again on 16 July 2026: 231 passed, 0 failed, 0 skipped.

---

### Live Verification

Tool calling was verified through the live OpenAI conversation provider during
the original implementation. The current documentation backfill did not repeat
an external provider request.

---

### Git Commit

`ed745af` – `031 - Tools`

---

### Lessons Learnt

- Tool contracts must be owned by Kryten rather than by an AI-provider SDK.
- Registry-driven dispatch keeps conversation orchestration independent of
  individual Tool implementations.
- Deterministic built-in Tools provide meaningful offline test coverage before
  live provider verification.
- Tool iteration limits and controlled failures are necessary to prevent an AI
  provider from creating unbounded execution loops.
- Tool results belong in the active provider interaction, while only successful
  conversational turns belong in conversation memory.

