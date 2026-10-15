# 011 · Running and real-time shoot-outs: two rule proposals (Motive, 2026-10-15)

**For Sky, via Aster. These are proposals only:** nothing in the GDD, the rulebook or the engine has changed. Keel implements whatever Sky approves.
Sky's questions were "How do I make players sprint?" and "I want realtime shoot outs like Syndicate. How would we add that to the controls?"
Research and sources: `briefs/011/research/realtime-fights.md`. The fight calculator behind every number: `briefs/011/motive-fights/fights.py`.

## Summary

**Running: speed costs attention.**
- **Control and speed.** Double-click the ground to run, at 2.8 m/s, twice the walk. There is no stamina.
- **Who notices.** Anyone who would witness an act notices a runner: people within 13 m with a clear line, and the cones of the guard, the patrol, the cameras and the response unit.
- **What people do.** They remember where and when they saw the runner, and the Fixer's face if it was him. That is a lead for investigators. It costs no Trail.
- **What security does.** If the guard, the patrol, a live camera or the response unit sees a runner, Alert goes up to 1 (inquiry), once per run. A guard who saw you no longer fights at your side in the safe final fight.
- **Who can't run.** Nobody runs while carrying Vale or a downed operative, while tethered, or while doing an action. Nobody shoots while running.
- **In a fight, running is free.** Everyone runs then.
- **The price chip** on the ground always shows two lines: walk time, and run time with who will see it.

**Shoot-outs: Syndicate-fast, but every bullet is still traced.**
- **Who is armed.** The whole cell can carry a sidearm, one per Arms point. The Enforcer keeps the carbine and stays the only one who carries Vale. The Handover issues all four.
- **Hits are deterministic: no dice.** Each weapon has a rate of fire and range bands. Hits come from a fixed rate: at 3 in 4, exactly three shots in four hit. The card shows it as "hits 3 in 4 · down in 5.5 s", and the cursor shows the band.
- **The cost moves from the shot to the victim.**
  - Each person hit costs +2 T, and +2 T more if they go down. Dropping the guard still costs 4 T, as the [B] two shots did.
  - Any gunfire raises Alert to at least 2 (§05's own rule), in place of +1 Alert a shot. The Handover's loud reference keeps T 6, A 2.
  - The final fight stays untraced (Sky's ruling).
- **Controls:**
  - F draws or holsters weapons for the selection, and the cursor becomes a crosshair.
  - Right click a hostile to fire until they go down; hold and sweep to switch target.
  - Left click still moves, and agents keep firing as they walk, at half accuracy.
  - X ceases fire. V cycles the stance: hold, return or at will.
  - Both buttons together is Syndicate's panic button: every armed agent fires at will. It works only once a fight is on.
  - Shooting a civilian or a contact needs Ctrl.
- **Price before commit: one gate.** The first draw or shot that would make a run loud opens a "Going loud" card. It shows Alert to 2 (or 3 with N civilians watching), police from the north street in 12–45 s, the seal time, rivals in 16 s, and that the final fight turns loud: no guard, no warning. Hold for half a second to commit. The game keeps running. After that, only a per-target chip.
- **Hostiles (Pulse).**
  - They advance to cover points worked out at build time, stop to shoot once in range, and pick a target: first whoever is shooting at them, then whoever holds Vale, then the nearest.
  - Rivals break when more than half of them are down. Police hold.
  - On a loud run, the response unit already on the street joins the final fight.
- **Balance** (the calculator below, on the real site positions):
  - The safe fight a prepared cell earns is clean.
  - An unprepared one kills a lone Enforcer.
  - The loud fight kills a lone Enforcer every time. The whole armed cell wins it, often with someone hurt or down.
  - Loud costs 6–10 T against quiet's 3–5.
  - Faster weapons make shooting the patrol the fastest loud route, so I propose one counterweight: Enter takes 30 s longer once Alert ≥ 2, because staff hide Vale. With it, route A is as fast as any loud run. Without it, loud is about 25 s faster and fails T2.

**Questions for Sky (5):**
1. Arm the whole cell? (Yes, limited by Arms.)
2. Run at 2.8 m/s? (Yes.)
3. Run while carrying Vale? (No.)
4. Should the going-loud gate pause the game? (No: hold to commit.)
5. Adopt +30 s Enter at Alert ≥ 2? (Yes.)

Detail, with reasons, at the end.

---

## 1. Running

### 1.1 Rules
| rule | value | tag · reason |
|---|---|---|
| run speed | **280 cm/s** (walk 140) | [S] 2× the walk, so the chip reads "half the time" at a glance. Syndicate Wars runs on double-click (research). It is faster than the response unit's 2.2 m/s [B], and that's fine: the police's threat is their cones, their guns and the seal, never a foot race. |
| stamina | **none** | [S] readable > rich. A bar to manage is one more meter (Syndicate has three, Satellite Reign two), and fatigue is a different lesson. The price is attention, which the city already measures. |
| who notices | everyone who would witness an act: simulated people with a clear line within **13 m** (witness-range-m), the guard's cone, the patrol's cone, live cameras, the response unit, the rivals | [S] no new range. A runner is noticed exactly where an act would be, in 3D (§05 levels). Hitman and MGS V tier noticing by pace (research); we have two paces, so walking is unnoticed and running is noticed. |
| a person who notices | writes a memory entry `saw-runner`: place and time, plus the Fixer's face if the Fixer ran. **No Trail, no standing, no Alert.** | [S] running is not a traceable act (§02 Trail: threats, visible payments, gunfire). It is a sighting, and sightings are leads once a Trail is open (§05 watchers, investigators). It costs later, which fits the pillar *live with it*. |
| security that notices (guard, patrol, live camera, response unit) | the sighting is a **found fault**: Alert rises to at least **1** (inquiry), once per run order. The camera's log entry carries place and time. | [S] §05 Alert 1 [S]: "witness report or found fault, once per incident". Someone running at the Records Office is a fault security checks. Alert 1 doesn't make the final fight loud (§07 needs a shot or A ≥ 2). |
| the guard who saw a runner | holds a memory, so the safe final fight loses him as an ally (the engine's "never saw the cell") | follows from the existing rule, nothing new. The calculator shows what that costs (§2.9): unwarned and with no guard, the lone Enforcer goes down. |
| when running is free | once the final fight has sprung, or the run is loud (A ≥ 2 or a shot fired), or the operative is under fire | [S] in a shoot-out everyone runs; the crowd scatters (§06). Charging attention then would teach nothing. |
| who can't run | the Enforcer holding Vale; anyone carrying a downed operative; the Tech while tethered; anyone doing an action | [S] §03 "Enforcer carries the target". A man being taken walks. It keeps the extraction's 39 s walk, which every fight timing rests on (Q3). |
| running and firing | a running operative doesn't fire. A walking one fires at the moving rate (§2.2). | [S] run or shoot: one decision, readable. |

### 1.2 Price chip
Hovering the ground shows two lines, so the price is on screen before the double-click:
- `walk 0:39`
- `run 0:20 · seen: guard cone, entrance camera (looped), ~6 people · Alert → 1`

The engine needs to provide:
- **`Quote.RunS`**, as `WalkS` exists today.
- **`Quote.RunSeenBy`**: the watcher cones the route crosses at the current tick, and the simulated people within 13 m of the route now, marked "~" because people move.

For Glyph, the route line breaks where it crosses a cone, as a shape cue rather than a colour.

### 1.3 Engine (Keel)
- **Order.** `move` gains `"pace": "walk" | "run"`. A double-click is a `move` with `run`.
- **Sightings.** Each tick, a running operative is tested against the watchers the witness test already uses. A sighting writes the `saw-runner` memory entry; a security sighting raises Alert to 1, once per run.
- **Hash.** The pace is integer state, so the checkpoint hash covers it.
- **Memory.** `memory.acts` gains `saw-runner` (facts: `place-time`, plus faceFacts).

---

## 2. Real-time shoot-outs

### 2.1 Who carries what
- **Any operative can carry a sidearm (pistol). Each one issued uses one point of Arms** (§02 Arms: winning fights, arming joiners). The Enforcer's carbine belongs to the cell and uses none.
- **The Enforcer stays the heavy:** carbine, health 6, and the only one who carries Vale (§03).
- **The Handover** (training, full cell, every verb once) issues three sidearms, so all four are armed.
- **The campaign** starts with the Fixer alone and Arms 0, so he is unarmed until a quartermaster or a hijack supplies Arms (§08 economy).
- **Why the whole cell:** Syndicate's feel is four guns under one commander. Group focus fire and the panic order mean nothing with one gun. And the calculator shows the loud fight kills a lone Enforcer (§2.9), so arming the others is the preparation loud asks for.
- **The cost stays social.** A Fixer who shoots is seen shooting: witnesses hold his face (§03: "the face witnesses remember"), and contacts who see it lose bravery and gain fear and resentment (§06). *People > powers* holds because the gun spends the people you turned.

### 2.2 Weapons and the hit model
| | pistol | carbine | why |
|---|---|---|---|
| damage | 1 | 2 | §06 "pistol 1 dmg, heavier 2–3" |
| time between shots | 1.5 s (15 ticks) | 2.0 s (20 ticks) | [S] Syndicate-fast. The [B] 6 s was a turn-length proxy from the top-down test. |
| point blank | ≤ 5 m: 1000 ‰ | ≤ 5 m: 1000 ‰ | [S] can't miss at arm's length |
| effective | ≤ 15 m: 750 ‰ | ≤ 20 m: 750 ‰ | [S] the Handover's fights stand 12–19 m from the car (site fight points), so a pistol fights at the edge of its band and the carbine inside it |
| long | ≤ 25 m: 400 ‰ | ≤ 30 m: 400 ‰ | [S] beyond the cones' 18–22 m, so you can hit what can't see you, badly |
| reload, ammunition | none | none | [S] Syndicate Wars dropped ammunition for one pool (research); readable > rich. Arms is the campaign's limit. |

**Modifiers** multiply, in per mille:
- the shooter is walking: ×500;
- the target stands still beside cover: ×500;
- the Enforcer is holding Vale and fires one-handed: ×750;
- the shooter is not a trained fighter (Scout, Fixer, Tech, rivals): ×750. The Enforcer, officers and the guard are trained (×1000).

**Deterministic hits: a rate, not a roll.** Each shooter keeps an integer accumulator per target. It starts at 500 when they acquire the target. Each shot adds the accuracy in per mille, and when the total reaches 1000 the shot hits and 1000 is taken off. At 750 that gives exactly three hits in four; at 400, the first shot misses and two in five hit. Nothing is seeded and nothing is random: the same orders give the same hits, and the card can say exactly when the target drops. This is Into the Breach's lesson (research): every loss is the player's own. The accumulators are integers in the checkpoint hash.

**Timing.**
- **Aim:** a shooter whose weapon is drawn fires 1.0 s after acquiring a new target.
- **Draw:** drawing takes 1.0 s; holstering takes 0.5 s.
- **Hostiles:** a hostile who first sees the cell outside a fight reacts in 2.0 s, replacing aim-s 3 [B]. An ambush crew that is lying in wait fires 1.0 s after springing, plus 10 s if Inez warned (§07).

**How hit chance is shown.**
- **The cursor** has a shape per band: three ticks at point blank, two at effective, one at long, and an ✕ when there is no line. Shape, not colour, because the signal colours are reserved.
- **The chip** reads `hits 3 in 4 · down in 5.5 s`, with "~" while either side is moving. With a group selected, it is the group's combined time.
- **Health pips** show over everyone in a fight (§06: bars for all in a fight).

**Time to drop**, one shooter firing at a standing target with no cover, from the order (1.0 s aim included; shots fired in brackets). From `fights.py`:

| weapon | band | shooter | ‰ | civilian (2) | guard, officer, rival (3) | patrol, Scout/Fixer/Tech (4) | Enforcer (6) |
|---|---|---|---|---|---|---|---|
| pistol | point blank | standing | 1000 | 2.5 s (2) | 4.0 s (3) | 5.5 s (4) | 8.5 s (6) |
| pistol | effective | standing | 750 | 2.5 s (2) | 5.5 s (4) | 7.0 s (5) | 11.5 s (8) |
| pistol | effective | walking | 375 | 5.5 s (4) | 10.0 s (7) | 14.5 s (10) | 22.0 s (15) |
| pistol | long | standing | 400 | 5.5 s (4) | 10.0 s (7) | 13.0 s (9) | 20.5 s (14) |
| carbine | point blank | standing | 1000 | 1.0 s (1) | 3.0 s (2) | 3.0 s (2) | 5.0 s (3) |
| carbine | effective | standing | 750 | 1.0 s (1) | 3.0 s (2) | 3.0 s (2) | 7.0 s (4) |
| carbine | effective | walking | 375 | 3.0 s (2) | 7.0 s (4) | 7.0 s (4) | 13.0 s (7) |
| carbine | long | standing | 400 | 3.0 s (2) | 7.0 s (4) | 7.0 s (4) | 13.0 s (7) |

What the table says:
- A trained carbine drops a guard in 3 s. It was 6 s at [B].
- An untrained pistol (×750) takes about twice as long as the trained figures above.
- Fights are short and lethal, as in Desperados and Shadow Tactics, but each fight still has a shape you can read.

**Health:** §06's table, brought forward from [L] to the Handover. Enforcer 6 · Scout, Fixer, Tech 4 · guard and officer 3 · rival 3 · patrol unit 4 · civilian 2. It replaces hits-to-drop 2 [B] and enforcer-health 3 [B]. The response unit becomes 2 officers, so downing it is a fight, not one 2-hit target. An operative at 0 is downed (§06): carry them to the car, or they are captured.

### 2.3 What gunfire costs: per person, not per shot
At 1.5 s a shot, the [B] per-shot prices (+2 T, +1 A) would turn a 10-second exchange into +14 T and a lockdown. So the price moves from the trigger to the victim. Each new rule keeps the Handover's reference numbers:

| | today [B] | proposal [S] | the loud reference (guard dropped, 2 hits) |
|---|---|---|---|
| Trail | +2 a shot | **+2 per person hit (once) and +2 more if they go down** | 4 T today, 4 T proposed |
| Alert | +1 a shot | **any gunfire raises Alert to at least 2**, which is §05's own [B] "any gunfire → 2"; violence seen by a civilian still sets 3 | A 2 today, A 2 proposed |
| witnesses, standing | one incident per fight per place (alert.rule) | unchanged | unchanged |
| response, rivals, patrol | gunfire calls the response unit, draws the rivals in 16 s and breaks the patrol within 30 m | unchanged | unchanged |
| final fight | untraced (Sky, 2026-10-15) | unchanged | unchanged |

This also settles the §05/§06 conflict the rulebook's `alert.rule` records, in §05's favour. The per-shot +1 A only existed because the prototype's shots were 6 s apart.

**A drawn weapon is an act.** Syndicate's police shoot an agent "with a weapon out" (research).
- **Seen by security:** a drawn weapon in the cone of the guard, the patrol, a live camera or the response unit raises Alert to at least 2, and that watcher engages: it reacts in 2.0 s, then fires.
- **Seen by people:** a drawn weapon seen within 13 m by people is one witnessed incident (+1 T, Alert at least 1), and they remember the face.
- **Unseen:** a weapon drawn where nobody sees it costs nothing.

### 2.4 Orders in the engine (Keel)
Everything stays in `suborned.sim.script/1`: one order per operative per input, logged, replayed.

| order | fields | meaning |
|---|---|---|
| `draw`, `holster` | op | 1.0 s / 0.5 s. Drawn is state, hashed. |
| `stance` | op, `stance`: `hold` · `return` · `free` | **hold**: drawn, fires only when ordered. **return** (the default on drawing): fires at whoever fires on this operative. **free**, fire at will: fires at any combatant in reach, nearest first. |
| `fire` | op, `target`: a person id or role, `until: "down"` | Continuous: fires at its cadence until the target is down, the order is replaced, or `cease` arrives. If the line breaks it holds aim for 3 s, then lapses. A `move` doesn't cancel it: the operative fires while walking, at the walking rate. A run does cancel it. |
| `cease` | op | stops firing, stays drawn |
| `panic` | op `cell` | every armed operative draws and goes to `free`. Valid only once a fight is on (someone has fired on the cell, or the final fight has sprung). Otherwise it is refused with "no fight". |
| `carry` | op, `target`: a downed operative | 3 s; then the carrier walks only and doesn't fire. `extract` takes them along. |
| `move` | + `pace` | §1.3 |

- **Combatant** (who `return` and `free` may shoot): a final-fight hostile, or anyone who has fired on the cell or is aiming at it. Never a civilian, a contact or Vale. Never a guard or patrol that hasn't engaged, so a stance can never start a loud run by itself. Only an explicit `fire` does.
- **Group focus fire** is the same `fire` order given to every selected operative on the same tick.
- **Cost:** at most 30 combatants (§06), each with an integer accumulator per target. That's trivial at 10 Hz.
- **`fight-volley-s` (20 s) is retired:** each hostile fires at its own cadence.

### 2.5 Controls under Sky's approved scheme
| input | today (approved) | with shoot-outs |
|---|---|---|
| left click | select; move | unchanged. A move keeps an operative's fire order going (walking rate). |
| double-click ground | run | unchanged; cancels firing for the run |
| right click | the action on what you point at | **with weapons drawn, on a combatant or security:** `fire` until down. On a civilian or contact it shows the chip only: **Ctrl + right click** forces it (the RTS force-attack convention). Vale can't be targeted at all. |
| hold right button and sweep | — | switches the fire order to whichever combatant is under the cursor; release leaves the last order standing. This is Syndicate's "hold to keep firing", without firing at empty ground. |
| **F** | (§10 "F fire") | **draw or holster** the selection. The cursor becomes a crosshair, and a thin effective-range ring shows round each armed, selected operative. F stays "fire", as §10 has it, and sits next to WASD. |
| **X** | — | cease fire |
| **V** | — | cycle stance: hold → return → at will. The portrait shows it as an icon. |
| **left + right together** (within 0.1 s) | — | **panic**: all armed operatives draw and fire at will. Only while a fight is on; otherwise it opens the going-loud gate. |
| 1–4 | verbs | the Enforcer's Fire verb stays: with it selected, a right click fires as above |
| F1–F4, portraits | select an agent | unchanged |
| Space | pause; orders while paused | unchanged: the T1 tactical pause is the real-time player's safety valve |

### 2.6 Price before commit, in real time
**The gate.** The first order that would make the run loud opens a **Going loud** card at the cursor. That order is:
- a `draw` that someone will see;
- a `fire` on anyone before a fight is on;
- a `panic` with no fight on.

The card reads from the engine's quote:
- **Now:** Alert → 2. Alert → 3 if *N* civilians can see it now, with the count.
- **Who comes:**
  - police from the north street in 12 / 18 / 45 s, depending on who reports it;
  - the seal at about *m:ss*, which is 2:25 after they arrive;
  - the rivals in 16 s;
  - the patrol breaks if it is within 30 m.
- **What you lose:** the final fight turns loud: 2–4 officers plus the response unit, no allies, the guard won't help, and Inez's warning is lost. The debrief reads Raised or High.
- **What it costs:** +2 T per person you hit, +2 T more per person you drop.

**Commit** by holding the same input (F, right button, or both buttons) for **0.5 s**; a ring fills. Release early, or press Esc, to cancel. **The game keeps running.**
- **Why hold rather than pause:** Sky's steer is real time. A hold costs half a second and can't be done by accident, and the card is in front of you while you hold.
- **No ambush exception is needed:** the gate never appears once a fight is on. If the rivals spring the ambush, or anyone fires on the cell, you're already in a fight and nothing gates your answer.
- **Space** still pauses, for a player who wants to read it at leisure.

**After the gate:** a compact chip at the crosshair only, for example `guard · hits 3 in 4 · down 3.0 s · +2 T hit · +2 T down · seen by 3`. In the final fight the Trail line reads `untraced`.
- **A civilian or contact:** the chip leads with the cost, in the order it matters: `CIVILIAN · Alert 3 · Menace · Inez lost` (§06).
- **Vale:** shows `mission fails`.

### 2.7 Hostile behaviour for an Act I fight (3–6 hostiles)
**Pulse owns** (fight AI, §14):
- the behaviour table below;
- the cover points;
- the crowd's scatter.

**Motive owns** the numbers. **Keel** implements both in the engine.

| state | enters when | does |
|---|---|---|
| routine | — | its schedule (Pulse's routines) |
| alerted | sees the cell act, a drawn weapon or gunfire within its reach | reacts for 2.0 s; the alert glyph shows over it |
| advance | alerted, with no foe in effective reach | moves to the nearest free **cover point** toward its objective. The objective is the getaway car in the final fight, otherwise the last place it saw the cell. Officers run at 2.2 m/s [B], rivals at 1.6 m/s [B]. |
| engage | a foe inside its weapon's effective reach | stops and fires at its cadence, from cover if it reached any |
| target priority | every shot | 1, whoever is shooting at it; 2, whoever holds Vale; 3, the nearest armed operative; 4, the nearest operative. Never Vale, never a civilian. |
| break (rivals) | more than half the crew is down | runs for the rival car and leaves the fight: down is down, and running away isn't a drop (no Trail) |
| hold (police) | — | police never break. At the seal they hold the cordon. |

- **Cover points are made at build time** (Pulse's generator, per site): spots within 1 m of an occluder 0.9–1.6 m high (planters, kiosks, parked cars, bollard clusters) that block the line from the likely approach. Keel adds `cover` to `handover.site.json`. Vantage's street furniture is what they stand behind. The site's ambush spots today stand "on the open square", so the calculator runs with and without cover.
- **The crowd** (§06 citizen actions): people within 30 m of gunfire scatter off the street, away from the shooter. Running people break sightlines, as crowds already do (§05). A Pulse table.
- **The response unit** already on the street joins a loud final fight: these are the same police.

### 2.8 Feedback (Latch builds; Glyph and Chime own)
**Glyph (look).** Everything here is drawn unlit. Signal colours stay reserved.

| what | spec |
|---|---|
| tracers | one per shot, 0.1 s, from muzzle to the point of impact |
| tracer colour | the cell in a cell colour; hostiles in their faction's signal (security amber, rivals violet). For Glyph to decide: it follows "signals belong to factions", but is a new use. |
| miss | the tracer runs past the target and ends in a puff on the ground or wall, never on the body, so a miss reads at 50 px |
| hit | a flinch and a pip lost |
| downed | the body falls and a down glyph shows. A downed operative's portrait goes grey with a carry hint. |
| health pips | over everyone in a fight; over the selection otherwise (§06) |
| crosshair | band ticks, ✕ when there's no line, and the "down in" time beside it. Drawn weapons show the effective-range ring round each selected operative. |
| stance | an icon on the portrait (hold / return / at will) |
| running | a thin dashed 13 m ring round a running operative while running is conspicuous (Desperados' noise circle, research); it goes once running is free |
| going-loud card | at the cursor, with the commit ring |

**Chime (sound).**
- the draw: a distinct, short sound per weapon;
- shots: one voice per weapon, so you can hear who is firing;
- a miss ricochets; a hit has its own sound; a downed body has a cue;
- police dispatch: a whistle or siren from the north street, so you hear the direction;
- the seal clock: ticks through its last 30 s;
- music: the fight layer on the first shot (§10: fight = full score);
- running footsteps are quicker and lighter;
- contacts' warnings are voiced (Inez's warning).

### 2.9 Balance against the Handover
**Method.**
- `briefs/011/motive-fights/fights.py` models the proposed rules at 10 Hz in integers, on the site's own points:
  - the service lane;
  - the getaway bay;
  - `ambush-1…5`;
  - `police-1…4`;
  - `security-entry`;
  - `guard-ally`.
- Every fight point has a clear line to the car by its own reason text, so the model uses no occluders.
- The Enforcer walks Vale from the service lane (54 m) and stops to fight while a hostile is within 20 m.
- Support operatives stand at the car, beside cover, and focus the hostile nearest the Enforcer.
- **This is a model, not the engine.** Keel's implementation and Suture's runs replace it.

**The final fight under the proposal.** "fight" is timed from the ambush springing (safe) or the police dispatch (loud). "total" is timed from the service lane. The walk alone is 38.7 s.

| set-up | result | fight | total | Enforcer | support | hostiles |
|---|---|---|---|---|---|---|
| **safe A** (5 rivals), Inez warns, guard ally, Enforcer alone | win | 22.9 s | 47.4 s | 6/6 | — | 3 down, 2 ran |
| safe A, warned, guard ally, + Scout, Fixer | win | 14.9 s | 39.4 s | 6/6 | 4/4, 4/4 | 3 down, 2 ran |
| safe A, warned, guard ally, + Scout, Fixer, rivals in cover | win | 20.9 s | 45.4 s | 6/6 | 4/4, 4/4 | 3 down, 2 ran |
| safe A, **no warning, no guard** (he saw a runner), alone | **Enforcer down** | 7.0 s | — | 0/6 | — | 1 down |
| safe A, no warning, no guard, + Scout, Fixer | win | 14.9 s | 39.4 s | 4/6 | 3/4, 4/4 | 3 down, 2 ran |
| **safe B** (3 rivals: driver turned, false tip), no warning, guard ally, alone | win | 20.9 s | 45.4 s | **1/6** | — | 2 down, 1 ran |
| safe B, + Scout, Fixer | win | 12.9 s | 37.4 s | 5/6 | 4/4, 4/4 | 2 down, 1 ran |
| **loud**, 2 officers, *without* the response unit (as the engine counts today), alone | win | 45.2 s | 45.2 s | 2/6 | — | 2 down |
| loud: response unit (2) + 2 officers, alone | **Enforcer down** | 22.8 s | — | 0/6 | — | 1 down |
| loud: response + 3 officers (Alert 3), alone | **Enforcer down** | 22.8 s | — | 0/6 | — | 1 down |
| loud: response + 2 officers, + Scout, Fixer | win | 34.3 s | 34.3 s | 6/6 | 4/4, **1/4** | 4 down |
| loud: response + 3 officers, + Scout, Fixer | win | 42.8 s | 42.8 s | **2/6** | **0/4, 0/4** (both downed) | 5 down |
| loud: response + 4 officers (A3 + a civilian shot), + all three | win | 34.3 s | 34.3 s | 6/6 | 1/4, 3/4, 4/4 | 6 down |
| loud: response + 4 officers in cover, + all three | win | 48.2 s | 48.2 s | 4/6 | **0/4**, 2/4, **0/4** | 6 down |

What the numbers say:
- **Preparation decides the safe fight.** Inez's warning and the guard who never saw you make it clean even for a lone Enforcer. Lose both (run past the guard, or never help Inez) and the lone Enforcer dies. Route B's driver and false tip cut the crew to 3, which a lone Enforcer survives on 1 health.
- **The loud fight needs the whole armed cell.** A lone Enforcer dies every time once the response unit is counted. With the cell, loud wins, usually with an operative hurt or downed. Wounds carry over (§08), and a downed operative must be carried or is captured.
- **Fights are 13–48 s.** §07 says 30–60 s; I propose 15–45 s, since real-time weapons are quicker and the walk bounds the length anyway.

**Trail and Alert by route** (proposal rules; quiet routes as the engine scores them today):

| route | Trail | Alert | debrief |
|---|---|---|---|
| route A (Help, call, loop, café witnesses; + reroute) | 3–5 | 0, or 1 if security saw a runner | Calm |
| route B (Pay or Help on the driver, false tip, witnesses) | 2–4 | 0–1 | Calm |
| loud via reroute (loop 1 + reroute 1 + guard hit 2 + guard down 2) | **6** (today 6) | 2 | Raised |
| loud, shooting the patrol in a sweep gap (loop 1 + guard 4 + patrol 4) | **9** | 2 | Raised |
| either loud run, if people on the square see it | +1 T, and +1 officer | 3 | High |

**Time: the one place faster weapons tip the balance.** These are fastest-play estimates in arithmetic from the probe's walk times, with running on every leg that allows it. They are not engine runs.

**Route A** (Scout runs 24 m, Watch 20, Listen 42; Fixer Help 50):
- the call lands at about 2:02;
- the Enforcer, waiting beside the door, enters in the 1:33–2:07 sweep gap at about 2:03;
- he is out at 2:41 and at the car at about 3:20;
- the safe fight springs on the way, so he is **done by about 3:25.**

**Loud via reroute:**
- the Tech runs to the cabinet (14 s), the loop is on at 1:16, and the reroute is done at 2:01;
- the guard is down at 2:04 and the Enforcer enters at 2:05;
- **done by about 3:22 without the counterweight, or about 3:52 with it.**

**Loud, shooting the patrol:**
- the loop is on at 1:16;
- the patrol and the guard are shot in the 1:33 gap (about 4 s with the cell's guns), and the Enforcer enters at about 1:38;
- **done by about 2:55–3:00 without the counterweight, about 25 s ahead of route A; or about 3:25–3:30 with it, level with route A.**

**The counterweight [S]: alarm inside.** Once Alert ≥ 2, the Office staff hide Vale, and Enter takes **38 + 30 = 68 s**.
- It is §05's "search" made physical in the one building that matters, and it reads in the Enter card as `68 s (alarm inside)`.
- With it, route A is the fastest route or level with loud, and every loud run costs more Trail, Alert, Heat and wounds. That is T2's "informed quiet runs finish faster".
- Without it, loud is the fastest route and the cheapest in time.

**Checked on today's engine.** I replayed three scenarios with `enter-s` at 68 s, shifting every later order by 30 s (`out/motive/fights/var-enter68/`). All three are at Alert ≥ 2 when they enter, so the rule applies to each.
- **S05** (the front route after the guard is shot beside Inez) still wins, 30 s later (5:38.8). The Enforcer leaves on 1 health.
- **N10** (loud, then the final fight) still wins, 30 s later (4:46.5).
- **S11** (the loud front, without the final fight) **now fails, Enforcer down.** The response unit reaches the service lane as he comes out, and he left the guard fight on 2 of today's 3 health.
  - So the rule bites hard on today's [B] health. With the proposal's health 6, and the cell able to cover the exit, it is a fight rather than a loss.
  - S11's scenario intent ("the loud front wins") has to be re-scripted with cover at the exit. Keel and Suture should know before Sky rules.

**What Suture should run once Keel lands it:**
- re-run the frontier (`briefs/011/motive-balance/fast.py`) on the real engine, including the patrol-shooting loud run, which my earlier frontier never tried;
- T2 asks the same question.

A probe on today's engine (`out/motive/fights/probe_loud.py`, a fixed N10 fight tail) found that shooting the patrol and the guard in a gap gets the Enforcer inside, but every such run ended with the Enforcer down on the way out. So today the response unit and police already punish it, and faster cell weapons are what would open it up.

### 2.10 The values, as rulebook entries
Every value is [S] and proposed. **New:**

| id | v | unit | src · reason |
|---|---|---|---|
| op-run-speed-cm-s | 280 | cm-per-s | Sky 2026-10-15 "sprint"; Syndicate Wars double-click run. 2× walk, so the chip reads half the time. |
| run-alert | 1 | alert | §05 Alert 1: found fault, once per incident. A runner seen by security. |
| weapon.pistol | dmg 1 · every 15 ticks · eff 15 m · max 25 m | — | §06 pistol 1 dmg; Syndicate-fast cadence; eff covers the site's 12–19 m fight spots |
| weapon.carbine | dmg 2 · every 20 ticks · eff 20 m · max 30 m | — | §06 heavier 2–3; the Enforcer's |
| point-blank-m | 5 | m | can't miss at arm's length |
| acc-point / acc-eff / acc-long | 1000 / 750 / 400 | per mille | three bands, one cursor tick each |
| mod-moving / mod-cover / mod-vale / mod-untrained | 500 / 500 / 750 / 750 | per mille | walk-and-shoot halves; cover halves; one-handed with Vale; the non-fighters |
| acc-start | 500 | per mille | the accumulator's start: a 50 %+ shot hits first |
| draw-s / holster-s | 1.0 / 0.5 | s | drawing is a visible beat; the going-loud gate sits on it |
| aim-new-s | 1.0 | s | first shot on a new target, weapon drawn |
| react-s | 2.0 | s | replaces aim-s 3 [B]: a hostile's surprise on first sight, outside a fight |
| health.* | enforcer 6 · op 4 · guard 3 · officer 3 · rival 3 · patrol 4 · civilian 2 | hp | §06 health [S][L], brought forward |
| response-unit-officers | 2 | count | the response unit as two officers, so downing it is a fight |
| shot-hit-trail / shot-down-trail | 2 / 2 | trail | per person, once. Keeps the reference: a dropped guard = 4 T = [B] 2 shots × 2. |
| gunfire-alert | 2 | alert | §05 [B] any gunfire → 2. Replaces the per-shot +1. |
| draw-seen-alert | 2 (security) / 1 (people) | alert | Syndicate's police and a visible weapon; a witnessed brandishing is one incident |
| rival-break | > ½ down | rule | rivals are hired; police hold |
| line-lapse-s | 3 | s | a fire order holds aim this long when the line breaks |
| carry-s | 3 | s | lifting a downed operative |
| loud-commit-hold-s | 0.5 | s | the going-loud gate |
| enter-alarm-s | 30 | s | Office staff hide Vale once Alert ≥ 2 (Q5) |

**Retired:**
- shot-every-s 6 [B], fire-reach-m 14 [B] and fire-rivals-reach-m 16 [B]: replaced by the weapon table;
- hits-to-drop 2 [B]: replaced by health;
- enforcer-health 3 [B]: now 6;
- shot-trail per shot and shot-alert per shot [B]: replaced by the per-person costs;
- fight-volley-s 20 [S]: each hostile now fires at its own cadence.

The guard, patrol, security and rivals `fireEvery` [B] values are replaced by each one's weapon.

**Kept:**
- every response delay, the seal at 2:25, the rivals drawn in 16 s, the patrol breaking within 30 m;
- the final-fight counts (safe 5 −1 −1, loud 2 +1 +1), plus the response unit;
- fight-warn-s 10, fight-ambush-m 20, fight-hold-m 25;
- fight.traced off.

**For Keel:** the scenarios that shoot (S03–S09, S11–S19) will change their timings. Their *intent* holds (a far shot leaves Inez unwitnessed, two hits drop the guard, an unseen shot is +45 s), but "2 hits" becomes "the guard's 3 health". Suture re-baselines them.

---

## 3. GDD delta for v0.22.0 (if Sky approves)
- **§02 resources.**
  - Arms: "how many of your people carry a gun this mission (one sidearm per point; the Enforcer's carbine is the cell's)".
  - Trail from: "threats, visible payments, people hit by gunfire".
- **§03 cell.**
  - Enforcer: "Enter, Take, Fire · only op who carries the target; carries the carbine".
  - New line: "Any op can carry a sidearm (Arms) and Fire. Every op can run (double-click): twice the walk, noticed."
  - Scout, Fixer and Tech notes: "fire as untrained".
- **§05 pressure.**
  - Alert 2: "2nd incident or any gunfire" stands, and **per-shot +1 A is removed**.
  - New lines:
    - "running: noticed where an act would be; people remember where and when; security counts it a found fault (A 1)";
    - "a drawn weapon seen by security → A 2 and they engage; seen by people → a witnessed incident";
    - "A ≥ 2: the target's building hides them (Enter +30 s)".
- **§06 fights.**
  - Keep the line "Not a separate combat game", and add "fast and real-time: a fight is short, lethal and decided by what you prepared".
  - Health [S][L] → [S].
  - New weapons table (§2.2).
  - New hit rule: "deterministic: a hit rate, shown as 'down in n s'; no dice".
  - New stances (hold, return, at will) and the panic order.
  - Downed: "carry (3 s) or captured".
  - Enemy behaviour line (advance to cover, engage, priority, rivals break, police hold).
  - Prototype [B]: superseded; keep the line as history.
  - Decisive shot [L]: unchanged.
- **§07 Handover.**
  - "Full cell, all four armed (Arms 3)".
  - Final fight: "15–45 s".
  - Loud: "2–4 officers from the north street, **plus the response unit if it is on the street**".
  - Route timings: shorter walking legs if run (Waymark retimes).
- **§10 controls.** Replace the line with: "left click select / move · double-click run · right click act (weapons drawn: fire until down; hold and sweep to retarget; Ctrl forces a civilian) · F draw / holster · X cease · V stance · both buttons panic (in a fight) · 1–4 verbs · F1–F4 or portrait select · Space pause · Next Event". Plus "readability: tracers, a distinct miss, pips for all in a fight, the going-loud card with a held commit".
- **§13 tests.** T2 is unchanged. New **T8**: "shoot-outs readable? Ask 'why did that operative go down?' Pass: ≥ 5/6 name the cause (outnumbered, no cover, walking, long range)."
- **§14.**
  - Pulse holds: fight AI states, cover points, crowd scatter.
  - Glyph holds: tracers, crosshair bands, going-loud card.
  - Chime holds: per-weapon voices, police dispatch from the north street.

## 4. Questions for Sky
1. **Arm the whole cell, limited by Arms (the Handover issues all four)? Or keep §03's "only the Enforcer shoots"?**
   - **Recommend: the whole cell.** Syndicate is four guns, and focus fire and the panic button need more than one. The loud fight kills a lone Enforcer (§2.9).
   - The Enforcer stays the heavy and the only carrier. A Fixer who shoots is seen shooting, which spends the people he turned.
2. **Run at 2.8 m/s (twice the walk), or 2.2 m/s (the police's running pace)?**
   - **Recommend 2.8.** It reads as a sprint, and the chip says "half the time".
   - 2.2 would match the walk rule's own reasoning ("the cell doesn't read as faster than the city"). But Sky asked for a sprint, and the police's threat is cones and guns, not foot races.
3. **Can the Enforcer run while holding Vale?**
   - **Recommend no.** A man being taken walks.
   - Every fight timing rests on the 39 s extraction walk, and a running extraction would let the Enforcer outpace the ambush trigger and the police.
4. **Should the going-loud card pause the game, or commit by holding for half a second while time runs?**
   - **Recommend hold, no pause.** It is real time, it can't be done by accident, and the card is under the cursor while you hold. Space still pauses.
   - The gate never appears once a fight is on.
5. **Should Enter take 30 s longer once Alert ≥ 2 (staff hide Vale)?**
   - **Recommend yes.** Faster weapons make shooting the patrol the fastest loud route, about 25 s ahead of route A. The extra 30 s puts route A level or ahead, as T2 asks, using a rule the fiction explains.
   - The cost: on today's engine, S11 (the loud front) turns into a loss, because the response unit now meets the Enforcer at the exit. Its script would need the cell covering the exit (§2.9).
   - The alternative is to accept loud as faster but costlier, and T2 fails as written.

## Evidence
```
python3 -I briefs/011/motive-fights/fights.py          # the time-to-drop table and the 14 final-fight set-ups above
OFFICER_WEAPON=carbine python3 -I briefs/011/motive-fights/fights.py   # rejected: police carbines make every loud fight lethal, even for the full cell
dotnet out/sim-unity/tree/sim/cli/bin/Release/net10.0/suborned-sim.dll probe        # walk times and fight-point distances
dotnet …/suborned-sim.dll play N10 | N11 | N11b | S11   # today's fights: loud 2 officers in 18 s, 1 hit; safe 5 rivals in 41 s, 2 hits; safe 3 rivals in 32 s, 2 hits
python3 -I out/motive/fights/probe_loud.py              # today's engine: shooting the patrol in a gap gets in, then the Enforcer goes down on every run tried
# enter-s 68 on a copy of the rules (out/motive/fights/var-enter68), later orders shifted 30 s:
dotnet …/suborned-sim.dll play S05.shift.json   # Success 5:38.8 (was 5:08.8)
dotnet …/suborned-sim.dll play N10.shift.json   # Success 4:46.5 (was 4:16.5)
dotnet …/suborned-sim.dll play S11.shift.json   # Failure, enforcer-down, 3:46.4 (was Success 3:45.2)
```
Two variants were tried and rejected:
- **Rivals breaking one by one at 1 health:** they collapsed in a chain, and every safe fight was clean.
- **Police carbines:** the loud fight killed even the full cell.

## Next owners
- **Aster:** take the five questions to Sky.
- **Keel**, on Sky's yes:
  - the rulebook values (§2.10);
  - the orders (§2.4) and the quote fields (§1.2, §2.6);
  - the per-person Trail and gunfire → A 2;
  - the alarm-inside rule;
  - re-run the 28 and the N-cases.
- **Pulse:**
  - the hostile states and target priority (§2.7);
  - cover points for `handover.site.json`;
  - crowd scatter.
- **Latch:** the controls (§2.5), the gate, tracers and pips.
- **Glyph:**
  - the crosshair bands, the chip, the going-loud card, the stance icons and the running ring;
  - rule on tracer colours.
- **Chime:** the cues in §2.8.
- **Waymark:** retime route A and route B with running.
- **Suture:**
  - re-baseline the scenarios;
  - re-run the frontier, including the patrol-shooting loud run;
  - add T8.
- **Motive:**
  - tune the numbers against Suture's frontier;
  - set the Heat threshold with the §06 fight event (from `motive-balance.md`).
