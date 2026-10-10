# Objective · stops and stations (Vantage, brief 001 output 2)
Place tram stops and sky rail stations on the lines Datum proposed (the candidate plan is in the task). Positions on the lines, platforms, entrances fronting public space, interchange pairs; every area in reach of one.

You return STOPS: every stop or station of every line you list (the list replaces that line's stops). `path` names one of the line's paths; `at` must lie on it; `fronts` names the road or foot path whose public space the entrance opens onto. Give sky rail stations a `length`. Mark an interchange as `"<line>/<stop id>"` on both members of the pair.

Plan checker rules your stops are judged by:
- T-S1: tram stop spacing and S-S2: sky rail station spacing, measured along the line's ordered paths (`standards.tram.stopSpacing`, `standards.skyrail.stationSpacing`).
- T-S2/S-S1: platforms on straight track or beam.
- N-P1: a stop sits on a path of its own line (within `standards.transit.onLine`). N-P2: no stop on a plot.
- N-R1: every plot centroid within `standards.transit.reach` of a stop or station (the worst plot is reported).
- W-2: every stop or station can be walked to from public space. Water and buildings block; roads are crossable. The Skyport Pier (foot path `skyport-pier`, z 0, x 312 to 590) and the Skyport apron (quay, x 480 to 590, z -40 to 40) are walkable over the water; the rest of the water (x 330 to 740) is not.
- W-3: `fronts` is required on every stop and must name an existing road or foot path id; the stop must be within 6 m of that road or foot path's edge (centreline distance minus half its width). A stop over water fronts the pier, not a street. Once any stop names `fronts`, all must.
- S-I1/N-C1: interchange partners within `standards.skyrail.interchange`; the network of lines is one connected whole.
Do not move lines; if a line cannot satisfy a rule, say so in `notes` for Datum.
Hand-offs: optional `handoffs`: `[{ owner, blocking, ask }]` for what another builder must change (owner Datum, Vantage or Pulse, never yourself). `blocking: true` means your output cannot be accepted until they act: it goes to them as a fault next round. `blocking: false` is advisory and is only recorded. Leave it out when you need nothing.
