# Brief 064: code review of the 2026-10-24 round (Redline)

- **Objective:** review the 2026-10-24 round for correctness, determinism and performance. The round covers the engine 0.29/0.30 host port, brief 057 §2–§4, the click ring, carry, the downed-agent filter, quiet cones, the crossing bake, kerb trims, RoadPoolJoints, Proof29, engine 0.30.0 and tools/qa, tools/world kerbreturn and tools/geometry. Also hunt the bug class Latch found 13 times today: a trailing `//` that swallows a statement.
- **Owner:** Redline (review). Fixes: Latch (`unity/`), Keel (`sim/`), Splice (`tools/`).
- **Baseline:** `b76d7657` (the last commit before the round). The round was committed while the review ran (checkpoints `c99070e1` and `887fcd61`, 2026-10-24 20:17 and 20:26). Line numbers are the working tree at 20:30. Read-only: nothing was edited, stashed or checked out.
- **Done-check:** each finding below is fixed or answered by its owner. The two perf findings (1, 2) are measured with the perf harness at play zoom on the 15-inch MacBook Air M4: warm, crowd on, Alt held over the café forecourt, with the patrol watched and walking.
- **Next owner:** Aster (route 1, 2, 4, 5, 6, 9 to Latch; 3, 8 to Keel; 7 to Latch; 10 to Splice).

## Result in one paragraph

No swallowed statements are left in today's added lines (method below). The engine reads no wall time, no unordered collection and no float in a way that could move a replay. The new host reads (`NoticesNow`, `PersonCone`, `PatrolLegs`, `MoodOf`, `WatchOffered`, `GoQuery`) are read-only, as their docs claim. The real risk is frame time. Today's two "show it in the street" features (people's cones, and every learned security fan drawn all the time) rebuild geometry from engine sight tests far more often than their docs say. Neither has been measured on the reference machine.

## Top 10, ranked (severity, then confidence)

### 1. People's cones: one rebuild is about 650–1,250 full engine sight queries, and more than one rebuild happens per frame (High perf · high confidence in the mechanism, cost not measured)
`unity/Assets/Suborned/Runtime/Play/HostNotice.cs:127-134, 159-163` · `sim/Suborned.Sim/GameNotice.cs:141-160` · `sim/Suborned.Sim/GameCrowd.cs:184-193`
- **What happens:** `BuildCone` calls `Game.NoticesNow(id, act, pt)` once per sample: 25 rays × 26 samples (13 m at 0.5 m) for a wedge, 48 × 26 for a security ring. Each `NoticesNow` starts again from nothing:
  - it walks `OnTheStreet()` (the cast plus up to 130 walkers, each `CrowdPos` computed) to find the person it already found;
  - it recomputes their facing (`FacingOf` → `CrowdPath` / `LegMm`);
  - it runs `SeesFrom` = 2 × `Sight3` over all 235 occluders.
- **How often:** the cache key holds the person's exact `At` and `Facing` (line 132). A walking civilian therefore changes key every tick, so the hovered cone rebuilds at 10 Hz. On top of that, one round-robin cone rebuilds every frame, and every cone with `BuiltTick < 0` builds in the frame it first appears. Pressing Alt can therefore build up to 7 cones (about 4,500 queries) in one frame. The doc comment says "one cone rebuilt a frame". The code does at least 2, and up to 7 on the first Alt frame.
- **Failure scenario:** the player holds Alt or hovers people on the busy forecourt. Each frame is expected to cost a few ms of main-thread sight tests, with a 10–20 ms spike when Alt is pressed. That breaks 60 fps on the Air. This is an estimate: measure it before and after the fix.
- **Fix:**
  - Keel: add a batch read such as `NoticesAlong(id, act, Pt[] pts, bool[] seen)`. It resolves the person, position and facing once, then calls the existing private `NoticesAt(p, wp, at, act, alert, facing)` per point.
  - Latch: build at most one cone a frame (drop the `BuiltTick < 0` bypass; draw a new cone's plain wedge until it is built). Quantise the key: `At` to 25 cm, facing to the 5° ray step.

### 2. Every learned fan is now drawn every frame, and moving watchers rebuild engine fan + host mesh every tick (High perf · high confidence)
`unity/Assets/Suborned/Runtime/Play/HostSight.cs:83-95, 117-129` · `unity/Assets/Suborned/Runtime/Play/HostViews.cs:529-541, 590-615` · `sim/Suborned.Sim/GameSight.cs:86-92, 209-235`
- **Before today:** `ShowSight` drew fans only on focus, hover or Alt.
- **Now (brief 057 §3)**, every frame:
  - `KnownFans()` allocates the list, `WatcherIds()` (with `"camera:" + id` per camera) and `ConeFan`'s interpolated key string per watcher.
  - `FanMatters` runs per fan: 4 agents × their whole walk sampled every metre × point-in-polygon.
  - The patrol sweeping, the rerouted patrol walking and officers all change pose every tick. Each change makes the engine rebuild the fan plus `FanShadows` (all occluders), and the host run `Build` → `Restrength` with `CutFill`, `QuadCut` (24 samples a segment) and `ShadowRims` (a sample every 0.1 m against every polygon). That is about 10 rebuilds a second per moving watcher, for the whole run once its sight is known.
  - During every fade (in 0.25 s, out 0.6 s), `Restrength` re-runs that whole geometry rebuild every frame: the per-frame step (0.067 in, 0.028 out) is above the 0.02 threshold. A strength change only needs new vertex colours and the rim's material.
- **Failure scenario:** after the Scout watches the patrol, its fan is rebuilt 10 times a second until the end. At Alert ≥ 2 every fan is "strong" and every officer's fan rebuilds as they move. Hovering in and out of fans re-runs the clipping for 15–36 frames each time.
- **Fix (Latch):**
  - `Restrength`: keep the clipped geometry and rewrite only `SetColors`. Keep two rim meshes, rest and strong, and swap renderers instead of rebuilding.
  - Compute `FanMatters` once per engine tick, not per frame.
  - Reuse one `HashSet` in `ShowFans` (line 531).
  - Optional (Keel): a non-allocating `KnownFans(List<Fan> into)`.
- **Measure** the patrol-watched case with the harness.

### 3. Engine hot path allocates in `Occluder.Blocks3` (Medium perf · high confidence)
`sim/Suborned.Sim/Geo.cs:144` (`var poly = new Pt[Poly.Length]`), called from `Site.Sight3`, which `Game.SeesFrom` calls twice per sight test.
- **What happens:** with `perception.mount` on, every line whose box meets an occluder allocates a scaled copy of that occluder's polygon. `za != zb` is the usual case: the chest target sits below the eye. Witness tests, Watchers (every second), takedown notice, fire lines and finding 1's host queries all go through it.
- **Failure scenario:** steady GC churn on Unity's main thread from the engine step, which shows up as incremental-GC spikes. Not a determinism risk.
- **Fix (Keel):** clip the segment in its own parameter space and test it against the unscaled polygon. Exact rationals work: compare `cross × D` against the unscaled edges. If that is not wanted, use a `[ThreadStatic]` scratch `Pt[]` grown on demand. A scratch buffer changes no result, because the test's output depends only on its inputs.

### 4. A fan stays "strong" for an agent whose walk has already passed through it (Medium correctness/UX · high confidence)
`unity/Assets/Suborned/Runtime/Play/HostSight.cs:126`
- **What happens:** `FanMatters` samples `op.Walk.Points` from index 1, so the walked part counts too. It never reads `op.WalkMm`.
- **Failure scenario:** the Tech walks through the entrance camera's cone and out the other side, 40 m on. The cone stays at full strength until he arrives. Brief 057 §3 asked for the cones to be "softer, kinda dominates", and this keeps them loud. It also doubles finding 2's sampling cost.
- **Fix (Latch):** start sampling at `op.Walk.At(op.WalkMm)` and skip the segments before it.

### 5. Reroute marks: the engine route is rebuilt and keyed every frame while the reroute is live; the drawn route lags up to 3 s behind the robot (Medium perf · high; Low visual · medium)
`unity/Assets/Suborned/Runtime/Play/HostReroute.cs:51-55`
- **What happens:**
  - `EngineRoute()` (`PatrolLegs`: new plan, lists and `AddWalk` per leg) and the concatenated `key` string run every frame before the 30-tick throttle on line 54. While live, that is about 60 s of per-frame allocation.
  - The live key holds `er[0]`, the robot's current position, so it changes every tick. The throttle then holds the old polyline for up to 30 ticks, and the dotted line starts at where the robot was up to 3 s ago.
- **Fix (Latch):** check the throttle first (on `PatrolAwayUntil`, mode and tick), then call `EngineRoute`. Draw from the robot's live position: prepend `m.Patrol` and drop route points behind it, so the line never trails it.

### 6. Per-frame strings and LINQ in the HUD added today (Low-Medium perf · high confidence)
Each one is small. Together they are steady garbage at 60 fps:
- `HudToolkit.Cones.cs:57, 61, 90`: a new `List<string>`, one concatenation per cone, and `string.Join` every frame. All of it only feeds the proof's `ConesShown`. Build it only when a proof reads it, or only when the set changes.
- `HudToolkit.Seal.cs:42`: `$"{left / 60}:{left % 60:00}"` every frame. Rebuild only when `left` changes.
- `HudToolkit.Mood.cs:36`: `"mood_" + mood`, and `Icon` → `"icons/singles/" + atlas`, per pip per frame. Cache the last mood per pip and skip when unchanged.
- `HudToolkit.Less.cs:269`: `System.Linq.Enumerable.Any(hw, char.IsDigit)` per roster row per frame (delegate plus string enumerator). Use a `for` loop.
- `HudToolkit.Ring.cs` (shut Go): `s.Holders.Split(",")` and a new `List` per frame while the ring is open. Split once when the ring opens.
- `AtmosphereLens.cs:442, 448`: `Hex(ev.sky.zenith)`, `Hex(ev.sky.mid)` and `Hex(ev.sky.sunGlow)` parse colour strings every frame at dusk and night. Parse them once in `Check`.
- `HostNotice.cs:107, 117, 120`, `PersonConesFrame`: a `want` list, a `near` list, `new[] { "waitress", "driver" }` and the `Guard` closure every frame.

**Fix (Latch):** as listed. Each fix is local.

### 7. `RoadPoolJoints.Collecting` stays true if a ground bake throws (Low-Medium correctness, editor · high confidence)
`unity/Assets/Suborned/Editor/World/CityLayers.cs:53-59` · `RoadPoolJoints.cs:18-24, 64`
- **What happens:** `Clear()` sets the static `Collecting = true` and `Fill()` resets it. Nothing protects the code between them, and `GroundPieces` throws on a piece without a kit ref (line 56).
- **Failure scenario:** a failed build leaves `Collecting` true in the editor session. Any later `KitPlacer.Bake` of a `DK_Road_Pool` piece outside `GroundPieces` is then quietly held in a static list and never baked, so the strip-light pools go missing with no error. The list also keeps `Transform`s of the dead scene.
- **Fix (Latch):** `try { … } finally { Fill() or Abort() }` in `GroundPieces`. `Abort` clears the list and resets `Collecting`.

### 8. Two `.rej` files are committed in the engine source folder (Low · high confidence)
`sim/Suborned.Sim/ScenariosFailCause.cs.rej`, `sim/Suborned.Sim/ScenariosHandover.cs.rej` (now in HEAD `887fcd61`)
- **What I checked:** both are rejected hunks from a failed patch at 01:41. Their content landed anyway, in a later form:
  - FailCause has all three `[6925,-4250]` edits.
  - Handover has `BesideDoor` at (6875, -4300) (engine 0.28) and the bounded `r.Until` wait at line 355.
- **Failure scenario:** none at build time (they are not `.cs`). But the next person who sees them may re-apply a stale hunk: the Handover `.rej` would move `BesideDoor` back to (6925, -4250).
- **Fix (Keel):** delete both files.

### 9. Host state added today is not reset by `Begin()` (restart or Replay) (Low · high confidence)
`unity/Assets/Suborned/Runtime/Play/HandoverHost.cs:203-237` does not clear:
- `rewalks` (`HandoverHost.cs:966`). The patrol-Watch rewalk cap of 4 can already be spent by the previous run, so the first patrol Watch of the new run gets no rewalk.
- `noticingNow` (`HostSight.cs:108`). Fans can be "strong" for a frame or two from the old run.
- `coneCache` and `coneRobin` (`HostNotice.cs:32-33`).
- `haveSweep`, `lastSweepAt`, `rerouteKey` and `rerouteKeyTick` (`HostReroute.cs:22`).
- `sealSpanFrom` and `sealSpanLock` (`HostSeal.cs:19`), and `noWalkCell` (`HostContext.cs`).

All of them are view-only and I found none that changes an order, so replay stays exact. But Begin's own contract is a clean start.

**Fix (Latch):** clear them in `Begin()`, with the other resets on lines 209 and 224.

### 10. `tools/world` kerb returns: the cache key misses inputs; KR-2 is looser than it says (Low · medium confidence, latent)
- `tools/world/gen/kerbreturn.ts:231, 273`: `indexOf` caches per `ctx.d` only, but its result also depends on:
  - `ctx.S` (`kerbReturnStd`, `S.sections`, the bays);
  - `ctx.civ` (`civicCorners`).
  
  A first call with a context that lacks `S` caches an empty list for that `d`, and every later call with the same `d` gets it. So does a call with a different standard on the same derived plan, e.g. a test that edits `kerbReturn` without re-deriving. Today's callers re-derive, so it is latent. **Fix (Splice):** key the WeakMap on `d`, then a small inner map keyed by `S` and `civ`, or keep the cache only when both are the ones `d` was derived with.
- `tools/world/rules/kerbreturn.ts:46`: KR-2 fails an arc only below 50 % stone cover (`m < len * 0.5`). Its header and the rule text say the arc is covered "to within a stone, less what crossings and drives drop". A return with half its kerb missing passes. **Fix (Splice):** compare against `len − droppedM − one stone length`, using the drop log `dressKerbArcs` already keeps.

## Lower, or uncertain

- `CrossingKit.cs:143-150` (editor): the static `lightMat` lives for the whole editor session, not "once a build" as its doc says. After `bun run kit` changes `DK_…Light` with no domain reload, the next build keeps the old glow. It heals itself if the asset is deleted. Fix: reset it at build start.
- `PlayInstall.cs:185` (editor): a walk file without `world` leaves `want` null, so the stale-check's message `want[..]` throws a NullReferenceException instead of the intended `InvalidDataException`.
- `HostSeal.cs:8-11, 35`: the summary still says it reads FailCause and SealOrigin "by name (reflection)". It no longer does (direct reads since engine 0.30). `NoSeal = int.MaxValue / 4` (line 14) copies the engine's private `INF` (`Game.cs:163`). Ask Keel for a public `Game.NoSeal`/`SealOn` so the two can't drift.
- `HandoverHost.cs:971` (uncertain): when `Playable` refuses an order, `IssueCore` reports success if `pending[k]?.Do == o.Do`. That is right for the deferred act. But a refused move ("Can't walk there") would also return true if a deferred walk-in `move` were ever pending for that agent. I found no path that sets one today.
- `FrontEnd.cs:39-60` `LoadClock`: if `Begin` runs and the game scene never loads (Quit before the load), `pending` stays true and `Loaded` stays subscribed, so later load timings are never logged. Timing only.

## The swallowed-statement hunt (the class Latch found 13 times)

**Method:**
- Every added line from `b76d7657` to the working tree, plus untracked files, in `unity/Assets/Suborned`, `sim`, `tools/{qa,world,plan,geometry,game}` (about 10,500 lines of C# and 10,000 of TS).
- Searched three ways:
  - (a) code lines whose trailing `//` comment holds `);`, `= new`, `) {`, `x = y;`, a call with `;`, `return …;`, `var`, `const` or `let`;
  - (b) whole-line comments that parse as code;
  - (c) unclosed `/*`.
- Every hit was read: all are prose. Examples of `);` in prose: "(rivals, police); the hold's…", "(0.64 -> 0.75 in view);".
- The same scan was also run over the whole current Runtime, Editor and sim tree.

**Result:** none left. The earlier cases (`beatsBox.style.flexDirection`, `x.style.left`, `Icon(fireIcon, …)`, `followChip.style.bottom`) are now real statements or `/* */`, as Latch's fixes left them.

**Suggestion (Splice):** a lint in `bun run test` that flags `; // … <identifier>(…);` and `; // … <identifier>.<member> = …;`. It would have caught all 13.

## Determinism (engine)

- **No engine source reads wall time, a RNG, `GetHashCode` or a float.**
  - The doubles in `ScenariosSealCar.cs` (`SegD2`) and `ScenariosFireMove` are in scenarios, not the engine.
  - `ResolveStopFaces` sorts its places.
  - `Hull` sorts by a total order.
  - `Blocks3`, `GameRerouteWalk` and `GamePatrolPath` are integer-only.
  - `PlanRerouteWalk`'s bisection is integer.
- `Hash.cs` now adds `KMood` once a mood has been watched. That is intended and comes with the 0.30 version bump. Pinned runs keep their engine.
- Host-side: `HostNotice`, `HostReroute`, `HostSeal` and `HudToolkit.Mood` only read. `GoQuery` saves and restores `refusal`. `Playable`'s deferred orders are logged at the tick they are issued, so replays stay exact.

## Not covered

- Proof29, ProofLevels, FadeProbe (proof and editor probes) and the FrontEnd panel layout.
- `AtmosphereLens`, `DistanceHaze`, `LightPhase` and `RoadLight` beyond the per-frame parse in finding 6. They changed again in `887fcd61` after I read them.
- tools/geometry beyond the CLI output format that tools/qa parses (it matches `statics.ts`).
- The QA runner reads correctly: mutants can leave orders out of tick order, but the QA host sorts them (`QaHost.cs:90`), as the CLI does.

## Latch's answers (2026-10-24, code in; measured after the coming batch)
- **1:** HostNotice: at most one cone built a frame (the hovered one first when its key moved, else round-robin among those that need it); a new cone shows its plain wedge (no engine reads) until its turn; the key is a struct quantised to 25 cm and the 5° ray step (ConeKey), no string; each build resolves the person once (Keel's `NoticeProbeOf`) and asks `NoticesAt` per sample. The want/near lists, the cast roles and the Guard delegate are reused.
- **2:** HostViews fans: the geometry (clipped fill, the rest rim and the strong rim as two meshes, the shadow ink) is built once a pose (`Geometry`); a fade step rewrites the fill's vertex colours and swaps which rim renderer is on (`ApplyStrength`). `FanMatters` once an engine tick a watcher (HostSight.FanMattersNow); ShowFans reuses one set; the cell order array is static.
- **4:** FanMatters samples the walk ahead only, from `WalkMm`.
- **5:** HostReroute: the route is fetched (PatrolLegs or the grid preview) only when its key changes (in play or not, PatrolAwayUntil, the preview's ends to 50 cm) or 30 ticks on; drawn each frame from the robot's live position, then the route past the nearest point on it.
- **6:** ConesShown is built when a proof reads it; the seal's words only when the second changes; the mood icon only when the mood changes; the roster's digit test is a loop and the carry pace's regex once per pace; the shut Go's holders split once a ring; AtmosphereLens parses the evening's sky colours once per string (`HexC`).
- **7:** CityLayers.GroundPieces: a throw calls `RoadPoolJoints.Abort()` (bands dropped, Collecting off) and rethrows.
- **9:** Begin calls `ResetRound24()`: rewalks, noticingNow, the cone cache and robin, the reroute state, the seal span, the no-walk cell, the FanMatters cache.
- **Measuring 1 and 2:** `-perf064 <dir>` (Perf064.cs): the Scout watches the patrol (it sweeps on), the Tech the entrance camera, the Enforcer selected with Takedown armed, Alt held (PeekLatch), the pointer riding the walker nearest the café forecourt, crowd on, 2880 x 1864, warm 30 s; then four 20 s windows, before, after, before, after (`HandoverHost.PerfLegacy` puts back the round's cone rule, exact key and per-sample person lookup, the full fan rebuild on every fade step and FanMatters every frame). Per window: frame ms, ShowSight (fans), PersonConesFrame and RerouteFrame ms a frame, cone builds and point tests a second, fan geometry builds a second. Runs on the Mac Studio after the batch's game-build (`out/scratch/latch/perf064.sh`); the Air needs Sky.
