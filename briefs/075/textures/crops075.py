"""Brief 075-B before/after crops (Vela): each ported surface on flat patches at the play camera (orthographic, 38 degrees down, yaw 30, zoom 14 at 1080p = 49.8 m across
1920 px), cropped to 640 x 640 px (16.6 m). Not the game's light or shader: Blender EEVEE, a warm sun and a plain sky, the same for before and after; it shows what the
texture and material response change, like for like. UVs are world metres; each map at its .mat's metres per repeat.

blender -b -P briefs/075/textures/crops075.py -- <before_dir> <ground_dir> <store_road_dir> <out_dir>
  before_dir      copies of the ground sheets as they were (ground_roadenamel_v1.png, ground_pavement_v1.png, ground_lawn_v1.png)
  ground_dir      out/decokit-ground (today's set)
  store_road_dir  ~/Projects/the-suborned-store/testbed-art/district-road (the enamel's own normal and orm, no longer made)
"""
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

A = sys.argv[sys.argv.index("--") + 1:]
BEFORE, GROUND, ROAD, OUT = (Path(a) for a in A[:4])
OUT.mkdir(parents=True, exist_ok=True)
PX, M_ACROSS = 640, 49.8 * 640 / 1920


def srgb(h):
    return tuple(((int(h[i:i + 2], 16) / 255 + .055) / 1.055) ** 2.4 if int(h[i:i + 2], 16) / 255 > .04045 else int(h[i:i + 2], 16) / 255 / 12.92 for i in (1, 3, 5))


def img(p, colour=True):
    im = bpy.data.images.load(str(p), check_existing=True)
    im.colorspace_settings.name = "sRGB" if colour else "Non-Color"
    im.alpha_mode = "CHANNEL_PACKED"
    return im


def ground_mat(name, albedo, tile, normal=None, ntile=None, mask=None, smooth=1.0, flat_smooth=None, detail=None, dnormal=None, dtile=1.5, dbump=.45, bump=1.0):
    """URP Lit as the .mat draws it: albedo x (detail x2), normal (+ detail normal), roughness = 1 - mask.a x smoothness (or the flat smoothness)."""
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    b = nt.nodes["Principled BSDF"]
    tc = nt.nodes.new("ShaderNodeTexCoord")

    def tex(image, t):
        mp = nt.nodes.new("ShaderNodeMapping")
        mp.inputs["Scale"].default_value = (1 / t, 1 / t, 1)
        nt.links.new(tc.outputs["UV"], mp.inputs["Vector"])
        n = nt.nodes.new("ShaderNodeTexImage")
        n.image = image
        nt.links.new(mp.outputs["Vector"], n.inputs["Vector"])
        return n

    a = tex(img(albedo), tile)
    col = a.outputs["Color"]
    if detail:
        d = tex(img(detail, False), dtile)
        mul = nt.nodes.new("ShaderNodeMixRGB")
        mul.blend_type, mul.inputs["Fac"].default_value = "MULTIPLY", 1.0
        x2 = nt.nodes.new("ShaderNodeVectorMath")
        x2.operation = "SCALE"
        x2.inputs["Scale"].default_value = 2.0
        nt.links.new(d.outputs["Color"], x2.inputs[0])
        nt.links.new(col, mul.inputs[1])
        nt.links.new(x2.outputs["Vector"], mul.inputs[2])
        col = mul.outputs["Color"]
    nt.links.new(col, b.inputs["Base Color"])
    if normal:
        n = tex(img(normal, False), ntile or tile)
        nm = nt.nodes.new("ShaderNodeNormalMap")
        nm.inputs["Strength"].default_value = bump
        nt.links.new(n.outputs["Color"], nm.inputs["Color"])
        if dnormal:
            dn = tex(img(dnormal, False), dtile)
            dm = nt.nodes.new("ShaderNodeNormalMap")
            dm.inputs["Strength"].default_value = dbump
            nt.links.new(dn.outputs["Color"], dm.inputs["Color"])
            nt.links.new(nm.outputs["Normal"], dm.inputs["Normal"]) if "Normal" in dm.inputs else None
            nt.links.new(dm.outputs["Normal"], b.inputs["Normal"])
        else:
            nt.links.new(nm.outputs["Normal"], b.inputs["Normal"])
    if mask:
        k = tex(img(mask, False), ntile or tile)
        sm = nt.nodes.new("ShaderNodeMath")
        sm.operation, sm.inputs[1].default_value = "MULTIPLY", smooth
        nt.links.new(k.outputs["Alpha"], sm.inputs[0])
        inv = nt.nodes.new("ShaderNodeMath")
        inv.operation, inv.inputs[0].default_value = "SUBTRACT", 1.0
        nt.links.new(sm.outputs["Value"], inv.inputs[1])
        nt.links.new(inv.outputs["Value"], b.inputs["Roughness"])
    else:
        b.inputs["Roughness"].default_value = 1 - (flat_smooth if flat_smooth is not None else .5)
    b.inputs["Specular IOR Level"].default_value = .5
    return m


def film_mat(name, glaze, alpha, horizon, zenith, street="#A78F78", fresnel=3.0, edge=.92):
    """A stand-in for Suborned/DecoKit/GlassSky (the window film): glaze over the 1.1 m charcoal cavity at the film's alpha, plus a fake sky by the reflected
    direction's height (street below the horizon, horizon to zenith above), weighted by a Schlick-like fresnel of power `fresnel`. Emission only, as the shader draws no probe."""
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.remove(nt.nodes["Principled BSDF"])
    out = nt.nodes["Material Output"]
    geo = nt.nodes.new("ShaderNodeNewGeometry")
    refl = nt.nodes.new("ShaderNodeVectorMath")
    refl.operation = "REFLECT"
    neg = nt.nodes.new("ShaderNodeVectorMath")
    neg.operation = "SCALE"
    neg.inputs["Scale"].default_value = -1
    nt.links.new(geo.outputs["Incoming"], neg.inputs[0])
    nt.links.new(neg.outputs["Vector"], refl.inputs[0])
    nt.links.new(geo.outputs["Normal"], refl.inputs[1])
    sep = nt.nodes.new("ShaderNodeSeparateXYZ")
    nt.links.new(refl.outputs["Vector"], sep.inputs["Vector"])
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    r = ramp.color_ramp
    r.elements[0].position, r.elements[0].color = .40, (*srgb(street), 1)
    r.elements[1].position, r.elements[1].color = 1.0, (*srgb(zenith), 1)
    e = r.elements.new(.5)
    e.color = (*srgb(horizon), 1)
    mapr = nt.nodes.new("ShaderNodeMapRange")
    mapr.inputs["From Min"].default_value, mapr.inputs["From Max"].default_value = -1, 1
    nt.links.new(sep.outputs["Z"], mapr.inputs["Value"])
    nt.links.new(mapr.outputs["Result"], ramp.inputs["Fac"])
    lw = nt.nodes.new("ShaderNodeLayerWeight")
    lw.inputs["Blend"].default_value = 1 - edge
    pw = nt.nodes.new("ShaderNodeMath")
    pw.operation, pw.inputs[1].default_value = "POWER", fresnel / 3.0
    nt.links.new(lw.outputs["Facing"], pw.inputs[0])
    gl = tuple(c * alpha + .035 * (1 - alpha) for c in srgb(glaze))
    mix = nt.nodes.new("ShaderNodeMixRGB")
    mix.inputs["Color1"].default_value = (*gl, 1)
    nt.links.new(pw.outputs["Value"], mix.inputs["Fac"])
    nt.links.new(ramp.outputs["Color"], mix.inputs["Color2"])
    em = nt.nodes.new("ShaderNodeEmission")
    nt.links.new(mix.outputs["Color"], em.inputs["Color"])
    nt.links.new(em.outputs["Emission"], out.inputs["Surface"])
    return m


def patch(name, mat, w, d, x=0.0, y=0.0, z=0.0, vertical=False):
    bpy.ops.mesh.primitive_plane_add(size=1)
    o = bpy.context.object
    o.name = name
    o.scale = (w, d, 1)
    o.location = (x, y, z)
    if vertical:
        o.rotation_euler = (math.radians(90), 0, math.radians(-30))  # faces the camera (yaw 30)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    uv = o.data.uv_layers.active.data
    for poly in o.data.polygons:
        for li in poly.loop_indices:
            v = o.data.vertices[o.data.loops[li].vertex_index].co
            uv[li].uv = (v.x + x, v.y + y)
    o.data.materials.append(mat)
    return o


def scene():
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o)
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE"
    sc.render.resolution_x = sc.render.resolution_y = PX
    sc.view_settings.view_transform = "AgX" if "AgX" in [i.name for i in type(sc.view_settings).bl_rna.properties["view_transform"].enum_items] else "Filmic"
    sc.view_settings.look = "None"
    w = bpy.data.worlds.new("w") if not sc.world else sc.world
    sc.world = w
    w.use_nodes = True
    w.node_tree.nodes["Background"].inputs["Color"].default_value = (*srgb("#97B8D1"), 1)
    w.node_tree.nodes["Background"].inputs["Strength"].default_value = .9
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy, sun.data.color = 3.2, srgb("#FFE2B8")
    sun.data.angle = math.radians(2)
    sun.rotation_euler = (math.radians(48), 0, math.radians(-35))
    sc.collection.objects.link(sun)
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    cam.data.type, cam.data.ortho_scale = "ORTHO", M_ACROSS
    pitch, yaw = math.radians(38), math.radians(30)
    fwd = Vector((math.sin(yaw) * math.cos(pitch), math.cos(yaw) * math.cos(pitch), -math.sin(pitch)))
    cam.location = -fwd * 80
    cam.rotation_euler = fwd.to_track_quat("-Z", "Y").to_euler()
    cam.data.clip_end = 400
    sc.collection.objects.link(cam)
    sc.camera = cam


def shot(name):
    bpy.context.scene.render.filepath = str(OUT / f"{name}.png")
    bpy.ops.render.render(write_still=True)


G = GROUND
BEFORE_SET = {
    "road": dict(albedo=BEFORE / "ground_roadenamel_v1.png", tile=4.0, normal=ROAD / "enamel-normal-v1.webp", mask=None, flat_smooth=.34),  # 2 m sheet x look tile 2 = 4 m; mask ~.62 x .55
    "pavement": dict(albedo=BEFORE / "ground_pavement_v1.png", tile=4.0, normal=G / "ground_pavement_v1_normal.png", mask=G / "ground_pavement_v1_mask.png"),
    "lawn": dict(albedo=BEFORE / "ground_lawn_v1.png", tile=8.0, normal=G / "ground_lawn_v1_normal.png", flat_smooth=.2),
}
AFTER_SET = {
    "road": dict(albedo=G / "ground_roadenamel_v1.png", tile=16.0, normal=G / "ground_asphalt_v1_normal.png", mask=G / "ground_asphalt_v1_mask.png", smooth=1.4,
                 detail=G / "ground_asphalt_detail_v1.png", dnormal=G / "ground_asphalt_detail_v1_normal.png"),
    "pavement": dict(albedo=G / "ground_pavement_v1.png", tile=4.0, normal=G / "ground_pavement_v1_normal.png", mask=G / "ground_pavement_v1_mask.png"),
    "lawn": dict(albedo=G / "ground_lawn_v1.png", tile=8.0, normal=G / "ground_lawn_v1_normal.png", flat_smooth=.12),
}
FILM = {"before": dict(glaze="#3F7A8E", alpha=.65, horizon="#D8E6E4", zenith="#86B8E0"), "after": dict(glaze="#3F7A8E", alpha=.65, horizon="#D1DBDD", zenith="#75A4D6")}

for when, sets in (("before", BEFORE_SET), ("after", AFTER_SET)):
    mats = {k: ground_mat(f"{k}-{when}", **v) for k, v in sets.items()}
    for k in ("road", "pavement", "lawn"):
        scene()
        patch(k, mats[k], 60, 60)
        shot(f"{k}-{when}")
    # a street slice: pavement | kerb line | carriageway | lawn median | carriageway | pavement, across the view
    scene()
    kerb = bpy.data.materials.new("kerb")
    kerb.use_nodes = True
    kerb.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (*srgb("#F1EEE6"), 1)
    for nm_, mat, w, x in (("pave-l", mats["pavement"], 8, -13), ("kerb-l", kerb, .3, -8.85), ("road-l", mats["road"], 7, -5.2), ("median", mats["lawn"], 3.4, 0),
                           ("road-r", mats["road"], 7, 5.2), ("kerb-r", kerb, .3, 8.85), ("pave-r", mats["pavement"], 8, 13)):
        patch(nm_, mat, w, 80, x=x, z=.15 if nm_.startswith(("pave", "kerb")) else (.1 if nm_ == "median" else 0))
    shot(f"street-{when}")
    scene()
    patch("film", film_mat(f"film-{when}", **FILM[when]), 14, 10, vertical=True)
    shot(f"glass-film-{when}")
print("[crops075] ->", OUT)
