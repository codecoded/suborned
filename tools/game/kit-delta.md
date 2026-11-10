# Kit changes: what rebakes, and the per-piece path still to build

Splice, 2026-11-11. Sky: "kit changes rebake only what changed".

## Measured (batch 105, `--full`, logs in out/batch/logs)

| step | seconds | what it does |
|---|---|---|
| unity-world: HarbourWorldBuilder | 44 | our scene from the export, kit pieces as prefab instances of `Kit/Prefabs/Built/` |
| unity-world: OldTownSwap.Build | 186 | the old town rebuilt with the old builders, our zone copied in |
| unity-world: Verify + Install | 29 | the zone regression, the host |
| unity-world: ZoneBake097.BakeTown | 417 | the zone's lightmaps (GPU lightmapper, 768 indirect samples) |
| unity-match | 471 | Unity against headless; engine changes only (not on the kit row) |
| game-build: full proof | ~1000 | the kit row is `full` |

## Done this round (safe; no Unity code changed)

- `kit.json` is read by reader (`proofdelta.ts kitParts`): `#unity` holds the tables, the adopted rows and the rejected generated rows (what KitPlacer and KitImport read); `#rest` holds the candidates and the other rejected rows. `#rest`, the kit tools, the DecoKit generator and the kit schema run only the kit and world steps. The robots' faces (`tools/decokit/art/robots`, read by RobotKit) stay on the full kit row.
- The scene and frame-time gates (unity-world, unity-match, late-fight, tp-long) read a fresh snapshot once the kit or world step ran. A copy or export the kit or world step really moved is seen in the same batch, not the next one.
- Over the last 40 commits that touched the kit, 9 no longer rebuild the scene (about 11 min each) and 7 no longer run the full proof (about 17 min each). They were candidate rounds and generator edits, e.g. 5f5753ad7 (the lamp mast candidate) and 01c8cbd46 (the hover camera candidates). Check with `bun tools/game/proofwhat.ts diff <c>^ <c>`.
- `bun run batch --sky` leaves out the proof, so a kit change for Sky costs the rebake and a 13 s smoke, not another 17 min of proof.

## Not done: an adopted piece's change still rebakes the whole zone (about 11 min)

Why this is a Unity round (Latch's scenes), not a tools change:

1. **The swap unpacks every kit instance.** SwapOurs runs `UnpackPrefabInstance(..., Completely)` so the zone cut can take parts of pieces. OldTown.unity then holds plain renderers. A rebuilt FBX reaches them only while its mesh names and material slot order stay the same. The camera rebuild (b97ad75f4) moved DK_Brass_Civic to slot 0, so in-place pickup would give HoverCamera the wrong materials.
2. **Per-instance state is derived at build time.** That covers colourway, livery and poster swaps, the fade bits, shadow proxies from LOD1, HEAD material swaps, and the zone-edge clipping. A per-piece path has to redo all of it for each instance.
3. **The lightmaps are keyed to the scene just written.** Any swap means a full bake. A changed piece inside the zone needs one anyway: its UV2 and its bounce move.

Suggested next round (Latch, with Glint for the bake):

- **KitDelta.Apply.** Pass it the changed ids, from the `#unity` asset rows diffed against a record written at each unity-world pass. It rewrites only those Built prefabs (KitLods with an id filter, skipping its stale-prefab sweep). In HarbourDistrict.unity the instances follow the prefab. In OldTown.unity it replaces each unpacked instance of a changed piece with a fresh unpacked copy at the same transform, then re-applies the per-instance passes above. It falls back to the full rebuild when a changed piece is clipped at the zone edge, or used by KitColumns, PlantingSwap077, OldTownFoliage075 or Recovery075. Gate it on the export being unchanged and on `zoneProblem` against a full rebuild once.
- **Bake reuse.** After a bake, record each zone renderer's bake key (its path, mesh and material GUIDs, matrix and flags) with its lightmap index and scale/offset. On the next build, reuse the lightmaps when the keys match. A changed piece's renderers take probe light (`ReceiveGI.LightProbes`, as the window panes already do) until the full batch behind rebakes. That saves 417 s on every zone-neutral rebuild. Unity keeps editor lightmap indices in LightingDataAsset, so this needs a small runtime applier (the PrefabLightmapData pattern).
- Estimated: a kit change for Sky drops from about 11 min to about 1–2 min, outside the zone or with bake reuse.
