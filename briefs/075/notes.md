# Brief 075 · coordination notes (Vela 074-H and Vela 075-B share tools/decokit)

Log every decokit run here before starting it (start / end, who, command). Do not run decokit while another run is open.

- 11:58 START Vela 074-H: bun run guard bun run decokit --mats-only (colourways.py: walls + plinth only, WALL_SOFTEN 0.35)
- 11:59 END   Vela 074-H decokit --mats-only (ok, 52 s)
- 12:01 Vela 074-H done with tools/decokit (colourways.py only; no further decokit runs from 074-H). Note for 075-B: SLOTS is wall+plinth, WALL_SOFTEN 0.35 lerps toward the old sheet's mean; if 075-B swaps the wall sheet, the neutral sheet/oldCream in colourways.py follow SRC_SHEET.

## Vela 075-B (textures and materials), 12:06
- Files I touch: tools/decokit/ground_tex.py (pavement and lawn targets), road_tex.py + road_ribbon.py (DK_Ground_RoadEnamel: the old asphalt_road_v1 sheet in powder blue), mats.py (GLASS_FILM sky colours only), unity look.json `road-enamel` tile and smoothness only. Not colourways.py (074-H's; done).
- Water split (proposal to Glint, 075-A): Glint owns the sea and harbour water (look.json `water`, Look.Water, the old DK_Ocean shader, DK_Sea_Normal, sky/probes/grade). Vela owns only kit water materials on pieces (DK_Pool_Water, the fountain's DK_Water/DK_Spray), which stay at the old values (they are the legacy .mats unchanged). Glint: say here if you want otherwise.
- Glass: the old windows' look came from 17 baked box-projected reflection probes (DecoKitAtmosphere.Probe, 256 px) plus the grade; that is Glint's (075-A). I port only the film's fake sky to the old street gradient (DK_Sky_Street.png).
- 12:07 START Vela 075-B: bun run guard bun run decokit --mats-only (road enamel from asphalt_road_v1, pavement old mean, lawn bands off, film sky)
- 12:08 END   Vela 075-B decokit --mats-only (ok, 53 s)
- 12:08 Vela 075-B: bun run guard bun run kit
- 12:11 START Vela 075-B: bun run guard bun run decokit --mats-only (road stain contrast 0.65)
- 12:12 END   Vela 075-B decokit --mats-only (ok)
- 12:12 Vela 075-B: bun run guard bun run kit
- 12:14 Vela 075-B done with tools/decokit (READY-B written). Open for 074-H: wall_panel_neutral grain (2.5 %) vs the old sheet's 6.7 % tonal cloud.

## Water ownership (Glint 075-A, agreed with Vela's 075-B split in textures.md)
- Glint owns the sea and the district water that the builder makes (look.json `water` → Look.Water, one material named `water`): its shader and light response. 075-A ports the old DecoKit/Ocean as `Suborned/DK Ocean` (Shaders/DecoKit/DK_Ocean.shader) with DecoKitHarbour.Ocean's values and the old DK_Sea_Normal sheet (Resources/Atmosphere/dk_sea_normal.png), from light.json `ocean`; Look.Water uses it when that key exists. The builder's fountain and pool basins that draw in `water` take it too (2 renderers in today's scene).
- Vela owns the kit's own water materials (DK_Pool_Water, DK_Fountain_Water, DK_Water: the old .mat files, as textures.md says) and any water albedo or texture on kit pieces.
- No decokit run from Glint. 12:40 Glint 075-A: Unity batchmode captures only (Glint075.cs, offscreen), no scene saved.
