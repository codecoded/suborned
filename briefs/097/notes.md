Brief 097 · the Handover zone at its best (main session, 2026-11-06, branch work/097-handover-look)

Target (concept art first, Codex $0, the style contract's concept_prompt + an open brief, today's frame as the reference):
  paid/097-look-bar/codex/office-y210-bar.png, square-y030-bar.png: contact shade under every prop, slab variation, granite kerb, even
  asphalt, lit rooms behind glass, spray and mist on a mosaic pool, birds, warmer longer sun (the grade is Sky's pick: not retuned).

LIGHT
  Zone lightmaps (ZoneBake097; the batch's unity-world bakes OldTown after Install): Unity's GPU lightmapper, Baked Indirect: the sun, its
    shadows and the cloud cookie stay realtime; the lightmaps hold the sky's occlusion and the bounce: soft shade at every wall foot, kerb,
    planter, bench and step, warm bounce into the shade. 5,302 renderers, 13 maps of 2048 (8 texels/m, 768 samples), a light-probe grid at
    feet and heads so people and props pick up the same light. Calibrated to the round's starting look (means 173/140/168 vs 176/134/169;
    sun-to-shade on the road 1.47 vs 1.49): sky x1, bounce x1, play exposure +0.6 EV and contact strips x0.55 when the scene is lightmapped.
    Four causes fixed on the way: Automatic lightmap stripping read EditorBuildSettings (SampleScene only) and stripped LIGHTMAP_ON (Custom
    stripping now keeps the plain lightmap variants); the bake lit the zone with the old stage's sun (3, other heading) instead of the play
    light; Lightmapping.Bake re-rendered the baked reflection probes from the raw editor scene and flooded the square white (probes now kept
    as they are); merged tiles' UV2 checked as a real lightmap layout. Cost on disk ~50 MB in the player.
  Cloud shadows: URP's main-light cookie, a soft cloud mask made at load, drifting down-wind (90 m tiles, 30 % of the sun). -noclouds.
  Contact shade: warm-neutral (#958c87, was a lilac that read magenta), sun-aware (0.45 kept in sun), one hard shadow tap.
  Vertex AO (Vela, tools/decokit/vertex_ao.py): every zone piece; keyword-free in the forward copy. -noao.

SURFACES AND SHADERS
  Rooms behind the glass (DK_Interior097 in GlassSky, walls only): pastel rooms, ceiling panels, blinds, desks; no texture. -norooms.
  Painted variation (Redline's audit item 4; DK_Variation097): a soft sun-faded wash, world space, one fetch, made at load, mean exactly 1. -novariation.
  Fountain: the testbed's water ported (swells, chop, the mosaic through the water, caustics, 12 landing rings, the wet pedestal ring),
    flowing jets and pulsing rings; spray and mist at the 12 landings.
  Wind (DK_Wind097, _DK_WIND) in every pass: cypress, palms (fronds by distance from the trunk), shrubs, beds, petals. -nowind.

ASSETS (Vela, Blender DecoKit, Codex concepts first, palette materials, AO on)
  New kit pieces for the zone's last procedural boxes: PolicePost (8, concept A's round drum, opal dome), MastCamera, CivicRobotDock,
  Dumpster_Civic. Rebuilt: Lockers (10.7k -> 2.9k tris), RecyclingBins, Cola and Snack vending (4.0k/3.2k -> 2.3k), PhoneBooth_Play
  (boulevard coral, chrome rims, brass hairline on a limestone plinth), PottedTopiary pot. Tripo not needed: Blender reached the bar.

LIFE AND JUICE (all looks; nothing in the sim reads them)
  Pigeons and doves on the square that scatter from people (one instanced draw); petals off the beds; steam from the three grates nearest
  the square. A camera put out: a hot flash, spark streaks and smoke, sparks again where it lands; a looped camera crackles blue-white;
  once the police are called every police post's dome pulses amber. -nofx; -fxdemo fires the camera moments for captures.

COST (Studio M5 Max, 1920x1080, final full OldTown scratch build vs the round's starting player 0.2.253, GPU ms medians, other sessions'
  jobs kept the load at 8-24, so 0.1 ms is noise): office y210 1.70 vs 1.65; office y300 1.98 vs 2.10; square 1.89 vs 1.85. The lightmaps
  replaced per-pixel ambient work and paid for the rest. Mean frame +0.1-0.4 ms (CPU: particles, birds, cookie). Draw calls: +1 birds,
  +6 particle systems. Triangles: AO cuts up to +15 % on big flat pieces, more than paid back by the rebuilt furniture.
  Variants: the cutaway forward pass 4,896 -> 1,920 (URP light layers and additional-light shadows off: nothing used them; AO keyword-free).
  The Air: every feature has a switch for the sitting (Redline §4).
  Batch: unity-world now 21 min with the bake (it runs only when the world or kit changes).
  Shader warm-up re-recorded (briefs/096/latch/prewarm.md's steps; Resources/Prewarm/play-metal.graphicsstate: 151 variants / 199 states, was
    103 / 130). Cold Metal cache, Studio: first third person 27.6 ms (0.2.253: 28.6), quarter turns 5-7.5 ms, cutaway 13.2 ms (13.0); the
    warm-up behind the loading bar on a build's first launch 33.4 s, worst frame 10.2 s (0.2.253: 27.3 s, 8.0 s): +6 s once per build.

DAMAGE FOUND AND REPAIRED IN THIS ROUND
  A bake attempt set every material's GI flag for the bake; URP's validation stripped _EMISSION from 23 kit materials (lamps, signals,
  glows) before it was put back. Restored from the catalogue's sources; the bake no longer touches GI flags. A dead keyword from an early AO
  attempt was cleared from 354 materials (the world builder refused a poster variant over it).

OPEN
  Sky's eye on the look in a build; the Air sitting with the switches. Keel and Motive: Dumpster_Civic carries the stash tag the procedural
  dumpster stood for (same site, holds 1). Night is not played in OldTown (no night phase), so no night work here. kit.test.ts: 4 tests
  fail the same way at the previous commit (they read this machine's generated output).
