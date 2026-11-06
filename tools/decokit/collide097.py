"""Brief 097 (Vela; Keel's rule, 2026-11-06): the pieces brief 097 added or rebuilt declare exactly the collision of what they replace, so the engine's walk grid,
sight occluders and takedown points (tools/sim/solids.ts -> sim/data) stay byte for byte what they were. export_kit writes these as the manifest row's `collide`
(declared volumes, which tools/decokit/collide.ts takes in place of the measure).

ITEM   pieces that replaced a procedural box: tools/sim/solids.ts stood prism(rect(at, size[0], size[1], yaw), z, size[2]) for the street item. The piece is placed
       at the kit scale the slot fits (tools/kit/fit.ts: s = clamp(item h / piece h, tag scale), rounded to 1e-3, uniform here: none is squeezed), so the box is
       the item's box divided by that scale (kit frame: x along, y up, z across).
AS     pieces that rebuilt a store piece (or a generated one in place): the volumes the replaced piece had in contracts/world/kit.collide.json before brief 097
       (commit 693495c5), copied verbatim into collide097.json, with its catalogue size (export_kit writes it as the row's size: the slot then fits
       the same scale, so the volumes land exactly where the old piece's did)."""
import json
from pathlib import Path

ITEM = {  # id: (along, across, height) of the street item it replaced, and the tag scale range (tools/kit/tags.ts SCALE, default [0.9, 1.1])
    "PolicePost": ((1.0, 1.0, 2.8), (0.9, 1.1)),        # tools/world/civic/quiet.ts QUIET.size['police-post']
    "MastCamera": ((0.4, 0.4, 8.0), (0.9, 1.1)),        # tools/world/gen/street/public.ts, transit.ts: size [0.4, 0.4, 8]
    "CivicRobotDock": ((1.2, 1.2, 2.6), (0.9, 1.1)),    # standards catalogue civic-robot-dock size
    "Dumpster_Civic": ((2.1, 1.186, 1.513), (0.9, 1.1)),  # the stash item: the store Dumpster's size as the world exports it
}
AS = json.loads((Path(__file__).resolve().parent / "collide097.json").read_text())["pieces"]


def volumes(name, height):
    """The declared volumes for `name` (kit frame), or None if brief 097 does not pin it. height = the built piece's height (Blender z extent)."""
    if name in AS:
        return AS[name]["volumes"]
    if name in ITEM:
        (a, b, h), (lo, hi) = ITEM[name]
        s = round(min(hi, max(lo, h / height)), 3)
        return [dict(shape="box", min=[-a / 2 / s, 0.0, -b / 2 / s], max=[a / 2 / s, h / s, b / 2 / s])]
    return None
