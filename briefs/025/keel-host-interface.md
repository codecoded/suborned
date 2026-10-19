# Brief 025 · The driven agent: host interface (Keel → Latch)

Engine `suborned.sim/0.9.0`. Rulebook `cell.drive` (EXPERIMENTAL). Code: `sim/Suborned.Sim/GameDrive.cs`.

## Each tick, for the agent you control
Do this before `StepOnce`, once per tick, never per frame:

```csharp
var o = Game.DriveSample(OpId.Scout, new Pt(xCm, yCm), shiftHeld, faceDeg); // block frame, integer cm; face 0..359
var a = Game.Issue(o);   // logged like any order; a.ok false: a.code says why (agent-down, enforcer-inside, in-car, unknown-verb, no-place)
```

- **What to send:** the controller's position rounded to centimetres. Send a sample only when the position, Shift or the facing changed since the last tick; a still agent sends nothing.
- **When the first sample lands:** it takes control. A click walk in progress ends.
- **Busy agents:** never gate a sample on `Busy`. The engine decides whether the agent is pinned or the action is cancelled, under `cell.drive.onBusy`.

## After the step: read the pose and pull back

```csharp
var p = Game.DrivePoseOf(OpId.Scout);
// p.Driven     false: control ended (down, inside, in the car, carried, or a click move). Leave the follow mode.
// p.Pos        where the engine has the agent (cm). If your controller is more than ~5 cm off, move it here.
// p.Corrected  this tick's sample was not taken whole (pace budget, a wall, the 40 cm gap, a busy agent pinned).
// p.Running    moving at a run this tick (false while carrying Vale or a body, or while the Tech holds the loop, even with Shift held).
// p.Face       the last facing you sent.
```

- **Drawing the agent:** draw it from your controller, not from the eased engine position, so input feels immediate.
- **Other figures:** ease them as now.

## Release

```csharp
Game.Issue(Game.DriveRelease(OpId.Scout));   // on leaving follow mode or switching agent
```

- **What release does:** the agent stands where it was left, under `onRelease stand`. Under `resume`, it walks on to the click destination that control took over.
- **What else ends control:** a click move ends it too. You don't need to release first.

## Acting while driven

- **Verbs:** issue them as now (E, the right-click card). They are judged from where the agent stands.
- **No auto-walk:** while driven, don't send a walk to reach. An action out of reach is refused (`too-far`); show that.

## How your mover should collide

- **Use the engine's walk grid:** collide against `Game.S.Grid` (`Free(Pt)` and `FreeCell(i, j)`, 50 cm cells), not Unity colliders. Then the engine rarely corrects you, and the agent never feels invisible walls.
- **How the engine resolves a step:** x first, then y, each axis stopping at the first blocked cell. Mirroring that gives identical sliding.
- **Pace:** walk 1.4 m/s, run 2.8 m/s. While carrying, use the carry pace. While a run is refused, walk.
  - The engine allows two ticks of pace at most in one sample, plus 2 cm.
  - Over time it holds you to the pace exactly.
  - Frame jitter is fine; faster isn't.
- **People:** the engine won't let a step close inside 40 cm of a standing person (`gap`). Your mover can do the same check, using the positions the engine draws them at, to avoid the pull-back.

## Playground switches

These go in the run's start state and are hashed into it. Set them only from the playground panel.

```csharp
new Start { DrivePace = 1500 /* per mille, 250..4000 */, DriveOnBusy = "hold" /* or cancel */, DriveOnRelease = "resume" /* or stand */, DriveGap = 0 /* cm, 0 off */ }
```

## Replays, saves and the fail camera

- **Nothing new to do.** Samples and releases are orders: `Script.Write` saves them, and `Engine.Replay` and the fail camera's rebuild read them back.
- **The proof:** scenario DP1 replays a recorded host-shaped session (`sim/Suborned.Sim/Fixtures/drive-session.script.json`, embedded in the DLL). It runs in `game:check`'s `-runScenarios` like every scenario, so the built player proves the replay as soon as the 0.9.0 DLL is in.
- **A live session:** once your mover exists, save a driven session from the game and run `bun run sim play <session.json>`. It must say "the replay matches its record". Then copy it into `Fixtures/` as a second proof, or tell me and I'll do it.

## Also new in 0.9.0

`CarHeading()` returns the site's `car.blockFacing` while the official car is parked at block-spot, which is (0, -100): along the bay's kerb, nose the way it pulled out. Previously it returned null there.
