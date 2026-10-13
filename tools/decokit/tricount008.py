"""Triangles per material of registered pieces (evaluated, as exported). Blender -b -P tricount008.py -- --ids A,B"""
import sys
from pathlib import Path

import bpy

sys.path.insert(0, str(Path(__file__).parent))
import kit  # noqa: E402

ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
reg = kit.load()
ids = ARGS[ARGS.index("--ids") + 1].split(",") if "--ids" in ARGS else list(reg)
for n in ids:
    obj = kit.build(n)
    dg = bpy.context.evaluated_depsgraph_get()
    per, tot = {}, 0
    for o in [obj, *obj.children_recursive]:
        me = bpy.data.meshes.new_from_object(o.evaluated_get(dg), depsgraph=dg)
        for p in me.polygons:
            m = me.materials[p.material_index].name if me.materials else "?"
            per[m] = per.get(m, 0) + len(p.vertices) - 2
            tot += len(p.vertices) - 2
        bpy.data.meshes.remove(me)
    print(f"[TRI] {n}: {tot} | " + ", ".join(f"{k.replace('DK_', '')}={v}" for k, v in sorted(per.items(), key=lambda kv: -kv[1])))
