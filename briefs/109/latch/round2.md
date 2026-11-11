# Brief 109 · Juice · round 2 (Latch, 2026-11-11)

Sky's notes on 0.2.325, and what was done about each:
- "lose camera shake": done.
- "not even sure if screen should darken on take down…since that can happen off camera": done. "I do like our mini bobble head action PIPs": the takedown's juice now lives on the pip.
- "the cars headlights don't align properly when it drives off": fixed.
- "in that example it drove through the rivals car": fixed.

**The build for Sky:** **0.2.326**, `out/build/play/The Suborned.app`. `bun run batch --sky`: BATCH OK, logged in runs/builds/builds.json.

## 1. No camera or screen-wide effects for events
- **Removed:** the Impulse shake and the vignette pulse are gone, and so is `Juice109Cam.cs` (brain, Do Nothing camera, Impulse source, vignette Volume). Nothing in the juice now touches the camera or the whole screen.
- **Cinemachine: removed.** Nothing else in the project used it, checked with `rg Cinemachine` over `unity/Assets` and `tools`. `com.unity.cinemachine` is out of `manifest.json` and `packages-lock.json`, with its only dependency `com.unity.splines`. The `Unity.Cinemachine` reference is out of `Suborned.Runtime.asmdef` and `tools/game/runtime-compile.ts`.
- **What's left of the round-1 package work:** VFX Graph (the tyre smoke) and the Shader Graph glow.

## 2. The takedown's juice on the bobble-head pip
- **Where it lives:** the done chip (brief 101, `HudToolkit.Gains`): the doer's cameo with the outcome badge, then the facts ("Inside · Out · Wakes 120 s"), over the thing that changed.
- **When it lands, on screen:**
  - the cameo pops from 0.35 to 1.28 with a −14° to +8° tilt, then settles to 1 with an overshoot (about 0.4 s);
  - the facts then settle in one by one: fading up and rising 8 px, 70 ms apart.
- **When it lands off screen** (no chip, so the feed card carries the beat), the doer's **rail portrait** bumps instead: scale 1.24 with a −7° tilt, then an EaseOutBack settle. The rail is what always shows the agents.
- **How it's built:** UI Toolkit's own transitions (scale, rotate, opacity, translate). Only a takedown's chip does this, since Sky hasn't ruled on the other moments. A reused chip element is put back at rest.
- **Files:** `HudToolkit.Juice109.cs` (`JuiceChip`, `JuiceRail`), plus one call each in `HudToolkit.Gains.cs` (`ChipFill`) and `HudToolkit.Done.cs` (`DoneMarks`).
- **Frames:**
  - `r2-takedown-chip-2x.jpg`, the chip at 2x from frame 29 to 47, before row over after row. The after cameo arrives small at 32, pops at 35, and the facts settle in from 38. The before chip appears whole at 32.
  - `r2-takedown-rail.jpg`, the rail with the view on the Scout far off. The Enforcer's portrait pops at 34–37 and settles by 40; the before row has no pop.

## 3. The headlight flares, locked to the lamps
**Cause:**
- The getaway car's view is a root that only moves. Its heading is applied to the child `model` (`HostViews.Cars`: `carModels[key].rotation`).
- Round 1 placed the flares in the **root's** frame, from bounds measured once at rest. So they never turned with the car.
- The "front" was then guessed from the direction of travel, which is why they showed on its back.

**Fix:**
- **Where the lamps are:** at build time `Juice109Assets.Lamps` reads the getaway sedan's own geometry: the `DK_Lamp` sub-mesh of `DK_Sedan_Getaway.fbx`, the front half, split left and right, the upper cluster on each side (the headlamp; the parking lamp sits below). It writes the two lamp centres to `Resources/Juice109/lamps-DK_Sedan_Getaway.txt`.
  - Found: **(±0.627, 0.644, 2.437) m** in the model's frame.
  - That matches the DecoKit source: `cars070.py` puts the lamp pods at x ±0.66 and height 0.70 in model units × 0.92 scale, at the nose.
- **Parent:** the flares are **children of the drawn `model`**, 4 cm proud of the lens. So the transform hierarchy moves and turns them with the car, with nothing to keep in step.
- **Update order:** they turn to face the camera in `LateUpdate`, after the host's frame has set the body's pose. Their size is kept on screen against the body's scale.
- **Rebuilds:** they are recreated if the view rebuilds the car.
- **Tyre smoke and exhaust** use the same body frame (front +Z, per the kit's `front` field).

**Checked frame by frame:** `r2-flares-turn-2x.jpg` is the car tracked by its roof every 6 frames (frames 100–184) through the pull-out turn, from facing up the screen to up-left, cropped at 2x. Both flares stay on the two headlamp pods at the nose corners in every frame.

## 4. The getaway drove through the rival car: view-only, fixed here
**Cause: the view, not the engine.**
- The engine ends the mission on the Go's tick and steps no more. The getaway's drive-off is then drawn by the view (`HostViews.Leaving`, brief 079): the grid's route at that moment, then 110 m straight on, with **no check against other cars**.
- In route-a-board the beaten crew's wheelman starts driving off at 2523 (`rivals.drives-off`; the engine calls our way `car.unboxed`). The Go lands at 2534. The rival car therefore froze about a second into its own drive-off, still across our bay mouth, and ours was drawn straight through it.
- The engine's own rules were right: it was a moving car, and the box-in rule of brief 105 rightly called us unboxed. So there is nothing for Keel.

**Fix, view-only, "walkable as seen" for cars:**
- **The rival car drives on out** (`HostViews.LeavingRivals`, new). When the clock stops while it is leaving (`CrewCar.Leaving`), the view carries on its own leg: the grid's route from where it stands to its entry (the same `CarRoute` the engine's `CarRouteP` uses to `S.RivalCarEntry`, read only), then straight on out of view, from a rolling start up to the street's speed. Its heading follows the way (`RivalCarFacing`).
- **Our drive-off drives as a driver would** (`HostViews.LeavingSteer`, new).
  - **Looking ahead:** each frame it looks 25 m down its way (more at speed) for the other drawn cars. Each car is a capsule from its own model's bounds and pose.
  - **Steering round:** if a car is in the way, it eases sideways to the nearest offset (up to 4.5 m) that clears it for the whole look-ahead, swerve included. It moves sideways only where the drive grid has room for the car (`Drive.ClearanceAt` at least its half-width; not out of its bay, not into a kerb), and harder from rest (0.9 m per m) than at speed (0.3 m per m). The heading turns with the swerve.
  - **Waiting:** if nothing clears, it brakes to a stop short of the car and waits.
  - **Cars alongside:** a car already alongside at the start blocks only if the way would bring us closer still.
  - **Returning:** it goes back onto its way once that is clear. No fade, no cut. The engine's car and the hash are untouched.
- **In the clip** (the log says the same): the rival car drives on out ("drives on out, 160 m"). Ours waits behind it at the bay mouth ("wait for car/rivals at 0.0 m"), then pulls out once it has gone and leaves. Nothing passes through anything.
- **The steering is a world fix, not juice.** It runs with `-juice off` too, so both halves of the new getaway clip show it. The old pass-through is round 1's `clips-r1/juice-getaway-side.mp4`.

## 5. The recorder's timestamps
- **Round 1's bug:** `clips.ts` derived each clip's rate from the ticks it covered. On the getaway the clock stops at the Go, so 360 frames over 0.6 s of ticks played in under a second.
- **Now:** each clip is encoded at its shot's own capture fps with constant timestamps (`-framerate`, `setpts=N/(fps*TB)`, `-r`).
- **Checked:** the getaway mp4s are 480 frames at 30/1 fps, 16.000 s; the takedown mp4s are 120 frames at 30/1 fps, 4.000 s (ffprobe).
- **The clips are at 30 fps**, from `briefs/109/latch/shots2.json`. Run with `bun briefs/109/latch/clips.ts <app> --shots briefs/109/latch/shots2.json`. Round 1's clips moved to `briefs/109/latch/clips-r1/`.

## Clips (before = `-juice off`, after = `-juice on`; `*-side.mp4` is before on the left, after on the right)
- `briefs/109/latch/clips/juice-getaway-side.mp4` (`-before`, `-after`): route-a-board 4:13.4, the Go.
- `briefs/109/latch/clips/juice-takedown-side.mp4` (`-before`, `-after`): route-g 2:37, the door guard's knockout on screen. The chip pops.
- `briefs/109/latch/clips/juice-takedown-offscreen-side.mp4` (`-before`, `-after`): the same knockout, viewed from the Scout. The rail portrait pops.

## Checks
- `bun tools/game/runtime-compile.ts`: compiles.
- `bun run hostlogic`: 208 pass, 0 fail. The `Juice109` cases dropped the removed `Takedown` call.
- `bun run typecheck`: clean.
- `bun test tools/game/proofwhat.test.ts tools/game/proofdelta.test.ts`: the proof map has rows for the two new `HostViews` partials, which sit on the car-leaving row (`cars`), and for `HostJuice109.cs*`. 68/73. The other 5, "the gate at the CLI", fail only because `Juice109Cam.cs` is tracked in HEAD but deleted in the work tree, so `cli.ts` keeps finding it "changed since the build". They should pass once the deletion is committed.
- Scratch builds 0.2.326-scratch: draw check ok each time.
- Clips: no exception in either pass.
- `bun run batch --sky`: **BATCH OK, 0.2.326.**
  - sim: 490/490 scenarios with identical replay.
  - words, compile, kit, world, unity-world and game-build: ok, with the smoke ok. Host logic: 208/0.
  - It took 1055 s, over the 600 s target: removing Cinemachine changed `manifest.json`, so every gated step reran, including the 782 s world rebuild.

## Files
**New**
- `unity/Assets/Suborned/Runtime/Play/HostViews.LeavingSteer.cs`
- `unity/Assets/Suborned/Runtime/Play/HostViews.LeavingRivals.cs`
- `briefs/109/latch/shots2.json`
- `briefs/109/latch/round2.md`

**Removed**
- `unity/Assets/Suborned/Runtime/Play/Juice109Cam.cs` (and its `.meta`)

**Changed**
- `Juice109.cs`: no Cam, no `Takedown`.
- `Juice109Fx.cs`: the flares on the body at the model's lamps, `LateUpdate`, the body frame for the tyre smoke and exhaust.
- `HostJuice109.cs`: no `JuiceLog`.
- `HandoverHost.cs`: the Drain hook removed; back to its pre-109 line.
- `HudToolkit.Juice109.cs`, `HudToolkit.Gains.cs`, `HudToolkit.Done.cs`: the pip pop and the rail bump.
- `HostViews.cs`: `RivalsLeaving()` called before `GetawayLeaving()`.
- `HostViews.Leaving.cs`: steering and reset hooks.
- `HostViews.Rivals.cs`: `RivalCarFacing` along its way out.
- `Editor/World/Play/Juice109Assets.cs`: `Lamps`.
- `Suborned.Runtime.asmdef`, `unity/Packages/manifest.json`, `unity/Packages/packages-lock.json`.
- `tools/game/runtime-compile.ts`, `tools/game/proofmap.json`, `tools/game/hostlogic/HostLogic.Juice109.cs`.
- `briefs/109/latch/clips.ts`.

## Open
- **Glyph:** review the pip pop and the rail bump against the kit. Both are in HUD files Glyph reads (`Gains`, `Done`).
- **Popovic's lent car (`HostViews.LeavingLent`, brief 096):** its drive-off has the same no-check shape as ours had. It isn't steered yet because it's outside this round's ask. It's the same `LeaveSteer` if Aster wants it.
- **The other moments** (orders, getaway juice beyond the fixes) are untouched, pending Sky.
