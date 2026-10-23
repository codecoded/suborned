"""Impostor trial for the furthest band (Vela, brief 028 "Palm LODs"): three crossed cards baked from the shipped palm, rendered next to L0 and L2 at the same distances.

blender -b -P tools/decokit/palm_lod_impostor.py -- <out_dir> [atlas_png_out]
Bakes CoconutPalm_Street L0 from azimuths 0, 60 and 120 (orthographic, level, the game's sun fixed in the world, 40 px a metre), packs the three views side by side, stands them as three
vertical cards crossing on the trunk's axis (6 triangles, one cutout material), and writes <piece>_L0_/_L2_/_L9_d<dist>_az<az>.png (L9 = the impostor) at the sweep's distances and four
azimuths, two of them half way between baked views (the worst case). Lighting is baked: the card shows the sun as it stood when the views were taken (palm_lod_scene.setup: the game's day sun).
"""
import math
import sys
from pathlib import Path

import bpy
import numpy as np
from mathutils import Euler, Vector

sys.path.insert(0, str(Path(__file__).parent))
import palm_lod_scene as S  # noqa: E402

OUT = Path(sys.argv[sys.argv.index("--") + 1]); OUT.mkdir(parents=True, exist_ok=True)
ATLAS = Path(sys.argv[sys.argv.index("--") + 2]) if len(sys.argv) > sys.argv.index("--") + 2 else OUT / "atlas.png"
NAME, HT, WD, PPM = "CoconutPalm_Street", 11.4, 8.0, 40
VIEWS = [0, 60, 120]
sc, cd, co = S.setup((int(WD * PPM), int(HT * PPM)), samples=64)
l0, l2 = S.make(NAME, 0), S.make(NAME, 2)
S.hide(l2, True)
tiles = []
for a in VIEWS:  # bake
    r = math.radians(a)
    cd.type = "ORTHO"; cd.sensor_fit = "VERTICAL"; cd.ortho_scale = HT; cd.shift_x = cd.shift_y = 0
    co.location = Vector((math.sin(r) * 60, -math.cos(r) * 60, HT / 2)); co.rotation_euler = Euler((math.radians(90), 0, r), "XYZ")
    p = OUT / f"bake_{a}.png"; sc.render.filepath = str(p); bpy.ops.render.render(write_still=True)
    bi = bpy.data.images.load(str(p)); w_, h_ = bi.size; tiles.append(np.array(bi.pixels[:]).reshape(h_, w_, 4))
a_ = np.concatenate(tiles, axis=1)
ai = bpy.data.images.new("atlas", a_.shape[1], a_.shape[0], alpha=True); ai.pixels = a_.ravel(); ai.filepath_raw = str(ATLAS); ai.file_format = "PNG"; ai.save()
# the cards
img = bpy.data.images.load(str(ATLAS)); img.alpha_mode = "STRAIGHT"
m = bpy.data.materials.new("imp"); m.use_nodes = True
nt = m.node_tree; nt.nodes.clear()
tex = nt.nodes.new("ShaderNodeTexImage"); tex.image = img; tex.interpolation = "Linear"
em = nt.nodes.new("ShaderNodeEmission"); em.inputs["Strength"].default_value = 2 ** 0.3  # the scene exposure is -0.3 stops and the bake already has it
tr =nt.nodes.new("ShaderNodeBsdfTransparent"); mix = nt.nodes.new("ShaderNodeMixShader"); out = nt.nodes.new("ShaderNodeOutputMaterial")
nt.links.new(tex.outputs["Color"], em.inputs["Color"]); nt.links.new(tex.outputs["Alpha"], mix.inputs["Fac"])
nt.links.new(tr.outputs[0], mix.inputs[1]); nt.links.new(em.outputs[0], mix.inputs[2]); nt.links.new(mix.outputs[0], out.inputs["Surface"])
m.surface_render_method = "BLENDED"; m.use_backface_culling = False
verts, faces, uvs = [], [], []
for k, a in enumerate(VIEWS):
    r = math.radians(a); right = Vector((math.cos(r), math.sin(r), 0))
    i0 = len(verts)
    for x, z in ((-1, 0), (1, 0), (1, 1), (-1, 1)):
        verts.append(tuple(right * (x * WD / 2) + Vector((0, 0, z * HT))))
    faces.append([i0, i0 + 1, i0 + 2, i0 + 3])
    uvs += [((k + (x + 1) / 2) / len(VIEWS), z) for x, z in ((-1, 0), (1, 0), (1, 1), (-1, 1))]
me = bpy.data.meshes.new("imp"); me.from_pydata(verts, [], faces); me.materials.append(m)
uv = me.uv_layers.new(name="UV")
for lp in me.loops:
    uv.data[lp.index].uv = uvs[lp.vertex_index]
imp = bpy.data.objects.new("imp", me); sc.collection.objects.link(imp)
print("[impostor] baked", ATLAS, "tris", len(faces) * 2)
sc.render.resolution_x = sc.render.resolution_y = 1100
objs = {0: l0, 2: l2, 9: imp}
S.hide(l0, False)
for lv, o in objs.items():
    for j, p in objs.items():
        S.hide(p, j != lv)
    for d in (100, 140, 200, 250, 300, 350):
        for az in (0, 30, 60, 90):
            S.shoot_persp(OUT / f"{NAME}_L{lv}_d{d}_az{az}.png", d, (1100, 1100), 5.5, cd, co, az=az)
print("[impostor] done")
