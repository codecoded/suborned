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
