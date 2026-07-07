

# Prompt 021 – Avalonia Client

## Goal

Create the first desktop client for Kryten Assist using Avalonia.

This prompt introduces a basic Avalonia application project, adds it to the existing solution, and displays a simple Kryten Assist shell window.

This is a foundation prompt only.

No API integration, prompt card loading, editing, or persistence should be added in this prompt.

---

## Why This Prompt Exists

Kryten Assist is intended to support more than one user interface.

The React client provides a browser-based interface.

The Avalonia client will provide a desktop interface that can later become a richer assistant-style application.

This prompt establishes the desktop client safely before adding behaviour.

---

## Scope

Implement only the Avalonia client foundation.

### In Scope

- Create a new Avalonia desktop project.
- Add the project to `KrytenAssist.sln`.
- Keep the project separate from the existing React client.
- Display a basic main window.
- Add simple Kryten Assist branding text.
- Confirm the solution builds successfully.

### Out of Scope

- Calling the API.
- Loading Prompt Cards.
- Creating Prompt Cards.
- Editing Prompt Cards.
- Dependency injection setup.
- MVVM refactoring.
- Navigation.
- Styling beyond a simple shell window.
- Packaging or publishing.

---

## Expected Project

Create a new project named:

```text
KrytenAssist.Avalonia
```

Expected location:

```text
KrytenAssist.Avalonia/
```

Expected project file:

```text
KrytenAssist.Avalonia/KrytenAssist.Avalonia.csproj
```

---

## Suggested Commands

Install or update Avalonia templates if required:

```bash
dotnet new install Avalonia.Templates
```

Create the Avalonia project:

```bash
dotnet new avalonia.app -o KrytenAssist.Avalonia
```

Add the project to the solution:

```bash
dotnet sln KrytenAssist.sln add KrytenAssist.Avalonia/KrytenAssist.Avalonia.csproj
```

Build the solution:

```bash
dotnet build
```

Run the Avalonia client:

```bash
dotnet run --project KrytenAssist.Avalonia
```

---

## Implementation Steps

### Step 1 – Create Prompt 021 Documentation

Create this prompt file and define the scope before touching the application.

### Step 2 – Create Avalonia Project

Create a new Avalonia desktop project called `KrytenAssist.Avalonia`.

Do not place it inside the React client.

Do not reuse the existing `KrytenAssist.Client` name.

### Step 3 – Add Project to Solution

Add `KrytenAssist.Avalonia.csproj` to `KrytenAssist.sln`.

Confirm the project appears in Rider.

### Step 4 – Create Basic Shell Window

Update the main window so the application clearly identifies itself as Kryten Assist.

The first shell should include:

- Application title
- Short subtitle
- Placeholder text explaining that the Avalonia client foundation is ready

Suggested window text:

```text
Kryten Assist
Desktop Client
Avalonia foundation ready.
```

### Step 5 – Build and Run

Run:

```bash
dotnet build
```

Then run:

```bash
dotnet run --project KrytenAssist.Avalonia
```

Confirm the desktop window opens successfully.

### Step 6 – Document Completion

Update this prompt with the final result once complete.

Update `Roadmap.md` to mark Prompt 021 as complete.

Create a git commit.

---

## Build Verification

Required before completion:

```bash
dotnet build
```

Expected result:

```text
Build succeeded
```

Existing API tests should still pass:

```bash
dotnet test
```

Expected result:

```text
9 tests passing
```

---

## Completion Criteria

Prompt 021 is complete when:

- `KrytenAssist.Avalonia` project exists.
- Project is included in `KrytenAssist.sln`.
- Avalonia app runs from the command line.
- Main window displays Kryten Assist shell text.
- `dotnet build` succeeds.
- `dotnet test` succeeds.
- `Roadmap.md` is updated.
- Git commit is created.

---

## Notes / Lessons Learnt

To be completed at the end of the prompt.

---

## Result

To be completed.

## Status

Not started.

## Files Created

To be completed.

## Files Updated

To be completed.

## Build

To be completed.

## Git Commit

To be completed.

## Lessons Learnt

To be completed.