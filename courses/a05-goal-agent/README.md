# Assignment 5 — goal-oriented coding agent

The agent compares the GDD with code, ranks gaps, generates tests and code through independent crew roles, then verifies and rescans after every write. The submitted Codex run completed one real headless simulation feature: exporting existing cases and area-watch consequences in JSON debriefs. This completes its explicit goal, not the whole game.

## For markers — human or agent

Start here or at the [submission guide](index.html). The [archived example report](recorded-run.html) describes the fixed 19 October 2026 generation and rerun; it does not update when you run commands. Reviewers do not need to start the crew, authorize AI calls, create a database, or open Unity. Agent reviewers should follow the same commands below and report the final exit status and result file; do not run live generation just to mark the submission.

From the repository root, with **Bun 1.4+ and the .NET 10 SDK** installed:

```sh
bun install --frozen-lockfile
bun run goals:verify
```

Allow **10 minutes** for the complete command on a busy machine. Package installation and the first NuGet restore need network access unless cached; verification itself makes no model or database calls. Tested with Bun 1.4.3 and .NET 10.0.401. No `.env`, model login, Docker, SurrealDB dump, Unity Editor or modelling assets are required.

The command checks TypeScript, the goal/RAG/crew test suite (including the real .NET verifier fixture), both sets of generated crew cards, the submitted simulation, and two archived campaigns. It prints a `PASS` line for each of six stages and finishes with:

```text
ASSIGNMENT 5 OFFLINE VERIFICATION PASSED
```

Success is exit **0**; a failed check is exit **1** with the failed stage and log location. **Open the HTML path printed by your command:** `out/goals/submission/<your-check-timestamp>/index.html`. It contains this execution's start/finish timestamps and six stage results, including FAIL or NOT RUN where appropriate. `verification.json` and logs sit beside it. These local outputs are ignored by Git. A missing SDK, unavailable package restore, missing evidence file or changed implementation can cause verification to fail; the final status will not claim success.

The [fresh-copy validation record](validation-fresh-copy.json) documents a successful run from an export of all files eligible for check-in, without `.git`, `.env`, `node_modules`, `out/` or compiler outputs. All six stages passed in 258 seconds. Host package caches were available; this was not a newly provisioned operating system.

After the report presentation changes, the [follow-up verification](validation-report-generation.json) passed all six stages again and checked the generated HTML on desktop and mobile. Its [dated report](validation/report-generation/index.html) is retained as another labelled execution record.

Expected evidence:

| Check | Expected result |
| --- | --- |
| Submitted simulation source | 871 tests pass: 5 generated acceptance tests plus 866 regressions |
| Archived pre-feature source | 5 acceptance assertions fail; all 866 regressions pass |
| Exact generated patch applied to that source | 5 acceptance tests and 866 regressions pass |
| Archived unchanged rerun | Retained proof validates and 5 acceptance tests pass again; recorded run made 0 model calls and 0 production writes |

**The five baseline failures are intentional evidence of the original gap.** The overall verifier passes only when the exact patch fixes them and its provenance, review, rescan and regression evidence also validate. It reconstructs that baseline in an isolated directory; it does not revert the submitted game or edit archived runs. Offline replay validates retained model transcripts; it does not call the model again.

For individual replays:

```sh
bun run goals verify runs/goals/2026-10-19T11-05-43-376Z-discovery
bun run goals verify runs/goals/2026-10-19T11-14-05-156Z-discovery
```

The broad game `bun run check` is not the Assignment 5 entry point. Existing unrelated failures reproduced on the starting commit are documented in [the baseline comparison](validation/baseline-comparison.json).

## What was generated, and why?

The simulation already stored cases and area-watch consequences but its JSON debrief omitted both collections. The crew selected this bounded, testable gap from the latest GDD available for the run: **v0.24.0, 18 October 2026**. Its priority score was **12**; the [selection evidence](../../runs/goals/2026-10-19T11-05-43-376Z-discovery/features/debrief-export-cases-area-watch/selection.json) records the scope and rationale.

- [Generated production change](../../sim/Suborned.Sim/Debrief.cs): adds `nextMission.cases` and `nextMission.areaWatch`, preserving existing tuple values and order. No new gameplay rule or campaign persistence.
- [Generated acceptance tests](../../sim/Suborned.Sim.Tests/GoalDebriefCaseWatchFidelityTests.cs): five tests cover fidelity, empty collections, existing fields and read/replay behaviour.
- [Agent source](../../tools/goals/): discovery, deterministic priority/dependency scoring, bounded independent test author/reviewer/writer roles, patch application, checks, rescan and persistent proof invalidation.
- [Technical report](technical-report.html) and [structured outcome](latest-run-summary.json): source map, exact scope, calls, budgets and limitations.

The completed run took one production-writing round and nine Codex calls. There were **no manual edits to the generated production patch or its frozen acceptance tests**. Coordinator, adapters, verifier and report work were implementation work outside the generated feature. Earlier incomplete attempts remain labelled as such; they are not counted as successful generation.

## What happens on another live run?

`goals:verify` is the repeatable review path. `goals run` is a new, authorized AI execution with different requirements and possible outcomes:

| Situation | Behaviour |
| --- | --- |
| Same exact goal, same GDD/code and retained backlog | Revalidate saved proof and rerun acceptance checks; skip already verified work. The recorded rerun did this with zero calls/writes. |
| Relevant code or GDD changes | Invalidate stale proof, inspect again and potentially generate a repair. |
| A new goal, or no `--goal` | Discover and rank a separate backlog. Omitting the goal explores the broader headless GDD; it does not guarantee the whole game will finish within one budget. |
| Missing auth, unsupported model/CLI, service outage, failed tests/review or exhausted budget | Stop with `ERROR` or `INCOMPLETE`, retaining evidence and unresolved work. A live run is not guaranteed to finish or reproduce identical code. |

Live CLI exits are **0 COMPLETE**, **2 INCOMPLETE**, **1 ERROR**. Completion always means the discovered requirements in that run's declared scope are verified.

For a new live execution, open the `report` path printed in its terminal JSON: `runs/goals/<your-run-id>/index.html`. The page shows that run's ID, timestamps, scope and outcome. Its `evidence` field points to `run.json`. A recorded campaign that stops before discovery has `report: null`. Earlier preflight failures may print only a terminal error; inspect any emitted evidence path rather than assuming an HTML report exists. The submission guide and archived example do not change into your new result.

### Optional live setup — not needed for marking

Use your own authenticated Codex CLI **0.163.0** and a model available to your account. This adapter pins that CLI version and rejects unsupported event/tool behaviour; a later CLI may require an adapter update. Docker is needed for the default local retrieval services, or configure existing services using [`.env.example`](../../.env.example).

```sh
bun run goals setup
```

Setup builds the SurrealDB corpus from the repository's GDD and contracts using local embeddings. It reuses an index only when its corpus identity matches. A fresh copy does not need somebody else's database or a checked-in database dump. The completed run reused an existing matching index; an empty-database live generation run is not claimed by the offline replay evidence.

After explicitly approving a new live run, replace both placeholders below. This command can write simulation code and use your model subscription:

```sh
SUBORNED_LIVE=1 bun run goals run \
  --provider codex --model '<available-model-id>' \
  --approved-by '<your-name>' \
  --max-tokens 500000 --max-calls 20 --max-iterations 3 \
  --max-features 1 --max-inspections 8 \
  --goal 'Export the existing case and area-watch collections in headless JSON debriefs. Preserve current consequence values, existing JSON fields, deterministic replay and side-effect-free reads. Do not add campaign persistence or change gameplay rules.'
```

Token limits are checked **between calls**; the last in-flight call can cross the threshold. Codex subscription dollar cost is unavailable, not zero. `goals scan` performs model-backed discovery without production code writes; `goals status --goal '<same exact text>'` reads saved status and checks hashes offline. The [Codex crew README](../../.codex/README.md) describes the matching 17 roles.

## What belongs in the check-in?

Keep the generated source and tests, `tools/goals/`, both crew card sets, the reports and their linked evidence. **Do not revert the successful feature or erase runs to make the next run find it again.** Baseline replay already demonstrates the missing feature safely.

The final proof depends on earlier evidence, so retain the chain:

1. [Accepted catalog origin](../../runs/goals/2026-10-19T11-01-02-466Z-discovery/) — this attempt stopped before generation, but supplied the accepted catalog.
2. [Completed generation](../../runs/goals/2026-10-19T11-05-43-376Z-discovery/) — source snapshot, patches, transcripts, reviews and test evidence.
3. [Unchanged rerun](../../runs/goals/2026-10-19T11-14-05-156Z-discovery/) — retained proof and rechecks.
4. `runs/goals/backlogs/` — goal-keyed state used to avoid rediscovering completed work.

The earlier crew scan, incomplete attempts and original Watch experiment are also linked by the report; keep them as labelled history. The verifier is the check for missing provenance. `.gitignore` excludes local credentials, dependencies, `out/`, and compiler `bin/`/`obj/` directories; retain source snapshots, logs and TRX test reports. Commit all intended new source and evidence files together, not only modifications to tracked files.
