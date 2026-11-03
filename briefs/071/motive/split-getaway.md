# 071 · Motive: the split getaway on the pickup way (two cars)

**Status:** a proposal for Sky. Nothing is built, and no GDD, contract or engine file is edited.
**Sky's idea (2026-10-26):** "could have enforcer drive off if the driver is turned and taken Vale to the service entry, then rest of team have to getaway in the other car." Sky said this while answering whether the service yard's exit lane should start at the car.
**Picture:** `briefs/071/motive/split-getaway.png`. It shows the service bay, both cars and their routes, drawn from the site data (`split_getaway.py`; the numbers are in `measures.json`).
**Probe:** engine at HEAD (82eaae77) on a scratch copy, plus one switch (`probe/engine-probe.patch`). With the switch off it replays c038 exactly: route-b-board 7:35.4, route-b 7:32.2, route-a-board 4:15.5. ("c038" is the current mission-check record; route-b-board is the pickup way today, with the cell's car brought round.)

---

## Summary for Sky (one page)

**The idea.** Today the pickup way and the front-door way end the same way. The Enforcer carries Vale into the cell's car, the rivals' ambush springs at that car, someone downs their road-blocker, everyone boards, and the car leaves. With your idea, the pickup way gets an ending of its own: Vale leaves quietly in his own car, and the fight is the other three getting out without their heavy.

**What happens in play, step by step**
1. As today, the Fixer turns the driver (pay or help), the Scout sends the rival car away with a false tip, and the Tech sends the patrol away from the yard.
2. Vale gets into his car at the front at 6:10. The turned driver brings him round to the service bay, out of every camera's view, and parks at 6:26.
3. The Enforcer, waiting by the service door, takes Vale in the back seat: 38 s, with nothing anyone can see. Done at 7:04 on the engine.
4. The driver steps out and the Enforcer takes the wheel. Vale's car is now the cell's to order.
5. Meanwhile the Scout, Fixer and Tech walk back to their own car in the square. Three rivals have stood posted round it from about 3:00. The cell sees them the whole time.
6. **The commander picks the moment.** Go on Vale's car: it drives down the service drive and east along Apron Road (48 m, 14 s) and off the map. **Vale is out, and the mission is won.**
7. The posted rivals see the prize gone and spring on the cell's car. The mission carries on: three pistols against three rivals, with no Enforcer. They can down the blocker, board and go, or break the crew and go.
8. The debrief counts what the rearguard cost: anyone captured, the car lost or seen.

**What the engine says** (the probe; the rival car tipped away, the lookout warned off, the guard on the cell's side):

| how the three play the rearguard | blocker down | their car leaves | hurt | Standing |
|---|---|---|---|---|
| at their car, all three fire on the blocker | 2.1 s | **4.2 s** (board and go) or 5.8 s (break the crew) | 1 hit, nobody down | +2 / +3 |
| at their car, each fires at will | 6.5 s | the Scout is down 0.5 s later | carry him in, or he is captured | +3 if they break it |
| walking in from the cabinet together | 8.0 s | 10.3 s | the Fixer down, captured | +2 |
| arriving one at a time | 24.4 s | – | the Tech, then the Scout, down | – |

Vale is out at about **7:05, 30 s sooner than today's 7:35**. With the rival car's return at 7:44, that leaves 39 s of margin instead of 9 s.

**My recommendation: Option A.** It gives the pickup way its own shape: a quiet extraction in the city's own car, and a fight that tests the three without their heavy. It keeps your "the fight always comes", and it is built from rules any mission with a turned driver can use.

**The choice** (each option's cost is rough; see §7):

| | what it is | cost |
|---|---|---|
| **A · Two cars, two Go's (my pick)** | As above. The win is fixed when Vale's car leaves, and the mission runs on until the others are out or left. | Engine: large (two of Keel's rounds; every getaway rule is written today for one car). Mission: medium. Host: medium. |
| B · Vale's car only, one Go | The Enforcer drives Vale off and the mission ends there. The other three walk out if the street is calm, and the cell's car is left behind (lost, the investigation gets it). | Engine: medium. Mission and host: small to medium. **But the pickup way has no fight**, which goes against your 2026-10-25 answer "no, it's to demonstrate the features". Restoring the fight puts it back at Vale's car in the yard: today's yard fight without the 3.5 m carry, so not much more distinct. |
| C · Keep today | The cell's car is brought round behind Vale's, and Vale is carried 3.5 m into it. On the question you were answering, my answer is yes: the yard's exit lane should start at the car. | Engine: small (the lane in parts, already specified). Mission: small. Nothing new for the player. |

**Two questions for you**
1. A, B or C? (My pick is A.)
2. Under A, if the three never go back for their car, the rule we already have captures them while the rivals stand. Is "captured" right for agents the rivals pin down? The alternative is that they walk out and only the car is lost. That would be kinder, but then the fight can be skipped.

---

## Detail

### 1 · Driving: who takes Vale's car, and what the rest do

**The Enforcer drives (pick).** This needs no new kind of actor. The rules already say Go needs a standing agent in the car, allies never board, and only the Enforcer handles Vale. The Take from the back seat ends with the Enforcer at the wheel and Vale aboard. The turned driver steps out as part of the Take, at no extra time, and walks off on foot.
- The driver is "turned for this very act", so he is never its witness and never reports it (the first row of who reports what, GDD §05).
- He is questioned later as today. The split makes him safer for the cell: today he sees the cell's car park behind him and can give it up if he ever talks. With the split he never sees it.
- His Talker test is unchanged. Paid or helped, he has loyalty 1, fear 0 and resentment 0, so he stays silent.

**The turned driver drives with the Enforcer aboard (not now).** This is the bravery ladder's Accomplice step ("hides op, drives", §04): a contact who has done jobs unexposed. A driver turned this mission is at the bottom of that ladder, so the rules don't allow it in the Handover, which is the campaign's first mission. It is a good campaign rule for later: any contact at Accomplice can drive for the cell. It would also cost him: he goes missing with the councillor, his ties worry, and he becomes a fugitive the cell keeps.

**The rest of the cell.** In the split, only the Enforcer is needed in the yard.
- The Scout's jobs end with the false tip (3:54), the Fixer's with the turn (1:52), the Tech's with the patrol reroute (it lands at 5:45).
- From then on they can gather at their own car. They stand among the posted rivals before the fight, as route A's three already do, and nobody reported them in any probe run (Alert 0).
- The walks back: the Tech from his cabinet 40 m (22 s); the Fixer from the driver about 79 m (44 s).
- Bringing the cell's car round (the Drive order, 121 m, 28 s) is no longer needed.

### 2 · Success: what counts

- **Win = Vale out alive in a cell car.** It is fixed the tick Vale's car leaves the map. Today's win line ("Vale in the getaway car when it leaves") becomes "Vale in a car of the cell's when it leaves".
- **The mission runs on after Vale is out.** It ends when no agent is left standing on the map (each gone in a car, or captured), or when the player ends it.
  - Once Vale is out, the menu's Withdraw becomes **End**. Today Withdraw is a free abort that captures nobody. After the win it can't stay free, or it would skip the rearguard.
- **Must all four get away? No.** Today already: in route A the Enforcer leaves with Vale, and the other three "walk out" because the street is calm. The left-behind rule stays as written: at the end, a downed agent not in a car is captured; a standing one is captured if a hostile is still up and not broken, or if the seal is running; otherwise he walks out free.
- **Fails after Vale is out become captures.** While Vale is on the map the fails stand as today: Vale killed, Vale leaves in his own driver's hands, rivals reach Vale, district seals, the Enforcer down before Vale is in a car, the whole cell down. Once Vale is out, the seal and the cell being down capture whoever is left, and the mission is still won.
- **If the second car is lost:**
  - **The cell's car is still standing on the map at the end.** It is lost: the rivals or the police have it. I price it as a car seen leaving (+1 Trail, the investigation hunts it next mission) because it is the same evidence. Next mission the cell needs a new car through a contact.
  - **The three are captured.** +4 Trail each, held at the district's police post, a rescue open for 3 missions, and questioned between missions (the Talker clock: a captive taken down holds out 2 missions, one abandoned standing 1). A captive who talks gives up every face, the car, the place and time, and every turned contact.
  - A win with three captured is a real result: Trail 4 + 12 = 16, which is the "infamous" band next mission.

### 3 · Both exit lanes, the rivals' blocker, the police, and M059-B10

**Two cars, two lanes, held separately.**
- Each car keeps its own hold: a hostile within 8 m of it, or on its exit lane, the first 30 m of its route out.
- Each car has its own blocker: the one hostile who goes for that car's road.
- The rule that "the fight always comes" (Sky 2026-10-25: Go is refused once the final fight has started, until its blocker is down) becomes per car:
  - **Go is refused on the car the fight is at, until its blocker is out or the fight is over.** Today's "with Vale aboard" is dropped.
  - Otherwise the three could sit in their car and leave the tick the ambush springs, before the blocker reaches the road.
- Vale's car in the yard isn't held by a fight 75 m away.

**The rivals.**
- **The new spring rule.** Today the posted three spring "as Vale comes within 20 m" of the cell's car. Add: **or the moment Vale leaves in another car of the cell's.** It happens that same tick, before any other Go.
  - It doesn't change a one-car route: in A and G, Vale still comes to the cell's car.
  - On the probe, with the three waiting at their car, the spring is the tick Vale goes out (7:03.9). The blocker is rival-2 every time, and he posts at the planter 11 m up Rocket Lane, as today.
- **The rival car.** It is tipped away until 7:44, and the lookout is warned off. If the rival car is back before Vale's car leaves, its wheelman comes for Vale's car, a cell car with Vale in it. Today's rules then apply to it unchanged: rivals reach Vale if no agent stands within 6 m, and hiding in a held car loses.
- **The floor of 3 rivals in the fight is met** by the three posted, so nobody is called back.

**The police (a loud run).**
- The responders come from the entry nearest the car with Vale. That is the Office's official entrance for the yard (Sky's 2026-10-25 answer).
- The Enforcer alone in the yard against them loses, as loud in the yard already does on today's engine (the Enforcer is downed at 7:42.5 in Motive's 059 balance pass).
- So the loud pickup stays a loss, and quiet still beats loud.

**Seen leaving.**
- A car's departure is charged only if a police officer, a robot or an engaged patrol has it in sight.
- Vale's car leaves down the drive and east along Apron Road, away from the pickup camera (25.7 m from the drive's mouth against an 18 m reach), with the patrol rerouted. Cameras don't count for this rule anyway. So it is unseen: 0 Trail.
- If it were seen: +1 Trail and a lead for the investigation. Nobody hunts it next mission, because it was never the cell's car.

**M059-B10: still needed, but it changes and is no longer urgent.**
- What B10 is: Motive's ruling that a getaway car standing on a drive starts its exit lane at the car and follows the drive at the drive's width, then the road. Today's §06 letter starts the lane on the first carriageway, so in the yard the lane begins 16.65 m from the cell's car. The blocker's post then lands 19.4 m out, beyond a pistol's 16 m.
- Under A, Vale's car stands in the service bay, further in. On today's letter its lane would start **22.25 m from the car** (measured). Any fight that reaches the yard (loud, or the wheelman back) would put a blocker where the Enforcer, sitting in the car, can't answer him. That is the same fault, made worse.
- So the clause stays and becomes general, for every car stand on a drive, Vale's bay included: 22.25 m of drive at 3 m wide, then 7.75 m of Apron Road; the post is 10 m down the drive.
- It is no longer urgent. B10's cost was route-b-board's slack falling to 2 s at the yard. Under A, no reference route fights at the yard, so that cost is moot.
- The yard stand for the cell's car and the Drive order stay as rules any mission can use. A player who brings the car round can still win the one-car way.
- **One new catch with two cars in the yard.** The yard stand is 1 m behind Vale's car, and the drive is one car wide. If the cell's car is brought round, Vale's car is boxed in until the cell's car leaves first.
  - The engine has no car-on-car blocking today; Pulse noted cars don't collide.
  - Needed: a standing car on another car's way out refuses that car's Go, naming it.

### 4 · Prices: the split against today's one-car getaway

The engine figures are for a quiet, prepared route B: the driver paid ($70,000, +2 Trail), the false tip (+1), the reroute (+1).

| | Trail | Alert | Standing | Heat next mission |
|---|---|---|---|---|
| today, route-b-board (car brought round, fight at the yard) | 4 | 0 | +3 | 4 (noticed) |
| split, focus fire, board and go | 4 | 0 | +2 | 4 (noticed) |
| split, focus fire, break the crew | 4 | 0 | +3 | 4 (noticed) |
| split, at will, the downed Scout left behind | 8 | 0 | +3 | 8 (wanted) |
| split, three captured, car lost | 4 + 12 + 1 = 17 | – | +2 | 17 (infamous) |

- **No new price on the driver.** Pay stays $70,000 · 50 s · +2 Trail, and Help $40,000 · 50 s · +1. He was already turned to deliver Vale to the cell. Lending the wheel is the same act, and the purse logic holds: Help on Inez plus the driver ($80,000) fits in $100,000; Pay plus the driver ($110,000) does not.
- **Lend:** $0, no extra time (it is part of the Take), 0 Trail.
- **Go on Vale's car:** $0, 0 Trail, no Alert, as any Go. It costs +1 Trail only if seen leaving.
- **The cell's car lost:** +1 Trail and hunted next mission. This is the same as being seen leaving, so it reads as one rule: "the car is known".
- **Captured agent:** +4 Trail, the custody and Talker clocks, as today. The split makes this likelier for careless play, because nobody with 6 health is absorbing fire.
- **Standing.** No public fight in the yard. The square is busy, so the rearguard is a public fight judged as today: breaking feared rivals +1, board and go 0.
- **Talkers.** The driver knows one fact fewer (the cell's car). Nothing else moves.

### 5 · Board and go, or fight it out (§06)

Today the choice is at the one car. Board and go means fewer seconds under fire and Standing 0. Fight it out means breaking the crew for +1 against feared rivals.

Under A it moves to the rearguard, and it gets sharper:
- With focus fire, board and go leaves at 4.2 s and the break comes at 5.8 s. Both are cheap.
- At will, the blocker is down at 6.5 s but the Scout is down 0.5 s later. Fighting it out then means carrying him in or leaving him.
- Today the Enforcer (6 health, carbine) is in the fight and the rivals aim at whoever holds Vale. Without him the three lightweights (4 health, untrained pistols) take every shot.

At Vale's car there is a different choice, a commander's one: **when to send the Enforcer.** Go too early, before the three are at their car, and the rivals spring on an empty car. The three then walk into a sprung ambush, as in the "walking in" and "one at a time" rows. Go when they are in place, and the fight opens at point-blank range on the cell's terms.

### 6 · Why it is better, and where it could break the pillars

**More fun and more distinct.**
- **Two endings, not one.**
  - Front door: protect the carrier. The Enforcer walks Vale through the ambush while the three cover him.
  - Pickup: a clean switch, then a rearguard without the heavy.
  - The same rivals and the same car, but different jobs.
- **Orchestrate.** Vale's Go is a window the player sees coming and times: "Are the three at the car? Go."
- **The fantasy.** The city's own car, with its own paperwork, carries Vale off. A turned driver lends more than a lift.
- **Less chore.** No Tech boarding at 6:25 to drive 121 m round, no one-car-wide drive to keep clear, no 3.5 m carry. Vale is out 30 s sooner.
- **Subvert people.** Knowing the driver makes him give more (the car) and know less (not the cell's car). That is the pillar's test passing.

**The risks.**
- **Readable > rich.** A win that comes before the mission ends is new, and so is a second car with its own hold.
  - It needs a clear beat ("Vale is out. Get the others out.") and a per-car "why won't it go".
  - The capture rule for agents far from the rivals is an abstraction (see question 2).
  - Test T11 (the getaway reads: "why won't the car go?", "who did you leave?") needs re-asking with both cars.
- **People > powers.** It passes: the car comes from a person the cell turned, not a power.
- **Systems > scripts.** Every piece is a general rule, and nothing names the Handover:
  - the lend: a turned driver's car with the target aboard;
  - the per-car hold, gate and blocker;
  - the spring when the target leaves by another car;
  - the win at the target's car;
  - the run-on and End;
  - the lost car.

  Any generated mission with a target driven by a cast driver gets the split. The Handover only supplies data: the bay and its route out, Apron Road east, which already exists as the mission's `getaway-service` route.
- **Preparation > improvisation.** It passes: the split rewards placing the three before the Go.
- **"Fights measure preparation"**, but harshly at the edge. Arriving one at a time loses two agents. That fits the rule, but Sky's note on build 0.2.122 was that the game may be too punishing. If Suture's play shows the rearguard too sharp, the soft lever is the guard ally, who already fights the rivals here, and his placement. I would not soften the rivals.
- **The Enforcer's carbine never fires on route B.** Route A still shows it, so the Handover still shows every feature.

### 7 · Changes and their sizes (Option A)

**Keel, the engine: large** (about two rounds, the size of the rival-car round of 2026-10-25).
1. **Cars as a list.** The cell's car plus lent cars, each with:
   - its stand, seats, who is aboard and its exit lane;
   - its hold, blocker and gate;
   - seen-leaving and its Go.

   Today these are singletons: `Car`, `CurStand`, `GetawaySeat`, `CarLeftTick`, `CarSeenBy` and the InCar flags (GameGetaway, GameDriveCar, parts of GameFight and GameCrew).
2. **The lend.** A Take in a turned driver's car (today's in-car Take) ends with the Enforcer at the wheel. The driver steps out and walks off. The car becomes a cell car at a stand built from its bay and the mission's route out.
3. **Two Go's.** The target's car leaving logs `vale.out` and fixes the win. The mission runs on until no agent is on the map, or End. After `vale.out`, the seal and the cell being down capture instead of failing. Withdraw becomes End, judged by the left-behind rule.
4. **The spring.** The posted crew springs when the target leaves by another car (that tick, before other Go's), as well as on today's 20 m.
5. **The gate per car.** Go is refused on the car the fight is at until its blocker is out or the fight is over.
6. **The lost car.** A cell car still standing at the end: +1 Trail, hunted (a debrief row).
7. **Boxed in.** A standing car on another car's way out refuses its Go, naming it.
8. **B10's lane in parts** for every drive stand (as specified in brief 059).
9. **Debrief, hash and scenarios.** Rows per car, the state hashed, and about 12 scenarios: SPLIT-LEND, -WIN-FIXED, -SPRING-ON-LEAVE, -GATE-PER-CAR, -LOST-CAR, -BOXED, -AFTER-WIN-SEAL, -END, and twins with the switch off. Built as a rule value (`getaway.split` 0/1), so Sky can turn it off.

**Waymark, the mission: medium.**
- mission.json: the service bay as a lend stand (route out: `getaway-service`), with its spots checked by Vantage.
- §07's route B, win, fail and schedule lines (Aster writes them into a new GDD version).
- New plans:
  - route-b-split (focus fire, board and go);
  - route-b-split-out (break the crew);
  - careless-split-early-go (Go before the three are placed);
  - careless-split-at-will.
- Keep route-b-board (the one-car way) as a choice.
- New mission checks: every split win has `vale.out` before the last Go, and no reference split route captures anyone.
- Re-record all routes.

**Latch, the host: medium.**
- Vale's car becomes orderable when lent: its card, ring and Go, and its own "held by" state.
- Two car chips.
- The "Vale is out" beat, with the clock running on and a new goal: get the others out.
- End in place of Withdraw after the win.
- Per-car refusals that name the holders.
- The debrief per car.
- No new asset: Vale's car already has a view (`car/official`).

**Also.**
- Fable: the words (Go on Vale's car, Vale's out, the rivals turn on your car, End, the debrief lines).
- Glyph: the second goal and the car marks.
- Pulse: the driver's walk-off and the spring.
- Suture: T11 with both cars, and whether the rearguard is too sharp.
- Motive: the rulings file, once Sky picks.

**Option B:** engine medium (items 1–2, the lent car's Go ending the mission, the lost car); mission small to medium; host small to medium. Plus a fight rule at the yard if Sky wants the fight kept.
**Option C:** engine small (item 8 only); mission small (re-compose route-b-board, about 2 s of slack).

### 8 · The probe, and what it doesn't show

- **Switch `PROBE_SPLIT=1`** (`probe/engine-probe.patch`, 37 lines). At the in-car Take, it takes the Enforcer and Vale off the street (standing in for Vale's car leaving). It then springs the posted crew on the first agent within 20 m of the cell's car.
- **Plans** (`probe/gen.py`) are built from HEAD's route-b-board up to the Take. The cell's car is never driven round, and the Take starts as soon as Vale's car parks.
- **To rerun:** copy `probe/` into `out/scratch/motive-split/`, extract HEAD's tree into `tree/` and apply the patch. Then run `PROBE_SPLIT=1 TAG=x bun run guard -- bun out/scratch/motive-split/probe.ts <plan>`.
- **Logs** for every row of the summary table are in `probe/logs/`. Each run's own name for its result:

| plan (probe/plans) | how the three come | Take done | spring | blocker out | the three's car leaves | hurt / left |
|---|---|---|---|---|---|---|
| split-bg-pff | at the car, focus fire, board and go | 7:03.9 | 7:03.9 | 7:06.0 | 7:08.1 | 1 hit |
| split-fo-pff | at the car, focus fire, break the crew | 7:03.9 | 7:03.9 | 7:06.0 | 7:09.7 (broken 7:07.5) | 1 hit, +1 Standing |
| split-bg-p / fo-p | at the car, at will | 7:03.9 | 7:03.9 | 7:10.4 | – (Scout down 7:10.9) | 7 hits |
| split-bg-c | walk in from the cabinet together, at will | 7:03.9 | 7:14.9 | 7:22.9 | 7:25.2 | Fixer down, captured |
| split-bg-y | the Tech first, the Scout and Fixer later from the yard | 7:03.9 | 7:14.9 | 7:39.3 | – | Tech and Scout down |
| route-b-board (today, for reference) | car brought round, fight at the yard | 7:23.0 | 7:23.0 | 7:32.1 | 7:35.4 | 6 hits, +1 Standing |

**Caveats.**
- The engine can't make two cars, so the three's Go is logged as "withdrawn" (car left without Vale). In the proposal that is the rearguard getting out.
- "The Enforcer captured" in two runs is a probe artefact: he is held off-map, not really left.
- In the "at the car" runs, the spring at the first agent within 20 m equals the proposed spring as Vale's car pulls away, because the three are already there.
- The proposed same-tick order (Vale's Go, then the spring, then any other Go) and the per-car gate aren't in the probe.
- Times are on one deterministic replay per plan. A player's own placement will vary.

**Next owner:** Aster, to put the A/B/C question and question 2 to Sky. Then, if A: Aster writes GDD §06, §07 and §08 into a new version, and Motive writes the rulings file. Keel builds behind `getaway.split`, then Waymark, Latch, Fable and Glyph, and Suture plays it.
