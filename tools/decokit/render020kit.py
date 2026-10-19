"""Brief 020 C: the kit pick's renders. Each piece on the studio ground at the play angle (orthographic, pitch 38, yaw 30 off its front, as the Handover camera) beside a 1.75 m
stand-in person, fitted to the frame, and a second frame at TRUE play scale (orthographic 28 m over 1080 px: 38.6 px/m) so what the player gets is visible. Cycles, the studio HDRI.
Blender -b -P render020kit.py -- --ids A,B --dir briefs/020/kit [--px 720] [--samples 48] [--tag x] [--eye]   (--eye adds a street-level view, 1.7 m, 40 mm)
Builds through kit.build (the exporter's own path, bevel and all). Stand-in person is render-only."""

import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).parent))
import render008 as R8  # noqa: E402
import kit  # noqa: E402
from geo import Shape  # noqa: E402
from mats import pal  # noqa: E402

arg = R8.arg


def person(at):
    s = Shape("DK_Person")
    with s.at(at):
        s.lathe([(0.0, 0.0), (0.2, 0.0), (0.24, 1.2), (0.2, 1.52), (0.0, 1.55)], pal.Enamel_Charcoal, seg=12, cap_bottom=False)
        s.lathe([(0.0, 1.55), (0.1, 1.58), (0.12, 1.66), (0.0, 1.75)], pal.Enamel_Ivory, seg=10, cap_bottom=False)
    return s.build(bevel=0.0)


def play_dir():
    yaw, pitch = math.radians(arg("--yaw", 30)), math.radians(38)
    return Vector((-math.sin(yaw) * math.cos(pitch), -math.cos(yaw) * math.cos(pitch), math.sin(pitch)))


def main():
    out = Path(arg("--dir", "briefs/020/kit"))
    out.mkdir(parents=True, exist_ok=True)
    reg = kit.load()
    ids = [n for n in arg("--ids", "").split(",") if n]
    px, tag = arg("--px", 720), arg("--tag", "")
    cam, ground = R8.studio(px, arg("--samples", 48))
    sc = bpy.context.scene
    for n in ids:
        obj = kit.build(n)
        lo, hi = R8.bounds(obj)
        c = (lo + hi) / 2
        ext = hi - lo
        ground.location.z = 0.0
        p = person((hi.x + 0.9, lo.y - 0.2, 0.0)) if max(ext.x, ext.y) < 40 else None
        if "--landing" in R8.ARGS and p:  # a figure at the Office entrance point: 0.75 m off the face, on the 0.75 m landing (brief 023)
            bpy.data.objects.remove(p, do_unlink=True)
            p = person((0.0, -0.75, 0.75))
        # fitted play-angle view, person included
        lo2, hi2 = (R8.bounds(p)[0].copy(), R8.bounds(p)[1].copy()) if p else (lo, hi)
        a, b = Vector([min(lo[i], lo2[i]) for i in range(3)]), Vector([max(hi[i], hi2[i]) for i in range(3)])
        cc, ee = (a + b) / 2, b - a
        sc.render.resolution_x, sc.render.resolution_y = px, int(px * 0.75)
        cam.data.type = "ORTHO"
        cam.data.clip_start, cam.data.clip_end = 0.1, 800
        d = play_dir()
        R8.look(cam, cc + d * 300, cc)
        yaw = math.radians(arg("--yaw", 30))
        pitch = math.radians(38)
        w = ee.x * abs(math.cos(yaw)) + ee.y * abs(math.sin(yaw))
        h = ee.z * math.cos(pitch) + (ee.x * abs(math.sin(yaw)) + ee.y * abs(math.cos(yaw))) * math.sin(pitch)
        cam.data.ortho_scale = max(w, h * 4 / 3) * 1.12 + 0.3
        sc.render.filepath = str(out / f"{n}.play{tag}.png")
        bpy.ops.render.render(write_still=True)
        # true play scale: 38.6 px/m, the piece at the frame centre
        if "--true" in R8.ARGS:
            sc.render.resolution_x, sc.render.resolution_y = 360, 270
            cam.data.ortho_scale = 360 / 38.6
            R8.look(cam, c + d * 300, c)
            sc.render.filepath = str(out / f"{n}.true{tag}.png")
            bpy.ops.render.render(write_still=True)
        if "--eye" in R8.ARGS:
            sc.render.resolution_x, sc.render.resolution_y = px, int(px * 0.75)
            cam.data.type, cam.data.lens = "PERSP", 40
            span = max(ext.x, ext.y, ext.z * 1.2, 1.5)
            R8.look(cam, Vector((c.x - span * 0.45, lo.y - span * 1.5, 1.7)), Vector((c.x, c.y, min(c.z, 2.0))))
            sc.render.filepath = str(out / f"{n}.eye{tag}.png")
            bpy.ops.render.render(write_still=True)
        for o in [obj, *obj.children_recursive] + ([p] if p else []):
            bpy.data.objects.remove(o, do_unlink=True)


main()
