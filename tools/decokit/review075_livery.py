"""Brief 075-C step 2 (Vela): the landmark livery, before | after, plus row C5 (the Skyport tower's red-and-white bands).

/Applications/Blender.app/Contents/MacOS/Blender -b -P tools/decokit/review075_livery.py -- <out_dir> [px]
Imports each piece's FBX as catalogued in contracts/world/kit.json (the generator's out/decokit or the legacy kit), colours its slots flat from
out/decokit/decokit_materials.json (a textured slot at its texture's mean, the way the variants are tinted), and renders every piece twice: `before` (the kit
materials) and `after` (each slot the kit's `landmarks.pieces` lists swapped to its key's hex from out/decokit/Data/livery.json). Also writes
<out_dir>/slots.json: per piece, the faces and area per material (C5: the coral band and vermilion lattice area on Skyport_Tower against Skyport_Tower_Portal).
Eevee, a warm sun and a teal sky, 3/4 view from 25 degrees up. Not the game's lighting: hue and area only."""
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

REPO = Path(__file__).resolve().parents[2]
A = sys.argv[sys.argv.index("--") + 1:]
OUT = Path(A[0])
PX = int(A[1]) if len(A) > 1 else 900
OUT.mkdir(parents=True, exist_ok=True)
GEN = REPO / "out/decokit"
LEGACY = Path("~/Projects/the-suborned/game/Assets/TheSuborned/Art/DecoKit").expanduser()
PIECES = ["SphereStation", "Skyport_Tower~legacy", "Skyport_Tower", "Skyport_Tower_Portal", "SpaceHQ_Portal"]  # ~legacy: the old kit's own FBX (the reel's), for row C5

kit = json.loads((REPO / "contracts/world/kit.json").read_text())
assets = {a["id"]: a for a in kit["assets"]}
table = {m["name"]: m for m in json.loads((GEN / "decokit_materials.json").read_text())["materials"]}
livery = json.loads((GEN / "Data/livery.json").read_text())["keys"]
pieces_lv = (kit.get("landmarks") or {}).get("pieces") or {}


def lin(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def hex_lin(h):
    h = h.lstrip("#")
    return [lin(int(h[i:i + 2], 16) / 255) for i in (0, 2, 4)]


def tex_mean(name):
    for r in (GEN / "Textures", LEGACY / "Textures"):
        p = r / name
        if p.exists():
            img = bpy.data.images.load(str(p))
            px = list(img.pixels[:])  # linear floats for sRGB images? Blender stores display-referred floats for 8-bit: treat as sRGB
            n = len(px) // 4
            m = [sum(px[i::4][::97]) / len(px[i::4][::97]) for i in range(3)]
            bpy.data.images.remove(img)
            return [lin(x) for x in m]
    return [1.0, 1.0, 1.0]


_means = {}


def base_lin(name):
    row = table.get(name.split(".")[0])
    if not row:
        return [0.6, 0.6, 0.6], 0.0, 0.5
    c = [lin(x) for x in row["color"]]
    if row["texture"]:
        m = _means.setdefault(row["texture"], tex_mean(row["texture"]))
        c = [a * b for a, b in zip(c, m)]
    return c, row["metallic"], row["smoothness"]


def fbx_of(pid):
    if pid.endswith("~legacy"):
        f = next(x for x in assets[pid[:-7]]["files"] if x["role"] == "fbx")
        return LEGACY / f["path"]
    f = next(x for x in assets[pid]["files"] if x["role"] == "fbx")
    return (GEN if f.get("root") == "generated" else LEGACY) / f["path"]


def clear():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def stage():
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items.keys() else "BLENDER_EEVEE"
    sc.render.resolution_x, sc.render.resolution_y = PX, int(PX * 1.25)
    sc.view_settings.view_transform = "Standard"
    w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.42, 0.66, 0.70, 1)
    w.node_tree.nodes["Background"].inputs[1].default_value = 0.9
    sun = bpy.data.lights.new("sun", "SUN"); sun.energy = 3.2; sun.color = (1.0, 0.95, 0.86)
    o = bpy.data.objects.new("sun", sun); sc.collection.objects.link(o); o.rotation_euler = (math.radians(50), 0, math.radians(-35))


def colour(objs, swap):
    done = {}
    for o in objs:
        for slot in o.material_slots:
            m = slot.material
            if not m:
                continue
            base = m.name.split(".")[0]
            if base not in done:
                c, met, sm = base_lin(base)
                if base in swap:
                    c = hex_lin(swap[base])
                nm = bpy.data.materials.new(f"r_{base}"); nm.use_nodes = True
                b = nm.node_tree.nodes["Principled BSDF"]
                b.inputs["Base Color"].default_value = (*c, 1); b.inputs["Metallic"].default_value = met; b.inputs["Roughness"].default_value = 1 - sm
                done[base] = nm
            slot.material = done[base]


def area_by_mat(objs):
    out = {}
    for o in objs:
        if o.type != "MESH":
            continue
        me = o.data
        s = max(o.matrix_world.to_scale())
        for p in me.polygons:
            n = o.material_slots[p.material_index].material.name.split(".")[0] if o.material_slots and o.material_slots[p.material_index].material else "?"
            r = out.setdefault(n, [0, 0.0]); r[0] += 1; r[1] += p.area * s * s
    return {k: dict(faces=v[0], m2=round(v[1], 1)) for k, v in sorted(out.items(), key=lambda kv: -kv[1][1])}


slots = {}
for pid in PIECES:
    for tag in ("before", "after"):
        clear(); stage()
        bpy.ops.import_scene.fbx(filepath=str(fbx_of(pid)))
        objs = [o for o in bpy.context.scene.objects if o.type == "MESH"]
        if tag == "before":
            slots[pid] = dict(fbx=str(fbx_of(pid).relative_to(GEN if str(fbx_of(pid)).startswith(str(GEN)) else LEGACY)), livery=pieces_lv.get(pid.split("~")[0], {}), area=area_by_mat(objs))
        swap = {b: livery[k]["hex"] for b, k in pieces_lv.get(pid.split("~")[0], {}).items()} if tag == "after" else {}
        colour(objs, swap)
        lo = Vector((min(v[i] for o in objs for v in [o.matrix_world @ Vector(c) for c in o.bound_box]) for i in range(3)))
        hi = Vector((max(v[i] for o in objs for v in [o.matrix_world @ Vector(c) for c in o.bound_box]) for i in range(3)))
        c, size = (lo + hi) / 2, (hi - lo)
        cam = bpy.data.cameras.new("cam"); cam.lens = 50
        co = bpy.data.objects.new("cam", cam); bpy.context.scene.collection.objects.link(co); bpy.context.scene.camera = co
        d = max(size.z, max(size.x, size.y) * 1.25) * 2.4
        yaw, pit = math.radians(30), math.radians(18)
        co.location = c + Vector((math.sin(yaw) * math.cos(pit), -math.cos(yaw) * math.cos(pit), math.sin(pit))) * d
        co.rotation_euler = (c - co.location).to_track_quat("-Z", "Y").to_euler()
        bpy.context.scene.render.filepath = str(OUT / f"{pid}_{tag}.png")
        bpy.ops.render.render(write_still=True)
        print(f"[075c] {pid} {tag} -> {OUT / f'{pid}_{tag}.png'}")
(OUT / "slots.json").write_text(json.dumps(slots, indent=1) + "\n")
