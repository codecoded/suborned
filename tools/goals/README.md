# Goal-oriented simulation development

The coordinator compares the latest matched agent/human GDD with the headless simulation, discovers requirements, ranks gaps, generates independently reviewed tests and code, then verifies and rescans after each write. It uses the existing SurrealDB retrieval pipeline and the Claude or Codex crew cards. Models return structured proposals; the coordinator owns bounded file writes and test commands.

Main includes the reusable workflow and generated simulation features. It starts with **no imported backlog or historical goal runs**. The [Assignment 5 submission](https://github.com/codecoded/suborned/tree/527ab5942d3a300bd5509debbb2d649f37aab5ee/courses/a05-goal-agent) remains on its frozen commit with its reports, transcripts and assignment-specific verification command.

## Check the tooling without a model or database

From the repository root, with Bun 1.4+ and the .NET 10 SDK:

```sh
bun install --frozen-lockfile
bun run typecheck
bun run crew --check
GOALS_DOTNET_TESTS=1 bun test tools/goals/test tools/rag/rag.test.ts tools/crew
dotnet test sim/Suborned.Sim.Tests/Suborned.Sim.Tests.csproj
```

The .NET opt-in enables the real retained-proof replay fixture; other model and retrieval interactions in these tests are simulated. No live model calls, Docker, Unity or database credentials are needed. Initial dependency restoration may need network access. `bun run goals:test` runs the goal tooling tests alone, with the .NET fixture skipped unless that environment flag is set.

```sh
bun run goals status
```

On a clean main checkout this reports `NO_BACKLOG`. After a run, `status --goal '<same exact goal>'` checks saved source/GDD hashes offline. Status inspection does not execute acceptance tests; a live run rechecks them before skipping completed work.

## Prepare retrieval for a live run

Use Docker for the default local services, or configure existing services through [`.env.example`](../../.env.example). A fresh checkout builds the database from the tracked GDD/contracts using local embeddings; it needs no copied database dump.

```sh
bun run goals setup
```

Setup ingests a corpus-hash-specific SurrealDB database, or reuses a complete matching index. It may start the default local stack when no stack is already running. It refuses to reconfigure an existing stack automatically. The highest numeric GDD version is selected, the agent and human versions must agree, and discovery compares their hashes against registered local main checkouts.

## Run an approved, bounded goal

Codex uses your authenticated ChatGPT CLI session. The adapter supports **Codex CLI 0.163.0** and rejects unsupported versions or unexpected tool/runtime events. Replace the model, approver and goal below. Do not opt in until you have approved the run: it uses the model subscription and can edit simulation code.

```sh
SUBORNED_LIVE=1 bun run goals run \
  --provider codex --model '<available-model-id>' \
  --approved-by '<your-name>' \
  --goal '<specific headless simulation outcome>' \
  --max-tokens 500000 --max-calls 20 --max-iterations 3 \
  --max-features 1 --max-inspections 8
```

For the existing Claude adapter, use `--provider claude --max-usd 10` instead of the Codex provider/model/token options; Claude Code must be signed in. Both require `SUBORNED_LIVE=1` and a recorded approver. Codex dollar cost is unavailable and `--max-usd` is rejected. Token thresholds are checked between calls: the last in-flight call can cross the threshold. Call-count, iteration and five-minute per-call limits bound execution.

Use the exact same goal text to continue its backlog. Omit `--goal` to discover a broader headless-simulation backlog; a bounded run may leave requirements unfinished. `goals scan` accepts the same live flags and performs discovery/inspection without production code writes. It still uses model calls and writes evidence/backlog state.

## Read this run's result

The terminal JSON identifies the run, status, stop reason, `report` and `evidence` paths:

```text
runs/goals/<your-run-id>/index.html
runs/goals/<your-run-id>/run.json
```

The HTML report shows this run's ID, start/finish timestamps, scope, priorities, statuses and next actions. Early recorded failures may have `report: null`; preflight failures may print only an error. Do not infer success from an older report.

| Exit | Status | Meaning |
| --- | --- | --- |
| 0 | COMPLETE | Every discovered requirement in the declared scope has current supporting evidence. |
| 2 | INCOMPLETE | Requirements remain unresolved, including when a budget or inspection limit stops progress. |
| 1 | ERROR | Execution could not complete because of a runtime/setup error. |

A repeat run keeps verified work only while its GDD/code hashes match and its acceptance tests still pass. Changed sources invalidate stale proof. A new goal gets separate state. Completion concerns the discovered requirements in the declared scope, not a guarantee that the whole game is finished. Live generation can fail tests or review and is not guaranteed to produce identical code or finish within a particular budget.

## Replay a run created in this checkout

```sh
bun run goals verify runs/goals/<your-run-id>
```

Replay uses archived source, model transcripts and exact patches without calling a model or database. For generated features it reconstructs the assertion-failing baseline and the repaired implementation, checking acceptance tests, regressions, reviews and provenance. An unchanged rerun can depend on its original generation and catalog runs; keep those directories together. Inspection-only older evidence is validated against its archived excerpts/checks, without claiming a full isolated source replay.

Evidence and goal-keyed backlogs are written under `runs/goals/`. They are not imported from Assignment 5 and are not automatically committed. Keep relevant source snapshots, transcripts and test reports when retaining a run; compiler caches are ignored. Never delete earlier runs merely to force rediscovery of a completed feature. A new approved goal is the way to ask for further work.

## Source and boundaries

- `campaign.ts`, `backlog.ts`, `discovery-sources.ts`: current GDD/code inspection, priority scoring, dependency gates and retained-proof invalidation.
- `feature.ts`, `patch.ts`, `checks.ts`: separate test author/reviewer/writer calls, frozen acceptance tests, bounded production patches and checks after each write.
- `models.ts`, `codex.ts`, `../rag/agent.ts`: provider adapters, opt-in and budget guards, isolated structured generation and retained call evidence.
- `verify-campaign.ts`: offline provenance validation and generated-feature replay.

The older fixed-scope workflow remains available through an explicit `--scope` file for compatible legacy evidence; normal development uses goal discovery. The coordinator is limited to headless code. Missing game rules or conflicting design sources require resolution through the GDD process, not invented values. Interactive [Codex crew delegation](../../.codex/README.md) is separate from the sequential structured calls made by this CLI.
