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

---

# Pass 2 (Vela, 2026-10-27): shop fillers and opaque side windows

Aster's second piece pass. (1) Splice's finding: 248 own-shopfront tenancies stand in front of calm side modules (225 street remnants on buildings with street
bays, 30 on sheds, 7 where no bay fits). Under Sky's "a)" a shopfront run keeps its shopfront, so these get frontage pieces. (2) Redline's look and cost item:
the side pieces' windows are opaque. Builder: `tools/decokit/shopfill.py` (new family, in `kit.FAMILIES`), window change in `tools/decokit/sides.py`. Pictures:
`fillers_sheet.png`, `fillers_close.png`, `fillers_lods.png` (Blender Eevee, sky over a warm street; not the game light).

## Pieces

| id | frame / band | width (kit x) | size [x, y, z] | tris L0 / L1 / L2 | mats | bounds | attaches |
|---|---|---|---|---|---|---|---|
| ShopFill_3 | ground + 3 (KIT, coping 16.3) | 3.0 nominal, scale 0.5-1.7 (1.5-5.1 m) | 3.0 × 16.3 × 3.78 | 1024 / 206 / 152 | 10 | −1.5, 1.5, −2.15, 1.63 | a shop run on a 3-storey wall (whole-wall column) |
| ShopFill_4 | ground + 4 (KIT, coping 19.7) | same | 3.0 × 19.7 × 3.78 | 1174 / 234 / 166 | 10 | −1.5, 1.5, −2.15, 1.63 | same, 4 storeys |
| ShopStack_Base | 5.65 (PLAN Base band) | same | 3.0 × 5.65 × 3.83 | 506 / 86 / 74 | 8 | −1.5, 1.5, −2.15, 1.68 | the ground row of a shop run on a stacked wall (over 4 storeys, sheds' stacks) |

No single-storey shed piece: the 30 shed tenancies stand on sheds 12-18 m tall that already take SideBay_3 (sy 0.92-1.10) or a Base/Mid/Top stack, so ShopFill_3
or ShopStack_Base covers them with the same column. Kit tags: use `face:shop` (never `facade`: facades.ts does not take them as street bays), scale 0.5-1.7,
squash 0.2; lod category `facades`, lodClass `bay3` / `bay4` / `storey` (tools/kit/tags.ts, lod.ts).

## What they are (and how they meet the fronts)

- **ShopFill_3 / _4**: the street bays' ground storey as a wall module: granite plinth 0-0.45, a 0.4 half pier at each joint (a 0.8 pier forms against a street
  bay or the next filler, as between StreamBays), glazed shop 0.45-4.0 at 0.3 behind the line (DK_GlassDark, brass frame and one mullion, transom 2.75), granite
  shop floor, DK_Lobby back wall 2.0 m in, **teal fascia 4.0-5.0 with brass beads at 4.06 / 4.94, ivory canopy 3.85-4.0 reaching 1.6 m with a chrome nose**:
  the exact heights and section of skyline.py `ground_trims` (PortholeBay_3L/_4L, StreamBay_3/_4). Above: per storey one window (1.5 m, centred) under the side
  bays' seafoam sill band and the 0.45 ivory eyebrow, then the KIT parapet band and coping (sides.py modules, kind "one"): floor lines, eyebrows and coping meet
  the side bays and the corner piers. Scaled with the building like every KIT piece: sy = H(n) / coping, so the fascia and canopy stay level with the street
  bays' at the same sy.
- **ShopStack_Base**: the same shopfront in the 5.65 m Base band. The tall street bays (PortholeBay_7, StreamBay_6/9, ...) stand at sy = H(n) / coping = 1.039-1.047
  for 5-10 storeys, so the band carries their ground at sy 1.043: plinth 0-0.45, glass 0.45-4.17 (transom 2.87), **canopy 4.016-4.172 reaching 1.65 m**, and the
  **fascia from 4.172 to the band top 5.65** with brass beads at 4.235 and 5.155 (the tall bays' two) and 5.59 (SideBayStack_Base's top bead). Its foot meets the
  street bays' fascia and canopy, its top meets SideBayStack_Base, SidePilaster_Base and the CornerPier_*_Base fascias (4.65-5.65), so it sits against either.
- L0 full detail. L1 / L2 hand-built: shop glass as an opaque DK_Stone_Dark quad (the dark shop at distance; no see-through, and a material of L0's set), fascia,
  canopy, plinth and bands as boxes, the upper windows as in the side bays. Brass is beads and frames only.

## Opaque side windows (Redline's item)

SideBay_3/_4 and SideBayStack_Base/_Mid/_Top now draw their windows and portholes in **DK_Window_Film**, a new opaque enamel (#4F6B6B, clear coat), in place of
the transparent DK_GlassDark film over a DK_Enamel_Charcoal back. Its colour is what the fronts' film shows at the play camera (DK_GlassSky: glaze #4A8588 lerped
0.41 toward its street mirror #A78F78, alpha 0.72 over charcoal, about #5F726E unlit), a step darker because the enamel is lit. Being Complex Lit it takes the
cutaway shader, so it fades with the wall in the occlusion cut (FadeTags skips transparent queues), and it drops the blended layer (Redline: 0.09-0.31 ms).
L1: a chrome frame quad with the film inset; L2: the film quad. SideBay 9 → 8 materials (charcoal and GlassDark gone), the stacks 7 → 6. AlleyWall keeps
DK_Window_Tower (its own look, unchanged). The pilasters and piers have no windows (unchanged, same hashes). The shop fillers' upper windows use the same film;
their **shopfront glass stays DK_GlassDark like the street bays'**, so a shop run reads the same along its length (and has the same unfaded-glass behaviour in
the cut as the street bays: open item for Redline / Latch below).

## Pivot and facing

As SideBay (above): pivot = the module's centre on the building line, at its foot; front +Z out of the wall; +X along it. The footprint runs from 2.15 into
the building (the shop's back wall) to 1.63 / 1.68 out (the canopy's nose), so kit.json gives `pivotOffset` [0, −0.26] (ShopFill) and [0, −0.235]
(ShopStack_Base): faces.ts `centreOf` already handles it. KIT pieces take the building's sy (scale [sx, sy, 1]); ShopStack_Base takes its band's sy.
Nothing stands over the coping. The shop box (piers, head, floor, back wall) sits inside the mass where KitCover removes the procedural wall.

## Placement rule (for Splice, tools/world/gen/faces.ts)

1. **Shop run**: an every-face run (side, pilaster or stack modules today) on a face where an own-shopfront street tenancy stands: a street-level frontage of this
   building with no `facade`, whose a→b lies on the run's line (within shareTol) and overlaps it by more than 0.5 m. The whole run becomes a shop run (tenancies
   fill a street edge end to end, so the run is a shopfront run along its length).
2. **Whole-wall column (3-4 storeys, sheds with SideBay_3/_4)**: keep the column the side run would take (same code, foot and sy = (z1 − foot) / coping) and use
   ShopFill_n for SideBay_n. Tile R with ShopFill alone: k modules of 3 m × sx, **k nearest R / 3 (the piece's own 3 m, not FACES.module 6)**, sx in 0.5-1.7, so every
   R ≥ 1.5 m tiles and no pilaster is needed. R under 1.5 m: SidePilaster_n as today (too narrow for a shopfront).
3. **Stacked column (over 4 storeys, sheds on a Base/Mid/Top stack), foot on the pavement**: the ground row is laid on its own. Drop the run's base-band modules
   (SideBayStack_Base, SidePilaster_Base) and tile the run's length with ShopStack_Base at z = the base band's foot, sy = the base module's sy, widths by the same
   rule as 2 (k nearest R / 3, 0.5-1.7). Mid and Top columns stay as they are; their 6 m / 1.5 m joints need not meet the 3 m shop row (the canopy and fascia
   run across the seam, as a shop row does under upper bays). A stacked run whose foot is above the pavement has no shop row.
4. **Corner piers**: unchanged. A shop run starts and stops at a pier's 1.0 m leg like any run; its canopy ends against the pier.
5. **Link the tenancy**: set `frontage.facade` to the shop module whose span holds the tenancy's midpoint (as the street bays do), so ProcShop draws no
   procedural shell or canopy there and keeps the sign. Your shopfit clamp then applies only to tenancies left unlinked (if any). Fascia foot for the clamp table:
   ShopFill 4.0 × sy, ShopStack_Base 4.172 × sy.
6. Family / instancing: ShopFill → `side` by familyOf's default (or a new `shop`, your call), ShopStack_Base → `stack-base` by the regex; instance key piece|colourway
   as for the rest. The face log can say `shop`.

## Evidence (commands run)

- `bun run decokit --only SideBay_3,SideBay_4,SideBayStack_Base,SideBayStack_Mid,SideBayStack_Top,ShopFill_3,ShopFill_4,ShopStack_Base` → exit 0, rows in the table;
  then `--only ShopFill_3,ShopFill_4,ShopStack_Base` twice more (L1/L2 fixes); the materials table kept every row (275, DK_Window_Film added: Splice's merge).
- `bun run kit` → kit.json written, 381 assets, adopted 295 (the three set adopted), copied into unity/Assets/Suborned/Kit (DK_Window_Film.mat written).
- `bun run collide` → 333 pieces, 8 measured; `bun run collide --check` ok; `bun test tools/decokit/collide.test.ts` 6 pass.
- Hashes moved: `bun run sim:site`, `bun run cover`, `bun run sim:site`; `bun run cover --check` exit 0 (cover changed only its site and kit hashes).
- `bun run typecheck` exit 0. `bun tools/plan/cli.ts --overlay tools/missions/handover/overlay.ts` exit 0.
- `bun test tools/kit tools/levels/skyline/test/fixture.test.ts` 15 pass (kit.digest fixture regenerated); `bun test tools/world/gen/{faces,kit020,kitrefs,shopfit}.test.ts
  tools/sim tools/missions/check/check.test.ts` 119 pass.
- Not mine, failing before and after: 5 of `tools/levels/skyline` (stubbed runs, units): C-0 fails on the test's pinned civic block (café height), not on kit.

## Open (pass 2)

- **Splice**: place by the rule above; then `bun run world`, `world:faces`, and the counts per piece. The world export does not place the shop pieces yet.
- **Latch**: DK_Window_Film is a new Complex Lit enamel (unity_mats wrote the .mat); the SideBay/stack prefabs change materials (no GlassDark). Rebuild after Splice.
- **Glint**: DK_Window_Film [S] #4F6B6B with the enamel clear coat: in Blender's test light the coat mirrors the world in shade and can glint near the mirror angle
  (as DK_Window_Tower does); judge it against the fronts' film in the game light and tell me the value.
- **Redline**: the shopfront glass of the shop pieces (as on the street bays) is still transparent DK_GlassDark at L0, so the cut leaves it unfaded on the ground
  storey there too; if that shows, an opaque shop-glass variant is a one-line change for both the street bays and the fillers (a palette/look call, ask first).
