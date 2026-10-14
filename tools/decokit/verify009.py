"""Brief 009 A: measure the built meshes (not the scripts): per piece and per LOD, every object (the piece and its parts): triangles, triangles per material, extents
about the pivot. Blender -b -P verify009.py -- [--ids A,B] ; writes out/decokit-009/measured.json. Builds through kit.build / kit.build_lod (the exporter's own path)."""

import json
import sys
from pathlib import Path

import bpy
from mathutils import Vector

REPO = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(Path(__file__).parent))
import kit  # noqa: E402

ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
IDS = ARGS[ARGS.index("--ids") + 1].split(",") if "--ids" in ARGS else ["SignalEye", "KerbStud", "LightBar_4", "LightDotRow_2", "LightDotRow_1", "CrossInlayArc_R24_W08", "CrossEdgeLine_2", "CrossEdgeLine_1"]
PX_PER_M = 1080 / 28  # play camera: orthographic size 14 on a 1080 p frame
r3 = lambda v: round(v + 0.0, 3)  # noqa: E731


def objects(obj):
    dg = bpy.context.evaluated_depsgraph_get()
    out = {}
    for o in [obj, *obj.children_recursive]:
        me = bpy.data.meshes.new_from_object(o.evaluated_get(dg), depsgraph=dg)
        me.transform(o.matrix_world)
        per, tris = {}, 0
        for p in me.polygons:
            m = me.materials[p.material_index].name if me.materials else "?"
            per[m] = per.get(m, 0) + len(p.vertices) - 2
            tris += len(p.vertices) - 2
        nz = [p.normal.z for p in me.polygons]
        up, down = sum(1 for z in nz if z > 0.5), sum(1 for z in nz if z < -0.5)
        c0 = Vector((0, 0, 2.70)) if "SignalEye" in o.name else Vector((0, 0, 0))
        outward = sum(1 for p in me.polygons if p.normal.dot(p.center - c0) > 0)
        # a pictogram or plate face looks out of the face it is on: its normal's y has the sign of its position
        facing = sum(1 for p in me.polygons if abs(p.normal.y) > 0.9 and p.normal.y * p.center.y > 0)
        co = [v.co for v in me.vertices]
        lo, hi = Vector([min(c[i] for c in co) for i in range(3)]), Vector([max(c[i] for c in co) for i in range(3)])
        out[o.name] = dict(tris=tris, materials=per, faces=len(me.polygons), normals=dict(up=up, down=down, outward_of_orb_centre=outward, face_plane_outward=facing), min=[r3(v) for v in lo], max=[r3(v) for v in hi], size=[r3(h - l) for l, h in zip(lo, hi)],
                           px_at_play_zoom=[round((h - l) * PX_PER_M, 1) for l, h in zip(lo, hi)])
        bpy.data.meshes.remove(me)
    return out


def measure(name, tag):
    obj = kit.build(name) if tag == "L0" else kit.build_lod(name, tag)
    bpy.context.view_layer.update()
    res = objects(obj)
    for o in [obj, *obj.children_recursive]:
        bpy.data.objects.remove(o, do_unlink=True)
    return res


def main():
    kit.load()
    out = {}
    for n in IDS:
        spec = kit.REGISTRY[n]
        lv = {"L0": measure(n, "L0")}
        if spec.get("near"):
            lv["L1"] = measure(n, "L1")
        out[n] = dict(category=spec["cat"], levels={k: dict(total_tris=sum(o["tris"] for o in v.values()), objects=v) for k, v in lv.items()})
        for k, v in out[n]["levels"].items():
            print(f"[M9] {n} {k}: {v['total_tris']} tris | " + ", ".join(f"{o.replace('DK_', '')}={d['tris']}" for o, d in v["objects"].items()))
    (REPO / "out/decokit-009/measured.json").write_text(json.dumps(out, indent=1) + "\n")


main()
