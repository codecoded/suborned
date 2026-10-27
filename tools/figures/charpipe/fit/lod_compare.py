"""Brief 069: close-ups of the face and a hand at LOD0, three ways, same light and camera: the generated mesh as delivered; the old reduction (the fit
test's: a plain decimate to the same budget with the normals cleared, which looked faceted); the new LOD0 (lib.reduce_lod: face and hands protected,
the full mesh's normals carried over). All in the figure's rest (the source pose).
  FIT_SET=e2e blender.sh -P tools/figures/charpipe/fit/lod_compare.py -- <id> <out dir>"""
import bpy, os, sys, json, math, importlib.util
import numpy as np
HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE); import look
L = importlib.util.module_from_spec(s := importlib.util.spec_from_file_location('fitlib', os.path.join(HERE, 'lib.py'))); s.loader.exec_module(L)
from mathutils import Vector

A = sys.argv[sys.argv.index('--') + 1:]; FID, OUT = A[0], L.ensure(A[1])
row = {r['id']: r for r in L.D['inputs']}[FID]; fit = json.load(open(os.path.join(L.SCR, 'fit', f'{FID}.fit.json'))); p = fit['params']
bpy.ops.wm.read_factory_settings(use_empty=True); sc = bpy.context.scene
with bpy.data.libraries.load(L.work(f'{FID}_H.blend'), link=False) as (src, dst): dst.objects = list(src.objects)
fig = [o for o in dst.objects if o is not None]
for o in fig:
    sc.collection.objects.link(o)
    if o.type == 'MESH' and (o.get('fit_lod', 0) > 0 or o.get('fit_role') in ('donor', 'prop')): o.hide_render = True
arm = next(o for o in fig if o.type == 'ARMATURE'); lod0 = next(o for o in fig if o.type == 'MESH' and o.get('fit_role') == 'body' and o.get('fit_lod', 0) == 0)
budget = len(L.tris_of(lod0))
def raw_copy(name):
    o = L.import_glb(row['src']); L.normalise(o); V = (np.array([list(v.co) for v in o.data.vertices]) - np.array([p['tx'], p['ty'], p['tz']])) / p['s']
    L.set_co(o, V); L.flat_material(o, L.base_color_image(o)); o.name = name; return o
raw = raw_copy('raw')
old = raw_copy('old'); L.clean_normals(old); L.decimate_to(old, budget)
H = float(np.array([list(v.co) for v in raw.data.vertices])[:, 2].max())
look.studio(sc, 700, 700); cam = look.camera(sc); cam.data.sensor_fit = 'VERTICAL'
Mw = arm.matrix_world; hd = Mw @ arm.data.bones['Head'].head_local; hand = Mw @ arm.data.bones['LeftHand'].head_local
views = {'face': (Vector((hd.x, hd.y, hd.z + 0.08)), Vector((-0.12, -0.5, 0.02)), 18), 'hand': (Vector((hand.x + 0.05, hand.y, hand.z - 0.06)), Vector((0.05, -0.45, 0.05)), 22)}
for who, ob in (('raw', raw), ('old', old), ('new', lod0)):
    for o in (raw, old, lod0): o.hide_render = o is not ob
    for vn, (t, off, fov) in views.items():
        cam.data.angle_y = math.radians(fov); look.look_at(cam, t + off, t)
        sc.render.filepath = os.path.join(OUT, f'{FID}_{vn}_{who}.png'); bpy.ops.render.render(write_still=True)
json.dump({'id': FID, 'budget': budget, 'rawTris': len(L.tris_of(raw)), 'oldTris': len(L.tris_of(old))}, open(os.path.join(OUT, f'{FID}_lod.json'), 'w'))
