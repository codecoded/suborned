# Brief 096 · Keel's report: Motive's misses, engine 0.43.0 (2026-11-05)

Branch `t3/7f898491` (this worktree, from `work/096` at 7dad862c). Motive's misses page (briefs/096/motive/misses.html, misses.json, probes
in misses-probe/); Sky 2026-11-05 "all recommended". GDD text for Aster: gdd-text-misses.md (gdd-text-lend.md's F2 line corrected too).
Bands: bands-misses-after-200.txt (before: bands-lend-after-200.txt, engine 0.42.0; the engine had not changed since).

## What changed (engine 0.43.0), rulebook first

| Rule | Rulebook | Engine |
|---|---|---|
| V3: "fired" is known only once the cell is placed | `security.unclaimed.fired` "placed" ("always" = 0.42.0); ai.intent's words | GameIntent.HasFoe: `Gun.Fired && !(FiredPlaced && GangPending)`. A shot security sees places the cell (V2 a), so it covers "saw the shot". Police and robots only (rivals never had "fired"). |
| L8: police clock 75 s | `split-police-s` 60 → 75 | value only; the countdown is `Game.SplitPoliceAt` as before |
| G1: a Go waits for a Board under way | new `car.go.waits` "board" ("none" = 0.42.0), schema rulebook.schema096m.ts | GameGoWaits.cs: a Go on either car while an agent's Board is under way waits for it and lands the tick it lands (Game.StepInner, after the Boards). Boxed, held, gated and gone still refuse at once; car-empty and car-no-driver wait. `LeftBehind()` counts the climber aboard. Shot down or called off mid-climb: the Go lands then, or logs `go.missed` with the refusal. Hashed only while a Go waits. |
| F2's words | `rivals.block.rule` | none (Motive: the code was right) |
| Pay on a rival needs a Watch first (rivals page pay.needs, settled: the page says "a Watch first (any agent, as Pay does)") | `rivals.turn.needs` "watched" ("none" = 0.42.0) | CrewTurn: refused `needs-routine` until `RivalsWatched` (a Watch on any member or their car). |

Host reads (Latch):
- **`Game.CrewActQuery(agent, "pay" | "mislead", target)`**: Pay and Mislead on a member before the click, answered exactly as Issue would
  (only-fixer, only-scout, target-unknown, agent-down, agent-gone, enforcer-inside, in-car, crew-gone, person-down, person-away,
  crew-fighting, crew-walking, refused-before, needs-routine, no-cash, too-far, no-walk; a busy agent gets the order's own refusal, else
  agent-busy, as LearnQuery). Read-only. HostCrewTurn.CrewLock can drop its copied checks and ask this.
- **`Game.LentCarWatcher()`**: who would see Popovic's car leave now ("" none, or not lent): the lent car's Go card's "+1 T if seen".
- **`Game.GoWouldWait(car)`, `Game.GoWaiting(car)`** ("car" or "car:official"): someone is climbing in, so a Go would wait / a Go waits.

Engine version suborned.sim/0.43.0 (Debrief.Version). DP1's drive session re-recorded. Plan runner (sim/cli/Adaptive.cs): a condition
`policein<:s` (the countdown on the HUD has under s seconds left); `rivals.block` and `rivals.back` kept as digest marks.

## Scenarios (Motive's list)

096-VET-5 (V3: a holstered agent who fired, the gang shooting unplaced: neither the guard nor the patrol counts him a foe for it),
096-VET-6 (the Tech fires on the entrance camera in its sight: placed, "fired" counts again), 096-LEND-13b (split-police-s 75; a late
vale.out meets the 8:10 close first), 096-LEND-22 (G1: two aboard, the Tech climbing in; Go waits and the car leaves the tick his Board
lands, the card left nobody behind), 096-LEND-22b (G1's twins: an empty car with nobody climbing in refuses car-empty; the climb called off,
the waiting Go lands as go.missed), 096-FIX-5 (F2: broken before the final fight, the crew drives off holstered and out of every fight and
comes back; at security level 0 so the seal doesn't end the run first; the re-block itself is a reported line since the seal can still come
first), 096-FIX-5b (broken in the final fight: never back), 096-PAY-6 (Pay on the lookout refused needs-routine by the query and the order;
after the Fixer's Watch, taken), 096-Q-1 (CrewActQuery against Issue), 096-Q-2 (LentCarWatcher is who the Go charges). The LEND-22 carry
case (4 s Board carrying) is the same rule and isn't staged separately: getting a downed agent there takes a staged shooting.

Updated for Pay's Watch: DealLookout's "pay" and 096-RIV-14 Watch first. `suborned-sim check`: 459/459 ok, bit-for-bit replays (30 s).

## Waymark's plans (written by Keel, as Waymark)

- **The late Go and the carries read the countdown** in every lend plan (quiet-b, quiet-b-thinned, quiet-b-lookout-ignored, quiet-b-taped,
  drive-round, drive-early, veteran, prepared-loud-lend): `go-late` at `policein<:5` (5 s left), the carries while 25 s are left. The
  countdown already stops at the 8:10 close, so the 8:05 Go Motive asked for is the same rule: no number in a plan.
- **quiet-b-thinned**, rewritten as the band means it: after the driver's Listen the Scout waits at the square and Listens to rival-2 as he
  posts (only if that starts by 3:55); the Fixer, once the Help has turned the driver, Watches him and Pays him if he is a Mercenary; the
  Scout then misleads the lookout, walking if he's done before 4:00, else running (a player who sees the clock runs), the Mislead given
  once he's near; plain quiet-b if rival-2 is Loyal. Note: temperaments are drawn by id, so rival-2 and the lookout are Mercenaries every
  game; Motive's 122/78 split doesn't occur on this engine. What varies by seed is when rival-2 posts (3:23-3:53); the late posts are the
  seeds the first version lost.
- **veteran-drive** (new variant, report only; the veteran stays as written): once the rival car is burning and the hold is over (4:38),
  the Tech goes to the cabinet for a second Reroute while the Fixer runs back to our car and drives it to the service drive, so the turned
  driver stops at the kerb, the Take lends Popovic's car there, and the three wait beside it. Three timings tried; the best wins 17.5 %
  against the veteran's 61 %. Our car is refused the Drive at the square until the rival car is gone (boxed in), the bay is within the
  scene's 30 m during the hold (placed if seen), and on the drive the patrol back from the first Reroute reports our car (41 seeds placed by
  car-reported), else it gets there after 5:53 and the driver parks in the bay. Not a rule problem: it needs a better plan than mine, or it is
  the price of the veteran's way. Motive's call.
- **The mission check's reds.** MC-0 and MC-13: both scripts were composed before the lookout existed and lost to him (0.41.0), which is
  not what either tests. They are **not meant to lose that way**: each now deals with the lookout as route B does and tests its own thing.
  careless-scout-idles-pickup is today's route B (the lend) plus the Scout idling in the pickup camera's view: reported, a win at Alert 1
  against route B's Alert 0 (MC-12 holds). fight-loud-wait-yard takes the drive-round's opening (our car on the drive by 2:45, so the rival
  car blocks the drive's mouth), shoots the camera as Popovic's car stops at the kerb, the cell waits in our car, the Take lends Popovic's
  car and its Go is refused car-boxed; the player withdraws (Alert 2). MC-13 now reads car-boxed as held as well as car-held (since 0.41.0
  the rival car's box is the final fight's gate, Motive K3), and on the lend reads Vale aboard at lend.lent. MC-11 and MC-14 read the way
  free at blocker.out, else car.unboxed under the block, else, for a lend win, the crew's spring (L7: the fight comes by rule when the
  lent car leaves) (tools/missions/check/blocker.ts). MC-11 also counts a rival's hold while his car boxes the stand in (rivals.block to
  car.unboxed) as coming for the car: the crew blocked it on arrival (Sky 2026-11-04). All MC-* now hold; KNOWN_RED in check.test.ts is down
  to R-7 and the band reports Motive and Pulse own.
- **Recomposed on 0.43.0:** route-b (G1: its Go waits for the last Board, 1 s sooner, 7:46.2), route-a, route-a-disable, route-a-pressure,
  route-g-fast, route-g-kill, careless-g-disable (the pre-change engine recomposes these five too: they were already stale on 0.42.0;
  on 0.43.0 G1 brings each Go to the tick its last Board lands), and the two twins above. Mission record **c053** pinned to the engine
  commit; replay exact.

## Bands, 200 seeds (before = 0.42.0, bands-lend-after-200.txt; after = bands-misses-after-200.txt)

| line | band (Motive's misses.json) | before (0.42.0) | after (0.43.0) |
|---|---|---|---|
| B-7 quiet-a final fight | median 10-40 s, p95 < 90 s | 13 s (MISS at 15-40) | 13 s, p95 42 ok |
| L96 A-fight | median 10-40 s | 13 s (MISS) | 13 s ok |
| L96 A | 90-97 % | 96 % | 96 % ok |
| L96 B (quiet-b) | Popovic out >= 85 % | 100 % (MISS at 85-95) | 100 % ok |
| L96 B-all | all four out 60-80 % of wins, <= 0.6 captured a win | 56.5 %, 0.88 (MISS) | 69 %, 0.56 ok |
| L96 B-thinned | out within 3 of B; all out >= B + 10 | 98 %; 42.3 % vs 56.5 % (MISS) | 100 %; 82.5 % vs 69 % ok |
| L96 G (quiet-g) | >= 75 % | 87 % (MISS at 70-82) | 87 % ok |
| L96 loud (prepared-loud) | 50-70 % | 66.5 % | 67.5 % ok |
| L96 loud-lend | report, expect 30-40 % | 35.5 % (MISS at 50-70) | 35.5 %, all out 43.7 % of wins |
| L96 veteran | Popovic out 40-70 % | 0 % (MISS at 15-35) | 61 %, all out 3.3 %, 2.88 captured a win ok |
| L96 veteran-drive | report | (new) | 17.5 %, all out 0 %, 2.94 |
| L96 drive-round | 5-20 % | 13.5 % | 13.5 % ok |
| L96 B-ignored | <= 5 % | 0 % | 0 % ok |
| L96 B-idle | median <= 30 s | 22 s | 22 s ok |

Still missed, owners unchanged: R-7 (quiet-a's call and enter, quiet-g beside-guard, Waymark and Pulse), B-2 thin-quiet 93.5 % (60-90),
B-8 quiet-g idle p95 98 s, B-11 variety (drive-early, drive-round, lookout-ignored, taped, quiet-b), B-14, B-16 drive-early. B-11
quiet-b-thinned is in band now (12-24 s).

## For Latch, Fable, Glyph, Chime

- **Latch:** the countdown reads 1:15 from data (Game.SplitPoliceAt; HostLent's PoliceLeftS and PoliceSpanS already read it), nothing to
  change. HostCrewTurn.CrewLock can be replaced by `CrewActQuery` (it also returns the new needs-routine refusal on Pay); the lent car's Go
  card's "+1 T if seen" from `LentCarWatcher()`. G1's card: while `GoWouldWait(car)` the Go button stays live with the climber counted
  aboard (`LeftBehind()` already leaves him off); after the click `GoWaiting(car)` until it lands.
- **Fable (placeholders in text.json silentLogs, marked PLACEHOLDER):** `go.waits` (card: the car waits for him climbing in, then goes),
  `go.missed` (card: the waiting Go didn't land; the refusal's words follow). Refusal `needs-routine` now also on Pay on one of the crew:
  today's line "Learn {who}'s routine first: Watch them." reads right; Fable may want a crew-specific line. No other new codes.
- **Glyph:** G1's Go card (the climber counted aboard, a "waits for him" state); the 1:15 plate; the "+1 T if seen" on Popovic's car's Go.
- **Chime:** a cue for the car pulling away the tick the last one is in (G1), if the Go click and the leave are now apart by up to 4 s.

## Checks

- `suborned-sim check`: 459/459 ok, replays exact.
- `bun run missions:check bands handover --seeds 200` (through `bun run guard`): 1:12; the table above.
- Mission check on the tree: HOLDS; every MC-* green.
- `bun run typecheck`: clean.
- C# tests (`dotnet test sim/Suborned.Sim.Tests -c Release`): 3135/3135 passed, 6:13. (A Debug run was stopped at my 20-minute limit; Release is
  the one to use.)
- `bun run test` (through `bun run guard`): 8:40, 61 fail and 5 errors against the pre-change tree's 60 and 5 (7:45). The 60 are the same on
  both (out/ files a fresh worktree lacks: people, kit, skyline, dialogue, levels). The one new one was mine (tools/lint/comments: code left
  in a comment in fromBands.ts), fixed; the lint and the check tests then pass (check.test.ts 36/36, the headless Handover with the new
  KNOWN_RED and the c038 replay included).
- Mission record c053, pinned to f82f2c64: HOLDS; `missions:check replay handover c053`: REPLAY OK.
