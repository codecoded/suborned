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

## Keel's answers (2026-10-25)
- **3 and 8:** confirmed: `Occluder.Blocks3` (sim/Suborned.Sim/Geo.cs) allocates nothing per test since engine 0.30.0 (`Pt` is a struct; the overflow check runs up front with no `Pt[]`), and no `.rej` or `.orig` file remains under `sim/` (tracked or untracked).

## Redline follow-up on build 0.2.122 (2026-10-26; read-only, no player run, no Unity)
- **Inputs:** briefs/041/port-progress.md "Batch 0.2.118 … BATCH OK on build 0.2.122" (the perf064 bullet, the renderer counts) and code round 3 ("live dot rows", "signal heads … clock rings"); briefs/041/build/0311/perf064 and 0309/perf064; briefs/064/perf (0.2.113); out/batch-0303b…0311.log; out/batch/late (0.2.122's late-fight frames); brief 059's note on S10 and engine 0.32.0 (worktree agent-ad654047fa24cd1cb at 100b5e3a).
- **Measured here:** the engine only, headless, on the Studio, through `bun run guard`. Three engines, each a scratch copy under out/scratch/redline/simperf with a per-call timer: 0.30.0 (0.2.117's, `git archive 3d7dcdb1`), 0.31.0 (0.2.118–0.2.122, this tree) and 0.32.0 (the worktree). Each played its own tree's route scripts 10 to 20 times, in Release on CoreCLR, interleaved. Load was 3 to 18 (uptime). Outputs and patch scripts are in briefs/064/perf/redline-1026/. The repo's sim/ was not touched. Line numbers below are this tree at 2026-10-26 00:50, or the worktree where marked (wt).
- **Done-check:** each finding answered by its owner; R1's open question closes with the Air run.

### Answers in short
1. **No frame-time regression against 0.2.113 at play zoom once load is accounted for.** The live dots were already in 0.2.118, which measured like 0.2.113–0.2.117. The ~1 ms swing since then came on an unchanged world. The live-dot design is within budget on the Air by estimate: it costs CPU draw submission, not GPU. One real change shows elsewhere: the late-fight row's shape since 0.2.120 (R2). The engine is ruled out for it.
2. **Baked-dots A/B in batch 0.2.123: no.** It cannot resolve the expected difference on the Studio, and the question it would answer is already answered within the Studio's resolution. The run is given under R5, for the Air only.
3. **0.32's "4 ms a run" is a cost only in batch tooling, and negligible there too.** It is ~1–2 µs a tick on average: 0.01–0.02 ms a second of play at 10 Hz. What reaches the live game is 0.32's worst ticks. The worst is 3.3 ms on CoreCLR, at route A 4:13.1: three rivals kite in one tick, through a grid flood that runs a sight test per cell. Grid A* is the hot path in every version. Two result-identical fixes are worth taking with the 059 port. Neither blocks it (R6).

### R1 · 0.2.122 against 0.2.113: no regression at play zoom beyond noise (Info · high confidence; owner: none, Aster to note)
- **Same world, different numbers.** Batch 0307 built the world once, with the live dots ("live dot rows: 194 modules", out/scratch/latch/live-world-0307.log). 0.2.118 and 0.2.119 share that world and its per-frame code; 0.2.119 changed only proofs and the briefing tags.
  - Play zoom: 0.2.118 289 fps (p99 4.8 ms); 0.2.119 267 fps (p99 5.2 ms).
  - Start shot GPU: 3.0 ms on 0.2.118, 3.5 ms on 0.2.119.
  - 0.2.113–0.2.117 (baked dots) read 290–294 fps (p99 4.6–4.8 ms). So the live dots cost nothing measurable at play zoom; the ~1 ms step at 0.2.119 is the machine.
- **perf064, like for like at 2880×1864:**
  - 0.2.122's after-windows run +0.35 and +0.53 ms (mean) over 0.2.113's.
  - 0.2.120 and 0.2.122 have the same content, yet differ by up to 0.40 ms mean (4.64 vs 4.24 in after-2): that is the noise band.
  - The host costs the harness isolates are flat (mean ms a frame): sight 0.045 vs 0.037–0.041, cones 0.078–0.118 vs 0.070–0.103.
  - The extra frame time is therefore outside the code that changed, and inside the noise.
- **Counts (no noise), tp-worst:**
  - drawn 2,145 → 1,894;
  - SetPass 255 → 218;
  - tris 5.7 → 5.1 M;
  - render thread 2.6 → 2.6 ms.
  
  The retired crossing pieces more than paid for the 160 extra renderers.
- **Air estimate:** play-opening GPU 3.2 ms × 4–4.5 (the brief 048 ratio) → 12.8–14.4 ms; tp-worst 3.6 ms → 14.4–16.2 ms. The binding limit stays the whole frame's GPU, which this round barely moved. **Needs the Air** (open since brief 064 findings 1 and 2).

### R2 · late-fight changed shape from 0.2.120 on; host or GC, not the engine (Medium · medium confidence; owner: Latch, with Splice)
- **The change:**
  - Through 0.2.117, loud-fight's worst frame was the deterministic end frame ("order+save+end", 19–20 ms), p99 5.6–5.8 ms.
  - On 0.2.120, 0.2.121 and 0.2.122 (3 of 3 runs) it is an untagged mid-fight frame (41.3, 32.7, 28.4 ms), p99 7.2, 7.4, 6.9 ms.
  - Load does not explain it: 0.2.120 ran at 0.2.113's load (5.7 → 11.3 against 4.9 → 11).
- **Where it falls:** in 0.2.122's frames (out/batch/late/loud-fight.frames.csv), the frames over 15 ms sit at ticks 2883–2913, plus 3025.
  - Frames that run a tick average 5.79 ms against 4.40 ms for frames that don't.
  - The tick frames' p99 is 17.1 ms, against 6.5 ms for the rest.
- **Not the engine:** 0.30.0 and 0.31.0 time the same on loud-fight.
  - Mean 14.5 vs 14.8 µs a tick, p99 116 vs 118 µs.
  - Same worst tick: 2883, the startled crowd's 9 routes, 2.84 vs 2.90 ms (tickprof-6).
  - So the 2883–2913 burst is host work around the crowd startle (views, cones, fans, ground lookups), or GC.
- **Fix (Latch):**
  - Let `-late` set `HandoverHost.PerfTiming`.
  - Write per frame into frames.csv: the engine step's ms (a Stopwatch round the host's `RunTo`), `ShowSight`, `PersonConesFrame`, `RerouteFrame`, `SignalHead` (all heads), and `GC.CollectionCount(0)`.
  - Splice: late.ts names the top section of any frame over 15 ms.
  - The next batch then says which one it is. Until then, don't chase it by guesswork.

### R3 · Live dots: within budget; what they cost is draw submission (Low perf · medium confidence; owner: Latch, optional)
- **What runs:**
  - 481 renderers carry MaterialPropertyBlocks: 41 heads × 7 parts = 287, plus 194 dot modules. The build log reads "not casting 481"; the baked build reads 287.
  - Unity 6 draws a renderer with an MPB outside both the GPU Resident Drawer and the SRP Batcher (briefs/064/research/mpb-batching.md). The live dots add 194 such draws, against 34 batch chunks removed.
  - The seat slot gets the MPB too (SignalHead.cs:146 sets it renderer-wide), but DK_Enamel_Charcoal has no `_EMISSION`, so the look is unaffected.
- **No LOD gap:** each head has one renderer per part (41 casting + 287 = 8 a head), so CrossingKit.cs:183's "first LOD found" wiring misses nothing.
- **GPU:** none to speak of. 24–48 tris a module, unlit opaque (Shaders/SignalDots.shader), with a depth pass and no shadow pass.
- **CPU:** at play zoom ~1–3 signalled crossings are in view, about 30–90 per-object draws (estimate). The Studio's tp-worst render thread did not move (2.6 ms).
- **If the Air shows its render thread within ~2 ms of its GPU time,** the change is the builder's, not the A/B:
  - merge each signalled crossing's live modules into one renderer at bake, with uv.x rewritten to metres along the crossing (CrossingKit.cs:195–213's `DotFrom` and the mirroring on line 208 already compute it), and `CityLayers.BakedCrossingPart` (CityLayers.cs:72–73) unchanged;
  - SignalHead then drives one renderer a crossing: 194 → 16 renderers, 388 → 32 slots, 194 → 16 MPBs.
  - No art change, so it needs no compression sign-off.
- **Not now:** nothing measured asks for it.

### R4 · SignalHead rewrites every part of every head in CLEARING, every frame (Low-Medium perf · high confidence in the mechanism, cost estimated; owner: Latch, code now)
`unity/Assets/Suborned/Runtime/Play/SignalHead.cs:126–147`
- **What happens:**
  - The change key (line 127) holds `half` to the centimetre for every head, including the 25 heads that drive no dots (`Dots.Length == 0`).
  - On all 16 signalled crossings `half` falls 65–69 cm/s (L 12–24 m over clearing 9–18 s; from the export), so at 60 fps the key changes every frame. That holds for ~28 % of the cycle on every crossing.
  - In those frames lines 133–140 rewrite all 7 part MPBs of each such head, and the driving head rewrites its ~12 modules (141–147).
  - That is about 11 heads × 7 plus ~55 modules ≈ 130 `SetPropertyBlock` a frame. Estimated 0.1–0.3 ms of main thread on Mono, and the off-screen heads pay it too.
- **Fix:**
  - (a) Line 126: compute `half` only when `Dots.Length > 0`.
  - (b) Give the dots a key of their own (`half` to 5 cm, or to the dot pitch: a dot is lit or dark by its centre's u, so nothing finer shows), apart from the head's (state, numeral, ring lit, blink, colour).
  - (c) Optional: skip the writes while none of the head's renderers `isVisible`. Keep a last-written key per head and per module, so a head that comes into view writes once.
  - Look only: no replay effect. Proof118's H frames re-shot as the check.

### R5 · Baked-dots player A/B: no, not in 0.2.123 (Info; owner: Aster)
- **Why not:**
  - The live dots were already measured against baked: 0.2.118 against 0.2.113–0.2.117 (R1), no difference at play zoom.
  - The expected difference (R3) sits below the Studio's ±1 ms GPU noise.
  - The one column precise enough to show it (tp-long's render thread, ±0.15 ms across builds) shows nothing.
  - It costs two world builds and a player build, with Latch mid package bump.
- **When it becomes yes:** only after the Air run, if tp-worst's render thread there is within ~2 ms of its GPU time.
- **The run then, on a quiet Studio:**
  1. `bun run guard /Applications/Unity/Hub/Editor/6000.6.2f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath unity -executeMethod Suborned.World.HarbourWorldBuilder.Build -world out/world/harbour-district.world.json -bakeddots -logFile out/scratch/latch/bakeddots-world-0123.log`. The log must say "dot rows baked, not gated (-bakeddots)".
  2. `bun run batch --from game-build`: the baked player with tp-long. Then out/scratch/latch/perf064.sh into briefs/041/build/<n>/perf064-baked.
  3. Keep that app for the Air.
  4. `bun run batch --from unity-world`: the live world and Sky's player.
  5. On the Air, warm and quiet: tp-long and perf064 on both players.
  6. Compare tp-worst render ms, SetPass and drawn, and perf064 after-window mean and p99.
- R3's merge would make the A/B moot. If anything is built, build that instead.

### R6 · Engine 0.32.0: the cost per run is noise for the game; the worst ticks are what matter; A* is the hot path (Low-Medium perf · high confidence on CoreCLR; owner: Keel)
- **S10 re-measured:** `suborned-sim perf 30 S10`, interleaved, 0.31 vs 0.32: 25.1/25.5 → 30.3/30.3 ms a run (+20 %, Keel's figure). S10 is 4,177 ticks, so that is 6.1 → 7.3 µs a tick.
- **In the live game** that is +0.012 ms a second at 10 Hz: nothing.
- **In batch tooling:** about 320 runs × 5 ms ≈ 1.6 s of CPU, spread over 18 cores: nothing.
- **Allocation per tick:**
  - 0.30: 3.9 KB; 0.31: 5.1 KB; 0.32: 6.2–6.6 KB a tick on average, up to 69 KB in one tick.
  - That is ~60 KB/s at 10 Hz: small for Unity's incremental GC, but it rises every version. Watch it; no action.
- **Worst ticks, median of 10–20 plays (CoreCLR, Studio):**
  - **0.32, route A, tick 2531 (4:13.1), 3.26 ms (0.31's worst on route A: 1.09 ms).** Three `ai.kite` answers in one tick:
    - 1.40 ms of it is 2,135 sight lines from KiteHide's blind-point flood: wt GameKite.cs:288, `S.Grid.Nearest(pos, q => !Sees(q, sp), 800)`, two `Sight3` per cell to 8 m;
    - 1.65 ms is 12 `Grid.Route` calls.
  
    The re-plans at 2571, 2611 and 2651 cost 0.5–0.65 ms each (~620–670 lines). The route A play has 16 `Nearest` calls in all, 7.5 ms, against none on 0.31.
  - **0.32 elsewhere:**
    - `GetawayAfterFire` at 2506: 1.37 ms (6 routes; 0.30 ms on 0.31);
    - `CrewStep` at 1724: 0.97 ms (2 routes).
  - **In every version, 0.30–0.32:**
    - loud-fight 2883 (the crowd startles): 2.9 ms, 9 routes;
    - 1535: 1.3 ms, a single route.
- **The hot path:**
  - **`Grid.Route`** (Grid.cs:206–250, the same in wt) is 15–28 % of step time: 42–148 µs a call, up to 1.3 ms for one long route, and several in one tick. None returns null. The search is ~80 % of it; the string-pull (line 244, quadratic) is ~20 %.
  - **`Site.Sight3`** (Site.cs:354; wt 415) is 16–29 % of step time: a linear scan of all 236 occluders with a box reject, ~0.6 µs a call. The host's cone builds run through it too (perf064 cones p99 3.1–3.6 ms on 0.2.122).
- **Mono:** the player runs the engine on Mono, so read these CoreCLR ticks as a floor. The factor is not measured. R2's engine-step column measures it in the player.
- **Fix before or with the 059 port (Keel).** Both are pure speed-ups, with the same results in the same order:
  - (a) KiteHide (wt GameKite.cs:284–289): build the occluder subset whose boxes meet the box of `sp` ∪ (pos ± 8 m) once per call, and test the predicate's two lines against it. The boolean and the first cell found are identical. Better and more general: a bucket index over `Site.Occluders`, built at load and walked along the segment with a stamp, so each occluder is tested at most once. Every sight test and the host's cones gain from it.
  - (b) `Grid.Route` and `Grid.Nearest` (Grid.cs:206, 253; `Heap` at 286):
    - a `long[]` heap that grows by doubling, in place of `List<long>`;
    - a one-cell blocked border on `free[]`, so the neighbour loop drops its bounds tests;
    - carry i and j instead of `k / Ny` and `k % Ny`;
    - the same push and pop order, so the same paths.
    
    Estimate 1.5–2× on the route share.
  - **Not recommended:** spreading same-tick re-plans over several ticks. It changes replays (version bump, re-record).
  - **Done-check:** `suborned-sim hashes` byte-identical before and after on every scenario and route script; `bun run test`; `suborned-sim perf 30 S10` and the scratch `tickprof` (briefs/064/perf/redline-1026/patch.py, patch2.py) before and after.

### R7 · The perf rows can't separate the build from the machine (Medium, process · high confidence; owner: Latch for the player, Splice for the batch)
- **The gap:**
  - perf064 times the wall-clock frame interval only, with no load and no CPU/GPU split (Perf064.cs:79–96).
  - late-fight samples load but has nothing per section (R2).
  - Every judgement above leans on comparing builds in different runs.
- **Fix:**
  - perf064 per window: FrameTimingManager main, render and GPU ms, plus the load average at the start and end of each window (the batch's sampler).
  - Batch: when a row is NOISY MACHINE, run perf064's after-window pair again (Splice, tools/game).
  - Counts stay the gate; ms only with a quiet machine or on the Air.

### What needs the Air (Sky's go-ahead; Aster)
- Play zoom and tp-worst, whole frame: GPU against 16.7 ms (estimate 12.8–16.2 ms). This decides 60 fps; nothing in this round moves it much.
- The engine's worst ticks on Mono on the M4, through R2's column: route A 4:13.1 on 0.32, and the crowd startle.
- tp-worst's render thread with the 481 per-object draws, to decide R3 and R5.

### Code now, by owner
- **Latch:**
  - R4 (SignalHead.cs:126–147);
  - R2's frames.csv columns and `-late` PerfTiming;
  - R7's perf064 columns.
- **Splice:** late.ts reads R2's columns; NOISY MACHINE re-runs perf064.
- **Keel (with the 059 port):** R6 (a) and (b), hash-identical.
- **Not now:** R3's merge, R5's A/B.

## Air 0.2.122 (2026-10-26)
- **Inputs:** briefs/064/air-0122/ (README, tplong, tplong-2, perf064, late, machine.txt, run-air.sh); the raw run on the Studio (out/scratch/main/air-0122/results/air-perf-0122-20261026-0214: machine.log, player logs, late frames CSVs); PC_RPAsset.asset, PC_Renderer.asset, QualitySettings, RenderCap.cs, TacticalShadows.cs, light.json `shadows.tactical`, HandoverHost `standOff`; URP 17.6's SSAO source in Library/PackageCache; briefs/008/perf/ab-1 (the one Air A/B we have, 008 B6). Sources: briefs/064/research/air-0122-gpu.md.
- **Measured here:** nothing on the Air, no player run, no Unity. Read and arithmetic only. Every ms below that is not from the Air run is labelled an estimate.
- **Done-check:** Sky's OK for the soak (Aster); the soak returns a GPU-clock trace; the settings A/B (second sitting, below) replaces the estimates in the table with warm Air numbers; Glint and Sky rule on the look items.

### Answers in short
1. **The evidence supports thermal throttling. It does not prove it.** With the same counts, every view slows by 1.40–1.56× after about 6 minutes. The main thread barely moves, and the frame also drifts inside a single perf064 run. No clock or temperature was traced, though, and the size of the rise is larger than the published ~22 % for this machine. The soak below settles it in one ~25-minute sitting, using the same player and kit, plus `macmon` (sudoless GPU clock and temperature).
2. **At play zoom the biggest single item costs us frame time and shows nothing.** The renderer's SSAO has Falloff 100 m. The play camera stands 160 m back, so URP's SSAO skips every street pixel (no occlusion), yet its blur and composite still run full-screen. Turning it off for the orthographic play camera should give back about 2–3.5 ms warm with no visible change (to confirm with shots). Below that, the ranked list gets play zoom to roughly 14–17 ms warm on look-cheap settings alone. **Street level (tp-worst) does not reach 60 warm without a render-scale decision.**
3. **That decision is also a spec question.** GDD §11 says "60 fps at 1080p". Every tplong and perf064 row ran at 2880 × 1864, which is 2.6× the pixels of 1080p. Sky should say which one is the bar (Aster).
4. **Headroom for the asset-quality brief: none today.** Target: warm mean ≤ 13.0 ms and p99 ≤ 16.7 ms at play-opening and tp-worst, at the shipped render resolution. Today these views are 8–10 ms over. Until a settings round is measured warm, that brief must be frame-neutral. After it, its allowance is 13.0 ms minus the measured warm mean, capped at 1.5 ms.

### A1 · Does the evidence support throttling? Yes, on four counts, with three gaps (Info · medium-high confidence; owner: Aster for Sky's OK)
- **For:**
  - **Same work, slower frame.** Same counts in both runs (play 1.81 M tris, 117 SetPass; tp-worst 5.14 M, 218, 1,894 drawn). Mean frame ×1.40 (play), ×1.53 (tp-street), ×1.56 (tp-worst); p99 ×1.35, ×1.67, ×1.62.
  - **The CPU is not what grew.** Main thread 8.8 → 8.0, 7.0 → 7.8, 5.8 → 7.5 ms. Render thread 2.0 → 2.1, 3.2 → 3.6, 3.2 → 4.2 ms. Both stay far under the frame, so the frame is GPU-bound and the GPU got slower. The CPU's small rise matches its own published power step-down (20.5 → 12 W by 5 min).
  - **It drifts inside one process.** perf064 after-1 → after-2, 40 s apart in the same run, about 3 and 4 minutes into the session: 15.46 → 17.31 ms mean, frames over 16.7 ms 220 → 652. The host did *less* work in after-2 (cone builds 5.5 → 2.5 a second, cones 0.52 → 0.32 ms a frame).
  - **The machine is known to do this.** The Air is fanless. Notebookcheck measured the GPU "dropped by around 22%" under longer load, with the SoC going from 29 W to about 9 W. GDD §11 already says it throttles.
- **Gaps:**
  - **No trace.** `pmset -g therm` prints nothing on Apple Silicon. `thermalState` and the GPU clock were not read.
  - **Larger than the published loss.** ×1.40–1.56 against ~×1.28. Something else may be adding to it:
    - T3 Code and Outlook were open;
    - the late pass ended at load 7.14. That is mostly the player's own threads, but it is not separated;
    - the runs were 2880 × 1864 windowed on a desktop whose first surface was 3840 × 2410, which hints at a scaled display mode the compositor pays for (see A4).
  - **Not settled.** The runs reached only ~7 minutes. Published CPU power settles at ~20 minutes, so the warm number may not be the floor yet.
- **Weak evidence, not used:** loud-left-downed's 10 s buckets rise 12.4 → 15.0 ms, but its content changes over the fight.
- **Metal gpuMs:** on Metal it is the span from the frame's first command buffer to its last. A pipelined, GPU-bound frame overlaps the next one and includes waits, so the span reads above the frame interval (008 B6 M6 saw 23.8 ms on a 13.1 ms frame). It is unusable as busy time. Use the frame interval when GPU-bound, macmon's `gpu_active_ratio`, and for a per-pass split `Recorder.gpuElapsedNanoseconds` (A3).

### A2 · The soak to ask Sky for (owner: Aster for Sky's OK; Latch or main to add the script to the kit)
- **Script:** `briefs/064/air-0122/soak-air.sh` (new; `zsh -n` clean; not run anywhere). It is run-air.sh's player, kit and rows, plus a trace and a cold gate. Copy it into the 0.2.122 kit folder beside run-air.sh and run it over SSH as before: `ROOM_C=<room °C> ./soak-air.sh`.
- **Trace, no sudo:**
  - `macmon pipe -i 500 > macmon.jsonl`: GPU clock, GPU power, GPU and CPU temperature, every 0.5 s. Needs `brew install macmon` on the Air, which is installing software on Sky's machine, so ask. Without it the script still runs, with the thermal state only.
  - `osascript -l JavaScript -e "ObjC.import('Foundation'); $.NSProcessInfo.processInfo.thermalState"` every 5 s, with the load average, into `thermal.log`.
  - `pmset -g thermlog` (expected empty).
  - The tracers are killed by PID at the end.
- **Controls:**
  - AC power, battery not charging (charging heats the chassis), Low Power Mode off;
  - every app quit (T3 Code and Outlook too), lid open on a hard desk, room temperature logged;
  - `display.txt` (system_profiler, with the "UI Looks like" mode), the top processes at start and end, and the player's own settings file copied;
  - **cold gate:** the script waits until thermal state is 0 and the GPU is under 45 °C (checked every 30 s, up to 20 minutes).
- **Sequence (~25 min of load):**
  1. tplong-01, cold, `-novsync`.
  2. tplong-02 … 07 back to back: the same three rows every ~110 s, to ~13 min. This is the soak and its time series.
  3. perf064 (warm 30, windows 20) and late, as run-air.sh runs them. Since the 1080p bar (below), every row asks for 1920 × 1080; 0.2.122's perf064 still runs at 2880 × 1864 and is logged as such.
  4. tplong-08 `-novsync`: the warm row.
  5. As shipped: tplong-09 with vsync on (the player's settings, no `-novsync`), and perf064 `-capture-native` at the display's own size.
- **Verdict rule (Redline reads it):**
  - **Throttling, confirmed:** counts within 1 % between tplong-01 and tplong-08, and the frame-time rise tracks the drop in mean `gpu_freq_mhz` over each row's window (frame ratio ≈ clock ratio, ±10 %) while `gpu_active_ratio` stays near 1.
  - **Then the bar is the plateau:** the mean of the last three soak rows, if they agree within 3 %.
  - **Not throttling:** the clock stays flat while the frame rises. Then look at top-start/top-end, the load and the display mode.
  - If macmon reads 0 for power (its macOS 27 caveat), the clock and temperature still decide.

### A3 · Where the GPU ms come back, against the warm number (owner: Latch for settings, Glint for the look, Sky for render scale)
- **What we know about the split:**
  - There is no measured per-pass GPU split anywhere. The Studio is main-thread-bound, and Metal's gpuMs is a span.
  - The only Air A/B is 008 B6 (1920 × 1080, world only, run contaminated, ratios only): SSAO from depth after opaque cost 1.2–1.9 ms over SSAO off (1.6–1.9 at five of seven stops), and shadows off saved 24–40 %.
  - The rest are estimates from that run and from bandwidth: 120 GB/s, and one full-screen read and write at 5.37 Mpx is ~43 MB, about 0.3–0.6 ms.
  - **One shape is measured:**
    - The cold frame does not follow triangles: tp-worst draws 2.8× play's triangles and 6.6× its renderers at the same 14.9 ms.
    - Late at 1080p ran 12.7–14.1 ms mean against play's 21.0 at 2880 × 1864, both warm-ish.
    - Read together, these put about 2.1–2.5 ms per Mpx warm on pixel work, so 53–64 % of the warm play frame scales with pixels. This is a two-point model across different scenes: crude.
- **As set today:**
  - render scale 1 on the Air (RenderCap.MaxPixels = 2880 × 1864);
  - SSAO full resolution, Depth source with Medium normals, Medium (8) samples, High (bilateral, 3-pass) blur, after opaque, Falloff 100;
  - main-light shadows 4,096 atlas, fitted to 2 cascades (2,048 each) at play, soft shadows High (7×7 tent);
  - HDR (R11G11B10), Opaque Texture and Depth Texture forced on for every camera;
  - Bloom half-resolution start, SMAA Medium on the play camera;
  - a signals overlay camera stacked on the play camera; Forward+.

| # | change (exact setting) | est. warm saving, play / tp-worst (2880 × 1864) | look cost | judge |
|---|---|---|---|---|
| 1 | **SSAO off for the orthographic play camera.** In `TacticalShadows.Begin`/`End` (already per camera, ortho only), `ScriptableRendererFeature.SetActive(false)` on PC_Renderer's SSAO, restored at End; or a second renderer data without SSAO in PC_RPAsset `m_RendererDataList`, set on the play camera. The follow view keeps SSAO. | **2–3.5** / 0 | **None expected.** URP's `ShouldSkipAO` (SSAOCommon.hlsl:184) gives no occlusion past Falloff 100 m, and the play camera's street lies 142–178 m from it (standOff 160). Only roofs taller than ~70–100 m come inside 100 m, and there URP scales AO by (1 − d/100)²: a few percent, except on the very tallest towers. Glint confirms on yaw 30 and 210 shots, as 008 B6 already proposed ("SSAO off as shipped"). If Glint wants AO at play zoom, the alternative is Falloff ≥ 260 with Downsample on, Blur Low and Samples Low: it then costs about 1–1.5 ms and adds a look. | Glint |
| 2 | **Render scale (the spec question).** `RenderCap.MaxPixels` (RenderCap.cs:12) 2880 × 1864 → 2160 × 1398 (0.75 on the Air) or 1920 × 1080 (≈ 0.62, the GDD's pixels); PC_RPAsset `m_UpscalingFilter` 0 (Auto) → FSR 1.0 (EASU + RCAS, ~0.3 ms back); STP as a variant (temporal: replaces SMAA, may ghost the crowd). | **4.5–6 at 0.75; 6.5–8.5 at 0.62**, both views | A softer 3D frame; the HUD stays native. Characters are 52–78 px at 0.75 and 43–64 px at 0.62, against the 40–60 px at 1080p bar: readability holds at both. | Sky, Glint |
| 3 | **Soft shadows High → Medium.** PC_RPAsset `m_SoftShadowQuality` 3 → 2 (the sun uses the pipeline's). 16 → 9 taps in every lit pixel. Set it in the asset, not at runtime: prefiltering may strip the other variant. | 0.7–1.5 / 0.7–1.5 | A slightly firmer penumbra: a 5×5 tent over ~3.3 px texels instead of 7×7. | Glint |
| 4 | **Opaque texture off.** PC_RPAsset `m_RequireOpaqueTexture` 1 → 0. No shader in unity/Assets reads `_CameraOpaqueTexture` or `SampleSceneColor` (rg); every camera is set to "use pipeline". | 0.3–0.8 / 0.3–0.8 | None. Latch checks the frame debugger (no CopyColor) and identical shots. | Latch |
| 5 | **Depth texture by request.** PC_RPAsset `m_RequireDepthTexture` 1 → 0, **and** the sun's `LensFlareComponentSRP` disabled while the play tier renders (AtmosphereLens.cs:440–442 only sets its intensity to 0 there). A registered data-driven flare with occlusion makes URP force the depth texture (UniversalRenderPipeline.cs:1487, `CheckPostProcessForDepth`); SSAO and DoF (the long view) request depth themselves. Pays only together with 1. | 0.3–0.6 (+ the flare's occlusion pass) / 0 | None if nothing else reads it (no shader in Assets does). Check that the flare and DoF still work in third person and the long view. | Latch |
| 6 | **Bloom starts at quarter resolution.** `Atmosphere.Profile` (Editor/World/Light/Atmosphere.cs:54–57): `bl.downscale.Override(BloomDownscaleMode.Quarter)`, from light.json `post.bloom`. | 0.3–0.7 / 0.3–0.7 | Wider, softer glow. Signals are drawn outside post, so they are untouched. | Glint |
| 7 | **The signals overlay camera becomes a pass in the base camera after post** (HostFightLab.SignalCamera, lines 315–330): one colour and depth load/store fewer on the tile GPU, and one camera's culling fewer on the CPU. | 0.3–0.8 / 0.3–0.8 | None, if signals stay outside the grade and draw the same. Medium confidence: measure. | Latch, Glint checks signals |
| 8 | **SMAA Medium → Low** (HandoverHost.cs:194 `antialiasingQuality`), or FXAA. Moot if 2 picks STP. | 0.3–0.7 / 0.3–0.7 | Figure edges (brief 020 asked for AA). | Glint |
| 9 | **Small-mesh shadow cull.** PC_RPAsset `m_ShadowSmallMeshScreenPercentages` (0 today), e.g. 0.5 % on the last cascade. | 0–0.3 / 0.3–1 | Tiny far props lose their shadows, inside the haze. | Glint |
| 10 | **Shadow atlas 4,096 → 2,048** (`m_MainLightShadowmapResolution`). A render target, not an art texture. | 0.3–0.8 / 0.3–0.8 | The street band goes from ~5 to ~10 cm a texel: softer, can shimmer when panning. Last resort. | Glint |

- **Not additive.** Items 1, 3, 4 and 6 shrink the very pixel work that 2 scales. My combined estimates:
  - play: 1 + 3–6 gives 21.0 → ~14–17 ms warm; adding 2 at 0.75 gives ~11–14.
  - tp-worst: 3–8 gives 23.3 → ~19.5–22; adding 2 at 0.75 gives ~15.5–18. That is still over the target. Street level also needs brief 048's geometry items (the palms' LODs, cull levels), or 2 at 0.62.
- **Not recommended:**
  - URP Tile-Only Mode (incompatible with post);
  - the drawer's GPU occlusion (+36 % on the Studio, render thread +3 ms);
  - HDR off (bloom and the HDR signals need it);
  - cascades 2 → 1 (halves the street's shadow sharpness);
  - MSAA in place of SMAA (with the depth copy and SSAO it adds resolves);
  - any decimation, triangle cap or texture cut (GDD §12: Sky).
- **Not measured, and likely to matter:**
  - Night and evening. The scene has 391 lights, the lamp pools are point lights, and Forward+ loops over them per pixel. No Air row has timed them.
  - The 1080p late row's p99 (19.7 / 23.1) and its 57.7 ms frame are CPU ("order+save"): R2, not the GPU.
- **Turning the estimates into numbers (Latch, for a second sitting after the soak; needs a build):**
  - **tplong:**
    - `-look <variant>` with: as-built; ssao-play-off; no-opaque (4 + 5); bloom-quarter; scale-075-fsr; scale-062-fsr; cheap = 1 + 4 + 5 + 6; cheap + scale-075.
    - Soft-medium is built into the asset for that build, because of the variant.
    - Run interleaved A B A B, 20 s each, at play-opening and tp-worst, after the 13-minute soak.
  - **Per row:** `Recorder.gpuElapsedNanoseconds` for URP's main-light shadow, opaques, SSAO and its blur, the depth and colour copies, transparents, bloom, uber post, SMAA, the final blit and the UI, plus `SystemInfo.supportsGpuRecorder` in the log. That gives the per-pass split we have never had.

### A4 · The display mode (Low-Medium · low confidence; owner: Latch, after the soak)
- **What the log shows:** the tplong player.log opens on a Metal surface of 3840 × 2410, then CaptureSize sets a 2880 × 1864 window.
- **If that is the Air's scaled desktop:** a player in the default borderless fullscreen gets a 3840 × 2410 drawable (9.3 Mpx). RenderCap then draws the 3D at ~0.75, but the final blit, the HUD and the compositor's downscale to the panel all run at 9.3 Mpx. That is perhaps 0.5–1.5 ms the harness never sees.
- **The soak shows which:** `display.txt` and perf064-native.
- **If confirmed:** size the fullscreen drawable on macOS to the panel's pixels, measured.

### A5 · The baseline for the asset-quality brief (owner: Aster, into that brief)
- **Target, warm on the Air, at the shipped render resolution, `-novsync`:**
  - mean ≤ 13.0 ms and p99 ≤ 16.7 ms at play-opening and at tp-worst;
  - derivation: the Air's p99/mean ratio is 1.20–1.41 (cold and warm, all three views), so p99 ≤ 16.7 needs mean ≤ ~13.
  - Vsync is on in the shipped player (GameBoot.cs:34), so any GPU frame over 16.7 ms is a dropped frame. The uncapped warm number is the right one to hold.
- **Today:** 21.0 / 26.0 (play) and 23.3 / 28.9 (tp-worst) at 2880 × 1864: **8 and 10 ms over. The headroom is negative.**
- **Rule until A3's round is measured warm:** the asset brief is frame-neutral. That means ≤ +0.3 ms warm mean in an interleaved Air A/B; on the Studio, counts may not grow (triangles ≤ +10 %, SetPass, casters).
- **After A3:** the allowance is 13.0 ms minus the measured warm mean, capped at 1.5 ms. The rest is kept for the fight (30 combatants), night lights and p99.
- **Where that brief's cost will come from (from this run):**
  - Triangles are not the main cost: 2.8× the triangles at the same cold frame. Warm, tp-worst's extra 3.3 M triangles and 1,600 renderers cost ≤ 2.3 ms. Read that as an upper bound of ~0.7 ms per extra million drawn triangles, shadows included.
  - **The risk is per pixel on surfaces that fill the screen** (facades, roofs, ground):
    - extra texture samples, normal or detail maps, parallax;
    - transparency;
    - above all alpha-test or discard, which costs the Apple GPU its hidden-surface removal. 008 B6: the fade's discard alone halved the heaviest stop before it was limited to the reach.
  - Each such material change gets its own warm A/B.

### Code now, by owner
- **Aster:**
  - Sky's OK for the soak and for `brew install macmon` on the Air;
  - the GDD question "1080p or the Air's panel" (§11), for Sky.
- **Latch:**
  - add soak-air.sh to the 0.2.122 kit;
  - then A3's `-look` variants and the GPU recorder columns in tplong for the second sitting;
  - item 4 can go in now (no look change; frame-debugger check);
  - items 1 and 5 after Glint's shots.
- **Glint:** items 1 (confirm it is invisible), 3, 6, 8, 9 and 10; render scale with Sky.
- **Not now:** item 10, STP.

## The 1080p bar (2026-10-26)
- **Sky, 2026-10-26: "1080p probably ok" as the 60 fps bar** (GDD §11's "60 fps at 1080p"; answers A1 gap 3 and the "1080p or the Air's panel" question). Air runs measure at 1920 × 1080 from now on. Render scale stays Sky's look call.
- **Scripts changed** (not run; `zsh -n` clean; the helpers dry-tested on the 0.2.122 kit and results on the Studio):
  - `briefs/064/air-0122/run-air.sh` and `soak-air.sh` rewrite every tp-long and late row to `"w": 1920, "h": 1080` as they copy the kit's resolved files. The 0.2.122 kit's tp-long rows say 2880 × 1864; late's already said 1920 × 1080.
  - perf064 gets `-capture-size 1920x1080`.
  - After each pass, `atsize` reads the size the pass really ran at from its own JSON (`screen`) into machine.log, with a WARNING when that size is not 1920 × 1080. A number is then never mixed in at the wrong size without its label.
  - soak's `perf064-native` (`-capture-native`) stays: it is A4's display-mode check, not a bar row.
- **What the player supports:** windowed only. CaptureSize.Apply sizes a windowed drawable of exactly 1920 × 1080 pixels (on the Air's Retina, a 960 × 540-point window). There is no fullscreen-at-1080 path in any harness. At 1920 × 1080, RenderCap (cap 2880 × 1864) draws the 3D at scale 1.
- **0.2.122's perf064 cannot measure at 1080.** Perf064.cs:46 calls `CaptureSize.Apply(2880, 1864)` unless `-capture-native` and ignores `-capture-size`. The flag is passed for the next build; until Latch's fix, its row comes back logged 2880 × 1864.
- **Which 0.2.122 Air numbers were taken at 2880 × 1864** (5.37 Mpx, 2.59× 1080p's 2.07 Mpx):
  - all tp-long rows in both runs: play-opening 14.98 / 19.27 → 20.99 / 25.97, tp-street 14.30 / 18.47 → 21.87 / 30.82, tp-worst 14.93 / 17.87 → 23.26 / 28.89 ms (mean / p99), and their main and render thread ms;
  - all of perf064: cones p99 26.1 → 7.7 and 22.9 → 6.3; whole-frame p99 45.0 / 41.2 → 25.2 / 27.2; after-1 → after-2 15.46 → 17.31 mean, frames over 16.7 ms 220 → 652;
  - every derived number: A3's savings column, A1's ×1.40–1.56 ratios, A5's "today" (21.0 / 26.0 and 23.3 / 28.9; 8 and 10 ms over).
- **Already at 1920 × 1080:** late (loud-fight 12.66 / 19.71, max 57.7; loud-left-downed 14.11 / 23.14, max 45.2), and 008 B6's A/B.
- **What should change when re-measured at 1080, and what should not** (estimates, from A3's two-point model: 2.1–2.5 ms a Mpx warm pixel work, 53–64 % of the warm play frame):
  - **Counts do not move:** triangles, SetPass, shadow casters, renderers drawn. Neither do the CPU costs: main and render thread, perf064's cone ms and builds, late's 57.7 ms order+save frame (R2).
  - **Play-opening warm:** ~21.0 → ~12.7–14.2 ms mean, which matches late's 12.7–14.1 at 1080. That is at or just over A5's 13.0 target. p99 ~15.3–20 (ratio 1.20–1.41): borderline.
  - **tp-worst warm:** ~23.3 → ~14.1–15.7 mean, p99 ~17–22. Still over: street level still needs brief 048's geometry items.
  - **Cold rows:** ~15 → ~9–10 mean.
  - **A3's per-pixel savings shrink about 0.39×.** SSAO off at play is ~0.8–1.4 ms instead of 2–3.5; items 3, 4, 6, 8 and 10 shrink alike. Items that are not per pixel do not shrink: 7's culling, 9's casters. Item 2 changes question: at a 1080 window the 3D is already at the GDD's pixels. What remains is what a fullscreen player on the Air gets (RenderCap draws up to 2880 × 1864 there), which is Sky's look call and A4.
  - **Throttling (A1) does not go away:** the rise is the GPU clock, so it should be about the same fraction at 1080. In ms it is smaller: ~+4–5 instead of +6–8.
  - **A5's target holds** (warm mean ≤ 13.0, p99 ≤ 16.7, at 1920 × 1080). "Today" becomes the re-measured warm 1080 rows; the asset brief stays frame-neutral until they are in.
- **Bar and shipped player differ:** the bar is 1080, but the shipped default is borderless fullscreen at the desktop's drawable, so a player on the Air draws more than the bar. Whether the shipped Air default should be 1080 is Sky's call (render scale), via Aster.
- **Handoffs:**
  - **Latch:** Perf064.cs:46 should honour `-capture-size` (default 1920 × 1080) and update its summary (line 18). CaptureSize.cs's comment (lines 11–15) says the 60 fps limit is set at 2880 × 1864.
  - **Splice:** move the measure sizes to 1920 × 1080:
    - `tools/game/tplong.ts` DEFAULT_SIZE (line 16) and its header, tplong.test.ts;
    - `tools/game/batch.ts` perf064 `-capture-size 2880x1864` (lines 305, 313) and the header lines 14–15;
    - `tools/game/cli.ts` AIR / `-frames-size 2880x1864` (line 242 on).
  - Then a fresh Air kit's resolved rows are already 1080, and the scripts' rewrite is a no-op.
