# 044 · Overlay style: put the control lab's overlays in the game's own look

**State:** A picked and revised 2026-10-22 (no kind colours; §6). Owner: Glyph (interface-artist). Next owner: Sky looks at `board-revised-a.png`; then Latch ports §6 (it replaces the colour and kind lines of §3 and §4), Glyph reviews the 2x crops of the build.

## Why
Sky, on build 0.2.56: "those overlays look awful - they're from the control lab - not Suborned style at all!" Latch ported the playground's flat web styling with the behaviour: the verb pill (bright blue / green / pink rounded bar, white 2 px border), the done chip (bright blue box), the window marker and its counter ("Guard away" green pill + "174" tag), the cancel tag, the closing pops, the walk-in plate. The rest of the HUD (top-right clock bar, bottom-right feed card, portrait column, goal line, plates) is dark teal in chrome frames with cream type and a thin brass line, which is what GDD §15 and the v10 "signage" pick say.

## Objective
The same pieces, the same words and the same behaviour, drawn in the kit's language. Only the look changes.

## Done-check
- 3 directions drawn at 1080p over the real 0.2.56 shots (control-lab overlays removed from the shots first), covering the pill (offer, hold, let go, firing), the done chip, the window marker with its countdown, the cancel tag, a closing pop, the walk-in pill (done).
- Every component cropped at 2x and checked for clipping, overlap, centring and padding: an automated DOM check on every picture (no text or icon outside its plate, no overlay on another, none off screen: 27 of 27 ok) plus a look at every 2x crop (done).
- One recommended and port notes naming the files (done, §3, §4).
- `contracts/style/art-bible.agent.md` "HUD and overlays" filled for the agent cards (done, Aster's request).
- Sky picks (open).

## Evidence
- `bun briefs/044/src/gen.ts` renders all 27 pictures with headless Chrome and runs the layout check on each: `QA a-… ok` for all 27 (A, B, C × 9 scenes). Fonts are the kit's Jost and Barlow; icons, cameos and frames are the kit's own files (`unity/Assets/Suborned/UI/Resources/Kit`), so what you see is what the sprites and slices give in the game.
- Backgrounds: the eight built shots in `briefs/043/build/` with the old overlays inpainted out (`briefs/044/src/clean.py` → `briefs/044/src/clean/`). The pavement behind a new overlay is smudged where an overlay is smaller than the old one; that is the retouch, not the design.
- Nothing in Unity, sim or tools was edited.

## 1. The rule they were measured against
The built HUD: deep-teal body, chrome rim, cream Jost Bold, a turquoise accent for the live thing, brass only as a hairline, round enamel discs for icons, kit sprites for every frame (`pill_status`, `panel_dark`, `panel_strip`, `plate_dark`), speech in small cream bubbles. Memory notes (Syndicate-simple, icons on characters, less reading, no new colour for signals) and the style contract's "Never ship a prototype's look". So: the overlays are dark so they read against the cream street and never look like speech; the kind of an act rides on a small enamel disc, not on the whole plate; shape cues (hazard stripes, dashed edge, square corners) carry harm and warning, never colour alone.

## 2. Three directions (pictures in `briefs/044/`)
Each has nine 1080p pictures `a-01-pill-states.png` … `a-09-tp-fire.png` (b-, c- likewise). The comparison boards `board-NN-*.png` put the three side by side at 2x for each piece.

| | A · Chrome plate | B · Station sign | C · Orb and label |
|---|---|---|---|
| Idea | The overlays are the HUD's own pieces, hung in the world: the pill is the clock bar's frame, the chip is the Intel card, the marker and pop are the number plate. | The v10 / k5 transit signage: flat teal enamel, cream keyline, a line-colour tab, a hanging chrome stem. | Icon first: an enamel orb on the target with a ring, a quiet dark label beside it, no frames. |
| Frames | Kit sprites: `pill_status`, `panel_dark`, `plate_dark`, `panel_strip` (harm). Chrome rim, screws. | New flat signs (keyline, rivets on the chip), chamfered corners for harm. | Orb with chrome ring; label is dark glass, hairline edge. |
| Kind | 26 px enamel disc; 4 px bar on the chip | coloured tab at the end of the sign | the orb's enamel |
| Hold / let go | turquoise sweep in the window / turquoise ring and glow, "Let go" lit | seafoam sweep / turquoise keyline | ring fills round the orb / ring complete |
| Window | `plate_dark` with eye + seconds, draining underline | London-style roundel with the seconds on its bar | orb with a draining ring and the seconds |
| Cancel tag | ink plate, dashed edge, hazard stripe, ✕ | ink sign, dashed keyline | dashed capsule, ✕ orb |
| New art | none (all sprites exist) | frames, stem | orb and ring (ring_track / ring_fill exist) |
| Reads over a cream street | best (chrome edge, dark) | good | good, but the label has no edge on busy ground |
| Matches clock bar, feed card, Intel | exactly | by cousin | no |

## Recommendation: A · Chrome plate
- It is the game's look already. Every frame is a sprite the HUD uses today, so it ports with no new art and cannot drift from the clock bar or the feed card.
- It is the best on the busy street: dark body and chrome rim separate it from cream stone and from speech bubbles (cream), so a pill is never mistaken for a person talking.
- It keeps the lab's good ideas (one line, face on the right, the hold sweep, "Let go", the struck way) and drops the lab's colours (#4aa8ff, #4cc35f, #ff4d7d, white 2 px borders).
- B is the fallback if Sky wants flatter and lighter (needs new frame art). C is the right idea for the window counter (a draining ring) and could be borrowed later; as the whole language it breaks the "frames" of the HUD and the label is weak on busy ground.
- Cost: the pill grows from 34 to 38 px and the chip is slightly taller than the lab's; the chrome needs 2x crops to check (done).

## 3. A in detail (what Latch builds)
Sizes are px at the 1920 × 1080 reference.
- **Pill** (`HudToolkit.ActMark.cs` BuildPill, `HudToolkit.Hold.cs` HoldPillFrame): body = `s-pill_status`, 38 px tall, slice scale 0.43 (caps 20 px), content inset 16 px left, 15 right. Inside: 26 px kind disc (enamel fill, 1.5 px #d2e6e2 ring, 1 px ink-deep outer ring, 16 px icon), text Jost Bold 15.5 cream, the head word ("Hold", "Let go", "Right hold") #b0cdc7 and the "·" at 70 %, faces 24 px with a 2 px role-colour ring and a 1 px ink-deep ring, overlapping 7 px. Tail: two stacked triangles, chrome #cbc6c3 18 × 11 and body #145a56 12 × 7.5, tip on the target. The words stay what `PillLine` makes.
- **Hold:** a turquoise-light fill at 40 % alpha, clipped to the window (left/right 15 px, top/bottom 6 px, radius 13), width = progress. **Let go:** head word #60e0ea, 2 px #60e0ea ring plus a soft glow, scale 1.05 (as built). The hold's dimming as the cursor pulls away stays.
- **Fire and harm** (`state == "fire"`, `Charge.Fire`, `Kind == harm`): frame `s-panel_strip` at slice scale 0.3 (7 px corners) darkened to 62 % brightness (`unityBackgroundImageTintColor`), a 7 × 22 px stripe block (cream/ink 45°, kit `stripes_warn` tile) at the left, coral disc #e4533d with the fire icon. Square corners and stripes are the cue; no pink.
- **"Watched spot"** (`vpillSeen`): a 22 px ink-deep plate, 1.5 px #d2e6e2 edge at 66 %, a 7 px stripe block, the `sawyou` icon, Jost Bold 12.5 cream. No coral.
- **Chip** (`HudToolkit.Done.cs` NewChip): `s-panel_dark` at scale 0.5, padding 13 / 17 / 12 / 21; a 4 px bar in the kind enamel at the left; 32 px cameo with the outcome badge (17 px disc: ✓ green #2c9a68, ½ grey #6d7b79, ✕ #8d3a30, cream icon 10 px; `mark_ok / half / no` sprites also fit); lines 22 px high, icon 16 + Jost Bold 15 cream; then a 1 px brass #99784d line, an arrow (`chev_up` rotated 90°, #60e0ea) and the verb pills: `s-plate_dark` at scale 0.37, 22 px tall, icon 12 + Jost Bold 12.5; a shut way at 65 % with a cream strike. Rises 10 px and fades as built.
- **Window marker:** `s-plate_dark` at scale 0.43, 26 px tall, `eye_shut` 15 px + seconds in Jost Bold 14.5 (tabular), a 2.5 px underline of #60e0ea draining with the time; the Tech's face (18 px) replaces the number for a held Loop. The last 10 s: underline #f0a020 and the 0.55 to 1 blink as built.
- **Cancel tag:** 26 px, ink-deep at 94 %, radius 6, a dashed 1.5 px cream edge at 60 % (a 1.5 px dashed border: UI Toolkit has none, so paint it with `Painter2D` as the dashed rings already are), a 9 px stripe block at the left, `close_x` 12 px, Jost Bold 13.5 cream.
- **Pop:** solid `s-plate_dark` 26 px (as built now), the icon 14 px; the cancelled pop and "Guard back" keep the same plate.
- Roles: scout #8fd3c8, fixer #f2b48a, tech #9db7ff, enforcer #e6dfcc stay (they are the cell's colours). Kind enamel replaces the lab kinds: look #2f6fd6, talk #4fc58c, tech #1cb9c6, take #eee9da (ink icon), harm #e4533d.
- Signals are untouched: cones red, guard amber, rivals violet are in the world, not here; the HUD's only amber is the window's last 10 s and the only red-orange is the harm disc and the refused badge.

## 4. Port notes for Latch (read only; I changed nothing)
What draws each overlay today (all under `unity/Assets/Suborned/Runtime/Play/`) and what changes.

| Overlay | Where | Today | Change |
|---|---|---|---|
| Kind colours | `HudToolkit.ActMark.cs:27` `KindOf` (public static, used by Hold.cs:76,82, Done.cs:75, ActMark.cs:88) | returns the lab's #4aa8ff, #4cc35f, #1fd1b5, #ff4d7d, #f1ead8 | return the enamel table in §3 (and a second value for the icon ink). Rename to enamel; callers use it for the disc only, no longer for a plate fill. |
| Lab constants | `HudToolkit.Hold.cs:20-21` | `Coral`, `LookBlue`, `HarmPink` | delete `HarmPink`, `Coral` (Watched spot) after the changes below; `LookBlue` is the Watch-reach ring (Hold.cs:164): make it #60e0ea (turquoise light, the live accent) so it matches the new pill. |
| The pill body | `ActMark.cs:67-76` `BuildPill`, `ActMark.cs:89`; `Hold.cs:94` | `E(marks, null …)` flat `backgroundColor = kind colour`, `Border(…, 2f, white)`, `Round(vpill, harm ? 6 : 17)`, 34 px | `E(marks, "s-pill_status")` (or `s-panel_strip` + tint for harm), `style.unitySliceScale = 0.43f` (or add a kit class), height 38, no border, no `backgroundColor`; padding 16 / 15. Add the 26 px disc as the first child (`vpillIcon` moves into it). |
| The pill's fill | `Hold.cs:29` `vpillFill`, `Hold.cs:93` | white 55 % bar 34 px high across the whole pill | turquoise-light 40 %, inset 15 / 6 px, radius 13, clipped (`overflow:hidden` on a wrapper inside the frame). |
| The tail | `ActMark.cs:73` `vpillTail` Paint | one white triangle 14 × 9 | two triangles (chrome 18 × 11, body 12 × 7.5); the position maths (`q.y - 10 - 9`) shifts by 2 px. |
| Head word | `Hold.cs:64,83`, `Text(vpillText, text)` | one Label | split the first "Hold ·" off into its own Label (#b0cdc7), the rest cream; "Let go" in #60e0ea when `state == "ready"`. The words are unchanged. |
| Faces | `Hold.cs:31,42` `Faces`, border 2.5 px role colour | | 24 px, 2 px role ring plus a 1 px ink-deep ring (an outer wrapper), overlap −7. |
| Watched spot | `Hold.cs:32` `vpillSeen` | coral label, white border | ink-deep plate, 1.5 px #d2e6e2 edge, 7 px stripe block, `sawyou` icon. |
| Done chip | `HudToolkit.Done.cs:19-57` `NewChip`, `:73-90` (colour and tone), `:31` `ChipDark` | blue/dark flat box, white 2 px border, radius 14 | `E(marks, "s-panel_dark")` at scale 0.5, no `Round`/`Border`/`backgroundColor`; add the 4 px kind bar; for tone `soft`/`no` keep the bar but swap the face badge (the badge replaces the bordered `Mark`). Unlock pills (`Done.cs:51`): `s-plate_dark` at scale 0.37 instead of `Ink` with radius 10; brass hairline = `borderTopColor` #99784d at 100 % instead of `PillInk` 35 %. Lines' colour is always cream (no `PillInk` branch). |
| Window marker | `Done.cs:110-124` (the element is made inline, `ChipDark` + cream 1.6 px border, radius 12) | | `s-plate_dark` at scale 0.43, 26 px; add the underline element (width = left/total; `WindowMarks` has `Left`; total is the window's length, which the host knows: add `Total` to `WindowMark` if it is not there) and amber in the last 10 s. |
| Cancel tag | `Done.cs:140-164` `CancelTagFrame` | `ChipDark`, radius 6, 1.6 px cream 80 % | ink-deep 94 %, dashed edge via `Painter2D` (the kit's dashed rings already use it: see `Circle`, Hold.cs:114), 9 px stripe block, same text and position logic. |
| Pops | `HudToolkit.Less.cs:564-594` `Pops` (the pool is `popEls`) | already `s-plate_dark`, `verb-name` 13 px | keep; set the type to Jost Bold 13.5, icon 14 px; the cancelled pop's `close_x` stays. Nothing else. |
| The lit pip | `Done.cs:167` `Sparkle`, `Hold.cs:104-106` | white 4-point star, ink edge | cream star with the ink edge is fine on the dark plate; move it to the plate's top-right chrome corner. |
| Other lab pieces in the same family | `Hold.cs:173-177` (paused-plan ghost pills: cream with ink border), `Hold.cs:188-196` (attention clock: coral / amber / red filled pills), `Hold.cs:203` (portrait-drag faces), `HudToolkit.Done.cs` face rings | cream and bright flat fills | not in Sky's list; do them in the same pass: ghost pill = `s-plate_dark` with the face; attention clock = `s-plate_dark` plus a 3 px coloured underline (cream → amber → red), keep the ring arc. I can draw these on request. |

How the kit is wired: `kit.uss` is generated by `Suborned/UI/Import Glyph's Kit` from `briefs/011/ui/kit/layout.json` + `sprites.json`; do not edit it. Either set `style.unitySliceScale` in code for the scale variants (simplest, the pieces are built in code) or ask Glyph to add scaled classes (`s-pill_order`, `s-panel_strip_fight`, `s-plate_dark_26`) to the kit source. Fonts and colour variables (`--cream`, `--text-dim`, `--turquoise-light`, `--brass`, `--ink-deep`) are already in `kit.uss`; use `t-chip-value` (Jost Bold) and set the size in code as now.

Checks for Latch's build: Glyph's 2x crop review of every state in `PillState`, `ChipWords`, `WindowWords`, `CancelTagState` (`HudToolkit` already exposes these for the proof); screenshots `done-*` and `hold-*` re-shot.

## 5. Open
- Sky picks A, B or C (or "A with C's countdown ring").
- The window's total length (for the underline) comes from the host: Latch confirms `WindowMark` carries it.
- The "done" plate under the portrait (`HudToolkit` status plate, kit `s-plate_dark` with a ✓) is already in the kit's look and is not changed.

## Sky's pick, 2026-10-22
"A - but what's with the blue outline? Seem to be introducing loads of new colors which were never there before." Direction A, without the new colours: no kind bar, no kind-coloured discs or rings; every colour comes from the as-built HUD kit (or the style contract palette where the kit already uses it), each with its source. The kind of act reads by its icon, as signals read by shape before colour.

## 6. Revision after Sky's pick (A without the new colours): audit and port notes for Latch
Pictures: `briefs/044/a-01…a-09-*.png` (revised A, nine scenes, QA 9 of 9 ok, 2x crops in `briefs/044/src/crop-a-*.png`), `briefs/044/board-revised-a.png` (old A left, revised A right: chip, help pill, hold, let go, offer and Watched spot, fire, window marker, cancel tag), old A kept as `a0-*.png`. Render: `bun briefs/044/src/gen.ts a`.

**Dropped:** the chip's 4 px kind bar; the kind enamels (#2f6fd6, #4fc58c, #1cb9c6, #eee9da, #e4533d) as discs, fills or rings; coral fire disc; the darkened `panel_strip`; the role-colour rings on faces (#8fd3c8, #f2b48a, #9db7ff, #e6dfcc: they exist only in the lab's `RoleColours`, HudToolkit.Hold.cs:20, and the roster does not use them); the hand-made badge greens and reds (#2c9a68, #6d7b79, #8d3a30); #145a56 and #14262a for the tail; the turquoise-to-turquoise-light gradient; seafoam #72b5a7.

**Colour audit of revised A (every colour has a source):**

| Colour | Where in A | Source |
|---|---|---|
| deep-teal body, chrome rim, screws | pill, chip, strip, plates | sprites `pill_status`, `panel_dark`, `panel_strip`, `plate_dark` (the clock bar, feed card, Intel card use the same) |
| ink #17302f, ink-deep #0b1615 | Watched spot and cancel plates, icon on light disc, hairline shadows | `kit.uss` `--ink`, `--ink-deep` |
| cream #f7f4ec | all type, icons, dashed edge | `kit.uss` `--cream` (clock bar, feed card) |
| dim #b0cdc7 | "Hold" head word | `kit.uss` `--text-dim` (clock bar labels) |
| chrome #cbc6c3 | tail, face rings | sprite rim; contract `palette.chrome` |
| panel line #d2e6e2 | Watched spot edge | `kit.uss` `--panel-line` |
| turquoise-light #60e0ea | hold sweep (40 %), "Let go" word, 2 px ring and glow, unlock arrow, window underline | `kit.uss` `--turquoise-light`; layout.json: activity ring fill and busy ring; HUD: clock bar fast-forward, queued icon, roster selected ring |
| brass #99784d | 1 px chip hairline | `kit.uss` `--brass` (thin line only) |
| amber #f0a020 | window underline and blink, last 10 s | contract `signal_colours.guard`; HUD already uses it for "running out" (HudToolkit.Less.cs:1005 `soon` pie, HudToolkit.cs:1032 warn border) |
| tail body #0c3434 | inner triangle | the body colour of `pill_status` (sampled) |
| kind disc | every act | sprite `markers/disc_dark_24`, cream icon; harm/fire `markers/disc_light_24`, ink icon (ivory #eee9da in `kit.uss`) |
| hazard stripes | Watched spot, cancel tag, fire pill ends | sprite `overlays/stripes_warn` (cream #ece9e0, ink #2c3434), the kit's warning header; `kit.uss` `--stripe-warn` |
| outcome badge | chip face | sprites `badges/mark_ok`, `mark_half`, `mark_no` (the red in `mark_no` is the kit's own, as in roster and Intel) |
| face ring | pill, chip, marker | chrome 2 px plus ink-deep 1 px; no role colour |

No warm accent is needed: the fire pill reads by shape (chamfered `panel_strip` corners, a stripe block at each end, the light disc against the dark ones). If Sky wants one anyway, the only warm colours the HUD shows are amber (guard signal) and the red in `mark_no` / cone badges; I would not spend either on a pill.

**Port changes (replace §3 and §4 where they differ):**
- `HudToolkit.ActMark.cs` `KindOf` (also Hold.cs:76,82, Done.cs:75, ActMark.cs:88): stop returning colours. Return only the icon name and whether the disc is light (`Kind == harm` or `state == "fire"`). The disc is the sprite `s-disc_dark_24` / `s-disc_light_24` at 26 px, icon 15 px (cream, ink on light); no `backgroundColor`, no ring code.
- Delete `HarmPink`, `Coral`, `LookBlue` (Hold.cs:20-21); the Watch-reach ring (Hold.cs:164) becomes `TurqLight`.
- `RoleColours` is used for face rings (Hold.cs:42, Done.cs:76,119, Hold.cs:205): replace the `Border(…, RoleColours[k])` there by the chrome ring (2 px `Chrome` #cbc6c3, 1 px ink-deep outer wrapper). Hold.cs:161,168 draw the move paths in role colours on the ground: not touched here, and a colour source question for Sky (they are not in the kit).
- Fire and harm: `s-panel_strip` at slice scale 0.3, **no tint**, a 10 × 24 `stripes_warn` block (tile repeat, 22 × 15 px) inside each end, the light disc. Square, chamfered, striped: no pink, no coral, no darkening.
- Watched spot (`vpillSeen`) and cancel tag: the stripe block is the `stripes_warn` tile, not drawn cream/ink bars; the plates are ink-deep as before.
- Chip (`Done.cs` NewChip): no kind bar, padding 13 / 17 / 12 / 17; the face badge is the kit sprite `badges/mark_ok|mark_half|mark_no` at 19 px (right -7, bottom -6) instead of a drawn disc with a tone colour; `tone soft/no` just picks the sprite.
- Pill fill: `TurqLight` at 40 % flat (no gradient). Tail body triangle `#0c3434`.
- Everything else in §3 and §4 stands (sizes, slice scales, window marker, pop, ghost pills). The attention clock (Hold.cs:188-196) must follow the same rule: `plate_dark` with a cream underline and the amber one only for "running out"; no coral or red fills.

Check for Latch's build: Glyph's 2x crop review of every state, and a grep that no colour in the HUD code is outside the audit table.
- Sky, 2026-10-22, on revised A: agreed (no kind colours); "make the icons larger like avatar on right": the act icon's disc matches the face cameo's size and chrome ring, the icon bright cream and large (the grey-on-dark phone was too faint). Build revised A with this; also restyle the move paths and paused-plan ghosts (no lab role colours) and remove the in-play sentence under the pill ("Debt. The guard visits her quietly. She startles.") per the no-sentences rule.

## Review of 0.2.65 (Glyph, 2026-10-22)

Gate before Aster shows Sky. Shots: `briefs/044/build/` (25). Boards: `briefs/044/review-0.2.65-pills.png` (pill, chip, fire pill against the lab) and `briefs/044/review-0.2.65-overlays.png` (speech stack, legend, verb plate, toggle, plan label, cancel tag). The shots are 1920 x 1080, so 1x: every crop above is an upscale. The 2x check needs shots taken at 2160 (CrispScale then runs at 2). Colour check: grep of `unity/Assets/Suborned/Runtime/Play` finds none of the dropped kind colours (#2f6fd6, #4fc58c, #e4533d, #4aa8ff, #4cc35f, #ff4d7d), no `HarmPink`, `Coral`, `LookBlue`. Two sources to confirm, see 6 and 9 below.

**Verdict: not ready for Sky. Revised A reads as built, but the act disc is not the size of the cameo, the discs and cameos sit on the end caps, and the third-person legend, the stacked speech and a few plates are off-kit.**

### Verdict per shot
| Shot | Verdict |
|---|---|
| done-help-pip | **fix**: disc 19 px vs cameo 29 px (1, 2); sparkle clipped at the cap corner (3); speech bubble covers the "Inez" name tag (5). Chip itself ok: badge sprite, brass hairline, verb plates, no bar. |
| done-listen-chip | ok chip; capture caught the pill mid-fade (ghost behind the bubble); re-shoot. |
| done-walkin | **fix**: head word "to Inez" is cream, the other pills use dim "Hold" (4); bubble sits 4 px under the pill and covers the name tag (5); disc small (1). |
| done-call-window | ok: window marker, closed-eye icon, underline in turquoise-light, the badge on the cameo. Speech bubble 8 px under the plate, acceptable. |
| done-guard-back | ok: pop (plate_dark, icon + one line), "Vale is out." pop. The pop body is a little see-through over the dithered fade (7). |
| done-cancel-warn | **fix**: no "Cancels Watch · 4 s lost" tag in the frame. Not visible, so unproven. Re-shoot with the tag up. |
| done-cancel-pop | ok: "Watch cancelled · 5 s lost" plate, close icon. Speaker disc "S" in the bubble: see 6. |
| done-tp-fire, hold-tp-fire | **fix**: key legend strip (8); "Drawing" pill: screws over the stripe blocks (2); disc small (1). |
| hold-tp-keeps-walk | **fix**: key legend strip (8); "Let go T" tag fine, 10 px from the scope. |
| hold-fire-firing | **fix**: stripe blocks under the corner screws (2); light disc 21 px vs cameo 29 px (1). Shape reads: chamfer, stripes both ends, light disc. |
| hold-fire-charge, hold-fire-ready | **fix**: the two frames are identical, so the sweep and the "Let go" ready state (turquoise-light word, 2 px ring, glow, 1.05) are not shown; the pill is also half covered by the range lab panel. Re-shoot each state. Words are a long sentence, "Right hold · Fire at an officer · +2/hit Trail · Alert ≥2": the price line is fine, but it is the longest pill; keep it one line. |
| hold-focus | **fix**: stripe block against the screw, two overlapped cameos are ok, disc small. |
| hold-act-offer | **fix**: 1, 3 (cap overlap), sweep absent (offer, so fine). |
| hold-act-charge, hold-act-walking | **fix**: 1, 3, 4; no turquoise sweep in the charge frame: the shot does not prove the hold fill. Ground ring "2" under Inez ok. |
| hold-plan-paused | **fix**: dashed white route is hard to read on the lit paving (no dark under-line), the label plate is see-through (7). The status plates ("queued") ok. |
| hold-squad-formation | ok: numbered rings in cream and turquoise-light, running pips with arcs. Pips float on the figure's body line at the right (the footprint disc covers an elbow): move them above the head or to the feet. Status plates ok. |
| hold-squad-leave | ok; bubble "Watching." sits on the pip, 6 px: nudge up. |
| hold-free-toggle | **fix**: "Return fire only": flat grey square, cream text with no plate, about 1.3:1 on the pale ground (9). |
| hold-watch-reach | **fix**: verb plate keycap numbers ("1.1", "1.2", "1.3") clipped by their badges, blank 4th slot (10). Pill ok except 1, 3. |
| hold-portrait-lock | ok pill; bubble "Aiming. Nobody move." is covered by the range lab's hit labels (lab only). |
| hold-range-panel, hold-under-fire | ok for the HUD; the lab panel and hit labels are not HUD. Roster plates: "under fire" ok. |

### Fixes for Latch, most visible first
1. **Act disc is not the cameo's size** (Sky asked for this). `ActDiscEl` (`HudToolkit.Plate.cs:76`) draws `disc_dark_24` at `disc.size` 26, but the sprite's visible disc is 56 of 64 px, so it shows about 19 to 21 px beside a cameo that is 24 + 4 + 2 = 30 box, about 29 visible (build, every pill). Set the disc box to `(FaceSize + 6) / 0.875`, 34 px, and the icon to 20 px (cream, ink on the light disc), or set `disc.size` in `revisedA.tokens.json` to 34 and `iconSize` 20. Same for `ActMark.cs` `ActDisc` (the over-the-person badge) if it should match its own cameo. Check at 2x: the disc and the cameo have the same outer diameter, the icon fills most of the disc.
2. **Stripe blocks and screws collide** on the fire and Drawing pills (hold-fire-firing, hold-focus, done-tp-fire). The `panel_strip` screws draw over the stripe block at each end. Move each stripe block 6 px inboard of the screw (or 8 px more end padding) so the screw sits on bare rim, as in `briefs/044/board-revised-a.png`.
3. **Disc and cameo sit on the end caps**, no padding (hold-act-offer, done-help-pip): the disc is cut by the left cap rim, the cameo ring touches the right rim, and the sparkle is clipped at the corner. The lab pill keeps both inside the body with 6 px of padding and the sweep running behind them. Add 8 px padding at both ends of the pill body (`vpill`, `Hold.cs`), and move the sparkle to the cameo's top-right.
4. **Head word colour**: "to Inez · Watch · 14 s" shows "to Inez" cream; "Hold ·" and "Right hold ·" are dim `#b0cdc7`. Make the walking head word dim too (same `LookHead`), the words unchanged.
5. **Speech over the name tag** (done-help-pip, done-walkin): the bubble covers "Inez", and the leader diamond lands on its edge. Stack: pill, then bubble, then name tag, with 6 px between each; or hide the name tag while a bubble speaks (the bubble has the speaker's dot). Bubbles also sit 4 px under the pill (done-walkin): 8 px.
6. **Colour sources to confirm**: the speech bubble's speaker dots are cyan (`S`, `F`) and amber (Inez, guard). Cyan reads as the dropped `#1cb9c6`: if it comes from `PlayHud.cs:435` `NewPill`, replace it with turquoise-light `#60e0ea` or cream; amber is the guard signal and fine for the guard, but a speaker dot should be one source (cream disc with ink initial) so a role colour does not return. Report the exact source in your reply.
7. **Plates that are see-through**: the paused-plan label ("Loop the entrance camera · 97 s") and the "Guard back" pop show the world through the body. Use the opaque `plate_dark` body (no alpha) like the window marker and "Vale is out." Add a 1 px ink-deep line under the dashed route so it reads on cream paving.
8. **Third-person key legend** (`HudToolkit.Fire.cs:63-64`, shots done-tp-fire, hold-tp-*, hold-tp-keeps-walk): seven caps with words along the bottom, keycap text clipped ("Shift", "hold E"), a plate peeking behind. Rule: a key legend means the controls are wrong, and no sentences in play. Remove it; the verbs are the pill, the act prompt and the plate. If a hint must remain, show only the one key the next step needs, as a cap on the pill, and fit the cap to its text (`KeyCapW` minWidth 22, padding 4 each side).
9. **"Return fire only" toggle** (hold-free-toggle): put the label on a `plate_dark` plate and draw the box with the kit's plate/disc sprite (a cream tick when on); grey square and bare cream text are off-kit and unreadable on pale ground.
10. **Verb plate numbers** (hold-watch-reach): the "1.1/1.2/1.3" keycaps are clipped by their badges; widen the badge to the text, drop the "1." prefix if it carries nothing, and show the empty 4th slot at the same value as the others or leave it out.
11. **Re-shoot** at 2160 for: fire charge and ready (sweep, "Let go", ring), the act charge (sweep), the cancel tag, the listen chip without the pill fading, the red `mark_no`/`mark_half` chip badges, and a "Watched spot" plate. None of these is in the 25 shots, so they are unchecked.
12. Low: the formation pips overlap an elbow; "Let go T" is 10 px from the scope; speech "Watching." sits on a pip.

### Verified as matching revised A
Chip frame (`panel_dark`, no kind bar, badge sprite, brass hairline, › and verb plates, cream type); window marker (plate, eye icon, count, turquoise-light underline; amber not shown in the shots); fire pill shape (`panel_strip`, stripes, light disc, ink icon); cancel pop and "Vale is out." pops; roster status plates; the move ring and paused route in cream, with no role colour; no kind bar and no coral anywhere; sentence under the pill removed (the old "Debt. The guard visits her quietly." is gone).

Next owner: Latch (items 1 to 11); Glyph re-reviews the re-shot at 2160.

## Review of 0.2.94 (Glyph, 2026-10-23)

Gate before Aster shows Sky. Shots: `out/scratch/latch/proof2160-0294/` (3840 x 2160, so these are true 2x), `briefs/049/build/2160-0294/`, `briefs/046/build/2160-0294/`, `briefs/048/build/0294/`, `briefs/055/build/`. I read about 35 frames at full size and cropped every HUD piece at native pixels. Boards (all git-ignored pictures): `briefs/044/review-0.2.94-pills.png`, `review-0.2.94-roster.png`, `review-0.2.94-rings.png`, `review-0.2.94-world.png`. Crops: `out/scratch/glyph/`.

**Verdict: close, not ready. The HUD kit is now right where it matters (act disc matches the cameo, no key legends, no kind colours, pill states, chips, roster dividers, ring occlusion). Three things would make Sky stop: a sentence-and-gap plate in the fight (`Firing at Vale's car`), the guard's amber ring hidden under the world marker (the one ring you cannot see in its own proof shot), and the building fade (a grey slab and a stipple smear over the Records Office door in four frames). The rest is polish.**

### Verdict per area
| Area | Verdict |
|---|---|
| 12 review fixes from 0.2.65 | **ok** for 1, 2, 3, 4, 8, 9: act disc 64 px vs cameo 62 px at 2x, ring and icon sit on the same circle; stripe blocks clear of the screws (act ready, act charge, fire ready); 8 px padding at both ends; head word "Hold" dim; the key legend strip is gone from every third-person frame; no "Return fire only" grey square. **fix** 7: "Guard back" pop is still see-through (`ring-loop-names`: the world shows through the body, grey text on ghost; may be mid-fade, but the fade is slow enough to catch in a proof frame). The 0.2.65 re-shoots exist now (charge, ready, cancel, listen), good. |
| Loop camera names | **ok**: radial wedges with the chosen wedge in turquoise-light, one label per way (`plate_dark`, name, clock, fingerprint +1), Jost Regular. Labels sit 16 px off their wedge, readable at 2x. Low: the wheel has no chrome rim (flat dark wedges), reads a notch plainer than the pills. |
| No key legends | **ok** in every frame (tp-options, hold-tp-fire, tp-street). **fix**: two bottom-centre sentences remain in play: "Loop the camera at the cabinet." (`done-window-soon`, `done-turn-chip`) and "Watch the café terrace." (all third-person frames, `tp-worst` overlaps the cell's ring). These are brief 049 row 15 hints; the noun-in-colour dot is a nice touch but it is a sentence. |
| Squad-fire plates | **fix**: `Firing at Vale's car · down in 45 s` (`commander-firing`) has a 120 px blank where the gun disc should be (text is indented, icon missing) and is a sentence. Brief 049 row 4: gun disc + `45 s`. The plain `Drawing` and `Let go · Fire · +2/hit Trail · Alert ≥2` pills are fine. |
| Roster | **ok**: icon row under each portrait (weapon disc, optional eye or "!" disc, pip counter), steel divider with no screws between slots, column B bare, one weapon icon, Jost Regular on plates and "Squad". Slot alignment: icon row is 24 to 27 px under every portrait, portrait centres identical (x = 114). **fix**: (a) in slot 4 the gun disc touches the corner screw of the rail (`roster-firing`, bottom: screw 8 px from the disc); move the last icon row up 6 px or drop the two bottom screws. (b) The under-fire burst in slot 1 and 2 is pale yellow (about #f6e3a0): no kit source (cream is #f7f4ec, amber #f0a020), and one ray overlaps the weapon disc ring. Make it cream (same as the white tick ring in slot 2). (c) The side plates still carry words ("aiming", "firing", "drawing", "refused", "not yet") on a 340 px plate with the icon far left and the word far right: brief 049 row 9 says icons, and an empty plate that wide reads as a bar. Shrink the plate to its content; words only where there is a number. |
| Target rings | **ok with two fixes.** Red aim ring (0.12 m, solid) reads at once on grey; cream hover ring with ticks is clear and sits behind the legs (back half hidden, front over the ground); violet rival rings read, solid vs dashed is clear, legs cover the ring correctly. **fix**: (1) the amber guard ring is not visible in `ring-plain-amber` and `ring-plain-out`: the eye marker (a 70 px dark disc) sits exactly on the guard's feet and covers it, and no patrol is in frame (the green figure is the driver). Re-shoot with the patrol, or lift the marker so the ring shows; as it stands amber is unproven. (2) The tick marks on the red rings are faint, 1.5 px, 50 % alpha grey on grey (`rings` board, top left): they carry the "in an exchange" meaning; make them cream at full alpha with the ink edge the hover ring has. |
| Latch's "rings range: hover FAILED" | **Checked: the frame is the cause, not the ring.** `ring-hover-passerby`: no weapon drawn (no drawn-gun pill, no ring on anyone), the two officers wear red "aim" rings, the bystander has none. Rule (049 round 1): a civilian wears a ring under the cursor only while a selected agent has a weapon drawn, so none is right here. In the next frame (`ring-ours-bystander`, drawing) the bystander gets the cream hover ring with ticks, so hover works. The probe hovers before it draws: reorder it (draw, then hover, then check). One oddity: the officers wear red aim rings at 0:00 in the range, before anyone has fired; fine for the range, make sure the Handover never shows it before a shot. |
| Fonts | **ok** Jost Regular on pills, chips, plates, tooltips and goal line; Barlow Medium on speech only. Bold stays on the tabular counters and the feed card titles ("Help", "Watch"). **fix** (briefing): the stat row (8:30, $100,000, 0/3, Unknown) is Jost Semibold and "The Handover" Medium; make the row Regular. |
| Colours with a source | **ok** except the yellow burst above. Reds: `mark_no` badge (kit sprite), cone red on aim rings and under-fire wedge. Amber: guard marker, "reported" underline. Violet: rivals. Turquoise-light: hold sweep, ready word and ring. The orange route line in `commander-firing` is the patrol's amber; fine. |
| Chips and plates (2x crops) | **ok**: `done-refused-chip` (face, red `mark_no` badge, brass hairline, struck "Ask" plate), `done-turn-chip` (half badge), feed card (screws, no clipping), window marker, watched-spot plate (stripes, eye). No clipping, centring good. Low: chip "Not yet" has 90 px of empty body right of the word; the window marker's underline floats 220 px beyond its plate as a bare cream line (`done-window-soon`), looks stray. |
| Fire pill detail | **fix, low**: the `stripes_warn` tile shows a seam (a broken diagonal in the middle of each block, `pill-fire-left`); the ready glow bands in steps at the corners; the head word and "Right" on the sweep are about 3:1 (dim #b0cdc7 over 40 % turquoise-light), fine for the offer, soft in the hold. |
| Third person | **ok**: roster plus pill, no legend, red wedge arrows, rings. **fix**: the `Let go` plate is detached at the top left (y 16), the text sits against its top rim (2 px above, 6 below), and it is smaller than every other plate: centre the label, anchor it to the pill's tail or the roster. |
| Briefing | **fix**: the Day focus ring is a square 3 px turquoise box on a lozenge button; Night is grey on cream (about 2:1); the build string touches the Begin button and the screw. Cream Begin/Day/Night plates are off the dark kit (they match the lab's old button, not the pill kit). Low priority, one-time screen. |
| Edge pointers and markers | **fix, low**: the clipped ghost disc at the top edge (`done-window-soon`, x 860 at 1080p) is a camera marker cut by the screen edge with a grey bar through it. The four face discs top left (`ring-plain-amber`) use a plain turquoise ring, not the chrome cameo ring. |

### World look (not mine to fix; for Aster, Glint, Vela)
Compared with `briefs/028/palm/ref-flyby-boulevard-old-city.png` and Sky's two stills.
- **Reads right**: the old palms (dark tapering trunks, long drooping fronds, gold-green light) are now the closest thing to the reference and carry the street (`palms-east`, `ring-plain-rival`); pearl kerb and pale paving read as honed limestone; the third-person haze (`tp-street` against `tp-street-atmosphere-off`) adds real depth: distant blocks soften and warm, the sky keeps its blue at the top; play-zoom grade is a warm cream with cool blue shadows, the umbrellas and mint enamel pop.
- **Does not**: (1) the road is a powder blue-grey flat with almost no marking and no traffic; the reference has dark asphalt, white lane lines, a median and cars, so ours reads as water or concrete and the street has no direction (`palms-east`, `tp-street`). (2) The flyby haze (`tp-street-long`) is an ochre dust: it reads as a sandstorm, not the reference's clear gold-and-blue light; skyline ghosts are good, the sky loses its blue. Pull the haze towards pale peach and keep the top 30 % of the sky blue. (3) Palms are in rows at close range but sparse and scattered at distance (`tp-street-long`: a flat green lawn with 20 small palms, an empty pale plaza); the reference has tight rows on both kerbs. (4) Close hedges (`tp-street`, `tp-worst`) are faceted green blobs with pink hexagon flowers; fine at play zoom, rough in third person. (5) The building fade: a flat grey slab across the top of `ring-plain-amber`, `done-watched-spot`, `done-window-soon` and a heavy stipple screen over the Records Office door (`amber-guard` crop); this is the single most visible flaw in the Handover frames.

### Ranked fix list for Latch
1. **Building fade**: grey flat slab and heavy dither over the door and top of frame (Handover, four frames). Fade the whole mass with a softer ordered pattern, never an opaque grey cap. (With Glint if it is the grade.)
2. **Guard ring hidden** under the eye marker (`ring-plain-amber`, `ring-plain-out`): draw the ring above the marker's disc or offset the marker; re-shoot with the patrol in frame.
3. **Status plate**: `Firing at Vale's car · down in 45 s`: put the gun disc back (blank 120 px) and replace the words with disc + `45 s` (049 row 4).
4. **Bottom hint sentences** ("Loop the camera at the cabinet.", "Watch the café terrace.") out of play or under Alt (049 row 15; Fable).
5. **Roster**: bottom gun disc vs screw (slot 4); under-fire burst colour to cream and off the weapon ring; side plates shrink to content, icons not words (049 row 9).
6. **Ring ticks** on red rings: cream, full alpha, ink edge.
7. **Probe order** for the hover check: draw first, then hover.
8. **Third-person `Let go` plate**: centre text, anchor it, match the kit size.
9. **`Guard back` pop** opaque; `plate_dark` body.
10. **Fire pill**: stripe tile seam, glow banding.
11. **Briefing**: Day focus ring, Night contrast, stat row Regular, build string padding.
12. Low: window-marker leader line, clipped camera marker at the top edge, cameo ring on the top-left face discs, wheel chrome rim.
World, for their owners: road finish (dark asphalt or a clear lane language), flyby haze hue, palm density at distance.

### Best shots for Aster to show Sky
1. `briefs/048/build/0294/play-opening.jpg` (play zoom, roster, clock, minimap, warm grade; the strongest overall frame)
2. `out/scratch/latch/proof2160-0294/hold-act-ready.png` (Let go pill with act disc and cameo, ground ring "2", thin reach arcs)
3. `out/scratch/latch/proof2160-0294/ring-loop-names.png` (radial with named ways, feed card, `Vale is out.` pop; the fade slab upper right is the flaw: crop it or wait for fix 1)
4. `out/scratch/latch/proof2160-0294/done-refused-chip.png` (the chip with the red badge and the struck "Ask")
5. `briefs/048/build/0294/tp-street.jpg` (third person, haze and old palms; no legend)
6. `out/scratch/latch/proof2160-0294/ring-plain-rival.png` (palms, violet rings, pill; do not use the range frames: the Fight Range panel is lab furniture)

Not to show: `ring-plain-amber`, `ring-plain-out` (grey slab, hidden ring), `commander-firing` (plate gap), any `FightRange` frame (lab panel).

Next owner: Latch (items 1 to 12); Glint and Vela for the world notes; Glyph re-reviews the re-shot at 2160.

## Review of 0.2.97 (Glyph, 2026-10-24)

Gate before Aster shows Sky. Shots: `briefs/051/build/0297/` (1920x1080 and 2880x1864), `briefs/049/build/2160-0297/` (3840x2160, true 2x), `briefs/055/build/0297/`, `briefs/048/build/0297/`. I read about 30 frames at full size and cropped every HUD piece at native pixels. Boards (git-ignored pictures): `briefs/044/review-0.2.97-frontend.png`, `review-0.2.97-pills.png`, `review-0.2.97-open.png`, `review-0.2.97-world.png`. Crops: `out/scratch/glyph/r97/`.

**Verdict: the front end is ready to show; the HUD fixes landed for 9 of the 12 items; the building fade is the one thing that would still make Sky stop (a flat grey slab in `ring-loop-names`, a dither screen over the Records Office door and paving in six more frames). The long-tier flyby is now right.**

### Verdict per area
| Area | Verdict |
|---|---|
| Front end vs spec and mockups | **ok.** Menu: Jost Regular caps at 22 px, no plates, one marker (turquoise-light diamond) and a 232 px line under the focused word, default focus Continue, order New game / Continue / Options / Controls / Quit. Options: no sprite, no close X, 1 px chrome border, tabs with the 2 px line, group heads in dim, the tick plus full-width line on the focused row, chevrons, slider with the cream diamond knob, switch box with the turquoise diamond and a word. Controls: primary and alias keycaps, the listening key ("Press a key", turquoise edge), the clash strip (`mark_half`, "N already: Next event", SWAP, a marker and line on CANCEL). Brief: Continue as a word with marker and line. Pause: smoked flat card, 7 words, 44 px pitch, marker and line on Resume. At 2880x1864 nothing clips and the text keeps its padding (2x crops in the frontend board). The 18 proof checks in `frontend.json` are all ok (hover = focus, disabled skipped, rebind round trip, clash, pause focus). |
| Front end, small differences from the mockups | **low.** (1) The menu picture is darker than the mockup: the sky top and the whole right side lose the warm light (mock 1920 vs build 1920). The left scrim is right; the extra darkening is a top and overall veil. Contrast is fine, but it is the one place the build looks plainer than the mock; ask Latch to check the veil alpha (spec: only the left scrim and the bottom scrim). (2) The sheet is the flat ink fallback, with no blurred silhouettes of the picture (the spec allows it; Sky's open question "bake or flat" is still open). (3) The mission brief's column is larger than the mock: the first line wraps ("...to Security.") and "paper." is a one-word orphan; narrow the type by about 8 %, or widen the column, to keep the mock's one-line heads. (4) Brief bullets are small turquoise dots, the mock has the diamond (use the marker). (5) The disabled "Frame cap" row keeps its chevrons at about 50 %, the spec says 25 %. (6) Gameplay page shows a "PLAYGROUND" group ("Third person: aim", "Third person: right button"): lab terms in the player's options; hide it or rename. |
| Long-tier flyby (`tp-street-long`) | **ok.** Blue sky overhead (the top third stays a clean pale blue), warm only at the horizon band, distant towers are soft peach ghosts, no ochre dust over the whole frame. This fixes the 0.2.94 "sandstorm". Left: the lowest haze band is still a sand colour (a touch desert); pull it a step towards peach. The foreground plaza is overexposed white. World notes below. |
| Third person street (`tp-street`, `road-enamel-boulevard-third`) | **ok.** Palms in rows on both kerbs, haze softening the far blocks, roster and clock bar clean, no legend. The road is still a powder blue-grey with faint markings (see world). |
| Records Office front at yaw 210 (`office-front-yaw210`, `levels-guard-landing`) | **ok.** The tower fades with a fine dither and the door, the guard, steps and the eye marker are readable. Best fade frame. |

### The 12 items of "Review of 0.2.94"
| # | Item | State |
|---|---|---|
| 1 | Building fade (grey slab, dither over the door) | **not fixed.** The slab is back, flat grey with a hard edge, in `ring-loop-names` (upper right, 480 x 380 px at 2x). In `done-window-soon`, `done-watched-spot`, `done-cancel-warn`, `ring-plain-amber`, `done-refused-chip` a vertical column of dither and a dither screen cover the door and about 400 px of paving; the guards in `done-watched-spot` are seen through it. Yaw 210 is good, yaw 30 is not. |
| 2 | Guard amber ring hidden | **fixed.** `ring-plain-amber`: the amber dashed ring shows at the guard's feet and the eye marker sits below it. `ring-plain-out` is the same. |
| 3 | `Firing at Vale's car` plate | **fixed.** Gun disc, bullet icon, `19 s`: no sentence, no gap. |
| 4 | Bottom hint sentences | **fixed.** Gone from `done-window-soon`, `done-turn-chip` and the third-person frames. The goal line `Take Vale alive.` stays (allowed). |
| 5 | Roster | **fixed.** Under-fire burst is cream with ink ticks; side plates are one gun icon, no words; no screw collision (bottom screws gone). Only the `Squad` plate and the 1 s / 19 s window bars carry text, as they should. |
| 6 | Ring ticks on red rings | **not shown.** `ring-ours-bystander` and the range frames show the red aim rings with small ticks, cream now, but they are 1 px at 2x on the grey floor; I could not prove full alpha and ink edge. Low. |
| 7 | Hover probe order | **not fixed, behaviour fine.** The 2160 pass still logs `rings range: hover FAILED [drawn; officers aim]; ours seen [..., bystander-1:Civilian:In:ours:hover]`. The same line in the game check says `ringHover ok: ...bystander-1:Civilian:In:hover`, so the hover ring works; the 2160 probe reads the rings before the drawing 1 s finishes (the frame shows the `Drawing` bar). It is the frame timing, not the ring. Make the probe wait for the drawn pill, then hover. |
| 8 | Third-person `Let go` plate | **fixed.** Anchored under the roster at its size and centred (`hold-tp-fire`). |
| 9 | `Guard back` pop opaque | **fixed.** `plate_dark` body, eye icon, no show-through (`ring-loop-names`, `done-guard-back`). |
| 10 | Fire pill seam and glow | **fixed enough.** Stripe blocks sit clear of the screws, no visible seam at 2x; the ready glow is smooth (`hold-fire-ready`). The sweep ends mid-word at a hard edge, expected. |
| 11 | Briefing card | **partly.** Stat row is Regular now and the build string no longer touches Begin. The in-game card is still the old kit: cream Begin / Day / Night plates (off the dark kit) and a chamfered turquoise box on Day. Same word-with-marker-and-line treatment as the new front end would fix it in one step (it is the last place with cream plates). |
| 12 | Low items | **mixed.** Wheel has a rim line now (ok). Window-marker leader line gone (ok). The top-left face discs still use a plain bright turquoise ring and a stray white corner square (`ring-plain-amber`, 4 discs). The docked camera callout at the top edge is still a ghost disc (see below). |

### Latch's three open notes
- **Docked entrance-camera callout (`done-window-soon`, x 1700, y 0): not fixed.** A milky translucent disc with a red dashed ring, three red dots and a small face badge, with the building's edge showing through it, cut by the top of the screen and a "^" tip. It is not a kit piece. Make it the opaque `disc_dark_24` with the camera icon, the same disc as the eye marker, and keep the face badge as the chrome cameo.
- **"Not yet" chip: fixed.** The chip now ends right after the word (`done-turn-chip`: face with half badge, target icon, "Not yet"); no empty body.
- **"rings range: hover FAILED":** checked above (item 7): the probe, not the game.

### Also seen (not mine; for Aster, Vela, Glint)
- **Roof boxes**: big flat orange and yellow boxes on the rooftops (every play frame, left side, and the `tp-street-long` roofs) read as untextured placeholders. They are the loudest colour in the frame, louder than any HUD piece.
- **Road finish** still powder blue-grey, no traffic.
- **Long tier**: the park lawn is a flat green rectangle with 25 scattered palms; the plaza is blown out white.
- **`game:check` exits 1** with `rulebook.values.mood-scared-fear.unit 'scale' is unknown` on stderr (`out/scratch/latch/game-check-0297.log`): a rulebook key the checker rejects. Keel's.

### Ranked fix list for Latch
1. **Building fade at yaw 30**: the flat grey slab (`ring-loop-names`) and the dither column and screen over the door and paving. Use the yaw 210 pattern (it reads); never fade the paving or the figures; no opaque cap.
2. **Docked camera callout**: opaque `disc_dark_24` and icon.
3. **Menu picture veil**: match the mockup's brightness (left and bottom scrims only).
4. **Brief page**: fit the lead line on one line, no orphan, marker diamond for bullets.
5. **Briefing card in game**: replace the cream plates with word + marker + line, and the Day/Night ring with the tab line.
6. **Options**: hide the PLAYGROUND group; disabled chevrons at 25 %.
7. **Top-left face discs**: chrome cameo ring, no white corner.
8. **2160 hover probe**: wait for the drawn pill, then hover.
9. World, for others: roof boxes, road finish, long-tier lawn and plaza exposure, haze band a step towards peach.

### Best shots for Aster to show Sky
1. `briefs/051/build/0297/2880x1864/menu.png` (the light-line menu at the Air's size; marker, line, no plates)
2. `briefs/051/build/0297/1920x1080/options-gameplay.png` (tabs, tick and line row, group heads, chevrons) and `briefs/051/build/0297/1920x1080/controls-clash.png` (keycaps, the clash strip)
3. `briefs/049/build/2160-0297/hold-fire-ready.png` (Let go pill) or `hold-act-ready.png`, whichever has the cleaner background; crop to the pill
4. `briefs/049/build/2160-0297/commander-firing.png` (gun disc plate, roster burst, bubbles; the building column near the top is the flaw, crop it out)
5. `briefs/048/build/0297/tp-street-long.jpg` (blue overhead, warm at distance: the answer to the 0.2.94 haze note)
6. `briefs/055/build/0297/office-front-yaw210.png` (the fade done right, guard and door readable)

Not to show: `ring-loop-names` (grey slab), `done-window-soon` (ghost disc, dither column), `done-watched-spot`, any `FightRange` frame (lab panel), `briefing` (cream plates).

Next owner: Latch (items 1 to 8); Glint and Vela for the world notes; Keel for the `game:check` rulebook key; Glyph re-reviews the next re-shot at 2160 and the 2880 front end.

## Review of 0.2.99 (Glyph, 2026-10-24)

Re-review of the 8 items from "Review of 0.2.97". Shots: `briefs/051/build/0299/` (1920x1080, 2880x1864), `briefs/049/build/2160-0299/` (true 2x), `briefs/055/build/0299/`, `briefs/048/build/0299/`, `briefs/028/road/game-0299/`. Boards (git-ignored): `briefs/044/review-0.2.99-fade.png` (0.2.97 against 0.2.99), `-frontend.png`, `-hud.png` (native 2160 crops), `-road.png` (testbed board against the game). Crops: `out/scratch/glyph/r99/`.

**Verdict: 7 of 8 fixed; the camera callout waits for the next build (Latch knows). The front end is ready to show. The fade is much better (paving is clean) but one grey roof slab and a hard vertical column remain. The road is now close to the testbed in the top-down views; the third-person boulevard still has painted-road bars.**

### The 8 items
| # | Item | State |
|---|---|---|
| 1 | Fade at yaw 30 | **mostly fixed.** The dots take the street's colour: the blue-tinted dither screen over paving and door is gone in `done-watched-spot`, `done-window-soon`, `ring-loop-names`, `levels-crew-steps` (compare the left and right columns of the fade board). Still seen: (a) the grey slab at the top right of `ring-loop-names`, `done-watched-spot` and `done-window-soon` (about 480 x 380 px at 2x): flat, one grey, no shading, with a ragged dither edge. Latch says it is the office wing's bare 15.68 m roof in shadow, not the fade; I accept that, but it is still the most wrong-looking patch in those frames (a roof needs its cream and a shade step, or a cap detail). Vela / Datum / Glint. (b) The fade is still a straight vertical column with hard edges: the café wall in `hold-act-ready` and the office in `levels-guard-landing` end in a vertical cut with the ghost wall beside it. Feather the column's two edges over about 100 px at 2x. Low-medium. |
| 2 | Camera callout | **not yet (known).** `done-window-soon` and `done-guard-back` show the opaque disc is not drawn yet: the disc is milky and the building edge shows through (they were shot mid fade-in). The red eye pictogram and the face badge are right. Re-check on the next build; the test is one frame after the fade-in finishes, edge not visible through the disc. |
| 3 | Menu veil | **fixed.** Sky band brightness 66 (0.2.97) to 92 (0.2.99), the right side warm again; only the left and bottom scrims remain. The top-left corner is still the darkest part of the picture; fine. |
| 4 | Brief layout | **fixed.** Lead line on one line ("...carries it to Security."), no orphan, bullets are the marker diamond, Continue as word with marker and line. |
| 5 | Briefing card | **fixed.** Day / Night are words with the tab line under Day; Begin is word, diamond, line. No cream plates left in the game. The card is the dark kit plate with screws and the dim build string. |
| 6 | Options | **fixed.** No PLAYGROUND group in Gameplay (Orders / Fights only); Frame cap's chevrons and value are about 25 %. |
| 7 | Top-edge face discs | **fixed.** Chrome ring, ink face, no white corner (`done-window-soon`). The roster cameos' selected ring (all four lit when Squad is on) is a saturated turquoise (about #22c8d8) rather than `--turquoise-light` #60e0ea. Low: check it against the token. |
| 8 | Hover probe | **fixed.** `ringHover ok: drawn; waited 0.00 s; ... bystander-1:Civilian:In:hover` in `proof.json`; no FAILED line. |

### The road against the testbed board (`briefs/028/road/boards/play-210-day.png`)
- **Top-down (yaw 210 and 30): close.** Powder blue-grey enamel with a fine grain, cream lozenges, brass beads on the crossing (no brass on the lozenges), a pearl kerb line, mint pearls on the crossing strip. This is the testbed's road, no longer asphalt.
- **Still different:** (1) the paving is pinker and mottled than the testbed's buff-gold pavement, and the testbed's gold kerb trim line is missing (the brass hairline is much thinner); (2) in shadow (`ring-loop-names`, `done-turn-chip`) the road falls to a deep navy slab, much darker than the testbed's shadowed road (Glint); (3) the grain is a little visible at 2x; the testbed's seam lines are cleaner.
- **Third person boulevard (`road-enamel-boulevard-third`): not the testbed.** Wide solid white bars for lane and edge lines, a pale pink median band and a dark slab beside it read as a present-day road. The brief says markings need not be paint: use the cream lozenge row and fine seams (as in the top-down), and take the white bars out. The light enamel is right.

### Also seen (not Latch's)
- Roof boxes: orange and yellow flat boxes on rooftops, left in every play frame and the long view (placeholders; the loudest colour in the frame).
- `tp-street-long`: the horizon band is a sand yellow again (more desert than peach; Glint's reflection cut is inert this build); the park lawn is a flat green rectangle; the plaza is blown out white.
- FightRange frames show the lab panel (`hold-fire-ready`): do not use them in a show.

### Ranked list
1. Latch: camera callout opaque (item 2); feather the fade column edges.
2. Vela / Datum / Glint: the grey roof slab; the shadowed road colour; the roof boxes; the horizon band.
3. Vela: boulevard markings (lozenges and seams, no white bars); pavement tint and the gold kerb trim line.
4. Latch: roster ring colour against `--turquoise-light`.

### Best shots for Aster to show Sky
1. `briefs/051/build/0299/2880x1864/menu.png` (light-line menu at the Air's size; marker, line, brighter sky)
2. `briefs/051/build/0299/1920x1080/brief.png` (the brief page now fits: one-line lead, diamond bullets)
3. `briefs/051/build/0299/1920x1080/options-gameplay.png` (clean tabs, tick and line row, no lab group) with `controls-clash.png`
4. `briefs/049/build/2160-0299/hold-act-ready.png` (the Let go pill over the café; the fade column is visible, crop the pill and the left of the frame if Sky dislikes it)
5. `briefs/049/build/2160-0299/commander-firing.png` (gun plate, roster burst, bubbles, a clean road; the building column near the top is the flaw, crop it)
6. `briefs/055/build/0299/office-front-yaw210.png` (the fade done right at yaw 210, the road with lozenges and brass-beaded crossing)

Not to show: `ring-loop-names`, `done-watched-spot`, `done-window-soon`, `done-guard-back` (grey slab, see-through callout), any FightRange frame, `road-enamel-boulevard-third` (painted-road bars).

Next owner: Latch (callout and fade feathering on the next build); Vela, Datum, Glint for the world items; Glyph re-checks the callout frame after the next shot.
