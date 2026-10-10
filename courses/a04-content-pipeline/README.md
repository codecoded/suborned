# A04 · Content pipeline: the Harbour District transit network

## For markers (human or AI)
**Submitted run:** `r008`, ACCEPTED in 3 rounds. The seven earlier runs (r001–r007) are kept as evidence. Each found a defect that was fixed before the next run.

### Where each requirement is met
| requirement | evidence |
|---|---|
| A game-specific pipeline | "Why this matters" and "How it works" below. The code is in `tools/levels/` (pipeline), `tools/rag/` (retrieval) and `tools/plan/` (deterministic checker). |
| Retrieval over the game's own sources | SurrealDB HNSW + BM25 with reciprocal rank fusion (`tools/rag/retrieval.ts`). Each call record `runs/levels/harbour-district/r008/round-*/*.call.json` lists its queries and every retrieved passage, with source, section, signals and score. |
| Three generated outputs | `runs/levels/harbour-district/r008/round-3/`: `datum.json` (lines), `vantage.json` (stops and stations), `pulse.json` (speed, dwell, headway, routes) and `timetable.json` (the timetable derived from them in code). |
| Query, retrieved chunks and output side by side | [`report.html`](report.html), one section per call. |
| A critic correction | "How the submitted run went" below. In r008 the corrections come from the checker, the critic's diagnosis and owner hand-offs. Keel's own catches, which no rule covers, are in earlier runs: see the "Earlier runs" table. |
| Before / after | Sheets [`evidence/r008/sheets/harbour-district-v0.1.0.html`](evidence/r008/sheets/harbour-district-v0.1.0.html) (before) and [`…-v0.2.0-r008.3.html`](evidence/r008/sheets/harbour-district-v0.2.0-r008.3.html) (accepted). Every round's `checks.json` gives each rule's result. |
| README | This file. |
| Self-assessment with a concrete tweak | [`self-assessment.html`](self-assessment.html). The retrieval tweaks' ranks are in `evidence/ranks.json` and reproduce with `bun run levels:ranks`. |
| Reproducibility | `bun run verify` replays every committed run offline. Fresh clones produced r004 (ACCEPTED) and r007 (a simulated AI marker's run, REJECTED); both are committed. A third fresh-install run, with an empty Docker, was ACCEPTED, but its folder was not kept; only its timings are reported under "Measured from nothing". |

### Verify it: pick the level your environment allows
| level | needs | command | time | expect |
|---|---|---|---|---|
| 0 · read | nothing | open `report.html`, `self-assessment.html`, `runs/levels/harbour-district/r008/` | none | — |
| 1 · offline | Bun | `bun install && bun run verify` | about 1 min | one line per run with its drift labelled by cause, then `VERIFY OK: 8 runs replayed (submitted r008 ACCEPTED) …` |
| 1b · full tests | Bun | `bun run check` | 3–10 min | exit 0 |
| 2 · retrieval | + Docker | `bun run up && bun run ingest && bun run retrieve "how does the player use trams and sky rail and what does riding cost"` | about 3 min the first time | ranked passages from SurrealDB, `§02 LOOP + RESOURCES › travel` among the first; each hit names its signals (`semantic`, `keyword` or both) |
| 3 · generation | + Claude Code, signed in | `bun run doctor && bun run levels transit` | 5–17 min | a new `r00N`. ACCEPTED (exit 0) or REJECTED (exit 1) are both valid outcomes. Then `bun run levels:replay r00N` → REPLAY OK |

AI tools with a 2-minute command limit should run levels 1b, 2 and 3 in the background, or with a longer timeout. Only `levels transit` adds files: a new run folder. `levels:ranks --evidence` would rewrite `evidence/ranks.json`; plain `levels:ranks` only writes to `out/`.

## What it generates
Three outputs for The Suborned's Harbour District, as edits to the district plan (`contracts/world/harbour-district.plan.json`, schema `suborned.plan/1`):

1. **Lines** (Datum, world planner): tram and sky rail routes, with fillet radii, double track and a path `order`.
2. **Stops and stations** (Vantage, level designer): positions on the lines, platforms, the road or path each entrance fronts, and interchange pairs.
3. **Timetable** (Pulse, schedule and AI designer): speed, dwell and headway per line, and the route of each journey. Run times and journey totals are derived in code from the checker's chainages. The GDD's boarding rules have to come from retrieval: the briefs say only "cite the GDD", not what the rules are. Anything the GDD doesn't decide goes to Motive as an open question.

## Why this matters for this game
The Suborned is a commander game. The cell moves through a city that runs on schedules, and a tram or sky rail is a window the player can see coming (pillar: orchestrate the city). Interchanges and stations are where the police seal the district. The GDD's house rule is "no AI at runtime; content generated at build time; same orders → same result". So the network is generated once, checked deterministically, and committed with everything needed to replay it.

## How it works
- **Objective:** a layer-4 brief, one per crew member, in `tools/levels/objectives/`.
- **Retrieval:** SurrealDB HNSW (BGE-small) and BM25 run per query, merged by reciprocal rank fusion. The index covers the agent GDD (chunked per section, with long sections split into their tagged entries, e.g. `§02 LOOP + RESOURCES › travel`), the human GDD (by heading) and the plan (by part).
- **Crew member:** runs headless through `claude -p`. Its crew card is the system prompt and structured output is its only tool. Builders run in order: Datum, then Vantage, then Pulse.
- **Validation:** each output is checked against its zod schema, then applied to a copy of the plan.
- **Plan checker:** `tools/plan/cli.ts --strict` gives exit code 0 or 1 and draws a CAD sheet. It checks against the run's pinned civic block. Its rules cover reach, spacing, connectivity, line ends, and walk access to every stop (W-2, W-3).
- **Checks in code:**
  - every cited passage was in that builder's own retrieval (`cite.ts`);
  - the timetable is derived and its routes checked (`timetable.ts`);
  - a blocking hand-off between owners is a fault unless the recipient runs later in the same round and answers it `done: true` (`handoff.ts`).
- **Critic:** Keel judges what no rule covers, and whether `done: true` answers are true. Keel's faults name an owner and cite chunk ids. Faults made by code name the checker rule or code check instead, with no chunk ids. A claim Keel can't find in its passages is an open question, not a fault.
- **Revision:** each fault goes back to its owner, with Keel's cited chunks pinned into their retrieval, for up to 3 rounds. A reach failure (N-R1) goes to both Datum and Vantage, because a new line or a moved stop can fix it.
- **ACCEPTED** means checker exit 0, Keel pass and no open fault from the code checks.
- **Replay:** `levels:replay` rebuilds a run offline from `runs/`. It labels drift by cause: GDD version, chunker version, superseded block, checker rules added since the run. Integrity failures still fail.

## The submitted run: `r008`, ACCEPTED in 3 rounds
r008 ran on GDD v0.15.0, chunker v2 (corpus of 190 chunks) and the corrected civic block: the Oct 9 city scale, with the café as the 22 × 11.3 × 34.8 m corner building, held by rule C-0. It also ran on the plan with Datum's Skyport Pier and apron. The run pins its block (`input.block.json` and its sha256) and records its GDD and chunker versions.

- **Record:** `runs/levels/harbour-district/r008/`, committed. It holds:
  - `run.json`;
  - the input plan and the input block;
  - for each round: the plan, the checks, each agent's output and call record, the derived timetable, the faults and Keel's verdict.

  Replay it with `bun run levels:replay r008` → `REPLAY OK`.
- **Report:** [`report.html`](report.html). Rebuild it with `bun courses/a04-content-pipeline/build-report.ts r008`.
- **Self-assessment:** [`self-assessment.html`](self-assessment.html).
- **Sheets:**
  - [before, v0.1.0](evidence/r008/sheets/harbour-district-v0.1.0.html): strict mode fails N-R1 and S-S2.
  - [round 1](evidence/r008/sheets/harbour-district-v0.2.0-r008.1.html): N-R1 fails.
  - [round 2](evidence/r008/sheets/harbour-district-v0.2.0-r008.2.html): T-L1 fails.
  - [accepted, round 3](evidence/r008/sheets/harbour-district-v0.2.0-r008.3.html)
- **Transcripts:** `evidence/r008/transcripts/` holds the prompt, card snapshot, retrieval, schema, stream-json events and output for each call.
- **The accepted plan** passes all 48 strict rules:
  - the worst area is 247 m from transit (crescent-north; the limit is 250 m);
  - S1 station spacing is 255, 245 and 205 m, with Skyport on the pier apron;
  - every stop can be reached on foot and touches the road or path it fronts (W-2 and W-3, 14/14);
  - the café is 22.0 × 11.3 × 34.8 m (C-0).

### How the submitted run went (all unseeded)
- **Round 1:** the checker failed N-R1, with four areas out of reach (docks-east 294 m, lantern-west 289 m, harbour-east 279 m, rocket-west 273 m).
  - Keel diagnosed it as a line problem and routed it to Datum: no stop on the existing lines could reach those areas.
  - Vantage raised the same point as a blocking hand-off to Datum.
  - Vantage also answered `done: true` to Datum's ask to move Skyport for spacing, and Keel confirmed the answer held (Skyport at x 545, gap 205 m).
- **Round 2:** Datum added T2 as a ring, which fixed reach. The checker then failed T-L1, because it read the ring's two ends as open with no turning loop.
  - Keel explained the cause and routed it to Datum.
  - Vantage answered one of Datum's asks `done: false`, which became a fault for Vantage.
- **Round 3:** Datum made T2 an open U, and Vantage placed its 7 stops and answered every ask. The checker was clean and Keel passed: ACCEPTED.

In r008 Keel's two faults restate checker failures, with a diagnosis and the right owner, and it raised no fault of its own. Its independent catches are in earlier runs (below).

**Retrieval:** in every round the GDD's boarding rule (`§02 … › travel`) reached Pulse at rank 4 and Keel at rank 8 from a natural question, with no rule text in the prompts. Keel judged the timetable against it ("time only, no fare").

### Earlier runs, kept as evidence
| run | outcome | what it taught (from its records) | fix before the next run |
|---|---|---|---|
| [`r001`](evidence/r001/report.html) | ACCEPTED, now **superseded** | The checker read the civic block from disk, so the Oct 5 → Oct 9 block change broke replay. | Runs pin `input.block.json` and its sha256. r001 replays as `superseded … replays at commit 3dbb9ca`. |
| [`r002`](evidence/r002/report.html) | REJECTED | Keel checked builders' citations against its own retrieval, so its "unretrieved id" faults were false and never cleared. | Citations are checked in code (`cite.ts`), and Keel sees each builder's retrieved set. |
| [`r003`](evidence/r003/report.html) | ACCEPTED | **Keel's own catch:** a Skyport station over water that nobody could walk to. The station was removed. **Keel's error:** it faulted Pulse's boarding rule, which was true, because neither had retrieved §02. Its accepted plan also fails the W-2 and W-3 walk rules added later (replay reports it). | W-2 and W-3 walk rules. A claim Keel can't find in its passages is "unverified", not a fault. |
| [`r004`](evidence/r004/sheets/harbour-district-v0.2.0-r004.3.html) | ACCEPTED, from a fresh clone | **Keel's own catches** (round 1): a run time off by 1 s and a miscounted spacing note were fixed; the Skyport station over water was held open, not fixed. **Keel's error, again:** it faulted Pulse's true "§02 says time only". Vantage's Skyport hand-off to Datum was never routed. | `handoffs` on every builder. Datum planned the Skyport Pier and apron. |
| [`r005`](evidence/r005/sheets/harbour-district-v0.2.0-r005.3.html) | REJECTED | The first run to place Skyport on the new apron. It still failed on Pulse's hand-computed run times, which matched stale stop positions. | Run times and journey totals are derived in code (`timetable.ts`). |
| [`r006`](evidence/r006/report.html) | ACCEPTED | A simulated AI marker found round 3 redundant: hand-offs were never checked as answered, and N-R1 always went to Vantage. The §02 query nearly quoted its target, so its rank proved nothing. | Hand-off answering, N-R1 routed to both owners, natural §02 queries and v2 chunking. |
| [`r007`](evidence/r007/sheets/harbour-district-v0.2.0-r007.3.html) | REJECTED, a simulated AI marker's run from a fresh GitHub clone | **Keel's own catch:** a reach claim that broke the standard (round 1). The run was rejected only because Vantage had fulfilled Datum's round-3 hand-off but nothing checked it as answered. | The same hand-off answering fix. |

`bun run verify` replays every run and labels drift by cause: GDD v0.14.0, chunker v1, checker rules added since the run (W-2, W-3), and r001's superseded block.

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
bun run up                     # SurrealDB + embedding service. First start pulls SurrealDB, builds the image and downloads the embedding model from Hugging Face (about 3 min)
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
| `levels transit` (ACCEPTED in 2 rounds; that run folder was not kept) | 414 s |
| replay of that run | 14 s |
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
| The first `up` is slow or times out | On first start the embedding service downloads its model, `BAAI/bge-small-en-v1.5` (about 130 MB, the local model that embeds passages and queries for SurrealDB's vector search; not Claude), from the Hugging Face Hub into the `embedding-cache` volume. That needs network access to huggingface.co the first time; after that it's cached and needs no network. If the download fails, rerun `bun run up`. |

Optional: `bun run check` runs the typecheck, the tests (model-free, 3–10 min) and the plan checker. `bun run levels:ranks` reprints the retrieval ranks. `bun run levels transit --seed station-on-plot` forces a labelled critic challenge.

## Time and cost per run
r008 made 12 model calls in 3 rounds, with 658 s of model time and about 12 minutes of wall time (calls run in sequence). The CLI reported $2.66. The committed runs r002–r008 made 8–12 calls and used 486–931 s of model time each, at $1.83–3.31. Budget about 20 minutes and $3.50 for one run. On a subscription the dollar figure is notional usage, not a charge.

## Where outputs land
- `runs/levels/<plan>/r00N/`: committed and small. This is everything replay needs.
- `out/levels/<plan>/r00N/`: git-ignored. It holds the sheets, prompts, retrievals and transcripts. Regenerate the sheets with `levels:sheets`.
- A run never writes `contracts/`. An accepted plan replaces `contracts/world/harbour-district.plan.json` only with Sky's OK.
