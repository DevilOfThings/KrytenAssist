# Prompt 018 – React Client Foundation

## Objective

Create the first React front-end for Kryten Assist using React, TypeScript and Vite.

This prompt establishes the front-end architecture that future prompts will build upon while keeping the UI intentionally simple.

During development, a small CORS configuration will be added to the ASP.NET API to allow the Vite development server to communicate with the backend. This does not change the API contract or business logic.

---

# Goals

- Create a React application using Vite.
- Use TypeScript.
- Establish a clean folder structure.
- Configure React Router.
- Create a typed API client.
- Connect to the existing ASP.NET API.
- Retrieve Prompt Cards from the API.
- Display Prompt Cards in a simple list.
- Keep the application easy to extend.
- Follow modern React best practices.

---

# Existing Backend

The existing backend already provides:

```http
GET     /api/promptcards
GET     /api/promptcards/{id}
POST    /api/promptcards
PUT     /api/promptcards/{id}
DELETE  /api/promptcards/{id}
```

No changes should be made to the backend.

---

# Technologies

- React
- TypeScript
- Vite
- React Router
- Fetch API
- CSS

No UI component library should be introduced at this stage.

---

# Suggested Project Structure

```text
KrytenAssist.Client
│
├── public
│
├── src
│   ├── api
│   │   ├── apiClient.ts
│   │   └── promptCardsApi.ts
│   │
│   ├── features
│   │   └── promptCards
│   │       ├── PromptCardList.tsx
│   │       ├── PromptCardItem.tsx
│   │       ├── PromptCard.ts
│   │       └── index.ts
│   │
│   ├── pages
│   │   └── HomePage.tsx
│   │
│   ├── router
│   │   └── AppRouter.tsx
│   │
│   ├── App.tsx
│   ├── main.tsx
│   └── index.css
│
├── package.json
└── vite.config.ts
```

---

# Architecture

The UI should follow a simple layered approach.

```text
Pages
    │
    ▼
Features
    │
    ▼
API Client
    │
    ▼
ASP.NET API
```

Business logic should remain inside the backend.

The React application should focus on presentation, routing and user interaction.

---

# Initial UI

Initially the application only needs to display:

```text
Kryten Assist

Prompt Cards

----------------------------------
Title

Category

Description
----------------------------------
```

No styling beyond basic readability is required.

---

# Routing

Configure React Router.

Initial routes:

```text
/
```

Future prompts will introduce additional pages.

---

# Development CORS

Because the React development server runs on a different origin (typically http://localhost:5173), configure a development-only CORS policy within the ASP.NET API.

Allow:

- Origin: http://localhost:5173
- Any HTTP header
- Any HTTP method

The CORS middleware should be registered before endpoint mappings.

This configuration is for local development only and does not alter the API contract.

---

# API Client

Create a reusable API layer.

Avoid making direct `fetch()` calls inside React components.

---

# Data Model

Mirror the existing backend response model.

```typescript
export interface PromptCard {
    id: string;
    title: string;
    category: string;
    description?: string;
    promptText: string;
    tags: string[];
}
```

---

# Acceptance Criteria

- React application created.
- TypeScript configured.
- React Router configured.
- Development CORS configured for the React client.
- Prompt Cards successfully retrieved from the API.
- Prompt Cards displayed.
- Project builds successfully.
- Existing backend unchanged.

---

# Result

The project will have its first working React client with a maintainable architecture ready for future UI enhancements while preserving the existing Clean Architecture backend.