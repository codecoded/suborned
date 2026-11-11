# 106 · Unity's free built-ins: occlusion, Mesh LOD, STP

Owner: Latch (unity-engineer). Aster, 2026-11-11. Next owner: Aster (commit), then Sky (play).

Sky, 2026-11-11: "I still feel we're not using Unity to its fullest. Do a deep dive into every plugin out there", then "Yes" to this round (switch on the free built-ins and measure them on the Air). Research: `out/research/reports/Unity 6 plugins for The Suborned.md` (ranked table, rows 1–3 and 7).

## Objective
Switch on three things Unity 6.6 already ships, each behind a switch so it can be A/B'd without a rebuild, and measure each on the Air at play zoom:
1. **GPU occlusion culling.** `m_GPUResidentDrawerEnableOcclusionCullingInCameras` is 0 in `unity/Assets/Settings/PC_RPAsset.asset`; turn it on (renderer is already Forward+ with the Resident Drawer). Unity warns it can cost more than it saves when little is hidden: measure, don't assume.
2. **Mesh LOD** on what has none today: the 48 m ground chunks from `DecoKitTileMerge.cs` (~45% of the boulevard draw), then one kit family moved from LOD Groups to Mesh LOD (Unity says don't mix the two on one object). `MeshLodUtility.GenerateMeshLods` is already used in `DecoKitCrowdMerge.cs`. The VAT crowd's `RenderMeshPrimitives` needs `RenderParams.forceMeshLod` if it ever uses it. Target: tp-street under 6M triangles (6.4M today; brief 101 r303 calls it draw distance).
3. **STP upscaling** at 67–77% render scale for fullscreen 2880×1864. Check readability as well as frame time: no smearing on the thin brass lines, the moving signals, the ring or the outlines.

## Done-check
- A table: for each switch, off vs on, at play zoom and the tp-long view on the Air: frame time (median, p95), triangles, draw calls. From the existing perf harness (`Runtime/Perf/`), not by eye.
- Stills of signals and brass lines with STP off and on, same frame.
- Each switch left on only where it wins; the rest left off with the number that says why.
- One build for Sky at the end via `bun run batch --sky` (full batch behind). No full batch per attempt; a kit/LOD change that forces the town rebake (~14 min) happens once.

## Rules
- Delta only. The Air run is OK'd by Sky for this measurement. Kill by PID only. Don't commit; Aster commits. Say how long a step takes before starting it.
- Out of scope: Cinemachine/Timeline (their own round), any paid asset, the weather study (brief 107, Glint, desk only).
