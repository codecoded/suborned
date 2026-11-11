# Brief 108 · the briefing as three pages, built (Glyph, 2026-11-11)

Sky's pick: the three pages (`../pages/`), plus his hint that the square's cameras see most of what we do. Built in **0.2.324** (`out/build/play`).

## Stills (from 0.2.324, `briefs/108/glyph/shots.json` through `bun run batch --only shots`)
- `p1-mission.png`: page 1.
- `p1-returning.png`: page 1 after a first play, with Skip to Begin.
- `p2-cafe.png`: page 2 on Inez's café, as the tour shows it.
- `p2-door-steps.png`: page 2 with the front door's chip open (its steps, live). The street is on the Office door.
- `p2-pickup.png`: page 2 on the pickup.
- `p3-cameras.png`: page 3 with the square's cameras hovered, the street on the mast, and Begin.
- `*-2x-*.png`: 2x crops of every piece: the page text, the rows, the chips, the steps, the pager, the label pinned in the street, the dial chip and the look pill. All are clean: nothing clipped, centred, padding right.

## Checks
- **Host logic:** 206/206, including the new `Brief108` (5 cases).
  - The pages come from the text, fit the grid and sit clear of the rail and the pager.
  - Begin shows only on page 3 at the start. Skip shows only after a first play.
  - A way's chip opens its steps, each mark equal to its live check.
  - The square's cameras have Inez in view most of the time from 0:00 to 5:30, by the engine's own sight, so Sky's line is true.
- **Proof r083 alone on 0.2.324:** ok, 108 rows, 0 failed. `bookNeeds` "test condition True".
  - On 0.2.323, two r085 clip rows failed once under load (load average 14–16) and passed on the rerun.
- **TS side:** typecheck clean; contract, HUD-budget, dialogue and shot tests 436/0.

## Words
Placeholders for Fable live in `contracts/missions/handover/text.json` under `book.brief`, with their schema in `book.schema.ts`.

## Open
- The badges on page 3 are the kit's 48 px sprites drawn at 70 px. They are a little soft at 2x; a 96 px set would fix it.
- The Journal never listed the ways' steps. They tick live behind the chips on page 2, which is where the Job tab opens in play.
- The nine ways to fail are still data (`book.job.ends`), shown nowhere at the start now; page 3 replaces them.
