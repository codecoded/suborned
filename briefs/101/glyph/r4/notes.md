# The HUD sweep, round 4: the docked line, the last chrome, the wrap (brief 101)

Glyph, 2026-11-09. Open `board.png` first (1000 × 5535, for a phone). It runs: Sky's pick (the wrap), item 9, item 10, then the coverage of every frame.

This round is a mock only:
- nothing changed under `unity/Assets`, in the GDD or in `text.json`;
- Latch's files weren't touched;
- nothing was committed;
- no paid or Codex call was made ($0).

Every picture is a real 0.2.290 frame. The copies are in `out/scratch/glyph/101r4/src/`, with `SHA256.txt` and a `KEEP` file, because the batch shots rotate and Latch has built 0.2.292 since. The old pieces are lifted off each frame and the new ones drawn in the built street kit (`HudToolkit.Street.cs`, `HudToolkit.Tabs.cs`, `Kit/street.uss`), under the five rules of rounds 2 and 3. No new colour, sprite or font: every piece is one the build already draws.

## 1. The docked line (item 9)

**Today** (`HudToolkit.Attention.cs:243`, `SpeechPlate`): when a line is said off screen, its speaker's live face docks at the screen's edge. The line sits beside it in the old bevelled `plate_light`, with a diamond tail, 13 px type, at most 320 px wide. It is drawn outside the pool of two bubbles, so a frame can show three lines:
- r123's fight proof at 4:15 has the Fixer's call line, a rival's line and the guard's docked line;
- the batch's `getaway-car-yaw210` has three too.

**The design:**
- **The look.** The street bubble as built: the book's page cream, 28 px, round ends, a soft shadow, Barlow Medium 15.5 in the book's ink. It has no face of its own (12 px padding each side): the docked face is the speaker. It wraps as every bubble does: one row today, or two rows at 460 if Sky picks B.
- **Where.** `SpeechPlate`'s tries are unchanged:
  1. beside the docked face, toward the screen's centre and level with it, 14 px off its rim;
  2. then below or above;
  3. then the diagonal;
  4. then the far side;
  5. then slid along the edge.

  It keeps 10 px off the HUD. The scope's buttons join that list: at 2:39 Inez's line slides up 7 px to keep 10 px off the eye button.
- **The stem.** The built `Stem` (cream 2.2 px on its ink keyline), with no dot, from the bubble's end to the face's rim.

**The rule, for v0.40.0 §10** (my round 5 review's sentence, with the docked line added):

> Speech bubbles at the speaker: at most two on screen at once, one per speaker (the newest), 3 s, twelve words or fewer. A line said off screen, where its speaker has a docked face, shows beside that face and counts toward the two. When a third line would show, the oldest gives way, wherever it is drawn; a docked line that gives way takes its face with it. The Journal keeps every line. A conversation's street line keeps the cutaway's caps.

**Why the oldest gives way.**
- That is already the pool's law: newest first, up to `BubbleMax` (2), one per speaker.
- The docked line simply joins the same queue, with no priority list to tune.
- A line that gives way is still in the Journal.

At 4:15 the age order (the call's line, then the rivals', then the guard's) is my reading of the run. The build knows each line's age.

| frame | today | round 4 | lines on screen |
|---|---|---|---|
| 4:15, r123 fight (0.2.290) | 5.11 % | 4.45 % | 3 to 2 |
| 2:39, r085 Inez docked (0.2.290) | 4.13 % | 4.31 % | 1 to 1 |

At 2:39 the line goes from the two-row plate (267 px wide) to one row of the bubble (398 px), which reads 0.18 points more.

## 2. The last chrome (item 10)

**Today (0.2.290):**
- **With the Action plate on** (a pause-menu setting, off by default):
  - the verb plate (`s-plate_verbs`, 248 × 78) with four chrome knobs, screws and ⇧1 to ⇧4 under them;
  - a fold tab and its stub;
  - the "Knock out" armed plate (`plate_dark`, 68 × 32) under the lit knob.
- **With it off:**
  - the strip on Tab or Shift for 2.5 s (`plate_dark`, rounded-square tiles), which takes no click;
  - the armed verb at the cursor in a teal disc with a turquoise ring.

**The design: one verb row for both.**
- **The tabs.** A built round tab (`RoundTab`, 30 px, the pictogram at 0.52) for each verb the agent has.
  - The row starts 14 px after the take-control tab (x 144 at 1080). The tabs sit 6 px apart, level with the selected face.
  - A missing verb is left out and the rest close up, as the strip does.
  - The 14 px gap against 6 px groups the verbs apart from the take-control tab.
- **A click** arms the verb (`host.Pick(k)`), as a knob does. A second click on Knock out flips it to Kill, as its key does. Giving the order disarms it, as today. Esc plays no part.
- **Hovered.** The built hover ring (turquoise-light), and 6 px under the row the verb's name in a street label (Jost Bold 13.5, ink), then " · " and its key (`m.VerbKey[k]`) in the soft ink. It's the take-control tab's way: "Fire · ⇧2".
- **Armed.** The book's deep teal body (`e-deep`) and the turquoise-light ring at 2.2 px. Its name sits under it as a street label ("Knock out", or "Kill" for that way). That label replaces the armed plate.
- **One name at a time.** A hovered tab's name replaces the armed name while the pointer is on it.
- **Shut** (Fire while carrying Popovic): the tab at 55 %, its pictogram kept.
- **The outcome marks** (`KnobPredFrame`: the Fixer's turn ways over a hovered person). The kit badge (`mark_ok`, `mark_half`, `mark_no`) sits at the tab's lower right on the cream disc, as the built face carries it: 0.42 of the tab. Rule 4 keeps the cream disc for the outcome badge only.
- **When it shows.**
  - With the Action plate on, always.
  - With it off, for 2.5 s after Tab or Shift (`VerbStripAt`), as the strip does. Its tabs take clicks while it shows.
- **At the cursor.** The armed verb is its pictogram keyed in ink (18 px, 14 px right of the tip and 16 px under it). There's no disc, because a disc means a click.
- **What goes:**
  - `s-plate_verbs`, the `btn_verb` knobs and the ⇧ numerals;
  - the fold tab, the stub and the `PlateFolded` preference: the row is about a quarter of the plate's size, and the setting turns it off;
  - `armedPlate`;
  - the strip's plate and tiles;
  - the cursor's disc;
  - the takedown's way pills (`wayRow`), already hidden in play (`HudToolkit.Less.cs:173`).

**Sky's rules:**
- no hit chance and no action time anywhere on the row: the outcome marks are answers (✓ ½ ✗), never a chance;
- every verb has a click: the tab, and the ring has them all too;
- keys show on hover only;
- Esc is untouched.

| frame | today | round 4 |
|---|---|---|
| the Action plate, Knock out armed (r29 proof) | 3.68 % | 2.94 % |
| the Action plate, Alt held (seed proof) | 3.63 % | 2.93 % |
| the strip after Shift (hold proof) | 3.05 % | 2.81 % |

## 3. The wrap, for Sky

**Today:** a bubble never wraps (`WhiteSpace.NoWrap`, `HudToolkit.Bubbles.cs`), and conversation lines may be 25 words. So the Fixer's 21-word line at 4:27 is one row 712 px wide.

**Option B:**
- A bubble is at most 460 px wide at 1080 while one row fits (416 px of text with the face, 436 without).
- Past that it takes two rows (47 px tall: the font's 18.6 px a row, corners 14 px). The bottom stays where the build puts it over the head, so the second row grows upward. The face is centred on the bubble's height.
- **The break:** after a comma or a full stop when that row is at most 40 px longer than the most balanced break ("It's yours, / Miss Nikolaou"). Otherwise it takes the break that makes the longer row shortest. That is a few lines of code: measure each word once when the text changes.
- **A line two rows of 460 can't hold widens its bubble, and never takes a third row.** Two lines do that today: 123 characters at 485 px and 128 characters at 490 px.
- Under B, 85 of the 140 conversation lines in `text.json` take two rows. Only 9 of the 1,144 barks do.

| frame | A · one row | B · two rows at 460 |
|---|---|---|
| 4:27, r123's card frame: the Fixer's 21-word line | 712 × 28 px · 4.58 % | 400 × 47 px · 4.46 % |
| 2:59, route A's call: the Fixer's 17-word line (as clamped) | 589 × 28 px · 4.89 % | 358 × 47 px · 4.87 % |

Notes on the figures:
- The build's own 4:27 reads 4.51 %. My redraw of the same row reads 4.58 %, so the drawing adds about 0.07.
- The build's 2:59 reads 4.53 % only because the line runs off the screen.
- Footprint (tools/hud-budget): 9.2 to 8.2 % at 4:27, and 9.7 to 9.1 % at 2:59.
- At 2:59, A has to slide 250 px off the Fixer to stay on screen, and B 135 px.

**For Sky, in one line:** today a long line is one row up to 712 px (a third of the screen); B wraps it at 460 px into at most two rows, breaking after a comma or full stop. The screen share is the same (4.58 against 4.46 %, 4.89 against 4.87 %). B reads at a glance and stays nearer its speaker. My pick: B.

## For Latch: the build order

Build 1 to 3 together with Splice's rule 1 (the routing already says so), so the batch never stops on a rule the build can't meet yet. Each piece gets shot and checked at 2x on its own.

| # | piece | files | size |
|---|---|---|---|
| 1 | **The pool counts the docked line.** `Bubbles()` takes the host's lines newest first, one per speaker, up to `BubbleMax`. Each goes at its speaker if the speaker is on screen, else beside its docked face if it has an inset (`Bubble.Inset`). A line with neither is skipped, as today. `Callouts()` hides a speech inset whose line isn't one of the two, so its face goes with it. The docked bubble is placed after the callouts, as `SpeechPlate` is today. | `HudToolkit.Bubbles.cs`, `HudToolkit.Attention.cs` | S |
| 2 | **The docked line's look.** The pool's bubble element with no face (`paddingLeft` 12), 14 px off the face's rim, then `Stem(end, rim, dot: false)`. `SpeechPlate`'s plate, diamond tail and 320 px cap go. Its tries and `SpeechPlateHits` stay. The manifest kind stays `speech`. | `HudToolkit.Attention.cs` | S |
| 3 | **Must 5, finished.** The clamp (`BubbleX`) keeps a bubble 10 px off the edge faces too. At 2:59, a line clamped 14 px inside the screen still covers the Scout's edge face (x 1879). The scope's buttons join the docked line's keep-outs. | `HudToolkit.Bubbles.cs`, `HudToolkit.Attention.cs` | XS |
| 4 | **The verb row** (item 10, as in section 2). It goes in a new `HudToolkit.VerbRow.cs`, and the old plate's code leaves `HudToolkit.cs` and `HudToolkit.Less.cs` (both over 500 lines). | see below | M |
| 5 | **If Sky picks B: the wrap.** `WhiteSpace.Normal` on the bubble's label. Its width is set from the measured rows when the text changes: one row up to 460, else the phrase or balanced break, wider only when two rows of 460 can't hold the line. Height 47, the face centred. | `HudToolkit.Bubbles.cs` | S |

The files for row 4:
- the plate's build: `HudToolkit.cs` 309 to 319;
- its placement, the armed plate and the fold: `HudToolkit.Less.cs` 175 to 205 and 946 to 964;
- the strip and the cursor's disc: `HudToolkit.Live.cs` 199 to 265;
- the outcome marks: `HudToolkit.Mood.cs` 91;
- the knob ring: `HudToolkit.StepMarks.cs` 84;
- the way pills: `HudToolkit.Takedown.cs` 50 to 57 and 112;
- the keep-out lists (`verbPlate` and `armedPlate` replaced by the row): `HudToolkit.Edges.cs` 33 and `HudToolkit.Attention.cs` 82;
- the selected face's label row starting after the row while it shows: `SelectedChipX`.

The manifest: the row is one `image` element, "verb row" (the tabs, and their words while hovered or armed), as the take-control tab is. "order plate", "plate fold tab", "plate stub" and "verb strip" go.

**Delta checks:** r085 (the inset), r123 (the fight), the shots with bubbles (`t101-window`, `getaway-car-yaw210`), the r29, seed and hold proofs (`r29-cone-alt`, `seed-fire-alt`, `hold-watch-reach`), and any proof that reads `verbPlate` or `armedPlate` (`rg` them).

## For Splice: the budget rules

1. **Two lines at once.**
   - In play (`rest`, `active`, `alt`), at most 2 `speech` elements on screen, the docked line included. Count manifest elements, not runs: the CLI's "speech N" counts runs.
   - Fault: `N speech bubbles on screen (at most 2; a docked line counts)`.
   - Today `r123-fight-safe` and `getaway-car-yaw210` show 3. Land this with Latch's 1 to 3.
2. **At most two rows for every bubble, conversation lines included.** `speechFault` caps rows only for bubbles that aren't a conversation's line ("the cutaway shows one line at a time"). In the street a conversation's line is a bubble. The word caps stay as they are: a bubble 12 words, a conversation's line the cutaway's 25 words, 2 sentences, 1 question.
3. **Inside the screen.**
   - A speech element's box lies 14 px inside the screen (Latch's clamp).
   - Fault: `the speech bubble "…" runs to x 2120 (inside 14 px of the edge)`.
   - Today's `t101-window` fails it, so land it with Latch's must 5.
4. **If Sky picks B, the width.**
   - A one-row bubble is at most 460 px wide at 1080. A wider bubble must be two rows: that's the line two rows of 460 can't hold.
   - Fault: `the speech bubble "…" is one row 712 px wide (a bubble wraps at 460 px)`.
   - Under A there is no width rule.
5. **The verb row's words** (a hovered tab's name and key, or the armed verb's name).
   - One row, at most 3 words plus one key, one at a time.
   - Until the `hover` kind lands, they sit inside the "verb row" image element, as the take-control tab's word does. Then they move to `hover`, with the lozenge's line and the next-event and eye tips.
6. **Not this round's, but it fails today:** `seed-fire-alt` reads `the order pill "Hold · Watch the guard" shows in a alt frame (a pill is active only)`. It does so in today's capture and in my mock alike. Either the hold pill shouldn't show while Alt is held, or the proof labels the state wrongly. Latch and Splice to settle.

## For Aster: Keel's Call offer (`briefs/102/keel/call-offer.md`)

1. **Confirmed: absent, not greyed.** The turned guard and the turned driver lose the Call, and Inez keeps hers. That is §03's law for a way that would do nothing. A shut wedge that can never open is noise, and on the guard it gave his lover away before any Listen.
2. **The Fixer's click on a turned contact with nothing to call:** one "Already turned" lock, not none and not four. It is a single shut wedge at 12 o'clock with the `turned` pictogram and Fable's short lock. The four turn ways stay absent by the same law, and the violent ways follow, dashed and last, as E2 has them. A click always answers (E2, Sky: "there are times I couldn't perform action on Inez"), and four identical locks would say one fact four times. No picture needed.

## What's illustrative

- The age order at 4:15: the build knows each line's age.
- The ground under each lifted piece: same-camera captures where they're clean, else the frame's own street. Under the Fixer's line at 4:15, the plaza between the two swept benches is a plausible fill, not a capture.
- The take-control tab, drawn as Latch is building it: it isn't in 0.2.290.
- 2:59 as clamped (Latch's fix 5 isn't in a capture yet), with the Scout's edge face redrawn where the old line hid its top half.
- The fifth state: the Fixer's row with outcome badges, beside an unselected face.

## Evidence

**The project's reader.** `python3 -I make/r4_budget.py` writes each mock's HUD layer and manifest, copies today's captures, and runs `bun tools/hud-budget/cli.ts` on both (`make/budget.txt`, `make/coverage.json`):
- today: 6/7 within budget;
- round 4: 8/9 within budget.

The one fault in each is the seed proof's hold pill (Splice item 6).

**The 2x crop check.** Every new piece, against today, was checked at 2x: the docked line at both edges, the verb row and its five states, the cursor's badge, both wraps and their stems (sheets in `out/scratch/glyph/101r4/`). I fixed:
- a ghost of the Fixer's lifted line's left end on the road at 4:15. The siblings were older builds whose road reads 14 levels darker, so the frame's own street fills it now.
- a red smear under the line's stem, brought in from a sibling. It's now a thin inpaint of the stem's few pixels.
- the balanced break splitting "Miss / Nikolaou" and "across / now,". Breaks after a comma or full stop now win within 40 px.
- Inez's docked line sitting 3 px over the scope's eye button. It now slides up 7 px to 10 px clear.
- on the board, "→" and "⇧" missing from Jost. They're now words, and the key in the hover state is drawn.

**Files.** All under 500 lines; the largest is `make/r4_base.py` at 180.

## Files

| file | what |
|---|---|
| `board.png` | the board for Sky's phone |
| `fight.png`, `inset.png` | item 9 at 4:15 and 2:39 (1920 × 1080) |
| `plate.png`, `seed.png`, `strip.png` | item 10 |
| `line-one.png`, `line-wrap.png`, `call-one.png`, `call-wrap.png` | the wrap: A and B at 4:27 and 2:59 |
| `make/r4_base.py` | the sources, the bubble and its wrap, the docked line, the verb row, the cursor's badge |
| `make/r4_frames.py` | the frames and the row's states |
| `make/r4_budget.py` | the project's reader on today and the mocks |
| `make/r4_board.py`, `make/r4_board_text.py` | the board |
| `make/coverage.json`, `make/budget.txt`, `make/boxes.json` | the measures and where each new piece landed |

To re-render (about 50 s):

```
cd briefs/101/glyph/r4/make && python3 -I r4_budget.py && python3 -I r4_board.py
```

`r4_budget.py` runs `r4_frames.py` first.
