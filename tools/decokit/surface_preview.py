"""Brief 072-F preview (Vela): the same street corner with today's flat colours and with surface_tex.py's colourless detail laid over them, at the play camera
(orthographic, 38 degrees, yaw 30, zoom 14 at 1080p: 49.8 m across 1920 px) and a 2x crop. Colours are facade.json's swatches and the kit's hexes, unchanged:
texture, not palette. Not the game's light or shader (FacadeLit's procedural ashlar joints and glass are not drawn here); it shows what the detail adds.

blender -b -P tools/decokit/surface_preview.py -- <surface_detail_v1.png> <out_dir>"""
import math
import sys
from pathlib import Path

import bpy
import numpy as np
from mathutils import Vector

A = sys.argv[sys.argv.index("--") + 1:]
ARRAY, OUT = Path(A[0]), Path(A[1])
OUT.mkdir(parents=True, exist_ok=True)
N = 1024
LAYER = {"render": (0, 3.0), "limestone": (1, 2.0), "enamel": (2, 1.0), "metal": (3, 0.5), "roof": (4, 1.5), "panel": (5, 2.0)}
# swatch -> (hex, roughness, metallic, layer or None, strength); facade.json swatches and DecoKit hexes as they are today
SURF = {
    "wall": ("#efe3c8", .86, 0, "limestone", 1), "side": ("#f3d2b8", .88, 0, "render", 1), "far": ("#e5d2b0", .88, 0, "panel", 1),
    "band": ("#5fb8b0", .22, 0, "enamel", 1), "brass": ("#99784d", .5, .3, "metal", 1), "chrome": ("#dfe3e5", .18, 1, "metal", .35),
    "roof": ("#ebd9b4", .8, 0, "roof", 1), "roof2": ("#faf6ea", .8, 0, "roof", 1), "glass": ("#24484a", .1, 0, None, 0),
    "kiosk": ("#1F6C67", .3, 0, "enamel", 1), "ground": ("#d5cbb8", .8, 0, None, 0),
}


def srgb(h):
    return tuple((int(h[i:i + 2], 16) / 255) for i in (1, 3, 5))


def lin(c):
    return c / 12.92 if c <= .04045 else ((c + .055) / 1.055) ** 2.4


def layer_images():
    """The array's layers as Blender images (R albedo x2, G height, B smoothness), non-colour."""
    src = bpy.data.images.load(str(ARRAY))
    px = np.array(src.pixels[:], dtype=np.float32).reshape(src.size[1], src.size[0], 4)  # bottom row first
    out = {}
    rows = src.size[1] // N
    for name, (i, _) in LAYER.items():
        top = rows - 1 - i  # Blender's rows are bottom-up; layer 0 is the top of the PNG
        im = bpy.data.images.new(f"L_{name}", N, N, alpha=True, float_buffer=True)
        im.colorspace_settings.name = "Non-Color"
        im.pixels = px[top * N:(top + 1) * N].ravel()
        out[name] = im
    return out


def material(key, detail, imgs):
    hexs, rough, metal, lay, k = SURF[key]
    m = bpy.data.materials.new(f"{key}_{'after' if detail else 'before'}")
    m.use_nodes = True
    nt = m.node_tree
    b = nt.nodes["Principled BSDF"]
    col = tuple(lin(c) for c in srgb(hexs)) + (1,)
    b.inputs["Base Color"].default_value = col
    b.inputs["Roughness"].default_value = rough
    b.inputs["Metallic"].default_value = metal
    if not detail or lay is None:
        return m
    idx, tile = LAYER[lay]
    tc = nt.nodes.new("ShaderNodeTexCoord")
    mp = nt.nodes.new("ShaderNodeMapping")
    mp.inputs["Scale"].default_value = (1 / tile,) * 3
    nt.links.new(tc.outputs["Object"], mp.inputs["Vector"])
    tx = nt.nodes.new("ShaderNodeTexImage")
    tx.image, tx.projection, tx.projection_blend, tx.interpolation = imgs[lay], "BOX", .15, "Cubic"
    nt.links.new(mp.outputs["Vector"], tx.inputs["Vector"])
    sep = nt.nodes.new("ShaderNodeSeparateColor")
    nt.links.new(tx.outputs["Color"], sep.inputs["Color"])
    # albedo: colour x lerp(1, 2R, k)  (URP's x2 detail; FacadeLit would scale by the class strength)
    mul = nt.nodes.new("ShaderNodeMath"); mul.operation = "MULTIPLY"; mul.inputs[1].default_value = 2.0
    nt.links.new(sep.outputs["Red"], mul.inputs[0])
    mix = nt.nodes.new("ShaderNodeMix"); mix.data_type = "FLOAT"; mix.inputs["Factor"].default_value = k; mix.inputs["A"].default_value = 1.0
    nt.links.new(mul.outputs[0], mix.inputs["B"])
    cm = nt.nodes.new("ShaderNodeMix"); cm.data_type = "RGBA"; cm.blend_type = "MULTIPLY"; cm.inputs["Factor"].default_value = 1.0
    cm.inputs["A"].default_value = col
    comb = nt.nodes.new("ShaderNodeCombineColor")
    for ch in ("Red", "Green", "Blue"):
        nt.links.new(mix.outputs["Result"], comb.inputs[ch])
    nt.links.new(comb.outputs["Color"], cm.inputs["B"])
    nt.links.new(cm.outputs["Result"], b.inputs["Base Color"])
    # roughness: 1 - (smoothness + (B - 0.5) * k)
    sub = nt.nodes.new("ShaderNodeMath"); sub.operation = "MULTIPLY_ADD"; sub.inputs[1].default_value = -k; sub.inputs[2].default_value = rough + .5 * k
    nt.links.new(sep.outputs["Blue"], sub.inputs[0]); sub.use_clamp = True
    nt.links.new(sub.outputs[0], b.inputs["Roughness"])
    bump = nt.nodes.new("ShaderNodeBump"); bump.inputs["Strength"].default_value = .35 * k; bump.inputs["Distance"].default_value = .02
    nt.links.new(sep.outputs["Green"], bump.inputs["Height"])
    nt.links.new(bump.outputs["Normal"], b.inputs["Normal"])
    return m


def box(name, mat, x0, x1, y0, y1, z0, z1):
    bpy.ops.mesh.primitive_cube_add(size=1, location=((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2))
    o = bpy.context.object
    o.name = name
    o.scale = (x1 - x0, y1 - y0, z1 - z0)
    bpy.ops.object.transform_apply(location=True, scale=True)  # object coordinates = world metres, so the box projection tiles by the metre
    o.data.materials.append(mat)
    return o


def cyl(name, mat, x0, x1, y, z, r):
    bpy.ops.mesh.primitive_cylinder_add(radius=r, depth=x1 - x0, location=((x0 + x1) / 2, y, z), rotation=(0, math.pi / 2, 0), vertices=16)
    o = bpy.context.object
    bpy.ops.object.transform_apply(location=True, rotation=True)
    o.data.materials.append(mat)
    return o


def scene(detail, imgs):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    imgs = layer_images()
    M = {k: material(k, detail, imgs) for k in SURF}
    box("ground", M["ground"], -30, 30, -30, 30, -0.1, 0)
    # the street building: 3 storeys of 3.8 m, front at y = 0 facing -y (toward the camera at yaw 30), 14 m wide, 10 m deep
    box("wall", M["wall"], -7, 7, 0, 10, 0, 12.4)
    for f in range(3):
        z = f * 3.8
        box(f"band{f}", M["band"], -7.05, 7.05, -0.12, 0.0, z + 3.2, z + 3.8)
        box(f"brass{f}", M["brass"], -7.05, 7.05, -0.15, 0.0, z + 3.12, z + 3.16)
        for w in range(4):
            x = -5.4 + w * 3.6
            box(f"win{f}{w}", M["glass"], x - 1.0, x + 1.0, -0.02, 0.2, z + 0.9, z + 2.8)
    box("roof", M["roof"], -6.9, 6.9, 0.1, 9.9, 12.4, 12.45)
    box("parapet", M["wall"], -7, 7, 0, 0.25, 12.4, 13.2)
    cyl("rail", M["chrome"], -7, 7, -0.05, 13.35, 0.04)
    box("side", M["side"], 7, 7.02, 0, 10, 0, 12.4)  # a matte (render) side face, as the procedural flank
    # the next building: a far-LOD style panel wall, lower, set back
    box("far", M["far"], 8, 20, 2, 12, 0, 9.4)
    box("roof2", M["roof2"], 8.1, 19.9, 2.1, 11.9, 9.4, 9.45)
    # a kit kiosk (enamel + chrome trim) on the pavement
    box("kiosk", M["kiosk"], -2, 0.4, -4.2, -2.6, 0, 2.3)
    box("kioskcap", M["chrome"], -2.1, 0.5, -4.3, -2.5, 2.3, 2.42)
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy, sun.data.color, sun.data.angle = 3.0, (1.0, .93, .82), math.radians(2)
    sun.rotation_euler = (math.radians(50), 0, math.radians(-35))
    bpy.context.scene.collection.objects.link(sun)
    w = bpy.data.worlds.new("w"); bpy.context.scene.world = w; w.use_nodes = True
    w.node_tree.nodes["Background"].inputs["Color"].default_value = (.55, .68, .82, 1)
    w.node_tree.nodes["Background"].inputs["Strength"].default_value = .6
    cd = bpy.data.cameras.new("c"); co = bpy.data.objects.new("c", cd); bpy.context.scene.collection.objects.link(co); bpy.context.scene.camera = co
    cd.type, cd.clip_end = "ORTHO", 400
    t = Vector((2.5, 0, 6.0)); p, yaw = math.radians(38), math.radians(30)
    co.location = t + Vector((math.sin(yaw) * math.cos(p), -math.cos(yaw) * math.cos(p), math.sin(p))) * 80
    co.rotation_euler = (t - co.location).to_track_quat("-Z", "Y").to_euler()
    s = bpy.context.scene
    s.render.engine = "BLENDER_EEVEE"
    s.eevee.taa_render_samples = 16
    s.view_settings.view_transform = "AgX"  # a tone curve, so lit cream does not clip to white and hide the detail
    s.render.image_settings.file_format = "PNG"
    tag = "after" if detail else "before"
    for name, px, scale in (("play", (1280, 720), 1), ("x2", (960, 540), 2)):
        s.render.resolution_x, s.render.resolution_y, s.render.resolution_percentage = px[0], px[1], 100
        cd.ortho_scale = 49.78 * px[0] / 1920 / scale  # zoom 14 at 1080p: 28 m tall, 49.8 m across 1920 px
        s.render.filepath = str(OUT / f"preview_{name}_{tag}.png")
        bpy.ops.render.render(write_still=True)


for d in (False, True):
    scene(d, None)
