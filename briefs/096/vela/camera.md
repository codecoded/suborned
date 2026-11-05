# Brief 096 · the hovering street camera (Vela, 2026-11-05)

Sky picked concept A3, "hovering" (`paid/096-street-camera/codex/A3-hovering.png`). This is the porthole eye from `A-porthole-eye.png` floating on a ring of light, with a wall cradle where a dead camera docks. It is built in Blender with the DecoKit generator, costs $0 and uses no paid calls.

- Builder: `tools/decokit/hover_camera.py` (family `hover_camera` in `kit.py`)
- Review renders: `tools/decokit/hover_camera_review.py`
- Sheet: `tools/decokit/hover_camera_sheet.py`

## Pieces and parts

**`HoverCamera`** (Signals) is the head. Its pivot is the body's turn centre, not the ground. Front is Unity +Z and up is +Y. All parts share the origin, and each part is its own renderer.

| object | what | material(s) |
|---|---|---|
| `DK_HoverCamera` | ivory teardrop body; seafoam swoosh on each flank with brass hairlines on both edges; coral eye rim; chrome bezel; chrome iris ring; lamp collar; four screws; belly pad with chrome boss | DK_Eye_Ivory, DK_Eye_Seafoam, DK_Eye_Coral, DK_Brass_Civic, DK_Chrome, DK_Enamel_Charcoal |
| `DK_HoverCamera_Lens` | the big dark lens dome (r 0.197 m) plus an inner pupil dome standing proud | DK_Eye_Lens |
| `DK_HoverCamera_Tally` | amber tally lamp on top (0.17 m across) | DK_Eye_Tally |
| `DK_HoverCamera_Ring` | flat ring of light, 1.6 cm under the belly (r 0.13–0.25 m) | DK_Eye_Ring |

**`HoverCamera_Cradle`** (Signals) is the wall cradle. Its pivot is the bottom centre of the wall plate, with the back face on the wall. Front (+Z) points out of the wall, as on DomeCamera.

- The wall plate is 0.36 × 0.64 m: a seafoam plate, three stepped ivory ribs, a brass hairline and chrome screws.
- A seafoam arm with a chrome collar carries a shallow ivory saddle that the belly ring settles into.
- It is one object.

## Measures for the host (Unity axes, metres, at scale 1)

**Head:**

| item | value |
|---|---|
| length | 1.035 m |
| width | 0.534 m |
| lens glass front | (0, 0, 0.23) |
| pupil tip | (0, 0, 0.256) |
| tally top | (0, 0.372, 0.03) |
| ring | under (0, −0.31, −0.06) |
| lowest point | y −0.328 |

**Placement, keeping today's sight lines:**
- Put the head origin at `at + up × 4.78 + facing × 0.66`.
- This puts the lens at the same point as today's pole lens: 4.78 m up and 0.89 m out (`HostViews.Attention.cs` LensUp / LensOut, the rulebook's 4.79 m).
- Tilt it 16.7° nose-down (LensDown) about its local X, then yaw it to `Camera.Facing`. The nod in `CamCue` turns the head about its origin as now.
- Hover is up to Latch. A bob of ±3 cm at about 0.5 Hz on the whole instance keeps the lens within the cue's tolerance.

**Cradle:**
- Mount it with its back on a wall and its foot at lens height − 0.47 (4.31 m for a 4.78 m lens).
- The docked head's origin is cradle-local (0, 0.47, 0.90), facing the cradle's +Z, level with no tilt.
- The docked tail clears the ribs by 4 cm.

**Dropped:**
- If the head falls to the ground instead, it rests on its ring with the origin 0.328 m up.
- Lying on its flank, the origin is about 0.27 m up.

## States (`MaterialPropertyBlock` on the part's renderer)

| state | Tally | Ring | Lens | body |
|---|---|---|---|---|
| live | as built (emission on) | as built | `CamCue` drives `_BaseColor` / `_EmissionColor` (the lens is emit kind, emission black as built) | hovering, bobbing |
| looped | not decided: my suggestion is a slow 1 Hz blink (Glyph / Motive to decide) | on | dim | hovering |
| dead (shot or cut) | `_EmissionColor` black, `_BaseColor` #6B4A1E | `_EmissionColor` black, `_BaseColor` #5E6B6A. Keep the ring drawn: the docked head rests on it. | LensDim | docked in its cradle, or dropped |

These values are in `hover_camera.DATA` and in `renders/review.json`.

## Materials (all new, registered in `hover_camera.py`)

| material | kind | colour | notes |
|---|---|---|---|
| DK_Eye_Ivory | ceramic (satin, no clear coat) | #EFE8D6 | the kit ivory |
| DK_Eye_Seafoam | ceramic | #72B5A7 | the civic seafoam |
| DK_Eye_Coral | ceramic | #E2725B | the concept's rim |
| DK_Eye_Lens | emit | #10181E | strength 0, glossy |
| DK_Eye_Tally | emit | #FF9A14 | ×1.6 |
| DK_Eye_Ring | emit | #5FE0C0 | ×1.8 |

- **Less shine:** the enamels are matte painted (smoothness 0.62, no mirror coat). They carry the kit's enamel surface detail, so no flat colour ships. Only the lens and a little chrome are glossy.
- **Shared materials:** brass is DK_Brass_Civic as hairlines only (SF-21), plus DK_Chrome and DK_Enamel_Charcoal.
- **Two changes from the concept:**
  - The ring is turquoise (the robots' hover glow), not the concept's warm white, which vanished against cream stone.
  - The tally is a deeper amber than DK_Signal_Amber, which burns to yellow.
- Glint owns the emissive levels in game light.

## Triangles and budget

| piece | parts | total | LOD1 / LOD2 (the exporter's collapse) |
|---|---|---|---|
| HoverCamera | body 1,946 · Lens 310 · Ring 336 · Tally 112 | **2,704** | 674 / 159 |
| HoverCamera_Cradle | one object | **1,842** | 459 / 103 |

- **Budget used:** the street-props row of `out/review/lod-thresholds.html` and the CCTVPole it replaces.
- The pole camera ships at **6,882** triangles (kit.json; its 1,500 cap from brief 007 r3 is not what shipped).
- Head plus cradle come to 4,546, below that. A live camera with no cradle is 2,704.
- I made it lighter by building it with fewer segments, not by decimating it, so it is not compression (GDD §12).
- At play zoom the LODs never switch: street L1 is 0.008 of the screen and the head is about 0.05.

## Readability at play zoom (orthographic size 10, 38°; the figure is about 80 px)

On the play camera the head is about 55 px long and the lens about 20 px. The facing reads from three things:
- the teardrop (blunt end forward)
- the coral rim
- the turquoise ring's crescent showing under the body

The amber lamp is about 8 px and reads amber, on and off. The dead state reads by colour and position: brown lamp, grey ring, the head sitting in the cradle.

Renders are Blender EEVEE with no URP grade. They show shape and read, not final colour.

## Pipeline evidence

- `bun run guard bun run decokit --only HoverCamera,HoverCamera_Cradle` wrote the FBX and the LOD1 and LOD2 files into `out/decokit/Models/Signals/`.
- The run then exited 1 at its fit table, but not because of these pieces. The shared manifest's tower orders say "no such order in the plan", and SpaceHQ_Portal's "order hash differs". Both were stale before this run.
- `bun run kit --catalogue` added exactly two entries to `contracts/world/kit.json`, both `status: candidate`, `source: generated`, with sha256 per file. No other asset or top-level field changed: I compared against a copy taken before the run.
- `bun run kit` copied 0 files. Candidates are not copied until they are adopted.
- `bun test tools/kit`: 35 pass.

## Open, and next owners

- **Splice:**
  - Give the two pieces their own use tags before adoption, e.g. `camera:hover` and `camera:cradle`. Today they take the Signals default `street`.
  - Fix the catalogue's pivot claim for HoverCamera: it says `origin-ground` with offset (0, −0.262), but the origin is the turn centre.
  - Add them to `orders.ts` BUILD if a bare `bun run decokit` should rebuild them.
  - The stale tower orders in the fit table, above.
- **Latch:** host it in `HostViews.Attention.cs` (KitPole / CamHeadFor) with the measures and states above, then adopt it. Scenes are untouched.
- **Vantage / Datum:** a camera site with no wall within about 1 m has nowhere for its cradle. Either it drops to the ground when dead, or it needs a mount: the old pole without its head, or a slim mast cradle that I can make.
- **Glint:** set the tally, ring and lens levels in game light.
- **Glyph / Motive:** decide how a looped camera looks.

## Renders (git-ignored, on disk)

All in `briefs/096/vela/renders/`:
- `sheet.png`: everything on one page, with the play crops at 1:1 and ×4
- `threeq.png`, `threeq-cradle.png`
- `side.png`, `side-cradle.png`
- `play-30.png`, `play-210.png`
- `review.json`
