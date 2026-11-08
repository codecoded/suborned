# 101 · Fable: the HUD's labels, and the keys Sky's rule on numbers retired

Fable, 2026-11-08, on `work/098-living-playbook`. Uncommitted; Aster commits. No Unity code, scene, GDD or engine was touched.

Sky's rule on numbers in play (2026-11-08): no hit chance and no action time in play; time only for key events. Latch's removal (0.2.273, `briefs/101/latch/numbers-in-play.html`) left keys unused. Sky then picked the HUD as a mix of Glyph's round 2: the top right in Street labels' style (`S-*.png`), the bottom right Corner tokens' news lozenge and Book roundel (`T-*.png`).

**In short:**
- **Retired:** `less.odds` (the fire chip, its sum, the limits and the streak note), the lost cancel's "{s} s lost", and "time" from the price grammar (its field, the order and all 23 verbs that had it). Their schema and test cases went with them.
- **Reworded:** `less.price.rule` (it now states Sky's rule), 11 of the 14 price examples (no seconds), and the cancel sentence of `less.done.rule`.
- **Judged:** `actions.mark.does`, `actions.steam.does` and `less.done.gain.wakes` keep their times. Each is a window the act opens in the world, not the act's own length.
- **New:** `less.street.loud`, "Goes loud", the first shot's row.
- **The mix's labels, through the style loop:** a fourth kind, `label`, and one family, `label/hud` (24 items). It ran twice, on `gpt-6.1-sol` through the Codex CLI on Sky's ChatGPT plan, approved by Sky. No paid API.
  - **Guide /10:** accepted 8 → 10 (4 calls, 78,818 tokens). Host logic then failed two lines it had shortened. The hurt-carry line had lost the live health, and the vale-out line the time the police close in. Brief 096's tests read both.
  - **Guide /11** holds those lines by pattern: 10 on the first draft (2 calls, 38,153 tokens).
  - Against `HEAD`, 13 strings changed and 20 were kept.
- **`apply`** ran with the ten-family restore list each time. Only label strings moved.

## Before and after, every key

### Retired

| key | before | now |
|---|---|---|
| `less.odds.chip` | `{pct} % · down in about {s} s` | gone |
| `less.odds.chipBare` | `{pct} %` | gone |
| `less.odds.sum` | `{pct} % to hit: aim {aim}, range {range}, cover {cover}, movement {move}.` | gone |
| `less.odds.limits` | Never under 5 % or over 95 %. | gone |
| `less.odds.streak.head` | Streaks lean your way, on purpose | gone |
| `less.odds.streak.hits` | After 3 hits in a row on one agent, that shooter's next shot misses. | gone |
| `less.odds.streak.misses` | After 3 misses in a row on one target, the next hits, at odds of 20 % or more. | gone |
| `less.odds.rule` | brief 080's fire preview | gone |
| `less.fates` | five `less.odds.*` fates | gone; `less.street` is `label` |
| `less.done.cancel.cost.lost` | `{s} s lost` | gone: a lost cancel shows no cost, as the host already does |
| `less.price.fields.time` | "n s, the walk plus the act (Alt splits them)" | gone |
| `less.price.order` | name, usd, **time**, trail, door, alert, lasts, eye, learns, pace, mark | name, usd, trail, door, alert, lasts, eye, learns, pace, mark |
| `less.price.verbs.*` | "time" in watch, listen, mislead, ask, pay, help, pressure, loop, disable, reroute, enter, take, board, drive-car, talk-down, covering-fire, carry, takedown, drop, stash, restrain, move, run | gone from all 23. Board, Drive, Talk down, Covering fire, Drop, Stash and Restrain now price as their name alone |

### Reworded

| key | before | now |
|---|---|---|
| `less.price.rule` | "… Alt shows every field including the zeros, the walk split from the act, and the rows a card carries today …" | "… Alt shows every field including the zeros, and the rows a card carries today … No time and no chance (brief 101, Sky 2026-11-08: …): a price is money, Trail, Alert risk and what the street will do, and never says how long the walk or the act takes, on the line or on Alt, nor a hit chance. A time stays only where the act opens a window in the world that the player plays to: the door a pressed call clears (`door`), a loop's run (`lasts`), the desk's report after a Disable (in `alert`)." |
| `less.done.rule` (cancel) | "… (lost, aborted, released, staff-report, vale-reports, ends; …), {s} the seconds spent." | "… (aborted, released, staff-report, vale-reports, ends; the rest are free and show no cost, and so is `lost` since brief 101 …)", with no `{s}` |
| `less.price.examples.knockout` | Knock out · 7 s · +4 · ▲1 · 👁 none | Knock out · +4 · ▲1 · 👁 none |
| `…kill` | Kill · 4 s · +8 · ▲2 · 👁 none | Kill · +8 · ▲2 · 👁 none |
| `…ask` | Ask · 20 s · +1 · ✗ | Ask · +1 · ✗ |
| `…pay` | Pay · $90k · 35 s · +2 · ✓ | Pay · $90k · +2 · ✓ |
| `…help` | Help · $40k · 35 s · +1 · ½ | Help · $40k · +1 · ½ |
| `…pressure` | Pressure · 25 s · +3 · ✓ | Pressure · +3 · ✓ |
| `…pressure-inez` | Pressure · 25 s · +3 · Door 1:40 · ✓ | Pressure · +3 · Door 1:40 · ✓ |
| `…loop` | Loop · 20 s · +1 · 2:00 | Loop · +1 · 2:00 |
| `…disable` | Disable · 10 s · +2 · ▲1 in 14 s | Disable · +2 · ▲1 in 14 s |
| `…board` | Board · 4 s | Board |
| `…drive-car` | Drive · 28 s | Drive |

Unchanged examples: `fire` "Fire · +2/hit · ▲≥2", `fire-camera` "Fire · +2 · ▲≥2", `go` "Go · +1".

### Judged and kept, by the rule (a window the act opens keeps its time; the act's own length does not)

| key | words | why it stays |
|---|---|---|
| `actions.mark.does` | Point him out. For 10 s his cover doesn't count, and his next move shows. | The 10 s is the window the Mark opens: the time the player has to fire while his cover doesn't count. It isn't how long the Mark takes. The verb is parked (moves off), so it shows nowhere in play today. |
| `actions.steam.does` | Open a heat main. For 8 s no line passes near the grille. | The same: 8 s of blocked lines that the player times a move to. Parked. |
| `less.done.gain.wakes` | Wakes {s} s | When a knocked-out person wakes is a world event the knockout sets off, and the player acts on it (tie, hide, be gone). Latch kept it on the same grounds, and the takedown card's "Wakes m:ss" row agrees. |

### New, and through the loop (`label/hud`)

| key | where | before | now |
|---|---|---|---|
| `less.street.loud` (new) | the first shot's row | none (the Alt outcome `less.alt.outcome.loud` already said it) | **Goes loud**: kept by the loop. `apply` writes it to both keys, and a test holds them equal |
| `briefing.timeline[2:35].short` | top right, the next key event | Rivals in | **Rivals arrive** |
| `briefing.timeline[5:20].short` | top right | Last chance | **Driver cutoff** |
| `book.feed.cards.hurt-carry.title` | the lozenge | Enforcer badly hurt | **{agent} badly hurt** |
| `book.feed.cards.hurt-carry.line` | the lozenge | {n} health, carrying Popovic: cover him | **{n} health; cover him and Popovic** (guide /11: the live health and Popovic held) |
| `book.feed.cards.hurt.title` | the lozenge | Enforcer badly hurt | **{agent} badly hurt** |
| `book.feed.cards.hurt.line` | the lozenge | {n} health left: get him into cover | **{n} health: get him to cover** |
| `book.feed.cards.for-car.title` | the lozenge | Rivals going for our car | **Rivals nearing our car** (the old title was five words; GDD §10 says four) |
| `book.feed.cards.for-car.line` | the lozenge | {n} rivals | **{n} coming for Popovic** |
| `book.feed.cards.rival-car.line` | the lozenge | east road | **off Orbit Walk** (the place's own name, as the Journal's card says it) |
| `book.feed.cards.vale-out.line` | the lozenge | get the rest clear by {police} | **the rest get clear by {police}** (the glossary's phrase; guide /11: the police time held) |
| `book.feed.cards.lent.line` | the lozenge | Go once the way out is clear | **Go when the way is clear** |
| `book.feed.cards.lookout.line` | the lozenge | follows Popovic's car if it goes | **follows Popovic's car out** |
| `book.feed.cards.boxed.line` | the lozenge | their car is across our way out | **their car across our way** (Glyph's mock words) |

Scored and kept as they are: Boxed in, Chime, Popovic exits, Popovic at car, Close; {agent} is down, Alert up, One of ours seen, Popovic comes out / official door, Rival car coming, {who} turned, {who} refused, Body found, Popovic is out, Popovic's car is ours, Rival lookout posted, Our car boxed in, `{i} of {n}`.

## What the mix draws, word by word

| place | word on the mock | key | state |
|---|---|---|---|
| top right (S) | the clock, the seal's lock and count, lamps, Trail, money, faces | none: numbers and icons | |
| top right (S) | "Chime 5:10", "Rivals in 2:35" | `briefing.timeline[].short` | scored; "Rivals in" before a clock time read as a countdown and is now "Rivals arrive" |
| bottom right (T) | the card's title | `book.feed.cards.*.title`, or the why block's police title | scored (the why titles are the why families') |
| bottom right (T) | its line, " · " and the clock it came at | `book.feed.cards.*.line`, or the why block's short | scored |
| bottom right (T) | the police's arrival on the ticket ("0:42") | the time alone: the lozenge keys it on the `{s}` in `police.line` ("here in {s}") | kept as it is: Latch's lozenge reads that `{s}` |
| bottom right (T) | "1 of n ›" | `book.feed.of` ("›" is drawn) | scored, kept |
| bottom right (T) | the Book roundel | no word | `book.key.word` "Book" is S's, dropped with S's bottom right |
| street (both) | "Fire" by the crosshair | `actions.fire.label` | already the game's |
| street (both) | "Goes loud", then lamps, police badge, lock, Trail | `less.street.loud` | new, scored |
| street (both) | the reporter's "9 s" by its icon | the time alone | a key event's time: no word |

Left out of the family on purpose:
- The police card: its title is the why block's, held by brief 100's tests, and its `{s}` keys the ticket.
- The seal card: never a card since 2026-11-05.
- The seen card's line: HostCrucial clears it.
- The two cards' fix chips: there's no room for them on the lozenge.
- The why block: it has its own families.

## The loop: a fourth kind, `label` (guide /10)

- **Rubric (`short-guide.ts`, `LABEL_10`):**
  - USEFUL: a label names what the player can act on at that spot.
  - TRUE: a word before a clock time names the event, never a countdown.
  - PLAIN: the glossary's words.
  - SHORT: the slot, and Sky's rule (no act's time, no chance, no number of the label's own).
  - NAMES: `{agent}` where the item has it.
  - VOICE: the book's voice, with today's words kept where they hold.
- **Sources** (added at /10 only, so every /1–/9 run rebuilds as it ran):
  - Sky's rule, from `briefs/101-hud-sweep.md`;
  - Glyph's street layer, corners and words, from `briefs/101/glyph/r2/notes.md`.
- **The family (`short-labels.ts`):** each item's place is stated once in the family's context. That took the prompt from 35k to 24k characters.
  - The title limit is 4 words and 24 characters (GDD §10 and today's widest).
  - The line limit is 6 words and 28 characters at the widest fills. That's lean: Glyph's boxed line is 24.
- **Code check:** a label carries no digit outside a fill.
- **`apply`** replaces each string where it stands, by path (`stringSpans`), so the file's layout stays. `less.street.loud` is also written to `less.alt.outcome.loud`. `--restore` works as for the other kinds.
- **Run 1, guide /10:** `runs/style-agent/short/2026-11-08T10-39-59.972Z-label-hud/` (`index.html`).
  - **Draft:** 8/10. Motive had three findings:
    - "Rivals in" reads as a countdown before 2:35.
    - "Last chance" doesn't say whose chance.
    - The vale-out line carried a clock time inside the label.
  - **Revision:** 10/10, no findings.
  - **The third finding was my rubric's fault.** /10 said "a label carries no number of its own", and Motive read the `{police}` fill as one. To fit 28 characters, the draft had also dropped `{n} health` from the hurt-carry line.
  - `bun run hostlogic` caught both: `Book096.TheEnforcerBadlyHurtCarryingPopovicTopsTheQueueUntilHeIsInTheCar` and `Lend096.ValeOutIsTheTopCardThenThePoliceCountdownFlagsTheRestAndEndEndsIt`.
- **Run 2, guide /11:** `runs/style-agent/short/2026-11-08T10-56-21.268Z-label-hud/`. Three changes:
  - A fill is how the game draws a number: a count, the health left, or a key event's time.
  - The lines a host test reads are held by pattern: `{n} health` and Popovic on the hurt-carry line, `{n} health` on hurt, `{police}` on vale-out.
  - The line slot is 32. Latch's lozenge clips a line at 300 px.
  - **Result:** 10/10 on the first draft, no findings. The /10 run still rebuilds as it ran.
- `bun tools/style-agent/cli.ts verify` on run 1: ok, no notes.

## Read before committing

- **"Driver cutoff"** is the one word I'd look at. It's true, and the hover gives "Next · Last chance to turn the driver", but "cutoff" leans formal. "Turn driver by" (14 characters) is the alternative, if Sky prefers an instruction.
- **The two hurt cards now name the Enforcer by his cell name** ("Arruns badly hurt", beside his hurt face), as the down card already did. HostCrucial passes the agent for both cards, so `{agent}` fills.

## Checks

- **`bun test tools/style-agent contracts/missions/handover tools/dialogue tools/names tools/drift-auto`: 351/352.**
  - The one failure is drift-auto's "nothing changed: nothing to declare", on skyline r011 and people r012–r016. `bun tools/drift-auto/cli.ts --check` lists "files changed since HEAD e4ab71b7: none" for both, so they fail at `HEAD` without this round.
  - Dialogue passes. My `text.schema.ts` edit moved its code hash, and so had Keel's `rulebook.schema.ts` and `rulebook.schema098b.ts` since `841e28f7`. `bun tools/drift-auto/cli.ts --objective dialogue --base 841e28f7` found every dialogue run replays as at `841e28f7` (no rule moves) and declared it in `tools/levels/dialogue/drift.json`: 3/3 replay OK.
  - Every saved short run rebuilds from its archive, including both label runs (`cli.ts verify`: ok, no notes).
- **`bun run guard -- bun tools/sim/unity.ts --tree`:** headless 472/472 and range 99/99, with identical replay. The mission text is `d82abc74825d`, the same bytes as the repo's.
- **`bun run guard -- bun tools/game/hostlogic.ts`:** 173/173 on the exported words. After run 1 it was 171/173: the two lines above.
- **`bun run typecheck`:** clean.
- **Not run** (delta rule): Unity, the batch, proofs.

## Open, and for whom

- **Aster:**
  - **The GDD still prices time.** §02 says "every order shows its full price before commit ($, time, T, A risk, …)", and §14 Glyph's hold says "walk + act as one time". Both are v0.40.0's change.
  - **The streak note has nowhere to go.** GDD §06 fairness [S] says the how-to-play tells the streak lean. With `less.odds` gone, no text tells it, and the game has no how-to-play. The words are at `HEAD` (`less.odds.streak`: head, hits, misses) for when one has a place. Glyph proposes the how-to-play.
  - **The dialogue ledger declaration** is mechanical (no rule moves). It also covers Keel's two rulebook schema files, which had moved the hash undeclared since `841e28f7`.
  - **"Driver cutoff"** is the word to check with Sky (see "Read before committing").
- **Latch:**
  - **`less.street.loud`** is the shot row's label (step 6). It's the same words as `less.alt.outcome.loud`, which the host already reads.
  - **The next key event's words changed:** "Rivals arrive" (2:35) and "Driver cutoff" (5:20). `HudToolkit.Clock.cs` reads them, and so do the book's start-page clock and the debrief's Time fold.
  - **The lozenge:**
    - The hurt cards' titles now use `{agent}`. HostCrucial passes the agent, so they fill.
    - Every line fits 32 characters at its widest fill, well inside the 300 px clip.
    - `police.line` "here in {s}" is unchanged, since its `{s}` keys your ticket.
  - **Keys left for you to say when they're unused,** then I retire them:
    - `book.feed.cards.seal`: never raised.
    - `book.feed.cards.seen.line`: cleared by the host.
    - `book.key.word` "Book": the roundel has no word, unless its hover uses it.
    - `less.sealed.live.*`: the seal card's words, once step 2 retires the card, unless the seal label's hover uses them.
- **Glyph:** the for-car and lookout cards' fix chips have no room on a 50 px lozenge. Where do they go?
- **Splice (skyline) and Pulse (people):** the drift-auto failure above.
