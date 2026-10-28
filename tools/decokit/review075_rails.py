"""Brief 075 golden view (Vela): the flush street-running track at the golden camera, in Blender.

/Applications/Blender.app/Contents/MacOS/Blender -b -P tools/decokit/review075_rails.py -- <out_dir> [before|after] [px]
The ours camera of briefs/075/golden/cameras.json (eye 8.1 m over the Boulevard, 58 deg vertical, looking down it): Blender X = plan x, Y = plan y (Unity z), Z = height.
The street as tramstreet.ts lays it: two tracks at x +-1.8 (3.6 m centres), the setts flat |x| <= 3.6, the powder-blue carriageway to |x| 11, cream pavement past it.
before = Tile_TramRail_12m (the sleeper bed) on DK_Gutter_Setts at tile 1 (look.json `setts` today); after = Tile_TramRail_Flush_12m on DK_Ground_Setts (2 m).
Textured slots (the ground set's albedo + normal + mask packing) are drawn with their maps on the kit's 1 m box UVs / the flats' world-metre UVs; metals flat.
Eevee, a warm low sun, a teal sky. Not the game's lighting or grade: the track's read only. Writes <out_dir>/<variant>.png and <variant>-crop.png (the lower middle third).
"""
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

REPO = Path(__file__).resolve().parents[2]
A = sys.argv[sys.argv.index("--") + 1:]
OUT = Path(A[0]); OUT.mkdir(parents=True, exist_ok=True)
VAR = A[1] if len(A) > 1 else "after"
PX = int(A[2]) if len(A) > 2 else 1920
GEN = REPO / "out/decokit"
GROUND = REPO / "out/decokit-ground"
TEX = GEN / "Textures"
table = {m["name"]: m for m in json.loads((GEN / "decokit_materials.json").read_text())["materials"]}
cam = json.loads((REPO / "briefs/075/golden/cameras.json").read_text())["ours"]


def lin(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def find(name):
    for d in (GROUND, TEX):
        if (d / name).exists():
            return d / name
    return None


_mats = {}


def ground_mat(name, albedo, tile, normal=None, mask=None, rough=0.75):
    if name in _mats:
        return _mats[name]
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; b = nt.nodes["Principled BSDF"]
    tc, mp = nt.nodes.new("ShaderNodeTexCoord"), nt.nodes.new("ShaderNodeMapping")
    mp.inputs["Scale"].default_value = (1 / tile, 1 / tile, 1)
    nt.links.new(tc.outputs["UV"], mp.inputs["Vector"])

    def img(file, non_color=False):
        t = nt.nodes.new("ShaderNodeTexImage"); t.image = bpy.data.images.load(str(find(file)), check_existing=True)
        if non_color:
            t.image.colorspace_settings.name = "Non-Color"
        nt.links.new(mp.outputs["Vector"], t.inputs["Vector"])
        return t
    nt.links.new(img(albedo).outputs["Color"], b.inputs["Base Color"])
    if normal and find(normal):
        nm = nt.nodes.new("ShaderNodeNormalMap"); nm.inputs["Strength"].default_value = 0.8
        nt.links.new(img(normal, True).outputs["Color"], nm.inputs["Color"]); nt.links.new(nm.outputs["Normal"], b.inputs["Normal"])
    if mask and find(mask):
        t = img(mask, True)
        inv = nt.nodes.new("ShaderNodeMath"); inv.operation = "SUBTRACT"; inv.inputs[0].default_value = 1.0
        nt.links.new(t.outputs["Alpha"], inv.inputs[1]); nt.links.new(inv.outputs["Value"], b.inputs["Roughness"])
    else:
        b.inputs["Roughness"].default_value = rough
    _mats[name] = m
    return m


def flat_mat(name):
    if name in _mats:
        return _mats[name]
    row = table.get(name, {})
    m = bpy.data.materials.new(name + "_flat"); m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    c = [lin(x) for x in row.get("color", [0.6, 0.6, 0.6])[:3]]
    b.inputs["Base Color"].default_value = (*c, 1)
    b.inputs["Metallic"].default_value = row.get("metallic", 0.0)
    b.inputs["Roughness"].default_value = 1 - row.get("smoothness", 0.5)
    _mats[name] = m
    return m


def kit_mat(name):
    base = name.split(".")[0]
    if base == "DK_Ground_Setts":
        return ground_mat(base, "ground_setts_v1.png", 2.0, "ground_setts_v1_normal.png", "ground_setts_v1_mask.png")
    if base in ("DK_Granite_Kerb", "DK_Granite_Kerb_B", "DK_Granite_Rail"):
        m = ground_mat(base, "ground_kerb_v1.png", 0.5, "ground_kerb_v1_normal.png", "ground_kerb_v1_mask.png")
        tint = {"DK_Granite_Kerb_B": "#EAE6DF", "DK_Granite_Rail": "#CBD4DE"}.get(base)
        if tint and not m.get("tinted"):
            nt = m.node_tree; b = nt.nodes["Principled BSDF"]; src = b.inputs["Base Color"].links[0].from_socket
            mul = nt.nodes.new("ShaderNodeMix"); mul.data_type, mul.blend_type = "RGBA", "MULTIPLY"; mul.inputs["Factor"].default_value = 1.0
            mul.inputs["B"].default_value = (*[lin(int(tint[i:i + 2], 16) / 255) for i in (1, 3, 5)], 1)
            nt.links.new(src, mul.inputs["A"]); nt.links.new(mul.outputs["Result"], b.inputs["Base Color"]); m["tinted"] = 1
        return m
    return flat_mat(base)


def plane(name, x0, x1, y0, y1, z, mat):
    me = bpy.data.meshes.new(name)
    me.from_pydata([(x0, y0, z), (x1, y0, z), (x1, y1, z), (x0, y1, z)], [], [(0, 1, 2, 3)])
    uv = me.uv_layers.new()
    for li, l in enumerate(me.loops):
        v = me.vertices[l.vertex_index].co
        uv.data[li].uv = (v.x, v.y)  # world metres, as the builder's flats
    me.materials.append(mat)
    o = bpy.data.objects.new(name, me); bpy.context.scene.collection.objects.link(o)
    return o


def imp(path):
    before = set(bpy.context.scene.objects)
    bpy.ops.import_scene.fbx(filepath=str(path))
    new = [o for o in bpy.context.scene.objects if o not in before]
    for o in new:
        if o.type == "MESH":
            for i, s in enumerate(o.material_slots):
                if s.material:
                    o.material_slots[i].material = kit_mat(s.material.name)
    return new


# ------------------------------------------------------------------ scene
bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene
sc.render.engine = "BLENDER_EEVEE"
sc.render.resolution_x, sc.render.resolution_y = PX, PX * 9 // 16
sc.view_settings.view_transform = "Standard"
w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
w.node_tree.nodes["Background"].inputs["Color"].default_value = (0.32, 0.72, 0.70, 1); w.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.45
sun = bpy.data.lights.new("sun", "SUN"); sun.energy = 3.6; sun.color = (1.0, 0.94, 0.84); sun.angle = math.radians(1.5)
so = bpy.data.objects.new("sun", sun); sc.collection.objects.link(so); so.rotation_euler = (math.radians(58), 0, math.radians(-40))

L = 252.0
road = ground_mat("road", "ground_roadenamel_v1.png", 16.0, "ground_asphalt_v1_normal.png", "ground_asphalt_v1_mask.png")
pave = ground_mat("pave", "ground_pavelimestone_v1.png", 2.0, "ground_pavelimestone_v1_normal.png", "ground_pavelimestone_v1_mask.png")
plane("road", -11, 11, -10, L, -0.02, road)  # the carriageway at the road level, the setts 2 cm over it (tramstreet.ts: the setts top 0.02)
for sg in (1, -1):
    plane(f"pave{sg}", *sorted((sg * 11, sg * 22)), -10, L, 0.15, pave)
if VAR == "before":
    setts = ground_mat("setts_before", "ground_gutter_v1.png", 1.0, "ground_gutter_v1_normal.png", "ground_gutter_v1_mask.png")
    piece = GEN / "Models/Ground/DK_Tile_TramRail_12m.fbx"
else:
    setts = kit_mat("DK_Ground_Setts")
    piece = GEN / "Models/Ground/DK_Tile_TramRail_Flush_12m.fbx"
plane("setts", -3.6, 3.6, -10, L, -0.001, setts)  # the builder cuts it under the tiles; here it lies 1 mm under them instead

src = [o for o in imp(piece) if o.type == "MESH"]
for o in src:
    o.rotation_euler.z += math.pi / 2  # the kit's track runs along X: turn it onto the Boulevard (+Y)
bpy.context.view_layer.update()
for x in (-1.8, 1.8):
    for k in range(int(L // 12)):
        for o in src:
            c = o.copy(); c.data = o.data  # linked
            c.parent = None
            c.matrix_world = o.matrix_world.copy()
            c.location = Vector((x, 12 * k, o.matrix_world.translation.z))
            sc.collection.objects.link(c)
for o in src:
    o.hide_render = True

# ------------------------------------------------------------------ camera (Unity x, y, z -> Blender x, z, y)
e, lk = cam["eye"], cam["look"]
eye = Vector((e[0], e[2] + 214.5703, e[1]))  # the street's start at Y 0 (the plan's y -214.57 is the eye)
look = Vector((lk[0], lk[2] + 214.5703, lk[1]))
cd = bpy.data.cameras.new("cam"); cd.sensor_fit = "VERTICAL"; cd.angle_y = math.radians(cam["fov_deg"]); cd.clip_start = 0.1; cd.clip_end = 2000
co = bpy.data.objects.new("cam", cd); sc.collection.objects.link(co); sc.camera = co
co.location = eye
co.rotation_euler = (look - eye).to_track_quat("-Z", "Y").to_euler()
sc.render.filepath = str(OUT / f"{VAR}.png")
bpy.ops.render.render(write_still=True)
print("[rails]", VAR, sc.render.filepath)
