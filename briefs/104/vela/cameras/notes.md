# Brief 104 · the camera family, before and after (Vela, 2026-11-10)

Sky, on the square mast's heads in 0.2.310 (`sky_crop_0.2.310.png`): "this ugly is poorly done - find any others like it and fix".

**Not in the game yet.** These are scratch exports (`out/scratch/vela/cam104/build`). `kit.json`, `bun run kit`, collide and sim:site are untouched; that step is Aster's, after Sky has seen this.

The renders are Blender EEVEE with review101's light and dress (palette rows × baked vertex AO), not Unity's URP:
- `*_play_y180` is at play zoom on the Air (93 px/m), seen from the opening side, like Sky's shot.
- `*_crop_y180` and `*_crop_y030` are at 160 px/m (the density of Sky's crop), from the back and from the lens side.
- `*_close` is a perspective three-quarter view.
- `<piece>_board.png` sets each before beside its after.

Made by `tools/decokit/review104_cameras.py`:
`blender -b -P tools/decokit/review104_cameras.py -- <out> <decokit root> [--only A,B]`

## The audit, ranked by how visible it is in play

| # | piece | where in play | fault | done |
|---|---|---|---|---|
| 1 | **MastCamera_Lamp** | the square's mast, beside the cell's start | petal heads, ragged teal edges, lumpy body, bare crown | rebuilt |
| 2 | **HoverCamera** | the Handover's mission cameras (the ones you loop) | the same ragged brass and teal edges, faceted flanks and belly | fixed |
| 3 | **MastCamera** | the district's other masts (not in the Handover zone) | its three tail fins floated about 0.5 m off the pod (a bug), faceted tail | fixed |
| 4 | DomeCamera | not placed in the zone or by the world | none: a clean store piece (7,550 tris) | left alone |
| – | HoverCamera_Cradle | rejected in kit.json | – | left alone |

**Aster's read was right on every point**, and the picture shows the cause of the ragged edge:
- **The brass hairlines on the stripe edges** were 3-sided tubes 8 mm across, laid chord to chord over a curved skin. They sank in and out of the body, so the edge read as a dashed brass-and-teal sawtooth.
- **The baked vertex AO** made it worse. The hairlines occluded the corners of the long, thin stripe triangles beside them, which drew dark wedges ("teeth") into the ivory. See `MastCamera_Lamp_ao_before.png` against `_ao_after.png`.
- **HoverCamera** had exactly the same hairlines (`hover_camera.py`), and nothing else in the tree uses that pattern on a curved skin.

**Zone scan** (`zone_scan.png`: 28 street pieces in the Handover zone at the play angle): no other piece has the ragged band or petal fault.
- Minor and left alone: the BollardSphere and CafeLamp_Testbed feet show their 12 sides close up, but these props are 0.5 m and about 45 px in play.
- The white planting and parasol cloth in the scan are the audit render's dress (no textures), not the pieces.

## 1 · MastCamera_Lamp (`tools/decokit/civic_mast101.py`)

**What changed in the heads** (they were a tapered ivory petal under a flared hood):
- **Housing.** Each head is now a streamline housing with a rounded-box section (a superellipse, power 2.7, 32 points), so it reads as a housing, not a bulb.
- **Back.** A long rounded back ends in a chrome tail cap, where the spoke goes in. A chrome trim ring sits behind the middle.
- **Visor.** The deep teal stripe is now a separate enamel sunshade. It rides 16 mm over the top and overhangs the lens by 7 cm, and its tail is swept.
- **Hood.** The deep teal hood is straight with a rounded lip (no flare), with a brass hairline ring where it meets the body. It is charcoal inside, down to the chrome bezel and the lens.
- **Crisp edges.** Every coloured edge is now the edge of its own solid, never a seam in the skin, and no hairline wanders over a curve. The visor's curved ends are quads, so Unity can't triangulate a concave n-gon badly.

**Crown** (it read as a glass bubble): it now has a chrome bead on the drum and a seafoam enamel cap sloping to a brass ring, then a smaller chrome dome, a finial collar, a stem and the ball. The top is still 8.195 m.

**Kept:**
- design B's silhouette: three heads, a 60° fan, 15° nose-down, CAM_S 1.15;
- ivory bodies and the deep teal stripe and hood;
- the lamp, column, banner and declared solids, untouched;
- the palette only, with no new material;
- the lens parts `_Lens0/1/2` on DK_Eye_Lens, with the same geometry in the same place.

**Lens check** (review101's `lens_check` on the new export): PASS, identical to before.
- Lens 0: (0.902, 7.687, 0.000) at 0.00°
- Lens 1: (0.443, 7.687, 0.772) at 60.16°
- Lens 2: (−0.443, 7.687, 0.772) at 119.84°

`LENS` prints the same table.

**Triangles:** L0 10,760 → **14,674**. The three heads are about 4,986 → about 8,700. LODs are 3,665 / 877.

## 2 · HoverCamera (`tools/decokit/hover_camera.py`)

**What changed:**
- The swoosh's brass hairlines are now 12 mm strips of the skin itself, so they are continuous and crisp, with no AO teeth.
- There are twice as many rings along the body (15 → 24).
- The ivory panels have 16 segments instead of 9, and the seafoam panels 6 instead of 4.

**Kept:** every measure the host reads (DATA: lens, tally, ring, pad, lowest), the parts and the materials.

**Triangles:** 2,704 → **3,970**.

## 3 · MastCamera (`tools/decokit/civic_props097.py`)

**What changed:**
- The fins' plane now holds the pod's axis. They had been extruded across it, so they floated half a metre out at the pod's middle.
- Their inner edge now meets the tail.
- The pod is a 28-sided lathe instead of 20, with a rounder tail profile.

**Triangles:** 2,182 → **2,454**. Its size in kit.json will shrink, since the floating fins set the old bounds.

## Left alone, with why

- **DomeCamera** is clean, and nothing places it.
- **HoverCamera_Cradle** is rejected.
- **The BollardSphere and CafeLamp feet** show their facets only close up.

## For Aster

1. **The kit step.** Rebuild MastCamera_Lamp, HoverCamera and MastCamera into out/decokit, then run `bun run kit` and collide.
   - None of the declared solids moved.
   - MastCamera's measured bounds change because the floating fins are gone.
2. **Exit code.** The scratch `bun run decokit --only …` runs exit 1 after a good export. The scratch root has no car enamel `.mat` from a full run (`[carpaint] … missing`), so this is not these pieces. The export lines are in `out/scratch/vela/cam104/build*.log`.
3. **Check in Unity.** The AO and the teeth were what Unity showed, and EEVEE only approximates it. Check one crop in the player.
