# 012 · The idle cell (Motive, 2026-10-16)

**Question (Aster, from Sky's play of the Handover).** "We need a way to keep the player entertained at all times." Sky worries most about the Enforcer, then the Fixer. Sky has already approved making view cones the Scout's skill: Watch teaches a watcher's cone and routine, and Keel is building it.

This is a design study. Nothing in the GDD, `contracts/` or `sim/` was edited.

**Crew.** I could not start Waymark or Keel from this thread, because it has no agent tool. So §3 (mission side) and the feasibility column in §2 are my own reading of `contracts/missions/handover/mission.json` and `sim/Suborned.Sim/`. Each is marked **for Waymark** or **for Keel** to confirm or overrule.

## 1 · Measurement

**Tool.** `tools/missions/idle/` contains two files:
- `Program.cs` replays each route script tick by tick, the way `Engine.Play` does, and samples `Game.ActivityOf` for each agent. That call changes nothing in the run.
- `idle.ts` turns the samples into intervals and reports them.

Run it with `bun tools/missions/idle/idle.ts`, with no arguments, over every script in `contracts/missions/handover/routes/`. Add `--json` for machine output. It exits 1 unless every stepped run ends on the same hash as `Engine.Play`. All 18 match, and `bun run typecheck` is clean.

**What it counts.** Each agent's time falls into one of these buckets:

| bucket | what counts |
|---|---|
| walking | walking or running |
| acting | an act with a progress bar |
| tethered | a held loop, or carrying Vale |
| fighting | drawing, or weapon out in the final fight |
| idle | none of the above |

"Pre-fight" means before the final fight starts, because the fight takes the same time for everyone. "Waiting for" is the order that ended the idle stretch and the events logged at that tick.

The 12 `fail-*` and `withdraw` scripts are the checker's minimal fixtures, not play. They are left out of the means; their output is in the appendix file.

**Winning routes, idle % before the final fight** (longest idle stretch in brackets). `route-b-block` measures the same as `route-b`, and `loud-fast` the same as `loud`.

| route | end | fight | Scout | Fixer | Tech | Enforcer | longest gap with no order | dead air (all 4 idle) |
|---|---|---|---|---|---|---|---|---|
| A (Help ×2) | 4:27 | 3:34 | 58 % (100 s) | 42 % (63 s) | 5 % | **61 % (130 s)** | 50 s | 0 s |
| A (Pressure) | 3:31 | 2:38 | 44 % (66 s) | **62 % (66 s)** | 7 % | 47 % (66 s) | 42 s | 0 s |
| B (driver) | 7:40 | 6:48 | 45 % (145 s) | **59 % (259 s)** | **100 %** | **81 % (328 s)** | **136 s** | **75 s** (4:54–6:10) |
| loud | 3:53 | 3:15 | 62 % (121 s) | 62 % (121 s) | 23 % | 52 % (102 s) | 68 s | 0 s |
| **mean of the 6 wins** | | | 52 % | 58 % | 43 % | 62 % | | 7 % of pre-fight time |

**What each agent was waiting for:**
- **Route A (Help ×2):**
  - Enforcer, 0:41–2:51: waits for Inez's call, which is gated by two 50 s Helps.
  - Fixer, 0:08–1:11: waits for Listen to land.
  - Scout, 1:11–2:51: has nothing left once Listen lands.
- **Route A (Pressure):**
  - Enforcer, 0:41–1:46: waits for the call.
  - Scout and Fixer, 2:03–3:10: have nothing left after the call, so they wait for the ambush.
- **Route B:**
  - Tech: has no job anywhere. A turned driver takes the take out of every camera's view.
  - Enforcer, 0:41–6:10: waits for the clock, because Vale reaches his car at 6:10.
  - Fixer, 3:00–7:19: has nothing after the Pay at 2:01.
  - Scout, 4:54–7:19: has nothing after the false tip.
  - The whole cell, 4:54–6:10: waits for Vale.
- **Loud:** until the guard is shot at 2:00, only two agents are in the route, so the Scout and Fixer wait.

**Findings:**
1. **The idle time belongs to single agents; the cell as a whole is rarely stopped.** All four are idle together only on B, for one 75 s stretch. Across the six wins that is 7 % of pre-fight time, while each agent idles 43–62 %. A time skip can cure at most the 7 %.
2. **Every verb sits on the main chain.** Once an agent's link is done, nothing it can do changes the outcome:
   - the Scout is done at Listen or the tip;
   - the Fixer is done at the turn or the call;
   - the Tech is done at the loop on A, and never starts on B;
   - the Enforcer's first verb is the take.
3. **Today's verbs used as preparation help everyone except the Enforcer, whose wait gets longer.** I measured this with `briefs/012/idle-cell-study/prepared-a.json`: route A by Pressure, plus turning the driver and a false tip at 3:10, all before the call.

   | | route A (Pressure) | prepared A |
   |---|---|---|
   | finish | 3:31 | 4:54 |
   | Scout idle | 44 % | 25 % |
   | Tech idle | 7 % | 4 % |
   | Fixer idle | 62 % | 48 % |
   | **Enforcer idle (longest stretch)** | 47 % (66 s) | **65 % (148 s)** |
   | rivals in the ambush | 5 | 3 |
   | Trail | 7 | 11 |
   | cash left | $100,000 | $30,000 |

   Every preparation the player adds lengthens the Enforcer's wait. He needs preparation of his own.

## 2 · Candidate prep jobs

Rules for every candidate:
- Prices are in the rulebook's units. "A risk" means the Alert the act can cause.
- Each job runs alongside the chain and has to change the outcome, the final fight or the debrief, or open a new option.
- "Surface" estimates how much rule and engine change it needs (**for Keel**).

### Enforcer (Sky's first worry)

| verb | needs | price | changes | counterplay | not busywork because | surface |
|---|---|---|---|---|---|---|
| **E1 Muscle**: stands behind the Fixer's Pressure | within 4 m of the Fixer and the subject for the whole Pressure | $0 · the Pressure's 35 s, for both · +1 T (a second face) · A: as the Pressure | Pressure is never "one step worse" for a temperament that doesn't suit it, though Loyal still reports and Idealist still refuses (§04) · subject fear +1 more · witnesses remember both faces · Menace if witnessed | the guard is Inez's lover and a café regular: if he sees the Enforcer lean on her, he won't fight at your side and remembers the face · more fear means more Talkers later | turns a Mercenary driver for $0 instead of $70,000, so the purse also covers Help ×2 for Inez: **both fronts on $80,000** | **small**: one `fit` condition on `turn.pressure` (muscle within m), one memory fact |
| **E2 Set the car** | at the getaway car (`atReach`), before the final fight | $0 · 30 s · 0 T · A none · anyone within 13 m learns "knows cell car" (an existing Talker fact) | when the fight starts, agents within 10 m of the car begin at its fight posts, in cover (the 500 ‰ cover modifier from the first shot) | witnesses: the car becomes a lead for investigators | fewer hits taken: in prepared A the Fixer went down in the open | **small to medium**: Pulse's cover points and `CoverRules.Select` exist; it adds a start-posts flag |
| E3 Case the door | within 13 m of an Enter door, out of the guard's and the camera's cone (if seen, a found fault, A 1) | $0 · 20 s · 0 T | Enter 38 → 25 s (68 → 55 once A ≥ 2) | the cones, which are the Scout's skill, show when the door is unwatched | a shorter tether for the Tech, and the exit lands sooner | small. **Not top 5**: weaker effect |
| E4 Take a lookout (Aster's shadow or stall) | a lone rival out of the other rivals' cones, no civilian within 13 m | $0 · 20 s · 0 T if unseen · seen = violence (A 3, +T, Menace; existing) | −1 rival | violet cones, crowds | the highest-drama job | **large**: the rivals have no bodies before the fight (`GameFight.cs:53–62` creates them at `fight.safe`). **Defer [L].** Stalling the rival car is **rejected**: a device doing a person's job, and the turned driver already blocks it (people > powers) |

### Fixer

| verb | needs | price | changes | counterplay | not busywork because | surface |
|---|---|---|---|---|---|---|
| **F1 Ask the keeper** (Aster's "ask around", through a person) | a keeper. In the r016 cast, café keeper Anika Nasser (`p-cafe-03`, Idealist) knows the needs of 18 people: the door guard and two more Records guards, but not Inez or the driver | the approach's own price (Ask $0 · 25 s · +1 T; a Hero Idealist may volunteer) | `keeper.tips` fires when she is turned, not only at mission start: everyone she knows has their need known without a Listen, and temperament stays "?" (Sky's rule) | she remembers the face and who you asked about (a Talker fact) | Learn stops being the Scout's alone, which removes the Fixer's opening 63 s wait · it opens the **guard himself** as a contact · she stays a source between missions (§08 economy) | **medium**: one phase line on `keeper.tips`, plus orders on any simulated person (today orders target roles, `PersonByRole`, `Game.cs:188`). That second part is shared with F2 |
| **F2 Quiet a witness** (already in GDD §05 counters, not built) | a person holding a fact about the cell, before an investigator reaches them | Pay $5,000 · 15 s · +0 T, or Pressure $0 · 15 s · +1 T with fear +1 | that person's facts are sealed: questioned, they give nothing · the debrief's investigation can drop a step (named witnesses come first, §08) | investigators question near the last incident about every 20 s once Trail is open, so it is a race · a Pressured witness can still become a Talker | spends the leftover purse ($20–30k) on the debrief (pillar: live with it) · fills the Fixer's 66–259 s after the turn | **medium**: one event, and the talker test reads "sealed"; shares person targets with F1 |
| **F3 A second contact** | today's verbs | the approach's price | turned driver −1 rival (measured: 5 → 3 with the tip) | the purse ($100,000) | already works with **no new rules**; the player just can't see it pays off | none; needs the fight ledger shown (§3) |
| F4 Buy a crowd | a person paid to gather one | — | crowd blocks sightlines | — | overlaps the Tech's Loop, and needs a new crowd behaviour (Pulse) | **rejected for now [L]** |

### Scout

| verb | needs | price | changes | counterplay | not busywork because | surface |
|---|---|---|---|---|---|---|
| S1 Read a watcher (**approved**) | Watch on the guard, the patrol or a camera | Watch 20 s · 0 T | that watcher's cone and routine are drawn | being seen watching is a sighting | the cones become knowledge you earned | Keel, in progress |
| **S2 Read the rivals** | Watch on the rival crew, near `rival-park`, after they arrive | $0 · 20 s · 0 T · inside the violet cone the rivals learn the Scout's face, so his later false tip fails | the ambush is seen before it fires (the safe fight's warning `warn`, as with a helped Inez) | the rivals' cone | gives the 3:10 arrival a job, and works on routes that don't turn Inez | **small**: a Watch target "rivals" (Mislead already uses it), plus one line in the fight's warning flag |
| S3 Tail | follow a person 30 s | — | learns their ties (introductions, lovers, summons) | — | a contact kept for later (Inherit) | medium to large: a follow verb. **Defer** |
| S4 False tip | exists | 44 s · +1 T | −1 rival | — | already a job | none. It needs the rivals there before the fight, which is a mission-side change (§3) |

### Tech

| verb | needs | price | changes | not busywork because | surface |
|---|---|---|---|---|---|
| **T1 Loop the pickup camera** | the same cabinet, one camera at a time | as Loop: 62 s · +1 T · tethered | the B front without the driver (Scout tip, plus the Enforcer takes at the bay), and the driver's turn kept off the camera's log | B gets a Tech job, and the $70,000 is freed | **small**: `GameOrders.cs:552` hard-codes `"entrance"`; it becomes a list of cameras in the mission data |
| T2 Reroute the patrol on B | exists | 45 s · +1 T | — | only matters if the patrol threatens B (§3 placement) | none |
| T3 Pull a watcher's log (records are paper, §01) | at the cabinet, or at a civic robot | $0 · 30 s · 0 T · seen by security = found fault (A ≥ 1) | that watcher's sightings so far are gone: no leads | the debrief | small to medium: sightings exist (`sighting:patrol:enforcer`). **Not top 5** |
| T4 False dispatch for Vale's car | — | — | — | replaces the driver | **rejected** (people > powers) |

## 3 · Mission side (proposed, **for Waymark**)

**More openings than the minimum route, all on today's verbs plus the jobs above:**
- **The guard himself.** Through the keeper (F1), or Watch then Pay: he is a Mercenary with a debt. A turned guard's job when called is to leave his post for `summon.stay`, the summons without a lover. This is one role-job line, like the driver's camera block.
- **B without the driver.** Loop the pickup camera (T1), tip the rivals, and the Enforcer takes Vale at the bay.
- **Both fronts, committed late.** Pressure Inez ($0) and Pay the driver ($70,000), or Help Inez twice ($80,000) and Pressure the driver with Muscle ($0). Either fits the purse.

**Optional gains that land in the debrief:**
- **Fight ledger.** Turn §07's safe-fight list into one rulebook list, shown on the briefing and the end card:

  | preparation | effect on the final fight |
  |---|---|
  | turned driver | −1 rival |
  | earlier false tip | −1 rival |
  | helped Inez, or rivals read (S2) | warning |
  | car set (E2) | agents start in cover |
  | guard never saw the cell | he joins against the rivals |

- **Clean exit.** Quiet witnesses (F2) and pulled logs (T3) can bring the investigation to "little".
- **Inherit.** A turned keeper tips needs at the next mission's start (§08, existing rule). A second contact left unexposed gains a bravery step. A public Help earns Hero standing.

**Keeping it free of scripts:**
- The jobs read things the generator already makes:
  - a keeper per unit, and her needs known;
  - witnesses, meaning whoever stood within 13 m;
  - cameras from the plan;
  - the getaway car.
- The ledger is a rulebook list. The mission adds no triggers.
- **Two timing and placement changes:**
  - the rivals arrive at about **2:10 instead of 3:10**, so reading and tipping them works on route A, whose fight starts at 2:38–3:34;
  - the patrol sweep crosses the service lane from **5:40 to 6:20**, so B needs the Tech.
- **Two checker rules** for every generated mission:
  - **M-idle-1:** on every reference win, each role gives at least one order that changes the outcome, the fight ledger or the debrief.
  - **M-idle-2:** on the informed reference route, no role idles more than 90 s before the final fight, except dead air that N can skip.
  - Keep T2: the minimal informed route must stay faster than loud. Preparation is optional and buys a smaller fight, not speed.

## 4 · Time control

"Skip to the next event" already exists. **N** (`HandoverHost.NextEvent`) runs the clock to the next log line or scheduled mark.
- **For an automatic skip when all four wait:** B's 75 s before Vale's 6:10. Replays are unaffected, because a skip only runs ticks.
- **Against:**
  - Dead air is 0 s on A and loud, and 7 % of pre-fight time on average. Agents idle 43–62 % each, which a skip can't touch.
  - An automatic skip rewards the waiting route.
  - It hides routines, and watching the patrol pass is now the Scout's play.
  - Sky set real time as the default.
- **Recommendation:** no automatic skip. Keep N manual. When all four agents are idle, the status pill reads "N ▸ 6:10 Vale at car". That is HUD work for Glyph and Latch, with no rule change.

## 5 · Ranked recommendation

The changes that cut the most idle time for the least rule change. All idle figures after the change are estimates from the measured timelines plus the job times above, not sim runs. Sky approves the GDD changes; Keel owns the rulebook.

| # | change | roles | idle before the final fight, today → after (estimate) | GDD | rulebook / engine |
|---|---|---|---|---|---|
| 1 | **Muscle** (E1) | Enforcer, Fixer | Enforcer on A (Pressure) 47 → about 15 %; on B 81 → about 70 % (it replaces the Pay); Fixer unchanged, but $70,000 freed for a second front | §03 Enforcer verbs · §04 approaches | `turn.pressure` fit condition and a memory fact |
| 2 | **Fight ledger**: Set the car (E2) and Read the rivals (S2), with the existing −1s shown | Enforcer, Scout | Enforcer −30 s on every route (A Help 61 → about 45 %; B → about 60 % with #1) · Scout −20 s | §06 · §07 final fight · §10 briefing and end card | `fight.safe` becomes a list; one new verb (`set`); a Watch target "rivals" |
| 3 | **Ask the keeper** (F1) | Fixer | Fixer on A (Help) 42 → about 28 % (fills the opening wait); opens the guard as a contact | §04 knowledge · §08 economy | `keeper.tips` fires mid-mission as well; orders on any simulated person |
| 4 | **Quiet a witness** (F2) | Fixer | Fixer on A (Pressure) 62 → about 40 % (the 35 s after the call); on B little by itself, since only one person saw the Pay | §05 counters (already [S]) · §08 debrief | event `witness.quiet` ("sealed" in the talker test); shares #3's targeting |
| 5 | **Every route needs every role** (Waymark): loop the pickup camera (T1), patrol over the service lane, rivals at about 2:10, checker rules M-idle-1 and 2 | Tech, Scout | Tech on B 100 → about 65 % (walk, loop, reroute; lower if he holds the loop) · Scout on A (Help) 58 → about 15 % (read and tip in its 100 s gap) | §07 timeline and route B | `mission.json` timeline, cameras and patrol; one loop target list; Waymark's checker |

**Expected idle before the final fight with all five** (estimates):

| route | Scout | Fixer | Tech | Enforcer |
|---|---|---|---|---|
| A (Help ×2) | 58 → ~15 % | 42 → ~20 % | 5 → ~5 % | 61 → ~45 % (~20 % if he muscles a second contact) |
| A (Pressure) | 44 → ~20 % | 62 → ~20 % | 7 → ~5 % | 47 → ~15 % |
| B | 45 → ~25 % | 59 → ~30 % (if the freed $70,000 buys the A backup, F3) | 100 → ~65 % | 81 → ~60 % |

On B, the 75 s of dead air stays, and N skips it.

**Open issues:**
- **B stays the patient route.** The Enforcer still waits for 6:10. Only E4 (Take a lookout, [L]) or a decision to accept B as the long con with N closes that.
- **Pressure gets stronger.** Muscle is a Menace-leaning tool and makes Pressure stronger. Suture should watch T2, and Motive owns the pressure on Talkers.
- **The cast decides whether the keeper helps.** F1's reach depends on the cast: in r016 the café keeper knows the guard's need but not Inez's.

## Evidence
- `bun tools/missions/idle/idle.ts`: 18 routes, all "replay hash matches", exit 0. Output: `briefs/012/idle-cell-study/all-routes.txt`.
- `bun tools/missions/idle/idle.ts briefs/012/idle-cell-study/prepared-a.json`: success at 4:54, 3 rivals, Trail 11, cash $30,000. Output: `briefs/012/idle-cell-study/prepared-a.txt`.
- `bun run typecheck`: clean. Engine at HEAD 23527c3. Keel's in-progress working-tree edits (rulebook, `Rules.cs`, `Geo.cs`, `sim/cli/Program.cs`; the cones work) landed during the study. A re-run over them gives identical output (`diff`: no change), and this study edited none of them.
