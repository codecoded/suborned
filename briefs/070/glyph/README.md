# Brief 070 · the briefing as a page (Glyph, mock for Sky)

Sky, 2026-10-26: "really hate our mission briefing overlay - very confusing. Why not have it like a proper page with just screengrabs / closeups of what matters".

This is a picture only. There's no code in the game, no Unity work, no GDD edit and no paid calls. `build.py` draws the mock from the mission's data files, the kit and existing captures (`python3 briefs/070/glyph/build.py`).

| file | what |
|---|---|
| `briefing-page-mock.png` | the page at 1920 × 1080 |
| `briefing-page-mock@2x.png` | the same at 3840 × 2160 |
| `check-2x.png` | the 2x check: the 2x page in four quadrants at full size |
| `before-after.png` | today's overlay (out/batch/shots/briefing-day.png, build 0.2.128) beside the page |

## What changed and why

| today (cards over the street) | the page |
|---|---|
| The ways in are icon chains (face → shield → door) with no words. | Each way is a row: a name, when it works ("Before 5:50", "From 6:10"), two plain sentences, and two close-up stills of the people and places, joined by arrows. |
| Dashed leaders run to places that aren't on screen. | There are no leaders. Every still has a numbered pin (1–4, and a car for the getaway car), and the small **Where** plan carries the same pins. |
| The street behind shows water towers and roofs. | The street is gone. Each still is a close-up of what matters: Inez, the door guard, the driver at Vale's car, the service bay and the getaway car. |
| The clock is icons, and nothing says what happens at 5:30 or 8:30. | **The clock** is a line of stations, each with its time and the mission's own label: 0:00 start, 2:35 rival car, 5:30 chime, 5:40 last chance to turn the driver, 5:50 Vale leaves, 6:10 Vale at his car, 8:30 close. |
| The cell are labelled EYES, PEOPLE… | Each face has one line about this mission ("Loops the door camera. Moves the yard patrol."). |
| A floating target card | The target is in the left column: Vale's file photo, his name and one fact (plus one DRAFT line saying when he comes out). |

The purse, Heat as one word, Day / Night and Begin stay as built, in a deep-teal band at the foot of the page. The page uses cream stone paper (the style contract's `cream_stone` #e4e0d3), a seafoam band at the top, brass only as 1 px hairlines, kit sprites (`plate_dark`, `disc_dark_24`, the chrome cameo ring), Jost only, and every colour from the kit or the contract.

One correction to main's layout: **5:30 isn't when Vale comes out.** 5:30 is the handover chime. Vale comes out at 5:50 and is at his car at 6:10. Unturned, his driver leaves at 6:13. Turned, the driver waits until 8:30. The page follows `mission.json timeline`.

### Lines marked DRAFT (for Fable)
- Front door: "Turn her, and her call takes him off the door for a few minutes. A camera watches the door too."
- Pickup: "Turn him before 5:40 and he drives Vale round to the back, and waits."
- Vale's second fact: "Comes out at 5:50 and leaves by car."
- Clock 0:00: "You start by the getaway car."
- The four cell lines.

Every other word is already in the data: `brief.json` title, place, time, hook and objective; `text.json` `briefing.hints`, `briefing.timeline[].label`, `briefing.win` and `briefing.heat`.

## Capture points (build time, never by hand, never AI)

Each still is taken by the shots tool during a **replay of the mission's reference route script** (`contracts/missions/handover/routes/route-a-board.json`, the route-B script), with the HUD off. The replay is deterministic, so the same mission always gives the same stills, and a generated mission gets its own from its own data. These are the rules the tool would follow:
- **Place:** the pin's place id.
- **Time:** the tick when the role is at that place, from the role's `shift` / `at` or the timeline entry.
- **Yaw:** the quarter step (30/120/210/300) that best faces the door's outward face in `civic-block.json`.
- **Pitch:** play pitch (38°), unless the row says otherwise.

| slot | capture Latch would take | place (data) | tick | camera | stand-in used in the mock |
|---|---|---|---|---|---|
| 1 Inez | `brief-contact-waitress` | `roles[waitress].at` = d:Café door | 0:30 (her shift from 0:00) | the conversation camera's profile rule (§10: 3.6 m off, 1.9 m high, 8° down, 35° field), one person, framed head to waist | `briefs/041/build/0314/proof-1080/r067-convo-inez-1` |
| 2 The door guard | `brief-guard` | `roles[guard].at` = d:Office entrance | 1:00 (his first check-in, on the landing) | yaw 210, zoom 8, occlusion cut off for this shot | `levels-guard-landing` (**stand-in**: the cut blob and in-painted marks show) |
| 3 The driver at Vale's car | `brief-pickup` | `roles[driver].at` = d:Meet driver | 6:10 (timeline: Vale at his car) | yaw facing the official entrance, zoom 9 | `r067-official-car` |
| 4 The service bay | `brief-service-bay` | `points[service-bay]` | 6:26 in the route-B replay (timeline 6:25 + 1 s) | yaw facing the east face, zoom 9 | `r059-625-parked` (**stand-in**: in-painted wait marker) |
| Getaway car | `brief-getaway` | d:Getaway bay | 0:00 | yaw 30 (opening), zoom 9, deck overhang cut off | `r29-squarebay-play` (**stand-in**: the deck hides half the car) |
| Target photo | none | `brief.target.role` → `portraits/card_councillor` | – | – | the kit portrait |
| Where (plan) | none: drawn | `civic-block.json` buildings, bays, objects + the pinned places | – | – | drawn |

If Sky would rather have a photo than the plan for **Where**, the capture is `brief-wide`: centred between d:Café door and d:Meet driver (about 90 m apart, so it can't be framed at play zoom), pitch 60°, zoom out until both are in frame, tick 0:00. Its pins would be projected from the same place ids.

Night: if Night is picked, the same captures are taken with the night look (two sets per mission).

## The data each element reads

| element | fields |
|---|---|
| title / hook / objective | `brief.json` title, `variants[pick].hook`, `variants[pick].dossier.objective` |
| place · time (top right) | `brief.json` place, time |
| target | `brief.target.role` → `roles[]` (name part "Vale"), `portraits/card_<role>`; fact = `variants[pick].dossier.target` after the name |
| ways | `brief.ways[]` (id, contact, steps) → `text.json less.briefing.ways.<id>` (title), `briefing.hints.<id>` (first sentence); timing from `mission.json timeline` / `windows[]` (front door: before Vale's exit; pickup: from Vale at his car) |
| stills + pins | one per way step's place (contact role `at`, the window's role, `points[]`); pins numbered in way order |
| ending | `d:Getaway bay` + `text.json briefing.win` |
| where | `civic-block.json` + the same place ids |
| clock | `mission.json timeline[].at` + `text.json briefing.timeline[].label` (plus 0:00) |
| cell lines | the agents' orders in the reference route scripts (who acts on which role and device), written as one line by Fable's template |
| purse / heat | `mission.json start` / purse ($100,000), `start.heat` → `text.json briefing.heat.<band>` |

## GDD line 306: a proposed replacement (for Aster; GDD not edited)

> - briefing [S] (a page, modal until Begin): a full-screen page in the game's paper look (cream stone, a seafoam band, brass hairlines, kit plates). On it: the title, the hook and the objective in words · the target's file card (photograph, name, one fact) · the ways in, one row each: its name, when it works, at most two plain sentences, and close-up stills of its people and places, each with a numbered pin · the shared ending (the getaway car) · a small plan of the block with the same pins · the clock as a line of stations, each a time and a short label · the cell's four faces, each with one line for this mission · the purse, Heat as one word (unknown, noticed, wanted, infamous), Day / Night, and Begin. The stills are captured at build time from a replay of the mission's reference route, HUD off, one per pinned place (the place's id, the tick the role is there, the quarter-step yaw facing its door, a close zoom), so a generated mission gets the same page with its own cast and places. Every word reads the mission's data (brief, roles, routes, timeline, text). Outside play, so the reading limits don't apply. Begin: the page lifts away onto the camera at the target's door, the camera glides to the cell (about 3 s; any key or click lands it), the cell's rings pulse once, the roster slides in, and the clock starts at 0:00 once it settles.

## Open questions for Sky
1. **Page or street?** In brief 062 you picked B (on the street) over A (a file on a blotter). This page goes back to paper, but with real stills and words instead of a drawn map and icons. Does it replace B outright?
2. **Where:** keep the small plan drawn from data (as mocked), or ask Latch for a `brief-wide` overhead photo?
3. **Clock:** all seven stations (the rivals at 2:35 and the 5:40 last chance included, as mocked), or only Vale's three (5:30, 5:50, 8:30)?
4. **How much to say:** the front-door hint already tells the player that Inez is close to the guard (Fable's line). Is that too much before they Watch, or right for a first mission?
5. **Night:** should Day / Night swap the stills to night captures?

## Issues found on the way (to Fable and Aster)
- `text.json briefing.clock.line` says "At 8:30 Vale's driver takes him away", and `briefing.timeline[7].label` says "Close: the driver drives Vale away". Both are true only with a turned driver. Unturned, he leaves at 6:13 (`timeline` 6:10). This needs a fix for Fable.
- `brief.json clock` gives three marks (0:00, 5:30 lit, 8:30), so today's overlay lights 5:30 as if it were the deadline. The page lights 5:50 and 8:30 instead.
- There are no clean close-ups of the guard, the service bay or the getaway car in any batch. Three of the five stills are stand-ins, tagged on the picture.
