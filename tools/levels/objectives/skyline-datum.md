# Objective · skyline (Datum, brief 007 A)
Re-plan the district's big moves: the sites of the signature structures and the sky rail that threads the buildings. You are shown the plan's signatures, tower sites, megastructures and sky rail, the standards that rule them, the geometry to place things on, the two concept sheets as images with their tile ids, and the massing grammar sections that bear on the skyline. Return edits, not a new plan.

Fixed (Sky, 2026-10-13):
- SpaceHQ is the dominant icon: the tallest thing in the district (about 252 m as built), the city's flagship and centrepiece, at Crown Circus, the hub. Everything else composes around it. It is crowned with an atom or a starburst over a needle mast, not a rocket; the kit's rocket liner, landing pad and gantry come off. Rockets stay at the horizon launch pads.
- The Skyport tower (108 m) is the harbour's second icon. Inland spires may be taller than the Skyport (rivals 123–194 m), but nothing within 150 m of the quay is taller than it.
- The sky rail threads the buildings, as in `blocks/rail_through` and `atomic/monorail_terminal`: beams pass through portals in buildings and megastructures, and stations sit inside buildings at the sky concourse (24 m), not only over streets. A building may stand over or around a line only at a declared portal; portal clearance is a standard of the plan.
- The Parade stays open to the sky. The sky rail passes through the gate block's portal. Glazed barrel canopies run over the side gallery promenades only, never over the carriageway or the play space.
- The look is the concept work with the GDD palette: pastel cream and peach base, saturated enamel accents, coral transit. No glass curtain walls; glazing is set in stone and chrome frames.
- The earlier rule that a kit piece is chosen to fit a plan that is fixed is lifted for the skyline: the plan makes the sites for the signature structures. Scale, levels, clearances and the checker still rule how they stand.

What to do:
1. Sites for the signature structures: SpaceHQ (dominant), Skylon, DiscTower, Tower_Needle, Tower_Starburst, Tower_Terrace, and LinerDock on the quay (a harbour has a berth). Add a SkyBridge where two towers stand at its span. Compose the skyline from the concept sheets: one dominant icon, a cluster of rival spires, and crowns everywhere. Where the skyline needs a higher band, say so in `notes` and cite the sheet; the standards are not yours to edit in this run.
2. The sky rail through the buildings: realign the sky rail lines so they thread megastructures and towers through portals, with at least two stations inside buildings at the concourse (Vantage places the stations on your lines). Keep the transit rules: N-R1 reach, S-S2 spacing, no dead ends, interchanges.
3. Revisit every rejection in the 006 B note (the plan's notes; retrieve them): adopt the piece, or give a new reason in `notes`.

How to answer:
- Every `signatures`, `towerSites` and `portals` entry is an upsert by id on the plan you are shown; a field you leave out keeps its value. To delete one, name its id in `remove`. You do not edit the plan's standards.
- Every site, signature and portal cites at least one concept tile in `concepts`, by the ids of the index in the task (for example `blocks/rail_through`). An id that is not in the index is a fault.
- A piece that must be built or rebuilt at another size goes in `pieceRequests` (for Vela, outside this run); it is not applied to the plan. Name only kit pieces that exist in the catalogue passages; a rejected piece is not a choice, and a signature's `regen.variant` has to be an adopted piece (a candidate is not built yet).
- A sky rail line that threads a building needs the portal declared on the signature or megastructure it passes through. Declare it with the clearance the plan's standard gives.
- Cite the passages you rely on by id in `sources`. Never invent a rule, a height or a price.
