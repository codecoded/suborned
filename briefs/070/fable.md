# Brief 070 · Fable: the briefing page's words

All in `contracts/missions/handover/text.json`. The page's new lines are in `briefing.page` (schema: `text.schema.ts`; tests: `text.test.ts`). Latch builds the page later.

## 1. Inez's line (front door, `briefing.hints.front-door`)
- old: "Front door: the guard takes his coffee at Inez's café, and she is close to him."
- new: "Front door: Inez and the door guard are close."

It still says Inez and the guard are close. It's now one sentence with no unclear "she" or "him". The coffee stop is still in `targets.guard.about`, and the player can learn it by watching.

## 2. The DRAFT lines (now data in `briefing.page`)
| element | old (hard-coded in build.py) | new (field) |
|---|---|---|
| Front door, second sentence | "Turn her, and her call takes him off the door for a few minutes. A camera watches the door too." | "Turn her to call him off the door, and loop the camera that watches it." (`ways.front-door.line`) |
| Front door, plate | "Before 5:50" | "Before 5:50" (`ways.front-door.when`) |
| Pickup, second sentence | "Turn him before 5:40 and he drives Vale round to the back, and waits." | "Turn him before 5:40, and he brings Vale round to the service bay and waits." (`ways.pickup.line`; "service bay" matches pin 4) |
| Pickup, plate | "From 6:10" | "From 6:10" (`ways.pickup.when`) |
| Vale's second fact | "Comes out at 5:50 and leaves by car." | unchanged (`target`) |
| Ending (under the getaway car) | `briefing.win` "Vale in the getaway car when it leaves." | "Both ways end here: Vale in the getaway car as it leaves." (`ending`; `briefing.win` itself is unchanged) |
| Scout | "Listens to Inez and the driver. Can send the rivals off." | "Listens to Inez and the driver. Can send the rivals the wrong way." |
| Fixer | "Turns Inez or the driver, with cash or help." | "Turns Inez or the driver, and calls Inez when it's time." |
| Tech | "Loops the door camera. Moves the yard patrol." | "Loops the door camera. Moves the patrol and drives our car round." |
| Enforcer | "Goes in for Vale. Only he can carry him." | "Goes in for Vale, or takes him at the car. Only he can carry him." |
| Clock 0:00 | "You start by the getaway car" | retired: the clock starts at 2:35 (item 3) |

The cell lines come from the reference routes: route A is the Scout watching and listening to Inez, the Fixer's Help and then Call, the Tech's loop and the Enforcer's Enter. Route B is the Scout listening to the driver and giving the rivals a false tip, the Fixer's Pay, the Tech rerouting the patrol and driving the car round, and the Enforcer's Take.

The page shows each way as two sentences: the hint after "Name: ", then `ways.<id>.line`. The test checks that each way keeps to two.

## 3. The clock: five stations (`briefing.page.clock` = 2:35, 5:30, 5:40, 5:50, 6:10)
| at | old label | new label |
|---|---|---|
| 2:35 | A rival car turns in | unchanged |
| 5:30 | Handover chime | unchanged |
| 5:40 | Last chance to turn the driver | unchanged |
| 5:50 | Vale leaves the Office | unchanged |
| 6:10 | Vale reaches his car. Unless turned, his driver drives off | Vale at his car. Unless turned, his driver takes him away |

`briefing.timeline` still holds every mark in mission.json (2:46, 3:10 and 8:30 included), because the status pill uses them. The page shows only the marks listed in `page.clock`.

## 4. True whether or not the driver is turned
- `briefing.clock.line`: "Handover chime at 5:30. At 8:30 Vale's driver takes him away." → "Handover chime at 5:30. Vale's driver takes him away at 6:10, or 8:30 if turned."
- `briefing.timeline` 8:30 label: "Close: the driver drives Vale away" → "Close: a turned driver gives up and drives Vale away"
- `briefing.card[4]` had the same problem: "We have $100,000, four people and until 8:30." → "We have $100,000, four people, and until 6:10, or 8:30 if we turn his driver."
- The game reads `clock.line` through PlayText.ClockLine, so it picks up the new wording without a code change.

## 5. Day / Night
- `less.briefing.day` ("Day") and `less.briefing.night` ("Night") are removed from text.json and the schema. The `less.briefing.rule` and `briefing.page.rule` both say so. Briefing.cs falls back to its own "Day" / "Night" until Latch builds the page.

## Open (not mine)
- `briefing.tracker` still says "closes 8:30". The test pins it to the close, and that is the run's horizon. Unturned, though, the run is lost at 6:13. Aster: keep it, or let me drop the close from the tracker?
- `brief.json clock` (Waymark) still lights 5:30 and ends at 8:30. The page doesn't read it.
- PlayText.cs:501 (Latch) has a fallback ClockLine ending "the district closes at 8:30". It only shows when text.json is missing.
- `less.briefing.job` (Eyes, People, Machines, Force) is the old on-street cell strip. The page uses `briefing.page.cell` instead, so it can be retired once Latch's page lands.

## 6. Section heads (Latch on 0.2.130: the page showed none) and two retired labels
- New `briefing.page.heads` (all in text.json, schema `text.schema.ts`, test `text.test.ts`): `ways` is keyed by how many ways the mission has ("1" One way in · "2" Two ways in · "3" Three ways in · "4" Four ways in), so the page picks `heads.ways[brief.json ways.length]` and the count comes from the data. `where` "Where" (over the plan), `clock` "The clock" (over the stations), `heat` "Heat" (beside the Heat word, as on the mock; Glyph and Latch decide whether it shows next to the eye). The Target head stays where Briefing.cs already reads it: `less.briefing.target` "Target".
- Retired: `less.briefing.fact` ("Alive") and `less.briefing.office` ("Records Office"). Nothing reads them: Briefing.cs takes only `target`, `begin` and `ways.*`, and the plan names its buildings from the stills data. The hud-budget fixture's "Records Office" is a recorded 0.2.122 manifest, not a reader.
- Drift declared (a536dc833fa6, covers no rule). `bun test contracts/missions tools/dialogue`: 213 pass, 0 fail; `bun run typecheck` clean.
- Latch: PlayText's BriefWords flattens only `less.briefing`, so it won't see the heads. Load them with the rest of `briefing.page` in HostBriefing BriefScreen.Load.

## 7. Glyph's word nits on 0.2.130 (N7, N11, N12)
- N7: `briefing.page.ways.front-door.line` "Turn her to call him off the door, and loop his camera." (no stranded "it.").
- N11: new optional `briefing.page.captions`, keyed by the pin's place (stills.json `pins[].place`, less any "d:"): `service-bay` "Vale's car at the service bay". `targets.car-official` stays "Vale's car" (play uses it; the car also stops at the drive's mouth). Latch: in HostBriefing, take `captions[place]` before `Cap1(targets[key])`.
- N12: `briefing.timeline` 6:10 label "Vale at his car; unless turned, the driver leaves" (one fragment; still matches tools/sim/site.ts `/Vale at his car/`; `short` unchanged).
- Drift declared (d96ccff4efc7, covers no rule). `bun test contracts/missions/handover/`: 165 pass, 1 fail, which is not these words: "every engine log code is spoken" now sees `entry.seen`, `entry.stopped` and `entry.reported` from the uncommitted GameEntrySeen.cs, and the owner of that engine work has to word or silence them. `briefstills` + `tools/levels/dialogue`: 15 pass. `bun run typecheck` clean.
