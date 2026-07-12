# Prompt 031d – Avalonia Prompt Management

## Goal

Introduce complete prompt management capabilities to the Avalonia desktop client.

Users should be able to create, select, use, edit and delete prompts through a consistent desktop workflow.

This prompt extends the prompt library while preserving the architecture established in previous prompts.

The existing prompt editor should be reused rather than creating a separate editor for editing.

---

## Why This Prompt Exists

Prompt 031b refined the desktop workflow and Prompt 031c polished the visual appearance.

The desktop application is now pleasant to use, but prompt management is incomplete.

Users can currently:

- Create prompts
- Browse prompts
- Search prompts
- Select a stored prompt for use in the AI conversation
- Chat with AI

However they cannot:

- Edit existing prompts
- Delete prompts
- Apply a stored prompt to the conversation composer
- Maintain categories created during experimentation

As the prompt library grows, these capabilities become essential.

---

## Scope

Implement prompt management only.

No cloud synchronisation, undo, version history, prompt sharing, bulk editing or category administration screens should be introduced.

---

# In Scope

## Use Prompt in Conversation

Allow the user to select a stored prompt and use its prompt text within the AI conversation.

The preferred interaction should be:

- Single-click selects the prompt card.
- A visible **Use Prompt** action copies the selected prompt into the conversation composer.

Double-click should remain reserved for opening the prompt in Edit Mode.

Using a prompt should:

- copy the stored `PromptText` into the conversation input
- preserve the selected prompt in the prompt library
- move keyboard focus to the conversation input
- allow the user to review or edit the text before sending
- never send the conversation automatically
- never modify the stored prompt

If the conversation input already contains text, do not overwrite it silently.

Instead either:

- append the prompt using a clear separator, or
- ask for confirmation before replacing the existing text.

The implementation should reuse the existing selected-prompt state and preserve the MVVM architecture.

---

## Edit Existing Prompt

Allow an existing prompt to be edited.

The preferred interaction should be:

- Double-click a prompt card

Alternative secondary interactions (optional):

- Edit button
- Context menu

The existing modal editor should open in **Edit Mode**.

The editor should:

- populate all existing values
- preserve validation
- preserve tags
- preserve category
- preserve description
- preserve prompt text

The dialog title should change appropriately.

Examples:

```
Create Prompt
```

becomes

```
Edit Prompt
```

The primary action button should become:

```
Save Changes
```

rather than creating a new prompt.

Saving should update the existing prompt.

No duplicate prompt should be created.

---

## Delete Prompt

Allow prompts to be deleted.

Deletion should require confirmation.

Example:

```
Delete Prompt

Delete "SQL Server Query Optimisation"?

This action cannot be undone.

[Delete]

[Cancel]
```

Deletion should immediately refresh:

- Prompt Library
- Search Results
- Category Suggestions

without restarting the application.

---

## Prompt Selection

Improve prompt selection behaviour.

The currently selected prompt should remain visually distinct.

The selected prompt should also be the prompt used by the **Use Prompt** action.

Selection should update consistently after:

- editing
- deleting
- searching
- creating

Avoid invalid selections.

---

## Prompt List Refresh

Refreshing should preserve the current search filter where practical.

Avoid unnecessarily rebuilding the entire UI.

---

## Category Behaviour

Before implementing category deletion, inspect the current architecture.

Categories are currently derived from stored prompts.

Do **not** introduce a separate category repository unless absolutely necessary.

If categories continue to be discovered dynamically:

- removing or editing the last prompt in a category should naturally remove that category from suggestions.

No additional work is required.

---

## User Experience

Editing should feel almost identical to creating.

Only small differences should exist:

- dialog title
- primary button text
- existing values pre-populated

Everything else should remain familiar.

Using a prompt should also feel natural.

The expected workflow is:

```
Browse
→ Select Prompt
→ Use Prompt
→ Review
→ Send
```

The user should remain in complete control of the final message before it is sent.

---

## Data Integrity

Editing must preserve:

- Prompt Id
- Created date

while updating:

- Updated date

Using a prompt must never modify the stored prompt.

---

## Validation

Existing validation must continue to operate.

Saving invalid data should not overwrite an existing prompt.

---

## Architecture

Maintain MVVM.

Keep business logic inside ViewModels and services.

Avoid adding editing logic directly into the View.

Reuse existing commands and selection state where practical.

---

## Out of Scope

Do not implement:

- Prompt version history
- Undo / Redo
- Prompt duplication
- Automatically sending a prompt when selected
- Prompt export
- Prompt import
- Prompt favourites
- Category editor
- Category rename
- Category colours
- Drag-and-drop organisation
- Multi-selection
- Bulk delete
- Cloud synchronisation
- API integration

These will be addressed by future prompts.

---

# Implementation Notes

Reuse the existing prompt editor.

Do not create separate CreatePromptWindow and EditPromptWindow implementations.

Instead support two operating modes:

```
Create
```

and

```
Edit
```

with the same dialog.

Likewise, reuse the existing prompt selection mechanism when implementing **Use Prompt**.

Avoid introducing duplicate state.

---

# Suggested Order

1. Add selected-prompt support for conversation use.
2. Add the **Use Prompt** action.
3. Populate the conversation composer without sending automatically.
4. Add edit mode support.
5. Populate existing prompt values.
6. Save changes.
7. Add delete command.
8. Add confirmation dialog.
9. Refresh the prompt library.
10. Refresh category suggestions.
11. Verify selection behaviour.

---

# Acceptance Criteria

The user can:

- Create prompts.
- Select and apply prompts to the conversation composer.
- Edit prompts.
- Delete prompts.
- Search prompts.
- Continue chatting.

Editing never creates duplicate prompts.

Using a prompt never sends automatically and never modifies the stored prompt.

Deleting immediately updates the UI.

Category suggestions remain accurate.

The application builds cleanly.

All existing tests continue to pass.

---

# Results

## Status

Implementation complete. Awaiting manual workflow verification.

## Files Created

- `KrytenAssist.Avalonia.Tests/ViewModels/MainWindowViewModelTests.cs`
- `docs/Session Handovers/2026-07-12 Session 015.md`

## Files Updated

- `KrytenAssist.Avalonia/App.axaml`
- `KrytenAssist.Avalonia/MainWindow.axaml`
- `KrytenAssist.Avalonia/MainWindow.axaml.cs`
- `KrytenAssist.Avalonia/ViewModels/MainWindowViewModel.cs`
- `docs/Roadmap.md`
- `docs/AI Playbook/031d - Avalonia Prompt Management.md`

## Build

✅ `dotnet build`

## Tests

- Avalonia tests: 25 passed
- API tests: 9 passed
- Total: 34 passed, 0 failed

## Manual Verification

Required. Verify selection styling, Use Prompt focus and append behavior, double-click editing, create/edit labels, validation, delete confirmation, search-filtered edits/deletes and category removal when the final prompt in a category is changed or deleted.

## Git Commit

Not created.

---

# Lessons Learned

- A single `SelectedPrompt` property can drive native list selection, selected styling, Use Prompt, edit and delete without duplicating state.
- Reusing the existing editor safely requires explicit create/edit mode state and property notifications when stored values are loaded programmatically.
- Replacing an edited prompt by ID preserves its identity and creation date while avoiding duplicate records.
- Reapplying the active search after every save or delete keeps filtered results accurate and prevents stale selections.
- Dynamically deriving categories from the refreshed prompt collection naturally removes unused categories without a separate repository.
- Use Prompt should append behind an explicit separator when the composer already contains text so user-authored work is never silently overwritten.
- Confirmation can remain MVVM-driven with a small overlay, leaving deletion and refresh logic testable in the ViewModel.
- Double-click handling belongs on the prompt card itself so empty list space cannot accidentally open the selected prompt for editing.
