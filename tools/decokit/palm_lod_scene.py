"""Shared scene for the palm LOD boards (Vela, brief 028): one sun, one camera model, the game's third-person lens.

Third person (HostFollow): perspective, 55 degrees vertical, a camera about 3.1 m up looking down the street, far clip 350 m, linear haze #ecd9b4 from 90 m to 350 m (haze.json).
Play zoom (HandoverHost): orthographic, half-height 14 m (12.5 to 18.5), pitch 38, yaw 30.
Pixel scale = the Air's panel, 1864 px tall (2880 x 1864), render scale 1.
"""
import math
import sys
from pathlib import Path

import bpy
from mathutils import Euler, Vector

sys.path.insert(0, str(Path(__file__).parent))
import kit  # noqa: E402
import palm_lod  # noqa: E402

FOV, PANEL_H = 55.0, 1864
F_PX = (PANEL_H / 2) / math.tan(math.radians(FOV / 2))   # focal length in panel pixels
EYE = 3.1                                                  # third-person camera height (followHeight 1.3 + 0.32 * 5.5 m back)
HAZE = (0xEC / 255, 0xD9 / 255, 0xB4 / 255)                # haze.json colour, sRGB
HAZE_START, HAZE_END = 90.0, 350.0


def fog(d):
    return min(1.0, max(0.0, (d - HAZE_START) / (HAZE_END - HAZE_START)))


def setup(res=(800, 800), samples=48):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    kit.load()
    palm_lod.register()
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE"
    sc.render.film_transparent = True
    sc.render.image_settings.file_format = "PNG"
    sc.render.image_settings.color_mode = "RGBA"
    sc.view_settings.view_transform = "Standard"
    sc.view_settings.exposure = -0.3
    sc.eevee.taa_render_samples = samples
    sc.render.resolution_percentage = 100
    sc.render.resolution_x, sc.render.resolution_y = res
    w = bpy.data.worlds.new("w"); w.use_nodes = True
    bg = w.node_tree.nodes["Background"]; bg.inputs[0].default_value = (.62, .70, .80, 1); bg.inputs[1].default_value = 0.9
    sc.world = w
    sun = bpy.data.lights.new("sun", "SUN"); sun.energy, sun.color = 3.2, (1, .93, .8)
    so = bpy.data.objects.new("sun", sun)
    # the game's sun stands at azimuth 150, 38 degrees up; the camera looks along +Y (compass 0), the sun comes from behind-right
    so.rotation_euler = Euler((math.radians(90 - 38), 0, math.radians(150 + 180)), "XYZ"); sc.collection.objects.link(so)
    cd = bpy.data.cameras.new("c"); co = bpy.data.objects.new("c", cd); sc.collection.objects.link(co); sc.camera = co
    cd.clip_end = 2000; cd.clip_start = 0.1
    return sc, cd, co


def link(o):
    sc = bpy.context.scene
    for k in [o, *o.children_recursive]:
        if k.name not in sc.collection.objects:
            sc.collection.objects.link(k)
    return o


def hide(o, on=True):
    for k in [o, *o.children_recursive]:
        k.hide_render = on
        k.hide_viewport = on


def make(name, lv):
    """Piece `name` at level lv (0 = the shipped build), linked into the scene, at the origin."""
    if lv in (0, 1, 2):
        o = kit.build(name) if lv == 0 else kit.build_lod(name, f"L{lv}")
    else:  # an experiment of palm_lod.LEVELS
        from geo import Shape
        H, wob, seed = palm_lod.PIECES[name]
        s = Shape(f"DK_{name}_X{lv}")
        palm_lod.level(s, H, wob, seed, lv)
        o = s.build(None, bevel=0.0)
    return link(o)


def shoot_persp(path, d, res, zc, cam_cd, cam_co, az=0.0):
    """Perspective shot at the panel's pixel scale: the camera `d` m from the palm foot at EYE height, level, centre of the crop at height zc. az turns the camera round the palm (0 = from -Y)."""
    sc = bpy.context.scene
    W, H = res
    sc.render.resolution_x, sc.render.resolution_y = W, H
    cam_cd.type = "PERSP"; cam_cd.sensor_fit = "HORIZONTAL"; cam_cd.sensor_width = 36.0
    cam_cd.lens = F_PX * 36.0 / W
    a = math.radians(az)
    cam_co.location = Vector((math.sin(a) * d, -math.cos(a) * d, EYE))
    cam_co.rotation_euler = Euler((math.radians(90), 0, a), "XYZ")  # looking at the foot, level
    cam_cd.shift_x = 0.0
    cam_cd.shift_y = ((zc - EYE) * F_PX / d) / W
    sc.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


def shoot_ortho(path, res, zc, cam_cd, cam_co, half=14.0, pitch=38.0, yaw=30.0, target=Vector((0, 0, 0)), dist=200.0):
    """Play zoom: orthographic, half-height `half` m on the panel, so the pixel scale is PANEL_H / (2 half) px a metre."""
    sc = bpy.context.scene
    W, H = res
    sc.render.resolution_x, sc.render.resolution_y = W, H
    cam_cd.type = "ORTHO"; cam_cd.sensor_fit = "VERTICAL"
    cam_cd.ortho_scale = 2 * half * H / PANEL_H     # H pixels of the panel's PANEL_H
    p, y = math.radians(pitch), math.radians(yaw)
    dvec = Vector((math.sin(y) * math.cos(p), -math.cos(y) * math.cos(p), math.sin(p)))
    t = target + Vector((0, 0, zc))
    cam_co.location = t + dvec * dist
    cam_co.rotation_euler = (t - cam_co.location).to_track_quat("-Z", "Y").to_euler()
    cam_cd.shift_x = cam_cd.shift_y = 0.0
    sc.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)
