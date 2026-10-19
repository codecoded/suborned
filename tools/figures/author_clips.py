"""Clips the Mixamo library does not have, posed here (brief 020). Run inside Blender; clips.py calls make() for a clips.json entry with an "author" key.

The skeleton, bone names, rest pose and units come from a donor library clip (clips.json "donor"), so the result is a clip-only Mixamo-skeleton FBX that
Unity reads as Humanoid like every other clip. Nothing is traced from another source: each bone is aimed along a direction written below, frame by frame.

sit_to_stand / stand_to_sit (make_pinned): the library's two transitions walk the pelvis half a metre (it backs into the chair, or steps away from it) and end on a seat of their
own, so they cannot chain with the sit idle on one fixed object root. Here the library clip is kept bone for bone and only the pelvis's horizontal travel is taken out:
each frame's Hips are slid sideways in the clip's plane until they sit over the sit idle's pelvis (the XY of its first frame). The body rises and falls over the seat, the feet
slide a little under it (they were planted in the library clip and the pelvis moved; here the pelvis is planted and the feet follow), and sit idle, stand_to_sit and
sit_to_stand all play on the same root.

seated (sit, sit_alt, sit_talk) and pinned both take entry['seat'] = [forward, up] (metres, in the clip's frame): where the pelvis sits over the object's root, measured from
the sit idle's own first-frame pelvis. The library chair was lower and nearer than the café's, and the Mixamo capture sits a hand's width forward of its origin; with
forward 0 and up +0.10 an object whose root is on the chair's centre at floor level sits on the café's Tulip chair with no offset of its own (manifest.seat).

carried_drape: a fireman's carry, the person draped belly-down over one shoulder. The pelvis is the apex, level and over the bar. The trunk folds forward and
hangs down one side (head and arms hanging, limp), the legs hang down the other. In the figure's own frame the trunk hangs toward +Z (the way it faces) and
the legs toward -Z, so the carrier seats the figure turned 180 degrees to his own heading: the trunk hangs down his back, the legs down his chest.
A slow limp sway (2.4 s loop) swings the hanging parts a few degrees about the shoulder line, so a walking carrier jogs them.
"""
import bpy, math, os
from mathutils import Vector, Matrix, Quaternion
from figlib import load, rigs

# The FBX exporter bakes a translation curve for every animated bone, with float noise around the rest offset; Unity's Humanoid import reads that as translation
# animation to discard and warns ("has translation animation that will be discarded", on every authored clip). Simplify 1.0 (Blender's default, about 0.1 degree and
# 0.1 mm) collapses the noise to constants, so the clips import clean; 0.3 and below still leave one arm bone per clip. FIG_SIMPLIFY overrides it (0 = none).
SIMPLIFY = float(os.environ.get('FIG_SIMPLIFY', '1.0'))
T = 72           # frames of the loop at 30 fps (2.4 s)
FPS = 30


def _dir(fw, up, a, b):
    return (fw * a + up * b).normalized()


def make(entry, store, dst):
    """Pose the donor's skeleton as entry['author'] says and write the FBX to dst. Returns the frame count."""
    bpy.ops.wm.read_factory_settings(use_empty=True)
    new = load(os.path.join(store, entry['donor']))
    arm, _ = rigs(new)
    for o in list(bpy.data.objects):
        if o.type != 'ARMATURE': bpy.data.objects.remove(o, do_unlink=True)
    sc = bpy.context.scene; sc.render.fps = FPS
    act = arm.animation_data.action if arm.animation_data else None
    if act: bpy.data.actions.remove(act)
    arm.animation_data_create()
    act = bpy.data.actions.new('mixamo.com'); arm.animation_data.action = act
    bpy.context.view_layer.objects.active = arm; bpy.ops.object.mode_set(mode='POSE')
    P = arm.pose.bones; M = arm.matrix_world; Mi = M.to_3x3().inverted()
    for pb in P: pb.rotation_mode = 'QUATERNION'; pb.rotation_quaternion = Quaternion(); pb.location = Vector()
    bpy.context.view_layer.update()

    def W(name):  # world head of a bone in the rest pose
        return M @ P['mixamorig:' + name].head
    up = Vector((0, 0, 1))
    toe = W('LeftToeBase') - W('LeftFoot'); toe.z = 0
    fw = toe.normalized()                                   # the way the figure faces
    lt = (W('LeftArm') - W('RightArm')); lt.z = 0; lt = lt.normalized()   # toward its left

    def aim(name, d_world):
        pb = P['mixamorig:' + name]
        d = (Mi @ d_world).normalized()
        m3 = pb.matrix.to_3x3(); c = (m3 @ Vector((0, 1, 0))).normalized()
        n3 = c.rotation_difference(d).to_matrix() @ m3
        pb.matrix = Matrix.Translation(pb.matrix.translation) @ n3.to_4x4()
        bpy.context.view_layer.update()

    ORDER = ['Hips', 'Spine', 'Spine1', 'Spine2', 'Neck', 'Head',
             'LeftShoulder', 'LeftArm', 'LeftForeArm', 'LeftHand', 'RightShoulder', 'RightArm', 'RightForeArm', 'RightHand',
             'LeftUpLeg', 'LeftLeg', 'LeftFoot', 'LeftToeBase', 'RightUpLeg', 'RightLeg', 'RightFoot', 'RightToeBase']
    names = [n for n in ORDER if 'mixamorig:' + n in P]

    def pose(t):
        a = 2 * math.pi * t / T
        s, s2, s3 = math.sin(a), math.sin(a + 1.1), math.sin(a + 2.2)
        L = lambda k: lt * k
        LEAN = 1.0   # how far the hanging parts lean out from under the carrier's shoulder (the cloth of his own body is in the way)
        lo = lambda k: lt * (k * LEAN)
        return {
            # pelvis level over the bar, its top toward the way the figure faces
            'Hips': _dir(fw, up, 0.97, 0.22),
            # the trunk folds forward over the bar and hangs, leaning out: each segment turns further down
            'Spine': (fw * 0.50 + up * -0.86 + lo(0.20)).normalized(),
            'Spine1': (fw * (0.16 + 0.05 * s2) + up * -0.98 + lo(0.34)).normalized(),
            'Spine2': (fw * (0.05 + 0.07 * s2) + up * -1.0 + lo(0.42)).normalized(),
            'Neck': (fw * (0.10 + 0.10 * s3) + up * -0.99 + lo(0.45)).normalized(),
            'Head': (fw * (0.22 + 0.16 * s3) + up * -0.97 + lo(0.45)).normalized(),
            # arms hang from the hanging shoulders, limp and well apart (they read as arms from above)
            'LeftShoulder': (L(1.0) + up * -0.30).normalized(),
            'RightShoulder': (L(-1.0) + up * -0.30).normalized(),
            'LeftArm': (L(0.62) + up * -0.80 + fw * (0.10 + 0.16 * s3)).normalized(),
            'RightArm': (L(-0.50) + up * -0.84 + fw * (0.10 - 0.16 * s3)).normalized(),
            'LeftForeArm': (L(0.50) + up * -0.86 + fw * (0.16 + 0.20 * s3)).normalized(),
            'RightForeArm': (L(-0.40) + up * -0.90 + fw * (0.16 - 0.20 * s3)).normalized(),
            'LeftHand': (L(0.36) + up * -0.92 + fw * (0.12 + 0.14 * s3)).normalized(),
            'RightHand': (L(-0.30) + up * -0.94 + fw * (0.12 - 0.14 * s3)).normalized(),
            # legs hang the other way, leaning out and apart, knees a hair bent, feet limp
            'LeftUpLeg': (L(0.30) + lo(0.34) + up * -0.92 + fw * (-0.06 + 0.10 * s)).normalized(),
            'RightUpLeg': (L(-0.18) + lo(0.34) + up * -0.92 + fw * (-0.06 + 0.10 * s2)).normalized(),
            'LeftLeg': (L(0.26) + lo(0.38) + up * -0.94 + fw * (-0.10 + 0.12 * s)).normalized(),
            'RightLeg': (L(-0.14) + lo(0.38) + up * -0.94 + fw * (-0.10 + 0.12 * s2)).normalized(),
            'LeftFoot': (up * -0.80 + fw * (0.55 - 0.1 * s) + lo(0.3)).normalized(),
            'RightFoot': (up * -0.80 + fw * (0.55 - 0.1 * s2) + lo(0.3)).normalized(),
            'LeftToeBase': (up * -0.4 + fw * 0.9).normalized(),
            'RightToeBase': (up * -0.4 + fw * 0.9).normalized(),
        }

    for t in range(T + 1):
        D = pose(t % T)
        for n in names:
            if n in D: aim(n, D[n])
        for n in names:
            pb = P['mixamorig:' + n]
            if n != 'Hips': pb.location = Vector()   # only the hips travel (a translation curve on another bone is discarded by Unity's Humanoid import, with a warning)
            pb.keyframe_insert('rotation_quaternion', frame=t + 1)
            if n == 'Hips': pb.keyframe_insert('location', frame=t + 1)
    act.frame_range = (1, T + 1)
    sc.frame_start, sc.frame_end = 1, T + 1
    bpy.ops.object.mode_set(mode='OBJECT')
    bpy.ops.object.select_all(action='DESELECT'); arm.select_set(True); bpy.context.view_layer.objects.active = arm
    bpy.ops.export_scene.fbx(filepath=dst, use_selection=True, object_types={'ARMATURE'}, add_leaf_bones=False, bake_anim=True,
                             bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False, bake_anim_simplify_factor=SIMPLIFY, bake_anim_step=1.0,
                             axis_forward='-Z', axis_up='Y', apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', bake_space_transform=False,
                             primary_bone_axis='Y', secondary_bone_axis='X', armature_nodetype='NULL', path_mode='STRIP')
    return T + 1


def make_carry(entry, store, dst):
    """carry_shoulder / carry_shoulder_idle: a library walk (or idle) with the arm on the load's side clamping the load, everything else as the library has it
    (feet, hips, spine, the other arm). The carrier holds a person draped over his RIGHT shoulder (carried_drape): the legs hang down his chest, so that arm
    reaches round them: a two-bone solve puts the wrist at the carried legs' knees (between them, in front of his chest), the elbow drops back and out, the hand
    points inboard across the legs and its fingers curl closed. The wrist target is a point fixed in the carrier's root frame, measured from his shoulder bone
    (the carried figure is seated from that bone, so the hand rides with the legs as he walks); it is stored as a fraction of his arm length so it scales with
    the rig. Nothing is traced: the offsets below were read off the seated pair (tools/figures/scratch/FigCarry.cs -probe) and written down.
    entry: donor = the library clip to start from; arm = 'Right' or 'Left'; the dict 'hold' may override CLAMP."""
    bpy.ops.wm.read_factory_settings(use_empty=True)
    new = load(os.path.join(store, entry['donor']))
    arm, _ = rigs(new)
    for o in list(bpy.data.objects):
        if o.type != 'ARMATURE': bpy.data.objects.remove(o, do_unlink=True)
    sc = bpy.context.scene; sc.render.fps = FPS
    act0 = arm.animation_data.action
    f0, f1 = int(act0.frame_range[0]), int(act0.frame_range[1])
    bpy.context.view_layer.objects.active = arm; bpy.ops.object.mode_set(mode='POSE')
    P = arm.pose.bones; M = arm.matrix_world; Mi = M.to_3x3().inverted()
    side = entry.get('arm', 'Right')
    def W(name): return M @ P['mixamorig:' + name].head

    def aim(name, d_world):
        pb = P['mixamorig:' + name]
        d = (Mi @ d_world).normalized()
        m3 = pb.matrix.to_3x3(); c = (m3 @ Vector((0, 1, 0))).normalized()
        n3 = c.rotation_difference(d).to_matrix() @ m3
        pb.matrix = Matrix.Translation(pb.matrix.translation) @ n3.to_4x4()
        bpy.context.view_layer.update()

    # The carrier's own frame, fixed for the clip: up, the way he faces, his right (the chest line averaged over the loop so the walk's twist cancels)
    up = Vector((0, 0, 1)); acc = Vector()
    for f in range(f0, f1 + 1):
        sc.frame_set(f); v = W('LeftArm') - W('RightArm'); v.z = 0; acc += v.normalized()
    lt = acc.normalized(); fw = lt.cross(up).normalized()
    rt = -lt                                     # his right
    sgn = 1.0 if side == 'Right' else -1.0       # right-ward component of this arm's directions
    out = rt * sgn
    # (outward, up, forward) metres in the enforcer's frame, measured from his shoulder bone to the point between the carried legs at the knee
    # (the seated pair, probe: carried knees r 0.131 / 0.424, u 1.21 / 1.28, f 0.14 / 0.12; shoulder bone r 0.072 u 1.401 f 0.013); fraction of arm length 0.424
    kind = entry.get('kind', 'carry')
    if kind == 'tray':
        # a serving tray at waist height: the elbow in at the side, the forearm forward and a little up, the palm up under the tray (the hand's local Z is its palm
        # normal in the Mixamo rest pose, where palms face down). Offsets as for the carry, in the enforcer's metres from the shoulder bone.
        CLAMP = entry.get('hold') or {
            'wrist': (0.13, -0.24, 0.26),          # upper arm hanging, forearm forward: the elbow is bent about 90 degrees (arm segments 0.184 and 0.24 at the ref)
            'ref': 0.424,
            'pole': (0.5, -1.0, -0.1),
            'hand': (-0.10, 0.12, 1.0),
            'palm': (0.0, 1.0, 0.0),             # the palm faces up
        }
    else:
        CLAMP = entry.get('hold') or {
            'wrist': (0.205, -0.19, 0.16),           # wrist, from the shoulder bone
            'ref': 0.424,                            # arm length (upper arm + forearm) the offsets were read at
            'pole': (0.55, -1.0, -0.35),             # elbow drops down, out and a little back (outward, up, forward)
            'hand': (-0.80, -0.25, 0.45),            # the hand points inboard across the legs (outward, up, forward)
            'curl': 1.15,                            # finger curl, radians at the middle joint
        }

    def roll_palm(name, want_world):
        """Turn a bone about its own Y axis until its local Z (the palm normal) points as near to want_world as the bone's direction allows."""
        pb = P['mixamorig:' + name]; m3 = pb.matrix.to_3x3()
        y = (m3 @ Vector((0, 1, 0))).normalized(); z = (m3 @ Vector((0, 0, 1))).normalized()
        t = (Mi @ want_world).normalized(); t = (t - y * t.dot(y))
        if t.length < 1e-6: return
        t.normalize()
        ang = math.atan2(y.dot(z.cross(t)), z.dot(t))
        n3 = Matrix.Rotation(ang, 3, y) @ m3
        pb.matrix = Matrix.Translation(pb.matrix.translation) @ n3.to_4x4()
        bpy.context.view_layer.update()

    names = [n[len('mixamorig:'):] for n in P.keys()]
    prefix = side
    final = {}
    for f in range(f0, f1 + 1):
        sc.frame_set(f)
        S = W(prefix + 'Arm'); E0 = W(prefix + 'ForeArm'); H0 = W(prefix + 'Hand')
        l1 = (E0 - S).length; l2 = (H0 - E0).length
        k = (l1 + l2) / CLAMP['ref']
        o, u_, fo = CLAMP['wrist']
        T = W(prefix + 'Shoulder') + (out * o + up * u_ + fw * fo) * k
        d = T - S; dist = d.length
        dist = min(max(dist, abs(l1 - l2) + 1e-3), l1 + l2 - 1e-3); dn = d.normalized()
        a = (l1 * l1 - l2 * l2 + dist * dist) / (2 * dist); h = math.sqrt(max(l1 * l1 - a * a, 0.0))
        po, pu, pf = CLAMP['pole']; pole = out * po + up * pu + fw * pf
        perp = pole - dn * pole.dot(dn); perp = perp.normalized()
        E = S + dn * a + perp * h
        Wr = S + dn * dist
        aim(prefix + 'Arm', E - S)
        aim(prefix + 'ForeArm', Wr - E)
        ho, hu, hf = CLAMP['hand']
        aim(prefix + 'Hand', (out * ho + up * hu + fw * hf).normalized())
        if kind == 'tray':
            pa, pu_, pf = CLAMP['palm']; roll_palm(prefix + 'Hand', out * pa + up * pu_ + fw * pf)
        else:
            # fingers close round the legs: a curl about each finger bone's own X axis (knuckle, middle, tip joints)
            c = CLAMP['curl']
            for fn in ('Index', 'Middle', 'Ring', 'Pinky'):
                for j, q in ((1, 0.55), (2, 1.0), (3, 0.7)):
                    pb = P[f'mixamorig:{prefix}Hand{fn}{j}']; pb.rotation_mode = 'QUATERNION'
                    pb.rotation_quaternion = Quaternion((1, 0, 0), c * q)
            for j, q in ((1, 0.3), (2, 0.5), (3, 0.5)):
                pb = P[f'mixamorig:{prefix}HandThumb{j}']; pb.rotation_mode = 'QUATERNION'
                pb.rotation_quaternion = Quaternion((0, 0, 1), c * q * 0.5)
        bpy.context.view_layer.update()
        final[f] = {n: P['mixamorig:' + n].matrix_basis.copy() for n in names}
    act0.name = 'orig'
    arm.animation_data.action = None
    act = bpy.data.actions.new('mixamo.com'); arm.animation_data.action = act
    for f in range(f0, f1 + 1):
        for n in names:
            pb = P['mixamorig:' + n]; loc, rot, _ = final[f][n].decompose()
            pb.rotation_mode = 'QUATERNION'; pb.rotation_quaternion = rot
            # only the hips travel: a translation curve on any other bone is discarded by Unity's Humanoid import (and it warns), so it is written as none
            pb.location = loc if n == 'Hips' else Vector()
            pb.keyframe_insert('rotation_quaternion', frame=f - f0 + 1)
            if n == 'Hips': pb.keyframe_insert('location', frame=f - f0 + 1)
    act.frame_range = (1, f1 - f0 + 1)
    sc.frame_start, sc.frame_end = 1, f1 - f0 + 1
    bpy.ops.object.mode_set(mode='OBJECT')
    bpy.ops.object.select_all(action='DESELECT'); arm.select_set(True); bpy.context.view_layer.objects.active = arm
    bpy.ops.export_scene.fbx(filepath=dst, use_selection=True, object_types={'ARMATURE'}, add_leaf_bones=False, bake_anim=True,
                             bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False, bake_anim_simplify_factor=SIMPLIFY, bake_anim_step=1.0,
                             axis_forward='-Z', axis_up='Y', apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', bake_space_transform=False,
                             primary_bone_axis='Y', secondary_bone_axis='X', armature_nodetype='NULL', path_mode='STRIP')
    return f1 - f0 + 1


def make_pinned(entry, store, dst):
    """The library clip entry['donor'] with the pelvis moved over the object's root: entry['kind'] 'pinned' pins the Hips' horizontal travel over the place the sit idle (entry['pin'],
    first frame) has the pelvis, every frame; 'seated' keeps the clip's own sway and slides the whole clip by the same constant. Both then add entry['seat'] = [forward, up]
    (see the module note). Every other bone's rotation is the library's. Returns the frame count."""
    bpy.ops.wm.read_factory_settings(use_empty=True)
    tgt = load(os.path.join(store, entry['pin'])); tarm, _ = rigs(tgt)
    sc = bpy.context.scene; sc.render.fps = FPS
    tact = tarm.animation_data.action; sc.frame_set(int(tact.frame_range[0]))
    pin = tarm.matrix_world @ tarm.pose.bones['mixamorig:Hips'].head; pin = Vector((pin.x, pin.y, 0.0))
    # the way the figure faces, from its rest pose (every bone back to identity)
    for pb in tarm.pose.bones: pb.rotation_mode = 'QUATERNION'; pb.rotation_quaternion = Quaternion(); pb.location = Vector()
    bpy.context.view_layer.update()
    Mt = tarm.matrix_world; toe = Mt @ tarm.pose.bones['mixamorig:LeftToeBase'].head - Mt @ tarm.pose.bones['mixamorig:LeftFoot'].head; toe.z = 0; fw = toe.normalized()
    for o in list(bpy.data.objects): bpy.data.objects.remove(o, do_unlink=True)
    new = load(os.path.join(store, entry['donor'])); arm, _ = rigs(new)
    for o in list(bpy.data.objects):
        if o.type != 'ARMATURE': bpy.data.objects.remove(o, do_unlink=True)
    act0 = arm.animation_data.action; f0, f1 = int(act0.frame_range[0]), int(act0.frame_range[1])
    sf, su = entry.get('seat', [0.0, 0.0])
    want = pin + fw * sf
    bpy.context.view_layer.objects.active = arm; bpy.ops.object.mode_set(mode='POSE')
    P = arm.pose.bones; M = arm.matrix_world; M3i = M.to_3x3().inverted()
    hb = P['mixamorig:Hips']; R3i = hb.bone.matrix_local.to_3x3().inverted()
    names = [n[len('mixamorig:'):] for n in P.keys()]
    final = {}
    for f in range(f0, f1 + 1):
        sc.frame_set(f)
        cur = M @ hb.head
        d = (want - Vector((cur.x, cur.y, 0.0))) if entry.get('kind') == 'pinned' else (want - pin)
        d = Vector((d.x, d.y, su))
        loc, rot, _ = hb.matrix_basis.decompose()
        shift = loc + R3i @ (M3i @ d)
        final[f] = {n: P['mixamorig:' + n].matrix_basis.copy() for n in names}
        final[f]['Hips'] = Matrix.Translation(shift) @ rot.to_matrix().to_4x4()
    act0.name = 'orig'; arm.animation_data.action = None
    act = bpy.data.actions.new('mixamo.com'); arm.animation_data.action = act
    for f in range(f0, f1 + 1):
        for n in names:
            pb = P['mixamorig:' + n]; loc, rot, _ = final[f][n].decompose()
            pb.rotation_mode = 'QUATERNION'; pb.rotation_quaternion = rot
            pb.location = loc if n == 'Hips' else Vector()
            pb.keyframe_insert('rotation_quaternion', frame=f - f0 + 1)
            if n == 'Hips': pb.keyframe_insert('location', frame=f - f0 + 1)
    act.frame_range = (1, f1 - f0 + 1)
    sc.frame_start, sc.frame_end = 1, f1 - f0 + 1
    bpy.ops.object.mode_set(mode='OBJECT')
    bpy.ops.object.select_all(action='DESELECT'); arm.select_set(True); bpy.context.view_layer.objects.active = arm
    bpy.ops.export_scene.fbx(filepath=dst, use_selection=True, object_types={'ARMATURE'}, add_leaf_bones=False, bake_anim=True,
                             bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False, bake_anim_simplify_factor=SIMPLIFY, bake_anim_step=1.0,
                             axis_forward='-Z', axis_up='Y', apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', bake_space_transform=False,
                             primary_bone_axis='Y', secondary_bone_axis='X', armature_nodetype='NULL', path_mode='STRIP')
    return f1 - f0 + 1
