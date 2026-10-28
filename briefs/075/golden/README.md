# Brief 075 · golden view

One fixed boulevard frame, old against ours, the same camera and size; make ours match old here first, then roll out.

Open `index.html`: old | newest step … | before, each 1920 × 1080 (rendered 3840 × 2160, supersample 2), fov 58. Sky's two frames (`sky-old.png`, `sky-ours.png`) sit under "reference".

## Render a step (one command)

From the repo root, after your change is in the plan, kit or code:

```
bun briefs/075/golden/golden.ts <step> --what "what changed" --by <name>
```

It runs, in order: `bun run world` (exits 1 if the plan fails) → a pinned copy in `out/scratch/latch/golden075/pin/<step>.world.json` → `bun run kit` → Unity `HarbourWorldBuilder.Build -world <pin>` → Unity `Golden075.Cli -tag <step>` → `<step>.png` + `<step>.json` (camera, export sha256) → an entry in `steps.json` (the same step name replaces its entry) → `index.html`.

- `--no-build`: render the scene as it is now (a look-only change: lens.json, haze.json, grade code). About 30 s; the full command about 75 s.
- `--index`: rebuild `index.html` from `steps.json` only.
- `--old`: render `old.png` again from the old project copy (`out/scratch/latch/oldworld/unity`, `DecoKitMenu.Flyby -dkStoryboard 1 -dkFrom 10 -dkTo 10`). Done once; no need to rerun.

Step names: letters, digits, `- _ .` (e.g. `s1-palms`, `s2-vista`, `s3-tram`, `s4-look`). `old`, `before` and `sky-*` are taken. Logs: `out/scratch/latch/golden075/<step>-build.log`, `<step>-render.log`.

Every Unity run is batchmode, offscreen (no editor window, no desktop capture), through the Sky-is-playing guard (`tools/game/playing.ts`), and waits while another Unity has the project open. Only Latch edits scenes: the build step regenerates `HarbourDistrict.unity` from the export, it never hand-edits it.

## The cameras (`cameras.json`)

Found by matching Sky's frames against every frame of the 074 reels:

| | world | reel time | eye | look (eye + 100 m) | yaw / pitch |
|---|---|---|---|---|---|
| old | old DecoKit street (circus at z 206) | old-flyby.mp4 t 10.0 s | (1.938, 9.718, 85.222) | (0.043, 10.855, 185.198) | −1.09° / +0.65° |
| ours (to s4) | Harbour District (circus at 0, 0) | new-on.mp4 t 6.0 s | (1.930, 8.109, −214.570) | (0.427, 9.003, −114.586) | −0.86° / +0.51° |

Both fov 58 (vertical), 1920 × 1080, shadow distance 120 m, no roll. Ours renders with the runtime lens and long haze installed (as Flyby074 `-lens on`). The old eye is 121 m short of its circus and 1.6 m higher; ours is 215 m short: the flights are each reel's own, as Sky saw them.

**Re-fit at s5-remap (2026-10-28):** the remapped plan is the old town in the old frame (plan y = old z − 206), so `ours` is now the old camera translated: eye (1.938, 9.718, −120.778), look (0.043, 10.855, −20.802), yaw −1.09°, pitch +0.65°, 121 m short of the circus as old. The 074 eye is kept as `ours_074` in `cameras.json`; steps s1–s4 were rendered from it.

`golden.ts` reads the ours camera from `cameras.json`; change it there (and say so in the step's `--what`) only if Sky asks for a different view.
