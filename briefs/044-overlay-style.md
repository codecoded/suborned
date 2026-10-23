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
