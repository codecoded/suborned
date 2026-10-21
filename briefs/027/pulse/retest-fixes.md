# Brief 027 · Retest fixes R1–R4 (Pulse)

2026-10-20. Answers `briefs/027/suture/retest.md`. Nothing in `sim/`, `contracts/` or `unity/` was edited. I prototyped in a scratch copy of `sim/` (Suture's `harness-policies.diff` on top, env-var tunables), measured with Suture's probe, then deleted it. The patch is kept as a reference only: `retest-fixes.prototype.diff` (not a patch to apply; env reads become rulebook reads). Motive's §7 in `motive/tuning.md` did not exist when I wrote this; items for him are marked **Motive**.

All numbers below are the lab preset unless a row says `today`. Rows are Suture's. Every run replayed identically.

## 0. Short answer

R1 has two causes and each needs its own fix.

1. **The chase rule was broken** (R2): the 20 m was spent on the walk in, then the officers froze. Fixing the budget alone turns Suture's two lab run-kite rows from "win, 11 %" into **heavy wins (55 %, 2 down, 2 captured)**. Lab only.
2. **Nothing contests the getaway** (the real R1): the loud officers' stands sit 17.7 m and 19.4 m from the car, outside the pistol's 15 m effective ring. An officer who isn't shot and sees no foe in reach holds a stand that can't reach the car. Pull **one** officer's stand into the car's ring and the same rows **fail `enforcer-down` (33–44 %)**, matrix L3 stays at 44 % heavy, and every prepared row is identical. This also closes the cheap line on `today` (see §3), which is a real game change and needs Sky's OK.

| row (lab) | now | budget fixes + R4 fallback | + one officer contests the car (+ rearm) |
|---|---|---|---|
| L3 matrix | win heavy 44 %, 2 down | same | same |
| L3:focus-enfgo-carfree-runkite | **win 11 %, 0 down (FAIL)** | **win 55 %, 2 down, 2 captured** | fail enforcer-down 33 % (rearm: win 55 %, 2 down 1 captured) |
| L3:enfgo-carfree-runkite | **win 11 % (FAIL)** | **win 55 %, 2 down, 2 captured** | fail enforcer-down 33 % (rearm: fail 88 %) |
| L3:enfgo-carfree-kite | win heavy 44 % | fail enforcer-down 55 % | fail enforcer-down 61 % |
| L3:kite / reset / focus-reset / cycle / focus-rawreset | fail sealed | fail sealed | fail sealed |
| L3:carbine-kite | (not run before) | | fail enforcer-down 33 % |
| P-A / P-G / P-B | 0 / 22 / 16 %, 0 down | identical | identical |

`today` (one officer contests, `CONTESTTODAY` in the scratch): matrix L3 unchanged (sealed, 66 %); `enfgo-carfree-runkite` 4:45, **0 hits, win** → **fail enforcer-down 33 %**; P-A, P-G, P-B unchanged (the contest only touches the loud branch).

## 1. R2 first: the chase budget (the rule I got wrong)

My rule counted every metre a body walked after the alarm, including its walk in from the street entry. Three fixes, all measured.

**1a. Budget from first contact, not from the alarm.** New value `lab-kite-contact-cm` = **3000** (30 m, [S]; the sidearm's 18 m max plus a 12 m margin).
- A body's walk while the shooter is farther than 30 m is free and does not count.
- Once within 30 m of the shooter, every metre counts against the 20 m.
- The route is cut at `remaining + free`, where `free = max(0, dist(body, shooter) - contact)`. The 10-tick re-plan recomputes it, so the body walks in at full pace and the budget starts at the door.
- Check (trace of the R1 lab row): officers alarmed 78 m out walk in free; `walked` stays 0 until 3:35 (29.7 m), reaches 2087 cm at 3:45, caps, and holds cover. Before: 1999 cm by 3:26 and frozen 54 m out.

**1b. A remainder shorter than a grid cell is spent (Keel's stall bug).** In `KiteBody`, `if (rem <= 0 || (rem < S.Grid.Cell && free == 0))` goes to the cap branch. `KitePath` returns null for a cut shorter than one cell, which today is a silent freeze. A null path from "no route" (not from the cut) should hold without capping.

**1c. A capped body re-arms.** New value `lab-kite-rearm-ticks` = **80** (8 s, [S]). A capped body whose alarm is still open (shots still landing, not lapsed) after 80 ticks gets a fresh budget and closes again. This answers "once capped, never back in range". Optional, but I recommend it; the reasoning against infinite kiting still holds because:
- a re-arm needs the shooter to keep firing at a body it can't kill quickly (a pip-1 shot is 250 ‰; a kill is 12 s for one agent against a 2:25 seal);
- the side moves together;
- measured: rearm 80 makes the adversarial rows heavier, not cheaper (kite rows fail sealed 66 %, runkite rows 55–88 %).

Budget stays **per run**, as before; re-arm is the only refill. (Per-alarm refill, tried as `KALARM`, wasn't needed.)

## 2. R1: the officers' job is the car

**What happens today** (verified in the data, not only in the log): from `d:Getaway bay` (850, −4820 cm) `police-1` is 17.7 m, `police-2` 19.4 m, `police-3` 12.2 m, `police-4` 18.6 m. The loud fight takes the first n stands in order (two officers in L3), so both are outside the ring. The pistol's ring is 15 m (max 18). `fight.hold` (8 m) was chosen in brief 018 so "the car is held by those who come for it". Nobody comes for it: an officer walks to its stand, then holds. A cell that never walks into reach is never fired on, and the car is never held.

**The rule (proposed).** In a loud final fight the first `lab-police-contest-n` officers (**1**, [S]) take, as their stand, the first point on the route from their stand to the car that is within `ai.stopReach` (90 %) of their weapon's effective ring of the car. That is the existing `StopPoint(stand, Car, eff)`: about 4 m of walk for `police-1` (17.7 → 13.5 m). The officer then holds, engages in reach, picks targets and uses cover exactly as a holder does today. Nothing else changes: the other officers and the joined response unit keep their stands.

- **Fits the fight AI.** "Hostiles advance to cover points made at build time, engage in effective reach": this moves one of those points to where the officer's job (the car) is in reach. It is not a chase and not a new state.
- **No foot race.** The rule is positional. It does not care whether the cell runs; the Tech running at 2.8 m/s past a 2.2 m/s officer still gets away from the officer, as the rulebook says, but cannot get Vale into the car unopposed. So the rulebook's "never a foot race" sentence stays true, and I would not change `op-run-speed-cm-s`.
- **Why one, not all.** All officers pulled in (tried): matrix L3 goes from win heavy 44 % to fail enforcer-down 77 %; the loud route stops being winnable by the matrix player. One officer keeps matrix L3 at 44 % and closes both cheap rows. Two is a Motive tuning call (`today` N=2 gave the same cheap-row result as N=1).
- **Joined response unit:** left alone (an all-joined variant was too lethal: 77 %).

**Alternative with the same effect, data not rule:** move `police-1` (and `police-2`) to within 13.5 m of `d:Getaway bay` (850, −4820 cm) in `handover.site.json`, with cover from `civic-block.cover.json`. Waymark/Vantage own it. It changes today's game the same way and costs nothing in the engine. I prefer the rule: it needs no new placement and works for any mission's stands.

**Who decides:** **Motive**, on whether a loud route that fails `enforcer-down` for a player who never fights the officers is the intended price (I think yes: "being out of reach is not safe" and "no loud win under 25 %" both say so).

## 3. Lab-only versus a change to today's game

| change | lab only? | needs Sky's OK? |
|---|---|---|
| 1a contact budget, 1b sub-cell cap, 1c re-arm | yes (the whole kite overlay is `fight.lab.kite`) | no |
| R4 fallback (§5), R3 police units (§4) | yes | no |
| §2 contest, `lab-police-contest-n` | built lab-gated first (`Lab && LabContest`; `today` hashes identical) | **yes, to promote it to `today`** |

Why §2 needs Sky: on `today` the cheap loud route is also free (Suture: 4:45, 0 hits, 0 % health). The kite is lab-only, but the uncontested car is not. Promote by adding `contest` to the base `fight.loud` rule: a `today` change in the loud branch, which moves `today` hashes for every loud run and shifts the loud stands. Measured on `today` with N=1: the cheap run becomes fail enforcer-down 33 %; matrix L3 and P-A/P-G/P-B are identical. It also moves what the GDD calls "police hold"; I read "hold" as holding the line to the car, but Sky may read it as holding a stand. That is the question for Sky, via Aster. Until then: lab-gated, with `today` byte-identical.

## 4. R3: the brief-013 police units

Add a third kind to the kite overlay: **police units** (`Units` of kind police: the response `police-n`).
- **Alarm:** a shot landing on a police unit marks that unit and every living police unit sharing its `Act` (the same dispatch). One incident, one dispatch.
- **Answer:** the same as the response unit: 5 ticks, then close to `StopPoint(pos, shooter, eff)` on the grid, re-plan every `kite.replan`, budget and contact as in §1. Needs a unit displacement, `unitDisp[u.Id]` (a `KiteDisp`, read by `UnitPose` and `Moving` ahead of the unit's own path while the unit is on the street), hashed in key order.
- **Cannot answer:** same `KiteCanAnswer` against `UnitSees` (the unit's cone), so a unit already trading fire is left alone.
- **Robots:** not covered. They are clockwork, on routes (GDD §01); a robot shot from the band is a fault on its own wire, not a decision. A robot downed raises the report through its existing `robotWire`. **Motive**: should a downed robot cost Trail/Standing? If not, a lab player can clear robots at the band for free. I'd say a downed robot is a found fault (Alert) and no kite.
- **Done-check:** Suture's `band.police-1.lab` micro: police-1 shot from 17.5 m must draw return fire or movement within **1.0 s** (5 ticks reaction, arithmetic); not measured here (no prototype).

## 5. R4: the pre-fight rival crew

Measured with the scratch fallback:
- **No hide in reach → advance to the ring.** If `KiteHide` returns null (no cover within `ai.seek.rival`, no blind point within 8 m), use `StopPoint(pos, shooter, eff)` as the destination, as the police do. Result: the first answer moves from **68–71 ticks to 5 ticks** after the first shot (log: `rivals answer tech: first shot at 2:34.3, move at 2:34.8 (5 ticks)`). The same fallback applies to the ambush `rival-n`.
- **It is not enough on its own.** The crew walked 3 m to 13.3 m from the Tech, but the Tech's kite steps (4 m back after each shot) keep the band at 16 m, and the crew's displacement is one trip: it never re-plans, so it still takes three hits at 2:38.7, 2:42.7, 2:46.7 and retreats for good. The police re-plan every 1 s; the crew must too.

**For Keel (not prototyped):**
1. **Re-plan while holding.** While `rivalsDisp.Phase == 1` and an operative with a weapon out is beyond the crew's ring but inside its max (the band) and still shooting, extend the displacement: the new path is the old polyline up to the crew's pose, then the route to the new `StopPoint`. Same chase budget and contact rule as §1 (the crew has its own `KiteWalked`). The detour length for the arrival clock is the whole polyline, so the crew returns the way it came.
2. **Do not lean out into a shooter who is still there.** Phase 1 → 2 needs the `lab-kite-hide-ticks` of quiet **and** no operative with a weapon out within the crew's max reach of the path start (`d.P.At(0)`) on a line to it. Otherwise it holds; a hold longer than 150 ticks ([S], `lab-kite-hide-max-ticks`) converts to the advance in item 1.
3. **Motive: drive-back cost.** `Damage()` restores health and leaves `Hit` set, so the second drive-back costs 0 T. My view: charge `hit-trail` once per drive-back (reset `Hit` on retreat), not `down-trail`: the crew is not downed, it is driven off, and the pickup should remain cheaper than the loud route.

## 6. For Keel

### 6.1 Rulebook (`fight.lab.kite`, every value `proposed: true`, reasons start "EXPERIMENTAL: brief 027 retest (Pulse retest-fixes.md)")
| key | value | meaning |
|---|---|---|
| `lab-kite-contact-cm` | 3000 | chase budget starts when the body is this near the shooter |
| `lab-kite-rearm-ticks` | 80 | a capped body with the alarm still open gets a fresh budget |
| `lab-kite-hide-max-ticks` | 150 | the pre-fight crew's hold becomes an advance (§5.2) |
| `lab-police-contest-n` | 1 | loud-fight officers whose stand is pulled to the car's ring (0 = off) |

`Rules.cs` line 650 area reads these; `rulebook.schema.ts`, `rulebook.ref.ts` and the lab test get them. Unknown = reject as for the other lab keys. The contest key lives in `fight.lab` (not `kite`) because it is not the kite.

### 6.2 Engine
The prototype diff shows the shape (`GameKite.cs`, `GameFight.cs`, `GameAi.cs`, `Model.cs`).
- **`KiteBody`:** count `KiteWalked` only while `G.Dist(pos, shooter) <= contact`; `free` as in §1a; the `rem < Grid.Cell` cap (§1b, with a scenario: a body with 1 cm left must be capped and take cover, not freeze; N156 covers the cap but not this); `KitePath(..., rem + free)`; re-arm: a new `Gun.KiteCapTick` (hash it with the other kite fields), `if (g.KiteCapped && Tick - g.KiteCapTick >= rearm) { g.KiteCapped = false; g.KiteWalked = 0; }`.
- **`StartFight` (loud):** for `i < LoudContestN`, `home = StopPoint(stand, Car, gun.W.EffCm)`; the officer's route runs from the entry to `home`, and `At`/`Home` are `home`. Nothing else in `FightBody` changes (the hold and engage-lapse branches already return to `Home`).
- **KiteRivals / KiteBody (rival branch):** `dest ??= StopPoint(pos, shooter, eff)` when `KiteHide` is null (and `sp2 != pos`); §5.1–5.2 for the path-driven crew.
- **Police units:** §4.
- `hashes --start-text BASE`: 229 of 229 identical with the lab off (all of this reads `Lab`/`LabKite`/`LabContest`).

### 6.3 Scenario checks (after N156)
- **N157** contact budget: officer alarmed 80 m out reaches the shooter's 30 m with the full 20 m left (`KiteWalked` ≈ 0 at that point); it then walks ≤ 20 m + slack.
- **N158** sub-cell remainder: a body at `rem` = 1 cm caps at once and logs it; no body stands uncapped with `KiteWalked ≥ budget - Grid.Cell`.
- **N159** re-arm: a capped body, shooter still firing for 80 ticks, closes again; the walk total ≤ 2 × 20 m; after the alarm lapses it walks home.
- **N160** contest: loud fight, N=1, officer-1's `Home` is within `stopReach` of the car's ring, and it fires at an operative at the car; officer-2's stand is unchanged. `contest-n` 0: the old stands.
- **N161** police unit: R3 micro, return fire or movement ≤ 1.0 s.
- **N162** pre-fight crew: no hide → answer in 5 ticks; re-plan while the band holds; no lean-out while a shooter holds the path start.
- **`fight-lab` rows** (both presets): `L3:enfgo-carfree-runkite`, `L3:focus-enfgo-carfree-runkite`, `L3:enfgo-carfree-kite` (Suture's `harness-policies.diff`), each "fail" or a heavy debrief; verdict lines for R5: hostiles' hits on the cell and officers alive and out of reach at Go.
- **Done-check:** `bun run sim fight-lab` shows the table in §0; P-A/P-B ≤ 25 %, P-G ≤ 33 %, no op down; `dotnet test` green; `bun run typecheck`.

## 7. Open, not mine

- **R5, R6, R7:** Keel/Motive as in the retest.
- **Walking vs running (Motive):** I'd keep running free in a fight. The rows show a runner stays safe from the chase but cannot take the car for free once §2 stands.
- **`L3:enfgo-carfree-kite`** now fails `enforcer-down` (55–61 %) where it was a 44 % heavy win: a walking kiter loses the Enforcer to the contesting officer. This is the intended price, but Motive should confirm that a failed (not "heavy win") result is acceptable for that row.
- **The cover on the contest point.** I used a plain `StopPoint` (no cover). `police-1`'s stand is a café wall cover point; a pulled-in officer stands in the open and is hit at full rate. If Motive wants it softer, the officer takes the best cover within `ai.seek.police` of the new point (the join logic already does this). Not measured.
