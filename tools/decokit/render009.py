"""Brief 009 A check renders. Blender -b -P render009.py -- --dir out/review/009-renders [--what pieces,scene] [--px 560] [--samples 48]
  pieces: each piece as a studio thumbnail (3/4 view), SignalEye in both aspects, LOD0 beside LOD1 (render008's studio and views)
  scene : an 8 m crossing built from the candidates on pale paving, kerbs and lilac-grey asphalt: bars and dots variants, each at the play camera (orthographic
          size 14, pitch 38, yaw 30, 1920 x 1080: the pixels the game draws) and at eye level (1.7 m, 35 mm, 1536 x 1024, the concepts' frame)
Everything is built through kit.build / kit.build_lod, the exporter's own path. No bloom here (Unity adds it): report009.py adds a stand-in for the sheet only."""

import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).parent))
import render008 as r8  # noqa: E402  (studio, bounds, view; importing runs no render)
import kit  # noqa: E402
import mats  # noqa: E402
from geo import Shape  # noqa: E402

REPO = Path(__file__).resolve().parents[2]
arg = r8.arg
for _n, _k, _h in (("DK_Scene_Asphalt", "stone", "#A5A2A8"), ("DK_Scene_Pavement", "ceramic", "#F9EFDC"), ("DK_Scene_Kerb", "stone", "#DDD4C5"),
                   ("DK_Scene_Peach", "fabric", "#E8B9A0"), ("DK_Scene_Lilac", "fabric", "#B7A9C9"), ("DK_Scene_Skin", "fabric", "#D9B79E")):
    mats.register(_n, _k, _h)
HALF, STRIP = 4.0, 2.0          # the 8 m crossing: kerbs at x = +-4, strip half width 2 m
R_OUT = 2.83                    # the arc's outer radius with its hairline


def place(name, loc, yaw=0.0, lod=False, state="walk"):
    """A piece in the scene; the orb shows one aspect (the other pictogram part is hidden, as Latch will enable one)."""
    o = kit.build_lod(name, "L1") if lod else kit.build(name)
    o.location, o.rotation_euler = loc, (0, 0, math.radians(yaw))
    if name == "SignalEye":
        for c in o.children_recursive:
            c.hide_render = c.name.split(".")[0].endswith("_Stop" if state == "walk" else "_Walk")  # names carry .001 suffixes after the first build
    return o


def person(s, x, y, mat, h=1.68, yaw=0.0):
    with s.at((x, y, 0), (0, 0, yaw)):
        s.lathe([(0.0, 0.0), (0.2, 0.0), (0.24, 0.1), (0.17, 0.72), (0.2, 1.2), (0.14, 1.42), (0.06, 1.46), (0.06, h - 0.2)], mat, seg=14, cap_top=False)
        s.lathe([(0.0, h - 0.22), (0.1, h - 0.2), (0.115, h - 0.1), (0.08, h - 0.01), (0, h)], mats.pal.Scene_Skin, seg=12)


def stage():
    s = Shape("Stage")
    s.box((-HALF - 40, -40, -0.04), (-HALF, 40, 0.0), mats.pal.Scene_Pavement)
    s.box((HALF, -40, -0.04), (HALF + 40, 40, 0.0), mats.pal.Scene_Pavement)
    s.box((-HALF, -40, -0.04), (HALF, 40, 0.0), mats.pal.Scene_Asphalt)
    for x0, x1 in ((-HALF - 0.3, -HALF), (HALF, HALF + 0.3)):
        s.box((x0, -40, 0.0), (x1, 40, 0.12), mats.pal.Scene_Kerb)
    person(s, 6.2, -1.2, mats.pal.Scene_Peach, yaw=40)
    person(s, 0.8, 0.9, mats.pal.Scene_Lilac, h=1.62, yaw=-80)
    person(s, -1.6, -0.7, mats.pal.Scene_Peach, h=1.74, yaw=100)
    person(s, -6.5, 1.3, mats.pal.Scene_Lilac, h=1.66, yaw=-10)
    return s.build(bevel=0.0)


def crossing(kind, face):
    """The 8 m crossing from the candidates; the strip is x -4..4 (walking), y -2..2. face = 'walk': each orb's display A looks along the walking direction;
    'camera': turned about the vertical axis to face the play camera (yaw 30). Returns every object made."""
    objs = [stage()]
    if kind == "bars":
        for k in range(8):
            objs.append(place("LightBar_4", (-3.5 + k, 0, 0), 90))
    else:
        for y in (-1.8, 0.0, 1.8):
            for k in range(4):
                objs.append(place("LightDotRow_2", (-3.0 + 2 * k, y, 0)))
    for sx in (-1, 1):
        for sy in (-1, 1):
            objs.append(place("SignalEye", (sx * 5.0, sy * 2.6, 0), -30 if face == "camera" else (90 if sx < 0 else -90)))
            for y in (2.35, 3.55, 4.75):
                objs.append(place("KerbStud", (sx * 4.55, sy * y, 0)))
            c = (sx * (HALF + R_OUT), sy * (STRIP + R_OUT), 0)  # outer edge tangent to the kerb line and to the strip's edge line
            yaw = {(1, 1): 180, (1, -1): 90, (-1, 1): 270, (-1, -1): 0}[(sx, sy)]
            objs.append(place("CrossInlayArc_R24_W08", c, yaw))
    return objs


def scene_views(cam, ground, out, kind, face, eye):
    sc = bpy.context.scene
    ground.hide_render = True
    objs = crossing(kind, face)
    yaw, pitch = math.radians(30), math.radians(38)
    d = Vector((-math.sin(yaw) * math.cos(pitch), -math.cos(yaw) * math.cos(pitch), math.sin(pitch)))
    sc.render.resolution_x, sc.render.resolution_y = 1920, 1080
    cam.data.type, cam.data.sensor_width, cam.data.clip_end, cam.data.ortho_scale = "ORTHO", 36, 600, 28.0
    r8.look(cam, Vector((0, 0, 0.5)) + d * 200, Vector((0, 0, 0.5)))
    sc.render.filepath = str(out / f"scene.{kind}.{face}.play.png")
    bpy.ops.render.render(write_still=True)
    if eye:
        sc.render.resolution_x, sc.render.resolution_y = 1536, 1024
        cam.data.type, cam.data.lens = "PERSP", 35
        r8.look(cam, Vector((-12.5, 0.0, 1.7)), Vector((0, 0, 1.95)))
        sc.render.filepath = str(out / f"scene.{kind}.{face}.eye.png")
        bpy.ops.render.render(write_still=True)
    for o in objs:
        for c in [o, *o.children_recursive]:
            bpy.data.objects.remove(c, do_unlink=True)
    ground.hide_render = False


def piece_views(cam, out, px):
    sc = bpy.context.scene
    sc.render.resolution_x, sc.render.resolution_y = px, int(px * 0.72)
    cases = [("SignalEye", "walk", False), ("SignalEye", "stop", False), ("SignalEye", "walk.L1", True), ("KerbStud", "", False), ("LightBar_4", "", False),
             ("LightDotRow_2", "", False), ("LightDotRow_1", "", False), ("CrossInlayArc_R24_W08", "", False), ("KerbStud", "L1", True), ("LightBar_4", "L1", True),
             ("LightDotRow_2", "L1", True), ("CrossInlayArc_R24_W08", "L1", True)]
    for name, tag, lod in cases:
        o = place(name, (0, 0, 0), 0, lod, "stop" if tag.startswith("stop") else "walk")
        lo, hi = r8.bounds(o)
        for kind in ("thumb",) if name != "SignalEye" else ("thumb", "eye"):
            r8.view(cam, lo, hi, kind, name)
            sc.render.filepath = str(out / f"{name}.{tag + '.' if tag else ''}{kind}.png")
            bpy.ops.render.render(write_still=True)
        for c in [o, *o.children_recursive]:
            bpy.data.objects.remove(c, do_unlink=True)


def main():
    kit.load()
    out = Path(arg("--dir", str(REPO / "out/review/009-renders")))
    out.mkdir(parents=True, exist_ok=True)
    what = arg("--what", "pieces,scene").split(",")
    cam, ground = r8.studio(arg("--px", 560), arg("--samples", 48))
    if "pieces" in what:
        piece_views(cam, out, arg("--px", 560))
    if "scene" in what:
        scene_views(cam, ground, out, "bars", "camera", False)
        scene_views(cam, ground, out, "bars", "walk", True)
        scene_views(cam, ground, out, "dots", "camera", False)
        scene_views(cam, ground, out, "dots", "walk", True)


main()
