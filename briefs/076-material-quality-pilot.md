# 076 · Material-quality pilot

29 October 2026. Sky: “Go” after the 25 October asset review and recommendation to prove one café bay, planter and character before wider adoption.

The tree moved on: 072-F/G already added façade detail and 075 has a civic restyle. Do not repeat the old review's flat-façade changes or overwrite those decisions.

## Delivered

- `tools/art-quality/pilot.py`: five isolated Unity material candidates and five PNG maps, deterministic GUIDs, current source hashes, explicit memory estimate. Only writes under `out/scratch`. It does not install anything or regenerate an FBX.
- `tools/art-quality/render.py`: 18 matched Blender images using the actual installed CafeBay_v2, PlanterRun_4_A and Scout LOD2 meshes. Day close, night close and tactical field, before/candidate. Candidates use current material GUID references. Preview lighting/glass approximate Unity; indirect-only AO is deliberately not faked by multiplying albedo.
- `tools/art-quality/report.py`: portable HTML with all images embedded and a view selector.
- `tools/art-quality/verify.py`: exercises the actual figure texture exporter in Blender and verifies source hashes and render count.
- `tools/figures/build.py`: stop recompressing normal maps into JPEG. Copy unchanged originals byte-for-byte; resize normals to PNG. Existing albedo compression stays. Load lazy image-copy pixels before changing the output path (the new check exposed an existing save failure). Importing the exporter no longer builds figures, enabling focused validation.

## Visual verdict

- Café cloth: restrained, little visible tactical gain. Keep isolated pending a Unity comparison; no reason to add these samples across the library yet.
- Planter: reject for rollout. The leaf pattern makes the domes mottled; a material-only change does not repair their silhouette. A future near-only leaf-cluster/silhouette study should follow the existing plant references. Do not recolour the current civic restyle or increase all foliage density.
- Scout: bounded 512-tier AO/smoothness test using the existing Scout authored mask/layout. Metallic zero, smoothness 0.15–0.28. Does not claim semantic skin/leather segmentation. Hold for Unity/movement comparison; no higher-cost tactical material adopted on Blender evidence alone.

Five maps at 512²: about 1.667 MiB with 1-byte-per-texel block compression and mips, before measuring Unity residency. No extra meshes. Shader keyword costs still require Unity measurement. No Air benchmark or game FPS claim.

## Reproduce

From repository root (Python needs NumPy and Pillow):

```sh
python3 tools/art-quality/pilot.py
bun run guard --wait-timeout 0 /Applications/Blender.app/Contents/MacOS/Blender -b -t 2 --python-exit-code 1 -P tools/art-quality/render.py -- out/scratch/codex/material-pilot
bun run guard --wait-timeout 0 /Applications/Blender.app/Contents/MacOS/Blender -b -t 2 --python-exit-code 1 -P tools/art-quality/verify.py -- out/scratch/codex/material-pilot
python3 tools/art-quality/report.py
```

Output: `out/scratch/codex/material-pilot/compare.html`, `manifest.json`, `renders.json`, `checks.json`, candidate `.mat/.meta`, PNG maps and comparisons. Regeneration produced the same manifest and texture hashes. Blender 5.2.2 checks passed: full-size normal source bytes preserved; resized normals PNG; normal import linear; albedo JPEG unchanged; all sampled installed sources unchanged; 18 matched renders. Python compilation passed.

## Remaining for adoption

Latch: Unity import of accepted candidates in an isolated review scene, accurate day/night and movement A/B, then Air measurement. Only Latch edits scenes. No scene, mesh, collider, kit catalogue, mission, simulation or GDD changes are included here. The normal-export fix applies on the next figure generation; existing installed normals have not been regenerated. No paid calls.
