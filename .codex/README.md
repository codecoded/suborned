# Codex crew

The same 17 people as the Claude crew, generated from the latest agent GDD by
`bun run crew`. Claude receives Markdown cards; Codex receives standalone TOML
agents. Each contains the same role responsibilities, holds, §01 core, source
section index and roster. Edit the GDD through the normal design process, then
regenerate both. Never edit a generated card.

## Start a team

Open a fresh Codex session in this repository (project configuration must be
trusted). For example:

```sh
codex "Act as Aster. Read .codex/agents/lead-producer.toml and follow its developer_instructions. Coordinate the Codex crew to inspect the next headless simulation gap against the latest GDD. Delegate source inspection to Keel and independent review to Redline. Report evidence; no Unity, assets or production edits."
```

Replace the task with the work you want done. Aster coordinates in the main
session; use the other job names as subagent types. The project allows three
concurrent specialists, in addition to the coordinator. Choose the model and
reasoning effort in your Codex client; all roles inherit them. The GDD's model
preferences remain documented, without forcing Claude models into Codex.

In T3, start a fresh Codex session in this same worktree. If its native subagent
tool does not expose these job names, the coordinator reads the matching TOML
file and passes its complete `developer_instructions` to a delegated child task
using the current Codex provider/model from T3's live catalog. It retains the
task ID and waits for the result. Creating files does not hot-reload an already
running thread's tool schema.

## Ownership and review

Aster assigns one objective, allowed paths, done-check and evidence per owner.
Independent work can run in parallel; only one writer owns a file at a time.
Keel owns simulation/rules, Splice owns tooling, and Redline/Suture independently
review. Reviewers write reports and return production fixes to the owner.
All 17 roles are present, including the art/Unity roles for future game work;
the current Assignment 5 scope stays headless code only.

The repo rules in `CLAUDE.md` apply to both crews. Parent session permissions,
MCP servers and tools are inherited: Claude's tool allowlists are not copied as
Codex security controls. No global Codex settings, credentials or MCP servers
are installed. Interactive crew configuration and the recorded pipeline are
separate: `goals run --provider codex --model <id>` selects the Codex adapter
explicitly. Other RAG pipelines keep their existing Claude adapter.

## Recorded goal workflow

The goal coordinator uses the same generated Codex cards for discovery,
inspection, test authoring, production writing and independent review. Calls
run sequentially with no repository tools; the coordinator supplies retrieved
GDD passages and inspected source, applies bounded structured patches, runs
tests and rescans after every write. This preserves one production writer and
separate review evidence.

```sh
bun run goals setup
SUBORNED_LIVE=1 bun run goals run \
  --provider codex --model gpt-6-astra \
  --approved-by 'Your name: approved this run' \
  --goal 'Describe the bounded outcome to discover and implement' \
  --max-tokens 500000 --max-calls 20 --max-iterations 3 \
  --max-features 1 --max-inspections 4
```

Use the exact same goal text to continue its backlog. Omit `--goal` for broad
headless-simulation discovery. `goals status --goal '<same text>'` checks local
evidence without a model; `goals run` also reruns retained acceptance tests
before skipping completed work. A fresh clone rebuilds its index from the
tracked GDD/contracts; an existing matching index is reused.

The adapter uses the existing ChatGPT login and pins the supported CLI version
(currently 0.163.0). Calls run in an isolated temporary directory with read-only
sandboxing and execution tools disabled. Transcripts, prompts, schemas, usage,
reviews and patches are retained under `runs/goals/`. Dollar cost is unavailable
for subscription calls, so `--max-usd` is rejected for Codex. The token threshold
is checked between calls (one in-flight call may cross it); call count and the
five-minute per-call timeout are hard bounds. Requested model identity is
recorded from the launcher, not presented as a server attestation.

## Check locally

```sh
bun run crew --check
bun test tools/crew
bun run typecheck
```

`--check` detects missing or stale cards without writing. Regeneration leaves
unrelated custom agent files alone. Tests parse every TOML card and compare its
role/core content with the Claude equivalent, and check the coordinator and
review boundaries. These checks do not call a model.

Configuration follows the [official Codex subagent documentation](https://learn.chatgpt.com/docs/agent-configuration/subagents)
and [configuration reference](https://learn.chatgpt.com/docs/config-file/config-reference).
Offline validation with the locally installed Codex CLI 0.163.0: its app-server
`config/read` accepted the project configuration and returned `enabled: true`
and `max_concurrent_threads_per_session: 3`. All 17 TOML cards parsed and matched
their Claude role/core content; 38 crew/RAG tests and TypeScript checking passed.
Those setup checks validate configuration and content without calling a model.

The first actual Codex crew scan is recorded in
[`runs/crew/2026-10-19T10-24-00Z-codex-discovery/run.json`](../runs/crew/2026-10-19T10-24-00Z-codex-discovery/run.json).
Keel and Splice inspected current code; Redline independently tested their
findings. Aster selected structured case/area-watch debrief export. The review
rejected a proposed second-takedown fix because real player orders already
cancel the report. Three Codex tasks completed; 61 scenario checks passed;
no production code changed in that scan. The selected export was subsequently
generated by the recorded goal workflow in
[`runs/goals/2026-10-19T11-05-43-376Z-discovery/run.json`](../runs/goals/2026-10-19T11-05-43-376Z-discovery/run.json):
five new acceptance tests and 866 existing regressions pass after one production
round. An unchanged rerun in `runs/goals/2026-10-19T11-14-05-156Z-discovery/`
passes with zero model calls and zero writes. See the
[Assignment 5 archived feature report](../courses/a05-goal-agent/recorded-run.html) for the exact scope,
earlier incomplete attempts and offline evidence replay.
Reproduce the independent review from the repository root with:

```sh
dotnet run --project runs/crew/2026-10-19T10-24-00Z-codex-discovery/review-probe/Probe.csproj
```
