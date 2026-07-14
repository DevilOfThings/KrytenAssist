# Prompt 033 – Cruise Domain Models

## Goal

Create the shared, provider-independent Cruise domain models that future Cruise Skills will use.

The domain should establish a small and stable vocabulary for representing:

- a cruise offered by a provider
- the provider publishing the offer
- a price associated with the offer
- the state of an offer at a particular point in time
- the observation that produced that state

This is a domain-foundation prompt only.

No Cruise Skill, web access, parsing, persistence, price comparison, alerting, scheduling, dashboard or user interface should be implemented.

---

## Why This Prompt Exists

Prompt 032 introduced the provider-independent Skills framework.

Prompt 034 will implement the first real Skill: Cruise of the Week.

Before that Skill can retrieve or interpret cruise information, Kryten Assist needs application-owned models that describe cruise data without depending on:

- a cruise-line website
- HTML structure
- a web client
- an AI provider
- Avalonia
- database entities
- a particular external API

Without a shared Cruise domain, provider-specific response shapes could leak into the rest of the application and make later capabilities difficult to evolve.

Prompt 033 creates that common language before any external integration begins.

---

## Domain Vision

The Cruise domain will support the first generation of Kryten Assist's Cruise Assistant.

Later prompts will use these models to:

- retrieve Cruise of the Week
- compare observations over time
- maintain price history
- create watch lists
- identify price drops
- monitor cabin availability
- detect new itineraries
- populate a Cruise dashboard

Those behaviours are not part of this prompt.

The models introduced here should contain only the information needed to represent cruise offers and observations cleanly. They should not predict every future requirement.

---

## Design Principles

The Cruise domain must:

- belong to Kryten Assist
- remain independent of cruise providers
- remain independent of external SDKs and transport formats
- remain independent of Skills infrastructure
- remain independent of persistence
- remain independent of user interfaces
- use immutable models where practical
- use explicit types for important concepts
- represent money without floating-point values
- represent observations with an explicit timestamp
- support deterministic unit testing
- avoid speculative abstractions

Provider independence does not mean that the source provider is discarded.

The domain should retain provider identity as data while avoiding provider-specific classes or response models.

---

## Proposed Architecture

The shared Cruise domain belongs in `KrytenAssist.Core`.

```text
KrytenAssist.Core
└── Cruises
    ├── CruiseOffer.cs
    ├── CruiseObservation.cs
    ├── CruisePrice.cs
    ├── CruiseProvider.cs
    └── CruiseSnapshot.cs
```

Use the namespace:

```csharp
KrytenAssist.Core.Cruises
```

The exact folder layout may be adjusted to match an established `KrytenAssist.Core` convention, but all Cruise models must remain in the shared Core project.

Do not place them in:

- `KrytenAssist.Avalonia`
- `KrytenAssist.Application`
- `KrytenAssist.Infrastructure`
- `KrytenAssist.Api`
- a provider-specific project or namespace

---

## Domain Language

### CruiseProvider

Identifies the organisation or source publishing a cruise offer.

Examples might include:

- Marella Cruises
- P&O Cruises
- Royal Caribbean

`CruiseProvider` should be an immutable value model rather than an enum.

An enum would require the shared domain to change every time support for a new provider is introduced.

The model should contain:

- a stable provider identifier
- a display name

Provider identifiers should be suitable for comparison and persistence by later prompts, but this prompt should not introduce a provider catalogue or registration service.

---

### CruisePrice

Represents a monetary price quoted for a cruise offer.

The model should contain:

- decimal amount
- ISO-style currency code
- optional price basis or description when required to preserve the meaning of the quote

Examples of a price basis could include:

- per person
- total
- from price

Do not use `double` or `float` for money.

Do not implement currency conversion, formatting, comparison services or discount calculations.

Cabin-specific pricing should not be invented unless it is necessary to represent the source-neutral contract agreed during implementation. Detailed cabin availability belongs to later prompts.

---

### CruiseOffer

Represents the provider-independent identity and descriptive details of an offered cruise.

The model should contain only stable, source-neutral information needed to identify and describe the offer, such as:

- provider
- provider's offer identifier
- title
- ship name
- departure date
- duration in nights
- departure port, when supplied
- itinerary or destination summary, when supplied

Optional source data should be represented honestly as optional rather than replaced with empty or invented values.

`CruiseOffer` should not contain:

- HTML
- JSON payloads
- provider SDK objects
- database identifiers
- retrieval logic
- persistence logic
- price-history calculations
- UI display state

The model should not assume that every provider publishes the same fields.

---

### CruiseSnapshot

Represents the observable state of a cruise offer at a particular point in time.

A snapshot should associate:

- the cruise offer
- the currently observed price or prices
- current promotional text or offer summary, when available
- any other small source-neutral value required to describe the offer's current state

The snapshot should be immutable.

It should not:

- retrieve its own data
- compare itself with previous snapshots
- decide whether a change is meaningful
- persist itself
- raise alerts

Those behaviours belong to later application services and Skills.

---

### CruiseObservation

Represents the fact that Kryten observed a cruise snapshot at a particular time.

The model should contain:

- the snapshot
- the observation timestamp
- optional source reference, when available

The source reference should remain transport-neutral. Do not introduce HTTP client or browser types into the model.

`CruiseObservation` is a domain record, not a database entity and not a background job.

It should not contain persistence state, retry state, scheduling state or change-detection results.

---

## Model Relationships

```text
CruiseObservation
└── CruiseSnapshot
    ├── CruiseOffer
    │   └── CruiseProvider
    └── CruisePrice (one or more, according to the agreed minimal contract)
```

The relationships should use composition.

Do not introduce inheritance hierarchies for providers, offers, prices, snapshots or observations.

---

## Value and Time Semantics

Use types that preserve domain meaning.

Preferred semantics include:

- `decimal` for monetary amounts
- `DateOnly` for a departure date when no time-of-day is meaningful
- `DateTimeOffset` for observation timestamps
- integer nights for cruise duration
- immutable or read-only collections
- nullable values for genuinely optional source information

Observation timestamps must be supplied by the caller.

The models must not read the current system clock internally. This keeps the domain deterministic and allows later retrieval services to use the application's clock abstraction.

Do not add time-zone conversion behaviour in this prompt.

---

## Construction and Validation

The models should prevent clearly invalid domain state without becoming a validation framework.

At minimum, consider guarding against:

- null required objects
- null, empty or whitespace-only required identifiers and names
- negative monetary amounts
- missing or invalid currency codes
- zero or negative cruise durations
- null collections

Validation rules should be small, deterministic and local to the model that owns the invariant.

Do not introduce FluentValidation or another package for these domain invariants.

Do not add provider-specific validation rules.

If collections are accepted by constructors, protect model immutability by copying them or exposing a genuinely read-only representation.

---

## Scope

### In Scope

- Add a shared Cruise namespace to `KrytenAssist.Core`.
- Implement `CruiseProvider`.
- Implement `CruisePrice`.
- Implement `CruiseOffer`.
- Implement `CruiseSnapshot`.
- Implement `CruiseObservation`.
- Define clear composition relationships between the models.
- Keep required and optional data explicit.
- Enforce small, model-owned invariants.
- Add comprehensive unit tests for the domain models.
- Verify that the shared models have no provider, UI, persistence or Skills dependencies.

### Out of Scope

- `ISkill` implementations
- Cruise of the Week
- Marella-specific models
- web requests
- HTML parsing
- JSON deserialization contracts
- browser automation
- provider SDKs
- repositories
- Entity Framework configuration
- migrations
- local storage
- historical comparison
- snapshot deduplication
- price trend calculation
- watch lists
- price alerts
- cabin availability monitoring
- itinerary detection
- dashboards
- navigation
- Avalonia UI
- React UI
- notifications
- background jobs
- scheduling
- dependency-injection registration
- AI-provider integration
- Tool integration

---

## Implementation Steps

### Step 1 – Create the Cruise Domain Structure

Create the Cruise folder and namespace in `KrytenAssist.Core`.

Confirm that the Core project remains free of references to outer application layers and provider packages.

---

### Step 2 – Implement Foundational Value Models

Implement:

- `CruiseProvider`
- `CruisePrice`

Define their minimal required values and local invariants.

Keep both models immutable and provider independent.

---

### Step 3 – Implement CruiseOffer

Implement the stable provider-independent description of a cruise offer.

Use `CruiseProvider` through composition.

Represent provider-supplied optional information explicitly.

Do not add retrieval, parsing, persistence or presentation behaviour.

---

### Step 4 – Implement CruiseSnapshot

Implement the point-in-time observable state of a `CruiseOffer`.

Use immutable composition and protect any price collection from external mutation.

Do not add comparison or change-detection logic.

---

### Step 5 – Implement CruiseObservation

Implement the record associating a `CruiseSnapshot` with an explicit observation timestamp and optional source reference.

Do not read the clock internally.

Do not add persistence or scheduling metadata.

---

### Step 6 – Add Domain Unit Tests

Add focused tests covering:

- successful construction of each model
- value equality where value models use record semantics
- required-field guards
- monetary amount validation
- currency validation
- cruise-duration validation
- optional values
- model composition
- immutable or defensively copied collections
- explicit observation timestamps

Tests should remain deterministic and should not use:

- network access
- file-system access
- system time
- dependency injection
- UI startup
- provider SDKs

Do not test compiler-generated behaviour unless it represents an intentional domain contract.

---

### Step 7 – Verify Domain Independence

Build and test the solution.

Inspect the new domain models and confirm that they contain no references to:

- Avalonia
- OpenAI
- Skills framework types
- HTTP or browser clients
- Entity Framework
- database entities or attributes
- provider-specific response types

Verify that future provider implementations can map their own response data into these shared models without changing the Core domain.

---

## Testing

The domain tests should verify behaviour and invariants rather than property-by-property implementation details.

At minimum, verify:

- valid models can be created
- required identity values cannot be blank
- prices use valid non-negative decimal amounts
- currency codes are normalised or validated consistently
- cruise duration is positive
- optional fields may be absent
- snapshots retain their associated offer and prices
- source collections cannot mutate an existing snapshot
- observations retain the exact caller-supplied timestamp
- model composition uses the expected instances or values

All existing tests must continue to pass.

---

## Acceptance Criteria

Prompt 033 is complete when:

- the shared Cruise domain exists in `KrytenAssist.Core`
- `CruiseProvider` has been implemented
- `CruisePrice` has been implemented
- `CruiseOffer` has been implemented
- `CruiseSnapshot` has been implemented
- `CruiseObservation` has been implemented
- the models are immutable where practical
- required invariants are enforced locally
- money uses `decimal`
- observations use caller-supplied `DateTimeOffset` values
- collections cannot be mutated externally
- comprehensive deterministic unit tests pass
- the solution builds successfully
- all existing tests pass
- no web, parsing, persistence, Skill or UI functionality has been introduced
- the domain is ready for Prompt 034

---

## Results

> Complete this section after implementation.

### Status

_Not Started_

### Files Created

_To be completed._

### Files Updated

_To be completed._

### Build

_To be completed._

### Tests

_To be completed._

### Git Commit

_Not Created_

---

# Lessons Learned

> Complete this section after implementation.

