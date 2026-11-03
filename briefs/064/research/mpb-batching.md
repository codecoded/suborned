# MaterialPropertyBlocks, the SRP Batcher and the GPU Resident Drawer (Unity 6) — Redline, 2026-10-26

Used for brief 064's 0.2.122 follow-up (finding R3): what a per-renderer MaterialPropertyBlock costs in URP on Unity 6000.6.

- GPU Resident Drawer requirements: a MeshRenderer is drawn by the drawer only if it "does not have MaterialPropertyBlocks set on them"; otherwise "Unity falls back to drawing the GameObject without GPU Resident Drawer". Also: materials must support BatchRendererGroup (DOTS instancing).
  https://docs.unity3d.com/Manual/urp/gpu-resident-drawer.html
- SRP Batcher: a GameObject that uses a MaterialPropertyBlock is not SRP Batcher compatible (the docs list "use a MaterialPropertyBlock" as the way to opt a renderer out); shader compatibility needs UnityPerDraw / UnityPerMaterial cbuffers.
  https://docs.unity3d.com/Manual/SRPBatcher-Incompatible.html
  https://docs.unity.cn/Manual/SRPBatcher-Materials.html
- Unity roadmap, "MaterialPropertyBlock SRP Batcher compatibility": under consideration (no commitment).
  https://activation.unity3d.com/roadmap/75-material-property-block-srp-batcher-compatibility
- Reducing CPU rendering work in URP (context for the batching options):
  https://docs.unity3d.com/Manual/urp/reduce-rendering-work-on-cpu.html

Applied: SignalHead writes an MPB on every head part (41 x 7 = 287 renderers) and, with live dots, on every dot module (194), so 481 renderers draw on the per-object path. The SignalDots shader (Shaders/SignalDots.shader) keeps its properties in UnityPerMaterial but has no DOTS-instancing variant, so it would not be drawer-drawn even without an MPB.
