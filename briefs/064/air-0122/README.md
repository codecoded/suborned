# Air run · build 0.2.122 · 2026-10-26 (main session, for Redline and Aster)
MacBook Air 15" M4 (Mac16,13), macOS 27.0.1, 2880x1864, AC power, Low Power Mode off. Chrome, the VM and Unity Hub closed; T3 Code and Outlook open.
Run by `run-air.sh` over SSH, in this order with no pause: tp-long (02:14:52, load 2.35) → perf064 (02:16:42) → late (02:18:42; load 7.14 at its end) → tp-long again (02:21:02 to 02:22:15). All -novsync.
Raw: frames CSVs, logs and the tp-long pictures are on the Studio in out/scratch/main/air-0122/results/ (not committed).

| row | run 1 (cold) mean / p99 ms | run 2 (after ~6 min) mean / p99 ms | main ms (1 / 2) |
|---|---|---|---|
| play-opening | 14.98 / 19.27 | 20.99 / 25.97 | 8.8 / 8.0 |
| tp-street | 14.30 / 18.47 | 21.87 / 30.82 | 7.0 / 7.8 |
| tp-worst | 14.93 / 17.87 | 23.26 / 28.89 | 5.8 / 7.5 |

- Counts identical between runs (play 1.81 M tris, 117 SetPass; tp-worst 5.14 M, 218, 1,894 drawn), so run 2's +6 to +8 ms is the machine: the fanless Air heating (no thermal log was captured: `pmset -g therm` printed nothing), plus whatever load the late pass left.
- FrameTimingManager gpuMs (19.6 to 27.6) exceeds the frame interval in run 1, so on Metal it is not a usable GPU figure; main 6 to 9 ms against a 15 ms frame says GPU-bound.
- perf064 (findings 1, 2): cones p99 26.1 → 7.7 ms and 22.9 → 6.3 ms legacy → after; whole frame p99 45.0 / 41.2 → 25.2 / 27.2. The fix holds on the Air; the frame is still over 16.7 ms in 17 to 56 % of after-window frames.
- late (1920x1080): loud-fight mean 12.66, p99 19.71, max 57.7 (1 over 50 ms, tail max 62.0); loud-left-downed 14.11, p99 23.14, max 45.2.
