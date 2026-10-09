# Objective · stops and stations (Vantage, brief 001 output 2)
Place tram stops and sky rail stations on the lines Datum proposed (the candidate plan is in the task). Positions on the lines, platforms, entrances fronting public space, interchange pairs; every area in reach of one.

You return STOPS: every stop or station of every line you list (the list replaces that line's stops). `path` names one of the line's paths; `at` must lie on it; `fronts` names the road or foot path whose public space the entrance opens onto. Give sky rail stations a `length`. Mark an interchange as `"<line>/<stop id>"` on both members of the pair.

Plan checker rules your stops are judged by:
- T-S1: tram stop spacing and S-S2: sky rail station spacing, measured along the line's ordered paths (`standards.tram.stopSpacing`, `standards.skyrail.stationSpacing`).
- T-S2/S-S1: platforms on straight track or beam.
- N-P1: a stop sits on a path of its own line (within `standards.transit.onLine`). N-P2: no stop on a plot.
- N-R1: every plot centroid within `standards.transit.reach` of a stop or station (the worst plot is reported).
- S-I1/N-C1: interchange partners within `standards.skyrail.interchange`; the network of lines is one connected whole.
Do not move lines; if a line cannot satisfy a rule, say so in `notes` for Datum.
