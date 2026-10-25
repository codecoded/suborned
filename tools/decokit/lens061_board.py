"""Frames for the brief 061 piece sheet (Vela, 2026-10-25): the built SignalLens / HaloRing / LightDotRow / Kerb_Pearl / RoadRibbon, as the generator builds them, in the four signal states, and the
re-cut pieces beside what they replace.

blender -b -P tools/decokit/lens061_board.py -- <out_dir> <scene> [--res WxH] [--old-from-git HEAD]
  scene  face-<state>   the lens face-on at eye level (6.3 m, fov 13), state = walk | clearing | stop | arriving
         edge-<state>   the same lens seen edge-on from the side (the rim and the ring say the state without the face)
         play-<state>   the Records-Office-sized crossing (13 m carriageway) at the play camera (ortho 14, pitch 38, yaw 30), two heads, two rings, two dot rows
         close-<state>  the same crossing, tight (ortho 3.2) on the near kerb end
         recut-before | recut-after    the road edge and the dot rows close at the play camera (ortho 2.6): the pieces as committed (`--old-from-git`) and as re-cut
         measure        no render: writes tris.json (triangles per piece, level and part, from the built objects)
Writes <scene>.lin.npy (linear, Raw) for road_board_post.py frame; the state is driven the way Latch drives it: _EmissionColor, _BaseMap_ST, _Cutoff, the dots' u gate.
Not the game's pipeline: Eevee, the light.json sun and ambient, no SSAO or contact shade; the post is road_board_post.py's numpy ACES + bloom."""

import json
import math
import subprocess
import sys
from pathlib import Path

import bpy
import numpy as np
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).parent))
import kit  # noqa: E402
import mats  # noqa: E402

REPO = Path(__file__).resolve().parents[2]
A = sys.argv[sys.argv.index("--") + 1:]
OUT, SCENE = Path(A[0]), A[1]
RES = tuple(int(x) for x in (A[A.index("--res") + 1] if "--res" in A else "1920x1080").split("x"))
GIT = A[A.index("--old-from-git") + 1] if "--old-from-git" in A else "HEAD"
OUT.mkdir(parents=True, exist_ok=True)
LIGHT = json.loads((REPO / "unity/Assets/Suborned/Editor/World/light.json").read_text())
LOOK = json.loads((REPO / "unity/Assets/Suborned/Editor/World/look.json").read_text())
SC = None
PAV = 0.154   # the kerb stone's top: the stage's pavement lies on it


def hexrgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) / 255 for i in (0, 2, 4))


def lin(c):
    return tuple(x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c)


def pal(key):
    return hexrgb({**LOOK["palette"], **LIGHT["palette"]}[key]["hex"])


# ----------------------------------------------------------------------------- the committed pieces ("before")
def load_old():
    """The pieces as committed, from git: their sources renamed X -> X_Old so they register beside the re-cut ones."""
    import importlib
    import re
    d = OUT / "old"
    d.mkdir(exist_ok=True)
    for mod in ("crossing_flat", "pave026", "road_ribbon"):
        src = subprocess.run(["git", "-C", str(REPO), "show", f"{GIT}:tools/decokit/{mod}.py"], capture_output=True, text=True, check=True).stdout
        src = re.sub(r'@asset\("(\w+)"', r'@asset("\1_Old"', src)
        src = src.replace('asset(f"Kerb_Pearl_{_i}"', 'asset(f"Kerb_Pearl_{_i}_Old"')
        (d / f"old_{mod}.py").write_text(src)
    sys.path.insert(0, str(d))
    for mod in ("crossing_flat", "pave026", "road_ribbon"):
        importlib.import_module(f"old_{mod}")


# ----------------------------------------------------------------------------- the world
def make_world():
    w = bpy.data.worlds.new("w")
    w.use_nodes = True
    nt = w.node_tree
    for n in list(nt.nodes):
        nt.nodes.remove(n)
    tc, sep, ramp, bg, out = (nt.nodes.new(t) for t in ("ShaderNodeTexCoord", "ShaderNodeSeparateXYZ", "ShaderNodeValToRGB", "ShaderNodeBackground", "ShaderNodeOutputWorld"))
    nt.links.new(tc.outputs["Generated"], sep.inputs["Vector"])
    e = ramp.color_ramp.elements
    e[0].position, e[0].color = 0.50, (*lin(pal("light.haze")), 1)
    e[1].position, e[1].color = 1.0, (*lin(pal("light.zenith")), 1)
    e.new(0.62).color = (*lin(pal("light.skyMid")), 1)
    nt.links.new(sep.outputs["Z"], ramp.inputs["Fac"])
    nt.links.new(ramp.outputs["Color"], bg.inputs["Color"])
    amb, lp, mix = nt.nodes.new("ShaderNodeBackground"), nt.nodes.new("ShaderNodeLightPath"), nt.nodes.new("ShaderNodeMixShader")
    amb.inputs["Color"].default_value = (*lin(pal("light.ambientEquator")), 1)
    amb.inputs["Strength"].default_value = 0.34
    nt.links.new(lp.outputs["Is Camera Ray"], mix.inputs["Fac"])
    nt.links.new(amb.outputs["Background"], mix.inputs[1])
    nt.links.new(bg.outputs["Background"], mix.inputs[2])
    nt.links.new(mix.outputs["Shader"], out.inputs["Surface"])
    SC.world = w


def sun():
    s = bpy.data.lights.new("sun", "SUN")
    s.energy, s.color, s.angle = 2.3, pal("light.sun"), math.radians(2.0)
    o = bpy.data.objects.new("sun", s)
    p, y = math.radians(LIGHT["sun"]["pitch"]), math.radians(LIGHT["sun"]["yaw"])
    o.rotation_euler = Vector((math.sin(y) * math.cos(p), math.cos(y) * math.cos(p), -math.sin(p))).to_track_quat("-Z", "Y").to_euler()
    SC.collection.objects.link(o)


def play_camera(focus, yaw, ortho, z=0.0):
    cd = bpy.data.cameras.new("cam")
    co = bpy.data.objects.new("cam", cd)
    SC.collection.objects.link(co)
    SC.camera = co
    p, y = math.radians(38), math.radians(yaw)
    fwd = Vector((math.sin(y) * math.cos(p), math.cos(y) * math.cos(p), -math.sin(p)))
    co.location = Vector((focus[0], focus[1], z)) - fwd * 160
    cd.type, cd.ortho_scale, cd.clip_start, cd.clip_end = "ORTHO", 2 * ortho * RES[0] / RES[1], 0.2, 400
    co.rotation_euler = fwd.to_track_quat("-Z", "Y").to_euler()


def eye_camera(eye, target, fov):
    cd = bpy.data.cameras.new("cam")
    co = bpy.data.objects.new("cam", cd)
    SC.collection.objects.link(co)
    SC.camera = co
    cd.type, cd.sensor_fit, cd.angle_y, cd.clip_start, cd.clip_end = "PERSP", "VERTICAL", math.radians(fov), 0.2, 100
    co.location = Vector(eye)
    co.rotation_euler = (Vector(target) - Vector(eye)).to_track_quat("-Z", "Y").to_euler()


# ----------------------------------------------------------------------------- placing
def put(name, loc=(0, 0, 0), yaw=0.0):
    o = kit.build(name)
    for k in [o, *o.children_recursive]:
        k.location = (0, 0, 0)
    o.location = Vector(loc)
    o.rotation_euler = (0, 0, math.radians(yaw))
    bpy.context.view_layer.update()
    return o


def quad_obj(name, pts, mat):
    from geo import Shape
    s = Shape(name)
    s.face(pts, mat)
    return s.build(bevel=0.0)


# ----------------------------------------------------------------------------- driving the state (what Latch's SignalHead writes)
def _principled(m):
    return m.node_tree.nodes["Principled BSDF"]


def drive(obj, colour, strength, st=None, cutoff=None, gate=None, alpha=None):
    """Give `obj` its own copy of its (single) emitting material and set it as a MaterialPropertyBlock would: emission colour x strength, _BaseMap_ST (du, dv) on the glyph, the alpha-clip `cutoff`,
    the dots' u gate (lo, hi), or a glow's alpha."""
    for i, m in enumerate(obj.data.materials):
        if m is None or not m.use_nodes:
            continue
        if not m.name.startswith(("DK_Signal", "DK_Cross")):
            continue
        m = m.copy()
        obj.data.materials[i] = m
        nt = m.node_tree
        b = _principled(m)
        col = (*lin(hexrgb(colour)), 1.0)
        b.inputs["Emission Strength"].default_value = strength
        if m.name.startswith("DK_Cross_Beam") or m.name.startswith("DK_Cross_Halo"):
            b.inputs["Base Color"].default_value = col
            b.inputs["Emission Color"].default_value = col
            b.inputs["Alpha"].default_value = alpha
            continue
        em = b.inputs["Emission Color"]
        src = em.links[0].from_node if em.links else None
        if src is not None and src.type == "MIX":           # the glyph x state colour node get() made
            src.inputs["B"].default_value = col
        else:
            em.default_value = col
        if m.name.startswith("DK_Cross_Light"):
            b.inputs["Base Color"].default_value = col
        tex = next((n for n in nt.nodes if n.type == "TEX_IMAGE"), None)
        if tex is not None:
            tex.interpolation = "Linear"
            tex.extension = "CLIP"
            if st is not None:
                tc, mp = nt.nodes.new("ShaderNodeTexCoord"), nt.nodes.new("ShaderNodeMapping")
                mp.inputs["Location"].default_value = (st[2], st[3], 0)
                mp.inputs["Scale"].default_value = (st[0], st[1], 1)
                nt.links.new(tc.outputs["UV"], mp.inputs["Vector"])
                nt.links.new(mp.outputs["Vector"], tex.inputs["Vector"])
            cut = nt.nodes.new("ShaderNodeMath")
            cut.operation = "GREATER_THAN"
            cut.inputs[1].default_value = cutoff if cutoff is not None else 0.5
            nt.links.new(tex.outputs["Alpha"], cut.inputs[0])
            nt.links.new(cut.outputs["Value"], b.inputs["Alpha"])
            m.surface_render_method = "DITHERED"
        if gate is not None:
            tc, sep, ge, le, mul = (nt.nodes.new(t) for t in ("ShaderNodeTexCoord", "ShaderNodeSeparateXYZ", "ShaderNodeMath", "ShaderNodeMath", "ShaderNodeMath"))
            ge.operation, le.operation, mul.operation = "GREATER_THAN", "LESS_THAN", "MULTIPLY"
            ge.inputs[1].default_value, le.inputs[1].default_value = gate[0] - 1e-4, gate[1] + 1e-4
            nt.links.new(tc.outputs["UV"], sep.inputs["Vector"])
            nt.links.new(sep.outputs["X"], ge.inputs[0])
            nt.links.new(sep.outputs["X"], le.inputs[0])
            nt.links.new(ge.outputs["Value"], mul.inputs[0])
            nt.links.new(le.outputs["Value"], mul.inputs[1])
            nt.links.new(mul.outputs["Value"], b.inputs["Alpha"])
            m.surface_render_method = "DITHERED"


STATE_DATA = None


def state_values(state):
    return STATE_DATA["states"][state]


def clearing_s():
    return 10.0   # the 13 m crossing's clearing seconds


def head_look(state):
    """What the head shows in the frame: figure cell, digits, ring lit count, dot window as the fraction drained from each end, rim brightness."""
    left = 7.0 if state == "clearing" else 3.0 if state == "arriving" else 0.0
    return dict(
        figure={"walk": "walk_full", "clearing": "walk_head", "stop": "stand_full", "arriving": "stand_head"}[state],
        digits={"walk": None, "clearing": "07", "stop": None, "arriving": "03"}[state],
        ring={"walk": 24, "clearing": round(24 * 7 / 10), "stop": 24, "arriving": 3}[state],   # arriving: 3 s of 3 left shows the first segments: the ring fills
        drain=(1 - 7 / 10) if state == "clearing" else 0.0)


def dress_head(head, state, colour, strength):
    look = head_look(state)
    parts = {c.name.split(".")[0].split("_")[-1]: c for c in head.children_recursive}
    tab = STATE_DATA["faces"]["parts"]
    if "--no-digits" in A:
        for k in ("Tens", "Units"):
            bpy.data.objects.remove(parts[k])
        parts.pop("Tens"); parts.pop("Units")
    drive(parts["Rim"], colour, strength)
    drive(parts["Figure"], colour, strength, st=tab["Figure"]["st"][look["figure"]], cutoff=0.5)
    for part, ch in (("Tens", look["digits"][0] if look["digits"] else "blank"), ("Units", look["digits"][1] if look["digits"] else "blank")):
        if part in parts:
            drive(parts[part], colour, strength, st=tab[part]["st"][ch], cutoff=0.5)
    drive(parts["Beam"], colour, 1.0, alpha=STATE_DATA["beam"]["alpha"] * 0.5)
    drive(parts["Halo"], colour, 1.0, alpha=STATE_DATA["halo"]["alpha"] * 0.5)


def dress_ring(ring, state, colour, strength):
    look = head_look(state)
    drive(ring, colour, strength, cutoff=1 - look["ring"] / 24 - 1e-4 if look["ring"] < 24 else 0.0)


def dress_dots(row, state, colour, strength, s0, total):
    """row: a LightDotRow object whose -X end is at global distance s0 along a crossing `total` m long."""
    d = head_look(state)["drain"] * total / 2
    drive(row, colour, strength, gate=(d - s0, total - d - s0))


# ----------------------------------------------------------------------------- the stage
def stage_road(old=False, half=16, w=13.0, pavement_depth=10):
    """Carriageway (enamel), kerbs, ribbons, pavement slabs; the crossing runs along Y across a `w` m road. Returns the kerb y."""
    sfx = "_Old" if old else ""
    road = quad_obj("road", [(-half, -w / 2, 0), (half, -w / 2, 0), (half, w / 2, 0), (-half, w / 2, 0)], mats.get("DK_Ground_RoadEnamel"))
    for side in (-1, 1):                       # pavement on the far side of each kerb: slabs at the kerb top
        for ix in range(-half // 2, half // 2):
            for iy in range(pavement_depth // 2):
                o = put("Pave_Slab", (ix * 2 + 1, side * (w / 2 + iy * 2 + 1), PAV), 0)
        for k in range(-half // 2, half // 2):   # the kerb: pivot = the kerb line at road level, stone on +Y (pavement side)
            put(f"Kerb_Pearl_{k % 2}{sfx}", (k * 2 + 1, side * w / 2, 0), 0 if side > 0 else 180)
        for k in range(-half // 6, half // 6 + 1):  # the strip light: carriageway on +Y; the far kerb mirrors it
            put(f"RoadRibbon_Straight_6m{sfx}", (k * 6, side * (w / 2 - 0.10), 0), 0 if side < 0 else 180)
    return w / 2


def stage_crossing(state, old=False, w=13.0, ox=0.0):
    """The strip's two dot rows (x = ox +/- 1.75) kerb to kerb, a head and a ring at each kerb end. The old build has no lens, ring or seat: the committed crossing is drawn with SignalEye."""
    sfx = "_Old" if old else ""
    colour, strength = STATE_DATA["states"][state]["colour"], STATE_DATA["states"][state]["emission"]["day"]
    mods = [(-w / 2 + 1.0 + 2 * k, "LightDotRow_2") for k in range(int(w // 2))]
    rest = w - 2 * int(w // 2)
    if rest:
        mods.append((w / 2 - rest / 2, "LightDotRow_1"))
    for sx in (-1.75, 1.75):
        for yc, nm in mods:
            L = 2.0 if nm.endswith("2") else 1.0
            row = put(nm + sfx, (ox + sx, yc, 0), 90)
            if not old:
                dress_dots(row, state, colour, strength, yc - L / 2 + w / 2, w)
    for sy, yaw, x in ((-1, 180, ox + 2.7), (1, 0, ox - 2.7)):
        pos = (x, sy * (w / 2 + 0.9), PAV)
        if old:
            eye = put("SignalEye", pos, yaw)
            for c in eye.children_recursive:           # the committed eye shows its Walk figure by default; Latch enables one of Walk / Stop
                if c.name.split(".")[0].endswith("_Stop"):
                    bpy.data.objects.remove(c)
        else:
            h = put("SignalLens", pos, yaw)
            dress_head(h, state, colour, strength)
            r = put("HaloRing", pos, yaw)
            dress_ring(r, state, colour, strength)


def day_road():
    """The strip light as the day build has it: the lens at its day emission (1.4), the light pool off (road_ribbon: alpha is added at evening and night only)."""
    pool = bpy.data.materials.get("DK_Road_Pool")
    if pool:
        _principled(pool).inputs["Alpha"].default_value = 0.0
    lens = bpy.data.materials.get("DK_Road_Lens")
    if lens:
        _principled(lens).inputs["Emission Strength"].default_value = 1.4


def backdrop(at, size=(12, 8), colour="#132c2b"):
    m = bpy.data.materials.new("backdrop")
    b = _principled(m)
    b.inputs["Base Color"].default_value = (*lin(hexrgb(colour)), 1)
    b.inputs["Roughness"].default_value = 0.9
    x, y = at
    quad_obj("wall", [(x - size[0] / 2, y, 0), (x + size[0] / 2, y, 0), (x + size[0] / 2, y, size[1]), (x - size[0] / 2, y, size[1])][::-1], m)


def shoot(name):
    SC.render.image_settings.file_format = "OPEN_EXR"
    SC.render.image_settings.color_depth = "32"
    SC.render.image_settings.color_mode = "RGBA"
    base = OUT / ("_tmp_" + name)
    SC.render.filepath = str(base)
    bpy.ops.render.render(write_still=True)
    f = Path(str(base) + ".exr")
    img = bpy.data.images.load(str(f))
    a = np.array(img.pixels[:], dtype=np.float32).reshape(RES[1], RES[0], 4)[::-1]
    bpy.data.images.remove(img)
    f.unlink()
    np.save(str(OUT / f"{name}.lin.npy"), a[..., :3])
    print("[lens061] wrote", name)


def measure():
    out = {}
    for n in ("SignalLens", "HaloRing", "LightDotRow_2", "LightDotRow_1", "Kerb_Pearl_0", "Kerb_Pearl_1", "RoadRibbon_Straight_6m", "RoadRibbon_Arc90_R4", "RoadRibbon_Arc90_R8",
              "LightDotRow_2_Old", "LightDotRow_1_Old", "Kerb_Pearl_0_Old", "Kerb_Pearl_1_Old", "RoadRibbon_Straight_6m_Old", "RoadRibbon_Arc90_R4_Old", "RoadRibbon_Arc90_R8_Old", "SignalEye"):
        if n not in kit.REGISTRY:
            continue
        lv = {}
        for tag, fn in (("L0", lambda: kit.build(n)), ("L1", lambda: kit.build_lod(n, "L1") if kit.REGISTRY[n].get("near") else None)):
            o = fn()
            if o is None:
                continue
            objs = {}
            for k in [o, *o.children_recursive]:
                objs[k.name] = dict(tris=sum(len(p.vertices) - 2 for p in k.data.polygons), materials=[m.name for m in k.data.materials])
            lv[tag] = dict(tris=sum(v["tris"] for v in objs.values()), objects=objs)
        out[n] = lv
    (OUT / "tris.json").write_text(json.dumps(out, indent=1))
    print("[lens061] tris.json", {k: {l: v["tris"] for l, v in lv.items()} for k, lv in out.items()})


def main():
    global SC, STATE_DATA
    bpy.ops.wm.read_factory_settings(use_empty=True)
    SC = bpy.context.scene
    kit.load(strict=True)
    mats.register_robot_enamels()
    import crossing_lens
    STATE_DATA = crossing_lens.data()
    if SCENE.startswith("recut-before") or SCENE == "measure":
        load_old()
    if SCENE == "measure":
        measure()
        return
    SC.render.engine = "BLENDER_EEVEE"
    SC.render.resolution_x, SC.render.resolution_y = RES
    SC.view_settings.view_transform = "Raw"
    make_world()
    sun()
    kind, _, state = SCENE.partition("-")
    if kind == "face" or kind == "edge":
        h = put("SignalLens", (0, 0, PAV), 0)
        c = STATE_DATA["states"][state]
        dress_head(h, state, c["colour"], c["emission"]["day"])
        r = put("HaloRing", (0, 0, PAV), 0)
        dress_ring(r, state, c["colour"], c["emission"]["day"])
        put("Pave_Slab", (0, -3.0, PAV), 0)
        backdrop((0, 3.2), (14, 9))
        if kind == "face":
            eye_camera((0, -6.3, PAV + 2.55), (0, 0, PAV + 2.55), 13)           # as the 061 pick board: 6.3 m, fov 13, face A square on
        else:
            eye_camera((6.3, 0.0, PAV + 2.55), (0, 0, PAV + 2.55), 13)          # from the side: the lens edge-on, rim and beam
    elif kind in ("play", "close"):
        stage_road(half=30 if kind == "play" else 10, pavement_depth=14 if kind == "play" else 8)
        stage_crossing(state)
        day_road()
        if kind == "play":
            play_camera((0.0, -3.0), 30, 14, PAV)
        else:
            play_camera((-2.1, 7.0), 30, 3.3, PAV + 0.9)
    elif kind == "recut":
        old = state == "before"
        stage_road(old=old, half=10, pavement_depth=8)
        stage_crossing("walk", old=old)     # the dot rows: the committed pieces as they are, the re-cut ones lit in the walk colour
        day_road()
        play_camera((-1.5, 6.2), 30, 2.8, PAV)   # the far kerb: the near one hides the first 0.2 m of road behind it at this camera
    shoot(SCENE)


main()
