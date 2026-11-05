"""Brief 096 review renders (Vela): the hovering street camera against cream stone, as built by hover_camera.py (kit.build, the same geometry the export writes).

blender -b -P tools/decokit/hover_camera_review.py -- <out_dir>
Scene: a cream limestone wall (its face on y = 0, facing -Y, the street) with a seafoam band, a return wall at x = 3.4, pale paving; a live head hovering 1.0 m off the wall
at the pole lens's 4.78 m (HostViews.Attention LensUp), turned 35 degrees and tilted 16.7 degrees nose-down (LensDown); a dead head docked in the cradle (tally and ring off);
a 1.8 m figure stand-in. Shots: threeq (perspective close-up), side (the live head in profile, the cradle from its side), play (orthographic size 10 at 38 degrees, yaw 30 and 210,
1920 x 1080: the play camera; figures about 80 px) and play-crop (the play frame cropped round the cameras at 1:1 and x4).
Not the game's light: Blender EEVEE, no URP grade. Writes <out_dir>/*.png and review.json (triangles, the cameras' pixel positions in the play shots, hover_camera.DATA)."""
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Euler, Vector

sys.path.insert(0, str(Path(__file__).parent))
import kit  # noqa: E402
import mats  # noqa: E402

OUT = Path(sys.argv[sys.argv.index("--") + 1])
OUT.mkdir(parents=True, exist_ok=True)
LENS_UP, LENS_DOWN, OFF_WALL = 4.78, 16.7, 1.0
CRADLE_X = 1.9


def flat(name, hexstr, rough=0.7):
    m = bpy.data.materials.new(name)
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*mats.linear(mats.srgb(hexstr)), 1.0)
    b.inputs["Roughness"].default_value = rough
    return m


def quad(name, pts, mat, uvs=None):
    me = bpy.data.meshes.new(name)
    me.from_pydata(pts, [], [(0, 1, 2, 3)])
    uv = me.uv_layers.new()
    for i, c in enumerate(uvs or [(p[0], p[2]) for p in pts]):
        uv.data[i].uv = c
    o = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(o)
    me.materials.append(mat)
    return o


def setup():
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE"
    sc.view_settings.view_transform = "Standard"
    sc.view_settings.exposure = -0.35
    w = bpy.data.worlds.new("w")
    w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (.62, .78, .92, 1)
    w.node_tree.nodes["Background"].inputs[1].default_value = 1.0
    sc.world = w
    sun = bpy.data.lights.new("sun", "SUN")
    sun.energy, sun.color, sun.angle = 3.2, (1.0, .93, .80), math.radians(2)
    so = bpy.data.objects.new("sun", sun)
    so.rotation_euler = Euler((math.radians(50), 0, math.radians(-35)))
    sc.collection.objects.link(so)
    stone = flat("Stone", "#F3E7CF", 0.8)       # cream limestone (the civic block's stone, style role limestone #f8eed9, a touch shaded)
    joint = flat("Joint", "#E2D3B6", 0.85)
    band = flat("Band", "#72B5A7", 0.5)         # the civic block's seafoam band
    pave = flat("Pave", "#F6ECDA", 0.75)        # pale paving (style role pavement #f9efdc)
    quad("ground", [(-12, -14, 0), (12, -14, 0), (12, 3, 0), (-12, 3, 0)], pave)
    quad("wall", [(-12, 0, 0), (3.4, 0, 0), (3.4, 0, 9), (-12, 0, 9)], stone)
    quad("return", [(3.4, 0, 0), (3.4, -6, 0), (3.4, -6, 9), (3.4, 0, 9)], stone)
    for z0, z1, m in ((0.9, 1.15, band), (3.55, 3.6, joint), (6.2, 6.25, joint)):   # a seafoam band at the dado, two stone courses
        quad("b", [(-12, -0.004, z0), (3.4, -0.004, z0), (3.4, -0.004, z1), (-12, -0.004, z1)], m)
    for x in (-6.0, -3.0, 0.0):   # vertical joints of the stone panels
        quad("j", [(x, -0.003, 0), (x + 0.04, -0.003, 0), (x + 0.04, -0.003, 9), (x, -0.003, 9)], joint)
    fig = flat("Figure", "#3E6F8A", 0.6)       # a 1.8 m stand-in figure under the camera, for scale
    bpy.ops.mesh.primitive_cylinder_add(radius=0.22, depth=1.8, location=(-1.6, -2.2, 0.9), vertices=16)
    bpy.context.active_object.data.materials.append(fig)


def place(name, loc, yaw=0.0, pitch=0.0):
    o = kit.build(name)
    o.location = loc
    o.rotation_euler = (math.radians(pitch), 0, math.radians(yaw))   # pitch about X tilts the -Y forward toward -Z (nose down), then yaw about Z
    return o


def dead(head):
    """Lamp and ring off (emission 0, the dimmed base colours the report gives the host): the dead / docked state."""
    for c in head.children:
        for tail, off in (("_Tally", DEAD["tally"]), ("_Ring", DEAD["ring"])):
            if c.name.split(".")[0].endswith(tail):   # the second build of a piece is named .001
                m = c.data.materials[0].copy()
                b = m.node_tree.nodes["Principled BSDF"]
                b.inputs["Emission Strength"].default_value = 0.0
                b.inputs["Base Color"].default_value = (*mats.linear(mats.srgb(off)), 1.0)
                c.data.materials[0] = m


def tris(o):
    dg = bpy.context.evaluated_depsgraph_get()
    out = {}
    for x in [o, *o.children_recursive]:
        me = x.evaluated_get(dg).to_mesh()
        out[x.name] = sum(len(p.vertices) - 2 for p in me.polygons)
    return out


def cam(name, loc=None, target=None, ortho=None, lens=50, res=(1280, 960), pitch=38, yaw=30, dist=60):
    sc = bpy.context.scene
    cd = bpy.data.cameras.new(name)
    co = bpy.data.objects.new(name, cd)
    sc.collection.objects.link(co)
    sc.camera = co
    sc.render.resolution_x, sc.render.resolution_y = res
    t = Vector(target)
    if ortho:
        cd.type, cd.ortho_scale = "ORTHO", ortho
        p = math.radians(pitch)
        loc = t + Vector((math.sin(math.radians(yaw)) * math.cos(p), -math.cos(math.radians(yaw)) * math.cos(p), math.sin(p))) * dist
    else:
        cd.lens = lens
    cd.clip_end = 400
    co.location = loc
    co.rotation_euler = (t - Vector(loc)).to_track_quat("-Z", "Y").to_euler()
    return co


def pixel(co, o):
    """Where o's origin lands in the render, in pixels from the top left."""
    from bpy_extras.object_utils import world_to_camera_view
    sc = bpy.context.scene
    v = world_to_camera_view(sc, co, o.matrix_world.translation)
    return [round(v.x * sc.render.resolution_x, 1), round((1 - v.y) * sc.render.resolution_y, 1)]


def shot(name):
    bpy.context.scene.render.filepath = str(OUT / f"{name}.png")
    bpy.ops.render.render(write_still=True)
    print(f"[096] wrote {OUT / name}.png")


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    kit.load(strict=True)
    import hover_camera as hc
    global DEAD
    DEAD = hc.DATA["head"]["dead"]
    setup()
    live = place("HoverCamera", (-1.6, -OFF_WALL, LENS_UP), yaw=-35, pitch=LENS_DOWN)
    cradle = place("HoverCamera_Cradle", (CRADLE_X, 0.0, LENS_UP - hc.DOCK[2]))
    docked = place("HoverCamera", (CRADLE_X + hc.DOCK[0], hc.DOCK[1], LENS_UP))
    dead(docked)
    counts = {"HoverCamera": tris(live), "HoverCamera_Cradle": tris(cradle)}
    px = {}
    fig_top = bpy.data.objects.new("figureTop", None)
    fig_top.location = (-1.6, -2.2, 1.8)
    bpy.context.scene.collection.objects.link(fig_top)
    print("[096] tris", json.dumps(counts))
    head = Vector((-1.6, -OFF_WALL, LENS_UP))
    cam("threeq", loc=head + Vector((0.95, -2.5, 0.75)), target=head + Vector((-0.05, 0, -0.08)), lens=50)
    shot("threeq")
    cam("threeq-cradle", loc=Vector((CRADLE_X - 1.5, -2.6, LENS_UP + 0.5)), target=Vector((CRADLE_X, -0.6, LENS_UP - 0.15)), lens=50)
    shot("threeq-cradle")
    live.rotation_euler = (math.radians(LENS_DOWN), 0, math.radians(-90))   # the side: the live head in profile against the wall
    cam("side", target=head, ortho=2.0, pitch=0.0, yaw=0.0, dist=8)
    shot("side")
    for o in [live, *live.children_recursive]:
        o.hide_render = True   # it would stand in front of the cradle from -X
    cam("side-cradle", target=Vector((CRADLE_X, -0.55, LENS_UP - 0.05)), ortho=1.9, pitch=0.0, yaw=-90, dist=8)   # from -X, against the return wall
    shot("side-cradle")
    live.rotation_euler = (math.radians(LENS_DOWN), 0, math.radians(-35))
    for o in [live, *live.children_recursive]:
        o.hide_render = False
    bpy.data.objects["return"].hide_render = True   # it would hide the cradle from the play camera
    mid = Vector((0.0, -1.5, 1.8))
    for yaw in (30, 210):   # the opening yaw, and the Records Office front's (the wall then faces away: the cameras seen from behind the wall would be hidden, so 210 is mirrored: the scene turns)
        if yaw == 210:
            for o in bpy.context.scene.objects:   # turn the whole set 180 degrees so the wall still faces the play camera, as the Records Office front does at yaw 210
                if o.parent is None and o.type != "CAMERA" and o.name != "sun":
                    o.matrix_world = Euler((0, 0, math.pi)).to_matrix().to_4x4() @ o.matrix_world
            mid = Vector((0.0, 1.5, 1.8))
        co = cam(f"play-{yaw}", target=mid, ortho=20.0 * 16 / 9, res=(1920, 1080), pitch=38, yaw=yaw, dist=80)
        shot(f"play-{yaw}")
        bpy.context.view_layer.update()
        px[f"play-{yaw}"] = {k: pixel(co, o) for k, o in (("live", live), ("docked", docked), ("figureTop", fig_top))}
    (OUT / "review.json").write_text(json.dumps(dict(counts=counts, totals={k: sum(v.values()) for k, v in counts.items()}, pixels=px, data=hc.DATA), indent=1) + "\n")


main()
