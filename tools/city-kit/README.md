# Lantern Exchange city kit

The market painting in the Suborned store and Sky’s coastal city board set the public scale, polished cream masonry and inhabited shop glass. The Lantern Exchange design study sets the bowed claret canopy, tall jade clock fin, paired upper windows and ground-floor trades. The rear elevations, roof and connections are inferred. The editable Blender builders keep these features independent.

The kit uses metres, Z up in Blender. The reusable six-metre bays face −Y with their origin at the middle of the pavement threshold. The 18-metre shop row fits three adjoining Harbour sockets, including their roof datum and upper window heights. The rounded court wings share the clock datum, footprints and floor counts from the district plan. Each installed row or court pair uses one opaque material and one active mesh renderer.

The canopy profile and raised stone piers carry the silhouette. The clock, fascia lettering and travel posters receive dedicated atlas regions. Fluted jade, drilled bronze soffits and honed stone use shared surface relief and roughness. Four illustrated shop interiors sit behind geometric jambs. The opaque atlas contains 2048 px base colour, 1024 px surface normals, and 512 px emission and packed material maps.

| Export | Close triangles | Street | Horizon |
| --- | ---: | ---: | ---: |
| ExchangeCourt, both wings | 17,978 | 13,602 | 1,226 |
| ExchangeShops, three adjoining bays | 2,826 | 2,354 | 260 |
| TicketBay, FloristBay, MarketBay, each | 450 | 354 | 240 |
| BladeCanopy | 444 | 260 | 68 |
| ClockFin | 386 | 282 | 158 |

From the repository root, regenerate the atlases with Python, Pillow and NumPy, then export the FBXs with Blender. The last argument selects where Blender saves the editable scene and triangle counts.

```sh
python3 tools/city-kit/finishes.py
blender --background --factory-startup --python-exit-code 1 --python tools/city-kit/build.py -- out/city-kit
```
