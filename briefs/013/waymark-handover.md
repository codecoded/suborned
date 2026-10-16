# 013 D · The Handover with takedowns (Waymark, 2026-10-16, rev 4)

- **Brief:** `briefs/013-takedowns.md` part D.
- **Rules:** `briefs/013/motive-takedowns.md` (rev 4, §0.2), `pulse-witnesses.md`, `vantage-quiet-places.md`.
- **Engine:** Keel's rev 4 on the uncommitted tree (source `410b81e232ef`, 382/382), at security level 2.
- **Untouched:** nothing in `sim/`, `contracts/rules/`, `contracts/people/` or the GDD was edited.

**Verdict: the Handover SHIPS, recorded as `runs/missions/handover/c010`.**
- MC-0 to MC-9 all hold over 3,010 plays.
- `bun run missions:check replay handover c010` gives REPLAY OK: 3010/3010 plays land on their hashes.
- c009 (the rev 3 engine) is superseded.

## 1 · What changed

### Rev 4 (this pass)
| path | change |
|---|---|
| `routes/careless-guard-left-door-held.json` | **New careless fixture.** Route-g-fast's knockout, but the Tech leaves the cabinet once the guard is down and the Enter waits half a minute. The camera reads the guard at 2:17 and a unit holds the door. The Enter at 2:36 is refused (`door-held`), and the rivals have Vale at 8:30. |
| `routes/careless-body-found.json` | Keel's report-chain fix lets the fixture end naturally: Vale is at the held car and the district seals at 5:37. Before, the fixture walked the Enforcer at the police (`enforcer-down`). |
| `tools/missions/check/judge.ts`, `run.ts` | Open cases are priced at the rulebook's `case-trail-eq` (3, read from the engine's rulebook) in every total the checker compares: MC-2, MC-7 and MC-9. |
| `check.test.ts` | Fixtures for the new careless script and the case price. |
| `routes/routes.json` | The fixture, and notes. |

### Rev 3 (c009, still in place)
- **Route G:** `route-g-fast` is the primary; `route-g` and `route-g-kill` are variants.
- **Careless scripts:** the `careless` kind with a `twin`, in `contracts/missions/routes.schema.ts`.
- **Loud:** re-authored. `loud-fast` became the hold-fire variant.
- **Checker:** Trail is compared in total (mission + campaign). New conditions MC-7, MC-8 and MC-9. The composer takes `how`.

## 2 · The routes as played (c010)

- Trail is mission + campaign + 3 per open case.
- $ is what is spent of the $100,000.
- Idle is the Enforcer's, from `tools/missions/idle/`.

| script | result | ends | $ | Trail | Alert | Enforcer idle |
|---|---|---|---|---|---|---|
| **route-g-fast** (route G, primary) | win | **3:37.0** | 0 | 6 | 1 | **35.0 %** |
| route-g (Motive's G-KO, after the 2:30 mark) | win | 4:07.4 | 0 | 6 | 1 | 43.0 % |
| route-g-kill (service door) | win | 4:44.3 | 0 | 2 + 2 + 3 (case) = 7 | 1 | 37.4 % |
| route-a-pressure | win | 3:31.0 | 0 | 7 | 1 | 35.0 % |
| route-a (Help ×2) | win | 4:27.1 | 80,000 | 7 | 1 | 48.9 % |
| route-b (driver Paid) | win | 7:40.4 | 70,000 | 4 | 0 | 71.4 % |
| loud | win, nobody captured | 4:23.5 | 0 | 12 | 2 | 50.1 % |
| loud-fast (hold fire) | win, Scout captured | 4:24.0 | 0 | 8 | 2 | not measured |

Careless fixtures (MC-7, 5/5):

| fixture | result |
|---|---|
| seen by a camera | fail `entry-seen-camera` at 2:37 |
| seen by the crowd (5:31) | fail `sealed` at 8:40 |
| body found before the exit | fail `sealed` at 5:37 |
| reporter not stopped | win, but at Alert 2 and 10 + 3 T, against route B's 4 T |
| guard left at the door | fail `rivals-reach-vale`, after a `door-held` refusal |

**Route G and the loop.** The guard is found at 2:17 and brought round, so his statement (+4 T) now lands in the mission. The total, the time and the Alert are unchanged.
- Holding the loop until the Enforcer is out (2:45) only moves the find to 2:57: same total, same win. The route doesn't hold it longer.
- Leaving the guard at his door is right on route G: the Enter starts 6 s after the knockout, before any watcher reads him, and the exit is by the service door.
- The new fixture shows the case where leaving him is wrong: the door is still needed when he is found.

## 3 · Motive's estimates against the engine, and the margin

| route | Motive (§6, rev 4) | engine (c010) |
|---|---|---|
| G-KO | 5:00 · 6 T · A 1 · idle 42 % | 4:07.4 · 6 T · A 1 · idle 43.0 % (no carry needed) |
| G-fast | 3:37 · 6 T · A 1 · idle 35 % | 3:37.0 · 6 T · A 1 · idle 35.0 % |
| G-kill | 4:44 · 7 T-eq | 4:44.3 · 7 T-eq · 1 open case |
| route B | 7:40 · 4 T · A 0 | 7:40.4 · 4 T · A 0 |
| driver route | ≈ 8:20 | not playable (§5) |
| loud | (re-record at S2) | 4:23.5 · 12 T · A 2 |

**Margin (MC-9):**
- G-fast is still 6 s slower than A-Pressure (3:37.0 against 3:31.0) and 1 T cheaper (6 against 7). Neither dominates the other.
- G-kill, with its case priced, is 7 T-eq at 4:44. A-Pressure (3:31, 7) dominates it.
- **No G route beats every turning route on both time and Trail, so the knockout lever (6 → 8 s) is not needed yet.** At 8 s, I estimate G-fast at about 3:39, still 1 T cheaper (not played).

Idle: the informed route (G-fast) is 35.0 %, under the 40 % target. G-kill is 37.4 %. Motive's G-KO is 43.0 %, because it waits for the 2:30 mark.

## 4 · Loud (rev 3, unchanged)

The old loud failed "sealed" at both security levels, for two reasons:
- the 2:00 shot let the 2:30 missed check-in call a second response;
- the response unit now starts at the Office's doors.

The re-authored loud shoots just after the 2:30 mark, has the Scout and Fixer return fire until the final fight, and puts the Fixer at the pavilion's centre support (cv-094). A search over cover posts found 263 clean wins in 1,008 plays.

## 5 · Rules that make a route impossible or dominant

1. **The driver route is still not playable on r016**, although the pickup loop and three bodies to a door now work. I played two timings.
   - **Early: guard 29 at 2:31, the driver, then guard 74 in the lane at 4:52, all through the service door.** Fails on the incident rule: each staff victim's missed check-in is its own incident.
     - 29's 4:00 mark lands at Alert 1, unanswered, because the patrol is rerouted.
     - 74's 5:30 mark is a second incident, which `alert.byIncident` raises to Alert 2.
     - The unit then searches 30 m around the official entrance, and the service door is 17 m away: it opens the door and finds the bodies. The run goes loud, and the Enforcer is downed at 6:50.
   - **Late: guard 74 and the driver at 5:55, after the shift change, the Take at 6:10.** Fails on two other rules:
     - The outgoing guards stand at the official entrance until about 6:00, and Vale is on the street from 5:50.
     - Every witness tells a tie: the driver tells Vale, and Vale tells guard 65 (Loyal, a workmate). Guard 65 reports, and it lands at Alert 3. The fight goes loud, the cell is downed, and at 8:30 the Enforcer is still holding Vale.
   - **What I'd change (for Motive, Pulse and Keel to choose):**
     - A missed mark that is never followed by a found body could stay one incident per post rather than one per person; or the search radius could stay at the reported place rather than 30 m.
     - A witness on the street could tell a tie only on reaching a phone. Pulse §3.4 can be read that way; the engine counts any tie who is indoors as reachable, whether the teller is near a phone or not.
   - The Sky-facing fallback (the turned driver drives the car) is route B already.
2. **Patrol route to 29's post** (from c009, still true): at Alert 1 only the free patrol answers 29's missed mark, and its grid route from the sweep's west end runs through the service lane.
3. **Close to dominant:** none, with the case priced (§3).

## 6 · Engine findings for Keel (c009's list, now)

- The report-chain `Collection was modified` error is **fixed**: the body-found fixture now seals naturally, and route G's slack is 117 s.
- Still to look at:
  - `alarm.dispatches` is logged and sent again for every report that lands on the same incident.
  - The debrief tags a knockout or a kill as `guard-shot-away-from-waitress`.
- The takedown's camera shortcut was rev 4's change to make. `careless-takedown-seen-camera` still fails `entry-seen-camera` at 2:37.0, so either Keel kept the shortcut, or the report chain lands before the Enter. Either way the fixture fails for its reason.

## 7 · Open issues

- **HEAD-engine test.** `check.test.ts` "played headless (Keel's engine at HEAD)" fails until Keel's engine is committed, because HEAD rejects `how`. After the commit, record the check against that commit and pin the test to it.
- **`bun run verify` fails, but not on missions.** It fails on runs r012–r014 (the people checker's code changed: `tools/people`, `contracts/people`) and on the dialogue runs r017–r019. Both are for their owners (Keel or Pulse for the people runs, Fable for the dialogue runs). The plan checks and typecheck pass in it.
- The driver-route plan I tried is kept at `out/waymark013/route-d-late.plan.json`, not in the manifest.

## Evidence

- **Mission check:** `bun tools/missions/check/cli.ts check handover --engine tree --record "…"` → SHIPS, `runs/missions/handover/c010`. The replay gives REPLAY OK, 3010/3010.
- **Idle:** `bun tools/missions/idle/idle.ts` on the route G, A, B and loud scripts. Every replay hash matches.
- **Tests:** `bun test tools/missions` gives 45 pass and 1 fail (the HEAD-engine test, §7). `bun run typecheck` is clean.
- **Verify:** `bun run verify` fails on r012–r014 and r017–r019 only (§7).

## Handoff

- **Motive and Pulse:** §5.1, the incident rule for missed marks and when a witness can tell a tie. These decide whether the driver route exists on this cast.
- **Keel:** the repeated dispatches, the debrief route tag, and committing the engine so the HEAD test can be pinned.
- **Suture:** T9 is `route-g*` against `careless-*` in c010.
