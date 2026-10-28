# Play-view occlusion, before and after (brief 075)

Share of pavement hidden behind buildings from the play camera: orthographic, zoom 14, pitch 38°, mean of yaws 30 / 120 / 210 / 300; 0.5 m raster. A figure counts hidden when its head (1.8 m) is. Informational, not a failing rule.

- before: `22923fe7` (out/scratch/occlusion/22923fe73994/out/occlusion.world.json)
- after: `5e0abf86` (out/scratch/occlusion/5e0abf867c00/out/occlusion.world.json)
- regions: this tree's plan areas; the Handover streets = the capstone zone [-226,-84,-372,-148] (standards.blocks.zone)
- command: `bun run world:occlusion --before 22923fe7 --after 5e0abf86 --out briefs/075/oldtown/occlusion`

### Pavement (all street-level footway: plot pavements, squares, courts, foot ways)

| region | m² before → after | ground hidden before | after | Δ pts | figures hidden before | after | Δ pts |
|---|---|---|---|---|---|---|---|
| **district** | 257107 → 295916 | 49.5% | 50.6% | +1.1 | 47.6% | 48.6% | +1.0 |
| **Handover streets** | 16060 → 16060 | 40.9% | 40.9% | +0.0 | 38.5% | 38.5% | +0.0 |
| Records Precinct | 30074 → 30570 | 42.4% | 41.5% | -0.9 | 40.5% | 39.8% | -0.7 |
| Lamplight | 26621 → 29718 | 67.4% | 63.1% | -4.3 | 64.1% | 59.8% | -4.3 |
| Sphere Gardens | 59157 → 56813 | 23.2% | 24.2% | +1.0 | 22.4% | 23.3% | +0.9 |
| Crescent Quarter | 17813 → 32984 | 72.2% | 67.3% | -5.0 | 71.1% | 65.4% | -5.7 |
| Harbour Quarter | 19062 → 43512 | 73.3% | 72.7% | -0.6 | 71.9% | 70.8% | -1.0 |
| Lantern Quarter | 52143 → 52271 | 45.7% | 39.9% | -5.8 | 44.1% | 38.0% | -6.2 |
| Market Docks | 39273 → 37082 | 61.6% | 61.2% | -0.4 | 58.3% | 58.1% | -0.2 |

Per yaw, ground hidden (before → after):

| region | 30 | 120 | 210 | 300 |
|---|---|---|---|---|
| district | 46.2% → 48.3% | 47.9% → 49.2% | 48.7% → 50.5% | 55.1% → 54.3% |
| Handover streets | 43.0% → 43.0% | 41.5% → 41.5% | 45.4% → 45.4% | 33.9% → 33.9% |

### Kerbside pavement (within 8 m of a kerb: the street pavement, without courts and yards)

| region | m² before → after | ground hidden before | after | Δ pts | figures hidden before | after | Δ pts |
|---|---|---|---|---|---|---|---|
| **district** | 88118 → 91547 | 53.4% | 45.9% | -7.6 | 51.6% | 44.5% | -7.1 |
| **Handover streets** | 6575 → 6575 | 34.8% | 34.8% | +0.0 | 32.3% | 32.3% | +0.0 |
| Records Precinct | 13331 → 13627 | 40.3% | 37.8% | -2.5 | 38.5% | 36.3% | -2.2 |
| Lamplight | 8948 → 9350 | 56.2% | 51.4% | -4.7 | 53.9% | 49.3% | -4.6 |
| Sphere Gardens | 11101 → 10979 | 39.8% | 43.0% | +3.2 | 37.5% | 41.0% | +3.5 |
| Crescent Quarter | 10500 → 11046 | 68.8% | 59.5% | -9.4 | 67.2% | 58.8% | -8.4 |
| Harbour Quarter | 13246 → 14685 | 69.2% | 52.2% | -17.0 | 67.8% | 51.3% | -16.5 |
| Lantern Quarter | 13247 → 13844 | 43.6% | 34.5% | -9.1 | 42.4% | 33.9% | -8.5 |
| Market Docks | 12352 → 12622 | 51.3% | 42.6% | -8.7 | 48.6% | 40.4% | -8.3 |

Per yaw, ground hidden (before → after):

| region | 30 | 120 | 210 | 300 |
|---|---|---|---|---|
| district | 50.0% → 44.5% | 54.0% → 45.6% | 56.5% → 49.9% | 53.3% → 43.5% |
| Handover streets | 31.7% → 31.7% | 42.0% → 42.0% | 41.0% → 41.0% | 24.6% → 24.6% |

### Open ground (pavement and carriageways)

| region | m² before → after | ground hidden before | after | Δ pts | figures hidden before | after | Δ pts |
|---|---|---|---|---|---|---|---|
| **district** | 421860 → 460669 | 46.9% | 44.0% | -2.9 | 44.9% | 41.3% | -3.5 |
| **Handover streets** | 22879 → 22879 | 40.4% | 40.4% | +0.0 | 37.5% | 37.5% | +0.0 |
| Records Precinct | 40217 → 40714 | 43.1% | 42.0% | -1.1 | 40.6% | 39.6% | -1.1 |
| Lamplight | 30713 → 33810 | 67.3% | 62.5% | -4.8 | 63.4% | 58.6% | -4.9 |
| Sphere Gardens | 63758 → 61414 | 25.2% | 26.4% | +1.2 | 24.3% | 25.4% | +1.0 |
| Crescent Quarter | 22964 → 38135 | 75.9% | 66.6% | -9.3 | 74.6% | 63.9% | -10.7 |
| Harbour Quarter | 26369 → 50819 | 77.8% | 70.7% | -7.1 | 76.1% | 67.5% | -8.6 |
| Lantern Quarter | 57727 → 57854 | 45.9% | 39.0% | -7.0 | 44.4% | 36.7% | -7.7 |
| Market Docks | 44664 → 42473 | 58.8% | 57.0% | -1.7 | 55.2% | 53.3% | -1.9 |

Per yaw, ground hidden (before → after):

| region | 30 | 120 | 210 | 300 |
|---|---|---|---|---|
| district | 43.5% → 41.7% | 45.6% → 43.1% | 46.4% → 43.5% | 52.0% → 47.7% |
| Handover streets | 38.7% → 38.7% | 43.8% → 43.8% | 46.3% → 46.3% | 32.6% → 32.6% |

### Decks (9.43 m deck, 24 m concourse)

| region | m² before → after | ground hidden before | after | Δ pts | figures hidden before | after | Δ pts |
|---|---|---|---|---|---|---|---|
| district | 36867 → 36867 | 36.4% | 34.7% | -1.7 | 32.5% | 31.1% | -1.4 |

Method: tools/world/occlusion.ts. Occluders are solid columns: building masses, kit facade pieces at their kit height (the face they dress extruded into the building, at most 14 m, so a canopy over the pavement does not count), civic masses the camera does not fade, megastructure parts and prims, signature kit boxes (an upper bound for slender towers; towers use their stand-in masses). Street items, trees, decks and the horizon are not occluders. Pavement = plot slabs, foot ways and footway tables, less ground under anything standing on the street; open ground adds the carriageways with their kerbs and the circuses. A cell is hidden when the camera's ray from it (or from a head 1.8 m above it, for figures) meets an occluder. All pavement includes courts and yards behind the buildings, hidden at any height; the kerbside layer is the street pavement the play view is about.
