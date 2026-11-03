"""Brief 069: a new fitted figure beside today's installed figure, at one scale and one light (static, no animation).
  FIT_SET=fixer blender.sh -P tools/figures/charpipe/fit/beside.py -- <new id> <today's figure id> <out dir>
Today's figure: unity/Assets/Suborned/People/Figures/<id>/<id>.fbx (read only) with its own albedo per LOD material; posed at its file's rest.
The new figure: the set's work/<id>_H.blend at its rest (the source A-pose). Both at true size in metres, standing on the ground, facing the camera.
Views: front, back, three-quarter close of face and shoulders, and the play camera (ortho, 38 deg down, yaw 30, 38.571 px/m: the game's play
zoom, a 1.75 m figure is 53 px) with each figure's LAST LOD, written at 1x and enlarged 3x (nearest). Writes <out>/beside_<view>.png."""
import bpy, os, sys, glob, math, importlib.util
import numpy as np
HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE); import look
L = importlib.util.module_from_spec(s := importlib.util.spec_from_file_location('fitlib', os.path.join(HERE, 'lib.py'))); s.loader.exec_module(L)
from mathutils import Vector

A = sys.argv[sys.argv.index('--') + 1:]; NEW, OLD, OUT = A[0], A[1], L.ensure(A[2])
PEOPLE = os.path.join(L.ROOT, 'unity/Assets/Suborned/People/Figures')
bpy.ops.wm.read_factory_settings(use_empty=True); sc = bpy.context.scene

# the new figure
with bpy.data.libraries.load(L.work(f'{NEW}_H.blend'), link=False) as (src, dst): dst.objects = list(src.objects)
new = [o for o in dst.objects if o is not None]
for o in new: sc.collection.objects.link(o)
narm = next(o for o in new if o.type == 'ARMATURE')
# show what ships: the delivered albedo (budget size, merged props' colour patches painted in) on the figure's own material
_tex = sorted(__import__('glob').glob(os.path.join(L.SCR, 'a', f'{NEW}_H_albedo.*')))
if _tex:
    _img = bpy.data.images.load(_tex[0])
    for _o in new:
        if _o.type == 'MESH' and _o.get('fit_role') in ('body', 'garment'):
            for _m in _o.data.materials:
                if _m and _m.use_nodes:
                    for _n in _m.node_tree.nodes:
                        if _n.type == 'TEX_IMAGE': _n.image = _img
nmesh = {o.get('fit_lod', 0): [] for o in new if o.type == 'MESH' and o.get('fit_role') not in ('donor', 'prop')}
for o in new:
    if o.type == 'MESH' and o.get('fit_role') not in ('donor', 'prop'): nmesh[o.get('fit_lod', 0)].append(o)
    if o.type == 'MESH' and o.get('fit_role') in ('donor', 'prop'): o.hide_render = True

# today's figure: its FBX, one matte material per LOD from its own albedo
before = set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=os.path.join(PEOPLE, OLD, f'{OLD}.fbx'))
old = [o for o in bpy.data.objects if o not in before]; oarm = next(o for o in old if o.type == 'ARMATURE')
if oarm.animation_data: oarm.animation_data_clear()
omesh = sorted([o for o in old if o.type == 'MESH'], key=lambda o: o.name)
def lod_of(o):
    n = o.name.rsplit('LOD', 1); return int(n[1][:1]) if len(n) == 2 and n[1][:1].isdigit() else 0
for o in omesh:
    for i, slot in enumerate(o.material_slots):
        nm = slot.material.name.split('.')[0] if slot.material else ''
        tex = sorted(glob.glob(os.path.join(PEOPLE, OLD, 'Textures', nm + '_albedo.*')))
        o.material_slots[i].material = L.simple_material(nm + '_view', bpy.data.images.load(tex[0]) if tex else None)
bpy.context.view_layer.update()
# stand both on the ground at true size: today's FBX is placed as imported (its own scale), lifted so its soles touch z = 0
def lowest(objs):
    return min(float(L.eval_co(o)[:, 2].min()) for o in objs)
GAP = 0.85
for root, objs, x in ((narm, [o for o in nmesh.get(0, [])], GAP / 2), (oarm, [o for o in omesh if lod_of(o) == 0] or omesh, -GAP / 2)):
    root.location.x += x; bpy.context.view_layer.update(); root.location.z -= lowest(objs); bpy.context.view_layer.update()
hn = max(float(L.eval_co(o)[:, 2].max()) for o in nmesh[0]); ho = max(float(L.eval_co(o)[:, 2].max()) for o in [o for o in omesh if lod_of(o) == 0] or omesh)
open(os.path.join(OUT, 'beside_heights.txt'), 'w').write(f'new {NEW}: {hn:.3f} m; today {OLD}: {ho:.3f} m (today\'s FBX as imported, no rescale)\n')

def show(lod):
    last_new = max(nmesh); last_old = max(lod_of(o) for o in omesh)
    for k, objs in nmesh.items():
        for o in objs: o.hide_render = k != (last_new if lod == 'last' else 0)
    for o in omesh: o.hide_render = lod_of(o) != (last_old if lod == 'last' else 0)

look.studio(sc, 1000, 900); cam = look.camera(sc); cam.data.sensor_fit = 'VERTICAL'
H = max(hn, ho); show(0)
for name, eye, tgt, fov, w, h in (('front', (0, -5.4, H * 0.55), (0, 0, H * 0.5), 24, 1000, 900), ('back', (0, 5.4, H * 0.55), (0, 0, H * 0.5), 24, 1000, 900),
                                  ('close', (-0.5, -2.6, H * 0.88), (0, 0, H * 0.8), 22, 1200, 700)):
    sc.render.resolution_x, sc.render.resolution_y = w, h; cam.data.type = 'PERSP'; cam.data.angle_y = math.radians(fov)
    look.look_at(cam, eye, tgt); sc.render.filepath = os.path.join(OUT, f'beside_{name}.png'); bpy.ops.render.render(write_still=True)
# play zoom, last LOD, both facing the camera's yaw so they read as on the street
show('last'); P = L.D['render']['play']
for root in (narm, oarm): root.rotation_euler.z += math.radians(180 - (180 + P['yawDeg']))
look.play_cam(cam, (0, 0, 0.85), P['yawDeg'], P['pitchDeg'], P['ppm'], P['h'])
sc.render.resolution_x, sc.render.resolution_y = P['w'], P['h']; sc.render.filepath = os.path.join(OUT, 'beside_play_1x.png'); bpy.ops.render.render(write_still=True)
