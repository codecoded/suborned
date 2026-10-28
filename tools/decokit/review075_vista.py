"""Brief 075 golden view, the vista (Vela): the Crown Avenue arch, the gold Crown Vista tower and the tram's span wires, renders and LOD check.

/Applications/Blender.app/Contents/MacOS/Blender -b -P tools/decokit/review075_vista.py -- <out_dir> [px]
Renders (reads out/decokit and, read only, the old DecoKit's FBX):
  arch_*       old DK_ParabolicArch | the pier's ParabolicArch | ParabolicArch@crown-avenue L0 / L1 / L2, all from the front at the same scale
  tower_*      old DK_DiscTower | DiscTower | DiscTower@crown-vista L0 / L1 / L2 (crown gold), and L0 in the livery gold (#F2C14E, metallic 1, smoothness 0.45)
  vista        the boulevard axis from the golden camera (briefs/075/golden/cameras.json `ours`, fov 58): the arch at plan (0, 160), the gate pair
               DiscTower@avenue-gate-w/e at (-44 / 44, 190), DiscTower@crown-vista x 1.1 at (0, 350), livery gold; a ground plane only, no city
  wires_*      a 20 m tram street (the Boulevard's section: kerbs +-10, building lines +-18) with two tracks at +-1.8: rosettes on both walls at 7.0,
               span wires every 30 m, hangers over each track, contact wires at 5.8 zig-zag +-0.2; one pole pair 0.45 off the kerb face; the old tram on
               a track. wires_play: about the play camera (pitch 38, 30 m off); wires_street: eye height; wires_close: the rosette, hanger and pole head
Writes <out_dir>/lods.json (per file: triangles, size). Eevee, a warm sun and a teal sky: shape and colour, not the game's lighting."""
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

REPO = Path(__file__).resolve().parents[2]
A = sys.argv[sys.argv.index("--") + 1:]
OUT = Path(A[0])
PX = int(A[1]) if len(A) > 1 else 640
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


report = {}
# ---- pieces from the front, one scale per row (so the arch and tower widths compare)
ROWS = {
    "arch": (110.0, [("old", LEGACY / "Models/Landmarks/DK_ParabolicArch.fbx"), ("pier", GEN / "Models/Landmarks/DK_ParabolicArch.fbx"),
                     ("L0", GEN / "Models/Landmarks/DK_ParabolicArch@crown-avenue.fbx"), ("L1", GEN / "Models/Landmarks/DK_ParabolicArch@crown-avenue_L1.fbx"),
                     ("L2", GEN / "Models/Landmarks/DK_ParabolicArch@crown-avenue_L2.fbx")]),
    "tower": (200.0, [("old", LEGACY / "Models/Landmarks/DK_DiscTower.fbx"), ("kit", GEN / "Models/Landmarks/DK_DiscTower@avenue-gate-w.fbx"),
                      ("L0", GEN / "Models/Landmarks/DK_DiscTower@crown-vista.fbx"), ("L1", GEN / "Models/Landmarks/DK_DiscTower@crown-vista_L1.fbx"),
                      ("L2", GEN / "Models/Landmarks/DK_DiscTower@crown-vista_L2.fbx"), ("L0-livery", GEN / "Models/Landmarks/DK_DiscTower@crown-vista.fbx")]),
}
for row, (span, files) in ROWS.items():
    for tag, f in files:
        LIVERY["on"] = tag.endswith("livery")
        clear(aspect=1.0)
        ground(400.0)
        objs = imp(f)
        lo, hi = bounds(objs)
        report[f"{row}/{tag}"] = dict(file=str(f).replace(str(Path.home()), "~"), tris=tris(objs), size=[round(v, 3) for v in (hi - lo)])
        h = span * 0.42
        camera((span * 0.35, -span * 1.9, h * 0.55), (0, 0, h), lens=50)
        render(f"{row}_{tag}")
        print(f"[075vista] {row} {tag} tris={report[f'{row}/{tag}']['tris']} size={report[f'{row}/{tag}']['size']}")

# ---- the vista from the golden camera (plan x, y = Blender x, y; z up)
LIVERY["on"] = True
clear(aspect=9 / 16)
ground(3000.0)
for x in (-9.0, 9.0):  # the avenue's carriageway edges, as a hint of the street
    block((x - 0.15, -400, 0), (x + 0.15, 300, 0.15), (0.75, 0.73, 0.70))
imp(GEN / "Models/Landmarks/DK_ParabolicArch@crown-avenue.fbx", (0, 160, 0))
for x, side in ((-44.0, "w"), (44.0, "e")):
    imp(GEN / f"Models/Landmarks/DK_DiscTower@avenue-gate-{side}.fbx", (x, 190, 0))
imp(GEN / "Models/Landmarks/DK_DiscTower@crown-vista.fbx", (0, 350, 0), scale=1.1)
cam = json.loads((REPO / "briefs/075/golden/cameras.json").read_text())
print("[075vista] cameras.json keys", list(cam.keys()))
ours = cam.get("ours") or cam.get("cameras", {}).get("ours")
eye, at = ours["eye"], ours["look"]
camera((eye[0], eye[2], eye[1]), (at[0], at[2], at[1]), fov=ours.get("fov_deg", 58))  # Unity (x, y up, z) -> Blender (x, z, y)
render("vista")
LIVERY["on"] = False

# ---- the wires on a 20 m tram street
W = "Models/Structures/DK_TramWire_{}.fbx"
clear()
ground(400.0, (0.36, 0.36, 0.38))
block((-60, 10, 0), (60, 18, 0.185), (0.90, 0.86, 0.78))
block((-60, -18, 0), (60, -10, 0.185), (0.90, 0.86, 0.78))
block((-60, 18, 0), (60, 30, 16.0), (0.95, 0.83, 0.70))
block((-60, -30, 0), (60, -18, 16.0), (0.93, 0.88, 0.76))
for yo in (-1.8, 1.8):
    for x in range(-60, 60, 12):
        imp(GEN / "Models/Ground/DK_Tile_TramRail_12m.fbx", (x + 6.0, yo, 0))
imp(GEN / "Models/Vehicles/DK_Tram.fbx", (6.0, 1.8, 0), rz=math.pi / 2)
H_C, H_S = 5.8, 7.0
spans = [-45.0, -15.0, 15.0]
for i, x in enumerate(spans):
    if i < 2:  # rosettes on both walls (the rosette's boss is along its kit front: -Y in Blender, turned to face the street)
        imp(GEN / W.format("Rosette"), (x, -18.0, H_S), rz=math.pi)
        imp(GEN / W.format("Rosette"), (x, 18.0, H_S))
        stretch(GEN / W.format("Span"), (x, -18.0 + 0.17, H_S), (x, 18.0 - 0.17, H_S))
    else:      # an open plot: the pole pair 0.45 off the kerb face
        for y in (-10.45, 10.45):
            imp(GEN / W.format("Pole"), (x, y, 0.185))
        stretch(GEN / W.format("Span"), (x, -10.45 + 0.16, H_S), (x, 10.45 - 0.16, H_S))
    for yo in (-1.8, 1.8):
        z = 0.2 if i % 2 else -0.2
        imp(GEN / W.format("Hanger"), (x, yo + z, H_C))
for yo in (-1.8, 1.8):
    pts = [(x, yo + (0.2 if i % 2 else -0.2), H_C) for i, x in enumerate([-75.0] + spans + [45.0])]
    for a, b in zip(pts, pts[1:]):
        stretch(GEN / W.format("Contact"), a, b)
camera((-38.0, -26.0, 24.0), (-2.0, 0.0, 3.0), lens=35)     # about the play camera: pitch ~ 38, ~ 45 m off
render("wires_play")
camera((-55.0, -6.0, 1.7), (20.0, 0.0, 6.0), lens=28)
render("wires_street")
camera((-15.0 + 3.5, -12.5, 6.2), (-15.0, -16.0, 6.4), lens=40)
render("wires_close_rosette")
camera((15.0 - 3.0, -7.0, 6.8), (15.0, -9.0, 6.6), lens=40)
render("wires_close_pole")
for pid in ("Contact", "Span", "Hanger", "Rosette", "Pole"):
    clear(aspect=1.0)
    objs = imp(GEN / W.format(pid))
    lo, hi = bounds(objs)
    report[f"wire/{pid}"] = dict(tris=tris(objs), size=[round(v, 4) for v in (hi - lo)])
    c, s = (lo + hi) / 2, max(hi - lo)
    camera(c + Vector((0.9, -1.6, 0.6)) * s * 1.1, c, lens=50)
    render(f"piece_{pid}")
(OUT / "lods.json").write_text(json.dumps(report, indent=1) + "\n")
print("[075vista] done")
