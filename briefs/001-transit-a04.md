# 001 · Transit network, generated (Assignment 4)

Deadline: **Sun 12 Oct, 11:59 BST**. Runs first; needs only the plan, the checker and `tools/rag`.

Goal: Sky's direction, generated through the build-time pipeline instead of drawn by hand. "Sky rail… should run across the city, and we have stations for player to get on / off it, same for tram, player can board it for fast nav around city." Retrieval → crew member proposes → plan checker (deterministic) → Keel (critic) → revisions routed to the owner (≤ 3 rounds) → an accepted plan revision.

Assignment 4 asks for: a game-specific pipeline, three generated outputs, query / retrieved chunk / output evidence side by side, a critic correction, a short README, and a self-assessment with a concrete prompt or retrieval tweak.

## Three outputs
1. **Lines** (Datum): sky rail and tram alignments as plan edits: paths, fillet radii, double track, inside `plan.standards`. Starting point (tide's draft, a proposal, not a given): a two-way Sky Loop round the district; T1 running through the circus; a T2 Harbour Line on Harbour Road.
2. **Stops and stations** (Vantage): positions on the lines, platforms, entrances fronting public space, interchange pairs; every area in reach of one.
3. **Timetable** (Pulse): headway, dwell and run times per line; journey times between stations. Datum's hold says the cell rides trams and sky rail between stops and stations; boarding has no price in the GDD: record it as an open question for Motive and Sky, never invent a price.

## Submission flow (Sky, 2026-10-10)
1. The crew works on branch `assignment-04`, on this brief only. 002 (visible world) and 003 (missions) run on main afterwards.
2. **Run 1:** one real run ends ACCEPTED, and `runs/levels/<plan-id>/r001/` plus the course folder are ready. The crew never commits; Sky commits and pushes the branch.
3. **Run 2:** a fresh clone of `assignment-04` into a new folder, following only `courses/a04-content-pipeline/README.md` (prerequisites, install, `up`, `ingest`, the run). It must produce `r002`, and `bun run levels:replay r001` must pass there.

   Any README step that fails is a fault for Splice (pipeline) or Aster (README). It is fixed on the branch and pushed, then run 2 repeats from a new clone.
4. Submit the branch when run 2 works.

The README states prerequisites (Bun, Docker, Claude Code signed in, Unity not needed), the expected time and model cost per run, and where the outputs land.

## A · Splice · the pipeline
objective: `tools/levels/` with `bun run levels transit`: layer-4 objectives in `tools/levels/objectives/`, zod schemas per output, proposals applied to a copy of the plan under `out/levels/<run>/`, the checker run on it and its CAD sheet drawn there each round (`bun tools/plan/cli.ts <plan> --out <sheet> --json <report>`), Keel as critic (objective + schema, cites chunk ids), faults routed to their owner with the cited chunks pinned. New checker rules first, each with an id, a source and a failing example: no dead ends (ends joined or off-sheet; closed loops fine), every area within 250 m of a stop or station, network connected through interchanges, sky rail station spacing 200–500 m, a line `order` so stop spacing reads across paths. `bun run levels:replay` checks a recorded run offline (schemas, source hashes, retrieved text, rerun checker; no model).
done-check: `bun run check` passes with the new rules and tests · one real run ends ACCEPTED (checker exit 0 + Keel pass) · replay passes on a fresh copy of the repo.
next owner: Aster.

### Versioning: every run is kept
- Run ids are sequential per plan: `r001`, `r002`, … Each round's candidate plan carries `version: <next minor>-r<run>.<round>` (e.g. `0.2.0-r003.2`), so every sheet and check report names exactly what it shows.
- Committed, small: `runs/levels/<plan-id>/r003/` holds `run.json` (date, objective, input plan version, corpus hash, card hashes, rounds, outcome, cost), and per round `plan.json`, `checks.json`, each agent output and Keel's verdict.
- Regenerated, ignored: `out/levels/<plan-id>/r003/` holds sheets, prompts, retrievals and transcripts. The sheet is derived from the plan, so `bun run levels:sheets` re-renders every run's sheets from the committed plans plus an `index.html` listing all runs (date, outcome, rounds, cost, version, link to each round's sheet).
- The sheet file name carries the full version (`harbour-district-v0.2.0-r003.2.html`); today `tools/plan/cli.ts` uses major.minor only, so two runs overwrite each other. Fix that.
- Promotion: an accepted run's final plan becomes `contracts/world/harbour-district.plan.json` at `0.2.0` only with Sky's OK, with a `history` entry (version, run id, date, accepted by Keel, approved by Sky).

## B · Aster · the run and the evidence
- One real run; if no critic correction happens naturally, one **seeded** challenge (e.g. a station moved onto a plot), labelled as seeded everywhere: Keel catches it, the owner repairs it, Keel rechecks.
- `courses/a04-content-pipeline/`: README (what it generates, why for this game, how to run), `report.html` (query, retrieved chunks, output side by side; each critic verdict), self-assessment, evidence: the submitted run (by id) from `runs/levels/`, its transcripts copied from `out/`, and the CAD sheets before (v0.1.0) and after (the accepted version), linked from the README.
- Retrieval tweak to record: the transit query "tram stop spacing sky rail stations every area in reach of a stop interchange" missed `standards › tram` (where `stopSpacing` lives) in the top 8; the plan's notes outranked it. Fix it in the objective's queries and show the ranks before and after.
- The accepted plan revision goes to Sky before it replaces `contracts/world/harbour-district.plan.json`.
