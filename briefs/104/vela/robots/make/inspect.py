"""Brief 104 (Vela): inspect every robot model file. Blender -b -P inspect.py -- <out.json> <file> [<file> ...]
Imports each file into an empty scene and reports triangles (evaluated, as drawn), size in metres (Blender Z up), mesh and material names,
and the parts that could be a face (names with face, display, screen, eye, visor, led, lens). Read-only on the sources."""

import json
import re
import sys
from pathlib import Path

import bpy
from mathutils import Vector

ARGS = sys.argv[sys.argv.index("--") + 1:]
OUT, FILES = Path(ARGS[0]), ARGS[1:]
FACE = re.compile(r"face|display|screen|eye|visor|led|lens|monitor|glyph", re.I)


def clear():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def load(path):
    p = path.lower()
    if p.endswith((".glb", ".gltf")):
        bpy.ops.import_scene.gltf(filepath=path)
    elif p.endswith(".fbx"):
        bpy.ops.import_scene.fbx(filepath=path)
    else:
        raise ValueError(path)


def info(path):
    clear()
    load(path)
    dg = bpy.context.evaluated_depsgraph_get()
    tris, lo, hi, meshes, mats, faces, images = 0, Vector((1e9,) * 3), Vector((-1e9,) * 3), [], set(), [], set()
    for o in bpy.context.scene.objects:
        if o.type != "MESH":
            continue
        ev = o.evaluated_get(dg)
        me = ev.to_mesh()
        me.calc_loop_triangles()
        n = len(me.loop_triangles)
        tris += n
        for v in me.vertices:
            w = ev.matrix_world @ v.co
            lo = Vector(map(min, lo, w))
            hi = Vector(map(max, hi, w))
        names = [s.material.name for s in o.material_slots if s.material]
        mats.update(names)
        meshes.append(dict(name=o.name, tris=n, materials=names))
        if FACE.search(o.name) or any(FACE.search(m) for m in names):
            faces.append(dict(object=o.name, materials=[m for m in names if FACE.search(m)] or names, tris=n))
        for s in o.material_slots:
            if s.material and s.material.use_nodes:
                for nd in s.material.node_tree.nodes:
                    if nd.type == "TEX_IMAGE" and nd.image:
                        images.add(f"{s.material.name}:{nd.image.name}:{nd.image.size[0]}x{nd.image.size[1]}")
        ev.to_mesh_clear()
    size = [round(hi[i] - lo[i], 4) for i in range(3)]
    return dict(file=path, tris=tris, size_xyz=size, min=[round(x, 4) for x in lo], max=[round(x, 4) for x in hi], meshes=len(meshes),
                mesh_list=meshes[:60], materials=sorted(mats), face_parts=faces, images=sorted(images))


rows = []
for f in FILES:
    try:
        rows.append(info(f))
    except Exception as e:  # noqa: BLE001
        rows.append(dict(file=f, error=repr(e)))
OUT.write_text(json.dumps(rows, indent=1) + "\n")
print(f"[inspect] {len(rows)} files -> {OUT}")
