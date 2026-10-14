# 010 · Pulse · the civic-block days, the Handover routine fixes, and the whole-district people plan

Pulse, 2026-10-15. Read-only on `tools/people/` and `contracts/people/` (Splice is editing them); no model call. The only files written outside `out/` are this plan and `briefs/010/pulse/`. Scratch and the prototype are in `out/pulse/`; what must ship is copied to `briefs/010/pulse/`. Inputs: `briefs/010-people.md`, `briefs/005-city-life.md`, `briefs/010/010-cast-r013.html`, Suture's QA, Redline's review (High H2, M6), `out/people/harbour-district.people.json`, the working tree of `tools/people` as of 02:24, Waymark's M-2 as committed (52f2a2d).

## 0. The Handover routine fixes (first; Aster's four asks)

**Result:** with the corrected table and the expander support below, M-2 with its playability conditions passes at the ruled start 17:24:30 and P-0…P-13 are clean (130 people). On today's vocabulary only the waitress casts.

| ask | what the people now do | M-2 line |
|---|---|---|
| 1 guard posts | 4 guards on the day shift: 1 at the public door, 1 at the service door, 2 on rounds inside (counted at the official side, not drawn). A 24 h watch of 2 (17:30 to 01:30, 01:30 to 09:30). At 17:30 the day door guard leaves for the café table (17:32); the watch's door guard comes from the café (pre-shift 17:13 to 17:28) and takes the door at 17:30. One guard visible at the public door every minute 17:20 to 17:35 (r013: 11). | guard: 1 who fits, alone, regular tie to a server on the afternoon shift |
| 2 Vale | One councillor. Arrives 09:28 by official car at the bay, works inside at the official entrance, at 17:30 is in the car at the bay (17:30 to 17:32), then off the sheet. No café table, no tram. The 5:50/6:10 seconds sit inside the minute-17:30 stop (routines are whole minutes): seconds are the sim's, the mission's events anchor them. | councillor: reaches Meet driver by 6:10 |
| 3 driver | One official driver, cash, workmate tie to the councillor. At the bay 09:30 to 17:30, in the car from 17:30 to 17:32 (continuous at the bay, so still in place at 5:40). | driver: workmate tie to councillor, waits for councillor |
| 4 guard regular | The door guard holds two regular ties to servers: the morning's (his pre-shift stop at 09:15) and the afternoon's (his stop after the shift, 17:32; staff on shift at arrival are preferred). The day guard is p-cafe-15's regular; the incoming watch guard is Inez's (p-cafe-11). | guard: regular tie to waitress |

```
bun briefs/010/pulse/m2.ts briefs/010/pulse/handover-fix.v2.people.json
ok   M-2  waitress p-cafe-11, driver p-records-office-27, guard p-records-office-07, councillor p-records-office-49
bun run people --cast briefs/010/pulse/handover-fix.v1.cast.json   (today's vocabulary, best effort: one guard, one driver, one councillor)
FAIL M-2  (abridged) driver: no workmate tie to the councillor · guard: no regular tie to a server on the afternoon shift · councillor: next stop cafe-stand-10@17:32, not the bay   (waitress and `alone` pass)
```

Files (`briefs/010/pulse/`): `handover-fix.v2.cast.json` (corrected tables; every row but records/records-office is r013's), `handover-fix.v2.people.json` and `.review.html` (expansion and review page on the prototype), `handover-fix.v1.cast.json` (the same fix in today's vocabulary, which cannot pass), `handover-support.patch` (the expander support, 106 changed lines in 6 files, against the working tree of 02:24), `make-fixture.ts`, `m2.ts` (runs Waymark's `castRoles` with `play: true` on any people file, the same call the overlay makes), `handover-day.py` (the days of the five people), `people-pulse.handover-addendum.md` (the paragraph for Pulse's objective; not applied, the plug-in hash and the schema are Splice's right now).

**What the expander must support** (all optional additions; absent means the old behaviour; the prototype in `out/pulse/proto/` has each):
1. **Exact counts and post spread.** `Position.count` (exactly n per shift; the rest by weight), `Position.posts` (the first n take that post: door 1, official 1, the rest inside), `Position.side` (which entrance an inside person is counted at). Needed because M-2's `alone` reads places, and inside people were counted at the public door. New post `official` (the service door).
2. **The official car.** `commute: 'car'` (lives beyond, driven to and from the unit's bay), `Unit.official` (the unit's second entrance). A hop of 6 m or less costs no minute (`STEP_OFF`): the door and the bay beside it were 2 minutes apart, which made 6:10 impossible. P-5 treats a `commute` stop at a bay as sitting in a vehicle.
3. **A civic watch.** `Position.watch.count` and `Unit.watch` (8 h shifts that tile the hours the office is shut, from the life.json employer). P-4 expects office staff plus watch. A watch's pre-shift café stop is skipped when the café is shut.
4. **Regular ties that reach the afternoon.** The second regular tie aims at a stop the first does not cover; staff on shift when the customer arrives are preferred; an `evening` stop at a café seat counts as a visit (P-7 reads the same rule). Today only a pre-shift stop counts, so a day guard can only be a regular of the morning staff.
5. **Aimed ties.** `Tie.positions` (who carries it), `Tie.with` (the position it is made with), `Tie.count`; aimed patterns are made first so a person they name is not full.
6. **Schema:** `commute` gains `car`, `Work.post` gains `official`.

Not in the fix and still open: stop times are whole minutes, so Waymark's "30 s walk" and the 5:50/6:10 offsets live in the sim, not the file; and an incoming guard who arrives late (a gap at the door after the chime) is one more field (`lateMin`), left out because the chime is already the busiest second.

**Corrected live run, `--preview` cost (no model call; ingest was local, 1:18; preview wrote `out/preview/`):** `bun run levels people --preview --scope civic-block`: round 1 about **$0.45** (builder $0.26, critic $0.18), each further round about $0.40, three rounds at most $1.35 (estimator ±40%; for the same shape it gave $0.85 against r013's actual $0.79). The objective grows about 2 KB and the schema about 1 KB (a few cents). Ask Sky for a **$2.00 ceiling**.

## 1. The civic-block days, as the routine owner

Source: `out/people/harbour-district.people.json` (r013, 126 people), recounted with scratch scripts. "Slack" below is `leaveBeforeMin`.

### 1.1 Plausible against life.json's bands and the r008 timetable?

| what | finding | numbers |
|---|---|---|
| Commutes | **Slack is spent as walking.** The expander puts the slack into the gap between two stops, which is drawn as walking, and P-1 checks only that a gap is long enough, never that it is not far too long. A 188 m walk from the T1 stop to the Office takes **56 min on average** (up to 80) for tram commuters, 27 min for walkers, 38 for guards. | 386 legs; 98 are over 10 min longer than the walk; 62 person-hours a day of "walking" that is waiting; 97 of 126 people have a leg over 15 min; implied speed 0.06 m/s. I wrote leaveBeforeMin up to 80 as a trip length; the field reads as a walk. |
| Trams | Commuters appear at the stop at any minute. The r008 timetable has T1 reaching Records at 54 s past each 180 s (load 6 per service), and rides are 39 to 378 s, so a tram commute is a hop with a visible arrival every 3 minutes. 40 of 126 people are "beyond" at night. | the timetable's service times are never read by the expander |
| Shifts | Office: all 72 start 09:30 and end 17:30 to the minute; arrivals are smeared only by the slack bug (62 walking at 09:10), departures are a spike: 3 walking at 17:20, **76 at 17:30**, 6 at 17:35. As a chime that is right (and now lands at the mission's 5:30); as a habit it is one shape. Café 6 to 12, 12 to 18, 18 to 24 with no overlap. | |
| Meals | 69 meals are taken standing at the post (a guard at the door, a driver at the bay), 33 at a seat. Lunch is the shift midpoint plus or minus 20, so 13:10 to 13:50 for all 72. Café staff eat at 08:44, 15:20, 21:00. | |
| Evenings | A stop called `evening` at 12:02 (the keeper of the 6-to-12 shift, at her own café). Only 8 people are out at 19:30 and none after 01:00. life.json calls records evening "steady" (0.23) and late "quiet". | |
| Residents | Their only errand is 15 min at the tram stop, 24 of 24; the sampled residents are at home 97% of the day. | 1395 of 1440 min at home on average |
| Nights | Nobody is out 01:00 to 05:00; the Office door is empty from 17:30 to 09:30 (fixed in the Handover fix by the watch). | |

By hour, people outside or walking (126 simulated): 05:30 6 · 08:30 24 · 10:30 28 · 12:30 27 · 13:30 41 · 15:30 25 · **17:30 83** · 18:30 11 · 19:30 15 · 20:30 7 · 02:30 0. Records is "lively" (0.57 to 0.64) from 08:00 to 19:00, which at the §15 density of 0.12 people/m2 is hundreds of people in the square, not 25. The simulated cast cannot be the street (section 3).

### 1.2 Where it feels samey

- **27 distinct day shapes for 126 people**; the Office's 72 have 9, and 24 of them have the identical day. Clerks are all `inside`, so the 66 largest group is invisible.
- Every café server is in debt (14 of 14) and 6 of 7 drivers need cash: right for the Handover's roles, but once a player knows it, Listen on a server tells nothing (§01 pillar test). Needs are identical for clerk, guard and councillor.
- Family ties: 60 ties, 2 share a family name. Surnames repeat inside a unit with no tie (Ferrer 4, Barlow 3, Bermudez 3, Novak 3 in the Office, Defalco 3 in the 24-person café): reads as family, is not.
- Temperament caps hold on the table and fail on the people for small rows (kiosks 67%): Suture's point; Motive rules.

### 1.3 What Latch's snapshots exposed

1. **Everyone indoors:** at 13:30, of 126: 41 inside at work, 40 at home, 4 beyond, 41 outside. At 02:30, 0. Door posts (café 14, Office 11, kiosks 4) are the only statics; they stand at the door for 6 to 8 hours.
2. **Straight-line walking:** the snapshot draws a straight line across the fountain because the walk graph is not exported, and the line is walked over 56 minutes (above).
3. **Nobody walking at 13:00:** 0 people in a gap at 13:00 and at 12:45, 3 at 13:15. Lunch is a stop, not a trip.

### 1.4 What puts people on the street at play zoom (ranked; targets are [S])

1. **Slack becomes a stop, not a walk.** `wait` at the tram stop, the door or a café seat; gaps equal the walk (P-1 gets an upper bound: gap at most the walk plus 2 min unless the stop is `wait`). Visible waiting at the stop and café, and honest speeds on any path.
2. **Tram arrivals on the timetable.** A commuter alights on a service: `T1/records` at 54 s past each 180 s, 6 per service in the morning band (120 an hour); the stop shows a bunch every 3 minutes. Needs a `ride` stop naming the service.
3. **Waves from the census, not from habit.** Arrival and departure windows per unit (Office 09:05 to 09:30, leave 17:30 plus 0 to 6 min), so the chime is a 6-minute stream.
4. **Lunch outside, staggered.** Desk staff lunch 11:45 to 13:45 at the café's seats (24 chairs plus 24 standing points hold 48 at once, so the café's terrace is the limit; the square needs more seating before 40% is possible): 40% of the 66 clerks would be about 26 outside at 13:00, if the seats exist.
5. **Breaks.** A tenth of staff step out for 3 minutes at 10:30 and 15:00 (smokers at the door, a clerk to the kiosk).
6. **Residents' errands to a frontage** (nearest retail, service or kiosk by band, from `residents.tripsPerHour`), not the tram stop; an evening at the café.
7. **Regulars at tables:** the guards' morning coffee and the Office staff who call at the café (r013: 11 regular ties from the Office into it).
8. **Door posts only where staff stand outside** (kiosk, café server, nightlife door, hotel porter), one or two per door; the rest are inside and step out for 5 and 6.
9. **The background crowd** (section 3): shoppers, revellers, dockers, commuters from beyond. 1 to 7 are 20 to 100 visible simulated people in the block; 9 is the other few hundred.

Targets for the next civic-block run: at least 60 visible simulated people at 13:00, at least 40 walking 17:25 to 17:35, no gap over the walk plus 2 min, at least 60 distinct day shapes of 130.

**Top 3 routine changes:** (1) slack as a `wait` stop with an upper bound in P-1; (2) post slots, a civic watch and the official car (the Handover fix, section 0, generalised to every unit); (3) street life: staggered lunch and breaks, errands to frontages, tram alighting on the r008 service.

## 2. The whole-district run, scopes

Numbers are from the placed world (`out/world`, 1,077 frontages), the plan and life.json; the world export is evidence only: the scope builder must call the frontage generator (`tools/world/gen/frontage.ts`), because out/ is never a source.

| | people | count |
|---|---|---|
| frontage units | 35 trades, 1,077 units (882 street, 141 deck, 54 concourse) | |
| workers, all shifts | by staffPerUnit and shifts: 10,468, plus civic and kiosks (14 x 3) | **about 10.6k** |
| residents | 18 home plots | 30,410 (144 sampled at 8 a plot) |
| on foot at the 17:00 peak | `tools/life/density.ts`, 25 streets | about 10.5k |
| cast rows (area x occupation) | frontage 79, kiosks 11, residents 6, civic 1 | **95**; 92 new after the civic block's three inherited |

**Two partitions, because the model and the simulation have different limits:**

- **Model scope = area (7).** The cast tables are written per area x occupation (§09). One levels run per area keeps a prompt at 10 to 18 rows (the schema allows 24) and a failed run costs one area. Order: records (inherits its civic-block rows; the café and Office rows are keyed to their unit, not the area's generic diner), then lamplight, harbour, lantern, crescent, gardens, docks.

  | area | units | workers | new rows | 2 rounds | 3 rounds |
  |---|---|---|---|---|---|
  | records | 126 | 1,033 | 10 | $1.33 | $2.00 |
  | lamplight | 203 | 1,614 | 13 | $1.60 | $2.39 |
  | crescent | 176 | 1,766 | 18 | $2.03 | $3.05 |
  | harbour | 230 | 2,307 | 14 | $1.68 | $2.53 |
  | lantern | 102 | 1,224 | 14 | $1.68 | $2.53 |
  | docks | 160 | 1,830 | 13 | $1.60 | $2.39 |
  | gardens | 80 | 694 | 10 | $1.33 | $2.00 |
  | **7 areas** | 1,077 | 10,468 | 92 | **$11.3** | **$16.9** |

  Cost model: builder round = $0.07 + $0.022 per row, critic round = $0.13 + $0.016 per row (fitted to r013's four calls: $0.14, $0.20, $0.21, $0.23 at 5 rows), times 1.15 for the larger objective and schema. `--preview` exists only for `civic-block` (above); area scopes do not exist in code yet, so these are not previews. Add one names-only call for the district pool (about $0.3) and the corrected civic-block run ($0.85, at most $1.35). **Expected total about $15 (range $12 to $19). Ask for a $20 ceiling and $3.50 per area**, and run one area at a time; r012 shows a run can be rejected after 3 rounds ($1.03).
- **Simulation scope = block (30).** A block is a plot or megastructure with its frontage, the nearest stops, and the home plots within the transit reach: the civic block plus 25 plots and 4 megastructures. Missions are one block (§01), the walk grid is local, and the simulated set is chosen per block (section 3).

  | block (largest first) | units | workers | on shift 17:00 | simulated by the rule at 17:00 |
  |---|---|---|---|---|
  | market-deck (docks, deck) | 89 | 936 | 435 | 317 |
  | harbour-boulevard | 84 | 908 | 341 | 285 |
  | parade-galleries (crescent, deck) | 83 | 844 | 386 | 280 |
  | crown-terraces (gardens, deck) | 77 | 616 | 264 | 259 |
  | harbour-court | 67 | 754 | 290 | 257 |
  | docks-boulevard | 57 | 735 | 288 | 215 |
  | the other 23 | 1 to 56 each | | | 2 to 146 each |

  All 29 frontage blocks fit the 600 budget on their own at 17:00 (the largest, 317); two large neighbours (for example harbour-boulevard and harbour-court, 542 plus residents) just fit, which is why the rule has a cut order.

## 3. How the simulated set is chosen, and what everyone else gets

This answers Redline H2 and Aster's question. The problem as measured: every staff slot of every unit becomes a simulated person; at district scale that is 10,686 people against a budget of 600, the expander takes 23 to 39 s (ties are quadratic), and P-8 fails with 3,772 duplicate names because r013's pool holds 6,912 full names. No rule noticed 4,926 people either.

**The budget (brief 005 A, Keel):** at most 600 simulated and 2,000 background in play. **Simulated = the mission's block plus whoever the rules pull in near an operative; never the camera.** Add the numbers to life.json `generator.tiers` (`maxSimulated: 600`, `maxBackground: 2000`; Keel and Motive set them, Aster quoted 600) so a rule can read them.

**Three layers:**

1. **Census (the whole district, code only, no model).** Per unit and shift: how many people, by position, temperament and need, as integer quotas of the cast row (largest remainder, no names). About 2,800 unit-shifts (2,714 for the frontage units, 42 kiosk shifts, the Office) and a few hundred KB. It is what the city is made of: it checks the rows over all 10.6k workers (P-4 staff counts, P-10 row caps) without making a person, it gives investigators and standing their population, and it feeds the background. The census is the check that covers the district.
2. **Simulated people (a block, at a mission window, at most 600).** Chosen by this rule, in order, each class cut by a stable hash of the person id if the budget binds; classes 1 and 2 are never cut (the run fails if they alone exceed it):
   1. people the campaign has turned, promoted, arrested or been introduced to (the ledger) whose day puts them in the block in the window, or who are tied to someone chosen;
   2. the mission's pinned roles and their ties one hop out (workmate, regular, family: the people an introduction can reach);
   3. the keeper of each unit in the block (§08: the keeper tips needs);
   4. posted staff (door, official, bay) on shift from 15 min before to 15 min after the window;
   5. every on-shift person of a unit with 6 or fewer on shift;
   6. a sample of 4 inside staff of each larger unit (department store 65, bonded warehouse 60, cinema 44...);
   7. residents, 8 per home plot within reach (HOME_SAMPLE, as now);
   8. people with a regular tie into a unit of the block who pass it in the window.
   People off shift and outside the window are not made: they are census. **Investigators question simulated people only (Q5)**, so the set is everyone who can witness anywhere in the block, which is why it is the block and not the cell's route (the plan is made in play, after the set is built).
3. **Background (anonymous, routine only, rules not persons).** Everyone else is the census minus the simulated: their shift-change waves (the Office's 66 who are not simulated leave at 17:30 as a stream), residents' trips from `residents.tripsPerHour`, shoppers, revellers and dockers from `pedestrians` (spill, walkers, feeders, promenade), commuters alighting on the r008 services, and the surges in life.json `events`. They are spawners seeded by plan id and clock: {kind, from, to, times, count, seed}; they carry no name, need or tie (people.rules.json `tiers.background`), cannot be questioned, and add T, Alert and standing when they witness (§09). Per block and window the concurrent count is checked at most 2,000 (rule P-15). District-wide `density.ts` gives about 10.5k on foot at the 17:00 peak (25 streets; a top-up is needed only on circus-road and skyport-pier); a block's reach takes a part of that, which the L-rules size per block. Beyond the block's reach nobody is made: the rest of the district is counts and density.

**Identity across missions.** A person is a pure function of `(unit, shift, slot)`, so any person the campaign meets can be made again, the same, at any time (bravery, resentment, memory and promotion live in a sparse ledger keyed by that id). Two things must change for that to hold: ids must not be positional (Redline L1: today a change to a row renumbers a unit) and every draw must depend on the person's own id, not on who else is in the file (ties pick from a pool today, so a block expanded alone would differ from the same block in a district). Ties become hashed picks over a unit-level candidate set (workmates: the unit; regulars: staff of units the person's stops pass, ranked by hash of both ids; family: a household id from the home plot).

**Names.** At most 600 people are in a file, so uniqueness is cheap per file. For persons met across missions, a name is a function of the id over the district pool, and the pool must outnumber the named roster: the schema's 90 x 110 = 9,900 full names is less than the 10.6k workers. One names-only model call writes a district pool (for example 120 given and 180 family, 21,600 full names), promoted as `contracts/people/names.pool.json`; area casts then carry no names. Motive's note that 60 x 80 covers a district is wrong by a factor of two. Pinned parts (Inez, Vale) are removed from the pool and never drawn for anyone else (Redline ML2).

**Cost at the new scale:** ties at 600 people are about 0.1 s (the 23 to 39 s at 10.7k is quadratic); 30 blocks expand and check in under 10 s together. The walk grid of a block is its bounding box plus 60 m, tens of thousands of cells, so the 1.9 GB of Redline M6 does not arise.

## 4. What has to change first

In order. Owner is who builds it; I specify the routine side of each.

1. **The Handover support** (section 0, items 1 to 6) and the corrected civic-block run. Splice ports the patch; Waymark's M-2 already reads the conditions.
2. **Routine vocabulary v2, before any area run**, or seven areas encode the slack-as-walking pattern and the one-shape day: a `wait` stop; `ride` on a named service; arrival and departure windows; breaks; lunch outside; errands to a frontage class; P-1 upper bound (gap at most the walk plus 2 min unless `wait`). Pulse specifies, Splice builds the stops and checks, Latch reads `wait` and `ride`. The civic block is re-run once with it.
3. **Tier rule and census** (section 3): scopes `area:<id>` (census, no walking, for the model run and its P-0, P-3, P-4, P-10), `block:<plot|megastructure>` (people), `district`; P-14 simulated at most `maxSimulated`; P-15 background concurrent at most `maxBackground`; stable ids; scope-independent draws; unit-keyed rows (the café and the Office are rows of one unit, the area's generic diner is another); kiosk rows keyed by the stop's nearest plot (S1/gardens is lamplight, the civic scope calls it records). Splice builds; Keel and Motive set the numbers. The levels run's checker gets one materialised block per area, at the peak window, so P-1, P-5 to P-7 and P-13 still bite inside the loop.
4. **Walk graph.** In order of value: (a) doors carry an explicit standing point exported by the world generator: at the entrance centre only 78 of 1,077 door points are walkable and 703 of 882 street doors reach the café; at a 2 m kerb offset 800 of 882 do; **82 never do** (lamplight-lane 16, starline-street 18, crescent-lane 12, harbour-lane 14, dock-road 9, lantern-lane 12, circus-road 1): no offset to 8 m lands on walkable ground, so the plot blockers close the lane or the door faces a court (Datum and Splice); (b) a level-aware graph: the 195 deck and concourse doors reach only through the 113 connectors, and without it P-1 passes them for the wrong reason (at a 6 m offset all 54 concourse doors "reach" the café, presumably by walking on the street beneath); Keel's design, 2 m grid per level; (c) sources are hubs only (homes, stops, seats, café; a door is never a source), `Float32Array`, stop at the longest walk a routine allows. My probes: grid build 37 ms, Dijkstra 24 ms a source (Redline 45 to 69 ms), 1.6 MB a source at 202k cells. Megastructure blocks wait for (b); the 25 plot blocks do not.
5. **Names pool** (section 3) and `P-8` rejecting pinned parts in the pool.
6. **Background tier** (`tools/life`, Pulse): flows by street and band from the existing terms, shift waves from the census, tram alighting from `transit.services`, surges from `events`; output `out/life/<block>.<window>.flows.json`, expanded by the sim. Checker L-rules: concurrent count at most 2,000; density at most the §15 target on busy streets; every spawner reaches its end.
7. **Vehicles** (`tools/life`, Pulse). What exists: `traffic` (lanes by class and band), `deliveries` (visits per unit per day), `transit.services`. To build: (a) deliveries expanded per unit by unit id (about 1.2k visits a day for 1,077 units: bay, time, type, dwell, blocks-lane), (b) traffic spawned at the 14 gates by class and band, (c) official cars as unit vehicles (the councillor's car stands at the bay all day and leaves at the chime: its id comes from the `car` commute), (d) patrol and response units (§05), (e) `ride` stops tied to services. Checker V-rules: one vehicle per bay at a time and the dwell ends before opening (V-1), lane flow within capacity (V-2), boarding price equals the plan timetable's wait plus ride (V-3), every official car has a driver and a passenger on shift (V-4). Needs bays as places with a capacity and a vehicle kind in the schema.

Run order after 1 to 6: names call; records area; the other six; then each block materialised at its peak window and accepted. 7 follows (it is not on the critical path for cast tables).

## 5. Ownership of `tools/people/`

**I take now (routine semantics; Splice keeps the files compiling until the split):**
- the day-building code of `expand.ts`: `workerCycle`, `residentCycle`, `fold`, `quota`, the post and slot assignment, watch and car, and the new wait, ride, break and errand stops. Proposal: move them to `tools/people/day.ts` so `expand.ts` keeps people assembly (ties, needs, names) and each file has one owner;
- the routine vocabulary in `contracts/people/cast.schema.ts` (`Routine`, `Position`, the tie patterns) and the stand-in cast `standin.ts` (it is a vocabulary fixture);
- `scope.ts` for time: `shiftsOf`, `OFFICE_HOURS` (read from life.json, Redline L4), civic watch, unit hours;
- the routine checks as rules: P-1 (reach and time, with the upper bound), P-5 (keep-clear), and the new P-14 and P-15 (budget); Splice writes them into the checker framework from my text;
- Pulse's objective `tools/levels/objectives/people-pulse.md` and `tools/levels/people/view.ts` (what the model is shown of the day);
- `tools/life/` (life.json, the background flows, vehicles), already mine; and the day section and street table of the review page.

**Stays Splice's:**
- the pipeline engine: `tools/gen`, `tools/rag`, the `tools/levels/people` plug-in (plugin, drift, hash, replay, pins, schemas), run recording and replay;
- the checker framework: the `Check` rows, `RULES_SRC`, `FIELD_READER`, and the data-rule checks P-0, P-3, P-8, P-9, P-11, P-12 (Redline M1: P-9 must re-expand);
- `inputs.ts`, `promoted.ts` (promotion and its drift check), `roles.ts`, `handover.ts`, `knowledge.ts`, `seats.ts` (café chairs and standing points), and the walk engine in `walk.ts` (I specify the hub-sourced, Float32, level-aware behaviour; Keel designs the level graph).
- ties: Splice keeps the mechanics and P-7; I own which stop a regular tie aims at and the `positions`, `with`, `count` vocabulary. Names and knowledge are Motive's rules.

## 6. Open questions for Aster

- **Sky:** the corrected civic-block run ($2.00 ceiling) and, later, the district run ($20 ceiling, $3.50 an area).
- **Motive/Keel:** the budget numbers in life.json `tiers` (600 and 2,000 are quoted, not set); whether the 60% cap applies to the table or the people (Suture); whether the "1 to 8 ties, nobody a hub" count includes incoming ties (a kiosk keeper holds 15, Redline L6); whether Idealist guards at the service door matter (no change made).
- **Waymark:** a role can name `official` posts only through the cast; M-2's `alone` is by place, so inside people must not share the public door's place (done by `side`); a pin id (`pin`) in mission.json would survive only if ids stop being positional.
- **Datum:** the 82 lane doors with no walkable standing point.
- **GDD:** §09 lists "staff counts" as a people check; under the census that check moves from persons to counts. No GDD edit made.
