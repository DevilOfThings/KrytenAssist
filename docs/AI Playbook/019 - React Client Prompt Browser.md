

# Prompt 019 – Prompt Browser

## Goal

Build a usable Prompt Browser in the React client so Future Robin can browse, search and filter Prompt Cards returned from the Kryten Assist API.

This prompt builds on Prompt 018, where the first React client was created and connected to the backend API.

## Scope

The Prompt Browser should:

- Display a clear page heading.
- Display the number of Prompt Cards found.
- Show loading, error and empty states.
- Render Prompt Cards using a reusable card component.
- Display each Prompt Card's title, category, description, prompt text and tags.
- Allow client-side searching by title and description.
- Allow client-side filtering by category.
- Show a useful message when no cards match the active search/filter.
- Keep the implementation simple and frontend-only for now.

## Out of Scope

Do not add backend filtering or search yet.

Do not add pagination yet.

Do not add create, edit or delete UI yet.

Do not introduce a component library yet.

## Suggested Steps

### Step 1 – Improve PromptCardList shell

Update the list component so it uses the heading `Prompt Browser` and shows:

- loading state
- error state
- empty state
- Prompt Card count

### Step 2 – Improve PromptCardItem display

Update the card component so each Prompt Card displays:

- title
- category
- description
- prompt text
- tags

### Step 3 – Add basic browser styling

Add simple CSS for:

- readable page width
- card layout
- spacing
- list reset
- left-aligned card content

### Step 4 – Confirm client build

Run:

```bash
cd KrytenAssist.Client
npm run build
```

### Step 5 – Add search input

Add a controlled search input to the Prompt Browser.

Search should match against:

- title
- description

Search should update results as the user types.

### Step 6 – Add category filter

Add a category dropdown.

The dropdown should include:

- `All Categories`
- one option for each category found in the loaded Prompt Cards

Selecting a category should filter the displayed Prompt Cards.

### Step 7 – Improve no-results state

When cards exist but none match the current search or category filter, show:

```text
No prompt cards match your search or filter.
```

This should be distinct from the true empty database state:

```text
No Prompt Cards found.
```

### Step 8 – Final verification

Run:

```bash
cd KrytenAssist.Client
npm run build
```

Then from the solution root run:

```bash
dotnet build
dotnet test
```

## Acceptance Criteria

Prompt 019 is complete when:

- The React client still loads Prompt Cards from the API.
- The Prompt Browser has a clear heading and count.
- Prompt Cards display title, category, description, prompt text and tags.
- Loading, error and empty states are handled.
- Search filters Prompt Cards by title and description.
- Category filtering works client-side.
- No-results messaging works correctly.
- `npm run build` succeeds.
- `dotnet build` succeeds.
- `dotnet test` succeeds.

## Result

Prompt 019 delivered a usable React Prompt Browser.

The browser now loads Prompt Cards from the Kryten Assist API and displays them in a cleaner card-based layout. It includes loading, error, empty and no-results states. Users can search Prompt Cards by title or description and filter them by category using a dynamically generated category dropdown.

## Status

Complete.

## Files Created

- `docs/AI Playbook/019 - React Client Prompt Browser.md`

## Files Updated

- `KrytenAssist.Client/src/features/promptCards/PromptCardList.tsx`
- `KrytenAssist.Client/src/features/promptCards/PromptCardItem.tsx`
- `KrytenAssist.Client/src/App.css`

## Build

- `npm run build` succeeded.
- `dotnet build` succeeded with existing package vulnerability warnings for `SQLitePCLRaw.lib.e_sqlite3`.
- `dotnet test` succeeded: 9 tests passed, 0 failed.

## Git Commit

Pending.

## Design Decisions

- Search and filtering were kept client-side because Prompt 019 is focused on the React UI and the current API already returns the complete Prompt Card list.
- `useState` was used for user-editable UI state such as the search term and selected category.
- `useMemo` was used for derived values such as the filtered Prompt Card list and the dynamic category list.
- The filtered list was not stored as separate state because it can be calculated from the loaded Prompt Cards, search term and selected category.
- The search and category controls were grouped inside a `filter-bar` container so the JSX describes the relationship between the controls.
- Basic CSS was used instead of a component library so the project remains simple and the layout concepts are easier to learn.

## What We Learned

### React

- `useState` stores values that change over time, such as user input.
- `useMemo` is useful for derived values that can be recalculated from existing state.
- Derived state should usually be calculated rather than stored separately.
- Controlled inputs keep form values in React state.
- Conditional rendering can distinguish between empty data and filtered no-results states.

### CSS

- A container can group related controls before styling is applied.
- `display: flex` lays child elements out in a row.
- `gap` creates spacing between flex items.
- `flex: 1` allows an element, such as the search input, to grow and fill available space.
- Basic card styling can make a simple list feel more like an application screen.

### Engineering

- A list page is not necessarily a browser. A browser should help users search, filter or navigate the data.
- It is useful to pause during implementation and check whether the feature meets the intent of the prompt, not just whether the code works.
- Prompt files should act as both implementation instructions and a learning record for Future Robin.

## Lessons Learnt

- We initially thought Prompt 019 might be complete after improving the list display, but re-evaluation showed it needed real browsing behaviour.
- Search and filtering made the feature more meaningful while keeping the implementation frontend-only.
- Understanding the intent behind CSS rules is more helpful than memorising CSS syntax.
- The AI Playbook should include learning notes, design decisions and interview talking points, not just implementation steps.
- Take care to distinguish source files from terminal/output panes when applying edits.

## Interview Talking Points

- Built a React Prompt Browser that consumes a .NET API.
- Added live client-side search using React state.
- Added category filtering using a dynamically generated list of categories.
- Used `useMemo` for derived filtering logic instead of duplicating filtered data in state.
- Improved UX with loading, error, empty and no-results states.
- Used simple Flexbox styling for a filter bar.
- Kept filtering client-side deliberately because the feature scope did not require backend search yet.

## Future Improvements

- Add tag filtering.
- Add sorting by title, category or created date.
- Add debounced search if the number of Prompt Cards grows.
- Add pagination or virtualisation for larger datasets.
- Highlight matching search text.
- Persist search and filter state in the URL.
- Add create, edit and delete UI in later prompts.