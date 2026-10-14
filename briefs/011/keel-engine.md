# 011 A · The engine, headless (Keel, 2026-10-15)

**What changed**
- `sim/Suborned.Sim/`: the engine.
  - A netstandard2.1 class library, C# 9, with no package references. Unity loads the same DLL. `Engine.FromTexts` lets a host pass the three input texts itself.
  - Integer state at 10 Hz: centimetres, ticks, dollars. There are no floats, no clock and no unseeded random anywhere in it.
  - It reads `contracts/rules/rulebook.json`, `contracts/people/people.rules.json` and `sim/data/handover.site.json`. Anything unknown is rejected: a condition or effect kind, a key, an order verb, or a ref that doesn't resolve to an integer.
- `sim/Suborned.Sim.Tests/` (xunit, .NET 10): 113 tests.
  - Each scenario passes.
  - Each scenario plays twice to identical checkpoint hashes.
  - Each scenario replays from its start state and order log to the same hashes.
  - An order script round-trips through the order format.
  - The rules' golden cases agree with `rulebook.test.ts`.
  - The 3D occluder case (N13), integer square root, cones, and unknown = reject.
- `sim/cli/`: `dotnet run --project sim/cli -- play <S01…|N08…|script.json> [--json]`. It prints the outcome, the event log, the checks, the debrief, the start hash, the final hash and the checkpoint hashes, and says whether a replay is identical. Other commands are `list`, `script <id>` (a scenario as an order script), `perf [runs]` and `probe`.
- `tools/sim/site.ts` (`bun run sim:site`) writes `sim/data/handover.site.json`. It holds:
  - the walk grid from `tools/plan/walk.ts` blockers and the block's solids, at 0.5 m;
  - sight occluders with heights;
  - the mission's points, timeline and window;
  - the cast;
  - the engine's own points, each with its reason.
- `tools/sim/sim.test.ts`: checks the site is fresh, then plays S10 twice and an S20 script through the CLI. The CLI tests are skipped where there is no `dotnet`.
- `contracts/rules/`:
  - Motive's patch #4, #6, #9, #10 and #12, plus his other findings 3 (the "?" logic) and 4 (incidents).
  - Sky's switch: `preview.knownBy = ["scout.listen"]`.
  - New `cell`, `security` and `perception` sections hold the top-down test's [B] numbers (Suture M-2, M-3). Each value is tagged.
- `package.json`: added `sim:site` and `sim`.

**Order format** (`suborned.sim.script/1`; Waymark writes route scripts in it): `{"tick": 520, "op": "fixer", "do": "turn", "approach": "help", "target": "waitress"}`.
- Verbs:
  - `move` (`to`: a place id or `[x, y]` in cm);
  - `watch` and `listen` (`target`: a role);
  - `turn` (`approach`, `target`);
  - `call` (a turned role);
  - `loop` (`entrance`);
  - `reroute` (`patrol`);
  - `mislead` (`rivals`);
  - `fire` (`guard`, `waitress`, `driver`, `councillor`, `patrol`, `security`, `rivals`);
  - `enter`, `take`, `extract`, `withdraw`.
- A rejected order changes nothing and is logged with its reason. The order log is the replay.

## The 28, rebased
Every scenario keeps its intent. These changes apply to all of them:
- **Actions take their rulebook time and land at its end.** The prototype landed every action at once. A call still lands at once, as the prototype's spill did.
- **Watch gives the routine, Listen gives the need and temperament** (Sky's ruling).
- **Inez is an Idealist:** S24's Pay is refused and remembered (`$100,000` kept, +2 T, card "?" then "refused"). S25's Pressure is +4 T.
- **The guard's walk is 45 s** (`mission.json` windows).
- **The mission is over at 8:00** (§07, M-1). At 8:00 the fail is "rivals reach Vale" if the rivals are present, otherwise "Vale leaves in the official car".
- **Health is the [B] numbers** (M-2).

Places are the civic block's. The Scout listens from `beside-inez`, because the Courtyard is 3.5 m from her and Listen's reach is 3 m. That makes S22 hold on the real ground.

| | change, with its reason |
|---|---|
| S01, S03, S04, S06, S07 | Each prototype "FIX" became orders or a hashed start state. S01 loops the camera by orders. S03, S04, S06 and S07 start with Inez turned. |
| S05 | The shot now lands at 3:38, after the rivals park (3:10). So the plan tips the rivals off first, as a player would. It still wins narrowly: the Enforcer is hit twice on the way out, 38 s before the seal. |
| S10 | The call waits until the sweep covers the door, so "covers at the first opening, then waiting makes one" still holds on real timings. |
| S13, S15, S17 | The Enforcer shoots from `BesideDoor`, out of the guard's cone and both cameras. The prototype stepped 5 units sideways. |
| S17, S19 | The prototype teleported the Enforcer. Here he walks onto the rivals' way in (S17), or to the north street where the response unit comes in (S19). |
| S20 | Route B adds a Listen at the car (42 s): the need now comes from Listen, not Watch. |
| S21 | Starts with the driver turned. With real action times a natural turn lands at about 3:20, and two blockades (2 × 2:38) no longer fit before 5:32. |

**N-cases done:**
- N8: withdraw.
- N9: the purse. $80,000 fits; a Mercenary Inez's $70,000 Pay leaves the driver unaffordable.
- N13: a slab above eye height doesn't block; a wall does.
- N14: the card's "?", the step, the refusal.
- N15: K1 memory with the face, K2 loyalty, K8 line items, and the Trail ledger.

**Not yet:**
- N10 and N11, the final fight (week 4).
- N12, people stepping their routines. Today the only witnesses are the four cast roles; the crowd comes next.
- The between-missions events (K3–K7, K9), investigators and watcher logs. They are in the rulebook, not yet in the engine.

## Readings I made (owners may overturn)
1. **Per-shot Alert.** §05 says "any gunfire → 2" and §06 [B] says "+1 A a shot". The engine follows §06 as Motive asked, and the rulebook's `alert.rule` records the conflict.
2. **The response unit's entry.** §07's "west street" is the north street: the real block is built up on its west side.
3. **Where the rivals and the response unit hold.** At the end of their run they face back the way they came, as the prototype's did.
4. **One door guard for the whole mission.** The guard shift change at the 5:30 chime (Waymark's guard window, 2:24–5:30) isn't modelled.
5. **Vale follows the mission's timeline, not his routine** (Suture B-2: the mission's events win for pinned roles). The driver's Help must land by 5:40.
6. **The rivals jump to the service lane at 6:10** when Vale goes there, as in the prototype. They don't walk there.

## For Glyph (spec gaps closed)
- `Quote` now has:
  - `Reports` ("yes", "no" or "?") and `AlertIfReported`;
  - `Witnesses`, the people within 13 m;
  - `WalkS`.
- `Game.Ledger` lists every Trail, Alert and standing change with its cause, for "why is the investigation active?".
- `Game.HoldsFace(person)` is the dossier's face bit.
- `Person.Suspected` carries the briefing's need from 0:00 (Sky's ruling). It unlocks nothing.

**Roads (Sky):** the grid comes from the plan's walk blockers, which leave every carriageway walkable. Operatives cross anywhere, and crossing costs no Trail and no Alert.

**Evidence**
- `dotnet test sim/Suborned.Sim.Tests`: 113 passed, 0 failed.
- `dotnet run --project sim/cli -- play S10 --json`, run twice: `success`, final hash identical, 31 checkpoints, `replayIdentical: true`.
- `perf 50` (Release, Studio M5 Max): **50,053 sim-s per wall-s**, which is 6.2 ms for a whole route A run.
- `bun test`: 613 pass, 0 fail (the full suite, before the last site edit). Since that edit: `bun test tools/sim contracts/rules` 33 pass, and `bun run typecheck` exits 0.
- The Release DLL is 146 KB, netstandard2.1, built with 0 warnings.

**Next owners**
- **Waymark (B):**
  - write the route scripts in this format (`dotnet run --project sim/cli -- script S10` is a worked example);
  - adopt the engine's points into `mission.json` (O-6): `cabinet`, `beside-inez`, `rival-park`, `rival-entry`, `security-entry`, `patrol-a`, `patrol-b`, `patrol-reroute`, `block-spot`, and the two cameras;
  - pin the cast.
- **Latch (D):** load `sim/Suborned.Sim` (netstandard2.1) with `Engine.FromTexts`, feed it orders, and run `Scenarios.All` in Unity.
- **Redline:** review D4 (no floats in state) and P1.
- **Suture:** re-baseline the 28 against `play --json`.
