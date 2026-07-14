# Development Workflow

Version: 2.0
Last Updated: July 2026

Major Changes

v2.0
- Introduced Codex-assisted development
- Introduced Backlog workflow
- Added AI Playbook review cycle
- Added Session Handover process
- Added architecture-first approach
- Added Rider review process
> **Version 2**
>
> This document supersedes the original Development Workflow.
>
> The original workflow was designed around ChatGPT generating implementation code directly.
>
> As Kryten Assist has grown, development has evolved into a collaborative process involving architectural design, Codex implementation, Rider review and Git verification.
>
> This document reflects the current development methodology.
> 
> ## Why this Changed

### Previous Workflow

The original workflow assumed:

- ChatGPT designed the solution.
- ChatGPT generated the implementation.
- Robin pasted the implementation into Rider.
- Robin built and tested.

This worked well for smaller prompts but became increasingly difficult as prompts became larger and architectural decisions became more important.

---

### Current Workflow

The current workflow separates responsibilities.

| Role | Responsibility |
|------|----------------|
| Robin | Product owner, reviewer and verifier |
| Kryten (ChatGPT) | Architecture, design, prompts and reviews |
| Codex | Implementation |
| Rider | Review, editing, testing and debugging |

The AI Playbook now defines the architecture.

Codex implements only the agreed implementation step.

Robin reviews every generated diff before it is accepted.

This keeps the architecture under human control while allowing implementation to be accelerated.

## Current Development Workflow

1. Select the next roadmap item.

2. Discuss the design.

3. Capture any new ideas in Backlog.md.

4. Agree the architecture.

5. Create or update the AI Playbook prompt.

6. Break the prompt into implementation steps.

7. Decide whether the next step should be:
    - Manual
    - Codex
    - Pair programmed

8. Produce a constrained Codex implementation prompt.

9. Run Codex.

10. Review every changed file in Rider.

11. Build and test.

12. Manually verify behaviour.

13. Update:
    - AI Playbook
    - Roadmap
    - Backlog
    - Session Handover

14. Commit.

15. Push.

16. Start the next implementation step.

## Decision Rules

During implementation:

- Good ideas should not interrupt the current prompt.
- Add future work to Backlog.md.
- Keep every prompt focused on one vertical slice.
- Keep the solution compiling after every step.
- Review all Codex output before accepting it.
- Prefer many small commits over one large commit.
