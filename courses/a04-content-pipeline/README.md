# A04 · Content pipeline: the Harbour District transit network

## What it generates
Three outputs for The Suborned's Harbour District, as edits to the district plan (`contracts/world/harbour-district.plan.json`, schema `suborned.plan/1`):

1. **Lines** (Datum, world planner): tram and sky rail routes, with fillet radii, double track and a path `order`.
2. **Stops and stations** (Vantage, level designer): positions on the lines, platforms, the public space each entrance fronts, and interchange pairs.
3. **Timetable** (Pulse, schedule and AI designer): speed, dwell and headway per line, plus the route of each journey. Run times and journey totals are derived in code from the checker's chainages. GDD v0.15 §02 makes boarding fast travel priced in time only (the wait plus the ride), so anything beyond that goes to Motive as an open question and is never invented.

## Why this matters for this game
The Suborned is a commander game. The cell moves through a city that runs on schedules, and a tram or sky rail is a window the player can see coming (pillar: orchestrate the city). Interchanges and stations are where the police seal the district. The GDD's house rule is "no AI at runtime; content generated at build time; same orders → same result". So the network is generated once, checked deterministically, and committed with everything needed to replay it.

## How it works
- **Objective:** a layer-4 brief, one per crew member, in `tools/levels/objectives/`.
- **Retrieval:** SurrealDB HNSW (BGE-small) and BM25 run per query, merged by reciprocal rank fusion, over the agent GDD, the human GDD and the plan.
- **Crew member:** runs headless through `claude -p`. Its crew card is the system prompt and structured output is its only tool.
- **Validation:** each output is checked against its zod schema, then applied to a copy of the plan.
- **Plan checker:** `tools/plan/cli.ts --strict` gives exit code 0 or 1 and draws a CAD sheet. It checks the run's pinned civic block, and its rules include reach, spacing, connectivity and walk access to every stop (W-2, W-3).
- **Checks in code:**
  - every cited passage was in that builder's own retrieval (`cite.ts`);
  - the timetable is derived and its routes checked (`timetable.ts`);
  - a blocking hand-off between owners becomes a fault for its owner (`handoff.ts`).
- **Critic:** Keel judges what no rule covers. Every fault names an owner and cites chunk ids. A claim Keel can't find in its passages is an open question, not a fault.
- **Revision:** each fault goes back to its owner with the cited chunks pinned into their retrieval, for up to 3 rounds.
- **ACCEPTED** means checker exit 0, Keel pass and no open fault from the code checks.
- **Replay:** `levels:replay` rebuilds a run offline from `runs/`. It labels drift by cause (GDD version, superseded block, checker rules added since the run). Integrity failures still fail.

## The submitted run: `r006`, ACCEPTED in 3 rounds
r006 ran on:
- GDD v0.15.0, corpus `dfd554a9695c`;
- the corrected civic block: the Oct 9 city scale, with the café as the 22 × 11.3 × 34.8 m corner building. Rule C-0 holds the block at that scale;
- the plan with Datum's Skyport Pier and apron.

The run pins its world inputs. The block is copied to `input.block.json`, with its sha256 in `run.json`, and the GDD version is recorded too. Replay and sheets use the pinned copies, never whatever is on disk.

- **Record:** `runs/levels/harbour-district/r006/`, committed. It holds:
  - `run.json`;
  - the input plan and the input block;
  - for each round: the plan, the checks, each agent's output, its call record (card hash and retrieved text), the derived timetable and Keel's verdict.

  Replay it with `bun run levels:replay r006` → `REPLAY OK`.
- **Report:** [`report.html`](report.html) shows each call's query, retrieved chunks and output side by side, plus each critic verdict. Rebuild it with `bun courses/a04-content-pipeline/build-report.ts r006`.
- **Self-assessment:** [`self-assessment.html`](self-assessment.html).
- **Sheets:**
  - [before, v0.1.0](evidence/r006/sheets/harbour-district-v0.1.0.html): strict mode fails N-R1 (reach) and S-S2 (station spacing).
  - [round 1, 0.2.0-r006.1](evidence/r006/sheets/harbour-district-v0.2.0-r006.1.html): fails T-S2 and N-R1.
  - [round 2, 0.2.0-r006.2](evidence/r006/sheets/harbour-district-v0.2.0-r006.2.html): checker clean, but a blocking hand-off is still open.
  - [accepted, 0.2.0-r006.3](evidence/r006/sheets/harbour-district-v0.2.0-r006.3.html)
- **Transcripts:** `evidence/r006/transcripts/` holds the prompt, card snapshot, retrieval, schema, stream-json events and output for each call. These are copied from `out/`.
- **What the accepted network has:**
  - three tram lines, T1, T2 and a new T3 Sunset Line, which brings lantern-west and rocket-west into reach (worst area 247 m, limit 250 m);
  - the S1 sky rail, with stations at Sunset Way, Gardens, Crown Circus, Harbour and **Skyport**. Skyport stands on the new pier apron, and you can walk to every station (rule W-2).
  - The timetable is derived in code from the checker's chainages. Boarding is fast travel priced in time only, as GDD §02 says.
- **Critic correction:** this happened without seeding, so no seeded challenge was run.
  - Round 1: the checker failed T-S2 (a T2 platform off straight track) and N-R1 (two areas out of reach). Datum and Vantage also raised blocking hand-offs to each other.
  - Round 2: Datum added T3 and Vantage moved the platform. The checker was clean and Keel passed, but a blocking hand-off from Datum (place the T3 stops and the Sunset Way interchange) was still open, so the run went on.
  - Round 3: Vantage placed those stops, and the run was ACCEPTED.
- **Retrieval tweaks:** see `evidence/ranks.json` (`bun run levels:ranks --evidence`).
  - `standards › tram`, home of `stopSpacing`, now ranks 2nd to 6th with the per-objective queries.
  - §02 travel (boarding as fast travel, time only) moved from 11th to 2nd for Pulse, and from outside the top 12 to 7th for Keel.

### Earlier runs, kept as evidence
| run | GDD | outcome | what it taught | fix before the next run |
|---|---|---|---|---|
| [`r001`](evidence/r001/report.html) | v0.14.0 | ACCEPTED, now **superseded** | The checker read the civic block from disk, so the Oct 5 → Oct 9 block change broke replay. | Runs pin `input.block.json` and its sha256. `levels:replay r001` reports `superseded: recorded against the Oct 5 block, replays at commit 3dbb9ca` and exits 1, because its checker can't be rerun. |
| [`r002`](evidence/r002/report.html) | v0.14.0 | REJECTED | The critic checked builders' citations against its own retrieval, so false "unretrieved id" faults never cleared. | Citations are checked in code (`cite.ts`), and Keel sees each builder's retrieved set. |
| [`r003`](evidence/r003/report.html) | v0.15.0 | ACCEPTED | Neither Pulse nor Keel retrieved §02, so Keel faulted Pulse's true boarding rule. The new walk rule shows its S1 Harbour had no walk. | §02 queries were added. A claim Keel can't find in its passages is now "unverified", not a fault. Rules W-2 and W-3 were added. |
| [`r004`](evidence/r004/sheets/harbour-district-v0.2.0-r004.3.html) | v0.15.0 | ACCEPTED, from a **fresh clone** | The README works end to end. The same §02 miss came back. Vantage's Skyport hand-off to Datum was never routed. | `handoffs` were added to every builder schema, and a blocking hand-off is a fault for its owner. Datum planned the Skyport Pier and apron. |
| [`r005`](evidence/r005/sheets/harbour-district-v0.2.0-r005.3.html) | v0.15.0 | REJECTED | With the checker clean, the last fault was Pulse's hand-computed run times, which matched stale stop positions. That arithmetic error also appeared in r002 and r004. | Run times and journey totals are derived in code (`timetable.ts`). Pulse chooses speed, dwell, headway and routes. |

Every run from r002 to r006 replays OK, with drift labelled by cause: "recorded against GDD v0.14.0" and "checker rules added since the run: W-2, W-3".

## Prerequisites
- [Bun](https://bun.sh) 1.4 or later, and git.
- Docker (Desktop, or Engine with Compose v2), running. The stack needs about 1 GB of disk for images and the model, and 1.5 GB of memory.
  - It uses the fixed compose project name `suborned`, on ports `127.0.0.1:18504` (SurrealDB) and `18505` (embeddings). Other services must not hold those ports.
  - Any checkout reuses the same containers and volumes. Running `bun run up` from a second checkout recreates those containers, so don't run generation from two checkouts at once.
- [Claude Code](https://code.claude.com), signed in, with `claude` on `PATH` (tested with 2.1.296). Set `CLAUDE_BIN` to use another path.
  - Calls run on your Claude plan. The crew cards ask for Opus (Datum, Vantage, Keel) and Sonnet (Pulse).
  - If your plan has no Opus in Claude Code, prefix the run with `CLAUDE_MODEL=sonnet`.
- Unity is **not** needed.
- Tested from a fresh GitHub clone with an empty Docker (no images, volumes or model) on macOS arm64. The Docker images are also built and run on linux/amd64.

## Run it from a fresh clone
```sh
git clone -b assignment-04 https://github.com/codecoded/suborned.git suborned-a04 && cd suborned-a04
bun install
bun run doctor                 # preflight: Bun, Docker, ports, Claude Code and its flags; one line per check, with the fix for any FAIL
bun run levels:replay r006     # offline: no Docker, no model. Checks schemas, hashes, retrieved text and the pinned block, reruns the checker → REPLAY OK
bun run up                     # SurrealDB + embedding service. First start pulls SurrealDB, builds the image and downloads the model (about 3 min)
bun run ingest                 # index the GDD and the plan into SurrealDB (about 25 s)
bun run doctor                 # optional: now also checks SurrealDB, the embeddings and that the corpus is current
bun run levels transit         # the generation run → runs/levels/harbour-district/r00N/ (the next free id) (exit 0 ACCEPTED, exit 1 REJECTED or ERROR; see below)
bun run levels:replay r00N     # replay the new run
bun run levels:sheets          # re-render every run's sheets + out/levels/index.html
```
**Measured from nothing** (fresh GitHub clone, empty Docker):

| step | time |
|---|---|
| clone + install | 7 s |
| replay r006 | 20 s |
| `up` | 163 s |
| `ingest` | 24 s |
| `levels transit` (r007, ACCEPTED in 2 rounds) | 414 s |
| replay r007 | 14 s |
| sheets | 97 s |

That is about 12 minutes in total.

**Where the database is used:** every model call in `levels transit` retrieves its passages from SurrealDB, with an HNSW vector search and a BM25 full-text search merged by reciprocal rank fusion. `levels:replay` doesn't need the database. Each call record stores the exact passages retrieved, and replay checks them against the GDD and plan on disk.

**Exit codes:** `levels transit` exits 0 only when the run is ACCEPTED. Any outcome still writes its `r00N` folder with `run.json` and every round. Exit 1 means REJECTED (3 rounds without an accepted round) or ERROR (a call failed twice). Either way, carry on: `levels:replay` and `levels:sheets` work for every recorded run. A REJECTED run is a valid outcome of the critic loop, not a broken install. The faults that stayed open are in `round-3/faults.json`.

**If something fails:**

| symptom | fix |
|---|---|
| `bun run up`: port is already allocated | Something else holds 18504 or 18505. Stop it. `bun run doctor` names the port. |
| `bun run up`: cannot connect to the Docker daemon | Start Docker Desktop, or the Docker service. |
| `levels transit`: "No corpus indexed" or "changed since the last ingest" | Run `bun run ingest`. Run it again after any GDD or plan change. |
| `levels transit`: a call fails twice and the run ends with ERROR | Look at `out/levels/harbour-district/r00N/calls/*.stderr.txt`. If you're not signed in, run `claude` once and sign in. For a model access error, prefix the run with `CLAUDE_MODEL=sonnet`. |
| The first `up` is slow or times out | The model download needs network access to Hugging Face. Rerun `bun run up`, which resumes. |

Optional: `bun run check` runs the typecheck, the tests (model-free, about 3 min) and the plan checker. `bun run levels:ranks` reprints the retrieval ranks. `bun run levels transit --seed station-on-plot` forces a labelled critic challenge.

## Time and cost per run
r006 made 11 model calls in 3 rounds, with 509 s of model time and about 10 minutes of wall time (calls run in sequence). The CLI reported $1.90. Runs r002–r006 cost $1.83–2.64 each. A run that needs all 3 rounds makes up to 12 calls (4 per round, or more if a call is retried), so expect roughly 8–15 minutes and $1–3. A test run that needed 3 rounds made 11 calls, took 585 s and cost $2.06. On a subscription the dollar figure is notional usage, not a charge.

## Where outputs land
- `runs/levels/<plan>/r00N/`: committed and small. This is everything replay needs.
- `out/levels/<plan>/r00N/`: git-ignored. It holds the sheets, prompts, retrievals and transcripts. Regenerate the sheets with `levels:sheets`.
- A run never writes `contracts/`. An accepted plan replaces `contracts/world/harbour-district.plan.json` only with Sky's OK.
