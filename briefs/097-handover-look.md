# 097 · The Handover zone at its best: light, surfaces, motion and juice

Owner: main session (as Aster), opened 2026-11-06 on branch `work/097-handover-look`.

## Sky (2026-11-06)
- "go through every non character asset in the district office and try to improve, optimize and make look as good as you can within reasonable triangle counts and draw calls. Looking for lighting, detailing, shadows, that hold up in LoD0. Aim for AAA standard lighting whilst always mindful of performance cost."
- "Can use the runware API of $30 spend to improve or swap out models if need be and the Tripo retopology."
- "Think about additional visuals we can add in the area to give the game an amazing feel. Think about VFX as well and shaders ... breathtaking game with juiciness. Be ambitious, take no shortcuts. Work on a new branch."
- "You can drop any assets outside of the district office for now that aren't used for faster iteration and we add back in later."
- "remember to maximise Unity to its fullest, using / installing any plugins that can help."

Scope = the Handover zone (OldTownSwap.ZonePlan: the civic block, Records Office, café, square and fountain, kerbs, roads, crossings, street furniture, planting, cameras) as played in OldTown. Characters are out of scope. Paid budget: $30 Runware (Tripo 3.1 for models), each pass through `tools/paid/paid.py`; Codex stills first ($0).

## Cost rules that hold
- The Air is the 60 fps reference and is GPU-bound at play zoom (Redline's audit: ~15–21 ms on 0.2.122, never measured on OldTown). Every addition is near-free per pixel, replaces a cost, or has an off switch (global) for the Air A/B. Draw calls stay ~110 SetPass; triangles at play stay far under the 2.5 M gate.
- Systems, not scripts; nothing at runtime is AI. Signals stay unlit and outside the grade (Glyph's).

## Fast loop (Sky's "drop assets outside")
`out/scratch/main097/build.sh [margin]`: GameBuild.CliScratch with `-lean` (Editor/World/Play/LeanScene.cs) saves Assets/Scenes/Lean/OldTown.unity (git-ignored, same scene name) without the pure geometry more than `margin` m (110) outside the zone and builds from it: 6,044 objects dropped, build 36 s, shots 48 s (`run.sh <id>`). The real OldTown.unity is untouched; a build without -lean ships the whole town. Perf numbers that count are taken on the full scene.

## Baseline (lean 0.2.254-scratch, 2026-11-06, `out/scratch/main097/shots/base`)
Office y210: GPU 1.56 ms, mean 4.18 ms on the Studio. Findings at play zoom and close: windows flat dark panels; the contact shade's lilac tint (0.55, 0.53, 0.6) reads magenta on cream under the grade and darkens sunlit ground as if shaded; the road a flat brown-grey with blotches (§15: the old town's dark grey asphalt); the limestone detail reads as camouflage blotches on the tower; nothing moves (no wind, no water, static jets); vertex AO (audit item 1, approved 2026-11-05) never built.

## Plan
1. Shaders: wind (`_DK_WIND`, all cutaway passes), vertex AO (`_DK_VERTEX_AO`, Vela's Blender bake), fountain water and jets, window interiors, sun-aware contact shade with a warm tint, cloud shadows (URP main-light cookie, scrolled).
2. Light: a zone-only GPU lightmap bake (Baked Indirect: realtime sun, baked bounce and occlusion) + light probes for people and props; reflection probes for chrome and glass.
3. VFX (Shuriken, built from code): fountain spray, mist and splash rings; ambient life (birds, drifting petals, dust in the sun, steam); play feedback.
4. Assets: weak props re-made or detailed (Blender first, Tripo only where it wins).
5. Measure: Studio A/B per item, the full scene before/after, tris, SetPass, texture MB; a self-contained report.

## Unity features considered
Used: light cookies (in the URP asset already), GPU lightmapper + light probes, reflection probes, Shuriken. Not used: Surface Cache GI (experimental, realtime ray-traced: too heavy for the Air); On-Tile post (no bloom); VFX Graph (its graphs can't be authored headless from code; Shuriken covers the particle counts here); SSAO (Sky rejected it for cost).

## Round 1 landed (main session, 2026-11-06; commit 117d59d6 + this one)
What is in, what it costs and what is parked: briefs/097/notes.md; pictures (today / 097 / the concept bar, self-contained, on disk): briefs/097/index.html.
Parked: the zone lightmap bake (ZoneBake097, -bake: bakes fine, draws wrong; needs an Editor session), the painted albedo variation (audit
item 4: concept art first, then Sky), the PhoneBooth finish (Glint), the café's interior and the night look in OldTown (none in the played scene yet).
For Sky: the look in a build; the Air sitting with the switches above. For Keel and Motive: Dumpster_Civic carries the stash tag the procedural
dumpster stood for (same site, holds 1). For Latch: the camera FX and the police-post pulse are coded and compile, not yet seen in a capture.

## Round 2 landed (2026-11-06; commits 34c5ef72, 9ce61708, fec8a285, 389a47c0)
The zone's lightmaps work and are in the real pipeline (batch unity-world: ZoneBake097.BakeTown after Install), calibrated to the starting look; the painted variation; PhoneBooth_Play and the topiary pot; camera FX and the police-post pulse seen in captures; the shader warm-up re-recorded. Everything, with costs: briefs/097/notes.md; pictures: briefs/097/index.html (on disk).

### For Keel (engine inputs: not regenerated by this round)
The new kit pieces' collide volumes (`bun run collide`) change what `bun run sim:site` / `bun run cover` would write: a few more blocked walk cells round the police posts and the robot dock, and civic-robot-dock-z1's stand 1.5 m east. The engine still runs on today's committed sim/data, so play is unchanged; the freshness tests in tools/sim/sim.test.ts (site, takedown, walk grid) and the cover file test fail until Keel regenerates and re-records. The people-file diff in the same test (s1-gardens, t1-rocket) is not from this round.

### Test status after round 2 (`bun run test`, 2,623 tests)
Fixed here: the kit and stash expectations (kit020, quiet), the kit digest fixture, the HostViews.HoverCam proof row. Still failing and not from this round's code as far as read: the comment lint (FrontEnd.Proof.cs:58), host rows for HostGoWaits / HostHelpOffer / HostDrive, the levels code hash (tools/names/ban.ts), the routines bake (the 5:30 shift's +40 s), plan/world rule examples and the levels pipeline stubs, the headless engine bands. The presentation crowd drops one walk loop (likely a new prop on its path; presentation cameras only).
