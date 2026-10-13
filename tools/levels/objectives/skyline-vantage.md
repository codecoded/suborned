# Objective · skyline stations (Vantage, brief 007 A)
Place the sky rail stations on the lines as Datum's candidate has them. You are shown the candidate's sky rail with the stations it now has, the signatures, megastructures and portals it threads, the standards that rule a station, the plan's geometry and the concept sheets as images.

What to do (brief 007 A):
- At least two stations stand inside buildings at the sky concourse (24 m), not only over streets. A station inside a building names the structure in `in` (`signatures/<id>` or `megastructures/<id>`) and stands inside that structure's declared portal on the line.
- Keep the transit rules: N-R1 reach (every area within reach of a stop or station), S-S2 spacing, no dead ends, interchanges with the tram.
- A station inside a building still reaches the street: its stair-and-escalator banks and lifts land on public ground, as the station standard says. The checker judges this; place the station so that it holds.
- `fronts` names the road or foot path whose public space the entrance fronts.

How to answer:
- The stations you return for a line replace that line's list. A station keeps its `in`, `kit` and `kitWhy` from the plan when you leave them out. Name a kit piece only if it is in the catalogue passages and is not rejected.
- You place sky rail stations only. Tram stops are not part of this run.
- If Datum's lines or portals leave no place for a station that holds the rules, say so as a hand-off to Datum (`handoffs`); do not move a line yourself.
- Cite the passages you rely on by id in `sources`. Never invent a rule.
