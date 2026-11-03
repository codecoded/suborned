"""Brief 067 (Vela): plates of the sedans for the rival-car comparison. Transparent RGBA renders of each car, lit as the game's sun, at the play camera's pitch (38) and yaw (30 or 210,
orthographic, true scale 38.57 px/m = 28 m over 1080 px), and close ones; tools/decokit/cars067_compose.py lays them on the street and writes the boards.

Blender -b -P cars067_plates.py -- --out DIR [--ids Sedan_Official,Sedan_Getaway,StandIn,Sedan_Rival] [--yaws 30,210] [--headings 45,135,225,315]
        [--views play,close,top,side,eye] [--samples 40] [--ppm 38.57] [--plate 360x260]
  StandIn   Latch's stand-in (HostViews.Rivals.cs): Sedan_Official with every renderer's base colour multiplied by RivalTrim (0.46, 0.43, 0.55) in sRGB, lamps left alone.
  heading   the screen direction of the car's nose, degrees: 0 right, 90 up the screen (away from the camera), 180 left, 270 down (toward it). Roads run diagonally on the game's screen,
            so 45 / 135 / 225 / 315 are the real ones.
  The sun is the game's (light.json: pitch 38, yaw 330, #ffe0d0) in the same world the camera yaw is read in; the sky is the DecoKit studio HDRI. Not the game's grade: relative reads only.
Builds through kit.build (the exporter's own path, bevel and all), so what is drawn is what is exported (before LODs)."""

import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).parent))
import render008 as R8  # noqa: E402  (reads its own --flags from the same argv)
import kit  # noqa: E402
import mats  # noqa: E402
from geo import Shape  # noqa: E402
from mats import pal  # noqa: E402

import cars020  # noqa: E402,F401  (registers the sedans)
import cars067  # noqa: E402,F401  (and the rivals' sedan)

arg = R8.arg
R8.HDRI = Path(arg("--hdri", str(Path(__file__).resolve().parents[2] / "out/decokit/HDRI/city.exr")))  # reflections only (the diffuse light is the flat warm ambient): a street sky; the game's probe is a haze-bright sky over a street canyon
RIVAL_TRIM = (0.46, 0.43, 0.55)  # HostViews.Rivals.cs RivalTrim: Latch's stand-in, multiplied into _BaseColor in sRGB
SUN_YAW, SUN_PITCH, SUN_COLOUR = 330.0, 38.0, "#ffe0d0"  # unity/Assets/Suborned/Editor/World/light.json sun
PPM = arg("--ppm", 38.57)  # px per metre at the play zoom: 1080 px over 28 m


def ambient_world(world, hdri_strength, amb):
    """Diffuse light from a flat warm ambient (the game's trilight is #dedce0 / #faeadc / #e2c5a4: warm neutral, no blue), reflections from the studio HDRI (so chrome and enamel keep a
    varied environment to mirror). Camera rays see nothing (transparent film)."""
    nt = world.node_tree
    hd = nt.nodes["Background"]
    hd.inputs["Strength"].default_value = hdri_strength
    flat = nt.nodes.new("ShaderNodeBackground")
    flat.inputs["Color"].default_value = (srgb_to_lin(0xf3 / 255), srgb_to_lin(0xe6 / 255), srgb_to_lin(0xd8 / 255), 1.0)
    flat.inputs["Strength"].default_value = amb
    lp, mix = nt.nodes.new("ShaderNodeLightPath"), nt.nodes.new("ShaderNodeMixShader")
    out = next(n for n in nt.nodes if n.type == "OUTPUT_WORLD")
    nt.links.new(lp.outputs["Is Glossy Ray"], mix.inputs[0])
    nt.links.new(flat.outputs["Background"], mix.inputs[1])
    nt.links.new(hd.outputs["Background"], mix.inputs[2])
    nt.links.new(mix.outputs["Shader"], out.inputs["Surface"])


def srgb_to_lin(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def lin_to_srgb(c):
    return c * 12.92 if c <= 0.0031308 else 1.055 * c ** (1 / 2.4) - 0.055


def tint_object(obj, mul, flat=True):
    """Latch's stand-in, as HostViews.Rivals.cs Trim() writes it: per renderer, a MaterialPropertyBlock with _BaseColor = (the renderer's FIRST material's colour) x mul in sRGB. A property block
    reaches every material slot of its renderer, and the sedan is one mesh with ten slots, one renderer: so every slot (body, roof, glass, chrome, rubber, whitewalls) takes the first slot's
    colour, the body teal x mul: one flat dark navy, the roof pill and the tyres gone. flat=False draws what the comment says it means (each slot's own colour x mul), for comparison.
    Emissive lamps keep their emission either way (the block sets only _BaseColor). Per-object copies of the materials, the shared ones untouched."""
    slots = [sl for o in [obj, *obj.children_recursive] for sl in o.material_slots if sl.material]
    first = slots[0].material.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value
    for sl in slots:
        m = sl.material
        c = m.copy()
        b = c.node_tree.nodes["Principled BSDF"]
        r, g, bl, a = first if flat else b.inputs["Base Color"].default_value
        if m.name in ("DK_Lamp", "DK_Beacon_Rose", "DK_Signal_Red"):
            if flat:  # base colour only: its emission stays as it is
                b.inputs["Base Color"].default_value = (srgb_to_lin(lin_to_srgb(r) * mul[0]), srgb_to_lin(lin_to_srgb(g) * mul[1]), srgb_to_lin(lin_to_srgb(bl) * mul[2]), a)
            sl.material = c
            continue
        b.inputs["Base Color"].default_value = (srgb_to_lin(lin_to_srgb(r) * mul[0]), srgb_to_lin(lin_to_srgb(g) * mul[1]), srgb_to_lin(lin_to_srgb(bl) * mul[2]), a)
        sl.material = c


def person(at=(0, 0, 0)):
    """The 1.75 m stand-in person of render020kit (render-only)."""
    s = Shape("DK_Person")
    with s.at(at):
        s.lathe([(0.0, 0.0), (0.2, 0.0), (0.24, 1.2), (0.2, 1.52), (0.0, 1.55)], pal.Enamel_Charcoal, seg=12, cap_bottom=False)
        s.lathe([(0.0, 1.55), (0.1, 1.58), (0.12, 1.66), (0.0, 1.75)], pal.Enamel_Ivory, seg=10, cap_bottom=False)
    return s.build(bevel=0.0)


def build(cid):
    if cid in ("StandIn", "StandInTint"):  # StandIn: Latch's code as it runs (one flat colour); StandInTint: what its comment means (each slot x RivalTrim)
        o = kit.build("Sedan_Official")
        tint_object(o, RIVAL_TRIM, flat=cid == "StandIn")
        return o
    if cid == "Person":
        return person()
    if cid == "Testbed":  # the 2.5D testbed's authored sedan (the store's civic-sedan-v1.glb, 8,128 triangles), for the bar only; render-only, never exported
        before = set(bpy.data.objects)
        bpy.ops.import_scene.gltf(filepath=str(Path("~/Projects/the-suborned-store/testbed-art/civic-street-props/civic-sedan-v1.glb").expanduser()))
        new = [o for o in bpy.data.objects if o not in before]
        root = next((o for o in new if o.parent is None), new[0])
        return root
    return kit.build(cid)


def hide(o, on):
    for k in [o, *o.children_recursive]:
        k.hide_render = on


def forward(yaw, pitch):
    y, p = math.radians(yaw), math.radians(pitch)
    return Vector((math.sin(y) * math.cos(p), math.cos(y) * math.cos(p), -math.sin(p)))


def sun_rig(sc, so):
    f = forward(SUN_YAW, SUN_PITCH)
    so.rotation_euler = f.to_track_quat("-Z", "Y").to_euler()
    so.data.color = tuple(srgb_to_lin(int(SUN_COLOUR[i:i + 2], 16) / 255) for i in (1, 3, 5))
    so.data.energy = arg("--sun", 4.2)


def set_nose(o, yaw, theta):
    """Turn car `o` so its nose (-Y) points at screen heading theta (deg) for a camera at `yaw`."""
    y = math.radians(yaw)
    right, up = Vector((math.cos(y), -math.sin(y), 0)), Vector((math.sin(y), math.cos(y), 0))
    n = right * math.cos(math.radians(theta)) + up * math.sin(math.radians(theta))
    o.rotation_euler = (0, 0, math.atan2(n.x, -n.y))


def render(path, w, h):
    sc = bpy.context.scene
    sc.render.resolution_x, sc.render.resolution_y = w, h
    sc.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


def main():
    out = Path(arg("--out", "out/scratch/vela/067/plates"))
    out.mkdir(parents=True, exist_ok=True)
    ids = [n for n in arg("--ids", "Sedan_Official,Sedan_Getaway,StandIn,Sedan_Rival").split(",") if n]
    yaws = [float(v) for v in arg("--yaws", "30,210").split(",")]
    heads = [float(v) for v in arg("--headings", "45,135,225,315").split(",")]
    views = arg("--views", "play").split(",")
    pw, ph = [int(v) for v in arg("--plate", "360x260").split("x")]
    cam, ground = R8.studio(pw, arg("--samples", 40))
    sc = bpy.context.scene
    sc.render.film_transparent = True
    sc.render.image_settings.color_mode = "RGBA"
    # the game grades with ACES (+18 saturation, +6 contrast) under a warm low sun; Khronos PBR Neutral keeps the base colours closest to what its crops show (checked against the official car's crop)
    sc.view_settings.view_transform, sc.view_settings.look = arg("--vt", "Khronos PBR Neutral"), arg("--look", "None")
    sc.view_settings.exposure = arg("--exp", 0.05)
    ambient_world(sc.world, arg("--env", 0.45), arg("--amb", 0.9))
    sc.cycles.sample_clamp_indirect = 3.0  # fireflies: the glossy rays of the HDRI mix and the shadow catcher
    sc.cycles.sample_clamp_direct = 12.0
    for kv in [x for x in arg("--set", "").split(",") if x]:  # colour trials without editing the module: --set DK_Enamel_Plum=#4A2C69,DK_Enamel_Ink=#1B1723
        k, v = kv.split("=")
        kind, *rest = mats.LIB[k]
        mats.LIB[k] = (kind, v, *rest)
    ground.is_shadow_catcher = True
    sun = bpy.data.objects["Sun"]
    sun_rig(sc, sun)
    cam.data.clip_start, cam.data.clip_end = 0.1, 800
    manifest = dict(ppm=PPM, plate=[pw, ph], plates=[])
    for cid in ids:
        o = build(cid)
        lo, hi = R8.bounds(o)
        c = (lo + hi) / 2
        c.z = 0.55
        for view in views:
            if view == "play" or view == "close":
                scale_ppm = PPM if view == "play" else arg("--close-ppm", 120.0)
                w, h = (pw, ph) if view == "play" else (arg("--close-w", 720), arg("--close-h", 520))
                for yaw in yaws:
                    d = -forward(yaw, 38)
                    cam.data.type = "ORTHO"
                    cam.data.ortho_scale = w / scale_ppm
                    R8.look(cam, Vector((0, 0, 0.55)) + d * 300, Vector((0, 0, 0.55)))
                    for th in heads:
                        set_nose(o, yaw, th)
                        name = f"{cid}.{view}.y{int(yaw)}.h{int(th)}.png"
                        render(out / name, w, h)
                        manifest["plates"].append(dict(file=name, id=cid, view=view, yaw=yaw, heading=th, ppm=scale_ppm, size=[w, h]))
            elif view in ("top", "side"):  # nose to the left, tail to the right, the long way across the plate
                w, h = arg("--close-w", 720), arg("--close-h", 360)
                sc_ppm = arg("--close-ppm", 120.0)
                cam.data.type = "ORTHO"
                cam.data.ortho_scale = w / sc_ppm
                if view == "top":
                    o.rotation_euler = (0, 0, -math.pi / 2)  # nose (-Y) turned to -X
                    cam.location, cam.rotation_euler = Vector((0, 0, 300)), (0, 0, 0)  # straight down, screen up = +Y
                else:
                    o.rotation_euler = (0, 0, 0)  # nose -Y; from +X looking to -X, screen right = +Y: the nose is on the left
                    R8.look(cam, Vector((300, 0, 0.55)), Vector((0, 0, 0.55)))
                name = f"{cid}.{view}.png"
                render(out / name, w, h)
                manifest["plates"].append(dict(file=name, id=cid, view=view, ppm=sc_ppm, size=[w, h]))
            elif view == "eye":
                w, h = arg("--close-w", 720), arg("--close-h", 520)
                cam.data.type, cam.data.lens, cam.data.sensor_width = "PERSP", arg("--lens", 42), 36
                o.rotation_euler = (0, 0, 0)
                d_eye, h_eye = arg("--eye-dist", 9.0), arg("--eye-h", 1.7)
                for tag, az in (("front", -38), ("rear", 142)):  # 3/4 front and 3/4 rear from the car's near side, eye 1.7 m
                    a = math.radians(az)
                    R8.look(cam, Vector((math.sin(a) * d_eye, -math.cos(a) * d_eye, h_eye)), Vector((0, 0, 0.62)))
                    name = f"{cid}.eye.{tag}.png"
                    render(out / name, w, h)
                    manifest["plates"].append(dict(file=name, id=cid, view=f"eye.{tag}", size=[w, h]))
        for k in [o, *o.children_recursive]:
            bpy.data.objects.remove(k, do_unlink=True)
    (out / "plates.json").write_text(json.dumps(manifest, indent=1))
    print(f"[C67] {len(manifest['plates'])} plates -> {out}")


main()
