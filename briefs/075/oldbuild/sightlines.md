# Brief 075 swap · Handover sightlines after the old-built district (Vantage, 2026-10-29)

Question (Aster): why did the Handover streets' occlusion rise from 41.6 % to 53 % when the zone itself did not change, and what is the smallest fix that brings it back to 41.6 % or below with every Handover point readable?

Inputs: `briefs/075/oldbuild/swap/occluders-rule.json`, `occluders-old.json`, the pinned export `out/scratch/latch/075swap/head.world.json` and its kit, built the same way as `briefs/075/oldbuild/swap/tools/occlusion.ts` (ours inside the zone, old pieces outside). Measure: `measure()` of `tools/world/occlusion.ts`, zoom 14, 38°, 0.5 m raster, Handover = the zone rect [-226, -84] × [-372, -148]. Scratch scripts: `out/scratch/vantage/075sight/` (`loo.ts` leave-one-out, `fix*.ts` candidate fixes, `streets.ts` per street, `v1.ts` V-1 on the old-built occluders). The baselines reproduce Latch's numbers (ours 41.6, rule 53.1, old 53.3).

Figures are hidden share per yaw 30 / 120 / 210 / 300 / mean.

## 1 · The cause: four Highrise roof towers, not the zone

| world | 30 | 120 | 210 | 300 | mean |
|---|---|---|---|---|---|
| ours, pre-swap | 45.8 | 41.5 | 45.1 | 33.9 | **41.6** |
| zone pieces only (no outside occluders at all) | 43.0 | 41.4 | 45.1 | 33.4 | 40.7 |
| swap, heights rule | 46.3 | 41.4 | 58.4 | 66.2 | **53.1** |
| swap, rule, all 8 Tower_* within shadow reach removed | 46.3 | 41.4 | 46.6 | 38.8 | 43.3 |

All the rise is at yaw 210 (+13 points) and yaw 300 (+32). Yaws 30 and 120 are unchanged.

Leave-one-out, heights rule (one piece removed at a time, mean on the zone):

| old piece | builder | top | gap to zone | without it | gain |
|---|---|---|---|---|---|
| Tower_Needle @ (25, -416), SS2 | DecoKitDecks / Highrise | 191.9 m | 96 m SE | 48.4 | **-4.69** (yaw 300: 66.2 → 47.5) |
| Tower_Atom @ (-72, -271), N1 | DecoKitDecks / Highrise | 136.3 m | 3 m E | 51.1 | **-1.99** |
| Tower_Starburst @ (-50, -273.5), N1 | DecoKitDecks / Highrise | 203.2 m | 24 m E | 52.4 | -0.65 (overlaps Atom's shadow) |
| run60.0 StreamBay/Corner_3 (zone's south edge) | frontage | 24.7 m | 2 m S | 52.4 | -0.64 |
| Tower_Lantern @ (-28, -272), N1 | DecoKitDecks / Highrise | 155.7 m | 47 m E | 52.8 | -0.31 |
| run59.0 StreamCorner_3 (south edge) | frontage | 24.7 m | 11 m S | 52.8 | -0.29 |
| run40.0 StreamCorner_3 (east edge, south of Apron Road) | frontage | 24.7 m | 6 m E | 53.0 | -0.11 alone, -1.02 once the towers are gone |
| Tower_Stepped @ (32.5, -274.5), N2 (in SwapCut's flagship list) | Highrise | 114.3 m | 105 m | 53.0 | -0.09 |
| all other pieces | | | | | ≤ 0.01 each |

Latch's guess was half right. The two N1 towers he named (Atom 136 m, Starburst 203 m) matter, but the biggest single cause is SS2's Tower_Needle, 96 m south-east of the zone. At yaw 300 its view-shadow runs 245 m (192 m / tan 38°) across the zone's east half.

Where the hidden pavement lands (1 m samples, ours against the swap, `streets.ts`):

| Handover street (pavement within 8 m of the kerb) | ours | swap | swap without the 4 towers |
|---|---|---|---|
| Apron Road (getaway-service and rivals routes) | 36.6 | 55.8 | 39.8 |
| Pylon Row (official route) | 30.7 | 34.6 | 30.7 |
| Orbit Walk (getaway origin) | 47.1 | 50.4 | 47.8 |
| Garden Row | 32.3 | 35.0 | 32.3 |
| Starline Street | 54.0 | 66.4 | 54.6 |
| Rocket Lane | 46.9 | 53.0 | 46.9 |
| south of Apron Road (y < -330: no point, no route) | 15.3 | 38.4 | 21.1 |
| whole zone | 41.6 | 53.0 | 43.6 |

## 2 · Are they flagships?

No, in my reading. All four are roof-programme towers that DecoKitDecks stands on Southside podium blocks, kit category Highrise:
- N1 "Constellation": three crowned towers on a pool deck (Concepts/Blocks/needle_trio.png).
- SS2: the "skyport tower" Googie needle with a skypad (skyport_tower.png). It is not the Skyport itself (the one in the water, reached by the S2 station).

The height rule (`OldTownSwap.RuleMap`) only maps frontage classes, so it never touched these towers. They stand because `SwapCut` treats the whole Highrise category as flagships for network clashes. Sky's flagship list (disc towers, gold tower, gate towers, SpaceHQ, sphere, Skyport, hero corners, set-piece fronts) doesn't name them. They are tall background towers placed by a per-block rule, and the old builder's own rule already keeps towers off blocks where "from the 2.5D camera they would bury" Orbit Square (DecoKitDecks header). The Handover zone is now in that position for N1 and SS2. Aster's call; if Sky counts the Constellation trio as a set piece, see option C.

## 3 · Handover sightlines

**V-1 still holds.** I ran it on the old-built occluders, blockers 300 m round the civic site (`v1.ts`; the checker uses 130 m), with 475 old pieces and all 9 nearby towers in the blocker set:

| world | V-1 |
|---|---|
| plan world | pass 64/65 (+1 hidden by design, Behind café) |
| swap, heights rule | pass 64/65 (+1 hidden by design) |
| swap, heights old | pass 64/65 (+1 hidden by design) |

Every row is identical to the plan world. The lowest row is the camera entrance cone at yaw 30, 93 % seen. No point is played at yaw 300, which is where the Needle does its damage. The 19 points played at yaw 210 (the service yard, around (-140, -295)) have the N1 trio to the east-north-east, not on their camera ray to the north-north-east. Their shadow falls south of Apron Road.

**Play frames** (`pair-play-cafe-square-y030/y210`, `pair-play-office-front-y030/y210`):
- The café, fountain and terrace, the Office's public door on the west face (yaw 30), and the service face and yard (yaw 210) all read in both worlds.
- No old tower body enters these four frames.
- What differs is sun shadow from the old stage light (a dark band across the Office's east pavement at y210). That is lighting, not occlusion: Glint's.

**Cut-out rule (GDD §10).** §10's premise is that the play streets are low, so the cut is the rule, and the Records Office is the one tall landmark left in play. Two of the towers break that at the Handover's edge:
- N1 Atom stands 3 m from the zone.
- At yaw 300 the Needle covers 3,031 m² of zone pavement on its own, and at yaw 210 Atom covers 942 m².
- When the player turns to 300 or 210, the cut would have to cut through 136–203 m towers, and crowd extras (whom the cut doesn't reach) are lost under them on Apron Road and Starline Street.

Removing the towers restores the §10 premise without needing the fallback. Only the yaw 30 and 210 checks are V-1-binding, but 300 is a heading the player can turn to.

## 4 · Candidate fixes (mean on the zone; rule / old heights)

| fix | rule: 30 / 120 / 210 / 300 / mean | old: mean |
|---|---|---|
| as swapped | 46.3 / 41.4 / 58.4 / 66.2 / 53.1 | 53.3 |
| drop Needle | 46.3 / 41.4 / 58.4 / 47.5 / 48.4 | 48.6 |
| drop N1 trio | 46.3 / 41.4 / 47.8 / 57.9 / 48.3 | 48.6 |
| drop N1 Atom + Starburst | 46.3 / 41.4 / 48.4 / 61.6 / 49.4 | 49.7 |
| drop Needle + Atom + Starburst | 46.3 / 41.4 / 48.4 / 42.9 / 44.8 | 45.0 |
| **A · drop Needle + N1 trio** | 46.3 / 41.4 / 47.8 / 39.1 / **43.6** | 43.9 |
| cap the 4 towers at 40 m | 46.3 / 41.4 / 47.9 / 40.3 / 44.0 | 44.2 |
| cap the 4 towers at 60 m | 46.3 / 41.4 / 50.4 / 40.5 / 44.6 | 44.9 |
| cap the 4 towers at 80 m | 46.3 / 41.4 / 56.9 / 41.9 / 46.6 | 46.9 |
| cap the 4 towers at 100 m | 46.3 / 41.4 / 58.4 / 46.1 / 48.1 | 48.3 |
| N1 trio dropped, Needle moved 60 m east | 46.3 / 41.4 / 47.8 / 44.2 / 44.9 | 45.5 |
| N1 trio dropped, Needle moved 60 m south | 46.3 / 41.4 / 47.8 / 48.2 / 45.9 | 46.3 |
| A + the three edge runs' 24.7 m corners lowered to 16.45 m | 44.5 / 41.4 / 47.3 / 37.2 / 42.6 | 42.9 |
| A + every old non-tower piece within 30 m of the zone ≤ 16.45 m | 44.5 / 41.4 / 47.3 / 37.1 / 42.6 | 42.8 |
| B · A + drop run40.0, run59.0, run60.0 | 43.6 / 41.4 / 46.3 / 35.2 / 41.7 | 41.9 |
| B′ · B + drop run29.0 | 43.6 / 41.4 / 46.3 / 34.9 / **41.6** | 41.8 |
| B″ · B + drop run29.0, run52.2, run53.0 | 43.6 / 41.4 / 46.3 / 34.7 / 41.5 | 41.8 |

About these numbers:
- The occluder files model StreamCorner_3's 0.5 m corner fin and mast (24.7 m) as a full 9.6 m box over a roughly 16.4 m body. Read with the fin and mast taken off, A comes out at 43.0, so part of the edge runs' cost is the box measuring the piece too tall.
- Capping or lowering only works below about 40 m. Moving the Needle helps less than dropping it.

## Recommendation

**A: drop the four Highrise roof towers.**
- Which: Tower_Needle on SS2, and Tower_Atom, Tower_Starburst and Tower_Lantern on N1.
- What stays: the podium decks keep their programmes (pools, cabanas, conservatory, bandstand, skypad).
- Why this rule: it is the old builder's own "no tower where it would bury the square" rule, applied to the Handover zone.

What A does:
- Every Handover street goes back to within 0.7 points of before the swap, except Apron Road, which is +3.2 (39.8 against 36.6).
- V-1 is unchanged: 64/65, plus the one point hidden by design.
- The zone comes to 43.6 % with heights rule, against 41.6 % before the swap.

The remaining +2.0 points lie almost entirely south of Apron Road, where there is no point and no route. They come from three ordinary 3-storey old frontage runs at the zone's south-east edge, about half of it the corner fin and mast being measured as a box.

To meet ≤ 41.6 % on the whole zone rect to the letter, B′ also removes four street-wall runs at the zone's edge (run40.0, run59.0, run60.0, run29.0). That leaves gaps in the street walls, and even then heights old only reaches 41.8. I don't recommend it. I recommend A and judging the target on the streets the mission uses (table in §1). If Aster wants the zone-rect number, the cheaper path is a measure fix: the corner pieces' fin and mast measured as a fin, not a box.

**C, if Sky counts the Constellation trio as a set piece:**
- Keep the trio and drop only the Needle: 48.4 %.
- Or keep the trio and cap the four towers at 40 m: 44.0 %. That cuts the towers to stubs, so it isn't really keeping them.
- Either way, the trio still hides about 11 more points of the zone at yaw 210 (58.4 with it, 47.8 without), on Apron Road's south side and the strip south of it.

## Open, for others

- V-1 runs on the plan-built world only, inside a 130 m box (`tools/missions/handover/visibility.ts` `blockersFor`). Its comment says nothing near is taller than about 100 m, which no longer holds: a 203 m tower reaches 260 m. Two changes are needed: widen the box to ≥ 260 m, and run V-1 once on the old-built occluders (OldTownSwap's export). Splice (tool), Waymark (mission check). Today the result is the same at 300 m.
- `SwapCut` treats the whole Highrise category as flagships. If A is taken, DecoKitDecks' tower exclusion needs the Handover zone added to it, or the trio and the Needle need taking out of the swap. Latch/Splice; Aster decides the classification.
- Sun shadows on the Office front at yaw 210 under the old stage light: Glint.
