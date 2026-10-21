"""run vs run_lean on a cell figure (brief 020, Forma). blender -b -P tv_lean.py -- <fig> <People dir> <out dir>
Reuses tv_render.py's retarget stand-in and cameras; both clips go through the same stand-in so the comparison is fair."""
import sys, os, json
a = sys.argv[sys.argv.index('--') + 1:]
FIG, PEOPLE, OUT = a
src = open(os.path.join(os.path.dirname(os.path.abspath(__file__)), 'tv_render.py')).read().split('results = {')[0]
sys.argv = sys.argv[:sys.argv.index('--') + 1] + [FIG, '-', PEOPLE, OUT, 'run', 'run', 'run', 'run']
exec(compile(src, 'tv_render.py', 'exec'))
results = {'fig': FIG}
for impl, clipname in (('now', 'run'), ('lean', 'run_lean')):
    arm, meshes = load_now(); arm.rotation_euler.z = 0
    act, f0, N, fps = retarget(arm, f'{PEOPLE}/Clips/{clipname}.fbx', clipname)
    facing_to_plus_x(arm); g = setup_render(); clay_overrides(meshes)
    arm.location = (0, 0, 0); bpy.context.view_layer.update()
    m = metrics(arm, act, f0, N, fps, f'{impl} {clipname}')
    lows = []
    for i in range(48):
        set_phase(arm, act, f0, N, i / 48)
        lows.append(min(W(arm, n).z for n in ('LeftToeBase', 'RightToeBase', 'LeftFoot', 'RightFoot')))
    lift = -min(lows) + 0.03
    results[impl] = {'run': m}
    print('METRIC', FIG, json.dumps(m))
    shoot(arm, meshes, act, f0, N, fps, m['naturalSpeedMs'], f'{FIG}_{impl}_run', g, lift)
json.dump(results, open(f'{OUT}/{FIG}_metrics.json', 'w'), indent=1)
print('DONE', FIG)
