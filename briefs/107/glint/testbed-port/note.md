# 107 · Night: the testbed café's light ported to the old town (Glint, 2026-11-11)

Sky, on 0.2.316's night: "What's with this horrible paving colors at night ... that blue circle", "Look at the test bed scene and lighting used for the cafe", "The cafe sign needs lighting at night as well", "This city should thrive at night time in terms of lighting".

- **Build:** 0.2.320 (b5da00ee0+, `bun run batch --sky`, smoke OK). It took two builds (0.2.319, then 0.2.320). The second added the sign switch and the tuned values (below).
- **Board:** `board.jpg`. Columns are testbed | now (0.2.318, the same night as 0.2.317) | proposed (0.2.320). Rows:
  - night at play zoom;
  - night wider (zoom 18);
  - rain at night;
  - the café sign;
  - third person down the street.
- **Frames:** `frames/`.

## What makes the testbed's café work (read from cafe-multiview.js, cafe-ambience.js, cafe-exterior-light-layout.js; captured headless at Evening, Night and Rain sheen)
- **A dark base:** moon 0.18 (#a7c5df), sky 0.12, the environment at 0.08, exposure 1.05 (ACES).
- **Many warm pools on top of it:**
  - café room spots #ffd39b to #ffdfb7, intensity 21–34;
  - terrace wall lights, white 2.8, shadowless;
  - palm uplights, #e4efc7 30, shadowless;
  - globe street lamps.
- **Emissive levels at night:**
  - the café sign's channel letters: 3.2;
  - room emitters: 2.6;
  - wall-light lens: 4.
- **The paving at night:** warm ivory tiles (#f2dfbd). The warm light makes it warm stone; nothing teal lies on it.

## Why ours read grey and teal
- The old town's lightmaps hold the day's bounce, and night kept 40 % of it. That is the grey dusk.
- The carried night grade tints every shadow and dark midtone teal (Shadows/Midtones/Highlights, shadows #6fa3a8). That tint washes the paving.
- The lamps were cool cream and weak.
- `RoadLight` copied glow materials **by name**. The kit and the old art each have a `DK_GlassDark` and `DK_Neon_*`, on different shaders. Whichever it met first won, so in some runs no window lit at all (the tp-long log: "DK_GlassDark has no _Lit ... left as it is").
- The cyan table lamp is the testbed's `DK_Lamp_Table_Teal`.

## What changed (all look only, no rule reads it)
| | was | now |
|---|---|---|
| day bounce at night (`nightIndirect`) | 0.6 taken away | 0.85 |
| old town night trilight / moon | x1 / x1 | x0.55 / x0.6 |
| night lamps (46 carried + the play look's) | as built | x1.6, range x1.25, leaned 55 % to #ffc890 |
| teal-hued lamp pools (150–215°) | cyan | #ffc890 |
| palm uplights (`palm/civic/`, 6 at the café) | none | spot #ffe2b0, 10, range 7.5, 50°, no shadows |
| grade over the night grade | — | teal shadow tint 85 % back to neutral, temperature +7, contrast +8, saturation +4 |
| kit GlassSky panes lit (`DK_GlassDark` lit) | 0.75 | 0.85 |
| old-art glass, its paint variants by prefix | dark | #ffd9a8 x0.35 on 65 % of buildings |
| towers, sky rail cars, bridges (`DK_Window_Tower`) | dark | #ffdcae x0.35 on 70 % |
| shopfronts (`DK_Window_Film`) / `DK_Window_Teal` | dark | #ffd6a0 x0.45 (75 %) / #ffdcae x0.35 (70 %) |
| tram cabins (`DK_Cabin`) | dark | #ffd49a x0.8 |
| neon signs | x1.7 | x2.4 |
| café interior self-light (`DK_Cafe_*`) | x0.1 | x0.4 (floor x0.25) |
| teal table lamp | cyan | tinted 85 % to #ffc890 |

The values live in two files:
- `Resources/Atmosphere/weather107.json`: `nightIndirect`, the new `night` block, `nightNote`.
- `Resources/Atmosphere/roadlight.json`: the glow list, `glowNote107`.

Both can be tuned on a built player without a rebuild: `-weatherJson <copy>` and `-roadlightJson <copy>` (new), with `-night`.

## Files
- `Runtime/RoadLight.cs`:
  - one copy per material **instance** and entry;
  - several entries may name one material (the first it can take is used);
  - `prefix`, `renderer`, `share` (a stable per-building pick) and `unbake`;
  - night copies drop `_DETAIL_MULX2` and `_ENVIRONMENTREFLECTIONS_OFF`, so they draw with a variant the build has;
  - the `-roadlightJson` loose copy.
- `Runtime/Weather/Weather107.cs`: the `NightLook` block.
- `Runtime/Weather/NightLamps107.cs` (new): the carried LightPhase lamps' scale, range, colour and re-hue, always set from the first-seen values.
- `Runtime/Weather/PlayView107.cs`: one call to it.
- `Runtime/OldTown/OldTownPlayLook.Night107.cs` (new): reads the same block for the old town's ambient, moon, lamps, uplights and the grade volume.
- `OldTownPlayLook.Weather107.cs`: hooks into the above; the old exposure-only volume is folded into `Grade107`.

## Cost (Studio, 1920x1080, -novsync, one tp-long run each, each look against its own run's clear)
| row | now night − clear | proposed night − clear |
|---|---|---|
| play | mean −0.02 ms, GPU +0.04 ms | mean +0.09 ms, GPU +0.28 ms |
| tp-street | mean −0.08 ms, GPU −1.0 ms | mean +0.06 ms, GPU +0.35 ms |

- The port adds about **0.3 ms of GPU at night on the Studio**: the brighter lamps' longer reach and the 6 uplights. Emission is free.
- Scaled the way 107's Air rows scale against the Studio, that is roughly +0.5 to +1 ms on the Air, at the top of the ~1 ms allowance.
- **No Air run was made.** Aster, please ask Sky if one should be. The cheapest cuts if it's over: `lampRange` 1.25 → 1.1, `upIntensity` 0 (no uplights).
- Raw rows: `out/scratch/glint/107/run/{now,p2}/tplong/tplong.json`.

## Open
1. **The café sign is still unlit.**
   - The old builders' café, sign included, is merged into the old art's `CityMass_x_y` chunks, one submesh per material. Its cream letters share `DK_Enamel_Cream` with every cream surface in the chunk, so nothing at runtime can light just the letters.
   - My `renderer: BladeSign_Cafe` entry lit the 16 *kit* `DK_BladeSign_Cafe` signs instead (their cream panel at x2.2).
   - **Vela:** give the café's blade sign letters their own emissive material (a neon cream, like the testbed's channel letters), and keep the sign out of the CityMass merge as its own object. That last part is Latch's swap.
   - **Glint:** then light it with one roadlight.json line, the testbed's 3.2. The `unbake` switch is there in case the sign is lightmapped.
   - A sign light (a shadowless spot on the blade) is the fallback if that waits. It needs the sign's position.
2. The inlay band glow stays off (Aster's 0.2.317) until Vela's thin edge light.
3. The old town's roofs outside the zone stay a flat grey-blue at night: their shader doesn't take `nightIndirect`. Small at play zoom.
4. Signals and rings were checked on the board: the teal rings and the red signal ring read in every proposed frame.
