# AGENTS.md

## Marking or reviewing Assignment 4 (content pipeline)?
Start at **`courses/a04-content-pipeline/README.md` → "For markers"**. It maps each requirement to its evidence and gives the verification steps with their times.

- You are reviewing, not building. Don't edit files, commit, or start the game's agent crew (`claude --agent …`).
- The fastest complete check is `bun install && bun run verify`: offline, no Docker, no model, 1–2 minutes. Give it a 5-minute timeout: on a busy machine it can pass 2 minutes.
- Some commands outlast a 2-minute tool timeout. Run them in the background, or with a longer timeout:
  - `bun run check`: 3–10 min;
  - `bun run up` on first start: about 3 min;
  - `bun run levels transit`: 5–17 min.
- Unity and the Unity MCP server in `.mcp.json` are not needed.

## Everything else
This repo is a game (The Suborned) built by an agent crew. Development instructions for that crew are in `CLAUDE.md`. They don't apply to marking.
