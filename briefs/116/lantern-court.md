# Lantern Court and Harbour Travel

The proposed first neighbourhood follows the District Office café, a residential courtyard and a transit stop. Keep the wider streets available for walking and driving. Concentrate new art, dialogue and playtesting on this circuit. The Handover remains the main mission.

The existing café gives the cell a familiar place to come back to. Give the court a travel agency. A station timetable, destination posters and tickets tell the player what the clerk does. A shaded bench gives the porter somewhere to wait. Through the arch the player can see the public tram stop and decide whether to follow them. Front doors and a shared stair explain who lives upstairs. One mature tree has room to grow and shade the pavement.

## When to expand

Give a new player the cell and let them explore without directions. Can they recognise the three places from the ground? Can they work out who knows whom, ask one person for help and later recognise what someone else did on their behalf? Can they find another lead after passing up the first opportunity? A second neighbourhood earns its production budget when the first supports that play, including in the third-person camera.

The concept study uses the art store's Streets/hero/residential_morning.png as its visual reference. The uneven three- to five-storey wings, open court, shade tree, booking counter and view through to the tram are proposals for the residential rebuild. The studio artwork and both generation prompts are recorded in the local 116-lantern-court art ledger. The repeated roof-garden study is preserved on the local study/lantern-roof-gardens branch.

## In the player

Harbour Travel currently has a booking window on the public Skyport apron. When the courtyard is built, move the counter and its clerk together and give the porter the walk back to boarding. The new quay forecourt joins the coastal walk. Twelve residents walk between the forecourt and the boarding apron. Mingzhu Ma, the booking clerk, works with ferry porter Carlos Martinez. Carlos calls on tram conductor Kanoko Yamashita at Circus. Those introductions are directed relationships in the district's SurrealDB graph and are available to the ordinary citizen-order system. The timetable moves people around. It imposes no deadline on these relationships.

The pavilion uses a shared colour, metal and light atlas, three fitted meshes and a reflection drawn from its distant silhouette. Across the city, 837 decorated facade bays now belong to 315 groups. At long range 194 groups draw combined meshes. The old spherical planting was removed from 26 residential roof sections. The residential wings and courtyard in the study still need building.

Unity's Time Ghost team used several levels of detail and impostors to keep dense vegetation affordable. That approach becomes most useful here after the tree species, spacing, shade and silhouette work at walking height. Amplify Impostors is a candidate for distant canopies once there is a convincing close tree to bake. [Unity production notes](https://unity.com/demos/time-ghost) · [Amplify authoring manual](https://wiki.amplify.pt/index.php?title=Unity_Products%3AAmplify_Impostors%2FManual)

## Verification

The Studio player is 0.2.377 on Unity 6000.6.2f1 and Metal. A third-person walk to the boarding window exposed a 1.03 m overlap between the original apron and the simulated floor. The processed scene removes the superseded flat mesh. The player now stands on the same tiled apron as the passengers.

All 14 district and citizen-order tests passed, including a walk onto the forecourt, the route of every ferry passenger to the boarding apron, and the ordinary introduction search from Mingzhu through Carlos to Kanoko. All 495 headless scenarios replayed identically. TypeScript typechecking passed. The full Bun suite reported 37 failures. The offline verifier reported the same nine historical replay checks and six civic geometry checks as before this pass.

The standalone free-roam check exercised the contact journal, driving to a clicked destination and changing it, and sending an order through two intermediaries. It found 651 residents and three available leads. The next test is whether a first-time player can discover and use an introduction unaided.

At 1920 × 1080 on the Studio, the city-facing pier averaged 5.89 ms per frame in 0.2.373. The two 0.2.377 runs averaged 10.10 and 6.39 ms. Their 99th-percentile frame times were 38.42 and 7.70 ms respectively. The inlet reached 22.72 ms on the repeat, and both pier views draw more triangles than the baseline. The runs vary enough to warrant another waterfront profile before adding the residential wings. Measurements cover the Studio only. [Five camera timings across all three runs](studio-timings.tsv)
