# Animation library comparison (2026-10-27)

Sky: "we need a pack that has a whole range of consistent animations right, not just the pistol?" Full report: `out/scratch/aster/anim-packs/report.html`. Clip-by-clip shortlist: `actorcore-shortlist.json` (beside this file).

| Option | Cost (USD, 2026-10-27) | Of our 50 actions: covered / partial / no | Consistency |
|---|---|---|---|
| **A. ActorCore, clip by clip (recommended)** | **$424.50** (127 clips; budget ~$475) | 38 / 9 / 3 | One skeleton and one store; takes match within a pack, short blends between packs |
| A2. ActorCore, the same 20 whole packs | $2,042.99 | same | same |
| B. ActorCore civilian ($301.50) + Mocap Online Shooter bundle | $451.00 on sale (to 2 Nov) / $600.50 list | about A | Two studios, so a seam at unarmed↔armed |
| C. Mocap Online bundles + Mixamo filler | $321.91 sale / $648.89 list | 20 / 14 / 16 | Mixamo fills carry, tie, serve, door and car, so the stitching comes back |
| D. Mixamo only (today) | $0 | 23 / 9 / 18 | The problem |
| E. Kevin Iglesias Mega | $130 | 4 / 10 / 36 | Stylised, hand-keyed |

**ActorCore covers the game's signature actions:** fireman's carry and carried, the handcuff takedown and restraint pairs, the drag struggle, the café server, sit and stand, door push and pull, car in and out, and a pistol set built around kneeling at cover.

**Licence:** the Reallusion Content EULA (1 Aug 2025) Standard §2.1(B) allows commercial games, with no revenue cap. It is locked to one account, raw files can't be redistributed, and no AI training or AI output is allowed. §2.1(C) "embedded content within applications" needs written confirmation from sales@reallusion.com before buying.

**Still missing with A:**
- frisking, stashing a body, loading a captive into a car;
- partial: beckon, tray walk, an unarmed silent kill, waking up, the pin hump, the cabinet;
- the car takes were captured on an SUV and a pickup, so each car needs an offset;
- the phone takes use mobiles, so a handset prop is needed.

**Before buying:**
1. Run ActorCore's free motions through our import ($0).
2. Preview each clip in ActorCore's web viewer.
3. Get the §2.1(C) clause confirmed in writing.

**Rules once bought:** keep the raw FBX outside git (like `mixamo_library`). Never feed the clips to `tools/rag` or any model. Never let them reach `assignment-04`.
