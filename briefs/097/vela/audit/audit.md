# Brief 097 · Handover zone audit (Vela, 2026-11-06)

Every distinct kit piece the zone draws (out/world/harbour-district.world.json, plan x -226..-84, y -372..-148): 117 pieces, 2593 placements. Sheets per family in this folder (play angle and three-quarter, `tools/decokit/review097_audit.py`). Magenta in the sheets = textures not loaded in Blender: those pieces were judged in the r3 play frames instead. Scores are 1–5 against paid/097-look-bar and briefs/008/concepts; 0 = not an environment piece.

| piece | count | L0 tris | source | score | after | fix |
|---|---|---|---|---|---|---|
| Pave_Slab | 719 | 2 | generated | 4 | 4 | ground set at the testbed finish (textured; AO on the kerbs); keep |
| Kerb_Pearl_0 | 298 | 216 | generated | 4 | 4 | ground set at the testbed finish (textured; AO on the kerbs); keep |
| RoadRibbon_Straight_6m | 281 | 8 | generated | 4 | 4 | ground set at the testbed finish (textured; AO on the kerbs); keep |
| Kerb_Pearl_1 | 280 | 324 | generated | 4 | 4 | ground set at the testbed finish (textured; AO on the kerbs); keep |
| RoadMark_Dash_2m | 72 | 2 | generated | 4 | 4 | ground set at the testbed finish (textured; AO on the kerbs); keep |
| HangingBasket | 65 | 2140 | store | 4 | 4 | at the bar in the r3 frames; keep |
| TreePit_Pearl | 60 | 244 | generated | 3 | 3 | a flat dark square: no grille or ring; next: a chrome grille and a brass hairline frame (generated, cheap) |
| DrainGrate | 50 | 4068 | store | 4 | 4 | reads at play zoom; 4k tris for a grate x50 is heavy (Redline: a 1k cut needs Sky's OK) |
| WindowBox | 48 | 2298 | store | 4 | 4 | at the bar in the r3 frames; keep |
| Tile_Terrazzo | 40 | 918 | store | 4 | 4 | ground set at the testbed finish (textured; AO on the kerbs); keep |
| CoconutPalm | 40 | 6310 | store | 4 | 4 | cards read well in the r3 frames (magenta in the sheet = textures not loaded in Blender); no Tripo needed |
| AlleyWall_4 | 35 | 4418 | generated | 3 | 3 | service-side walls: not in the r3 play views, so not judged in play light; next round (AO cuts and a stone base course) |
| AlleyWall_3 | 31 | 3776 | generated | 3 | 3 | service-side walls: not in the r3 play views, so not judged in play light; next round (AO cuts and a stone base course) |
| StreamBay_4 | 28 | 1706 | store | 4 | 4 | judged in the r3 play frames (FacadeLit layers and colourways don't render outside Unity): streamline bands, rounded corners, kit on every face; keep |
| ShopFill_3 | 26 | 1024 | generated | 4 | 4 | judged in the r3 play frames (FacadeLit layers and colourways don't render outside Unity): streamline bands, rounded corners, kit on every face; keep |
| ShopFill_4 | 24 | 1174 | generated | 4 | 4 | judged in the r3 play frames (FacadeLit layers and colourways don't render outside Unity): streamline bands, rounded corners, kit on every face; keep |
| LightDotRow_2 | 22 | 336 | generated | 4 | 4 | ground set at the testbed finish (textured; AO on the kerbs); keep |
| CornerPier_Out_Top | 18 | 456 | generated | 4 | 4 | judged in the r3 play frames (FacadeLit layers and colourways don't render outside Unity): streamline bands, rounded corners, kit on every face; keep |
| ManholeCover | 16 | 5388 | store | 4 | 4 | ground set at the testbed finish (textured; AO on the kerbs); keep |
| StreamBayStack_Top | 16 | 414 | generated | 4 | 4 | judged in the r3 play frames (FacadeLit layers and colourways don't render outside Unity): streamline bands, rounded corners, kit on every face; keep |
| InlayArc_R2_W06 | 16 | 64 | generated | 4 | 4 | ground set at the testbed finish (textured; AO on the kerbs); keep |
| SidePilaster_4 | 15 | 652 | generated | 4 | 4 | judged in the r3 play frames (FacadeLit layers and colourways don't render outside Unity): streamline bands, rounded corners, kit on every face; keep |
| SidePilaster_Top | 13 | 152 | generated | 4 | 4 | judged in the r3 play frames (FacadeLit layers and colourways don't render outside Unity): streamline bands, rounded corners, kit on every face; keep |
| ShrubMound | 13 | 2526 | store | 3 | 3 | a soft card blob; reads as a hedge cushion at play zoom; Tripo wouldn't beat the testbed cards at this size; next: the testbed planting (077 cards) |
| SideBayStack_Top | 12 | 494 | generated | 4 | 4 | judged in the r3 play frames (FacadeLit layers and colourways don't render outside Unity): streamline bands, rounded corners, kit on every face; keep |
| AlleyWall_3D | 12 | 4614 | generated | 3 | 3 | service-side walls: not in the r3 play views, so not judged in play light; next round (AO cuts and a stone base course) |
| BollardSphere | 12 | 555 | generated | 3 | 4 | rebuilt: BollardSphere (finish). an all-chrome post: reads grey at play zoom |
| StreamBay_3 | 11 | 1444 | store | 4 | 4 | judged in the r3 play frames (FacadeLit layers and colourways don't render outside Unity): streamline bands, rounded corners, kit on every face; keep |
| AlleyWall_4D | 11 | 5254 | generated | 3 | 3 | service-side walls: not in the r3 play views, so not judged in play light; next round (AO cuts and a stone base course) |
| TopiaryCone | 11 | 2484 | store | 4 | 4 | cards read well in the r3 frames (magenta in the sheet = textures not loaded in Blender); no Tripo needed |
| Roof_Tank | 10 | 2136 | store | 3 | 3 | roof programme pieces, mostly out of the play view; store; next round |
| SidePilaster_3 | 10 | 572 | generated | 4 | 4 | judged in the r3 play frames (FacadeLit layers and colourways don't render outside Unity): streamline bands, rounded corners, kit on every face; keep |
| Bench | 10 | 3412 | store | 3 | 4 | rebuilt: Bench_Play. red shell seats on mint legs, no base |
| SandwichBoard | 10 | 2116 | store | 3 | 3 | plain A-frame, but its face carries the poster sheet (billboards.ts): a rebuild must keep that face; next round |
| BikeRack | 10 | 7572 | store | 2 | 4 | rebuilt: BikeRack_Play. present-day hoops with blue feet |
| InlayBand_12_2 | 10 | 10 | generated | 4 | 4 | ground set at the testbed finish (textured; AO on the kerbs); keep |
| RecyclingBins_Play | 9 | 1910 | generated | 4 | 4 | built in brief 097 passes 1-3; collision now the replaced box or store piece exactly |
| PottedTopiary | 9 | 4720 | store | 3 | 4 | rebuilt: PottedTopiary_Play (capstone routed to it). grey straight pot |
| Hydrant | 8 | 6840 | store | 3 | 4 | rebuilt: Hydrant_Play. present-day hydrant shape, saturated red |
| PolicePost | 8 | 3262 | generated | 4 | 4 | built in brief 097 passes 1-3; collision now the replaced box or store piece exactly |
| RobotWaiter | 8 | 15324 | store | n/a | n/a | cast (Forma), not an environment piece: not scored |
| VendingMachine_Cola_Play | 8 | 2256 | generated | 4 | 4 | built in brief 097 passes 1-3; collision now the replaced box or store piece exactly |
| VendingMachine_Snack_Play | 8 | 2256 | generated | 4 | 4 | built in brief 097 passes 1-3; collision now the replaced box or store piece exactly |
| CafeSet_Tulip_Play | 8 | 3984 | generated | 4 | 4 | at the bar in the r3 frames; keep |
| PlazaSphereLamp_Play | 8 | 7932 | generated | 4 | 4 | at the bar in the r3 frames; keep |
| PhoneBooth_Play | 7 | 19344 | generated | 4 | 4 | built in brief 097 passes 1-3; collision now the replaced box or store piece exactly |
| Parasol_Testbed | 7 | 3008 | generated | 4 | 4 | testbed-finish civic piece (077 / 026 rebuilds), AO baked; keep |
| PottedPalm_Small | 6 | 2396 | store | 4 | 4 | at the bar in the r3 frames; keep |
| Cypress_Play | 6 | 5004 | generated | 4 | 4 | cards read well in the r3 frames (magenta in the sheet = textures not loaded in Blender); no Tripo needed |
| PottedTopiary_Play | 6 | 4272 | generated | 4 | 4 | cards read well in the r3 frames (magenta in the sheet = textures not loaded in Blender); no Tripo needed |
| SideBay_4 | 5 | 2496 | generated | 4 | 4 | judged in the r3 play frames (FacadeLit layers and colourways don't render outside Unity): streamline bands, rounded corners, kit on every face; keep |
| PlazaSphereLamp | 5 | 7932 | store | 4 | 4 | at the bar in the r3 frames; keep |
| CoconutPalm_Street | 5 | 8230 | generated | 4 | 4 | cards read well in the r3 frames (magenta in the sheet = textures not loaded in Blender); no Tripo needed |
| PlanterRun_4_A_TB | 5 | 6139 | generated | 4 | 4 | testbed-finish civic piece (077 / 026 rebuilds), AO baked; keep |
| Streetlamp | 4 | 5782 | store | 3 | 3 | the old town's red lamp and bollard: the old piece wins outside the zone; inside, a finish pass (Glint) rather than a rebuild |
| SignalLens | 4 | 822 | generated | 4 | 4 | at the bar in the r3 frames; keep |
| PlanterArc_TB | 4 | 12284 | generated | 4 | 4 | testbed-finish civic piece (077 / 026 rebuilds), AO baked; keep |
| GlobeLamp_TB | 4 | 1016 | generated | 4 | 4 | testbed-finish civic piece (077 / 026 rebuilds), AO baked; keep |
| PlanterRun_4_B_TB | 4 | 5393 | generated | 4 | 4 | testbed-finish civic piece (077 / 026 rebuilds), AO baked; keep |
| InlayArc_R4_W06 | 4 | 88 | generated | 4 | 4 | ground set at the testbed finish (textured; AO on the kerbs); keep |
| InlayArc_R9_W12 | 4 | 124 | generated | 4 | 4 | ground set at the testbed finish (textured; AO on the kerbs); keep |
| Roof_Mast | 3 | 1792 | store | 3 | 3 | roof programme pieces, mostly out of the play view; store; next round |
| StreamCorner_4 | 3 | 14170 | store | 4 | 4 | judged in the r3 play frames (FacadeLit layers and colourways don't render outside Unity): streamline bands, rounded corners, kit on every face; keep |
| StreamBay_3W | 3 | 1444 | store | 4 | 4 | judged in the r3 play frames (FacadeLit layers and colourways don't render outside Unity): streamline bands, rounded corners, kit on every face; keep |
| RoundBench | 3 | 2512 | store | 4 | 4 | at the bar in the r3 frames; keep |
| CafeLamp_Testbed_Coral | 3 | 760 | generated | 4 | 4 | testbed-finish civic piece (077 / 026 rebuilds), AO baked; keep |
| PlanterRun_4_C_TB | 3 | 6476 | generated | 4 | 4 | testbed-finish civic piece (077 / 026 rebuilds), AO baked; keep |
| Tile_Tactile | 2 | 14844 | store | 3 | 3 | red tactile at 14.8k tris x2; store; keep the look, triangle cut needs Sky's OK |
| StreamCorner_3 | 2 | 12302 | store | 4 | 4 | judged in the r3 play frames (FacadeLit layers and colourways don't render outside Unity): streamline bands, rounded corners, kit on every face; keep |
| Bollard | 2 | 5116 | store | 3 | 3 | the old town's red lamp and bollard: the old piece wins outside the zone; inside, a finish pass (Glint) rather than a rebuild |
| RobotShopkeeper | 2 | 14642 | store | n/a | n/a | cast (Forma), not an environment piece: not scored |
| RobotButler | 2 | 16216 | store | n/a | n/a | cast (Forma), not an environment piece: not scored |
| Automat | 2 | 3967 | store | 4 | 4 | at the bar in the r3 frames; keep |
| Lockers_Play | 2 | 2918 | generated | 4 | 4 | built in brief 097 passes 1-3; collision now the replaced box or store piece exactly |
| Planter_Urn_Play | 2 | 3642 | generated | 4 | 4 | at the bar in the r3 frames; keep |
| InfoLightbox | 2 | 5370 | store | 3 | 3 | black frame round the lightbox; store; next round (_Play at the same size) |
| RobotNanny | 2 | 19688 | store | n/a | n/a | cast (Forma), not an environment piece: not scored |
| PhoneBooth_Single | 2 | 9298 | generated | 3 | 4 | rebuilt: PhoneBooth_Single (finish). saturated coral drum, thin rims, no foot |
| LightDotRow_1 | 2 | 168 | generated | 4 | 4 | ground set at the testbed finish (textured; AO on the kerbs); keep |
| UtilityCabinet | 2 | 4874 | store | 2 | 4 | rebuilt: UtilityCabinet_Play. flat powder-blue box |
| OvalPlanter | 2 | 3336 | store | 4 | 4 | at the bar in the r3 frames; keep |
| CafeLamp_Testbed_Amber | 2 | 760 | generated | 4 | 4 | testbed-finish civic piece (077 / 026 rebuilds), AO baked; keep |
| InlayBand_06_2 | 2 | 10 | generated | 4 | 4 | ground set at the testbed finish (textured; AO on the kerbs); keep |
| Roof_Pool | 1 | 2136 | store | 3 | 3 | roof programme pieces, mostly out of the play view; store; next round |
| StreamEnd_3 | 1 | 10820 | store | 4 | 4 | judged in the r3 play frames (FacadeLit layers and colourways don't render outside Unity): streamline bands, rounded corners, kit on every face; keep |
| BalconyBay_3 | 1 | 3340 | store | 4 | 4 | judged in the r3 play frames (FacadeLit layers and colourways don't render outside Unity): streamline bands, rounded corners, kit on every face; keep |
| PortholeBay_3L | 1 | 4322 | generated | 4 | 4 | judged in the r3 play frames (FacadeLit layers and colourways don't render outside Unity): streamline bands, rounded corners, kit on every face; keep |
| StreamEnd_4 | 1 | 12540 | store | 4 | 4 | judged in the r3 play frames (FacadeLit layers and colourways don't render outside Unity): streamline bands, rounded corners, kit on every face; keep |
| CornerPier_In_3 | 1 | 2068 | generated | 4 | 4 | judged in the r3 play frames (FacadeLit layers and colourways don't render outside Unity): streamline bands, rounded corners, kit on every face; keep |
| SideBay_3 | 1 | 2026 | generated | 4 | 4 | judged in the r3 play frames (FacadeLit layers and colourways don't render outside Unity): streamline bands, rounded corners, kit on every face; keep |
| CornerPier_In_4 | 1 | 2392 | generated | 4 | 4 | judged in the r3 play frames (FacadeLit layers and colourways don't render outside Unity): streamline bands, rounded corners, kit on every face; keep |
| MastCamera | 1 | 2182 | generated | 4 | 4 | built in brief 097 passes 1-3; collision now the replaced box or store piece exactly |
| CivicRobotDock | 1 | 2546 | generated | 4 | 4 | built in brief 097 passes 1-3; collision now the replaced box or store piece exactly |
| Dumpster_Civic | 1 | 2764 | generated | 4 | 4 | built in brief 097 passes 1-3; collision now the replaced box or store piece exactly |
| FlowerBed | 1 | 2718 | store | 4 | 4 | cards read well in the r3 frames (magenta in the sheet = textures not loaded in Blender); no Tripo needed |
| DrinkingFountain | 1 | 3122 | store | 4 | 4 | at the bar in the r3 frames; keep |
| CafeUpper_3_TB | 1 | 15573 | generated | 4 | 4 | testbed-finish civic piece (077 / 026 rebuilds), AO baked; keep |
| CafeUpper_3_Planting | 1 | 32835 | generated | 4 | 4 | testbed-finish civic piece (077 / 026 rebuilds), AO baked; keep |
| CafeBay_v2 | 1 | 5786 | generated | 4 | 4 | testbed-finish civic piece (077 / 026 rebuilds), AO baked; keep |
| RecordsTower_TB | 1 | 39691 | generated | 4 | 4 | testbed-finish civic piece (077 / 026 rebuilds), AO baked; keep |
| RecordsTower_Planting | 1 | 65692 | generated | 4 | 4 | testbed-finish civic piece (077 / 026 rebuilds), AO baked; keep |
| CivicCabinet | 1 | 9054 | generated | 4 | 4 | at the bar in the r3 frames; keep |
| RecordsEntrance_Official | 1 | 2284 | generated | 4 | 4 | testbed-finish civic piece (077 / 026 rebuilds), AO baked; keep |
| BoardingPavilion_TB_Square | 1 | 28766 | generated | 4 | 4 | testbed-finish civic piece (077 / 026 rebuilds), AO baked; keep |
| RecordsEntrance | 1 | 8450 | generated | 4 | 4 | testbed-finish civic piece (077 / 026 rebuilds), AO baked; keep |
| AtomFountain_Testbed | 1 | 41888 | generated | 4 | 4 | testbed-finish civic piece (077 / 026 rebuilds), AO baked; keep |
| CafeLamp_Testbed_Peach | 1 | 760 | generated | 4 | 4 | testbed-finish civic piece (077 / 026 rebuilds), AO baked; keep |
| CafeLamp_Testbed_Teal | 1 | 760 | generated | 4 | 4 | testbed-finish civic piece (077 / 026 rebuilds), AO baked; keep |
| CafeLamp_Testbed_Lilac | 1 | 760 | generated | 4 | 4 | testbed-finish civic piece (077 / 026 rebuilds), AO baked; keep |
| CafeInterior_Testbed | 1 | 18690 | generated | 4 | 4 | testbed-finish civic piece (077 / 026 rebuilds), AO baked; keep |
| CafeTerrace_TB | 1 | 352 | generated | 4 | 4 | testbed-finish civic piece (077 / 026 rebuilds), AO baked; keep |
| CafeTerrace_Planting | 1 | 44694 | generated | 4 | 4 | testbed-finish civic piece (077 / 026 rebuilds), AO baked; keep |
| CivicBedPlanting_dropoff-west | 1 | 23303 | generated | 4 | 4 | testbed-finish civic piece (077 / 026 rebuilds), AO baked; keep |
| CivicBedPlanting_facade-southeast | 1 | 26521 | generated | 4 | 4 | testbed-finish civic piece (077 / 026 rebuilds), AO baked; keep |
| CivicBedPlanting_facade-southwest | 1 | 28285 | generated | 4 | 4 | testbed-finish civic piece (077 / 026 rebuilds), AO baked; keep |
| CivicBedPlanting_office-north | 1 | 21426 | generated | 4 | 4 | testbed-finish civic piece (077 / 026 rebuilds), AO baked; keep |
| CivicBedPlanting_office-west | 1 | 52252 | generated | 4 | 4 | testbed-finish civic piece (077 / 026 rebuilds), AO baked; keep |

Scored 113 (4 robots not scored): before mean 3.80, ≤3: 21; after mean 3.88, ≤3: 14.

Collision (Keel's rule): every brief-097 piece declares the replaced piece's volumes and catalogue size (tools/decokit/collide097.py, collide097.json from kit.collide.json at 693495c5). Checked: with the pieces catalogued (scratch withKit), the site's grid rows, occluders, places, getaway and takedown equal the committed; only `grid.collide` (a digest naming the pieces) and `cast` (the people update) differ.
