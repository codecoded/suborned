# One rendering implementation for the played city

Sky, 4 November 2026, during the first Linux build: "old town / city? Why two
lots?", then "sounds like we should consolidate".

There is one played map. Its surrounding old-town assets and the newer Handover
zone currently use separate generations of DecoKit materials and shaders. Unity
compiles both shader families for the same player.

The copies have diverged. `Shaders/DecoKit/DK_Cutaway.hlsl` has the current
character capsules, multiple visibility points, rim, shadow and reach controls.
The old-town copy has the earlier single-focus cutaway. The newer Lit shader also
has surface-detail and reflection controls. The old material builder still asks
for `OldTown/DecoKit/Lit Cutaway` and `OldTown/DecoKit/Complex Lit Cutaway`.

Use the newer DecoKit implementation as canonical. Preserve the old town's
material colours, textures, surface settings and silhouettes while moving it to
the current visibility behaviour. Keep leaf shading as an intentional specialised
variant: vertex colour and double-sided normals are real features, not duplicates.

The consolidation should:

1. Inventory materials and scene/build dependencies on both shader GUID families,
   including embedded materials and generated prefab parents.
2. Update the old material and ground generators to request the canonical shaders.
   Update the asset-port step so it does not reintroduce the legacy shader copies.
3. Migrate existing materials through Unity, preserving their properties, render
   queues and keywords. Check the reflection scale and surface-detail defaults;
   those differ between the copies. Do not edit generated scene YAML by hand.
4. Compare the same city views before and after: all four tactical headings,
   third person, day/night, people hidden by buildings, shadows, chrome and leaves.
5. Remove legacy shaders only once the dependency check proves nothing still
   needs them. Compare shader/variant counts and player contents as well as images.

The first Linux build is the baseline. This audit does not perform the migration.
Consolidation removes a maintenance fork; it does not by itself eliminate the
large variant workload of the canonical shader. The cold Linux compile had
7,488 variants for the newer cutaway versus 1,152 for the old-town cutaway;
feature/variant stripping is a separate optimisation to measure afterwards.

Relevant source: `Editor/OldTown/DecoKit/DecoKitMaterials.cs`,
`DecoKitGroundMesh.cs`, `Editor/World/FadeTags.cs`, `Editor/World/Look.cs`,
`Shaders/DecoKit/`, and the ported `OldTownArt/DecoKit/Shaders/`.
A7 and its evidence stay locked.
