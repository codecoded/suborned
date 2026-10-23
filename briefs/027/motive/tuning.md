# Brief 027 · Fight lab tuning (Motive)

**Date:** 2026-10-20.
**Inputs:** Keel's `briefs/027/keel/fight-lab.md` and `fight-lab.json`, and the engine `suborned.sim/0.10.0`.

**What changed:**
- In `contracts/rules/rulebook.json`, six EXPERIMENTAL `lab-*` values: their `v`, and their `reason` (which now names this file).
- Nothing else. A JSON diff of every other value and section against the pre-edit copy is empty. The copy is `out/motive027/rulebook.before.json`.
- The `today` preset, the `lab` preset file, the GDD and the code are unchanged.

## 1. Final values

| value | Keel's first value | final | why |
|---|---|---|---|
| `lab-sidearm-eff-m` | 12 | **15** | Hostiles engage only inside their effective ring. At 12 m the police at their 14 m stands never fired, while the cell hit them from 20 m. This puts today's 15 m back (Keel's proposal 1). |
| `lab-sidearm-max-m` | 20 | **18** | It shortens the cell's long band, where it shoots what can't shoot back, to 3 m. With 20, L3 lost only 27 %; with 18, 33 %. |
| `lab-sidearm-every-ticks` | 6 | **10** | At 0.6 s, two officers dropped an op in 1.1 s. At 1.0 s it takes 2.5 s. |
| `lab-pip-3` | 750 | **600** | Three pips (a trained shooter in the effective ring) hit harder than anything else. At 750 the floor needs a 1.4 s cadence or more, and that makes every fight drag. 600 keeps 10 damage and three hits to drop a rival. |
| `lab-draw-ticks` | 5 | **3** | Ambushers fire 0.5 s after the spring. A cell that draws as it springs now answers inside 0.8 s instead of 1.0 s, which takes a hit off P-G (6 → 5). |
| `lab-carbine-charge` | 8 | **20** | Keel's proposal 4, "make the charge bite": 5 shots from a full cell, then it waits for regen. No matrix run binds it (the Enforcer fires only a few carbine shots before he takes Vale). Judge it in play. |

**Kept:**
- sidearm damage 10, so a rival (30) takes 3 hits and an op (40) takes 4;
- pip-1 250, pip-2 500, pip-4 1000;
- `aimNew` 5;
- every other lab value.

**Keel's proposal 2 (sidearm 8 damage every 0.8 s) is not adopted:**
- At 8 or 9 damage a rival takes 4 hits, every fight lasts longer, and P-G loses an op (57–77 %).
- Damage 10 is the anchor: 3 hits for a rival against 4 for an op is the cell's whole edge in a fair-sized fight.

**Keel's proposal 5, the recharge reading, is confirmed:** the bar refills only when no charged shot has fired within the weapon's cadence. Continuous fire never refills it, which is what the spec meant by "while no charged weapon fires".

## 2. The matrix, before and after

Commands:
- before: `bun run sim fight-lab` on Keel's values;
- after: `bun run sim fight-lab --rulebook contracts/rules/rulebook.json`. This is probe mode, so Keel's `fight-lab.json` is not overwritten. The output is in `out/motive027/matrix-final.txt`.

The `lab-charge-off` and `lab-field-on` rows are identical to `lab` in both, because neither charge nor field binds in these scripts.

| run | today (v0.24) | lab, Keel's values | **lab, tuned** |
|---|---|---|---|
| L1 | fail (camera, 0:56) | fail (camera) | fail (camera) |
| L1c | fail (sealed), 44 %, 8 hits | fail (sealed), 22 %, 4 hits | fail (sealed), 33 %, 6 hits, 1 op down |
| L2 | fail (sealed), 33 %, 6 hits | fail (sealed), **0 %, 0 hits** | fail (sealed), 0 %, 0 hits |
| L3 | fail (sealed), 66 %, 3 ops down | **win 4:50, 0 %, 0 hits** | win 4:50, **33 %, 6 hits, 1 op down**, 22 T, A 2, Standing 2 → −2 |
| L4 | fail (sealed), 11 % | fail (sealed), 0 % | fail (sealed), 0 % |
| P-A | win, 11 % | win, 0 % | **win, 0 %**, 5 T, A 1, +3 |
| P-G | win, **44 %, 1 op down** | win, 22 % | win, **27 % (5 hits), no op down**, 6 T, A 1, +3 |
| P-B | win, 22 % | win, 16 % | **win, 22 %**, 3 T, A 0, +3 |
| lethality (two trained at the effective ring) | op 2.5 s, Enforcer 5.5 s | **op 1.1 s, Enforcer 2.3 s** | **op 2.5 s, Enforcer 4.5 s** |
| §6.4 triggers | 6.4.5 (P-G downs an op) | 6.4.1, 6.4.6, 6.4.7 | 6.4.6 by the coded verdict only (see §3.1) |

**Against the coordinator's targets:**
- **Loud is winnable only at a real cost.**
  - L3, the only loud win, now costs an op down, a third of the cell's health, 22 Trail, Alert 2 and four steps of Standing. Before, it cost nothing.
  - The other loud runs fail by the seal clock, as they do today.
  - L2 and L4 take no hits because they are sealed before any fight reaches them. That is the response dial, not the fight numbers.
- **Prepared runs stay clean.** All three win with no op down, Trail 3–6, Alert ≤ 1 and Standing +3. P-G is 2 % over the spec's 25 % line (§3.2).
- **Lethality is at the 2.5 s floor**, not under it (an op 2.5 s, the Enforcer 4.5 s). Cadence 11 gives 2.7 s, but P-G goes up to 33 %. I kept 10.
- **P-G improves on today** (44 % with an op down, to 27 % with none). It still fails the coded 25 % by one hit.

## 3. For Sky (via Aster), and the spec re-reads

### 3.1 §6.2 "public security High" (Keel's proposal 3): re-read
- The debrief's High needs Alert 3, and Alert 3 needs a civilian to see violence.
- A loud run with no civilian witness ends at "raised", however much it costs. Today's own loud reference route does too.
- **Re-read:** a heavy debrief needs
  - Trail ≥ 12;
  - Alert ≥ 2;
  - Standing at least 2 below the start;
  - investigation active;
  - one of: an op down or captured, ≥ 40 % health lost, or an open case.
- "Public security High" moves out of the AND.
- **Under the re-read, tuned L3 is a heavy debrief** (22 T, A 2, 2 → −2, active, an op down), so 6.4.6 clears.
- **Keel:** change the verdict in `ScenariosFightLab.cs`. This is my spec's line, and I am correcting it.

### 3.2 P-G's 25 % line
- P-G is route G-fast: the door is prepared, but not the getaway.
  - No Inez warning, no turned driver, no false tip, no guard ally, so five rivals.
  - The cell stands in the open; the rivals take cover.
- In the tuned run, two of its five hits come in the ambush's opening volley, 0.5 s after the spring. Nothing in the cell's numbers can answer before then.
- I swept every lab value that moves it:
  - sidearm damage 8–15, cadence 6–12, rings 12–20 m;
  - pips 1–4, draw 1–5, aimNew 2–8;
  - carbine charge.
  
  27 % (5 hits) is the floor. `healthScale` is pinned by the `lab` preset's start (10), so the rulebook value is not read.
- **Two ways to make P-G clean:**
  - **a. Cover snap (B5, not built).** The cell would kneel behind the planters at the car, as the rivals do. That is the honest fix: preparation by position.
  - **b. Read P-G at ≤ 30 %.** It is the one prepared route that prepares nothing for the fight.
- **Recommendation:** a, and keep the 25 % line for P-A and P-B.

### 3.3 Things Sky should know before playing
- **Three pips is now 600, not 750.** The pips read as dots, so nothing on screen changes. But "●●●○ = three in four" in brief 027 rev. 2 is now "about three in five". That is a playground value, not a decision.
- **The sidearm fires once a second.** A three-on-one focus drops a rival in about 1.5 s, and the whole safe fight runs 5–7 s of shooting. That is fast and short, as Syndicate is, and above the readability floor.
- **Charge and field never bind in the scripted fights.** Sky judges them by hand in target mode, where volleys pay charge.

## 4. Handoffs

### 4.1 Keel: tests that assert the first values
The values in §1 change what these assert. All five are in Keel's files, and I haven't edited them.

| test | fails because | fix |
|---|---|---|
| `N140` | it expects draw 5 ticks | read `R.LabDraw`, or expect 3 |
| `N141` | its "2 −1 untrained" case stood beyond the old 12 m ring; at 15 m it is inside (got 4, 3, 2, 1; wanted 4, 3, 1, 1) | move the case past 15 m, or expect 2 |
| `N142` | it asserts the first weapon values (rings, charge) | read the rulebook |
| `N145` | it expects the carbine at 8 a shot | read `lab-carbine-charge` (now 20) |
| `DP1` | the rulebook text moves the inputs hash (any rulebook edit does) | re-record: `suborned-sim drive-session` |
| `contracts/rules/rulebook.test.ts:349` | it expects 3 pips = 750 | expect 600, or read `lab-pip-3` |

**Evidence (2026-10-20):**
- `dotnet test sim/Suborned.Sim.Tests`: 902 of 907 pass; the 5 above fail.
- `bun test contracts tools/sim`: 200 pass and 1 fails, the line above. Fable's 3 `text.test.ts` failures from Keel's report now pass in this tree.
- `bun run typecheck`: clean.
- Behaviour hashes: `bun run sim hashes --start-text BASE` (`out/motive027/hashes-tuned.json`) against Keel's `out/keel027/hashes-after.json`. Every run that existed before is identical; only the inputs hash differs. N140–N148 are new since that baseline. **So the `today` preset is byte-identical.**

**If the shared tree must be green before Keel gets to it,** copy `out/motive027/rulebook.before.json` back over the rulebook. That restores Keel's values exactly; this file keeps the tuned numbers.

### 4.2 The rest
- **Keel:** the §6.2 verdict re-read (§3.1).
- **Latch:** cover snap at the car (B5) for P-G (§3.2).

## 5. How to repeat
- **Probe helpers:**
  - `out/motive027/sweep.py`: one probe of `lab-*` values.
  - `out/motive027/grid.py`: a grid with the verdicts and the §3.1 re-read.
  - `out/motive027/look.sh <rulebook> <run>`: plays one run's fight log from a scratch root.
- **Sweeps:** `out/motive027/grid1.txt` to `grid12.txt`.
- **All probes** use `--rulebook`. They write nothing in the repository.

## 6. Rulings on Suture's balance check (2026-10-20)

**Inputs:** `briefs/027/suture/balance.md` (F1–F8, with `evidence/` and `probe/`), Keel's `fight-lab.md` §6–7, Sky's go-ahead for cover snap.
**Scope:** every rule below is lab-only (`fight.lab.on`). The `today` preset must stay byte-identical (`hashes --start-text BASE`, 229 of 229). I have not edited the rulebook or the sim; Keel adds the values (EXPERIMENTAL, `proposed: true`, reason naming this section) and the engine.

### 6.1 Aim memory: re-aim, switch, holster and the freeze queue (F1)
**Rule:** the hit count belongs to the pair *(shooter, target)*, not to the gun's current aim.
1. A shooter keeps one count per target it has fired at, for the whole mission. A new target's count starts at `lab-acc-start` (§6.2).
2. Changing aim to a target the shooter has already fired at resumes that target's stored count. Nothing resets it: not `cease`, `stance hold`, `holster`, the draw, `OrderEnd`, a volley's end, a `fire.close` walk, nor a target switch.
3. A target switch of any kind still costs `lab-aim-new-ticks` (5) before the next shot, whether or not the target is remembered. The rule is `NextShot = max(NextShot, Tick + aimNew)`.
4. **Freeze-queue switches are ordinary switches.** A queued "fire on B" for an agent shooting A stores A's count and gives B its own. Re-queuing the agent's current target changes nothing.
5. **The same rule covers every shooter in the lab:** cell, rivals, police and response units. One rule reads the same both ways. For hostiles it can only remove free hits, never add them.
6. The chip (`PreviewFire.ShotsToDown`) and the volley forecast read the pair's count, so the HUD stays honest. A remembered target reads the same after a cease as before it.

**Why:** it closes cease-and-fire, A→B→A bouncing and queue switching with one rule. Bouncing now buys nothing: each target pays its own ranging shot (§6.2), once.

**Engine note:** a small map per gun from target id to count. It is hashed only with the lab on. A downed target's entry may be dropped.

### 6.2 The first shot on a new target ranges (F3)
**Rule:** new value `lab-acc-start` = **0** ‰ (today's `acc-start` 500 stays for the `today` preset).
- A new target's first shot hits only at four pips (point blank, 1000). At one to three pips it misses, and the count builds from there.
- What the player sees: "●●●● hits every shot; anything less ranges first." The chip already shows it: a fresh target reads one shot more.
- It applies both ways. The ambush's opening volley at 2–3 pips no longer lands at 0.5 s, and the cell's first volley on an uncovered rival no longer drops it at 0.5 s.

**New verdict line §6.4.7b (cell → hostile floor):** in the lab, an uncovered rival under focus from three sidearms at their effective ring must take **at least 1.0 s** from the first shot to go down. At acc.start 0 it is exactly 1.0 s (miss at 0 s, three hits at 1.0 s). Keel adds the check beside the 6.4.7 line.

**Evidence:** Suture's acc.start sweep (`evidence/accstart.txt`, value 0): L3 heavy at 33 %, P-A 0 %, P-G 27 % (no op down), P-B 16 %, no §6.4 trigger, op down in 3.5 s and the Enforcer in 4.5 s. The values in between (250–499) are worse for P-G. 0 is the only value below 500 that holds.

**Fallback:** if the combined run (§6.7) puts P-G over its line (§6.5), set `lab-acc-start` back to 500 and keep §6.1 alone. That is Suture's measured fix 2: P-G 27 %, L3 heavy at 55 %, `reset-kite` sealed. §6.4.7b is then dropped and noted as open.

### 6.3 Charge rests after any targeted shot (F5)
**Rule:**
1. Any targeted shot (a volley shot, any weapon) stamps `LastCharged`, as a charged weapon's own shot already does.
2. Regen waits a fixed rest after the last charged or targeted shot. New value `lab-charge-rest-ticks` = **20** (2.0 s). This replaces "within the weapon's cadence". After the rest, `lab-charge-regen` stays 1 a tick.
3. Unfired shots refunded at a volley's early end do not stamp anything.

**Effect** (arithmetic; Keel to confirm with Suture's micro):
- volley(1) every second from a full bar: 10 volleys, then empty, then one shot per 3 s (2 s rest + 1 s refill). Today's low point of 88 goes to 0.
- volley(6): 60 paid, full again after 2 + 6 = 8 s, instead of about 6 s.
- carbine at will (20 a shot every 0.8 s): continuous fire already never refilled, so only its recovery after a burst slows. I predict no matrix row moves, because the Enforcer fires only a few carbine shots.

### 6.4 One warning per watcher (F4)
**Rule:**
1. Each watcher (officer, guard, patrol, response unit) gives **at most one** warning per mission. This covers any cell agent. New value `lab-warn-per-watcher` = 1.
2. If the same watcher later sees a drawn weapon before any shot, that is `drawn.seen` at once, as today, with no second chance.
3. A heeded warning also adds **+1 Trail** (the watcher logs it; logs are paper that investigators pull). New value `lab-warn-trail` = 1. The Alert-1 found fault and the remembered face stay as they are.
4. Cameras and rivals still never warn.

**Effect:** Suture's 30-cycle spam goes from 60 warnings at Alert 1 to one warning, then `drawn.seen` on the second draw in the guard's view. Probing who watches costs 1 Trail and one face per watcher. The guard memory cap no longer matters.

### 6.5 P-G's line, and cover snap (F8)
**P-G's line:** a prepared-route pass for **P-G only** is health lost **≤ 33 %** (≤ 6 hits) with **no op down**. Trail, Alert and Standing lines are unchanged. **P-A and P-B keep 25 %.**
- Reason: P-G prepares the door and nothing for the getaway (five rivals, no warning, no turned driver). 27 % is a confirmed floor across nine policies.
- Keel: change the P-G verdict in `ScenariosFightLab.cs`.
- Revisit if cover reaches the car posts or §6.2 lowers P-G.

**Cover snap: keep it in the lab preset, scoped.** It doesn't fix P-G, so it is not counted as one. It stays because Sky approved it, and because it is the readable way for a player to fight by position: in hand play and the loud runs, kneeling at a mark is a choice the player sees coming.
- **Required scope** (Keel, F8):
  - the snap fires only while the agent is drawn or a fight is open;
  - it never fires on a move to a named place (`d:` or `crew:` targets).
- **Done-check:** with the snap on, lab P-B passes again (≈ 8:33, ≤ 25 %). No other matrix row gets worse than with the snap off.
- If the scoped snap still moves any prepared row, the preset turns it off. It does not ship broken.
- Cover points within 1.5 m of the car posts would make the snap work for P-G. That is placement (Vantage / Waymark), not a rule; I'm handing it to Aster as an option, not a request.

### 6.6 The stand-off band (F2): what Pulse's fix must achieve
**Outcome required** (lab, with §6.1–6.4 in place):
1. **No cheap loud win.** The adversarial rows `L3:kite`, `L3:reset-kite`, `L3:focus-reset-kite` and `L3:cycle-kite` either fail (sealed) or win as a **heavy debrief** under §3.1, including the casualty line (an op down or captured, ≥ 40 % health lost, or an open case). No loud win comes in under 25 % health lost (6.4.1).
2. **Being out of reach is not safe.** An armed hostile that is shot at from beyond its effective ring must respond within **1.0 s** of the first shot. Pulse picks how: take cover out of the shooter's line, or advance to its effective ring (police and response units close in, since that is their job; rivals take cover).
   - Measured: in any adversarial row, no single cell agent downs more than **one** armed hostile without being fired on.
3. **Prepared rows don't move past their lines.** P-A and P-B ≤ 25 %, P-G ≤ 33 % with no op down, and Trail, Alert and Standing as now.
4. The fix is deterministic, lab-only, and `today` stays byte-identical.
5. **The carbine's band is covered too.** Its band is 15–30 m against the hostiles' 15 m. Keel adds an `L3:carbine-kite` row (the Enforcer clears from 15–20 m before the Enter), and condition 1 must hold for it.

**Fallback if the AI fix misses** (weapon rings I accept):
- **Sidearm:** `lab-sidearm-max-m` 18 → **16** (a 1 m long band). Suture's `band-max.txt` shows kite and focus-reset-kite fail sealed, matrix L3 stays heavy (38 %), and P-G goes to 33 % (6 hits, no op down), inside §6.5's line. I prefer 16 to 15: it keeps a visible long pip at the edge.
- **Carbine:** only if the `carbine-kite` row wins cheaply, `lab-carbine-eff-m` 20 → **15** and `lab-carbine-max-m` 30 → **22**.
- The hostile sidearm's 15 m effective ring stays where it is.

### 6.7 Predicted matrix (lab, all of §6.1–6.5; Pulse's fix not yet in)
These are predictions from Suture's separate measurements. The combination has not been run. Keel measures it; Suture retests.

| run | now (tuned, §2) | predicted | from |
|---|---|---|---|
| L1, L1c, L2, L4 | fail | fail (unchanged) | sealed before the fight decides |
| L3 matrix | win, heavy, 33 %, 1 down | win, heavy, **33–55 %**, 1–2 down | accstart 0 (33 %), fix 2 (55 %) |
| L3 reset-kite / focus-reset-kite / cycle-kite | **win, 0–5 %** (F2) | **fail, sealed 5:05** | fix 2 seals them already; at acc 0 a re-aim only slows the shooter |
| P-A | 0 % | 0 % | both probes |
| P-G | 27 % (fails 25 %) | **27 %, passes ≤ 33 %** | both probes, §6.5 |
| P-B | 22 %; fails with unscoped snap | **16–22 %, passes** once the snap is scoped | accstart 0, `rb-snap2-nosnap` |
| hostile → op lethality | op 2.5 s, Enforcer 4.5 s | op **3.5 s**, Enforcer 4.5 s | accstart 0 |
| cell → uncovered rival (6.4.7b) | 0.5–0.8 s | **1.0 s** | arithmetic |
| panic spam (micro) | 60 warnings, A 1 | 1 warning, then `drawn.seen` | §6.4 |
| volley(1) spam (micro) | bar low 88 | bar empties after 10 volleys | §6.3 |

**If a prediction misses:** P-G over 33 % or an op down → §6.2's fallback. Any adversarial L3 row still cheap → wait for Pulse (§6.6), then the sidearm 16 m fallback.

### 6.8 Handoff
- **Keel.**
  - **Inputs:**
    - §6.1 aim memory;
    - new values `lab-acc-start` 0, `lab-charge-rest-ticks` 20, `lab-warn-per-watcher` 1, `lab-warn-trail` 1;
    - §6.3 stamping;
    - §6.4 warning rules;
    - the P-G ≤ 33 % verdict and §6.4.7b;
    - the cover-snap scope (§6.5);
    - adversarial rows in `fight-lab` from Suture's `SEngage`: `L3:reset-kite`, `L3:focus-reset-kite`, `L3:cycle-kite`, `L3:carbine-kite`, `P-G:reset`, plus the panic-spam and volley-spam micros;
    - doc nit: `lab-target-carbine-charge`'s reason should say "on top of the carbine's own 20".
  - **Done-check:**
    - §6.7 measured, with each row matching the prediction or the miss reported;
    - `dotnet test` green;
    - `today` 229 of 229 identical;
    - `bun run typecheck` clean.
  - **Next owner:** Suture (retest F1–F5, F8 with the same scripts).
- **Pulse.**
  - **Inputs:** §6.6.
  - **Done-check:** §6.6 items 1–5 on the adversarial rows.
  - **Next owner:** Keel, then Suture.
- **Aster** (FYI):
  - cover points at the car posts are an option for P-G (Vantage / Waymark);
  - the freeze's focus lines are information a pause doesn't give (Suture F7). Either show them in real time too, or say so in the playground doc. That's a Glyph / Latch call.

## 7. Rulings on Suture's retest (2026-10-20)

**Inputs:** `briefs/027/suture/retest.md` (R1–R7, scripts in `retest-scripts/`), brief 018 §10 (the getaway, Sky's intent), `briefs/011/motive-fights.md` (run speed), GDD v0.24 §03, §05, §06, §07.
**My own check:** I replayed four of Suture's scripts with `bun run sim play` on the real tree. Each says "replay identical", and each gives Suture's result:
- `L3.enfgo-carfree-runkite.today`: success 4:45.0, 18 T, 0 hits;
- `L3.focus-enfgo-carfree-runkite.lab`: success 4:36.4, 15 T;
- `P-G.lab-cover`: rivals-reach-vale 3:49.5;
- `P-G.lab`: success 4:41.9.

I edited nothing under `contracts/`, `sim/` or `unity/`. Keel builds.

**Two kinds of ruling.** Every ruling below says which kind it is.
- **LAB:** only under `fight.lab.on`. `today` stays byte-identical (`hashes --start-text BASE`).
- **TODAY:** it changes the v0.24 game. It needs Sky's OK through Aster before Keel builds it outside the lab. Until then it is built lab-only, or not at all.

### 7.1 What the loud route should cost (R1)
**Sky's intent** (brief 018, 2026-10-17): "I want to get my team into the car, and get away. But it's interesting, because the enforcer can't run, so they have to support the enforcer." In §05: "Fighting out is a last resort, not a route."
- So the loud getaway is a supported walk. The Enforcer carries Vale at 1.4 m/s and can't run (§03). The police must come for him.
- Left alone, he goes down. Brief 011 measured that: the Enforcer alone against the response unit and 2 officers is down in 22.8 s.
- Covered by the three, he can make it.

**Why R1 is cheap.** The police never come for him.
- In both presets, the lane blocker was the response unit's officer: `security-1` in today, `security-2` in the lab. The cell shot it down (today from 22–23 m, in the long band).
- `officer-1` and `officer-2` either hold stands about 19 m from the car or stall on the kite budget (R2).
- Hostiles fire only at a foe inside their effective ring (15 m). So the Enforcer walks past them and boards with nobody shooting at him.
- Running doesn't cause this. It only keeps the three out of reach while the Enforcer's walk stays uncontested.

**The price of a loud win.** The scripted player is a perfect-reflex player, so this is the lowest a loud win may cost. A real player pays more. Every column must be worse than the quiet routes:

| | quiet, prepared (P-A, P-B, P-G) | loud, lowest price allowed to win |
|---|---|---|
| Trail | 3–6 | **≥ 12** |
| Alert | ≤ 1 | **2 or more** |
| Standing | +3 | **≤ start − 2** |
| investigation | — | **active** |
| the fight | ≤ 25 % (P-G ≤ 33 %), no op down | **≥ 40 % health lost, or an op down or captured** |
| contact | — | **the getaway was contested (6.4.8 below)** |

This is §3.1's heavy debrief plus §6.6 item 1's casualty line, which already stand. R1 in the lab passes every line except the fight: 15 T, A 2, 2 → 0, active, but only 11 %. So the fix is the contact, not more Trail.

**Loud must stay winnable.** At least one supporting row must win heavy in the lab: matrix L3 does today, at 44 %. If no supporting row wins, loud has become a dead end, which is also wrong ("they have to support the enforcer").

**New verdict line 6.4.8, a contested getaway (LAB, Keel):** a loud win fails the verdict if either of these holds:
- (a) a final-fight police body is up at Go, and from Vale coming out to Go it never once had the Vale holder, or a cell agent within 15 m of him, inside its effective ring on a clear line;
- (b) the hostiles fired no shot at the cell between Vale coming out and Go.

This is Suture's R5 "contact" line. §6.6.2's free-downs count can't see R1, because the cheap line downs nobody it doesn't need to.

### 7.2 The rule that fixes it: in the final fight, the police go for Vale (R1)
**LAB now; TODAY with Sky's OK.** R1 holds under `today` too: 0 hits at 4:45.

1. **Objective.** A final-fight police body's objective is **whoever holds Vale**, and once Vale is in the car, the car. This covers the officers, a joined response unit, and the brief-013 units and robots that are on the fight's street.
   - With no foe inside its effective ring, it leaves its stand and advances until the Vale holder is inside its effective ring on a line (today's `StopPoint`, 90 %).
   - It takes cover on the way, as an advance already does.
   - It holds a stand only while a foe is in its reach.
   - **Why:** §06 already says it. Hostiles "advance … toward their objective (the getaway car in the final fight)", and their target priority puts "whoever holds Vale" second. The stands were placed for a cell parked at the car. A 44 s walk past them is what broke them.
   - **GDD wording for later** (Aster, at the next version): "toward their objective (Vale, then the getaway car, in the final fight)".
2. **The blocker keeps its lane.** Nothing moves a blocker off its post: not the kite answer, and not rule 1. Brief 018 §10.2 stands, including "not replaced".
3. **Shooters out of reach.** In the final fight, a police body shot by a cell agent it can't answer takes cover against that shooter on its way to Vale. It does not turn to chase the shooter.
   - The kite chase, and its 20 m budget, are pre-fight answers only.
   - An objective move never counts against any budget: the walk in, the advance, the blocker's walk.
   - Target priority 1 ("whoever shoots at them") still applies the moment the shooter is in reach.
   - So covering fire from the band slows the police, but it can't stop them. That is the support Sky described, and it costs the cell time while the Enforcer is under fire.
4. **The car's hold stays 8 m or the exit lane, in both presets.** Suture asked whether the hold should reach officers who can see the car. No:
   - a hold by sight would make every loud Go wait for every officer in sight, which is "kill them all", not "get my team into the car";
   - brief 018 chose this hold with Sky.

   With rule 1, the officers are inside their reach of the car door when the Enforcer boards (4 s), so the board is contested anyway.
5. **What doesn't move:** the prepared rows, P-A, P-B and P-G. They are quiet, and their final fight is the rivals; rule 1 is for police only. If any prepared row's numbers change, that is a bug, not a balance result.

### 7.3 Running and "never a foot race" (R1)
**Provenance:**
- Sky chose the 2.8 m/s run (brief 011, Q2).
- "Never a foot race" is my own reasoning in that brief. It was copied into the reason for `op-run-speed-cm-s`. It is not Sky's rule.

**Ruling: it stands, read correctly. TODAY, text only.**
- **Running stays free once a fight is on**, as §05 says. No new cost.
- **The police never need to win a foot race.** The fight is decided at Vale, and nobody runs carrying Vale (§03). The cell can outrun the police, but Vale can't, so outrunning them buys nothing at the car.
- **Pulse's "no infinite kite" limit 1** (a walking kiter) applies pre-fight, where the police's threat is the seal clock and the Trail per person hit. In the final fight, §7.2 rule 3 replaces it.
- **Keel:** change the reason for `op-run-speed-cm-s` to "… and that's fine: the police's threat is cones, guns and the seal, never a foot race; Vale can't run, so in the final fight the police go for him and the car, not for the runners (Motive, briefs/027/motive/tuning.md §7.3)". The value doesn't change, but the inputs hash moves, as any rulebook text does.

### 7.4 R4: the pre-fight crew's second drive-back
**Ruling: every drive-back charges `hit-trail` (+2 T). The first one included. None charges `down-trail`.**
- The crew is one gun that stands for several people. A drive-back is three fresh hits on people who were already hit once, so "+2 T per person hit (once)" read per gun undercharges.
- Nobody drops: they run. The engine's comment "running away isn't a drop (no Trail)" stays true, so there is no `down-trail`.
- **Engine (Keel):** when the crew's health is restored at a drive-back, also clear its `Hit`. No new value.
- **LAB now; TODAY with Sky's OK.** In `today` this moves only runs that drive the crew back twice. Keel's hash diff names them.
- **The AI side is Pulse's:** a fallback when there is no hide, and no leaning back out into a shooter who is still there.

### 7.5 R6: why using the cover marks loses P-G, and the fix
**Why** (my replays of `P-G.lab-cover` and `P-G.lab`):
1. **The cover player leaves the Enforcer.** The policy sends the three to the marks nearest each of them:
   - the café wall: cv-085, cv-045, cv-048;
   - then the fountain: cv-015, cv-020, 15–30 m north of the Enforcer.

   From there they kill rival-4 and rival-2. The blocker, rival-1 at cv-087, about 11 m from the car, shoots the Enforcer unanswered. Its target is him, priority 2, "whoever holds Vale". Its six hits run 3:30–3:49, and the Enforcer goes down: rivals-reach-vale.

   In plain P-G the three stay by the car and fire on rival-1 from 12–14 m from 3:33, and the Enforcer ends the run at 40 of 60 health.

   So the snap rule isn't wrong. The player who chases cover stops supporting the Enforcer. That is brief 018 §10.3's point: "the three must leave the Enforcer to flush it, which is the opposite of supporting him".
2. **A probable bug (Keel):** in both runs the Scout snaps onto **cv-087**, at 3:35.2 in plain P-G and 3:35.7 in the cover run, while rival-1, the blocker, still stands on it. It took the point at 3:26.6.
   - `SnapPoint` skips a point held by someone else. So either the blocker's `coverTaken` entry was dropped while it stayed on the point, or something else broke the bookkeeping.
   - Two bodies on one point is wrong either way.
   - It puts the Scout at point blank to the blocker, and the Scout goes down at 3:48.5 in the cover run.

**Fix:**
- **Keel (LAB):**
  - find the double hold on cv-087 and add a scenario for it: "a point a hostile stands on can't be snapped to";
  - change the `lab-cover` harness so it seeks marks only within 15 m of the Vale holder once Vale is out. Otherwise it stays put. A fair cover player supports the Enforcer; the old harness measured a policy no one should be taught.
  - **Done-check:** `lab-cover` P-G passes P-G's line, ≤ 33 % and no op down. Plain P-G and P-B don't change.
- **Cover rule:** unchanged. The snap scope from §6.5 stands.
- **Options, not requests:**
  - cover within the car ring (Vantage, through Aster). It changes the site, so the `today` fights move with it. **TODAY**;
  - marks within `lab-cover-marks-m` of the Vale holder as well as the selected agent, so the player sees cover where it is needed. This is readability, so Glyph and Latch, through Aster. **LAB**.
- Until the Keel items are in, the `lab-cover` row is reported, not judged.

### 7.6 R7: the volley's charge
**Ruling: intended. No change.**
- Charge pays for the frozen tick's chosen and forecast shots: an exact target, an exact count, and hits shown in advance. It does not pay for the right to fire.
- Real-time fire is free in every mode, as a `fire` order is.
- After a volley, the agent goes back to its stance with its aim still on the target (`host-interface`, playground §1.3). So "16 volleys paid, 30 shots fired" is correct.
- Aim memory (§6.1) means that keeping the aim gives no extra accuracy.
- **Glyph and Fable, FYI:** never show charge as ammunition. It is "planned shots".

### 7.7 The split with Pulse
Pulse's `briefs/027/pulse/retest-fixes.md` didn't exist when I wrote this. Here is the split I propose. **Pulse:** confirm it or change it in your file.
- **Motive owns the outcomes and prices:**
  - the loud price table and 6.4.8 (§7.1);
  - the objective rule in words, and the scope of the chase budget (§7.2);
  - the run ruling (§7.3);
  - the drive-back Trail (§7.4);
  - the P-G line (§6.5);
  - R7.
- **Pulse owns the motion:**
  - how a body advances on the Vale holder: cover on the way, the stop point, re-plans;
  - R2's budget counting, refill and capped bodies, pre-fight;
  - R3, units and robots as a third kind;
  - R4's no-hide fallback and the lean-out.
- **The order of authority:** where we disagree, Pulse's file decides how bodies move, and mine decides what must be true at the end. A clash between them goes to Aster.

### 7.8 Handoffs
- **Keel.**
  - **Inputs:**
    - §7.2 rules 1–3, lab-only;
    - verdict line 6.4.8;
    - §7.3's reason text;
    - §7.4 (clear `Hit` at a drive-back, lab-only);
    - §7.5's double-hold bug and the `lab-cover` harness;
    - Suture's R5 rows for both presets, and the sub-cell stall from R2.
  - **Done-check:**
    - in the lab, `L3:enfgo-carfree-runkite`, `L3:focus-enfgo-carfree-runkite` and `L3:enfgo-carfree-kite` fail, or win heavy with the casualty line and 6.4.8;
    - matrix L3 still wins heavy;
    - P-A and P-B ≤ 25 %, P-G and `lab-cover` P-G ≤ 33 %, all with no op down;
    - `today` 229 of 229 identical, apart from the reason text's inputs hash;
    - `dotnet test` green;
    - `bun run typecheck` clean.
  - **Next owner:** Suture.
- **Pulse.**
  - **Inputs:** §7.2 and §7.7.
  - **Done-check:** his `retest-fixes.md` confirms or changes the split, and specifies the advance on the Vale holder.
  - **Next owner:** Keel.
- **Aster, for Sky.** These would change the v0.24 game, and each needs an OK:
  - (1) §7.2, the final-fight police go for Vale. R1 shows `today`'s loud getaway is uncontested (0 hits), so the v0.24 reference "loud 4:23.5 · 13 T · A 2 · 0" is cheaper than §07's intent;
  - (2) §7.4, a second drive-back costs +2 T;
  - (3) cover in the car ring (§7.5);
  - (4) today's long pistol band, where the response unit was shot down from 22–23 m with no answer: the lab's 18 m (or §6.6's 16 m) carried to today.

  The GDD wording in §7.2 rule 1 goes in the next version.

### 7.8 Sky's decision (2026-10-20)
"stay in lab": none of the four today-game changes (police contest for Vale, +2 Trail per drive-back, cover inside the car ring, the shorter pistol band) is promoted to `today`. All stay lab-gated until Sky has played the lab.

### 7.9 Sky's decision (2026-10-20): promote the kite response and the 16 m sidearm to `today`
After Sky found police at cover never answering fire on the fight range under `today`: "yeah, promote both". The lab's kite response (Pulse's rule with the R2 budget fix, R3 police units, the no-cover close-in fallback and the N7 5-tick re-plan) and `sidearm-max-m` 16 move from lab-gated to `today`. The other lab rules (police contest of the car, +2 Trail per drive-back, cover in the car ring, aim memory, acc-start, charge, warnings) stay lab-only, per 7.8.

### 7.10 Police in cover answer fire (Sky's fight-range report, range scenario R08)
**Inputs:** the coordinator's note, Sky's report from the fight range, `sim/Suborned.Sim/ScenariosRange.cs` R08 and R11, `GameKite.cs` (`KiteFightCover`, `KiteFightClose`), and Pulse's `retest-fixes.md`.

**Why it happens:**
- §7.2 rule 3, as built, says a final-fight police body shot from out of its reach takes cover that shields it and stops there. A shielded body never plans again (`KiteFightCover` returns while `Protected`).
- Hostiles fire only at a foe inside their effective ring (15 m).
- So at R08's 20 m the carbine (effective 20 m, max 30 m) keeps landing hits, at the halved rate for cover. The officers stand and take it until they drop.
- Cover halves the hit rate. It doesn't stop the hits, so holding is a slow death with no answer. Sky is right that it reads as a bug.
- Rule 3 was written to stop a chase. It went one step too far: it also stopped the answer.

**Ruling: shot from cover, the police answer. TODAY and LAB.** This fixes the behaviour Sky reported, inside the kite response he promoted in §7.9. It changes `today`'s hashes. Keel's hash diff names the runs that move.

1. **They fire back to their max ring.**
   - A police body that has been shot at in the last 3 s, hit or miss, by an operative it has a line to, fires back while that shooter is inside its weapon's **max** ring, not just its effective ring.
   - This covers final-fight officers, the response unit before and after it joins, and the brief-013 police units. It does not cover robots, the door guard or the patrol.
   - It fires from where it stands, keeping its cover, at its band's odds: today, pistol long band 15–16 m, 400 ‰ with the usual modifiers; lab, the 1-pip band.
   - Target priority 1 (whoever shoots at them) already picks the shooter. This rule only lets that priority reach the max ring.
   - No new value. It reads `pistol-max-m` (today 16) and `lab-sidearm-max-m` (lab 16).
2. **Beyond their max, they hold for 3 s, then close.**
   - If the shooter is beyond the body's max ring, as with the carbine at 16–30 m or the lab rifle, the body holds its cover for at most **`kite-cover-hold-ticks` = 30** (3.0 s, [S], new; one value read by both presets) from the first shot it can't answer there.
   - Then it closes. It walks to the best shielding cover within `ai-seek-police-cm` (8 m) of its stop point of the shooter (`StopPoint(pos, shooter, eff)`, 90 % of the effective ring). With none, it walks to the stop point in the open. This is Pulse's suggestion in `retest-fixes.md`, and it is `KiteFightClose` with cover on the end.
   - It fires by rule 1 the moment the shooter is inside its max ring and it stands.
   - While it walks it fires nothing, as today.
   - If shots keep landing and the shooter is still out of its max, it plans again every 3.0 s. If it is unshielded, it plans again `kite-react-ticks` after its last plan (N7, as built).
   - Why 3 s:
     - today's carbine fires every 2.0 s, so the officers move after its second shot;
     - the lab carbine fires every 0.8 s, so they move after about four shots, near the five its charge bar pays for;
     - it reads as "shot, take cover, come for you".
3. **Never a foot race.** The kite response's existing limits still bound the close:
   - it is at the walk (2.2 m/s);
   - it goes only toward a shooter who has fired within `kite-lapse-ticks` (10 s);
   - a running operative can't fire, so a cell that runs away isn't chased past the lapse;
   - pre-fight, the budget, contact and re-arm hold as built (`kite-chase-cm`, `kite-contact-cm`, `kite-rearm-ticks`);
   - in the final fight there is no budget, because only a shooter who stands still keeps the alarm open.

   The outrun-them case is unchanged: Vale walks, so the getaway is decided at him (§7.3).
4. **Who stays put.**
   - The **blocker** keeps its lane (brief 018).
   - **In the lab only**, the officers Pulse's contest rule sends to the car (`lab-police-contest-n`) also stay at their post.
   - Both answer by rule 1 only. Their side's other bodies close by rule 2, as the kite moves the whole side.
   - So a shooter can't pull the car's holders off it.
5. **Rule 3's new wording** (Keel, in the `GameKite.cs` header and the rulebook reasons): "in the final fight, a police body shot from out of its reach takes the cover that shields it, fires back to its max ring, and after `kite-cover-hold-ticks` with the shooter still out of its max, closes to the shielding cover nearest its stop point. It never chases a runner. The blocker keeps its lane."

**Does it reopen a cheap loud win?** This is an argument; it hasn't been run yet. Keel measures it, and Suture retests.
- **It only adds police fire and movement where there was none.** Before, a police body in cover took every shot at ×500 and answered none. Now it answers at the band, or walks at most 8 m (about 3.6 s in the open) to where it can.
- **The only way it could make loud cheaper is a lure**, pulling police off the getaway:
  - in `today` the police don't contest the walk anyway (R1, kept by Sky in §7.8), so there's nothing to lure them from;
  - in the lab, rule 4 keeps the car's holders put.
- **The band shooter loses its safe line.** The kite rows stand just outside the ring and fire at 16 m. They now draw return fire at the same 400 ‰ (today) or the same pip (lab). With the carbine at 16–30 m, the police are in reach within about 3 s plus one bound.
- **The prepared rows can't move.** Their final fight is the rivals, and this rule is for police only.

**Keel:**
- **Build:** rules 1–5 in both presets, and the new value `kite-cover-hold-ticks` 30 (reason naming this section).
- **R08 changes meaning.** It now asserts that police in cover at 20 m, under carbine fire from beyond their 16 m max:
  - hold no more than 3.0 s, then close;
  - fire on the Enforcer within 10 s of his first shot;
  - never leave by more than one bound per 3.0 s.

  R11 is unchanged.
- **New scenario:** "return fire in the band". An officer in cover is shot at 15.5 m. It fires back within `kite-react-ticks` plus its aim time, and doesn't move.
- **Done-check:**
  - R08 (new meaning) and the new scenario pass;
  - none of Suture's adversarial rows gets cheaper in either preset: no new win, and no win with less health lost. That covers `L3:kite`, `reset-kite`, `focus-reset-kite`, `cycle-kite`, `carbine-kite`, `enfgo-carfree-kite`, `enfgo-carfree-runkite` and `focus-enfgo-carfree-runkite`;
  - matrix L3 still wins heavy in the lab;
  - P-A, P-B and P-G are unchanged;
  - `dotnet test` green;
  - `bun run typecheck` clean.
- **Next owner:** Suture, then Sky on the fight range.

**Pulse:** the motion is yours (§7.7). If the stop-point cover pick or the 3.0 s re-plan reads wrong in motion, change it in your file. The outcome I need: no police body is hit twice by a shooter it can see without either firing back or moving toward where it can.

### 7.11 After §7.10 was built (engine 0.14.0): rule 4, R08's targets, lab P-G
**Inputs:** Keel's report through the coordinator. My own probe-mode matrix (`bun run sim fight-lab --rulebook contracts/rules/rulebook.json`, output `out/motive027/matrix-0.14.txt`; nothing written to the repository). Four rows exported with `--scripts out/motive027/s014` and replayed with `bun run sim play`, all "replay identical":
- lab P-G: rivals-reach-vale at 3:44.3, 55 %;
- today P-G: success 4:26.6, 27 %;
- lab `L3:km10-fm5-step200-norivals-at208-runkite`: success 4:39.5, 27 %;
- today, the same row: enforcer-down 4:06.9.

#### 7.11.1 Rule 4 and the km10 row: the car's contest officer answers like the others (LAB)
**What happens** (lab log):
1. The response unit and officer-2 are taken one at a time: the blocker, `security-2`, falls at 3:45.3, and officer-2 at 4:04.4.
2. Then `officer-1`, the officer Pulse's contest rule sends to the car, stands at his post 13 m from it. Under rule 4 he fires only on the Enforcer: 600 ‰, one hit.
3. The three stand at 15 m, in his long band, and take him down at 250 ‰ unanswered.

So the contest officer is alone, and the three pick him off from the band.

Before §7.10 he closed toward the shooters, traded with them at effective range, and the row failed at 44 %. Keel's measure with rule 4 off fails at 55 %. The lure I wrote rule 4 against doesn't happen: pulled off the car, he costs the cell more than he does by staying there.

**Ruling:**
- **Rule 4 now covers the blocker only.** The blocker keeps its lane: brief 018, where its lane is the hold.
- **The contest officer answers by rules 1–3, as every other police body does.** His post is where he starts, not a place he must stay. This is lab-only, because the contest itself is lab-only.
- **Keel:**
  - drop `lab-police-contest-n` officers from rule 4;
  - keep the blocker;
  - change rule 4's text in §7.10 and in the rulebook reason to "the blocker keeps its lane and answers by rule 1 only".
- **Done-check:**
  - the km10 row fails, or wins heavy (≥ 40 % or an op down), in every kite-on lab preset;
  - none of Suture's adversarial rows gets cheaper than it is in `out/motive027/matrix-0.14.txt`;
  - matrix L3 still wins heavy;
  - `today` doesn't move (rule 4's contest part is lab-only).
- **If it still wins cheap**, the next lever is the officer's cover: he takes the best shielding cover within `ai-seek-police-cm` of his contest point (Pulse's open item in `retest-fixes.md`). It is not a new value. Pulse rules on the motion.

#### 7.11.2 R08: police closing fire on whoever comes into reach first — fine (TODAY and LAB)
- **Yes, that is right.** The bug Sky saw was a police body with a target in reach that didn't fire. A body that fires on whatever it can reach can never read like that.
- **The one condition (Keel to confirm, no new rule):** target priority 1 ("whoever shoots at them") must win as soon as the shooter is in reach too. The shot that picks a target re-reads the priority each time, so a body that drew in on the Enforcer and met the Scout on the way turns to the Enforcer once he is in reach and still shooting at it, within the last 3 s.
- **Stopping short to trade with the nearer agent is also fine.** That agent has screened the Enforcer, which is support.
- **R08's assert:** "fires on the cell within 10 s", not "fires on the Enforcer". Add one line: "once the Enforcer is in its max ring and has fired on it in the last 3 s, its next shot is at him".

#### 7.11.3 Lab P-G: a harness and posts problem, not a lab rule change
**Facts:**
- `today` P-G passes at 27 % with the same posts and the same 16 m sidearm. The lab fails at 55 %.
- The ambush now springs at 3:30.8, against 3:26.6 in Keel's retest3, with the cell 4–10 m from the rivals' cover. That is point blank, where lab pips hit at 1000 and 500 ‰.
- Two causes lose the run:
  1. **A ping-pong in the scripted player.** From 3:40.5 the Fixer and the Tech get a new move order every tick: 100 `heads for` lines, and 16 snaps onto cv-001 that the next tick undoes. One is the harness's fire-close walk to the blocker's point (1925, −5075); the other is the cover snap at the car. They never settle, and never fire from a post.
     - With `lab-snap-off`, P-G doesn't lose Vale: success, 50 %.
     - `today` has no snap, and 0 such lines.
  2. **Nobody answers rival-3.** From cv-079, 11–13 m from the Enforcer, rival-3 lands four of his six hits (3:34.3–3:40.3), while the three work nearest-first through rival-4, rival-2 and the blocker.

**Ruling: fix the player and the posts, and keep the lab rules as they are.**
- **Keel, harness (LAB):**
  - (a) **End the ping-pong.** A move whose end the snap moves counts as arrived at the snapped point. A fire-close walk and a cover seek never alternate on one agent: fire-close wins until its target is down or out of reach, then the snap.
  - (b) **Focus order for the prepared player:** whoever fires on the Vale holder first, then the blocker, then nearest. This is "support the Enforcer" as a policy. The old nearest-first order measures a player we wouldn't teach.
  - (c) **P-G's posts are moved as the recorded routes were** (`briefs/031/keel-route-moves.json`: inside 15 m, at cover, as N11 and N11b's `FightItOutAll` stand):
    - three cover points within 15 m of the car;
    - each 6–14 m from the ambush cover it faces, so it is outside point blank;
    - each shielding against that cover.

    The posts belong to the harness's P-G, not to `mission.json`. If the mission's own `crew:` posts are the problem in play as well, that is Waymark's data, through Aster. **TODAY:** it would move the recorded routes.
- **Done-check:**
  - lab P-G ≤ 33 % with no op down (§6.5), in `lab`, `lab-snap-off` and `lab-cover`;
  - `today` P-G stays a pass;
  - P-A and P-B are unchanged;
  - no adversarial row gets cheaper.
- **Fallback, only if (a)–(c) still leave P-G over its line:** §6.2's fallback, `lab-acc-start` 0 → 500. It softens point blank's first shot both ways. §6.4.7b is then dropped and noted as open.

#### 7.11.4 Handoff
- **Keel:**
  - **Inputs:** §7.11.1 (rule 4 is the blocker only), §7.11.2 (the priority-1 check and R08's extra line), §7.11.3 (a)–(c).
  - **Done-check:** each section's own, plus `dotnet test` green, `bun run typecheck` clean, and `today`'s behaviour hashes identical (R08's new line is a scenario assert, not behaviour).
  - **Next owner:** Suture.
- **Pulse (FYI):** whether the contest officer takes cover at his point (§7.11.1's next lever) is yours if it's needed.

## 8. Firing on the move (Sky playing build 0.2.19, 2026-10-21)

**Inputs:** Sky, through the coordinator: (1) "the agent should stop when I right click to fire, not keep walking ... unless I'm running"; (2) "firing whilst running should be allowed, but with reduction in accuracy"; (3) "Running whilst shooting allows agents to avoid a mass firefight, we can reduce accuracy even further if running away from the target"; (4) "or they can go in head first, hoping to get closer to target for better accuracy"; (5) the two cases in §8.7. Code read: `GameCombat.cs` (`Fire`, `CellShooter`, `Aim`, `Shoot`), `GameOrders.cs` (`Move`), `GameFightLab.cs` (`FireRefusal`, `PreviewFire`, `CloseIn`), `GameAi.cs` (hostile fire while moving), `Rules.cs` (`Accuracy`, `LabPips`); host `HostFight.cs` (`FireSelected`, `ShotAt`).
**Kind: TODAY and LAB.** Sky asked for it, so it changes the v0.24 game. `today`'s behaviour hashes move; Keel's hash diff names the runs. I edited nothing under `contracts/`, `sim/` or `unity/`.

### 8.1 What the engine does today
Checked in the code, not the host. The host's `FireSelected` sends a plain `fire` order and never sends a move again.

| the agent is | fire order, target in reach | fire order, target out of reach |
|---|---|---|
| standing | stands and fires | walks in (`weapons.close`), then fires |
| walking (a click move) | **keeps walking to the old destination**, firing at `mod-moving` 500. This is what Sky saw | the walk-in **replaces** the walk |
| running | refused, `fire-running` | refused |
| driven (WASD) at a walk | fires at 500 | the host walks it in, lab only |
| driven at a run (the gait latch) | refused, `fire-running` | refused |

The other order, a move given while a fire order stands:
- **A walk** keeps the fire order, and the agent fires at 500 as it walks. If the target is out of reach when the walk ends, the fire order walks it back in. That is a surprise: you moved it away, and it walks back.
- **A run** ends the fire order (`fire.cancelled`).

Hostiles fire nothing while they move in `today` (`GameAi.cs:210`). In the lab they fire as they walk (`hostilesFireMoving`).

### 8.2 The order rule: which order ends the other
1. **A fire order stops a walker.** If the agent is walking (a click move, a walk-in or a cover-snap walk), the walk ends at once. It stands and fires. If the target is out of reach or has no line, the walk-in replaces the walk, as built.
2. **A fire order never stops a runner.** If the agent is running, the run goes on to its end and the agent fires as it runs, at the run's chance (§8.3).
   - If the target is out of reach, the order holds while the run goes on. It fires as soon as the target is in reach and in line.
   - If the run ends with the target still out of reach, the walk-in starts then, as built: `CellShooter`'s close already waits for `Walk == null`.
3. **A driven agent (WASD) keeps its drive.** The player owns the motion. Each tick it fires at that tick's gait and heading. A driven agent with no key held fires as standing.
   - Out of reach, the host's walk-in applies in both presets (`HostFight.cs:186` is lab-only today; brief 030 asked for both).
   - Any WASD input ends the walk-in. The order holds, as `weapons.close` already says for a driven agent.
4. **A move after a fire order keeps the fire.** A walk or a run given while a fire order stands keeps that order, and the agent fires on the move. A run no longer ends it.
   - **"You moved it, it stops chasing":** a player move (click, run or drive) given while a fire order stands spends that order's walk-in.
   - If the target is out of reach or line when the move ends, the order holds its aim for `line-lapse-s` (3 s) and then lapses, as an order with close off does.
   - A new fire order gets its walk-in back.
5. **Stop and cease, as built.** Stop on the ground ends the move and keeps the fire. Cease ends the fire and keeps the move.
6. **Stances fire on the move too.** Return and free fire at the gait's chance while walking or running. Before, a stance fired nothing at a run.

So the player reads one line: fire plants a walker; a runner keeps running; the last move you give is the motion. To walk and shoot, fire first and then click the walk.

**What doesn't change:**
- **The Enforcer walking Vale** neither fires nor runs (`fire-carrying-vale`, `run-carrying-vale`, brief 018). Carrying a body or a downed agent: no fire, as built.
- **The lab rifle** never fires moving, walking or running (`noFireMoving`).
- **The draw:** drawing on the move is allowed, at the same `draw-ticks`.
- **Auto-holster:** unchanged (`auto-holster-s` 4, for weapons drawn by fire or panic). A fire order given at a run draws, the gun is "drawn by fire", and it holsters 4 s after its fight ends unless a hostile with a line is in reach. Running neither keeps the gun out nor puts it away.
- **Running is noticed pre-fight** (`run-alert`), and running is free once a fight is on, as built.
- **The kite budget:** a shot fired at a run is a shot. It keeps the shooter's alarm open (`kite-lapse-ticks`) like any shot, so firing at a run keeps the police coming. To break contact, stop firing.

### 8.3 Accuracy by gait and direction (the same for every shooter)
**Gait** is read each tick from the shooter's own step that tick:
- **stand:** no step;
- **walk:** a walk, a walk-in, a driven walk, or any hostile's move (hostiles have one moving pace, which counts as their walk);
- **run:** a run order, or a driven run.

**Direction** (runs only):
- **away:** the step points within `run-away-cone-deg` = **60°** of straight away from the target. In integers, with s the step and t the vector to the target: `dot(s, t) < 0 and 4·dot² ≥ |s|²·|t|²`.
- **toward or across:** anything else. One class: charging and strafing pay the same, and the charge earns its pay in range (§8.5).

**`today` (accumulator, ‰), new modifiers on `weapons.mod`:**
- `mod-moving` **500**: walk. Unchanged in value; its reason text becomes "walk and shoot halves it".
- `mod-running` **250** (new): run, toward or across.
- `mod-running-away` **125** (new): run away.

Walk halves, run quarters, running away an eighth. They apply first, in the place of today's moving step in `Rules.Accuracy`.

| band (trained / untrained) | stand | walk | run, toward or across | run away |
|---|---|---|---|---|
| point blank (≤ 5 m) | 1000 / 750 | 500 / 375 | 250 / 187 | 125 / 93 |
| effective | 750 / 562 | 375 / 281 | 187 / 140 | 93 / 69 |
| long | 400 / 300 | 200 / 150 | 100 / 75 | 50 / 37 |

**`lab` (pips):**
- walk: −1 pip, floor 1, as built;
- run, toward or across: **−2 pips, floor 0** (new `lab-run-pips` 2);
- run away: **0 pips**, whatever the band;
- 0 pips is a new rung, `lab-pip-0` = **100** (○○○○, "one in ten"). Only a running shooter can reach it. Standing and walking shooters keep the floor of 1, so nothing that stands or walks changes.

| band (trained / untrained) | stand | walk | run, toward or across | run away |
|---|---|---|---|---|
| point blank | 4 / 4 | 3 / 3 | 2 / 2 (500) | 0 (100) |
| effective | 3 / 2 | 2 / 1 | 1 / 0 (250 / 100) | 0 |
| long | 2 / 1 | 1 / 1 | 0 (100) | 0 |

**Why the run needs floor 0 in the lab:** with the walk's floor of 1, an untrained shooter in the long band would fire as well at a run as standing (1 pip either way). Circling at 15–16 m would then cost nothing.

**A running target is a little harder to hit:** `mod-target-running` = **850** (new), in both presets. In the lab it is applied to the pip's chance, after the pips.
- It applies to a body that runs this tick: a cell runner (a click or a driven run), a fleeing civilian (`crowd-run-cm-s`), a breaking rival, or any hostile run Pulse adds. A walker is not harder to hit.
- It never applies at point blank: at arm's length nobody dodges.
- **The cap:** it is one fixed step and never stacks with cover. A moving body already has no cover (`CoverMod` returns 1000 for a mover). So running in the open is always easier to hit than standing in cover: 850 against full cover 500 and partial 750. In the lab, cover at the effective ring is 600 → 500 and running is 600 → 510. Meanwhile the runner's own fire drops to a quarter or less. Running is a defence for someone who has stopped fighting, never the best way to fight (§8.6, K1).

**Hostiles under the same rule.** Hostiles fire while they walk in both presets, at the walk's chance. Today `GameAi.cs:210` returns while moving unless the lab's `hostilesFireMoving` is on; the rulebook's `ai` rule text changes from "fires nothing while it moves" to "fires as it moves, at weapons.mod for its gait". This also reverses the "while it walks it fires nothing, as today" line in §7.10 rule 2. If Pulse ever gives a hostile a run, the run's row applies to it unchanged.
- **Why this is required:** without it, `today`'s cell could fire on the move at police who can't, and that is a kite made cheaper.
- **Fallback, symmetric:** if a prepared row passes its line only because rivals now fire on their step to cover, then a step to cover fires nothing, for everyone, cell included (a cover-snap walk). Advances and closes still fire.

### 8.4 Optional, measured first: cover inside point blank (TODAY)
In the lab, cover doesn't count at point blank. In `today` it does (×500 at 1000). For Sky's "head first" (§8.5) to beat a covered target in `today` as it does in the lab, cover should stop counting inside `point-blank-m` in `today` too: "can't miss at arm's length" is that value's own reason. It cuts both ways: an ambush 4–5 m from the cell in cover hits harder.
- **Keel:** measure it as a separate switch.
- **Adopt only if** today's P-A, P-B and P-G stay inside their lines. Otherwise drop it and note it open.

### 8.5 The charge ("head first")
A charge trades wild shots and exposure for a better shot on arrival:
- **On the way** it fires at a quarter (`today`) or −2 pips (`lab`) and is in the open: no cover while moving. It gets only the small running-target step (850), and none inside 5 m.
- **On arrival** it fires at the band it reached: effective, or point blank at 1000 (lab: 4 pips, with no cover or untrained step).
- **What it buys is time to reach.** At 2.8 m/s it closes twice as fast as the walk-in (1.4 m/s). Its fire on the way is mostly ranging: the shots build the count, so the first standing shot lands.
- **What it costs:** the hostiles answer at their standing odds, ×850 on a runner, and inside 5 m at 1000. A charge into equal or greater numbers loses (§8.7, C1b). That is the trade.

### 8.6 Cheap-win check
Arithmetic, not the engine: a 1-D accumulator model at 0.1 s ticks with no geometry, in `out/motive027/s8/arith3.py` and `arith4.py` (scratch, git-ignored; `arith4.py` spreads the six's first targets over the four). Keel measures the real thing.

| row | today: time · hits taken · hits dealt · cell down · foes down | lab |
|---|---|---|
| **C1** four rush a lone guard from 25 m, stop at 12 m | 4.8 s · 1 · 3 · 0 · 1 | 6.2 s · 1 · 2 · 0 · 1 |
| C1, rush all the way to 4 m | 7.8 s · 3 · 4 · 0 · 1 | 7.8 s · 2 · 2 · 0 · 1 |
| C1, walk-in instead | 8.0 s · 1 · 4 · 0 · 1 | 4.6 s · 0 · 2 · 0 · 1 (the lab carbine alone does it; the model has no charge bar) |
| **C1b** four rush 4 officers to 4 m | 14.1 s · 21 · 9 · **4** · 2 | 10.6 s · 21 · 5 · **4** · 1 |
| C1b, rush to 12 m | 9.3 s · 7 · 13 · 1 · 4 | 13.8 s · 12 · 12 · 2 · 4 |
| C1b, walk-in | 12.5 s · 10 · 13 · 1 · 4 | 11.5 s · 5 · 12 · 0 · 4 |
| **C2** six officers at 14 m, the fight hot (all aimed, first targets spread); the cell runs straight away firing | 6 · 0 · 0 · 0 (all six on one agent: that agent down) | 0 · 2 · 0 · 1 |
| C2, two seconds of standing fire, then the break | 11 · 8 · 1 · 2 | 6 · 6 · 0 · 2 |
| C2, stand and fight | 9.1 s · 19 · 16 · **4** · 5 | 8.1 s · 20 · 12 · **4** · 3 |
| **K1** the Enforcer circles at 15.5 m, running across, against 2 officers | 25 s · 6 · 3 · **1** · 1 | 12.6 s · 4 · 4 · 0 · 2 |
| K1, the Enforcer stands there | 7.1 s · 2 · 4 · 0 · 2 | 6.2 s · 2 · 4 · 0 · 2 |

**Against the cheap-win families** (`briefs/027/suture/` and `out/motive027/matrix-0.14.txt`):
- **Kiting** (`kite`, `reset-kite`, `cycle-kite`, `carbine-kite`, `*-runkite`, `km*`) **can't get cheaper:**
  - running across fires at ¼ (`today`) or 0–1 pip (`lab`), while the police fire back at their standing odds ×850. The ratio is ≥ 4 : 1 against the kiter (K1). Circling is three to four times slower than standing and costs more hits;
  - running away fires at ⅛ or 0 pips: suppression, not killing fire. In `today` it is about one hit in 15–20 shots at the effective ring. The hits a break "takes with it" are mostly the counts already built standing, plus the carbine, the only gun still in reach after the first second (C2's rows). That is Sky's "maybe";
  - every runner's shot keeps its alarm open (§8.2), and `today`'s police now fire as they close. So the run-kite rows, which today stay out of reach without firing, gain nothing by firing, and lose the lapse that ended the chase;
  - in the final fight the police go for Vale (lab §7.2), and Vale can't run (§7.3). Unchanged.
- **Police and rivals get the same rule both ways:** one gait table, one running-target step, and hostiles fire as they walk (§8.3).
- **Prepared routes** (P-A, P-B, P-G): the cell's posts are standing fights, so the cell's own numbers don't move. Two things can move them:
  - (a) rivals and police now fire on their walks in `today` (the lab already does, and its P-A and P-B pass);
  - (b) any recorded route that gave a fire order to a walking agent, which now plants it. `loud`, `loud-fast`, `loud-fight`, `loud-left-downed`, `loud-left-standing` and `fight-loud-enforcer-down` have both fire and run orders. Their runs no longer end their fire orders, and their walks stop at a fire order.
  
  The done-check holds the lines: P-A and P-B ≤ 25 %, P-G ≤ 33 %, no op down. Else the §8.3 fallback.
- **"Ammo plenty":** there is no ammunition in either preset. Real-time fire is free (§7.6), so "ammo plenty" is always true. "Energy low" is health in `today`, and health plus the charge bar in the lab: the sidearm at will draws no charge, the carbine draws 20 a shot. A retreat on an empty bar is a sidearm retreat, which is what Sky described. **Nothing new is needed.**

### 8.7 Sky's two cases, as named scenarios (Keel; both presets)
Both run on the fight range (brief 029) with straight open ground, so they replay without the Handover's posts.
- **F8-RUSH, "the whole squad running for a quick takedown":**
  - **Setup:** four drawn agents, the Enforcer with the carbine and three sidearms, 25 m from one trained pistol hostile standing in the open with the fight on. All four run toward it, stop at 12 m, and are given fire orders while running.
  - **Assert:** it is down within **8 s** of the run order. No agent down. ≤ 2 hits taken. In `today` it is down sooner than the same start with a walk-in (predicted 4.8 s against 8.0 s). The lab has no such line: its carbine alone decides the walk-in (4.6 s).
- **F8-RUSH-B, "a charge costs":**
  - **Setup:** as F8-RUSH, against four trained hostiles. Variant A runs to 4 m; variant B is a walk-in.
  - **Assert:** A loses at least 2 more agents than B. The charge doesn't pay when you don't outnumber.
- **F8-BREAK, "run away but take some of them with me":**
  - **Setup:** four agents at 14 m from six trained hostiles in a hot fight, all aimed, their first targets spread over the four. Variant A runs straight away for 20 s with its fire orders kept, and the hostiles close by §7.10. Variant B fights 2 s standing, then does the same.
  - **Assert:**
    - A: no agent down, health lost ≤ 50 %, at most one hostile down;
    - B: at most one agent down, and at least one hostile down;
    - from the same start, standing and fighting to the end loses at least 3 agents;
    - a variant of A with cease takes no fewer hits than A.
- **F8-CIRCLE, the cap:**
  - **Setup:** the Enforcer runs across at 15.5 m from 2 trained hostiles for 30 s, firing.
  - **Assert:** he takes more hits than he deals. Standing at the same spot downs both sooner.

**Order scenarios (unit-sized, no balance):**
- **F8-O1:** a walker given fire in reach stands that tick.
- **F8-O2:** a walker given fire out of reach walks in.
- **F8-O3:** a runner given fire keeps its run and fires at `mod-running` (`today`) or −2 pips (lab).
- **F8-O4:** fire, then a walk: walks and fires at 500.
- **F8-O5:** fire, then a run: the order stands.
- **F8-O6:** a walk-in, then a player move ending out of reach: no second walk-in, and the order lapses 3 s after the move ends.
- **F8-O7:** a driven run fires at the run's chance; a driven agent with no key held fires as standing.
- **F8-O8:** the away cone at exactly 60° and 61° (Keel picks the side for the exact edge and asserts it).
- **F8-O9:** the Vale holder is still refused; carrying a body is still refused.
- **F8-O10:** `today`: a hostile advancing in a fight fires at 500.
- **F8-O11:** a running target takes ×850 outside 5 m and no step inside it.

### 8.8 What the HUD preview shows (Latch, Glyph for the marks)
- **The chip at the current gait and heading:**
  - `today`: "hits 1 in 5 · down in 9 s", from the chance the next shot would have now;
  - lab: the dots now, so a runner sees one dot or none.
  - It updates every tick while driven, because steering changes the heading.
- **A gait mark before the chance:** walk, run, or run away (Glyph: a shape cue, no words).
- **Now → on arrival, when the agent is moving and its move ends inside the weapon's max ring with a line:** the chip adds an arrow and the chance of the first standing shot at the move's end, from that point to the target's position now. For example `today` "1 in 5 → 3 in 4", or lab "●○○○ → ●●●●". This is how the charge reads: a bad shot now, a good one there. Without such an end point there is no arrow.
- **The ring's Fire pill and the hover chip read the same preview.** No new lines of text; the arrow is the only addition.
- **Engine (Keel) for the preview:** `FirePreview` gains `Gait` (`stand`, `walk`, `run`, `run-away`); `AccEnd`, `PipsEnd` and `ShotsToDownEnd` for the move's end; and `TargetRunning`. `fire-running` leaves the refusal codes.

## 9. Anyone can be shot (Sky, 2026-10-21: "players should be able to shoot anyone (think GTA) - nothing more annoying than being restricted in a game")

**Kind: TODAY and LAB.** Sky asked for it. The rule: **every person and every machine that acts can be shot. The consequences come from the systems that already price harm, never from a refusal.** Accidents stay unlikely through the controls of brief 030, not through the engine.

### 9.1 What is refused today, and what happens to each refusal
| refusal (where) | today | from now |
|---|---|---|
| `civilian-target` (engine): civilians, café staff and customers, Inez, the driver, turned contacts | refused without `force` | **Stays only as the order's explicit flag.** The ring's Fire and Ctrl always send `force`, so the player never sees it. Fable: the pop goes. |
| `never-vale` (host, `HostContext.cs:303`; `ShotAt` returns false) | refused at the click | **Gone.** Vale is in the ring with Fire. The engine already allows it: `vale-dead` fails the mission. |
| `fight-over` (engine): a final-fight hostile after `fight.over` | refused | **Gone.** It is an ordinary person again: traced Trail, and violence if a civilian sees it. |
| `target-unknown` for cameras (engine: no gun) | refused | **Cameras become targets** (§9.2). The fight range keeps its own limit to its spawned hostiles. |
| response unit or robot (host maps `unit` to `"security"`, the nearest officer) | fires on someone else | **Fire on the clicked unit's own id** (Latch). The engine already takes it. |
| a knocked-out or restrained person (engine `no-target`, since they are down) | refused | **A shot kills them.** It is takedown kill's consequences plus gunfire. |
| `fire-on-cell` (engine): your own agents | refused | **Kept: the one target refusal.** It is your own hand, not the city; no system prices it, and it has no use. Flagged for Sky. |
| `fire-running` | refused | gone (§8) |
| `fire-carrying`, `fire-carrying-vale`, `no-weapon`, `out-of-range`/`no-clear-line` without a walk | refused | **Kept.** They are about the shooter, not the target. |

### 9.2 Consequences, all from existing systems except two gaps
- **Built:**
  - gunfire: Alert ≥ 2 and the response called. `gunfire-trail` +2 per shooting, `hit-trail` +2 and `down-trail` +2 per person;
  - violence seen by a civilian: Alert 3 and a Menace step (`traceable.violence`);
  - the debrief's public line;
  - Inez or a contact shot: `Turned = false`. She loses her calls and warnings, and a lover's summon goes;
  - the driver shot after Vale leaves the Office: `driver-shot`; before, the car never leaves (`driverKnownDown`);
  - Vale: `vale-dead`;
  - the guard: `guard-shot` and his missed check-in.
- **Gap 1 (Keel): a person shot down outside the final fight leaves no body today.** `CivilianHit` only sets `Down`. From now:
  - such a person becomes a **dead body of brief 013** where they fall;
  - it is found by `body.found`, with Standing `found-kill-standing` −1, a case (`case-close-missions` 3), and the ties' `body.grieved` (a turned tie takes `saw-violence`);
  - it can be carried and stashed like any body.
  
  The final fight's downed stay as they are: the fight is untraced.
- **Gap 2 (Keel): cameras.**
  - New value `health-camera` = **1** (×`healthScale` in the lab: 10, one sidearm hit).
  - A shot-out camera is dark for the rest of the mission: it watches nothing, and a loop on it is moot.
  - The shot is gunfire, so Alert ≥ 2 and the response. Its reporter is the existing `Reporter`: the camera reports a shooter that stood in its cone when it was shot.
  - No hit or down Trail (it is not a person) and no Standing step. It costs the shooting's `gunfire-trail` once.
- **Not targets, open for Sky:** cars, doors, cabinets and props. No system prices their harm yet. Shooting the getaway car would need a whole rule.

### 9.3 Keeping accidents unlikely without blocking anyone
- **Brief 030 stands:**
  - a quick right click never fires on a non-hostile;
  - Fire on a person or machine is in the ring (dashed, with the loud mark) or on Ctrl;
  - the sweep never moves fire onto a non-hostile;
  - stances never pick a non-combatant.
- **New, one line: an order whose shot would fail the mission always takes the 0.5 s hold** (`loud-commit-hold-ticks`), loud or not. That is Vale, and the driver while Vale is out. Today the card's hold shows only when the order makes the run loud. The card says what fails, in one line: "Vale dies. The mission fails."

### 9.4 Fable
**Lines that go:**
- "never fire on Vale: the mission fails" (becomes the hold card's line);
- "a running operative doesn't fire: walk or stop first";
- "runs: the fire order ends";
- "the fight is over: … is no target";
- the Ctrl hint for civilians.

**Lines that come:**
- the hold card's fail line for Vale and the driver;
- "The camera goes dark.";
- a body line for a person shot dead, if brief 013's are worded only for takedowns.

Check them against the plain-copy rules.

### 9.5 Scenarios (Keel)
- **S9-VALE:** forced fire on Vale holds 0.5 s, then `vale-dead`.
- **S9-CROWD:** a café customer shot dead in sight of a civilian: Alert 3, a Menace step, a body that is later found, a case, and `found-kill-standing`.
- **S9-INEZ:** turned Inez shot: `Turned` false, the guard's summon is refused, and between missions the guard grieves.
- **S9-CAM:** a camera shot: dark, Alert ≥ 2, `gunfire-trail` once, no hit Trail.
- **S9-AFTER:** fire on a broken rival after `fight.over` is taken and traced.
- **S9-KO:** a knocked-out body shot: a kill.
- **S9-SELF:** fire on an own agent is still `fire-on-cell`.
- **S9-UNIT:** fire on a robot or unit targets that one.

### 9.6 Handoffs
- **Keel** (engine 0.15):
  - **Inputs:**
    - §8.2 rules 1–6 in `Fire`, `Move`, `CellShooter` and the drive;
    - §8.3: new values `mod-running` 250, `mod-running-away` 125, `mod-target-running` 850, `run-away-cone-deg` 60, `lab-run-pips` 2 and `lab-pip-0` 100 (each `[S]`, `proposed: true`, reason naming this section);
    - `Rules.Accuracy` and `LabPips` take a gait and a running target;
    - hostiles fire as they walk in both presets: the rulebook `ai` text and §7.10 rule 2's text;
    - `mod-moving`'s reason text;
    - the §8.4 switch, measured and not adopted until it passes;
    - the §8.8 preview fields;
    - §9.1 (lift `fight-over`; a shot on a knocked-out body), §9.2 gaps 1 and 2 (`health-camera` 1), and §9.3's always-hold for mission-failing shots in `GoingLoud`;
    - scenarios F8-RUSH, F8-RUSH-B, F8-BREAK, F8-CIRCLE, F8-O1 to O11 and S9-*;
    - adversarial rows in `fight-lab`: `L3:runfire-kite` (the run-kite rows with their fire kept at a run), `L3:circle-kite` (K1 at the car), `L3:charge` (all four rush the officers), and `P-G:runfire`.
  - **Done-check:**
    - every scenario passes;
    - no adversarial row in `out/motive027/matrix-0.14.txt` gets cheaper in either preset: no new win, and no win with less health lost;
    - lab matrix L3 still wins heavy;
    - `today` P-A and P-B ≤ 25 % and P-G ≤ 33 %, all with no op down (else the §8.3 fallback);
    - the hash diff lists each `today` run that moves, with why;
    - `dotnet test` green;
    - `bun run typecheck` clean.
  - **Next owner:** Waymark, then Suture.
- **Waymark:**
  - **Inputs:** Keel's hash diff for the recorded routes, above all the loud family (§8.6 b).
  - **Done-check:** each moved route is re-recorded, or its order changed (fire first, then the walk) so it keeps its intent.
  - **Next owner:** Suture.
- **Latch:**
  - **Inputs:**
    - the chip at gait and heading, with now → arrival (§8.8);
    - no host-side `fire-running` refusal;
    - the driven walk-in in both presets;
    - Vale and every person in the ring with Fire (§9.1);
    - `unit` fire on the clicked id;
    - the always-hold card.
  - **Done-check:**
    - the chip equals the engine's next shot in F8-O3 and F8-O7;
    - the arrival value equals the first standing shot in F8-RUSH;
    - the HUD budget at rest is unchanged (`bun run verify`).
  - **Next owner:** Sky, on the fight range and one loud Handover.
- **Glyph:** the gait marks (walk, run, run away) and the arrow, as shape cues.
- **Fable:** §9.4.
- **Pulse (FYI):** hostiles now fire on their walks. If the stop-point or the advance reads wrong with fire on the move, the motion is yours (§7.7).
- **Suture:**
  - **Inputs:** §8.6–8.7 and §9.5.
  - **Done-check:** a retest of the kite, charge and break-contact families by hand and by script, including whether running in circles is ever the best defence.
- **Aster, for Sky:**
  - (1) `fire-on-cell` is the one target refusal kept;
  - (2) cars, doors and props are not targets yet;
  - (3) hostiles now fire as they walk in `today`. It is needed for "same rule both ways", and it makes `today`'s loud fights harder;
  - (4) §8.4 (cover inside point blank) is measured and not yet adopted.

### 9.7 Sky's answers (2026-10-21)
1. **Own agents:** can't be shot (the refusal stays).
2. **Cars:** shootable, as in Syndicate. Doors and props: no, perhaps later with a destructible world.
3. **Hostiles firing as they walk in `today`:** OK.
4. **Point blank ignores cover in `today` (Sky: yes),** as in the lab; Keel adopts it and reports the prepared routes (P-A, P-B, P-G) against their lines; if one falls outside, it comes back to Sky.

## 10. Rulings on engine 0.15.0 (2026-10-21)

**Inputs:** Keel's `briefs/027/keel/fight-lab.md` §12, `matrix-0.15.txt`, `route-diff-0.15.txt`, and the coordinator's note.
**My probes:** a scratch copy of the engine in `out/motive027/s10/tree/` (git-ignored; nothing in `sim/` edited), built with four probe-only environment switches:
- `SETTLE`: §10.2;
- `HWF=0`: hostiles don't fire walking in `today`;
- `FOCUSALL` and `PGPOSTS`: the §7.11.3 (b) support player and (c) posts, applied to `today`.

Outputs: `out/motive027/s10/matrix-*.txt` and `sum-*.txt`. Every probe is `fight-lab --rulebook contracts/rules/rulebook.json` from the repo root.

**Not tuned around the prepared lines.** No value below moves to make a line pass.

### 10.1 Is `today` too hard now? Plainly
- **Quiet, prepared play, with a player who supports the Enforcer:** no, it isn't too hard. With the §7.11.3 (b) focus (whoever fires on the Vale holder first, then the blocker, then the nearest) and P-G's posts at cover within 15 m, `today` P-G is a **success at 22 %, no op down**, and P-A is 0 % (probe `SETTLE=1 PGPOSTS=1`). With the same focus, P-B drops to 5 % health. Treat that P-B number as unconfirmed: the same scratch run also reached Alert 2 in P-B, which I haven't traced (Keel).
- **Quiet play with the nearest-first scripted player** (what the matrix runs in `today`): **yes, harder.**
  - P-G loses Vale. The blocker and rival-3 put six hits on the Enforcer walking Vale while the three shoot the nearest rivals.
  - P-B costs an op (44 %).
  - The quiet recorded routes go from 44 % to 66 % (`route-a-pressure`, the route-g family). Their outcomes are unchanged.
  - Part of this predates §8: at 0.14.1, P-G was already at 55 % with an op down, and P-B at 33 % with an op down.
- **Loud play:** **yes, too hard.** No loud row wins in either preset. L3 already failed at 0.14.1, and now the v0.24 loud reference N10 loses at security level 1 (3 down, sealed 5:48.5; a win at 5:00.3 before).
  - The cause is hostiles firing as they walk. With only that switched off in `today` (`HWF=0`), N10 wins at 5:00.3 again. P-G is 38 % (support focus at the crew posts: the scratch tree turns P-G's focus on in `today`) and P-B 38 % (nearest-first), and no adversarial row gets cheaper.
  - This breaks §7.1's "loud must stay winnable".

**For Sky (via Aster).** Firing on the move, for both sides, made the game more lethal for whoever doesn't answer the shooter. The prepared game holds if the player supports the Enforcer, which is Sky's own "they have to support the enforcer". The loud game no longer holds. Two choices:
- **(A) Keep both sides firing on the move** (Sky's answer 3 stands). Loud becomes a losing route at routine security: "a last resort", as §05 says, not a route. The v0.24 reference "loud 4:23.5" is retired.
- **(B) Hostiles don't fire on the move in `today`** (answer 3 reversed; the lab keeps it). N10 wins again, nothing in the matrix gets cheaper (with §10.2 in), and the cell has the edge on the move.
- **My lean is A.** It is the fair world Sky asked for, and the kite rows depend on it.

If Sky wants loud winnable under A, it needs a cell edge, which is a design change, not a number: for example, the Enforcer's carbine counting as trained on the move. That is unmeasured.

### 10.2 The new cheap win: a run doesn't reset the aim, but stopping from one costs a settle (TODAY and LAB)
- **Cause** (Keel): the run no longer ends the fire order. So the kite step keeps its aim and never waits `aimNew`. Before, every step re-issued fire and waited 1.0 s.
- **Rule: settle.** A shooter whose run ends (it stands or walks again, after a click run or a driven run) waits `aimNew` before its next shot. It keeps the target and the pair's count.
  - `NextShot = max(NextShot, Tick + aimNew)` on the tick the run ends.
  - No new value: `aim-new-ticks` 10 (`today`), `lab-aim-new-ticks` 5.
  - The same rule applies to every shooter that runs.
  - It doesn't reset the count. A reset would be the reset-kite exploit again (in `today` it hands back 500).
- **Measured** (`SETTLE=1`, full matrix, `matrix-settle1.txt` against `matrix-settle0.txt`):
  - the km10 row fails, enforcer-down at 77 % (`today`);
  - no prepared row moves;
  - no adversarial row gets cheaper; `L3:charge` still fails.
- **Reads as:** "stop from a run and you take a beat to aim". A charge pays it once, on arrival.
- **Preview:** the arrival value in §8.8 includes the settle, so the first standing shot's time is honest.

### 10.3 N10 and the loud rows
- **The rule is not changed for N10.** The scenario keeps recording, not asserting, until Sky picks A or B (§10.1).
- **Under A,** N10's assert becomes "the loud fight ends in a heavy debrief or a failure (sealed or the Enforcer down)", and §7.1's "at least one loud row wins heavy" is dropped from the done-checks. Under B, the win comes back as the assert.

### 10.4 My §8.7 predictions: what was wrong
- **F8-RUSH's 8 s line came from my 1-D toy,** which has no draw, no reaction time and no geometry. **I'm dropping the absolute line.** The line that matters is relative, and it was met: in `today` the rush is sooner than the walk-in, 8.3 s against 12.9 s. The settle (§10.2) adds about 1 s and keeps it well under. No agent down and ≤ 2 hits were met.
- **F8-BREAK, "cease takes no fewer hits":** Keel is right, I inverted it. The line is "firing while breaking takes no fewer hits than ceasing", and it is met: 10 against 0 in `today`, 11 against 10 in the lab. So it agrees with §8.2: to break contact, stop firing.
- **F8-BREAK, 2 down:**
  - **Geometry:** the range's far wall stops the straight run after about 7 s. After that the cell runs along the wall, across, not away, and the walking police (2.2 m/s) close on it.
  - **New setup:** a straight lane of at least 60 m behind the cell, or the asserts measured at the wall.
  - **Re-judge:** if it still loses 2 with firing kept, that is the true price of "taking some with you". I'll put it to Sky as the price, not tune it. Ceasing is the clean break (0 hits in `today`).

### 10.5 The quiet recorded routes (44 → 66 %)
- **Expected under rule A.** The blocker fires on its lane walk, so it hits the Vale holder at 281 ‰, where before it didn't fire.
- **Outcomes and fail codes are unchanged.**
- **Waymark:** re-record their expected health. If a route's intent is "supported", its fire orders go to whoever fires on the Enforcer. That is the taught play, not a number change.

### 10.6 Readings confirmed
1. **A person shot dead is the same incident as the shooting.** The act starts landed: one dispatch, one Alert rise, one witness Trail.
   - **Standing moves once per incident.** A seen killing takes the violence step at sight. The later find of that body adds no second step. An unseen killing takes `found-kill-standing` when found.
   - **A case opens either way.** A seen killing counts as found at once.
2. **Trail is the gunfire's only:** `gunfire-trail` once per target shot, `hit-trail` and `down-trail` for the person. No extra kill Trail.
3. **A fire order given in the final fight ends at `fight.over`, as built.** A new order after it is taken and traced. Fleeing rivals don't become traced Trail by accident.
4. **The guard shot keeps his own missed-check-in path.**

### 10.7 Cars (Sky §9.7: "shootable, as in Syndicate")
Every car on the street is a target: parked, moving, the official car, the rival car and the getaway car. Keel builds it next round. Every value is `[S]`, `proposed`, with a reason naming this section.

| value | today | lab (×`healthScale`) | rule |
|---|---|---|---|
| `health-car` | 10 | 100 | 10 pistol hits or 5 carbine hits in `today`; 10 sidearm or 5 carbine in the lab |
| `car-disabled-at` | 5 | 50 | at or below it the car is **disabled**: it stops where it is and can't drive. Smoke, a world mark, no text |
| `car-burn-s` | 6 | 6 | at 0 it **burns** for 6 s, then **explodes**; the blast ring shows on the ground while it burns |
| `car-blast-m` | 5 | 5 | one ring |
| `car-blast-damage` | 3 | 30 | to everyone inside the ring with a line to the car, cell included (blasts don't ask whose they are); occupants still inside take it too |

- **Occupants:**
  - disabled: the occupants get out on their side and act as people do (witnesses, targets);
  - burning: they get out at once, and anyone who can't (a restrained body) is still inside at the blast;
  - Vale inside at the blast: `vale-dead`.
- **The getaway car:**
  - disabled: **Board and Go are refused, and the mission fails** with a new code, `getaway-lost` ("the getaway car is wrecked"). Unless an operative is inside, every exit is gone;
  - an operative inside at the blast takes it as an occupant.
- **The official car:**
  - disabled before Vale boards: it stays at the pickup, and the Take there is still possible;
  - with the driver inside: the driver gets out as an occupant.
- **The rival car:** disabled, a broken crew leaves on foot (`rivalGone` unchanged).
- **Costs, all existing:**
  - shooting the car is a shooting: `gunfire-trail` once, Alert ≥ 2 and the response;
  - the car itself takes no hit or down Trail and no Standing step (property);
  - the blast is violence: seen by a civilian, Alert 3 and a Menace step;
  - each person it hits is hit by the cell (`hit-trail`, `down-trail`, a body if they die).
- **Accidents:** hostiles never choose a car as a target. A car is never a quick-click default; Fire on it is in the ring or on Ctrl (brief 030), and a car with Vale inside, or the getaway car, takes the always-hold (§9.3).
- **Cars stay cover.** A car's cover point is gone when it explodes.

### 10.8 Handoffs
- **Keel:**
  - **Inputs:**
    - §10.2's settle (both presets; scenario F8-SETTLE: a runner given fire, its run ending, waits `aimNew` and keeps the count);
    - §10.4's F8-BREAK setup, and the inverted cease line;
    - §10.3's N10 recording;
    - trace the Alert 2 in P-B under the support player (§10.1);
    - §10.7's cars (values, the `getaway-lost` code, and scenarios: a car disabled, burned and exploded with people in the ring, Vale in the official car, the getaway car lost);
    - once Sky has picked A or B (§10.1), build it.
  - **Done-check:**
    - the km10 row fails in both presets;
    - no adversarial row in `matrix-0.15.txt` gets cheaper;
    - F8-RUSH's relative line, F8-BREAK (new setup) and F8-SETTLE hold;
    - the car scenarios pass;
    - `dotnet test` green;
    - `bun run typecheck` clean.
  - **Next owner:** Suture.
- **Keel and Suture:** run the `today` matrix's prepared rows with the support player as a second column. It is the taught player, not a pass condition: P-G 22 % by my probe.
- **Waymark:** §10.5.
- **Fable and Glyph:** `getaway-lost`'s line; the car's smoke, flame and blast-ring marks (shape cues, no text).
- **Aster, for Sky:** §10.1's A or B, and whether loud may lose at routine security.

### §10 decision (Sky, 2026-10-21)
- **§10.1: A.** Sky: "I can try A - will play the game again when everything settled." Both sides keep firing on the move in `today`; loud may lose at routine security; N10 (the v0.24 loud reference) is retired, not fixed. Provisional until Sky plays the settled build.
- Sky's play note, open for Glyph and Motive: "shooting seems autonomous once started - is that how Syndicate worked?" (a draw sets the agent to at will, and a fire order runs until the target is down, a cease or the fight's end).

### 10.9 Keel: built, engine 0.16.0 (2026-10-21)
**`today` changes, each under Sky's recorded decisions** (the settle and cars are directions Sky approved, with Motive's numbers; A is the §10 decision above):
1. **The settle (§10.2), both presets.** `weapons.settle` in the rulebook (rule text, no new value: `aimNew` 10 ticks, the lab's 5). A shooter whose run ends waits `aimNew` before its next shot. It keeps its order, its target and the pair's count.
2. **Cars (§10.7), both presets.** `car.target` and the values `health-car` 10, `car-disabled-at` 5, `car-burn-s` 6, `car-blast-m` 5, `car-blast-damage` 3, all `[S]`, `proposed`, reason §10.7. The lab multiplies health, the disable point and the damage by `healthScale`. New events `car.shot` and `car.blast`, and the fail code `getaway-lost` (why `wrecked`). The targets are `car:getaway`, `car:official` and `car:rivals`; the rival car is a target only once it has parked.
3. **N10 is retired, not fixed (§10.1 A, §10.3).** Its assert is now §10.3's: a heavy debrief, or a failure (sealed, or the Enforcer down). §7.1's "a loud row wins heavy" is no longer a done-check. There is no switch for B.
4. **F8-BREAK runs on the range layout `lane` (§10.4).** This is the open layout on a 90 m floor (`tools/sim/range.ts`). The cease line now reads the right way round. The open and dense sites are byte-identical.

**Drift, declared.**
- Recorded runs:
  - 11 of 278 moved (`hashes --start-text BASE` against a baseline copy taken before the first edit): N21, N159, S05, S19, the six loud route scripts and `briefs/011/motive-fights/loud-carry`. The settle moved all 11.
  - Cars moved 0 of 275.
  - The range results and checkpoint counts are unchanged. Every range hash moves by the inputs hash, as any rulebook change does.
- `route-diff`: 0 of 38 routes moved in outcome, fail, end, Trail, Alert or health (`briefs/027/keel/route-diff-0.16.json`).
- DP1 re-recorded (same orders, start, until and outcome).
- The dialogue drift ledger is declared by `drift:auto` (no replay moved). `bun run verify` passes.

**Measured.**
- **Matrix** (`briefs/027/keel/matrix-0.16.txt`):
  - In `today`, km10 now fails: the Enforcer is down, 77 % health lost (it won at 11 %).
  - P-A, P-G and P-B are unchanged in both presets.
  - No `today` or `lab` adversarial row got cheaper.
  - In the switch-off columns, one row newly wins cheap: `lab-snap-off` `tight-step200-focus-norivals-at236-runkite`, 22 %. `lab-field-on` km15 now wins heavy, at 88 %.
- **Suture's 3,792-policy sweep** (`briefs/027/keel/sweep-0.16.txt`):
  - `today`: wins fell from 524 to 216, and cheap wins (under 40 %) from 335 to 26.
  - 10 of those 26 are newly cheap, all `norivals` runkite. The cheapest is `km20-fm15-step250-norivals-at220-runkite` at 16 %.
  - The lab has 13 newly cheap; `lab-field-on` has 27.
  - Cause, probed on the cheapest: the settle re-times which agent fires first, so the kite pair walks its 20 m and closes together. Knife-edge policies flip both ways (54 new wins, 362 lost in `today`). The settle is not closed against the sweep.
- **F8-BREAK on the lane, both presets:**
  - A (break at once): 0 down, 0 hits.
  - B (2 s of standing fire, then break): 0 down, and 0 hostiles down, so the line "at least one hostile down" is missed.
  - Standing and fighting to the end: 4 down.
  - Firing against ceasing: 0 hits against 0.
  - Breaking is free once the lane is long enough.
- **F8-RUSH:** 8.3 s against 12.9 s for the walk-in, so the relative line is met.
- **F8-SETTLE** holds in both presets.
- **Car scenarios** (CAR-SHOT, CAR-BLAST, CAR-BLAST-LAB, CAR-VALE, CAR-GETAWAY, CAR-RIVALS) pass. `dotnet test`: 1219 of 1219.

**The P-B Alert 2 under the support player (§10.1)** is not gunfire. The final fight is untraced. The fight ends sooner (7:34.1 against 7:41.1), but the scripted player boards one agent at a time. That leaves the Enforcer holding Vale beside the getaway car from 7:34 to 8:47.8. The crowd comes back from shelter at about 8:28, and p-records-office-57 sees "a body carried" at 8:36.4. The report lands at 8:41.4: Alert 2, +1 T and a Menace step. This is the fight-lab player's slow, serial extraction, not a fight cost. Scripts: `out/keel037/pb/`.

**Readings to rule on (Motive).**
- The getaway car fails at disabled, so §10.7's "an operative inside at the blast" can't happen.
- The official car disabled stays put, but `vale-leaves` at the close still says Vale leaves in it.
- The street's parked and passing cars aren't simulated, so they aren't targets.
- Burning starts at 0 health, as in your table. The disable comes first, at 5.

**Handoffs.**
- **Fable** (`contracts/missions/handover/text.json` and `text.schema.ts` FAILS):
  - `getaway-lost`: a failcam caption and a tip for why `wrecked`, plus the card's fail line.
  - Log codes, if a line is wanted: `car.hit`, `car.disabled`, `car.out`, `car.burning`, `car.exploded`, `car.blast-hits`, and `<op>.hit.blast`.
- **Glyph:**
  - Marks: smoke on a disabled car, flame while it burns, the 5 m blast ring on the ground while it burns, and the wreck. Shape cues, no text.
  - They read `Game.CarOf(id)` (`DisabledAt`, `BurningAt`, `BlastAt`, `Blown`) and `R.CarBlastCm`.
- **Latch:**
  - The target ids are `car:*`. Host reads: `CarAt`, `CarOf`, `CarDisabled`, `CarBurning`, `CoverStanding`.
  - `SimBundle` and `tools/sim/unity.ts` don't need the `lane` layout (it is headless only).
- **Waymark:**
  - Re-record the six loud route scripts' checkpoints. Their values don't move.
  - §10.5's quiet-route health.
- **Suture:** rerun the sweep's newly cheap `norivals` policies; the matrix's support column is still open.

## 11. Follow fire and the trigger (Sky on firing, 2026-10-21)

**Inputs:** brief 024, "Firing" and "Sky on firing, 2026-10-21"; §10 decision A (both sides fire on the move); `GameCombat.cs` (`CellShooter`, `Aim`, `Shoot`, `StanceTarget`).
**Probes** (scratch tree `out/motive027/s11/tree/`, nothing in `sim/` edited; Keel's tree was mid-build, so today's base P-G already fails there):
- `probe1.txt`: fight range, lane layout, four drawn agents against 1, 2, 4 and 6 trained officers at 14 m and 10 m. Policies: split, focus, two locks plus two following, and one shooter alone.
- `matrix-base.txt` against `matrix-focusone.txt`: the full fight-lab matrix, the second with `FOCUSONE=1`. Under it, every scripted agent with reach locks one common target.

**Kind: TODAY and LAB.** This covers the rules for the driven agent, its followers and locks. Every number below already exists, except the radius.

### 11.1 Near: one ring, 8 m around the driven agent
- **Near** means within **8 m** of the driven agent (new value `follow-m` 8, `[S]`). The engine checks it with integer cm (`D2 ≤ 800²`).
- It is the E prompt's reach and `fight-hold-m`. It covers a squad that came when called (the Come spacing) and leaves out an agent posted at another door.
- **Shown** as a faint ground ring round the driven agent, only while it aims. Each follower inside the ring wears the follow mark (Glyph: a shape, no text).
- **Who follows:** a cell agent that is drawn, not locked, not returning fire, not busy, not carrying and not inside. It must also have its own reach and line to the target. A holstered agent never draws to follow: the draw stays the player's F.
- **A follower never walks.** It fires from where it stands. A follower out of reach or line just doesn't fire, and its mark stays hollow.
- **Rejected: the same lane of fire.** It changes with every aim and can't be seen.

### 11.2 Joining, release and aim-new
1. **A joining agent pays aim-new** like any new target (`Aim`): `aim-new-ticks` 1.0 s today, `lab-aim-new-ticks` 0.5 s in the lab. In today it also takes `acc-start` 500. In the lab it takes the pair's count.
   - No waiver. The 3 s `ai-reacquire-s` is for hostiles regaining a target they lost, not for taking a new one.
   - The §10.2 settle applies to followers too.
2. **Release holds the aim. Nothing resets.** When you let go, a follower (follow off) and the driven agent itself keep their target and count. Press again on the same target and they fire on cadence, with no aim-new and no reset.
   - The aim drops only when the agent takes another target, leaves the ring, is engaged, or holsters.
   - **This is required.** Without it, in today every tap would re-aim to `acc-start` 500. An untrained shooter's first shot (500 + 562) always hits, so tapping would turn a 56 % gun into a 100 % gun: the reset-kite exploit, through the trigger.
3. **Follow on:** a press, or a change of target while pressed, locks every follower in the ring at that tick. The lock is a fire order with its walk-in spent (§8.2 rule 4), so it lapses after `line-lapse-s` 3 s out of line and never walks off. Once locked, the follower is engaged and ignores the ring.

### 11.3 Focus fire: what four guns on one target do
**today** (the range, 14 m, trained officers):

| foes | split locks | focus (all on one) | two locks + two following | one shooter alone |
|---|---|---|---|---|
| 1 | 1.1 s, 1 hit taken | the same | the same | Enforcer 2.2 s · Scout 4.1 s |
| 4 | 7.1 s, **1 down**, 72 % lost | **5.6 s, 0 down, 44 %** | 7.1 s, 1 down, 61 % (10 m: 6.3 s, 0 down) | loses |
| 6 | loses, 4 down | loses, 4 down | loses, 4 down | loses |

- **Why focus wins in today:** with `acc-start` 500, each fresh shooter–target pair's first shot hits. Four fresh guns put 4 hits on a 3-health officer, so one officer falls per pistol cadence (1.1, 2.6, 4.1, 5.6 s).
- **In the lab, focus is worse:** 4 against 4 takes 7.6 s with 1 down, against split's 5.6 s with 0 down. `lab-acc-start` 0 makes the first shots range, and the fourth hit is overkill.
- **Matrix with every player focusing (`FOCUSONE`):**
  - no loud or adversarial row changes its result in either preset;
  - only prepared rows move: today P-B goes from 44 %, 1 down to 5 %, 0 down (but reaches Alert 2, as the §10.1 untraced Alert did); lab P-B goes from 50 to 22 %; lab P-G from 33 to 22 %;
  - today P-G gets worse, 55 → 72 %: weakest-first ignores who is shooting the Vale holder (the §7.11.3 support focus is the taught one).
- **The rush on a lone guard is cheap as a fight, and I'm reporting it, not tuning it.** Four on one at 14 m drops him in 1.1 s for one hit taken; the F8-RUSH run takes 8.3 s against a 12.9 s walk-in.
  - This was already true with four fire orders. Follow only makes it one press.
  - Its price is the shooting, not the fight: +6 T for the guard, Alert ≥ 2, the response, and a Menace step if it is seen.
  - It's a real option, never a free one. Focus doesn't save 4 against 6.

### 11.4 The trigger tap and the cadence: keep 1.5 s
- **Keep `pistol-every-ticks` 15 and `carbine-every-ticks` 20.**
  - A pistol at 1.0 s in today drops an op to two trained officers in 2.0 s, under the 2.5 s floor (§1).
  - The fallback, if Sky still finds it sluggish after play, is the lab sidearm package as a whole (1.0 s, health ×10, `lab-acc-start` 0), already measured. Today's pistol is not changed alone.
- **A tap is held until its shot.** Any press fires exactly one shot, at the first ready tick within one cadence. A press during the cooldown is never dropped, and that one shot is the most it buys (a buffer of one, not a queue).
  - The shot is lost if, before it fires, the agent aims at something else or the target goes.
  - Glyph: the reticle shows the cadence refilling, so the rhythm can be learned.
- GTA feel comes from four guns and the tracers, not from a faster pistol.

### 11.5 Determinism: confirmed, with three conditions
Following is a pure function of the state and the logged inputs (`trigger`: op, on/off, target id or aim point, tick; and the drive samples). It holds if:
1. **the follow setting is an input:** in the start, or logged as an order on the tick it changes, because it changes what the engine does;
2. **the engine derives the followers**, in op id order with integer distances, and logs them (`fire.follow`, `fire.lock`). The host never issues them as orders, so a replay doesn't depend on the host's version;
3. **an aim point is turned into a target by the engine**, not the host.

The tap buffer (§11.4) and the held aim (§11.2) are both derived from on/off ticks.

### 11.6 Handoffs
- **Keel:**
  - **Inputs:** the §11.1 to §11.5 rules; `follow-m` 8 (`[S]`, `proposed: true`, reason naming this section); the `trigger` input and the logged follow setting.
  - **Scenarios:**
    - F11-NEAR: 7.9 m follows, 8.1 m doesn't;
    - F11-HOLSTER: a holstered agent never follows;
    - F11-JOIN: a joining agent waits aim-new;
    - F11-TAP: a tap-tap-tap in today keeps the count, with no reset to 500;
    - F11-BUF: a press in the cooldown fires once, at the ready tick;
    - F11-LOCK: follow on locks those in the ring at the press, and the lock never walks;
    - F11-REPLAY: hash-identical.
  - **Next owner:** Suture.
- **Glyph:** the ring, the follow mark and the cadence refill.
- **Aster, for Sky:** (1) in today, focus fire wins 4 against 4 cleanly because the first shot always lands; is that the edge you want? (2) is 1.5 s right once the tap buffer is in?

### 11.7 Keel: built, engine 0.17.0 (2026-10-21)
**A `today` change under Sky's recorded decision** ("Sky on firing, 2026-10-21", brief 024; his model: a GTA-style trigger on the driven agent, locks for the others, agents near you that are not engaged fire your target). Both presets. Rulebook: `weapons.trigger` (rule text), `weapons.follow` = `follow-m` 8 (`[S]`, `proposed`, reason §11.1). Engine `sim/Suborned.Sim/GameTrigger.cs`.
1. **The `trigger` input** (order `{"do":"trigger","op","target","on"}`, `force` for the Ctrl case; refused `not-driven` for an agent not driven). Pressed: fires on cadence while it has reach and a line. Each press fires exactly one shot at the first ready tick (`Gun.TrigTap`, a buffer of one), lost on a change of target, the target gone, or no reach or line when the gun is ready. Release keeps the target, the aim and the count (§11.2 rule 2: the tap reset is closed). A press ends the agent's own fire order; a fire order, cease, holster, the agent down or control ending ends the trigger. While it has a trigger target the driven agent's stance doesn't fire.
2. **Locks** are fire orders. A lock counts as engaged only while its target is up (a press on the tick a lock's target falls takes that agent; found by the F11-FOCUS log, see below). Drawing sets `return` in `today` (already so; the lab's two stances draw at will, unchanged: reading 2).
3. **Follow** (§11.1): derived each tick in op id order, integer cm. Setting `Start.followLock` or a logged `{"do":"follow","op":"cell","on"}`. Off: followers fire only while the trigger is held and keep their aim on release. On: a press, or a change of target while pressed, locks each follower with reach and a line (walk-in spent; `fire.lock`). Logged `fire.follow`, `fire.unfollow`, `fire.lock`.
4. **Cadence unchanged** (pistol 15, carbine 20 ticks). **Host reads (Latch):** `FollowersOf(OpId)` (who follows, line or not), `LockOf(OpId)`, `FireStateOf(OpId)` (Mode trigger, trigger-held, lock, follow, follow-held, return, free; Target; Firing for the tracer; Hollow for the follow mark; Shot this tick; NextShot and Every for the refill; Lead), `FollowRingCm`.

**Scenarios** (range, lane): F11-NEAR, F11-HOLSTER, F11-JOIN (+LAB), F11-TAP (+LAB), F11-BUF, F11-LOCK, F11-REPLAY, F11-FOCUS, F11-RUSH, F11-SPAM. All checks pass and all replay identically. The §11.3 lines are met: focus 5.6 s with 0 down, split 7.1 s with 1 down, the rush 1.1 s for 1 hit. Focus by follow (one press per target, follow on) and the held trigger (follow off) also give 5.6 s with 0 down.

**Drift, declared.** §11 alone moved no recorded result: `hashes --start-text BASE` gave 284 of 284 identical against `out/keel038/base`, and `range hashes --start-text` (new option) 42 of 42. Every inputs hash moves with the rulebook. DP1 re-recorded (same orders, start and outcome). The dialogue drift ledger was declared by `drift:auto`; `bun run verify` passes.

**Readings to rule on (Motive).**
1. A holding agent follows. §11.1 doesn't exclude it, and excluding it would leave the lab nothing that can follow. In the lab a lock clears hold, as a fire order does.
2. In the lab a draw is at will (stances two), so a drawn lab agent with a combatant in line is engaged and never follows. Sky's "a draw sets return" holds in `today` only.
3. Taking control keeps a standing fire order (§8.2 rules 3 and 4). Glyph's "taking control drops it" was not adopted. A trigger press ends it.
4. **Adversarial (F11-SPAM, line missed):** an untrained shooter that taps the other target the tick after each shot hits 2 of 2 against the hold's 1 of 2, because each change of target resets to `acc.start` 500 and 500 + 562 hits. Fire orders allow the same thing today; the trigger makes it one key. For the trained Enforcer it gains nothing (4 of 4 each). Not tuned.
5. Aim points: the engine takes a target id only. The host resolves the reticle and the id is logged. Resolving a point in the engine needs a snap radius, which is Motive's number.

**Handoffs.** Latch: the host reads above. Fable: lines for `not-driven` and `follow-not-cell`. Glyph: ring, follow mark, refill from `FireStateOf`. Suture: trigger policies for the sweep (no current policy touches §11).

## 12. Rulings on engine 0.16.0's sweep (2026-10-21)

**Inputs:** §10.9; `briefs/027/keel/sweep-0.16.txt`, `matrix-0.16.txt`, `route-diff-0.16.json`.
**Probes** (scratch tree `out/motive027/s12/tree/`, a copy of engine 0.16.0 with its rulebook snapshot, inputs `b7bbe8ecebb010d3`; nothing in `sim/` or `contracts/` edited):
- `sw/<preset>.0.txt`: the 3,792-policy sweep as built. It reproduces Keel's counts exactly (today 216 wins, 26 cheap; lab 152/18; lab-field-on 299/37; lab-snap-off 128/41).
- `sw/<preset>.1.txt` and `matrix-many1b.txt`: the same with the switch `MANY=1` (§12.2).
- `cheapest.log`, `cheapest-base.log`: the cheapest policy replayed at 0.16.0 and at 0.15.0 (`out/keel037/base`).
- The §11.3 range probe (`ScenariosS11.cs`, ported) under both.

**Kind: TODAY and LAB.**

### 12.1 The ten newly cheap `today` wins: one trick
All ten are the loud route (L3) with the same stand-off. The other flags are incidental.
- **The trick, as a player would do it:** "Put the Scout, Fixer and Tech just past the police's pistol range, so they can still hit, and take turns firing. Step back whenever an officer walks in."
- **The strip:** `pistol-eff-m` 15 and `pistol-max-m` 16 leave a 1 m strip. The cell fires from inside it at long odds. Every cheap win in the sweep has a margin of 30 cm or less (km5 to km30, tight). The default 1 m margin wins **0 of 572**.
- **Why the police lose it:** §7.10 rule 1 lets a police body answer to its max ring, but only the *last* shooter (`Gun.ShotBy` is one slot). With three guns taking turns, the class-1 target and the rule-3 cover flip every shot. In the cheapest run the two officers fire 5 shots in 26 s against the cell's 45, and they change cover 17 times.
- **The incidental flags are fair:**
  - `norivals`: leaving the rival crew alone. In the loud branch they fight only when shot. That is the spec: §07 loud has police and no allies, and the rivals' ambush belongs to the safe branch.
  - `enfgo`, `at<s>` and `carfree`: when the Enforcer walks Vale out.
- **Why the settle flipped them:** the settle re-times the turns. The same policies lost at 0.15.0. It is knife-edge both ways: 54 new wins and 362 lost.
- **Verdict:**
  - **8 of 10 are an exploit.** The margin can't be seen, and the win beats a rule, not the police.
  - **The two `tight-step60` rows are fair.** They stay at 38 % under the fix, with standing shots at 300 ‰ against two officers' 400. That is a 3-on-2 at long range, paid with T 18 and Standing +2 → −3.

### 12.2 Rule: the police answer everyone who is shooting them (no new value)
§7.10 rule 1 widens from the last shooter to **every operative who fired on the body within `line-lapse-s` (3 s)**, out to its max ring.
- For police bodies only (`PoliceBody`), class 1 in `ChooseTarget` is any such shooter. The target it already aims at stays while that shooter keeps firing, under the existing "only a better class replaces it".
- Rule 3's cover applies only when it can answer none of them.
- Rivals' targeting is unchanged.
- **State:** a last-shot tick per operative on each gun: 4 ints in op id order, hashed. It is derived from shots, so replays hold.
- **Measured** (`MANY=1`):
  - `today`: wins 216 → 165, cheap 26 → 8. The 8 left are the same 30 cm strip at 22–38 %, all heavy (T ≥ 15, Standing ≤ −2), with no common trick. I am not chasing them.
  - P-A, P-G and P-B are unchanged in every preset.
  - No adversarial row in the matrix gets cheaper. L2 goes 100 → 83 % and still fails; L4 is sealed at 22 % (was 16 %).
  - The §11.3 range table is unchanged (focus 4 v 4 is still 5.6 s, 0 down, 44 %).

### 12.3 The lab, lab-field-on and the snap-off row
- **Lab (13 newly cheap):** the same strip family. Under §12.2 it goes 18 → 13 cheap.
- **lab-snap-off, `tight-step200-focus-norivals-at*-runkite` at 22 %:** the same trick plus focus. Under §12.2 it fails at 100 % (cheap 41 → 9).
- **lab-field-on (27 newly cheap):** the strip plus the field. Under §12.2 it gets cheaper: wins 299 → 506, cheap 37 → 59.
  - The police now stand and answer instead of walking cover to cover, so they die before the seal.
  - 35 of the 59 lose in the plain lab, so the field is what carries them.
  - **Not a reason to drop §12.2.** Field-on is an experimental mode Sky hasn't chosen. If he wants it, the field's price needs its own pass (`lab-field-cost`, `lab-field-ticks`).

### 12.4 §11 (follow fire, locks, the trigger)
- **§12.2 has to land with §11, not after it.** Follow makes three or four guns on one target a single press. Under the one-slot rule, each follower's shot moves the class-1 target, so follow fire *is* the rotation trick, done by default.
- The tap buffer and the held aim (§11.2, §11.4) don't touch it.
- A lock lapses after 3 s out of line, the same window as §12.2's memory.
- No §11 value moves.

### 12.5 Handoffs
- **Keel:**
  - **Inputs:** §12.2. No new value; the rule text goes on `ai` / §7.10 rule 1.
  - **Scenarios:**
    - F12-ROT: three shooters 15.3 m from one officer, firing in turn. It keeps its target and fires on cadence.
    - F12-NONE: every shooter beyond its max ring. Rule 3's cover, as built.
  - **Done-check:**
    - today's sweep is at or below 8 cheap, with none at a margin over 30 cm;
    - P-A, P-G and P-B are unchanged;
    - no adversarial row gets cheaper;
    - F11-* hold;
    - `dotnet test` is green.
  - **Next owner:** Suture.
- **Aster, for Sky:** field-on only (§12.3).

### 12.6 Keel: built, engine 0.17.0 (2026-10-21)
**`today` and lab, shipped with §11 at Aster's direction (2026-10-21) on this ruling.** **Sky said yes (2026-10-21):** "1. Yes" to the police answering everyone who has shot at them recently, not only the last one (see "§12 decision" below). It stays as built.
- **The rule:** police (`PoliceBody`) answer every operative who fired on them within `line-lapse-s`, out to their max ring. It applies to class 1 in `ChooseTarget`, the max-ring foes and `KiteBody`'s answer; rule 3's cover only when they can answer none. Rivals are unchanged.
- **State:** `Gun.OpShot` (4 ints, op id order). It is hashed only while live and only where it says more than `ShotBy`/`ShotByTick`. Rule text is on `fight.kite` and `ai`.
- **Scenarios:**
  - F12-ROT (three shooters at 15.1–15.5 m, firing in turn): passes. The officer keeps its target, fires on cadence (15 ticks) and stays on its stand. With the one-slot rule restored in a scratch copy it fails: 7 target flips, 2 shots in 7.8 s.
  - F12-NONE passes.
  - Side finding, as built: with a lone carbine shooter at 20 m, the officer alternates cv-001 and cv-002 every 0.5 s (for Pulse).

**Drift, declared.** All of it is §12.2's.
- **Recorded runs:** 29 of 284 moved: S05, S11, N10, N11, N11b, N18, N25, N95, N113, N114, N137, N143, N164, N165, S9-AFTER and 14 route scripts. Range: 5 of 42 moved (F8-RUSH, F8-RUSH-B, F8-RUSH-LAB, F8-BREAK-LAB, R10), none in result.
- **`route-diff`:** 6 of 38 moved (`briefs/027/keel/route-diff-0.17.json`; Waymark's list `briefs/027/keel/route-moves-0.17.json`). **4 expected successes now fail `enforcer-down`: `loud`, `loud-fast`, `loud-left-downed`, `loud-left-standing`.** The officers no longer flip between the cell's shooters (first line apart at 3:48.0). `loud-fight` still wins (health lost 38 → 66 %); `careless-body-found`'s health lost goes 33 → 38 %.
- **`missions:check`** (tree engine and inputs, against a repo copy carrying the 0.16.0 `sim` and rules): MC-0 goes 37/38 → 32/38 (the five loud rows); MC-2 still fails, now because the loud route loses; MC-6 newly fails (no loud win); MC-10 newly passes. HEAD's own MC-0 failure (27/38) is other uncommitted work.

**Measured (sweep `briefs/027/keel/sweep-0.17.txt`, matrix `matrix-0.17.txt`).** The nets reproduce §12.2 exactly:

| preset | wins | cheap | newly cheap |
|---|---|---|---|
| `today` | 216 → 165 | 26 → 8 | 5 |
| lab | 152 → 125 | 18 → 13 | 13 (every one of them) |
| `lab-field-on` | 299 → 506 | 37 → 59 | 58 |
| `lab-snap-off` | 128 → 68 | 41 → 9 | 9 |

- **`today`:** all 8 cheap wins have a margin of 30 cm or less, at 22–38 % lost. 5 of them were losses at 0.16 (knife-edge, as §12.1 says).
- **Matrix:** P-A, P-G and P-B are unchanged. No `today` or lab adversarial row gets cheaper: L2 goes 100 → 83 % and still fails; L4 goes 16 → 22 %. The snap-off row now fails. `lab-field-on` km10 newly wins, at 66 %. Field-on is reported, not fixed (§12.3).


## 13. Rulings on engine 0.17.0 (2026-10-21)

**Inputs:** §11.7 readings 1, 2 and 4; §12.6; `briefs/027/keel/sweep-0.17.txt`, `matrix-0.17.txt`.
**Probes** (scratch tree `out/motive027/s13/tree/`, a copy of engine 0.17.0, inputs `bc7440bc7efc04cf`; nothing in `sim/`, `contracts/` or `unity/` edited). Switches, scratch only: `PAIR=1` (13.1), `NOHOLD=1` (13.2), preset `lab3` = the lab with `stances: three` (13.4). The base sweeps reproduce Keel's exactly (today 165/8, lab 125/13; 0 lines differ).

**Kind: TODAY and LAB.**

### 13.1 The tap-switch exploit: the count is the pair's in `today` too (no new value)
- **Rule:** `today` adopts the lab's aim memory (§6.1–6.2). The count belongs to the pair (shooter, target) for the mission, for every shooter. Taking a target resumes the count the shooter left on it; a target it has not fired at starts at `acc-start` 500. Nothing resets it.
- **"A switch always pays aim-new" does not close it.** Aim-new (1.0 s) runs inside the pistol's 1.5 s cooldown, so a switch the tick after a shot costs nothing.
- **What it buys:** the 500 head start once per person, never once per switch. Switching between two targets gains at most one hit over the mission.
- **Measured:**
  - Scout alone, unkillable, 30 s against two officers (`S13-SPAM`): hold 6 of 10, switch 6 of 6 → **6 of 10**, clear 86 → 146 ticks (the hold's).
  - `today` sweep: wins 165 → 156, cheap 8 → 4. All 4 are knife-edge and heavy (T 16–18, Standing → −2 or −3).
  - Matrix: P-A, P-G and P-B are unchanged. `P-G:reset`, the reset exploit itself, goes from success at 55 % to failure, as in the lab. No adversarial row gets cheaper.
  - Recorded runs: 0 of 283 results and 0 of 57 range results move. The checkpoint hashes move wherever a shot is fired (the memory is hashed).
- **F11-SPAM can't see it:** the Scout is down after 2 shots. Lengthen it to F11-SPAM's switch against hold over ≥ 8 shots.

### 13.2 Hold never follows (confirms Aster)
- §06 hold = "fires only when ordered". Following is derived, not ordered, so a holding agent neither follows nor is locked by a press.
- Hold is the only way to keep a drawn agent quiet beside the driven one. Without it, the player must holster to stop the agent.
- **Measured:** every `today` F11/F12 scenario passes unchanged. F11-JOIN-LAB fails, because its follower holds: that is 13.4's case.

### 13.3 The 5 newly cheap `today` wins: knife-edge and heavy, no trick
All 5 cost T 15–18 and Standing → −2 or −3. None has a cheap neighbour (one step of km, fm, step, at or one flag) except the two `tight-step100-…-carfree-kite` rows. They are one policy (`enfgo` is incidental), and step 60, 200 and 400 all lose at 66–100 %. Detail: `out/motive027/s13/neighbours-today.txt`.

### 13.4 Lab draw: yes, `return` (lab `stances: three`, an existing value)
- It matches `today` and §06, gives the lab followers with 13.2 in place, and keeps the trigger model comparable across presets.
- **Measured** (`lab3`): wins 125 → 111, cheap 13 → 12, the same strip family, all heavy. P-A and P-B are unchanged.
- **P-G goes from 33 % with 0 down to 38 % with 1 down.** The scripted player never presses V. With the draw left at will (`DRAWFREE=1`), P-G returns to exactly 33 %, 0 down. So this is the harness, not the rule: P-G's support agents set stance `free` after drawing.

### 13.5 Handoffs
- **Keel:**
  - **Inputs:** 13.1, with the rule text on `weapons` (the accumulator sentence) and `fight.lab`'s aim memory widened to both presets; 13.2 in `CanFollow` and the `trigger` text ("a holding agent is not engaged: it follows" becomes "a holding agent never follows"); 13.4, the rulebook's `fight.lab.stances` set to `three`, and the P-G harness setting `free` on its support agents.
  - **Scenarios:**
    - F11-SPAM over ≥ 8 shots: switch hits ≤ hold + 1;
    - F13-HOLD: a holding agent in the ring neither follows nor locks;
    - F11-JOIN-LAB with a `return` follower.
  - **Done-check:**
    - `today` cheap ≤ 4, each knife-edge;
    - P-A, P-G and P-B are unchanged (lab P-G back to 33 %, 0 down);
    - `P-G:reset` fails;
    - no adversarial row gets cheaper;
    - `dotnet test` is green.
  - **Next owner:** Suture.
- **Aster:** no Sky decision is needed. 13.1 and 13.4 use values that already exist, and 13.2 is §06 as written.

### 13.6 Keel: built, engine 0.18.0 (2026-10-21)
**A `today` change, recorded under §13.1, which closes an exploit in a rule Sky approved** (the §11 trigger model, "Sky on firing, 2026-10-21", brief 024): the trigger made the tap-switch reset one key. No new value. §13.2 is §06 as written; §13.4 uses an existing value. §12.2 has Sky's yes (§12.6, §12 decision).
1. **§13.1, the count is the pair's in `today`** (`AccFor`, `Shoot`): taking a target resumes the count the shooter left on it, and a target it has not fired at starts at `acc-start` 500. This holds for every shooter. `Gun.Mem` is kept in today and hashed only where the state doesn't already say it: not the target in hand at its count, and not a target shot down (a gun down stays down). The lab hashes it as before. Rule text is on `weapons` (the accumulator sentence) and `fight.lab`.
2. **§13.2, hold never follows** (`CanFollow`): a holding agent neither follows nor is locked by a press. Rule text is on `weapons.trigger`.
3. **§13.4, the lab's stances are `three`**: `fight.lab.stances` and `briefs/027/presets/lab.json`, and the range's lab preset. A lab draw sets `return`. N148 now plays stances `two` explicitly, as a lab option. P-G's support agents set `free` after drawing, in the lab only.
4. **Fix: a press in the tick a lock's target falls.** Orders land before the tick's fire, so the lock was still live at the press and the press was lost for that agent; it then stood idle with follow on. Now the press asks that agent (`Gun.LockAsk`, hashed only when set). If the lock's target falls in that tick, the agent locks the pressed target at the tick's end (`LateLocks`). This applies only while the lead still drives that target with follow on, and only if the agent then follows with reach and a line. A press on the tick *after* the fall already worked (0.17.0's `LiveLock`).

**Scenarios** (range, lane): F11-SPAM over 30 s with the Scout kept up gives hold 6/10 and switch 6/10 (was 6/6), checked as switch ≤ hold + 1 over ≥ 8 shots. F13-HOLD and F13-RELOCK pass (the same-tick case failed before the fix). F11-JOIN-LAB now runs with a `return` follower. `dotnet test` 1267/1267.

**Drift, declared.** Each rule was hashed alone, in order, with `hashes --start-text BASE` against `out/keel039/base` (built before the first edit):
- **§13.1:** 59 of 281 recorded runs moved, 0 results. 15 of them move in behaviour, and the other 44 only because the memory is now hashed. Range: 7 of 56 moved, 0 results.
- **§13.2:** 0 recorded runs moved. Range: only F11-JOIN-LAB.
- **§13.4:** 23 lab scenarios moved (N143–N165, CAR-BLAST-LAB), 0 results. Range: 20 lab rows moved.
- **The fix:** 0 moved.

The rulebook moves every inputs hash, so every recorded route re-records once against 0.18.0. DP1 was re-recorded (same orders, start and outcome). `route-diff` (frozen contracts: `briefs/027/keel/route-diff-0.18.json`, Waymark's list `route-moves-0.18.json`) shows 5 of 38 routes moved and no outcome moved:
- `route-b`, `route-b-block`, `careless-scout-idles-pickup` and `careless-reporter-not-stopped`: health lost 38 → 22 %; the last also goes from Alert 2 to 1.
- `fail-enforcer-down` ends 0.3 s sooner.

`bun run verify` passes, and `drift:auto --check` has nothing to declare.

**Measured** (`sweep-0.18.txt`, `matrix-0.18.txt`). `today` matches your PAIR=1 sweep line for line, and the lab matches lab3.

| preset | wins | cheap | newly cheap |
|---|---|---|---|
| `today` | 165 → 156 | 8 → 4 | 4 (all 4) |
| lab | 125 → 111 | 13 → 12 | 5 |
| `lab-field-on` | 506 → 494 | 59 → 62 | 19 |
| `lab-snap-off` | 68 → 65 | 9 → 12 | 5 |

- **`today`:** all 4 cheap wins are the L3 strip at margins of 10–30 cm, with T 16–18 and Standing → −2 or −3. The cheapest, `km10-fm5-step60-focus-norivals-at208-runkite`, loses only 11 %.
- **Matrix:** P-A, P-G and P-B cost the same. Lab P-A ends 0.1 s later. Lab P-G is at 33 % with 0 down. `P-G:reset` in `today` fails `rivals-reach-vale`. No row becomes a win: some failing rows lose less health, and three `lab-field-on` rows now fail `sealed` instead of `enforcer-down`.
- **Not regenerated:** `fight-lab.json` and the CP-LOUD and CP-SAFE checkpoints. A full default run reads the repo's routes, which Waymark is mid-edit on. Run one after his re-record.

**Handoffs.**
- **Latch:** in the lab, X sends `stance hold`. With stances `three`, hold no longer ends a fire order, so X should cease, as it does in today.
- **Suture:** the 4 newly cheap `today` wins.
- **Motive:** the cheapest of them, at 11 %.


### §12 decision (Sky, 2026-10-21)
- **§12.2: yes.** Sky: "1. Yes" to "should police fire back at everyone who has shot at them recently, not only the last one?" No longer awaiting Sky.
- On the taught loud plan (wait by the car, lock as the doors open): "It's a way. But not sure how they can lock targets easily. Also what about rival gang? As long as player has to be present and adaptive then how they play is fine, and they're not just doing only a few actions."


## 14. Syndicate fights: weapons free (Sky, brief 041, 2026-10-21)

**Inputs:** brief 041, "Syndicate fights and squad-leave adopted" (Sky: "that feels syndicate like and more engaging"); `testbed/controls/fire.js` (`autoEngage`, `focusFire`, `FIGHT_S` 10); Sky via Aster: "definitely need officers / bots etc firing at you if you're on high alert, do something wrong etc - rivals especially"; Pulse's `briefs/027/pulse/hostile-initiative.md`.
**Probes** (scratch tree `out/motive027/s14/tree/`, a copy of engine 0.18.0; `treewf/` is the same with the switch on by default, for `route-diff`; nothing in `sim/`, `contracts/` or `unity/` edited). Switch `WF=1`: attacker first, then nearest, and the target is kept. `WF=2`: nearest only. `WFAIM=1`: a hostile that is only aiming also counts as a fight. With `WF=0` the matrix and the sweep match `matrix-0.18.txt` and `sweep-0.18.txt` line for line.
- `matrix-wf*.txt`, `sw/<preset>.<WF>.txt` (the 3,792 policies), `route-diff.txt`, `s11-wf*.txt` (the §11.3 probe plus a `wfree` policy: one opening shot, then nothing), `range0/`, `range1/` (all 67 range scenarios).

**Kind: TODAY and LAB.** No new value.

### 14.1 Weapons free is in, in `today` and the lab
- **The rule:** while weapons free is on and a fight is on (14.2), a drawn cell agent picks its own target if its stance is `return` or `free` and it has no fire order, lock, trigger or follow.
  - It takes whoever is attacking it first: a combatant whose gun aims at it, or its last shooter within `line-lapse-s` 3 s.
  - If nobody is attacking it, it takes the nearest combatant (integer `D2`, ties in `CombatantIds` order), within its max ring and with a line.
  - It keeps the target in hand while that target is still a combatant in reach and line. It changes only when an attacker appears and the current target is not one.
- **What it never does:** fire on a non-combatant (a civilian, a calm guard, a car, a camera), fire while holding, draw a holstered gun, or walk.
- **Precedence**, as the playground has it: a lock or fire order (portrait drag, focus click) comes first, then the trigger, then follow, then weapons free.
- **X** (cease) stands the selected agents down from weapons free. They pick again only when they are shot at, when they get a new fire order or trigger, or when the fight ends.
- **G** turns it on and off for the cell. Off, a draw is `return` again (0.18.0 exactly). It starts on, as in the playground.

**Measured (`today`, WF=1 against 0.18.0):**

| check | 0.18.0 | weapons free |
|---|---|---|
| sweep wins | 156 | 182 (89 new, 63 lost) |
| sweep cheap (< 40 % lost) | 4 | 6 (5 newly cheap) |
| cheapest | 11 % | 11 % |
| matrix rows that change result | | 0 of 42 |
| L1–L4 | all fail | all fail, the same codes (L3 T 28 → 30) |
| P-A | 0 % | 0 % |
| P-G | fails, 55 % | fails, 72 % |
| P-B | 44 %, T 3 | 55 %, T 7, 20 s sooner |
| recorded routes | | 0 of 38 outcomes move; 16 move health lost |
| range 4 v 4 at 14 m | focus 5.6 s, 0 down, 44 % | wfree 5.6 s, 0 down, 44 % (10 m: 5.8 s, 55 %) |
| range 4 v 6 | loses | loses (4 down, against focus's 2) |
| the rush, 4 on 1 | 1.1 s, 1 hit | 1.1 s, 1 hit |

- **It does not break the table.** What gets cheaper is *clicks*, not fights. One opening shot now buys what one focus order per target bought (4 v 4 is the same 5.6 s), and it never beats focus.
- **The loud route:** no loud row and no loud recorded route changes its outcome. Weapons free alone doesn't make loud a win. Pulse's initiative (14.6) is what will move it.
- **The rush** is unchanged. It was already four guns on one.
- **The cheap-win families:**
  - All 6 cheap `today` wins are the §12.1 strip family (L3 stand-off, margin ≤ 30 cm): T 15–20, Standing → −2 or −3. Three of the old four lose their cheapness: weapons free fires past the plan, so T goes 18 → 28 and agents go down.
  - Lab: wins 111 → 114, cheap 12 → 13, the same family. Field-on and snap-off have no new cheap win.
  - This is a modest edge, earned by drawing and standing in the fight. Reported, not tuned.
- **P-B and P-G cost more, and it's a knife-edge, not a trend.** In P-B the Fixer aims a tick early, rival-3 switches onto him, and he loses the duel at 281 ‰. The downed Tech is captured (+4 T). A plan that needs an agent quiet gives it `hold`.
- **`WF=2`** (nearest only) gives the same wins and cheap counts. Attacker first stays because it reads: whoever shoots you is shot back.

### 14.2 When a fight is on: the existing fight (fight.public), not 10 s
- **It starts** at the first shot by or at the cell (`OpenFight`), or at the final fight. The range is always on.
- **It ends** `crowd-quiet-s` 30 s after the last such shot, and the final fight ends at fight.over. Then `auto-holster-s` 4 s runs as built.
- **That makes one meaning of "a fight"** for Standing, auto-holster and weapons free. The playground's `FIGHT_S` 10 should be 30.
- **An aiming hostile doesn't start it** (`WFAIM=1`: wins 181, cheap 6, the same). If it did, a guard who engages a seen draw would be shot first by a stance. That breaks "a stance can never start a loud run by itself" and Pulse's "drop it" beat.
- **Hostile initiative still opens fights.** The hostile's first shot opens the fight, and the drawn cell answers after `aim-new` 1.0 s.

### 14.3 Follow fire (§11): fold it in
- **In a fight, follow does nothing.** A weapons-free agent with a combatant in line is engaged (`CanFollow` reads `StanceTarget`), so it never follows. Under WF=1, F11-NEAR, F11-LOCK, F11-RUSH, F11-REPLAY, F13-HOLD, F13-RELOCK and RK-LINES fail for exactly that reason.
- **Keep follow (`follow-m` 8) for what weapons free can't do:** the opening shot from the driven agent (third person) before a fight is on, targets that are not combatants (a car, a camera, a calm guard), and weapons free off.
- **The follow-on lock** (`Start.followLock`, the `follow` order) is the focus click by another key. Recommend it goes when the playground set is ported. Keel can keep it as a test setting until then.

### 14.4 Determinism: confirmed
- Weapons free is an input: `Start.weaponsFree`, and a logged `{"do":"free","op":"cell","on"}` on its tick.
- The pick reads only hashed state: positions, lines, each hostile's `Target`, `ShotBy` and `ShotByTick`, the agent's own `Target`, and `OpenFight`. The ties go in a fixed id order.
- The stand-down is one tick per agent (`StandDown`), hashed when set.
- The engine derives every pick and logs it (`fire.free`). The host never issues them.
- Every chunk of every sweep variant reported "every run replays identically from its order log".

### 14.5 Handoffs
- **Keel:**
  - **Inputs:** 14.1 to 14.4. The rule text goes on `weapons` (the stance sentence) and `weapons.trigger` (follow is inert in a fight under weapons free). `Start.weaponsFree`, the `free` order, `fire.free` and `StandDown`.
  - **Scenarios:**
    - F14-OPEN: a drawn agent with a guard aiming and no shot fired doesn't fire;
    - F14-ATTACKER: the attacker is taken before a nearer foe;
    - F14-STICK: no change of target while the target is valid;
    - F14-X: X holds the agent until it is shot at;
    - F14-OFF: matches 0.18.0's hashes;
    - F14-QUIET: weapons free ends 30 s after the last shot;
    - F14-4V4: 5.6 s, 0 down, 44 %;
    - F14-REPLAY: toggled mid-fight, hash-identical.
    - Run F11-*, F13-* and RK-LINES with weapons free off. F12-ROT under weapons free fails only its time check (three guns at once drop the officer in 3.3 s); the officer still keeps one target, so §12.2 holds. Keep it off, and add F12-ROT-WF checking the target only.
  - **Done-check:** `today` cheap ≤ 6, all of the strip family; no loud row and no recorded route outcome moves; `dotnet test` is green.
  - **Next owner:** Suture.
- **Waymark:** no route changes outcome. 16 of 38 move health lost: up 5–11 % on most of them (safe-branch ambushes and boards), down 6 % on four `-board` rows, and `careless-reporter-not-stopped` goes from Alert 1 to 2. Give `hold` to any agent the plan needs quiet. Re-record once, after Keel builds this *and* Pulse's initiative, which moves the loud and careless rows again.
- **Latch, Glyph:** G and its icon, X as stand-down, the `fire.free` pick on the agent's line.

### 14.6 With hostile initiative (Pulse)
- **Weapons free is balanced against hostiles that start fights, not passive ones.** Their first shot is the cell's cue, and the 2 s react plus `aim-new` 1.0 s is the window to holster, break the line or draw.
- **Weapons free spends Trail without a click** (the first shot at each target, each hit, each down, outside the final fight). That is the price of G, and it is the player's to switch off.
- **The numbers Pulse's proposal needs from me, for its own ruling:**
  - the `ai.intent` ladder per S level;
  - the "face known" and Heat rungs (wanted 6, infamous 10, existing);
  - the "drop it" warning in `today` (`lab-warn-ticks` 2 s, +1 T when heeded);
  - when the rivals' cone comes on;
  - Standing for a fight the hostiles start;
  - whether an agent a hostile aims at draws by itself. **My view: no.** The draw is the player's commitment, and a seen draw raises Alert.

### §14 note (Sky, 2026-10-21)
On "is 30 s the right fight length?": "I mean fight lasts as long as energy / health is there, or player moves away - just timing is about weapon damage / player / enemy health balance really, not actual timed shooting (though time between shots is the baseline ofc)." So "fight on" ends on contact, not a clock: when no hostile is engaging the cell (all down, or contact broken by moving out of sight or range), with at most a short settle after the last shot. Fight length comes from damage, health and cadence. Motive to restate §14.2 this way.

### 14.7 Keel: built, engine 0.19.0 (2026-10-21)
**Kind: TODAY and LAB**, as Sky recorded: brief 041 "Syndicate fights adopted", the §14 note (a fight ends on contact, not a clock) and Sky's answers in `briefs/027/pulse/hostile-initiative.md` (per agent; no auto-draw; rivals see the street from 3:10; robots fire on known faces). Rulebook `weapons.free`, `ai.intent` (schema optional, engine required); host fields in `briefs/027/keel/host-interface.md` (0.19.0).
- **Weapons free (§14.1–14.4)** as specified. Fight on/off per Sky's note: on at a shot by or at the cell, kept while a combatant aims at an operative inside its max ring on a line, off `weapons.free.settle` = `line-lapse-s` 3 s (an existing value) after the last contact or shot. fight.public's 30 s judging and auto-holster are unchanged. Focus = `fire` with `focus: true`, a lock (never walks in). X sets `Gun.StandDown`. Follow folded in (§14.3).
- **Intent, per agent:** police and robots foe on drawn, fired, Vale, violence seen; a known face from Alert 2 (robots at any Alert at wanted/infamous Heat). Rivals: calm drawn or Vale; alerted (drawn, or Alert 2) also what they saw that agent do; the drawn crew presses for the Vale holder. Final fight and range: every operative, as before. A face is known from a seen draw, seen violence, a warning, or a landed witness report of violence or a draw (not loitering: with it, one loiter report got the Enforcer shot at 1:40 in S14). Police warn a drawn weapon at Alert 0–1 (the lab's numbers), not an agent already given a target. From Alert 3 at security level 1 and up, police close in on a sighting (fight.kite's close-in).
- **New [S] values (Motive's to rule):** intent-face-alert 2, intent-warn-max-alert 1, intent-close-alert 3, intent-close-level 1, intent-rivals-alert 2. Not built: Pulse's full per-level dial, the Heat "rival hit", rival ambush on sight.
- **Proof** (baseline `out/keel041/base`, built before the first edit): both rules off by temporary switches, 287/287 hashes and 67/67 range identical; switches removed, 294/294 + 77/77 identical to before removal. `dotnet test` 1365 pass. Matrix (`briefs/027/keel/matrix-0.19.txt`): no `today` row changes result; P-A 0 %, P-B success 44 → 55 %, P-G failure 55 → 72 % (weapons free, your numbers; intent moves none of them).
- **Routes** (`briefs/027/keel/route-moves-0.19.json`): weapons free moves 16 routes' health and careless-reporter's Alert 1 → 2, no outcome (your §14.5). Intent: the 5 loud wins now fail enforcer-down (the press: the crew walks up on the Enforcer, who can't fire walking Vale; press off, they win as before). S11 wins once the three cover the walk.
- **Sweep** (`briefs/027/keel/sweep-0.19.txt`): built, `today` wins 156 → 28, cheap 4 → 0, newly cheap 0; every lab preset cheap → 0. The press alone causes the drop. Hidden under it: weapons free alone gives `today` 163 wins, cheap 9, **7 newly cheap (cheapest 5 %)**, the norivals runkite family. If the press is softened, they come back.
- F14-4V4 line missed: 6.7 s, 0 down, 44 % (yours 5.6 s), because the fight now starts at the first shot rather than at the spawn.
- **For Motive:** the press strength (loud wins 156 → 28); the 5 new values; the final fight keeping "every operative" (Sky's per-agent answer may reach it); the 7 hidden cheap wins. **Waymark:** re-record the loud rows with cover for the walk. **Fable:** the placeholder lines for `free` and `free-not-cell` in text.json. **Latch, Glyph:** G, X, focus, the `UnderFireOf` under-fire cue, the crew's cone from 3:10.

## 15. Rulings on engine 0.19.0 (2026-10-22)

**Inputs:** §14.7 (Keel, engine 0.19.0); Pulse's `briefs/027/pulse/hostile-initiative.md` with Sky's answers; Sky on the press: "a clever player would protect their enforcer, by positioning his squad around him - or in cover"; Waymark's protective loud plan (`briefs/027/waymark/loud-robust-0.19.txt`: 5:09.6, 14 T, Alert 2, 11 %, 36 of 41 standable moves clean); Keel's `sweep-0.19.txt`, `matrix-0.19.txt`, `route-moves-0.19.json`.
**Probes** (`out/motive027/s15/`; nothing in `sim/`, `contracts/` or `unity/` edited). `tree/` is a copy of the working tree at 0.19.0 with two env switches: `S15FINAL=agent` (the final fight per agent, 15.2) and `S15COVER=1` (a rejected candidate, 15.3). `rb/*.json` are the rulebook variants. Unswitched, the tree reproduces `matrix-0.19.txt` line for line, Keel's sweep B (`today` 28 wins, 0 cheap) and Waymark's mission check (37/38, the same MC-0 line). Results: `m/matrix-*.txt`, `m/sw/<preset>.<tag>.txt`, `m/routes-<tag>.txt` (all 38 route scripts), `m/mc-*.txt`, `m/robplan-*.txt`.

**Kind: TODAY and LAB.** No new value.

### 15.1 The five values and the press: confirmed as built
Each value moved one step either way: the `today` sweep (3,792 policies) and all 38 route scripts.

| value | built | one step down | one step up | ruling |
|---|---|---|---|---|
| `intent-face-alert` | 2 | 1: wins 28 → 56, none cheap (police engage a known face at Alert 1, so fights start earlier and split) | 3: wins 24 | **2.** A known face is a target from search, as Pulse's S1/S2 dial has it. |
| `intent-warn-max-alert` | 1 | 0: wins 26 | 2: identical | **1.** A heeded warning is itself Alert 1, so a warning only makes sense while it can still keep things at 1. |
| `intent-close-alert` | 3 | 2: wins 0, and `fail-district-seals` becomes `enforcer-down` at 3:10 (T 19): search turns into a hunt | 4: the rulebook rejects it (0..3) | **3.** Close-in is the lockdown's beat. |
| `intent-close-level` | 1 | 0: identical at the Handover (S1) | 2: identical (close-in never moves a sweep row) | **1.** S0 (lax) never closes in; routine does. |
| `intent-rivals-alert` | 2 | 1: identical | 3: identical | **2.** In every played run, gunfire draws the crew before the Alert does. |

- No step of any value changes a route outcome, and no step makes a cheap win (< 40 % lost).
- **The press: on, as built.** Off: `today` wins 28 → 159, cheap 0 → 9 (7 newly cheap against 0.18.0, cheapest 5 %), the loud rows 14 → 11 T. The press is what makes the loud route need the protective plan Sky described, and Waymark's plan shows it can be won (11 %). That is the intended result. The press is not a tuning knob: there is no softer setting. Any later change to it re-runs this sweep (15.3).

### 15.2 The final fight follows per-agent intent (rule; Sky to confirm)
- **The rule:** `ai.intent.final` takes `agent`. In the final fight a hostile fires on an operative only for that operative's own causes, the same list as before the fight:
  - police and robots: drawn, fired, Vale, violence, or its own known face (the final fight is at Alert 2 or more);
  - rivals: drawn, Vale, or seen.
- **The range keeps `every`.** It has no history to read.
- **No new value:** an existing key gets its second setting.
- **Why:** Sky's rule ("each character has their own history… the Scout shouldn't be punished if the Enforcer goes in wild") has no exception for the final fight. "Every operative" was Pulse's §2.1 carry-over, a crew-made rule.

**Measured (`S15FINAL=agent` against the built engine):**

| check | result |
|---|---|
| matrix, 42 rows | identical, hashes too (L3 `f103ee2e4a95` both ways) |
| sweep, 3,792 × `today`, `lab`, `lab-field-on`, `lab-snap-off` | every row identical |
| quiet routes (all A, B, G variants, withdraw) | outcome, T, Alert, health and hash identical: 30 of 38 scripts hash-identical |
| loud routes (`loud`, `-fast`, `-fight`, `-left-downed`, `-left-standing`) | outcome, T, Alert and hits identical; hashes move |
| three failure rows | the same failure, but the agents who did nothing are no longer shot: `careless-takedown-seen-crowd` 3 down → 0 (18 → 6 hits); `fight-loud-enforcer-down` 3 down → 0 (T 9 → 8); `fight-safe-rivals-reach-vale` Tech down → none (10 → 6 hits) |
| Waymark's protective plan, its 80 robustness variants (`m/robplan-loud-base.txt`; the 68 composed scripts in `m/robscripts/`, replayed both ways in `m/robreplay.txt`) | the base plan is identical (5:09.6, 14 T, 11 %). Flank waits stay 32/32. Car waits drop from 36 to 33 clean of 48: S 0,+1 m, S +1,+1 m and T +1,0 fail. Re-composed with the rule on, those three still don't board. |
| mission check | identical (37/38, the same MC-0 line, 15.4) |

- **It makes the loud route slightly harder, not easier.**
  - In the three lost car waits, the response unit has joined the final fight and walks past the car at 2–3 m.
  - It now shoots only the agent who drew first (the Fixer, drawn at 3:45.5, down at 3:49.6) instead of spreading its shots over the holstered agents.
  - The counter is the player's: draw together.
- **It opens no exploit.**
  - A clean agent can't join the fight without becoming a foe: drawn in sight, fired, or its face known.
  - Only the Enforcer takes people down, and in the final fight he holds Vale.
  - What the player gains is what Sky asked for: an agent kept out of it stays out of it, at the cost of one gun fewer.
- **Sky to confirm,** because it changes GDD §06's target priority. "… → nearest armed op → nearest op" becomes "… → nearest op it has cause to fire on". It also decides how T6 ("final fight reads as consequence") reads: per agent.

### 15.3 The 7 hidden cheap wins: none survives the press
- **With the press on** (and either final-fight rule), all 7 fail with `enforcer-down`, 38–66 % lost. Press off is `rb/nopress.json` with intent on, the same 7 as Keel's A:

| policy (L3) | press off | press on |
|---|---|---|
| km10-fm5-step60-focus-norivals-enfgo-carfree-runkite | success 4:54.6, 11 % | enforcer-down 3:50.2, 38 % |
| km20-fm20-step300-norivals-at220-runkite | success 5:01.0, 38 % | enforcer-down 3:55.2, 66 % |
| tight-step200-focus-norivals-at212-runkite | success 4:55.0, 38 % | enforcer-down 3:57.0, 50 % |
| tight-step300-focus-norivals-at224-runkite | success 4:44.8, 33 % | enforcer-down 3:59.2, 61 % |
| tight-step300-focus-norivals-at228-runkite | success 4:44.7, 27 % | enforcer-down 4:03.2, 55 % |
| tight-step300-focus-norivals-at232-runkite | success 4:48.3, 22 % | enforcer-down 4:07.2, 55 % |
| tight-step300-focus-norivals-at236-runkite | success 4:52.3, 22 % | enforcer-down 4:11.2, 55 % |

- **The mechanism** (`at228`, both logs): the policy never fires on the crew and fights the two officers at the edge of their ring.
  - Press off, the drawn crew stands at the Office front and never acts, so the stand-off is free.
  - Press on, the crew walks onto the Enforcer, who is walking Vale, and drops him from 9 m down to 3 m by 4:03.2.
  - The press is the fix. No rule is needed.
- **One candidate was tried and rejected:** a police body in cover re-plans only once each `kite.coverHold`, the officers' cover churn in that log (30 cover changes, 2 shots). It makes press-off cheaper (cheap 9 → 22), so the churn is not what makes them cheap.

### 15.4 `careless-reporter-not-stopped`: Alert 2 is the expected cost; Waymark updates the expectation
- **Under 0.19.0 the ambush fight lasts longer:** 7:28.0 to 7:42.6, against 7:36.9 on 0.18.0. Weapons free spreads the fire (the Tech takes rival-2 at 15 m, 150 ‰).
- **The door guard finds the body:** he runs in against the rivals and passes it at 7:38.2. He radios it in, and the report lands at 7:43.2: Alert 2, before Go at 7:53.9. On 0.18.0 nobody passed the body before Go.
- **This is what the twin exists to show:** a body left where it fell is found and reported. The route's own note already predicted Alert 2 (guard 64, landing at 7:40.2).
- MC-7 still counts it worse than route B. The final-fight rule does not move it.

### 15.5 Handoffs
- **Keel (15.2, once Sky confirms):**
  - **Inputs:** `ai.intent.final` takes `agent` (Rules: `OneOf` every, agent). With `agent`, `FoesInReach` reads `Provoked` in the final fight, and the range keeps every. Rule text on `ai.intent`.
  - **Scenarios:**
    - F15-CLEAN: a holstered Scout who was never seen, inside an officer's ring in the loud final fight, draws no fire;
    - F15-DRAWN: the same Scout drawn in sight does;
    - F15-FACE: a Scout whose face is known (a seen draw earlier) does;
    - F15-RANGE: the 77 range hashes are unchanged;
    - F15-SWEEP: matrix and sweep rows identical.
  - **Done-check:** the 8 moved hashes above and no other; `dotnet test` green.
  - **Next owner:** Suture.
- **Keel (15.1):** drop `proposed` on the five values and the press; no number changes.
- **Waymark:**
  - `careless-reporter-not-stopped` expects Alert 2 (15.4).
  - After 15.2 is built, re-record the hashes of the 5 loud rows and the 3 failure rows. Their expectations don't change.
  - Check whether drawing the car waits on one tick wins back the three lost car waits (33 → 36 of 41 standable).
- **Suture:** retest 15.2's no-exploit claim with a policy that keeps one agent holstered through the final fight.
- **Aster → Sky:** confirm 15.2 (GDD §06 priority line).

**Sky on 15.2, 2026-10-22:** "I am not sure...I think so - sounds right." Built as provisional: Keel builds `ai.intent.final = agent`; Sky confirms after playing the build. The GDD §06 priority line changes only when Sky confirms (folded into the GDD v0.25 review).

### 15.6 Keel: built, engine 0.20.0 (2026-10-22), PROVISIONAL until Sky plays it
**Kind: TODAY and LAB.** 15.2 as specified, as a rulebook value, not a switch: `ai.intent.final` is `"agent"` (schema and engine `OneOf` every, agent; `IntentRules.FinalEvery`). With `agent`, `FoesInReach` reads `Provoked` in the final fight; the range keeps `every`; the guard ally is unchanged. The probe's line, exactly: `(anyOp && (R.Intent.FinalEvery || Range)) || Provoked(id, op)`. The rule text on `ai.intent` and its `reason` say PROVISIONAL (Sky 2026-10-22, to confirm after play); GDD §06's priority line waits for Sky.
- **15.1:** `proposed` dropped on the five intent values (src now ends "confirmed as built, Motive tuning.md §15.1"); no number changed. The press lives inside the `ai.intent` block, whose `proposed` stays because of `final: agent`; its reason says the press is confirmed.
- **Scenarios** (`sim/Suborned.Sim/ScenariosFinalIntent.cs`): F15-CLEAN (the holstered Scout 6 m from police-1's stand, in an officer's effective ring on a line 30 of 30 s, draws no fire; its control with `every`, same orders, is shot 0.3 s after exposure), F15-DRAWN (weapons free off; he draws there and officer-1 and officer-2 fire on him first), F15-FACE (the guard's drop-it warning heeded at 0:49; in the final fight at Alert 2 security-1 fires on him holstered). F15-RANGE, F15-SWEEP and the route moves are both-ways tests (`sim/Suborned.Sim.Tests/FinalIntent.cs`, `Scenarios.WithFinal`: the same inputs read as `every`): all 77 range runs, the 8 matrix runs under today and the lab, and all 38 route scripts.
- **Proof** (baseline `out/keel-s15/base`, built before the first edit; `hashes --start-text BASE`, `briefs/027/keel/hash-moves-0.20.json`): range 77/77 identical. Scenarios and routes: 285 of 294 identical, **9 move**: your 8 (5 loud rows, outcome, T, Alert and hits unchanged; `careless-takedown-seen-crowd` 3 down → 0, hits on the cell 18 → 6; `fight-loud-enforcer-down` 3 down → 0, T 9 → 8; `fight-safe-rivals-reach-vale` Tech down → none, 10 → 6) **and N36**, a scenario outside your measured set: in its lost safe fight rival-2 no longer shoots the Fixer (Pressure, loitering, never drawn); same failure, its checks pass. Plus 3 new (the F15 scenarios).
- **Matrix** (`briefs/027/keel/matrix-0.20.txt`): 315 rows identical to 0.19.0 line for line; only the header (engine, inputs) differs. **Sweep** (`briefs/027/keel/sweep-0.20.txt`): 3,792 × today, lab, lab-field-on, lab-snap-off, every row identical (wins 28, 1, 29, 0); every chunk replays identically.
- **Tests:** `dotnet test sim/Suborned.Sim.Tests` 1509/1509 (1m48). `bun test contracts/rules` 61/61; `bun run typecheck` clean. Full `bun test` 1467 pass, 2 fail, neither from this change: the mission check's MC-0 on the tree engine (`careless-reporter-not-stopped` expects Alert 1, gets 2: 15.4, Waymark's) and drift-auto's end-to-end (skyline r011, from `tools/missions/check/engine.ts`, not mine). `bun run verify`: r008 ACCEPTED, REPLAY OK. `bun run drift:auto`: people and dialogue declared (dialogue: `contracts/rules/rulebook.schema.ts`, `contracts/missions/handover/text.schema.ts`; people: `tools/missions/check/engine.ts`), skyline refused (r011, the same engine.ts; its owner declares it).
- **Also:** `Fixtures/drive-session.script.json` re-recorded (orders identical; engine and inputs moved), DP1 passes. Host note in `briefs/027/keel/host-interface.md` (0.20.0): no new field; the host must not assume everyone is a target in the final fight.
- **Run times:** the machine ran at load 100–190 from parallel batch plays (`out/missions/plays/bench-*`), so: full `bun test` 29 min (over 10: the mission-check and drift-auto suites replay every route and run on a scratch copy; CPU 41 min), the sweep 9 min (1m41 unloaded at baseline), the matrix 4m21, `verify` 4m26.
- **Next:** Waymark (careless-reporter-not-stopped → Alert 2; re-record the 8 route hashes; N36 is a scenario, nothing to re-record), Suture (one agent holstered through the final fight), Latch (port the dll with the rulebook in one batch: a 0.19.0 dll rejects `final: agent`).

## 16. Rulings on engine 0.21.0: the lasting fire order on the move (2026-10-22)
**Input:** Sky's rule (brief 041, 2026-10-22: "Keep them firing until out of range I would say, same for running"), as Keel built it (`fight-lab.md` §16, `sweep-0.21.txt`, `matrix-0.21.txt`). **No value moves. The rule stands as Sky gave it.**

**How I checked:** I re-ran the 48 sweep rows whose win moved, on both engines (`fight-lab --runs … --scripts`, about 3 s per preset), and played each script with its log (96 plays in 3.6 s per engine). Scratch is in `out/motive-s16/`.

### 16.1 The lost wins: coin flips on the ring's edge, not a line the player loses
- **One cause, the same in every row.** All 48 rows (today 12 lost and 3 new, lab 1 lost, lab-field-on 21 lost and 11 new) match 0.20.0 line for line until their first `fire.out-of-range`. Every one of those ends is a target 1 m outside the sidearm's 16 m max ring (effective ring 15 m).
- **After that tick, the result is chaos.** Each preset gains wins as well as losing them, and every win on either side is heavy. The lost wins had lost 50–83 % health, the new ones 55–88 %. The cheapest win in the sweep is still 55 % (it was 55 % today and 50 % in lab-field-on). Cheap wins stay at 0. For example, `lab-field-on L1c:tight-step100-blk-aware-at208-runkite` won at 4:59.3 on 0.20.0 and seals at 5:00.4 on 0.21.0. That is 1.1 s inside a 5:00 clock.
- **Only the kite scripts are hit.** They hold a stand-off band: they step away inside a foe's 15 m ring and fire from within 16 m. That keeps the shooter on the exact edge where the rule applies. The 0.20.0 chase walked them back in for free, so the engine was playing for them. Losing that is the rule doing its job.
- **No human line loses its target.**
  - The plain matrix player (L1–L4, P-A, P-B, P-G) is identical in every preset.
  - The one exception is lab-cover L3, Trail 30 → 32. At 4:11.9 the patrol steps out to 16 m and the Tech's order on it ends. Weapons free takes police-1, so one more person is hit, at Trail per person. It is the same failure.
  - No route script moves by behaviour (Keel, 38/38).
- **Nobody is shot by a target they dropped.** Across 613 ends in the 48 runs, the dropped target never fired on that agent from beyond 16 m within 3 s. Both sides carry sidearms with the same ring.
- **Re-engaging is already cheap.**
  - In a fight, weapons free picks up 341 of the 613 ends (56 %) with no input: 188 on the same target once it is back in reach, 153 on another foe in reach.
  - The agent's next shot comes a median 1.7 s after the end (p90 5.6 s).
  - Otherwise, re-engaging takes one right hold. That is fine: the player stays present (Sky's "present and adaptive"), and the rule says exactly when to re-aim.
- **The one need is to see why the agent stopped.** Keel's host note already sends `fire.out-of-range` to the pop `less.pop.fire-out-of-range` (placeholder "Too far", for Fable to word). Latch ports it. This is not a rule change.

### 16.2 `P-G:reset` winning at 55 % in lab-field-on: acceptable, no fix
- **What moved:**
  - At 3:40.4, rival-2 steps out to 16 m and the Fixer's order ends.
  - Weapons free picks rival-1, who is shooting the Enforcer from 9 m.
  - The hit on the Enforcer at 3:41.3 no longer lands (on 0.20.0 it left him at 10 health), and the run wins at 4:34.5.
- **The new rule wins it, not the reset.** It drops the far target and takes the near threat. At lab `acc-start` 0, the reset (cease and fire again after each shot) only slows the shooter.
- **It still costs more than playing straight.** Plain P-G in the same preset wins at 33 %, inside its line. The reset row wins at 55 %, with 0 ops down, and stays FAIL on P-G's line (55 > 33). The §6.6.1 bar holds: no cheap loud win, and no exploit beats the honest line.

### 16.3 Values: none
- Sky's rule is the direction, and nothing measured asks for a number. The sidearm's 1 m band (effective 15 m, max 16 m, tuning.md §7.9) is where every end happens. Widening it would bring the free chase back by the side door, so it stays.

### 16.4 Handoffs
- **Fable:** word `less.pop.fire-out-of-range` (now the placeholder "Too far"; `text.json` has the refusal "Too far · {m} m"), following the plain-copy rules.
- **Latch:** in the 0.21.0 port, show the pop on the agent's plate or pill when the order ends (`host-interface.md` 0.21.0, step 3).
- **Keel, Waymark, Suture:** nothing new from this section.

**Sky on running, 2026-10-22:** "I think running only in front of camera, patrol bots and guards get you noticed, anyone else fine, need to loosen that up." (Context: moving about on foot feels slow, which lowers exploration.) Ruling for Keel: only security notices a runner (cameras, robots, guards; police officers count as security, Sky confirmed "yep"); ordinary people no longer remember a run. The walk pace and Sky's three-speed idea (walk, jog drawn, sprint holstered) are not decided.
