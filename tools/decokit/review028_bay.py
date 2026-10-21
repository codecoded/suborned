"""Brief 028 comparison renders (Vela): the car bay shelter, testbed | now | proposed, same camera, same Blender.

blender -b -P tools/decokit/review028_bay.py -- <out_dir> [square|office] [day|night]
  testbed   the store's source glb as imported (its own glTF material, 4096 px maps), fitted to 12 m and scaled as the testbed placed it (0.70 getaway, 1.25 official); no fittings
  now       BoardingPavilion_Square_v2 + the block's PavilionPier_Square pair / BoardingPavilion_Office + PavilionPier_Office pair
  proposed  BoardingPavilion_TB_Square / _Office (the port, with the testbed's fittings)
A sedan of the bay's colour stands in front of the shelter (the kerb side). Play camera: 38 degrees, yaw 30, orthographic. Not the game's lighting."""
import math
import sys
from pathlib import Path

import bpy
from mathutils import Euler, Vector

sys.path.insert(0, str(Path(__file__).parent))
import kit  # noqa: E402
import mats  # noqa: E402
import pavilion  # noqa: E402

A = sys.argv[sys.argv.index("--") + 1:]
OUT = Path(A[0])
WHICH = A[1] if len(A) > 1 else "square"
MODE = A[2] if len(A) > 2 else "day"
OUT.mkdir(parents=True, exist_ok=True)
NIGHT = MODE == "night"


def link(o):
    for k in [o, *o.children_recursive]:
        if k.name not in bpy.context.scene.collection.objects:
            bpy.context.scene.collection.objects.link(k)
    return o


def place(name, loc, yaw=0.0):
    o = link(kit.build(name))
    o.location, o.rotation_euler = loc, Euler((0, 0, math.radians(yaw)))
    return o


def ground():
    me = bpy.data.meshes.new("g")
    me.from_pydata([(-20, -14, 0), (20, -14, 0), (20, 14, 0), (-20, 14, 0)], [], [(0, 1, 2, 3)])
    uv = me.uv_layers.new()
    for i, (x, y) in enumerate([(-20, -14), (20, -14), (20, 14), (-20, 14)]):
        uv.data[i].uv = (x / 4, y / 4)
    o = bpy.data.objects.new("g", me)
    bpy.context.scene.collection.objects.link(o)
    me.materials.append(mats.get("DK_Ground_Pavement"))
    # the carriageway on the front side
    me2 = bpy.data.meshes.new("r")
    me2.from_pydata([(-20, -14, 0.002), (20, -14, 0.002), (20, -4.2, 0.002), (-20, -4.2, 0.002)], [], [(0, 1, 2, 3)])
    uv2 = me2.uv_layers.new()
    for i, (x, y) in enumerate([(-20, -14), (20, -14), (20, -4.2), (-20, -4.2)]):
        uv2.data[i].uv = (x / 16, y / 16)
    o2 = bpy.data.objects.new("r", me2)
    bpy.context.scene.collection.objects.link(o2)
    me2.materials.append(mats.get("DK_Ground_Asphalt"))


def lights():
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE"
    sc.render.resolution_x, sc.render.resolution_y = 1280, 960
    sc.view_settings.view_transform = "Standard"
    sc.view_settings.exposure = 0.0 if NIGHT else -0.3
    w = bpy.data.worlds.new("w")
    w.use_nodes = True
    bg = w.node_tree.nodes["Background"]
    bg.inputs[0].default_value = (.04, .07, .12, 1) if NIGHT else (.62, .78, .92, 1)
    bg.inputs[1].default_value = 0.25 if NIGHT else 1.1
    sc.world = w
    sun = bpy.data.lights.new("sun", "SUN")
    sun.energy, sun.color = (0.12, (0.65, 0.78, 1.0)) if NIGHT else (3.2, (1.0, .93, .80))
    sun.angle = math.radians(2)
    so = bpy.data.objects.new("sun", sun)
    so.rotation_euler = Euler((math.radians(52), 0, math.radians(-40)))
    sc.collection.objects.link(so)
    if NIGHT:  # a warm spot under each fitting (the testbed's night: 32 each)
        for x in pavilion_fix():
            sp = bpy.data.lights.new("s", "SPOT")
            sp.energy, sp.color, sp.spot_size, sp.spot_blend = 260 * SC * SC, (1.0, .87, .64), math.radians(60), 1.0
            so2 = bpy.data.objects.new("s", sp)
            so2.location = Vector(x)
            so2.rotation_euler = (0, 0, 0)
            sc.collection.objects.link(so2)


def pavilion_fix():
    """Fitting positions in the scene (the pavilion's pivot at the origin, rotation applied)."""
    out = []
    for x, h, z in pavilion.FIX:
        px, py = x * SC, -z * SC
        if WHICH == "office":
            px, py = -py, px
        out.append((px, py, h * SC - 0.05))
    return out


def camera(target, half, yaw=30):
    cd = bpy.data.cameras.new("c")
    co = bpy.data.objects.new("c", cd)
    bpy.context.scene.collection.objects.link(co)
    bpy.context.scene.camera = co
    cd.type, cd.ortho_scale, cd.clip_end = "ORTHO", 2 * half * 4 / 3, 400
    p, d, t = math.radians(38), 80, Vector(target)
    co.location = t + Vector((math.sin(math.radians(yaw)) * math.cos(p), -math.cos(math.radians(yaw)) * math.cos(p), math.sin(p))) * d
    co.rotation_euler = (t - co.location).to_track_quat("-Z", "Y").to_euler()


def testbed_body():
    bpy.ops.import_scene.gltf(filepath=str(pavilion.SOURCE))
    body = next(o for o in bpy.context.scene.objects if o.type == "MESH" and o.name == "Mesh_0")
    me = body.data
    bpy.context.view_layer.update()
    me.transform(body.matrix_world)
    body.matrix_world.identity()
    xs, ys, zs = [v.co.x for v in me.vertices], [v.co.y for v in me.vertices], [v.co.z for v in me.vertices]
    k = 12.0 / (max(xs) - min(xs)) * SC
    for v in me.vertices:
        v.co = ((v.co.x - (max(xs) + min(xs)) / 2) * k, (v.co.y - (max(ys) + min(ys)) / 2) * k, (v.co.z - min(zs)) * k)
    if WHICH == "office":
        body.rotation_mode = "XYZ"
        body.rotation_euler = (0, 0, math.pi / 2)


def main(variant):
    global SC
    SC = 0.70 if WHICH == "square" else 1.25
    bpy.ops.wm.read_factory_settings(use_empty=True)
    ground()
    lights()
    pavilion.SOURCE  # noqa: B018
    if variant == "testbed":
        testbed_body()
    elif variant == "now":
        if WHICH == "square":
            place("BoardingPavilion_Square_v2", (0, 0, 0))
            for x in (-3.22, 3.22):
                place("PavilionPier_Square", (x, -0.5, 0))
        else:
            place("BoardingPavilion_Office", (0, 0, 0))
            for y in (-5.5, 5.5):
                place("PavilionPier_Office", (0, y, 0))
    else:
        place("BoardingPavilion_TB_Square" if WHICH == "square" else "BoardingPavilion_TB_Office", (0, 0, 0))
    car = "Sedan_Getaway" if WHICH == "square" else "Sedan_Official"
    if WHICH == "square":
        place(car, (0, -4.6, 0), 0)
    else:
        place(car, (-4.6, 0, 0), 90)
    camera((0, 0, 1.5), 6.5 if WHICH == "square" else 9)
    bpy.context.scene.render.filepath = str(OUT / f"{WHICH}-{variant}-{MODE}.png")
    bpy.ops.render.render(write_still=True)


import mats as _m  # noqa: E402
for v in ("testbed", "now", "proposed"):
    kit.load()
    main(v)
