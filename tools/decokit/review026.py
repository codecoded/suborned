"""Brief 026 round 1 review renders (Blender EEVEE, no Unity): the ground roles and the street props at play zoom (ortho 14, pitch 38, yaw 30 / 210) and street level.

blender -b -P tools/decokit/review026.py -- <out_dir> [--old]    --old draws the ground from the old city's full-size derived sheets (the 'before'), else the new budgeted set.
Scene: a street slice (pavement and a seafoam terrazzo band | granite kerb | sett gutter | asphalt with a lane edge), a lawn and a park path, then the props on the pavement. Not the game's
lighting: one warm sun, a pale sky; it compares materials and shapes with the same light on both sides, nothing more."""

import math
import os
import sys
from pathlib import Path

import bpy
from mathutils import Euler, Vector

sys.path.insert(0, str(Path(__file__).parent))
import kit  # noqa: E402
import mats  # noqa: E402

ARGS = sys.argv[sys.argv.index("--") + 1:]
OUT = Path(ARGS[0])
OLD = "--old" in ARGS
if "--old-enamel" in ARGS:  # the colours before brief 007's retune (a gate that is gone): Enamel_Red #C42A1F, Enamel_Mustard #EEA82D
    mats.LIB["DK_Enamel_Red"] = ("enamel", "#C42A1F")
    mats.LIB["DK_Enamel_Mustard"] = ("enamel", "#EEA82D")
TAG = "oldenamel-" if "--old-enamel" in ARGS else ""
PROPS = [a for a in (ARGS[ARGS.index("--props") + 1].split(",") if "--props" in ARGS else []) if a]
OUT.mkdir(parents=True, exist_ok=True)
DER = mats.SOURCE_ART / "Textures/derived"


def old_mat(name, stem, tile, mask=True, tile_y=None):
    """The old sheets at full size, untouched (the before)."""
    m = bpy.data.materials.new(name)
    nt = m.node_tree
    b = nt.nodes["Principled BSDF"]
    tc, mp = nt.nodes.new("ShaderNodeTexCoord"), nt.nodes.new("ShaderNodeMapping")
    mp.inputs["Scale"].default_value = (1 / tile, 1 / (tile_y or tile), 1)
    nt.links.new(tc.outputs["UV"], mp.inputs["Vector"])

    def tex(file, nc=False):
        t = nt.nodes.new("ShaderNodeTexImage")
        t.image = bpy.data.images.load(str(DER / file), check_existing=True)
        t.image.alpha_mode = "CHANNEL_PACKED"
        if nc:
            t.image.colorspace_settings.name = "Non-Color"
        nt.links.new(mp.outputs["Vector"], t.inputs["Vector"])
        return t
    a = tex(f"{stem}.png")
    nt.links.new(a.outputs["Color"], b.inputs["Base Color"])
    if (DER / f"{stem}_normal.png").exists():
        n = tex(f"{stem}_normal.png", True)
        nm = nt.nodes.new("ShaderNodeNormalMap")
        nt.links.new(n.outputs["Color"], nm.inputs["Color"])
        nt.links.new(nm.outputs["Normal"], b.inputs["Normal"])
    if mask and (DER / f"{stem}_mask.png").exists():
        k = tex(f"{stem}_mask.png", True)
        inv = nt.nodes.new("ShaderNodeMath")
        inv.operation, inv.inputs[0].default_value = "SUBTRACT", 1.0
        nt.links.new(k.outputs["Alpha"], inv.inputs[1])
        nt.links.new(inv.outputs["Value"], b.inputs["Roughness"])
    else:
        b.inputs["Roughness"].default_value = .75
    return m


def quad(name, x0, y0, x1, y1, z, mat, uvmode="world", sx=1.0, sy=1.0):
    me = bpy.data.meshes.new(name)
    me.from_pydata([(x0, y0, z), (x1, y0, z), (x1, y1, z), (x0, y1, z)], [], [(0, 1, 2, 3)])
    uvl = me.uv_layers.new()
    pts = [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]
    for i, (x, y) in enumerate(pts):
        uvl.data[i].uv = (x, y) if uvmode == "world" else (y, x - x0)  # strip: u along the kerb (y), v across (x from the strip's inner edge)
    o = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(o)
    me.materials.append(mat)
    return o


def build_ground():
    g = (lambda n: mats.get(n)) if not OLD else None
    if OLD:
        flags = old_mat("o_flags", "pave_flags_v1", 4.0)
        kerb = old_mat("o_kerb", "granite_kerb_v1", 0.5)
        gut = old_mat("o_gut", "gutter_setts_v1", 4.0, tile_y=0.375)
        asph = old_mat("o_asph", "asphalt_road_v1", 16.0, mask=False)
        lawn = old_mat("o_lawn", "lawn_v1", 8.0, mask=False)
        path = flags
        terr = old_mat("o_terr", "terrazzo_seafoam_v1", 1.5, mask=False)
    else:
        flags, kerb, gut = g("DK_Ground_Pavement"), g("DK_Ground_Kerb"), g("DK_Ground_Gutter")
        asph, lawn, path = g("DK_Ground_Asphalt"), g("DK_Ground_Lawn"), g("DK_Ground_Path")
        terr = g("DK_Terrazzo_Seafoam")
    # x: pavement -20..0, kerb 0..0.3 (raised 0.15), gutter -> road 0.3..0.675, asphalt 0.675..30; y along the street -30..30
    quad("pave", -24, -30, -4, 30, 0.15, flags)
    quad("band", -4, -30, -2.4, 30, 0.152, terr)
    quad("pave2", -2.4, -30, -0.0, 30, 0.15, flags)
    quad("kerbtop", -0.0, -30, 0.3, 30, 0.152, kerb)
    quad("gutter", 0.3, -30, 0.675, 30, 0.003, gut, uvmode="strip")
    quad("road", 0.675, -30, 30, 30, 0.0, asph)
    # kerb face
    me = bpy.data.meshes.new("kerbface")
    me.from_pydata([(0.3, -30, 0.0), (0.3, 30, 0.0), (0.3, 30, 0.15), (0.3, -30, 0.15)], [], [(0, 1, 2, 3)])
    uvl = me.uv_layers.new()
    for i, (y, z) in enumerate([(-30, 0), (30, 0), (30, .15), (-30, .15)]):
        uvl.data[i].uv = (y, z)
    o = bpy.data.objects.new("kerbface", me)
    bpy.context.scene.collection.objects.link(o)
    me.materials.append(kerb)
    # lawn and path on the far side of the pavement
    quad("lawn", -44, -30, -24, 30, 0.1, lawn)
    quad("path", -33, -30, -31, 30, 0.105, path)
    return


def place(name, x, y, rot=0.0):
    o = kit.build(name)
    o.location = (x, y, 0.15)
    o.rotation_euler = Euler((0, 0, math.radians(rot)))
    bpy.context.scene.collection.objects.link(o) if o.name not in bpy.context.scene.collection.objects else None
    for c in o.children_recursive:
        if c.name not in bpy.context.scene.collection.objects:
            bpy.context.scene.collection.objects.link(c)
    return o


def setup_render(res=(1920, 1080)):
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE"
    sc.render.resolution_x, sc.render.resolution_y = res
    sc.render.film_transparent = False
    sc.view_settings.view_transform = "Standard"
    sc.view_settings.look = "None"
    sc.view_settings.exposure = -0.3
    w = bpy.data.worlds.new("w")
    w.use_nodes = True
    bg = w.node_tree.nodes["Background"]
    bg.inputs[0].default_value = (.62, .78, .92, 1)
    bg.inputs[1].default_value = 1.1
    sc.world = w
    sun = bpy.data.lights.new("sun", "SUN")
    sun.energy, sun.angle, sun.color = 3.2, math.radians(2), (1.0, .93, .80)
    so = bpy.data.objects.new("sun", sun)
    so.rotation_euler = Euler((math.radians(52), 0, math.radians(-40)))
    sc.collection.objects.link(so)


def camera(kind, target, yaw=30.0):
    cam = bpy.data.cameras.new("cam")
    co = bpy.data.objects.new("cam", cam)
    bpy.context.scene.collection.objects.link(co)
    bpy.context.scene.camera = co
    t = Vector(target)
    if kind == "play":  # Unity: orthographic size 14 = half height; pitch 38; yaw 30 / 210 about up
        cam.type, cam.ortho_scale = "ORTHO", 28 * 16 / 9
        pitch = math.radians(38)
        d = 60
        off = Vector((math.sin(math.radians(yaw)) * math.cos(pitch), -math.cos(math.radians(yaw)) * math.cos(pitch), math.sin(pitch))) * d
        co.location = t + off
        co.rotation_euler = (t - co.location).to_track_quat("-Z", "Y").to_euler()
        cam.clip_end = 300
    elif kind == "close":  # the same pitch, a tight frame for a prop
        cam.type, cam.ortho_scale = "ORTHO", 2 * 2.6 * 16 / 9
        pitch = math.radians(38)
        off = Vector((math.sin(math.radians(yaw)) * math.cos(pitch), -math.cos(math.radians(yaw)) * math.cos(pitch), math.sin(pitch))) * 30
        co.location = t + off
        co.rotation_euler = (t - co.location).to_track_quat("-Z", "Y").to_euler()
        cam.clip_end = 200
    else:  # street level: a person's eye
        cam.type, cam.lens = "PERSP", 28
        co.location = Vector(target) + Vector((float(kind.split(":")[1]) if ":" in kind else 5, -float(kind.split(":")[2]) if kind.count(":") > 1 else 7, 1.45))
        co.rotation_euler = (t + Vector((0, 0, 0.9)) - co.location).to_track_quat("-Z", "Y").to_euler()
        cam.clip_end = 200


def shoot(name):
    bpy.context.scene.render.filepath = str(OUT / f"{name}.png")
    bpy.ops.render.render(write_still=True)
    print("[review026] wrote", OUT / f"{name}.png")


def each():
    for n in PROPS:
        o = place(n, -8, 0, rot=0)
        for k in [o, *o.children_recursive]:
            k.location.z = 0.15
        for c in [c for c in bpy.data.objects if c.type == "CAMERA"]:
            bpy.data.objects.remove(c)
        h = max(o.dimensions.z, 0.6)
        camera("close", (-8, 0, 0.15 + h / 2), 30 if "--yaw" not in ARGS else float(ARGS[ARGS.index("--yaw") + 1]))
        bpy.context.scene.camera.data.ortho_scale = max(h * 1.5, o.dimensions.x * 1.6, 1.4) * 16 / 9 if h > 0 else 4
        shoot(f"{TAG}each-{n}")
        for k in [o, *o.children_recursive]:
            bpy.data.objects.remove(k)


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    kit.load()
    setup_render()
    build_ground()
    if "--each" in ARGS:
        each()
        return
    if "--ground-only" not in ARGS:
        for i, n in enumerate(PROPS):
            place(n, -8 - (i % 4) * 2.6, -6 + (i // 4) * 3.0, rot=0)
    cam_target = (-8, 0, 0)
    for k in ("play30:30", "play210:210"):
        pass
    for label, kind, tgt, yaw in (("play-30", "play", (-6, 0, 0), 30), ("play-210", "play", (-6, 0, 0), 210), ("street-pave", "street:4:6", (-3, 0, 0), 0), ("street-kerb", "street:-6:5", (0.3, 2, 0), 0)):
        for o in [o for o in bpy.data.objects if o.type == "CAMERA"]:
            bpy.data.objects.remove(o)
        camera(kind, tgt, yaw)
        shoot(("old-" if OLD else "new-") + label)


main()
