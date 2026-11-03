"""Brief 070 (Vela): plates of the getaway car before (Sedan_Getaway, adopted) and after (Sedan_Getaway_v2, candidate), and Vale's car with the same lift (render-only). Transparent RGBA
renders over a shadow catcher, lit as cars067_plates.py lights the sedans (the game's sun, a warm flat ambient, the street HDRI for reflections, Khronos PBR Neutral); cars070_compose.py lays them.

Blender -b -P cars070_plates.py -- --out DIR [--ids Sedan_Getaway,Sedan_Getaway_v2,Sedan_Official,Sedan_Official_v2] [--views still,play,close,eye] [--samples 64]
  still  the briefing page's getaway still (briefs/070/build/0133/stills/brief-getaway.jpg): ShotRunner's car pose, the camera at the view's (1.8, 5.8, 11.5) m, looking at (0, 0.4, 0),
         32 degrees vertical, 1920 x 1080; the car turned along its exit lane as the still shows it (--still-rot, degrees, fitted to the frame)
  play   the play camera: orthographic, 38 degrees down, yaw 30, 38.57 px per metre (28 m over 1080 px), the nose toward the camera (315) and away (135)
  close  the same angle at 150 px per metre;  eye  1.5 m eye height, front and rear three-quarter
Builds through kit.build, the exporter's own path (Sedan_Official_v2 through cars070.sedan_v2: it is not catalogued)."""

import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).parent))
import render008 as R8  # noqa: E402
import kit  # noqa: E402
from geo import Shape  # noqa: E402

kit.load()
import cars070  # noqa: E402

arg = R8.arg
R8.HDRI = Path(arg("--hdri", str(Path(__file__).resolve().parents[2] / "out/decokit/HDRI/city.exr")))
SUN_YAW, SUN_PITCH, SUN_COLOUR = 330.0, 38.0, "#ffe0d0"  # unity/Assets/Suborned/Editor/World/light.json
PPM = 38.57


def lin(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def ambient_world(world, hdri_strength, amb):
    nt = world.node_tree
    hd = nt.nodes["Background"]
    hd.inputs["Strength"].default_value = hdri_strength
    flat = nt.nodes.new("ShaderNodeBackground")
    flat.inputs["Color"].default_value = (lin(0xf3 / 255), lin(0xe6 / 255), lin(0xd8 / 255), 1.0)
    flat.inputs["Strength"].default_value = amb
    lp, mix = nt.nodes.new("ShaderNodeLightPath"), nt.nodes.new("ShaderNodeMixShader")
    out = next(n for n in nt.nodes if n.type == "OUTPUT_WORLD")
    nt.links.new(lp.outputs["Is Glossy Ray"], mix.inputs[0])
    nt.links.new(flat.outputs["Background"], mix.inputs[1])
    nt.links.new(hd.outputs["Background"], mix.inputs[2])
    nt.links.new(mix.outputs["Shader"], out.inputs["Surface"])


def forward(yaw, pitch):
    y, p = math.radians(yaw), math.radians(pitch)
    return Vector((math.sin(y) * math.cos(p), math.cos(y) * math.cos(p), -math.sin(p)))


def set_nose(o, yaw, theta):
    """Turn car o so its nose (-Y) points at screen heading theta (deg: 0 right, 90 up the screen, 270 toward the camera) for a camera at yaw."""
    y = math.radians(yaw)
    right, up = Vector((math.cos(y), -math.sin(y), 0)), Vector((math.sin(y), math.cos(y), 0))
    n = right * math.cos(math.radians(theta)) + up * math.sin(math.radians(theta))
    o.rotation_euler = (0, 0, math.atan2(n.x, -n.y))


def build(cid):
    if cid == "Sedan_Official_v2":  # render-only: Vale's car with the same lift
        s = Shape("DK_Sedan_Official_v2")
        cars070.sedan_v2(s, "DK_Enamel_Teal", "DK_Enamel_Ivory")
        return s.build(bevel=0.0)
    return kit.build(cid)


def render(path, w, h):
    sc = bpy.context.scene
    sc.render.resolution_x, sc.render.resolution_y = w, h
    sc.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


def main():
    out = Path(arg("--out", "out/scratch/vela/070/plates"))
    out.mkdir(parents=True, exist_ok=True)
    ids = [n for n in arg("--ids", "Sedan_Getaway,Sedan_Getaway_v2,Sedan_Official,Sedan_Official_v2").split(",") if n]
    views = arg("--views", "still,play,close,eye").split(",")
    cam, ground = R8.studio(360, arg("--samples", 64))
    sc = bpy.context.scene
    sc.render.film_transparent = True
    sc.render.image_settings.color_mode = "RGBA"
    sc.view_settings.view_transform, sc.view_settings.look = "Khronos PBR Neutral", "None"
    sc.view_settings.exposure = arg("--exp", 0.05)
    ambient_world(sc.world, arg("--env", 0.45), arg("--amb", 0.9))
    sc.cycles.sample_clamp_indirect, sc.cycles.sample_clamp_direct = 3.0, 12.0
    ground.is_shadow_catcher = True
    sun = bpy.data.objects["Sun"]
    sun.data.color = tuple(lin(int(SUN_COLOUR[i:i + 2], 16) / 255) for i in (1, 3, 5))
    sun.data.energy = arg("--sun", 4.2)
    cam.data.clip_start, cam.data.clip_end = 0.1, 800
    manifest = dict(ppm=PPM, plates=[])
    for cid in ids:
        o = build(cid)
        for view in views:
            if view == "still":
                # the still's sun throws the car's shadow to the screen's left and a little toward the camera: a sun behind-right of the camera, 38 degrees up
                sun.rotation_euler = forward(arg("--still-sun", 160.0), SUN_PITCH).to_track_quat("-Z", "Y").to_euler()
                cam.data.type, cam.data.sensor_fit, cam.data.angle = "PERSP", "VERTICAL", math.radians(32)
                o.rotation_euler = (0, 0, math.radians(arg("--still-rot", 181.0)))
                R8.look(cam, Vector((1.8, 11.5, 5.8)), Vector((0, 0, 0.4)))  # Unity (x, y, z) -> Blender (x, z, y)
                name = f"{cid}.still.png"
                render(out / name, 1920, 1080)
                manifest["plates"].append(dict(file=name, id=cid, view="still", size=[1920, 1080]))
                continue
            sun.rotation_euler = forward(SUN_YAW, SUN_PITCH).to_track_quat("-Z", "Y").to_euler()
            if view in ("play", "close"):
                ppm = PPM if view == "play" else arg("--close-ppm", 150.0)
                w, h = (300, 220) if view == "play" else (arg("--close-w", 1000), arg("--close-h", 760))
                cam.data.type, cam.data.ortho_scale = "ORTHO", w / ppm
                R8.look(cam, Vector((0, 0, 0.55)) - forward(30, 38) * 300, Vector((0, 0, 0.55)))
                for th in (315, 135):
                    set_nose(o, 30, th)
                    name = f"{cid}.{view}.h{th}.png"
                    render(out / name, w, h)
                    manifest["plates"].append(dict(file=name, id=cid, view=view, heading=th, ppm=ppm, size=[w, h]))
            elif view == "eye":
                cam.data.type, cam.data.sensor_fit, cam.data.lens, cam.data.sensor_width = "PERSP", "AUTO", 38, 36
                o.rotation_euler = (0, 0, 0)
                for tag, az in (("front", -38), ("rear", 142)):
                    a = math.radians(az)
                    R8.look(cam, Vector((math.sin(a) * 6.6, -math.cos(a) * 6.6, 1.5)), Vector((0, 0, 0.62)))
                    name = f"{cid}.eye.{tag}.png"
                    render(out / name, 900, 520)
                    manifest["plates"].append(dict(file=name, id=cid, view=f"eye.{tag}", size=[900, 520]))
        for k in [o, *o.children_recursive]:
            bpy.data.objects.remove(k, do_unlink=True)
    (out / "plates.json").write_text(json.dumps(manifest, indent=1))
    print(f"[C70] {len(manifest['plates'])} plates -> {out}")


main()
