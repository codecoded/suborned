# 027 · Fight playground: three fight modes on the same fight (spec for Keel and Latch)

**Status (2026-10-20):** spec, ready to build. Author: Motive. It follows Sky's direction at the top of `briefs/027-syndicate-fights.md`:

> Try it in the playground (brief 025 P panel), not on paper. Three switchable fight modes on the same fight; third person stays live real time in every mode; plus a balance test: a run that goes loud from the first second must fail or end with a heavy debrief.

**Scope:**
- Every new rule is **EXPERIMENTAL**, as `cell.drive` is: a playground switch, not a design decision.
- The 15 questions in brief 027 stay open. My recommendations are the switch defaults.
- The GDD (v0.24) is unchanged until Sky has played it.
- No GDD, rulebook or code was edited to write this.

**Owners:**
- Keel: the engine and rulebook entries.
- Latch: host, HUD and the P panel.
- Glyph: reviews the reads.
- Suture: runs the balance test.
- Pulse: owns hostile behaviour; this spec needs none of it changed.

## 0. The one principle: a freeze is host time, never sim time

- **No ticks run during a freeze.** The host stops calling `StepOnce`, as the T1 tactical pause already does. The engine has no "frozen" state, so nothing about a freeze is hashed or logged.
- **Orders are ordinary rulebook orders,** issued with `Game.Issue` at the frozen tick. A queue is host memory until release. On release the host issues every queued order on that same tick, in agent order (scout, fixer, tech, enforcer), then resumes stepping.
- **Replay is unchanged:** the order log (tick, order) plus the start state gives the same run. A session played in freeze mode replays headless with no notion of freezes. The same holds for target mode: its volleys are orders.
- **Driven samples** (brief 025) only flow while ticks run, so a freeze never needs a pose.
- **A scripted run is the perfect-reflex player.** A headless script issues orders with zero reaction time, which is what a player in freeze or target mode can do. So the balance test (§6), run headless, bounds all three modes from above. If a loud run is too cheap for the script, it is too cheap in freeze mode.

## 1. The modes (P panel `fightMode`)

All three modes share the same rules, numbers and fight. They differ only in when the host stops the clock and how orders are entered.

### 1.1 `realtime`: pure real time (Syndicate)
- **What stops:** nothing, by default. The switch `pauseInFight` (default **off** in this mode) decides whether Space pauses while a fight is on. Off makes it pure; on is today's T1 pause.
- **Input:**
  - right click on a target: focus fire for the selection;
  - hold and sweep to move the focus;
  - X to hold fire;
  - V or both buttons for panic;
  - F to draw or holster;
  - left click to move.
- **Seen:**
  - bars over every combatant;
  - the chip at the cursor: pips · −damage · ×hits, plus charge and Trail;
  - rings under the selected drawn agents;
  - the cursor filled, half filled or hollow.
- **Price:** the going-loud card on the first loud order (0.5 s hold, no pause); afterwards the chip.

### 1.2 `freeze`: the plan view stops when a fight starts (Desperados III Showdown)
- **When it freezes (auto triggers):** the first of these that lands this tick. The host reads the engine's log codes, which exist today:

  | trigger | log code | switch (default) |
  |---|---|---|
  | the cell's order would go loud (the going-loud card opens) | host-side, `GoingLoud().Loud` | `freezeOnLoudOrder` **on**. The card shows inside the freeze, and the queue holds the loud order |
  | a hostile spots a drawn weapon | `drawn.seen` | `freezeOnSpotted` **on** |
  | anyone fires on the cell | first `shot.*` whose target is an op | `freezeOnFiredOn` **on** |
  | the final fight springs | `fight.ambush`, `fight.loud` | **on** |
  | reinforcements arrive | `response.reinforces`, a new unit on the street | `freezeOnReinforce` **on** |
  | one of the cell goes down | `<op>.down` | `freezeOnDown` **on** |
  | beat end: every queued action of the last release is done (its target down, its walk ended, or refused) | host-side, from the queue | `freezeOnBeat` **off** (try it on) |
  | manual | the Freeze key (proposed **Space** while a fight is on; it is the pause key outside one) | always |

- **What stops:** ticks. The camera, hover, Alt and the scope stay live.
- **What the player does:**
  - Select an agent (portrait or F1–F4), then click as usual: a target to fire on, ground to move, cover to take, a carry, Board.
  - Each agent holds **one queued action**. A new click replaces it. Shift with the whole cell selected gives the same action to all.
  - **Release:** Enter, or the Go chip on the queue strip. Every queued action is issued on the frozen tick and the clock runs.
  - **Leave the freeze without orders:** Esc, or Space again. Unqueued agents keep their state: drawn at will, holstered, or hold.
- **What is seen during a freeze:**
  - A **queue strip** above the roster: one slot per agent with the queued action's glyph, target and price.
  - **Ghost lines** from each agent to its queued target or destination.
  - **The hostiles' current focus** as a dashed line in their colour (rivals violet, police amber), so you see who is about to be shot. This is engine truth (`Gun.Target`), shown only in a freeze or in target mode.
  - **Cover marks** near selected drawn agents; **rings**; **bars** over everyone.
- **Price per queued slot:**
  - pips ●●●○ · −10 · ×3;
  - charge ▮8 if a charged weapon;
  - Trail: "+2 T" for the first shot at a new target outside the final fight; hit and down Trail with Alt;
  - Alert: lamps to 2, or 3 if a civilian would see;
  - Standing: an arrow by opponent class, "▼1 security", "▲1 feared rivals, if broken", "0";
  - who sees it: the eye, as the move chip shows.

  A walk into range shows its route and move ring.
- **Freeze limits (anti-micro, all switches):**
  - `freezeMinGapS` 3: no auto freeze within 3 s of game time after a release; manual is allowed.
  - `freezesPerFight` **unlimited** by default; a number caps the manual ones.
  - Auto triggers always count as free.
  - A freeze gives no information a pause doesn't, so limits are about pace, not fairness. Fairness is the numbers (§6).

### 1.3 `target`: VATS-style target mode (Fallout)
- **When:** the Target key (proposed **Y**, a free key; Latch confirms), or automatically on the `freeze` triggers marked with `targetAuto` (default: fight springs, fired on, op down).
- **What stops:** ticks, as in freeze.
- **What the player does:**
  - Select an agent, then click a combatant. Each click adds **one shot** to that agent's **volley** on that target, up to `target.maxShots` (6), and up to what the agent's charge pays (§3.3).
  - Right click removes a shot. Several agents can each hold one volley. Clicking a different target with the same agent restarts its volley there.
  - Release with Enter, or with the Target key again.
- **What is seen, per volley at the target:**
  - the pips;
  - **the exact hits:** the accumulator is counted, so with the target where it stands, the HUD can show which of the queued shots will hit (●○●● under the target);
  - the damage they will do, lit as a chunk on its health bar;
  - "down" if the chunk reaches zero.

  This is a forecast "if they stand": the HUD reads the engine's `PreviewVolley`, which changes nothing. The cost in charge shows per shot (▮), and the agent's charge bar drains in preview.
- **Playback:**
  - The volleys run as ordinary fire at the weapon's cadence. Each `volley` order fires exactly its shots at its target, then the agent returns to its state (at will by default).
  - `targetPlayback` (host, default **1.0**, try 0.5) slows the host's tick rate while any volley is live. This is presentation only; the ticks are identical.
- **Price:** as freeze, per volley, plus the total charge spent.
- **Its own limit:** charge is target mode's action points. With `charge` **off**, `target.volleysPerFight` (3) caps volleys per agent per fight instead.

### 1.4 Third person in every mode
- **Always live real time.**
- **Soft lock:** the nearest combatant inside the camera's forward 90° and the weapon's max ring, units included; Tab cycles.
- **Fire:** hold the left button or E. Walking keys move at walking pips.
- **While driving, no auto freeze or auto target fires.** The HUD flashes the trigger: a rim pulse plus the trigger's glyph. The other three keep their state (drawn at will).
- **Leaving the drive (T) while a fight is on** in `freeze` or `target` mode opens a freeze (or target mode) at that tick, counted as auto. The host issues `DriveRelease` first, on the same tick, before any queued order.
- **The Freeze or Target key while driving** leaves the drive first (release), then freezes. There is no freeze while driven.

## 2. The P-panel switches (experimental defaults)
Engine values go in a new rulebook section `fight.lab` (EXPERIMENTAL; read only when `fight.lab.on`). Host values go in `PlayControls` and the Lab, alongside brief 025's.

| switch | default | where | values |
|---|---|---|---|
| `fightMode` | realtime | host | realtime · freeze · target |
| `pauseInFight` | off in realtime, n/a otherwise | host | on · off |
| `freezeOn…` (loud order, spotted, fired on, ambush, reinforce, down, beat) | on, on, on, on, on, on, off | host | each on or off |
| `freezeMinGapS` / `freezesPerFight` | 3 / unlimited | host | s / n |
| `targetAuto`, `targetPlayback` | on (spring, fired on, down), 1.0 | host | set, 0.25–1.0 |
| `fight.lab.on` | on in the playground | rulebook | on · off (off = today's v0.24 rules) |
| `healthScale` | 10 | rulebook | 1 (today) · 10 |
| weapon damage, cadence (ticks at 10 Hz), effective / max m | sidearm 10, 6, 12/20 · carbine 20, 8, 20/30 · machine pistol 6, 3, 10/16 · riot gun 30 within 5 m else 10, 10, 6/10 · long rifle 25, 20, 35/50 (no fire walking) | rulebook | numbers |
| `pips` | on | rulebook | on (four steps 250/500/750/1000 with edges: untrained −1, cover −1, walking −1, high +1) · off (today's multipliers) |
| `draw` / `aimNew` | 5 / 5 ticks | rulebook | ticks |
| `stances` | two (holstered · drawn at will, plus hold) | rulebook | two · three (today) |
| `fire.then` | free | rulebook | free (fight on) · stop (today) |
| `fire.close` | on | rulebook | on (walk into range) · off (refuse) |
| `charge` | on | rulebook | on · off; max 100, regen 1 a tick while no charged weapon fires, machine pistol 4 a shot, carbine 8 a shot |
| `field` | off | rulebook | on · off; 50 charge, absorbs 30 damage, 30 ticks, no fire |
| `target.chargePerShot` | sidearm 10 · carbine 15 · machine pistol 6 · riot 15 · rifle 25 | rulebook | numbers (a targeted shot also pays its weapon's own charge) |
| `target.maxShots` / `target.volleysPerFight` | 6 / 3 (only if charge off) | rulebook | n |
| `policeWarning` | on, 20 ticks | rulebook | on · off |
| `hostilesFireMoving` | on (at walking pips) | rulebook | on · off |
| `loadout` | Handover issue: three sidearms and the carbine | host → start state | per agent: sidearm · machine pistol · riot gun · long rifle, within Arms |

**Two presets** (saved in `briefs/027/presets/`):
- `today.json`: `fight.lab.on` off, `fightMode` realtime. The baseline Sky has played.
- `lab.json`: the defaults above.

## 3. Engine changes (Keel), minimal

### 3.1 `fight.lab` (EXPERIMENTAL) and its reads
- **The section:** `fight.lab` holds the values in §2's rulebook rows. Every value is `"proposed": true`, its reason "EXPERIMENTAL: brief 027 playground", and its src is this file.
- **Health:** `healthScale` multiplies every `weapons.health` at start.
- **Accuracy:** `Rules.Accuracy` gets a pips branch when `pips` is on. Range band 4/3/2, then edges as steps, clamped 1–4, mapped to the step table.
  - The **high** edge: the shooter's level index is greater than the target's.
  - `mod-vale` is not read.
- **Weapons:** the weapon table gains `machine`, `riot` (two damage bands) and `rifle` (`noFireMoving`). `Gun.W` comes from the loadout in the start state. Arms is checked against `arms` costs; unknown = reject.

### 3.2 Orders
- **`fire`** gains:
  - `then` (`free` · `stop`), default from `fire.then`;
  - `close`: when out of range or with no line, the agent walks the grid to the nearest free point inside its effective ring with a clear line (search on the grid, ties by id), then fires.

  The fire order then neither lapses nor is refused for range.
- **`stance`** with `stances: two`: `free` (drawn) and `hold`. `return` is rejected (unknown = reject).
- **`panic`** with the lab on: allowed outside a fight. The host gates it with the going-loud card.
- **`volley`** {op, target, shots}: new.
  - It fires exactly `shots` shots at the target at the weapon's cadence, then reverts to the stance.
  - It pays `target.chargePerShot` per shot when issued, or counts one of `volleysPerFight` with charge off. It is refused with `no-charge` if unpaid.
  - It ends early if the target is down; the unspent charge is refunded.
- **`field`** {op}: new; only with `field` on.

### 3.3 State and reads
- **`Op.Charge`:** an integer, hashed; regen and costs as §2. The read is `Game.ChargeOf(op)`.
- **`Game.PreviewVolley(op, target, shots)`:** read only. It returns per shot hit or miss and the damage, from the shooter's current accumulator on that target (or acc.start if new), the current pips, and the target's health. The host's forecast uses it.
- **`Game.PreviewFire(op, target)`:** read only. It returns pips, damage, hits to down, charge per shot, and the walk if `close`. The chip uses it in place of `DownTicks`.
- **`Gun.Target` of hostiles** is already public; the freeze shows it.

### 3.4 Not needed
- No engine freeze.
- No new log codes for the freeze: the host reads the existing `drawn.seen`, `fight.ambush`, `fight.loud`, `response.reinforces`, `<op>.down` and `shot.*`.

## 4. Host and HUD changes (Latch, with Glyph), in build order: smallest playable first

1. **B0 · Freeze on today's rules (host only).**
   - `fightMode` freeze with the auto triggers and the manual key.
   - The queue strip (one action per agent), release on the frozen tick, ghost lines, the hostiles' focus lines.
   - It uses today's orders and the today preset.
   - Done when Sky can play the Handover's loud fight frozen, and the saved session replays headless.
2. **B1 · Damage and health.**
   - Keel: `fight.lab` with `healthScale`, damage, cadence, `pips`, `fire.then`, `stances: two`, and `PreviewFire`.
   - Latch: segmented health bars over every combatant and on the portraits; the chip as pips · −damage · ×hits (no seconds); the damage-preview chunk; X as hold; V as panic.
   - Done when the lab preset plays in realtime and in freeze.
3. **B2 · Target mode.**
   - Keel: `volley`, `PreviewVolley`, and `Op.Charge` (needed as AP; with `charge` off, `volleysPerFight`).
   - Latch: the target key, per-shot forecast under the target, the charge bar on the portrait, `targetPlayback`.
4. **B3 · Range and reach.**
   - Keel: `fire.close`.
   - Latch: rings, the filled cursor, the walk-into-range route; the drive soft lock to max ring, including units; a right click on a response robot or officer fires (today it only labels, `HandoverHost.cs` 626).
5. **B4 · Charged weapons and loadout.**
   - Keel: charge costs for the machine pistol and carbine; the new weapons; the `loadout` start state.
   - Latch: the loadout row in the briefing (P panel first).
6. **B5 · City reads.**
   - Keel: `policeWarning` and `hostilesFireMoving`.
   - Latch: the warning "!" and ring; cover marks with the move snap (Keel: snap on a move end within 150 cm, EXPERIMENTAL `fight.lab.coverSnap`).
   - Forma: flinch and fall. Chime: the stub sounds (one report per weapon, a hit, a ricochet, a scream, a warning).
7. **B6 · The field** (switch, off by default).

Through every step:
- `game:check`, the Unity match and `bun run verify` pass.
- A driven or frozen session saved in the game replays headless.
- Glyph reviews B1 and B2 shots.
- Frame time at play zoom on the Air is reported.

## 5. What each mode must show, side by side (Sky's comparison)
- **Same fixture:** the Handover's two fights from the same start, saved as two checkpoints:
  - **CP-LOUD:** route A, then the Enforcer fires on the guard at 2:36, as N10.
  - **CP-SAFE:** route A quiet, at Vale's exit before the ambush, as N11.
- The P panel switches `fightMode` and the preset and reloads the checkpoint.
- Each try writes its outcome line, for Sky's notes and Suture's table:
  - result;
  - the cell's health left;
  - downs on each side;
  - Trail, Alert and Standing;
  - the number of freezes or volleys;
  - real seconds taken (wall clock, not game).

## 6. The balance test (Suture runs it; Keel adds the scenarios)
**The aim (Sky):** you can't shoot your way through cheaply. Loud may win, but it must cost. A prepared plan must clearly beat it.

### 6.1 The runs
All runs are headless scripts with zero reaction time, the perfect player (§0). Each is run under two presets, `today` and `lab`; `lab` also runs with charge off and with the field on.

| id | script (loud from 0:01) |
|---|---|
| **L1 front** | 0:01: all four draw (panic). The Enforcer walks to the Records Office door and fires on the guard; the others focus the patrol and anyone who engages. Once the guard is down: Enter (68 s, alarm inside), out by the service door with Vale, to the car, Board everyone standing, Go when free |
| **L2 pickup** | 0:01: all draw, walk to the pickup, fight whatever comes (patrol, response), focus fire, then panic. At 6:10, the Take at the official car, to the getaway car, Board, Go |
| **L3 guard shot** | Route G's timing, but the guard is shot, not knocked out (the Enforcer at the door at his mark), then Enter |
| **L4 everyone** | 0:01: panic, then focus each new combatant in turn, nearest first, until nothing engages. Then the shortest route to Vale that is still open |
| **P-A** (prepared) | Route A Help, reference (§07): watch, Help Inez, loop the camera, call, Enter, the safe fight fought out with focus fire, Board, Go |
| **P-G** (prepared) | Route G-fast (§07), safe fight fought out |
| **P-B** (prepared) | Route B, driver paid, rivals tipped (3 rivals) |

### 6.2 What a loud run must end in
Each L run must end in **fail** or a **heavy debrief**:
- **Fail:** any of seal, Enforcer down, Vale dead, rivals reach Vale.
- **Heavy debrief:** success, with **all** of:
  - Trail ≥ 12 (the reference loud run is 13);
  - Alert ≥ 2 at the end;
  - Standing at least 2 below the start (+2 → 0 or lower);
  - public security High;
  - investigation active;
  - **and at least one** of: an op down or captured; the cell's total health lost ≥ 40 %; an open case.

### 6.3 What the prepared runs must do
P-A, P-G and P-B succeed with:
- Trail ≤ 7;
- Alert ≤ 1;
- Standing ≥ the start;
- no op down;
- the cell's health lost ≤ 25 % in the safe fight.

### 6.4 The numbers are wrong (retune before Sky judges the modes) if any of these hold
1. Any L run succeeds with **no op hit at all**, or with the cell's health lost < 25 %.
2. Any L run succeeds with Trail **< 2×** the best prepared run's Trail.
3. Any L run is **the fastest route** by more than 15 s of game clock against P-G (G-fast 3:36).
4. Two or more of the L runs succeed with Standing ≥ the start.
5. Any prepared run loses its safe fight, or downs an op. The numbers are too hard: preparation must be rewarded.
6. Under `lab`, an L run that fails under `today` now succeeds, with no added cost on 6.2's list. The new numbers made loud cheaper.
7. **Lethality ceiling** (Syndicate Wars' warning: "riddled with bullets by the time you get a grasp on the situation"): in the lab, an op at full health under focus from two hostiles at effective range goes down in under **2.5 s of game clock**. This is internal tuning, never shown to the player. It is too fast to read or answer, even with `freezeOnFiredOn`.

### 6.5 Output
- Keel adds the runs as engine scenarios (N-ids), each asserting 6.2 or 6.3, plus a matrix CLI, `bun run sim fight-lab`, printing one row per run × preset:
  - result;
  - Trail, Alert and Standing;
  - downs and health lost;
  - game clock at the end.
- Suture reports the matrix with any 6.4 trigger, and the numbers go back to Motive.

## 7. Open, decided by playing
All 15 questions of brief 027. Mostly:
- Q1, damage and health;
- Q4, pips;
- Q6, lethality;
- Q12 and Q13, charge and field;
- which `fightMode` Sky keeps (a new question, Q16): realtime, freeze or target, or a mix, such as freeze on the map with target on a key.
