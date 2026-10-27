"""Brief 069 props: the seat check (fails loudly) and the pictures for a held prop on the new pipeline's figures.
  out/scratch/forma/char-pipeline/blender.sh -P tools/figures/charpipe/props/prop_show.py -- <spec.json>
spec: animate.py's ({"out", "name", "figures": [{"blend", "label", "id", "x"}]}) plus "prop": id. The figures and clips go through animate.py's own
machinery (its Humanoid-like retarget, the same zero), so what is checked here is what the clip pictures show.
Writes <out>/: raw_vs_clean_<view>.png, <figure>_<clip>_close.png and _play.png (hand: aim_pistol, idle; holster: walk), hand_scale_<figure>.png,
seat.json. Exit 1 when the seat check fails (props.json seat rule)."""
import os, sys, json, math, importlib.util
import numpy as np
import bpy
from mathutils import Vector, Matrix
HERE = os.path.dirname(os.path.abspath(__file__)); CP = os.path.dirname(HERE); ROOT = os.path.dirname(os.path.dirname(os.path.dirname(CP)))
AN = importlib.util.module_from_spec(s := importlib.util.spec_from_file_location('animate', os.path.join(CP, 'fit', 'animate.py'))); s.loader.exec_module(AN)
L = AN.L; look = AN.look
D = json.load(open(os.path.join(HERE, 'props.json'))); SPEC = AN.SPEC; PID = SPEC['prop']; P = D['props'][PID]
PA = os.path.join(ROOT, 'out/scratch/forma/char-pipeline/props', PID, 'a'); OUT = L.ensure(SPEC['out'])
GLB = os.path.join(ROOT, 'paid', P['mesh']['job'], 'meshes', f'{PID}_t1.glb')
# Unity bone frame -> Blender bone frame: Unity +Y, +Z are the bone's own Y, Z; Unity +X is the bone's -X (the X mirror of the FBX import).
# The prop's Blender axes: barrel +Y, up +Z, its right side -X. Unity's identity localRotation = barrel on the bone's Z, up on its Y.
M_ROT = Matrix(((-1, 0, 0, 0), (0, 0, 1, 0), (0, 1, 0, 0), (0, 0, 0, 1)))   # prop (Blender) -> bone (Blender): X->-X, Y->Z, Z->Y


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


def seat_frame(fig, prop):
    origin = np.array(prop.matrix_world.translation); dmin = 1e9
    for V, T, td in body_parts(fig):
        hv = np.unique(T[np.array([str(b).startswith('RightHand') for b in td])])
        if len(hv): dmin = min(dmin, float(np.linalg.norm(V[hv] - origin, axis=1).min()))
    a = fig.arm; fa = a.matrix_world @ a.pose.bones['RightForeArm'].head; hd = a.matrix_world @ a.pose.bones['RightHand'].head
    barrel = (prop.matrix_world.to_3x3() @ Vector((0, 1, 0))).normalized(); by = crossings(fig, prop)
    return {'gripToHandM': round(dmin, 4), 'barrelToForearmDeg': round(math.degrees(barrel.angle((hd - fa).normalized())), 2),
            'propBodyPairs': sum(v for k, v in by.items() if k not in ('RightHand', 'LeftHand')), 'crossings': by}


def holster_frame(fig, prop):
    by = crossings(fig, prop); arm = ('RightHand', 'LeftHand', 'RightForeArm', 'LeftForeArm', 'RightArm', 'LeftArm')
    return {'propBodyPairs': sum(v for k, v in by.items() if k not in arm), 'armPairs': sum(v for k, v in by.items() if k in arm), 'crossings': by}


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
    raw.data.transform(Matrix.Scale(s, 4) @ Matrix.Translation(Vector(-c)))
    for o, z in ((raw, 0.18), (clean, 0.0)): o.location = (0, 0, 0.12 + z)
    raw.location.z += 0.0; cv = np.array([list(v.co) for v in clean.data.vertices]); clean.location.z = 0.12 - (cv[:, 2].max() + cv[:, 2].min()) / 2
    views = {'left': Vector((0.9, 0, 0)), 'right': Vector((-0.9, 0, 0)), 'threeq': Vector((0.55, 0.6, 0.35))}
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
            fore = (a.matrix_world @ a.pose.bones['RightHand'].head) - (a.matrix_world @ a.pose.bones['RightForeArm'].head)
            vs.setdefault(f.label, []).append(np.array(B.inverted() @ fore.normalized()))
    allv = np.concatenate([np.array(v) for v in vs.values()]); v = Vector(allv.mean(0)).normalized()
    TURN = Vector((0, 0, 1)).rotation_difference(v).to_matrix().to_4x4()
    spread = {k: round(max(math.degrees(Vector(x).angle(v)) for x in vv), 2) for k, vv in vs.items()}
    hq = unity_quat(TURN); hold_off = A['hand']['offset']
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
    attach = {'convention': D['convention'], 'skeleton': 'the new pipeline (MPFB, Attach_* by fit.json attach.frame)', 'prop': PID,
              'hand': {'bone': A['hand']['bone'], 'localPosition': hold_off, 'localRotation': hq, 'localEulerAngles': unity_euler(hq), 'aimFrames': frames, 'spreadDegByFigure': spread},
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
        mount(hp, f, A['hand']['bone'], hold_off, TURN); hd = f.arm.matrix_world @ f.arm.pose.bones['RightHand'].head
        side.matrix_world = Matrix.Translation(hd + Vector((0, -0.05, 0.22))) @ Matrix.Rotation(math.radians(-90), 4, 'Z')   # beside the hand, side on, true size
        look.look_at(cam, hd + Vector((-0.1, -1.0, 0.1)), hd + Vector((0, 0, 0.1))); cam.data.type = 'ORTHO'; cam.data.ortho_scale = 0.6
        render(sc, cam, os.path.join(OUT, f'hand_scale_{f.label}.png'), 800, 800)
        look.look_at(cam, hd + Vector((-0.6, -2.4, 0.0)), Vector((f.arm.location.x, 0, 0.9))); cam.data.ortho_scale = 2.0
        render(sc, cam, os.path.join(OUT, f'hand_scale_{f.label}_full.png'), 700, 800)
        hp.hide_render = True; side.hide_render = True
    # 3. the clips: checks at their frames, pictures at the shown ones
    shows = [('aim_pistol', 'hand'), ('idle', 'hand')] + ([('walk', 'holster')] if vis else [])   # the holster shows on roles with a visible holster only
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
                mount(props[f.label]['hand'], f, A['hand']['bone'], hold_off, TURN); mount(props[f.label]['holster'], f, A['holster']['bone'], hol_off); bpy.context.view_layer.update()
                if fr in fr_check and not (mode == 'holster' and f.label in skip_holster):
                    r = seat_frame(f, props[f.label]['hand']) if mode == 'hand' else holster_frame(f, props[f.label]['holster']); r['frame'] = fr
                    seat.setdefault(f.label, {}).setdefault(cname, []).append(r)
                    if mode == 'hand':
                        if r['gripToHandM'] > S['gripToPalmMaxM']: fails.append(f"{f.label} {cname} f{fr}: grip {r['gripToHandM'] * 100:.1f} cm from the hand")
                        if r['barrelToForearmDeg'] > S['barrelToForearmMaxDeg']: fails.append(f"{f.label} {cname} f{fr}: barrel {r['barrelToForearmDeg']:.1f} deg off the forearm")
                    if r['propBodyPairs'] > S['propBodyMaxPairs']: fails.append(f"{f.label} {cname} f{fr}: prop crosses the body {dict((str(k), v) for k, v in r['crossings'].items())}")
            if fr not in fr_show: continue
            for f in figs:
                if mode == 'holster' and f.label in skip_holster: continue
                only(figs, f)
                for g in figs:
                    for k, o in props[g.label].items(): o.hide_render = g is not f or k != mode
                a = f.arm; hd = a.matrix_world @ a.pose.bones['RightHand'].head; hdh = a.matrix_world @ a.pose.bones['Head'].head
                hip = a.matrix_world @ a.pose.bones['Hips'].head; tag = f'{f.label}_{cname}_f{fr:03d}'
                if mode == 'hand':   # close third person: over the right shoulder, looking down the arm
                    look.look_at(cam, hdh + Vector((-0.45, 0.9, 0.2)), hd + Vector((0, -0.3, 0)))
                else: look.look_at(cam, hip + Vector((-1.1, -0.8, 0.25)), hip + Vector((-0.1, 0, -0.1)))
                cam.data.type = 'PERSP'; cam.data.angle_y = math.radians(35)
                render(sc, cam, os.path.join(OUT, f'{tag}_close.png'), 720, 720)
                if mode == 'hand':   # and from the front-right, the hand and the prop clear
                    look.look_at(cam, hd + Vector((-0.55, -0.75, 0.15)), hd); render(sc, cam, os.path.join(OUT, f'{tag}_hand.png'), 720, 720)
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
