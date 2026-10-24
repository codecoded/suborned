"""Keep mask for a tint (brief 063, Forma): the UV area of the faces above a height, painted white, so apply_tint never recolours skin that shares a garment's hue.
  blender -b -P tools/figures/keepmask.py -- <built figure .fbx> <out.png> <zAbove metres> [size=2048] [grow=4]
The generators' UV atlases are fragmented (the driver's lips sit in five small islands), so the mask is taken from the mesh, not drawn by hand. LOD0's UVs, top-left origin."""
import bpy, sys
from PIL import Image, ImageDraw, ImageFilter
a = sys.argv[sys.argv.index('--') + 1:]; fbx, out, z0 = a[0], a[1], float(a[2]); size = int(a[3]) if len(a) > 3 else 2048; grow = int(a[4]) if len(a) > 4 else 4
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=fbx)
o = sorted([x for x in bpy.context.scene.objects if x.type == 'MESH' and x.name.endswith('LOD0')], key=lambda x: x.name)[0]
me = o.data; uv = me.uv_layers.active.data; im = Image.new('L', (size, size), 0); d = ImageDraw.Draw(im); n = 0
for p in me.polygons:
    if (o.matrix_world @ p.center).z > z0:
        d.polygon([(uv[l].uv.x * size, (1 - uv[l].uv.y) * size) for l in p.loop_indices], fill=255); n += 1
if grow: im = im.filter(ImageFilter.MaxFilter(grow * 2 + 1))
im.save(out); print('keepmask', out, n, 'faces above', z0)
