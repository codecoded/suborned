# Tram surface geometry checkpoint

Opus Redline provisionally accepted the corrected joined rail/bed surface, seams, crossing surface ownership and normal/tangent/UV output as an **editor-only geometry checkpoint**. Review: `tram-final-review/report.html`. The saved corrected scene and its captured payloads remain the reviewed candidate, not a player build.

Whole-tram acceptance remains on hold. The visible kerb-cut fins, oblique end closure and streaked cap UVs still need correction. G-3a remains 59 against cap 58; Keel's wider sight proposal is unapplied. Performance, memory, normal-build behavior, replay and release are not cleared.

The rewritten candidate has approximately 1.04 GB of vertex data, 1,584 separate tile meshes and approximately 2 GB of generated mesh assets. No compression is authorized by this checkpoint. Source reuse/indexing/sharing and readable-mesh ownership need an implementation decision before player use.

After review, Aster made two small source corrections:

- Normal `OldTownSwap.Build` keeps the previous shared-tile path by default. `-swapJoinedTram on` explicitly enables the experimental joined rewrite and its imported-ground/inlay/kerb corrections. Default builds do not require the topology sidecar or allocate the experimental per-tile meshes. The separate corrected-candidate entry points remain explicit experiments. This does **not** claim that the default path's known old seams are fixed.
- Replacement render objects copy their original static editor flags.

The edited assembly compiles with zero errors. These post-review changes do not rewrite the saved candidate or its historical evidence. A fresh normal-build/memory check remains required before player use; no new Unity job was run for this checkpoint.

The committed compiling dependencies include recovered Swap source whose broader behavior has only been compile-checked: SwapKeep, SwapCut, SwapOurs, SwapPlan, SwapShots and SwapWest. This checkpoint preserves that distinction rather than treating the whole recovered bundle as fully accepted. The optional tram generator diagnostic callback and sidecar API were separately checked by Splice with default generation unchanged; their focused four-test suite and typecheck passed.

Generated meshes/scenes, FBXs, images, clips, logs, build output and the held sight patch stay local. Code, checking tools, small structured evidence and reports are checkpointed. Original-asset foliage work is separate and remains review-only until its actual Unity comparison is shown.
