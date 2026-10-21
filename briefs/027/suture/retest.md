# Brief 027 · Retest of F1–F5 and F8, and the next cheapest win (Suture)

**Date:** 2026-10-20. **Engine:** `suborned.sim/0.10.0`, inputs `c75922cfa066d97d`, rulebook sha256 `008eeeb37e7d5f59…` (the tree Keel's `matrix-final.txt` was made on; my lab matrix rerun matches it row for row).
**Asked by:** Aster, after Keel built Motive's §6.1–6.5 and Pulse's kite response (`keel/fight-lab.md` §8).
**Read-only:** no file under `sim/`, `contracts/` or `unity/` was edited. Probes ran in a scratch copy (`out/suture027/src3`) whose engine is byte-identical to `sim/` (`diff -rq`: only the scripted-player file differs, my harness patch). The headline script was also replayed with `bun run sim play` on the real tree: same final hash.

## 0. Short answer

Motive's rulings work. F1, F3, F4, F5 and F8 are closed. F2 is closed for the four kite rows, but **loud L3 can still be won cheaply**:
- **lab:** win at 4:36.4, 11 % health lost, 2 hits, no op down, Standing 2 → 0, 15 T. This fails §6.4.1 and §6.6 item 1.
- **today:** the same orders win at 4:45.0 with **0 hits**. The cheap loud win is not caused by the lab; the matrix player is just too weak to find it (it is the F7 problem again).

The player needs three things the scripted rows never do:
1. walk the Enforcer and Vale straight to the car, and don't stop to fight;
2. leave once the car is free (the matrix L3 does this; Keel's adversarial rows wait for the fight to end);
3. **run** the kite steps (2.8 m/s against the police's 2.2 m/s).

The kite response doesn't stop this, and its chase budget makes things worse: the officers spend their 20 m walking in, then stand frozen out of reach.

| # | finding | severity | owner |
|---|---|---|---|
| R1 | **Cheap loud win, both presets:** run-kite, Enforcer straight to the car, leave when the car is free | **critical** | Pulse (AI) + Motive (running vs the chase, car hold) → Keel |
| R2 | **Chase budget spent, then held:** the walk in uses it up; with 1 cm left nothing moves; once capped, an officer never comes back into range | **high** | Pulse (rule) → Keel (stall bug) |
| R3 | **brief-013 police units** can be shot down from 17.5 m with no answer | medium | Pulse → Keel |
| R4 | **Pre-fight rival crew:** answers 6.8–7.1 s late, never fires beyond 15 m, leans back out into the same line; a second drive-back costs 0 T | medium | Pulse (fallback) + Motive (retreat cost) |
| R5 | **The matrix can't see R1:** §6.6.2 "free downs" passes (1/1), because the cheap win needs no kills; the adversarial rows have two handicaps the matrix L3 doesn't | high (process) | Keel |
| R6 | **lab-cover P-G fails:** the player who uses the cover marks loses Vale (55 %, 2 down) | medium | Motive + Keel (Vantage for the marks) |
| R7 | After a volley the agent keeps firing at will for free (16 volleys paid, 30 shots fired) | low | Motive (confirm intent) |

## 1. Retest of F1–F5 and F8

| F | was | now (lab) | call |
|---|---|---|---|
| F1 accumulator reset | a cease + fire after every shot always hit at 2+ pips | `L3:reset-kite` is the same as `L3:kite` row for row (72 shots, 6 hits, 33 %); `P-G:reset` is the same as `P-G` | **closed** |
| F2 stand-off band | L3 reset-kite wins at 0–5 % | the four kite rows fail sealed; the response unit moves 0.5 s after the first shot | **closed for those rows; reopened by R1–R4** |
| F3 first shot always hits | the cell downed a rival 0.5 s after the first shot | §6.4.7b 1.0 s; op 3.5 s, Enforcer 4.5 s; P-G 22 %, no op down | **closed** |
| F4 panic spam | 60 warnings, Alert 1 | 1 shout, then 16 `drawn.seen`, Alert 3, 22 T, run ends enforcer-down | **closed** |
| F5 volley charge | bar low point 88 | volley(1) spam: bar low 0, 154 refused | **closed** (see R7) |
| F8 cover snap breaks P-B | P-B failed rivals-reach-vale | P-B 8:40.6, 16 %; snap off 8:40.5, 16 % | **closed** (see R6) |

**Keel's change to my re-aim script is fair.** My test "NextShot moved" also fires on the aim itself, because `Aim()` pushes NextShot by aimNew. At acc-start 0 the script re-aimed every tick and never fired: that was a bug in my script, not a strategy a player would use.
- I added the honest worst case, `rawreset`: after every real shot, cease and fire again, with no accumulator test.
- In the lab it changes nothing. `L3:rawreset-kite` is the same as `L3:kite`; `L3:focus-rawreset-kite` lands at 22 % with 73 shots, against 75 for `focus-reset-kite` (the aimNew wait shifts one shot). `L3:rawreset-enfgo-carfree-kite` is the same as its non-reset row.
- Under aim memory, re-aiming the same target is just ordinary play. Bouncing A → B → A (`cycle`) is also the same as `kite`.
- Under `today`, `rawreset` still runs F1's loop (195 shots, 0 hits taken; it fails sealed). That is today's rule, which stays as it is.

## 2. Findings, ranked

### R1 · Cheap loud win, lab and today (critical)
**Script:** `retest-scripts/L3.focus-enfgo-carfree-runkite.lab.script.json` (lab) and `L3.enfgo-carfree-runkite.today.script.json` (today).
**Replay:** `bun run sim play briefs/027/suture/retest-scripts/<file>`.

**The orders** (policy `focus-enfgo-carfree-runkite`, harness diff in `probe/retest/`):
- L3 as Keel scripts it: route G-fast to 2:03.6, the Enforcer shoots the guard, then Enter.
- The three keep out of every armed hostile's effective ring + 1 m, and make their 4 m steps away as **runs**. They fire only when they stand.
- When Vale comes out (3:14), the Enforcer walks him straight to the car and never stops.
- Everyone boards once the car is free (`!CarHeld()`, the same test the matrix L3 uses).

| preset | row | result | end | T | A | St | down | hp lost | hits |
|---|---|---|---|---|---|---|---|---|---|
| lab | matrix L3 | win, heavy | 4:54.6 | 22/26 | 2 | 2 → −2 | 2 (1 captured) | 44 % | 8 |
| lab | **focus-enfgo-carfree-runkite** | **win, FAIL** | **4:36.4** | 15 | 2 | 2 → 0 | **0** | **11 %** | 2 |
| lab | enfgo-carfree-runkite | win, FAIL | 4:39.2 | 15 | 2 | 2 → 0 | 0 | 11 % | 2 |
| lab | focus-enfgo-carfree-kite (walked steps) | win, heavy | 4:55.3 | 17 | 2 | 2 → 0 | 1 | 27 % | 5 |
| lab, kite off | enfgo-carfree-runkite | win, FAIL | 4:34.2 | 13 | 2 | 2 → 0 | 0 | 11 % | 2 |
| today | matrix L3 | fail, sealed | 5:05 | 26 | 2 | 2 → −2 | 3 | 66 % | 12 |
| **today** | **enfgo-carfree-runkite** | **win, FAIL** | **4:45.0** | 18 | 2 | 2 → 0 | **0** | **0 %** | **0** |

**What happens** (lab log):
1. **2:34–2:55:** the Tech drives the pre-fight rivals back from 16–17 m, unanswered (R4).
2. **3:07:** the response unit answers 5 ticks after the Tech's first shot. The Tech runs (2.8 m/s), and they never close on him: both hits he takes come later, when he trades with security-1 at 14 m.
3. **3:14:** the final fight starts. officer-1 and officer-2 are alarmed 83 m out and spend their 20 m chase on the walk in (R2). They stop 54 m away, then stand 18–20 m from the cell to the end, **without firing a shot**.
4. **4:02–4:36:** everyone boards and the car leaves. Both officers are alive, but neither holds the car (the run logs no `car.free` line, because nothing ever held it).

**Under `today`** the Tech kills security-1 from 18–23 m: the today pistol's band is 15–25 m. F2 exists in today too.

**Why** (two layers):
- **The getaway is never contested.** Final-fight officers who hold a stand don't close on foes out of reach unless they are shot, and they don't count as holding the car from 18 m. The kite rule only fires on a shot, and the cheap line doesn't need to shoot them.
- **Running beats the chase.** The rulebook says (`op-run-speed-cm-s` reason): "Faster than the response unit's 2.2 m/s [B], and that's fine: the police's threat is cones, guns and the seal, never a foot race". Running is free in a fight. Pulse's "no infinite kite" limit 1 assumes a walking kiter, so the two rules conflict.

**Probe:** `lab-kite-chase-cm` 30 m or more (also tried 40, 60 and 1000 m) makes both run-kite rows fail `enforcer-down` (55 %, 2 down). Matrix L3, P-A, P-G and P-B are unchanged (`evidence/retest-probes.txt`). A bigger budget closes this sample, but R2's stall and the uncontested car are the real causes.

**Owners:**
- **Pulse:** armed final-fight bodies whose foes are out of reach contest the car, or close to their ring, without needing to be shot.
- **Motive:** pick between running staying free in a fight and the chase being a foot race; decide whether the car's hold should reach officers who can see it.
- **Keel:** add these rows (R5).

### R2 · The chase budget is spent, then held (high)
**Script:** the R1 lab script (trace below), and Keel's own row, `retest-scripts/L3.kite.lab.script.json`.

**Trace** (`TRACE=officer-1,officer-2,security-2`; `KiteWalked` in cm):
- **The walk in uses up the budget.** officer-1 and officer-2 are alarmed at 3:15.9 by a shot at security-1, while they are still 83 m out. Their walk in counts toward the budget: 1944 cm by 3:25, **1999 cm by 3:26**.
- **With 1 cm left, they freeze.** `KitePath` cuts the route to the remaining budget, so it is shorter than one grid cell and returns null. Nothing moves, and the body is never capped.
  - officer-1 and officer-2 stand **54 m out from 3:26 to 3:43**.
  - security-2 stands at 16.4 m with 1999 cm, unable to answer, while the Tech shoots it from 20 to 10 health.
  - officer-2 is never capped, and holds 19.4 m away to the end.
- **Capped is for good.** officer-1 caps at 4:01 (2001 cm) and holds cover 17.7 m from the Scout. `KiteCapped` never resets, so an officer once capped never comes back into its ring.
- **In Keel's own `L3:kite` row** (matrix-final):
  - 4:09.9: "officer-1/officer-2 has walked its 20 m: it takes cover";
  - then security-2, officer-2 and officer-1 are downed from 16–17 m, 4:10–4:28, **with no return fire**.
  - It fails sealed only because the scripted Enforcer stands still with Vale (R5).

**Owners:**
- **Pulse:** count only the walking the kite itself orders, not the body's own walk in or its fight AI; consider refilling the budget per alarm; a capped body whose shooter stays put should still close.
- **Keel:** treat a remainder shorter than a grid cell as spent (cap it), and add a scenario for it. N156 covers the cap, but not the walk-in drain or the stall.

### R3 · brief-013 police units are free at the band (medium)
**Script:** `retest-scripts/band.police-1.lab.script.json`.
- It is the R1 run's orders up to 3:49.0. Then the Fixer walks to a point 16–17.5 m from police-1 with a clear line, and fires.
- **Result:** police-1 is down at 4:24.3 after 12 shots at 250 ‰, standing still. It fires no shot and doesn't move, and the Fixer keeps 40/40.
- This is Pulse's open issue 1, now measured. `KiteSide("police-1")` is 0. Robots share `UnitsCombat`; not measured separately.

**Owner:** Pulse (a third kind in the kite overlay), then Keel.

### R4 · The pre-fight rival crew (medium)
**Script:** the R1 lab script, 2:34–2:55 (trace in the evidence).
- The Tech fires at 15.9–18.5 m for 20 s. The crew never fires (its ring is 15 m).
- **The first answer comes 68–71 ticks after the first shot, not 5.** `KiteHide` found no cover and no blind point within 8 m, so the crew stood still (Pulse's open gap). It moved only once its path brought one into reach.
- After `kite.hide` it leans out again into the same line, takes the third hit (2:55.4) and retreats for 175 s, which is the rest of the mission.
- **Cost:** `hit-trail` (2 T) once. `Damage()` restores health and leaves `Hit` set, so the next drive-back costs 0 T.

**Owners:**
- **Pulse:** a fallback when there is no hide, such as advancing to its ring or leaving the line by its path; and not leaning out into a shooter who is still there.
- **Motive:** whether the drive-back should cost `down-trail`, or once per drive-back.

### R5 · The matrix is still not an upper bound (high, process)
The adversarial rows lose to their own script, not to the city:
1. **The Enforcer stands still while Vale is with him** and foes are present. The `SEngage` branch stops his walk.
2. **`SFightItOut` waits for `FightOverTick`.** That is stricter than the matrix L3's `!CarHeld()`, and the player is never forced to wait for it.

Fix both and `L3:focus-enfgo-carfree-kite` already wins heavy at 27 %; with runs, it is R1.

§6.6.2's "free downs" check passes R1 (1/1), because the cheap line downs nobody it doesn't have to. The verdict needs a line about contact, for example the hostiles' shots at the cell and the officers alive and out of reach at Go.

**Keel:**
- add rows `L3:enfgo-carfree-runkite`, `L3:focus-enfgo-carfree-runkite` and `L3:enfgo-carfree-kite`, for both presets (`probe/retest/harness-policies.diff`: flags `enfgo`, `carfree`, `runkite`, `units`, `rawreset`; all off by default, so the existing rows are unchanged);
- reset `Scenarios.Kites` and `Resets` for matrix rows (they carry over from the last adversarial run; cosmetic).

### R6 · Using cover loses P-G (medium)
**Script:** `retest-scripts/P-G.lab-cover.script.json`.

Keel's `lab-cover` preset (the player seeks the marks, snap on) **fails rivals-reach-vale at 3:49.5: 55 % health lost, 2 down, 10 hits.** Plain lab P-G passes at 22 %. The row is in `matrix-final.txt`, but §8 doesn't mention it.

Motive kept the snap because "kneeling at a mark is a choice the player sees coming". As things stand, that choice loses the prepared run.

**Owners:** Motive (rule) and Keel (diagnosis). Vantage, through Aster, for cover at the car posts (already offered as an option in tuning.md §6.5).

### R7 · Target mode charge binds only the count (low)
In Keel's volley micro: 16 volleys paid, then **30 shots and 22 hits**. After each volley the agent goes back to "at will" with its aim still on the target, and keeps firing for free. That matches `host-interface` ("the aim stays on the target"). So the bar limits a player only when they want an exact shot count.

**Owner:** Motive, to confirm that's intended.

## 3. Handoffs (inputs · done-check · next owner)

1. **Pulse.**
   - **Inputs:** R1 (contest the car / close on foes out of reach without being shot), R2 (budget counting, refill, capped bodies), R3 (units), R4 (crew fallback, leaning out).
   - **Done-check:** both R1 scripts' policies fail or are heavy under the lab; the R2 trace shows no body frozen with a sub-cell remainder; the R3 band micro draws return fire or movement within 1.0 s; P-A and P-B ≤ 25 %, P-G ≤ 33 % with no op down.
   - **Next owner:** Keel.
2. **Motive.**
   - **Inputs:** running against the chase (R1), the car's hold reach (R1), the drive-back cost (R4), cover vs P-G (R6), R7.
   - **Done-check:** rulings written in tuning.md.
   - **Next owner:** Keel.
3. **Keel.**
   - **Inputs:** the R2 stall bug, the R5 rows and verdict line, R6's diagnosis.
   - **Done-check:** `bun run sim fight-lab` prints the R5 rows for both presets; `dotnet test` stays green; `today` hashes are identical.
   - **Next owner:** Suture, to retest.
4. **Aster** (FYI): R1 holds under `today` too. The v0.24 loud route is cheaper than the matrix said; that matters for the GDD and for Sky's question, not only for the lab.

## 4. Evidence and how to repeat

- **Lab matrix rerun:** `dotnet <scratch cli> fight-lab --presets today,lab --rulebook out/suture027/rulebook.snapshot3.json` (so nothing is written). Output in `evidence/retest-matrix.txt`; the lab rows are the same as `out/keel027/matrix-final.txt`, and every run replays identically.
- **Probes:** `evidence/retest-probes.txt` covers both presets, kite off, the chase-budget probes (30, 40, 60 and 1000 m; scratch rulebooks `out/suture027/rb3-*.json`), the traces and the band micro. 0 replay differences.
- **Repeat:**
  1. Copy `sim/Suborned.Sim` and `sim/cli` to a scratch folder.
  2. Apply `probe/retest/harness-policies.diff` (the scripted player only).
  3. Put `probe/retest/Program.cs` and `probe.csproj` beside it, and `dotnet build -c Release`.
  4. From the repo root: `RUNS=L3:focus-enfgo-carfree-runkite PRESET=lab dotnet <probe.dll> contracts/rules/rulebook.json`.
     - `TRACEID=<run> TRACE=officer-1,officer-2 FROM=<tick>` prints the kite state;
     - `BANDID=<run> BAND=police-1@2290@fixer` runs the band micro;
     - `KITE=0`, `SNAP=0`, `COVER=1` and `SCRIPTS=<dir>` are the other switches.
- **Scripts:** all 9 in `retest-scripts/` replay identically with the scratch CLI's `play`. The R1 lab script also replays identically with `bun run sim play` on the real tree (final `c725c5a99667e4bb`).
- **Old scripts:** the earlier `scripts/` were recorded open-loop on inputs `d001d608…`. They still replay, but their orders no longer fit the new rules (`focus-reset-kite.L3` now ends with 3 ops captured). Use the closed-loop rows and `retest-scripts/` instead.
