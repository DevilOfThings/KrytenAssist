

# Prompt 025 – Avalonia Offline Prompt Search

## Goal

Add simple offline search to the Avalonia desktop prompt editor.

This prompt builds on Prompts 022–024 by allowing users to quickly find locally stored prompt templates using a basic text search box.

This is a foundation prompt only.

No semantic search, embeddings, AI ranking, saved searches, advanced filters, API synchronisation, or cloud storage should be added in this prompt.

---

## Why This Prompt Exists

The Avalonia client can now load prompt templates offline, create new templates, and reuse categories.

As the local prompt library grows, users need a simple way to find templates without scrolling through the entire list.

This prompt introduces the first search capability while keeping everything offline and JSON-backed.

---

## Scope

Implement only basic local text search over the loaded prompt templates.

## Architecture

This prompt builds directly on the offline JSON prompt store, MVVM structure, prompt editor, and category support introduced in Prompts 022–024.

The implementation should continue following the MVVM pattern:

- The View owns layout and data binding.
- The ViewModel owns search text, filtering behaviour, and filtered results.
- The JSON prompt store remains the persistence mechanism.
- Search operates only on locally loaded prompt templates.
- Dependency Injection continues to resolve application services.

## Design Principles

The implementation should favour incremental evolution over expansion.

- Keep search local and offline.
- Keep `PromptCards` as the full loaded collection.
- Introduce a filtered collection for display.
- Avoid introducing search services or indexing infrastructure.
- Avoid semantic search or AI ranking.
- Keep the behaviour easy to verify manually.

### In Scope

- Add a search text input to the Avalonia UI.
- Add a `SearchText` property to the ViewModel.
- Add a filtered prompt collection for display.
- Filter prompt templates as the user types.
- Search across:
  - Title
  - Category
  - Description
  - Prompt Text
  - Tags
- Show all prompts when the search box is empty.
- Show a simple no-results message when no prompts match.
- Refresh filtered results after saving a new prompt template.

### Out of Scope

- Semantic search.
- Embeddings.
- AI ranking.
- Highlighting matched text.
- Advanced filters.
- Category-only filters.
- Tag filters.
- Saved searches.
- Search history.
- Search indexing.
- API synchronisation.
- SQLite.
- Cloud storage.
- AI integration.

---

## Expected Outcome

After completing this prompt:

- Users can type into a search box.
- The displayed prompt list updates as the search text changes.
- Search matches prompt title, category, description, prompt text, and tags.
- Clearing the search box restores the full prompt list.
- A no-results message appears when nothing matches.
- The application remains fully functional offline.

---

## Implementation Notes

- Keep the existing `PromptCards` collection as the complete local collection.
- Add a separate filtered collection for the UI to display.
- Rebuild the filtered collection whenever prompts load, prompts are saved, or search text changes.
- Keep filtering logic inside the ViewModel for now.
- Use simple case-insensitive substring matching.
- Treat null or empty fields safely.
- Continue using MVVM binding and avoid code-behind event handlers.
- Do not introduce a new search service yet.

## Expected Files

### New Files

No new files are expected.

No new services, models, or architectural components should be introduced unless they are essential to support basic offline search.

### Updated Files

Typical files likely to change include:

- MainWindow.axaml
- MainWindowViewModel.cs

---

## Implementation Steps

1. Add a `SearchText` property to the ViewModel.
2. Add a filtered prompt collection to the ViewModel.
3. Update loading so both the full and filtered prompt collections are populated.
4. Add filtering logic that searches title, category, description, prompt text, and tags.
5. Rebuild filtered results whenever `SearchText` changes.
6. Update the UI to bind the prompt list to the filtered collection.
7. Add a search box above the prompt list.
8. Add a simple no-results message.
9. Verify that saving a new prompt refreshes the filtered results.

## Success Criteria

- Search box is displayed in the Avalonia UI.
- Typing search text filters the prompt list immediately.
- Search matches title, category, description, prompt text, and tags.
- Search is case-insensitive.
- Clearing the search restores all prompts.
- No-results state is shown when nothing matches.
- Saving a new prompt refreshes search results correctly.
- `dotnet build` succeeds.
- `dotnet test` succeeds.
- No API dependency has been introduced.

## Definition of Done

This prompt is considered complete when:

- Users can search locally stored prompt templates.
- Search results update as the user types.
- The full prompt list returns when search text is cleared.
- No-results behaviour is visible and understandable.
- Existing prompt creation and category chip behaviour continues to work unchanged.
- The solution builds successfully.
- All automated tests continue to pass.

## Verification

Verify the implementation by confirming:

- Searching by title works.
- Searching by category works.
- Searching by description works.
- Searching by prompt text works.
- Searching by tag works.
- Search is case-insensitive.
- Clearing the search box restores all prompt templates.
- A search with no matches shows the no-results message.
- Creating a new prompt updates the searchable collection.
- Existing category chips continue to work.
- `dotnet build` succeeds.
- `dotnet test` succeeds.

## Result

Status: Completed.

Prompt 025 introduced the first offline search capability for the Avalonia desktop client.

Implemented outcomes:

- Added a search box to the prompt browser.
- Added `SearchText` and a filtered prompt collection to the ViewModel.
- Implemented live, case-insensitive filtering as the user types.
- Enabled searching across title, category, description, prompt text, and tags.
- Added a simple no-results message when no prompts match.
- Ensured filtered results refresh after loading and saving prompt templates.
- Confirmed the application builds, runs, and remains fully functional offline.

Files updated:

- MainWindow.axaml
- MainWindowViewModel.cs

Build:

- `dotnet build` succeeded.

Tests:

- `dotnet test` succeeded.

Future prompts can build on this foundation by introducing favourites, sorting, category filters, semantic search, and AI-assisted ranking without changing the underlying search architecture.

---

## Lessons Learnt

- Keep the complete prompt collection separate from the filtered collection presented to the UI.
- Centralise filtering logic in a single ViewModel method to simplify future enhancements.
- Live search provides a significantly better user experience than manual search actions.
- Simple case-insensitive text search provides a solid offline foundation before introducing semantic search.
- Continue evolving search incrementally so future prompts can add richer capabilities without redesigning the architecture.