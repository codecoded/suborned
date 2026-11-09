# The Office's globe: today and three options (brief 103)

Vela, 9 Nov 2026. Open `board.png` first (1000 px wide, for a phone).

This is renders only. No piece was exported, and the kit, the world, the sim and Unity weren't touched. Nothing was committed, and no paid or Codex call was made ($0).

## What it is

- The ball over the Office's public door is the globe of the adopted piece `RecordsEntrance`, made by `globe()` in `tools/decokit/civic_records.py`.
  - It's a relief globe 2.4 m across: chrome seas, satin-steel continents and a ring, tilted 23.5°.
  - Its centre is 7.4 m up and 0.9 m out from the wall. It stands on a chrome stand with a stone plinth on the marquee roof.
- It comes from the 26 Sep painting. There a mirror ball sits high in a very tall window, on three chrome columns, well clear of the canopy.
- Our window is 3.3 m wide and 4.75 m tall. At the painting's share of the window, the ball fills it and hangs over the marquee.
- In play, the chrome mirrors the old town's one probe of sky and sunlit paving. So it has no shaded side and reads as pale-blue glass, and the steel continents read as pale blotches.

## The options

Each option is rendered at yaw 210 (the play heading) and yaw 300 (the quarter turn where the front is almost face-on).

- **Today:** as above.
- **A · the painting's ball.** It's smooth and 1.5 m across, with its centre 8.0 m up and 0.9 m out, inside the window's arch.
  - It stands on the painting's three slim columns, which rise from the marquee roof. The middle one cradles the ball and the two beside it stop short. They echo the three blades on the crown.
  - The finish is satin nickel (see below).
- **B · no globe.** The window, the blades, the marquee and the lanterns stay as they are.
- **C · a matte enamel armillary** in A's place, with no shine:
  - sunshine-yellow hoops (§15 gives the records area sunshine #f8e26c);
  - a coral band and a turquoise core;
  - a slim deep-teal stem.

## The finish for A

The answer to "what keeps it from reading as pale-blue glass":
- **Not mirror chrome.** The game's chrome mirrors one bright probe, so a chrome ball has no shaded side and reads as glass.
- **Satin nickel works.** It's the kit's satin-brass recipe (`brass` kind: metallic 0.6, smoothness 0.6, the sun's highlight kept) on a warm silver, #C9C4BA. The sun lights it and it has a shaded side, so it reads as a solid silver ball. It also has less shine.
- **Satin gold** reads solid too, and warmer. §15 allows gold on a landmark.
- **Turquoise enamel** reads solid, but on its columns it looks like a toy.

## My pick: A, in satin nickel

- It's the concept art's idea at the size our face can hold.
- It no longer reads as glass.
- It moves the ornament up into the arch, 2.2 m above the marquee, so nothing hangs over the guard.

B is the safe second if Sky would rather the door stood alone. C has the most colour, but it's a new motif. Its thin hoops (3 to 6 px wide at 1080p) may step and shimmer as the camera moves, since the play camera has no MSAA.

## The door and the guard at play zoom

- **The globe never covers the doorway.** This is measured with a stand-in that fills the public doorway (3.4 m wide over the brass frame, from the landing at 0.75 m to the soffit at 4.5 m).
  - In every option, 42.5% of it shows at yaw 210 and 59.4% at yaw 300.
  - The marquee and the guard hide the rest.
- **What changes is what sits above the guard.** Today the biggest, brightest thing in view hangs right over him. In A the ball sits in the arch. The board draws Glyph's hover line (ink with a cream edge) on the door for today and for A.
- **At yaw 30 and 120 the front faces away.** The wing hides the entrance; only one pier's finial peeks round the corner. No option changes those headings.

## What the change would touch (A)

- **The generator** (`tools/decokit/civic_records.py`): `globe()` becomes the ball and its three columns. Their numbers are in `make/globe_variants.py` (`A_C`, `A_R`, `columns()`).
- **A new material row** for satin nickel in `civic_common.py`. Unity's .mat is written as the brass rows are.
- **The export:** `DK_RecordsEntrance.fbx` and its L1 and L2 get new hashes, triangle counts and materials. Splice updates the `kit.json` row, which stays adopted, and Latch re-imports the kit.
- **`kit.collide.json`:** the entrance's entry is re-measured.
  - Today the globe is only a "thin" box above 5 m. Thin boxes stop neither a walker nor a shot, and sight is checked at eye height.
  - So no walk, sight or shot change is expected. Derived sim data may need `bun run sim:site` for the new hash.
- **The door and the click points don't move.** The door, the landing, the steps, the piers and the marquee are untouched, so the Office entrance point (block 64, -41.25, on the 0.75 m landing) and the door's stand-in stay where they are.

B touches the same files, minus the new material. C adds one enamel row (sunshine); its coral and turquoise are the kit's own.

## For Sky to decide

1. **Which ornament goes over the Office's public door?**
   - It's the Handover's door. Today's globe reads as blotchy pale-blue glass hanging over the guard.
   - The options: A, the painting's ball, smaller and in satin; B, no globe; C, a matte enamel armillary.
   - I recommend A, with B as the safe second.
2. **If A: satin nickel or satin gold?**
   - Nickel is the painting's silver. Gold is warmer and reads more strongly, and §15 allows it on a landmark.
   - I recommend nickel.
3. **Should the four chrome finials take the same satin finish?**
   - These are the two balls on the piers and the two small ones on the marquee's corners. They read as pale-blue glass for the same reason. The board shows A both ways.
   - It's the same piece and the same kind of change.
   - I recommend yes, whichever option is picked.

## Open, not Sky's (Glint)

Every chrome in the Handover zone mirrors the old town's one zone probe (`OldTownSwap`: "Handover zone probe", 8 m over the zone's centre). So rails, bollards and lamp globes read as pale glass too. §15 wants chrome to mirror a street canyon so it shows form. That's a lighting fix, separate from this piece.

## How the renders were made

- **Engine:** Blender 5.2, Cycles on 6 CPU threads, run through `bun run guard`.
- **The piece:** built by the generator itself (`kit.build("RecordsEntrance")`), with only `globe()` swapped in this session.
- **The setting:** the game's `DK_RecordsTower_TB.fbx`, read from the kit copy (never written), sits 10.5 m behind the entrance's pivot, as the world export places both. A 1.75 m stand-in plays the guard, on the Office entrance point.
- **The light:** `light.json`'s sun (pitch 31, yaw 330, #ffddb5).
  - Enamels are matte (brief 096 K2), the stone carries the kit's 2 m detail map, and the window glass is the game's brown film.
  - Chrome and steel take no sun, as in the game.
  - Chrome mirrors a sky and paving rather than its neighbours, as the zone probe does.
- **The grade:** the play grade's saturation and contrast plus a warmer balance (`make/grade.py`).
- **Checked against Sky's frame:** today's render matches it closely at yaw 210: the pale-blue ball, the grey blotches, the glassy finials.
- **The cameras:** orthographic, pitch 38, at the game's yaws, at the Air's density (93.2 px a metre). Board tiles are scaled to 54 px a metre, the play size at 1080p.

Files:
- `make/globe_variants.py`: the variants.
- `make/render.py`: the scene and the cameras.
- `make/grade.py`: the grade.
- `make/board.py` and `make/board_text.py`: the board.
- Full-size renders and masks: `out/scratch/vela/globe/renders/`.

To rerun:

```
bun run guard /Applications/Blender.app/Contents/MacOS/Blender -b -P briefs/103/vela/globe/make/render.py -- out/scratch/vela/globe/renders
python3 briefs/103/vela/globe/make/board.py
```

## Built, 10 Nov: Sky's picks

Sky picked A, in satin nickel, and said "sure" to the finials. Open `real-piece.png` (before and after, and the official front's finials). The kit step hasn't run yet.

- **The generator.**
  - `civic_records.py`: `globe()` is now A's ball on its three columns, with the board's numbers: centre 8.0 m up and 0.9 m out, radius 0.75.
  - `civic_common.py`: one new row, `DK_Nickel_Satin` (`brass` kind, #C9C4BA, the sun's highlight kept).
  - The piers' ball finials and the marquee's two corner finials are satin nickel. The marquee is shared, so `RecordsEntrance_Official`'s two corner finials are too.
  - Nothing else in the piece changed.
- **The look.** `civic-look.json` has the palette row for the new material (unlit #C9C4BA), so rule CD-4 passes once `kit.json` lists it. The two entrances' material roles are updated.
- **The export** (`bun run decokit --only RecordsEntrance,RecordsEntrance_Official`):
  - RecordsEntrance goes from 8,450 to 10,188 triangles (L1 2,192, L2 516). The official front stays at 2,284.
  - In `out/decokit` only these changed: the six FBXs, the new .mat and its .meta, and four tables.
- **Checked on the export:**
  - The ball is 1.5 m across, centred at (0, −0.9, 8.0). Its foot is 2.25 m over the marquee, and its back is 1 cm clear of the window's grid.
  - Under the marquee soffit every vertex is where it was, and every material keeps its area. The door, landing, steps, piers, marquee, blades and banners are unchanged.
  - The few new vertices are vertex-AO cut lines. The cut budget is 15 % of the piece's triangles, and the ball added triangles.
  - On the official front every vertex is where it was. Only its two finials changed material.
  - `verify008`'s extents agree, except the piece's back (0.30 → 0.23 m): the old globe poked into the tower.
- **Checked in pictures:** the after renders match the board's A with the finials in nickel, to 3/255. The door shows 42.5 % at yaw 210 and 59.4 % at 300, the same as before.

To rerun the pictures:

```
bun run guard /Applications/Blender.app/Contents/MacOS/Blender -b -P briefs/103/vela/globe/make/real.py -- out/scratch/vela/globe/real
python3 briefs/103/vela/globe/make/real_sheet.py
```
