# 086 · LOD pass for the kit

Owner: Aster. Opened 2026-10-31. Sky: "next pass is a LoD pass for our kit - every key item having a LoD pass - same size so no popup occurs. Implementing AAA LoD technicques in our game so it runs perfomant".

This is the "when the time comes" of Sky's standing rule (2026-10-20, "any compression goes through me - we create a pipeline to go through and review every model that's compressed"). Crew may now make LODs. Each piece's before and after still goes through Sky's review, and character and silhouette beat triangles (the hydrant and the mailbox).

## Objective
Every key item in the kit has authored LODs that hold the same silhouette and bounds, switch with no visible pop, and bring the game to its limit: 60 fps at play zoom on the 15-inch MacBook Air M4.

## Done-check
- A per-piece list of what's a key item, what it costs placed, and its LODs (triangles, switch point, shadow caster).
- No pop: at each switch, a rendered A/B of the same frame shows a measured image difference under a set limit, the silhouette and bounds stay within a set tolerance, and Sky sees it in motion, in both views, zooming.
- Frame time measured on the Air, before and after, in the player, in the worst views; third person (`tp-worst`, 6.9 M triangles against 6 M) back under its limit.
- A review page Sky can go through piece by piece: before, after, triangles and frame cost. Nothing replaces a full-detail model until he has passed it. Full-detail sources stay.

## Owners
Redline (audit, the plan and the technique picks) · Vela (pieces and their LODs, `tools/decokit`) · Splice (the pipeline, catalogue, review page, `tools/kit`) · Latch (LODGroups, crossfade, Unity settings) · Glint (shadows and light on the switch) · Forma (figures, only if the audit says so) · Sky (each piece).

## Depends on
- The current round (brief 085) lands first; only reading until Latch's build is done, so the player and scenes stay stable.
- The lens: Sky's camera call (play view orthographic or perspective). An orthographic camera picks a LOD from zoom alone, the same for near and far things; a perspective one picks by distance. Thresholds and what's worth authoring depend on it.
