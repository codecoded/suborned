# 071 · Motive: the split getaway, rule tables (Option A)

**Status:** rule tables for Aster (GDD text) and Keel (rulebook and engine). Nothing is built and no GDD, rulebook or sim file is edited here.
**Sky's call (2026-10-26):** Option A of `split-getaway.md`: two cars, two Go's. On the pickup way the Enforcer drives Vale off in Vale's own car; the other three get out in the cell's car.
**Main's call, not Sky's (flagged so Sky can overturn it):** agents still on the map at the end are captured while the rivals stand, so the rearguard fight can't be skipped. It is one value, `split-strand-capture` (§4), so Sky's answer is a value change, not a rebuild.
**When:** after Keel's engine 0.35.0 (the B fight) lands, behind the switch `getaway-split`. With it at 0, every run replays byte for byte as 0.35.0 does.
**Tags:** every rule here is **today** (the game, not the lab), [S] (a starting value, tuned by play). None is behind a fight-lab switch. `getaway-split` is a build switch for Keel's rollout, not a lab switch: once it ships at 1 it is the game.
**Evidence:** the measured play is in `split-getaway.md` §8 and `probe/` (engine at 82eaae77 with one probe switch; with the switch off it replays the c038 mission check exactly). The geometry is in `measures.json`.

Words used below: a **cell car** is any car the cell may order (its own car, or a lent car). The **target** is Vale in the Handover. A **stand** is a place a car stands, with its exit lane, ambush spots and police stands.

---

## 1 · The rules in one table

| # | rule | what it says | replaces | tag |
|---|---|---|---|---|
| R1 | the lend | A turned driver's car, standing at the stand he was turned to bring it to, with the target inside, becomes a cell car when the in-car Take ends. The Enforcer is at the wheel with the target; the driver steps out within the Take (no extra time) and walks off. | nothing (new) | today [S] |
| R2 | cars, plural | Each cell car has its own stand, seats, hold, exit lane, blocker, gate, seen-leaving and Go. Board, Drive and Go name the car. | "the getaway car" (singular) in §06 | today [S] |
| R3 | the win | Fixed the tick a cell car leaves with the target aboard. | "Vale in the getaway car when it leaves" | today [S] |
| R4 | the run-on | After the win the mission runs on. It ends when no agent stands on the map outside a car that has left, or at End. The menu's Withdraw becomes End once the target is out. | "the car leaves … and the mission ends that tick" | today [S] |
| R5 | left behind | Judged once, when the mission ends, not at each Go: a downed agent not in a car that left is captured; a standing one is captured if a hostile is up and not broken, or the seal runs; otherwise he walks out free. With `split-strand-capture` 0, rivals alone (no police on the map, no seal) don't capture: he walks out. | judged "at the tick the car leaves" | today [S]; the capture is **main's call** |
| R6 | fails after the win | Once the target is out, the seal and the cell all down no longer fail the mission: whoever is left is captured. Every other fail is about the target and can't happen after the win. | §07 fail list, unchanged before the win | today [S] |
| R7 | the spring | The crew posted round a cell car springs as today (the target within 20 m of that car), **or**, while the target is in another cell car: an agent within 20 m of their car, or the target's car leaving. | "as Vale comes within 20 m" | today [S] |
| R8 | Go order in a tick | (1) acts land (Take, Board, step-out); (2) the spring check (R7 a, b); (3) the Go's: the target's car first, then the spring check (R7 c), then the other cars by car id; each Go judged against its car's state at that moment. | nothing (one Go a tick today) | today [S] |
| R9 | Go refused per car | Refused on a car that is held (its own hold), gated (the fight is at it), boxed in (R10), or has no standing agent aboard. Each refusal names its own holders. | the gate "with Vale aboard" | today [S] |
| R10 | boxed in | A standing car on another car's way out (the route from its stand to the first carriageway, at the car's width plus clearance) refuses that car's Drive and Go, and names the car in the way. | nothing (cars never block cars today) | today [S] |
| R11 | the lane from the car | Every stand's exit lane runs from the car: along the drive at the drive's width, then the carriageway at the road's width, 30 m in all. This is M059-B10 (Motive's ruling of 2026-10-26 that the yard's lane starts at the car), widened to every car on a drive, lent cars included. | "the first 30 m of the getaway route's carriageway" | today [S] |
| R12 | the car left behind | A cell car of the cell's own still standing on the map when the mission ends is lost, and charged as seen leaving: +1 Trail, hunted next mission. A lent car can't be left (it leaves with the target or the mission is no win). | nothing (new) | today [S] |
| R13 | the loud fight's car | Police come for the car with the target aboard (or, before he boards, the car he is being taken to, as today). Once the target is out they come for the cell car nearest any standing agent. | "the getaway car's stand" | today [S] |

---

## 2 · GDD text drafts (present tense; for Aster)

### §06 FIGHTS · getaway (replaces the getaway block; unchanged lines are kept as they are)

> getaway [S]: the cell gets into a car and drives away; the Enforcer can't run, so the others support him. The cell may have more than one car: its own, and a car a turned driver lends (below). Each cell car has its own stand, seats, hold, exit lane, blocker and Go.
> - cars: a standing car is solid: people walk round it, and reach to it is measured from its body; a driving car blocks nothing. A standing car on another car's way out (the route from its stand to the first carriageway) boxes it in: that car's Drive and Go are refused and name the car in the way.
> - lend: a turned driver whose car stands where he was turned to bring it, with the target inside, lends it: when the in-car Take ends, the Enforcer is at the wheel with the target and the driver steps out and walks off. A lent car is a cell car for Board and Go; it is never Driven to another stand.
> - Board: an order on a cell car, by any op within 1 m of its body, not running, mid-action or carrying a body: 1 s, or 4 s carrying (the carried go in first; only the Enforcer loads Vale). In a car an op is off the street and doesn't fire, and can still be hit as a target beside cover (×500): hiding in a held car loses. A move on a boarded op steps out at the car's side (1 s); Vale stays in. Standing at a car is not boarding. Allies (the guard, joiners) never board. One order boards every selected agent who can, one Board each in the same tick.
> - Drive: an op in the cell's own car drives it to one of the mission's car stands, on the drive grid at a car's pace (5 m/s on a carriageway, 2 m/s on a drive or in a bay); $0, 0 T, no Alert. A Drive under way runs to its stand: it can't be stopped, and nobody steps out until the car stands.
> - Go: an order on a cell car once a standing op is in it (a downed op can't drive). The car leaves at once with whoever is inside. Go is refused while that car is held, gated or boxed in, and the refusal names why. The tick the target's car leaves, the mission is won; it runs on until no op stands on the map outside a car that has left, or End. Before the target is out, Go without him is the cell withdrawing by car (Withdrawn, left-behind rules apply) unless another cell car still holds an op, and the menu's Withdraw stays a free abort that captures nobody; once he is out, the menu offers End, judged by the left-behind rules.
> - order in a tick: acts land first; then the spring; then the Go's, the target's car first, then the spring again (the target gone), then the other cars by id, each judged as its car stands at that moment.
> - the hold: a combatant not down, broken or gone holds a car while it stands within 8 m of it (any level, line or not) or on its exit lane: the first 30 m of the stand's route out from the car, at the width of the way it runs on (a drive at the drive's width, then the carriageway at the road's), derived from the stand's getaway route (no route, no mission: unknown = reject). An ambusher lying in wait holds only if its spot does.
> - the blocker: one hostile per fight goes for the road of the car the fight is at: in the safe branch the ambusher with the shortest grid walk to its post, moving when the ambush springs; in the loud branch the first officer on the street to reach his post (ties by id). Its post is the nearest free cover inside that lane 10–13 m from the car, else the open lane centreline 10 m along it. It walks there without stopping, firing as it walks, holds it as police hold a stand, and is never replaced: down it or break its crew (a rival blocker breaks with its crew) and the car is free.
> - the fight always comes [S]: once the final fight starts, Go on the car the fight is at is refused until its blocker is down or gone, or the fight ends; crew sent away (a false tip, a warned lookout) are recalled to it, never spawned. The fight is at the car the posted crew stand round (safe), or at the car with the target aboard, then, once he is out, the cell car nearest a standing op (loud). A prepared cell meets a smaller fight, never none.
> - left behind, when the mission ends: a downed op not in a car that left is captured · a standing op is captured if a combatant is still up and not broken, or the seal clock runs · otherwise he walks out, free, and is back next mission. A failed mission captures nobody (§08 captives).
> - seen leaving: a police officer, a robot or an engaged patrol up and with a car in its cone or within 13 m on a clear line (a final-fight officer: a clear line within its weapon's max, 16 m) makes that car's departure one witnessed incident: +1 T, and the investigation holds the cell's car as a hunted car next mission (§08); a lent car seen leaving is +1 T and a lead, not a hunted car. A car of the cell's own still standing on the map when the mission ends is lost and counts as seen leaving. Rivals don't report. The final fight's shots stay untraced; this charges the departure.
> - rivals reach Vale: a rival within 1 m of a cell car's body while Vale is in it, with no op standing within 6 m of that car, fails the mission. (Rest of the line unchanged.)
> - so every route chooses at the car the fight is at: board and go (down the blocker, board, Go: fewer seconds under fire, the fight moves Standing 0) or fight it out (break the crew, then go: +1 against feared rivals).

### §07 HANDOVER (lines that change)

> places: … a turned driver brings Vale round in his car to the east yard's service bay, by the service drive · the getaway car stands at the square's boarding pavilion; its second stand is on the service drive, nose out, behind Vale's car (a car there boxes Vale's car in).

> route B, the pickup (from 6:10): Scout listens to the driver: he needs cash (42 s) → Fixer turns the driver before 5:40 (Pay $70,000, or Help $40,000): he warns the rivals' lookout off, and once Vale is in he drives him round to the service bay (13 s) and waits there until 8:30 → Scout gives the rival car a false tip (44 s), or it follows Vale's car and its wheelman reaches him first → Tech reroutes the patrol from the cabinet (by 5:00), so the yard's sweep is away at the pickup → Enforcer takes Vale from the back seat (38 s); the driver steps out and the Enforcer takes the wheel → Scout, Fixer and Tech gather at the cell's car among the posted three → Go on Vale's car: Vale is out, and the posted three spring on the cell's car → the three down the blocker, board, Go (or break the crew). The cell may instead bring its car round behind Vale's (Drive, 28 s) and carry Vale 3.5 m into it: the one-car way.

> the Take [S]: at Vale's own car with his driver turned, in the service bay, the Take happens inside the car, off-screen, with nothing to witness, and ends with the Enforcer at the wheel: the driver lends the car (§06). A Take begun before 8:30 holds the driver until it ends. …

> getaway [S]: the getaway car starts in the square's bay. An agent in it can Drive it to one of the mission's car stands: the square's bay, or the service drive behind Vale's car (nose out, once Vale's car is in; on the drive first, it blocks his way and he stops in the road). Vale's car, lent by a turned driver, leaves the service bay down the drive and east along Apron Road. Each stand has its own exit lane, ambush spots and police stands.

> rivals [S]: … The three posted round the cell's car spring as Vale comes within 20 m of it or, once Vale is in Vale's car with the Enforcer, as an op comes within 20 m of it or Vale's car leaves.

> final fight [S]: always … (unchanged), at the car the posted crew stand round (safe) or the car with Vale (loud).

> win: Vale in a car of the cell's when it leaves. The mission runs on until the rest of the cell is out or left behind.
> fail: (unchanged list), and the district seals and the cell down (every op down) fail only while Vale is still on the map; after he is out they capture whoever is left.

### §08 CAMPAIGN (lines that change)

> hunted car [S]: a getaway seen by security, or the cell's car left standing on the map at the end, puts that car in the investigation as a hunted car next mission; the cell must answer it (a new car through a contact). A lent car seen leaving is a lead, not a hunted car: the cell doesn't keep it.

> debrief rows:
> | car seen [S] | per car: who saw it leave, or "left on the street"; +1 T; the cell's car hunted | seen leaving, the car left behind (§06) |
> | left behind [S] | who, fate (captured / walked out), why (down when the mission ended, the fight still on, the district sealed, the street calm) | the mission's end (§06) |

### §13 test T11 (agent copy; for Aster)

> | T11 | the getaway reads? | Handover final fight, both branches and the split pickup; ask "why won't the car go?" (each car), "who did you leave, and what happens to them?" | ≥ 5/6 name the blocker or the car in the way; ≥ 5/6 say captured or walked out and why |

---

## 3 · Prices

Every price shows on the order line before commit (§02), per car.

| act | $ | time | Trail | Alert | Standing | other |
|---|---|---|---|---|---|---|
| Pay the driver | 70,000 | 50 s | +2 | – | – | unchanged |
| Help the driver | 40,000 | 50 s | +1 | – | – | unchanged |
| the lend (inside the in-car Take) | 0 | 0 s (the Take's 38 s) | 0 | – | – | the driver steps out, keeps quiet (turned for this very act) |
| Go on a lent car | 0 | 0 s | 0; +1 if seen leaving | – | – | seen: a lead, not hunted |
| Go on the cell's car | 0 | 0 s | 0; +1 if seen leaving | – | – | seen: hunted next mission |
| the cell's car left on the map at the end | – | – | +1 | – | – | hunted next mission (a new car through a contact) |
| an agent captured | – | – | +4 | – | 0 | held at the police post, rescue 3 missions, Talker clock; abandoned standing: resentment +1, loyalty −1 (unchanged) |
| the rearguard fight | – | – | 0 (final fight untraced) | – | board and go 0; feared rivals broken +1 | a public fight (the square is busy) |

**The driver's Talker facts.** Under the split he never sees the cell's car, so he holds the Enforcer's face and place and time, not `cell-car`. His test is unchanged: paid or helped, loyalty 1 (being turned), fear 0 and resentment 0, so he stays silent.

**Heat next mission** (Trail + Menace; measured on the probe for a prepared route B: Pay +2, tip +1, reroute +1):

| result | Trail | Standing | Heat | band |
|---|---|---|---|---|
| focus fire, board and go | 4 | +2 | 4 | noticed |
| focus fire, break the crew | 4 | +3 | 4 | noticed |
| at will, the downed Scout left | 8 | +3 | 8 | wanted |
| three captured, the car lost | 17 | +2 | 17 | infamous |
| today, route-b-board (for reference) | 4 | +3 | 4 | noticed |

---

## 4 · Rulebook entries (for Keel)

Values (rulebook `values`). Every entry is `"tag": "S"` and `"proposed": true`. Src for all: "Sky 2026-10-26, Option A of briefs/071/motive/split-getaway.md; Motive briefs/071/motive/rules.md".

| id | v | unit | reason | marks |
|---|---|---|---|---|
| `getaway-split` | 1 | count | The cell may hold more than one car: a turned driver lends his, and each car has its own hold, lane, gate and Go (R1–R13). 0 reads every rule as engine 0.35.0 does: one getaway car, the mission ends at its Go, and every run replays byte for byte. | build switch, today once shipped |
| `split-strand-capture` | 1 | count | Left behind with only rivals up (no police on the map, no seal running): 1 = captured (the rivals pin him until the police come), so the rearguard can't be skipped; 0 = he walks out and only the car is lost. | **main's call** (2026-10-26, Sky didn't answer); Sky may set 0 |
| `lend-step-s` | 0 | s | The driver steps out within the in-car Take: the Take's 38 s already covers the change of seats. | today |

The rest reads existing values: fight-hold-m 8, fight-block-m 30, fight-block-near-m 10, fight-block-far-m 13, fight-ambush-m 20, witness-trail 1, captive Trail 4, car-len-cm 473, car-w-cm 182, car-clear-cm 50, board-s 1, car-road-cm-s 500 and car-yard-cm-s 200. No number changes.

Rule blocks (text for Keel to place; the keys are suggestions):

- **`car.lend`**: R1. Reads: the role whose car it is (mission roles: the driver), `turned`, the car's stand at the in-car Take (`car.pickup`'s service bay), the target aboard, `lend-step-s`. The driver leaves by his routine's way off the map (Pulse) and is a turned witness of nothing (reports row 1). Refusals: none new (the Take's own refusals stand).
- **`getaway.cars`**: R2. Cars: `car:getaway` (the cell's: Drive allowed, hunted if seen), plus any lent car by its own id (`car:official`: no Drive, a lead if seen). Seats 5 each (the cell's four plus the target). Each car carries stand, lane, holders, blocker, gate and left tick.
- **`getaway.goOrder`**: R8, as in §06 "order in a tick". Ties by car id. The R7 c spring and the blocker pick happen between the target's Go and the other Go's, so a car with no blocker yet is gated by "whoever is coming" (today's `alwaysRule` (1)).
- **`fight.alwaysRule` (amended)**: R9. Drop "with Vale aboard" when `getaway-split` is 1; the gate is on the car the fight is at (the posted crew's car, safe; R13, loud). Refusal code stays `car-held`, with the car's id in `ids`.
- **`getaway.boxed`**: R10. Refusal code `car-boxed` (new; Fable's words), ids = the car in the way. The way out is the stand's lane polyline up to the first carriageway, swept at car-w-cm + 2 × car-clear-cm. Only a standing car boxes; a car driving blocks nothing (unchanged).
- **`fight.holdRule` / lane (amended)**: R11. The lane is parts: from the car along the drive at the drive's own width (civic-block drives), then the carriageway at the road's width, fight-block-m in all. A stand on a carriageway is unchanged: the square's lane stays byte-identical. Splice's `exitLane`/`stands.ts` exports the parts for every stand, the lent car's bay included. Measured: the bay's lane is 22.25 m of drive, then 7.75 m of Apron Road; its post 10 m down the drive at (73.75, −15.0). Today's letter would start it 22.25 m out.
- **`rivals.springSplit`**: R7, read only with `getaway-split` 1.
- **`getaway.end`**: R3–R6. Log codes: `vale.out` (the win fixed), `mission.end` (cause: last car, End, nobody standing). End is an order on the cell (menu), offered only after `vale.out`; before it, Withdraw is unchanged.
- **`debrief.carLost`**: R12, a `car-seen` line with `by: "left"`; the hunted fact as for a car seen.
- **`fight.loud` (amended)**: R13. The entry nearest the fight's car's stand (Sky's S2, 2026-10-25, unchanged); after `vale.out` the officers re-plan to the remaining car's police stands.

**Scenarios Keel adds** (each with its twin at `getaway-split` 0, which must equal 0.35.0's result):

| id | sets up | expects |
|---|---|---|
| SPLIT-LEND | route B to the in-car Take | the Take ends with the Enforcer at the wheel; the driver out and walking; Go offered on `car:official` |
| SPLIT-WIN-FIXED | Go on the lent car, the three still at the square | `vale.out`, outcome not yet final; the mission runs on |
| SPLIT-SPRING-NEAR | the three at their car, the Take ends | spring the tick the Take lands (R7 b) |
| SPLIT-SPRING-LEAVE | the three 30 m away, Go on Vale's car | spring the tick it leaves (R7 c); the three's car held |
| SPLIT-SAME-TICK | Go on both cars in one tick, the three aboard theirs | Vale's car leaves; the cell's car refused `car-held` (gated by whoever is coming) |
| SPLIT-GATE-PER-CAR | a fight at the square, Vale's car free in the yard | Go on Vale's car taken; Go on the cell's car refused until the blocker is out |
| SPLIT-BOXED | the cell's car Driven to the service drive, then Go on Vale's car | refused `car-boxed`, naming `car:getaway` |
| SPLIT-LANE-BAY | a hostile on the drive 10 m from Vale's car | the car is held (R11); at today's letter it would not be |
| SPLIT-LOST-CAR | End after `vale.out` with the cell's car standing | +1 T, hunted, the debrief's `car-seen` line `left` |
| SPLIT-STRAND | End after `vale.out` with the rivals up, no police | captured at `split-strand-capture` 1; walked out at 0 |
| SPLIT-AFTER-WIN-SEAL | loud, the seal lands after `vale.out` | success; the agents left are captured, no `district-seals` fail |
| SPLIT-WITHDRAW | Go on the cell's car without Vale, Vale still in the yard with the Enforcer | the car leaves; the mission doesn't end (another cell car holds an op) |

**Reference numbers** for the mission check (probe, engine 82eaae77; `split-getaway.md` §8): the Take done 7:03.9; with the three at their car, focus fire downs the blocker 2.1 s after the spring and the car leaves 4.2 s after it (board and go) or 5.8 s (break the crew); Trail 4, Alert 0. Waymark's composed routes decide the recorded values.

---

## 5 · Unchanged, superseded, open

- **Unchanged:** the driver's prices and 5:40 deadline, the false tip, the floor of 3 rivals, rivals reach Vale (per car), Drive's prices, the custody and Talker rules, and every one-car route (A, G, the one-car B): with Vale brought to the cell's car, R7 a is today's spring and the gate is today's gate.
- **Superseded:** the singular getaway lines in §06, §07's win line, "captures happen only at Go" (now: when the mission ends), and M059-B10's scope (the yard stand only → every drive stand).
- **Open:**
  - **For Sky:** `split-strand-capture` (main's call, 1).
  - **For Pulse:** the driver's way off the map after the lend.
  - **For Fable and Glyph:** the words and marks for `vale.out`, End, `car-boxed`, the per-car holders and "left on the street".
  - **For Vantage:** check the bay's police stands and ambush spots against R11's lane (the svc-* spots were placed for the service-drive stand, 5.6 m away).
  - **For Suture:** play the rearguard for sharpness. On the probe, a careless approach loses an agent: at will, one down in 7 s; arriving one at a time, two down.

**Next owner:** Aster (the GDD text into a new version, then `bun run crew`), then Keel after 0.35.0 lands (`getaway-split`, the blocks and scenarios above), then Splice (the lane parts), Vantage, Waymark, Latch, Fable and Glyph.
