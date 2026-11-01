# 085 · A discreet Listen

Motive, 2026-11-01. A report: nothing in `sim/`, `contracts/` or `text.json` changed. Measured on a scratch copy of HEAD (engine 0.38.0) in `out/scratch/motive/085l/`, seeds 1-500 at human pace through `bun run guard`. The probe and raw tables are in `briefs/085/motive/probe-listen/`.

## The rule

Clicking Listen on someone, the Scout picks his own spot and listens from there. **Listening itself is never noticed** (Sky: "Listening shouldn't get noticed, scout is a scout").

| Part | Value |
|---|---|
| Reach | **7 m** (`listen-reach-m`, 3 m today) |
| Never closer than | **3 m** when choosing a spot (new `listen-min-m`) |
| Time | still 30 s; sitting and rising happen inside it |
| Trail, Alert, witness | none, as today |

**Choosing the spot, for anyone on any mission:**
1. **The anchor:** someone who works rounds from a post is listened to from near the post. Anyone else, from where they stand.
2. **First choice:** the nearest chair to the Scout, 3-7 m from the anchor, free from now until the Listen would end, and out of every watcher's cone.
3. **Second choice:** the nearest free ground 3-7 m from the anchor, beside something solid (wall, planter, furniture), on a clear line, out of every cone.
4. **Nothing found:** walk in as today and stop at reach. This never fired in any plan I ran.
5. **Already placed:** a Scout standing 3-7 m off and beside something solid stays put.
6. **Facing:** his back or side to the person. A chair's own facing, or side on when leaning. View only.

If she is out of reach when he arrives, he waits there. If she is still gone after 30 s, he picks again. Once she is within 7 m, the Listen starts and she stops as today, but no longer turns to face him. Her scene is Milena and Inez talking, so stopping still reads.

## 1. Reach

**Why 7 m and not 5:** the chairs near her door are nearly all taken at Listen time. On seed 3, `cafe-table-1-seat-0` is taken until 2:30, `cafe-table-1-seat-1` until 1:30, `cafe-table-2-seat-0` until 2:25, `cafe-table-2-seat-2` until 6:31 and `cafe-table-2-seat-1` for the whole mission. `cafe-table-1-seat-2` is free, but chairs sit in blocked grid cells, and its nearest walkable point is 6.0 m from her post.

| Rule | `quiet-a` win | `thin-quiet` | Prepared Pressure | Helped, then pressured | Spot taken |
|---|---|---|---|---|---|
| Today (3 m) | 99.0 | 86.6 | 95.0 | 97.8 | stands beside her |
| 5 m | 98.6 | 86.2 | 94.2 | 97.4 | leans, sits about 7 % at most |
| 6 m | 98.8 | 86.6 | 94.6 | 97.4 | leans, sits about 7 % at most |
| **7 m** | **97.8** | **85.6** | **95.4** | **97.6** | **sits in 498-500 of 500** |

Differences under about 2 points are noise at 500 seeds. `quiet-b` (91.2) and `quiet-g` (74.2) don't move: the Scout leans by the driver, and route G has no Listen.

**The cost is time, not wins.** He often arrives while she's out on her first round, and waits.

| Plan, medians | Heard, today | Heard, 7 m | Call, today | Call, 7 m |
|---|---|---|---|---|
| `quiet-a` | 1:14 | 1:33 | 2:35 | 2:50 |
| `thin-quiet` | 1:22 | 1:33 | 2:02 | 2:14 |
| Prepared Pressure | 1:14 | 1:33 | 1:48 | 2:05 |

Order to heard goes from about 33 s to about 52 s (longest in prepared plans 64 s; 76 s in `thin-quiet` and `prepared-loud`).

**What else changes:**
- **What's heard:** nothing. A Listen teaches only its subject, and no other rule reads the reach.
- **Who notices:** see section 3. Reports on `thin-quiet` fall from 23 to 2 of 500, because spots avoid cones. Without the cone test, 8 m chairs drew reports in up to 308 of 500 runs, as the plans left the Scout in a cone afterwards. I didn't run 7 m without it.
- **Bands (200 seeds):** R-7 fails with the rule off and on (`quiet-b take`), so it isn't this rule. Bands missed go from 6 to 7. The new one is B-11 `quiet-a`: the middle half of its fights spans 12-19 s against 12-24 s today, under the 10 s floor. `thin-quiet` stays in its band (87 % to 85 %). I'd let B-11 report rather than tune for it.

The driver and the guard also work: 500 of 500 heard from a leaning spot, with no plan change (`probe-listen/listen-driver`, `listen-guard`).

## 2. The seat

**Can a walker already have it?** Yes, often, so the choice checks each walker's schedule for that chair across the whole Listen. A walker who is due later is simply not a clash. The claim lasts only for the act, so an idle Scout is drawn standing.

**Does sitting cost time?** No. The 30 s stays, and Forma's 2.3 s clips play inside it: `stand_to_sit` at the start, `sit_to_stand` over the last 2.3 s, as `HostSeats.cs` already does for walkers. While waiting he is drawn seated. A cancel cuts straight to walking.

**What the engine must know:**
- Each chair's name and its walkable point (from `handover.seats.json`, baked into the site). The chair point itself is blocked, so **the Scout's engine position is the walkable point, about 1 m from the chair.**
- Who holds a chair: already known, from `Walker.Place/From/To`.
- The chosen spot, and that a Listen walk-in is waiting.

**What stays view only, so replays hold:** the draw on the chair and the 1 m slide to it, the facing, the sit clips.

**New rule beyond the spot:** waiting for her holds the attention clock like the Listen itself.

**Art:** ships with `sit` and `idle`. No clip is required. `stand_to_sit`, `sit` and `sit_to_stand` exist, but Latch must confirm the Scout's figure has them. There's no lean clip, so leaning is `idle` beside the wall, side on. A lean clip would be nicer, not necessary (Forma).

**Vantage:** no new points are required, since spots come from the chairs and the grid. `beside-inez` is no longer needed, since a Watch reaches 30 m and the Listen walks itself. Plans still using it work, with an extra short walk. Waymark can drop it.

**Fable, one note:** "Scout" speaks last in `listen-inez`, and that needs to read as a report, not as him speaking to her.

## 3. "Listening shouldn't get noticed"

**Today a Listen already costs nothing and draws nothing:**
- `scout.listen` in the rulebook: `private: true`, price is time only (`v:listen-s`), no witnesses, no `also`, and its one outcome only teaches.
- Attention: an act in sight holds the clock (`AttnInAct`, `GameAttention.cs:64`), §05 "each second".
- Played: on seed 3 the ledger had no entry at the Listen; the first Trail came with the turn.

**The "+4" isn't the Listen's and isn't a Trail total.** I haven't seen that shot. In the code, the feed's small "+N" is how many more lines a conversation card holds (`HudToolkit.cs:916`). A Trail "+N" appears only on a price above 0 and with its icon (`HudModel.Less.cs:94-96`), and a Listen's Trail is 0. Glyph can confirm against the shot.

**The one gap the new rule would open:** waiting for her at the spot would count as idling, +1 a second in a cone. The rule holds it instead.

**Still seen, unchanged:**
- Idle or Watching in a cone before or after the Listen: +1 a second. A Watch still counts.
- Running to the spot would be seen as any run, but the walk in is a walk.
- A drawn weapon, a carry, a takedown, gunfire: their own rows.
- A clock already at noticed or wary keeps its mark, and a report already in flight still lands. The Listen only adds nothing.

**One thing to know:** the cone test reads the true cones, even ones the cell hasn't learned. That hands the player nothing to read, and it fits "a scout is a scout", but it is the Scout knowing more than the player.

## 4. What re-records

Against 49 saved scripts, the 385 scenarios and the sim tests, with the rule on:

| What | Count | Detail |
|---|---|---|
| Hashes | 49 of 49 | any rulebook edit |
| Outcome changes | 6 | `route-a`, `route-a-board`, `route-a-disable`, `route-a-pressure`, `route-a-pressure-board` go from win to `entry-seen-guard`; `careless-loop-runs-out` changes fail code |
| Why | | their fixed ticks now run ahead of a later Listen. Re-compose them from their plans |
| Scenarios to rewrite | 4 | S22, N130, N131, N178 assert the 3 m walk in and its card estimate |
| Fixture to re-record | 1 | DP1, `drive-session` |
| Sim tests | 5 of 2,765 fail | all `ScenarioTests.Passes`; 0 with the rule off. This run was before the last spot change; the scenario check after it fails the same 5 |

Not run: the Unity proofs. `ProofDone` and `ProofHold` watch Listen's walk in, so Latch should expect to look at them. The card's walk time must read the spot's walk, which N130 and N178 show it doesn't in the probe. The adaptive plans need no change.

## Sky's call

**Is 7 m all right, or should the Scout sit only when a closer chair is free?** 7 m is what gets him sitting, because the closer chairs are taken: he sits every time, at about +19 s to hear her. At 5-6 m he mostly leans by a wall, about as fast as today. Both fit his note; the rule and the numbers are the same otherwise. I recommend 7 m.

## What I'm unsure of

- **The look:** nobody has seen it. 7 m may look too far to overhear, and the 1 m slide to the chair may show.
- **Which chair:** the probe logs the kind of spot and the distance, not the chair's name. `cafe-table-1-seat-2` is my reading from seed 3 and the geometry.
- **The wait:** a player may read a seated, waiting Scout as stuck. The action label should say he's waiting for her (Glyph).
- **Spot search cost:** the probe scans chairs and a grid ring when the spot is picked, up to one route a pick. It isn't timed.
