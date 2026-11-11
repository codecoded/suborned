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
