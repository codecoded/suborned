# The fight-on line logs more than once per fight (engine 0.35.1)

From: Fable · To: Keel (engine) · Via: Aster · 2026-10-27

## What is wrong

"A fight is on: drawn agents fire on their own." (`fire.free-on`) should log once per fight. At 0.35.1 it logs again after each lull in the final fight, with no `fire.free-off` in between.

The text side was already silent. `silentLogs` covers `fire.` and now also names `fire.free-on`, `fire.free-off` and `fire.free-setting`, so the host never says the line. The repeats are in the engine log itself, which the debrief, the sim traces and the checkpoint hash read.

## Cause

`sim/Suborned.Sim/GameWeaponsFree.cs`, `FreeStep()`:

```cs
if (wfFrom < 0) { wfFrom = Tick; if (WeaponsFree) Note("fire.free-on", "A fight is on: drawn agents fire on their own."); }
...
wfFrom = -1; wfLast = -1;
foreach (var op in Ops) op.Gun.StandDown = -1;
if (WeaponsFree && !FinalFree) Note("fire.free-off", ...); // engine 0.35.1 (Motive's ruling 1)
```

Motive's ruling 1 removed the `fire.free-off` line during the final fight under one input. The fight state still resets (`wfFrom = -1`) after the settle, though. So the next shot finds `wfFrom < 0` and logs `fire.free-on` again. The player was never told the fight went off, and is now told again that it is on.

Evidence from `briefs/039/keel/0.35.1/batch-routes-0.35.1.jsonl`: 16 repeated `on` lines (an `on` with no `off` since the one before) across 11 routes:

| route | sequence |
|---|---|
| loud-left-downed | on off on on on on success (3 repeats) |
| loud-fight, loud-left-standing | on off on on on success (2 each) |
| route-a-pressure | on on on success (2) |
| loud, route-a, route-a-disable, route-g-kill, route-g-kill-board, careless-body-found, fight-safe-cell-down | one repeat each |

## The change (log only; play does not change)

Log `fire.free-on` only when the log has no `fire.free-on` since the last `fire.free-off` (or since the start). Read it back from the log, so there is no new state to hash or snapshot:

```cs
/// <summary>The fight-on line was said and no fight-off since (Fable, briefs/039/fable-fight-on.md): one line per fight.</summary>
bool FreeOnSaid()
{
    for (int i = Log.Count - 1; i >= 0; i--) { var c = Log[i].Code; if (c == "fire.free-on") return true; if (c == "fire.free-off") return false; }
    return false;
}
...
if (wfFrom < 0) { wfFrom = Tick; if (WeaponsFree && !FreeOnSaid()) Note("fire.free-on", "A fight is on: drawn agents fire on their own."); }
```

Leave `wfFrom`, `wfLast`, `StandDown` and the `fire.free-off` rule as they are. Under one input, the drawn agents' behaviour in the final fight stays exactly as it is.

Optional, Motive's call: in `fail-enforcer-down`, `fire.free-on` logs at tick 1336, the same tick as `fail.enforcer-down` and after it. A guard `&& !Over` would drop a fight-on line once the mission has ended. Flagged only. It is not part of this change.

## Done-check

- A scenario (ScenariosWeaponsFree.cs, beside lines 105 and 118): in `loud` under one input, the `fire.free-on` and `fire.free-off` codes alternate, and `fire.free-on` logs once until a `fire.free-off`.
- `suborned-sim batch` over the routes: for every route, no two `fire.free-on` lines without a `fire.free-off` between. That is 0 repeats, down from 16.
- `bun test contracts/missions tools/dialogue` stays green. The text needs no change.

## Knock-on: the hashes move

The checkpoint hash reads every log entry's code and tick (Hash.cs line 143). So the 11 routes above get new final hashes and checkpoints, and need re-recording. Their outcome, ticks, cash, Trail and Standing do not change. Waymark is re-recording routes now, so Aster should time this engine bump to land before or with that round, not after it. That way the routes are re-recorded only once.

Next owner: Keel (engine change and scenario) → Waymark (re-record the 11 routes) → Suture (batch).
