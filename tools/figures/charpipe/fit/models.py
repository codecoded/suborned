"""Brief 069 end-to-end: the static model check (Sky via Aster, 2026-10-27: 'no point doing the video if the models are bad'), before any animation.
  FIT_SET=e2e blender.sh -P tools/figures/charpipe/fit/models.py -- <id> <out dir> [--shoulders]
Renders, at one studio light and one camera per view: the raw generated mesh (as delivered, its own texture, brought to the fitted figure's size and
place: the fit's normalisation, then the fit's scale and offset taken out, exactly as the figure) beside the fitted figure (LOD0, the figure's own
pieces, no props, no helpers) in its rest (the source A-pose). Views: front, back, three-quarter close-up of face and shoulders (also at 2x).
--shoulders: the fitted figure at rest, arms down (each upper arm and forearm turned 40 deg down from rest) and arms up (to horizontal), front close-up.
Writes <out>/<id>_<view>.png."""
import bpy, os, sys, json, math, importlib.util
import numpy as np
HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE); import look
L = importlib.util.module_from_spec(s := importlib.util.spec_from_file_location('fitlib', os.path.join(HERE, 'lib.py'))); s.loader.exec_module(L)
from mathutils import Vector, Matrix

A = sys.argv[sys.argv.index('--') + 1:]; FID = A[0]; OUT = L.ensure(A[1]); SH = '--shoulders' in A
row = {r['id']: r for r in L.D['inputs']}[FID]; fit = json.load(open(os.path.join(L.SCR, 'fit', f'{FID}.fit.json')))
bpy.ops.wm.read_factory_settings(use_empty=True); sc = bpy.context.scene
# the figure
with bpy.data.libraries.load(L.work(f'{FID}_H.blend'), link=False) as (src, dst): dst.objects = list(src.objects)
fig = [o for o in dst.objects if o is not None]
for o in fig:
    sc.collection.objects.link(o)
    if o.type == 'MESH' and (o.get('fit_lod', 0) > 0 or o.get('fit_role') in ('donor', 'prop')): o.hide_render = True
arm = next(o for o in fig if o.type == 'ARMATURE')
# the raw mesh, placed as the figure was made from it
raw = L.import_glb(row['src']); L.normalise(raw); p = fit['params']
V = (np.array([list(v.co) for v in raw.data.vertices]) - np.array([p['tx'], p['ty'], p['tz']])) / p['s']; L.set_co(raw, V)
L.flat_material(raw, L.base_color_image(raw)); raw.name = 'raw'
GAP = 0.9; raw.location.x = -GAP / 2; arm.location.x = GAP / 2
look.studio(sc, 1000, 900); cam = look.camera(sc); cam.data.sensor_fit = 'VERTICAL'
H = float(V[:, 2].max())
if not SH:
    for name, eye, tgt, fov, w, h in (('front', (0, -5.2, H * 0.55), (0, 0, H * 0.5), 24, 1000, 900), ('back', (0, 5.2, H * 0.55), (0, 0, H * 0.5), 24, 1000, 900),
                                      ):
        sc.render.resolution_x, sc.render.resolution_y = w, h; cam.data.angle_y = math.radians(fov)
        look.look_at(cam, eye, tgt); sc.render.filepath = os.path.join(OUT, f'{FID}_{name}.png'); bpy.ops.render.render(write_still=True)
    # three-quarter close-ups of face and shoulders: each subject alone and centred, the same camera relative to it
    figm = [o for o in fig if o.type == 'MESH' and not o.hide_render]
    for name, k, w, h in (('close', 1, 700, 760), ('close@2x', 2, 1400, 1520)):
        for who, x in (('raw', raw.location.x), ('fit', arm.location.x)):
            raw.hide_render = who != 'raw'
            for o in figm: o.hide_render = who != 'fit'
            sc.render.resolution_x, sc.render.resolution_y = w, h; cam.data.angle_y = math.radians(20)
            look.look_at(cam, (x - 0.75, -1.35, H * 0.9), (x, 0, H * 0.82)); sc.render.filepath = os.path.join(OUT, f'{FID}_{name}_{who}.png'); bpy.ops.render.render(write_still=True)
        raw.hide_render = False
        for o in figm: o.hide_render = False
else:
    raw.hide_render = True; arm.location.x = 0
    sc.render.resolution_x, sc.render.resolution_y = 900, 800; cam.data.angle_y = math.radians(20)
    def arms(deg):
        L.bp.ctx(arm, 'POSE')
        for pb in arm.pose.bones: pb.rotation_mode = 'QUATERNION'; pb.rotation_quaternion = (1, 0, 0, 0)
        bpy.context.view_layer.update()
        for s_, sg in (('Left', 1), ('Right', -1)):
            pb = arm.pose.bones[s_ + 'Arm']; hd = pb.head.copy()
            R = Matrix.Rotation(math.radians(deg) * sg, 4, Vector((0, -1, 0)))       # + = up (outward) for both sides about the forward axis
            pb.matrix = Matrix.Translation(hd) @ R @ Matrix.Translation(-hd) @ pb.matrix; bpy.context.view_layer.update()
        L.bp.ctx(arm, 'OBJECT')
    # arm angle now: the rest's drop below horizontal
    h = arm.matrix_world @ arm.data.bones['LeftArm'].head_local; t = arm.matrix_world @ arm.data.bones['LeftArm'].tail_local
    drop = math.degrees(math.atan2(h.z - t.z, abs(t.x - h.x)))
    for name, deg in (('rest', 0.0), ('armsdown', -40.0), ('armsup', drop)):
        arms(deg); look.look_at(cam, (-0.6, -2.4, H * 0.85), (0, 0, H * 0.75)); sc.render.filepath = os.path.join(OUT, f'{FID}_sh_{name}.png'); bpy.ops.render.render(write_still=True)
    json.dump({'restArmDropDeg': round(drop, 1)}, open(os.path.join(OUT, f'{FID}_sh.json'), 'w'))
