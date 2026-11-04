# Brief 096 · new words for Fable (Latch, 2026-11-04)

Glyph's placeholders from the mocks (`briefs/096/glyph/index.html`), now live in the build. I wrote none of Fable's lines; every word below is new and sits in
`contracts/missions/handover/text.json` → `book`, each block optional in `book.schema.ts`. Change the words freely: the code reads them from these keys only.

| Key | Placeholder | Where it shows |
|---|---|---|
| `book.sections[id=menu].word` | Menu | the book's rail, last sign: shuts the book and opens the pause menu (Sky's call 6) |
| `book.start.begin` | Begin | the start page, bottom right where Back would be |
| `book.start.shared` | Both end at our car: Popovic in · Go · the rest clear before the seal | the line under the two ways; the shared conditions (guarded, go, left-safe) sit once under it |
| `book.start.purse` | Purse | not shown yet (the purse reads as its icon and amount); kept for a label if wanted |
| `book.start.heat` | Heat | beside the Heat word (Unknown, Noticed…) on the fails row |
| `book.start.cell.scout` | Watches from furthest; listens in. | the cell strip under the clock (replaces `briefing.page.cell` on the page; the false tip is gone) |
| `book.start.cell.fixer` | Turns Inez or the driver; calls Inez. | 〃 |
| `book.start.cell.tech` | Loops cameras, moves the patrol, drives our car. | 〃 |
| `book.start.cell.enforcer` | Takes Popovic. Only he can carry him. | 〃 |
| `book.start.stations["5:50"]` | His car leaves | the Job page's last clock station (before `briefing.timeline[].short`) |
| `book.journal.sub` | everything so far, newest first | the Journal's subline |
| `book.journal.new` | {n} new | after the subline while rows are unread |
| `book.journal.begins` | The job begins | the Journal's first row at 0:00 |
| `book.journal.empty` | Nothing yet | read, not shown yet |
| `book.journal.filters[].word` | All · Crucial · Learned · Turned · City | the Journal's filter chips |
| `book.key.word` | Book | the HUD's Book button, bottom right |
| `book.key.stop` | Stop | the fire line's Stop button (replaced the "X stop" keycap) |
| `book.feed.of` | {i} of {n} | the slim feed's count |
| `book.feed.cards.seal` | District sealing · sealed at {seal} | crucial card 1 |
| `book.feed.cards.police` | Police coming · here in {s} | 2 |
| `book.feed.cards.down` | {agent} is down | 3 |
| `book.feed.cards.for-car` | Rivals for our car · {n} rivals; fixes: Step out, draw · Down them | 4 (objective 7) |
| `book.feed.cards.alert` | Alert up (with the three lamps) | 5 |
| `book.feed.cards.seen` | One of ours seen | 6 (Glyph's "Hasti was seen · by the entrance camera" needs who and by what: see "Left" in report.md) |
| `book.feed.cards.target-out` | Popovic is out · official door | 7 |
| `book.feed.cards.rival-car` | Rival car coming · east road | 8 |
| `book.feed.cards.turned` | {who} turned | 9 |
| `book.feed.cards.refused` | {who} refused | 9 |
| `book.feed.cards.body` | Body found | 10 |
| `book.heldGo.heldBy` | held by | Go's pill on the car's ring while rivals hold it, before a badge per holder |
| `book.heldGo.fix` | Step out and draw · or down them | the plate under that pill |

Placeholders: `{agent}` an agent's name, `{who}` a person's, `{n}` a count, `{seal}` and `{s}` times. Titles stay at four words or fewer (Glyph);
the schema caps a title at 32 characters and a card line at 40.

Glyph also proposed two changes to existing lines; they are yours to make or not (I left them as they are):
- `book.job.needs.ways.pickup[id=driver].words`: "The driver is turned" → "The driver is turned by 5:20" (`{t:driverLast}`).
- `book.job.sub`: the hook as a short second line ("to the getaway car, before his file reaches Security").
