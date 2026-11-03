# People panel and guided start: mock notes (brief 085, note 10)

Glyph, 2026-11-02. Mock only: nothing under `unity/Assets`, no GDD or `text.json` edits. All type and sprites are the kit's (Jost Bold, Barlow Medium for speech, `panel_dark`, `plate_dark`, `panel_strip`, kit icons and badges), drawn by `make/` (`python3 board.py`).

Look at `board.png` and `guide-board.png` on the phone first.

## What's there today
`HudToolkit.Intel.cs`: a 280 px panel behind the 103 x 36 px people plate under the clock bar, shut until clicked. It holds three cards with icon slots and rim states (solid learned, dashes told, dots unknown), and a chip with the doer's face and one verb. That's the "useful but understated" panel.

Kept: the rims, the facts, the doer's face and FindPerson. Changed: it's open at the start, the faces are big, the facts have words, and it offers several ways rather than one chip. Dropped: the sort by plan, because putting one person first reads as the right route. Order is now fixed: Inez, guard, driver, Vale.

## The three options

**A · The dossier** (`A-dossier-*`). Everyone open at once, 340 px wide down the right side: face, name, job, three facts, ways. A stranger can read the whole picture in one go, but it's the most reading and the most covered screen. It would fold to names after the first Turn. Hovering a card rings the person on the street.

**B · On the street** (`B-street-*`). No panel. A face tag stands on each person in view, and an arrow tag at the screen's edge stands for anyone off screen. Facts show only on hover (`B-street-hover`), or for 4.2 s on the existing learn chip. It's the most Syndicate and the least reading, but a stranger may not find out there's anything hidden: nothing says "hover me".

**C · Four faces, one card** (`C-rail-*`). Recommended. Four 58 px faces under the clock bar: who matters, at a glance, each with a badge for the free thing to try and three dots for how much is known. One card opens below, with its facts, what's hidden and its ways, and a line ties the card to the person. Clicking a face moves the view to them (`C-rail-click-guard`). A new fact puts a dot on the face.

### Why C
- A stranger's first 30 seconds: four big faces say who matters without reading. The card spells out "Nature ?" and "Secret ?", so it's clear there's more to find, and the struck "Listen" shows what comes first.
- The rail stays cheap (6.4 %) when the card is shut, so it can stay up all game.
- Clicking a face moves the view, so the panel teaches the map, which the frame alone can't. It also carries the guide, one face at a time.
- Risk: the opening card is on Inez only because she's the one in view, which leans towards her. The guide lights all three, and if the guide is skipped the card could start shut. Worth watching in the new-player test.
- A is the fallback if the new player doesn't click a face.

## Budget (`bun tools/hud-budget/cli.ts`, mask = the shot's real HUD layer + the mock's layer)
| shot | state | HUD % | limit | result |
|---|---|---|---|---|
| A opening / learned | rest | 12.0 / 14.7 | 15 | **fails: 4 chips on screen, the cap is 2** |
| B opening / hover / learned | rest / active / rest | 6.0 / 9.1 / 10.6 | 15 / 22 / 15 | pass |
| C rail only, rest | rest | 6.4 | 15 | pass |
| C opening, card open | rest | 9.5 | 15 | pass |
| C learned, card open, no hover | rest | 12.1 | 15 | pass; over the 10 % aim while open |
| C learned, card shut | rest | 9.1 | 15 | pass |
| C learned, hover / click guard | active | 12.8 / 11.9 | 22 | pass |
| guide 1 to 6 | active | 8.8 to 11.5 | 22 | pass |

- A passes only if its cards are tagged `image`, as today's Intel is, which hides its words from the check. Tagged honestly as `chip` it fails. It needs a new allowance in the budget.
- C's open card sits at the `chip` caps (3 facts, 3 verbs, one word a verb). A violent way is an icon with no word.
- The learned masks use `cafe-front-day`'s HUD layer, a near match for that frame. The door frames have no layer of their own, so their figures are close, not exact.

## Where each word comes from
| on screen | source |
|---|---|
| Inez, Guard, Driver, Vale | `names.speakers`, `names.named` |
| Okafor, after Listen | the Scout's line and the scene (`scenes` names); see the mismatch below |
| café waitress, door guard, Vale's driver | `HudToolkit.Intel.cs:193`; councillor from `targets.councillor.about` |
| Told: debt / Told: cash | `Game.cs:362`: `Inez.Suspected`, `Driver.Suspected` from 0:00 |
| Needs, Nature, Secret, each "?" until learned | `Person.KNeed`, `KTemperament`, `KSecret`; the words "Needs" and "Nature" are new, **for Fable** |
| Bureau loan, Fearful, Loves guard | `real-player-overhead.png`'s chip; `needs.records`, `less.intel`, cast `p-cafe-14` |
| Loves Inez, on the guard | `Model.cs:43`: learning the affair sets `Lover.KSecret` |
| Watch, Listen, Ask, Pay, Help, Pressure | `actions.ring-*` |
| Listen and Pay shut on Inez ("Watch first") | `locks.needs-routine`; `ScenariosNotice.cs:172` |
| Help and Pressure shut ("Listen first") | `locks.needs-need` |
| Listen and Pay open on the driver | `ScenariosNotice.cs:173-174` |
| Guard's Watch first; his knock-out | `ScenariosNotice.cs:166` (WM-1); GDD §07 route G |
| Help $20k, 35 s, +1, half mark; Pressure and Pay ok marks | `r085-door-price.png`, the real ring at 0:59 |
| Inside, Out 4:50 | `targets.councillor.about`; `briefing.page.target` |
| Take alive | `briefing.objective` |
| Bubbles in the learned frame | `dialogue`: Inez's rounds line, the Scout's Listen line for a Fearful Inez |

Nothing secret shows before it's learned: the affair and Fearful stay "?" at the opening. One line outside the mock already hints at it: `briefing.hints.front-door` says "Inez and the door guard are close".

## Guided start (`guide-1` to `guide-6`, on C)
After Begin's glide on a first game, the clock waits at 0:00. Each step lights one portrait and that agent speaks. The view goes to where their work is, the player can give a real order, and the plate at bottom right has Next and Skip. Skip is always there, and it never runs again once done or skipped.

**Changed from the draft:** the cell starts bunched by the car, so gliding to each agent shows nothing new. The view goes to their work instead, which also tours the map. Each step teaches one input, and the words are in the goal line's place.

| step | speaker says (draft) | line (≤ 6 words) | input | more than one target |
|---|---|---|---|---|
| 1 | I watch and listen. Hold on someone, I'll learn what they hide. (12) | Hold left on someone. | left hold, done for real | Inez ringed; guard and driver lit on the rail (the hold takes Watch or Listen, whichever is free) |
| 2 | Eyes on. (real ack) | Taken. It starts with the clock. | none | the card shows what's still hidden |
| 3 | I turn people: Ask, Pay, Help or Pressure. Each has a price. (12) | Click someone. See every way. | left click opens the ring | Inez and driver lit; their open ways differ |
| 4 | I loop cameras and move the patrol, from that cabinet. (9) | Click the ground. Tech walks. | left click on the ground | cabinet and camera both ringed |
| 5 | I take Vale, inside or at his car. Only I carry him. (12) | Hold right to fire. Not yet. | right hold, **shown, not done** | door and Vale's car both ringed |
| 6 | Vale's inside until the chime. (real line) | Take Vale alive to the car. (real goal) | Start | any orders given run together |

All of these words are for Fable. Steps 1, 3, 4 and 5 lean on `briefing.page.cell`.

### Choices to check
- **Hold right isn't practised.** A first input that shoots in a calm street raises Alert and costs Standing. It's shown once, then H17 and H21 teach it in the fight. This goes against "show a real input by doing it", on purpose.
- **Step 4 doesn't suggest Loop.** A Loop runs 2:00, so one given at 0:00 is gone before the door matters. It's still on the cabinet's ring with its price. The ring being there is not a push.
- **Step 3 doesn't push Ask.** Ask is the only open way on Inez at 0:00, and a refused Ask is remembered (later offers +$10,000, +1 Trail). The ring's "?" says the outcome is unknown; the step asks to look, not to act.
- **No word about the rivals.** Their schedule is unknown at the start, so the mock doesn't mention it.

### Orders during a stopped clock: yes, already built (read from the code, not run; Keel to confirm)
- `Game.Issue` only demands `o.Tick == Tick` (`GameOrders.cs:16`), and scenarios give several orders at tick 0 (`ScenariosNotice.cs:116`). The clock only advances in `StepOnce`, behind `!Paused` (`HandoverHost.cs:290`).
- The plan queue exists and is on by default: `Settings.planTogether = true`, player option "Orders while paused: Start together". `HostHoldStatus.cs:240-260` holds one order per agent as a ghost (dashed path, face plate) and gives them all on one tick when the pause lifts. Proof row `holdActs plan` covers it; `hold-plan-paused.png` shows it at 0:00.
- `BriefingBegin` already leaves `Paused = true` during the start sequence (`HostBriefing.cs:78`). The guide takes over where the sequence calls `StartClock`, and leaving the guide lifts the pause. `FlushPlan` then gives the orders with the ordinary `IssueCore`, on that tick.
- Replay: the engine sees ordinary orders at tick 0, so a saved run (seed + orders) replays as any other. The guide writes nothing to the engine. With the option off, orders go straight to the engine at tick 0, which is also fine.
- Two things to handle: Space must mean Start during the guide (`HandoverHost.cs:419`), and a plan's one-order-per-agent rule means a second order to the Scout replaces the first. A second step can't queue Watch then Listen, and the mock doesn't ask for it.

No alternative is needed.

### Hints
- **They can't talk over the guide.** `Hints()` runs only from `StepOnce` (`HandoverHost.cs:341`), so nothing shows while the clock is stopped, tips included. The goal line waits too, because `ClockStarted` is set only when the clock starts.
- **After Start:** the goal line shows for 6 s from `ClockStarted`, and hints still need 8 s of mission time without an order, so the two don't collide.
- **Replaced:** H1, by step 1, when the guide ran. If skipped, H1 stays.
- **Retire when its input was really done in the guide:** H3's click, through `Learn()`.
- **Overlap to review after the new-player test:** H2 and H6 repeat the rail's badge and card. Keep them until that test.
- **Unchanged:** H4 to H22. They come later, teach other inputs, or rescue. H4 and H9 are the most route-like and worth a look, but they're outside this job.
- **Reset:** `RelearnHints` should also clear the guide's "done" flag.

## Size to build (size only)
| piece | size | what |
|---|---|---|
| C | M | replaces `HudToolkit.Intel.cs`, reuses its facts and `IntelNext`; line to the person; several ways per person; proof rows; 2x crop review |
| A | M | as C, plus a budget ruling for 4 chips, and folding |
| B | L | world-anchored tags, edge tags at four yaws, overlaps, occlusion, hover cards |
| Guide | M | a new host file for steps and the first-game flag, the view moves, bubbles by the portrait, Next and Skip, input checks per step, proof rows; no engine change |
| Words | S | Fable: `less.intel` labels, guide lines |
| Guard's face | S | none exists: the mock uses the brief 063 model render. Forma paints one (free Codex still), or he stays a pictogram |

Order: C, then the guide, then the new-player test.

## What the mock can't show, and what I assumed
- **No motion:** the glides, the hold's charge and the pulses are stills. Only the real build shows whether a stranger clicks a face.
- **Frames:** `C-rail-opening`, A's and B's openings and `guide-1` stand on a real 0:05 frame, with the clock re-lettered for the guide. The learned frames are a real 0:59 frame with the open ring lifted out using the same view one second later, two bubbles replaced by real lines and small bits painted out. Guide 3, 4 and 5 have the cell painted out, so look for smudges there. Guide 4 and 5 stand on a 1:20 frame, whose guard marks and scope wouldn't be there at 0:00.
- **Estimates:** the walk + act times in the guide (Watch Inez 34 s, Ask 39 s) are mine. The game's own numbers will replace them.
- **Assumed:** clicking a way on a card selects the doer and opens the person's ring, never commits, so the price always shows first. Guard's Listen is shut until his Watch, from WM-1. Inez's fit marks are carried from the real ring after Listen.
- **Only the Handover at day was checked.** Other yaws, night and the fight (where the rail should probably fold) aren't mocked.
- **Colour:** everything is sourced except the ring's lit wedge, matched by eye from the real ring.
- **New asset:** a small mouse glyph in the goal line, drawn for the mock; the kit has none.

## For Aster (mismatches)
1. GDD §07 says the guard and driver are "named in play by their cast names"; `text.json` `names.targets` says "the guard" and "the driver", and the scenes say Okafor and Zhou. The mock uses Guard until the Listen, then Okafor. Fable or Aster to settle.
2. GDD §10 says bubbles are ≤ 10 words, while the budget's `SPEECH_MAX` and Sky's allowance say 12. The mock uses up to 12.
3. If C is picked, §10's "known-people discs" and Intel lines need a new GDD version, and the guide needs a line in §10.
