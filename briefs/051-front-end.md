# Brief 051: front end (main menu, options, controls, mission brief)

**Status (2026-10-23):** open. Sky: "we also need proper game menu options / controls and then when starting new game - for now we just show a mission brief at the start explaining the lore of the mission".
**Owners:** Glyph (layout and spec, beside the loading screen of brief 047), Fable (the mission brief's words), Latch (builds it once both land; one build and test, code first) · **Next:** Sky plays it.
**Done-check:** launching the game runs loading screen → main menu → New game → mission brief → the existing briefing (until Begin) → play. Options and Controls work from the main menu and the pause menu, and are saved and restored between launches. `game:check` passes, and the capture set has frames of every screen at 1920x1080 and 2880x1864.

## Flow
1. **Loading screen** (brief 047, picture B, swappable).
2. **Main menu** over the same picture: New game · Continue (only when a saved run exists) · Options · Controls · Quit. The build number goes in a quiet corner. The playground panel (P) and the camera lab stay debug-only, not menu items.
3. **New game → mission brief:** one screen that tells the story of the mission before play: who Councillor Vale is, the file on the cell he hands to Security at shift change (Sky, 2026-10-23; replaces the clearance order), why the cell takes him alive, the city's surface and what lies under it. A few short paragraphs, then Continue. This is outside play, so the "no sentences in play" rule (§10) doesn't apply; the existing tactical briefing (modal until Begin) follows unchanged.
4. **Pause menu (Esc), a rework (Sky: "which needs a lot of work"):** today it's one column of up to 14 buttons, and most of them are playground toggles that step a setting when clicked (control scheme, leash settle, conversations, action plate, quick action, click acts, aim hold/toggle, follow, hold fire, third-person right fires, plan together, edge scroll), plus Restart, Fight range and Quit. It becomes a short menu: Resume · Options · Controls · Restart mission · Fight range (while the range exists) · Quit to menu · Quit. Every toggle moves to a labelled control on an Options page (a switch or a choice showing its current value), mostly Gameplay, with a "Playground" group for the brief 025/041 experiments until Sky decides them. Nothing is lost: each keeps its saved value.

## Options (first cut; Sky adjusts after playing)
- **Display:** window mode, resolution, render scale, quality (the Air's default), frame cap or vsync, HUD scale.
- **Audio:** master, music, effects, voices (sliders even while there's little audio).
- **Camera:** pan speed, edge pan on/off, orbit sensitivity, invert orbit, trackpad drag orbit, third-person distance (from the camera lab presets).
- **Gameplay:** hints on/off, default game speed.

Settings are saved to one JSON file in the player's data folder; Reset to defaults works per page.

## Controls
- A page listing every binding from the one table (GDD §10: "bindings remappable from one table"), grouped as command, camera, fights and third person.
- Click a row, press a key, and it rebinds. A clash is shown and resolved, never silently overwritten. Reset to defaults.
- The page reads the same table the game uses, so it can never drift from the real keys.

## Mission brief as data
The brief's words live in the mission folder (`contracts/missions/handover/brief.json`: title, place, time, paragraphs), not in code. Missions generated later supply their own brief the same way (systems > scripts). Fable writes the Handover's from §01 and §07 in the GDD's world voice. People prompts carry no date, so neither does the prose: the cell are visitors from a nearby timeline, and the city is a 1950s world that discovered atomic power centuries ago.

## GDD
The front end is new to the GDD. It is queued for v0.25 (§10, presentation) with Sky's words; this brief is the source until then.
