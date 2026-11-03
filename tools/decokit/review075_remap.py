"""Brief 075 remap (Vela): contact renders of the old pieces adopted for the remap's roofs, gardens, plaza, seaside and alleys: the old DecoKit FBX (left, read only)
and ours, the generator's binary that ships (right), at true size on one ground, with a 1.8 m post for scale.

/Applications/Blender.app/Contents/MacOS/Blender -b -P tools/decokit/review075_remap.py -- <out_dir> [px]
Eevee, a warm sun and a teal sky: shape and colour, not the game's lighting. Writes <id>.png per piece and keys.json (size, tris old / ours).
"""
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

REPO = Path(__file__).resolve().parents[2]
A = sys.argv[sys.argv.index("--") + 1:]
OUT = Path(A[0]); PX = int(A[1]) if len(A) > 1 and A[1].isdigit() else 560
OUT.mkdir(parents=True, exist_ok=True)
GEN = REPO / "out/decokit"
LEGACY = Path("~/Projects/the-suborned/game/Assets/TheSuborned/Art/DecoKit").expanduser()
table = {m["name"]: m for m in json.loads((GEN / "decokit_materials.json").read_text())["materials"]}
IDS = ["Roof_Bandstand", "Roof_Radome", "Roof_Dish", "Roof_Drum", "Roof_Fin", "PlazaFountain", "DecoPavilion", "CafeSet", "GrandStair", "PlanterBed", "PlanterHex", "FlutedColumn",
       "BusShelter", "TramCanopy", "BistroSet", "CafeTable_Tulip", "CafeChair_Tulip", "CoinTelescope", "LifebuoyStand", "AlleyBridge", "BikeLean", "CableSpan_6", "CrateStack",
       "MilkCrates", "Pallet", "PipeBundle_6", "SteamVent", "TrashCans", "BackDoorLamp"]
kit = {a["id"]: a for a in json.loads((REPO / "contracts/world/kit.json").read_text())["assets"]}
_means, _mats = {}, {}


def lin(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def tex_mean(name):
    for r in (GEN / "Textures", LEGACY / "Textures"):
        p = r / name
        if p.exists():
            img = bpy.data.images.load(str(p)); px = list(img.pixels[:])
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
        m = _means.setdefault(row["texture"], tex_mean(row["texture"])); c = [a * b for a, b in zip(c, m)]
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
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.42, 0.66, 0.70, 1); w.node_tree.nodes["Background"].inputs[1].default_value = 0.9
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


def imp(path, dx):
    before = set(bpy.context.scene.objects)
    bpy.ops.import_scene.fbx(filepath=str(path))
    new = [o for o in bpy.context.scene.objects if o not in before]
    for r in [o for o in new if o.parent is None]:
        r.location = Vector(r.location) + Vector((dx, 0, 0))
    bpy.context.view_layer.update()
    objs = [o for o in new if o.type == "MESH"]
    colour(objs)
    return objs


def ground(rgb=(0.86, 0.82, 0.74)):
    me = bpy.data.meshes.new("g"); o = bpy.data.objects.new("ground", me); bpy.context.scene.collection.objects.link(o)
    me.from_pydata([(-500, -500, 0), (500, -500, 0), (500, 500, 0), (-500, 500, 0)], [], [(0, 1, 2, 3)])
    m = bpy.data.materials.new("gm"); m.use_nodes = True; m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (*[lin(x) for x in rgb], 1)
    me.materials.append(m)


def post(x, y):
    bpy.ops.mesh.primitive_cylinder_add(radius=0.12, depth=1.8, location=(x, y, 0.9))
    o = bpy.context.active_object
    m = bpy.data.materials.new("post"); m.use_nodes = True; m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.75, 0.08, 0.06, 1)
    o.data.materials.append(m)


def tris(objs):
    return sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in objs)


keys = {}
for i in IDS:
    a = kit[i]; cat = a["category"]
    old = LEGACY / f"Models/{cat}/DK_{i}.fbx"; ours = GEN / f"Models/{cat}/DK_{i}.fbx"
    w, h, d = a["size"]; gap = max(w, d) * 0.6 + 0.8
    clear(); ground()
    o1 = imp(old, -(w / 2 + gap / 2)); o2 = imp(ours, w / 2 + gap / 2)
    post(0, -max(d, 1.0) / 2 - 0.6)
    span = max(2 * w + gap, h * 1.6, d * 1.2, 2.2)
    cam = bpy.data.cameras.new("cam"); cam.lens = 50; cam.clip_end = 5000
    co = bpy.data.objects.new("cam", cam); bpy.context.scene.collection.objects.link(co); bpy.context.scene.camera = co
    tgt = Vector((0, 0, h * 0.42)); dist = span * 1.55
    eye = tgt + Vector((dist * 0.35, -dist * 0.85, dist * 0.5))
    co.location = eye; co.rotation_euler = (tgt - eye).to_track_quat("-Z", "Y").to_euler()
    bpy.context.scene.render.filepath = str(OUT / f"{i}.png"); bpy.ops.render.render(write_still=True)
    keys[i] = dict(size=a["size"], status=a["status"], use=a["tags"]["use"], tris_old=tris(o1), tris_ours=tris(o2), lods=[l["tris"] for l in a.get("lods", [])])
(OUT / "keys.json").write_text(json.dumps(keys, indent=1) + "\n")
print(f"[review075_remap] {len(keys)} pieces -> {OUT}")
