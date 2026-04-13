# Godot AI Bot

Godot AI Bot is a Windows Forms desktop assistant for Godot development. It can index the official Godot documentation, answer Godot-focused questions, generate lightweight implementation plans, flag common code issues, and persist small user preferences between sessions.

## Features

- Loads and searches the Godot stable documentation in-app
- Responds to Godot gameplay, UI, API, and architecture questions
- Generates quick implementation plans with `/plan`
- Stores user preferences with `/remember` and shows them with `/memory`
- Performs lightweight Godot code review heuristics on pasted snippets

## Requirements

- Windows
- .NET SDK 10.0 or newer with Windows Desktop support

## Run

```powershell
dotnet build
dotnet run
```

The first time you use the app, click `Load Godot Docs` to build the in-memory search index before asking documentation-heavy questions.

## Commands

- `/help` shows the command list
- `/plan <feature request>` generates a short implementation plan
- `/remember <fact or preference>` stores a user preference
- `/memory` lists saved preferences
- `/clear-memory` clears stored memory

## Project Layout

- `Program.cs`: application entrypoint
- `Form1.cs`: Windows Forms UI event handling
- `GodotAssistantBot.cs`: response composition, command handling, and heuristic analysis
- `GodotKnowledgeBase.cs`: documentation indexing and search
- `BotStateStore.cs`: persistent memory storage
- `Models.cs`: shared records and enums

## Notes

- The documentation index is kept in memory for the current app session.
- Saved memory is written to `%LocalAppData%\GodotAIBot\memory.json`.
- The repository currently includes generated `bin/` and `obj/` artifacts from earlier work; `.gitignore` is now set up to avoid adding more of them.
