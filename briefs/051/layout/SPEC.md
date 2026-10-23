# Front end: layout spec for Latch (brief 051, Glyph, 2026-10-23)

Main menu, Options (four pages), Controls (the binding table), the mission brief and the reworked pause menu, in the HUD kit's language, on the loading screen's picture (brief 047). Everything is drawn from `frontend.json` (this folder) by `fe.py`, which is the reference implementation; the PNGs here are its output at 1920x1080 and 2880x1864. Regenerate: `python3 -I fe.py` (about 25 s).

| Screen | 1920x1080 | 2880x1864 |
|---|---|---|
| Main menu (first, centrepiece) | `menu-1920x1080.png` (and `menu-nosave-` without a saved run) | `menu-2880x1864.png` |
| Options: Display / Audio / Camera / Gameplay | `options-display-`, `options-audio-`, `options-camera-`, `options-gameplay-` `1920x1080.png` | same at `2880x1864` |
| Controls: browsing (a row waiting for a key) / a clash | `controls-command-`, `controls-clash-` | same |
| Mission brief | `brief-1920x1080.png` | `brief-2880x1864.png` |
| Pause menu (over a game capture) | `pause-1920x1080.png` | `pause-2880x1864.png` (the capture is 1080p scaled up; the real one will be sharp) |

## Files
- `unity/Assets/Suborned/Resources/Loading/frontend.json` (copy of `briefs/051/layout/frontend.json`) beside `loading.json` and `loading_bg.png`. Picture, title, cover/crop maths, ref px (`px at 1080p x H / 1080`) and fonts are the loading spec's (`briefs/047/layout/SPEC.md`): **one picture swap changes the loading screen and the whole front end, no code change.**
- Parts come from the kit (`Resources/Kit`): `btn_text_{normal,hover,pressed,disabled}`, `panel_dark_modal`, `plate_light` / `plate_dark` (tabs, keycaps, small buttons), `panel_strip` (the clash strip), `btn_close_normal`, `disc_light_24` (slider and switch knob), `mark_half` (clash badge), `chev_down` (turned for the choice arrows), the `stripes` and icons as already imported. **No new sprite, no new colour.** Fonts: `Jost-Medium` (titles, buttons, keycaps), `Jost-Regular` (body), `Jost-Regular-Tab` (values, build line); Barlow is not used.
- Colours (all `kit.uss`): cream #f7f4ec (type), ink #17302f (type on cream plates), ink-deep #0b1615 (scrims, tracks, row focus), text-dim #b0cdc7 (group heads, "dim" values), chrome #cbc6c3 (rims), panel-line #d2e6e2 at 12-22 % (row and section lines), turquoise-light #60e0ea (the live thing: focus ring and tick, slider fill, switch on, listening ring), brass #99784d (one 1 px rule, the brief's). Kind, role and harm have no colour here.

## Flow and transitions
`loading -> main menu -> New game -> mission brief -> the existing tactical briefing (until Begin) -> play`. Options and Controls open from the main menu and from the pause menu and return to where they came from (Back, Esc, or the corner close).
- Loading -> menu: the picture and title stay; the bar group fades out (loading spec), then the five buttons fade in over 0.40 s, staggered 0.05 s, each rising 6 ref px.
- Menu -> Options / Controls: buttons and title fade out (0.30 s), the even veil (ink-deep 0.52) fades in and the panel fades in over it. Menu -> brief: the title and buttons fade out, the right scrim fades in, the column fades up (0.30 s). Back reverses it. Page switch inside a panel: the body cross-fades in 0.12 s; the frame, tabs and footer do not move.
- Opened from the pause menu there is no picture: the panel stands over the frozen game with the same ink-deep veil at 0.40 (one line of data; the pause menu itself keeps its clear veil, as built).
- Esc: closes the rebind prompt, else goes back one screen, else (main menu) does nothing; in play it opens and closes the pause menu as today.
- Keyboard and controller: Up/Down (or W/S) move the focus, Enter or Space takes it, Left/Right change a choice or a slider (hold = repeat; Shift = x10 on sliders), Tab / Shift+Tab or Q/E switch tabs. The focus is drawn as in the mockups (the cream button gets the hover sprite plus a 2 ref px turquoise-light ring and a soft glow, the same ring as the HUD's "ready"; a row gets an ink-deep 38 % fill and a 3 ref px turquoise-light tick). The mouse hover is the hover sprite or the row fill alone, no ring.

## Main menu (`menu-*.png`)
- Same picture, title and scrims as the loading screen (top scrim for the title, bottom scrim for the build line) plus a **left scrim** (ink-deep 0.62, 36 % of the width, the first 45 % of it held, then a smoothstep to 0) that gives the buttons a calm ground without touching a face.
- Title: unchanged from loading (stacked THE / SUBORNED, brass hairline, top right in the open sky). Build line: bottom right, as loading.
- Buttons: the kit's cream `btn_text` plates, **300 x 56 ref**, 14 ref gap, left margin 72, the stack's bottom 96 ref from the screen bottom. Label Jost Medium 19, tracking 0.14 em, upper case, ink. Order: **Continue** (only when a saved run exists; absent, not greyed), **New game**, **Options**, **Controls**, **Quit**. Default focus: Continue when present, else New game. (Brief 051 lists New game first; Continue first puts the focus on the first row. Say if Sky wants New game on top.)
- Debug items (playground, camera lab) stay out of the menu.
- Fits at 2880x1864 (scale 1.726; the 13 % side crop does not reach the buttons or the title; the Scout's left arm is about 90 ref px clear of the plates).

## Options and Controls panel (`options-*.png`, `controls-*.png`)
- **Panel:** `panel_dark_modal` nine-slice (slice 44 at 2x, drawn at half so the chrome rim is as the pause menu's), **960 x 780 ref**, centred, clamped to screen height minus 96. Padding 48 / 40 / 48 / 28. Title "OPTIONS" / "CONTROLS": Jost Medium 26, tracking 0.20 em, upper case, cream, 40 ref from the top. Close: `btn_close_normal`, 29 ref, top right (same as Back).
- **Tabs** (y 100, 34 ref tall, 10 gap, 26 side padding): `plate_light` + ink label for the active tab, `plate_dark` + cream label for the others, Jost Regular 15.5. Options: Display, Audio, Camera, Gameplay. Controls: Command, Camera, Fights, Third person. A panel-line rule under the tabs.
- **Body** from y 158 to the footer line (100 ref above the panel bottom): rows 44 ref (Controls rows 36), a group head 38 ref (Jost Regular 12.5, tracking 0.16 em, upper case, text-dim), a panel-line hairline between rows (none after the last). Label left (Jost Regular 17, cream, 10 ref in), control right-aligned 10 ref from the edge. If a page is taller than the body it scrolls (wheel, drag, keys); a 4 ref chrome bar at 50 % shows it. Every page here fits without scrolling; Gameplay with a non-hold click scheme shows four more rows (see the table) and may scroll.
- **Controls (widgets):**
  - **Slider:** 250 ref track (6 ref, ink-deep 70 %, 1 ref chrome rim at 35 %), turquoise-light fill, `disc_light_24` knob 22 ref, value right (Jost Regular-Tab 17, 56 ref zone).
  - **Switch:** 46 x 24 ref pill, ink-deep 65 % off / turquoise-light 40 % on, 1.5 ref chrome rim at 55 %, `disc_light_24` knob 20 ref (left off, right on), the word "On" / "Off" beside it (Off in text-dim). The current value is always a word, never colour alone.
  - **Choice:** 300 ref zone, the value centred, `chev_down` 16 ref turned a quarter each way (USS `rotate: 90deg` for the left arrow, `-90deg` for the right) 16 ref either side of the text; a click on either arrow steps, wrapping at the ends; a dim row (frame cap while V-sync is on) is text-dim at 45 %.
- **Footer:** line, then a small `plate_dark` "Reset page" (Controls: "Reset all"), 168 x 34, left, and the cream **Back** button 170 x 48 right. Reset applies to the current page only (confirm-free; the values show at once).

### Options rows (first cut; Sky adjusts after playing)
| Page | Rows (control, range, default) | Notes |
|---|---|---|
| Display | Window mode (Windowed / Borderless / Fullscreen; Borderless) · Resolution (the monitor's list; native) · Render scale (slider 50-100 %, 100) · Quality (Low / Balanced / High; Balanced = the Air's default) · V-sync (switch, on) · Frame cap (30 / 60 / 120 / Off; 60, dim and ignored while V-sync is on) · HUD scale (slider 80-150 %, 100) | HUD scale scales the in-play HUD only; the front end always scales by H / 1080. Resolution and window mode apply with a 10 s "keep?" pop only if they fail to hold (Latch's call). |
| Audio | Master, Music, Effects, Voices (sliders 0-100; 80 / 60 / 80 / 90) | sliders even while there is little audio |
| Camera | Commander view: Pan speed (slider, the `keyPanSpeed` 18 m/s, 6-36), Edge pan (switch, off), Orbit sensitivity (slider 0.25-2, 1.0), Invert orbit (off), Trackpad drag orbit (off) · Third person: Camera distance (Close / Default / Far, the camera lab's presets), Camera settle (switch, on) | |
| Gameplay | Orders: Click scheme, Orders while paused, Hints, Default game speed (x1 / x2 / x4 / x8; x1), Conversations · Fights: Agents near you (+ Map fire) · Playground: third-person experiments (below) | Hints = the `H` key's setting, kept as before |

## The pause menu rework and every toggle (`pause-*.png`)
- **Card:** `panel_dark_modal`, **360 ref wide**, centred, top at 12 % of the height, padding 34 / 28 / 34 / 26. Title "PAUSED" (Jost Medium 26, tracking 0.20 em). Seven cream buttons 292 x 46 ref, 10 gap, in this order: **Resume** (focused by default), **Options**, **Controls**, **Restart mission**, **Fight range** (label "The Handover" while in the range; the item exists only while the range exists), **Quit to menu**, **Quit**. The build line (`BuildStamp.Line`, Jost Regular-Tab 11.5, text-dim) under the last button, centred. The veil stays clear as built (a dim counts against the HUD budget; `pause.veil.alpha` is the one number if Sky wants it).
- Quit to menu = what Quit does today (saves the run at its last tick) and then returns to the main menu, where Continue is now present. Quit leaves the app. No confirm (as built); a confirm is a later call.
- The pause menu keeps its 14 buttons' values: nothing is deleted, each setting moves.

### Where each of today's toggles lands (the key is the existing `PlayerPrefs` key; values, defaults and handlers are unchanged)
| # | Pause-menu button today | Field (`PlayControls`) / PlayerPrefs key | Lands on | Control | Shown when |
|---|---|---|---|---|---|
| 1 | "Controls: click walks, hold acts / double click / as built" | `preset` / `Suborned.Controls.Preset2` (0 double click, 1 as built, 2 hold; default 2) | Gameplay > Orders, "Click scheme" | choice (3) | always |
| 2 | "Camera settle" | `leashSettle` / `Suborned.Controls.LeashSettle` | Camera > Third person, "Camera settle" | switch | always |
| 3 | "Conversations" | `conversations` / `Suborned.Controls.Conversations` (0 always, 1 first time, 2 never) | Gameplay > Orders, "Conversations" | choice (3) | always |
| 4 | "Action plate" | `actionPlate` / `Suborned.Controls.ActionPlate` | Gameplay > Playground, "Action plate" | switch | scheme is not "hold" |
| 5 | "Right-click" | `quickAction` / `Suborned.Controls.QuickAction` | Gameplay > Playground, "Right-click" | choice (Ring / Quick action) | scheme is not "hold" |
| 6 | "Left click acts" | `clickActs` / `Suborned.Controls.ClickActs` | Gameplay > Playground, "Third person: left click acts" | switch | scheme is not "hold" |
| 7 | "Aim" | `aimMode` / `Suborned.Controls.AimMode` (0 hold, 1 toggle) | Gameplay > Playground, "Third person: aim" | choice (Hold / Toggle) | always |
| 8 | "Agents near you" | `followLock` / `Suborned.Controls.FollowLock`; also sends the engine's logged follow order via `SetFollowLock` | Gameplay > Fights, "Agents near you" | choice (Fire with you / Lock your target) | always |
| 9 | "Map fire" | `commanderHoldFire` / `Suborned.Controls.CommanderHoldFire` | Gameplay > Fights, "Map fire" | choice (Until down / While held) | scheme is not "hold" |
| 10 | "Third person: right hold fires / right aims, left fires" | `tpRightFires` / `Suborned.Controls.TpRightFires` | Gameplay > Playground, "Third person: right button" | choice (Right hold fires / Right aims, left fires) | scheme is "hold" |
| 11 | "Orders while paused" | `planTogether` / `Suborned.Controls.PlanTogether` | Gameplay > Orders, "Orders while paused" | choice (Start together / Start as given) | scheme is "hold" |
| 12 | "Edge pan" | `edgeScroll` / `Suborned.Controls.EdgePan` | Camera > Commander view, "Edge pan" | switch | always |
| - | Resume / Restart mission / Fight range / Quit | host calls as today | pause menu | button | Fight range only while the range exists |
| new | (the `H` key's hints) | `Suborned.Play.Hints` (`HintsOn`) | Gameplay > Orders, "Hints" | switch | always |

A row hidden by the click scheme keeps its saved value and returns with it (the same hide rules as `MenuFrame` today, moved to the page). The Playground group is the brief 025/041 experiments' home until Sky decides them; when one is decided it moves up or is deleted by one line in the page's row list. Each change calls `SaveSettings()` (and `host.ControlsChanged()` for #1 and #10, as today).
**Storage:** one JSON file in `Application.persistentDataPath` (`settings.json`, `{ "version": 1, "display": {...}, "audio": {...}, "camera": {...}, "gameplay": {...}, "bindings": [...] }`). On first launch after this build, read each `PlayerPrefs` key above (and `Suborned.Play.Hints`) into the file once, so no saved value is lost, then stop reading the keys. Reset (per page) writes that page's defaults; the Controls page's reset restores `PlayControls.Defaults()`.

## Controls page: the binding table
- Rows come from `PlayControls.keys` (`Binding.action`, `keys[]`, `label`), the one table the game reads (GDD §10), so the page cannot drift. Row label = the action's friendly name (a small table in code); the **primary key** is `keys[0]` as a `plate_light` keycap (Jost Medium 15, ink, 30 ref tall, at least 52 wide, 14 side padding); **aliases** (`keys[1..]`, e.g. F1-F4 for the agent keys, arrows for pan) show as a dim `plate_dark` keycap (24 ref, text-dim) left of it and are rebound by clicking them. Two columns of up to 13 rows per tab (36 ref rows).
- **Groups (tabs):** Command (agents, select all, next agent, add to group, Pick action 1-4, Cancel, Walk or run, Pause, Faster, Slower, Next event, Briefing, History, Intel, Response list, Hints, Acknowledge, Detail, All strips, Play again) · Camera (pan x4, turn left / right, free turn, follow view) · Fights (draw or holster, cease fire, stance, force fire, target mode, step target) · Third person (Act). Debug keys (F11 HUD view, F12 debug, P camera lab, backquote if wanted) are hidden outside development builds. The mouse is fixed by the click scheme and is not on this page.
- **Rebind:** click a row (or Enter on it): the keycap becomes a dim plate with "Press a key" inside a 2 ref turquoise-light ring and the row takes the focus fill. The next key pressed binds. Esc cancels; Backspace on an alias clears it; a primary cannot be cleared. Esc and the mouse buttons cannot be bound.
- **Clash is shown and resolved, never silent:** a key already used by another action in the same context shows the clash strip (`panel_strip`, 52 ref, `mark_half` badge, "E already turns the view right", plate buttons **Swap** and **Cancel**); both keycaps carry a `mark_half` badge (24 ref, top-right rim seat) until it is resolved. Swap gives each action the other's key; Cancel keeps the old binding. See `controls-clash-*.png`.
- **What the table needs (the one change):** today a binding is a bare key list and the label carries the Shift ("⇧1"). Add to `Binding`: `mods` (none / Shift / Ctrl / Alt: Pick action 1 is Shift + 1, Select agent 1 is 1) and `context` (map / third person). The clash test compares key + mods **inside a context**, so `E` Turn right (map) and `E` Act (third person) are allowed, and `Digit1` Select agent 1 against `Shift+Digit1` Pick action 1 is not a clash; the page prints the chord ("Shift 1"). `Migrate()` fills the new fields for a saved table.
- Persist to `settings.json > bindings` (only the changed actions); Reset restores the defaults.

## Mission brief (`brief-*.png`)
- Picture, title hidden, a **right scrim** (ink-deep 0.88 at the edge, 48 % of the width, the first 56 % held) so the cell, on the left two thirds, stays clear and the text sits on the dark. The text column is **568 ref wide, 64 ref from the right edge**, from 84 ref down.
- Order, from `contracts/missions/handover/brief.json` (`title`, `place`, `time`, `paragraphs[]`): title (Jost Medium 42, tracking 0.10 em, upper case, cream) · a 1 px brass hairline the column's width · two head lines "PLACE" and "TIME" (Jost Regular 13.5, tracking 0.16 em, text-dim; value 15.5 cream at 76 ref in) · the paragraphs (Jost Regular 20 / 30 ref line, cream, 14 ref between paragraphs, left aligned, ragged right, no hyphenation) · the cream **Continue** button (220 x 52, focused with the ring), bottom right, 64 ref from the bottom, fixed.
- **Size for 120-200 words:** the placeholder has 144 words and ends at 720 ref (text bottom); 200 words ends near 850 ref, still about 110 ref above the button; past about 24 lines the column scrolls (never expected). Longer than that: shorten, do not shrink the type.
- **Picture focus for this screen:** `brief.imageFocus` [1.0, 0.5]. At 16:9 it changes nothing; at 16:10 (the Air) the 13 % side crop all comes off the left instead of both sides, so the Enforcer's sleeve and the Tech's head are clear of the column (centred, the sleeve ran under the first lines: seen and fixed in the mockup).
- Enter, Space or a click on Continue leaves for the existing tactical briefing. No Back (it is a story beat); Esc does nothing here. Fable's text may use only the three fields and plain paragraphs (no markup).

## Checks for the build (code first, test once)
- Captures at 1920x1080 and 2880x1864 of every screen above, compared with these mockups (`game:check` + the capture set; hud-budget does not apply to menus, but the in-play capture under the pause menu still must).
- A rebind round trip: bind, quit, relaunch, the key is kept; a clash shows the strip and does not overwrite; Reset restores `Defaults()`.
- Each of the 12 toggles: change on its page, quit, relaunch, same value; first launch after the migration keeps the old PlayerPrefs values.
- Swap `loading_bg.png` for picture A or D (`briefs/047/picks/`), move `title.rect`, and the menu, panels and brief draw with no code change (the left and right scrims are data too; `brief.imageFocus` and `scrims.*` are the knobs if the new picture's faces sit elsewhere).
- 2x crops of buttons, tabs, rows, keycaps, the clash strip and the pause card, for clipping, centring and padding (done for the mockups: none found).

## Open for Sky
- Title wording and treatment (stacked THE / SUBORNED; the GDD has no title treatment).
- Continue on top (as drawn) or New game first (brief 051's order).
- Pause menu: Quit and Restart without a confirm (as built).
- The Controls "Third person" tab holds one key (Act, E) because third person is parked; fold it into Camera if Sky prefers three tabs.
- The mission brief words are placeholders; Fable's `brief.json` replaces them.
