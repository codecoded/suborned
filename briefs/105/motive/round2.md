# Brief 105, round 2: take another car, and the instant ends

Motive, 2026-11-10. This is for Keel and builds on round 1 (`unguarded-car.html`, committed in 1b8121c56). Sky approved Q1, Q2, Q4, Q5 and Q6 as recommended, and answered Q3 in round 2: a shot-out car is not a loss. Proposed for the rulebook, tagged `proposed: true`, src brief 105. Nothing here is in the GDD yet.

Already with Keel and not repeated here: no take from a car, riders safe, rivals shoot people first and then the car, agents at the car count drawn or not, and a shot-out car puts everyone out with Popovic to fight on.

---

## 1. `car.take`: any standing car can become a cell car

**What the lend already covers.** The lend (engine 0.42.0, `GameLend.cs`) already makes a second cell car: its own seats, hold, lane, box and Go, with its Go as the win. It is wired to the official car alone: `LentSeat`, `lentLane`, `LentDisabled`, `RivalReachesLent`. `car.take` generalises it from "the official car, lent at the in-car Take" to "any car taken". It adds no new prices: every price is an existing rule's.

### Rule text (lift as `car.take.rule`)

> **Take car** (brief 105; Sky 2026-11-10: "if everyone is out, and there's other cars still around, then they can take those ones e.g. the rivals car"). Any car the mission simulates can become a cell car if it is **standing, not disabled, and nobody is at its wheel**. That covers the rival car, a passing car standing or abandoned (brief 098), the official car, and the cell's own car, whose Board already is this rule. Burning cars and cars that have left don't qualify.
>
> **Order.** The order is `take-car <car>`, from the ring of the car. Any standing operative not carrying anyone can give it. He walks to the door as any click-to-act does, and on reaching `car.footprint.reach` of its body he is at the wheel `board-s` later. From then on it is a cell car (`car:<id>` keeps its id; log `car.taken`). It has its own Board, Step out and Go, as the cell's car and the lent car have.
>
> **Someone at the wheel.** The ring offers Take car with a fix:
> - **A civilian driver** (a passing car's, or the official car's driver unturned): the operative needs a drawn gun. Within `car.footprint.reach`, the driver gets out on the kerb side and runs, taking the crowd's flight from a shot (traffic law L8). This is the `weapon-drawn` act with its own witnesses and Standing. The driver is a witness who reports, by the crowd's report walk.
> - **The rival wheelman:** only the Enforcer can take the car, as a takedown at the door (`takedown.reach`, the takedown's knock-out time and price: `takedown` Standing if seen). The wheelman is then out, knocked out on the road side. Nobody else can pull him out, and he can't be shot in the car (round 1 Q2).
> - The ring names the fix: "Draw first", or "The Enforcer takes him out".
>
> **Seats.** A car seats its kind's `seats`. Popovic takes one seat, so the Enforcer's carry takes two. Board is refused `car-full` past that. *Keel: the traffic kinds have no seat count today. I propose sedan 4, van 6, scooter 1 (a scooter takes no passenger, so it can't carry Popovic). The mission cars keep theirs: getaway as today, rival car 5, official car as the lend's L3.*
>
> **Who drives.** Any standing operative aboard (fight.unguarded (c), car-no-driver, unchanged).
>
> **Way out and Go.** A taken car's way out is the drive grid from where it stands to the nearest mouth of the zone that is not cordoned (response a3Units). It is boxed (`car.box`) and held (`fight.hold`) by the same laws as any cell car: Go is refused when it is boxed, held or gated. Go with Popovic aboard is the win, fixed (`vale.out`, as the lend's L5/L7). The mission runs on for anyone left, by the lend's L8 (`split-police-s`, then everyone not in a car that has left is captured).
>
> **Prices** (all existing):
> - A taken car seen leaving is `+witness-trail` and a lead, like the lent car (L9).
> - A car taken in a watcher's sight is logged like any act there.
> - The cell's own car left standing is lost (L9: hunted, car-seen 'left').
> - Taking the rival car clears the box it was making: a car that leaves unboxes (car.box).
>
> **Rivals and police.** A taken car with Popovic aboard is "a car we hold" for the round-1 target rule: it gets shot at when nobody stands outside it. Unknown car kind, or a car with no way out to a mouth: Take car is refused `no-way-out` (unknown = reject).

**The one ring action:** "Take car" on the car's ring, with a badge for the fix ("Draw first" / "Enforcer"). It is clickable, with no key needed, per Sky's no-key rule. Fable writes the words, Glyph the ring.

**Keel's checks for this:**
- (a) Everyone out of a shot-out car takes the rival car after its crew breaks or after the Enforcer's takedown at the door, and gets away.
- (b) A passing car stopped by gunfire, its driver run off, is taken and driven out by its lane's mouth.
- (c) Take car on a scooter with Popovic carried is refused `car-full`.
- (d) Same seed + same orders replay byte-identical. No bytes are hashed until a car is taken.

---

## 2. The fails that end the Handover at once

Sky's principle: the mission ends when the cell can't get out, with the police coming until they're dead or captured, never "just like that".

| Fail (cause) | Ends at once? | Verdict | What it becomes |
|---|---|---|---|
| `getaway-lost` (wrecked) | yes | **change now** (Keel has it) | The riders get out with Popovic and the cell fights on, on foot, or with `car.take`. |
| `rivals-reach-vale` / `reached-car` | yes | **change now** (Keel has it) | Gone. Nobody takes anyone from a car. |
| `cell-down` | yes | **keep** | It is the end Sky names: the cell is dead. |
| `rivals-reach-vale` / `downed-enforcer`, and `enforcer-down` | yes | **own brief** | Today only the Enforcer carries Popovic, so his fall ends it. The brief: another operative can carry him (slower, untrained carry `takedown.carry.untrained`), and rivals on foot can take him only from the ground, then must carry him to their car. That is a chase, not an end. |
| `rivals-reach-vale` / `lookout` | yes (0.1 s after he runs) | **own brief** (Q6) | The take stays (not our car), but it must be visible: the lookout runs, reaches the car and pulls Popovic out, rather than the fail landing the tick he starts running. |
| `rivals-reach-vale` / `beat-to-pickup` | yes | **own brief** | The rivals hold Popovic and walk him to their car. The cell can still stop them before their car leaves. The fail lands only when their car is gone with him. |
| `sealed` | yes, at `Lockdown` | **own brief, the end itself** | The seal stops being a fail: the cordon closes the mouths (a3Units) and units keep coming, so the end is `cell-down` or captured. **The police must not stall** (see below). |
| `closed-in` (no-driver, car-held, on-foot, no-go, boxed) | yes, at the close | **own brief** (with `sealed`) | At the close the district seals: the same cordon and the same endless units, not a verdict. |
| `entry-seen-guard`, `entry-seen-camera`, `entry-seen-patrol` | yes, at the door | **own brief** | They become an alarm: Popovic held inside, the Office locked (`doorsLock`), Alert raised. The cell withdraws or goes loud. Note: careless plays 97 % end here today. |
| `abduction-seen-camera`, `driver-shot` | yes | **own brief** (with the entry fails) | Security rescues Popovic as an act with time and bodies (officers walk him back), not a cut. |
| `vale-leaves`, `vale-dead` | yes | **keep** | The target is gone or dead: the objective is over. Not the cell being stopped. |
| `engine-error` | yes | keep | Not play. |

**Change now:** `getaway-lost` and `reached-car` (Keel, in progress) plus `car.take`. Everything else waits for its own brief. The biggest of those is "the seal, the close and the police", because it sets the end that every other change falls back to.

**The police stall (for that brief; checked in the code).** `GameResponse.cs:283–291` counts the units *sent* against `unitCap`, so the escalation stops for good: at the Handover's level (watchful), that is 1 unit plus 1 every 40 s, 3 units in all, 6 officers. A cell that downs them and stays waits for the seal, and with the seal no longer a fail it would wait forever. The law for that brief: **after the seal, one more unit every `reinforce` while fewer than `unitCap` units have an officer up, without end.** The cap then counts the living, not the sent, so the pressure is steady rather than a swarm, and it always ends in `cell-down`, captured, or a getaway. Until then the seal stays a fail, so a stranded cell's fight on foot is still bounded by today's seal timer (145 s at watchful).

No sweep this round (Sky: be quick). Round 1's probe is the upper bound: 87 freed runs waited a median 47 s for the close. `car.take` gives them something to do in that time.
