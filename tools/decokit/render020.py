"""Brief 020 check renders: the café front in context (CafeBay_v2, CafeInterior, CafeTerrace, a stand-in upper mass above z 4.7, stand-in people 1.75 m) at play zoom
(orthographic 28 m at 1080 p, pitch 38, yaw 30 as the Handover camera) and at street level (eye 1.7 m); plus close views through the glass. Cycles, the DecoKit studio HDRI.
Blender -b -P render020.py -- --dir briefs/020/cafe [--px 1920] [--samples 64] [--views play,eye,door,side,cut]  [--old]  (old = the shipped CafeBay for the before shot)
Stand-ins in the frame (the upper mass, the pavement, the capsules) are render-only and never exported."""

import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).parent))
import render008 as R8  # noqa: E402  (reads its own --flags from the same argv)
import kit  # noqa: E402
from civic_common import front_path  # noqa: E402
from geo import Shape  # noqa: E402
from mats import pal  # noqa: E402
from structures import ccw  # noqa: E402

arg = R8.arg
OLD = "--old" in R8.ARGS


def context():
    """The generator's upper mass over the ground storey (cream, z 4.7 up), a pavement slab, capsule people."""
    s = Shape("DK_Context")
    P = front_path(6, 4)
    poly = ccw([(x, y) for x, y in P] + [(11, 11.3), (-11, 11.3)])
    s.extrude(poly, 4.7, 17.0, pal.Limestone)
    for x, y, h in ((0.0, -1.9, 1.75), (-2.4, -3.2, 1.7), (3.3, -4.6, 1.8), (-7.5, -4.0, 1.65), (6.9, -2.8, 1.75), (9.8, -8.0, 1.7)):
        with s.at((x, y, 0)):
            s.lathe([(0.0, 0.0), (0.2, 0.0), (0.24, 1.2), (0.2, 1.55), (0.0, 1.58)], pal.Enamel_Charcoal if x < 0 else pal.Enamel_Teal, seg=10, cap_bottom=False)
            s.lathe([(0.0, h - 0.2), (0.11, h - 0.15), (0.12, h - 0.05), (0.0, h)], pal.Enamel_Ivory, seg=8, cap_bottom=False)
    return s.build(bevel=0.0)


def build_all():
    objs = [kit.build("CafeBay" if OLD else "CafeBay_v2"), kit.build("CafeTerrace"), context()]
    if not OLD:
        objs.append(kit.build("CafeInterior"))
    return objs


def aim(cam, pos, target, lens=None):
    cam.data.type = "PERSP"
    if lens:
        cam.data.lens = lens
    R8.look(cam, Vector(pos), Vector(target))


def setview(cam, kind):
    cam.data.clip_start, cam.data.clip_end = 0.05, 600
    if kind == "play":
        yaw, pitch = math.radians(30), math.radians(38)
        d = Vector((-math.sin(yaw) * math.cos(pitch), -math.cos(yaw) * math.cos(pitch), math.sin(pitch)))
        cam.data.type = "ORTHO"
        R8.look(cam, Vector((0, 1.5, 2.2)) + d * 200, Vector((0, 1.5, 2.2)))
        cam.data.ortho_scale = arg("--ortho", 28.0)
    elif kind == "zoom":  # play angle, framed on the door and the window row
        yaw, pitch = math.radians(30), math.radians(38)
        d = Vector((-math.sin(yaw) * math.cos(pitch), -math.cos(yaw) * math.cos(pitch), math.sin(pitch)))
        cam.data.type = "ORTHO"
        R8.look(cam, Vector((-1.5, 1.0, 1.2)) + d * 200, Vector((-1.5, 1.0, 1.2)))
        cam.data.ortho_scale = 12.0
    elif kind == "eye":
        aim(cam, (-4.5, -11.5, 1.7), (0.5, 0.0, 2.1), 32)
    elif kind == "follow":  # the T follow view's height and distance, behind the cell looking at the front
        aim(cam, (3.2, -9.0, 3.4), (-0.5, 1.0, 1.4), 30)
    elif kind == "door":
        aim(cam, (1.2, -5.2, 1.7), (0.0, 4.0, 1.5), 30)
    elif kind == "side":
        aim(cam, (17.5, 1.5, 1.7), (8.0, 7.5, 1.8), 28)
    elif kind == "cut":  # looking down through the glass at 38 deg, close
        yaw, pitch = math.radians(-24), math.radians(38)
        d = Vector((-math.sin(yaw) * math.cos(pitch), -math.cos(yaw) * math.cos(pitch), math.sin(pitch)))
        cam.data.type = "ORTHO"
        R8.look(cam, Vector((4.5, 1.0, 1.0)) + d * 200, Vector((4.5, 1.0, 1.0)))
        cam.data.ortho_scale = 11.0


def main():
    out = Path(arg("--dir", "out/work020/render"))
    out.mkdir(parents=True, exist_ok=True)
    kit.load()
    cam, ground = R8.studio(arg("--px", 1920), arg("--samples", 64))
    sc = bpy.context.scene
    sc.render.resolution_x, sc.render.resolution_y = arg("--px", 1920), int(arg("--px", 1920) * 9 / 16)
    build_all()
    ground.location.z = -0.2
    tag = arg("--tag", "")
    for k in [v for v in arg("--views", "play,eye,door,side,cut,zoom").split(",") if v]:
        setview(cam, k)
        sc.render.filepath = str(out / f"cafe.{k}{tag}.png")
        bpy.ops.render.render(write_still=True)


main()
