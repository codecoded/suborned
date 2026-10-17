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

