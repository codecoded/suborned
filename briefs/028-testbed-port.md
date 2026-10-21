# 028 · Bring the testbed over

**Status (2026-10-20):** open. Asked by Sky: "as much as possible from testbed should come over to new." Owner: Aster. Follows brief 026 (render quality), where Sky compared the old 2.5D web testbed and the old city with today's Unity world and preferred the old look nearly everywhere.

## Objective

The old 2.5D web testbed (`~/Projects/the-suborned/testbed/`, art in `~/Projects/the-suborned-store/testbed-art/`) is the reference for look and assets. Port as much of it as possible into the Unity game: faithfully, as generated kit pieces, materials and world-generator rules (deterministic, hash-checked, provenance kept), never as hand-placed scene edits.

Already under way (brief 026): road (strip-light ribbons, powder-blue enamel, lozenges, seams, pearl kerb), paving (limestone, teal tile, tree pits), café lighting (night version), café sign plate, parasols and interior, street palms, the old props (hydrant, pneumatic post, twin booth, curved kiosk, automat, control cabinet), the enforcer's run (`run_lean`).

New here: the car park stand / pickup bay (testbed `boarding-pavilion*`), and a full inventory so nothing else is missed.

## Done-check

1. `briefs/028/inventory.html`: every testbed module (`testbed/src/*.js`) and every `testbed-art/` folder, one row each: what it is, where it shows, today's equivalent, status (ported / in progress / to port / not wanted, with reason), owner.
2. The car bay ported and placed at the Handover's kerb bays (Vela piece, Splice wiring).
3. Each ported item shown to Sky as testbed | now | proposed before it replaces anything.
4. Unity match, HUD budget, verify, `game:check` green; frame time on the Air measured for the heavier pieces.

## Limits

Fixed limits (CLAUDE.md) stand: camera, exteriors only (interiors as dressing seen through glass), levels, 60 fps on the Air, no AI at runtime, rules from Keel's rulebook. Character over triangles unless a piece is placed in the hundreds and measurably costs frame time.

## Next owner

Vela (inventory, car bay), then Splice (wiring), Latch (scene), Glint (lighting), Forma (people and clips).

## Rule: compression goes through Sky (2026-10-20)

Sky: "any compression goes through me - we create a pipeline to go through and review every model that's compressed when the time comes." Crew ship full-detail models. No new decimation, segment or `lean` cuts, triangle `cap`s, slim `_Play` copies or texture downscaling without Sky's sign-off. Existing compressions (e.g. `Bollard` at lean 0.6; distance LODs) are listed in the inventory, not extended. When frame time on the Air needs it, Splice builds the review pipeline: per piece, full vs compressed renders at play zoom and close, triangle and frame cost; Sky approves each one.

## Sky's road calls (2026-10-20)
1. Light ribbon on every road class ("try all"), as Datum set in `standards.streetscape.roads`.
2. No garden fountains: the teal tile ring and bands only round the Records Square fountain outside the café, for now.
