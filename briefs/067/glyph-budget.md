# Glyph · HUD budget limits for the new proof pieces (2026-10-26)

For Splice (the tool kinds) and Latch (the manifest tags). Read with batch 0.2.123's stop (briefs/041/port-progress.md, "STOPPED at budget") and glyph-r4 §4.

**The rule that does not move (glyph-r4 §4):** never hide words from the reader. Every piece below carries its `text` in the manifest and the tool counts it. None of them is tagged `hud` or `image` to pass. Each gets its own kind with its own cap, the way `pop` and `marker` did, so a long one still goes red.

## 0 · How to count words (all kinds below)
- Count words as shown. A keycap is one word (as `shownWords`).
- The separator `·` is not a word.
- A figure with its unit is **one** word: `8 s`, `29 s`, `1:55`, `$20,000`, `+2`, `siren 1`, `draws 1.0 s`.
- Icons, faces, discs and stance marks are not words.
- One element = one manifest rect. Give rows inside a chip as separate fields (below), not as one `text`.

## 1 · Order pill: kind `pill`
The hold pill on the doer: "Hold · Board · 8 s", "Hold · Drive into the service drive · 29 s", the fire state.
- **Shape:** `<head> · <way> · <time>`, plus price marks where the way has them.
  - head: `Hold`, or `Let go` in the ready state.
  - way: Fable's activity name.
- **Lines:** 1. It never wraps. A wrapped pill is a fault.
- **Words:** at most 8 in all.
  - head: 1 or 2.
  - way: at most 5.
  - time: exactly 1. **The time appears once.** Two time figures in one pill is a fault, so the tool catches "Hold · Board · 1 s · 1 s" (r123-fight-ground).
  - Price marks: at most 2 more (`+1`, `siren 1`, the draw mark). They count inside the 8.
- **On screen:** at most 1 `pill` or `lock`, in total (one pointer, one held way).
- **States:** `active` only (offer, hold, ready, fire). A `pill` in a `rest` frame is a fault, **except** in the follow view while a hold is live. r123-fight-ground is `rest` with a Board pill, so that frame must be tagged `active`. Latch: tag the state from the pill, not from the view.
- **Not a `line`.** It never counts against the goal line's one-on-screen. The goal "Take Vale alive." and one pill may share a frame (r059-drive-hover, r067-squad-board-pill).

## 2 · Lock pill: kind `lock`
The shut way on hover: "Listen to the guard · Already heard".
- **Shape:** lock icon · `<way>` · `<reason>` · the doer's face.
- **Lines:** 1, no wrap.
- **Words:** at most 7. The way is at most 5. The reason is at most 2: Fable's shut word, the same word as the refusal pop.
- **On screen:** shares the one pill slot with §1. A `lock` and a `pill` never show together.
- **States:** `active` only.
- A refusal `pop` repeating the reason at the clicked person may show with it (r067-shut-pop). The pop keeps its own caps (POP_MAX).

## 3 · Done chip: kind `chip`
The learn chip: face, facts, brass hairline, ›, verb pills.
- **Manifest:** one rect per chip with `facts: string[]` and `verbs: string[]`. Don't send one joined `text`.
- **Facts:** at most 3 rows. Each row is one icon and at most 2 words ("Bureau loan", "For hire", "Loves Inez", "Rounds", "Cone").
- **Verbs:** one row, at most 3 pills, 1 word each. A shut verb is struck through and still counts.
- **Words per chip:** at most 9 (3 × 2 + 3).
- **Rows are a list, not running text.** Inside a `chip` rect the stacked-block and three-word sentence rules do not apply. The caps above replace them. A fact row of 3 or more words, or a fourth row, fails.
- **On screen:** at most 2 chips (lifetime 4.2 s; two learns close together, as in r067-lock-pill-guard). A third is a fault.
- **States:** `rest` and `active`. Never in `alt` over a ring.

## 4 · Alt line on the car ring: kind `altline`
The consequence line under a ring wedge in the Alt state ("Go without Vale calls off the attempt. …").
- **Lines:** 1, no wrap.
- **Words:** at most 6. A fragment, never a sentence: no full stop inside the line, and never two sentences.
  - The shipped text is 14 words in two sentences, and it wraps to two lines. That is a content fault, not a tool gap: Fable rewrites it to 6 words or fewer (e.g. "Calls off the take; you escape", Fable's words to choose).
- **On screen:** at most 1, for the hovered wedge only.
- **States:** `alt` only. An `altline` outside `alt` is a fault.
- It must not overlap other HUD. In r067-car-ring-go-alt it runs over the feed card. Splice: fail if an `altline` rect intersects any other manifest rect except its own ring.

## 5 · The conversation cutaway's top bar
- **Ruling: no new kind. The bar is drawn at play size.** The cutaway shows the same clock bar as play (scale 1.0, 48 px tall at 1080, top right, 12 px margins), inside the top letterbox band.
  - At 1080 the whole cutaway HUD draws at about 2x: the bar, the bubble and Skip, in all four `r067-convo-*` frames (the alert plate is 209 × 96 against 106 × 48 in play). At 2160 it is right. So the cutaway's panel scale doesn't follow the play panel's. Only inez-2 tripped the budget, but all four are wrong.
  - The 1.7–2x scale is the fault: it breaks "$20,000" into stacked fragments, and the bar fights the speech bubble for the eye.
  - Latch fixes the scale. Then the bar reads as one row of figures, as it does in every play frame, and passes as `hud`.
- If Splice wants the bar checked in all states, add kind `bar`:
  - 1 row;
  - figures and icons only: clock, speed, end time, cash, Trail, alert lamps, pause;
  - **no letter words** (any word of letters fails);
  - exactly 1 on screen;
  - all play states and the cutaway.

## 6 · Summary for the tool

| kind | lines | words | on screen | states |
|---|---|---|---|---|
| `pill` | 1 | ≤ 8, one time figure only | 1 (shared with `lock`) | active |
| `lock` | 1 | ≤ 7 (way ≤ 5, reason ≤ 2) | 1 (shared with `pill`) | active |
| `chip` | facts ≤ 3 + verbs 1 | fact ≤ 2, verb 1, chip ≤ 9 | ≤ 2 | rest, active |
| `altline` | 1 | ≤ 6, no sentence | 1 | alt only; no overlap |
| `bar` (optional) | 1 | 0 letter words | 1 | all |

Coverage is unchanged: every kind counts toward the state's % limit (rest 15, active 22, alt 30).

**Expected after the tags, before content fixes:**
- These pass: r059-drive-hover, r067-drive-pill-car, r067-squad-board-pill, r067-shut-pop, r067-stack-tab and r067-lock-pill-guard.
- These still fail, rightly:
  - r123-fight-ground: the time twice (Latch).
  - r067-car-ring-go-alt: the Alt line is 14 words and overlaps (Fable and Latch).
  - r067-convo-inez-2: the bar scale (Latch).
- Splice: no allowance for these three. They go green when their fixes land.
