"""Brief 048 fix 2 (Vela): the flat black window bands on the run ends and corners (StreamEnd / StreamCorner, DecoEnd...), before | after.

blender -b -P tools/decokit/review048_flank.py -- <out_dir> <tag> [Piece,Piece...]
Each piece is built from the generator as it stands, placed on a pale ground with a neighbour block at its side, and shot from the flank side at the play camera
(38 degrees, yaw 120 so the party-wall flank faces the camera) and from 30 degrees. Not the game's lighting."""
import math
import sys
from pathlib import Path

import bpy
from mathutils import Euler, Vector

sys.path.insert(0, str(Path(__file__).parent))
import kit  # noqa: E402
import mats  # noqa: E402
import facades  # noqa: E402,F401
import skyline  # noqa: E402,F401
import facades_tower  # noqa: E402,F401
import landmarks  # noqa: E402,F401
import skyport_parts  # noqa: E402,F401
import lowrise  # noqa: E402,F401
import highrise  # noqa: E402,F401

A = sys.argv[sys.argv.index("--") + 1:]
OUT, TAG = Path(A[0]), A[1]
PIECES = (A[2] if len(A) > 2 else "StreamEnd_9W,StreamCorner_9").split(",")
OUT.mkdir(parents=True, exist_ok=True)


def link(o):
    for k in [o, *o.children_recursive]:
        if k.name not in bpy.context.scene.collection.objects:
            bpy.context.scene.collection.objects.link(k)
    return o


def setup():
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE"
    sc.render.resolution_x, sc.render.resolution_y = 1280, 1280
    sc.view_settings.view_transform = "Standard"
    sc.view_settings.exposure = -0.3
    w = bpy.data.worlds.new("w")
    w.use_nodes = True
    bg = w.node_tree.nodes["Background"]
    bg.inputs[0].default_value, bg.inputs[1].default_value = (.62, .78, .92, 1), 1.1
    sc.world = w
    sun = bpy.data.lights.new("sun", "SUN")
    sun.energy, sun.color, sun.angle = 3.2, (1.0, .93, .80), math.radians(2)
    so = bpy.data.objects.new("sun", sun)
    so.rotation_euler = Euler((math.radians(52), 0, math.radians(-40)))
    sc.collection.objects.link(so)
    me = bpy.data.meshes.new("g")
    me.from_pydata([(-60, -60, -0.02), (60, -60, -0.02), (60, 60, -0.02), (-60, 60, -0.02)], [], [(0, 1, 2, 3)])
    o = bpy.data.objects.new("g", me)
    sc.collection.objects.link(o)
    me.materials.append(mats.get("DK_Ground_Pavement"))


def camera(target, half, yaw):
    cd = bpy.data.cameras.new("c")
    co = bpy.data.objects.new("c", cd)
    bpy.context.scene.collection.objects.link(co)
    bpy.context.scene.camera = co
    cd.type, cd.ortho_scale, cd.clip_end = "ORTHO", 2 * half, 400
    p, d, t = math.radians(24), 80, Vector(target)
    co.location = t + Vector((math.sin(math.radians(yaw)) * math.cos(p), -math.cos(math.radians(yaw)) * math.cos(p), math.sin(p))) * d
    co.rotation_euler = (t - co.location).to_track_quat("-Z", "Y").to_euler()


for name in PIECES:
    for yaw in (-60, 60):
        bpy.ops.wm.read_factory_settings(use_empty=True)
        setup()
        o = link(kit.build(name))
        o.location = (0, 0, 0)
        bpy.context.view_layer.update()
        bb = [o.matrix_world @ Vector(c) for c in o.bound_box]
        for k in o.children_recursive:
            bb += [k.matrix_world @ Vector(c) for c in k.bound_box]
        lo = Vector((min(v.x for v in bb), min(v.y for v in bb), min(v.z for v in bb)))
        hi = Vector((max(v.x for v in bb), max(v.y for v in bb), max(v.z for v in bb)))
        c = (lo + hi) / 2
        camera((c.x, c.y, 18), 24, yaw)
        bpy.context.scene.render.filepath = str(OUT / f"{TAG}-{name}-yaw{yaw}.png")
        bpy.ops.render.render(write_still=True)
        print("rendered", bpy.context.scene.render.filepath)
