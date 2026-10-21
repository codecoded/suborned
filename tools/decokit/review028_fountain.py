"""Brief 028 comparison renders (Vela): the atom fountain, testbed | now | proposed, same camera and lights, same Blender.

blender -b -P tools/decokit/review028_fountain.py -- <out_dir> [day|night]
  testbed   the testbed's sources read directly: basin-close.json (its own geometry) with the store's webp maps wired in as plain Principled materials, atom-close-v1.glb as imported (Polished gold / silver)
  now       AtomFountain (adopted)
  proposed  AtomFountain_Testbed (the port: kit materials, parts)
Play camera: 38 degrees, yaw 30, orthographic. Not the game's lighting: no clearcoat, no animated water."""
import json
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
MODE = A[1] if len(A) > 1 else "day"
NIGHT = MODE == "night"
STORE = Path("~/Projects/the-suborned-store/testbed-art/civic-fountain").expanduser()
OUT.mkdir(parents=True, exist_ok=True)


def link(o):
    for k in [o, *o.children_recursive]:
        if k.name not in bpy.context.scene.collection.objects:
            bpy.context.scene.collection.objects.link(k)
    return o


def setup():
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE"
    sc.render.resolution_x, sc.render.resolution_y = 1280, 960
    sc.view_settings.view_transform = "Standard"
    sc.view_settings.exposure = 0.0 if NIGHT else -0.3
    w = bpy.data.worlds.new("w")
    w.use_nodes = True
    bg = w.node_tree.nodes["Background"]
    bg.inputs[0].default_value = (.04, .07, .12, 1) if NIGHT else (.62, .78, .92, 1)
    bg.inputs[1].default_value = 0.25 if NIGHT else 1.1
    sc.world = w
    sun = bpy.data.lights.new("sun", "SUN")
    sun.energy, sun.color = (0.12, (0.65, 0.78, 1.0)) if NIGHT else (3.2, (1.0, .93, .80))
    sun.angle = math.radians(2)
    so = bpy.data.objects.new("sun", sun)
    so.rotation_euler = Euler((math.radians(52), 0, math.radians(-40)))
    sc.collection.objects.link(so)
    if NIGHT:  # the testbed's waterline uplight and two submerged sculpture uplights
        for loc, w_, col in (((0, 0, .85), 900, (.56, .91, .92)), ((-2.25, -1.25, .48), 600, (.91, .95, 1.0)), ((1.6, 2.1, .48), 400, (.91, .95, 1.0))):
            p = bpy.data.lights.new("p", "POINT")
            p.energy, p.color = w_, col
            o = bpy.data.objects.new("p", p)
            o.location = loc
            sc.collection.objects.link(o)
    me = bpy.data.meshes.new("g")
    me.from_pydata([(-14, -14, 0), (14, -14, 0), (14, 14, 0), (-14, 14, 0)], [], [(0, 1, 2, 3)])
    uv = me.uv_layers.new()
    for i, (x, y) in enumerate([(-14, -14), (14, -14), (14, 14), (-14, 14)]):
        uv.data[i].uv = (x / 4, y / 4)
    o = bpy.data.objects.new("g", me)
    sc.collection.objects.link(o)
    me.materials.append(mats.get("DK_Ground_Pavement"))


def camera(target, half, yaw=30):
    cd = bpy.data.cameras.new("c")
    co = bpy.data.objects.new("c", cd)
    bpy.context.scene.collection.objects.link(co)
    bpy.context.scene.camera = co
    cd.type, cd.ortho_scale, cd.clip_end = "ORTHO", 2 * half * 4 / 3, 400
    p, d, t = math.radians(38), 80, Vector(target)
    co.location = t + Vector((math.sin(math.radians(yaw)) * math.cos(p), -math.cos(math.radians(yaw)) * math.cos(p), math.sin(p))) * d
    co.rotation_euler = (t - co.location).to_track_quat("-Z", "Y").to_euler()


def img_mat(name, base=None, colour=(1, 1, 1), rough=0.5, metal=0.0, orm=None, alpha=1.0, emit=None, uvscale=1.0):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    if base:
        t = m.node_tree.nodes.new("ShaderNodeTexImage")
        t.image = bpy.data.images.load(str(STORE / base))
        m.node_tree.links.new(t.outputs["Color"], b.inputs["Base Color"])
    else:
        b.inputs["Base Color"].default_value = (*colour, 1)
    b.inputs["Roughness"].default_value, b.inputs["Metallic"].default_value, b.inputs["Alpha"].default_value = rough, metal, alpha
    if emit:
        b.inputs["Emission Color"].default_value, b.inputs["Emission Strength"].default_value = (*emit[0], 1), emit[1]
    return m


def lin(h):
    return tuple(((int(h[i:i + 2], 16) / 255) / 12.92 if int(h[i:i + 2], 16) / 255 <= .04045 else ((int(h[i:i + 2], 16) / 255 + .055) / 1.055) ** 2.4) for i in (1, 3, 5))


def testbed():
    d = json.loads((Path(__file__).parent / "art/fountain/basin-close.json").read_text())
    M = {"stone": img_mat("m", "marble-base.webp", rough=.2), "teal": img_mat("t", "ceramic-base.webp", rough=.13), "champagne": img_mat("g", colour=lin("#f9e2a2"), rough=.12, metal=1),
         "light": img_mat("l", colour=lin("#d3edeb"), emit=(lin("#93e7e4"), 1.4 if NIGHT else 0.0)), "jets": img_mat("j", colour=lin("#bde4e8"), rough=.06, alpha=.5),
         "foam": img_mat("f", colour=lin("#d3eddf"), rough=.35, alpha=.42), "water": img_mat("w", "pool-mosaic.webp", rough=.085)}
    for kind, k in d["kinds"].items():
        v = [(k["v"][i], -k["v"][i + 2], k["v"][i + 1]) for i in range(0, len(k["v"]), 3)]
        t = [tuple(k["t"][i:i + 3]) for i in range(0, len(k["t"]), 3)]
        me = bpy.data.meshes.new(kind)
        me.from_pydata(v, [], t)
        uvl = me.uv_layers.new()
        for p in me.polygons:
            for li, vi in zip(p.loop_indices, p.vertices):
                uvl.data[li].uv = (v[vi][0] * .5, v[vi][1] * -.5) if kind == "water" else (k["uv"][vi * 2], k["uv"][vi * 2 + 1])
        for p in me.polygons:
            p.use_smooth = True
        me.materials.append(M[kind])
        o = bpy.data.objects.new(kind, me)
        bpy.context.scene.collection.objects.link(o)
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=str(STORE / "sculpture/atom-close-v1.glb"))
    for o in set(bpy.data.objects) - before:
        if o.type == "MESH":
            for p in o.data.polygons:
                p.use_smooth = True
            if o.data.materials:
                o.data.materials[0] = M["champagne"] if "Gold" in o.name else img_mat("s", colour=lin("#f5f5f5"), rough=.065, metal=1)


def main(variant):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    setup()
    kit.load()
    if variant == "testbed":
        testbed()
    else:
        o = link(kit.build("AtomFountain" if variant == "now" else "AtomFountain_Testbed"))
    camera((0, 0, 1.6), 5.2)
    bpy.context.scene.render.filepath = str(OUT / f"fountain-{variant}-{MODE}.png")
    bpy.ops.render.render(write_still=True)


for v in ("testbed", "now", "proposed"):
    main(v)
