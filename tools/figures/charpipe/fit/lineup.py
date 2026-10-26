"""Rest lineups for the fit test (brief 069): figures side by side in the standard rest, front and three-quarter back, close studio.
  blender.sh -P tools/figures/charpipe/fit/lineup.py -- <out png prefix> <label=blend> ..."""
import bpy, sys, os, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__))); import look
a = sys.argv[sys.argv.index('--') + 1:]; out = a[0]; figs = [x.split('=', 1) for x in a[1:]]
bpy.ops.wm.read_factory_settings(use_empty=True); sc = bpy.context.scene
gap = 1.0; n = len(figs)
for i, (lab, path) in enumerate(figs):
    with bpy.data.libraries.load(path, link=False) as (s, d): d.objects = list(s.objects)
    for o in d.objects:
        if o is None: continue
        sc.collection.objects.link(o)
        if o.type == 'MESH' and (o.get('fit_lod', 0) > 0 or o.get('fit_role') == 'donor'): o.hide_render = True
        if o.type == 'ARMATURE': o.location.x = (i - (n - 1) / 2) * gap
look.studio(sc, int(330 * n), 720); cam = look.camera(sc); cam.data.sensor_fit = 'VERTICAL'; cam.data.angle_y = math.radians(30)
span = (n - 1) * gap; dist = max(4.2, span * 1.6)
for name, ang in (('front', -8), ('back', 160)):
    r = math.radians(ang); look.look_at(cam, (dist * math.sin(r), -dist * math.cos(r), 1.25), (0, 0, 0.92))
    sc.render.filepath = f'{out}_{name}.png'; bpy.ops.render.render(write_still=True)
