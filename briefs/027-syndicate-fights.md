# 027 · Fights like Syndicate

**Status (2026-10-20, rev. 2):** a proposal for Sky to discuss. Author: Motive (systems). Queued after brief 026 (render quality).

> **Sky, 2026-10-20:** "For the fighting, you think the real time makes sense over strategy? I did initially think can do both, so third person is real time but plan view is strategy … Or like fallout, can target someone instead of realtime … we need to ensure if realtime the balance of play makes it fair so it's not super easy to shoot your way through it." Agreed next step ("Yea"): try it in the playground (brief 025 P panel), not on paper. Three switchable fight modes on the same fight: (1) pure real time; (2) the plan view freezes when a fight starts (Showdown-style: queue one action per agent, release together); (3) VATS-style target mode (time stops, pick a target, pips and a charge cost per shot). Third person stays live real time in every mode. Plus a balance test: a run that goes loud from the first second must fail or end with a heavy debrief. The 15 questions stay open; the playground uses Motive's recommendations as switchable defaults, marked experimental, and the GDD is not edited until Sky has played it.

**Asked by Sky:**
- 2026-10-20: "The fire fights right now is very bad. Need to make that like syndicate."
- Brief 025: "The shooting was always going to be more Syndicate simplicity rather than accuracy."
- 2026-10-19: "hit chances, not aiming" and "not Ghost Recon".

**Rev. 2 follows Sky's note on rev. 1** (2026-10-20): "Did syndicate have energy levels? Wouldn't be a time concern, would be proper fighting." So:
- A fight now reads as **damage and health**, never as time. "Down in n s" is gone from the chip and the price. Seconds stay only as internal tuning in `ttk.ts`.
- Syndicate Wars-style **energy** and an IPA-style **Adrenaline** lever are added as options.

**Nothing is edited:** the GDD, the rulebook, the engine and the host are untouched. Every item is a proposal (P1 to P13 and options A to C) with the question Sky has to answer.

**Evidence:**
- Research with sources: `briefs/027/research/syndicate-fights.md`. It covers the 1993 manual, fan weapon tables, Syndicate Wars controls and energy.
- Calculator: `bun briefs/027/ttk.ts`. It uses the engine's counting rule and prints pips, damage and hits to down. Its seconds are internal.
- Engine runs: `bun run sim play N10` and `N11`, 2026-10-20.

## Summary

**What is wrong today:**

1. **Too many inputs, and the cell stops after every kill.**
   - The inputs: draw (F), a right click per target, V cycling three stances, X to cease, Ctrl to force, hold-and-sweep, both buttons for panic, and a 0.5 s hold to go loud.
   - A fire order ends when its target drops. The default stance ("return") answers only whoever shot that agent, so after each kill the agent stands idle until you click again.
2. **The fight reads as a timer, not a fight.**
   - The chip says "hits 3 in 4 · down in 14 s".
   - Health is a row of 2 to 6 small pips, and each hit removes one or two.
   - Damage hardly differs: pistol 1, carbine 2.
   - You can't see a hit hurt, or how much the next one will.
3. **Hit chances you can't read.** Four multipliers stack (range band, walking, cover, untrained), so the log shows 750, 562, 300, 281 and 150 ‰. Cover points are invisible.
4. **Slow, silent and thin.**
   - One shot every 1.5 to 2 s, drawn as a 2 px tracer.
   - No audio anywhere in `unity/Assets`: no AudioSource and no AudioClip.
   - The downed swap to a lying pose; there is no fall and no flinch.
5. **Range is a refusal.** Firing out of range is refused, and an order lapses 3 s after its line breaks.
6. **Holes in the controls.**
   - A right click on a response robot or officer (brief 013 units) shows only a label (`HandoverHost.cs` 626).
   - Take-control's E reaches 8 m and skips units (`HostDrive.cs` 280). Fights stand 10 to 19 m off.
7. **No loadout.** There are two guns, and Arms only counts carriers.

**What Syndicate had** (research note §"Health, energy and drugs"):
- **Syndicate (1993):** one **health bar** per agent. "When the Health Bar is completely black, the Cyborg is dead". It is restored by a single-use **Medikit**. The manual names no regeneration.
- **1993 durability:** **chest and heart mods** add it ("survive direct hits", "strength and durability"). A short-lived **Energy Shield** "can withstand all projectile weapons".
- **1993 IPA bars:** they set speed, reactions, accuracy and autonomy, and the boosts wear off into dependency.
- **Syndicate Wars:** health, a **shield on top of health**, and one **weapon energy pool** instead of ammunition, all recharging over time. Drugs (Red Mist, Blue Funk) slow healing while they are in an agent.
- **So yes: Syndicate had energy levels.** The 1993 game had health plus drug levels; Wars had health, shield and energy.

**The proposal in brief:**
- **Fights read as damage and health:**
  - Every combatant shows a segmented **health bar**.
  - Each weapon has its own **damage per hit**. Health is scaled ×10 so damage can differ (a sidearm takes 10 of a rival's 30).
  - Hovering a target lights the chunk one hit would take off its bar.
  - The chip shows **pips (the chance) · damage per hit · hits to down**.
  - Hits are still counted, never rolled.
- **Drawn means fighting.** A click focuses the selection, and they fight on when the target drops.
- **Hit chance is four pips** from range and visible edges: untrained, cover, walking, high ground.
- **Weapons are chosen in the briefing within Arms.** They trade damage, range and rate of fire.
- **Range shows on the cursor and the ground**, and an order out of range walks into range.
- **The city reacts out loud:** a police warning, crowds that scream and scatter, a sound for every weapon, a ricochet on a miss.
- **Option A, a charge** (Syndicate Wars energy, recommended in a small form). Each agent carries one charge bar. It powers the heavy weapons and a short field, and recharges while those weapons rest. The sidearm never uses it.
- **Option B, Adrenaline** (IPA, recommended no). Option C is a drug-free stand-in, recommended later if at all.
- **Everything else stands:** Trail per person hit, Alert ≥ 2 on gunfire, one Standing move per public fight, the untraced final fight, deterministic replay, the content line, and rules only from Keel's rulebook.

**Questions for Sky (15):** in §6.

---

## 1. What we have now (read from the code and the runs)

| area | today | where |
|---|---|---|
| orders | draw/holster (F), fire until down (right click on a target), cease (X), stance hold/return/at will (V), panic (both buttons, only once a fight is on), Ctrl forces a civilian, hold-and-sweep retargets | `HostFight.cs`; `GameCombat.cs` Draw, Fire, Stance, Panic |
| after a kill | `fire.done`: the order ends; stance "return" (the default on draw) fires only at whoever shot this agent, so the agent stops | `GameCombat.cs` CellShooter, StanceTarget, StartDraw |
| health, damage | health civilian 2, guard/officer/rival 3, patrol 4, Scout/Fixer/Tech 4, Enforcer 6; pistol 1, carbine 2; pips over fighters | rulebook `weapons.health`; `HostFight.Fighters` |
| hit chance | band 1000/750/400 ‰ × walking 500 × cover 500 (partial 750) × untrained 750 × Vale 750, counted with an accumulator from 500 | `Rules.Accuracy`; rulebook `weapons.acc`, `weapons.mod` |
| the chip | "hits 3 in 4 · down in n s · +2 T", for the first selected agent only | `HostFight.Aim` |
| cadence | pistol every 1.5 s, carbine 2.0 s; first shot 1.0 s after taking a target; draw 1.0 s | rulebook values |
| range | pistol eff 15 / max 25 m, carbine 20 / 30; out of range refused; a broken line lapses after 3 s | `GameCombat.Fire`, `line-lapse-s` |
| cover | invisible build-time cover points; a still target beside one is ×500 (full) or ×750 (partial) | `Cover.cs`, rulebook `ai.cover` |
| hostiles | advance to cover, fire nothing while moving; police hold; rivals break at more than half down | rulebook `ai.rule` |
| feedback | a 2 px tracer with a muzzle star, a pulse on a hit, small health pips; no sound, fall or flinch | `HostFight.Shot`, `HudToolkit.Less.cs` 1217, `HostViews.Figures.cs` 126–133 |
| take control | E acts on the focus within 8 m; units skipped | `HostDrive.cs` 270–312 |
| healing | none in a mission; wounds carry over and a turned doctor heals between missions (§06, §08) | GDD |

**Runs (2026-10-20):**
- **N10 (loud):** 34 shots and 19 hits. The Fixer hit 3 of 10 at 150–300 ‰.
- **N11 (safe):** every new target needed a fresh fire order. A player gives those by hand.

## 2. What Syndicate does that makes its fights good

Sources are in `briefs/027/research/syndicate-fights.md`.

1. **Few verbs, all on the mouse** (manual pp. 38–39):
   - select a weapon (draw, and police notice);
   - right click to fire, or hold it to keep firing;
   - both buttons for panic;
   - holster to hide again.
2. **Range is one colour.** "When this is red the target is within range."
3. **Health is a bar you watch.** "When there's more black than white showing it's time to take evasive action." Survival is prepared: chest and heart mods, the Medikit, the Energy Shield.
4. **Autonomy and edge are prepared, not commanded.** The IPA bars set reactions, accuracy and speed before the fight ("They fire to defend themselves while awaiting the next command"), and are paid for later in dependency.
5. **Weapons differ in damage, rate and range,** chosen within a budget, and in Wars they are fed by an energy pool. Fights are fast because they were prepared.
6. **The city has rules you can hear.**
   - Police hold fire until a weapon is drawn, then shout "Police! Put down your weapons!"
   - Rival agents fire before you've armed.
   - Civilians panic.
7. **The ceiling (Syndicate Wars):** "riddled with bullets by the time you get a grasp on the situation." Keep the numbers small and the reads big.

**Not copied:**
- cyborgs;
- the Persuadertron as a gun (turning people is the Fixer's verb, and Hero crowds already join fights);
- area weapons in crowded streets;
- routine stray-fire civilian deaths (Q9).

## 3. The proposal

Every item is **[proposed]**. It keeps these pillars:
- commander > avatar: no aiming;
- preparation > improvisation: loadout, place, cover and timing decide the fight;
- readable > rich: bars, pips, rings and sounds, never sentences;
- every order shows its price: $, Trail, Alert, who sees it, and the walk for a move;
- public fights move Standing once, by opponent;
- deterministic replay; Keel's rulebook only.

### P1 · Fights read as damage and health
- **Health bars, not pips.** Every combatant in a fight gets a segmented bar over the head: the cell, hostiles, an engaged guard, patrol or officer, and the ally guard.
  - Each segment is 10 health. The cell's bars also sit on their portraits.
  - Glyph's two-icon budget counts the bar as one icon.
- **Health ×10, the same proportions as today:**

  | who | health |
  |---|---|
  | civilian | 20 |
  | guard, officer, rival | 30 |
  | patrol, Scout, Fixer, Tech | 40 |
  | Enforcer | 60 |

  Integers throughout, so the counting stays exact.
- **Damage per hit by weapon** (P5). A hit knocks its damage off the bar with a flash of the lost segments and a flinch. A miss shows a spark on stone and is heard as a ricochet.
- **The damage preview.** Hovering a target with a drawn selection lights, on the target's bar, the chunk the selected weapons' next hit would take.
- **The chip** shows three things and no time:
  - the **pips**: the chance, ●●●○ = three hits in four;
  - the **damage per hit** ("−10");
  - **hits to down** ("×3").

  With several agents selected it shows the best one, and each portrait shows its own pips. Alt adds the Trail line.
- **Seconds are internal.** Cadence and fight-length targets live in the rulebook and `ttk.ts` for tuning, and never reach the player.
- **Q1.**

### P2 · Two states: holstered or drawn (retires the V stances)
- **Holstered:** never fires. This is the covert state; drawing is the act the city sees (§05).
- **Drawn:** fires at will at **combatants** in range, nearest first. A combatant is defined as today: a final-fight hostile, or anyone who has fired on or aims at the cell; never a civilian, a contact, Vale, or an unengaged guard or patrol. So drawing can't start a loud run; only an explicit fire order can.
- **Hold fire (X):** stays drawn but doesn't shoot. A slashed sidearm shows on the portrait. Any fire order clears it.
- **Q2.**

### P3 · Focus fire that fights on, and panic any time
- **Right click on a target:** every selected armed agent focuses it until it is down, then returns to at will. The engine's `fire` gains `then: "free"`.
- **Hold and sweep** moves the focus, as today.
- **Moving while drawn** keeps the agent firing, one pip worse.
- **Panic** (both buttons, or V, freed by P2) draws every armed agent and sets at will, at any time. Outside a fight it opens the going-loud card when it would be seen.
- **Q3.**

### P4 · Hit chance is four pips
Hits are still counted by the accumulator: same orders, same hits. All values are per mille.

```
pips = range band          4 point blank (≤ 5 m) · 3 inside the weapon's effective ring · 2 out to its max ring
     − 1 untrained shooter (Scout, Fixer, Tech, rivals), beyond point blank
     − 1 target beside cover that shields it from the shooter, beyond point blank
     − 1 shooter walking
     + 1 shooter on a higher level than the target (deck over street, concourse over deck)
clamped 1–4   →   1 = 250 · 2 = 500 · 3 = 750 · 4 = 1000
```

- **Every edge is visible and can be prepared:** train, climb, flank, stop.
- **Retires** `mod-vale`. The Enforcer can't fire while walking Vale (brief 018), so it is never read.
- **Q4.**

### P5 · Weapons, chosen before the mission
Arms becomes a loadout budget, set in the briefing. The Handover keeps today's issue (three sidearms and the carbine), so its reference results stay comparable.

| weapon [proposed] | Arms | damage per hit | rate | effective ring | max ring | for |
|---|---|---|---|---|---|---|
| sidearm | 1 | 10 | medium | 12 m | 20 m | anyone; the default |
| machine pistol | 2 | 6 | very fast | 10 m | 16 m | close and loud (Syndicate's Uzi) |
| carbine | the cell's | 20 | medium | 20 m | 30 m | the Enforcer, as today |
| riot gun | 2 | 30 within 5 m, else 10 | slow | 6 m | 10 m | holding a door or the car |
| long rifle | 2 | 25 | very slow | 35 m | 50 m | overwatch from the deck; can't fire walking |

- **Rate** is a word for the player and a cadence for the engine: medium 0.6 s, very fast 0.25 s, slow 1.0 s, very slow 2.0 s; the carbine 0.8 s.
- **Draw** 0.5 s. **First shot on a new target** 0.5 s.

**What the chip would say** (from `ttk.ts`; health ×10):

| case | sidearm | machine pistol | carbine |
|---|---|---|---|
| rival in the open, 12 m (30) | ●●●○ −10 ×3 | ●●○○ −6 ×5 | ●●●○ −20 ×2 |
| rival beside cover, 15 m, untrained | ●○○○ −10 ×3 | ●○○○ −6 ×5 | ●○○○ −20 ×2 |
| same, from the deck | ●○○○ | ●○○○ | ●●○○ |
| an op (40) under a trained officer, 12 m | ●●●○ −10 ×4 | | |
| the Enforcer (60) under the same | ●●●○ −10 ×6 | | |

- **Internal tuning:** a duel at effective range lasts 1 to 3 s, against 5 to 13 s today. Shooting in a fight lasts 10 to 25 s, and the whole final fight 30 to 60 s (Sky, 2026-10-17). It is lethal both ways, as Syndicate is.
- **Act III [L]:** a belt-fed squad gun, and smoke that blocks sight like a crowd.
- **Q5, Q6.**

### P6 · Range you can see, and walking into range
- **Rings.** A selected drawn agent shows its effective and max rings on the ground.
- **The target cursor** is filled when every selected agent can fire, half filled when some can, and hollow when none can (Syndicate's red cursor).
- **Out of range.** A fire order out of range walks to the nearest point inside the effective ring with a clear line, then fires. The walk shows as any move does: route and move ring.
- **Lost line.** The focus holds when the line breaks, and the agent closes in again.
- **Q7.**

### P7 · Cover you can see and take
- **Marks.** Build-time cover points show as half-shield marks within 10 m of a selected drawn agent.
- **Snap.** A move that ends within 1.5 m of a free point snaps to it, facing the nearest combatant.
- **Effect.** Cover is −1 pip only inside its arc (`Cover.cs` already has the arc), so flanking beats it. That is tactics by position.

### P8 · Selection and group fire
- **Stays:** F1–F4 and the portraits select, and Shift adds.
- **One tick.** A group order is one order per agent on one tick, ties by id.
- **No group-mode toggle.** Syndicate's group icon "reduces the strategy".

### P9 · Take control: fighting from the ground
- **Soft lock.** The nearest combatant within the camera's forward 90° and inside the weapon's max ring, units included. Tab cycles.
- **Fire.** Hold the left button or E to fire on it. Walking keys move at walking pips; running never fires.
- **The same reads:** bars, pips, damage preview, rings.
- **The other three** keep their own state.
- **Replay holds:** it is the same `fire` order, and the engine reads the pose (brief 025).
- **Q8.**

### P10 · The city in a fight
- **Police warning.**
  - An officer, guard or response unit who sees a drawn weapon, with no shot fired yet in the incident, shouts and gives 2.0 s.
  - Holster in time: Alert 1 (a found fault), the face is remembered, no engagement.
  - Stay drawn: Alert 2, and he fires.
  - Rivals never warn.
  - **Q10.**
- **Crowds:** the crowd rule stands (startle, flee to a refuge, return after the shooting stops), now seen and heard: crouch or flinch, run, screams within 20 m. Scattering people still block watchers' lines.
- **Witnesses** are unchanged: one incident per target, Trail per person hit, violence seen by a civilian sets Alert 3, one Standing move per public fight.
- **Stray shots:** still none (`crowd.rule`). **Q9.**
- **Hostiles** (Pulse owns the behaviour): they fire while walking, at walking pips; rivals rush to cover inside their effective ring and flank one point at a time; police hold. **Q11.**

### P11 · The price
- **Before going loud:** the going-loud card stays the one gate, committed by a 0.5 s hold, with no pause.
- **Trail:** +2 T once per shooting, +2 T per person hit, +2 T more when they go down.
- **Alert:** any gunfire raises it to at least 2; violence seen by a civilian sets 3.
- **The final fight** is untraced. **Standing** moves once per public fight, by opponent.
- **The chip** shows a shot's price as pips, damage, hits to down and Trail, with no time.
- **A walk into range** is priced as any move.
- **A loadout's Arms** shows in the briefing.

### P12 · HUD (Glyph) and audio (Chime)

**HUD:**

| read | drawn as |
|---|---|
| health | segmented bar over every combatant and on each portrait; lost segments flash |
| chance, damage | four pips · "−10" · "×3" at the cursor; the preview chunk lit on the target's bar |
| range | rings on the ground; the cursor filled, half filled or hollow |
| focus | a thin dotted line from each firing agent to its focus |
| a shot | the tracer (exists), a muzzle flash, a spark on stone for a miss |
| down | a fall (Forma); the body stays |
| cover | half-shield marks near drawn agents |
| warning | an amber "!" and a 2 s ring that empties |
| charge (option A) | a second, thinner bar under health on each portrait, in the cell's turquoise; the field shows as a shimmer round the agent |

**Audio** (no audio exists yet). Every sound is a build-time clip.

| cue | sound |
|---|---|
| each weapon | its own report: sidearm pop, machine-pistol rattle, carbine crack, riot-gun boom, rifle snap |
| hit | a thud and a grunt, distinct when it is one of the cell |
| miss | a ricochet |
| down | a body fall |
| police | the warning shout |
| crowd | screams and running feet |
| fight on / over | a danger layer while a fight is on, a stinger when it is over |
| response | sirens that rise as the response nears |
| charge (option A) | a hum that climbs as it fills; a dry click when it runs out |

### P13 · Retire the decisive shot
§06 "decisive shot [L]" is one aimed shot in slow time. That is aiming. **Folded into Q4.**

## 4. The energy and Adrenaline options

### Option A · The charge (Syndicate Wars energy, small)
- **What it is:**
  - Each agent carries one **charge** bar of 100, an integer.
  - It feeds the **charged weapons**: the machine pistol (4 per shot) and the carbine (8 per shot).
  - It also feeds a **field**: 50 charge; for 3 s it absorbs up to 30 damage before health; the agent can't fire while it holds.
  - The **sidearm, riot gun and rifle never draw charge.**
  - The bar refills by 10 a second while no charged weapon fires and no field holds.
  - A full bar is about 25 machine-pistol shots (4 rival downs at 3 pips) or 12 carbine shots (6 downs).
- **Why it fits:**
  - Atomic power made energy cheap (§01), so an atomic cell in the grip is 1950s style with centuries-ahead technology.
  - The cell are "visitors from a nearby timeline" (§10, §15), so their kit may outclass the city's.
  - It gives a fight a rhythm you play by position: burst, then let one agent recharge while another holds. That is the "proper fighting" Sky described, and it is not a timer.
  - It replaces ammunition without bringing back reloads.
- **What it costs:**
  - one more bar per agent;
  - the field is a power, which tests people > powers.
  - Hostiles carry no charge (sidearms, and the response's carbines at a cadence), so the bar is the cell's alone.
- **Determinism:** integer charge per tick, counted like hits.
- **Price:** the chip adds the charge per shot ("▮4"). The field is an order (one key) with its cost on the plate.
- **Recommendation:** **yes for the charged weapons; the field as a separate yes or no** (Q12, Q13). No health regeneration in a mission: wounds carry over and the turned doctor heals (§06, §08), which is our Medikit.

### Option B · Adrenaline (IPA, per agent)
- **What it would be:**
  - A lever per agent, or a one-shot order, for 20 s: +1 pip, faster reactions (draw and first shot 0.25 s), walk 1.8 m/s.
  - Then a **crash** for 40 s: −1 pip, walk only.
  - Repeated use in a mission lengthens the crash, as Syndicate's dependency did.
- **Its price:**
  - the boost and the crash both show on the order plate before you commit;
  - an agent on a boost is noticed like a runner (a lead, A ≥ 1 if security sees it).
  - Campaign option: each use adds +1 fear, for the Talker test, to the agent who took it.
- **Against it:**
  - It is a power, not a person (people > powers, the second tiebreaker).
  - It is something you work on the avatar mid-fight (commander > avatar).
  - Its whole price is time and pips. A drug in the cell's kit also fights the look: sharp, clean visitors, not cyborgs.
  - The GDD has no rule against drugs. 1950s pep pills fit the era, but not the cell.
- **Recommendation: no** (Q14).

### Option C · A drug-free stand-in for IPA
- **What it is:** Syndicate's IPA really sets how much an agent does by himself and how well. We cover the autonomy part with P2's holstered / drawn / hold. The "edge" part could come from **people**:
  - **Steady:** a turned former soldier or a quartermaster's training gives +1 pip to one agent for the mission, bought between missions.
  - **Armour:** a quartermaster's vest gives +10 health, already in §06 "armour via quartermaster".
- **Fit:** preparation, people, and nothing to manage mid-fight.
- **Recommendation:** campaign [L], not the Handover (Q15).

## 5. What would change

### GDD §06 (v0.25, once Sky decides)
- **health:** ×10; bars, not pips.
- **weapons:** P5's table with damage per hit and Arms costs; the loadout in the briefing.
- **modifiers:** P4's pips and edges; `mod-vale` gone.
- **hits:** keep the accumulator; the chip shows pips, damage and hits to down; no "down in n s".
- **orders:**
  - P2's states;
  - P3's focus that fights on, and panic any time;
  - P6's walk into range;
  - P7's cover snap.
- **hostiles:** fire while walking; rivals flank.
- **Options A, B or C** as decided.
- **Remove:** the decisive shot.
- **Also:**
  - §05: the police warning.
  - §10: V moves from stance to panic, X moves from cease to hold fire, drive-mode soft lock, the HUD reads.
  - §03: "fires as untrained" stays.

### Rulebook (Keel; unknown = reject)
- **`weapons`:**
  - `health` ×10;
  - each weapon's `damage`, `every`, `eff`, `max`, `arms`, and `charge` (option A);
  - `acc.pips`, and `edges` as pip steps;
  - drop `mod.*`;
  - `draw` 5 ticks, `aimNew` 5 ticks.
- **`weapons.orders`:**
  - `fire.then` and `fire.close`;
  - `stance` hold/free;
  - `panic` through the card;
  - `cover` snap.
- **`cell.charge` (A):** `max`, `regen`, `field.cost`, `field.absorb`, `field.s`.
- **`security.warn-s`.**
- **`ai`:** fire while moving; flank steps.
- **The HUD** stops reading `DownTicks` for the chip. It stays as an internal check.
- **Tests:**
  - T2 and T3: quiet still beats loud on Trail, Alert and Standing.
  - T8 is re-read.
  - New scenarios: a pip ladder; damage and hits-to-down per weapon; fighting on after a kill; walking into range; a warning backed off; a high-ground shot; charge use and refill (A); a driven fight replayed.
  - Suture measures the internal fight lengths.

## 6. Handoff
- **Inputs:** this brief, the research note, `ttk.ts`.
- **Done-check:** Sky answers Q1 to Q15. Then:
  - Aster versions the GDD;
  - Motive writes the rulebook values;
  - Keel builds them, and the scenarios pass;
  - Pulse handles the hostiles;
  - Latch builds the controls and the soft lock;
  - Glyph, Forma and Chime handle the reads, the animations and the sounds;
  - Suture measures.
- **Next owner:** Aster, to put the questions to Sky.

## 7. Questions for Sky

**The shape:**
1. **Damage and health, not time** (P1). Health ×10 shown as segmented bars over every combatant, damage per hit by weapon, and the chip showing pips · damage · hits to down? *Recommend yes.*
2. **Drawn means at will** (P2). Retire the three stances for holstered, drawn and hold fire? *Recommend yes.*
3. **Focus fire that fights on, and panic any time** through the going-loud card (P3)? *Recommend yes.*
4. **Four pips** with a high-ground step (P4)? And strike the decisive shot (P13)? *Recommend yes to both.*

**Weapons and reads:**

5. **Weapons and loadout** (P5). Machine pistol, riot gun and long rifle at Arms 2 each, chosen in the briefing? *Recommend yes.*
6. **Lethal both ways.** About 3 hits drop a rival or an officer, and about 4 drop one of the Scout, Fixer or Tech. *Recommend yes.*
7. **Out of range, walk into range** instead of the refusal (P6)? *Recommend walk.*
8. **Take control:** soft lock ahead, hold to fire (P9)? *Recommend yes.*

**The city:**

9. **No stray shots** (P10)? *Recommend keep: civilians would otherwise be collateral in every fight.*
10. **Police warning** with a 2 s holster window (P10)? *Recommend yes.*
11. **Hostiles fire while moving, and rivals flank** (P10)? *Recommend yes.*

**Energy and Adrenaline:**

12. **Option A, the charge for heavy weapons** (one recharging bar per agent; the sidearm free)? *Recommend yes.*
13. **Option A, the field** (50 charge, absorbs 30 damage for 3 s, no firing)? *Recommend not for the Handover. Try it in the playground first.*
14. **Option B, Adrenaline** (+1 pip and faster for 20 s, then a priced crash)? *Recommend no: a power, not people, and not the cell's look.*
15. **Option C, edge from people** (a trainer's +1 pip, the quartermaster's armour) as the campaign's IPA? *Recommend yes, for the campaign [L].*
16. **Which fight mode, or which mix?** Real time, the plan-view freeze (Showdown), or VATS-style target mode, with third person live in all (briefs/027/playground.md). *Decide after playing the playground.*
