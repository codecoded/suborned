# Brief 024 · Free third-person control · feasibility (Keel)

Keel, 2026-10-19. A feasibility answer only: no engine code has changed. It is read against engine 0.8.2 (`sim/Suborned.Sim`) and the play host (`unity/Assets/Suborned/Runtime/Play`).

This version replaces my first answer (quantised `steer` orders). Sky wants free movement: Unity moves the controlled agent with a character controller and hands the engine the agent's position at each tick.

**Verdict: it can be done, and every run still replays exactly.**
- The positions Unity hands over become the input, and the engine logs them like orders.
- Replay reads the logged positions back. It never re-runs the character controller, so float maths in Unity can't reach the engine.
- The engine checks every position against the walk grid and pins illegal ones to the nearest legal point, so driving can't break a rule.
- Planned missions (no driving) keep "same orders, same result" unchanged.

## What the engine does today (the facts this rests on)
- The engine runs at 10 Hz. An order is stamped with its tick and must match the engine's tick (`GameOrders.cs:16`). Every order, taken or refused, goes into `Orders`.
- The session save (`Script.Write`), replay (`Engine.Play`/`Replay`), the fail camera (a second game rebuilt from the start state and the order log), `SimCheck` (the Unity match) and `tools/missions/check` all consume that one order list.
- An operative's movement is a `Path` advanced in whole millimetres each tick: walk 140 mm/tick, run 280 mm/tick, carrying slower. Positions are integer centimetres.
- The walk grid is one street-level layer of 50 cm cells. `Grid.LineFree` checks a segment every 10 cm.
- Operatives have no facing in the engine, and nothing reads one. There is no crouch.
- People don't block operatives; only the grid does.
- A run's suspicion check (`RunAttention`) runs every tick of a run. Witnesses count once each per run, and a security sighting gives a found fault (Alert 1) once per run. Today a run is one `run: true` move order.
- The walk-then-act queue (`pending[4]`) lives in the host. The engine holds only `op.Walk` and `op.Busy`.

## 1 · The engine change

**The input record: a `drive` sample, logged as an order.** I recommend it goes in the existing order list, not a separate track.
- Format: `{"tick": t, "op": "scout", "do": "drive", "to": [x, y], "run": true, "face": 0..359}`.
  - `to` is integer cm, the same key `move` uses.
  - `run` is the gait: absent means walk, true means run.
  - `face` is whole degrees, for the view and the fail camera. It's hashed; nothing reads it yet.
  - A gait the rulebook doesn't name, such as `crouch`, is rejected (unknown = reject).
- Because a sample is an order, the save, replay, fail camera, Unity match and the TS checker's script type all carry it with no new plumbing. "Any run replays from its start state and the player's orders" stays literally true.
- The host sends one sample per driven operative per tick, just before `Step`, and only when the position, gait or facing changed. An agent standing still sends nothing.

**The "driven" state.** `Op.Driven` begins with the first `drive` sample. While it is set:
- The engine takes `op.Pos` from the validated sample (§2) instead of advancing `op.Walk`. The agent is then simply where it is: sight, witnesses, Trail, reach, the tether, the run check, the guard's view and the cameras all read `op.Pos` as they do now.
- A run sample runs the same `RunAttention`. A run lasts from the first run sample to the first walk sample or the release of control, however often the direction changes. The rulebook's "once per run order" becomes "once per run" (Motive to sign off; click runs are unaffected).
- Carrying works as now: a carried operative, and Vale beside the Enforcer, follow `op.Pos`. A run sample while carrying is held to the carrying pace.
- Control is released with `{"do": "drive", "release": true}`, sent when the follow view is left or another agent is taken. A click-move or a new action on the agent also ends driving.

**How the agent's other orders interact.**
- **A click-move in progress** ends when driving starts. Every engine rule is defined on the current position, so nothing needs undoing.
- **The host's walk-then-act queue** (`pending[op]`) is dropped.
- **An action in progress** (`op.Busy`: a turn, a loop, a takedown, a carry): I propose that moving more than about 25 cm cancels it at the rulebook's cancel cost, the same as the `cancel` order, logged as a cancel. Smaller movement is pinned to the agent's spot until the action lands. The alternative is that the agent can't move until the action lands. **Sky decides.**
- **Actions given while driving** (right-click and verbs) work as now, from where the agent stands. An action out of reach is refused (`too-far`); the host doesn't auto-walk, because that would take control away.
- **On release:** the agent stands where it was left, and nothing resumes. Resuming is possible (the host re-issues the dropped move from the new position, as an ordinary logged order), but an agent wandering off on its own after you let go is the surprise third-person players hate. **Sky decides.**
- **Refused while driving:** down, inside the Office, in the car. These use the existing codes (`agent-down`, `enforcer-inside`, `in-car`), with no new ones.

## 2 · Validation: no impossible moves

Each tick the engine checks the sample's position Q against the engine's current position P.

1. **Speed:** the distance from P to Q must not exceed the gait's cap plus a small tolerance (rulebook `cell.drive.slackCm`, about 2 cm).
   - The caps are those the engine already uses: walk 140 mm a tick, run 280 mm, carrying slower, and walking whenever the run refusals apply (carrying Vale or a downed operative, the Tech on the loop).
   - A sample beyond its cap is cut back along PQ to the cap. That also handles teleports: a jump of any size moves the agent at most one step.
2. **Walls:** the segment from P to Q must stay on free cells.
   - A diagonal squeeze between two blocked corners fails, as A* refuses it.
   - Otherwise the engine takes the furthest free point along PQ, at 10 cm steps, then tries sliding along x alone and y alone.
3. **Off the grid:** a sample on a blocked cell is handled by step 2; the agent never stands on one.

**The engine's position wins.** The host reads `op.Pos` back after the step and pulls the character controller to it if they differ by more than a few cm.
- The engine logs the correction once per span (`drive.held`), not per tick, so a clamp doesn't spam the log.
- Clamping is a pure function of the logged sample and the state, so it replays the same.
- Clamps should almost never happen in fair play. **This only holds if the controller collides with the same grid.** Rasterising the plan into 50 cm cells means Unity's colliders and the grid disagree by up to half a cell. If the controller uses Unity colliders, players will feel invisible walls and get pulled back.
- So Latch's controller should be a kinematic mover that asks `Sim.Grid` what is free (it is already in the DLL), not Unity physics. Unity colliders can stay for the camera and the look.

**What validation doesn't cover:**
- Turning on the spot faster than a person could. Facing feeds no rule.
- Driving through people. People don't block anyone today; see §4.

## 3 · Replays, the checker and the Unity match

**Replays:** no new mechanism. The save, `Engine.Replay` and the fail camera already rebuild from the start state and the order log, and drive samples are in that log.
- On replay, the fail camera shows the driven agent at the logged positions and facing, eased like everyone else.
- `Hash.State` adds the driven flag, facing and run span only while driving, the way a run adds bytes today. Every existing checkpoint hashes as before.

**Record size:** at most 10 samples a second while moving, each about 70 bytes.
- That is about 40 KB per minute of driving, or about 400 KB for a mission driven end to end.
- `Orders` holds about 6,000 entries at most, which is fine.
- If saves need to be smaller, the samples can later be packed as deltas in the save file without touching the engine.

**Routes and checker:**
- Waymark's routes and `tools/missions/check` need no change. Routes stay click scripts, so planned missions are untouched.
- The TS `Order` type in `tools/missions/check/engine.ts` gains `face` and `release`, so it can read a driven save.
- The plan checker (`bun tools/plan/cli.ts`) checks the plan, not runs, and is unaffected.

**Unity match:**
- Engine scenarios with synthetic drive tracks run in both runtimes, as all scenarios do.
- New scenarios:
  - straight walk;
  - zig-zag run counts once;
  - a push into a wall slides;
  - a squeeze between corners is refused;
  - a teleport is cut to one step;
  - running while carrying Vale is held to a walk;
  - the Tech drives away and the loop ends;
  - moving cancels a busy action;
  - release leaves the agent standing;
  - an unknown gait is rejected.
- The real proof needs one more check: a session driven live in Unity and saved, replayed headless with `dotnet`, with every checkpoint matching. Add it to `game:check` as a recorded fixture.

## 4 · Size, and what Sky must decide

**Engine (Keel), about 2 days including about 10 scenarios, about 450 lines:**
- `Model.cs`: `Order.Face` and `Order.Release`; `run` allowed on `drive`; `Op.Driven`, `Op.Face` and the run span.
- `GameDrive.cs` (new): apply a sample, the speed cap, the wall test and slide, the busy cancel, release, the run span and the `drive.held` note.
- `Game.cs`: in the operative loop, a driven op takes its sample instead of advancing `Walk`.
- `GameOrders.cs`: dispatch; `move` and actions end driving.
- `GameCombat.cs`: count runs by span rather than by order.
- `GameCancel.cs`: release and cancel.
- `Hash.cs`: the drive bytes.
- `Rules.cs`, `contracts/rules/rulebook.json` and `rulebook.schema.ts`:
  - a `cell.drive` block: the gaits, `slackCm`, the busy cancel radius, the cap per gait drawn from the existing speeds, and the needs, changes and witnesses as for `move`;
  - the "once per run" rewording.
- `ScenariosDrive.cs`.
- `tools/missions/check/engine.ts`: two keys on the `Order` type.

**Host (Latch), about 3 days:**
- a kinematic character mover against `Sim.Grid`, camera-relative WASD and Shift;
- one sample per tick before `StepOnce`, read back and pull the controller back after;
- drawing the driven agent from the controller rather than from the eased engine position, so input feels immediate;
- release on leaving the view;
- no fast-forward while driving;
- the recorded-session fixture for `game:check`.

**Sky must decide:**
1. **Design change or test.** The follow view is a test today (`HostFollow.cs`). Driving one agent by hand pulls against "commander > avatar" and "preparation > improvisation". If it stays, §10 (camera above the street) and §01's pace need a new GDD version, through Aster.
2. **Moving while busy:** cancel the action at its cancel cost (my proposal), or stand still until it lands.
3. **Release:** stand where left (my proposal), or resume the dropped order.
4. **The price before you commit.** A driven run carries the found-fault risk with no card first. Either show the risk live on the runner, or §02's "every order shows its full price" takes an exception.
5. **People as obstacles.** At shoulder height, walking through the crowd looks wrong.
   - Making people block the driven agent is deterministic: crowd positions are a function of tick and state, so it is a rule in the validator.
   - Click paths don't avoid people, so the two kinds of movement would differ.
   - Motive's ruling; I'd start with walk-through.

## For Ghost Recon-style hybrid play (plan overhead, play in third person)

None of these blocks driving one agent. Each one bounds how far the hybrid can go.
- **One flat walk grid at street level.** No decks, stairs, ledges or cover heights, so a driven agent can't climb and sight has no vertical cover. Using the deck and concourse needs a multi-level walk graph and connectors in the engine: the biggest engine item, and needed by click play too.
- **Fights resolve themselves.** "Down in n s" fire comes from deterministic hit rates and preparation. Aiming by hand has no place in the engine. Adding it means a new aimed-fire input and a ruling against "preparation > improvisation". Keep fire as an order in third person.
- **No posture or facing in the rules.** Crouch, cover posture and "seen from behind" don't exist. Stealth by posture would need new rulebook entries (Motive), not engine plumbing.
- **Plans live in the host, not the engine.** The engine holds one walk and one action per operative. Ghost Recon's plan-then-go (waypoints, go-codes, "go when the guard leaves", squadmates following the leader) needs engine-side queues and triggers. The host could fire them as logged orders, but triggers are rules and belong in the rulebook so the price can show them. This is the most useful engine work for the hybrid, and it helps planned missions too.
- **The 10 Hz tick** is fine if the driven agent is drawn from the controller. Sight and witnesses then lag the player by at most one tick (100 ms), which nobody will notice. The 10 Hz rate is a hold, and I would not change it.
- **Pause and fast-forward.** Planning overhead under pause works today. Fast-forward and driving don't mix, so the host should drop to 1× while anyone is driven.

## Recommendation
- Feasible and deterministic.
- Log each driven agent's validated position, gait and facing as a `drive` order at each tick it changes.
- Have the engine cap the speed at the gait's existing pace and hold the agent to the walk grid.
- Have the host's character controller collide with that same grid, so validation never fights fair play.
- Cost: about 2 days of engine work and scenarios (Keel), about 3 days of host work (Latch), and up to about 400 KB per fully driven save.
- Before building, Sky rules on items 1–5 in §4, starting with whether this is a design change. Motive signs off "once per run" and the people-as-obstacles ruling.
- For the hybrid, the engine work that matters most is the multi-level walk graph and engine-side plan triggers; aimed fire is the part that fights the design.
