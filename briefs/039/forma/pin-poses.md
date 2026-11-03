# 039 B, cover and pin: the pose proof (Forma, 2026-10-26)

Sky picked B. Glyph's worry: does B read from the body alone at play zoom? On the simplified board the squatter behind a 1 m hedge showed 0 px, and a kneeler and a ducker in the open were 7 px apart. This is the proof, made in Blender only. No build was touched: nothing went into `People/`, and clip metas, `clips.json`, `unitymeta.py`, `FigureBuild.cs` and `author_clips.py` are unchanged.

**Update, later on 2026-10-26:** Glyph passed the proof (`briefs/039/glyph/b-final.md`), and the clips of section C are now authored in `tools/figures/author_clips.py` and listed in `tools/figures/clips.json`. The proof was re-rendered from the authored clips (section D, `briefs/039/forma/authored/`). Nothing is installed in `unity/Assets/Suborned/People/`. Section D is Latch's install note.

## Pictures (open first)
- `briefs/039/forma/pin-poses-sheet.png`: the sheet. One row per state, with the six real figures (scout, fixer, tech, enforcer, rival, guard) at the play camera (zoom 14 at 38°, yaw 30; 1080 / 28 px per m). Three settings: in the open; behind a 0.95 m hedge at 0.85 m (the cover points' stand-off, `tools/cover/gen.ts` standoffCm); and snug at 0.45 m (the worst case). Shown at 2x nearest, plus a true-size column. Under each strip is each man's visible area and height in px.
- The loops (GIF at 2x, MP4 at 3x, 12 fps, 3.6 s, with a timeline under them: red = a round lands near him, cream = he fires, bar = the pin):
  - `pin-loop-cover.gif` / `.mp4`: in cover, not shot at
  - `pin-loop-fire.gif` / `.mp4`: rises out of cover, two shots, back down
  - `pin-loop-pin_cover.gif` / `.mp4`: pinned in cover. Two rounds land, he ducks, flinches, peeks as the pin runs out, then lets go
  - `pin-loop-pin_open.gif` / `.mp4`: pinned in the open. Aims, then a round drops him into the duck, he flinches, holds and stands back up at the pin's end
- `pin-poses-counts.json`: every count on the sheet, per figure.
- How I chose: `pin-candidates-rival.png` (+ `.json`), the candidates on the rival including Glyph's squat (`Crouching_Idle`), measured the same way. `pin-survey-a.png` and `pin-survey-b.png` are contact strips of 19 library clips.
- Scripts in `out/scratch/forma/pin-poses/`:
  - `pin_render.py`: a copy of `tools/figures/proof.py`'s light and retarget, plus clip frames per step, an upper-body layer with a weight, the hedge and a mask pass;
  - `adjust.py`: the authored additive layer;
  - `states.py`: the four states, timed;
  - `loops.py` and `outputs.py`.
  - To rerun: `bun run guard python3 -I out/scratch/forma/pin-poses/loops.py`, then `python3 -I out/scratch/forma/pin-poses/outputs.py briefs/039/forma` (about 3 min).

## What I changed, and why it fixes the hedge case
The squat failed because it was a different, lower body: 0.95 m, so behind a snug hedge he was gone (rival: 60 px², 6 px). **The pin now keeps the kneel's legs and changes only the upper body.** On the first round he curls over with his hands over his head (the library's crouched duck, `Ducking__c9c8aef4`, its upper body at frame 22). He is never lower than the cover's own top, so a hump of back and elbows always shows over it. Four things carry the state on the body:

1. **Shape and colour of what shows over the cover.** In cover he shows his head, face and gun hands, upright. Pinned, he shows a wide hump of jacket and elbows, with no face. Firing, he rises with his arms out. They are three different outlines in the same place.
2. **The drop.** A round lands, and in 0.25 s he goes from head-up to hump.
3. **A flinch per round** (additive, 0.05 s in, about 0.3 s out): the spine curls 14° + 8°, the head 15°, the shoulders come up 6° and he drops 5 cm. Every round that lands visibly hits him, even behind the hedge (−1 to −2 px of height and −19 to −83 px² of sliver at true size). Chips (Glint) and the "Down" bark (Chime) land on the same frame.
4. **The peek is the pin's timer.** From 1.2 s after the last round, over 0.6 s, the head comes up (spine −22°, neck −15°, head −20°) and the face shows again. At the pin's end the duck lets go in 0.3 s. So "still pinned" and "nearly free" show on the body, which answers "pinned for 2 s and still there".

Firing from cover uses the library's kneel-to-stand to its frame 18 (a half-rise), played at double speed, with `aim_pistol` on the upper body and a kick per shot. It is 6–8 px taller than the kneel and shows the chest and arms over the cover. Pinned in the open stays the build's `cower` (frames 10→47, held) with the same flinch, and stands back up when the pin ends.

## The counts (true size, 1 px = 1 px of a 1080p frame; the range over the six figures; heading 195, facing the shooter across the hedge)

| state | open: tall px | 0.85 m hedge: visible px² / tall | 0.45 m hedge: px² / tall |
|---|---|---|---|
| stands, aims | 51–56 | n/a | n/a |
| in cover: kneel | 45–48 | 364–594 / 27–32 | 216–388 / 18–23 |
| fires from cover | 52–56 | 451–696 / 34–39 | 349–524 / 25–30 |
| pinned in cover: held | 38–42 | 359–533 / 22–24 | 210–330 / 13–15 |
| pinned in cover: a round lands | 36–39 | 318–495 / 20–23 | 174–305 / 11–14 |
| pinned in cover: peeks | 42–44 | 375–618 / 24–26 | 227–410 / 15–17 |
| pinned in the open: held | 38–41 | n/a | n/a |
| pinned in the open: a round lands | 32–38 | n/a | n/a |
| (Glyph's squat, rival, for comparison) | 26 | 261 / 15 | **60 / 6** |

Glyph's done-check, per figure in the open:
- **Squat (now the pinned hunch) ≥ 10 px below standing: pass**, 11–18 px.
- **Duck ≥ 15 px below standing: 4 of 6 pass.** It is 11–18 px held: the tech is at 11 and the guard at 14. A round landing takes it to 16–24 for all six.
- **Kneel ≥ 10 px below standing: fails at this heading.** It is only 6–8 px, though facing the camera it is 10–11 (rival 58 → 48, guard 60 → 49, `out/scratch/forma/pin-poses/tune_*.json`). That doesn't hurt B: the kneel is told from standing by the cover in front of him, and from the pin by its outline.

Stagger and flinch against aim were not re-rendered here: they are Glyph's build and library clips, already on `board-poses-simple.png`.

**The hedge sliver is never zero.** Pinned behind the snug hedge is the worst case, and still at least 174 px² (11 px tall) on the slightest figure in mid-flinch. At the stand-off the sim actually uses (0.85 m), the 38° camera looks over the hedge and the pinned man shows 318–533 px², as much as the kneeler.

## Verdict
**B reads on poses alone, and I would not add the chevron disc now.** In cover, firing and pinned are three outlines over the cover, in the same spot. A pinned man always shows a sliver, and it moves on every round. The pin's 2 s show as the peek and the release.

The weak pair is **kneel against pinned behind cover as a still**. Their areas are the same, and the pinned man is only 5–8 px lower. The read is the outline (face up against a hump), not the size. A player glancing at one of eight fighters will catch the drop and the flinch, and may miss a held hump. That should be judged on the fight range in motion. If it fails there, add the disc for the cell's own pinned agents first: the player needs to know his man is stuck, and a rival's pin is the player's own doing, which he just watched.

Limits of the proof:
- It is a Cycles render with `proof.py`'s fitted light over the real road patch, not Unity's pixels.
- The hedge is a box stand-in (0.95 m, the cover rule's 0.9 m minimum plus a shrub top), with its long side square to the view: the most it can hide. The real planters' shrub bumps would show more.
- There are no muzzle flash, chips or sound in the loops (Glint and Chime), so the body is all there is.

## Install list for Latch (when it's time; nothing is installed)
A. In the build already: `aim_pistol` (also as the fire upper body), `cower` (pinned in the open; play frames 10→47 in 0.9 s, hold, reverse in 0.45 s at the pin's end), `hit`, `hit_large`, `kill_victim_back`, `lying`.

B. From the library, new `clips.json` rows (Forma adds them once Latch is free):

| clip | source | loop | use |
|---|---|---|---|
| `cover_kneel` | `Pistol_Kneeling_Idle__c9cdc22d` | yes | in cover, not shot at |
| `cover_in` | `Pistol_Stand_To_Kneel__c9cdc166` | no | arriving at a cover point (1.0 s) |
| `cover_out` | `Pistol_Kneel_To_Stand__c9cdc2f1` | no | leaving cover |

C. To make (authored in `tools/figures/author_clips.py` as a new `layer` kind: a donor clip kept bone for bone on the legs, the upper body from a second clip, plus fixed additive turns about the figure's own axes; the prototype is `out/scratch/forma/pin-poses/adjust.py` and `states.py`):

| clip | how | loop | use |
|---|---|---|---|
| `cover_pin` | legs `Pistol_Kneeling_Idle`, upper `Ducking__c9c8aef4` frames 22→24.5→22 (a 0.5 s shiver) | yes | pinned in cover (crossfade from `cover_kneel` in 0.12 s, out in 0.3 s at the pin's end) |
| `cover_fire` | legs `Pistol_Kneel_To_Stand` frame 18 held, upper `aim_pistol` | yes | firing from cover (crossfade 0.3 s up, 0.3 s down) |
| `flinch_add` | additive, 0.4 s: Spine pitch +14, Spine1 +8, Head +15, shoulders up 6, Hips −5 cm; 0.05 s in, decay τ 0.16 s | no | every round that lands near a pinned man, in cover or in the open (an additive layer, restarted per round) |
| `kick_add` | additive, 0.15 s: Spine1 pitch −7, Head −4, RightArm −10 (muzzle climb); 0.03 s in, τ 0.09 s | no | each shot from `aim_pistol` or `cover_fire`, the sidearm's missing kick |
| `pin_peek_add` | additive pose: Spine pitch −22, Neck −15, Head −20 | pose | weight driven by the host: 0 until 1.2 s after the last round, up to 1 over 0.6 s, back to 0 when the pin ends |

The host needs:
- an additive layer for `flinch_add`, `kick_add` and `pin_peek_add`;
- the pin's remaining time, to drive the peek weight;
- the kneel point on the far side of the cover from the shooter (Latch and Keel).

No Avatar Mask is needed if `cover_pin` and `cover_fire` are baked as above.

## Next owner
Glyph judges the read: the sheet first, then `pin-loop-pin_cover.gif` against `pin-loop-cover.gif`. Done-check: in the hedge rows, can he tell kneel, pinned and firing apart at true size, and does the flinch read? If yes, Forma authors section C and Latch installs A to C after his current build. If not, Glyph adds the disc for the cell only, and the range test decides about rivals.

## D. The authored clips, and Latch's install note (Forma, 2026-10-26)
Glyph passed B on the body (`briefs/039/glyph/b-final.md`). The clips of B and C are now in `tools/figures/clips.json`, made by `tools/figures/clips.py`, with two new kinds in `tools/figures/author_clips.py`:
- `layer` (`make_layer`): a donor clip on the hips and legs, the upper body from a second clip. The library clips share one rest pose, so each bone's local rotation copies straight across. Optional `turns` and `reach` come after.
- `additive` (`make_additive`): one donor frame held, plus turns about the figure's own axes. Frame 0 is the untouched reference pose.

**Nothing is installed.** The generated build `out/people-models/People` was refreshed (clips and manifest, 59 clips), so `tools/figures/sway.test.ts` holds the new metas. Note that any `bun tools/figures/run.ts --install` from now on carries these 8 clips into `People/`, because it rebuilds from `clips.json`. Until the host change below lands, `FigureBuild` would only add them as 8 unused base-layer states, which does no harm.

### Pictures
`briefs/039/forma/authored/`: `pin-poses-sheet.png`, `pin-poses-counts.json` and the four loops (`pin-loop-*.gif` / `.mp4`). They come from the authored FBX files, played the way the host will play them: base-layer cross-fades (linear weight) plus additive layers (the clip less its frame-0 reference, times the weight), with no hand-made adjustments. The scripts are in `out/scratch/forma/pin-clips/`: `states.py` holds the timings below, `pin_render.py` does the layer and additive playback, then `loops.py` and `outputs.py`. To rerun: `bun run guard python3 -I out/scratch/forma/pin-clips/loops.py`, then `python3 -I out/scratch/forma/pin-clips/outputs.py briefs/039/forma/authored`. The clips come from `People/Clips` in that folder (`clips.py` on a filtered `clips.json`).

### The counts, authored against the proof (true size; the range over the six figures; heading 195)
| state | open: tall px (proof) | 0.85 m hedge: px² / tall (proof) | 0.45 m hedge: px² / tall (proof) |
|---|---|---|---|
| stands, aims | 56–60 (51–56) | n/a | n/a |
| in cover: kneel | 45–48 (45–48) | 364–594 / 27–32 (same) | 216–388 / 18–23 (same) |
| fires from cover | 53–56 (52–56) | 442–707 / 35–39 (451–696 / 34–39) | 340–535 / 26–30 (349–524 / 25–30) |
| pinned in cover: held | 39–42 (38–42) | 358–532 / 22–24 (359–533 / 22–24) | 209–335 / 13–15 (210–330 / 13–15) |
| pinned in cover: a round lands | 36–41 (36–39) | 341–516 / 19–23 (318–495 / 20–23) | 189–325 / 10–14 (174–305 / 11–14) |
| pinned in cover: peeks | 42–44 (42–44) | 379–623 / 24–26 (375–618 / 24–26) | 231–414 / 15–17 (227–410 / 15–17) |
| pinned in the open: held | 38–41 (38–41) | n/a | n/a |
| pinned in the open: a round lands | 34–39 (32–38) | n/a | n/a |

The counts still pass:
- The hedge sliver is never zero: at least 189 px², 10 px tall, snug and in mid-flinch.
- Kneel, pinned and firing keep their proof outlines and heights to within a pixel.
- Glyph's open checks, against standing. Standing is now the full-body `aim_pistol`; the proof had the aim on the cower's legs, 4 px shorter:
  - pinned in the open is 16–22 px lower: the duck check (≥ 15) passes on all six;
  - kneel is 9–12 px lower: the kneel check (≥ 10) passes on four of six, with the scout and the rival at 9. As in the proof, the cover in front of him carries the kneel.

Two differences from the proof, both by design:
- **The flinch has no hips drop.** In an additive layer a hips drop would sink the planted knee and feet, so the flinch is rotations only: Spine +14, Spine1 +8, Head +15, shoulders up 6. The height still drops 1–3 px per round on every figure, but the sliver shrinks less at the 0.85 m hedge (−5 to −81 px², against −19 to −83). The guard (−5) and the fixer (−17) move least, so the flinch reads by its jolt, not by its area. Glint's chips and Chime's "Down" land on the same frame.
- **cover_fire squares the shoulders and straightens both arms (Glyph's ask).** The library aim is bladed: the left shoulder is 18 cm forward and the left elbow bent 62°. The authored clip turns the chest 30° to square it to the cover, with the neck and head turned back so the head keeps its line. Both arms reach a two-handed grip at shoulder height, with both elbows at about 16° and forearms within 3° of level (measured on the clip). At heading 195 the forearms point at the camera, so they are foreshortened: the rise is the same 7–8 px as the proof, and the area gain over the kneel at the 0.85 m hedge is +43 to +116 px² (proof +31 to +129; scout +43, fixer +78). Side-on headings are where the forearm line shows longest. The range checks that, with Glyph's other-headings case.

### The clips
Each row lists frames at 30 fps (Unity's lastFrame + 1), loop, and Root Transform Position (XZ) Bake Into Pose (`bakes_xz`: every clip here stays in place, so all bake XZ). All have `feet: true`, so height comes from the feet.

| clip | made from | frames | loop | layer | use |
|---|---|---|---|---|---|
| `cover_kneel` | library `Pistol_Kneeling_Idle` | 115 (3.8 s) | yes | base | in cover, not shot at |
| `cover_in` | library `Pistol_Stand_To_Kneel` | 31 (1.0 s) | no | base | arriving at a cover point (hips move 7 cm, stays in place) |
| `cover_out` | library `Pistol_Kneel_To_Stand` | 42 (1.4 s) | no | base | leaving cover |
| `cover_pin` | `layer`: legs `Pistol_Kneeling_Idle` 1–115, upper `Ducking` frame 22 plus a 2.5-frame shiver ×8 (0.48 s) | 115 | yes | base | pinned in cover: the hump, no face |
| `cover_fire` | `layer`: legs `Pistol_Kneel_To_Stand` frame 18 held, upper `Pistol_Aim` 1–214, chest squared 30°, both arms straight out | 214 (7.1 s) | yes | base | firing from cover |
| `flinch_add` | `additive` on the kneel: Spine +14, Spine1 +8, Head +15, shoulders ±6; up in 2 frames, τ 0.16 s, eased to 0 by 0.5 s | 16 (0.5 s) | no | additive | one per round that lands near a pinned man (in cover or the open) |
| `kick_add` | `additive` on `Pistol_Aim`: Spine1 −7, Head −4, both upper arms −10 (muzzle climb); up in 1 frame, τ 0.09 s, 0 by 0.3 s | 10 (0.3 s) | no | additive | one per shot (from `aim_pistol`, `cover_fire` or the Enforcer's one-handed fire) |
| `pin_peek_add` | `additive` on the kneel: Spine −22, Neck −15, Head −20, held | 11; the clip plays frames 1–10 | yes | additive (weighted) | the head comes up as the pin runs out |

`kick_add` turns both upper arms, not only the right as section C had it, so the two-handed grip doesn't split on a shot.

The additive clips have **Additive Reference Pose on, frame 0**, written by `unitymeta.clip_meta(additive_ref=...)` from `clips.json` `additive` / `refPose`. In `pin_peek_add`, frame 0 is the reference and the clip itself is frames 1–10 (`first: 1`), so it adds the full peek at any time. Each manifest row says `additive: true`, so `FigureBuild` can tell them apart.

### Host (Latch)
`FigureBuild` today puts every manifest clip on layer 0. For B:
1. **Layer 0, base (as now):** add `cover_kneel`, `cover_in`, `cover_out`, `cover_pin` and `cover_fire` as states, like any clip. They are full-body clips, so no Avatar Mask is needed.
2. Leave rows with `additive: true` off layer 0, and add **two additive layers** (blending Additive, no mask):
   - **"react"**, weight 1: a default empty state, plus `flinch_add` and `kick_add`. Each has an exit transition back to empty at exit time 1 with no blend. The host restarts the clip on each event with `Play(state, react, 0)`. Two triggers:
     - a round lands near a pinned man (in cover or in the open, so over `cover_pin` and `cower` alike): `flinch_add`;
     - he fires a shot: `kick_add`, on the muzzle flash's frame.
   - **"peek"**, weight driven by the host every frame with `SetLayerWeight`: one looping state, `pin_peek_add`. The weight is 0 until 1.2 s after the last round, rises to 1 over 0.6 s (smoothstep), and falls to 0 over 0.3 s at the pin's end, with the duck. Outside `cover_pin` it is 0.
3. **Cross-fades** on layer 0 (`CrossFadeInFixedTime`, in seconds):

| from → to | when | fade |
|---|---|---|
| walk/run (armed) → `cover_in` | reaches the kneel point | 0.15 |
| `cover_in` → `cover_kneel` | at its end (1.0 s) | 0.1 |
| `cover_kneel` → `cover_pin` | the first round lands near him (the drop) | 0.12 |
| `cover_pin` → `cover_kneel` | the pin ends | 0.3 |
| `cover_kneel` → `cover_fire` | he fires from cover | 0.3 |
| `cover_fire` → `cover_kneel` | the fire order ends | 0.3 |
| `cover_fire` → `cover_pin` | a round lands while he is up | 0.12 |
| `cover_kneel` → `cover_out` | he leaves cover | 0.1 |
| `cover_out` → walk/idle | at its end (1.4 s) | 0.2 |
| `aim_pistol` → `cower` (frames 9→46 in 0.9 s, hold) | pinned in the open, as built (A) | 0.1 |
| `cower` reversed (0.45 s) → `aim_pistol` | the pin ends in the open | 0.25 |

4. Import checks:
   - No "translation animation discarded" warnings: only the hips carry translation, as in the existing authored clips.
   - The import preview of `pin_peek_add` shows the plain kneel at frame 0.
   - The authored clips' FBX rest is their first frame, as it already is for `sit` and `serve`; the clip's own avatar is fitted from it as before. If Unity complains about the avatar for `cover_pin` (its rest is the hump), tell me and I'll export the library's T-pose rest instead.

### What Keel supplies (the host reads it, nothing drawn)
- the pin per person: whether he is pinned, the time of the last round that landed near him, and the pin's remaining time (it drives the peek weight and the release);
- a "round lands near him" event per round (the flinch, Glint's chips, Chime's bark) and a "he fires" event per shot (the kick and Glint's flash);
- the kneel point per cover point, on the far side of the prop from the shooter, and whether the agent is at it (cover_in, cover_kneel and cover_out);
- per Glyph, the move-snap test (an end within 1.5 m of a cover point within 10 m).

