# Brief 027 · Lab fight preset, independent balance check (Suture)

**Date:** 2026-10-20.
**Asked by:** Aster, after the crashed session. A search found no Suture output in `briefs/027/`. A probe from the crashed session survives in `out/suture027/` (git-ignored); its tables are copied to `earlier-session/` and **not re-verified** (§6).
**Engine:** `suborned.sim/0.10.0`, headless only. Unity was not touched, and no file under `sim/`, `contracts/` or `unity/` was edited.
**Question (Sky, brief 027):** "ensure if realtime the balance of play makes it fair so it's not super easy to shoot your way through it."

## 0. Short answer

**It is still easy to shoot your way through.** Use two tricks the matrix player never uses:
1. re-aim after every shot (cease, then fire again);
2. stand 15–18 m off the police.

With them, **L3 wins at 4:43 with 0 hits taken**, against the matrix's 33 % and one op down. Motive's heavy-debrief verdict for L3 then fails, and triggers 6.4.1 and 6.4.6 fire.

The prepared runs hold. P-G's 27 % is a real floor: none of nine scripted tactics went below it.

There is also a **new regression**: the B5 cover snap (it landed in the tree during this check) makes lab **P-B fail**, on rivals-reach-vale.

| # | finding | severity | owner |
|---|---|---|---|
| F1 | **Accumulator reset:** cease, hold or switch target after a shot, and the next shot starts at acc.start 500. Two- and three-pip shots then always hit; one pip goes from 1 in 4 to 1 in 2. | **high** | Motive (rule) → Keel |
| F2 | **Stand-off band 15–18 m:** police, officers and the response unit never fire beyond their 15 m ring, and never close in. One agent kills them all with no return fire. | **critical** (with F1) | Pulse (AI) + Motive |
| F3 | **First shot always hits** at 2+ pips, for both sides: 500 + 500 ≥ 1000. This sets P-G's floor and the cell's 0.5–0.8 s rival kill. §6.4.7 measures only one direction. | medium | Motive |
| F4 | **Panic spam:** 30 draw/holster cycles beside the guard open 60 warnings and still give only Alert 1. The one cost is the guard's memory. | low–medium | Motive |
| F5 | **Target-mode charge never binds for sidearms:** regen 10/s matches spend 10/s. Volleys are free, and with F1 every volley(1) hits. | medium (target mode) | Motive → Keel |
| F6 | **Walk-in forecast:** no exploit. The walk always stops inside the target's own 15 m ring. | none | — |
| F7 | **Freeze queue, and the matrix as an upper bound:** "the script bounds all three modes from above" (playground §0) is false. The matrix player uses neither F1 nor F2. | high (process) | Keel |
| F8 | **Cover snap breaks lab P-B:** at 0:00 the Scout's move to `d:Meet driver` snaps to cv-008, so Watch is refused (6 m). The driver chain fails, the Take is refused at 6:10, and the run fails rivals-reach-vale at 8:30. | **high** | Keel |

## 1. Method and how to repeat

**Snapshots.** I copied the sim to a scratch tree and built it there, so the shared `sim/` build and Latch's editor were never touched.
- `out/suture027/src` is the tree at 15:09. Rulebook sha256 `aa194e60…deb0`, inputs `d001d608…`. Its matrix is identical to `fight-lab.json`, row for row.
- `out/suture027/src2` is the tree at 15:20, after Keel's B5 cover snap. Rulebook sha256 `674b0714…db93`, inputs `ec12cac1…`.

**The probe** is `probe/SutureProbe.cs` (a `Scenarios` partial added to the scratch copy only) and `probe/Program.cs`. It replays each matrix run's own route and timing (`Replay`, `Support`, `ToAmbush`, `EnterAndOut`, `FightToCar`). Only the fight loop changes. It is `SEngage`, `Run.Engage` with the matrix logic plus these policies:

| policy | what it does |
|---|---|
| `matrix` | Keel's `PlayLabRun`, unchanged (the control) |
| `reset` | after each shot, if the accumulator is under acc.start: `cease`, then `fire` on the same target, on the same tick. aimNew (5) is under the cadence (10), so it never costs a shot |
| `cycle` | as `reset`, but switches to another target in reach |
| `focus` | every agent on one common target: the weakest that any agent reaches |
| `kite` | never stand inside a hostile's effective ring + 1 m: step 4 m away. Fire only from within max − 0.5 m |
| `enfkite` | the Enforcer walking Vale steps out of reach instead of standing still |
| `clear` | L3 only: the whole cell, carbine included, clears the response unit before the Enter |

Every probe run replays identically from its order log (`Engine.Replay`).

**To repeat:**
1. Copy `sim/Suborned.Sim` and `sim/cli` to a scratch folder.
2. Add `probe/SutureProbe.cs` to the copy, and put `probe/Program.cs` and `probe.csproj` beside it.
3. `dotnet build -c Release`.
4. From the repo root, run `RUNS=L3,P-G POLS=matrix,reset-kite dotnet <probe.dll> <rulebook.json>`. Set `LOG=L3:reset-kite` to print the fight log, `MICRO=1` for the micro probes and `FIELD=1` for the field-on variant.

**Replayable scripts** are in `scripts/`. Each replays identically on the current tree with `bun run sim play briefs/027/suture/scripts/<file>`; I checked this with the src2 build.
- `focus-reset-kite.L3.script.json`: success at 4:43.3, 24 T, A 2, 19 traced shots, 0 hits taken.
- `reset-kite.L3.script.json`: 4:44.9.
- `matrix.L3`, `matrix.P-G`, `reset.P-G` and `enfkite.P-G`: the controls.

**Raw output** is in `evidence/`:
- `smart-1.txt` and `smart-snap2.txt`: policies × runs, on each tree;
- `micro.txt`;
- `accstart.txt`;
- `fix-*.txt`;
- `matrix-baseline.txt` and `matrix-snap2.txt`.

## 2. Findings

### F1 · Accumulator reset by re-aiming (high)
**Rule read:**
- `Aim()` (GameCombat.cs) sets `Acc = acc.start` (500) whenever `Gun.Target` changes.
- `cease`, `stance hold`, `holster` and `OrderEnd` all clear `Target`.
- So "cease + fire" on the same target, or a switch to another target, restarts the count at 500.
- aimNew (5 ticks) is shorter than the sidearm's cadence (10), so the re-aim is free.

**Reproduction:**
- `MICRO=1` gives the pattern table and a target-mode volley on the guard.
- In play: Scout drawn on the guard at 12 m (3 pips). After each shot: `{"do":"cease"}`, then `{"do":"fire","target":"guard"}`.

**Numbers:**

| pips (‰) | plain, 8 shots | reset after every miss-making shot |
|---|---|---|
| 1 (250) | ○●○○○●○○ (2/8) | ○●○●○●○● (4/8) |
| 2 (500) | ●○●○●○●○ (4/8) | ●●●●●●●● (8/8) |
| 3 (600) | ●○●○●●○● (5/8) | ●●●●●●●● (8/8) |

- Volley on the guard (30 health): plain volleys take **5 shots for 3 hits**. Volley(1) with a cease between takes **3 shots for 3 hits** (`micro.txt`).
- The chip (`PreviewFire.ShotsToDown`) and the volley forecast both read the accumulator, so the HUD shows the player the trick: a fresh aim reads one shot less.

**Impact:**
- Alone, `reset` changes no prepared run: P-A 0 %, P-G 27 %, P-B 22 %.
- In L3 it is the half of the exploit that beats the seal clock. With `kite` it doubles a one-pip shooter's rate (F2).

**Prototype fix:** re-aiming the **same** target keeps its accumulator; a new target still resets. I built it in a scratch copy only (`probe/fix-prototypes.diff`, `SUTURE_FIX=2`). Results (`fix-warm.txt`):
- the prepared runs are byte-for-byte the same as the matrix (P-A 0 %, P-G 27 %, P-B 22 %);
- matrix L3 gets dearer: 55 %, 2 down, still a heavy debrief;
- `reset-kite` L3 now **fails sealed** at 5:05.

A stronger version, "the accumulator is the shooter's and never resets" (`SUTURE_FIX=1`, `fix-accshooter.txt`), breaks the prepared balance (P-G 4 down). The matrix numbers lean on the fresh-target bonus (F3).

**Still open with fix 2:** A→B→A bouncing within one cadence when two targets are in reach. In my L3 runs it did not win (`cycle-kite` under fix 2: sealed). Motive should pick the rule. One option: "a re-aim within the cadence never raises the accumulator". Another: aimNew ≥ every.

**Owner:** Motive (rule), then Keel (engine and a scenario).

### F2 · The stand-off band, and kiting response officers (critical with F1)
**Rule read:**
- Hostiles choose foes only inside their weapon's **effective** ring (`FoesInReach`: 15 m for the lab sidearm).
- The cell fires out to **max**: sidearm 18 m, carbine 30 m.
- Police hold their stands (`IsHolder`).
- Response units and other pre-fight bodies (`PreFight`) shoot or do nothing; they never close on a shooter they can't reach.

**Reproduction:** `RUNS=L3 POLS=focus-reset-kite`, or `scripts/focus-reset-kite.L3.script.json`.
- The script is route G-fast with the guard shot at 2:03.6 (L3).
- The cell then keeps 15–18 m from anything armed and resets after each shot.

**Numbers:**
- Current tree (`smart-snap2.txt`): the result is the same as on the first snapshot.
- In the fight log, **the Tech alone** fires at 15–17 m at one pip, ○●○●, and downs:
  - the rivals (driven back);
  - security-1 and security-2 (the response unit);
  - officer-1 and officer-2 (the loud final fight).
- **No hostile fires a shot.**

| L3, lab | result | end | T | A | Standing | down | health lost | hits | verdict |
|---|---|---|---|---|---|---|---|---|---|
| matrix (Motive's row) | win | 4:50.3 | 22 | 2 | 2 → −2 | 1 | 33 % | 6 | heavy debrief |
| kite alone | fail, sealed | 5:05.0 | 18 | 2 | 2 → −1 | 1 | 22 % | 4 | (fail) |
| reset + kite | **win** | 4:44.5 | 24 | 2 | 2 → −1 | 0 | **5 %** | 1 | **FAIL**: no op down, < 40 %, no case |
| focus + reset + kite | **win** | 4:43.2 | 24 | 2 | 2 → −1 | 0 | **0 %** | **0** | **FAIL** |
| cycle + kite | **win** | 4:47.0 | 24 | 2 | 2 → −1 | 0 | 5 % | 1 | **FAIL** |

- §6.4 triggers on these rows: **6.4.1** (a loud win with 0 hits / < 25 %) and **6.4.6** (it fails under today and wins under the lab without a heavy debrief).
- 6.4.3 (a loud win more than 15 s before P-G) does not fire: these wins come at 4:43–4:47, against P-G's 4:11.

**Band probes** (rulebook copies `rb-snap2-max-15/16.json`, current tree with cover snap, no fix; `evidence/band-max.txt`):
- Sidearm max 15 m or 16 m starves the scripted kiter: L3 kite and focus-reset-kite fail sealed, with 1 foe down. Matrix L3 stays a heavy debrief (38 %).
- But **P-G rises to 33 %** (6 hits), because the cell's 16 m shots at rival-5 go away.
- So closing the band on the cell's side costs the prepared runs.
- The better lever is AI side, which needs Pulse. For example:
  - a body shot from outside its reach takes cover or advances to its effective ring;
  - or it fires back at the long band at 1 pip. This was Keel's proposal 1b, not adopted.

**Untested risk:** the carbine's own band is **15 m** wide (eff 20, max 30, against the hostiles' 15). No scripted loud line uses it: the Enforcer is inside, or walking Vale, during every fight, and my `clear` variant died on the entrance camera first. Judge it by hand in the playground.

**Owner:** Pulse (hostile AI out of reach), with Motive (band and rings). Severity: critical. This is the "shoot your way through" Sky asked us to rule out, and it takes one agent and two clicks a second.

### F3 · First shot always hits; lethality measured one way only (medium)
- acc.start 500 plus any step ≥ 500 reaches 1000. So every **new** target's first shot hits at two pips or more, on both sides.
- **The cell:** three sidearms on an uncovered rival drop it on their first volley, **0.5 s after taking it**. In the P-G log, rival-4 drops 0.8 s after the spring.
- **Motive's §3.3** says "a three-on-one focus drops a rival in about 1.5 s". That holds only for a rival in cover (1 pip: ○ then ●), as rival-2 shows at 1.9 s.
- **The hostiles:** P-G's opening volley lands **2 of its 5 hits** at 0.5 s. rival-2 hits at 500 ‰ and rival-4 at 600 ‰, both first shots. This is the "nothing can answer" Motive found.
- **§6.4.7** checks only hostiles → op (2.5 s and 4.5 s: confirmed). There is no ceiling on cell → hostile, which is 0.5–0.8 s.
- **acc.start sweep** (`accstart.txt`, lab only):
  - 0: P-G 27 %;
  - 250: P-G 38 % with 1 down;
  - 400 and 499: 44 % with 1 down.
  
  Lowering it slows the cell more than the ambush, so it is **not** a P-G lever.
- **Owner:** Motive. Options:
  - add a cell → hostile line to §6.4.7;
  - a separate accumulator start for ambushers (an "ambush start" below 500), to blunt the opening volley without slowing the cell.

### F4 · Panic spam under the police warning (low–medium)
**Reproduction:** `MICRO=1` runs `PanicSpam`.
- The start is security 1, with the camera looped and the patrol rerouted.
- Scout and Fixer stand in a spot the guard watches.
- `panic`, holster after 0.5–1.5 s, and repeat.

**Numbers:** 30 cycles open **60 warnings**. The run ends at:
- Alert **1** (max 1);
- Trail 2, all of it from the setup;
- Standing 2 → 2;
- **no** drawn.seen, and the guard is not engaged;
- guard memory 8, the cap: saw-weapon-drawn ×8.

**What it buys:** free probing of who watches, and a cell that sits drawn and ready beside security. While a warning is open, the agent is not a foe (`FoesInReach` skips Warned).

**What it costs:** the guard remembers faces, so the safe branch's guard ally is lost.

**Owner:** Motive. Suggestion: the second warning from the same watcher in a run is drawn.seen; no second chance.

### F5 · Sidearm target mode costs nothing (medium, target mode only)
**Rule read:**
- The sidearm has no own charge.
- `LastCharged` is set only by charged shots.
- So the bar regenerates 1 a tick (10/s) during sidearm volleys, while a targeted sidearm shot costs 10 at one shot a second.

**Numbers** (`micro.txt`):
- volley(1) again and again for 30 s: the bar's low point is **88 of 100**;
- volley(6): low point 40, and full again within 6 s.

With F1 (cease between volleys), each volley(1) hits: 3 for 3.

**Owner:** Motive → Keel. Suggestion: targeted shots also stamp `LastCharged` (so regen pauses), or a lower regen while a volley is queued.

### F6 · Walk-in forecast (no exploit found)
- `fire.close` and an out-of-range volley walk to the nearest point inside the **shooter's** effective ring, 15 m.
- That point is also inside the hostile sidearm's 15 m ring. So the walk always ends in mutual range and cannot reach the F2 band; only a manual move gets there.
- `WalkExposedTo` uses the foes "as they stand now". That is a forecast, not a promise, and I found no way to profit from it.
- The volley holds its shots until the stop point and lands exactly as forecast (N149 holds).
- **Note for Glyph and Latch:** the walk ends exposed by design. The stop-point ghost should read as "inside their ring".

### F7 · Freeze queue, and what the matrix proves (high, process)
**The host rules** (playground §1.2, `presets/lab.json`):
- each agent queues one action;
- auto freezes need a 3 s gap, but **manual freezes are unlimited and ignore the gap**;
- the freeze shows the hostiles' focus lines (`Gun.Target`).

**Abuse in freeze mode:**
- **One queued action is enough for F1.** Queue "fire on B" for an agent that is shooting A, and the accumulator resets.
- **The opening alpha needs no freeze.** On the ambush freeze, three sidearms queued on one uncovered rival kill it on release (F3). The matrix player's zero reaction already does the same.
- **A manual freeze is a pause on demand,** even with `pauseInFight: false`.
- **The focus lines are information a pause does not give.** That contradicts §1.2's "a freeze gives no information a pause doesn't". It is minor, but the doc should say it.

**The real problem:**
- Playground §0 says "the script bounds all three modes from above".
- That is true only if the script plays optimally. It doesn't: on the same routes, F1 + F2 turn L3 from "heavy debrief" into "0 hits".
- The matrix is a regression check, not an upper bound.

**Owner:** Keel. Add adversarial player rows to `fight-lab` (at least `L3:reset-kite` and `L3:focus-reset-kite`; `SEngage` in `probe/SutureProbe.cs` is a ready reference), so §6.4 is judged against them.

### F8 · Cover snap breaks lab P-B (high, new)
**Seen on src2,** with B5 `fight.lab.coverSnap.on: true` (`matrix-snap2.txt`):
- lab P-B **fails rivals-reach-vale at 8:30**, with 0 shots, Standing 2 → −1, and trigger 6.4.5;
- today's P-B still passes.

**Cause** (`LOG=P-B:matrix ALL=1`):
1. At 0:00 and again at 1:17.6: `scout heads for d:Meet driver and takes cover at cv-008 (car car/official)`.
2. 0:57.6: `watch driver: stand within 6 m of driver` is refused. The pay approach's knowledge never comes, so the turn and the listen are refused at 1:17–1:19.
3. 6:10: `enforcer take: stand beside Vale` is refused. The route falls apart.

**Proof:** with `coverSnap.on: false` (`rb-snap2-nosnap.json`), lab P-B passes again (8:33.2, 22 %), and L1/L1c return to their earlier rows.

**The snap fires out of a fight, on a named-place move, with no combatant anywhere.** B5's text is "the cell kneels behind the planters as the rivals do". Suggestion: snap only while drawn or in a fight, and never on a move to a named place (`d:` or `crew:`).

**Also:** cover snap does **not** move P-G in the matrix (still 27 %, 5 hits). The crew posts have no cover point within 1.5 m. So Motive's fix (a) for P-G is not yet shown to work.

**Owner:** Keel. Latch should know before Sky plays P-B in the lab preset.

## 3. Motive's targets: confirm or dispute

| run | Motive's target / tuned result | my check | call |
|---|---|---|---|
| L1 | fail (camera 0:56) | same | **confirm**. It tests nothing about fights; keep L1c as the probe |
| L1c | fail sealed, 33 %, 1 down | every policy also fails (Enforcer down, camera or sealed) | **confirm** |
| L2, L4 | fail sealed, 0 % | matrix reproduced; not re-probed with the policies (they are sealed before the fight) | **confirm**, not stress-tested |
| **L3** | **win, heavy debrief** (22 T, A 2, 2 → −2, 1 down, 33 %) | matrix reproduced; **reset + kite wins with 0–5 % and no op down** | **dispute**. The heavy debrief holds only for the matrix player. Trail, Alert, Standing and investigation are met in every line; only the casualty line is avoidable, and F1 + F2 avoid it. With prototype fix 2, L3 is heavy (55 %, 2 down) and the cheap line fails sealed |
| P-A | win, 0 % | 0 % under matrix, reset, focus and enfkite; kite is worse | **confirm** |
| P-B | win, 22 % | 22 % (first tree); **fails rivals-reach-vale on the current tree** (F8) | **confirm the numbers; regression open** |
| **P-G** | win, **27 %** vs the 25 % line | 9 policies, none under 27 % (5 hits): reset, enfkite, reset-enfkite, focus, cycle, kite and combinations; the rest were worse. Field on: 27 %. Cover snap: 27 %. acc.start sweep: worse | **confirm the floor.** Two of the 5 hits are F3's guaranteed opening volley. I support Motive's (b) for now: read P-G at ≤ 33 % (≤ 6 hits) with no op down, keep 25 % for P-A and P-B, and revisit if cover snap is reworked or an ambush start (F3) is tried |
| lethality | op 2.5 s, Enforcer 4.5 s | same (`Lethality`) | **confirm**, but it is one-sided (F3): cell → rival is 0.5–0.8 s |
| §3.3 "focus drops a rival in ~1.5 s" | — | 0.5–0.8 s uncovered, ~1.9 s in cover | **dispute** (wording) |

**Doc nits** (Motive, rulebook reasons):
- `lab-target-carbine-charge` still says "on top of the carbine's own 8"; the carbine is now 20.
- `fight-lab.json` carries inputs `6efad04c…`. The current trees hash `d001d608…` and then `ec12cac1…`. The behaviour rows matched on the first tree.

## 4. Handoffs (inputs · done-check · next owner)

1. **Motive.**
   - **Inputs:** F1, F3, F4, F5, the P-G line (§3) and the doc nits.
   - **Done-check:** rulebook rules for the re-aim (fix 2 or a variant), the warning's second chance and the volley regen. Then `L3:reset-kite` fails or is heavy, and P-A, P-G and P-B are unchanged.
   - **Next owner:** Keel.
2. **Pulse.**
   - **Inputs:** F2. Bodies shot from outside their ring must respond, by taking cover, advancing or firing at long range at 1 pip.
   - **Done-check:** `L3:focus-reset-kite` takes hits from the response unit and police, or fails sealed.
   - **Next owner:** Keel.
3. **Keel.**
   - **Inputs:** F8 (the cover snap scope), F7 (adversarial rows in `fight-lab`, from `probe/SutureProbe.cs`), and the engine side of 1 and 2.
   - **Done-check:** lab P-B passes with the snap on; `bun run sim fight-lab` prints the adversarial rows; `dotnet test` stays green.
   - **Next owner:** Suture to retest.
4. **Latch and Glyph** (FYI, nothing to build):
   - P-B in the lab preset is broken until F8 is fixed;
   - the walk-in ghost ends inside the target's ring (F6);
   - the freeze's focus lines are new information (F7).
5. **Suture:** retest F1, F2 and F8 when Keel reports, using the same scripts and `SEngage` policies.

## 5. Evidence (commands run, 2026-10-20)

- `dotnet out/suture027/src/cli/.../suborned-sim.dll fight-lab --rulebook out/suture027/rulebook.snapshot.json --presets today,lab`: the matrix is identical to `fight-lab.json`, and every run replays identically (`evidence/matrix-baseline.txt`).
- `dotnet out/suture027/src2/cli/... fight-lab --rulebook out/suture027/rulebook.snapshot2.json --presets lab`: P-B fails, trigger 6.4.5 (`evidence/matrix-snap2.txt`).
  - The same with `rb-snap2-nosnap.json`: P-B passes.
- Probe runs: `evidence/smart-1.txt` (first tree) and `evidence/smart-snap2.txt` (current tree). Every probe run replays identically from its order log.
- `bun run sim play`-equivalent (the src2 CLI `play`) on all six `scripts/`: each prints "replay identical" with the outcomes above.
- Fix prototypes: `evidence/fix-warm.txt` (fix 2), `evidence/fix-accshooter.txt` (fix 1), `probe/fix-prototypes.diff`. Scratch only; **not** a proposal for the sim code as written.
- Micro probes: `evidence/micro.txt`. acc.start sweep: `evidence/accstart.txt`.

## 6. The crashed session

- Its probe (`out/suture027/probe`, git-ignored) ran its own L3 scripts on the same first tree (`d001d608…`).
- Its tables are kept in `earlier-session/` as found.
- They agree on the main point: **L3:kite** is a cheap win (lab: 5 %, `FAIL CHEAP-WIN`).
- They also report two things I did **not** reproduce with my harness (`FIELD=1`: L3 field + reset ends sealed, or 5 % with kite):
  - **L3:field+focus** is a cheap win with the field on (22 %, 27 hits absorbed);
  - a **today** L3 that wins, where Keel's today L3 fails.
- Treat both as unverified leads, not findings.
