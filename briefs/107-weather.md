# 107 · Weather and environment: haze, night, rain

Owner: Glint (lighting-artist), with notes for Vela (wet materials), Chime (rain sound), Pulse (people in rain). Aster, 2026-11-11. Next owner: Aster → Sky (pick).

Sky, 2026-11-11, after the Unity plugin research: "But also think about the weather, environment. Haze, night time, rain etc". The research report (`out/research/reports/Unity 6 plugins for The Suborned.md`) skipped sky and weather packs because the sky is rarely in frame; Sky wants the question taken seriously at street level.

## Where we are
- Time of day: `TimeOfDay097.cs` (brief 097) drives sun, sky, exposure and probes, and blends Adaptive Probe Volume scenarios (morning, noon, evening, night). Night is a look only; no rule reads it (decision log Q7, still open: day, night, or chosen at the briefing?).
- Haze: `DistanceHaze.cs`. The play camera (orthographic) has **fog off**, because URP's fog breaks under an orthographic projection; haze only shows in perspective cutaways and flybys.
- Night grade, `RoadLight.cs`, `SignalLight.cs`, emissive windows. No weather at all.

## Objective (a study, no build)
A page, `briefs/107/glint/weather.html`, with the options for each of: **haze at the play camera**, **night**, **rain** (falling rain, wet streets and puddles, splashes, reflections), and anything else worth it (overcast, mist, wind). For each:
- how it would be made: built-in URP (VFX Graph or particles, a global wetness value in our shaders, decals for puddles, reflection probes, a depth-based haze pass for the ortho camera) vs an asset (Kronnect Volumetric Fog & Mist 2, Shiny SSR 2, Enviro 3, COZY 3, rain or wet-surface packs; check each for URP 17 Render Graph, Metal, last update, price);
- what it costs on the Air at play zoom (estimates, from the asset's docs or our perf data, labelled as estimates);
- what it does to readability: signals (cones red, guard amber, rivals violet), outlines, the ring and the 80 px people must stay readable;
- what it needs from others: wet materials (Vela), rain sound (Chime), umbrellas and people seeking cover (Pulse, Forma).

## Design questions for Sky (don't decide)
Should weather or night ever change play (sight ranges, crowd size, faces hidden under umbrellas, which suits social infiltration), or stay a look only? Is weather rolled by the seed per game, chosen at the briefing, or fixed? Put options and a recommendation on the page; the GDD changes only after Sky rules.

## Rules
- Desk study: no Unity runs this round (Latch has Unity for brief 106). Read code and docs; reuse existing captures under `briefs/097/` etc. if useful.
- Concept stills for the look come from Aster in the main thread (`briefs/107/concepts/`); use them on the page.
- No paid calls. Don't commit; Aster commits. Plain English, short.

## Ruled (Sky, 2026-11-11: "Yes to all three")
Look only for now; the weather is rolled from the seed into each game's start state (on its own stream, so saved runs replay unchanged) and can be changed at the briefing; it holds for the whole mission. Buy nothing yet. Decision log "After v0.39.0".

## The round (one owner per piece; Latch builds the view end to end)
1. **Keel** (own worktree): `weather` and the day/night choice in the start state. A rulebook dial for the weather's odds (clear, overcast, rain, mist; Glint's values), read by no rule; a briefing order to change it; logged at start. Old saves without it replay as today (clear, day). sim:test + the sweep must be unchanged.
2. **Vela**: the night glows Glint names (seafoam inlay bands, planter feet; lit rooms behind glass are Latch's `look097.json`) as kit candidates, no Unity.
3. **Chime**: rain sound (shower bed, rain on awnings and parasols, drips, wet steps) and a night street bed, made the way the 087 pack was (Seed Audio / Mirelo, under $2), a page for Sky's pick.
4. **Latch**, after brief 106: fix night in the old town (Glint's fix: one driver of the play light, a LightPhase from Glint077's night data, no silent fallback, the batch's night-vs-day brightness check), then rain (wetness and puddles in our shaders, streak reflections as decals, VFX Graph rain, rings drawn after the rain), then the ground mist pass; read Keel's weather; adopt Vela's glows in one kit step; one `--sky` build.
5. Later, own step: umbrellas (Forma: a worn prop on about a third of the crowd, pale, never the cell) and people sheltering (Pulse), once rain is in.
