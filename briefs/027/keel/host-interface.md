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
