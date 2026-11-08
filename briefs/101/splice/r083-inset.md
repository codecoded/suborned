# r083's inset rows judged in mission time (brief 101, Splice, for Latch)

Splice, 2026-11-09. A patch for `Proof085Scenes.cs`, not applied. That file is outside `tools/`, and Latch is working in its other half, `Proof085Beats.cs`. Apply it with `patch -p0 < briefs/101/splice/r083-inset.patch` from the repo root; it dry-runs clean on the tree as of today. It compiles with the rest of the player's scripts (the `runtime-compile` csproj with the patched copy swapped in). It hasn't run in a player.

## What flaked

`r085insetRouteA` needs the guard's two docked lines inside 8 s of real time, from the moment he reaches the café on route A. On 0.2.283 one run passed and two failed, with the same code and words. Each failing run saw his first line ("I'm here, Inez. What is it?") and not his second ("You said it couldn't wait. What is it?").

## What the runs show

- The proof pins seed 0. In the host (no views), the guard reaches the café at tick 2124 (3:32.4). His two inset lines come at +0 and +15 ticks. Latch's probe found the same +0/+15 on seeds 1 to 20.
- The picture taken after the window (`r085-inset-route-a`) reads 3:40 or 3:41 on the mission clock in passing and failing runs alike. In every one, the second line is still docked at the top.
- A bubble lives 3 s (`bubbleSeconds`), so that line was born at least 5 s into the window. The host says it 1.5 s in by mission time.

So in the player the clock stalls for several seconds at the start of the window, then catches up. The tick loop keeps adding real time to `acc` and steps up to 80 ticks a frame. The second line therefore shows near the 8 s mark of real time, and the row passes or fails by the wall clock. The pressed row has the same weakness: Inez's line is due by the wall clock, and the picture taken just before the window can outlast it.

## The patch

`EveryInsetLine(h, ticks, ...)`:

- The window is mission time: 80 ticks on route A, 70 for the pressed run (the 8 s and 7 s it had).
- It stays open while a line said in the window is still up or due and not yet sampled.
- The lines are the host's own: inset bubbles from the window's opening tick.
- A real-time guard (30 s plus the window) only stops a clock that won't run, and fails the row with the ticks it reached.
- A line the host said in the window that never showed docked is written to the proof's log.
- Each sampled line records its tick and how far into the window it docked, so a future miss says which way it went.
- The pressed run's `r085-inset-inez` picture is taken inside the window, once her line is sampled.
- The rows' conditions are unchanged.

Run `-proofOnly r083` (it is on `Proof085*.cs`'s row) on the next build.

## Open (Latch)

What stalls the clock at the start of route A's window? With scenes off, no conversation should hold it. It may be a first-frame hitch after the proof's synchronous steps (the inset camera's first render?). If play can do it too, that's worth knowing apart from the proof.

Probe: `out/scratch/splice/inset/` (`bun out/scratch/splice/inset/run.ts`, seed 0, on the last host-logic build).
