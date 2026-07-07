

# Prompt 020 – Prompt Editor

## Goal

Build the first Prompt Editor screen in the React client so Future Robin can create new Prompt Cards from the web UI instead of using Swagger.

This prompt builds on Prompt 019, where the Prompt Browser was improved with search, filtering, loading, empty and no-results states.

## Why?

Prompt Cards are the core content type in Kryten Assist. Until now, Prompt Cards can be created through the API, but not through the React client.

Adding a simple editor makes the application feel more complete and starts moving Kryten Assist from a read-only browser into an interactive tool.

## Scope

The Prompt Editor should:

- Add a simple form to create a new Prompt Card.
- Capture title, category, description, prompt text and tags.
- Submit the form to the existing Prompt Cards API.
- Refresh the Prompt Browser after a card is created.
- Show basic success and error feedback.
- Keep the implementation simple and frontend-only unless an API issue is discovered.

## Out of Scope

Do not add edit existing Prompt Card support yet.

Do not add delete support yet.

Do not add advanced validation UI yet.

Do not introduce a component library yet.

Do not add routing changes unless they are required.

## Architecture

The first Prompt Editor should live inside the existing Prompt Cards feature area.

Suggested files:

- `KrytenAssist.Client/src/features/promptCards/PromptCardForm.tsx`
- `KrytenAssist.Client/src/features/promptCards/PromptCardList.tsx`
- `KrytenAssist.Client/src/App.css`

For this prompt, the editor can sit above the Prompt Browser on the home page. A separate route can be introduced later when the UI grows.

## Design Decisions

- Start with create-only support because creating Prompt Cards removes the current need to use Swagger for manual data entry.
- Keep the editor near the browser so the user can immediately see the new Prompt Card appear.
- Use controlled form inputs so React owns the form state.
- Convert comma-separated tag text into an array before submitting to the API.
- Keep validation lightweight for now and rely on the existing backend validation rules.

## Suggested Steps

### Step 1 – Confirm API contract

Review the existing create Prompt Card request shape.

Expected payload:

```json
{
  "title": "Prompt 020 Prompt Editor",
  "category": "Development",
  "description": "Build the first create form in the React client.",
  "promptText": "Create a React form that posts a Prompt Card to the API.",
  "tags": ["react", "forms", "prompt-020"]
}
```

### Step 2 – Create PromptCardForm component

Create a new component for the create form.

The form should include:

- title input
- category input
- description textarea
- prompt text textarea
- tags input as comma-separated text
- submit button

### Step 3 – Add form state

Use `useState` for each form value.

Start with separate state variables rather than a complex form object so the behaviour is easy to understand.

### Step 4 – Submit to the API

On submit:

- prevent the default browser form submission
- build a request object
- split the tags string into an array
- POST to `/api/promptcards`
- show a useful error message if the request fails

### Step 5 – Refresh browser after create

After a successful create:

- clear the form
- refresh the Prompt Card list
- show a short success message

This may require moving the load function so it can be reused after creation.

### Step 6 – Add simple styling

Add basic CSS for:

- form layout
- inputs and textareas
- submit button
- success and error messages

### Step 7 – Verify manually

Run both applications:

```bash
dotnet run --project KrytenAssist.Api
```

```bash
cd KrytenAssist.Client
npm run dev
```

Create a Prompt Card from the UI and confirm it appears in the Prompt Browser.

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

Prompt 020 is complete when:

- A Prompt Card can be created from the React UI.
- The form captures title, category, description, prompt text and tags.
- Tags entered as comma-separated text are submitted as an array.
- The Prompt Browser refreshes after creation.
- Success and error feedback are shown.
- `npm run build` succeeds.
- `dotnet build` succeeds.
- `dotnet test` succeeds.

## Result

Prompt Editor implemented successfully. Users can now create Prompt Cards directly from the React UI. The form submits to the existing API, refreshes the Prompt Browser after creation, clears the form, and displays success and error feedback.

## Status

Complete.

## Files Created

- `docs/AI Playbook/020 - Prompt Editor.md`

## Files Updated

- `KrytenAssist.Client/src/features/promptCards/PromptCardForm.tsx`
- `KrytenAssist.Client/src/features/promptCards/PromptCardList.tsx`
- `KrytenAssist.Client/src/api/promptCardsApi.ts`
- `KrytenAssist.Client/src/api/apiClient.ts`
- `KrytenAssist.Client/src/App.css`

## Build

- `npm run build` ✅
- `dotnet build` ✅
- `dotnet test` ✅ (9 tests passed)

## Git Commit

Recommended commit message:

`Prompt 020 - Prompt Editor`

## What We Learned

This prompt completed the first end-to-end Prompt Editor in the React client. We introduced controlled form components, integrated them with the existing .NET API, refreshed the Prompt Browser after a successful create, and added user feedback through success and error messages. During implementation we also diagnosed a React component lifecycle issue where the form was being unmounted during list refreshes, causing local state to be lost. Refactoring the page to keep the form mounted produced a more robust and user-friendly design.

## Lessons Learnt

- React component state is lost when a component is unmounted.
- Avoid early returns that unmount unrelated UI during loading or error states.
- Keep forms mounted while refreshing surrounding data to preserve local state such as success messages and user input.
- Small UX improvements, such as clearing the form and providing immediate feedback, make the application feel significantly more polished.

## Interview Talking Points

- Built a reusable React form using controlled components.
- Integrated the React client with an existing .NET API.
- Converted comma-separated tag input into a strongly typed array before submission.
- Refreshed application state after a successful create without requiring a page reload.
- Diagnosed and resolved a React component lifecycle issue caused by unmounting during loading.

## Future Improvements

- Add edit existing Prompt Card support.
- Add delete support from the browser.
- Add form validation messages beside each field.
- Add route-based navigation between browser and editor.
- Add reusable form components.
- Add optimistic UI updates.