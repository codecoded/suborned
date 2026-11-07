# The HUD sweep (brief 101)

Glyph, 2026-11-07. Open `board.png` first. It's 1000 × 9322, made for a phone, and runs: today, then the three directions, then my pick. Sky's words, on the seal card: "really hate this card and design. Lot of our HUD UI has become lazy looking. Do a sweep to see how we can massively improve it".

This is a mock only. Nothing changed under `unity/Assets`, in the GDD or in `text.json`. Unity wasn't started and nothing was committed. Two free Codex stills were made for mood.

## What's here

| file | what |
|---|---|
| `board.png` | the whole sweep, for Sky's phone |
| `A-page-{loud,quiet,aim,rest}.png` | A · The page's edge (**my pick**) |
| `B-sign-{loud,quiet,aim,rest}.png` | B · Enamel signs |
| `C-street-{loud,quiet,aim,rest}.png` | C · In the street |
| `crops/` | each direction's six moments at 2x (`A-seal.png` …), and today's six (`today-*.png`) |
| `make/` | the renderer: `h_base` (frames, faces, data), `h_page`, `h_sign` and `h_street` (the directions), `h_crops`, `h_rest` (coverage, writes `coverage.json`), `h_board` and `h_board_text` (the board) |
| `paid/101-hud/codex/B-enamel.png`, `C-street.png` | the two mood stills, $0, ledger rows 1 and 2 |

Re-render: `cd briefs/101/glyph/make && python3 -I h_rest.py && python3 -I h_crops.py && python3 -I h_board.py` (about 40 s). `h_rest.py` renders all three directions, then measures them.

Each direction is drawn on the same three real frames with today's HUD lifted off. Between them they cover the six moments:
- **loud**: `loud.seed3` at 2:34, from Latch's `why-loud-police` capture (brief 100). The Enforcer shot the guard at the Office door at 2:33. The seal runs to 5:41 and police are coming, "shots heard at the Office door", on the street at 3:16. This frame shows the seal warning, police coming with its why, and the top bar mid-mission.
- **quiet**: `route-g-radio` at 3:28, from Latch's `why-quiet-radio`. Another guard radios in the guard lying at the Office door; it lands in 4 s and raises the Alert to 1. The feed card is "Our car boxed in". This frame shows the reporter's countdown, a feed card and the top bar at rest.
- **aim**: the Enforcer about to fire on the guard: 5 m, 75 %, down in about 4 s (`loud.seed3`, 2:31, the shot's own odds of 750 ‰). Going loud means Alert 2, police in 0:45, the seal at 5:41 and Trail +6 (+2 fire, +2 hit, +2 down, the run's ledger). The street is the batch's `fight-loud-checkpoint`, which shows the same door and distance.

## The inventory: every HUD piece in play

The common fault is **one frame for everything**: brief 044's "Chrome plate", a chrome-rimmed deep-teal nine-slice with screws and small cream type, on every piece. With one frame and one size, a sentence, a number and a warning all weigh the same, so nothing leads. The dark plates also fight the bright street and the book's cream page, which is the look Sky picked three times (the book, the start page, debrief A). Small isn't the cure: brief 093 made the seal big because Sky missed the small lock badge. The cure is big where it matters, and designed.

| piece | what's lazy | what works | verdict |
|---|---|---|---|
| **Seal card** (420 × 104, top centre) | Three lines of small text on the generic plate. "District seals in 3:07" sets the number at sentence size. The 2 px bar says nothing. "Get Popovic into the car and leave." repeats the job, which Sky called out on the objective pill. Two sentences in play break §10's rule. | It's persistent and can't be missed (brief 093's point). The amber last 30 s. | Rebuild: the number huge, the cause and the job as pictures |
| **Top bar** (clock, next mark, $, Trail, Alert, pause) | Six equal tokens on a chrome pill with hairline dividers, like a spreadsheet row. Alert, the number that matters most in trouble, is three 9 px dots beside the pause. ▶▶ and "?" before the next time mean nothing without a key. | One place for state. Tabular figures. | Rebuild: hierarchy (clock, then Alert), the next mark named |
| **Known people** (under the bar) | Four 14 px pictograms on a second small plate. They don't read as people and don't look clickable. | They're findable with a click. | Faces, bigger, grey until known |
| **Roster** | The painted faces, the best art in the HUD, sit in a chrome tray with screws. Health is 8 px dots, there are no names, and the busy chip (bar, icon, "3 s") is one more dark plate sticking out. | Live portraits, a fixed place, the sidearm mark. | Keep the faces, lose the tray; add names, a busy arc and readable health |
| **Squad button** | A dark plate with a word. | Clear. | Round tab |
| **Plot scope and its buttons** | The map is low contrast. The three rim buttons are identical 30 px chrome coins, so the new eye differs only by a 12 px icon. | The period-instrument idea; quiet. | Keep the idea, restyle; the eye as its own tab |
| **Feed card** (crucial card) | The same plate and size for every kind of news, so police and a boxed-in car look alike. The why (brief 100) is a grey 13 px run-on: "→ shots heard at the Office door · here in ? · 2:33", with a literal "?" where the seconds should be. "1 of 2 ›" is tiny. | One card at a time, clickable, a short title. | Rebuild: badge large, the why its own line, the number big |
| **Why meter** (brief 100) | Grey two-line rows on a dark drawer that covers the street. It reads as a log. | It answers Sky's question, on demand, and a row pans to its place. | Restyle as a page fold |
| **Hold / aim pill** | One line of equal tokens: "Let go · Fire · 75 % · Alert ≥2". The chance, the one number to decide on, sits mid-line at body size, and "Alert ≥2" is jargon. Today's fix hid the breakdown (Sky: "what's with all this text", "even then it's ugly"), but the pill itself is still a sentence. | The hold mechanic, the sweep, the shooter's face. | Rebuild: the chance huge, the cost of going loud as icons |
| **Act ring and its price plates** | The wedges are fine. Each wedge's plate is a row of symbols ("Fire draws 1.0 s ⩘+2 ⚠≥2 !") that needs a legend. | Wedges, icons, the turquoise selected wedge. | Keep the ring, restyle the plates |
| **Speech bubbles** | Right place, too small: 13 px Barlow on a thin dark pill, with a letter in a circle ("T") where the speaker's face should be. | Placed at the speaker, short, timed. | The speaker's face, a bigger cream bubble |
| **Refusal and result pops** | Readable, but the same plate. Two at once ("Camera on", "Patrol back") look like debug labels. | Short and on the spot. | Restyle |
| **Reporter's countdown** (brief 100) | The most urgent thing on screen (stop him in 4 s or the Alert rises) is a 26 px plate: an icon and "4 s", no face. Its meaning is on hover only. | It sits on the person, and the act ring means you can still stop him. | Rebuild: his face, a draining ring, the seconds big |
| **Window markers and count badges** | "⩘ 46" floats in the street: 46 what? The loop's seconds, with no face or unit. The "taking aim" tag is lowercase 11 px. | The new eye-and-count badge (cones round) is clear. | Faces or icons with units |
| **World markers** (door, check-in pip, lock reticle, edge cameos) | Consistent, but small dark stickers on a bright street. 24 px edge cameos. | Shape language is consistent. | Restyle in the new voice |
| **Attention callout** | Its close-up face inset is drawn as one more chrome-rimmed disc. | The idea: the watcher's face as it notices. | Keep the idea |
| **Book key** | A dark plate with a book icon and the word, beside a book that's a cream page with round tabs. | Clear, clickable, with the unread dot. | The book's own round tab |
| **Dial panel** (dev) | The one dark slab on the light start page. | A dev tool: plain is fine. | Low priority; cream fold if it stays |
| **End card → debrief** | Today's one-line beat plate ("Popovic is in the car.") is being retired by debrief A. | | The HUD should lead into that page, not look like another game |

Captures read: Latch's brief 100 shots (`briefs/100/latch/shots/`), the cones round (`out/scratch/latch/cones100/after` and `proof`), the batch (`out/batch/shots`), and the dial (`out/scratch/latch/098j/dial`). Each was checked with its `.manifest.json` and `.parts.json`, and cropped at 2x (`out/scratch/glyph/101/crops/`).

## The three directions

**A · The page's edge** (`A-page-loud.png`). The book's cream page peeks into play at the right-hand corners, cut by its big curve with the turquoise halo:
- The top corner holds the clock, the pause as a round tab, Alert as three lamps, Trail, cash and the next stop by name ("Rivals in 2:35"). The key people's faces sit on the curve, grey until known.
- The bottom corner holds the one crucial card. Its badge sits on the curve, breaking it, with the title beside it, the why and its time on one line, the number big ("here in 0:42"), "1 of 2 ›" and the Book tab.
- When something matters, the corner grows. The seal turns the halo orange, sets 3:07 huge under the clock with "seals 5:41", shows its cause as one icon, and draws the job as Popovic ···· car (Go would sit on the car once he's aboard).
- The roster is the painted faces with name tags and an orange busy arc. The scope is the real map reprinted in the book's inks.
- The reporter is his face on a stem, a draining orange ring and "4 s" in an orange pill; hover shows what he's doing, "Stop them before it lands", and then Alert 1 and police.
- The aim is a cream slip: the shooter's face breaking its edge, "Fire · 5 m", **75%** huge, "down in about 4 s", then the going-loud row (Alert 2, police 0:45, the seal 5:41, Trail +6) and a "Goes loud" tag.
- Pressing Book would grow the corner into the full page.

**B · Enamel signs** (`B-sign-loud.png`, mood `paid/101-hud/codex/B-enamel.png`). The HUD is the city's own signage: flat vitreous enamel in the book's deep teal with a cream inline, the start page's Front door and Pickup plates and the book's tabs, cut as Googie roundels and lozenges.
- The clock is a station roundel, and its lozenge tail carries the numbers, with the key people hung under it.
- The seal hangs from the clock as an orange roundel (lock, 3:07, "seals 5:41"), with its cause and the job on a small lozenge.
- The card is a lozenge with an orange badge roundel overlapping its end. Numbers sit on cream price tickets.
- There's no chrome, no screws and no gradients; a crisp shadow stands each sign off the street.

**C · In the street** (`C-street-loud.png`, mood `paid/101-hud/codex/C-street.png`). Almost nothing sits in the corners. The city is labelled where things happen, like a planner's drawing: ink labels with a cream halo on the street, faces on stems, thin lines from a cause to its place, and rings on the ground.
- The seal is the district closing in: an orange edge round the whole view, with its count in a tab in the top edge and the job under it.
- Police coming docks at the edge they come from, "0:42 Police coming", with a dotted line back to "shots heard 2:33" at the Office door.
- The reporter's ring on the ground drains orange under his feet.
- The hit chance is a gauge on the target's ring: 75 % of the ring lit, the number beside it, and the going-loud row by the shooter.

Room on screen, measured the way `tools/hud-budget` reads a capture: the HUD layer alone, alpha ≥ 24, so shadows and halos count. The data is in `make/coverage.json`.

| | at rest | loud (seal + police card) | quiet (reporter + card) | aim |
|---|---|---|---|---|
| today | 4.8 % | 8.8 % | 6.6 % | 5.0 % |
| A | 7.5 % | 14.4 % | 12.2 % | 10.6 % |
| B | 5.4 % | 9.3 % | 9.3 % | 8.7 % |
| C | 2.9 % | 10.9 % | 4.5 % | 4.0 % |

The limit is 15 % at rest (aim under 10 %), and 22 % with the ring or an order card open (GDD §10). A's busiest frame, the seal and a police card at once, is under it at 14.4 %. A at rest (7.5 %) is under the 10 % aim but above today's 4.8 %, because its corners are page, not plates.

## My recommendation: A, with three street marks from C

1. **It makes the HUD the same object as the book**, the start page and debrief A, all of which Sky picked. Play, the book and the debrief read as one designed game, which is the brief's objective. The corner growing into the book's page is the bridge.
2. **It fixes the hierarchy, not just the style.** Each moment gets one big number (3:07, 0:42, 75 %, 4 s), with the words small beside it. That's what was missing from every piece today.
3. **It's the most readable of the three.** Ink on cream has the best contrast at play zoom. The corners never move, so the middle of the street stays clear.
4. **From C, take the marks that belong on a person, not in a corner:** the reporter's draining ring at his feet, the chance as a gauge on the target's ring, and the why's dotted line to its place. Don't take C's orange screen edge: A's orange corner already carries the seal, and two alarms at once is too much.

B is the fallback if A's cream corners prove too heavy in a real capture: it keeps today's layout and teal and fixes the shapes. Its risk is that it reads as today's HUD recoloured. C on its own is the most Syndicate and the leanest, but its labels depend on what's underneath (look at 3:28 over the palm in `crops/C-top.png`), and readable beats rich.

## What building A costs

Latch builds it piece by piece, each one shot and checked at 2x on its own. The sizes are my estimate for Latch to check.

| step | piece | files today | size |
|---|---|---|---|
| 0 | Measure the HUD alone on the Air at play zoom, HUD on and off. Its cost has never been split (brief 086, Redline: "Not split"). | | S |
| 1 | Kit: the corner (a Painter2D shape, or a sprite with its halo baked), round tabs, lamps, ink and cream text styles, and the book's colours (page cream, ink, deep teal, orange, hairline) in `kit.uss`, sourced from the book | `kit.uss`, `HudTheme.tss`, sprites | S |
| 2 | **The top corner and the seal first** (Sky's pain point): clock, numbers, key people; it grows for the seal; the job as Popovic ···· car; Go on the car once he's aboard (a click, as on the car's ring). This retires the 420 × 104 card and the status pill. | `HudToolkit.Seal.cs` (73 lines), the status pill in `HudToolkit.cs` (1300), the key people in `HudToolkit.Less.cs` (1847) | M |
| 3 | The bottom corner: the crucial card (badge on the curve, title, why, big number, 1 of n) and the Book tab | `HudToolkit.Crucial.cs`, `HudToolkit.BookKey.cs` | M |
| 4 | The reporter: face on a stem, the draining ring at his feet (C), the seconds, the hover slip | `HudToolkit.OnWay.cs`, `HudToolkit.Ground.cs` | S |
| 5 | Aim and price: the chance big on a slip, down-in, the going-loud row; the gauge on the target's ring (C) | `HudToolkit.Hold.cs`, `HudToolkit.Fire.cs`, `HostOdds.cs` | M |
| 6 | Roster: names, the busy arc, readable health; the live portraits' states and cues stay | `HudToolkit.Roster.cs`, `HudToolkit.Live.cs` | M |
| 7 | Plot scope: the map in the book's inks, round tabs (turn, turn, eye) | `HudToolkit.Scope.cs` | S |
| 8 | The rest in the same voice: bubbles with the speaker's face, pops, window markers and badges, the ring's price plates, the why meter as a page fold, edge cameos, the callout | `HudToolkit.Less.cs`, `Ring.cs`, `Why.cs`, `Attention.cs`, `CuesEdge.cs` | M |

`HudToolkit.cs` (1300 lines) and `HudToolkit.Less.cs` (1847) are already over the 500-line rule. Each piece moves into its own file as it's restyled (brief 084).

**What stays:**
- every position on the GDD's HUD map, every mechanic and every click;
- the signal colours and the cones (red, amber, violet, unlit and emissive);
- the painted faces, Jost and the kit's `icons/singles`;
- the ring's wedges and the turquoise act ring;
- Fable's keys: the long lines move to hover and the Journal;
- the HUD budget and the 2x crop check.

**The GDD:** §15's HUD kit ("Chrome plate", nine-slice frames, the colour list, "brass as a hairline") and the art bible's HUD block change once Sky has seen a real capture. That's a design change, so a new GDD version, discussed with Sky first.

## On the Air (60 fps at play zoom)

No direction adds a full-screen pass or a blur at run time. Every shape is a sprite or a vector mesh rebuilt only when it changes, with its halo and shadow baked in.
- **GPU:** a cream corner covers 3 to 6 % of the screen in one blended layer, a tiny fraction of a millisecond on the M4's GPU. That's an estimate, not a measurement.
- **Main thread:** the cost to watch is text rebuilt every frame. The rule is to set a label only when its value changes (the clock once a second, seconds counters once a second).
- **C:** its labels and rings move with the world every frame, a dozen at most: cheap, but not free.
- **A's corner growing** (the seal, Book) is a short scale or reshape. If it's a Painter2D shape, regenerate it only during the grow.

Step 0 measures all this before anything is built. A real per-piece number needs Latch's profiler markers on the Air.

## What's real in the pictures

- **Streets.** These are real game frames with today's HUD lifted off by its own `.hud.png` mask. Each piece is filled with the nearby street that best continues its surroundings, found by matching the ring around it. The seal card and the police card were replaced by the same view a moment earlier (the brief 100 clip, frame 0 at 2:32, the same camera with no card yet). The old clock bar's corner is filled along its kerb. Small seams remain under the old roster and scope, where every direction draws its own pieces over them.
- **Data.** Each number is the run's own: the capture's clock, cash, Trail and Alert; the seal's 5:41 and the unit at 3:16 from the run's log; the odds, Trail and timings from `loud.seed3`'s log and ledger. "here in 0:42" is 3:16 minus 2:34. Today's card shows "here in ?" because the host counts only a unit already moving (`RespSoonestS` in `HostCrucial.cs`), and this one isn't on the street until 3:16.
- **Illustrative:**
  - which key people are known (Inez, the guard and Popovic known, the driver not);
  - the two running agents' seconds in the aim frame (4 s).
- **Words.** Fable's `why` block (titles, shorts, the on-way plate, fix and nofix), the feed's card titles and Fable's `less.odds` wording are used as they are. New labels are listed below.
- **Faces and icons.** Faces are the kit's hero paintings (the cell, Popovic) and brief 063's (Inez, the driver, the guard). Icons are the kit's `icons/singles` and `badges/signal_police`.
- **Colours.** The book's own: page cream #f9f7ee, ink #17302f, soft, deep teal #1f6c67, pale, turquoise #60e0ea and the café orange #e8622c, already used on the book, the start page and debrief A. Amber #f0a020 is the guard's signal (on the target's ring and face). B adds one tint, a lighter cream-teal for second lines on enamel.
- **Mood stills.** Two Codex stills at $0 (`paid/101-hud/ledger.jsonl` rows 1 and 2, one take each):
  - B's is the material, not a layout.
  - C's figures wear period dress: that's the generator, not our people (§15). The still is only for the annotation idea.
  - The helper updated its rollup, `tools/paid/spend.json` (uncommitted, for Aster).

## Evidence

- **2x crop check** of all eighteen moments (`out/scratch/glyph/101/check-{A,B,C}.png`). I fixed:
  - A's "here in" touching the curve;
  - A's small card line running under the Book tab, and its start outside the curve;
  - B's seal caption running past its inline;
  - B's aim sign over the clock's tail;
  - C's Alert lamps overlapping their icon.
- **The board:** I read it end to end at full size in four pieces, the way a phone scrolls.
- **Coverage:** `make/coverage.json`, method above.
- **Files:** all under 500 lines (the largest is `h_street.py`).

## Captures I didn't have (for Aster)

1. The going-loud card, with its held commit.
2. The police countdown after Popovic is out ("Police close in", End now): the seal's successor in the same corner.
3. The getaway's Go card with seat pips, and a blocked Go.
4. The rivals' tally under the clock.
5. The why meter pinned, with a row hovered (its sentence).
6. Any HUD state at night. Only the café's night rest exists, and A's cream corners need a night check.
7. The Air's fullscreen 2880 × 1864. Every capture is 1920 × 1080, and the corners should scale with the panel.
8. The ground (follow) view with a card and the seal.
9. The roster under fire, hurt and downed in one frame.
10. A refusal pop at the cursor ("Too far · 4 m").

## Words for Fable

New short labels in the mocks, all stand-ins:
- "here in" (the police card's number);
- "seals" (under the seal's count, "seals 5:41");
- "then" (the reporter's slip, before what the report sets off);
- "Goes loud" (the aim slip's tag);
- "down in about" (already in `less.odds.chip`).

A takes `less.sealed.live`'s three sentences out of play: the cause becomes an icon (its line on hover) and the job becomes Popovic ···· car. They stay for hover and the Journal.

## Open questions (via Aster)

- **Sky:** A, B or C, or A with C's street marks as recommended.
- **Latch, with Keel if the engine must expose it:** the police card's "here in" shows "?" until a unit is moving. The seal's start already names the street time (3:16 here), so the card could count to it from the call.
- **Latch:** step 0 (the HUD's own cost on the Air), then the build order above, once Sky picks.
