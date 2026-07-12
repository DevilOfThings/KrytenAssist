# Prompt 031c – Avalonia Desktop Visual Polish

## Goal

Refine the visual appearance of the Kryten Assist Avalonia desktop client without changing application behaviour, layout architecture or functionality.

Prompt 031b established the desktop workspace.

This prompt improves the visual identity, consistency and usability of that workspace.

---

## Why This Prompt Exists

The application now has a functional desktop layout.

The next step is to make the interface feel like a polished desktop application rather than a collection of functional controls.

Visual improvements should be subtle.

The objective is clarity and professionalism rather than decoration.

---

## Scope

Implement only visual refinements.

### In Scope

- Refine the application header.
- Introduce a subtle Kryten colour palette.
- Improve border consistency.
- Improve spacing consistency where required.
- Improve typography hierarchy.
- Improve button emphasis.
- Improve status presentation.
- Improve prompt card appearance.
- Improve conversation message presentation.
- Improve overall visual consistency.

### Out of Scope

Do not modify:

- ViewModels
- Commands
- Application behaviour
- AI conversations
- Prompt creation logic
- Offline storage
- Tool framework
- Runtime context
- Navigation
- Window layout established in Prompt 031b

---

# Design Philosophy

The desktop application should resemble a modern professional desktop tool.

Good references include:

- Visual Studio
- JetBrains Rider
- GitHub Desktop
- Azure Data Studio

Avoid making the interface resemble a brightly coloured web application.

Professional, restrained styling is preferred.

---
# Kryten Brand Identity

The Kryten Assist desktop client should communicate:

- Professional
- Calm
- Technical
- Helpful
- Trustworthy

The interface should resemble a high-quality desktop engineering tool rather than a consumer web application.

When making styling decisions, prefer subtle refinement over decoration.

Avoid:

- bright gradients
- excessive colour
- large icons
- decorative graphics
- cartoon styling
- excessive animations

---

# Consistency

Where styling changes are introduced:

- reuse existing resources
- avoid duplicated styles
- avoid hard-coded values where shared resources are appropriate
- keep spacing and sizing consistent across similar controls

Prefer shared brushes, styles and theme resources.

Where a new Kryten accent colour is introduced, define it once and reuse it throughout the application.

Avoid repeating identical styling across multiple controls.


---


# Header

Replace the plain header with a branded application header.

Include:

- Kryten Assist
- the project motto

> Making Future Robin's Life Easier

- existing provider or availability status
- existing workspace actions

Introduce a subtle accent background.

The header should remain compact.

Do not increase the vertical footprint significantly.

---

# Colour Palette

Use colour sparingly.

Introduce a single accent colour that becomes the visual identity of Kryten Assist.

Use the accent colour for:

- primary buttons
- selected prompt
- keyboard focus
- important actions

Avoid multiple competing accent colours.

---

# Borders

Replace plain borders with a consistent subtle border style.

Where existing borders exist, use a shared border brush rather than leaving borders undefined.

Avoid heavy outlines.

---

# Buttons

Establish a clear distinction between:

Primary

- Send
- Save
- New Prompt

Secondary

- Cancel
- Clear Conversation

Primary actions should draw the user's attention without becoming visually dominant.

---

# Prompt Cards

Improve prompt card presentation.

Examples include:

- improved spacing
- better title hierarchy
- cleaner category presentation
- softer borders
- improved hover feedback

Do not redesign the prompt card layout.

---

# Conversation

Improve readability.

Examples include:

- better spacing
- subtle distinction between user and assistant messages
- consistent typography
- improved busy indicator presentation

Do not introduce Markdown rendering.

---

# Status Colours

Use colour only where it communicates meaning.

Suggested usage:

- Green — Ready
- Amber — Busy
- Red — Error

Avoid decorative status colours.

---

# Theme Resources

Prefer Avalonia theme resources.

Avoid hard-coded colours where a suitable theme resource exists.

Where a custom accent colour is required, define it centrally so it can be reused consistently.

---

# Accessibility

Maintain readable contrast.

Do not rely on colour alone to communicate state.

Support keyboard navigation.

Maintain usability in both light and dark themes if supported.

---

# Acceptance Criteria

- [x] Compact branded application header
- [x] Motto displayed beneath the application title
- [x] Consistent accent colour
- [x] Consistent border styling
- [x] Improved button hierarchy
- [x] Cleaner prompt cards
- [x] Cleaner conversation presentation
- [x] Meaningful status colours
- [x] No behavioural changes
- [x] Solution builds successfully
- [x] Existing tests continue to pass
- [x] Header communicates the Kryten brand identity.
- [x] Colours remain restrained and professional.
- [x] Styling is implemented using shared resources where practical.
- [x] Visual changes have been manually reviewed.

---

# Manual Review

Visual refinement requires human review.

After implementation:

- run the application
- inspect the desktop layout
- verify readability
- verify spacing
- verify colours
- verify behaviour has not changed

Do not continue refining the appearance automatically.

Stop after one implementation pass.

---

# Results

### Status

✅ Complete. The visual-polish pass was implemented, built, tested and manually reviewed. Button-alignment feedback from the review was addressed through the shared button style.

### Implementation Summary

- Added centralized light and dark Kryten theme dictionaries.
- Introduced a single restrained steel-blue accent for primary actions, focus indication and interactive feedback.
- Added shared semantic styles for headers, workspace surfaces, dialogs, statuses, prompt cards, conversation messages and typography.
- Added a compact branded header containing the application name, project motto and existing provider status.
- Established primary, secondary and tertiary button hierarchy.
- Centered all button content horizontally and vertically through the shared button style.
- Refined prompt-card hierarchy and hover feedback without introducing selection behavior.
- Improved conversation message, busy-state and error presentation.
- Preserved the Prompt 031b layout, MVVM architecture and all existing application behavior.

### Files Created

- `docs/Session Handovers/2026-07-12 Session 014.md`

### Files Updated

- `KrytenAssist.Avalonia/App.axaml`
- `KrytenAssist.Avalonia/MainWindow.axaml`
- `docs/Roadmap.md`
- `docs/AI Playbook/031c - Avalonia Desktop Visual Polish.md`

### Build

✅ `dotnet build KrytenAssist.sln`

### Tests

- Avalonia tests: 19 passed
- API tests: 9 passed
- Total: 28 passed, 0 failed

### Manual Verification

✅ Completed. Visual feedback identified button-label alignment, which was corrected centrally for all button variants. No further automatic visual iteration was performed.

### Git Commit

Not created.

---

# Lessons Learned

- Theme dictionaries allow the Kryten palette to remain restrained while preserving usable contrast in both light and dark modes.
- Semantic style classes keep visual hierarchy centralized and avoid repeating hard-coded colours throughout the view.
- Shared button alignment keeps labels consistently centred across every button hierarchy and surface.
- A single muted accent is sufficient for primary actions, focus indication, category labels and restrained hover feedback.
- Status styling should retain explicit text so busy and error information never depends on colour alone.
- The existing prompt library does not expose selection state. Adding selection solely for styling would change the established interaction model, so this pass improves hover feedback without introducing new selection behaviour.
