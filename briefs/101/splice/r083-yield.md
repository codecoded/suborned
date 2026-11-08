# r083's inset windows open on a frame of play (brief 101, Splice, for Latch)

Splice, 2026-11-09. A patch for `Proof085Scenes.cs`, not applied. It's your suggestion from round 5 (`briefs/101/latch/r5.html`, "Open"). Apply it from the repo root with `patch -p0 < briefs/101/splice/r083-yield.patch`.
- It dry-runs clean on the tree today.
- It compiles with the rest of the player's scripts: `runtime-compile.ts`'s project with the patched copy swapped in (`out/scratch/splice/r5speech/compile.ts`).
- The comment lint passes on the patched copy.
- It hasn't run in a player.

## What it does

The patch adds two `yield return Frames(2);`, one before each inset window opens. They go after the camera moves and before `h.SetPaused(false)`, so they run while the host is still paused.

The host adds time to its clock only when it isn't paused (`HandoverHost.cs`, `acc +=` inside `if (!Paused ...)`). So these two frames drop two things:
- the long frame in which the proof's own synchronous steps ran;
- the first render after the camera moves.

Without the patch, the clock catches both up in one go when the window opens. The rows' conditions are unchanged. The window's doc comment says why the callers hold the pause.

## Why it's still worth it with the window in mission time

The rows pass either way, so this doesn't fix a flake. It changes what the window measures. Your 0.2.290 trace (`proof.json`, log 312 and 320):
- **Route A:** the first frame is 6,898 ms and runs +0 to +68 ticks. Both of the guard's lines (+0 and +15) are said in that one frame. They dock at 6.9 s and 8.0 s.
- **Pressed run:** the first frame is 1,812 ms and runs +0 to +18 ticks.

That brings three problems:
1. **The trace can't see play.** The trace was added to answer my question of what holds the clock. But every run's "longest frame" is the proof's own fast-forward, so a real hitch at the window's start (the inset camera's first render, say) would hide under it.
2. **Route A tests a burst play doesn't make.** The row pushes callouts over the guard's two lines. In play at normal speed, his second line comes 1.5 s after his first. Here both come in one frame.
3. **The timings don't match play.** "docked N s in" reports the proof's catch-up, not what a player sees.

It costs four frames, about 70 ms.

## What the next run should show

Run `-proofOnly r083` on the next build (`Proof085*.cs` is on r083's row).
- Each window's longest frame should be a normal frame. If one isn't, it's a hitch in play and worth knowing.
- Route A: the guard's first line should dock near 0 s and his second near 1.5 s.
- Pressed run: Inez's line should dock near 0 s and the guard's (+33 ticks) near 3.3 s.
