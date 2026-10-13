# 002 · Visible world: the Harbour District plan as a Unity graybox

**Status (2026-10-12): superseded by 004 from A1 on; kept for history.** A0 mostly done: `contracts/world/civic-block.json` (`suborned.block/3`), C-0 locks the café and office, the mission layer is in `contracts/missions/handover/` with its overlay checks. Left over: mission words in the plan notes and in `harbour-district.life.json` (Datum, Pulse). A1 done (B-1). A and B done under 004 (`tools/world/export.ts`, `HarbourWorldBuilder.cs`; the checker counts 52 pylons). C (Suture review) is folded into 004 E. Below, `handover-block.json`, `suborned.block/2`, `tools/plan/export.ts` and "53 pylons" are as first written.

Goal: the city stands in Unity as a graybox, rebuilt from the plan in `contracts/world/harbour-district.plan.json` by one command, so every plan change is visible the same day. The plan is the source; nothing in Unity is placed by hand.

Scope (Sky, 2026-10-10): "we're just meant to be world building here, and then adding the NPC's and mission objectives after to suit the world" · "but we need the cafe and office placed in our world".

The city's buildings are world, including the civic block: the café's corner building, the Records Office, the square, fountain, gardens, pavilions, kerb bays and cabinets, at the Oct 9 scale. The mission layer is not world: places and destinations, crew start, cast, mission routes, mission checks and mission markers. That layer is fitted to this world in 003. Nothing in the world is chosen for a mission (camera, heights, gates).

## A0 · Splice · the civic block at its Oct 9 scale, without the mission
Already done on `assignment-04` (merge it first): `bun run block` writes `handover-block.json` as `suborned.block/2` from the Oct 9 sources, and rule C-0 locks the café and office scale.
objective: rename it `contracts/world/civic-block.json` (`bun run block`). It holds:
- buildings: the café's corner building (22 × 11.3 m, 34.8 m) and the Records Office;
- structures and street furniture: fountain, gardens, pavilions, cabinets and monument;
- kerb bays, named by place (office bay, square bay).

It holds no places, destinations, crew or cast. `civic.ts` maps block → plan with `blockToPlan` alone (no testbed zones).

Move the mission layer to `contracts/missions/handover/`, unchanged. This is 003's input, not deleted. It covers:
- the destinations in today's `handover-block.json`;
- the plan's `routes` (getaway, official);
- checker rules about mission points or routes (M-1).

W-1 keeps its walks, but from building entrances (café entrance, office entrance): doors are world.

source (Oct 9, block frame: yaw 90, x south, y east), reference only, read by `bun run block` and nothing else: `~/Projects/the-suborned/game/Assets/TheSuborned/`
- `Data/World/handover-navigation.json`: solids. corner-cafe polygon x[-22, -10.7] y[-40, -18] z 0–34.8; civic-office-site x[55.5, 72.5] y[-40.5, -19.5]; pavilions, street props, floors. Leave out the mission-cast, parked-vehicle and sentry solids.
- Heights from `Editor/World/HandoverCafe.cs`: 22 × 11.3 m, corner arc 5.3, terrace 0.45, ceiling 4.6, building 34.8 m.
- Heights from `Editor/World/HandoverKit.cs`: "footprints are the nav's; heights are the city's". The Records Office is the Hall set at ×0.75 in plan and ×1.4 in height (wings ≈ 15.5 m), with the entrance towers at ×2 (crown ≈ 38 m) and a giant order of 12 m.

Reuse from run 1 (parked on branch `crew/001-run1`; read with `git show crew/001-run1:<path>`):
- `tools/plan/block.ts`: the Oct 9 extraction with source hashes. Drop its destinations and crew output.
- `contracts/world/handover-block.adjust.json`: bay and pavilion positions with reasons. Rename the bays by place.
- `unity/Assets/Suborned/Editor/Graybox/{MeshKit,Poly,Json}.cs`.

Take nothing else from run 1: it tuned plot heights and the play camera for mission sightlines.
done-check: `rg -i "handover|getaway|vale|destination|mission" contracts/world tools/plan` finds nothing. The civic block has the café and office at the sizes above. `bun run check` passes, and the checker exits 0. `CLAUDE.md`'s layout is updated.
next owner: Datum (A1).

## A1 · Datum · building scale as world standards
objective: `standards.buildings` in the plan gives every built plot its height and ground floor by use, so scale comes from the city, never from the builder or a mission. The civic block's café building and Records Office are the calibration for the rest:
- a corner building is 22 m of frontage and 34.8 m tall, with a corner arc of 5.3 m and a ground floor on a 0.45 m plinth with a 4.6 m ceiling;
- the facade bay is 6 m;
- a civic hall has wings of about 15.5 m, a crown of about 38 m and a giant order of 12 m.

Pick heights for the other uses inside these, for the city's skyline and street enclosure. Cite the source in the plan, as with the other standards.
done-check: every built plot resolves to a height through the standards (a checker rule with an id). The checker exits 0.
next owner: Splice.

## A · Splice · world export
objective: `tools/plan/export.ts` (`bun run world`) writes the derived world as JSON the Unity side can read without re-deriving anything. It covers:
- road carriageways by class, medians and kerbs;
- plots by use with their heights from the standards;
- areas, the circus, water and quay;
- tram track centrelines, sky rail beams and pylons, stops and stations;
- gates and landmarks;
- the civic block's buildings, structures and street furniture.

Frame: x east, y north, in metres; Unity = (x, height, y).
done-check: `bun run world` exits 0 only when the plan passes the checker. The output is regenerated, never hand-edited. `bun run typecheck` is clean. The schema is a short typed contract (a TS type plus one example) in `contracts/world/`.
evidence: command output, file size, and counts per layer that match the checker's own counts (e.g. 53 pylons).
next owner: Latch.

## B · Latch · graybox builder
objective: a Unity editor command that reads the export and builds `Assets/Scenes/HarbourDistrict.unity` from primitives. It builds:
- carriageways at road level, with the pavement 0.18 m above;
- plots extruded to their standard heights (built plots as blocks, parks and squares flat);
- the circus, the water plane and the quay;
- the tram track, sky rail beams at 24 m on pylons, and station and stop platforms;
- the landmarks;
- the civic block: the café building and the office at exact footprint and height, plus its street furniture.

Heights come only from the export; no height constants in the builder. Use flat colours by layer and no art. Add one overview camera and one play camera (zoom 14 at 38°) over the civic square, at a yaw chosen for reading the city. The scene is generated, so git-ignore it (Unity re-IDs churn the file on each build).
done-check: rebuilding from a clean scene gives the same result, and the Unity console has 0 errors. It works through the Unity MCP when the editor has `unity/` open, or batchmode `-executeMethod` when it doesn't.
evidence: two screenshots (overview, and the play camera) under `out/shots/`, the console output, and object counts per layer that match the export.
next owner: Suture.

## C · Suture · readability check
objective: compare the graybox with the CAD sheet (`bun run plan`, `out/plan/`). Roads, track, sky rail, stations, plot heights and the civic block should be in the right places and readable at play zoom.
done-check: a short `.html` report in `out/review/` listing every mismatch with plan coordinates, each routed to its owner (Splice for the export, Latch for the builder, Datum for the plan).
next owner: Aster.

## Then
- 003 · Missions fitted to the world: the Handover is sited in the civic block this builds, then its NPCs, objectives and routes are designed, using Assignment 3's mission generator on the crew (`003-mission-crew.md`).
- Before this: 001 · Transit network generated through `tools/rag` (Assignment 4, `001-transit-a04.md`). Its accepted plan revision is what this graybox shows.

## Open for Sky
- The plan implies left-hand traffic.
