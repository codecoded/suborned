"""Brief 075 golden view (Vela): the tram pieces' contact sheet and LOD check.

/Applications/Blender.app/Contents/MacOS/Blender -b -P tools/decokit/review075_tram.py -- <out_dir> [px]
For each piece: the generator's L0, L1 and L2 (out/decokit), and for the two old pieces the old DecoKit's own FBX beside them (old | ours). Slots are coloured flat
from out/decokit/decokit_materials.json (a textured slot at its texture's mean), emissive rows lit. Also a stop vignette: two tracks of Tile_TramRail_12m and
Tile_TramRail at the plan's 3.6 m centres, the tram on one, TramCanopy_Play, TicketKiosk and TimetableColumn on a platform. Writes <out_dir>/lods.json:
per file, triangles and the bounds (x, y, z extents in Blender's frame after import), so a level that loses height (StreamCornerStack_Top's mast) shows.
Eevee, a warm sun and a teal sky. Not the game's lighting: shape, colour and the LOD silhouettes only."""
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
VIGNETTE_ONLY = "--vignette" in A  # the stop vignette alone (lods.json untouched)
OUT.mkdir(parents=True, exist_ok=True)
GEN = REPO / "out/decokit"
LEGACY = Path("~/Projects/the-suborned/game/Assets/TheSuborned/Art/DecoKit").expanduser()
PIECES = {"Tram": "Vehicles", "Tile_TramRail": "Ground", "Tile_TramRail_12m": "Ground", "TicketKiosk": "Structures", "TimetableColumn": "Furnish",
          "StreamCornerStack_Top": "Facades"}
OLD = {"Tram": "Models/Vehicles/DK_Tram.fbx", "Tile_TramRail": "Models/Ground/DK_Tile_TramRail.fbx"}
table = {m["name"]: m for m in json.loads((GEN / "decokit_materials.json").read_text())["materials"]}


def lin(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


_means = {}
_mats = {}


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
    row = table.get(name)
    if not row:
        return [0.6, 0.6, 0.6], 0.0, 0.5, 0.0
    c = [lin(x) for x in row["color"]]
    if row.get("texture"):
        m = _means.setdefault(row["texture"], tex_mean(row["texture"]))
        c = [a * b for a, b in zip(c, m)]
    emit = 1.0 if (row.get("emission") or 0) > 0 or row.get("kind") == "emit" else 0.0
    return c, row.get("metallic", 0.0), row.get("smoothness", 0.5), emit


def clear():
    _mats.clear()
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items.keys() else "BLENDER_EEVEE"
    sc.render.resolution_x, sc.render.resolution_y = PX, int(PX * 0.75)
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


def imp(path, at=(0, 0, 0), rz=0.0):
    before = set(bpy.context.scene.objects)
    bpy.ops.import_scene.fbx(filepath=str(path))
    new = [o for o in bpy.context.scene.objects if o not in before]
    roots = [o for o in new if o.parent is None]
    for r in roots:
        r.location = Vector(r.location) + Vector(at)
        r.rotation_euler.z += rz
    bpy.context.view_layer.update()
    objs = [o for o in new if o.type == "MESH"]
    colour(objs)
    return objs


def bounds(objs):
    pts = [o.matrix_world @ Vector(c) for o in objs for c in o.bound_box]
    lo = Vector([min(p[i] for p in pts) for i in range(3)]); hi = Vector([max(p[i] for p in pts) for i in range(3)])
    return lo, hi


def tris(objs):
    return sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in objs)


def shoot(objs, name, yaw=35, pit=20, k=2.3, focus=None):
    lo, hi = bounds(objs)
    c, size = (lo + hi) / 2, hi - lo
    if focus:
        c = Vector(focus)
    cam = bpy.data.cameras.new("cam"); cam.lens = 50
    co = bpy.data.objects.new("cam", cam); bpy.context.scene.collection.objects.link(co); bpy.context.scene.camera = co
    d = max(size.z, max(size.x, size.y)) * k
    y, p = math.radians(yaw), math.radians(pit)
    co.location = c + Vector((math.sin(y) * math.cos(p), -math.cos(y) * math.cos(p), math.sin(p))) * d
    co.rotation_euler = (c - co.location).to_track_quat("-Z", "Y").to_euler()
    bpy.context.scene.render.filepath = str(OUT / f"{name}.png")
    bpy.ops.render.render(write_still=True)


report = {}
for pid, cat in ({} if VIGNETTE_ONLY else PIECES).items():
    files = [("L0", GEN / f"Models/{cat}/DK_{pid}.fbx"), ("L1", GEN / f"Models/{cat}/DK_{pid}_L1.fbx"), ("L2", GEN / f"Models/{cat}/DK_{pid}_L2.fbx")]
    if pid in OLD:
        files.insert(0, ("old", LEGACY / OLD[pid]))
    for tag, f in files:
        clear()
        objs = imp(f)
        lo, hi = bounds(objs)
        report[f"{pid}/{tag}"] = dict(file=str(f).replace(str(Path.home()), "~"), tris=tris(objs), size=[round(v, 3) for v in (hi - lo)])
        yaw, pit = (200, 18) if pid == "Tram" else (35, 50) if pid.startswith("Tile") else (215, 15) if pid in ("TicketKiosk", "TimetableColumn") else (35, 15)
        shoot(objs, f"{pid}_{tag}", yaw=yaw, pit=pit, k=1.9 if pid.startswith("Tile") else 2.3)
        print(f"[075tram] {pid} {tag} tris={report[f'{pid}/{tag}']['tris']} size={report[f'{pid}/{tag}']['size']}")

# stop vignette: two tracks at 3.6 m centres, the tram on one, the platform's shelter, kiosk and column
clear()
objs = []
for yo in (-1.8, 1.8):
    for x in (-12.0, 0.0, 12.0):
        objs += imp(GEN / "Models/Ground/DK_Tile_TramRail_12m.fbx", (x, yo, 0))
    for x in (19.0, 21.0):
        objs += imp(GEN / "Models/Ground/DK_Tile_TramRail.fbx", (x, yo, 0))
pl = bpy.data.meshes.new("pl"); po = bpy.data.objects.new("platform", pl); bpy.context.scene.collection.objects.link(po)
import bmesh
bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1.0); bm.to_mesh(pl); bm.free()
po.scale = (30, 3.0, 0.3); po.location = (2.0, 4.6, 0.15)
pm = bpy.data.materials.new("plat"); pm.use_nodes = True; pm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (*[lin(x) for x in (0.9, 0.86, 0.78)], 1)
pl.materials.append(pm)
tram = imp(GEN / "Models/Vehicles/DK_Tram.fbx", (-9.0, 1.8, 0), rz=math.pi / 2)  # the car's length is along Blender Y: turned onto the +X track
tl, th = bounds(tram)
objs += tram
objs += imp(GEN / "Models/Structures/DK_TramCanopy_Play.fbx", (-2.0, 5.2, 0.3))
objs += imp(GEN / "Models/Structures/DK_TicketKiosk.fbx", (6.0, 5.3, 0.3))
objs += imp(GEN / "Models/Furnish/DK_TimetableColumn.fbx", (3.2, 4.2, 0.3))
report["vignette/tram"] = dict(size=[round(v, 3) for v in (th - tl)])
shoot(objs, "stop_vignette", yaw=-28, pit=26, k=0.62, focus=(2.0, 3.0, 1.4))
if not VIGNETTE_ONLY:
    (OUT / "lods.json").write_text(json.dumps(report, indent=1) + "\n")
print("[075tram] done")
