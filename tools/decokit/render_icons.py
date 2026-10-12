"""Render every registered asset as a studio icon (Cycles) and compose a contact sheet.

Blender -b -P Tools/Blender/decokit/render_icons.py -- [--only A,B] [--samples 48] [--px 360] [--cam x,y]
Outputs: SourceArt/DecoKit/icons/<Name>.png (+ order.txt); compose with compose_sheet.py
The studio mirrors the reference sheet: cream backdrop, contact shadows (shadow catcher), a soft key
from upper left, and out-of-frame dark flags so chrome has something to reflect.
"""

import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).parent))
import kit  # noqa: E402

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / "SourceArt/DecoKit"
ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []


def arg(key, default):
    return type(default)(ARGS[ARGS.index(key) + 1]) if key in ARGS else default


VIEW = Vector((*map(float, arg("--cam", "-0.55,-0.83").split(",")), arg("--elev", 0.36))).normalized()  # camera
# sits front-left, above (front = -Y); --cam x,y turns it (e.g. -0.2,-0.98 to face a character)


def studio():
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
        print("[DK] cycles devices:", [(d.name, d.type, d.use) for d in prefs.devices])
    except Exception as e:
        print("[DK] GPU setup failed:", e)
    sc.cycles.samples = arg("--samples", 48)
    sc.cycles.use_denoising = True
    sc.render.film_transparent = True
    vt, look = arg("--view", "AgX|AgX - Punchy").split("|")
    sc.view_settings.view_transform = vt
    sc.view_settings.look = look
    sc.view_settings.exposure = arg("--exposure", -0.6)
    w = bpy.data.worlds.new("DK_Studio")  # CC0 Poly Haven HDRI (bundled with Blender): real reflections
    sc.world = w
    nt = w.node_tree
    env, mp, tc = nt.nodes.new("ShaderNodeTexEnvironment"), nt.nodes.new("ShaderNodeMapping"), nt.nodes.new("ShaderNodeTexCoord")
    env.image = bpy.data.images.load(str(OUT / "HDRI" / (arg("--hdri", "studio") + ".exr")))
    nt.links.new(tc.outputs["Generated"], mp.inputs["Vector"])
    nt.links.new(mp.outputs["Vector"], env.inputs["Vector"])
    mp.inputs["Rotation"].default_value[2] = math.radians(arg("--hdri-rot", 0.0))
    nt.links.new(env.outputs["Color"], nt.nodes["Background"].inputs["Color"])
    nt.nodes["Background"].inputs["Strength"].default_value = arg("--hdri-strength", 1.0)
    rig = bpy.data.objects.new("Rig", None)
    sc.collection.objects.link(rig)
    bpy.ops.mesh.primitive_plane_add(size=60)
    ground = bpy.context.object
    ground.is_shadow_catcher = True
    fm = bpy.data.materials.new("Floor")  # reflections see cream close in, falling to dark (product sweep)
    ft = fm.node_tree
    tco, ln, fr = ft.nodes.new("ShaderNodeTexCoord"), ft.nodes.new("ShaderNodeVectorMath"), ft.nodes.new("ShaderNodeValToRGB")
    ln.operation = "LENGTH"
    ft.links.new(tco.outputs["Object"], ln.inputs[0])
    ft.links.new(ln.outputs["Value"], fr.inputs[0])
    fr.color_ramp.elements[0].position, fr.color_ramp.elements[0].color = 0.35, (0.9, 0.86, 0.76, 1)
    fr.color_ramp.elements[1].position, fr.color_ramp.elements[1].color = 1.4, (0.05, 0.05, 0.05, 1)
    ft.links.new(fr.outputs["Color"], ft.nodes["Principled BSDF"].inputs["Base Color"])
    ground.data.materials.append(fm)
    ground.parent = rig
    key = bpy.data.lights.new("Key", "AREA")
    key.size, key.color = 3, (1.0, 0.96, 0.9)
    ko = bpy.data.objects.new("Key", key)
    sc.collection.objects.link(ko)
    ko.location = (-3, -2.5, 4.5)
    ko.rotation_euler = (math.radians(40), 0, math.radians(-50))
    ko.parent = rig
    # softboxes (camera-invisible emitters): overhead panel, tall left strip, right rim strip
    for name, size, loc, rot, power in (("Top", (3, 3), (0, 0, 5), (0, 0, 0), 4.0),
                                        ("Left", (0.8, 4), (-4, -1, 2), (0, 90, -20), 9.0),
                                        ("Rim", (0.5, 4), (3.5, 2.5, 2), (0, -90, 30), 6.0)):
        bpy.ops.mesh.primitive_plane_add(size=1, location=loc, rotation=[math.radians(a) for a in rot])
        f = bpy.context.object
        f.scale = (*size, 1)
        f.visible_camera = f.visible_shadow = False
        m = bpy.data.materials.new(name)
        b = m.node_tree.nodes["Principled BSDF"]
        b.inputs["Base Color"].default_value = (0, 0, 0, 1)
        b.inputs["Emission Color"].default_value = (1, 0.98, 0.95, 1)
        b.inputs["Emission Strength"].default_value = power
        f.data.materials.append(m)
        f.parent = rig
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    cam.data.lens = 70
    sc.collection.objects.link(cam)
    sc.camera = cam
    return cam


def frame(cam, obj):
    dg = bpy.context.evaluated_depsgraph_get()
    ev = obj.evaluated_get(dg)
    pts = [obj.matrix_world @ Vector(c) for c in ev.bound_box]
    lo = Vector([min(p[i] for p in pts) for i in range(3)])
    hi = Vector([max(p[i] for p in pts) for i in range(3)])
    c, r = (lo + hi) / 2, (hi - lo).length / 2
    fov = 2 * math.atan(18 / cam.data.lens)
    cam.location = c + VIEW * (r / math.sin(fov / 2) * 1.02)
    rig = bpy.data.objects["Rig"]  # studio scales with the asset: same light on a hydrant and a tram
    k = max(r, 0.5) / 0.6
    rig.location, rig.scale = (c.x, c.y, min(lo.z, 0)), (k, k, k)  # floor under sub-grade parts (rails, sumps)
    bpy.data.lights["Key"].energy = 900 * k * k
    cam.rotation_euler = (-VIEW).to_track_quat("-Z", "Y").to_euler()


def main():
    reg = kit.load()
    only = [n for n in arg("--only", "").split(",") if n]
    names = only or list(reg)
    px = arg("--px", 360)
    cam = studio()
    sc = bpy.context.scene
    sc.render.resolution_x = sc.render.resolution_y = px
    icons = OUT / arg("--dir", "icons")
    icons.mkdir(parents=True, exist_ok=True)
    objs = {n: kit.build(n) for n in names}
    for n, o in objs.items():
        for other in objs.values():
            other.hide_render = other is not o
        frame(cam, o)
        sc.render.filepath = str(icons / f"{n}.png")
        bpy.ops.render.render(write_still=True)
        tris = sum(len(p.vertices) - 2 for p in o.evaluated_get(bpy.context.evaluated_depsgraph_get()).data.polygons)
        print(f"[DK] {n:18s} {o['dk_cat']:10s} tris={tris}")
    # every icon on disk in registry order, so an --only re-run never truncates the sheet list
    (icons / "order.txt").write_text("\n".join(n for n in reg if (icons / f"{n}.png").exists()))


main()
