# Brief 096 · the hovering street camera in play (Latch, 2026-11-05)

Sky picked concept A3 ("hovering"), then said:
- "it's not wall mounted - it's same position as current camera, just floating"
- "doesn't need a cradle - this is retro future world"
- "you need to give the camera a shadow"

So both mission cameras (entrance, pickup) are now Vela's `HoverCamera`. It floats where the pole camera's head was, with no pole and nothing holding it up.

## What it does (view only: it reads the engine, never changes it)

- **Where:**
  - The lens glass sits exactly on the old pole lens's point: 4.78 m up, 0.89 m out along the camera's facing. Cones and sight lines don't move.
  - The head is tilted 16.7° nose-down and yaws to the engine's facing, read every frame.
  - Vela's origin, 0.66 m out, would have put the tilted glass 7 cm lower. Sky asked for the lens to stay put, so the glass point wins.
- **Live:** it bobs ±3 cm at 0.5 Hz, the turquoise ring breathes with the bob, and the amber tally lamp is lit.
- **Looped:** the tally blinks at 1 Hz. This is Vela's placeholder until Glyph and Motive decide.
- **Dead (shot or cut, the engine's CameraDark):**
  - The lamp and ring go dark, using Vela's dead colours (#6B4A1E and #5E6B6A).
  - The lens goes dim.
  - Over 1.6 s the head sinks and settles on the ground at its spot, resting on its ring.
- **Shadow:**
  - Every part casts the sun's shadow, live and down, the ring included.
  - A soft contact shadow lies on the ground straight under the head. It uses the game's own `Suborned/Contact Shade` blob, the one under the street pieces.
  - The contact shadow follows the bob. It is a little fainter and wider when the head is up, and darkest when it rests.
- **Kept:** the click target (the pole's collider, 5.5 m), the callout anchor (`CamHeadUp`, `CameraTall`), and the attention cue on the lens.
  - While a cue shows, the lens wears the cue's unlit colour.
  - The flash scales the lens about its own centre.
  - The inset close-up frames the head.

## Pictures (play zoom 10, 38°, 1920×1080; git-ignored, on disk)

Each `compare-*.png` sheet shows the same window three times, ×2: the old pole (0.2.244), the hover head at 1.0, and the hover head at 1.4 (scaled uniformly about the lens point, so the lens stays put).

- `compare-entrance-live-yaw30.png`
- `compare-entrance-live-yaw210.png` (the Records Office front is played at 210)
- `compare-pickup-live-yaw30.png`
- `states-entrance-yaw210.png`: live, looped (blink caught lit), dead (resting on the ground)
- `full/`: the whole frames, plus close-ups at zoom 4 and the dead frames at yaw 30 and 210

**Scale (Aster to show Sky; I haven't picked one):**
- At 1.0 the head is about 55 px long beside an 80 px figure. Its facing reads from the coral rim, the lamp on top and the ring's crescent.
- At 1.4 it reads at a glance from across the frame.

**Notes for the review:**
- **Pickup at yaw 210:** the camera stands behind the Office tower from that turn and is hidden. The occlusion cut opens for people, not for the head. The old pole was hidden there too: see `full/pole-pickup-live-yaw210.png`.
- **Entrance at yaw 210:** the head projects over the foot of the banner lamp post behind it.
- **Sun shadow:** at these two sites at this hour the head's sun shadow falls into the Office's shade or onto its front. The contact shadow is what reads on the ground.

## Kit

- `HoverCamera` is adopted.
- `HoverCamera_Cradle` is rejected, with `why` set to Sky's words. `bun run kit` pruned its copied FBXs from `unity/Assets/Suborned/Kit`.
- Catalogue change for Splice: `KitAsset.why?` in `contracts/world/kit.schema.ts`, carried over by `tools/kit/catalogue.ts` the same way `status` is. Before this, a reason written by hand would have been dropped by the next `bun run kit`.

## Lab switches (player command line, for review shots only)

- `-hoverScale s` scales the head about the lens point (0.5 to 3).
