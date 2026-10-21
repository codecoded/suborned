# Brief 027 · F2, the stand-off band: the hostiles answer (Pulse)

2026-10-20. Answers Suture's F2 (`briefs/027/suture/balance.md`) and Motive's §6.6 (`briefs/027/motive/tuning.md`). Lab only (`fight.lab`, EXPERIMENTAL); `today` stays byte-identical. Nothing here is in the real C#; Keel implements. Every number is [S], starting value.

## 1. The rule, in one line

A shot at any armed hostile, hit or miss, from a shooter it cannot answer, makes that hostile's whole side move 0.5 s later. Police and the response unit close to their effective ring (their job). Rivals go to cover out of the shooter's line. Nobody stands still and takes it.

## 2. The rule

**Who.** Armed hostile bodies, three kinds, two sides:
- **Police side:** the response unit (`security-1`, `security-2`, before or after they join), the final-fight officers (`officer-n`), and a joined response unit.
- **Rival side:** the ambush crew (`rival-n`) and the pre-fight crew (`rivals`).
- **Not covered:** the door guard (a door is one person's post), the patrol (it already chases the shot's origin), civilians, and the brief-013 response `Units` (police-n, robots; see §6 open issue 1).

**Trigger (the alarm).** In `Resolve()`, when an operative's shot lands on a body above (hit or miss), every living body on the target's side records the shooter and the tick (`KiteAt` at the first, `KiteSeen` at the latest). One incident, one dispatch: the side moves together, so the second officer of a pair never waits for his own first shot. This is what stops "one agent downs every hostile, one after another".

**Cannot answer.** At `KiteAt + 5` ticks a body acts only if no operative with a weapon out (drawn or has fired) is inside its effective ring on a line it sees. A body already trading fire with anyone keeps fighting its own fight; the sniper is not a reason to drop it. (Without this exemption P-B's rivals left their cover for a sniper and the matrix gained 39 points of health lost: measured.)

**Reaction time: 5 ticks (0.5 s).** The body begins to move 5 ticks after the shot lands, so inside Motive's 1.0 s with 0.5 s to spare. (3 and 8 ticks were swept: no row changes class.)

**Police side: close in.**
1. Pick the first point on the grid route (Dijkstra, existing `Grid.Route`) from where it stands to the shooter that is within `ai.stopReach` (90 %) of its effective ring, so 13.5 m for the lab sidearm. This is today's `StopPoint`, reused.
2. Walk it at the unit's pace (response unit and officers: `security.speed`, 2.2 m/s).
3. Re-plan to the shooter's current position every 10 ticks (1.0 s), so a step back does not shake it off.
4. It stops the moment the shooter is inside its effective ring on a line (the existing `advance` -> `engage` transition) and fires as it does today (aimNew, then its pips). While it walks it fires nothing in the final fight and fires at walking pips pre-fight, exactly as today.
5. **Chase budget: 20 m of walk per body for the run** (`lab-kite-chase-cm` 2000), counted as distance actually moved. A spent budget means: take the nearest cover that shields it from the shooter within `ai.seek.police` of where it stands, and hold. It does not walk home until the alarm lapses.
6. **Lapse:** 100 ticks (10 s) with no shot on its side clears the alarm. It then does what it does today (holders walk home).

**Rival side: out of the line.** Same trigger and delay.
- *Ambush crew (`rival-n`):* take the cover point the data give that shields it from the shooter (`SelectCover`, `ai.seek.rival`, one try per `cover.stepGap`), else the nearest free cell within 8 m with no sight line to the shooter. Existing `TakeCover` / `MoveTo`. They fight from there as today. They do not advance on a sniper.
- *Pre-fight crew (`rivals`, path-driven):* walk out to that same point, hold, and walk back when no shot has landed on the crew for 30 ticks (`lab-kite-hide-ticks`). The crew's own arrival clock pauses for the whole detour (the hide shifts the path start by its length), so it resumes where it left off and arrives late by the detour, never early.

**Why police advance and rivals hide.** The police's job is to reach the scene; they hold a ring and close it. The rivals' job is the car; they are not paid to run at a rifle. Both answers cost the shooter: the first removes the free band, the second removes the line.

**No infinite kite.** Four limits, all measured or arithmetic:
1. The re-plan chases a step back (4 m every shot) at 2.2 m/s against a walker; a stationary shooter loses 2.2 m of band a second to a 3 m band, so the gap closes inside 1.4 s.
2. The 20 m budget is per body, per run, not per alarm. It does not refill on lapse.
3. Past the budget the answer is cover, which costs the cell a pip (`LabPips`), so a trained shooter drops to 1 pip (250 per mille): a kill is 12 shots, 12 s for one agent, against a seal clock of 2:25.
4. The side moves together: every extra kill brings the rest of the side onto the shooter.

## 3. How it reads to the player (Sky: readable over rich; show it in the street)

No new text. Four street reads, all existing visual language:
- **The turn:** at +0.5 s every body on that side turns to face the shooter (its cone swings; the cones are the Scout's skill). This is the "they know" beat, and it is readable at 40-60 px.
- **The run:** police break into the unit run pose toward the shooter. The destination is drawn as the move ring on the ground, a thin line from the body to its stop point (the same ring the cell's own move orders use). The player sees where the officer will stop before he arrives.
- **The duck:** rivals drop to the cover pose at a cover point; the point is marked as the cell's cover points already are. The pre-fight crew visibly steps behind the nearest planter or wall and leans out again after 3 s.
- **The chip:** aiming at a body beyond its ring shows a forecast, not a sentence: the stop point as a ghost ring and "reaches you in n s" (police) or the cover point (rivals). The price chip stays one line. The forecast needs one read-only call, `PreviewAnswer(target)` returning `{side, stopPoint | coverPoint, ticksToRing}` (host-interface addition for Keel and Latch; Glyph draws it).

Nothing here owns a colour; the cues are shape and motion.

## 4. Measured (prototype in a scratch copy; deleted)

Scratch copy of `sim/` at 2026-10-20 ~15:30 plus Suture's `fix-prototypes.diff` fix 2 (re-aim of the same target keeps its accumulator) and `acc-start` 0, as the stand-in for Motive's §6.1. It is NOT Motive's full §6.1-6.5: §6.3 charge stamping and §6.4 warning rules were not in the tree. Keel must re-measure on the real tree. Rows are Suture's `SEngage` policies from `briefs/027/suture/probe/`, unchanged. Every run replayed identically.

**Config B (aim memory + acc 0), response OFF -> ON:**

| row | OFF | ON |
|---|---|---|
| L3 matrix | win 4:50, 33 %, 1 down (heavy) | win 4:54, 44 %, 2 down (heavy) |
| L3:kite | fail, sealed, 22 % | fail, sealed, 22 % |
| L3:reset-kite | fail, sealed, 22 % | fail, sealed, 22 % |
| L3:focus-reset-kite | fail, sealed, 22 % | fail, sealed, 38 % |
| L3:cycle-kite | fail, sealed, 22 % | fail, sealed, 22 % |
| P-A matrix | 0 % | 0 % |
| P-G matrix | 27 %, 0 down (fails 25 % today) | **16 %**, 0 down |
| P-B matrix | 16 % | **11 %** |

**Config A (the tree as it stands: acc 500, no aim memory), response ON** (the case that matters, because it is where F2 wins at 0-5 %):

| row | before (Suture) | with the response |
|---|---|---|
| L3:kite | fail, sealed | fail, sealed, 38 %, 1 down |
| L3:reset-kite | **win, 5 %** | **fail, sealed**, 22 %, 1 down |
| L3:focus-reset-kite | **win, 0 %** | **fail, sealed**, 22 %, 1 down |
| L3:cycle-kite | **win, 5 %** | **fail, sealed**, 22 %, 1 down |
| L3 matrix | win, 33 %, 1 down | win, 44 %, 2 down (heavy) |
| P-A / P-G / P-B matrix | 0 % / 27 % / 16 % | 0 % / 27 % / 16 %, 0 down, Trail, Alert, Standing as now |

So the response fixes F2 on its own, without relying on Motive's §6.1 (Suture's note that fix 2 alone also seals those rows is independent confirmation, not a dependency). Where the two overlap, the response adds the second wall: the outranged officers come to the shooter instead of waiting to be shot.

**Reaction check (item 2):** the fight log carries `ai.kite` lines: `security-1 answers tech: first shot at 1836, moves at 1841 (5 ticks), 15 m out`. In the L3:kite log, `security-1` and `security-2` return fire (14 m, 600) 3.6 s after the first shot at them, and the Tech is the first of the cell to go down. No cell agent downs a second armed hostile before the side has fired.

**Item 3 (prepared rows):** P-A 0 %, P-G 27 % (A) or 16 % (B), P-B 16 % (A) or 11 % (B). All inside P-A/P-B <= 25 %, P-G <= 33 %, no op down.

**Item 5, the carbine (crude, one row):** the Enforcer alone, carbine, kiting every hostile at 19.5 m or 29.5 m, others holstered (`CarbineKite` in my scratch copy). Response OFF: free kills at 15-17 m (the rivals, security-1, security-2, 600 per mille, 0 hits taken until the police units arrive). Response ON: security-1 falls unanswered in 1.6 s (allowed: one kill), then security-2 reaches 14 m and lands 4 hits on him; the arriving police units finish him at 3:51 (L3 fails `enforcer-down`, 33 % lost). So the carbine's 15-20 m band is not free with the response. Judge it again with Keel's real `L3:carbine-kite` row. The carbine fallback (eff 15, max 22) is NOT needed on this evidence.

**Sensitivity** (config B, ON): chase budget 10/20/30 m, reaction 3/5/8 ticks, stop 80/90/100 %, re-plan 5/10/20 ticks, hide 3 s (default) and 6 s. Every L3 adversarial row stayed "fail, sealed" and every prepared matrix row identical in every setting. Only the stop at 100 % moved a row (L3 matrix 66 % health lost), which is why I keep 90 %.

## 5. Check against Motive's §6.6

| item | status |
|---|---|
| 1. no cheap loud win on the four kite rows | pass on both configs: all fail, sealed. `L3:carbine-kite`: measured crude only, see item 5 |
| 2. respond within 1.0 s | by construction 0.5 s; `ai.kite` log lines show 5 ticks |
| 2. one free kill at most | holds in the L3 logs: the second target is already answering |
| 3. P-A/P-B <= 25 %, P-G <= 33 %, no op down | pass; P-G 27 % (A), 16 % (B) |
| 4. deterministic, lab-only, `today` identical | everything gated on `Lab` and `Start.fightLab.kite`; all state is in the hash |
| 5. the carbine band | not free (above); remeasure on the real row |

## 6. For Keel

### 6.1 Rulebook (section `fight.lab`, every value `proposed: true`, reason starts "EXPERIMENTAL: brief 027, F2 (Pulse briefs/027/pulse/kite-response.md)")
New object `fight.lab.kite` and `Start.fightLab.kite` (bool, default the rulebook's `on`; lab preset `true`, today absent):

| key | value | meaning |
|---|---|---|
| `lab-kite-on` | 1 | the switch's default |
| `lab-kite-react-ticks` | 5 | shot lands to first step |
| `lab-kite-replan-ticks` | 10 | police re-plan to the shooter |
| `lab-kite-chase-cm` | 2000 | walk budget per body per run |
| `lab-kite-lapse-ticks` | 100 | no shot on the side: alarm clears |
| `lab-kite-hide-ticks` | 30 | pre-fight crew leans out after no shot for this long |
| (reuse) `ai.stopReach` | 900 | police stop point (no new key) |
| (reuse) `ai.seek.rival`, `ai.seek.police`, `cover.stepGap` | | existing |

Add to `rulebook.schema.ts`, `rulebook.ref.ts` and the lab test; read through `Rules.Lab`. Unknown = reject as for the other lab keys.

### 6.2 Engine (a new `GameKite.cs`, plus hooks)
My scratch implementation is saved as a reference, not a patch: `briefs/027/pulse/kite-response.prototype.cs.txt` (about 150 lines, env-var tunables to replace with rulebook reads, ad hoc `KE()` removed). Its shape:
- **`Gun`** gains `KiteAt` (-1), `KiteSeen`, `KiteLast` (-1), `KiteWalked`, `KiteBy`, `KitePrev`, `KiteCapped`. Hash them with the lab on (in `HashLab`), after `Then`.
- **`Resolve()`**, right after `tg.ShotBy = s.By; tg.ShotByTick = Tick;`: `if (Lab && LabKite && IsOpId(s.By)) KiteAlarm(s.By, s.T);`. `KiteAlarm` ignores a target that is an operative, a civilian, the guard, the patrol or a `Unit`; the pre-fight `rivals` body is its own one-gun side; otherwise it marks every living `Gun` of the target's side (police: `SecGuns` and non-rival `Hostiles`; rivals: rival `Hostiles`).
- **`Combat()`**, before `foreach (var id in security) CityBody(id);`: `if (Lab && LabKite) KiteTick();`. `KiteTick` visits `SecGuns` (not joined), then `Hostiles`, then the pre-fight rivals, in that fixed order (ids sorted, as `Combat` already does for ties), applies lapse, the cannot-answer test (any operative with a weapon out inside the body's `W.EffCm` on `Sees`), the 5-tick delay, then the side's answer.
- **Police answer on a `Hostile`:** `MoveTo(h, dest)`, `g.St = "advance"`. `FightBody` already stops it when a foe is in reach. Its `hold` branch walks a holder home when idle; `KiteTick` runs first each tick and re-issues the chase, so the two do not fight. Verify this in N151.
- **Police answer on a pre-fight `SecGuns[k]` (no `Hostile` yet):** a displacement, `secDisp[k] = {Path, Start, Face}`. `SecOfficerPose(k)` returns it ahead of the path pose while `!secJoined[k]` (position `Path.At((Tick - Start) * secSpeed)`, heading along it, then `Face` = toward the shooter when it arrives); `Moving(id)` reports it. A joined officer uses its `Hostile` as today, so `JoinResponse` starts from the displaced pose. Hash `secDisp` in key order.
- **Pre-fight `rivals`:** `rd = {Path out, Start, Phase 0 out / 1 hold / 2 back, BackStart, Face}`; `RivalsPose()` and `Moving("rivals")` read it. When phase 2 ends add the detour length to `RivalsDrawnTick` (not when `RivalsDelayedUntil > RivalsDrawnTick`, that restart case just skips the hide). Hash `rd`.
- **Ambush rivals:** `TakeCover` + `MoveTo` as in the prototype; blind-point fallback is `Grid.Nearest(pos, q => !Sees(q, sp), 800)` (800 cm).
- **Log:** one `ai.kite` line per body per alarm, at the first step: "`security-1` answers `tech`: first shot at 1836, moves at 1841 (5 ticks), 15 m out." Use the existing silent `ai.` prefix so no log code is added.
- **`PreviewAnswer(string target)`**: read-only, `{side, point, ticksToRing}` for the host. Same maths, no state change.
- **Not in the lab:** `Lab == false` runs none of this, adds no hash bytes. Confirm `hashes --start-text BASE` 229 of 229.

### 6.3 Scenario checks (N150 onwards; every one also gets the suite's replay and checksum tests)
- **N150** police close in: lab, response unit holding at path end, one shot from the Tech at 16 m. Both officers start moving 5 ticks after the shot (both, from one shot), stop with the Tech inside `EffCm` and a line, and fire. Lab with `kite` off: no movement (the old behaviour).
- **N151** no tug of war: an officer holder at a stand, shooter 17 m out; it leaves the stand, closes, and after the lapse walks home. No frame where it moves toward and away.
- **N152** chase cap: shooter steps back 4 m after every shot. The officer's total walk is <= 20 m, then it holds cover and does not walk home until 100 ticks after the last shot.
- **N153** engaged exemption: an officer firing on operative A at 12 m is not diverted by a shot from B at 17 m.
- **N154** rivals: an ambush rival shot at from 17 m takes cover within 5 ticks (an `ai.cover` line) or a blind point; the pre-fight crew goes out, holds, comes back after 30 quiet ticks, and its arrival is late by exactly the detour.
- **N155** rows (in `fight-lab`, with the rest of the adversarial set): `L3:kite`, `L3:reset-kite`, `L3:focus-reset-kite`, `L3:cycle-kite`, `L3:carbine-kite` each "fail, sealed" or a heavy debrief with the casualty line; P-A and P-B <= 25 %, P-G <= 33 %, no op down. Add one new measured line to the verdict (§6.6 item 2): for each cell agent, hostile downs before the hostile side's first shot at the cell <= 1.
- **Today:** `hashes --start-text BASE` unchanged for all 229 runs; `dotnet test`; `bun run typecheck`.

### 6.4 Open issues for Keel to report, not fix
1. **`Units` (brief 013 police-n and robots).** They are armed, path-driven bodies in `UnitsCombat` and appear in L3 after about 3:27 (`police-1` and `police-2` fire in my `ckite` log). I did not cover them. Same overlay, a third kind. Not needed for any adversarial row to pass (they arrive after the seal is near), but a lab player who clears the response unit first and then the police units is unmeasured.
2. **Pre-fight rivals at zero health still run off for 175 s** (`rivals-retreat-s`) without a Trail. That is an old rule, and it is the cheapest "win" left in the pickup: three hits at the band. The hide makes it harder, not impossible. Motive's call whether the retreat should cost.
3. **Re-measure the whole matrix on the real tree** with §6.1-6.5 in. My Config B is a stand-in.

## 7. Evidence
- Scratch tree `out/pulse027/` (a copy of `sim/` plus the probe and my hooks), deleted at the end; probe from `briefs/027/suture/probe/`. Command pattern: `RUNS=L3,P-A,P-G,P-B POLS=matrix,kite,reset-kite,focus-reset-kite,cycle-kite dotnet probe.dll <rulebook>`; `SUTURE_FIX=2` for aim memory; `KITE=0` for the response off.
- Reference code: `briefs/027/pulse/kite-response.prototype.cs.txt`.
- No file under `sim/`, `contracts/` or `unity/` was edited.
