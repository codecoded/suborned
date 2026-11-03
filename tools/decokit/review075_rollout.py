"""Brief 075 roll-out step 1 (Vela): contact renders for the sky track, the crane-less DiscTower, the presentation vehicles and the car paints.

/Applications/Blender.app/Contents/MacOS/Blender -b -P tools/decokit/review075_rollout.py -- <out_dir> [px]
Reads out/decokit and, read only, the old DecoKit's FBX. Helpers copied from review075_vista.py. Eevee, a warm sun and a teal sky: shape and colour, not the game's lighting.
  track_play / track_street / track_close  the old single line (legacy Foot + Shaft + Head + one beam, left) and ours (SkyRail_Pylon_Tall + Shaft + SkyRail_Pylon_HeadTwin
                                           + two SkyRail_Beam_4m lines at x +-3, beam top 24, right), pylons every 30 m, a SkyRail car pair on the east beam
  tower        old DK_DiscTower (crane) | DiscTower@avenue-gate-w (no crane), front
  vehicles     HoverScooter, AirCar_Coral / Teal / Saucer, SkyRail_Car_Head + Mid
  paints       Sedan_Official in the 11 carpaint keys (body / roof swapped as carpaint.json says), the official and the getaway for reference
"""
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

REPO = Path(__file__).resolve().parents[2]
A = sys.argv[sys.argv.index("--") + 1:]
OUT = Path(A[0])
PX = int(A[1]) if len(A) > 1 and A[1].isdigit() else 640
OUT.mkdir(parents=True, exist_ok=True)
GEN = REPO / "out/decokit"
LEGACY = Path("~/Projects/the-suborned/game/Assets/TheSuborned/Art/DecoKit").expanduser()
table = {m["name"]: m for m in json.loads((GEN / "decokit_materials.json").read_text())["materials"]}
LIVERY_GOLD = ([0.0, 0.0, 0.0], 1.0, 0.45)


def lin(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


LIVERY_GOLD = ([lin(int("F2C14E"[i:i + 2], 16) / 255) for i in (0, 2, 4)], 1.0, 0.45)
_means, _mats = {}, {}
LIVERY = {"on": False}


def tex_mean(name):
    for r in (GEN / "Textures", LEGACY / "Textures"):
        p = r / name
        if p.exists():
            img = bpy.data.images.load(str(p))
            px = list(img.pixels[:])
            m = [sum(px[i::4][::97]) / len(px[i::4][::97]) for i in range(3)]
            bpy.data.images.remove(img)
            return [lin(x) for x in m]
    return [1.0, 1.0, 1.0]


def look(name):
    if LIVERY["on"] and name == "DK_Crown_Gold":
        c, met, sm = LIVERY_GOLD
        return c, met, sm, 0.0
    row = table.get(name)
    if not row:
        return [0.6, 0.6, 0.6], 0.0, 0.5, 0.0
    c = [lin(x) for x in row["color"]]
    if row.get("texture"):
        m = _means.setdefault(row["texture"], tex_mean(row["texture"]))
        c = [a * b for a, b in zip(c, m)]
    emit = 1.0 if (row.get("emission") or 0) > 0 or row.get("kind") == "emit" else 0.0
    return c, row.get("metallic", 0.0), row.get("smoothness", 0.5), emit


def clear(aspect=0.75):
    _mats.clear()
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items.keys() else "BLENDER_EEVEE"
    sc.render.resolution_x, sc.render.resolution_y = PX, int(PX * aspect)
    sc.view_settings.view_transform = "Standard"
    w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.42, 0.66, 0.70, 1)
    w.node_tree.nodes["Background"].inputs[1].default_value = 0.9
    sun = bpy.data.lights.new("sun", "SUN"); sun.energy = 3.2; sun.color = (1.0, 0.95, 0.86)
    o = bpy.data.objects.new("sun", sun); sc.collection.objects.link(o); o.rotation_euler = (math.radians(50), 0, math.radians(-35))


def colour(objs):
    for o in objs:
        for slot in o.material_slots:
            if not slot.material:
                continue
            base = slot.material.name.split(".")[0]
            if base not in _mats:
                c, met, sm, emit = look(base)
                nm = bpy.data.materials.new(f"r_{base}"); nm.use_nodes = True
                b = nm.node_tree.nodes["Principled BSDF"]
                b.inputs["Base Color"].default_value = (*c, 1); b.inputs["Metallic"].default_value = met; b.inputs["Roughness"].default_value = 1 - sm
                if emit:
                    b.inputs["Emission Color"].default_value = (*c, 1); b.inputs["Emission Strength"].default_value = 1.5
                _mats[base] = nm
            slot.material = _mats[base]


def imp(path, at=(0, 0, 0), rz=0.0, scale=1.0, rx=0.0):
    before = set(bpy.context.scene.objects)
    bpy.ops.import_scene.fbx(filepath=str(path))
    new = [o for o in bpy.context.scene.objects if o not in before]
    for r in [o for o in new if o.parent is None]:
        r.location = Vector(r.location) * scale + Vector(at)
        r.scale = Vector(r.scale) * scale
        r.rotation_euler.z += rz
        r.rotation_euler.x += rx
    bpy.context.view_layer.update()
    objs = [o for o in new if o.type == "MESH"]
    colour(objs)
    return objs


def stretch(path, a, b):
    """A 1 m wire piece (along its kit X) from a to b: scale X by the length, turn onto the direction (yaw, then pitch)."""
    a, b = Vector(a), Vector(b)
    d = b - a
    objs = imp(path, at=(a + b) / 2)
    for r in {o for o in objs if o.parent is None} | {o.parent for o in objs if o.parent}:
        r.scale.x *= d.length
        r.rotation_mode = "XYZ"
        r.rotation_euler = (r.rotation_euler.x, -math.atan2(d.z, d.xy.length), math.atan2(d.y, d.x))
    bpy.context.view_layer.update()
    return objs


def ground(size=2000.0, rgb=(0.86, 0.82, 0.74), z=0.0, at=(0, 0)):
    me = bpy.data.meshes.new("g"); o = bpy.data.objects.new("ground", me); bpy.context.scene.collection.objects.link(o)
    h = size / 2
    me.from_pydata([(at[0] - h, at[1] - h, z), (at[0] + h, at[1] - h, z), (at[0] + h, at[1] + h, z), (at[0] - h, at[1] + h, z)], [], [(0, 1, 2, 3)])
    m = bpy.data.materials.new("gm"); m.use_nodes = True; m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (*[lin(x) for x in rgb], 1)
    me.materials.append(m)
    return o


def block(lo, hi, rgb):
    import bmesh
    me = bpy.data.meshes.new("b"); o = bpy.data.objects.new("block", me); bpy.context.scene.collection.objects.link(o)
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1.0); bm.to_mesh(me); bm.free()
    o.scale = [h - l for l, h in zip(lo, hi)]; o.location = [(l + h) / 2 for l, h in zip(lo, hi)]
    m = bpy.data.materials.new("bm"); m.use_nodes = True; m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (*[lin(x) for x in rgb], 1)
    me.materials.append(m)
    return o


def bounds(objs):
    pts = [o.matrix_world @ Vector(c) for o in objs for c in o.bound_box]
    return Vector([min(p[i] for p in pts) for i in range(3)]), Vector([max(p[i] for p in pts) for i in range(3)])


def tris(objs):
    return sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in objs)


def camera(eye, look_at, lens=None, fov=None):
    cam = bpy.data.cameras.new("cam")
    if fov:
        cam.sensor_fit = "VERTICAL"; cam.angle_y = math.radians(fov)
    else:
        cam.lens = lens or 50
    co = bpy.data.objects.new("cam", cam); bpy.context.scene.collection.objects.link(co); bpy.context.scene.camera = co
    co.location = Vector(eye)
    co.rotation_euler = (Vector(look_at) - co.location).to_track_quat("-Z", "Y").to_euler()
    cam.clip_end = 5000
    return co


def render(name):
    bpy.context.scene.render.filepath = str(OUT / f"{name}.png")
    bpy.ops.render.render(write_still=True)



import json as _json
CP = _json.loads((GEN / "Data/carpaint.json").read_text())
report = {}


def hexlin(h):
    h = h.lstrip("#")
    return [lin(int(h[i:i + 2], 16) / 255) for i in (0, 2, 4)]


def paint(objs, swap):
    """Per-object override: slot base name -> sRGB hex (an enamel with the car's look)."""
    for o in objs:
        for slot in o.material_slots:
            for base, hx in swap.items():
                if slot.material and slot.material.name.startswith(f"r_{base}"):
                    nm = bpy.data.materials.new(f"p_{base}_{hx}"); nm.use_nodes = True
                    b = nm.node_tree.nodes["Principled BSDF"]
                    b.inputs["Base Color"].default_value = (*hexlin(hx), 1); b.inputs["Roughness"].default_value = 0.3
                    slot.material = nm


def track(x0, twin):
    """A 70 m line along Y at x0: pylons at y 0, 30, 60; beam top 24."""
    top, under = 24.0, 22.4
    for y in (0.0, 30.0, 60.0):
        if twin:
            imp(GEN / "Models/Sky/DK_SkyRail_Pylon_Tall.fbx" if (GEN / "Models/Sky/DK_SkyRail_Pylon_Tall.fbx").exists() else LEGACY / "Models/Skyport/DK_SkyRail_Pylon_Tall.fbx", (x0, y, 0))
            foot_top = 12.0
            imp(GEN / "Models/Sky/DK_SkyRail_Pylon_HeadTwin.fbx", (x0, y, under))
        else:
            imp(LEGACY / "Models/Sky/DK_SkyRail_Pylon_Foot.fbx", (x0, y, 0)); foot_top = 1.2
            imp(LEGACY / "Models/Sky/DK_SkyRail_Pylon_Head.fbx", (x0, y, under))
        sh = imp(LEGACY / "Models/Sky/DK_SkyRail_Pylon_Shaft.fbx", (x0, y, foot_top))
        from mathutils import Matrix
        k = under - 2.2 - foot_top
        for r in {o for o in sh if o.parent is None}:  # stretch along world Z about the foot top (the FBX root carries its own axis turn)
            p0 = Vector((x0, y, foot_top))
            r.matrix_world = Matrix.Translation(p0) @ Matrix.Diagonal((1, 1, k, 1)) @ Matrix.Translation(-p0) @ r.matrix_world
    for bx in ((-3.0, 3.0) if twin else (0.0,)):
        for k in range(18):
            imp(GEN / "Models/Sky/DK_SkyRail_Beam_4m.fbx", (x0 + bx, -2.0 + 4.0 * k, top))
    if twin:
        imp(LEGACY / "Models/Sky/DK_SkyRail_Car_Head.fbx", (x0 + 3.0, 20.0, top))
        imp(LEGACY / "Models/Sky/DK_SkyRail_Car_Mid.fbx", (x0 + 3.0, 38.4, top))


clear(0.5625); ground(rgb=(0.80, 0.78, 0.72))
block((-40, -20, 0), (-36, 90, 0.15), (0.6, 0.6, 0.6))
track(-25.0, False); track(12.0, True)
camera((60, -55, 52), (-5, 28, 14), lens=32); render("track_play")
camera((48, -20, 42), (12, 25, 18), lens=35); render("track_twin")
camera((40, -14, 3), (5, 30, 20), lens=28); render("track_street")
camera((24, 6, 20.5), (12, 0, 22.4), lens=40); render("track_close")

clear(1.0); ground()
imp(LEGACY / "Models/Landmarks/DK_DiscTower.fbx", (-35, 0, 0)); imp(GEN / "Models/Landmarks/DK_DiscTower@avenue-gate-w.fbx", (35, 0, 0))
camera((0, -330, 90), (0, 0, 95), lens=50); render("tower")
camera((0, -110, 150), (0, 0, 140), lens=50); render("tower_top")

clear(0.5); ground()
imp(LEGACY / "Models/Vehicles/DK_HoverScooter.fbx", (-9, 0, 0))
for i, n in enumerate(("AirCar_Coral", "AirCar_Teal", "AirCar_Saucer")):
    imp(LEGACY / f"Models/Sky/DK_{n}.fbx", (-5 + 6 * i, 0, 1.6))
imp(LEGACY / "Models/Sky/DK_SkyRail_Car_Head.fbx", (16, 4, 1.0), rz=math.radians(90)); imp(LEGACY / "Models/Sky/DK_SkyRail_Car_Mid.fbx", (34.4, 4, 1.0), rz=math.radians(90))
camera((10, -34, 11), (11, 2, 1.2), lens=26); render("vehicles")

clear(0.5625); ground(rgb=(0.72, 0.78, 0.82))
keys = list(CP["keys"].items())
for i, (k, v) in enumerate(keys):
    x, y = (i % 6) * 3.0, (i // 6) * 6.5
    o = imp(GEN / "Models/Vehicles/DK_Sedan_Official.fbx", (x, y, 0))
    paint(o, {"DK_CarEnamel_Teal": v["bodyHex"], "DK_CarEnamel_Ivory": v["roofHex"]})
imp(GEN / "Models/Vehicles/DK_Sedan_Official.fbx", (15.0, 6.5, 0)); imp(GEN / "Models/Vehicles/DK_Sedan_Getaway.fbx", (18.0, 6.5, 0))
camera((8, -14, 9), (8, 3.5, 0.6), lens=35); render("paints")
(OUT / "keys.json").write_text(_json.dumps([k for k, _ in keys] + ["(Official)", "(Getaway)"]))
