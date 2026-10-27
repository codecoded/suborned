# Brief 072 · DecoKit on every face, the Records Office landmark, the sky's planets, and a looser style (proposal)

Owner: Aster. Opened 2026-10-27 from Sky's answers the same day. Branch `work/2026-10-21-fight-and-testbed`.

## Why
Sky: "I really miss the brilliance of DecoKit and quality we had there." · "DecoKit played fine though on the Air in FPS walking through the street" · "our play cam means we see a lot of the sides of the buildings".

Vela's finding (2026-10-27, files uncommitted in the tree, renders in `out/scratch/vela/`, `block_compare.png`): 79 % of the wall area seen in the play streets is the flat procedural facade (`unity/Assets/Suborned/Editor/World/Proc/*`: one swatch colour per face, flat shaded, no bevels, no maps). Kit fronts (PortholeBay_3L/_4L, CafeUpper_3/_4, street bays) cover only street-facing edges. Her estimate for the play streets with kit on every visible face: +0.15–0.4 M tris on ~1.6 M (limit 2.5 M), +~70 SetPass on ~105 (limit 375). The whole district with kit on every wall is ~4,200 modules, 17.6 M tris placed: it fits only through L1/L2 or coarser.

## Sky's decisions (2026-10-27, verbatim)
1. Side-wall look: **"a)"**: a new calm frontage-style side bay wherever the public sees a wall; service walls (pipes, AC, docks: the AlleyWall family) only on true service backs.
2. Scope: **"every"**: DecoKit kit on every face of every building in the whole district. 60 fps at play zoom on the 15-inch Air M4 is the engineering problem: Redline costs it; LODs, combining and instancing are designed to fit. The flat procedural facade is no longer visible anywhere at play zoom; it may survive only as a far LOD or impostor if Redline shows it is needed.
3. Records Office tower as a landmark kit piece: **"yes"**.
4. Planets and style: **"I think our style kit is too restrictive - the original decokit looked great, though I even loosened the desaturated hue as well a little"**. Keep the old DecoKit planet colours. Loosen the style kit back toward the original DecoKit (style contract palette_world, materials, notes; GDD §15; facade.json families and area palettes; restrictive rules such as no blue tint in glass). A design change: shown to Sky as pictures first; **no GDD or style-contract edit until Sky OKs them.** Kit work that does not depend on the palette goes ahead now.

5. Texture, added the same day: **"Anywhere with a flat color, no texture, we need to give it one, as long as it doesn't hurt performance"**. Scope: every surface that renders as a flat colour with no texture: the procedural facade swatch atlas (FacadeLit, Atlas.cs: 16×16 point-sampled cells, still drawing far LODs, roofs, megastructures, gate towers and the civic style kit), untextured DecoKit rows (enamel, metal and emit materials that unity_mats.py writes with no maps), roads, ground, roofs, plinths, and any primitive or stand-in material. Performance is a hard condition: neutral, or measured within 064 A5. Shared tiling detail and normal maps or texture arrays come before unique textures; reuse DecoKit's maps (limestone, enamel, metal); keep the SRP batcher and instancing keys; no new SetPass per colour; stay inside a stated texture-memory budget. Colour stays under today's rules until Sky OKs the style proposal: this is texture, not palette (objective 072-F).

GDD note (Aster): decision 2 replaces §10's "DecoKit kit where it counts, procedural fill to the same bar" and §09's "dressed by a DecoKit piece … or procedural to the same bar". The GDD change is held and made once, as one new version, together with the style loosening after Sky's OK on the pictures. The decision log carries all four now.

## Frame budget (read first)
Brief 064 A5 holds: warm mean ≤ 13.0 ms, p99 ≤ 16.7 ms at 1920 × 1080 on the Air. The play view is estimated at ~12.7–14.2 ms warm at 1080, at or just over the line, and brief 068 made asset work frame-neutral until the warm 1080 baseline exists. Sky's "every" is a deliberate spend; this round has to show where the frame comes from (LODs, combining, instancing, the 064 A3 settings items) rather than ignore the line. Full detail stays at L0 (GDD §12): distance LODs are allowed and listed; no new decimation of L0, triangle caps or texture downscaling without Sky.

## Piece names (the contract between Vela and Splice)
Module width 6 m on plan, tiled at 0.85–1.15 scale; storey heights from plan standards; `_3` / `_4` = storeys.
- `SideBay_3`, `SideBay_4`: the calm frontage-style side bay (no shopfront; windows, bands, panelling).
- `SideBayStack_Base`, `SideBayStack_Mid`, `SideBayStack_Top`: the same bay as per-storey modules for walls taller than 4 storeys (towers beyond the play streets).
- `CornerPier_Out_3`, `CornerPier_Out_4` (+ a `_Mid` stack module): rounded outside-corner pier (Sky's "curvy" rule).
- `CornerPier_In_3`, `CornerPier_In_4` (+ `_Mid`): inside corner.
- `AlleyWall_3`, `AlleyWall_4`, `AlleyWall_3D`, `AlleyWall_4D` (D = with a dock or service door): service backs only.
- `RecordsTower`: the Records Office landmark (crown 38 m, the block's footprint and entrances unchanged; `civic-block.json` and C-0 stay as they are).
Vela may add or rename variants; she tells Splice and this brief records the final list.

## Objectives

### 072-A · Vela (pieces) · palette-independent kit
Build the pieces above in `tools/decokit` (Blender, headless), each with L0/L1/L2, materials from today's tables and no more than ~10 materials for the AlleyWall family (shared with the side bay where possible). Adopt AlleyWall_* and the new pieces through the catalogue (`contracts/world/kit.json`, status adopted, sha256) with Splice. RecordsTower matches the Office's footprint, entrances and crown and reads as the one tall landmark.
- Done-check: `bun run decokit --only <pieces>` builds them; `bun run kit` and the collide check pass; per piece tris L0/L1/L2 and material count in a table; a contact sheet (play zoom 14 at 38°, yaw 30 and 210) in `briefs/072/pieces/`.
- Evidence: commands and output; the table; renders.
- Next: Splice (placement), Latch (build).

### 072-B · Vela (style) · the loosening proposal for Sky (no contract or GDD edit)
Compare the original DecoKit (`~/Projects/the-suborned`: `workbench/Tools/Blender/decokit`, `game/Assets/TheSuborned/Art/DecoKit`, its materials table and renders) with today's `contracts/style/suborned.style.json`, GDD §15, `unity/Assets/Suborned/Editor/World/facade.json` (families, area palettes) and `contracts/world/civic-look.json`. List exactly which rules and values hold the look back (rule or key, today's value, DecoKit's value, proposed value). Render a before/after of a play street (today against loosened), at play zoom, yaw 30 and 210, plus one close shot, with the loosened values applied only in a scratch override. Write `briefs/072/style/proposal.html`, self-contained (images embedded), short.
- Done-check: the table covers every rule that differs; the before/after is the same view, light and geometry; nothing under `contracts/style/` or `GDD/` changed.
- Next: Aster shows Sky; on Sky's OK: Vela edits the contract and facade.json, Aster versions the GDD.

### 072-C · Splice · pipeline
1. Fix `bun run decokit --only X` rewriting the shared materials table with only that run's materials (merge, never shrink; a test).
2. The every-face pass in `tools/world/gen/facades.ts`: every building edge not already dressed by a frontage piece and not shared with a neighbour gets kit: SideBay on publicly seen walls, AlleyWall only on true service backs (define the rule in code from the plan: lanes, service yards, backs facing another building's back within a set distance), corner piers at every outside and inside corner, stack modules above 4 storeys, RecordsTower on the Office. Deterministic, seeded by id. `KitCover.Spans` removes the procedural wall under kit; review the `kitFaced` crown check. The export says per face which piece dresses it, so "no flat face at play zoom" is checkable: add a check that fails if any wall face in the district has no kit.
3. Emit what Redline's LOD plan needs (per module: LOD group, combine group per block or cell, instancing key).
- Done-check: `bun run plan`, `bun run world`, `bun run kit`, `bun run typecheck`, `bun run test` pass; the coverage check reports 0 undressed faces; counts of modules placed per piece; same plan → same world (two runs, same hash).
- Next: Latch.

### 072-D · Redline · cost and LOD plan, measured
Measure on the Studio (counts are machine-independent; ms are labelled Studio) the current scene at play zoom yaw 30/120/210/300 and at the old DecoKit boulevard reference: triangles, SetPass, batches, renderers, shadow casters, frame ms. Then measure the every-face scene once Latch has it. Design the fit: LOD distances and screen-size thresholds per piece class, static combining or HLOD per block, GPU instancing / SRP batcher keys, shadow-caster policy for far modules, whether the procedural facade or an impostor is needed as the far LOD. Fold in brief 064 A3's settings savings with their 1080 estimates. Specify the Air A/B run that would confirm it (needs Sky's OK; don't run it).
- Done-check: a table before → after for each view (Studio, measured), the plan with numbers, the projected warm Air mean against 13.0 ms with the model shown, and the Air run spec.
- Next: Splice/Latch implement the LOD and combine plan; Aster asks Sky for the Air run.

### 072-E · Latch · build and shots
Rebuild `HarbourDistrict.unity` from the new export with the adopted kit (`bun run kit` copy), apply Redline's LOD/combine plan, Unity compile and console clean. Shots at play zoom 14 at 38°, yaw 30 and 210, from the café and Records Office front, plus one framing of the planets (Horizon_PlanetRinged, Horizon_Moon) in the sky. Before/after where possible. Shots in `briefs/072/shots/` (git-ignored).
- Done-check: compile and console (0 errors, warnings listed); shots; Redline's counts taken on this scene.
- Next: Redline (measure), Aster (Sky).

### 072-F · Vela (textures, materials) + Latch (shader, builder) + Redline (cost) · no flat colour left
1. The list first (Vela, with Latch's per-material screen area from the play views): material or shader · where it shows at play zoom · share of screen area at yaw 30/120/210/300 · has maps or not. Committed as briefs/072/texture/flat.json + flat.md.
2. Texture the biggest-area surfaces first: one shared tiled detail and normal array indexed by surface class in FacadeLit (the swatch tint kept), DecoKit's own maps for the untextured kit rows, roads, ground, roofs, plinths.
3. Redline measures before and after on the same scene: SetPass, batches, texture memory (budget stated up front), Studio ms; the Air A/B goes in the same Air run as every face.
- Done-check: every row on the list textured or listed with a reason; counts neutral (no SetPass growth from colour, SRP batcher compatible); texture memory within the budget; play-zoom shots before and after.

Datum or Vantage only if the plan or block layouts must change (they should not).

## Rules for this round
Shared tree: never stash, checkout, reset or clean. Kill by PID only. No paid API calls (Codex stills are free). Checks under 10 minutes with `bun run test`. No heavy load while Sky plays (`bun run guard`). No deploy, no Air run without Sky. Crew don't commit; Aster commits checkpoints with this round's files only.

## Sky's answers to the round's questions (2026-10-27)
- Q1a palette loosening as rendered: **"yep, more, more reds / blues allowed as well"**: approved and pushed further; more reds and blues as accents and in world colour. Signals still don't own red, amber or violet (Sky, 2026-10-16).
- Q1b DecoKit's deep teal band back on the Records Office: **"yep"**.
- Q1c road: **"keep powdblue - feels more modern"**: the powder-blue road stays; no asphalt A/B.
- Q1d grade toward DecoKit's: **"yeo"** (yes); Glint.
- Q2 the Air sitting: **"yeah wiat"**: ask for it once 064's no-look settings and the window-shadow fix are in one development player.
- Q3 window shadows: **"ok"**: Vela cuts window holes in the low-detail shadow copies; shadows stay on the copies; Redline confirms the cost.
- Q4 balcony awnings: **"ok"**: stay at 0; an awninged side-bay variant only if the play shots read flat.

### Follow-up (072-G)
1. Vela: the loosened palette into contracts/style/suborned.style.json, facade.json and the look sources (civic-look.json regenerated, never hand-edited), with more reds and blues and the Office's teal band; a before/after render.
2. Aster: one GDD version for brief 072 (§15 style, §09/§10 every face, texture on flat colour); `bun run crew`, `--check`.
3. Glint: the grade toward DecoKit's; detail strength for stone and roofs (the textured Records Office reads blotchy, not honed).
4. Vela: window holes in the shadow copies; Redline measures.
5. Latch: rebuild, offscreen capture only, the compare views.

## Status
- 2026-10-27: brief, decision log and the planet/moon rebuild committed (96176fed). Phase A out: 072-A Vela (pieces), 072-B Vela (style proposal), 072-C Splice, 072-D Redline phase 1, 072-E Latch baseline.
- 072-A done: 25 pieces adopted (briefs/072/pieces/pieces.md; the names grew: SidePilaster_*, CornerPier_*_Base/_Top), plus pass 2: ShopFill_3/_4 and ShopStack_Base for street-facing shop stretches, and opaque side windows (DK_Window_Film). Commits e8614ede, a405be1e.
- 072-B done, waiting on Sky: briefs/072/style/proposal.html, rules.json (51 rows). Finding: only 4 of DecoKit's 113 materials differ; what holds the look back is the written rules (brass hairline cap, no blue glass, near-white walls), the flat walls, the grade and the powder-blue road.
- 072-C done: --only never shrinks the tables; faces.ts dresses every wall (1,128 faces, 0 undressed, `bun run world:faces`); shopfront boxes clamped, then replaced by fillers (0 clashes); the skyline drift declared. Aster's rulings: shopfront awnings were never lost; the 603 balcony awnings on the old flat facades stay at 0 this round.
- 072-D phase 1 done: briefs/072/redline/plan.md. Ortho play camera: LODs never switch at play zoom, so the frame bounds the cost (+11–53 k view tris at zoom 14); shadows are the risk (L0 casting +0.2–1.9 M), so modules cast from L2 proxies; no combining; stack columns baked. Projected warm Air mean at the opening ~12.5 ms (11.0–14.0) with 064 A3's no-look items, against 13.0.
- 072-E: baseline captured (briefs/072/latch/baseline/); planet maps fixed (6d7772b2); builder plan in (db30a0af; trial build passes). The after capture waits for the Studio to be free (it needs a visible editor window, and Sky was using the Mac).
- Keel: the Office's sim footprint is the block polygon (dd2d6cfe); the Handover ships (893 plays). c040 re-record (Waymark) once the kit settles.
- Corner fix: K-6, no facade piece outside its building (1,648 → 0), export v4 a50c649a (a75057ae). Fillers placed, 0 shopfront boxes against side pieces (be0f1657).
- 072-E after capture, offscreen in batchmode (the Studio was in use, so no visible editor): briefs/072/latch/after/, shots in briefs/072/shots/after/ and compare/ (95bdbe0f). Every face against today on the same build: +1 k to +51 k main-view tris, shadow tris −0.3 k to +8.4 k, SetPass estimate unchanged, materials +0–2, 0–29 modules in view.
- 072-D phase 2: briefs/072/redline/phase2.md. Modules in view match the census exactly; full-detail district total 9.73 M tris (not the assumed 22.6 M). Air projection at the opening: every face +0.04 to +0.21 ms; ~13.55 ms today (12.7–14.4); ~12.2 ms (10.9–13.5) once 064 A3's no-look items land, against 13.0. Air time not asked for yet: one sitting once 072-F, A3, F1 and a CPU check are in one player.
- 072-F (decision 5): briefs/072/texture/ (flat.md, design.md, area.json, area-after.json); briefs/072/latch/textured/report.md. Flat colour 43.9 % → 11.7 % of the frame (what is left is mostly excluded on purpose: dark glass, the crossing pool, contact shades); +16 MiB of the 24 MiB budget; SetPass estimate +1 to +3 (Redline's cap +4); tris, materials and casters unchanged; a runtime switch `surfaceDetail` for the Air A/B (79283d9f).
- Redline's fixes in (79283d9f): shell inset, mip derivatives, 16-bit columns, the cull index, crowns at L0. F1 is still open: the solid L2 shadow proxy turns windows near-black (briefs/072/shots/f1/); with L0 shadows they read lit.
- Queued: the UnityStats Game-view capture (needs the Studio idle); `surfaceDetail` into perf-072.json (Redline); Glint on F1 and the detail strengths; Waymark re-records c040; the style proposal and the Air sitting wait on Sky; GDD version after Sky's OK on the pictures.
- 072-G done: the palette applied, style contract v0.22.0 (Vela: more reds and blues, brass-gold, blue-teal glass, the Office's deep teal band, powder-blue road kept; briefs/072/style/applied.html); the grade toward DecoKit and honed stone (Glint; briefs/072/grade/); window holes in the L2 shadow copies (F1 closed, proxy-vs-L0 mismatch 2.9–4.8 % → 0.2–0.3 % of pixels); CLEARING refit to 1.3–1.4 dE; no brass on the road edge (LightBar frames charcoal, the pinstripe retired). Commits a5234fdb and the 072-G closing commit. GDD v0.31.0 (2d255056).
- Rebuild on export 6841dcfc, offscreen: briefs/072/shots/g/, compare/start-*.png (the whole round), compare/g-*.png, f1g/. Counts in briefs/072/latch/g/.
- Redline phase 3 (briefs/072/redline/phase3.md): holes ≈ 0.001 ms at play (≤ 0.05 ms at street level); 072-F passes the measurable items of §4.3; projected warm Air mean at the opening 13.68 ms without 064 A3, 12.3 ms (11.0–13.7) with A3 items 1, 5, 7, none of which is in the code yet. The Air sitting waits on them (Sky: "yeah wiat").
- Open: A3 items 1, 5, 7 (Latch, Glint); the UnityStats capture and the SRP batcher lines, texture reads per shader, a player memory snapshot (Latch); the pinstripe rejected in the catalogue (Splice, Datum); c040 (Waymark); the stretch look on scaled pilasters and fillers (Vela, Glint).
