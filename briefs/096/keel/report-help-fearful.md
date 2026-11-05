# Brief 096 · Keel's report: Help for a Fearful person, engine 0.44.0 (2026-11-05)

Branch `t3/fearful-help-refusal-before-listen` (this worktree, from `work/096` at 46a3a116). Sky 2026-11-05 on paying Inez, option 1:
"I think 1 is fine for now". Motive's rule: briefs/096/motive/help-fearful.md and help-fearful.json (written in this round, as Motive).
GDD text for Aster: gdd-text-help-fearful.md. Bands: bands-help-before-200.txt (engine 0.43.0 at 46a3a116) and bands-help-after-200.txt.
Mission record c054.

## The rule as built (engine 0.44.0), rulebook first

| Rule | Rulebook | Engine |
|---|---|---|
| Help is offered once the need is told (the briefing's word), not only heard | turn.help needs `{known: need-told}`, which replaces `{requires: help}`. Knowledge `need-told` = told or heard (schema KNOWS, ref Know) | Person.Knows("need-told") = KNeed or Suspected. Refusal with nothing told: still `needs-need` ("Listen first") |
| H1 the small offer: before the need is heard, priced alone at $0, 35 s, +1 T | price line `small-offer` (when not turned, need not known; s and trail from Help's base) with `alone: true` (new price-line key: when it holds, the card is that line only) | GameHelpOffer.cs `QuoteAlone`: the card's Usd, S, Trail and WorstUsd come from that line, Orders 1, every other line "no". No cash asked or taken |
| H1 everyone refuses it, a remembered refusal; Help not marked refused | outcome `small-offer` (after kept, before refused): charge s and trail, remember refusal, debrief `small-offer` (new debrief line: who, approach). No `learn refused` | from the rulebook. Log `turn.help.small-offer`. The card's Outcome is `small-offer` for every temperament and band (definite, never "?") |
| H2 a Fearful person takes the full offer once after the Listen | `one-more-help` and `visits-share-first` read temperament in mercenary, idealist, loyal; `visits-share-later` mercenary, idealist | from the rulebook. The Fearful's Help: $40,000, one order, turned |

Engine version suborned.sim/0.44.0. GameOrders.cs and Rules.cs (both over 500 lines) changed in place with no new lines; the new code is
GameHelpOffer.cs (22 lines) and ScenariosHelpOffer096.cs. DP1's drive session re-recorded (version only).

What a refusal leaves (H1): a memory `refusal` by the Fixer, so she holds the Fixer's face and the place and time (a Fearful person is a
Talker until helped); every later Help and Pay +$10,000 and +1 T, Pressure +1 T; Ask cold closed (`refusal-remembered`); the debrief line.

## Scenarios

New: 096-HELP-1 (Inez told only: the card says small-offer $0, 35 s, +1 T, the line alone; given, she refuses, no money moves, +1 T, she
holds the Fixer's face, Help not marked refused, the log and debrief lines; Ask closed; the guard, nothing told, refused needs-need),
096-HELP-2 (after the Listen: card $40,000, one order, turned; she turns on the first visit), 096-HELP-3 (small offer first, then the Listen:
$50,000 and +2 T, turns once; the driver helped twice after: $10,000 left, the purse fits), 096-HELP-4 and -4b (a driver made Fearful by the
start: the same small offer refused; heard, one visit at $40,000), 096-HELP-5 and -5b (Menace: Fearful still folds free on Ask; the small
offer definite in Menace; heard, the full offer once), 096-HELP-6 (before the Listen the Help card is the same for Inez and the Mercenary
driver: no temperament shown).

Moved by the rule, each noted in place: S22, S23, S24 (Help before the Listen was refused; now the card's small-offer is checked, and S22's
lock is checked on the guard), N15 (nine line items), N93 and P85-MIXED (a Mercenary Inez by the start: they need a Help that doesn't turn
her), P85-PRICE (Inez one visit at $40,000; the driver's two visits unchanged), P85-PURSE's words. `suborned-sim check`: 467/467 ok,
replays exact.

TS: rulebook096h.test.ts (H1 across every temperament and band through the reference evaluator, H2, H5, H6, unknown = reject); rulebook.test.ts
(Fearful Hero orders 2 → 1; the keeper-tip card lists the small-offer line). text.test.ts and conversations.test.ts: the Fearful "not yet"
Help reply and scene for Inez and the driver can no longer happen, so the four entries are removed from text.json (git keeps them, for
Fable) and the cell floor is 29.

## The plans (as Waymark, written by Keel)

- **Route A's Help waits for the Listen.** The composer places each order at the first tick the engine takes it; Help before the Listen
  used to be refused, and now it is taken as a small offer. route-a, route-a-board, route-a-disable and careless-loop-runs-out: one Help,
  `when: scout.listen.heard` (the second Help step is gone).
- **The call waits for the rival car.** One Help turns Inez 35 s sooner. Called at once, Popovic comes out as the rival car turns in
  (2:20-2:53 by seed): quiet-a fell to 83.5 % (rivals-reach-vale 17, closed-in 14, down per win 0.54). Either the Enforcer stood in the
  open holding Popovic while the crew posted (a clerk saw the carry: Alert 2), or the cell fired on the boxing car as its three stepped out.
  A prepared player who knows their timetable calls once the rival car is seen, so Popovic comes out to a posted crew. The Pressure pair has
  done this since c038 ("so Vale comes out to the posted crew"). quiet-a's call adds `log:rivals.coming` (95.5 %); the route-A reference
  plans' call `when: rivals.coming`. Also tried and not kept: holding fire on the boxing car until its three have posted (91 %; not needed
  once the call waits).
- **prepared-loud**: its dead second Help on Inez is removed (it already called after rivals.block). The driver's plans are unchanged
  (Lukac is a Mercenary: two visits, as before).
- fail-entry-seen-camera (a hand script) loses its second Help; its call is kept where it was.
- **Recomposed:** route-a and route-a-disable (both win at 4:12.7 on seed 0: 3 T, Alert 0 and 4 T, Alert 1; on 0.43.0 3:49.6), and
  careless-loop-runs-out (same fail, same tick). route-a-board still fails to compose, as it did before this change (step 23 now, 24 before).
  It is one of the 14 plans that fail to compose on the pre-change tree too (route-a-pressure-board, route-b-board, route-b-thin, route-g,
  route-g-board, route-g-fast-board, route-g-kill-board, loud, loud-fight, loud-left-downed, loud-left-standing, careless-reporter-not-stopped,
  careless-thin-all); their committed scripts stand, so `missions:rerecord` stops at compose. c054 was recorded with the check's `--record`
  on the committed engine, as c053 was.

## Bands, 200 seeds a plan (before = engine 0.43.0 at 46a3a116; after = 0.44.0)

| plan | wins before → after | fails after | fight median (p25-p75, p95) | T | down/win | idle med/p95 |
|---|---|---|---|---|---|---|
| quiet-a | 96 % → 95.5 % | rivals-reach-vale 8, sealed 1 | 13 → 15 s (11-32, 41) | 4 → 3 | 0.04 → 0.02 | 15/37 → **48/83 s** |
| prepared-loud | 67.5 % → 69 % | sealed 36, rivals 12, enforcer-down 11, withdrawn 3 | 18 → 20 s (16-48, 114) | 29 | 0.57 | 18/42 → **54/73 s** |
| quiet-b | 100 % → 100 % | - | 25 s | 4 | 0.45 | 22/37 s |
| quiet-b-thinned | 100 % → 100 % | - | 18 s | 6 | 0.16 | 5/15 s |
| quiet-g | 87 % → 87 % | unchanged | 25 s | 5 | 0.18 | 59/98 s |
| thin-quiet | 93.5 % → 93.5 % | unchanged | 18 s | 7 | 0.31 | 17/52 s |
| prepared-loud-lend | 35.5 % → 35.5 % | unchanged | 80 s | 8 | 0.69 | 23/38 s |
| veteran / veteran-drive | 61 % / 17.5 % → same | unchanged | | | | |
| drive-round | 13.5 % → 13.5 % | unchanged | | | | |
| careless, unprepared-loud, the twins | unchanged (0 %) | | | | | |

quiet-a ends at a median 4:44 (4:42 before): the 35 s the second visit took are now spent waiting for the rival car.

Band lines that moved: L96 A 96 → 95.5 % ok; B-1 quiet-a ok; B-7 quiet-a 13 → 15 s ok; B-9 quiet-a 0.04 → 0.02 ok; B-4 and L96 loud
ok. **New misses: B-8 quiet-a (idle p95 37 → 83 s) and B-8 prepared-loud (42 → 73 s)**, against "no reference plan waits over 45 s with
the whole cell idle". The second visit was filling route A's wait for the rival car. That is a design question (Motive's Q3 below), not a
plan to tune. Misses unchanged from 0.43.0: R-7, B-2 thin-quiet, B-8 quiet-g, B-11 (five plans), B-14, B-16 drive-early.

## What the host must show (Latch, with Glyph and Fable)

- **The Help card before the Listen** (`Preview("help", role)` with `Outcome == "small-offer"`): $0 · 35 s · +1 T and a refusal mark,
  "refuses a small offer: Listen first" (the line `small-offer` holds; every other line "no"; WorstUsd 0, so no cash lock). Help is no
  longer locked `needs-need` once the need is told (Inez and Lukac from 0:00), so the ring and ContextRank now offer it: show it with the
  refusal mark, not as a lock. With nothing told (anyone else) the lock stays.
- **The Help card after the Listen** on a Fearful person: $40,000 · 35 s · +1 T · turns (Outcome `turned`, Orders 1). With a remembered
  refusal the `refused-before` line (+$10,000, +1 T).
- The log `turn.help.small-offer`; the plate and gain read "refused" (text.json done.plate and done.gain: `small-offer`). The feed and
  debrief words are placeholders (text.json silentLogs `turn.help.small-offer`, marked PLACEHOLDER).
- Unity code that gives Inez two Helps: GameBoot.RouteToCall (Help at 695 and 1195: the second is now refused need-met, harmless; the call
  at 1695 still lands), Proof067 (r067-convo-inez-1/-2: the second Help scene can't play as a Help), Proof085Scenes (r085-help-2),
  ProofDone (one Help, fine). Latch's proofs to follow the engine.
- **Fable:** the small-offer scene (Inez refuses a small offer) and replies (`reply`/`done` with outcome small-offer, every temperament and
  role); the full offer that lands (one visit); help-inez-1 (the first-visit conversation, outcome not-yet) can't play now and waits for
  the small-offer scene; the four removed "not yet" Fearful entries are in git (46a3a116). **Glyph:** the refusal mark on the Help card.

## Checks

- `suborned-sim check`: 467/467 ok, replays exact (36 s).
- `bun run missions:check bands handover --seeds 200` (through `bun run guard`): 105 s before, 112 s after; 9 → 11 of 54 bands missed (the two B-8 lines); the table above.
- Mission check on the committed engine: HOLDS, every MC-* green; recorded **c054**, pinned to 1afd2155; `missions:check replay handover
  c054`: REPLAY OK.
- C# tests (`dotnet test sim/Suborned.Sim.Tests -c Release`): 3175/3175 passed, 11:10. That is over the 10-minute limit; the last round's
  run took 6:13 (156 % CPU this time, so it ran less parallel).
- `bun run typecheck`: clean. `bun test contracts/`: 312 pass.
- `bun run test` (through `bun run guard`), against the pre-change tree (46a3a116, a scratch worktree, the same out/): before 51 fail and
  3 errors (11:59), after the fixes below the same set less one. Five were new on the first after-run: four follow from the rule and are
  updated (check.test.ts: route-a has 4 contact orders, not 5, and the two B-8 reds go on KNOWN_RED, owners Motive and Sky (Q3);
  dialogue.test.ts: 29 reply cells, the counter group 29 + 9, the security group 19 + 8). The fifth, skyline's "replay recomputes", timed out
  at 300 s under the parallel load and passes alone. One baseline failure is fixed ("recorded routes that touch Inez ... taken at its
  recorded tick"). The rest are the same on both trees (out/ files a fresh worktree lacks: people, kit, skyline, levels). Both runs were
  over 10 minutes (13:49 after, 11:59 before, against last round's 8:40), so the machine was slower today, not the suite longer; flagged
  for Splice (brief 042). check.test.ts 36/36 and dialogue.test.ts 48/48 after the fixes.

## Open questions (for Sky, through Aster)

Motive's page has Q1 (Lukac's two visits) and Q2 (the Loyal's two Helps). This round adds Q3:

**Q3 · Route A's wait.** One Help puts Inez ready about 35 s before the rival car is due. The prepared plan now waits for the car (idle
p95 83 s; B-8 asks ≤ 45). Options: (a) accept: route A's band for idle is wider than 45 s, and the wait is the player's to fill (Watch the
rival car to learn its timetable, Pay a Mercenary rival after a Watch, thin the posts); (b) Waymark gives the plan's idle agents that
preparation, so the reference shows what a player does with the time; (c) the timeline moves (the rival car earlier), which touches routes B
and G. **Recommend (b), and (a) for the band until it is built**: the idle is real time a player can spend, and §07 already offers the
moves.
