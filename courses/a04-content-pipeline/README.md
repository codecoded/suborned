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

## The submitted run: `r003`, ACCEPTED in 2 rounds
r003 ran on GDD v0.15.0 (corpus `3acb05ba5c76`) and on the corrected civic block: the Oct 9 city scale, with the café as the 22 × 11.3 × 34.8 m corner building. Rule C-0 holds the block at that scale. The run pins its world inputs:
- the block is copied to `input.block.json`, with its sha256 in `run.json`;
- the GDD version is recorded in `run.json`.

Replay and sheets use the pinned copies, never whatever is on disk.

- **Record:** `runs/levels/harbour-district/r003/`, committed. It holds:
  - `run.json`;
  - the input plan and the input block;
  - for each round: the plan, the checks, each agent's output, its call record (card hash and retrieved text) and Keel's verdict.

  Replay it with `bun run levels:replay r003` → `REPLAY OK`.
- **Report:** [`report.html`](report.html) shows each call's query, retrieved chunks and output side by side, plus each critic verdict. Rebuild it with `bun courses/a04-content-pipeline/build-report.ts r003`.
- **Self-assessment:** [`self-assessment.html`](self-assessment.html).
- **Sheets:**
  - [before, v0.1.0](evidence/r003/sheets/harbour-district-v0.1.0.html): strict mode fails N-R1 (reach) and S-S2 (station spacing); C-0 passes.
  - [round 1, 0.2.0-r003.1](evidence/r003/sheets/harbour-district-v0.2.0-r003.1.html): fails N-R1.
  - [accepted, 0.2.0-r003.2](evidence/r003/sheets/harbour-district-v0.2.0-r003.2.html)
- **Transcripts:** `evidence/r003/transcripts/` holds the prompt, card snapshot, retrieval, schema, stream-json events and output for each call. These are copied from `out/`.
- **Retrieval tweak:** see `evidence/ranks.json`. With the old single query, `standards › tram` (home of `stopSpacing`) ranked 9th, outside the top 8. With the per-objective queries it ranks 2nd to 5th.
- **Critic correction:** this happened without seeding, so no seeded challenge was run. In round 1 the checker failed N-R1, and Keel raised 3 faults:
  - plot rocket-west was 273 m from any stop, over the 250 m limit (→ Datum);
  - S1/skyport stood over water, where nobody could walk to it (→ Vantage);
  - the timetable stated a boarding-price rule it hadn't cited (→ Pulse).

  In round 2, Datum added a T2 stop at Sunset Way (worst reach now 247 m), Vantage removed the Skyport station and handed the missing apron to Datum, and Pulse moved boarding cost into an open question. The checker was clean and Keel passed.

### Earlier runs, kept as evidence
- **`r002`** (GDD v0.14.0, REJECTED after 3 rounds; [report](evidence/r002/report.html), [self-assessment](evidence/r002/self-assessment.html)). It exposed a critic-scope defect. From round 2 on, the only faults were false "cites an unretrieved id" faults: each builder cited only passages from its own retrieval, but Keel checked them against its own top 8. The fix, made before r003: citation membership is checked in code (`tools/levels/cite.ts`), and Keel is shown each builder's retrieved ids and cited text. `levels:replay r002` → `REPLAY OK … outcome REJECTED (recorded)`, with drift labelled "recorded against GDD v0.14.0".
- **`r001`** (superseded; ACCEPTED in 2 rounds; [evidence](evidence/r001/report.html)). It was recorded against the stale Oct 5 civic block, with no pinned block. `levels:replay r001` reports `superseded: recorded against the Oct 5 block, replays at commit 3dbb9ca` and exits 1, because its checker can't be rerun on this tree. Its card and corpus drift is labelled "recorded against GDD v0.14.0".

## Prerequisites
- [Bun](https://bun.sh) 1.4 or later, and git.
- Docker, running. The stack uses the fixed compose project name `suborned` on ports `127.0.0.1:18504` (SurrealDB) and `18505` (embeddings). Any checkout reuses the same containers and volumes, and other services must not hold those ports. `bun run up` from a second checkout rebuilds and recreates those containers, which interrupts any run in progress in another checkout, so don't run two checkouts' generation at once.
- [Claude Code](https://code.claude.com), signed in, with `claude` on `PATH`. Set `CLAUDE_BIN` to use another path. Calls run on your Claude plan. The models come from the crew cards: Opus for Datum, Vantage and Keel; Sonnet for Pulse.
- Unity is **not** needed.

## Run it from a fresh clone
```sh
git clone -b assignment-04 https://github.com/codecoded/suborned.git suborned-a04 && cd suborned-a04
bun install
bun run levels:replay r003     # offline: no Docker, no model. Checks schemas, hashes, retrieved text and the pinned block, reruns the checker → REPLAY OK
bun run up                     # SurrealDB + embedding service; the first start builds the image and downloads the model (a few minutes)
bun run ingest                 # index the GDD and the plan
bun run levels transit         # the generation run → runs/levels/harbour-district/r00N/ (the next free id) (exit 0 ACCEPTED, exit 1 REJECTED or ERROR; see below)
bun run levels:replay r00N     # replay the new run
bun run levels:sheets          # re-render every run's sheets + out/levels/index.html
```
**Exit codes:** `levels transit` exits 0 only when the run is ACCEPTED. Any outcome still writes its `r00N` folder with `run.json` and every round. Exit 1 means REJECTED (3 rounds without checker exit 0 and Keel pass) or ERROR (a call failed twice). Either way, carry on: `levels:replay` and `levels:sheets` work for every recorded run. A REJECTED run is a valid outcome of the critic loop, not a broken install. The faults that stayed open are in `round-3/keel.json`.

Optional: `bun run check` runs the typecheck, the tests (model-free, about 3 min) and the plan checker. `bun run levels:ranks` reprints the retrieval ranks. `bun run levels transit --seed station-on-plot` forces a labelled critic challenge.

## Time and cost per run
r003 made 8 model calls in 2 rounds, with 575 s of model time and about 10 minutes of wall time (calls run in sequence). The CLI reported $2.11. r002 needed all 3 rounds: 12 calls, 591 s, $2.34. A run that needs all 3 rounds makes up to 12 calls (4 per round, or more if a call is retried), so expect roughly 8–15 minutes and $1–3. A test run that needed 3 rounds made 11 calls, took 585 s and cost $2.06. On a subscription the dollar figure is notional usage, not a charge.

## Where outputs land
- `runs/levels/<plan>/r00N/`: committed and small. This is everything replay needs.
- `out/levels/<plan>/r00N/`: git-ignored. It holds the sheets, prompts, retrievals and transcripts. Regenerate the sheets with `levels:sheets`.
- A run never writes `contracts/`. An accepted plan replaces `contracts/world/harbour-district.plan.json` only with Sky's OK.
