# Prompt 023 – Avalonia Prompt Template Editor

## Goal

Add the first prompt template editing capability to the Avalonia desktop client.

This prompt introduces a simple editor that allows users to create new prompt templates and save them to the existing local JSON prompt store created in Prompt 022.

This is a foundation prompt only.

No editing of existing templates, deletion, API synchronisation, cloud storage, search, favourites, or AI integration should be added in this prompt.

---

## Why This Prompt Exists

Prompt 022 introduced offline prompt storage and MVVM.

The application can now load prompt templates from local storage, but users cannot yet create their own templates.

This prompt evolves the desktop application from a read-only viewer into the first usable offline prompt editor while continuing to build the MVVM architecture.

---

## Scope

Implement only the first prompt template editor.

## Architecture

This prompt builds directly on the offline prompt store introduced in Prompt 022.

The implementation should continue following the MVVM pattern:

- The View owns layout and data binding.
- The ViewModel owns user interaction and save behaviour.
- The JSON prompt store owns persistence.
- Dependency Injection continues to resolve application services.

## Design Principles

The implementation should favour incremental evolution over expansion.

- Extend existing components before creating new ones.
- Keep responsibilities clearly separated.
- Avoid speculative abstractions.
- Introduce only the minimum functionality required to satisfy this prompt.
- Preserve the offline-first architecture established in Prompt 022.

### In Scope

- Add a prompt template entry form.
- Allow users to enter:
  - Title
  - Category
  - Description
  - Prompt Text
  - Tags
- Save new templates to the existing JSON prompt store.
- Refresh the displayed template list immediately after saving.
- Extend the existing ViewModel with create/save behaviour.
- Continue using dependency injection and the JSON-backed prompt store.

### Out of Scope

- Editing existing templates.
- Deleting templates.
- Categories management.
- Search or filtering.
- API synchronisation.
- SQLite.
- Cloud storage.
- AI integration.
- Import/export.

---

## Expected Outcome

After completing this prompt:

- Users can create a new prompt template.
- Saving writes the template to the local JSON file.
- The template immediately appears in the list.
- Restarting the application reloads the saved template.
- The application remains fully functional offline.

---

## Implementation Notes

- Reuse the PromptCard model introduced in Prompt 022 where appropriate.
- Keep business logic inside the ViewModel.
- Keep persistence inside the JSON prompt store.
- Keep the UI focused on data binding.
- Continue following MVVM principles.
- Use command binding (`ICommand` or `AsyncRelayCommand`) for user actions rather than code-behind event handlers.

## Expected Files

### New Files

No new architectural layers are expected.

The implementation should primarily extend the existing ViewModel and View created in Prompt 022.

No new services, models, or architectural components should be introduced unless they are essential to support the prompt editor. Prefer extending the MVVM structure established in Prompt 022 rather than introducing additional abstractions.

### Updated Files

Typical files likely to change include:

- MainWindow.axaml
- MainWindowViewModel.cs
- JsonPromptCardStore.cs
- Any supporting dependency injection registration.

---

## Implementation Steps

1. Extend the ViewModel with editable template properties.
2. Bind the editor controls to the ViewModel.
3. Implement a ViewModel save command.
4. Bind the Save button to the ViewModel command.
5. Persist new templates using the existing JSON store and refresh the observable collection.
6. Verify persistence by restarting the application.

## Success Criteria

- Prompt template form is displayed.
- Save button persists a new template.
- Prompt list refreshes automatically.
- Existing templates continue to load correctly.
- dotnet build succeeds.
- dotnet test succeeds.
- No API dependency has been introduced.

## Definition of Done

This prompt is considered complete when:

- A user can create a new prompt template.
- The template is persisted to the local JSON store.
- The list refreshes without restarting the application.
- Existing functionality from Prompt 022 continues to work unchanged.
- The solution builds successfully.
- All automated tests continue to pass.

## Verification

Verify the implementation by confirming:

- A new template can be entered.
- Clicking Save creates a new JSON entry.
- The new template immediately appears in the list.
- Restarting the application reloads the template.
- Existing templates continue to load correctly.
- `dotnet build` succeeds.
- `dotnet test` succeeds.

## Result

Status: Completed.

Prompt 023 transforms the Avalonia client from a read-only offline browser into the first usable desktop prompt authoring tool.

Implemented outcomes:

- Added editable prompt template fields to the Avalonia ViewModel.
- Added a prompt template entry form to the main Avalonia window.
- Added save behaviour using MVVM command binding rather than code-behind event handlers.
- Persisted new templates using the existing JSON prompt store.
- Refreshed the prompt list after saving.
- Confirmed the application builds and runs successfully.

Files updated:

- MainWindow.axaml
- MainWindowViewModel.cs
- JsonPromptCardStore.cs

Build:

- `dotnet build` succeeded.

Tests:

- `dotnet test` succeeded.

Future prompts can safely build on this foundation by introducing editing, searching, categorisation, synchronisation, and AI-assisted capabilities without requiring architectural changes.

---

## Lessons Learnt

- Introduce one desktop capability at a time.
- Keep UI logic inside the ViewModel.
- Reuse the existing JSON persistence layer.
- Maintain clear separation between presentation and persistence.
- Prefer incremental evolution over large feature additions.
- Build reusable capabilities incrementally so each prompt provides a complete, independently verifiable improvement.
- Maintain MVVM separation by preferring command binding over UI event handlers wherever practical.