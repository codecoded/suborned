# 006 · Kit transplant: the planned district built from the DecoKit

Goal (Sky, 2026-10-12): "proper planning and then transplanting some of that relevant kit and structures [from the-suborned], a better modelled world with real planning" · "good with procedural as well to fill things out if we can get it to amazing quality where it counts" · "in keeping with our atomic age / concept work and art bible".

The plan decides what stands where and why (004); the DecoKit supplies how it looks. Procedural geometry fills what the kit lacks, to the same bar. Nothing is hand-placed: the old repo's placement builders (DecoKitCityPlan.cs, DecoKitStreetSkyline.cs, …) are layout reference only.

## Sources (read-only, old repo)
- **DecoKit:** `~/Projects/the-suborned/game/Assets/TheSuborned/Art/DecoKit/`. Holds `decokit_manifest.json` (202 assets: name, category, fbx, tris, size m, materials, parts, lod1), `decokit_materials.json` (113 data-driven materials), Models (FBX), Prefabs, Materials, Textures, HDRI (CC0), and Shaders (DK Lit/ComplexLit Cutaway, Ocean, SpaceSky, Contrail, RobotFace). Pivot = footprint centre at ground; front = Unity +Z; 1 unit = 1 m.
- **Generator:** `~/Projects/the-suborned/workbench/Tools/Blender/decokit/` (56 .py: kit.py registry, geo.py, mats.py, mega_parts.py, one module per family; `blender -b -P export_kit.py`).
- **Later (005):** VAT crowd (Scripts/Crowd, Citizens3D/crowd), sky traffic (Scripts/Sky), RetroFutureMobility (GLB; needs glTFast), robots.
- **Concept work (the bar):** `~/Projects/the-suborned-store/concepts/`: AtomicAge (18, contact_sheet.jpg), Blocks (9: skyport tower, hanging terraces, lifted megablock), Streets (39), Props (27), District (4), Horizon (25), Style (107). Judge shots against these, not prose.

## Fixed
- **Look = the concept work.** Luminous cream limestone, streamline curves, pale teal and mint enamel, gold and chrome trim, coral as a touch, palms and cypress, warm haze. "Bright and colourful" = sunny and pastel, not saturated primaries. The harbour palette is re-tuned to the concept sheet; §15 is updated from the result (v0.17.0).
- **The plan rules everything:** sizes, heights, clearances and levels come from the plan and its checker; a kit piece is chosen to fit, or scaled within its stated range, never the other way round.
- **Kit where it counts, procedural to fill:** landmarks, megastructure signatures, towers, the civic block, frontages, signage and street furniture come from the kit. Bulk masses, decks and connectors are procedural, built to the same material and trim standard. Every procedural stand-in is listed in the export (`source: "procedural"`) so gaps are visible.
- **No repetition:** neighbouring buildings and signs must differ (metrics from Vela's probe: adjacent duplicates = 0).

## Binaries: gradual move (Sky, 2026-10-12)
Binaries stay in `~/Projects/the-suborned-store` (and the old repo) as the source. `bun run kit` copies only the pieces the catalogue marks `adopted` into `unity/Assets/Suborned/Kit/` (git-ignored), checking each file's sha256. Committed: `contracts/world/kit.json` (catalogue + hashes + source paths) and the import code. Pieces move over a few families at a time, as the generator starts using them; nothing is copied speculatively. Work happens on branch `world/harbour-district`.

## A · Splice · kit catalogue + generator output
objective: `contracts/world/kit.json` = the DecoKit manifest normalised (id, category, size, pivot, front, scale range, tags such as `use`, `trade`, `storeys`, `corner`), written by `bun run kit` from the old manifest with its sha256. The generator then emits a `kit` reference on every element it can fill:
- building bays and corners → Facades and Skyline sets by storeys and use;
- tower sites → Highrise;
- landmarks → SphereStation, Skyport_Tower, CircusMonument;
- street items → Street, Signals, Booths, Furnish, Plants;
- roof items → Rooftop;
- signs → Signage.

Each choice is seeded by id, with neighbour contrast. Otherwise it emits `source: "procedural"`.
done-check: `bun run world` is byte-identical twice; the coverage per layer is printed (kit vs procedural); tests show no adjacent duplicate facade or sign and that kit sizes fit their slots.

## B · Datum · old structures through planning
objective: compare the old landmarks, highrises and megastructure pieces (SpaceHQ, Skylon, ParabolicArch, DiscTower, Tower_Atom/Starburst/Lantern/Needle/Terrace, SphereStation, Skyport_Tower, LinerDock, SkyBridge) and the Blocks concepts with the plan's four megastructures and five tower sites. Adopt those that serve the district (skyline, levels, reach), sited under the plan's rules; reject the rest with a reason.
done-check: the checker exits 0 (including the 004 world rules), and a ≤ 30-line note maps each old piece → adopted or rejected.

## C · Latch · import + place
objective: import the DecoKit into `unity/Assets/Suborned/Kit/`. Rebuild materials from `decokit_materials.json`, port the DK shaders with the include path fixed (keep Cutaway for §15 ghosting), and import the HDRIs. The builder places kit prefabs by the export's `kit` refs (GPU instancing for repeats) and builds procedural only where `source: "procedural"`.
done-check: two clean builds are identical; counts match; 0 console errors; play-camera draws and vertices reported; shots: play, street, parade, skyline, docks, plus a concept-match set (the same subjects as AtomicAge/Blocks where the district has them).

## D · Vela + Glint · match the concept work
Vela: re-tune the palette and procedural fill to the concept sheet; materials consistent between kit and procedural. Glint: the lighting from the HDRIs and the concept light (low warm sun, soft haze). done-check: Sky judges the shots beside the concept images.

## E · Suture · review
Plan vs world vs sheet; repetition metrics; readability at play zoom (signals, 40–60 px); a report in `out/review/`.
