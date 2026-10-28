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

No logic, number, colour, material or layout was changed by E1-E7. E8 changes nothing while `Lower` is null (the proof's builds and the swap's "old" heights).

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
