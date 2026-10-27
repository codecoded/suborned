"""Brief 069 props, the studio route (Sky: "official best practice"): each weapon in each figure's hands on its class's own clip set, with the
clip's own fingers (the chosen clips hold a weapon of standard size), checked and pictured. Blender preview; the Unity measure is PropCal.
  out/scratch/forma/char-pipeline/blender.sh -P tools/figures/charpipe/props/weapon_show.py -- <spec.json>
spec: animate.py's ({"out", "name", "figures": [{"blend" | "fbx", "label", "id", "x"}]}) plus "weapons": [prop ids].
Per figure and weapon: the seat (props.json seat): the weapon's grip socket at the centre of the clip's closed right middle finger (knuckle, last
joint, tip) at the aim frames, in Attach_RightHand's frame, rotation identity (the hand-seated rule); figures without Attach bones (today's game
figures) get them by fit.json attach's rule first. Checks at the aim frames (props.json spec.checks): wrap (middle and ring >= wrapMinDeg about
the grip), hand-weapon crossings <= handCrossMaxPairs, and reported: the support socket's distance from the clip's left Attach_LeftHand (the gap
Two Bone IK closes), long guns' shoulder socket from the right shoulder. Writes <out>/<figure>_<weapon>_{close,hand,play}.png and checks.json;
exit 1 when a gate fails."""
import os, sys, json, math, importlib.util
import numpy as np
import bpy
from mathutils import Vector, Matrix, Quaternion
HERE = os.path.dirname(os.path.abspath(__file__)); CP = os.path.dirname(HERE); ROOT = os.path.dirname(os.path.dirname(os.path.dirname(CP)))
AN = importlib.util.module_from_spec(s := importlib.util.spec_from_file_location('animate', os.path.join(CP, 'fit', 'animate.py'))); s.loader.exec_module(AN)
L = AN.L; look = AN.look
D = json.load(open(os.path.join(HERE, 'props.json'))); SPEC = AN.SPEC; OUT = L.ensure(SPEC['out']); CK = D['spec']['checks']
M_ROT = Matrix(((1, 0, 0, 0), (0, 0, 1, 0), (0, -1, 0, 0), (0, 0, 0, 1)))   # prop (Blender: barrel -Y, up +Z) -> bone frame (prop_show.py)


def PA(pid): return os.path.join(ROOT, 'out/scratch/forma/char-pipeline/props', pid, 'a')


def load_weapon(pid, name):
    before = set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=os.path.join(PA(pid), f'{pid}.fbx'))
    ms = [o for o in bpy.data.objects if o not in before and o.type == 'MESH']
    for o in ms:
        o.data.transform(o.matrix_world); o.matrix_world = Matrix.Identity(4); o.parent = None
        stem = o.name.split('.')[0] if o.name.split('.')[0].startswith(pid) else pid
        alb = os.path.join(PA(pid), f'{stem}_albedo.png'); alb = alb if os.path.exists(alb) else os.path.join(PA(pid), f'{pid}_albedo.png')
        m = bpy.data.materials.new(f'{stem}_view'); m.use_nodes = True; nt = m.node_tree; b = nt.nodes['Principled BSDF']
        t = nt.nodes.new('ShaderNodeTexImage'); t.image = bpy.data.images.load(alb); nt.links.new(t.outputs['Color'], b.inputs['Base Color'])
        mp = alb.replace('_albedo', '_metal')
        if os.path.exists(mp):
            mt = nt.nodes.new('ShaderNodeTexImage'); mt.image = bpy.data.images.load(mp); mt.image.colorspace_settings.name = 'Non-Color'
            sp = nt.nodes.new('ShaderNodeSeparateColor'); nt.links.new(mt.outputs['Color'], sp.inputs[0]); nt.links.new(sp.outputs[0], b.inputs['Metallic'])
            inv = nt.nodes.new('ShaderNodeMath'); inv.operation = 'SUBTRACT'; inv.inputs[0].default_value = 1; nt.links.new(mt.outputs['Alpha'], inv.inputs[1]); nt.links.new(inv.outputs[0], b.inputs['Roughness'])
        o.data.materials.clear(); o.data.materials.append(m)
        if 'LOD' in o.name and not o.name.split('.')[0].endswith('LOD0'): o.hide_render = True; o.hide_viewport = True
    main = [o for o in ms if not o.hide_render][0]
    for o in ms:
        if o is not main and not o.hide_render: pass
    return main, ms


def ensure_attach(f):
    """today's game figures have no Attach bones: add them by the same rule (fit.json attach), from the rest skeleton and the body"""
    if 'Attach_RightHand' in f.arm.data.bones: return
    body = [o for ms in f.meshes.values() for o in ms if o.get('fit_role', 'body') == 'body'][0]
    co = np.array([list(body.matrix_world @ v.co) for v in body.data.vertices])
    L.add_attach(f.arm, co); bpy.context.view_layer.update()


def mount(o, fig, bone, off, turn=None):
    B = fig.arm.matrix_world @ fig.arm.pose.bones[bone].matrix
    o.matrix_world = B @ Matrix.Translation((-off[0], off[1], off[2])) @ (turn or Matrix.Identity(4)) @ M_ROT


def socket_world(o, sock):
    p = sock['position']; return o.matrix_world @ Vector((-p[0], -p[2], p[1]))          # Unity prop frame -> Blender prop frame (barrel -Y, up +Z, x mirrored)


def aim_frames(fig, clip, n):
    sc = bpy.context.scene; hs = []
    for fr in range(clip.f0, clip.f1 + 1):
        sc.frame_set(fr); AN.retarget(fig, clip); bpy.context.view_layer.update(); a = fig.arm
        hs.append(((a.matrix_world @ a.pose.bones['RightHand'].head) - (a.matrix_world @ a.pose.bones['Hips'].head)).z)
    hs = np.array(hs); ok = np.nonzero(hs >= hs.max() - 0.05)[0] + clip.f0
    return [int(ok[int(round(i))]) for i in np.linspace(0, len(ok) - 1, n)]


def circle_seat(fig):
    a = fig.arm; Mw = a.matrix_world; Bi = (Mw @ a.pose.bones['Attach_RightHand'].matrix).inverted()
    p1, p3, p4 = [Bi @ (Mw @ x) for x in (a.pose.bones['RightHandMiddle1'].head, a.pose.bones['RightHandMiddle3'].head, a.pose.bones['RightHandMiddle3'].tail)]
    u, v = p3 - p1, p4 - p1; w = u.cross(v)
    c = p1 + (w.cross(u) * v.length_squared + v.cross(w) * u.length_squared) / (2 * w.length_squared)
    return c, (c - p1).length


def wrap(fig, w):
    a = fig.arm; Mw = a.matrix_world; g = Vector(w.matrix_world.translation); axis = (w.matrix_world.to_3x3() @ Vector((0, 0, 1))).normalized()
    Vp = AN.co(w); hp = (Vp - np.array(g)) @ np.array(axis); out = {}
    for f in ('Index', 'Middle', 'Ring', 'Pinky'):
        pts = [Mw @ a.pose.bones[f'RightHand{f}{i}'].head for i in (1, 2, 3)] + [Mw @ a.pose.bones[f'RightHand{f}3'].tail]
        lv = float((pts[1] - g).dot(axis)); sel = np.abs(hp - lv) < 0.01; gc = Vector(Vp[sel].mean(0)) if sel.sum() >= 3 else g
        tot = 0.0
        for p_, q_ in zip(pts, pts[1:]):
            u = (p_ - gc) - axis * (p_ - gc).dot(axis); v = (q_ - gc) - axis * (q_ - gc).dot(axis)
            if u.length > 1e-6 and v.length > 1e-6: tot += math.degrees(math.atan2(u.cross(v).dot(axis), u.dot(v)))
        out[f] = round(abs(tot), 1)
    return out


def crossings(fig, w):
    Vw = AN.co(w); Tw = L.tris_of(w); n = 0
    for ms in fig.meshes.values():
        for o in ms:
            if o.get('fit_role', 'body') in ('donor', 'prop'): continue
            gi = {g.index: g.name for g in o.vertex_groups}; T = L.tris_of(o)
            dom = np.array([gi[max(v.groups, key=lambda x: x.weight).group] if v.groups else '' for v in o.data.vertices])
            T = T[np.char.startswith(dom[T].astype(str), 'RightHand').any(1)]
            if len(T): n += AN.overlap_pairs(Vw, Tw, AN.co(o), T)
    return n


def only(figs, f, weapons, wid):
    for g in figs:
        for ms in g.meshes.values():
            for o in ms: o.hide_render = (g is not f) or o.get('fit_role', 'body') in ('donor', 'prop')
    for (g, pid), (w, _) in weapons.items(): w.hide_render = not (g is f.label and pid == wid)
    for o in bpy.data.objects:
        if o.get('fit_role') in ('donor', 'prop'): o.hide_render = True


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    figs = [AN.Fig(f) for f in SPEC['figures']]
    for f in figs: ensure_attach(f)
    sc, cam = AN.setup_render(); RP = L.D['render']['play']; lib = os.path.expanduser(L.D['clips']['lib'])
    weapons = {(f.label, pid): load_weapon(pid, f'{pid}_{f.label}') for f in figs for pid in SPEC['weapons']}
    res = {'figures': {}, 'fails': []}
    for pid in SPEC['weapons']:
        P = D['props'][pid]; info = json.load(open(os.path.join(PA(pid), f'{pid}.json'))); sock = info['sockets']
        cs = D['clipSets'][P['class']]; clip = AN.Clip(os.path.join(lib, D['clipFiles'][cs['aim']]))
        for f in figs:
            w, _ = weapons[(f.label, pid)]; frames = aim_frames(f, clip, CK['aimFrames'])
            seats = []; radii = []; dirs = []
            for fr in frames:
                sc.frame_set(fr); AN.retarget(f, clip); bpy.context.view_layer.update(); c_, r_ = circle_seat(f); seats.append(c_); radii.append(r_)
                a = f.arm; Mw = a.matrix_world; Bm = Mw @ a.pose.bones['Attach_RightHand'].matrix
                dirs.append((Bm.to_3x3().inverted() @ ((Mw @ a.pose.bones['Attach_LeftHand'].head) - (Mw @ a.pose.bones['Attach_RightHand'].head))).normalized())
            c = sum(seats, Vector()) / len(seats); seat = [round(-c.x, 4), 0.0, round(c.z, 4)]           # bone (Blender) -> Unity local; the grip height kept
            turn = None
            if D['spec']['classes'][P['class']].get('barrelToSupportHand'):
                # a two-handed long gun lies from the right hand toward the left: its barrel along the clip's own right-to-left hand line
                dv = (sum(dirs, Vector()) / len(dirs)).normalized(); turn = Vector((0, 0, 1)).rotation_difference(dv).to_matrix().to_4x4()
            rows = []
            for fr in frames:
                sc.frame_set(fr); AN.retarget(f, clip); bpy.context.view_layer.update(); mount(w, f, 'Attach_RightHand', seat, turn); bpy.context.view_layer.update()
                a = f.arm; Mw = a.matrix_world; r = {'frame': fr, 'wrapDeg': wrap(f, w), 'handCrossPairs': crossings(f, w)}
                if 'support' in sock: r['supportGapM'] = round(float((socket_world(w, sock['support']) - Mw @ a.pose.bones['Attach_LeftHand'].head).length), 3)
                if 'shoulder' in sock: r['shoulderGapM'] = round(float((socket_world(w, sock['shoulder']) - Mw @ a.pose.bones['RightArm'].head).length), 3)
                rows.append(r)
                if any(r['wrapDeg'][k] < CK['wrapMinDeg'] for k in CK['wrapFingers']): res['fails'].append(f'{f.label} {pid} f{fr}: wrap {r["wrapDeg"]}')
                if r['handCrossPairs'] > CK['handCrossMaxPairs']: res['fails'].append(f'{f.label} {pid} f{fr}: the hand crosses the weapon ({r["handCrossPairs"]} pairs)')
            q_ = (turn or Matrix.Identity(4)).to_quaternion(); res['figures'].setdefault(f.label, {})[pid] = {'clip': cs['aim'], 'seat': seat, 'localRotation': [round(q_.x, 6), round(-q_.y, 6), round(-q_.z, 6), round(q_.w, 6)], 'closedHandRadiusM': round(float(np.mean(radii)), 4), 'checks': rows}
            fr = frames[len(frames) // 2]; sc.frame_set(fr)
            for g in figs: AN.retarget(g, clip)
            bpy.context.view_layer.update(); mount(w, f, 'Attach_RightHand', seat, turn); bpy.context.view_layer.update(); only(figs, f, weapons, pid)
            a = f.arm; hd = a.matrix_world @ a.pose.bones['RightHand'].head; hip = a.matrix_world @ a.pose.bones['Hips'].head; tag = f'{f.label}_{pid}'
            look.look_at(cam, hd + Vector((-0.6, -0.8, 0.2)), hd + Vector((0.1, -0.1, 0))); cam.data.type = 'PERSP'; cam.data.angle_y = math.radians(35)
            AN.look; sc.render.resolution_x = 720; sc.render.resolution_y = 720; sc.render.filepath = os.path.join(OUT, f'{tag}_close.png'); bpy.ops.render.render(write_still=True)
            look.look_at(cam, hd + Vector((-0.28, -0.22, 0.05)), hd); cam.data.angle_y = math.radians(30); sc.render.filepath = os.path.join(OUT, f'{tag}_hand.png'); bpy.ops.render.render(write_still=True)
            look.play_cam(cam, tuple(hip), RP['yawDeg'], RP['pitchDeg'], RP['ppm'], RP['h']); sc.render.resolution_x = RP['w']; sc.render.resolution_y = RP['h']
            sc.render.filepath = os.path.join(OUT, f'{tag}_play.png'); bpy.ops.render.render(write_still=True); cam.data.type = 'PERSP'
        clip.remove()
    res['ok'] = not res['fails']
    json.dump(res, open(os.path.join(OUT, 'checks.json'), 'w'), indent=1, sort_keys=True)
    print('WEAPONS ' + json.dumps({'ok': res['ok'], 'fails': res['fails'][:8]}))
    if res['fails']: sys.exit(1)


main()
