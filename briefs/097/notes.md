Brief 097 · the Handover zone at its best (main session, 2026-11-06, branch work/097-handover-look)

Target (concept art first, Codex $0, the style contract's concept_prompt + an open brief, today's frame as the reference):
  paid/097-look-bar/codex/office-y210-bar.png, square-y030-bar.png: contact shade under every prop, slab variation, granite kerb, even
  asphalt, lit rooms behind glass, spray and mist on a mosaic pool, birds, warmer longer sun (the grade is Sky's pick: not retuned).

LIGHT
  Zone lightmaps (ZoneBake097; the batch's unity-world bakes OldTown after Install): Unity's GPU lightmapper, Baked Indirect: the sun, its
    shadows and the cloud cookie stay realtime; the lightmaps hold the sky's occlusion and the bounce: soft shade at every wall foot, kerb,
    planter, bench and step, warm bounce into the shade. 5,302 renderers, 13 maps of 2048 (8 texels/m, 768 samples), a light-probe grid at
    feet and heads so people and props pick up the same light. Calibrated to the round's starting look (means 173/140/168 vs 176/134/169;
    sun-to-shade on the road 1.47 vs 1.49): sky x1 pulled to the warm equator colour, bounce x1.3, play exposure +0.4 EV and contact strips x0.55 when the scene is lightmapped (see COLOUR).
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
  once the police are called every police post's dome swings between deep amber glass and a white-hot glow (1.1 s; one shared runtime
  material on the dome's slot at every LOD). -nofx; -fxdemo fires the camera moments and the call for captures.
  Seen in captures: out/scratch/main097/shots/fxfull4 (flash, spark streaks, smoke), post7 (the dome's pulse), full4 (the baked zone).

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

COLOUR (Sky 2026-11-06: "why you reduce the colors, and made them washed out again? Everything seems to be converging on blue")
  Measured against the round's start: saturation down, shade cooler. Causes: the baked sky was the trilight's blue top lighting every shade;
  the exposure lift (+0.6 EV) washed colour out; the rooms' palette was cool. Fixed: the bake's sky colour is pulled to the warm equator
  colour (look097.json bake.skyWarm), warm bounce x1.3, exposure in a lightmapped scene +0.4 EV, the rooms cream/apricot/butter/rose/mint;
  Vela recoloured the zone's props toward the boulevard palette.

DECALS (Sky: "URP Decal projector sounds interesting")
  URP's Decal renderer feature on the play renderer (Decals097, Screen Space: no DBuffer prepass, no new lit variants, decal layers off,
  120 m). Three projected looks made at build time: the fountain's wet halo on the paving, fallen petals under each flower bed, and a chip
  mark where a missed shot lands (pool of 32, fading out over a minute, so a long fight never adds draws without end). A miss also throws
  grit and dust; a shot that cover takes throws chips off the prop.

CAFÉ GLASS (Sky: "the cafes window look bad")
  The white diagonal scratches were modelled: cafe_upper.py lays two painted reflection streaks a pane (DK_Glass_Sheen, alpha 0.2), made when
  the glass was flat enamel; over GlassSky's own sheen and rooms they read as scratches. Now a faint glint (alpha 0.06, a look097.json row).
  The glaze is warm amber-brown (was dark teal, which read mauve under the grade), the film thinner at grazing (_Edge 0.92 -> 0.72) so the
  rooms show from the play camera, and the rooms fade out where the glass curves (screen-space curvature) instead of smearing into stripes.

AIR (15-inch MacBook Air M4, Sky's OK 2026-11-06; briefs/097/air/: run.sh, rows, air-perf.json)
  1920x1080 windowed, vsync off, on AC, after a warm-up launch; order base, final, all-switches-off, base, final. GPU / mean ms:
    square y030   0.2.253 5.1-5.7 / 7.3-7.9   097 6.0-6.7 / 7.6-8.4   097 switches off 6.1 / 7.7
    office y210   4.2-5.3 / 5.2-6.7           5.0-5.8 / 6.3-7.4       5.3 / 7.0
    office y300   5.0-6.8 / 6.4-8.8           6.0-6.7 / 7.6-8.5       6.1 / 7.9
    third person  8.2-9.5 / 11.5-13.6         10.1-10.8 / 13.7-15.1   10.2 / 11.7
  The Air warms through the run (base's second pass is up to 1.8 ms slower than its first), so each build's two passes give its range.
  Play zoom: about +0.5-1 ms GPU, 120-160 fps, far inside 60. Third person: about +1.5 ms GPU, mean 66-73 fps for 097 (73-87 for 0.2.253);
  p99 23-30 ms in both builds (the follow camera's own hitches, not this round's). The switches barely move the GPU: the cost is the baked
  lighting itself (lightmap fetches and probes), which has no runtime switch.

DAMAGE FOUND AND REPAIRED IN THIS ROUND
  A bake attempt set every material's GI flag for the bake; URP's validation stripped _EMISSION from 23 kit materials (lamps, signals,
  glows) before it was put back. Restored from the catalogue's sources; the bake no longer touches GI flags. A dead keyword from an early AO
  attempt was cleared from 354 materials (the world builder refused a poster variant over it).

OPEN
  Sky's eye on the look in a build; the Air sitting with the switches. Keel and Motive: Dumpster_Civic carries the stash tag the procedural
  dumpster stood for (same site, holds 1). Night is not played in OldTown (no night phase), so no night work here. kit.test.ts: 4 tests
  fail the same way at the previous commit (they read this machine's generated output).
