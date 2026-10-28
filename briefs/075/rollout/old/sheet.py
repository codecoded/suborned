"""Scratch (Vela, brief 075 old buildings): each old frontage class as the old placer ran it, with the explicit mirrored caps (no negative scale anywhere).
Per class two runs, true size (scale 1): A = corner (base) + 3 bays + end `_M` (the head end, round on the run's far side); B = end (base) + 3 bays + corner `_M`.
Builder frame (plaza.py / facades.py): facade faces -Y at y 0, bays on 6 m joints along X; a base corner's cell is x -4..4, y 0..8 with the round at -X (its next bay
at x + 7); a base end rounds on -X. blender -b -P sheet.py -- <out dir> [class ...]"""
import math, sys
from pathlib import Path
import bpy
from mathutils import Euler, Vector
sys.path.insert(0, "/Users/sky/Projects/suborned/tools/decokit")
import kit, mats  # noqa: E402

ARGS = sys.argv[sys.argv.index("--") + 1:]
OUT = Path(ARGS[0]); OUT.mkdir(parents=True, exist_ok=True)
# the old DecoKitFrontage.Of table (bay, end, corner), plus the two intended ends and the tower cap
ROWS = {
    "S2": ("StreamBay_2", "StreamEnd_2", "StreamCorner_2"), "S3": ("StreamBay_3", "StreamEnd_3", "StreamCorner_3"),
    "S3W": ("StreamBay_3W", "StreamEnd_3W", "StreamCorner_3"), "S4": ("StreamBay_4", "StreamEnd_4", "StreamCorner_4"),
    "S6": ("StreamBay_6", "StreamEnd_6", "StreamCorner_6"), "S9": ("StreamBay_9", "StreamEnd_9", "StreamCorner_9"),
    "S9W": ("StreamBay_9W", "StreamEnd_9W", "StreamCorner_9"), "D10": ("DecoBay_10", "DecoEnd_10", "DecoCorner_10"),
    "P7": ("PortholeBay_7", "StreamEnd_6", "StreamCorner_6"), "B8": ("BalconyBay_8", "StreamEnd_9W", "StreamCorner_9"),
    "A3": ("ArcadeBay_3", "HallCornerWing", "HallCornerWing"), "H": ("HallBay", "HallCornerWing", "HallCornerWing"),
    "P3": ("PortholeBay_3", "StreamEnd_3W", "StreamCorner_3"), "B3": ("BalconyBay_3", "StreamEnd_3", "StreamCorner_3"),
    "P4": ("PortholeBay_4", "StreamEnd_4", "StreamCorner_4"), "B4": ("BalconyBay_4", "StreamEnd_4", "StreamCorner_4"),
    "P7-new": ("PortholeBay_7", "PortholeEnd_7", "StreamCorner_6"), "B8-new": ("BalconyBay_8", "BalconyEnd_8", "StreamCorner_9"),
    "S6-tower": ("StreamBay_6", "StreamEnd_6", "CornerTower_12"),
}
PICK = ARGS[1:] or list(ROWS)
bpy.ops.wm.read_factory_settings(use_empty=True)
kit.load()
sc = bpy.context.scene
cache = {}


def place(name, x, y=0.0, yaw=0.0):
    if name not in cache:
        o = kit.build(name)
        for k in [o, *o.children_recursive]:
            k.hide_render = True
        cache[name] = o
    src = cache[name]; root = None; made = []
    for k in [src, *src.children_recursive]:
        c = k.copy(); c.hide_render = False; sc.collection.objects.link(c); made.append(c)
        if root is None: root = c
        else: c.parent = root
    root.location = (x, y, 0.0); root.rotation_euler = Euler((0, 0, math.radians(yaw)))
    return made


def half(corner):
    return 5.0 if corner.startswith("CornerTower") else 4.0


def hall_end(end):
    return end == "HallCornerWing"  # the old: the wing is the hall's end (placed as a corner cell)


def run_a(x0, bay, end, corner):
    """corner (base) at the tail, 3 bays, end _M at the head."""
    out = []; h = half(corner)
    out += place(corner, x0 + h)
    x = x0 + 2 * h + 3.0
    for _ in range(3):
        out += place(bay, x); x += 6.0
    out += place(end + "_M", x + (1.0 if hall_end(end) else 0.0))
    return out


def run_b(x0, bay, end, corner):
    """end (base) at the tail, 3 bays, corner _M at the head."""
    out = []; h = half(corner)
    x = x0 + 3.0
    out += place(end, x - (1.0 if hall_end(end) else 0.0)); x += 6.0
    for _ in range(3):
        out += place(bay, x); x += 6.0
    out += place(corner + "_M", x - 3.0 + h)
    return out


def ground():
    me = bpy.data.meshes.new("g"); r = 200
    me.from_pydata([(-r, -r, -0.01), (r, -r, -0.01), (r, r, -0.01), (-r, r, -0.01)], [], [(0, 1, 2, 3)])
    o = bpy.data.objects.new("g", me); sc.collection.objects.link(o); me.materials.append(mats.get("DK_Limestone"))


w = bpy.data.worlds.new("w"); sc.world = w
w.use_nodes = True; w.node_tree.nodes["Background"].inputs[0].default_value = (0.78, 0.84, 0.92, 1); w.node_tree.nodes["Background"].inputs[1].default_value = 0.85
sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN")); sun.data.energy = 3.4; sun.data.angle = math.radians(2); sun.data.color = (1.0, 0.95, 0.86); sc.collection.objects.link(sun)
sun.rotation_euler = Euler((math.radians(48), 0, math.radians(-110)))
cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam")); sc.collection.objects.link(cam); sc.camera = cam
cam.data.clip_end = 3000; cam.data.type = "ORTHO"
sc.render.engine = "BLENDER_EEVEE"; sc.render.resolution_x, sc.render.resolution_y = 1280, 640
sc.view_settings.view_transform = "Standard"
ground()
for cls in PICK:
    bay, end, corner = ROWS[cls]
    objs = run_a(-46.0, bay, end, corner) + run_b(6.0, bay, end, corner)
    yaw, el = 15.0, 24.0
    d = Vector((math.sin(math.radians(yaw)) * math.cos(math.radians(el)), -math.cos(math.radians(yaw)) * math.cos(math.radians(el)), math.sin(math.radians(el))))
    top = max(max(v.co.z for v in o.data.vertices) for o in objs if o.type == "MESH" and len(o.data.vertices))
    cam.location = Vector((0.0, 6.0, top * 0.5)) + d * 400; cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
    cam.data.ortho_scale = max(108.0, top * 2.3)
    sc.render.filepath = str(OUT / f"{cls}.png"); bpy.ops.render.render(write_still=True)
    for o in objs:
        bpy.data.objects.remove(o)
