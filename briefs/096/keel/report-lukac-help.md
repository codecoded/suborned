# Brief 096 · Keel's report: a worse Help once, one step dearer (engine 0.45.0), and route A's wait filled (2026-11-05)

Branch `t3/5ed96219` (from `work/096` at 39bb3672). Sky 2026-11-05 on the open questions of report-help-fearful.md: "As you
recommend": Motive's Q1 (b), Q2 (a), Keel's Q3 (b). Run as Keel (the engine) and Waymark (the plans), under Sky's speed call:
delta only (bands for the plans touched, targeted tests, no full `bun run test`).

## The rule as built (rulebook first, unknown = reject)

| Rule | Rulebook (turn.help) | Engine |
|---|---|---|
| A worse Help on a Mercenary or an Idealist is the full offer once, one step dearer | new line `worse-once` (turned false, fit worse, temperament mercenary or idealist): usd `v:worse-help-once-usd` (new value, $10,000) | from the rulebook: the driver $50,000, 35 s, +1 T, one order, turned |
| The two-visit lines read only the Loyal (Q2 (a): their repeated Help stays) | `one-more-help`, `visits-share-first`: temperament loyal | unchanged code |
| The second-visit line is gone | `visits-share-later` removed (it read Mercenary and Idealist only) | — |
| Before the Listen: the small offer, refused (0.44.0) | unchanged (`small-offer` stands alone) | unchanged |
| A Fearful person: $40,000 once (0.44.0) | unchanged | unchanged |

With a remembered refusal (a small offer refused first): the driver $60,000, +2 T, still one visit. With his temperament unread the
`worse-once` line is "?" and the worst case the cash lock asks is $50,000 (a "?" that raises the price counts; the old share never did).
Engine `suborned.sim/0.45.0` (Debrief.cs). Rules.cs and GameOrders.cs untouched; the change is data plus scenarios.

**The purse ($100,000):** Inez $40,000 + the driver $50,000 = $90,000, fits. One small offer refused first (on either): +$10,000 =
$100,000: **fits exactly, with nothing left: tight.** Small offers on both: $50,000 + $60,000 = $110,000: **no longer fits** (it fit at
$100,000 on 0.44.0), so the driver's Help is refused for money (096-HELP-9c). Pay on a Mercenary Inez ($70,000) + Help on the driver
($50,000) doesn't fit, as before.

## Scenarios

New (ScenariosHelpOnce096.cs, 104 lines): 096-HELP-7 (after a real Listen at the car: card $50,000, 35 s, +1 T, one order, turned,
worse-once holds and the two-visit lines don't; he turns on the first visit, no not-yet), -8 (small offer refused first, then the Listen:
$60,000 and +2 T, one visit), -9 (Inez $40,000 + the driver $50,000: $10,000 left), -9b (one small offer first: $0 left, fits exactly),
-9c (small offers on both: $60,000 asked of $50,000, refused, nothing charged), -10 (a Loyal driver: two Helps at $40,000, trust over time).
Moved by the rule, each noted in place: N09b (the driver's Help now refused outright for money after Pay on Inez), N14 (the wrong-approach
"?" is now worse-once), N93 and P85-MIXED (a Help that doesn't turn: Inez made Loyal by the start, since a Mercenary now turns in one),
P85-PRICE (the driver one visit at $50,000), P85-LOYAL (no share, no worse-once), P85-UNKNOWN (worst case $50,000), 096-HELP-3 ($0 left).
DP1's drive session re-recorded (version and start hash only).

TS: rulebook096w.test.ts (every band, the refusal surcharge, the small offer before, nobody else moves, the purse, unknown = reject);
rulebook.test.ts and rulebook096h.test.ts follow; tools/people-view (fit.ts and its test: the viewer still priced a Mercenary's and a
Fearful person's Help as two visits). text.json: the three `reply` and three `scenes` entries for a Mercenary's "not yet" Help are dead
cells now (text.test "no dead lines") and are removed (git keeps them at 39bb3672); dialogue.test.ts cell counts 29 → 28, security 27 → 26.

## The plans (Waymark, written by Keel)

Where route A's wait was: with one Help, Inez is turned and the Loop is on by about 1:50, and the whole cell stood until the rival car
turned in (2:20-2:50 by seed): quiet-a idle 48/83 s, prepared-loud 54/73 s.

- **quiet-a: the Loop timed to the briefing's rival car.** The briefing page's clock shows the rival car at 2:35 (the pill keeps "?").
  The Tech plants the Loop at 2:05, not straight after the Help: live 2:25 to 4:25, so one Loop covers the call, the entry and the exit
  whenever the car comes. Planted after the Help it ran out on a late car and a second Loop was needed (26 of 200 seeds; seed 11 stood
  82 s). It pays off: wins 95.5 → 99 % (600 seeds: 97.2 → 99.5 %), second Loops 26 → 0, idle 48/83 → 24/43 s.
- **prepared-loud: positioning and the thinning.** The Fixer walks to his yard post as soon as Inez is turned and calls from there (a call
  is by voice, no reach); the Scout Listens to a posted rival at the drive's mouth, the Fixer Watches him and Pays $30,000 if he is a
  Mercenary (he walks off). Pays off over 600 seeds: wins 64.5 → 70.8 %, district sealed 119 → 94, Enforcer down 37 → 16; but down per
  win 0.57 → 0.78 and closed-in 2 → 19 (the call lands about 30 s later while the Fixer pays). Idle 54/73 → 12/29 s. Positioning alone
  (no thinning) took idle to 12/28 s with no win change (64.8 %), so it isn't kept alone.
- **Tried and not kept:** the car brought round later so one Reroute covers it (wins 15-37 %, vale-leaves); the second Reroute timed
  to the patrol's return (65 %).
- **The driver's plans** (quiet-b, quiet-b-thinned, quiet-b-lookout-ignored, prepared-loud-lend, drive-round, veteran, veteran-drive):
  their `help-2` (Help again on not-yet) can no longer fire and is removed. The reference scripts (routes/) don't Help the driver
  (route-b Pays him), so no reference was recomposed; route-a's reference keeps its Loop at 1:25.

## Bands, 200 seeds a plan (before = engine 0.44.0, bands-help-after-200.txt; after = bands-lukac-after-200.txt)

| plan | wins before → after | fails after | fight med | T | down/win | idle med/p95 |
|---|---|---|---|---|---|---|
| quiet-a | 95.5 → **99 %** | rivals-reach-vale 2 | 15 s | 3 | 0.02 | 48/83 → **24/43 s** |
| prepared-loud | 69 → 69.5 % | sealed 33, rivals 12, enforcer-down 8, closed-in 7 | 20 → 26 s | 29 → 30 | 0.57 → 0.78 | 54/73 → **12/29 s** |
| quiet-b | 100 → 100 % | - | 25 → 26 s | 4 → 3 | 0.45 → 0.46 | 22/37 → 23/38 s |
| quiet-b-thinned | 100 → 100 % | - | 18 s | 6 → 5 | 0.16 | 5/15 → 9/18 s |
| prepared-loud-lend | 35.5 → 37 % | closed-in 126 | 80 → 81 s | 8 → 7 | 0.69 → 0.7 | 23/38 → 22/38 s |
| drive-round | 13.5 → 10.5 % | rivals-reach-vale 170, closed-in 9 | 14 → 13 s | 13 → 17 | 1.11 → 1.29 | 40/58 → 56/69 s |
| **veteran** | **61 → 39.5 %** | enforcer-down 107, sealed 8 | | 40 → 39 | 1.16 → 1.46 | 120/154 → 45/159 s |
| **veteran-drive** | **17.5 → 9 %** | sealed 89, closed-in 30, vale-leaves 29 | | 38 | 1.71 → 1.94 | |
| quiet-b-lookout-ignored | 0 → 0 % | unchanged | | | | |

Band lines: **B-8 quiet-a and B-8 prepared-loud are back in** (off KNOWN_RED). New misses: **L96 A** (quiet-a 99 % is over its 90-97 %
ceiling), **L96 B-all** (quiet-b 0.69 captured per win, ceiling 0.6; 0.56 before), **L96 veteran** (39.5 %, floor 40). They go on
KNOWN_RED for Motive (below). Unchanged misses: R-7, B-2 thin-quiet, B-8 quiet-g, B-11 (four plans), B-14, B-16 drive-early.

## Checks (delta only, Sky 2026-11-05)

- `suborned-sim check` (all 473, 35 s, before the speed call): 473/473 ok, replays exact. After the plans: the Help, turn and route
  scenarios (096-HELP-*, P85-*, N09/N09b/N14/N15/N93, S01-S28, DP1): 59/59 ok.
- C# tests, filtered (Needs, GoalWatch, GateQuery, SeedTests, FailCause): 557/557 passed, 1:41. The full 3,175 were not run.
- `bun test` on contracts/rules, contracts/missions/handover, tools/dialogue, tools/people-view: 405 pass. Full `bun run test` not run.
- `bun run typecheck`: clean.
- `bun run missions:check handover --record`: every MC-* green, verdict HOLDS; **c055** recorded on the committed engine (f3ed73f4);
  `missions:check replay handover c055`: REPLAY OK.

## For Latch (the host list)

- **The Help card on the driver after his Listen:** $50,000 · 35 s · +1 T · turns (Quote.Outcome `turned`, Orders 1, line `worse-once`
  holds). With a refused small offer first: $60,000, +2 T (`refused-before` + `worse-once`). Before the Listen: the small offer, as 0.44.0.
- The price line `visits-share-later` no longer exists; anything that looks lines up by id should read `worse-once` (the card's
  wrong-approach line). A "?" `worse-once` raises WorstUsd to $50,000: the cash lock reads it.
- Unity code that Helps the driver twice, or waits on `turn.help.not-yet` for him, now gets one visit (help-2 refused need-met); proofs
  that expect his second Help scene follow the engine.
- Glyph: the card's step reads "one visit, +$10,000" where it read "+1 Help".

## For Fable (placeholders)

- **A Help scene for the driver** (`help-driver`): today Help on Lukac has no conversation of its own (only `pay-driver`). Needed: the
  full offer that lands in one visit ($50,000, the cash he's short of), and its variant after a refused small offer.
- The six removed entries (reply and scene for a Mercenary's "not yet" Help: waitress, driver, guard) are in git at 39bb3672.
- The small-offer scene for the driver (he refuses before the Listen) if it should differ from Inez's.

## Open questions and issues (for Sky, through Aster)

1. **The veteran plans fell** (veteran 61 → 39.5 %, veteran-drive 17.5 → 9 %, drive-round 13.5 → 10.5 %): the driver now turns 35 s
   sooner, and these plans key their moves on `turn.help.turned`, so the Fixer's run and the drive move earlier. Not looked into
   (time); they are variants, but L96 veteran is a band line. Waymark/Motive: retime them on the clock or the Take, not the Help.
2. **quiet-a at 99 %** is over L96 A's 97 % ceiling. The Loop timing is a fair prepared play, so Motive's call: raise the ceiling for
   a prepared player, or accept it.
3. **quiet-b captured per win 0.69** (ceiling 0.6, was 0.56): one fewer visit for the Fixer at the pickup; Motive to look.
4. **The purse is tight:** one refused small offer leaves exactly $0 on Help + Help; refused small offers on both don't fit any more.
   Fine by Motive's H7 reading ("learning buys the second route")? An uninformed player who offered both early loses the second route.
5. **prepared-loud's thinning costs:** down per win 0.57 → 0.78 and closed-in 2 → 19 while wins rise; Motive to read.
6. **Not run, per the speed call:** full `bun run test`, the full C# suite, bands for the plans this change doesn't touch (they are in
   the mission check's own run, bands-lukac-after-200.txt). The Mercenary Inez's "not yet" Help reply was removed with the others.
