# 001 · Visible world: the Harbour District plan as a Unity graybox

Goal: the city stands in Unity as a graybox, rebuilt from the plan in `contracts/world/harbour-district.plan.json` by one command, so every plan change is visible the same day. The plan is the source; nothing in Unity is placed by hand.

Scope (Sky, 2026-10-10): "we're just meant to be world building here, and then adding the NPC's and mission objectives after to suit the world" · "but we need the cafe and office placed in our world".

The city's buildings are world, including the civic block: the café's corner building, the Records Office, the square, fountain, gardens, pavilions, kerb bays and cabinets, at the Oct 9 scale. The mission layer is not world: places and destinations, crew start, cast, mission routes, mission checks and mission markers. That layer is fitted to this world in 002. Nothing in the world is chosen for a mission (camera, heights, gates).

## A0 · Splice · the civic block stays, the mission layer moves out
Done already (keep): `handover-block.json` as `suborned.block/2` from the Oct 9 sources, with the café at 22 × 11.3 m and 34.8 m, the Records Office, and Vantage's adjustments.
objective:
- Rename it to the civic block: `civic-block.json`, `civic-block.adjust.json`, `bun run block`. Bays are named by place (office bay, square bay), not by mission role.
- Move the mission layer to `contracts/missions/handover/`, unchanged in content. This is 002's input, not deleted. It covers:
  - the block's `destinations` and `crew`;
  - the plan's `routes` (getaway, official);
  - the mission checks (M-1, V-1 and any other rule about mission points; they move to `tools/missions/` in 002);
  - the visibility study (`Study.cs`, `V1.cs`, `tools/unity/v1.ts`, `out/shots/study/`);
  - mission markers in the Unity build.
- C-* (café and office inside their blocks) stays: it checks buildings.
- Update `CLAUDE.md`'s layout.

done-check: `rg -i "handover|getaway|vale|café door|destination|mission" contracts/world tools/plan unity/Assets/Suborned` finds nothing. The world export still has the café and office at their Oct 9 footprint and height. `bun run check` passes, and the checker exits 0.
next owner: Datum (A1), Splice (A).

## A1 · Datum · building scale as world standards
objective: `standards.buildings` in the plan gives every built plot its height and ground floor by use, so scale comes from the city, never from the builder or a mission. The civic block's café building and office (placed, Oct 9 scale) are the calibration for the rest (reference only: `~/Projects/the-suborned/game/Assets/TheSuborned/Editor/World/HandoverCafe.cs`, `HandoverKit.cs`):
- a corner building is 22 m of frontage and 34.8 m tall, with a corner arc of 5.3 m and a ground floor on a 0.45 m plinth with a 4.6 m ceiling;
- the facade bay is 6 m;
- a civic hall has wings of about 15.5 m, a crown of about 38 m and a giant order of 12 m.

Pick heights for the other uses inside these. Cite the source in the plan, as with the other standards.
done-check: every built plot resolves to a height through the standards (a checker rule with an id). No height constant is left in the builder (`Style.BlockHeight` goes). The checker exits 0.
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

Use flat colours by layer and no art. Add one overview camera and one play camera (zoom 14 at 38°) over the civic square, at a yaw chosen for reading the city, not for mission points.
done-check: rebuilding from a clean scene gives the same result, and the Unity console has 0 errors. It works through the Unity MCP when the editor has `unity/` open, or batchmode `-executeMethod` when it doesn't.
evidence: two screenshots (overview, and the play camera) under `out/shots/`, the console output, and object counts per layer that match the export.
next owner: Suture.

## C · Suture · readability check
objective: compare the graybox with the CAD sheet (`bun run plan`, `out/plan/`). Roads, track, sky rail, stations and plot heights should be in the right places and readable at play zoom.
done-check: a short `.html` report in `out/review/` listing every mismatch with plan coordinates, each routed to its owner (Splice for the export, Latch for the builder, Datum for the plan).
next owner: Aster.

## Then
- 002 · Missions fitted to the world: the Handover is sited in the world this builds, then its NPCs, objectives and routes are designed, using Assignment 3's mission generator on the crew (`002-mission-crew.md`).
- 003 · Transit network generated through `tools/rag` (Assignment 4, due Sun 12 Oct): `003-transit-a04.md`. This is world work and runs in parallel with this brief.

## Decisions (Aster, 2026-10-10)
- The generated `HarbourDistrict.unity` is git-ignored: it is derived from the plan and rebuilt by command (Redline: Unity re-IDs churn the file on each build).
- Block heights move from the builder (`Style.BlockHeight`) into the plan: the plan is the source for anything that decides what the player can see. This is now A1.
- Moved to 002 by the scope change; the world does not choose its camera or heights for a mission:
  - play-camera yaw by mission-point visibility (90 vs 270, `out/shots/study/visibility.json`);
  - Rocket Gate on both bay routes;
  - the "mission points visible at play yaw" rule;
  - café–office framing;
  - bay route lengths (getaway 696 m, official 1023 m).
- Still open for Sky, a world question: the plan implies left-hand traffic.
