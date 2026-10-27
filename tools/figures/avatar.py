"""Humanoid avatars written into the .meta by rule (brief 069, Latch's foot and bow-leg diagnosis, briefs/069/latch/leftfoot.md).

Unity builds a Humanoid avatar for every FBX it imports: it maps the bones, takes the file's pose, and moves it toward a T-pose
(AvatarSetupTool.SetupHumanSkeleton then MakePoseValid). That pose is the avatar's muscle zero. Two faults come out of it:
  - a Mixamo clip-only file's pose is its frame 0 (walk: left heel up 65 degrees), so every clip carries its own frame-0 offset into every body;
  - a figure's legs stay spread (5 to 6 degrees out), so every walk is played on top of that spread (the bow legs).
The fix, proven by Latch in Unity: the same avatar, but with the legs neutral (each leg straight down, each foot pointed along the hips' facing at a
flat-foot pitch, the toe level), written to the meta's humanDescription with auto-mapping off so it sticks. Clips all copy one such avatar (one zero
reference); every figure gets its own.

This module reproduces Unity's import-time avatar from the FBX itself, so the meta writer needs no Unity: read the file's node transforms the way
Unity imports them (Y-up right-handed file: X mirrored, file units to metres; a clip-only file at its first key), map the Mixamo names, run Unity's
MakePoseValid (ported from UnityCsReference Editor/Mono/Inspector/Avatar/AvatarSetupTool.cs, same table and order), then neutralLegs (Latch's
FootDiag.NeutralLegs). Checked against the avatars Unity itself wrote in Latch's proof (tools/figures/avatar_check.py).
Pure python + numpy; the FBX is read by Blender's own parser (io_scene_fbx.parse_fbx), so callers run inside Blender."""
import math, os, sys
import numpy as np

# ---------------------------------------------------------------- vector and quaternion maths, Unity's conventions (x, y, z, w)

def v(x, y, z): return np.array([x, y, z], dtype=np.float64)
UP, RIGHT, FWD, ZERO = v(0, 1, 0), v(1, 0, 0), v(0, 0, 1), v(0, 0, 0)
QI = np.array([0.0, 0.0, 0.0, 1.0])


def norm(a):
    n = np.linalg.norm(a)
    return a / n if n > 1e-5 else ZERO.copy()   # Vector3.normalized: zero below 1e-5


def qmul(a, b):
    ax, ay, az, aw = a; bx, by, bz, bw = b
    return np.array([aw * bx + ax * bw + ay * bz - az * by, aw * by - ax * bz + ay * bw + az * bx,
                     aw * bz + ax * by - ay * bx + az * bw, aw * bw - ax * bx - ay * by - az * bz])


def qinv(q): return np.array([-q[0], -q[1], -q[2], q[3]]) / np.dot(q, q)


def qrot(q, p):
    x, y, z, w = q; u = np.array([x, y, z])
    return 2.0 * np.dot(u, p) * u + (w * w - np.dot(u, u)) * p + 2.0 * w * np.cross(u, p)


def qaxis(axis, deg):
    h = math.radians(deg) / 2; s = math.sin(h); a = norm(axis)
    return np.array([a[0] * s, a[1] * s, a[2] * s, math.cos(h)])


def qnormal(q): return q / np.linalg.norm(q)


def angle(a, b):
    """Vector3.Angle, degrees."""
    d = math.sqrt(np.dot(a, a) * np.dot(b, b))
    if d < 1e-15: return 0.0
    return math.degrees(math.acos(max(-1.0, min(1.0, np.dot(a, b) / d))))


def from_to(a, b):
    """Quaternion.FromToRotation: the shortest arc from a to b."""
    a = norm(a); b = norm(b); d = float(np.dot(a, b))
    if d >= 1.0 - 1e-12: return QI.copy()
    if d <= -1.0 + 1e-12:
        ax = np.cross(RIGHT, a)
        if np.linalg.norm(ax) < 1e-6: ax = np.cross(UP, a)
        return qaxis(ax, 180.0)
    c = np.cross(a, b); q = np.array([c[0], c[1], c[2], 1.0 + d])
    return qnormal(q)


def slerp(a, b, t):
    d = float(np.dot(a, b))
    if d < 0: b = -b; d = -d
    if d > 0.9995: return qnormal(a + (b - a) * t)
    th = math.acos(d); s = math.sin(th)
    return (math.sin((1 - t) * th) * a + math.sin(t * th) * b) / s


def look_rotation(fwd, up):
    f = norm(fwd); r = norm(np.cross(up, f)); u = np.cross(f, r)
    m = np.array([r, u, f]).T   # columns: x, y, z
    return mat_to_q(m)


def mat_to_q(m):
    t = m[0, 0] + m[1, 1] + m[2, 2]
    if t > 0:
        s = math.sqrt(t + 1.0) * 2; return qnormal(np.array([(m[2, 1] - m[1, 2]) / s, (m[0, 2] - m[2, 0]) / s, (m[1, 0] - m[0, 1]) / s, 0.25 * s]))
    if m[0, 0] > m[1, 1] and m[0, 0] > m[2, 2]:
        s = math.sqrt(1.0 + m[0, 0] - m[1, 1] - m[2, 2]) * 2; return qnormal(np.array([0.25 * s, (m[0, 1] + m[1, 0]) / s, (m[0, 2] + m[2, 0]) / s, (m[2, 1] - m[1, 2]) / s]))
    if m[1, 1] > m[2, 2]:
        s = math.sqrt(1.0 + m[1, 1] - m[0, 0] - m[2, 2]) * 2; return qnormal(np.array([(m[0, 1] + m[1, 0]) / s, 0.25 * s, (m[1, 2] + m[2, 1]) / s, (m[0, 2] - m[2, 0]) / s]))
    s = math.sqrt(1.0 + m[2, 2] - m[0, 0] - m[1, 1]) * 2; return qnormal(np.array([(m[0, 2] + m[2, 0]) / s, (m[1, 2] + m[2, 1]) / s, 0.25 * s, (m[1, 0] - m[0, 1]) / s]))


def project_on_plane(a, n):
    s = np.dot(n, n)
    return a.copy() if s < 1e-15 else a - n * np.dot(a, n) / s


# ---------------------------------------------------------------- the imported hierarchy (what Unity makes of the FBX)

class Node:
    def __init__(self, name, parent):
        self.name, self.parent, self.children = name, parent, []
        self.lpos, self.lrot, self.lscl = ZERO.copy(), QI.copy(), v(1, 1, 1)

    @property
    def rotation(self):
        return self.lrot if self.parent is None else qmul(self.parent.rotation, self.lrot)

    @rotation.setter
    def rotation(self, q):
        self.lrot = qnormal(q if self.parent is None else qmul(qinv(self.parent.rotation), q))

    @property
    def position(self):
        if self.parent is None: return self.lpos.copy()
        return self.parent.position + qrot(self.parent.rotation, self.parent.lossy * self.lpos)

    @position.setter
    def position(self, p):
        self.lpos = p.copy() if self.parent is None else qrot(qinv(self.parent.rotation), p - self.parent.position) / self.parent.lossy

    @property
    def lossy(self):
        return self.lscl if self.parent is None else self.parent.lossy * self.lscl   # uniform scales only (all ours are 1 or near)


def _fbx_parser():
    try:
        from io_scene_fbx import parse_fbx
    except ImportError:
        sys.path.insert(0, os.environ.get('BLENDER_ADDONS', '/Applications/Blender.app/Contents/Resources/5.2/scripts/addons_core'))
        from io_scene_fbx import parse_fbx
    return parse_fbx


def _euler_q(deg, order=0):
    """FBX euler (degrees) to a right-handed quaternion. Order 0 = eXYZ: X applied first (R = Rz Ry Rx)."""
    qx, qy, qz = qaxis(RIGHT, deg[0]), qaxis(UP, deg[1]), qaxis(FWD, deg[2])
    seq = {0: (qx, qy, qz), 1: (qx, qz, qy), 2: (qy, qz, qx), 3: (qy, qx, qz), 4: (qz, qx, qy), 5: (qz, qy, qx)}[order]
    q = QI.copy()
    for r in seq: q = qmul(r, q)
    return q


def read_fbx(path, root_name, stack=None):
    """The FBX as Unity imports it: one root GameObject (root_name) and every Model node under it, in file order, each with its local pose.
    A node's pose is its first animation key where the file animates it (a clip-only file: Unity's pose for it is frame 0, Latch's `walkAvatar`),
    else its Lcl values. Right-handed Y-up file to Unity: x mirrored (position -x; rotation y and z negated); positions in metres."""
    pf = _fbx_parser(); top, _ = pf.parse(path)
    el = lambda e, i: [c for c in e.elems if c.id == i]
    gs = {p.props[0]: p.props[4:] for p in el(el(top, b'GlobalSettings')[0], b'Properties70')[0].elems}
    axes = tuple(gs[k][0] for k in (b'UpAxis', b'UpAxisSign', b'FrontAxis', b'FrontAxisSign', b'CoordAxis', b'CoordAxisSign'))
    if axes != (1, 1, 2, 1, 0, 1): raise ValueError(f'{path}: axis system {axes}, only Y-up right-handed (1,1,2,1,0,1) is handled')
    unit = gs[b'UnitScaleFactor'][0] * 0.01
    objs = el(top, b'Objects')[0]; models, curves, cnodes = {}, {}, {}
    for o in objs.elems:
        if o.id == b'Model':
            props = {p.props[0]: p.props[4:] for p in (el(o, b'Properties70')[0].elems if el(o, b'Properties70') else [])}
            models[o.props[0]] = (o.props[1].split(b'\x00')[0].decode(), props)
        elif o.id == b'AnimationCurve':
            t = el(o, b'KeyTime'); vv = el(o, b'KeyValueFloat')
            if t and vv: curves[o.props[0]] = (list(t[0].props[0]), list(vv[0].props[0]))
        elif o.id == b'AnimationCurveNode':
            cnodes[o.props[0]] = {}
    parent, order, anim = {}, [], {}
    cn_target = {}; keep_cn = None
    if stack is not None:
        # several takes in one file (ActorCore: '0_T-Pose' and the clip): only the curve nodes of the named stack's layers
        stacks = {o.props[0]: o.props[1].split(b'\x00')[0].decode() for o in objs.elems if o.id == b'AnimationStack'}
        layers = {o.props[0] for o in objs.elems if o.id == b'AnimationLayer'}; lay_of = {}; keep_l = set()
        for c in el(top, b'Connections')[0].elems:
            if c.props[0] == b'OO' and c.props[1] in layers and stacks.get(c.props[2]) == stack: keep_l.add(c.props[1])
        keep_cn = set()
        for c in el(top, b'Connections')[0].elems:
            if c.props[0] == b'OO' and c.props[1] in cnodes and c.props[2] in keep_l: keep_cn.add(c.props[1])
    for c in el(top, b'Connections')[0].elems:
        kind, child, par = c.props[0], c.props[1], c.props[2]
        if kind == b'OO' and child in models and (par == 0 or par in models) and child not in parent: parent[child] = par; order.append(child)
        elif kind == b'OP' and child in curves and par in cnodes: cnodes[par][c.props[3]] = child
        elif kind == b'OP' and child in cnodes and par in models: cn_target[child] = (par, c.props[3])
    for cn, (m, prop) in cn_target.items():
        if keep_cn is not None and cn not in keep_cn: continue
        vals = []
        for ax in (b'd|X', b'd|Y', b'd|Z'):
            cid = cnodes[cn].get(ax)
            vals.append(None if cid is None else curves[cid][1][int(np.argmin(curves[cid][0]))])   # the first key
        anim.setdefault(m, {})[prop] = vals
    root = Node(root_name, None); nodes = {0: root}; out = [root]
    pending = [m for m in order]
    while pending:   # parents first, file order kept
        rest = []
        for m in pending:
            if parent[m] in nodes:
                name, props = models[m]; n = Node(name, nodes[parent[m]]); nodes[parent[m]].children.append(n); nodes[m] = n; out.append(n)
                def get(k, dflt):
                    val = list(props.get(k, dflt)); a = anim.get(m, {}).get(k)
                    if a: val = [a[i] if a[i] is not None else val[i] for i in range(3)]
                    return val
                t = get(b'Lcl Translation', [0, 0, 0]); r = get(b'Lcl Rotation', [0, 0, 0]); s = get(b'Lcl Scaling', [1, 1, 1])
                ro = int(props.get(b'RotationOrder', [0])[0])
                pre = _euler_q(props.get(b'PreRotation', [0, 0, 0]), 0); post = _euler_q(props.get(b'PostRotation', [0, 0, 0]), 0)
                q = qmul(qmul(pre, _euler_q(r, ro)), qinv(post))
                n.lpos = v(-t[0], t[1], t[2]) * unit; n.lrot = qnormal(np.array([q[0], -q[1], -q[2], q[3]])); n.lscl = v(*s)
            else: rest.append(m)
        if len(rest) == len(pending): raise ValueError(f'{path}: orphan nodes {[models[m][0] for m in rest]}')
        pending = rest
    return out


# ---------------------------------------------------------------- Unity's human map and T-pose (AvatarSetupTool), ported

# HumanBodyBones order; the human names Unity writes; the Mixamo bone each maps from (Unity's auto-mapper's choice on every rig we have)
HUMAN = ['Hips', 'LeftUpperLeg', 'RightUpperLeg', 'LeftLowerLeg', 'RightLowerLeg', 'LeftFoot', 'RightFoot', 'Spine', 'Chest', 'Neck', 'Head',
         'LeftShoulder', 'RightShoulder', 'LeftUpperArm', 'RightUpperArm', 'LeftLowerArm', 'RightLowerArm', 'LeftHand', 'RightHand', 'LeftToes',
         'RightToes', 'LeftEye', 'RightEye', 'Jaw'] + [f'{s} {f} {p}' for s in ('Left', 'Right') for f in ('Thumb', 'Index', 'Middle', 'Ring', 'Little')
                                                      for p in ('Proximal', 'Intermediate', 'Distal')] + ['UpperChest']
MIXAMO = ['Hips', 'LeftUpLeg', 'RightUpLeg', 'LeftLeg', 'RightLeg', 'LeftFoot', 'RightFoot', 'Spine', 'Spine1', 'Neck', 'Head', 'LeftShoulder',
          'RightShoulder', 'LeftArm', 'RightArm', 'LeftForeArm', 'RightForeArm', 'LeftHand', 'RightHand', 'LeftToeBase', 'RightToeBase', None, None, None] + \
         [f'{s}Hand{f}{i}' for s in ('Left', 'Right') for f in ('Thumb', 'Index', 'Middle', 'Ring', 'Pinky') for i in (1, 2, 3)] + ['Spine2']
H = {n: i for i, n in enumerate(HUMAN)}


def human_parent(i):
    """HumanTrait.GetParentBone."""
    n = HUMAN[i]
    fixed = {'Hips': -1, 'LeftUpperLeg': 'Hips', 'RightUpperLeg': 'Hips', 'LeftLowerLeg': 'LeftUpperLeg', 'RightLowerLeg': 'RightUpperLeg',
             'LeftFoot': 'LeftLowerLeg', 'RightFoot': 'RightLowerLeg', 'Spine': 'Hips', 'Chest': 'Spine', 'UpperChest': 'Chest', 'Neck': 'UpperChest',
             'Head': 'Neck', 'LeftShoulder': 'UpperChest', 'RightShoulder': 'UpperChest', 'LeftUpperArm': 'LeftShoulder', 'RightUpperArm': 'RightShoulder',
             'LeftLowerArm': 'LeftUpperArm', 'RightLowerArm': 'RightUpperArm', 'LeftHand': 'LeftLowerArm', 'RightHand': 'RightLowerArm',
             'LeftToes': 'LeftFoot', 'RightToes': 'RightFoot', 'LeftEye': 'Head', 'RightEye': 'Head', 'Jaw': 'Head'}
    if n in fixed: return -1 if fixed[n] == -1 else H[fixed[n]]
    s, f, p = n.split(' ')
    return H[f'{s}Hand'] if p == 'Proximal' else H[f'{s} {f} {"Proximal" if p == "Intermediate" else "Intermediate"}']


class Pose:
    def __init__(self, d, glob, mx, plane=None, kids=None):
        self.dir = norm(d) if np.any(d) else ZERO.copy(); self.glob = glob; self.max = float(mx); self.plane = plane if plane is not None else ZERO.copy(); self.kids = kids


_L, _R = -RIGHT, RIGHT
POSES = [Pose(UP, True, 15), Pose(v(-0.05, -1, 0), True, 15), Pose(v(0.05, -1, 0), True, 15), Pose(v(-0.05, -1, -0.15), True, 20),
         Pose(v(0.05, -1, -0.15), True, 20), Pose(v(-0.05, 0, 1), True, 20, UP), Pose(v(0.05, 0, 1), True, 20, UP),
         Pose(UP, True, 30, None, [H['Chest'], H['UpperChest'], H['Neck'], H['Head']]), Pose(UP, True, 30, None, [H['UpperChest'], H['Neck'], H['Head']]),
         Pose(UP, True, 30), None, Pose(_L, True, 20), Pose(_R, True, 20), Pose(_L, True, 5), Pose(_R, True, 5), Pose(_L, True, 5), Pose(_R, True, 5),
         Pose(_L, False, 10, FWD, [H['Left Middle Proximal']]), Pose(_R, False, 10, FWD, [H['Right Middle Proximal']]), None, None, None, None, None] + \
        [Pose(v(-1, 0, 1), False, m) for m in (10, 5, 5)] + [Pose(_L, False, m) for _ in range(4) for m in (10, 5, 5)] + \
        [Pose(v(1, 0, 1), False, m) for m in (10, 5, 5)] + [Pose(_R, False, m) for _ in range(4) for m in (10, 5, 5)] + \
        [Pose(UP, True, 30, None, [H['Neck'], H['Head']])]
assert len(POSES) == 55 and len(HUMAN) == 55 and len(MIXAMO) == 55


def map_bones(nodes, bonemap=None):
    """The human map: Unity's auto-mapper on a Mixamo rig ('mixamorig:' prefix or none), or a bone map in data ({human name: bone name},
    tools/figures/bonemaps.json: what Unity's auto-mapper chose on that rig). Returns {human index: node}."""
    if bonemap:
        byf = {n.name: n for n in nodes[1:]}
        return {i: byf[bonemap[h]] for i, h in enumerate(HUMAN) if h in bonemap and bonemap[h] in byf}
    by = {}
    for n in nodes[1:]:
        by.setdefault(n.name.split(':')[-1] if n.name.startswith('mixamorig:') else n.name, n)
    return {i: by[m] for i, m in enumerate(MIXAMO) if m and m in by}


def _orientation(b):
    if not all(k in b for k in (H['LeftUpperLeg'], H['RightUpperLeg'], H['LeftUpperArm'], H['RightUpperArm'])): return QI.copy()
    lul, rul, la, ra = (b[H[k]].position for k in ('LeftUpperLeg', 'RightUpperLeg', 'LeftUpperArm', 'RightUpperArm'))
    legs = norm(rul - lul); arms = norm(ra - la); right = norm(legs + arms)
    sensible = abs(right[0] * right[1]) < 0.05 and abs(right[1] * right[2]) < 0.05 and abs(right[2] * right[0]) < 0.05
    up = norm((la + ra) * 0.5 - (lul + rul) * 0.5)
    if sensible:
        ax = int(np.argmax(np.abs(up))); s = math.copysign(1.0, up[ax]); up = ZERO.copy(); up[ax] = s
    fwd = np.cross(right, up)
    if not np.any(fwd) or not np.any(up): return QI.copy()
    return look_rotation(fwd, up)


def _child(b, i):
    for k in range(55):
        if human_parent(k) == i: return k
    return -1


def _dir(b, q, i):
    if POSES[i] is None: return ZERO.copy()
    p = POSES[i]; ci = -1
    if p.kids is not None:
        for k in p.kids:
            if k in b: ci = k; break
    else: ci = _child(b, i)
    if ci >= 0 and ci in b: d = b[ci].position - b[i].position
    else:
        if len(b[i].children) != 1: return ZERO.copy()
        d = b[i].children[0].position - b[i].position
    return norm(d)


def _space(b, q, i):
    pd = QI.copy(); p = POSES[i]
    if not p.glob:
        pi = human_parent(i)
        if pi > 0 and pi in b and POSES[pi] is not None:
            d = _dir(b, q, pi)
            if np.any(d): pd = from_to(qrot(q, POSES[pi].dir), d)
    return qmul(pd, q)


def _align(b, q, i):
    p = POSES[i]
    if i not in b or p is None: return
    if i == H['Hips']:
        ax, ay, az = angle(qrot(q, RIGHT), RIGHT), angle(qrot(q, UP), UP), angle(qrot(q, FWD), FWD)
        if ax > p.max or ay > p.max or az > p.max: b[i].rotation = qmul(qinv(q), b[i].rotation)
        return
    d = _dir(b, q, i)
    if not np.any(d): return
    sp = _space(b, q, i); goal = qrot(sp, p.dir)
    if np.any(p.plane): d = project_on_plane(d, qrot(sp, p.plane))
    da = angle(d, goal)
    if da > p.max * 0.99:
        adj = from_to(d, goal); foot = None
        if i in (H['LeftUpperLeg'], H['LeftLowerLeg']): foot = b.get(H['LeftFoot'])
        if i in (H['RightUpperLeg'], H['RightLowerLeg']): foot = b.get(H['RightFoot'])
        fr = foot.rotation if foot is not None else None
        adj = slerp(QI.copy(), adj, min(1.0, max(0.0, 1.05 - p.max / da)))
        b[i].rotation = qmul(adj, b[i].rotation)
        if foot is not None: foot.rotation = fr


def _position(b):
    if H['LeftUpperLeg'] not in b or H['RightUpperLeg'] not in b: return
    hip = (b[H['LeftUpperLeg']].position + b[H['RightUpperLeg']].position) * 0.5
    toes = H['LeftToes'] in b and H['RightToes'] in b
    lf, rf = (b[H['LeftToes']], b[H['RightToes']]) if toes else (b.get(H['LeftFoot']), b.get(H['RightFoot']))
    if lf is None or rf is None: return
    foot = (lf.position + rf.position) * 0.5; hh = hip[1] - foot[1]
    if hh <= 0: return
    adj = ZERO.copy()
    if foot[1] < 0 or foot[1] > hh * (0.1 if toes else 0.3): adj[1] = -(hip[1] - hh * (1.03 if toes else 1.13))
    if abs(hip[0]) > 0.01 * hh: adj[0] = -hip[0]
    if abs(hip[2]) > 0.2 * hh: adj[2] = -hip[2]
    if np.any(adj): b[H['Hips']].position = b[H['Hips']].position + adj


def make_pose_valid(b):
    """AvatarSetupTool.MakePoseValid: each human bone, in HumanBodyBones order, turned only as far as its tolerance allows; then the feet to the ground."""
    q = _orientation(b)
    for i in range(55):
        _align(b, q, i)
        if i == H['Hips']: q = _orientation(b)
    _position(b)


# ---------------------------------------------------------------- Latch's neutral legs (FootDiag.NeutralLegs)

LEGS = ('UpLeg', 'Leg', 'Foot', 'ToeBase', 'Toe_End')


def neutral_legs(nodes, pitch_deg=None, b=None):
    """Each leg straight down; Foot->ToeBase along the hips' facing, pitch_deg down (None: the mean of this rest's own two pitches, a figure's
    flat stance); ToeBase->Toe_End level along the facing. Only the leg bones change. b: the human map (any rig); None: Mixamo names."""
    by = {(n.name.split(':')[-1]): n for n in nodes[1:]}
    if b is not None:
        for s_ in ('Left', 'Right'):
            for k, h in (('UpLeg', 'UpperLeg'), ('Leg', 'LowerLeg'), ('Foot', 'Foot'), ('ToeBase', 'Toes')):
                if H[s_ + h] in b: by[s_ + k] = b[H[s_ + h]]
            t = by.get(s_ + 'ToeBase')
            if t is not None and t.children: by[s_ + 'Toe_End'] = t.children[0]
            elif s_ + 'Toe_End' in by: del by[s_ + 'Toe_End']
    F = lambda k: by[k]
    lr = F('RightUpLeg').position - F('LeftUpLeg').position; lr[1] = 0; fwd = norm(np.cross(lr, UP))
    def aim(bn, ch, d): bn.rotation = qmul(from_to(ch.position - bn.position, d), bn.rotation)
    if pitch_deg is None:
        ps = []
        for s in ('Left', 'Right'):
            d = F(s + 'ToeBase').position - F(s + 'Foot').position; dy = -d[1]; d[1] = 0; ps.append(math.degrees(math.atan2(dy, np.linalg.norm(d))))
        pitch_deg = float(np.mean(ps))
    pr = math.radians(pitch_deg)
    for s in ('Left', 'Right'):
        aim(F(s + 'UpLeg'), F(s + 'Leg'), -UP); aim(F(s + 'Leg'), F(s + 'Foot'), -UP)
        aim(F(s + 'Foot'), F(s + 'ToeBase'), norm(fwd * math.cos(pr) - UP * math.sin(pr)))
        if (s + 'Toe_End') in by: aim(F(s + 'ToeBase'), F(s + 'Toe_End'), fwd)
    return pitch_deg


# ---------------------------------------------------------------- the whole rule, and the meta text

def neutral_avatar(fbx_path, root_name, pitch_deg=None, stack=None, bonemap=None):
    """Unity's own avatar for this file, with neutral legs. Returns (human rows [(bone, human)], skeleton nodes, pitch used).
    stack: the take whose first key is the rest (ActorCore's '0_T-Pose'); bonemap: the human map in data for a non-Mixamo rig."""
    nodes = read_fbx(fbx_path, root_name, stack)
    b = map_bones(nodes, bonemap)
    for k in ('Hips', 'LeftUpperLeg', 'RightUpperLeg', 'LeftLowerLeg', 'RightLowerLeg', 'LeftFoot', 'RightFoot', 'LeftToes', 'RightToes'):
        if H[k] not in b: raise ValueError(f'{fbx_path}: no bone for {k}')
    make_pose_valid(b)
    p = neutral_legs(nodes, pitch_deg, b if bonemap else None)
    return [(b[i].name, HUMAN[i]) for i in sorted(b)], nodes, p


def _f(x):
    x = float(x)
    if x == 0: return '0'
    return f'{x:.9g}'


def human_description(human, nodes, limits=None):
    """The humanDescription's human and skeleton arrays, in the layout Unity writes them.
    limits: {humanName: {'min': [x, y, z], 'max': [x, y, z]}} (degrees per muscle axis: x twist, y spread/in-out, z front-back/stretched; Unity's
    HumanLimit with useDefaultValues off = modified 1, its length the bone's own length to its first child in this skeleton)."""
    by = {n.name: n for n in nodes}
    def length(bn):
        kids = [c for c in by[bn].children] if bn in by else []
        return float(np.linalg.norm(kids[0].lpos * kids[0].parent.lossy)) if kids else 0.0
    def row(bn, hn):
        L_ = (limits or {}).get(hn)
        if not L_: return (f"    - boneName: {bn}\n      humanName: {hn}\n      limit:\n        min: {{x: 0, y: 0, z: 0}}\n        max: {{x: 0, y: 0, z: 0}}\n"
                           f"        value: {{x: 0, y: 0, z: 0}}\n        length: 0\n        modified: 0\n")
        mn, mx = L_['min'], L_['max']
        return (f"    - boneName: {bn}\n      humanName: {hn}\n      limit:\n        min: {{x: {_f(mn[0])}, y: {_f(mn[1])}, z: {_f(mn[2])}}}\n"
                f"        max: {{x: {_f(mx[0])}, y: {_f(mx[1])}, z: {_f(mx[2])}}}\n        value: {{x: 0, y: 0, z: 0}}\n        length: {_f(length(bn))}\n        modified: 1\n")
    h = ''.join(row(bn, hn) for bn, hn in human)
    s = ''
    for n in nodes:
        p, q, c = n.lpos, n.lrot, n.lscl
        s += (f"    - name: {n.name}\n      parentName: {'' if n.parent is None else n.parent.name}\n"
              f"      position: {{x: {_f(p[0])}, y: {_f(p[1])}, z: {_f(p[2])}}}\n"
              f"      rotation: {{x: {_f(q[0])}, y: {_f(q[1])}, z: {_f(q[2])}, w: {_f(q[3])}}}\n"
              f"      scale: {{x: {_f(c[0])}, y: {_f(c[1])}, z: {_f(c[2])}}}\n")
    return h, s


def rig_differences(ref_nodes, fbx_path, tol_m=0.01):
    """Where a clip's rig differs from the reference avatar it would copy (brief 069): a human bone missing, under a different parent, or a bone
    more than tol_m longer or shorter than the reference's. Empty list = the same rig, so Copy From Other Avatar fits it."""
    rb = map_bones(ref_nodes); cb = map_bones(read_fbx(fbx_path, 'clip'))
    out = []
    for i, rn in sorted(rb.items()):
        cn = cb.get(i)
        if cn is None: out.append(f'{HUMAN[i]}: missing'); continue
        rp = '' if rn.parent.parent is None else rn.parent.name; cp = '' if cn.parent.parent is None else cn.parent.name
        if rp != cp: out.append(f'{HUMAN[i]}: parent {cp or "(root)"} not {rp or "(root)"}')
        if i != H['Hips']:   # the bone's offset from its parent, in metres in the world (a Blender export puts cm under an Armature scaled 0.01)
            d = abs(float(np.linalg.norm(cn.position - cn.parent.position)) - float(np.linalg.norm(rn.position - rn.parent.position)))
            if d > tol_m: out.append(f'{HUMAN[i]}: {d * 100:.1f} cm longer or shorter than the reference')
    return out


def lone_root_above_hips(fbx_path, hips='Hips'):
    """The file's only top-level node, when it is not the hips but holds them (a Blender export's `Armature`, ActorCore's `root`); else None. Unity
    folds a lone root into the prefab root unless Preserve Hierarchy is on, so an avatar written with that node in it fails to build (brief 069 A).
    hips: the hips bone's name (a bone map's 'Hips'; Mixamo's by default)."""
    nodes = read_fbx(fbx_path, 'root'); tops = nodes[0].children
    is_hips = lambda n: n.name.split(':')[-1] == hips
    if len(tops) != 1 or is_hips(tops[0]): return None
    return tops[0].name if any(is_hips(n) for n in nodes if n is not tops[0]) else None


def stacks(fbx_path):
    """The file's takes (FBX AnimationStacks), in file order."""
    fbx_bin = _fbx_parser(); top, _ = fbx_bin.parse(fbx_path)
    objs = [e for e in top.elems if e.id == b'Objects'][0]
    return [o.props[1].split(b'\x00')[0].decode() for o in objs.elems if o.id == b'AnimationStack']


def rig_of(fbx_path, rigs):
    """Which rig in data (bonemaps.json `rigs`) this file uses: the first whose `match` bone it has; None = Mixamo (the default rules)."""
    names = {n.name for n in read_fbx(fbx_path, 'root')}
    return next((k for k, r in rigs.items() if r['match'] in names), None)


def reference_rest(ref_nodes, fbx_path, root_name):
    """The reference avatar's rest written into another file's own hierarchy (brief 069 A: the clips authored in Blender sit under an `Armature`
    root, so they can't copy the reference avatar, and their own frame 0, a seated or crossed-leg pose, made a bad zero: a leg aimed straight down
    from a crossed thigh keeps the cross's twist, and sit_talk played with its legs swapped). Their bones are the reference's bones (the same
    Mixamo frames: every child offset points the same way, 0.0 degrees), so each bone takes the reference's world rotation by name, the Hips its
    world position too; the file's own nodes above the Hips (Armature) and every local offset stay the file's. Returns (human rows, nodes)."""
    nodes = read_fbx(fbx_path, root_name); ref = {n.name: n for n in ref_nodes}
    for n in nodes[1:]:   # read_fbx lists parents first
        r = ref.get(n.name)
        if r is None: continue
        n.rotation = r.rotation
        if n.name.split(':')[-1] == 'Hips': n.position = r.position
    b = map_bones(nodes)
    return [(b[i].name, HUMAN[i]) for i in sorted(b)], nodes


def finger_limits(rule, human):
    """fit.json transfer.fingers.humanLimits expanded to every mapped right and left finger bone: {humanName: {'min', 'max'}}"""
    out = {}
    for _, hn in human:
        parts = hn.split(' ')
        if len(parts) != 3 or parts[1] not in ('Thumb', 'Index', 'Middle', 'Ring', 'Little'): continue
        key = ('Thumb' if parts[1] == 'Thumb' else 'Finger') + ' ' + parts[2]
        if key in rule: out[hn] = rule[key]
    return out
