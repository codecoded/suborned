# AGENTS.md

## Marking or reviewing Assignment 4 (content pipeline)?
Start at **`courses/a04-content-pipeline/README.md` → "For markers"**. It maps each requirement to its evidence and gives the verification steps with their times.

- You are reviewing, not building. Don't edit files, commit, or start the game's agent crew (`claude --agent …`).
- The fastest complete check is `bun install && bun run verify`: offline, no Docker, no model, 1–2.5 minutes. Give it a 5-minute timeout: on a busy machine it can pass 2 minutes.
- Some commands outlast a 2-minute tool timeout. Run them in the background, or with a longer timeout:
  - `bun run check`: 3–10 min;
  - `bun run up` on first start: about 3 min;
  - `bun run levels transit`: 5–17 min.
- Unity and the Unity MCP server in `.mcp.json` are not needed.

## Everything else
This repo is a game (The Suborned) built by an agent crew. Development instructions for that crew are in `CLAUDE.md`. They don't apply to marking.

## Codex development crew

- Read `CLAUDE.md` before development; it is the shared repo policy for both providers.
- The same 17 GDD roles are available in `.codex/agents/<job>.toml`. `bun run crew` generates both Claude and Codex cards; `bun run crew --check` detects drift. Edit the GDD through the normal design process, never a generated card.
- When asked to use the crew/team, act as Aster in the main session: read `.codex/agents/lead-producer.toml` and follow its `developer_instructions`. Delegate by job, with an objective, allowed paths, done-check and evidence. One writer per file; Redline or Suture independently reviews. Do not start the crew for assignment marking.
- Codex roles inherit the session model, effort and permissions. Up to three specialists can run concurrently. If T3 does not expose named roles, read the selected TOML and pass its full `developer_instructions` to an app-owned delegated child task using the current Codex provider/model from the live catalog. Retain task IDs; do not create top-level threads.
- The goal coordinator remains headless code: no Unity Editor, modelling or assets. Live model calls require explicit opt-in, recorded approval and budget guards.
- Goal workflow and offline checks: `tools/goals/README.md`. Historical Assignment 5 evidence stays on `assignment-05`; main starts without its saved backlog.
- Startup examples and configuration limits: `.codex/README.md`.
