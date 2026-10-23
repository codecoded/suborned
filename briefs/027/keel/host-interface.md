# Brief 027 · The fight lab: host interface (Keel → Latch)

Engine `suborned.sim/0.10.0`. Rulebook `fight.lab` (EXPERIMENTAL). Code: `sim/Suborned.Sim/GameFightLab.cs`.

**Backwards compatible.** Nothing in the 0.9.0 API changed signature, and with the lab off every run plays and hashes as before. Your current `HostFight` keeps working on the today preset. With the lab on, its chip goes wrong, because `R.Accuracy`, `w.Damage` and `DownTicks` are today's rules: switch the chip to `PreviewFire`.

## Turning the lab on: the preset goes in the start state

```csharp
var s = new Start { FightLabOn = true };          // or Start.Parse(presetJson["start"])
s.LabCharge = false; s.LabField = true;           // any switch; null = the rulebook's fight.lab default
s.LabLoadout[(int)OpId.Scout] = "machine";        // sidearm | carbine | machine | riot | rifle | none
var g = Engine.New(inputs, s);                    // a loadout over Arms or an unknown weapon throws FormatException: show it in the P panel
```

- **Where the presets live:** `briefs/027/presets/{today,lab}.json`. Each has a `start` part (the engine's) and a `host` part (yours: fightMode, pauseInFight, freezeOn…, freezeMinGapS, freezesPerFight, targetAuto, targetPlayback).
- **The today preset** is `fightLab.on: false`. It adds no bytes to the start state.
- **Recorded sessions.** `start.fightLab` is written by `Script.Write`, and `Engine.Replay` replays it like any other start key.

## Reloading a checkpoint (§5)

- **The files:** `briefs/027/checkpoints/cp-loud.script.json` (2:36.5, the tick of the Enforcer's fire order on the guard; that order is the script's last) and `cp-safe.script.json` (5:53.8, the Enforcer out with Vale, before the ambush).
- **To load one:**
  1. Parse the checkpoint.
  2. Set its `Start`'s fight-lab fields from the chosen preset (the checkpoint itself carries none).
  3. Play its orders to `until` (`Engine.Play(inputs, start, orders, until)`), then hand the game to the player.
- **The checkpoint's `expect` record** holds for the today preset only. With the lab on, the start hash differs, so don't verify `expect` then.

## Freeze and target mode: host time only

- **No engine freeze.** To freeze, stop calling `StepOnce`. Queued actions are host memory.
- **On release:**
  - Issue every queued order with `Game.Issue` at the frozen tick, in agent order: scout, fixer, tech, enforcer.
  - If an agent was driven, issue `DriveRelease` for it first, on the same tick.
  - Then resume stepping.
- **Replay.** The order log replays the session headless with no notion of the freeze.
- **The triggers come from codes that already exist:** `drawn.seen`, `shot.*` whose target is an op, `fight.ambush`, `fight.loud`, `response.reinforces`, and `<op>.down`. Read them from `Game.Log` after each step.
  - With the police warning on, the "spotted" moment is when `Game.Warnings` gains an entry. `drawn.seen` follows only if the agent stays drawn.

## New orders

```csharp
// target mode: one volley per agent, issued on release
Game.Issue(new Order { Tick = g.Tick, Op = "scout", Do = "volley", Target = "rival-2", Shots = 3 });
// refused: no-charge (the bar can't pay shots × the weapon's target charge; charge off: no volleys left this fight; shots outside 1..target.maxShots),
//          plus fire's refusals (no-weapon, fire-running, fire-carrying, fire-carrying-vale, no-target, fire-on-cell, civilian-target, fight-over, out-of-range / no-clear-line only if no walk into range exists)
Game.Issue(new Order { Tick = g.Tick, Op = "enforcer", Do = "field" });          // field switch on; refused no-charge short of field.cost; off: unknown-verb
Game.Issue(new Order { Tick = g.Tick, Op = "scout", Do = "fire", Target = "officer-1", Then = "stop" }); // optional per-order then; "" = the rulebook's (free)
Game.Issue(new Order { Tick = g.Tick, Op = "cell", Do = "panic" });              // with the lab: allowed outside a fight; gate it with GoingLoud(order)
```

- **Stance.** With `stances: two`, only `free` and `hold` are accepted; `return` is refused with `stance-unknown`.
  - X → `stance hold`. It also ends the agent's fire order and refunds any volley.
  - A draw is at will.
  - Any fire order clears hold.
- **Walking into range (`close`).** A fire order out of range or with no clear line is taken. The engine walks the agent and sets `Op.CloseWalk`.
  - Draw the walk as a move: `op.Walk` is the route.
  - A click move takes the agent back.
  - A driven agent is never walked: its order holds until it is in range.

## Reads (read-only: nothing changes, nothing is hashed)

```csharp
FirePreview p = g.PreviewFire(OpId.Scout, "rival-2", force: false);
// p.Refused  "" or a refusal code (see above; agent-down too)
// p.Pips 1..4 (0: no line, out of range, or pips off) · p.Acc ‰ · p.Damage · p.Health/p.Max · p.HitsToDown   → the chip: ●●●○ −10 ×3
// p.ShotsToDown (by the accumulator from this agent's aim now; -1 can't hit) — internal, not for the chip
// p.ChargePerShot (weapon) · p.TargetChargePerShot (target mode) · p.Charge/p.ChargeMax
// p.Band point|eff|long|out · p.DistanceCm · p.Line · edges p.Moving p.Cover p.Untrained p.High   → why the pips are what they are (Alt)
// p.Walk, p.WalkTo, p.WalkS   → close: the walk into range (route seconds at a walk)
// p.TrailShooting, p.TrailHit, p.TrailDown (0 once paid), p.Untraced (final fight)

VolleyPreview v = g.PreviewVolley(OpId.Scout, "rival-2", 3);
// v.Shots[i] = (hit, damage, healthAfter) — the ●○●● under the target; exact while target and shooter stand where they are
// v.Down, v.HealthAfter (the lit chunk), v.ChargeCost, v.ChargeLeft (drain the bar in preview), v.VolleysLeft (charge off), v.Refused

int c = g.ChargeOf(OpId.Scout);                    // the portrait's charge bar; max = g.Labr.ChargeMax
var (ticks, absorb) = g.FieldOf(OpId.Enforcer);    // the field ring
List<string> ids = g.Combatants();                 // whom the health bars go over (plus the cell); the hostiles' focus is GunOf(id).Target
IReadOnlyList<LabWarning> w = g.Warnings;          // the police "!" and its ring: Watcher, Op, Until (tick), DrawTick
bool lab = g.Lab; int scale = g.HealthScale;       // bars: GunOf(id).Health / .Max (×10 with the lab)
```

- **Bars.** `Gun.Health` and `Gun.Max` hold integer health for everyone. One segment is 10.
- **Ranges.** The rings use `op.Gun.W.EffCm` and `MaxCm`.
- **Weapons.** `Gun.W.Id` is `sidearm`, `carbine`, `machine`, `riot`, `rifle` with the lab on, and `pistol` or `carbine` with it off.
- **Draw and aim delays.** Use `g.DrawT` and `g.AimNewT` (the lab's, or else today's), not `R.DrawTicks` and `R.AimNewTicks`.
- **The hostiles' focus line** (shown in a freeze or in target mode): `g.GunOf(hostileId).Target`.

## What you need for B0–B2

- **B0, freeze on today's rules.** Nothing new from the engine: freeze = stop stepping; release = `Issue` at the frozen tick. Prove it by saving a frozen session and checking that `bun run sim play <session.json>` says "the replay matches its record".
- **B1, damage and health.**
  - Load the lab preset into the start state.
  - The bars: `Health`, `Max` and `Combatants()`.
  - The chip: `PreviewFire` (pips · −damage · ×hits, no seconds).
  - The damage chunk: `PreviewFire.Damage` against the bar.
  - X sends `stance hold`; V sends `panic`.
- **B2, target mode.**
  - `PreviewVolley` for the forecast; the `volley` order on release.
  - `ChargeOf` for the bar.
  - `targetPlayback` is host-only (the tick rate): the ticks are identical.
- **Before any Unity match:** run `bun tools/sim/unity.ts` (it copies the 0.10.0 DLL and the new rulebook into the project). I have not run it, because it writes into `unity/`.
- **Known gaps for B3 and later:**
  - The drive soft lock and units as targets: `PreviewFire` already accepts unit ids (`officer-*`, `robot-*`, unit ids).
  - The cover snap is not built.
  - The high edge never fires until the site has levels.

## Update 2026-10-20: target mode out of range (Latch's B2 question)

**Decision (Keel):**
- A volley on a target that is out of range, or with no clear line, **walks in first and holds its shots until it stands inside the effective ring with a line**. It is the same walk as `fire.close`.
- The forecast is **fired from that stop point**:
  - `PreviewVolley` and `PreviewFire` walk the route tick by tick, at a walk, exactly as the engine walks it.
  - The stop is the first tick's position inside the ring with a line.
  - Pips, chance, band, edges, damage and the ●○●● are all read from there, standing.
- It is deterministic and read-only, and stays "if they stand". Scenario **N149** proves it: a 3-shot volley at 65 m forecast ●○● from the stop, landed ●○●, and the Scout stood exactly on the forecast's `StopAt` with no shot on the way.
- Focus fire (`fire`) and at-will fire still shoot on the way, at walking pips. Only a volley holds its shots.

**New fields** (read-only, on both `FirePreview` and `VolleyPreview`):

```csharp
v.Walk            // true: it walks first; the shots are from StopAt
v.StopAt          // where it will stand (cm): draw the route's end marker here, not WalkTo
v.WalkTicks       // ticks of walking until it stands there (the first shot comes after, at the weapon's draw/aimNew)
v.WalkExposedTo   // combatants (ids, ordinal order) whose effective ring and line reach some tick of that walk as they stand now: the danger
// FirePreview also keeps WalkTo (the grid point the walk heads for) and WalkS (route seconds to WalkTo); prefer StopAt and WalkTicks
```

**What Latch changes in the HUD:**
1. **Under the target:** when `v.Walk`, keep the ●○●● forecast (it is now real), and prefix it with a walk glyph and `WalkTicks` as seconds (`🚶 4 s · ●○●`). Don't grey it out.
2. **On the ground:** draw the walk route (the move ring style) to `v.StopAt`, with a ghost of the agent standing there. In a freeze, the ghost line goes to `StopAt`.
3. **Danger:** for each id in `v.WalkExposedTo`, draw that combatant's warning tint, or a small "exposed" mark on the route. Put the count on the queue strip slot ("⚠2").
4. **The fire chip** (`PreviewFire`) can show the same thing when `p.Walk`: walk seconds, then the pips from the stop.
5. **Nothing to re-sync** beyond the DLL and inputs you already copy with `bun tools/sim/unity.ts`. The `volley` order is unchanged.

## PreviewAnswer (brief 027 F2, Pulse kite-response.md §3; engine 0.10.0, lab kite on)
`Game.PreviewAnswer(OpId op, string target, Pt? from = null)` → `KiteAnswer? {Side: "police"|"rivals", Point, Ticks, Cover}`. Read-only. What `target` would do if `op` fired on it now (or from `from`, e.g. a walk-in's stop point): police walk to `Point` (inside their ring of the agent) and are there in `Ticks`; rivals go to `Point` (a cover point when `Cover`, else a blind point). Null when the target would answer from where it stands (the agent is inside its ring and seen, or another drawn agent is), it is not an armed hostile of a side (the guard, the patrol, civilians, brief-013 units), its chase budget is spent, or the kite is off. Glyph draws the ghost ring; no text.

## Engine 0.15.0: fire on the move, anyone can be shot (tuning.md §8, §9; both presets)
**Orders (what the host sends does not change; what the engine does with it does):**
- `fire` to a walking agent stops the walk (in reach) or replaces it with the walk-in (out of reach). To walk and shoot, send `fire`, then the `move`.
- `fire` to a running agent never stops the run; out of reach, the walk-in starts when the run ends.
- `move` (walk or run) or the first `drive` sample while a fire order stands keeps the order (no `fire.cancelled`) and sets `Op.CloseSpent`: no walk-in for that order; out of reach after the move it lapses `weapons.lineLapse` later. A new `fire` resets it.
- A driven agent fires at each tick's gait (`DriveSample` with `run`, or the same point for "no key held" = standing). The driven walk-in (`PreviewFireWalkIn`) is for both presets now (HostFight.cs:186 is lab-only today: Latch).
- Refusal codes no longer given: `fire-running`, `fight-over` (still in `Answer.Codes` until Fable drops their lines). `fire-on-cell` is the one target refusal.
- New target ids: `camera:<id>` (any site camera; `Game.CameraDark(id)`), a brief-013 unit by its own id (`police-1`, `robot-1`, …: send the clicked unit's id, not `security`), Vale (`councillor`) and every person with `force: true`, a knocked-out or restrained body by the person's id or role (a hit kills).

**FirePreview (new fields):**
```
p.Gait            // "stand" | "walk" | "run" | "run-away": the gait the chance is read at now (a walk-in: "stand" at StopAt)
p.TargetRunning   // the target runs this tick (×weapons.mod.targetRunning past point blank)
p.HasEnd, p.EndAt // the agent is moving and its move ends inside the weapon's max ring with a line: the arrival point
p.AccEnd, p.PipsEnd, p.ShotsToDownEnd // the first standing shot from EndAt at the target where it stands now: the "now → on arrival" arrow
// p.Pips is 0 both out of range and for the lab's new 0-pip rung (a runner): read p.Line / p.Acc (lab-pip-0 = 100 ‰) to tell them apart
```
The chip updates each tick while driven (the gait and heading change). `p.TrailHit` and `p.TrailDown` are 0 on a camera.

**LoudCard (GoingLoud):** `c.Fails` ("vale-dead"; "driver-shot" while Vale is out; else "") and `c.Hold` (= `Loud || Fails != ""`): hold `c.HoldTicks` (5) for any order with `Hold`, loud or not. Fable's line: "Vale dies. The mission fails."

**Log codes:** `fire.camera-dark`, `fire.camera-hit` (silent under `fire.` until Fable cards "The camera goes dark."). A person shot dead logs `civilian.down` or `guard.down` as before and leaves a brief-013 body (`Bodies`, act `How == "shot"`).

## Engine 0.19.0: weapons free and hostile intent per agent (tuning.md §14; hostile-initiative.md, Sky's answers; both presets)
**Orders:**
- `{"do":"free","op":"cell","on":true|false}`: the G toggle. Logged on its tick; any other op is refused `free-not-cell`. The start can set `weaponsFree` (default on: `weapons.free.start`). Off, a drawn agent returns fire exactly as 0.18.0.
- `{"do":"fire","op":<agent>,"target":<hostile>,"focus":true}`: the focus click on a hostile in a fight, one order per selected agent in the same tick. It is a lock: lasting until the target is down, its walk-in spent (it never walks in), drawn if holstered. The first shot of a fight is still the hold: send a plain `fire` (or the trigger) for that, and `focus` only once `FreeFightOn`.
- `cease` (X) on an agent under weapons free stands it down: it picks again only once shot at after that, given a fire order or a trigger, or when the fight goes off.
- The engine derives every weapons-free pick. Never send fire orders for them.

**Reads (read-only, not hashed):**
```
g.WeaponsFree                 // the G setting now
g.FreeFightOn, g.FreeFightSince // a fight is on (contact, weapons.free.settle 3 s after the last contact or shot); since which tick (-1 none)
g.FireStateOf(id).Mode        // new value "weapons-free": the agent fires on its own pick (Target); "return"/"free" as before when WF is off or no fight
g.StandDownOf(id)             // the tick X stood it down, -1 none: draw the agent's "holding" mark
g.UnderFireOf(id)             // List<UnderFireBy {By, Since, Last, Aiming}>: every hostile whose last shot at this agent landed within lineLapse (3 s), in id order.
                              // Sky's answer 1: no auto-draw; the portrait and the world show "being shot at" from this (Since = when the spell began)
g.KnownFaces                  // agents whose face security knows (they are fired on by police from Alert 2, by robots at wanted/infamous Heat at any Alert)
g.RivalsSawAct                // agents the rival crew saw act (drawn, firing, takedown, carry)
g.RivalsStreetWatching(p)     // the crew's cone from its arrival (3:10); SeesNow("rivals", p) and ConeFan("rivals") now show it from arrival too
g.Warnings                    // the drop-it warnings now open, in today too (police at Alert 0-1, a drawn agent with no target): the "!" ring
```
**Log codes (silent under `fire.` and `ai.kite`):** `fire.free` (a pick), `fire.free-setting`, `fire.free-on`, `fire.free-off`, `fire.focus`, `ai.kite` "sees the X at n m and closes in" and "The rivals see Vale taken and press for the Enforcer". `patrol.breaks` also when the patrol knows a face.

## Engine 0.20.0: the final fight per agent, PROVISIONAL (tuning.md §15.2; Sky 2026-10-22 "I think so - sounds right", to confirm after play)
**No new field, order, refusal or log code.** The engine reads `ai.intent.final` (`agent` now; `every` was 0.19.0). What changes on screen:
- In the final fight a hostile fires on an agent only for that agent's own causes, as before it: police and robots on a drawn weapon in their sight, an agent who has fired, the Vale holder, seen violence, or the agent's own known face (`g.KnownFaces`); the rivals on drawn, Vale or what they saw that agent do (`g.RivalsSawAct`). A holstered agent never seen acting can stand in an officer's ring and draw no fire.
- So `g.UnderFireOf(id)` and the portrait's under-fire cue stay empty for that agent through the final fight, and no hostile's `Target` is it. The host should not assume "final fight = everyone is a target" in any hint or line it draws.
- The fight range is unchanged (`every`: it has no history to read); range hashes are identical.
- Read `g.R.Intent.FinalEvery` (false under the shipped rulebook) only if a debug overlay needs to show which rule is live.

## Engine 0.21.0: the lasting fire order on the move (Sky 2026-10-22, playing 0.2.48; brief 041's last line; both presets)
Sky: "Keep them firing until out of range I would say, same for running." A lasting fire order (the right hold let go, a focus, a portrait lock, a follow lock) ends when its target is down, when the target leaves the weapon's max ring, on cease (X or the plate's ×), or on a new act.

**What the engine did at 0.20.0, and what changed:**
- A walk or a run given while a fire order stands: kept the order and fired on the move at the gait's chance (0.15.0, tuning.md §8.2–8.3). **Unchanged.** Nothing to send: a click walk or run never needs a cease, and the host must not send one.
- The target out of reach: at 0.20.0 the order never ended for range. Standing with its walk-in unspent, it walked back in after the target (a chase); after a player move it held while moving and lapsed `lineLapse` 3 s after the move ended; a driven agent held while driven. **Now:** once the order has had its target in reach (the weapon's max ring, `g.W.MaxCm`) and line on any tick, the target beyond the max ring ends it that tick, whoever moved (log `fire.out-of-range`). No walk back in, no lapse.
- An order given out of reach or line still walks in first (brief 030) and still lapses after a player move that ends out of reach (F8-O6): the new rule starts only once the order has had reach and line.
- Line lost inside the max ring (a pillar, a car): as before (walk-in if unspent, else the 3 s lapse).
- A new act (any order that engages the agent: watch, listen, turn, call, loop, reroute, mislead, takedown, carry, drop, stash, restrain, enter, take, board): at 0.20.0 it paused the order through the act and the agent fired again after it. **Now** the act's own order ends it (log `fire.cancelled`, "<agent> stops firing on <target> for the <verb>").

**Host (Latch):**
1. Keep `EndLasting(k)` at an act's click (the cease before the walk to the act). The walk to an act is a plain `move`, which keeps the order, so without the cease the agent fires on its way to the act. The engine's own end at the act is the backstop for scripts.
2. Fix the comment on `EndLasting` (HostHold.cs:384): "it lapses out of reach" is now "it ends out of range".
3. On a `fire.out-of-range` line (`Game.Log`, code `fire.out-of-range`; the text starts "<agent>: <target> is out of range (<m> m)", agent = `Op.Name`): `LocksDrop(new[] { k })` and show the pop `less.pop["fire-out-of-range"]` (PLACEHOLDER "Out of range", Fable to word) on that agent's plate or pill. The order is already gone (`Gun.Order == ""`), so `FireStatus` stops naming it on the next frame either way; the pop says why.
4. Read `g.Ops[k].Gun.Reached` (true while the order has had reach and line) only if the fire line or the reticle should tell "about to end" from "still walking in". Nothing else is new: no order, no refusal code, no field the host must send.
5. Weapons free: when the order ends out of range in a fight, the agent may pick its own target at once (`fire.free`), as after any order ends. That is the engine's pick, not the host's.

**Port:** the engine dll and the rulebook (rule text on `weapons.rule`) go together: every inputs hash moves. DP1 (`Fixtures/drive-session.script.json`) is re-recorded (orders identical).

## Engine 0.22.0: only security notices a run (Sky 2026-10-22; both presets)
Sky: "I think running only in front of camera, patrol bots and guards get you noticed, anyone else fine, need to loosen that up." Police officers count as security (Sky via Aster, "yep").

**What changed in the engine:**
- A run is remembered (`saw-runner`: place, time and the Fixer's face) only by security staff on the street within witness range on a clear line: the door guard and anyone whose job is in `witness.staff` (guard, patrol-officer, watcher, investigator). Passers-by, café and Office staff who are not security, Inez, the driver and Vale no longer notice a run. The rival crew never noticed one and still doesn't.
- The found fault: a live camera, the door guard (by his cone), the patrol, the response's robots or police that see the run raise Alert to `cell.runAlert` (1), once a run, log `run.seen`. This is as before.
- New: a crowd guard (a security-staff person on the street, not turned) also raises it when the run is in his facing cone with the near sense, on a clear line. At a stop he has only the near sense. The ledger cause is `runner-seen:<person id>:<agent>`. The log text is "The guard <name> sees the <agent> running: a found fault. Alert n." `HostFeed`'s `run.seen` parser reads it as "The guard <name> saw the <Agent> run."
- `MoveQuote.RunSeenBy` lists such a guard by person id when the route passes him now. `MoveQuote.Seen` (the card's sight, rulebook sight.unknown) doesn't include crowd guards: they are people walking the street, not watchers with cones. The chip's Alert can therefore read lower than the run's when a guard is on the route. Glyph and Latch decide whether to mark it, for example with a guard marker over the person.
- Takedown awareness: a running Enforcer within witness range is heard only by a security-staff target. Sight by the facing cone and a wary guard are unchanged.
- `MoveQuote.RunPeople` now counts security staff only (Unity doesn't read it). `RunSeenBy`, `AlertIfRun`, `Seen` and the `run.seen` log are unchanged.

**Host (Latch):**
1. Nothing to send and no new field or code. The run chip and the running agent's eye (`RunEye`, `Seen`) read the security watchers only; see the crowd guard note above.
2. The Run verb's line in `text.json` (`actions.run.does`) now says "Only guards, police, patrols and cameras notice." This is a placeholder for Fable to word.

**Port:** ship the engine dll and the rulebook together (rule text on `cell.rule` and the takedown preview line), because every inputs hash moves. DP1 (`Fixtures/drive-session.script.json`) is re-recorded with identical orders. No route outcome moves. The route-a family reaches Alert 1 at 0:05.8 by a crowd guard instead of 2:52.7 (see `hash-moves-0.22.json`).

## Engine 0.23.0: a person act out of reach walks in (Sky 2026-10-22: "I can't listen to Inez a lot of the time with the Scout - I think we shouldn't block actions whilst Inez is moving about")
Rulebook `cell.approach` (`walkIn: true`, `repath` = `approach-repath-s` 1 s). Both presets; the range has no person acts and is unchanged.

**What changed in the engine:**
- Watch on a person, Listen, and Ask/Pay/Help/Pressure given out of reach (or a person's Watch with no clear line) are **taken**, no longer refused `too-far` / `no-clear-line`. The order is checked as if in reach first, so the real refusals stay: `person-away`, `person-down`, `needs-*`, `already-heard`, `already-watched`, `refusal-remembered`, `no-cash`, `driver-too-late` (now counting the walk), `only-scout`, `run-tethered`, `agent-busy`. No route to her: `no-walk`.
- The agent walks the grid at a walk toward where she will be when he gets there (a server's round is read ahead; anyone else where they stand), re-aimed every second and when the walk ends. The act starts on the first tick in reach (a Watch also needs its line), checked again as an order given there. From then it is the act as before: its own time, Inez held where she stands (`PersonPose` phase `held`), the Fixer pays at the start. Nothing is paid or held during the walk.
- Refused at the start (she went off the street or down, another agent's Watch already taught it, cash spent): dropped. `g.Dropped` gains `(tick, op, order, code, why)` and the log says `order.dropped`. The code is an existing refusal code, so its `locks` line in text.json is the pop.
- The walk in ends on any other order to that agent (a move, another act, fire, drive; log `order.walk-in-ends`) and on cancel (`cancel.move`). A stance, draw, holster or cease leaves it standing.
- A **driven** agent is never walked: out of reach is still `too-far` (cell.drive: acts while driven are taken where the agent stands).
- `too-far` for person acts now comes only from a driven agent. Cameras, the patrol, containers, takedowns, stashes and doors keep it.

**Reads (read-only, not hashed):**
```
g.WalkInS(agent, verb, role)  // whole seconds the agent walks before the act starts: 0 in reach, -1 none (off the street, no route, or driven and out of reach).
                              // verb: watch, listen, ask, pay, help, pressure. Best estimate from her known round (the intercept). The card's time = this + Quote.S
g.ActivityOf(id)              // while walking in: Phase "walking", Verb the act's ("watch", "listen", "turn"), Target the role, OnDone "in reach, the <act> starts", cancel "stop"
g.Ops[k].Act                  // the act walking in (null none); op.Walk is its route (draw it as any walk)
g.Dropped                     // acts dropped on the way or at the start
```
`Preview(approach, role).WalkS` is unchanged (the same idea, for the approaches).

**Host (Latch):**
1. Remove the host's own serving lock in `HandoverHost.Locked` (the `ServingBackS(o.Target) > 0` line → `too-far`) and the HUD's "back in n s" price swap in `HudToolkit.Less.cs` (~line 653). Give the order: the engine walks the agent in.
2. Your click-to-act walk for person acts (a host-side walk, then the order on arrival) can go: issue the act at the click and draw `op.Walk`. Keep it for a driven agent only.
3. Card time: `WalkInS + Quote.S` for Watch and Listen (`Quote(LearnEv(id), p).S`), `Preview.WalkS + Quote.S` for the approaches.
4. On `order.dropped` (or a new entry in `g.Dropped`), pop the refusal's `locks` line for its code on that agent.
5. `LearnQuery` / `ScoutQuery` now answer ok out of reach. Callers that treated `too-far` as "open, walk first" (HudToolkit.Intel `Ok`, HostValeSteps `Can`) keep working.
6. `text.json` `locks["too-far"].byTarget.serving` ("Inez is serving a table. Back at the door in {s} s.") is no longer reached. Left for Fable to retire or reword; no code changed, so no new line is needed.

**Port:** ship the engine dll (`suborned.sim/0.23.0`) and the rulebook together, because every inputs hash moves. DP1 (`Fixtures/drive-session.script.json`) is re-recorded with identical orders. No recorded route outcome moves. Behaviour moves only in S22, N106 and N130 (rewritten) and N178–N180 (new). See `hash-moves-0.23.json`.

## Engine 0.24.0: the window marker's one read and `patrol.returns` (brief 043 §6, the done moment)
No rule changes and no inputs hash moves. Both presets; the range is unchanged.

**Read (read-only, not hashed):**
```
g.Windows()   // List<OpenWindow>, fixed order; each Kind, Thing, Until (the tick it closes as the engine has it now, -1 while held)
  call    guard        Call Inez: the guard away from the door until GuardAwayUntil; closes on guard.back. Not listed while he is held or down.
  call    pickup       Call driver: from car.blocks until BlockedUntil (closes on car.clears), or until driverLast if that comes first and the
                       turned driver takes the car to the service lane (closes on car.leaves).
  reroute patrol       Reroute: until PatrolAwayUntil; closes on patrol.returns (new).
  mislead rivals       False tip: until RivalsDelayedUntil (a retreat under fire is not listed). No log code at its end.
  loop    <camera id>  Loop: LoopCam ("entrance", "pickup", a mast), Until -1 (held while the Tech stays); closes on loop.ends.
```
The number is `(Until − g.Tick) / Game.Hz`. `Until` can move (an Alert recall brings the guard back sooner). The anchor is `Thing`; the face is the Fixer for a call, the Tech for a loop or reroute, the Scout for a false tip.

**New log code:** `patrol.returns`, on the tick the rerouted patrol's false orders run out (`Tick == PatrolAwayUntil`, patrol not down). The window's closing pop, as `guard.back`, `car.clears` and `loop.ends` are for theirs. `text.json` `silentLogs["patrol.returns"]` is a PLACEHOLDER for Fable (the pop's word is brief 043 §4's `Patrol back`).

**Host (Latch):** read `g.Windows()` for the marker instead of the five fields; pop on `patrol.returns`. Nothing to send.

**Port:** ship the engine dll (`suborned.sim/0.24.0`). The log code is hashed with the log, so runs where a rerouted patrol comes back before the end move from that tick: 50 of 307 scenarios and route scripts, 597 of 2336 mission-check plays (re-recorded as c030); no outcome, tick, Trail, Alert or gunfire moves. DP1 re-recorded with identical orders, inputs and checkpoints (only the engine string moved). The fight lab's matrix is unchanged. See `hash-moves-0.24.json`.

## Engine 0.25.0: why a hostile fires on an agent (`FireWhyOf`, `ai.why`)
Sky, playing 0.2.56: "Enforcer was shot by the door guard - I couldn't tell if that's because weapon was out, carrying Vale or coz they shot police etc?" No rule changes and no inputs hash moves. Both presets and the range.

**Read (read-only, not hashed):**
```
g.FireWhyOf(OpId op)          // List<FireWhy>, by id: every hostile aiming at the agent (its Target) or that fired on it within weapons.lineLapse (3 s)
  .By                         // "guard", "patrol", "security-1", "rivals", "rival-3", "officer-1", "police-2", "robot-1", …
  .Causes                     // string[] the causes it has for him NOW, strongest first; empty if none is live (its last shots after he holstered)
  .First                      // string[] the causes it had when it first took him (the ai.why line's)
  .Since                      // the tick it first took him
  .Aiming, .Firing            // its gun is on him; it fired on him within lineLapse
  .Main                       // Causes[0], else First[0]: the one word for the cue
g.FireCauses(string by, OpId op)   // the causes `by` has for him now, aiming or not (a hover, a debug overlay); sight and reach are not causes
g.FireWhyFirst(string by, OpId op) // (tick, causes) when `by` first took him; (-1, []) never
Game.FireCauseOrder           // range, final, fired, vale, violence, drawn, face, seen
```
**The causes** are exactly what the hostile's targeting reads (ai.intent), in that fixed order:
- `fired`: he has fired (any shot this mission). Police and robots read it; the rival crew does not.
- `vale`: he holds Vale. Everyone reads it.
- `violence`: security saw him do violence (a takedown, a carry, a failed takedown). Police and robots.
- `drawn`: his weapon is out, in that body's sight (not while a drop-it warning runs). Everyone.
- `face`: security knows his face (it saw a draw or violence, warned him, or a witness's report landed), from the kind's Alert rung (police 2; robots 2, or any Alert at wanted or infamous Heat). Police and robots.
- `seen`: the rival crew saw him act (a weapon out, a shot, a takedown, a carry), read once the crew is alerted.
- `range`: the fight range, where every body fires on every operative. `final`: the same rule in the final fight under `ai.intent.final every` (not the shipped rulebook).

So Sky's case reads, for example, guard `First` = `drawn, face` (he took the Enforcer as the fire order drew), then `Causes` = `fired, drawn, face` once he had fired; the crew at the car reads `vale` alone.

**New log code:** `ai.why`, once per (hostile, agent) pair, on the tick the hostile first takes the agent as its target: "<by> takes the <agent> as its target: <causes>." It is silent in `text.json` (`silentLogs["ai.why"]`, a PLACEHOLDER for Fable to word if it should speak). It is an `ai.` code, not `fire.`: `fire.*` lines are the cell's fire orders and the mission check counts them as shots before the final fight (`tools/missions/check/judge.ts`). Latch: read `FireWhyOf` rather than parse the line.

**Host (Latch):** nothing to send. Under the under-fire cue (`UnderFireOf`), show `FireWhyOf(op)`'s `Main` per shooter (one word or icon; Glyph and Fable pick the words). Prefer `Causes` (live) and fall back to `First`.

**Port:** ship the engine dll (`suborned.sim/0.25.0`). The log line is hashed with the log (tick and code), so every run in which a hostile takes an operative as a target moves from that tick: 104 of 309 scenarios and route scripts, 69 of 85 range runs. With the line suppressed (a probe, since removed) all 309 + 85 are byte-identical to 0.24.0, so the line is the only move. No outcome, tick, Trail, Alert, gunfire or shot count moves. Mission check re-recorded as c031 (2298 of 2336 plays move hash only). DP1 re-recorded with identical orders, inputs and checkpoints (only the engine string and mvid moved). Fight lab matrix unchanged. New scenarios: FW-DRAWN, FW-FIRED, FW-VALE, FW-VIOLENCE, FW-FACE, FW-SEEN, FW-RANGE. See `hash-moves-0.25.json`.

### Engine 0.25.0, also: brief 045's engine side (Redline, late lag): `ClosingHash()` and a cheaper Listen gate
No rule changes and no hash moves (the BASE gate: 315/315 scenarios and routes and 86/86 range runs identical to the fire-why build).

**F3: `g.ClosingHash()` (read-only, O(1) in mission length).** It returns the closing checkpoint `Engine.Play(PlayInputs, Game.Start, orders, Game.Tick)` ends with, from the live game, without a replay and without changing it. With a public fight open it closes it as `Finish` would (Standing, the ledger, the log, the fight's record), hashes, and puts every piece back. With no fight it is the checkpoint already taken at this tick, else `Hash.State`. Cost headless: 0.43 ms median at Sky's session 1's last save with a fight open (tick 4194; the replay there is 160 ms), 0.13 ms in loud-fight's.

Latch, in `HandoverHost.Save()` (HandoverHost.cs:1347–1348 and `ClosingHash()` at 1380): replace the body of the host's `ClosingHash()` with `return Game.ClosingHash();` (or call it directly) and drop the `Engine.Play` replay. Nothing else in `Save` changes: the `add` and `swap` bookkeeping stays as written. F1's worker thread is then not needed. `End()` still calls `Game.Finish()` first, as now. F2 (the replay-identical check and `FailCamStart`'s second replay) is still yours.

Tests (`dotnet test`, `ClosingHashTests`): for every scenario, every range run and every route script, a live replay calls `ClosingHash` at every order tick and every 5 s, as `Save` does. Its value at the end equals `Finish`'s checkpoint. At the first and last save with a fight open it equals `Engine.Play`'s closing checkpoint, and the run's checkpoints are still the played run's, so the calls change nothing. For loud-fight and Sky's session 1 it is checked against `Engine.Play` at every save with a fight open. `CostsUnderOneMillisecond` times it at session 1's last save with a fight open.

**F5: the Listen gate.** `LearnQuery` out of reach now asks only whether a walk in exists: one grid route to where the person stands, instead of the intercept's up to 120 routes along a server's round. The answer is the same (the intercept is null exactly when that route is; `GateQueryTests` checks it against `WalkInS` over both of Sky's sessions and route A). Your `Gates()` (HostDone.cs:84), three `LearnQuery("scout","listen",role)` calls, measured every second headless:

| run | before (0.24.0) | now |
|---|---|---|
| Sky's session 2 | mean 1.54 ms, p99 15.9 ms, max 16.9 ms | mean 0.05 ms, p99 0.38 ms |
| Sky's session 1 | mean 0.11 ms, p99 0.45 ms | mean 0.07 ms, p99 0.45 ms |

The card's numbers are unchanged: `WalkInS` and `Preview(...).WalkS` still run the intercept, so call them for the hovered card only, never per frame. F4 (`Gates()` once a tick) is still worth doing.

## Engine 0.26.0: the squad fire order agent by agent (`FireQuery`)
Sky playing 0.2.65: "I had the whole squad selected but some of my agents weren't firing at the target e.g. the Scout." Findings and the fix split are briefs/041/next-batch.md item 13.

- `Game.FireQuery(OpId op, string target, bool force, bool focus, out bool walk)` (and an overload without `walk`): the `Answer` that `Issue` would give a `fire` order for this agent now, without giving it. Taken, or the refusal's code, words and numbers, in the order `Check` then `Fire` test them: `mission-over`, `agent-down`, `enforcer-inside`, `agent-busy` (tick = when the act ends), `in-car`, `no-weapon`, `fire-carrying`, `fire-carrying-vale`, `no-target`, `fire-on-cell`, `target-unknown`, `civilian-target`, then `out-of-range` / `no-clear-line` only when there is no walk in. `walk` = taken and it would walk into range or to a line first (never for a focus or a driven agent).
- Taken includes what the engine resolves: a holstered gun draws (`draw-ticks` 1.0 s, then `aim-new` 1.0 s), out of reach or line walks in (`weapons.close`). A focus out of reach is taken but never walks in, and it lapses after `line-lapse-s` 3 s.
- Read-only. No rule, state, inputs or run hash moves (today 312/312, range 86/86 identical to 0.25.0 under `--start-text BASE`). Scenarios QF-HOST, QF-ALL, QF-LINE, QF-CANT, QF-FOCUS (range) and QF-BUSY (Handover) keep it equal to `Issue`.
- Use it for every selected agent before a squad fire order, and give the order to each one it takes. Each one it refuses gets its code on the plate (two words, Fable). Ask once per order or per hovered target, never per frame for all targets: the walk check runs one grid search per agent out of reach.
