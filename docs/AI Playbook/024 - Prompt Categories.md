

# Prompt 024 – Avalonia Prompt Categories

## Goal

Add category discovery and category selection support to the Avalonia desktop prompt editor.

This prompt builds on Prompt 023 by making categories more useful when creating prompt templates. Existing categories should be discovered from the local JSON prompt store and made available in the UI so users can reuse them consistently.

This is a foundation prompt only.

No category management screen, category editing, category deletion, category IDs, category colours, API synchronisation, search, or AI integration should be added in this prompt.

---

## Why This Prompt Exists

Prompt 023 introduced the first offline prompt template editor.

Users can now create templates, but categories are still entered as free text. This makes it easy to accidentally create inconsistent categories such as `Coding`, `code`, `Development`, or `Dev`.

This prompt introduces the first lightweight category support by deriving categories from existing prompt templates and making them visible during template creation.

---

## Scope

Implement only lightweight prompt category discovery and reuse.

## Architecture

This prompt builds directly on the offline JSON prompt store and MVVM structure introduced in Prompts 022 and 023.

The implementation should continue following the MVVM pattern:

- The View owns layout and data binding.
- The ViewModel owns category discovery and selection behaviour.
- The JSON prompt store remains the persistence mechanism.
- Categories remain simple strings on prompt templates.
- Dependency Injection continues to resolve application services.

## Design Principles

The implementation should favour incremental evolution over expansion.

- Reuse the existing `Category` string on `PromptCardModel`.
- Do not introduce a separate category entity or store.
- Extend the existing ViewModel before creating new services.
- Keep all behaviour offline-first.
- Avoid speculative category management features.

### In Scope

- Discover categories from existing prompt templates.
- Expose discovered categories from the ViewModel.
- Display available categories as selectable chips beneath the category input.
- Allow users to reuse an existing category when creating a prompt template.
- Continue allowing users to type a new category.
- Refresh the category list after saving a new prompt template.

### Out of Scope

- Dedicated category management screen.
- Creating categories independently of prompt templates.
- Editing categories.
- Deleting categories.
- Category IDs.
- Category colours or icons.
- Category ordering preferences.
- Search or filtering.
- API synchronisation.
- SQLite.
- Cloud storage.
- AI integration.

---

## Expected Outcome

After completing this prompt:

- Existing categories are discovered from saved prompt templates.
- Categories are visible as reusable selectable chips in the prompt editor.
- Users can reuse an existing category when creating a template.
- Users can still type a new category when needed.
- Saving a template refreshes both the prompt list and the category list.
- The application remains fully functional offline.

---

## Implementation Notes

- Keep category values as strings on `PromptCardModel`.
- Add an observable category collection to the existing ViewModel.
- Derive categories from loaded prompt cards using distinct, non-empty category values.
- Keep category discovery inside the ViewModel for now.
- Keep the category input as a normal text field and display discovered categories as selectable chips beneath it.
- Selecting a category chip should populate the category input while still allowing users to type a completely new category. Keep the implementation fully MVVM and avoid code-behind event handling.
- Continue using command binding for user actions.
- Do not introduce a new category persistence model.

## Expected Files

### New Files

No new files are expected.

No new services, models, or architectural components should be introduced unless they are essential to support category selection.

### Updated Files

Typical files likely to change include:

- MainWindow.axaml
- MainWindowViewModel.cs

---

## Implementation Steps

1. Add a categories collection to the ViewModel.
2. Populate categories from loaded prompt cards.
3. Update `LoadAsync()` so categories refresh whenever prompt cards are loaded.
4. Keep the category TextBox and display discovered categories as selectable chips beneath it.
5. Ensure users can still enter a new category manually.
6. Verify that saving a new prompt refreshes both prompts and categories.

## Success Criteria

- Existing categories are displayed in the Avalonia UI.
- The category list is derived from saved prompt templates.
- Duplicate and empty categories are excluded.
- Users can reuse an existing category by selecting a category chip.
- Users can still type a new category.
- Saving a prompt refreshes the category list.
- `dotnet build` succeeds.
- `dotnet test` succeeds.
- No API dependency has been introduced.

## Definition of Done

This prompt is considered complete when:

- Categories are discovered from local prompt templates.
- Categories are available as selectable chips during prompt creation.
- New categories entered by users are saved as part of the prompt template.
- The category list refreshes after saving.
- Existing Prompt 023 editor behaviour continues to work unchanged.
- The solution builds successfully.
- All automated tests continue to pass.

## Verification

Verify the implementation by confirming:

- Existing saved template categories appear in the UI.
- Selecting an existing category chip populates the category input.
- Typing a new category works.
- Saving a prompt with a new category adds that category to the available category list.
- Empty category values are not displayed as category options.
- Existing templates continue to load correctly.
- `dotnet build` succeeds.
- `dotnet test` succeeds.

## Result

Status: Completed.

Prompt 024 introduced lightweight offline category discovery and reuse for the Avalonia desktop client.

Implemented outcomes:

- Added automatic category discovery from existing prompt templates.
- Exposed categories through the ViewModel.
- Displayed reusable category chips beneath the category input.
- Allowed users to populate the category field by selecting a category chip.
- Continued supporting manual entry of new categories.
- Refreshed both prompts and categories after saving.
- Confirmed categories persisted correctly after restarting the application.
- Confirmed the application builds and runs successfully.

Files updated:

- MainWindow.axaml
- MainWindowViewModel.cs

Build:

- `dotnet build` succeeded.

Tests:

- `dotnet test` succeeded.

Future prompts can build on this foundation by introducing category management, search, filtering, favourites, and AI-assisted prompt organisation without changing the underlying architecture.

---

## Lessons Learnt

- Reuse existing prompt metadata before introducing new persistence models.
- Keep category discovery as a derived ViewModel concern rather than a stored entity.
- Selectable chips provide a cleaner user experience than an editable ComboBox for lightweight selection.
- Maintain MVVM by handling chip selection through commands rather than code-behind.
- Continue evolving the desktop client through small, independently verifiable improvements.