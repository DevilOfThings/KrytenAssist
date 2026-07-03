# Prompt 015 – SQLite Repository

## Goal

Replace the in-memory PromptCard repository with a SQLite-backed repository while preserving the existing Clean Architecture boundaries.

The Application and Core projects must remain persistence-agnostic.

## Objectives

- Add SQLite persistence in the Infrastructure layer.
- Preserve the existing `IPromptCardRepository` abstraction.
- Replace the in-memory repository registration with a SQLite repository.
- Use `Microsoft.Data.Sqlite`.
- Avoid Entity Framework Core for this prompt.
- Create the database schema automatically on application startup.
- Store PromptCards persistently between API runs.
- Keep the existing API endpoints unchanged.
- Ensure existing integration tests continue to pass.

## Architectural Rules

- Core must not reference SQLite.
- Application must not reference SQLite.
- API should only depend on Infrastructure through dependency injection.
- Infrastructure owns database access.
- No EF Core yet.
- No migrations yet.
- No direct SQL in API endpoints.
- Existing use cases should not need to know how data is stored.

## Planned Changes

### Infrastructure

Add SQLite support:

```text
KrytenAssist.Infrastructure/
├── Persistence/
│   ├── DatabaseInitializer.cs
│   └── SqlitePromptCardRepository.cs
```

### Configuration

Add a connection string:

```json
"ConnectionStrings": {
  "PromptCards": "Data Source=krytenassist.db"
}
```

### Repository

Implement:

```csharp
SqlitePromptCardRepository : IPromptCardRepository
```

The repository should support:

- Create
- Get all
- Get by id
- Update
- Delete

`UpdateAsync` and `DeleteAsync` return `bool` to indicate whether a row was affected. This allows the API to return `404 Not Found` when an update or delete is requested for an unknown PromptCard id.

### Database Schema

Create a `PromptCards` table with:

- `Id`
- `Title`
- `Category`
- `Description`
- `PromptText`
- `Tags`
- `CreatedAt`
- `UpdatedAt`

Tags may be stored as JSON text for now.

`CreatedAt` and `UpdatedAt` are stored as ISO 8601 text values using `DateTimeOffset.ToString("O")` and parsed back into the domain entity when read from SQLite.

## Implementation Steps

1. Add the `Microsoft.Data.Sqlite` package to Infrastructure.
2. Add the connection string to `appsettings.json`.
3. Create `DatabaseInitializer`.
4. Create the `PromptCards` table if it does not exist.
5. Create `SqlitePromptCardRepository`.
6. Map database rows back to `PromptCard`.
7. Update Infrastructure dependency injection to read the `PromptCards` connection string, initialise the database, and register `SqlitePromptCardRepository`.
8. Update `Program.cs` to call `AddInfrastructure(builder.Configuration)`.
9. Run the existing integration tests.
10. Update Roadmap and commit.

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

A known `NU1903` warning may appear from the transitive SQLite dependency `SQLitePCLRaw.lib.e_sqlite3`. This is accepted as technical debt for this prompt and should be reviewed in a future maintenance prompt.

## Success Criteria

- API still exposes the same endpoints.
- Existing tests pass.
- PromptCards persist to SQLite.
- Clean Architecture boundaries remain intact.
- No EF Core has been introduced.
- Prompt 015 is documented and committed.
- Existing integration tests pass without modification.

## Commit Message

```bash
git commit -m "015 SQLite Repository"
```

## Lessons to Capture

- Repository abstraction allowed persistence to change without changing API behaviour.
- SQLite gives the project real persistence while keeping the implementation simple.
- Storing tags as JSON is acceptable for this stage and can be normalised later.
- Integration tests protect the API contract while the persistence implementation changes.
- `UpdateAsync` and `DeleteAsync` returning `bool` gives the API a clean way to distinguish successful operations from unknown ids.