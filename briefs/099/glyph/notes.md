# The debrief, told like the book (brief 099)

Glyph, 2026-11-07. Open `board.png` first. It's 1000 px wide, made for a phone, and shows the three concepts one after another. Each gets its win at full width, its fail and one opened number side by side, and a readable crop of that opened explainer.

This is a mock only: nothing changed under `unity/Assets`, in the GDD or in `text.json`, Unity wasn't started, and nothing was committed. The layout is real type drawn by my own renderer (`make/`, built on look4's primitives). No image generator touched the layout.

## What's here

| file | what |
|---|---|
| `board.png` | the three concepts, for Sky's phone |
| `A-front-{win,fail,detail}.png` | A · The front page (**recommended**) |
| `B-clock-{win,fail,detail}.png` | B · The clock |
| `C-people-{win,fail,detail}.png` | C · The people |
| `words.md` | every new word the mocks use, by key, for Fable (140 keys) |
| `runs/*.json` | the six real runs the pages read (trimmed: no checkpoints) |
| `make/` | the renderer: `d_data` (run → facts), `d_reasons` (numbers and their reasons), `d_words`, `d_base` and `d_parts` (the light page and shared pieces), `d_lift` (the end card lifted from the frame), `d_front`/`d_clock`/`d_people` (the concepts), `d_board` |

Each concept shows the same three states, so they compare directly:
- **win**: the quiet win, `route-a`, seed 0. The front door: Inez turned, the loop, her call, a clean fight with the rivals, away at 4:12.
- **fail**: `fail-rivals-reach-vale`, seed 0. The real cause is "Their lookout got there first", at 5:53.
- **detail**: the loud win, `loud.seed3`, with its Trail +27 explainer open. It's the busiest run, so it also stress-tests each layout.

## The three concepts

**A · The front page.** One spread, readable at a glance, built from the start page the player read at 0:00:
- the outcome and its line;
- "your way in", with the start page's own rows (`book.job.needs.ways`) ticked, each with the time it came true and how. The row that lost the run is in orange;
- on a fail, "Next time", plus the two ways that would have dealt with the culprit (Larthi's False tip, Hasti paying him off);
- five numbers, each with its one-line reason and a › to open it;
- three rows on what the city does now;
- the run along the foot, on the book's own track, with the cell's acts shown as their faces.

A number opens as the book's fold. Trail +27 shows a bar of every point (shooting in orange), then what left it, biggest first, with faces and times.

**B · The clock.** The run to scale on one line:
- the cell's acts above it as faces;
- the city and the mission's own stops below it;
- the end as a car (a win) or an orange cross (a fail).

Under the line, each number is a step line on the same clock, so every rise sits under the act that caused it: Trail and Spent rise at 1:34, under Hasti turning Inez. A fail marks the window that lost it: "Nobody dealt with him for 1:54", from the lookout taking his post to the end. Opening a number lights its line and names each rise where it happened, while the rest steps back. B is the calmest page (72 to 97 words) and the best at saying *when*. It's the weakest on the plan and the people, and it reads more like a chart.

**C · The people.** The cell as four cards, each face over its card's edge the way the book's face sits over the curve. Each card shows:
- what that agent did, with hits of shots, downs and hits taken;
- who saw them;
- their health;
- the moment that mattered for them.

Then come the people the run touched, each with what they are to us now (on the win: Inez Turned, the guard Drawn off, the rivals 3 down, 2 people Knows Hasti's face; on the fail: Popovic Taken, the driver Turned, the patrol Saw it). Whoever is already the page's big face is left out of the row. After that, the five numbers in one band, Standing as a Hero ↔ Menace bar, and the investigation. Opening Trail puts each agent's share on their own card: Larthi +10, Hasti +8, Arruns +6, Sethre +2. C is the most "people > powers" of the three, but the plan and the cause read less directly than in A.

## My recommendation: A

1. **It's the start page, answered.** The rows the player read at 0:00 come back with the time each came true. A win shows the plan working. A fail points at the one row that lost it, then "Next time" names the cell's own ways that would have saved it. The player leaves knowing why, and what to try.
2. **Every number is in view with its reason**, with no click needed. Any one of them opens to the full story.
3. **It's the cheapest to build.** Most of its pieces are the start page's own: caps, title, a way, its rows, the track, the orange pill. The explainer is the book's fold.

Two things worth borrowing from the others: C's cards as the Cell tab's page at the end (A has no cell cards), and B's clock as the Journal's view of the run. A's Trail fold already uses B's pip bar.

Risk: A is the wordiest page (154 to 184 words; the book's pages were 19 to 67). If it's too much, the city rows can shrink to one line, or move into the Standing number's explainer.

## What earns its place, and what I left out

1. **The outcome and the story to the end.** All three use today's end card words (Fable's `debrief.cause`, `headline`, `result`).
   - A tells the story as the start page's rows answered. They carry the same facts as `debrief.route` ("Inez drew the guard off the door", and so on), in the words the player has already read.
   - B tells it as the strip; C tells it through the people.
2. **The run in numbers.** I kept five: Time, Spent, Trail, Alert and Standing. These say how close it was, or carry into the campaign. The rest are left out as numbers of their own:
   - shots, hits and hit rate; knockouts and kills: on C's cards and in Trail's explainer. "2 hits of 6" is plainer than "33 %";
   - people turned: shown in A's rows ("The driver is turned by 5:20 · paid"), on C's cards and as C's tags;
   - cameras looped: shown in A's rows and in Trail's explainer;
   - witnesses and reports: told as who saw what (A's city rows, B's city lane, C's people). A bare count with no "who" means little.
3. **The cell's cards.** C has them in full. A and B show the cell as faces on the strip; I'd leave the cards to the book's Cell tab at the end.
4. **The city's answer.**
   - Kept: the investigation and its cause, Standing, who saw what, and between missions (the case on the guard, more watch at the door, the hunt for our car, word of mouth).
   - Left out: the investigators' list of leads (internal ids, mostly "no one to ask", with no words yet).
   - Left out: each contact's new need and introductions ("Inez can introduce 2 café workmates", "debt → secret"). These belong on her People page, for the next mission.
5. **The timeline.** A uses the book's evenly spaced track; B draws it to scale.
6. **Ways on.** Watch the replay · Back to the start · Try again, with a "New start | Same start · seed" choice for brief 080's seeds. All are buttons; nothing is key-only. The seed shows only there.

## Where each number comes from

| on the page | source |
|---|---|
| head and line | win: `debrief.headline.win`, `debrief.result.win`; fail: `debrief.cause[fail][why]` from the run's `cause` (`rivals-reach-vale` / `lookout`), as today's end card |
| "DONE AT" / "LOST AT" | play `tick`; a fail: `cause.clock` |
| your way in | Pickup if `turn.*.turned` names the driver, else Front door |
| each row and its time | log: `call.waitress` or `guard.down` (guard off the door); `loop.on` (camera); `enter.in` (inside); `board.in` with Popovic (getaway); `turn.pay.turned` on the driver; `camera.recorded` on the pickup camera |
| the row that lost it | the fail's `cause` (its head and clock) |
| Next time | new words per cause key; its two ways are the cell's own acts on the lookout (`book.crew`: Mislead, Pay) |
| Time | play `tick`; "to spare" is his car at 5:50 (`book.job.stops`, `vale.at-car`) minus the end |
| Spent | play `cash` subtracted from the $100,000 purse; which act paid comes from `turn.pay.turned` / `turn.help.turned` (the ledger has no cash rows) |
| Trail | play `trail`; ledger rows `what=trail` (tick, delta, cause). The agent comes from the shot at that tick (`gunfire`, `hit`), the last hit before it (`down`), or the order line (`loop`, `reroute`). The law ("+2 fire, +2 hit, +2 fall") is rulebook `gunfire-trail`, `hit-trail`, `down-trail` |
| Alert | ledger rows `what=alert` (the highest `now`; cause `loiter:camera:pickup:fixer`, `gunfire:guard`) |
| Standing | debrief `nextMission.standing` (`from`, `to`, `band`, `cause`, `moves`, including word of mouth); play `standing` (now); ledger `what=standing` (B's steps); the bands are rulebook `hero-at` 3 and `menace-at` −3 |
| investigation | debrief `investigation.state` and `cause`, in Fable's `debrief.investigation` and `debrief.causes` |
| kept | debrief `assets`, in Fable's `debrief.assets` |
| case, more watch, our car hunted | debrief `nextMission.cases`, `areaWatch`, `investigators.hunted`, `carSeenBy` |
| "2 people heard what Hasti looks like" | debrief `nextMission.lines`, `line=word` with `fixer-face` in the detail |
| the strip's beats | log codes `scout.watch.watched`, `scout.listen.heard`, `turn.*.turned`, `loop.on`, `patrol.rerouted`, `call.waitress`, `enter.in`, `enter.out`, `rivals.coming`, `rivals.lookout-posted`, `attention.landed`, `camera.record-lands`, `vale.exits`, `fight.judged` (its span), `report.landed`, `car.seen`; the end is `success` / `fail.*` |
| a card's health | debrief `cell` (`op`, `health`, `max`) |
| a card's hits of shots, downs, hits taken | log `shot.hit` / `shot.miss` by shooter; a down (`fight.down`, `rivals.down`, `guard.down`, `security.officer-down`, `security.down`) goes to the last agent who hit that target |
| seen by | log `run.seen`, `attention.noticed`, `drawn.seen` |
| a card's moment | the agent's own log lines (watched and listened, turned and its cash, loop and reroute, `enter.in`, `order.rejected`) |
| who the run touched | Popovic (the outcome; `enter.out`); the culprit (`cause.culprits` role `reached`; `rivals.lookout-posted`); contacts (`nextMission.contacts` and `assets`); the guard (downs and `cases`; `call.waitress` and `run.seen`); grief (`lines`, `line=grief`); police and rivals down; the patrol (`attention.noticed`); word of mouth (`lines`, `line=word`) |
| Same start · n | play `seed` |

## The runs

- `bun run sim play tools/game/pins/loud.seed3.json` prints the log and the `debrief`, but on a win only.
- So I built Suture's QA host on the working tree into my scratch folder: `dotnet build tools/qa/host -c Release -o out/scratch/glyph/debrief/qa -p:QaKey=scratch-glyph-099`.
- Then I played the scripts with `dotnet out/scratch/glyph/debrief/qa/suborned-qa.dll --list <list> --out <jsonl> --jobs 2`. It prints the same line as `play --json`, plus `Debrief.Of(g)` on every ending, which is how `HandoverHost` builds it.
- Result: 12 scripts played in 4.2 s with 0 errors or replay differences. Light load, two cores, while Sky played.

| run | outcome | end | spent | Trail | Alert | Standing (now → next) |
|---|---|---|---|---|---|---|
| `quiet-win.route-a` (seed 0) | win | 4:12 | $40,000 | 3 | 0 | +3 → +4 Hero |
| `fail-lookout` (seed 0) | rivals reach Popovic: lookout | 5:53 | $70,000 | 3 | 1 | +2 → +2 |
| `loud-win.seed3` | win, loud | 5:01 | $0 | 27 | 2 | −3 → −3 Menace |
| also kept: `quiet-win.route-b`, `fail-sealed`, `fail-vale-dead` | | | | | | |

One more result, not used in the mocks: `route-g` on seed 0 now fails, closed in on foot at 8:10. That's one for Waymark, not for this page.

## What's real in the pictures

- **The streets are real game frames of these places, not frames of these runs.** The sim is headless, and I didn't start Unity while Sky played. In the build, the page would stand on the run's own frozen last frame.
  - Win: `out/batch/shots/getaway-car-yaw210`, a real end of a win at the bay. I lifted today's end card off it with `d_lift.py`: the road copied two lane-dash periods along the road, the kerb and pavement one step back along the kerb. Faint seams remain.
  - Fail: the official car at the pickup, from Latch's in-game book capture `briefs/088/latch/job.png` (taken at 0:05).
  - Loud: `fight-loud-body`, the loud final fight at our car. Its HUD came off with its own `.hud.png` mask, and the seal card was filled from the plaza tiles below it. There are small smudges where speech bubbles were. The fight's dashed ring is drawn on the ground, so it stays.
- **Faces:** the kit's `hero_*` paintings (the cell, Popovic) and brief 063's (Inez, the driver, the guard).
  - The rival lookout is one Codex still at $0: `paid/099-debrief/codex/lookout.png`, ledger row 1, 37 s, one take. No rival has a painting, so it's a stand-in for Forma.
  - People with no painting get the book's bust pictogram.
- **Icons:** all from kit `icons/singles`. Watch the replay uses `loop`, Back to the start uses `papers`: both stand-ins.
- **Colours:** the book's own (ink, soft, deep teal, pale, turquoise, and the café orange it already uses for the current tab and Turned). Three are new: the Standing bar's two zone tints (pale orange, pale teal) and the fold's darker cream (#f0ede2). They're tints of the book's colours, for Latch to source from the style contract.

## Rules kept

- **Clickable.** Every number has a › and opens its explainer, the ways on are buttons, and the tabs are the book's (Cell, People, Job, Journal; at the end the Job tab holds the debrief). Nothing is key-only.
- **Read-only.** The page reads the run (debrief, ledger, log) and gives no orders, so a replay is untouched.
- **Plain words.** All the stand-ins are in `words.md` for Fable.

## Evidence

- **Words per page** (the renderer's own count; win / fail / open): A 154 / 159 / 184, B 89 / 97 / 72, C 163 / 164 / 159.
- **2x crop check.** I checked A's rows and numbers, B's strip and C's cards at 2x, and fixed:
  - a city line clipping A's right edge;
  - B's step words sitting on their lines;
  - overlapping labels on the strip;
  - Arruns' eight health pips running off his card;
  - the title crowding its line.

  The final images were re-checked. When I moved the inline words into `NEW`, A and C re-rendered pixel-identical.
- **Board.** I read `board.png` at full size in three sections, the way a phone shows it. It's 1000 × 5104.
- **Re-render** with `python3 -I briefs/099/glyph/make/d_front.py`, then `d_clock.py`, `d_people.py`, `d_board.py` and `d_words.py`. Files are 27 to 209 lines.

## Open questions and handoffs (via Aster)

- **Keel:**
  1. `sim play --json` writes the debrief on a win only. The page needs it on every ending; the QA host and `HandoverHost` already build it.
  2. The ledger has no cash rows. A row (tick, delta, cause) would make "Spent" exact.
  3. Per-agent shots, hits and downs come from log lines here. The debrief has only totals, and `stats` counts shots both ways.
  4. When each start-page row came true is read from the log here. The book's needs checks (`HostBook.Needs.cs`) could record it.
- **Motive:**
  - Are these the five numbers?
  - Is "to spare" the right measure (against his car at 5:50)?
  - Should Standing show the next mission's value ("+4 next") or the mission's own (+3)? A and C show "Hero +4"; B shows both.
- **Fable:** the 140 keys in `words.md`, including a "Next time" line per fail cause (only the lookout's is written here) and a time reason per cause.
- **Forma:** a painting for a rival (the lookout here is a Codex stand-in).
- **Latch:** build on the book's slots once Sky picks.
- **Chime:** the page's sound.
