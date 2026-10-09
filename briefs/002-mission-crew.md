# 002 · Mission crew: the §09 mission generator, from Assignment 3

Goal: the GDD's mission generator runs in this repo on the crew itself. Given a brief (objective, target, closing event, purse) and a block, Motive, Pulse, Waymark, Fable and Keel generate a rule-checked mission package (cast, schedule, routes, dialogue, check), as Assignment 3 proved on Handover. Runs after 001 because missions are designed on the block the world builder stands up.

Source: `~/Projects/game-course-assignments/assignment-03-agent-crew` (929 lines of bun TS + zod; sample run in `out/handover/`). Reference only: copy what is needed, never import from it at runtime.

One agent per GDD person. A3 named its roles by job (`systems-designer` = Motive, `schedule-designer` = Pulse, `mission-designer` = Waymark, `dialogue-designer` = Fable, `simulation-engineer` = Keel, `lead-producer` = Aster). Here the crew cards are layer 3; A3's "In this crew" sections become layer-4 objective files. No second set of agents.

## A · Splice · port the pipeline
objective: `tools/missions/` with A3's orchestration (`crew.ts`), checker, schemas, spatial, render, usage and tests; `bun run missions <brief>` and `bun test`. Calls go through `tools/rag/agent.ts` `runAgent` (retrieval for the objective + crew card as system prompt + evidence per call); A3 had no retrieval, so each objective gets a query. Don't write a second adapter. The recipe it uses (2026-10-10, CLI 2.1.295): body of `.claude/agents/<name>.md` (frontmatter stripped) as `--system-prompt`, `--model` from the card's frontmatter, `--tools=` `--setting-sources project` `--strict-mcp-config` `--json-schema …` `--output-format stream-json --verbose` `--no-session-persistence`, env `CLAUDE_CODE_DISABLE_CLAUDE_MDS=1`; the init event then lists exactly `["StructuredOutput"]` and no MCP servers. Reject any session whose init lists more. (`--agent <name>` does not work here: the card's Glob and Grep survive `--tools` and `--disallowedTools`.) Layer 4 = `tools/missions/objectives/<name>.md` (A3's "In this crew" text, unchanged in substance). GDD passages come from the agent GDD by section number, not A3's v0.12 `docs/gdd/` split. Briefs in `briefs/missions/` (A3's `handover.json`). Block layout: A3's `layouts/civic-district.json` + overlay → `contracts/world/handover-layout.json` until B replaces it.
done-check: `bun test` passes (A3's checker tests) · `bun run typecheck` clean · one real run on Handover ends ACCEPTED (checker + Keel pass, ≤ 3 rounds) with tokens per agent logged · model calls are opt-in, never in tests.
evidence: test output, the run's `report.json` and `briefing.html` under `out/missions/handover/`, a diff of what changed from A3 and why.
next owner: Keel.

## B · Vantage · the Handover layout from the plan
objective: derive the mission layout (places with tags, residents, watchers, walk speed, crew start) from the plan's Handover block (`contracts/world/harbour-district.plan.json` civic + `handover-block.json`), in plan coordinates, so missions sit in the world 001 builds. Same schema as A3's layout; no new mechanics.
done-check: every A3 place has a counterpart (or a stated reason it's gone); the mission crew run in A is repeated on this layout and ends ACCEPTED.
next owner: Waymark.

## C · Keel · review
objective: check the accepted package against §07 (2+ quiet routes win, loud costs more Trail, every fail reachable, no essential contact, a minute to spare) and the checker report; send faults to their owner.
next owner: Aster (accept or return).
