"""Brief 101 (Vela): review renders of the square's lamp mast (civic_mast101.py) from its exported FBX, EEVEE, the kit's material rows x the baked vertex AO.

/Applications/Blender.app/Contents/MacOS/Blender -b -P tools/decokit/review101_mast.py -- <out_dir> <decokit_root> [--lamp]
  play_y030.png / play_y210.png   the play camera (orthographic, 38 deg down) from the opening yaw 30 and from 210, at the Air's fullscreen density
                                  (2880 x 1864 at play zoom 10: 93.2 px/m); *_1080.png the same at 1080p (54 px/m). The piece stands as the world
                                  stands it at B: plan yaw -30 deg, so the opening camera looks along Blender -Y (kit +Z = plan 60 deg). The four
                                  figures (1.75 m) stand on the cell's start marks (contracts/missions/handover/mission.json crew, block frame).
  check_y030.png                  the opening view with each head's facing drawn on the ground (yaw + k x 60): the pods must run parallel to them.
  34.png / crown_34.png           a three-quarter view of the whole mast from the lens side, and a close one of the crown; crown_back_34.png from the cell's side.
  lamp_vs_mast_34.png (--lamp)    today's terrace lamp (DK_PlazaSphereLamp_Play) beside the mast.
  Measures each lens part from the export (lens_check: head k at k x 60 deg from kit +X toward +Z, 7.68 m up; PASS or FAIL) and prints civic_mast101.LENS.
... --capture <png> <out.png>     instead: the mast composited into a real opening capture (out/batch/shots/start-overhead-day.png), Cycles, a shadow catcher."""
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

A = sys.argv[sys.argv.index("--") + 1:]
OUT, ROOT, LAMP = Path(A[0]), Path(A[1]), "--lamp" in A
OUT.mkdir(parents=True, exist_ok=True)
ROWS = {r["name"]: r for r in json.loads((ROOT / "decokit_materials.json").read_text())["materials"]}
AIR, P1080 = 1864 / 20, 1080 / 20                       # px per metre at play zoom 10 (half-height 10 m)
MAST = (-3.85, -42.55)                                  # the mast's foot, block frame (Vantage's B)
CELL = {"Fixer": (-0.2, -46.6), "Tech": (1.2, -44.6), "Scout": (-1.6, -44.3), "Enforcer": (2.7, -47.6)}   # mission.json crew, block frame
YAW = math.radians(-30)                                 # the item's plan yaw at B


def lin(c):
    return tuple(x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c)


def blender_of_block(p):
    """A block point -> the piece's Blender frame (pivot at the mast's foot): block (x south, y east) -> plan (y, -x) -> kit (along yaw, across) -> Blender (-kit x, -kit z)."""
    bx, by = p[0] - MAST[0], p[1] - MAST[1]
    px, py = by, -bx
    kx = px * math.cos(YAW) + py * math.sin(YAW)
    kz = -px * math.sin(YAW) + py * math.cos(YAW)
    return (-kx, -kz)


def setup():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE"
    sc.view_settings.view_transform = "AgX"
    sc.view_settings.look = "AgX - Medium High Contrast"
    sc.world = bpy.data.worlds.new("w")
    sc.world.use_nodes = True
    nt = sc.world.node_tree   # a soft day sky over a warm stone ground: the play look's chrome is the street mirrored, never a dark photo
    tc, sep, mr, cr = (nt.nodes.new(n) for n in ("ShaderNodeTexCoord", "ShaderNodeSeparateXYZ", "ShaderNodeMapRange", "ShaderNodeValToRGB"))
    mr.inputs["From Min"].default_value, mr.inputs["From Max"].default_value = -1.0, 1.0
    els = cr.color_ramp.elements
    for i, (pos, c) in enumerate(((0.0, (0.55, 0.5, 0.44)), (0.47, (0.86, 0.8, 0.72)), (0.505, (0.95, 0.93, 0.9)), (0.72, (0.74, 0.83, 0.94)), (1.0, (0.5, 0.65, 0.88)))):
        e = els[i] if i < len(els) else els.new(pos)
        e.position, e.color = pos, (*lin(c), 1)
    nt.links.new(tc.outputs["Generated"], sep.inputs[0])
    nt.links.new(sep.outputs["Z"], mr.inputs["Value"])
    nt.links.new(mr.outputs["Result"], cr.inputs["Fac"])
    nt.links.new(cr.outputs["Color"], nt.nodes["Background"].inputs[0])
    nt.nodes["Background"].inputs[1].default_value = 1.0
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy, sun.data.color, sun.data.angle = 3.4, (1.0, 0.93, 0.82), math.radians(2.5)
    to_sun = Vector((0.62, 0.38, 0.69)).normalized()    # from upper left of the opening view: shadows fall to its right, as in the game's shots
    sun.rotation_euler = (-to_sun).to_track_quat("-Z", "Y").to_euler()
    sc.collection.objects.link(sun)
    bpy.ops.mesh.primitive_plane_add(size=80)
    g = bpy.context.object
    m = bpy.data.materials.new("pave")
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*lin((0.93, 0.89, 0.81)), 1)
    b.inputs["Roughness"].default_value = 0.85
    g.data.materials.append(m)
    return sc


def dress(objs):
    for o in objs:
        has = bool(o.data.color_attributes)
        for slot in o.material_slots:
            m = slot.material
            if not m or m.get("dk101"):
                continue
            m["dk101"] = 1
            nm = m.name.split(".")[0]
            r = ROWS.get(nm, {"kind": "enamel", "color": list(m.diffuse_color)[:3], "emission": 0.0, "texture": ""})
            m.use_nodes = True
            nt = m.node_tree
            b = nt.nodes.get("Principled BSDF")
            base = (*lin(r["color"]), 1.0)
            kind = r["kind"]
            b.inputs["Base Color"].default_value = base
            b.inputs["Metallic"].default_value = 1.0 if kind in ("chrome", "metal") else 0.6 if kind == "brass" else 0.0
            b.inputs["Roughness"].default_value = {"chrome": 0.12, "metal": 0.3, "brass": 0.35, "stone": 0.75, "fabric": 0.85, "glass": 0.05}.get(kind, 0.45)
            if nm == "DK_Eye_Lens":
                b.inputs["Roughness"].default_value = 0.06
            elif kind == "emit":
                b.inputs["Emission Color"].default_value = base
                b.inputs["Emission Strength"].default_value = 2.5
            col = None
            if r.get("texture"):
                tex = nt.nodes.new("ShaderNodeTexImage")
                tex.image = bpy.data.images.load(str(ROOT / "Textures" / r["texture"]))
                col = tex.outputs["Color"]
            if has and kind not in ("emit", "glass"):   # occlusion = vertex colour R into the base colour
                at = nt.nodes.new("ShaderNodeVertexColor")
                at.layer_name = o.data.color_attributes[0].name
                mul = nt.nodes.new("ShaderNodeMix")
                mul.data_type, mul.blend_type = "RGBA", "MULTIPLY"
                mul.inputs[0].default_value = 1.0
                if col is not None:
                    nt.links.new(col, mul.inputs[6])
                else:
                    mul.inputs[6].default_value = base
                nt.links.new(at.outputs["Color"], mul.inputs[7])
                col = mul.outputs[2]
            if col is not None:
                nt.links.new(col, b.inputs["Base Color"])


def load(fbx, at=(0.0, 0.0)):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(fbx), colors_type="SRGB")
    objs = [o for o in bpy.data.objects if o not in before and o.type == "MESH"]
    for o in objs:
        o.location.x += at[0]
        o.location.y += at[1]
    dress(objs)
    return objs


def figure(name, xy, colour=(0.33, 0.39, 0.45)):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (*lin(colour), 1)
    m.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.6
    bpy.ops.mesh.primitive_cylinder_add(radius=0.2, depth=1.45, location=(xy[0], xy[1], 0.725), vertices=16)
    body = bpy.context.object
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.13, location=(xy[0], xy[1], 1.62), segments=16, ring_count=8)
    head = bpy.context.object
    for o in (body, head):
        o.data.materials.append(m)
        o.name = name


def strip(a_deg, length=9.0, w=0.05, colour=(0.12, 0.2, 0.2)):
    """A thin dark line on the ground from the foot along Blender angle a (a head's facing)."""
    a = math.radians(a_deg)
    bpy.ops.mesh.primitive_plane_add(size=1, location=(math.cos(a) * length / 2, math.sin(a) * length / 2, 0.004))
    o = bpy.context.object
    o.scale = (length, w, 1)
    o.rotation_euler = (0, 0, a)
    m = bpy.data.materials.new("strip")
    m.use_nodes = True
    m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (*lin(colour), 1)
    o.data.materials.append(m)


def cam(sc, target, yaw, pitch, dist, ortho=None, res=(1000, 1000), lens=50):
    """yaw 180 = from Blender +Y (the opening view of the piece at B), 0 = from -Y (yaw 210)."""
    c = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    sc.collection.objects.link(c)
    y, p = math.radians(yaw), math.radians(pitch)
    d = Vector((math.sin(y) * math.cos(p), -math.cos(y) * math.cos(p), math.sin(p)))
    c.location = Vector(target) + d * dist
    c.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
    if ortho:
        c.data.type, c.data.ortho_scale = "ORTHO", ortho
    else:
        c.data.lens = lens
    c.data.clip_end = 500
    sc.camera = c
    sc.render.resolution_x, sc.render.resolution_y = res
    return c


def shoot(sc, name):
    sc.render.filepath = str(OUT / name)
    bpy.ops.render.render(write_still=True)


def play_frame(yaw):
    """Target and size of the play crop: the mast, its crown and the four figures, with a margin."""
    # screen-up of a point (x, y, z) for this camera: -y sin38 + z cos38 (yaw 180) or y sin38 + z cos38 (yaw 0)
    s, c = math.sin(math.radians(38)), math.cos(math.radians(38))
    sign = -1 if yaw == 180 else 1
    figs = [blender_of_block(p) for p in CELL.values()]
    vs = [sign * y * s for _, y in figs] + [sign * y * s + 1.75 * c for _, y in figs] + [8.3 * c + 0.9 * s, 0.0]
    xs = [x for x, _ in figs] + [-1.2, 1.2]
    v0, v1, x0, x1 = min(vs) - 1.2, max(vs) + 1.0, min(xs) - 2.4, max(xs) + 2.4
    return (v0 + v1) / 2, v1 - v0, x1 - x0, (x0 + x1) / 2


def play(sc, yaw, name, ppm):
    vm, h, w, xm = play_frame(yaw)
    s, c = math.sin(math.radians(38)), math.cos(math.radians(38))
    # the point on the view axis whose screen-up is vm: along screen-up u = (0, -sign s, c) from the origin
    sign = -1 if yaw == 180 else 1
    target = Vector((xm, 0.0, 0.0)) + Vector((0.0, sign * s, c)) * vm
    res = (int(round(w * ppm / 2) * 2), int(round(h * ppm / 2) * 2))
    cam(sc, target, yaw, 38, 60, ortho=max(res) / ppm, res=res)
    shoot(sc, name)


def lens_check(objs):
    """Each lens part's centre as exported, in kit axes (x = -Blender x, y = Blender z, z = -Blender y): head k must look along kit +X turned k x 60 deg
    toward +Z (the world export's facings[k] once the piece stands at its item's yaw), its centre 7.68 m up. Prints one line per head and PASS or FAIL."""
    ok = True
    for k in range(3):
        o = next(o for o in objs if o.name.startswith(f"DK_MastCamera_Lamp_Lens{k}"))
        pts = [o.matrix_world @ Vector(c) for c in o.bound_box]
        c = sum(pts, Vector()) / 8
        kx, ky, kz = -c.x, c.z, -c.y
        ang = math.degrees(math.atan2(kz, kx))
        good = abs(ang - 60 * k) < 0.5 and abs(ky - 7.7) < 0.08
        ok &= good
        print(f"[101] lens {k}: kit centre ({kx:.3f}, {ky:.3f}, {kz:.3f}), bearing {ang:.2f} deg from kit +X toward +Z (want {60 * k}), {'ok' if good else 'WRONG'}")
    print("[101] lens check " + ("PASS" if ok else "FAIL"))


def review():
    sc = setup()
    mast = load(ROOT / "Models/Signals/DK_MastCamera_Lamp.fbx")
    lens_check(mast)
    for n, p in CELL.items():
        figure(n, blender_of_block(p))
    play(sc, 180, "play_y030.png", AIR)
    play(sc, 0, "play_y210.png", AIR)
    play(sc, 180, "play_y030_1080.png", P1080)
    play(sc, 0, "play_y210_1080.png", P1080)
    for k in range(3):
        strip(180 + 60 * k)
    play(sc, 180, "check_y030.png", AIR)
    for o in [o for o in bpy.data.objects if o.name.startswith("Plane.") or (o.type == "MESH" and o.name.split(".")[0] in CELL)]:
        bpy.data.objects.remove(o)
    figure("scale", (0.9, -1.3))                                       # one 1.75 m figure beside the plinth, for scale
    cam(sc, (-0.2, -0.3, 4.3), -28, 11, 16.0, res=(1100, 1500), lens=50)
    shoot(sc, "34.png")                                                # the front three-quarter: the cameras' lenses
    cam(sc, (-0.25, -0.5, 7.72), -22, 17, 3.7, res=(1400, 1000), lens=50)
    shoot(sc, "crown_34.png")
    cam(sc, (-0.3, -0.3, 7.75), 168, 30, 3.9, res=(1400, 1000), lens=50)
    shoot(sc, "crown_back_34.png")                                     # from the cell's side: the backs, the lenses away
    if LAMP:
        for o in [o for o in bpy.data.objects if o.type == "MESH" and o.name.split(".")[0] == "scale"]:
            bpy.data.objects.remove(o)
        load(ROOT / "Models/Signals/DK_PlazaSphereLamp_Play.fbx", at=(2.6, 0.0))
        cam(sc, (1.0, 0.0, 4.0), 200, 10, 20.0, res=(1400, 1500), lens=50)
        shoot(sc, "lamp_vs_mast_34.png")



sys.path.insert(0, str(Path(__file__).parent))
try:
    import civic_mast101
    print("[101] LENS " + json.dumps(civic_mast101.LENS))
except Exception as e:  # noqa: BLE001  (the review still stands without the printout)
    print(f"[101] LENS not printed: {e!r}")


# ----------------------------------------------------------------------------- the opening capture, with the mast composited in (--capture <png> <out.png>)
# The play camera's own projection (Vantage's mock, briefs/101/vantage/square-camera.md §6): orthographic, half-height 10 m, Euler(38, 30, 0), framed on
# HostLess.OpeningFocus (block -2.5, -40.5 m, street level; the pavement stands 0.18 m above it). The mast is drawn where today's lamp-terrace-w stands
# (block -3.7, -42.4; Vantage's site is 0.15 m off it, 8 px at 1080p) so it covers that lamp; Cycles, a shadow catcher on the pavement, the sun from
# the capture's own shadows (screen right, a little toward the camera).
def composite(capture, out):
    global MAST
    MAST = (-3.7, -42.4)
    sc = setup()
    sc.render.engine = "CYCLES"
    sc.cycles.samples, sc.cycles.use_denoising = 96, True
    try:   # the Mac's GPU when Cycles has it (Metal), else the CPU
        cp = bpy.context.preferences.addons["cycles"].preferences
        cp.compute_device_type = "METAL"
        cp.get_devices()
        for d in cp.devices:
            d.use = True
        sc.cycles.device = "GPU"
    except Exception:  # noqa: BLE001
        pass
    sc.render.film_transparent = True
    sc.view_settings.view_transform, sc.view_settings.look = "Standard", "None"
    sun = bpy.data.objects["sun"]
    sun.rotation_euler = (-Vector((-0.58, 0.3, 0.76)).normalized()).to_track_quat("-Z", "Y").to_euler()
    ground = [o for o in bpy.data.objects if o.name == "Plane"][0]
    ground.is_shadow_catcher = True
    load(ROOT / "Models/Signals/DK_MastCamera_Lamp.fbx")
    fx, fy = blender_of_block((-2.5, -40.5))
    cam(sc, (fx, fy, -0.18), 180, 38, 60, ortho=1920 / P1080, res=(1920, 1080))
    sc.render.use_border, sc.render.use_crop_to_border = True, False
    sc.render.border_min_x, sc.render.border_max_x, sc.render.border_min_y, sc.render.border_max_y = 0.2, 0.6, 1 - 0.7, 1 - 0.05
    sc.render.filepath = str(OUT / "_mast_layer.png")
    bpy.ops.render.render(write_still=True)
    import numpy as np
    lay = bpy.data.images.load(str(OUT / "_mast_layer.png"))
    a = np.array(lay.pixels[:], dtype=np.float32).reshape(1080, 1920, 4)[::-1]
    base = bpy.data.images.load(str(capture))
    b = np.array(base.pixels[:], dtype=np.float32).reshape(1080, 1920, 4)[::-1]
    rgb = a[..., :3] * a[..., 3:4] + b[..., :3] * (1 - a[..., 3:4])   # PNG alpha is straight: the layer over the capture, both sRGB
    outimg = bpy.data.images.new("comp", 1920, 1080, alpha=False)
    outimg.pixels[:] = np.concatenate([rgb, np.ones((1080, 1920, 1), np.float32)], axis=2)[::-1].ravel()
    outimg.filepath_raw, outimg.file_format = str(out), "PNG"
    outimg.save()


if "--capture" in A:
    i = A.index("--capture")
    composite(Path(A[i + 1]), Path(A[i + 2]))
else:
    review()
