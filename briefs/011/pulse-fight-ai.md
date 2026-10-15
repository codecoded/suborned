# 011 · Fight AI: how hostiles and crowds behave in a real-time fight (Pulse, 2026-10-15)

For Keel to implement, through Aster. Sky approved real-time shoot-outs from Motive's proposal (`briefs/011/motive-fights.md`, §2.7 hands the hostile behaviour, the cover points and the crowd's scatter to Pulse). Motive owns the numbers of weapons, hit rates, health and cost; this file owns what a hostile or a crowd *does*. Nothing in `sim/` is touched; nothing is in the rulebook yet.

**What Keel gets**

| file | what |
|---|---|
| this spec | states, transitions and thresholds, as rules a 10 Hz integer sim can run |
| `briefs/011/pulse-fight-ai.values.json` | 28 proposed rulebook `values` entries (id, v, unit, tag, src, reason), the same shape as `rulebook.json` |
| `contracts/world/civic-block.cover.json` | the cover points, sight blockers and refuges of the civic block, `suborned.cover/1`, block frame, integer cm. Schema `contracts/world/cover.schema.ts` |
| `bun run cover` | `tools/cover/`: the generator, the rules CV-1 and CV-2, and a report of where a fight lacks cover. `bun test tools/cover` (26 tests) |
| `tools/cover/select.ts` | the engine's three cover functions in integers (`protects`, `coverModifier`, `selectCover`), as the reference for the C#; the vectors in §8 are what both give |

## 1. Determinism: what makes it replayable

All of these are rules for the tick, so that the same orders give the same fight and the player can predict a hostile from what is on screen.

1. **Integers only.** Positions are cm, speeds cm/s (a body moves `speed / 10` cm a tick by the engine's own Path convention), distances compare as squared integers, angles are the per-mille `cos` the cover file carries, rates are per mille. No floating point, no `Random`.
2. **A tick has an order.** Bodies act in this order: the cell, allies (the guard), security (response unit, officers, the guard when engaging, the patrol), rivals; inside a group by id, ordinal string compare. Ties in any choice (nearest foe, nearest point) break on the lower id.
3. **Shots resolve together.** Every body decides from the start-of-tick state; the shots are applied at the end of the tick (accumulators first, then damage, then `down`). A body downed at tick t fires nothing from t+1, and no body has the advantage of acting earlier in the order. Motive's accumulator rule is unchanged.
4. **One transition a tick**, evaluated before the action of that tick.
5. **Memory is a number.** "Whoever is shooting at me" is the foe that last *fired at* this body (hit or miss) within `line-lapse-s` (3 s = 30 ticks, Motive's own value); the body stores the foe's id and the tick.
6. **Nothing the AI does uses the crowd.** A hostile's own sight ignores people, a shot never hits a bystander (§06: civilians are never the payload), and people never block movement. The crowd occludes only *watchers* (§4).

## 2. One machine, six parameter sets

Every armed hostile, the guard when he engages, and the guard who is your ally run the same machine; §3 gives each one's weapon, speed, cover reach, objective, trigger and exit. The patrol bot runs a reduced copy.

### 2.1 States and transitions

| state | enters when | does | leaves to |
|---|---|---|---|
| `routine` | start | its schedule (routines, the patrol's sweep, a post) | `alerted` on its faction's trigger (§3) |
| `alerted` | the trigger | stands, turns to face it, shows the alert glyph; moves and fires nothing for `react-s` (2.0 s, Motive). A summoned body (officers, the response unit) skips it; the ambush crew's wait is `aim-new-s` (1.0 s), plus `fight-warn-s` (10 s) if Inez warned | `engage` if a foe is in reach and sight when the time is up, else `advance` |
| `advance` | alerted or hold with no foe in reach, and an objective to reach | runs the grid route to its **stop point**: first the cover point `selectCover` gives (§5.3), else the point on the route at `ai-stop-reach-pct` (90%) of its weapon's effective reach from the foe, or from the objective if it has no foe. Fires nothing while it moves (run or shoot, Motive) | `engage` the tick a foe is in reach and sight; `hold` on arrival; `break` (rivals) |
| `engage` | a foe is in **reach** (inside the weapon's effective reach and a clear sight line, `eye-cm` 160) | stops where it is that tick, picks its target (§2.2), aims `aim-new-s` on a new target, fires at its cadence by the hit model. May **step to cover** (§2.3) | `hold` or `advance` when no foe has been in reach for `ai-engage-lapse-s` (1.0 s): `hold` for holders, `advance` for the rest; `break`; `down` |
| `hold` | arrival at the stop point with no foe; or a holder (police, the patrol, a guard at his door) when its foes leave reach | stands, faces the last foe or the objective, watches. **Keeps its aim warm**: a target it loses and reacquires within `ai-reacquire-s` (3 s) is shot at once, with no new aim delay | `engage` the tick a foe is in reach |
| `break` | rivals only: more than half the crew down (§3.1) | runs at its top speed on the grid route to its car (`rivals.park`), fires nothing, takes no cover. It is no longer a combatant (stances `return` and `free` do not shoot it; an explicit `fire` still may), no longer holds the car (`fight-hold-m`), and a drop is no drop for Trail | `gone` within `rival-gone-cm` (1 m) of the car |
| `gone` | | out of the fight, not on the street | — |
| `down` | health ≤ 0 | falls; its cover point is released; its accumulators are dropped | — |

**Reach and foes.** *In reach* means distance ≤ the weapon's effective reach (pistol 15 m, carbine 20 m) and `Sees` with the site's occluders (plus the cover file's blockers, §5.4). Foes are the operatives of the cell (every one, armed or not: Motive's priority 4), and, for the rivals, the guard when he is your ally. Never Vale, never a civilian or a contact.

### 2.2 Choosing a target

A body chooses at the tick it enters `engage` or `hold` with a foe in reach, and again at every shot. Four **classes**, in this order, Motive's list:

1. a foe that fired at this body in the last 30 ticks (the most recent shooter; ties on id);
2. the operative who holds Vale (`Vale == WithEnforcer`);
3. the nearest foe with a weapon drawn;
4. the nearest foe.

The body is **committed**: it keeps its target until it is down, out of reach, out of sight for `line-lapse-s` (3 s), or a foe of a *better class* appears (a class 1 foe may replace a target of class 2, 3 or 4; class 2 may replace 3 or 4; a foe of the same class never does). Choosing a new target costs `aim-new-s` (1.0 s) before its first shot, which is what keeps two foes taking turns to shoot a hostile from making it dance. One order for rivals and police alike, so the player learns one rule; Motive's calculator can swap classes 1 and 2 for police (`OFFICER_PRI=enforcer`) if the numbers want it.

### 2.3 Cover: how a body uses it

Cover points are data (§5). The rules:

- **A body is beside a point** within `cover-snap-cm` (70 cm) of it. A body that is standing still beside a point that `protects` it from its shooter takes the hit modifier: `mod-cover` (500 per mille, Motive) for a point of quality ≥ `cover-q-full` (600), `mod-cover-partial` (750, proposed) for 350–599. Inside `point-blank-m` (5 m) cover turns nothing. A body that is moving takes none (Motive: "standing still beside cover").
- **One body to a point**, from the tick it is assigned until it leaves, breaks or is down. Assignment follows the tick's body order, so it is deterministic.
- **Choosing a point** is `selectCover` (§5.3): candidates are points not taken, present, at least partial, within the faction's seek distance; with a foe it must shield against the foe and may not bring the body more than `cover-step-closer-cm` (3 m) nearer the foe (it may fall back to do so); advancing without a foe it may not take the body more than `cover-retreat-cm` (2 m) back from its objective. The choice minimises `distance − quality` (cm against per mille), then the lower id.
- **Step to cover.** A body in `engage` that is not protected from its target, and for which `selectCover` finds a point, steps to it; it fires nothing while it steps. At most one step each `cover-step-gap-s` (4 s). It leaves its point only when it has been unprotected from its target for `cover-leave-s` (2 s) or the point is gone (a car that drove off).
- **Holders** (police, the patrol, a guard on his door) do not step: they stand where the data put them. The data decides whether the stand has cover (§6 says which do not).

## 3. The parameter sets

| | rivals | officers (police) | response unit | guard, your ally | guard, engaging | patrol bot |
|---|---|---|---|---|---|---|
| weapon (Motive §2.2) | pistol | pistol | pistol | pistol | pistol | pistol |
| skill | untrained ×750 | trained | trained | trained | trained | trained |
| health | 3 | 3 | 3 each (`response-unit-officers` 2) | 3 | 3 | 4 |
| speed (cm/s) | 160 `rivals-speed-cm-s` | 220 `security-speed-cm-s` | 220 | 220 | 220 | 85 `patrol-chase-cm-s` |
| cover seek | 800 | 800 | 1200 | 600 | 400 | none (a robot) |
| steps to cover | yes | no | no | yes | no | no |
| breaks | when more than half down | never | never | never | never | never |
| objective | the getaway car; else the last place it saw the cell | the police stand, then the car | the nearest police stand | the getaway car | his post | the shot's origin |

The officers' weapon follows Motive's calculator (a trained pistol); a carbine would be one value (`weapon.carbine` for `OFFICER_WEAPON`).

### 3.1 Rivals

*Violet. The ambush crew of the safe fight; the crew drawn by gunfire elsewhere.*

- **Lurk.** In the final fight the crew stands at `ambush-1…n` (n from the rulebook `fight.safe`: 5, −1 for the turned driver, −1 for the earlier false tip), not present until the ambush springs: the Enforcer with Vale within `fight-ambush-m` (20 m) of the getaway car (rulebook, unchanged).
- **Spring.** Each rival goes to `alerted` for `aim-new-s` (1.0 s), +`fight-warn-s` (10 s) if a helped Inez warns; then `engage`, if the Enforcer is in reach, or `advance`. During the warning they stand where they are: the player has seen them and may answer.
- **Advance.** To the cover point `selectCover` gives with the Enforcer (or the car) as its foe, within 800 cm; else straight to the stop point 13.5 m from the car. The posts stand 13.4–15.9 m from the car, so most ambushers are in reach of it from where they lurk.
- **Engage.** Stops, fires at its cadence (15 ticks) at the §2.2 target, steps to cover once if it has none.
- **Break.** The crew is every rival the fight made (`n`). At the tick a rival goes down, if `downs × 2 > n`, every rival still up goes to `break` at once. It runs the route to `rival-park` (77 m from the car, about 50 s at 160 cm/s in a straight line, more along the route) and is `gone` at 1 m. Down is down; running away is not a drop (no Trail), as Motive's proposal says. A broken rival no longer counts for `CarHeld` (the car may leave).
- **Drawn by gunfire, elsewhere.** Rivals called by gunfire (`rivals-drawn-s`, 16 s) arrive at `rival-entry` as `advance` with the Office front (`rivals.front`) as their objective, and then run the same machine. They are not an ambush: no lurk, no aim delay beyond `react-s` if they see the cell act first.

### 3.2 Police on the loud branch

*Amber. Officers: `fight.loud` 2, +1 at Alert 3, +1 if a civilian was shot.*

- **Arrive.** At the tick the final fight turns loud the officers appear at `security-entry` in `advance` (summoned, no alert delay) and run the grid route (56.9 m straight; about 26 s at 2.2 m/s, more along the route) to `police-1…n`, the first n posts, 12–19 m short of the car. §07 and the rulebook call the entry "the west street"; the site calls it the north street. The point is `security-entry` (block −46, −32 m).
- **Hold and suppress.** On reaching its stand an officer is in `hold` and **never leaves it**: no advance, no step to cover. In `engage` or `hold` it fires at its cadence at the §2.2 target **even when the target is behind cover**: it does not wait for an exposed foe, so cover costs the cell accuracy (×500) but never buys quiet. It keeps its aim warm for `ai-reacquire-s` (3 s): a foe who steps out of a pier's shadow is shot at once.
- **Do not break.** No morale rule. At the seal they hold the cordon (`seal-s`, unchanged).
- **A stand holds one body.** A body sent to a taken stand (the response unit joins, §3.3) takes the nearest free cover point within 800 cm of the stand, else a slot `ai-stand-overflow-cm` (120 cm) beside it along the perpendicular to the car, alternating sides.

### 3.3 The response unit

It is already on the street (`response-unit-officers` 2, §05 search: a response unit to the reported route; the seal 2:25 after it arrives). In a loud fight it is the same police.

- **En route** (routine): its own route `security-entry → via → to` (`d:Office approach`, `d:Service lane`) at 220 cm/s, as the engine runs it.
- **Join.** At the tick the final fight turns loud, or at its arrival if that comes later, each officer of the unit goes to `advance` with *no alert delay*, from wherever it stands, to the nearest police stand it can reach (overflow rule above), by the grid route, and joins §3.2 from then on. A unit that is still dispatched when the fight starts joins the moment it arrives at `security-entry`. It does not wait for the officers: the unit already on the street is why a loud fight with the cell alone is lost (Motive §2.9).
- **While en route and shot at**, before the final fight, it engages anyone in its reach who fired or drew (Motive §2.3: the watcher engages, reacting 2.0 s), with this machine, and goes on to the join when the fight starts.

### 3.4 The guard who is your ally in the safe fight

§07: the guard "never saw you" fights the rivals. Engine condition kept: `!GuardDown && health == full && no memory of the cell`; a guard who saw a runner (Motive §1) holds a memory, so he is not an ally.

- **Run.** At the spring he goes `advance` (no alert delay: he hears the shooting) from his post to `guard-ally` (9.8 m from the car, the Office side) at 220 cm/s, taking cover within 600 cm of it if `selectCover` finds one.
- **Engage.** Pistol, trained. His foes are the rivals. His targets, in order: 1, a rival that fired at him; 2, **the rival nearest the Enforcer** (the carrier is what he is there to cover); 3, the nearest rival. Rivals shoot him by their §2.2 class 1 and 4 like anyone.
- **He does not break.** If he is hit he fights on from cover; he is down at 0. If he is down the cell is alone again.
- **A guard who is not your ally** and sees no fight does nothing: a loud branch has no guard-ally, and in the safe fight a guard with a memory holds his post (the rulebook's untraced fight adds no Alert).

### 3.5 The guard, engaging (a loud run before the final fight)

When a drawn weapon is in his cone and reach, or gunfire is within 30 m (`patrol-break-m`, the same distance the patrol uses), he is `alerted` (2.0 s), then `engage`s from his post: pistol, trained, no cover (`ai-seek-guard-cm` 400, no steps), follows a foe no farther than `guard-leash-cm` (15 m) from the door, goes back to it `guard-return-s` (10 s) after the last foe left reach. Motive's gate already raises Alert to 2 when security sees a drawn weapon; this is what he does after.

### 3.6 The patrol bot

A civic robot: clockwork-smart, a fixed routine, breaks it only for gunfire.

- **Sweep** (routine): `patrol-a ↔ patrol-b`, `patrol-period-s` 100 s, its cone (rulebook).
- **Break.** At a shot whose *shooter* is within `patrol-break-m` (30 m) of it, and it is not already engaged: it goes to `advance`, no alert delay, along the grid route to the shot's origin as it was at that tick, at `patrol-chase-cm-s` (85 cm/s: it is slow; the cell has a minute or more). A shot beyond 30 m moves it not at all.
- **Engage.** On a foe in reach and sight: stops, fires pistol, trained, 4 health, §2.2 target order. It never takes cover and never breaks.
- **Resume.** `patrol-resume-s` (15 s) after the last foe left reach it returns to the nearest point of its sweep and resumes. Any shot calls the response unit as the rulebook already says (`WakePatrol`); that is not new.

## 4. The crowd

*People on the street: the simulated people of the cast and the anonymous background walkers (`briefs/010/pulse-district-plan.md` §3). §06 citizen actions; §04 Menace (crowds scatter under fire).*

| state | enters when | does | leaves to |
|---|---|---|---|
| `routine` | | its routine, or the walker's route | `startled` |
| `startled` | a **shot** within `crowd-scatter-m` (30 m) of them, whether they see it or not; or a **drawn weapon** within `witness-range-m` (13 m) on a clear line | stands for `crowd-startle-ticks` (5 ticks = 0.5 s); a Fearful person for `crowd-startle-fearful-ticks` (2). **This is when they witness** (below) | `fleeing` |
| `fleeing` | startled time up | runs at `crowd-run-cm-s` (260) by the grid route to a **refuge** (below) | `sheltered` on arrival |
| `sheltered` | at a refuge | a door: inside (exteriors only: off the street, not drawn, still counted); a mouth: off the block's window | `returning` `crowd-quiet-s` (30 s) after the last shot of the fight |
| `returning` | quiet | walks back from the refuge to the stop their routine has for the clock, by the grid | `routine` |

- **Refuge.** From the cover file's `refuge` list (the block's entrances and the two street mouths): the refuges within `crowd-flee-max-cm` (40 m) of the person that end **farther from the nearest shooter** than the person stands; the nearest of those, ties on id. If none: the refuge farthest from the nearest shooter. "Nearest shooter" is the shooter of the most recent shot. So the crowd runs *away*, into the doors and out of the square, and a shoot-out at the car empties the café's terrace into the café.
- **Witnesses.** At every shot's tick, and at every hit, the people (simulated or background) within 13 m with a clear line to the shooter or the target are witnesses: simulated people write a `saw-gunfire` memory (place, time, and the Fixer's face if it was the Fixer); background walkers are only counted. The count is what Motive's rules read (a civilian who sees violence sets Alert 3; one incident adds witness Trail once). Taken **before** the startle delay ends, so the first shot is always witnessed by whoever was there.
- **Scatter breaks sightlines.** A person on the street who is not sheltered occludes a *watcher's* sightline (the guard, the patrol, the cameras, the response unit) that passes within `crowd-occlude-cm` (30 cm) of them (a body about 60 cm across, head above the eye). A running crowd crossing a cone is intermittent cover for the cell, a crowd standing round a fountain is none; this is §05's "crowds also block guard/camera sightlines", made a rule. Hostiles' own sight ignores people (§1.6), so a fight stays readable. The engine does not yet occlude by people; it needs a per-tick grid of positions.
- **Nobody is hit.** No stray fire, no bystander as the payload. Cost for hitting a civilian stays Motive's (explicit Ctrl-fire).
- **Hero and Menace** (§04: join and hide, or obey and report) are [L] and not specified here.

## 5. Cover, as data

### 5.1 The file

`contracts/world/civic-block.cover.json`, `suborned.cover/1` (schema `contracts/world/cover.schema.ts`), block frame, integer cm, the frame of `sim/data/handover.site.json`.

| field | meaning |
|---|---|
| `points[]` | `id` `cv-NNN`; `at` [x, y] cm on free ground of the site's grid (body clearance kept); `faces` [fx, fy] per mille, unit vector from the point to the object (the direction it shields against, where a shooter would stand); `arc` half-angle degrees; `cos` = floor(1000 × cos arc); `h` object height cm; `against` `fire` or `fire+sight` (object ≥ `eye-cm`: it blocks sight too); `q` quality per mille; `kind`; `of` the object; `present` [from, to) mission seconds, for cars |
| `blockers[]` | the cover objects at or above eye height (the fountain, the robot, the pavilion piers) with their footprints: to be added to the site's occluders |
| `refuge[]` | the crowd's refuges: entrances (`door:`) and street mouths (`mouth:`) |
| `areas[]` | the fight areas (§5.5): anchor, hostile posts, cell places, allies, and per post the cover points that shield it / that answer it |
| `rules`, `from` | the constants used, and the sha256 of the inputs (block, kit, plan, and the parts of the site the cover reads) so a stale file is seen |

### 5.2 How the generator makes it

`bun run cover`, deterministic, nothing authored. From the civic block as the world draws it (`civicMasses`: planter runs, the pavilion piers, the cabinets, the robot, the fountain, with the kit's own heights), the terrace wall's solids, the three cars the mission parks (the getaway car along its bay, the official car and the rivals' car along their kerb; present from/to the schedule), and the engine's site grid.

- **Which objects:** height ≥ `minHeightCm` (90, Motive's 0.9 m). The 0.52 m beds, tables and palms are out; foliage (cypress, topiary) is out; posts narrower than a body (`minWidthCm` 60: bollards, lamp feet) are out.
- **Where a point stands:** each face of an object is a run of edges (turn under 30°, under 6 m); a point at the middle of each run up to 4.6 m (one more each 4.6 m beyond), on the free grid cell nearest 85 cm off the face, 65–135 cm, outside every other object by a body.
- **Facing and arc:** `faces` is the unit vector from the point to the nearest point of the face; `arc` is 85% of the narrower side of the face's angular span seen from the point.
- **Quality:** `q = H × (250 + 750 × min(arc, 60) / 60) / 1000`, H = 800 (0.9–1.19 m), 900 (1.2–1.59 m), 1000 (eye height and up), floored. ≥ 600 full, 350–599 partial, below not written. So a planter run (1.1 m, 55°) is 749, a cabinet 607, a pier 612 (the others 462–512), cabinets 460–607, a car 630–663, the fountain 1000.
- **What a point protects against:** `fire` for objects under eye height (a body can see over them: they turn bullets, not sight); `fire+sight` for objects at or above it. A shooter is shielded if it is at least 5 m away and inside the cone about `faces` (§5.3).

### 5.3 What the engine does with it (the three functions)

```
protects(c, s):     d = |s − c.at|;  d ≥ 500  and  dot(s − c.at, c.faces) ≥ c.cos × d     (integers; c present at the tick)
coverModifier(p, s): best, over points c with |p − c.at| ≤ 70 and protects(c, s):   500 if q ≥ 600, 750 if q ≥ 350   (1000 if none)
selectCover(me, taken): over points c not taken, present, q ≥ 350, |me − c.at| ≤ seek:
                      with a foe:    protects(c, foe)  and  |c − foe| ≥ |me − foe| − 300
                      without one:   |c − objective| ≤ |me − objective| + 200
                      minimise |me − c.at| − c.q, then the lower id
```
`dot` is in cm·per mille and `d` an integer square root, so the test is exact. Reference: `tools/cover/select.ts`.

### 5.4 Two things the engine needs beyond the hit modifier

- **Blockers.** The file's `blockers` are not in the site's occluders today (the site holds the buildings and the plots), so a fight behind the fountain or a pier is seen through. Adding them changes sightlines, and so possibly some recorded scenario; the 28 scenarios should be run before Keel adopts them.
- **Cars.** A point of a car exists only in its `present` window; the getaway car is there for the whole mission, the official car until Vale leaves (370 s), the rivals' from their arrival (190 s). A hostile or an operative beside a car that drives off loses the point at once.

### 5.5 Fight areas and the rule

An area has an anchor (the getaway car), hostile posts (the ambush posts for the safe fight, the police posts for the loud), the cell's places (the crew), and allies. Rule **CV-2**, "every fight area has cover for both sides": at least half the hostile posts have a shielding cover point within 6 m (`hostileReachCm`, the two seconds a rival can run after a first second of aim), and at least half are answered by a cover point within 8 m of the car (`cellReachCm`) that shields the cell from a shooter at the post. **CV-1**: the file is well formed (free ground, unit facings, `cos` and `q` recomputed, blockers at eye height, names resolve, the inputs it was made from are today's). Both have failing examples in `tools/cover/cover.test.ts`.

## 6. The Handover's fights lack cover: where, and whose it is

`bun run cover` today: **CV-1 passes, CV-2 fails** (the safe fight's hostile side). 97 points: 24 on planter runs, 30 on the terrace wall, 11 on the fountain, 14 on the cars, 8 on the pavilion piers, 10 on cabinets, and almost none of it near the posts.

| area | hostile posts with cover | the cell is answered at |
|---|---|---|
| getaway-safe (rivals) | **1 of 5** (only `ambush-4`, at the café terrace's wall) | 5 of 5 |
| getaway-loud (police) | 2 of 4 (`police-3`, `police-4`: the wall) | 4 of 4 |

The posts with none, what is under each, the nearest shielding point, and what would give it (from the report; a planter run broadside to the car, clear of the keep-clear zones, checked by making the file again with it in):

| post | distance to the car | stands on | nearest shielding point | a PlanterRun would shield it at |
|---|---|---|---|---|
| ambush-1 (20, −56) | 13.9 m | **road rocket-lane** | 31 m (café wall) | (18.7, −55.1), on the road |
| ambush-2 (−4, −58) | 15.9 m | **road rocket-lane** | 16 m (café wall) | (−2.7, −57.0), on the road |
| ambush-3 (22, −42) | 14.9 m | plot records-square | 22 m (fountain) | (20.6, −42.7) |
| ambush-5 (14, −36) | 13.4 m | plot records-square | 13 m (fountain) | no free spot within 4 m |
| police-1 (−4, −50) | 12.6 m | plot records-square | 9 m (café wall) | no free spot within 4 m |
| police-2 (−8, −56) | 18.3 m | **road rocket-lane** | 13 m (café wall) | (−6.6, −55.3), on the road |

What the numbers say:

- The cell side is covered, by the getaway car and the two pavilion piers beside the bay. Thin (two of the five ambush posts are answered by a single point), and it is all there is: the block's dressing keeps a zone round the car clear of planters, lamps, bollards and trees (`crew-line-getaway`, x −2…16, y −52…−44, "the first thing the player reads and the last walk of the mission"). That zone is a deliberate Vantage and Glint choice, and it is why nothing near the car shields anyone.
- The hostile side has cover only where the café terrace's wall happens to be (`ambush-4`, `police-3`, `police-4`). The other posts stand in the open: **three on the road** (`rocket-lane`) and three on the open square.
- **Whose it is.**
  - **The three posts on the lane** (`ambush-1`, `ambush-2`, `police-2`) are the mission's to move, not the world's to furnish: a planter run in the roadway is wrong. They are the engine's own points in `tools/sim/site.ts` (Waymark is to adopt them into `mission.json`, Suture O-6). Move them onto cover that exists (the terrace wall, 9–16 m off) or onto the square. **Waymark and Keel.**
  - **The three on the square** (`ambush-3`, `ambush-5`, `police-1`) and the thin cell side are the block's furniture: **Vantage** (block layouts and dressing). One planter run near `ambush-3` and a second kind of cover near the bay's flanks, outside `crew-line-getaway`, would answer them; `ambush-5` and `police-1` stand against beds and the zone, so they want moving or a different piece.
  - **Not Datum.** The plan's roads and plots are right; Datum owns neither the furniture nor the posts. The one Datum question would be to put cover in the lane's streetscape (parked cars, planter strips) so a post on the road has some; not recommended, since it moves the fight into the street.
- Motive saw the same from the posts' own reason text ("the open square, each with a clear line to it"): the open square is by design, and his loud fight in cover (4 officers in cover, the last row of his table) is the one that costs the cell most. Cover for the hostiles is now a count: 1 post of 5 for the rivals, 2 of 4 for the police.

The test `the safe fight's ambush posts 1, 2, 3 and 5 …` locks this finding; it changes with the world.

## 7. Values

28 proposed entries in `briefs/011/pulse-fight-ai.values.json`, every one tagged [S], with its source and reason; it reuses Motive's weapon, hit, health, reaction and fight values by id (listed in `reuses`). New:

| id | v | what |
|---|---|---|
| cover-snap-cm / cover-q-full / cover-q-partial | 70 / 600 / 350 | beside a point; full and partial cover |
| mod-cover-partial | 750 | partial cover (Motive owns it; full is his `mod-cover` 500) |
| cover-retreat-cm / cover-step-closer-cm | 200 / 300 | how far back an advancer, and how far forward a fighter, may take cover |
| cover-step-gap-s / cover-leave-s | 4.0 / 2.0 | no shuffling between points |
| ai-engage-lapse-s / ai-reacquire-s / ai-stop-reach-pct | 1.0 / 3.0 / 900 | hysteresis, the warm aim that makes police suppress, and where an advancer halts |
| ai-seek-rival / police / response / ally / guard -cm | 800 / 800 / 1200 / 600 / 400 | how far each looks for cover |
| ai-stand-overflow-cm | 120 | a second body on a stand |
| rival-gone-cm / guard-leash-cm / guard-return-s / patrol-resume-s | 100 / 1500 / 10 / 15 | exits and returns |
| crowd-scatter-m / -startle-ticks / -startle-fearful-ticks / -run-cm-s / -flee-max-cm / -quiet-s / -occlude-cm | 30 / 5 / 2 / 260 / 4000 / 30 / 30 | the crowd |

## 8. Conformance vectors for Keel

`tools/cover/cover.test.ts` runs these in TypeScript; the C# must give the same.

1. `protects` with a point at (0, 0) facing (0, 1000), arc 50° (`cos` 642): a shooter at (0, 1500) yes; (1000, 1500) yes (34°); (1800, 1500) no (50.2°); (0, −1500) no; (0, 499) no (point blank); (0, 500) yes. A car point present 100–200 s: shooter at (0, 1500) gives no at 99, yes at 100, yes at 199, no at 200.
2. `coverModifier`: beside a q 700 point, shooter in the cone at 20 m: 500; beside a q 400 point: 750; from behind it: 1000; 1.2 m from it: 1000; shooter at 3 m: 1000.
3. `selectCover` on the real file (`foe` and `objective` the getaway car, nothing taken):

| body at | with the car as foe | advancing (no foe) |
|---|---|---|
| ambush-1 | none | cv-084 |
| ambush-2 | none | none |
| ambush-3 | none | cv-087 |
| ambush-4 | **cv-044** (café wall, q 800) | cv-046 |
| ambush-5 | none | cv-021 |
| police-1 | none (12 m seek: cv-044) | none |
| police-2 | none | cv-082 |
| police-3 | **cv-047** (wall, q 800) | cv-094 |
| police-4 | **cv-042** (wall, q 600) | cv-043 |
| guard-ally | none | cv-085 |

4. The cell's cover at the car (always present): `cv-001…006` (the getaway car: its two ends and sides, q 630–663) and `cv-090…097` (the pavilion piers, q 462–612).
5. Determinism: two `bun run cover` runs give the same bytes; a fight run twice gives the same checksums.

## 9. Open items

- **Motive:** `mod-cover-partial` 750 and the classes' thresholds (`cover-q-full` 600); whether police should take the Enforcer as class 1 (his `OFFICER_PRI`); the rivals' break rule counts the whole crew (`n`), including rivals not yet present.
- **Keel:** (a) load `suborned.cover/1` (the schema is strict: unknown = reject), either reading the contract or a byte copy that `sim:site` writes to `sim/data/handover.cover.json`, held fresh by a test as the people file is; (b) add `blockers` to the occluders and re-run the scenarios; (c) the crowd's occlusion of watchers needs a position grid; (d) the response unit's join and the stand overflow; (e) the new events I would log: `ai.alerted`, `ai.cover` (point id), `ai.target` (class), `ai.break`, `ai.gone`, `crowd.startled`, `crowd.sheltered`, `crowd.witness` (count).
- **Waymark:** the six posts (three on the road); `security-entry` is called the west street in §07 and the north street in the engine's notes.
- **Vantage:** the furniture on `records-square` near `ambush-3`, `ambush-5`, `police-1`, and whether `crew-line-getaway` should still bar every kind of cover.
- **Latch and Glyph:** cover points are worth showing (a low chevron on the ground, `faces` as its direction); an order "take cover" for the cell that sends an operative to `selectCover` with the nearest foe is the player's version of §2.3.
- **Stale file:** the cover file hashes the parts of `handover.site.json` it reads (grid, places, schedule, fight references). If Keel changes the posts or the grid, `bun run cover` writes it again and `bun test tools/cover` says so until then.
