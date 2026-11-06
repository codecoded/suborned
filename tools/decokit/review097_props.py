"""Brief 097 (Vela): review renders of the props (civic_props097.py) from their exported FBX, EEVEE, material colour x the baked vertex AO, warm sun.

/Applications/Blender.app/Contents/MacOS/Blender -b -P tools/decokit/review097_props.py -- <out_dir> <fbx> [<fbx> ...]
  per piece: <name>_34.png (three-quarter, perspective, 1000 px) and <name>_play.png (orthographic 38 degrees down, yaw 30, at play zoom: a 1.75 m figure is
  about 80 px, as a 1080p frame at zoom 10; a 480 x 360 crop, a figure-sized grey post beside the piece for scale)."""
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

A = sys.argv[sys.argv.index("--") + 1:]
OUT = Path(A[0])
OUT.mkdir(parents=True, exist_ok=True)
PX_PER_M = 80 / 1.75


def setup():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE"
    sc.view_settings.view_transform = "AgX"
    sc.world = bpy.data.worlds.new("w")
    sc.world.use_nodes = True
    sc.world.node_tree.nodes["Background"].inputs[0].default_value = (0.62, 0.7, 0.8, 1)
    sc.world.node_tree.nodes["Background"].inputs[1].default_value = 0.9
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy = 3.2
    sun.data.color = (1.0, 0.93, 0.84)
    sun.data.angle = math.radians(3)
    sun.rotation_euler = (math.radians(50), 0, math.radians(-35))
    sc.collection.objects.link(sun)
    bpy.ops.mesh.primitive_plane_add(size=60)
    g = bpy.context.object
    m = bpy.data.materials.new("pave")
    m.use_nodes = True
    m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.75, 0.7, 0.62, 1)
    g.data.materials.append(m)
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
            base = tuple(m.diffuse_color)
            if b is None:
                continue
            nm = m.name
            b.inputs["Base Color"].default_value = base
            b.inputs["Roughness"].default_value = 0.25 if "Chrome" in nm else 0.55
            b.inputs["Metallic"].default_value = 1.0 if ("Chrome" in nm or "Brass" in nm) else 0.0
            if "Lamp" in nm:
                b.inputs["Emission Color"].default_value = base
                b.inputs["Emission Strength"].default_value = 2.0
            if has and "Lamp" not in nm:   # occlusion = vertex colour R into the base colour (the shader also takes it into indirect light)
                at = nt.nodes.new("ShaderNodeVertexColor")
                at.layer_name = o.data.color_attributes[0].name
                mul = nt.nodes.new("ShaderNodeMix")
                mul.data_type = "RGBA"
                mul.blend_type = "MULTIPLY"
                mul.inputs[0].default_value = 1.0
                mul.inputs[6].default_value = base
                nt.links.new(at.outputs["Color"], mul.inputs[7])
                nt.links.new(mul.outputs[2], b.inputs["Base Color"])


def cam(sc, target, yaw, pitch, dist, ortho=None, res=(1000, 1000)):
    c = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    sc.collection.objects.link(c)
    y, p = math.radians(yaw), math.radians(pitch)
    d = Vector((math.sin(y) * math.cos(p), -math.cos(y) * math.cos(p), math.sin(p)))
    c.location = target + d * dist
    c.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
    if ortho:
        c.data.type = "ORTHO"
        c.data.ortho_scale = ortho
    else:
        c.data.lens = 50
    c.data.clip_end = 500
    sc.camera = c
    sc.render.resolution_x, sc.render.resolution_y = res
    return c


for f in A[1:]:
    sc = setup()
    bpy.ops.import_scene.fbx(filepath=f, colors_type="SRGB")
    objs = [o for o in bpy.data.objects if o.type == "MESH" and o.name != "Plane"]
    dress(objs)
    pts = [o.matrix_world @ Vector(c) for o in objs for c in o.bound_box]
    lo = Vector([min(p[i] for p in pts) for i in range(3)])
    hi = Vector([max(p[i] for p in pts) for i in range(3)])
    mid, size = (lo + hi) / 2, (hi - lo).length
    name = Path(f).stem.replace("DK_", "")
    cam(sc, mid, 35, 14, size * 2.3)
    sc.render.filepath = str(OUT / f"{name}_34.png")
    bpy.ops.render.render(write_still=True)
    bpy.ops.mesh.primitive_cylinder_add(radius=0.22, depth=1.75, location=(lo.x - 0.6, lo.y - 0.2, 0.875))   # a figure for scale
    w = 480
    h = max(360, int((hi.z * math.cos(math.radians(38)) + 2.5) * PX_PER_M))   # tall pieces (the mast) keep their whole height in the crop
    sc.render.resolution_x, sc.render.resolution_y = w, h
    cam(sc, Vector((mid.x - 0.3, mid.y, hi.z * 0.45)), 30, 38, 60, ortho=max(w, h) / PX_PER_M, res=(w, h))
    sc.render.filepath = str(OUT / f"{name}_play.png")
    bpy.ops.render.render(write_still=True)
