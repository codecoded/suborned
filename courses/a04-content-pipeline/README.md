# A04 · Content pipeline: the Harbour District transit network

## What it generates
Three outputs for The Suborned's Harbour District, as edits to the district plan (`contracts/world/harbour-district.plan.json`, schema `suborned.plan/1`):

1. **Lines** (Datum, world planner): tram and sky rail routes, with fillet radii, double track and a path `order`.
2. **Stops and stations** (Vantage, level designer): positions on the lines, platforms, the public space each entrance fronts, and interchange pairs.
3. **Timetable** (Pulse, schedule and AI designer): headway, dwell and run times per line, plus journey times. Boarding as fast travel has no price in the GDD, so it goes out as an open question and is never priced.

## Why this matters for this game
The Suborned is a commander game. The cell moves through a city that runs on schedules, and a tram or sky rail is a window the player can see coming (pillar: orchestrate the city). Interchanges and stations are where the police seal the district. The GDD's house rule is "no AI at runtime; content generated at build time; same orders → same result". So the network is generated once, checked deterministically, and committed with everything needed to replay it.

## How it works
- **Objective:** a layer-4 brief, one per crew member, in `tools/levels/objectives/`.
- **Retrieval:** SurrealDB HNSW (BGE-small) and BM25 run per query, merged by reciprocal rank fusion, over the agent GDD, the human GDD and the plan.
- **Crew member:** runs headless through `claude -p`. Its crew card is the system prompt and structured output is its only tool.
- **Validation:** each output is checked against its zod schema, then applied to a copy of the plan.
- **Plan checker:** `tools/plan/cli.ts --strict` gives exit code 0 or 1 and draws a CAD sheet.
- **Critic:** Keel judges the candidate. Every fault names an owner and cites chunk ids.
- **Revision:** each fault goes back to its owner with the cited chunks pinned into their retrieval, for up to 3 rounds.
- **ACCEPTED** means checker exit 0 and Keel pass.

## The submitted run: `r001`, ACCEPTED in 2 rounds
- **Record:** `runs/levels/harbour-district/r001/`, committed. It holds `run.json`, the input plan and, for each round, the plan, checks, each agent's output, its call record (card hash and retrieved text) and Keel's verdict.
- **Report:** [`report.html`](report.html) shows each call's query, retrieved chunks and output side by side, plus each critic verdict. To rebuild it, run `bun courses/a04-content-pipeline/build-report.ts r001`.
- **Self-assessment:** [`self-assessment.html`](self-assessment.html).
- **Sheets:**
  - [before, v0.1.0](evidence/sheets/harbour-district-v0.1.0.html) (strict: fails N-R1 reach and S-S2 station spacing)
  - [round 1, 0.2.0-r001.1](evidence/sheets/harbour-district-v0.2.0-r001.1.html)
  - [accepted, 0.2.0-r001.2](evidence/sheets/harbour-district-v0.2.0-r001.2.html)
- **Transcripts:** `evidence/transcripts/` holds the prompt, card snapshot, retrieval, schema, stream-json events and output for each call. These are copied from `out/`.
- **Retrieval tweak:** see `evidence/ranks.json`. With the old single query, `standards › tram` (home of `stopSpacing`) ranked 9th, outside the top 8. With the per-objective queries it ranks 2nd to 5th.
- **Critic correction:** this happened without seeding. In round 1 the checker was clean, but Keel raised 3 faults:
  - a Skyport station over water with no walkable entrance (→ Vantage);
  - a journey time that didn't add up (→ Pulse);
  - an open question that misquoted the GDD (→ Pulse).

  Both owners revised, and Keel passed round 2.

## Prerequisites
- [Bun](https://bun.sh) 1.4 or later, and git.
- Docker, running. The stack uses the fixed compose project name `suborned` on ports `127.0.0.1:18504` (SurrealDB) and `18505` (embeddings). Any checkout reuses the same containers and volumes, and other services must not hold those ports.
- [Claude Code](https://code.claude.com), signed in, with `claude` on `PATH`. Set `CLAUDE_BIN` to use another path. Calls run on your Claude plan. The models come from the crew cards: Opus for Datum, Vantage and Keel; Sonnet for Pulse.
- Unity is **not** needed.

## Run it from a fresh clone
```sh
git clone -b assignment-04 <repo-url> suborned-a04 && cd suborned-a04
bun install
bun run levels:replay r001     # offline: no Docker, no model. Checks schemas, hashes and retrieved text, reruns the checker → REPLAY OK
bun run up                     # SurrealDB + embedding service; the first start builds the image and downloads the model (a few minutes)
bun run ingest                 # index the GDD and the plan
bun run levels transit         # the generation run → runs/levels/harbour-district/r002/
bun run levels:replay r002     # replay the new run
bun run levels:sheets          # re-render every run's sheets + out/levels/index.html
```
Optional: `bun run check` runs the typecheck, the tests (model-free, about 3 min) and the plan checker. `bun run levels:ranks` reprints the retrieval ranks. `bun run levels transit --seed station-on-plot` forces a labelled critic challenge.

## Time and cost per run
r001 made 7 model calls with about 400 s of model time. Wall time was about 8 minutes (calls run in sequence). The CLI reported $1.34. A run that needs all 3 rounds makes up to 10 calls, so expect roughly 8–15 minutes and $1–3. On a subscription the dollar figure is notional usage, not a charge.

## Where outputs land
- `runs/levels/<plan>/r00N/`: committed and small. This is everything replay needs.
- `out/levels/<plan>/r00N/`: git-ignored. It holds the sheets, prompts, retrievals and transcripts. Regenerate the sheets with `levels:sheets`.
- A run never writes `contracts/`. An accepted plan replaces `contracts/world/harbour-district.plan.json` only with Sky's OK.
