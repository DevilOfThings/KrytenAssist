# Prompt 022 – Avalonia Offline Prompt Store

## Goal

Add the first offline data capability to the Avalonia desktop client.

This prompt gives the desktop app its own local prompt card model, JSON-backed storage, dependency injection, and a ViewModel so that prompt cards can be loaded without the API running.

This is a foundation prompt only.

No editing, deleting, API synchronisation, SQLite, or cloud storage should be added in this prompt.

---

## Why This Prompt Exists

The Avalonia client is intended to become a desktop-first interface for Kryten Assist.

Unlike the React client, the desktop app should eventually be useful even when the API is unavailable.

This prompt introduces the first part of that capability by adding a simple offline prompt store.

This prompt also establishes the architectural foundation for the Avalonia client by introducing MVVM, dependency injection, and an abstraction for local persistence. These patterns will support all future desktop features.

---

## Scope

Implement only the first offline prompt store foundation.

### In Scope

- Create an Avalonia-local prompt card model
- Create a prompt card store interface
- Create a JSON-backed prompt card store implementation
- Store prompt cards under the user's application data folder
- Register the store with dependency injection
- Add a `MainWindowViewModel`
- Load prompt cards from the offline store
- Bind the main window to the ViewModel
- Display offline prompt cards in the Avalonia window

### Out of Scope

- Creating prompt cards from the Avalonia UI
- Editing prompt cards
- Deleting prompt cards
- Search and filtering
- API synchronisation
- Conflict resolution
- SQLite storage
- EF Core in the Avalonia client
- Authentication
- AI integration
- Embeddings

---

## Expected Files Created

- `KrytenAssist.Avalonia/Models/PromptCardModel.cs`
- `KrytenAssist.Avalonia/Services/IPromptCardStore.cs`
- `KrytenAssist.Avalonia/Services/JsonPromptCardStore.cs`
- `KrytenAssist.Avalonia/ViewModels/MainWindowViewModel.cs`

---

## Expected Files Updated

- `KrytenAssist.Avalonia/KrytenAssist.Avalonia.csproj`
- `KrytenAssist.Avalonia/Program.cs`
- `KrytenAssist.Avalonia/MainWindow.axaml.cs`
- `KrytenAssist.Avalonia/MainWindow.axaml`

---

## Implementation Notes

### Local Model

Create a desktop-side model named `PromptCardModel`.

This model is intentionally separate from the Core domain entity because the Avalonia application requires a simple serialisable offline representation. It represents the desktop application's local persistence model rather than the application's domain model, allowing the storage format to evolve independently without coupling the UI directly to the Core layer.

The model should include:

- `Id`
- `Title`
- `Category`
- `Description`
- `PromptText`
- `Tags`
- `CreatedAt`
- `UpdatedAt`

---

### Store Interface

Create `IPromptCardStore` to define the offline storage boundary.

The interface should expose:

```csharp
Task<IReadOnlyCollection<PromptCardModel>> GetAllAsync();

Task SaveAllAsync(IReadOnlyCollection<PromptCardModel> promptCards);
```

The UI and ViewModel should depend on this abstraction rather than directly depending on JSON storage.

---

### JSON Store

Create `JsonPromptCardStore` to implement `IPromptCardStore`.

It should:

- Use `System.Text.Json`
- Store data in a `prompt-cards.json` file
- Create a `KrytenAssist` application data folder if it does not exist
- Return an empty collection, or a sample seeded collection, when no file exists
- Save prompt cards using indented JSON

On macOS, the file is expected to be created under a path equivalent to:

```text
~/Library/Application Support/KrytenAssist/prompt-cards.json
```

---

### Dependency Injection

Add `Microsoft.Extensions.DependencyInjection` to the Avalonia project.

Register:

```csharp
services.AddSingleton<IPromptCardStore, JsonPromptCardStore>();
services.AddTransient<MainWindowViewModel>();
```

The Avalonia application should expose a service provider from `Program` so that the initial window can resolve the ViewModel.

---

### ViewModel

Create `MainWindowViewModel`.

It should:

- Accept `IPromptCardStore` through its constructor
- Expose an `ObservableCollection<PromptCardModel>` named `PromptCards`
- Provide a `LoadAsync()` method
- Load all prompt cards from the store
- Clear and repopulate the observable collection

This keeps loading behaviour out of the View.

---

### Main Window Wiring

Update `MainWindow.axaml.cs` so that it:

- Resolves `MainWindowViewModel` from DI
- Sets it as the `DataContext`
- Calls `LoadAsync()` when the window opens

Update `MainWindow.axaml` so that it:

- Displays the title `Kryten Assist`
- Displays the subtitle `Offline prompt cards`
- Binds an `ItemsControl` to `PromptCards`
- Displays each prompt card title, category, and description

---

## Verification

Run:

```bash
dotnet build
```

Expected behaviour:

- The application builds successfully.
- The Avalonia window opens.
- The `MainWindowViewModel` is successfully resolved from dependency injection.
- Prompt cards are loaded into the ViewModel.
- Existing prompt cards are displayed correctly.
- If no JSON file exists, one can be created manually or temporarily seeded during development to verify the offline storage pipeline.
- The JSON file is successfully read on subsequent application launches.

---

## Result

Prompt 022 establishes the first offline architecture for the Avalonia client.

The desktop application can now load prompt cards from local JSON storage without requiring the API to be running.

This prompt also establishes the desktop architectural patterns that future prompts will build upon:

- Model: `PromptCardModel`
- View: `MainWindow.axaml`
- ViewModel: `MainWindowViewModel`
- Storage abstraction: `IPromptCardStore`
- JSON implementation: `JsonPromptCardStore`
- Dependency Injection
- MVVM

The application is now capable of running as a self-contained desktop client, providing the foundation for future editing, synchronisation, and richer desktop functionality.

---

## Future Prompts

This prompt establishes the foundation for future Avalonia features.

Subsequent prompts may build upon this work by introducing:

- Prompt card editing
- Prompt card creation
- Search and filtering
- Empty-state UI improvements
- Synchronisation with the API
- Alternative storage providers such as SQLite

---

## Lessons Learnt

- A desktop client should not depend on the API for every feature.
- JSON storage is a simple first step for offline capability.
- `IReadOnlyCollection<T>` keeps consumers from depending on mutable concrete list types.
- Depending on `IPromptCardStore` instead of `JsonPromptCardStore` follows Dependency Inversion.
- MVVM helps keep UI rendering separate from state and loading behaviour.
- Testing ViewModels is easier than testing Views.
- Empty-state handling should be considered separately from persistence logic to keep responsibilities well defined.

---

## Follow-Up Recommendation

Before considering the Avalonia prompt browser feature complete, add an empty-state message for when the offline store exists but contains no prompt cards.

Suggested text:

```text
No offline prompt cards found.
```

This should be treated as a small UI polish item, not a reason to expand Prompt 022 into editing or synchronisation.

---

## Git Commit Suggestion

```text
Add Avalonia offline prompt store
```