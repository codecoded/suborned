# The HUD sweep, round 3: the rest of the old kit (brief 101)

Glyph, 2026-11-09. Open `board.png` first (1000 × 9140, for a phone).

This round is a mock only. Nothing changed under `unity/Assets`, in the GDD or in `text.json`. Unity wasn't started, nothing was committed, and no paid or Codex call was made ($0). Every picture is a real 0.2.286 or 0.2.287 frame. The old pieces are lifted off it and drawn again in the kit Latch built for steps 4 to 6 (`HudToolkit.Street.cs`, `Kit/street.uss`). Every colour comes from street.uss, so there is nothing new to learn.

## Five rules, one per family of old pieces

1. **Words in the street are street labels.** Pops, plan ghosts, stash names, the hint line and the act ring's prices lose their plates, chrome and screws.
2. **What you click is a round tab, or a capsule if it has a word.** The tab is the built pause button's form. The capsule is the news card's fix chip.
3. **Time that runs is the tab's own cream ring.** It drains for a window and fills for someone's attention. In the last 10 s it turns orange on ink, like the seal.
4. **Every face wears the built cream ring.** That covers chips, the callout, the shooters on the aim row, plan ghosts, the slip and the pips.
5. **The bottom right holds only the news and the Book.**

## The inventory

I went through 219 captures of 0.2.286 and 0.2.287: the batch's shots and proofs, and Latch's round 4 frames. That is 281 manifests with their parts (`make/r3_inventory.py`; sheets in `out/scratch/glyph/101r3/inventory/`).

| still in the old kit | captures | becomes |
|---|---|---|
| step markers (door, car, body, stand) | 83 | round tabs |
| pips over people | 99 | the badge in the cream ring |
| hint disc · hint line | 43 · 7 | round tab · street label |
| edge chips (reroute, rival car, lock) | 31 | round tab with the ink chevron |
| done chips | 21 | face with its tick, facts as labels, new ways as capsules |
| window markers | 19 | timed tab and its seconds |
| attention callout | 17 | cream-ringed face (a camera: its red eye), a stem, the stage ticks on the agent's badge |
| person cards (Inez's) | 13 | a slip on her, never docked |
| pops | 11 | icon and words, no plate; a refusal wears `mark_no` |
| attention clocks | 7 | timed tab, the stage word |
| stack tab (••) | 9 | small capsule (not drawn) |
| plan ghosts, stash plates | 2 | street labels (a stash name drops under its point, clear of the clock stack) |
| act ring price plates, chrome rim | each ring | street labels, cream rim |
| shooters' faces on the aim row | each aim | cream ring |

Already in the street style: the state words ('done' beside a face, 'taking aim' over a target), the hover label, bubbles, the car's marks, the focus ring and the edge faces. Out of scope: the conversation scene's own frame.

## The next event's limit (for Splice)

Today the budget reads the next-event row as a window marker (`MARKER_MAX = { words: 2 }`), so 'Popovic exits 5:30' fails as three words. It's a different piece: a timeline mark under the stats row. Its form is right as built (`HudToolkit.Clock.cs`): the next-event icon, the name in Jost SemiBold 15, the time in Bold 17, and the time alone while the cell can't know the event. The hover tip keeps the full name.

**The limit:**
- The name is at most 3 words and 16 characters, spaces included. The time is not part of the name.
- Exactly one m:ss, at the end.
- One line, no full stop, one on screen.
- Unknown: the time alone.

**Why 16 characters.** The stats row sets the clock stack's width: 251 px at the start ($100,000), 231 px at $5,000.
- A 16-character name is about 115 px, or 139 px with many capitals (Jost SemiBold 15, measured).
- So the row is 184 to 207 px and sits inside the stack, which never widens or shifts as the next event changes.
- Only below $1,000 does the stats row shrink under it (193 px at $0).

**Why 3 words.** You take three in at a glance. It sits between the lock pill's 2-word reason and the goal line's 6 words.

All seven timeline shorts fit already (at most 14 characters and 3 words).

**For `tools/hud-budget/budget.ts`:**
- A kind `next` (Kind, TextKind, TEXT_KINDS) with `NEXT_MAX = { lines: 1, nameWords: 3, nameChars: 16, times: 1, onScreen: 1 }`.
- Text is `<name> <m:ss>` or `<m:ss>`.
- Faults: over 3 words · over 16 characters · not exactly one clock at the end · a full stop · more than one line · more than one on screen.
- Until the host says `next`, `recognise()` maps a `marker` with letters before a trailing clock to `next`. In today's captures only the next row's texts match ('Chime 5:10', 'Close 8:10', 'Turn driver by 5:20', 'Popovic exits 5:30'). Bare clocks stay markers.
- `MARKER_MAX` stays 2 words.
- Tests:
  - the 7 shorts pass;
  - 'Popovic leaves the Office 5:30' fails on words;
  - 'Councillor Popovic exits 5:30' (24 characters) fails on characters;
  - two rows fail.

Latch then names the row's manifest kind `next` (`HudToolkit.Clock.cs` line 57).

## Inez's card

It goes on her, never beside the news. On screen it becomes a slip of the book's cream page on a fine stem to her head, the same family as the bubbles:
- her face breaks its corner;
- each label word (Debt, Secret) becomes its kit icon, and Fable's line follows as written;
- the ways it opened are capsules you can click, with 'New' in the book's teal;
- the type is 15 px, as the bubbles.

Off screen it doesn't dock. It waits, its read time starting when she's in view. Meanwhile her face among the key people (top right) carries the Book's new dot. Nothing is lost: Intel shows her need and secret once learned, and newly opened ways lead her ring with a lit pip for 90 s.

- r123's moment: 7.33 % of the screen today, 3.41 % without the docked card. The proof's '2 card panels' fault goes.
- On her: 3.81 % as today's plate, 2.49 % as the slip.

## The news line: 40 characters, confirmed

Measured in its own type (Jost Medium 13) in its 300 px box:
- ' · 2:46' takes 40 px;
- real why lines run 5.7 px a character, so 40 characters is 217 to 236 px, with 24 to 43 px spare;
- 45 characters is the edge.

Keep the lozenge as it is. Growing it to fit today's 67-character line takes 430 px, about 130 px more. Its left end would reach x 1310, over the faces docked on the bottom edge (one sits at x 1492 in the quiet frame).

For Fable: the long lines say the place twice ('the square's mast camera saw the Fixer stay too long beside the café'). The mock's line is illustrative: 'a camera saw the Fixer stay too long · 2:46' (36 characters).

## The order for Latch

Each piece is shot and checked at 2x on its own. Run the delta only: the proof sections that show it (r059, r067, r083, r101, r123) and the shots.

| # | piece | files | size |
|---|---|---|---|
| 1 | Inez's card: the slip on her; off screen it waits and her key face carries the dot (clears r123's fault) | `HudToolkit.Card.cs` | M |
| 2 | the next row's manifest kind `next`, once Splice's `NEXT_MAX` lands | `HudToolkit.Clock.cs` | S |
| 3 | timed tabs: window markers (drain) and attention clocks (fill), orange for the last 10 s | `HudToolkit.Done.cs`, `Hold.cs` | S |
| 4 | pops as street labels; a refusal wears `mark_no`; 'Report stopped' is the report's disc struck through | `HudToolkit.Less.cs` (pops) | S |
| 5 | done chips: face with tick, keyed icon and word a fact, new ways as capsules (each a click) | `HudToolkit.Done.cs` | M |
| 6 | the act ring: price rows as street labels, the rim cream, `mark_no` for a shut way | `HudToolkit.Ring.cs` | S |
| 7 | the callout: cream-ringed face (a camera: red eye on the body), a stem, ticks on the agent's badge | `HudToolkit.Attention.cs` | S |
| 8 | the sweep: step markers, pips, the hint disc and edge chips as round tabs; plan ghosts, stash names and the hint line as labels; the shooters' faces in cream; world labels keep out of the clock stack | `Less.cs`, `Hold.cs`, `StepMarks.cs`, `CuesEdge.cs` | M |

## Coverage

Coverage is measured as `tools/hud-budget` reads a capture: the HUD layer alone, alpha ≥ 24 at 1920 × 1080. Per piece, the old figure is today's HUD layer inside the piece's box and the new figure is the piece drawn alone (`make/coverage.json`, `make/coverage-kit.json`, and the board's last table).

| frame | build | today | in the kit |
|---|---|---|---|
| quiet, route G 3:28.6 | 0.2.286 | 3.86 % | 3.68 % |
| the guard's away window, 2:59 | 0.2.286 | 4.20 % | 3.74 % |
| Inez's card docked, r123 4:27 | 0.2.286 | 7.33 % | 3.41 % |
| Inez's card on her, r085 | 0.2.286 | 6.24 % | 4.98 % |
| the act ring on Inez, r101 | 0.2.287 | 4.05 % | 3.84 % |

- 32 of the 35 pieces measured like for like come out smaller.
- The body marker, the hint disc and a pip grow by a hair: the built tab carries a soft shadow.
- The biggest saves: the done chip (0.53 → 0.12 %), 'Uneasy › Listen, Pay' (0.95 → 0.26 %), the stash plates (0.52 → 0.16 %) and the act ring's plates (0.51 → 0.27 %, plus 0.14 % for the cream rim drawn where the chrome one was).

## What's illustrative

- 'Report stopped': no capture shows today's plate, so it is drawn on the quiet frame's reporter.
- The window's last 10 s and a loop the Tech holds are drawn over the 55 s window.
- The news line's words.
- Inez's new dot.
- The stack tab is specified, not drawn.

## Evidence

- **The project's reader on the mocks:** `python3 -I make/r3_budget.py` writes each frame's HUD layer and manifest (lifted pieces out, new pieces in, named as the build names them) and runs `bun tools/hud-budget/cli.ts out/scratch/glyph/101r3/budget`: **5/5 within budget**, no running text (`make/budget.txt`).
- **Today, for comparison:** `bun tools/hud-budget/cli.ts out/batch/proofset/r123`: 6/7. `r123-body-safe` fails '2 card panels on screen', the fault the slip removes. Latch's r4 frames: 8/8.
- **The 2x crop check:** every piece of every frame (`out/scratch/glyph/101r3/*-2x.png`, `kit-*.png`). I fixed:
  - a sibling capture's turquoise ground ring left under a lifted pip;
  - plaza paving copied onto the road under the old lozenge (now cloned along the kerb's measured slope, −0.348);
  - the old stash plates over the clock's stats row (now the next frame's HUD layer, with the names under their points);
  - the bubble's stem cut by the lift under 'reported';
  - the callout's stage tick floating loose (now on the agent's badge);
  - the pip drawn bigger than the old one.
- **Fills:** each lifted piece is filled from another capture of the same camera where one exists (`SIBS`). Inez's docked card is filled from brief 098's capture of the same moment without it. The three pops with no clean sibling ('Already here', 'Watch cancelled', 'Busy: Watch') and the 'Parked' chip were dropped from the sheet rather than shown over smeared paving.
- **Files:** all under 500 lines (the largest is `make/r3_base.py`, 299).

## Open

- **Fable:** 'Take control: T' names a key, and Sky's rule is that nothing is key-only. The pop should name the click, or go.
- **Fable:** the card's label words (Debt, Secret, Help, Pressure) become icons. The rest of each row stays as written. OK?
- **Sky:** the slip is the one piece of reading left in play. It shows for its read time, as today.

## Files

| file | what |
|---|---|
| `board.png` | the board for Sky's phone |
| `quiet.png`, `window.png`, `card.png`, `conv.png`, `ring.png` | the five frames in the kit (1920 × 1080) |
| `make/r3_base.py` | the lift and fills, the street kit's pieces |
| `make/r3_frames.py` | the five frames and their coverage |
| `make/r3_kit.py` | every other piece, before and after |
| `make/r3_budget.py` | the project's reader on the mocks |
| `make/r3_inventory.py` | the inventory sheets |
| `make/r3_board.py`, `make/r3_board_text.py` | the board |
| `make/coverage.json`, `make/coverage-kit.json`, `make/budget.txt` | the measures |

To re-render (about 2 minutes):

```
cd briefs/101/glyph/r3/make && python3 -I r3_inventory.py && python3 -I r3_kit.py && python3 -I r3_budget.py && python3 -I r3_board.py
```

`r3_budget.py` runs `r3_frames.py`.
