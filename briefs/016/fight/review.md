# Fight readability review (Glyph, 2026-10-18)

Reviewed at 4x to 7x: `01-car-held-armed-pips`, `02-extract-row-held`, `03-scout-hover-rival`, `04-h17-hint`, `05-scout-firing` (whole shots and crops). I did not play it. Brief 018 section 6 is the grammar I checked against.

**Verdict: PASS for play, with six fixes for Latch (1 to 6) and four things to move onto the 018 grammar now (A to D).** The fight reads: the holders are findable, the lock and tether show who is pinning the car, and the pips on the portraits are legible. Nothing is clipped on the HUD itself.

## What works
- **Holders and tethers** (`01`): every holder has one lock disc over its head and a dotted line to the car. That is already the 018 holder mark, one icon each, inside the two-icon limit.
- **Sidearm pips** (`01`, `03`): one small gun disc at each portrait's lower right, away from the F badge and the health dots. Readable at 1x.
- **Rival hover chip** (`03`): the violet diamond, the time and the gun count read in one look. The ground ellipse under the rival is clear.
- **H17** (`04`): glyph, cameo, a short imperative. Padding is even.
- **Goal lines**: "Get ▼ Vale to the car." and "Put Vale in the car." use the car glyph, not a ring.

## Fixes for Latch
1. **Rival name tag collides with the holder lock (`03`, `05`).** "Rival" sits on the lock disc over his head, which is cut off by the tag. Put the name tag above the lock, or drop the tag while a lock shows (the violet diamond on the chip already says rival).
2. **The "3/10" has no meaning (`03`).** A gun glyph and "3/10" could be shots, hit chance or magazine. Say which: if it is the count of shots to down him, use the `hit` form the rules use ("down in 14.0 s" is the time); otherwise drop it from the chip and put it in the hover line. Also the chip shows 14.0 s with a decimal where the rest of the HUD uses whole seconds; use whole seconds unless the fraction decides something.
3. **Violet text on dark teal is too dim (`04`).** The word "rival" and its ▼ are about 2.3 : 1. The signal colours are free now (Sky, 2026-10-16), so lighten it: `#b894ec` for text, keep `#8a4fd0` for the pictogram and the diamond.
4. **The action panel covers the car chip and a locked row shows a tick (`02`).** The panel opens on top of the "Car held" chip ("figh" is all that is left). Open the panel to the side away from the marker, as the Vale panel does. The locked "Into car" row also shows a green tick (`mark_ok`, "works") next to its lock; use `mark_no` or no mark, since the row is refused.
5. **Sidearm pip touches the selection ring (`01`, F4).** On the selected portrait the pip overlaps the turquoise inner ring at the lower right. Move the pip 3 px in and 2 px up on all four, or put it under the ring in the draw order. Also give the "drawn" state a shape cue, not only turquoise versus cream: a turquoise rim ring round the disc when drawn.
6. **Fire timer (`05`).** The bar chip beside the plate has no number, sits far from the target, and its left cap collides with the plate's fold tab. Put it with the shooter (above their portrait's cameo) or at the target, and give the cap 6 px of clearance from the tab. The shot also shows no tracer or crosshair ticks, so tracer colours are not reviewed: send a frame with a bullet in flight.

## Move onto the 018 grammar now
- **A. Drop the sentence chip.** "Car held · fight on" is a clause on the street, and "Car" is a second label above it (`02`). The 018 car marker is the ring and the lane, a seat count and the lock. Draw the held count on the lock ("🔒 3": the number of holders) and nothing else, no words.
- **B. One car marker, not two.** `01` shows a lock disc, `03` and `05` a dashed turquoise ring with a gun glyph, and `02` a white ellipse with a lock. Make it one marker whose state is the shape: quiet solid ring when free; dashed ring plus lock when held; the ring marks the 8 m reach, not a 3 m circle round the car.
- **C. Tethers need an underlay.** They are 2 px white dots on cream stone (`01`, `03`) and vanish where the paving is light. Draw each with a 1 px ink underlay at 50 percent, and end it on the car's ring, not mid-air. When a holder is downed the tether and lock go together (018 requirement), with a one-beat fade.
- **D. Keep room on the portrait for the in-car badge.** 018 puts an in-car badge on each portrait and a left-behind flag. The lower right is the sidearm pip and the lower middle the health dots, so the in-car badge goes top right (the F badge is top left). Reserve it now so the pips do not move twice.

## Show Sky (4, after fixes 1 to 3)
1. `briefs/016/fight/01-car-held-armed-pips.png`: the holders, locks, tethers and portrait pips.
2. `briefs/016/fight/03-scout-hover-rival.png`: the rival hover with its price chip.
3. `briefs/016/fight/04-h17-hint.png`: the hint.
4. `briefs/016/fight/05-scout-firing.png`: only once a tracer is in the frame.

---

# Round 2 (Glyph, 2026-10-18)

Re-reviewed at 4x to 8x: `fight/01` to `05` and `intel/h1-told-slot`, `intel/h9-heard-need`. I did not play it.

**Verdict: PASS, with one fix for Latch on the tracer colour (T).** All six fixes and grammar A to D are in, and the Intel need label works.

## Fixes confirmed
1. **"Rival" tag over the lock:** gone. The lock sits clear and the violet diamond on the chip carries "rival".
2. **"3/10":** removed. The chip is the diamond and "14 s" in whole seconds.
3. **H17 violet:** "rival" and its ▼ are now lighter and readable on the dark plate. The line also reads better ("Fire at ▼ rival: right click.").
4. **Action panel on the car:** it opens to the right of the lock and covers nothing (`02`). The locked "Into car" row now carries the red slashed circle instead of a tick.
5. **Sidearm pips:** moved clear of the selection ring on F4 (`01`).
6. **Fire timer:** it now carries "13 s" and a bar, and clears the fold tab (`05`).
- **A (no sentence chip) and B (one marker):** the "Car held · fight on" chip and the "Car" tag are gone. The marker is a lock with a count badge "5" (`01`, `02`), and a dashed turquoise ring when armed (`05`).
- **C (tethers):** a ring-dot line with a dark underlay, readable on the light paving and in the shade. They end on the lock.
- **D:** the top right of each portrait is free for the 018 in-car badge.

## Intel
- **`h1-told-slot`:** the hover fragment sits inline on line 1 ("Inez · [debt] Debt") with the turquoise ring on the slot, and nothing is covered. Told (dashed dashes) and unknown (fine dots) now differ at a glance.
- **`h9-heard-need`:** "Inez · [debt] Bureau loan" reads clean and fits the line. The `turn_him` chip shows with Inez's all-learned card, the `secret_none` tab is on the driver, and the turned badge's tick is bigger and legible. One small note: the need glyph in the hover line is about 12 px against 16 px in the slot, which looks fine.

## One fix for Latch
- **T. The tracer is violet (`05`).** The Scout's shot to the rival is drawn in the rival's signal violet, so a cell shot reads as rival fire. I rule the tracer colours (§14):
  - **cell fire:** a cream core with a turquoise glow;
  - **rival fire:** violet;
  - **guard and police fire:** amber.
  - Add a small flash disc at the muzzle, and keep the line to 2 px with a 1 px ink underlay so it holds on pale stone.
  - The tracer also needs to be a little shorter lived than the fire timer, so it reads as a shot and not a tether (it looks like the holder tether now).

## Still waiting on brief 018
The 8 m hold ring and the lane. The "5" on the lock is the count the 018 car marker should keep.

## Show Sky (4)
1. `briefs/016/fight/01-car-held-armed-pips.png`: holders, locks, tethers and the portrait pips.
2. `briefs/016/fight/03-scout-hover-rival.png`: the rival hover chip.
3. `briefs/016/fight/04-h17-hint.png`: the hint.
4. `briefs/016/intel/h9-heard-need.png`: Intel after a Listen: the need label, the chip and the turned badge.
Hold `05-scout-firing.png` until the tracer colour is fixed.
