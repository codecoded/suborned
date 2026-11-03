"""Frames for brief 079 note 2 (Vela, 2026-10-30): the crossing lights, now and proposed, on one stage, driven the way SignalHead drives them. Review only; the sheet is crossing079_sheet.py.

DECOKIT_CROSS079=1 blender -b -P tools/decokit/crossing079_board.py -- <out_dir> <cand|now>-<stop|walk>-<day|shade|night>-<play|close> [--res WxH]    (cand needs the env; now must not have it)
Writes <scene>.lin.npy (linear). The Unity shaders are written out here as node trees from the same numbers (FALL below, the handoff's .mat table): the stud's profile, the premultiplied blend, the beam's soft edge, the pool.
Not the game: Eevee, no SSAO, no fog, no contact shade; the bloom and the grade are crossing079_sheet.py's numpy pass of lens.json bloomPlay and the play grade."""

import json
import math
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
OUT.mkdir(parents=True, exist_ok=True)
BUILD, STATE, LIGHTING, VIEW = SCENE.split("-")
CAND = BUILD == "cand"
LIGHT = json.loads((REPO / "unity/Assets/Suborned/Editor/World/light.json").read_text())
LOOK = json.loads((REPO / "unity/Assets/Suborned/Editor/World/look.json").read_text())
SIG = json.loads((REPO / "unity/Assets/Suborned/Resources/Atmosphere/signals.json").read_text())["crossing"]["states"]
PAV = 0.154
SC = None

# The look of the candidate: what the handoff's shaders and .mat values carry. One source for both.
STUD = dict(edge=0.72, core=0.35, coreWhite=0.08, corePow=6.0, glow=0.22, fall=2.4, absorb=0.12)
RIM = dict(edge=0.45, core=0.5, coreWhite=0.20, corePow=3.0)
BEAM = dict(strength=2.0, alpha=0.30, floor=0.06, fall=1.5, soft=2.2)
POOL = dict(strength=1.0, alpha=0.70, centre=0.535, reach=0.5, fall=2.0, absorb=0.45)
NOW_BEAM = dict(strength=2.0, alpha=0.28, floor=0.25)       # Materials/Glow/DK_Cross_Beam.mat as committed
NOW_HALO = dict(strength=6.0, alpha=0.40, floor=0.35, inner=0.15, radius=0.47)   # Materials/Glow/DK_Cross_Halo.mat as committed


def hexrgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) / 255 for i in (0, 2, 4))


def lin(c):
    return tuple(x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c)


def pal(key):
    return hexrgb({**LOOK["palette"], **LIGHT["palette"]}[key]["hex"])


PHASE = "night" if LIGHTING == "night" else "day"
COLOUR = lin(hexrgb(SIG[STATE][PHASE]["colour"]))
LEVEL = SIG[STATE][PHASE]["level"]
E = tuple(c * LEVEL for c in COLOUR)


# ----------------------------------------------------------------------------- a tiny node language
class G:
    def __init__(self, name):
        self.m = bpy.data.materials.new(name)
        self.m.use_nodes = True
        self.t = self.m.node_tree
        for n in list(self.t.nodes):
            self.t.nodes.remove(n)
        self.m.surface_render_method = "BLENDED"
        self.m.use_backface_culling = False
        self.m.use_transparency_overlap = True

    def node(self, kind):
        return self.t.nodes.new(kind)

    def _in(self, sock, v):
        if hasattr(v, "links") or hasattr(v, "is_linked"):
            self.t.links.new(v, sock)
        else:
            sock.default_value = v

    def math(self, op, a, b=None, clamp=False):
        n = self.node("ShaderNodeMath")
        n.operation = op
        n.use_clamp = clamp
        self._in(n.inputs[0], a)
        if b is not None:
            self._in(n.inputs[1], b)
        return n.outputs[0]

    def sat(self, a):
        return self.math("MAXIMUM", self.math("MINIMUM", a, 1.0), 0.0)

    def lerp(self, a, b, t):
        return self.math("ADD", self.math("MULTIPLY", a, self.math("SUBTRACT", 1.0, t)), self.math("MULTIPLY", b, t))

    def smooth(self, e0, e1, x):
        t = self.sat(self.math("DIVIDE", self.math("SUBTRACT", x, e0), e1 - e0))
        return self.math("MULTIPLY", self.math("MULTIPLY", t, t), self.math("SUBTRACT", 3.0, self.math("MULTIPLY", 2.0, t)))

    def uv(self):
        n = self.node("ShaderNodeUVMap") if False else self.node("ShaderNodeTexCoord")
        s = self.node("ShaderNodeSeparateXYZ")
        self.t.links.new(n.outputs["UV"], s.inputs[0])
        return s.outputs["X"], s.outputs["Y"]

    def vec(self, c):
        return tuple(c)

    def rgb(self, parts):
        """Sum of (colour tuple, scalar socket or float): a colour socket."""
        chans = []
        for ch in range(3):
            total = 0.0
            for col, k in parts:
                total = self.math("ADD", total, self.math("MULTIPLY", float(col[ch]), k))
            chans.append(total)
        n = self.node("ShaderNodeCombineColor")
        for i, c in enumerate(chans):
            self._in(n.inputs[i], c)
        return n.outputs[0]

    def out(self, colour, alpha):
        """The premultiplied blend: src + dst x (1 - alpha)."""
        em, tr, add, o = self.node("ShaderNodeEmission"), self.node("ShaderNodeBsdfTransparent"), self.node("ShaderNodeAddShader"), self.node("ShaderNodeOutputMaterial")
        self._in(em.inputs["Color"], colour)
        em.inputs["Strength"].default_value = 1.0
        one = self.math("SUBTRACT", 1.0, alpha)
        c = self.node("ShaderNodeCombineColor")
        for i in range(3):
            self._in(c.inputs[i], one)
        self.t.links.new(c.outputs[0], tr.inputs["Color"])
        self.t.links.new(em.outputs[0], add.inputs[0])
        self.t.links.new(tr.outputs[0], add.inputs[1])
        self.t.links.new(add.outputs[0], o.inputs["Surface"])
        return self.m


def hue(c):
    m = max(c)
    return tuple(x / m for x in c)


def tint_white(c, w):
    return tuple(x * (1 - w) + w for x in c)


def stud_material(colour, p, gate=None, seat=(0.055, 0.065, 0.07)):
    """SignalDots (candidate): uv.x gates; uv.y is the fall-off. Lens: 0..1, alpha 1. Spill: 1..2, additive into a tint."""
    g = G("Stud")
    g.m.use_backface_culling = True
    u, v = g.uv()
    r = g.sat(v)
    base = g.lerp(p["edge"], 1.0, g.smooth(0.55, 1.0, g.math("SUBTRACT", 1.55, r)))
    core = g.math("POWER", g.math("SUBTRACT", 1.0, r), p["corePow"])
    white = tint_white(hue(colour), p["coreWhite"])
    is_spill = g.math("GREATER_THAN", v, 1.0005)
    k = g.math("POWER", g.sat(g.math("SUBTRACT", 2.0, v)), p.get("fall", 1.0))
    keep = g.math("SUBTRACT", 1.0, is_spill)
    colour_s = g.rgb([(colour, g.math("MULTIPLY", keep, base)), (white, g.math("MULTIPLY", keep, g.math("MULTIPLY", core, p["core"]))), (colour, g.math("MULTIPLY", is_spill, g.math("MULTIPLY", k, p.get("glow", 0.0))))])
    alpha = g.math("ADD", keep, g.math("MULTIPLY", is_spill, g.math("MULTIPLY", k, p.get("absorb", 0.0))))
    if gate is None:
        return g.out(colour_s, alpha)
    lit = g.math("MULTIPLY", g.math("GREATER_THAN", u, gate[0] - 1e-4), g.math("LESS_THAN", u, gate[1] + 1e-4))
    dark = g.math("SUBTRACT", 1.0, lit)
    sep = g.node("ShaderNodeSeparateColor")
    g.t.links.new(colour_s, sep.inputs[0])
    glass = g.math("MULTIPLY", keep, g.lerp(0.6, 1.0, r))
    c = g.node("ShaderNodeCombineColor")
    for i in range(3):
        g._in(c.inputs[i], g.math("ADD", g.math("MULTIPLY", lit, sep.outputs[i]), g.math("MULTIPLY", dark, g.math("MULTIPLY", glass, seat[i]))))
    return g.out(c.outputs[0], g.math("ADD", g.math("MULTIPLY", lit, alpha), g.math("MULTIPLY", dark, keep)))


def gate_flat(colour, gate, seat=(0.13, 0.14, 0.15)):
    """SignalDots as committed: a gated dot goes to the flat seat colour."""
    g = G("FlatGate")
    g.m.use_backface_culling = True
    u, _ = g.uv()
    lit = g.math("MULTIPLY", g.math("GREATER_THAN", u, gate[0] - 1e-4), g.math("LESS_THAN", u, gate[1] + 1e-4))
    c = g.node("ShaderNodeCombineColor")
    for i in range(3):
        g._in(c.inputs[i], g.math("ADD", g.math("MULTIPLY", lit, colour[i]), g.math("MULTIPLY", g.math("SUBTRACT", 1.0, lit), seat[i])))
    return g.out(c.outputs[0], 1.0)


def flat_material(colour):
    g = G("Flat")
    g.m.use_backface_culling = True
    c = g.node("ShaderNodeCombineColor")
    for i in range(3):
        c.inputs[i].default_value = colour[i]
    return g.out(c.outputs[0], 1.0)


def beam_material(colour, cand):
    g = G("Beam")
    _, v = g.uv()
    if cand:
        k = g.lerp(BEAM["floor"], 1.0, g.math("POWER", g.sat(v), BEAM["fall"]))
        geo = g.node("ShaderNodeNewGeometry")
        dot = g.node("ShaderNodeVectorMath")
        dot.operation = "DOT_PRODUCT"
        g.t.links.new(geo.outputs["Incoming"], dot.inputs[0])
        g.t.links.new(geo.outputs["Normal"], dot.inputs[1])
        edge = g.math("POWER", g.math("ABSOLUTE", dot.outputs["Value"]), BEAM["soft"])
        k = g.math("MULTIPLY", g.math("MULTIPLY", k, edge), BEAM["strength"] * BEAM["alpha"])
    else:
        k = g.math("MULTIPLY", g.lerp(NOW_BEAM["floor"], 1.0, g.sat(v)), NOW_BEAM["strength"] * NOW_BEAM["alpha"])
    return g.out(g.rgb([(colour, k)]), 0.0)


def halo_material(colour, cand):
    g = G("Halo")
    u, v = g.uv()
    d = g.math("SQRT", g.math("ADD", g.math("MULTIPLY", u, u), g.math("MULTIPLY", v, v)))
    if cand:
        k = g.math("POWER", g.sat(g.math("SUBTRACT", 1.0, g.math("DIVIDE", g.math("ABSOLUTE", g.math("SUBTRACT", d, POOL["centre"])), POOL["reach"]))), POOL["fall"])
        return g.out(g.rgb([(colour, g.math("MULTIPLY", k, POOL["strength"] * POOL["alpha"]))]), g.math("MULTIPLY", k, POOL["absorb"]))
    k = g.lerp(1.0, NOW_HALO["floor"], g.smooth(NOW_HALO["inner"], NOW_HALO["radius"], d))
    inside = g.math("LESS_THAN", d, NOW_HALO["radius"])
    return g.out(g.rgb([(colour, g.math("MULTIPLY", inside, g.math("MULTIPLY", k, NOW_HALO["strength"] * NOW_HALO["alpha"])))]), 0.0)


def map_material(colour, image, st=None, cutoff=0.5):
    """emitmap: URP Lit, base black, the map's RGB x the state's HDR colour, alpha clip."""
    g = G("Map")
    g.m.use_backface_culling = True
    tex = g.node("ShaderNodeTexImage")
    tex.image = image
    tex.interpolation = "Linear"
    tex.extension = "CLIP"
    if st is not None:
        tc, mp = g.node("ShaderNodeTexCoord"), g.node("ShaderNodeMapping")
        mp.inputs["Location"].default_value = (st[2], st[3], 0)
        mp.inputs["Scale"].default_value = (st[0], st[1], 1)
        g.t.links.new(tc.outputs["UV"], mp.inputs["Vector"])
        g.t.links.new(mp.outputs["Vector"], tex.inputs["Vector"])
    on = g.math("GREATER_THAN", tex.outputs["Alpha"], cutoff)
    sep = g.node("ShaderNodeSeparateColor")
    g.t.links.new(tex.outputs["Color"], sep.inputs[0])
    chans = [g.math("MULTIPLY", on, g.math("MULTIPLY", sep.outputs[i], colour[i])) for i in range(3)]
    c = g.node("ShaderNodeCombineColor")
    for i in range(3):
        g.t.links.new(chans[i], c.inputs[i])
    return g.out(c.outputs[0], on)


# ----------------------------------------------------------------------------- dressing a piece as the runtime does
def swap(obj, name_prefix, material):
    for i, m in enumerate(obj.data.materials):
        if m and m.name.startswith(name_prefix):
            obj.data.materials[i] = material


def part_of(head, tag):
    for c in head.children_recursive:
        if c.name.split(".")[0].endswith("_" + tag):
            return c
    raise KeyError(tag)


def dress_head(head, ring):
    import crossing_lens
    data = crossing_lens.data()
    atlas = next(m for m in bpy.data.materials if m.name.startswith("DK_Signal_Atlas")).node_tree.nodes
    atlas = next(n for n in atlas if n.type == "TEX_IMAGE").image
    fig = data["faces"]["parts"]["Figure"]["st"][{"walk": "walk_full", "stop": "stand_full", "clearing": "walk_full"}[STATE]]
    swap(part_of(head, "Figure"), "DK_Signal_Atlas", map_material(E, atlas, st=fig))
    for tag in ("Tens", "Units"):
        bpy.data.objects.remove(part_of(head, tag))
    rim = part_of(head, "Rim")
    if CAND:
        swap(rim, "DK_Signal_RimLens", stud_material(E, RIM))
    else:
        swap(rim, "DK_Signal_Rim", flat_material(E))
    swap(part_of(head, "Beam"), "DK_Cross_Beam", beam_material(COLOUR, CAND))
    swap(part_of(head, "Halo"), "DK_Cross_Halo", halo_material(COLOUR, CAND))
    rimg = next(n for m in bpy.data.materials if m.name.startswith("DK_Signal_Ring") for n in m.node_tree.nodes if n.type == "TEX_IMAGE").image
    swap(ring, "DK_Signal_Ring", map_material(E, rimg, cutoff=0.5 / 255))
    for o in [head, *head.children_recursive, ring]:
        o.visible_shadow = o is head


def dress_row(row, stud, flat):
    swap(row, "DK_Cross_Light", stud if CAND else flat)
    row.visible_shadow = False


# ----------------------------------------------------------------------------- stage and light
def make_world():
    night = LIGHTING == "night"
    w = bpy.data.worlds.new("w")
    w.use_nodes = True
    nt = w.node_tree
    for n in list(nt.nodes):
        nt.nodes.remove(n)
    amb, out = nt.nodes.new("ShaderNodeBackground"), nt.nodes.new("ShaderNodeOutputWorld")
    a = pal("night.ambientEquator") if night else pal("light.ambientEquator")
    amb.inputs["Color"].default_value = (*lin(a), 1)
    amb.inputs["Strength"].default_value = 0.055 if night else 1.15 if LIGHTING == "shade" else 0.34   # shade: raised until the shaded road measures as Sky's frame does (the stage has no sky bounce or walls)
    nt.links.new(amb.outputs["Background"], out.inputs["Surface"])
    SC.world = w


def sun():
    night = LIGHTING == "night"
    s = bpy.data.lights.new("sun", "SUN")
    s.energy, s.color = (0.5, pal("night.moon")) if night else (2.3, pal("light.sun"))
    s.angle = math.radians(2.0)
    o = bpy.data.objects.new("sun", s)
    p, y = (math.radians(52), math.radians(40)) if night else (math.radians(LIGHT["sun"]["pitch"]), math.radians(LIGHT["sun"]["yaw"]))
    d = Vector((math.sin(y) * math.cos(p), math.cos(y) * math.cos(p), -math.sin(p)))
    o.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
    SC.collection.objects.link(o)
    return d


def blocker(d):
    """A building out of frame: its shadow falls across the crossing with the edge through the near kerb, as the Records Office does at 210."""
    from geo import Shape
    h = 40.0
    at = Vector((0, -5.5 if VIEW == "play" else -14.0, 0)) - d * (h / -d.z)   # close: the whole frame in shade
    m = mats.get("DK_Enamel_Charcoal")
    s = Shape("Blocker")
    s.face([(at.x - 200, at.y, h), (at.x + 200, at.y, h), (at.x + 200, at.y + 300, h), (at.x - 200, at.y + 300, h)], m)
    o = s.build(bevel=0.0)
    o.visible_camera = False


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


def put(name, loc=(0, 0, 0), yaw=0.0):
    o = kit.build(name)
    for k in [o, *o.children_recursive]:
        k.location = (0, 0, 0)
    o.location = Vector(loc)
    o.rotation_euler = (0, 0, math.radians(yaw))
    return o


def stage(half, depth, w=13.0):
    from geo import Shape
    s = Shape("road")
    s.face([(-half, -w / 2, 0), (half, -w / 2, 0), (half, w / 2, 0), (-half, w / 2, 0)], mats.get("DK_Ground_RoadAsphalt"))
    s.build(bevel=0.0)
    for side in (-1, 1):
        for ix in range(-half // 2, half // 2):
            for iy in range(depth // 2):
                put("Pave_Slab", (ix * 2 + 1, side * (w / 2 + iy * 2 + 1), PAV), 0)
            put(f"Kerb_Pearl_{ix % 2}", (ix * 2 + 1, side * w / 2, 0), 0 if side > 0 else 180)
    stud = stud_material(E, STUD) if CAND else None
    flat = flat_material(E)
    mods = [(-w / 2 + 1.0 + 2 * k, "LightDotRow_2") for k in range(int(w // 2))]
    rest = w - 2 * int(w // 2)
    if rest:
        mods.append((w / 2 - rest / 2, "LightDotRow_1"))
    half_lit = w / 2 * 7 / 10   # clearing: 7 s of 10 left, the dots dark from both kerbs inward (SignalHead's window)
    for sx in (-1.75, 1.75):
        for yc, nm in mods:
            L = 2.0 if nm.endswith("2") else 1.0
            if STATE == "clearing":
                s0 = yc - L / 2 + w / 2
                gate = (w / 2 - half_lit - s0, w / 2 + half_lit - s0)
                dress_row(put(nm, (sx, yc, 0), 90), stud_material(E, STUD, gate) if CAND else None, gate_flat(E, gate))
            else:
                dress_row(put(nm, (sx, yc, 0), 90), stud, flat)
    for sy, yaw, x in ((-1, 180, 2.7), (1, 0, -2.7)):
        pos = (x, sy * (w / 2 + 0.9), PAV)
        dress_head(put("SignalLens", pos, yaw), put("HaloRing", pos, yaw))


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
    print("[079] wrote", name)


def main():
    global SC
    bpy.ops.wm.read_factory_settings(use_empty=True)
    SC = bpy.context.scene
    kit.load(strict=True)
    mats.register_robot_enamels()
    SC.render.engine = "BLENDER_EEVEE"
    SC.render.resolution_x, SC.render.resolution_y = RES
    SC.view_settings.view_transform = "Raw"
    SC.eevee.taa_render_samples = 64
    make_world()
    d = sun()
    housing = mats.LIB.get("DK_Cross_Housing") and mats.get("DK_Cross_Housing")
    if housing:   # _SPECULARHIGHLIGHTS_OFF: reflections stay, the sun's highlight goes
        housing.node_tree.nodes["Principled BSDF"].inputs["Specular IOR Level"].default_value = 0.0
    if VIEW == "play":
        stage(30, 14)
        play_camera((0.0, -3.0), 30, 14, PAV)
    else:
        stage(10, 8)
        play_camera((1.2, -6.2), 30, 3.3, PAV)   # the near kerb end: the head's foot and the first studs
    if LIGHTING == "shade":
        blocker(d)
    shoot(SCENE)


main()
