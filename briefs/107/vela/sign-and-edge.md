# Brief 107 round 2 · the café sign lit, the inlay edge light (Vela, 2026-11-11)

Sky asked for two things:
- "The cafe sign needs lighting at night as well."
- The seafoam bands should look like the night concept (`briefs/107/concepts/night.png`): dark bands with a thin line of light along their edges. The flat lit band was "that blue circle".

Board: `sign-and-edge-board.jpg` (this folder, git-ignored). Its rows:
1. Sign by day: shipped | lit.
2. Sign at night: shipped | lit.
3. Inlays by day: today | with the edge strips.
4. Inlays at night: today | edge light.
5. The fountain rings at night, closer: today | edge light.

## 1 · The café sign: one roadlight.json line, no kit step, no rebake

**The sign is not merged.** Glint's note says the old art's city chunks swallowed it, but that is a different building. The café in the zone is ours: `CafeBay_v2`, restyled in brief 075. Its blade sign is `stacked_sign("CAFE", …)` in `tools/decokit/cafe_grand.py`:
- the body is `DK_Enamel_Teal`;
- the letters are **`DK_Enamel_Ivory`**, not Cream;
- the edge bead is `DK_Chrome`.

In `OldTown.unity` the sign sits on the piece's own renderer, `DK_CafeBay_v2`. It has a LODGroup, with LOD0 on that object and LOD1 on its child `__LOD1`. It is enabled, its static flags are 0 (lit by probes, not lightmapped), and it holds 10 slots. Slot 10 is `DK_Enamel_Ivory`, and that slot draws only the letters: Ivory is used nowhere else in `CafeBay_v2`. RoadLight's `renderer` filter checks the renderer's own name and three parents up, so `"CafeBay_v2"` catches both LODs and nothing else. There is one `DK_CafeBay_v2` in the scene.

So the sign needs no new material and no change to the merge. A new `DK_Sign_CafeLetters` slot would mean re-exporting `CafeBay_v2` (a new FBX sha, a `kit.collide.json` row and its AO pass) for the same result, so I didn't make one.

**Glint: replace the `BladeSign_Cafe` entry in `roadlight.json` with this line.** The old entry lights the cream bodies of 16 kit `DK_BladeSign_Cafe` signs outside the zone as flat panels. Those signs already glow through their `DK_Neon_*` entries.

```json
{ "material": "DK_Enamel_Ivory", "renderer": "CafeBay_v2", "mode": "night", "colour": "#ffeccc", "evening": 1.2, "night": 3.2, "unbake": true, "note": "Vela 107 r2 (Sky: 'the cafe sign needs lighting at night as well'): the café's CAFE fin (CafeBay_v2's stacked sign; its letters are the piece's only DK_Enamel_Ivory slot, on its own renderer, not merged), lit on the letters only; the testbed's channel letters are 3.2 at night" }
```

`unbake` changes nothing here because the renderer isn't lightmapped. It is kept in case a later bake maps it. The whole copy is `sign-and-edge/roadlight.sign.json`.

**Proof in the built player** (0.2.320, `-roadlightJson` loose copy, `-shots`, route-a-board tick 600, focus on the waitress, yaw 0; frames in `out/scratch/vela/107b/sign/`):
- The log shows `DK_Enamel_Ivory:night` among the dusk lights. The night frame shows the CAFE letters lit white on the dark teal fin (board row 2).
- **Day is the same.** I ran fresh day-only sessions: shipped, lit, and shipped a second time. The sign crop's difference between lit and shipped (max 46, 952 px over 8) is within run-to-run noise (shipped against shipped: max 84, 717 px). RoadLight never swaps a material until the look leaves day.
- **One caveat, Glint's, and not new.** If a session goes from night to day, the letters fade to black over `easeSeconds` (1.5 s), but the night copy is still the one drawn. That copy has `_DETAIL_MULX2` and `_ENVIRONMENTREFLECTIONS_OFF` dropped, which is true of every `night` entry. Crops: `sign-and-edge/sign-crops-night-session.png`. It matters only if the look can change mid-mission.

## 2 · The inlay edge light: a kit candidate, one kit step and the town rebake

**What it is.** Each of the five inlay pieces gains a thin flat strip along each long edge:
- the strip runs from the border 0.06 m **into** the band, 1.5 mm over the enamel;
- its UV v is the distance from the border in metres.

It draws in a new glow material, **`DK_Glow_InlayEdge`**: Suborned/Cross Glow `_Mode 2` (field), so k = (1 − |v| / `_Radius`)^1.5 with `_Radius` 0.06. The light is brightest on the border and fades to nothing 6 cm into the band. The band itself stays dark at night, since `DK_Inlay_SeafoamNight` stays at 0. The 2 m module ends get no line, so a run of bands reads as one band with two lit edges. The arcs use exact radial normals, so the quarters meet square.

| | value |
|---|---|
| material | `DK_Glow_InlayEdge`, kind `glow`, row `#B4F5E2`, strength 2, alpha 0.6 (night) |
| Unity .mat | `unity/Assets/Suborned/Materials/Glow/DK_Glow_InlayEdge.mat` (+ .meta, new guid `ef9aded1…`): a copy of `DK_Glow_PlanterFoot.mat`, `_Mode 2`, `_Radius 0.06`, `_BaseColor` (0.7059, 0.9608, 0.8863), `_Strength 2`, **`_Alpha 0`** |
| by day | `_Alpha` 0 adds exactly 0. In Blender, day today against day with strips: 14 pixels differ, by 1/255 |
| footprint, size, pivot | unchanged. The strip lies inside the band, and a new `height=` on `kit.asset` keeps the catalogue height at the stone's 4 mm, so the world export's civic trim masses do not change |
| triangles | +16 a band, +40 R2, +56 R4, +80 R9. The zone places 12 bands, 16 R2, 4 R4 and 4 R9, which is **+1,376**. CD-3 goes from 362,909 to about 364,285 of 370,000 |

**Glint: the entry for `roadlight.json`.** It uses the existing `alpha` mode. These are starting values; tune on the loose copy:

```json
{ "material": "DK_Glow_InlayEdge", "mode": "alpha", "evening": 0.3, "night": 0.6 }
```

- The peak is 2 × 0.6 = 1.2 × colour, additive.
- In the Blender frames the line reads clearly but stays quiet at 0.6 (rows 4 and 5). Up to 1.0 if Sky wants it brighter.
- Keep the `DK_Inlay_SeafoamNight` entry at 0.

**Renders:** `renders-r2/` (git-ignored), made by `tools/decokit/review107_glows.py`. They show the café square as the world export places it, in EEVEE. Night uses stand-in lamps; Glint's Unity night decides the final levels.

## Exact next steps (one kit step and one rebake, Latch)

1. **The inlays into the generated root:**
   `bun run decokit --only InlayBand_06_2,InlayBand_12_2,InlayArc_R2_W06,InlayArc_R4_W06,InlayArc_R9_W12`
   This takes about 57 s.
2. **The catalogue:** `bun run kit --extra out/decokit-107-extra`, as in round 1.
   - Dry run: `bun out/scratch/vela/107b/dry.ts`. The five inlay rows stay `adopted` and change only in `tris`, `materials` (+`DK_Glow_InlayEdge`) and `files` (new FBX sha).
   - `size` and `pivotOffset` are unchanged.
3. **Collision table:** `bun run collide`.
   - `tools/decokit/collide.test.ts` fails today for 19 pieces: round 1's 14 planters and 5 inlays. `kit.collide.json` (last changed in b97ad75f4) was not refreshed after round 1's kit step (6274e7ec8).
   - This run clears those and the five new inlay hashes together.
4. **Commit** `Materials/Glow/DK_Glow_InlayEdge.mat` and its .meta. They are already written; `bun run kit` warns that kind `glow` has no .mat in any root, which is expected.
5. **`roadlight.json`** (Glint): the two lines above. Replace the `BladeSign_Cafe` entry, and add the `DK_Glow_InlayEdge` entry.
   - The sign line works on 0.2.320 as it stands. It can go into Sky's next build with no rebake.
6. **The town rebake:** one, for the inlays.
   - The strip is a second, transparent slot on the inlay renderer, not a separate object.
   - Cross Glow has no shadow-caster or meta pass, so it neither casts nor bakes.
   - Like the planter foot, keep it out of anything that measures renderer bounds. Its bounds are the band's, so nothing new is needed there.
7. **Done-check:** the batch's night/day pair.
   - At night: the band edges lit as thin lines, the bands themselves dark, the CAFE letters lit.
   - The day shot unchanged.

## Files

**Changed:**
- `tools/decokit/civic_paving.py`: `edge_line`, and `height=PROUD` on the five inlays.
- `tools/decokit/glows107.py`: the `EDGE_*` values and `DK_Glow_InlayEdge`.
- `tools/decokit/kit.py`: `asset(height=)`.
- `tools/decokit/export_kit.py`: two lines that apply it.
- `tools/decokit/review107_glows.py`:
  - the mode 2 glow;
  - the band's own glow is read from `roadlight.json`;
  - the ring view.
- `contracts/world/civic-look.json`: the `DK_Glow_InlayEdge` palette row.

**New:**
- `unity/Assets/Suborned/Materials/Glow/DK_Glow_InlayEdge.mat` (+ .meta);
- `briefs/107/vela/sign-and-edge/` (the roadlight copy and the sign crops).

**Built, not committed:** `out/decokit-107` (review root, the five inlays).

**Checks:**
- `bun test` over civic-dress, `tools/world/civic`, `tools/kit` and `tools/decokit`: 126 pass, 1 fail. The fail is the round-1 collide staleness above.
- CD-4 on the post-adoption materials: 74/74 (`out/scratch/vela/107b/cd4.ts`).
- `bun run typecheck`: clean.

Nothing was adopted and nothing in Unity was opened or rebaked. The player runs went through `bun run guard`.

**Next owners:**
- Glint: the sign line now; the edge level after the rebake.
- Latch: steps 1 to 6.
- Glyph: one look at a night frame where a sphere lamp's edge rings meet a teal clock ring.
