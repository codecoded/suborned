# Brief 023 · Motive on engine 0.8.2: three rule questions from Keel

Motive, 2026-10-19. Read against `contracts/rules/rulebook.json`, engine 0.8.2 (`sim/Suborned.Sim`, Debug build 05:44) and the scenarios Keel named (N95, S05). Evidence: `briefs/023/motive/probe/` (a read-only probe on the engine) and `briefs/023/motive/car-route.py` (the drive route rebuilt from `sim/data/handover.site.json` `drive`).

**Nothing is written into the rulebook yet.** Every ruling here needs a new key, and the engine rejects unknown keys at load: `Rules.cs` reads `car` with `Only("drive", "tag", "src", "proposed", "reason")` and `fight` with a fixed list, and `rulebook.test.ts` fails on a value that no rule reads. Keel should land the JSON below in the same change as the engine, so the rulebook and the engine never disagree and `verify` stays as it is.

## 1 · Fire after the final fight: the engine ends it (sure)

**Ruling.** When the final fight is over (`fight.over`: every hostile down, broken or gone), the engine ends every open fire order whose target is a final-fight hostile, and refuses any new fire order on one. A crew that broke is no longer a target, just as a stance already treats it (`Combatant()` excludes `break` and `gone`). Fire orders on anyone else are untouched: an officer still up, the patrol, a response unit that arrives later.

**Why.** "Fire until down" is a standing order the player gave during the fight. Without this ruling it outlives the fight without the player seeing it, and its next shot opens a fight of its own (`FightShot`: the final fight is judged, so the shot starts a new, non-final fight). That fight is judged `rivals`: the cell fired on the feared crew, and the crew is broken. So shooting a fleeing man in the back earns **+1 Hero a second time**, and once more each time `crowd.quiet` lapses while he is still in sight. That reads backwards and can be farmed. The shot is also untraced, because `IsFightHostile` holds after the fight too, so it costs nothing. The final fight is the consequence, not a new act (fight.tracedReason). Ending it at `fight.over` keeps that true.

**Consequences.**
- **Standing:** the final fight moves Standing once, when it ends (N95: +1 `seen-fight-rivals`; route A still ends at +4 with Inez's word). No straggler fight, so no second move.
- **Trail and Alert:** none, unchanged (`fight.traced` false).
- **Debrief:** unchanged.
- **During the fight:** a fire order on a rival who has broken but whose fight isn't over yet stays as it is. Its shots belong to the final fight and are judged with it.

**Keel must:**
- In `CellShooter`, end the fire order (log `fire.done`) when `FightOverTick != INF` and `IsFightHostile(g.Order)`.
- In `Fire`, refuse with a new code `fight-over` in the same case.
- Accept the new text in `fight`, then remove the `cease` call from `FightItOut` (`Scenarios.cs:164`). N95 must pass without it.
- Add a scenario: N95 with an explicit fire order on a breaking rival, holding `Fights.Count == 1` and one Standing move.

**Fable:** text for refusal code `fight-over` and for the `fire.done` line ("the fight is over").

**Aster:** GDD §06 orders should read "fire (until the target is down, or the final fight is over; …)" at the next version.

Text to add to `fight` (or append to `fight.rule`):

```json
"over": "When the final fight is over (every hostile down, broken or gone) every fire order on a final-fight hostile ends and a fire order on one is refused (fight-over): the crew that broke is no target, as a stance already treats it, so no shot after the final fight opens a fight of its own. The final fight moves Standing once when it ends (fight.public) and adds no Trail or Alert (traced). Fire orders on anyone else are untouched.",
"overReason": "Motive 2026-10-19 (brief 023, Keel's N95 on the west-face door): a standing fire order outliving the fight shot a fleeing rival, which fight.public judged a second 'rivals' fight, +1 Hero for a shot in the back, repeatable and untraced."
```

## 2 · The service drive's pace

**Ruling (values [S], proposed).** A car moves at its surface's pace, not stretched across a time slot:

| value | v | where | source |
|---|---|---|---|
| `car-road-cm-s` | 500 | carriageway cells | `harbour-district.life.json` traffic.byClass.access.speed 5 m/s (Apron Road is `access`); Pulse and Datum's city value, restated here with a test that the two are equal (as `rounds` does with its baked file) |
| `car-yard-cm-s` | 200 | drive and bay cells (dropped kerb, yard, kerb bay) | Motive: a car among people at a little over a walk (1.4 m/s); a car park's 5 mph is 2.2 m/s |

**How the drive runs:**
- The turned driver still leaves at `driverLast` (5:40).
- He drives the route at those paces, about 8.0 m of road and 24.7 m of bay and drive, so about **14 s**. He is in the service bay by about 5:54.
- He waits there for Vale, who still arrives at 6:10. A chauffeur is there before his passenger, and the player sees the car parked before Vale comes out.
- The **block move keeps `blockMoveS` 8 s [B]**: 5.4 m out of a kerb bay to the block spot is a manoeuvre, and it is about that slow in life. It also sets the call window.
- **The kerb-hugging is geometry, not pace** (Keel with Pulse; a recommendation, not ruled). Apron Road is 12 m wide, the plan's traffic runs right-hand, and life.json has a 3.3 m lane. A car on a carriageway should keep to the lane centre for its heading, and on a drive or in a bay keep to the middle (the drive here is 3 m wide). Today's route rides the edge cell, 0.5 m from the edge, from y −21 to the yard (`car-route.py`).

**Does any timing break?** I don't expect any outcome to change:
- Every Take at the service lane is at 6:10 or later (tick 3700 in every route-B script).
- Vale's walk and `valeAtCar` are unchanged, and the rivals stand at the service lane only after `valeAtCar`.

What does change:
- `CarPos` is hashed (`Hash.cs:77`), so every run with a turned driver changes its checkpoints from 5:40. Re-record route-b, route-b-board, route-b-block, route-b-block-board, careless-reporter-not-stopped, fail-rivals-reach-vale and fight-safe-rivals-reach-vale, plus the turned-driver scenarios.
- A takedown on the driver mid-drive (`carStoppedAt`) now has about a 14 s window, down from 30 s.
- The driver, who is a seeing person, is in the yard about 16 s earlier.

Keel confirms all of this by running the routes. If any outcome moves, send it back to me.

**Keel must:**
- Mark road and yard cells in the drive grid. `tools/sim/drive.ts` has both classes and writes only `.`/`#`, so it would write e.g. `r` and `y`.
- Time `carToService` as Σ length / pace from `driverLast`, then hold the car at the bay.
- Accept `car.pace` (below) in `Rules.cs` and the schema.

```json
"pace": { "road": "v:car-road-cm-s", "yard": "v:car-yard-cm-s" },
"paceRule": "A moving car drives each drive-grid cell at its surface's pace: `road` on a carriageway, `yard` on a drive or in a bay. The turned driver leaves at driverLast and waits parked in the service bay until Vale comes; the block move keeps the schedule's blockMoveS (a manoeuvre out of the bay, not a drive)."
```

## 3 · S05: run to the door, no value moves (sure)

**Ruling.** S05 should run the Enforcer to the door: `Send(Enf, "d:Office entrance", run: true)`. No value moves.

**Why.**
- Running costs nothing here. The rulebook's `cell.rule` says "running is free while a fight is on", and the shot's fight is open until 5:19, so the whole 4:49–5:12 run falls inside it. Even outside it, a run seen only adds Alert 1, and S05 is already at Alert 3.
- GDD §07 timings already says "walking legs shorten where the cell runs".
- Every loud route script already runs.
- He can't run the second leg (no one runs carrying Vale), so the 45 s from the service lane to the car stands. That cost comes from where the city put the door, and it is a fair one.
- A witnessed shooting is meant to be the tightest loud route. Inez's report brings the unit 12 s sooner and Alert 3, against the plain loud route's minute.

**Measured on 0.8.2** (probe `walk` / `run`):

| | at the door | out at the lane | at the car | result | seal |
|---|---|---|---|---|---|
| walk (as S05 is now) | 5:35.2 | 6:43.2 | — | fail sealed 7:24.0 | 7:24.0 |
| run to the door | 5:12.1 | 6:20.1 | 7:05.2 | **success 7:09.3** | 7:24.0, **14.7 s spare** |

**Other loud scripts** (probe `script`: the car leaves vs arrival + 145 s seal):
- loud, loud-fast, loud-left-*: 65–66 s spare
- loud-fight: 58.8 s
- route-g-kill: 95.5 s

The seal (145 s, S2) and Enter +30 s stay.

**Keel must:**
- Make the S05 move a run and update its comment.
- Optionally keep the walking version as a negative check ("walking at Alert 3 after a witnessed shooting: sealed"), which shows the seal clock biting.

## Next owner

Keel lands 1 and 2 in the engine together with the rulebook JSON above. The done-check is `dotnet test`/scenarios green, N95 passing without the helper's `cease`, the route records re-recorded for 2, and `bun test contracts/rules` plus `bun run verify` no worse than now. Fable writes the `fight-over` text. Aster carries the §06 wording into the next GDD version.
