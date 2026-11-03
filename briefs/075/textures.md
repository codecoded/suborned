# 075-B · textures and materials: ours against the old DecoKit (Vela, 2026-10-28)

Sky: "yeah, let's get moving back to the old world textures. designs etc". I judged each surface at play zoom (zoom 14, 38°; before/after crops in `briefs/075/textures/`) and in the 074 flyby grid's street view (`briefs/074/flyby/grid/old-t6.png` against `on-t6.png`). The scope is texture and material response only: palette colours stay, and the road keeps #8FAFC1. The machine copy, with sources and sha256, is `textures.json`.

The main finding: most of our kit already is the old DecoKit. Enamel, chrome, steel, DK_Glass, DK_Limestone and DK_Render_White are the legacy `.mat` files unchanged. The 072-F detail maps and the ground set are built from the old DecoKit's own sheets. Where the old reel looks better, the difference is in four places:
- **the road:** a testbed sheet, not the old one
- **the pavement and lawn:** recoloured or banded away from the old
- **the glass:** the old windows got their look from baked reflection probes and the grade, which is Glint's 075-A
- **the walls:** the neutral panel sheet's softened grain (074-H's file)

| surface | ours | old DecoKit | pick | why |
|---|---|---|---|---|
| **Walls: panel** (kit wall modules, colourway walls) | DK_Limestone_Tex + 12 colourways: `wall_panel_neutral_v1` (the old facade_panel_v1's luminance, body grain 6.7 → 2.5 %), the old normal and mask | DK_Limestone_Tex with scene paint copies, all base colour white: facade_panel_v1 2048 ×3 at 2 m, mask smoothness ×1 | ours (074-H's file) | Same sheet, normal and mask. The old cream moved into the tint so colourways land. **Open for 074-H:** the old sheet's tonal cloud is what gives the old walls life, and the softened grain reads flatter. |
| **Walls: limestone** (civic stone) | DK_Limestone legacy (#E7DECA, sm .25) + 072-F pair from limestone_polished_v1 | DK_Limestone, flat, same values | ours | Identical material. The detail is the old sheet. |
| **Walls: render** | DK_Render_White legacy (+ colourways). FacadeLit layer 0 from limestone_polished_v1 | DK_Render_White, sm .25, flat | ours | Same response. The texture comes from the old sheet. |
| **Glass: window film** (DK_GlassDark) | GlassSky film: glaze #3F7A8E a .65, fake sky #D8E6E4 → #86B8E0 | URP Lit transparent: #35585A a .3, sm .98, lit by 17 baked box probes of DK_Sky_Street | **ours + old sky (changed)** | The old look is probes plus grade. As Lit at a .3 it is brief 048's black void. The film's fake sky is now the old street gradient (horizon #D1DBDD, zenith #75A4D6). The probes are 075-A. |
| **Glass: curtain and clear** | DK_Glass legacy unchanged. Café clear/sheen from the legacy GlassDark .mat | DK_Glass #CFEDE6 a .3 sm .98 | ours (= old) | Byte for byte the old. The glass-slab buildings are 075-C. |
| **Paving** | DK_Ground_Pavement / DK_Pave_Flags: pave_flags_v1 at 2048, **recoloured #D5CBB8** | DK_Pave_Flags: pave_flags_v1 4096 ×3, mean #EAE0C4 | **old colour, our size (changed)** | The old reel's pale cream flags. Ours read grey-brown next to the colourway walls. Re-centred on the old mean, which is also nearer the palette's #F9EFDC. Brief 048's one-curve recolour is kept. |
| **Roads** (carriageway) | DK_Ground_RoadEnamel: testbed enamel webp, 4 m repeat, ~.34 smooth | DK_Asphalt_Road: asphalt_road_v1 2048 + normal at 16 m, sm .28 | **old texture, powder blue (changed)** | The enamel's speckle read as terrazzo. The old sheet's luminance is laid on #8FAFC1 at 16 m, with stains at 65 % (at full strength they read as dirt). Normal, mask and fine-aggregate detail are shared with DK_Ground_Asphalt, at smoothness ×1.4 (the old .28). Same variant, same memory. |
| **Kerbs** | world kerb DK_Ground_RoadPearl (testbed pearl). Granite pieces use the old sheet recoloured warm | DK_Granite_Kerb: granite_kerb_v1, blue-grey #A4ABBA | ours | The old reel's kerbs read pale. Blue-grey granite next to a powder-blue road loses the edge. The pearl is the capstone's (brief 028). |
| **Water** | sea: look.json `water` / Look.Water. Kit: DK_Pool_Water, DK_Fountain_Water, DK_Water legacy | DK_Ocean + DK_Sea_Normal (sea). DK_Water #6FB7C4 a .3. DK_Pool_Water enamel | **split** | Glint (075-A) owns the sea: its shader, sky and grade. The kit's pool and fountain water stay the old `.mat` files (notes.md). |
| **Metals: chrome** | DK_Chrome/Steel legacy + 072-F brushed detail (half bump on chrome) | #EDEDED, metallic 1, sm .95. Steel .7. Brushed steel at .5 m | ours (= old) | Same values. The mirror look needs 075-A's probes. |
| **Metals: brass-gold** | kind `brass`: DK_Gold #D6AE62 (old hex), metallic .6, sm .6. Crowns #E3D681 metal | DK_Gold #D6AE62, metallic 1, sm .7 | ours | The colour is already the old one. Full metal mirrored the dark zenith as black studs (briefs 009, 072-G). Retry after the probes land. The gold towers are 075-C. |
| **Enamel** | legacy Complex Lit (sm .68, coat 1/.97) + 072-F glaze detail | same | ours (= old) | Identical. The detail is an old sheet. |
| **Roofs** | builder roof colours + FacadeLit roof layer (asphalt_fine_v1) | DK_Roof_Felt #8F8A83, roof decks, roof lawns | ours | The texture source is old. Roof lawns and decks are design (075-C). |
| **Planters and greens** | DK_Ground_Lawn: lawn_v1 at 1536, stripes softened, hue 108, **+ 4 m mowing bands**, sm .2 | DK_Lawn: lawn_v1 2048 + normal at 8 m, sm .12 | **old response, bands off (changed)** | The added bands read as a sports pitch down every median in the 074 grid. They are now off, and smoothness is .12 as in DK_Lawn. The leaf cutouts are the old sheets. The palms are 075-C. |

**Old scene material response, where it differs from ours:**
- the road was .28 smooth (ours was ~.34)
- the lawn was .12 (ours was .2)
- glass and chrome relied on baked box-projected probes (256 px, 17 in the street) and the panoramic street sky; we have no probes on the street, which is 075-A
- normal strength was 1 throughout, with no detail maps

**Texture memory (BC7/BC5 with mips):**
- ground set 27.24 MiB of 30, unchanged
- road set 24.75 MiB, unchanged: the road albedo is 1536 px, the same texels as the enamel's three maps, and its normal, mask and detail are shared
- 072-F surface set 15.98 MiB of 24, unchanged
- kit copy: −1024² normal, −512² mask, +1536² albedo, net 0
- no new shader variants (DK_Ground_RoadEnamel now has DK_Ground_Asphalt's keyword set)
