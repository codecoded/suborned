"""Brief 009 look options: the crossing at the play camera, options (a) to (d), and the brass study (studs and edge line in three browns).
Blender -b -P render009opts.py -- --dir out/decokit-009/options [--what options,brass,calib] [--samples 48] [--only a,b2]
Camera = the game's: orthographic, 28 m of the street tall (zoom 14), pitch 38, yaw 30, 1920 x 1080, so 38.6 px per metre. (render009.py set ortho_scale 28 on the frame's
WIDTH, which draws 68.6 px per metre: its play frames are 1.78x too close; this script fixes the sensor fit.) One 12 m road (the district's commonest: 42 of 95 crossings), kerbs at
x = +-6, the 4 m strip y = -2..2, lights from 0.5 m off each kerb. Every piece is built through kit.build / kit.build_lod, the exporter's own path.
Look of the stage = the Unity capture of the cafe crossing (briefs/009/shots/009c-cafe-crossing-play.png): asphalt (139, 128, 128), sunlit pavement (209, 191, 162), a seafoam dot (154, 219, 198), no bloom.
Writes <dir>/opt.<id>.png, brass.<tag>.png and frames.json (projection of the strip, studs and edge line into the frame, for the sheet's crops)."""

import json
import math
import sys
from pathlib import Path

import bpy
from bpy_extras.object_utils import world_to_camera_view
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).parent))
import render008 as r8  # noqa: E402  (studio, look; importing runs no render)
import kit  # noqa: E402
import mats  # noqa: E402
from geo import Shape  # noqa: E402

REPO = Path(__file__).resolve().parents[2]
arg = r8.arg
HALF, STRIP, END = 6.0, 2.0, 0.5            # 12 m road: kerbs at x = +-6; strip half width; lights start 0.5 m off each kerb
ROOM = 2 * HALF - 2 * END                   # 11 m of light line
R_OUT = 2.83
ASPHALT, PAVE, KERB = arg("--asphalt", "#7A6F72"), arg("--pave", "#C6B39B"), arg("--kerb", "#C0AF98")   # albedo, tuned by --what calib to the Unity pixels
DOT_PX = (154, 219, 198)                    # the Unity capture's lit dot
POOL_PEAK, FIELD_PEAK = arg("--pool-peak", 0.42), arg("--field-peak", 0.30)   # additive strength at the centre of a glow quad, as a multiple of the dot colour
for _n, _k, _h in (("DK_Scene_Asphalt", "stone", ASPHALT), ("DK_Scene_Pavement", "ceramic", PAVE), ("DK_Scene_Kerb", "stone", KERB),
                   ("DK_Scene_Peach", "fabric", "#B8745F"), ("DK_Scene_Lilac", "fabric", "#7E6C97"), ("DK_Scene_Skin", "fabric", "#C49A82")):
    mats.register(_n, _k, _h)


def modules_in(room, modules):
    """Whole modules, longest first (the generator's modulesIn)."""
    out, left = [], room + 1e-9
    for m in sorted(modules, reverse=True):
        while left >= m:
            out.append(m)
            left -= m
    return out


# id -> (name, light line modules {length: piece}, line centre |y|, glow under the lines {length: piece} or None, glow kind)
OPTIONS = {
    "none": dict(title="no light lines (the stage alone: the baseline the sheet measures lit pixels against)", lines={}, y=0),
    "a": dict(title="as built: dots", lines={2: "LightDotRow_2", 1: "LightDotRow_1"}, y=1.75),
    "b2": dict(title="rows two dots wide", lines={2: "LightDotBand2_2", 1: "LightDotBand2_1"}, y=1.5),
    "b3": dict(title="rows three dots wide", lines={2: "LightDotBand3_2", 1: "LightDotBand3_1"}, y=1.25),
    "c": dict(title="light bars along the walk", lines={4: "LightBar_4", 2: "LightBar_2", 1: "LightBar_1"}, y=1.745),
    "d1": dict(title="lit strip: field under the dots", lines={2: "LightDotRow_2", 1: "LightDotRow_1"}, y=1.75, glow={2: "LightField_2", 1: "LightField_1"}, glow_y=0.0, half=1.95, peak=FIELD_PEAK),
    "d2": dict(title="light pools under the dots", lines={2: "LightDotRow_2", 1: "LightDotRow_1"}, y=1.75, glow={2: "LightPool_2", 1: "LightPool_1"}, glow_y=1.75, half=0.65, peak=POOL_PEAK),
}


# ----------------------------------------------------------------- materials as Unity draws them

def emit_only(m, rgb255, strength=1.0):
    """The lights as Latch draws them (an emission-only seafoam, no bloom): base colour black, emission = the pixel the capture shows."""
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (0, 0, 0, 1)
    b.inputs["Emission Color"].default_value = (*mats.linear([c / 255 for c in rgb255]), 1)
    b.inputs["Emission Strength"].default_value = strength
    return m


def additive_glow(name, rgb255, peak, half):
    """kind glow as Latch draws it: additive, double-sided, fade = 1 - |v| / half (v = the face's UV y = metres across the line)."""
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    new = nt.nodes.new
    out, tr, em, add = new("ShaderNodeOutputMaterial"), new("ShaderNodeBsdfTransparent"), new("ShaderNodeEmission"), new("ShaderNodeAddShader")
    uv, sep = new("ShaderNodeUVMap"), new("ShaderNodeSeparateXYZ")
    ab, dv, sb, mx, pw, mul = (new("ShaderNodeMath") for _ in range(6))
    uv.uv_map = "UVMap"
    ab.operation, dv.operation, sb.operation, mx.operation, pw.operation, mul.operation = "ABSOLUTE", "DIVIDE", "SUBTRACT", "MAXIMUM", "POWER", "MULTIPLY"
    dv.inputs[1].default_value, sb.inputs[0].default_value, mx.inputs[1].default_value, pw.inputs[1].default_value, mul.inputs[1].default_value = half, 1.0, 0.0, 1.5, peak
    for a, b in ((uv.outputs["UV"], sep.inputs["Vector"]), (sep.outputs["Y"], ab.inputs[0]), (ab.outputs[0], dv.inputs[0]), (dv.outputs[0], sb.inputs[1]), (sb.outputs[0], mx.inputs[0]),
                 (mx.outputs[0], pw.inputs[0]), (pw.outputs[0], mul.inputs[0]), (mul.outputs[0], em.inputs["Strength"]), (tr.outputs[0], add.inputs[0]), (em.outputs[0], add.inputs[1]), (add.outputs[0], out.inputs["Surface"])):
        nt.links.new(a, b)
    em.inputs["Color"].default_value = (*mats.linear([c / 255 for c in rgb255]), 1)
    m.surface_render_method = "BLENDED"
    return m


def restyle(o, glow=None):
    """Swap the light materials of a placed piece for the Unity-like ones (and the glow quad for its additive material)."""
    for c in [o, *o.children_recursive]:
        for sl in c.material_slots:
            nm = sl.material.name.split(".")[0] if sl.material else ""
            if nm in ("DK_Cross_Light", "DK_Cross_Walk"):
                sl.material = emit_only(sl.material.copy(), DOT_PX if nm == "DK_Cross_Light" else (190, 235, 220))
            if nm == "DK_Cross_Pool" and glow:
                sl.material = additive_glow("Glow_" + o.name, DOT_PX, glow["peak"], glow["half"])


def place(name, loc, yaw=0.0, lod=False, state="walk", glow=None):
    o = kit.build_lod(name, "L1") if lod else kit.build(name)
    o.location, o.rotation_euler = loc, (0, 0, math.radians(yaw))
    if name == "SignalEye":
        for c in o.children_recursive:
            c.hide_render = c.name.split(".")[0].endswith("_Stop" if state == "walk" else "_Walk")
    restyle(o, glow)
    if name != "SignalEye":                # castShadows false in the world export for every crossing part but the orb body
        for c in [o, *o.children_recursive]:
            c.visible_shadow = False
    return o


def person(s, x, y, mat, h=1.68, yaw=0.0):
    with s.at((x, y, 0), (0, 0, yaw)):
        s.lathe([(0.0, 0.0), (0.2, 0.0), (0.24, 0.1), (0.17, 0.72), (0.2, 1.2), (0.14, 1.42), (0.06, 1.46), (0.06, h - 0.2)], mat, seg=14, cap_top=False)
        s.lathe([(0.0, h - 0.22), (0.1, h - 0.2), (0.115, h - 0.1), (0.08, h - 0.01), (0, h)], mats.pal.Scene_Skin, seg=12)


def stage():
    s = Shape("Stage")
    s.box((-HALF - 60, -60, -0.04), (-HALF, 60, 0.0), mats.pal.Scene_Pavement)
    s.box((HALF, -60, -0.04), (HALF + 60, 60, 0.0), mats.pal.Scene_Pavement)
    s.box((-HALF, -60, -0.04), (HALF, 60, 0.0), mats.pal.Scene_Asphalt)
    for x0, x1 in ((-HALF - 0.3, -HALF), (HALF, HALF + 0.3)):
        s.box((x0, -60, 0.0), (x1, 60, 0.12), mats.pal.Scene_Kerb)
    # five people on and beside the strip: scale (1.7 m = 51 px) and what a foot does to a light
    for x, y, m, h, yaw in ((-1.2, -0.9, mats.pal.Scene_Peach, 1.68, 100), (0.6, 0.7, mats.pal.Scene_Lilac, 1.62, -80), (2.8, -1.1, mats.pal.Scene_Peach, 1.74, 95),
                            (-3.9, 1.2, mats.pal.Scene_Lilac, 1.66, -85), (HALF + 1.6, -3.4, mats.pal.Scene_Lilac, 1.7, 40)):
        person(s, x, y, m, h, yaw)
    return s.build(bevel=0.0)


def crossing(opt, brass=None):
    """The 12 m crossing for option `opt`; returns every object made. `brass` = a material to put on every DK_Brass_Civic slot (the brass study)."""
    objs = [stage()]
    o = OPTIONS[opt]
    glow = dict(peak=o.get("peak", 0), half=o.get("half", 1))
    # light lines
    for sy in (-1, 1):
        x = -ROOM / 2
        for L in modules_in(ROOM, list(o["lines"])):
            objs.append(place(o["lines"][L], (x + L / 2, sy * o["y"], 0)))
            x += L
    if "glow" in o:
        for ys in ((-1, 1) if o["glow_y"] else (0,)):
            x = -ROOM / 2
            for L in modules_in(ROOM, list(o["glow"])):
                objs.append(place(o["glow"][L], (x + L / 2, ys * o["glow_y"], 0), glow=glow))
                x += L
    for sy in (-1, 1):                  # the brass edge line, kerb to kerb (the generator's end = 0)
        for k in range(6):
            objs.append(place("CrossEdgeLine_2", (-HALF + 1 + 2 * k, sy * STRIP, 0)))
    for sx in (-1, 1):
        for sy in (-1, 1):
            objs.append(place("SignalEye", (sx * (HALF + 1.0), sy * 2.6, 0), -30))
            for y in (2.35, 3.55, 4.75):
                objs.append(place("KerbStud", (sx * (HALF + 0.55), sy * y, 0)))
            c = (sx * (HALF + R_OUT), sy * (STRIP + R_OUT), 0)
            yaw = {(1, 1): 180, (1, -1): 90, (-1, 1): 270, (-1, -1): 0}[(sx, sy)]
            objs.append(place("CrossInlayArc_R24_W08", c, yaw))
    if brass is not None:
        for ob in objs:
            for c in [ob, *ob.children_recursive]:
                for sl in c.material_slots:
                    if sl.material and sl.material.name.split(".")[0] == "DK_Brass_Civic":
                        sl.material = brass
    return objs


def camera(cam):
    sc = bpy.context.scene
    yaw, pitch = math.radians(30), math.radians(38)
    d = Vector((-math.sin(yaw) * math.cos(pitch), -math.cos(yaw) * math.cos(pitch), math.sin(pitch)))
    sc.render.resolution_x, sc.render.resolution_y = 1920, 1080
    cam.data.type, cam.data.sensor_fit, cam.data.sensor_width, cam.data.clip_end, cam.data.ortho_scale = "ORTHO", "VERTICAL", 36, 600, 28.0   # 28 m tall: the game's zoom 14
    r8.look(cam, Vector((0, 0, 0.5)) + d * 200, Vector((0, 0, 0.5)))


def px(cam, p):
    v = world_to_camera_view(bpy.context.scene, cam, Vector(p))
    return [round(v.x * 1920, 1), round((1 - v.y) * 1080, 1)]


def delete(objs):
    for o in objs:
        for c in [o, *o.children_recursive]:
            bpy.data.objects.remove(c, do_unlink=True)


def brass_material(tag, hexstr, metallic, rough):
    m = bpy.data.materials.new("Brass_" + tag)
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*mats.linear(mats.srgb(hexstr)), 1)
    b.inputs["Metallic"].default_value, b.inputs["Roughness"].default_value = metallic, rough
    return m


BRASS = {"99784D": "#99784D", "AD866F": "#AD866F", "BF917C": "#BF917C"}
SHADING = {"metal": (1.0, 0.3), "satin": (0.3, 0.5)}   # (metallic, roughness): the kit's `metal` kind as built, and a satin brass


def main():
    kit.load()
    out = Path(arg("--dir", str(REPO / "out/decokit-009/options")))
    out.mkdir(parents=True, exist_ok=True)
    what = arg("--what", "options,brass").split(",")
    only = [x for x in arg("--only", "").split(",") if x]
    cam, ground = r8.studio(arg("--px", 1920), arg("--samples", 48))
    sc = bpy.context.scene
    sc.view_settings.view_transform, sc.view_settings.look, sc.view_settings.exposure = "Standard", "None", arg("--exposure", 0.0)
    ground.hide_render = True
    camera(cam)
    frames = {}
    if "calib" in what:
        objs = [stage()]
        sc.render.filepath = str(out / "calib.png")
        bpy.ops.render.render(write_still=True)
        frames["calib"] = dict(asphalt=px(cam, (HALF - 2.0, -0.5, 0)), pavement=px(cam, (-HALF - 3.0, -2.0, 0)), pavement_far=px(cam, (HALF + 4.0, 1.0, 0)))
        delete(objs)
    if "options" in what:
        for opt in OPTIONS:
            if only and opt not in only:
                continue
            objs = crossing(opt)
            sc.render.filepath = str(out / f"opt.{opt}.png")
            bpy.ops.render.render(write_still=True)
            delete(objs)
    if "brass" in what:
        for hexkey, hexstr in BRASS.items():
            for shade, (metal, rough) in SHADING.items():
                tag = f"{hexkey}.{shade}"
                if only and tag not in only:
                    continue
                objs = crossing("a", brass_material(tag, hexstr, metal, rough))
                sc.render.filepath = str(out / f"brass.{tag}.png")
                bpy.ops.render.render(write_still=True)
                delete(objs)
    frames["strip"] = [px(cam, (x, y, 0)) for x, y in ((-HALF, -STRIP), (HALF, -STRIP), (HALF, STRIP), (-HALF, STRIP))]
    frames["studs"] = [px(cam, (HALF + 0.55, y, 0)) for y in (-4.75, -3.55, -2.35, 2.35, 3.55, 4.75)]
    frames["edge_line_ends"] = [px(cam, (x, y, 0)) for x in (-HALF, HALF) for y in (-STRIP, STRIP)]
    frames["origin"] = px(cam, (0, 0, 0))
    frames["px_per_m_x"] = [px(cam, (1, 0, 0))[i] - px(cam, (0, 0, 0))[i] for i in (0, 1)]
    frames["px_per_m_y"] = [px(cam, (0, 1, 0))[i] - px(cam, (0, 0, 0))[i] for i in (0, 1)]
    (out / "frames.json").write_text(json.dumps(frames, indent=1))


main()
