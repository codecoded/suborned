# Brief 027 · Hostile initiative: who starts a fight on the cell (Pulse)

2026-10-21. Answers Sky: "definitely need officers / bots etc firing at you if you're on high alert, do something wrong etc - rivals especially." Context: Syndicate fights, weapons free for drawn cell agents (brief 041), rivals' second front (brief 039), parked far end brief 037.

Evidence is a code and rulebook read of engine 0.18.0 (`GameAi.cs`, `GameKite.cs`, `GameCombat.cs`, `GameRange.cs`, `GameResponse.cs`, `GameTakedown.cs`, `GameFight.cs`, `Game.cs`, `GameSight.cs`, `GameFightLab.cs`), `contracts/rules/rulebook.json`, `briefs/027/pulse/kite-response.md`, `briefs/027/motive/tuning.md` §6.4, §7, §8, §10, §12 and GDD §05–§07. I ran nothing and edited nothing under `sim/`, `contracts/` or `unity/`. Every number below is [S], Motive owns it; "existing" means a rulebook value that is already there.

## 1. What the engine does today, per side (one line each)

The single test behind all of it is `FoesInReach` (`GameAi.cs:70-84`): before the final fight a hostile counts an operative as a foe only if he has a weapon drawn, has fired, holds Vale, or security saw him do violence (`markedFoes`: a takedown or a carry seen by armed security). Inside the final fight every operative is a foe. **Alert level alone never starts a shot.** It only changes who is sent, how many, and how fast.

| Who | When it fires unprompted today |
|---|---|
| **Final-fight officers at their stands** (loud branch, Alert 2+ or any shot when Vale is taken) | At once, no react: every operative inside 15 m on a line, no matter what he holds. They exist only after the Enforcer has Vale; they do not exist before. Lab only: a stand is pulled into the car's ring. |
| **Response unit** (`security-1`, `security-2`, from the north street) | Only at a drawn weapon, a shooter, the Vale holder or a marked operative, inside 15 m, inside its cone, after the 2 s react. A holstered agent walks past them at Alert 3. |
| **Brief-013 police units** (found body, alarm; officers) | Same test as the response unit. A found body brings them and holds the scene; it does not make them shoot. |
| **Door guard** | A drawn weapon seen (Alert 2, he engages after react), a takedown or carry seen (marked), or being shot at. Never at Alert alone. |
| **Patrol** | Same as the guard, plus it breaks its sweep to gunfire within 30 m and turns on a drawn weapon. A found body only rings the desk (4 s). |
| **Civic robots** | Armed: a robot is "an armed patrol unit" (GDD §05), a pistol, trained, health 4, cone sight only, counted in the 30-combatant cap. Same test as the units, so they fire only at a drawn weapon, a shooter, the Vale holder or a marked face. They never fire at a holstered agent at any Alert. |
| **Cameras** | Never. They log, report at the next scan + 3 s, and a drawn weapon in view makes Alert 2 and calls the response. They are targets (health 1), not shooters. |
| **Rivals before gunfire** | Never. Their cone is off until `RivalsDrawn` (`GameSight.cs:149`, `RivalsWatching`): the car parks at 3:10 and the crew "watches the building", not the street. |
| **Rivals after gunfire** (loud branch) | `DrawRivals` brings them in 16 s later to the Office front; there they face away and fire only at an armed operative that walks into their cone (Waymark's c027 check: they stop at the door and never act). |
| **Rival ambush** (safe branch) | The one real rival-started fight: it springs when the Enforcer brings Vale within 20 m of the getaway car, aimed after `aimNew`, then every operative in reach is a foe. A fixed ring, not a read of what they saw. |

So today the cell starts almost every fight. The two exceptions are the safe-branch ambush and "a guard sees a drawn weapon", which is the cell starting it too.

## 2. The proposal: one rule, "intent", from facts the sim already has

No scripts and no new events. One function replaces the inline `foe` expression at `GameAi.cs:76`:

`Provoked(body, operative) -> {none, react, fire}`, evaluated for every hostile body that has the operative inside its weapon's effective ring on its own line of sight. It reads only:

- the operative: weapon drawn, has fired, holds Vale, carries a body, **face known** (any `Remember` entry about him in a witness, the guard or the watcher log: people.rules already holds them, and `markedFoes` is the security-saw-violence subset);
- the world: Alert (0-3), the security level S, the Heat band (unknown, noticed, wanted at 6, infamous at 10), gunfire this mission;
- the body: its kind (officer, guard, patrol, robot, rival), its cone.

Its answer is a ladder. The rungs are data (`ai.intent`, a new rulebook section; unknown = reject), not code.

### 2.1 Police and officers (response unit, police units, final-fight officers, the guard, the patrol)

| Rung | They treat as a foe | Beat before the first shot |
|---|---|---|
| Alert 0-1 | drawn, fired, Vale holder, marked (today) | **A "drop it" beat for a drawn weapon only: the lab's police warning (`lab-warn-ticks` 20 = 2 s, one per watcher, heeded = holster and +1 T). Promote it to the default for police.** Violence already done (marked) gets no beat. |
| Alert 2 (search) | + **a face the district knows** | None beyond the 2 s react (`react-s`, a reaction time, not a chance to surrender). |
| Alert 3 (lockdown) | + **anyone** in the cone inside 15 m | The 2 s react. A cell agent who breaks the line inside it is not fired on (the existing cone and sight rules). |

- The rung each Alert needs moves one step per security level, as data: S0 never; S1 face at A3 and no "anyone"; S2 face at A2, anyone at A3; S3 the same; S4 face at A1, anyone at A2. The Handover runs at S2. [S] starting values, Motive's call.
- **Initiative, not only reply:** a police body that has a foe flagged but beyond its effective ring (inside cone reach, 18 m) closes to its stop point with the kite response's own close-in (stop reach 90 %, walk 2.2 m/s, 20 m budget, 10 s lapse, `GameKite.cs`), where today it stands and waits. The trigger is a sighting instead of a shot. No new walk code; no foot race (the existing limits).
- **Final-fight officers** already shoot every operative. The change there is Motive's §7.2 rule 1 (press for the Vale holder), which stands; brief 039 item 3 asks for more.

### 2.2 Rivals (more aggressive, as Sky said)

| Rung | They treat as a foe | Beat |
|---|---|---|
| Before gunfire and Alert 0-1 | **drawn, fired, or holding Vale, anywhere in their cone** | None. Rivals never warn (Motive §6.4). A 2 s react, then fire. |
| Vale holder seen | The Enforcer with Vale in a rival's cone inside 15 m starts the ambush at once, not only at 20 m from the car | 2 s react, then the whole crew is alerted together (the kite side rule). |
| Gunfire anywhere, or Alert 2 and up (drawn by `DrawRivals`) | **every operative in sight inside 15 m**, as the final fight does today | 2 s react. |

- **They get eyes.** From arrival (3:10) the crew has its cone (existing `rivals.reach` 18 m, `half` 8 m) on the street, facing the Office. This is the one change that alters the quiet routes; see §4. It is also how the Scout reads them: the cone is the Scout's skill (GDD §03), `rivals: learn at arrival, routine: none`.
- **They press for Vale (brief 039, the second front).** Once drawn, a rival crew's objective is the Vale holder, not the Office front: `Advance` with the holder as its foe point (cover within `ai.seek.rival` 8 m, else the stop point), instead of walking to the door and facing away. It uses the machine that already runs the ambush; nothing new moves.
- **Aggression with a limit:** speed 1.6 m/s (the cell runs 2.8, officers walk 2.2), break past half down, 3 health, untrained (750 ‰). The cell can out-run, out-shoot or out-think them; they cannot out-wait it.
- **Standing:** a fight the rivals start is "fired first = no" under fight.public, so it costs the cell no Standing, and breaking feared rivals is +1. Their initiative is a lever the player can use: a feared rival gang that opens on the cell, in public, is a Hero move.
- **Heat (later, §06 frequency):** at Heat 6 (wanted) the next mission's rival crew arrives already facing the cell's start ("rival hit"). Heat is in the sim already (`Start.Heat`); this needs a Waymark template line and Motive's threshold, no engine change beyond the ladder.

### 2.3 Robots

Armed already; "clockwork-smart" (GDD §01) means they follow rules, never judgement. So robots do **not** take the "anyone" rung. They fire on:
- today's list (drawn, fired, Vale holder, marked);
- **a face on the list**: any operative the district has a face for, at Alert 2, or at once if the Heat band is wanted or infamous, wherever the Alert is. Cone only, no near sense, no close-in beyond their stop point, never past `robot-reach-m` (60 m) from their start. Their face display changes expression at first sight (Sky's own line in brief 031); that is the 2 s react and the readable cue.

Armed drones (brief 037) are the next row of this same ladder, "infamous, Alert 3": not designed here.

### 2.4 Cameras

Unchanged. They watch and report. Making them shoot would break "calls are voice, watched by people" (GDD §01) and make a cone you can't read.

## 3. Weapons free: how it meets this

- **Hostiles starting a fight make it two-sided on their own.** A hostile that aims at a cell agent is a "combatant" (`Combatant`: `IsOpId(g.Target)`), and `FightOn` turns true when anyone has fired on the cell. Drawn agents on stance return or free then answer; the panic order, free running, and the 0.5 s going-loud card all follow from existing rules. Nothing new is needed on the cell side.
- **The beat is the window.** The 2 s react (and the warning at Alert 0-1) is when the player's agents can holster, break the line, or draw. A holstered agent shot at does not draw by itself today (weapons free covers drawn agents). Open question for Motive and Latch: should an agent a hostile aims at draw on its own (the lab draw is 0.3 s)? Without it, "firing at you" lands on the unarmed Scout and Fixer for free.
- **Chaos has a ceiling.** Crowds within 30 m scatter; nobody is hit by a stray shot (existing); hostiles never target civilians; robots and officers count against the 30-combatant cap; the seal clock still runs.

## 4. What could make stealth impossible (flag, with the guard I propose)

1. **Rivals with eyes at Alert 0.** The Handover rival car parks near Vale's pickup at 3:10, and routes B and G Take Vale there at 5:50. A crew that sees the street would end every pickup route. Guard: at Alert 0-1 rivals fire only at a drawn weapon, a shooter or the Vale holder; a holstered agent in their cone is safe; the Take must be out of the cone (the Scout can learn it; turned driver, false tip 3:50 and "rival car blocked" already change it). Done-check: P-A, P-B, P-G (and their hashes) unchanged, else it is a bug. If it still closes a route, the rival cone stays off until Alert 2 and only §2.2's last two rows apply.
2. **Alert never falls inside a mission.** "Anyone at Alert 3" is permanent once a takedown is seen on landing or a civilian sees violence. Guard: cone and sight only, 15 m, crowds block sight, the 2 s react, and the dial puts "anyone" at A3 from S2, never below A3 at the Handover.
3. **Careless routes change.** Waymark's careless and loud recordings reach Alert 2-3 and will move (rows `loud`, `loud-fast`, `loud-fight`, `careless-*`). Quiet rows hold Alert at 1 or less.
4. **A held scene (found body)** brings officers but must not make them fire. It does not: the face rung applies only to a face that is known.
5. **Standing:** police firing first on a cell that did nothing visible would be unfair. Guard: every rung is a fact the player caused (a draw, a seen act, a known face, a high Alert, Heat); nothing fires on a clean, unknown, holstered cell at Alert 0-1.

## 5. How it reads in the street (readable over rich; no sentences)

- The hostile's cone swings to the operative (the "they know" turn), the same cue the kite response uses; a robot's face changes; an officer's arm comes up for the warning.
- A cone that would fire now (given the Alert and the faces known) wears the cones' "armed" motion cue, so the Scout's picture shows who is hot before you step in. Speech only: a bark ("Drop it!") is allowed copy; Fable writes it to the plain-copy rules.
- No colour is claimed; shape and motion only.

## 6. Owners

| Owner | Builds |
|---|---|
| **Pulse (me)** | The `Provoked` rule and its ladder as a spec; close-in on sight (reusing the kite code); rivals' eyes, press for the Vale holder, and the ambush trigger by sight; the street cues list; the scenarios to assert (below). |
| **Motive** | Every number: the dial columns, the "face known" and Heat rungs, the 2 s react at Alert 2/3, whether the warning becomes the default, the rivals' cone and press, Standing for a rival-started fight, the TODAY / LAB ruling (his convention: lab first, `today` identical until Sky says), the cheap-win check on the loud rows. |
| **Keel** | The engine: replace `GameAi.cs:76` with `Provoked`; rulebook `ai.intent` (schema, ref, test, unknown = reject); rivals' cone before `RivalsDrawn` (`GameSight.cs:149`, `RivalsWatching`); sighting trigger into `KiteBody`'s close-in; hashing only when set; lab switch `fight.lab.intent`; scenarios; `hashes --start-text BASE` unchanged with the switch off. |
| **Waymark** | Re-record the Handover routes that reach Alert 2-3; place the rival cone against the pickup routes; one route that lets the rivals open on the cell in public; Heat-triggered rival hit as a mission template line (later). |
| **Suture** | Retest: the quiet rows unchanged; no cheaper loud win; the stealth-impossible check in §4. |
| **Latch, Glyph, Forma, Fable** | The cues in §5 and the bark lines. |

Scenarios to assert (Keel): holstered agent in an officer's cone at A1 draws no fire; the same at A3 draws fire after 2 s and not if he breaks the line; a drawn weapon at A1 gets the warning, holstered in time costs +1 T; a robot fires on a known face at A2 and never on an unknown holstered one at A3; rivals fire at the Vale holder in their cone at A0 and not at a holstered Scout; a rival crew drawn by gunfire advances on the holder, not the door; P-A, P-B and P-G unchanged; the same orders replay to the same hash.

## 7. Questions for Sky

1. At Alert 3, should every officer and the guard fire on any agent they see (after 2 s), or only on faces they know? I recommend "anyone" at A3, "known faces" at A2 (§2.1).
2. Should the rival crew see the street from the moment their car parks, so a Take in their cone starts a fight? It is the strongest "rivals especially" and the one change that can close the pickup routes (§4.1); the guard is that the Take can still be made out of their cone.
3. Armed robots on a known face at "wanted" Heat, wherever the Alert is: is a machine shooting a list the tone you want ("optimistic surface, coercive underneath"), or should robots stay at drawn weapons and violence seen until drones (037)?
4. Should a holstered agent a hostile aims at draw by itself?

## Sky's answers (2026-10-21)
1. **Auto-draw when aimed at: no**, "but the player character avatar should clearly show they're being shot at": an unmistakable under-fire cue on the agent's portrait (and in the world). Owner: Glyph (playground first), then Latch.
2. **Alert 3 does not mean "anyone in a cone": no.** "whole point is each character has their own history, scout for example is stealth - shouldn't be punished if enforcer goes in wild." Hostile intent is per agent: an agent is fired on for what it is seen doing or for its own known face, never for another agent's acts.
3. **Rivals watch the street from 3:10: yes.**
4. **Robots fire on known faces: yes.**
Also (§14 note in Motive's tuning.md): "fight on" ends on contact, not a clock.
