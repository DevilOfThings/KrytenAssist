# Development Workflow

## Shared AI Playbook Workflow

1. Discuss the design.

2. Agree the architecture and responsibilities.

3. Write or update the AI Playbook prompt.

4. Create the target file(s) in Rider.

5. Open the target file.

6. Ask Kryten to implement the currently open file.

7. Review the generated code together.

8. Verify the change using the appropriate workflow.

9. Update the AI Playbook.
    - Result
    - Status
    - Files Created
    - Files Updated
    - Build / Verification
    - Notes / Lessons Learnt

10. Commit to Git.

11. Review the roadmap and agree the next vertical slice.

---

## Backend Verification Workflow

Use this workflow for .NET backend, application, infrastructure and test changes.

From the solution root:

```bash
dotnet build
dotnet test
```

To run the API from the solution root:

```bash
dotnet run --project KrytenAssist.Api
```

Do not use `dotnet run` from the solution root unless a runnable project is explicitly specified.

---

## Frontend Verification Workflow

Use this workflow for React, TypeScript, Vite and CSS changes.

During active UI development, keep the Vite development server running:

```bash
cd KrytenAssist.Client
npm run dev
```

After each small UI change:

1. Save the file.
2. Check the browser.
3. Confirm Vite hot reload has updated the page.
4. Continue to the next small change.

At the end of each logical step, or before committing, run:

```bash
npm run build
```

Use `npm run build` to confirm the TypeScript and production Vite build still succeed.

---

## Full Verification Before Commit

For backend-only changes:

```bash
dotnet build
dotnet test
```

For frontend-only changes:

```bash
cd KrytenAssist.Client
npm run build
```

For full-stack changes:

```bash
dotnet build
dotnet test

cd KrytenAssist.Client
npm run build
```

Also manually verify the React client in the browser when UI behaviour has changed.