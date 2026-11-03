# Front end, direction A "light line": spec for Latch (brief 051, Glyph, 2026-10-23)

Sky picked A ("A seems ok") after "not into the highlighted design - let's make the menu button / options really nice". This replaces the cream chrome-capped plates and the cyan ring everywhere: main menu, Options, Controls, the mission brief's Continue and the pause menu. It supersedes the button, tab, row-focus, keycap, slider, switch and pause-card parts of `briefs/051/layout/SPEC.md`; everything else there (flow, pages, rows table, binding table, settings storage, checks) stands.

**One rule for the whole language:** text on the dark, and only three marks move: the **marker** (a small square turned 45 degrees), the **line** (turquoise-light, under the focused item) and the **tick** (a 3 ref bar at a row's left edge). Words get marker + line. Rows get tick + line. Nothing else changes when focus moves (no weight, size, position or plate), so nothing reflows.

## Pictures (all rendered from real parts: the real picture, the kit's fonts and chevron/badge sprites, `testbed/hud-style/frontend.*`)
| What | 1920x1080 | 2880x1864 (the Air) |
|---|---|---|
| Main menu (Continue present / absent) | `menu-1920x1080.png`, `menu-nosave-` | `menu-2880x1864.png`, `menu-nosave-` |
| Options: Display, Audio, Camera, Gameplay; opened from pause | `options-display-`, `-audio-`, `-camera-`, `-gameplay-`, `options-from-pause-` | same at 2880x1864 |
| Controls: browsing, a row listening, a clash | `controls-browse-`, `controls-listen-`, `controls-clash-` | same |
| Mission brief (Continue) | `brief-1920x1080.png` | `brief-2880x1864.png` |
| Pause menu | `pause-1920x1080.png` | `pause-2880x1864.png` |
| Boards: every state of words, of rows and controls; the timing frames | `board-words-`, `board-rowsboard-`, `board-timing-1920x1080.png` | (ref-px boards, 1x only) |
| 2x crop checks (clipping, centring, padding): menu stack, options head and rows, options foot, gameplay rows, clash strip, keys, pause card, Continue | `crop2x-*.png` | |
| Contrast report | `contrast-report.txt` | |
Live version with hover, arrow keys and press: open `http://localhost:5175/frontend.html#s=menu` (after `bun run hud-style`); `#s=options`, `#s=controls`, `#s=pause`, `#s=brief`, `#s=words`, `#s=rowsboard`, `#s=timing`. Regenerate everything: `sh testbed/hud-style/frontend-boards.sh` (about 2 minutes). Numbers: `frontend.a.json` here (written by `bun testbed/hud-style/frontend-export.ts` from `testbed/hud-style/frontend.tokens.js`; the pictures are drawn from the same table, so the spec and the mockups cannot drift).

## Colours (no colour without a source; all `kit.uss`)
cream `#f7f4ec` (all type) · ink `#17302f` (the sheet's tint) · ink-deep `#0b1615` (scrims, veil, knob rim) · text-dim `#b0cdc7` (group heads, build line, alias keycaps, "Off") · chrome `#cbc6c3` (hairline borders, rails, keycap edges) · panel-line `#d2e6e2` (rule and row hairlines at 16-22 %) · turquoise-light `#60e0ea` (the focus line, marker, tick, slider fill, switch mark, listening ring) · brass `#99784d` (the title's and the brief's one 1 px rule, unchanged). Cream alphas, four only: idle word **0.74** · idle row label **0.90** · focus **1.0** · disabled **0.40** (chevrons idle 0.50, disabled 0.25). Turquoise reads as "live": the focus line/tick/marker and a live value (slider fill, switch mark); it is never a fill behind text. Kind, role and harm keep no colour. No new sprite: the kit's `chev_down` (turned) and `mark_half` badge, nothing else.

## Type (Jost Regular is the HUD font per brief 046; `fontSpec.body` Jost-Regular-Tab 400, `strong` Medium 500)
All **Jost Regular**, tabular digits for values (`Jost-Regular-Tab`). **Medium** only where it is already used: the title lockup's SUBORNED and the brief's title and hook. Sizes (ref px) and tracking (em of size), upper case where marked: menu words **22 / 0.22 / upper** · pause words and Continue **18 / 0.20 / upper** · footer and clash actions **15.5 / 0.24 / upper** · page title (OPTIONS, CONTROLS) **32 / 0.30 / upper** (PAUSED 22) · tabs **15.5 / 0.20 / upper** · group heads **12.5 / 0.20 / upper / text-dim** · row label **17 / 0.02** · row value **17 tab** · keycap **14.5 tab**, alias keycap **12 tab text-dim** · build line **12.5 tab text-dim 85 %**. Text shadow on words that sit on the picture only (menu, brief): ink-deep 55 %, blur 6, y 1.

## Every state (`board-words`, `board-rowsboard`)
**Hover = focus.** A pointer entering an item moves the focus to it (marker slides, line draws), so one thing is marked at a time and the pointer, keys and pad all look the same (less to read). The pointer leaving the list leaves the focus where it was. Disabled items never take focus and are skipped by keys and pad.

| State | Words (menu, pause, footer, Continue, clash actions) | Rows (Options, Controls) | Tabs |
|---|---|---|---|
| Idle | cream 74 %, no marker, no line | label cream 90 %, value cream 100 %, 1 ref panel-line hairline at 16 % under the row, chevrons 50 % | cream 74 % |
| Hover / focus (pointer, keys, pad) | cream 100 %; marker on its left; 2 ref turquoise-light line under it | label 100 %; 3 ref turquoise-light tick at the row's left edge (7 ref inset top and bottom); the row's hairline becomes a 2 ref turquoise-light line across the full row; chevrons 100 %; slider knob grows 11 to 15 | pointer over (not active): cream 92 % and a 1 ref cream 40 % underline; keyboard focus on the strip: the active tab's line goes 3 ref and the marker sits at its left |
| Pressed | line turns cream, marker scales to 70 %, label stays 100 % (80 ms in, 120 ms out); the action runs on release (on press for the first Enter/Space of a held key) | choice: the pressed chevron turns turquoise-light and nudges 3 ref outward; slider: knob 17 while dragging, value follows; switch: box edge 2 ref turquoise-light, mark scales to 60 %; keycap: edge cream 2 ref | the tab goes active at once |
| Disabled | cream 40 %, no marker, no line | label and value 40 %, chevrons 25 %, no tick | not used |
| Active (tabs only) | | | cream 100 % with a 2 ref turquoise-light line under the label, 14 ref wider each side, sitting on the 1 ref panel-line rule (22 %) |

Keycaps (Controls): primary = 28 ref tall, min 48 wide, 12 side padding, 1.5 ref chrome edge at 85 %, ink-deep 45 % fill, radius 3, cream text; alias = 22 ref, chrome edge 35 %, no fill, text-dim 12. **Listening:** primary shows "Press a key" in a 2 ref turquoise-light edge that blinks to 45 % and back every 1.0 s (ink-deep 60 % fill); Esc cancels. **Clash:** both caps get a 2 ref cream edge and a `mark_half` badge (18 ref, top-right, -8 / -10 offset); the strip below the table is 56 ref tall, a 1 ref chrome 50 % rule on top, ink-deep 35 % fill, a 26 ref `mark_half` at left, "N already: Next event" (Jost Regular 16.5), and two word actions **Swap** and **Cancel** (Cancel focused by default, because it keeps the old binding).

## The marker, the line, the tick
- **Marker:** a 9 ref square turned 45 degrees, turquoise-light. In vertical lists with several words (main menu, pause menu) it is **one element that slides**; in single words (footer actions, Continue, clash actions) and the tab strip each word owns a marker that fades in. Centre: 30 ref left of the label's left edge, at the label's vertical centre.
- **Line (words):** 2 ref, turquoise-light, starts at the label's left edge, fixed length (menu 232, pause 266, Continue 192, footer actions = label width + 14), 37 ref below the item's top in the 48 ref menu item (33 in the 44 ref pause item, 29 in a 40 ref action). It **draws in from the left** and **retracts to the left** on the item that loses focus.
- **Line and tick (rows):** the line is the row's bottom edge, full row width, 2 ref; the tick is **one element that slides** between rows (top), 3 ref wide, row height minus 14.
- Scroll: a page taller than the body scrolls as before (4 ref chrome bar at 50 %); the tick follows the focused row and the focused row is kept in view.

## Timing (`board-timing`; Unity 6 USS has these easings)
| Move | Time | Easing |
|---|---|---|
| Marker slide (word lists) | **180 ms** | ease-out-cubic (`cubic-bezier(.215,.61,.355,1)`) |
| Line draw-in (new item) | **220 ms after a 40 ms delay** | ease-out-cubic, width 0 to full |
| Line retract (old item) | **100 ms** | ease-in-cubic, width back to 0 |
| Tick slide (rows) | **160 ms** | ease-out-cubic |
| Tab line slide | **180 ms** (left and width) | ease-out-cubic |
| Label opacity | 120 ms | ease-out |
| Press in / out | 80 / 120 ms | ease-out |
| Switch mark flip | 120 ms, scale 0 to 1 | ease-out-cubic |
| Page body cross-fade | 120 ms | as built |
| Screen fade | 300 ms | as built |
| Main menu in (after loading) | items fade 400 ms, 50 ms apart, rise 6 ref; **marker appears at the default item after 300 ms** (no slide, fades in 120 ms); its line then draws once | ease-out |
| Listening blink | 1.0 s loop | ease-in-out |
Transitions run on unscaled time (the pause menu is on a frozen game). Reduced motion, if it is added later: all of the above become a 60 ms opacity change.

## Screens
- **Main menu:** same picture, title and bottom scrim as loading (unchanged). Left scrim is now **ink-deep 66 % held over the first 50 % of 40 % of the width**, then smoothstep to 0 (was 62 % over 36 %), because words sit straight on it. Items: left 72, last item's bottom 100 from the screen bottom, 300 x **48** hit area each, 12 ref gap, label at 52 ref in (marker centre at 22). Order: **New game, Continue (only with a saved run; absent, not disabled), Options, Controls, Quit** (the order of brief 051 and of the concept; the earlier "Continue first" is dropped). **Default focus: Continue when present, else New game.**
- **Options and Controls sheet:** no sprite and no corner close (Esc, Back and B close it). **960 x 780 ref**, centred, 1 ref chrome border at 34 %, radius 2. **Smoked glass = the same picture, blurred, inside the sheet** (UI Toolkit has no backdrop blur): `loading_bg` baked once at 1/12 size, box-blurred about 18 ref, used as the sheet's background and positioned so it lines up with the full-screen picture (`background-position = -sheet.left, -sheet.top`; size = the screen's). Over it, in order: ink-deep at the veil's 52 % (what the picture gets round the sheet, so the glass is not brighter than its surroundings), then ink at **72 %**. Fallback if the bake is cut: a flat ink 90 % fill (same text contrast, no silhouettes). From the pause menu the veil is 40 % and the bake comes from the frozen frame (or the flat fallback). Screens that show the picture also get the loading screen's bottom scrim (ink-deep 68 % over 28 % of the height) so the build line stays readable at 16:10.
  Inside, in ref px from the sheet's top-left: padding 48 left and right · title at y 42 · tabs at y 116, 36 tall, 22 gap, 14 side padding, the rule at y 152 · body from y 172 · rows 44 (Controls 34), group heads 40 · footer rule 92 above the bottom, footer words centred in the strip below it (Reset page left, Back right). Row label 14 ref in, control right-aligned 14 ref in, control zone 300.
- **Choice:** chevron (`chev_down` 14 ref, turned, 28 ref hit box) at each end of a 286 ref zone, value centred between (190 ref wide at least, tab digits). **Slider:** 220 ref rail (2 ref, chrome 38 %), turquoise-light fill, knob = the marker's square (11 ref, 15 focused, 17 dragging, cream with a 1.5 ref ink-deep rim), value right (54 ref, right-aligned). **Switch:** a 20 ref box (1.5 ref chrome edge at 60 %, radius 2), the marker's square (9 ref, turquoise-light) inside when on and nothing when off, then the word On or Off (Off in text-dim); the value is always a word as well as the mark.
- **Controls:** two columns of 13 rows (column gap 44), keycaps right-aligned in each row; focus tick and line span the column. Clash strip sits between the table and the footer rule (table ends 614, strip 622-678).
- **Mission brief:** unchanged except Continue: a word, **18 ref**, focused by default (marker + line drawn), bottom-right, 64 ref from the right and bottom, 232 ref wide item. Enter/Space or a click takes it. One scrim change measured at the Air: the right scrim's **hold is now 656 ref** (column 568 + margin 64 + 24) instead of 0.705 of the scrim's width; at 2880x1864 the old hold left the dossier heads at 1.7:1.
- **Pause menu:** the kit modal card is replaced by the same smoked sheet, **360 ref wide** at 12 % of the height, but **flat ink 90 %** (it stands over a live frozen game; a bake there is optional) with the 1 ref chrome border. Padding 32 / 30 / 32 / 24. "PAUSED" 22 ref at the left (aligned with the marker's left edge), then seven words at **44 ref** pitch (marker slides): Resume (focused), Options, Controls, Restart mission, Fight range (The Handover in the range), then **a 14 ref extra gap**, Quit to menu, Quit. Build line (11.5 ref, text-dim) at the bottom left. The veil stays clear as built.

## Contrast (`contrast-report.txt`; WCAG ratio of the effective text colour over the brightest 5 % of the pixels under each item, 1920x1080 and 2880x1864)
All text at 17 ref and under is **>= 5.0:1** at the worst spot; menu words idle (cream 74 %) **5.0 to 6.2:1**, focused 8.6 to 11.0; sheet text 5.0 to 14.8; tabs idle 6.0; pause words idle 6.1; the brief 6.4+. Exceptions: the title lockup (unchanged, 64 ref, 3.9:1, large text) and disabled items (40 %, 3.2 to 3.4, exempt). Contrast is why the left scrim went to 66 % (at 50 % the idle words fell to 3.6:1 over the café umbrellas) and the sheet tint to 72 %.

## Port notes for Latch (read-only survey; nothing edited)
- **Data:** `unity/Assets/Suborned/Resources/Loading/frontend.json` (`suborned.frontend/1`, classes `FrontEnd.FrontData` in `FrontEnd.cs` lines 71-94). Add a `lightline` block (or bump to `/2`) from `frontend.a.json`: `alpha`, `marker`, `line`, `menu` (itemH 48, lineW...), `sheet` (glassTint, glassBlurRef), `timing`, and the changed scrims. Existing `button.focus` (ring) and `rowFocus` (fill + tick) go away; `type.button / menuButton / tab` become the word/action/tab entries above; `type.panelTitle` becomes 32 / 0.30.
- **One item type replaces every plate button:** `FrontEnd.Button(...)` (`FrontEnd.cs` ~550) is used by the main menu, the footer (Reset page / Back, `FrontEnd.Panel.cs` 40-42), the clash strip (Swap / Cancel, ~400) and the brief's Continue. Make it a `WordItem` (Label + a line element + a marker for the single-word case), parent-owned shared marker for the two vertical lists (main menu in `FrontEnd.cs`, pause in `HudToolkit.Menu.cs` `BuildMenu`).
- **Marker:** a `VisualElement` 9 x 9, `backgroundColor` live, `style.rotate = 45deg`, `transitionProperty: top`, `transitionDuration: 0.18s`, `transitionTimingFunction: ease-out-cubic`. **Line:** `height 2`, `width` transition 0.22 s ease-out-cubic with `transitionDelay 0.04s` on focus, 0.10 s ease-in-cubic on blur (set the transition on the focused/unfocused USS class). **Label:** `opacity` 0.74 / 1.0 with a 0.12 s transition; do not change `-unity-font-style`, size or letter-spacing on focus.
- **Focus:** `Focusable` + `FocusInEvent` / `FocusOutEvent` set the class; `PointerEnterEvent` calls `Focus()` (hover = focus); NavigationMove for keys and pad as built; disabled items `focusable = false`.
- **Rows** (`FrontEnd.Panel.cs`: `RowEl` 120, `Slider` 137, `Switch` 154, `Choice` 167, `Cap` 314, `PanelLayout` 46, `BuildClashStrip` 393): row focus = the shared tick element (`top` transition 0.16 s) plus the row's own bottom line; drop the `rowFocus` fill and radius. Slider knob and switch mark are the rotated square; the old `disc_light_24` knob and the pill switch go. Choice chevrons keep `chev_down`. Keycaps lose `plate_light` / `plate_dark` for a border and a faint fill (USS `border-width`, `border-color`, `background-color`). `PaintListening` becomes the blinking edge.
- **Sheet:** drop `panel_dark_modal` and the close button for a bordered element with the three background layers; the blur bake is one small `Texture2D` made when the front end loads the picture (downsample to about 213 x 120, a few box-blur passes on the CPU, bilinear). Pause card: flat ink 90 %, no bake.
- **Pause:** `HudToolkit.Menu.cs` builds the card with `s-btn_text_*` plates and a ring on Resume (`menuResumeRing`); both go, and `MenuBtnW/H` are replaced by the 44 ref pitch. The playground and camera-lab debug items still stay out.
- **Checks to run on the build:** capture the set again at 1920x1080 and 2880x1864 and compare with `briefs/051/lightline/*`; check marker, line and tick positions at both sizes (they scale by H / 1080); text is never clipped at 2880x1864; focus moves with keys, pad and pointer; a disabled row is skipped; `game:check` passes.

## Open for Sky
- Hover = focus (above) versus a separate soft hover; one marked thing at a time is the proposal.
- The glass blur: with the bake the sheet shows soft silhouettes of the cell behind (as in the concept); the flat fallback is darker and plainer.
- Words stay Regular weight when focused (no bold); the line and marker carry the focus. Say if the focused word should read bolder (it would need a Medium twin and a reserved width).
- No close X on the sheet (Esc / B / Back).
