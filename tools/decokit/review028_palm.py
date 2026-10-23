"""Brief 028 palm board (Vela, 2026-10-22): the old city's palm, the testbed's palm and the game's palms, one sun, one camera.

blender -b -P tools/decokit/review028_palm.py -- <out_dir>

Pieces (all full detail as built; nothing decimated here, the L1/L2 are the kit's own exported FBX files):
  old    CoconutPalm       the old DecoKit city's boulevard palm (byte-identical FBX to the old repo's)   L0 6310, L1 1576, L2 378
  tb     palm-gameplay/near/close-v1.glb, the testbed's palm, three authored levels                     1880, 6102, 19330
  ave    WashingtoniaPalm_Street   the avenue palm the game places 803 times, no LOD                     19190
  st     CoconutPalm_Street        phoenix lawns and medians, 78 placed                                  8230, L1 2056, L2 493
Renders: play-<k>.png (play zoom: pitch 38, yaw 30, ortho, a 1.75 m figure beside), close-<k>.png (crown, perspective), lod-<k>.png (the levels side by side).
"""
import math
import sys
from pathlib import Path

import bpy
from mathutils import Euler, Vector

sys.path.insert(0, str(Path(__file__).parent))
import kit  # noqa: E402

OUT = Path(sys.argv[sys.argv.index("--") + 1]); OUT.mkdir(parents=True, exist_ok=True)
TB = Path("~/Projects/the-suborned-store/testbed-art/palm").expanduser()
FBX = Path(__file__).resolve().parents[2] / "out/decokit/Models/Plants"
bpy.ops.wm.read_factory_settings(use_empty=True)
kit.load()
sc = bpy.context.scene
sc.render.engine = "BLENDER_EEVEE"
sc.render.film_transparent = False
sc.view_settings.view_transform = "Standard"
sc.view_settings.exposure = -0.3
w = bpy.data.worlds.new("w"); w.use_nodes = True
w.node_tree.nodes["Background"].inputs[0].default_value = (.62, .78, .92, 1); sc.world = w
sun = bpy.data.lights.new("sun", "SUN"); sun.energy, sun.color = 3.2, (1, .93, .8)
so = bpy.data.objects.new("sun", sun); so.rotation_euler = Euler((math.radians(52), 0, math.radians(-40))); sc.collection.objects.link(so)
gm = bpy.data.materials.new("g"); gm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (.84, .78, .66, 1)
me = bpy.data.meshes.new("g"); me.from_pydata([(-400, -400, 0), (1200, -400, 0), (1200, 400, 0), (-400, 400, 0)], [], [(0, 1, 2, 3)])
g = bpy.data.objects.new("g", me); sc.collection.objects.link(g); me.materials.append(gm)


def link(o):
    for k in [o, *o.children_recursive]:
        if k.name not in sc.collection.objects:
            sc.collection.objects.link(k)


def tris(root):
    dg = bpy.context.evaluated_depsgraph_get(); n = 0
    for o in [root, *root.children_recursive]:
        if o.type != "MESH":
            continue
        m = bpy.data.meshes.new_from_object(o.evaluated_get(dg), depsgraph=dg)
        n += sum(len(p.vertices) - 2 for p in m.polygons); bpy.data.meshes.remove(m)
    return n


def height(root):
    z = [(o.matrix_world @ Vector(c)).z for o in [root, *root.children_recursive] if o.type == "MESH" for c in o.bound_box]
    return max(z) - min(z)


def holder(name):
    e = bpy.data.objects.new(name, None); sc.collection.objects.link(e); return e


def glb(path, name):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=str(path))
    new = [o for o in bpy.data.objects if o not in before]
    r = holder(name)
    for o in new:
        if o.parent is None:
            o.parent = r
    return r


def fbx_level(path, name, donor):
    """An exported kit LOD file, dressed with the donor piece's materials by name (the FBX carries no textures into Blender)."""
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(path))
    new = [o for o in bpy.data.objects if o not in before]
    mats = {m.name: m for o in [donor, *donor.children_recursive] if o.type == "MESH" for m in o.data.materials if m}
    r = holder(name)
    for o in new:
        if o.type == "MESH":
            for i, s in enumerate(o.material_slots):
                nm = s.material.name.split(".")[0] if s.material else ""
                if nm in mats:
                    o.material_slots[i].material = mats[nm]
        if o.parent is None:
            o.parent = r
    return r


def figure(x, y):
    """A 1.75 m stand-in person: a cylinder body and a sphere head, neutral grey."""
    m = bpy.data.materials.new("fig"); m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (.35, .37, .4, 1)
    bpy.ops.mesh.primitive_cylinder_add(radius=.22, depth=1.5, location=(x, y, .75)); b = bpy.context.object; b.data.materials.append(m)
    bpy.ops.mesh.primitive_uv_sphere_add(radius=.14, location=(x, y, 1.62)); h = bpy.context.object; h.data.materials.append(m)
    return [b, h]


pieces = {}  # key -> (label, root, tris)
O = lambda n: kit.build(n)  # noqa: E731
old = O("CoconutPalm"); link(old); pieces["old0"] = ("CoconutPalm (old city, L0)", old)
old1 = fbx_level(FBX / "DK_CoconutPalm_L1.fbx", "old1", old)
old2 = fbx_level(FBX / "DK_CoconutPalm_L2.fbx", "old2", old)
pieces["old1"] = ("CoconutPalm L1", old1); pieces["old2"] = ("CoconutPalm L2", old2)
st = O("CoconutPalm_Street"); link(st); pieces["st0"] = ("CoconutPalm_Street L0", st)
st1 = fbx_level(FBX / "DK_CoconutPalm_Street_L1.fbx", "st1", st); st2 = fbx_level(FBX / "DK_CoconutPalm_Street_L2.fbx", "st2", st)
pieces["st1"] = ("CoconutPalm_Street L1", st1); pieces["st2"] = ("CoconutPalm_Street L2", st2)
av = O("WashingtoniaPalm_Street"); link(av); pieces["ave0"] = ("WashingtoniaPalm_Street (no LOD)", av)
for k, f in (("tb2", "palm-gameplay-v1.glb"), ("tb1", "palm-near-v1.glb"), ("tb0", "palm-close-v1.glb")):
    pieces[k] = (f, glb(TB / f, k))

# one spot each, 60 m apart, so nothing overlaps in any frame
spots = {}
for i, k in enumerate(pieces):
    spots[k] = Vector((i * 60.0, 0, 0)); pieces[k][1].location = spots[k]
bpy.context.view_layer.update()
info = {k: dict(label=v[0], tris=tris(v[1]), h=round(height(v[1]), 2)) for k, v in pieces.items()}
import json
(OUT / "tris.json").write_text(json.dumps(info, indent=1))
for k, v in info.items():
    print("[palm]", k, v)

cd = bpy.data.cameras.new("c"); co = bpy.data.objects.new("c", cd); sc.collection.objects.link(co); sc.camera = co
cd.clip_end = 2000
FIG = {}


def shoot(path, target, kind, scale, pitch, yaw, res, dist=120):
    sc.render.resolution_x, sc.render.resolution_y = res
    cd.type = "ORTHO" if kind == "ortho" else "PERSP"
    if kind == "ortho":
        cd.ortho_scale = scale
    else:
        cd.lens = scale
    p, y = math.radians(pitch), math.radians(yaw)
    d = Vector((math.sin(y) * math.cos(p), -math.cos(y) * math.cos(p), math.sin(p)))
    co.location = target + d * dist
    co.rotation_euler = (target - co.location).to_track_quat("-Z", "Y").to_euler()
    sc.render.filepath = str(path); bpy.ops.render.render(write_still=True)


def fig_at(k):
    for f in FIG.pop("f", []):
        bpy.data.objects.remove(f)
    FIG["f"] = figure(spots[k].x + 3.2, spots[k].y)


# play zoom: ortho, pitch 38, yaw 30, one scale for every palm so heights compare; the avenue palm (15.6 m) sets it
for k in pieces:
    fig_at(k)
    shoot(OUT / f"play-{k}.png", spots[k] + Vector((0, 0, 7.2)), "ortho", 20.0, 38, 30, (700, 900))
    h = info[k]["h"]
    cs = {"ave": 6.0, "st": 8.5, "old": 8.0, "tb": 7.0}[k[:-1] if k[:-1] in ("ave", "st", "old") else "tb"]
    shoot(OUT / f"close-{k}.png", spots[k] + Vector((0, 0, h - cs * .33)), "ortho", cs, 14, 30, (800, 800))
print("[palm] done")
