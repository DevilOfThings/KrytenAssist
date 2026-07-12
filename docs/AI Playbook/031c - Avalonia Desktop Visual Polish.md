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

- [ ] Compact branded application header
- [ ] Motto displayed beneath the application title
- [ ] Consistent accent colour
- [ ] Consistent border styling
- [ ] Improved button hierarchy
- [ ] Cleaner prompt cards
- [ ] Cleaner conversation presentation
- [ ] Meaningful status colours
- [ ] No behavioural changes
- [ ] Solution builds successfully
- [ ] Existing tests continue to pass
- [ ] Header communicates the Kryten brand identity.
- [ ] Colours remain restrained and professional.
- [ ] Styling is implemented using shared resources where practical.
- [ ] Visual changes have been manually reviewed.

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

> To be completed after implementation.

### Status

_Not Started_

### Files Created

-

### Files Updated

-

### Build

_Not Run_

### Tests

_Not Run_

### Manual Verification

_Not Performed_

### Git Commit

_Not Created_

---

# Lessons Learned

> To be completed after implementation.