# Prompt 031b – Pass 2

## Objective

Implement the usability improvements identified during the manual review of Pass 1.

Do not redesign the application.

Prompt 031b remains the source of truth.

---

## Requested Changes

### Keyboard

- Enter sends the current message.
- Shift+Enter inserts a newline.
- Empty messages are ignored.
- Keep keyboard focus in the message input after sending.

### Conversation

- Automatically scroll to the latest message.
- Preserve chronological ordering.

### Layout Refinements

- Improve spacing where required.
- Improve conversation readability.
- Refine prompt card density.
- Improve alignment.
- Preserve the existing two-pane layout.

---

## Constraints

- Preserve MVVM.
- Avoid unnecessary code-behind.
- No Prompt 031c visual styling.
- No new functionality.
- Build and test before stopping.

---

## Results

### Status

✅ Pass 2 complete and manually verified.

### Files Changed

- `KrytenAssist.Avalonia/MainWindow.axaml`
- `KrytenAssist.Avalonia/MainWindow.axaml.cs`
- `docs/AI Playbook/031b - Pass 2.md`

### Implementation

- Enter sends through the existing `SendMessageCommand`.
- Shift+Enter retains the TextBox's native newline behaviour.
- The Enter handler uses Avalonia's tunnelling input route so it runs before the multiline TextBox handles the key internally.
- Existing ViewModel guards continue to prevent empty or whitespace-only messages from being sent.
- Focus returns to the conversation input after Enter is handled.
- The conversation list scrolls to each newly appended user or assistant message.
- Existing append order remains unchanged, preserving oldest-to-newest chronology.
- Prompt cards use slightly tighter spacing and typography.
- Conversation messages use improved internal spacing while retaining the existing presentation and two-pane structure.

The code-behind changes are limited to view-only keyboard, focus and scrolling orchestration. Conversation behavior remains owned by the existing ViewModel command.

### Build Result

✅ `dotnet build KrytenAssist.sln`

### Test Result

✅ `dotnet test KrytenAssist.sln --no-build`

- Avalonia tests: 19 passed
- API tests: 9 passed
- Total: 28 passed, 0 failed

### Manual Verification

Completed:

- ✅ Enter sends a non-empty message.
- ✅ Shift+Enter inserts a newline without sending.
- ✅ Empty and whitespace-only input is ignored.
- ✅ Focus remains in the message input after sending.
- ✅ User and assistant messages automatically scroll into view.
- ✅ Long conversations remain oldest-to-newest.
- ✅ Prompt density and conversation readability remain comfortable at representative window sizes.
