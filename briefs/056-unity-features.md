# Brief 056: Unity features we aren't using (audit)

Sky, 2026-10-24: "is there anything we could be benefitting from using unity that we're not in this game?" Six read-only studies by the crew (audio left for later). Nothing was edited, built or run; every number below is quoted from existing reports, not measured now.

- **Owner:** Redline (Air measurement and the render cuts), Splice (texture and load work). **Next owner:** Aster (Sky picks what runs).
- **Done-check:** the Air run below is on file; SSAO and shadow cuts are decided from it; build size and cold-load time are recorded before and after the import changes.
- **Evidence:** `out/scratch/claude/air/` (the resolved rows); the Air's results go to `~/suborned-perf/out/tplong.json` on the Air, then a copy to `briefs/056/air/`.

## Do now (they serve the 60 fps limit, not new features)

1. **Measure the Air, once, quiet and plugged in.** The only Air run is 14 Oct (1080p, no people, on battery, busy machine): GPU 8 to 13 ms against main thread 2 to 4 ms, two heavy stops at 25 to 28 ms. Build 0.2.97 is already copied to `~/suborned-perf` on the Air with rows `warm`, `play-opening`, `tp-street`, `tp-worst`, each at 2880×1864 and 1920×1080. **Blocked: Sky plugs the Air in and keeps clear of it (about 2 minutes).** Decision rule: GPU ms above main-thread ms means Burst and occlusion are off the table.
2. **SSAO and shadows (Glint look, Latch wiring, Redline numbers).** Brief 008 B6: SSAO off took GPU 11.6 → 5.6 and 10.9 → 4.5 ms; shadows off cut frame time 16 to 43 %. Glint's report says SSAO has no visible effect at play zoom (contact shade is baked geometry). Run SSAO off (or half resolution, or depth only) for the ortho camera, keep it for third person; try 2048 or medium soft shadows. Estimated 2 to 6 ms on the Air.
3. **Build size and load (Splice).** App 935 MB; textures 40 %, meshes 27 %, the district level 20 % (510 MB text scene, 123,671 objects). Cap the five 4096 px kit atlases at 2048 and move normals and masks to a cheaper format (est. 40 to 60 MB per atlas set); turn on streaming mipmaps; check mesh read/write and compression on the 681 FBX; strip about 40 MB of cutaway shader variants. First, a stopwatch around `LoadSceneAsync` (`FrontEnd.cs:472`) for a cold and warm baseline: none is recorded anywhere. If load stays slow after the texture cut, the scene object count is the problem, not asset size.

## Small proofs, after the above (new features: see `parked.md`)

- **Adaptive Probe Volumes with sky occlusion, civic block only** (Glint): visible shade under the 9.43 m deck and 24 m concourse, warm bounce on chrome and figures. Pass: shade difference visible at an under-deck stop, under 0.5 ms. Needs the Air headroom from item 2 first.
- **Decals and Particle System** (Vela, with Latch for the renderer feature): ground decals for target rings (briefs 046/049), body and Trail marks, small steam vents. Pass: under 0.3 ms at play zoom, Cutaway fade and SSAO unaffected. Presentation only, seeded from plan ids, never read by the sim.
- **HDR colour grading with a 64 LUT** (Glint): pastel cream gradients are the likeliest place to band; it reshades every colour, so Glint re-tunes.

## Decided: not now or not at all

| | verdict | reason |
|---|---|---|
| Burst / Jobs / Collections | wait for a measured CPU miss | main thread about 5 ms; presentation code that Burst could reach is 7 to 8 % of it (0.2 to 0.4 ms); the sim is integer managed code Burst can't compile; no Burst result may feed the sim |
| Occlusion culling (Umbra or drawer) | no | play view draws 325 renderers; the cost is per pixel; the drawer's occlusion made worst-case third person 36 % slower on the Studio; Umbra needs a re-bake on every generated build |
| VFX Graph | no | 20 to 200 particles; adds a package and Metal compute for nothing |
| Addressables | later | one district, local build; revisit for districts II and III as one scene per district with shared kit and people groups; never for `StreamingAssets/sim` |
| Cinemachine | later | replaces only about 150 to 250 lines, in the follow view; revisit if third person returns (brief 024 already turned down its deoccluder: the city has no colliders) |
| Timeline | never for play | fail camera and takedown beats are assembled per act; authoring them breaks "nothing hand-authored" |
| NavMesh | never at runtime | the sim paths on its own integer grid; mission windows depend on path lengths to about 0.1 s; about 40 recorded routes and the pinned `handover.site.json` would need re-baking. At most an editor-only cross-check of route lengths that writes nothing back |
| Baked lightmaps, realtime GI, SSR | no | rejected in `briefs/026/glint/cafe-lighting.md`; over the Air's budget |

## Better use of the NavMesh time

The Handover site is street-only in the sim (`LevelOf` returns 0). Deck and concourse exist only in the plan and the world export. A timed connector graph (stair, lift, escalator edges priced in seconds from the plan) gives the sim levels, which an off-mesh link can't express. Owner would be Keel with Pulse; not started.

## Not covered

Audio (Sky: later). Chime has no code in the repo yet: no `AudioSource` or mixer.
