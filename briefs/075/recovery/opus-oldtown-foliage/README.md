# 075 · Opus 3D assessment: original OldTown foliage in the café troughs

Vela (Opus 5.5), 2026-10-29. This is review only. I made no production, Unity, generator or kit changes, ran no renders and spent nothing on paid calls. Exact transforms are in `placements.json`. Evidence I inspected: `../oldtown-foliage-reuse/` (source gallery, comparison sheets, manifest, audit, distinctions) and `../latch-integration/foliage/today-day-cafe-square-y210.png` and `today-day-trough-detail-y210.png`. Code I read: `DecoKitDistrictDressing.cs`, `DecoKitPlazaLayout.cs`, `DecoKitGardens.cs`, `tools/decokit/plaza.py` and `civic_common.py`.

## Verdict

**Adjust.** Use the right assets, the original intact FlowerBed and ShrubMound, but at the wrong scales. The proposed 2 × FlowerBed at 0.75 and 7 × ShrubMound at 0.48 are faithful (nothing warped) but not useful. They leave a bare cream slab with small, separated plants on it. The old town never looked like that, and it would not answer Sky's "why not just use the old town one?".

The shrinking is self-imposed. Both scales fit the plants to about 0.47 m, the width of the *current recipe's* internal planting band (`planting(w=0.5)`). That band is narrower than the current domes themselves (Ø 0.50–0.62 m). The real limit is the current renderer envelope, which keeps sight, clearance and the HOLD untouched:

- coping |y| ≤ 0.34 m
- container |x| ≤ 2.0 m
- top ≤ 1.062 m (B) or 1.120 m (A)

| Trough | Direction | Old-town precedent | Tris (current → trial) |
|---|---|---|---:|
| `run-west-cafe/1` (B) | **2 × FlowerBed, scale 1.0**, x = ±1.0, z 0.655, yaw 0 | `DecoKitPlazaLayout.cs:114–118` "cafe terrace edge": PlanterBed (opening 3.7 × 0.7) + two FlowerBeds at ±1 m, scale 1. This is literally the old café-edge planting. The beds are 1.98 × 0.62 m: they fill 3.98 × 0.62 of the 4.0 × 0.68 coping, top 1.029 (≤ 1.062). | 672 → 5,496 |
| `run-west-cafe/0` (A) | **6 × ShrubMound, scale 0.64**, x = (i − 2.5) × 0.62, z 0.645, yaws 0/180/90/270/180/0 | `DecoKitPlazaLayout.cs:108–110` ShrubMound hedge along the façade base; `DecoKitGardens.cs` 60/40 shrub/flower mix. 0.64 is the largest uniform scale under the 1.120 top ((1.120 − 0.645) / 0.728 = 0.65). The mounds are 0.61–0.64 m across at a 0.62 m step, so they touch, like the current hedge. | 732 → 15,216 |
| `cafe-palm-0/2/4` | **No change**, not part of the trial | Geometry, slots, UVs and normals are already identical to the original. Swapping bytes or materials buys nothing. | 0 |

Shrubs in A and flowers in B keep the neighbours different, and match the old mix. Putting FlowerBeds in both troughs would make the 8 m run read as a duplicate pair.

Quarter-turn yaws are the only rotation, so the axis-aligned footprint (0.64 × 0.61 m) is guaranteed inside the coping. That rotation is placement only, with no new arrangement inside the mesh.

## What this is not

- **Not the old café terrace itself.** The old `Terrace()` (`DecoKitDistrictDressing.cs:45–58`) had no troughs. Its plants were a PottedFern at each terrace end plus HangingBaskets on the piers. Bringing those over means *new placements* in the world (fern pot Ø 1.3 m), so it is a plan/layout change for Vantage and Datum if Sky wants it. It is not render-only reuse, and I don't propose it here.
- **No new leaf geometry, tint, soil slab, clamp or decimation.** The original FBXs keep their unclamped below-ground droop. It sits inside the solid coping and body (the current trough has no soil recess; the coping is a full slab).

## Materials and shader: evidence and unknowns

- `distinctions.json`: the OldTown and current `DK_Leaf_CoreShrub`, `DK_Leaf_Shrub` and `DK_Flower_Coral` differ in **`m_Shader` only**. Every other property line is identical.
- The two shaders, from a diff I ran:
  - `OldTown/DecoKit/Lit Cutaway` (`OldTownArt/DecoKit/Shaders/DK_LitCutaway.shader`, 92902edf…) is an older fork of the current `DecoKit/Lit Cutaway` (`Shaders/DecoKit/DK_LitCutaway.shader`, 2529b421…).
  - The fork lacks three things: the `_DK_CUT` multi_compile (cut code is always compiled), the `DK_SurfaceDetail` global switch, and the `_ClearCoatMask` environment-reflection scale.
  - Both gate the cut on rendering-layer bit 20 and `_DKCutFocus`. Planting is not fade-tagged, so the cut should never fire on it.
  - On alpha-clipped leaves the always-on discard path costs nothing extra.
  - What is not measured: SRP Batcher compatibility of the fork, shader-variant stripping in the player build, and whether the surface-detail switch changes anything on these leaves.
- **Bind the original OldTown `.mat`s** (Sky asked for the old ones), set explicitly per slot. Don't let `KitLods`/library-name remapping substitute the current twins. The current `Kit/Materials` twins are a recorded fallback only if the fork misbehaves in the play scene. If the fallback is used, say so in the evidence.
- The Blender Principled study is not URP. The terracotta-brown read of `DK_Flower_Coral` in the Cycles sheets is unverified in game. The fountain-garden beds in Latch's frame suggest it reads warmer under the play grade. Judge the colour only from the Unity capture.

## Asset-local study vs world

Everything in `../oldtown-foliage-reuse/` is asset-local: isolated headings 30/210, no grade, no neighbours, no cutaway. It shows silhouette and fit, nothing else. Things only the Unity trial can show:

- the actual world headings (opening yaw 30 and the Records Office front at 210)
- the play grade, probes and shadows
- alpha shimmer in motion
- frame cost

## Final capture criteria (Unity owner, after the tram turn)

1. **Pairs:** before and after with the same build, lighting and grade:
   - café square at play zoom 14 / 38°, world yaw 30 and 210
   - trough detail close-up at 210, framed as `latch-integration/foliage/today-day-trough-detail-y210.png`
   - one night frame
2. **Fit:** imported world bounds of each instance within `expectedLocalBounds` in `placements.json`.
   - No leaf card visible through the trough body below the coping lip. If one is, B moves to the `fallbackZ` 0.687.
   - No z-fighting where a FlowerBed core meets the coping.
   - No part beyond the coping into the walk or the kerb.
3. **Untouched:** colliders, nav, sight, doors, routes, the trough slot 0 restyle, the palms, the topiaries and the sight HOLD.
   - The world export and sim data are byte-unchanged.
   - The planting stays render-only, with no colliders.
4. **Shading:** the original `.mat` bound on every slot (log the GUIDs).
   - Alpha edges stable in a short camera move at play zoom.
   - Leaf shadows are cutouts, not cards.
   - No cutaway effect on the planting.
   - No LODGroup: confirm the full mesh is what draws.
5. **Cost**, on the 15-inch Air at play zoom over the café, against the same frame before:
   - frame time
   - SetPass and draw calls
   - expected: +19,308 tris, renderers 2 → 10, submesh draws 10 → 20, about 18.9k of the added tris alpha-tested
   - If it misses 60 fps, use A's fallback (5 mounds at a 0.75 step, same scale) before anything else. Never a smaller scale.
6. **Look:** Sky judges against the old-town plaza beds and the 26 Sep paintings, not against the current domes. The aesthetic gaps get reported, and do not count as automatic failures.

## Needs Sky (not decided here)

- **Full-size ShrubMound (scale 1.0)** would be the most literal old hedge. It overhangs the coping by about 0.15 m a side onto the walk and kerb, and rises about 0.25 m above the current envelope. That is a clearance and sight change, so it needs a waiver from Sky with Keel and Vantage while the HOLD stands.
- **The old café terrace's own PottedFerns and HangingBaskets:** new world placements, which makes them a plan/layout decision.
- **Coral FlowerBed colour vs the current flamingo/peach blooms:** Sky's call from the Unity capture.

## Next owner

Aster assigns the Opus Unity/3D integration after the tram turn.

- **Inputs:** `placements.json`, the source FBX and `.mat` paths above, the evidence in `../oldtown-foliage-reuse/manifest.json`.
- **Done-check:** criteria 1–5 with captures and a console log. Then Sky judges criterion 6.
- **No production adoption** comes from this note.
