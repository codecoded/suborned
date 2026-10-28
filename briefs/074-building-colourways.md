# Brief 074 · Building colours: a colourway per building

Owner: Aster. Opened 2026-10-28. Sky: **"Do building colors"** (2026-10-28), after brief 072's finding that the pushed palette (style contract v0.23.0, 12 wall families) barely shows: every wall is a kit module in one shared colourway (cream stone, teal fascia, seafoam bands), so the families reach only roofs, shopfronts and accents.

## Scope (Redline's costing, briefs/072/redline/phase3.md §5)
- At most 12 district-wide colourways, one per wall family in unity/Assets/Suborned/Editor/World/facade.json: cream, peach, mint, sky, sand, ivory, blush, powder, butter, lilac, apricot, seagreen. A colourway recolours the module's wall, band and fascia (and plinth where the piece has one) from that family. `civic` stays as today (seafoam and deep teal) and is not one of the 12. The powder-blue road is unchanged.
- Never one per building: per-building variety still comes from skins, shopfronts, roofs and accents.
- Brief 073 is untouched: no 073 finish goes into production. The Air sitting stays unscheduled.

## The contract (shared by all three owners)
- Colourway id = the facade.json family name. The world export decides each building's family (deterministic, seeded by building id) and writes it on the building and on every facade module (`colourway`). Unity reads it instead of picking a family itself, so roofs, plinths and modules agree.
- Neighbouring buildings get visibly different colourways: wall ΔE ≥ about 22 median between street neighbours (072's push reached a 22.1 median, 6.1 minimum); no two neighbours share a family.
- Materials: one material set per colourway, differing only in base colour (same shader, same keywords, so no new SetPass; SRP batcher and instancing kept). The wall texture is neutral so a tint can make blue and lilac walls.
- Column meshes are baked once per layout, never per colourway; materials are swapped per placed instance. Shadow proxies keep the base materials.

## Objectives
- **074-A · Splice:** the export assigns each building a colourway (family) by area weights, seeded by id, with the neighbour rule; writes it on buildings and modules; a check that fails on neighbours sharing a family or a module whose colourway differs from its building; two `bun run world` runs give the same hash. The civic block is `civic`.
- **074-B · Vela:** neutral base textures for the wall modules (SideBay, stacks, pilasters, piers, AlleyWall, ShopFill, ShopStack, and the street fronts' wall slots), and the 12 material sets (wall, band, fascia, plinth tints from facade.json families), generated in tools/decokit, deterministic, adopted via `bun run kit`. No new shader variants. Texture memory stays within the 24 MiB set budget.
- **074-C · Latch:** the builder reads the colourway, swaps materials per placed module and column instance, bakes each column layout once (KitColumns mesh key without the colourway), keeps shadow proxies on the base materials, and makes roofs and plinths follow the building's family. Rebuild, compile clean, offscreen shots: today against colourways at play zoom, café square (cell-open) and the Office front, yaw 30 and 210, plus one wide view across a district street.
- **074-D · Redline:** measure draws, batches, SetPass and CPU at play zoom and at street level (tp-street, tp-worst) against today; pass mark: no new SetPass; play zoom ≤ +40 batches; street level ≤ +260 module draws.
- **Keel/Waymark if needed:** the Handover site and checks still ship (colour-only changes should leave the site byte-identical).

## Status
