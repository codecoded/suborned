# Review of build 0.2.104 (HUD and world), Glyph, 2026-10-24

Scope: Latch's 0300 evidence (`briefs/049/build/2160-0300/`, `054/build/0300/`, `057/build/0300/`, `028/corners/game-0300/`, `028/road/game-0300/`, `041/build/0300/`) against hud29, brief 057 §2-§4, Revised A and the art bible. The 3840 x 2160 shots are 2x of 1080p, so a native crop is the 2x crop. The corner street eyes are 1920 x 1080 (1x). Board of the worst issues: `briefs/041/review-0300/board-worst-issues.png` (single crops beside it).

## Verdict: a fix round before Sky plays, then ship

The HUD pieces I could check are in the kit and read well. Four things stop it: the stray kerb bars (they were meant to be gone), the stray "Patrol away" chip, the empty reroute edge chip, and the pause menu and Fight Range panel looking like the lab. Several new pieces were not in the evidence at all, so they are unreviewed (list at the end).

## Ranked fixes

### 1. Stray kerb bars still lie in the road at the civic junction and junction NE (blocker)
- Shots: `briefs/028/corners/game-0300/r29-civic-junction-street.jpg`, `r29-civic-junction-street-b.jpg`, `r29-junction-ne-street.jpg` (crops `041/review-0300/01-stray-kerb-civic.png`, `01b-stray-kerb-junction-ne.png`). Also the white line in `r29-civic-junction-heights.png`, a straight raised line across the road at (435-620, 247).
- What is wrong: straight raised stone bars about 10 cm tall, with mitred stubs and gaps, run across the carriageway at the junction mouths. They are not curvy and not clean. These are the straight kerb strips that Layers.Ground is meant to drop (the "stray kerb" in port-progress #22). The bays are fine (squarebay, officebay: kerb rounds the bay mouth, no bar). So the fix covers the junction flats but not these two.
- Fix: extend the strip cut-out (StripGrow 0.35 m) to the junction `ground/return/*` flats at civic-junction and junction-ne. Port-progress says `tools/geometry/returns.ts` has no `plot/bend`, flagged to Splice: that gap is the likely cause. Re-shoot the heights image to confirm it is flat.
- Owner: Splice (returns.ts and the world export), then Latch (Layers.Ground).

### 2. The stray "Patrol away" chip carrying Pay, Help and Pressure (diagnosed)
- Shots: `057/build/0300/r29-reroute-walk-0.png` (fading, over the camera pole), `r29-reroute-walk-1.png` (solid, stacked on "Hold · Watch the patrol · 35 s"); crops `02-...`, `02b-...`.
- What it is: the Reroute landing's done chip (HostDone.cs ~282: `AnchorKey = "patrol"`, row "Patrol away", the Tech's face). Its unlock row is a diff of `Gates()` (HostDone.cs ~190-199). The line is `var chip = made.Find(x => x.Role == parts[0]) ?? made[^1];`. A reroute chip has `Role == ""`. So any gate that opened on the same tick (a person's `role|pay`, `role|help`, `role|pressure`) matches nothing and falls back to `made[^1]`, the reroute chip. That gives the patrol an unlock row for a person it has nothing to do with. The pills do not say whose they are.
- Why the gates opened on that tick is not shown in the evidence. I did not trace it. Latch should log the keys (`open` minus `openBefore`) on that tick.
- Fix: delete the `?? made[^1]` fallback. Attach an unlock only to the chip whose Role matches. If none matches, show no unlock pill. The unlock row only ever belongs to a person's chip (Listen, Watch, turn). Add a check to ProofDone and Proof29: a chip with Role "" never has Unlocks.
- Second fault in the same frames: in walk-1 the chip is lifted above the Watch pill and shows the same chip 30 s later. That is the proof's fast-forward, not a bug, but check that a chip lives only 4.2 s in real play.
- Owner: Latch.

### 3. The reroute edge chip is an empty plate (blocker)
- Shot: `057/build/0300/r29-reroute-walk-0.png` at about (825, 55); crop `03-edge-chip-empty.png`.
- What is wrong: `plate_dark` with the cameo on the left and nothing on the right. The spec (057 §2) is the cameo plus the `pointer` icon rotated toward the wait spot. The pointer is missing or off the plate. The plate is also about 48 x 23 px (1080p), wider than its content. The rule is: cameo + arrow, nothing else, plate shrinks to fit. Port-progress says the chip was "30 px with a 10 px arrow", so the arrow is clipped or not tinted cream.
- Fix: draw the pointer icon (cream, 12 px) rotated, with the plate sized to cameo + 4 px + arrow + 6 px padding each side. Re-shoot at 2x.
- Owner: Latch.

### 4. Night: dark radial spikes in the strip-light pool at the rounded corner
- Shot: `028/road/game-0300/road-enamel-office-210-night.jpg` around (1130-1240, 235-440 in my crop); `04-night-corner-spikes.png`.
- What is wrong: the kerb strip itself curves smoothly, but the warm pool on the asphalt shows black wedges fanning out from the corner. That is a triangle-fan artefact in the pool mesh (a fan on a convex arc). It reads as a sunburst, not a pool.
- Fix: build the pool along the arc as a strip (ribbon with per-vertex falloff), not as a fan from the corner centre. Or add arc segments to the pool mesh so the falloff is continuous.
- Owner: Latch (strip-light mesh), Glint to approve.
- Related: at "evening" (lens 3.0, pool 0.14) the frames are identical to day in all three views (`road-enamel-*-evening.jpg` against the day shots). Either the phase is not applied by the road tool, or lens 3.0 is below visibility. If that is the intended look, say so. Otherwise the evening state is untested. Glint to confirm. Day and night read well otherwise: a clean cream edge line, dotted crossing studs, no steps, no z-fight at the bay mouths in the shots I saw.

### 5. Pause menu is a flat rectangle over the briefing card; Fight Range is the lab
- Shots: `049/build/2160-0300/menu.png` (crop `06-...`), `hold-range-panel.png` and the whole hold-fire-* and hold-squad-nogun shots (crop `07-...`).
- Pause menu: a sharp-cornered, translucent dark rectangle with letter-spaced words. The briefing card text ("The Handover", "BEGIN", the stats) shows through and collides with RESUME and FIGHT RANGE. It is the only major surface without a kit frame. Fix: `panel_dark` (nine-slice, chrome rim, corner screws), opaque, one centred plate. If the briefing is behind it, dim the briefing. Owner: Latch, to my spec.
- Fight Range: flat grey web buttons on a translucent teal panel. This is the lab look and breaks the "lab look stays in lab" rule. If it is a dev tool, hide it from the player build (it is in the pause menu as "FIGHT RANGE"). If Sky should see it, I restyle it in the kit. Aster to decide; I recommend hiding.
- Owner: Latch (hide), Glyph (restyle only if kept).

### 6. Smaller HUD faults (fix in the same round)
- **Worse mark plate is cramped.** `049/build/2160-0300/done-help-pip.png`, price line end; `05-...`. The mint half disc fills the whole inner height of the plate: about 0 px padding top and bottom. The plate is 16 px at 1080p and the spec is 22. Fix the height to 22 px with a 15 px mark. The colour passes: it is the kit `mark_half`, an existing source. The same stack shows the fading "Uneasy" chip under the Intel chip, which is a transitional state (fine).
- **Hint under the ring.** `041/build/0300/r29-ring-downed-carry.png`; `08-...`. The plate "Pick when to fire" sits under the ring's lower rim, so the text is struck through by the ring edge and dimmed. Fix: pin the first-act hint below the ring's bounding circle (y + radius + 10 px), or hide it while a ring is open. Owner: Latch.
- **CARRYING plate** (`041/build/0300/r29-carrying.png`). Icon at the left and "0.7 m/s" at the far right of a 165 px plate with an empty middle (it is as wide as the footprints/timer plate below it). Narrow it to its content, or give the middle to a word. Detail, not a blocker. Also, a second floating button with the body icon sits at the roster's top right next to the down portrait. It is the same icon as the disc under the portrait. Check whether it is a duplicate.
- **Alt over Inez shows three discs** (`054/build/0300/r29-cone-alt.png`): the cup, the mood disc and a cyan walker disc. The walker is the armed order's marker, not a status icon, but it reads as a third icon over a person. Move it out of the stack, or put it on the person's ground ring.
- **"Read all"** (`049/.../hold-plan-paused`, `r29-cone-alt` history card): a flat white pill. Use the kit's `plate_dark` chip or cream `btn_text`, not a bright white fill.

### 7. Reroute marks: pass with one note
- `057/build/0300/r29-reroute-walk-0..3.png`: the route is a thin cream-over-ink dashed line, two parallel legs (out and back), chevrons in opposite directions on the two legs, the robot sits on it, the line stays all the way through the walk, the window marker "115" under the camera, the wait ring with a cream cameo at the cabinet. Hairline weight is right. Edge chip: see 3.
- Note: the two parallel dashed legs about 50 px apart at 2x double the line count. Fine for now. If Sky finds it busy, draw the return leg dimmer.

## The `signal_*` cameos: decision for Aster
They pass for this build and do not need the cream patrol cameo before Sky sees them, with two conditions.
- They have a kit source (the action panel header already ships them) and sit at 18 px in the chrome cameo ring.
- Amber is the guard colour and the patrol is security, so the in-world marker (the amber shield over the guard, the unit's pip) matches. No kind or role colour is added to the plates themselves.
- Condition 1: the edge chip (3) must not ship as the only coloured thing on an otherwise empty plate. Once it has its pointer, it reads as "amber robot, that way".
- Condition 2: I did not see the Call cameo, the cause disc in `signal_police`, or the holder badges (not in the shots). If any of them turns up in a Go-held or Call frame, it needs the same check.
- The cream `badges/npc_patrol` is still wanted. Forma draws the face from `041/build/0300/r29-patrol-face.jpg`; I make the disc. Not blocking.

## World checks

- **Rounded corners (7 spots):** the kerb return stones read curvy and clean at the corners: junction NE (`r29-junction-ne-street.jpg`: smooth kerb arc, brass line parallel; small jogs in the brass line and a pale triangle shard on the paving at about (870-940, 155-175) of my crop, minor), officebay and squarebay (smooth S-curve kerb). No steps, gaps or z-fighting at the arcs themselves. The failure is the straight bars (1). The heights images for all other spots are flat.
- **Bay mouths:** no stray kerb at squarebay or officebay. Pass. At the office bay the kerb stones step up in small pale blocks at both ends of the mouth (`r29-officebay-play.png`, near (1230-1330, 600-640) of my crop): small, but check the end caps.
- **Road, three phases:** see 4. Day and night read well; evening shows no change.
- **Road seams:** thin dark hairlines cross the asphalt at officebay, squarebay and boulevard (`r29-officebay-street.jpg`, `r29-boulevard-street.jpg`). They are slab seams. Low; a lighter tone-match would hide them.
- **Sky-rail shadows:** at boulevard, boulevard x market and circus mouth the rail shadows are very dark, hard, olive-brown bands across the road. They read as painted stripes. Glint to lighten and soften them. Not a blocker.
- **Patrol face upright:** pass. `041/build/0300/r29-patrol-face.jpg` shows an upright dot-matrix smile (the mouth is below the eyes). The service robot face (`r29-service-robot-face.jpg`) is also upright in two orange screens. Pass. The patrol stops 1.77 m from the pole (needs 0.6 m): pass.

## Pieces that pass at 2x (HUD)
- **Mood disc** (`054/build/0300/r29-mood-likely.png`, crop at 1.3x): `disc_dark_24`, cream icon, chrome ring, centred over the head with the cup disc beside it. Uneasy reads (head, glancing eyes, one drop). Max two icons: ok (Alt aside).
- **Pill with "Pay Inez"**: single line, no mark, correct (Pay is neither likely nor worse for uneasy; the half mark appears on Help).
- **Seal marker** (`057/build/0300/r29-seal-clock.png`): `plate_dark`, lock icon 17 px, "2:15" Jost Bold, turquoise-light underline draining, right edge 3582 (spec 3590), 24 px (12 px at 1080) clear of the known-people discs. The cause disc (`eye_open`, dark disc in chrome ring) is centred, the icon fills it, and the gap to the plate is 20 px. Pass. Not shot: the amber last-10-s state.
- **Quiet cones:** `057/build/0300/r29-cone-rest.png` against `r29-cone-strong.png`. Rest is a thin red rim at about 55 % with a barely visible fill, far edge still dashed; the stairs, door and people read through it. Strong is the old fill, fading. They read as the same cone at two strengths. Pass. The scan line is gone.
- **"Camera looped" plate and "Patrol away":** the label and icon are in the kit. The Loop pill (`hold-plan-paused.png`) is clean, with the Tech's face.
- **Shortened Watch footer:** `054/build/0300/r29-cone-alt.png`: "Watch +3 0:09 / Inez: Hush, and drink it before it cools." Clean, centred, padded. The 1.3x crop shows no clipping.

## Not in the evidence (need a shot before I can sign off)
- The "likely" plate (dashed ring, check, word). Only the exact solid half mark is shown.
- The seal marker in its last 10 s (amber, blink), and the held-Go wedge with holder badges.
- The no-walk cursor (a cursor is probably not captured by the screenshot tool; needs a hand-drawn overlay shot).
- The "Camera looped" done plate itself (only the Loop pill appears).
- Inez's round (Latch already flags it).

## Other odd things in the 2160 set
- The sealed end card (`proof.json`) gives two lines that both say the car is the problem: "The getaway car hadn't left when security sealed the district" and "Vale wasn't in the car when security sealed the district." Fable should merge them into the one line of the empty/ready/held table.
- The Intel chip says "Loves the guard" with a family (adult and child) icon. This is the known missing lover icon (Aster).
- The kit scene is otherwise consistent: roster, order plate, status pill, known-people discs, plot scope and notes key are all in place. HUD budget was not re-measured.

## Handoffs
| # | Owner | Done-check |
|---|---|---|
| 1 | Splice, then Latch | street shots at civic-junction and junction-ne show no bars; heights flat |
| 2 | Latch | no `?? made[^1]` fallback; a Role "" chip never has unlocks; reroute chip has only "Patrol away" |
| 3 | Latch | edge chip shows cameo + pointer, 2x crop |
| 4 | Latch, Glint | night pool has no spikes; evening state confirmed or explained |
| 5 | Latch (hide Fight Range, kit-frame the pause menu); Aster to decide Fight Range | menu shot over briefing is opaque kit plate |
| 6 | Latch | 2x crops of the hurt plate, hint, carrying plate |

## 0.2.112 check (Glyph, 2026-10-24)

Scope: Latch's 0302 evidence, checked at native 3840 x 2160 crops (the 2x crop); the corner street eyes are 1920 x 1080. Crops are in `out/scratch/glyph/0302/`.

### Verdict: ship to Sky

Every blocker and fix from the 0.2.104 review is met. No fix round is needed. Four nits below are for later and none holds up Sky's play.

### Fixed, checked
| 0.2.104 item | Shot | Result |
|---|---|---|
| 1 stray kerb bars | `028/corners/game-0302/r29-civic-junction-street.jpg`, `-street-b.jpg`, `r29-junction-ne-street.jpg`, `-street-b.jpg` | Gone. Road is clean asphalt to a smooth kerb arc at both spots. |
| 2 stray "Patrol away" unlocks | `057/build/0302/r29-reroute-walk-1.png`; `049/.../proof.json` | The chip holds only the Tech's face and "Patrol away". `rolelessUnlocks` 0; the three waitress gates log as `unattachedGates`, shown on no chip. |
| 3 edge chip | `041/build/0302/r29-reroute-edge-chip.png` | Cameo in a chrome ring (amber robot) plus a cream pointer on a snug `plate_dark`, with padding. It is no longer empty. |
| 4 night pool spikes | `028/road/game-0302/road-enamel-office-210-night.jpg` | The rounded corner is a clean warm line with a smooth falloff. No wedges and no doubled spikes. |
| 5 pause menu | `041/build/0302/menu.png` | Opaque dark plate, 1 px chrome rim, briefing hidden, no Fight range. It is the light-line frame Sky picked in brief 051. At 2x it does not fail (no clipping, words centred, even padding), so it stays. The square corners are Sky's pick. |
| 6 worse mark plate | `041/build/0302/done-help-pip.png` | The mint half disc has air all round now, in a 22 px plate. |
| 6 hint under the ring | `r29-ring-downed-carry.png` | "Pick when to fire" sits below the ring's rim. Nothing is struck through. |
| 6 carrying plate | `r29-carrying.png` | Icon plus "0.7 m/s" on a snug plate. The duplicate disc beside the downed portrait is gone. |
| 6 "Read all" | `done-help-pip.png`, `menu.png` | A `plate_dark` chip with cream words. |
| Fight Range | `hold-range-panel.png` | It shows only in the range scene (a proof or `-debug` build), by design. The release menu has no Fight range. |

### Pieces I could not see before (#8), now reviewed
- **Likely plate** (`054/build/0302/r29-likely-plate-forced.png`): the dashed ring, check and "likely" are the kit's grey mark plate, with even padding at the end of the price line. Pass.
- **Hurt plate** (`r29-hurt-plate-forced.png`): a dashed ring with a half disc, the same plate, one icon. It reads as worse. Pass.
- **No-walk cursor** (`041/build/0302/r29-no-walk-cursor.png`): a cream footprint struck through on a dark disc. It reads as "can't walk there" at once and does not clash with the chip above it. Pass.
- **Camera looped** (`r29-camera-looped.png`): the Tech's cameo with a check badge, the loop icon and "Camera looped", in the kit's chip frame. It is centred, with no clipping. Pass.
- **Seal, last 10 s** (`057/build/0302/r29-seal-last10.png`): the lock and "0:08" are amber (the HUD's own "running out" colour), with the underline nearly drained and a small amber stub. No other colour is added. Pass.
- **Held Go** (`r29-go-held.png`): a grey "Go" plate with a lock and the holder badge, which is a violet rival disc in a chrome ring (the rival signal colour is free). It reads as locked and held by a rival. Pass.
- **End card** (`r29-seal-end`, `proof.json`): one car line under the title. Pass.

### Nits (not blocking; log, do not fix now)
1. Junction NE (`r29-junction-ne-street.jpg`, `-street-b.jpg`): a small pale sand triangle sits against the kerb at about (850-1050, 590-610), and the brass line shows visible kinks along the arc. Owner: Splice (returns.ts), low.
2. No-walk cursor (`r29-no-walk-cursor.png`): the disc is a translucent grey with no chrome ring, unlike the kit's `disc_dark_24`. A cursor is allowed more freedom, so this is low. Owner: Latch.
3. Held Go (`r29-go-held.png`): the lock sits on the edge of the car pip and the "1" badge, touching both. Owner: Latch, low.
4. Seal plate (`r29-seal-last10.png`): the plate shows the scene faintly through it, as the known-people plate does. They match each other, so it is not a fault. If Sky says "busy", make both opaque.

### Handoffs
None required before Sky plays. Aster: the cream `badges/npc_patrol` disc (Forma face, my disc) and the "Loves the guard" lover icon are still open from the 0.2.104 review; neither blocks.

## 0.2.113 check (Glyph, 2026-10-24)

Scope: Latch's 0303 evidence (`049/build/2160-0303`, `041/build/0303`, `054/build/0303`, `057/build/0303`, `028/corners/game-0303`, `028/road/game-0303`, `048/evening/game-0303`), the changed pieces cropped at 2x or more (crops in `out/scratch/glyph/0303/`).

### Verdict: ship to Sky. Two small fixes are worth doing in the next batch; neither blocks play.

### Evening phase (Glint): holds the look
- **Palette:** `048/evening/game-0303/start-overhead-evening-vs-day.jpg`, `road-enamel-boulevard-third-evening.jpg`. The low warm sun, peach haze and coral-to-mauve sky read as the same city at a later hour. Cream walls go peach (sampled #dbb58e, hue 30) and the road goes a cooler periwinkle (#6e7788). Nothing greys out.
- **Teal and seafoam do not go olive.** Deep teal stays hue 166-188 (#4b6468 on windows, #517c72 on ground bands). Sunlit seafoam trim moves from hue 136-147 to about 99 (#89a37c), a sage; in shade it stays 137. At 2x (`road-enamel-office-210-evening.jpg`, crop at 1000-2000 x 0-900) it still reads mint-sage, not olive. Pass, but it is the one to watch if the sun gets warmer.
- **HUD over it:** roster, clock bar, pill, goal plate, feed card and plot scope are unchanged and read cleanly over the evening paving, the road and the peach sky (`office-front-yaw210-evening.png`, `cafe-front-evening.png`). The HUD adds no colour of its own.
- **Lit windows:** at night (`road-enamel-boulevard-third-night.jpg`) the warm amber panes are the only light and read well. In the evening they are not noticeable in the play-view shots (daylight still wins). Fine.
- **Watch, not a fix (Glint):** at night the building faces and the paving crush to near-black (wall #020100, paving #061212; `road-enamel-office-210-night.jpg`). The district reads as a silhouette with lit panes, which suits a moment, but nothing in the evidence shows the play view at night with the HUD. Before night is offered to Sky, give me a night play shot (start overhead and café front) and I will check that the cast, rings and cones still read. A deep teal or indigo floor instead of pure black would stay closer to the bright retro-future.
- **Minor, Glint:** on the boulevard evening shot the khaki wall on the right (day #c2c5b2, already sage) drops to #7a7964 in shade, which reads muddy olive-grey. The fix is a touch more warmth in the shade grade for that hue band. Low.

### Corners round 2: good at the play camera
- **Lawn-track ends:** `r29-boulevard-market-play-210.png`: pill ends, with the cream kerb following the round. Pass.
- **Junction NE:** `r29-junction-ne-street.jpg` and `-street-b.jpg`: the sand triangle is gone and the arc is smooth. Pass.
- **Civic junction, office bay, square bay, tram platforms** (`r29-civic-junction-play.png`, `r29-officebay-play-210.png`, `r29-squarebay-play.png`): rounded returns, brass a clean hairline, no kinks at play zoom. Pass.
- **Nit 1, Splice (returns.ts), low:** at street level the brass hairline and the kerb are still faceted (visible straight segments with a corner every few metres) on the junction-NE arc in `r29-junction-ne-street-b.jpg`, around (780-900, 600-950); there is also a small step in the outer brass line at the top of that crop. Not visible at the play camera, so it will not stop Sky; it only shows in third person.
- **Nit 2, Splice (returns.ts), medium:** `r29-circus-mouth-play.png` still has two stray cream line fragments lying in the road, at about (870, 665) and (1210, 570). In 0.2.112 they were straight bars; now they are faceted arcs. They are the leftover kerb returns that brief 057's "no stray kerb bars" meant to remove. They read as chalk marks on the road. Same area in `r29-circus-mouth-street.jpg` if you want a ground view.
- **Nit 3, Splice, low:** the road sheets leave faint tonal steps and thin hairlines across the road in `r29-boulevard-market-play-210.png` (around (1300, 200) to (1700, 580) and the thin lines near the lawn). Visible only when you look for it. The taupe-olive band across the road (#777262) is the sky-rail guideway, unchanged since 0.2.112; if it is meant to read as enamel it is the one thing in the corner shots that tends olive. Vela's call.

### My pieces and your nits
- **Lovers icon** (`049/build/2160-0303/done-listen-chip.png`, crop at 2.5x): two figures with a heart above, in the same solid cream as Bureau loan and Fearful, on the same row grid. Reads "couple" at once. Pass; the old open item is closed.
- **Patrol stand-in face** (`057/build/0303/r29-reroute-edge-chip.png`, crop at 2.5x): a cream disc with the dot-matrix robot face glyph in deep teal, inside the kit's chrome ring, on the edge chip. It reads as a robot. Pass. The icon beside it is the white cursor arrow, not a part of the chip.
- **No-walk cursor** (`041/build/0303/r29-no-walk-cursor.png`, crop at 8x): the footprint pair, struck by a dark diagonal, now sits in the 2 px chrome ring with the 1 px ink-deep edge, the same as the kit disc. Pass. The test parked it on the corner of the Uneasy chip, so it overlaps the chip's rim there; in play it follows the cursor, so that is the shot, not the build.
- **Held Go** (`r29-go-held.png`, crop at 3x): "Go", the lock and the violet rival badge have even gaps; the lock no longer touches the badge. The plate is still translucent, as the known-people plate is. Pass.
- **Driver** (`049/build/2160-0303/follow-pickup.png`, crop at 3x): dark-brown face and hands, mint-green uniform, black boots. No teal skin. Pass.

### Perf changes (cones)
- The fan rim and wedge still frames (`054/build/0303/r29-cone-strong.png`, `r29-cone-rest.png`, `r29-cone-pressure-a1.png`) look right: a red outline with a soft wedge, the rim crisp at 2x, the quiet cone faint at rest and strong when picked.
- A still cannot show the two perf changes (one cone built per frame, a rim that steps rather than widens at mid-fade). At 0.2.113 every new cone is built within 1-2 frames per Latch's numbers (5.7 and 2.6 builds a second), so a plain wedge would show for under 0.1 s. Not seen as a problem. Suture's replay or Sky's first play should confirm no flash of unlit wedge when the patrol's fan changes pose (10 geometry builds a second remain).

### One small HUD thing (Latch, low)
- In the corner "replay 0.6x" shots (`r29-officebay-play-210.png`, `r29-civic-junction-play.png`, `r29-squarebay-play.png`), the roster portraits are hidden but the four pip rows ("o o o o") stay on screen as free-floating dots at the left (crop at (0-400, 0-900)). If replay hides the roster, hide the pips with it. These are test shots, so it only matters if the same state shows in a real replay.

### Fix list (all optional before Sky)
| # | Item | Shot | Fix | Owner |
|---|---|---|---|---|
| 1 | Stray cream arcs in the road | `028/corners/game-0303/r29-circus-mouth-play.png` (870,665) and (1210,570) | Drop the orphan kerb-return fragments | Splice |
| 2 | Pips left without the roster in replay | `r29-officebay-play-210.png`, left edge | Hide with the roster | Latch |
| 3 | Night play view not shown | none | Provide start-overhead and café-front night shots before offering night | Glint, Latch |
| 4 | Faceted brass line and kerb on the NE arc | `r29-junction-ne-street-b.jpg` | More samples on the arc | Splice |
| 5 | Road sheet seams; taupe-olive guideway | `r29-boulevard-market-play-210.png` | Even the sheet tone; Vela decides the guideway colour | Splice, Vela |

No handoff to Aster is needed. The HUD stays inside the art bible's colour rule: the only colours I found were kit sources, and the violet rival badge is the free signal colour.
