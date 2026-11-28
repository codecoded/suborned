"""Selected-to-active Cycles normal/AO bake of the raised winged postal seal."""

import bpy, sys
from pathlib import Path
import numpy as np

out = Path(sys.argv[sys.argv.index("--") + 1]).resolve()
out.mkdir(exist_ok=True, parents=True)
impath = Path(sys.argv[-1]).resolve()
bpy.ops.wm.read_factory_settings(use_empty=True)
im = bpy.data.images.load(str(impath))
w, h = im.size
px = np.empty(w * h * 4, np.float32)
im.pixels.foreach_get(px)
tile = px.reshape(h, w, 4)[256:512, :256, :3]
grid = 128
amp = np.clip((tile[::2, ::2].mean(axis=2) - 0.085) * 3.1, 0, 1)
for radius in (1, 2):
    amp = (
        amp * 4
        + np.roll(amp, radius, 0)
        + np.roll(amp, -radius, 0)
        + np.roll(amp, radius, 1)
        + np.roll(amp, -radius, 1)
    ) / 8
v = [
    (x / (grid - 1) - 0.5, y / (grid - 1) - 0.5, 0.002 + float(amp[y, x]) * 0.009)
    for y in range(grid)
    for x in range(grid)
]
f = [
    (y * grid + x, y * grid + x + 1, (y + 1) * grid + x + 1, (y + 1) * grid + x)
    for y in range(grid - 1)
    for x in range(grid - 1)
]
mesh = bpy.data.meshes.new("Die struck wings")
mesh.from_pydata(v, [], f)
mesh.update()
hi = bpy.data.objects.new("Stamped bronze master", mesh)
bpy.context.collection.objects.link(hi)
for poly in mesh.polygons:
    poly.use_smooth = True
bpy.ops.mesh.primitive_plane_add(size=1)
low = bpy.context.object
low.name = "Planar backing"
mat = bpy.data.materials.new("Receiver")
mat.use_nodes = True
low.data.materials.append(mat)
node = mat.node_tree.nodes.new("ShaderNodeTexImage")
mat.node_tree.nodes.active = node
hi.select_set(True)
low.select_set(True)
bpy.context.view_layer.objects.active = low
sc = bpy.context.scene
sc.render.engine = "CYCLES"
sc.cycles.samples = 12
sc.cycles.device = "CPU"
sc.render.bake.use_selected_to_active = True
sc.render.bake.cage_extrusion = 0.025
sc.render.bake.max_ray_distance = 0.06
sc.render.bake.margin = 1
for kind, mode in [("normal", "NORMAL"), ("ao", "AO")]:
    dest = bpy.data.images.new("die-" + kind, 256, 256, alpha=False, is_data=True)
    node.image = dest
    bpy.ops.object.bake(type=mode)
    dest.file_format = "PNG"
    dest.filepath_raw = str(out / (kind + ".png"))
    dest.save()
    dest.pack()
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(out / "postal-seal-master.blend"))
