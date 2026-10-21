"""Brief 028: where BoardingPavilion_TB_Office could stand at the official bay (Vela's proposal for Waymark and Splice; the block is not changed).

blender -b -P tools/decokit/review028_office.py -- <out_dir>
Block frame (x south, y east; plan x = -130 + y, plan y = -230 - x), drawn here with Blender x = block x, y = block y. Stand-ins: the tower face at x 75.5 (a wall), pavement to x 77, the official bay
x 77..80, y -38.2..-23.8 as a pale slab, road beyond x 80, a getaway-free sedan in the bay, pegs at the plan's points (Official entrance 76.25,-30 red; Boarding pavilion 78.3,-31 gold; Official drop-off
78.2,-31 white; Meet driver 78.1,-33.7 blue) and the bay's four kerb corners.
  A  today: RecordsEntrance_Official (3 x 6 m marquee at 4.05) and BoardingPavilion_Office stand-in not drawn (the marquee is the block's office-boarding-pavilion)
  B  proposal: RecordsEntrance_Official_Open (no marquee) + BoardingPavilion_TB_Office centred (77.8, -31), long axis along y, open front toward +x (the road), back eave 0.4 m off the tower face (x 75.9: the pilasters stand 0.3 m proud)
  C  the clash: the TB shelter at the testbed-literal point (78.3, -31) with today's marquee kept
Each from yaw 30 and 210 (120: the face seen from the south-east) at play zoom (orthographic, 38 degrees)."""
import math
import sys
from pathlib import Path

import bpy
from mathutils import Euler, Vector

sys.path.insert(0, str(Path(__file__).parent))
import kit  # noqa: E402
import mats  # noqa: E402

OUT = Path(sys.argv[sys.argv.index("--") + 1])
OUT.mkdir(parents=True, exist_ok=True)


def link(o):
    for k in [o, *o.children_recursive]:
        if k.name not in bpy.context.scene.collection.objects:
            bpy.context.scene.collection.objects.link(k)
    return o


def quad(x0, y0, x1, y1, z, mat):
    me = bpy.data.meshes.new("q")
    me.from_pydata([(x0, y0, z), (x1, y0, z), (x1, y1, z), (x0, y1, z)], [], [(0, 1, 2, 3)])
    uv = me.uv_layers.new()
    for i, (x, y) in enumerate([(x0, y0), (x1, y0), (x1, y1), (x0, y1)]):
        uv.data[i].uv = (x / 4, y / 4)
    o = bpy.data.objects.new("q", me)
    bpy.context.scene.collection.objects.link(o)
    me.materials.append(mat)


def box(x0, y0, z0, x1, y1, z1, mat):
    v = [(x, y, z) for x in (x0, x1) for y in (y0, y1) for z in (z0, z1)]
    f = [(0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)]
    me = bpy.data.meshes.new("b")
    me.from_pydata(v, [], f)
    me.update()
    o = bpy.data.objects.new("b", me)
    bpy.context.scene.collection.objects.link(o)
    me.materials.append(mat)
    return o


def flat(name, rgb):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (*rgb, 1)
    m.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.6
    return m


def scene(variant, yaw):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    kit.load()
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE"
    sc.render.resolution_x, sc.render.resolution_y = 1280, 900
    sc.view_settings.view_transform = "Standard"
    sc.view_settings.exposure = -0.3
    w = bpy.data.worlds.new("w")
    w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (.62, .78, .92, 1)
    sc.world = w
    sun = bpy.data.lights.new("sun", "SUN")
    sun.energy = 3.2
    so = bpy.data.objects.new("sun", sun)
    so.rotation_euler = Euler((math.radians(52), 0, math.radians(-40)))
    sc.collection.objects.link(so)
    quad(60, -50, 77, -14, 0.0, mats.get("DK_Ground_Pavement"))
    quad(77, -38.2, 80, -23.8, 0.01, flat("bay", (.78, .8, .84)))
    quad(80, -50, 100, -14, 0.0, mats.get("DK_Ground_Asphalt"))
    quad(77, -50, 80, -38.2, 0.0, mats.get("DK_Ground_Pavement"))
    quad(77, -23.8, 80, -14, 0.0, mats.get("DK_Ground_Pavement"))
    box(60, -40.5, 0, 75.5, -19.5, 14, flat("tower", (.93, .9, .82)))
    for (x, y, rgb) in ((76.25, -30, (.9, .15, .1)), (78.3, -31, (.95, .75, .2)), (78.2, -31.6, (1, 1, 1)), (78.1, -33.7, (.1, .35, .9))):
        box(x - .15, y - .15, 0, x + .15, y + .15, 1.8, flat("p", rgb))
    for (x, y) in ((77, -38.2), (80, -38.2), (77, -23.8), (80, -23.8)):
        box(x - .08, y - .08, 0, x + .08, y + .08, .5, flat("k", (.1, .1, .1)))
    ent = "RecordsEntrance_Official_Open" if variant == "B" else "RecordsEntrance_Official"
    o = link(kit.build(ent))
    o.location, o.rotation_euler = (75.5, -30, 0), Euler((0, 0, math.radians(90)))
    car = link(kit.build("Sedan_Official"))
    car.location, car.rotation_euler = (78.5, -29.0, 0), Euler((0, 0, math.radians(90)))
    if variant in ("B", "C"):
        p = link(kit.build("BoardingPavilion_TB_Office"))
        p.location = (77.8 if variant == "B" else 78.3, -31, 0)
    cd = bpy.data.cameras.new("c")
    co = bpy.data.objects.new("c", cd)
    sc.collection.objects.link(co)
    sc.camera = co
    cd.type, cd.ortho_scale, cd.clip_end = "ORTHO", 22, 400
    pit, d, t = math.radians(38), 80, Vector((77, -31, 1.5))
    co.location = t + Vector((math.sin(math.radians(yaw)) * math.cos(pit), -math.cos(math.radians(yaw)) * math.cos(pit), math.sin(pit))) * d
    co.rotation_euler = (t - co.location).to_track_quat("-Z", "Y").to_euler()
    sc.render.filepath = str(OUT / f"office-{variant}-{yaw}.png")
    bpy.ops.render.render(write_still=True)


for v in "ABC":
    for y in (30, 120):
        scene(v, y)
