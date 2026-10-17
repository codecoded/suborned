# Tech Watch review (Glyph, 2026-10-18)

Reviewed at 4x to 10x: `01` to `08`, whole shots and crops of the plate, the target pins, the panel, the chips and the markers. Layout and graphics only; Fable owns the words. I did not play it.

**Verdict: PASS, with four small fixes (1 to 4) and one missing shot (5).** The Watch knob, the panel row, the busy chip, the tethered state and the door-marker chip all read and follow the kit. Nothing is clipped.

## What works
- **`01`/`02` plate:** the third knob (binoculars, key 3) sits in its socket like the others and the fourth stays an empty socket. Armed (`02`) it takes the turquoise ring and the "Watch" name plate appears below. The 3 on the numeral row is clear of the rim.
- **`03` panel:** header (Tech cameo, arrow, red camera badge, "Camera"), then two rows, Loop and Watch, in the kit's ivory plates. Padding, the key numerals and the price icons are aligned and the close button clears the corner screws. Two icons per price (time, one fact) keep it within the line budget.
- **`06` tethered:** both rows are greyed with a lock, and the Tech's plate loses the Watch knob (an empty socket) and shows the loop as a single 36 px chip beside it. It is clear that the loop holds him.
- **`07`/`08` door chip:** cameo plus the `watch` glyph. Cream rim in `07` (a click selects the Tech), turquoise rim and glyph in `08` (a click gives the order). This is the same grammar as the Intel chip.
- **`04`** the busy chip beside the plate: glyph, bar, "17 s", with 6 px clear of the fold tab.

## Fixes for Latch
1. **Target pins in `02` hug the clock.** The top-edge cameo at (1187, 14) is 9 px from the status pill's left cap. Keep edge pins 14 px clear of the pill, the same margin as the other HUD groups, and slide them left.
2. **`02`: "Watch" name plate touches the plate's bottom rim** (2 px gap). Give it 6 px, as the armed name plate has under the order plate elsewhere.
3. **`04` and `05` bubble tails touch the watching ring.** The diamond tail of "Lens first." lands on the ring's top. Lift the bubble 8 px, or anchor it to the head and not to the marker. `05`'s bubble also sits where the ring was, which makes the end of the watch look abrupt; leave the ring for one beat after learning, with a tick.
4. **`07`/`08`: the unknown marker's "?" is lost in the dither.** The door sits on the edge of the building fade, and the `eye_unk` glyph's "?" is broken up by the dither pattern. Draw the marker disc and the chip above the dither, or keep the fade hole clear of the marker, so the glyph reads whole. In `08` the dashed ring is also cut by the dither.

## Missing
5. **No cone in any shot.** The brief lists "timer and cone" for `04`, but no frame shows the camera's learned cone, or the camera marker while it is watched. I can review the cone only from a shot with the camera on screen: send one of `04` at a moment when a camera is visible, and one of `05` after learning (the cone should appear, drawn red and unlit).

## Show Sky (4)
1. `briefs/016/techwatch/03-panel-entrance.png`: the camera panel with Loop and Watch rows.
2. `briefs/016/techwatch/06-tethered.png`: the tethered state with both rows greyed (the words are changing, the layout is the point).
3. `briefs/016/techwatch/08-door-next-selected.png`: the door marker and the Tech's turquoise next-step chip (after fix 4).
4. `briefs/016/techwatch/02-target-mode.png`: the plate with Watch armed (after fix 1).
