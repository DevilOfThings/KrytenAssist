# Prompt 016 – Introduce Entity Framework Core

## Goal

Introduce Entity Framework Core into the Infrastructure layer by replacing the hand-written SQL implementation while preserving the existing Clean Architecture boundaries and repository abstraction.

The Application and Core projects must remain completely independent of Entity Framework Core.

## Objectives

- Add Entity Framework Core to the Infrastructure layer.
- Keep SQLite as the database provider.
- Introduce `KrytenAssistDbContext`.
- Map `PromptCard` using EF Core.
- Replace the existing persistence implementation inside `SqlitePromptCardRepository` with Entity Framework Core.
- Preserve the existing `IPromptCardRepository` abstraction.
- Preserve the existing `SqlitePromptCardRepository` public type.
- Keep Application and Core independent of EF Core.
- Keep API endpoints unchanged.
- Avoid EF Core migrations in this prompt.
- Ensure all existing integration tests continue to pass.

---

## Architectural Rules

- Core must not reference EF Core.
- Application must not reference EF Core.
- API must not contain persistence or EF Core logic.
- Infrastructure owns all persistence implementation.
- Repository abstraction remains in Application.
- `SqlitePromptCardRepository` remains the repository implementation.
- `DatabaseInitializer` remains responsible for database initialisation.
- Use `Database.EnsureCreated()` only for this prompt.
- Prompt 017 will replace `EnsureCreated()` with proper EF Core migrations.

---

## Planned Changes

### Infrastructure

Extend the existing persistence layer:

```text
KrytenAssist.Infrastructure/
└── Persistence/
    ├── DatabaseInitializer.cs
    ├── KrytenAssistDbContext.cs
    ├── PromptCardConfiguration.cs
    └── SqlitePromptCardRepository.cs
```

---

### Packages

Add Entity Framework Core SQLite support:

```bash
dotnet add KrytenAssist.Infrastructure package Microsoft.EntityFrameworkCore.Sqlite
```

---

### DbContext

Create:

```csharp
KrytenAssistDbContext : DbContext
```

with:

```csharp
DbSet<PromptCard> PromptCards
```

The DbContext should apply entity configurations automatically:

```csharp
modelBuilder.ApplyConfigurationsFromAssembly(typeof(KrytenAssistDbContext).Assembly);
```

---

### Entity Configuration

Configure `PromptCard` using `IEntityTypeConfiguration<PromptCard>`.

The configuration should include:

- Primary key
- Required properties
- Optional properties
- JSON value conversion for `Tags`
- ValueComparer for Tags to support EF Core change tracking.
- ValueConverter for CreatedAt and UpdatedAt to persist DateTimeOffset as ISO-8601 strings compatible with SQLite ordering.

Implement a `ValueConverter` so:

```csharp
IReadOnlyCollection<string>
```

is stored as JSON text and automatically converted back into the domain model.

---

### Repository

Retain:

```csharp
SqlitePromptCardRepository : IPromptCardRepository
```

Replace the existing persistence implementation with Entity Framework Core while preserving the public repository contract.

The repository must continue to support:

- Create
- Get all
- Get by id
- Update
- Delete

`UpdateAsync` and `DeleteAsync` must continue returning `bool`.

The repository interface must remain unchanged.

---

## Database Initialisation

Update `DatabaseInitializer` so it is responsible for ensuring the EF Core database exists.

Replace the manual SQL table creation with:

```csharp
context.Database.EnsureCreated();
```

No migrations are introduced in this prompt.

---

## Dependency Injection

Update Infrastructure dependency injection to:

- Register `KrytenAssistDbContext`.
- Read the existing `PromptCards` connection string.
- Initialise the database through `DatabaseInitializer`.
- Register `SqlitePromptCardRepository`.

---

## Implementation Steps

1. Add `Microsoft.EntityFrameworkCore.Sqlite`.
2. Create `KrytenAssistDbContext`.
3. Create `PromptCardConfiguration`.
4. Configure JSON value conversion, ValueComparer for Tags, and DateTimeOffset value converters.
5. Refactor `SqlitePromptCardRepository` to use EF Core.
6. Register `KrytenAssistDbContext` using the existing `PromptCards` connection string.
7. Update `DatabaseInitializer` to replace manual SQL schema creation with Database.EnsureCreated().
8. Verify dependency injection still resolves `IPromptCardRepository`.
9. Remove the obsolete manual persistence implementation.
10. Run build and integration tests.

---

## Validation

Run:

```bash
dotnet build
dotnet test
```

Expected result:

```text
Build succeeded
9 tests passing
0 failing
```

The known `NU1903` warning from the transitive SQLite dependency may still appear and is accepted technical debt for now.

Confirmed that all 9 integration tests pass after migrating the repository to Entity Framework Core without changing the public API.

---

## Success Criteria

- API endpoints remain unchanged.
- Existing integration tests pass without modification.
- SQLite remains the persistence provider.
- Repository abstraction remains unchanged.
- `SqlitePromptCardRepository` continues to be the Infrastructure implementation.
- Application and Core remain independent of EF Core.
- EF Core is isolated entirely within Infrastructure.
- `DatabaseInitializer` remains responsible for database initialisation.
- No EF Core migrations are introduced.

---

## Commit Message

```bash
git commit -m "016 Introduce Entity Framework Core"
```

---

## Lessons to Capture

- Repository abstractions allow implementation details to evolve without affecting application behaviour.
- Entity Framework Core removes repetitive persistence code while preserving architectural boundaries.
- Value converters provide a clean mechanism for persisting richer domain types such as collections.
- `Database.EnsureCreated()` is appropriate during early development but should be replaced with migrations before production.
- Integration tests provide confidence when replacing internal implementations without changing external behaviour.
- SQLite cannot translate DateTimeOffset ordering directly; persisting ISO-8601 strings via a ValueConverter preserves correct ordering.
- Collection properties using ValueConverter should also define a ValueComparer so EF Core can correctly detect changes.