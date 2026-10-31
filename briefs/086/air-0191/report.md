# Brief 086 · The Air sitting on 0.2.191 (Splice, 2026-10-31)

**No Air frame time was measured.** The Air's screen is locked, so the game can't draw. `rows` in `results.json` is empty on purpose.

## What happened
- Pre-flight passed: AC, charged, Low Power Mode off, thermal state 0, load 2 to 4 on 10 cores. Spotlight's indexer holds one core; `preflight/` has the logs.
- macmon 0.10.0 is installed in `~/.local/suborned-air/macmon`, its sha256 matching the release's digest. The build is at `~/Downloads/suborned-air-0191/`.
- Two launches (21:53 for 2:31, 21:58 for 7:01): the process started, the log stopped after scene load, the GPU sat at 338 MHz and 0.2 W, and no row was written. I stopped each by its PID.
- `CGSSessionScreenIsLocked` is true, locked since 19:40. The lock is the likely cause, not a proven one: only an unlocked launch settles it. I didn't try to get past it or change any setting.

## To run it
Sky unlocks the Air and keeps it from locking for about 35 minutes, then:
`ssh sky@skys-macbook-air-1 'cd ~/Downloads/suborned-air-0191; nohup caffeinate -dims kit/run-air.sh > run.out 2>&1 < /dev/null &'`
Then pull the newest `run-*` folder and run `python3 -I kit/collect.py <folder> results.json`.

| launch | rows | zoom, size |
|---|---|---|
| cold-z10 | play, tp-street, tp-worst | 10, 1080 |
| soak-z10 | 14 × 30 s, with macmon: the throttling curve | 10, 1080 |
| warm-z10 | play and surface detail off, A B A B; native A, B; tp-street, tp-worst | 10, 1080 and 2880x1808 |
| warm-z8, z14, z18 | play; z8 also native | 8, 14, 18.5 |
| scale, 4 launches | render scale 100, 65, 100, 65 (65 at 2880x1808 is the 1080 equivalent) | 10 |
| shipped | `-perf064 -capture-native`: the real full screen, Redline's cone scene, not the opening | as it comes |
| frames | `-proofOnly frames`: SSAO off, SMAA off, crowd shadows off, capsules, follow spots | 1080 |

Each launch's first-row wait is untimed on an unlocked Air, so the 30-minute aim isn't proven; 32 to 35 is my estimate.

## What Redline should correct or note
1. **The "zoom 14" rows were never zoom 14.** Player 0.2.191 doesn't read a row's `zoom` or `yaw` (`TpLongRun.cs`), so every play row draws the camera's own zoom: 10 since Oct 30. The 1.26 M at play in the plan's §1.1 is zoom 10. The picture bears this out.
2. **Third person is worse at the real bar.** At 1920x1080 tp-worst draws 7.9 M triangles, against 6.9 M at 2880x1864. A wider aspect at the same vertical FOV is the likely reason. §5.4's "about 5.5 M from three pieces" starts from 6.9 M, so it reads about 6.5 M here, over the limit.
3. **The shipped size on this Air** is display 3840x2410 with RenderCap 0.75, so a 3D pass of about 2880x1808, not 2880x1864.
4. **Zoom works without a player change.** A camera preset beside the app sets it, one zoom a launch. Checked on the Studio: zoom 18.5 draws 1.98 M triangles, 161 SetPass, 735 casters, 448 renderers, under the play gate of 2.5 M.

## What this player can't give
No switch exists in 0.2.191 for soft shadows, bloom, leaves' alpha, `shadows-off`, `old-far-off` or `planting-off`, and there's no per-pass GPU recorder. macmon gives only the whole GPU's clock, power and temperature. A render-scale row prices the smaller pass, not FSR: the asset's filter is Auto.
**Since the sitting has slipped, Latch could add those switches first, so one sitting covers everything.**
