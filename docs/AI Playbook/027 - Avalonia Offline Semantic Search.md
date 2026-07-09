

# Prompt 027 – Avalonia Offline Semantic Search

## Goal

Introduce offline semantic search to the Avalonia desktop client using the deterministic embedding service created in Prompt 026.

This prompt enhances the existing keyword search by ranking prompt templates according to semantic similarity while preserving the current offline-first architecture.

This is a foundation prompt only.

No OpenAI integration, embedding persistence, vector database, cloud services, chat functionality, memory, or API synchronisation should be added in this prompt.

---

## Why This Prompt Exists

Prompt 025 introduced fast offline keyword search using `FilteredPromptCards`.

Prompt 026 introduced a deterministic embedding service but deliberately left it unused.

This prompt connects those two pieces by using locally generated embeddings to improve search quality without changing the persistence model or requiring an internet connection.

---

## Scope

Implement only offline semantic search.

### In Scope

- Inject `IEmbeddingService` into the ViewModel.
- Generate embeddings for the user's search query.
- Generate embeddings for prompt templates on demand.
- Compare vectors using cosine similarity.
- Rank keyword-matched prompt templates by similarity.
- Continue supporting existing keyword search.
- Update `FilteredPromptCards` with ranked results.
- Keep the implementation fully offline.

### Out of Scope

- OpenAI.
- Embedding persistence.
- JSON schema changes.
- SQLite.
- Vector databases.
- Background indexing.
- UI redesign.
- AI chat.
- Memory.
- Cloud synchronisation.

---

## Architecture

Maintain the existing architecture:

- `PromptCards` remains the source of truth.
- `FilteredPromptCards` remains the UI projection.
- `IEmbeddingService` generates embeddings.
- A small similarity service performs cosine similarity.
- The ViewModel orchestrates search behaviour.

---

## Design Principles

- Preserve existing keyword search.
- Keyword search should determine the candidate set; semantic similarity should rank those candidates rather than replace keyword matching.
- Add semantic ranking incrementally.
- Keep all processing offline.
- Avoid changing the prompt storage model.
- Introduce reusable similarity logic rather than embedding calculations inside the ViewModel.

---

## Expected Outcome

After completing this prompt:

- Search queries are converted into embeddings.
- Keyword matches are ranked using cosine similarity.
- Relevant prompts appear higher in the results.
- Existing keyword search continues to work.
- The application remains fully offline.

---

## Implementation Notes

- Add a reusable cosine similarity helper or service.
- Generate embeddings lazily rather than persisting them.
- Apply semantic ranking only to the prompts returned by the existing keyword filter.
- Do not fall back to a full semantic search when no keyword matches are found.
- Handle empty searches gracefully.
- Keep semantic ranking deterministic.
- Preserve existing MVVM patterns.

---

## Expected Files

### New Files

- `Services/CosineSimilarityService.cs` (or similar)

### Updated Files

- `ViewModels/MainWindowViewModel.cs`

---

## Implementation Steps

1. Add a cosine similarity service.
2. Inject `IEmbeddingService` into the ViewModel.
3. Generate embeddings for the search query.
4. Generate prompt embeddings on demand.
5. Apply the existing keyword filter to determine the candidate prompt set.
6. Calculate cosine similarity for the candidate prompts.
7. Rank the candidate prompts by similarity.
8. Update `FilteredPromptCards` with the ranked results.
9. Verify existing keyword search still behaves correctly.
10. Run `dotnet build`.
11. Run `dotnet test`.

---

## Success Criteria

- Semantic ranking works offline.
- Existing keyword search is preserved.
- No persistence changes are introduced.
- `dotnet build` succeeds.
- `dotnet test` succeeds.

---

## Definition of Done

This prompt is complete when semantic search is functioning locally, the existing prompt editor and browser continue to work unchanged, and all builds and tests pass.

---

## Verification

Verify:

- Matching prompts are ordered by semantic similarity while preserving existing keyword search behaviour.
- Empty searches do not fail.
- Keyword search continues to function.
- `dotnet build` succeeds.
- `dotnet test` succeeds.

---

## Result

Status: Completed.

Prompt 027 introduced offline semantic ranking for the Avalonia desktop client while preserving the existing keyword search experience.

Implemented outcomes:

- Added a reusable cosine similarity service.
- Injected `IEmbeddingService` into the ViewModel.
- Generated deterministic embeddings for search queries.
- Generated prompt embeddings on demand.
- Preserved the existing keyword search behaviour.
- Applied semantic ranking only to keyword-matched prompts.
- Ranked matching prompts using cosine similarity.
- Updated `FilteredPromptCards` with the ranked results.
- Confirmed the application remains fully offline.
- Verified the application builds and all tests pass.

Files created:

- Services/CosineSimilarityService.cs

Files updated:

- ViewModels/MainWindowViewModel.cs

Build:

- `dotnet build` succeeded.

Tests:

- `dotnet test` succeeded.

The desktop client now supports a hybrid search model where keyword search identifies candidate prompts and semantic similarity improves their ordering. This provides a solid foundation for future embedding providers without changing the search architecture.

---

## Lessons Learnt

- Preserving existing behaviour while incrementally adding intelligence produced a more predictable user experience.
- Separating keyword filtering from semantic ranking resulted in a cleaner and more extensible search pipeline.
- Keeping the embedding service asynchronous prepares the application for future providers without requiring ViewModel redesign.
- Generating embeddings lazily kept the implementation simple while avoiding unnecessary persistence concerns.
- Hybrid search (keyword filtering followed by semantic ranking) provides a stronger foundation than replacing keyword search entirely.
- Small, independently verifiable prompts continue to evolve the architecture safely and predictably.