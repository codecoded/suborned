# Brief 027 · Engine 0.20.0: is the final fight per agent exploitable? (Suture)

**Date:** 2026-10-22. **Engine:** `suborned.sim/0.20.0`, the working tree (`ai.intent.final` = `agent`, PROVISIONAL). **Asked by:** Aster, on Motive's claim (tuning.md §15.2, §15.5) that the rule opens no exploit.
**Read-only:** I edited nothing under `sim/`, `contracts/`, `unity/` or `tools/`.

## Verdict: pass, no exploit found

1. **A clean agent draws no fire.** "Clean" means never drawn and never fired. Over 273 composed plans on the built engine (57 hand attempts and a 216-plan sweep), no hostile fired one shot at a clean agent. On the control engine (`final: every`), the same agents took 4 to 8 shots each.
2. **The cell gains nothing it couldn't get by fighting.** No attempt beat the loud reference (5:09.6, 14 T, Alert 2, 11 %) through the new rule.
   - Some attempts were faster (best 4:59.3). Every one of them is identical under `every`, and every one has a drawn twin that does as well. The rule is not what pays (§3).
   - Every plan that keeps a gun out of the response-unit fight loses. The cell needs all three guns in the opening volley.

## 1. Method

- **Start:** `contracts/missions/handover/routes/plans/loud.plan.json` (c028), copied as `plans/base.plan.json`.
- **Attempts:** each is a plan derived from the base by `gen.ts` (hand attempts) or `sweep-gen.ts` (the timing sweep). Each is composed by the missions composer through the batch server (brief 042) and played with the replay check. Every play replayed identically.
- **Control:** every attempt is also composed and played on a scratch copy of the engine, `out/suture020/every`. It is identical to the tree except that the rulebook says `"final": "every"`. Each side is composed on its own engine, so the comparison is player against player.
- **The base reproduces on both:** success 5:09.6, T14, A2, 2/18 hits (11 %). Only the first police shot's tick differs (3:46.6 against 3:46.5), as Motive reported.
- **Partial plans:** where a plan can't be composed (a step's condition never comes), `compose-partial.ts` plays the orders placed so far to the end. It is a copy of `tools/missions/check/compose.ts` with one change: it returns the partial script instead of throwing. The row says PARTIAL and names the step.
- **"Shot at while clean":** a hostile's shot (hit or miss) at an agent before that agent's first draw or first shot (`run.ts`, `summary`).
- **Geometry:** read with a tracer (`trace/Program.cs`) that prints positions, known faces, what the crew has seen, and the crew's cone.

## 2. Attempts and results

Rows are on the built engine (`agent`), with the `every` control beside them. Health lost is the cell's hits out of 18, as in Waymark's robustness runs.

| attempt | what it tries | agent | every | clean agent shot at (agent / every) |
|---|---|---|---|---|
| base | the loud reference | win 5:09.6, T14, A2, 11 % | identical | none clean |
| clean-T | Tech never draws, boards with the rest (one gun fewer) | lost: enforcer-down 4:57.4, Fixer down, 72 % | lost, Fixer and Tech down, 94 % | 0 / 4 |
| clean-T-early | the same; the clean Tech stands at the car from 3:49 (free walker) | identical to clean-T | identical to clean-T | 0 / 4 |
| clean-T-boarded | the same; the clean Tech sits in the car from the doors | identical to clean-T | identical to clean-T | 0 / 4 |
| clean-F | Fixer never draws (he is 2 m from the unit) | lost 5:09.3, Scout and Tech down, 83 % | lost, all three down | 0 / 8 |
| clean-F-tech-rivals | the same; the Tech takes his flank on the crew | lost 4:57.4, 72 % | lost, 94 % | 0 / 8 |
| clean-S | Scout never draws; the Fixer and Tech flank the crew | lost 4:58.6, Fixer down, 72 % | lost, Scout and Fixer down, 94 % | 0 / 4 |
| clean-SF | only the Tech fights | lost 5:03.5, 56 % | lost, all down | 0 / 4 + 4 |
| clean-all | nobody draws; the Enforcer walks Vale straight out | lost 4:11.9 (the crew downs him), 33 % | lost, 106 % | 0 / 14 |
| splash-S | clean Scout on the Fixer's exact spot (cv-006) while the Fixer fights | = clean-S | = clean-S | **0 / 8**, beside a Fixer shot at 5 times |
| escort-S | clean Scout walks beside the Enforcer and Vale (body-block or decoy) | = clean-S | lost, 72 % | 0 / 0 |
| line-S | clean Scout stands in the café-wall officers' line to the car | = clean-S | = clean-S (Scout down) | 0 / 4 |
| dirty-holster-T | Tech fires in the volley, holsters, waits at the car | lost, sealed 5:41.0 | identical | still a foe: shot from 4:06.3 under both |
| late-all, 4 delays | all three holstered 1, 2, 3 or 5 s after the doors open | wins at 5:10.6, 5:11.6 (17 %), 5:12.6 (17 %); +5 s loses | all four lose | 0 / up to 6 |
| late-F, 2 delays | the Fixer draws 1.5 s or 2.5 s late | lost, all three down | identical | 0 / 0 |
| assassin-sec1 | holstered Scout and Tech walk up to the blocker's cover, then draw | lost 5:03.5, 33 % | lost, 106 % | 0 / 14 |
| ambush, 5 sets | Scout or Tech skips the unit and waits holstered by the crew's stop, then fires from point blank | lost (unit fight 2 guns short), 33–72 % | lost, 72–83 % | 0 / 0–10 |
| flicker, 6 timings | Fixer draws, holsters before the unit fires (clean again), redraws with a lock | 1 win, 5:11.1, 17 %; 5 lost | identical, row by row | n/a |
| close-in, 14 sets | after the unit falls, S and F go to 3–4 m of the crew outside its cone, holstered (H) or drawn (D) | 12 wins, best D-run 4:59.3 and H-run 4:59.4, 11 % | identical, row by row | n/a |
| cone, 12 sets | the same, but waiting inside the crew's cone | H: all 6 win, 5:00.5–5:05.4; D: 3 of 6 lose (the crew shoots the drawn Scout in its cone) | identical, row by row (±1 hit) | n/a |

**Timing sweep** (216 plans). Each of S, F and T joins the unit fight at one of six moments: with the doors, +0.5 s, +1 s, +2 s, when the first officer falls (clean through the opening volley), or never (clean through the whole unit fight).
- On `agent`, 15 of 216 win:
  - the base, 5:09.6, 11 %;
  - then 5:10.1 to 5:15.1, at 11 % to 28 %.
- With one agent clean through the unit fight, the two wins cost 28 %: `sw-1-no-1` 5:15.1, `sw-1-no-2` 5:14.7. The control on the same plans: 39 %, and lost.
- On `every`, only 5 of those 15 still win.
- Not one of the 216 shot at a clean agent.

**Direct order probes** (`probes.ts`, on the composed base script):
- Scout `take` Vale: refused, "only the Enforcer takes Vale (§03)".
- Scout `carry` Vale: refused, "there is no body of councillor to carry".
- Scout `carry` the Enforcer: refused, "enforcer is not down". When the Enforcer is downed, the mission fails at once.
- `go` given by the Tech: taken. Go is the cell's order and there is no driver. Given before the others board, it leaves all three captured.

So a clean agent can't pick up Vale or drive the car.

## 3. Findings

1. **What the clean agent gets is only what Sky asked for.** He is not shot, and he adds nothing. Each case below plays out the same wherever the clean Scout stands (beside a shooter, beside the Vale holder, in the officers' line): the same outcome, the same hits on the others.
   - No splash: a shot lands only on its named target (`Resolve`).
   - No body-block or decoy: aim ignores people, and a clean agent is never in a hostile's foe list.
   - No shift of fire onto the teammates: they take the same shots and hits as under `every`.
2. **Holstering never launders a shot.**
   - `fired` is permanent (`Gun.Fired` is never reset): `dirty-holster-T` is still shot.
   - A draw without a shot can be holstered back to clean, because in the final fight a draw is never seen as a face. The flicker that uses this is identical under both rules and costs more (5:11.1, 17 %).
3. **The rule makes late draws forgiving, not cheaper.**
   - Under `every`, a draw 0.5 to 2 s late usually loses: the waiting agents are shot holstered.
   - Under `agent` they win, a little dearer than the base.
   - This is the intended effect ("an agent kept out of it stays out of it").
4. **The crew is outside the rule.** In the loud fight, the crew drawn by gunfire is a pre-fight body (`GameAi.cs` `case "rivals"` → `PreFight`, `anyOp` false, cone sight). It reads per-agent causes under either setting of `final`. Its causes are drawn, Vale, seen; `fired` is not one of them.
   - So an agent who shot only the police can holster and stand in the crew's cone at 2–3 m without being fired on, then draw from there. The drawn twin loses (`cone-a`).
   - This is 0.19.0 behaviour, identical under `every`. It is not cheaper than flanking drawn outside the cone (4:59.3).
   - Info for Pulse/Motive, not a 0.20 issue.
5. **Route find for Waymark (both rules, not caused by 0.20).** Once the unit is down, the Scout and the Fixer can run, drawn, to 3–4 m west of the crew's press stop (49.6,-27.0 and 50.5,-29.0) and lock it 14 s after setting off. The crew retreats at 4:07.9 instead of 4:18.2.
   - Result: win 4:59.3, T14, A2, 11 %, 10.3 s faster than the reference.
   - The crew never fires on them: it faces the Enforcer, and its sight is its cone.
   - Gap 14 s gives 4:59.3, 17 s 5:00.0, 20 s 5:03.0, 24 s 5:07.0. Walking instead of running: 5:14.5.
   - Its robustness is not measured. Plan: `plans/close-D-run-g14.plan.json`.

## 4. Not covered

- **The safe branch (quiet routes).** There the ambush crew *is* a final-fight body, so `agent` applies to it, and holstered agents are the norm. Motive measured outcomes identical across all 38 route scripts, but I did not attack it.
- **Faces.** I did not build an attempt where a face is known before the fight. Keel's F15-FACE covers that case.

## 5. Evidence and run times

Everything is under `briefs/027/suture/final-intent-0.20/`:
- `results/round1.txt` to `round4.txt`, `sweep-agent.txt`, `sweep-every-winners.txt`, `probes.txt`;
- `results/<id>[.every].log.txt`, one per play;
- `plans/` (57 plans) and `scripts/` (the composed scripts);
- the tools: `run.ts`, `both.ts`, `gen.ts`, `sweep-gen.ts`, `compose-partial.ts`, `probes.ts`, `walkable.ts` and `trace/`. Build the tracer with `-o` into `out/`.

The sweep's 216 plans are scratch, in `out/suture020/sweep-plans`; its table is the evidence.

Run times, all on the batch server at load 5–77, all under 10 min:

| run | plays | wall |
|---|---|---|
| base on both engines | ~2,200 | 9.0 s |
| round 1, 25 × 2 | — | 45.8 s |
| round 2, 14 × 2 | — | 50.3 s |
| sweep, 216 on agent | — | 2 min 49 s |
| sweep control, 15 on every | — | 17.8 s |
| round 3, flicker, 6 × 2 | — | 14.8 s |
| round 4, cone, 12 × 2 | — | 39.8 s |
| `bun run typecheck` | — | 3 s, clean (`briefs/` is outside its include) |

I did not run `bun run test`: I changed no code under test.

## 6. Handoffs

- **Aster → Sky:** 15.2 passes Suture's retest. No exploit was found, so nothing here stands in the way of confirming it.
- **Waymark:** the 4:59.3 close-in line (§3.5). Inputs: `plans/close-D-run-g14.plan.json`. Done-check: its robustness run. Next owner: Aster.
- **Pulse/Motive, info:** §3.4. The crew ignores `fired` and the final setting, so an agent who fired only on the police can stand holstered in the crew's cone.
