# 007 · Space-age pass: the Harbour District reads as the concept work

**Status (2026-10-13): started.** A, B, C and D are running. F follows B.

Goal (Sky, 2026-10-13, after the first flyby): "I didn't see some of the really cool megastructures our DecoKit had. Most of these too close to modern city, not a retro future / optimistic space age vibe." · "SpaceHQ as dominant is fine, and would love the sky rail to thread more the buildings like one would expect."

The bar is the concept work, not prose: `~/Projects/the-suborned-store/concepts/AtomicAge/contact_sheet.jpg` (exposition palace, power spire, monorail terminal, grand and starlight hotels, googie café, planetarium) and `Blocks/contact_sheet.jpg` (hanging terraces, lifted megablock, machine crowns, mixed skyline, needle trio, rail through, saucer spires, skyport tower), plus `Horizon/`. Every shot is judged beside them.

## Decisions (Sky, 2026-10-13)
- **Signals render unlit and emissive.** Cones, guard and rival markers draw unlit, each with its shape or motion cue, so lighting never shifts them. The ΔE2000 ≥ 20 gate applies to the unlit palette against the signal colours (Glint measured that lit and shaded surfaces can't hold 20). Recorded in GDD v0.18.0 (§10, §15).
- **Spires may be taller than the Skyport.** Inland rivals reach 123–194 m; the Skyport (108 m) is the harbour's icon, and nothing within 150 m of the quay is taller.
- **SpaceHQ is the city's flagship and centrepiece**, at Crown Circus, the hub, as Datum sited it.
- **SpaceHQ is crowned with an atom or starburst over a needle mast, not a rocket** (Sky, after the second flyby). It's like the Exposition Palace and Grand Hotel concepts. The kit's rocket liner, landing pad and gantry come off. Rockets stay at the horizon launch pads. The thin striped shaft becomes a heroic fluted cream-and-gold tower with streamline fins. Owner: Vela.
- **The ocean liner is concept B, "atomic"** (Sky, from `briefs/007/liner-*.jpg`, made with the Codex CLI image tool at no cost; prompts in `liner-provenance.json`). It has a flared white hull with a coral boot-top and a gold sheer line, a domed glass observation lounge at the bow, twin swept-back coral tail-fin funnels with teal bands, a chrome radar mast topped by an atom, and a rounded superstructure with ribbon windows. It replaces the current OceanLiner, which reads as a modern cruise ship. Owner: Vela.
- **Performance target: 60 fps at 1080p on a MacBook Air M4** (Sky, later on 2026-10-13; this replaces the mid-range-PC-only target set earlier that day). The Air is the reference machine. It has no fan and a 10-core GPU, so it's measured warm, after several minutes of running, at play zoom with people, vehicles and the simulation in, by a perf harness in a development build (Redline's action 1, built by Latch). A mid-range PC (RTX 3060-class GPU, 6-core CPU) must also pass, as a check, not the bar. Reason: the play camera sees a small slice of the city (Latch counted 230–360k triangles in view at three stops), so integrated graphics is the right bar. Recorded in GDD v0.18.0 (§11).
- **The Parade stays open to the sky** (Sky, from concept art in `briefs/007/`, made with the Codex CLI image tool at no per-image cost; prompts in `parade-provenance.json`). The sky rail passes through the gate block's portal. Glazed barrel canopies run over the side gallery promenades only, never over the carriageway or the play space. Reasons: the tactical camera sees the street, its sun and shadows, and the full skyline of SpaceHQ and the spires. Reference: `parade-B-open-*.png`, with the side canopies from `parade-A-vault-aerial.png`.

## What went wrong (Aster's review of the flyby)
1. **006 B rejected the signature pieces because the plan had no sites.** It treated the plan as fixed ("a kit piece is chosen to fit, never the other way round"). SpaceHQ, Skylon, DiscTower, Tower_Needle, Tower_Starburst, Tower_Terrace, LinerDock and SkyBridge were all rejected for want of a site or a span. **That rule is lifted for the skyline:** the plan now makes sites for the signature structures. Scale, levels, clearances and the checker still rule how they stand.
2. **Ordinary buildings are modern boxes.** Facade wave 2 never ran, so the generator makes flat-roofed slabs with blue glass grids. The concepts have streamline curves, rounded corners, horizontal speed bands, portholes, stepped setbacks and a crown on every tower.
3. **Nothing frames the district.** There's a flat sky, no haze, and the sea stops at x 740.

## Fixed
- **SpaceHQ is the dominant icon** (Sky, 2026-10-13). It's the tallest thing in the district, about 252 m as built; everything else composes around it. The Skyport tower stays the harbour's second icon.
- **The sky rail threads the buildings** (Sky, 2026-10-13), as in "rail_through" and "monorail terminal". Beams pass through portals in buildings and megastructures, and stations sit inside buildings at the sky concourse (24 m), not only over streets. This is a world rule change: rules that keep the sky rail over public space only (e.g. S-C1) gain a "through a building portal" case, with portal clearance as a standard.
- **Look = the concept work**, with the GDD v0.17.0 palette (pastel cream and peach base, saturated enamel accents, coral transit). No glass curtain walls; glazing is set in stone and chrome frames.
- **The chain doesn't change:** plan → checker → `bun run world` → Unity, all generated, nothing hand-placed. The A04 runs on `assignment-04` are frozen. On main, a checker change that alters recorded A04 runs is reported as replay drift by cause, never "fixed" by editing `runs/`.

## A · Datum · the skyline and the threaded sky rail (plan)
objective: re-plan the district's big moves in `contracts/world/harbour-district.plan.json`:
1. **Sites for the signature structures.** SpaceHQ (dominant), Skylon, DiscTower, Tower_Needle, Tower_Starburst, Tower_Terrace, and LinerDock on the quay (a harbour has a berth). Add a SkyBridge where two towers stand at its span. Compose the skyline from the concept sheets: one dominant icon, a cluster of rival spires, and crowns everywhere. Raise height bands where the skyline needs it, citing the concept sheet as the source.
2. **The sky rail through the buildings.** Realign the sky rail lines so they thread megastructures and towers through portals, with at least two stations inside buildings at the concourse. Keep the transit rules: N-R1 reach, S-S2 spacing, no dead ends, interchanges.
3. Revisit every rejection in the 006 B note: adopt the piece or give a new reason.

done-check: the plan checker exits 0 with the new standards (portal clearance, signature sites), and any new rule is handed to Splice (id, condition, source, failing example). The 006 B note is replaced by a ≤ 30-line adopt/reject list.
next owner: Splice (rules, generator), Vela (pieces regenerated to fit).

## B · Splice · facade wave 2 and the space-age massing grammar (generator)
objective: `tools/world/` stops making modern boxes:
- **Kit refs for building bays and corners:** StreamBay, DecoBay, StreamEnd, StreamCorner, CornerTower and HallEntranceTower, by storeys, use and corner, seeded by id with neighbour contrast. There's a test that no two adjacent facades duplicate (owed from 006 A).
- **Procedural fill follows the same grammar:** rounded streamline corners, horizontal speed bands, set-in glazing and portholes, stepped setbacks, and every roof programmed. Every tower gets a crown (finial, atom, starburst, saucer, antenna or dish).
- **The rule changes from A:** portal clearance, sky rail through buildings, and signature sites. Each comes with an id, a source and a failing example.

done-check: `bun run check` passes, and `bun run world` is byte-identical twice. Coverage per layer prints kit vs procedural, with the procedural share of building faces down from today's.
next owner: Latch.

## C · Vela · pieces, horizon and grammar sign-off
objective:
- Regenerate, through `tools/decokit` (Vela owns the pieces), the signature pieces whose plan sites need a different size: SkyRailStation (the plan's 24 m twin-beam, 50 m side platforms) and the portal versions of the threaded buildings.
- Build the **horizon band**: distant deco towers, rockets and planets from the `Horizon/` concepts, beyond the district edge. Extend the sea to the horizon.
- Write the massing grammar Splice implements (B) as a short spec with references to concept images.

done-check: `bun run decokit` and `bun run kit` succeed with hashes. The spec sits in `contracts/world/` and is cited by B.
next owner: Splice, Latch.

## D · Glint · light and haze
One warm low sun and one haze colour across fog, sea and the horizon band, as in the concept sheets. The signal colours stay readable (GDD v0.17.0 §15, ΔE ≥ 20).

## E · Latch · rebuild and flyby
Rebuild from clean. The flyby gains stops at SpaceHQ, the spire cluster and a sky rail run through a building. Render Flyby Frames writes `out/shots/flyby/flyby.mp4` directly (ffmpeg, 1920×1080, 30 fps). The props audit reports 0 misses.
done-check: 0 console errors; counts match the export.
next owner: Sky judges the flyby beside the concept sheets; then Suture reviews.

## F · Splice · one generation pipeline: the skyline first, missions next
Why (Sky, 2026-10-13): missions must be planned through the build-time pipeline (§09: generated, checked, replayable), and so must the world's big moves. Datum's interactive pass in A is a fast first try for the flyby. The accepted pipeline run replaces it, as r008 replaced the hand-drawn transit.
Starts after B lands (one Splice in `tools/` at a time).
objective:
- Extract the general loop from `tools/levels`: rounds ≤ 3, retrieval through `tools/rag` `runAgent` (SurrealDB), structured output per objective, an apply step, deterministic checks, Keel as critic, faults routed to their owner with cited chunks pinned, a recorded run and offline replay.
- Objectives, schemas, apply and checks become plug-ins. Transit is the first plug-in, unchanged in behaviour.
- Add `bun run levels skyline` (or `bun run gen skyline`): Datum proposes the signature sites and the threaded sky rail, Vantage the stations inside buildings, the plan checker judges, Keel critiques. The concept sheets are pinned run inputs with their hashes; retrieval covers the GDD, the plan and `kit.json`.
- 003 then becomes "add the mission objectives and the mission checker to this pipeline", not a second port.

done-check:
- `bun run verify` still says VERIFY OK: every recorded A04 run replays, and drift is labelled by cause. `bun run check` passes.
- One real skyline run ends ACCEPTED and replays offline. Its plan goes to Sky before it replaces `contracts/world/harbour-district.plan.json`.
- The `assignment-04` branch is untouched.
next owner: Aster (accept), Sky (approve the plan), Latch (rebuild).

**Status (2026-10-14): step 1 done.** The loop is in `tools/gen/` (plug-in interface, loop, replay, crew checks) and transit runs on it unchanged: 850 offline output files identical before and after, all ten recorded runs replay byte-identical, VERIFY OK. Step 2 (the skyline plug-in) is designed in `out/review/007F-skyline-plugin.html`, not built.

Decisions (Sky, 2026-10-14), done before step 2:
- **The critic runs only on a checker-clean candidate.** A candidate the checker fails goes straight back to its owner with the checker's faults (in the ten A04 runs Keel judged 11 candidates the checker had already failed). Recorded runs made under the old rule must still replay.
- **A rejected run can be continued.** A follow-on run starts from the rejected run's last candidate and its open faults, not from the base plan (three rejected A04 runs took 38% of the spend). The link to the parent run is recorded and replays.
- **The corpus gains the plan's `signatures[]`, `megastructures[]` and tower sites, and `kit.json`.** The chunker version goes to 3 and the index is re-ingested; replay labels the chunker drift on older runs by cause.
- **The crew sees the concept sheets as images.** A paid experiment is approved: image input through `tools/rag`, compared with a text digest of the same sheets.

## Carried over from the first flyby
- Stair towers stand on 6 of the 8 sky-rail platforms (`bank/S1/circus/l/tower` covers 3 m of the 4 m platform). Owners: Datum and Splice, folded into A and B.
- 63 frontage signs stand 3.2 m off the wall instead of 0.8 m (the market-deck stalls and the skyport-tower frontages). Owner: Splice.
- The civic square's fountain, pavilions and café furniture are plain extrusions. Owner: Vela, with Furnish and Plaza kit.
