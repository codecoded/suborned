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

## Round 2 (2026-10-15): the final fight, the city's people, Aster's two fixes, Redline's review

**The final fight (§07 [S]; N10, N11).** It starts when the Enforcer has Vale. The branch follows §07's rule: loud if any shot was fired or Alert ≥ 2, otherwise safe. The numbers are in the rulebook's `fight` section, tagged and proposed.
- **Loud:** 2 officers, +1 if Alert reached 3, +1 if a civilian was shot. They run in from the north street, the seal clock runs, and there are no allies.
- **Safe:** the rival crew ambushes when the Enforcer brings Vale within 20 m of the car. They are 5, minus 1 if the driver is turned and minus 1 if the rivals were tipped off earlier: exactly §07's 3–5.
  - A helped Inez's warning holds back their first volley by 10 s.
  - A guard who never saw the cell and was never shot runs in and fights them.
- **The volleys:** the hostiles fire one volley every 20 s while any of them sees the Enforcer.
- **The car:** it can't leave while a hostile stands within 25 m of it.
- **Results:**
  - N10 (loud) wins through 2 officers in 18 s.
  - N11 (route A, 5 rivals, warned, guard ally) wins in 41 s, 2 hits taken.
  - N11b (route B, 3 rivals) wins in 32 s.
- **For Motive:** `fight.traced` is a switch, today `true`. With it on, the fight's shots add Trail and Alert like every shot, so every Handover ends Raised or High with about +10 T, and a quiet route is never Calm.
- **The 28 keep their world.** S01–S28 run with `finalFight: false` and `crowd: false`, as the top-down test had neither.

**People step their routines (N12, N12b).**
- `tools/sim/people.ts` reads the people file and writes `sim/data/handover.people.json`: block cm, mission seconds, out on the street or not, job, home and work flags, ties.
- In the engine, each of the 122 people who aren't cast stands at the stop their routine gives, or walks between two stops, or is off the street.
- Everyone on the street witnesses (`Start.crowd`, Suture's T4 switch). Security staff are not "civilians" for §05's violence rule.
- N12b: four café customers see the Fixer clear Inez's debt. The witnessed act adds +1 T once, standing moves +1, and they remember the Fixer's face.

**Aster's fixes.**
1. **The cast now comes from M-2.** `sim:site` casts with `castRoles(..., { play: true })` from a people file, at `startsAt` 17:24:30, and fails loudly if a role can't be cast. Today the promoted file fails M-2 on the driver, guard and councillor.
   - The site and the crowd therefore come from a labelled fixture, `sim/data/fixtures/harbour-district.handover-fix.people.json` (Pulse's fix cast, sha256 77eba841…).
   - The fixture's driver is a Mercenary, so one Help no longer turns him and **§04's $80,000 purse no longer holds for this cast**. That is for Pulse and Motive.
   - S20 and N11b now turn the driver the way the card says works in one order (Pay). N09 states an Idealist driver, since it tests the purse rule itself.
2. **A refusal teaches the cell only that this approach is refused** (the card's ✗), never the temperament. No approach learns the temperament; only Listen does. S24 checks it.

**Redline's review: what changed for each finding.**

| finding | fix |
|---|---|
| E1, unknown = reject | The rules and the site are compiled at load into typed fields.<br>Every object the engine reads is key-checked. Every condition and effect value is checked.<br>Where a kind may stand is checked: cash-covers only in needs; nothing that prices in a price line; fit and orders-reached only in a Turn; no recursion.<br>Every value is bounded by its unit. Durations are at least 1 s, in C# and in zod.<br>All 15 of Redline's mutations are now rejected at load, and a test holds them.<br>`Step()` can't throw: an engine fault ends the run as `engine-error`, with the reason logged. |
| E2, money | The card's price is locked when the order is given. The cash taken is kept on the operative, and a refusal refunds exactly that. A test moves standing mid-order: 90,000 taken, 90,000 back. |
| E3, 0 s actions | Actions land on `BusyEnd <= Tick`, and 0 s values are rejected at load. |
| E4, replay records | Inputs are hashed after dropping a BOM and making CRLF into LF; Windows and Mac replay the same.<br>`script <id>` writes a record: inputs hash, `Engine.Version` with the MVID, start hash, every checkpoint and the outcome. `play` verifies a stored record and names the first difference.<br>The checkpoint hash now covers GuardReachedCafe, incidents, the ledger, debrief lines, the chain, actions, orders with their answers, log codes, the money held on orders, and the patrol's presence. |
| E5, cones | Facings are normalised to 1,024 inside `InCone`. A test checks 655 m and 926 m. |
| E6, perf | A*'s buffers are pooled per thread with a generation stamp. S10 runs at about 60,000 sim-s/s; with 120 people and the fight, 11,000–15,000 (31 ms a run). |
| E7, culture | Point and check text is invariant. A test compares every scenario's full `--json` report under sv-SE, ar-SA, fa-IR and tr-TR. |
| E8, the 8:00 horizon | At 8:00, if the Enforcer holds Vale, the run logs `horizon.open` and goes on. That ruling is for Waymark and Motive. |
| E9, overflow | Checked arithmetic in Debug, which the tests use. It found one cast, now explicit. |
| E10, reproducible builds | `PathMap` is set, so the same source gives the same DLL bytes on any machine. |
| E11, JSON | A leading BOM is dropped. Leading zeros, raw control characters and a bad `\u` are rejected. |
| I1–I3 | Fixed:<br>I1: an order can no longer half-apply on a rules error, since nothing reads rules by path in play.<br>I2: `Play` stops at `until`.<br>I3: the patrol's presence is hashed. |

**For Latch and Glyph:**
- `Preview` builds a card: one A* plus four temperament evaluations. Call it when the selection or target changes, not every frame.
- Use `Engine.FromTexts`, not `Engine.Load`, in Unity. `Load` caches statically.
- Add a `link.xml` that preserves `Suborned.Sim` for High stripping.

**Evidence**
- `dotnet test sim/Suborned.Sim.Tests`: 139 passed, 0 failed. That covers 39 scenarios × 3 (pass, twice identical, replay), Redline's probes, cultures and rules.
- `bun test tools/sim contracts`: 49 pass, 0 fail.
- `bun run typecheck`: exits 0.
- `perf` (Release): S10 at 49,900–67,000 sim-s/s, N11 at 11,000, N12 at 15,600.
- `play out/sim/s20.record.json` matches its record. A tampered checkpoint fails with "checkpoint 5 differs at tick 600".

**Next (Aster's 3):** investigators and watcher logs, then between-missions events, as far as the Handover's debrief needs.

## Round 3 (2026-10-15): Sky's balance rulings applied
- **Watch is 20 s** (`values.watch-s`, was 52 [B]; Motive's `briefs/011/motive-balance.md`).
- **`fight.traced` is off.** The final fight's shots add no Trail and no Alert, and the debrief doesn't count them as a shot fired: they are counted apart, as `finalFight.shots`. Shots before the fight count as before.
- **The close comes from `mission.json`.** The site reads each timeline entry by its words, not its time, so Waymark's 8:00 → 8:30 reaches the engine at the next `bun run sim:site`. Today `mission.json` still reads 8:00. A script without `until` plays to that close.
- **What changed in the 38 scenarios** (all green, each with identical checkpoints twice and on replay):
  - S05, S22, S23, S24, S25 and N12b end 32 s earlier, which is Watch's 52 → 20 s. Every check still holds.
  - N10 (loud): Trail 14 → 6, Alert 3 → 2, shots 6 → 2, debrief High → Raised. The fight's 4 shots no longer count.
  - N11 and N11b (safe): Trail 19 → 5 and 12 → 4, Alert 3 → 0, debrief High → Calm. A quiet route's fight leaves it quiet.
  - The other 29 are unchanged.

## Round 4 (2026-10-15): running and real-time shoot-outs (Sky approved briefs/011/motive-fights.md, all five answers)
Engine `suborned.sim/0.4.0`. Every value is in `contracts/rules/rulebook.json`, tagged [S], proposed, with "Sky 2026-10-15, briefs/011/motive-fights.md" as its source.
- **Running.** `op-run-speed-cm-s` 280. A run is refused, with its reason, for the Enforcer holding Vale, a carrier, the Tech while the loop holds, and anyone mid-action; `op-carry-run-cm-s` is retired. A run is noticed where an act would be. People within 13 m remember `saw-runner` (where, when, the face); it costs no Trail. Security (the guard, the patrol, a live camera, the response unit) counts one found fault a run: Alert → `run-alert` 1. A guard who saw the run is no ally. Running is free once a fight is on or the run is loud. `Game.QuoteMove` is the chip: walk and run time, who would see, Alert after.
- **Weapons** (`weapons` section): pistol and carbine, three range bands, the four modifiers, health per kind, and the response unit as 2 officers. Hits are counted by an integer accumulator (start 500; at 1000 a hit, minus 1000), so at 750 ‰ exactly three in four hit: `Rules.Accuracy` = `rulebook.ref.ts accuracy()`. Arms come from site `arms` (3: all four armed). Orders: draw, holster, fire until down (`force` for a civilian or contact), cease, stance (hold, return, free), panic (op `cell`, only once a fight is on), carry. New keys are written only when set: older orders hash as before.
- **Costs.** +2 T a person hit (once) and +2 T at a down; any gunfire → Alert ≥ 2 (`alert.gunfire` replaces `perShot`). A drawn weapon seen by security → Alert 2, and the watcher engages and calls it in. Seen by people, it is one witnessed incident (+1 T, Alert ≥ 1, the face) under the new traceable `weapon-drawn`; its standing is "none", which is Motive's call. The final fight stays untraced.
- **Going loud.** `Game.GoingLoud(order)` is pure: it reads loud or not, Alert to, civilians, who reports, police ETA, arrival, seal, rivals, patrol, the fight after, the officers, ally and warning lost, the price, and the 0.5 s hold. The engine never pauses.
- **Enter +30 s** once Alert ≥ 2 (`enter-alarm-s`).
- **Retired:** `shot-every-s`, `fire-reach-m`, `fire-rivals-reach-m`, `shot-trail`, `shot-alert`, `hits-to-drop`, `enforcer-health`, `aim-s`, the four `*-fire-every-s` values, and `fight-volley-s`. Take's "rivals within reach" now reads the rivals' pistol effective reach (15 m, was 16).
- **Sky's playtest (Aster):** acts on a device or a door are done at it. `at-reach-cm` 150 applies to Loop and Reroute at the cabinet, Enter at the public door, and Extract at the car. `tether-m` goes from 8 to 2. People acts keep `act-reach-m` 6.
- **Re-timed scenarios:**
  - S03–S09, S12–S19 fire until down, with honest expectations: the guard falls to 2 carbine hits, the react is 2 s, a drawn weapon is seen first, and civilians need force.
  - S05 and S11 became losses: the response unit stands at the service lane when Vale comes out. They are re-scripted with the cell covering: S05 holds lane posts; S11 holds posts at the Office approach and Enters at the door.
  - S19 runs to the north street (running is free once loud).
  - N10, N11, N11b and N18 fight with the whole armed cell. A lone Enforcer dies in N10 and N11b, as Motive's calculator says for loud.
- **New scenarios:**
  - N20 a running witness, N20b running free once loud, N21 runs refused;
  - N22 hit, hit, miss, hit;
  - N23 the drawn-weapon incident, N24 going loud, N25 panic, N26 downed and carried.
- **Evidence:**
  - `dotnet test sim/Suborned.Sim.Tests`: 185/185 (52 scenarios × 3, plus the rest);
  - `bun test contracts/rules tools/sim` 38/38; `bun run typecheck` clean.
  - Identical to HEAD except hashes: N08, N09, N09b, N12, N12b, N14–N17b, S01, S02, S23–S25, S27, S28.
  - Perf: S10 37k sim-s/s, N10 19k, N12 12k.

## Round 5 (2026-10-15): cancel and the activity read (Sky's playtest)
- **Order:** `{"op": …, "do": "cancel"}`. It stops what `Game.ActivityOf(op)` shows: an act in progress first, then a walk or run, a fire order, a draw, a loop held, and last a downed operative carried. The rulebook's `cancel` section names each verb's cost (closed sets; unknown = reject). Proposed, for Motive:
  - Watch and Listen part-done: the time is lost, nothing is learned.
  - An approach is `aborted`. The price, held at the order and paid only in the outcome's charge (Redline E2), comes back. There is no Trail. The person remembers an `aborted` approach with the Fixer's face: no refusal, so the approach can be retried.
  - A loop held `ends` (the camera comes back). A part-done Loop, Reroute, Mislead or lift is `lost`.
  - Enter: `staff-report`. He comes back out at the public door without Vale, and one reported incident is counted.
  - Take: `vale-reports`. Vale is let go at his car, and one reported incident is counted.
  - Carry: `put-down` where the carrier stands. Extract is instant. Holding Vale is no cancel.
- **Read:** `Game.ActivityOf(OpId)` and `Game.Activities()` return an `Activity` with Verb, Target, Phase, Progress (per mille), TicksLeft, OnDone, Queued (always "": the engine keeps no queue), CanCancel, CancelCost and CancelText. They are pure.
- **Scenarios:** N27–N35 cancel each verb mid-way. Each one runs twice and replays from its orders, and checks the state before and after (`Hash.Person`).
- **Evidence:**
  - `dotnet test`: 212/212, on the committed site and on the working tree's site.
  - Old runs are unchanged with hashes stripped: 51 of 52 identical; N24 differs only in a check's printed hash.

## Round 6 (2026-10-15): the fight AI and the cover (Pulse's briefs/011/pulse-fight-ai.md)
- **Cover:** `tools/sim/site.ts` copies `contracts/world/civic-block.cover.json` into the site's `cover` (`from`, `sha256`, points, blockers). The site test keeps the copy fresh, so the engine still reads one site file. `Cover.cs` is `select.ts` in integers. The §8.1–2 vectors are xunit tests. §8.3 runs on the site's own cover: `suborned-sim cover-vectors` must equal `select.ts` (tools/sim/sim.test.ts), which holds whatever posts Waymark sets.
- **Sight blockers (reported apart):** the cover file's 4 blockers (fountain, robot, piers) join the occluders. On all 61 scenarios, before and after with hashes stripped, only S19 changed: its shot line ran through the atom fountain. S19 is re-scripted to wait for a clear line.
- **AI** (`GameAi.cs`, rulebook `ai` and Pulse's 20 values that the engine reads):
  - One machine: routine, alerted, advance, engage, hold, break, gone. Targets come by class, with commitment, aimNew, and warm reacquire. Rivals step to cover and leave it when unprotected. Police hold their stand. Rivals break at more than half down, run for their car and are gone at 1 m; broken rivals no longer hold the car or count as combatants, and they cost no Trail.
  - The response unit joins the loud fight at the nearest police stand, overflowing to the nearest free cover or a 1.2 m slot. The guard ally runs to cover by his post and shoots the rival nearest the Enforcer. An engaging guard stands down after 10 s. The patrol chases to the shot's origin, stops to engage, and resumes its sweep after 15 s.
  - Shots resolve together at the end of the tick. Cover modifies a hit: 500 full, 750 partial, past point blank, standing still.
- **Not yet:**
  - the crowd's states (§4) and their 7 values;
  - the response unit and the drawn rival crew firing while they walk (their routes are timed, not stopped);
  - the guard's leash and his walk back to the door (he stands down where he is).
- **Evidence:**
  - `dotnet test` 213/213; `bun test tools/sim contracts/rules` 39/39; typecheck clean.
  - The 61 scenarios against the post-blocker run: 47 identical with hashes stripped; the fights differ, as the AI intends.
  - Perf in Release: S10 32k sim-s/s, N10 10k, N12 8.7k.

## Round 7 (2026-10-15): refusal codes (Fable), Motive's addendum, the crowd (Pulse §4)
- **Refusal codes:**
  - `Game.Issue` returns `Answer { ok, code, why, m, cm, usd, tick }`. It deconstructs as (ok, why) or (ok, code, why), and converts to the old (ok, why) tuple.
  - Codes are Fable's 76 names (`Answer.Codes`; `suborned-sim codes`), plus `needs-temperament` and `needs-<kind>` for other rulebook needs. A code not in the list fails a test.
  - The numbers ride as fields: `m` for too-far, out-of-range and the stand-beside reaches; `cm` for the device reaches; `usd` for no-cash's worst case; `tick` for busy-until, the car, the patrol and the driver.
  - The hash reads the code, not the words. Only runs that refuse an order changed hash: N09b, N19b, N21, N25, N26, N27, N33, S07, S22, S23, S24, S27 and route fail-district-seals. Their outcomes are unchanged.
- **Motive's addendum** ("Sky-approved fights, Motive addendum 2026-10-15"):
  - `gunfire-trail` 2: once a shooting, at its first shot at a target, hit or miss; not in the final fight.
  - `draw-incident-s` 5: drawings within 5 s and witness range are one incident.
  - `weapon-drawn` moves standing one step toward Menace.
  - Vale remembers an aborted take.
  - A guard downed by the rivals: chain `guard-down-by-rivals`, check-in kept, no ledger lines.
  - The safe fight lost with the Enforcer down fails `rivals-reach-vale`.
  - The going-loud card gains `TrailPerShooting` and `DownedCaptured`.
  - Waymark's loud goes to 9 T without the crowd (and 10 T with it: a fleeing person sees the Tech draw, 55 s after the Scout).
  - N36 is new: the unprepared safe fight lost.
- **Crowd** (`GameScatter.cs`, rulebook `crowd`):
  - The city's people startle at a shot within 30 m or a drawn weapon in sight within 13 m, flee to a refuge, shelter, and walk back 30 s after it's quiet.
  - Witnesses remember `saw-gunfire`. People on the street block watchers' sightlines (not the hostiles' aim).
  - The cast by role keep their places.
  - N37 and N38 are new.
  - Against the pre-crowd run, every outcome, tick, Trail and Alert is unchanged; logs gain `crowd.*`, and N11b gains one sighting span.
- **Evidence:** `dotnet test` 223/223; `bun test tools/sim contracts/rules` 39/39; typecheck clean. Perf in Release: S10 26k sim-s/s, N10 5.5k, N12 8.9k.

## Round 8 (2026-10-15): Inez and the guard are lovers (Motive, briefs/011/motive-lovers.md)
- **people.rules.json:**
  - `ties.kinds.lover` (mutual, secret, no introductions) and `betweenKinds.routine-meet`;
  - `secrets` (affair, record), and `person.is` and `tiers.simulated.carries` gain `secret`;
  - the schema follows.
  - The need `secret` is not retired yet: that is Pulse's, with the cast tables.
- **Data:** the test fixture ties p-cafe-11 and p-records-office-07 as lovers with the secret `affair`. `tools/sim/site.ts` writes each cast role's `secret` and `lover` from the people file's ties.
- **Rulebook:**
  - a `summon` section (stay 60 s, walkMax 90 s, alertMax 1, perShift 1, together 4 m);
  - Listen learns the secret, and a Watch that ends with the lovers within 4 m learns it too (new condition `lover-within`);
  - `turn.pressure` needs `leverage` (the need it acts on, or the secret known); a known secret removes the wrong-approach line and the report;
  - new memory acts `saw-violence` and `saw-weapon-drawn`.
- **Engine:**
  - The waitress's hard-coded lure is gone. A turned contact's call summons their lover, read from the cast's ties. The visit walks the grid at walking pace (46.2 s on the Handover's block), stays 60 s and walks back. Alert 2 ends it and he runs back.
  - Refusals cost nothing and carry codes: `summon-busy`, `-came-once`, `-alert`, `-saw-violence`, `-too-far`, `-off-street`, `-no-visit`, plus `no-leverage`.
  - A secret learned on one lover is learned on both. `Quote.Leverage` marks the exposure line. The card's "reports" reads the event's own report rows.
  - The test start state gains `untie`.
- **Moved:** S25 and N17 (Pressure +3 T, was +4); N14's card check; route-a-pressure 8 → 7 T. The visit is 46.2 s each way, not the mission's 47 s, so a few ticks shift (N11, S19) and some log texts change; outcomes are unchanged.
- **New scenarios:** N39 (the window is the tie), N39b (no tie, no pull), N39c (busy, came once), N39d (Alert 2 ends the visit), N40 (Watch together, then Pressure on the Loyal guard). A new unit test covers each refusal.
- **Evidence:** `dotnet test` 242/242; 70 scenarios.
