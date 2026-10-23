# 046 · HUD style lab: the HUD's look as tokens, outside the game

**State:** built and checked headless 2026-10-22; Sky picked Revised A and gave the roster / arrows / ground-marker notes below; rounds 3 (icons under the portrait, one weapon icon), 4 (type) and 5 (character-plate research, dividers) done; Sky picked Jost Regular and icons under the portrait; divider picked (steel line, no rivets) and the roster column art cleaned in round 6 (rivets, side stripes, seams). Owner: Glyph (interface-artist). Next owner: Sky picks a preset (and a roster layout); then Latch loads `testbed/hud-style/export/hud.tokens.json` (and the recoloured sprites) into the HUD; Splice lifts the roster alignment check.

## Why
Sky, after the revised direction A boards and playing 0.2.56: "the panels have lost that cream / clean look a little now - overly blue. And the hold progress bar animation isn't great. Let's put an art style together outside of the game so we can iterate." Then, on the roster crop: "there's a lot of polish needed - look how bad the icons align". Aster added: "why did the door guard shoot the Enforcer?" (the cause cue).

## Objective
A web page that shows the whole HUD kit as the game draws it, with the look as tokens you can change live, several hold-progress animations to press and hold, presets as tabs, and an export the game can load. The lab is the source for HUD colours, the hold animation, the roster badge slots and the under-fire "why" cue.

## Run
- `bun run hud-style` then open http://localhost:5175/ (not published; the controls lab's publish step is the model if wanted).
- `bun run hud-style:check` (server up) = `bun testbed/hud-style/check.ts`: every preset, every scene, layout QA, roster alignment, real mouse hold tests, the export; pictures to `briefs/046/` (git-ignored). `bun testbed/hud-style/shot.ts "p=creamPlate&s=board&z=1&bare=1" out.png 2 "x,y,w,h"` shoots one thing.
- Links carry state: `#p=<preset>&s=<scene>&z=fit|1|2&g=<ground>&c=<cause>&i=1`.

## What is in it (`testbed/hud-style/`)
- **Real parts:** the kit's own `@2x` sprites and Jost / Barlow, read straight from `unity/Assets/Suborned/UI/Resources/Kit` (served at `/kit/`), so the lab cannot drift from the game's kit. `kit.uss` colours are the only swatches; any other colour is marked "no source" and the export refuses to call it sourced.
- **Backdrops:** the eight 0.2.56 shots with the control-lab overlays inpainted out (brief 044) and now the whole HUD inpainted out too (`prep/backdrops.py`), so every HUD piece is drawn live. The smudges where the old roster, clock and feed were are retouch, not design. The speech bubble "Debt. The guard visits her quietly. She startles." is baked into the cafe shots (it is the world's speech; the sentence under the pill was already removed from the build per 044).
- **Scenes:** Board (every piece, over a blurred ground you can swap), the nine brief 044 scenes, Under fire (why), Roster slots. Fit / 1x / 2x zoom and a "Jump" list that centres any piece at 2x.
- **Components:** roster column and portraits, status strip, Squad plate and gun key, clock bar, mini plate, minimap, feed card, Intel card, goal line; verb pill in offer / hold / ready ("Let go") / fire / order (walk-in plate) / Watched spot; done chip with facts and unlock pills (also half answer, shut way); window marker (counter, last 10 s amber blink, the Tech's face); cancel tag; pops; the rings (ground selected, hint, activity ring on the badge, aim); the under-fire cue.
- **Tokens (right panel):** body colour of the HUD frames and of the overlays (kit teal, cream, ivory, seafoam, pale seafoam, ink, deep teal, or a kit swatch), texture, shadow; words / head word / dim; live accent, "running out" amber, brass hairline, tail body; kind-disc family (dark / light), disc body, icon colour, disc size, icon size, face size; hold animation, easing, time, release moment, cancel, progress colour, strength, ticks; roster layout; why cue (where, disc, size, icon, pulse, ring and chevron colours, count). Key `\` flips to the last preset.
- **Recolouring the frames:** the kit has only dark teal frames and a few cream plates (`plate_light`, `panel_card`). The lab recolours the sprites' body pixels at runtime (chrome rim, screws, inner line and stripes kept, the sprite's own shading kept) so cream, seafoam and ink bodies show true to the kit's chrome. If Sky picks one, Glyph repaints that body into the kit sprites properly; the export carries the lab's recoloured PNGs as the target.

## Presets (tabs)
| Tab | Idea | Hold | Source of colours |
|---|---|---|---|
| As built 0.2.56 | what Sky played: kit HUD, flat bright overlays (blue, green, pink, white 2 px border), white 55 % bar, roster badges as drawn | fill, white | history only |
| Revised A | brief 044 A: kit teal, chrome, cream type, turquoise-light for the live thing | fill sweep, turquoise-light 40 % | all kit.uss |
| Cream plate | cream body on every frame, chrome rim, ink type, deep teal for the live thing, light disc with ink icon | rim fill | cream, ink, warm ink, deep teal, brass, amber (kit.uss, contract) |
| Seafoam enamel | the civic seafoam band (`--footer-result` #bae2d6) as the body, ink type, deep-teal discs | segmented ticks | kit.uss |
| Cream with teal discs | cream overlays, the HUD frames and the discs keep the kit's deep teal | radial on the disc | kit.uss |
| Ink and ivory | no cyan: ink body, ivory live accent, brass hairline | word wipe | kit.uss |
Pictures: `briefs/046/board-presets.png` (pills and chips, all six), `<preset>-<scene>.png` (1080p), `crop2x-<preset>-{pills,chips,hud,rings,why}.png` (2x crops).
My view: **Cream plate** or **Cream with teal discs** answer "lost the cream / clean look" without leaving the kit: the teal stays on the HUD frame and the discs, cream carries the readable parts. Sky decides.

## Hold progress (Sky: "the animation isn't great")
Press and hold any "Hold" pill (Board, Pill states, Help pill scenes). Seven progress styles, each with easing (linear, ease-in, ease-out, ease-in-out, snap-end), time, a release moment (snap: scale pop; ripple: ring out; flash: wash; none), and what an early let-go does (drain back or snap to zero). Ready always says "Let go" in the live colour with the 2 px ring and glow.
| Style | What moves |
|---|---|
| Fill sweep | the as-built bar inside the window |
| Rim fill | a 3 px line draws round the edge, clockwise from the top left |
| Segmented ticks | 4-20 ticks in the window light one by one (reads like the pips) |
| Radial on the disc | a ring fills round the kind disc, the disc swells 8 % |
| Underline | a 3 px line under the words |
| Word wipe | the words light from dim to full, left to right |
| Charge glow | a glow grows round the pill |
Pictures: `board-hold-cream.png`, `board-hold-dark.png` (frames at 0 / 25 / 50 / 75 / 100 %), `release-{snap,ripple,flash}.png` (110 ms after the mouse goes up), `live-<style>-mid.png` (real mouse, mid hold). Headless checks press the mouse for each style: hold at mid, ready says "Let go", release resets, an early release cancels.

## Roster slots (Sky: "look how bad the icons align")
**What is wrong in 0.2.56** (crop in the brief 044 thread): the orange-ringed eye badges are cut by the rim at three different offsets and the orange (#e8603a) is in no kit source; number plates sit on the faces; the gun discs overlap the rim and the Tech's is a grey ring, the others a cyan fill; the dots lie over the bottom of the face; the Enforcer has a second eye plus a "II" plate over the top-right; the top portrait is tight to the frame. The lab reproduces this as layout "As built" so it can be compared and as the negative control of the alignment check.
**The geometry that forces a choice (measured from `roster_column@2x`):** the column is drawn at 0.79 of its 109 x 416 art at (14, 15). Face window 66 nat = 52.14 px (r 26.07); ivory mat to r 29.6; chrome rim to r 36.34; ring centres at x 56.98, y 66.82 / 141.00 / 216.37 / 290.71 (pitch 74.3 / 75.4 / 74.3: the sprite's own spacing; the rings are 2.3 nat px apart and the column edge is 3 px past the rim). **There is no free margin inside the column**: any badge inside it is on the face or on the rim. So:
- **Rail (recommended, the only layout that meets "nothing crosses the rim or the face")**: a rail tab on the column's right edge per portrait, `plate_dark` (nine-slice, scale 0.4) 54 x 66, left edge at column x 84.11 (abs 98.11), top = ring centre - 33. The status strip (`done`) and the slot strips move right to x 162 (they were at 110/125): Latch's dock changes: done strip +52 px, slot strips +37 px.
- **Inside (kit's original band design, tidied)**: badges on the face, same slots for all four, smaller (17 px); kept to compare, not recommended.

**Slot spec, px at 1080p (multiply by 2 for the 2x art; sizes are the 1080p sizes of `@2x` sprites)**. Rects are [x, y, w, h] from the tab's top-left (rail) or the face window's top-left (inside).
| Slot | Holds | Rail rect | Inside rect | Sprite and size |
|---|---|---|---|---|
| `num` | F1..F4 number | [4, 7, 24, 16] | [3, 3, 22, 15] | `markers/plate_light`, Jost Bold 10.5 ink |
| `status` | one of: down, seen, watched, busy | [30, 5, 20, 20] | [32, 3, 17, 17] | `markers/disc_dark_24`, icon 12 px (60 %) cream |
| `weapon` | drawn / holstered | [6, 29, 20, 20] | [2, 24, 17, 17] | drawn: `disc_light_24` + `fire` icon in ink; holstered: `disc_dark_24` + `fire` icon at 55 % |
| `alert` | the "why" icon when under fire, else hurt | [30, 29, 20, 20] | [33, 24, 17, 17] | `disc_light_24` + cause icon ink; hurt: `disc_dark_24` + `s_hurt` |
| `dots` | four pips (health or ammo as built) | [8, 53, 38, 8] | [13, 42, 26, 6] | `markers/pip_full` / `pip_empty` 6 px, gap 4, on a track in `--pip-empty` #142c2c so full and empty read on cream bodies too (inside: 4 px, gap 3) |
Rules: one badge per slot, one icon at a time; status priority down (`s_downed`) > seen (`sawyou`) > watched (`cone`) > busy (the action's `s_*` icon); the number is always shown; a slot with nothing in it is empty, not hidden (the layout never moves); portrait art follows the state (`portrait_<role>_normal | hurt | down`); selected = `portrait_ring_selected`, busy = `portrait_ring_busy`, under fire = the pulse ring on the rim (the one thing allowed to sit on the rim, it is the cue). Kit sprites only: the orange ring, the cyan gun fill and the grey ring are dropped.
Every state is shown in `Roster slots` (normal, selected, busy, seen, watched, drawn, holstered, under fire, hurt, down) for the chosen layout, and the three layouts side by side; the "Slot guides" box draws the slot rects (pink), the face (cyan) and the rim (yellow).

**Automated alignment check (for Splice)**: `ALIGN` in `testbed/hud-style/check.ts` runs on any DOM with `.roster[data-layout]`, `.rw[data-i]`, `.rtab`, `.bd[data-slot][data-i]` and passes for rail in every scene of every preset. It asserts, per roster: (1) every badge's content lies inside its slot rect (0.6 px); (2) rail: every slot rect is at the spec offset from its tab; (3) rail: no badge comes within r 36.34 of the face centre (clear of face and rim); (4) no two badges touch; (5) the same slot has the same offset from the ring centre on all four portraits (0.7 px). Negative control: the "As built" column must fail with at least 6 violations (it fails with 16). The Unity version of the check reads the same numbers from `rosterSpec` in the tokens file.

## Under fire: why (Aster: Enforcer shot by the door guard, cause unclear)
One icon, the strongest cause the engine reports for a hostile (Keel's read: drawn in sight, fired, has Vale, violence, known face; rivals: drawn, Vale, seen), no sentence. It rides on the existing under-fire cue in two places: **on the portrait** (a pulse ring on the rim plus the cause disc in the roster's `alert` slot) and **over the agent** (number plate plus the cause disc, at most two icons over a person, and three ground chevrons pointing at the shooter, pulsing outwards). Icon map (kit icons only, in the tokens): drawn = `fire`, fired = `bang`, has Vale = `s_carrying`, violence = `takedown`, known face = `papers` (a physical wanted sheet, in this world's terms), seen (rivals) = `sawyou`. The disc is the kit's disc (light with an ink icon by default so it reads on the dark roster and on pavement), 34 px over the agent, 20 px in the slot; the ring and chevrons are cream or ivory with an ink-deep edge, a deep-teal ring on cream presets: no new colour, and no red or amber (those stay signals). Tokens: `why.place` (both / portrait / head / none), `disc`, `size`, `iconRatio`, `ring`, `chevrons`, `pulseMs`, `chevronCount`, `icons`. Pictures: `board-why.png` (six causes on two presets), `why-<preset>-<cause>.png`; the Cause menu in the bar switches it.
Open for Keel/Sky: `fired` and `drawn` are both guns; I used the `bang` pictogram for "fired" because the gun already means "drawn". If a stronger "fired" glyph is wanted it is a new kit icon (Glyph can draw it).

## Other badges (same slot discipline)
- **Seats:** a badge either sits wholly inside a named slot, or on a *rim seat*: centre on the rim circle at 45 degrees, drawn on top with its own ink-deep ring (never cut by the frame). The chip's outcome mark (`mark_ok / half / no`) is a rim seat on the 32 px cameo (offset -6.4 / -6.4; was -7 / -6, which sat off the rim by a pixel).
- **Sizes:** discs come from {17, 20, 26, 30, 34}; the icon is 58-60 % of the disc.
- **Repeated rows:** the Intel card's discs and marks have identical offsets in every row.
- Checked in `ALIGN` (`intel`, `chipmarks`). In the game (not touched): the overhead marks, the slot strips and the feed card kind disc need the same pass when Latch ports; send me a crop of any that look off.

## Export (what Latch loads)
The panel's **Export as picked** writes `testbed/hud-style/export/<preset>.tokens.json`, copies it to `export/hud.tokens.json` (the file Latch loads; today it holds Cream plate as my recommendation, not Sky's pick) and the recoloured frame sprites to `export/sprites/`. Schema `suborned.hud.tokens/1`: `frame`, `body` (hud, overlay, gain), `type`, `accent`, `disc`, `face`, `rim`, `roster.layout`, `why`, `hold` (anim, ease, ms, release, cancel, fill, alpha, segments), plus `resolved` (HUD text and live colours that follow the HUD body, hold fill as rgba), `sources` (every colour and where it comes from; none is "NO SOURCE"), `recolor` (the algorithm and the sprite list), `rosterSpec` (the geometry and slot rects above). The check writes and validates all six.
Latch reads it at startup (one `HudTokens` ScriptableObject or JSON in Resources), sets the `kit.uss` custom properties from `type` / `accent` / `body`, picks the sprite set, and drives the hold from `hold`. Easing names are the CSS ones; `snap-end` = slow to 80 % then rushes (0.7 at 0.8, linear after).

## Done-check
- [x] Lab runs from one command; real kit sprites, fonts and backdrops; 1080p and 2x zoom.
- [x] Every listed component; verb pill in every state; hold animations playable with the mouse (headless real-mouse test per style).
- [x] Tokens as controls; six presets as tabs (As built, Revised A, three cream / clean directions, ink fallback); export JSON per preset.
- [x] Roster as a first-class component with slot spec, every state, three layouts, guides, alignment check with negative control.
- [x] Why cue, six causes, in the presets and the export.
- [x] Headless: `bun run hud-style:check` = ALL OK (6 presets x 8 scenes layout QA, ALIGN, 28 hold checks, why checks, 6 exports). `bun run typecheck` clean.
- [ ] Sky plays it and picks (preset, hold style, roster layout, why placement).

## Evidence
- Command: `bun run hud-style:check` -> `ALL OK (layout problems: 0)`; every line `PASS` (QA per preset and scene, `ALIGN` per roster, `hold <style>: mid / ready / release / cancel`, `why <preset> <cause>`, `export <preset>`), negative control `16 found`.
- Pictures (all in `briefs/046/`): `board-presets.png`, `board-hold-cream.png`, `board-hold-dark.png`, `board-why.png`, `<preset>-board.png`, `<preset>-roster.png`, `crop2x-*`, `release-*`, `live-*-mid`, `holdstrip-*`.
- Files: `testbed/hud-style/{index.html, app.js, parts.js, scenes.js, tokens.js, sprites.js, hud.css, lab.css, serve.ts, check.ts, shot.ts, prep/backdrops.py, bg/*.jpg}`, `package.json` scripts `hud-style`, `hud-style:check`. Nothing in Unity, sim or tools was edited.

## Open
- Sky: which preset; which hold style and release; rail or inside for the roster (rail moves the strips right by 37-52 px and widens the left column by 54 px).
- The recoloured bodies are lab approximations (a runtime recolour of the kit's pixels). The picked one is repainted into the kit by Glyph before Latch ships it.
- Roster dot meaning (ammo or health) and what "watched" should show (the cone icon is my pick) need confirming with Motive/Keel; the layout is the same either way.
- The minimap is drawn as an empty frame (the real map art is Latch's); the lab does not restyle it beyond the frame.
- The game still draws the move paths and ghost pills in the lab's role colours (brief 044 §6); they are not in this lab yet.

## Next owner
Sky (play, pick) -> Latch (load `hud.tokens.json`, build the roster to the slot spec, move the strips) -> Splice (lift `ALIGN` into the Unity proof) -> Glyph (repaint the picked body into the kit; 2x crop review of the build).

## Sky's picks, 2026-10-22
- **Style: Revised A** (the lab's "Revised A" preset is the game's tokens).
- **Roster:** the rail's neatly aligned slots "kinda make sense", but "not into showing the 1,2,3,4 here - not entirely sure if needed that much". Try the rail without the number tabs and see what changes.
- **Cancel tag: removed for now** ("confusing, remove it for now and I'll think of something as I play through"). The cancel pop and the "cancelled" plate stay.
- **Ground markers must respect the foreground:** the selection ring drew over the agent's legs; markers sit on the ground and are hidden by anything in front of them.
- **The shooting arrows** (the ground chevrons toward the shooter) "are horrible at the moment, need to be a lot more subtle". Redesign in the lab.


## Round 2 (Glyph, 2026-10-22): roster without numbers, subtle under-fire cue, ground markers behind the agent
`bun run hud-style:check` -> `ALL OK (layout problems: 0)`, 412 PASS, 0 FAIL (6 presets, 11 scenes, ALIGN incl. the new layout, the arrow checks, 28 hold checks, 6 exports); `bun run typecheck` clean. Revised A is exported last, so `export/hud.tokens.json` is Revised A (id `revisedA`). The lab's other presets stay as tabs.

### Roster: rail without number tabs (layout `railLite`, now Revised A's default; `rail` with numbers is still a menu choice)
- Slots: `status` [12, 6, 20, 20], `weapon` [12, 29, 20, 20], `dots` [4, 53, 36, 8], on a **44 x 66 px tab** (was 54), left edge at column x 84.11 as before. Alert (under fire) takes the `status` slot with priority down > under fire > seen > watched > busy; hurt has no badge (the portrait art already changes: `portrait_<role>_hurt`). Re-balanced: discs stack in one column, pips under them, a 3 px gap, 5-6 px padding; the status strip moves to x 152 (was 162; 110 in the original).
- **What is lost without numbers:** the key hint (1-4 still select; the column order is fixed Scout, Fixer, Tech, Enforcer, and the portrait faces name who is who); the number plate over an agent under fire is dropped in this layout (the cause disc alone is over the head); nothing else read the number. Pills and chips already name people by face.
- **Narrower?** Yes: 54 to 44 px (-10), and the strip -10. Floor is about 36 px: the plate's nine-slice border is 9.6 px a side at scale 0.4, leaving 24.8 px inside; 20 px discs fit down to about 36 if the plate is drawn at scale 0.3 (the one `panel_strip` uses). I stopped at 44 so the pips keep 6 px / 3 px gap.
- Every state shown (normal, selected, busy, seen, watched, drawn, holstered, under fire, hurt, down): scene **Roster: no numbers**, with the numbered rail beside it. `ALIGN` runs on it (slot at spec offset, nothing within r 36.34 of the face, no touching badges, same offsets on all four) and passes; the as-built negative control still fails with 16.
- Pictures: `briefs/046/board-roster-nonum.png`, `nonum-roster-2x.png`, `nonum-states-2x.png`, `revisedA-roster-nonum.png`.
- Open: with the status slot empty most of the time the weapon disc sits in the middle of the tab. Alternative is weapon on top, status below; say if you want it.

### Under-fire arrows: four subtle ground cues (scene **Arrows: every style, three grounds**, and **Under fire: arrow and why**)
Sky: "horrible ... need to be a lot more subtle". Replaced the three stacked chevrons (kept as "As built" for comparison). Each cue is **one mark**, cream (#f7f4ec) with an ink-deep (#0b1615) edge so it reads on pavement and asphalt, 100 % at most and breathing lower, drawn in the ground plane (flattened by sin 38 deg = 0.616), animated, no red or amber, no new colour. Units: the selected ring's outer radius = 100. Specs are in `ARROW_SPEC` (tokens.js) and in every tokens export under `ground.cueSpec`.
| | What it is | Moves |
|---|---|---|
| A Arc | a 70 deg arc just outside the ring (r 118), 10 units thick, centred on the shooter | breathes 60 to 100 % opacity, 0.9 s |
| B Notch | one small wedge on the ring edge, tip toward the shooter | drifts 8 units out and back with the pulse |
| C Streak | a faint 60 unit streak that travels in along the line to the shooter and fades before the ring | one pass per 1.3 s, nothing between passes |
| D Ripple | a 50 deg arc that leaves the ring toward the shooter and fades | one pulse per 1.3 s, nothing between |
My lean: **A** for readability, **C** if you want the quietest. The cue works with no ring (agent not selected) because it carries its own radius. Controls: Style, Weight, Strength, Colour, Edge in the panel; Shooter angle, Ring (selected / hint / none) and Behind agent in the bar; hash `as=<style>&a=<deg>&rg=<ring>&o=<0|1>&cue=<head|arc|portrait>`.
- **Backdrop:** the only real play-zoom frame with a clean person on the ground (briefs/024 calm-ring, the city guard), cut out (`prep/ground.py`, polygon + GrabCut; he stands in for the cell), on pavement and pasted on asphalt. Shown at 1:1 (person about 65 px = play zoom), at 1.8x and 2.6x. The earlier third-person backdrop could not be cleaned of its baked ring and chevrons, so the under-fire scene no longer uses it.
- **Why-cue placement next to it** (`why.arrow.cue`): `head` = the 26 px cause disc above the head (was 34; no number plate); `arc` = a 20 px disc riding on the cue so direction and cause are one object; `portrait` = only the roster tab (the cause disc in the status slot) and the pulse ring on the rim. Picture: `board-cue-place.png`. Default `head`.
- Checks: each style gives one decal per tile, sits under the agent, uses only kit colours, is animated (paused frames at 0 / 220 / 440 / 660 ms); `cue-*` placement tests.
- Pictures: `board-arrows-frames.png` (frames, pavement and asphalt), `board-arrows-1x.png` (play-zoom size, pavement / asphalt / not selected), `revisedA-arrows.png` (all styles on three grounds), `revisedA-under-fire.png`, `arrowfr-*`, `arrow1x-*`.

### Ground markers behind the agent (scene **Ground markers: behind the agent**)
Ground rings and cues are decals: the agent hides the back half; the lab layers bg, ring and cue, then the agent cutout, then the HUD disc. Toggle "Behind agent" in the bar; `board-ground-occlusion.png` shows 0.2.56's look (ring over the shoes) against the decal look, on pavement and asphalt. The export carries `ground.occludedByAgent: true`, flatten 0.616, ring 19 px radius at play zoom. **For Latch:** draw the ring and the cue as ground-plane decals that depth-test against characters (or a stencil from the agent), render queue below characters and above the ground; the cause disc and number plates are screen HUD and stay on top. The overlap is only the back arc, so a cheap alternative is to split the ring into front and back halves.
Not changed: Unity, sim and tools.


## Round 3 (Glyph, 2026-10-22): icons under the portrait, one weapon icon
Sky, on the roster rail without numbers: "what about we add icons under the character?" Read as: weapon, status and dots in one slim row directly under each portrait, inside the column. Scene **Roster: icons under the portrait** (`#s=roster-under`), layouts `under` and `hang`, picture `briefs/046/revisedA-roster-under.png`.

### The column (nothing redrawn)
The rings are 3.2 px apart, so the column has to grow. The kit's `roster_column@2x` (218 x 833) is cut at the clean body row in each gap (rows 223, 412, 601, 789: no chrome, no screw) and the one body row at 222 is repeated `d2x` times in each of the four gaps (three between rings, one before the foot): nine-slice by rows, the rings, rims, screws, caps and seafoam stripes are the sprite's own pixels. The repeated row is smoothed sideways inside the stripes so the sprite's grain does not stretch into a line (`COL`, `TALL` in `tokens.js`; `tallColumn` in `sprites.js`; recipe in the export `rosterSpec.column`). Seam crops: `briefs/046/under-seam-4x.png`, `hang-seam-4x.png`.

### The three layouts, at 1080p (numbers come from `rosterCost`, checked against the drawn DOM)
| | Column height | Share of screen height | Dock width | Share of screen area | Portrait pitch | Status strip x |
|---|---|---|---|---|---|---|
| Rail, no numbers (was the default) | 328.6 px | 30.4 % | 128.1 px (86.1 column + 42 tabs) | 1.90 % | 74.5 | 152 |
| **Icons under the portrait** (new default) | 412.3 px (+83.6) | 38.2 % | 86.1 px | 1.71 % | 95.4 | 110 |
| Icons on a hanging plate | 393.3 px (+64.7) | 36.4 % | 86.1 px | 1.63 % | 90.7 | 110 |
Under costs +7.8 points of screen height but 42 px less width, so the whole dock is 10 % smaller in area, and the status strip returns to its original x 110 (the rail moved it to 152). The Squad plate and gun key move down with the column (358 + 4 x d).
- **Under:** a 67 x 16 px row on the column body, centred in the gap: weapon disc 16, status disc 16, four pips (5 px) on a 28 px track. Nothing touches a rim (clear of every ring, 4 px each side) or the seafoam stripes.
- **Hanging plate:** the same row on a 72 x 22 `plate_dark` tucked 1 px over the edge of the ring above and the ring below; discs 15. Saves 19 px of height but the plate makes it read as one object per portrait (and a frame within a frame). My view: **under** wins (cleaner, the smaller dock); the plate is there if the row needs to read as a unit.
- Slots: `weapon [0,0,16,16]`, `status [19,0,16,16]`, `dots [39,4,28,8]` from the row; hang: `[3.5,3.5,15,15]`, `[21.5,3.5,15,15]`, `[40,7,28,8]` from the plate. Priority unchanged (down > under fire > seen > watched > busy). Empty slots stay empty.
- **Alignment check** (`ALIGN`) passes on both: slot at spec offset, no badge within 36.34 px of any ring centre (own or neighbour), badges and row inside the column body (x 6.7 to 79.4), no touching, same offsets on all four. Negative control (as built) still 16 violations.

### One weapon icon
The "two guns" in the rail were the weapon disc (holstered: the kit's `fire` pistol at 55 %) and, on the Enforcer under fire, the cause disc "drawn", which is also `fire`. Decision: **one pistol pictogram (the kit's `fire`), one slot, two states**: drawn = light disc, full ink gun; holstered = dark disc, the same gun at 55 %. Nothing else on a portrait is a gun: when the strongest cause is "drawn" the weapon disc itself carries it (forced to drawn, drawn with the kit's `disc_light_24_selected`, turquoise ring), and the status slot stays free; "fired" shows `bang` in the status slot beside the drawn gun. Applied to every layout (`under`, `hang`, `railLite`, `rail`, `inside`); `ALIGN` fails any portrait with more than one `fire` icon. Token: `roster.weapon`; export `rosterSpec.weapon`. All states are in the scene (normal, selected, busy, seen, watched, drawn, holstered, fired on, drawn in sight, hurt, down) for both layouts.

### Default
Revised A's roster layout is now `under` (the other layouts stay menu choices: Badges). Export: `rosterSpec.slots.under / hang`, `rosterSpec.cost`, `rosterSpec.column` (recipe, cuts, row, d2x, resulting ring centres), `rosterSpec.weapon`; the exported sprites include the stretched columns (`hud-roster_column-under@2x.png`, `-hang@2x.png`) as the target art. For Latch: build the column from the kit sprite by the recipe (nine-slice by rows), not from a new file. Open: dot meaning (health or ammo) still with Motive.

## Round 4 (Glyph, 2026-10-22; Aster relaying Sky): the HUD type
Sky: "I don't like our font - it's too bold, needs to be a lot cleaner." Token `font` (panel: Type; hash `f=<id>`), scene **Type: six fonts on the same HUD** (`#s=fonts`): pills, chip, marker counters, clock bar, feed card, goal, status strip and plates, once per option; pictures `briefs/046/fonts-1x.png`, `board-fonts-2x.png`, `font-2x-<id>.png`, `fc-compare.png`.
| Option | Body / strong | Licence | Note |
|---|---|---|---|
| Jost Bold (as built) | 700 / 700 | SIL OFL 1.1 | what Sky played; proportional digits |
| **Jost Medium** | 500 / 600 | SIL OFL 1.1 (Indestructible Type, 2020; no reserved name) | same family, two weights lighter; recommended |
| Jost Regular | 400 / 500 | same | lightest; thin on a dark frame at 12 px |
| Outfit Medium | 500 / 600, scale 0.96 | SIL OFL 1.1 (Outfit Project Authors 2021; no reserved name) | geometric, a little rounder |
| Figtree Medium | 500 / 600, scale 0.93 | SIL OFL 1.1 (Figtree Project Authors 2022; no reserved name) | tallest x-height, most legible small |
| Manrope Medium | 500 / 600, scale 0.87 | SIL OFL 1.1 (Manrope Project Authors 2018; no reserved name) | wide, even, less retro |
All are OFL, so they can ship in Unity (licence text goes beside the fonts, as the kit does for Jost and Barlow). **Strong** = clock, head word (Hold / Let go), counters, Intel head, feed head, goal name, status word, clock bar values; everything else is body. Barlow Medium stays for speech and Intel lines. `scale` matches each font's x-height to Jost's so one size means the same everywhere; effective size = nominal x scale, rounded to 0.5 (pill 15.5, chip 15, counter 14.5, clock 20 ... in the export `fontSpec.sizes`).
- **Tabular numbers:** Unity's text engine has no `tnum` switch, so the lab uses static instances of the variable files (`fonts/*.ttf`, built by `fonts/build-instances.py.txt`, fontTools) with the digits already mapped to their tabular forms (modified builds are allowed by the OFL with no reserved name; keep the licence texts). Check: `1111`, `0000` and `8888` are the same width on all five (Jost Bold: 30.6 / 39.7 px).
- **Recommendation: Jost Medium (500 body, 600 strong).** It keeps the Futura-like silhouette the kit, the icons and the paintings already sit with and only removes the weight; no new family in the build (the kit has Jost Medium and SemiBold already; the tabular builds replace them). If it is still not clean enough, Figtree Medium is the one to try: friendliest and the best at 12 px. Revised A (and every preset except As built) now uses Jost Medium.
- Export: `font {id}` and `fontSpec` (family, body and strong weight and file, scale, licence, source, tabular method, sizes per role, strong roles, all six options). Latch: `-unity-font` the two files, sizes from `fontSpec.sizes.effective`.

## Sky's picks, 2026-10-22 (round 4)
- **Font: Jost Regular** ("think I like Jost Regular").
- **Roster: icons under the portrait**, "but try with a divider between characters perhaps? Maybe find some AAA game titles for examples of good character plates and states."


## Round 5 (Glyph, 2026-10-22): character-plate research, dividers between characters, Jost Regular
Sky: font Jost Regular (keep tabular digits); roster icons under the portrait, "but try with a divider between characters perhaps? Maybe find some AAA game titles for examples of good character plates and states."

### 1. Research (sources and links: `briefs/046/research/sources.md`; captures stay on disk under `briefs/046/research/`, never committed)
Twelve studies, real-time tactics and squad games first (Desperados III, Shadow Tactics, Commandos: Origins, Satellite Reign, Syndicate 1993 and Wars), then turn-based squads (Jagged Alliance 3, Phantom Doctrine, Mutant Year Zero, Gears Tactics, XCOM 2) and party frames (Baldur's Gate 3, Mass Effect). Not used: Diablo IV and Dragon Age: The Veilguard (no reachable source showed their party frames; I did not guess). Boards: `briefs/046/board-research-studies.png` (12 crops, what works in a line or two each) and `board-research-principles.png`. Open the page: http://localhost:5175/research.html.
What I saw, in brief: a squad column is one portrait with a thin state line directly under or beside it (BG3 hit points under the face, JA3 and Mutant Year Zero ticks under the card, Commandos ticks); characters are divided by space (BG3, JA3, Commandos), by a recessed slot (Desperados III), or by heavy frames (Satellite Reign, Syndicate: the busiest); selected means promoted (bigger, colour against grey, one frame, a notch tab); hurt and down change the portrait (BG3 red fill, ME2 red then grey, JA3 red tick and "!"); words appear only for the selected one; digits only where the game is numeric.
**Principles for our roster**
1. The block is one portrait and one thin state line; nothing else sits on a character.
2. Separate characters with space or one quiet mark, never a frame per block.
3. The state row hugs its own portrait; the air goes after the row (proximity says whose icons they are).
4. Selected is promoted (scale, colour against grey, one frame), never recoloured by role.
5. Hurt and down change the picture first (art), the bar second.
6. One status glyph in a fixed slot with a fixed priority; an empty slot stays empty; shot at or seen is one glyph or one ring pulse on the portrait.
7. Words only for the selected one (ours: none on the roster).
8. Digits only where the game is numeric (ours is time and chance: pips, no digits, no key numbers).
Our roster already follows 1, 4, 5, 6, 7, 8; the divider work is 2 and 3.

### 2. Dividers (scenes `Roster: dividers between characters (2x)` = `#s=roster-div` and `Roster: dividers, every state (1x)` = `#s=roster-divstates`; token `roster.divider`, hash `dv=<id>`, panel Roster)
Seven variants in the same "icons under the portrait" column, the same four portraits, every state (normal, selected, busy, seen, watched, drawn, fired on, hurt, down), kit sources only. The row now hugs its own portrait in every variant but the first (2.5 px under the rim); the mark and the air go after it. The mark lives in the gap between two portraits, so the gap, and with it the column, grows by the mark's height. Geometry and heights are in the export (`rosterSpec.column.under.dividers`).
| # | Divider | Mark (all from kit sources) | Column px | % of height | Air above / below the row |
|---|---|---|---|---|---|
| 1 | None (as now) | nothing; row centred in the gap | 412.3 | 38.2 | 4.05 / 4.05 |
| 2 | Spacing only | nothing; the air under the row is the divider | 418.6 | 38.8 | 2.5 / 7.2 |
| 3 | Brass hairline | 1 px, 58 wide, fading at the ends; `--brass` #99784d | 415.4 | 38.5 | 2.5 / 6.4 |
| 4 | **Seafoam band** | 3 px bar, stripe to stripe (72 wide); `--seafoam` #72b5a7 | 423.3 | 39.2 | 2.5 / 8.4 |
| 5 | Screw strip | the column's own two screws (cropped from the sprite, 4.7 px) and a 1 px chrome line at 45 %; `--chrome` | 428.1 | 39.6 | 2.5 / 9.6 |
| 6 | Chrome rule, notch | 2 px chrome rule with a 9 x 4.5 chrome tail (the chip tail shape, body #0c3434) at the centre | 434.4 | 40.2 | 2.5 / 11.1 |
| 7 | Recessed well | the icon row in a darker well, 70 x 21; `--pip-empty` #142c2c; air below | 423.3 | 39.2 | 4.5 / 3.9 |
Pictures: `briefs/046/board-dividers-1x.png` (the seven columns, with the table), `board-dividers-2x.png`, `board-dividers-states-1x.png` (7 variants x 9 states and the seven columns at actual size), `dividers-states-2x.png`, `dividers-actual-2x.png`, `dividers-dock-2x.png` (the recommended one in the dock).
**Recommendation: 4, the seafoam band.** It is the only mark that reads at actual size without reading as clutter: a flat 3 px rung from stripe to stripe, in the column's own seafoam, so the column looks like a ladder of four bays (the cap at the top already has a seafoam square). It costs 11 px of height over today (39.2 % of the screen height instead of 38.2 %), the width does not change, and it adds no colour. The hairline (3) is the quietest but almost disappears at 1x; the notch (6) points like a "expand" chevron; the screws (5) are too fine to read; spacing only (2) is the cheapest and is what BG3, JA3 and Commandos do, if you want no mark at all; the well (7) is the alternative if you want each character's icons to read as one plate (same cost as the band). Sky picks; every variant is a menu choice (Roster, Divider) and a `dv=` link.
Applied: Revised A's `roster.divider` is `band` until Sky picks; the export carries all seven (geometry, source colour, column height, ring centres). The kit column is still cut by rows (the gap follows the divider, `d2x` 53 / 57 / 55 / 60 / 63 / 67 / 60 for the seven variants, in the export); nothing is redrawn.

### 3. Type
Revised A's `font` is now Jost Regular (400 body, 500 strong), tabular digits kept (static instances, same files as round 4: `1111 = 0000 = 8888` wide, checked). The other presets stay on Jost Medium. Revised A is exported last, so `export/hud.tokens.json` is Revised A with Jost Regular, icons under the portrait and the band.

### Alignment and checks
`ALIGN` now also checks every divider: at the spec offset from its ring centre (0.6 px), 3 marks in a full column and none under the last portrait, centred on the column, inside the column body (6.7 to 79.4), at least 1.5 px from its own portrait and from the next, clear of the icon row (1 px), the same offset under all three; the negative control moves a mark 3 px and must be caught. Colours of every mark are checked against the kit set. Run: `bun run hud-style:check`.

### Evidence (round 5)
- `bun run hud-style:check` -> `ALL OK (layout problems: 0)`, 1120 PASS, 0 FAIL, 3 min 43 s (6 presets x 15 scenes incl. both divider scenes, ALIGN with the divider rules, negative controls, 28 hold checks, 6 exports). Fast path for the new work: `HUD_ONLY=dividers bun testbed/hud-style/check.ts` (about 25 s). `bun run typecheck` clean.
- Divider checks: 7 columns and 63 cells (9 states x 7 dividers) ALIGN clean; marks only in kit colours; mark moved 3 px is caught; Revised A defaults (Jost Regular, under, band) and `dv=` hash; export lists all seven dividers.
- Pictures (`briefs/046/`, git-ignored): `board-research-studies.png`, `board-research-principles.png`, `board-dividers-1x.png`, `board-dividers-2x.png`, `board-dividers-states-1x.png`, `dividers-states-2x.png`, `dividers-actual-2x.png`, `dividers-dock-2x.png`.
- Files: `testbed/hud-style/{tokens.js (DIVIDERS, divGeom), parts.js, sprites.js, scenes.js, app.js, hud.css, check.ts, shot.ts, research.html}`, `research-crops` (link to the git-ignored captures), `briefs/046/research/sources.md`. Nothing in Unity, sim or tools edited.
- Open for Latch when Sky picks: build the `under` column from the kit sprite by the recipe with the picked divider's `d2x`; draw the mark from `rosterSpec.column.under.dividers.<id>.mark`. Splice: `ALIGN`'s divider rules lift with the rest.

## Sky, 2026-10-22 (round 5 pick)
- **Divider: brass hairline** ("for now"). Then corrected: "I liked 5 the steel for the divider, but not the extra (more) rivets." Final: divider 5's chrome line without its two screws (id `steel`, replaces `screws`).
- "The more I look at that character plate, the more rivets seem to appear and few oddities like the blue side borders not really connected to anything." Clean the roster column itself: fewer rivets, and the side stripes either join the top and bottom caps or go.
- Sky, correcting: "I liked 5 the steel for the divider, but not the extra (more) rivets." Divider = 5's chrome (steel) line, without its two screws.

## Round 6 (Glyph, 2026-10-22): the roster column art cleaned, the steel divider
Sky: "the more rivets seem to appear and few oddities like the blue side borders not really connected to anything", then "I liked 5 the steel for the divider, but not the extra rivets".

### What was wrong on the kit column (audited on `roster_column@2x`, 218 x 833, and on the stretched copies)
| # | Oddity | Count / where | Fix |
|---|---|---|---|
| 1 | Rivets | **16** per column at 2x (and at 1x: each is a 4.7 px dot at 1080p, all visible): 8 on the bezels (left and right of every ring, so they crowd the face), 2 at each ring's top corners (rings 1, 2, 3) plus 2 at the foot = 8. The gaps were uneven: gap 1 and gap 2 had a pair, gap 3 none. The stretch recipe cuts at rows 223 / 412 / 601 / 789, just above the rivets, so **it duplicates none** (the stretched kit column also counts 16, checked); the column only looks like rivets keep appearing because the taller column shows them with air between. The old divider 5 added 6 more (2 per gap). | Bezel 8 removed, gap pairs 4 removed in all three; the 4 outer corners kept in A and B (they are the only ones the face plate needs: one pair at the top under the cap, one pair at the foot), removed in C. No divider rivets. |
| 2 | Side stripes ("blue borders") | Two seafoam stripes, 10 px (2x) wide, starting 10 px below the top cap and ending 9 px above the foot cap (rows 49 to 780): square-cut, floating in the body, with a dark channel and a pale hairline either side, and a nick at the bottom corners. The right one sits 1 px off the mirror of the left. | A and C: redrawn as an inlay from the sprite's own stripe profile, symmetric, running to the lip under both caps and clipped by the rim's rounded corners, so they end where the frame ends (joined). B: removed (the cap tabs are the only seafoam). |
| 3 | Key-colour spill | A lime-green line (about 1 px at 2x) round every portrait window, inside the cream mat; magenta/purple pixels along the outer silhouette (3272 and 1248 px fixed). It rings every portrait in the as-built roster ("green outline"). | Recoloured from the cream mat / neutral. |
| 4 | Seam under the foot rivets and tone banding in every gap | The repeated body row (222, lighter than the shaded rows beside each ring) showed as a lighter band, most at the foot just under the rivets. | Recipe `tallColumn/2` (below). |
| 5 | Cream caps with the teal tab | Checked: the tab is a flat block in the stripe family but unconnected; left as the kit's own (it is the only mark that says "cap"). With A / C the stripes now run up to the cap lip, so tab and stripes read as one inlay. | Not repainted. |
| 6 | Ring bezels | The bezels are slightly lumpy (hand-lit chrome), the right outer rim has a double line; unchanged (it is the sprite's character; "do not slim a prop"). | Not repainted. |

### Repainted or patched: patched, with provenance
Not a new image (cost 0). The kit sprite (`briefs/011/ui/kit/roster/roster_column@2x.png`, itself cut by `out/concepts/ui-011-kit/build1.py` from the Codex sheet `col4_cab`) is still the source of every ring, bezel, mat, cap, tab and body pixel; `testbed/hud-style/prep/roster_column.py` removes rivets (bezel: vertical interpolation across the rivet; body: inpaint from the plain face), clears the stripes, defringes, and redraws the inlay from the sprite's own stripe pixels. Same size, same ring centres, same cut rows, so every number above holds. Output: `testbed/hud-style/sprites/roster_column_{inlay,bare,rivetless}@2x.png` (and @1x), copied to `testbed/hud-style/export/sprites/` with `PROVENANCE.json` (source sha256, rivet positions found, pixels fixed, output sha256). Re-run: `python3 -I testbed/hud-style/prep/roster_column.py`; rivet count: `... --count <png>`.

### The three columns (token `roster.column`, panel Roster > Column art, hash `rc=`)
| | Rivets | Side stripes |
|---|---|---|
| As built (kit) | 16 | float, 10 px short of each cap |
| A `inlay` | 4 (outer corners) | run to both caps |
| B `bare` | 4 | none |
| **C `rivetless` (recommended, Revised A default)** | **0** | run to both caps |
Why C: the chrome rim and the bezels already say "built"; four dots in the corners are the only fixings left and they are what Sky keeps counting. A is one click away (`rc=inlay`) if the plate looks too bare. Dropped: an inlay that turns the corner under each cap as an L: drawn and tried, but the foot's L lies across the foot cut row (789), so the stretch smears it into a slab; a top-only L would be lopsided.

### Stretch recipe `tallColumn/2` (replaces /1; same cuts, same d2x per divider)
Each gap's block is no longer the one row 222 copied d2x times: it is the plain row 222 (smoothed sideways inside the body), tone-matched per channel to the gap's own edge columns (x 17..30 and 187..200, ratio of the mean of rows cut-1 and cut), eased into the row just above the cut over the first 6 rows and into the row at the cut over the last 6 (smoothstep); outside the body (rim, stripes, ring flanks) each row is a linear blend from row cut-1 to row cut. Export `rosterSpec.column`: recipe name, ease 6, edge columns. Latch: build the column from the picked sprite by this recipe.

### Divider: steel line
`steel` replaces `screws`: a 1 px chrome line (`--chrome` #cbc6c3) at 45 %, 54 px wide, centred, no screws; air above / below the icon row 2.5 / 2.5, 3 px to the next portrait: the same column height as the hairline (415.4 px, 38.5 %, d2x 55). It is Revised A's default (`roster.divider`); the other six stay menu choices.

### Evidence
- Command: `bun run hud-style:check` (full) and `HUD_ONLY=column bun testbed/hud-style/check.ts` (about 40 s): see the numbers in the reply. New checks: rivets per column by template matching (kit 16 = 8 + 8, A 4, B 4, C 0, bezel 0 in every cleaned one; the table in the lab agrees); `ALIGN` on the 4 + 4 column board (8 rosters) and on 4 x 9 state cells (36); Revised A defaults (rivetless, steel, Jost Regular, under); `rc=` hash; export lists the four columns.
- Pictures (`briefs/046/`): `board-roster-clean.png` (four columns at 2x and at actual size, rivet table), `board-roster-clean-states.png` (every state, 4 columns, 1x), `roster-clean-2x-top.png`, `roster-clean-2x-bottom.png`, `roster-clean-actual-2x.png`, `roster-clean-states-2x.png`, `roster-clean-dock-2x.png` (the recommendation in the dock).
- Files: `testbed/hud-style/{tokens.js (COLUMNS, steel), sprites.js (tallColumn/2, column variants), parts.js, scenes.js (roster-clean, roster-cleanstates), app.js, hud.css, check.ts}`, `prep/roster_column.py`, `sprites/`, `export/sprites/`, `export/*.tokens.json`. Nothing in Unity, sim or tools edited.
- Open for Latch: import `roster_column_rivetless@2x.png` (or the picked one) instead of the kit's `roster_column` and apply recipe `tallColumn/2`; the portraits' windows are unchanged (same alpha). Steel divider = a 54 x 1 px `--chrome` rect at 45 % at `rosterSpec.column.under.dividers.steel.mark`.
- **Column: B "bare"** (Sky, 2026-10-22: "I think B for the roster"): 4 rivets at the outer corners, no side stripes. Revised A's roster = icons under the portrait, steel divider (no screws), column `bare`, one weapon icon (drawn/holstered), Jost Regular.
