# 025 · Playground: camera and play-style experiments

**Status (2026-10-19):** open. Asked by Sky: "I like 2.5D for planning, but once I've got a plan / action being done, I want to follow / take control of the player to get there and enjoy it, make the action, and then control the player to the next action. Sometimes just want to explore. The shooting was always going to be more Syndicate simplicity rather than accuracy. I think the camera angle needs work - would love a debug on this to try various options. I feel 2.5D zoom has been too limited for me and third person maybe too down to the ground or close etc. I need to have more creative freedom to try out various gameplay styles to make a decision." Owner: Aster.

## Objective

Give Sky a playground inside the game build to try play styles before the GDD changes. Nothing here is a design decision yet: every new behaviour sits behind a playground switch, and the GDD (v0.24) changes only after Sky has played it and decided.

1. **Camera debug panel** (Latch). A hotkey panel in the build with live sliders and switches for both views, and named presets Sky can save, load and send back:
   - overhead: zoom range (far wider than today), pitch, free yaw or quarter steps, orthographic or perspective, field of view;
   - third person: distance, height, pitch, shoulder offset, field of view, orbit on drag (trackpad and mouse), zoom range, collision with walls and ground, smoothing;
   - presets saved as JSON beside the app and in `briefs/025/presets/`, so a preset Sky likes becomes the default.
2. **Take control** (Keel engine, Latch view). From any view, drop into the selected agent and move them freely (WASD, Shift to run, camera-relative), Unity-smooth. The engine follows the driven agent's position each tick (`drive` samples in the order log, validated against the walk grid), so sight, witnesses, Trail and replays keep working. Act on whoever you're near (E, with the price chip), then walk on to the next action. Switch back to the map at any time. Keel's note: `briefs/024/keel-wasd.md`; Glyph's controls notes: `briefs/024-third-person-controls.md`.
3. **Explore.** Walk the district with no clock pressure (the mission clock paused, or a free-roam start with no mission) to enjoy the world at street level.
4. **Fights stay Syndicate-simple:** hit chances, not aiming (Sky, 2026-10-19).
5. **Not Ghost Recon** (Sky, 2026-10-19: "seems I was off the mark for that"). No squad-shooter framing: no cover shooting, aiming or command wheel as a design goal. Where brief 024 says otherwise, this brief wins.

## Playground switches (defaults for the first build; each one is a switch, not a ruling)

- Moving a busy agent cancels the action at its cancel cost.
- On release the agent stands where left; nothing resumes.
- A driven run shows its Alert risk live on the runner (no price card before it).
- People keep a 40 cm gap from the driven agent.
- Walk and run speeds as the rulebook (1.4 / 2.8 m/s), with a playground multiplier.

## Limits

- Planned missions keep "same orders, same result"; driven play replays from its logged samples.
- 60 fps at play zoom on the 15-inch MacBook Air M4 (report frame time at street level too).
- The GDD is unchanged until Sky decides; new rulebook entries are marked experimental.

## Done-check

Sky can open the panel, change camera values live, save a preset, take control of an agent on the Handover, walk to Inez and act, return to the map, and roam freely; `game:check`, the Unity match and `bun run verify` pass; a driven session saved in the game replays headless on every checkpoint.

## Next owner

Latch (camera panel first, then the driven mover), Keel (driven agent in the engine), Glyph (the ground HUD from brief 024, as switches), then Sky plays.
