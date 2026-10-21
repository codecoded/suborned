# Brief 027 · The fight lab: engine, matrix and first numbers (Keel)

2026-10-20. Engine `suborned.sim/0.10.0`. Rulebook section `fight.lab` (EXPERIMENTAL). Spec: `briefs/027/playground.md` (Motive).

Nothing here is a design decision. The GDD (v0.24) is unchanged. Every lab value is `proposed: true`, and its reason starts "EXPERIMENTAL: brief 027 playground". No numbers were tuned to pass. Proposals for Motive are in §4.

## 1. What is built

### Switches
- **The rulebook.** `contracts/rules/rulebook.json` has a new `fight.lab` section and 50 `lab-*` values. The section's `on` is **false**.
  - The section holds §2's rulebook rows: `healthScale`; the five weapons (`damage`, `every`, `eff`, `max`, `arms`, `target`, plus `charge`, `near`/`nearDamage` and `noFireMoving`); `pips` and `chance` (the four steps); `draw` and `aimNew`; `stances`; `then`; `close`; `charge`; `field`; `target`; `policeWarning`; `hostilesFireMoving`; `loadout`.
  - The zod schema, the TS reference `labPips()` and the rulebook tests cover it.
- **The run's start.** A run's start turns the lab on with `Start.fightLab` (`{on, healthScale, pips, stances, then, close, charge, field, policeWarning, hostilesFireMoving, loadout}`). It works like `cell.drive`: every key it leaves out falls back to the rulebook's default.
  - With the lab off (the today preset), the start adds no bytes and nothing new is read or hashed.
  - Proof: `hashes --start-text BASE` before and after the change are identical for all 229 runs (189 scenarios and every route script). The files are `out/keel027/hashes-{before,after}.json`.
- **The presets.** `briefs/027/presets/today.json` (`fightLab.on: false`) and `lab.json` (§2's defaults). Each has a `start` part for the engine and a `host` part for Latch's PlayControls.

### Engine (`sim/Suborned.Sim/GameFightLab.cs`, plus small hooks in GameCombat, GameAi, GameOrders, Hash, Rules, Model, Grid)
- **Health and weapons.**
  - Every health is multiplied by `healthScale`.
  - The cell carries the loadout. Arms is checked: the carbine costs 0 and the cell has at most one. An unknown weapon is rejected.
  - Every other gun carries the lab sidearm.
  - Damage depends on distance (the riot gun's near band). The long rifle never fires while walking.
  - `draw` and `aimNew` come from the lab.
- **Pips** (`Rules.LabPips`). The band, then the edges, clamped to 1–4, then the step. Hits are counted, not rolled, with the same accumulator as today.
  - The **high** edge is wired, but `LevelOf` returns 0: the Handover site carries no levels yet. Datum or Splice need to add them.
  - `mod-vale` is not read when pips are on.
- **Stances `two`.** A draw sets the agent to at will. `return` is refused with `stance-unknown`. Hold ends any fire order. A fire order clears hold.
- **`fire.then`.** It is per order (an optional `then` key), and defaults to the rulebook value. With `free`, an agent whose target goes down returns to at will.
- **`fire.close`.** A fire order on a target out of range, or with no clear line, walks the agent into range:
  - It runs Dijkstra over the walk grid (`Grid.Nearest`, 60 m of walk, ties to the lowest cell) to the nearest free point inside the effective ring with a clear line.
  - The agent fires on the way at walking pips, and stops once it is inside the effective ring with a line.
  - The order neither lapses nor is refused for range. It is refused only if no such point exists.
  - A driven agent is never walked.
- **Charge** (`Op.Charge`).
  - Each charged weapon pays its charge per shot and waits while the bar is short.
  - The bar regenerates 1 a tick while the weapon rests, meaning no charged shot within the weapon's cadence. This is my reading of "while no charged weapon fires"; Motive should confirm it.
- **`volley {op, target, shots}`.**
  - It fires exactly `shots` shots, then the agent returns to its stance.
  - It pays `target × shots` when given. With charge off, it uses one of `volleysPerFight` instead. A fight is the final fight, or else the open public fight.
  - If unpaid, or if `shots` is outside 1–6, it is refused `no-charge`.
  - Unfired shots are refunded when the volley ends early.
  - The aim stays on the target after the volley, so at-will fire on the same target keeps its count.
- **`field {op}`** (switch on). It costs 50, absorbs 30 for 30 ticks, and the agent does not fire while it is up. With the switch off it is an unknown verb.
- **Police warning.** An officer, the guard, the patrol or a response unit who sees a drawn weapon before any shot gives 20 ticks:
  - **Holstered in time:** Alert 1, the guard remembers the face, no engagement.
  - **Still drawn:** today's `drawn.seen`.
  - Cameras and rivals never warn.
  - While the warning runs, no unit fires on that agent.
  - The warning adds **no log code**. The host reads `Game.Warnings`.
- **`hostilesFireMoving`.** A final-fight hostile that is walking (a cover step, a blocker on its lane) fires at walking pips.
- **Panic.** With the lab on, panic is allowed outside a fight. The going-loud card shows the loudest of the draws it would cause.

### Previews (read-only)
- `PreviewFire`, `PreviewVolley`, `ChargeOf`, `FieldOf`, `Combatants()` and `Warnings`.
- See `host-interface.md`.

### Codes
- **Refusal code:** one new code, `no-charge`.
- **Log codes:** none new. The new lines reuse the silent prefixes: `fire.volley`, `fire.done` and `order.field`.

### Scenarios
- N140–N148, all with the lab on: the presets; the pip ladder and the chip; damage and hits to down per weapon, and a loadout over Arms refused; fighting on after a kill; walking into range; the volley forecast is exact, with charge paid, drawn and refilled, and `no-charge`; the police warning, backed off and held; the field; the two stances.
- Every scenario also gets the suite's same-checksums-twice and replay tests.

### Matrix
- **Code:** `sim/Suborned.Sim/ScenariosFightLab.cs` (the scripted runs and the §6.2–6.4 verdicts) and `sim/cli/FightLab.cs`.
- **Usage:** `bun run sim fight-lab [--runs ..] [--presets ..] [--scripts <dir>] [--rulebook <file>]`.
  - `--scripts` writes each run as a replayable script.
  - `--rulebook` runs a probe of other numbers and writes nothing.
- **Outputs:** `briefs/027/keel/fight-lab.json` and the checkpoints.

### Checkpoints
- **CP-LOUD:** `briefs/027/checkpoints/cp-loud.script.json`. It has N10's start (security 1) and orders, and ends at 2:36.5 on the Enforcer's fire order on the guard.
- **CP-SAFE:** `cp-safe.script.json`. It has N11's route A (quiet), and ends at 5:53.8, when the Enforcer is out with Vale and before the ambush.
- Both are written without `fightLab`. The host puts the preset's `start.fightLab` on top and replays to `until` (see `host-interface.md`).
- Both replay identically.

## 2. Evidence
- `dotnet test sim/Suborned.Sim.Tests`: **907/907 pass**. That covers the 189 earlier scenarios, N140–N148 and their replay and checksum tests.
- The DP1 session was re-recorded for 0.10.0 and the new inputs hash (`drive-session`). Its orders, start, until and outcome are byte-identical to HEAD; only the record moved.
- `hashes --start-text BASE` gives the same behaviour as before for all 229 runs (lab off).
- `bun test contracts tools/sim`: **198 pass, 3 fail**. All 3 failures are in `contracts/missions/handover/text.test.ts` and are Fable's `text.json`:
  - `volley` and `field` have no action;
  - `no-charge` has no lock line.
  - These need a handoff (§5). I have not edited `text.json`.
- `bun run typecheck`: clean.
- `bun run sim fight-lab`: exit 0, and every run replays identically from its order log. The output is in `out/keel027/matrix.txt`.

## 3. The matrix (the perfect player, headless)

**How the runs are scripted.**
- All of them are closed-loop C# scripts with zero reaction time.
- **Prepared runs:**
  - P-A, P-G and P-B replay `route-a-board`, `route-g-fast-board` and `route-b-board` up to their fight (2490, 2066 and 4470 ticks).
  - Then the Enforcer walks Vale to the car as the ambush springs.
  - The three fight it out with focus fire; anyone downed is carried; everyone boards; Go.
- **L1 and L4** are as the spec says. L4 first fights every combatant that comes until 15 s pass with none, or until 4:00.
- **L2** waits drawn at the pickup and Takes at 6:10.
- **L3** is route G-fast with a shot on the guard in place of the knockout at 2:03.6.
- **L1c** is my extra probe: L1 with the Tech looping the entrance camera first. L1 as written fails on the camera at 0:56, before any fight, so it tests nothing about fights.

**How to read the columns.**
- T = Trail (mission/total). Total adds the campaign Trail the debrief carries (captives, cases).
- St = Standing (start→end).
- hp-lost = the cell's health lost.
- hits = hits the cell took.

### today (v0.24)

| run | result | end | T | A | St | public | down | hp lost | hits | verdict |
|---|---|---|---|---|---|---|---|---|---|---|
| L1 | fail, entry-seen-camera | 0:56 | 14 | 2 | 2→−1 | raised | 0 | 0 % | 0 | pass (fail) |
| L1c | fail, sealed | 4:58 | 31 | 2 | 2→−2 | raised | 0 | 44 % | 8 | pass (fail) |
| L2 | fail, sealed | 3:22 | 44 | 3 | 2→−2 | high | 0 | 33 % | 6 | pass (fail) |
| L3 | fail, sealed | 5:05 | 26 | 2 | 2→−2 | raised | 3 | 66 % | 12 | pass (fail) |
| L4 | fail, sealed | 7:21 | 31 | 3 | 2→−2 | high | 0 | 11 % | 2 | pass (fail) |
| P-A | success | 5:00 | 5 | 1 | 2→3 | calm | 0 | 11 % | 2 | pass |
| P-G | success | 4:24 | 6 | 1 | 2→3 | calm | **1** | **44 %** | 8 | **FAIL** |
| P-B | success | 8:35 | 3 | 0 | 2→3 | calm | 0 | 22 % | 4 | pass |

- §6.4 triggers: **6.4.5** (P-G downs an op; it is carried in, so no capture).
- 6.4.7 lethality: an op goes down in 2.5 s and the Enforcer in 5.5 s. That is on the line, not under it.

### lab (§2 defaults)

| run | result | end | T | A | St | public | down | hp lost | hits | verdict |
|---|---|---|---|---|---|---|---|---|---|---|
| L1 | fail, entry-seen-camera | 0:56 | 14 | 2 | 2→−1 | raised | 0 | 0 % | 0 | pass (fail) |
| L1c | fail, sealed | 5:00 | 31 | 2 | 2→−2 | raised | 0 | 22 % | 4 | pass (fail) |
| L2 | fail, sealed | 3:24 | 44 | 3 | 2→−3 | high | 0 | **0 %** | **0** | pass (fail) |
| L3 | **success** | 4:50 | 22 | 2 | 2→−2 | raised | 0 | **0 %** | **0** | **FAIL**: public security raised; no op down or captured, health lost < 40 %, no open case |
| L4 | fail, sealed | 7:21 | 31 | 3 | 2→−2 | high | 0 | **0 %** | **0** | pass (fail) |
| P-A | success | 4:56 | 5 | 1 | 2→3 | calm | 0 | 0 % | 0 | pass |
| P-G | success | 4:14 | 6 | 1 | 2→3 | calm | 0 | 22 % | 4 | pass |
| P-B | success | 8:28 | 3 | 0 | 2→3 | calm | 0 | 16 % | 3 | pass |

- §6.4 triggers:
  - **6.4.1:** L3 wins with 0 hits taken.
  - **6.4.6:** L3 failed under today and wins under the lab with no added cost.
  - **6.4.7:** an op goes down in **1.1 s** and the Enforcer in **2.3 s**, both under 2.5 s.
- **lab-charge-off and lab-field-on:** every row is the same as the lab's.
  - Charge never binds in these runs. The sidearms are free; the carbine at 8 a shot and 0.8 s gets about 12 shots of continuous fire before it stops, and the fights are shorter than that.
  - The scripted player's field rule (raise it below half health while fired on) never triggers, because nobody gets that low.
  - The charge and the field need a fight long enough to be felt before they can be judged.

### What the numbers say
1. **Police and the response unit never fire in the lab's loud runs.** L2, L3 and L4 have 0 hostile shots; today's L3 takes 12 hits. There are two causes:
   - Hostiles only fire inside their weapon's **effective** ring (Pulse's `FoesInReach`). The lab sidearm's ring is 12 m, against today's pistol at 15 m.
   - Meanwhile the cell fires out to its max ring (20 m sidearm, 30 m carbine) and kills in about 1 s.
   - So today's officer-2 at his 14 m stand is out of reach for him, while he is in reach for the cell.
2. **The lab makes loud cheaper than today.** L3 turns from a fail into a clean win with no damage taken. This is the case Sky asked us to rule out.
3. **The lethality ceiling is broken both ways.** Two trained officers drop an op in 1.1 s; the cell drops a rival in under a second. It is too fast to read, even with `freezeOnFiredOn`.
4. **Prepared runs pass under the lab**, and P-G improves on today (today it downs an op).
5. **6.2's "public security High" is out of reach for an Alert-2 loud run** with no civilian witness. Today's reference loud route (`loud.json`, 10 T, Alert 2) would not meet it either. Only L2 and L4 reach Alert 3. Because of this, L3 under the lab can never become a "heavy debrief" however it is tuned.

## 4. Proposals for Motive (not applied)

I ran each probe with `--rulebook out/keel027/rb-probe-*.json`. The probes write nothing.

| probe | change | what it did |
|---|---|---|
| a | sidearm eff 15 m, max 25 m (today's rings) | Hostiles fire again (L3 takes 1 hit). L3 still wins. P-G and P-B now lose 27 % (just over 25 %). Lethality is unchanged at 1.1 s. |
| b | a, plus sidearm every 10 ticks | L3 loses 22 %. P-G loses 38 %. Lethality 1.5 s. |
| c | a, plus sidearm damage 8, every 8 ticks | Lethality is fixed (op 2.9 s, Enforcer 3.7 s). L3 loses 35 % with 1 op down and meets every heavy-debrief line except "public security High". But P-G and P-B each lose an op (62 % and 35 %). |

Recommendations, for Motive to decide:
1. **Keep the hostiles' reach at least today's 15 m.** Either set the lab sidearm `eff` back to 15 m, or (Pulse) let hostiles fire out to their max ring at 2 pips. Without this, the lab hands the cell a free range band. This is the first thing to fix.
2. **Slow the kill for 6.4.7:** sidearm 8 damage every 0.8 s (probe c), or op health 50.
   - The chip would read a rival as ×4 instead of ×3.
   - Rivals would then need softening so the prepared runs stay inside 25 %. One option: make rivals untrained beyond point blank, which they already are, and give them a shorter 12 m ring than police.
   - That is a Motive/Pulse call.
3. **Re-read §6.2's "public security High".** It requires Alert 3, which needs a civilian to witness violence. Suggestion: accept "raised" plus Trail ≥ 12 and a casualty line, or make Alert ≥ 2 enough.
4. **Charge.** To see it in play: the carbine at 10 or more a shot, or regen paused for 1 s after any shot. That too is a Motive call.
5. **L1 as written** dies on the entrance camera. Keep it, or adopt my L1c (camera looped) as the loud front probe.
6. **The high edge** needs levels on the site (Datum or Splice). Until then it never applies.

## 5. Open issues and handoffs
- **Fable** (`contracts/missions/handover/text.json`) needs three entries; until they exist, the 3 `text.test.ts` failures stay:
  - a lock line for `no-charge`, with verbs `volley` and `field`;
  - actions for `volley` (`engine.do: "volley"`) and `field` (`engine.do: "field"`).
- **Motive:** the §4 numbers; the reading of charge regen; whether the per-volley shot limit should keep its `no-charge` refusal or get its own code.
- **Latch:** see `host-interface.md`. `unity/` is untouched, and the Unity export (`bun tools/sim/unity.ts`) has not been run.
- **Not built:** cover snap (`fight.lab.coverSnap`, B5). It was not in this request.
- **Suture:** the matrix rows are in `fight-lab.json`, and the scripts can be re-run with `--scripts`.

## 6. Update (2026-10-20): Motive's tuning

Motive changed six `lab-*` values (`briefs/027/motive/tuning.md` §1): sidearm eff 15 m, max 18 m, every 10 ticks; pip 3 = 600; draw 3 ticks; carbine charge 20.

What I changed in response:
- **Tests.** N140, N141, N142 and N145, plus the lab test in `contracts/rules/rulebook.test.ts`, now read the lab numbers from the rulebook instead of hard-coding my first values. Further tuning won't break them.
- **Verdict.** The heavy-debrief verdict (§6.2) follows Motive's corrected reading (tuning.md §3.1): "public security High" is no longer one of the required conditions.
- **DP1.** Re-recorded for the new inputs hash. Its orders, start and outcome are unchanged.

The re-run matrix (`bun run sim fight-lab`, output in `out/keel027/matrix-tuned.txt`, written to `fight-lab.json`):
- **today** is unchanged from §3.
- **lab:**
  - L3 wins at 4:50 and is now a heavy debrief: 22 T, Alert 2, Standing 2 → −2, one agent down, 33 % health lost, 6 hits taken.
  - L1, L1c, L2 and L4 fail.
  - P-A and P-B pass.
  - P-G fails only on 27 % health lost (over 25 %). Motive recommends cover snap (B5) to fix it.
  - No §6.4 trigger fires. Lethality is 2.5 s for an agent and 4.5 s for the Enforcer, right at the floor.

The `today` preset is byte-identical:
- `hashes --start-text BASE` gives the same behaviour as before the brief for all 229 earlier runs. The file is `out/keel027/hashes-tuned.json`.
- N140–N148 are new.

Evidence:
- `dotnet test`: 907/907 pass.
- `bun test contracts tools/sim`: 201 pass, 0 fail.
- `bun run typecheck`: clean.
- Both checkpoints replay identically.

Fable added `volley` and `field` to `VERBS` in `contracts/missions/handover/text.schema.ts`. That is correct: both are panel orders (target mode's shots, and the field), not controls like `drive`.

## 7. Update (2026-10-20): volley out of range

- **Decision:** a volley out of range walks in and holds its shots until it stands inside the effective ring with a line. The forecast is fired from that stop point. Details are in `host-interface.md`.
- **Engine changes:**
  - The walk-into-range stop check now runs before the ready check, so an agent still drawing stops at the same place. In lab runs only.
  - A volley holds its shots while it closes in.
- **New scenario:** N149.
- **Results:**
  - The lab matrix is identical to §6.
  - Today is byte-identical (229 of 229).
  - `dotnet test` passes 911 of 911.

## 8. Update (2026-10-20): Motive's §6 rulings and Pulse's F2 answer

**Built (all lab-only; `today` byte-identical):**
- §6.5 cover snap scope: a drawn agent's move to a ground point only (named places `d:`/`crew:` never snap). Lab P-B passes (8:40.6, 16 %).
- §6.1–6.2 aim memory (`Gun.Mem`, the pair's count; nothing resets it; any switch waits aimNew) and `lab-acc-start` 0, for every shooter.
- §6.3 a volley's shot stamps the charge rest; `lab-charge-rest-ticks` 20; refunds stamp nothing.
- §6.4 one shout per watcher (`lab-warn-per-watcher` 1; agents drawn in his view while it is open share it); a heeded shout +1 Trail once (`lab-warn-trail`).
- §6.5 P-G line ≤ 33 % with no op down; §6.4.7b check (`LethalityCell`).
- F2 (Pulse, `briefs/027/pulse/kite-response.md`): `fight.lab.kite` and `GameKite.cs`; `Start.fightLab.kite`; `PreviewAnswer`. "Can answer" uses the body's own sight (a pre-fight officer's watch cone), not a plain line: with a plain line a drawn agent behind an officer exempted him while he never fired (found by N157).
- Adversarial rows `L3:kite`, `L3:reset-kite`, `L3:focus-reset-kite`, `L3:cycle-kite`, `L3:carbine-kite`, `P-G:reset` (`ScenariosFightLabAdv.cs`, Suture's `SEngage`); the panic-spam and volley-spam micros; the free/side column (§6.6.2).
- Scenarios N152–N154 (§6.1–6.4) and N155–N158 (Pulse's N150–N154, renumbered).
- Two changes to Suture's script: the reset fires only right after a shot (at acc-start 0 "NextShot moved" re-aimed every tick and never fired); the volley micro runs at healthScale 100 so the guard outlives it.

**Evidence:** `out/keel027/matrix-final.txt` (written to `fight-lab.json`), `out/keel027/hashes-final.json` (228 of 228 pre-brief runs identical to `hashes-before.json`).

**Open:** brief-013 units (police-n, robots) not covered by the kite (Pulse 6.4.1); a pre-fight crew with no cover in its seek radius and no blind point within 8 m stands (Pulse's rule has no further fallback); `ai.kite` needs a `silentLogs` entry in `text.json` (Fable).

## 9. Update (2026-10-20): Suture's retest fixes (R1–R6), lab-only

Inputs: Suture `suture/retest.md`, Motive `motive/tuning.md` §7, Pulse `pulse/retest-fixes.md`. Everything reads `Lab` (and the kite switch); `today` is byte-identical (`hashes --start-text BASE`: 228 of 228).

**Built**
- Rulebook (`fight.lab`): `lab-kite-contact-cm` 3000, `lab-kite-rearm-ticks` 80, `lab-kite-hide-max-ticks` 150 (`kite.contact/rearm/hideMax`), `lab-police-contest-n` 1 (`fight.lab.policeContest`); schema; rule text. §7.3's sentence added to the `op-run-speed-cm-s` reason (dialogue drift declared).
- R2: the chase budget counts only the walk the kite answer orders, within `contact` of the shooter (the walk in and every objective move are free: Pulse §1a and Motive §7.2 rule 3 both hold); a remainder under one grid cell caps; a capped body re-arms after 8 s while the shooter fires.
- R1 movement: Pulse §2's contest (the first officer's stand pulled to 90 % of his ring of the car). Motive §7.2 rule 3: in the final fight no police body chases; shot from out of reach it takes cover against the shooter, no farther from the car than its stand. The blocker keeps its lane.
- R3: brief-013 police units (not robots) are a third kite side, one dispatch together; a unit at its scene answers off its post (`unitDisp`).
- R4: no hide → advance to the ring (5 ticks); held out of the line it walks on while the shooter is in its band and firing; no lean-out while an armed agent covers the lean-out point; a hold past 15 s walks on.
- §7.4: the crew's `Hit` clears at a drive-back; a shot later in that tick finds the crew gone (else the same volley paid +2 T twice).
- §7.5: the snap and the marks treat a point a hostile stands on or walks to as held. Cause: `cover.leave` releases the blocker's bookkeeping on its walk to cv-087, then the Scout snapped onto the point it was still walking to. The `lab-cover` player seeks marks only within 15 m of the Vale holder once Vale is out.
- R5: rows `L3:enfgo-carfree-runkite`, `L3:focus-enfgo-carfree-runkite`, `L3:enfgo-carfree-kite` (Suture's flags); counters reset per row; verdict 6.4.8 (a) a fight police body up at the end that never had the Vale holder (or an agent within 15 m) in reach, (b) no hostile shot at the cell after Vale came out; contact column `c had/seen s shots`.
- `PreviewAnswer`: in the final fight a police body's answer is its cover point (`Cover` true); a unit at its scene answers like the response unit; a crew with no hide shows its advance.
- Scenarios N159–N165.

**Matrix, lab** (`matrix-retest.txt`): L3 win heavy 38 %, 1 down · P-A 0 % · P-G 22 % · P-B 16 % · lab-cover P-G 22 % · the three R1 rows fail enforcer-down (33, 33, 55 %) · kite rows fail sealed · no 6.4 trigger.

**Open**
1. Clash (Aster): Motive §7.2 rule 1 sends every police body (officers, the joined unit, brief-013 units) for the Vale holder; Pulse's contest pulls one officer to the car (all pulled in, matrix L3 fails 77 %). Built: Pulse's. Read literally over units, 6.4.8a fails matrix L3 (police-1..4 up, never in reach), so 6.4.8a judges the fight's bodies and lists the units as a note.
2. `lab-field-on`: L3:kite, reset-kite, cycle-kite win at 16 % and enfgo-carfree-kite at 22 % (all FAIL the verdict). Rule 3 took away the final-fight chase; the field keeps the band shooters up. The field is off in the lab preset. Motive.
3. `ai.kite` now also carries the contest and cover lines (Fable: silentLogs reason).

**Today with the candidates on** (scratch engine, `retest-today-probe/`): the contest alone closes R1 (fail enforcer-down 33 %) but opens reset-kite, focus-reset-kite and cycle-kite (fail sealed → win 16 %, 0 shots at the cell after Vale). The contest plus the 18 m band closes every loud row. The drive-back Trail changes no today row. The 18 m band alone doesn't close R1 (win 4:39.8, 0 hits). Cover inside the car ring was not run: it needs Vantage's points.

## 10. Update (2026-10-20): Suture's second retest (N1, N3, N4), lab-only

Inputs: Suture `suture/retest2.md` (N1–N6), his probe diffs `suture/probe/retest2/`, Motive `motive/tuning.md` §6.6 (the pre-accepted fallback). `today` is byte-identical (`hashes --start-text BASE`: 228 of 228, `retest2/today-hashes.txt`). Inputs hash now `f869a690f20e18c1`.

**Built**
- N1 value: `lab-sidearm-max-m` 18 → 16 (Motive §6.6; reason updated). The carbine fallback was not taken: `L3:carbine-kite` and `L3:tight-carbine-kite` fail enforcer-down in every lab preset.
- N1 AI (Suture's FB probe, as built; **Pulse to review the motion**): rule 3 with no qualifying cover: the final-fight police body closes to its stop point of the shooter (first route point within ai.stopReach of its effective ring), once each cover.stepGap; its stand becomes that point. Not changed from the probe: a cover point it held is not released on the walk; it re-plans only each 4 s against a runner. Rule text in `fight.lab.rule`; `PreviewAnswer` shows the close point.
- R3/N4: the same fallback for brief-013 police units in the final fight (they walk there off their post). Suture's band micro (`BAND=police-1@2290@fixer`, 18 m rulebook copy to reproduce his 17.4 m case): police-1 moves 5 ticks (0.5 s) after the first shot.
- N3 rows: `at<s>`, `hold[30|90]`, `holdcar[20]` and the band margins as policy flags (`tight` = 0.3 / 0.2 m, or `km<cm>` / `fm<cm>`), all off unless named. New rows: `L3:at208-runkite`, `focus-at208-runkite`, `at208-kite`, `at196-runkite`, `at236-runkite`, `hold-runkite`, and a `tight-` pass over every kite row (12). 6.6.2's free-downs trigger already prints for any `:` row with free/side > 1.
- N161 reads both answers (cover, or no cover → close in). DP1 re-recorded for the inputs hash (orders, start, until and outcome identical).

**Matrix, lab** (`matrix-retest2.txt`): L3 win heavy 50 %, 2 down, 1 captured · P-A 5 % · P-G 27 %, no op down · P-B 16 % · every adversarial row fails (the N3 rows enforcer-down 33–100 %) · no 6.4 trigger. No lab preset has a FAIL win. `today` shows N6's rows (FAIL wins at 0 %), unchanged by design.

**Sweep** (`retest2/`, Suture's probe player, 771 L3 policies × margins 1.0/0.5, 0.3/0.2, 0.1/0.05 m): lab 0 wins, lab-field-on 0 wins, 0 replay differences. 165 policies end entry-seen-camera (the `clear` variants walk into the entrance camera), as before.

**Note: at max 16 the default-margin kite player can't fire** (it steps away inside 16 m and fires inside 15.5 m), so the default rows now measure little; the `tight` rows carry the band.

**Open**
1. N2 (Motive): under `lab-field-on` the casualty line reads health lost, which the field hides. No lab-field-on row wins now, so it has no case in the matrix, but the measure question stands: count absorbed damage (or hits) when the field is on? Not built.
2. Pulse: review the no-cover close-in (stop point, re-plan cadence, the held cover point, keeping the car in reach). N5 (pre-fight crew against a runner) untouched.
3. Rule 1 (police go for the Vale holder) still not built (Aster's ruling stands).

## 11. Update (2026-10-20): Suture's third retest (N7, N8, N9), lab-only

Inputs: Suture `suture/retest3.md` (N7–N10), his diffs `suture/probe/retest3/`. `today` is byte-identical (`hashes --start-text BASE`: 228 of 228, `retest3-today-hashes.txt`). Inputs hash now `ba228028a60bdf42` (rule text only); DP1 re-recorded (orders, start, until and outcome identical).

**Built**
- N7 (Suture's `PROBE_REPLAN`, as probed): a final-fight police body whose position no longer shields it from where the shooter stands now plans again `kite.react` (5 ticks) after its last plan, not `cover.stepGap` (4 s). A shielded body holds. Rule text in `fight.lab.rule`.
- N8: the scripted player takes Suture's flags `step<cm>`, `norivals`, `blk`, `aware` (all off unless named). Three matrix rows: `L3:km10-fm5-step200-norivals-at208-runkite`, `L3:km15-fm5-step300-norivals-enfgo-carfree-runkite`, `L3:tight-step200-focus-norivals-at236-runkite`. Before the fix they reproduced his wins (4:21.5 at 0 %, 4:37.4 at 0 %, 4:58.7 at 5 %); now they fail enforcer-down in every kite-on preset.
- N9 **not built**. Releasing the held cover on closing in, together with N7, reopened 4 cheap lab wins in the sweep (16–38 %): the freed point sends the partner officer to cover instead of closing. Back to Pulse.

**Matrix** (`matrix-retest3.txt`): unchanged outcomes. L3 50 % heavy, P-A 5 %, P-G 27 %, P-B 16 %, lab-cover L3 66 %. No kite-on lab preset has a FAIL win. Damage moved on failing rows only: L1c 44 → 55 % (still sealed 5:00.4), the `tight-*kite` rows heavier. `lab-kite-off` now shows one FAIL win (the km10 row, kite answer off by design, as `today`'s N6 rows).

**Sweep** (`sweep-retest3.txt`, Suture's 3,792 on the real tree): lab 79 wins, 40 FAIL, all ≥ 61 % and 6.4.8a-only, 0 cheap; lab-field-on 5 cheap (27–33 %, N2). 0 replay differences. Same as his `PROBE_REPLAN` row.

**Open**: N9 (Pulse), N2 and N10 (Motive), Suture's 3,792 rerun.

## 12. Update (2026-10-21): engine 0.15.0, fire on the move and anyone can be shot (tuning.md §8, §9; both presets)

Inputs: Motive `motive/tuning.md` §8, §9 (handoff §9.6) and §9.7 (Sky's answers: own agents stay refused, hostiles fire walking in today OK, point blank ignores cover in today adopted, cars shootable but not built here). Baseline: the 0.14.1 tree as it stood before this round (`out/keel036/base`, a copy of `out/sim-unity/tree` at 04:41).

**Built** (`GameCombat.cs`, `GameFightLab.cs`, `GameOrders.cs`, `GameDrive.cs`, `GameAi.cs`, `GameShootAnyone.cs` new, `Rules.cs`; rulebook values `mod-running` 250, `mod-running-away` 125, `mod-target-running` 850, `run-away-cone-deg` 60, `lab-run-pips` 2, `lab-pip-0` 100, `health-camera` 1, each `[S]`, proposed, reason naming §8.3 or §9.2; keys `weapons.pointBlankCover` false, `weapons.runAwayCone`, `weapons.mod.{running,runningAway,targetRunning}`, `fight.lab.{runPips,pip0}`, `weapons.health.camera`, `ai.cover.stepFires` true)
- §8.2 rules 1–6 as written; a fire order given in the final fight still ends at `fight.over`, a new one after it is taken and traced.
- §8.3 tables in `Rules.Accuracy(…, Gait, …, targetRunning)` and `Rules.LabPips(…, Gait, …, runPips)`; the away cone as an integer predicate (`Rules.RunsAway`, cos 60° = 500 ‰ at load; the edge counts as away; exact 60° is not reachable by integer vectors). Hostiles fire as they walk in today.
- §8.4 adopted (Sky): inside point blank cover does not count in today. It moves no prepared row (`out/keel036/matrix-pbcover-true.txt` against `matrix-0.15.txt`: P-A, P-G, P-B identical in both presets).
- §8.3 fallback (a step to cover fires nothing) built as the switch `ai.cover.stepFires` and **not adopted**: its condition is not met. Today's P-G fails because the blocker and the advancing rivals fire on the Enforcer walking Vale (rival-1 on its lane at 281 ‰), not because of cover steps; with the fallback on, P-G today still fails (61 %).
- §8.8 preview fields; §9.1–9.3 (fight-over lifted, knocked-out bodies are targets, gap 1 bodies, gap 2 cameras, the always-hold `LoudCard.Fails`/`Hold`).
- Scenarios: F8-O1…O8, O10, O11, F8-RUSH, -RUSH-B, -BREAK, -CIRCLE (range, today and `-LAB`), F8-O9 and F8-O9-VALE, S9-VALE, -CROWD, -INEZ, -CAM, -AFTER, -KO, -SELF, -UNIT (Handover). Matrix rows `L3:runfire-kite`, `L3:circle-kite`, `L3:charge`, `P-G:runfire`.

**Motive's §8.7 lines, as the engine plays them** (recorded as `Run.Line`, not failed checks, so they come back to Motive):

| line | today | lab |
|---|---|---|
| F8-RUSH down ≤ 8 s | MISSED 8.3 s (arith 4.8) | MISSED 9.3 s (arith 6.2) |
| F8-RUSH no agent down, ≤ 2 hits | met (0, 1) | met (0, 1) |
| F8-RUSH sooner than walk-in | met 8.3 vs 12.9 s | (lab walk-in 3.0 s) |
| F8-RUSH-B charge loses ≥ 2 more | met 2 vs 0 | met 4 vs 0 |
| F8-BREAK A: no agent down / ≤ 50 % / ≤ 1 foe | MISSED 2 down, 55 % / met | MISSED 2 down, 55 % / met |
| F8-BREAK B: ≤ 1 down / ≥ 1 foe | MISSED 2 / met | MISSED 2 / met |
| F8-BREAK standing loses ≥ 3 | met 4 | met 4 |
| F8-BREAK cease takes no fewer hits | MISSED 0 vs 10 | MISSED 10 vs 11 |
| F8-CIRCLE takes > deals; standing sooner | met 6 > 1; 7.1 s vs not in 30 s | met 6 > 2; 15.8 s vs not in 30 s |

F8-BREAK: the range floor is 45 m, so the 20 s break runs straight away to the far wall (~7 s), then along it. Firing while breaking keeps the police alarm open and they close (§8.2 says so: "to break contact, stop firing"); ceasing breaks contact (today 0 hits). Motive's "cease takes no fewer hits" contradicts his own §8.2.

**Matrix** (`matrix-0.15.txt`; baseline `out/keel036/matrix-base.txt`):
- P-A today/lab 0 %, pass. P-G lab 33 %, pass (38 % before). **P-G today: failure rivals-reach-vale at 3:42.3** (success at 55 %, already over its 33 % line, before). **P-B today 44 %** (33 % before), lab 50 % (50 % before): both over 25 %, 1 op down, as at the baseline.
- **One adversarial row got cheaper: `today L3:km10-fm5-step200-norivals-at208-runkite` wins at 11 % (failure enforcer-down 100 % before).** Cause (probed by switching each rule off): a run no longer ends the fire order, so the kite step keeps the aim (today: no re-aim, no `aimNew` wait after each step); with either "fire stops a walker" or "a run keeps the fire" off it fails again. For Motive.
- The new rows all fail: `L3:runfire-kite` sealed (today 5 %, lab 0 % health lost), `L3:circle-kite` sealed (61 %, 66 %), `L3:charge` enforcer-down (100 %), `P-G:runfire` rivals-reach-vale (72 %, 77 %).
- Loud rows gain a case (the guard's body, gap 1). Lab L3 is a failure (enforcer-down 88 %), as at the baseline: "L3 still wins heavy" was not true before this round either.
- The scripted matrix player now fires first and then gives the walk again (`FireKeepWalk`), and leaves the Tech at the cabinet while the loop holds; it never fires on a final-fight hostile after `fight.over`.

**Routes** (`route-diff-0.15.txt`, A = the 0.14.1 tree, B = 0.15.0, the same scripts): 38 routes, 27 moved, no outcome or fail code moved; the loud family moves only at the run (no `fire.cancelled`); the boarded and prepared routes lose more health (route-g*-board 11 → 33 %, route-b*-board 0 → 11 %, route-a/g/g-fast/g-kill 44 → 66 %) from the blocker firing on its lane.

**Evidence**: `dotnet test sim/Suborned.Sim.Tests` 1189/1189; `bun test contracts tools/sim tools/route-diff` 221 pass; `bun run typecheck` clean; `bun run verify` OK (dialogue drift declared by `bun run drift:auto`); every matrix run replays from its order log; DP1 re-recorded (orders, start, until identical; only the record moved).

**Open**
1. Motive: the km10 new win (keep the aim through a run, or reset it at a run's start?); P-G today; F8-RUSH's 8 s line; F8-BREAK's lines; N10's loud route now loses at level 1 (3 down, sealed 5:48.5; was a win at 5:00.3): the scenario records it instead of asserting the win.
2. Cars as targets (Sky §9.7): not built; needs Motive's numbers (health, disabled/burning/explodes, blast harm, occupants, the getaway car's fail or change).
3. Fable: `fire-running` and `fight-over` stay in `Answer.Codes` until their lines go; new silent codes `fire.camera-dark`, `fire.camera-hit`; the hold card's fail line.
4. Rule reading to confirm (Motive): the shot-dead person's act starts landed (one incident, one dispatch: the shooting already called the response), its Trail is the gunfire's (no extra kill Trail), and the guard shot keeps his own missed check-in path.
