# Brief 097 · the four procedural props as kit pieces (Vela, 2026-11-06)

Concepts: Codex ($0), `paid/097-props/codex/`, pass 1. Every prompt = `concept_prompt` era + city + the prop type's line and finish + rules.all + avoid, then the subject below.

## Picks

- **police post: A** (`police-post-A.png`), a teal enamel column with an ivory keyhole panel (hand-set and speaker), chrome ring pairs and an opal dome. From above it reads as a single identity: teal column plus glowing dome. B (`police-post-B.png`) is an ivory booth with a window. It reads as a kiosk with an interior, it is mostly ivory from above (blends into the cream stone), and it would need glazing.
- **mast camera** (`mast-camera.png`): a limestone flared foot, a tapering ivory pole with seafoam collars, and a streamline pod on a chrome yoke. Built with the pod at 1.25× so it reads at 8 m.
- **civic robot dock** (`civic-robot-dock.png`): an ivory alcove with a seafoam hood band and fins, an opal light strip under the hood lip, a chrome charging column, a dial and cradle shoes. Built simpler than the concept (no stepped deco fins).
- **dumpster** (`dumpster.png`): a teal body, three ivory roll lids with chrome ribs and a hinge bar, a kick rail, coral pulls, a brass hairline. Kept inside the store Dumpster's slot (the stash size and holds stay the store piece's).

## Pieces (tools/decokit/civic_props097.py)

| piece | world items | L0 before AO cuts | L0 | L1 | L2 | parts |
|---|---|---|---|---|---|---|
| PolicePost | police-post ×8 | 3408 | 3528 | 850 | 204 | PolicePost_Lamp (DK_Lamp dome: the host lights it on a call) |
| MastCamera | mast-camera ×3 (1 in the zone) | 2182 | 2182 | 545 | 129 | MastCamera_Head (turns about the mast; lens −Y = Unity +Z) |
| CivicRobotDock | civic-robot-dock ×8 (1 in the zone) | 2238 | 2546 | 558 | 129 | none |
| Dumpster_Civic | dumpster ×1 (the stash container) | 2420 | 2764 | 604 | 142 | none |

Palette materials only: DK_Enamel_Teal/Ivory/Seafoam/Charcoal/Coral, DK_Limestone, DK_Chrome, DK_Brass_Civic (hairline), DK_Lamp, DK_GlassDark. The bevel modifier is off (it cost 2–3×); the rounding is modelled. AO bake on (vertex_ao.PIECES).

Renders: `props/<piece>_34.png` (three-quarter) and `props/<piece>_play.png` (38°, yaw 30, play zoom at 80 px per 1.75 m figure, grey figure post for scale); `tools/decokit/review097_props.py`.

## Prompts

### police-post-B.png

Subject: a city police post on the kerb, about 1 m across and 2.8 m tall, shaped like a slender rounded capsule booth: ivory enamel body with a seafoam-teal band and a teal crown ring, a pale stone foot, a glowing opal glass beacon dome with a chrome ring on top, a curved front door-panel with a porthole speaker and a hand-set cradle, a thin brass hairline. Centuries-ahead technology in 1950s streamline style. Three-quarter view from slightly above.

### dumpster.png

Subject: a civic refuse container beside a service door, about 2.1 m long, 1.2 m deep and 1.5 m tall: a rounded streamline enamel bin in deep teal with ivory lid shells that roll back on chrome hinges, rounded corners, a chrome kick rail, short rounded feet, a thin brass hairline. Clean and prosperous, not grimy. Three-quarter view from slightly above.

### mast-camera.png

Subject: a tall civic surveillance mast, 8 m high and slim: a tapering ivory enamel pole on a rounded cream stone foot, teal enamel collars, and at the top a streamline camera head like a polished chrome and teal pod with a single round lens, on a turning yoke, a small opal tally lamp. Closed-circuit, watched by people; 1950s style, centuries-ahead technology. Whole mast in frame, three-quarter view.

### civic-robot-dock.png

Subject: a civic robot dock on a public square, about 1.2 m square and 2.6 m tall: a rounded streamline alcove of ivory and teal enamel on a honed cream stone base where a service robot stands to recharge, a curved canopy hood with an opal glass light strip, a chrome charging cradle and a small round dial; thin brass hairline trim. Empty dock, no robot. Three-quarter view from slightly above.

### police-post-A.png

Subject: a city police call post standing on the pavement kerb, about 1 m square and 2.8 m tall: a rounded streamline enamel column in deep teal and ivory with a honed cream stone plinth, a thin brushed-brass hairline at the base, an opal glass lamp dome on top that glows when a call is made, a recessed front panel with a hand-set telephone in a chrome cradle and a round speaker grille. Readable from high above at a distance by its dome and its silhouette. Three-quarter view from slightly above.


## Pass 2 (2026-11-06)

**PolicePost, rebuilt to concept A:** a round teal drum (r 0.36) with chrome ring pairs top and bottom, an ivory skirt and crown, a cream keyhole panel wrapped into the front with a chrome bead, the dark hand-set recess under a seafoam brow, a chrome hand-set, cradle and round speaker, the opal dome kept as the `DK_PolicePost_Lamp` part, on the honed limestone plinth with its brass hairline. It's 2.78 m tall, inside the 1.0 × 1.0 × 2.8 envelope.

**The five zone pieces furthest below the bar** (r3 crops against office-y210-bar / square-y030-bar):
1. **Lockers** (×2): a flat powder-blue slab with a square cream lid, red/blue door blocks and no base. The bar shows a teal enamel box with a honed stone cap and a brass foot.
2. **RecyclingBins** (×9): two plain cylinders with black lids and grey label patches. Present-day bins.
3. **PhoneBooth** (twin, ×7): the bar's own shape, but a saturated red drum, thin rims and no brass foot ring, at 17.8k tris. Not rebuilt: Sky asked for the old booth back (brief 026), so it's a material and finish job (Glint), not a new shape.
4. **VendingMachine_Cola / _Snack** (×16): boxy present-day vending cabinets with lettering.
5. **PottedTopiary** pot (×9): a plain grey cylinder; the bar has a cream pot with a teal band and brass. The foliage is fine. The pot sits in a store binary that _Play copies 1:1, so it's next if wanted (Blender reaches it; no Tripo needed).

**Rebuilt** (tools/decokit/civic_props097b.py, each at its store piece's size; the store pieces keep a retired tag; capstone-rules names the _Play lockers and vending pair):

| before | after | L0 before | L0 after (L1/L2) |
|---|---|---|---|
| PolicePost | PolicePost | 3528 | 3262 (804/190) |
| Lockers | Lockers_Play | 10712 | 2918 (706/139) |
| RecyclingBins | RecyclingBins_Play | 1676 | 1910 (477/114) |
| VendingMachine_Cola | VendingMachine_Cola_Play | 3958 | 2256 (466/104) |
| VendingMachine_Snack | VendingMachine_Snack_Play | 3246 | 2256 (466/104) |

None of these needs Tripo: each is a hard-surface prop. Renders: props2/before, props2/after (`_34` three-quarter, `_play` play zoom).
