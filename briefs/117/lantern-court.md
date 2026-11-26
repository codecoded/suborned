# Lantern Court

The housing block between Lantern Lane and Market Street is now a public court. Six residential wings turn around a brass and enamel globe. The lower wings face the lane, the clock tower rises from the northern wing, and a glazed bridge shelters the passage out to the tram. The site plan supplies the building outlines, planting positions and paved ground to both the model and the walking map. The existing District Office café remains the café on the circuit. Harbour Travel has a recessed ticket counter under a cantilevered canopy.

The visual references were Sky’s city sheet and the studio art store’s `concepts/Streets/hero/residential_morning.png`. The globe uses the public-domain [Natural Earth land outlines](https://www.naturalearthdata.com/downloads/110m-physical-vectors/110m-land/). The fitted stone, glazing and ticket-room art are in the two local brief 117 image jobs.

Sixteen named residents use the court. They wear the city’s existing residential outfits and walk between the globe and Market Street. Kimberly Tillman visits Ogonna Arinze at the Lantern newsstand. They are connected in the district’s SurrealDB graph and can make introductions through the same rules as the other citizens. All 651 previous district residents retain their names and simulation indices.

The first player walk revealed that a curved night-market kiosk filled the sightline from the lane. The market now trades from its cart and the court entrance stays open. Two seats face the court, and the original arcade’s opal pendant hangs from the underside of the bridge.

## Reproduce the street views

Build the district world, rebuild the played town and run the Studio player. The shot files are `briefs/117/lantern-court.json` and `briefs/117/harbour-street.json`. The walking shot sends the scout a normal movement order. The other courtyard shots use fixed street cameras. The pre-rebuild images and local player captures are in `out/scratch/aster/harbour-street-scale/`.

The court’s three groups of buildings and the globe share one material and switch distance meshes independently. Their combined meshes contain 71,854 triangles at the closest level, 38,718 at the middle level and 15,734 at the far level, before planting and ground. The ferry pavilion uses 11,258, 7,792 and 3,912 triangles.

The 15 district and citizen-order tests cover the through-walk, the solid building and clock footprints, local residents’ routes and the newsstand introduction. The full offline verifier still flags the same nine archived pipeline replays and the same five civic geometry checks as the previous Studio build.

Build 0.2.381 opened the Handover briefing and HUD on the M5 Max Studio. At 1920 × 1080 the daytime third-person sample averaged 7.71 ms per frame, with a 99th percentile of 11.71 ms. The first evening sample averaged 20.54 ms. On a second approach with the scout and the crowd present, the evening view averaged 12.23 ms on arrival and 7.90 ms once settled. These short captures cover the Studio only. The ten court views and the matching Lantern Lane shots show the booth’s removal in the built player. The simulation bundle passed 495 headless scenarios and 99 range scenarios with identical replays.
