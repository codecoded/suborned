# Brief 107 · the night glows (Vela, 2026-11-11)

Item 2 of the round. Glint's page asks for the seafoam inlay bands and the planter feet to glow "as light set in the ground" (`briefs/107/glint/weather.html`, Night). The target is `briefs/107/concepts/night.png`.

Both are built as kit candidates in `tools/decokit`. Nothing is adopted, and Unity is untouched. By day neither glow draws anything.

## What was built

| Glow | Pieces | Material Latch drives | How it lights |
|---|---|---|---|
| Inlay bands | `InlayBand_06_2`, `InlayBand_12_2`, `InlayArc_R2_W06`, `InlayArc_R4_W06`, `InlayArc_R9_W12` (36 placed in the zone) | **`DK_Inlay_SeafoamNight`** | Emission on the band itself |
| Planter feet | `PlanterRun_{2,4,Corner,End}_{A,B,C}_TB`, `PlanterArc_TB`, `CafeTerrace_TB` (the zone places 12 runs, 4 arcs and the terrace) | **`DK_Glow_PlanterFoot`** | A new object `<level object>_glow` in every level |

**`DK_Inlay_SeafoamNight`** is a twin of `DK_Inlay_Seafoam`, with the same kit row (ceramic, #72B5A7). Its generated .mat matches the generated `DK_Inlay_Seafoam.mat` line for line apart from `m_Name`. Emission is black and `_EMISSION` is off. The meshes are unchanged; only the slot's material name differs. It is a twin rather than the old name, so nothing else that ever draws `DK_Inlay_Seafoam` lights up with the bands.

**`DK_Glow_PlanterFoot`** is a strip at the foot of each planter, in two parts:
- a pool lying on the paving, 0.30 m out from the wall;
- a wash up the wall face, 0.12 m high, or the face's own height where that is lower. The runs' plinths are 0.10 m.

It sits 4 mm off the paving and 3 mm off the wall, mitred at the joints. UV v is 0 on the foot line and 1 at the far edge. It uses kind `glow`, so the generator writes no .mat: Unity draws it with `Suborned/Cross Glow` `_Mode 3` (pool), additive, the same as `DK_Road_Pool`.

The stone, soil and planting objects keep their slots and their order. The glow is its own object, with shadows off in the row's `objects`.

The foot is traced on L0's stone faces only, never the leaves that spill over. A foot is skipped in two cases:
- stone or soil closes it from above (inner walls under the soil, including the companion `CafeTerrace_Planting` soil);
- stone stands within 0.2 m outside it (a run's body inside its own plinth).

Every strip lands outside its planter. See `renders/plan_glow.png`, where the strips are drawn red.

### Values

These are a starting point for Glint, who tunes them on the runtime copy:

| | evening | night |
|---|---|---|
| Inlay emission: linear(`#6FE3C8`) × level | 0.15 | 0.30 |
| Foot pool `_Alpha` (`_BaseColor` #FFD49A, `_Strength` 2, `_Floor` 0, `_Absorb` 0) | 0.25 | 0.42 |

At night the foot's peak is 2 × 0.42 = 0.84 linear × colour. The road pool's peak is 0.44, but the foot strip is much smaller. The values live in `tools/decokit/glows107.py`. The kit row's `alpha` 0.42 is the night value, as `DK_Road_Pool`'s 0.22 is.

### Cost

- **Inlays:** +0 triangles and +0 slots.
- **Triangles per planter (L0):** +32 for a 4 m run, +24 for a 2 m run, +56 for an end, +80 for a corner, +224 for an arc, +128 for the terrace. The same strip is in every level.
- **Zone total:** about +1,400 triangles.
- **CD-3** (civic LOD0 budget): 362,397 → 362,909 of 370,000.
- **Draw cost:** one transparent additive draw per glow batch. With alpha 0 it adds nothing to the image.

## Does `RoadLight` already drive anything like this?

Yes, nearly:
- `DK_Road_Lens`: an emission level;
- `DK_Road_Pool`: the same Cross Glow pool, `_Alpha` 0 by day;
- the `glow` list: modes `emission`, `tint` and `lit`.

**But no current mode can start from black:**
- `emission` multiplies the material's own emission, and refuses a black one ("has no emission to scale").
- `tint` leans in proportion to it.
- `pool` is a single material, not a list.

Two small additions, both Latch's:

1. **Mode `night`:** emission = linear(`colour`) × level. The level is 0 by day, `evening` and `night` otherwise. `_EMISSION` is enabled on the runtime copy only.
2. **Mode `alpha`, or `pool` as a list:** eases a Cross Glow material's `_Alpha` from 0.

Entries for `roadlight.json`:

```json
{ "material": "DK_Inlay_SeafoamNight", "mode": "night", "colour": "#6FE3C8", "evening": 0.15, "night": 0.3 },
{ "material": "DK_Glow_PlanterFoot", "mode": "alpha", "evening": 0.25, "night": 0.42 }
```

Nothing lights in the old town until Glint's fix gives it a `LightPhase`. `RoadLight` reads that phase.

## Latch's one kit step

1. **Inlays into the generated root:** `bun run decokit --only InlayBand_06_2,InlayBand_12_2,InlayArc_R2_W06,InlayArc_R4_W06,InlayArc_R9_W12`. This writes `Materials/DK_Inlay_SeafoamNight.mat` into `out/decokit`.
2. **Planters:** use `bun run kit --extra out/decokit-107-extra`, **in place of** `out/decokit-097-extra`.
   - `out/decokit-107-extra` is a full copy of 097-extra with the 14 planter rows rebuilt. It has a `KEEP` file.
   - To rebuild it: `bun run guard /Applications/Blender.app/Contents/MacOS/Blender -b -t 1 -P tools/decokit/glows107_extra.py`.
   - Rerun it after any later rebuild of 097-extra, such as an AO pass.
3. **Glow material:** commit `Assets/Suborned/Materials/Glow/DK_Glow_PlanterFoot.mat`. Copy `DK_Road_Pool.mat`, give it a new guid, and set `_BaseColor` r 1, g 0.8314, b 0.6039, plus `_Strength` 2, `_Mode` 3, `_Floor` 0, `_Alpha` 0. `bun run kit` will warn that there is no .mat in any root for it, which is expected for kind `glow`.
4. **`RoadLight`:** add the two modes and the entries above.

**Dry run of step 2, writing nothing** (`out/scratch/vela/107/dry.ts`):
- 14 planter rows change: triangles, material, the `_glow` object (`off`), files and LOD hashes. Their sizes are unchanged.
- The other 14 rows of the extra root change only their `extra` path. Their bytes are identical.
- Every status stays `adopted`.

Step 1 re-hashes the 5 inlays: same meshes and sizes, new material name.

**Palette (CD-4):** `contracts/world/civic-look.json` now lists both new materials. CD-4 passes with the post-adoption materials (73/73; `out/scratch/vela/107/cd4.ts`).

## Things Latch needs to know

- **Bake and bounds.**
  - Keep the `_glow` objects out of the lightmap bake and GI. They are transparent additive, and `Combine` already never lets a Cross Glow cast.
  - Keep them out of anything that measures renderer bounds: the zone-edge clip, the occlusion cut and cover. Their bounds reach 0.30 m past the stone.
  - The kit `size` is unchanged (the stone footprint), and collision is declared, so it is untouched.
- **Draw order.** The pool lies 4 mm over the paving in the transparent queue, where the ring and target decals also sit. It is additive, so the order only matters where it meets an alpha-blended ring. If a ring at a planter foot reads brighter, drop the glow material's queue below the decals.
- **Day exactness.** In Unity, emission is black with the keyword off, and the pool's `_Alpha` is 0, which adds exactly 0.
  - The planter files were re-exported through `vertex_ao_extra`'s round trip, the same one that made 097-extra. That moves the stone by float32 noise:
    - positions by up to 0.5 µm;
    - normals by up to 0.0004 on the L0s the play camera draws;
    - normals by 0.01 on `CafeTerrace_L2`, which play zoom never shows.
  - In Blender, the day frame of today and of the candidates differ in **177 of 2,073,600 pixels, by 1/255**, all on one far planter run. The batch's day-versus-day shot is the Unity proof.
- **Not covered here:**
  - The five civic **bed kerbs** by the Records Office are built by Unity from the export's rings, not from kit pieces. To match, the builder would lay the same strip (the numbers above) in `DK_Glow_PlanterFoot`.
  - `Planter_Trough` (a street piece) and the old town's own planters are outside the zone.
  - The **table lamps under the parasols** on Glint's list are lights, so they belong to `TimeOfDay097.Lamps`.
  - The concept draws the bands as bright-edged tubes. Ours glow evenly. A brighter edge would need an emission map on the inlays; that's a later option if Sky wants it.
- **Glyph** should check one night frame for the warm foot beside the guard's amber, as Glint asked. The foot is pale (#FFD49A) and the guard is saturated (#F0A020).

## Renders

These are in `briefs/107/vela/renders/` (git-ignored). Built by `tools/decokit/review107_glows.py`: the café square as the world export places it, EEVEE, the kit's material rows.

| File | Shows |
|---|---|
| `board.png` | Concept, day, night today and night with the glows, 2 × 2 |
| `day_today.png` / `day_glow.png` | The play frame by day (20 m tall, 38°), today and the candidates |
| `night_today.png` / `night_glow.png` | The same frame at night |
| `night_close.png` | 8 m tall, on the terrace foot and the inlay spine |
| `plan_glow.png` | Top down, the strips in red |

The night frames are a Blender stand-in for the look (moon, dark teal sky, point lamps at the sphere and globe lamps, bloom). Glint's Unity night decides the final levels.

## Files

- New:
  - `tools/decokit/glows107.py`: values and registrations;
  - `tools/decokit/glows107_extra.py`: the planter strip into an extra root;
  - `tools/decokit/review107_glows.py`: the renders.
- Changed:
  - `tools/decokit/civic_paving.py`: the inlays draw `DK_Inlay_SeafoamNight`;
  - `contracts/world/civic-look.json`: two palette rows.
- Built, not committed:
  - `out/decokit-107-extra`, which Latch imports;
  - `out/decokit-107`, the inlay review root only. Latch rebuilds the inlays into `out/decokit`.
- Checks: `bun run typecheck` is clean. `civic-dress`, `look`, `beds`, `kit`, `extra`, `wave2` and decokit `run` tests: 100 pass, 0 fail.

**Next owner:** Latch, in his night round, after Glint's old-town night fix. The done-check is the batch's night-versus-day shot pair: the bands and planter feet lit at night, and the day shot unchanged.
