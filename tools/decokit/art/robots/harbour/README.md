# Harbour service robots

The quay porter brings luggage to the ferry and platforms. The flower clerk tends a market counter or carries folded papers on a news round. The Crumb Sweeper works the pavement outside coffee shops and market stalls. The Capsule Post Runner shuttles between a pneumatic post and its local counter.

| Machine | Height | Close triangles | Middle triangles | Far triangles |
| --- | ---: | ---: | ---: | ---: |
| Quay porter | 1.39 m | 9,952 | 3,796 | 744 |
| Flower clerk | 1.68 m | 10,978 | 3,820 | 794 |
| Crumb Sweeper | 0.48 m | 5,432 | 1,980 | 544 |
| Capsule Post Runner | 0.79 m | 5,496 | 2,074 | 542 |

Open the corresponding Blender file and play frames 1–120 at 24 fps. The porter lifts a level luggage deck on parallel links. The clerk reaches for a stem in its bowl. The sweeper’s two brush cartridges spin in opposite directions. The runner unfolds its brass mail flag and opens its letter hatch. The capsule fits between two saddle straps on its right flank. In play the district clock drives these actions, wheel rotation, blinks and glances. Pausing holds the pose.

Every distance model has smoked glass, an emissive matrix and the same eye and mouth layout. The porter and runner use amber lights. The clerk and sweeper use teal. A shared enamel and metal atlas covers the bodies. The clerk’s totals include both interchangeable loads.

The Harbour painting, the art bible and the approved face studies in `briefs/031/robot-face/` guide the models. The service and construction studies and the two selected utility designs are in `briefs/119/harbour-robots/` on the Studio, mirrored under `concepts/Robots/harbour-service/` in the art store.

From the repository root, run `python3 tools/decokit/harbour_robots/finishes.py`, then `blender -b --python-exit-code 1 -P tools/decokit/harbour_robots/build.py`. Add `-- --review /absolute/output/directory` for the clay and two working poses, or `-- --only Sweeper` to rebuild one machine. The exporter reimports all twelve FBX files and checks scale, UVs, materials and triangle ceilings. It also checks the porter’s deck, clerk’s wrist, brush rotation, mail flag, hatch and LED register changes.
