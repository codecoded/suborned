# 075-C · Designs inventory (Splice, 2026-10-28)

Read-only. I changed no code or data. The machine copy is `designs.json` in this folder. The one probe is `out/scratch/splice/075/why.ts`: it runs the generator in memory and prints, for each building group, why wave-2 street bays were or weren't placed.

Sources:
- the old kit: `~/Projects/the-suborned/game/Assets/TheSuborned/Art/DecoKit` (202 pieces);
- the old editor code: `Editor/DecoKit`, mainly StreetLayout, StreetSkyline, CityMass, SkyRail, Colourways and DistrictDressing;
- the old scene `DecoKitStreet.unity` (8340 prefab instances, counted by source guid);
- the reel: `briefs/074/flyby/grid*`;
- ours: `contracts/world/kit.json` (294 adopted, 77 candidate, 10 rejected), `out/world/harbour-district.world.json` (10:58 today) and plan v0.4.0.

## The table

| # | item | old source and placement | our kit | why it doesn't show today | needs (owner) |
|---|---|---|---|---|---|
| C1 | rounded glass-slab boulevard buildings | StreamBay/End/Corner _6/_9, DecoBay_10, in a 36 m canyon with 37–44 m walls; colour by ensemble (one family per wall, one coral fascia ribbon) | all **adopted** and placed (StreamBay_9W 92, _6 85, _9 70) | (1) the flyby Boulevard's west side is capped at 17–21 m (91–100 m from the civic block); (2) wave-2 gives each building one bay piece and forbids the same piece next door, so a wall mixes Stream, Balcony, Porthole and Deco; (3) the every-face pass dresses all other tall faces with punched **SideBayStack** (9340 Mid modules; docks-boulevard 1438 against 29 StreamBay); (4) Crown Avenue has the width but no continuous walls | ensemble rule: one family per street side, contrast by variant and colourway (**Splice**) · canyon streets in the plan (**Datum**) · new **StreamStack** ribbon-glass side stack (**Vela**) · StreamStack on tall faces outside the cap (**Splice**) |
| C2 | red spheres | SphereStation ×1.0 (Ø120, 188 m); livery painted the hull brick red #9C2E2B and the lattice vermilion | **adopted**, same FBX; placed ×0.425 (Ø51, top 80) | no livery: our landmarks keep their kit materials, so the sphere is ivory. 074 tints only walls and plinths. Kept small by the plan's skyline order (the Skyport, 108 m, leads) | livery table in kit.json, written by the export, rule LV-1 (**Splice**) · tinted materials (**Vela**) · per-instance swap (**Latch**) · size (**Sky**) |
| C3 | gold arch | ParabolicArch across the boulevard just past the circus, framing SpaceHQ | **adopted**, placed once: as the **Skyport pier gateway** (plan `skyport-tower.gateway`) | it is in the world but on the pier; nothing frames the Crown Avenue vista. At 38.5 m wide (tags allow ×0.9 at least) its feet reach past Crown Avenue's 34 m building lines | a Crown Avenue gateway in the plan (**Datum**) · a 30 m-span regen (**Vela**) · generalise pier.ts's gateway placement, feet off the walk zone (**Splice**) |
| C4 | gold disc towers | the old SpaceHQ (gold legs, three discs, rocket core; ×1.35, 337 m) on the boulevard axis; DiscTower pair at ±44 with SkyBridge | old SpaceHQ **rejected** (brief 007; Sky 10-13 "atom, not rocket"); SpaceHQ_Portal, DiscTower@avenue-gate-w/e and SkyBridge **adopted** and placed | **bug:** every tower site also exports square procedural tower masses (lower 24 m, upper 18 m up to 125 m), and the every-face pass dresses them in SideBayStack. The punched-window boxes swallow the round kit shafts (on-t20: DiscTowers, Needle and Starburst read as square offices with a saucer on top). SpaceHQ_Portal is cream with gold ribs, so nothing reads gold | drop the drawn tower masses where a kit tower stands, rule T-K1 (**Splice**) · gold livery on SpaceHQ_Portal (**Vela/Splice/Latch**) · the old form back? (**Sky**) |
| C5 | red-and-white striped towers | Skyport_Tower, with its lattice made vermilion by livery and coral bands | Skyport_Tower_Portal **adopted** and placed; Tower_Lantern@lantern-beacon | no livery: the lattice stays ivory, so the tower reads ivory and black (on-t50) | livery table as C2 · check the portal regen's coral bands (**Vela**) |
| C6 | rocket gantry | LinerDock (rocket in a 176 m gantry) closing Market Street's vista from its pier | LinerDock **adopted**, same FBX, **unused**; LinerDock@liner-berth is the liner terminal Sky asked for in brief 007 round 2 | no plan site: rockets were moved to the horizon pads (brief 007) | **Sky** confirms (reverses 007 r2) · a plan landmark on its own caisson at the end of Market Street (~420, −410), ×0.5 = 102 m under the Skyport (**Datum**) · vermilion livery (**Splice**) |
| C7 | old palms, lush, close to the kerb | CoconutPalm in 2 m pits, 1.0 m from the kerb line, every 10 m, staggered, random yaw, no LOD | CoconutPalm **adopted**, same FBX; 586 placed (+294 median, 191 fan) | same piece. Differences: spacing 12 against 10, a wider street (44 m against 36), the crown material and light (075-A/B), L1 from about 90 m in perspective | 10 m staggered? (**Sky**; reverses the 10-22 call) · plan key + generator (**Datum, Splice**) · frond material, L1 check (**Vela, Redline**) |
| C8 | billboards, kiosks, street props | billboards hand-placed (rooftop, wall, pylon); AdColumn 8, InfoLightbox 14, CoffeeCart, CurvedKiosk, DecoPavilion | all 8 billboards **adopted**, **0 placed**; AdColumn 1, CurvedKiosk 3, Newsstand 15, InfoLightbox 2; signs fine (167) | no rule names `billboard:*`; the `sign-frame` roof programme (20 roofs) draws a procedural frame; the advertising column loses a shared corner roll | sign-frame takes Billboard_Rooftop, wall billboards on party walls above a lower neighbour, pylons at quays and open plots, the column gets its own roll (**Datum**, **Splice**, rules BB-1..3) · billboard art (**Vela**) |
| C9 | textured sky rail beam | swept 1.2 × 1.6 m filleted beam, ivory with a proud teal stripe, UV along the line; kit pylons every 30 m (366 pylon pieces in the scene) | SkyRail_Pylon_* and SkyRail_Car_* are **candidates** | our beam is a flat coral box (`beam` #f5849e) on square pylons; the old head carries one beam where ours carries two | colour (**Sky**: GDD §01 "coral transit"; proposal: ivory beam, coral stripe) · profile and pylon kit refs in the export, rule S-K1 (**Splice**) · twin head, beam sheet (**Vela**) · sweep (**Latch**) · trains in 075-D |
| C10 | skyline not white cut-outs | (1) DecoKitCityMass: 70 m grid out to 1400 m, round-ended slabs, setbacks, banded drums, ribbon glass, 5 materials, rising with distance, merged per 420 m chunk; (2) 6 far kit towers at 560–1080 m; (3) the painted far-city band in the sky | Horizon_* **adopted** (212 placed, 750–8000 m); Tower_Setback/Stepped/Saucer **candidates**; the far band comes with Glint's 075-A sky port | nothing between the sheet edge and 750 m (open sand), then a ring of single-colour tan spires; no haze on them in the flyby; plus C4's boxes and C1's SideBayStack | a mid-ground ring in horizon.ts on DecoKitCityMass lines, rules HZ-M1..3 (**Splice**) · adopt the 3 far towers at the elevation limit (**Vela**, **Splice**) · DK_Far_* hues (**Vela** 075-B) · haze (**Glint** 075-A) · cost (**Redline**) |

## Palms against our street furniture rules

| | old reel | ours | note |
|---|---|---|---|
| spacing | 10 m | 12 m (`trees.spacing`) | ours 16.7 per 100 m of street, old 20 (+20 %). 12 was kept on 2026-10-22 so crowns roof 50 % of the kerb line, not 60 % (readable > rich) |
| pattern | staggered, the west side 6 m off the east | pairs opposite (`avenues.district`) | staggering fills the gaps seen down the street; no rule change, a plan key |
| kerb offset | 1.0 m (the 2 m pit's edge on the kerb line) | 1.2 m (`kerbMid` = SF-5's 0.45 + half the 1.5 m pit) | ours is at its rule limit; the old would fail SF-5. 0.2 m is not visible |
| crown overhang | 2.0 m over the carriageway | 1.8 m | the same |
| yaw | random | random (kitrefs.ts) | the same |
| LOD | none | L1 (1330 tris) from 8 % screen height ≈ 90 m at fov 58 | perspective views only; at play zoom palms stay L0 |
| street | 36 m face to face, 16 m road, 10 m promenade | Boulevard 44 m, 28 m road + 10 m lawn reservation, 8 m pavements | the main reason the same palm reads sparser |

"Close to the kerb" is already met. "Lush" is spacing and stagger (Sky's call), plus the frond material and light (075-A/B).

## The old skyline against our horizon

- **Old:** a continuous mid-ground city from the district edge to 1400 m. It used cheap merged slabs and drums with glass ribbons in five materials, its height rose with distance into a painted far band, and haze covered the lot. Six real kit towers stood between 560 and 1080 m.
- **Ours:** polar rings of DK_Far_* spires and massifs from 750 m out, elevation-limited, on sand land with parks. Nothing stands in the 150–750 m band, and the materials are one tan.
  - What reads as cut-outs is the gap (open sand), the single tan colour, no haze in the flyby, and the near tower sites' square boxes (C4).
- **What to do:** the mid-ground ring is the old CityMass rule ported to `horizon.ts`: seeded by cell, colourway families with teal or coral caps, never flat white, merged per chunk. The far band and haze come with 075-A.

## Height cap trade-off

- **The cap** (plan `playStreets`) holds every area with a plot within 150 m of the civic block at 3–4 storeys (16.8–20.4 m). The old canyon was 36 m face to face with 37–44 m walls, a height-to-width ratio of 1.0–1.2.
- **The flyby's Boulevard today** is a half canyon:
  - the west side (boulevard-west) is 91–100 m from the civic block, so it stays capped: 17–21 m, ratio 0.4;
  - the east side (harbour-boulevard) is 28–42 m, ratio 0.9.
- **Outside the cap, ready for the old canyon:**
  - **The Boulevard south of Market Street** (lantern-boulevard, docks-boulevard; 254–527 m from the civic block; 35–38.6 m today, both sides). This is the best fit: straight, on the tram axis, already 8–10 storeys. It needs only the ensemble rule and StreamStack.
  - **The Boulevard's east side** (harbour-boulevard, 160–227 m). It is the east wall of the opening shot, and needs the same work.
  - **Crown Avenue** (276–480 m). It has the right width (34 m) and the arch-and-gate vista. It lacks walls: towers stand back on podiums, so Datum would plan slab frontage.
- **Inside the cap:** the same streamline language at 3–4 storeys, one ensemble per street side: StreamBay_3/3W/4, StreamEnd and StreamCorner _3/_4 (the old scene itself has 79 StreamBay_3 and 74 StreamBay_4). Corners are rounded and the coral fascia ribbon runs unbroken. It reads as the old reel's low streamline terraces, with the tall wall across the road and the towers behind. It is not the canyon.
- **For Sky, with pictures:** keep the cap and build the old canyon where the cap doesn't reach, with streamline terraces inside it? Latch renders the Boulevard south after step 3, against old-t6.

## Recommended order

1. **Tower-site boxes off** (C4 bug). Splice, then Latch. A small export fix with the biggest skyline change near the axis.
2. **Landmark livery**: red sphere, vermilion lattices, gold SpaceHQ (C2, C4, C5). Splice and Vela, then Latch. Materials only; it reuses 074's per-instance swap and can ride the A/B rebuild.
3. **Glass-slab ensembles and StreamStack** (C1). Splice, Vela and Datum. This settles the height-cap picture for Sky.
4. **Mid-ground massing and far towers** (C10). Splice and Vela, after 075-A's haze and band.
5. **Sky rail profile, texture and kit pylons** (C9), after Sky's colour call.
6. **Billboards and street props** (C8). Datum, Splice, Vela.
7. **Palms 10 m staggered** (C7), after Sky's call.
8. **Sited landmarks**: the Crown Avenue arch, the rocket gantry, the old SpaceHQ form (C3, C6, C4), after Sky's calls.

Each step: Latch rebuilds once, re-shoots the play-zoom compare and the flyby grid; Redline costs. Placement is always from the plan and the generator, never by hand.

## Questions for Sky (via Aster)

1. **Height cap:** the old canyon only outside the cap, streamline terraces inside? Pictures first.
2. **Sphere size:** ×0.425 (top 80) / ×0.56 (top 105) / ×0.67 (Ø80, top 125, over the Skyport).
3. **The old gold SpaceHQ form back?** It reverses 2026-10-13's "atom, not rocket", and needs a portal regen because S1's circus station runs through it.
4. **The rocket gantry back?** It reverses brief 007 round 2. What size against the Skyport (×0.5 = 102 m)?
5. **Sky rail colour:** coral (GDD §01 "coral transit") or the old ivory with a stripe? Proposal: ivory with a coral stripe.
6. **Palms at 10 m staggered?** It reverses the 2026-10-22 call for 12 m.
