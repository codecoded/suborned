# 026 · Full render quality: the scenery

Sky, 2026-10-19: "Our world is a little too bright and we need to keep working now on full render quality for the scenery. The buildings need a lot more work compared to our original deco kit city. And the plants / signage / roads / pedestrian crossing etc. Let's start on full render quality."

## The bar
- **Street level and play zoom read like the 26 Sep paintings** (`briefs/008/concepts/`, the reference for the civic block and the street) and at least match the old DecoKit city (`~/Projects/the-suborned/game/Assets/TheSuborned/Art/DecoKit/`, generator `~/Projects/the-suborned/workbench/Tools/Blender/decokit/`, source art `~/Projects/the-suborned-store/source-art/DecoKit/`: textures, derived normals, HDRI). Skyline: the AtomicAge and Blocks sheets (brief 007).
- **Too bright today.** The pastel base stays (§15), but exposure, grade, haze and material response must give form: shadow, AO, contrast between stone, enamel and chrome, depth into the distance. "Bright retro-future" is the palette, not a blown-out frame.
- **Scope:** buildings (facades, corners, roofs, trim, glazing, depth), plants (palms, cypress, beds, planters), signage (shop signs, banners, wayfinding), roads (asphalt, kerbs, gutters, setts, paving bands), pedestrian crossings (brief 009: floating signal eyes, light set flush in the road, brass studs, no zebra or slab), street furniture.
- **Fixed:** 60 fps at play zoom on the 15-inch Air (§11; measured only on the Air); the plan is the source and nothing is hand-placed; only Latch edits scenes; concept art first for any new look decision (show Sky pictures); the HUD and the signals are out of scope.

## Sky's calls (2026-10-20, after the phase 1 shots)
- **The old DecoKit city is the floor** ("this to be our base level and only move up from here in quality"). Nothing ships below it: textured ground, real robots, street props, plants and signage at least as good as `briefs/026/old-decokit/`.
- **Focus stays on the capstone game** (the Handover block), but with the right robots, street props and textures from the original kit.
- **The café needs more work:** the Suborned 2.5D three.js testbed had much higher resolution (`~/Projects/the-suborned-store/testbed-art/cafe-3d`, on the Air, not yet on the Studio).
- **Next after this:** the fire fights, made like Syndicate (brief 027).

## Phase 1 · see the gap (done)
- **Latch:** a fixed baseline shot set of the current world, same stops every round (`briefs/026/baseline/`, with each stop's camera in `stops.json`): play zoom 14 at 38° at yaw 30 and 210, and street level, at the Records Office front, the café, a crossing, the avenue, a planted square or park, a side street, and one skyline view. The shots are the round-to-round comparison. If the old project can render the same kinds of views of the old DecoKit city, capture those too (`briefs/026/old-decokit/`).
- **Vela:** the gap audit, one HTML report (`briefs/026/vela/gap.html`): per family (buildings, plants, signage, roads, crossings, street furniture), what the old DecoKit had (pieces, materials, textures, normals, detail), what the world uses now (kit adopted vs procedural stand-ins, flat colours vs textures), and a ranked plan (what to port, what to build, what needs concept art first), with costs in frame time where known.
- **Glint (after Latch's baseline):** why it reads too bright: exposure, tonemapping, grade, sun and sky intensity, ambient, haze, material albedo and smoothness. A proposed fix shown as before/after pairs on the baseline stops, without changing the palette's hues.

## Phase 2 · build (after Sky sees phase 1)
Order and owners come from Vela's plan and Sky's call. Likely: lighting fix (Glint), textured road, kerb and paving materials and the crossing pieces (Vela, Splice for placement), facade detail and kit port (Vela), plants and signage (Vela), each round re-shot on the baseline stops and measured on the Air.

## Done
Sky looks at the baseline stops and says the scenery is at full render quality.
