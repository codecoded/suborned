"""Brief 026 café review renders (Blender EEVEE): the testbed's café glb vs our café pieces under the same sun, at the play pitch and street level, plus a stats report.

blender -b -P tools/decokit/review026_cafe.py -- <out_dir> testbed <glb> [--rot DEG]     (the testbed's model, as exported)
blender -b -P tools/decokit/review026_cafe.py -- <out_dir> ours                          (CafeBay_v2 + CafeInterior + CafeTerrace + sets, our kit pieces from tools/decokit)
Same warm sun and pale sky as review026.py. Stats: objects, triangles, materials, image sizes -> <out_dir>/<mode>-stats.json."""

import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Euler, Vector

sys.path.insert(0, str(Path(__file__).parent))
A = sys.argv[sys.argv.index("--") + 1:]
OUT, MODE = Path(A[0]), A[1]
OUT.mkdir(parents=True, exist_ok=True)
HALF = float(A[A.index("--half") + 1]) if "--half" in A else 7.0


def tris(objs):
    t = 0
    dg = bpy.context.evaluated_depsgraph_get()
    for o in objs:
        if o.type == "MESH":
            t += sum(len(p.vertices) - 2 for p in o.data.polygons)
    return t


def stats(objs, label):
    imgs = {}
    mats = set()
    for o in objs:
        if o.type != "MESH":
            continue
        for s in o.material_slots:
            if s.material:
                mats.add(s.material.name)
                if s.material.node_tree:
                    for n in s.material.node_tree.nodes:
                        if n.type == "TEX_IMAGE" and n.image:
                            imgs[n.image.name] = list(n.image.size)
    r = dict(objects=len([o for o in objs if o.type == "MESH"]), triangles=tris(objs), materials=len(mats), images=imgs,
             texelsM=round(sum(w * h for w, h in imgs.values()) / 1e6, 1), bc7MiBWithMips=round(sum(w * h for w, h in imgs.values()) * 4 / 3 / 2 ** 20, 1))
    (OUT / f"{label}-stats.json").write_text(json.dumps(r, indent=1))
    print("[cafe]", label, {k: v for k, v in r.items() if k != "images"})
    return r


def world(sc):
    sc.render.engine = "BLENDER_EEVEE"
    sc.render.resolution_x, sc.render.resolution_y = 1920, 1080
    sc.view_settings.view_transform = "Standard"
    sc.view_settings.exposure = -0.3
    w = bpy.data.worlds.new("w")
    w.use_nodes = True
    bg = w.node_tree.nodes["Background"]
    bg.inputs[0].default_value = (.62, .78, .92, 1)
    bg.inputs[1].default_value = 1.1
    sc.world = w
    sun = bpy.data.lights.new("sun", "SUN")
    sun.energy, sun.angle, sun.color = 3.2, math.radians(2), (1.0, .93, .80)
    so = bpy.data.objects.new("sun", sun)
    so.rotation_euler = Euler((math.radians(52), 0, math.radians(-40)))
    sc.collection.objects.link(so)


def ground(cx, cy, sz=60):
    me = bpy.data.meshes.new("g")
    me.from_pydata([(cx - sz, cy - sz, 0), (cx + sz, cy - sz, 0), (cx + sz, cy + sz, 0), (cx - sz, cy + sz, 0)], [], [(0, 1, 2, 3)])
    o = bpy.data.objects.new("g", me)
    bpy.context.scene.collection.objects.link(o)
    m = bpy.data.materials.new("g")
    m.diffuse_color = (.80, .74, .62, 1)
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (.60, .52, .40, 1)
    b.inputs["Roughness"].default_value = .8
    me.materials.append(m)


def cam(kind, target, yaw):
    for c in [o for o in bpy.data.objects if o.type == "CAMERA"]:
        bpy.data.objects.remove(c)
    cd = bpy.data.cameras.new("c")
    co = bpy.data.objects.new("c", cd)
    bpy.context.scene.collection.objects.link(co)
    bpy.context.scene.camera = co
    t = Vector(target)
    if kind == "play":
        cd.type, cd.ortho_scale = "ORTHO", 28 * 16 / 9
        p, d = math.radians(38), 80
    elif kind == "zoom":
        cd.type, cd.ortho_scale = "ORTHO", 2 * HALF * 16 / 9
        p, d = math.radians(38), 80
    if kind in ("play", "zoom"):
        off = Vector((math.sin(math.radians(yaw)) * math.cos(p), -math.cos(math.radians(yaw)) * math.cos(p), math.sin(p))) * d
        co.location = t + off
        cd.clip_end = 400
    else:  # street: eye 1.6 m, 14 m out from the front (-Y), looking at the glazing
        cd.type, cd.lens = "PERSP", 26
        co.location = t + Vector((-3, -14, 1.6))
        cd.clip_end = 300
        t = t + Vector((0, 0, 1.6))
    co.rotation_euler = (t - co.location).to_track_quat("-Z", "Y").to_euler()


def shoot(n):
    bpy.context.scene.render.filepath = str(OUT / f"{n}.png")
    bpy.ops.render.render(write_still=True)
    print("[cafe] wrote", n)


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    world(sc)
    if MODE == "testbed":
        bpy.ops.import_scene.gltf(filepath=str(Path(A[2]).expanduser()))
        objs = [o for o in bpy.context.scene.objects if o.type in ("MESH", "EMPTY")]
        rot = float(A[A.index("--rot") + 1]) if "--rot" in A else 0.0
        meshes = [o for o in objs if o.type == "MESH"]
        mn = Vector((1e9,) * 3)
        mx = Vector((-1e9,) * 3)
        for o in meshes:
            for c in o.bound_box:
                w = o.matrix_world @ Vector(c)
                mn = Vector(map(min, mn, w))
                mx = Vector(map(max, mx, w))
        print("[cafe] bounds", tuple(round(v, 2) for v in mn), tuple(round(v, 2) for v in mx))
        stats(meshes, "testbed")
        ctr = (mn + mx) / 2
        ground(ctr.x, ctr.y)
        for lab, kind, yaw in (("play-30", "play", 30), ("play-210", "play", 210), ("zoom-30", "zoom", 30), ("street", "street", 0)):
            cam(kind, (ctr.x, ctr.y, 0.5), yaw + rot)
            shoot(f"testbed-{lab}")
    elif MODE == "oursfurn":
        import kit
        kit.load()
        objs = []
        for n in ("CafeSet_Tulip", "Parasol_Stripe"):
            o = kit.build(n)
            for k in [o, *o.children_recursive]:
                if k.name not in sc.collection.objects:
                    sc.collection.objects.link(k)
            objs += [o, *o.children_recursive]
        stats([o for o in objs if o.type == "MESH"], "oursfurn")
        ground(0, 0)
        for lab, kind, yaw in (("zoom-30", "zoom", 30), ("street", "street", 0)):
            cam(kind, (0, 0, 1.0), yaw)
            shoot(f"oursfurn-{lab}")
    else:
        import kit
        kit.load()
        names = ["CafeBay_v2", "CafeInterior", "CafeTerrace"]
        objs = []
        for n in names:
            o = kit.build(n)
            for k in [o, *o.children_recursive]:
                if k.name not in sc.collection.objects:
                    sc.collection.objects.link(k)
            objs += [o, *o.children_recursive]
        # sets on the terrace: the kit's own sets, as the world places them
        for i, (x, y) in enumerate(((-8, -3.2), (-3, -3.8), (3, -3.8), (8, -3.2))):
            for n, dx in (("CafeSet_Tulip", 0), ("Parasol_Stripe", 0)):
                o = kit.build(n)
                o.location = (x + dx, y, 0)
                for k in [o, *o.children_recursive]:
                    if k.name not in sc.collection.objects:
                        sc.collection.objects.link(k)
                objs += [o, *o.children_recursive]
        stats([o for o in objs if o.type == "MESH"], "ours")
        ground(0, 0)
        for lab, kind, yaw in (("play-30", "play", 30), ("play-210", "play", 210), ("zoom-30", "zoom", 30), ("street", "street", 0)):
            cam(kind, (0, 5, 0.5), yaw)
            shoot(f"ours-{lab}")


main()
