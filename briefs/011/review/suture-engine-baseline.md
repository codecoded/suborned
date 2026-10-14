# Suture · engine baseline · brief 011 A at 5448655 · 2026-10-15

I re-baselined the 28 scenarios and Keel's N cases on the engine as committed at 5448655. I did not edit the engine.

**Why a pinned copy:** the working tree's `sim/` changed while I worked (`GameFight.cs` is new, and `Game.cs`, `GameOrders.cs`, `Hash.cs`, `Scenarios.cs` and `handover.site.json` are modified, all uncommitted). So I extracted the commit with `git archive 5448655 sim contracts/rules contracts/people/people.rules.json` into `out/suture/engine/at-5448655/`, built it there in Release, and ran everything from that copy.

My first pass ran on the tree's Debug build before those edits. It gives the same final hash for all 33 runs, so it was the commit too.

## Verdict
- **The engine passes the 28 and Keel's N cases** (N08, N09, N09b, N14, N15). Each was played twice: 0 failed checks, identical hashes, replay identical. 113/113 xunit tests pass.
- **Every scenario keeps the intent the test plan recorded.** Keel changed 16 of them, and all 16 changes are fair. Two are fair only with a caveat (S05, S21), and S24 and S25 follow Sky's ruling (S24 with caveat E-4).
- **Every §07 fail is reachable** with my probe scripts; Waymark's scripts don't exist yet.
- **The main gap isn't in the engine:** the engine plays a hard-coded 17:00 cast that the mission checker (M-2) now rejects at the 17:24:30 start (E-1).

## 1. Results
`python3 -I out/suture/engine/summary-pinned.py` → `out/suture/engine/pinned/summary.txt`

| check | result |
|---|---|
| 33 scenarios × 2 runs | 33/33 OK: 0 failed checks; start, checkpoint and final hashes identical across the runs; `replayIdentical` true |
| Debug (tree, before the edits) against Release (pinned) | identical final hashes for all 33 (supports D4) |
| `dotnet test sim/Suborned.Sim.Tests` | 113 passed, 0 failed |
| `bun test tools/sim contracts/rules` | 33 pass (includes the site-is-fresh test) |
| `perf 20` (Release) | 45,449 sim-s per wall-s, 6.8 ms a route A run (Keel: 50,053 on the M5 Max) |
| Sky's rulings | Listen reveals temperament (`preview.knownBy = ["scout.listen"]`); Pay on Idealist Inez is refused (S24); moves across the block cost T 0 and A 0 (S27, S28, N08) |

Outcomes:
- **Route A, quiet (S10):** wins at 5:09.7; $60,000 left, T 3, A 0, Calm.
- **Route B, quiet (S20):** wins at 7:26.8; $60,000, T 2, A 0, Calm.
- **Loud front (S11):** wins at 3:45.2; $100,000, T 6, A 2, Raised.
- **Witnessed front (S05):** wins at 5:37.1; T 8, A 3, High.
- **S09:** `vale-dead` at 6:10.

## 2. Do the changed scenarios still test fairly?
Variants: `python3 -I out/suture/engine/variants-pinned.py` → `out/suture/engine/pinned/variants.txt`.

| scenario | Keel's change | judgement |
|---|---|---|
| All | actions land at the end of their rulebook time; Watch gives routine, Listen gives need and temperament; guard walk 45 s; over at 8:00; [B] health | **Fair.** These are rulings or 010 fixes, and each scenario's checks still assert its intent |
| S01, S03, S04, S06, S07 | the prototype's direct state edits became orders (S01) or a hashed start state with Inez turned | **Fair:** replay = start state + orders, and the start hash is in the output |
| S05 | adds a Scout false tip at 3:12.9 | **Fair, with a caveat (E-5).** Without the tip the same orders fail: `enforcer-down` at 5:12.6. The prototype won without it. The intent holds (narrow escape, High, "Inez witnessed"), but only when a second operative's verb is added. Keep the no-tip run as a recorded fail variant |
| S10 | the call is timed so the sweep covers the door at the first opening | **Fair.** A second-by-second scan of the entry from 2:55 to 5:39 (`window-scan.py`):<br>• 3:00–3:52: patrol sees the entry<br>• 3:53–4:27: entry succeeds<br>• 4:28–5:23: patrol sees the entry<br>• 5:24 on: the guard is back<br>"Waiting makes an opening" holds on real timings (E-6) |
| S13, S15 | shoots from `BesideDoor`, out of the guard's cone and both cameras | **Fair:** the same geometry the prototype's 5-unit step made |
| S17, S19 | teleports became walks (onto the rivals' way in; to the north street) | **Fair:** no state editing, and the intent holds |
| S20 | adds Listen at the car (42 s) | **Fair** (Sky's ruling). The turn lands at 3:23.1, before 5:40. The finish at 7:26.8 is 33 s before 8:00 (E-2) |
| S21 | starts with the driver turned | **Fair as a mechanics test only.** From a natural turn (S20's orders), one blockade fits and the route wins at 7:26.8, T 4. A second blockade is refused ("too late: the car leaves for the pickup at 5:40"). So the two-blockade state the scenario asserts can't be reached in play (E-3) |
| S22 | listens from `beside-inez` (the Courtyard is 3.5 m from her; Listen's reach is 3 m) | **Fair** |
| S24 | Pay refused and remembered: $100,000 kept, +2 T, "?" before Listen; Pay accepted moved to N09b (a Mercenary Inez) | **Fair** per Sky, with a caveat (E-4) |
| S25 | Pressure +4 T (one step worse) | **Fair** per Sky |

## 3. Against my 15 added cases
| case | status |
|---|---|
| N1 vale-leaves | No script (Waymark). Reachable: mislead at 5:20, no take → `vale-leaves` at 8:00 |
| N2 rivals-reach-vale | No script. Reachable: an empty script → `rivals-reach-vale` at 8:00 |
| N3 sealed | No script. Reachable: S14's or S15's orders, then walk away → `sealed` at 5:05.4 or 5:34.0. Walking off in the guard's cone gets `enforcer-down` first |
| N4 enforcer-down | No script. Reachable: S12 extended → 0:43.4 (the guard and the chasing patrol both fire) |
| N5–N7 entry seen | No scripts. Reachable: guard (enter at 1:00, no lure); camera (S10 without the Tech, 3:52.9); patrol (S10 entering 3:10–3:50) |
| N8 withdraw | Done (N08) |
| N9 purse | Done (N09: $80,000 fits; N09b: Mercenary Pay leaves the driver unaffordable) |
| N10, N11 final fight | Not yet. Work in progress in the tree (`GameFight.cs`), not committed, not baselined |
| N12 people step routines | Not yet. Only the four cast roles exist; the 10 other door guards and everyone's routines are invisible (E-1) |
| N13 3D witnessing | Partial: one eye height per line (`Occluder.Blocks(a, b, z)`); street-to-deck lines aren't modelled (E-7) |
| N14 card, refusal, wrong approach | Done |
| N15 K-rules in the mission | Partial: K1, K2, K8 and the Trail ledger. K3–K7 and K9 are between missions |

## 4. Findings
| id | finding | evidence | owner |
|---|---|---|---|
| E-1 | **The engine's cast is hard-coded, and M-2 rejects it.** `tools/sim/site.ts` writes the four roles as constants, "as M-2 cast it at the 17:00 start". At Sky's 17:24:30 start, M-2 fails: driver, guard and councillor can't be cast. The guard check finds "at most one guard … 11 at 17:26:54"; no driver has a workmate tie to a councillor; no councillor's routine reaches Meet driver by 6:10. The site-is-fresh test still passes, so nothing connects the two. §01: systems > scripts | `bun tools/plan/cli.ts --overlay tools/missions/handover/overlay.ts` → M-2 FAIL; `tools/sim/site.ts:128` | Keel (the site reads M-2's pins and fails when M-2 fails), Waymark (pins), Pulse (cast) |
| E-2 | **Route B finishes 33 s before 8:00.** The take can't start before Vale reaches the service lane at 6:10; take (38 s) plus the walk to the getaway bay (38.8 s) gives 7:26.8. If 8:00 is route B's closing event, §09's "≥ 1:00 before" fails for every route B | S20 and my variants, all 7:26.8 | Waymark (closing event), Motive (timings) |
| E-3 | **The camera-block branch is not a route.** With the driver turned, the pickup always moves to the service lane (`PickupService = Driver.Turned`), and a block call is refused after 5:32. A blockade never protects an intercept at the official bay; it only adds Trail. Same in the prototype. §07 route B says "pickup moves to service lane, or driver blocks pickup camera" | `Game.cs:225`, `GameOrders.cs` Call | Waymark, Motive (or cut: the camera-block variant is third in the §11 cut order) |
| E-4 | **A refused approach teaches the temperament.** The rulebook's preview rule says "Any approach's outcome tells the cell the temperament", and S24 checks "the cell now knows her temperament". Sky ruled "Only Listen reveals temperament". The natural reading says a refusal shows it, but the wording conflicts | `contracts/rules/rulebook.json` preview.rule; S24 | Aster → Sky (one line) |
| E-5 | **The loud, witnessed front route needs the Scout's false tip.** Without it the drawn rivals and security down the Enforcer at 5:12.6 | variant `S05-no-mislead` | Motive (T2 context); keep as a fail variant |
| E-6 | **Route A has a 35 s entry opening without a reroute** (3:53–4:27, after the 2:53 call). The patrol covers the door 3:00–3:52 and 4:28–5:23; the guard returns at 5:23. Players must read the sweep: Glyph's Next Event and sweep cone matter here. Waymark's V-delay will find the margin | `window-scan.txt` | Waymark, Glyph, Pulse (T1) |
| E-7 | **Sightlines run at one height**, so §05's "a slab blocks sight straight up and down, an open edge does not" isn't modelled. The Handover is street-level, so this doesn't block week 3 | `Tests.cs` N13 | Keel (before levels matter) |
| E-8 | **Loud is the fastest win:** S11 at 3:45.2 with $100,000 kept, against quiet routes at 5:09.7 and 7:26.8. T2 asks that informed quiet runs finish faster. Its costs are T 6 against 2–3 and Raised. If players find this, T2's second criterion fails | summary | Motive (T2) |
| E-9 | **Fail codes beyond §07's list:** `driver-shot` ("Security pulls Vale away") and `abduction-seen-camera`. The second reads as §07's "seen by live camera"; the first isn't in §07 | `GameOrders.cs:461, 504` | Aster (GDD mismatch, minor) |

## 5. Evidence commands
```
git archive 5448655 sim contracts/rules contracts/people/people.rules.json | tar -x -C out/suture/engine/at-5448655
DOTNET_ROOT=/opt/homebrew/opt/dotnet/libexec dotnet build out/suture/engine/at-5448655/sim/cli -c Release
bash out/suture/engine/run-pinned.sh            # 33 scenarios × 2, plus perf 20
python3 -I out/suture/engine/summary-pinned.py   # determinism, checks, outcomes
python3 -I out/suture/engine/variants-pinned.py  # S05/S21 fairness, fail reachability
python3 -I out/suture/engine/window-scan.py      # route A entry window
dotnet test sim/Suborned.Sim.Tests ; bun test tools/sim contracts/rules
bun tools/plan/cli.ts --overlay tools/missions/handover/overlay.ts   # M-2 FAIL (E-1)
```

## Hand-offs
- **Keel:**
  - E-1: the site takes its cast from M-2's pins and fails when M-2 does.
  - N10–N12; E-7.
  - Commit or park the in-progress fight work, so I can baseline it.
- **Waymark:** the route and fail scripts (my probes in `out/suture/engine/pinned/variants/` are starting points); the closing events (E-2); E-3; the pins; O-6.
- **Motive:** E-5 and E-8 for T2; E-3.
- **Aster → Sky:** E-4; B-2 (whether the mission or the routine wins for pinned roles); E-9.
- **Me:** re-baseline at Keel's next commit; then D2 and D3 when Latch's host lands.
