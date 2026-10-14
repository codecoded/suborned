# Objective · cast tables (Pulse, brief 010)
Propose the cast tables for the scope in your task: who works each unit and lives near it, as a mix. You write rows, not people. Code (tools/people) turns each row into individuals, seeded by plot and unit id, and then the checker holds both the rows and the people to `people.rules.json`. Anything the rules do not list is rejected; do not invent temperaments, needs, tie kinds or positions.

One row per area × occupation in the scope (the task lists them; add none, drop none). Each row says:
- `positions`: the jobs in the unit and their weights. A trade's positions are its category's in the rules (the keeper is made by code, one per unit; do not weight it). A civic employer's are the roles the rules give it (clerk, guard, official-driver, councillor). Residents are `resident`. Each position has a `post`: `door` (stands at the unit's door), `inside` (off-screen), `bay` (at the nearest bay: drivers).
- `temperaments` and `needs`: weights over the rules' lists. A person only draws a temperament their position allows and a need their temperament can be approached on (the rules' `respondsTo` against a need's `metBy`), and a need whose requirement they do not meet (home, work, family tie, rival tie) is not drawn. Spread them so no job reads as one kind of person (GDD §09 risk: sameness), but fit them to the work: a guard is Loyal or Mercenary, a driver on cash.
- `ties`: patterns, kind and the share of the unit's people who carry one. Workmates for a shift, regulars where the routine passes a unit, family at home, debtors, the odd rival.
- `routines`: variants over the life.json skeletons, fitted to its bands and to the unit's hours: how long before the shift they leave, whether they live on the sheet and walk or come by the tram, a meal on site or in the square, a stop at the café before the shift (the Office guard's, GDD §05), an hour in the square after. Code times every walk on the walk graph and makes the stops fit; you choose the shape of the day.
- `why`: one line of what the row rests on (the trade, its hours, its staff).

`names`: one pool for the district: at least the minimum of given and family names in the rules, from several origins with none over the rules' share, in the rules' name form, none on the avoid list, no date or era in any name. Names are people's, not a period's (GDD §15).

Fit the Handover's ground: a server on shift at the café at 17:00, the official driver at the bay, a guard at the Office door, and a councillor in the Office must all exist in your rows with positions and weights that give each at least one person. You do not name them and you do not edit the mission.

State in `notes` (at most 8 short lines) the choices that are yours (starting values, [S]) and any question for Motive. Cite in `sources` the passage ids you used. Hand-offs do not exist in this crew.
