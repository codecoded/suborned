# Harbour District massing grammar (atompunk / space age)

Owner: Vela (brief 007 C). Implementer: Splice (`tools/world`, brief 007 B). Look: GDD v0.17.0 §15, palette = `contracts/style/suborned.style.json` `palette_world`.
Concept root: `~/Projects/the-suborned-store/concepts/` (cited below as `AA/` AtomicAge, `BL/` Blocks, `SH/` Streets/hero, `SS/` Streets/spaceage, `SM/` Streets/mega, `HZ/` Horizon). Every rule is a number so it can be tested; `[S]` = starting value, tune by flyby.
Grid inherited from the plan (`standards.buildings`): storey 3.6, ground storey 5.2, plinth 0.45, bay 6, halfBay 3, cornerArc 5.3, podium 2 storeys, setback above storey 9 by 6, crownTip 106. Civic block (C-0) is fixed and exempt.

## 0. Why the first flyby read "modern city"
Flat roofs, square corners, a blue glass grid on every face, one height, no crowns. The concepts have the opposite on all five (`SH/residential_morning.png`, `BL/mixed_skyline.png`). Rules 1-6 reverse each one.

## 1. Plan form: streamline corners (radii)
| where | radius | cite |
|---|---|---|
| street corner of a block, storeys 4-9 | 5.3 (= cornerArc), kit `StreamCorner_n` | `SH/residential_morning.png`, `AA/corner_cafe.png` |
| 1 in 3 street corners (seeded) | 9.6 "wrap corner": balcony rings every storey, kit `StreamCorner_9` / `DecoCorner_10` | `AA/grand_hotel.png` |
| inner court corners, deck corners | 2.4 | `BL/needle_trio.png` |
| tower tiers | 4.0 (tier width <= 30), 6.0 (podium) | `BL/lifted_megablock.png` |
| ground-storey frontage of any corner | 5.3 plus a 0.45 m chrome nosing at the plinth | `SH/cafe_row.png` |
| port sheds | barrel: radius = half span | `SM/liner_dock.png` |
No square 90-degree convex corner on a street-facing massing edge. Concave corners stay square. Every rounded corner is a single smooth mesh (no kit seam inside the arc).

## 2. Horizontal speed bands (facade)
- Band = 0.35 m deep-set strip, full wraparound (continues round rounded corners), a pair 0.25 m apart.
- Pitch: one pair at every 2nd floor line (7.2 m) on blocks; every 3rd (10.8 m) on towers' podiums; towers above the podium use rule 4 instead.
- Height placement: first pair at storey 2 floor (z = 9.43 above street, the deck line), so the podium reads as a base.
- Colour roles (v0.17, revised round 3; see §2a for the share and where it goes): accent roles are look.json surfaces `accent_teal` `#36b0a8` (teal faience) 35%, `accent_coral` `#fc9c84` 20%, `accent_turquoise` `#1cb9c6` 15%, `accent_mint` `#7fd0b8` 10%, `accent_flamingo` `#e8609a` 10%, `accent_peach` `#fcc4ac` 10%; cobalt `#2f84e8` stays civic and landmark roofs and domes only. Pair = chrome line + one role colour. **Gold** `accent_gold` `#d0c848` (brass gold, dE2000 20.9 from guard; the old `#c9c08e` was 14.8 from the cream and vanished) is the trim on every building with a crown, a marquee or a civic or hotel frontage, not only civic.
- cite: `SH/dusk_boulevard.png` (continuous pink bands, rounded), `AA/starlight_hotel.png`, `AA/couture_house.png`.

## 2a. Colour life (round 3, Sky: "too much beige")
The concept sheets (`AA/contact_sheet.jpg`, `BL/contact_sheet.jpg`, `SS/contact_sheet.jpg`) keep the cream body but carry colour on many small elements at once. The flyby carried one thin band per 7.2 m. The numbers below replace "accent area <= 12%".
- **Accent share: 20-30% of a street facade's area, measured over the ground storey + the first two upper storeys** (where the play camera looks); 10-15% over the whole facade; never 100% of a face. Base stays pastel (cream, ivory, peach, mint, sky families).
- **Which elements carry accent (each building takes >= 4 of these):**
  1. Ground storey = faience: the plinth (0.5), the ground-top band (0.4) and the shop bulkheads in the family's plinth faience (`facade.json` `*_p`: cream→teal, peach→coral, mint→deep teal, sky→cobalt-blue, sand and ivory→gold); piers between shop bays in the building's accent A on 1 in 2 buildings (`AA/corner_cafe.png`, `AA/couture_house.png`, `AA/starlight_hotel.png`).
  2. Speed bands: a pair at every 2nd floor line (as §2), the role colour on the lower line, chrome on the upper.
  3. Spandrel panels under ribbon windows in the accent (the ribbon family's `below` panel). A spandrel may run the full depth from a ribbon's head to the next sill, up to 1.9 m (3.6 storey - 1.7 glass: the "deep" spandrel), and a solid balcony front (parapet panel) in the accent counts as a spandrel (`AA/starlight_hotel.png`, `AA/couture_house.png`). No 1-in-3 limit: use them where a building needs them to reach the share; the 30% ceiling, the one-role rule and "never 100% of a face" are the caps (Vela, round 3, approving Splice's facade pass).
  3a. Porthole fronts (`PortholeBay_*` family and porthole columns): a porthole panel in the accent behind each porthole, 1.5 x 1.5 m (or a 1.5 m band behind a porthole row), chrome ring proud of it (`SH/cafe_row.png`). Porthole fronts carry few spandrels, so this is how they reach 20%: the 15-19.9% porthole fronts of the round 3 pass take it; a front still under 20% after it is listed by id, not tolerated silently.
  4. Vertical fins: corner and entrance fins in coral or teal, 1 per street corner and 1 over every lobby entrance, full height of the body, with a gold edge bead (`AA/power_spire.png`, `SS/sky_bridges.png`).
  5. Gold trim: the parapet coping bead, the marquee edge, crown collars.
  6. Awnings (see below), banners and flags (see below), the roof programme's pool, cabana and planters.
- **Sheds** (port and market sheds: barrel or eave roofs, no shop storey) are exempt from the 20-30% street band. They take the accent on their doors (sliding or roller doors in the building's role), an eave band, and the porthole row's panels (3a), 8-15% of the facade, never 0 (`SM/liner_dock.png`).
- **One accent role per building plus gold**; a second role only on its awnings and banners. Neighbours differ (§7).
- **Awnings:** every ground-floor tenancy with a canopy (frontage `canopy`) has a striped awning: trade colour + white, stripe 0.75 m (already `ProcShop`); add **1 striped awning per 2 upper-storey bays on 1 in 3 residential buildings** (balcony awnings, 2.4 x 0.9 m, the building's accent + white), `AA/corner_cafe.png`, `SH/residential_morning.png`.
- **Banners and flags:** a vertical banner (1.2 x 6 m, accent role + gold border) on every lobby frontage and 1 in 3 street corners above the deck line; a row of flag masts (8 m, pennant 1.2 x 1.8 in the coral, teal, gold, flamingo set, never red) along civic and hotel frontages and both sides of the Parade, 9 m pitch (`AA/exposition_palace.png`, `SS/processional.png`, `SS/atomic_crowns.png`). Red cone colour is forbidden on fabric; banners use the accent roles only.
- **Greenery and water:** every podium roof and every setback terrace carries planters (`@leaf`) on >= 30% of its edge; every civic or hotel forecourt >= 200 m² takes a fountain or a pool; palms and cypress in pairs at lobby entrances (`AA/grand_hotel.png`, `AA/starlight_hotel.png`).
- Gate: every accent hex keeps unlit dE2000 >= 20 from cones `#e03a2f`, guard `#f0a020`, rivals `#8a4fd0` (all values above pass: teal 43.3, coral 21.7, turquoise 42.4, mint 40.3, flamingo 24.6, peach 21.0, gold 20.9, cobalt-blue `#3b8fc8` 29.2).

## 3. Glazing: set in stone and chrome, portholes, no curtain wall
- Window = ribbon of 3.9 m glass per 6 m bay between 1.05 m limestone piers (65% glass of the bay), sill 0.9, head 2.6 (height 1.7 of the 3.6 storey), reveal 0.35 (glass set back), stone frame 0.45 wide, chrome mullion every 1.3 m.
- Ground storey (5.2): shopfront 4.6 high, rounded-top bays (arch radius = half bay) on 1 in 3 bays (`SH/fashion_street.png`), gold or chrome frames, marquee 1.8-2.4 m deep over entrances (`SH/hotel_frontage.png`, `AA/cinema.png`).
- Portholes: diameter 0.9, chrome ring 0.1; 1 vertical column per building end wall and stair core (one per storey, 3.6 pitch); a `PortholeBay_n` on 1 in 4 bays; a row (pitch 3.0) under every barrel/eave of sheds. cite `SH/cafe_row.png`, `AA/grand_hotel.png`.
- Forbidden: a glazed panel > 4 m wide without a pier; > 55% glass on any facade; the same window pattern repeated > 3 storeys without a band break; blue curtain-wall tint; continuous dark glass ribbons wrapping a tower (the black-and-white striped slab reads modern: break every ribbon with piers at <= 3 m).
- Glass colour (signed off round 3): `glass_clear` `#2e6d70` = the kit's `DK_Window_Teal` (deep teal set back in stone reads as a window at play zoom and matches the kit pieces; dE2000 29.0 from rivals), `glass_t` `#a9cfc8` (clear acrylic read opaque: glass blocks, lanterns), `glass_dark` `#24484a` (shop doors only). `DK_Window_Tower` `#34434b` (slate) is retired from new pieces; regenerated towers use `DK_Window_Teal`.
- Brise-soleil fins (0.3 deep x 1.2 apart) on south and west faces of 1 in 3 blocks.

## 4. Towers: vertical fins, stepped tiers
Applies to tower sites (floorplate <= 30 x 30) and signature landmarks. cite `AA/power_spire.png`, `BL/needle_trio.png`, `SS/venus_spires.png`.
- Tiers: 2-4. First step at storey 9 (25.2 m over the podium), by 6 (plan standard); each further step every 6-8 storeys by 3 (halfBay). Tier corners R 4.
- No speed bands above the podium. Vertical fin pairs at each tier corner, 1.2 wide x 0.6 proud, running the full tier and **rising 2 storeys above the roof as a parapet fin** (cite `AA/exposition_palace.png`); glazing = 2.4 m recessed strips between fins, chrome edged.
- Enamel on towers: turquoise fin inlays (the plan's tower role), 1 gold or chrome vertical stripe per face on the tallest tier.
- Height variety: adjacent towers differ >= 20% in height.

## 5. Stepped setbacks and terraces (mid blocks)
- Any block >= 7 storeys on a street corner steps back one bay (6) at storey n-2, the step is a planted terrace with a chrome rail 1.1 high (`BL/hanging_terraces.png`, `BL/mixed_skyline.png`). Blocks 4-6 storeys step 3 at the top storey on 1 in 2.
- Two-storey podium, body set back to the street wall line (plan rule); podium roof = deck terrace.

## 6. Roofs: every roof programmed, every tower crowned
Roof programme (kit `Roof_*`, procedural to the same bar): a roof has >= 60% of its area programmed, parapet 1.1 whose corner radius equals the plan corner radius, **no bare flat slab**. Choose by use and seed:
| use | programme (weight) |
|---|---|
| residential | deck + planters 35, pool/lap pool 15, cabana/bandstand 20, radome 10, hover pad 10, conservatory 10 |
| mixed / commercial | pool terrace 25, pad 20, dish 15, billboard-and-mast 20, cabana 20 |
| industrial / port | barrel or sawtooth with a porthole row, tank + mast 40, dish 20 |
cite `BL/needle_trio.png` (pool terraces on the lifted podium), `BL/machine_crowns.png` (dishes, masts), `SH/residential_morning.png`.

Crown catalogue (kit pieces generated this pass, `Crowns` category; fixed size, pivot base centre, collar r 2.5, scale 0.8-1.4; width of crown <= 0.7 x top-tier width, crown tip must still be <= plan crownTip):
| crown | kit id | height m | width m | cite |
|---|---|---|---|---|
| finial (fluted drum + gold bulb + needle) | `Crown_Finial` | 12 | 5 | `AA/grand_hotel.png`, `SH/civic_crossing.png` |
| atom (nucleus + 4 orbits) | `Crown_Atom` | 11.5 | 7 | `AA/exposition_palace.png`, `AA/corner_cafe.png` |
| starburst (26 spikes) | `Crown_Starburst` | 11 | 8 | `AA/monorail_terminal.png`, `AA/googie_cafe.png` |
| saucer (observation disc D14 + spire) | `Crown_Saucer` | 14 | 14 | `AA/power_spire.png`, `AA/starlight_hotel.png` |
| antenna (mast + 3 loop arms) | `Crown_Antenna` | 16 | 6 | `BL/machine_crowns.png` |
| dish (yoke + dish R 3.6) | `Crown_Dish` | 9 | 7.2 | `BL/machine_crowns.png`, `SS/atomic_crowns.png` |
| needle (fluted spire, 3 rings) | `Crown_Needle` | 20 | 5 | `BL/needle_trio.png`, `SS/skylon_avenue.png` |
Assignment:
- Plan sites keep their regen order's crown: `parade-gate-n/s` finial pair (matched), `harbour-hotel` saucer (plan observation-saucer), `crescent-tower` atom, `lantern-beacon` antenna (a beacon).
- Signature kit towers keep their own: SpaceHQ, Skylon, DiscTower, Tower_Needle (needle), Tower_Starburst (starburst), Tower_Terrace (finial).
- Procedural towers (storeys >= 9) and procedural blocks >= 7 storeys: weighted seed by use. commercial: needle 25, finial 25, starburst 20, atom 15, saucer 10, dish 5. residential: finial 50, needle 20, atom 15, dish 10, antenna 5. port: antenna 40, dish 40, finial 20 (clock/signal tower <= 30 m). A crown goes on **every** building >= 9 storeys; storeys 6-8 take a Roof_Fin or radome.
- Neighbour rule: within 150 m no two towers share a crown type; saucer <= 1 per 400 m; the atom is the exposition motif (civic precinct and hotels).

## 7. Neighbours differ (adjacent duplicates 0)
For two buildings sharing a street frontage (or within 40 m): at least 2 of {accent role, speed-band pitch (2 vs 3 storeys), corner radius class (5.3 vs 9.6), roof programme, crown type, window rhythm (ribbon vs porthole bay vs balcony bay), height (>= 2 storeys apart)} differ. Test: no two adjacent facades with the same kit id + storeys + accent.

## 8. Sky rail through buildings (portals)
cite `BL/rail_through.png`, `AA/monorail_terminal.png`, `SS/sky_bridges.png`.
- Twin beam: centres 6.0 apart (plan `skyrail.beamCentres`), beam top 23.0, underside 21.5, car floor and platform 24.0, car top 27.4. **Suggested portal standard** (Datum owns the number): opening 12.0 wide (6 + car 3 + 1.5 each side) x 7.5 high (z 21.5-29.0) for a twin line; 8.0 x 7.5 for a single beam; arched head (radius 6 on the twin) with a 0.4 chrome lintel band and flamingo-coral inlay (coral transit `#f5849e`), stone return 0.6, building floors above and below stay structural (storey 7 floor lands at 24.0, plan rule).
- Station inside a building: platforms 50 long, 4.2 wide each side of the twin cars (x 4.7-8.9), canopy soffit 29.0, frame in stone and chrome; kit `SkyRailStation_Twin` is the freestanding version (footprint 27.4 x 54.3 m incl. lift tower and overbridge).

## 9. Horizon band (beyond the district edge)
cite `HZ/pano/pano_harbour.png`, `HZ/street/harbour_edge.png`, `HZ/street/planetrise.png`, `HZ/street/launch_day.png`, `HZ/sky/celestial.png`. All of it is derived (no plan edit), generated in the world export, drawn unlit-ish under the one haze colour `#f3e8d4`. Kit pieces (category `Horizon`, <= 3.3k tris, instanced): `Horizon_Spire_A` stepped deco 220 m, `_B` saucer spire 270 m, `_C` streamline slab 180 m with atom ball, `_D` needle trio 300/240/200 m, `_Massif` stepped podium + 3 towers 60-190 m, `Horizon_RocketPad` gantry 120 m + rocket 90 m, `Horizon_RocketAscent` rocket + 420 m exhaust column, `Horizon_PlanetRinged` R 100 (ring 130-220, tilt 18 deg), `Horizon_Moon` R 100.
Centre C = (170, -230) (district bbox centre); polar placement, all seeded by hash(plan.id, ring, index), rotation random about Z, uniform scale per ring:
| ring | r from C (m) | count | pieces | scale | top elevation angle from C | haze mix |
|---|---|---|---|---|---|---|
| A near | 750-1250 | 40 | Spire A/B/C/D 20/20/30/30 %, Massif 0 | 0.6-1.0 | <= 9 deg | 15% |
| B mid | 1400-2600 | 60 | Spire A-D 20 % each, Massif 20 % | 1.0-1.5 | <= 7 deg | 40% |
| C far | 3000-5000 | 80 | Massif 50 %, Spire A-D 12.5 % each | 1.8-2.6 | <= 5.5 deg | 70% |
| far shore | 5500-8000, sea sector only | 24 | Massif, Spire B/C in clumps of 3-5 | 3.0-4.0 | <= 3 deg | 85% |
- Sea sector = azimuth within +-60 deg of +X (east): rings A-C leave it empty (the bay); only far shore stands there. Elsewhere pieces must sit >= 150 m outside the plan's outermost footprint and keep >= 60 m clear of each other (no overlap, no grid: jitter 35% of spacing, cluster by a 3-lobe noise so a "downtown" cluster rises NW).
- Sea: derive a visual sea plane at the water surface, 16 km x 16 km about C, same lagoon colour and swell as the plan water, fading into the haze beyond r 4 km. The plan's own water polygon stays the playable water (Datum may extend it for ferry lanes; the visual plane does not need the plan).
- Rockets (revised round 3: the B-ring pad at az 135 r 1900 stood behind SpaceHQ in the flyby and read as a white spire with a rocket tip inside the district; Sky: no rockets inside the district): every rocket stands at r >= 3200 from C with haze mix >= 0.7, and none within +-35 deg of the azimuth from C to SpaceHQ (96, 0), 108 deg, nor of its opposite, 288 deg. 3 `Horizon_RocketPad` (az 200 r 3600, az 245 r 3400, az 165 r 4200), 2 `Horizon_RocketAscent` (above r 3800 az 210 at 700 m, above r 4300 az 170 at 900 m), tilt 6 deg downwind. Static; nothing animates.
- Land (round 3, Splice's `horizon/land/*`): `beyond_ground` `#c4bf9c` suburban ground (warm sage stone), `beyond_green` `#9fb585` parks and tree clumps, `beyond_hill` `#a9a28e` terraced hills (tops and steps one colour). dE2000 from the haze `#f3e8d4`: 12.1 / 21.1 / 19.5; from the signals >= 22.4. `beyond` `#f1e4cc` stays for the district edge strips; promenade and mole reuse `quay`.
- Ring A pieces read as deco towers (round 3): every tier carries full-height pilaster fins in `DK_Far_Shade` (the vertical rhythm survives the fog), teal faience stripes, a gold coping; Spire_A ends in a ziggurat crown, Spire_C is twin round towers with a coral centre fin and an atom ball, not one slab. Far palette: Stone `#f2e3c6`, Shade `#e2cdaa`, Band `#5cc0b5`, Coral `#f4a08e`, Gold `#d0c848`.
- Planets: `Horizon_PlanetRinged` at az 110 deg (ESE, over the bay), distance 6000, elevation 24 deg, scale 4.8 (R 480, apparent 9 deg); `Horizon_Moon` az 150 deg el 36 deg scale 0.9, and az 60 deg el 48 deg scale 0.7. Not shadow-casting, drawn behind fog with haze mix 25%. Colours already >= dE2000 20 from every signal colour.
- Budget: ~200 instances, ~110k tris, one draw per piece type via instancing.

## 10. Kit additions this pass (hand to Splice for catalogue, `bun run kit`)
`SkyRailStation_Twin`, `SkyRailStation_Decks` (decks only, for the SpaceHQ drum and the Skyport frame), `ParadeGate_Block` (Skyport), `SpaceHQ_Portal`, `DiscTower@avenue-gate-w|e`, `LinerDock@liner-berth` (x 0.5), `Tower_Needle@avenue-needle`, `Tower_Starburst@avenue-starburst`, `Tower_Terrace@crescent-terrace` (regen orders from the plan), `Crown_Needle|Finial|Atom|Starburst|Saucer|Antenna|Dish` (Crowns), `AtomFountain`, `BoardingPavilion_Office|Square`, `PavilionPier_Office|Square` (Plaza), `Horizon_Spire_A|B|C|D`, `Horizon_Massif`, `Horizon_RocketPad`, `Horizon_RocketAscent`, `Horizon_PlanetRinged`, `Horizon_Moon` (Horizon). New material roles registered by the generator: `DK_Enamel_Flamingo|Sunshine|Cobalt|Turquoise|Mint17|Coral17`, `DK_Gold17` (`#c9c08e`), `DK_Planet_1-4`, `DK_Planet_Ring`, `DK_Moon_1-2`, `DK_Far_Stone|Band|Shade`.

## 11. Civic square mapping (civic-block.json objects and structures)
| block id | kit piece | note |
|---|---|---|
| `atom-fountain` (R 3.3, 4.2 high) | `AtomFountain` (new) | exact footprint; chrome atom, jets |
| `cafe-table-0..7` (D 1.25, 1.04 high) | `CafeSet_Tulip` (Furnish, 1.7 x 1.9) | seeded yaw, chairs included |
| `cafe-palm-0..5` (D 0.9, 1.6) | `PottedPalm_Small` (Furnish 1.5 x 1.7) | person scale 1, seeded yaw; a palm whose fronds reach the café wall slides straight out until 0.1 m clear (max 0.6, never into a table): `cafe-palm-0/1` 0.35 m |
| `fountain-garden-*` (4 curved bands about the fountain, r 5.36-7.06, 0.52 high) | `PlanterArc` (Plaza, round 3 wrap-up: 4.63 x 1.87, scale 1) | pivot 6.12 m out from the fountain centre, outer curve (kit +Z) away from it; block-frame yaws 224.94 / 315.06 / 45.06 / 134.94 (NE / NW / SW / SE) = export yaw - 90 (`tools/world/build.ts` `ARC`) |
| `civic-*` planters (kerb ring 0.52 high, free-form polygons) | procedural limestone kerb ring + `FlowerBed` / `ShrubMound` / `TopiaryCone` inside | no kit piece fits free-form outlines; `PlanterBed`/`PlanterHex` only where the polygon is rectangular or hexagonal |
| `office-boarding-pavilion` (3.83 x 15, 4.62) / `square-boarding-pavilion` (8.4 x 2.14, 2.587) | `BoardingPavilion_Office` / `_Square` (new, canopy only) | |
| `*-pavilion-support-*` (2.0 and 1.12 sq) | `PavilionPier_Office` / `_Square` (new) | |
| `civic-*-cabinet*` | `UtilityCabinet` (Booths) | scale to polygon |
| `foreground-civic-robot` (civic-monument 2.22) | `RobotButler` plinth, or leave as the cast prop | |
