"""Mesh bounds of kit pieces for `bun run geometry` (brief 055, Splice): where the shipped mesh really stands against what the catalogue says.

Blender -b -P tools/geometry/bounds.py -- <out json> <id>=<fbx path>[|<mat>,<mat>...] [...]
Brief 107: `|<materials>` names the light-only materials the piece carries (kit.collide.json `light`): light has no body, so their triangles are no part of the bounds; `tris` counts the whole mesh.
Each FBX is imported with the DecoKit export axes; written { id: { min: [x, y, z], max: [x, y, z], tris } } in the kit frame (x across, y up, z front: kit x = -Blender x, kit z = -Blender y), metres from the pivot.
"""
import json
import re
import sys
from pathlib import Path

import bpy

ARGS = sys.argv[sys.argv.index("--") + 1:]
OUT = Path(ARGS[0]); JOBS = [(a.split("=", 1)[0], *(a.split("=", 1)[1].split("|", 1) + [""])[:2]) for a in ARGS[1:]]
res = {}
for pid, path, lit in JOBS:
    light = set(m for m in lit.split(",") if m)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    try:
        bpy.ops.import_scene.fbx(filepath=path, axis_forward="-Z", axis_up="Y")
    except Exception as e:  # a piece that will not import is reported, not fatal
        res[pid] = {"error": str(e)[:120]}; continue
    xs, ys, zs, n = [], [], [], 0
    for o in bpy.context.scene.objects:
        if o.type != "MESH": continue
        me = o.data; me.calc_loop_triangles(); n += len(me.loop_triangles)
        names = [re.sub(r"\.\d{3}$", "", x.name) if x else "" for x in me.materials]
        keep = me.vertices if not light else [me.vertices[i] for i in sorted({i for t in me.loop_triangles if not (t.material_index < len(names) and names[t.material_index] in light) for i in t.vertices})]
        for v in keep:
            w = o.matrix_world @ v.co; xs.append(w.x); ys.append(w.y); zs.append(w.z)
    if not xs:
        res[pid] = {"error": "no mesh"}; continue
    res[pid] = {"min": [round(-max(xs), 4), round(min(zs), 4), round(-max(ys), 4)], "max": [round(-min(xs), 4), round(max(zs), 4), round(-min(ys), 4)], "tris": n}
OUT.write_text(json.dumps(res, separators=(",", ":")))
print(f"bounds of {len(res)} pieces")
