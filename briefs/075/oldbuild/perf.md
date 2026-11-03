# Brief 075 · Perf A/B: the old-built town vs our district (Redline, 2026-10-29)

Sky's question: "what's the performance difference in rendering?"
- **A:** `Assets/Scenes/OldTown.unity`, the swap built by OldTownSwap at cc903ae0, rendered as Sky saw it (the old PC_RPAsset and renderer, in memory).
- **B:** `Assets/Scenes/HarbourDistrict.unity`, from our pipeline.
- **Budget:** brief 064 A5, warm mean ≤ 13.0 ms and p99 ≤ 16.7 ms at 1920 × 1080 on the 15" Air M4.

How each kind of number is labelled:
- **MEASURED (files):** read from the scene YAML, the pipeline assets and the glTFs. No Unity was run. Data: `briefs/075/redline/oldtown-census-075ab.json`.
- **MEASURED (Studio, 075f1):** Latch's offscreen estimator on our district, `briefs/075/latch/billboards/counts.md`, 2026-10-28.
- **PENDING (Studio):** Latch's A/B capture of both scenes (`briefs/075/notes.md`, "Perf A/B", item A).
- **ESTIMATE:** my projection, with its range.
- **Air rows:** pending Sky's OK. The kit is `briefs/075/redline/run-air-075ab.sh` and `perf-075ab.json`.

## Short answer
- **Where the old town shows its own streets, it costs several times our frame.** This is an estimate until A lands.
- **The main cause is its baked crowd:**
  - 2,420 glTF people and 170 cars stand in the scene as static props.
  - Together they hold 61.3 M triangles and 71,342 sub-mesh renderers.
  - They have no LODs, no static flag, and three tint material overrides each.
  - In a circus or harbour play view that comes to 42–73 figures, 1.0–1.75 M triangles and 1,100–1,900 draws, before shadows. Our entire view is 0.64–0.77 M main triangles in 213–293 renderers.
  - The old pipeline adds to it: MSAA 4×, the depth and opaque textures always on, the GPU Resident Drawer off, a full-screen fog pass, Gaussian DOF and an occluded sun flare.
- **Inside the Handover zone (office front, café), both scenes hold our content.** There only the pipeline and lens differ. That costs an estimated +1.6 to +3.6 ms, which puts even the zone over 13.0.
- **With the ranked cuts below at play zoom only, the old town comes back to about 13–16 ms** (estimate). Cut 1 alone is most of that, and it costs nothing in play: the static crowd would stand frozen inside the simulation's own people.

## The table: old vs ours vs budget

| row | old-built town (OldTown, as Sky saw it) | our district (HarbourDistrict) | budget / bar | source |
|---|---|---|---|---|
| **Scene as built** | | | | |
| mesh renderers in the scene file, direct | 3,594 | 12,374 | n/a | MEASURED (files) |
| prefab instances | 11,294 (2,590 of them the glTF crowd) | 5,882 | n/a | MEASURED (files) |
| glTF crowd: people + cars | 2,420 + 170; 61.3 M tris; 71,342 sub-mesh renderers; no LOD; not static | none in the scene (the sim's people come at runtime, LOD'd; GDD crowd budget 12k / 4.5k / 1.5k tris) | crowd 12k-4.5k-1.5k, 22 bones (Sky 2026-10-27) | MEASURED (files) |
| triangles per figure | people 11.1k–32.1k (mean 23.7k), cars 21.5k–23.5k; 18–53 sub-meshes each | n/a | 12k LOD0 | MEASURED (glTF) |
| per-metre ground instances | 6,430 (1,469 tram-rail tiles, 3,421 paving flags, 1,540 kerbs), static-flagged | ground as merged sheets (075-B road sheet) | n/a | MEASURED (files) |
| reflection probes | 26 baked 256² box-projected + 1 custom 128 | 2 custom 128 | n/a | MEASURED (files) |
| lights | 1 (sun, with LensFlareComponentSRP and occlusion) | 1 enabled (sun) + 279 night lamps disabled | n/a | MEASURED (files) |
| materials | 284 in OldTownArt + 130 embedded + glTF tints; about 105 on Complex Lit clear coat | scene 358, modules 48, 8 module variants (clear coat on enamel modules too) | n/a | MEASURED (files) / 075f1 |
| pipeline | MSAA 4; depth + opaque texture on; GRD **off**; 4 × 2048 cascades at 50 m; APV on (no cell data); fog full-screen pass; SSAO from depth-normals 0.9/0.45; lodBias 2 | MSAA off; depth/opaque by request; GRD **on**; 1 × 4096 at 260 m; SSAO from depth 0.4/0.3, off at play (A3 item 1) | n/a | MEASURED (files) |
| lens | bloom threshold 0.8 / intensity 0.6; Gaussian DOF high quality (300–1800 m); sun flare, occluded | play bloom 1.05 / 0.45; no DOF at play; flare off at play (A3 item 5) | n/a | MEASURED (files) |
| GPU texture memory | PENDING (Studio); the probes alone ≈ 14 MB as BC6H (estimate) | 174.5 MiB (072-G, 2026-10-28; before 075's road sheet, sky and liveries) | n/a | PENDING / MEASURED (072-G texmem) |
| **Play zoom, per view (ortho 14, pitch 38, 1920 × 1080)** | | | | |
| office-front 30 (zone: our content in both) | PENDING (Studio). Old crowd in frame: 0 | SetPass est. 36 (shadow 20); 232 renderers; main 639k / shadow 354k tris; 152 casters; 89 materials | n/a | 075f1 / PENDING |
| office-front 210 (zone) | PENDING. Old crowd: 0 | SetPass est. 37 (19); 213; 636k / 354k; 144; 86 | n/a | 075f1 / PENDING |
| cell-open 30 = café 30 (zone) | PENDING. Old crowd: 0 | SetPass est. 43 (23); 293; 769k / 759k; 333; 142 | n/a | 075f1 / PENDING |
| café 210 (zone) | PENDING. Old crowd: 0 | PENDING (cell-open 210: 43 (23); 273; 753k / 654k; 315; 142) | n/a | 075f1 / PENDING |
| circus 30 | PENDING. Old crowd: 42 people + 4 cars, 1.07 M tris, 1,342 sub-mesh draws (main pass only) | PENDING | n/a | crowd: MEASURED (files, footprint estimate) |
| harbour-terrace 30 | PENDING. Old crowd: 51 + 1, 1.21 M tris, 1,544 draws | PENDING | n/a | as above |
| southside 30 (worst crowd) | PENDING. Old crowd: 68 + 5, 1.75 M tris, 1,901 draws | PENDING | n/a | as above |
| golden / street view | PENDING (Studio, perspective; the follow camera keeps SSAO and 2 cascades) | PENDING | n/a | PENDING |
| **Frame time on the Air (warm, 1920 × 1080, uncapped)** | | | | |
| zone views (office front, café): mean | **ESTIMATE 14.0–17.5 (15.6)**: ours + MSAA 4× 0.8–2.0, depth/opaque/fog copies 0.3–0.7, DOF 0.3–0.7, flare 0.1–0.3, GRD off ≤ 0.5 (CPU, mostly hidden) | **ESTIMATE 11.0–13.9 (12.4)** (075 redline.md §4; no measured warm 1080 number yet) | ≤ 13.0 | ESTIMATE |
| old-content views (circus, harbour terrace, southside): mean | **ESTIMATE 20–30**, render-thread bound: crowd 2,200–3,800 draws with shadows at 1.5–3 µs = +3.3–11 ms CPU; +2–3.5 M micro-triangles (about 25 per pixel on a 50 px figure) under MSAA = +1.5–4 ms GPU; the old city's own draws without GRD: PENDING | ESTIMATE ≈ the opening's 11–14 (no old crowd; our sim people come later and are budgeted) | ≤ 13.0 | ESTIMATE |
| mean, measured | pending Sky's OK | pending Sky's OK | ≤ 13.0 | Air |
| p99, measured | pending Sky's OK | pending Sky's OK | ≤ 16.7 | Air |
| street level (tp-street, tp-worst, tp-circus, tp-harbour) | pending Sky's OK | pending Sky's OK (ours: tp-worst 14.1–15.7 + every face, estimate) | ≤ 13.0 / 16.7 | Air |

## The suspected costs, checked against the files
1. **The glTF crowd and cars: confirmed, and the largest cost.** Each figure is 18–53 renderers (one per glTF mesh node). They are not static, so there is no static batching, and the old pipeline has GRD off, so there is no instancing. Every sub-mesh is a draw in the main pass and again in each shadow cascade it falls in. The scene has 71k renderers to cull for the main view and for every cascade. At 50 px tall, a 25k-triangle figure is about 25 triangles per pixel: quad overshading, made worse by MSAA. They are frozen props, and the game's crowd is the simulation's, so in play both would show.
2. **The old lens: confirmed, but cheap.**
   - The bloom threshold changes the look, not the cost: the mip pyramid is the same.
   - The Gaussian DOF (high quality) runs the whole pass at play, yet its blur starts at 300 m. The play frame's farthest ground is about 200 m away, so at play it blurs nothing.
   - The occluded flare forces the depth texture, and at pitch 38° the sun is almost never in the play frame.
3. **MSAA 4× and the always-on depth and opaque textures: confirmed in the old PC_RPAsset.** Our asset has MSAA off and asks for these textures only on request.
4. **Unique materials breaking batching: partly.** The SRP Batcher keys on shader variant, not material, so the extra materials (crowd tints, colourways) add little SetPass. They do block GRD instancing, and GRD is off in the old pipeline anyway. The draw count is the cost, not the material count.
5. **The per-metre ground sheets: confirmed, 6,430 instances.** They are static-flagged, so a player static-batches them. Without GRD they stay one draw per instance in a view, which costs CPU, not GPU.
6. **Complex Lit clear coat on enamel: both scenes use it.** About 105 old materials and our enamel modules carry it. The difference is coverage, which Latch's area census can measure.
7. **Reflection probes: minor.** The 26 baked box probes cost per-pixel blending in Forward+ and about 14 MB. They are baked, not realtime, despite "realtime probes on".
8. **Unknowns that change the answer, for Latch to confirm (notes item A):**
   - Does OldTown's play camera carry TacticalShadows? If it does, the cascades are refit and SSAO is off at play, as ours. If it doesn't, add 1–2 ms.
   - Is the sun flare still active at play?
   - Do the old shader copies (OldTownArt DK_LitCutaway) support GRD (DOTS instancing)? Cut 4 depends on it.

## Ranked cuts at play zoom only (presentation keeps the full look)
Old town, at an old-content view (circus class). Savings are estimates on the Air, warm, at 1080. Each one is a proposal to Sky, measured by its variant in `perf-075ab.json` once the Air run is approved.

| # | cut at play zoom | est. saving (ms) | what it visibly loses | owner |
|---|---|---|---|---|
| 1 | Hide the baked glTF crowd and cars at play (variant `old-no-crowd`). If Sky wants them, the alternative is the old VAT crowd renderer (one draw per citizen type) or LOD'd figures within the GDD crowd budget | **5–12** (render thread 3.3–11, GPU 1.5–4; plus culling 0.5–1.5 on the main thread) | The busy promenades and traffic of the stills. In play, the simulation's people and vehicles stand where these would; kept, they would stand frozen among them | Latch (switch); Sky decides |
| 2 | MSAA 4× → off at play (`old-no-msaa`), as ours ships | 0.8–2.0 | Edge smoothing on mullions, rails and figure outlines | Glint, Latch |
| 3 | Lean lens at play (`old-lean-lens`): DOF off, sun flare off, depth and opaque textures by request. Bloom kept | 0.5–1.4 | Nothing at play zoom: the DOF starts past the frame's farthest ground, and the sun is out of frame | Latch |
| 4 | GPU Resident Drawer on (or render through our pipeline, `old-our-pipeline`) | 0.5–1.5 (CPU, at old-content views; the 6,430 ground tiles share meshes) | Nothing, if the old shaders support DOTS instancing (to check) | Latch |
| 5 | Harbour fog full-screen pass off at play, folded into URP fog or the grade | 0.2–0.4 | A slight uniform haze over the play frame | Glint |
| 6 | Clear coat off at play (enamel through Lit, not Complex Lit, on the play camera only) | 0.3–0.8 | The lacquer's second highlight on enamel; Sky has asked for less shine | Glint, Vela; Sky decides |
| 7 | Merge the per-metre ground into one mesh per cell and material at build (meshes merged, no detail lost) | 0.2–0.8 with GRD off; about 0 with cut 4 | Nothing on screen; more mesh memory | Latch |
| 8 | Reflection probes: blending off at play, or fewer probes | 0.1–0.3 | Local reflections in enamel and chrome near the camera | Glint |

- **Sum of cuts 1–5:** about 7–17 ms at old-content views. That brings 20–30 down to an estimated 13–16: at or near 13.0, still borderline on p99. The old city's own draws without the crowd are the unknown; A answers it.
- **Zone views:** cuts 2, 3 and 5 alone give an estimated 1.3–3.1. That brings 14.0–17.5 down to about 12.4–14.4, which is our district's own range.
- **Not proposed:** decimation, triangle caps or texture downscaling. The house rule needs Sky's sign-off for those, and none of them is where the cost is.
