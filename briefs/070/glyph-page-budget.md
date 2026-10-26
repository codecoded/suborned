# Glyph · the briefing page's own limits (2026-10-26)

For Splice (tools/hud-budget) and Latch (the page's manifest). GDD v0.27.0 §10 briefing: the page is outside play, so the reading limits don't apply. The page still gets its own checks.

Read with build 0.2.130's budget stop (briefs/041/port-progress.md, "Briefing budget") and briefs/067/glyph-budget.md (§0 word counting is the same here).

**The rule that doesn't move:** never hide words from the reader. Every word on the page sits in a manifest element with its `text`, and the tool counts it. A new state changes the caps, but it doesn't add an exemption.

## 1 · A state of its own: `page`
- Add `page` to `State`. Latch tags the briefing page's manifest `state: "page"`.
- Retire `briefing` (the street overlay B is gone in v0.27.0). Keep it in the tool only while old fixtures use it.
- `page` isn't in `LIMIT`. **Coverage isn't checked**: a full-screen page is 100 % by design, and 37.3 % only measures how much of it is pictures.
- `BRIEFING_READING` (3 sentences, 20 words) doesn't apply to `page`. The running-text and block rules don't apply either: the page's lines are sentences by design.
- The front end (menu, options, controls) can take `page` later, with its own caps. That's out of scope here.

## 2 · What the tool checks instead
Coverage told us "how much of the world is hidden". On a page nothing of the world shows, so the checks are about whether it's **legible, complete and tidy**.

### 2.1 Modal and opaque
- No pixel of the street shows through while the state is `page`. The paper and the band are opaque.
- Check: the shot's world mask (or `--world`) is empty, or the manifest's `paper` rects cover the frame. Fail: "the page shows the street".

### 2.2 Everything is in the manifest
- Every text stroke the reader finds lies inside a manifest element that carries `text`. Fail: "unlisted words at x,y".
- **No element larger than its piece.** An `image` covers one picture. Today "cell faces" is one rect of 1920 × 1080, which hides the whole page from this check. Each face is its own `image`, 52 × 52.
- **The plan's words and pins are elements.** The plan stays an `image` for its lines. Its labels are `hud` elements (`Corner café`, `Records Office`) and its pins are `pin` elements, so 2.4 can see them.

### 2.3 Nothing clipped, nothing outside
- Every element lies inside the safe frame: 48 px in from each edge at 1080. The seafoam band and the foot band may bleed.
- Every text element lies inside its parent (a still caption under its still, a line inside the band). Fail: "clipped".

### 2.4 Nothing touches
- No two text, `pin` or `image` elements overlap, and each keeps a gap of at least 4 px at 1080.
- **The one exception:** a `pin` sits on its own still's corner, inside the still.
- This is the check that catches the plan's "Records Office" label under pin 2 (0.2.130).

### 2.5 Type size
- Body text (way sentences, the target's facts, cell lines, clock labels, captions) has a line height of at least 20 px at 1080. Clock labels in 0.2.130 measure 20 px, so they pass.
- Headings are bigger than body (the tool reads heights).

### 2.6 Pins agree
- Every still has exactly one `pin`. Every still's pin appears once on the plan and once in the legend, with the same number or glyph.
- Pins are numbered 1..n in way order, and the ending has the car glyph.
- The data is in the manifest (`pin` elements carry `n`). Fail: "pin 3 on a still, missing on the plan".

### 2.7 Word caps per part
Counted as glyph-budget §0. The 0.2.130 count is in brackets.

| part | cap | 0.2.130 |
|---|---|---|
| mission name | 1 line, ≤ 3 words | 2 |
| hook | 1 sentence, ≤ 8 words | 6 |
| objective | 1 sentence, ≤ 10 words | 7 |
| target: name + facts | name ≤ 3 words; ≤ 2 facts, each 1 line, ≤ 8 words | 2; 5, 8 |
| way: name | ≤ 3 words | 2, 1 |
| way: when tag | 1 plate, ≤ 2 words + a time | "Before 5:50" |
| way: sentences | ≤ 2 sentences, ≤ 30 words | 22, 28 |
| still caption | 1 line, ≤ 5 words | ≤ 3 |
| ending | ≤ 1 sentence, ≤ 16 words | 12 |
| clock | 3 to 7 stations, times ascending; each label ≤ 2 rows, ≤ 12 words | 5 stations; 11 max |
| cell line | 4 faces; each ≤ 2 rows, ≤ 16 words | 14 max |
| purse | 1 figure | $100,000 |
| heat | 1 word from {Unknown, Noticed, Wanted, Infamous}, plus the head word "Heat" | Unknown |
| Begin | 1 word | Begin |
| **the page** | **≤ 200 words** (about a minute's reading) | 182 |

- Ways: 1 to 3 rows, each with 1 to 3 stills.
- The total cap is the one that keeps "the reading is too much" from creeping back. A generated mission with three ways must still fit 200 words, so Fable's lines get shorter, not the page longer.

### 2.8 No widows
- A wrapped text element doesn't end on a line of one word. Fail: "widow".
- The tool can't see line breaks from the manifest. Either Latch emits the rows (`rows: string[]`), or the reader counts runs per element.
- 0.2.130 fails this rule: the front door's "it.".

## 3 · Done-check (Splice)
- `bun tools/hud-budget/cli.ts briefs/070/build/0130/budget/briefing-day.png --state page` fails these rules and no others:
  - 2.2: the 1920 × 1080 "cell faces" rect.
  - 2.4: once Latch emits the plan labels, the "Records Office" label and pin 2 overlap.
  - 2.7: the Heat head word is missing.
  - 2.8: the front door's widow.
- After Latch's fixes (port-progress "Glyph 2x (0.2.130)" N2, N6, N7, N8) it passes.
- The fixtures need tests for a failing case of each rule:
  - 2.4: a label on a pin.
  - 2.6: a pin missing on the plan.
  - 2.7: a 31-word way and a 201-word page.
  - 2.8: a widow.
- `bun run test` stays green, and `bun run typecheck` is clean.

Next owner: Splice (the tool), then Latch (`state: "page"` and the manifest pieces in 2.2 and 2.6).
