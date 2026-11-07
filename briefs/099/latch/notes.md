# Brief 099 · the debrief page: Latch's build notes

Latch, 2026-11-07, on `work/098-living-playbook`. Uncommitted; Aster commits after review.

Sky picked concept A, the front page ("yeah, try A"). It now shows at every ending in place of the old end card. The old card stays built as a fallback: a host with a fault, or the fight range, still shows it.

## How the page is built

**It is the book's own page.** At the end the book opens on its Job sign, in an *end mode*: `HostDebrief.cs`, `BookEnd`.
- It opens once the run has ended and nothing else wants the screen. A fail's camera plays first, as before.
- The book's other signs still work. Cell and Journal open their own pages, and Back there returns to the debrief. Menu opens the pause menu, and so does Esc. When the menu shuts, the page comes back.
- It is built from `BookPage` slots, laid out by the pure `BookLayout`, and drawn by `HudToolkit.Book*`, like the start page and the Journal. The new slots are WayIn, Ticks, Next, Stats, Fold, City and Again. The track now takes faces and a hot stop.
- The page is built when it opens or a fold toggles, never per frame. Building it takes about 1 ms.

**The pieces and where each comes from:**

| on the page | source |
|---|---|
| kicker, head, line | the end card's words: `debrief.headline/result` for a win, the lend's own (`book.cars`), and the cause's words (`debrief.cause`, `HostEndWords.CauseWords`) for a fail or a withdrawal |
| the face | Popovic. A fail shows the culprit only where the kit has their painting. No rival has one yet, so Popovic stands in. |
| the way taken | the way whose own rows came true most; a tie goes to the first. This is general; no engine route kinds are mapped. |
| its rows | `book.job.needs`: the start page's own column for that way, checked with the start page's own vocabulary. `BookCheck` became a static `CheckOn`, so there is one vocabulary. |
| when each row came true | **the first tick its check held.** The host reads it once a tick from `CrucialTrack` as it steps. A run the host did not step from 0:00 is replayed once from its start and orders, a tick at a time: a continued save, or a run adopted headless. |
| how each row came true | the log line of its kind at or just before that tick: `call.waitress`, `guard.down`, `takedown.out/dead`, `loop.on`, `camera.cut`, `fire.camera-dark` and `enter.in`. For a turned contact it is the person line, "(driver)", and its approach. A camera row that never held, but whose camera taped an act, says "it taped the pay" with the ledger's `recorded:…:camera:<id>` tick. |
| the row a fail lost | the first row that never came true and names Popovic's state (`vale:`), else the first that never came true. It carries the cause's head and the end's clock. The rows after it are spare: they give way first on a full page. A withdrawal loses no row. |
| next time | `debrief.page.next.by[fail][key]`, keyed as the end card keys its cause. Its chips are the crucial card's own fixes for that cause (`book.feed.cards.lookout`: Mislead, Pay, Take, each with its agent). |
| Time | the end tick. A win's spare time is measured against the site's `valeAtCar` (5:50); a fail's reason is `next.by[..].time`. |
| Spent | the start's cash less the cash at the end (exact). What it went on comes from the turns that landed (the person lines `turn.pay/help.turned`) and from crew pay (the ledger's `pay:<id>`). |
| Trail | `Game.Ledger`, what "trail", every row (`HostDebrief.Trail.cs`) |
| Alert | the highest level the ledger reached. Each rise is worded by brief 100's live why row; a run the host did not step gets the same words built at the end, which may say less. |
| Standing | the debrief's `nextMission.standing` line: band and `to`, and its cause's first sentence. The fold shows every move. |
| the city | the debrief: a capture (`leftBehind`, captured only), the investigation and its cause, cases, more watch, the hunt for our car, who we kept, and word of mouth (with the Fixer's face where a teller describes it). |
| the run | the first log line of each kind (a fight each time one starts), the first Alert rise, then the end. Past eight, the quiet city beats give way first. The cell's acts show as their agent's face, from the order that gave them. |

**The fold.** A click on a number opens its fold under the numbers, as the book's fold does. The rest of the page steps back under a veil; a click on the veil, or the same number again, shuts it.
- It opens *over* the numbers, with its tail pointing down, only when more of its rows fit there.
- Opening it moves nothing above it. The page is always laid out as if shut, and the fold takes the city's and the run's place.
- When rows don't fit, the biggest stay. For Trail, Alert and Standing the last row sums the rest ("+3 Smaller things"). A row marked Keep always shows: the Time fold's end.
- Trail: what it means, a bar of every point (the shooting in orange), and the rows biggest first. A shooting is one row per target group, its shooters' faces and its span ("Hasti and Larthi shot 3 rivals 4:06–4:20"); each act is one row. The law comes last, with the rulebook's own numbers.
- Time: the mission's stops and the end. Spent: each payment. Alert: each rise with its why. Standing: each move.

**Fitting the page.** If the closed page would reach the ways on, it gives way in order: first the spare rows from the last, then the rows closer together and smaller. The city drops before the run does. Any line too long for its slot ends with an ellipsis rather than run into its neighbour.

**The ways on, all clicks:**
- **Watch the replay** shows only where there is a replay to watch: a fail whose culprit is placed, through the fail camera. When it has played, the page comes back.
- **Back to the start** is the main menu, the pause menu's own Quit to menu.
- **Try again** comes with **New start | Same start · seed**. The same start keeps the seed and the dial's counts (`BeginSameSeed`); a new start rolls.

## The words

There are no words in code. Each is read by key (`HostDebrief.Words.cs`):
- first from text.json's `debrief.page`, in Fable's schema (`contracts/missions/handover/debrief.schema.ts`, still uncommitted);
- for a key that block doesn't have yet, from `unity/Assets/Suborned/Resources/Debrief099/standins.json`. This file is clearly marked as stand-ins: Glyph's words in Fable's shape, plus a few fill-ins listed in its note.

Each key switches to Fable's the moment it exists. A key in neither, or a fill left unfilled, shows nothing rather than half a line. The proof prints which keys still come from the stand-ins.

Keys the page reads that Fable's schema doesn't have yet:
- `head.withdraw` (a withdrawal's kicker);
- `beats.withdrew`;
- `folds.{time,cash,alert,standing}.{head,means}` (the four other folds' head words);
- `folds.rest`;
- `and` (the list joiner);
- `missions.one/many`;
- `how.killed`.

## Captures (beside Glyph's concept)

These are 1920 x 1080, from the real player: scratch build 0.2.263-scratch, `out/scratch/latch/099/player`, run with `-debriefShots`. They sit in `briefs/099/latch/shots/`; the PNGs are git-ignored, while the manifests and the text files ship.

| capture | beside |
|---|---|
| `debrief-quiet-win.png` (route A, 4:12) | `briefs/099/glyph/A-front-win.png` |
| `debrief-fail-lookout.png` (the lookout, 5:53) | `A-front-fail.png` |
| `debrief-fail-lookout-time.png` (its Time fold, opened over the numbers) | |
| `debrief-loud.png`, `debrief-loud-trail.png` (the loud win, Trail opened) | `A-front-detail.png` |
| `debrief-withdraw.png` (a withdrawal at 0:30) | |

- `pages.txt` holds every page's words as built.
- `audit.txt` holds each picture's layout audit.
- `replay.txt` holds the replays and the ways on.

- **Words**: the page read 95 keys from the stand-ins and none from Fable's block, which hasn't landed (`evidence/hostlogic/words.txt` lists them).
- `evidence/` keeps the text of the last run, committed: the audits, the replays and the ways on, the page words, the manifests, and the budget's verdicts.

## Checks

- **Host logic: 167/167** (157 before, plus 10 in `tools/game/hostlogic/HostLogic.Debrief099.cs`). The ten:
  - the quiet win is the start page answered;
  - the lookout fail marks the row it lost and says what to do next time;
  - the loud win's Trail fold names who left every point, and its rows and pips add up to the Trail;
  - every number's fold opens and fits, on three runs;
  - opening a fold moves nothing above it;
  - **every route script ends on its own debrief**: 42 runs (the wins, each fail cause, the withdrawals, the final fights), each fitting the grid, nothing half-filled, five numbers, one lost row on a fail;
  - the page reads only: the state hash, the ledger, the log and the orders are unchanged, and the saved run replays to the same hash;
  - a run the host didn't step gets the same rows from a replay;
  - Try again on the same start keeps the seed;
  - every word comes from Fable's block or the marked stand-ins.
- **Typecheck clean.** The TS tests of the files touched (the proof map, the proof set, the host-logic runner) pass 98/98.
- **Unity**: a scratch player built with 0 errors (0.2.263-scratch; my files add no warnings).
- **In the player** (`replay.txt`):
  - all three runs replay identical, and each saved session still replays to the same hash after the page was opened and every fold toggled;
  - Watch the replay plays the fail camera, then the page comes back;
  - Try again on the same start gives seed 3 again, a new run at 0:00, and the start book on Job;
  - Back to the start brings up the main menu and shuts the book.
- **Layout audit: clean on all six pictures.** That is the HUD's own audit (`HudToolkit.AuditText`) plus the page's: no two words' ink over each other, every word on the screen, and the fold over the veiled page by design.
- **HUD budget** (`tools/hud-budget`, state `page`; the book had no budget state until now): **FAIL on all six**, as expected for a book page. See `evidence/budget.txt`.
  - The mask comes from each page's own manifest: every word, picture, disc and pip, plus the cream page as paper.
  - What remains besides the rules below: two-line run labels whose line boxes overlap (their ink is apart, and the page's ink audit is clean), and the fold row's two faces overlapping by design.
  - The word cap holds: 140 to 182 words against 220.
  - The rest is the 070 briefing page's own parts, which this page doesn't have: objective, hook, Heat, its clock and its ways.
  - Also flagged: the street showing (the light page shows it by design), and the book grid's margins. The kicker sits 3 px inside the 48 px frame, the ways on and the clock chip are Glyph's positions, and the signs' words are 2 px from their icons.

## Open issues

**Glyph**
- **Watch the replay on a win.** The game has no replay to watch on a win; the fail camera needs a culprit. So a win shows no Watch the replay. Do we want a win replay, and of what?
- **The book has never had a budget state.** The page rules (`page.ts`) are the 070 briefing page's, so a book page fails the parts it doesn't have, the 48 px frame against the book's grid, and the street showing. A `book` state with the book's own checks would make the budget mean something here.
- **The fold's room.** On the front door's five rows, the Trail fold shows its two biggest rows and sums the rest ("+3 Smaller things"). Your mock merged an agent's small acts into one row ("Sethre looped … and sent the patrol off"); that needs words written for the join, not a join in code.
- **A fail's face is Popovic** until a rival painting exists (Forma). The culprit's own painting shows where the kit has one.

**Fable**
- **Keys the page reads that the schema lacks** (listed under The words). The stand-ins hold them for now.
- **The stand-ins are Glyph's words.** A few were written to fit the data: "Helping {who}" and "Paying {who}", because the money can go to Inez, the driver or a rival; "Smaller things"; "Pulled out".
- **The rows' "how" and "never"** are read by key; `how.killed` is new.

**Keel (after law L9)**
- **Cash rows.** Spent is exact, but where it went is read from the turn lines and the crew pay. A fold shows an amount only when one payment is the whole sum.
- **A shot's shooter** is read from the shot lines, as `Scenarios.Shots` reads them, at the ledger row's own tick. A shooter on the ledger row would end the text read.
- **When each start-page row came true.** The host reads it each tick, or from one replay for a run it didn't step (a continued save replays at its end, about 0.3 to 1 s).
- **A turned contact's approach** is read from "(role)" in the person line.

**Motive**
- Standing shows the next mission's value and band ("Hero +4") with the run's own cause, as in A.
- "To spare" is measured against his car at 5:50, as in Glyph's notes.

## Files

**New**
- `HostDebrief.cs`: the end mode and the page;
- `HostDebrief.Words.cs`, `HostDebrief.Rows.cs`, `HostDebrief.Numbers.cs`, `HostDebrief.Trail.cs`, `HostDebrief.Run.cs`;
- `BookLayout.Debrief.cs`;
- `HudToolkit.BookDebrief.cs`, `HudToolkit.BookFold.cs`, `HudToolkit.BookAudit.cs`;
- `DebriefShots.cs`;
- `Resources/Debrief099/standins.json`;
- `tools/game/hostlogic/HostLogic.Debrief099.cs`.

**Changed**
- `Book.cs`: the slots and their data;
- `BookLayout.cs`: the name's fit, the debrief's line size, the run and the ways on;
- `HudToolkit.Book.cs`: the slots' drawing, the veil, no Back on the debrief;
- `HudToolkit.BookStart.cs`: the orange kicker;
- `HostBook.cs`: the end mode's open and keys;
- `HostBook.Needs.cs`: the static `CheckOn`;
- `HostBook.Pages.cs`: Job is the debrief at the end;
- `HostBook.Start.cs`: the menu shuts the debrief;
- `HostCrucial.cs`: the per-tick read and the reset;
- `HudToolkit.cs`: one line, the old card hidden while the debrief is the end;
- `tools/game/proofmap.json`: one row for the debrief's files, one for its capture runner.

Every new file is under 170 lines; `HudToolkit.cs` stays at 1300 lines (an in-place edit).
