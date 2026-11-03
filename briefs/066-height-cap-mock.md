# 066 · Height cap mock: low play streets, the tall city as backdrop

**State:** mock, 2026-10-26 (Datum). Picture first: no source edits, no GDD edit. Sky decides whether the cap becomes design; only then a GDD version, the plan and the rules.
**Asked by:** Aster, from Sky (2026-10-26): "the more I play this, the more I realise our world is too built up, buildings too high for a 2.5D game."
**The proposal Sky said yes to mocking up:**
1. cap the play streets (the Handover site, about 150 m round the civic block) at 3–4 storeys, 12–16 m, with the Records Office allowed as the one taller landmark;
2. keep the tall city (towers, megastructures, the AtomicAge skyline) as backdrop beyond the play streets;
3. E (dropping a tall block by storeys) only as a fallback for any tall block left in play.

**Objective:** show what the cap looks like and what it costs before anyone builds it: a capped scratch plan and world, every checker failure on it with what it means, a read of the cost and the gain, then the frames Latch renders for Sky.
**Owner:** Datum (this mock) → Vantage (blocks) → Vela (facades, if Sky goes on) → Latch (renders after batch 0.2.123).
**Done-check:** the scratch plan exports a scratch world; the checker failures are listed and explained; V-1 measured today and capped; Latch's frames rendered from the two scratch worlds; Sky picks.
**Evidence:** `out/scratch/datum/height-cap/` (folder holds a `KEEP` file): `compare.png` (the picture), `massing.html` (all 16 play frames + context + section), `cap.check.txt` / `cap-overlay.check.txt` (checker on the scratch plan), `today.check.txt` / `today-overlay.check.txt` (same checker on the contracts plan), `*.heights.txt`, `*.yaws.txt`, `world.export.txt`. Scripts there: `analyse.ts`, `yaws.ts`, `diff.ts`, `render.ts`, `skyline.ts`.
**Picture:** `out/scratch/datum/height-cap/compare.png` (massing at the V-1 camera, today left, capped right; not a render).
**Next owner:** Vantage (handoff below).

---

## 1. The mock

Scratch only. `contracts/` and `out/world/` are untouched: `out/world/harbour-district.world.json` keeps its Oct 25 20:01 timestamp.

| file | what changed from the source |
|---|---|
| `out/scratch/datum/height-cap/harbour-district.plan.json` | `standards.buildings.caps.records.m` 38 → **16.8** (3 storeys) · `caps.lamplight.m` 31.2 → **20.4** (4 storeys) · `civic.block` → the scratch block · `version` 0.3.0-cap066, `status` SCRATCH. Nothing else. |
| `out/scratch/datum/height-cap/civic-block.json` | café `height` 34.8 → **16.8** (3 storeys). The Records Office is untouched (wings 15.5, crown 38, giant order 12). |
| `out/scratch/datum/height-cap/harbour-district.world.json` | the capped world (`bun tools/world/export.ts <scratch plan> --out <scratch> --dev`; `--dev` because C-0 and B-2 fail). |
| `out/scratch/datum/height-cap/today.world.json` | today's world from the contracts plan, with the same generator code, for a fair pair (exits 0, no `--dev`). |

**The tools already take a scratch plan; no new flag is needed.** `bun tools/plan/cli.ts <plan.json> --out --json` and `bun tools/world/export.ts <plan.json> --out <file> [--dev]` both take a plan path. The block comes from the plan's own `civic.block` path, so a scratch plan can point at a scratch block. Always pass `--out`: without it the checker writes the sheet to `out/plan/` and the export writes to `out/world/`.

**Why area caps.** The generator caps by area only (`caps[plot.area]`, `tools/world/gen/masses.ts` `storeysFor`). The Records Precinct area is roughly the 150 m ring: every plot building inside 150 m of the civic block is in `records` or `lamplight`. Lamplight runs on north to the Gardens station forecourt (about 250 m), so the mock caps a little more than the ring. Lantern (south, from 164 m) and the Boulevard's east side (harbour-boulevard, from 173 m) stay as they are: the tall city starts there.

**Storeys against metres.** H(n) = 9.6 + 3.6 (n − 1) to the roof line, including a 3.95 m roof crown. So 3 storeys are 12.85 m to the eaves and 16.8 m to the roof; 4 storeys are 16.45 m and 20.4 m. Sky's "12–16 m" is the eaves of 3–4 storeys. Under today's storey arithmetic it is not the roof line (question 1 below). The mock puts the precinct, café included, at 3 storeys (16.8 m) and the Lamplight court behind at 4 (20.4 m): a step up away from the square, with the tall city beyond.

**Heights** (tallest mass per plot building, above the carriageway, scratch exports; `*.heights.txt`):

| | today | capped |
|---|---|---|
| plot buildings < 150 m of the civic block (58) | median 27.8 m, max 35.0 m (rocket-west 20.6–35.0, boulevard-west 31.4–35.0, lamplight-south 20.6–31.4) | median 17.0 m, max 20.6 m |
| café | 34.98 | 16.98 |
| Records Office crown | 38.18 | 38.18 |
| 150–300 m (92) / > 300 m (160) | median 31.4 / 31.4, max 42.2 | unchanged |
| storeys, records (43) / lamplight (52) | 4–8 (mean 6.2) / 4–7 (mean 6.1) | all 3 / all 4 |

The cap leaves no tall block in play other than the Records Office, so E (GDD §10 occlusion: "dropping a tall block by storeys is tried in play only if…") is not needed in the play streets.

## 2. Plan checker on the scratch plan

`bun tools/plan/cli.ts out/scratch/datum/height-cap/harbour-district.plan.json --out … --json …`: **135 PASS, 2 FAIL, exit 1** (with `--overlay tools/missions/handover/overlay.ts`: 140 PASS, 2 FAIL). The contracts plan, same tree: 137 PASS, exit 0 (overlay 142, exit 0).

| rule | value on the scratch plan | what it means | the way through, if Sky goes on |
|---|---|---|---|
| **C-0** civic block at its city scale | café 22.0 × 11.3 × **16.8** m (locked 22 × 11.3 × 34.8), office 15.5 / 38 | C-0 holds the café and the Office to the Oct 9 city scale, so they cannot slide back to the Oct 5 block. A deliberate cap trips it the same way a regression would. | Sky's decision restates `plan.civic.scale.cafe.height`. The café height reaches the block only through `bun run block` from HandoverCafe.cs (34.8) and its sha256. The block's `edit` op reaches structures and objects, not buildings (`tools/plan/block.ts` line 107), so **Splice** adds a building-height op to `civic-block.adjust.json`, with a reason, and updates `tools/plan/civic.test.ts`. The agent GDD does not name 34.8, so the GDD change is in §07/§09's wording, not a number. |
| **B-2** heights follow H(n), n inside the use band, caps hold | 271/320; **49 below their use band** | The cap forces storeys under the use band's floor: rocket-west residential 4–9 → 3 (×35), boulevard-west commercial 6–10 → 3 (×6), garden-row-east and pylon-row-east mixed 5–9 → 3 (×1 each), lamplight-east mixed 5–9 → 4 (×6). The generator clips quietly (`min(n, cap)`), and its neighbour-contrast draw has nothing left to choose, so **each capped area comes out one flat height**: a plateau, not the composed 3–4 mix Sky described. | **Splice:** a cap carries its own storey band (e.g. `caps.records = { storeys: [3, 4], mode: 3 }`). The generator draws from it, so neighbours differ, and B-2 checks n against it. Optional: cap by a play-street zone or by plot, not by whole area. |

**Everything else passes**, including every rule Aster expected to break:
- the concourse and levels: L-1 to L-9, connectors 113/113, stations 8/8;
- sky rail S-R1, S-R2, S-P1, S-P2, S-S1, S-C1, P-1 and SF-18, and beam clearance B-6;
- transit and tram; frontage F-1 to F-7; the streetscape SF, KR and RF rules; megastructures MS; the skyline SG-1, SK-1 and SK-2; walking W;
- the Handover's mission rules M-1 to M-3, V-1, the takedown rules and CAP.

B-6, L-6 and SF-21 only count fewer parts.

## 3. What the cap costs, and what it buys

**What it buys (street hidden per block at 38°).** A wall hides 1.28 × its height of ground behind its face:

| wall | ground hidden |
|---|---|
| the café today (35 m) | 44.5 m |
| the district median (28 m) | 35.6 m |
| a capped block (17 m) | 21.5 m |

V-1 on all 65 Handover points (≥ 50 % of a figure seen past architecture; `yaws.ts`):

| heading | today | capped |
|---|---|---|
| yaw 30 | 56 | 57 |
| yaw 120 | 29 | **50** |
| yaw 210 | 28 | 35 |
| yaw 300 | 53 | 56 |
| all four (of 260) | 166 (64 %) | **198 (76 %)** |
| own heading (V-1 as it stands) | 64/65 + 1 hidden by design | 64/65 + 1 hidden by design |

At yaw 120 the Rocket Lane terrace (bld/rocket-west/17–21, 35 m) no longer hides the cell's start, the Office approach and door, the cabinet, the patrol and the ambush points. What still hides points at a turned heading is the café and the Records Office themselves:
- the cell behind the café at 210;
- the service yard behind the Office at 30 and 120.

The cap halves occlusion; it does not end it. An access lane is 25 m face to face (6.5 + 12 + 6.5), so a camera-side wall at 17 m still covers the near pavement and the carriageway. A lane reads whole past its near wall only at ≤ 2 storeys. Turning and the occlusion cut still matter.

**The 24 m sky concourse.** It is already outside the play streets, and nothing about it breaks.
- **Where it is today.** Sky rail S1 runs on y = 0: Gardens station (−160, 0) is about 255 m north of the civic block, then Circus, Harbour and Skyport. No deck or concourse lies within 150 m (31 decks, none there), and no raised street item does either. The only things that meet the 24 m line are megastructures and their stations (Parade Galleries, SpaceHQ, Skyport Tower), all backdrop.
- **(a) Outside the play streets** is what the plan does now.
- **(b) On its own pylons through low streets** is how S1 already runs between stations. Pylons stand in the median, mall, park, quay, water and island zones, and stations stand free with their own stair, escalator and lift banks on public ground (L-5 8/8). The beam underside (21.5 m) clears a 3-storey roof by 4.7 m and a 4-storey roof by only 1.1 m. If a line ever crosses a play street, keep those streets at 3 storeys, or keep the line over a median or park.
- **(c) Better, proposed: the 24 m line becomes the edge of the play streets.**
  - Inside a play-street zone, no roof passes the cap and nothing meets the concourse.
  - Outside, the plan's rule holds as written: a building meeting the concourse stretches storey 6 so its storey-7 floor lands at 24.0.
  - The stations and megastructures stand at the edge. Boarding (fast travel, priced in time) sets the cell down at the edge of a low precinct, and they walk in through a gate, which is where a mission seals.
  - From the low streets, the line reads as a raised rail over low roofs.
  - This is a rule for Splice and a §09 sentence, not a geometry change in the Harbour District.

**C-0.** It fails, as expected (section 2). The café keeps its footprint (22 × 11.3), its ground storey kit (CafeBay_v2, CafeInterior_Testbed, CafeTerrace) and its terrace. Its limestone body above the ground storey goes from 30.1 m to 12.1 m.

**Frontages (the trade program).** No cost to trades. The program is set per area and per level (street, deck, concourse), not per storey, and upper storeys carry no trade.
- All 1077 frontages are still there (street 882, deck 141, concourse 54), and **0 trades change**. The 100 that changed only point at re-picked facade ids.
- Within 150 m there are 170, all at street level: lobby 50, newsstand 20, diner 19, barber 16, florist 12, soda-fountain 10, radio-tv-repair 9, and so on.
- **The cost is people.** City life sizes residents from each band's mode storeys and ignores caps (`tools/life/model.ts` line 23). Under the cap, upper-floor area falls about 52 % in the precinct and 34 % in Lamplight. Housing over a 2-storey mixed podium drops from about 5 storeys to 1 (precinct) or 2 (Lamplight). `harbour-district.life.json` would overstate residents near the Handover until the life model reads the cap. That matters if the street crowd (cover and witnesses) is sized from residents: **Pulse** checks.

**The skyline.** The cap costs the backdrop nothing it has today, and opens the sky for it.
- **In the play frame.** The play camera is orthographic: zoom 14 gives a frame 28 m tall, about 45 m of ground from 23 m in front of the focus to 23 m behind it. Nothing beyond reaches the frame at any height, so the backdrop never shows at play zoom, today or capped.
- **In the follow view** (perspective, linear haze 90 → 350 m, far clip 350 m, brief 048), the icons are already haze or clipped. From the café front:

  | icon | distance | haze |
  |---|---|---|
  | avenue-needle | 302 m | 82 % |
  | lantern-beacon | 321 m | 89 % |
  | harbour-hotel | 347 m | 99 % |
  | SpaceHQ | 347 m | 99 % |
  | avenue-starburst | 364 m | clipped |
  | Crown Avenue gate towers, Parade Galleries, Skyport | beyond 430 m | clipped |

  From the Office front, SpaceHQ (398 m) is clipped.
- **What the cap opens.** Seen from the middle of Records Square, the roofline drops from 50° to 29° (from mid-lane, 69° to 51°). That puts SpaceHQ (36° up), the needle (29°) and the starburst (28°) above the capped roofs, where today they are under the wall line.
- **What it would take to read.** The skyline reads in play only if **Glint/Redline** give it a layer past the clip (brief 048's open question).
- **The near backdrop.** It is the Boulevard's east side (harbour-boulevard, 31–42 m) from about 170 m and Lantern from 164 m. The step from 17 m to 31–42 m across the 28 m Boulevard is the new edge: **Vela** composes it as the frame of the precinct, not an accident.

**The café and Records Office scenes.**
- **The Records Office becomes the clear landmark:** crown 38 over 16.8 m roofs.
- **Its wings fall level with the street:** 15.5 m, 1.3 m under the neighbours' roof line and 2.65 m over their eaves. The rule note "nothing in the precinct outranks the civic order" then rests on the crown alone: Vela or Sky to say whether the wings should lead.
- **V-1 at each point's own heading is unchanged** (64/65 + 1 hidden by design; the briefs' older "47/48" is the same rule on the earlier point list).
- **Behind café** (the Scout's seat, hidden by design) stays hidden at its heading. At 210 it rises from 0 % to 28 % seen, still under 50 %.

**Street furniture and decks.** The street layer (3346 items), connectors (113), decks (31), levels, megastructures and signatures are identical. Lamps (4.8 m), cypresses (6.5–7 m) and the RecordsEntrance porticos (13.8 m) read larger against 3 storeys: Vela's proportion call.

**The world, today → capped** (`diff.ts`):

| layer | change |
|---|---|
| building masses | 737 → 712: 23 speed-band slabs and 5 setback steps gone; 93 bodies lower |
| roofs | 93 roofs lower; programmes and counts unchanged |
| facade pieces | 972 → 958, re-picked to the 3- and 4-storey variants (StreamBay_3, BalconyBay_3, PortholeBay_3, StreamCorner_3, StreamBay_4, BalconyBay_4…), all `adopted` in kit.json |
| fabric | awnings 726 → 592 (−134 balcony awnings), banners 63 → 58 |

Expect fewer triangles; Latch measures them.

## 4. Handoffs

**Vantage (next).**
- *Inputs:* the two scratch worlds and plans above, `cap.yaws.json` / `today.yaws.json` (every point at every heading), `massing.html`.
- *Check in the blocks under the lower context:*
  1. **Mission points:** V-1 at each point's own heading (unchanged, 64/65). Now that yaw 120 opens (29 → 50), say whether any point should be played from a different heading: Office approach, Office entrance, the patrol sweep and the ambush points are all seen at 120 now. Also whether the rival lookout and car-entry points still make sense now that the precinct around them is lower (rival-lookout-exit is seen at 210 now; it was hidden by pylon-row-east).
  2. **Sightlines:**
     - watchers (pole and canopy cameras, the guard's door cone, the patrol's sweep, the 13 m witness range) are ground rules and do not change;
     - check that the quiet places (the service yard, Behind café) are still unwatched now that the 3- and 4-storey walls show more of the street round them at turned headings.
  3. **Cover:** footprints are unchanged, so the cover points should hold. Run `bun run cover` against the scratch world to confirm CV-2.
  4. **The café's terrace:** under a 3-storey café, does the terrace still read as the café's (ground storey kit unchanged), and do Inez's door and Between tables read from yaw 30?
  5. **The Records Office front:** the public door on the wing's west face at yaw 30, the official side and Meet Vale. The office is unchanged; judge it against 16.8 m neighbours.
- *Never move plan geometry; ask Datum.*
- *Done-check:* a short list (point · heading · keep or change, why), and a yes or no on the play-street extent (records + lamplight areas, or a 150 m ring).
- *Next owner:* Vela, then Latch.

**Vela (if Sky goes on).**
- Re-compose for 3–4 storeys:
  - the café body above CafeBay_v2 (12.1 m, not 30.1);
  - the precinct plateau: one height per area until Splice's capped band lands;
  - the 3.95 m roof crown, which is about a quarter of a 3-storey building (a thinner crown for capped blocks would also bring the roof line into Sky's 12–16 m);
  - the speed bands and balcony awnings the cap removes;
  - the Boulevard edge step;
  - the Office wings against their neighbours.
- The facade kit already has adopted 3- and 4-storey pieces.

**Latch (after batch 0.2.123; render only, no scene edits beyond the scratch scene).**
- *Build:* two scenes, never `HarbourDistrict.unity`:
  - `HarbourWorldBuilder -world out/scratch/datum/height-cap/today.world.json -scene Assets/Scenes/HarbourDistrict-today066.unity`
  - `-world out/scratch/datum/height-cap/harbour-district.world.json -scene Assets/Scenes/HarbourDistrict-cap066.unity`
- *Play frames* (play camera, zoom 14, pitch 38°, 1920 × 1080, day look; each frame today and capped, 16 frames), at yaws 30, 120, 210 and 300 on:
  - **the café front:** focus plan (−168.1, −225.5), the midpoint of the Café door and the cell's start. The cell on its crew points (block Fixer (−0.2, −46.6), Tech (1.2, −44.6), Scout (−1.6, −44.3), Enforcer (2.7, −47.6)), Inez at the Café door, and Vale staged at Courtyard (block −6.8, −29) so he is in frame;
  - **the Records Office front:** focus plan (−165.6, −300.1), the midpoint of the Office entrance and Meet Vale. Vale at Meet Vale, the guard at the Office entrance, the driver at Meet driver, and the cell staged at Office approach (block 64, −44.4).
- *Follow-camera frames* (one each, today and capped, 4 frames):
  - behind the Fixer at his start, facing the Café door;
  - behind Vale at Meet Vale, facing the official entrance.
  - Optional fifth pair: from the middle of Records Square facing SpaceHQ (north-east), to show the skyline against the capped roofline.
- *Counts* per frame, today and capped: triangles and drawn renderers (batches and SetPass if the harness gives them), from the perf harness at play zoom.
- *Deliver to:* `briefs/066/latch/` (git-ignored shots stay on disk), and a `compare` sheet in the same layout as `compare.png`.

**Others, only if Sky goes on:**
- **Splice:** the capped storey band and B-2; a building-height op for the block; C-0's restated scale; caps by zone; the life model reads caps; rule (c) for the concourse edge.
- **Pulse:** crowd density under the cap.
- **Glint:** shadows in a lower precinct (more sun on the square), and the skyline far layer with Redline.

## 5. Questions for Sky

1. **12–16 m to the eaves or to the roof?** To the eaves is 3–4 storeys (roofs 16.8–20.4 m). To the roof is 3 storeys only, or a thinner roof crown on capped blocks (3 storeys 14.05 m, 4 storeys 17.65 m).
2. **Precinct 3 and Lamplight 4 as mocked (a step up away from the square), or 3–4 mixed throughout?** The mix needs Splice's capped band; without it each area is one flat height.
3. **The café:** 3 storeys (16.8 m, mocked), or a 4-storey corner (20.4 m) that marks the corner and stays under the Office crown?
4. **How far the play streets reach:** the Records and Lamplight areas (to about 250 m north, mocked), or a 150 m ring (needs a cap by zone)?
5. **The concourse:** adopt (c), the 24 m line as the edge of the play streets?
