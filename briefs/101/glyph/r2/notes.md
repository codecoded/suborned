# The HUD sweep, round 2: lean first (brief 101)

Glyph, 2026-11-08. Open `board.png` first. It is 1000 × 8406, made for a phone, and runs: the busiest moment three ways, the coverage table, what a shot shows now, each direction's four moments, then my pick and its cost.

This is a mock only. Nothing changed under `unity/Assets`, in the GDD or in `text.json`. Unity wasn't started, nothing was committed, and no paid or Codex call was made ($0).

Sky on round 1:
- "A just covers too much of the screen. And that's not even a busy example you're showing"
- "Why is their stats about 75% etc showing when firing?"

Then Sky's rule on numbers in play (brief 101):
- no hit chance anywhere;
- no action durations;
- time only for key events in the world;
- an order's price is money, Trail, Alert risk and what the street will do.

Both directions follow it.

## The busiest moment

The final fight, 3:47.5 into the pinned loud run on seed 3 (`tools/game/pins/loud.seed3.json`, tick 2275; the batch's `fight-loud-body`, build 0.2.268).

| | room on screen | picture |
|---|---|---|
| Today | 10.0 % | `today-busy.png` |
| 1 · Street labels | 4.1 % | `S-busy.png` |
| 2 · Corner tokens (my pick) | 5.1 % | `T-busy.png` |

What is on screen:
- the seal at 1:54 (it seals at 5:41);
- Police coming, with four more cards waiting (1 of 5);
- two bubbles: a rival's "Block the street. Nobody leaves." and the Scout's "One of them's heading for the road.";
- our car held by its blocker, with its lock, the count and five empty seat pips;
- the blocker in cover at the planter run, with Fixer and Tech firing on him;
- the player's right hold on him (the Scout is selected);
- a report about to land (9 s);
- Popovic off screen with the Enforcer, as a face at the edge;
- the roster in its live states (Scout firing and hit once, Fixer under fire, Tech firing, the Enforcer carrying), the scope and the Book.

What is real and what is laid on:
- **Real (the capture):** the street, the clock, the seal, the cards, both bubbles, the car and who holds it, the edge marks, the roster's states. The state comes from the sim log of the same script (`dotnet sim/cli/bin/Debug/net10.0/suborned-sim.dll play tools/game/pins/loud.seed3.json`, engine 0.50.0). The fight started at 3:45.1: two police in cover, security-1 the blocker holding the car from the road. Fixer and Tech pick security-1 at 3:47.5.
- **Laid on, for today:** the pieces this capture lacks, each cut from another real capture's HUD layer and placed where the game puts it (`make/r2_today.py`):
  - the hold pill on the blocker (`hold-fire-ready`, the fight range);
  - the focus disc, red pips and fire mark (`hold-focus`);
  - the reporter's plate and "!" disc (`why-quiet-radio`).

  The colours come from each capture's picture and the shape from its HUD layer. The fight range's HUD layer was grabbed in a different flash state from its picture, but the alpha is the same.
- **Borrowed:** no report is in flight at 3:47 on seed 3. The one shown is from 4:54.1 of the same run (an office worker walks to the Office door to report a body; it lands at 5:03.2), on the man walking at the top of the frame.
- **The player's hold** is illustrative. At 3:48.7 the Scout's real order is the rivals.

The seal card's paving was rebuilt pixel by pixel from the paving's own lattice ((37, 6) and (31, −11) px), so the brick joints carry on. The rest of today's HUD was lifted with round 1's patch fill. The loud frame takes the quiet frame's facade under the old roster (same camera; the views match within 0.7 per channel below the balcony).

## Room on screen

Measured the way `tools/hud-budget` reads a capture: the HUD layer alone, alpha ≥ 24 at 1920 × 1080, so keylines, shadows and halos count (`make/coverage.json`). The rings at people's feet are drawn on the HUD layer in the mocks and count. In the game, ground marks are world meshes (`HudToolkit.Ground.cs`), so a build would read a little lower.

| moment | today | 1 · Street labels | 2 · Corner tokens |
|---|---|---|---|
| at rest (roster, clock, scope, Book) | 4.6 % | 2.5 % | 3.0 % |
| quiet, 3:28: a guard radioing it in (4 s), Our car boxed in | 6.6 % | 3.2 % | 4.1 % |
| loud, 2:34: the seal (3:07), Police coming, here in 0:42 | 8.8 % | 3.5 % | 4.5 % |
| aiming the first shot, 2:31 | 6.0 % | 2.9 % | 3.5 % |
| **the busiest moment, 3:47** | **10.0 %** | **4.1 %** | **5.1 %** |

- Today's numbers are build 0.2.268, before Latch's removal of the odds and durations. That trims words, not plates.
- Today's rest reads 4.6 % on this build; round 1 read 4.8 % on the build of 7 November.
- Today's aim is `fight-loud-checkpoint` with the hold pill laid on. Round 1 measured the capture alone (5.0 %).
- **The project's own reader agrees.** `bun tools/hud-budget/cli.ts out/scratch/glyph/101r2/budget` (made by `make/r2_budget.py`): 10/10 within budget, with the same numbers.
  - Its text check first flagged two misreads: the big keyed "3:28" read as a three-word line, and the car's seat pips read as two lines of letters.
  - With the clock declared a clock reading and the car marks declared as art (as the build's own manifests declare them), all ten pass with no running text outside speech and the card.

## The two directions

The street layer is the same in both, because Sky's rule decides it. The choice is the corners.

**The street layer (both):**
- Labels are bold Jost with a solid cream keyline and a soft shadow, so they read on paving, asphalt, palms and the fountain. 3:28 over the palm, flagged in round 1, now reads (`out/scratch/glyph/101r2/check-S-quiet.png`).
- Urgent numbers are orange with an ink keyline.
- Bubbles are cream, with the speaker's face or badge (the rival's violet badge).
- Rings sit at the feet: the target's ring in his signal colour, and the reporter's ring draining orange.
- The car carries its own lock, holder count and seat pips. A holder mark sits over the holder, and Popovic docks at the edge as a face with a chevron.

**1 · Street labels** (`S-*.png`). No plates anywhere.
- The corners are bare type: the time (42 px), the seal's lock and count (42 px, orange) under it, Alert lamps, Trail and money, the next key event ("Chime 5:10"), and the key people's faces.
- The roster is the live faces only, with health pips and the act's icon (no seconds).
- The scope is a small round lens of dark glass with three round tabs.
- News with a place docks at the edge it is past: police coming dock at the right edge with "0:42" big. The rest sits by the Book.

**2 · Corner tokens** (`T-*.png`). Small enamel tokens only where something must sit in a corner, in flat vitreous enamel (the book's deep teal, a cream inline, orange for now) with a crisp shadow and no chrome or screws.
- The clock is a station roundel with the Alert lamps on its face.
- The seal is an orange roundel hung under it.
- One slim tail carries the next key event, Trail and money; the key people hang under it.
- The news is a lozenge by the Book: its badge on an orange roundel, a cream ticket for a key time ("0:42"), and "1 of n ›".
- The roster's faces sit in enamel rings, and the scope is a round enamel instrument.

Both directions keep the live portrait's cue layer: a spiked ring under fire, and orange rays at the sidearm while firing.

## What a shot shows instead of a percentage

- **The first shot** (2:31, the Enforcer on the guard at 5 m) shows what the street will do: Goes loud, Alert to 2 (lamps), police come (badge), the district seals (lock), Trail +6. The crosshair's hold ring fills turquoise as the commit's hold completes. There's no "75 %", no "down in about 4 s", and no "police in 0:45" or "seals 5:41" on the shot: those times show on the seal and the police card once they start.
- **On the target:** his ring in his signal colour.
  - Whole means he's in range; three arcs mean a long shot. That is the only hint, and it has no number. It is §10's band ticks and shield badge moved onto the ring.
  - A shield on the ring's front means his cover counts; half-filled, it counts in part.
- **In the final fight** a shot costs nothing new. Fight shots are untraced (`rulebook.json` fight `traced: false`) and the Alert is already 2, so the aim is just his ring, the crosshair and the word Fire. Over him: his health pips and, if he holds our car, the holder mark.
- **The roster:** a firing agent shows rays at the sidearm, never the seconds to down. The hit rate moves to the debrief's numbers.

**Today's odds plate** (`HudToolkit.Hold.cs` line 145, `HostOdds.cs`, as at `4bc5bb7a`) showed the chance, its sum (aim, range, cover, movement), the 5–95 limits and the streak note on Alt, and the sum line alone on a 0.6 s rest.
- Latch's removal, in progress, already retires it. `HostOdds.cs` keeps only the debrief's signed number and the Enforcer's flash, and `Hold.cs` no longer shows a fire breakdown on Alt or on a rest.
- **Retire it, don't move it.** Its parts become shapes:
  - range is the ring's arcs;
  - cover is the shield;
  - movement is the target's own run;
  - aim is the agent's Aim pips on the book's Cell page.
- The streak note goes to the how-to-play (§06 fairness says the how-to-play tells it).
- **One Alt-only line is left beside it:** a locked way's long reason (`LockAltFrame` on Alt only). It should also come up on the 0.6 s rest, so it isn't key-only (Latch).

## My pick: 2 · Corner tokens

1. **The corners are glanced at all the time.** A fixed shape in a fixed place is found without reading: the orange roundel is the seal before you read 1:54. Sky missed the seal when it was small (brief 093). A token can't be missed and still costs little.
2. **It's still half of today:** 3.0 % at rest against 4.6 %, and 5.1 % at the busiest moment against 10.0 %. Everything on people is the shared street layer.
3. **It has a voice.** The city's own enamel signs, in the book's deep teal, cream and orange, belong with the book, the start page and debrief A.
4. **Street labels (1) is the fallback** if Sky wants it leaner still. Its type reads on any ground now, but the corners lose their shape.

## What building it costs

Latch builds it piece by piece, each piece shot and checked at 2x on its own. The sizes are my estimate for Latch to check (S is small; M is a few S).

| step | piece | files today | size |
|---|---|---|---|
| 0 | Measure the HUD alone on the Air at play zoom, with the HUD on and off. Its cost has never been split (brief 086). | | S |
| 1 | Kit: the enamel roundel and lozenge sprites; the keyline (UI Toolkit's own text outline, `-unity-text-outline-width/-color`, plus a soft text shadow); keyed copies of about 20 street icons; the book's colours in `kit.uss` | `kit.uss`, sprites | S |
| 2 | The clock token and the seal roundel first (Sky's pain point). They replace the status pill and the 420 × 104 seal card; the key people hang under the tail. | `HudToolkit.Seal.cs` (73), the pill in `HudToolkit.cs` (1300), the key people in `HudToolkit.Less.cs` (1847) | M |
| 3 | The news lozenge and the Book roundel | `HudToolkit.Crucial.cs`, `HudToolkit.BookKey.cs` | S |
| 4 | The roster: enamel-ringed live faces, no tray, health pips, the act's icon with no seconds, the cue layer kept | `HudToolkit.Roster.cs`, `HudToolkit.Live.cs` | M |
| 5 | The scope as a round enamel instrument with three round tabs | `HudToolkit.Scope.cs` | S |
| 6 | The street layer: keyline labels, the shot (ring, arcs, shield, hold, Fire, the Goes-loud row), the report's draining ring, the car's lock and seat pips, holder marks, edge faces, bubbles with the speaker's face. The rings join the world-mesh batch. | `HudToolkit.Ground.cs`, `Hold.cs`, `Fire.cs`, `TargetRings.cs`, `OnWay.cs`, `HeldGo.cs`, `CuesEdge.cs`, the bubbles in `Less.cs` | M |

- Removing the odds and the durations is Latch's work already under way.
- `HudToolkit.cs` and `HudToolkit.Less.cs` are over the 500-line rule, so each piece moves into its own file as it changes (brief 084).

**What stays:**
- every position on the GDD's HUD map, every mechanic and every click;
- the signal colours and their rules (cones red, guard amber, rivals violet, drawn unlit); no cones in a fight;
- the live portraits and their cues;
- Jost and the kit's `icons/singles`;
- the act ring's wedges;
- the budget check and the 2x crop check.

**The GDD:**
- §15's HUD kit ("Chrome plate", nine-slice frames, the colour list) changes once Sky has picked and seen a real capture. That's a design change, so it gets a new version.
- §10's going-loud card ("who comes and when, the seal time") loses its times under Sky's rule. That goes with v0.40.0's change to the full-price rule.

**On the Air (60 fps at play zoom):** there's no full-screen pass and no blur at run time.
- Tokens are sprites.
- The keyline is the text outline UI Toolkit already draws.
- The rings are world meshes in the existing batch.
- Labels are set only when their value changes (the clock once a second).

That is an estimate; step 0 measures it.

## Words for Fable

- **New:** one label, "Goes loud" (on the first shot's row).
- **Already the game's:** "Fire", "Chime" (the timeline's short word) and "1 of n ›".
- **Leaves play:** the `less.odds` block (chip, chipBare, sum, limits, streak). The streak note moves to the how-to-play.

## Captures I need (for Aster)

1. One real capture of the busiest moment, so no composite is needed: the loud run on seed 3 at about 3:48, the Scout selected, a right hold on security-1 (the policeman at the planter run).
2. A report walking during a fight. On seed 3 none does at 3:47; the next starts at 4:54, after the fight. Either a seed with one, or 4:54 to 5:03 of this run (four office workers walking to report, the seal under a minute).
3. The same moment at the Air's fullscreen 2880 × 1864, and one at night.
4. The police countdown after Popovic is out, and the Go card with the seat pips filling.

## Files

| file | what |
|---|---|
| `board.png` | the board for Sky's phone |
| `today-{rest,quiet,loud,aim,busy}.png` | today's HUD at the five moments (aim and busy composed from real HUD layers) |
| `S-{rest,quiet,loud,aim,busy}.png` | 1 · Street labels |
| `T-{rest,quiet,loud,aim,busy}.png` | 2 · Corner tokens |
| `make/coverage.json` | the measure |
| `make/r2_base.py` | the frames (the busiest one's lift), the keyline, the street layer, the data |
| `make/r2_street.py`, `make/r2_tokens.py` | the two directions' corners and renders |
| `make/r2_today.py` | today's composites |
| `make/r2_cover.py` | the coverage table |
| `make/r2_budget.py` | the project's reader on the mocks |
| `make/r2_crops.py` | the 2x check sheets |
| `make/r2_board.py`, `make/r2_board_text.py` | the board |
| `out/scratch/glyph/101r2/src/` | the source captures as used, with `SHA256.txt` and a `KEEP` file. The batch shots rotate, and Latch is changing the HUD now. |
| `out/scratch/glyph/101r2/check-{S,T}-*.png` | the 2x crop check, every piece of every frame |
| `out/scratch/glyph/101r2/loud3.log` | the sim log of the loud run |

To re-render (about 90 s):

```
cd briefs/101/glyph/r2/make && python3 -I r2_today.py && python3 -I r2_street.py && python3 -I r2_tokens.py && python3 -I r2_cover.py && python3 -I r2_budget.py && python3 -I r2_crops.py && python3 -I r2_board.py
```

## Evidence

- **The 2x crop check** covered every piece of all ten frames (`check-{S,T}-{rest,quiet,loud,aim,busy}.png`). I fixed:
  - the clock and seal roundels clipping the right edge;
  - "Fire" touching the crosshair's tick;
  - the holder mark colliding with the focus marks (the shooters' faces over the target are dropped; the roster says who fires);
  - the reporter's seconds sitting on red flowers;
  - the scope's lines lost over the building (now a round lens);
  - the broken facade under the old roster in the loud frame.
- **Coverage:** `make/coverage.json`, and the project's reader at 10/10 within budget (command above).
- **The board:** I read it end to end at full size in six pieces, the way a phone scrolls.
- **Files:** all under 500 lines (the largest is `r2_base.py`, 262).
- **Colours on the board's chart:** checked with the dataviz validator (`#009688`, `#e8622c` on `#f9f7ee`: all checks pass). Today is the de-emphasis grey, carried by labels and the table.

## What's illustrative

- which key people are known;
- the player's hold on the blocker;
- the report borrowed from 4:54;
- the long-shot and part-cover cues, which no frame here needs and which are drawn on the board's key only.
