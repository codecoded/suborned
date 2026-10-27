# 072-A · DecoKit every face: the pieces (Vela, 2026-10-27)

Final list for Splice's placement (`tools/world/gen/faces.ts`) and Latch's build. Builders: `tools/decokit/sides.py` (SideBay*, SidePilaster*, CornerPier*),
`tools/decokit/alleys.py` (AlleyWall_3/_4/_3D/_4D, `low=True`), `tools/decokit/records_tower.py` (RecordsTower). Built headless with
`bun run decokit --only <ids>`; all 25 are adopted in `contracts/world/kit.json` (source generated, sha256) and copied by `bun run kit`; collide volumes in
`contracts/world/kit.collide.json`. Palette: today's material tables, nothing new registered. Pictures: `contact_sheet.png` (+ the single frames and `lods_*.png`) here.

## Axes

Numbers below are in the **kit frame** (Unity: x across, y up, z front, front = +Z; the FBX mirrors Blender x, so kit x = −Blender x, kit z = −Blender y).
`bounds` = [x0, x1, z0, z1] footprint about the pivot, as the manifest gives it.

## Storey frames

- **KIT** (whole-wall `_3` / `_4` pieces): the street fronts' frame. Ground 0–5.0 (plinth 0–0.45, teal fascia 4.0–5.0), storeys of 3.4, coping = 6.1 + 3.4 n
  (16.3 for `_3`, 19.7 for `_4`), the same as PortholeBay_3L/_4L and StreamBay_3/_4. Scale them with the building's fronts: sy = H(n) / coping
  (16.3 → 16.8, 19.7 → 20.4). Floor lines, fascia, eyebrows (0.45 proud), parapet band and coping then meet the fronts' round the corner.
- **PLAN** (stack modules `_Base` / `_Mid` / `_Top`): the plan's standards.buildings. Base = plinth 0.45 + ground 5.2 = **5.65**, Mid = one storey **3.6**,
  Top = roof crown **3.95** (an attic band: lights, parapet band, brass line, coping). Base + (n − 1) × Mid + Top = H(n) at sy 1. Each module is exactly its
  band tall (the Mid's eyebrow sits inside the band's top, not over it). Stack pivot z = the band's foot.

## Pieces

| id | storeys / band | width (kit x) | size [x, y, z] | tris L0 / L1 / L2 | mats | bounds | attaches |
|---|---|---|---|---|---|---|---|
| SideBay_3 | ground + 3 (KIT) | 6.0 | 6.0 × 16.3 × 0.85 | 2084 / 254 / 184 | 9 | −3, 3, −0.4, 0.45 | publicly seen non-front wall, 3-storey code |
| SideBay_4 | ground + 4 (KIT) | 6.0 | 6.0 × 19.7 × 0.85 | 2572 / 316 / 224 | 9 | −3, 3, −0.4, 0.45 | same, 4-storey code |
| SideBayStack_Base | 5.65 (PLAN) | 6.0 | 6.0 × 5.65 × 0.48 | 564 / 44 / 40 | 7 | −3, 3, −0.35, 0.125 | walls over 4 storeys, ground band |
| SideBayStack_Mid | 3.6 (PLAN) | 6.0 | 6.0 × 3.6 × 0.8 | 500 / 74 / 52 | 7 | −3, 3, −0.35, 0.45 | one per upper storey |
| SideBayStack_Top | 3.95 (PLAN) | 6.0 | 6.0 × 3.95 × 0.54 | 512 / 50 / 40 | 7 | −3, 3, −0.4, 0.14 | the roof crown band |
| SidePilaster_3 | ground + 3 (KIT) | 1.5 | 1.5 × 16.3 × 0.85 | 572 / 228 / 156 | 6 | −0.75, 0.75, −0.4, 0.45 | filler: the rest of a run whole 6 m modules leave (Splice's request) |
| SidePilaster_4 | ground + 4 (KIT) | 1.5 | 1.5 × 19.7 × 0.85 | 652 / 276 / 180 | 6 | −0.75, 0.75, −0.4, 0.45 | same |
| SidePilaster_Base | 5.65 (PLAN) | 1.5 | 1.5 × 5.65 × 0.49 | 232 / 48 / 48 | 4 | −0.75, 0.75, −0.35, 0.14 | filler column, per band |
| SidePilaster_Mid | 3.6 (PLAN) | 1.5 | 1.5 × 3.6 × 0.8 | 92 / 60 / 36 | 4 | −0.75, 0.75, −0.35, 0.45 | |
| SidePilaster_Top | 3.95 (PLAN) | 1.5 | 1.5 × 3.95 × 0.55 | 152 / 60 / 48 | 5 | −0.75, 0.75, −0.4, 0.15 | |
| CornerPier_Out_3 | ground + 3 (KIT) | leg 1.0 per face | 1.49 × 16.3 × 1.49 | 2180 / 640 / 384 | 6 | −0.49, 1.0, −1.0, 0.49 | convex vertex between two dressed faces |
| CornerPier_Out_4 | ground + 4 (KIT) | leg 1.0 | 1.49 × 19.7 × 1.49 | 2420 / 748 / 448 | 6 | −0.49, 1.0, −1.0, 0.49 | |
| CornerPier_Out_Base | 5.65 (PLAN) | leg 1.0 | 1.48 × 5.65 × 1.48 | 1116 / 176 / 128 | 5 | −0.475, 1.0, −1.0, 0.475 | stacked pier column |
| CornerPier_Out_Mid | 3.6 (PLAN) | leg 1.0 | 1.45 × 3.6 × 1.45 | 240 / 108 / 64 | 3 | −0.45, 1.0, −1.0, 0.45 | |
| CornerPier_Out_Top | 3.95 (PLAN) | leg 1.0 | 1.49 × 3.95 × 1.49 | 456 / 160 / 64 | 5 | −0.49, 1.0, −1.0, 0.49 | |
| CornerPier_In_3 | ground + 3 (KIT) | leg 1.0 | 1.38 × 16.3 × 1.38 | 2068 / 632 / 384 | 6 | −0.38, 1.0, −0.38, 1.0 | concave vertex |
| CornerPier_In_4 | ground + 4 (KIT) | leg 1.0 | 1.38 × 19.7 × 1.38 | 2392 / 752 / 448 | 6 | −0.38, 1.0, −0.38, 1.0 | |
| CornerPier_In_Base | 5.65 (PLAN) | leg 1.0 | 1.35 × 5.65 × 1.35 | 840 / 152 / 128 | 4 | −0.35, 1.0, −0.35, 1.0 | |
| CornerPier_In_Mid | 3.6 (PLAN) | leg 1.0 | 1.35 × 3.6 × 1.35 | 324 / 120 / 64 | 3 | −0.35, 1.0, −0.35, 1.0 | |
| CornerPier_In_Top | 3.95 (PLAN) | leg 1.0 | 1.38 × 3.95 × 1.38 | 392 / 120 / 64 | 4 | −0.38, 1.0, −0.38, 1.0 | |
| AlleyWall_3 | ground + 3 (KIT) | 6.0 | 6.0 × 16.3 × 1.30 | 3776 / 168 / 106 | 11 | −3, 3, −0.4, 0.895 | true service backs only |
| AlleyWall_4 | ground + 4 (KIT) | 6.0 | 6.0 × 19.7 × 1.30 | 4418 / 190 / 116 | 11 | −3, 3, −0.4, 0.895 | |
| AlleyWall_3D | ground + 3 (KIT), dock | 6.0 | 6.0 × 16.3 × 1.78 | 4614 / 178 / 118 | 11 | −3, 3, −0.4, 1.38 | one module in a service run (dock 1.2 m out) |
| AlleyWall_4D | ground + 4 (KIT), dock | 6.0 | 6.0 × 19.7 × 1.78 | 5254 / 200 / 128 | 11 | −3, 3, −0.4, 1.38 | |
| RecordsTower | wings 15.5, crowns 38.0 | 23.7 | 23.7 × 38.0 × 22.28 | 40927 / 7107 / 3380 | 13 | −11.85, 11.85, −11.395, 10.885 | civic-office (see below) |

L0 is full detail (no decimation). Every L1 and L2 is hand-built (`kit.asset near= / far=`): flat window quads and box bands, so windows survive the far levels.
(The auto 25 % collapse of the carved AlleyWall slab threw shards off the wall; it is no longer used for any of these.)

Materials. SideBay: Limestone_Tex, Stone_Dark, Enamel_Teal, Enamel_Seafoam, Enamel_Ivory, Enamel_Charcoal, Chrome, Gold (brass beads), GlassDark (the fronts'
window film, over a charcoal back). AlleyWall family 17 → 11: Limestone_Tex, Enamel_Ivory/_Teal/_Seafoam/_Charcoal/_Mustard, Stone_Dark, Chrome, Gold,
Window_Tower (opaque), Lamp (8 shared with SideBay; Seam → stone, Steel → Chrome, Rubber → Charcoal, Decal_Transit → Ivory, blinds → Mustard / Ivory).
RecordsTower: Limestone, Stone_Dark, Enamel_Seafoam/_Teal/_Ivory/_Charcoal, Chrome, Brass_Civic (hairlines only), GlassDark, Leaf_Civic/_CivicDark,
Bloom_Coral/_Cream (the roof planting).

## Pivot and facing (the contract)

**Wall modules (SideBay*, SidePilaster*, AlleyWall_3/_4/_3D/_4D)** — the same as the existing AlleyWall: pivot = the module's centre on the **building line**
(the wall's outer plane), at the module's foot; front +Z out of the wall; +X along it. The render face is 2 cm proud (z +0.02), the slab 0.35 m deep
behind it (z −0.35, into the building), trims up to 0.45 proud (AlleyWall fixtures 0.9, dock 1.38). Tile along x at scale 0.85–1.15 (SidePilaster
0.33–1.7: 0.5–2.55 m). Nothing stands over the coping (the AlleyWall_3/_4 riser stops under it, no cowl). KIT pieces take the building's sy; PLAN modules sy 1.

**Corner piers** — pivot = the building's vertex on the building line, at the foot. **The front (+Z) is the outward normal of face A = the face that ENDS at the
corner on a CCW ring (the incoming edge, `n0` in faces.ts), not the bisector.** Reason: the pier's footprint box is then aligned with both faces and a clip of
the box against the face lines (faces.ts `rIn` / `rOut`) gives exactly the leg, 1.0 m; with a 45° bisector yaw the box clip reads about 1.5 m and leaves
0.5 m of wall undressed on each face.
- CornerPier_Out_*: face A runs from the vertex along kit **+X** (outward +Z); face B along kit **−Z** (outward −X); the building is the quadrant x > 0, z < 0.
  The round is R 1.0 standing 0.35 proud of both faces (the plan's sharp corner lies inside it); eyebrows run on tangent to the faces' 0.45 eyebrows.
- CornerPier_In_*: face A along kit **+X** (outward +Z); face B along kit **+Z** (outward +X); the open air is the quadrant x > 0, z > 0. Cove R 0.9.
- Both take **LEG = 1.0 m** of each face from the vertex: modules start there. Scale [1, sy, 1] (the plan footprint is never stretched).
In faces.ts terms: `yaw = yawOf(n0)` where `bis` is used today; nothing else changes.

**RecordsTower** — placed as a civic dressing piece: pivot = block (64, −30) (the wing polygon's box centre), z 0 = pavement top, front +Z = the block's west
(the public face): block yawDeg 0 (plan yaw = block yaw + 90°). This is what `tools/world/build.ts recordsTower()` already does. Builder (Blender) X = block x − 64,
Y = block y + 30; kit x = −(block x − 64), kit z = −(block y + 30). It covers the wing polygon (four chamfers rounded inside the polygon)
and both 6 × 6 towers to 38.0 (chrome sphere tops). The entrances stay the block's pieces: the west face is flat stone x 59.9–68.1 up to 12.75 m for
RecordsEntrance (piers let 0.23 in), tower official's south face is flat 0–5.9 m for RecordsEntrance_Official; the service door (block 62.5, −18.9) is drawn
in the piece. At ground nothing stands more than 0.3 off the polygon but the service door's 0.15 m step (0.44 into the yard). Collide volumes are
**declared** (wing as 0.5 m strips of its outline, towers, giant-order feet, the step as `stand`): the measured 0.25 m cells overshot the bounds
(collide.test) and rounded the wall out a cell.

## Evidence (commands run)

- `bun run decokit --only <25 ids>` → all built (table above); two builds into separate folders give byte-identical FBX (L0, L1, L2 checked on 8 files).
- `bun run kit` → exit 0, 25 adopted, copied; `bun run collide` → 330 pieces, 0 errors; `bun run collide --check` ok; `bun test tools/decokit/collide.test.ts` 0 fail.
- `bun run typecheck` exit 0. Plan checker with the Handover overlay exit 0.
- Hashes went stale (RecordsTower now placed by build.ts): `bun run sim:site`, `bun run cover`, `bun run sim:site` again; `bun run cover --check` exit 0;
  `bun test tools/sim` 54/54; `tools/missions/check/check.test.ts` 36/36. Site walk grid: 2593 → 2590 cells; takedown places unchanged.
- `tools/levels/skyline/test/fixtures/kit.digest.json` regenerated (adoption).

## Handoffs / open

- **Splice**: pier yaw `bis` → `n0` (above). Fixture sizes in faces.fixture.ts can take the table. `tools/decokit/export_kit.py` make_lods: one line changed so a
  hand-built L2 is made under the 1500-triangle floor (only these pieces are affected; every other far= piece is over it) — please keep it through your edits.
  dress048 "office roof terrace" expects [] once RecordsTower is the office; the world still generates the office roof (yours).
- **Latch**: SideBay windows use DK_GlassDark (transparent film) like the fronts; AlleyWall uses the opaque Window_Tower. A white-fronted building (PortholeBay,
  Render_White) may want its side bays' Limestone_Tex slot swapped to DK_Render_White (KitSlots.Swap) — Vela's call on the pictures.
- **Redline**: per-module L0 is 2.1–2.6k (bays), 2.1–2.4k (whole piers), 0.1–1.1k (stack modules), 3.8–5.3k (AlleyWall), 40.9k RecordsTower.
- The contact sheet is Blender Eevee, not the game light; the in-game read is Latch's shots (072-E).
