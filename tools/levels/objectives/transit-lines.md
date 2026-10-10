# Objective · transit lines (Datum, brief 001 output 1)
Propose the sky rail and tram alignments for the Harbour District as edits to the plan. Starting point from tide's draft (a proposal, not a given): a two-way Sky Loop round the district; T1 running through the circus; a T2 Harbour Line on Harbour Road. Keep what works, change what the plan rules reject.

You return LINES (a line = tram or skyrail, with paths, optional `order`). A path is a polyline `[x, y]` or `[x, y, r]` (r = fillet radius at that vertex, m). Existing lines you do not list are kept as they are; list a line again to replace its paths. Stops and stations are not yours: Vantage places them on your paths.

Plan checker rules your lines are judged by (the checker is the rulebook; anything not in the plan standards is rejected):
- T-R1/T-R2, S-R1/S-R2: minimum radius per path role, from `standards.tram` / `standards.skyrail`.
- T-C1: a tram path stays in street space (a road's width) or the circus island. S-C1: sky rail passes over public space only. S-P1/S-P2: pylon spans by curvature, pylons only in medians, malls, parks, quays, water or the island. Fixed context: the Skyport Pier (foot path, z 0, x 312 to 590) and apron (quay, x 480 to 590, z -40 to 40) cross the water, so the line may run over them and stand pylons on the quays.
- T-L1: a tram end inside the sheet turns on a `loop` path that joins both tracks at that end.
- N-D1: no dead ends; every path end joins another path of the same line or reaches the sheet edge (`plan.sheet` x and y limits); a path that closes on itself is fine.
- N-O1: a line whose paths chain end to end lists them in `order`; stops on several paths need an order.
- N-C1: every line connects to every other through an interchange (Vantage places those; leave the lines close enough: an interchange pair must be within `standards.skyrail.interchange`).
Do not invent powers, speeds or prices. If the sources conflict, say so in `notes`.
Hand-offs: optional `handoffs`: `[{ owner, blocking, ask }]` for what another builder must change (owner Datum, Vantage or Pulse, never yourself). `blocking: true` means your output cannot be accepted until they act: it goes to them as a fault next round. `blocking: false` is advisory and is only recorded. Leave it out when you need nothing.
