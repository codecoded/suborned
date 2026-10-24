# Brief 048: draw distance (haze, far clip, LOD)

**Objective:** third person (and any long perspective view) shows the district crisp to the horizon; Sky: "nothing seems to have that fog / haze effect". Add distance haze so far buildings melt into one haze colour from ~60 m, and let the far clip and LODs follow the haze.
**Owners:** Glint (Haze, below) · Redline (Perf: far clip, LOD, cost; section below, to be written) · Latch (compile, apply, in-game check).
**Done-check:** third person at the Handover shows a soft street end; the play camera is unchanged at zoom 14 (no fog, signals as drawn); fog end matches the far clip; frame rate at play zoom unchanged.

## Haze (Glint, 2026-10-22)

**Why it was crisp.** The scene fog is linear 1100 to 7500 m (light.json `fog`), set for the horizon rings only; the district (<= 1 km) never met it. Third person also cuts at `HostFollow.followFar` 600 m, so the far street ends in a visible cut, not a fade. The play camera is orthographic, and URP's vertex fog reads clip depth, which an orthographic projection breaks (Metal reversed-Z: `max(near - z, 0)` / `(1 - z/near) * far`; fragment fog uses view depth and would fog the whole frame at a stand-off of 85 to 420 m). So fog must be off for the play camera whatever the scene says.

**Superseded 2026-10-22 (Sky: "try the atmosphere"): the follow haze below is now LINEAR 90 to 350 m, not exp2, and `haze.json` is `suborned.haze/2`; see "Old city atmosphere: built" at the end. The exp2 text and table stay as the record of the first design.**

**Design (look only; the sim, sight and replays read nothing from it).**
- **Perspective cameras with a short far plane (<= 900 m: third person, conversation and fail cutaways)** get exponential-squared haze: clear near, soft from ~80 m, one colour at the far clip. Fog colour stays the scene's haze colour (`light.haze` by day, `night.haze` at night, which LightPhase swaps), so fog, sky horizon, sea edge and far band are one colour (GDD 15). Colour is not duplicated in the new data.
- **Orthographic cameras (the play camera, zoom 14 at 38 degrees) and overlay cameras (the signals camera): fog off.** Play area exactly as built; fire lines, which draw on the overlay camera, are untouched.
- **Long views (flybys, free fly, far > 900 m)** keep the scene's linear 1.1 to 7.5 km fog, so the horizon rings (1.4 to 8 km, hazed 4 to 90 percent) still show. Exp2 at the ring distance would be 100 percent and erase the skyline.
- **One knob:** `farClip`, now **350 m** (Aster, 2026-10-22, after Redline measured the third-person cost between 250 and 450 m: 450 saves 1 %, 350 saves 17 %), with `reach` **0.97**: density = sqrt(-ln(1 - reach)) / farClip = 0.00535. Reach went 0.95 to 0.97 so the district is gone into the haze colour before the far plane cuts (3 % of the contrast left at 350, not 5 %; a dark tower at 5 % would still show a cut). **For Latch: `HostFollow.followFar` 600 to 350** (`unity/Assets/Suborned/Runtime/Play/HostFollow.cs` line 29, and the serialized `followFar: 600` in HarbourDistrict.unity and FightRange.unity; I did not edit them). `DistanceHaze.FarClip` exposes the haze value; they must stay equal. 450 can come back with LODs and the shadow fit: `farClip` 450, `reach` 0.95, `followFar` 450 together.

| distance | 40 | 80 | 100 | 150 | 250 | 300 | 350 |
|---|---|---|---|---|---|---|---|
| haze, far clip 350 (in force) | 4 % | 17 % | 25 % | 47 % | 83 % | 92 % | 97 % |
| haze, far clip 450 (the first design) | 2 % | 9 % | 14 % | 28 % | 60 % | 74 % | 84 % |

Cost of the shorter clip: the street reads as haze from ~60 m, not ~80 m, and the painting-like depth is gone by mid-street; the haze is heavy enough that the far end of a long avenue is a flat cream band. That is the price of the 350 m frame budget; it is the right look for a hazy 1950s street and keeps one haze colour. The horizon cards and ring (1.4 km and beyond) sit past the 350 m far plane in third person, so third person shows sky and haze only above the district; that was already true at 600 m. Redline's open question (let the cards fade, or a layer past the clip) stays open: it needs a layer split, and the cards would have to be fogged to the same haze colour.

(History: far clip 600 (density 0.0029) was gentler and 450 was my first choice; both cost more draw than the frame allows. 350 is Aster's call.)

**Data and code (not wired into a scene; nothing to rebuild).**
- `unity/Assets/Suborned/Resources/Atmosphere/haze.json` (`suborned.haze/1`): the settings. Unknown schema, mode, or out-of-range values are rejected with an error and the scene keeps its fog.
- `unity/Assets/Suborned/Runtime/DistanceHaze.cs`: installs itself after the first scene loads (`RuntimeInitializeOnLoadMethod`), swaps fog per camera in `beginCameraRendering` and restores in `endCameraRendering` (the pattern TacticalShadows and ArchitectureFade use). Compile-checked here against the Unity 6000.6.2f1 assemblies with `dotnet build` (0 errors); NOT run: the editor is Latch's.
- Not built, on purpose: a height term (haze lying low so tower tops stand clear). It needs a depth pass per frame; ask Redline if the budget has room.

**Pictures (a MOCK; no render was possible with the editor in Latch's hands).** Depth is estimated from the screenshot (ground plane analytic: camera ~2.5 m up, 55 degree fov; standing masses by hand), fog applied in linear light toward `#f3e8d4`. Read it for the look and the amounts, not the pixels.
- `briefs/048/glint/before-after.png`: build 0.2.65 as played, and with the haze at far clip 350 (density 0.00535).
- `briefs/048/glint/after-exp2c.png` (chosen, far clip 350), `after-exp2b.png` (far clip 450, the first design), `after-exp2.png` (far clip 600), `after-lin.png` (linear 80 to 600, the flat alternative); `mock.py` makes them.
- `briefs/048/glint/signals-under-haze.png`, `signals.py`: the signals fogged by distance (`signals.py`, now at far clip 350, reach 0.97).
- References: `briefs/008/concepts/the-suborned-city-hero-retrofuture-v2-2026-09-26.webp` (far towers pale cream, low contrast, one haze band), `briefs/007/concepts/AtomicAge/contact_sheet.jpg`.

**Signals (measured, `signals.py`).** The play camera takes no fog, so at zoom 14 cones, guard and rivals draw exactly as before. In third person the signals drawn on the main camera (cone lamps, `Unlit` materials) fog like everything else: dE to the unfogged colour 2 to 3 at 20 m, 8 to 10 at 40 m (about the grade's own shift, 7.2 / 5.0 / 3.3), 23 to 29 at 80 m, 45 to 62 at 150 m; red stays red (hue shift <= 2 degrees to 80 m, 8 at 150 m), contrast to the haze is >= 1.6 to 80 m (red 2.5, violet 2.7, amber 1.6) and 1.3 (amber) to 1.7 (violet) at 150 m, and amber is the weakest. (At far clip 450 it was dE 15 to 18 at 80 m and amber 1.5 at 150 m: the 350 clip costs about a fifth more fade.) Fire lines draw on the overlay camera and are exempt. Decision for Sky/Aster: signals past ~60 m in third person are not actionable (the commander view is where they matter), so I leave them in the haze; if Sky wants them clear at any distance, Latch swaps the signal materials to a fogless unlit shader (I can write it; it is a shader change, not a camera one).

**Open for Latch (check in game, cannot be done here):**
1. Third person: the street end softens; the sky horizon matches the far haze (no band).
2. Play camera at zoom 14: unchanged (compare a proof shot with the previous build).
3. That `RenderSettings.fog` set in `beginCameraRendering` takes effect per camera in this URP (pattern proven for shadows, not yet for fog): switch to third person and back; the signals camera shows no haze.
4. Night phase: the swapped fog uses `night.haze` colour.

**Shadow fit sign-off (Glint, 2026-10-22; Redline's item 2: ~80 m, 2 cascades, for the follow view).** Checked against the look by reasoning on the mock's fog table, not rendered (editor is Latch's). A ground shadow keeps (1 - haze) of its contrast: 83 % at 80 m, 75 % at 100 m, 66 % at 120 m, 53 % at 150 m. So a shadow cut at 80 m would still show as a hard line on the avenue, with 83 % of the shadow's contrast, and the long soft shadows are a main part of the look (GDD 15). **I accept 100 m, 2 cascades, as the floor; 120 m is what I want.** Conditions for Latch: (1) split the first cascade near 20 to 25 m so the agent and nearby shadows keep their sharpness (the stated gain), the second to the end; (2) a soft fade over the last ~30 % of the distance (URP `Last Border` / shadow fade, so the shadow thins out between about 70 and 100 m, not a cut), which the haze then covers; (3) shadow casters beyond the distance still cast onto receivers inside it (check that tall far towers' shadows on the near street are not lost: that is the visible failure; if they are, use 120). Redline measured 80 m only; 100 and 120 m need one count each (casters, ms) before this closes. Latch checks in game: third person facing 90 down the long avenue and the tp-street heading, looking for a shadow edge that ends in mid-street. Shadows only; this changes nothing for the play camera, which keeps its own 4 cascades (TacticalShadows).

## Perf (Redline, 2026-10-22)

**Short answer.** Sky is right about third person. It draws the district out to 600 m with no fog and no LOD cull: 3,400 to 6,100 renderers and 9.7 to 17.5 M triangles a frame, where the play view draws about 400 renderers and 1.7 M triangles. About three quarters of what third person draws is more than 150 m away. Shadows add the rest: the follow view inherits the asset's 260 m single cascade. On the Studio every view still runs at 4 to 9 ms. On the Air the GPU is the limit, and by estimate third person runs at 30 to 55 fps. The play view itself sits at the edge (14 to 18 ms estimated): draw distance is not its problem, but it still has to be measured on the Air.

### How it was measured
- **Player:** 0.2.65 as built (`out/build/play`), never the editor. The built player's flags give frame times but no draw counts and no way to aim the follow view, so `briefs/048/probe/` builds a small DLL against the player's own assemblies and loads it into a **copy** of the app (`out/redline/048/app`; `run.sh setup`). The probe:
  - aims the follow view (walks the cell with engine move orders, then `FollowFace`);
  - reads FrameTiming (GPU, main and render thread) and every Render-category ProfilerRecorder counter;
  - counts what each view draws, LOD-aware and static-batch-aware. Its camera-only triangle count (5.99 M) matches the player's own counter with shadows off (5.99 M).
- **Settings:** `-novsync`, HUD on, clock running. Each window is 6 s after a 1.5 s settle.
- **Sizes:**
  - 1920 x 1080;
  - 2880 x 1864, the Air's panel, render scale 1. This is the main column;
  - native 6016 x 3384, with the render cap at 0.5 (3008 x 1692).
- **Views:**
  - **play:** the opening, zoom 14, at yaws 30, 120, 210 and 300;
  - **tp-street:** Sky's screenshot reproduced. The cell walks 20 m south of the start, faces 180, and the teal tower and the long palm street are in frame (`briefs/048/runs/tp-street.jpg`);
  - **tp-worst:** 32 headings were searched (`runs/cand1`). This one is start + (-10, -25) m, facing 90, down the long avenue (`runs/tp-worst.jpg`).
- **Noise:** the Studio is shared, so GPU ms vary by about ±1 ms between repeats (tp-worst GPU read 6.2, 6.9 and 7.5 in three runs). Runs started only when no other player and no CPU burner was running (`probe/series.sh` logs this). Rows that overlapped another player are left out. Counts do not depend on noise.
- **Builds:** the culling and far-clip variant sets (`*-tp2`, `*-tp3`) ran on 0.2.67, which Latch built while I was measuring. The world is the same: baselines match 0.2.65 to within 0.3 ms and to the exact renderer and triangle counts.

### Numbers (Studio, M5 Max)

| view (2880 x 1864) | frame ms (mean / p99) | GPU ms | main / render thread ms | triangles (camera + shadow) | SetPass | shadow casters | renderers drawn | of them beyond 150 m |
|---|---|---|---|---|---|---|---|---|
| play, opening (4 yaws) | 4.1 to 5.1 / 5 to 10 (one window 6.6 with a 62 ms hitch) | 3.3 to 4.1 | 4.2 to 5.0 / 1.2 to 1.3 | 1.5 to 1.7 M | 100 to 110 | 500 to 690 | 350 to 370 | (orthographic) |
| tp-street | 5.4 to 6.0 / 7.6 to 12 | 4.6 to 4.9 | 5.6 to 6.0 / 2.9 to 3.2 | 9.7 M (camera 5.5) | 228 | 2,611 | 3,368 | 2,546 (76 %) |
| tp-worst | 8.1 to 8.5 / 9.7 to 11 | 6.2 to 7.5 | 8.4 / 4.3 to 4.6 | 17.5 M (camera 10.5) | 426 | 3,446 | 6,081 | 4,377 (72 %) |

- **1080p:** play 5.3 to 5.8 ms, GPU 2.9 to 3.7; tp-street 6.4 ms, GPU 3.9, 10.7 M triangles. Both 1080p runs overlapped another player (a proof run), so read their times as upper bounds; the counts hold.
- **Native 6016 x 3384 (capped):** play 4.5 to 5.0 ms, GPU 3.5 to 4.7; tp-street 5.2 ms, GPU 4.7. The render cap holds the 3D pass near the Air's pixel count, so native costs about what the Air's size costs.
- **On the Studio the frame is main-thread bound:** main ≈ frame. Every view is under 9 ms, so draw distance alone does not make the Studio drop below 60.
  - Sky's sessions tonight (Player.log: 20:15 and 20:22, 6016 x 3384, vsync) overlapped crew load. Splice's 12 `yes` CPU burners ran from 20:19:40 to about 20:31, together with late-fight player runs. Latch's proof players were also running, as was a `dotnet` batch at about 800 % CPU.
  - Hitches tonight are as likely to come from that load as from the game. **Ask (Aster): no load tests or player batches on the Studio while Sky plays.**
- **Air estimate:** Studio GPU ms x 4 to 4.5 at the same pixel count (`research/sources.md`). This is not evidence; the Air must be run.
  - play: 14 to 18 ms;
  - tp-street: 18 to 22 ms (45 to 55 fps);
  - tp-worst: 25 to 34 ms (30 to 40 fps).

### What is set today
- **Far clip:**
  - follow view 600 m (`HostFollow.followFar`);
  - play camera max(far, stand-off + 1500) = 1,683 m. It is orthographic, so this adds nothing visible.
- **Fog:** linear from 1,100 to 7,500 m, so it does nothing inside 600 m (Glint's section).
- **LODGroups:** 9,049 groups, on 23,260 of the 29,803 active renderers.
  - **9,030 of them never cull:** KitLods by design, "the last level never culled". Only 19 have a cull level.
  - Combined facade tiles (`batches/*`, static) have no LOD.
  - **`DK_WashingtoniaPalm_Street` has no LOD at all:** 803 placed at 19,190 triangles each, 15.4 M in total. That is 59 % of the triangles beyond 150 m in tp-street (129 palms, 2.5 M) and 61 % in tp-worst (242 palms, 4.6 M).
  - The 11,434 crossing pieces are tiny (about 30 triangles), but 1,000 to 1,500 of them are drawn far away in every street view.
- **Occlusion culling:** none. The scene has no baked data (`m_OcclusionCullingData: 0`). The GPU Resident Drawer's GPU occlusion is off, and its small-mesh cull is 0 %.
- **GPU Resident Drawer:** on (Instanced Drawing). Static batching is also on (the player default): 5,745 renderers are static-batched, 1,487 of them inside LODGroups. The drawer does not instance those, and Unity advises turning static batching off with it.
- **Shadows:**
  - The play view fits 4 cascades to its stand-off (TacticalShadows).
  - The follow view switches that off, so it gets the asset's **260 m, 1 cascade, 4,096 px**: about 6 cm per texel near the agent. It casts 2,600 to 3,400 shadows, and the shadow pass is 41 to 45 % of all triangles (tp-street 4.2 M of 9.7 M; tp-worst 7.0 M of 17.5 M).
- **Layers:** the whole world is on `Default`, so per-layer cull distances are not possible today.

### What each change buys (measured, tp-worst at 2880 x 1864 unless marked)

| change | frame ms | GPU ms | triangles | renderers drawn |
|---|---|---|---|---|
| as built | 8.29 | 6.85 | 17.5 M | 6,081 |
| far clip 450 (Glint's haze far) | 8.22 (−1 %) | 6.13 | 16.0 M | 4,959 |
| far clip 350 | 6.91 (−17 %) | 5.41 | 13.9 M | 3,743 |
| far clip 250 | 5.81 (−30 %) | 4.71 | 11.7 M | 2,781 |
| shadows 80 m, 2 cascades | 7.93 | 5.78 | 12.8 M | 6,081 (casters 3,446 → 1,007) |
| far 300 + shadows 80 m / 2 cascades | 6.04 (−27 %) | 4.90 | 8.0 M | 3,112 |
| far 250 + shadows 80 m + LOD bias 0.5 | 5.33 (−35 %) | 4.25 | 6.0 M | 2,768 |
| cull level: pieces < 8 m cull below 1 % of screen height, larger below 0.4 % | 7.28 (−14 %) | 5.90 | camera 9.2 M | 3,882 |
| GRD small-mesh cull 3 % | 7.49 (−12 %) | 5.82 | n/a | n/a |
| GRD GPU occlusion on | 11.52 (**+36 %**) | 7.81 | n/a | render thread 4.6 → 7.7 |
| GRD off | 10.75 (**+32 %**) | 7.88 | 17.5 M | render thread 4.4 → 6.5 |
| GRD off, **play view** (3 runs) | 3.3 to 3.9 (main thread 4.2 to 5.2 → 3.3 to 3.8) | n/a | n/a | n/a |
| LOD bias 0.5 / 0.25 | within noise | n/a | −12 % / −20 % | n/a |
| SSAO off | within noise on the Studio | n/a | n/a | n/a |

### Plan, ranked
1. **Far clip and haze in third person: Glint (the look) and Latch (wiring).**
   - Glint's 450 m haze far buys almost nothing (−1 %): the cost is in what stands between 250 and 450 m.
   - Perf asks for **`followFar` 300 to 350 m, with the haze's `farClip` set to match** (Glint's one knob). At 350, reach 0.95 gives density 0.00495: 15 % haze at 80 m, 42 % at 150 m.
   - If Glint and Sky want buildings visible to 450 m, the small pieces must stop earlier (item 3). Then 450 costs about what 300 does.
   - The skyline cards (`HarbourDistrict/horizon`, 523 renderers, no shadows) need a decision: let them fade with the haze, or draw them on their own layer past the clip. That takes a layer split.
   - Conflict to settle (Aster, with Glint): 450 for the look, against ≤ 350 for the frame.
2. **A shadow fit for the follow view: Latch (Glint signs off the look).**
   - Instead of the asset's 260 m single cascade: about 80 to 100 m, 2 cascades.
   - Casters drop from 3,446 to about 1,000. With far 300 the frame is 8.29 → 6.04 ms and triangles 17.5 → 8.0 M.
   - It also sharpens near shadows. On the Air this goes straight to GPU time.
3. **LODs where the far triangles are: Vela (palm LODs) and Latch (KitLods cull level).**
   - Give `DK_WashingtoniaPalm_Street` LOD1 and LOD2 (≤ 4 k and ≤ 800 triangles) and a cull level. It is 60 % of far triangles in both third-person views, and a shadow caster.
   - KitLods: give small pieces a cull level (≤ 8 m at about 1 % of screen height), not "never culled". Crossings, lamps, bollards, benches and signs then stop being drawn hundreds of metres away. Measured: drawn 6,081 → 3,882, −14 % frame.
   - The play view is unaffected: at zoom 14 every piece is far above 1 % of the screen.
   - The combined facade tiles (`Combine.cs`, static) have no LOD. Impostors or a merged far mesh for blocks beyond about 250 m are a later step (Vela/Latch), after items 1 to 3.
4. **The GPU Resident Drawer's per-frame rebuild (brief 045 F6): Latch.**
   - Turning the drawer off saves 0.4 to 1.9 ms of main thread in the play view, but costs 2.6 ms in third person. So keep it, and find what toggles renderers every frame.
   - Also try static batching off (Player Settings), so the 5,745 static-batched renderers are instanced by the drawer; measure both views.
5. **Occlusion: Latch, later.** The drawer's GPU occlusion made third person 36 % slower on the Studio (render thread +3 ms): leave it off. A baked occlusion pass in `HarbourWorldBuilder` (street canyons hide most far blocks) is worth one experiment after items 1 to 3. Re-test the drawer's occlusion on the Air, where the GPU is the limit.
6. **Not draw distance, but the Air's next limit: Glint and Latch.** The play view's estimated 14 to 18 ms on the Air sits on the line. Brief 008 B6 measured full-resolution SSAO at 24 to 35 % of the frame on the Air: SSAO from depth, or at half resolution, is the next candidate once the Air is measured.

### Perf row for the batch: `tp-long` (Splice: tools/game rows and gates; Latch: the hook)
- **Views:** the follow view behind the Scout at tp-street and tp-worst (above), clock running, HUD on. 2880 x 1864, `-novsync`, 1.5 s settle and a 6 s window each. Add the play view at the opening for comparison. About 40 s of player time.
- **Report per view:**
  - mean, p99 and max ms;
  - GPU, main-thread and render-thread ms (FrameTiming);
  - the Render counters `Triangles Count`, `SetPass Calls Count` and `Shadow Casters Count`;
  - renderers drawn and those beyond 150 m (as `probe/Probe.cs` `Census`).
- **Gates on the Studio:** counts, which do not depend on machine noise, and a frame time with headroom.
  - Third person: triangles ≤ 6 M, SetPass ≤ 375 (350 until build 0.2.85: the DK_GlassSky window film added a transparent pass, tp-worst 332 to 351; Redline, 2026-10-23), shadow casters ≤ 1,200, renderers drawn ≤ 2,500 (≤ 1,500 beyond 150 m), mean ≤ 8.3 ms, p99 ≤ 16.7 ms.
  - Play view: triangles ≤ 2.5 M and mean ≤ 6 ms.
  - Today tp-worst fails every count (17.5 M, 426, 3,446, 6,081, 4,377), as it should.
- **Gate on the Air (the real limit, run when Sky allows a copy there):** mean ≤ 16.7 ms and p99 ≤ 20 ms, for the play view and both third-person views.
- **Hook (Latch, ShotRunner):**
  - a follow view by heading: `faceHeading` in degrees, beside `face`'s view key;
  - the counters above and the FrameTiming split in the `perf` window's result.
  - Until then, `briefs/048/probe/run.sh` (a copy of the app plus `Redline048.dll`) gives the same numbers. It is a measuring tool, not a batch step.

#### The row in the batch (Splice, 2026-10-22) and the player's hook (Latch)
`bun run batch` has the step `tp-long` (last). Files: `tools/game/tplong.json` (the three rows), `tools/game/tplong.ts` (file, gates, verdict), tests `tools/game/tplong.test.ts`. Until the built player carries the hook the step is **skipped, with the reason on the screen** (it looks for `suborned.tplong.report/1` in `Suborned.Runtime.dll`, as late-fight does for its marker); the batch goes on. Once the hook is there, today's tp-worst fails every count, as expected, until the draw-distance work lands.

- **Rows:** `play-opening` (overhead, yaw 30, zoom 14), `tp-street` (follow, scout, walk `[0,-20]`, faceHeading 180), `tp-worst` (follow, scout, walk `[-10,-25]`, faceHeading 90). Each: 2880 x 1864, `-novsync`, clock running, HUD on, 1.5 s settle, 6 s window (about 25 s of window, 40 s with the walks).
- **Command (the batch runs):** `"The Suborned" -tplong out/batch/tplong.resolved.json out/batch/tplong -novsync -logFile out/batch/tplong/player.log`. The resolved file is `suborned.tplong.resolved/1`: `outDir` and `rows[]` with `name`, `view` (`follow` or `overhead`), `kind`, `agent`, `walk [dx, dz]` (metres from the start, in world x and z; the cell walks there by engine move orders, as `Probe.cs` `Walk`), `faceHeading` (degrees clockwise from +z: direction (sin h, cos h) in x, z, as `Probe.cs` `FollowAt`; -1 for the overhead view), `yaw`, `zoom`, `settleSeconds`, `seconds`, `w`, `h`, and the constants `clockRunning`, `hud` true, `vsync` false.
- **Per row the player:** sets the size; walks; for a follow view calls `FollowFace` at `faceHeading` (the new `faceHeading` beside `face`'s view key; shots.json may take it too); unpauses; waits `settleSeconds`; then for `seconds` records every frame's unscaled delta, the FrameTiming means and the Render counters; then takes the census. It writes `<outDir>/tplong.json`.
- **Report `suborned.tplong.report/1`:** `{ status, build?, rows: [{ name, status: ok|failed, error?, screen: [w, h], faceHeading?, frames: { count, meanMs, p99Ms, maxMs }, gpuMs, mainMs, renderMs (-1 when the platform gives none), counters: { triangles, setPass, shadowCasters }, drawn: { renderers, beyond150 } }] }`.
  - `counters` are the window means of the Render-category ProfilerRecorders `Triangles Count`, `SetPass Calls Count` and `Shadow Casters Count` (camera and shadow passes together), as `Probe.cs` records them.
  - `drawn` is `Probe.cs` `Census`: renderers drawn this view, LOD-aware and static-batch-aware, and those more than 150 m from the camera.
  - `faceHeading` is the heading really faced after `FollowFace`; the verdict fails a row more than 2 degrees off, a size that is not the row's, or fewer than 100 frames.
- **Gates (tplong.ts `GATES`):** third person triangles <= 6 M, SetPass <= 375, shadow casters <= 1,200, renderers drawn <= 2,500 (<= 1,500 beyond 150 m), mean <= 8.3 ms, p99 <= 16.7 ms; play view triangles <= 2.5 M, mean <= 6 ms. On a noisy machine (load average over 0.6 per core while the row ran) the two frame-time gates are noted, not judged; counts always are.
- **Sky is playing:** the batch (and `bun run guard -- <command>` for load tests) waits while a player of his is open; see CLAUDE.md Rules and `tools/game/playing.ts`.

### Open
- **The Air:** none of the numbers above were taken on it (HANDOFF: Studio numbers are not evidence for the 60 fps limit). The probe or the row needs one run there, with the machine quiet, before and after items 1 to 3. That needs Sky's go-ahead to copy a build to the Air.
- GPU ms on the shared Studio are good to about ±1 ms. The rankings rest on counts and on the larger deltas.
- Evidence:
  - `briefs/048/runs/` (`measure.json`, `log.txt` per run; `inventory-0.2.65.txt`; screenshots `tp-street.jpg`, `tp-worst.jpg` and `overhead-open.jpg`, git-ignored);
  - `briefs/048/probe/` (`Probe.cs`, `probe.csproj`, `run.sh`, `series.sh`, `tab.py`; `python3 briefs/048/probe/tab.py air-worst` prints a run);
  - `briefs/048/research/sources.md`.
- Nothing in `unity/` was edited. The probe ran only in a copy of the app; the shipped `out/build/play` was not touched.

## Old city atmosphere (Glint, 2026-10-22; Sky: "the old city had fog / haze effect I believe as well - was a beautiful world")

**Answer in one breath.** The old city's beauty was not a heavy fog. It was a *stack*: a sky shader whose horizon is a wide warm sand-cream haze with a sun aureole, a gentle exponential fog in exactly that colour, a wide low-threshold warm bloom, a Gaussian far blur, a sun starburst flare, a baked painted far-city band in the sky, a pale mid-ground, an ACES grade that pushed saturation +25, and HDR energy above 1 in the sky and fog so the bloom caught them. Today's game has the sky gradient, a fog that does nothing inside 1.1 km, a small bloom and the grade; the third-person haze I wrote (exp2, 350 m) is a different, front-loaded curve and greys the near street. Port the stack by camera tier; the play camera takes only the grade.

**Pictures.** No engine render was possible (the editor is Latch's), so: **old** = Sky's two stills, plus the old project's own renders (`out/old-world/unity/Scratch/Captures/DecoKit/`, copies in `briefs/048/glint/old/`); **today** = real captures of build 0.2.65 (`briefs/048/runs/`) and the 2026-10-19 baseline (`briefs/026/baseline/skyline-aerial.png`); **proposed** = MOCKS (`briefs/048/glint/atmos_mock.py`): today's capture with estimated depth (ground plane analytic, standing masses by hand; the harbour view is a rough row-to-depth map, its sky and sea recoloured by hand), fog in linear light, bloom, grade. Read them for look and amounts, not pixels. The old flyby video itself is not on disk (only its 17 s deck cut and the reel stills); the old project's renders are.
- `briefs/048/glint/board-boulevard.png`: old "The boulevard" | game today (third person, long avenue) | mock of `haze.json` as written now | mock of the proposal.
- `briefs/048/glint/board-harbour.png`: old "Out over the harbour, looking back" | game today (aerial baseline; not the same pose) | proposal mock | proposal with the old teal sky (option B).
- `briefs/048/glint/board-playzoom.png`: old iso tactical | game today at play zoom | the play-camera proposal (grade only). The mock grade also touched the HUD pixels; in game the HUD is an overlay and is not graded.

### What the old city used (exact; sources in `~/Projects/the-suborned/game/Assets/TheSuborned/`, scene values read from the saved `DecoKitStreet.unity`)
Flyby = `Editor/DecoKit/DecoKitFlyby.cs`: the saved street, 58 degree fov, far clip 2500, frame by frame, 4096 shadow map with distance `clamp(y*8, 120, 1200)`, 2x supersample, MSAA off + SMAA, ffmpeg crf 17. The look comes from the scene `DecoKitStreetDemo.Build`.

| layer | old value | where |
|---|---|---|
| sun | directional, colour (1, .93, .8), intensity **3.0**, soft shadows, travel (.5, -.7, .5) = 44.7 degrees up, behind-left of the boulevard axis | StreetDemo `SunIntensity` |
| sky | custom shader "DecoKit/Space Age Sky": zenith sea-green (88,168,170), mid (127,218,208), **horizon haze (246,229,195)**, all x **SkyGain 1.5** (HDR), `_MidHeight` 0.95, **`_HazeHeight` 0.10** (haze = exp(-sin(elev)/0.10), a wide band), sun aureole (0.8, power 180) + haze **lifted toward the sun** (0.6, power 6), cirrus 0.3, ringed planet + two moons, **far-city band** | `Shaders/DK_SpaceSky.shader`, `DecoKitSky.Apply` |
| far band | `DK_Skyline_Far.png` 8192 x 512, -1 to +9 degrees elevation, three layers (700 / 380 / 150 buildings) each softened 3 / 2 / 1 px and paler with distance, stored as a ratio to the haze so it takes the sky's own horizon tint per azimuth; 1 texture sample | `sky_sheet.py`, `_SkylineBand` |
| fog | **exponential 0.0005**, colour = the sky horizon x gain, i.e. linear (1.16, 1.08, 0.92): **above 1, so bloom catches it**. 4 % at 80 m, 7 % at 150, 22 % at 500, 39 % at 1 km, 63 % at 2 km, 78 % at 3 km. (Was 0.0011, thinned: "the cream haze whitened the distance"). The iso play shots ran fog x0.35 | `DecoKitSky.Apply`, `DecoKitIso` |
| ambient | trilight (.46,.49,.54) / (.53,.48,.41) / (.60,.50,.37), fill 1.0, box-projected baked reflection probes every 40 m | `StreetDemo.Ambient` |
| exposure and grade | ACES, **post-exposure -0.8**, **contrast 10, saturation 25** (was 10: "colour study") | `DecoKitStage.Grade` |
| bloom | **threshold 0.8, intensity 0.6, scatter 0.85**, tint (1, .96, .9) ("diffusion filter": low-threshold wide glow) | `DecoKitLens.SoftFocus` |
| depth of field | Gaussian, far only: **start 300, end 1800 m, max radius 1.2**, high-quality sampling ("the distance goes soft") | `DecoKitLens.SoftFocus` |
| sun flare | `LensFlareComponentSRP`, occlusion on (0.02, 32 samples): 16-ray starburst texture (scale 16, 2.5), haze glow circle (7, 0.25), six hexagon ghosts (1.4, 0.2, spread 0.75), one far ghost (3.2, 0.05), intensity 1 | `DecoKitLens.SunFlare` |
| mid-ground | 943 pale streamline blocks on a 70 m grid, 344k tris, 56 chunks of 420 m, "only ever seen through haze"; the note itself says it pays off only in raised views | `DecoKitCityMass` |
| sea | `DecoKit/Ocean`: deep (.05,.32,.42), crests (.10,.55,.50), sky reflection lift, sun glitter, **per-pixel fog** (custom shader, so not the per-vertex fog error) | `DecoKitHarbour.Water` |
| AO and URP | SSAO on (intensity 0.9, radius 0.45, depth-normals), MSAA 4 in the asset, HDR on | `PC_Renderer.asset` |

Notes from the stills and renders (measured): the old far haze reads sand-gold, `#c0af87` to `#e5d5aa` (S 0.25 to 0.40), and the far city is *darker and more saturated* than a white haze would leave it; the old sea is deep teal-blue near (`#013546`, `#1c5258`) and pales only toward the horizon (`#adaa93`); the bright point at the avenue's end in "The boulevard" is a bloom on an HDR highlight, not the sun (the sun is behind the camera). Play zoom, old iso frame vs today (whole frame): mean luma 0.70 vs 0.68, **p5 0.46 vs 0.19** (the old shadows were lifted, warm), **mean saturation 0.34 vs 0.23**, mean rgb (192,179,133) vs (181,174,155). The old iso play shots had no meaningful haze (fog x0.35 over 150 m is 3 %) and no DoF (ortho depth is under 300 m): **the old play-zoom beauty was the grade, the warm ambient and the bloom, not fog.**

### What today's game has
- `light.json` (Glint 026 r026): sun 1.8, pitch 38 yaw 330 (the sun stands at azimuth 150, 38 degrees up); ambient trilight x0.69 / 0.71 / 0.41; ACES, post-exposure +0.15, contrast 6, saturation 18, **bloom 1.1 / 0.22 / 0.6**, vignette 0.1, colour filter white; no DoF, no flare, no sun disc; SSAO as the old renderer asset.
- Sky `Suborned/SkyGradient`: haze to mid over `midHeight` 0.16 with a smoothstep, `glowWidth` 40 / `glowStrength` 0.22; zenith `#3f86de`, mid `#8fc2ee`, **haze `#f3eee4`** (the contract's `palette_world.roles.haze` is `#f3e8d4`). In the 2026-10-19 baseline the sky an eighth of the way up reads **neutral grey-white (213,214,213)**, cooler than the cream city below it: the sky and the city do not share one haze colour today.
- Fog: **linear 1.1 to 7.5 km** (`light.json fog`): nothing in the district, 4 to 90 % on the horizon rings 1.4 to 8 km. The horizon cards (523 renderers) are cut by the 350 m far clip in third person.
- Third person: `haze.json` exp2, far clip 350 (written, not yet verified in game); play camera orthographic, fog off.
- Water: `Water.shader` reads pale teal-grey in the baseline aerial against the old deep blue.

### Why the first haze I wrote is the wrong shape (my correction)
exp2 to a 97 % reach at 350 m is front-loaded: 17 % at 80 m, 47 % at 150 m. Against the old curve (4 % and 7 %) it washes the mid street, and in the mock (`board-boulevard.png`, third row) the enamel and teal read greyed by 100 m. The old look kept the near and mid field saturated and let the haze build late. With a 350 m budget the honest compression is **clear to 90 m, then a linear ramp to the haze colour at the far clip**: 0 % at 80 m, 23 % at 150, 62 % at 250, 81 % at 300, 100 % at 350. Same fixed-function fog, no cost change, and a far clip that is still invisible.

### Proposed port (data: `briefs/048/glint/proposal/lens.json`, `suborned.lens/1`; nothing is in Assets, nothing wired, nothing compiled)
| tier and cameras | gets | does not get |
|---|---|---|
| **play** (orthographic play camera, overlay/signal cameras) | the grade only: `light.haze` fix below; saturation 22 to 25 and a warm shadow lift are an **A/B to shoot on the baseline stops** (old mean saturation 0.34 vs 0.23). Bloom stays 1.1 / 0.22 | fog, DoF, flare, any haze: the 38 degree down view never sees sky or the far band, so there is nothing for haze to do, and signals stay as drawn |
| **follow** (third person, conversation and fail cutaways; perspective, far <= 900) | linear haze 90 to 350 in the one haze colour; warm bloom 0.86 / 0.35 / 0.7; sky horizon band with the sun aureole and the haze lift toward the sun; the baked far band (below, instead of the cards); flare at 0.6 | DoF (nothing past 300 m to blur) |
| **long** (flyby, free fly, far > 900) | the old values whole: exp 0.0005, bloom 0.8 / 0.6 / 0.85, Gaussian far DoF 300 to 1800 (radius 1.2), starburst flare, the horizon cards or the band; the deeper sea | n/a (not a gameplay view) |

Concrete pieces, in the order they pay off:
1. **One haze colour, set to match the sky.** `light.haze` `#f3eee4` is near white (S 0.07) and is not the contract's `#f3e8d4`; the old city's displayed haze is sand-cream. Proposal `#ecd9b4` for fog, sky horizon, below-horizon, sea edge and far band, **Sky to confirm** (palette is tuneable; the signals draw outside it). Cost: nothing.
2. **Sky shader (Latch/Glint, `Suborned/SkyGradient`):** haze height 0.12 (exp falloff, not a smoothstep), sun aureole with the haze lifting toward the sun, optionally an HDR gain on the horizon so the bloom catches it. Drawn only where the sky shows (early-z): free at play zoom.
3. **Baked far band (the cheap port of the old far city):** the builder renders the *existing* horizon cards and ring once into an 8192 x 512 equirect strip at build time (deterministic, from the plan, nothing hand-painted; the old `DK_Skyline_Far.png` was a painted band and is not reused) and the sky shader samples it with the horizon haze of its own azimuth. One texture sample; every perspective camera sees a far city whatever its far clip (in third person the 523 card renderers are already outside the 350 m far plane, so the band brings the far city back at no draw cost). This also settles Redline's open question (cards fade vs a second layer): neither.
4. **Per-camera volumes, data only:** a runtime-built Volume profile from `lens.json` (bloom, DoF) on its own layer; the cameras' `volumeLayerMask` chooses the tier in `beginCameraRendering`, the same hook `DistanceHaze` uses. The sun flare is a `LensFlareComponentSRP` added to `RenderSettings.sun` at runtime. No scene edit. **Unverified: whether URP reads `volumeLayerMask` set in `beginCameraRendering` for that same camera (I believe `RenderSingleCamera` fires it before the volume update, but I could not run it); Latch checks, fallback is to toggle the volume weight.**
5. **DistanceHaze change (mine; I did not touch it because Latch's batch is compiling):** add `mode: "linear"` with `start` and `end` (= far clip) beside `exp2`; `haze.json` today is rejected if the mode is not `exp2`, so the file and the code change together.
6. **Sea (Vela/Latch):** deeper body and a Fresnel lift toward the haze with distance (old `_Deep (.05,.32,.42)`, per-pixel fog). Aster's note on the second still: saturated deep blue against the cream city. The mock recolours the sea by hand to show it; it is not an engine value.
7. **Sky colour, a decision for Sky:** the old city's sky was sea-green teal (zenith 88,168,170, mid 127,218,208: Sky's 1970s reference) against cream; today's is cobalt-cerulean. `board-harbour.png` row 4 shows option B. The contract's concept palette (`haze_teal #a7c2cb`, `sky_cerulean #719fb5`, `zenith_cobalt #296587`) leans to B. I did not change `light.json`.

### Cost against Redline's numbers (all estimates; none measured, the Air is not available to me)
- **Play zoom: 0.** No fog, DoF or flare; bloom and the grade unchanged (the saturation A/B is a colour change, no cost). Draw distance is untouched, so the 14 to 18 ms Air estimate for the play view does not move.
- **Third person:** linear instead of exp2 fog is free; the sky changes touch only visible sky pixels; the baked band adds one sample to the sky shader and no renderers (the cards are already outside the 350 m clip in Redline's rows, so his numbers do not change and no layer split is needed); the bloom change alters only threshold and intensity (the iteration count follows scatter and resolution; scatter 0.6 to 0.7 may add one half-resolution step: **measure**); the flare is one pass only while the sun is in frame. Far clip 350, shadow fit and LODs stay Redline's and Aster's calls.
- **Flyby and free fly:** DoF is the real cost (full-resolution CoC plus two half-resolution blurs, estimate 0.5 to 1.5 ms at 2880 x 1864); acceptable offline and in free fly, never in a gameplay view. Brief 008 B6's SSAO (24 to 35 % of the Air frame at full resolution) is a bigger item than any of this and stays Redline's open item 6.

### Not copied, on purpose
The 943-block mid-ground (344k tris, useless from the street, and the world already has its own massing); the old painted far-city PNG; Gaussian DoF in any gameplay view; saturation 25 and exposure -0.8 as constants (the 026 rounds tuned today's grade against Sky's "too bright" call: shoot A/Bs first); the old planet and moons (the sky is the GDD's call: "ringed planets and faded daytime moons").

### For Latch (nothing here edits a scene)
1. Apply item 5 and verify item 4's hook; shoot the third-person boulevard (heading 90, `tp-worst`) and the aerial baseline before and after.
2. Capture a real flyby stop with the `long` tier and the harbour stop looking back along the quay; those are the pictures to replace my mocks.
3. Report the bloom scatter cost on the Air, once, with the haze change.

### Open for Sky (via Aster)
(a) haze colour `#ecd9b4` (warm sand-cream) against today's near-white; (b) sky A (cobalt, today) or B (the old teal); (c) a play-zoom saturation A/B (22 to 25) on the baseline stops, the only change the commander view would get; (d) whether the long-view full look (DoF, flare) is wanted in free fly as well as the flyby.

## Old city atmosphere: built (Glint, 2026-10-22; Sky: "try the atmosphere")

**Built as data plus one runtime component. No scene edited, no editor opened on the game project.** Nothing needs wiring: both components install themselves after the first scene loads, like `DistanceHaze` did.

### What is in the tree
| path | what |
|---|---|
| `unity/Assets/Suborned/Runtime/DistanceHaze.cs` | rewritten: tiers (`TierOf`), fog per tier, `mode` linear / exp2 / exp. **Fixed a latent bug**: the component was added with no data, so `OnEnable` returned before subscribing and the haze never ran; it now fills in its data before it switches on (same fix in `AtmosphereLens`). |
| `unity/Assets/Suborned/Runtime/AtmosphereLens.cs` (+ `.meta`) | new: `AtmosphereSettings` (the toggles), `AtmosphereLens` (volumes, flare, sky inputs, far band hook). |
| `unity/Assets/Suborned/Shaders/SubornedSky.shader` | haze band, sun aureole, haze lift toward the sun, teal override, far band sample, all through `_Atm*` shader globals; all zero = the sky exactly as built. |
| `unity/Assets/Suborned/Resources/Atmosphere/haze.json` | `suborned.haze/2`: colour `#ecd9b4`; follow linear 90 to 350; long exp 0.0005; play none. |
| `unity/Assets/Suborned/Resources/Atmosphere/lens.json` (+ `.meta`) | `suborned.lens/1`: sky shape, teal palette, bloom per tier, far DoF, flare elements, play grade, far band slot. |
| `unity/Assets/Suborned/Resources/Atmosphere/flare_starburst.png` (+ `.meta`, new guid) | the old `DK_Flare_Starburst.png`, 1024 x 1024. |
| `unity/Assets/Suborned/Editor/World/light.json` | `light.haze` `#f3eee4` to `#ecd9b4` (a scene rebuild now agrees with the runtime; the runtime does not need the rebuild). |

### By tier (cameras are classified every frame by `DistanceHaze.TierOf`)
| tier | cameras | gets | does not get |
|---|---|---|---|
| **Play** | orthographic play camera (zoom 14, far 1683), every overlay camera (signals, fire lines), insets | **grade only**: saturation = the scene grade's own x 1.16 (ColorAdjustments 18 becomes 36.9), and a warm shadow lift (LiftGammaGain: c x 0.97 + 0.03, warm hue at 60 %, white stays white); both live in the grading LUT | fog (off, as before), haze, blur, flare, sky change. Signals draw on their overlay camera outside the grade, untouched. |
| **Follow** | perspective, far <= 900: third person, cutaways | linear fog 90 to 350 m in `#ecd9b4` (0 % at 80, 4 at 100, 23 at 150, 42 at 200, 62 at 250, 81 at 300, 100 at 350); bloom 0.86 / 0.35 / 0.7 tint `#fff0d6`; sky horizon haze band (`exp(-sin(elev) / 0.12)`), aureole 0.8 at power 180, haze lift 0.6 toward the sun, broad lobe 0.15; flare at 0.6; the sky's horizon and below-horizon colour is the fog colour | DoF; the saturation lift (third person keeps the scene grade) |
| **Long** | perspective, far > 900: flyby, free fly | exp 0.0005 fog; bloom 0.8 / 0.6 / 0.85; Gaussian far blur 300 to 1800 m, radius 1.2, high quality; flare at 1.0; the same sky; a camera made without post-processing (free fly) is given it, from its second frame | the grade change |

Night (`LightPhase.Night`) and the toggle off: nothing here applies (the scene's own fog and sky). The Scene view and probes are never touched (`CameraType.Game` only).

### Settings (a debug toggle, not a game control)
- **Sky colour:** cobalt is the default and is the scene's own sky, untouched. Teal (the old city: zenith `#2f93a8`, mid `#6ec6c4`, haze `#ecd9b4`) is switched with **Shift+F9** (remembered in PlayerPrefs `suborned.sky.teal`) or the command line `-sky teal` / `-sky cobalt`. No game binding uses F9 or F10.
- **Atmosphere on/off** (the A/B: off = the scene as built, no haze, lens, grade or sky change): **Shift+F10** or `-atmosphere off`; not remembered.
- `AtmosphereSettings.Status` is one line for a debug panel (Latch may add it to the F12 panel); `AtmosphereSettings.SkyTeal` / `.Enabled` can be set from a shot script.

### Checked here (no game editor, no scene)
1. **Compile:** `dotnet build` of `DistanceHaze.cs`, `AtmosphereLens.cs` and `LightPhase.cs` against the Unity 6000.6.2f1 assemblies: 0 errors, 0 warnings (`out/glint048/check/Check.csproj`).
2. **Shader:** the real Unity shader compiler on `SubornedSky.shader` in a scratch project (`out/glint048/shaderproj`, batchmode, URP 17.6.0): 0 messages; an injected undeclared identifier was caught on the same path, so the check discriminates (D3D11 front end; Metal is the same HLSL).
3. **Behaviour, in a scratch URP project** (Unity batchmode play mode, Deferred renderer, ACES grade 18 / bloom 1.1, linear colour space; a plane, seven blocks from 40 to 1000 m, a sun; four cameras rendered to HDR targets; **not the game's scene or renderer asset**): picture `briefs/048/glint/atmosphere-scratch-check.png`, values in the table below.
   - The hook order holds: `beginCameraRendering` fires before URP updates the volume stack and reads `RenderSettings`, so per-camera Volume weights, fog and sky globals set there apply to that camera. Log per camera: play: fog off, `lens/play` weight 1; follow: linear 90 / 350, colour `ECD9B4`, `lens/follow` 1; long: exponential 0.0005, `lens/long` 1. After the last camera the scene fog is back (`True Linear 1100`).
   - Atmosphere off renders with the scene's own fog (linear 1100 / 7500, `F3EEE4`) and the sky material's own colours. Toggling sky teal changes only the sky. The play grade changes the play frame (ground (0.855, 0.748, 0.622) to (0.887, 0.755, 0.581), the teal block more saturated) and nothing else in it. The follow frame gains the haze, the bloom and the sky band; the long frame gains the haze; a camera made with no post-processing renders with it (identical to the one that had it).
   - The sun flare draws (starburst, hexagon ghost chain, haze glow); the glow circle reads as a faint disc with a defined edge (fallOff 0.08 and 0.5 looked the same); if it reads as a coin in the game, set `flare.elements[1].intensity` to 0.
   - **Volume masks:** every camera in `HarbourDistrict.unity` (23 of 23), `FightRange.unity` and `SampleScene.unity` has `m_VolumeLayerMask` = Default, and the ones in the two game scenes have update mode `UsePipelineSettings` (the URP asset's: every frame). The lens Volumes are on Default, and **switch by weight, not by mask**: that needs no free layer and no change to any camera's data. Overlay cameras get weight 0 and no post (the signals stay outside the grade); the base camera's post uses the stack it updated at its own `Begin`.
4. Not checked (needs the game): the Deferred renderer of `PC_Renderer` with the real scene, the night phase, shot scripts, any frame time.

### For Latch: verify in the game (nothing here edits a scene; a scene rebuild is optional)
1. **Compile and console:** the new scripts and the shader import with no error; no `DistanceHaze:` or `AtmosphereLens:` error line in the log (a rejected json logs one).
2. **`HostFollow.followFar` 600 to 350** (field in `HostFollow.cs` and the serialized `followFar` in `HarbourDistrict.unity` and `FightRange.unity`), equal to `DistanceHaze.FarClip`; until then geometry from 350 to 600 m renders as flat haze colour.
3. **Play zoom, yaws 30 and 210:** compare a shot with the previous build, then Shift+F10 off and on. Expected: only colour moves (saturation, a warm lift in shadows); no fog, no flare, no bloom change; cones, guard and rivals look the same as before. The grade values are **start values**: if lit cream reads mustard or peach, `lens.json` `gradePlay.saturationPercent` 16 to 8 and `liftOffset` 0.03 to 0.02.
4. **Third person (`tp-street`, `tp-worst`):** the street end melts from about 90 m into the sand-cream haze; near and mid enamel keep their colour; the sky horizon is a wide warm band in the same colour as the far street, with no step; facing the sun (azimuth 150) a starburst and ghost chain. Switch to the play camera and back: the fog must not stick (the signals camera shows none).
5. **Flyby and free fly:** the long look: soft far blur, the flare, the exponential haze, the old bloom. **Free fly in a player build** (`SUBORNED_FREEFLY`): the exponential fog variant may be **stripped** (below); if the far haze is missing there, that is why.
6. **Night phase:** unchanged (the scene's fog colour `night.haze`, the night sky and grade, no flare).
7. **Teal:** Shift+F9 in third person and in a flyby stop; Sky compares cobalt (default) and teal.
8. **Fog variants in the player (important):** `ProjectSettings/GraphicsSettings.asset` has `m_FogStripping: 0` (Automatic), which strips the fog modes the built scenes do not use. The scene's fog is **Linear**, so linear survives (that is why follow is linear); the exp2 this file held before, and the long tier's exp, may be stripped from a **built player** even though the editor shows them. If long-tier haze is wanted in a player: set fog stripping to Custom with Linear and Exp kept (`m_FogStripping: 1`, `m_FogKeepLinear: 1`, `m_FogKeepExp: 1`). Not edited here (build settings are Latch's).

### Not built, next
- **Baked far band** (the horizon cards and ring rendered once into the sky): the sky shader and a loader already sample `Resources/Atmosphere/farband` when it exists, tinted by the horizon haze of its own azimuth; nothing is drawn until then. The bake is a builder step (Latch): render the existing horizon cards and ring (1.4 to 8 km) once, from the camera at the district, into an 8192 x 512 equirect strip over -1 to +9 degrees of elevation, RGB = the card colour as a ratio to the horizon haze, A = cover (the old `sky_sheet.py` convention), written to that path as a linear (sRGB off), clamped, mipped Texture2D. Until it lands, third person shows haze and sky above the 350 m cut; the cards (523 renderers) stay outside the clip, so Redline's counts do not change.
- **Sea and horizon rings mix toward the world export's haze** (`contracts/world/*` `haze` `#f3e8d4`, via `SkylineKit`), not `light.haze`; with a sand-cream sky the far sea edge will read cooler than the sky until Datum / Vela move the export haze to `#ecd9b4`. Not mine to edit.
- `contracts/style/suborned.style.json` `palette_world.roles.haze` is `#f3e8d4`: Aster to move it if Sky keeps `#ecd9b4`.

### Cost, for Redline's `tp-long` row (estimates; none measured: the Air is not available to me)
- **`play-opening`: 0 per pixel.** Saturation and lift are folded into the grading LUT (built once a frame, sampled by the one post pass that already runs); no new pass, no new renderer, same bloom values. CPU: three more global Volumes in the stack update (two at weight 0 are skipped): under 0.05 ms, estimate. Gate (triangles <= 2.5 M, mean <= 6 ms) unchanged.
- **`tp-street`, `tp-worst` (follow): no change to any count gate.** No renderer, shadow caster, triangle or SetPass is added (the far band is one texture sample in the sky shader, not geometry; the horizon cards stay outside the 350 m clip); linear fog costs the same as exp2. Bloom: threshold and intensity only; **URP's bloom pyramid depth follows the resolution only (`BloomPostProcessPass`), so scatter 0.6 to 0.7 costs nothing: this corrects my earlier "may add one half-resolution step".** Sky: about 25 more ALU on visible sky pixels only (early-z), under 0.05 ms at 2880 x 1864. Flare: one occlusion-sampled pass (32 depth samples) **only while the sun (azimuth 150, 38 degrees up) is in frame**; at `tp-street` (heading 180) and `tp-worst` (heading 90) it is below the frame, so expect 0; facing the sun 0.1 to 0.3 ms on the Air, estimate. Expected: frame time within the Studio's +-1 ms noise.
- **Long (flyby, free fly): about +0.5 to 1.5 ms** (Gaussian DoF: a full-resolution CoC pass and two half-resolution blurs at 2880 x 1864; estimate, unmeasured) plus the flare; not a gameplay view and not in `tp-long`. If Redline wants a row: `fly-long`, the free-fly camera at `tp-worst` raised 60 m.
- Far clip 350, the shadow fit and the LODs stay Redline's and Aster's calls; the haze does not move them.

### Open for Sky (via Aster)
(a) haze `#ecd9b4` is live (the sky and the far street share it); (b) sky: cobalt is the default and teal is Shift+F9; say which stays; (c) the play grade (+16 % saturation, warm shadow lift) is a start value to judge on the A/B (Shift+F10) in the commander view; (d) the flare in third person and free fly is on (0.6 and 1.0); say if free fly should keep it.

## Long tier retune: blue sky overhead, warm distance (Glint, 2026-10-23; Glyph's review of 0.2.94)
**Verdict from Glyph:** the flyby haze (`tp-street-long`) is an ochre dust, a sandstorm with no blue sky. Third person and play zoom read right and are not touched.

**What I measured on `briefs/048/build/0294/tp-street-long.jpg` (and `-nodof`, identical sky, so it is not the blur) against Sky's old harbour still (`briefs/048/glint/old/sky-still-2-harbour.png`):**
| | 0.2.94 long | follow (`tp-street`) | old harbour still |
|---|---|---|---|
| sky at the top of the frame | `#aec4d7` S 0.19 | `#90b4d4` S 0.32 | `#5aa8ad` S 0.48 (teal) |
| sky 10 percent down | `#becbd3` S 0.10 | | `#69afb0` S 0.40 |
| sky 15 to 20 percent down | grey (S 0.03 to 0.06), then ochre at hue 40 | | `#89b8b3`, a clear band, then cream |
| far plain / horizon haze | `#d4c8a4` to `#dccdb0`, V 0.83 to 0.86, S 0.20 to 0.25 | | `#dacbab` far city, `#efe0c3` the bright band (V 0.94) |

Three causes, all in look data:
1. **The sky is washed before it is hazed.** Long's top is 13 points less saturated than follow's with the same sky shape. The long bloom (0.8 / 0.6 / 0.85, from the old city) fills the frame with a veil from the white roofs and plaza.
2. **The haze band is too tall.** `sky.hazeHeight` 0.12 is 65 percent haze at 3 degrees and still 36 percent at 7. The sky goes blue to grey to ochre across the top fifth of the frame, and the blue to cream blend passes through grey (S 0.03), which reads as dust. The old still crosses a teal that stays saturated down to the horizon.
3. **The haze is dull, not luminous.** Same colour as the old city on paper, but the old fog was pushed past 1, so the tonemap bleached it to V 0.94 and bloom caught it. Ours is `#f4e3c3` at most 1.0 and renders V 0.86, S 0.23: mid-tone sand over the whole far plain, which is most of the lower frame.

**Changed (data; two runtime files get a small optional field each, all defaults keep the old behaviour):**
| file | change |
|---|---|
| `Resources/Atmosphere/haze.json` | `long.colour` `#f1e5cc` (paler peach cream, S 0.15) and `long.gain` 1.3 (a LINEAR multiplier, the old HDR fog); density stays 0.0005 (the old city's); follow untouched; `longNote` explains |
| `Resources/Atmosphere/lens.json` | `sky.hazeHeightLong` 0.075 (haze weight 50 percent at 3 degrees, 20 at 7, 6 at 12) for the cobalt sky in the long tier only; `bloomLong` 0.8 / 0.6 / 0.85 to threshold 1.0, intensity 0.35, scatter 0.8, tint `#fff4e2`; follow, play grade, teal, DoF, flare untouched |
| `Runtime/DistanceHaze.cs` | `Profile.colour`, `Profile.gain` (optional; gain 0 or 1..3; colour #rrggbb), `LinearHaze(tier)`: the one linear haze value, fog colour = its gamma form; long only, by day |
| `Runtime/AtmosphereLens.cs` | `Skyp.hazeHeightLong` (optional, 0 = none), `Sky(on, tier)` takes the tier and uses `DistanceHaze.LinearHaze(tier)` for the sky horizon, so sky and fog stay one haze with no step |

Nothing else moved: play tier (grade only), follow (fog `#f4e3c3` linear 90 to 350, hazeHeight 0.12, bloom 0.86 / 0.35 / 0.7), teal (own band 0.18), flare, DoF. **Night:** `on` is false at night, so the lens, the sky shape and the bloom do not run; the fog colour guard `!Night` is unchanged, so the long gain and colour never apply at night (the scene's `night.haze` `#102b36` stays and matches the night sky's horizon); the long exp density 0.0005 at night is as 0.2.94. Not changed at night, so nothing to retune there; Latch to look once (below).

**Compile:** `dotnet build --no-incremental` of `DistanceHaze.cs`, `AtmosphereLens.cs`, `LightPhase.cs` against the Unity 6000.6.2f1 assemblies (`out/glint048/check/Check.csproj`): 0 errors, 0 warnings. Both json files parse. The shader is unchanged (it already reads `_AtmHaze` linear, `_AtmSkyShape.x`).

**Mock:** `briefs/048/glint/long-before-after.png` (as built, mock, Sky's harbour still), made by `briefs/048/glint/long_mock.py` from the 0.2.94 long shot. It is a mock: sky pixels are moved along their own blue-to-haze blend with the new band height (w_new = w_old ^ (0.12 / 0.075)), the new body colour (`#8db4dc`, follow's) and a lighter haze (`#f1e6d0`, V 0.94); hazed far pixels are shifted by the change in haze colour in proportion to how hazy they are. There is no depth, no engine bloom and no real ACES, so read it for the amounts and direction, not as a measurement. It shows a blue sky overhead that turns cream only in the last quarter above the horizon, pale peach distance, ghosts kept. The predicted real render of the haze (`#f1e5cc` x 1.3 linear) is near `#eee3cb` to `#f0e4cc` from the 0.2.94 calibration (authored `#f4e3c3` rendered `#dccdb0`, about 0.795 in linear across channels); that is a prediction.

**What it still will not do (not a lighting fix):** the old still's blue is the SEA and a teal sky; ours at the far end is a flat pale plain (`sea` drawn 4 beyond 150 m in `census-tp-street.json`). If the plain stays the biggest pale shape in the long frame, sea colour or a far-sea card (Vela, Datum) is the next lever, not haze.

### For Latch: verify in the game (next batch; nothing here edits a scene)
1. **Compile and console:** no `DistanceHaze:` or `AtmosphereLens:` rejection line (a bad `long.gain` or `long.colour` or `hazeHeightLong` logs one and the whole file is ignored, so the fog stays as built).
2. **Re-shoot `tp-street-long` (flyby) and `tp-street-long-nodof`** with the same rows as 0.2.94 and sample the same points. Expected: sky at the top about S 0.30 or more (`#90b4d4` like follow, was `#aec4d7` S 0.19); blue holds to about 20 percent of the frame above the horizon, then warms to cream; far plain rendered about `#eee3cb`, V >= 0.92, S <= 0.17 (was `#d4c8a4` to `#dccdb0`); no grey band (S < 0.05) in the sky.
3. **If the long haze did not change at all:** `RenderSettings.fogColor` is clamped to 1 on this path (the gain does nothing, no error). Then set `long.gain` to 1 and instead lighten `long.colour` to `#fbf1dc`; tell me.
4. **If the horizon blooms too hard or the sky is veiled again:** `bloomLong.threshold` 1.0 to 1.1, or intensity 0.35 to 0.25. If the far haze looks flat and grey, intensity 0.35 to 0.5.
5. **Teal (Shift+F9) in a flyby:** must be as 0.2.94 (the long band applies to cobalt only).
6. **Follow tier (`tp-street`, `tp-worst`) and play (`play-opening`):** pixel-compare with the 0.2.94 shots; they must not move (long-only fields).
7. **Night, flyby and free fly:** look once; expect the dark teal fog and night sky as in 0.2.94 (no gain, no bloom, no long band). Report if the night far plain looks muddy; that would be the exp 0.0005 density (it applies at night), and the fix is `long.density` 0.0004 or a night profile.
8. **Frame time:** long only; bloom is cheaper not dearer (same pass count), no new pass.

## Shade, long view and haze: Glyph's review of 0.2.99 (Glint, 2026-10-24)
**Pictures (mocks on the 0.2.99 shots, not renders; read for amount and direction):** `briefs/048/glint/shade/shade-before-after.png` (play zoom, `ring-loop-names`), `briefs/048/glint/shade/long-before-after.png` (`tp-street-long`), full-size `ring-loop-names-after.png`, `tp-street-long-after.png`; scripts `shade_mock.py`, `long_mock.py`, `aces.py` beside them (an ACES stand-in calibrated on the 0.2.99 horizon pixel to within 2 levels).

**1. Shade (the office wing's roof slab, the navy road).** Cause measured on `ring-loop-names`: a shaded surface sees only the ambient, and the ambient is a neutral sky (#dedce0 x 0.69), so a near-white roof (`roof_a` #faf6ea and kin) shades to a pure neutral grey (#8c8b8c, S 0.01) and a powder-blue road to #455668 (V 0.41, S 0.33: the warm sun desaturates the lit road, the shade has no sun to do it). The lit side is right; the shade has no warm light.
- **Change (live, no scene rebuild): `lens.json` `sun.shadowStrength` 0.62.** AtmosphereLens sets `Light.shadowStrength` on the sun by day (the scene's own value restored at night, with the atmosphere off, and when the component goes). A shadow now takes 62 percent of the sun out, so the sun fills the shade in its own warm colour. The sun term on lit surfaces is unchanged, so **the sunlit side and the signals do not move**. Shade irradiance on up-facing surfaces x1.84 R, x1.64 G, x1.52 B. I chose this over raising `ambient` in light.json because it needs no scene rebuild and cannot lift the sun side.
- Also: `gradePlay.liftOffset` 0.03 to 0.04 and `liftWarmth` 0.15 to 0.25 (small).
- **Predicted (mock, same frame):** road shade #455668 (V 0.41, S 0.33) to #6f7a88 (V 0.53, S 0.18, hue 214: powder blue-grey); roof slab shade #8c8b8c to #b4aca9 (S 0.06, hue 20: warm); paving shade #7e726b (V 0.50) to #a8968b (V 0.66, S 0.17); lit road, paving and cream unchanged (k_lit 1.00). The shade/lit ratio of the road goes from 0.31 to about 0.5 in light; the testbed's is 0.74 but it has no real sun, so I stopped at the crisp-shadow end.
- **Not lighting, said plainly:** the slab's **lit** side is #e2dedc, also neutral: the bare roof albedo is a flat near-white. The shade now reads warm and lighter, but the slab will still look like a plain slab until the roof has a cream albedo and a trim or cap line (Vela; `facade.json` roof_a to roof_e are #faf6ea, #e6f5ee, #fbe9dc, #e8f2f8, #f2ead6: only roof_a, roof_c and roof_e are warm).

**2. Long view.**
- **Horizon band, sand yellow.** Cause: two colours. The far plain is the sea material's own baked haze (`_Haze` #f4e3c3, no gain, built into the scene: it renders #d9cbaf, S 0.19, hue 40) while the sky's horizon is #f1e5cc x 1.3 (renders #dddbce, S 0.07), and my 0.2.97 retune only moved the second. Change: `Shaders/Water.shader` now takes the global `_AtmHaze` (the sky's own haze, unclamped) for the sea's fog colour and its far-shore fade when the atmosphere is on (perspective cameras by day; zero in play, so the play tier and the scene as built are unchanged). The far plain and the sky now end on one colour. `haze.json` long: colour `#fad9bd` (peach-cream, hue 27, was `#f1e5cc`), gain 1.2 (was 1.3, less bleach), density 0.0006 (was 0.0005). Predicted horizon and plain #ddd5c9, S 0.09, hue 34, V 0.87 (was S 0.07 and S 0.19, two different colours).
- **Grey sky band** (S 0.00 at 20 percent down in 0.2.99, the blue to cream blend passing through grey). `lens.json` `sky.peach` 1.0 and `SubornedSky.shader`: a peach multiplier (1.12, 0.96, 0.80) on the middle of the blend, zero at both ends. Predicted S 0.06, hue 30 there.
- **Lawn.** Flat olive (#768447, H 73, S 0.46) because the greens hue protection began at 70 degrees. Widened: from 55, full from 72, to 135, shift 10 (was 70, 95, 140, 8). Predicted lawn H 82. Yellows (roof boxes H 50 to 52), sand (H 40 to 45) and cream stay out of it; palm greens move +2 more (106 to about 108). **It stays flat**: the lawn is one albedo with no mowing bands or paths; that is Vela's or Datum's (lighting cannot make a flat texture). I have not added a fake light pattern.
- **Plaza blown out.** I could not fix this by exposure and say so. The ACES shoulder is flat above about 200: 0.2 EV off moves the plaza paving only from 220 to 214, and 0.45 EV to 212. The cause is a near-white, neutral plaza albedo at about 1.2 scene-linear (S 0.01 in the long view, buff at play zoom): paver contrast is lost in the shoulder. Done: `gradeLong.exposureDelta` -0.2 (the long tier only: the scene grade's postExposure plus the delta, new optional field, ColorAdjustments on the long Volume, only exposure overridden) and `bloomLong.threshold` 1.0 to 1.2 (the plaza at 1.18 no longer feeds the bloom veil). **The real lever is Vela's plaza paving albedo about 15 to 20 percent lower and warm buff** (the paving at play zoom already reads buff).
- **Roof boxes** (orange and yellow): not lighting; Vela's.

**3. Road reflection cut (`_ClearCoatMask`).** Waits for Vela's material regen and Latch's re-shoot. When it lands I will judge the Boulevard on three numbers: lit enamel V against the testbed (0.68 to 0.71), the shade/lit ratio of the same enamel (now about 0.5), and whether any pale sheen remains at grazing angles in `road-enamel-boulevard-third` (that is the reflection probe, not exposure; its cut is in `light.json` `reflection.street`). If the road is still pale after the shade change, the remaining lever is the road albedo, not exposure, because lowering exposure would also crush the shade I just lifted.

### Files changed
`unity/Assets/Suborned/Resources/Atmosphere/lens.json` (sun.shadowStrength, gradeLong, sky.peach, bloomLong.threshold, lift, greens), `.../haze.json` (long colour, gain, density, notes), `unity/Assets/Suborned/Runtime/AtmosphereLens.cs` (Sunp, Longp, SunShade, long exposure, sky.peach to `_AtmSkyExtra.z`; `Check` ranges), `unity/Assets/Suborned/Shaders/SubornedSky.shader` (peach band), `.../Water.shader` (`_AtmHaze` hook). No scene, no light.json edit (the ambient is untouched on purpose: no rebuild needed).

### Checked here
1. `dotnet build --no-incremental` of `DistanceHaze.cs`, `AtmosphereLens.cs`, `LightPhase.cs` against the Unity 6000.6.2f1 assemblies (`out/glint048/check`): 0 errors, 0 warnings; an injected undefined identifier gave 2 errors (so the check discriminates), then removed.
2. The Unity shader compiler, batchmode, in the scratch project `out/glint048/shaderproj` (not the game project): `Suborned/SkyGradient` and `Suborned/Water`, supported, 0 messages.
3. `lens.json` and `haze.json` parse (python json); values are inside the C# `Check` ranges (shadowStrength 0.62 in 0.2..1, peach 1.0 in 0..2, exposureDelta -0.2 within 1.5, greens 55 < 72 <= 135 < 170).
4. Not checked (needs the game): any real render, the Deferred path's use of `Light.shadowStrength`, `unity_FogColor` clamping, frame time.

### For Latch: verify in the game (no scene edit; nothing needs a rebuild)
1. **Compile and console:** no `AtmosphereLens:` or `DistanceHaze:` rejection line; both shaders import with no message.
2. **Shade, play zoom, yaws 30 and 210** (`ring-loop-names`, `done-turn-chip`, `office-front-yaw210`): sample (a) the shaded road: expect about #6f7a88 (V 0.50 to 0.58, S 0.15 to 0.22), no longer #455668; (b) the roof slab shade: warm, V about 0.7, S 0.04 to 0.08; (c) **sun side must not move**: lit road about #aabfbf, lit paving, lit cream within 3 levels of 0.2.99; (d) cones, guard, rivals: pixel-identical (they draw on the overlay camera, no grade; the sun strength is not read by an unlit shader). Then Shift+F10 off: the scene as built (strength 1.0) must come back.
3. **If Deferred ignores `shadowStrength`** (no change in shade at all): tell me; the fallback is light.json `ambient` x1.35 with sun 1.8 to 1.55 and a scene rebuild (mock numbers in `shade_mock.py` args `#dedce0 0.9 1.55`), or `liftOffset` 0.05.
4. **If shade is too flat** (crisp-shadow look lost): `sun.shadowStrength` 0.62 to 0.7 (shade lift then about 70 percent of this one).
5. **Long view** (`tp-street-long`, `-nodof`): expect the far plain and the horizon within 6 levels of each other, about #dcd3c8 to #e0d5c9 (S 0.08 to 0.13, hue 30 to 38; was #d9cbaf S 0.19 vs #dddbce S 0.07), no grey band in the sky (S >= 0.05 at 20 percent down), lawn hue 78 to 86. **If the plain is still sand**: fog colour is clamped at 1 and the sea's `_FadeFrom` 4000 m is beyond the visible plain; then tell me and I will set `haze.long.gain` 1.0 and lighten the colour. **If the towers go yellow** against a pinker plain: same cause (clamped `unity_FogColor` on the city shaders); same fix.
6. **Night, flyby, free fly:** `on` is false at night (strength, exposure, peach and the Water hook are all off); look once. Follow tier (`tp-street`, `tp-worst`): expect only the shade change and the Water hook (follow's haze is `#f4e3c3` gain 1, as the baked `_Haze`, so the sea there does not move).
7. **Frame time:** no new pass; a shadow-strength float and one global read in Water; nothing to measure beyond the Studio's noise.

### Open (not mine)
Vela: bare roof albedo (cream, with a cap or trim line), plaza paving albedo about 15 to 20 percent darker and warm, lawn variation (mowing bands or paths), roof boxes. Next owner: Latch (verify above, re-shoot `ring-loop-names`, `tp-street-long`, `road-enamel-boulevard-third`), then Glint again for the Boulevard; Glyph re-checks the same three frames.

## Road strip light by phase, and the calm rails (Glint, 2026-10-24)
**Ask (Sky, via Vela):** the road edge is modern strip lighting, not white edges. Vela shipped the light ribbon at the kerb face (DK_Road_Lens at the day value 1.4, pearl band dropped, `light.json` `emissive` `roadLens` 5.0 night / 1.4 day). Nothing raised it. Now the rig does.

**What was there:** no emissive in the game was driven by any phase (`light.json` `night.emissive` was data only; `LightPhase` held day and night States and the night lamps, nothing else). There was no evening. So this is the first phase-driven emissive, and evening is new.

**Changes (no scene edit, nothing rebuilt):**
| File | Change |
|---|---|
| `unity/Assets/Suborned/Runtime/RoadLight.cs` (new) | Self-installing (`RuntimeInitializeOnLoadMethod`, like AtmosphereLens). Each frame reads `LightPhase.Current`; eases (1.5 s, unscaled time) the lens emission and the road pool alpha to the level. **Day is untouched:** nothing is scanned or swapped until the look first leaves day, so day frames are byte-identical. On first leaving day it copies `DK_Road_Lens` and `DK_Road_Pool` once and gives the copies to the renderers that draw them (one `FindObjectsByType<Renderer>` scan, again after a scene load). **The shared .mat assets are never written** (a play-mode edit of a shared material sticks to the asset in the Editor and would break the kit's hash check). Validates its json (`suborned.roadlight/1`); a bad file logs one `RoadLight:` error and the strip stays at day. |
| `unity/Assets/Suborned/Resources/Atmosphere/roadlight.json` (new) | lens day 1.4 / evening 3.0 / night 5.0 (the .mat's own scale: the kit writes rgb as `(linear x level)^(1/2.2)`, level in alpha; RoadLight scales the day rgb by `(L / 1.4)^(1/2.2)`, so evening is x1.41 and night x1.97 in the shader's raw colour, which is what the Blender board's 3.0 and 5.0 map to); pool alpha 0 / 0.14 / 0.22 (0.22 is the kit's own value); ease 1.5 s. |
| `unity/Assets/Suborned/Runtime/Play/LightPhase.cs` | `enum Phase { Day, Evening, Night }`, `Current`, `SetEvening()`. `Set(bool)` as before (sets Day or Night). **Evening has no sun or grade of its own** (not built, not asked for): `SetEvening()` applies the day State and sets the phase, so only the dusk lights change. Street lamps stay a night thing. |
| `unity/Assets/Suborned/Editor/World/light.json` | `emissive.evening` (roadLens 3.0, roadPoolAlpha 0.14, note). Data for Vela and Latch; the runtime reads `roadlight.json`, so change both together. |

**The pool (soft light on the road, "only if cheap"):** it was already in the kit and the builder: `DK_Road_Pool` (Materials/Glow, `Suborned/Cross Glow` mode 3, additive, no shadow, alpha 0 by day), a 1 m band beside each ribbon, batched by `Combine`. It costs no light and no extra pass, only a little additive overdraw on a strip of road. So the answer is emission + the pool alpha + the night bloom (0.9 / 0.35) and nothing else; no real lights, no decal, no lightmap. At evening the day bloom (threshold 1.1, 0.22) catches the lens (raw R about 1.64), at night the night bloom does.

**Checked here:**
1. `dotnet build --no-incremental` of `RoadLight.cs`, `LightPhase.cs`, `AtmosphereLens.cs`, `DistanceHaze.cs` against the Unity 6000.6.2f1 assemblies (`out/glint048/check`): 0 errors, 0 warnings; an injected undefined member gave 2 errors (the check discriminates), then restored.
2. `roadlight.json` and `light.json` parse; `roadlight.json` values pass `RoadLight.Check`'s ranges by reading.
3. Not checked (needs the game): any render, the renderer scan count and time, the pool's look, frame time.

**Calm rails (`look.json` `track` #6f7a72, smoothness 0.3, metallic 0.1), under the grade:** a rough stand-in (the `aces.py` curve, sun 1.8 at 38 degrees, trilight ambient, exposure +0.15; it is a model, not a render) puts a flat rail at about (166, 165, 148) in sun, (148, 148, 131) in shadow at the sun's 0.62 shadow strength (96, 103, 90 with no sun fill), metallic 0.1 about 6 levels lower. Two things: (a) the warm sun turns the green-grey into a khaki grey, a quiet line, good; (b) **its luma matches the lawn track's** (lawn about (174, 175, 83), luma 164, rail 164) so on the lawn it separates by chroma only, and may read as a faint ghost line rather than a rail. Metallic 0.1 is fine (no sky streak; the reflection cube is dark at low angles). **Recommendation, not applied** (look.json is a rebuild and Vela's colour): if a play-zoom shot shows the rail within about 20 levels of the lawn in luma, set `look.json` `track.value` 1 to 0.82 (rail about 135 in sun, 120 in shade, still calm, a dark thin line on green). Latch to sample; Vela to decide.

### For Latch: verify in the game (no scene edit; the two new files need Unity to make their .meta on import)
1. **Compile and console:** no `RoadLight:` error. At the first non-day phase one line `RoadLight: N renderers on DK_Road_Lens / DK_Road_Pool (runtime copies)`; N more than 0 on the Handover block (the ribbons are laid on enamel; the asphalt option drops them, so 0 and the "not laid" line there is right).
2. **Day (default):** pixel-identical to 0.2.99+ shots (nothing scanned). Strip is a pale cream line (lens 1.4), no pool.
3. **Evening:** nothing calls it yet. In a debug console or a one-line `-evening` arg beside `-night` in `HostNight.cs` (Latch's file, I did not touch it): `LightPhase.Instance.SetEvening()`. Expect the strip clearly lit, a soft pool 1 m wide on the carriageway beside it, the day sun and grade unchanged, lamps off. Compare `briefs/048/vela/road-strip-light.png` bottom right "evening".
4. **Night (`-night`):** strip at 5.0 with the night bloom, pool 0.22; the road should read as a line of light with a soft wash, not a white band; if bloom swells the line past about 0.4 m at zoom 14, lower `night.lens` to 4.0 in `roadlight.json` (no rebuild).
5. **Ease:** switch day to night in play (the briefing choice): the strip fades over about 1.5 s. A night launch opens lit.
6. **No asset writes:** `git status unity/Assets/Suborned/Kit/Materials/DK_Road_Lens.mat` and `Materials/Glow/DK_Road_Pool.mat` unchanged after a play session.
7. **Frame time:** one scan at the first phase change (look for a hitch there; if over a frame at the Air, tell me and I will move it to the scene load); then two material writes only when the value moves. No light, no pass.
8. **Signals:** cones, guard, rivals pixel-identical at day, evening and night (they draw on their own camera).
9. **Calm rails:** sample the rail and the lawn at play zoom, yaws 30 and 210; see the rail note above.

## Evening phase: low sun, dusk sky and haze, grade, lit windows (Glint, 2026-10-24)
**Why evening looked like day (0.2.112):** `LightPhase.SetEvening()` kept the day State and only RoadLight's strip rose (lens 3.0, pool 0.14). It now leans the look from day toward `lens.json evening` by `LightPhase.Dusk` (0 day .. 1 evening, eased over `evening.easeSeconds` 1.5 s on unscaled time; night snaps it to 0). Nothing is in the scene: it runs on the runtime state, so a scene that never calls SetEvening renders as before. Boards: `briefs/048/glint/evening/` (real game frames: `board-1` the opening at yaw 30, the Records Office front at yaw 210 and the café front, with the game's own shadows; `board-2` the Office front at yaw 30 and 210 through Latch's road test; `board-3` third person down the Boulevard and toward the sky rail; `board-4` the sky-rail crossing; `board-5` the follow view; full-size evening frames beside). Each board is day | evening | night.

| Part | Where | Evening value (day) |
|---|---|---|
| Sun | `lens.json evening.sun`, LightPhase | colour #ffdac0 (#ffe0d0), intensity 3.0 (1.8), pitch 17 (38), yaw 120 (330), shadow strength 0.75 (0.62). Ground irradiance I x sin pitch 0.88 against 1.11, sunlit walls 2.9 against 1.4: long shadows (3.3 x the height) with the street still readable. **Yaw 120 on purpose:** the light runs along the axis that is sideways to both play headings (yaw 30 and 210), so the long shadows fall across the picture, left at 210 and right at 30, and the Records Office front (north) takes the low light; a sun behind or in front of the camera hides its own shadows. |
| Ambient, reflections | `evening.ambient`, `evening.reflection` | lilac skylight #d6d0ee x 0.78, peach equator x 0.76, warm ground x 0.50 (day 0.69 / 0.71 / 0.41 on their own hexes); reflection intensity 0.70 (0.80), probes x 0.85: the baked cubes are the day's, so they are turned down, not re-baked |
| Sky (follow, long) | `evening.sky`, SubornedSky.shader | zenith #4a78d2, mid #e6a79a, glow #ff9a58, the glare follows the real sun (new `_AtmSunDir` / `_AtmSunCol`, w 0 = the scene's own) |
| Haze | `haze.json evening` | #f3c6a2 (#f4e3c3), long #f8c7a8 gain 1.15 density 0.0007 (#fad9bd 1.2 0.0006), follow starts at 70 m (90). Bright, hue 27 to 30. |
| Grade | `evening.grade`, per tier on the tier's own volume | exposure +0.15, contrast +4, saturation +2, filter #fffaf4, white balance +3 / +1, cool tint in the shadows (#e4e4f6), warm in the highlights (#fff8f0), vignette 0.16 (0.10). No pass, no per-pixel cost (it is the LUT). |
| Windows, signs | `roadlight.json glow`, DK_GlassDark.shader, RoadLight | DK_GlassDark (972 renderers): 35 % of panes lit at evening, 75 % at night (new `_Lit`, `_LitColour`, 0 on every .mat as shipped; a pane = a 6 m x 3.6 m cell of the facade, so the share is exact and which window is not). The neon, lightbox and screen materials x1.25 evening, x1.7 night of their own emission (**eight of them are drawn in the Handover scene**; I had counted none from the scene file, the player log lists them). RoadLight's own strip levels (lens 3.0, pool 0.14) are unchanged. |

**Measured (game frames, 1920 x 1080, Studio, play-opening-30, 9 x 9 mean):** lit paving #edcfa2 (S 0.32, V 0.93) to #ce9e6c (S 0.48, V 0.81); fountain teal ring #87b9a4 (V 0.73) to #577d70 (V 0.49, in shade); seafoam and teal enamel stay teal and seafoam (the first try, a #ffb48c sun with a warm filter, turned them olive and was dropped: a deep orange light kills teal, so the sun is a pale peach and the warmth comes from direction, haze and sky). Day frames: **unchanged**. Three runs of the same four shots at day, 0.2.112's own player against this build: mean difference 0.01 to 0.10 (follow 0.98) against 0.01 to 0.10 (0.99) between two runs of 0.2.112 (animation noise), so day is the same within the noise. Frame time (`perf-opening`, 6 s): 0.2.112 4.10 / p99 5.49 ms, this build day 4.08 / 5.48, evening 4.09 / 5.61, night 4.00 / 5.50 (Studio). No new real-time light, no new pass: sun, ambient, haze and grade values only.

**Night changes too, on purpose:** lit windows at night (75 %), because an evening that lit them and a night that put them out again would be wrong. Revert is `night` 0 in `roadlight.json glow` for DK_GlassDark.

**Sky-rail shadows (Glyph's review):** not a defect I can lift per caster. At the Boulevard x sky-rail crossing (board-4, day) the rail's shadow takes the road from V 0.74 lit to 0.56 (S 0.10 to 0.17, blue-grey) and the lawn from 0.85 to 0.71 (hue unchanged): it is the same lifted shade as any shadow since `sun.shadowStrength` 0.62, not darker. URP has one shadow strength per light and no per-caster strength, so a lighter rail shadow only comes from (a) the global `sun.shadowStrength` (0.62 to 0.55 lightens every shadow, a look decision for Sky) or (b) the rails no longer casting (`ShadowCastingMode.Off` on `skyrail/*` and `transit/pylon*`: Latch or Vela, a one-line change in the builder, cheap but it loses the overhead cue). The dark olive-brown stripes in `r29-boulevard-play.png` that sit beside the grey stripe may be the tram track material (`look.json track`), not shadows: compare with shadows off before changing the light. In the evening the rail's shadow runs along the rail (sun axis along the beam), so it no longer crosses the road as a stripe.

**Slab seams (Glyph):** not lighting. The same thin dashed lines on the asphalt are in the frame with the sun's shadows switched off (`seam-not-lighting-shadows-on-vs-off.png`, left day, right `-noshadows`), at the same pixels; they are coplanar road slabs meeting (z-fight or an open edge), a geometry or material job for Vela or Splice (overlap the slabs by 1 to 2 cm or share the edge vertices).

**Files (mine):** `Runtime/Play/LightPhase.cs` (Dusk, ApplyDusk), `Runtime/AtmosphereLens.cs` (evening data, grade by tier, sky, shadow strength; Latch's Redline 064 #6 colour cache kept, the evening code now uses it), `Runtime/DistanceHaze.cs` (evening haze), `Runtime/RoadLight.cs` (the glow list), `Shaders/SubornedSky.shader` (`_AtmSunDir`, `_AtmSunCol`), `Shaders/DecoKit/DK_GlassSky.shader` (`_Lit`; **Vela's shader, a small additive term, 0 by default; flag to Vela**), `Resources/Atmosphere/lens.json` / `haze.json` / `roadlight.json`, `Editor/World/light.json` (notes only, no scene change).

**Checked:** `dotnet build` of the four scripts against the Unity assemblies: 0 errors, 0 warnings. A real player build in a scratch copy of the project (`out/scratch/glint/evening/repo`, an APFS clone of unity/ with symlinks to contracts, briefs, runs, sim and tools; its own version.json, so build 0.2.112 and `out/build/play` were not touched): build Succeeded 0 errors, the road test and the shots ran, player logs have no exception or rejection line, and the RoadLight line lists the nine sign and window materials it found. Board scripts and shot rows: `briefs/048/glint/evening/repro/`.

**For Latch (next batch):** (1) no rebuild of the world needed; new files need none (no new .meta); the edited shaders import on the next open. (2) Shoot evening with `-evening` on the existing shots (`office-front-yaw210`, the opening); the road test's camera is a copy of the play camera without `TacticalShadows`, so its day, evening and night frames have **no shadows** (the boards here use a scratch copy that adds the component to that camera: `TpLongRun.RoadTest`, four lines, worth keeping). (3) Read: no `LightPhase:`, `AtmosphereLens:` or `DistanceHaze:` rejection line at start; evening eases in over 1.5 s with no pop; day to evening to day returns to the day frame; Shift+F10 (atmosphere off) in evening keeps the sun and ambient (LightPhase) but drops the haze, sky and grade. (4) Evening to night snaps (night is its own full State). Next owner: Latch (verify above), then Glyph on the boards.
