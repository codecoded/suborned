# Glyph's review of 0.2.290 (round 3 as built), 2026-11-09

Recorded by Aster from Glyph's report. Glyph checked 0.2.290 against the round 3 board at 2x and 4x. Crops are in `out/scratch/glyph/101r5/` (git-ignored); `bun tools/hud-budget/cli.ts out/scratch/glyph/101r5/r101` reads 7/7. $0.

Most of round 3 matches the board. Four frames are within 0.03 % of the mock; the card frame's extra 1.1 % is a 21-word street line, not a round 3 piece.

## Latch's three questions and the ring
1. **The lozenge's hover line: ok.**
   - It is right-aligned to the lozenge's end, 10 px over the card, and a second row grows upward (`HudToolkit.Crucial.cs:96`).
   - It is 14.5 px SemiBold, 10 px clear of the roundel.
   - Nice, for Splice: hover lines aren't in the manifest (nor the next-event and eye tips), so the budget never reads them.
2. **Inez's slip under her feet: ok.** The stem stops about 9 px under her shoes, and nothing crosses her body. Over her head it matches the mock.
3. **The round tab after "Watch": the content is right, the form is wrong. Must.**
   - It is `mood_uneasy`, the Watch's learns mark (`HudModel.Less.cs:119`). The mock misread it as the Scout's face.
   - It is drawn as a `RoundTab` (`HudToolkit.RingLabels.cs:89`). A round tab means "click me" (rule 2), so this one, and Listen's two "?" marks, read as fake buttons.
   - Fix: `Ki(li, "ki-ink")` like the other price marks. The cream disc is for the outcome badge only.
4. **The ring at the fountain is a picture bug, not a placement bug.**
   - The proof clicks Inez, and the ring opens there (`HostContext.cs:213-216`).
   - 0.2.289's full proof runs a 2880 × 1864 window, switched to 1920 × 1080 per picture (`tools/game/cli.ts:255`). The open ring keeps its pixel spot (`HudToolkit.Ring.cs:87`).
   - Play doesn't resize, so in play the ring opens on what was clicked.
   - **Must:** retake the picture at 1080 (and a 3840 copy for the 2x check).
   - **Nice:** re-place an open ring on a resize, as `PointerWorld` does (`HandoverHost.cs:453`).

## Fixes
5. **A bubble runs off the screen. Must.**
   - Route A's call, 2:59 (`t101-window`): the Fixer's line runs to x 2120 of 1920 and covers a right-edge face.
   - Bubbles are centred with no clamp (`HudToolkit.Bubbles.cs:66`).
   - Fix: clamp inside the screen with 14 px margins; `BubbleTails` slides the stem.
6. **An edge face's chevron touches the news roundel. Must.**
   - The chevron tip reaches 28 px from the face's centre, and the free test covers only the face's box (`HudToolkit.Edges.cs:47`).
   - Fix: about 22 px of air on `crCard` and `historyKey2` (`HudToolkit.Edges.cs:34`). Rule 5: the bottom right holds only the news and the Book.
7. **The knockout's wake dots. Nice.**
   - 16 turquoise dots around the zZ tab (`HudToolkit.Takedown.cs:116-120`) are a second clock form, against rule 3.
   - Fix: the tab's own ring fills toward waking, orange in the last 10 s.
8. **The done chip sits 8 px above the window tab. Nice.** Make it 14 px (Glyph's floor is 10).

## Missing from the board (Glyph's inventory gap)
9. **The docked callout's speech line** is still the old bevelled `plate_light` with a diamond tail (`HudToolkit.Attention.cs:243`). It should become the street bubble with a stem to the docked face. Glyph specs it in round 4.
10. **The old verb plate, the "Knock out" armed plate and the tiled action strip** are still chrome. They show in r29-cone-alt, seed-fire-alt and hold-watch-reach. Round 4.

## Splice's questions
1. **The `slip` kind (2464dd096): agreed.**
   - Latch tags the manifest kind `slip` in `HudToolkit.Card.cs`.
   - Nice: a place rule, so that a slip never sits in the news and Book box or over the clock stack.
2. **"At most two" means two bubbles on screen at once.**
   - Brief 014 says so (`glyph-less.md:109`, `fable-less.md:286`), and the build does it (`bubblesAtOnce` = 2, `PlayText.cs:304`).
   - The docked callout's line is drawn outside the pool, so a frame can show three today.
   - **Rule for v0.40.0 §10:** "Speech bubbles at the speaker: at most two on screen at once, one per speaker (the newest), 3 s, twelve words or fewer. A conversation's street line keeps the cutaway's caps."
   - **The budget:** at most 2 speech elements on screen in play, and keep the 2-row cap per bubble.
   - The agent GDD's §10 says "≤ 10 words" (line 326); the human GDD, §12 and text.schema say 12.
   - **For Sky:** conversation street lines run as one row up to 712 px wide. Glyph would wrap bubbles at 460 px into at most two rows. That is a design change.

## Routing (Aster)
- **Latch, in the take-control round:** musts 3, 5, 6 and the ring picture (or the re-place); nice 7 and 8.
- **Glyph, round 4 (mock):** items 9 and 10, plus the 460 px wrap beside today's one row, for Sky.
- **Splice, with round 4's build:**
  - a kind for the hover lines;
  - the slip's place rule;
  - two speech elements at once.

  Latch counts the docked line toward the two in the same round, so the batch never stops on a rule the build can't yet meet.
- **Aster:** the bubble rule and the 12-word cap go in the GDD v0.40.0 catch-up (brief 098).
