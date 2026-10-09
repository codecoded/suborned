# 001 · Visible world: the Harbour District plan as a Unity graybox

Goal: the plan in `contracts/world/harbour-district.plan.json` stands in Unity as a graybox, rebuilt from the plan by one command, so every plan change is visible the same day. The plan is the source; nothing in Unity is placed by hand.

## A · Splice · world export
objective: `tools/plan/export.ts` (`bun run world`) writes the derived world (the geometry the checker already derives: road carriageways by class, medians, kerbs, plots by use, areas, circus, water and quay, tram track centrelines, sky rail beams and pylons, stops and stations, gates, landmarks, the Handover block's buildings and structures) as JSON the Unity side can read without re-deriving anything. Frame: x east, y north, metres; Unity = (x, height, y).
done-check: `bun run world` exits 0 only when the plan passes the checker; output is regenerated, never hand-edited; `bun run typecheck` clean; the schema is a short typed contract (TS type + one example) in `contracts/world/`.
evidence: command output, file size, counts per layer that match the checker's own counts (e.g. 53 pylons).
next owner: Latch.

## B · Latch · graybox builder
objective: a Unity editor command that reads the export and builds `Assets/Scenes/HarbourDistrict.unity` from primitives: carriageways at road level with the pavement 0.18 m above, plots extruded by use (built plots as blocks, parks and squares flat), the circus, water plane and quay, tram track, sky rail beams at 24 m on pylons, station and stop platforms, landmarks, the café and civic office at exact footprint and height. Flat colours by layer, no art. One overview camera and one play camera (zoom 14 at 38°) over the Handover block.
done-check: rebuilding from a clean scene gives the same result; Unity console has 0 errors; works through the Unity MCP when the editor has `unity/` open, or batchmode `-executeMethod` when it doesn't.
evidence: two screenshots (overview, play camera on the Handover block) under `out/shots/`, console output, object counts per layer matching the export.
next owner: Suture.

## C · Suture · readability check
objective: compare the graybox with the CAD sheet (`bun run plan`, `out/plan/`): roads, track, sky rail, stations and the Handover block in the right places and readable at play zoom.
done-check: a short `.html` report in `out/review/` listing every mismatch with plan coordinates, routed to its owner (Splice for export, Latch for builder, Datum for plan).
next owner: Aster.

## Then
002 · Mission crew: Assignment 3's mission generator on the crew, designing on the block this builds (`002-mission-crew.md`).
003 · Transit network generated through `tools/rag` (Assignment 4, due Sun 12 Oct): `003-transit-a04.md`. Runs in parallel with this brief.
