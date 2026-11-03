# 054 · What a Watch shows you

**State:** design, revision 4, 2026-10-24 (Motive), written to Sky's answers to revision 3 (quoted at the end). Nothing is built. Revision 4 replaces §3 (sight: civilians see by a cone and care about violence only below Alert 2), measures it on the Handover with a probe (§3.7), and merges both steps into one engine bump for Keel (Done-check). Two small calls for Sky are in §8. Revision 4.1 (2026-10-24, after engine 0.28.0): §9 answers Keel's two open questions (a body at one's feet; the affair Watch). Owner: Motive. Next: Keel (rulebook and engine, one bump), Pulse (stop facings), Glyph (cones, mood mark), Fable (words), Waymark (scripts), then Latch (host) and Suture (questions).

## Why
Sky, 2026-10-23, after a Watch on Inez (the feed said "Watch · Inez · café door — Learned: where Inez goes, and when."): "I don't get what the point of this is - I watch Inez and don't really get any insight."

Revision 2 offered four things a Watch could show: sight, mood, her round, and who she meets. Sky kept mood (reading from any cause, option B) and a cone shown "on hover over, only though if it matters". He dropped the round and the tie for now, and dropped Watch wherever it would show nothing.

## 1. The answer in one line
A Watch on a person shows one thing you can act on. Otherwise it isn't offered.
- **Their mood toward the cell** (uneasy, scared or friendly). It tips one approach on the price card to "likely".
- **For security, their cone and check-in marks**, as today.

A civilian's sight needs no Watch. On hover their cone shows, but only when it can catch something the cell is doing or has armed (§3). Below Alert 2 that means only violence: ordinary people ignore everything else (§3.2).

Listen still gives the need, the secret and the exact temperament. Help and Pressure still need it.

## 2. Who can be watched: no empty Watches
### 2.1 The rule (Keel: a read over state, no new state but one field, §2.3)
Watch on a person is on the order list only when it would change something the cell reads:

| A Watch teaches | Offered when, all of these hold |
|---|---|
| **mood** | simulated, on the street, not down, not turned · the cell doesn't know their temperament (after Listen the card is exact, so mood adds nothing) · their mood (§4) isn't neutral · it isn't the mood the cell already saw |
| **sight and marks** | security (the door guard, a job in `witness.staff`) whose sight, or, at a post, whose check-in marks, the cell doesn't know |

- Machines are unchanged: cameras, the patrol and containers.
- Anything else is refused with a new code, **`nothing-to-see`**, at no cost. The host never draws it.

### 2.2 How the order list shows it
- **No Watch to give: the row is absent, not greyed.** The action panel, the ring and verbs 1 to 4 skip it. ContextRank never picks it. The Intel card's next step goes straight to Listen.
- **After a Watch:** the row is greyed with ✓ "Already watched", as today (Sky, 2026-10-18). If the person's mood later differs from what the cell saw, the row comes back.
- **The gate:** today Listen and Pay need "routine known" (`scout.listen`, people.rules `approaches.pay.requires`). The new rule: they need a Watch first only when a Watch is offered for that person. The "Watch first" lock only ever sits beside a Watch row the player can see.
  - So the driver, who has no Watch to give (§6), can be paid or listened to straight away.
  - Inez, who has a Watch to give, still has it first, so route A's chain and timing stand.
  - A person who was never watched and turns uneasy or scared mid-mission now has a Watch to give, so Listen and Pay on them wait for it. That's honest: there is now something to see. A person already watched never re-locks.

### 2.3 What a Watch stores
- **The mood at the Watch's end.** The cell knows what it saw (knowledge is witnessed or told, §04), not a live feed.
- It's one knowledge field per person, hashed only when set. The run hashes of scripts that Watch Inez move; no outcome does.
- `KRoutine` stays the "watched" flag: it opens the guard's clock pip and a staff victim's next mark.

### 2.4 The cost: a free tell (Sky accepted it, 2026-10-24)
Whether the row is there says "this person has a mood" before any Watch:
- In a district the cell hasn't touched, the only mood at the start is the Fearful temperament's fear 1.
- So at the start, a Watch row on a civilian means Fearful.
- This is Sky's accepted (B) tell, now at no cost instead of 20 s. The row coming back mid-mission likewise says "their mood changed".

It's still a small tell:
- Help and Pressure need the need, so Listen stays on every turning route.
- Pay on a Fearful person is one step worse (+$20,000), and the card already shows that worst case.
- The purse lesson stands (§6).

### 2.5 "Her own life" is her fear, not her secret or her debt
Sky: "she's nervous coz having an affair and in debt etc." In the engine that nervousness is fear: a Fearful person starts at fear 1.

I don't read a secret or a need into mood:
- **A need:** all 130 people in the Handover cast carry one, so everyone would read uneasy.
- **A secret:** it would show the secret before Listen.

The affair and the debt stay Listen's to tell. The Scout's line can say "jumpy", not why.

## 3. Sight and care: a cone, and only violence until Alert 2 (revision 4)
Sky, 2026-10-24: "Ordinary people shouldn't really care unless there's high alert status - think real world. And yeah, would be in cone area only for seeing."

**The rule in one line:** an ordinary person sees only what is in front of them, and until the city is on edge (Alert 2) they react only to violence. Security watch as they do today, at every Alert. Sounds reach everyone.

### 3.1 When the cone shows on hover (a host read, nothing hashed)
**Hover a civilian:** their cone draws only if, under §3.2 at the Alert now, they would notice something the cell is doing or has armed:
- **(a) an armed or aimed order counts them on its eye.** That's the takedown card's people (`TakedownQuote.People`), the Pressure card's witnesses (`Quote.Witnesses`, which needs ids, not just a count) and a draw's or a carry's witnesses.
- **(b) an operative they would see is doing a seen act now.** That covers carrying a body or Vale, a drawn weapon, or a takedown in progress.

Otherwise hover shows the name and the mood mark only. No cone.
- **So at Alert 0 or 1, an armed Pressure draws no civilian cones:** they don't care. Security on its eye draw theirs.
- **Security:** as today. A Watch teaches their cone. They are drawn whenever an armed act would count them (§3.4).
- **On Alt:** the cones of everyone on the armed order's eye, capped at the 6 nearest.
- **At rest:** nothing new.

**Draw:** one wedge at a time, rim only, clipped by walls and decks (brief 049: no big filled shapes).

### 3.2 Who notices what: act × Alert (Keel, rulebook `witness`, `perception`)
"High" is **Alert 2** (search: any gunfire, a found kill, a second incident). It's the same mark at which a Mercenary starts reporting (`mercenary-report-alert` 2), so there's one "the city is on edge" line. New value **`people-care-alert` 2** [S].

| Act (rulebook id) | Civilian, Alert 0–1 | Civilian, Alert 2–3 | Security, every Alert | What noticing does (unchanged) |
|---|---|---|---|---|
| **Listen, Ask, Pay, Help** (`privacy`: private) | never | never | never | nothing; the subject's own report stands |
| **Watch, standing still, loitering** | never | never | the attention clock (`attention`) | as today |
| **Loop, Reroute, Mislead, Call, Enter, Board** | never | never | cameras, guard and patrol as today | as today |
| **Running** (`cell.runAlert`) | never | in their cone: remembers the runner, a sighting with the face (`saw-runner`) | found fault, as today | civilians raise no Alert; security as today |
| **Pressure** (`threat`) | never | in their cone: `witness.sees` | sees it, as today | +1 T once, Menace step, `saw-act` |
| **Vale's escort, the grip** (rev 4 R4.2) | kin only (13 m, in their cone) | anyone within 3 m, in their cone | within 3 m, as today | a carry seen: Pulse's list, report → Alert 2 |
| **A drawn weapon** (`weapon-drawn`) | in their cone | in their cone | as today (cone watchers engage) | startle and flee; +1 T, Menace step, Alert ≥ 1 |
| **A takedown, the Take's struggle** | in their cone | in their cone | as today | Pulse's list; report → Alert 3 |
| **Carrying a body or Vale** | in their cone | in their cone | as today | Pulse's list; report → Alert 2 |
| **An open body** | in their cone, or within 1.5 m any facing (§9.1) | the same | as today | Pulse's list |
| **Gunfire, a person shot, a car blown** | heard: within 13 m on a clear line, any facing (the bang turns heads) | same | same | `saw-gunfire`; a person shot seen → Alert 3; startle within 30 m with no line (`crowd.scatter`) |
| **A scream, a stash call-out** | heard within 20 m; within 13 m on a clear line, heads turn and they see what's there | same | same | the seen act by Pulse's list; the rest hold `heard-alarm` |

- **Once noticed, the reaction is Pulse's list, unchanged** (`witness.rule`, `pulse-witnesses.md` §2.3): staff radio, kin report, the cell's people keep quiet, an Informer reports, the terrified flee, Loyal report, Idealists check then report, Mercenaries keep quiet until Alert 2, Fearful flee or avoid and tell a tie.
- **Temperament shades the reaction, not the notice.** A Fearful person still flees violence and tells a tie. Before Alert 2 a takedown seen only by Mercenaries and Fearful people raises no Alert, as today.
- **Every act a witness path asks about must be in one row.** Unknown = reject.

### 3.3 The cone and the facing
**The cone:** a civilian sees a point within 13 m (`witness-range-m`) and within ±60° of their facing, on a clear line at eye height.
- Keel reads it as the existing cone test (side ≤ half × ahead ÷ reach) with reach 13 m and half **`people-half-cm` 2252** (13 m × tan 60°), plus the 13 m distance, so the shape is a true sector, not a triangle reaching 26 m at its corners.
- No near sense: Sky's "cone area only". Rev 3's 3 m all round is dropped for civilians. One exception, for bodies only: a body within 1.5 m is found whatever the facing (§9.1).
- The measurements keep ±60° to 13 m (§3.7): no Handover outcome depends on a facing the data doesn't already give.

**The facing (Pulse generates it; Keel reads it, hashed):**

| The person is | They face |
|---|---|
| walking a routine leg | the way they walk (today) |
| at the end of a leg, early, waiting | the facing of the stop they've reached |
| seated | the chair's `face` (`contracts/people/handover.seats.json`, now an engine input) |
| a server on her round | the round's `face` (`sim/data/handover.rounds.json`, already read) |
| at a door stop on the street | out of the door: the entrance's stand minus its point |
| at a stand by a unit (the café's stands) | the unit's door |
| at a bay, a kerb or a transit stand | the kerb |
| held in an approach | the operative speaking |
| startled | what startled them |
| fleeing, reporting, checking, returning | the way they run (today) |
| the door guard | his post's or the café's facing (site `guard`, today) |
| the driver at his stand; Vale at the bay | their car |
| Vale walking out | the way he walks |

- **A stop with no facing is rejected at load.** On the Handover that's the 63 + 24 stops at `t1-records` (off the block), 7 at `s1-gardens` and 5 at `office-bay`: Pulse gives them one.
- **Stop facings also feed two existing reads.** A security person on foot at a stop now sees a run by his facing cone, not by the 3 m near sense alone (engine 0.22.0's stand-in). A stopped takedown target can now be aware, so it struggles (+3 s) and screams (motive-takedowns §1.3: "until Pulse gives a stop its facing").
- **A civilian target's awareness uses the same people cone**: aware if the Enforcer is in it at the order. Rev 3's "a witness stays aware" stands.

### 3.4 Security keep their attention
Security means anyone with a job in `witness.staff` (guard, patrol-officer, watcher, investigator) and the door guard, on shift or off.
- **What they see:** acts as today, all round within 13 m on a clear line, plus their cones, at every Alert. The Alert gate never applies to them.
- **Runs:** their run rule, the facing cone plus 3 m, now with the stop facing at a stop.
- **Why unchanged:** they are the counterweight (§3.8). Measured alternative: security by the guard cone + 3 m instead (§3.7). It makes the café's off-duty guards miss the Pressure, and guard 29 at the official door miss a body to his side. Not recommended.

### 3.5 The price never lies (Keel)
**One function:** `Sees(person, point, act)` decides who notices, in the engine and on every card. A card counts exactly the people the engine would.

**Every card that counts people reads it:**
- the takedown quote (`People`, verb and landing)
- the Pressure quote (`Witnesses`, with ids)
- a draw (`unseen-draw` / civilians)
- a fire order (civilians who'd see the person shot)
- a move (runs: security; at Alert 2 or more, civilians as "remembers")
- a carry and a drop (who would see the body)

**Over the act's whole span:**
- The card tests each tick of the act (a takedown's 3 to 9 s, a carry's walk), reading routines ahead: positions and facings, as the server's round is read ahead today.
- So the forecast equals the outcome unless something else happens first (another act, a shot). That keeps the existing "~".

**Below Alert 2:** an odd act (running, Pressure, the grip) counts no civilian, and no civilian cone draws for it.

**Investigators:** the "who had a sightline" read (`PeopleNear(at, t)`) uses the same rule at tick t: facing at t, and the Alert at t for odd acts.

### 3.6 Replays
- **The switch:** behind a rulebook switch, `perception.people.sees`: `around` or `cone`. Absent means `around`, so every pinned run replays byte-identical.
- **The new value:** `people-care-alert` is read only under `cone`.
- **Determinism:** facing is derived from paths and fixed stop data, so the same orders give the same result.
- **Hashes:** every inputs hash moves. The rulebook, the people file (stop facings) and the seats file are inputs.

### 3.7 What it moves on the Handover (measured)
**Method:**
- **Probe:** the working tree's engine (suborned.sim/0.27.0) copied to `out/scratch/motive/b054/`, with the rule patched into the copy behind switches (`briefs/054/motive/probe-r4.patch`).
- **Scripts:** the 38 route scripts snapshotted from `contracts/missions/handover/routes`, played with `bun run guard … batch`.
- **Baseline:** the switch off replays every final hash byte-identical to the unpatched copy. The tree's baseline matches c031's outcomes.
- **Facing proxy:** stop facings come from the seats, the rounds and the door, café and car rules of §3.3. Every other stop was tried three ways: a hashed facing, facing the act and blind. All three give identical outcomes.
- **Output:** `briefs/054/motive/probe-r4.txt`.

| Script | Today | With the rule | Why |
|---|---|---|---|
| 31 of 38 scripts (the three marked unchanged below among them) | | identical outcome, end, T, A, Standing | no civilian faces their acts, or security saw them anyway |
| **route A-pressure** (+board) | win 4:00.7 (3:37.9), 7 T, A 1, Standing 2 | **unchanged** | 9 saw the threat; now 2. The 7 residents at the café tables don't care at Alert 0, but the second-shift guards p-records-office-73 and -74 take coffee at table 1, and security sees it |
| **loud**, loud-fast, loud-left ×2 | win, 14 T, A 2, Standing −2 | win, **13 T**, A 2, **Standing −1** | the one civilian who saw the draw at 2:31 faces away. The shots are heard, so Alert 2 and the reports stand |
| loud-fight | win, 13 T, Standing −2 | win, **12 T**, Standing −1 | the same |
| fail-district-seals | sealed 5:05, 9 T, Standing −1 | sealed 5:05, **8 T**, Standing **0** | the same draw |
| **careless-takedown-seen-crowd** | fails enforcer-down 6:53.9, 8 T, A 3, Standing 0. 11 civilians see the 5:31 takedown; first report lands 5:39.2 | fails enforcer-down **6:32.9**, **7 T**, A 3, Standing **1** | the 5:30 clerks walk away from the door with their backs to it. Guard 73 (security) radios and it lands at 5:36.0, sooner. Alert 3 locks the doors before the Enforcer's Enter (5:37, refused), so no Vale carry and the fight comes sooner |
| **careless-reporter-not-stopped** | win 7:59.6, 3 T, A 2 | **unchanged** | its reporter is the door guard, who finds the body at 7:38 (security) |
| fail-vale-dead | | outcome unchanged; 1 of 2 sees the draw | |

**Fight lab** (`fight-lab`, brief 027):
- 90 rows in the today and lab presets: 11 move numbers. No verdict moves (all 270 lines), and no 6.4 trigger of today or lab moves.
- What moves is the draw's witness: L1 −2 T and Standing −1 → +1; L1c, L2, the tight kites −1 T and +1 Standing.
- Today's L4 gains 1 T. The lab's L4 ends 14 s later.
- Because the shot turns heads, Alert 3 for a shooting seen stands (without it, today's L3 and L4 fell to Alert 2).

**Witness counts** (reactions, then reports landed):
- crowd-seen takedown: 27 → 13, and 16 → 3
- Pressure on Inez: 9 → 2 witnesses
- the loud draw: 1 → 0

**Mission conditions, by hand on these numbers:**
- **MC-2 holds:** loud 13 + 3 (case) T, more than the dearest quiet win's 7.
- **MC-7 holds:** the crowd-seen twin still fails at Alert 3.
- **MC-9 holds:** no takedown win moves.
- **MC-10 holds:** quiet 3–7 T, A ≤ 1, Standing ≥ 2, against loud 15–16 T, A 2, Standing −1.
- **MC-1, MC-5, MC-6, MC-12:** nothing moves.
- **T9 holds in outcome.** Its "witness who reports" is now a guard, not the crowd (Waymark, below).

### 3.8 The capstone, and what's now free
**The Handover still holds as the capstone.**
- No quiet route moves: A, B and G keep their times, Trail, Alert and Standing.
- The Handover is watched by security: the cameras, the patrol, the door guard, guards 29, 73 and 74 at the Office doors, and the off-duty guards at the café. All of them are unchanged.
- Quiet routes don't get easier here. That's the honest answer to "expect quiet routes to get easier".
- Loud gets 1 T and 1 Standing cheaper, because nobody civilian faces the draw. It's still dearer on every count.

**What's now free, in general (not on the Handover):**
1. **Pressure below Alert 2 is priced only by security in sight.**
   - Route A-pressure keeps its 7 T only because two guards are having coffee at table 1. That's a cast coincidence.
   - Pressure keeps its own price: +3 T, the subject's fear and resentment (a Talker), and a Loyal subject's own report.
   - This is the real-world reading Sky asked for, but it reverses part of a ruling (§8.1).
2. **A takedown behind a lone civilian's back is unseen.**
   - That's the decision the cone exists to make (T4: "players route around crowds on purpose").
   - Walkers who approach a body still find it: a body is checked every 0.5 s, and a walker heading toward it has it in their cone.

**Counterweight:**
- **None is needed for the Handover now.** The capstone's numbers don't move.
- **If play shows quiet takedowns or Pressure too free in generated missions, use what exists.** No new system or number now:
  - **Guards:** security sees all round within 13 m, so a posted guard is now the main witness. Each found or missing case already adds a posted guard and a camera to the block (`area-watch-step`).
  - **The dial:** a district's `securityBase` +1.
  - **Cameras and the patrol:** routed over the quiet places.
- **And a check, not a system:** every generated mission keeps a seen-takedown twin with a civilian who faces it and reports (MC-7, T9). So the generator can't build a district with nobody looking.

## 4. Mood (revision 2's §3, with Sky's (B) decided)
### 4.1 The read: no new state but §2.3's field; the first row that holds wins
| Mood | When |
|---|---|
| **scared** | fear ≥ 2, or remembers being attacked, or saw (or was told of) violence, a takedown or gunfire by the cell |
| **uneasy** | fear 1, or resentment ≥ 1, or remembers a threat (pressure, saw-act), a drawn weapon, a found body or an alarm (or was told of one); a security watcher's wary stage |
| **friendly** | loyalty ≥ 1, or remembers being helped or paid by the cell |
| **neutral** | none of the above: no mark, and no Watch to give (§2) |

- **(B), Sky's call:** fear counts whatever its cause. The Fearful temperament's fear 1 reads uneasy.
- **What stays out:**
  - A refusal: it already has its own price line.
  - District Standing: it already moves turning by band, and counting it twice at +2 would make everyone friendly.

### 4.2 It moves the fit by one step; temperament still sets it
| Mood | Help | Pay | Pressure |
|---|---|---|---|
| friendly | +1 | +1 | −1 |
| uneasy | −1 | 0 | +1 |
| scared | −1 | 0 | +1 |

- **Uneasy lifts Pressure (Motive, 2026-10-24, correcting rev 4).** Sky, 2026-10-23, as relayed by Aster: "those that are wary would know you need to pressure rather than help." Rev 4 gave uneasy Pressure 0, while §4.3 and §7 said "Pressure likely" for uneasy. Fable found the mismatch, and the brief's card wins: uneasy is +1 on Pressure, the same as scared. For a Fearful person this counts her fear twice: the temperament already suits Pressure, and her fear 1 reads uneasy. The cost is one Trail on her Pressure, and the Watch then shows something real on the capstone. Keel: `mood.shift.uneasy.pressure` 0 → 1. Waymark: re-record the A-pressure family.
- **The steps:**
  - A step up from suits is **ready**: Help one order fewer (at least one; already at one: −$10,000), Pay −$20,000, Pressure −1 T.
  - A step down from suits is the existing **worse**.
- **The limits:**
  - Mood never lifts refused.
  - Mood never makes anyone refuse: worse is the floor.
  - Mood never moves Ask.

### 4.3 The card
- **Before a Watch:** as today. The price always shows, with "?" where temperaments differ.
- **After a Watch:**
  - The mood mark sits on the person's row.
  - The approach the mood favours wears **likely**, a dashed ✓: Pressure for uneasy or scared, Help for friendly.
  - The approach it hurts shows a dashed ½.
  - Amounts the temperament decides stay "?".
- **After Listen:** every mark is exact.

## 5. Names do the "who she meets" job
Sky: "we would use character names to help user realise that's the person."

**Where names do it today:**
- **The guard's card** (`text.json` `targets.guard.about`): "Takes his coffee at Inez's café…". It names her.
- **Inez's card** (`targets.waitress.about`): "People say she's close to the door guard." It names his role.
- **Listen's result:** "Secret: she's seeing the door guard."
- **The Intel card's secret line** (`HudModel.cs:115`): "Secret: seeing {TargetName(lover)}", which today reads "seeing The guard".
- **Speech:** the guard says "Inez" in the Listen scenes. Inez says "officer", never his name.

**What's missing:**
- **The guard has no name in play.** The cast names him Folake Okafor (p-records-office-01), but `targets.guard.name` is "The guard". The driver, Mei Zhou, is "The driver".
  - Give both their cast names in `names.targets`, for example "Folake, the door guard". Then Inez's secret line, her card, his hover and the Listen lines all say the same name, so the player links them.
  - With one door guard in the Handover the role word works today. In generated missions with many guards it won't.
- **Pronouns:** the text calls the guard "he". The cast carries no pronoun field, and Folake is often a woman's name. Fable and Datum should check before the name goes in.
- **A promise the game never keeps:** the guard's card says he takes coffee at her café "before he goes off shift". His café stops are before 0:00 and at 8:30, the close, so the player never sees it. Fable should reword it or drop it.

## 6. The Handover: who can be watched, and the numbers
**At the start:**
- **Inez:** yes. She's uneasy (Fearful, fear 1), and Pressure shows "likely".
- **The door guard:** yes: his cone and check-in marks, until watched (route G).
- **Other security staff on the street at a post:** yes, the same.
- **Eighteen other Fearful people, while they're on the street:** yes. Another café server, two cooks, seven clerks, five residents, a kiosk keeper and two attendants, all uneasy.
- **The driver (Mei Zhou):** no. She's Mercenary, fear 0, so neutral. Her sight needs no Watch.
- **Vale:** no (neutral).
- **The other ~94 civilians:** no.

**Mid-mission:**
- Anyone who sees a threat or the cell's violence, or is told of it, turns uneasy or scared and becomes watchable.
- Anyone turned leaves the list.
- Inez stays uneasy after a first Help (not yet turned): fear 1 outranks friendly.

**Numbers:**
- **Route A** (Help ×2, 4:22.8, $80,000, 5 T): unchanged. Her Watch is still offered. Uneasy hurts Help, but Fearful Help is already worse, the floor.
- **A-pressure** (3:29.9, $0): 7 T → **6 T**. Uneasy lifts her Pressure from suits to ready (−1 T), whether or not she was watched. The Watch is what lets the card say so ("pressure likely").
- **Route B** (Pay the driver, 7:39.3, $70,000, 3 T): unchanged numbers, but **eight scripts carry a Watch of the driver that would now be refused** (`nothing-to-see`): route-b, -board, -block, -block-board, careless-reporter-not-stopped, careless-scout-idles-pickup, fail-rivals-reach-vale, fight-safe-rivals-reach-vale.
  - Waymark drops those orders.
  - Pay still lands at its tick and no longer needs the Watch.
  - The idle Scout in careless-scout-idles-pickup still stands in the camera's sight: idle and watching run the loiter clock alike.
- **Purse lesson** (§04: pay + driver $110,000 > $100,000): stands. Nobody is friendly at the start, so no Pay is cut.
- **T2** (informed quiet faster): no time moves.

**`bun run missions:check`, the Watch set and mood alone:** no time, T, A, Standing or outcome moves. The inputs hash moves, and so do the run hashes of scripts that Watch Inez (§2.3). The cone and care rule's moves are measured in §3.7.

**Fight lab and range:** the Watch set and mood read no approach fit and no knowledge of people. The cone moves 11 fight-lab rows' numbers and no verdict (§3.7). The range has no crowd.

## 7. Words and marks
**Glyph** (Suborned HUD kit):
- **Cone on hover:** a wedge rim, thin, clipped by walls and decks, no fill, one at a time; Alt shows the armed order's people (cap 6).
  - **Who shows one** (§3.1): a civilian only when §3.2 says they'd notice the armed or active act at the Alert now. So below Alert 2 an armed Pressure, a run or Vale's grip draws no civilian cone. Violence does: a takedown, a carry, a drawn weapon.
  - **Civilians:** a ±60° wedge to 13 m with no near ring. When the armed order leaves or drops a body, add the 1.5 m body ring at their feet (§9.1).
  - **Security people:** they see acts all round within 13 m (§3.4). Draw a thin 13 m ring rim, clipped, when an armed act would count them. When a run is armed, draw their run cone (the guard's facing cone with its 3 m ring). The door guard and the machines keep today's cones.
  - **Gunfire:** no cone. A shot is heard all round; the existing scatter cue covers it.
- **Mood mark:** three faces in one family, each its own shape, not colour alone: friendly (open), uneasy (a side glance), scared (drawn in). Neutral has no mark. At most 2 icons over a person.
- **The card:** a dashed "likely" ✓ and a dashed ½.
- **Order list:** no row for `nothing-to-see`. Verb slots close up or stay empty: Glyph and Latch decide which.
- **Test frame:** Inez at play zoom, yaw 30 and 210: at rest, hovered with nothing armed (no cone), hovered with a takedown armed near the café (cone), and on Alt. Add: a café customer hovered with a Pressure armed at Alert 0 (no cone) and at Alert 2 (cone); an off-duty guard at table 1 hovered with the same Pressure (ring).

**Fable.** These strings promise a Watch, a round or a tie that's gone. In `contracts/missions/handover/text.json`:
- `actions.watch.does` "Learn where they go and when." Change it to mood or sight.
- `actions.watch.onTarget`:
  - waitress "Learn Inez's rounds: door, tables, and when the guard drops in.": change to her mood.
  - guard "…and his coffee break.": change to his cone and marks.
  - driver and councillor: delete. There's no Watch on them at the start, and if one appears later it falls back to `does`.
- `targets.*.watched` for the waitress, driver, guard and councillor: same treatment.
- `needs-routine` "Learn {who}'s routine first: Watch them.": change to "Watch {who} first." (verbs listen, pay; short "Watch first" stays).
- New code `nothing-to-see`: no line, never shown.
- The `overheard` Watch scenes:
  - Retire driver and councillor.
  - Retire the waitress scene and the guard's café-visit variant, or generate them from the mood.
  - Their result "Learned: where {who} goes, and when." becomes "{who} · {mood}".
- The `read` bark's trigger note ("Watch the routine"): change to "Watch: the mood, or a watcher's sight".

**Fable · lines that promise a civilian notices something** (§3.2; in `text.json` unless named):
- `targets.crowd.about` "Just passing. They notice and remember what you do." This is no longer true below Alert 2. It should say: they see what's in front of them, they react to violence, and anything else only once the city is on edge.
- `actions.takedown.does` "Quiet if unseen; anyone who sees reports it." Not everyone reports (temperament), and only those facing it see. Fable rewords if the short form misleads.
- `actions.drive.does` "Anyone nearby still sees them." Make it guards and cameras.
- `actions.enter.does` "If anyone sees you go in, you fail." Only security fails it (already true today). Fable rewords.
- `events.witness-screams.trigger` "heard within the scream range with no line": add that hearers within 13 m on a clear line turn and see.
- `events.drawn-seen.trigger` "a witnessed incident for people": add "by their cone".
- `events.run-seen.speakers` includes `crowd`: a civilian never makes a run a found fault. At Alert 2 or more they only remember it, which is a silent log. Keep `crowd` only if it voices a crowd guard.
- `debrief.standing.causes.seen-threat` "A threat was seen.": still right (security, or anyone from Alert 2).

**Fable · new words:**
- Moods: Friendly · Uneasy · Scared.
- Card tag: likely.
- Feed: "Watch · Inez · uneasy · pressure likely".
- Scout's line (≤ 10 words, from the facts): "She's jumpy. Leaning on her would work."
- Names as in §5.

**In Unity code** (Fable's words, Latch's edit):
- `HudModel.cs:117` "Watch: learn the routine"
- `HudModel.cs:426` "Learns their routine"

## 8. What Sky decides
Decided (revision 3 answers): the cone (civilians, no near sense), "don't care unless high Alert", the free tell, Listen and Pay gated only where a Watch is offered, and the guard's and driver's names.

Two of the act × Alert rows refine earlier rulings, so Sky confirms them (via Aster):
1. **Pressure is unseen by ordinary people below Alert 2** (§3.2). Sky ruled on 2026-10-16 that conversations are private "unless it's pressure". Under this rule that exception holds for security at every Alert and for civilians from Alert 2. Before that, a threat at a café table is minded-your-own-business.
   - **Recommended: yes.** It's Sky's real-world reading.
   - **Measured:** on the Handover nothing moves, because two off-duty guards at table 1 still see it. In general Pressure below Alert 2 is priced by security in sight, plus its own +3 T and Talker.
   - **The alternative:** Pressure is seen by the cone at every Alert, like a drawn weapon. Measured: the same Handover numbers, with 3 of the 7 café residents facing it.
2. **Civilians remember a runner from Alert 2** (§3.2). Sky ruled on 2026-10-22: "running only in front of camera, patrol bots and guards get you noticed, anyone else fine". This keeps that below Alert 2. On edge, a civilian who sees a run keeps the face as a lead, but raises no Alert and gives no found fault.
   - **Recommended: yes.** It's the twitchy city at almost no cost.
   - **No:** the row reads "never".

## 9. Keel's two questions on engine 0.28.0 (Motive, 2026-10-24)
Keel built §2 to §4 as engine 0.28.0 and left two calls open: N68's OPEN note, and the affair row in `WatchTeaches`.

### 9.1 A body at your feet: a 1.5 m near sense, for bodies only
**The problem.** With no near sense, the clerks who step out of the Office door after the 5:30 chime never find the dead guard. He lies at his post, which is the door point itself (`d:Office entrance` (64.00, −41.25); the door's stand is 25 cm east). Everyone who comes out starts on top of the body, facing out of the door or along their walk, so the body is always behind them. **No facing fixes this.** The stop facings Keel generated are right; a body lying in a doorway is behind anyone who leaves through it.

**Real world:** you step over a body in a doorway, or past it, and you notice it. Sky's "cone area only for seeing" is about what people pay attention to across a street. It isn't about what is under their feet.

**The rule.**
- A civilian finds an **open body** within **`people-near-body-cm` 150** [S] on a clear line, whatever their facing. Outside 1.5 m the cone rule stands.
- **Bodies only.** A takedown, the Take's struggle and a carry stay cone-only: an aware victim's scream already turns heads (§3.2). I measured adding them too; it moves `careless-takedown-seen-crowd` (+1 T, −1 Standing, a later end), and it isn't needed.
- At every Alert: a body is an always act.
- Security is unchanged (all round within 13 m).
- A stashed body isn't open, so stashing still hides it. Stashing stays the answer to "I left him by the door".
- **Why 1.5 m:** one stride either side of a walker. At walking pace (1.4 m/s, 0.7 m between the 0.5 s body checks) anyone who passes within about 1.3 m of a body is sampled inside the ring. 1.0, 1.2 and 1.5 m measure the same on everything checked (below); 2.0 m also moves S9-CROWD's hash, and 3.0 m moves a route's hash (`careless-takedown-seen-crowd`). 1.5 m is the widest value in the flat band.
- **One test:** it lives in `Notices` (the `body` act), so the takedown card's "found" forecast, the carry and drop cards and the investigators' `PeopleNear` count the near finder with no extra code. The price never lies (§3.5).

**Keel, rulebook and engine:**
- `witness.notice` gains `near: { acts: ["body"], cm: "v:people-near-body-cm" }`, and the new value `people-near-body-cm` 150 [S] (src: Motive, brief 054 §9.1). Every act in `near.acts` must be an act in `always`. Unknown = reject.
- `NoticesAt`: for a civilian, an act in `near.acts` within `near.cm` with `Sees` is noticed before the cone test. The body's line test is the same as today's: the body counts as a standing person until brief 055's lying-body height lands. Even then, a 1.5 m line to the ground is clear unless a solid stands between.
- `PersonSight` carries `NearCm` (150; 0 when there is no near row), for the host.
- The `witness.rule` text says it: "a body within 1.5 m is found whatever the facing".
- **N68:** under the cone, put back the 0.27.0 assertion and drop the OPEN note: the Idealist clerk 09 finds the body after the chime and walks to look, his check ends at 5:34.1 and he walks to the phone with a dead body, and the run is sealed at 6:16 with the camera's confirmed report as origin.
- **New scenario:** a body left in a doorway is found by a civilian who steps out with their back to it; the same body 2 m behind a seated café customer, outside her cone, is not found; the takedown card's found count equals the outcome for the doorway case.

**What it moves** (probe: the working tree's engine 0.28.0 copied to `out/scratch/motive/b055near/eng/`, the near sense behind `MOTIVE_BODY_NEAR`; 285 scenarios plus a snapshot of the 38 route scripts taken at 03:27, through `bun run guard … batch --no-replay`, about 4 s a pass; the fight lab with `--rulebook`, which writes nothing; patch `briefs/054/motive/probe-r41.patch`, results `briefs/054/motive/probe-r41.txt`):

| set | at 1.5 m |
|---|---|
| N68 | **passes every 0.27.0 check again.** 12 clerks find the body at 5:30.1, clerk 09 checks it and walks to the phone at 5:34.1, sealed at 6:16, origin the camera's confirmed report. The only failing check is 0.28.0's inverted "does not find it", which Keel replaces. |
| other scenarios | run hashes move in N54, N87, N159, N161 and N163, with no failed check. Only N163's numbers move: T 17 → 18, Standing −1 → −2 (the 5:30 clerks now find the dead guard too). |
| the 38 route scripts | **no final hash moves.** That includes the body-found ones: `careless-body-found` (found by the camera), `careless-reporter-not-stopped` and `careless-takedown-seen-crowd`. |
| fight lab | the today preset is unchanged. In the lab presets, L4 ends 18 s sooner (7:03.2, not 7:21.1) in lab, snap-off, cover, charge-off and field-on, shots 50 → 51 in four. No verdict moves. |
| MC conditions | nothing moves: no route outcome, T, A or Standing changes. |

**Glyph:** when the armed order leaves or drops a body (a takedown, a drop), draw a civilian's 1.5 m ring rim at their feet with their wedge (§3.1 a). It's small, about one character high at play zoom. No other new mark.

**Done-check:** the rulebook rows and value above; `bun run test` green; 285 scenarios pass, N68 with its 0.27.0 chain under the cone and the new doorway scenario included; the 38 routes replay with no outcome moved from the 0.28.0 tree; the fight-lab verdicts unchanged.

### 9.2 The affair Watch: drop it
**Ruling: a Watch never teaches a secret.** Drop the affair from the offer and from what a Watch teaches. Listen alone reveals the affair, as it reveals the need and the temperament.

**Why:**
- **§04 and the canon say Listen.** The §04 preview line reads "Watch gives where and when; Listen gives who they are" (Sky, 2026-10-15). The v0.23.0 notes carry "Listen alone reveals temperament" next to "Inez and the guard are lovers". The Story spine has it the same way: "you learn it by listening in at the café." The Watch row came from my own brief (`briefs/011/motive-lovers.md` §1.2, marked proposed), not from Sky. §2.5 of this brief already says a secret must not show before Listen.
- **The offer is itself a tell, and it's the tie line Sky parked.** A Watch row that appears on two people only while they stand together says "these two share a secret" before anyone listens. That is the "who she meets" line, which Sky parked on 2026-10-23.
- **It shows nothing for the capstone.** The lovers are together only at his café stops (before 0:00, and at 8:30 when the Handover closes) and during the summons visit, after Inez is turned. By then her call has already pulled him off the door. Knowing the affair then is leverage on a guard who is no longer at the door. It fails Sky's "offered only where it shows something actionable".
- **The capstone is untouched.** The summons reads the tie, not what the cell knows. Listen on Inez still teaches the affair on both lovers, so route A-pressure (Watch, then Listen at 0:29, then Pressure) stands.

**Keel:**
- Rulebook: `cell.watch` drops its `also` row (`lover-within`) and the motive-lovers clause in its `src`. `mood.rule` drops "or the affair while the two lovers are together …".
- Engine: remove the affair branch in `WatchTeaches` (`GameMood.cs`) and the `lover-within` exception in the Watch's "learns" check (`GameOrders.cs`).
- Dead data: `lover-within` loses its last reader, so remove it from `Rules.cs` and `rulebook.schema.ts` (unknown = reject). `together-m` and `summon.together` were read only to keep the rounds' partner stops inside the Watch's 4 m, so they go too, with the `Rules.cs` check `rounds.close is inside summon.together`.
- **N40:** learn the affair by Listen during the visit (the Scout listens to Inez, or to the guard after the Watch his gate needs), then the rest as now: Pressure on the Loyal guard at its proper step, no wrong-approach line, no report.
- **N105b:** the text drops "only while it can show the affair"; the assertion stands (a second Watch is already-watched), and it adds the same answer with the lovers together.

**Pulse:** `tools/routines/rounds.test.ts` lines 33 and 157 read `together-m`. Hold the partner stops to your own value (`partners.closeCm`, the ring) and drop the "still teaches the affair" reason from `routines.rules.json` `closeCm`.

**What it moves** (the same probe, behind `MOTIVE_NO_AFFAIR_WATCH`): of 285 scenarios, **only N40** (its "a Watch teaches the affair" chain, as expected). All 38 route scripts are byte-identical (final hash). No string in `text.json` or the host promises that a Watch shows the affair.

**Done-check:** `cell.watch` has no `also`; a Watch on Inez with the guard beside her is offered only for her mood (or refused `already-watched`) and teaches no secret; N40 passes via Listen; the 285 scenarios and `bun run test` are green; the routes replay with no outcome moved.

## 10. HUD for 0.29 (Glyph, 2026-10-24; for Latch, brief 041 next-batch item 19)
Designed in the HUD style lab (`testbed/hud-style`, Revised A, kit colours only, Jost Regular, no sentences) over real play-zoom frames (the café terrace at yaw 30 and the Office front), at 1x and 2x. Boards: `briefs/054/hud/board-01-civilian-cone-when.png`, `board-02-alt-and-told-apart.png`, `board-03-mood-watch-price.png`, `board-04-security-fan-shadows.png`; every lab frame at 1x in `briefs/054/hud/qa/`. Machine copy of every number below: `briefs/054/hud/hud29.spec.json`. New art: `briefs/054/hud/icons/mood_{friendly,uneasy,scared}.svg` and `@2x.png` (64 px). Lab pieces: `testbed/hud-style/world29.js` (scenes `w29-*`), checks `testbed/hud-style/check29.ts` (46 checks, all pass). Nothing in Unity, the sim or tools was touched. Widths below are metres on the ground, layer = the signal layer (unlit, emissive, outside the post grade, depth-tested).

**10.1 A civilian's cone on hover** (board 01, panels 1 to 3; board 02)
- **When** (the host reads, §3.1; nothing new): hover a civilian, or Alt (the armed order's people, the 6 nearest). Draw only if `CaresAbout(id, armedAct)`, or they are on the armed card's eye (`Quote.WitnessIds`, `TakedownQuote.People`), or `NoticesNow(id, act, point)` for an operative's seen act now. One wedge at a time on hover. Frames shown: Pressure armed at Alert 0 (panel 1: hover ring, no cone), the same at Alert 2 (panel 2: cone), takedown armed (panel 3: cone and near-body ring), the off-duty guard with the same Pressure (panel 4: a ring, 10.3).
- **Look:** a rim-only wedge, no fill, no apex mark, no motion (it turns with the person). Cream rim 0.075 m over an ink-deep edge 0.17 m (alpha 0.6): the civilian hover ring's own colours (kit.uss `--cream`, `--ink-deep`), so a civilian is not a red signal. Radii and the 13 m arc are one stroke. Drawn under whatever stands in front of it.
- **Told apart from security** (board 02, panel 6): civilian = cream rim, no fill, still · security cone = red fill 38 %, red rim 0.085 m, sweeps · security ring = thin red ring · near-body ring = dashed cream · hover ring = small solid cream. Five shapes, two colours, no new one.
- **Shadows, drawn honestly:** every `Fan.Shadows` polygon inside the wedge is cut out of the rim; its edge gets a rim of its own (cream 0.05 m over ink-deep 0.12 m, alpha 0.55) and the dead ground is hatched (45 degrees, cream 0.045 m lines, 0.34 m pitch, alpha 0.7). Hatch, never tint: it is the only fill a civilian wedge ever has. Walls and decks the host already clips are cut with the same edge rim and no hatch. Board 01 panel 3 shows a 2 m topiary's dead ground.
- **Alt:** the same marks; the hovered one full, the others at alpha 0.9. Cap 6 as the host already decides (board 02, panel 5).
- **Size is the truth:** 13 m is 500 px across at play zoom. The rim is thin so it does not shout; the wedge covers a quarter of the width and nothing else changes.

**10.2 The near-body ring** (board 01 panel 3; board 02 panel 6)
- `PersonSight.NearCm` (150): a **dashed** ellipse at the civilian's feet, radius `NearCm / 100` m (57.9 px across, 35.7 px down), 36 dashes at 56 % on, cream 0.07 m over ink-deep 0.14 m (alpha 0.6). Dashed because a ring is a place and a wedge a direction; it also keeps it apart from the solid hover ring (0.68 m) and the solid security ring.
- **Only** with that civilian's wedge and only while the armed order leaves or drops a body: a takedown (knockout or kill) or a drop. Not a carry, not a stash. Security (`NearCm` 0) never get one.
- It does not touch the investigators' dotted line between leads (a line joining two people); if Sky finds them close, the ring can take shorter dashes.

**10.3 Security sight with real heights** (board 04, panel 8 against 7)
- **It does change today's look, a little.** The fan stays a red fill with a red rim; each `Fan.Shadows` polygon is cut out of fill and rim, with a thin red rim round the cut (0.045 m over ink-deep 0.08 m, alpha 0.45) so a cut reads as a cut, not as a drawing error. No hatch (the ground shows through).
- **What it looks like on the Handover:** the door guard on his landing (eye 2.35 m) has a strip of dead ground behind the 6.2 m cypress on his line (board 04). A pole camera's 4.79 m lens over a 2 m planting hides about 1 m behind it, a hairline notch. The patrol robot (eye 0.95 m) behind a 1.1 m planter is hidden only from 4.3 times the planter's distance on, so only when it stands within about 2 m of one. Most fans look as before; the rim round the cut is what makes the rest visible.
- **The ring** (`Kind ring`): security see all round within `Reach`. A thin closed ellipse (red 0.07 m over ink-deep 0.14 m, alpha 0.5), no fill, clipped like a fan, drawn on hover or Alt only when the armed act would count that person, never at rest (board 01 panel 4: the off-duty guard at table 1, clipped by the café wall and the corner building). The door guard and the machines keep today's cones.
- The lab computes the shadows from the engine's own rule (brief 055 §7.5: eye height, solid height, head 160 and chest 120) and checks it against the engine's examples (1.1 m behind a 2 m planting under the 4.79 m lens; nothing behind a 1.1 m planter for a person's eye; 4.33 times for the robot). Latch draws the engine's polygons, not these.

**10.4 Watch offered, mood, and the price line** (board 03)
- **Watch only when offered.** `WatchOffered(role)` false: no row anywhere (action panel, ring, verbs 1 to 4, Intel's next step). **The rows close up** in ContextRank order, the plate shows the next ranked verbs, and nothing is struck, greyed, empty or shown for `nothing-to-see` (no pop, no pill, no sound). If fewer than four verbs remain, the trailing knobs are the built `btn_verb_disabled`. Board 03 panels 12 and 13 show the plate with and without Watch (kit `plate_verbs` with its four knobs). After a Watch `already-watched` greys the row as built; it comes back if the mood changes.
- **Mood on the person:** one kit disc (`markers/disc_dark_24` on a 32 px element, the face 17 px, cream) centred over the head, its bottom edge 2 px below the head top. Three faces in one family, three silhouettes: **friendly** (round head, open smile), **uneasy** (round head, side glance, flat mouth, one drop), **scared** (head drawn in over hunched shoulders, wide eyes, round mouth, tremble marks). Neutral and unknown draw nothing. No words, no colour. Legible at 32 px and 16 px (board 03 panel 10); the face must stay at 0.53 of the element or it eats the disc's dark interior (a first try at 0.72 read as a pale sticker).
- **Two icons at most** over a person: the mood disc and, beside it with a 3 px gap, the one other icon the person carries (board 03 panel 9 shows Inez with the Scout's listening disc).
- **The price line:** after a Watch the favoured approach wears a small kit plate at the end of the line, before the faces: a dashed ring (cream, 15 px) around a check and the word **likely** (Jost 12.5, Fable's tag); the hurt approach wears the same plate with a dashed ring around a half disc and no word. After Listen both become the kit's own solid `mark_ok` and `mark_half` (15 px, no dash, no word): dashed = the Scout's read, solid = exact. The verb plate's knobs carry the same dashed marks, no word. Before a Watch, as today.
- **If Sky finds the word too much:** drop "likely" and the dashed check carries it alone; one line in `hud29.spec.json`.

**10.5 What Latch builds** (all in the signal layer or the HUD kit; nothing needs a new sprite but the three faces)
1. Import `mood_*@2x.png` into `Kit/icons/singles`; a `MoodDisc` over-head element (max two icons, 10.4).
2. A civilian wedge: rim only, one mesh of the sector minus `Fan.Shadows`, with the hatch inside the shadows only; reuse the security fan's shadow cut (stencil or triangulation) so one routine serves both.
3. A dashed `NearCm` ring (LineRenderer, 36 dashes) with the wedge, on a body-leaving order only.
4. The security ring (10.3), hover and Alt only.
5. The likely and worse plates on the price pill and on the verb knobs; the exact marks after Listen.
6. Verb plate: no Watch row when not offered; rows close up.
The words in `HudModel.cs:117` and `:426` are Fable's (§7).

**10.6 Checks** (`bun testbed/hud-style/check29.ts`, the lab on `bun run hud-style`): 46 pass. Every w29 scene at 1x: nothing off screen, clipped or overlapping; every disc holds one icon centred within 1 px; at most two icons over a person; HUD budget 6.2 to 7.1 % of the frame at rest and 10 % with five pills on screen (limit 15 %; `qa/budget.json`); only the sourced colours on the world marks; the near ring is 57.9 px across at play zoom; the shadow rule against the engine's examples; two negative controls (an icon moved 4 px, a third icon over a person) are caught; 2x pictures of the price, mood and takedown frames; the console is clean; `bun run typecheck` passes.

**10.7 What Sky may want to see before it ships:** (a) the civilian wedge in cream, not red (board 02 panel 6); (b) the hatch on dead ground (board 01 panel 3); (c) the word "likely" on the pill. The world marks sit over backdrops without depth, so a person stands under a wedge in the lab; the game draws the wedge under them.

## Parked (Sky, 2026-10-23: "for now")
- **Her round** (revision 2 §4). Sky: "don't think routine is really easy to show right now - getting into complex strategy concepts."
  - A clock pip over her (the guard's mark, `HudToolkit.Less.cs:975`), emptying toward her next move.
  - Her next walk dotted on the ground.
  - Alt adds the seconds and the loop.
  - The driver's pip counts to 5:50.
  - Data: `sim/data/handover.rounds.json`, `Game.PoseAhead`, `Game.RoundNext`. No rule change.
- **Who she meets** (revision 2 §4). Sky: "we would use character names to help user realise that's the person."
  - A Watch shows the subject's public ties whose routines meet (workmate, regular), at both ends. Lover and family ties never show.
  - For Inez: a dotted line to the guard, an edge arrow, his disc lit in Intel.
  - The Call row reads "brings ?" until Listen.
  - One derived rule, no state.
- **The probe** from revision 2 (whether a Pressure on Inez can land unseen) is answered in §3.7: below Alert 2 no civilian counts; the guards at table 1 do.

## Done-check
- Aster versions the GDD, queued in brief 035:
  - §03 Scout "Watch reveals a routine" and §04 Pay "routine known (Watch)" change to §2's rule, and §04's preview line changes.
  - §05 witnesses take §3.2's table and §3.3's cone: civilians cone only, the care line at Alert 2, security unchanged, shots and screams turn heads.
  - §08 nothing.
  - Sky's two confirmations (§8) first.

**Keel: one engine bump, one hand-off** (`contracts/rules/rulebook.json`, `people.rules.json`, the engine).
- **The Watch set and mood** (§2, §4):
  - `watch.offers` (§2.1), and the refusal `nothing-to-see`, free, in `Answer.Codes`.
  - The Listen and Pay gate (§2.2).
  - The known mood at the Watch's end, hashed when set (§2.3).
  - The mood read (§4.1), the fit step and **ready** (§4.2), and the preview's **likely** (§4.3).
- **Sight and care** (§3):
  - **Rulebook:**
    - `perception.people` `{sees: around | cone, reach: v:witness-range-m, half: v:people-half-cm}`, with `people-half-cm` 2252 [S].
    - `witness.notice`: §3.2's rows as data (`always`, `from: v:people-care-alert`, `odd`, `never`), every act id a witness path asks about in exactly one list. Unknown = reject.
    - `people-care-alert` 2 [S].
    - The `witness.rule`, `cell.rule` (running) and `privacy.rule` texts say it.
  - **Engine:**
    - One `Sees(person, point, act)` used by every civilian test. Today those are `WitnessesOf`, `WitnessesWithPos` (shots, draws, Pressure, runs), `ScanAct`, `ScanEscort`, the crowd's startle by a drawn weapon, the takedown forecast, the fire and draw quotes, `PeopleNear(at, t)` and the car blast.
    - Security keep their tests (§3.4).
    - A shot, a person shot and a car blast are seen all round within 13 m on a clear line (the bang turns heads).
    - A scream turns heads: hearers within 13 m on a clear line become witnesses.
    - Running at Alert 2 or more makes a civilian remember the runner (`saw-runner`, a sighting), with no found fault.
  - **Facing** (§3.3): stop facings read from the people file (Pulse) and the seats file (now an input, hashed), the rounds' face, the door, café, kerb and car rules, the startled face their startle. A stop with no facing is rejected at load. A civilian target's awareness uses the people cone.
  - **Cards** (§3.5): every card that counts people uses `Sees`, over the act's span, with routines read ahead. Below Alert 2 an odd act counts no civilian.
- **Host reads:**
  - `MoodOf` (the known mood)
  - `WatchOffered`, or `ScoutQuery` returning `nothing-to-see`
  - the person ids on the Pressure quote, not just a count
  - a person's cone shape and kind (wedge, ring, none) and `Notices(person, act)` now, for §3.1's hover (as `Fans` gives watchers)
- **Scenarios:**
  - the Watch set: Inez uneasy and watchable at 0:00; the driver and Vale not offered; Pay and Listen on the driver with no Watch; Listen on Inez refused "Watch first" before her Watch; a never-watched bystander scared by a seen takedown (Watch offered, Listen gated, Pressure −1 T); the Watch row back after a mood change; refused never lifted; the Help floor at worse
  - a takedown behind a lone clerk is unseen; the same takedown in front of her is seen
  - a seated café table sees a takedown from every side
  - a Pressure in front of a civilian at Alert 0 is unseen and at Alert 2 seen; an off-duty guard behind it sees it at Alert 0
  - a scream turns heads
  - a shot is seen behind within 13 m
  - a civilian sees a run at Alert 2 and remembers it, and raises nothing at Alert 1
  - a stop with no facing is rejected at load
  - the card's count equals the outcome for a takedown while walkers pass
  - `around` replays every pinned run byte-identical
- **`bun run missions:check` equals §3.7, or Motive signs off what differs:**
  - Moves: loud ×4 13 T and Standing −1; loud-fight 12 T; fail-district-seals 8 T and Standing 0; careless-takedown-seen-crowd fails at 6:32.9 with 7 T and Standing 1.
  - Everything else holds its outcome, end, T, A and Standing.
  - The inputs hash and every run hash move.
  - MC-0 to MC-12 hold.
- **The rest:** fight-lab verdicts unchanged. `bun run test` green.
- **Reference:** `briefs/054/motive/probe-r4.patch` is the probe that measured §3.7: a scratch reading of the rule, not the build.

**Pulse** (`tools/routines`, the people file):
- a facing for every on-street stop by §3.3's table (door, stand, bay, kerb, transit, seat), so none is left unknown
- the seats' `face` as engine data
- §3.3's table into `pulse-witnesses.md` §2.2 (done in this revision as a note; Pulse owns the wording)

**Waymark:**
- Drop the driver's Watch from the eight scripts (§6).
- **careless-takedown-seen-crowd** is now seen by guard 73, not the crowd (§3.7). T9 wants "a witness who reports" the player can read. Re-site it so a reporting civilian faces it (a Loyal or Idealist café customer, or a clerk facing the door), or keep it as the guard-seen twin and add a crowd-seen one. MC-7 holds either way.
- careless-reporter-not-stopped needs nothing (its reporter is the door guard).
- No quiet route needs re-timing: none moves (§3.7).
- Re-record once Keel's bump lands. `rejected` stays 0.

**Glyph:** §7: the cones (who shows one and its shape), the mood mark, likely and ½, on the §7 test frame.

**Fable:** §7: the Watch strings, the lines that promise a civilian notices, the §5 names (with the pronoun check), and the overheard scenes retired or generated.

**Latch:**
- an absent Watch row (panel, ring, verbs, ContextRank)
- Intel's next step skips to Listen when no Watch is offered (`HudToolkit.Intel.cs:153`, `HudModel.cs:117`)
- the cone on hover by §3.1's rule, from Keel's read (never its own geometry)
- the mood mark, and likely on the card
- the two strings in code

**Suture** (uncoached):
- After a Watch on Inez and before Listen, ask "How would you turn her, and why?" Pass: ≥ 5/6 say Pressure because she's nervous.
- With a takedown armed by the café, hover a customer and ask "Will they see it?" Pass: ≥ 5/6 answer from the cone.
- At Alert 0 with a Pressure armed, ask "Who will mind?" Pass: ≥ 5/6 name the guards, not the customers.
- These are candidates for §13 (T4, T9); Aster decides.

## Evidence
- **Read:**
  - The rulebook: `cell.watch`, `scout.listen`, `turn.*`, `perception`, `sight`, `attention`, `witness`, `privacy`, `preview`, `traceable`, `alert`, `crowd`, `cell.rule` (running), the events `witness.sees`, `takedown.act`, `takedown.witnessed`, `body.found`, `report.lands`.
  - people.rules: `approaches`, `temperaments` (Fearful start fear 1).
  - The cast in `sim/data/handover.people.json`: 130 people, 19 of them Fearful; Inez p-cafe-14 Fearful; the guard p-records-office-01 "Folake Okafor", Mercenary; the driver p-records-office-31 "Mei Zhou", Mercenary. Its on-street stops are 153 seats, 72 café stands, 87 at `t1-records`, 29 doors, 7 `s1-gardens` and 5 `office-bay`.
  - The engine:
    - `GameCrowd.cs` `WitnessesOf` (all-round, 13 m, line)
    - `GameInvestigation.cs` `WitnessesWithPos`, `PeopleNear`
    - `GameTakedown.cs` `ScanAct`, `Decide`, `Shout`, `ScanEscort`, `Awareness`, `FacingOf` (none at a stop), `QuoteTakedown`
    - `GameOrders.cs` `WitnessAct`
    - `GameCombat.cs` (shots, draws, the fire and draw quotes)
    - `GameScatter.cs` (startle)
    - `GameCars.cs` (blast)
    - `Geo.cs` `InCone` (a triangle, so the 13 m distance is added)
  - Seats: `contracts/people/handover.seats.json`, chair `face`, view only. Rounds: `face` per post and table.
  - Briefs: `briefs/013/pulse-witnesses.md`, `briefs/013/motive-takedowns.md`.
  - Text and Unity: `contracts/missions/handover/text.json`; `HudModel.cs:115`, `:117`, `:426`; `HudToolkit.Intel.cs:153`.
- **Ran** (all through `bun run guard`; each batch 1 to 3 s, the fight lab 13 s):
  - The working tree's sim (0.27.0) was copied to `out/scratch/motive/b054/base` and `cone`. The cone copy was patched (`briefs/054/motive/probe-r4.patch`). The 38 route scripts were played with `batch --list --no-replay` under:
    - today
    - the rule with unknown stop facings hashed, facing the act and blind
    - without the Alert gate
    - security by the guard cone
    - without the shot turning heads
  - The fight lab under today and the rule.
  - The switch off is byte-identical to today (every final hash). All results are in `briefs/054/motive/probe-r4.txt`.
- **Not run:** `bun run missions:check` and its variations (they need Keel's engine). The MC conditions in §3.7 are read by hand from the plays.

## Next owner
- **Keel:** the one bump (Done-check), after Sky's two confirmations in §8. They change one list entry each, not the build.
- **In parallel:**
  - Pulse (stop facings)
  - Glyph and Fable (§5, §7)
  - Waymark (the driver's Watch; the crowd-seen twin)
- **Then:** Latch builds, and Suture asks the questions.

## Sky's answers to §7 (2026-10-23)
1. **Her own nervousness counts: (B).** Sky: "yeah, I mean she's nervous coz having an affair and in debt etc... people have their feelings outside of the cell." Mood reads from any cause, the person's own life included, not only what the cell did.
2. **The set, trimmed:**
   - **Sight:** "just a cone on hover over, only though if it matters." A cone on hover, and only where that person's sight matters.
   - **No empty Watches:** "If a person being watched has no impact then we just drop that watched action." Watch isn't offered on someone when watching them would change nothing.
   - **Her round is dropped for now:** "don't think routine is really easy to show right now - getting into complex strategy concepts."
   - **Who she meets is dropped for now:** "for now - don't think so... we would use character names to help user realise that's the person." There's no tie line; names do that job.

## Sky's answers to revision 3 (2026-10-24)
1. **Ordinary people and sight:** "Ordinary people shouldn't really care unless there's high alert status - think real world. And yeah, would be in cone area only for seeing." Civilians see by a facing cone only, and they don't care about what the cell does unless Alert is high. What "care" covers (which acts still draw a reaction at low Alert, e.g. gunfire, a kill in plain view, a scream) is for Motive and Pulse to define against brief 013, then price.
2. **The free tell** (the Watch row marking the Fearful people) is accepted: "that's fine".
3. The defaults Sky didn't stop go ahead: Listen and Pay need a Watch only where one is offered, and the guard and the driver are named in play.

## Sky's calls (2026-10-25)
Sky, 2026-10-25, on the outstanding-work summary that listed each pick: "Your recommendation seem good".
1. Pressure is unseen by ordinary people below Alert 2: **yes**.
2. Civilians remember a runner from Alert 2: **yes**.
Next: Keel (rulebook and engine, one bump) as in the Done-check, then Pulse, Glyph, Fable, Waymark, Latch, Suture.
