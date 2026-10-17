# Intel panel review (Glyph, 2026-10-18)

Reviewed at 4x to 5x: `01-start`, `02-mid`, `03-late`, `h1` to `h6` (the worst case "Bartholomew" and "Advancement" included), cropped to the panel. I did not play it.

**Verdict: PASS, with Latch's calls ruled below and four small fixes.** The panel does what the spec said. Padding clears the screws, the columns align, name and role sit on one line, the slots are the same three in the same order on every card, and the chip tells you who acts and with what. The long names fit: "Bartholomew · café waitress" ends 12 px inside the card.

## Rulings on Latch's five calls
1. **"No secret" tab: it needs a glyph. Fixed in art.** A solid empty tab reads as a missing icon or a bug. I drew `secret_none`, the outlined envelope with a strike through it. It keeps the solid learned rim, so "known: nothing to find" is clearly different from "unknown" (dashed, "?") and from a learned secret (the filled envelope). Hover line: "No secret".
2. **Chip with the `turned` glyph for "all learned, not turned": no, change the glyph.** The `turned` glyph is a grey disc with a tick, which is the same mark a turned person's badge carries, so the chip would read "already done" next to a person you have not turned. I drew `turn_him` (a bust and a U-turn arrow). Keep the Fixer's cameo with it, and keep the rim rules (turquoise if the Fixer is selected, cream if a click selects him), and opening his action panel is the right behaviour. This state was not in the shots, so I could not look at it; check it once it is.
3. **Tooltips above the slot cover the card's own name: not acceptable. Move them.** At 5x they hide "Inez" and cut the role into fragments ("I... itress", "caf... waitres"), and on the last card they sit across the next row's badge ring. Do not float them. Instead, while a slot or the chip is hovered, show the fragment inline on line 1, in place of the role text: "Inez · [glyph] Debt", "Inez · [watch] Watch". The slot keeps its turquoise ring. No overlay, nothing covered, nothing to clip at the edges. "Advancement" is about 78 px, so it fits in the 176 px of line 1 the role uses now.
4. **Turned: ring and tick, no chip: accepted.** It reads as done. Make the tick a little bigger and brighter (12 px, turquoise `#60e0ea`, with a 1 px ink outline), since at 1x the mint tick on the badge is hard to see (`03-late`).
5. **No keyboard walk: fine to leave out.** The mouse covers it, and Tab is taken. If it is wanted later, use the arrow keys while the panel has focus.

## Other defects for Latch
- A. **Told and unknown rims look nearly the same.** Both are fine dots at 1x, and a told fact is only told apart by its glyph. Make the told rim longer dashes (3 px on, 2 px off) and keep the unknown rim as fine dots, or the reverse, so the rim differs at a glance.
- B. **Chip content is a little right-heavy.** The verb glyph sits 10 px from the right edge but 12 px from the cameo. Make both gaps 8 px.
- C. **The first card's bar and the lighter fill look good.** No change; noted so it is not "fixed".

## Art added
- `briefs/014/glyph/glyphs/secret_none@1x.png`, `@2x.png`
- `briefs/014/glyph/glyphs/turn_him@1x.png`, `@2x.png`
- script `briefs/014/glyph/scripts/intel_glyphs.py`
- Latch: re-run "Import Glyph's Kit" (it reads that folder).

## Show Sky (5)
1. `briefs/016/intel/02-mid.png`: the panel at rest, with the priority card.
2. `briefs/016/intel/03-late.png`: a turned person, the order of the cards, and the chips (after the tick and `secret_none`).
3. `briefs/016/intel/h6-worst-case.png`: the longest name and fact (after the inline hover).
4. `briefs/016/intel/h2-unknown-slot.png`: the unknown slot says how to learn it (after the inline hover).
5. `briefs/016/intel/intel.png`: the before and after, with my mock.
