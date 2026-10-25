# 062 · The briefing and the game start, redone

**State:** designed 2026-10-24 (Glyph). Boards in `briefs/062/`. Nothing built. **Next owner:** Aster (Sky picks), then Latch (spec below) with Fable, Waymark and Forma (asks below).
**Sky, 2026-10-24:** "I don't like our brief card design at all though - massively improve that, same for when game starts, should look a lot better."
**Inputs:** brief 051 (front end) and `briefs/051/lightline/SPEC.md`; Latch's 0.2.99 notes (Day / Night tab words, Begin with its marker); the 0.2.99 captures in `briefs/051/build/0299/`; `out/batch/shots/`; the real 2160 frames in `briefs/049/build/2160-0299/`; mission data `contracts/missions/handover/{mission,brief}.json`; art bible and style contract.
**Boards:** `board-01-today.png` · `board-02-directions.png` · `board-03-start-sequence.png` · `board-04-pick-crops.png`. Full-size (3840 x 2160) mockups: `dir-a-file.png`, `dir-b-street.png`, `dir-c-cards.png`. Sources: `common.py`, `portraits.py`, `build_a.py`, `build_b.py`, `build_c.py`, `seq.py` (run `python3 -I briefs/062/seq.py`). Research: `research/notes.md`.

## 1 · What is there today (board 01)
Four pieces, 0.2.99:
- **The mission brief** (New game): a wall of text on the key art. A hook, three lines, four fields (target, location, window, objective), four intel bullets and a sign-off: 14 lines.
- **The in-game briefing card**: one small plate (`panel_dark_modal`) over the live scene: title, one line, four faces, three stats (8:30, $100,000, 0/3, "Unknown"), Day / Night, Begin. It says nothing about who Vale is, where, or how you get to him, and the scene behind it shows the plaza, not the Office.
- **The menu** and **the loading screen**: restyle A, the key art, the title. Fine.
- **The moment play begins**: the clock is already at 0:05 when the first frame shows; nothing says where you are or why.

The two briefing screens say the same thing twice, in words, and neither has a picture of the man you are after.

## 2 · Research (full notes `research/notes.md`)
Thin: the web gave little layout detail. What it did give and what I know of the games:
- **Syndicate (1993):** the briefing is a map of the level, drawn as a blueprint, and a failed run is shown happening to it. **Phantom Doctrine:** a board of pinned documents joined by string; the strongest screen, but it teaches poorly. **Hitman 3:** planning is a phase you can come back to. **Shadow Tactics / Desperados III:** the map is open at any time; the route you took is replayed after. **Deus Ex:** a handler gives objectives and entry options while you are already in the first room, and the HUD is introduced there. **Invisible, Inc.:** pick faces, read one objective.
- From memory, not found by search: XCOM 2's mission card is a reward and a clock; the squad is a row of faces; the drop-in is a short shot before control.
Rules taken: the target is a face; the map is the briefing and the routes are lines on it; the cell is faces in a row; the first seconds teach; the HUD arrives after the camera.

## 3 · Three directions (board 02)
All three: pictures first (Vale's face, the Records Office, the two ways in, the clock to the handover), at most two short lines, kit frames and kit colours only, no sentence beyond the hook and the objective. The cell, the purse, Day / Night and Begin are the same strip in all three, so they compare on the rest.

| | What it is | Strength | Weakness |
|---|---|---|---|
| **A  The file** | a paper dossier on a dark blotter: Vale's file photograph with a clip and a TARGET stamp, a plan of the block with the two routes, the two ways, a dial | records are physical in this world (stamped forms, photographs); the plan is built from `civic-block.json`, so it generalises | the plan is flat and brings its own colours (asphalt, planters); it is a second picture of a place the game already shows; the most to read |
| **B  On the street (pick)** | the real Records Office frame; Vale's file card, the two ways and the clock are anchored to it; Begin, and the camera flies to the cell | the briefing is the game, so it teaches the board it is about to hand you; the start sequence comes with it; cheapest to build (kit frames, existing ground rings and leader lines, the camera) | the card sits on a busy scene, so it needs the vignette and the scrim; the face art is a stand-in |
| **C  Lobby cards** | three framed pictures (Vale, the front door, the pickup) over the key art, a dial beside | the most handsome; each picture is one thing | the pictures are crops of a place you cannot then see whole; the dial is outside the frame logic; no start sequence of its own |

## 4 · Pick: B, "on the street"
**Why.** The player's job is to read a place and the people in it; B briefs them in that place, in the language they will read it in (cream-on-ink leader lines, dashed ground rings, kit cameos). It costs the least to build and it ends in the start sequence for free: the briefing camera is over the Office, and Begin flies it to the cell. A and C make a good page; B makes the first minute of the game. B takes one thing from A: the photograph on a kit `panel_card`, a file card.

### What is on the screen (1080p; board 04 has the 2x crops)
- **Title** (64, 58): `THE HANDOVER` Jost Medium 15, tracking 0.30, cream, with a 2 px turquoise-light line 132 px long; the hook beneath, Jost Bold 34 ("Vale has a file on you."), then the objective, Jost Regular 19 ("Take him alive before it reaches Security."). Over a corner scrim (ink-deep 0.82 falling to 0 over 700 px). Both words come from `brief.json` (`hook`, `dossier.objective`).
- **The file card** (1448, 132, 408 x 478): kit `panel_card` at 0.5; header `TARGET` (Jost Bold 11.5, tracking 0.12, cream); the photograph 360 x 296, 10 px radius on an ink-deep rim; a brass hairline; the name Jost Bold 28 ink; one fact, `take` icon + "Alive". A leader (cream 2.4 px over ink-deep) runs to the Office door with a dashed ground ring. Nothing else on it.
- **The Office tag** over the canopy: `plate_dark` with `papers` and "Records Office", chrome tail onto the door.
- **The two ways** (1448, 640 and 768; 408 x 112): kit `panel_dark` at 0.5; the way's name in Jost Bold 15.5; three pictures joined by arrows: a face in a cameo ring, a disc with the middle step, a disc with the end. Front door: Inez, the guard's badge (`signal_guard`), the door (`enter`). Pickup: the driver, the car, the take. A dashed leader runs from each to the door and the car, ringed in the world.
- **The clock** (right edge at 1856, top 40; 560 x 82): kit `plate_dark`; `p_time` 0:00, `enter` 5:30 (turquoise-light, the handover), `car` 8:30; a track with the preparation sweep to 5:30 in turquoise-light. Numbers from `mission.json` `timeline` and `clock`.
- **The strip** (bottom, scrim ink-deep 0.82): the four faces (kit `portrait_*_normal` in the chrome ring, 62 px) with role tags (Jost SemiBold 11.5 dim), the purse on a `plate_dark`, Day / Night as the light line's tab words, and **Begin** (Jost Medium 26, tracking 0.34, a turquoise-light diamond and a 2 px line; always focused, Space begins, as 0.2.99 has it).
- **Dropped from the card:** "0/3" and "Unknown" (the HUD shows them in play), the build line (it stays in the menu corner), the wall of text (14 lines), the paragraph of intel (play teaches it: Inez and the guard are in Intel the moment you Listen).

### Colours and checks
- No new colour: cream, ink, ink-deep, brass hairline, chrome, deep teal and the turquoise-light of the light line, all from `kit.uss`. The only other hue is the kit's own `signal_guard` badge (amber), which already ships in the action panel header. The photographs are duotones from ink-deep to cream (derived, not a new colour).
- Contrast of the type against its worst backdrop (the brightest 5 % of the pixels under it), cream or dim on the scene with the scrims: title 7.2:1, hook 7.2:1, sub-line 5.9:1, role tags (dim) 4.8:1, Begin 5.7:1, Day / Night 4.7:1. The unfocused "NIGHT" is cream at 50 % and is below 4.5:1 by design (the light line's unfocused tab); Latch should use whatever alpha the menu gives its unfocused items, so the two read alike.
- 2x crops (board 04): no clipping, the photograph rim, card padding 24 px, chain pictures centred on the row. Checked on the mockup only; Glyph reviews the build's crops.

### Data (nothing hand-authored)
The screen reads the mission, not a file of its own: the target is the `roles` entry for Vale (cast by id: its portrait); the ways are the mission's routes' first contact and the verbs they use (`windows`, `roles`: Inez + guard + enter; driver + car + take); the clock marks are `timeline` (0:00, the chime, the end); the hook and objective are `brief.json`. A generated mission gets the same screen with its own cast. Waymark and Keel: the mission needs `brief.ways[]` (name, contact role, up to three step icons) and `brief.clock[]`; I did not edit contracts.

## 5 · The start (board 03)
1. **Briefing:** the camera rests over the Office (the frame is `overhead-pickup`, taken from where the Records Office front is played).
2. **Begin (0.0 to 0.3 s):** the line under the word fills, a soft chime (Chime); the title lifts away, the cards slide right, the strip drops; the scene stays lit; the clock has not started.
3. **The glide (0.3 to 3.3 s):** the camera flies from the Office to the cell, eased in and out, along the street; any key or click lands it at once.
4. **The settle (3.3 s):** the four ground rings pulse once; the roster slides in over 0.25 s.
5. **Play (3.6 s):** the clock bar fades in and the clock starts at 0:00; the goal line is up.
Today the clock runs from the first frame. Frame 3 of the board is indicative (the start frame a moment before it settles, motion-blurred); the glide's path and length are Latch's to measure from the real camera (the cell is about 64 m west of the Office door).

## 6 · The menu and the loading screen
Kept as 0.2.99 (restyle A). Two continuity rules only: New game goes straight to the briefing (the lore screen of 051 §3 is gone, its job done by the hook and the objective), and the key art cross-dissolves to the Office street over 0.4 s so the briefing is not a hard cut. Begin uses exactly the menu's light-line item.

## 7 · Asks and limits
- **Vale has no face.** The councillor in the game is a stand-in (a young commuter model, brief 022). The mockup turns its hair silver in the photograph; it still reads young. **Forma:** a head-and-shoulders portrait of Vale (and the briefing needs the cast's own portraits for the contacts: Inez and the driver are renders of the waitress and driver models here, the driver's skin tint is the model's own albedo). Until then the screen falls back to the kit's `npc_councillor` badge, which is too small: the briefing cannot ship without the face.
- **Codex returned 401 ("workspace routing discovery unauthorized") on every call from this session**, three tries, so no generated stills were made and no paid API was used. The boards are built from the game's own frames, the kit sprites and Blender renders of the game's figures (`out/scratch/glyph/062/blender/bust.py`). Aster: if Sky wants mood stills from Codex, the login needs refreshing.
- **Fable:** the hook and the objective are the only two lines; if Sky wants fewer, the objective goes.
- **Not done:** the Day / Night preview (Day / Night changes the scene behind the card; I did not draw the night frame); the controls and options pages (not part of this brief); a 2160 capture of the build (none exists yet).
- **Alerts for Aster:** the briefing drops the two 051 screens' text (the sign-off "Quiet hands. Go.", the four intel bullets, the handler lines). They stay in `brief.json`; if Sky wants them they can sit behind a small "file" button as the case notes, not on the first screen.

## Spec for Latch
New `Briefing.cs` (replaces the briefing card and the lore screen): build the layers of board 03 frame 1 as UI Toolkit elements on the real scene: scrim (vignette 0.42 from 0.5, corner scrim), title, file card, two way cards, clock plate, bottom strip. Leaders and rings: the existing `stroke` and ground-ring code of the world markers (cream over ink-deep). Camera: the briefing camera is the play camera placed at a mission-given `brief.camera` (the target's door at the right of centre, about 38 degrees, play zoom); Begin runs a `CameraGlide` to the cell's position over 3.0 s with an ease-in-out and a skip. HUD pieces enter in the order of section 5. Clock starts when the roster has settled (3.6 s). Kit sources are named in each line above.

## Done-check
Boards and mockups written; nothing in Unity, the sim or contracts was edited. **Next owner:** Aster (Sky picks), then Latch with the asks above.

## Sky's calls (2026-10-25)
Sky, 2026-10-25, on the outstanding-work summary that listed each pick: "Your recommendation seem good".
Pick **B, "on the street"**. Not answered: the dropped 051 text behind a "file" button (left out until Sky asks).
Next: Latch (spec above), with Fable (hook and objective), Waymark (timeline numbers) and Forma (Vale's portrait, brief 063).
