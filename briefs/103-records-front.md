# 103 · The Records Office front: the globe

**Asked by:** Sky (2026-11-09), on a frame of the Office's public front at yaw 210: "also what is this on the districts office? looks terrible".

## Objective
Replace or remove the globe over the Records Office's public door so the front reads cleanly at play zoom.

**What it is today:**
- The globe belongs to the adopted piece `RecordsEntrance`, made by `tools/decokit/civic_records.py`'s `globe()`.
- It is a 2.4 m relief globe with blocky continents on a 40 × 20 grid, chrome seas, and an equator and a meridian ring, tilted on a stand on the marquee roof.
- In play it reads as a blotchy pale-blue ball. It fills the arched window and hangs over the door, where the guard stands.

**The source:** the 26 Sep painting `briefs/008/concepts/the-suborned-records-office-retrofuture-v2-2026-09-26.webp`. There it is a smooth mirror-chrome ball on a slim stand, high inside a much taller window than our 6 × 6 m tower face has.

## Owner
- **Vela:** options and renders, then the piece.
- **Splice:** the kit.
- **Latch:** the build.

## Done-check
- Sky picks from Vela's renders at the play camera (zoom 10, 38°, yaw 210 and 30): today; A, the painting's ball, smaller, set in the window; B, no globe; C, Vela's idea with less shine.
- The chosen piece is adopted, the kit is re-copied, and the build shows the Office front at yaw 210 with the door and the guard reading clearly.

## Evidence
`briefs/103/vela/globe/` (board and notes), then the build's `office-front-yaw210` shot.

## Next owner
Vela (options), then Sky (the pick), then Vela, Splice and Latch (the build, in one batch with whatever else is ready).

## Vela's options (2026-11-10)
Board: `briefs/103/vela/globe/` (f7c4b3ee9).

**Vela's pick: A.** The painting's ball, 1.5 m across, sits in the window's arch 2.2 m above the marquee on three slim columns, in satin nickel. B is the fallback; C, an enamel armillary, is the most colour.
- **The door:** the doorway shows equally in every option: 42.5 % at yaw 210 and 59.4 % at 300. The door and click points don't move.
- **For Sky:**
  - A, B or C;
  - if A, nickel or gold;
  - whether the four chrome finials get the satin finish too.
- **Open, for Glint:** every chrome surface in the zone reflects the old town's single probe, so rails, bollards and lamp globes read as pale glass too. That's a lighting fix, separate from this piece.

## Sky's picks (2026-11-10)
"1. A · 2. Nickel · 3. sure":
- the painting's ball, smaller, set in the window's arch;
- satin nickel;
- the four chrome finials in the same satin.

Glint's probe fix wasn't picked, so it's not started.

**Next:**
1. Vela makes the piece in `civic_records.py` and runs DecoKit for it, without the kit step while Latch's outline round builds.
2. Aster runs the kit step after that round lands.
3. The full batch follows (a kit change), and Sky gets one build.


**Built (Vela, 323cb578a).** Sheet: `briefs/103/vela/globe/real-piece.png`.
- `civic_records.py`: the ball on three columns, 1.5 m across, in the arch.
- `DK_Nickel_Satin` is in `civic_common.py` and `civic-look.json`.
- The official front's two marquee finials are nickel too.
- Below the marquee soffit, nothing moved. The doorway shows 42.5 % at yaw 210 and 59.4 % at 300, as before.
- **Kit step after Latch's outline round:**
  1. `bun run kit` (two rows, 8 files);
  2. `bun run collide`;
  3. `bun run sim:site` (only `grid.collide`'s hash should change; Keel re-pins anything that pins it);
  4. then Latch re-imports, and the full batch runs.
- **Left as is (Aster):** `collide097.json` pins the entrance's depth at 3.7 m, so the world's footprint ring sits 3.5 cm off the new mesh. Nothing drawn moves, so the pin stays.

**The kit step (ce351a60a).** `bun run kit` copied the 8 files. `kit.collide.json` keeps only the two entrances' new measures, and `sim:site` changed only `grid.collide`'s hash.
- **A plain `bun run collide` regresses the walk law.** It also re-declared 43 other pieces from `collide097.json`, which undoes engine 0.49.0's solids-as-built (31 measured pieces went back to declared). That part was left out. Splice is fixing the tool and adding a test.
- **Next:** Latch's full batch with the new ball, then one build for Sky.

**Splice's fix (5f4f10d93), confirmed by Keel.**
- `collide_law.ts` states 0.49.0's rule: a builder's row or an extra root's row declares, everything else is measured, and 097 pins count only when `PINNED` is on (and a test fails if it is).
- A plain run's plan keeps all 466 records.
- Keel's measuring command is now recorded in `briefs/098/keel/walk-law.html`.
- **For Vela's next kit round (cleanup, not urgent):**
  - re-export the 42 stale-manifest pieces and the two entrances with pins off (a normal `bun run decokit --only`, with AO and LODs; ShrubMound through `shrub097_extra.py` after `vertex_ao_extra.py`), to scratch first;
  - compare each FBX's sha256 with `kit.json`;
  - done when `bun run kit` copies nothing, `collide --plan` shows nothing to do, and neither contract changes.
  - If a hash moves, it's a kit change.

