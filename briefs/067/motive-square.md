# Brief 067 · Motive: the square after the planters, the yard patrol, GDD lines

2026-10-26, for Aster. Measured on a scratch copy of engine 0.33.1 with my pace switches (1.8 m/s, the run rule, `mod-running` 375) and K3, on the data round 1 committed (71bf4032: A the yard patrol, J the robot out, K the planters out, L the car points), with the patrol facing Vantage's points need. Nothing in `sim/`, `mission.json` or the adjust file was touched; probes ran under `bun run guard` in `out/scratch/motive/067sq/`. Scripts and results: `briefs/067/motive/sq/`.

## 0 · Rulings
1. **(a) The square fight is not fair as round 1 left it, and cover is not the fix.** Two rule gaps the planters hid break route A's fights and route G kill's board and go. With the two rules below (and K3 corrected) the bare square is fair. **No cover goes back, so there are no spots for Vantage.**
2. **(b) The hold is entered only on purpose (M067-SQ1).** A hostile's answer to fire never ends inside the car's 8 m hold or on its exit lane, and a walk to a post goes round the hold. MC-11 gets a third clause, read from a new log code.
3. **No stall (M067-SQ2).** A rival with nobody in reach for 1 s (today's `ai-engage-lapse-s`) advances again, on an operative, not on the car's point.
4. **K3 (a) is amended (urgent for Keel, who is building it now):** the rivals walk to the car only while it is **held** with nobody outside. Unamended, it turns a free car into a loss in the second before Go.
5. **The yard patrol: Aster's calls stand, and the numbers back them.** The MC-3 change is in §5.3.
6. **GDD v0.26.0 lines:** §6.

All rules are [S]. Values are in `briefs/067/motive-values.json` (M067-K3 amended, M067-SQ1 and M067-SQ2 added).

## 1 · (a) The square after the planters

### 1.1 What K did
Per-operative jitter ensembles: 8 samples, each operative 0–2 s late; wins of 8 (`sq/results/ensembles.txt`).

| plan (re-composed for 1.8) | planters back (`nok`) | round 1 (`k`) |
|---|---|---|
| route A, fight it out (`ra-h3`) | 8/8 | **0/8**, the fight never ends |
| route A, board and go (`route-a-board`) | 8/8 | **0/8**, rivals reach Vale |
| route A, Pressure, board (`route-a-pressure-board`) | 6/8 | **0/8**, rivals reach Vale |
| route G kill, board (`rgkb-h3`) | 8/8 | **0/8**, rivals reach Vale |
| route G kill, fight it out (`rgk-h3`) | 0/8 | 0/8 |
| route G, route G fast board | 8/8 | 8/8 |
| loud (`loud-x4`) | 0/8 (A, J, L moved it, not K) | 8/8 |

### 1.2 Why: two gaps the planters hid, and one in my K3
- **A walk to a post crosses the hold.** rival-4, the late car's man, walks from the rival car to ambush-4 by the café, straight past our car.
  - In `route-a-board` the whole cell boards a free car at 4:11.5–4:15.5 (blocker down at 4:07.9).
  - rival-4 is inside the hold at 4:15.0 (7.3 m) and holds it.
  - run-sq-south used to bend that walk away from the car.
  - Picture: `sq/maps/joiner-before.png`.
- **The fight stalls.** In `ra-h3`, after 4:23 the three rivals left hold 14.6–17.6 m from the car.
  - The nearest operative is 15.3 m away, just past the pistol's 15 m effective ring.
  - Two of five are down, so the crew never breaks and `fight.over` never comes. The Enforcer waits with Vale until 8:30.
  - A rival in `hold` never advances again (only police and blockers walk back to a stand). A rival's advance with no cover stops at 90 % of 15 m from the car's point, not from the operatives standing round the car.
  - The planters were the cover they advanced to. Picture: `sq/maps/stall-before.png`.
- **K3 (a) presses a free car.** It started the rivals' walk the tick Vale boarded (4:15.5), while the car was free. That put rival-4 in the hold before the plan's Go a second later.

### 1.3 Cover doesn't fix it; tall cover makes it worse
Candidates were added as block structures in scratch and run through block → site → cover → site.

| site | CV-2 (safe) | fight it out (`ra-h3`) | board (`route-a-board`, `rapb`, `rgkb-h3`) | `route-g-fast-board` |
|---|---|---|---|---|
| kiosk 1.6 × 2.6 m, 2.5 m high, at (20.0, −43.2) | 5/5 | **0/8**: a rival behind it can't be seen, so nobody fires | 8/8, 8/8, 8/8 | 1/8 |
| bench pair 0.7 × 4.8 m, 1.05 m high, at x 20.5, y −45.2..−40.4 | 5/5 | 5/8 | 8/8, 0/8, 0/8 | 0/8 |
| the bench pair with the rules on | 5/5 | 8/8 | 8/8, 0/8, 0/8 | **0/8** (cell down) |

- Every placement just moves the knife-edges.
- The square's edges can't shield the south posts. The car stands on the square's own kerb, so any piece on an edge lies along the line of fire (Vantage found the same).
- A piece that shields must stand in the open between the posts and the car. That is where the planters stood.

### 1.4 With the rules, the bare square is fair
The probe is `sq/rules-probe.patch` over `engine-probe.patch`. Ensembles on round 1's site (`ensembles.txt`, `op-k-r7c`):

| plan | round 1 today | round 1 + SQ1 + SQ2 + K3 on a held car |
|---|---|---|
| `ra-h3` | 0/8 | **8/8**, 4:37.3–4:41.2 (fight 4:01.1–4:22.9, 22 s), Standing 3 |
| `route-a-board` | 0/8 | **8/8**, 4:15.5–4:17.2, Standing 2 |
| `rgk-h3` | 0/8 | **8/8**, 5:56.9–5:58.9 (fight 30 s); its first win at 1.8 m/s |
| route G, route G fast board, loud | 8/8 | 8/8 |
| `rgkb-h3`, `route-a-pressure-board` (Vale walked in at the spring) | 0/8 | 0/8 |
| `rgkb-bo` (`rgkb-h3` walking Vale in once the blocker is down) | - | **8/8**, 5:36.1–5:38.1 |

- **The last two walk Vale in at the spring.** That walk crosses the blocker's lane walk, and the blocker fires as he walks (Sky's decision A, §6.4).
  - Walking in once the blocker is down wins `rgkb-bo` (8/8). `route-a-board` already does it that way.
  - The Pressure board still loses. Pressure means no warning from Inez, so all five rivals are up: that is the Pressure route's price, and Waymark re-composes it. That family was already on his c038 list at 1.8.
- **Mission check** with these rules (`sq/results/cand-k-rules.txt`):
  - MC-1: 3 quiet routes (A, B, G).
  - MC-2, MC-4, MC-5, MC-6 and MC-8 to MC-12 hold. MC-11: 9/9 posts clear, 12/12 reference wins free at Go.
  - Still failing, all on the re-compose list: MC-0 (the manifest's expectations), MC-3 (the patrol line, §5.3) and MC-7 (`careless-reporter-not-stopped` no longer composes).
- **The choice at the car still reads.**
  - Board and go: 4:15.5, Standing 2.
  - Fight it out: 4:37.3, Standing 3 (+1 against feared rivals).
  - Both fights are inside §07's "up to 60 s".

### 1.5 Ruling (a)
- **The square needs no new cover. CV-2 holds at 3/5** (the café terrace wall shields ambush-2, -4 and -5).
- **The two south posts stand in the open.** That is a cost for the rivals the player can read: they are easier to hit, and T8's "no cover" names it.
- **Later dressing on the square must stay low and out of the fight.**
  - Low means 0.9–1.2 m: it shields and never hides.
  - Keep it out of the line between the south posts and the car, or re-measure.
  - Nothing tall (a kiosk, a booth) within 20 m of the car's stand: it hides a rival whole and stalls the fight.

## 2 · (b) Step-out spots: the hold is entered only on purpose (M067-SQ1) [S]
- **Rule.** In the final fight, a hostile's answer to fire never ends inside the getaway car's hold (8 m of the car) or on its exit lane. Answers to fire are:
  - a rival's step out of a shooter's line (the shielding cover, else the nearest free spot within 8 m with no line to him);
  - a rival's step to cover while engaged;
  - a police body's cover or close.

  If nothing outside serves, the body stays where it stands.
- **A walk to a post goes round the hold.** It takes the grid route with the hold's ground closed, unless the post or the walker is already inside the hold.
- **Exceptions.** These come for the car, so they may hold it:
  - the blocker (his lane is the road);
  - the rivals walking to a held car with nobody outside (K3 a);
  - an advance on the car or on an operative.
- **Measured.**
  - The walk round the hold, with K3 on a held car (§4), turns `route-a-board` from 0/8 to 8/8. rival-4 keeps 8.9 m or more from the car (`sq/maps/joiner-after.png`).
  - Neither change does it alone: K3 on a held car alone loses at 4:19.8; the walk with K3 as first given loses at 4:19.9.
  - The answer-to-fire part moved no outcome in this set. My tree067 run had a kite step end in the hold (motive.md §3), so the rule stays as a guard.
- **Keel (log):** log `car.held.<how>` when a body first holds the car. `<how>` is one of `blocker`, `press`, `advance`, `answer`, `post`. These are new codes, so the hashes move; Keel re-records anyway.
- **Waymark (MC-11, third clause):** "no played script logs `car.held.answer` or `car.held.post`". The value reads "n holds, all by bodies that came for the car".
- **Optional static check (Vantage's overlay):** a route round the hold exists from each stand's rival stop (`rival-drop`, `rival-tail`) to each of its ambush spots.

## 3 · No stall (M067-SQ2) [S]
- **Rule.** In the final fight, a rival with no foe in his effective reach for `ai-engage-lapse-s` (1 s) advances again, from `hold` as from `engage`.
  - He advances on the nearest operative standing on the street that he has cause to fire on (the Vale holder counts).
  - He goes to the cover that shields him from that operative, else to his stop point of that operative (`ai-stop-reach-pct`), never of the car's point.
  - Police and the blocker hold their posts, as built. No new value: the rulebook already says "stays stopped 1 s before it advances again".
  - This changes §06's "they don't advance on a sniper" for the final fight only. Before it, the pre-fight crew still steps out and walks back.
- **Measured.**
  - `ra-h3` and `rgk-h3` go from 0/8 to 8/8. Picture: `sq/maps/stall-after.png`.
  - Leaving rivals at cover where they stand (only the ones in the open advance) brings `rgk-h3`'s stall back.
  - Advancing on an operative with no cause makes rivals walk up to him and not fire. That is why the rule names the cause.
- **Pulse:** the motion is yours. `AdvanceOnOp` in the patch is only how the probe did it.

## 4 · K3 (a) amended [S], for Keel now
- **Rule.** In the safe fight, with Vale in the car, **the car held** and no operative standing outside it, the rivals walk to the car and take Vale. A free car with the cell aboard waits for Go.
- **Measured.**
  - `route-a-board` is 0/8 unamended and 8/8 amended (with SQ1 and SQ2).
  - BAL-YARD-AT-WILL is a held car (rival-3), so it still fails rivals-reach-vale: 7:43.0 on round 1's site, the same either way.
- `motive-values.json` M067-K3 carries the amendment and its reason.

## 5 · The patrol in the service yard (Aster's calls)

### 5.1 No "way out seen by the patrol" fail: agreed
The yard patrol already bites everything that waits or carries in the yard:
- route B's Enforcer waiting at 'Service lane' is reported at 1:53.4 (motive.md §3);
- the Scout and Fixer posted at 5:55 are reported at 7:12 (motive.md §3);
- the patrol breaks into the yard fight (motive.md §3);
- route G's kill carry is seen at 2:59.8 (Alert 2), and the patrol then shoots the Enforcer (`sq/results/reroute-yard.txt`).

It doesn't bite an escort walking out: machines never see the grip, and a walker's clock drains. So the street rule reads simply: don't wait or carry in the yard while it sweeps.

### 5.2 The Reroute: optional on A and G, needed on B and on a carry into the yard
Round 1's square with the rules (`sq/results/reroute-yard.txt`). Without the Reroute, plain A and G win at the same time and 1 T cheaper:

| plan | with the Reroute | without |
|---|---|---|
| route A (`ra-h3`) | 4:37.3, 5 T, A 0 | 4:37.3, **4 T**, A 0 |
| route A, Disable (`rad-h3`) | 4:37.3, 6 T, A 1 | 4:37.3, **5 T**, A 1. The patrol answers the dead screen at 0:41.6, is wary of the Tech at 1:30.0, and stands down at 1:38.2. No report. |
| route G (`rg-ff`) | 3:59.6, 6 T, A 1 | 3:59.6, **5 T**, A 1 |
| route G kill, board (`rgkb-bo`) | 5:36.1, 2 T, A 1 | **loses**: carry seen 2:59.8, Alert 2, Enforcer down 3:53.4 |
| route B (`rb-rr`) | 7:32.2, 4 T, A 0 (Reroute at 5:00, lands 5:45, away to 7:40) | reported loitering, 9 T, A 2 (motive.md §3) |

**Waymark:**
- Plain A and G references don't take the Reroute (informed play doesn't pay 1 T for nothing).
- Route B and its two yard twins take it by 5:05. Route G's kill variants take it before the carry.

### 5.3 "Entry seen by patrol": exactly how MC-3 adapts
- **The fail line stays general (§07).** On round 1's site the patrol never watches the entry:
  - Vantage measured 'Office entrance' at 0 % of the round.
  - I composed six honest chains (`fesp-a`..`f`: a Disable at 1:20–2:05 in place of the Loop, no Reroute). The patrol answers the dead screen and walks to the camera. It arrives from the north facing west and never has the public door in its cone, and every Enter landed.
- **Waymark:**
  1. `contracts/missions/routes.schema.ts`: a `fails` entry gains an optional `needs: "patrol-watches-entry"` (the schema is strict, so it is a schema change). `routes.json` sets it on `entry-seen-patrol`.
  2. The fact comes from the engine (Keel): `patrolWatchesEntry` is true when the sweep's cone sees the public door (`S.GuardPost`) at its eye in at least one second of its round, or at the reroute wait. That is `Game.SweepSeesEntrance` over one period. It goes out as a field the check can read, for example `suborned-sim facts`.
     - A TypeScript port of the cone is fine if it is tested against the engine on both sides: true on 0.33.1's forecourt site, false on round 1's.
  3. `judge.ts` MC-3 counts a line with `needs` only when the fact holds. Otherwise it lists the line as "not on this site (no patrol watches an entry)" and leaves it out of the count: value 7/7. When the fact is true, a played script must reach it, as today.
  4. `fail-entry-seen-patrol.json` gets `pending: "the patrol sweeps the service yard (brief 067 A) and watches no entry; it plays again when patrolWatchesEntry is true"`, so MC-0 stops expecting a fail it can't reach.
- A generated mission then ships only the fail lines its site can reach (Waymark's "every fail can happen").

### 5.4 §07 wording the move touches
- **Disable variant (the line I flagged):** "a Disable in place of the Loop spends the mission's first incident, so the guard's next missed mark, if the cell is still there for it, becomes the second (Alert 2, a loud final fight)".
- **Route A:** "then reroutes the patrol (optional: it sweeps the yard, not the door)".
- **Route G:** "Tech loops the entrance camera (and reroutes the patrol before a body is carried into the yard)". Kill variant: "stash in the yard's container or through the service door with the patrol rerouted".
- **Route B:** add "the Tech reroutes the patrol from the cabinet by 5:05 (it sweeps the yard; rerouted, it stays out of the yard fight)".
- **Places:** "the service door ... the quiet back way, out of sight of the public door, the camera and the guard's cone; the patrol sweeps the yard in front of it, and the Tech's Reroute clears it".
- **Loitering:** add "nobody waits in the service yard while the patrol sweeps it".

## 6 · GDD v0.26.0 lines (one each; Aster places them)
1. **Pace and running** (§03, §05; M067-B1, B2):
   - §03: "Every op walks at 1.8 m/s, brisker than the city's 1.4, and runs (double-click, or G on a moving op) at 3.6 m/s; a run is seen and suspicious, never an Alert by itself (§05). Any op can carry a body (the Enforcer at the walk, the others at half of it)."
   - §05 running: "a watcher that keeps a clock notices a runner at once (its clock jumps to the noticed mark) and counts each second of the run in its sight as standing still, so a long run in sight makes it wary and a longer one is reported as a loiterer is (a found fault, A ≥ 1)".
   - §05 attention, "each second": add "· running in its sight → the clock jumps to the noticed mark, then +1 s".
2. **The held car** (§06 getaway; M067-K3 as amended):
   > Hiding in a held car loses: while the car is held and no op stands outside it, the rivals up and not broken (the blocker excepted) walk to it, firing as they walk, and the first within 1 m of its body takes Vale. Once every op is down, in the car or not, the mission fails. Go is taken only with a standing op in the car (a downed op can't drive).
3. **The shooting's Trail** (§06 cost, §02):
   > +2 T once per person, camera or car shot at (the first shot at it, hit or miss), +2 T per person hit (once), +2 T more if they go down (a dropped guard = 6 T)

   - Sky approved this on 2026-10-15: `gunfire-trail`, briefs/011/motive-fights.md Addendum Q1, the rulebook's src. Brief 027 tuning §10.6 (2) reads it per target.
   - §02's Trail list: "people shot at or hit by gunfire".
4. **Hostiles fire on the move** (§06 hostiles and the blocker; Sky 2026-10-21, brief 027 tuning §9.7 answer 3 and the §10 decision A, "both sides keep firing on the move"):
   - "advance to the nearest free cover point (made at build time) toward their objective ..., firing as they walk (at the walking odds)".
   - The blocker: "walks there without stopping, firing as he walks".
5. **The square's rules** (§06 answering fire, rivals; M067-SQ1, SQ2):
   > ... else the nearest free spot within 8 m with no line to him, never inside the getaway car's hold or on its exit lane (only the blocker, a rival walking to a held car, or an advance comes for the car); a walk to a post goes round the hold. Before the final fight they don't advance on a sniper; in it, a rival with nobody in his effective reach for 1 s advances again, on the nearest op he has cause to fire on.

## 7 · Handoffs
- **Keel** (next; urgent for K3).
  - **Inputs:** `motive-values.json` (M067-K3 amended, M067-SQ1, M067-SQ2), `sq/rules-probe.patch` (switches `PROBE_SQ` and `PROBE_K3=2`, over `engine-probe.patch`), `car.held.<how>`, the `patrolWatchesEntry` fact.
  - **Done-check:**
    - K3 (a) presses only a held car: BAL-YARD-AT-WILL still fails rivals-reach-vale; `route-a-board` wins on round 1's site;
    - SQ1-WALK, SQ1-STEP, SQ2-NO-STALL pass (values file);
    - the fact reads false on round 1's site and true on 0.33.1's;
    - `sim-check` replays identically.
- **Pulse:** the motion for SQ1 (the hide filter, the walk round the hold) and SQ2 (re-advance from `hold`, the stop point on an operative). How bodies move is yours; the probe's code is a sketch.
- **Waymark:**
  - MC-11's third clause and MC-3 as in §5.3.
  - Re-compose on the final site:
    - `rgkb` walks Vale in at `blocker.out` (`rgkb-bo`);
    - the Pressure board family;
    - plain A and G without the Reroute;
    - route B and its twins with it (`rb-rr`, `rbb-rr`, `crns-rr`, `csip-rr`);
    - `rgk-h3` as route-g-kill's fight it out (8/8 with the rules);
    - the manifest's expectations (route A at Alert 0).
- **Vantage:** no cover to place. Keep tall pieces at least 20 m from the square stand. The overlay check in §2 is optional.
- **Fable:** a line for a rival coming on when nobody is in reach (the probe logs `probe.advance`), and `car.held.<how>` if the history shows it.
- **Aster:** §5.4 and §6 for v0.26.0. Sky's call stands that the planters go and nothing replaces them.

## Evidence
All paths under `briefs/067/motive/sq/`; trees in `out/scratch/motive/067sq/`.
- **Build:** `mk.sh` builds the scratch tree from 71bf4032, with the patrol facing fixed in scratch and a `trace` command (`Trace.cs`). `site.sh` and `variant.py` make the site variants. tree2 and tree3 carry `rules-probe.patch`.
- **Results:**
  - `results/ensembles.txt` (from `results/ens-op-*.tsv`: `ens-op.sh`, `enstable.py`);
  - `results/cand-k.txt` (round 1) and `results/cand-k-rules.txt` (round 1 with the rules): the mission check, `cand.sh`;
  - `results/reroute-yard.txt` (§5.2, §5.3).
- **Pictures:** `maps/joiner-before.png`, `joiner-after.png`, `stall-before.png`, `stall-after.png`, `k.png` (`map.py`).
- **Single runs:** `probe.ts`; logs and traces in `out/scratch/motive/067sq/{logs,traces}`.
