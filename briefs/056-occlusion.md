# Brief 056 (occlusion): camera occlusion, research and six live options

Sky, 2026-10-24: "I hate our camera occlusion logic in place." Earlier (2026-10-21): "not into our main fade - was gonna come back to that."
Number note: `briefs/056-unity-features.md` already holds 056 and shares the folder `briefs/056/`; this brief's files are `briefs/056/board-options.png`, `shots/` and `research/occlusion-sources.md`. Aster may renumber.

- **Owner:** Glyph (options, recommendation). Port to the game: Latch (shader and host), with Glint for the look of the rim.
- **Done-check:** Sky plays the six options in the playground at all four turns and picks one (or a pair); only then does Latch port the behaviour. Nothing in Unity, sim or tools was changed.
- **Evidence:** `briefs/056/board-options.png` (6 options by 4 yaws); `briefs/056/shots/<option>-yaw<30|120|210|300>.png` (1920x1080, the squad in the street); `briefs/056/shots/circle-yaw30-2x-crop.png` (retina crop); research in `briefs/056/research/occlusion-sources.md`; code in `testbed/controls/occlusion.js`, `world.js`, `app.js`, `index.html`, `style.css`; the shooting script `testbed/controls/shoot-occ.ts`. Checks run: `bun testbed/controls/shoot-occ.ts` loaded the page at 24 option and yaw combinations, 1x and 2x, third-person once; console errors: none. I did not run `shoot.ts` (the older control checks) or the Unity build.
- **Next owner:** Sky (play and pick), then Aster, then Latch.

## What is wrong today (read from the code and the 044 reviews)

`ArchitectureFade` + `DK_Cutaway.hlsl` cut a **world-space disc on the ground**: 11.2 m radius at zoom 14 round the screen centre, 4 m at the cursor and target, 3.2 m clean round each person, and `column` mode ghosts everything above a ghosted fragment. A tower in front hides a strip of street behind it, but the thing that is actually in the way of a person is a thin tube along the sight line. So the cut is far bigger than the need (the vertical column and screen), the ghost is a hard 4x4 Bayer dither with a 2 m feather (a screen door; the grey slab was the veil), and the same code decides for every yaw, so yaw 210 reads and yaw 30 does not.

## Research in one paragraph

First-hand material was thin (see the sources file for what is sourced and what is memory). What is sourced: BG3-style local cutout round the character (sphere mask, smoothed in time, ground kept apart); DOS2 outline of the figure behind the obstruction (a player's word); Diablo-style raycast fade, wall split in a low part that stays and an upper part that fades; dissolve shaders with their depth and shadow caveats; Sims 4 cutaway as a player-chosen wall mode with known glitches; XCOM 2 floor switching and Q/E turns; Commandos: Origins rotating to reveal plus a separate X-ray key (a player calls it a crutch); Shadow Tactics' moderator saying the aim is to reduce the need to rotate and improve clickability; Syndicate Wars still having skyscrapers in the way despite rotation; Satellite Reign moving the camera aside, not fading; Disco Elysium and Hades solving it in the art. Readers' complaints cluster on three things: the player character lost behind scenery, clicks blocked by what you cannot see, and cuts that look like glitches (V-shaped, floating pieces).

## Principles for The Suborned

1. **Cut what is in the way, not what is near.** The blocking set is the tube along the sight line to a person. Cut that, in screen space, only where the wall is nearer than the person. Never a ground disc, never a column.
2. **Keep the building as a mass.** Look: cream stone, seafoam bands, chrome, a skyline. Whole-building ghosts and dither screens make the city look broken. A cut should read as a deliberate port: soft edge, a faint cream rim, no grey slab, no hard-edged column. At 2x the noise is fine enough to read as a soft edge (see the crop).
3. **People are never lost.** Anyone who matters (the four agents, hostiles, key people) keeps a flat fill with a cream edge where something nearer covers them: a backstop for when the cut cannot reach (a thick wall, a person beyond the radius, a squad in a line). Crowd extras do not get it.
4. **The cut follows what the HUD follows.** Selected agents, then the target, then the cursor's ground point, then the rest: the list `HostTurn.FadePoints` already builds.
5. **Ground, doors and shopfronts do not fade** (the existing rule) and what a person stands in front of stays whole.
6. **Turn first, fade second.** Players expect rotation (Q/E, Commandos, Shadow Tactics, XCOM); do not make the fade do the camera's job. Yaw 210 for the Records Office front stays.
7. **Ease in time** (about 0.15 to 0.25 s, floors in steps) so nothing flickers as an agent walks; a cut that is on for under a quarter second should not appear.
8. **What you see is what you can hover.** A person seen through a cut or as a silhouette can be hovered and clicked; one fully hidden cannot (Shadow Tactics' blocked-click complaint). Rings and markers already draw over walls.
9. **No new colour.** Rim cream, silhouette fill in the agent's own roster colour; cones, guard and rivals keep their cues. Nothing owns a hue.

## The six options (playground)

Run `bun run controls` and open http://localhost:5173/ (the script's port; set `PORT=...` if it is taken). The **Occlusion bar** is top right (always on, not under Settings): A to F, plus **Silhouettes** By option / On / Off. `O` steps through the options, Shift+O back. `?occ=circle&yaw=30` in the URL sets the start. Q and E turn the camera as before; T for third person works with every option. The scene now has tall buildings on all four sides: the Records Office 19 m (north), a tower across the road 22 m (south), the cafe 16 m (west), a block closing the east end 26 m. **To see the 120 turn, walk the squad to the east end of the street** (x 14 to 20); the 300 turn works near the cafe.

| | what it does | looks | worry |
|---|---|---|---|
| A Column dither (today) | A port of `DK_Cutaway.hlsl`: ground discs, column mode, ghost 0.2, Bayer 4x4 | the vertical screen of stipple, hard bands; at yaw 30 half the frame | the thing Sky hates |
| B Silhouettes only | no fade; people drawn again, cream edge, role fill, wherever a wall or prop is nearer | street stays hidden, only people show | you cannot see doors, props, cones' ground or what the Scout is meant to watch |
| C Soft circle cut | a soft round hole round each person (head to feet) and the cursor, only through walls nearer than them, cream rim | the BG3 look; the city stays whole | squads in a line merge into one big hole; holes over a far window can show a dark inside |
| D Whole building + edges | the blocking building goes to 13 % with 3D edge bars | clear street, loud look; dark edge bars are heavy | loses the mass and the skyline; flips on and off as people move |
| E Floors come off | storeys above the sight line to anyone come off, floor by floor, ground floor stays, a cream cap marks the cut | the cleanest street; it reads as architecture | near a front wall the whole building drops to one storey; the skyline in front vanishes |
| F Circle + silhouettes | C with B | people always readable, city whole | none new |

Known lab limits: perspective camera 38 m away, so buildings are bigger on screen than in the ortho game; cones are drawn on top of walls (the lab's choice, not part of this); silhouettes also show behind props such as the crate; the same code works in third person but I only looked at one frame.

## Recommendation

**F: the soft circle cut with silhouettes, as the one rule for every yaw, and keep E as an optional second layer for tall blocks.**

- It answers the complaint directly: no column, no screen, no grey slab; the cut is local, soft and sized to a person. It keeps the bright city whole, which the look needs.
- The silhouettes cover the cases a cut cannot (a wall too thick to cut, a person outside the radius, a squad of four) and make the game never lose a person, with no new hue.
- It needs no per-building logic and works the same at 30, 120, 210 and 300, which is where A fails.
- Weakness to watch: four agents close together make one large hole (see C, yaw 30). If Sky dislikes that, add **E for that building only** when holes cover more than a third of its visible face: the building drops by storeys to its ground floor. I would test that blend in play before building it.
- D is the worst fit with the look; A should go.

Port notes for Latch (no change made): replace `DKCutDisc` with a screen-space capsule per point (head and feet projected, radius about 2 m at the person's depth, `smoothstep` from 0.55 r to r), kept only where the fragment's eye depth is nearer than the person's by 0.45 m (works for the ortho camera with linear eye depth); keep the existing point list, the layer tag and the keyword; use finer noise than Bayer 4x4 (interleaved gradient noise looked soft at 2x) and a small cream lean on the rim; ease with `fadeTime`. Silhouette pass: a URP Render Objects feature on the people layer, depth test Greater, flat colour, drawn before the people's own pass (so other people never silhouette). Ghost shadows and the shadow-pass cut stay as built.

## Open questions for Sky

1. F alone, or F plus the floors blend (E) for tall blocks?
2. Do silhouettes show for hostiles and key people too (as now), or only for your four agents?
3. Does the rim help or look like a decoration? (`occVeil` in `occlusion.js`; easy to drop.)

## Sky's calls (2026-10-25)
Sky, 2026-10-25, on the outstanding-work summary that listed each pick: "Your recommendation seem good".
1. **F** (the soft circle cut with silhouettes) as the one rule for every yaw. E (dropping a tall block by storeys) is tried in play only if four close agents make holes over a third of a building's face; not built before that.
2. and 3. Not answered: silhouettes stay as now (hostiles and key people too), and the rim stays until Sky sees it in play.
Next: Latch ports F (port notes above), Glint the rim's look; Glyph reviews the 2x crops.
