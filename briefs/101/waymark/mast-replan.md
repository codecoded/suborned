# Brief 101 · Waymark: the routes re-planned for the square's mast, and two rigid lines (engine 0.52.0, 2026-11-09)

Branch `t3/8de1bbaa`, from `work/098-living-playbook` (88f73a949), with Keel's `t3/fix-mast-sim-scenarios` merged in (43acbc06d: engine 0.52.0 on
Vantage's lamp mast and Vela's `MastCamera_Lamp`). Read: brief 101 "The square's camera: Sky picks B", `briefs/101/motive/mast-pressure.md`,
`briefs/101/vantage/square-camera.md` §B and §5, `briefs/101/keel/report-lamp-mast.md`.

## The short answer

- **Every band in the brief holds.** B-1 quiet-a 100 %, B-2 thin-quiet 89.5 % (it drops into its 60–90 % from 91.5 %), B-3 0 %, B-4 53.5 %,
  B-6 53.5 against 0, B-8 quiet-a p95 31 s and prepared-loud p95 15 s, L96 B-thinned out 99.5 against 100, all out 63.3 against 32.5.
- **The two rigid lines are in the bands and green:** R-8 (§09, no prepared quiet plan has a loiterer reported: 0 of 200 seeds each) and R-9 (the
  cell's start out of every clock-keeping watcher's sight: 200 of 200 seeds).
- **The record is c064** (`bun run missions:rerecord`): HOLDS on c063's red less B-2, nothing new red. MC-0 23/23 and MC-12 3/3 again.
- **One report line is under Motive's expectation:** thin-quiet's Pressure is taped on 73.5 %, not 90 % or more. The cause is the crowd (§6).

## 1. The re-plans

Each plan is composed or played on the engine; no composed script was edited by hand, and no value was moved.

### Route A: the waits behind the mast

`quiet-a` (adaptive), `route-a`, `route-a-board`, `route-a-disable`, `careless-loop-runs-out` (plans → scripts).

- **The Scout watches Inez from her start** (17 m, a clear line, behind the lenses), with no run to `beside-inez`. Beside Inez the Watch's 20 s ran on
  the mast's clock. The Listen then walks to its discreet spot (18 s on seed 0).
- **The Fixer waits at her start, behind the mast**, walks in only for the Help (never taped), walks back to `crew:fixer` once it ends, and makes the
  Call from there (a Call has no reach: `GameCall.cs` checks no distance). At the Courtyard the mast reported her at 45 s on every seed.
- **quiet-a only: the Scout walks back behind the mast after the Listen.** On seed 40 Inez's round held the Listen off until 2:17, its spot was in the
  mast's view, and idle there after it she was reported 45 s later. The seed-0 scripts don't take this step: on seed 0 her spot is out of view, and
  there it would remove the one case Keel's `BodiesIdleTests` law needs (an idle agent off her placed spot making room for passers-by).
- **careless-loop-runs-out** takes route A's opening too, so it differs from its twin by its one mistake (the loop planted first and the call late):
  it fails `entry-seen-camera` at Alert 0 again, as its manifest says (on 0.52.0 before the re-plan the mast's report made it Alert 1: MC-0 22/23).
- **route-a-disable (checked):** the same opening; success at Alert 1, T 4, from the dead screen's report only, as its manifest says.
- **What it gives:** quiet-a 200/200 and no loiterer reported (200 of 200 on 0.52.0 before the re-plan; 81 of 200 on c063); whole cell idle p95 31 s,
  worst 37 s (84 s before). route-a on seed 0: success at 4:11.4, T 3, **Alert 0** (Alert 1 before: the mast reported the Fixer at 0:38).

### prepared-loud

Its waits move with route A's: the Scout watches from her start, and the Fixer waits behind the mast, walks in for the Help and goes from Inez to her
yard post. Loiterers reported: 200 → 3 of 200. B-4 53.5 % (c063 54.5 %): in its band, but 3.5 points over the floor, so read it carefully, as Motive
said. B-8 p95 15 s.

### thin-quiet and careless: unchanged

Motive: keep thin-quiet thin (no Watch or loop of the mast, no switch to Help, waits where they are). careless stays careless.

### route-a-pressure and route-a-pressure-board: loop the mast across the Pressure

- **Why the loop, not the tape:** the Pressure card shows "the mast would tape it: Alert 1 in n s, +1 T". The loop costs the same +1 T and buys
  silence: no Alert, no robots sent to her post, no face on file, and no log of the Fixer beside the turned Inez, so her job stays unexposed (a pressured
  Inez with her job exposed becomes an Informer). Its only price is time, and route A has the time. A player reading both cards loops.
- **The plan:** the Tech loops the mast from the cabinet (0:32.5 to 2:32.5 on seed 0). The Scout watches Inez from her start and listens beside her
  inside the loop. The Fixer walks in from behind the mast for the Pressure (1:21.4 to 1:59.4) and walks back before the loop runs out. The step carries
  `unless: camera.recorded`, so the composer proves no camera records it.
  - The call goes when the rival car is seen coming (2:35.6).
  - The entrance loop follows the mast's (one at a time): live from 2:52.5.
  - The Enter starts at 2:56.6, inside the pressed guard's window (Motive: a call at 2:35 keeps him away to about 4:14).
- **What it gives (seed 0):** success at 4:24.8, Alert 0, T 7 (the old ticks on the new site: failure `vale-leaves`, Alert 3, T 37). The board twin:
  success at 4:25.9, Alert 0, T 7.

### route-b-thin: the knockout inside a loop, the body hidden

- **Why more than a loop:** rival-3's post (`ambush-2`, (−5.75, −37.75) m) is in the mast's view. A takedown there is seen (Alert 3). A body left
  there is found once the loop runs out: a camera finds an open body (`GameTakedown.cs` `WatcherNotices`), and a knockout lasts 2:00, as long as a loop.
- **The plan:**
  - Once the pickup camera's loop has run out, the Tech loops the mast (planted at 4:15, live 4:35 to 6:35). He is free again for the Reroute at 4:40.
  - The Enforcer knocks rival-3 out inside it, ties him, carries him 20 m to the café's kitchen-door stash and goes on to the service lane.
  - Why tied: untied, he comes round in the stash at about 7:10 and calls out, and the Office's staff reported the noise (Alert 1 before the Go). Tied
    and hidden he stays out, so the rearguard at our car faces one gun fewer for good. Before, he woke at about 7:08 and rejoined.
- **Two fixes that are not the mast's:**
  - The approach point was 2.1 m from his post, past the takedown's 1.5 m reach (refused `too-far`). The new point is 0.9 m off his back, on the free
    ground north of him.
  - After the driver's Pay the Fixer stood at the pavilion, in the pickup camera's view once its loop ran out, and was reported at 3:52. She now steps
    back out of its cone, as quiet-b-thinned's Fixer does.
- **What it gives:** success at 7:29.9, Alert 0, T 7, as its manifest says. Seeds 1–50 at fixed ticks: 50/50.

### careless-thin-all (checked)

- Careless, it takes no loop, so its first knockout, at rival-3's post, is now seen by the mast.
- Its approach point is fixed as route-b-thin's, so that step composes. The plan still stops at step 5, as on 0.48.0 (the 098 cleanup): rival-1 never
  sees rival-2 taken (`rivals.see-takedown` is not logged).
- Its script stands as at HEAD. It is retired, so the check doesn't play it.

### quiet-b-thinned: loop the mast, then pay

- **The card read.** A new plan condition, `taped:rival-n:pay`, in the plan driver (`sim/cli/Adaptive.cs`). It is the Pay card's tape mark
  (`CrewQuote`): "yes" for a watched camera, "?" for one not watched, the mark a player reads before the click. Anything else is rejected (R-6 6/6).
- **The plan:**
  - Once the Scout's Listen shows rival-2 a Mercenary and his card shows the mast's tape, the Tech loops the mast (by 4:45, so his 20 s end before the
    5:05 Reroute).
  - While the card shows the tape, the Fixer waits out of the mast's view by our car. She has just watched rival-2 in its view, 20 s on its clock.
    The Pay walks her back in once the card shows none.
  - After the Pay, or when no Pay comes (Loyal, or his ring still "?"), she goes to her rearguard post. Idle where she watched him, she was reported on
    52 of 200 seeds before.
  - The loop rule sits after the lookout's rules in the plan's order: the lookout's card is the crucial one. Given first, the loop took the slot the
    Scout's run to the lookout needed, and the lookout reached Popovic on 2 of 200 seeds.
- **What it gives:**
  - 99.5 % out. The one loss (seed 153) is a person's input gaps: the extra inputs push the Mislead 13 s later. I left it as the plan's price rather
    than fit the plan to a seed.
  - The tape shown on 75 of 200 seeds, the mast looped first on 68. On the other 7 the 4:45 lapsed, the Pay was dropped and the Fixer waited out of view.
  - Taped 0 (70 before). Loiterers reported 0 (52 before). All four out on 63.3 % of wins (64 %).

### The others

I composed all 27 plans on 0.52.0. The 20 that compose give the same bytes as before, apart from the seven re-planned. These don't compose:
`route-g` (step 25, the Go), `loud`, `loud-fight`, `loud-left-downed` and `loud-left-standing` (step 29, the Scout's move),
`careless-reporter-not-stopped` (step 20) and `careless-thin-all` (step 5).
- They don't compose on the pre-merge engine either (88f73a949, engine 0.51.0), so this predates the mast.
- The check doesn't play any of these scripts: route-g and loud play as their adaptive plans, the rest are retired.
- Their scripts stand, so the rerecord tool's compose step now names them and goes on (`--unplayed-ok`). It still stops on the plan of any script the
  check plays.

## 2. The rigid lines (`tools/missions/check/bands.ts`)

- **R-8, §09's "no quiet route has a watcher report a loiterer"**, held on every seed of each prepared quiet plan. Read from the bands' rows
  (`attention.reporting`). c064: quiet-a 0/200, quiet-b 0/200, quiet-g 0/200.
- **R-9, the cell's start out of sight.** No camera, guard or robot that keeps a clock sees any free point within 2 m of a start mark, at any tick of an
  idle mission (no orders), on any seed.
  - It is read from the engine: `suborned-sim stance`, new in `sim/cli/Stance.cs`. It uses the engine's own `SeesNow`, with the crowd off, since people
    only ever hide. A person's near sense counts by distance alone.
  - It covers 4,991 points on a 10 cm lattice over 706,000 ticks in 10 s. c064: 200/200 seeds unseen. Widened to 6 m it finds the mast's cones.
  - Over the whole idle mission it is stronger than the patrol's first-15-seconds rule (SEED-FAIR), and the patrol meets it too.
- **Motive's two report lines** (no band, never a miss): thin-quiet's Pressure taped, 73.5 % (147/200); quiet-b thinned, the tape shown on 37.5 %
  (75/200), looped first on 68, taped 0.

## 3. The bands, before and after

c063 is engine 0.51.0 with the old mast; "0.52.0, unchanged" is the merged engine with today's plans; c064 has the re-plans.

| line | band | c063 | 0.52.0, unchanged | c064 |
|---|---|---|---|---|
| B-1 quiet-a | ≥ 90 % | 99 % | 99 % | 100 % |
| B-1 quiet-b · quiet-g | ≥ 80 · ≥ 75 % | 100 · 97 % | 100 · 97 % | 100 · 97 % |
| B-2 thin-quiet | 60–90 % | 91.5 % (miss) | 89.5 % | 89.5 % |
| B-3 careless | ≤ 30 % | 0 % | 0 % | 0 % |
| B-4 prepared-loud (loud wins) | 50–75 % | 54.5 % | 55.5 % | 53.5 % |
| B-6 | ≥ 40 points | 54.5 against 0 | 55.5 against 0 | 53.5 against 0 |
| B-8 quiet-a (whole cell idle) | p95 ≤ 45 s | p95 30 s, worst 84 | p95 31 s, worst 84 | p95 31 s, worst 37 |
| B-8 prepared-loud | p95 ≤ 45 s | p95 15 s | p95 16 s | p95 15 s |
| L96 A (quiet-a) | 90–97 % | 99 % (miss) | 99 % (miss) | 100 % (miss, known) |
| L96 B-thinned | out within 3 points, all out ≥ +10 | 100 against 100; 64 against 32.5 | 100 against 100; 64 against 32.5 | 99.5 against 100; 63.3 against 32.5 |
| loiterer reported, quiet-a | R-8: 0 | 81/200 | 200/200 | 0/200 |
| loiterer reported, prepared-loud · quiet-b-thinned | none | n/a | 200 · 52 | 3 · 0 |
| MC-0 · MC-12 | ship lines | 23/23 · 3/3 | 22/23 · 2/3 | 23/23 · 3/3 |

R-7 stays at 11/12 (quiet-g's beside-guard window, known red).

## 4. The record: c064

`runs/missions/handover/c064`, recorded with `bun run missions:rerecord`: compose, check and record on the tree.
- HOLDS, red: R-7, B-8 quiet-g, B-9 quiet-b, B-11 ×7, B-14, B-16 drive-early, L96 A, L96 B-all.
- That is c063's list less B-2 thin-quiet. Nothing new is red.
- `check.test.ts`'s known-red list follows it, and simview's dump ends on c064's final hashes.

## 5. Pulse's bake tripwire: `rules.rounds.clear`'s walker retired

- **What the test says:** "the plain grid walk comes short of the drawn solids" asserted a short leg, with "if the grid ever keeps clear by itself, this
  rule has nothing left to do: retire it".
- **Why now:** on the new site no plain leg comes short (the old mast's case is gone), and since engine 0.49.0's walk law the grid keeps a body clear by
  itself. The bake with the walker and without it is byte for byte the same (13,351 bytes).
- **What I did:** the bake walks the plain grid walk.
  - The walker's code is gone: `walkerOf` and `easeLeg` in `rounds.ts` (505 → 453 lines), `inflate`, `pushes` and `closest` in `clear.ts`.
  - Its two knobs, `stepCm` and `tries`, are gone from `contracts/people/routines.rules.json` and its schema. The reason now says why.
  - `marginCm`, `faceCm` and `anchorCm` stay, as the measure the test holds every leg to. The test now reads the other way: a leg that comes short again
    fails it, and then the walker comes back.
- **The ripple:** the baked rounds differ only in the rules file's sha256, which moves the engine's inputs hash.
  - `hashes --start-text`: 534 of 534 scenarios and route scripts the same, so no run's behaviour moves.
  - DP1 is re-recorded (`suborned-sim drive-session`, as its own comment asks when the inputs move). Its orders are unchanged.

## 6. Open issues and next owners

1. **Motive**
   - thin-quiet's Pressure is taped on 73.5 % (147/200), not the 90 % or more expected. On the rest, the café's people stand across the mast's line:
     with the crowd off, 11 of 12 of those seeds are taped. That is the camera's crowd rule (`CameraWatching(..., crowd: true)`), and B-2 sits at 89.5 %.
   - B-4 is 3.5 points over its floor.
   - Next: Motive.
2. **Pulse**
   - `rounds.test.ts` "recorded routes … taken at its recorded tick" places each agent at her last move. Route A's Scout watches from her start, so she
     has "no place at tick 0". The Help walking in from behind the mast would also read as a wait.
   - It was red before this round on HEAD's scripts too: an approach waited 290 ticks.
   - Proposal: start each agent at `crew:<op>`, and let an act given out of reach walk in, as the engine has since 0.23.0.
   - Next: Pulse.
3. **Latch**
   - The host-logic cases that play `route-a` and `route-a-pressure` at fixed ticks (`HostLogic.Convo085`, `HostLogic.Quiet102`) were not run here:
     this worktree has no Unity Library.
   - Expected: the same scenes in the same order (Listen, Help or Pressure, Call). The Call is at 2:35.6, before tick 1600. route-a-pressure's Listen is
     given at 0:32.5, after the 280 and 300 checks.
   - The Call is now given from the Fixer's wait, 17–20 m from Inez. Whether `call-inez` plays from there is the host's question, and the words are Fable's.
   - Next: Latch.
4. **Keel**
   - Review the two `sim/cli` additions: `Stance.cs` and the `taped:` condition. Motive named you for the condition. Both are read-only, and no hash moves.
   - DP1 was re-recorded for the rounds file's new sha.
   - Next: Keel.
5. **Aster**
   - route-g and loud don't compose on 0.52.0 or 0.51.0. The check plays them as adaptive plans, but their scripts, which tools/qa, the host tests and
     the fight lab read, stand at their 0.48-era ticks.
   - My suggestion: Waymark next round.
   - Next: Aster.

## Checks

- `bun run guard bun run missions:check bands handover` (after the re-plans): RIGID R-1 to R-6, R-8 and R-9 ok, R-7 11/12 (known). 15 plans × 200 seeds
  in 118 s.
- `bun run missions:rerecord`: c064, HOLDS, as §4.
- `bun run sim:test`: 3,400 passed, 0 failed (150 s). One run in between failed `BodiesIdleTests.AnIdleOperativeMakesRoomAndStepsBackAsAnyoneDoes`
  ("made room 0 times"). Its cause was my seed-0 Scout walk-back (§1), now taken out of the scripts.
- `bun run test`: 2,746 pass, 7 fail. All 7 were red before this round (Keel's list):
  - the three stubbed skyline runs;
  - the two skyline and units checks (SK-3, the critic);
  - drift-auto (r011, r012–r016);
  - Pulse's recorded-routes model (§6).
  Off the list since Keel's run: Pulse's bake case, simview's recorded hash and the check's headless bands.
- `bun run typecheck`: clean. `bun tools/lint/comments.ts`: 0 hits.
