# 072 capture (before), 2026-10-27 18:13

Scene from out/scratch/latch/072/baseline.world.json sha256 d6f7560de2f1ccfc89f156c715d3ee1d70d72adc54330cf896a2567395596edb. Play camera (ortho 14, pitch 38), Game view 1920x1080, GPU Resident Drawer Disabled in the Editor (EditorDrawerGuard keeps it off; the asset and the player run InstancedDrawing). Counts are UnityStats from the Game view (tris and verts include the shadow passes; draw calls = the Statistics window's Batches); renderers and census tris are the frustum + LOD estimate of the main view; ms is the Studio editor, indicative only.

| view | yaw | tris | verts | SetPass | draw calls | shadow casters | visible renderers | census tris | editor ms median / p90 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| office-front | 30 | 977,671 | 1,159,190 | 55 | 827 | 469 | 221 | 611,861 | 3.4 / 4.4 |
| office-front | 120 | 1,002,215 | 1,213,044 | 59 | 854 | 473 | 234 | 604,365 | 5.4 / 8.7 |
| office-front | 210 | 945,857 | 1,113,063 | 57 | 602 | 289 | 207 | 596,799 | 4.4 / 8.9 |
| office-front | 300 | 854,331 | 1,017,667 | 58 | 595 | 268 | 214 | 586,831 | 2.7 / 3.4 |
| cell-open | 30 | 1,454,243 | 1,654,981 | 69 | 1463 | 913 | 272 | 750,641 | 3.6 / 4.2 |
| cell-open | 120 | 1,405,678 | 1,625,099 | 66 | 1442 | 859 | 273 | 740,075 | 3.8 / 9.2 |
| cell-open | 210 | 1,379,150 | 1,553,104 | 68 | 1145 | 674 | 258 | 736,128 | 3.4 / 4.5 |
| cell-open | 300 | 1,322,310 | 1,499,233 | 67 | 1085 | 574 | 268 | 753,864 | 3.3 / 4.2 |
