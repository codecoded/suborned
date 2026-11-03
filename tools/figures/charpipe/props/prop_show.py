"""Brief 069 props: the seat check (fails loudly) and the pictures for a held prop on the new pipeline's figures.
  out/scratch/forma/char-pipeline/blender.sh -P tools/figures/charpipe/props/prop_show.py -- <spec.json>
spec: animate.py's ({"out", "name", "figures": [{"blend", "label", "id", "x"}]}) plus "prop": id. The figures and clips go through animate.py's own
machinery (its Humanoid-like retarget, the same zero), so what is checked here is what the clip pictures show.
Writes <out>/: raw_vs_clean_<view>.png, <figure>_<clip>_close.png and _play.png (hand: aim_pistol, idle; holster: walk), hand_scale_<figure>.png,
seat.json. Exit 1 when the seat check fails (props.json seat rule)."""
import os, sys, json, math, importlib.util
import numpy as np
import bpy
from mathutils import Vector, Matrix, Quaternion
HERE = os.path.dirname(os.path.abspath(__file__)); CP = os.path.dirname(HERE); sys.path.append(os.path.dirname(CP)); ROOT = os.path.dirname(os.path.dirname(os.path.dirname(CP)))
AN = importlib.util.module_from_spec(s := importlib.util.spec_from_file_location('animate', os.path.join(CP, 'fit', 'animate.py'))); s.loader.exec_module(AN)
L = AN.L; look = AN.look
D = json.load(open(os.path.join(HERE, 'props.json'))); SPEC = AN.SPEC; PID = SPEC['prop']; P = D['props'][PID]
PA = os.path.join(ROOT, 'out/scratch/forma/char-pipeline/props', PID, 'a'); OUT = L.ensure(SPEC['out'])
GLB = os.path.join(ROOT, 'paid', P['mesh']['job'], 'meshes', f'{PID}_t1.glb')
# Unity bone frame -> Blender bone frame: Unity +Y, +Z are the bone's own Y, Z; Unity +X is the bone's -X (the X mirror of the FBX import).
# The prop's Blender axes: barrel -Y, up +Z, its right side (Unity +X) Blender -X. Unity's identity localRotation = barrel on the bone's Z,
# up on its Y, right side on its Unity X (Blender -X): prop -> bone is X->X, -Y->Z, Z->Y, a -90 degree turn about X.
M_ROT = Matrix(((1, 0, 0, 0), (0, 0, 1, 0), (0, -1, 0, 0), (0, 0, 0, 1)))


def prop_material(o):
    m = bpy.data.materials.new(f'{PID}_view'); m.use_nodes = True; nt = m.node_tree; b = nt.nodes['Principled BSDF']
    def img(name, cs):
        t = nt.nodes.new('ShaderNodeTexImage'); t.image = bpy.data.images.load(os.path.join(PA, name)); t.image.colorspace_settings.name = cs; return t
    a = img(f'{PID}_albedo.png', 'sRGB'); nt.links.new(a.outputs['Color'], b.inputs['Base Color'])
    if os.path.exists(os.path.join(PA, f'{PID}_metal.png')):
        mt = img(f'{PID}_metal.png', 'Non-Color'); sep = nt.nodes.new('ShaderNodeSeparateColor'); nt.links.new(mt.outputs['Color'], sep.inputs[0])
        nt.links.new(sep.outputs[0], b.inputs['Metallic']); inv = nt.nodes.new('ShaderNodeMath'); inv.operation = 'SUBTRACT'; inv.inputs[0].default_value = 1
        nt.links.new(mt.outputs['Alpha'], inv.inputs[1]); nt.links.new(inv.outputs[0], b.inputs['Roughness'])
    if os.path.exists(os.path.join(PA, f'{PID}_normal.png')):
        nm = img(f'{PID}_normal.png', 'Non-Color'); nn = nt.nodes.new('ShaderNodeNormalMap'); nt.links.new(nm.outputs['Color'], nn.inputs['Color']); nt.links.new(nn.outputs[0], b.inputs['Normal'])
    o.data.materials.clear(); o.data.materials.append(m)


def load_prop(name):
    before = set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=os.path.join(PA, f'{PID}.fbx'))
    o = next(x for x in bpy.data.objects if x not in before and x.type == 'MESH'); o.name = name
    o.data.transform(o.matrix_world); o.matrix_world = Matrix.Identity(4); o.parent = None
    prop_material(o); return o


def mount(o, fig, bone, off, turn=None):
    """off: the Unity localPosition under the bone; turn: a Blender bone-frame rotation (Matrix 4x4) applied before the axis map (None = identity)"""
    B = fig.arm.matrix_world @ fig.arm.pose.bones[bone].matrix
    o.matrix_world = B @ Matrix.Translation((-off[0], off[1], off[2])) @ (turn or Matrix.Identity(4)) @ M_ROT


def unity_quat(R):
    """a rotation in the Blender bone frame as the Unity localRotation (x, y, z, w): the frame's X is mirrored on import"""
    q = R.to_quaternion(); return [round(q.x, 6), round(-q.y, 6), round(-q.z, 6), round(q.w, 6)]


def unity_euler(qu):
    """Unity's Quaternion.eulerAngles (Z, then X, then Y), degrees 0..360"""
    x, y, z, w = qu
    sx = 2 * (w * x - y * z); sx = max(-1.0, min(1.0, sx)); ex = math.asin(sx)
    ey = math.atan2(2 * (w * y + x * z), 1 - 2 * (x * x + y * y)); ez = math.atan2(2 * (w * z + x * y), 1 - 2 * (x * x + z * z))
    return [round(math.degrees(a) % 360, 3) for a in (ex, ey, ez)]


def tris_world(o):
    V = AN.co(o); return V, L.tris_of(o)


_DOM = {}


def body_parts(fig):
    """LOD0 body meshes, posed: (V, T, the dominant bone of each triangle)."""
    out = []
    for r in ('body', 'garment', 'hat', 'hair'):
        for o in fig.objs(r):
            if o.name not in _DOM:
                gi = {g.index: g.name for g in o.vertex_groups}; dom = np.full(len(o.data.vertices), '', object)
                for v in o.data.vertices:
                    if v.groups: g = max(v.groups, key=lambda x: x.weight); dom[v.index] = gi.get(g.group, '')
                T = L.tris_of(o); _DOM[o.name] = (T, dom[T[:, 0]])
            T, td = _DOM[o.name]; out.append((AN.co(o), T, td))
    return out


def region(b):
    b = str(b)
    for k in ('RightHand', 'LeftHand', 'RightForeArm', 'LeftForeArm', 'RightArm', 'LeftArm', 'RightUpLeg', 'LeftUpLeg', 'RightLeg', 'LeftLeg'):
        if b.startswith(k): return k
    return 'torso/head' if b else 'none'


def crossings(fig, prop):
    """prop-body triangle crossings by body region"""
    Vp, Tp = tris_world(prop); by = {}
    for V, T, td in body_parts(fig):
        regs = np.array([region(b) for b in td])
        for r in sorted(set(regs)):
            n = AN.overlap_pairs(Vp, Tp, V, T[regs == r])
            if n: by[str(r)] = by.get(str(r), 0) + n
    return by


def has_fingers(fig): return 'RightHandIndex1' in fig.arm.pose.bones


def _inside_unused(Vw, bvh, tol):
    n = 0
    for v in Vw:
        loc, nr, _, _ = bvh.find_nearest(Vector(v))
        if loc is not None and (Vector(v) - loc).dot(nr) < -tol: n += 1
    return n


def solve_grip(fing, props, A, G, seat, TURN):
    """props.json grip.contact: the grip pose solved from the prop's own surface (no per-figure tuning). On each finger-boned figure at rest, the
    prop on its hand: (1) the grip is pushed out of the palm along the palm's normal, stepM at a time, until no palm triangle crosses the prop's
    surface; (2) each finger's bones close one after another, knuckle first, stepDeg at a time up to curlMaxDeg, and each stops at the step before
    a triangle of that bone or the ones beyond it would cross the prop's surface (moving in small steps from outside, a finger can't get inside
    without crossing it first; generated props have inner shells, so a point-inside test by nearest normal is not trusted). One pose for every
    figure: per joint the smallest angle any figure allowed, the seat the farthest out."""
    C = G['contact']; seats = []; per = []
    for f in fing:
        a = f.arm; Mw = a.matrix_world; pr = props[f.label]['hand']
        for pb in a.pose.bones: pb.matrix_basis = Matrix.Identity(4)
        bpy.context.view_layer.update()
        body = [o for o in f.objs('body')][0]; gi = {g.index: g.name for g in body.vertex_groups}
        dom = np.array([gi[max(v.groups, key=lambda x: x.weight).group] if v.groups else '' for v in body.data.vertices])
        Tb = L.tris_of(body); tdom = dom[Tb]
        Bw = Mw @ a.pose.bones[A['hand']['bone']].matrix; off = list(seat)
        h = Mw @ a.pose.bones['RightHand'].head; m1 = Mw @ a.pose.bones['RightHandMiddle1'].head
        # the palm proper: the hand's own triangles short of the knuckles (the fitted fingers' first joints); past them it is finger, whatever
        # its weight says (the fit blends the proximal phalanx with the hand)
        hax = np.array((m1 - h).normalized()); kn = min(float((np.array(Mw @ a.pose.bones[f'RightHand{x}1'].head) - np.array(h)) @ hax) for x in ('Index', 'Middle', 'Ring', 'Pinky'))
        Vb0 = AN.co(body); short = ((Vb0 - np.array(h)) @ hax) < kn
        t1 = np.array(Mw @ a.pose.bones['RightHandThumb1'].head); t3 = np.array(Mw @ a.pose.bones['RightHandThumb3'].tail); td_ = (t3 - t1) / np.linalg.norm(t3 - t1)
        rel = Vb0 - t1; thenar = np.linalg.norm(rel - np.outer(rel @ td_, td_), axis=1) < C['thenarM']           # near the thumb's line: the thenar, the thumb's to pose
        palm_t = Tb[(tdom == 'RightHand').all(1) & short[Tb].all(1) & ~thenar[Tb].any(1)]
        Tp = L.tris_of(pr); Vl = np.array([list(v.co) for v in pr.data.vertices])
        sb = float(json.load(open(os.path.join(PA, f'{PID}.json')))['grip']['slideBottomM'])
        slide_t = Tp[(Vl[Tp][:, :, 2] > sb).all(1)]; grip_t = Tp[(Vl[Tp][:, :, 2] <= sb).any(1)]   # the prop's slide (above its underside) and the rest
        hand_t = Tb[np.char.startswith(tdom.astype(str), 'RightHand').any(1)]
        def place():
            mount(pr, f, A['hand']['bone'], off, TURN); bpy.context.view_layer.update(); return AN.co(pr)
        def cross(T, P_=None): return AN.overlap_pairs(Vp, Tp if P_ is None else P_, AN.co(body), T)
        Vp = place(); c = Vector(pr.matrix_world.translation); q = h + (m1 - h) * max(0.0, min(1.0, (c - h).dot(m1 - h) / (m1 - h).length_squared))
        out_b = Bw.to_3x3().inverted() @ (c - q).normalized()                       # the palm's outward normal, in the bone frame
        # (0) the slide rests on the hand's top, not in it: the prop lifted along its own up (the bone's +Y, the thumb side) until no hand triangle
        # crosses the slide
        lifted = 0
        while cross(hand_t, slide_t) > 0 and lifted < C['maxPushSteps']:
            off = [off[0], off[1] + C['stepM'], off[2]]; Vp = place(); lifted += 1
        start = list(off); trace = {}
        for sg in (1, -1):                       # out of the palm: whichever way along the palm's normal clears it first
            off = list(start); Vp = place(); n = cross(palm_t, grip_t); k = 0; tr = [n]
            while n > 0 and k < C['maxPushSteps']:
                off = [off[0] - sg * out_b.x * C['stepM'], off[1] + sg * out_b.y * C['stepM'], off[2] + sg * out_b.z * C['stepM']]; Vp = place(); n = cross(palm_t, grip_t); k += 1; tr.append(n)
            trace[sg] = (n, k, list(off), tr)
        sg = min(trace, key=lambda x: (trace[x][0], trace[x][1])); n, pushed, off, _ = trace[sg]; Vp = place()
        if os.environ.get('PROP_DEBUG'): print('PUSH', f.label, 'lifted', lifted, {k: (v[0], v[1], v[3][:12]) for k, v in trace.items()})
        if os.environ.get('PROP_DEBUG'): print('PARTS', f.label, 'palm', cross(palm_t), 'hand all', cross(hand_t), 'fingers', cross(Tb[np.char.startswith(tdom.astype(str), 'RightHand').any(1) & ~((tdom == 'RightHand').all(1) & short[Tb].all(1))]))
        ang = {}; fx = {}
        for fn, mx in G['curlMaxDeg'].items():
            pbs = [a.pose.bones.get(f'RightHand{fn}{i}') for i in (1, 2, 3)]
            if any(pb is None for pb in pbs): ang[fn] = [0, 0, 0]; continue
            T = Tb[np.isin(tdom, [f'RightHand{fn}{k}' for k in (1, 2, 3)]).any(1)]
            if fn == 'Thumb':
                # the thumb lies in front of the palm in the generated rest: it is turned out of the grip's way first, at its base, about both of
                # its bone's cross axes (the smallest turn with the fewest crossings), then closed like a finger
                best = None
                for zx in [(x, z) for x in range(-C['thumbTurnDeg'], C['thumbTurnDeg'] + 1, 10) for z in range(-C['thumbTurnDeg'], C['thumbTurnDeg'] + 1, 10)]:
                    pbs[0].rotation_mode = 'QUATERNION'; pbs[0].rotation_quaternion = Quaternion((0, 0, 1), math.radians(zx[1])) @ Quaternion((1, 0, 0), math.radians(zx[0]))
                    bpy.context.view_layer.update(); k = (cross(T), abs(zx[0]) + abs(zx[1]), zx)
                    if best is None or k < best: best = k
                base_q = Quaternion((0, 0, 1), math.radians(best[2][1])) @ Quaternion((1, 0, 0), math.radians(best[2][0])); pbs[0].rotation_quaternion = base_q
                bpy.context.view_layer.update(); ang['ThumbBase'] = list(best[2])
            else: base_q = Quaternion()
            lo = 0.0
            rows = []
            for t in np.linspace(1.0, lo, C['scanSteps']):
                for k_, (pb, m) in enumerate(zip(pbs, mx)): pb.rotation_mode = 'QUATERNION'; pb.rotation_quaternion = (base_q if k_ == 0 else Quaternion()) @ Quaternion((1, 0, 0), -math.radians(float(t) * m))
                bpy.context.view_layer.update(); rows.append((cross(T), -float(t)))
            n, nt = min(rows); t = -nt                                       # the fewest crossings; of those, the most closed
            for k_, (pb, m) in enumerate(zip(pbs, mx)): pb.rotation_quaternion = (base_q if k_ == 0 else Quaternion()) @ Quaternion((1, 0, 0), -math.radians(t * m))
            bpy.context.view_layer.update(); ang[fn] = [round(t * m, 1) for m in mx]; fx[fn] = n
        if os.environ.get('PROP_DEBUG'): print('FINGERS', f.label, ang, fx)
        seats.append((off, pushed + lifted)); per.append(ang)
        for pb in a.pose.bones: pb.matrix_basis = Matrix.Identity(4)
        bpy.context.view_layer.update()
    pose = {fn: [min(p[fn][i] for p in per) for i in range(len(G['curlMaxDeg'][fn]))] for fn in G['curlMaxDeg']}
    if all('ThumbBase' in p for p in per): pose['ThumbBase'] = per[0]['ThumbBase']
    far = max(seats, key=lambda x: x[1])[0]
    return {'seat': [round(x, 4) for x in far], 'curlDeg': pose, 'palmPushSteps': [x[1] for x in seats], 'byFigure': {f.label: p for f, p in zip(fing, per)}}


def support_target(S):
    """props.json attach.support: the left palm's target in the prop's Unity frame, by rule from the prop's own grip geometry"""
    import avatar as av
    R = S['rule']; info = json.load(open(os.path.join(PA, f'{PID}.json')))
    me = load_prop('_support_probe'); Vb = np.array([list(v.co) for v in me.data.vertices]); bpy.data.objects.remove(me, do_unlink=True)
    Vu = np.stack([-Vb[:, 0], Vb[:, 2], -Vb[:, 1]], 1)                                  # Blender prop axes -> Unity (+X right, +Y up, +Z barrel)
    sb = info['grip']['slideBottomM']; gh = info['grip']['gripHeightM']; level = sb - 0.55 * gh   # about the middle finger's height on the grip
    sec = Vu[(np.abs(Vu[:, 1] - level) < 0.006) & (Vu[:, 1] < sb)]
    pos = [round(float(sec[:, 0].min() - R['fingerThicknessM']), 4), round(float(level), 4), round(float(sec[:, 2].max() - R['frontInsetM']), 4)]
    q = av.look_rotation(av.norm(np.array(R['longAxis'], float)), np.array(R['thumb'], float))
    return {'bone': S['bone'], 'inPropFrame': {'position': pos, 'rotation': [round(float(x), 6) for x in q]}, 'gripSectionM': {'width': round(float(np.ptp(sec[:, 0])), 4), 'depth': round(float(np.ptp(sec[:, 2])), 4)}, 'fingers': S['fingers']['curlDeg']}


def grip(fig, G):
    """props.json grip: the closed hand of the drawn states, over the clip's own fingers (in the game, a hand layer masked to the right fingers).
    Each finger bone turns about its own X (fit.json fingers: the bones' Z is on the back of the hand), toward the palm by the angle given."""
    if not has_fingers(fig): return
    sol = G.get('solved') or G.get('curlDeg') or G['curlMaxDeg']; tb = sol.get('ThumbBase', [0, 0])
    for f, angs in sol.items():
        if f == 'ThumbBase': continue
        for i, ang in enumerate(angs, 1):
            pb = fig.arm.pose.bones.get(f'RightHand{f}{i}')
            if pb is None: continue
            b = Quaternion((0, 0, 1), math.radians(tb[1])) @ Quaternion((1, 0, 0), math.radians(tb[0])) if (f == 'Thumb' and i == 1) else Quaternion()
            pb.rotation_mode = 'QUATERNION'; pb.rotation_quaternion = b @ Quaternion((1, 0, 0), -math.radians(ang))
    bpy.context.view_layer.update()


def wrap(fig, prop):
    """the closed hand around the grip: each finger's chain (the hand's head, its knuckle, its joints, its tip), seen down the grip's own axis
    (the prop's down), turns about the grip point by this many degrees; a finger that closes round the grip turns past wrapMinDeg"""
    if not has_fingers(fig): return None
    a = fig.arm; Mw = a.matrix_world; g = Vector(prop.matrix_world.translation)
    axis = (prop.matrix_world.to_3x3() @ Vector((0, 0, 1))).normalized()          # the prop's up (Blender +Z): the grip runs along it
    Vp = AN.co(prop); hp = (Vp - np.array(g)) @ np.array(axis)
    out = {}
    for f in ('Index', 'Middle', 'Ring', 'Pinky', 'Thumb'):
        if f'RightHand{f}1' not in a.pose.bones: continue
        pts = [Mw @ a.pose.bones[f'RightHand{f}{i}'].head for i in (1, 2, 3)] + [Mw @ a.pose.bones[f'RightHand{f}3'].tail]   # knuckle, joints, tip
        # the grip's own centre at this finger's height along it (the grip rakes back: one line through the grip point misses the lower fingers)
        lv = float((pts[1] - g).dot(axis)); sel = np.abs(hp - lv) < 0.01; gc = Vector(Vp[sel].mean(0)) if sel.sum() >= 3 else g
        def flat(p, gc=gc): d = p - gc; return d - axis * d.dot(axis)
        tot = 0.0
        for p, q in zip(pts, pts[1:]):
            u, v = flat(p), flat(q)
            if u.length < 1e-6 or v.length < 1e-6: continue
            tot += math.degrees(math.atan2(u.cross(v).dot(axis), u.dot(v)))
        out[f] = round(abs(tot), 1)
    return out


def seat_frame(fig, prop):
    origin = np.array(prop.matrix_world.translation); dmin = 1e9
    for V, T, td in body_parts(fig):
        hv = np.unique(T[np.array([str(b).startswith('RightHand') for b in td])])
        if len(hv): dmin = min(dmin, float(np.linalg.norm(V[hv] - origin, axis=1).min()))
    a = fig.arm; fa = a.matrix_world @ a.pose.bones['RightForeArm'].head; hd = a.matrix_world @ a.pose.bones['RightHand'].head
    barrel = (prop.matrix_world.to_3x3() @ Vector((0, -1, 0))).normalized(); by = crossings(fig, prop)
    lr = (a.matrix_world @ a.pose.bones['LeftUpLeg'].head) - (a.matrix_world @ a.pose.bones['RightUpLeg'].head); lr.z = 0; face = lr.cross(Vector((0, 0, 1))).normalized()
    flat = Vector((barrel.x, barrel.y, 0)); fa_ = (hd - fa).normalized(); ff = Vector((fa_.x, fa_.y, 0))
    info = {'barrelToFacingGroundDeg': round(math.degrees(flat.angle(face)), 2), 'barrelPitchDeg': round(math.degrees(math.asin(max(-1, min(1, barrel.z)))), 2),
            'forearmToFacingGroundDeg': round(math.degrees(ff.angle(face)), 2), 'forearmPitchDeg': round(math.degrees(math.asin(max(-1, min(1, fa_.z)))), 2)}
    return {**info, 'gripToHandM': round(dmin, 4), 'barrelToForearmDeg': round(math.degrees(barrel.angle((hd - fa).normalized())), 2), 'barrelToSightDeg': round(math.degrees(barrel.angle(sight(a).normalized())), 2),
            'propBodyPairs': sum(v for k, v in by.items() if k not in ('RightHand', 'LeftHand')), 'crossings': by}


def holster_frame(fig, prop):
    by = crossings(fig, prop); arm = ('RightHand', 'LeftHand', 'RightForeArm', 'LeftForeArm', 'RightArm', 'LeftArm')
    return {'propBodyPairs': sum(v for k, v in by.items() if k not in arm), 'armPairs': sum(v for k, v in by.items() if k in arm), 'crossings': by}


def sight(a):
    """the aim line in world: level, from between the shoulders out through the gun hand (the arms reach for the target; the clip holds the
    gun at chest height, so a line from the eyes through the hand pointed 25-28 degrees at the ground)"""
    mid = (a.matrix_world @ a.pose.bones['LeftArm'].head + a.matrix_world @ a.pose.bones['RightArm'].head) / 2
    d = (a.matrix_world @ a.pose.bones['Attach_RightHand'].head) - mid; d.z = 0; return d


def aim_frames(fig, clip, S):
    """seat.aimFrames: the frames where the right hand is near its highest above the hips"""
    sc = bpy.context.scene; hs = []
    for fr in range(clip.f0, clip.f1 + 1):
        sc.frame_set(fr); AN.retarget(fig, clip); bpy.context.view_layer.update(); a = fig.arm
        hs.append(((a.matrix_world @ a.pose.bones['RightHand'].head) - (a.matrix_world @ a.pose.bones['Hips'].head)).z)
    hs = np.array(hs); ok = np.nonzero(hs >= hs.max() - S['aimFrames']['handWithinM'])[0] + clip.f0
    return [int(ok[int(round(i))]) for i in np.linspace(0, len(ok) - 1, S['aimFrames']['count'])]


def render(sc, cam, path, w, h):
    sc.render.resolution_x = w; sc.render.resolution_y = h; sc.render.filepath = path; bpy.ops.render.render(write_still=True)


def raw_vs_clean():
    bpy.ops.wm.read_factory_settings(use_empty=True); sc = bpy.context.scene; look.studio(sc, 900, 600); cam = look.camera(sc)
    clean = load_prop('clean'); before = set(bpy.data.objects); bpy.ops.import_scene.gltf(filepath=GLB)
    raw = [x for x in bpy.data.objects if x not in before and x.type == 'MESH'][0]
    raw.data.transform(raw.matrix_world); raw.matrix_world = Matrix.Identity(4); raw.parent = None
    V = np.array([list(v.co) for v in raw.data.vertices]); s = P['size']['lengthM'] / (V[:, 1].max() - V[:, 1].min()); c = (V.max(0) + V.min(0)) / 2
    raw.data.transform(Matrix.Rotation(math.pi, 4, 'Z') @ Matrix.Scale(s, 4) @ Matrix.Translation(Vector(-c)))   # shown the way the cleaned one faces (barrel -Y)
    for o, z in ((raw, 0.18), (clean, 0.0)): o.location = (0, 0, 0.12 + z)
    raw.location.z += 0.0; cv = np.array([list(v.co) for v in clean.data.vertices]); clean.location.z = 0.12 - (cv[:, 2].max() + cv[:, 2].min()) / 2
    views = {'left': Vector((-0.9, 0, 0)), 'right': Vector((0.9, 0, 0)), 'threeq': Vector((-0.55, -0.6, 0.35))}
    for vn, d in views.items():
        tgt = Vector((0, 0, 0.21)); look.look_at(cam, tgt + d, tgt); cam.data.type = 'ORTHO'; cam.data.ortho_scale = 0.5
        render(sc, cam, os.path.join(OUT, f'raw_vs_clean_{vn}.png'), 700, 800)
    return {'rawTris': len(L.tris_of(raw)), 'cleanTris': len(L.tris_of(clean))}


def only(figs, f):
    """render one figure: its shown LOD meshes on, every other figure's off"""
    for g in figs:
        for ms in g.meshes.values():
            for o in ms:
                o.hide_render = (g is not f) or o.get('fit_role', 'body') in ('donor', 'prop')
    for o in bpy.data.objects:   # the figures' own separate props (the guard's tray) never show here
        if o.get('fit_role') in ('donor', 'prop'): o.hide_render = True


def main():
    res = {'prop': PID, 'rawVsClean': raw_vs_clean()}
    bpy.ops.wm.read_factory_settings(use_empty=True)
    figs = [AN.Fig(f) for f in SPEC['figures']]; sc, cam = AN.setup_render(); RP = L.D['render']['play']
    import glob
    for f, fs in zip(figs, SPEC['figures']):   # the delivered albedo (budget size, merged props' colour patches painted in), as models.py shows it
        t = sorted(glob.glob(os.path.join(os.path.dirname(fs['stats']), f"{fs['id']}_H_albedo.*")))
        if not t: continue
        img = bpy.data.images.load(t[0])
        for ms in f.meshes.values():
            for o in ms:
                for m in o.data.materials:
                    if m and m.use_nodes:
                        for n in m.node_tree.nodes:
                            if n.type == 'TEX_IMAGE': n.image = img
    A = P['attach']; S = P['seat']; lib = os.path.expanduser(L.D['clips']['lib'])
    def clip_path(c): return os.path.join(lib, L.D['clips']['list'].get(c) or D['clipFiles'][c])
    props = {f.label: {'hand': load_prop(f'{PID}_hand_{f.label}'), 'holster': load_prop(f'{PID}_holster_{f.label}')} for f in figs}
    fails = []; seat = {}
    for d_ in props.values():
        for o in d_.values(): o.hide_render = True
    hold = props[figs[0].label]['hand']   # placeholder, re-mounted per figure
    # 1. the hand turn from the clip (attach.hand.turnFromClip): the forearm line in the bone's frame over the aim frames, every figure
    aim = AN.Clip(clip_path(S['clip'])); frames = aim_frames(figs[0], aim, S); vs = {}
    for fr in frames:
        sc.frame_set(fr)
        for f in figs:
            AN.retarget(f, aim); bpy.context.view_layer.update(); a = f.arm
            B = (a.matrix_world @ a.pose.bones[A['hand']['bone']].matrix).to_3x3().normalized()
            fore = sight(a)   # the aim line: the eyes through the gun hand (attach.hand.turnFromClip)
            vs.setdefault(f.label, []).append(np.array(B.inverted() @ fore.normalized()))
    allv = np.concatenate([np.array(v) for v in vs.values()]); v = Vector(allv.mean(0)).normalized()
    TURN = Vector((0, 0, 1)).rotation_difference(v).to_matrix().to_4x4()
    UC = os.path.join(PA, 'propcal.json'); unity = json.load(open(UC)) if os.path.exists(UC) else None
    if A['hand'].get('seat') == 'hand':
        # the grip seated by the hand alone (props.json attach.hand.seat): the attach frame's own axes, barrel along the hand's rest forearm line,
        # the same in Blender and Unity (the frame agrees to 0.0 degrees); where the barrel points in a clip is the wrist's, and Unity's
        # Humanoid wrist differs from Blender's, so the level aim is a runtime correction (Latch), measured by PropCal in Unity
        TURN = Matrix.Identity(4)
    spread = {k: round(max(math.degrees(Vector(x).angle(v)) for x in vv), 2) for k, vv in vs.items()}
    hq = unity_quat(TURN); hold_off = A['hand']['offset']; hold_fing = None
    fing = [f for f in figs if has_fingers(f)]
    if fing and P['grip'].get('seatFromClosedHand'):
        # the grip seated by the closed hand (props.json grip.seatFromClosedHand): at rest with the grip pose on, the circle through the middle
        # finger's knuckle, last joint and tip has its centre where the grip's line runs; the prop's origin goes onto that centre (its height
        # along the grip kept), the same in every finger-boned figure's hand frame (the mean of them)
        cs = []
        for f in fing:
            for pb in f.arm.pose.bones: pb.matrix_basis = Matrix.Identity(4)
            bpy.context.view_layer.update(); grip(f, P['grip']); a = f.arm; Mw = a.matrix_world
            Bm = (Mw @ a.pose.bones[A['hand']['bone']].matrix); Bi = Bm.inverted()
            p1, p3, p4 = [Bi @ (Mw @ x) for x in (a.pose.bones['RightHandMiddle1'].head, a.pose.bones['RightHandMiddle3'].head, a.pose.bones['RightHandMiddle3'].tail)]
            u, v = p3 - p1, p4 - p1; w = u.cross(v); cc = p1 + (w.cross(u) * v.length_squared + v.cross(w) * u.length_squared) / (2 * w.length_squared)   # circumcentre
            cs.append(cc)
        cc = sum(cs, Vector()) / len(cs)
        hold_fing = [round(-cc.x, 4), A['hand']['offset'][1], round(cc.z, 4)]   # the bone frame (Blender) to Unity local: x mirrored; the grip's height kept
        for f in figs:
            for pb in f.arm.pose.bones: pb.matrix_basis = Matrix.Identity(4)
        bpy.context.view_layer.update()
    GRIP = None
    if fing and P['grip'].get('mode') == 'contact':
        GRIP = solve_grip(fing, props, A, P['grip'], hold_fing, TURN); hold_fing = GRIP['seat']; P['grip']['solved'] = GRIP['curlDeg']
    def hoff(f): return hold_fing if (hold_fing and has_fingers(f)) else hold_off
    width = float(json.load(open(os.path.join(PA, f'{PID}.json')))['sizeM']['width']); R_ = A['holster']['offsetRule']
    ROLES = D['roles']; role = {f.label: ROLES.get(next(x for x in SPEC['figures'] if x['label'] == f.label).get('role', ''), {}) for f in figs}
    vis = [f for f in figs if role[f.label].get('holster') == 'visible' and role[f.label].get('weapon') == PID]
    skip_holster = {f.label for f in figs if f not in vis}
    for f in vis:   # a role with a visible holster keeps its right hip free (fit/lib.role_skips): a bag there is a build fault
        st = json.load(open(next(x for x in SPEC['figures'] if x['label'] == f.label)['stats']))
        if any(c.get('prop') == 'prop_bag' for c in st.get('colourPatches', [])) or 'prop_bag' in st.get('hybrid', {}).get('props', []): fails.append(f'{f.label}: a bag on the holster hip')
    def hol(back): return [round(width / 2 + R_['clearM'], 4), -back, -R_['upM']]
    def arm_out(f, deg):   # the holster side's upper arm turned out about the figure's facing (a fixed layer for holster roles; animate.layer's form)
        if not deg: return
        a = f.arm; lr = (a.matrix_world @ a.pose.bones['LeftUpLeg'].head) - (a.matrix_world @ a.pose.bones['RightUpLeg'].head); lr.z = 0
        fwd = lr.cross(Vector((0, 0, 1))).normalized(); pb = a.pose.bones['RightArm']; hd = pb.head.copy()
        R = Matrix.Rotation(math.radians(deg) * -1, 4, (a.matrix_world.inverted().to_3x3() @ fwd))
        pb.matrix = Matrix.Translation(hd) @ R @ Matrix.Translation(-hd) @ pb.matrix; bpy.context.view_layer.update()
    # the holster trials (seat.holster.trials) on every figure that shows it: crossings with the arm and with the body, the fewest wins
    H_ = S['holster']; trials = []
    if vis:
        wk = AN.Clip(clip_path(H_['clip'])); n = wk.f1 - wk.f0; hfr = [wk.f0 + int(round(t * n)) for t in H_['frames']]
        for tr in H_['trials']:
            row = {'armOutDeg': tr['armOutDeg'], 'backM': tr['backM'], 'armPairs': 0, 'bodyPairs': 0, 'framesTouched': 0, 'byFigure': {}}
            for fr in hfr:
                sc.frame_set(fr)
                for f in vis:
                    AN.retarget(f, wk); bpy.context.view_layer.update(); arm_out(f, tr['armOutDeg'])
                    mount(props[f.label]['holster'], f, A['holster']['bone'], hol(tr['backM'])); bpy.context.view_layer.update()
                    r = holster_frame(f, props[f.label]['holster']); row['armPairs'] += r['armPairs']; row['bodyPairs'] += r['propBodyPairs']
                    row['framesTouched'] += 1 if r['armPairs'] else 0; row['byFigure'].setdefault(f.label, []).append(r['armPairs'])
            trials.append(row)
        wk.remove()
        best = min(trials, key=lambda r: (r['bodyPairs'] > 0, r['armPairs'], r['armOutDeg'], r['backM']))
    else: best = {'armOutDeg': 0, 'backM': R_['backM']}
    hol_off = hol(best['backM']); ARM_OUT = best['armOutDeg']
    SUP = support_target(A['support'])
    attach = {'support': SUP, 'convention': D['convention'], 'skeleton': 'the new pipeline (MPFB, Attach_* by fit.json attach.frame)', 'prop': PID,
              'hand': {'bone': A['hand']['bone'], 'localPosition': hold_off, 'localPositionFingerBoned': hold_fing, 'grip': GRIP, 'localRotation': hq, 'localEulerAngles': unity_euler(hq), 'source': 'the hand alone (attach frame identity)' if A['hand'].get('seat') == 'hand' else 'Blender aim frames',
                       'unityHold': None if unity is None else {f: {'frames': [r[0]['frame'], r[-1]['frame']], 'pitchDeg': [min(x['pitchDeg'] for x in r), max(x['pitchDeg'] for x in r)], 'yawToAimDeg': [min(x['yawToAimDeg'] for x in r), max(x['yawToAimDeg'] for x in r)]} for f, r in unity['figures'].items()},
                       'blenderAimFrames': frames, 'blenderSpreadDegByFigure': spread},
              'holster': {'usedBy': A['holster']['usedBy'], 'note': A['holster']['note'], 'bone': A['holster']['bone'], 'localPosition': hol_off, 'localRotation': [0, 0, 0, 1], 'localEulerAngles': [0, 0, 0],
                          'armOutDeg': ARM_OUT, 'armOutNote': 'for roles with a visible holster: the right upper arm turned out this far about the figure facing (a Unity layer or an additive pose, Latch)', 'trials': trials},
              'roles': ROLES, 'show': A['hand']['show']}
    json.dump(attach, open(os.path.join(PA, 'attach.json'), 'w'), indent=1, sort_keys=True)
    # 2. the hand-scale strip, each figure at rest: the prop beside the hand, then in it
    sc.frame_set(1)
    for f in figs:
        for pb in f.arm.pose.bones: pb.location = (0, 0, 0); pb.rotation_quaternion = (1, 0, 0, 0)
    bpy.context.view_layer.update()
    for f in figs:
        only(figs, f); hp = props[f.label]['hand']; hp.hide_render = False; side = props[f.label]['holster']; side.hide_render = False
        mount(hp, f, A['hand']['bone'], hoff(f), TURN); hd = f.arm.matrix_world @ f.arm.pose.bones['RightHand'].head
        side.matrix_world = Matrix.Translation(hd + Vector((0, -0.05, 0.22))) @ Matrix.Rotation(math.radians(-90), 4, 'Z')   # beside the hand, side on, true size
        look.look_at(cam, hd + Vector((-0.1, -1.0, 0.1)), hd + Vector((0, 0, 0.1))); cam.data.type = 'ORTHO'; cam.data.ortho_scale = 0.6
        render(sc, cam, os.path.join(OUT, f'hand_scale_{f.label}.png'), 800, 800)
        look.look_at(cam, hd + Vector((-0.6, -2.4, 0.0)), Vector((f.arm.location.x, 0, 0.9))); cam.data.ortho_scale = 2.0
        render(sc, cam, os.path.join(OUT, f'hand_scale_{f.label}_full.png'), 700, 800)
        hp.hide_render = True; side.hide_render = True
    # 3. the clips: checks at their frames, pictures at the shown ones
    shows = [('aim_pistol', 'hand'), ('idle', 'hand'), ('run', 'curl')] + ([('walk', 'holster')] if vis else [])   # the holster shows on roles with a visible holster only; run: the clip's own finger curl
    for cname, mode in shows:
        clip = aim if cname == S['clip'] else AN.Clip(clip_path(cname)); n = clip.f1 - clip.f0
        if cname == S['clip']: fr_check = frames; fr_show = [frames[len(frames) // 2]]
        elif cname == S['holster']['clip']: fr_check = [clip.f0 + int(round(t * n)) for t in S['holster']['frames']]; fr_show = fr_check[1:3]
        else: fr_check = []; fr_show = [clip.f0 + n // 2]
        for fr in sorted(set(fr_check) | set(fr_show)):
            sc.frame_set(fr)
            for f in figs:
                AN.retarget(f, clip); bpy.context.view_layer.update()
                if mode == 'holster' and f.label not in skip_holster: arm_out(f, ARM_OUT)
                if mode == 'hand' and cname in A['hand']['show']['onlyWhile']: grip(f, P['grip'])   # the drawn states close the hand
                mount(props[f.label]['hand'], f, A['hand']['bone'], hoff(f), TURN); mount(props[f.label]['holster'], f, A['holster']['bone'], hol_off); bpy.context.view_layer.update()
                if fr in fr_check and not (mode == 'holster' and f.label in skip_holster):
                    r = seat_frame(f, props[f.label]['hand']) if mode == 'hand' else holster_frame(f, props[f.label]['holster']); r['frame'] = fr
                    if mode == 'hand' and has_fingers(f) and cname in A['hand']['show']['onlyWhile']:
                        r['wrapDeg'] = wrap(f, props[f.label]['hand'])
                        if r['crossings'].get('RightHand', 0) > P['grip']['handCrossMaxPairs']: fails.append(f"{f.label} {cname} f{fr}: the hand crosses the grip ({r['crossings'].get('RightHand', 0)} pairs)")
                        if any(r['wrapDeg'].get(k, 0) < P['grip']['wrapMinDeg'] for k in P['grip']['wrapFingers']): fails.append(f"{f.label} {cname} f{fr}: a finger does not close round the grip {r['wrapDeg']}")
                    seat.setdefault(f.label, {}).setdefault(cname, []).append(r)
                    if mode == 'hand':
                        if r['gripToHandM'] > S['gripToPalmMaxM']: fails.append(f"{f.label} {cname} f{fr}: grip {r['gripToHandM'] * 100:.1f} cm from the hand")
                        if r['barrelToSightDeg'] > S['barrelToSightMaxDeg'] and unity is None: fails.append(f"{f.label} {cname} f{fr}: barrel {r['barrelToSightDeg']:.1f} deg off the aim line")
                    if r['propBodyPairs'] > S['propBodyMaxPairs']: fails.append(f"{f.label} {cname} f{fr}: prop crosses the body {dict((str(k), v) for k, v in r['crossings'].items())}")
            if fr not in fr_show: continue
            for f in figs:
                if mode == 'holster' and f.label in skip_holster: continue
                only(figs, f)
                for g in figs:
                    for k, o in props[g.label].items(): o.hide_render = g is not f or k != mode
                a = f.arm; hd = a.matrix_world @ a.pose.bones['RightHand'].head; hdh = a.matrix_world @ a.pose.bones['Head'].head
                hip = a.matrix_world @ a.pose.bones['Hips'].head; tag = f'{f.label}_{cname}_f{fr:03d}'
                if mode == 'curl':   # the hand alone, close
                    if not has_fingers(f): continue
                    for k, o in props[f.label].items(): o.hide_render = True
                    look.look_at(cam, hd + Vector((-0.35, -0.35, 0.05)), hd + (a.matrix_world @ a.pose.bones['RightHandMiddle1'].head - hd) * 0.8); cam.data.type = 'PERSP'; cam.data.angle_y = math.radians(25)
                    render(sc, cam, os.path.join(OUT, f'{tag}_hand.png'), 720, 720); continue
                if mode == 'hand':   # close third person: over the right shoulder, looking down the arm
                    look.look_at(cam, hdh + Vector((-0.45, 0.9, 0.2)), hd + Vector((0, -0.3, 0)))
                else: look.look_at(cam, hip + Vector((-1.1, -0.8, 0.25)), hip + Vector((-0.1, 0, -0.1)))
                cam.data.type = 'PERSP'; cam.data.angle_y = math.radians(35)
                render(sc, cam, os.path.join(OUT, f'{tag}_close.png'), 720, 720)
                if mode == 'hand':   # and from the front-right, the hand and the prop clear; and a close-up of the grip
                    look.look_at(cam, hd + Vector((-0.55, -0.75, 0.15)), hd); render(sc, cam, os.path.join(OUT, f'{tag}_hand.png'), 720, 720)
                    look.look_at(cam, hd + Vector((-0.25, -0.2, -0.12)), hd); cam.data.angle_y = math.radians(30); render(sc, cam, os.path.join(OUT, f'{tag}_grip.png'), 720, 720)
                    look.look_at(cam, hd + Vector((0.05, -0.3, 0.12)), hd); render(sc, cam, os.path.join(OUT, f'{tag}_grip2.png'), 720, 720)
                    pr = props[f.label]['hand']; ga = (pr.matrix_world.to_3x3() @ Vector((0, 0, 1))).normalized(); gp = pr.matrix_world.translation
                    look.look_at(cam, gp - ga * 0.45, gp); render(sc, cam, os.path.join(OUT, f'{tag}_gripaxis.png'), 720, 720); cam.data.angle_y = math.radians(35)
                look.play_cam(cam, tuple(hip), RP['yawDeg'], RP['pitchDeg'], RP['ppm'], RP['h'])
                render(sc, cam, os.path.join(OUT, f'{tag}_play.png'), RP['w'], RP['h']); cam.data.type = 'PERSP'
        if clip is not aim: clip.remove()
    aim.remove()
    json.dump(attach, open(os.path.join(PA, 'attach.json'), 'w'), indent=1, sort_keys=True)
    res['attach'] = attach; res['seat'] = seat; res['fails'] = fails; res['ok'] = not fails
    json.dump(res, open(os.path.join(OUT, 'seat.json'), 'w'), indent=1, sort_keys=True)
    print('SEAT ' + json.dumps({'ok': not fails, 'fails': fails, 'hand': attach['hand'], 'holster': attach['holster']}))
    if fails: sys.exit(1)

main()
