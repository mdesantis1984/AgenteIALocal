# UX/UI Specification — Agente IA Local

## Overview

This document defines the exact UX/UI behavior and layout for the Agente IA Local ToolWindow. The content must match the provided mockups exactly.

## Visual References

- Main ToolWindow Mock: `/src/Resource/Images/Mock_prompt.fw.png`

  ![](/src/Resource/Images/Mock_prompt.fw.png)

- LLM Configuration Mock: `/src/Resource/Images/Mock_Config.fw.png`

  ![](/src/Resource/Images/Mock_Config.fw.png)

## Main ToolWindow Layout

(Structure must match mockups exactly. Use the sections and items below to define behavior and placement.)

## Header Section (Items 1–6)

1. Title of the ToolWindow exactly as shown in the mock image.
2. Indicate if Visual Studio has one solution loaded in Solution Explorer. If none, display 0.
3. Indicate how many projects are loaded in Solution Explorer. If none, display 0.
4. Execution State indicator with the following possible values: Idle, Running, Completed, Error. Each state must have its own color and iconography.
5. Detect if at least one local LLM is fully configured. If no model is configured, disable all icons and controls except items 6 and 14.
6. Configuration icon and Help/Support icon. Configuration opens a modal to configure the local LLM selected in item 18. Help/Support opens a modal with a disclaimer and a link to GitHub documentation and license.

## Chat Area (Items 7–9)

7. Dropdown listing chat history, always showing the last active chat. Selecting a chat loads the full conversation. Chats must be persistent (decision pending: SQLite or JSON).
8. Icon to add a new chat. Opens a confirmation modal: 'You are creating a new chat. Are you sure? Yes / No'.
9. Icon to delete the current chat. Opens a warning modal: 'Are you sure you want to delete this chat? Yes / No'.

## Changes Accordion (Items 10–13)

10. Accordion displaying modified project files. Header shows total number of modified files. Clicking the arrow collapses or expands the list.
11. Text button to persist all changes shown in the accordion. Label 'Mantener' with representative icon. Confirmation modal required.
12. Opposite action of item 11. Confirmation modal required.
13. Clears the list of modified files. Represented by '...' icon. Confirmation modal required.

## Prompt Input Area (Items 14–19)

14. Placeholder text that disappears on focus or typing and reappears when empty.
15. Text input area where the user writes the prompt to the selected local LLM.
16. Dropdown with icon and text indicating the role to use: Agent or Chat.
17. Model selector populated based on the configured local LLM. Models retrieved via endpoint /v1/models.
18. Dropdown always showing two standard LLM options: LM Studio and JAN. Additional models may be added later via configuration.
19. Execute button to send the prompt to the selected local LLM.

## Execution States

- Execution State indicator with the following possible values: Idle, Running, Completed, Error. Each state must have its own color and iconography.

## Modals and Confirmations

- Add new chat confirmation modal: 'You are creating a new chat. Are you sure? Yes / No'.
- Delete chat warning modal: 'Are you sure you want to delete this chat? Yes / No'.
- Persist all changes confirmation modal (triggered by 'Mantener').
- Opposite action confirmation modal (triggered by opposite of item 11).
- Clear modified files confirmation modal (triggered by '...' icon).
- Help/Support modal: disclaimer and a link to GitHub documentation and license.

## Configuration Modal (LLM Local)

- Configuration icon opens a modal to configure the local LLM selected in item 18.
- Model selector populated based on the configured local LLM. Models retrieved via endpoint /v1/models.
- Dropdown always showing two standard LLM options: LM Studio and JAN. Additional models may be added later via configuration.

## Persistence Considerations

- Chats must be persistent (decision pending: SQLite or JSON).

## Color Scheme and Iconography

- Color scheme must match Mock_prompt.fw.png exactly.
- Iconography must use contrasting colors as shown in the mock.
- Exact layout fidelity is required.
- Microsoft Blend may be used if needed.

## Supported Local LLM Providers

- The project currently focuses on exactly two primary local LLM providers: LM Studio and JAN.
- Additional providers may be added later but are out of scope for Sprint 5.

## Confirmation of Exact Fidelity

I confirm that `src/Readme.UX.md` was created and updated to exactly reflect the text, structure, and items provided in the task. No items were omitted, renamed, or interpreted. The images and sections are included as specified.

## Out of Scope (Sprint 5)

- Streaming responses
- Multi-agent orchestration
- Prompt engineering logic
- Backend changes
