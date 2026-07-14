# Codex Prompt 034b – Marella Cruise Parser

## Source Prompt

Implement **Step 2 only** from:

```text
docs/AI Playbook/034 - Cruise of the Week Skill.md
```

Step 1 has already been implemented.

Do not implement Steps 3–7.

---

## Goal

Implement a deterministic, network-free parser for Marella Cruises' Cruise of the Week HTML.

This task introduces:

- AngleSharp in `KrytenAssist.Infrastructure`
- `MarellaCruiseOfTheWeekParser`
- mapping from representative Marella HTML into the provider-independent Prompt 033 Cruise domain

The parser must accept HTML supplied by a caller and return a complete `CruiseObservation`.

It must not perform HTTP requests, read the system clock, persist results, register services, implement a Skill or add tests.

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

Use the existing Application contract:

```text
KrytenAssist.Application/Cruises/CruiseOfTheWeekException.cs
```

Use the existing Core domain:

```text
KrytenAssist.Core/Cruises/CruiseProvider.cs
KrytenAssist.Core/Cruises/CruisePrice.cs
KrytenAssist.Core/Cruises/CruiseOffer.cs
KrytenAssist.Core/Cruises/CruiseSnapshot.cs
KrytenAssist.Core/Cruises/CruiseObservation.cs
```

Do not duplicate, redesign or extend these contracts.

Do not implement `ICruiseOfTheWeekProvider` in this task. That is Step 3.

---

## AngleSharp Package

Add this package reference to:

```text
KrytenAssist.Infrastructure/KrytenAssist.Infrastructure.csproj
```

```xml
<PackageReference Include="AngleSharp" Version="1.5.2" />
```

Do not add:

- AngleSharp to Core or Application
- AngleSharp.Io
- browser automation packages
- JavaScript engines
- any other NuGet package

All AngleSharp types must remain inside Infrastructure.

---

## Parser Location

Create:

```text
KrytenAssist.Infrastructure/Cruises/Marella/MarellaCruiseOfTheWeekParser.cs
```

Use the namespace:

```csharp
KrytenAssist.Infrastructure.Cruises.Marella
```

Implement:

```csharp
public sealed class MarellaCruiseOfTheWeekParser
```

Expose one public parsing method:

```csharp
public CruiseObservation Parse(
    string html,
    DateTimeOffset observedAt,
    string sourceReference)
```

The method should be synchronous because it parses an in-memory string and performs no I/O.

Do not expose AngleSharp document or element types in the public method signature.

---

## Input Validation

The parser must:

- reject `null` HTML with `ArgumentNullException`
- reject empty or whitespace-only HTML with `ArgumentException`
- reject `null` source reference with `ArgumentNullException`
- reject empty or whitespace-only source reference with `ArgumentException`
- preserve the exact caller-supplied `DateTimeOffset`
- preserve the exact valid source reference

Do not validate the source reference as a URI in the parser.

Do not read:

- `DateTimeOffset.Now`
- `DateTimeOffset.UtcNow`
- `DateTime.Now`
- `DateTime.UtcNow`

---

## Marella Provider Identity

The parser is the provider-specific adapter and should create:

```csharp
new CruiseProvider(
    id: "marella",
    name: "Marella Cruises")
```

Keep these values private to the Marella implementation.

Do not add them to Core, Application or a shared provider enum.

---

## Document Parsing

Use AngleSharp's HTML parser to create an in-memory document from the supplied string.

Do not:

- load external resources
- enable scripting
- execute JavaScript
- create a browsing context with network loading
- follow links
- fetch images, stylesheets or scripts

Use AngleSharp only for standards-aware HTML document parsing and DOM traversal.

Normalize extracted human-readable text consistently by:

- decoding HTML entities through the parsed DOM
- collapsing repeated whitespace to a single space
- trimming leading and trailing whitespace

Do not change the case of user-facing extracted values.

---

## Weekly Deal Identification

Identify the dedicated weekly promotion heading semantically.

The current page uses wording similar to:

```text
This week's deal: Save an extra £100 on cruise "Highlights of the Mediterranean" with code CRUISE
```

Requirements:

- search heading elements rather than the entire raw HTML string
- locate exactly one heading whose normalized text starts with `This week's deal:` using case-insensitive comparison
- extract the quoted cruise title from that heading
- retain the complete normalized heading text as the promotion summary
- fail if no matching heading exists
- fail if more than one matching heading exists
- fail if the quoted title cannot be extracted unambiguously

Use a small focused regular expression only for extracting the quoted title from the already-selected heading text.

Do not hard-code the example title, saving amount or promotion code.

---

## Cruise Result Identification

After extracting the weekly cruise title, identify exactly one matching cruise-result heading elsewhere in the document.

Requirements:

- compare normalized heading text with the extracted title using case-insensitive equality
- exclude the weekly promotion heading itself
- identify a surrounding result container that includes the matching title and semantic cruise-result labels
- require the selected container to contain `Departure date and trip duration`
- require the selected container to contain at least one clearly labelled per-person price
- fail if no matching result can be selected
- fail if multiple valid matching results remain ambiguous

When walking ancestors, choose the nearest ancestor that contains the complete result contract rather than a broad page container that also includes other deals.

Do not simply select:

- the first heading on the page
- the first price on the page
- the first cruise card
- content under `Other cruise deals`

Keep any CSS selectors or semantic-label constants private and centralized inside the parser.

---

## Required Field Extraction

The selected result must provide:

- title
- ship name
- departure date
- duration in nights
- at least one unambiguous per-person price

If any required value is missing, malformed or ambiguous, throw `CruiseOfTheWeekException` with a safe message identifying the missing type of information.

Do not include raw HTML in exception messages.

Do not invent fallback values.

---

## Title

Use the exact normalized text of the selected cruise-result heading as `CruiseOffer.Title`.

Confirm that it matches the title extracted from the weekly promotion heading, ignoring case.

Preserve the result heading's display casing.

---

## Ship Name

Extract the ship name from the selected result region.

Prefer a dedicated semantic element or attribute when present.

When representative HTML supplies the ship as the first meaningful text element after the title and before departure information, that relative structure may be used.

Requirements:

- normalize whitespace
- require one non-empty value
- preserve display casing
- do not create a separate ship model
- do not infer a ship from the itinerary title

Fail safely if the ship cannot be identified unambiguously.

---

## Departure Port

Extract the departure port when the selected result publishes text in a form similar to:

```text
From Palma, Majorca on 27 Oct 2026
```

Requirements:

- extract only the port text between `From` and `on`
- normalize whitespace
- preserve punctuation and display casing
- treat the port as optional if the source does not publish it
- do not introduce a Port model

Use a focused regular expression on the already-selected departure text rather than the full document.

---

## Departure Date

Prefer the semantically labelled `Departure date and trip duration` value, currently shaped like:

```text
Tue 27 Oct 2026 - 7 nights
```

Parse the date using explicit English month rules and invariant culture.

Support an optional abbreviated weekday prefix.

Return `DateOnly`.

Requirements:

- do not use current culture implicitly
- do not use the system clock
- do not substitute another date from the page
- do not calculate departure from a return date
- fail with `CruiseOfTheWeekException` when invalid

---

## Duration

Extract a positive integer number of nights from the semantically labelled departure/duration value.

Support singular and plural source wording:

```text
1 night
7 nights
```

Requirements:

- require a value greater than zero
- do not calculate duration from dates
- do not accept unrelated `Other cruise deals` durations
- fail with `CruiseOfTheWeekException` when missing or invalid

---

## Price Extraction

Parse only prices inside the selected cruise-result container whose basis is clear.

### Required Per-Person Price

Recognize normalized text shaped like:

```text
£903 pp
£1,069pp
```

Map to:

```text
Currency: GBP
Basis: per person
```

Requirements:

- remove grouping commas before decimal parsing
- use invariant numeric parsing
- use `decimal`
- preserve the first clearly labelled current per-person price in source order
- fail if there is no clearly labelled per-person price
- fail if the selected region contains multiple competing current per-person prices that cannot be distinguished

### Optional Total Price

Recognize a clearly labelled value shaped like:

```text
£1805 Total price based on 2 sharing
```

Map to:

```text
Currency: GBP
Basis: total based on 2 sharing
```

Add it after the per-person price when present.

### Ambiguous Prices

Ignore unlabeled values that merely resemble prices, including:

- previous or struck-through prices without a clear current basis
- discount amounts inside promotion text
- values inside `Other cruise deals`
- unrelated recommended-deal prices

Do not calculate total from per-person price or vice versa.

Do not calculate or apply the promotional saving.

---

## Itinerary Summary

Set `CruiseOffer.ItinerarySummary` only when the selected result exposes a distinct source-neutral itinerary or destination summary.

Otherwise use `null`.

Do not copy:

- promotion wording
- board basis
- the title merely to fill the property
- departure or return text

---

## Provider Offer Identifier

Prefer a stable provider-supplied identifier from the selected result when available, such as:

- a dedicated data attribute
- a stable cruise identifier in the selected deal link

Do not use:

- DOM element positions
- randomized identifiers
- hash codes
- observation time
- current time

When no stable provider identifier is available, derive:

```text
marella:{normalized-title-slug}:{yyyy-MM-dd}
```

Slug rules:

- use invariant lowercase
- retain ASCII letters and digits
- replace each run of other characters with one hyphen
- trim leading and trailing hyphens
- require a non-empty result

Example format only:

```text
marella:highlights-of-the-mediterranean:2026-10-27
```

Do not hard-code the example identifier.

Keep derivation private to the Marella parser and deterministic.

---

## Domain Construction

Construct the domain in this order:

```text
CruiseProvider
    -> CruiseOffer
    -> CruisePrice collection
    -> CruiseSnapshot
    -> CruiseObservation
```

Map:

```text
Provider.Id = marella
Provider.Name = Marella Cruises
Offer.ProviderOfferId = extracted or deterministically derived identifier
Offer.Title = selected result title
Offer.ShipName = parsed ship
Offer.DepartureDate = parsed DateOnly
Offer.DurationNights = parsed positive duration
Offer.DeparturePort = parsed port or null
Offer.ItinerarySummary = distinct summary or null
Snapshot.Prices = per-person price, followed by optional total price
Snapshot.PromotionSummary = weekly promotion heading or null
Observation.ObservedAt = exact caller-supplied value
Observation.SourceReference = exact caller-supplied value
```

The promotion heading is required to identify the weekly cruise, so a successfully parsed result should normally retain it as `PromotionSummary`.

---

## Failure Handling

Throw `CruiseOfTheWeekException` for expected content failures such as:

- no weekly promotion heading
- multiple weekly promotion headings
- missing quoted title
- no matching result
- multiple ambiguous matching results
- missing ship
- invalid departure date
- invalid duration
- missing or ambiguous per-person price
- inability to derive a provider offer identifier

Messages should be concise and safe for eventual display by the Skill.

Do not include:

- raw HTML
- full page text
- stack traces
- cookies
- headers
- user information

Allow input guard exceptions to remain normal argument exceptions.

Do not catch every exception and convert programmer errors into content failures.

---

## Architecture Requirements

The parser may reference only:

- Base Class Library types
- AngleSharp
- `KrytenAssist.Application.Cruises`
- `KrytenAssist.Core.Cruises`

It must not reference:

- Avalonia
- OpenAI or another AI provider
- Skills framework types
- Entity Framework
- database types
- `HttpClient`
- HTTP response types
- browser automation
- JavaScript engines

Do not add project references. Infrastructure already references Application and Core.

---

## Design Constraints

Keep the parser:

- deterministic
- network free
- provider specific
- independently testable
- culture explicit
- cancellation independent because it performs only short in-memory parsing
- isolated from UI and persistence

Prefer:

- small private extraction methods
- centralized semantic selectors and labels
- focused regular expressions over extracted text
- explicit failure messages
- immutable domain output

Avoid:

- one large method
- one regular expression over the whole document
- global mutable state
- static mutable parser configuration
- silent fallback values
- duplicated whitespace normalization
- provider details outside the Marella namespace
- comments that merely repeat the code

Do not rename, move or reformat unrelated files.

---

## Explicitly Out of Scope

Do not implement:

- `MarellaCruiseOfTheWeekProvider`
- `ICruiseOfTheWeekProvider` implementation
- HTTP clients
- web requests
- provider options
- dependency-injection registration
- configuration
- retries
- caching
- persistence
- history
- comparison
- alerts
- background scheduling
- Cruise of the Week Skill
- `ISkill` integration
- UI
- tests
- test fixtures
- new test projects
- live Marella verification
- Steps 3–7 from Prompt 034

---

## Verification

Before building, inspect the final diff and confirm that implementation changes are limited to:

```text
KrytenAssist.Infrastructure/KrytenAssist.Infrastructure.csproj
KrytenAssist.Infrastructure/Cruises/Marella/MarellaCruiseOfTheWeekParser.cs
```

From the repository root, run:

```bash
dotnet build
```

Do not call the live Marella website.

Do not make unrelated changes merely to remove pre-existing warnings.

The existing SQLite package vulnerability warnings and unrelated Avalonia command-event warnings should be reported but not addressed.

The task is successful when:

- AngleSharp 1.5.2 is referenced only by Infrastructure
- the parser exposes the required provider-neutral public method
- the parser performs no network or clock access
- required values map into Prompt 033 models
- prices use unambiguous source basis
- provider identifier extraction or derivation is deterministic
- expected content failures use `CruiseOfTheWeekException`
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

- weekly deal and result identification
- required field extraction
- price mapping
- identifier strategy
- domain construction
- controlled failure behavior

### Package Changes

Report the exact package and version added and confirm its project scope.

### Build

Report:

- command executed
- success or failure
- warning count
- error count

Distinguish pre-existing warnings from warnings introduced by this task.

### Tests

Confirm that no tests were added or modified and that parser tests are reserved for Prompt 034 Step 6.

### Scope Check

Confirm that:

- only Step 2 was implemented
- only Infrastructure was changed
- no HTTP provider or web request was added
- no Skill was added
- no dependency-injection or configuration changes were made
- no persistence or UI behavior was added
- no tests or fixtures were added
- only AngleSharp 1.5.2 was added
- no project references were added
- no documentation files were modified

