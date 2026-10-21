# 036 · Anima pilot (Latch and Keel)

**State:** running from 2026-10-21. Owner: Aster. Sky: "let's pilot it then next and keep track if it helps. We spin up anima agents (using indigo to birth them) - if anima starts being a hindrance we drop it."

## Setup
- Agents: `latch` and `keel` in `~/.anima`, both born from Indigo (architect) on 2026-09-26, 0 breaths before the pilot.
- Roster: Aster's roster for this project records `latch` owns `unity`, `keel` owns `sim` (host claude).
- No repo dependency: nothing in `.claude/agents/` cards, the GDD or the pipelines calls Anima, so `runAgent` runs (A4 replays, future A6) are unaffected. The protocol lives only in how Aster spawns the two:
  - start: `anima read --wake --agent <name> --objective "<task>"`, use matching lessons, then work;
  - delivery: one `anima breathe --agent <name>` (the decision, evidence, surprise or correction, next check); not after every small step.
- If `anima` is missing, the agent carries on without it.

## Measures (per round)
| measure | baseline (2026-10-17 → 21, without Anima) |
|---|---|
| Latch tokens per round | 126k–959k, typically 450k–950k |
| Keel tokens per round | 114k–714k, typically 300k–650k |
| rediscovery | known mistakes found again (e.g. the fixed-frame-rate capture script, the A-pose/T-pose avatar arms, shot routines rewritten each round) |
| rounds to green | first-pass failures needing a second round |

For each pilot round, record: tokens, tool uses, minutes, whether wake surfaced a relevant lesson, any rediscovery, and the cost of the Anima steps themselves.

## Decision rule
Drop Anima if, over the next ~4 rounds each, the two agents are not cheaper or faster, or the wake/breathe steps cost more than they save, or they cause confusion (wrong lessons applied). Extend to the rest of the crew if they help.

## Log
| date | agent | task | tokens | tools | minutes | lesson used? | rediscovery? | notes |
|---|---|---|---|---|---|---|---|---|
| 2026-10-21 | latch | third-person input fixes, fire-stops-walk, conversation framing, single-pass capture, ring by default, hover marker; first `bun run batch` | 406k | 175 | 68 | no (first wake, no stored lessons) | none reported | batch needed 5 first-run fixes; game:check blocked by Keel's in-flight engine |
| 2026-10-21 | keel | Motive §8 fire-on-the-move and §9 anyone can be shot (engine 0.15.0) | 489k | 208 | 59 | no (first wake, no stored lessons; wake cost a few hundred tokens) | none reported | breath recorded 3 lessons (baseline copy before engine change; fight-lab --rulebook when probing; new rulebook keys optional in schema) |
| 2026-10-21 | latch, keel | seeding reflection from Aster's evidence packs (no project work) | 58k / 57k | 24 / 24 | 2 / 2 | n/a | n/a | Latch: 3 skills, 4 policies (promoted by Aster); Keel: 2 skills, 3 policies in force; each corrected some of Aster's claims against evidence |
| 2026-10-21 | keel | Motive §10: settle rule, F8-BREAK lane, cars, P-B Alert trace (engine 0.16.0) | 365k | 160 | 39 | yes: both skills (baseline before first edit; sweep caught newly cheap wins) | yes: `missions:check` defaults to HEAD; first sweep read dropped rows (skills updated) | 1219/1219 tests; 11/278 hashes moved; within baseline range, long sweep |
| 2026-10-21 | keel | Motive §11 trigger, locks, follow fire + §12.2 police answer every recent shooter (engine 0.17.0) | 310k | 132 | 39 | yes: both skills (baseline first, a proof per rule, new cheap wins reported apart from the net) | yes: range hashes need a fixed start text; a lock whose target dies blocks re-lock that tick | 1261/1261; §11 alone byte-identical; §12.2 moved 29/284 and broke 4 loud routes |
| 2026-10-21 | keel | Motive §13: pair count in today, hold never follows, lab draw return, same-tick re-lock (engine 0.18.0) | 226k | 108 | 25 | yes: both skills | none reported | 1267/1267; 59 runs moved, no outcome changed; found the re-lock case 0.17 had half-fixed |
| 2026-10-21 | latch | batch 0.2.32: action controls, visible firing, fire animation, follow/lock host, cars, brief 038 items | 639k | 225 | 102 | yes: policies + both skills used | yes: stale-pointer-after-resize trap in a new form (added to batch skill) | six builds (0.2.27→0.2.32), not one; game:check passes |
| 2026-10-21 | keel | fight range up to 0.18: car and bystander spawns, lane layout, plain range lines | 334k | 95 | 22 | yes: engine-change-proof (added to it) | none reported | 1294/1294; today 286/286 identical; 9 new range scenarios |
| 2026-10-21 | latch | fight range panel up to 0.18 (build 0.2.33): lane, cars, bystanders, lines, settings | 322k | 102 | 25 | yes: batch-runs (headless pre-play caught a bad wait, so one build) | none reported | one build, game:check passes |
