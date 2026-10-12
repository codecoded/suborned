# 004 · Harbour World: the district as a living atompunk city in Unity

Goal (Sky, 2026-10-11): "Let's see if our world map planner can convert to a great looking Unity world. Create the harbour district, make sure it's interesting, multi level with megastructures and proper working architecture, include shops, bars, repair shops etc, all places an atompunk world would have in the retro future."

This brief takes over 002 from A1 onward and widens it from a flat graybox to a built city. 002 A0 (civic block without the mission layer) still runs first. The chain stays plan → checker → export → Unity, and every step is generated: Datum writes rules and places the big moves, generators fill the plots, and nothing in Unity is placed by hand (§09: nothing hand-authored; systems > scripts).

## Fixed for this brief
- **Exteriors only (§01).** A shop, bar or repair shop is a frontage: shopfront, canopy, sign blank, door, a service bay, and stock or tables spilling onto the pavement. Interiors are never modelled.
- **Levels are world.** The plan gains named levels: street, deck and sky concourse. Sky concourse = sky rail height (24 m, `standards.skyrail.height`) so stations open onto it. Each level is a walkable exterior surface with its stairs, ramps, lifts and bridges, and every one connects to the street (checker rule).
- **The simulation stays one level for now.** Upper levels are exported with their navigation links; Keel's rulebook (witness 13 m, sightlines, cameras) gains levels in a later brief. Nothing here changes a rule.
- **Look = §15.** Cream limestone, teal and mint enamel, chrome, coral transit. Every roof programmed, towers mixed in, composed never a grid. No text on signs: a sign is a shape, colour and icon.
- **Scale is calibrated by the civic block** (café corner building 22 m frontage, 34.8 m tall, 0.45 m plinth, 4.6 m ground-floor ceiling, 6 m facade bay; Records Office wings ≈ 15.5 m, crown ≈ 38 m, giant order 12 m).
- **Graybox first (Sky, 2026-10-11).** C builds a graybox: real massing, levels and frontages, flat §15 palette colours, no detail kit. D (look) starts only after the graybox is reviewed.
- **Rules may flex; the GDD follows.** Levels are a GDD change: Aster versions the GDD (v0.16.0) from A1's numbers, then `bun run crew`.
- **No paid spend.** Geometry and materials are procedural in Unity; painted plates (Runware) are a later, priced step.

## A0 · Splice · civic block without the mission layer
As written in `002-visible-world.md` A0. Runs in parallel with A1.

## A1 · Datum · the world program (in the plan)
objective: extend `contracts/world/harbour-district.plan.json` so the district reads as a working atompunk city. The program is rules and the big moves, not shop-by-shop placement:
1. `standards.buildings`: storey heights, plinth, bay, and height bands by use, calibrated on the civic block (002 A1).
2. `standards.levels`: street 0, deck, sky concourse 24 m; minimum widths; stair, ramp and lift spacing; how a level meets a station.
3. `standards.frontage` (tenancy catalogue): the ground-floor and deck trades an atompunk harbour city has, each with a frontage width range, depth, service needs, opening hours and the street types it wants. Examples: diner, automat, cocktail and tiki bar, atomic appliance showroom, robot repair, hover-car garage and service station, rocket-parts chandler, radio and TV repair, launderette, barber, newsstand, tailor, cinema, bowling, hotel lobby, ship's chandler, bonded warehouse, tram and sky rail kiosks. Each area gets a mix (e.g. Market Docks = repair and trade; Lantern Quarter = bars and nightlife; Crescent = shops and cafés).
4. **Megastructures:** 2–4 named, sited structures that give the district its skyline and its levels, each with a footprint, levels, height, the plots or streets it spans, and why it is there. Candidates: a harbour deck over Market Docks (cargo below, market above), a stacked arcade bridging a street with skybridges into the Skyport Line, the Skyport pier tower, terraced housing over a podium.
5. **Streetscape** (Sky, 2026-10-11: "kiosks, tram stops, phonebooths, tree lined streets and rooftops"): `standards.streetscape` sets the street section for each road class (pavement zones, trees, lamps), the street furniture catalogue (staffed or not, blocks sight or not), the transit kits, roof item sizes, and watcher mounts. The generator places it all per street edge; it is world the simulation sees, not dressing.
6. Every new element cites its source (GDD section or the civic block), as the existing standards do.
done-check: `bun tools/plan/cli.ts contracts/world/harbour-district.plan.json` exits 0. Any new key Splice must check is listed in a hand-off with the rule it needs (id, condition).
next owner: Splice (B).

## B · Splice · generator, checker rules, export
objective:
- **Checker rules** for A1's hand-offs. As a minimum: every built plot resolves to a height; every level connects to the street; megastructure pylons and columns land in allowed zones; frontages fit their plot edges.
- **`tools/world/` generator** (deterministic, seeded by plot id): fills each plot from the standards. It produces building masses (podium, body, setbacks, towers, every roof programmed), facade bays, ground-floor tenancies picked from the area's mix along street edges, entrances, decks, stairs and lifts, and the megastructures.
- **`bun run world`** writes `out/world/harbour-district.world.json`. Frame: x east, y north, metres; Unity = (x, height, y). Exit 0 only when the plan passes the checker. Its schema (a TS type plus one example) goes in `contracts/world/`.
done-check: `bun run check` passes; running `bun run world` twice gives byte-identical output; the counts per layer (plots, buildings, tenancies by trade, levels, megastructures, pylons) are printed and match the checker.
next owner: Latch.

## C · Latch · Unity world builder
objective: an editor command (MCP, or batchmode `-executeMethod`) that reads the world export and builds `Assets/Scenes/HarbourDistrict.unity` (generated and git-ignored). It builds:
- roads, kerbs, the circus, water and the quay;
- the tram and sky rail;
- buildings from their masses (podium, body, setbacks, towers, roofs), frontages as coloured blocks by trade, and entrances;
- decks, bridges, stairs and lifts;
- the megastructures and the civic block at exact size.

Use URP materials from the §15 palette (`contracts/style/suborned.style.json`); no height or colour constants in C#. Add an overview camera and a play camera (zoom 14 at 38°).
done-check: a rebuild from clean is identical; the console shows 0 errors; object counts match the export.
evidence: `out/shots/`: overview, play camera at the civic square, a street of shops, a megastructure, a deck level.
next owner: Glint, then Vela.

## D · Glint + Vela · make it look good
Glint: one lighting setup that makes the city read §15 (warm hazy sun, long soft shadows, one haze colour across fog and the far band). Vela: material passes and the procedural facade kit's detail (fins, canopies, chrome trim, enamel panels), still procedural.
done-check: the shots sit beside the §15 board without a seam (Sky judges).

## E · Suture · review
Compare the Unity world with the CAD sheet and the export, check readability at play zoom (signal colours stay unused by surfaces), and report mismatches by owner in `out/review/`.
next owner: Aster.

## Decisions and carry-overs (Aster, 2026-10-11)
- Transit: A04 run r008 promoted into the plan (v0.2.0, Sky approved). N-R1 and S-S2 pass with no waivers.
- **Building line (generator):** plots run to the kerb; buildings stand at the plot edge set back by the street section's pavement (`standards.streetscape.pavement` and `sections`). Civic blocks keep their 4 m walk. Frontage fit (F-1) is measured on the building line.
- **Checker rules owed by Splice:** B-1…B-6, L-1…L-6, MS-1…MS-5, F-1…F-6 (A1 hand-off), SF-1…SF-17 (`standards.streetscape.rules`), and Datum's scratch megastructure check `out/datum/mega-check.ts` turned into rules.
- **Level-change times** (stair 130 s, escalator 105 s, lift 31 s street → concourse) and **watcher cones** are Keel's to own (§05 levels: "time from the plan"); Pulse owns staffed shifts.
- **Look:** Sky wants the world bright and colourful; Vela's palette options decide §15's palette (Sky picks).
- **Palette (Sky, 2026-10-12): a mix.** sunrise's warm cream and peach base (ground, stone, bodies: closest to §15 limestone, best for reading characters), fair's saturated enamel accents (cobalt, flamingo, sunshine, mint, turquoise), lagoon's turquoise water. Transit keeps coral (§15 identity). Colour lives on enamel panels, awnings, signs, roofs and trim, not whole buildings. Vela finalises it as `look.json`, then Aster updates §15 + `contracts/style/suborned.style.json` (v0.17.0).
