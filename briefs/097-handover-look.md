# 097 · The Handover zone at its best: light, surfaces, motion and juice

Owner: main session (as Aster), opened 2026-11-06 on branch `work/097-handover-look`.

## Sky (2026-11-06)
- "go through every non character asset in the district office and try to improve, optimize and make look as good as you can within reasonable triangle counts and draw calls. Looking for lighting, detailing, shadows, that hold up in LoD0. Aim for AAA standard lighting whilst always mindful of performance cost."
- "Can use the runware API of $30 spend to improve or swap out models if need be and the Tripo retopology."
- "Think about additional visuals we can add in the area to give the game an amazing feel. Think about VFX as well and shaders ... breathtaking game with juiciness. Be ambitious, take no shortcuts. Work on a new branch."
- "You can drop any assets outside of the district office for now that aren't used for faster iteration and we add back in later."
- "remember to maximise Unity to its fullest, using / installing any plugins that can help."

Scope = the Handover zone (OldTownSwap.ZonePlan: the civic block, Records Office, café, square and fountain, kerbs, roads, crossings, street furniture, planting, cameras) as played in OldTown. Characters are out of scope. Paid budget: $30 Runware (Tripo 3.1 for models), each pass through `tools/paid/paid.py`; Codex stills first ($0).

## Cost rules that hold
- The Air is the 60 fps reference and is GPU-bound at play zoom (Redline's audit: ~15–21 ms on 0.2.122, never measured on OldTown). Every addition is near-free per pixel, replaces a cost, or has an off switch (global) for the Air A/B. Draw calls stay ~110 SetPass; triangles at play stay far under the 2.5 M gate.
- Systems, not scripts; nothing at runtime is AI. Signals stay unlit and outside the grade (Glyph's).

## Fast loop (Sky's "drop assets outside")
`out/scratch/main097/build.sh [margin]`: GameBuild.CliScratch with `-lean` (Editor/World/Play/LeanScene.cs) saves Assets/Scenes/Lean/OldTown.unity (git-ignored, same scene name) without the pure geometry more than `margin` m (110) outside the zone and builds from it: 6,044 objects dropped, build 36 s, shots 48 s (`run.sh <id>`). The real OldTown.unity is untouched; a build without -lean ships the whole town. Perf numbers that count are taken on the full scene.

## Baseline (lean 0.2.254-scratch, 2026-11-06, `out/scratch/main097/shots/base`)
Office y210: GPU 1.56 ms, mean 4.18 ms on the Studio. Findings at play zoom and close: windows flat dark panels; the contact shade's lilac tint (0.55, 0.53, 0.6) reads magenta on cream under the grade and darkens sunlit ground as if shaded; the road a flat brown-grey with blotches (§15: the old town's dark grey asphalt); the limestone detail reads as camouflage blotches on the tower; nothing moves (no wind, no water, static jets); vertex AO (audit item 1, approved 2026-11-05) never built.

## Plan
1. Shaders: wind (`_DK_WIND`, all cutaway passes), vertex AO (`_DK_VERTEX_AO`, Vela's Blender bake), fountain water and jets, window interiors, sun-aware contact shade with a warm tint, cloud shadows (URP main-light cookie, scrolled).
2. Light: a zone-only GPU lightmap bake (Baked Indirect: realtime sun, baked bounce and occlusion) + light probes for people and props; reflection probes for chrome and glass.
3. VFX (Shuriken, built from code): fountain spray, mist and splash rings; ambient life (birds, drifting petals, dust in the sun, steam); play feedback.
4. Assets: weak props re-made or detailed (Blender first, Tripo only where it wins).
5. Measure: Studio A/B per item, the full scene before/after, tris, SetPass, texture MB; a self-contained report.

## Unity features considered
Used: light cookies (in the URP asset already), GPU lightmapper + light probes, reflection probes, Shuriken. Not used: Surface Cache GI (experimental, realtime ray-traced: too heavy for the Air); On-Tile post (no bloom); VFX Graph (its graphs can't be authored headless from code; Shuriken covers the particle counts here); SSAO (Sky rejected it for cost).
