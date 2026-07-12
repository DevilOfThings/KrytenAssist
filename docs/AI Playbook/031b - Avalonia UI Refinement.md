# Prompt 031b – Avalonia Desktop UX Refinements

## Goal

Refine the Kryten Assist Avalonia desktop interface into a clearer, more practical two-pane workspace.

The existing functionality works, but the current layout does not use the available screen space efficiently. Prompt browsing, prompt creation and AI conversations should feel like parts of one coherent desktop assistant rather than separate controls stacked into a long scrolling page.

This prompt focuses entirely on desktop usability, layout and visual refinement.

No new AI, persistence, search, tool, memory or provider functionality should be introduced.

---

## Why This Prompt Exists

The Avalonia client has grown incrementally through the earlier prompts.

It now supports:

- offline prompt storage
- prompt creation
- categories
- keyword and semantic search
- AI conversations
- conversation memory
- tool calling
- runtime context injection

These features work, but they have been added to the original foundation layout.

As a result:

- the window requires excessive vertical scrolling
- prompt creation permanently occupies valuable workspace
- the prompt browser and conversation interface compete for space
- long prompt lists and long conversations are difficult to navigate
- the desktop experience lacks a clear visual hierarchy

Before adding further functionality, the application needs a stronger desktop workspace foundation.

---

## Scope

Implement only the Avalonia desktop UX refinement described in this prompt.

### In Scope

- Introduce a two-pane desktop workspace.
- Optimise the layout for laptop and tablet-sized desktop displays.
- Place prompt browsing and search in the left pane.
- Place the AI conversation workspace in the right pane.
- Allow the two primary panes to scroll independently where appropriate.
- Move prompt creation out of the permanent main layout.
- Add a clearly visible `New Prompt` action.
- Display prompt creation in a dialog, overlay or separate editor surface.
- Preserve all existing prompt creation behaviour.
- Improve spacing, alignment, visual hierarchy and control sizing.
- Improve conversation history readability.
- Keep the conversation input and primary actions readily accessible.
- Preserve existing bindings, commands and ViewModel behaviour.
- Retain support for resizing the application window.
- Ensure the application remains usable at smaller desktop window sizes.
- Build and run the existing tests.

### Out of Scope

Do not add:

- new AI features
- new tools
- new runtime context providers
- API integration
- prompt editing
- prompt deletion
- conversation persistence
- conversation search
- multiple conversations or chat tabs
- Markdown rendering
- rich text editing
- drag and drop
- theme switching
- new embedding behaviour
- new persistence formats
- navigation frameworks
- major architectural restructuring
- replacement of MVVM with code-behind logic

Do not redesign completed functionality unless a small adjustment is required to support the new layout.

---

## UX Direction

The refined desktop client should behave like a focused assistant workspace.

At a high level:

```text
┌──────────────────────────────────────────────────────────────┐
│ Kryten Assist                              Status / Actions   │
├─────────────────────────┬────────────────────────────────────┤
│ Prompt Library          │ Conversation                       │
│                         │                                    │
│ Search                  │ Conversation history               │
│ Categories / Status     │                                    │
│                         │                                    │
│ Prompt list             │                                    │
│                         │                                    │
│                         ├────────────────────────────────────┤
│                         │ Message input and actions          │
│                         │                                    │
│ New Prompt              │ Send / Cancel / Clear              │
└─────────────────────────┴────────────────────────────────────┘
```

This diagram describes the intended hierarchy rather than exact styling.

Codex may adapt the details to fit the existing controls and Avalonia implementation.

---

## Layout Requirements

### Application Header

Provide a compact application header that includes:

- Kryten Assist branding
- a short supporting subtitle where space allows
- relevant provider or availability status already exposed by the ViewModel
- primary workspace actions where appropriate

The header should not consume excessive vertical space.

Do not place large decorative elements above the working area.

---

### Left Pane – Prompt Library

The left pane should contain the prompt discovery workflow.

It should include:

- prompt search
- provider or semantic-search status already shown by the application
- category suggestions or filters
- prompt count or no-results information
- the existing prompt list
- a `New Prompt` action

The prompt list must have its own usable scrolling region.

The search controls and `New Prompt` action should remain accessible while navigating a long prompt list.

The left pane should be wide enough to display prompt titles and useful summary information, but it should not dominate the conversation workspace.

A starting width of approximately 30–40% of the usable workspace is appropriate.

Do not hard-code the interface to only one exact screen size.

---

### Right Pane – Conversation Workspace

The right pane should be the primary working area.

It should contain:

- the existing conversation history
- busy and error information
- the current message input
- Send
- Cancel
- Clear Conversation

The conversation history should use the majority of the available vertical space.

The message input and actions should remain accessible without requiring the user to scroll to the bottom of the entire application window.

The conversation history should scroll independently from the prompt list.

Improve message spacing and readability without introducing rich Markdown rendering.

Preserve the current distinction between user and assistant messages.

---

## Prompt Creation

The prompt creation form should no longer permanently occupy the main workspace.

Add a clearly labelled:

```text
New Prompt
```

action in the prompt-library area or application header.

Selecting this action should open the existing prompt editor in one of the following forms:

- a modal dialog
- an in-window overlay
- a dedicated editor window

Prefer the simplest approach that integrates safely with the existing architecture.

The prompt editor must preserve:

- title entry
- category entry and suggestions
- description entry
- prompt text entry
- tags entry
- validation
- saving to the existing offline prompt store
- automatic refresh of the prompt list
- automatic refresh of known categories
- success and error behaviour

The editor should include clear Save and Cancel/Close actions.

Closing or cancelling the editor must not save incomplete changes.

Do not move prompt-saving logic into code-behind.

Small amounts of view-only orchestration may be used where Avalonia requires it, but application behaviour must remain in the ViewModel or existing services.

---

## Resizing and Minimum Usability

The layout should resize sensibly.

At normal laptop or tablet-style desktop widths:

- both panes should remain visible
- the conversation pane should retain the larger share of space
- controls should not overlap
- important buttons should not be clipped

At smaller widths:

- content may compress
- text may wrap
- reasonable minimum pane widths may be used

Do not implement a full mobile or responsive navigation system in this prompt.

A minimum usable window size may be introduced if it improves reliability.

---

## Visual Refinement

Improve:

- consistent outer margins
- spacing between related controls
- alignment of labels and actions
- button sizing
- visual grouping
- hierarchy between headings, status text and body content
- readable prompt cards
- readable conversation messages
- empty-state presentation

Use existing Avalonia theme resources where practical.

Avoid:

- excessive borders
- deeply nested cards
- large decorative headers
- cramped controls
- unnecessary colours
- hard-coded styling repeated across many controls
- introducing a new external UI library

This is a refinement prompt, not a complete visual redesign.

---

## Architecture Requirements

Preserve the existing Avalonia MVVM architecture.

### ViewModels

Existing ViewModels and commands should continue to own application behaviour.

ViewModel changes are permitted only where necessary to support:

- opening and closing the prompt editor
- exposing UI state required by the refined layout
- preserving existing prompt creation behaviour

Do not add layout-specific business logic to services.

---

### Views

XAML should own:

- layout
- spacing
- presentation
- control templates
- visual states

Avoid placing application logic into code-behind.

If a dialog or separate window requires view-level orchestration, keep it minimal and document why it is required.

---

### Services

Do not modify:

- conversation provider architecture
- tool execution
- memory behaviour
- runtime context injection
- embedding providers
- offline prompt persistence

unless a compile-time adjustment is unavoidable.

Any unavoidable modification outside the UI area must be reported clearly.

---

## Suggested Implementation Passes

Because visual refinement requires manual inspection, implement this prompt incrementally.

### Pass 1 – Structural Layout

Implement:

- compact application header
- two-pane workspace
- independent prompt-list scrolling
- independent conversation-history scrolling
- fixed conversation composer area
- removal of the permanent prompt editor from the main layout
- `New Prompt` action
- prompt editor dialog, overlay or window

Build and run tests after this pass.

Stop so that the application can be manually inspected.

---

### Pass 2 – Visual Refinement

After manual feedback, refine:

- pane proportions
- spacing
- prompt card density
- conversation message presentation
- headings
- button placement
- minimum dimensions
- wrapping and alignment
- keyboard interaction and desktop usability
- conversation scrolling behaviour

Implement the following desktop interaction improvements where appropriate:

- Pressing **Enter** sends the current message.
- Pressing **Shift+Enter** inserts a newline without sending.
- Empty or whitespace-only messages must not be sent.
- Keyboard focus should remain in the message input after sending.
- The conversation should automatically scroll to the newest message whenever new messages are added.
- Preserve chronological conversation order (oldest to newest).

Do not assume the first structural implementation is the final visual design.

---

### Pass 3 – Final Polish

After a second manual review:

- resolve remaining clipping or scrolling issues
- ensure layout remains usable at representative window sizes
- remove redundant visual elements
- verify keyboard and pointer usability
- perform the final build and test run

Only perform this pass when specifically requested.

---

## Acceptance Criteria

Prompt 031b is complete when:

- [x] The application uses a clear two-pane desktop workspace.
- [x] Prompt browsing is located in the left pane.
- [x] AI conversations are located in the right pane.
- [x] The prompt list scrolls independently.
- [x] The conversation history scrolls independently.
- [x] The conversation composer remains readily accessible.
- [x] Prompt creation no longer permanently occupies the main layout.
- [x] A clear `New Prompt` action opens the prompt editor.
- [x] Prompt creation retains all existing behaviour.
- [x] Existing category suggestions remain available.
- [x] Prompt search and semantic search continue to work.
- [x] AI conversations continue to work.
- [x] Clear Conversation continues to work.
- [x] Send and Cancel continue to work.
- [x] Existing provider-status information remains visible.
- [x] The application is usable at common laptop and tablet-style window sizes.
- [x] No core AI functionality has changed.
- [x] No provider-specific types have leaked into shared abstractions.
- [x] The solution builds successfully.
- [x] All existing tests pass.
- [x] The interface has been manually reviewed and refined through at least one feedback pass.

---

## Codex Working Instructions

Before making changes:

1. Read `AGENTS.md`.
2. Read `docs/Roadmap.md`.
3. Read this prompt in full.
4. Inspect the current Avalonia XAML, ViewModels, commands and prompt editor implementation.
5. Identify the minimum files required for the structural first pass.

Implement **Pass 1 only** initially.

Do not proceed automatically into subjective visual polishing.

Preserve existing behaviour and bindings.

Do not inspect or modify unrelated API, React, persistence or infrastructure code unless required to resolve a build error introduced by this prompt.

After the structural pass:

1. Run `dotnet build`.
2. Run the relevant Avalonia tests.
3. Run `dotnet test` if practical.
4. Stop and provide a summary.

The application will then be run and visually inspected by the user before further changes are requested.

---

## Expected Files

The exact files should be confirmed after inspecting the current implementation.

Likely files include:

- `KrytenAssist.Avalonia/MainWindow.axaml`
- `KrytenAssist.Avalonia/MainWindow.axaml.cs`
- existing Avalonia style or resource files
- the current prompt editor view
- the current prompt editor ViewModel
- `KrytenAssist.Avalonia/ViewModels/MainWindowViewModel.cs`

Do not create unnecessary abstractions or restructure unrelated files.

---

# Results

### Status

✅ Prompt 031b complete after all three implementation passes.

### Passes Completed

- Pass 1 established the compact header, two-pane workspace, independent scrolling and prompt-editor overlay.
- Pass 2 refined spacing and readability, added Enter/Shift+Enter behavior, retained input focus and added automatic conversation scrolling.
- Pass 3 bounded long composer input, kept prompt-editor actions visible during form scrolling, improved action alignment and removed the ambiguous `Clear` label.

### Files Created

- `docs/AI Playbook/031b - Pass 2.md`

### Files Updated

- `KrytenAssist.Avalonia/MainWindow.axaml`
- `KrytenAssist.Avalonia/MainWindow.axaml.cs`
- `KrytenAssist.Avalonia/ViewModels/MainWindowViewModel.cs`
- `docs/Roadmap.md`
- `docs/AI Playbook/031b - Avalonia UI Refinement.md`
- `docs/AI Playbook/031b - Pass 2.md`

### Build

✅ `dotnet build KrytenAssist.sln`

### Tests

- Avalonia tests: 19 passed
- API tests: 9 passed
- Total: 28 passed, 0 failed

### Manual Verification

- ✅ Pass 1 manually reviewed.
- ✅ Pass 2 manually reviewed, including corrected Enter and Shift+Enter behavior.
- Pass 3 final sizing and fixed-action layout are ready for visual confirmation.

### Git Commit

Not created.

---

# Lessons Learned

- Separating the prompt library and conversation into independently scrolling panes makes long-running use practical without changing application behavior.
- Keyboard handling for a multiline Avalonia `TextBox` must use the tunnelling route so plain Enter can send while Shift+Enter retains native newline behavior.
- Focus and scrolling are view concerns; keeping their minimal orchestration in code-behind preserves command and conversation behavior in the ViewModel.
- Bounding the composer prevents long drafts from displacing conversation history.
- Keeping prompt-editor actions outside the form scroll area makes Save and Cancel consistently accessible at smaller window heights.
- Incremental visual passes with manual review reduce the risk of over-designing the desktop workspace.
