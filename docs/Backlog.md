# Kryten Assist Backlog

## Backlog Items

### KB-001 — Save a Conversation Prompt for Reuse

**Type:** Feature  
**Area:** Conversations / Prompt Management  
**Priority:** High  
**Status:** Backlog

After submitting a prompt and receiving an answer, the user should be able to save the original prompt as a reusable prompt template.

The save action should be available directly from the conversation without requiring the user to copy and paste the prompt into the prompt editor.

#### Possible Interaction

- Right-click the user message and select **Save as Prompt**
- Use an action button beneath the message
- Open the prompt editor with the original prompt text already populated
- Allow the user to edit the title, description, category and tags before saving

#### Acceptance Ideas

- The original prompt text is copied into a new prompt template
- The user can edit the prompt before saving
- Saving the prompt does not interrupt or alter the conversation
- Duplicate prompts should either be allowed or clearly identified

---

### KB-002 — Save Answers and Maintain Answer History

**Type:** Feature / Research  
**Area:** Conversations / Knowledge  
**Priority:** Medium  
**Status:** Backlog

Investigate whether Kryten should save the answer associated with a reusable prompt.

Some answers may change over time because of updated information, different AI models, changing personal context or improvements to Kryten.

Rather than storing only one answer, Kryten may need to retain an answer history.

#### Questions to Resolve

- Should saving a prompt automatically save its current answer?
- Should answer history be optional?
- Should each answer record include its creation date?
- Should Kryten record which AI provider and model produced the answer?
- Should the user be able to mark one answer as the preferred answer?
- Should running the prompt again create a new answer version?
- Should unchanged or nearly identical answers be retained?
- How long should answer history be kept?

#### Possible Stored Information

- Prompt used
- Generated answer
- Date and time
- AI provider
- AI model
- Conversation reference
- Personal context or runtime context used
- User notes
- Favourite or preferred answer flag

---

### KB-003 — Categorise Saved Prompts from a Conversation

**Type:** Feature  
**Area:** Conversations / Prompt Management  
**Priority:** High  
**Status:** Backlog

When saving a prompt from a conversation, the user should be able to choose where it belongs.

The interaction should make it quick to place the prompt into an existing category without leaving the conversation.

#### Example Categories

- Cruise
- Fixed Income Interview
- Home Energy
- Kryten Development
- Personal Finance

#### Possible Interaction

Right-click a conversation prompt and select:

- **Save to Cruise**
- **Save to Fixed Income Interview**
- **Save to another category…**
- **Create new category…**

Alternatively, selecting **Save as Prompt** could open a small editor containing a category picker.

#### Acceptance Ideas

- Existing categories are available from the save action
- A new category can be created during the save process
- The selected category is retained when the prompt editor opens
- The user can change the category before confirming the save
- Categories remain user-created and manageable

---

### KB-004 — Recall Saved Knowledge and Previous Answers

**Type:** Feature / Epic  
**Area:** Memory / Knowledge Retrieval  
**Priority:** High  
**Status:** Backlog

Kryten should provide an easy way to jog the user’s memory by recalling previously saved prompts, answers and related notes.

The user should not need to remember the exact wording of a prompt or where an answer was stored.

#### Example User Requests

- “What did we decide about the Cruise skill?”
- “Remind me what we said about fixed-income interview preparation.”
- “What was the answer about setting `OPENAI_API_KEY`?”
- “Show me the previous answers for this prompt.”
- “What did I save about cruise price tracking?”

#### Possible Capabilities

- Search saved prompts and answers using natural language
- Search by category, title, tag or date
- Display the most relevant saved answer
- Show previous versions of an answer
- Link the answer back to its original conversation
- Allow the user to add personal notes or a short summary
- Let the user mark important answers as favourites
- Generate a short memory-jogging summary rather than always showing the full answer

#### Design Principle

The recall experience should be based on approximate meaning rather than exact keyword matching.

The user should be able to remember part of an idea and rely on Kryten to locate the relevant saved knowledge.

---

## Suggested Relationship Between Items

These backlog items could eventually form a larger epic:

### Epic — Conversation Knowledge Library

Turn useful conversations into reusable and searchable personal knowledge.

The likely workflow would be:

1. The user asks a question.
2. Kryten provides an answer.
3. The user saves the prompt, the answer or both.
4. The user assigns a category and optional tags.
5. Kryten stores later answers as versions.
6. The user recalls the information through search or natural-language questions.

This epic should remain in the backlog until its storage model, answer-versioning rules and relationship with existing conversation memory have been designed.