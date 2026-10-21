"""Brief 026 street-tree comparison (Vela): the testbed's street palm (palm-gameplay-v1.glb), the kit's CoconutPalm, FanPalm_Play, and today's street trees Cypress and PottedTopiary_Play, one row, one sun, play zoom 30.
blender -b -P tools/decokit/review026_palms.py -- <out_dir>"""
import math
import sys
from pathlib import Path

import bpy
from mathutils import Euler, Vector

sys.path.insert(0, str(Path(__file__).parent))
import kit  # noqa: E402

OUT = Path(sys.argv[sys.argv.index("--") + 1])
GLB = Path("~/Projects/the-suborned-store/testbed-art/palm/palm-gameplay-v1.glb").expanduser()
bpy.ops.wm.read_factory_settings(use_empty=True)
kit.load()
sc = bpy.context.scene
sc.render.engine = "BLENDER_EEVEE"
sc.render.resolution_x, sc.render.resolution_y = 1920, 760
sc.view_settings.view_transform = "Standard"
sc.view_settings.exposure = -0.3
w = bpy.data.worlds.new("w"); w.use_nodes = True
w.node_tree.nodes["Background"].inputs[0].default_value = (.62, .78, .92, 1); sc.world = w
sun = bpy.data.lights.new("sun", "SUN"); sun.energy, sun.color = 3.2, (1, .93, .8)
so = bpy.data.objects.new("sun", sun); so.rotation_euler = Euler((math.radians(52), 0, math.radians(-40))); sc.collection.objects.link(so)
gm = bpy.data.materials.new("g"); gm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (.66, .6, .5, 1)
me = bpy.data.meshes.new("g"); me.from_pydata([(-20, -8, 0), (20, -8, 0), (20, 8, 0), (-20, 8, 0)], [], [(0, 1, 2, 3)])
g = bpy.data.objects.new("g", me); sc.collection.objects.link(g); me.materials.append(gm)
X = -9.0
for name in ("Cypress", "PottedTopiary_Play", "FanPalm_Play", "CoconutPalm", "CoconutPalm_Street"):
    o = kit.build(name)
    for k in [o, *o.children_recursive]:
        sc.collection.objects.link(k) if k.name not in sc.collection.objects else None
    o.location = (X, 0, 0); X += 5.0
    print("[palms]", name, [round(d, 2) for d in o.dimensions])
before = set(bpy.data.objects)
bpy.ops.import_scene.gltf(filepath=str(GLB))
new = [o for o in bpy.data.objects if o not in before]
root = Empty = bpy.data.objects.new("r", None); sc.collection.objects.link(root)
for o in new:
    if o.parent is None:
        o.parent = root
root.location = (X + 1.5, 0, 0); root.rotation_euler = (0, 0, 0)
cd = bpy.data.cameras.new("c"); co = bpy.data.objects.new("c", cd); sc.collection.objects.link(co); sc.camera = co
cd.type, cd.ortho_scale, cd.clip_end = "ORTHO", 24 * 16 / 9 * 0.95, 400
t = Vector((X / 2 - 1.0, 0, 5.0)); p = math.radians(20); yaw = 0
co.location = t + Vector((0, -math.cos(p), math.sin(p))) * 80
co.rotation_euler = (t - co.location).to_track_quat("-Z", "Y").to_euler()
sc.render.filepath = str(OUT / "palms.png"); bpy.ops.render.render(write_still=True)
