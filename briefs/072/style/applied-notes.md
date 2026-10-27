# 072-G item 1 · applied palette push (Vela) · coordination notes

Files this Vela edits (please avoid or tell me here):
- contracts/style/suborned.style.json (palette_world, materials, notes, concept_prompt city/avoid)
- unity/Assets/Suborned/Editor/World/facade.json (swatches, families, areas, trades, materials, paletteOverride, note)
- unity/Assets/Suborned/Editor/World/look.json and looks/harbour.json (palette hexes; new accent_red / accent_royal surfaces)
- tools/decokit/mats.py (brass rows, KIND brass, gate-era rows, GLASS_FILM glaze), civic_common.py / crossing_common.py (DK_Brass_Civic row), atompunk.py (DK_Gold17 via BRASS_LIGHT, value only)
- tools/decokit/records_tower.py (deep teal storey band)
- tools/world/gen/colour.ts + skins.ts comment (accent roles: + accent_red, accent_royal)
- contracts/world/civic-look.json through tools/decokit/lookcolours072.py (colour values only)
- tests that pin those values: tools/kit/kit.test.ts (brass hexes)

Not touched: sides.py, alleys.py, shopfill.py (window holes, the other Vela), surface_tex.py, unity_mats.py bump values, light.json (Glint).
Runs: `bun run decokit --mats-only`, `--only RecordsTower`, `bun run guard bun run kit`, `bun run world`. If you run decokit/kit at the same time, tell me here.
- 23:26 Vela (palette): running bun run decokit --mats-only, then --only RecordsTower
- 23:30 Vela (palette): decokit mats + RecordsTower done, kit copied; running world, cover, sim:site
- 23:46 Vela (palette): running decokit --only Sedan_Getaway (car coral follows accent_coral), then kit and collide
- Vela (palette) to Vela (holes), sides.py: the film glaze is now #3F7A8E a0.65 (mats.GLASS_FILM). DK_Window_Film #4F6B6B was derived from the old glaze; the same derivation from the new one gives about #48636B (lerp 0.41 to the street mirror #A78F78, a0.65 over charcoal, a step darker). Not edited by me (your file): please take it in your pass, or tell me to.
- Vela (palette) done: decokit --mats-only, --only RecordsTower, --only Sedan_Getaway (DK_CarEnamel_Coral follows the new accent_coral, cars070.test), kit, collide, world, cover, sim:site. Your later decokit/kit runs keep my values (they live in mats.py / look.json).
