

# Prompt 017 – Database Migrations

## Goal

Replace `Database.EnsureCreated()` with proper Entity Framework Core migrations so Kryten Assist has production-ready database versioning.

This prompt introduces the EF Core migrations workflow while preserving the existing Clean Architecture boundaries.

---

## Current State

Kryten Assist currently uses Entity Framework Core with SQLite.

The Infrastructure layer contains:

```text
KrytenAssist.Infrastructure/
└── Persistence/
    ├── DatabaseInitializer.cs
    ├── KrytenAssistDbContext.cs
    ├── PromptCardConfiguration.cs
    └── SqlitePromptCardRepository.cs
```

Database creation is currently handled by:

```csharp
context.Database.EnsureCreated();
```

This works for local development but bypasses EF Core migrations and does not provide versioned database schema changes.

---

## Objective

Introduce EF Core migrations properly.

By the end of this prompt:

- `Database.EnsureCreated()` will be replaced with `Database.Migrate()`
- The EF Core design package will be installed
- A design-time DbContext factory will exist
- An initial migration will be generated
- Migrations will be applied automatically during API startup
- Integration tests will continue to pass
- Clean Architecture boundaries will remain intact

---

## Architecture Rules

Do not reference Entity Framework Core from:

```text
KrytenAssist.Core
KrytenAssist.Application
```

Entity Framework Core must remain inside:

```text
KrytenAssist.Infrastructure
```

The API project may continue to call Infrastructure registration and database initialisation methods, but it must not directly depend on persistence implementation details.

---

## Step 1 – Add EF Core Design Package

Add the EF Core design package to:

```text
KrytenAssist.Infrastructure/KrytenAssist.Infrastructure.csproj
```

Add:

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.9">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

Then run:

```bash
dotnet restore
dotnet build
```

Expected result:

```text
Build succeeded
```

The existing `NU1903` warning from `SQLitePCLRaw.lib.e_sqlite3` is currently accepted.

---

## Step 2 – Add a Design-Time DbContext Factory

Create a new file:

```text
KrytenAssist.Infrastructure/Persistence/KrytenAssistDbContextFactory.cs
```

Add:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KrytenAssist.Infrastructure.Persistence;

public sealed class KrytenAssistDbContextFactory : IDesignTimeDbContextFactory<KrytenAssistDbContext>
{
    public KrytenAssistDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<KrytenAssistDbContext>();

        optionsBuilder.UseSqlite("Data Source=krytenassist.db");

        return new KrytenAssistDbContext(optionsBuilder.Options);
    }
}
```

Purpose:

- Allows `dotnet ef` to create `KrytenAssistDbContext` at design time
- Keeps migration creation independent of the running API
- Keeps EF Core tooling concerns inside Infrastructure

Then run:

```bash
dotnet build
```

Expected result:

```text
Build succeeded
```

---

## Step 3 – Replace EnsureCreated with Migrate

Update:

```text
KrytenAssist.Infrastructure/Persistence/DatabaseInitializer.cs
```

Replace:

```csharp
context.Database.EnsureCreated();
```

with:

```csharp
context.Database.Migrate();
```

Add this using directive if required:

```csharp
using Microsoft.EntityFrameworkCore;
```

Purpose:

- `EnsureCreated()` creates a database directly without migrations
- `Migrate()` creates the database if required and applies any pending migrations
- Future schema changes can now be versioned and applied safely

Then run:

```bash
dotnet build
```

Expected result:

```text
Build succeeded
```

---

## Step 4 – Install or Verify EF CLI Tooling

Check whether the EF CLI is available:

```bash
dotnet ef --version
```

If the command is unavailable, install the tool:

```bash
dotnet tool install --global dotnet-ef
```

If already installed but version mismatches become a problem, update it:

```bash
dotnet tool update --global dotnet-ef
```

Then verify again:

```bash
dotnet ef --version
```

Expected result:

```text
10.x.x
```

---

## Step 5 – Create the Initial Migration

From the solution root, run:

```bash
dotnet ef migrations add InitialCreate \
  --project KrytenAssist.Infrastructure \
  --startup-project KrytenAssist.Api \
  --output-dir Persistence/Migrations
```

Expected result:

A new folder should be created:

```text
KrytenAssist.Infrastructure/
└── Persistence/
    └── Migrations/
        ├── <timestamp>_InitialCreate.cs
        ├── <timestamp>_InitialCreate.Designer.cs
        └── KrytenAssistDbContextModelSnapshot.cs
```

Review the generated migration and confirm it creates the `PromptCards` table.

The migration should include columns for:

- `Id`
- `Title`
- `Category`
- `Description`
- `PromptText`
- `Tags`
- `CreatedAt`
- `UpdatedAt`

---

## Step 6 – Validate Runtime Migration Application

Remove any local generated database file if it exists:

```bash
rm KrytenAssist.Api/krytenassist.db
```

Then run:

```bash
dotnet build
dotnet test
```

Expected result:

```text
Build succeeded
9 tests passing
```

The API should recreate the database automatically by applying the initial migration.

---

## Step 7 – Check Git Status

Run:

```bash
git status
```

Expected files should include:

```text
KrytenAssist.Infrastructure/KrytenAssist.Infrastructure.csproj
KrytenAssist.Infrastructure/Persistence/DatabaseInitializer.cs
KrytenAssist.Infrastructure/Persistence/KrytenAssistDbContextFactory.cs
KrytenAssist.Infrastructure/Persistence/Migrations/*
docs/AI Playbook/017 - Database Migrations.md
```

The generated SQLite database file must not be committed.

Confirm `.gitignore` still excludes:

```text
*.db
*.db-shm
*.db-wal
```

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
Test summary: total: 9, failed: 0, succeeded: 9, skipped: 0
```

Known accepted warning:

```text
NU1903 SQLitePCLRaw.lib.e_sqlite3
```

---

## Result

Kryten Assist now uses proper EF Core migrations instead of direct database creation.

The database schema is version controlled, future schema changes can be added safely, and the application can automatically apply pending migrations during startup.

---

## Files Created

```text
KrytenAssist.Infrastructure/
└── Persistence/
    ├── KrytenAssistDbContextFactory.cs
    └── Migrations/
        ├── <timestamp>_InitialCreate.cs
        ├── <timestamp>_InitialCreate.Designer.cs
        └── KrytenAssistDbContextModelSnapshot.cs
```

---

## Files Updated

```text
KrytenAssist.Infrastructure/
├── KrytenAssist.Infrastructure.csproj
└── Persistence/
    └── DatabaseInitializer.cs

 docs/
└── AI Playbook/
    └── 017 - Database Migrations.md
```

---

## Lessons Learnt

- `EnsureCreated()` is useful for simple prototypes but bypasses migrations.
- `Migrate()` applies versioned schema changes and is more suitable for real applications.
- EF Core CLI tooling needs a reliable way to create the DbContext at design time.
- A design-time factory keeps EF tooling concerns inside Infrastructure.
- Migrations become part of the source-controlled history of the database schema.

---

## Commit Message

```text
017 Introduce database migrations
```