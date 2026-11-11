# Brief 109 · Juice · round 1 (Latch, 2026-11-11)

Moments 1–3 (orders, takedown, getaway), behind one launch switch, all view only. Sky judges these before the rest are built.

**The build for Sky:** **0.2.325**, `out/build/play/The Suborned.app` (`bun run batch --sky`: BATCH OK, logged in runs/builds/builds.json). Opened from Finder, the juice is on; `-juice off` shows the before.

## The switch
- `-juice on|off` turns the juice on or off for that launch.
- **With no flag:** on when Sky opens the app himself, off for any automation launch (a proof, shots, the late-fight check). Any flag `playing.ts` calls automation counts.
- So the batch's proofs, pictures and frame-time gates are the same as before. When Sky adopts the juice, the proofs should switch to `-juice on`. That is a one-line change in `Juice109.Decide`.
- **Replays don't move.** Nothing in the engine or in the host's decisions reads the juice, and the game clock is never touched.
  - Host logic: **208 pass, 0 fail.** That is 206 before, plus 2 new `Juice109` cases: the switch, and that with no rig installed an order, the clock and a save-and-replay hash the same.

## What each moment does
1. **Orders.**
   - A dust puff where the agent sets off: 12 warm grey-brown particles, kicked back from the direction of travel.
   - A turquoise pulse at the destination or target. Two rings in turquoise-light #60e0ea, the HUD's own colour, drawn with the Shader Graph glow.
   - The ring opens with a pop: scale 0.8 to 1 with a small overshoot, using UI Toolkit's own USS transitions. PrimeTween was not needed.
   - The money counts to its new value over 0.45 s, and the figure bumps when it changes. Trail bumps when it changes.
2. **Takedown** (on `takedown.out` or `takedown.dead`).
   - A 0.26 s Cinemachine Impulse bump: 0.2 m across the screen, its direction set by the tick, so a replay shakes the same way.
   - A 0.6 s vignette pulse in ink-deep #0b1615, peak 0.38.
3. **Getaway.**
   - Tyre smoke from a **VFX Graph** at the two rear wheels while the car pulls away (1.2 s).
   - Exhaust from the tailpipe while the car drives.
   - Two headlight flares (Shader Graph glow).
   - After the Go, the view eases after the car until it leaves, or until the player touches the view (pan, wheel, drag or turn).
   - Front and back come from the direction the car actually moves. The getaway view's +z is its rear, which differs from CarFx's rig.

**Camera, as the brief asked.** The tactics camera stays in charge, and the host places the Unity camera as before. While a shake is live, the end of the frame copies that pose and lens into one CinemachineCamera with no position or rotation control ("Do Nothing", no damping). It then steps the CinemachineBrain by hand with `ManualUpdate`, and blends are cut. The shaken pose exists only for the render: the next frame's first script puts the camera back before anything reads it. Picking, the HUD's anchors and the host's camera code never see the shake. When no shake is live, the brain isn't stepped at all.

## Packages
- `com.unity.cinemachine` **6.6.0**. In Unity 6000.6, Cinemachine is a core package built into the Editor. Its changelog continues straight on from 3.1.6, so this is the Cinemachine 3.1 line, with Impulse and `ManualUpdate(frame, dt)`.
- `com.unity.visualeffectgraph` **17.6.0**, built in and matching URP 17.6.
- `com.unity.splines` 2.9.0 came in as Cinemachine's dependency.
- Unity filled in `ProjectSettings/VFXManager.asset` (the VFX runtime shaders) when the package arrived.
- Import plus the authoring test took 30 s in batchmode.

## Shader Graph / VFX Graph: can the crew author and edit them? Yes, for Shader Graph. Yes for VFX Graph values, not for its structure.
- **How it was tested.** `tools/juice/author.ts` writes every version as text from Unity's own templates in the Editor's BuiltInPackages, the way any agent would edit a file. `Juice109Authoring` (Editor, batchmode) then imports each version and reports:
  - whether it compiled;
  - its properties and their defaults;
  - whether the importer rewrote the agent's file;
  - one pixel drawn with it (a quad with vertex colour (1, 0.5, 0.25), additive over black).
  - Report: `briefs/109/latch/authoring.json`.
- **Shader Graph: every step passed.**

  | version | edit made as text | compiled | `_Tint` default | drawn pixel | importer rewrote the file |
  |---|---|---|---|---|---|
  | v1 authored | the HDRP target stripped; set transparent and additive, no shadows; the colour renamed `_Tint` (HDR); map alpha wired to Alpha | yes, 0 messages | 1 0.8 0.5 | **1 0.8 0.5** | no |
  | v2 tuned | one value: the Tint default | yes | 0.4 0.9 1 | **0.4 0.9 1** | no |
  | v3 structural | two nodes added (Vertex Color, and a Multiply cloned from the template's) and the edges rewired | yes | 1 1 1 | **1 0.5 0.25** (the vertex colour reaches the output) | no |

  The game's glow, `Generated/Juice109/Juice_Glow.shadergraph`, is v3. **Verdict:** new effect shaders can be Shader Graphs.
  - **The workable route** is a template plus a small TS generator that edits the multi-JSON by object id, with Unity's import as the check, as `author.ts` does. A new node is cloned from one Unity wrote.
  - **There is no public C# API** that builds a graph node by node. Unity's graph model is internal.
  - **Hand-writing a whole graph from nothing would be slow and fragile:** every node needs its slot ids, GUIDs and edges.
  - **Tuning values is easiest:** exposed properties are ordinary material properties, so they can live in a json the player reloads.
- **VFX Graph: values yes, structure no.**
  - **Values:** a `.vfx` is Unity YAML, and every value sits under its slot's property name (Count, MinSpeed, Force, Size, Color, bounds…). `author.ts`'s `setVfxSlot` sets 11 values by name on `Simple_Burst` and makes `Juice_TyreSmoke.vfx`, the game's tyre smoke.
    - It imports with no messages, and the importer doesn't rewrite the file.
    - It draws in the player: seen in the getaway clip, with its size fixed once after the first look.
  - **Structure:** adding or swapping blocks (say, constant-rate spawn in place of a burst) would mean authoring YAML documents with internal fileIDs and script GUIDs. There is no public API for it either. Keep VFX Graph to templates plus named values; anything structural is a human's five minutes in the graph editor, or Shuriken from code (as Fx097 and CarFx do).
  - The burst count can't be read back in batchmode (`aliveParticleCount` is 0 there), so the check is the player.

## Clips (before = `-juice off`, after = `-juice on`; the batch's own clip capture, ShotRunner)
Each clip is played on its route script's own orders and encoded at true game speed. A clip frame steps the game by its real frame time, about 27 ms, so encoding at the nominal 15 fps would play it slow. `*-side.mp4` is before (left) next to after (right). Files are in `briefs/109/latch/clips/`, git-ignored as briefs' mp4s are:
- `juice-orders-{before,after,side}.mp4`: route-a-board 0:20.6, the Scout's Listen on the waitress. The waitress's pulse, the order's target, shows through the occlusion cut. The Scout is on the deck.
- `juice-cash-{before,after,side}.mp4`: fight-safe-rivals-reach-vale 2:01.7, the Fixer's Pay on the driver. The money counts down instead of jumping.
- `juice-takedown-{before,after,side}.mp4`: route-g 2:37, the door guard's knockout.
  - Measured: shake 0.195 m at its peak.
  - Measured: the vignette darkens the corners by up to 56 grey levels (out of 255) and fades over about 0.6 s.
  - In the clip the shake spans only 3 frames, because a captured frame is slow. In play at 60 fps it is about 15 frames.
- `juice-getaway-{before,after,side}.mp4`: route-a-board 4:13.4, the Go.
  - The car pulls away with tyre smoke, exhaust and flares, and the after view follows it out. The before view stays put.

Frames: `out/scratch/latch/109/clips/{off,on}/<name>/`. Scratch build: `out/scratch/latch/109/player` (0.2.325-scratch).

## Cost (Studio, Mac Studio M5 Max): `briefs/109/latch/costs.json`
- **Setup:** 1920×1080 windowed, play zoom, route-a-board 0:40, 8 s of frame times with the clock running, three launches per variant, medians.
- **Worst case:** each `-juicedemo` variant repeats one moment without end, so it is the worst case, not a moment in play.
- **Noise:** the machine was busy with other agents (load average 7 to 20), so differences under about 0.4 ms are within the noise.

| variant | frame mean (ms), 3 runs | Δ mean vs off | GPU (ms), 3 runs | Δ GPU | Δ p99 |
|---|---|---|---|---|---|
| off | 4.72 · 4.37 · 4.63 | — | 1.49 · 1.54 · 1.53 | — | — |
| on, idle | 4.81 · 4.64 · 5.06 | +0.18 | 1.43 · 1.52 · 1.59 | −0.02 | +0.09 |
| order, every 0.5 s | 4.47 · 4.58 · 5.28 | −0.04 | 1.37 · 1.50 · 1.76 | −0.04 | −0.09 |
| takedown, every 0.6 s | 4.26 · 4.21 · 4.97 | −0.36 | 1.69 · 1.69 · 1.57 | **+0.16** (the vignette's post) | −0.30 |
| getaway launch, without end | 4.69 · 4.28 · 5.08 | +0.06 | 1.58 · 1.65 · 1.54 | +0.05 | +0.19 |

- **Nothing measures above the 0.3 ms aim.** The one signal clear of the noise is the vignette's +0.16 ms GPU while it shows.
- **Before the idle fix** (on a quieter machine), the idle juice cost a steady +0.23 ms of CPU. That came from the brain stepped every frame and the two tyre VFX components always present. Now the brain steps only while a shake is live, and the VFX objects are on only during a launch.
- **Air:** not run (ask Aster first). A quiet-machine rerun, `bun briefs/109/latch/clips.ts <app> --perf`, would tighten these numbers.

## Checks run
- `bun tools/game/runtime-compile.ts`: Suborned.Runtime compiles.
- `bun run hostlogic`: 206/206 with the juice in, then `hostlogic Juice109`: 2/2. The batch's own run: 208 pass, 0 fail.
- `bun run typecheck`: clean.
- `bun test tools/game/proofwhat.test.ts tools/game/proofdelta.test.ts`: 73/73. The proof map has a row for the juice's files.
- Editor authoring test: `authoring.json` (above).
- Scratch builds: draw check ok each time. Clips and perf runs: no exception in any player log.
- With `-juice off`, the log says "Juice109: off" and nothing is installed.
- `bun run batch --sky`: **BATCH OK, 0.2.325.** Sim 490/490 scenarios with identical replay, words ok, compile ok, host logic 208/0, kit, world and unity-world ok. The smoke was ok: OldTown, the briefing, play to tick 40, the HUD drew 20 elements, no Exception.
  - It took 1081 s, over the 600 s target. The packages changed `unity/Packages/manifest.json`, which every gate reads, so sim, words, kit, world and the 774 s world rebuild all ran. That is a one-time cost of adding the packages; the next `--sky` goes back to the delta.

## Files
**New**
- `unity/Assets/Suborned/Runtime/Play/Juice109.cs`: the switch and the facade. It holds no package type, so the host logic's dotnet never loads Cinemachine.
- `unity/Assets/Suborned/Runtime/Play/Juice109Cam.cs`: Cinemachine brain, Do Nothing camera, Impulse, vignette.
- `unity/Assets/Suborned/Runtime/Play/Juice109Fx.cs`: dust, pulse, exhaust, tyre VFX, flares, the follow, `-juicedemo`.
- `unity/Assets/Suborned/Runtime/Play/HostJuice109.cs`: the host's hooks.
- `unity/Assets/Suborned/Runtime/Play/HudToolkit.Juice109.cs`: the ring pop, the money and Trail ticks.
- `unity/Assets/Suborned/Editor/World/Play/Juice109Assets.cs`: the Resources/Juice109 materials, ring sprite and tyre VFX copy, made at build.
- `unity/Assets/Suborned/Editor/World/Play/Juice109Authoring.cs`: the authoring test.
- `unity/Assets/Suborned/Generated/Juice109/`: `Juice_Glow.shadergraph`, `Juice_TyreSmoke.vfx`, and the `Test/` v1–v3 graphs and v1–v2 VFX.
- `tools/juice/author.ts`: writes every graph; `--check` reports any drift.
- `tools/game/hostlogic/HostLogic.Juice109.cs`.
- `briefs/109/latch/`: `round1.md`, `shots.json`, `perf.json`, `clips.ts`, `authoring.json`, `costs.json`.

**Changed**
- `unity/Packages/manifest.json`, `packages-lock.json`, `unity/ProjectSettings/VFXManager.asset`: the two packages.
- `unity/Assets/Suborned/Runtime/Suborned.Runtime.asmdef`: references `Unity.Cinemachine`.
- `HandoverHost.cs`: two inline calls, `JuiceLog` in Drain and `JuiceOrder` when an order is taken. The line count is unchanged.
- `HudToolkit.Ring.cs` and `HudToolkit.Clock.cs`: one call each.
- `Editor/World/Play/Look097Materials.cs`: `Juice109Assets.Ensure()` at build.
- `tools/game/runtime-compile.ts` (Splice's): `Unity.Cinemachine` added to the reference list.
- `tools/game/proofmap.json` (Splice's): a row for the juice's files.
- `.gitignore`: `Resources/Juice109`.

## Open
- **The follow and GDD §10's "the view never switches by itself".** It doesn't switch views: it is the same overhead camera, easing its centre after the car the player has just sent off. It stops for good at any camera input. Aster or Sky should say whether that is inside the rule. If not, a click on the car can start it instead.
- **The ring pop shows only in play.** Scripted clips give orders without the ring, so no clip shows it.
- **Next owners.**
  - Glyph: review the HUD motion (the ring pop, the figure bumps) against the kit.
  - Chime: sounds for the three moments (brief 109's own note).
  - Splice: the two small edits in their tools (`runtime-compile.ts`, `proofmap.json`).
- **Proofs.** Once Sky adopts the juice, the batch's proofs should launch with `-juice on`, so frame-time gates measure it.
- **Before rounds 4–8.** The order dust is faint on the cream paving even after darkening: Sky to judge. The flares are white discs, readable but plain. A flare sprite would be the next step if Sky wants more.
