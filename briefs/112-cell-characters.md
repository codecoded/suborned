# 112 · The cell from the Happy Mouse bundles

Owner: one session end to end (2026-11-12), in Forma's and Latch's ground. Next owner: Sky (play it), then Aster (merge).

Sky, 2026-11-12: "we are wanting to replace our main cell with these characters and animations. find what's suitable, do the best you can to swap them and use appropriate animations."

## What it is
The four cell members are now the Happy Mouse Labs game bundles (`~/Projects/the-suborned-store/characters/cell_hml/`, four ZIPs, sha256 pinned in `tools/figures/cell_hml.json`). Each keeps its own mesh, textures, skeleton and clips. Nothing is decimated or resized: compression goes through Sky.

- **Build:** `bun tools/figures/cell_hml.ts --install` (about 30 s). It unzips the bundles, converts each in Blender (`cell_hml.py`, `cell_hml_clips.py`), writes `briefs/112/people/manifest-cell.json` and copies the figures over the stand-ins in `unity/Assets/Suborned/People/Figures/<id>` (git-ignored). The same bytes every build.
- **Unity:** `FigureBuild.CellOverrides` reads that manifest. Each figure is Generic on its own skeleton and plays its own clips through an override of the shared controller. A figure whose installed file is not the manifest's build keeps its stand-in, with a warning, so a tree without the install still runs.
- **Props:** the pistol, the carbine and the Tech's visor are part of each figure and show by state, as the bundle lists them per action: the gun only in the armed states, the visor always (`HostViews.Cell`).
- **Superseded:** the testbed cell's builder (`tools/figures/cell_generic.py`, brief 020) is out of the repo; its manifest is no longer read. A copy is in `out/archive/112-cell-characters/` of the main checkout, with the session's one-off helpers.
- **Check:** `CellSheet112` (Unity batch mode) plays every state of every figure through the game's own controller and writes stills and `cell.json`. `tools/figures/cell_hml.test.ts` checks the manifest against the spec, the rulebook's speeds and the states the host uses.

## Which action plays where
The full map is `tools/figures/cell_hml.json`; the manifest lists every clip with its action, length, loop flag and props.

| The game's state | The bundle's action |
|---|---|
| idle, idle_alt | Idle |
| walk, walk_f | Walk (the Scout: the bundle's brisker `Walking` clip, see limits) |
| run, run_lean | Run |
| walk_armed, run_armed | Walk Armed, Run Armed |
| aim, aim_pistol | Aim Armed |
| shoot | Fire Armed |
| draw | Draw, re-timed to the 2.0 s the host expects |
| cover_in, cover_out | Cover In, Cover Out |
| cover_kneel | Crouch Aim Armed (the Enforcer: Cover Idle) |
| cover_fire | Crouch Fire Armed |
| cover_pin | Crouch Armed |
| cower | Duck (holds its last frame) |
| hit, hit_large | Hit, Hit Large |
| lying | Lying Hurt |
| sit, sit_alt, sit_talk | Sit Idle, Sit Idle, Sit Talk, slid onto the café chair |
| carry_shoulder, carry_shoulder_idle | authored from the figure's own walk and idle: the right arm round the carried legs |

States only the cell has, picked by `HostViews.CellState`:

| When | State | Action |
|---|---|---|
| gun out, standing, not aiming | idle_armed | Idle Armed |
| gun out, walking | walk_armed | Walk Armed |
| the engine is holstering | holster | Holster |
| watching | look_around | Look Around |
| the Tech, a loop or a reroute running or held | tinker | Panel Tinker |
| the Scout misleading | mislead | Mislead |
| the Fixer on a call | phone | Phone Talk |

Also in each figure, with no host hook yet: reload, interact, nod, shrug, reassure, argue, wait, dodge, death, drive, enter_car, exit_car, and per role persuade (Fixer), keypad, gadget, panel_open, whisper (Tech), takedown, restrain, carry, enter_door, cover_stand, peek_left, peek_right, raise_aim (Enforcer).

## Fixed in the conversion
- **Feet on the floor:** the bundles' clips have no foot contact. The Fixer's soles went up to 9 cm under the ground as she crouched or ran. Every standing clip is now lifted frame by frame so no sole is under the floor, and dropped if it floated.
- **The seat:** the bundle's sit clips have no chair. Each is slid so the hip joints rest on the café chair's cushion; where the legs are longer than the chair is high the feet stay on the floor and the seat is up to 8 cm proud of the cushion.

## Known limits
- **Weight:** about 29,000 to 31,000 triangles each in one level, against the stand-ins' 1,500 to 2,000 at play zoom. Textures are 2048 px, the Tech's body and the Enforcer's carbine 4096 px. On the Studio at 1920x1080 the play view costs the same within noise (stand-ins 3.85 and 3.95 ms a frame, the new cell 3.84 and 3.98 ms; the smoke's frame window, two runs each). The Air is not measured.
- **Walks:** the cell walks at 1.8 m/s. The Scout and the Enforcer fit (their walks play at 1.25 and 1.21 times their speed). The Fixer's and the Tech's walks are strolls of about 1 m/s and play at the 1.8 times cap, which reads hurried. They need a brisker walk in their bundles.
- **The Scout's walk** is the bundle's `Walking` clip, not her Walk action (`Walking (2)`, 0.92 m/s, which would play at 1.96 times). Her Walk action is kept as `walk_stroll`.
- **Portraits:** unchanged. The HUD's stills are the same four designs, so they match the new figures.
- **The Fixer sits side-saddle:** her Sit Idle has both legs to her left. It is the bundle's pose.
- **Takedown and restrain** are single-person clips; the host's takedown is unchanged.
- **Fingers:** the authored carry leaves the carrying hand open (the rigs name their fingers differently).
- **Additive layers** (the flinch, the kick, the peek) are Humanoid clips and do nothing on these rigs.
- **The bundle's own notes:** Cower does not loop cleanly; the Enforcer's mesh has one shading seam.

## Done-check
1. `bun tools/figures/cell_hml.ts --install` runs clean and a second run gives the same sha256.
2. `bun test tools/figures/cell_hml.test.ts` passes.
3. `CellSheet112`: no import message, every state has a clip, the props are found, the soles are within 3.5 cm of the floor in the standing states.
4. A game build's smoke passes with the cell drawn.
5. Sky plays it.

## Result, 2026-11-12
- A scratch build of this branch (`out/build/cell112`, no build number spent): the full proof ran every section, status ok, no Exception in the player's log.
- Seven rows fail: `r29 fire busy (act ending within 2 s)`, `r123 fight safe`, and r083's `r085partnerOut`, `r085scoutSideOn`, `r085listenFrame`, `r085viewBack` and `r085sitClips`. They are not the figures': the same branch built with the stand-ins put back fails the same seven and passes the same 143 (both runs alone, the window focused throughout). They are this commit's, with engine 0.57.0; brief 110's proof passes them in the shared tree with changes not yet committed.
- The first full run also failed `r085tpLeftTap`, `r085clipOut`, `r085clipTopSpeed` and `r085clipFindsHim`: another proof was running at the same time and the window lost focus eight times. Alone, they pass.

## Evidence
- `briefs/112/people/manifest-cell.json` (committed).
- Stills and `cell.json`: `out/scratch/claude/cell112/` in the tree that ran the check (git-ignored).
