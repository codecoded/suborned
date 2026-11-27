# Harbour rendering audit

The M4 Air previously averaged 30.8 FPS on the 1080p daytime market walk. Its 99th-percentile frame took 53.7 ms. The same sample recorded 15.3 ms of GPU time and 32.5 ms on Unity's main thread. That run included other applications. A 60 FPS frame has 16.7 ms available. Repeat the walk on the Air before attributing a frame-rate gain to these changes.

## What the Studio player actually submitted

The Scout is on Lantern Lane at tick 1930, seed 9, in the 1920 × 1080 follow camera. The market and the camera are held fixed throughout the experiment. Values are medians from 36 frames per setting. Unity 6000.6.2f1 on Metal exposes separate native draw counters. The aggregate batch counter returns no samples. The table reports the two busiest paths independently. Null-geometry draws held at 23 and the other five draw paths reported zero. Unity defines SetPass as a shader-pass change. [Unity rendering profiler reference](https://docs.unity3d.com/6000.6/Documentation/Manual/ProfilerRendering.html)

| Corner treatment | Submitted triangles | Standard draws | Instanced draws | Shadow casters |
| --- | ---: | ---: | ---: | ---: |
| Original | 1,319,570 | 1,110 | 437 | 702 |
| Next native mesh level | 1,115,889 | 1,121 | 437 | 708 |
| Same mesh level and a single opaque shadow mesh | 1,028,269 | 1,086 | 352 | 592 |
| Repeat with the shadow mesh | 1,028,263 | 1,085 | 352 | 592 |
| Restore original | 1,319,192 | 1,112 | 437 | 704 |

Blender's FBX reader inventoried 1,352 project files and 1,713 mesh records. Nine independently imported FBXs gave the same triangle counts. The three-storey Streamline corner has 12,170 triangles and 13 material slots. Ivory and seafoam enamel account for 6,992 triangles between them. The player contains 76 three-storey corners and 16 four-storey corners eligible for this treatment outside the civic core. A streetlamp has 5,782 triangles at source. Temporarily hiding its Deco Street family made little difference to draw calls in this view.

The build uses four shared shadow meshes for those 92 corners in the review scene. Glass openings stay open. Masked or two-sided shadow materials retain their original caster. Close stonework keeps its UVs. The middle and far facade palettes retain the metal and smoothness of plain finishes through a shared linear 32 × 1 map. In the review scene, 66 palettes used 12 different finish maps, totalling 1,536 bytes of raw texels before allocation overhead. The existing shop-interior atlas supplies 16 additional ground-floor displays around the market.

## Where the remaining draws go

In a separate capture with the updated surfaces, all scene renderers produced 1,078 standard and 353 instanced draws. Temporarily hiding renderers whose nearest point was more than 100 m from the camera reduced this to 537 and 232. Temporarily disabling all shadow-casting lights produced 677 and 208. These tests overlap. The full skyline and the lights were restored after each measurement.

The same test individually hid the Southside straight facade bays (1,060 standard, 305 instanced), the rounded corners (1,044 and 293) and the Southside merged street tiles (1,010 and 353). These families warrant testing together from the follow and tactical cameras. The target is to preserve the play-distance shopfronts and replace distant per-piece draws with a detailed block silhouette.

Moving 315 outer facade groups onto their shared distance meshes earlier took daytime instanced draws from 353 to 302 and night draws from 360 to 303. The chosen daytime setting submitted 1,001,983 triangles and 1,075 standard draws. The opposite shopfront kept its window divisions in both views. A more aggressive setting reached 286 daytime instanced draws and removed a conspicuous row of those divisions. The build uses the smaller adjustment.

During 128 seconds of live simulation, the first and last ten samples had median resident texture memory of 426.9 and 428.4 MiB. Used GPU buffers rounded to 1,021.0 MiB at both ends. Unity's total allocated memory went from 2,017.5 to 2,008.3 MiB. The Studio session was locked. These checks verify geometry, resource counts and play, and a foreground Air run is needed to verify frame pacing.

## Repeat the checks

- Run the source inventory with Blender in background using `--python-exit-code 1 --python tools/decokit/audit_fbx.py -- out/harbour-assets.json`. Each FBX level is reported separately. An imported library total includes assets outside the current scene.
- Use `bun run batch --shots briefs/119/lantern-market-views.json` for the reproducible market walk. The capture reports the native draw paths, triangle count, shader changes, simulation time, GPU time and closest resident in the follow viewport.
- The Studio review player uses the previous Unity scene bake and the updated simulation and player assemblies. The normal build generates the fixtures, finish maps and shadow meshes during scene processing. A fresh Unity build requires an active Editor licence.
