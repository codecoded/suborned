"""Palm LOD check (Vela, brief 028): level 0 of palm_lod.level equals the shipped piece, and the triangle counts and bounds of L1 and L2.

blender -b -P tools/decokit/palm_lod_check.py
"""
import sys
from pathlib import Path

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).parent))
import kit  # noqa: E402
import palm_lod  # noqa: E402,F401

bpy.ops.wm.read_factory_settings(use_empty=True)
kit.load()
palm_lod.register()
from geo import Shape  # noqa: E402


def meshes(root):
    return [o for o in [root, *root.children_recursive] if o.type == "MESH"]


def tris(root):
    dg = bpy.context.evaluated_depsgraph_get(); n = 0
    for o in meshes(root):
        m = bpy.data.meshes.new_from_object(o.evaluated_get(dg), depsgraph=dg)
        n += sum(len(p.vertices) - 2 for p in m.polygons); bpy.data.meshes.remove(m)
    return n


def verts(root):
    out = []
    dg = bpy.context.evaluated_depsgraph_get()
    for o in meshes(root):
        m = bpy.data.meshes.new_from_object(o.evaluated_get(dg), depsgraph=dg)
        out += [tuple(round(c, 4) for c in (o.matrix_world @ v.co)) for v in m.vertices]; bpy.data.meshes.remove(m)
    return sorted(out)


def bounds(root):
    pts = [o.matrix_world @ Vector(c) for o in meshes(root) for c in o.bound_box]
    lo = Vector((min(p[i] for p in pts) for i in range(3))); hi = Vector((max(p[i] for p in pts) for i in range(3)))
    return tuple(round(x, 2) for x in (hi - lo)), tuple(round(x, 2) for x in lo)


ok = True
for name, (H, wob, seed) in palm_lod.PIECES.items():
    ship = kit.build(name)
    s = Shape(f"T_{name}"); s.lean = 1.0
    palm_lod.level(s, H, wob, seed, 0)
    mine = s.build(None, bevel=0.0)
    a, b = tris(ship), tris(mine)
    va, vb = verts(ship), verts(mine)
    same = va == vb
    ok &= same and a == b
    print(f"[palm-lod] {name} L0 shipped {a} tris, level(0) {b} tris, vertices identical: {same}")
    for lv, tag in ((1, "L1"), (2, "L2")):
        o = kit.build_lod(name, tag)
        print(f"[palm-lod] {name} {tag} {tris(o)} tris, size {bounds(o)[0]}, vs L0 size {bounds(ship)[0]}")
print("[palm-lod] OK" if ok else "[palm-lod] MISMATCH")
sys.exit(0 if ok else 1)
