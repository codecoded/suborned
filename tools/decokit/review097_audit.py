"""Brief 097 (Vela): the Handover zone audit renders. For each kit piece listed (id + FBX path), two views in EEVEE with the review097_props light and dress
(material colour x baked vertex AO where the mesh has it, chrome and brass metallic, lamps emissive): the play angle (orthographic, 38 degrees down, yaw 30)
and a three-quarter perspective, both framed on the piece's bounds, 360 px.

/Applications/Blender.app/Contents/MacOS/Blender -b -P tools/decokit/review097_audit.py -- <out_dir> <list.json>
  list.json: [{"id": ..., "fbx": <absolute path>, "linear": bool}]  ->  <out_dir>/<id>_play.png, <id>_34.png"""
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

A = sys.argv[sys.argv.index("--") + 1:]
OUT = Path(A[0])
OUT.mkdir(parents=True, exist_ok=True)
ITEMS = json.loads(Path(A[1]).read_text())
PX = 360


def stage():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE"
    sc.view_settings.view_transform = "AgX"
    sc.render.resolution_x = sc.render.resolution_y = PX
    sc.world = bpy.data.worlds.new("w")
    sc.world.use_nodes = True
    bg = sc.world.node_tree.nodes["Background"]
    bg.inputs[0].default_value, bg.inputs[1].default_value = (0.62, 0.7, 0.8, 1), 0.9
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy, sun.data.color, sun.data.angle = 3.2, (1.0, 0.93, 0.84), math.radians(3)
    sun.rotation_euler = (math.radians(50), 0, math.radians(-35))
    sc.collection.objects.link(sun)
    return sc


def dress(objs):
    for o in objs:
        has = bool(o.data.color_attributes)
        for slot in o.material_slots:
            m = slot.material
            if not m or m.get("dk097"):
                continue
            m["dk097"] = 1
            m.use_nodes = True
            nt = m.node_tree
            b = nt.nodes.get("Principled BSDF")
            if b is None:
                continue
            nm, base = m.name, tuple(m.diffuse_color)
            b.inputs["Base Color"].default_value = base
            b.inputs["Roughness"].default_value = 0.25 if "Chrome" in nm else 0.6
            b.inputs["Metallic"].default_value = 1.0 if ("Chrome" in nm or "Brass" in nm or "Gold" in nm) else 0.0
            if "Lamp" in nm or "Light" in nm:
                b.inputs["Emission Color"].default_value = base
                b.inputs["Emission Strength"].default_value = 1.5
            if has and not any(k in nm for k in ("Lamp", "Leaf", "Petal")):
                at = nt.nodes.new("ShaderNodeVertexColor")
                at.layer_name = o.data.color_attributes[0].name
                mul = nt.nodes.new("ShaderNodeMix")
                mul.data_type, mul.blend_type = "RGBA", "MULTIPLY"
                mul.inputs[0].default_value = 1.0
                mul.inputs[6].default_value = base
                nt.links.new(at.outputs["Color"], mul.inputs[7])
                nt.links.new(mul.outputs[2], b.inputs["Base Color"])
            elif any(k in nm for k in ("Leaf", "Petal")) and has:   # testbed planting: vertex colour is the albedo
                at = nt.nodes.new("ShaderNodeVertexColor")
                at.layer_name = o.data.color_attributes[0].name
                nt.links.new(at.outputs["Color"], b.inputs["Base Color"])


def view(sc, mid, size, yaw, pitch, ortho, path):
    c = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    sc.collection.objects.link(c)
    y, p = math.radians(yaw), math.radians(pitch)
    d = Vector((math.sin(y) * math.cos(p), -math.cos(y) * math.cos(p), math.sin(p)))
    c.location = mid + d * size * 3
    c.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
    c.data.clip_end = size * 20
    if ortho:
        c.data.type, c.data.ortho_scale = "ORTHO", size * 1.05
    else:
        c.data.lens = 45
        c.location = mid + d * size * 1.9
    sc.camera = c
    sc.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(c)


for it in ITEMS:
    sc = stage()
    try:
        bpy.ops.import_scene.fbx(filepath=it["fbx"], colors_type="LINEAR" if it.get("linear") else "SRGB")
    except Exception as e:  # noqa: BLE001
        print(f"[AUDIT] {it['id']} import failed: {e!r}")
        continue
    objs = [o for o in bpy.data.objects if o.type == "MESH"]
    if not objs:
        continue
    dress(objs)
    pts = [o.matrix_world @ Vector(c) for o in objs for c in o.bound_box]
    lo = Vector([min(q[i] for q in pts) for i in range(3)])
    hi = Vector([max(q[i] for q in pts) for i in range(3)])
    mid, size = (lo + hi) / 2, max((hi - lo).length, 0.3)
    bpy.ops.mesh.primitive_plane_add(size=size * 6, location=(mid.x, mid.y, lo.z))
    g = bpy.context.object
    gm = bpy.data.materials.new("pave")
    gm.use_nodes = True
    gm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.75, 0.7, 0.62, 1)
    g.data.materials.append(gm)
    view(sc, mid, size, 30, 38, True, OUT / f"{it['id']}_play.png")
    view(sc, mid, size, 35, 14, False, OUT / f"{it['id']}_34.png")
    print(f"[AUDIT] {it['id']} done")
