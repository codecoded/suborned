# every shipped clip: hips XZ range, first->last hips travel, and how far a planted foot moves (Blender, in the clip's own frame)
import bpy, sys, json, os
d = sys.argv[sys.argv.index('--')+1]; names = json.load(open(sys.argv[sys.argv.index('--')+2]))['clips']
out = []
for c in names:
    f = os.path.join(d, c['name'] + '.fbx')
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=f)
    arm = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
    act = arm.animation_data.action; s, e = map(int, act.frame_range)
    pb = {b.name.split(':')[-1]: b for b in arm.pose.bones}
    H = []; F = {'LeftFoot': [], 'RightFoot': []}
    for fr in range(s, e+1):
        bpy.context.scene.frame_set(fr); m = arm.matrix_world
        h = m @ pb['Hips'].head; H.append((h.x, h.y))
        for k in F: p = m @ pb[k].head; F[k].append((p.x, p.y, p.z))
    rng = lambda a: max(a) - min(a)
    foot = max(max(rng([p[0] for p in L]), rng([p[1] for p in L])) for L in F.values())
    travel = ((H[-1][0]-H[0][0])**2 + (H[-1][1]-H[0][1])**2) ** 0.5
    out.append(dict(clip=c['name'], loco=c.get('loco', False), s=round((e-s+1)/30, 1), hipSide=round(rng([h[0] for h in H]), 3), hipFwd=round(rng([h[1] for h in H]), 3), hipTravelEnd=round(travel, 3), endDX=round(H[-1][0]-H[0][0], 3), endDY=round(H[-1][1]-H[0][1], 3), footMove=round(foot, 3)))
open(os.path.join(os.path.dirname(os.path.abspath(__file__)), 'all.json'), 'w').write(json.dumps(out, indent=1))
print('JSON' + json.dumps(out))
