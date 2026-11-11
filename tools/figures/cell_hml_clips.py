"""Clip work for the Happy Mouse cell (brief 112, tools/figures/cell_hml.py): a bundle action as an FBX take, the authored shoulder carry, the seat offset, a clip's ground speed.
Runs inside Blender. The four rigs name their bones differently (UpperArm / Arm, LowerLeg / Leg, Toes / ToeBase), so every bone is asked for by one name and found by either."""
import math
import bpy
from mathutils import Vector, Matrix

ALT = {'UpperArm': 'Arm', 'LowerArm': 'ForeArm', 'UpperLeg': 'UpLeg', 'LowerLeg': 'Leg', 'Toes': 'ToeBase'}


def bone(arm, key):
    P = arm.pose.bones
    if key in P: return P[key]
    for k, v in ALT.items():
        if key.endswith(k) and key[:-len(k)] + v in P: return P[key[:-len(k)] + v]
    raise KeyError(f'no bone {key}')


def channelbags(act):
    for l in act.layers:
        for s in l.strips:
            for cb in s.channelbags: yield cb


def fcurves(act):
    for cb in channelbags(act):
        for fc in cb.fcurves: yield fc


def assign(arm, act):
    ad = arm.animation_data; ad.action = act
    if act is not None and ad.action_slot is None and len(act.slots): ad.action_slot = act.slots[0]


def prep(src, name, fps, seconds=None):
    """A copy of a bundle action that starts at frame 0 and ends on a whole frame (the FBX bake samples whole frames), re-timed to `seconds` when given. Returns (action, last frame)."""
    act = src.copy(); act.name = name; act.use_fake_user = True
    f0, f1 = act.frame_range; span = f1 - f0
    n = max(1, round(seconds * fps)) if seconds else max(1, round(span)); k = n / span if span > 0 else 1.0
    for fc in fcurves(act):
        for kp in fc.keyframe_points:
            kp.co.x = (kp.co.x - f0) * k; kp.handle_left.x = (kp.handle_left.x - f0) * k; kp.handle_right.x = (kp.handle_right.x - f0) * k
        fc.update()
    return act, n


def shift(arm, act, name, delta_world):
    """Slide a clip: add a constant world offset to a bone's location keys (made at zero when the clip has none)."""
    pb = arm.pose.bones[name]
    R = (pb.parent.matrix @ pb.parent.bone.matrix_local.inverted() @ pb.bone.matrix_local).to_3x3() if pb.parent else pb.bone.matrix_local.to_3x3()
    d = R.inverted() @ (arm.matrix_world.to_3x3().inverted() @ delta_world); path = f'pose.bones["{name}"].location'
    for cb in channelbags(act):
        if not any(fc.data_path.startswith('pose.bones') for fc in cb.fcurves): continue
        for i in range(3):
            fc = cb.fcurves.find(path, index=i)
            if fc is None: fc = cb.fcurves.new(path, index=i); fc.keyframe_points.insert(0, 0.0)
            for kp in fc.keyframe_points: kp.co.y += d[i]; kp.handle_left.y += d[i]; kp.handle_right.y += d[i]
            fc.update()


def soles_rest(arm):
    """Each foot bone's height at rest, where the soles stand on z = 0."""
    return {bone(arm, s + k).name: (arm.matrix_world @ bone(arm, s + k).bone.head_local).z for s in ('Left', 'Right') for k in ('Foot', 'Toes')}


def soles(arm, rest):
    """The lower sole now (m above the floor): the heel under the ankle or the ball under the toe joint, whichever foot is lower."""
    return min((arm.matrix_world @ arm.pose.bones[n].head).z - z for n, z in rest.items())


def ground(arm, act, n, rest):
    """Feet on the floor for a standing clip. A clip that floats (its lowest sole over the whole clip is above z = 0) is dropped by that much. Then every frame whose lower
    sole is under the floor is lifted by exactly that much: a retarget without foot contact stands right and sinks as it crouches or strides (the Fixer's heels: up to 9 cm),
    so one offset for the clip would leave it floating where it stood right. Returns {'drop', 'liftMax'} in metres, None when it is within a centimetre."""
    sc = bpy.context.scene; assign(arm, act); hips = bone(arm, 'Hips').name; low = []
    for f in range(n + 1): sc.frame_set(f); bpy.context.view_layer.update(); low.append(soles(arm, rest))
    drop = min(low) if min(low) > 0.01 else 0.0; lift = [max(0.0, drop - z) for z in low]
    if not drop and max(lift) <= 0.01: return None
    if drop: shift(arm, act, hips, Vector((0, 0, -drop)))
    if max(lift) > 0.01:
        shift(arm, act, hips, Vector((0, 0, 0)))   # makes the location curves when the clip has none
        pb = arm.pose.bones[hips]; path = f'pose.bones["{hips}"].location'
        sc.frame_set(0); bpy.context.view_layer.update()
        R = (pb.parent.matrix @ pb.parent.bone.matrix_local.inverted() @ pb.bone.matrix_local).to_3x3() if pb.parent else pb.bone.matrix_local.to_3x3()
        up = R.inverted() @ (arm.matrix_world.to_3x3().inverted() @ Vector((0, 0, 1)))
        for cb in channelbags(act):
            for i in range(3):
                fc = cb.fcurves.find(path, index=i)
                if fc is None: continue
                vals = [fc.evaluate(f) + up[i] * lift[f] for f in range(n + 1)]
                fc.keyframe_points.clear(); fc.keyframe_points.add(n + 1)
                for f, kp in enumerate(fc.keyframe_points): kp.co = (f, vals[f]); kp.interpolation = 'LINEAR'
                fc.update()
    return {'drop': round(drop, 3), 'liftMax': round(max(lift), 3)}


def seat(arm, act, n, target, at, rest):
    """Sit a clip on the chair: slide it until its seated pose (at: 'first', 'last' or 'mean' frames) has the hip joints at `target` (world, the figure's root on the chair's
    point), then lift it if that puts the soles under the floor (longer legs than the chair is high: feet on the floor win, the seat is a little proud of the cushion).
    Returns the slide and how far the hips sit above the target."""
    sc = bpy.context.scene; assign(arm, act); M = arm.matrix_world; legs = [bone(arm, 'LeftUpperLeg'), bone(arm, 'RightUpperLeg')]
    frames = [n] if at == 'last' else [0] if at == 'first' else list(range(0, n + 1, max(1, n // 24)))
    def mean(fn):
        acc = None
        for f in frames:
            sc.frame_set(f); bpy.context.view_layer.update(); v = fn(); acc = v if acc is None else acc + v
        return acc / len(frames)
    delta = target - mean(lambda: (M @ legs[0].head + M @ legs[1].head) / 2); shift(arm, act, bone(arm, 'Hips').name, delta)
    low = mean(lambda: soles(arm, rest)); proud = 0.0
    if low < 0: proud = -low; shift(arm, act, bone(arm, 'Hips').name, Vector((0, 0, proud)))
    return {'slide': [round(x, 3) for x in delta], 'proud': round(proud, 3), 'solesUp': round(max(low, 0.0), 3)}


def own_speed(arm, act, n, fps):
    """Natural ground speed (m/s) of an in-place loco clip: the planted foot's median speed in mid-stance (within 2 cm of its lowest), as cell_generic.own_speed reads it.
    A run has too few such frames (a foot is down for three or four), so the band widens to 4 cm, then to each foot's lowest step."""
    sc = bpy.context.scene; assign(arm, act); M = arm.matrix_world
    P = {s: [] for s in ('Left', 'Right')}
    for i in range(n + 1):
        sc.frame_set(i); bpy.context.view_layer.update()
        for s in P: P[s].append(M @ bone(arm, s + 'Foot').head)
    ground = min(p.z for s in P for p in P[s]); step = lambda p, q: Vector((q.x - p.x, q.y - p.y, 0)).length * fps
    for band in (0.02, 0.04):
        sp = sorted(step(P[s][k - 1], P[s][k]) for s in P for k in range(1, len(P[s])) if P[s][k - 1].z <= ground + band and P[s][k].z <= ground + band)
        if len(sp) > 3: return round(sp[len(sp) // 2], 2)
    low = [min(range(1, len(P[s])), key=lambda k: P[s][k - 1].z + P[s][k].z) for s in P]
    return round(sum(step(P[s][k - 1], P[s][k]) for s, k in zip(P, low)) / 2, 2)


def lowest(arm, meshes, act):
    """The lowest point of the skinned meshes on the clip's first frame (m): the soles against y = 0, read off the mesh."""
    sc = bpy.context.scene; assign(arm, act); sc.frame_set(0); bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get(); low = 1e9
    for o in meshes:
        e = o.evaluated_get(dg); me = e.to_mesh()
        low = min(low, min((e.matrix_world @ v.co).z for v in me.vertices)); e.to_mesh_clear()
    return round(low, 3)


def _aim(pb, d_local):
    m3 = pb.matrix.to_3x3(); c = (m3 @ Vector((0, 1, 0))).normalized()
    n3 = c.rotation_difference(d_local.normalized()).to_matrix() @ m3
    pb.matrix = Matrix.Translation(pb.matrix.translation) @ n3.to_4x4()
    bpy.context.view_layer.update()


# (outward, up, forward) from the shoulder bone, read at an arm length (upper arm + forearm) of 0.424 m: author_clips.make_carry's hold, which Forma measured on the seated pair
HOLD = {'wrist': (0.205, -0.19, 0.16), 'ref': 0.424, 'pole': (0.55, -1.0, -0.35), 'hand': (-0.80, -0.25, 0.45)}


def carry(arm, src, name, n, side='Right'):
    """carry_shoulder / carry_shoulder_idle on the figure's own clip: the arm on the load's side reaches round the legs of the person draped over that shoulder (a two-bone
    solve to a point fixed in the carrier's frame, scaled by his arm), everything else as the clip has it. The recipe of author_clips.make_carry, without the finger curl
    (these rigs name their fingers differently). Returns the new action."""
    sc = bpy.context.scene; act = src.copy(); act.name = name; act.use_fake_user = True
    chain = [bone(arm, side + 'UpperArm'), bone(arm, side + 'LowerArm'), bone(arm, side + 'Hand')]; sh = bone(arm, side + 'Shoulder')
    paths = {f'pose.bones["{b.name}"].rotation_quaternion' for b in chain}
    for cb in channelbags(act):
        for fc in [fc for fc in cb.fcurves if fc.data_path in paths]: cb.fcurves.remove(fc)
    assign(arm, act); M = arm.matrix_world; Mi = M.to_3x3().inverted(); W = lambda pb: M @ pb.head
    up = Vector((0, 0, 1)); acc = Vector(); left, right = bone(arm, 'LeftUpperArm'), bone(arm, 'RightUpperArm')
    for f in range(n + 1):
        sc.frame_set(f); bpy.context.view_layer.update(); v = W(left) - W(right); v.z = 0; acc += v.normalized()
    lt = acc.normalized(); fw = lt.cross(up).normalized(); out = -lt if side == 'Right' else lt
    frame = lambda t: out * t[0] + up * t[1] + fw * t[2]
    l1 = (chain[1].bone.head_local - chain[0].bone.head_local).length; l2 = (chain[2].bone.head_local - chain[1].bone.head_local).length; k = (l1 + l2) / HOLD['ref']
    for b in chain: b.rotation_mode = 'QUATERNION'
    for f in range(n + 1):
        sc.frame_set(f); bpy.context.view_layer.update()
        S = W(chain[0]); d = W(sh) + frame(HOLD['wrist']) * k - S
        dist = min(max(d.length, abs(l1 - l2) + 1e-3), l1 + l2 - 1e-3); dn = d.normalized()
        x = (l1 * l1 - l2 * l2 + dist * dist) / (2 * dist); h = math.sqrt(max(l1 * l1 - x * x, 0.0))
        pole = frame(HOLD['pole']); perp = (pole - dn * pole.dot(dn)).normalized()
        E = S + dn * x + perp * h; Wr = S + dn * dist
        _aim(chain[0], Mi @ (E - S)); _aim(chain[1], Mi @ (Wr - E)); _aim(chain[2], Mi @ frame(HOLD['hand']))
        for b in chain: b.keyframe_insert('rotation_quaternion', frame=f)
    return act


def fbx_takes(path):
    """The animation stacks an FBX holds, by name (Blender writes '<object>|<action>')."""
    data = open(path, 'rb').read(); out = []; i = 0; tag = b'\x00\x01AnimStack'
    while True:
        i = data.find(tag, i)
        if i < 0: break
        j = i
        while j > 0 and 32 <= data[j - 1] < 127: j -= 1
        out.append(data[j:i].decode('ascii')); i += len(tag)
    return out


def pin_fbx():
    """The same bytes every build (as author_clips._pin_fbx, brief 069): the exporter's object ids hashed from their keys, the header's creation time fixed."""
    import hashlib, datetime
    from io_scene_fbx import export_fbx_bin as eb, fbx_utils
    fbx_utils._key_to_uuid = lambda uuids, key: int.from_bytes(hashlib.sha256(repr(key).encode()).digest()[:8], 'big') >> 1 or 1
    orig = getattr(eb, '_cp_orig_header', None) or eb.fbx_header_elements; eb._cp_orig_header = orig
    eb.fbx_header_elements = lambda root, scene_data, time=None: orig(root, scene_data, datetime.datetime(2026, 1, 1))
