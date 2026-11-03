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

## Restyle: direction A, "light line" (Sky, 2026-10-23)
Sky on the built menu: "not into the highlighted design - let's make the menu button / options really nice" (the cream chrome-capped pill buttons and the cyan focus ring). Four concept directions were made, free with Codex (`paid/051-frontend/codex/{menu,options}_{a-lightline,b-enamel,c-console,d-editorial}.png`), and Sky picked A: "A seems ok".
- **A:** no button plates. Items are text on the existing dark scrim; the focused item is brighter, with a thin turquoise-light line beneath and a small marker that slides between items; the others are dimmer cream. Options is a dark smoked-glass sheet with hairline rows, plain tabs underlined in turquoise-light, and the focused row marked by a side tick plus underline.
- **One language** for the main menu, Options, Controls, the mission brief's Continue and the pause menu.
- **Owners:** Glyph writes the spec and mockups, then Latch builds once, code first. **Done-check:** the capture set from the first build, redone, matches the spec, and `game:check` passes.

### Direction A spec and mockups (Glyph, 2026-10-23)
Spec, numbers, every state, timing, contrast and port notes: `briefs/051/lightline/SPEC.md`; pictures in `briefs/051/lightline/` (menu, Options x4, Controls x3, brief, pause at 1920x1080 and 2880x1864; state and timing boards; 2x crops; `contrast-report.txt`); numbers `briefs/051/lightline/frontend.a.json`; the live page `testbed/hud-style/frontend.html` (`bun run hud-style`). Measured at the Air: left menu scrim 66 %, brief scrim hold 656 ref (the old hold left the dossier heads at 1.7:1 at 16:10). **Next owner: Latch** (build once, code first).
