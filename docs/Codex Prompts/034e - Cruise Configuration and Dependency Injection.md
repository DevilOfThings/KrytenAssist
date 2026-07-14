# Codex Prompt 034e – Cruise Configuration and Dependency Injection

## Source Prompt

Implement **Step 5 only** from:

```text
docs/AI Playbook/034 - Cruise of the Week Skill.md
```

Steps 1–4 have already been implemented.

Do not implement Steps 6 or 7.

---

## Goal

Configure and register the Marella Cruise of the Week provider and the `CruiseOfTheWeekSkill` through dependency injection.

This task should:

- introduce validated Marella provider options
- configure a named `HttpClient`
- register the parser and provider through a focused Infrastructure extension
- add the Infrastructure reference at the Avalonia composition root
- register `CruiseOfTheWeekSkill` as an `ISkill`
- add the public Marella URL and timeout to desktop configuration
- preserve the existing Skill Registry population mechanism

No tests, live HTTP request, persistence, UI, retries, caching or Prompt 034 verification should be added.

---

## Allowed Projects

Make implementation changes only inside:

```text
KrytenAssist.Infrastructure
KrytenAssist.Avalonia
```

The solution may be built from the repository root for verification.

Allowed files are limited to:

```text
KrytenAssist.Infrastructure/KrytenAssist.Infrastructure.csproj
KrytenAssist.Infrastructure/Cruises/Marella/MarellaCruiseOfTheWeekOptions.cs
KrytenAssist.Infrastructure/Cruises/Marella/MarellaCruiseServiceCollectionExtensions.cs
KrytenAssist.Avalonia/KrytenAssist.Avalonia.csproj
KrytenAssist.Avalonia/DependencyInjection/SkillServiceCollectionExtensions.cs
KrytenAssist.Avalonia/Program.cs
KrytenAssist.Avalonia/appsettings.json
```

Do not modify:

```text
KrytenAssist.Core
KrytenAssist.Core.Tests
KrytenAssist.Application
KrytenAssist.Api
KrytenAssist.Api.Tests
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

## Existing Types

Use the existing contracts and implementations:

```text
KrytenAssist.Application/Cruises/ICruiseOfTheWeekProvider.cs
KrytenAssist.Infrastructure/Cruises/Marella/MarellaCruiseOfTheWeekParser.cs
KrytenAssist.Infrastructure/Cruises/Marella/MarellaCruiseOfTheWeekProvider.cs
KrytenAssist.Avalonia/Skills/Cruises/CruiseOfTheWeekSkill.cs
KrytenAssist.Avalonia/Skills/Services/ISkill.cs
KrytenAssist.Avalonia/Skills/Services/ISkillRegistry.cs
KrytenAssist.Avalonia/Skills/Services/SkillRegistry.cs
```

Do not redesign or duplicate these types.

Do not modify `SkillRegistry` or `ISkillRegistry`.

---

## Infrastructure Package References

Add these package references to:

```text
KrytenAssist.Infrastructure/KrytenAssist.Infrastructure.csproj
```

```xml
<PackageReference Include="Microsoft.Extensions.Http" Version="10.0.9" />
<PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" Version="10.0.9" />
```

These packages support:

- `IHttpClientFactory`
- named `HttpClient` configuration
- options binding and validation

Do not add any other package.

Do not change the existing AngleSharp reference.

---

## Marella Options

Create:

```text
KrytenAssist.Infrastructure/Cruises/Marella/MarellaCruiseOfTheWeekOptions.cs
```

Use the namespace:

```csharp
KrytenAssist.Infrastructure.Cruises.Marella
```

Implement:

```csharp
public sealed class MarellaCruiseOfTheWeekOptions
```

Expose:

```csharp
public string SourceUrl { get; set; } = string.Empty;

public int TimeoutSeconds { get; set; }
```

The options model is configuration state and may use setters for binding.

Do not add provider credentials, cookies, API keys or booking details.

Do not place this options model in Core or Application.

---

## Infrastructure Registration Extension

Create:

```text
KrytenAssist.Infrastructure/Cruises/Marella/MarellaCruiseServiceCollectionExtensions.cs
```

Use the namespace:

```csharp
KrytenAssist.Infrastructure.Cruises.Marella
```

Implement a public static extension class with:

```csharp
public static IServiceCollection AddMarellaCruiseOfTheWeek(
    this IServiceCollection services,
    IConfiguration configuration)
```

Requirements:

- reject null `services`
- reject null `configuration`
- bind the supplied configuration object directly to `MarellaCruiseOfTheWeekOptions`
- validate options at startup
- register a named `HttpClient`
- register `MarellaCruiseOfTheWeekParser`
- register `ICruiseOfTheWeekProvider` using `MarellaCruiseOfTheWeekProvider`
- return the original `IServiceCollection`

The caller will pass the exact `CruiseOfTheWeek:Marella` configuration section.

Do not call `GetSection` inside Infrastructure.

---

## Options Validation

Use the standard options validation pipeline.

Validate on start.

### SourceUrl

Require:

- non-null, non-empty, non-whitespace value
- an absolute URI
- HTTPS scheme

Use a clear validation message.

Do not require a specific TUI hostname in the generic validation rule. The checked-in configuration supplies the intended Marella URL.

### TimeoutSeconds

Require:

```text
1 to 300 seconds inclusive
```

This ensures a positive finite timeout and prevents accidentally excessive configuration.

Use a clear validation message.

Do not silently replace invalid configured values with defaults.

---

## Named HttpClient

Use a private constant name inside the extension, for example:

```text
MarellaCruiseOfTheWeek
```

Register the named client through `AddHttpClient`.

Configure each client using validated `MarellaCruiseOfTheWeekOptions`:

```text
BaseAddress = configured SourceUrl
Timeout = configured TimeoutSeconds
User-Agent = KrytenAssist/0.1
```

Requirements:

- use `Uri` created from the validated absolute URL
- use `TimeSpan.FromSeconds`
- add a syntactically valid user-agent header
- do not add authentication
- do not add cookies
- do not add default query parameters
- do not add retry or resilience handlers
- do not customize redirects in this task

The HTTP request itself remains lazy. Registration must not perform a network call.

---

## Parser Lifetime

Register:

```text
MarellaCruiseOfTheWeekParser
```

as a singleton.

The parser is stateless and performs deterministic in-memory parsing.

Do not manually instantiate it in the composition root.

---

## Provider Lifetime

Register:

```text
ICruiseOfTheWeekProvider
```

as a singleton using a factory that:

1. resolves `IHttpClientFactory`
2. creates the configured named client
3. resolves the singleton parser
4. constructs `MarellaCruiseOfTheWeekProvider`

This lifetime aligns with the existing singleton Skill and singleton Skill Registry, avoiding an accidental captive transient provider.

The `HttpClient` created by `IHttpClientFactory` may be retained by the singleton provider; the factory continues to manage its underlying handler lifetime.

Do not register the concrete provider separately unless required by the existing factory pattern.

Do not instantiate an HTTP handler manually.

---

## Avalonia Infrastructure Reference

Add a project reference from:

```text
KrytenAssist.Avalonia
```

to:

```text
KrytenAssist.Infrastructure
```

Because the project contains the namespace `KrytenAssist.Application`, the existing Application project reference uses a compile-time alias to avoid colliding with Avalonia's `Application` class.

Use the same isolation pattern for Infrastructure:

```xml
<ProjectReference Include="..\KrytenAssist.Infrastructure\KrytenAssist.Infrastructure.csproj">
  <Aliases>KrytenInfrastructure</Aliases>
</ProjectReference>
```

Do not remove or change the existing `KrytenApplication` alias.

The Infrastructure reference is permitted only because `Program.cs` is the desktop composition root.

Do not reference Infrastructure from the Skill implementation or other Avalonia feature classes.

---

## Composition Root Registration

Update:

```text
KrytenAssist.Avalonia/Program.cs
```

Use:

```csharp
extern alias KrytenInfrastructure;
```

Create an alias for the focused Infrastructure extension class, for example:

```csharp
using MarellaCruiseServiceCollectionExtensions =
    KrytenInfrastructure::KrytenAssist.Infrastructure.Cruises.Marella
        .MarellaCruiseServiceCollectionExtensions;
```

Call the extension explicitly during service setup:

```csharp
MarellaCruiseServiceCollectionExtensions.AddMarellaCruiseOfTheWeek(
    services,
    configuration.GetSection("CruiseOfTheWeek:Marella"));
```

Register provider infrastructure before `services.AddSkills()`.

Do not call the broad API `AddInfrastructure()` extension because it also configures unrelated API persistence services.

Do not manually construct the parser, provider or `HttpClient` in `Program.cs`.

The registration must not execute the provider or make a startup HTTP request.

---

## Skill Registration

Update:

```text
KrytenAssist.Avalonia/DependencyInjection/SkillServiceCollectionExtensions.cs
```

Add:

```csharp
services.AddSingleton<ISkill, CruiseOfTheWeekSkill>();
```

Keep:

```csharp
services.AddSingleton<ISkill, EchoSkill>();
```

The existing registry factory should continue to:

- resolve all registered `ISkill` instances
- register those exact instances
- preserve registration order

Register `EchoSkill` first and `CruiseOfTheWeekSkill` second.

Do not change registry behavior or add reflection/assembly scanning.

Do not manually register the Skill with a resolved registry.

---

## Configuration

Update:

```text
KrytenAssist.Avalonia/appsettings.json
```

Add:

```json
"CruiseOfTheWeek": {
  "Marella": {
    "SourceUrl": "https://www.tui.co.uk/cruise/deals/marella-cruise-of-the-week",
    "TimeoutSeconds": 30
  }
}
```

Preserve all existing configuration sections and formatting conventions.

The URL is public configuration, not a secret.

Do not add:

- credentials
- cookies
- authentication tokens
- booking data
- environment-specific duplicates

---

## Resolution Graph

After registration, the dependency graph should be:

```text
ISkillRegistry (singleton)
└── CruiseOfTheWeekSkill as ISkill (singleton)
    └── ICruiseOfTheWeekProvider (singleton)
        ├── HttpClient from named IHttpClientFactory registration
        └── MarellaCruiseOfTheWeekParser (singleton)
```

The graph must resolve without making an HTTP request.

Do not resolve the Cruise Skill manually at startup solely to force validation or retrieval.

Standard `ValidateOnStart` options behavior is sufficient for configuration validation.

---

## Architecture Requirements

Preserve these boundaries:

- Application owns `ICruiseOfTheWeekProvider`
- Infrastructure owns Marella options, parser, HTTP provider and provider registration
- Avalonia owns the Skill and composition root
- Core owns only provider-independent Cruise models

Provider-specific types may appear in:

- Infrastructure
- the Avalonia composition root alias used to invoke Infrastructure registration

Provider-specific types must not appear in:

- Core
- Application contracts
- `CruiseOfTheWeekSkill`
- Skill Registry contracts

Do not add OpenAI or other AI-provider integration.

---

## Design Constraints

Keep registration:

- focused
- explicit
- lazy with respect to network access
- options validated
- constructor-injected
- aligned with existing extension-method conventions

Prefer:

- standard options binding
- named `HttpClient`
- intentional singleton lifetimes
- existing Skill registry population
- minimal composition-root wiring

Avoid:

- manual service construction in `Program.cs`
- service location outside DI factories
- automatic discovery
- reflection
- assembly scanning
- retry policies
- caching
- startup retrieval
- unrelated refactoring

Do not rename, move or reformat unrelated files.

---

## Explicitly Out of Scope

Do not implement:

- parser or provider redesign
- additional providers
- live HTTP requests
- startup retrieval
- retries
- caching
- persistence
- history
- comparison
- alerts
- background scheduling
- dashboards
- navigation
- Avalonia views or view models
- conversation-provider integration
- tests
- fake providers
- new test projects
- Prompt 034 Step 6 tests
- Prompt 034 Step 7 verification

---

## Verification

Before building, inspect the final diff and confirm implementation changes are limited to the allowed files.

From the repository root, run:

```bash
dotnet build
```

Do not call the live Marella website.

Do not start the application solely to test retrieval.

Do not make unrelated changes merely to remove pre-existing warnings.

The existing SQLite package vulnerability warnings and unrelated Avalonia command-event warnings should be reported but not addressed.

The task is successful when:

- provider options bind and validate on start
- named `HttpClient` uses the configured HTTPS URL, timeout and user agent
- parser and provider use intentional singleton lifetimes
- Avalonia references Infrastructure only through the composition-root alias
- focused Infrastructure registration is called before `AddSkills()`
- `CruiseOfTheWeekSkill` is registered after `EchoSkill`
- the existing registry factory remains unchanged
- the complete service graph compiles
- no HTTP request occurs during registration or build
- no later Prompt 034 behavior was added
- the solution builds successfully

---

## Completion Report

After implementation, report:

### Files Created

List every file created.

### Files Modified

List every existing file modified.

### Implementation Summary

Briefly describe:

- options and validation
- named `HttpClient` configuration
- parser and provider lifetimes
- composition-root registration
- Skill registration and registry population

### Package Changes

Report each package and version added and confirm it was added only to Infrastructure.

### Project Reference

Report the Avalonia-to-Infrastructure reference and its compile-time alias.

### Build

Report:

- command executed
- success or failure
- warning count
- error count

Distinguish pre-existing warnings from warnings introduced by this task.

### Tests

Confirm that no tests were added or modified and that all Prompt 034 tests remain reserved for Step 6.

### Scope Check

Confirm that:

- only Step 5 was implemented
- only allowed Infrastructure and Avalonia files were changed
- no live HTTP request occurred
- no parser, provider, Skill or registry redesign was performed
- no persistence or UI behavior was added
- no retries, caching or scheduling were added
- no tests were added or modified
- only the approved packages were added
- no documentation files were modified

