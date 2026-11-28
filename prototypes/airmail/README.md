# Meridian Air Post

A playable art and rendering study for The Suborned. A four-bay postal concourse, a clock tower and a corner dispatch office sit along a working tram street. Walk under the roof you can see from the squad camera. The brass letterbox seal comes from a high-resolution Blender casting. The service windows have recessed counters, frames and four painted sorting rooms. The far shore uses the Harbour skyline painting.

## Play

On Apple Silicon, open **Meridian Air Post.app**. From source, use Godot 4.7.2 with the Forward+ renderer.

```sh
cd prototypes/airmail
godot --headless --path game --import
godot --path game
```

Click pavement to walk, or use WASD. Right-drag to turn. Tab switches between the shoulder camera and a rotatable orthographic camera. Scroll to zoom in the overhead view, H to hide the controls and Escape to leave. Both cameras follow one resident. The study includes fourteen other residents and a moving Harbour tram. It covers this postal block and runs independently of the Unity campaign.

L cycles through the original daylight, the relit arcade and the relit arcade with the postal finishes. The player opens with all finishes enabled. Walk from the tram stop to the booking counter to cross the sun line. The collection box opposite that counter carries a cast roundel, letter slot and service door.

## Studio result

Measured on a Mac Studio with an M5 Max and 128 GB of unified memory, running macOS 27.0.1 and native Metal. Draw calls below are the median total per frame, including shadows. The 95th percentile is the frame interval that 95% of the samples met or beat.

| Play camera | Mean fps, full / optimized | 95th percentile, full / optimized | Draw calls, full / optimized |
| --- | ---: | ---: | ---: |
| Over the shoulder | 120 / 120 | 9.44 / 8.60 ms | 99 / 80 |
| Isometric | 120 / 120 | 9.30 / 8.73 ms | 188 / 139 |

Both versions settled near the Studio's 120 Hz presentation cadence. Grouping reduced submissions by 19% on the street and 26% overhead. Instancing each repeated module type separately increased the street total to 108 draws. Spatially combining different modules that shared the atlas handled this block better.

The open passage took 8.38 ms at the median with 47 draws. Turning the same three bays into an L took 8.39 ms with 48 draws. The M4 MacBook Air completed the same geometry test on AC power. The grouped street averaged 61.4 fps at 17.60 ms p95. The grouped isometric route averaged 64.7 fps at 17.44 ms p95.

During a separate 125-second run alternating between cameras, the player's macOS resident set went from 263.3 to 264.7 MiB after warm-up. Godot reported 144 live resources at both ends. The native player archive is 35.0 MiB.

## The kit

The Blender source builds 14 parts with three authored levels of detail per part. Each mesh has one single-sided opaque PBR material. The kit includes three vaulted counter bays, a rotunda and roof lantern, a clock tower, two ribbon-window frontages, a dispatch end wall, a distant spire, a cast-letter sign, a postal box, a parcel trolley, a bench and a flower trough. Frontage and vault sections meet on six-metre sockets. The mesh names end in `_L0`, `_L1` and `_L2`. Use one level at a time.

The portable kit archive includes the packed Blender scene, the casting master, a self-contained PNG-textured GLB, 2K and 1K KTX2 GLBs and a triangle inventory. Their four mipmapped KTX2 maps occupy an estimated 10.7 and 2.7 MiB respectively on a compatible GPU. The Godot project keeps the same maps as importable sources and builds native compressed textures.

The rectangular rooflights, rotunda lantern and dispatch return are modelled for the overhead and street cameras. A fixed reflection panorama from the arcade supplies local shapes in the metal and glazing. The game draws the daylight sky separately. People, the tram, foliage and shadows remain live. The flying camera adds light haze in its first two shots.

## Reproduce the measurements

The benchmark uses the exported ARM64 release and a fixed 1920 × 1080 render target with render scale 1 and VSync disabled. The visible window can be smaller. The normal player uses VSync. With Godot's matching macOS export template installed, run the following from this directory.

```sh
python3 author/pack_mac.py "/tmp/Meridian Air Post.app"
player="/tmp/Meridian Air Post.app/Contents/MacOS/Meridian Air Post"
"$player" --resolution 1280x720 --disable-vsync -- \
  out=/tmp/meridian-benchmark raster=1080 suite=1 repeats=2 warm=85 frames=220
godot --headless --path game --script "$PWD/author/input_smoke.gd"
```

Each repetition follows the same street, isometric and corner routes. The second pass reverses the case order. After 85 warm-up frames the recorder retains 220 intervals per case. The nine conditions produce 3,960 measured frames on each machine. The complete percentile and draw-count results are in [results.json](results.json).

| Mode | Rendering |
| --- | --- |
| `full` | Individual modules at highest detail, individual street fixtures, occlusion off |
| `instances` | Distance-based LODs and spatial MultiMesh groups per module type, individual street fixtures, occlusion off |
| `lod` | Shared-material meshes combined within 18 m cells and by prop size, grouped street fixtures, occlusion off |
| `optimized` | The same combined meshes with solid building cores used for occlusion |

The open and turned corner use the same three frontage bays and their dispatch return. Only their positions and orientations change. The route, sky, actors, number of modules and material set match. Visible and shadow-pass counters are reported separately. Frame intervals include presentation and scheduling. The Metal backend returned no GPU timing samples.

For the 125-second camera loop, `python3 author/resident_memory.py "$player" /tmp/meridian-soak` records the macOS process resident set alongside Godot's resource counts. This Metal build returned an invalid texture byte counter. The report leaves that field empty.

To capture the four-shot camera move at a fixed 30 fps, run the following. Movie capture is separate from the benchmark.

```sh
godot --path game --script "$PWD/author/film.gd" --resolution 1440x810 \
  --fixed-fps 30 --write-movie /tmp/meridian.avi --quit-after 420
```

## Asset sources

The store's `concepts/AtomicAge/monorail_terminal.png` guided the shell rhythm and tower. The rotunda draws on `concepts/Streets/hero/hotel_frontage.png`. The distant shoreline is clipped from `concepts/AtomicAge/skyline_panorama.png`. The street follows the project's Harbour palette, flush tram rails and wire-free transit. [Suborned art bible](../../contracts/style/art-bible.agent.md)

The two animated figures and the tram derive from existing Harbour L1 meshes. The palm comes from the store's `testbed-art/palm/palm-gameplay-v1.glb`. Four original workroom studies were generated with the project's Codex art helper and incorporated in the counter atlas.

The road, terrazzo aggregate and daylight panorama incorporate CC0 source material. [Asphalt 04](https://polyhaven.com/a/asphalt_04) · [Terrazzo Tiles](https://polyhaven.com/a/terrazzo_tiles) · [Kloofendal partly cloudy pure sky](https://polyhaven.com/a/kloofendal_48d_partly_cloudy_puresky)

The stone courses use the photographed colour, OpenGL normal and roughness from Poly Haven's Marble 01, repeated across 2.40 m on walls and 2.85 m on paving. The stone keeps the scan's natural pits and coursing. The other finish maps and the lettering are authored for this kit. [Marble 01](https://polyhaven.com/a/marble_01) · [CC0 licence](https://polyhaven.com/license)

Horror Asylum's shipped `mat_asylum_arch` and `mat_asylum_props` informed the material treatment. Its 2K architecture page gives glazed tile, plaster and flooring distinct colour, relief and roughness. Meridian uses a vitreous 240 × 120 mm faience wainscot, a polished loading edge, folded copper roof sheets and a collection hatch. Graphics occupy their own UVs. Each bay and prop retains one opaque material and all three mesh LODs. Source reference in the neighbouring project is `src/blender/common/materials_high.py` and the 2K glTF material resources under `viewer/public/models`.

The new sun direction matches the daylight panorama. A 50 × 12 × 20 irradiance grid stores sky visibility and a first warm reflection off the static arcade for architecture, paving and moving residents. Four unshadowed, limited-range counter lights share one visible fitting mesh. A fixed reflection capture carries the shape of the street into the polished metal.

## Rebuild the architecture

Use Blender 5.2 and Python with Pillow, NumPy and SciPy. Run from this directory. The resulting Blender file packs the texture maps. The portable GLB embeds them. `author/split_images.py` prepares the Godot copy with external images so a fresh checkout can import the compressed textures before loading the meshes.

```sh
build_dir="$(mktemp -d)"
python3 author/atlas.py --source art --rooms art/workrooms.jpg --out "$build_dir/maps"
blender -b -P author/stamp_bake.py -- "$build_dir/bake" "$build_dir/maps/base.png"
python3 author/pavement.py art game/assets "$build_dir/bake" "$build_dir/maps"
blender -b -P author/build.py -- --maps "$build_dir/maps" --dest "$build_dir/kit"
cp "$build_dir/kit/meridian.glb" game/assets/meridian.glb
python3 author/split_images.py game/assets/meridian.glb
python3 author/finishes.py art game/assets
godot --headless --path game --import
python3 author/import_settings.py game/assets
godot --headless --path game --import
```

The authoring scripts also include the six-face static reflection capture and the image projection used by the sky shader. To rebake after changing the street, run `godot --path game --script "$PWD/author/reflection_faces.gd" --resolution 512x512 -- "$build_dir/faces"`, then `python3 author/reflection.py "$build_dir/faces" game/assets/arcade_reflection.jpg`.
