# A04 · Content pipeline: generating a city's transit network

This pipeline designs the tram and sky rail (elevated monorail) network for the Harbour District of *The Suborned*, a city-scale tactics game, at build time. The district's layout lives in one JSON file, the **district plan** (`contracts/world/harbour-district.plan.json`: roads, building plots, transit), and the network is generated as edits to it. Four AI agents each retrieve what they need from the game's design documents (the GDD) and the district plan, using RAG over SurrealDB, and then propose their part. Then a deterministic plan checker (geometry rules), code checks (citations, timetable arithmetic, requests between agents) and a critic agent judge the result. Every fault goes back to the agent who owns it, for up to 3 rounds. The output is an accepted revision of the district plan, drawn as a CAD sheet and recorded so that it can be replayed offline.

Why build time: the game's design rule is "no AI at runtime; content generated at build time", and every run must replay exactly. The network is generated once, checked, and committed with everything needed to replay it.

This answers the course brief in [`briefs/001-transit-a04.md`](../../briefs/001-transit-a04.md). The design documents are in [`GDD/`](../../GDD/).

## The agents
The game is built by a crew of AI agents defined in its GDD; four of them run here. The names are the crew names used in the GDD and the run records. Datum, Vantage and Keel run on Claude Opus, and Pulse on Claude Sonnet.

| agent | job | produces each round |
|---|---|---|
| **Datum** | world planner | the tram and sky rail lines (routes, curves, double track) |
| **Vantage** | level designer | stops and stations: positions, platforms, the street each one faces, interchanges |
| **Pulse** | schedule designer | per line: speed, dwell (seconds stopped at each stop) and headway (minutes between services); the route of each journey. Code then works out the timetable. |
| **Keel** | simulation engineer, **the critic** | a verdict. Each fault names its owner and cites the GDD or plan passages it rests on. |

```
each round (at most 3):
  Datum ─► Vantage ─► Pulse        each one: retrieve passages from SurrealDB, then a Claude call with structured output
                │
                ▼
  plan checker (rules) + code checks + Keel (critic)
                │
       all clean? ── yes ─► ACCEPTED
                └── no ──► each fault goes to its owner, with the cited passages pinned, and the next round starts
```

## Terms
- **Run id and version:** runs are numbered `r001`, `r002`, …; a round's plan version reads `0.2.0-r008.1` (next plan version, run 8, round 1).
- **Round:** one pass of Datum → Vantage → Pulse, then judging. At most 3 per run.
- **Fault:** a problem routed to the agent who owns it. It comes from Keel, from a checker rule or from a code check.
- **Hand-off:** one agent's request to another (e.g. Vantage asks Datum for a new line). A blocking hand-off is a fault unless the recipient answers it `done: true` in the same round.
- **Checker rules** (ids in the run records):
  - N-R1: every area within 250 m of a stop;
  - S-S2: sky rail station spacing, 200–500 m;
  - T-L1: tram line ends must be joined, looped or off the sheet;
  - T-S2: platforms on straight track;
  - W-2 / W-3: every stop reachable on foot, and touching the street it names;
  - C-0: the café and records office, the buildings the game's first mission is set around, at their fixed size.

  `bun tools/plan/cli.ts contracts/world/harbour-district.plan.json` lists all of them.
- **Chunk / passage:** a piece of the GDD or plan in the retrieval index, e.g. `§02 LOOP + RESOURCES › travel`.
- **Replay:** rechecks a recorded run without any model or database. It validates every recorded output, re-applies them to the recorded input plan, reruns the checker, and compares the result with what was recorded.
- **Pinned input:** a run copies the files it depends on (e.g. the civic block, the café and office geometry) into its own folder with their hashes, so later edits can't change it.
- **Drift:** something changed since a run was recorded, such as the GDD version, the retrieval chunker or new checker rules. Replay labels it by cause. A run is *superseded* when it can't be rechecked (r001, recorded before inputs were pinned).
- **Seeded / unseeded:** a seeded run has a deliberately planted defect, labelled everywhere, to test the critic. Unseeded runs have none.

## Quick start
**Needs:** [Bun](https://bun.sh) 1.4+ and git. A full run also needs Docker and [Claude Code](https://code.claude.com), signed in. Unity is not needed. Run every command from the repo root (the cloned folder); all paths here are relative to it, except links to files in this folder.

**Check the recorded runs** (offline, no Docker, no model, 1–2.5 min):
```sh
git clone -b assignment-04 https://github.com/codecoded/suborned.git suborned-a04 && cd suborned-a04
bun install
bun run verify        # typecheck, replays every committed run, reruns the checker → VERIFY OK
```

**Generate a new run.** A run takes 5–17 min, plus about 3 min of one-time setup. It uses $2–3.50 of model usage: notional on a Claude subscription, real money only on API billing.
```sh
bun run doctor           # preflight: Bun, Docker, ports, Claude Code. Prints the fix for anything that fails
bun run up               # starts SurrealDB and the local embedding service (first start ~3 min: downloads images and the model)
bun run ingest           # indexes the GDD and the plan into SurrealDB (25–90 s)
bun run levels transit   # the run → runs/levels/harbour-district/<run-id>/ (it prints the id, e.g. r011)
bun run levels:replay r011   # replay it, using the id the run printed
bun run levels:sheets    # draws every run's sheets and out/levels/index.html
bun run down             # afterwards: stops the stack. `docker compose down -v` also deletes the index and model cache
```
A run ends **ACCEPTED** (exit 0) or **REJECTED** (exit 1, when 3 rounds weren't enough). Both are normal outcomes of the critic loop, and either way the run's full record is written and replays. Exit 1 can also mean **ERROR**, a model call that failed twice: the run's `run.json` `outcome` tells you which, and Troubleshooting says what to do. If your Claude plan has no Opus in Claude Code, run `CLAUDE_MODEL=sonnet bun run levels transit`.

## What you'll see
- **Terminal:** one line per call and one per verdict:
  ```
  [01-transit-lines] 8 passages; world-planner attempt 1
  round 1 0.2.0-r008.1: checker FAIL N-R1
  round 1: Keel fail, 2 fault(s) → Datum
  round 2 0.2.0-r008.2: checker FAIL T-L1
  round 2: Keel fail, 2 fault(s) → Datum, Vantage
  round 3 0.2.0-r008.3: checker clean
  round 3: Keel pass, 0 fault(s) → ACCEPTED
  r008 ACCEPTED (0.2.0-r008.3) → runs/levels/harbour-district/r008
  ```
- **`out/levels/index.html`:** every run (date, outcome, rounds, cost), with a link to each round's sheet.
- **CAD sheets:** the district drawn to scale (roads, plots, lines, stops, walking routes), with the checker's PASS/FAIL table beside the drawing.
- **[`report.html`](report.html):** for each agent call in the submitted run, the queries, the retrieved passages and the output side by side, then the critic's verdict.

## For markers
**Submitted run:** `r008`, ACCEPTED in 3 rounds, unseeded. **Seeded critic challenges:** `r009` and `r010`. **Earlier runs:** `r001`–`r007`, each of which found a defect that was fixed before the next run. The full story of every run is in **[`runs.html`](runs.html)**.

| requirement | where |
|---|---|
| Game-specific pipeline | This page. Code: `tools/levels/` (pipeline), `tools/rag/` (retrieval), `tools/plan/` (checker) |
| Retrieval (RAG) | SurrealDB HNSW vector search + BM25, fused by rank (`tools/rag/retrieval.ts`). Each call record (`runs/levels/harbour-district/r008/round-*/*.call.json`) lists its queries and retrieved passages |
| Three generated outputs | `runs/levels/harbour-district/r008/round-3/`: `datum.json`, `vantage.json`, `pulse.json` (+ `timetable.json`, derived) |
| Query, retrieved chunks and output side by side | [`report.html`](report.html) |
| Critic correction | Unseeded: r008 (checker, critic and hand-offs), and Keel's own catches in r003 and r004. Seeded and labelled: r010 and r009. The details, including their limits, are in [`runs.html`](runs.html) |
| Before / after | [before](evidence/r008/sheets/harbour-district-v0.1.0.html) and [accepted](evidence/r008/sheets/harbour-district-v0.2.0-r008.3.html) sheets |
| Self-assessment with a concrete tweak | [`self-assessment.html`](self-assessment.html). The tweaks are to retrieval queries and to how the GDD is chunked, with ranks before and after in [`evidence/ranks.json`](evidence/ranks.json) (`bun run levels:ranks`) |

**Verify at the level your environment allows:**

| level | needs | command | expect |
|---|---|---|---|
| 0 · read | nothing | open [`report.html`](report.html), [`runs.html`](runs.html) and [`self-assessment.html`](self-assessment.html), all in `courses/a04-content-pipeline/` | — |
| 1 · offline | Bun | `bun install && bun run verify` | `VERIFY OK: 9 runs replayed, 1 superseded (expected) (submitted r008 ACCEPTED), checker clean, typecheck clean` |
| 1b · tests | Bun | `bun run check` (3–10 min): typecheck, every unit test and the plan checker, whereas `verify` replays the recorded runs | exit 0 |
| 2 · retrieval | + Docker | `bun run up && bun run ingest && bun run retrieve "how does the player use trams and sky rail and what does riding cost"` | ranked passages from SurrealDB, with `§02 LOOP + RESOURCES › travel` near the top |
| 3 · generation | + Claude Code | `bun run doctor && bun run levels transit` (5–17 min) | a new run, ACCEPTED or REJECTED; then `bun run levels:replay <id>` → REPLAY OK |

**For AI markers:** if your shell tool has a 2-minute limit, give `verify` a 5-minute timeout, and run levels 1b–3 in the background. Only `levels transit` adds files: a new run folder.

## Troubleshooting
| symptom | fix |
|---|---|
| `bun run up`: port is already allocated | Something else holds 18504 (SurrealDB) or 18505 (embeddings). Stop it; `bun run doctor` names the port. |
| `bun run up`: cannot connect to the Docker daemon | Start Docker Desktop, or the Docker service. |
| The first `up` is slow or fails | The embedding service downloads its model (`BAAI/bge-small-en-v1.5`, ~130 MB, the local model behind the vector search; not Claude) from huggingface.co, once. Rerun `bun run up`. |
| `levels transit`: "No corpus indexed" or "changed since the last ingest" | Run `bun run ingest`. |
| A run ends with ERROR | Read `out/levels/harbour-district/<run>/calls/*.stderr.txt`. If you're not signed in, run `claude` once. For a model-access error, use `CLAUDE_MODEL=sonnet`. |

The stack uses the compose project `suborned`, about 1 GB of disk and 1.5 GB of memory. Every checkout shares it, so don't run generation from two checkouts at once. It was tested from a fresh clone with an empty Docker on macOS arm64, and the images also build and run on linux/amd64.

## Where outputs land
- `runs/levels/harbour-district/<run>/`: committed and small. It holds everything replay needs: inputs, the pinned civic block, and each round's plan, checks, outputs, call records and verdict.
- `out/levels/harbour-district/<run-id>/`: git-ignored. It holds the sheets, prompts, full transcripts and error logs (`calls/*.stderr.txt`). Recreate the sheets with `levels:sheets`.
- A run never edits `contracts/`. An accepted plan replaces the district plan only with the designer's approval.
