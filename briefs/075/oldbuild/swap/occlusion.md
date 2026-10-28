# Brief 075 swap · play-view occlusion (tools/world/occlusion.ts)

Share of pavement the play camera (orthographic, zoom 14, pitch 38°, yaws 30/120/210/300) cannot see behind buildings, 0.5 m raster; regions from the plan. Informational, not a rule.

| world | district pavement hidden | kerbside (≤ 8 m from a kerb) | Handover streets | figures (district) |
|---|---|---|---|---|
| our pipeline (pinned export as built, before the swap) | 29.4% | 43.2% | 41.6% | 28.2% |
| old-built, heights "old" | 28.3% | 37.4% | 53.3% | 27.0% |
| old-built, heights "rule" | 27.0% | 36.2% | 53.1% | 25.7% |

Command: `bun briefs/075/oldbuild/swap/tools/occlusion.ts` (kit sizes: out/scratch/latch/075swap/head/contracts/world/kit.json).
