# 026 · Splice round 2 (resume after crash)

Where I stopped: round 1 was complete in the tree (capstone.ts, capstone-rules.ts, capstone-civic.ts, rules CAP-1..CAP-7, kit adoption, world.ground.pieces). Nothing needed re-placing; I re-ran everything.

Evidence (2026-10-20 ~15:15)
- `bun run plan`: exit 0, 0 FAIL (CAP-3/4/5/6/7 pass: 1604 programme items, 97/97 no shared footprint, 218/218 planter vs palm).
- `bun run world`: exit 0. Old kit pieces placed: robots 18, vending 18, bike racks 10, sandwich boards 11, baskets 69, window boxes 50, drains 47, manholes 14, kerb 453, flags 745, pits 67, etc. 73 coverage gaps logged (listed in round1.md section 4; unchanged).
- `bun run typecheck`: clean.
- `bun test tools/world tools/kit tools/plan contracts`: 521 pass, 0 fail. `bun test tools/sim`: 15 pass.

Handover site / cover
- The cover edit (contracts/world/civic-block.cover.json, 14:03) was mine, as a side effect: `bun run cover` regenerated it after the kit.json adoption changed the kit hash. Diff is only `from.site` and `from.kit` hashes; the 100 cover points are identical. `bun tools/cover/cli.ts --check` passes (CV-1, CV-2). Final.
- sim/data/handover.site.json: I ran `bun run sim:site` (also rewrote handover.people/takedown/provisional, content unchanged apart from below). The 14:15 file already equalled the generator output, so it was in sync; its only difference from HEAD is `cover.sha256` (it records the sha of the cover file, 1624a412...). Nothing else in the site moved.
- The engine never hashes `CoverSha` (Site.cs reads it, nothing uses it), so this change cannot move any replay hash.

Sim tests: `dotnet test sim/Suborned.Sim.Tests`: 910 pass, 1 fail: ScenarioTests.Passes(DP1) (the saved drive session, got 431b63c73aa7cdae want 63366e656c2f76b1, checkpoint 0 differs at tick 100). Not caused by the site or cover (see above). Cause is Keel's in-progress engine edits (Game.cs, GameOrders.cs, Rules.cs, GameFightLab.cs all modified 15:18-15:19, after the fixture was last written 13:46). So the session needs re-recording by Keel (`suborned-sim drive-session`) once the engine edits settle; I did not re-record it.

Left: nothing in my scope. Open for Aster/Sky: the 73 gaps; kerb corner returns (offer in round1 4b).
Latch: can shoot render round 3 (world export current, ground.pieces reader and kerb split as in round1 sections 4b and 5). I touched no Unity files.
