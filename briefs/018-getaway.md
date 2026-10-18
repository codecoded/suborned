# 018 · The getaway

**Status (2026-10-18):** approved by Sky (2026-10-18, "ok with the getaway suggestions"); Keel-ready. See §9 for the answers. Author: Motive (systems). Asked by Aster after Sky played the Handover: they got Vale into the getaway car mid-fight, "but then nothing happened". Sky chose option 3: "I want to get my team into the car, and get away. But it's interesting, because the enforcer can't run, so they have to support the enforcer." No code or data has been edited.

## Summary

- **Only what is near the car holds it.** A combatant holds the car if it stands within **8 m** of the car or on the **exit lane**: the first **30 m** of the getaway route's carriageway ahead of the car. The ring is 25 m today, which takes in the whole fight. The ambush and police stops stand 12–19 m away, so they no longer hold the car just by standing there.
- **One hostile per fight goes for the road (the blocker).** Without that, nothing would ever hold the car. Down the blocker, or drive the crew off, and the car is free. The job does not pass to the next hostile, so you never have to kill them all.
- **Board, then Go.**
  - **Board** is an order on the car, given within 1.5 m of it. It takes **1 s**, or **4 s** if the op is carrying someone (Vale or a downed op go in first).
  - **Go** is an order on the car once anyone is in it. It is refused while the car is held, and the refusal names the holders. The car leaves at once with whoever is inside.
  - The mission is won when **Vale is in the car when it leaves**.
- **Boarding is the fight.** The Enforcer walks with Vale and can't run or fire. Hostiles shoot whoever last shot at them, then whoever holds Vale. So the other three keep him alive by drawing fire, and they board last.
  - An op in the car doesn't fire and can still be hit, as a target beside cover (×500). Hiding in a held car loses.
  - Boarding early leaves the Enforcer alone.
- **Left behind**, at the tick the car leaves:
  - A downed op is **captured**.
  - A standing op is **captured** if the fight is still on or the district's seal clock is running.
  - Otherwise a standing op **walks out** and is back next mission.
- **What a capture costs the campaign:**
  - **+4 T** per op captured.
  - The op is held in the district's police post. That opens a **rescue** objective for up to 3 missions; after that the op is moved out of reach.
  - Between missions the captive is **questioned with the same Talker test as anyone else**. Custody adds fear and wears down loyalty. An op left standing (abandoned while still fighting) resents it and loses loyalty faster.
  - With the training cell's starting values, an op captured while downed holds out for **2 missions** and an abandoned one for **1**.
  - When a captive talks, the investigators get every op's face, the cell's car, the place and time, and **every contact the cell has turned**. They then question those contacts, link by link.
- **Leaving under the eyes of authority costs +1 T** and gives the investigation the cell's car. This applies when a police officer, robot or the patrol is still up and sees the car leave. Rivals don't report.
- **Standing:** the final fight is judged when the car leaves, which the engine already does for a fight still open when the mission ends.
  - Leave before the feared rivals break and the fight moves **0**: that is the one-step price of running.
  - Firing on police is −1, whenever you leave.
- **The Handover:**
  - Every quiet route can now board and go once the blocker is down. That is faster and safer, at one Standing step.
  - Fighting it out still earns the +1.
  - Quiet still beats loud on Trail (3–7 against 14), Alert, Standing and the seal.

## 1. The hold

`CarHeld()` returns true while any **holder** stands. It should return the holders' ids, because the HUD names them.

A **holder** is a combatant (§06: a final-fight hostile, or anyone who has fired on or aims at the cell: rivals, officers, the response's robots, an engaged patrol). It must not be down, broken or gone. It must also be either:

- **near:** within `fight-hold-m` (8 m) of the car's point, on any level and with or without a line of sight; or
- **on the exit lane:** standing on the street level inside the exit lane.

An ambusher lying in wait counts as a combatant (as today), but its spot holds the car only if it lies within 8 m or on the lane. On the Handover none does.

**The exit lane is derived from the getaway route, not authored.** Take the mission's `routes.getaway` (plan graph). Project the car's bay point onto the route's first road, then walk the route forward `fight-block-m` (30 m). The lane is that stretch of carriageway at the plan road's own width `w` (Rocket Lane: 12 m).

- The site exporter writes it into `handover.site.json` as `getaway.exit`: a polyline in the site frame plus a width, with src to the route and the road.
- The engine reads only `getaway.exit`. Every generated mission gets its lane the same way.
- If the plan gives no route, the site build fails (unknown = reject).

**The blocker** (Pulse owns the behaviour; these are the numbers):

- `fight-blockers` (1) hostiles per fight take the job of the road.
  - In the safe branch, the blocker is the ambusher with the shortest grid walk to the lane, and it moves when the ambush springs.
  - In the loud branch, it is the first officer to arrive, who takes the lane instead of a police stop.
  - Ties go by id.
- The blocker goes to the nearest free cover point inside the lane. If there is none, it stands in the lane in the open.
- Downed, broken or gone, the blocker is not replaced.
- A hostile that wanders within 8 m holds the car only while it is there.

**Fail rule:** a rival within 1.5 m of the car while Vale is in it, with no op standing within 6 m of the car, reaches Vale. The mission fails (`rivals-reach-vale`, as today).

## 2. Boarding and Go

| order | who | needs | time | effect |
| --- | --- | --- | --- | --- |
| **Board** (target: the car) | any op, not downed | within `at-reach` 1.5 m of the car; not running, not tethered, not mid-action | `board-s` 1 s; carrying, +`carry-s` 3 s, the carried first | the op, and whoever they carry, are in the car: off the street, no fire. Only the Enforcer carries Vale, so only he loads Vale |
| **move** on a boarded op | the op | — | `board-s` 1 s to step out, then the move | out at the car's side; Vale stays in |
| **Go** (target: the car) | anyone, given from the car's card | someone in the car; the car not held (refusal `car-held`, with the holders' ids) | instant | the car leaves; the mission ends at this tick; left-behind resolves (§3) |

- **The win:** "Vale alive at the getaway car" becomes "Vale in the getaway car when it leaves". `extract` retires into Board, for the Enforcer carrying Vale, plus Go.
- **Go without Vale** is the cell withdrawing by car. The outcome is Withdrawn and the left-behind rules apply. The menu's Withdraw is unchanged (open question 4).
- **In the car:**
  - An op in the car is a target at "still beside cover" (×500, already in `weapons.mod`) and doesn't fire.
  - Vale in the car is never a target (§06).
  - The car seats the cell and the target. Allies (the guard, joiners) never board.
- **Enforcer down** fails the mission only while Vale is not yet in the car. Once Vale is in, the Enforcer is any op: carry him in, or he is left behind.
- **No auto-board.** Standing at the car isn't boarding, because an op at the car may be there to fight. With the whole cell selected, a right click on the car boards each op in turn.

## 3. Left behind

### In the engine, at the tick the car leaves

| state at Go | fate | why (debrief field) |
| --- | --- | --- |
| downed, not in the car (carried or not) | captured | `downed` |
| standing; a combatant still up and not broken, or the seal clock running (`Lockdown` set) | captured | `fighting` / `sealed` |
| standing; the fight over and no seal clock | walks out, back next mission | `walked` |

- `WouldBeCaptured()` becomes `LeftBehind()`. It returns (op, fate, why) for every op not in the car. It is the Go card's preview and the H11 warning.
- `Captures()` runs at Go, and only at Go. A failed mission (seal, Vale killed) keeps today's behaviour; the training mission replays.

### In the campaign, worked at the debrief (no campaign layer runs yet)

- **Trail:** +`captured-trail` (4) per captured op. A captive is as much evidence as a knockout victim's statement (also +4).
- **Out of play:** a captured op is unavailable until rescued.
  - The op is held at the district's police post, a place on the plan.
  - A **rescue** objective is offered there for `custody-missions` (3) missions. It is the Handover's shape with the captive as the target.
  - After that the op is moved out of the district and the objective lapses (open question 2).
  - Wounds carry over, as §08 says.
- **Questioning:** once in each between-missions phase that the op is held, in this order:
  1. fear +`custody-fear` (1);
  2. loyalty −`custody-loyalty` (1);
  3. the Talker test (fear + resentment > bravery + loyalty).
  - At capture, an op left standing takes resentment +`abandoned-resentment` (1) and loyalty −`abandoned-loyalty` (1). They watched the car go.
  - Every value is clamped to its scale.
  - Talking is once and for good; a later rescue doesn't take back what was said.
- **Ops' own scales:** recruits keep the scales they were turned with. The founding (training) cell starts at bravery 2 (accomplice), loyalty 3 (sworn), fear 0, resentment 0. Your own people answer to the same rule as everyone else: a recruit you pressured talks first.

Talk timelines with the training cell's starting values (talks at the questioning marked):

| captured as | q1 (fear + res vs brav + loy) | q2 | q3 | holds |
| --- | --- | --- | --- | --- |
| downed | 1 + 0 vs 2 + 2 | 2 + 0 vs 2 + 1 | **3 + 0 > 2 + 0** | 2 missions |
| left standing | 1 + 1 vs 2 + 1 | **2 + 1 > 2 + 0** | — | 1 mission |
| a recruit turned with Help (bravery 1, loyalty 1), downed | 1 vs 1 | **2 > 1** | — | 1 mission |

- **What a captive gives:** the facts a cell member holds, as Talker facts:
  - `cell-face:<op>` for every op (today's `fixer-face` and `enforcer-face` generalise);
  - `cell-car`;
  - `place-time` for the mission;
  - **`contact:<person>`** for every turned contact.
- Each fact becomes a lead (§05 output). A contact lead sends the investigators to that person next mission, where that person's own Talker test applies. That is §02's "the city investigates backward, link by link", started by your own op.
- `people.rules talker.facts` gains `cell-face` and `contact`. That is Keel's vocabulary; Motive writes the entries.
- **Heat** = T + Menace, unchanged: a capture adds 4 T to Heat.

**Seen leaving:** if at Go a police officer, a robot or an engaged patrol (security, not rivals) is up and has the car within its sight (its cone, or the 13 m witness range on a clear line), the getaway is one witnessed incident: +`witness-trail` (1) T, and the fact `cell-car` (place and time) goes to the investigation.

- The final fight's shots stay untraced. This charges the departure, not the shots.
- Rivals don't report to police.

### The debrief: new lines, fields only (Fable writes the words, Glyph lays them out)

| line | fields | src |
| --- | --- | --- |
| `left-behind` | who, fate (captured / walked), why (downed / fighting / sealed / walked) | this brief §3 |
| `captive` | who, held-at, holds (missions until the Talker test passes, projected), rescue-until | this brief §3 |
| `car-seen` | by, trail | this brief §3 |
| `give-up` (existing) | who = the captive, facts, left, right | people.rules talker; Motive K7 |

## 4. Standing (fight.public) when the cell drives off

No rule changes; the departure is when the judging happens.

- The final fight is one fight from its first shot to its end or the mission's (fight.public `how`). Go ends the mission, so the fight is judged at the tick the car leaves, by the first outcome that holds.
- Feared rivals:
  - broken or all down before Go: **+1**, `seen-fight-rivals`;
  - not broken when the car leaves: **0** (`none`; the `how` text already names a withdrawal).
- Firing on police at any point in the fight: **−1**.
- Firing first on rivals the public doesn't fear: **−1**.
- A capture moves nothing. The public judges the fight, not who was left.
- Keel adds one scenario: route A, board and go with the crew unbroken → Standing 0 from the fight. It is the twin of N95.

## 5. The Handover

- Nothing before the car changes: routes, prices, timings, the fight's size, Inez's warning, the guard ally, −1 for a turned driver and −1 for a false tip.
- At the car, every route now chooses between:
  - **board and go:** down the blocker, board, Go. Fight Standing 0, and fewer seconds under fire;
  - **fight it out:** break the crew (more than half down), then go for +1.
- Expected Standing, start +2; the arrow shows Standing after the word:

| route (c015) | fight it out | board and go |
| --- | --- | --- |
| A Help ×2 | +3 → +4 (Hero) | +2 → +3 (Hero) |
| A Pressure | +2 → +1 | +1 → 0 |
| B driver paid (3 rivals) | +3 | +2 |
| G | +3 → +2 | +2 → +1 |
| loud | 0 (c015) | 0, and +1 T if seen leaving |

- **Loud:**
  - 2–4 trained officers come in from the north street; they hold and never break. The first to arrive takes the lane.
  - The seal clock runs, so anyone left standing is captured.
  - Leaving costs +1 T if an officer sees it.
- **Quiet still beats loud:**

| | quiet routes | loud |
| --- | --- | --- |
| Trail | 3–7 T | about 14 T (13 + seen leaving) |
| Alert | ≤ 1 | 2 |
| Standing | at worst neutral, up to Hero | 0 (c015) |
| hostiles | untrained rivals who break at half; preparation removes two and warns 10 s early | trained police who hold |
| left-behind fate | can walk out | anyone left is captured |

  The new rule shortens both kinds of fight, but loud's costs are paid before the fight.
- **Waymark re-records after Keel builds:**
  - for each route, a board-and-go reference and a fight-it-out reference;
  - one run that leaves a downed op (captured, +4 T) and one that leaves a standing op mid-fight (captured, abandoned);
  - checks that no reference is held by a stop it merely stands at;
  - then the c016 table and §07's reference results.

## 6. Readability (Glyph specs the look; Fable the words)

Show it on the street, one line per order, no new screens:

1. **The car's reach**, drawn only during the final fight: the 8 m ring and the 30 m exit lane, both on the ground. They are drawn quiet while the car is free and active while it is held. The difference is in shape and motion; signals own no colours.
2. **A holder mark** over each holder (one icon, within the ≤ 2-icon budget), with a tether line to the car. Downing or breaking the holder removes both, so the player sees the car come free.
3. **A car marker:** five seat pips (Vale plus four), filled as each boards, and the state: free, or held with a count.
4. **The Board chip:** "Board 1 s", or "Board 4 s" when carrying.
5. **The Go card:** one line. Nobody left: "Go". Otherwise "Go · leaves Tech (down), Scout": each name carries a fate glyph (captured / walks out), plus "+1 T" if seen leaving.
   - If anyone would be left, Go commits with the going-loud card's 0.5 s hold (`loud-commit-hold-ticks`), with no pause.
   - While held, Go is greyed and its refusal pulses the holder marks.
6. **The roster:** an in-car badge per op. While the Go card is open, a left-behind flag shows on every op not in the car (H11 widens to this).
7. **Hints** (Fable): H11 reworded ("anyone not in the car stays behind"), and a new H17 for Board/Go on the first final fight.
8. **Debrief:** the §3 lines, and each captive with "holds n missions".

## 7. Rulebook values (all `proposed`, tag S)

| id | v | unit | src | reason |
| --- | --- | --- | --- | --- |
| `fight-hold-m` | **8** (was 25) | m | Sky 2026-10-18, option 3: "get my team into the car, and get away" | a hostile within 8 m reaches a door in 5 s at the rivals' 1.6 m/s, about pistol point blank (5 m) plus a car's width. The ambush and police stops (12–19 m) stand outside it, so the car is held by those who come for it, not by the fight around it |
| `fight-block-m` | 30 | m | same | the stretch a car needs to pull out of a kerb bay and reach speed (about 4 s at 3.5 m/s²); a body in it is in the car's way |
| `fight-blockers` | 1 | count | same | one hostile per fight goes for the road. It doesn't pass on, so the car is freed by downing one, not all |
| `board-s` | 1 | s | same | getting in or out of a car beside it |
| (reuse) `carry-s` | 3 | s | §06 downed: carry | putting a carried body in mirrors lifting it |
| `captured-trail` | 4 | trail | §06 downed: captured → rescue; Sky 2026-10-18 | as much evidence as a knockout's statement (+4) |
| `custody-fear` | 1 | scale-steps | §04 Talker test; this brief | custody pressures as one Pressure does |
| `custody-loyalty` | 1 | scale-steps | same | "they're not coming": loyalty wears each mission held |
| `abandoned-resentment` | 1 | scale-steps | same | an op who watched the car leave without them resents it |
| `abandoned-loyalty` | 1 | scale-steps | same | and trusts the cell less |
| `custody-missions` | 3 | count | §06 captured → rescue mission | the rescue window. Every founding op has talked by then |
| `cell-start` | bravery 2, loyalty 3 | scale-steps | §03 training: full cell | the founding cell is sworn, not fearless |

Rule text changes:

- `fight.rule`: "the car can't leave while one stands within `hold`" becomes the hold above (near or on the lane), plus the blocker.
- `cell.rule`: Extract becomes Board and Go; downed not brought to the car → `LeftBehind`.
- `debrief.lines`: the three new lines.
- `people.rules talker.facts`: + `cell-face`, `contact`.
- Version bump: engine 0.7.0.

## 8. Handoffs

- **Keel:** the hold with its holder ids; `getaway.exit` read; Board, Go and step-out; `LeftBehind`; seen-leaving; the custody projection for the debrief; the Enforcer-down and rivals-reach-Vale changes; scenarios for each §3 row, §4's twin of N95, and Go refused naming its holders. Done when the scenarios pass and the Unity match holds.
- **Pulse:** the blocker's pick and walk; in-car targeting at ×500; the final fight's objective once Vale is in the car (the car, still stopping at `ai-stop-reach-pct`).
- **Splice:** `getaway.exit` in the site export, from `routes.getaway` and the plan road's `w`, with a site check that it exists and lies on the carriageway.
- **Waymark:** re-record per §5.
- **Glyph:** §6.
- **Fable:** the words for §3 and §6.
- **Aster:** the GDD v0.24 lines:
  - §03 (Board and Go);
  - §06 (downed: left behind; in-car fire);
  - §07 (final fight "up to 60 s; the cell may leave once the car is free"; win; Enforcer-down fail; the hold);
  - §08 (captives, rescue, the debrief lines).

## 9. Open questions for Sky

1. **Fight length.** §07 says the final fight is 30–60 s, your number. Board and go will often end it in 15–30 s. Should §07 read "up to 60 s"?
2. **A captive after the rescue window:** lost for good, or out of reach until a later act (a prison-break objective in act III)? I propose out of reach, so the act can come back to it.
3. **Walking out:** an op left standing on a calm street walks home at no cost. Should that cost one mission lying low (unavailable)? I left it free, because a calm street has nothing to fear.
4. **The menu's Withdraw** ends the attempt with nobody captured, which now contradicts Go without Vale. Keep it as a free abort (the training mission replays anyway), or make it Go without Vale?
5. **Seen leaving: +1 T** only. Should a mid-fight getaway in a loud run also give the police the car to hunt next mission (a "car known" state the cell must answer, e.g. a new car via a contact)? That is richer; I left it as a fact in the investigation.

### Sky's answers (2026-10-18: "ok with the getaway suggestions")

1. **Fight length:** §07 reads "up to 60 s" (GDD v0.24).
2. **Captive after the rescue window:** out of reach (moved out of the district), not lost for good; act III can come back to it.
3. **Walking out:** free. A calm street has nothing to fear.
4. **Menu Withdraw:** stays a free abort, nobody captured. Go without Vale is the in-world withdrawal with left-behind rules.
5. **Seen leaving:** +1 T, and the car becomes **known**: the investigation holds `cell-car` as a hunted car next mission, which the cell must answer (e.g. a new car through a contact). The campaign side waits for a campaign layer; the engine records the fact.


## 10. Pulse review (2026-10-18): the blocker, its post, and seen leaving

Asked by Keel after building §1 from the numbers. Sky's intent (2026-10-17): "I want to get my team into the car, and get away. But it's interesting, because the enforcer can't run, so they have to support the enforcer." The getaway is a tense supported walk and board, beatable by the three covering the Enforcer; the quiet routes still beat loud. No C# was edited. Evidence came from a scratch copy of the engine (outside the repo, since deleted; the rules were switched by environment variable) run on the real block over N10, N11, N75, N87, N111 to N116 and four probes: the Enforcer walks Vale in while the three (P1) fire on the others and spare the blocker, (P2) fire on all, (P3) fire on the blocker alone, (P4) only draw fire.

### What the numbers showed

- **cv-004 is a door-camper.** The planter-run cover cv-004 (975, -5025) is 2.4 m from the car point (850, -4820), inside the exit lane. From the west the nearest lane cover is always cv-004, in the quiet branch too (N11, N18, N95, N111 to N115: rival-4 picks it). A shot at 2 to 4 m is inside `point-blank-m` (5 m): accuracy 750 per shot for a rival, no cover discount for the blocker against anyone within 5 m, and 750 against the Enforcer where every other rival hits at 562. The blocker is the best shot in the fight, and the cell's cover is worth nothing against it.
- **The blocker is not the main danger in the quiet branch.** The other four rivals do most of the damage: in P1 and P4 the Enforcer is dead in about 14 s with the blocker at cv-004, about 17 s with it at 11 m. It adds 0.5 hits/s at the door against 0.375 from each other rival. A blocker at the door is the problem, not blockers.
- **Loud is deadly because of four trained officers, not the blocker.** At level 1 with the cell parked at the car (L1 to L3), the cell is down in about 31 s whichever cover the blocker takes. That is the price of going loud (§5) and I leave it alone.
- **The pick can choose the wrong body.** `NearestInExitLane` pulls the measuring point to 1 m inside the lane's edge. For a rival west of the bay that point is (465, -5282): behind the lane's start, outside the lane. By that measure rival-4 (west, 13.8 m) beats rival-1 (east, on the road, 17.3 m), so the blocker runs 26 m through the cell and is dead in 2.5 s (N113). The mechanic then never bites in the quiet branch.

### Decisions

**1. The pick: shortest grid walk, measured to the blocker's own post. Ties by id. Candidates are the officers on the street on the fight's first tick, the response unit's included.**

- Keep "on the first tick, from every officer on the street". "The first to arrive" is the shortest walk, because they all walk at one pace.
- Change the measurement point. Each candidate is scored by the walk from where it stands to the post it would take (cover point, else the open point of decision 3), not to a lane point.
- Why: it picks the body that really is nearest to the job, and it never picks a body for a point outside the lane. In the Handover's quiet branch this makes the blocker rival-1 (east, on the road, 17 m from the lane; the shortest walk to a post in the ring), not rival-4.
- The response unit's officers count where they stand. Pulling one off an earlier exchange is the right reading of "first to arrive".

**2. Behaviour: accept all three as built.**

- It never stops on the way: it fires nothing while it moves, so a long walk through the cell is a gauntlet the cell can shoot at, and the blocker has no free hits.
- It holds its post as police hold a stand, and does not step to other cover. A blocker that left its post to chase cover would free the car by itself.
- Not replaced: a downed, broken or gone blocker frees the car.
- A rival blocker breaks with its crew (`RivalDown`), because a broken crew is gone. Police never break. That is the existing rule, and it is what makes "fight it out" (break the crew) and "board and go" (down the blocker) different jobs.
- One fact to keep: the warning (`fight-warn-s`, 10 s) does not hold the walk. The blocker takes its post while the rest hold their fire, so a warned player sees the road close and has 10 s to answer it.

**3. The post: a ring, not "nearest to the blocker".** The nearest free cover inside the lane, but only at `fight-block-near-m` (10 m) to `fight-block-far-m` (13 m) from the car point, measured in a straight line as the hold is. If no cover stands in that ring, the blocker stands in the open on the lane centreline `fight-block-near-m` along the lane from its start.

- **near = 10 m.** Beyond the hold ring (8 m), so the blocker holds the car by the lane, not by the door, and beyond point-blank (5 m) with margin, so the cell's cover works against it and the Enforcer's 4 s Board is not at point blank.
- **far = 13 m.** Inside `pistol-eff-m` (15 m) with 2 m to spare, so the cell around the car and the blocker are in each other's effective reach. At 15 m or more an unengaged blocker stands off: nobody fires, and Go stays refused with nothing to answer. Cover at 19 m (cv-092, N75 and N87 as built) is the same trap.
- **Why a ring and not "the lane's far half" or "no cover".** The far half of the lane is 15 to 30 m: out of the cell's reach at the car, so the three must leave the Enforcer to flush it, which is the opposite of supporting him. Open ground with no cover is kinder than cv-004 but takes away the cell's choice of how to fight and any reason to read the cover. A ring lets cover in the right place do its job and gives the open fallback a defined spot on a block with no planter.
- **What it does on the Handover:** the only cover in the ring is cv-088 (1925, -5075), 11.05 m from the car, on the lane's north kerb, facing the car. Quiet and loud both go there (N75, N87 now cv-088 instead of cv-092).
- **Why that is a supported walk.** The blocker is 11 m from the cell at the car, in its reach and in cover. Three ops at that range hit a covered target at 281 per mille each, so about 5 s to down a 3-health officer when all three fire. They draw its fire first (target class 1, whoever shot it within 3 s), so it shoots them and not the Enforcer. The Enforcer walks into its reach only as the cell is already firing, and boards with Vale in 4 s. A blocker left alone shoots him at 0.375 to 0.5 hits/s, so ignoring it still costs: P3 and P4 die, P1 and P2 board.
- **Measured (quiet, 5 rivals, Inez warned, the guard fights them):**

| | blocker cv-004 (as built) | cv-088 (ring, pick by post) |
| --- | --- | --- |
| P1 fire on others, spare the blocker | Enforcer dead at 14.2 s | in the car at 18.3 s, 2 health |
| P2 fire on all | in the car at 18.3 s, 2 health | in the car at 18.3 s, 2 health |
| P3 fire on the blocker alone | Enforcer dead at 17.2 s | Enforcer dead at 17.2 s |
| P4 only draw fire | Enforcer dead at 14.2 s | Enforcer dead at 17.2 s |
| N11 fight it out (final health) | Enforcer 1 of 6 | Enforcer 4 of 6 |

  Boarding is beatable and tense: the Enforcer ends at 2 to 4 of 6 health when the three do the job, and dies when they stand idle. Dying needs only ignoring the field. Quiet still beats loud, whose cost is paid before the car (§5).

**4. Seen leaving: final-fight officers see the car out to `weapons.pistol.max` (25 m) on a clear line.**

- Final-fight officers have no cone, so Keel gave them security's 19 m. An officer in a fight watches the fight, and the car is where the shooting is: if it can shoot as far as the car's door, it sees the car go.
- Use the officer's weapon maximum (25 m, existing value `pistol-max-m`), still needing a clear line. An officer with no line to the car does not see it, as built. No new key.
- Effect on the real block: N10 (a won loud route) now ends with "officer-2 sees the car leave", +1 T and the car hunted. Before, every officer was at 19.4 m or hidden, and the +1 T almost never came, which broke Sky's answer 5 (a loud mid-fight getaway gives the police the car). Quiet runs are unchanged: rivals never report, and the guard ally and the patrol are as built.
- The cone does not change for the response unit's own officers, the robots or the patrol.

### What Keel changes

`sim/Suborned.Sim/GameGetaway.cs`

1. `BlockCover(from)`: skip a cover point unless its straight-line distance to `Car` is at least `R.BlockNearCm` and at most `R.BlockFarCm`. Keep: free (`coverTaken`), `Q >= CoverQPartial`, `Present`, `S.InExitLane`; nearest to `from`, ties by id.
2. New `Pt BlockPost(Pt from, out CoverPoint? cv)`: `cv = BlockCover(from)`; the post is `cv.At` or, with none, `S.LanePointAt(R.BlockNearCm)`.
3. `PickBlockers`: for each candidate compute its post first, then the walk from `S.Grid.NearestFree(from)` to `S.Grid.NearestFree(post)`; sort on that walk, then id. Do not take cover while scoring; take it for the winner only, as now. Drop the `NearestInExitLane(from)` entry point. Skip a candidate whose route is null, as now.
4. `CarWatcher`, the loop over `Hostiles` for officers: replace `R.SecReachCm` with `R.Pistol.MaxCm` (the final fight's officers only; leave the response unit, robots and patrol on their cones).

`sim/Suborned.Sim/Site.cs`: add `public Pt LanePointAt(int cm)`, the point `cm` along the `ExitLane` polyline from `ExitLane[0]` (the last point if the lane is shorter). On the centreline, no inset.

`contracts/rules/rulebook.json` and `contracts/rules/rulebook.schema.ts`, `Rules.cs`:

| key | v | unit | tag | src | reason |
| --- | --- | --- | --- | --- | --- |
| `fight-block-near-m` | 10 | m | S | brief 018 §10, Pulse | beyond `fight-hold-m` (8 m) and point-blank (5 m): the blocker holds the lane, not the door, and the cell's cover works against it |
| `fight-block-far-m` | 13 | m | S | brief 018 §10, Pulse | inside `pistol-eff-m` (15 m) by 2 m: the cell at the car and the blocker are in each other's effective reach, so the fight starts and Go is never a standoff |

- Add `fight.blockNear` and `fight.blockFar` (`v:` refs) in the `fight` section. Read them as `R.BlockNearCm` and `R.BlockFarCm`.
- Validate `BlockNearCm > HoldCm`, `BlockFarCm >= BlockNearCm` and `BlockFarCm < Pistol.EffCm`; reject otherwise (unknown = reject).
- `fight.rule`: the blocker's post is the nearest free cover inside the lane at 10 to 13 m from the car, else the open lane at 10 m. Seen leaving: a final-fight officer with a clear line to the car within `weapons.pistol.max`.
- `fight.blockers` stays 1, `fight.block` stays 30.

Fixtures and records that will move (rule consequences, not defects):

- **N11, N114.** `FightItOut` ends when the car is free. With the east rival as blocker it falls first, so the crew is not broken: N11's "rest break" and N114's "fight over, walk out, +1" fail (the three are captured "fighting", the fight moves 0). The fixture, not the rule: N114 should run `FightItOutAll()` as N115 does, or the check should read that the crew is whole and the car free.
- **N115.** The seal clock needs the guard downed by the rivals; with the blocker pick changed the guard survives the timing. Fixture only.
- **N75, N87, N111, N113.** The blocker's post becomes cv-088. N111 and N113 pass unchanged as built; check N111's "post inside the lane" assertion against the ring.
- **N10.** Ends with a `car-seen` line (+1 T): update its expected Trail and the debrief's lines.
- **N116.** Unchanged (the response unit's cone).
- The Unity match and Waymark's re-record (§5) take the new posts and the +1 T on loud.

If Keel wants the smaller diff first: change only step 1 and the two keys (leave the pick). Only N115 moves, but the lane-cap artifact stays and the west rival (rival-4) is still the quiet blocker, so the mechanic barely bites. I recommend the full set.

### Not changed

`fight-hold-m` 8, `fight-block-m` 30, `fight-blockers` 1, `board-s` 1, `carry-s` 3, the loud fight's four-officer size and the stands, and §5's table are all kept.

## 11. Tension after c019 (Waymark, 2026-10-18)

c019 ships. The getaway is a supported walk on B and G (the Enforcer loses 2–3 health) but a walkover on A (Inez's 10 s warning), and the rival blocker leaves when its crew breaks, so it never makes the car wait. Waymark's options: (1) the rival blocker holds the lane until downed, like police; (2) rival health 3 → 4 (too lethal alone); (3) Inez's warning 10 → 5 s. Aster recommends 1 + 3. **Sky (2026-10-18): play the build first, then decide.** Open bug for Keel: if the three never fire, the downed Enforcer and a held car leave the mission with no outcome; needs a time limit with a plain fail line.
