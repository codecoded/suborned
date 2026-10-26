# Handoff · The Suborned · 2026-10-25

Read `CLAUDE.md` first (the game and the rules), then this page. Lead: `claude --agent lead-producer` (Aster). The 2026-10-14 handoff it replaces is in git history.

## Where things are
- **Branch:** `work/2026-10-21-fight-and-testbed`. `assignment-04` is frozen: never touch it.
- **GDD:** v0.26.0 (2026-10-26). Files:
  - `GDD/the-suborned-agent-gdd-v0.26.0-2026-10-26.md` (what agents load)
  - `GDD/the-suborned-gdd-v0.26.0-2026-10-26.html` (wins on conflict)
  - `GDD/the-suborned-decision-log.md` (the why and when, plus the "Open for Sky" list)

  Older versions are in `~/Projects/the-suborned/GDD/`. After any GDD change, run `bun run crew`.
- **Build:** 0.2.122 is the last build shipped to Sky. The next one is **batch 0.2.123**: `briefs/041/next-batch.md`, the sections at the end; Sky's notes on 0.2.122 are routed in `briefs/067-sky-play-0122.md`. It has one list, code first, and one test pass.
- **Machines:** all work happens on the Mac Studio. The 15-inch MacBook Air M4 is the 60 fps reference, so perf numbers count only when taken on the Air, warm, with the machine quiet. Air runs need Sky. Retrieval (SurrealDB) is served from the Studio over Tailscale; `bun run doctor` checks it.

## Session start
1. `bun tools/game/playing.ts --check`. If Sky is playing, run no loads; wrap any load in `bun run guard`.
2. `bun run typecheck`, `bun run plan`, `bun run crew --check`. Run `bun run test` only when the game is closed.
3. Read the open briefs below, then `briefs/parked.md`. Nothing on the parked list is design until Sky picks it up.

## Open work (2026-10-25)
| brief | state | next |
|---|---|---|
| 054 Watch and witnesses | Sky confirmed both rows | Keel's engine release, then Waymark's re-record |
| 055 levels and collision | Aster's fix order is in the brief | Latch proves step 1 in batch 0.2.118, then Splice flips the audit |
| 056 occlusion | F picked | Latch is porting it (batch item C) |
| 058 cameras | loop runs 2:00; Disable is an incident; stopgap now | Keel (engine), Waymark (retimes), Latch (stopgap and port) |
| 059 route B and rivals | 1a, 2a, 3a; GDD §07 written | Pulse, Motive, Vantage → Splice → Keel → Waymark → Latch. mission.test.ts has 2 failing asserts until Waymark lands mission.json |
| 061 crossings | head A, 26 crossings, footways | Datum and Vela → Splice → Latch and Glint |
| 062 briefing | B picked | Latch (batch item D); waits on Vale's portrait |
| 063 portraits | Vale B, Inez A, driver A, patrol A | Forma is making the Vale model ($3 pass), then Latch imports it |
| 064 code review | 1, 2, 4–7, 9 coded; 3, 8 closed in engine 0.30; 10 with Splice | perf on the Air needs Sky |
| 039 adaptive fights, 052 teaser | undecided | wait for Sky |

## Rules that bite
- The working tree is shared: never `git stash`, `checkout`, `reset` or `clean`. Kill processes by PID only.
- Checkpoint commits go on the work branch and hold game files only. Never commit `briefs/**/build/**`, `briefs/**/game-0*/**`, screenshots or logs. Check `git diff --cached --name-only` before every commit.
- Paid calls go only through `tools/paid/paid.py`, with a budget. Try Codex stills first. Any pass estimated over $10 needs Sky.
- Don't start a Unity build, a batch or an Air perf run without being asked.
