# 013 A · Takedowns, bodies and quiet places: the rules (Motive, 2026-10-16, rev 4)

Brief: `briefs/013-takedowns.md`, part A, including "Sky's answers to Motive's calls" (2026-10-16). Motivation: `briefs/012/idle-cell-study.md` (the Enforcer idles 47–81 % of a quiet run).

**Rev 2 reconciles three sources:**
- Pulse's witness design: `briefs/013/pulse-witnesses.md`, `pulse-witnesses.values.json`, `pulse/probe-output.txt`.
- Vantage's survey: `briefs/013/vantage-quiet-places.md`, `vantage/quiet.json`.
- Sky's answers to C1–C6.

**Ownership:**
- **Pulse** owns what a person does: reactions, report points, the walk and the call, telling a tie, the woken victim, suspicion, and the swarm's behaviour.
- **Vantage** owns where a body can go.
- **Motive** (this file) owns the prices, the Alert, Trail and Standing for each case, the wake and talk numbers, missing persons, and the response dial's values.

Nothing in `contracts/`, `sim/` or the GDD is edited. Every value is **proposed**, for Keel next.

**The design in one line:** the player decides what the city learns (faces, leads, a body or a missing person) and when it learns it (the report window, the check-in mark). People carry every fact to security, so a takedown nobody reports never raises Alert.

---

## 0 · Reconciliation (Aster's five points and Sky's answers)

| point | decision | reason |
|---|---|---|
| **1. When Alert rises** | **When the report lands, for every silent act** (takedown, carry, body, missing person). **Gunfire keeps its rule at sight**, because security hears it. Witness Trail (+1) and Standing still move **at sight**, under the existing `witness.sees` once-per-incident rule. | §01: "information moves through people and paper; people are the weak point". If Alert rose at sight, a witness's temperament, the report window and every counter of Pulse's §3 would not matter. That would make a pillar-one system (subvert people) into a coin of "was anyone within 13 m". The rulebook's "violence seen by a civilian → 3 at once" was written for gunfire, where security hears the shot. **Adopting Pulse's recommendation.** |
| **2. The report chain** | Adopted as Pulse writes it: landing, then dispatch, then walk, then verify. The dispatch time comes from the **response dial** (§5.3; 12 s at the Handover's level, Pulse's figure). The Alert per report kind is in §2.3. **A knockout never costs more than Alert 1**, which overrides Pulse's "confirmed lying person → 2" and "woken victim → 2". **One takedown is one incident:** Alert is the highest of its landed or confirmed reports. The **seal starts at a confirmation at Alert ≥ 2**: Pulse's verify, combined with my "no seal on an inquiry". | Found knockout = an inquiry, found kill = a search. This line is the only in-mission difference between the two choices. If a woken victim made Alert 2, a knockout would be a slower kill. |
| **3. The guard route against the probe** | **It holds, and it stays in the service lane.** It is retimed to the lane's quiet window (0:00 to about 4:00, before guard 74 at 4:04) and to the guard's real check-in marks (1:00, 2:30, 4:00; §5.1). D2 and R1 are 65–67 s carries from his post across the open square: they suit café takedowns, not the guard. §6.1. | Vantage `quiet.json` and Pulse §7.2. |
| **4. Pulse's open questions** | (a) A turned witness (loyalty ≥ 1) adds **no witness Trail** for the new acts. (b) **No** Tech action to cut a call-box line. (c) The Idealist at bravery ≥ 2 does nothing more in v1. (d) `wake-s` 120. (e) A takedown at a report point is priced like any takedown. (f) **Every staff post checks in**, as a periodic routine (§5.1). | (a) Pillar test: "does knowing more make a person quieter?" A contact who keeps quiet should cost nothing; the fact stays in their memory and the talker test can still draw it out. Scoped to the new acts so existing replays hold. (b) It is a new power (people > powers), and the counters already exist: be unseen, turn the people around you, take the reporter down. |
| **Sky 1** | A found killing gives Standing **−1**, as one value. | Sky: yes. |
| **Sky 2** | **No auto-find rule.** A hidden body becomes a **missing person**: a campaign case, an area that hardens, and a district security level (§2.6). | Sky. |
| **Sky 3** | `enforcer-face` is a talker fact. | Sky: agreed. |
| **Sky 4** | The guard checks in every 90 s at times the Scout can learn, generalised to every staff post. | Sky: ok. |
| **Sky 5** | D1, the service door, and a Dumpster in the lane. | Sky: ok. |
| **Sky 6** | **The driver is takeable.** Two new rules: an expected person who is missing raises suspicion (Pulse's behaviour, my numbers), and the response dial. The driver route is re-costed (§6.2): possible, at the edge of the clock, and dearer than both turning and loud. | Sky. |

## 0.1 · Rev 3: Keel's build, confirmed or corrected

Keel built rev 2: 340/340 tests, 22 new scenarios. These are my answers to the eight choices Aster raised, for Aster to route; I have not edited the rulebook.

| # | Keel built | Motive |
|---|---|---|
| 1 | A seen knockout lands Alert 3. | **Confirm.** Seen violence is Alert 3, whatever the outcome. The knockout's Alert-1 cap covers only a knockout that is *found* or *woken*, never one that was *seen*. |
| 2 | A takedown seen by armed security is confirmed at sight, and the seal starts then. | **Correct the seal, keep the confirmation.** It is confirmed at sight, but the seal starts at the later of two moments: confirmation at Alert ≥ 2, and the unit on the street. The seal is the unit's cordon, and gunfire already works this way. §2.3. |
| 3 | "Doors lock" = Enter refused at Alert 3. | **Confirm.** An Enter already under way finishes, at +30 s (Alert ≥ 2). |
| 4 | `enforcer-face` added to `people.rules.json` and its schema. | **Confirm.** It implements Sky's D3; they are my files and the change is right. |
| 5 | Level 1 has Alert 3's cordon unit and +1 unit every 60 s, so it is not today's response. | **Correct: drop both at S1 (and S0).** S1 becomes exactly today's response, the anchor every recorded run replays against. The swarm starts at S2, the Handover's civic level. The loud fight's existing +1 officer at Alert 3 stays. §5.3. |
| 6 | Guard 29's marks fall at 1:00, 2:30, 4:00, 5:30. | **Keel is right.** His post stops merge from −28,470 s; rev 2 read only the last stop. The table and the driver route are fixed: 29 is taken at 2:31, and his 4:00 missed mark clears the bay before Vale arrives. §5.1, §6.2. |
| 7 | The D1 Dumpster is emptied at 5:22 (phase from its world id). | **The round is right.** It is a learnable deadline. G-kill ends around 5:00, so it holds, ~20 s inside the round. The driver route, which has to last to 6:10, puts every body through the D1 door. §6.1, §6.2. |
| 8 | Not built (see below). | **One is needed now, the rest can wait.** |

**Item 8: what is needed now:**
- **Facing at a stop (Pulse):** needed before the driver route or any crowd takedown counts. Until it exists, build the small rule in §1.3: **a witness of the cell's act stays aware for the rest of the mission**. The guard route needs neither, because the door guard has his door cone.

**Item 8: what can wait:**
- **Walking toward a scream to look:** defer. Screams come only from aware, unarmed victims, and neither Handover route makes one with anyone within 20 m.
- **A told person goes to see:** defer.
- **The 8 m detour:** defer; it is crowd texture only.
- **The guard's 15 m scream check:** defer, until a route takes someone within 15 m of an *upright* door guard.
- **The 60 m robot reach:** defer, if it is only how far a robot looks for things. If it bounds where an alarm's robots go (to the missing person's connections, Sky 6), the driver route's suspicion needs it, so Keel should say which.
- **Quiet (F2):** defer. It is not approved and no route needs it; "turned" falls back to a full Turn.
- **Shot people becoming bodies:** defer to [L]. A shooting is loud anyway (Alert ≥ 2 at sight). The shot guard's missed mark is already built.

## 0.2 · Rev 4: Waymark's c009 and Keel's two rule changes (Keel-ready)

Inputs:
- `briefs/013/waymark-handover.md` (c009 SHIPS);
- Sky's decision of 2026-10-16: a found body locks down the scene;
- the rulebook's dial as Keel built it: A1 sends `a1Robots` robots, only to places within `robot-reach-m` 60 m.

All values below are proposed, tag S. Nothing in the rulebook is edited.

### R4.1 · A found body locks down the scene (Sky)

| rule | value | src · reason |
|---|---|---|
| **A confirmed body brings the response unit**, knocked out or dead, **at any Alert**. The unit is officers, so the robot reach doesn't bind it. It comes in addition to the level's A1 robots. Alert stays as §2.3 says (a knockout 1, a kill 2). | the dial's `dispatchS` from the report's landing; the unit starts at the site's docks and posts, else the response entry | Sky 2026-10-16. "Found" means a report has landed (the body is seen by a person, a robot or a live camera, or the woken victim reports). A body nobody has reported holds nothing. |
| **Held doors** (`scene-hold`) are the entrance the body lies at (within `near-m` 3 m), plus every building entrance within **`witness-range-m` 13 m** of the body on a clear line (`Site.Sight`). | reuse `near-m`, `witness-range-m` | "Doors in sight of the body": the same test as every witness, so the "seen by" preview can show it. A door around a corner (D1 from the guard's post) is not held. |
| **A held door refuses Enter, and refuses any exit through it** (the Enforcer, with or without Vale). An Enter already under way at that door finishes, with the +30 s alarm (Alert ≥ 2) if it applies. The cell can still leave by any door that isn't held. | codes `door-held` (Enter), `exit-held` | the scene is held: nobody goes in or out past the officers. |
| **How long a door is held:** from the unit's arrival at the scene until it stands down. That is `stand-down-inquiry-s` **60 s** after the last alarm at Alert 1, `stand-down-search-s` **180 s** at Alert 2, and never at Alert 3. | reuse the existing values | no new clock. At Alert 1 that is a minute; at Alert 2, three. |
| **On arrival the unit searches** for `search-s` **60 s** (existing). It looks with its cone, and that counts as a watcher. At Alert ≥ 2 it also opens every stash within `search-stash-m` 30 m, at 10 s each. | existing | the sweep Aster asked about. At Alert 1 it opens nothing, so a stashed body survives an inquiry. |
| **A missed check-in mark** stays as built: a report at Alert 1, answered by the A1 robots within 60 m. **It is not a found body.** The post is empty, so it holds no door. | as built | this keeps the Tech's Reroute meaningful: with the patrol away, a missed mark has nobody near enough to answer. The player reads it on the Reroute card ("patrol away: a missed check-in goes unanswered"). |
| **Price line, for Glyph:** the takedown card's "if found" line reads "**Alert 1 · unit in ~n s · holds the Office door ~1:00**" (a knockout) or "**Alert 2 · police in ~n s · holds the Office door ~3:00 · search 30 m**" (a kill). The carry hover over a destination reads "**out of sight of doors ✓**" or "**holds: Office door**". | | §02: every order shows its full price. |

**Re-costing route G (estimates; Waymark plays them in c010):**
- **G-fast** (knockout at 2:01, Enter at once, the guard left at his door) **is unchanged: 3:37, 6 T, Alert 1, idle 35 %.** Nobody can find him before the run ends:
  - the camera is looped;
  - the patrol is rerouted, so the 2:30 missed mark brings no robot;
  - the first passer is guard 74 at 4:14;
  - he wakes at 4:01, after the extract.

  The lock doesn't bite because there is no finder. **That is the honest result: carrying beats leaving only where a finder would come while the cell still needs that door.** This is not a bug: the card shows "found by: none before 4:14", and leaving him is the informed choice.
- **Where carrying wins** (the hold decides):

  | play | if he is left at the door | if he is carried |
  |---|---|---|
  | the Tech drops the Loop before the Enforcer is out (for example, to reach the car for the fight) | the camera's next scan finds him (≤ 23 s) and the unit holds the front door | to R2 (15 s), out of the door's sight: the door stays open |
  | no Reroute: knockout in the 2:13–2:47 sweep gap | the patrol returns at 2:47, finds him in 4 s, and the unit holds the front door while the Enforcer is still inside | Enter can't start before the patrol returns (the carry takes 15 s), so this play is dead either way: Reroute is required |
  | a slow run past 4:00 (the 4:00 mark, guard 74 at 4:14) | found: the door is held, and the robots and unit are on the forecourt as he comes out | R2 is found at 4:04 too, but it holds no door. D1 (a stash) is never found at Alert 1. |
  | any route A + G combination (Inez called while the guard is down) | the café crowd and the summons path cross the forecourt | carry |

- **Route G against turning Inez** (MC-9): G-fast at 3:37 / 6 T against A-Pressure at 3:31 / 7 T. Pressure is faster and G is cheaper, so **neither dominates**.
  - G-kill with the case priced (R4.4): 2 + 2 + 3 = **7 T-equivalent** at 4:44, which A-Pressure dominates (3:31, 7). So the kill is never the efficient choice on the Handover. It is right only when the victim knows too much.
  - **Watch item for Suture:** the G-fast margin is 6 s and 1 T. If c010 shows G-fast ≤ A-Pressure on both, the first lever is `ko-s` 6 → 8.

### R4.2 · The witnessed Take, and route B (Keel's change 5)

| decision | rule | src · reason |
|---|---|---|
| **A person turned for this act stays quiet, whatever the tie.** | A turned contact whose job enables an act of the cell (the driver turned to move the pickup or block the camera, for the Take at his car) is an **accomplice** of that act, never its witness. **New first row** in Pulse's reaction list, ahead of kin. | §04 bravery ladder: an Accomplice hides operatives and drives. A man paid to bring the car is part of the act he set up; reporting it would report himself. |
| **Workmate is not kin.** | **Kin** = lover or family only (Pulse §2.3's own definition). Workmate, regular and debtor ties never use the kin row. | `ties.kinds`: Pulse defined kin as lover or family. Keel's build counts workmate; correct it. |
| **A Take at Vale's own car, with its driver turned, isn't a visible act.** | Vale gets into his car as on every day; the Enforcer is already inside, and the Take happens **inside the car**: an interior, off-screen (§01 exteriors only). **No witness window.** Onlookers see a councillor get into his official car. | §01 interiors are timed off-screen actions; §07 route B is "the pickup moves to the service lane". The turned driver is what makes the car a cover. |
| **What makes a Take look wrong elsewhere.** | Any other Take (at the bay with the driver not turned, in the open) has a **struggle window of `take-struggle-s` 5 s** at its start. A witness within 13 m on a clear line during the struggle sees a takedown (Alert 3 on landing). The rest of the 38 s does not read as an act. | Vale resists only when seized. 5 s is the takedown's struggle (3 s) plus the seizure. |
| **Escorting Vale** to the getaway car (on foot, walk only, existing). | A held Vale walking with the Enforcer is **seen as wrong only by Vale's kin** (lover or family, 13 m, clear line), **or by anyone within `near-m` 3 m**, who sees the grip. Either is a `carry` report, Alert 2 on landing. Everyone else sees two men walking. | readable, and it reuses existing ranges. It also applies to route A's exit through D1, as one rule. |

**Route B with these rules:**
- The Take at the service lane car at 6:10 has no witness window.
- The escort to the getaway car (Service lane → Getaway bay, 38.8 s) passes the seven Office staff in the lane. They are Vale's colleagues, not his kin, so they see nothing unless one comes within 3 m.
- **Expected: route B back to 7:40, 4 T, Alert 0.**
- **Waymark:** confirm that the escort path keeps 3 m from the lane's staff at 6:48. If it can't, the fallback is for Sky: the turned driver drives the car to the getaway bay (his job, §04 Accomplice "drives"; it needs car travel, so Keel's estimate first).

### R4.3 · Stash capacity

| stash | holds | src · reason |
|---|---|---|
| container (Dumpster) | **1** | as built (Vantage `stash: { holds: 1 }`) |
| service door | **3** (`stash-door-holds`; rev 2 said unlimited, Keel built 1) | a service door opens on a storeroom, which holds a few. 3 is what the hardest quiet route needs (§6.2: guard 74, guard 29, the driver, all through D1, since the Dumpster is emptied at 5:22). The cap stops one door from being the answer to every body. |

**The driver route needs no second stash** at 3.

### R4.4 · The price of an open case (for MC-9)

| value | v | src · reason |
|---|---|---|
| `case-trail-eq` | **3** T-equivalent per open case, counted in the checker's total Trail beside `nextMission.campaignTrail` | Sky 2 (missing persons). An open case would add 4 T if the body were ever found; it is discounted by 1 because it may never be. The area watch (+1 guard, +1 camera for 3 missions) and the S contribution are shown in the debrief, not priced again. |

What it does:
- It gives MC-9 an honest number:
  - **G-kill = 2 + 2 + 3 = 7** T-eq;
  - **G-KO = 6**;
  - **B = 4**;
  - **A-Pressure = 7**.
- **Kill-and-hide never prices below a knockout** (7 against 6). It wins only on what it removes: the witness (his face and the facts he held), and the wake clock.
- In the campaign the case still works through S, as in §2.6. **3 is a checker number, not Heat.** Heat stays T + Menace, as Sky set it.

### R4.5 · Enter under a live camera

**Keep the instant fail for Enter (§07 `entry-seen-camera`).**
- Reason: the report chain gives the same outcome. A camera's report lands within 3–23 s, and the Enter takes 38 s, so the Enter can never finish before Alert 3 and the doors lock. The instant fail just reads better, and it is the Handover's taught lesson.
- **Generated missions carry "entry seen by a door's watcher" as a standard fail condition from the reference**, not a scripted one.

**Correct one shortcut:** a **takedown** under a live camera goes through the report chain (the next scan + 3 s, then Alert 3 on landing), not through the old guard-down instant fail at `takedown.out`. Waymark found it in §6 of their doc.
- The careless fixture still fails, because its Enter follows under the same live camera.
- But a takedown seen by a camera with nobody entering is an Alert 3 run, not an instant fail. One rule for silent acts.

### R4.6 · Values added or changed in rev 4

| id | v | unit | src |
|---|---|---|---|
| `stash-door-holds` | 3 | count | R4.3 |
| `take-struggle-s` | 5 | s | R4.2 |
| `case-trail-eq` | 3 | trail | R4.4; the checker only |
| scene hold | reuses `near-m`, `witness-range-m`, `stand-down-inquiry-s`, `stand-down-search-s`, `search-s` | | R4.1 |

Rule text changes:
- `security.rule`: a confirmed body brings the unit at any Alert; held doors.
- Pulse's reaction list: accomplice row first; kin = lover or family.
- The Take: no struggle at its own car with the driver turned; the escort is seen by kin or within 3 m.
- Remove the takedown's camera shortcut.

---

## 1 · The takedown

### 1.1 Who, range, time

| | knockout | kill |
|---|---|---|
| who | the Enforcer only | the Enforcer only |
| target | any simulated person on the street (cast or crowd) | the same |
| reach | 1.5 m (`at-reach-cm`) | the same |
| time, target surprised | **6 s** | **3 s** |
| time, target aware and unarmed | 6 + **3 s struggle** | 3 + 3 s |
| target aware and armed | fails: a fight (§1.3) | the same |
| $ | 0 | 0 |
| sound | silent | silent: none of the gunfire rules apply |
| after | **unconscious**; wakes in 120 s (§3) | **dead**; never wakes, never talks |

- **Kill is faster.** That is its only edge in the act itself. Everything after the act is worse or riskier (§2, §3).
- **Not targets:**
  - Vale. Kill = the mission fails (§07); knockout is refused ("use Take").
  - The patrol robot and cameras: Reroute and Loop are their verbs.
  - The rivals: [L], until they have bodies before the fight.
- **A turned contact can be taken down.** It ends the relationship (§06).

### 1.2 Only the Enforcer
- The quiet takedown is his alone (§03; he is the one trained fighter, §06). The verb exists to give him preparation of his own.
- All four can **carry**, **restrain** and **stash** (§3.3, §4). Only the Enforcer carries at walking pace.
- Rejected: an untrained knockout at double time. If Sky wants it, it is one value (`takedown-untrained-mult` 2000 ‰).

### 1.3 The target sees it coming

**Aware** means: in the react-s (2 s) before the takedown starts, the target had the Enforcer in its cone, outside the near circle.
- Security uses its existing cones: the door cone (26 m reach, 19 m half), the facing cone, the security cone.
- Anyone else uses the facing cone along their facing. Pulse owns facing at a stop.
- A runner within 13 m of the target also makes him aware: he hears them.
- **A witness stays aware** (rev 3). Someone who saw a takedown, a carry or a drawn weapon by the cell this mission is aware of the Enforcer whatever their facing, for the rest of the mission. This is needed now: until facing at a stop exists, anyone standing still is always surprised, even the driver who just watched guard 29 go down 4 m away.

**What awareness does:**
- **Unarmed and aware:**
  - +3 s struggle;
  - the victim screams, Pulse's `shout` (`scream-m` 20 m, no line needed; hearers turn and look);
  - the victim saw the face, so he holds **`enforcer-face`**.
- **Armed and aware:**
  - the takedown fails; he draws and engages after react-s, under the existing drawn-weapon rule (Alert ≥ 2, at sight, because security sees it);
  - the preview refuses the order ("he sees you: a fight"), or shows "aware: ?" while his cone is unknown.
- **Surprised:**
  - the victim knows place and time, and no face.

### 1.4 Seen while doing it

The test is Vantage's `seenBy`, the same watchers Pulse's discovery uses.

| | at sight | when the report lands (Pulse's chain) |
|---|---|---|
| a civilian or staff member sees the takedown | +1 witness Trail; Standing −1 (traceable violence); witnesses hold `saw-takedown` and **`enforcer-face`**; a contact who sees it takes the saw-violence move | **Alert 3**; the response, by the dial (§5.3) |
| a live camera sees it | — | its next scan + 3 s (Pulse), then as above |
| armed security sees it | it engages; Alert ≥ 2 at sight (drawn weapons and shots are heard) | — |
| only the rivals see it | nothing (they never report to the city) | — |

- **Only turned witnesses:** a witness with loyalty ≥ 1 (`word-quiet-loyalty`) adds **no witness Trail** and makes no report. Standing still moves, because they saw it. Scoped to the new acts.
- **Stopping the report:** a reporter taken down before the report lands drops it (Pulse §3.2). That is a new takedown, and it is seen by whoever stands at the call box.

---

## 2 · Bodies

### 2.1 A body is a person, down
- A body stays where it fell until it is moved.
- On open ground it is noticed by anyone within 13 m on a clear line, and by watchers in their cones. Pulse §2.2 sets the timing (checked every 5th tick).
- **Hide levels for v1: two, `open` and `shut`** (Vantage's rule; C-new-5).
  - `shut` covers a stash-tagged container or a service door.
  - Pulse's `screened` level (4 m) is **[L]**, together with the body-height sightline. Both make more ground quiet; v1 takes the conservative bound.
- **A carried body is a body.** A carry that is seen is Pulse's `carry` report (§2.3).

### 2.2 Who finds a body and how they react
Pulse §2.3: reactions by temperament, report points, kin, the turned, telling a tie, and the reporter as a volunteer when questioned. My numbers are §2.3 and §5.

### 2.3 What each report costs (one incident per takedown)

| report kind (Pulse §3.3) | Alert on landing | when verified (Pulse §3.5) | Trail | Standing |
|---|---|---|---|---|
| a person lying, state unknown (a civilian who didn't check) | **1** | unconscious: **stays 1** (the unit brings him round: his statement, §3.2). Dead: **2**, and the seal starts | the act's Trail on confirmation: knockout +4, kill +6 | kill: **−1 on confirmation** (Sky 1) |
| a dead body (staff, or an Idealist after the check) | **2** | 2; the seal starts | +6 on confirmation | −1 |
| a woken victim's own report | **1** (Pulse proposed 2) | confirmed on landing | +4 | — |
| a takedown seen | **3** | 3 | witness +1 at sight; the act's Trail on landing | −1 at sight |
| a carry seen | **2** | 2 (the body's state on confirmation) | witness +1 at sight; the act's Trail on confirmation | −1 at sight (a witnessed act) |
| a call-out from a container (Pulse §5) | 1 | verify opens it: as the first row | as the first row | as the first row |
| a missed check-in, or suspicion of a missing person (§5.1, §5.2) | **1** | an empty place: unfounded; the unit searches `search-s` 60 s | none, unless something is found | — |

Notes:
- **One takedown is one incident.** Alert is the highest of its landed or confirmed reports. A second, separate takedown that is discovered is a **second incident**, and the existing `byIncident` rule makes it at least 2.
- **Unfounded** (Pulse): if the body is gone when the unit verifies, Alert stays at what the landing set, and no seal starts.
- **Search:** during the unit's `search-s` 60 s at **Alert ≥ 2**, it opens every stash within **30 m** of the reported place, nearest first, **10 s** each. At Alert 1 it looks and opens nothing.
- **The seal** (`seal-s` 145, or the dial's) starts at **the later of** two moments: the incident is confirmed at Alert ≥ 2, and the response unit is on the street. The seal is the unit's cordon.
  - Gunfire is confirmed by being heard, so it starts at the unit's arrival, as today.
  - A body report is confirmed at the unit's verify, which comes after its arrival.
  - A takedown seen by armed security is confirmed at sight, but its seal waits for the unit. (Rev 3 corrects Keel's build, which starts it at sight.)
- **Act Trail:** a knockout is +4 (`hit-trail` 2 + `down-trail` 2: hit and down, §06 per person). A kill is +6 (+ **`kill-trail`** 2).
  - It is charged when a report about the incident is **confirmed**, when a victim's statement is taken (§3), or as a **missing person** (§2.6).
  - A quiet takedown is always 2 T cheaper than shooting the same person, because it saves the gunfire 2 T.
- **The final fight** goes loud if Alert ≥ 2 before the Enforcer has Vale (the existing branch rule). So a confirmed kill makes the run loud, and a confirmed knockout does not.

### 2.4 Stashes (Vantage §2.1)

| stash | time | how a body in it is found |
|---|---|---|
| a container (`stash: { holds: 1 }`, the Dumpster) | **5 s**, on the street, seen as a carry | a search at Alert ≥ 2 within 30 m; its **servicing round** (Pulse §4.1: a bin every 900 s, phase from its id, learnable by Watch); a call-out (§3.3) |
| a service door, **holds 3** (rev 4) | **10 s**, a timed off-screen action | a search; a routine that uses the door (Pulse); a call-out |

Not found in the mission means not found: no auto-find (Sky 2). A body still hidden at the end becomes a **missing person** (§2.6). A hidden knockout victim gets out after the mission and comes home (§3.2).

### 2.5 Grief (a found killing, between missions)
The ties counted are family, lover and workmate:
- a **turned contact** takes the saw-violence move (bravery −1, fear +1, resentment +1). In the Handover, killing the guard does this to Inez, his lover. A Pressured Inez (resentment 1) becomes an **Informer**.
- **everyone else:** fear +1.

### 2.6 Missing persons (Sky 2; Heat and the debrief)

A person killed and never found during the mission is **missing**.

| | found killing | missing person | knockout (always comes home) |
|---|---|---|---|
| Trail | +6 | **+2** (`missing-trail`: a file, no homicide) | +4, unless quieted (§3.3) |
| Standing | −1 Menace | **none**: nothing was found and nothing seen | none, unless seen |
| ties | grief (§2.5) | **worry**: fear +1 for every family, lover and workmate tie; no resentment, because nobody knows anything | the victim's own fear +1 (and resentment +1 if aware) |
| the investigation | a lead at the place it was found | a lead at the **last sighting** (a watcher's log or a person's memory: the city investigates backward, §02) | the victim's statement: place and time, and the face if aware |
| **area watch** next mission (the block where he was last seen) | **+1 posted guard and +1 camera** (`area-watch-step`) | **the same** | none |
| **district security level S** (§5.3) | +1 case, which **closes after 3 missions** | +1 case, which **stays open until the body is found** | no case |
| later | — | the case can convert: a later search, servicing round or routine that finds the body makes it a found killing at once (+4 T more, −1 Menace, grief) | — |

**The district's level:** S = the district's base + ⌊open cases ÷ 2⌋, capped at the base + 2 (`cases-per-level` 2, `case-level-cap` 2).

**Why kill-and-hide does not dominate:**
- It is the cheapest choice now: 2 T, no Menace, no witness.
- But it pays twice later:
  - the **area hardens exactly as for a found body** (Sky: "the pressure is similar, but its cause is different");
  - its case **never closes**, so missing persons pile up into the district's response level (faster police, more units, §5.3) for the rest of the campaign.
- A knockout pays now (4 T and a statement) and leaves the district as it was.
- So: **knock out where you'll come back; kill-and-hide where you're leaving; never kill where it'll be found.** All three are live choices across a campaign.

**For the mission checker,** an open case counts as `case-trail-eq` 3 T (rev 4, R4.4). **Heat stays T + Menace** (§06). A missing person adds 2 T and no Menace. The district's level S is a separate axis:
- Heat brings fights to the cell (raids, ambushes);
- S makes the district watch harder.

---

## 3 · Waking and talking (knockout)

### 3.1 When
- `wake-s` **120 s**. The preview shows "wakes m:ss".
- Then Pulse's `stagger-s` 4 s.

### 3.2 What he knows, and how it becomes Trail
- He holds memory act `attacked`: `place-time`, plus **`enforcer-face`** if he was aware, plus everything he held before.
- Fear +1. Resentment +1 only if he was aware.
- **Woken free in the mission:** he reports, whatever his temperament (Pulse §5). On landing: Alert ≥ 1, +4 T, and his facts become leads.
- **Not in the mission** (hidden or restrained to the end): he gets free after the mission and gives a **statement**:
  - +4 T;
  - his facts become leads;
  - questioned later, the talker test (fear 1, plus resentment 1 if aware, > bravery + loyalty) means he talks.

### 3.3 What stops it (Sky: hidden, restrained or turned; Pulse §5)

| | price | effect |
|---|---|---|
| **hidden** (in a stash) | the stash time | he can't walk to a phone. **He calls out every `call-out-s` 10 s**, heard within 20 m (Pulse): a hearer may report "a noise" (Alert 1), and the verify opens the stash. Silent only if nobody passes within 20 m. |
| **restrained** | **any operative, 8 s at the body** | he can't move or call out. On open ground he is still seen and found, and he reports when freed. |
| **hidden and restrained** | both | silent until a search, a servicing round or the mission's end; then the statement (§3.2) |
| **quieted** (the Fixer; idle study F2) | Pay $5,000 · 15 s · 0 T, or Pressure $0 · 15 s · +1 T, fear +1. Priced by temperament fit like any approach; a full Turn also works. | facts **sealed**. A sealed victim who comes home **gives no statement, so no +4 T**. Woken free in the mission, he still raises the incident (Alert 1) but gives no leads. |

Quiet (F2) is not built. Without it, "turned" means a full Turn at its normal price.

**Neither choice is always right:**
- a kill is quiet in the debrief, but loud if it is found during the mission, and it hardens the district if it isn't;
- a knockout is safe if found, but leaves a witness and 4 T, unless the Fixer quiets him.

---

## 4 · Carrying and quiet places

### 4.1 Carry

| | lift | pace | while carrying |
|---|---|---|---|
| Enforcer | `carry-s` 3 s | walk, 1.4 m/s (Vantage's carry times stand) | no run, no fire |
| Scout, Fixer, Tech | 3 s | **0.7 m/s** (`carry-untrained-cm-s` 70) | the same |
| put down on open ground | `drop-s` 1 s | | |

A carry that is seen is a `carry` report: Alert 2 when it lands (§2.3).

### 4.2 Quiet: Vantage §2.2, exactly
- **Hiding** comes from the world: stash-tagged containers and service doors.
- **Quiet** is computed every tick: `seenBy(p, t)` is empty.
  - The watchers are live cameras, the guard, the patrol, the response officers and robots, and every simulated person on the street within 13 m on a clear line.
  - **The rivals are not in it** (they don't report). Vantage keeps them as a separate column for display.
- An act is unseen when its whole span is quiet. A body on open ground is found at the first tick its spot isn't quiet.
- Pulse §7 adds the second clock: a seen spot is a report **in transit**, so a body moved before the unit verifies makes the report unfounded.

---

## 5 · Clocks and prices

### 5.1 Check-ins: every staff post (Sky 4; Pulse §4.1(3))
- **Every staff person with a post** (guards, patrol officers, watchers) **checks in by voice every 90 s** (`check-in-period-s`).
  - The marks are counted **from the start of their shift at the post**: deterministic, read from the routine, and taught by a Watch.
  - **The end of a post shift is a mark too:** the handover call.
- **A missed mark** is a report that lands at the desk **at the mark**: Alert ≥ 1, robots and the unit sent to the post and to the person's connections (§5.2). It is one incident with the takedown that caused it.
- This retires `check-in-s` 18 [B]. A guard shot unseen is now called in by the gunfire (45 s), not 38 s after he falls. The loud route gets up to 7 s of slack; **re-measure T2.**
- **The Handover's marks:**

  | guard | shift at the post from | marks |
  |---|---|---|
  | the door guard (`p-records-office-01`) | −28,470 s | **1:00, 2:30, 4:00**, and 5:30 (shift end) |
  | official-entrance guard 29 | −28,470 s (his consecutive post stops merged, as Keel builds it; rev 2 wrongly read the last stop) | **1:00, 2:30, 4:00**, and 5:30 (shift end) |
  | relief 74 | 5:30 | from 5:30, every 90 s |

### 5.2 Suspicion: an expected person who is missing (Sky 6; Pulse's behaviour)
- A person who **expects** someone at a meeting raises the alarm after **`suspicion-s` 20 s** at the place without them. Examples: Vale at his car expecting the driver; a relief guard expecting the outgoing one at the handover; a lover expecting a summons.
- That alarm is a report, delivered by Pulse's chain (a staff member nearby radios it): a **missing-person alarm, Alert 1**. It is one incident with the takedown it uncovers.
- **The alarm's deployment** (Sky: "to the scene and to where the alarm-raiser's connections are meant to be"; Pulse designs it):
  - the **first unit** goes to the scene;
  - the level's **A1 robots** (§5.3) go one each, nearest first, to the places where the missing person's ties and duties are meant to be **now**: his post, his car, his lover's stop.
  - Each robot holds for `search-s` 60 s and is a watcher there.
- **Vale cannot raise it while being Taken:** a held person reports nothing. So the Take must start before suspicion fires.

### 5.3 The response dial: values per district and mission, not code (Sky)

The dial is one rulebook table, `response.levels`, indexed by the district's **security level S**.
- **S = the district's base** (data in the district plan, by its character: harbour 1, civic 2) **+ cases** (§2.6) **+ the mission's own `securityBonus`** (mission data).
- Pulse designs how units and robots move. These are the counts and clocks:

| S | name | dispatch: landing → on the street | Alert 1 sends | Alert ≥ 2 sends | reinforce: +1 unit every | units, cap | seal after confirmation | Alert 3 adds |
|---|---|---|---|---|---|---|---|---|
| 0 | lax | 20 s | 1 robot | 1 unit (2 officers) | — | 1 | 180 s | — |
| 1 | routine | **12 s** (today) | 1 robot | 1 unit | **—** | **1** | **145 s** (today) | **—** (the loud fight's existing +1 officer at Alert 3 stays) |
| 2 | watchful | 10 s | 2 robots | 1 unit + 2 robots | 40 s | 3 | 145 s | +1 unit, doors lock |
| 3 | guarded | 8 s | 2 robots | 2 units + 2 robots | 30 s | 4 | 120 s | +1 unit, doors lock |
| 4 | swarm | 6 s | 3 robots | 2 units + 4 robots | 20 s | 6 | 90 s | +2 units, doors lock |

- **Fixed by existing rules:** a unit is `response-officers` 2, health 3 each. A robot is a patrol unit: health 4, pistol, trained.
- **Gunfire** keeps its existing reporter delays as its landing time: seen 18 − 12 = **6 s to land**; unseen 45 − 12 = **33 s to land**; a civilian shot 8 − 12 → **0 s**, so the unit is on the street after the dial's dispatch alone. **At S1 every recorded run is unchanged.** S1 has no reinforcement and no cordon unit; rev 2 wrongly gave it both.
- **The §06 cap** of 30 simulated combatants holds: reinforcements stop at the cap.

**How it scales across the campaign:**
- **Act I** (Fixer alone, Arms 0): districts at S1–2.
  - At S2 a fight-out meets 2 officers + 2 robots, then +2 officers every 40 s. The cell has 1–4 people and few guns, so **fighting out is a last resort** (Sky).
- **Act II–III:** S rises with cases and Heat-driven missions to 3–4.
  - The cell has grown faster if it built its network: Arms, Fighter contacts, Hero crowds that block police cars (§04, §06).
  - So fighting out becomes possible for a prepared cell, and still costly.
- The dial measures the city's fear against the cell's preparation (§06: "fights measure preparation").

**The Handover:** the civic base is **S2**.
- This makes the loud reference (`loud.json`) harder: +2 robots on the response, and a unit every 40 s.
- **Waymark / Suture:** re-record loud. If no loud win remains, use `securityBonus` −1 (S1, today's numbers) for the training mission. Either way it is data.

**The order card** (Glyph and Latch):
- **"Alert 2 · police in ~n s · +1 unit every 40 s"**, where n = the forecast landing (Pulse's reaction function: staff 5 s, a Loyal civilian ~22 s, "?" while the reporter's temperament is unknown) + the dial's dispatch + the walk from the security entry at `security-speed-cm-s` 2.2 m/s.
- Example: a body at the guard's post, found by a staff member: 5 + 10 + about 40 = **~55 s**.

### 5.4 Order cards (lines for Glyph and Latch)

**Takedown** (the Enforcer, on a person):

| line | knockout | kill |
|---|---|---|
| $ / time | 0 / 6 s | 0 / 3 s |
| from behind | ✓ / ✗ "he sees you: a fight" / ? | the same |
| seen by | known watchers' answers, "?" for unknown ones (the `sight` rule); people within 13 m now, with their verb once their temperament is known ("Dana, Loyal: reports in 22 s"; Pulse §3.6) | the same |
| if seen | Alert 3 on landing · police in ~n s | the same |
| if found | Alert 1 · +4 T | Alert 2 · police in ~n s · +6 T · Menace |
| if never found | comes home: +4 T and a statement | **missing**: +2 T · the area hardens · the case stays open |
| then | wakes m:ss · knows where and when (and the face, if aware) | never talks · ties: n (their names, if known) |
| clock | next check-in mark m:ss (a staff victim, once his routine is known) | the same |

**Carry to** (a ground hover with a body held): the carrier's pace; "seen on the way by"; the destination's kind ("stash: Dumpster (1 free)", or "open ground: found if seen (?)").

**Stash** (5 or 10 s): "hidden: found by a search at Alert 2 within 30 m, its servicing round (m:ss, if watched), or a call-out (restrain to stop it)".

**Restrain** (8 s) · **Quiet** (the Fixer; F2 prices, "?" for the fit until the temperament is known).

---

## 6 · The Handover (estimates: Waymark plays them in the sim)

Sources: probe walks (`bun run sim probe`), Vantage's carries (`quiet.json`, lift included), Pulse's clocks (§7.2: guard 74 enters the lane at 4:04 at R2 and reaches D1 at 4:27; the door has passers from 4:14), and the reference runs.

| reference win (today) | ends | $ spent | Trail | Alert | Enforcer idle |
|---|---|---|---|---|---|
| A, Help ×2 | 4:27 | 80,000 | 7 | 1 | 61 % |
| A, Pressure | 3:31 | 0 | 7 | 1 | 47 % |
| B, driver Paid | 7:40 | 70,000 | 4 | 0 | 81 % |
| loud | 3:53 | 0 | 8 | 2 | 52 % |

### 6.1 Route G: take the guard (viable; stays in the lane)

**The window:**
- The guard's marks are 1:00, 2:30 and 4:00.
- The lane is quiet until 4:04.
- The door has passers from 4:14.
- The entrance camera sees the post all mission, so it must be looped.
- The patrol sees the post 66 % of each 100 s sweep, so it must be rerouted, or the act must start inside a ~24 s gap.

**G-KO (the reference): take him just after the 2:30 mark and leave him on the ground at R2.**

| clock | who | what |
|---|---|---|
| 0:00–1:16 | Tech | runs to the cabinet (14 s); Loop on the entrance camera (+1 T) |
| 1:46–2:31 | Tech | Reroute (+1 T), timed so the patrol is away **2:31–4:26** (`reroute-away-s` 115) |
| 0:00–0:35 | Scout | Watches the guard: his cone, his facing and his check-in marks |
| 0:00–0:40 | Enforcer | to R2, the lane's north mouth, outside the door cone |
| **2:31–2:37** | Enforcer | knockout from beside the door (surprised) |
| 2:37–2:53 | Enforcer | lift and carry to **R2** (Vantage: 17 m, 14.9 s), put down 1 s; **no stash** |
| 2:53–3:03 | Enforcer | to the public door (~14 m) |
| 3:03–3:41 | Enforcer | Enter, 38 s (A 0) |
| 3:41–~4:22 | Enforcer | out by D1 with Vale, through the lane's mouth by ~3:49, to the car (38.8 s) |
| 4:00 | city | missed mark: **Alert 1**; 2 robots (S2) to the post and to the café (his next stop, where Inez is), on the street ~4:10 |
| 4:04 | city | guard 74 finds the guard at R2 and radios (5 s): a person lying, Alert stays **1** (the same incident) |
| ~4:22–5:00 | cell | safe fight: 5 rivals, **no guard ally** |

**Expected:**
- ends ≈ **5:00** · **$0** · Trail **6** (Loop 1 + Reroute 1 + assault 4) · **Alert 1**;
- the guard's statement: place and time, no face;
- Enforcer idle 0:40–2:31 ≈ 111 s of ~262 s ≈ **42 %**.

**G-fast: take him at 2:01**, as soon as the Reroute (started at 1:16) lands.
- Out of the lane by ~3:19; ends ≈ **4:30**; idle ≈ **34 %**.
- But the 2:30 missed mark puts robots on the forecourt at **~3:20**, exactly as the Enforcer crosses with Vale.
- A robot that sees the carry: Alert 2, and the run goes loud.
- It is a real gamble, and the Scout's Watch shows it coming.

**G-kill: just after 2:30 again, but the body must be stashed.** A dead guard found at R2 at 4:04 would be Alert 2, a loud run.

| clock | what |
|---|---|
| 2:31–2:34 | kill |
| 2:34–2:56 | carry to **D1** (Vantage: 21.8 s) |
| 2:56–3:01 | into the Dumpster, 5 s |
| 3:01–3:10 | **run** to the door (9 s; nobody is there to notice) |
| 3:10–3:48 | Enter |
| ~3:56 | through the lane's mouth: **8 s** ahead of guard 74 |
| 4:00 | missed mark: Alert 1; robots search at A1, which opens nothing |

**Expected:**
- **the Dumpster is emptied at 5:22** (Keel's build: the round's phase comes from the D1 Dumpster's world id; the rule is right). That is a deadline, not a flaw: the run must be over by 5:22. Expected end ≈ 5:00, which leaves ~20 s. If the fight runs long, the robot opens the Dumpster, reports in 4 s and the dead guard is confirmed (Alert 2). The Scout can Watch the round. **A kill that must outlast 5:22 goes through the D1 door** (10 s), not into the Dumpster;
- debrief: Trail **4** (Loop, Reroute, **missing 2**) · **no Menace** · Inez's worry (fear +1) · **next Office mission: +1 posted guard and +1 camera** · 1 open case in the district.

**So:**
- G-KO is safer in the mission and costs 2 T and a witness.
- G-kill is cheaper now and tighter on the clock, and it hardens the district for good.
- **Neither dominates the A routes:** both are slower (5:00 against 3:31 and 4:27), use the Tech's whole kit and lose the guard as an ally. G-KO's 6 T beats loud's 8.

**D2 and R1 are no help here.** They are 65–67 s carries from the post across the open square (Vantage). They fit café takedowns: Inez's door is 22–25 s from them, but she is watched every second (Pulse §7.3).

**Knobs for Waymark:**

| knob | try |
|---|---|
| `ko-s` | 5 / 6 |
| `stash-container-s` | 5 / 8 |
| `check-in-period-s` | 90 / 120 |
| S | 1 / 2 |

### 6.2 Route D: take the driver (Sky 6: takeable, and costly)

**Who must go** (Pulse §7.3, r016):
- Official-entrance guard **29** stands 4.1 m from the driver until 5:30 (marks 1:00, 2:30, 4:00, 5:30).
- **74** relieves him at 5:30, walking the lane 4:04–5:12.
- The **pickup camera** sees the bay.
- The **rivals** park 6.5 m away at 3:10.
- **Vale** expects the driver at 6:10.

**A quiet driver route on r016:**

| clock | who | what |
|---|---|---|
| ~1:29–6:48 | Tech | Loop on the **pickup** camera (needs idle-study T1, a loop target list), live from ~2:31 and held until after the Take (+1 T) |
| **2:31** (just after 29's 2:30 mark; before the rivals park at 3:10) | Enforcer | **29**. The driver sees it: as a Mercenary at Alert 0 he keeps quiet (Pulse row 6), but holds the fact and the face. The parked rivals may see it too, and they never report. |
| right after | Enforcer | the **driver** (aware: 9 s for a knockout, 6 s for a kill; a scream heard within 20 m) |
| ~2:45–3:20 | Enforcer | carries of 16 s each to D1 (Vantage), **both through the door** (10 s each). Not the Dumpster: it is emptied at 5:22, before Vale comes out. |
| ~3:15 | Scout | false tip on the rivals (+1 T): away until ~7:05 (`false-tip-s`) |
| ~4:05 | Enforcer | **74** in the lane, an ambush from behind as he walks; through D1 (10 s) |
| 4:00 | city | 29's missed mark: Alert 1; robots at the official entrance ~4:15, unfounded, searching to ~5:15, gone before Vale. If he were taken after 4:00 instead, his 5:30 mark would put robots at the bay through 6:10. **Their route to the post may run through the lane while 74 is being taken (~4:05): Waymark checks it.** |
| 5:50–6:10 | Vale | out, to the bay; **the Take starts at 6:10**, before his 20 s suspicion (6:30) |
| 6:48 | Enforcer | Take done (38 s); carry Vale to the car, ~57 s |
| ~7:45 | cell | fight: 4 rivals, back from the tip |

**Expected:**
- ends ≈ **8:20**, against the **8:30** close;
- **$0**;
- Trail as knockouts: Loop 1 + Mislead 1 + Take of an unturned car 2 + 3 assaults × 4 = **16**. As kills, hidden: 1 + 1 + 2 + 3 × 2 = **6**, plus **3 missing persons** (two Office posts harden; with one case already open, S rises);
- Alert 1.

**Possible, but costly.** It is dearer in Trail than loud (knockouts), or it hardens the Office district for the rest of the campaign (kills). It never beats Paying the driver (7:40, 4 T, $70,000).

**The fight-out version:** skip 74 and take Vale in front of him.
- 74 radios the abduction (Alert 3, landing in 5 s); at S2 police are on the street 10 s later and at the bay in ~55 s, with +1 unit every 40 s.
- The Enforcer carries Vale at walking pace (~57 s) and is caught at the car with police and reinforcements: **a last resort**, as Sky asked.

**Open, for Waymark and Keel:** the Take is not witness-checked today. Under one set of rules, an abduction seen within 13 m is a takedown seen (Alert 3 on landing). Re-measure route B (R3 has passers from 4:28) before adopting it.

---

## 7 · Rulebook entries (all `proposed: true`, tag S)

### 7.1 Values (mine; Pulse's 29 stand except where noted)

| id | v | unit | src · reason |
|---|---|---|---|
| `ko-s` | 6 | s | Sky: knockout possible · twice the kill |
| `kill-s` | 3 | s | brief 013; Vantage assumed 3 · the kill's one edge |
| `struggle-s` | 3 | s | §1.3 · an aware target fights back |
| `takedown-reach-cm` | ref `at-reach-cm` | cm | hands |
| `kill-trail` | 2 | trail | §06 per person · a kill = hit 2 + down 2 + 2 |
| `missing-trail` | 2 | trail | Sky 2 · a file, not a homicide |
| `found-kill-standing` | −1 | standing | Sky 1 · one tunable value |
| `wake-s` | 120 | s | Sky · two minutes |
| `restrain-s` | 8 | s | Sky · any operative |
| `carry-untrained-cm-s` | 70 | cm/s | Aster · half the walk |
| `drop-s` | 1 | s | open ground |
| `stash-container-s` | 5 | s | Vantage's assumption |
| `stash-door-s` | 10 | s | Vantage: an off-screen action |
| `search-stash-m` | 30 | m | = `patrol-break-m` · one block face |
| `search-stash-s` | 10 | s | one stash opened |
| `check-in-period-s` | 90 | s | Sky 4 · replaces `check-in-s` 18 [B]; marks from the shift start; the shift end is a mark |
| `suspicion-s` | 20 | s | Sky 6 · an expected person absent this long → an alarm |
| `area-watch-step` | 1 | count | Sky 2 · +1 posted guard and +1 camera in that block next mission, for a found killing or a missing person |
| `area-watch-missions` | 3 | missions | the hardening lasts while the case could still be fresh |
| `cases-per-level` | 2 | count | §2.6 · S rises 1 per 2 open cases |
| `case-level-cap` | 2 | levels | S never rises more than 2 above the base from cases |
| `case-close-missions` | 3 | missions | a found killing's case closes; a missing person's never does |
| `response.levels` | the §5.3 table | table | Sky: the difficulty dial is data · per S: `dispatchS`, `a1Robots`, `a2Units`, `a2Robots`, `reinforceS`, `unitCap`, `sealS`, `a3Units` |
| district `securityBase`, mission `securityBonus` | civic 2, harbour 1; Handover bonus 0 (−1 if the loud win must stay at today's numbers) | level | data, not code |

**Pulse's values changed:**
- `report-alert-assault` 2 → **1**;
- `verify-alert-down` 2 → **1**, and dead → 2;
- `dispatch` = the dial's `dispatchS` (12 at S1).

### 7.2 Sections, events, memory
- **`takedown`, `bodies`** (new): as rev 1, plus §2.3's report table as data: kind → Alert on landing, on verify.
- **Events:**
  - `takedown.lands` (surprised, struggle or fight);
  - `takedown.witnessed`: at sight, witness Trail and Standing; **no Alert**; the report goes into Pulse's chain;
  - `report.lands` / `report.verifies`: Alert per §2.3, the incident's maximum, the act's Trail on confirmation, the seal from a confirmation at Alert ≥ 2;
  - `victim.wakes`, `victim.quiet`, `post.missed` (the periodic check-in), `person.missed` (suspicion);
  - between missions: `bodies.surface` (knockouts come home and give statements; hidden kills → missing), `body.grieved`, `missing.worried`, `area.hardens`, `cases.count` (S).
- **`alert.rule`:**
  - silent acts raise Alert **on landing**, gunfire at sight;
  - one takedown is one incident;
  - a knockout never exceeds 1 by itself.
- **`security`:**
  - `checkIn.period`;
  - `response.levels`;
  - the seal at a confirmation at Alert ≥ 2 (gunfire: at arrival);
  - search opens stashes at Alert ≥ 2.
- **`witness.sees`:** for the new acts, a witness with loyalty ≥ `word-quiet-loyalty` adds no Trail.
- **`memory.acts`:**
  - add `attacked`, plus Pulse's `saw-takedown`, `found-body`, `saw-carry`, `heard-alarm`, `reported`;
  - **`memory.faceFacts`** add `enforcer: "enforcer-face"`.
  - **Keel:** this gives the face to anyone who already saw the Enforcer shoot. Scope it to the new acts if replays must hold.
- **people.rules:**
  - `talker.facts` add `enforcer-face`;
  - Pulse's `talker.reporter` stands.
- **`fight.safe`:** the guard helps only if never taken down.
- **The official car needs its driver:** with no driver it waits.

### 7.3 GDD sections that change
- **§02:** Trail from people taken down and missing persons; Time: takedown 3–6 s.
- **§03:** Enforcer: Takedown. All ops: carry, restrain, stash.
- **§04:**
  - the `enforcer-face` fact;
  - Standing: a found killing is the one unwitnessed act that moves Standing;
  - between missions: grief and worry.
- **§05:**
  - silent acts reach Alert through a report;
  - report points and the report chain (Pulse);
  - bodies and stashes;
  - check-ins for every post;
  - suspicion;
  - the **response dial**;
  - the seal at a confirmation at Alert ≥ 2.
- **§06:** the takedown; the cost per person covers it; fighting out against the dial.
- **§07:** route G; the driver route; the official car needs its driver; D1.
- **§08:**
  - missing persons and cases;
  - the area hardens;
  - the district security level S beside Heat;
  - the debrief lines.
- **§10 (Glyph):** the cards in §5.4, "police in ~n s", reporters in transit (Pulse §3.6).
- **§13:**
  - T2 re-measured (check-ins, the dial);
  - **T9:** on the Handover, an unseen takedown route wins with Alert ≤ 1; the same takedown with a witness who reports ends in failure or a loud run.

---

## 8 · For Sky: one merged list (Motive and Pulse)

**Decided by Sky (2026-10-16), and how they landed here:**
- **D1. A found killing gives Standing −1, as one value.** In §2.3 and §2.6.
- **D2. No auto-find; a hidden body is a missing person.**
  - A missing person costs +2 T and no Menace.
  - The block where he was last seen gets +1 guard and +1 camera for 3 missions.
  - His case never closes, and every 2 open cases raise the district's level by 1.
  - Design in §2.6; **please confirm the numbers.**
- **D3. The Enforcer's face is a talker fact.** In §1.3, §3.2 and §7.2.
- **D4. Check-ins every 90 s, learnable**, extended to every staff post; the shift end is a mark (§5.1).
- **D5. D1 and a Dumpster in the lane.** For Vantage and Splice.
- **D6. The driver is takeable.**
  - With suspicion (20 s) and the dial, it costs three takedowns, ends ≈ 8:20 against the 8:30 close, and is dearer than Pay or loud (§6.2).
  - Fighting out is a last resort at the civic level.

**Open, needing Sky:**
1. **Silent acts raise Alert when a report lands, not at sight** (Pulse's recommendation; I agree). Gunfire stays at sight. It changes the GDD's "violence seen by a civilian → 3 at once" for silent acts.
2. **A knockout never costs more than Alert 1.** Pulse proposed 2 for a confirmed living victim or a woken report. I hold 1: it is the knockout's whole reason to exist.
3. **The response dial** (§5.3): five levels S0–S4 as data, the district's base plus cases plus a mission bonus. The Handover at S2 makes the loud win harder (re-record `loud.json`); use −1 if Sky wants today's loud.
4. **Mercenary witnesses keep quiet until Alert 2** (Pulse §2.3). This is a strong choice: 50 of the 130 people in r016. I agree: "responds to Pay" read as "no stake until the city pays for words".
5. **Turned witnesses (loyalty ≥ 1) add no witness Trail** for the new acts (my answer to Pulse).
6. **Two hide levels in v1 (open, shut).** Pulse's `screened` (4 m) and a body-height sightline are [L]. I recommend two for readability.
7. **Call boxes placed by a world rule** (Pulse R-REPORT: every walkable cell within 40 m of a report point; 7 boxes on the civic block). More boxes mean fewer quiet places.
8. **No Tech action to cut a call-box line** (new power; Pulse and Motive agree).
9. **The bin servicing round** (Pulse: every 900 s, learnable) runs in the mission only, never between missions (D2).
10. **Content:** the kill stays off-screen in presentation. For Forma and Glyph to confirm.
11. **(rev 4) A Take at Vale's own car, with its driver turned, happens inside the car, unseen. A held Vale on foot looks wrong only to his kin or to someone within 3 m.** This keeps route B viable under the witnessed Take. The fallback, the driver driving them to the getaway bay, needs car travel.
12. **(rev 4) Leaving a knocked-out guard at his door is still the right play on the Handover's fast route,** because nobody can find him before 4:14. Sky's scene lockdown bites on slower or combined plays. If Sky wants carrying to be required on route G itself, a door's own staff would have to see their doorstep. That is a new watcher, and I don't recommend it.

## What Pulse's doc should change (for Aster to route; I have not edited it)
1. **§0.3, §3.3:**
   - `report-alert-assault` and `verify-alert-down` → **1**: a confirmed knockout and a woken victim's report stay at Alert 1;
   - add the call-out row (1);
   - the missed check-in is periodic (§5.1 here).
2. **§3.2 row "a missed check-in" and §4.1(3):** marks every 90 s from the shift start, plus the shift end; not `check-in-s` 18.
3. **§3.5:**
   - the seal starts on a confirmation **at Alert ≥ 2** (a confirmed knockout at 1 starts none);
   - during `search-s` at Alert ≥ 2 the unit opens stashes within 30 m (10 s each).
4. **§1 `by`, §5 "a face only if it is the Fixer's":** `enforcer-face` is now a fact (Sky D3). It goes to act witnesses and to an aware victim.
5. **§1 "a found body moves no Standing":** a found killing moves −1 (Sky D1).
6. **§2.2, §4.2 hide levels:** `screened` marked [L] for v1, unless Sky picks it (open item 6).
7. **§4.1(3) "everyone else is missed … between missions":** replaced by in-mission suspicion (§5.2, `suspicion-s` 20 s) and missing persons (§2.6).
8. **§3.2 `dispatch-s`:** read from the dial (`response.levels[S].dispatchS`). Also add the alarm's deployment to the connections and the swarm per §5.3's counts (Pulse is already on this).
9. **§2.3 row 3 (`cell`):** a turned witness also adds no witness Trail (my answer).

## Evidence
- `bun run sim probe`: the walks and cones in §6.
- `dotnet run --project sim/cli -- play contracts/missions/handover/routes/{route-a,route-a-pressure,route-b,loud}.json --json`: Trail 7/7/4/8 · Alert 1/1/0/2 · cash left 20k/100k/30k/100k.
- Check-in phases: computed from post stops in `sim/data/handover.people.json`. The door guard is at the post from −28,470 s, so marks fall where t ≡ 60 (mod 90). Guard 29 is at his post from −11,070 s, so t ≡ 0.
- Stops within 13 m of the driver: guard 29 to 330 s, then 74, both 4.1 m away; this matches Pulse §7.3.
- Vantage's `quiet.json` (carries: guard→R2 14.9 s, →D1 21.8 s; pickup→D1 16.0 s; guard→R1/D2 65/67 s) and Pulse's `probe-output.txt` (lane clocks, reporters) for every clock quoted.

## Handoff
- **Keel** (B): §1–§5 and §7, with Pulse §8 and Vantage §2. Done-check:
  - the entries land with these ids;
  - scenarios for knockout and kill × seen, reported, unfounded, confirmed, stashed, call-out and missing; a missed mark; suspicion; a dial level;
  - recorded runs unchanged at S1, except where §7.2 flags a change.
- **Pulse:** the nine items above; the swarm and the connections deployment against §5.3's counts.
- **Vantage / Splice:** D1 and the Dumpster; rivals as a separate column; call boxes (R-REPORT) if Sky approves.
- **Waymark** (D):
  - G-KO, G-fast and G-kill (§6.1); the driver route and its fight-out (§6.2);
  - the Dumpster's servicing phase;
  - re-record loud at S2; idle with `tools/missions/idle/`.
- **Suture:** T2 and T9.
- **Aster:** the merged list in §8 to Sky.
