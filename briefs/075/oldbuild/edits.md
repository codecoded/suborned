# 075 remap step 2 · the old DecoKit builders in our project: edits to the verbatim copy

Source: `~/Projects/the-suborned/game` (Unity 6000.6.2f1, same as ours). Everything below was copied with its `.meta`
(GUIDs kept). Every change from the old source is listed here. Machine records sit beside this file: `edits.json` (each
rule, file and count), `clashes.json` (each GUID clash), `guidmap.json` (old → new GUID). The scripts that did the work
are in `tools/` (`stage.sh` copy, `guids.py` clash scan, `regu.py` re-GUID, `edits.py` source edits, `dangling.py`
reference check, `proof.py` composite and diff).

## What was copied
| from (old `Assets/`) | to (our `unity/Assets/`) | tracked? |
|---|---|---|
| `TheSuborned/Editor/DecoKit/` (56 .cs) | `Suborned/Editor/OldTown/DecoKit/` | code, can be committed |
| `TheSuborned/Scripts/Sky/` (SkyPath, SkyTraffic, RocketFlight), `Robots/` (RobotFace, RobotIdle), `Crowd/` (9 files), `DKCutaway.cs`, `FirstPersonController.cs` | `Suborned/Runtime/OldTown/` | code, can be committed |
| `TheSuborned/Art/DecoKit/` (760 MB: materials, models, prefabs, textures, HDRI, shaders, Generated) | `Suborned/OldTownArt/DecoKit/` | git-ignored |
| `TheSuborned/Art/RetroFutureMobility/` (24 MB: the glTF cars and people that DecoKitCitizens loads) | `Suborned/OldTownArt/RetroFutureMobility/` | git-ignored |
| `TheSuborned/Data/RobotGlyphs.txt` | `Suborned/OldTownArt/Data/` | git-ignored |
| `Settings/PC_RPAsset.asset`, `Settings/PC_Renderer.asset`, `TheSuborned/Data/Civic/HarbourFog.mat`, `TheSuborned/Shaders/Civic/HarbourFog.shader` (the old renderer's fog pass, which does nothing outside HarbourDistrict) | `Suborned/OldTownArt/Settings/` | git-ignored |

Why Crowd, DKCutaway and FirstPersonController: the editor code uses them (DecoKitCrowdTest, DecoKitIso, DecoKitWalker,
which the street build calls), so copying them kept DecoKitMenu unedited. Not copied: Citizens3D and the VatCrowd
shaders (only CrowdTest and CitizenLineup use them; the street build doesn't).

## New files (not from the old project)
- `Suborned/Runtime/OldTown/Suborned.OldTown.asmdef`: runtime; refs InputSystem, URP Core/Universal Runtime, Burst,
  Collections, Mathematics; `autoReferenced: false`, so none of our assemblies can see it.
- `Suborned/Editor/OldTown/Suborned.OldTown.Editor.asmdef`: Editor only; refs Suborned.OldTown plus the URP editor
  assemblies; `autoReferenced: false`.
- `Suborned/Editor/OldTown/Proof/OldTownProof.cs`: the driver. `Street()` and `Flyby()` call the old `DecoKitMenu.Street` /
  `DecoKitMenu.Flyby` with the old project's render settings swapped in, in memory: the old PC_RPAsset/PC_Renderer (MSAA 4,
  depth and opaque textures, 4 cascades, probe volumes, GPU resident drawer off) and the old PC quality level (lodBias 2,
  realtime reflection probes on, mip streaming off). It puts everything back afterwards, including the AA that the swap
  rewrites. The first run's restore wrote `customRenderPipeline: 0` and `antiAliasing: 4` into ProjectSettings/QualitySettings.asset.
  I restored the file from HEAD and fixed the driver (`GetRenderPipelineAssetAt`, explicit AA restore). The second run left
  ProjectSettings clean.

## Source edits (code; `edits.json` has every site)
| id | change | files | sites | why |
|---|---|---|---|---|
| E1 | `Assets/TheSuborned/Art/` → `Assets/Suborned/OldTownArt/` | DecoKitData (Root), DecoKitCitizens (Rfm), DecoKitCrowdMerge, DecoKitCrowdTest, DecoKitCitizenLineup | 6 | art location |
| E2 | `Assets/TheSuborned/Data/` → `Assets/Suborned/OldTownArt/Data/` | DecoKitRobots | 1 | glyph file location |
| E3 | `Assets/TheSuborned/Shaders/` → `Assets/Suborned/OldTownArt/Shaders/` | DecoKitCrowdTest | 2 | consistency (crowd test not run; shaders not copied) |
| E4 | `ScenesDir` `"Assets/TheSuborned/Scenes"` → `"Assets/Scenes"` | DecoKitData | 1 | scene beside ours |
| E5 | namespace `TheSuborned` → `Suborned.OldTown` (namespace, using, using static, aliases) | all 72 .cs | 159 | distinct namespace (executeMethod is now `Suborned.OldTown.Editor.DecoKit.DecoKitMenu.*`) |
| E6 | shader-name literals `"DecoKit/…"` → `"OldTown/DecoKit/…"` | DecoKitMaterials (3), GroundMesh (2), Harbour, Sky, SkyTraffic, Studio | 9 | our project has its own shaders under the same names (see A1) |
| E7 | `DecoKitStreetDemo.SceneName` `"DecoKitStreet"` → `"OldTownProof"` | DecoKitStreetDemo | 1 | scene is `Assets/Scenes/OldTownProof.unity` (OpenSaved and Flyby follow) |
| E8 | the one height parameter: `internal static Func<DecoKitPlacer, string, Piece, Piece> Lower` (null = the old builders as they were); `Of` returns `Lower(p, cls, Raw(p, cls))`, the old switch is now `Raw`; `Crescent` reads `Raw` (the circus crescents keep their height) | DecoKitFrontage | 3 | brief 075 swap, Sky's height rule (OldTownSwap sets it for "rule", clears it after) |
| E9 | the "no tower here" parameter: `internal static Func<string, string, GameObject, bool> Stands` (null = the old builders as they were); `Build` asks it of each roof-programme piece as placed and destroys the piece when it answers false (`continue`, so the deck's people treat the spot as free) | DecoKitHighrise | 2 | brief 075 swap, Aster 2026-10-29 after Vantage's sightlines.md: the old rule of DecoKitDecks' header ("towers would bury the square") extended to the Handover zone; the rule is `Swap/SwapTowers.cs`, set by OldTownSwap.Build for both heights, cleared after |
| E10 | plan 0.5.5's parameters, each defaulting to the old value: `DecoKitStreetLayout.Walk` const 0.15 → static field; `BoulevardKerbX` (= KerbX 8) and `Kx(z)` (the boulevard's kerb line south of the circus, the avenue's north), read by the walks, side street, dressing, circus mouths and lamps, Ground's road deck and verges, drains, ad columns, crowd lanes and the Southside's kerb lines; `DecoKitCityPlan.RingR` const 26 → static field; `DecoKitPlacer.GroundScaleY` (1: kerb, paving and tree-pit modules placed at y 0 scaled in height); `DecoKitRoadMarkings` pad height from `Walk`; `DecoKitSouthside.Blocks` settable and `SatelliteHalf` (4) for the greens and the square's south kerbs; `DecoKitCityMass.Rule` (null); two default parameters `y = L.Walk` → NaN (C# needs a constant) | StreetLayout, CityPlan, Circus, StreetDemo, StreetLife, SouthsideLife, DistrictDressing, DistrictLife, Southside, Placer, RoadMarkings, CityMass | 41 | brief 075 swap, second build: OldTownSwap sets walk 0.18, boulevard kerbs ±10, ring r 29, ground ×1.2, the Southside on its plan blocks, the mass rule, and puts every one back after the build |
| E11 | the old road-level modules lifted 3 mm clear of the deck: `SwapWest.LiftRoadModules`, called at the end of `LiftRoad`. Rule by shape, never by id: every old outermost prefab instance whose top lies at the deck (\|top\| < 1 mm) and whose body is sunk in the road (bottom < -2 cm) moves up `RoadModuleLift` 0.003 m (the height our tram tiles are laid at). The old ground.py modules (Tile_TramRail and its kin) top out at z 0 over a deck that was at -0.002; E10 lifted the deck to 0, so their tops became coplanar with the asphalt and z-fought (the Boulevard's "shattered white fragments" in swap2/pair-vista.png) | Swap/SwapWest.cs (the swap's own code; no old file changed) | 1 | brief 075 tram bed (coordinator's bug from swap2/pair-vista.png) |
| E12 | our tram bed is what reads on the route. `SwapNetwork.Rails` lays the old Tile_TramRail on every rail piece of ours, the Boulevard's double track (x ±1.8) included (the first and second builds skipped it within 4.6 m of the old single track at x -2.5 and kept the old track), its top `BedLift` 5 mm over the deck (over the old road modules at 3 mm, the old paint at 4 mm and our crossings' road plates at 3 mm, so nothing lies level with the bed); each tile's 2 × 2 m footprint goes into `SwapNetwork.Bed`. `SwapNetwork.ClearBed` (after the network, before SwapCut), by shape: old road-level modules (top within 6 mm of the deck, body sunk) overlapping our bed, and old road paint (generated flat meshes under 1 cm thick, top 0.5–12 mm over the deck) by connected part where the part overlaps our bed, give way; `CrossGround`: in each corridor quad of a plan tram road (`SwapPlan.TramRoads`, the road's `tram` field: sunset-way, crown-road, market-street, boulevard) that carries our bed, old flat pieces standing over the bed (top 1 cm to 1 m) more than 0.8 m in from the kerb line give way whole, old generated walk-level ground over the bed is cut exactly at the kerb lines (old Orbit Square spans the plan's Boulevard at the walk), and old Kerb_Granite close the cut edges. `SwapOurs`: our flush rail parts (road level, ≤ 12.6 m, centre in a rail piece's footprint) drop with the rest of ours outside the zone, the west included, where they had been batched into our kept ground 2 cm over the old tiles. Each removal logged ("tram bed (our route, E12)", "old ground across our tram street (E12…)"). `SwapTramAudit` (a build step after the tile merge, and `OldTownSwap.TramAudit` on a built scene) samples every 1 m along each track and reports faces level with or over our bed (tram/tram-audit*.json). Shots: `-swapShots tram` (SwapShots.Tram) | Swap/SwapNetwork.cs, Swap/OldTownSwap.cs, Swap/SwapOurs.cs, Swap/SwapPlan.cs, Swap/SwapTramAudit.cs (new), Swap/SwapShots.cs (the swap's own code; no old file changed) | 6 | brief 075 tram bed: "our tram bed and rails must be what reads on the route, and the old track and markings give way where they clash" (Aster) |

E10 changes nothing while its parameters hold their defaults (the proof's builds). E11 and E12 are in the swap's own code (Swap/), so the proof's builds never run them.

No logic, number, colour, material or layout was changed by E1-E7. E8 changes nothing while `Lower` is null (the proof's builds and the swap's "old" heights). E9 changes nothing while `Stands` is null (the proof's builds).

## Art edits (in the git-ignored copy only; our Kit untouched)
| id | change | files | sites |
|---|---|---|---|
| A1 | `Shader "DecoKit/…"` → `Shader "OldTown/DecoKit/…"` | the 7 old DK shaders | 7 |
| A2 | `#include "Assets/TheSuborned/Art/DecoKit/Shaders/DK_Cutaway.hlsl"` → the OldTownArt path | DK_LitCutaway, DK_ComplexLitCutaway | 8 |
| A3 | re-GUID of the 495 clashing GUIDs, new = md5("suborned.oldtown/" + old), replaced in every .meta and Unity YAML file of the copy | 849 files | 5,166 |

## GUID clashes (`clashes.json`)
Of the copy's 1,135 GUIDs, 495 also exist in our `unity/Assets`: 323 byte-identical, 172 different.
- DecoKit Materials 101 (48 differ: ours retuned), Models 185 (116 differ), Prefabs 180 (identical), Textures 20 (identical), Shaders 4
  (LitCutaway, ComplexLitCutaway, DK_Cutaway.hlsl differ; RobotFace identical), folder metas.
- Scripts: `RobotFace.cs` and `RobotIdle.cs` share GUIDs with our `Suborned/Runtime/Robots/` versions (different code).
- `RobotGlyphs.txt` (ours in Runtime/Robots, different), `Settings/PC_RPAsset.asset` and `PC_Renderer.asset` (different).

Unity would have reassigned one side of each pair at random. That could have pointed the old prefabs at our retuned
materials, models, shaders and robot scripts, or rewritten our metas. So the copy got new GUIDs and its own internal
references were rewritten to match (A3). Our files and metas are unchanged. The only dangling reference after the remap is
the glTFast importer GUID, which the package resolves.
The shader names also clashed (four `DecoKit/*` names exist in our `Suborned/Shaders/DecoKit/`). That's why A1/E6 exist:
`Shader.Find` would otherwise pick either copy.

## Project-level change (Aster to decide)
- `unity/Packages/manifest.json`: added `com.unity.cloud.gltfast` 6.20.0, the old project's version. DecoKitCitizens loads the
  RetroFutureMobility `.glb` cars and people, and the circus frame is full of them. The lock file gained gltfast and
  com.unity.mathematics 1.4.0 (builtin). Burst and Collections versions are unchanged. Our project has no `.glb` assets, so
  nothing of ours imports differently.
- `.gitignore`: OldTownArt, the proof scene and its bake folder, `unity/Scratch/` (the old `DecoKitData.CaptureDir`).

## Runs (all batchmode, through `bun run guard`)
1. import + compile (`-nographics`): both assemblies compile, 0 `error CS`, no GUID-conflict messages. Log: `out/scratch/latch/075oldbuild/import.log`.
2. `-executeMethod Suborned.OldTown.Editor.Proof.OldTownProof.Street`: the old `DecoKitStreetDemo.Build` end to end ("street built;
   missing prefabs: none", "street OK"): 1,243 people and 70 cars, Southside 1,608 people and 162 cars, 466 city-mass blocks, colourway
   plan+livery+soft, 515 frontage pieces, ambient L0 (0.530, 0.485, 0.425). Log: `street.log`.
3. `-executeMethod Suborned.OldTown.Editor.Proof.OldTownProof.Flyby -dkOut oldtown_proof -dkStoryboard 5 -dkFrom 20 -dkTo 20 -dkW 1920 -dkH 1080 -dkSS 2`
   (the old storyboard's arguments): "flyby path clips: DK_CircusMonument at 21.8-22.6 s", the same line as the old storyboard log. Log: `flyby.log`.

## Proof (`proof.png`, `proof.json`, `oldtown_proof_t20.png`)
Against `briefs/074/flyby/old-stills/flyby_t20.png`, 1920×1080: mean |diff| 0.036/255, RMSE 0.27, PSNR 59.6 dB, 92.4% of pixels
identical, 0.14% differ by more than 2, 0.02% by more than 8, none by more than 32 (max 84 on a few isolated pixels). It is the old frame.

## The swap (remap step 2, 2026-10-29): new code beside the copy
- `Suborned/Editor/OldTown/Swap/` (own asmdef `Suborned.OldTown.Swap.Editor`, Editor only, `autoReferenced: false`): `OldTownSwap` (Build, Shoot, Flyby,
  Verify), `SwapOurs` (our scene cut to the zone and the network), `SwapNetwork` (our routes in the old pieces), `SwapCut` (the old gives way),
  `SwapExport` (built.json, occluders), `SwapShots`, `SwapVerify`, `SwapMesh` and `SwapShapes` (mesh components and exact XZ clipping).
- `Suborned/Editor/OldTown/Proof/OldTownSwapAccess.cs`: `InternalsVisibleTo("Suborned.OldTown.Swap.Editor")`, so the swap calls the old internal API
  without making any of it public. The old builders' private members it needs (DecoKitStreetDemo's tunables and Ground, DecoKitSkyRail.Beam,
  DecoKitFlyby.Pose and Duration, OldTownProof.WithOldSettings) are called by reflection.
- Report, frames and evidence: `briefs/075/oldbuild/swap/` (index.html).
