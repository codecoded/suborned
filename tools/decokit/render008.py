"""Brief 008 check renders: each piece as a studio thumbnail, a play-angle view (orthographic, pitch 38, yaw 30 off the
piece's front, as the Handover camera sees a south-facing front) and an eye-level view (1.7 m, 40 mm). Cycles, shadow-catcher
ground, the DecoKit studio HDRI. Blender -b -P render008.py -- --ids A,B --dir out/review/008-renders [--px 560] [--samples 40]
Builds through kit.build (the exporter's own path, bevel and all), so what is rendered is what is exported (before LODs)."""

import math
import sys
from pathlib import Path

import os

import bpy
from mathutils import Vector

REPO = Path(__file__).resolve().parents[2]
os.environ.setdefault("DECOKIT_TEXDIRS_EXTRA", str(REPO / "out/decokit-008-src/Textures/derived"))  # candidate textures (paving)
sys.path.insert(0, str(Path(__file__).parent))
import kit  # noqa: E402

ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []


def arg(key, default):
    return type(default)(ARGS[ARGS.index(key) + 1]) if key in ARGS else default


HDRI = Path(arg("--hdri", str(REPO / "out/decokit-008/HDRI/studio.exr")))


def studio(px, samples):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    try:
        prefs = bpy.context.preferences.addons["cycles"].preferences
        prefs.compute_device_type = "METAL"
        prefs.get_devices()
        for d in prefs.devices:
            d.use = True
        sc.cycles.device = "GPU"
    except Exception as e:  # noqa: BLE001
        print("[R8] GPU setup failed:", e)
    sc.cycles.samples, sc.cycles.use_denoising = samples, True
    sc.render.film_transparent = False
    sc.view_settings.view_transform, sc.view_settings.look = "AgX", "AgX - Punchy"
    sc.view_settings.exposure = arg("--exposure", 0.35)
    w = bpy.data.worlds.new("R8")
    sc.world = w
    nt = w.node_tree
    env, mp, tc = nt.nodes.new("ShaderNodeTexEnvironment"), nt.nodes.new("ShaderNodeMapping"), nt.nodes.new("ShaderNodeTexCoord")
    env.image = bpy.data.images.load(str(HDRI))
    nt.links.new(tc.outputs["Generated"], mp.inputs["Vector"])
    nt.links.new(mp.outputs["Vector"], env.inputs["Vector"])
    mp.inputs["Rotation"].default_value[2] = math.radians(arg("--hdri-rot", 20.0))
    nt.links.new(env.outputs["Color"], nt.nodes["Background"].inputs["Color"])
    nt.nodes["Background"].inputs["Strength"].default_value = arg("--hdri-strength", 1.0)
    bpy.ops.mesh.primitive_plane_add(size=400)
    ground = bpy.context.object
    gm = bpy.data.materials.new("Ground")
    gb = gm.node_tree.nodes["Principled BSDF"]
    gb.inputs["Base Color"].default_value = (0.74, 0.69, 0.58, 1)  # warm pavement, linear
    gb.inputs["Roughness"].default_value = 0.8
    ground.data.materials.append(gm)
    sun = bpy.data.lights.new("Sun", "SUN")
    sun.energy, sun.angle, sun.color = 4.0, math.radians(4), (1.0, 0.93, 0.8)
    so = bpy.data.objects.new("Sun", sun)
    sc.collection.objects.link(so)
    so.rotation_euler = (math.radians(52), 0, math.radians(-38))  # from front-left, high-ish: long soft shadows to the right
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    sc.collection.objects.link(cam)
    sc.camera = cam
    sc.render.resolution_x, sc.render.resolution_y = px, int(px * 0.72)
    return cam, ground


def bounds(obj):
    dg = bpy.context.evaluated_depsgraph_get()
    pts = []
    for o in [obj, *obj.children_recursive]:
        ev = o.evaluated_get(dg)
        pts += [o.matrix_world @ Vector(c) for c in ev.bound_box]
    lo = Vector([min(p[i] for p in pts) for i in range(3)])
    hi = Vector([max(p[i] for p in pts) for i in range(3)])
    return lo, hi


def look(cam, pos, target):
    cam.location = pos
    cam.rotation_euler = (Vector(target) - Vector(pos)).to_track_quat("-Z", "Y").to_euler()


EYE = {"CafeBay": dict(dist=9.5, dx=-3.0, tz=2.4), "RecordsEntrance": dict(dist=9.0, dx=-0.5, tz=3.6)}  # eye-level framing per piece


def view(cam, lo, hi, kind, name=""):
    c = (lo + hi) / 2
    ext = hi - lo
    r = ext.length / 2
    cam.data.type = "PERSP"
    cam.data.sensor_width = 36
    if kind == "thumb":
        cam.data.lens = 70
        d = Vector((-0.55, -0.83, 0.36)).normalized()
        fov = 2 * math.atan(18 * 0.72 / cam.data.lens)  # the frame is 1 : 0.72, so the vertical field is the tight one
        look(cam, c + d * (r / math.sin(fov / 2) * 1.04), c)
    elif kind == "play":
        cam.data.type, yaw, pitch = "ORTHO", math.radians(30), math.radians(38)
        d = Vector((-math.sin(yaw) * math.cos(pitch), -math.cos(yaw) * math.cos(pitch), math.sin(pitch)))
        look(cam, c + d * 200, c)
        cam.data.clip_end = 600
        cam.data.ortho_scale = max(ext.x * 0.98 * math.cos(yaw) + ext.y * math.sin(yaw), (ext.z * math.cos(pitch) + (ext.x * math.sin(yaw) + ext.y * math.cos(yaw)) * math.sin(pitch))) * 1.18 + 0.3
    else:  # eye level: 1.7 m, a little left of the front axis, framing the whole piece width
        cam.data.lens = 40
        span = max(ext.x, ext.z * 1.3, 2.0)
        dist = span / (2 * math.tan(math.atan(18 / 40))) * 1.1 + max(ext.y, 0) * 0.5 + 1.0
        tz = min(c.z, 2.4) if ext.z > 3 else c.z
        e = EYE.get(name)
        if e:
            look(cam, Vector((c.x + e["dx"] - e["dist"] * 0.26, lo.y - e["dist"], 1.7)), Vector((c.x + e["dx"], c.y, e["tz"])))
        else:
            look(cam, Vector((c.x - dist * 0.26, c.y - dist, 1.7)), Vector((c.x, c.y, tz)))


def paving_sample():
    """Render-only piece: a 14 x 10 m patch of DK_Pave_Civic with a 1.2 m spine band, a 0.6 m ring of four R2 arcs round a
    play-zoom sphere lamp and a quarter of the fountain's R 4.3 / R 8.6 rings: the band design at scale."""
    from geo import Shape
    from mats import pal
    s = Shape("DK_PavingSample")
    s.extrude([(-12, -6), (8, -6), (8, 7), (-12, 7)], -0.03, 0.0, pal.Pave_Civic)
    for x in range(-7, 8, 2):
        with s.at((x, -2.5, 0)):
            reg["InlayBand_12_2"]["fn"](s)
    for k in range(4):
        with s.at((3.5, 1.6, 0), (0, 0, 90 * k)):
            reg["InlayArc_R2_W06"]["fn"](s)
    with s.at((3.5, 1.6, 0)):
        reg["PlazaSphereLamp_Play"]["fn"](s)
    for rn, w in (("InlayArc_R4_W06", 4.3), ("InlayArc_R9_W12", 8.6)):
        with s.at((-9.0, 1.6, 0), (0, 0, 0)):
            reg[rn]["fn"](s)
    obj = s.build(bevel=0.0)
    return obj


def shelter_sample():
    """Render-only piece: BoardingPavilion_Square_v2 on the block's two PavilionPier_Square supports (stand-in boxes 1.12 x 1.12,
    1.855 x 1.2 high as the catalogue note asks Splice to scale them)."""
    from geo import Shape
    from mats import pal
    s = Shape("DK_ShelterSample")
    reg["BoardingPavilion_Square_v2"]["fn"](s)
    for x in (-3.22, 3.22):
        s.rbox((x - 0.56, -1.07, 0.0), (x + 0.56, 0.05, 1.855 * 1.2), 0.03, pal.Limestone)
    return s.build(bevel=0.0)


def main():
    global reg
    reg = kit.load()
    ids = [n for n in arg("--ids", "").split(",") if n] or list(reg)
    out = Path(arg("--dir", str(REPO / "out/review/008-renders")))
    out.mkdir(parents=True, exist_ok=True)
    kinds = [k for k in arg("--views", "thumb,play,eye").split(",") if k]
    cam, ground = studio(arg("--px", 560), arg("--samples", 40))
    for n in ids:
        obj = paving_sample() if n == "PavingSample" else shelter_sample() if n == "ShelterSample" else kit.build(n)
        lo, hi = bounds(obj)
        ground.location.z = lo.z - 0.04 if n == "PavingSample" else 0.0  # the sample's slab top is z = 0: keep the studio ground below it
        for k in kinds:
            view(cam, lo, hi, k, n)
            bpy.context.scene.render.filepath = str(out / f"{n}.{k}.png")
            bpy.ops.render.render(write_still=True)
        for o in [obj, *obj.children_recursive]:
            bpy.data.objects.remove(o, do_unlink=True)


main()
