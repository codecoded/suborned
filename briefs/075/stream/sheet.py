"""Scratch (Vela, brief 075-E): a 7-storey streamline block built from the stack modules (PLAN frame: Base + 6 Mid + Top = H(7) 31.2 m) beside a 4-storey block of
whole-wall pieces (StreamCorner_4 / StreamBay_4 / StreamEnd_4 at sy = H(4) / 19.7 = 20.4 / 19.7), an 8 m lane between their round ends.
Play camera: orthographic, 38 deg down, zoom 14 (28 m of screen height) at yaw 30 and 210, and a wider zoom; street level: perspective at eye height. Blender Eevee.
blender -b -P sheet.py -- <out dir> [shots...]"""
import math, sys, os
from pathlib import Path
import bpy
from mathutils import Euler, Vector
sys.path.insert(0, "/Users/sky/Projects/suborned/tools/decokit")
import kit, mats  # noqa: E402

ARGS = sys.argv[sys.argv.index("--") + 1:]
OUT = Path(ARGS[0]); OUT.mkdir(parents=True, exist_ok=True)
SHOTS = ARGS[1:] or ["play30", "play210", "wide30", "wide210", "street", "street2", "lods"]
LOD = os.environ.get("LOD")  # L1 / L2: every stack module at that level
bpy.ops.wm.read_factory_settings(use_empty=True)
kit.load(); mats.register_robot_enamels()
sc = bpy.context.scene
cache = {}


def place(name, loc, yaw, scale=(1, 1, 1), lod=None):
    key = (name, lod)
    if key not in cache:
        o = kit.build(name) if not lod else kit.build_lod(name, lod)
        for k in [o, *o.children_recursive]:
            k.hide_render = True
        cache[key] = o
    src = cache[key]; root = None
    for k in [src, *src.children_recursive]:
        c = k.copy(); c.hide_render = False; sc.collection.objects.link(c)
        if root is None: root = c
        else: c.parent = root
    root.location = loc; root.rotation_euler = Euler((0, 0, math.radians(yaw))); root.scale = scale
    return root


def R(yaw, v):
    a = math.radians(yaw); return (v[0] * math.cos(a) - v[1] * math.sin(a), v[0] * math.sin(a) + v[1] * math.cos(a))


SY4 = 20.4 / 19.7
BANDS7 = [("Base", 0.0)] + [("Mid", 5.65 + 3.6 * k) for k in range(6)] + [("Top", 5.65 + 3.6 * 6)]


def put(fam, xy, yaw, sx=1.0, stack=False):
    """A whole-wall piece (fam = id, sy 1.0355) or a column of stack modules (fam = family name)."""
    if not stack:
        place(fam, (xy[0], xy[1], 0.0), yaw, (sx, SY4, 1)); return
    for tag, z in BANDS7:
        place(f"{fam}_{tag}", (xy[0], xy[1], z), yaw, (sx, 1, 1), lod=LOD)


def face_bays(a, b, yaw, fam, stack):
    L = math.dist(a, b); k = max(1, round(L / 6.0)); w = L / k
    for i in range(k):
        f = (i + 0.5) / k
        put(fam, (a[0] + (b[0] - a[0]) * f, a[1] + (b[1] - a[1]) * f), yaw, w / 6.0, stack)


def corner(V, yaw, fam, stack):
    d = R(yaw, (4.0, 0.0)); put(fam, (V[0] + d[0], V[1] + d[1]), yaw, 1.0, stack)


def block(X0, X1, Y0, Y1, end_west, stack):
    """Corners R 6 on three vertices; the fourth (front west or front east) is a run end R 3 whose 14 m side wraps the side face."""
    bay, cor, end = ("StreamBayStack", "StreamCornerStack", "StreamEndStack") if stack else ("StreamBay_4", "StreamCorner_4", "StreamEnd_4")
    if end_west:
        put(end, (X0 + 3.0, Y0), 0, 1.0, stack)
        face_bays((X0 + 6.0, Y0), (X1 - 8.0, Y0), 0, bay, stack)
        corner((X1, Y0), 90, cor, stack)
        face_bays((X1, Y0 + 8.0), (X1, Y1 - 8.0), 90, bay, stack)
    else:
        corner((X0, Y0), 0, cor, stack)
        face_bays((X0 + 8.0, Y0), (X1 - 6.0, Y0), 0, bay, stack)
        put(end, (X1 - 3.0, Y0), 0, -1.0, stack)  # mirror X: the round on +X
        face_bays((X0, Y1 - 8.0), (X0, Y0 + 8.0), 270, bay, stack)
    corner((X1, Y1), 180, cor, stack)
    face_bays((X1 - 8.0, Y1), (X0 + 8.0, Y1), 180, bay, stack)
    corner((X0, Y1), 270, cor, stack)
    if end_west:
        pass  # the west face is the end's 14 m side (y 0-14) and the NW corner's leg (14-22)
    else:
        pass  # the east face likewise


def ground():
    me = bpy.data.meshes.new("g"); r = 150
    me.from_pydata([(-r, -r, -0.01), (r, -r, -0.01), (r, r, -0.01), (-r, r, -0.01)], [], [(0, 1, 2, 3)])
    o = bpy.data.objects.new("g", me); sc.collection.objects.link(o); me.materials.append(mats.get("DK_Limestone"))
    me = bpy.data.meshes.new("road"); y0, y1 = -12.0, -2.5
    me.from_pydata([(-r, y0, 0.0), (r, y0, 0.0), (r, y1, 0.0), (-r, y1, 0.0)], [], [(0, 1, 2, 3)])
    o = bpy.data.objects.new("road", me); sc.collection.objects.link(o); me.materials.append(mats.get("DK_Stone_Dark"))


w = bpy.data.worlds.new("w"); sc.world = w
w.use_nodes = True; w.node_tree.nodes["Background"].inputs[0].default_value = (0.78, 0.84, 0.92, 1); w.node_tree.nodes["Background"].inputs[1].default_value = 0.85
sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN")); sun.data.energy = 3.4; sun.data.angle = math.radians(2); sun.data.color = (1.0, 0.95, 0.86); sc.collection.objects.link(sun)
cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam")); sc.collection.objects.link(cam); sc.camera = cam
cam.data.clip_end = 3000
sc.render.engine = "BLENDER_EEVEE"; sc.render.resolution_x, sc.render.resolution_y = 1920, 1080
sc.view_settings.view_transform = "Standard"
try:
    sc.eevee.use_shadows = True
except Exception:
    pass


def shoot(tag, tgt, yaw, scale):
    cam.data.type = "ORTHO"
    d = Vector((math.sin(math.radians(yaw)) * math.cos(math.radians(38)), -math.cos(math.radians(yaw)) * math.cos(math.radians(38)), math.sin(math.radians(38))))
    cam.location = Vector(tgt) + d * 400
    cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
    cam.data.ortho_scale = scale
    sun.rotation_euler = Euler((math.radians(48), 0, math.radians(yaw - 140)))
    sc.render.filepath = str(OUT / f"{tag}.png"); bpy.ops.render.render(write_still=True)


def street(tag, eye, tgt, lens=24):
    cam.data.type = "PERSP"; cam.data.lens = lens
    cam.location = Vector(eye); cam.rotation_euler = (Vector(tgt) - Vector(eye)).to_track_quat("-Z", "Y").to_euler()
    sun.rotation_euler = Euler((math.radians(48), 0, math.radians(30 - 140)))
    sc.render.filepath = str(OUT / f"{tag}.png"); bpy.ops.render.render(write_still=True)


ZOOM14 = 28.0 * 16 / 9
if [s for s in SHOTS if s != "lods"]:
    block(-50.0, -18.0, 0.0, 22.0, end_west=False, stack=False)   # 4 storeys, whole-wall, its run end facing the lane
    block(-10.0, 22.0, 0.0, 22.0, end_west=True, stack=True)      # 7 storeys, stack modules, its run end facing the lane
    ground()
for s in SHOTS:
    if s == "play30": shoot(s, (-14, 4, 8), 30, ZOOM14)
    elif s == "play210": shoot(s, (-14, 18, 8), 210, ZOOM14)
    elif s == "wide30": shoot(s, (-14, 11, 12), 30, 90)
    elif s == "wide210": shoot(s, (-14, 11, 12), 210, 90)
    elif s == "street": street(s, (-22.0, -30.0, 1.7), (-12.0, 6.0, 10.0), lens=28)
    elif s == "street2": street(s, (34.0, -22.0, 1.7), (14.0, 6.0, 12.0), lens=28)
    elif s == "lods":
        for o in list(sc.objects):
            if o.type == "MESH" and not o.hide_render and o.name not in ("cam", "sun"):
                bpy.data.objects.remove(o)
        ground()
        for i, fam in enumerate(("StreamCornerStack", "StreamBayStack", "StreamEndStack")):
            for j, lod in enumerate((None, "L1", "L2")):
                x = -40 + i * 30 + j * 9.5
                z = 0.0
                for tag, h in (("Base", 5.65), ("Mid", 3.6), ("Mid", 3.6), ("Top", 3.95)):
                    place(f"{fam}_{tag}", (x, 0, z), 0, lod=lod); z += h
        cam.data.type = "ORTHO"
        shoot(s, (-1, 0, 9), 20, 110)
