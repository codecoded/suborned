# 085 · A pressured Inez's call: the rule, its numbers and the Help price

Motive, 2026-10-31. A report: the GDD, cards, sim, contracts and `text.json` are untouched. Everything measured ran on a scratch copy of HEAD's sim under `out/scratch/motive/085p/`, with the rule behind environment switches. The patches, plans and raw table are in `briefs/085/motive/probe/`.

## 1. The rule

**When Inez is called and she remembers a Pressure (any Pressure: helped then pressured counts, N93), the guard:**

| Beat | Today | Pressured | Source |
|---|---|---|---|
| Walks to her side | about 57 s at the city's walk | the same | the grid |
| Stays | 60 s | **5 s** | new value, `summon-pressed-stay-s` [S] |
| Goes back | walks at 1.4 m/s, about 57 s | **runs at 2.2 m/s, about 37 s** | existing `security-speed-cm-s` |
| Door clear for | **2:54.4** | **1:38.9** | 1,744 and 989 ticks, the same on all 500 seeds |

**Added: nothing else.** No Alert, no check-in, no watching for the cell, no change to the Trail or to the call itself. He still fights the rivals in a safe final fight, and he still comes once a shift. Help and Pay are untouched: 3 Help plans ran 500 seeds with the rule on and off, and all 1,500 final hashes matched.

Why add nothing: the shorter visit already brings a second cost without any new rule. He is back on the street while the Enforcer brings Vale out, and an ordinary watcher who sees that carry reports it (section 3).

Why 5 s rather than 0: she has to tell him, and the player has to see him hear it. Why 2.2 m/s rather than 3.6: 2.2 is the response unit's existing run, and 3.6 is the cell's run, which goes against Sky's "guards move slow". Your 1:20 estimate is right for 0 s at 3.6 m/s (measured 1:19.7), but section 3 shows what that does to a distracted player.

Mark: today, once Keel builds it; [D] until then. It is not a lab rule, since Sky said yes.

## 2. The price line

Pressure on Inez, for Fable to finish:

> Quick and free, but she'll warn him. The door stays clear about 1:40, not 3:00.

The chips stay $0 · 25 s · +3 T. The two times should come from the engine, rounded to 10 s, because the walk is the grid's. Listen teaches her need and her secret together, and Pressure needs that Listen, so this line can't give away the affair.

## 3. Fairness

Seeds 1-500, human pace, one process per sweep (`suborned-sim batch --seeds 500 --digest --no-replay`, through `bun run guard`). At 500 seeds, read differences under about 2 points as noise. "N s late" means the Enforcer sets off for the door N s after he could. The plans are bots: one that sees the guard back still tries the Enter and fails, where a person would hold off and lose the route another way.

**Win rate, today against each candidate:**

| Plan | Today | **5 s, 2.2 m/s (1:39)** | 5 s, 3.6 m/s (1:25) | 0 s, 3.6 m/s (1:20) |
|---|---|---|---|---|
| Prepared Pressure, on time | 94.6 | **94.8** | 93.0 | 91.0 |
| Helped once, then pressured (N93) | 98.4 | **98.0** | 97.8 | 99.0 |
| Enforcer 40 s late | 93.2 | **93.0** | 93.0 | 93.0 |
| Enforcer 60 s late | 95.8 | **94.8** | 92.8 | 87.0 |
| Enforcer 80 s late | 94.2 | **79.6** | 21.0 | 0 |
| Enforcer 100 s late | 93.0 | **0** | 0 | 0 |
| Call first, then Loop, distracted | 93.6 | **84.0** | 73.4 | 65.8 |
| Enforcer walks from the car, distracted | 93.6 | **92.4** | 90.4 | 89.0 |
| `thin-quiet` (band 60-90) | 86.8 | **86.6** | 86.2 | 85.8 |
| `careless` (band up to 30) | 0 | **0** | 0 | 0 |
| `quiet-a`, Help (control) | 99.0 | **99.0** | 99.0 | 99.0 |

**Time left, from the Enter order until he is back at the door:**

| Plan | Today, median (min) | 5 s, 2.2 m/s, median (min) |
|---|---|---|
| Prepared Pressure, on time | 162 s (138) | **87 s (63)** |
| `thin-quiet` | 156 s (59) | **80 s (12)** |
| Enforcer 60 s late | 107 s (82) | **31 s (7)** |
| Call first, distracted | 102 s (42) | **28 s (1)** |

The Enter takes 38 s, so a prepared Enforcer finishes it with about 49 s to spare (25 s at worst).

**What Alert and Trail do on wins (Alert 2 or more, share of wins; mean total Trail):**

| Plan | Today | 5 s, 2.2 m/s | 5 s, 3.6 m/s |
|---|---|---|---|
| Prepared Pressure, on time | 3 %, T 9.9 | **12 %, T 11.6** | 23 %, T 13.3 |
| Enforcer 20 s late | 16 %, T 14.7 | **53 %, T 15.1** | 16 %, T 10.6 |
| Enforcer 40 s late | 28 %, T 14.5 | **28 %, T 12.5** | 28 %, T 12.5 |

The cause is the carry. Over 200 seeds at 20 s late, reports of the carry by the guard went from none to 84; reports by an office worker stayed at 33. On time, the guard's reports went from none to 29, and office workers' stayed at 5.

**The verdict:**
- **Pressure stays playable.** No on-time plan loses more than noise at the recommended number, and `thin-quiet` stays in its band.
- **A prepared player's win rate doesn't change, so in wins alone Pressure is priced the same as before.** The new price is elsewhere: the slack after the call halves, a distracted player loses about 10 points, and the guard is often on the street during the carry.
- **I propose no harsher number.** Going to 3.6 m/s barely touches the on-time player (94.8 to 93.0) and mostly takes wins from the late and distracted, as failed Enters. Sky asked for a shorter window, never a failed call.
- **Against Help, Pressure is still a real trade.** On time it calls about 47 s sooner and pays $0. It wins 95 % against 99 %, with 0.76 agents down per win against 0.10 (no warning from a helped Inez).

**The official check**, `missions:check bands handover --seeds 200 --engine tree`, today against 5 s, 2.2 m/s:
- R-7 fails on both, on `quiet-b take` (92 % to 75 %), so it isn't this rule.
- Bands missed go from 5 to 6. The new one is B-7 `thin-quiet`: median fight 14 s against a 15 s floor, where today's is 15 s. The 500-seed sweeps also read 14 s, so it's a real 1 s and not noise. As band owner I'd let it report rather than tune for it.
- `missions:check check` holds on both, on the same lines plus that one. MC-11 and R-7 already fail with the rule off. That was in my scratch copy; I didn't run it on the main tree.

## 4. What rehashes

Any rulebook edit moves every saved run's start hash, so this comes in two layers.

**Behaviour changes (rule on, no rulebook edit), 2 of 49 saved scripts:**
- `contracts/missions/handover/routes/route-a-pressure.json`: still a win, same end tick 2585.
- `contracts/missions/handover/routes/route-a-pressure-board.json`: still a win, same end tick 2539.

Both are listed as pending in the mission check. The rows of two adaptive plans also move, with no pins on them: `thin-quiet` and `careless`.

**Hash changes once the value is in the rulebook, 49 of 49:** I added only the new value, with no behaviour. All 49 start and final hashes moved, and no outcome did. The 49 are the 42 route scripts and 7 fixtures.
- **Must re-record:** `sim/Suborned.Sim/Fixtures/drive-session.script.json`. Scenario DP1 fails until `suborned-sim drive-session` is run.
- **Must record:** a new mission check, since c046 is the latest.
- **Should be unaffected:** the recorded checks replay on their pinned engine commit, by design. I didn't replay them.

**Scenarios:** 374/374 pass with the rule on, so none needs changing for the behaviour, and none yet tests it. Keel adds one. `ScenariosFights.cs:340` is the only pressure-then-call I confirmed.

**Sim tests:** `dotnet test sim/Suborned.Sim.Tests` passes 2,710 of 2,710 with the rule off and with it on (no rulebook edit in either run).

**For Keel** (`probe/pressure-guard-probe.patch` is a working sketch):
- Add a back-walk time beside `walkT`; `GuardPose` divides by it. `GuardAwayUntil` is already hashed, so no new hashed field should be needed.
- `guard.at-cafe` says "She keeps him talking", which is wrong for this visit.
- The Pressure card needs the window as a read.

**For Latch:** the phase stays "returning to door", so the guard needs a way to show a run.

## 5. Job 2: the Help price

**What the sim charges, played on seed 3:**

| Act | Charged | After it |
|---|---|---|
| Inez, Help × 2 | $40,000 + $40,000 | $20,000 left |
| Driver, Help × 2 | $40,000 + $40,000 | he's a Mercenary, so Help is his wrong approach too |
| Driver, Pay | $70,000 | |

**The real total is $160,000, not $120,000** (Inez twice, driver twice), or $150,000 with the driver paid. GDD §07 route B's "Help $40,000" is one visit short as well.

**In play:** route A wins with $20,000 stranded. Pay and Help on the driver are both refused `no-cash`. The only pairings I found that fit the purse start with Pressure: Pressure + driver Pay leaves $30,000, Pressure + driver Help × 2 leaves $20,000. So today the game teaches the opposite of its own purse line: only coercing Inez buys the second route.

**The fix: split the money across the visits. The purse stays $100,000, so nothing here goes to Sky on that count.** A Help that takes two visits costs $20,000 a visit, $40,000 in all, still 35 s and +1 T each. For Keel, rulebook data only and no engine code:

- A new value, `worse-help-usd: -20000`.
- Two new price lines on `turn.help`, in `probe/help-split-rulebook.patch`: one for a first visit at a worse fit, one for any later visit by a Fearful, Mercenary or Idealist person.

One line isn't enough. With it, the driver's second visit charged the full $40,000 because that visit no longer read as a worse fit. I think his mood is the cause, but I didn't confirm it. Loyal people keep paying in full for each visit, since repeated Help is their whole road.

**Measured with the split:**

| Check | Result |
|---|---|
| Inez Help × 2 + driver Help × 2 | $20,000 left: §04's $80,000 is now true |
| Inez Help × 2 + driver Pay | refused at $60,000: §04's $110,000 failure is now true |
| `quiet-a` | 99.0 %, $60,000 left |
| `quiet-b` | 91.2 %, $30,000 left, as it pays |
| New plan, Inez then driver then the door | 78.6 % over 500 seeds |

That new plan is possible but tight: 37 seeds run out of time on the driver, and 67 reach the door after Vale has left the Office. It doesn't beat `quiet-a`, so I can show the second route is now affordable, not that it pays. Waymark may want to look.

**One lesson moves:** a Mercenary's Help (two visits, $40,000) becomes cheaper than his Pay ($70,000), though 35 s slower and needing the Listen. That suits "knowing more makes a person cheaper", but it contradicts the `worse-pay-usd` reason text.

**What the split changes:**
- 9 of 49 scripts change their closing cash, with the same outcomes and end ticks: `route-a`, `route-a-board`, `route-a-disable`, `sq-ra-h3`, `sq-ra-h3-pin`, `sq-route-a-board`, `careless-loop-runs-out`, `fail-entry-seen-camera`, `fail-entry-seen-guard`.
- 3 scenarios assert the old price and need rewriting: S23, N09b and N15.
- All 49 rehash, as above, so build it together with the guard rule and record once.

The GDD's numbers then become true as written. "She may need it twice" could say the $40,000 is across two visits; Aster's call.

## 6. Job 3: the camera

**Yes, the entrance camera still has to be dealt with, so "Door empty a few minutes" can mislead.** It's true of the guard only.

- The line: `sim/Suborned.Sim/GameOrders.cs:770` fails the Enter as `entry-seen-camera` if the entrance camera is watching, right after the guard check on line 769. §07's fail list says the same: "entry seen by guard or live camera".
- The test: `quiet-a` with the Loop removed, Call then Enter, failed `entry-seen-camera` on 200 of 200 seeds. `careless` does the same on 500 of 500.
- Hint H14 already checks the camera. The scene doesn't: its `opens` line offers "Enter the Office" whatever the camera is doing.

Waymark owns this. A pressured scene also can't say "a few minutes" at 1:39 (`GUARD_AWAY` in `tools/dialogue/conversations.ts`), and "Walk, love. Don't run." won't fit it. Those are Fable's.

## 7. What I'm unsure of

- **The look:** nobody has seen this in Unity or played it. Whether 2.2 m/s reads as a run, and whether 5 s reads as her telling him, needs Sky's eye. 3.6 m/s is the fallback if it doesn't read.
- **The carry spike:** 53 % of wins at Alert 2 or more when 20 s late is sharp, and it falls back to 28 % at 40 s. It's a timing overlap, not a tuned number. The player can see him coming, but whether it feels fair is a play question.
- **Next-mission cost:** I read Trail and Alert only, not Standing or Heat in the following mission.
- **Not tested:** Pressure on Inez combined with route B across seeds. I only played those pairings on one seed, for their cash.
- **A mismatch, not touched:** the rulebook's summon rule says an alarm sends him back "at security's pace", but the code (`Game.cs:490`, `GameResponse.cs:664`) uses `guard-speed-cm-s`, 1.4 m/s, a walk, while the log says he "runs". I'd leave the behaviour and correct the words, since changing it would rehash more runs. It's separate from this rule, so it needs a decision.

## 8. Handoff

- **Keel:** inputs are sections 1, 4 and 5 and `probe/`. Done when the rule and the split are in, a new scenario covers the pressured window, and `bands` reads as in section 3.
- **Fable:** the price line, the pressured call scene and the `guard.at-cafe` words.
- **Waymark:** the camera wording, the routes' times, and a `press` adaptive plan if wanted (`probe/plans/press-late0.adaptive.json`).
- **Latch:** the guard's run on the way back.
- **Aster:** the §07 wording once built, and the alarm-run mismatch.
