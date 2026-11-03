"""Brief 075-F1 (Vela): the billboard variants at play zoom.

/Applications/Blender.app/Contents/MacOS/Blender -b -P tools/decokit/review075_posters.py -- <out_dir>
Imports the 8 billboards' FBXs as catalogued (contracts/world/kit.json, the generator's out/decokit), draws each art slot through a variant exactly as
Unity will (kit.json `posters`: the variant's sheet, its _BaseMap offset over the baked canonical UVs, Repeat wrap; DK_Billboard_Ads_Lit adds emission),
colours every other slot flat from out/decokit/decokit_materials.json (textured slots at the texture's mean), and renders 1920 x 1080 shots with the play
camera's framing: orthographic, pitch 38, yaw 30, a 1.75 m figure about 50 px tall (GDD §01: characters 40-60 px at 1080p). Eevee, a warm sun, a teal sky:
not the game's lighting or grade (Glint's), only legibility and fit of the art at that size.
Shots: play_a.png and play_b.png (the same three boards, two seeds' worth of variants), each with a 2x crop.
"""
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

REPO = Path(__file__).resolve().parents[2]
OUT = Path(sys.argv[sys.argv.index("--") + 1])
OUT.mkdir(parents=True, exist_ok=True)
GEN = REPO / "out/decokit"
LEGACY = Path("~/Projects/the-suborned/game/Assets/TheSuborned/Art/DecoKit").expanduser()
kit = json.loads((REPO / "contracts/world/kit.json").read_text())
assets = {a["id"]: a for a in kit["assets"]}
ps = kit["posters"]
table = {m["name"]: m for m in json.loads((GEN / "decokit_materials.json").read_text())["materials"]}
# (piece, x, y, z, yaw deg, ad key): a 3-storey cream block with a rooftop board, a wall board on its front, a pylon on the plaza in front
SHOTS = {"play_a": {"Billboard_Rooftop_A": "luna", "Billboard_Wall_A": "tidy", "Billboard_Pylon_A": "coral"},
         "play_b": {"Billboard_Rooftop_A": "skyrail", "Billboard_Wall_A": "mints", "Billboard_Pylon_A": "pearls"}}
PLACE = {"Billboard_Rooftop_A": (0.0, 2.0, 12.6, 0), "Billboard_Wall_A": (-5.0, -6.0, 4.4, 0), "Billboard_Pylon_A": (8.0, -16.0, 0.0, 0)}


def lin(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def tex_path(name):
    for r in (GEN / "Textures", LEGACY / "Textures"):
        if (r / name).exists():
            return r / name
    return None


_means = {}


def tex_mean(name):
    if name not in _means:
        p = tex_path(name)
        if not p:
            _means[name] = [1.0, 1.0, 1.0]
        else:
            img = bpy.data.images.load(str(p))
            px = img.pixels[:]
            _means[name] = [lin(sum(px[i::4][::97]) / len(px[i::4][::97])) for i in range(3)]
            bpy.data.images.remove(img)
    return _means[name]


def flat(name):
    row = table.get(name)
    if not row:
        return [0.6, 0.6, 0.6], 0.0, 0.5, 0.0
    c = [lin(x) for x in row["color"]]
    if row["texture"] and row["kind"] not in ("art", "emit"):
        c = [a * b for a, b in zip(c, tex_mean(row["texture"]))]
    return c, row["metallic"], row["smoothness"], (row.get("emission") or 0.0) if row["kind"] == "emit" else 0.0


def art_mat(base, key):
    ad = ps["ads"][key]
    sheet = ps["sheets"][ad["sheet"]]["texture"].split("/")[-1]
    m = bpy.data.materials.new(f"art_{base}_{key}"); m.use_nodes = True
    nt = m.node_tree; b = nt.nodes["Principled BSDF"]
    uv = nt.nodes.new("ShaderNodeTexCoord"); mp = nt.nodes.new("ShaderNodeMapping"); tx = nt.nodes.new("ShaderNodeTexImage")
    mp.inputs["Location"].default_value = (ad["offset"][0], ad["offset"][1], 0)
    tx.image = bpy.data.images.load(str(tex_path(sheet)), check_existing=True); tx.extension = "REPEAT"; tx.interpolation = "Smart"
    nt.links.new(uv.outputs["UV"], mp.inputs["Vector"]); nt.links.new(mp.outputs["Vector"], tx.inputs["Vector"]); nt.links.new(tx.outputs["Color"], b.inputs["Base Color"])
    b.inputs["Roughness"].default_value = 0.6
    if base.endswith("_Lit"):
        nt.links.new(tx.outputs["Color"], b.inputs["Emission Color"]); b.inputs["Emission Strength"].default_value = 0.35
    return m


def colour(objs, base_slot, key):
    done = {}
    for o in objs:
        for slot in o.material_slots:
            if not slot.material:
                continue
            n = slot.material.name.split(".")[0]
            if n not in done:
                if n == base_slot:
                    done[n] = art_mat(n, key)
                else:
                    c, met, sm, em = flat(n)
                    nm = bpy.data.materials.new(f"r_{n}"); nm.use_nodes = True
                    b = nm.node_tree.nodes["Principled BSDF"]
                    b.inputs["Base Color"].default_value = (*c, 1); b.inputs["Metallic"].default_value = met; b.inputs["Roughness"].default_value = 1 - sm
                    if em:
                        b.inputs["Emission Color"].default_value = (*c, 1); b.inputs["Emission Strength"].default_value = min(em, 2.0)
                    done[n] = nm
            slot.material = done[n]


def box(lo, hi, rgb):
    bpy.ops.mesh.primitive_cube_add(size=1, location=[(a + b) / 2 for a, b in zip(lo, hi)])
    o = bpy.context.active_object; o.scale = [b - a for a, b in zip(lo, hi)]
    m = bpy.data.materials.new("b"); m.use_nodes = True
    m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (*[lin(x) for x in rgb], 1)
    m.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.8
    o.data.materials.append(m)
    return o


def shot(name, keys):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items.keys() else "BLENDER_EEVEE"
    sc.render.resolution_x, sc.render.resolution_y = 1920, 1080
    sc.view_settings.view_transform = "Standard"
    w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.42, 0.66, 0.70, 1); w.node_tree.nodes["Background"].inputs[1].default_value = 0.9
    sun = bpy.data.lights.new("sun", "SUN"); sun.energy = 3.2; sun.color = (1.0, 0.95, 0.86)
    so = bpy.data.objects.new("sun", sun); sc.collection.objects.link(so); so.rotation_euler = (math.radians(50), 0, math.radians(-35))
    box((-60, -60, -0.1), (60, 60, 0), (0.93, 0.89, 0.82))                     # pale paving
    box((-11, -6, 0), (11, 8, 12.6), (0.96, 0.91, 0.82))                       # 3-storey cream block (16.8 m cap: roof at 12.6 m, board above)
    box((-11.02, -6.02, 3.6), (11.02, -5.98, 4.0), (0.47, 0.78, 0.72))          # a seafoam band, for scale
    for x in (-2.0, 0.0, 2.0):                                                  # three 1.75 m figures on the pavement
        box((x - 0.25, -9.2, 0), (x + 0.25, -8.9, 1.75), (0.15, 0.2, 0.22))
    for pid, key in keys.items():
        f = next(x for x in assets[pid]["files"] if x["role"] == "fbx")
        before = set(sc.objects)
        bpy.ops.import_scene.fbx(filepath=str((GEN if f.get("root") == "generated" else LEGACY) / f["path"]))
        objs = [o for o in sc.objects if o not in before]
        colour([o for o in objs if o.type == "MESH"], ps["adPieces"][pid], key)
        root = bpy.data.objects.new(f"root_{pid}", None); sc.collection.objects.link(root)
        for o in objs:
            if o.parent is None:
                o.parent = root
        x, y, z, yaw = PLACE[pid]
        root.location = (x, y, z); root.rotation_euler = (0, 0, math.radians(yaw))
    cam = bpy.data.cameras.new("cam"); cam.type = "ORTHO"; cam.ortho_scale = 1920 / 1080 * 1.75 / 50 * 1080  # 1.75 m = 50 px at 1080p
    co = bpy.data.objects.new("cam", cam); sc.collection.objects.link(co); sc.camera = co
    yaw, pit, tgt = math.radians(30), math.radians(38), Vector((0, -4, 5))
    co.location = tgt + Vector((math.sin(yaw) * math.cos(pit), -math.cos(yaw) * math.cos(pit), math.sin(pit))) * 120
    co.rotation_euler = (tgt - co.location).to_track_quat("-Z", "Y").to_euler()
    cam.clip_end = 400
    sc.render.filepath = str(OUT / f"{name}.png")
    bpy.ops.render.render(write_still=True)
    print(f"[075f1] {name} -> {OUT / f'{name}.png'}")


for n, k in SHOTS.items():
    shot(n, k)
