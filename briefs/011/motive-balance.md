# 011 · Balance: loud against informed quiet (Motive, 2026-10-15)

**Question (Aster, from Suture's E-2, E-6, E-8).** What are the smallest [S] changes, in `contracts/rules/rulebook.json` or the mission, that make an informed quiet run faster or clearly better than loud, without making loud pointless? Grounding: §02, §05, §06, §07, §09; T2 asks "< 25 % shoot before 2:00; informed quiet runs finish faster".

**Method.**
- Keel is editing `sim/` and the rulebook, and the working-tree engine currently rejects its own people file. So every run below is on HEAD 846bfc5 (engine 5448655), pinned into `out/motive/engine/head/` and built in Release.
- Variants are copies of the rules under `out/motive/engine/var/`. Nothing in the repo's `contracts/` or `sim/` was touched.
- Each route is played at its **fastest**: every order at the first tick it can be given, and the entry scanned second by second. That's the frontier a T2 player who reads the clock can reach, so the numbers below are not the scenario scripts' own timings.
- Harness: `briefs/011/motive-balance/` (`fast.py` frontier, `window.py` entry windows, `suite.py` the 33 scenarios against HEAD).

## Today (HEAD)
| route, played fastest | finish | T | A | cash | note |
|---|---|---|---|---|---|
| loud: loop, reroute, 2 shots on the guard, enter | **3:38.1** | 6 | 2 | $100,000 | the Enforcer is hit (3 → 2) |
| loud without the reroute, shooting in a sweep gap | never wins | | | | the patrol sees the entry |
| route A with the Tech's idle reroute | 4:05.1 | 4 | 0 | $60,000 | |
| route A, no reroute | 5:09.8 | 3 | 0 | $60,000 | Suture's S10 |
| route B | 7:26.8 | 2 | 0 | $60,000 | 33 s before 8:00 |

**Why loud wins.**
- Loud's critical path is the Tech: loop 62 s plus reroute 45 s, so the first shot is at 2:15, followed by a 6 s fight.
- Route A's critical path is the Scout and Fixer: walk 18 s, Watch 52 s, Listen 42 s, Help 50 s, so the call is at 2:42. That misses the patrol's 2:13–2:47 gap by about 4 s, so route A waits for 3:53 or spends the reroute.
- Gunfire's costs never reach the Enforcer, who is inside for 38 s and leaves by the service lane:
  - the drawn rivals go to the Office front;
  - the response unit reaches the service lane about 58 s after it arrives;
  - the seal lands at 4:58.
- HEAD has neither of the two systems the GDD prices loud with: the §07 final fight (loud: police, no allies) and the crowd (violence seen → Alert 3).

## Proposal (two [S] changes)

### 1. `values.watch-s`: 52 → 20 (rulebook)
```json
"watch-s": { "v": 20, "unit": "s", "tag": "S", "src": "§04 Pay requires routine known (Watch); top-down test watchCafe was 52 s [B]", "proposed": true, "reason": "Motive balance (brief 011, Suture E-8, T2): at 52 s the informed route A calls at 2:42 and loud (3:38) beats it. At 20 s the call is at 2:10, inside the 2:13-2:47 sweep gap, and route A finishes at 3:33, 5 s before the best loud. Watch keeps its place in the chain (Watch, then Listen, then Help) and Sky's Pay lesson stays on the main path: after Watch the Pay card shows '?' and $90,000 worst case. Sky's steer to more real-time also argues for shorter prep timers." }
```

| played fastest | finish | T | A | cash |
|---|---|---|---|---|
| route A (call 2:10.3, enter 2:16.3, no reroute needed) | **3:33.1** | 3 | 0 | $60,000 |
| loud (unchanged) | 3:38.1 | 6 | 2 | $100,000 |

- **E-6 eases.** With no reroute, route A's entry wins in two windows: 2:16–2:46 (31 s) and 3:53–4:26 (34 s), 65 s in all. Before, it was one 34 s window. The reroute widens it further.
- **The 33 scenarios:** 5 end earlier (S05, S22, S23, S24, S25: their orders run sooner after Watch lands), and none fails a check.

### 2. The closing event: 8:00 → 8:30 (mission)
- In `contracts/missions/handover/mission.json`, the timeline entry `{"at": "8:00", "what": "rivals take Vale or car leaves = over"}` becomes `"8:30"`.
- The waitress role's shift runs to the close, so it becomes `0:00-8:30`. Waymark should re-run M-2: Inez's cast shift must still cover it.
- `tools/sim/site.ts` then writes `over: 510`.
- **Effect:** route B (7:26.8) gets **63 s** before the close, meeting §09's "≥ 1:00 before the closing event". At 8:00 it gets 33 s.
- **Why 8:30 holds up:**
  - Route B can't start its take before 6:10, when Vale reaches the service lane, so §07's own timings already gave it about 28 s. No order timing can fix that.
  - Route A and loud finish long before 8:00 and are unchanged.
  - 8:30 sits inside the 8–12 min mission rule.
- **The 33 scenarios:** none changes; none tests the 8:00 fails, so N1 and N2 should be scripted at 8:30.

## Loud stays a real choice
- Loud still wins at 3:38.1 with no contact and no Listen, and it keeps $40,000. It is what you do when Inez is refused, shot or never learned.
- **What it costs, all on the GDD's current [B] costs:**
  - T 6 against 3;
  - A 2 (Raised);
  - the Enforcer hit, and §08 wounds carry over;
  - more Heat.
- §06 says "T2 decides whether costs rise". So I don't raise loud's [B] costs before T2.

## Tested and rejected
| idea | result |
|---|---|
| Guard at 3 hits (§06 health [S]) | Loud only slows by 6 s (3:44.1, T 8, A 3). One `hits-to-drop` value is shared by guard, patrol, response and rivals, and the change crashes a scenario in the suite. It needs a per-role health split first. |
| Seal clock 65–90 s | Loud's best still wins (at 65 s, by 0.2 s: a knife-edge, not a choice). 75 s breaks S05's [B] narrow escape. |
| Listen without Watch (drop `known routine` from `scout.listen`) | Route A 3:30.1, 8 s ahead of loud, windows 61 s. But informed routes then never use Watch (§07 "every verb introduced once"), and Sky's Pay lesson leaves the main path. Hold it as Sky's option if T2 finds 5 s too thin. |

## Still to land, and these will widen the gap
- **The §07 final fight (Keel's GameFight, in progress):**
  - loud gets 2–4 police from the west street, no allies, the seal running;
  - quiet gets rivals with allies: the guard who never saw you, Inez's 10 s warning, the driver's block, the false tip.
- **The crowd (GameCrowd, in progress):** a shooting at the Office door seen by a civilian means Alert 3 at once.
- **Re-run `fast.py` when they land.** If loud then becomes impossible rather than costly, that is the next balance call.
- **Campaign (mine, not a mission value):** the Heat threshold for scheduling a §06 fight. A threshold of 5 makes loud's Handover (T 6) schedule a fight next mission, and quiet's (T 3–4) doesn't. I'll set it with the fight event; a value nothing reads fails the schema.

## GDD text owed if Sky accepts
- §04 table: "routine known (Watch 52 s)" → 20 s.
- §07 route A: "listens to Inez (1:34)" → (1:02).
- §07 route B: the Watch time.
- §07 schedule: 8:00 → 8:30.

Watch at 52 s is a [B] number from the top-down test, so Sky should see the change.

## Evidence
```
git archive HEAD sim contracts/rules contracts/people/people.rules.json contracts/missions/handover | tar -x -C out/motive/engine/head
DOTNET_ROOT=/opt/homebrew/opt/dotnet/libexec dotnet build out/motive/engine/head/sim/cli -c Release
python3 -I briefs/011/motive-balance/fast.py '{"name":"base"}'                                       # loud 3:38.1 · A 4:05.1 · B 7:26.8
python3 -I briefs/011/motive-balance/fast.py '{"name":"all","values":{"watch-s":20},"site":{"over":510}}'  # loud 3:38.1 · A 3:33.1 · B 7:26.8 (63 s to 8:30)
python3 -I briefs/011/motive-balance/window.py '{"name":"w","values":{"watch-s":20}}'                # 2:16–2:46, 3:53–4:26
python3 -I briefs/011/motive-balance/suite.py '{"name":"s","values":{"watch-s":20},"site":{"over":510}}'  # 33 scenarios, 5 end earlier, 0 checks fail
```

## Next owners
- **Aster:** relay change 1 to Keel and change 2 to Waymark; take the [B] change and the GDD text to Sky.
- **Keel:** `watch-s` in the rulebook; regenerate the site.
- **Waymark:** the 8:30 close, the waitress shift, M-2.
- **Suture:** re-baseline, and run T2 against loud 3:38 and route A 3:33.
- **Motive:** re-run the frontier when the final fight and the crowd land; set the Heat threshold with the §06 fight event.
