# Brief 022: the living café, a waitress who works her tables and a couple who stand together

Owner: Pulse (data and reference), then Keel (engine), Waymark (twins of the recorded routes), Latch (draw it). Asked by Sky (2026-10-18), after playing the Handover with real figures: "have the waitress also walk around the cafe tables, and when guard comes, they stand together". Pulse, 2026-10-18. No C#, no Unity, no `sim/data` edit, no model call, no spend.

Systems, not a script: nothing below names Inez. A rule finds the people it applies to from their job, their stop and their ties. Nobody is told one right way to turn her.

## 1. Two behaviours

**Rounds** (`rounds` in `contracts/people/routines.rules.json`). A server who stands a door with a terrace of tables beside it works the tables. Between excursions she stands her post (24 to 36 s). She then goes to the table whose party has waited longest: a new party is greeted 10 s after it sits, a seated table is visited again every 75 to 105 s, the most overdue first, then the nearer. She never goes to an empty table (occupancy is the drawn walkers' seat stops; the four cast roles are not counted, because the engine plays them by role). She stands in the gap between two chairs, 130 cm from the table, facing it, for 12 to 18 s, and walks back. Walking pace is the cell's, 1.4 m/s. No excursion starts before 0:30, so the opening is Inez at her door. Seeded by id; baked at build time.

**Partners** (`partners`). Two people tied as lovers who meet stop, face each other and stand 120 cm apart, instead of passing.
- Summoned (the capstone): he walks the route he always walked and stops 120 cm short of her post, facing her; she is at her post facing him. He stays 60 s (rulebook `lover-stay-s`), they hold 2 s, he goes. 120 cm is inside `together-m` (4 m), so a Watch that ends with them there still teaches the affair.
- Chance (the generic tie behaviour): two walkers, both out on the street, neither on a post, who come within 8 m on a clear line: each makes an excursion (walk to a place beside the other, stand facing 20 s, walk back) and the routine clock stops for it. Once a mission per pair. The Harbour cast has two lover pairs and no chance meeting in the mission window (the guard is met at his door only by the summons), so this is baked as `meetings: []` and tested on a synthetic pair.

One primitive under both and under the guard's visit already in the engine: an excursion (leave a point, walk out, stand facing, walk back) with the clock stopped for it.

## 2. What is data now, what Keel builds

Data (done, nothing the engine reads is touched): `contracts/people/routines.rules.json` (every number cited, `proposed` with a reason), `tools/routines/{rules,walk,rounds}.ts` and `rounds.test.ts`, `bun run rounds` (`--check`), and the baked candidate **`briefs/022/handover.rounds.json`**. It is not in `sim/data/` on purpose: the mission checker copies every `sim/data/*.json` into its pinned inputs. Keel moves it (`bun run rounds -- --out sim/data/handover.rounds.json`) when he reads it. The code lives outside `tools/people` and `contracts/people/*.ts` because those files are in `peopleHash()`; `rounds.test.ts` holds that.

The file holds, for each server: post, facing, and on a round clock (whole seconds from mission 0) the excursions `{table, at, face, start, out, serve, back, path}`; `meetings`; the guard's stop point (`guard.stopAt` (-910,-2897), 6455 cm walk); and `vectors`: reference poses (free, held, called, ended) from `poseAt` in `rounds.ts`, which Keel must match to 30 cm and by phase.

Keel builds, behind a new `Start.Rounds` (`"rounds": true` in a script's start; default false so every recorded script, scenario and hash replays as now; on in the play host):
1. Read the file as an optional input like the provisional and takedown files (hash only when present). `PersonPos(Inez)` and a new `PersonPose` (pos, facing, phase: at post, to table, serving, from table, held, to post, together) come from the round, as `GuardPose` does for the guard. Every reader of her position follows from that one change.
2. The round clock stops while she is held. Held = an order taken on her with a verb in `holds.verbs` (listen, ask, pay, help, pressure), from the tick it is taken to the tick it lands plus `lingerS` (3 s); she stands where she is, turned to the operative. An order taken inside the linger extends the same hold (route A re-issues at the landing tick). Watch holds nothing.
3. Ends: Alert 2, any shot, a body found, the final fight starting. She walks back along her own path (at most one leg, 15 s here) and stands her post for the rest of the mission.
4. Called: the summons' target is her post, not her pose (so `CallQuery`, `summon-too-far` and the guard's `walkT` are unchanged); the guard's route stops `closeCm` short and he faces her; she breaks off, walks back, waits, `together` from his arrival to his turn plus 2 s, and the clock resumes at the start of the next rest.
5. For the HUD: `PoseAhead(p, ticks)` and `RoundNext(p)` (ticks until she is next at her post, still), so a Watch (which teaches the round) can show "back in 12 s" and Preview's walk aims at where she will be.
6. Hash the round state only when Rounds is on. `Path.Heading` should skip zero-length segments (a Crowd leg repeats its end points; at mm 0 it answers 0,0). Chance meetings later: a freeze window per walker from `meetings`.

## 3. Consequences, per reader of her position

All figures from the baked data for the Handover cast, mission 0:00 to 8:30: 8 excursions (tables 3, 0, 2, 3, 2, 3, 5, 2 at 0:34, 1:38, 2:34, 3:26, 4:21, 5:21, 6:23, 7:40). At her post 53.1 % of the time, walking 23.9 %, serving 22.9 %. Longest away 46 s (the far flank table, 15 s each way); a front table takes her away 22 to 34 s, a flank table 36 to 48 s.

| Reader | Before | Now |
|---|---|---|
| Listen (3 m) from `beside-inez` | always | 62.5 % of the time (at her post, and the first steps of a walk); she must be there when the order is given |
| Ask, Pay, Help, Pressure (4 m) from the Courtyard | always | 64.9 % (at her post, and the first or last steps of a walk). Her spots at tables 1 and 2 are 4.3 and 4.5 m from the Courtyard, half a metre out of reach: the Fixer steps closer. A refusal reads `too-far`, and the card shows the wait |
| Watch (6 m) from the Courtyard | always | 77.8 % (her post, and tables 1 and 2); reach is checked when the order is given, so she may walk off while it runs |
| Held once an order is taken | n/a | she cannot walk out from under a Listen or a Help: reach holds for the whole order |
| "Seen by" on Pressure (not private), customers within 13 m, straight line | 4 to 9 at her post | 2 to 9 at the front tables; **0 to 1 at the two far flank tables (5 and 7), 18 m from the door**: a quiet corner, found by Watching the round. The Fixer must be there at 4 m when she is |
| Her witnessing | from the door | from the terrace; runs from the car cross her view as they cross the customers'. Leads only, no Trail |
| Her 10 s warning (safe fight) | at her post | unchanged: rounds end when the final fight starts, and the warning reads `Inez.Turned` and the helped memory, not her position |
| The call to the guard | walk to her post, 46.1 s (64.55 m) | the same route, stopping 1.2 m short: 45.2 s, the unguarded window 2:32.2 becomes 2:30.5 (1.7 s). She is back at the post in at most 15 s, he needs 45 s. Even to the farthest spot it could not exceed 60.8 s, inside `summon-walk-max-s` 90 |
| 5:30 handover, check-ins 1:00, 2:30, 4:00, 5:30 | guard's, at his door | unchanged: the guard's timeline is the guard's; only the café end of his walk moves by 1.2 m |
| Suspicion (a lover who is not where he should be) | she is never within `meet-m` (6 m) of his table | his off-shift stop is table 3 seat 0 from 7:30; table 3's spot is 1.1 m from it and table 2's 5.1 m. With a guard down and unfound after 7:30, a visit by her to either raises the missing-person alarm 20 s later (Alert 1). Her visits in 7:30 to 8:30: table 2 at 7:40. No recorded route is still on the street then. The engine does not play his seat (cast), so the generator leaves him out of occupancy |
| Fights, cover, response, scatter | at her post | unchanged by the end rule |

**Per route** (static read of the recorded scripts against the reference, `rounds.test.ts`; not an engine run):
- Route A (Help, Pressure, and their board variants) and `fail-entry-seen-{camera,guard,patrol}`: the only 7 scripts that touch her. Every Inez order is taken at its recorded tick. Route A: Watch 5.4 s and Listen 27.5 s come before her first walk (0:34); the chain Listen, Help, Help, call is one hold from 27.5 s to the call, so she stands at her post the whole route. A's outcome, cash, Trail and Alert cannot move (4:31.2, $20,000, 5 T, A1). The fail-entry Listen is at 81.3 s, when she is at her post (65 to 98 s) for this cast's seeds; another seed could cost it a wait of up to 48 s, and Waymark would need its twin to wait. The negative control in the test: a Help given while she serves table 3 is refused and waits 17 s.
- Route B, B-block, G, G-fast, G-kill, loud, careless-*, other fail-*, withdraw: no Inez order. Rounds end at the first shot, Alert 2, a found body or the fight, so every loud or fighting part is played with her at her post as recorded. The quiet part adds only her walk and her sight.
- Quiet still beats loud: the recorded numbers (A 4:31.2, 5 T, A1; loud 4:23.5, 13 T, A2) cannot move, because the rounds add no act, no Trail and no Alert; they add timing the player can read.
- Engine arm: with `Start.Rounds` off every recorded script and hash is as now (`route-a.json` replays to Success 4:31.2 on today's tree). Waymark makes twins of the 7 with rounds on; their expected result is the same outcome with new hashes.

## 4. Done-check

- `bun test tools/routines`: 19 pass (rules parse and copy the rulebook; bytes deterministic; service points free and clear of chairs; walk and seconds are the grid's; she never visits an empty table; at her post about half the time; held, chain, called, ended; the guard's stop; the 7 scripts; partners on a synthetic pair).
- `bun run rounds -- --check`: the committed candidate is what the inputs make. `bun run typecheck`: clean. `bun test tools/people tools/levels/people contracts/people tools/sim tools/routines`: 284 pass, 1 skip.
- Not done: the engine reading it (Keel); a play with rounds on (Suture); the figure facing and clips (Latch).

## 5. Next owners

1. **Keel**: §2 above; add scenarios that read `vectors`, a Listen refused `too-far` while she is serving, the chain hold, the call from mid-walk, the end at the first shot; fold the numbers into `rulebook.json` (Motive confirms them). Gate: rounds off is bit-identical.
2. **Waymark**: twins of the 7 with rounds on; re-measure `mission.json` windows (`summon` window 2:32 becomes 2:30); consider moving `beside-inez` 1 m east or west (the guard now stops 0.9 m from it).
3. **Latch**: draw her from `PersonPose`; walk clip out and back, a talk clip for serving, idle while held; the couple facing each other, 1.2 m apart; keep her figure off the chairs (service points are 60 cm clear).
4. **Suture**: play it; check the flank corner reads and the Listen window is learnable from the Watch.
5. **Aster / Sky**: GDD lines to add in a new version: §04 routine "a server works her tables: rests at the door 24 to 36 s, goes to the longest-waiting seated table for 12 to 18 s; stands still while spoken to; walks home at the first shot"; §07 cast "Inez works the café's tables; the guard who comes at her call stops 1.2 m from her and they stand together". And Sky: the numbers above are starting values [S].

## 6. Risks

- The static read depends on seeds: regenerating the people moves her first walk and `bun test tools/routines` says so at once.
- She is out of the Courtyard's reach about a third of the time. If play feels like chasing, shorten `rest`/`revisit` or lengthen `settleS` in the rules file and rebake; nothing else moves.
- A far table is a long trip: the east and west flanks take 42 to 48 s. They give the quiet corner and the sense of a café; `leashM` 25 keeps them in, 15 would drop them.

## 7. Addendum 2026-10-18 (Pulse): the guard stands at her side; route A's wait

Two play notes from Waymark's c020 (engine 0.8.0). Data and a ready patch only; no engine file, no `sim/data` file was touched.

**7.1 The summoned partner's stop** (`partners.summoned.side`, `partners.clearCm` in `contracts/people/routines.rules.json`; rulebook `partners-clear-cm` 100 [S] to add). 0.8.0 stopped him 120 cm short of her post on his own route, straight ahead of her: 0.9 m from the Scout at `beside-inez` (2.1 m from her post). The rule now:
- Stops are baked, not computed: `guard.stops` in the rounds file lists every point on the ring of `closeCm` (120) round her post, every 15 degrees off her facing on both hands, that is free ground on the walk grid and walkable from the guard's post; ordered nearest 90 degrees first (a side, then the front quarter; on a tie the front one), her left before her right (a south-facing post: east is her left). `guard.stopAt` and `face` are stops[0]; `ringCm` replaces `shortCm`.
- Her post is a door alcove 2 m wide, so at 120 cm only 0, 15, 30 and 45 degrees either hand are free: the list is 45L (-945,-2815), 45R (-945,-2985), 30L, 30R, 15L, 15R, 0 (-910,-2900, the old stop). Walks to them 6372, 6412, 6352, 6393, 6339, 6360, 6335 cm (the post: 6455).
- The pick, at the tick of the call (`pickStop` in `tools/routines/rounds.ts` is the reference): the first stop with nobody within `clearCm` (100 cm) of it, over the operatives not down and everyone on the street, not her and not him; if all are taken, the stop whose nearest person is farthest, the earlier in the list on a tie. Fixed order, so the same people give the same stop. This is a superset of "away from anyone within 1.5 m of her": anyone near her stands near a stop.
- He faces her (`cafeFace = post - stop`), she faces him (`together` faces `CallStop - post`, already so). Result on the Handover: nobody within a metre of the first stop, so he stands at 45L, 151 cm from the Scout (was 90), 120 cm from her; walk 45.5 s (was 45.25), the unguarded window 2:30.6 becomes about 2:30.9; the summons' refusals and limit still read her post. `beside-inez` does not move.
- With the Scout on 45L at the call he takes 45R (checked). A stop is never more than `closeCm` farther to walk to than her post, so `summon-walk-max-s` (90) can be passed only by a post that is already within 0.9 s of it.

**Exactly what changes for Keel** (`briefs/022/partner-stop.patch`, applies to the tree as of today with `git apply -p1`; also tried on a scratch copy: 787/787 engine tests, N130-N136 pass):
1. `contracts/rules/rulebook.json`: value `partners-clear-cm` 100; `rounds.clear: "v:partners-clear-cm"` (Motive confirms).
2. `sim/Suborned.Sim/Rules.cs`: `PartnersClearCm`; `rd.Only` gains `clear`.
3. `sim/Suborned.Sim/GameRounds.cs` `RoundsData.Parse`: `rules` gains `clearCm` (must equal the rulebook's); `guard` is `from, post, walkCm, ringCm, stopAt, face, stops` (ringCm equals closeCm, replacing the `shortCm` check; each stop `at, face, deg, walkCm`; stopAt equals stops[0]); new fields `GuardStops`, `GuardStopWalkCm`. New `Pt? PartnerStop(Person server, Person lover)`: the pick above over `Ops` (not Down) and `Walkers` (`CrowdPos`), null when the file has no stop.
4. `sim/Suborned.Sim/GameOrders.cs` `Summon`, in the `if (sv != null)` block: instead of `guardOut.Cut(...)`, `guardOut = new Path(Grid.Route(NearestFree(from) ?? from, stop))` for `var stop = PartnerStop(p, lover)`, the old cut when it is null; `walkT`, `cafeFace`, `RoundCall` as they were. `SummonRefusal` and its walk are untouched.
5. `sim/data/handover.rounds.json` = `briefs/022/handover.rounds.json` (`bun run rounds -- --out sim/data/handover.rounds.json`; the test `rounds.test.ts` holds the live copy to the same bytes once it carries `guard.stops`). Do this with the patch: the old parser rejects the new `guard` and `rules` keys.
6. `sim/Suborned.Sim.Tests/Rounds.cs` (the vector's lover walk is `GuardStopWalkCm`, not `GuardWalkCm - CloseCm`) and `ScenariosRounds.cs`: N132's "1.2 m short" check becomes "his walk is to stops[0]" (it fails otherwise, 45.6 s against the cut's 45.3 s); new N134 (nobody near: stops[0], 151 cm from the Scout), N135 (Scout on stops[0]: he takes stops[1]), N136 (all taken: the farthest).

**Hashes** (behaviour only, `hashes --start-text <old inputs hash>` against the tree, rounds on both): move in 20 scenarios, all with a call to Inez (S03 S04 S05 S06 S10 S19 N11 N18 N32 N36 N39 N39c N39d N40 N95 N111 N113 N114 N120 N132); every other scenario is bit-identical. All 36 recorded routes keep outcome, end tick, cash, Trail, Alert and every log line but six: route-a, route-a-board, route-a-pressure, route-a-pressure-board, fail-entry-seen-camera and fail-entry-seen-patrol, where only `guard.at-cafe` is 0.3 s later (3:35.1, was 3:34.8). The inputs hash moves for all (the rulebook and the rounds file are inputs): Waymark re-records the whole set as c021, as c020 did; the Unity match (Latch) moves for the six.

**7.2 Route A's wait.** Measured on the tree (engine 0.8.0, rounds on) with the real recorded script, `route-a.json`:
- She is out of the Listen's reach (3 m from `beside-inez`) 36.7-62.4 s (25.7 s), 101.0-124.1 (23.1), 157.5-174.6 (17.1), then 24.7, 19.1, 22.7, and 385.6-426.5 (40.9 s, the far flank); the Help's (4 m from the Courtyard): 24.7, 22.3, 14.5, 23.7, 16.5, 21.7, 40.1, 15.5. The longest refusal anywhere in 0:00 to 8:30 is 40.9 s (Listen) and 40.1 s (Help). Waymark's "up to 37 s" is the first Listen's shift, 27.5 to 62.5 s.
- Only the first Listen can be refused on the recorded plan: it is given at 27.5 s, before her first walk (0:34). Once taken she is held through Listen, Help, Help (each re-issued within the 3 s linger), and the call reads her post, never her pose. A Help given 13 to 37 s late, after the linger, can meet her out (25 s refusal), which costs the same kind of wait.
- Whole script shifted late from the Listen by D seconds (the Tech's orders too; every order that lands, lands): wins for every D at which the Listen is taken, up to D = 175 (Listen 3:22.5, call 5:44.5, the Enforcer in at 5:49.4, Vale leaves at 5:50 and D = 176 is the first to fail). Clean (Trail 5, Alert 1) for D up to 129 (call at 4:58.5); the calls landing about 5:17 to 5:27 (D 148 to 158) still win but loud-ish (Trail 18, Alert 2), the Enter overlapping the 5:30 chime surge (cause not traced; Keel or Suture to say).
- A player who waits, tried in the engine as a waiting player: the first Listen taken at the first moment she is back (62.5 s, +35 s); then every Help re-issued 13 s, 42 s and 60 s after it could first be (so each meets a refusal and waits it out): +61, +152 and +160 s on the call (call 3:50 to 5:29.5): all win (Trail 5, Alert 1 except the +152 case at Trail 18, Alert 2). Worst by the numbers, Listen, Help and Help each meeting the longest refusal (40.9 + 40.1 + 40.1 = 121 s): the call at 4:50.5, Vale secured at 5:33 (after the 5:30 chime, before the 5:50 exit), played clean at D = 121. So waiting for her can never make route A miss; the margin on the realistic path (one wait, 25.7 s at most) is about 1:50 between Vale secured and the 5:50 exit.
- The one hard dependency: the Tech's Reroute. Left at its recorded 1:14.5 (not retimed) the call can slip 92 s (D = 100 fails entry-seen-patrol): that is Waymark's 92 s. A player who waits retimes the Reroute (free, 45 s), as the checker's own late test does when it shifts the later orders.
- The checker's slack of 8 to 12 s for route A is a scripted order given late and refused, not a player waiting. The honest spare is the call's: 92 s with the Tech as recorded, 129 s clean with the Tech moved with it.
- Numbers: none to change in `handover.rounds.json` or the rules for route A. The one to watch in play is `lingerS` (3 s), the window to chain Help after Listen before she resumes her round: a miss costs one refusal wait of 25 s at most on A's timeline, and A has 92 to 129 s; left at 3. New: `clearCm` 100 [S].

Done-check: `bun test tools/routines` 25 pass; `bun run rounds -- --check` ok; `bun run typecheck` clean; `bun test tools/people tools/levels/people contracts/people tools/sim tools/routines` 290 pass, 1 skip.
