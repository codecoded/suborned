"""UV masks by place on the body, for the recolours (brief 067, Forma): the texels of a figure whose surface satisfies a test on its position, white on black.
  blender -b -P tools/figures/zmask.py -- <built figure .fbx> <out.png> <zLow m> <zHigh m>|<expression> [size=2048] [bleed=14]
The test is a band of heights (z in metres, feet at 0) or an expression over the surface point (x across, y front to back with the front at -y, z up; numpy as np), for
example "(z>=1.52)&~((y<-0.03)&(z<1.66))" for the hair. Like keepmask.py (the driver's head), but cut per texel: the height of every texel is interpolated across its triangle, so a cut at 0.82 m is one clean horizontal
line round the body, never a zigzag of whole faces (the first version of this took faces by their centre, and the tunic's hem came out torn). The heights are then bled outward
into the atlas's gutters (`bleed` px, the island's own edge value), so the padding round an island joins the side of the cut its island is on. The generators' UV atlases are
fragmented, which is why a recolour that must stop at the waist is taken from the mesh, never drawn on the map. LOD0's UVs, top-left origin; the LODs after it keep the same UVs
(decimation delimits on UV), so one mask serves every LOD of the figure."""
import bpy, sys
import numpy as np

a = sys.argv[sys.argv.index('--') + 1:]
fbx, out = a[0], a[1]
try: expr = f'(z>={float(a[2])})&(z<{float(a[3])})'; rest = a[4:]
except ValueError: expr = a[2]; rest = a[3:]
size = int(rest[0]) if len(rest) > 0 else 2048; bleed = int(rest[1]) if len(rest) > 1 else 14
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=fbx)
o = sorted([x for x in bpy.context.scene.objects if x.type == 'MESH' and x.name.endswith('LOD0')], key=lambda x: x.name)[0]
me = o.data; me.calc_loop_triangles(); uv = me.uv_layers.active.data
vp = np.array([(o.matrix_world @ v.co)[:] for v in me.vertices], dtype=np.float64)
pmap = np.full((3, size, size), np.nan)   # pmap[axis][row from the top, column]; the interpolated position of the surface under each texel


def tri(pts, vs):
    xs, ys = pts[:, 0], pts[:, 1]
    x0, x1 = int(max(0, np.floor(xs.min()))), int(min(size - 1, np.ceil(xs.max()))); y0, y1 = int(max(0, np.floor(ys.min()))), int(min(size - 1, np.ceil(ys.max())))
    if x1 < x0 or y1 < y0: return
    gx, gy = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0, y1 + 1) + 0.5)
    (ax, ay), (bx, by), (cx, cy) = pts
    den = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy)
    if abs(den) < 1e-9: return
    w1 = ((by - cy) * (gx - cx) + (cx - bx) * (gy - cy)) / den; w2 = ((cy - ay) * (gx - cx) + (ax - cx) * (gy - cy)) / den; w3 = 1 - w1 - w2
    inside = (w1 >= -0.02) & (w2 >= -0.02) & (w3 >= -0.02)
    for ax_ in range(3):
        z = w1 * vs[0][ax_] + w2 * vs[1][ax_] + w3 * vs[2][ax_]
        sub = pmap[ax_, y0:y1 + 1, x0:x1 + 1]; sub[inside] = z[inside]


for t in me.loop_triangles:
    pts = np.array([(uv[l].uv.x * size, (1 - uv[l].uv.y) * size) for l in t.loops]); tri(pts, [vp[v] for v in t.vertices])
covered = float((~np.isnan(pmap[2])).mean())
for _ in range(bleed):   # the gutters take the position of the island edge beside them
    nz = np.isnan(pmap[2]); p = np.pad(pmap, ((0, 0), (1, 1), (1, 1)), constant_values=np.nan)
    nb = np.stack([p[:, :-2, 1:-1], p[:, 2:, 1:-1], p[:, 1:-1, :-2], p[:, 1:-1, 2:]]); has = ~np.isnan(nb)
    s = np.where(has, nb, 0.0).sum(0); n = has.sum(0)
    fill = nz & (n[2] > 0)
    for ax_ in range(3): pmap[ax_][fill] = s[ax_][fill] / n[ax_][fill]
x, y, z = pmap[0], pmap[1], pmap[2]
with np.errstate(invalid='ignore'): m = np.asarray(eval(expr, {'np': np, 'x': x, 'y': y, 'z': z}), dtype=bool) & ~np.isnan(z)
img = bpy.data.images.new('zmask', size, size, alpha=False); px = np.zeros((size, size, 4), np.float32); px[..., 3] = 1
px[..., :3] = m[::-1, :, None]       # Blender rows run bottom to top
img.pixels.foreach_set(px.reshape(-1)); img.filepath_raw = out; img.file_format = 'PNG'; img.save()
print('zmask', out, expr, 'uv coverage', round(covered, 3), 'mask covers', round(float(m.mean()), 4))
