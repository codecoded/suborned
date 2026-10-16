# 013 · Witnesses, bodies and reports: what people do (Pulse, 2026-10-16)

For Motive (numbers), Keel (engine), Vantage and Splice (report points, hide levels), Waymark (the Handover), Glyph and Latch (what the player sees), Fable (log lines), through Aster. Part of brief 013 (`briefs/013-takedowns.md`). Sky decided: anyone can be targeted; kill or knockout is the player's choice; bodies persist and are seen; the Enforcer carries them to quiet places. This file owns what a *person* does; Motive owns the prices, Trail, Alert and Standing for each case, and the wake and talk numbers (`briefs/013/motive-takedowns.md`, in parallel; it was not on disk when this was written, so every Alert mapping below is **proposed** and Motive's to change). Nothing in `sim/`, `contracts/rules/` or `contracts/people/` is touched. Every rule is marked **(proposed)** or **(existing)**; every number is [S].

**Third pass (2026-10-16, written to Motive's rev 2, `motive-takedowns.md`, which wins on prices, Alert, Trail, Standing, the dial and missing persons):** §0a lists every change and every place I still differ; §11 to §14 are the missing expected person, where an alarm sends the response, the response behaviour on Motive's dial (one dial), and missing persons (one mechanism). Anything that hinges on Sky's open calls 1 to 9 is marked **[Call n]**. Text marked *(changed)* was rewritten in a later pass.

| file | what |
|---|---|
| this spec | reactions, reporting, discovery, the woken, determinism and cost, the Handover on r016 |
| `briefs/013/pulse-witnesses.values.json` | my proposed `values` in the shape of `rulebook.json`, written to Motive's ids where we overlap (Motive's id wins) |
| `briefs/013/pulse-response-profiles.json` | the response *behaviour* on Motive's one dial (§13): where units start, launch gap, destination order, assignment, arrival, optional levers; the dial itself is Motive's |
| `briefs/013/pulse/probe/` (`Program.cs`, `probe.csproj`) | the measuring probe: runs the engine as it is on the r016 cast with no orders, sights each place, and holds `React`, the reference for the C# reaction function. `dotnet build briefs/013/pulse/probe -c Release --artifacts-path out/pulse013/art` |
| `briefs/013/pulse/probe-output.txt` | its output: every table quoted below |

## 0. What matters most (read this first)

1. **A person's reaction is a short decision list over fields they already carry** (temperament, the four scales, job, ties, Turned). No new person field is needed (§8). Two of the four temperaments do not report at Alert 0: a Mercenary keeps quiet until the city is awake (Alert 2, the posted reward), a Fearful person avoids the body, tells a tie, and **talks when questioned** (the Talker test: fear 1 > 0). Loyal and Idealist report now. So the temperament decides *when it bites*: the window now (Loyal, Idealist), the investigation later (Fearful), nothing unless paid or hurt (Mercenary). On the r016 clerks that is 31 of 58 who report at once; with two witnesses someone reports four times in five; with five, 98%.
2. **The report is a person walking to a phone.** A civilian walks to the nearest *report point* (a staffed door, a call box, a guard post), calls (6 s), the report **lands at the desk**, and the response unit is on the street `dispatchS` later (the dial: 12 s at S1, 10 s at the Handover's S2). Sight to landing is about 15 to 30 s for a civilian, 3 to 5 s for a guard or a robot, up to 23 s for a camera. That is **the player's window** and it is on screen: the runner, the line to the box, the "lands in n s" chip.
3. **Alert rises when the report lands, for every silent act [Call 1].** Gunfire keeps its rule at sight (security hears it). Witness Trail (+1) and Standing move at sight. So a takedown seen by a civilian who never reports raises no Alert; one that is reported raises it when the report lands (3 for a seen takedown, 2 for a found kill, 1 for a found knockout, §3.3). The temperament table, the window and every counter below depend on this, and it changes the GDD's "violence seen by a civilian → 3 at once" for silent acts. *(This is where my first draft was; the second pass withdrew it; Motive's rev 2 restores it.)*
4. **The window outlasts the report.** After it lands the unit needs `dispatchS` (10 s at S2) to roll and about 45 to 70 s to walk to a place on the Handover block. A body moved before the unit *verifies* is an unfounded report: Alert stays at what the landing set, and no seal starts (the seal starts only on a confirmation at Alert 2 or more, Motive §2.3). That makes carrying a body away a real play even when a Mercenary or a slow reporter found it. A **knockout never costs more than Alert 1** [Call 2].
5. **Counters in the window are physical, not social.** The approach clocks (Ask 25 s, Pressure 35 s, Help or Pay 50 s) are longer than a report in transit, so a witness already running cannot be turned. What works: be unseen (the "seen by" forecast), take the witness down before the call ends (the Enforcer within about 50 m of the box), have turned the people around you first (loyalty 1 keeps quiet), or move the body before the unit arrives.
6. **The Handover as generated is not quiet.** The service lane is walked by the second-shift guard p-records-office-74 from 4:04 and by 38 clerks after 5:30. A staff guard (p-records-office-29) stands the official entrance 4.1 m from the driver's stand until 5:30, and p-records-office-73 and -74 stand the two doors from 5:30. Guards radio at once (5 s). Waymark needs this (§7); it was invisible until witnesses had consequences. Only R1 (behind the café) and D2 (café kitchen door) are never seen on this cast.

## 0a. Reconciled to Motive's rev 2 (third pass): what changed, what hinges on Sky, where I still differ

Motive's `motive-takedowns.md` rev 2 is the reference. Sky's calls 1 to 9 (its §8) are not confirmed yet; rows that hinge on one say **[Call n]**. Sky's ruling on kills: **they are shown, not cut away; the body lies where it fell until moved.**

| # | topic | now (Motive rev 2) | hinges on |
|---|---|---|---|
| 1 | when Alert rises | **at report landing for every silent act** (takedown, carry, body, missing person); **gunfire at sight**; witness Trail +1 and Standing move at sight (Motive §0.1, §1.4) | Call 1 |
| 2 | a knockout | **never above Alert 1**: found knockout, confirmed living victim, woken victim's report all stay at 1; a found kill is 2; a takedown seen is 3 on landing; a carry seen 2 (Motive §2.3). My earlier "confirmed raises to 2" and "woken victim 2" are withdrawn | Call 2 |
| 3 | the seal | starts **only on a confirmation at Alert 2 or more**; a confirmed knockout at 1 starts none; gunfire from arrival. The 60 s search at Alert 2 or more opens stashes within 30 m, 10 s each (Motive §2.3). My "open delta" is closed | follows Call 1 |
| 4 | one incident per takedown | every discovery (seen, found, victim's report, carry, missed mark, suspicion) is one incident; Alert once to the worst; Trail once; a second takedown is a second incident | |
| 5 | turned witnesses | a witness with loyalty at least 1 adds **no witness Trail** and makes no report; Standing still moves (Motive §1.4) | Call 5 |
| 6 | the response | **one dial**: Motive's `response.levels` S0 to S4 (§5.3), S = district base + open cases (÷2, cap +2) + mission bonus; the Handover is S2. My four profiles, my watch level and my per-mission modifiers are **dropped**; §13 and `pulse-response-profiles.json` are now only the behaviour on that dial (where units start, launch gap, routes, readability) | Call 3 |
| 7 | missing persons | **one mechanism**: a case (+2 Trail, no Menace, ties worry fear +1), the block gets +1 guard and +1 camera for 3 missions (`area-watch-step`, `area-watch-missions`), and every 2 open cases is +1 district level (Motive §2.6). My watch level 0 to 3 is dropped (§14) | |
| 8 | suspicion | **`suspicion-s` 20 s** at the place without the person, at Alert 1; a held person reports nothing (a Vale being Taken) (Motive §5.2). My temperament graces are not used by default (see below) | Sky 6 |
| 9 | hearing | `scream-m` **20 m** is back (Motive's aware victim screams, the call-out is heard within 20 m); a hearer's `look` still needs a clear line within 13 m | |
| 10 | the call-out | **on**: a stashed victim calls out every 10 s; a hearer may report "a noise" (Alert 1) and the verify opens the stash; hidden *and* restrained is silent (Motive §3.3). My "optional, off" is withdrawn | |
| 11 | hide levels | two, `open` and `shut`; `screened` (4 m) and a body-height sightline are [L] (Motive §2.1) | Call 6 |
| 12 | check-ins | every staff post, every 90 s from the shift start, the shift end a mark (Motive §5.1) | |
| 13 | the bin round | 900 s, phase from the bin's id, learnable, **in the mission only** (Motive §2.4) | Call 9 |
| 14 | Mercenary | keeps quiet until Alert 2 (Motive agrees; 50 of the 130) | Call 4 |
| 15 | call boxes, no cut | placed by R-REPORT (40 m); no Tech action to cut a line | Calls 7, 8 |
| 16 | the Enforcer's face | `enforcer-face` is a fact: witnesses of a takedown hold it, an aware victim holds it (Motive §1.4) | Sky 3 |
| 17 | robots | a robot is a **patrol unit** (health 4, pistol, trained, counts against the 30 cap); my unarmed "warden" is an optional class, off (§13) | Call 3 |

**Where I still differ from Motive** (each is a switch or a note, not a blocker):
1. **Suspicion by temperament.** Motive's `suspicion-s` is one number, 20 s. I would keep it as the base and scale it by temperament in per-mille (Fearful 500, Loyal 1000, Idealist 1500, Mercenary 2250, so 10, 20, 30, 45 s), because a Mercenary Vale who waits 45 s is a different person from a Fearful one, which is pillar one. Default in this file is **flat 20 s** (all multipliers 1000); the multipliers are values `suspicion-*-pm`. Motive and Sky decide.
2. **Where the alarm sends units.** Motive §5.2: to the scene and to where the *missing person's* ties and duties are meant to be. Sky's words: where the *alarm-raiser's* connections are. I implement both as ordered tiers (§12); the dial's robot count bounds how many go. **[Sky 6]**
3. **Robot speed.** A patrol robot walks 85 cm/s. A swarm at that speed takes 47 s to cross 40 m. I add a value `robot-alarm-cm-s` (default 85, so nothing changes) as a second dial lever.
4. **Hero slowdown.** Citizens block cars: I keep an optional arrival multiplier per Hero step (default off), because Motive's S moves only with cases and mission data.
5. **Idealist and the Fearful finder** keep their rows (§2.3); Motive does not contradict them.

Sky's answer 6 is §11 and §12; Sky's note on the swarm is §13 (one dial, as values); missing persons are §14.

## 1. The model

A fact travels only through people, paper and a few watchers (GDD §01: no networks). Four kinds of fact, each `{kind, place, tick, state, by}`:

| kind | made when | who can hold it |
|---|---|---|
| `act` | a person with a clear line within 13 m sees a takedown (existing `WitnessesOf`: range plus clear line, 360°) | witnesses; memory entry `saw-takedown` |
| `body` | a person, robot or camera sees a body lying in view (§2.2) | finders; `found-body` |
| `carry` | someone sees an operative carry or drag a body | witnesses; `saw-carry` |
| `alarm` | someone within `scream-m` (20 m) hears a scream or call-out (no line needed) | hearers; no fact beyond place-time, until they look |

`state` is `out` or `dead`, known to a finder only after the `check` (§2.1) or at once to staff; `by` is a face. *(changed)* The Fixer's face and the **Enforcer's** (`enforcer-face`, Sky, Motive §1.4) are facts: witnesses of a takedown and an aware victim hold the Enforcer's.

One chain for every case, in ticks, deterministic:

```
notice (checked every 5th tick) -> shock -> decide (list, §2.3) -> [check] -> walk -> call -> LAND at the desk
   -> Alert rises (silent acts) -> dispatch (the dial) -> unit on the street -> walks to the place -> VERIFY (§3.5) -> confirmed | unfounded
```

Existing rules this keeps unchanged: *(changed)* **one takedown is one incident, however many discoveries it has** (seen, found, the victim's report, a carry seen, a missed check-in, a missing-person report, a later search): Alert rises once, to the worst level reached, and Trail is charged once (Motive §2.3); a second takedown is a second incident; an incident adds witness Trail once and moves Standing once however many see it; Standing moves for *witnessed acts only* (a finder of a body did not witness the act, so a found body moves no Standing, except the one exception Sky confirmed: a found *killing* is −1 toward Menace on confirmation, Motive §2.3); news along ties is facts only and never moves Standing (`standing.rule`, `word.passes`); Alert never falls within a mission; any gunfire keeps its own rule at sight (A at least 2, response called, seal from arrival); a silent act raises Alert only when a report lands [Call 1].

## 2. Reactions

### 2.1 The nine verbs (proposed)

| verb | does | numbers |
|---|---|---|
| `freeze` | stands still while the shock runs | act: `crowd-startle-ticks` 5 (a Fearful person 2), existing; body: `react-s` 2.0 s, existing |
| `flee` | the existing crowd machine: run to a refuge, shelter, return | unchanged (`crowd.*`) |
| `shout` | a scream: everyone within `scream-m` (20 m, no line) **hears**; hearers who are not already reacting turn and `look` | once per person per incident |
| `look` | a hearer walks toward the sound until they have a clear line within 13 m (at most `scream-look-s` 6 s, at walking pace), then reacts to what they now see | Mercenary and Fearful hearers do not look (Fearful ones walk away) |
| `check` | walk to within `near-m` (3 m) of the body, stand `body-check-s` (2.0 s), learn its state, then decide again | only Idealists *(changed: kin no longer check; they report at once, Motive §2.2)* |
| `report` | walk to the nearest report point (§3.1), call, the report lands | walk 1.4 m/s if they found a body, run 2.6 m/s (`crowd-run-cm-s`) if they saw an act or a carry |
| `radio` | staff: report from where they stand | `post-call-s` 3.0 s |
| `tell` | at the phone, a second call to one tie (§3.4) | `call-s` 6.0 s |
| `seek` | a told relative walks to the place and becomes a witness of what is there | walking pace |
| `free` | a finder of a *restrained* victim unties them (3 s); the victim then reports at once (Motive §3.3) | `free-s` 3 |
| `quiet` / `avoid` | walks on / detours at least 8 m round the body; nothing is reported | |

"Scream makes more witnesses" is `shout`: it brings hearers within 20 m into sight of the scene, and each of them reacts by this same list. A scream does not reach security by itself, except that the cast's door guard checks a scream within `guard-leash-cm` (15 m) of his post (his existing "searching" hold). Robots ignore screams (they break to gunfire only).

### 2.2 When a person notices (proposed)

- **Act or carry:** at the tick it happens, everyone within 13 m with a clear line (the existing witness set). Taken before any startle, as gunfire witnesses are.
- **Body:** a body that is *open* (lying where it fell, or carried) is noticed by anyone within 13 m on a clear line. In a *shut* place (a container or a service door, Motive §2.1: the only things that hide a body) not at all, until it is serviced (§4.1), searched, or the victim calls out. My *screened* level (4 m) is [L] for v1 **[Call 6]**; the 4 m rows in §7 stay as a measurement. Checked every 5th tick (phase from the body's id), so noticing is at most 0.5 s late. A person who is `sheltered`, inside, or off the street sees nothing.
- **Watchers:** the patrol robot and the guard see a body in their cone; a live camera records it. A looped camera sees nothing (existing `LoopOn`).
- **Crowds screen bodies:** a person standing round a body blocks a watcher's line (existing `CrowdBlocks`); this is emergent, no rule.

### 2.3 The decision list (proposed; first matching row wins; evaluated once, when the shock ends)

Inputs, all existing fields: `staff` = job guard, patrol-officer, watcher or investigator (or the cast guard); `kin` = a lover, family or workmate tie with the victim; `cell` = `Turned` or loyalty at least `word-quiet-loyalty` (1); b, r, f = bravery, resentment, fear; A = Alert now; temperament.

| # | row | does | why (the rule it reads) |
|---|---|---|---|
| 1 | staff | `radio` at once; a guard within 15 m of his post then holds the scene | §05 security; the cast guard's hold |
| 2 | kin of the victim *(changed)* | `shout`, then `report` (run) **without hesitation**, whatever their temperament; terrified (f = 3): `shout` and `flee` only | Motive §2.2: family, lover or workmate reports at once; makes the lover tie a tripwire (§7.3) |
| 3 | cell | `quiet`; says nothing to anyone (the `word.passes` quiet rule); **adds no witness Trail** for the new acts [Call 5]; Standing still moves | people.rules: a person who owes the cell keeps what they saw (rulebook `word-quiet-loyalty`) |
| 4 | r = 2 (Informer) | `report`, as a Loyal person | `contact.informs`: Informer sells the cell |
| 5 | f = 3 (terrified) | `flee`; `shout` if it was an act; never reports | scales.fear steps |
| 6 | Loyal | `report` | people.rules `reportsApproach`: Loyal reports; a Loyal clerk of the Office reports at the Office door, which is a report point |
| 6 | Idealist | act: `shout`, `check`, `report`; body: `check`, `report` | "volunteers"; bravery fast |
| 6 | Mercenary | A below 2: `quiet`; A at least 2: `report` | "Mercenary responds to Pay": no stake until the city pays for words; Alert 2 is the posted reward |
| 6 | Fearful | act or carry: `flee` (startle 2 ticks); `shout` if f at least 2; body: `avoid`; both: `tell` | "Fearful responds to Pressure"; fear 2 and 3 come from pressure and threats |

After any `quiet`, `avoid` or `flee` the person also `tell`s if they reach a phone or a tie is within 20 m (§3.4). Row 3 beats row 6 and row 2 beats row 3 (Inez, turned, still cries out for a lover she finds down). The person's own safety is not modelled: a witness 2 m from an armed man behaves by the list, and the player reads that list in the forecast (§3.6).

Bravery and resentment *move* from this event as existing rules say (`violence-bravery` −1, `violence-fear` +1 for a turned contact who sees violence); this spec adds no scale move.

### 2.4 Who reports, on the r016 cast

| group (r016, 130 simulated) | reports at Alert 0 | at Alert 2 | talks if questioned (Talker test, fear + resentment > bravery + loyalty) |
|---|---|---|---|
| Loyal (33) | yes, walks to a post | yes | no (0 > 0 is false) |
| Idealist (28) | yes, after the check | yes | no |
| Mercenary (50) | no | yes | no |
| Fearful (19) | no (`avoid`, `tell`) | no | **yes** (1 > 0) |
| staff (16 guards of the 130) | `radio`, 5 s | same | by their temperament |

Of the 58 clerks: 21 Loyal, 10 Idealist, 20 Mercenary, 7 Fearful, so 31 report at Alert 0 and 51 at Alert 2. A reporter is a *volunteer*: when investigators question a person who reported they give place and time without the Talker test (rulebook `investigator.question`: proposed `reporter` flag, §8).

## 3. Reporting

### 3.1 Report points (proposed)

A report point is anywhere a voice reaches the dispatch desk (GDD §01: calls are voice, by telephone or radio):

1. **A staffed door.** Every entrance of a unit with someone on shift has a telephone behind the counter. The site's `refuge` doors are exactly these (`door:cafe-entrance`, `door:office-entrance`, `door:official-entrance` on the Handover). Going to a door takes the person off the street (exteriors only; it is a refuge they already use).
2. **A call box** (kit piece `PhoneBooth`, adopted, none placed; Vantage said `placed: 0`). One rule places them: **R-REPORT, every walkable street cell is within 40 m of a report point on the grid walk.** On the Handover grid, doors alone cover 84% of 4 m samples (median 30 m, worst 56 m); a greedy deterministic fill adds **7 boxes** and reaches 100% (median 18 m, worst 40 m; positions in `probe-output.txt` are an example, not a layout: Vantage and Splice place the pieces, the checker proves the rule). More boxes make fewer quiet places; the 40 m is a tuning value.
3. **A guard post** (the door guard's post telephone; same cost as a door, `post-call-s` for him).
4. **The patrol robot's hailing plate.** A person within 2 m of the patrol presses it; the robot calls (`robot-report-s`). Taken only if it is nearer than the next point.

Choice: the nearest by grid walk, ties on id. The distance field to the nearest report point is computed **once at load** (a multi-source Dijkstra over the walk grid), so a reporter's route costs nothing per report (§6).

### 3.2 The timeline and the window (proposed)

Seconds from the first sight, for a civilian finder who reports: `react-s` 2.0 + (Idealist only: walk to 3 m + `body-check-s` 2.0) + walk (route length ÷ pace) + `call-s` 6.0 = **landing**; the unit is on the street the dial's `dispatchS` later (`response.levels[S].dispatchS`: 12 s at S1, 10 s at the Handover's S2; Motive §5.3). The watchers are faster and fixed:

| reporter | chain | landing, s from sight |
|---|---|---|
| staff on foot | react 2.0 + post call 3.0 | 5 |
| the patrol robot | its cone has the body: `robot-report-s` 4.0 | 4 |
| a live camera | next scan of its screen (every `camera-scan-s` 20 s, phase from its id) + `post-call-s` 3.0 | 3 to 23, mean 13 |
| Loyal civilian, 20 m from the box | 2.0 + 14.3 + 6.0 | 22 |
| Idealist, 10 m from the body, 20 m from the box | 2.0 + 5.0 + 2.0 + 14.3 + 6.0 | 29 |
| an act seen, run to a box 20 m away | 0.5 + 7.7 + 6.0 | 14 |
| a missed check-in *(changed)* | every staff post checks in by voice every 90 s from the shift start, the shift end a mark (Motive §5.1); a missed mark lands at the desk **at the mark** | the mark itself |
| suspicion of a missing tie | `suspicion-s` 20 s at the place without them, then walk and call (§11) | 20 + 9 for Vale at the bay |

Only the **earliest landing** matters to Alert (as `CallSecurity` already takes the earliest arrival), but every pending reporter stays a person in transit until it lands, so the player can still stop the first and face the second. At most `report-cap` 4 reporters per body are kept (the four earliest), which bounds the state.

**What stops a report before it lands:** the reporter is knocked out, killed or carried (down or held); an approach that turns them lands first; or the line cannot be placed (dead box, not modelled). Their pending report is dropped; the next earliest lands. A report that has landed cannot be recalled.

**The Enforcer's reach.** The Enforcer runs at 2.8 m/s (existing `op-run-speed-cm-s`). A reporter stands at the box for `call-s` (6 s) before the report lands, so the Enforcer stops them if the takedown is done before the call ends (Motive: kill 3 s, knockout 6 s on a surprised target): he must be at the box by `t_arrive + 6 − takedown`. For a Loyal finder 20 m from the box (`t_arrive` = 2.0 + 14.3 = 16.3 s) a kill needs a run of about **54 m** (19.3 s at 2.8 m/s) and a knockout about **46 m**; for an Idealist who checks first (`t_arrive` = 23.3 s) about **74 m** and **65 m**; less the seconds the player takes to select and order. Keel's forecast can print "reach in n s" from the same numbers (§3.6). Taking a witness down at the box is a new takedown, seen by whoever stands at the box.

### 3.3 What a landed report does *(Motive §2.3, adopted)*

A report carries `{kind, place, tick, severity}` and the reporter's name (the desk logs it). **Alert rises when it lands** (silent acts) [Call 1]. One takedown is one incident: Alert is the highest level any of its landed or confirmed reports reaches, and a second, separate takedown is a second incident (the rulebook's `byIncident` makes it at least 2). Witness Trail (+1) and Standing move at sight, once per incident; a witness with loyalty at least 1 adds no witness Trail [Call 5]. The takedown's own Trail (+4 knockout, +6 kill) is charged on a confirmation, a statement, or as a missing person (Motive §2.3).

| report says | Alert on landing | on verify | Trail | Standing |
|---|---|---|---|---|
| a person lying, state unknown (a civilian who did not check) | **1** | unconscious: **stays 1**; dead: **2**, and the seal starts | the act's Trail on confirmation: knockout +4, kill +6 | kill: −1 on confirmation |
| a dead body (staff, or an Idealist after the check) | **2** | 2; the seal starts | +6 on confirmation | −1 |
| a woken victim's own report | **1** | confirmed on landing | +4 | none |
| a takedown **seen** | **3** | 3 | witness +1 at sight; the act's Trail on landing | −1 at sight |
| a carry seen | **2** | 2 (the body's state on confirmation) | witness +1 at sight; the act's Trail on confirmation | −1 at sight |
| a call-out from a container (§5) | **1** | the verify opens the stash: as the first row | as the first row | as the first row |
| a missed check-in, or suspicion of a missing person (§11) | **1** | an empty place: unfounded; the unit searches 60 s | none unless something is found | none |

**A knockout never costs more than Alert 1 by itself [Call 2].** The response is called by the dial (§13): at Alert 1 robots (and the patrol) check the place and no unit is sent; at Alert 2 or more a unit goes, and the **seal starts only at a confirmation at Alert 2 or more** (a confirmed knockout at 1 starts none; gunfire keeps its rule from arrival). A seen takedown that nobody reports raises no Alert: the player's work is to be unseen, to take the reporter down, or to have turned the people around.

### 3.4 Telling a tie (proposed; in-mission form of `word.passes`)

A person who `quiet`s, `avoid`s, `flee`s or has just called may `tell` **one** tie, facts only:

- who: the first of lover, family, workmate, regular (ties.kinds; debtor and rival never) that is *reachable*: indoors at a unit (they have a phone: the teller is at one, or the tie's current stop is inside) or on the street within 20 m;
- how: voice, `call-s` 6.0 s at the phone, or a shout across 20 m;
- what the hearer gets: a `told` memory entry (place, time, the fact). One hop: a told entry is never retold (existing memory rule). No Standing, no scale moves.
- what the hearer does (hearsay row): Loyal: `report` with the hearsay flag (lands as a lying-person report, severity 1); Idealist: `seek` (walks to look, becomes a first-hand witness if the scene is still there); a lover or family hearer with b at least 1: `seek`; Mercenary, Fearful: `quiet`. A `cell` hearer (loyalty at least 1) passes nothing on.

This is why a lover tie is a tripwire: the guard's lover Inez hears at once if the guard reaches a phone, and a told Inez who is not turned walks to the door.

### 3.5 Verify: what the unit finds (proposed; Motive §2.3 adopted)

When the patrol, the door guard, a robot or the response unit comes within 13 m of the report's place with a clear line, it checks:

- **body present and visible** (an open body, or a carry in its cone): *confirmed*. Alert goes to the confirmed level of §3.3 (a living knockout stays 1; a dead body 2), the act's Trail is charged, and the scene is held: the patrol or guard stands within 5 m, so moving the body now is a carry in a watcher's cone. **The seal starts only if the confirmation is at Alert 2 or more.**
- **not there** (moved, stashed, carried out of the cone): *unfounded*. Alert stays at what the landing set and no seal starts. The unit searches for `search-s` (60 s); **at Alert 2 or more it opens every stash within 30 m of the reported place, nearest first, 10 s each**; at Alert 1 it looks and opens nothing. The witnesses remain leads.

That is what turns the 45 to 70 s walk (after the dial's dispatch) into the player's second window. Gunfire needs no verifying and keeps its rule.

### 3.6 What the player sees (for Glyph, Latch, Fable)

- **Forecast on the takedown quote**, from the same function: for each person with a clear line to the act and the doer's route: "seen by 3 · 2 would report (≈ 22 s)", and the person's verb once their temperament is known ("Dana, Loyal: reports in 22 s"); an unknown temperament shows "?" (Sky Q3). Never a hidden dice: the forecast is the outcome.
- **In transit:** the reporter runs; a thin line to the report point; a chip "report lands in n s" (≤ 2 icons over a person, §10); the Scout's Watch shows the box.
- **Kills are shown, not cut away (Sky).** The body lies where it fell until moved, drawn as a body marker; a person who finds it reacts in view (the verb chip); nothing about the report is hidden.
- **Event codes** for Fable: `body.found`, `carry.seen`, `scream`, `witness.react` (verb), `report.started`, `report.stopped`, `report.landed`, `report.verified` (confirmed or unfounded), `told`, `wake.report`.

## 4. Routine discovery

### 4.1 Three ways a body is found by routine (proposed)

1. **By passers** (the city's people on their routines): the §2.2 sight rule at the body's hide level. Measured on the r016 cast in §7.
2. **By a servicing visit** (bodies in a *shut* place only): a bin is emptied by a sanitation round, a crate opened by a delivery hand, a cabinet or service door used by staff. The servicer is a routine of the city, so the Scout can learn it by Watch and the player sees the window. Proposed rows for `life.json` (Pulse owns `tools/life`; Datum signs the schema): `servicing: {bin: {by: "round-robot", periodS: 900, dwellS: 20}, crate: {by: "delivery", via: deliveries}, cabinet: {by: "staff"}}`; the phase is `Hash.Of(container id) % periodS`. A bin is therefore visited in a given 8.5-minute mission with probability 510 / 900 = 57%, and the Watch teaches *when*; it runs in the mission only (Motive §2.4) **[Call 9]**; Waymark's routes must schedule round the mark, and the Watch teaches *when*. The round robot that opens a bin with a body in it phones it in (`robot-report-s`).
3. **By a missed duty:** every staff post checks in every 90 s from its shift start, the shift end a mark (Motive §5.1); a stash does not defeat it. A *tie's* missed expectation is §11 (suspicion, 20 s). Everyone else is missed by their ties at their next shared stop, which is **between missions** (`tie.lapses`, `word.passes`), not in the mission.

Deliveries are rare in a short mission. From `life.json` (`deliveries`: café 2 visits a day, the Office lobby 0.5, the early band 25% of them in 3 hours) the expected number of delivery visits to the Handover block in an 8.5-minute window is (2 + 0.5) × 0.25 ÷ 3 h × 0.1417 h = **0.03**. A district mission with 20 food and trade units expects about 0.5. So delivery discovery is a district-scale rule that does not trouble the Handover.

### 4.2 Hide levels *(changed)*

| level | what it is | seen from | found by |
|---|---|---|---|
| `open` | anywhere that is not a stash (Motive §2.5), lying or carried | 13 m, clear line | passers, watchers |
| `shut` | in a container (the Dumpster) or behind a service door | never | a search at Alert 2 within 30 m, the servicer (§4.1), a call-out, or, if still hidden at the mission's end, a missing person (§14) |

My `screened` level (4 m) is [L] for v1 **[Call 6]**; it remains in the measurements as an optional rule if Sky wants bodies lost behind low screens.

## 5. The awake knocked-out person (proposed)

Motive owns when they wake (`wake-s` 120 s). *(changed)* On waking: a **free** victim reports (below); a **stashed** victim is silent until found; a **restrained** victim cannot move or report until a finder frees them (`free`, 3 s), and then reports at once (Motive §3.3).

1. **Stagger** for `stagger-s` 4 s: stands, moves at 50% pace, cannot call or run, cannot be a threat. A second takedown in the stagger is a new takedown (Motive prices it).
2. **Decide**, by the same list with the victim's own row: staff `radio` (3 s); anyone else `report` to the nearest report point, whatever their temperament, because it happened to them. Exceptions: a `cell` person (loyalty at least 1) says nothing and goes back to their routine; a terrified person (f = 3) `flee`s first and reports from the refuge door. The report is an *assault*: the row "a woken victim" of §3.3 (Alert 1, Trail +4 if not yet charged, Motive §3.2; a knockout never exceeds Alert 1).
3. **Tell:** at the phone, one more call to their best tie (lover first): `told`. The woken guard's lover Inez hears from him.
4. **Back to routine:** staff return to the post (`guard-return-s`, 10 s), others to their routine.

They know place and time, and a face if they were aware of the Enforcer (`enforcer-face`) or it was the Fixer's. A reporter is a volunteer when questioned.

**Hidden (in a stash):** the victim cannot walk to a phone. **They call out every `call-out-s` 10 s**, a `shout` heard within `scream-m` 20 m (Motive §3.3). A call-out is heard, not seen: it makes a *lead* (place, time, memory `heard-alarm`), and a Loyal or Idealist hearer may report "a noise from the bin" as an Alert 1 report whose verify opens the stash. A stash with no one within 20 m stays quiet; a stash by a thoroughfare does not. **Restrained** (8 s by any operative): cannot move or call out; on open ground still seen and found; freed by the finder (`free`, 3 s), then reports at once. **Hidden and restrained:** silent until a search, a servicing round or the mission's end, then a statement (+4 T, Motive §3.2). **Quieted** by the Fixer (Motive F2, not built): facts sealed; a woken free victim still raises Alert 1 but gives no leads.

## 6. Determinism and cost

### 6.1 Determinism (proposed rules)

1. **Integers only.** Delays in ticks (seconds × 10); lengths in cm along the grid route (`Path.Length` in mm); speeds in mm per tick (the engine's convention: cm/s = mm per tick); distances compare as squared integers. No `Random`, no float.
2. **No dice anywhere.** The reaction is a function of the person's integers, Alert, and the incident; the forecast equals the outcome. The only "seeds" are phases: `Hash.Of(id) % period` (bodies' check phase, cameras' scan phase, servicing).
3. **Order.** A tick processes bodies by id, then persons by ordinal id; ties on landing go to the lower person id; one state transition per person per tick; the report with the earliest landing tick wins, then the lower id.
4. **State is explicit and hashed.** Per body: id, position, state, hide level, container, carrier, found tick, incident, the pending reports. Per active person: state, extending the crowd's `Walker.St` (`startled`, `fleeing`, `sheltered`, `returning`) with `checking`, `walking-to-report`, `calling`, `telling`, `seeking`, `looking`, since, target, route and start tick (position is `route.At(elapsed × speed)`, as the crowd's `Run` is). These join `Hash.State`, and the replay of a run from start state and orders is identical (Keel's replay check).
5. **Background people** carry no state (Sky Q5; `tiers.background`): the sim has none yet. When it does, background witnesses are counted for the incident (Trail, Standing, Alert as `witness.sees` says) and, if at least one has a clear line, **one anonymous report** starts from the nearest such person with the Loyal timing. They never `check`, `tell` or talk.
6. **A gunfire event overrides.** Anyone not already `walking-to-report` or `calling` is taken by the existing crowd machine; a person who is in transit keeps going (they have decided), unless fear is at least 2, who `flee`s and reports from the refuge door.

### 6.2 Cost

Measured with the probe on this machine (the engine as in the working tree, .NET release build, one thread):

| | measured |
|---|---|
| the whole engine tick with the 130-person crowd (0:00 to 5:30) | 23.4 µs, so about 0.02% of a 100 ms tick |
| one `Sight` test (13 occluders) | 0.2 µs |
| one grid route (A*) | 0.58 ms (so never per reporter: the distance field is built once) |
| the discovery check, 130 people, 1 body / 4 / 12, linear scan | 0.5 / 6.9 / 5.7 µs a tick (8, 24, 66 sight tests; all 130 placed randomly on the block's grid, the densest case) |
| 1,300 people, 1 / 4 / 12 bodies | 5.8 / 16 / 27 µs |
| 3,000 people | 5 / 22 / 65 µs |
| 6,000 people | 9 / 42 / 135 µs |

The check is a squared-distance compare against every on-street person and a sight test only inside 13 m. The densest district street the life file allows (0.12 people/m²) puts about 64 people inside 13 m, so at most about 64 sight tests (13 µs) per body per check, and checks run every 5th tick. **A full district** (an estimate, since the life file has no district count yet: the plan has 34 plots and a civic block's scope carries 130 simulated people, so about 1,300 to 3,000, with 6,000 as a ceiling) costs **under 0.14 ms a tick with 12 bodies and 6,000 people**, 0.14% of a tick and well under 1 ms a frame even if a tick lands in one frame. Bodies found and reported stop being checked for new finders at the full rate (they only add to the incident's witness list, at 2 Hz). The reactions add `O(active witnesses)` per tick (those within 13 m, within 20 m for a scream, and told ties: tens), each a position lookup on a stored route. What dominates is already in the engine: the whole tick is 23 µs for 130 people, at most 0.18 µs a person, so about 1.1 ms a tick at 6,000. Keel should build the street's position array once a tick and let `WitnessesOf`, `CrowdBlocks`, `CrowdStep` and the body check all read it; today each query walks `OnTheStreet()` and recomputes.

Memory: at most 12 bodies (a cap Keel can choose) × `report-cap` 4 pending reports, and under 64 active witnesses: a few kilobytes.

## 7. The Handover on the r016 cast

Method: the engine with no orders, the walkers on their routines; a body dropped at a place at 48 times (every 10 s from 0:05 to 8:05), "pre" = dropped before 5:00 (30 times), "post" = from 5:00 (18). The finder's reaction is §2.3 at Alert 0 and the report chain of §3.2 to the nearest door (doors only; the 7 boxes change none of these rows). Watchers: the entrance and pickup cameras (live), the guard's cone, the patrol sweep. A median is seconds from the drop to the **first** report landing at the desk; "not reported" means none landed within 240 s *or before the mission's end*, so late drops in "post" count as not reported. Full tables: `probe-output.txt`.

### 7.1 Where bodies are reported, and how fast (13 m, open body; the "4 m" rows in `probe-output.txt` measure a screened level that is not adopted)

| place (block m) | nearest report point (walk) | pre: reported within 30 s / 60 s / not in 240 s; median; by whom | post |
|---|---|---|---|
| guard's door post (51.8, −30.0) | the door itself, 0 m | 100% / 100% / 0%; **4 s**; patrol 66%, camera 23%, staff 10% | 100%; 4 s; patrol 55%, staff 44% |
| pickup, the driver's stand (78.1, −33.7) | official door 4.1 m | 100% / 100% / 0%; **5 s**; staff 100% (guard 29, 4.1 m away) | 100%; 5 s; staff (73, 74) |
| K1 service cabinet 0 (57.8, −43.8) | office door 21.7 m | 53% / 80% / 0%; 29 s; patrol 73%, staff 26% | 55% / 83%; 22 s |
| K3 control cabinet (23.9, −17.0) | office door 34.5 m | 0% / 0% / **56%**; 182 s; staff | 38% / 55% / 11%; 42 s |
| K6 bins, Rocket Lane (46.0, −61.8) | office door 34.0 m | 0% / 0% / **56%**; 206 s; civilian | 11% / 33% / 22%; 76 s |
| **R1 behind the café** (−23.4, −35.0) | café door 27.3 m | **never** | **never** |
| R2 lane, north mouth (49.0, −44.5) | office door 16.7 m | 23% / 36% / 0%; 104 s; staff 96% | 55% / 66% / 33%; 11 s |
| R3 lane, middle (63.0, −44.0) | official door 21.3 m | 66% / 93% / 0%; 13 s; patrol 80% | 77% / 94%; 10 s |
| R4 lane, south end (74.0, −43.5) | official door 13.9 m | 13% / 23% / 16%; 118 s; staff 83% | 38% / 38% / 61%; 5 s |
| R5 office east face (63.8, −18.8) | official door 22.7 m | 70% / 100% / 0%; 19 s; patrol 100% | 88% / 94%; 5 s |
| R6 official tower pocket (72.8, −34.8) | official door 7.8 m | 3% / 13% / 23%; 131 s; staff 76% | 27% / 27% / 72%; 5 s |
| R7 café east end (−18.8, −16.3) | café door 18.6 m | 0% / 0% / 80%; 222 s; staff | 11% / 27% / 0%; 102 s |
| D1 Office service door (63.0, −41.1) | official door 20.9 m | 20% / 30% / 10%; 107 s; staff 90% | 44% / 50% / 50%; 5 s |
| **D2 café kitchen door** (−22.4, −26.0) | café door 30.2 m | **never** | **never** |

Entrance camera looped by the Tech: the guard's door goes from camera 23% to patrol 86% (still 93% within 30 s); every other row is unchanged, because the entrance camera does not see the lane. The patrol robot is the finder of most pre-chime bodies at the door, K1, R3 and R5 (it sees R3 about a third of the sweep, R5 two-fifths, in line with Vantage's `watchers`); rerouting it (the Tech's reroute) removes that channel, and what is left is the staff and the clock below.

### 7.2 The quiet clock: the first person who can see the place, and the stream

| place | open (13 m): first reporter in range | stream starts | within 4 m (measured, not adopted): first | 
|---|---|---|---|
| R1, D2 | none | never | none |
| K3 | guard 65 at 6:52 | | none |
| R7 | guard 65 at 8:02 | | guard 65 at 8:19 |
| K6 | clerk 06 (Loyal) at 6:54; 31 people after | 6:54 | none |
| R2 | **guard 74 at 4:04** | 5:30: 38 people | guard 36 at 6:22 |
| K1 | guard 74 at 4:19 | 5:30: 39 | guard 74 at 4:37 |
| D1 | guard 74 at 4:27 | | guard 74 at 4:44 |
| R3 | guard 74 at 4:28 | | guard 74 at 4:47 |
| R4 | guard 74 at 4:48 (the driver, Mercenary, is in range from 0:00 and keeps quiet) | | guard 74 at 5:07 |
| R6 | guard 74 at 5:11 (driver in range from 0:00, quiet) | | guard 74 at 5:12 |
| R5 | guard 65 at 5:53 | 5:30: 24 | guard 65 at 6:00 |
| guard's door | guard 74 at 4:14; camera and patrol from 0:00 | 5:30: 59 in 60 s | guard 73 at 5:23 |

The service lane (R2, K1, D1, R3, R4) is quiet only from 0:00 to about 4:00: the second-shift guard 74 walks home → official entrance through it from 4:04 to about 5:12. After the 5:30 chime a place that only the 4 m rule watches would still be quiet (7 people within 4 m of R2, against 38 within 13 m); that is the case for the optional screened level, not adopted for v1.

### 7.3 Who finds a body at the two posts (the cast, the walkers)

- **The guard's door post.** Before 4:14 nobody passes; the *watchers* find it: the patrol robot sweeps the door (4 s) and the entrance camera sees it (3 to 23 s). From 4:14 guard 74, then, from 5:30, guard 73 standing the same door (Mercenary, they `radio` in 5 s); in the minute after the chime about 60 people pass (the 58 clerks leave over the next minutes): of the clerks 21 Loyal `report`, 10 Idealists `check` then `report`, 20 Mercenaries and 7 Fearful do not. So a body *left* at his door is reported in about 4 s at any time. The play is the carry (Vantage's `seenOnTheWay`: camera 61%, the door itself 23%, the patrol up to 100% of the path), and the lane that receives it is quiet until about 4:00.
- **The pickup.** The driver, Mei Zhou (Mercenary, cash), is in sight of the outgoing shift's guard **p-records-office-29, 4.1 m away, standing the official entrance from 0:00 to 5:30**; he `radio`s in 5 s. From 5:30 guards 73 and 74 stand the two doors and the pickup is in 13 m sight of the official one. The driver himself is a finder of any body near the pickup and, as a Mercenary, keeps quiet. A driver takedown at the pickup is therefore a reported incident in 5 s on this cast at every time of the mission. For Waymark: take him somewhere out of those guards' sight, or deal with the guards first; the generator put them on the doors because the Office has door guards, and the mission did not know.
- **Inez.** She is Fearful (fear 1) and the guard's lover. If she *sees* the guard go down she `shout`s, `check`s and `report`s (row 2), whatever else her temperament says. She does not see the door (62 m from the café door, no line). If she is turned (Help twice) and a witness, row 2 still beats row 3 for a lover.

### 7.4 One worked chain

Drop at 3:00 at R3 (lane, middle), entrance camera looped, patrol not rerouted. Nobody else is in range until guard 74 at 4:28. The patrol sweeps R3 about a third of the time: from the table, first landing median 13 s, 93% within 60 s. With the patrol rerouted by the Tech the landing is guard 74 at 4:28 + 5 s = **4:33**, 93 s after the drop; after the dial's dispatch (10 s at the Handover's S2) the unit walks from the north street to R3 (about 125 m at 2.2 m/s, 57 s) and verifies at about 5:40. A body at R3 that is moved away between 4:33 and 5:40 makes the report *unfounded*. That is the shape: a real, readable, time-limited play.

## 8. New fields and rulebook entries (proposed; nothing edited)

### 8.1 `contracts/people/people.rules.json`

**No new field on a person.** Every row of §2.3 reads existing fields; add the readers to `person.readBy.fields` so the schema's "every field names its reader" test stays true:

| field | new reader to name |
|---|---|
| `temperament` | §05 witness reaction: which row of the reaction list (the Loyal/Idealist/Mercenary/Fearful rows) |
| `scales` (bravery, resentment, fear, loyalty) | §05 witness reaction: informer (r = 2), terrified (f = 3), scream (f ≥ 2), cell (loyalty ≥ `word-quiet-loyalty`) |
| `ties` (lover, family, workmate, regular) | §05 kin row (lover, family); `tell` (lover, family, workmate, regular) |
| `memory` | new acts `saw-takedown`, `found-body`, `saw-carry`, `heard-alarm`, `reported` |
| `routine` | which people pass which places (discovery); the staff post (check-in) |

New blocks (names are mine; Motive and Keel may rename):
- `witness.verbs` (the nine of §2.1) and `witness.reactions` (the rows of §2.3 as data: `when` over temperament, scale ops, `staff`, `kin`, `cell`, Alert; `does` a list of verbs). Unknown verb = reject.
- `staff`: the closed list of jobs that `radio` (guard, patrol-officer, watcher, investigator) and have a post (check-in).
- `talker.reporter`: a person who reported gives their known facts when questioned (bypasses `talker.test`).
- `knowledge.telling`: `order: [lover, family, workmate, regular]`, `reach: phone-or-20m`, `hops: 1`.

### 8.2 `contracts/rules/rulebook.json` (events, memory, values), agreeing with Motive §7.2

- **Memory acts** (all facts `place-time`): Motive's `attacked`; mine `saw-takedown`, `found-body`, `saw-carry`, `heard-alarm`, `reported`, `missed-expected`; `told` gets a `hearsay` flag; `memory.faceFacts` gets `enforcer: "enforcer-face"` (Motive; Keel scopes it to the new acts if replays must hold). `memory.cap` 8 is enough; `whenFull` drops `heard-alarm` first.
- **Events** (names Motive's where he has them): `takedown.witnessed` (at sight: witness Trail and Standing, **no Alert**; the report goes into my chain), `report.lands` and `report.verifies` (Alert per §3.3, the incident's maximum, the act's Trail on confirmation, the seal from a confirmation at Alert 2 or more), `victim.wakes`, `post.missed`, `person.missed` (suspicion, §11), `alarm.dispatches` (§12), `person.reacts`, `body.found`, `carry.seen`, and between missions `bodies.surface`, `body.grieved`, `missing.worried`, `area.hardens`, `cases.count`. `witness.sees`'s `violence` row stays for gunfire; for the new acts a witness with loyalty at least `word-quiet-loyalty` adds no Trail.
- **Values:** `briefs/013/pulse-witnesses.values.json` (mine) beside Motive's §7.1; where we overlap, **Motive's id wins** (`suspicion-s`, `check-in-period-s`, `area-watch-step`, `area-watch-missions`, `cases-per-level`, `case-level-cap`, `missing-trail`, `response.levels`, `search-stash-m`, `search-stash-s`, `wake-s`, `restrain-s`); the file's note lists what I removed.
- **Seal:** adopted: from a confirmation at Alert 2 or more (gunfire: from arrival).
- **Response:** `response.levels` S0 to S4 (Motive §5.3) is the dial; `plan.areas[*].securityBase`, mission `securityBonus`; my behaviour layer (§13) hangs from the level (`briefs/013/pulse-response-profiles.json`).
- **Life file:** `servicing` (§4.1); robot docks and police posts as world pieces (Vantage, Vela); `report points` as a world export item: `kind: call-box`, derived by R-REPORT.

### 8.3 Checker rules (Splice, Vantage)

- **R-REPORT:** every walkable cell within `report-reach-m` (40 m) of a report point on the walk grid.
- **R-HIDE:** a place that hides a body is a stash (a container or a service door, Motive §2.5); every other place is open.
- **R-SERVICE:** every `shut` container has a servicer row.
- **R-DOCK:** every walkable cell is within `dock-reach-m` of a dock or a post on the grid walk, and each district has at least the robots and units its **highest level** names (so the dial's numbers can be delivered).

## 9. Scenarios for Keel (the cases the rules must show)

1. A Loyal finder at 10 m, door 20 m: lands 2.0 + 14.3 + 6.0 = 22.3 s, response on the street 12 s later; `report.landed` once.
2. A Mercenary at Alert 0: nothing; the same Mercenary at Alert 2: reports.
3. A Fearful finder: `avoid`, a `tell` of a tie within 20 m; questioned later, a Talker.
4. An Idealist: `check` (3 m, 2.0 s) then `report`: later than the Loyal by 4 to 10 s.
5. Staff: `radio` 5 s. The patrol robot: 4 s. A looped camera: nothing. A live camera: next scan boundary + 3 s.
6. Kin: the lover finds the guard and `shout`s; hearers `look`.
7. Cell: a turned person (loyalty at least 1) sees a takedown and says nothing; a turned lover still cries out.
8. Reporter stopped: knocked out before landing; the next earliest lands.
9. Verify: body moved before the unit's arrival, so unfounded: Alert stays, no seal; body present: confirmed, seal starts.
10. Woken: stagger 4 s, report, `told`.
11. A scream: hearers within 20 m turn; a Fearful hearer walks away.
12. Replay identical for each; hash includes the new state; an old script (no takedown) hashes as before.
13. A body in a `shut` place is never seen; an open one at 12.9 m yes and 13.1 m no.
14. Suspicion: Vale at the bay, the driver stashed or gone: the missing-person alarm lands at about 399 s (370 + 20 + 3 + 6) and is Alert 1; a turned driver or a turned Vale: no alarm; a Vale being Taken reports nothing; a summoned lover is not missing.
15. A knockout is never above Alert 1 however it is found, confirmed or reported; a kill found is 2; a takedown seen and reported is 3; a seen takedown nobody reports raises nothing.
16. The seal: starts at a confirmation at Alert 2 or more only; a confirmed knockout starts none; unfounded starts none; the 60 s search at Alert 2 or more opens stashes within 30 m, 10 s each, nearest first.
17. A turned witness (loyalty at least 1) adds no witness Trail; Standing still moves.
18. An alarm builds the same destination list and the same assignment twice; units launch one every `launchGapS`; the forecast at a point equals the arrival; level S0 to S4 change counts and times only through `response.levels`.
19. A missing person: a hidden kill at mission end is a case (+2 T, no Menace, ties' fear +1); the block gets +1 guard and +1 camera for 3 missions; every 2 open cases is +1 level, capped at +2; a body found later converts the case.
20. A call-out from a stash is heard within 20 m and may be reported (Alert 1); hidden and restrained is silent.

## 11. A missing expected person (Sky answer 6; Motive §5.2 sets the number)

**The rule is about routines, not about Vale.** Anyone who is due to meet a tie at a place, and finds them missing, grows suspicious, looks, and raises the alarm like anyone who finds a body.

### 11.1 Expectations come from the people file

An **expectation** is `(waiter P, expected Q, place, from, to)`, derived once at load from the routines, with no authored list:
- P and Q have a tie of kind lover, family or workmate (a regular only customer-to-staff; debtor and rival never); and
- both routines have a stop at the **same place id** with overlapping windows.

It runs both ways. It **opens** when P is at the place and the stop has begun: `max(P's arrival, the stop's start)`; it **closes** at the end of the shared window. Motive's other examples fit the same rule: a relief guard expects the outgoing one at the handover (a workmate sharing the post's stop), a lover expects a summons.

On the r016 cast, in the mission window 0 to 510 s: **143 expectations** (106 workmate, 30 family, 7 regular); **only 2 have both people on the street**: Vale and the driver at `office-bay`, 330 to 450 s (the engine plays Vale at the bay at 370 s). The rest are indoors; an indoor waiter notices a missing workmate the same way and raises the alarm at the unit's telephone, off-screen, no walk. None involves the cast guard: his ties share no stop with him inside the window, so his absence is caught by the 90 s mark, not by a tie.

### 11.2 Missing, and the delay (Motive §5.2)

Q is **missing** at tick t in the window when P is at the place and Q is not within `meet-m` (6 m) of the place's stand, and Q's routine says they should be there: Q is down, dead, carried, held or stashed, or not where the routine puts them. Q away on an allowed errand (a lover summoned to a call, the existing `summon` visit) is not missing.

After **`suspicion-s` 20 s** at the place without them (Motive; the first 10 s P scans the place and Q's other routine stops within 30 m, and if a body is in sight the ordinary find, §2, takes over) P raises the alarm: a report, at once, by the nearest report point; staff `radio`. It is a **missing-person alarm, Alert 1**, one incident with the takedown it uncovers, and it sends the response by §12. Kin rule (Motive §2.2): no check, whatever the temperament. A `cell` waiter (loyalty at least 1, or `Turned`) says nothing (a turned driver does not report Vale missing); a turned lover still raises it (row 2). **A held person reports nothing:** a Vale being Taken cannot raise it, so the Take must start before suspicion fires.

**Temperament (where I differ, §0a):** Motive's number is flat. I propose the 20 s as the base for a Loyal waiter and scale it by temperament, in per-mille values `suspicion-fearful-pm` 500, `suspicion-loyal-pm` 1000, `suspicion-idealist-pm` 1500, `suspicion-mercenary-pm` 2250 (10, 20, 30, 45 s). **Default here: all 1000 (flat 20 s).** Motive and Sky decide whether temperament should matter here; if it does, a Mercenary Vale (r016) waits 45 s, not 20.

**Vale and the pickup, worked (flat 20 s).** Vale reaches the bay at 6:10 (370 s) and the driver is gone (taken at the pickup with the pickup camera down, Sky's case). Suspicion fires at 6:30. He walks 4 m to the official entrance (3 s) and calls (6 s): the report lands at about **6:39** (399 s), Alert 1. Motive's route D has the Take start at 6:10, before the 20 s, so Vale is held and reports nothing. With the temperament multiplier Vale (Mercenary) would land at about 7:04; a Fearful one at 6:29.

**Check against the reference runs.** Routes A (3:31, 4:27) and G (4:50) end before the first window opens; B ends at 7:40 with a turned driver, who is quiet. Nothing recorded changes. Motive and Waymark should confirm the `office-bay` stop times (the engine's Vale timeline and the people file differ by 20 s).

**Cost:** the expectation list is built once (143 rows for 130 people; at most 8 ties each); per second the engine tests only the open expectations whose waiter is on the street (tens).

## 12. Where an alarm sends the response (Sky answer 6; Motive §5.2 and §5.3 set the counts)

Sky: "raising alarm brings in robots, police etc to the scene, and to where their connections are meant to be." An alarm is any landed report that calls the response (found kill, seen takedown, a missing-person alarm, a missed mark, gunfire).

### 12.1 The destination list

When an alarm lands the engine builds an ordered list of destinations, in this order and no other:

1. **D0, the scene:** the reported place (the body's place; for a missing person, the place they should be).
2. **The missing person's duties and ties, now (Motive §5.2):** their post, the vehicle bound to them (the official driver's car, at its bay), and the expected stand of each tie of kind lover, family, workmate, in that order and by id, from the stop their *routine* puts that tie at this tick.
3. **The alarm-raiser's ties, now (Sky's words):** the same for the raiser's ties, up to `alarm-spread` 3. **[Sky 6: Motive reads "their" as the missing person's, Sky's quote as the raiser's; I implement both tiers in this order, and the dial's robot count bounds how many go.]**
4. **At Alert 3 only:** the units the dial adds post at the district mouths (the refuge `mouth:` points), one each.

Duplicates within 13 m collapse into the first. A tie with no street stop (indoors, `beyond`) is represented by its unit's door, so a unit knocks there.

*Vale's example:* D0 = the pickup bay (the driver's place); the driver's car is bound to the bay (collapsed into D0); the driver's ties are Vale (the raiser, skipped) and p-records-office-39 (indoors until 5:30: the Office door); Vale's other ties expect nobody on the street. At S2 the Alert 1 sends 2 robots: one to the bay, one to the Office door.

### 12.2 How units are chosen and routed (deterministic)

1. **Counts come from the dial** (§13): at Alert 1 the level's `a1Robots` robots; at Alert 2 or more `a2Units` units (2 officers each) and `a2Robots` robots; then +1 unit every `reinforceS` up to `unitCap`; Alert 3 adds the level's extra units. A robot is a patrol unit (health 4, pistol, trained; counts against the 30-combatant cap).
2. **Roster** at the tick: the patrol robot (its current sweep position), other robots at their docks, officers at the nearest police post (or the district mouth in the legacy site), in id order.
3. **Assignment, destination by destination in list order:** the free unit of an allowed class with the smallest travel time `ceil(route length ÷ speed)` in ticks (ties on unit id). The first unit takes D0; at Alert 1 robots take D0 and then one destination each. Officers take D0 and tier 2; robots take what is within `robot-reach-m` 60 m. A unit takes one destination; if units run out, destinations wait for the first unit to finish (priority order). **Reinforcements** go to the destination with the fewest units (ties in list order).
4. **Routes** are the grid shortest route from the unit's position at assignment (ties on cell id), computed once; the unit's position afterward is `route.At(elapsed × speed)`, as the crowd's runs are.
5. **Launch:** `dispatchS` after the landing (the dial); a patrol robot's wire is shorter (`robot-wire-s` 4 s); units leave in assignment order, one every `launchGapS`.
6. **Arrival:** a unit checks the place 10 s, then robots and patrol hold it (a held scene is a watcher's cone) and officers search (60 s; at Alert 2 or more the stashes within 30 m, Motive §2.3). On the way, what a unit's cone sees is seen by security (a carried body, an operative).
7. **Re-run** only at the next landed alarm or an Alert step, never per tick. Integer arithmetic, no random choice.

**What the Handover's legacy numbers are** (`probe-output.txt`, mode `resp`, walking from the security entrance at 2.2 m/s): guard's door 48 s, R3 lane 52 s, pickup bay 60 s, getaway bay 27 s, café door 22 s; from the patrol's ends at 0.85 m/s the guard's door 14 to 24 s, the pickup bay 50 to 59 s. Motive's order card ("police in ~n s") adds the forecast landing (staff 5 s, a Loyal civilian ~22 s), the dial's dispatch and the walk: a body at the guard's post found by a staff member: 5 + 10 + 48 = **63 s** from a mouth entrance, and about 20 s if a police post stands at the Office door.

## 13. The response behaviour on Motive's dial: one dial, as values (Sky's swarm note)

Sky: "it's easy to swarm the play area with robots if we need to temper the game play." The size and speed of the response are the difficulty dial, and Motive owns it: **`response.levels`, S0 to S4**, the district's `securityBase` (civic 2, harbour 1) plus open cases (÷2, cap +2) plus the mission's `securityBonus` (Motive §5.3). Tuning is a values change; no code. I own how the units move and look.

### 13.1 The dial (Motive §5.3, reproduced as the reference; Motive's file wins)

| S | name | dispatch | Alert 1 sends | Alert ≥ 2 sends | reinforce every | unit cap | seal | Alert 3 adds |
|---|---|---|---|---|---|---|---|---|
| 0 | lax | 20 s | 1 robot | 1 unit | — | 1 | 180 s | +1 unit |
| 1 | routine | **12 s** (today) | 1 robot | 1 unit | 60 s | 2 | **145 s** (today) | +1 unit |
| 2 | watchful | 10 s | 2 robots | 1 unit + 2 robots | 40 s | 3 | 145 s | +1 unit, doors lock |
| 3 | guarded | 8 s | 2 robots | 2 units + 2 robots | 30 s | 4 | 120 s | +1 unit, doors lock |
| 4 | swarm | 6 s | 3 robots | 2 units + 4 robots | 20 s | 6 | 90 s | +2 units, doors lock |

A unit is 2 officers (health 3); a robot is a patrol unit (health 4). My four named profiles and my modifiers (early mission, watch, Hero, Menace, turned dispatcher) from the second pass are **dropped**: an early mission is `securityBonus` in mission data; a missing person moves S through cases (§14); Standing is Motive's alone. The Handover is S2: the loud reference gets +2 robots and a unit every 40 s; re-record it, or use `securityBonus` −1 for S1 (today's numbers). **[Call 3]**

### 13.2 What I add on top (all values, in `briefs/013/pulse-response-profiles.json`)

| lever | default | what it does |
|---|---|---|
| `robot-alarm-cm-s` | 85 (the patrol's speed; nothing changes) | how fast robots converge: raise to 120 or 150 and the same S swarms faster. A second dial for a swarm that arrives, not only one that is large |
| `launch-gap-s` | 1.5 | units leave one at a time, so a swarm streams out and can be counted |
| origins by class | patrol: its sweep; robots: docks; officers: police posts, else the mouth | where each class starts. More docks and posts mean the same S **arrives** sooner; they are world pieces (Vantage, Vela), checked by R-DOCK |
| `hero-block` (optional, off) | 0 | per Hero step: +3 s dispatch, −10% speed ("citizens block cars", GDD §05). Motive's S moves only with cases and mission data, so this is a switch, not in the table |
| unarmed `warden` (optional, off) | not in the roster | a civic robot with no weapon: converges, holds a point, watches, rings the desk, never fights, does not count against the 30 cap. Sky's swarm could use many; Motive's robots are armed patrol units. Off unless Motive and Sky want it **[Call 3]** |

Robots stay clockwork: a robot has one fixed alarm routine (leave the dock in order, walk the assigned route, check 10 s, hold, return when stood down) and no choice anywhere. Stand-down: 60 s after the last alarm at Alert 1, 180 s at Alert 2, never within the mission at Alert 3.

### 13.3 Readable at play zoom, and in the price

- The dock lights **flash** (the alert glyph) on the landing; units leave **one every `launch-gap-s`**, so the swarm streams out and can be counted; a robot is a distinct silhouette, an officer a figure.
- **The price shows the arrival** (Motive's card: "Alert 2 · police in ~n s · +1 unit every 40 s"): the forecast landing plus the dial's dispatch plus the walk from the unit's origin. For any ground point the hover shows "response here: robots n s, police m s", the same assignment run for an alarm raised now at that point, from distance fields built once at load. It equals the outcome except for the patrol's sweep position and units already busy.
- Every unit shows an **arrival chip** when hovered or selected; the first unit of each class always does; at most 12 drawn with chips; a dock or post shows its stock as pips.
- Cost: a unit is a stored route and a position lookup per tick (40 units under 10 µs a tick), one A* (0.6 ms) per assignment, once per alarm.

### 13.4 Behaviours and timings to test (Motive sets the numbers, Waymark and Suture run them)

1. **The loud route** from the start at S1, S2 and S3: the unit at the Office side in `dispatchS` + 48 to 60 s from a mouth, `dispatchS` + ~10 s with a post at the Office door. Find the S at which fighting out from 0:00 loses (Sky: "fighting out should be a last resort").
2. **A found kill** in the service lane: the first robot at the scene in n s, officers in m s, per S; the stash search opens the Dumpster 10 s into the search.
3. **The same alarm at S1 and S2 and with `robot-alarm-cm-s` 85 and 150.**
4. **A swarm of S4** on one block: the block stays readable, the framerate holds, and the combatant cap holds (reinforcements stop at 30).
5. **Replay:** the same orders give the same swarm (positions and arrival ticks); a recorded run at S1 replays unchanged.

## 14. Missing persons: what changes next mission (Sky answer 2; Motive §2.6 sets the numbers)

Sky: a hidden body becomes a missing person, not a found body; the district answers with more guards and cameras in that area next time; the pressure is similar, its cause different. **One mechanism, Motive's:**

| | found killing | missing person | knockout |
|---|---|---|---|
| Trail | +6 | **+2** (`missing-trail`) | +4 unless quieted |
| Standing | −1 | none | none unless seen |
| ties | grief | **worry**: fear +1 for each family, lover and workmate tie | the victim's own fear |
| the block (area watch) | **+1 posted guard, +1 camera** for 3 missions | **the same** | none |
| district level S | +1 case, closes after 3 missions | +1 case, **stays open** until the body is found | none |

S = base + ⌊open cases ÷ 2⌋ (cap +2). My second-pass "watch level 0 to 3" is **dropped**: the area watch is Motive's `area-watch-step` 1 and `area-watch-missions` 3, and the district's readiness is S.

### 14.1 The record (mine)

At the end of a mission, each takedown victim still **shut** (stashed, or hidden and restrained) becomes a case: `{victim id, last-seen place, last-seen time, routine stops (home, work)}`. A victim who woke and reported, or whose body was found, is an ordinary incident. A later search, servicing round or routine that finds the body converts the case to a found killing (Motive §2.6). The investigation's lead is the **last sighting** (a watcher's log or a person's memory).

### 14.2 What "+1 guard, +1 camera on the block" is in routines and patrols (mine; generated, never hand-placed)

- **The guard** is a generated staff person with a post routine at the staffed door nearest the last-seen place (`tools/life` adds them to the cast): a post stop on the street, the staff `radio` reaction (§2.3 row 1), a 90 s check-in like every post, and a Watch teaches the marks. Two cases on one block post two guards at different doors, nearest first, ties on door id.
- **The camera** is a live camera on the pole site that sees the last-seen place and its approaches best (derived from the world's pole sites by coverage, ties on id), watched by a person (scan 20 s); the Tech can loop it like any camera.
- **Patrol and robots:** no extra rule beyond S (the dial sends more robots, sooner). My second-pass options (a shorter sweep, 60 s check-ins) are dropped to keep one mechanism; they can be a value later.
- **Ties of the missing person** are uneasy (fear +1, Motive's worry); their suspicion (§11) is unchanged. The victim's routine places become leads.

### 14.3 Determinism

Cases, S and the area watch are campaign state, hashed with the mission's start state; guards and cameras come from the same tools that make the cast and the world, by rule from the record, ties on id. The same orders give the same next mission.

## 15. Open items

- **Sky (through Aster), Motive's calls 1 to 9** (the file's §8): this file is written to Motive's version, and these hinge: **Call 1** (Alert on landing for silent acts: §0, §3.3, the whole witness game), **Call 2** (a knockout never above 1), **Call 3** (the five-level dial and the robots as armed patrol units; my unarmed warden is off), **Call 4** (Mercenaries quiet until Alert 2), **Call 5** (turned witnesses add no Trail), **Call 6** (two hide levels), **Calls 7, 8** (call boxes by R-REPORT, no cut), **Call 9** (bin round in the mission only). Also Sky 6's reading of "their connections" (§12.1) and whether temperament should scale suspicion (§11.2).
- **Motive:** the S table is yours; check §12.2 reads it right (reinforcements go to the destination with the fewest units); the numbers behind my optional levers (`robot-alarm-cm-s`, `hero-block`).
- **Keel:** the street position array shared by all queries (§6.2); `Person` and unit states in `Hash`; the destination list and assignment (§12); expectations built at load (§11); `response.levels` as data; background people when they exist.
- **Vantage / Splice / Vela:** R-REPORT, R-DOCK and the call box placement (7 boxes are an example); robot docks and police posts as world pieces; the Dumpster and D1 (Motive §2.4).
- **Datum:** `plan.areas[*].securityBase`; the district population and a background layer.
- **Waymark:** the shift-change guards (p-records-office-29 outgoing, -73 and -74 incoming) are the Handover's most important witnesses (§7.3); route G must schedule round the Dumpster's servicing mark and the check-in marks; Vale's suspicion at 6:30 (landing about 6:39) against the Take; the engine's Vale (370 s) and the people file (330 s) disagree.
- **Latch, Glyph, Fable, Forma:** the in-transit marker, the forecast line, the response clock on ground hover, arrival chips, event codes (§3.6); a body marker (kills are shown); the robot silhouette.
- **Facing at a stop (Motive §1.3 asks me):** still to do; I will propose a facing for each stop kind (a door stop faces the street, a bay stop faces the bay) from the world.
