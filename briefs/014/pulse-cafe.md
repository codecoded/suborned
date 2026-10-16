# 014 · Pulse · the café street (Sky: "clustering at the café entrance, no idea who the waitress is")

Pulse, 2026-10-16. No model call, no spend. The promoted cast (r016) is unchanged byte for byte; only where people stand and when changed, by generator rule. Nothing promoted, `sim/data/` and `out/people/` are not touched (the candidate is beside this file).

## Cause
Five servers of the 12:00–18:00 shift had `post: door` and every `door` person stood the unit's one door point all shift (the café's whole mission window: 5 drawn people on `cafe-entrance`, 5 within 3 m of the Scout's `beside-inez`). The rest of the street was the guards, the driver and 2 café chairs (12 drawn people before the chime).

## What changed (all by rule, seeded, same bytes twice)
1. **A door is one person's post** (`tools/people/day.ts insideFromDoor`, called at the end of `expand`): of a frontage unit's door staff on one shift, one stands the door, the rest work inside (`post: inside`, same place, not drawn). The one who stands it is the one the street knows: a lover at another employer outweighs regulars, then the lowest id. On the 12:00 shift that is Inez (p-cafe-14, the guard's lover); 06:00 p-cafe-05; 18:00 p-cafe-20. Ties, needs, names and ids are read, never redrawn.
2. **Customers come and go all day** (`day.ts visitsOf`, `residentCycle`): each resident makes 4 to 7 café visits a day from their own stream `<id>/visits`; the hour is drawn by the food draw of the band it falls in (life.json: peak 0.9, evening 1, night 0.1) inside the café's hours, 60% sit for the food dwell (40 min ±25%, a chair, nearest table first), 40% take a coffee to go (6 min at the counter window: a standing point nearest the door). Kept clear of their errand and evening by both walks, so they walk 50 m from home and back (arrivals and departures are visible walking).
3. **Distinct slots** (generator + checker, no engine rule): the café's chairs and standing points were already one holder at a time (P-13); the counter window and the tram-stop queue (24 standing points round `t1-records`, residents' errand) now take standing points the same way, and **new P-16** fails any second drawn person on one place at once (doors, bays, stops). It covers every non-civic unit and unowned places; a civic employer's door and bay are still its row's (`count`, `posts`) and M-2's `alone` and `waitsFor`, because the stand-in fixture stacks them and the real cast does not.
4. Files: `tools/people/{day,expand,scope,seats,check}.ts`; tests `tools/people/test/street.test.ts` (new), `day.test.ts`, `seats.test.ts`, `tools/levels/people/test/people.test.ts` (17 checks), `tools/people-view/view.test.ts` (17 checks); cause declared in `tools/levels/causes.json` and the people ledger (`bun run causes people`). Cast schema, objective and card untouched, so r012 to r016 still replay.

## Before and after, drawn people in the window 0:00 to 8:30 (sampled every 10 s)
| place group | before: people / most at once / mean | after: people / most at once / mean |
|---|---|---|
| café door (`cafe-entrance`) | 5 / 5 / 5.0 | **1 / 1 / 1.0 (Inez)** |
| café terrace chairs | 14 / 12 / 1.0 | 22 / 16 / 6.3 |
| café counter window | 0 | 1 / 1 / 0.7 |
| Office public door, official door, official bay | 2, 2, 2 / 1, 1, 2 | unchanged |
| tram stop, sky rail kiosk (off the play area) | 1, 1 | unchanged |
| walking between places (whole sheet) | 72 / 70 / 20.8 | 82 / 71 / 22.5 |
| drawn on the street 0:00 to 5:20 | 12 (flat) | **16 to 17**, customers turning over |
| drawn on the street 5:30 to 8:30 (the chime on) | 42 to 81, mean 66.6 | 44 to 83, mean 68.6 |

Customers (people on a terrace chair or the counter window), per 30 s from 0:00: **6 10 10 10 10 9 9 5 5 5 5 6 6 5 5 5 5** (before: 2 2 2 2 2 1 1 0 0 0 …). 5 arrive and 5 leave in 0:00 to 5:30. Who sits where, before and after, at 0:30, 3:10 and 5:00: `briefs/014/pulse/cafe-frontage-before-after.png` (plan view, block frame, straight lines between stops, ring = Inez).

## Inez, and what Waymark re-checks
- Her place does not move: `cafe-entrance` (-10.3, -29), the mission's `Café door` role point (near 4), standing it alone for the whole window; `beside-inez` (-8.2, -29) is 2.1 m from her and now **only she** is within 3 m of it (before: 5). No mission.json change. M-2 casts the same four people (waitress p-cafe-14, driver p-records-office-31, guard -01, councillor -07).
- **Customers on the terrace when the guard visits her:** call at the earliest 1:52, he is at her door about **2:40 to 3:30 with 9 customers** (tables 1 to 3, the two nearest the door 5 to 6 m from her), and **5 from 3:30 to 5:30**. Anyone on the street within 13 m of the café door in 1:52 to 4:30: 6 to 11 (before 5 to 7). All are ordinary witnesses.
- Newly in the way, for the sight and Trail checks: walkers pass the Tech's cabinet (23.9, -17) within 13 m at **3:30 to 4:00** (p-pylon-row-east-03), **4:20 to 4:50** (-04, p-garden-row-east-07) and 7:30 to 8:00 (p-garden-row-east-06); none before. Table 0 (north, 8.8 m from the door) sits 1 to 4 m from `ambush-2` and `police-1`, the posts on the café's north wall arc: a customer is there to about 3:00 and none from 3:30 to 8:30 in this data (5 to 6 customers then, at tables 2 and 3 on the south side), but any other cast would put one there, so the fights should be checked with a customer on table 0. Office doors, the bay and the getaway are as before (same people within 13 m).
- Routes G, A, B and the loud fight are written against the old crowd; this is the whole of what moved. Waymark to re-run the checks on the candidate: `sim/data/handover.people.json` from `briefs/014/pulse/handover.people.after.json` (made by `bun briefs/014/pulse/mksim.ts out/pulse/after.people.json <out>`), then `bun run people && bun tools/sim/site.ts` swaps the live data once Sky says yes (the sim tests hold the file to `out/people/`, which is why I did not swap it).

## How many people out, for the street to feel lived in
The 130 simulated people cannot make a flow: 24 residents, 72 Office staff, 24 café staff, 6 kiosk. With the rules above the street is **16 to 17 drawn before the chime** (about 7 customers, 1 Inez, 2 guards, the driver, and walkers) and **44 to 83 at the chime**. For the first five minutes to read as a city I would put **about 35 to 40 drawn people** in view (customers 7 to 10 at most, 3 to 6 passers on each of two or three approach lines, never standing at a door), which is the 0.04 to 0.07 people per m² the square's promenade asks of the peak band. The missing 20 are the background tier (anonymous walkers from `tools/life`: `residents.tripsPerHour`, the T1 alighting 6 a service, the shift waves), which the engine does not read yet, so the gain left is Keel and Latch, not more named residents (each is a person with a need and ties; raising `HOME_SAMPLE` from 8 changes the tie graph, so I did not).

## Open issues
- Whole-minute stops make a short walk take a minute or two, so a walker between 50 m homes and the café moves at normal pace, but any stop that waits at the start of a gap would still crawl; the Office clerks at the chime walk the whole gap (the 56-minute walk of the district plan). Pacing a leg at 1.4 m/s from the end of its gap is a Keel/Splice change to `tools/sim/people.ts`; not done, it would move every crowd position.
- The objective and the cast schema do not mention the door rule (editing them breaks the replay of r012 to r016). A future run's model can still write `post: door` for every server; the generator makes it one.
- The tram stop queue and the P-16 exemption for civic doors are noted above.

## Evidence
- `bun run people` in the tree: P-0 to P-16 ok, M-2 ok (waitress p-cafe-14 …). `bun run verify`: VERIFY OK, 18 runs replayed, r016 REPLAY OK (drift: rules added P-16). `bun run typecheck` clean. `bun test tools/people tools/people-view tools/people-graph tools/levels tools/sim tools/missions tools/dialogue contracts`: 584 pass, 1 skip, 0 fail.
- Counts: `python3 briefs/014/pulse/report.py sim/data/handover.people.json briefs/014/pulse/handover.people.after.json`; picture: `python3 briefs/014/pulse/render.py <out.png>` (reads `out/pulse/geom.json`, made by `bun briefs/014/pulse/geom.ts`).

Next owner: Sky (accept or not), then Waymark (re-check the routes on the candidate data), then Splice/Keel (swap `sim/data`, `bun run sim:site`); background walkers: Keel and Latch.
