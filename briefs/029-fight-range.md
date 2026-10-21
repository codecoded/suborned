# 029 · The fight range

**Status (2026-10-20):** open. Asked by Sky after trying the fight lab in the Handover: "I think I need a proper lab rather than in game - a sort of graybox shooting area with other NPCs firing. A playground as such so I can get a real feel for what's going on, because right now really confusing." Owner: Aster.

## Objective

A separate graybox scene, reached from the start menu ("Fight range"), where Sky can feel the brief 027 fight rules on their own, without the Handover's city, schedule, cast or mission around them.

- **Ground:** a flat, well-lit grey floor with a 1 m grid and 5 m distance rings, so ranges read at a glance (15 m effective, 16 m max).
- **Cover:** a few kinds of graybox cover, each labelled by shape: low wall (half cover), high wall (full), pillar, car-sized box. Placed in a fixed, readable layout; a second layout with more cover.
- **The cell:** the four agents at one end, the same orders, HUD and fight display as the lab (freeze, target, cover marks, ghosts, forecasts, health bars).
- **Hostiles:** pick from a panel: police (amber), rivals (violet), the response unit; how many (1–6); where (near, at the effective ring, beyond it, behind cover). They use the real fight AI (kite response, contest, cover), not a script.
- **Controls:** Restart (R), switch preset (today / lab), switch fight mode (realtime / freeze / target), slow time (½×), and a "show the numbers" toggle (hit chance, range, cover state on each line of fire).
- **Same engine:** it runs the real sim with a synthetic site; every run replays; no AI at runtime. It never touches the Handover's data or replays.

## Done-check

Sky can open the range from the menu, spawn three police behind cover at 20 m, watch them answer fire, move an agent into cover and see the hit chance change, all in under a minute, without reading anything longer than a line. Unity match and engine tests green; the Handover's `today` hashes unchanged.

## Owners

Keel (the synthetic site, start state, spawn presets, a `range` scenario set), Latch (scene, menu entry, spawn panel, controls), Glyph (one review of the panel and the numbers overlay), Suture (a 10-minute uncoached try against the done-check).
