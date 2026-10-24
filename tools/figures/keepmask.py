"""Keep mask for a tint (brief 063, Forma): the UV area of the faces above a height, painted white, so apply_tint never recolours skin that shares a garment's hue.
  blender -b -P tools/figures/keepmask.py -- <built figure .fbx> <out.png> <zAbove metres> [size=2048] [grow=4]
The generators' UV atlases are fragmented (the driver's lips sit in five small islands), so the mask is taken from the mesh, not drawn by hand. LOD0's UVs, top-left origin."""
import bpy, sys
import numpy as np
a = sys.argv[sys.argv.index('--') + 1:]; fbx, out, z0 = a[0], a[1], float(a[2]); size = int(a[3]) if len(a) > 3 else 2048; grow = int(a[4]) if len(a) > 4 else 4
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=fbx)
o = sorted([x for x in bpy.context.scene.objects if x.type == 'MESH' and x.name.endswith('LOD0')], key=lambda x: x.name)[0]
me = o.data; uv = me.uv_layers.active.data; m = np.zeros((size, size), bool); n = 0   # m[row from the top, column]
def tri(m, pts):
    xs, ys = pts[:, 0], pts[:, 1]; x0, x1 = int(max(0, np.floor(xs.min()))), int(min(size - 1, np.ceil(xs.max()))); y0, y1 = int(max(0, np.floor(ys.min()))), int(min(size - 1, np.ceil(ys.max())))
    if x1 < x0 or y1 < y0: return
    gx, gy = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0, y1 + 1) + 0.5)
    (ax, ay), (bx, by), (cx, cy) = pts
    den = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy)
    if abs(den) < 1e-9: m[y0:y1 + 1, x0:x1 + 1] |= False; return
    w1 = ((by - cy) * (gx - cx) + (cx - bx) * (gy - cy)) / den; w2 = ((cy - ay) * (gx - cx) + (ax - cx) * (gy - cy)) / den; w3 = 1 - w1 - w2
    m[y0:y1 + 1, x0:x1 + 1] |= (w1 >= -0.02) & (w2 >= -0.02) & (w3 >= -0.02)
for p in me.polygons:
    if (o.matrix_world @ p.center).z > z0:
        ls = [uv[l].uv for l in p.loop_indices]; pts = np.array([(u.x * size, (1 - u.y) * size) for u in ls]); n += 1
        for i in range(1, len(pts) - 1): tri(m, np.array([pts[0], pts[i], pts[i + 1]]))
for _ in range(grow):   # dilate: the bleed around each island
    d = m.copy(); d[1:] |= m[:-1]; d[:-1] |= m[1:]; d[:, 1:] |= m[:, :-1]; d[:, :-1] |= m[:, 1:]; m = d
img = bpy.data.images.new('keep', size, size, alpha=False); px = np.zeros((size, size, 4), np.float32); px[..., 3] = 1
px[..., :3] = m[::-1, :, None]       # Blender rows run bottom to top
img.pixels.foreach_set(px.reshape(-1)); img.filepath_raw = out; img.file_format = 'PNG'; img.save()
print('keepmask', out, n, 'faces above', z0, 'covers', round(float(m.mean()), 4))
