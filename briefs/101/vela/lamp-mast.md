# Brief 101 · the square's lamp mast (Vela, 2026-11-09)

Sky picked B (`briefs/101/vantage/square-camera.md`, decision in `briefs/101-hud-sweep.md`): the terrace's west sphere lamp, beside the cell's start, becomes the square's camera mast.

The piece is `MastCamera_Lamp`, built by `tools/decokit/civic_mast101.py`.

**Adopted** in `contracts/world/kit.json` on 2026-11-09: Sky approved it as built ("looks good"), deep teal hoods included. The reason is in the entry's `why`.

Its tag is `mast-camera:lamp`, so no mast takes it until the world names it for the square's mast. A world export with it adopted is byte-identical to one with it as a candidate.

Renders are in this folder (git-ignored pictures), with `board.png` for Sky's phone.

## The piece

- **The lamp stays the lamp.** Today's `PlazaSphereLamp_Play` keeps its own heights:
  - the teal sleeve and the chrome pole;
  - the two arms at 2.42 and 3.80 m, with the café's green comet banner between them;
  - the glow ring and the 0.45 m chrome globe at 4.36 m (top 4.59 m).

  It now stands on a honed limestone plinth with a brass hairline.
- **The column carries on** through the globe in chrome, with seafoam bands, to a chrome crown at 7.4–8.2 m: a flared bowl with a seafoam band, the drum, a brass hairline, a domed cap and a ball finial.
- **Three cameras fan out of the crown,** 60° apart on chrome spokes, 15° nose-down.
  - Each has an ivory streamline body tapered to its spoke.
  - A deep teal stripe runs along the top and widens into a deep teal lens hood. The hood is charcoal inside and reaches further at the top, like a sunshade.
  - Inside the hood are a chrome bezel and a dark lens. Each lens centre is 7.68 m up (the engine's mast lens, GDD §05).
- **Why deep teal on the cameras, not seafoam.** In the real opening frame the cameras stand in front of the café's seafoam-and-white glazing, and seafoam hoods disappeared there. Deep teal is the lamp's own sleeve colour. Seafoam stays on the column's and the crown's bands.
- **Brass** is only hairlines: the plinth, the stripes' edges, the hood joints and the crown.
- **Signal colours:** none on the piece. The HUD draws the cones.
- **Materials:** palette only, no new material. DK_Limestone, DK_Brass_Civic, DK_Chrome, DK_Enamel_Teal, DK_Enamel_Seafoam, DK_Enamel_Ivory, DK_Lamp, DK_Banner_Comet, DK_Enamel_Charcoal, DK_Eye_Lens.
- **Size:** 1.77 × 8.20 × 1.25 m (kit x, y, z). The pivot is the column's foot on the pavement, and the footprint centre sits at kit (0.177, 0.331) from it. The banner reaches kit −X; the cameras reach the front.
- **Triangles:** L0 10,760. L1 2,687 and L2 641 are the export's decimations; the play camera always draws L0.
  - By part: lamp 3,630, column 640, crown 1,504, the three cameras 4,986.
  - Compared with other street furniture: today's terrace lamp is 7,932, Streetlamp_Play 5,782, PolicePost 3,262, PhoneBooth_Play 19,344. There is one mast in the zone.
- **Collision** is declared, not measured (`contracts/world/kit.collide.json`):
  - the plinth, 0.58 × 0.58 × 0.14 m;
  - the sleeve, 0.47 × 0.47 m up to 0.96 m;
  - the column, 0.16 × 0.16 m up to 7.40 m.

  The crown, the cameras and the banner are left out, so no lens stands inside its own volume.
- **Parts:** `DK_MastCamera_Lamp_Lens0`, `_Lens1` and `_Lens2`, one lens per head.

## How the cameras match the world's facings

1. The world export gives a mast `facings = yaw + k × headStep` (`tools/world/gen/street/quiet.ts`). The square's head step is 60 (Datum's plan change).
2. The world stands a street piece at its item's yaw: kit +X turns to plan (cos yaw, sin yaw) (`KitPlacer.Place`).
3. So camera k is built looking along kit +X turned k × 60° toward kit +Z: kit (cos 60k, 0, sin 60k). In Blender that is the angle 180 + 60k, since kit x = −Blender x and kit z = −Blender y.

Placed at its item's yaw, camera k looks along `facings[k]` at any yaw. No extra turn is needed.

- **At B** the item's yaw is plan −30°: Vantage's yaw 330, or block `yawDeg` 240 through `civic/dress.ts planYaw`, which is `lamp-terrace-w`'s yaw today.
  - The cameras look ESE, ENE and N.
  - The arms and banner hang where today's lamp has them: plan 150°, broadside to the opening camera, left of the pole on screen. The real capture shows the same.
- **Measured from the export** (`review101_mast.py` lens check, kit axes from the pivot):

  | head | lens centre | bearing from kit +X | look direction |
  |---|---|---|---|
  | 0 | (0.902, 7.687, 0.000) | 0.0° | (0.966, −0.259, 0) |
  | 1 | (0.443, 7.687, 0.772) | 60.2° | (0.483, −0.259, 0.837) |
  | 2 | (−0.443, 7.687, 0.772) | 119.8° | (−0.483, −0.259, 0.837) |

  Result: PASS. `check_y030.png` draws the three facings on the ground; the cameras run parallel to them.

## What Latch needs to place it

1. **Export, then copy (adopted 2026-11-09).** After the merge, in the main checkout, run `bun run decokit --only MastCamera_Lamp`, then `bun run kit`. The generated file isn't there until then, and the kit copy tests need it too. The export is byte-identical on a rerun (sha256 `074728ce…`).
2. **The world names it** (Splice, step 3 of the order).
   - The square's mast `street/records-square/mast-camera-z0` stands at block (−3.85, −42.55) = plan (−172.55, −226.15), plan yaw −30°, facings (−30°, 30°, 90°).
   - It takes `{ id: 'MastCamera_Lamp', scale: [1, 1, 1] }` with no yaw of its own (it uses the item's), its pivot on the pavement top.
   - Today's `mast-camera` slot is 1.2 × 1.2 m (`kitrefs.ts`), and this piece's footprint is 1.77 × 1.25 m (the crown and the banner overhead). So the item should name its kit, as programme items do, or get a `mast-camera:lamp` slot at the piece's own size (its tag is `mast-camera:lamp`).
   - The zone pin for the old corner no longer matches.
3. **One piece at the site.** `lamp-terrace-w` (`PlazaSphereLamp_Play`, `civic-block.adjust.json` lamps) must go when the mast arrives, since the mast carries its globe and banner. Otherwise two lamps stand 0.2 m apart.
4. **The lens cue.** Today the host gives world masts no lens cue (`HostViews.Attention`: `if (c.World) return;`). To show calm, noticed, wary and reporting on this mast, tint `DK_MastCamera_Lamp_Lens{k}` for the head that saw: `DK_Eye_Lens`, emission black as built, as `DK_HoverCamera_Lens`. Lens centres and looks are in the table above.
5. **Heights for Watch and the HUD.** The host's camera anchors assume a pole camera: `CamHeadUp` 4.78 m and `CameraTall` 5.5 m. The callout frames 1.1 m above the stand-in, and the click collider covers the foot to 3.2 m. On this mast the lenses are at 7.68 m and the crown tops 8.2 m.
   - The "?" marker would land on the globe.
   - The Watch callout would frame the column.
   - A click on the cameras, the obvious thing to click, would miss.

   So the "?", the callout and the click target should reach the crown.
6. **Worth a look.** By my reading of `HostViews.cs:44–51`, the 3.2 m car-coloured stand-in cylinder at a camera's foot is hidden only for the mission's pole cameras (`KitPole`). A world mast keeps it visible. With the mast now beside the cell, hide its renderer and keep its collider.
7. **Batch:** a world and kit change, so a full batch (Aster's order, step 6).

## Checks (this worktree)

- `bun run decokit --only MastCamera_Lamp`
  - The piece exports: 10,760 triangles, LODs, three lens parts.
  - A second export into another folder is byte-identical (FBX sha256 `074728ce…`).
  - The run still exits 1 at its fit table, for a reason that isn't this piece: the shared generated root's manifest keeps rows for the eleven towers rejected on 2026-11-08 (Splice emptied `REHOMED`). Each reads "no such order in the plan". Splice's to clear.
- `bun run kit --catalogue`: kit.json gains only `MastCamera_Lamp` (candidate, source generated, sha256 per file). Nothing else in the catalogue changed.
- `bun run collide --only MastCamera_Lamp`: the declared volumes.
- `bun run cover` and `bun tools/levels/skyline/test/digest.ts`: the two derived files that pin kit.json. Only the kit hash and the digest line change.
- Kit and DecoKit tests: 63 pass, 3 skip, 0 fail (the same as before).
- Every test that reads the catalogue (35 files): 395 pass, 18 skip, 0 fail.
- `bun run typecheck`: clean.

**After adoption:**
- Only the status and its `why` changed in kit.json, and `bun run kit --catalogue` keeps it ("kit.json unchanged").
- `bun run cover` and the digest were regenerated. The cover moves only its kit hash; the digest moves only the mast's status.
- `bun run world` with the mast adopted, and again with it a candidate: `harbour-district.world.json`, `harbour-district.faces.json` and the generator's log are byte-identical.
- Kit and DecoKit tests: 63 pass, 3 skip, 0 fail.
- Every test that reads the catalogue or the adopted set (41 files): 494 pass, 8 skip, 0 fail. Fewer skip now that the world export is on disk.
- `bun run typecheck`: clean.

## For Splice (not changed: Splice's files)

- **The fit table's exit 1 has a one-line fix.** `tools/decokit/run.ts:42` holds every manifest row that carries an order to that order. The manifest merges and never shrinks, so the eleven towers rejected on 2026-11-08 keep failing with "no such order in the plan".

  Skip the rows kit.json rejects (the fit test already requires a piece the plan no longer orders to be rejected, with its reason):

  ```ts
  const rejected = new Set((JSON.parse(readFileSync(resolve(repo, 'contracts/world/kit.json'), 'utf8')).assets as { id: string; status: string }[]).filter(a => a.status === 'rejected').map(a => a.id)), orderSet = allOrders(), rows = manifest.filter(a => a.order && a.built && !rejected.has(a.name)).map(a => fitRow(orderSet, a.name, a.order!, a.built!));
  ```

  Run over today's manifests (the main checkout's and this worktree's): 12 rows with 11 failing (exit 1) become 1 row that fits, SpaceHQ_Portal (exit 0). Tested with a scratch copy of the line; run.ts is untouched.
- **`MastCamera_Lamp` should join `BUILD`** (`tools/decokit/orders.ts`), as the brief 097 props did. Then a bare `bun run decokit` rebuilds it from an empty `out/`. Until then, only `--only MastCamera_Lamp` builds it.

## Renders

- `opening_y030_capture.png`: the real opening frame (`out/batch/shots/start-overhead-day.png`, 1080p) with the mast composited.
  - It uses the play camera's own projection (Vantage's) and is drawn at today's lamp spot, 0.15 m from Vantage's site, so it covers that lamp.
  - The sun is set from the capture's shadows.
- `play_y030.png` and `play_y210.png`: play zoom at the Air's fullscreen density (93 px/m). `*_1080.png` are the same at 1080p (54 px/m).
  - The four figures stand on the cell's start marks.
- `check_y030.png`: the three facings drawn on the ground.
- `34.png`, `crown_34.png`, `crown_back_34.png`, `lamp_vs_mast_34.png`: three-quarter views.
- `board.png`: all of the above, with captions.

Made by `tools/decokit/review101_mast.py`, which runs in Blender on the exported FBX.
