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
 12 Lightmaps (ZoneBake097, -bake): the zone's opaque pieces lightmapped (Baked Indirect: the sun stays realtime), the town round it
    contributing by probes, a probe grid at feet and heads for people; the contact strips drop to 0.55 where lightmaps exist.

Fixes found on the way: GlassSky (DK_GlassDark) is the zone's glass, not the window enamels (those are the old town's); rooms there replace the
bay's dark back wall, walls only (the Records Office dome went black when the glass over it took a room). The leaf shader's Metal pipeline error
came from a stage-only (_vertex) wind keyword: now a plain shader_feature_local. Clouds at 190 m tiles read as the whole 35 m frame dimming;
90 m tiles put a cloud's soft edge inside the frame. Iteration rule learned: an edit to any include of the Lit shader recompiles all its variants
(30+ min cold); tuning goes through globals (look097.json) instead.
