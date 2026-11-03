# Cover tick for r123-body-safe (Keel, 2026-10-27, engine suborned.sim/0.35.2)

For Latch (Proof123). The ask was one tick in r123-safe's run (fixture `sq-ra-h3`) with a rival pinned behind cover (`PostureOf` = `pinned-cover`) and a cell agent kneeling at cover, both inside one zoom-14 frame.

## 1. sq-ra-h3 as it stands: no such tick

Every tick from 0 to the end (2773, Success) was probed headless. Each fighter's `PostureOf` was read at every tick, using the same id set as `HandoverHost.BodyRead`: ops not in a car or inside, present hostiles, SecGuns, street units, the guard and rival-1..5.

- **No rival is ever `pinned-cover`.** Rival pins are all `pinned-open`: rival-1 at 2431–2451 and 2535–2567, rival-2 at 2431–2461, rival-4 at 2598–2627.
- The cell kneels at cover for the whole fight: scout at cv-003, fixer at cv-002, tech at cv-077.
- The only rival in cover is rival-3, kneeling unpinned at cv-032 (the café's west arc wall) from 2088 to 2446. Nobody fires at her, so she is never pinned.

Closest frames in the unchanged run (no extra order):

| need | tick | ids | sim cm |
|---|---|---|---|
| rival pinned (open, not cover) + agent kneeling | 2440 (rival-1 PinLeft 12) | rival-1 `pinned-open` (2212,-4262); scout `kneel` cv-003 (725,-4625) | 13.9 m apart |
| rival kneeling in cover (not pinned) + agent kneeling | 2440 | rival-3 `kneel` cv-032 (-575,-3625); tech `kneel` cv-077 (675,-4625) | 16.0 m apart |

At tick 2440 all of these share one frame: rival-1 and rival-2 pinned in the open, rival-3 kneeling in cover, and three cell agents kneeling. The x span is -5.75..22.1 m and the y span is -50.25..-36.25 m, so the centre is about (820, -4325) cm. That is the best picture without a pinned-cover rival.

## 2. Fixture plan that has one: `sq-ra-h3-pin`

Take `sq-ra-h3` and add one order: `{ "tick": 2412, "op": "scout", "do": "fire", "target": "rival-3" }`. The file is at `briefs/039/keel/sq-ra-h3-pin.script.json`.

- Up to tick 2411 every posture row is the same as in sq-ra-h3 (diffed). The run still ends Success at 2773.
- rival-3 is `pinned-cover` at cv-032 from **2431 to 2550**, without a break and standing still. The pin refreshes every 15 ticks: PinLeft is 21 at 2431, 2446, 2461, 2476 … 2536. She goes down at 2551.
- Throughout that span, **tech** kneels at cv-077 and **fixer** kneels at cv-002. The scout steps out to fire, so she stands at (654,-4475).

**Pick: tick 2461.** rival-3 is `pinned-cover` at cv-032 with PinLeft 21 (the proof's ≥ 8 test holds until 2474). tech is `kneel` at cv-077. As a bonus, rival-2 is `pinned-open` (PinLeft 21) at (1525,-4884), in the same frame.

| id | posture | cover | sim cm | Unity (x, z) via blockToPlan |
|---|---|---|---|---|
| rival-3 | pinned-cover | cv-032 (wall, h 1.13 m) | (-575, -3625) | (-166.25, -224.25) |
| tech | kneel | cv-077 (pier, h 1.96 m) | (675, -4625) | (-176.25, -236.75) |
| fixer | kneel | cv-002 | (725, -5025) | (-180.25, -237.25) |

**Focus point:** sim (75, -4325) cm. That is Unity ≈ (-173.25, ground, -230.75); use `frame.World(new Pt(75, -4325))` to get the true height. All three bodies are within 7.1 m of it. Zoom 14 at 38° shows about ±22.7 m deep on the ground and ±24.9 m wide, so there is room to spare. Adding rival-2 still keeps everything within 16 m.

**Yaw: 30** (`TurnSnap(0)`, as r123-body-safe already uses). The cell's backs are to the camera, and rival-3 is ducked behind her wall facing them. cv-032 faces block +x, which is Unity -z, so at yaw 30 the camera looks across her 1.13 m wall from the cell's side. The wall may hide the lower half of her duck.

- If the pixels show too little of her, the next quarter step is **120** (side-on to both covers).
- **210** puts the camera behind her, but then tech's 1.96 m pier stands between the camera and him.

These yaws are worked out from the cover facings and the frame axes; I haven't seen them in pixels.

**Feeding it in Proof123:** `Fixture("sq-ra-h3-pin")`, then `feed.To(h, 2461)` paused. Or `To(2457)` with the proof's 0.4 s run. Proof123's `Fixture()` reads only `sim/Suborned.Sim/Fixtures/`, so the script has to go there first. I haven't put it there (the ask was read-only). I can add it as a Keel fixture once Aster gives the go-ahead. It would be a new file next to sq-ra-h3, not an edit: sq-ra-h3 feeds MC-0/MC-3 and r123-fight-safe, and must stay as it is.

## Evidence

- Probe: `out/scratch/keel/covertick/` is a read-only C# console against the HEAD-built `Suborned.Sim.dll` (sim/ is clean in git). It prints `engine suborned.sim/0.35.2`.
- `bun run guard dotnet out/scratch/keel/covertick/bin/Release/net10.0/covertick.dll sq-ra-h3` ran 2774 rows, `end 2773 Success`. Results: `rival-*:pinned-cover` 0 times; `pinned-open`: rival-1 54, rival-2 31, rival-4 30 ticks.
- The `sq-ra-h3-pin` variant gave `end 2773 Success`, with `rival-3:pinned-cover` at 120 ticks (2431–2550). Its rows before 2412 are identical to sq-ra-h3.
- No engine code changed. Nothing was committed.
