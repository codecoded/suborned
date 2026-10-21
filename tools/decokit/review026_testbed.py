"""Brief 026 testbed comparison renders (Vela): the street and the café front, now vs proposed, day and night, same camera, same Blender.

blender -b -P tools/decokit/review026_testbed.py -- <out_dir> <what> [...]
  road-now | road-asphalt | road-enamel     a street slice (pavement, kerb, gutter, carriageway, kerb, pavement) at play zoom 30; asphalt = today's dark ground + ribbon + lozenges, enamel = the powder enamel + ribbon + lozenges + seams
  cafe-now | cafe-prop                      CafeBay_v2 + CafeInterior + CafeTerrace (+ CafeFascia and the lamps for prop)
  lamps                                     the four lamp pieces on a table / wall / ground, close
Each writes <what>-day.png and <what>-night.png. Night = cool dim sun and sky, warm point lights at the lamps; emissive strengths are the pieces' own (no bloom here: review.py adds a mild one in PIL).
Not the game's lighting; a like-for-like comparison of shapes and emissive materials."""

import math
import sys
from pathlib import Path

import bpy
from mathutils import Euler, Vector

sys.path.insert(0, str(Path(__file__).parent))
import kit  # noqa: E402
import mats  # noqa: E402

A = sys.argv[sys.argv.index("--") + 1:]
OUT = Path(A[0])
WHAT = A[1:]
OUT.mkdir(parents=True, exist_ok=True)


def link(o):
    for k in [o, *o.children_recursive]:
        if k.name not in bpy.context.scene.collection.objects:
            bpy.context.scene.collection.objects.link(k)
    return o


def place(name, loc, yaw=0.0, parent_z=0.0):
    o = link(kit.build(name))
    o.location = (loc[0], loc[1], loc[2] if len(loc) > 2 else parent_z)
    o.rotation_euler = Euler((0, 0, math.radians(yaw)))
    return o


def quad(name, x0, y0, x1, y1, z, mat, uv=None):
    me = bpy.data.meshes.new(name)
    me.from_pydata([(x0, y0, z), (x1, y0, z), (x1, y1, z), (x0, y1, z)], [], [(0, 1, 2, 3)])
    uvl = me.uv_layers.new()
    for i, (x, y) in enumerate([(x0, y0), (x1, y0), (x1, y1), (x0, y1)]):
        uvl.data[i].uv = (x, y) if uv is None else (y, abs(x - x0))
    o = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(o)
    me.materials.append(mat)
    return o


def world(night):
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE"
    sc.render.resolution_x, sc.render.resolution_y = 1920, 1080
    sc.view_settings.view_transform = "Standard"
    sc.view_settings.exposure = 0.0 if night else -0.3
    w = bpy.data.worlds.new("w")
    w.use_nodes = True
    bg = w.node_tree.nodes["Background"]
    bg.inputs[0].default_value = (.04, .07, .12, 1) if night else (.62, .78, .92, 1)
    bg.inputs[1].default_value = 0.25 if night else 1.1
    sc.world = w
    sun = bpy.data.lights.new("sun", "SUN")
    if night:
        sun.energy, sun.color = 0.12, (0.65, 0.78, 1.0)
    else:
        sun.energy, sun.color = 3.2, (1.0, .93, .80)
    sun.angle = math.radians(2)
    so = bpy.data.objects.new("sun", sun)
    so.rotation_euler = Euler((math.radians(52), 0, math.radians(-40)))
    sc.collection.objects.link(so)


def point(loc, colour, watts, radius=0.05):
    l = bpy.data.lights.new("p", "POINT")
    l.energy, l.color, l.shadow_soft_size = watts, colour, radius
    o = bpy.data.objects.new("p", l)
    o.location = loc
    bpy.context.scene.collection.objects.link(o)


def camera(target, yaw, half, kind="play"):
    for c in [o for o in bpy.data.objects if o.type == "CAMERA"]:
        bpy.data.objects.remove(c)
    cd = bpy.data.cameras.new("c")
    co = bpy.data.objects.new("c", cd)
    bpy.context.scene.collection.objects.link(co)
    bpy.context.scene.camera = co
    cd.type, cd.ortho_scale, cd.clip_end = "ORTHO", 2 * half * 16 / 9, 400
    p, d, t = math.radians(38), 80, Vector(target)
    co.location = t + Vector((math.sin(math.radians(yaw)) * math.cos(p), -math.cos(math.radians(yaw)) * math.cos(p), math.sin(p))) * d
    co.rotation_euler = (t - co.location).to_track_quat("-Z", "Y").to_euler()


def shoot(n):
    bpy.context.scene.render.filepath = str(OUT / f"{n}.png")
    bpy.ops.render.render(write_still=True)
    print("[testbed] wrote", n)


def pool_gradient(m, k=0.6):
    """The glow pool as Latch's shader draws it: alpha fades across the quad (UV v 0 at the lens to 1 at the outer edge)."""
    nt = m.node_tree
    b = nt.nodes["Principled BSDF"]
    uv, sep, inv, sq = nt.nodes.new("ShaderNodeTexCoord"), nt.nodes.new("ShaderNodeSeparateXYZ"), nt.nodes.new("ShaderNodeMath"), nt.nodes.new("ShaderNodeMath")
    inv.operation, inv.inputs[0].default_value = "SUBTRACT", 1.0
    sq.operation, sq.inputs[1].default_value = "POWER", 2.2
    nt.links.new(uv.outputs["UV"], sep.inputs["Vector"])
    nt.links.new(sep.outputs["Y"], inv.inputs[1])
    nt.links.new(inv.outputs["Value"], sq.inputs[0])
    mul = nt.nodes.new("ShaderNodeMath")
    mul.operation, mul.inputs[1].default_value = "MULTIPLY", k
    nt.links.new(sq.outputs["Value"], mul.inputs[0])
    nt.links.new(mul.outputs["Value"], b.inputs["Alpha"])


def street(variant, night):
    g = mats.get
    road = {"now": g("DK_Ground_Asphalt"), "asphalt": g("DK_Ground_Asphalt"), "enamel": g("DK_Ground_RoadEnamel")}[variant]
    W0, W1 = 0.675, 7.875                      # carriageway x range (7.2 m)
    quad("pave", -24, -40, -2.4, 40, 0.15, g("DK_Ground_Pavement"))
    quad("band", -4, -40, -2.4, 40, 0.152, g("DK_Terrazzo_Seafoam"))
    quad("pave2", -2.4, -40, 0.0, 40, 0.15, g("DK_Ground_Pavement"))
    quad("kerb", 0.0, -40, 0.3, 40, 0.152, g("DK_Ground_Kerb"))
    quad("gut", 0.3, -40, W0, 40, 0.003, g("DK_Ground_Gutter"), uv=True)
    quad("road", W0, -40, W1, 40, 0.0, road)
    quad("gut2", W1, -40, W1 + 0.375, 40, 0.003, g("DK_Ground_Gutter"), uv=True)
    quad("kerb2", W1 + 0.375, -40, W1 + 0.675, 40, 0.152, g("DK_Ground_Kerb"))
    quad("pave3", W1 + 0.675, -40, W1 + 24, 40, 0.15, g("DK_Ground_Pavement"))
    for x, x1 in ((0.3, 0.0), (W1 + 0.375, W1 + 0.375)):  # kerb faces
        me = bpy.data.meshes.new("kf")
        me.from_pydata([(x, -40, 0), (x, 40, 0), (x, 40, 0.15), (x, -40, 0.15)], [], [(0, 1, 2, 3)])
        o = bpy.data.objects.new("kf", me)
        bpy.context.scene.collection.objects.link(o)
        me.materials.append(g("DK_Ground_Kerb"))
    for yy in (-20, 0, 20):  # context: lamps and a bench on the pavement
        place("Streetlamp", (-1.0, yy + 6), 90, 0.15)
    place("Bench", (-2.0, -6), 90, 0.15)
    place("Cypress", (-3.5, 8), 0, 0.15)
    if variant != "now":
        cx = (W0 + W1) / 2
        for k in range(-7, 7):  # 6 m straights both sides; lens centre 0.1 in from the road edge
            y = k * 6 + 3
            place("RoadRibbon_Straight_6m", (W0 + 0.1, y, 0.0), -90)
            place("RoadRibbon_Straight_6m", (W1 - 0.1, y, 0.0), 90)
        for k in range(-5, 5):
            place("RoadMark_Lozenge", (cx, k * 8.5 + 2, 0.0), 90)
        if variant == "enamel":
            for k in range(-4, 5):
                place("RoadMark_Seam", (cx, k * 9, 0.0), 90)
        # brass hairline along each kerb crown (street-level brass, hairline only)
        br = g("DK_Brass_Civic")
        quad("hair1", 0.28, -40, 0.30, 40, 0.1535, br)
        quad("hair2", W1 + 0.375, -40, W1 + 0.395, 40, 0.1535, br)
        pm = bpy.data.materials.get("DK_Road_Pool")
        if pm:
            pool_gradient(pm, 0.6 if night else 0.0)
        ln = bpy.data.materials.get("DK_Road_Lens")
        if ln and not night:  # the ribbon is the lighting rig's: dim by day
            ln.node_tree.nodes["Principled BSDF"].inputs["Emission Strength"].default_value = 0.3
    if night:
        for yy in (-20, 0, 20):
            point((-1.0 - 0.3, yy + 6, 4.2), (1.0, .85, .6), 900)
    return (W0 + W1) / 2


def floor(variant, night):
    """A pavement patch at the kerb: slabs / flags, a tile / terrazzo band, kerb, tree pits with their trees, road."""
    prop = variant == "prop"
    for ix in range(5):
        for iy in range(-5, 5):
            x, y = -1 - 2 * ix, 2 * iy + 1
            name = "Pave_Slab" if prop else f"Pave_Flags_{(ix & 1) + 2 * (iy & 1)}"
            o = place(name, (x, y, 0), 0)
            if not prop:
                o.location.z = 0.0
    for iy in range(-5, 5):
        place(f"Kerb_Pearl_{iy & 1}" if prop else f"Kerb_Granite_{iy & 1}", (0, 2 * iy + 1, 0), 90)
    road = mats.get("DK_Ground_RoadEnamel" if prop else "DK_Ground_Asphalt")
    quad("road", 0.0 if prop else 0.675, -12, 14, 12, 0.0, road)
    if prop:
        for k in range(-2, 2):
            place("RoadRibbon_Straight_6m", (0.1, k * 6 + 3, 0.0), -90)
        place("PaveInlay_Band_6m", (-4.0, 0.0, 0.0), 0)
    else:
        for k in range(3):
            place("InlayBand_12_2", (-1.0 - 2 * k - 0.0, 0.0, 0.15), 0)
    for y, tree in ((-6, True), (6, True)):
        place("TreePit_Pearl" if prop else "TreePit_Flags_0", (-5, y, 0), 0)
        t = place("CoconutPalm_Street" if prop else "Cypress", (-5, y, 0.0), 0)
        t.location.z = 0.15
    place("Bench", (-1.6, 3.0), 90, 0.15)
    place("Streetlamp", (-1.0, -2.5), 90, 0.15)
    pm = bpy.data.materials.get("DK_Road_Pool")
    if pm:
        pool_gradient(pm, 0.0)
    return (-4, 0, 0.5)


def cutaway():
    """Take the ceiling off the café pieces (faces wholly above z 4.5), as the testbed's roof cutaway button does, so the room can be seen from the play camera."""
    import bmesh
    for o in [o for o in bpy.data.objects if o.type == "MESH" and o.name.startswith(("DK_CafeInterior", "DK_CafeBay"))]:
        bm = bmesh.new()
        bm.from_mesh(o.data)
        bm.transform(o.matrix_world)
        bmesh.ops.delete(bm, geom=[f for f in bm.faces if all(v.co.z > 4.5 for v in f.verts)], context="FACES")
        bm.transform(o.matrix_world.inverted())
        bm.to_mesh(o.data)
        bm.free()


def cafe(variant, night, cut=False):
    ground = bpy.data.materials.new("g")
    b = ground.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (.60, .52, .40, 1)
    quad("g", -60, -60, 60, 60, 0.0, ground)
    interior = "CafeInterior_Testbed" if variant == "tb" else "CafeInterior"
    for n in ("CafeBay_v2", interior, "CafeTerrace"):
        place(n, (0, 0, 0))
    spots = ((-8, -3.2), (-3, -3.8), (3, -3.8), (8, -3.2))
    cols = [(1, .75, .4), (.4, .84, .81), (1, .82, .6), (1, .66, .57), (.72, .61, .89)]
    for i, (x, y) in enumerate(spots):
        if variant == "tb":
            o = place("CafeSet_Testbed", (x, y, 0), 0)
            place("Parasol_Testbed", (x, y, 0), 8.6 * i)
            if night:
                point((x + 0.2, y - 0.2, 1.0), cols[i % 5], 40)
        else:
            place("CafeSet_Tulip", (x, y, 0))
            place("Parasol_Stripe", (x, y, 0))
    if variant in ("prop", "tb"):
        place("CafeFascia", (0, 0, 0))
    if variant == "tb" and night:
        for x, y in ((-7.0, 3.6), (-7, 6.0), (-7, 8.3), (-3.3, 9.5), (0.8, 7.9), (3.5, 7.9), (6.1, 7.9), (8.1, 8.8)):
            point((x, y, 2.3), (1, .82, .55), 120)
        point((3.5, 8.3, 1.4), (1, .62, .3), 80)
    if cut:
        cutaway()
    return (0, 5, 0.5)


def lamps(night):
    ground = bpy.data.materials.new("g")
    ground.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (.5, .45, .38, 1)
    quad("g", -4, -4, 4, 4, 0.0, ground)
    quad("tbl", -0.5, -0.5, 0.6, 0.5, 0.74, mats.get("DK_Cafe_Walnut"))
    for i, h in enumerate(("Amber", "Teal", "Peach", "Coral", "Lilac")):
        place(f"TableLamp_{h}", (-0.4 + i * 0.22, 0, 0.74), 0)
    quad("wall", -3, 1.2, 0.0, 1.25, 0.0, mats.get("DK_Limestone"))
    for name, loc in (("AwningLamp", (-1.2, 1.2, 1.4)), ("PalmUplight", (1.0, 0, 0)), ("StreetLampGlobe", (2.0, 0, 0))):
        place(name, loc, 0)
    return (0.4, 0.3, 0.4)


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    kit.load()
    mats.register_robot_enamels()
    for what in WHAT:
        for night in (False, True):
            bpy.ops.wm.read_factory_settings(use_empty=True)
            for m in list(bpy.data.materials):
                bpy.data.materials.remove(m)
            world(night)
            if what.startswith("road"):
                cx = street(what.split("-")[1], night)
                camera((cx, 0, 0), 30, 9.0)
            elif what.startswith("floor"):
                camera(floor(what.split("-")[1], night), 30, 9.0)
            elif what.startswith(("cafe", "cut")):
                camera(cafe(what.split("-")[1], night, what.startswith("cut")), 30, 7.5)
            else:
                t = lamps(night)
                camera(t, 30, 1.4)
            shoot(f"{what}-{'night' if night else 'day'}")


main()
