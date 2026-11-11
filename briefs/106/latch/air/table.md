| view | variant | median ms | p95 ms | GPU ms | main ms | triangles | SetPass | renderers drawn | Δ median vs off |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| play | all off (as built before 106) | 11.0 | 14.7 | 15.8 | 9.1 | 1.24 M | 151 | 216 | — |
| play | GPU occlusion on | 12.2 | 15.9 | 15.3 | 10.7 | 0.66 M | 227 | 216 | +1.1 (+10 %) |
| play | Mesh LOD on (threshold 16) | 11.2 | 15.6 | 15.9 | 9.9 | 1.24 M | 151 | 216 | +0.1 (+1 %) |
| play | Mesh LOD on (threshold 8) | 11.2 | 15.5 | 16.7 | 9.9 | 1.24 M | 151 | 216 | +0.2 (+2 %) |
| play | STP at 75 % | 15.9 | 19.9 | 19.1 | 10.7 | 1.85 M | 164 | 216 | +4.9 (+44 %) |
| play | STP at 70 % | 16.0 | 20.4 | 19.6 | 11.1 | 1.85 M | 164 | 216 | +5.0 (+45 %) |
| play | all three on (STP 75 %) | 16.3 | 20.6 | 24.4 | 11.8 | 0.72 M | 240 | 216 | +5.3 (+48 %) |
| tp-street | all off (as built before 106) | 17.6 | 21.9 | 22.6 | 15.2 | 6.33 M | 311 | 1508 | — |
| tp-street | GPU occlusion on | 18.7 | 23.8 | 19.3 | 19.5 | 2.10 M | 414 | 1508 | +1.1 (+6 %) |
| tp-street | Mesh LOD on (threshold 16) | 15.6 | 20.2 | 20.3 | 16.1 | 4.34 M | 311 | 1508 | -2.0 (-11 %) |
| tp-street | Mesh LOD on (threshold 8) | 17.3 | 21.9 | 22.6 | 15.5 | 5.86 M | 311 | 1508 | -0.3 (-2 %) |
| tp-street | STP at 75 % | 24.4 | 27.8 | 24.0 | 13.3 | 10.50 M | 361 | 1508 | +6.8 (+38 %) |
| tp-street | STP at 70 % | 24.3 | 28.1 | 24.1 | 14.1 | 10.48 M | 361 | 1508 | +6.7 (+38 %) |
| tp-street | all three on (STP 75 %) | 23.3 | 27.0 | 32.2 | 16.0 | 1.42 M | 464 | 1508 | +5.7 (+33 %) |
| tp-worst | all off (as built before 106) | 18.2 | 22.8 | 23.0 | 16.6 | 8.22 M | 298 | 1612 | — |
| tp-worst | GPU occlusion on | 22.6 | 30.1 | 15.9 | 23.4 | 2.24 M | 410 | 1612 | +4.3 (+24 %) |
| tp-worst | Mesh LOD on (threshold 16) | 16.5 | 20.3 | 21.1 | 16.3 | 6.40 M | 298 | 1612 | -1.8 (-10 %) |
| tp-worst | Mesh LOD on (threshold 8) | 17.8 | 22.1 | 22.4 | 15.9 | 7.52 M | 299 | 1612 | -0.5 (-3 %) |
| tp-worst | STP at 75 % | 26.3 | 29.0 | 25.7 | 12.5 | 14.19 M | 340 | 1612 | +8.0 (+44 %) |
| tp-worst | STP at 70 % | 26.5 | 29.6 | 26.3 | 14.1 | 14.17 M | 340 | 1612 | +8.2 (+45 %) |
| tp-worst | all three on (STP 75 %) | 23.4 | 31.1 | 22.5 | 24.4 | 1.90 M | 451 | 1612 | +5.2 (+28 %) |
