# 048 research (Redline, 2026-10-22)

What shaped the report, with sources. The GDD and the brief stay the source; these are only the facts used.

## Studio to Air GPU scale (used only for a rough Air estimate; the Air must be measured)
- M5 Max 40-core GPU: Geekbench Metal 218,772 and 232,718. That is about 20 % above the M4 Max. https://macrumors.com/2026/03/05/m5-max-geekbench-benchmarks/
- In 3DMark Steel Nomad the M5 Max is about 8 % faster than the M4 Max (3,924 against 3,621). https://www.notebookcheck.net/Apple-M5-Pro-M5-Max-GPU-Analysis-M5-Max-GPU-on-par-with-the-GeForce-RTX-5070-and-faster-than-Strix-Halo.1246060.0.html
- Base M5 MacBook Air with a 10-core GPU: Geekbench Metal 49,557. No M4 Air score was found, so the M4 Air is taken as at or below this. https://macmyths.com/m5-chip-benchmarks-base-m5-vs-m5-pro-and-m5-max-results/
- Used as: the Air's GPU time is about 4 to 4.5 times the Studio's at the same pixel count. The Air also has no fan, so it will throttle in a long session.

## GPU Resident Drawer (URP, Unity 6)
- Requirements: Forward+, the SRP Batcher, BatchRendererGroup variants Keep All. Static batching is not a hard incompatibility, but Unity suggests turning it off. The drawer has its own GPU occlusion culling. https://docs.unity3d.com/6000.3/Documentation/Manual/urp/gpu-resident-drawer.html
- A renderer is compatible when it has a MeshRenderer, no MaterialPropertyBlock, no per-instance callbacks and a DOTS-instancing shader. To exclude one, use "Disallow GPU Driven Rendering". https://docs.unity3d.com/6000.3/Documentation/Manual/urp/make-object-compatible-gpu-rendering.html
- `Renderer.isPartOfStaticBatch` is reliable in a built player, because batches are made at build time. https://docs.unity3d.com/ScriptReference/Renderer-isPartOfStaticBatch.html
