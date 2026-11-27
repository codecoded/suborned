# Quay porter and flower clerk

These civil-service machines follow fixed rounds. The porter serves ferry landings and platforms. The clerk tends flowers at market counters and carries folded papers on news rounds. Their reference is the Harbour painting, the service-machine study in the studio art store and the robot section of the art bible.

Open `Porter.blend` or `Clerk.blend` and play frames 1–120 at 24 fps. The editable parts include the luggage deck, its four-bar linkage and wheels, the clerk’s jointed hands and casters, and each destination display. The two concept sheets are in `briefs/119/harbour-robots/` on the Studio and in the art store under `concepts/Robots/harbour-service/`. The underside, wheel forks and enclosed drive are interpretations of the construction sheet.

| Machine | Height | Close | Middle | Far |
| --- | ---: | ---: | ---: | ---: |
| Quay porter | 1.35 m | 9,610 triangles | 3,908 | 746 |
| Flower clerk | 1.68 m | 10,636 triangles | 3,940 | 792 |

The Unity meshes use one shared enamel, metal and leather atlas, plus an inset dot-matrix display. The far stage merges the fixed machinery. The clerk’s two interchangeable loads are included in its totals. In play, wheel rotation follows distance travelled and the hands or deck work during the scheduled stop. Pausing or seeking the simulation also pauses or seeks the mechanism.

From the repository root, run `python3 tools/decokit/harbour_robots/finishes.py`, then `blender -b --python-exit-code 1 -P tools/decokit/harbour_robots/build.py`. Add `-- --review /absolute/output/directory` for the clay and two working poses. The exporter reimports all six FBXs and checks scale, UVs, materials, triangle ceilings, deck levelling and wrist travel.
