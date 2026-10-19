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

## Marking or reviewing Assignment 5 (goal-oriented coding agent)?
Start at **`courses/a05-goal-agent/README.md` → "For markers — human or agent"**, then the linked outcome report. The revised workflow completed a real Codex generation run: structured case/area-watch debrief export, five new acceptance tests and 866 existing regression tests passing. Completion is for the explicit debrief-export goal, not the whole game.

The earlier Codex crew scan in `runs/crew/2026-10-19T10-24-00Z-codex-discovery/` selected the task without production edits. The actual CLI generation is `runs/goals/2026-10-19T11-05-43-376Z-discovery/`; its unchanged rerun is `runs/goals/2026-10-19T11-14-05-156Z-discovery/` (zero model calls, zero writes). Earlier incomplete attempts and the original fixed-scope Watch feature remain separately labelled.

- `bun install --frozen-lockfile && bun run goals:verify` is the complete offline reviewer check. Allow ten minutes. It prints six stage results and an explicit final PASSED/FAILED, with JSON/logs under `out/goals/submission/`. Do not launch live generation or the crew for marking.
- `bun run goals verify runs/goals/2026-10-19T11-05-43-376Z-discovery` reconstructs the failing baseline and replays the generated patch in an isolated temporary workspace. Verify `runs/goals/2026-10-19T11-14-05-156Z-discovery` to also validate the unchanged rerun and its retained proof. Allow five minutes per command; .NET 10 is required. Initial dependency restore may need network access.
- Verification needs no Docker, database, model call, Unity Editor or modelling assets. A live `goals run` is separate and requires explicit opt-in and recorded approval.
- Default `goals run` discovers a backlog from the latest GDD; `--goal <text>` bounds discovery and keeps a separate backlog. `goals scan` discovers without writing simulation code; `goals status [--goal <same text>]` checks saved proof hashes offline. Explicit `--scope` selects the legacy experiment. Bounded runs remain INCOMPLETE while unresolved requirements remain. Codex is selected explicitly with `--provider codex --model <id>` and token/call bounds; Claude retains dollar bounds.
- The report documents existing broad-suite failures reproduced on the starting commit; these are separate from the passing Assignment 5 checks.

## Everything else
This repo is a game (The Suborned) built by an agent crew. Development instructions for that crew are in `CLAUDE.md`. They don't apply to marking.

## Codex development crew

- Read `CLAUDE.md` before development; it is the shared repo policy for both providers.
- The same 17 GDD roles are available in `.codex/agents/<job>.toml`. `bun run crew` generates both Claude and Codex cards; `bun run crew --check` detects drift. Edit the GDD through the normal design process, never a generated card.
- When asked to use the crew/team, act as Aster in the main session: read `.codex/agents/lead-producer.toml` and follow its `developer_instructions`. Delegate by job, with an objective, allowed paths, done-check and evidence. One writer per file; Redline or Suture independently reviews. Do not start the crew for assignment marking.
- Codex roles inherit the session model, effort and permissions. Up to three specialists can run concurrently. If T3 does not expose named roles, read the selected TOML and pass its full `developer_instructions` to an app-owned delegated child task using the current Codex provider/model from the live catalog. Retain task IDs; do not create top-level threads.
- Assignment 5 remains headless code: no Unity Editor, modelling or assets. Existing live-call approval and budget guards still apply.
- Startup examples and configuration limits: `.codex/README.md`.
