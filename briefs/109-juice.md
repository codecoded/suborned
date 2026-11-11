# 109 · Juice: make the game pop

Owner: Latch (unity-engineer), end to end. Chime matches sounds after. Aster, 2026-11-11. Next owner: Aster (commit), then Sky.

Sky, 2026-11-11: "what VFX does unity have to make our game 'pop' a little more and give it a bit more 'juice'", then "yeah, let's add juice. And surely using shader graph if means faster iteration is better?"

## Rules for every effect
- Tied to something real in the engine (an order, a takedown, a shot, Alert, the getaway), so it tells the player something. View only: nothing reads it, and replays don't move.
- No slow motion, hit-stop or time-scale tricks: the game clock stays true. Punch comes from the view (shake, flash, light, particles).
- Readability first: signals (cones red, guard amber, rivals violet), the teal rings, outlines and the 80 px people stay readable.
- Air budget: the play view is about 11 ms today against 16.7 ms. Aim at 0.3 ms or less per effect, and measure on the Studio. Ask Aster before any Air run.

## Tools (Unity's own first)
- **Cinemachine 3.1** with Impulse for shake. The current tactics camera stays in charge and drives a Cinemachine camera in "Do Nothing" mode, with damping 0 so the quarter-step turns aren't smoothed. Tick it with the sim's frame where a replay needs it.
- **VFX Graph** (com.unity.visualeffectgraph, matching URP 17.6) for GPU particles. Shuriken stays where it already works.
- **Shader Graph** for new effect shaders. Sky's point is iteration speed, so test it first on one effect: can the crew author and edit .shadergraph and .vfx assets reliably, through Editor scripting, templates or the Unity CLI? If yes, new effect shaders use it. If not, say why plainly and use hand-written HLSL, with every tuning value in a json the player reloads (like -weatherJson), which gives the same fast iteration.
- **UI motion**: UI Toolkit's own USS transitions first. Use PrimeTween only if they can't do it.

## The moments (build three first, then the rest)
1. **Orders:** the ring pops on click, the destination pulses, a dust puff where the agent sets off, HUD numbers tick instead of jump.
2. **Takedown:** a pop on the Enforcer's bobble-head action pip, and on the edge marker or portrait rail when off screen. (Sky, 2026-11-11: "lose camera shake"; "not even sure if screen should darken on take down...since that can happen off camera"; "I do like our mini bobble head action PIPs".)
3. **Getaway:** tyre smoke, exhaust, headlight flares, a camera that follows the car out.
4. **Turning someone:** a warm flare on them.
5. **Alert rising:** on the HUD's Alert pips, never a screen tint (no screen-wide effects for events).
6. **Gunfights:** muzzle flashes that light the street for a frame or two, tracers, stone chips, glass. No shake.
7. **The living city:** tram pantograph sparks, sky-traffic trails, night searchlights in the sky, the odd neon flicker, a soft glow on the robots' faces.
8. **A miniature look trial:** tilt-shift depth of field (Gaussian) at play zoom, as a switch to judge.

## Done-check
1. **Show Sky the first three:** build moments 1–3 behind one launch switch, then capture a short before/after clip of each (the batch's clip capture). Sky judges before the rest are built.
2. **Then the rest**, each behind the same switch, with a Studio cost table per effect.
3. **One `bun run batch --sky` build per showing.** No full batch per attempt.
