# Codex Prompt 034c – Marella HTTP Provider

## Source Prompt

Implement **Step 3 only** from:

```text
docs/AI Playbook/034 - Cruise of the Week Skill.md
```

Steps 1 and 2 have already been implemented.

Do not implement Steps 4–7.

---

## Goal

Implement the Marella HTTP adapter for the provider-independent `ICruiseOfTheWeekProvider` contract.

This task introduces:

```text
MarellaCruiseOfTheWeekProvider
```

The provider should retrieve HTML from the source configured on an injected `HttpClient`, pass that HTML to the existing deterministic Marella parser, and return the resulting `CruiseObservation`.

The provider must propagate caller cancellation and translate expected HTTP or timeout failures into the existing application-owned `CruiseOfTheWeekException`.

Do not add dependency-injection registration, options, configuration, Skills, tests, retries, persistence or UI.

---

## Allowed Project

Make implementation changes only inside:

```text
KrytenAssist.Infrastructure
```

The solution may be built from the repository root for verification.

Do not modify:

```text
KrytenAssist.Core
KrytenAssist.Core.Tests
KrytenAssist.Application
KrytenAssist.Api
KrytenAssist.Api.Tests
KrytenAssist.Avalonia
KrytenAssist.Avalonia.Tests
KrytenAssist.Client
KrytenAssist.sln
```

Do not add or modify test projects in this task.

Do not modify:

```text
docs/AI Playbook/034 - Cruise of the Week Skill.md
docs/Roadmap.md
docs/Backlog.md
docs/Session Handovers
```

Do not modify other Codex prompts or documentation files.

Robin will update project documentation after reviewing the implementation.

---

## Existing Contracts

Use the existing Application types:

```text
KrytenAssist.Application/Cruises/ICruiseOfTheWeekProvider.cs
KrytenAssist.Application/Cruises/CruiseOfTheWeekException.cs
```

Use the existing parser:

```text
KrytenAssist.Infrastructure/Cruises/Marella/MarellaCruiseOfTheWeekParser.cs
```

Do not duplicate, redesign or extend these contracts.

The parser owns all HTML interpretation and domain mapping. The HTTP provider must not contain selectors, regular expressions, text parsing or domain-construction logic.

---

## Provider Location

Create:

```text
KrytenAssist.Infrastructure/Cruises/Marella/MarellaCruiseOfTheWeekProvider.cs
```

Use the namespace:

```csharp
KrytenAssist.Infrastructure.Cruises.Marella
```

Implement:

```csharp
public sealed class MarellaCruiseOfTheWeekProvider
    : ICruiseOfTheWeekProvider
```

Do not expose a second provider-specific public interface.

---

## Constructor

Use constructor injection:

```csharp
public MarellaCruiseOfTheWeekProvider(
    HttpClient httpClient,
    MarellaCruiseOfTheWeekParser parser)
```

Requirements:

- reject a null `HttpClient`
- reject a null parser
- require `httpClient.BaseAddress` to be non-null
- require `BaseAddress` to be an absolute URI
- require the URI scheme to be HTTPS
- retain both supplied dependencies

Use normal .NET argument exceptions for invalid constructor configuration.

The exact Marella URL and timeout will be applied when the `HttpClient` is registered in Step 5.

Do not:

- manually instantiate `HttpClient`
- manually instantiate the parser
- accept `IServiceProvider`
- accept `IConfiguration`
- introduce an options class in this task
- hard-code the Marella URL in the provider
- use a static `HttpClient`

---

## Provider Contract

Implement the existing method exactly:

```csharp
public async Task<CruiseObservation> GetCurrentAsync(
    DateTimeOffset observedAt,
    CancellationToken cancellationToken = default)
```

The caller-supplied `observedAt` must be forwarded unchanged to the parser.

Do not read the system clock.

---

## HTTP Request

Use the injected `HttpClient` and its configured `BaseAddress`.

Issue one GET request to the base address using:

```text
HttpCompletionOption.ResponseHeadersRead
```

Pass the supplied `CancellationToken` to:

- the HTTP request
- response-body reading

Use an empty relative request URI or another minimal approach that resolves exactly to the configured `BaseAddress`.

Do not:

- append query parameters
- follow or construct booking links manually
- send credentials
- send cookies explicitly
- attempt myTUI login
- issue a HEAD request first
- issue multiple requests
- perform browser automation
- execute JavaScript

Automatic redirect behavior may remain at the standard `HttpClient` handler default configured by the composition root.

---

## HTTP Response

The provider must:

- dispose the response
- require a successful HTTP status code
- read the complete response body asynchronously
- reject an empty or whitespace-only response body with `CruiseOfTheWeekException`

Do not inspect, log or return the response body outside the parser.

Do not expose:

- `HttpResponseMessage`
- status codes
- response headers
- raw HTML

through the Application contract.

---

## Parser Invocation

Invoke:

```csharp
parser.Parse(
    html,
    observedAt,
    httpClient.BaseAddress.AbsoluteUri)
```

Requirements:

- pass the HTML without rewriting it
- pass the exact caller-supplied observation timestamp
- pass the configured absolute source URL
- return the parser's `CruiseObservation` unchanged

Do not reconstruct or enrich the domain result in the provider.

`CruiseOfTheWeekException` raised by the parser should propagate unchanged.

---

## Failure Handling

### Caller Cancellation

When the supplied `CancellationToken` is cancelled:

- allow `OperationCanceledException` to propagate
- do not wrap it
- do not convert it into `CruiseOfTheWeekException`

This includes cancellation during request or response-body reading.

### HTTP Timeout

An `OperationCanceledException` or `TaskCanceledException` caused by the configured `HttpClient` timeout when the caller's token was not cancelled should become:

```text
CruiseOfTheWeekException
```

Use a safe message such as:

```text
The Cruise of the Week request timed out.
```

Preserve the original exception as `InnerException`.

### HTTP Failure

Wrap `HttpRequestException` in `CruiseOfTheWeekException` with a safe message such as:

```text
The Cruise of the Week could not be retrieved.
```

Preserve the original exception as `InnerException`.

This includes non-success status handling produced by `EnsureSuccessStatusCode`.

### Empty Response

Throw `CruiseOfTheWeekException` with a safe message when the successful response contains no usable HTML.

### Parser Failure

Allow an existing `CruiseOfTheWeekException` from the parser to propagate unchanged.

Do not catch every `Exception`.

Programmer errors and unexpected failures should not be disguised as provider availability failures.

---

## Cancellation Ordering

Check cancellation before issuing the request so an already-cancelled token causes no HTTP call.

Continue to pass the token through every asynchronous operation.

Do not add retries, delays or cancellation-token linking in this task.

---

## Resource Lifetime

The provider does not own the injected `HttpClient` and must not dispose it.

The provider does own each `HttpResponseMessage` returned by its request and must dispose it promptly.

Do not create or dispose an HTTP handler inside the provider.

---

## Architecture Requirements

The provider may reference only:

- Base Class Library HTTP types
- `KrytenAssist.Application.Cruises`
- `KrytenAssist.Core.Cruises`
- the existing Marella parser

It must not reference:

- Avalonia
- OpenAI or another AI provider
- Skills framework types
- Entity Framework
- database types
- AngleSharp DOM types directly
- browser automation
- JavaScript engines

AngleSharp remains an implementation detail of the parser and should not appear in the provider file.

Do not add project references or NuGet packages.

---

## Design Constraints

Keep the provider:

- small
- asynchronous
- cancellation aware
- provider specific
- transport focused
- independently testable with a fake `HttpMessageHandler` in Step 6
- independent of UI and persistence

Prefer:

- constructor injection
- one request per execution
- controlled expected failures
- exact forwarding to the parser

Avoid:

- service location
- static state
- manual dependency construction
- parsing logic
- domain mapping
- logging raw HTML
- retries
- caching
- fallback data
- comments that merely repeat the code

Do not rename, move or reformat unrelated files.

---

## Explicitly Out of Scope

Do not implement:

- provider options
- dependency-injection registration
- typed-client registration
- configuration files
- user-agent configuration
- timeout configuration
- retry policies
- caching
- persistence
- history
- comparison
- alerts
- background scheduling
- `CruiseOfTheWeekSkill`
- `ISkill` integration
- UI
- tests
- fake HTTP handlers
- new test projects
- live Marella verification
- Steps 4–7 from Prompt 034

---

## Verification

Before building, inspect the final diff and confirm that the only implementation file created or modified by this task is:

```text
KrytenAssist.Infrastructure/Cruises/Marella/MarellaCruiseOfTheWeekProvider.cs
```

From the repository root, run:

```bash
dotnet build
```

Do not call the live Marella website.

Do not make unrelated changes merely to remove pre-existing warnings.

The existing SQLite package vulnerability warnings and unrelated Avalonia command-event warnings should be reported but not addressed.

The task is successful when:

- the provider implements `ICruiseOfTheWeekProvider`
- constructor dependencies and base address are validated
- one cancellable GET request is issued
- response-body reading is cancellable
- successful HTML is passed unchanged to the parser
- observation time and source reference are forwarded correctly
- parser output is returned unchanged
- caller cancellation propagates
- timeout and HTTP failures use the controlled exception
- empty HTML fails safely
- no DI, options, configuration or later behavior was added
- the solution builds successfully

---

## Completion Report

After implementation, report:

### Files Created

List every file created.

### Files Modified

List every existing file modified.

If none were modified, state:

```text
None
```

### Implementation Summary

Briefly describe:

- constructor validation
- HTTP request behavior
- parser forwarding
- cancellation behavior
- controlled failure translation
- resource disposal

### Build

Report:

- command executed
- success or failure
- warning count
- error count

Distinguish pre-existing warnings from warnings introduced by this task.

### Tests

Confirm that no tests were added or modified and that provider tests are reserved for Prompt 034 Step 6.

### Scope Check

Confirm that:

- only Step 3 was implemented
- only the Marella provider file was added
- the parser and existing contracts were not redesigned
- no dependency-injection, options or configuration changes were made
- no Skill was added
- no persistence or UI behavior was added
- no retries or caching were added
- no tests were added or modified
- no NuGet packages or project references were added
- no documentation files were modified

