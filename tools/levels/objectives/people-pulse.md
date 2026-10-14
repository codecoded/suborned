# Objective · cast tables (Pulse, brief 010)
Propose the cast tables for the scope in your task: who works each unit and lives near it, as a mix. You write rows, not people. Code (tools/people) turns each row into individuals, seeded by plot and unit id, and then the checker holds both the rows and the people to `people.rules.json`. Anything the rules do not list is rejected; do not invent temperaments, needs, tie kinds or positions.

One row per area × occupation in the scope (the task lists them; add none, drop none). Each row says:
- `positions`: the jobs in the unit and their weights. A trade's positions are its category's in the rules (the keeper is made by code, one per unit; do not weight it). A civic employer's are the roles the rules give it (clerk, guard, official-driver, councillor). Residents are `resident`. Each position has a `post`: `door` (stands at the unit's door), `inside` (off-screen), `bay` (at the nearest bay: drivers).
- `temperaments` and `needs`: weights over the rules' lists. A person only draws a temperament their position allows and a need their temperament can be approached on (the rules' `respondsTo` against a need's `metBy`), and a need whose requirement they do not meet (home, work, family tie, rival tie) is not drawn. Spread them so no job reads as one kind of person (GDD §09 risk: sameness), but fit them to the work: a guard is Loyal or Mercenary, a driver on cash.
- `ties`: patterns, kind and the share of the unit's people who carry one. Workmates for a shift, regulars where the routine passes a unit, family at home, debtors, the odd rival.
- `routines`: variants over the life.json skeletons, fitted to its bands and to the unit's hours: how long before the shift they leave, whether they live on the sheet and walk or come by the tram, a meal on site or in the square, a stop at the café before the shift (the Office guard's, GDD §05), an hour in the square after. Code times every walk on the walk graph and makes the stops fit; you choose the shape of the day.
- `why`: one line of what the row rests on (the trade, its hours, its staff).

`names`: one pool for the district: at least the minimum of given and family names in the rules, from several origins with none over the rules' share, in the rules' name form, none on the avoid list, no date or era in any name. Names are people's, not a period's (GDD §15).

What the Handover needs of the cast (Motive §3; read them from the mission's roles in the task, name no person):
- the row of a role with a `need` weights that need: the café server row weights `debt`, the official-driver row `cash`. Use `positions` on a need entry to weight it for that position only (a server, not the cook), and keep a temperament the role's `approach` does not refuse (the server and the driver are turned with Help, once);
- the row of a role with `regularOf` carries a routine variant with `preShift: cafe` for that position (use `positions` on the variant: the guard's day starts at the café, the clerk's does not) and a `regular` tie share, so a share of its people hold a regular tie into the café;
- no temperament over 60% of a row's weight, and every unit of 3 or more staff carries at least 2 temperaments (rules `temperamentsBy.mix`, checker P-10);
- the café keeper knows the needs of workmates and regulars; code derives it from the ties (rules `knowledge`, P-11), so give the café `workmate` and `regular` ties and let its guards and clerks pass through it;
- every field a person carries is read by a rule (P-12): add no field, no bio, no value or dream; `why` stays about the place, at most 300 characters.

Fit the Handover's ground: a server on shift at the café at 17:00, the official driver at the bay, a guard at the Office door, and a councillor in the Office must all exist in your rows with positions and weights that give each at least one person. You do not name them and you do not edit the mission.

State in `notes` (at most 8 short lines) the choices that are yours (starting values, [S]) and any question for Motive. Cite in `sources` the passage ids you used. Hand-offs do not exist in this crew.
