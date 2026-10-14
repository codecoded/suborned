"""Brief 008 B2: measure the built meshes (not the scripts) against the block's numbers. Blender -b -P verify008.py
Builds RecordsEntrance, RecordsEntrance_Official and CafeBay the way the exporter does (kit.build), reads every vertex, and
reports the extents in the block frame (civic-block.adjust.json `dressing.pieces`: the placements and yaws it gives).
Writes out/decokit-008/measured.json. Nothing here knows the dimensions it checks: they are read off vertices."""

import json
import math
import os
import sys
from pathlib import Path

import bpy

REPO = Path(__file__).resolve().parents[2]
os.environ.setdefault("DECOKIT_TEXDIRS_EXTRA", str(REPO / "out/decokit-008-src/Textures/derived"))
sys.path.insert(0, str(Path(__file__).parent))
import kit  # noqa: E402

PLACE = {"CafeBay": ((-10.7, -29.0), 90), "CafeTerrace": ((-10.7, -29.0), 90), "RecordsEntrance": ((52.5, -30.0), -90), "RecordsEntrance_Official": ((75.5, -30.0), 90)}
r3 = lambda v: round(v + 0.0, 3)  # noqa: E731


def verts(name):
    """[(x, y, z, material)] of every face corner of the built piece (evaluated, world space), one row per polygon corner."""
    obj = kit.build(name)
    dg = bpy.context.evaluated_depsgraph_get()
    rows = []
    for o in [obj, *obj.children_recursive]:
        me = bpy.data.meshes.new_from_object(o.evaluated_get(dg), depsgraph=dg)
        me.transform(o.matrix_world)
        for p in me.polygons:
            m = me.materials[p.material_index].name.replace("DK_", "") if me.materials else "?"
            for i in p.vertices:
                v = me.vertices[i].co
                rows.append((v.x, v.y, v.z, m))
        bpy.data.meshes.remove(me)
    for o in [obj, *obj.children_recursive]:
        bpy.data.objects.remove(o, do_unlink=True)
    return rows


def to_block(name, lx, ly):
    (x0, y0), yaw = PLACE[name]
    c, s = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
    return x0 + lx * c - ly * s, y0 + lx * s + ly * c


def box(name, pts):
    """Block-frame bounds [xmin, xmax, ymin, ymax] of local (x, y) points."""
    b = [to_block(name, p[0], p[1]) for p in pts]
    xs, ys = [p[0] for p in b], [p[1] for p in b]
    return [r3(min(xs)), r3(max(xs)), r3(min(ys)), r3(max(ys))]


def pick(rows, f):
    return [r for r in rows if f(*r)]


def public(rows):
    n = "RecordsEntrance"
    L = lambda x, y, z, m: m == "Limestone"  # noqa: E731
    out = {}
    land = pick(rows, lambda x, y, z, m: m == "Limestone" and abs(z - 0.75) < 1e-4 and -1.6 < y < 0.001 and abs(x) < 2.4)
    out["landing"] = dict(top_z=r3(land[0][2]), xy=box(n, land))
    treads, ys = [], {}
    for k in range(1, 6):
        tk = pick(rows, lambda x, y, z, m, k=k: m == "Limestone" and abs(z - (0.75 - 0.125 * k)) < 1e-4 and -3.301 < y < -1.499 and abs(x) < 2.31)
        treads += tk
        ys[k] = (r3(max(-r[1] for r in tk) - min(-r[1] for r in tk)), r3(0.75 - 0.125 * k))
    out["steps"] = dict(xy=box(n, treads), treads=5, going=[v[0] for v in ys.values()], tread_tops=[v[1] for v in ys.values()],
                        rise=r3(0.75 - ys[1][1]), risers=6 if abs(ys[5][1] - 0.125) < 1e-6 else None)
    ch = pick(rows, lambda x, y, z, m: m == "Limestone" and 2.31 < abs(x) < 2.6 and z < 1.3)
    out["cheeks"] = dict(xy=box(n, ch))
    pl = pick(rows, lambda x, y, z, m: m == "Limestone" and z < 1e-3 and abs(x) > 2.9)
    out["pier_plinths"] = dict(xy=box(n, pl), local_x=[r3(min(abs(r[0]) for r in pl)), r3(max(abs(r[0]) for r in pl))], local_y=[r3(min(r[1] for r in pl)), r3(max(r[1] for r in pl))])
    mq = pick(rows, lambda x, y, z, m: m in ("Enamel_Teal", "Enamel_Mint", "Enamel_Seafoam", "Enamel_Ivory", "Accent_Gold") and 4.04 < z < 4.63 and y < 0.0)
    out["marquee"] = dict(xy=box(n, mq), z=[r3(min(r[2] for r in mq)), r3(max(r[2] for r in mq))])
    allp = [(r[0], r[1]) for r in rows]
    out["piece"] = dict(width=r3(2 * max(abs(r[0]) for r in rows)), x_extent=[r3(min(r[0] for r in rows)), r3(max(r[0] for r in rows))],
                        front=r3(min(r[1] for r in rows)), back=r3(max(r[1] for r in rows)), height=r3(max(r[2] for r in rows)), xy=box(n, allp))
    over = [r for r in rows if abs(r[0]) > 3.98 + 1e-4]
    out["beyond_plinth_width"] = len(over)
    return out


def official(rows):
    n = "RecordsEntrance_Official"
    out = {}
    mq = pick(rows, lambda x, y, z, m: m in ("Enamel_Teal", "Enamel_Mint", "Enamel_Seafoam", "Enamel_Ivory", "Accent_Gold") and 4.04 < z < 4.63 and y < 0.07)
    out["canopy"] = dict(xy=box(n, mq), z=[r3(min(r[2] for r in mq)), r3(max(r[2] for r in mq))], embedded_behind_face=r3(max(r[1] for r in mq)))
    below = pick(rows, lambda x, y, z, m: z < 3.9)
    out["max_projection_under_canopy"] = r3(max(-r[1] for r in below))
    worst = {}
    for r in below:
        worst[r[3]] = max(worst.get(r[3], 0), -r[1])
    out["projection_by_material_m"] = {k: r3(v) for k, v in sorted(worst.items(), key=lambda kv: -kv[1]) if v > 0.05}
    out["floor_level_solids_beyond_0.31"] = len(pick(rows, lambda x, y, z, m: z < 0.7 and y < -0.31 and m in ("Limestone", "Chrome")))
    pil = pick(rows, lambda x, y, z, m: m == "Limestone" and abs(x) > 1.9)
    out["pilasters"] = dict(xy=box(n, pil), projection=r3(max(-r[1] for r in pil)), local_x=[r3(min(abs(r[0]) for r in pil)), r3(max(abs(r[0]) for r in pil))],
                            embedded_behind_face=r3(max(r[1] for r in pil)),
                            y_runs=[box(n, [(r[0], r[1]) for r in pil if (r[0] < 0) == neg])[2:] for neg in (True, False)])
    doors = pick(rows, lambda x, y, z, m: m == "GlassDark" and abs(x) < 1.5)
    out["door"] = dict(sill_z=r3(min(r[2] for r in doors)), plane_off_face=r3(-doors[0][1]), half_width=r3(max(abs(r[0]) for r in doors)))
    out["piece"] = dict(width=r3(2 * max(abs(r[0]) for r in rows)), front=r3(min(r[1] for r in rows)), height=r3(max(r[2] for r in rows)), xy=box(n, [(r[0], r[1]) for r in rows]))
    out["stairs_rails_cheeks"] = len(pick(rows, lambda x, y, z, m: y < -0.31 and z < 3.9 and m == "Chrome" and abs(x) > 0.5))
    return out


def cafe(rows):
    """CafeBay (ground storey, canopy, sign) alone: the terrace wall is its own piece since 14 Oct."""
    n = "CafeBay"
    out = {}
    can = pick(rows, lambda x, y, z, m: m in ("Enamel_Ivory", "Enamel_Seafoam") and 4.1 < z < 4.7 and y < -0.5 and abs(x) < 6)
    out["canopy"] = dict(reach_block_x=r3(-10.7 - min(r[1] for r in can)), soffit=r3(min(r[2] for r in can)), top=r3(max(r[2] for r in can)))
    out["piece"] = dict(width=r3(max(r[0] for r in rows) - min(r[0] for r in rows)), front=r3(min(r[1] for r in rows)), height=r3(max(r[2] for r in rows)))
    out["wall_stone_in_bay"] = len(pick(rows, lambda x, y, z, m: m == "Limestone" and 0.55 < z < 0.7 and y < -1.0))
    return out


def terrace(rows):
    n = "CafeTerrace"
    out = {}
    wall = pick(rows, lambda x, y, z, m: m == "Limestone" and 0.55 < z < 0.7)  # nothing else in the piece is limestone at that height
    flat = [r for r in wall if -6.9 < r[1] < -5.9 and abs(r[0]) < 5.8]
    out["coping"] = dict(local_y=[r3(min(r[1] for r in flat)), r3(max(r[1] for r in flat))], block_x=[r3(-10.7 - max(r[1] for r in flat)), r3(-10.7 - min(r[1] for r in flat))])
    neg = max(r[0] for r in flat if r[0] < 0)
    pos = min(r[0] for r in flat if r[0] > 0)
    out["gate_stone"] = dict(width=r3(pos - neg), block_y=[r3(-29 + neg), r3(-29 + pos)])
    zone = pick(rows, lambda x, y, z, m: -7.2 < y < -5.9 and abs(x) < 5.7 and z < 2.0)
    out["gate_zone_materials"] = sorted({r[3] for r in zone if abs(r[0]) < 2.59})
    neg2 = max(r[0] for r in zone if r[0] < 0)
    pos2 = min(r[0] for r in zone if r[0] > 0)
    out["gate_clear_all_materials"] = dict(width=r3(pos2 - neg2), block_y=[r3(-29 + neg2), r3(-29 + pos2)])
    w = pick(rows, lambda x, y, z, m: y < -0.6 and z < 2.0 and m in ("Limestone", "Leaf_Civic", "Leaf_CivicDark", "Bloom_Coral", "Bloom_Cream", "Bloom_Flamingo", "Bloom_Peach"))
    out["wall_all"] = dict(xy=box(n, [(r[0], r[1]) for r in w]), top_z=r3(max(r[2] for r in w)))
    lim = wall
    out["wall_stone_ends_block"] = [[r3(v) for v in to_block(n, r[0], r[1])] for r in sorted(lim, key=lambda r: r[0])[:1] + sorted(lim, key=lambda r: r[0])[-1:]]
    lo = min(lim, key=lambda r: r[0])
    hi = max(lim, key=lambda r: r[0])
    out["wall_arc_ends_local"] = dict(left=[r3(lo[0]), r3(lo[1])], right=[r3(hi[0]), r3(hi[1])])
    out["piece"] = dict(width=r3(max(r[0] for r in rows) - min(r[0] for r in rows)), front=r3(min(r[1] for r in rows)), height=r3(max(r[2] for r in rows)))
    return out


def radius(name):
    rows = verts(name)
    return r3(max(math.hypot(r[0], r[1]) for r in rows)), r3(max(r[0] for r in rows)), r3(min(r[0] for r in rows)), r3(max(r[1] for r in rows)), r3(min(r[1] for r in rows))


def main():
    kit.load()
    res = {"frame": "block frame, metres; piece placements from civic-block.adjust.json dressing.pieces"}
    res["RecordsEntrance"] = public(verts("RecordsEntrance"))
    res["RecordsEntrance_Official"] = official(verts("RecordsEntrance_Official"))
    res["CafeBay"] = cafe(verts("CafeBay"))
    res["CafeTerrace"] = terrace(verts("CafeTerrace"))
    res["CafeSet_Tulip_Play"] = dict(zip(("max_radius", "x_max", "x_min", "y_max", "y_min"), radius("CafeSet_Tulip_Play")))
    res["Parasol_Stripe"] = dict(zip(("max_radius", "x_max", "x_min", "y_max", "y_min"), radius("Parasol_Stripe")))
    tp = verts("PottedTopiary_Play")
    res["PottedTopiary_Play"] = dict(width=r3(max(r[0] for r in tp) - min(r[0] for r in tp)), height=r3(max(r[2] for r in tp) - min(r[2] for r in tp)), base_z=r3(min(r[2] for r in tp)))
    out = Path(os.environ.get("VERIFY008_OUT", REPO / "out/decokit-008/measured.json"))
    out.write_text(json.dumps(res, indent=1) + "\n")
    print("[MEASURED]", json.dumps(res))


main()
