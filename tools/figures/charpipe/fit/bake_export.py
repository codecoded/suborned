"""Brief 069: the baked garment-chain motion (animate.py's bake/*.json) as one Generic FBX clip per state, for Unity.
  FIT_SET=fixer blender.sh -P tools/figures/charpipe/fit/bake_export.py -- <figure id> <bake dir> <out dir>
Each FBX holds the chain bones and their ancestors only (Hips, ...: the same names and paths as in <id>_H.fbx) with keys on the chain bones: one key per
clip frame, rotation only, no simplification, the clip's frame range and rate. In Unity: import as Generic (no avatar), and play it on a second Animator
layer over the Humanoid body clip, with a Transform avatar mask of the chain bones and the layer synced to the body layer (handoff-latch.md)."""
import bpy, os, sys, json, glob, importlib.util
HERE = os.path.dirname(os.path.abspath(__file__))
L = importlib.util.module_from_spec(s := importlib.util.spec_from_file_location('fitlib', os.path.join(HERE, 'lib.py'))); s.loader.exec_module(L)

A = sys.argv[sys.argv.index('--') + 1:]; FID, BAKE, OUT = A[0], A[1], L.ensure(A[2])
done = []
for path in sorted(glob.glob(os.path.join(BAKE, '*.json'))):
    b = json.load(open(path)); clip = b['clip']
    bpy.ops.wm.open_mainfile(filepath=L.work(f'{FID}_H.blend'))
    arm = bpy.data.objects['Armature']; sc = bpy.context.scene
    sc.render.fps = int(round(b['fps'])); sc.render.fps_base = 1.0; f0, f1 = b['frames']; sc.frame_start, sc.frame_end = f0, f1
    for o in list(bpy.data.objects):
        if o is not arm: bpy.data.objects.remove(o, do_unlink=True)
    bones = sorted({bn for fr in b['rot'].values() for bn in fr})
    # keep only the chain bones and their ancestors (Hips, Head...): the same paths as in the figure, and nothing else to key
    keep = set(bones)
    for bn in bones:
        p_ = arm.data.bones[bn].parent
        while p_ is not None: keep.add(p_.name); p_ = p_.parent
    L.bp.ctx(arm, 'EDIT')
    for e in list(arm.data.edit_bones):
        if e.name not in keep: arm.data.edit_bones.remove(e)
    L.bp.ctx(arm, 'OBJECT')
    act = bpy.data.actions.new(f'{FID}_chains_{clip}'); arm.animation_data_create(); arm.animation_data.action = act
    for bn in bones: arm.pose.bones[bn].rotation_mode = 'QUATERNION'
    for fr in range(f0, f1 + 1):
        for bn, q in b['rot'][str(fr)].items():
            pb = arm.pose.bones[bn]; pb.rotation_quaternion = q; pb.keyframe_insert('rotation_quaternion', frame=fr)
    for fc in (act.fcurves if hasattr(act, 'fcurves') else []):
        for kp in fc.keyframe_points: kp.interpolation = 'LINEAR'
    L.bp.pin_fbx(); bpy.ops.object.select_all(action='DESELECT'); arm.select_set(True); bpy.context.view_layer.objects.active = arm
    out = os.path.join(OUT, f'{FID}_chains_{clip}.fbx')
    bpy.ops.export_scene.fbx(filepath=out, use_selection=True, object_types={'ARMATURE'}, add_leaf_bones=False, bake_anim=True, bake_anim_use_all_bones=False,
                             bake_anim_use_nla_strips=False, bake_anim_use_all_actions=False, bake_anim_force_startend_keying=True, bake_anim_step=1.0,
                             bake_anim_simplify_factor=0.0, axis_forward='-Z', axis_up='Y', apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
                             primary_bone_axis='Y', secondary_bone_axis='X', armature_nodetype='NULL', path_mode='STRIP')
    done.append({'clip': clip, 'file': os.path.basename(out), 'frames': [f0, f1], 'fps': b['fps'], 'bones': bones, 'seam': b['seam']})
json.dump(done, open(os.path.join(OUT, 'chains_clips.json'), 'w'), indent=1, sort_keys=True)
print('BAKED', [d['file'] for d in done])
