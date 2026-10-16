# Glyph review, HUD polish round 2 (2026-10-16)

Reviewed: the 38 crops in `briefs/014/play/polish/round2/`, `polish/roster-plate-2x.png`, `polish/status-strip-2x.png`, the full shots, and Sky's five crops. Crops are 2x renders. I inspected them at 5x to 9x, and the kit art at 8x to 16x.

**Verdict: NOT PASS.** Three of Sky's four flags are fixed:
- the selection ring no longer covers the F4 badge;
- the photos are centred, with no cream band in any of the four;
- the activity chip is no longer cramped.

I found two real defects Latch's note does not mention:
- The plate bolts are not whole, because the art is wrong. I fixed the art.
- The key numerals collide with the plate's bottom rim.

I also found one asymmetry on the plate, and several smaller spacing defects. Fix the list below, run the kit import, and I'll re-check.

## Art I changed (kit source `briefs/011/ui/kit/`, script `briefs/014/glyph/scripts/round2_art.py`; originals in git 1f8e3e3)

| File | Change |
|---|---|
| `roster/roster_column@1x/@2x.png` | The 8 frame screws (4 corners, 4 between the portraits) moved 6 art px in from the rails. Top and bottom corner screws moved 3 px away from the caps. Rails restored behind them. Nothing touches a rail or a cap any more. Window rects and bezel centres are unchanged, so no code change. |
| `panels/plate_verbs@1x/@2x.png` | Rebuilt at 96x78 and 192x156. It was 96x54 and 192x108, with half-bolts drawn at the 9-slice split. Stretching to 68 px smeared them into streaks. They are now whole screws of the pill and roster family, centred on the plate. Nothing sits in the stretched band. |
| `portraits/portrait_ring_inner@1x/@2x.png` (new, 78 and 156 px) | A selection ring that sits inside the bezel, over the photo. See decision 1. |
| `sprites.json` | `plate_verbs` size and use note updated. `portrait_ring_inner` added. |

## Decisions

**1. Selection without a ring: not clear enough. Use the new inner ring.**
- The roster is about 87 px wide in a 1726 px shot. A turquoise 25x21 badge and 17% dimming are easy to miss. The plate beside the portrait only helps while a verb plate is open.
- Sky asked for selection to be clear, and there is no room outside the bezel.
- The new ring lives inside the photo circle, so it cannot touch the badge or the screws.
- Sprite: `portraits/portrait_ring_inner@2x.png`, 78 logical px. It has a 3.8 px turquoise band at the rim, a cream line and a soft inward glow.
- Latch: add one element the same size and position as the photo clip (78 x 78 on the bezel centre, r 39), shown only for the selected agent.
- Layer order inside the clip: photo, scrim, **ring**, pips. The key badge stays on top of everything.
- Keep the turquoise badge and the dimming. Do not bring back `portrait_ring_selected`.
- Pips: with the ring, raise the pip row 2 px, to `2*rad-14` from the clip top, so the dots clear the band.

**2. Roster bottom screws: they read as cramped.** At 2x the bottom-left screw touched the rail end and sat 2 render px from the cap rim. The top pair did the same. Fixed in the art, as in the table above. No code change.

## Defects for Latch

Crop names are in `briefs/014/play/polish/round2/`.

**High**

1. **`plate--01-tech-loop-targets.png` and `keys` numerals: the key numbers cross the plate's bottom rim.**
   - The feet of "1" and "2" overlap the teal inner rim, and the plate is too short for the knob row plus numerals.
   - New art: draw the plate at exactly **78 px tall** (`PlateH = 78`). The art is 1:1, so the screws never stretch.
   - Field inside the rim: y 8.5 to 69.5.
   - Place the knob element at y = 9.5 (bezel top 13.5, bezel bottom 53.5, 5 px of field above).
   - Numerals: top at y 56, cap height 8 or less, so about 5.5 px of field remains below.

2. **`plate--01…png`: the content is off-centre.** The gap from the left screw to the first bezel is about 4.7 px. The gap from the fourth bezel to the right screw is about 1.9 px. Use `KnobX0 = 28.5` and `PlateW = 248`. Both gaps are then about 8 px, and the knob pitch of 47.6 is unchanged. Re-measure after the render.

3. **`edge-1--10-held-door.png`, `edge-1--11…png`, `edge-1--01…png`, `edge-1--cafe-after.png`: the inner disc is off-centre.**
   - The off-screen edge marker's inner dark and cream disc sits about 5 to 6 render px down and right of its silver bezel disc.
   - It reads as mis-registered art. The single pips (`pip-guard--03/04`, `pip-waitress`) are centred correctly.
   - Centre the glyph disc on the bezel disc and on the turquoise or amber ring. The direction wedge is the only thing that should be off-axis.

**Medium**

4. **`pip-guard--10-held-door.png`, `--11…png`, `pip-driver--08…png`: two icons overlap.**
   - The shield disc covers about a third of the lock disc, so the lock's bezel is cut and the pair reads as a mess.
   - Place them side by side with a 2 px gap, or stack with at least a 2 px gap.
   - Both discs must stay within the person's two-icon limit and inside the crop. The shield is clipped at the bottom now.

5. **`pop-1--11…png` ("Held") and `beat-1--08…png` ("Reported."): the glyph is too tall for its chip.**
   - The lock shackle and the handset touch the chip's top edge, with 3 render px of padding or less.
   - Shrink the glyph to at most 70% of the chip height, or grow the chip.
   - Left padding is about 10 px against about 30 px on the right. Make them equal, about 14 px.
   - `beat-1--10`/`11` ("Police coming."): the shield disc is 6 px from the chip top and bottom and 8 px from the left, against 30 px on the right. Same fix.

6. **`armed--03-knockout-price.png` ("Knock out"): the text is crammed against the chip edges.** Left and right padding is about 6 render px, and the stroke touches the rim. Add 8 px (logical) of horizontal padding and 3 px vertical.

7. **`sheet--01-tech-loop-targets.png` (Cabinet panel).**
   - The time and Trail columns shift left by about 50 render px on the rows that carry a key badge (rows 1 and 4), against the rows without one. Reserve the badge slot on every row so the columns align.
   - On rows without a badge, "+1" ends 10 render px from the row edge. Give every row at least 14 px of right padding.
   - The close button (x) sits about 8 px from the top-right screw. Move it 8 px left or down.

8. **`end--09-end-card.png`.**
   - The buttons are centred at left 93 and right 60 px, so the group is off-centre. Centre it, or left-align it to the headline at x=75.
   - Padding below the buttons is about 25 px against about 52 px above the headline. Match the top and bottom padding.
   - The "Return" keycap on Replay is tight against the right tip, and the label is off-centre. Move the label to the centre of the lozenge body and the keycap into the right third.

9. **`roster--01…png`, F4: 6 health pips crowd the bezel.** The first and last pips touch the chrome inner edge. Cap the row width to the chord at the pip y (about 60 px), or shrink the pip diameter and the gap with count. Do this together with the 2 px raise in decision 1.

**Low**

10. **`goal--01…png` ("Take ▼ Vale alive."):** the gap before the triangle is about 14 px and after it about 4 px. Make both 8 px.

11. **`scope--01…png`:** the three level dots are 8 px from the frame's right edge and 15 px from the bottom. Move them 6 px in, to 14 px from the edge, to match the other HUD margins.

12. **`status-strip-2x.png` after, "→| 2 ⏱ 1 s":** the arrow glyph is about 8 px from the left cap. Add 6 px. This is the same padding issue as 5.

## What passes (do not touch)
- Photos centred in the bezel, no cream band showing, clipped to r 39. Badge position is consistent on all four.
- Activity chip (`timer-*`): fixed slots, even 24 px padding left and right, bar and text centred. `timer-3--07/08` (loop icon in its small chip) is fine.
- Status pill (`pill--01…png`): ends, dividers and group spacing are even.
- Price chip (`chip--03…png`): group spacing is acceptable.
- Bubbles (`bubble-1/2`) and `keys--01…png`: balanced.
- Soft art: the roster frame, plate and portraits render at less than 1:1 of the @2x art, so none is upscaled. The photos themselves are 132 px sources, as in round 1.

## Hand-off to Latch
1. Run Suborned/UI/Import Glyph's Kit. It copies the new plate, the roster frame and the new `portrait_ring_inner` sprite, and regenerates `kit.uss`, which gets a `.s-portrait_ring_inner` class.
2. Apply the code values above: `PlateH 78`, `PlateW 248`, `KnobX0 28.5`, knob y 9.5, numeral y 56, the inner ring, the pip raise and pip cap. Then items 3 to 12.
3. Re-run the polish capture. I will re-check the roster, plate and edge markers at 5x.

## Evidence
- Crops viewed: all 38 round2 crops, the two composite sheets, and Sky's roster, plate and chip crops.
- Measurements made on the art and the renders with Pillow and OpenCV.
- Screw positions found by template match (7 of 8 above 0.8; the 8th at (199, 424) is the mirror of the 6th).
- The new art was checked at 6x to 7x after each pass; the plate, the roster and the ring were each rendered in a test composite.
- No Unity code or scene was edited, and nothing was committed.

---

# Round 3 (Glyph, 2026-10-16)

Reviewed: the 46 crops in `briefs/014/play/polish/round3/` at 5x (roster, plate and edge markers first, then the rest at 3x to 7x), plus the kit art at 7x.

**Verdict: not pass, but close.** The roster, the edge markers, the plate content and 9 of the 10 spacing items from round 2 pass. One defect is mine, an art seam on the plate, and I fixed it. Three small ones are Latch's. Fix those and I will sign off from the crops without another full pass.

## Confirmed fixed
- **Roster (`roster--01…png`).**
  - The screws clear every rail and cap.
  - The inner ring shows on F3 only. It is clear, it does not touch the F3 badge, and no cream band shows.
  - The photos are centred.
  - F4's six pips now sit inside the chord, 4 px from the chrome.
  - The dimmed F1, F2 and F4 read clearly against F3.
- **Plate content (`plate--01…png`).**
  - The screws are whole and centred.
  - The numerals sit about 6 px clear of the rim.
  - I measured the gaps at 8.5 px (left screw to first bezel) and 8.3 px (fourth bezel to right screw) at 2 px per logical unit. That is even, within 0.2 px. Latch's 9.5 and 9.2 are about 1 px larger than I get, with the same left/right balance. No change is needed.
- **Edge markers (`edge-1--*`).** The disc is concentric with the ring, and the wedge is the only off-axis part.
- **Pips.** The guard and driver pips are single discs, so the overlap is gone.
- **Chips and panels:**
  - "Reported.", "Police coming." and "Held": the glyph is inside the chip, with padding of about 5 px.
  - Cabinet sheet: the columns line up on every row, and the close button is clear of the screw.
  - End card: the buttons are centred (left margin 70, right 73).
  - "Take ▼ Vale alive.": the gaps around the ▼ are even.
  - `scope`: the level dots are 19 px from the edge.

## Defects

1. **Plate end-cap seam: my art, fixed, needs a re-import.**
   - At 7x the rim at both ends of the plate showed two horizontal steps, where the middle band repeated rows 53 and 54 of the original art. Row 53 was an outlier.
   - I rebuilt the middle band as a smooth morph between rows 51 and 56, with no repeated row. `briefs/011/ui/kit/panels/plate_verbs@1x/@2x.png` changed, with the same size and slices as before.
   - **Latch:** re-run Suborned/UI/Import Glyph's Kit. No code change.

2. **`armed--03-knockout-price.png` ("Knock out"): the text is too tall for the chip.**
   - The cap height fills the chip, with about 1 px above and below. The side padding is now fine.
   - Make the chip about 6 px taller, or the text 2 px smaller, so there is 4 px or more above and below.

3. **`keys--01-tech-loop-targets.png`: the document glyph is flush with the chip.**
   - It touches the right inner edge, the top and the bottom, and its lower-left corner is square against the chip.
   - The gap from the divider to the glyph is about 20 px, against 0 px on the right.
   - Make the glyph about 16 px tall, with 6 px of padding on every side, and put it 6 px right of the divider.

4. **`timer-3--05-takedown.png`, `timer-3--07-carry.png`, `timer-3--08-body-hidden.png`: the loop glyph in the small chip is too large.**
   - The ring touches the chip's inner top and bottom, and its arrowhead is cut by the top edge.
   - Scale it to about 75% (about 18 px), and centre it on the chip.

5. **`end--09-end-card.png` (low):** the padding is about 52 px above the headline and about 35 px below the buttons. Add 12 px under the buttons.

## When these are fixed
After items 2 to 4 are fixed and the kit re-imported, these are the shots and crops I would show Sky:
- `briefs/014/play/polish/round3/roster--01-tech-loop-targets.png`: the screws are clear, the inner selection ring shows on F3, the photos are centred, and F4's six pips fit.
- `briefs/014/play/polish/round3/plate--01-tech-loop-targets.png`: the whole screws, the numerals clear of the rim, the even gaps, and no seam.
- `briefs/014/play/polish/round3/edge-1--10-held-door.png`: the disc is centred in the ring.
- `briefs/014/play/polish/round3/sheet--01-tech-loop-targets.png`: the aligned columns and the clear close button.
- `briefs/014/play/polish/round3/end--09-end-card.png`: the centred buttons.
- The full shot `briefs/014/play/01-tech-loop-targets.png`, with the selected agent's ring, the dimmed others, and the plate next to the selected portrait.

---

# Round 4 (Glyph, 2026-10-16)

Reviewed: the 46 crops in `briefs/014/play/polish/round4/`. I checked the changed items at 5x to 7x. I diffed the crops against round 3 and looked at every one that changed.

**Verdict: PASS.**

- **Plate end caps:** the seam is gone. The rim is continuous at both ends, and the screws are whole and centred. The numerals clear the rim, and the gaps are even, as in round 3.
- **"Knock out" chip:** it is 32 px tall with about 4 px of clear space above and below the text, so it is no longer cramped.
- **`keys` (history glyph):** the glyph is 15 px with about 10 px of padding. It is clear of the edges and the divider.
- **Loop glyph in the small chips (`timer-3--05`, `--07`, `--08`):** it is centred and the arrowhead is whole. At 12 px it is slightly small but readable, and I accept it.
- **End card:** the padding under the buttons is now about 55 px against about 52 px above the headline. The buttons stay centred.
- **No regressions:** `roster`, `scope`, `pop-1` (Held) and `response` match round 3. The other changed crops differ only in the scene behind them (shot variation).

## Show Sky (all in `briefs/014/play/polish/round4/` unless noted)

1. `roster--01-tech-loop-targets.png` shows the screws clear of the rails, the inner selection ring on F3, the dimmed others, centred photos and F4's six pips.
2. `plate--01-tech-loop-targets.png` shows the whole screws, the numerals clear of the rim, even gaps and a smooth end cap.
3. `edge-1--10-held-door.png` shows the disc centred in the ring.
4. `sheet--01-tech-loop-targets.png` shows the aligned Cabinet columns and the clear close button.
5. `end--09-end-card.png` shows the centred buttons and the even padding.
6. `armed--03-knockout-price.png`, `keys--01-tech-loop-targets.png` and `timer-3--05-takedown.png` show the three small chips that were crowded before.
7. The full shot `briefs/014/play/01-tech-loop-targets.png` shows the selected agent in context.
