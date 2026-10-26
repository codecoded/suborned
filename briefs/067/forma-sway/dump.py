# bone world positions per frame for a few clips (Blender): {clip: {"fps":30, "frames":[{bone:[x,y,z]}]}}
import bpy, sys, json, os
a = sys.argv[sys.argv.index('--')+1:]; out = a[0]; files = a[1:]
BONES = ['Hips','Spine1','Neck','Head','LeftShoulder','LeftArm','LeftForeArm','LeftHand','RightShoulder','RightArm','RightForeArm','RightHand','LeftUpLeg','LeftLeg','LeftFoot','LeftToeBase','RightUpLeg','RightLeg','RightFoot','RightToeBase']
res = {}
for f in files:
    bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=f)
    arm = next(o for o in bpy.data.objects if o.type == 'ARMATURE'); act = arm.animation_data.action; s, e = map(int, act.frame_range)
    pb = {b.name.split(':')[-1]: b for b in arm.pose.bones}; fr = []
    for k in range(s, e+1):
        bpy.context.scene.frame_set(k); m = arm.matrix_world
        fr.append({b: list(m @ pb[b].head) for b in BONES if b in pb})
    res[os.path.basename(f)] = fr
json.dump(res, open(out, 'w'))
