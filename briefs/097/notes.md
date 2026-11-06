Brief 097 · the Handover zone at its best (main session, 2026-11-06, branch work/097-handover-look)

Target (concept art first, Codex $0, the style contract's concept_prompt + an open brief, today's frame as the reference):
  paid/097-look-bar/codex/office-y210-bar.png, square-y030-bar.png. Contact shade under every prop, slab variation, granite kerb,
  even asphalt, lit rooms behind glass, spray and mist on a mosaic pool, birds, warmer longer sun (the grade is Sky's pick: not retuned).

What changed (every item a look only; nothing in the sim reads it):
  1 Wind (DK_Wind097.hlsl, _DK_WIND): cypress, palms (fronds by distance from the trunk), shrubs, beds, petals sway and flutter, in the
    colour, shadow and depth passes alike. Globals from Suborned.Look097 (Resources/Atmosphere/look097.json). -nowind for an A/B.
  2 Cloud shadows: URP's main-light cookie (already enabled in the asset), a tiling soft cloud mask made at load, drifting down-wind;
    30 % of the sun under a cloud's middle. -noclouds for an A/B.
  3 Fountain: the testbed's water ported (Suborned/Fountain Water: swells, chop, the mosaic through the water, caustics, the 12 landing
    rings, the wet pedestal ring), flowing jets and pulsing landing rings (Suborned/Fountain Jet), spray and mist at the 12 landings (Fx097).
  4 Rooms behind the glass (DK_Interior097.hlsl, _DK_INTERIOR): interior mapping on the tower, film, teal and lobby panes: pastel rooms,
    ceiling panels, blinds, desks; no texture, glass pixels only. _DKInteriorOff for an A/B.
  5 Contact shade: warm-neutral tint (#958c87, was a lilac that read magenta) and sun-aware (0.45 of it kept where the sun falls).
  6 Vertex AO (Vela, tools/decokit/vertex_ao.py + _DK_VERTEX_AO): Redline's audit item 1, approved 2026-11-05.
  7 Life: pigeons and doves on the square that scatter from people (Birds097, one instanced draw), steam from three drain grates,
    petals off the flower beds (Fx097).
  8 Variants (audit item 5): ShaderStrip097 strips the families the game never uses (vertex lights, screen-space shadows, DBuffer, SSR,
    screen-space irradiance, Quest, soft LOW). First try also stripped _LIGHT_LAYERS and _ADDITIONAL_LIGHT_SHADOWS and the zone drew white:
    URP turns them on for every draw while the asset supports them. They are now off at the source instead (PC_RPAsset: light layers and
    additional-light shadows off; nothing in the game used either: only the suns cast shadows, no light uses a layer). URP already strips the
    other families itself, so the stripper is a guard. Vertex AO is keyword-free (a mesh with no colour stream reads white), so it doubles nothing.
  9 Lean scratch builds (-lean): the zone plus 110 m; 36-44 s builds for iteration. Perf that counts is taken on the full scene.

 10 Props (Vela): the four procedural boxes are kit pieces now: PolicePost (8; its opal dome pulses amber once the police are called),
    MastCamera, CivicRobotDock, Dumpster_Civic. Concepts first (paid/097-props/codex), palette materials, AO on, 2.2-3.5k tris.
 11 Play's moments (Fx097.Events): a camera put out throws hot sparks and smoke and sparks again where it lands; a looped camera crackles
    blue-white as the loop takes and every 2.3 s while it holds; the police posts' domes pulse from security.called.
 12 Lightmaps (ZoneBake097, -bake): NOT IN THE BUILD. Built and run twice: the GPU lightmapper bakes the zone in 4-5 minutes (4,900 renderers,
    6 maps of 2048, ~24 MB), but the player draws the lightmapped zone blown out with per-texel noise, with or without the GPU Resident
    Drawer. The sky it saw and the scale/offsets it wrote are sane; the lightmapped meshes seem to be sampled through other UVs than the
    baker's. Parked behind its opt-in flag for an Editor session; it is the biggest lighting gain still open (true contact occlusion and
    warm bounce, ~0 ms per pixel). Its side effect (lightmap UVs switched on in 146 kit importers) is put back.

Fixes found on the way: GlassSky (DK_GlassDark) is the zone's glass, not the window enamels (those are the old town's); rooms there replace the
bay's dark back wall, walls only (the Records Office dome went black when the glass over it took a room). The leaf shader's Metal pipeline error
came from a stage-only (_vertex) wind keyword: now a plain shader_feature_local. Clouds at 190 m tiles read as the whole 35 m frame dimming;
90 m tiles put a cloud's soft edge inside the frame. Iteration rule learned: an edit to any include of the Lit shader recompiles all its variants
(30+ min cold); tuning goes through globals (look097.json) instead.

Cost (Studio M5 Max, 1920x1080, full OldTown scratch build vs the round's starting player 0.2.253; GPU ms medians of 3-4 alternating
runs; other sessions' Blender jobs kept the load at 8-12, so treat 0.1 ms as noise):
  office y210: +0.1 ms (1.57 -> 1.69); office y300 (most glass): +0.14; the square (planting, fountain, birds): +0.2 to +0.4 after the fix below.
  By switch (full scene): clouds 0.08-0.26 ms, rooms 0-0.17, wind and AO within noise, FX (particles, birds) within noise.
  The square's non-switchable part was mostly the contact shade's new shadow lookup run as a 9-tap soft filter on every strip pixel:
  now one hard tap. Mean frame time (CPU-bound on the Studio) +0.05 to +0.16 ms.
  Air estimate: GPU work scales about 4-5x, so up to 1-2 ms on the square; every feature has a switch for the Air sitting (Redline §4):
  -nowind -noclouds -norooms -noao -nofx. Variants: 4,896 -> 1,920 for the cutaway forward pass (light layers and additional-light
  shadows off, AO keyword-free), so the first-use shader hitches and the build's cold compile both shrink.
Triangles: the AO cuts add up to 15 % on the big flat pieces (RecordsTower 34.9k -> 39.7k); Vela's rebuilt lockers (10.7k -> 2.9k), cola and
  snack vending (4.0k/3.2k -> 2.3k) and police posts (3.3k) more than pay for it. Draw calls: +1 birds, +6 particle systems, nothing per prop.
Paid spend: $0 (Codex stills for the concepts and the props; Tripo not needed: Blender reached the bar for every piece reviewed).
