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

## Engine 0.28.0: sight from real heights, who notices what, the Watch that shows something (briefs 055 §7, 054 rev 4)
One bump. Every inputs hash moves (the rulebook, the drawn site, the people file's `faces`, the re-baked rounds); a run pinned to 0.27.0 replays byte-identical on its own inputs (the rules read only when their rulebook sections are there).

**What the host must read (Latch):**
- **The fan has shadows.** `Fan.Shadows` (Pt[][], convex, block frame cm): stretches inside `Points`/`More` the watcher does not see, behind a solid it sees over (a 2 m planting under the 4.79 m lens: about 1 m behind it) or under (the patrol robot's 0.95 m eye and a 1.1 m planter: from some way behind it on), and behind a person when the eye is above their head. Draw a point as in sight when it is in a polygon and in no shadow; cut the shadows out of the wedge's fill or rim. `SightTests` hold the fan to `InSight` at every sample.
- **The mission's cameras are pole cameras** (site `cameras[].kind`, lens 4.79 m above its floor). Draw them with `CCTVPole` at the plan's 5.5 m mount (Motive, brief 055 §7), not the 3.2 m stand-in, so the glass is where the engine sees from.
- **A civilian's cone on hover, only where it matters** (brief 054 §3.1): `Game.PersonCone(personId)` → `PersonSight` (Kind `wedge`: At, Facing, Reach 1300, HalfCm 2252 at the reach, a ±60° sector with no near ring; `ring`: security, all round within Reach; `none`). Draw it on hover only if `Game.CaresAbout(personId, act)` for the act armed (`takedown`, `carry`, `weapon-drawn`, `threat` for Pressure, `run`), or the person is on the armed card's eye (`Quote.WitnessIds` for Pressure, `TakedownQuote.People`), or an operative they would see is doing a seen act now (`Game.NoticesNow(personId, act, point)`). Below Alert 2 a Pressure, a run or Vale's grip draws no civilian cone; a takedown, a carry or a drawn weapon does. Alt: the cones of the armed card's people, the 6 nearest. Never compute a cone in the host: the engine's reads are the rule.
- **The Watch offer.** `Game.WatchOffered(role)`: false → no Watch row at all (panel, ring, verbs 1–4, ContextRank skip it; Intel's next step goes to Listen). `ScoutQuery("watch", role).code == "nothing-to-see"` says the same (free; text.json has no line for it: never shown). After a Watch, `already-watched` greys the row as before; if the person's mood changes the row comes back. Listen and Pay need a Watch only while one is offered (`needs-routine` otherwise), so the driver can be listened to and paid at once.
- **Mood on the card.** `Game.MoodOf(role)`: the mood the cell knows (`""` none, `uneasy`, `scared`, `friendly`, `neutral`), from its last Watch, or the true one once Listen has told the temperament. `Quote.Mood`, `Quote.Likely` (a dashed ✓: the approach the mood favours) and `Quote.Hurt` (a dashed ½) on the turn previews. Two new price lines on turn.help (`ready`, `ready-at-one`), one on turn.pay and turn.pressure (`ready`): they hold only when a mood lifts a suiting fit.
- **Strings in code** (Fable's words, brief 054 §7): `HudModel.cs:117` "Watch: learn the routine" and `:426` "Learns their routine" promise a routine the Watch no longer teaches.

Reads are read-only and cost little: `PersonCone`, `NoticesNow` and `CaresAbout` per hovered person, not per frame for everyone.

## Engine 0.29.0: a body at your feet, and no affair Watch (brief 054 §9, Motive 2026-10-24)
- **`PersonSight.NearCm`** (new field on `PersonCone(id)`): a civilian's near sense for a body, 150 cm (rulebook `witness.notice.near`, `people-near-body-cm`). It is set for every civilian on the street, a wedge or not (a civilian with no facing still finds a body within 1.5 m); 0 for security (their `ring` already covers it), off the street, or a rulebook with no near row. Glyph's mark (§9.1): while the armed order leaves or drops a body (a takedown, a drop), draw a ring rim of `NearCm` at the civilian's feet with their wedge. No other new mark.
- `NoticesNow(id, "body", at)` now answers true within `NearCm` on a clear line whatever the facing; `NoticesNow(id, "takedown" | "carry", at)` stays cone-only. `CaresAbout` is unchanged (a body is an always act).
- **The affair Watch is gone:** `WatchOffered(role)` is never true for the affair; with the lovers together a Watch on Inez is offered only for her mood (and refused `already-watched` once watched or turned). No host string promised it; nothing to remove.
- Inputs moved (rulebook, people rules text, the rounds file's rules sha): every inputs hash, DP1 and the lab checkpoints re-recorded with only the engine pin and hashes moved.

## Engine 0.30.0: the reroute walks, the seal's car and clock, a true debrief (briefs 057 §2 and §4, 060 #8; Suture's bot pass)
All in `sim/Suborned.Sim`; the Unity plugin copy (`unity/Assets/Suborned/Sim/Plugins/Suborned.Sim.dll`) still predates 0.29 (`tools/game/runtime-compile.test.ts` fails on `PersonSight`): copy the 0.30.0 build in first. Names are exact; your reflection in HostSeal and HostReroute finds them, but plain calls are better now.

**The patrol (brief 057 §2; Sky: "the robot follows the drawn path and moves smoothly").** With `cell.reroute.walk` in the rulebook the rerouted patrol walks: from where it stands at the landing, on the walk grid at `patrol-chase-cm-s` (0.85 m/s, 8.5 cm a tick) to `patrol-reroute`, waits facing away, then walks back so it stands on its sweep exactly where `SweepPos` has it at `PatrolAwayUntil`. The whole trip is inside the false orders, and the patrol is blind throughout as before, so the window marker's `reroute:patrol` and `patrol.returns` keep their meaning and tick. From route A's landing the trip is about 46 s out, 19 s waiting and 50 s back. `PatrolPose()` is the walk, never a jump (N186: the largest step in a tick is 9 cm).
- `Pt[]? Game.PatrolRoute()`: the path from now as one polyline (block cm): on the sweep, where it stands then both ends; rerouted, the walk out, the wait spot, the walk back, the sweep. Null when it is down. HostReroute already reads this name; it no longer needs its own `Grid.Route` fallback.
- `PatrolPlan Game.PatrolLegs()`: the same, with times. `Mode` (`sweep`, `away`, `chase`, `alarm`, `down`), `At`, `SweepA`, `SweepB` and `Wait` (the site's `patrol-a`, `patrol-b` and `patrol-reroute`, never typed in), and `AwayUntil` and `BackAt` (both `PatrolAwayUntil` while rerouted, else -1). `Legs` is a `List<PatrolLeg>`, each with `Kind` (`out`, `wait`, `back`, `meet`, `sweep`, `chase`, `alarm`, `hold`), `bool Walk` (true: it moves along `Points`; false: it stands at `Points[0]`), `List<Pt> Points`, `List<int> Ticks` (the tick it is at each point; a chase's are estimates), and `int From` and `Until` (-1: no end known).
- To draw while the reroute is in play: show `PatrolRoute()` while `PatrolAwayUntil > Tick`, and the sweep line otherwise. The robot lies within 1 cm of the line read the tick before (N186, 2,496 ticks). A new order can change the line, so re-read it each frame or when `Mode` changes.

**The seal (brief 057 §4).**
- `FailCause.Car` (`string`): on a `sealed` fail, `"empty"` (Vale not aboard), `"ready"` (Vale aboard, the car free, Go never given) or `"held"` (Vale aboard while combatants held the car). On every other ending it is `""`. `FailCause.Holders` (`List<string>`) is exactly `CarHolders()` at the seal, in id order: the ids Go's `car-held` names. `HostSeal.SealCarOf` gets `IEnumerable` and joins them, so your `{holders}` phrase works unchanged. Also `int AboardAt` (Vale's `board.in` tick, -1), `int GoRefusedAt` (the last refused Go before the seal, -1) and `string GoRefused` (`car-held` or `car-empty`). The cause's JSON carries `car`, `holders`, `aboardAt`, `goRefusedAt` and `goRefused` for the seal only.
- `Game.Windows()` now ends with `seal:district` while the clock runs (`!Over && Lockdown < INF`): `Until` = `Lockdown`, plus new `OpenWindow.From` (the tick the clock was last set or pulled earlier: your underline's span) and `OpenWindow.Origin` (`CauseOrigin`). The other kinds have `From` -1 and `Origin` null. HostDone's window list will pick it up at once, so give `WindowKey("seal","district")` the lock marker or skip it there if HostSeal draws its own.
- `CauseOrigin? Game.SealOrigin` (live): the report whose seal time is running now (a later report that pulled the clock earlier, else the one that set it). It is null before the clock is set. `Because` is `loud-fight`, `response` or `confirmed`. `Report` is the report itself (`gunfire:<target>`, `drawn-seen:<op>`, `post-empty:<who>` for a missed check-in, `report:<kind>:<victim>`). `Reporter.Id` and `Reporter.Kind` are in the engine's grammar (`camera:<id>`, `guard`, `patrol`, `security-<n>`, `robot-<n>`, a person's role or id, `desk`, `heard`), and `Confirmer` is set for `confirmed`. `Game.SealFirstOrigin` is the one that started it. HostSeal's `GetProperty("SealOrigin")` finds it, so the log guess can go.
- `seal.starts` is now also logged when the response's call sets the clock (rulebook `security.sealWarns`), with the time, the unit's arrival and the report it came from. `fight.loud` still announces the loud fight's own clock.

**The debrief (brief 060 #8).** `Debrief.Of(g).Route` claims only what happened. `vale-reaches-getaway-car` appears only once Vale boarded (`Game.ValeBoarded`), `vale-intercepted-outside` only once the Take landed (`Game.ValeTakenOutside`), and `vale-taken-through-office` only on `enter.out`. The guard, loop, patrol and pickup-camera lines appear only when they held at the Enter or the Take (`Game.AtEntry`, `Game.AtTake`: `RouteFacts` with `Lured`, `Looped`, `PatrolAway`, `PatrolSweeping` and `CameraBlocked`). A rerouted entry now reads `patrol-diverted` (0.29 said `entry-timed-between-sweeps`). There are no new ids, so Fable's `debrief.route` covers them all, and the `{holders}` story line follows `FailCause.Car`. `HudModel.cs:389-393`'s map keys are unchanged.

**Refusals.** A call or turn on someone who is no cast role (a crowd id such as `p-records-office-73`) is now refused with `not-a-contact` or `target-unknown` instead of throwing. `CanCall` returns false for them.

**The grid.** The mission cameras' poles (entrance (53, -44.5), pickup (74, -40), r 0.48 m) are walk solids, so routes keep the body clear: the crew's route to the driver passes 0.77 m from the pickup pole (it was -0.28 m, through it).

### Engine 0.30.0, also: the batch notice read (Redline 064 #1)
- `NoticeProbe? Game.NoticeProbeOf(string id)`: the person resolved once a tick (where they stand on the street, how they face). It is null when they are not on the street. `NoticeProbe.At` is their position and `NoticeProbe.Id` their id.
- `bool Game.NoticesAt(NoticeProbe probe, string act, Pt at)`: exactly `NoticesNow(id, act, at)` (scenario NQ-1: 123,627 reads, 0 differ), without re-scanning the street for each point. A probe from another tick throws, so make one per tick.
- `Occluder.Blocks3` no longer allocates a `Pt[]` for each test (#3). Every scenario hash and every range hash is identical to the baseline's.

### Engine 0.30.0, also: Listen and Watch locks while the agent is busy (the 0.2.116 bot row, Q-OFFER `listen:needs-routine`)
- Listen still needs the routine while a Watch is still to give on that person (`WatchOffered(role)`: brief 054 §2.1). `WM-1` and `NQ-2` hold to it.
- `LearnQuery(agent, verb, target)` used to answer `agent-busy` before anything else. `Locked` lets `agent-busy` through under the hold preset, so a busy Scout's Listen on Inez was offered and then refused as `needs-routine` once he was free. For a busy agent, LearnQuery now returns the order's own refusal first (`needs-routine`, `needs-need`, `already-heard`, `only-scout`, `nothing-to-see`, …), and `agent-busy` only when reach or line is the only other bar (`too-far`, `no-clear-line`, `no-walk`).
- The host should read: `LearnQuery(<the agent who will give it>, verb, role)`. That agent is always the Scout for Listen, whoever is selected, because HostValeSteps already hands Listen to the Scout. Show any code other than `too-far` and `agent-busy` as the lock. No host change is needed beyond asking for the giver: with this engine, `Locked` gets `needs-routine` and shuts the wedge.

## Engine 0.31.0: Loop v2, Disable, the camera shot's price, a runner remembered from Alert 2 (briefs 058 §6 and 054 §8; Sky 2026-10-25)
One bump. The rulebook (`cell.loop.run`, `cell.disable`, values `loop-s` 20, `loop-run-s` 120, `disable-s` 10, `disable-trail` 2; `tether-m` and `cancel.loopHeld` retired; `witness.notice.never` gains `disable`) and the site's people sha (the needs text, brief 041) moved, so every inputs hash moves. A run pinned before 0.31.0 replays on its own inputs: the engine keeps the tether, its refusals and the held-loop cancel for a rulebook that still carries `cell.loop.tether`. The dll copy into Unity is the batch's sim step (`tools/sim/unity.ts`).

**What the host must change (Latch):**
- **The loop has an end, and no tether.** A landed loop plays `cell.loop.run` (2:00) from its landing wherever the Tech goes; the Tech may run, Watch, board and walk off. Drop the walk-off stopgap's hold (058 §5) and the "Holding the loop" plate with this engine: a move never ends a loop now. `Game.LoopUntil` is the tick it ends (`INF` when none runs); `Windows()` lists `loop:<cam>` with that `Until` (was -1), so the window marker counts 2:00 down like the others (Glyph 058 §3: drop `m.Face` for the loop). At the end the engine logs `loop.ends` ("The entrance loop runs out").
- **Cancel:** a landed loop is no activity of the Tech's: `ActivityOf(Tech)` never reads `loop / holding` and a cancel on her is `nothing-to-cancel` unless she is still splicing (then `lost`, as before).
- **One loop at a time:** a Loop on another camera while one runs is refused `loop-holds` with `Answer.tick` = the tick the running loop ends (text.json `{tick}`); a Loop on the camera already looping is taken: a re-splice, `loop.s` again, +1 T, a fresh 2:00 from its landing (no stacking). A Loop or a Disable on a dead camera (shot or cut) is refused `camera-dead`.
- **Disable, the Tech's new verb on a camera:** order `{ "do": "disable", "op": "tech", "target": "<camera id>" }` (the ids the cabinet loops: `entrance`, `pickup`, `mast-314`), at the cabinet (`stand-at-cabinet`), 10 s, +2 T when it lands (`camera.cut`). The camera is dead to the end: `CameraDark(id)` true, `CameraDeadAs(id)` = `"cut"` (`"shot"` for a shot-out one, `""` while it lives: Glyph's dead disc, 058 board 02), its fan `Live` false. Refusals: `only-tech`, `stand-at-cabinet`, `disable-wrong-target`, `camera-dead`, `unknown-verb` (a rulebook with no `cell.disable`). A cut ends a loop on that camera (`loop.ends`).
- **The desk's clock:** after a cut, `Windows()` lists `disable:<cam>` with `From` the cut's tick and `Until` the tick its report lands (the desk's next scan of that screen + `post-call-s`); it goes when the report lands (`camera.cut-reported`: a reported incident, Alert to at least `byIncident[n-1]`, the dial's dispatch to the camera). Before the order: `Game.CutNoticedAt(cam, landAt)` is when a cut landing at `landAt` would be noticed (the order line's "in n s": landAt = now + `disable-s` for an order given now) and `Game.AlertIfCut()` the Alert its report would raise (the siren's number: 1 for the first incident, 2 after). `Game.CutReps` (`Cam`, `From`, `LandAt`, `Done`) is the same, read-only.
- **Camera shot price:** unchanged in the engine and already right: `PreviewFire(op, "camera:<id>")` gives `TrailShooting` 2 (0 once the cell has fired at it), `TrailHit` 0, `TrailDown` 0. Build the pill, the panel row, the aim chip and HostFight's cost line from those three (058 §3): never "/hit" for a camera or a car. Any armed operative may fire on a camera (scenario CAM-ANY): offer Fire on a camera's ring to every armed selected agent.
- **Running at Alert 2:** `QuoteMove(op, to).RunRememberedBy` now lists the civilians who would see the run in their cone and keep the face when Alert ≥ 2 and the run is not loud (no fight, no shot fired, not under fire); before 0.31.0 it was always empty. `RunSeenBy` and `AlertIfRun` are unchanged (security is free of a run from Alert 2). Draw it as the move card's "remembers" mark (brief 054 §3.5).
- **Unchanged names you already read:** `LoopOn`, `LoopCam`, `Windows()`, `PatrolRoute()`, `PatrolLegs()`, `FailCause.Car`/`Holders`, `SealOrigin`. `PatrolLeg.ToString()` is now culture-invariant (it wrote -1 with the culture's minus).

**New log codes:** `camera.cut`, `camera.cut-reported`, `loop.dead` (the camera died while the loop was being spliced: nothing lands, nothing charged), `disable.dead` (shot while being cut). Changed text, same codes: `loop.on` ("One feed repeats until m:ss" / "re-splices"), `loop.ends` ("runs out" / "the line is cut"). The ledger's Trail causes: `loop`, `disable:<cam>`; the Alert cause of a cut's report: `report:cut:camera:<cam>`.
**New refusal codes:** `camera-dead`, `disable-wrong-target` (both in `Answer.Codes`; Fable's text.json has both). `run-tethered`, `board-tethered` and the held loop's cancel line are no longer given on the live rulebook (kept for pinned runs).

## Engine 0.32.0: the pickup round the back, the rival crew of five, the getaway car's Drive (brief 059 K1–K11; GDD v0.25.0 §07; Sky's answers 1a, 2a, 3a)
One bump. The rulebook (`rivals` section, `cell.driveCar`, `car.pickup`, `fight.safe.leaveUnfought`, `fight.loud.entry`, `memory.acts.saw-getaway`, eleven values; `fight-safe-hostiles` and `fight-modifier` retired), the site (rival-park at (90.5, -31); `getaway.stands`, `rivals.car` and `rivals.posts` folded in from the old side file) and mission.json's timeline moved, so every inputs hash moves. The dll copy into Unity is the batch's sim step (`tools/sim/unity.ts`). Scenarios P59-* (`sim/Suborned.Sim/ScenariosPickup059.cs`) show every rule below working.

**Vale and his car (K1–K5).**
- Vale always walks out of the official door at 5:50 to his car in the official bay (6:10). He gets in `car.pickup.valeIn` (3 s) later: new `ValeAt.InCar` (added last to the enum). With the driver **unturned**, his car then drives him away and the run fails `vale-leaves` (cause `driven-away`, Sky 3a). With the driver **turned**, it drives round on the drive grid at `car.pace` to the service bay (`car.parks` at about 6:25.9, then `PickupService` true) and waits there until the 8:30 close, when it drives him on (`car.leaves`, then `vale-leaves`). The service-lane walk is gone: `vale.exits` and `vale.at-car` are always the front, so drop HostFeed.cs:315–316's and HostTakedown.cs:439's `PickupService` branches for those two codes.
- Reads: `Game.CarPos()` and `CarHeading()` follow the car while it drives; `ValePos()` is the car's while he is `InCar`. `ValeCarParked` (bool) = parked round the back with him in it. `DriverPos()` = the driver's seat. `CarAtMouth` (bool) = the car stopped at the drive's mouth because the getaway car stood on the drive first (K5; log `car.blocked`). In that case the Take there is in-car as in the bay.
- **The Take:** with the driver turned the Take is in his car once it parks (`TakeInCar`). It is refused `driver-brings-vale` at the front and `car-not-parked` while the car drives. With the driver unturned it is the front Take as before, but only while he stands at the car (6:10 to 6:13). A front Take with the rival car parked and its wheelman aboard fails `rivals-reach-vale` (cause `beat-to-pickup`, culprit the wheelman: P59-BEAT).
- **The driver's Call is retired** (Motive M059-6.1): `call` on the driver is refused `nothing-to-call`. Nothing blocks the pickup camera any more (`PickupCameraLive()` is the loop's answer alone). Remove HostFeed.cs:264/317's and HostLess.cs:46's `car.blocks` entries: the code is never logged now, and text.json has no card for it.

**The getaway car's Drive (K6; `drive` stays Take control).**
- Order `{ "do": "drive-car", "op": "<agent in the car>", "target": "<stand id>" }`. The stand ids are the site's `getaway.stands`: `square` (the Getaway bay, where it starts) and `service-drive`. Everyone in the car rides: the others get a `ride` busy order to the same end tick. The order is logged `order.drive-car` and its landing `car.parked`.
- Refusals: `drive-no-driver` (the agent is not in the car), `unknown-stand`, `drive-here` (already there), `drive-busy` (someone in the car is mid-act), `drive-held` (`Answer.ids` = the holders, as for `car-held`), `drive-fight` (the final fight has started: fight where you are and Go), `unknown-verb` (a rulebook with no `cell.driveCar`). Board is refused while it drives, and a Board that lands after it leaves is `board.missed`.
- Reads: `Game.CurStand` (`Stand`: `Id`, `Car`, `CarAt`, `Facing`, `Exit`, `ExitHalfCm`, `Ambush`, `Police`) is where it stands now. `GetawayDest` (`Stand?`) is where it is going, `GetawayDriving` (bool), `GetawayCarPos()` and `GetawayHeading()` its live pose (the engine's own car point is `GetawayCarPos()` now, so seats, the exit lane and the fight follow it), and `GetawaySeat` the boarding point at the current stand. `DriveTicks(standId)` is the drive's length in ticks for the order line (-1 for no stand). `LiveCoverOf(of)` counts the live cover points round a car. The exit lane, the fight spots, the police posts and the ambush spots are the current stand's. The turned driver who sees the getaway car park within witness range learns it (memory act `saw-getaway`: the Talker test as for anyone).

**The rival crew of five (K7–K10; Pulse briefs/059/pulse/rival-routine.md).**
- Ids `rival-1`..`rival-3` (the three posted round our car), `rival-4` (the lookout) and `rival-5` (the wheelman), plus the car `car:rivals`. Nobody of the crew is on the map before the car turns in. Its timetable is computed back from the 3:10 park and stored on the game: `CrewEntry` 2:35.6, `CrewDropArrive` 2:46.4, `CrewDropLeave` 2:54.4. The car drops the three at `rival-drop`; they walk to their spots round the getaway car's stand (rival-1 to ambush-3, rival-2 to ambush-1, rival-3 to ambush-2). It parks at `rival-park` at 3:10 and the lookout posts at `rival-lookout`.
- **One read for the bodies:** `RivalPoses()` → `(members: List<(id, pos, facing, mode)>, car: (pos, heading, state))`. `mode` is one of `aboard`, `walking`, `posted`, `talking`, `boarding`, `fight`, `down`. A member aboard is listed at the car, a member down where he fell, a member gone not at all. Car `state` is one of `off`, `entering`, `dropping`, `rounding`, `parked`, `tail`, `tipout`, `away`, `tipback`, `tostand`, `stopped`, `front`. `RivalCarPose()` is the car alone. `MemberBeat(id)` is the pose a posted member plays now (rulebook `rivals.beats`: `watch`, `lean`, `check-watch`, `look-around`, `read-paper`, `pace-out`, `pace-back`; the wheelman aboard `wheel`, `mirror`, `window-arm`; `walk`, `talk`, `board`; `""` in the fight; `lean` reads `watch` with no cover beside him), so Forma's idles can key off it. `Crew` (`List<CrewMember>`), `CarSt` and `CrewOf(id)` are there for the debug view only: draw from `RivalPoses()`.
- **What a Watch on the rivals teaches** (`watch` on `rivals` or `car:rivals`, Scout): `RivalsWatched` turns true and `RivalNext(id)` → `(kind, tick, at)` shows each member's and the car's next move: car `stops`/`leaves`/`follows`/`back`, posted `spring` (the ring round our car), lookout `leaves`. `CrewNextMove` is the car's raw next event (debug).
- **The follows:** the posted three walk after a Driven getaway car (`rivals.follow-car`, at `rivals-follow-cm-s`: Sky's S3). The car tails Vale's car round the back when he is driven (`rivals.follows`), and the wheelman walks to his door (`rivals.at-vale`, fail cause `tail` if he gets there first). The false tip (`mislead` at the parked car with the wheelman aboard; refusals `no-rivals`, `stand-near-rivals`) sends the car away and back (`rivals.tip-leaves`, `rivals.back`). The turned driver warns the lookout off (`rivals.warned`, `rivals.word-off`, `rivals.lookout-gone`, `rivals.no-warning`). Gunfire before 2:35 brings the car in within 16 s to the Office front (`rivals.drawn`, `rivals.drawn-stop`).
- **Fights:** the safe fight's rivals are the crew's own bodies (no fresh spawns). `CrewInFight` (int) = bodies plus those still coming (the lookout boarding). A cell shot at a lurking member springs the ambush. On the street a member is hit and downed one by one (`rivals.hit`, `rivals.down`). If more than half of the crew on the map is down before the final fight it breaks: it leaves in its car for `rivals-retreat-s` and comes back with whoever is still up (`rivals.retreat`, `CrewBrokenPre`). Sky's S1: with crew still to come, Go leaves unfought (`fight.safe.leaveUnfought` true). S2: the loud fight's police come from the entry nearest the car's stand (`fight.loud.entry` `nearest`).
- **Changed meanings (Latch):**
  - `WatcherIds()` lists `rival-1`..`rival-5`, and `WatcherKind(id)` answers `rivals` for each. HostAttention.cs:71 skips them already.
  - `PosOf("rivals")` and `GunOf("rivals")` are null, and `Known("rivals")` is gone. ProofRings.cs:91 and HostFight's rivals-gun rows must read members (`PosOf("rival-n")`, `GunOf("rival-n")`) or `RivalPoses()`.
  - `fire` on `rivals` is an alias for the nearest firable member. `mislead` and `watch` take `rivals` or `car:rivals`.
  - `RivalsPresent` = the car on the map or any member on foot.
  - `RivalsPose()` is a migration shim (the car's pose, else the first member on foot). HostViews.cs:174–179, HostLess.cs:318 and HostFeed.cs:369 should move to `RivalPoses()`: one marker per member and one for the car, so HostViews' three followers go.
  - HostFeed.cs:63's `"rivals" => "the pickup"` should name the place the member stands.
  - HostFeed.cs:183's mislead feedback should read `CarSt` (`away`) rather than `RivalsPresent`.
  - PanelGuard's `rivals` row, HostTargetRings.cs:94 and HostAim.cs:263 keep the family; HostAim.cs:540 already names `rival-n` "a rival".

**Codes.** New refusals: `drive-no-driver`, `unknown-stand`, `drive-here`, `drive-busy`, `drive-held`, `drive-fight`, `driver-brings-vale`, `car-not-parked` (all in `Answer.Codes` and in text.json's locks; `drive-here` and `drive-fight` have placeholder words for Fable). Retired: `car-gone`, `car-busy`, `call-too-late`. New log codes: `car.parks`, `car.parked`, `car.blocked`, `car.leaves`, `order.drive-car`, and `rivals.coming`, `.drop`, `.post`, `.arrive`, `.follows`, `.follow-car`, `.at-vale`, `.tip-leaves`, `.back`, `.warned`, `.word-off`, `.lookout-gone`, `.no-warning`, `.drawn-stop`, `.joins`, `.late`, `.hit`, `.down`, `.retreat`. The ones Fable has not yet worded are in text.json's `silentLogs` (PLACEHOLDER). `Start.valeWaits` (Vale waits at his car past 6:13) is test only: never set it from the host.

**Dead camera, cut or shot:** unchanged since 0.31.0. `CameraDeadAs(id)` is `"cut"`, `"shot"` or `""`.

## Engine 0.33.0: the final fight always comes, the crew as people, Motive's balance fixes, the crowd's legs (brief 059 Sky's S1 = no; Motive s1-rulings.json, balance-rulings.json; brief 060 cause A)
One bump. The rulebook moved (values `fight-always` 1, `rivals-floor` 3, `rivals-free-cm` 150; `fight.safe.always`/`floor` replace the retired `fight.safe.leaveUnfought`; `fight.alwaysRule`, `rivals.free`, `rivals.peopleRule`; the ai.intent foe `crewmate` in rivals calm and alerted), the site moved (svc-ambush-1 at (77.35, -8.0), Vantage; `rivals.entry` dropped; the cover's north refuge is `mouth:rival-car-entry`), and the crowd's legs moved, so every inputs hash and many routes move. Scenarios S1-*, BAL-*, CREW-BODY and CREW-FREE (`sim/Suborned.Sim/ScenariosAlways059.cs`) show each rule; `GateC037Tests` and `CrowdLegTests` are xunit tests (they read the repo's routes and the people file).

**The gate (Go).** With `fight-always` 1, from the final fight's start until it is over, Go with Vale aboard is refused `car-held` while the fight's blocker is up, wherever he stands (not only within 8 m or on the lane). Before a blocker is named (the late spring below), whoever is coming holds it:
- `car:rivals` while a member still rides in the rival car. The answer's `tick` is then the first rider's step-out tick (or `-1` while the car drives with no step-out set yet; its stop plus rivals.out). The words read "(the rival car is coming: out in N s)". Show the countdown from `tick` (Motive M059-S1.7: "Rivals coming · 18 s"; Fable words it). `GoQuery()` answers the same.
- a member walking back to the fight (the recalled lookout) or one knocked out and due to wake, by his id; the loud fight's officers on their way.
Go without Vale and the menu's Withdraw are not gated. Drive is still `drive-fight` once the fight has started. Draw `car:rivals` as a holder with the car's marker (it is a car target id already).

**The late spring.** The ambush springs as before, or once Vale is in the car. If nobody of the crew is on foot then, no blocker is picked until the first rider steps out (`fight.blocker` logs it then, among those on foot). Each member who steps out after the spring fires `AimNewT` after his step-out. With `fight-always` 0 none of this applies, and engine 0.32.0's leave-unfought behaviour comes back (S1-GATE-SWITCH-OFF).

**The floor and crew destroyed.** At the safe fight's start, if fewer than `rivals-floor` members are up and coming, away members come back, the earliest at the stand first:
- the tipped car cuts its trip short and rides to the stand's rival stop (`CarSt` `tostand`; `RivalsDelayedUntil` becomes now);
- or the warned lookout walks back from where he is (or from `rival-lookout-exit` once gone): he reappears in `RivalPoses()` walking, mode `fight`.
Each logs `rivals.recalled`, and the fight's why gains `rivals-recalled`. With every member killed or tied (none knocked out to wake), the fight is over at its start: `rivals.wiped`, fight why `crew-destroyed`, chain `rivals-destroyed`; the gate is open. The debrief's `fight.why` carries both codes.

**The crew as people (takedown, carry, drop, stash, restrain).** `takedown` on `rival-1`..`rival-5` (the Enforcer) and the body orders `carry`, `drop`, `stash`, `restrain` on a rival body work as on anyone, through the brief-013 pipeline (`Bodies`, `Acts`, `BodyById("rival-n")`, the same log codes: `takedown.starts/out/dead/fails`, `carry.body`, `carry.down`, `stash.*`, `restrain.done`, `restrain.freed`). A member in the rival car or in the final fight is refused `person-away`; fire on him there.
- He is armed and aware of the Enforcer by his own facing cone (no all-round near ring): aware, the takedown fails (`takedown.fails`) and he fires.
- A crewmate on foot who sees a takedown on one of the crew (his cone with its near ring, a line, inside his weapon's max) fires on the taker: `rivals.see-takedown`. His cause in `FireCauses`/`ai.why` is the new `crewmate` (HudToolkit.Roster.cs `WhyIcon` needs a case for it; FireCauseOrder puts it after `violence`).
- Knocked out, he wakes after wake-s: `rivals.wakes` (never `victim.wakes`). He makes no report and no statement, keeps the Enforcer as a foe (he shoots him on sight before the fight), and rejoins his crew: his post, the front if drawn, the car if the crew has gone back to it, off the map (`rivals.gone`) if the car is away; in a running safe fight he joins it.
- Tied, a crewmate within `rivals-free-cm` frees him in free-s: `rivals.frees`. A civilian finder frees him too, as for anyone.
- Civilians who see the act or find the body report it, and the Trail and Alert are charged as for anyone. A rival never reports, is never missed, and between missions a rival act counts only as a found killing: `TakedownQuote` on a rival reads `Armed` true, `IfNeverFound` "" and `TrailIfMissing` 0.
- `RivalPoses()` lists a member taken down where his body is (at his carrier while carried) and not at all while stashed. A fight body that fell is listed where he fell (0.32.0 listed (0,0): HostViews can drop any guard for that). `MemberPos` follows the body.

**Balance fixes (no new codes).**
- M059-B1: a posted or walking member becomes a fight body where he stands (0.32.0 started them all at (0,0)).
- M059-B2: a rival blocker never takes the kite step out of a shooter's line; he keeps his lane.
- M059-B3: a crew broken before the final fight never presses (no `ai.kite` "presses" after `rivals.retreat`).

**Brief 060, cause A (the crowd).** A routine leg whose end has no free walk cell within 6 m (off the window, as the T1 stop) now joins the walk grid at the free cell nearest it with no cap (`Grid.NearestFreeAny`) and leaves the window from there. An end on a cut-off pocket (cafe-table-5-seat-2) joins the other end's piece. No routine leg with an end in the window is a straight line now (`CrowdLegTests`). The 5:30, 6:30 and 7:30 clerks bound for T1 walk round the Office and past the official bay to Apron Road's north end; they no longer walk through the Office. So `OnTheStreet` no longer counts people inside it, and more of them see the official bay from about 5:45 to 6:10 (N82 re-staged). `HostViews.GiveWay` (cause B) is still Latch's.

**Retired.**
- `fight.safe.leaveUnfought`, and the scenarios P59-S1-GO and P59-S1-HOLD.
- The debrief route line `pickup-camera-blocked` (with `car.blocks`: nothing blocks the pickup camera now). HudModel.cs:397 and tools/qa/checks/outcome.ts:79 can drop it.
- The site's `rivals.entry` (the engine reads it if present, and nothing needs it). Vantage can retire the place `rival-entry`.

**Test only.** `Start.CrewGone` (script start key `crewGone`, rival ids): members killed before the mission, with no body. For the floor scenarios only: never set it from the host.

**Codes for Fable** (text.json, not edited here). New log codes:
- `rivals.recalled`: the floor brings back the tipped car or the warned lookout.
- `rivals.wiped`: the safe fight is over at its start, the crew destroyed.
- `rivals.see-takedown`: a crewmate sees one of the crew taken down and draws.
- `rivals.wakes`: a knocked-out rival comes round (or is brought round).
- `rivals.frees`: a crewmate unties a tied rival.
- `rivals.gone`: a woken rival with his car away walks off the map.

Also for Fable:
- New fight why codes `rivals-recalled` and `crew-destroyed`.
- The new fire cause `crewmate`.
- `car-held` now sends `tick` when `car:rivals` holds: text.test's numbers check fails until its line takes `{tick}` or says why not.
No refusal code is new or retired.

**Fixtures.** `sim/Suborned.Sim/Fixtures/bal-loud-x4.script.json`, `bal-rb-ff.script.json` and `bal-rbb-ff.script.json` are Motive's probe plans composed on this engine (`bun tools/sim/fixtures059.ts`). Re-compose them when the engine moves, as DP1's drive session is re-recorded.

## 0.33.x addendum (Latch: on top of your 0.33.0 port). Engine 0.33.1: cars are solid, Vale at his car's door, the official car's heading, a Drive runs to its stand (brief 041, Sky playing 0.2.122: "characters are walking through cars"; Fable's Drive question)
One bump. The rulebook moved (`car.footprint` with values `car-len-cm` 473, `car-w-cm` 182, `car-clear-cm` 50, `car-reach-cm` 100; `cancel.driveCar` `drives-on`), so the inputs hash moves, and walks round the cars move many routes (route diff: briefs/059/keel/route-diff-0.33.1.txt). No site, mission or text change. `suborned-sim cars` prints the cars standing at the start, what lies on their ground, the crowd legs that would cross the cars' other places, and security people sharing one spot.

**Sky's shot (cars-walk-through.png): two causes, both fixed in the engine.**
- The heading. `Game.CarHeading()` returned null at the pickup, so the host fell back to its own rule (HostViews.Figures.cs: across the line from the car to `d:Meet driver`). Since 0.32.0 the driver stands 2.7 m *along* the official bay from the car, so that rule laid the car across the bay, rear on the kerb by the official door. `CarHeading()` now always answers (never null): `car.blockFacing` (0, -100), the bay's long axis, at the pickup; along its drive while it drives; `car.serviceFacing` in the service bay; along the road at the drive's mouth; the way it was going where it stopped. Your `CarFacing()` path already prefers it, so nothing to change; the `d:Meet driver` fallback is dead code now.
- The walk. The walk grid never held cars. Now every simulated car that stands (the getaway car at its stand; Vale's car at the pickup, parked round the back or stopped; the rival car at its stops, parked, waiting or disabled; the range's cars; wrecks too) blocks the walk cells whose centres lie in its footprint (473 x 182 cm, centred on the car's point, along its heading) grown by 50 cm. While it drives it blocks nothing. The figure inside the car in the shot was the crowd's guards at `official-entrance`, which the car drawn across the bay covered; along the bay the car stands 1 m clear of that door.

**Reads for the host (all read-only, never hashed).**
- `Game.Walk` (a `WalkGrid`): the site grid seen with the cars standing now. Same calls as `Grid`: `Free`, `FreeCell`, `NearestFree`, `NearestFreeAny`, `LineFree`, `Nearest`, `Route`, plus `CarCovers(p)` (free ground a car covers) and `RouteBeside(a, b)`. Use it, not `g.S.Grid`, wherever the host tests or previews a walk: the click's free test and walk preview, `HandoverHost.ActPlace` (its `s.Grid.Route` / `NearestFree` / `Free`), `HostViews.GiveWay`'s free test (Suture's cause B), the bot's walk previews (BotRun). `S.Grid` stays the site's own ground (cells, floors, sight).
- `Game.StandingCars()`: the cars standing now, `(Id, At, Heading)` in the order car:getaway, car:official, car:rivals (the range's by id). `Game.CarBodyCm(id, p)`: cm from p to that car's body (0 on it, -1 when it isn't standing).
- `Game.AtGetaway(p)`: Board's own test. "At the car" is now within `car-reach-cm` (100 cm) of its **body**, not `at-reach-cm` (150 cm) of its centre (walkers can't reach the centre any more). `stand-at-getaway` answers `cm` 100 now. In `ActPlace`, board's "already there" test (`G.Dist(op.Pos, st) <= 100` on the stand's point) never holds now: use `g.AtGetaway(op.Pos)`.

**What moves on the street.**
- A move onto a car's ground (a click on the car, `move` to `d:Getaway bay`, `d:Meet driver`) ends beside it, on the side the agent comes to: a door (its long sides) for a point along the car, an end only for a point by that end (`d:Meet driver`, 34 cm off the official car's tail, ends behind the tail). No step in. The order's `To` is unchanged; the walk's end is the side point.
- A walk an engine person makes *to* a car (the step in) ends at that side and then steps to the car's point; a walk out of one starts at the free cell nearest. The getaway car's step-out puts the agent at the free cell nearest its seat (0.33.0: the car's point).
- Vale: walks from the official door to his car's **kerb-side door** (the car's right: -x in the official bay) and waits there (`ValeAt.Pickup`), not at the car's point; `ValePos()` is that door until he is in (`InCar`: the car's point; hide him then, as before). Seized at the front (`Taking`) he stays at the door; in the back round the back (`Taking` with the pickup round the back) he is at the car's point.
- A disabled official car: the driver stands on its road side, Vale on its kerb side, across its heading (0.33.0 put them 1.5 m along +y, which in the official bay is along the car, under its nose and tail).
- The crowd's routine legs go round the cars standing when each leg sets out (read at the start of each tick): the legs built at load go round the start's two cars (on 0.33.0, 45 of the 260 legs walked through one of them, from 2 to 6 m of each: the café's and the Office's traffic past the square's bay); after a car moves, a leg that would cross a standing car is walked again round it. A leg already under way when a car stops on it is not re-walked (the rival car's 8 s stop at `rival-drop` lies across 54 legs; a few people may cross it then).
- A walk that was made before a car stopped is not re-walked either (an agent mid-walk when the getaway car arrives at `service-drive` may pass over it).

**Cancel of a Drive (Fable's question).** 0.33.0 had no line for `drive-car`: the cancel took the default (`lost`), cleared the driver's order and left the car driving for ever (its landing never ran: `GetawayDriving` stayed true, Board refused, the car frozen at its end). Now a Drive under way is no cancel: `ActivityOf` for `drive-car` (the driver) and `ride` (the others in the car) gives `CanCancel` false, `CancelCost` **`drives-on`**, `CancelText` "the car drives on to {stand}: it stops only at a stand"; a `cancel` order is refused **`car-driving`**. Hide Cancel on the ring and HUD for those (as for any `CanCancel` false).

**Codes for Fable** (text.json, not edited here).
- New refusal: `car-driving` (a cancel of the Drive under way, by the driver or a rider). `Answer.Codes` lists it.
- New cancel cost: `drives-on` (the car drives on to the stand it was given).
- Changed: `stand-at-getaway`'s `{cm}` is 100 now and measured from the car's body ("within 1 m of the car").
- No new or retired log code.

**The "x2" over Vale's car.** Two (up to four) security people on one spot, all crowd: from 5:11 to 5:30 the shift guards p-records-office-29, -39, -61 and -74 stand on `official-entrance` (7625, -3000) together (the relief comes early on its leg and waits on the point); from 5:19 to 6:13 the relief guard p-records-office-73 stands on `office-entrance`, the cast guard's post (Suture's finding). The engine puts everyone at a stop on the stop's point; separate stands are Pulse's (handover.people.json), the drawing's spread is yours (`GiveWay`).

**Test only.** None new.

## Engine 0.34.0: the cell walks faster, a run is noticed not punished, hiding in a held car loses, the square's two rules, the patrol in the yard (brief 067, Sky playing 0.2.122; Motive motive-values.json and motive-square.md; Pulse, Vantage and Splice in briefs/067)
One bump. The rulebook moved: values `op-speed-cm-s` 180, the new `people-walk-cm-s` 140, `op-run-speed-cm-s` 360, `carry-untrained-cm-s` 90 and `mod-running` 375; `cell.runAlert` retired; `crowd.walk`, `attention.run`, `ai.noStall`, `fight.unguarded` and `fight.holdRule` new. The site moved too: the patrol sweeps the service yard, the square's planters are gone, Vale's car points moved. The people file moved (Pulse's waiting points, `beside`). So every inputs hash and most routes move (route diff against e17d8ca7: `briefs/067/keel/route-diff-0.34.0.txt`). Scenarios PACE-SPLIT, RUN-*, K3-*, SQ1-WALK, SQ1-STEP, SQ2-NO-STALL, FACT-PATROL, POST-BESIDE and DOOR-KERB (`sim/Suborned.Sim/ScenariosPace067.cs`) show each rule.

**The walk split (M067-B1): what your previews read.**
- `R.CellSpeed` (cell.speed) is the cell's walk only, now 180 cm/s (was 140).
- `R.PeopleWalk` (crowd.walk, 140) is the city's walk. It covers the summoned partner, reporters and tellers walking to a phone, a witness walking to a body, the crowd walking back from shelter, and the rounds bake's pace.
- The other paces: `R.RunSpeed` 360, `R.CarryUntrainedSpeed` 90; the Enforcer carries at `R.CellSpeed`.
- Every engine read that quotes an operative's walk now uses the cell's pace:
  - the move quote (`WalkS`, `RunS`);
  - the approach card (`WalkS`);
  - `QuoteCarry` (`PaceCmS`, `S`);
  - the fire order's walk into reach;
  - `ActivityOf` (`TicksLeft`).
- Your own previews that divide by `R.CellSpeed` show 1.8 m/s with no change on your side:
  - HandoverHost's intent time and `ActWalk.Seconds`;
  - `HostHold.WalkSeconds` and `HostHoldStatus.WalkLeftS`;
  - HostSight's 4 s ahead;
  - HudModel's and HostTakedown's "walk only · m/s" rows;
  - HostDrive's pace and DriveCheck.
- None of those may stand in for a person of the city, who walks at `R.PeopleWalk`. The engine's poses are the truth for people.
- `VisitWalkTicks` (the summoned guard's walk to Inez) is the city's pace. It is unchanged, so Inez's call window is as on 0.33.1.

**The run (M067-B2).** A run is seen and suspicious, never an Alert on its own.
- `cell.runAlert` is retired and `R.RunAlert` is gone: drop any read of it.
- `MoveQuote.AlertIfRun` and `SeenQuote.AlertIfSeen` are now the Alert as it stands: a run never raises them.
  - HostFight.cs:418-421 still prints a run as "seen by … (Alert N)". The words should now say that a watcher notices it (Fable words it, Glyph cues it).
  - `RunSeenBy` and `Seen` still say who would see it.
- Which watchers count it: a watcher that keeps an attention clock (a live camera, the door guard not turned to your side, the patrol, the response's officers and robots).
- What they do:
  - On the run's first tick in its sight, the watcher's clock jumps to the noticed mark: `attention.noticed`, "… notices the {op} running in its sight.". Show its noticed cue on that tick.
  - After that, each tick of the run in its sight counts as standing still does, so it turns wary and reports as for a loiterer.
- `run.seen` still logs the sighting once a run, without "a found fault. Alert N".
- Unchanged: a run is free at Alert 2 and above, when it is loud, and under fire; from Alert 2, ordinary people who see a run remember it (Sky's 054 call).

**The held car (Motive M067-K3, as amended in motive-square.md §4).**
- **(a) The walk to the car.**
  - When: the safe fight, with Vale in the getaway car, the car held (`CarHolders()` not empty), and no operative standing outside it.
  - The rivals up and not broken (not the blocker) walk to the car, firing as they walk (`rivals.for-car`). The first to come within 1 m of its body takes Vale (`rivals-reach-vale`).
  - An operative standing outside again, or the car no longer held, sends them back to cover (`rivals.back-to-cover`).
  - A free car with the cell aboard waits for Go: nothing walks to it.
  - New read: `Game.RivalsForCar`, the ids walking for the car now (sorted). Draw them as coming for the car (Glyph's cue).
- **(b) The cell down.** The tick the last operative goes down, in the car or not, the mission fails `cell-down` ("The whole cell is down."). The fail camera is the shot that downed the last to fall: why `shot`, the shooter first, then the operative (as for `enforcer-down`).
- **(c) Go needs a driver.**
  - With only the downed aboard, Go is refused `car-no-driver` ("nobody aboard can drive: only the downed are in the car"). `GoQuery()` answers the same.
  - Once a standing operative is aboard, any standing operative may give Go from anywhere.

**The square's two rules (M067-SQ1, SQ2).**
- **SQ1, the hold is entered only on purpose.**
  - An answer to fire never ends within 8 m of the car or on its exit lane. That covers a rival's step out of a line or to cover, and a police body's cover or close. When nothing outside serves, the body stands where it is.
  - A walk to a post goes round the hold: the rival crew walking to its spots, police and the response unit walking to their stands. On screen they bend round the car at about 8 m.
  - New log codes: `car.held.<how>` marks the first tick each body holds the car at a stand. `<how>` is one of `blocker`, `press`, `advance`, `answer`, `post`. These are log only: the holder list (`CarHolders()`, Go's `car-held` ids) is unchanged.
- **SQ2, no stall.**
  - A rival holding with nobody in his reach for 1 s advances again (`ai.advance`, "{id} has had nobody in reach for 1 s: he advances on the {op}[ to {cover}].").
  - He goes for an operative he has cause to fire on: to the cover that shields him from that operative, else to the first point of the walk to him that has a clear line to him.
  - Police and the blocker hold their posts.

**The patrol in the service yard (brief 067 A).**
- The sweep runs between patrol-a (61.25, -7.25) and patrol-b (76.25, -7.25), facing west toward the wing's east face and the service door. Rerouted, it waits at (46.25, -9.25) facing north.
- It never sees the public door: `SweepSeesEntrance(t)` is false at every tick. The new reads `Game.SweepCoversEntrance` and `Game.PatrolWatchesEntry` (with `SweepSecondsOnEntry`; also `suborned-sim facts`) are false on this site.
- Anything you time to the sweep at the front door has nothing to show there now; the yard is where the patrol matters.
- The debrief never says `entry-timed-between-sweeps` on this site. It still says `patrol-diverted` when the patrol was rerouted at the Enter.
- `patrol.rerouted` and `patrol.returns` now name no place: "leaves its sweep", "is back on its sweep".

**Pulse's posts (brief 067 L).**
- A relief waits at a point beside a staffed door and steps on at the change (the people file's `beside`).
- `PostOf` reads that waiting point as her post, so she keeps her check-in marks (guard 73 from 5:30).
- Guards posted at the Office's doors face out of them, toward the kerb.
- At the change (5:30:00) a standing person can move up to 1.2 m in one tick. Ease that step (Pulse's note to you).
- New read: `Game.FacingNow(id)`, the way a person faces now as the engine reads it.

**Harness only.**
- `Game.HarnessStepLimit` (static, 0 = off) bounds every game a harness makes; past it, `RunawayException`. Never set it from the host: a played session has no step limit.
- `check` and `batch` take `--timeout s` (default 300).

**Codes for Fable** (text.json, not edited here).
- New log codes: `rivals.for-car`, `rivals.back-to-cover`, `ai.advance`, `car.held.blocker`, `car.held.press`, `car.held.advance`, `car.held.answer`, `car.held.post`, `fail.cell-down`.
- New fail code: `cell-down`.
- New refusal: `car-no-driver`. `Answer.Codes` lists it.
- Changed texts: `run.seen` (no found fault, no Alert), `attention.noticed` (also "… running in its sight"), `patrol.rerouted` and `patrol.returns` (no "entrance sweep").
- text.json still says the patrol watches the square or the door in these places:
  - `patrol.about` and `watched` ("sweeps the square", "reaches the door at times");
  - line 1123 ("A patrol sweeps this door … N shows when it comes round") and line 1851 ("The patrol sweeps past the door");
  - line 2247 ("Mind the entrance");
  - the watch reads on the patrol, lines 2088, 2089 and 2095 ("I know when it sees the door", "The door is clear between sweeps");
  - `patrol-returns`'s trigger, line 735 ("its entrance sweep").
- No log code is retired.

**Retired.** `cell.runAlert` (`R.RunAlert`) and the `runner-seen:` Alert cause.

**Fixtures.**
- `bal-rb-ff` and `bal-rbb-ff` are now Motive's rb-rr and rbb-rr (the same plays, with the Tech's Reroute at 5:00).
- New: `sq-route-a-board` and `sq-ra-h3`, the square's reference plays.
- Re-compose them with `bun tools/sim/fixtures059.ts` when the engine moves. DP1's drive session is re-recorded on 0.34.0.

## Engine 0.34.1: an advance closes on its operative (brief 067; Aster's ruling within M067-SQ2)
No API change: a rival advancing on an operative now takes a cover only if it is strictly nearer him by walk (and inside or nearer the rival's reach of him), else walks to his stop point of him taking no kite step on the way (`Hostile.Closing`, hashed only when set), else holds, so the rivals no longer shuffle between two covers (ra-h3's rival-3, cv-030 and cv-029); the rulebook's `ai.noStall` text moved, so the inputs hash moves (DP1's drive session, CP-LOUD and CP-SAFE are re-recorded; scenario SQ2-CLOSES; route diff from 0.34.0: `briefs/067/keel/route-diff-0.34.1.txt`).
