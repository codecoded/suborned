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
| 2026-10-21 | latch | range fixes, click-through proof, Sky picks (double click, verb pill, leash camera), bun run game proof (0.2.36–0.2.39) | 423k | 159 | 59 | yes: unity-batch-proof-runs (updated) | none reported | scope grew mid-batch (Sky picks) → 4 builds; game:check passes; hudClick proof added |
| 2026-10-21 | latch | port web controls (Click walks, hold acts) to game and range, builds 0.2.40–0.2.43; two host restarts mid-task | 630k | 65+ | 48+ | yes: batch-proof pre-build checks; new gotchas added (mid-hold picture releases button; unfocused player drops portrait press) | none reported | resumed twice via progress file; game:check passes |
| 2026-10-21 | keel | car "down in" preview (counts to disabled), confirm walk in act times | 117k | 52 | 7 | partial (short round, breath kept nothing new) | none | 1307/1307; hashes unchanged |
| 2026-10-21 | glyph | born from Indigo; seeding reflection from Aster's evidence pack (no project work) | 44k | 12 | 1 | n/a | n/a | 2 skills (controls-playground-iteration, play-feedback-to-preset), 5 policies operational (commit-on-release held back: still on trial); corrected 3 of Aster's claims as unsupported by files |
| 2026-10-21 | glyph | playground: Syndicate fights toggle, weapons free (G), quiet fight pill, cosmetic return fire, squad-act leaves selection, hold latch on moving targets | 170k | 34 | 42 | (wake run; skills/policies in force) | none reported | three asks folded into one round; published |
| 2026-10-22 | keel | weapons free (§14) + per-agent hostile intent (Pulse, Sky's answers), engine 0.19.0 | 551k | 232 | 73 | yes: engine-change-proof, adversarial matrix (added the masking finding) | none reported | 1365 tests; found rivals' Vale press masks weapons-free cheap wins and fails all loud wins |
| 2026-10-22 | keel | engine 0.25.0: FireWhyOf + ai.why, ClosingHash() from live state, one-route Listen gate | 286k | 147 | 62 | yes: engine-change-proof (baseline copy, switch the line off to prove what moves); added the fire.* lesson | none reported | 2022/2022; c031 re-recorded; killed Unity via `pkill -f cat` (PID-only rule added) |
| 2026-10-22 | glyph | HUD style lab (brief 046): kit sprites over game shots, 6 presets, 7 hold styles, roster slots, why cue, tokens export | 341k | 111 | 48 | yes: controls-playground lesson shaped the structure (presets, headless shots, export) | none reported | 0 fails; hud-style:check ALL OK |
| 2026-10-22 | glyph | lab round 2: roster rail without numbers, 4 subtle under-fire cues, ground occlusion | 260k | 91 | 24 | shaped structure, no decision changed | none reported | 412 PASS |
| 2026-10-22 | latch | build 0.2.65: Revised A overlays from lab tokens, ground marks behind legs, third person keeps orders, late-lag fixes (ClosingHash, async end checks, gates per tick, render cap), -late hook, FireWhyOf data | 575k | ~200 (307 by harness) | ~180 | yes: batch-proof (engine file times found a moving tree), mid-hold pictures | none reported | one batch, 5 fix passes; 2 failures from other crews (Keel's late engine copy, pkill) |
| 2026-10-22 | keel | squad fire: why agents skip (host drawn-only filter; focus never walks in; 16 m end; Scout down), engine 0.26.0 FireQuery | ~200k | ~55 (94 by harness) | 29 | yes: engine-change-proof (baseline first made the identical-hash proof one diff, predicted re-records); added: re-record drive session on version bump, replay Sky's saved sessions when triaging | none reported | 2052/2052; 312/312 + 86/86 hashes identical |
| 2026-10-22 | latch | Glyph's 12 review fixes, Loop names, no key legends, item 13 squad fire (FireQuery), LateRun asks; builds 0.2.66–0.2.73 | ~499k | ~190 (244 by harness) | 126 | yes: picture-resize releases a held button (why charge/ready frames matched; 2x run uses a fixed window) | none reported | 7 build numbers spent (engine landing mid-batch, load 76, own proof bugs, Glint compile); stopped on Sky's code-first rule |

## Without Anima (2026-10-22, by accident)
After a context reset Aster spawned the crew from their cards without the wake/breathe steps. These rounds are a comparison set, not pilot rounds.

| date | agent | task | tokens | tools | minutes | notes |
|---|---|---|---|---|---|---|
| 2026-10-22 | keel | engine 0.20.0: final fight per agent (§15.2), 5 scenarios | 233k | 146 | 70 | machine loaded by other runs; 1509 tests |
| 2026-10-22 | keel | stale Ctrl text | 54k | 21 | 6 | |
| 2026-10-22 | keel | engine 0.21.0: fire order ends out of range / on a new act | 237k | 111 | 20 | |
| 2026-10-22 | keel | engine 0.22.0: only security notices a run (+ crowd guards, resumed) | 204k + 234k | 111 + 131 | 18 + 30 | |
| 2026-10-22 | keel | fire-at-patrol bug: not the engine, FL-KERB | 180k | 87 | 22 | |
| 2026-10-22 | keel | engine 0.23.0: people acts walk in | 259k | 109 | 31 | |
| 2026-10-22 | keel | engine 0.24.0: Windows(), patrol.returns | 156k | 72 | 27 | |
| 2026-10-22 | latch | build 0.2.48: commit-on-release fire, Syndicate fights, squad-leave, hold latch, under-fire, engine 0.20 | 423k | 144 | 64 | one batch + 4 fix passes |
| 2026-10-22 | latch | build 0.2.56: next-batch items 1–12 + done moment | 485k | 173 | 94 | ported playground styling (Sky: "not Suborned style") |
| 2026-10-22 | glyph | playground fire on move | 68k | 19 | 11 | |
| 2026-10-22 | glyph | done moment (brief 043), playground | 325k | 93 | 32 | |

## Back on (Sky, 2026-10-22: "switch it on")
The protocol above applies again to every Keel, Latch and Glyph round from 2026-10-22 (Latch's overlay-style build woken mid-task). Log rows go in the pilot table; compare against the "Without Anima" set; decide after about four rounds each.
