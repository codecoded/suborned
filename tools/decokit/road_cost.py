"""What the road finish adds to a frame (brief 028 road, Vela, 2026-10-22): pieces, triangles, materials and Combine batches in view, from tools/world/roadfinish-dev.ts's placements.

python3 -I tools/decokit/road_cost.py <placements.json> <out.json>

Not a frame-time measurement (that needs Latch's rebuild and the Air): a count of what the camera sees. Unity's Combine makes one mesh per (layer, material, 64 m tile), so the draw calls the finish adds are
(tiles in view) x (new materials in the ground-pieces layer); triangles are the piece's own (ribbon 16, shoulder 12, pinstripe 2, lozenge 4, seam 2: scaled along their length, never re-cut). The ribbon is
emissive, drawn unlit, and adds no light and no shadow caster; its pool is one alpha-blended strip 1 m wide beside each kerb (overdraw counted as area)."""

import json
import math
import sys
from pathlib import Path

P = json.loads(Path(sys.argv[1]).read_text())
TRIS = {"RoadRibbon_Straight_6m": 16, "RoadShoulder_Straight_6m": 12, "PaveEdge_Pinstripe_6m": 2, "RoadMark_Lozenge": 4, "RoadMark_Seam": 2}
MATS = {"RoadRibbon_Straight_6m": ["DK_Brass_Civic", "DK_Road_Lens", "DK_Road_Pool"], "RoadShoulder_Straight_6m": ["DK_Ground_RoadPearl", "DK_Chrome"], "PaveEdge_Pinstripe_6m": ["DK_Brass_Civic"], "RoadMark_Lozenge": ["DK_Brass_Civic", "DK_Ground_RoadPearl"], "RoadMark_Seam": ["DK_Chrome"]}
items = P["items"]
TILE = 64.0


def stats(sel):
    n, tris, mats, tiles, pool = 0, 0, set(), set(), 0.0
    for i in sel:
        k = i["kit"]["id"]
        n += 1
        tris += TRIS[k]
        mats.update(MATS[k])
        tiles.add((math.floor(i["at"][0] / TILE), math.floor(i["at"][1] / TILE)))
        if k == "RoadRibbon_Straight_6m":
            pool += i["size"][0] * 1.0
    return dict(pieces=n, tris=tris, materials=sorted(mats), tiles=len(tiles), batchesAdded=len(tiles) * len(mats), poolAreaM2=round(pool, 1))


def in_rect(i, c, yaw, hw, hd):
    # the play camera's ground footprint: a rectangle (half width hw across, half depth hd along) about the focus, turned to the camera yaw
    y = math.radians(yaw)
    dx, dy = i["at"][0] - c[0], i["at"][1] - c[1]
    along = dx * math.sin(y) + dy * math.cos(y)
    across = dx * math.cos(y) - dy * math.sin(y)
    return abs(across) <= hw and abs(along) <= hd


def in_cone(i, eye, yaw, far, half):
    y = math.radians(yaw)
    dx, dy = i["at"][0] - eye[0], i["at"][1] - eye[1]
    d = math.hypot(dx, dy)
    if d > far:
        return False
    if d < 3:
        return True
    return (dx * math.sin(y) + dy * math.cos(y)) / d > math.cos(math.radians(half))


tile_counts = {}
for i in items:
    t = (math.floor(i["at"][0] / TILE), math.floor(i["at"][1] / TILE))
    tile_counts.setdefault(t, []).append(i)
out = {
    "district": stats(items),
    "perTile": dict(tiles=len(tile_counts), busiestPieces=max(len(v) for v in tile_counts.values()), busiestTris=max(sum(TRIS[i["kit"]["id"]] for i in v) for v in tile_counts.values()), meanTris=round(sum(sum(TRIS[i["kit"]["id"]] for i in v) for v in tile_counts.values()) / len(tile_counts))),
    "play zoom 14 at the Records Office front (49.8 x ~45 m of ground)": stats([i for i in items if in_rect(i, (-160, -281), 210, 25 + 8, 23 + 8)]),
    "third person down Orbit Walk (far 450, 85 degrees)": stats([i for i in items if in_cone(i, (-184, -265), 90, 450, 42.5)]),
    "third person down the Boulevard (far 450, 85 degrees)": stats([i for i in items if in_cone(i, (-9.5, -104), 180, 450, 42.5)]),
}
Path(sys.argv[2]).write_text(json.dumps(out, indent=1))
print(json.dumps(out, indent=1))
