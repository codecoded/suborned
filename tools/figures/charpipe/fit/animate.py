"""Stage 3 (brief 069 fit test): play the stress clips on built figures, measure every sampled frame, render the close and play views.
  out/scratch/forma/char-pipeline/blender.sh -P tools/figures/charpipe/fit/animate.py -- <spec.json>
spec: {"out": dir, "name": str, "figures": [{"blend": path, "label": str, "x": metres, "layer": bool}], "clips": [names] (default fit.json clips),
       "frames": n, "close": bool, "play": bool, "metrics": bool}
Retarget: as Unity's Humanoid with Latch's shared neutral reference. Each clip's zero is its own Mixamo T-pose (the FBX bind pose) with neutral legs
(UpLeg, Leg straight down, Foot forward at its own pitch, toe level), never its frame 0; each figure's zero is its rest (the reference T-pose with neutral
legs, built that way). A bone's world turn away from the clip's zero is applied to the figure's rest; the hips move by the clip's hips x the hip-height ratio.
Measured per sampled frame (fit.json thresholds): edge stretch and compression (p99, max), the worst triangles (area ratio, flips), interpenetration
(covered body vertices that cross their garment's inner wall), self-intersection at the hems, arm-torso and thigh-thigh intersections, prop-body
intersections. Writes <out>/<name>.metrics.json and frames under <out>/<name>/."""
import os, sys, json, math, time, glob, importlib.util
import numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
L = importlib.util.module_from_spec(s := importlib.util.spec_from_file_location('fitlib', os.path.join(HERE, 'lib.py'))); s.loader.exec_module(L)
sys.path.insert(0, HERE); import look
import bpy
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree

SPEC = json.load(open(sys.argv[sys.argv.index('--') + 1])); TH = L.D['thresholds']; RD = L.D['render']
OUT = L.ensure(SPEC['out']); NAME = SPEC['name']; FR_DIR = L.ensure(os.path.join(OUT, NAME))


def bare(n): return n.split(':')[-1]


# ------------------------------------------------------------------ figures
def load_fbx_figure(f):
    """an installed figure (unity/.../Figures/<id>/<id>.fbx, read only): its meshes tagged as body by LOD suffix, a matte material per slot from its own
    albedo, and the Humanoid zero computed by the same rule the new figures carry (lib.standard_aims on a copy: reference T-pose, neutral legs)."""
    before = set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=f['fbx']); objs = [o for o in bpy.data.objects if o not in before]
    arm = next(o for o in objs if o.type == 'ARMATURE')
    if arm.animation_data: arm.animation_data_clear()
    tex = os.path.join(os.path.dirname(f['fbx']), 'Textures')
    for o in objs:
        if o.type != 'MESH': continue
        n = o.name.rsplit('LOD', 1); o['fit_lod'] = int(n[1][:1]) if len(n) == 2 and n[1][:1].isdigit() else 0; o['fit_role'] = 'body'
        for i, slot in enumerate(o.material_slots):
            nm = slot.material.name.split('.')[0] if slot.material else ''
            t = sorted(glob.glob(os.path.join(tex, nm + '_albedo.*')))
            o.material_slots[i].material = L.simple_material(nm + '_view', bpy.data.images.load(t[0]) if t else None)
    a2 = arm.copy(); a2.data = arm.data.copy(); bpy.context.scene.collection.objects.link(a2)
    # limbs only: an installed rig's spine, neck and head already stand upright at rest, and its head bone need not point along the Mixamo head's
    # direction (aiming it tipped today's Fixer's head back); arms, hands, fingers and legs take the reference T-pose and neutral legs
    aims = {k: v for k, v in L.standard_aims(a2).items() if not (k.startswith('Spine') or k in ('Neck', 'Head', 'Hips'))}
    L.pose_blender(a2, aims); L.bp.ctx(a2, 'POSE'); bpy.ops.pose.armature_apply(selected=False); L.bp.ctx(a2, 'OBJECT')
    arm['fit_zero'] = json.dumps({b.name: [list(r) for r in (arm.matrix_world.inverted() @ a2.matrix_world @ b.matrix_local)] for b in a2.data.bones})
    bpy.data.objects.remove(a2, do_unlink=True)
    for o in objs: bpy.context.scene.collection.objects.unlink(o)
    return objs


class Fig:
    def __init__(self, f):
        if 'fbx' in f: objs = load_fbx_figure(f)
        else:
            with bpy.data.libraries.load(f['blend'], link=False) as (src, dst): dst.objects = list(src.objects)
            objs = [o for o in dst.objects if o is not None]
        col = bpy.data.collections.new(f['label']); bpy.context.scene.collection.children.link(col)
        for o in objs: col.objects.link(o)
        self.arm = next(o for o in objs if o.type == 'ARMATURE'); self.label = f['label']; self.layer = f.get('layer', False)
        self.arm.location.x += f.get('x', 0.0)
        self.meshes = {}
        for o in objs:
            if o.type != 'MESH': continue
            r = o.get('fit_role', 'body'); lod = o.get('fit_lod', 0)
            if lod != SPEC.get('lod', 0) and r != 'donor': o.hide_render = True; o.hide_viewport = True; continue
            if r in ('donor', 'prop'): o.hide_render = True          # props are measured, never in the beauty renders (Sky, 2026-10-27)
            self.meshes.setdefault(r, []).append(o)
        for pb in self.arm.pose.bones: pb.rotation_mode = 'QUATERNION'
        self.weight = float(self.arm.get('fit_weight', 0.5))
        self.cape = sorted([b.name for b in self.arm.data.bones if b.name.startswith('Cape_')])

    def objs(self, role): return self.meshes.get(role, [])


def co(o):
    dg = bpy.context.evaluated_depsgraph_get(); ev = o.evaluated_get(dg); me = ev.to_mesh()
    a = np.empty(len(me.vertices) * 3); me.vertices.foreach_get('co', a); ev.to_mesh_clear()
    M = np.array(o.matrix_world); return a.reshape(-1, 3) @ M[:3, :3].T + M[:3, 3]


# ------------------------------------------------------------------ the clip, with its neutral zero
class Clip:
    def __init__(self, path):
        before = set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=path)
        new = [o for o in bpy.data.objects if o not in before]
        self.arm = next(o for o in new if o.type == 'ARMATURE')
        for o in new:
            if o.type == 'MESH': bpy.data.objects.remove(o, do_unlink=True)
        self.arm.hide_render = True; self.arm.hide_viewport = False
        act = self.arm.animation_data.action; self.f0, self.f1 = int(act.frame_range[0]), int(act.frame_range[1])
        CW = self.arm.matrix_world; self.map = {bare(b.name): b.name for b in self.arm.data.bones}
        self.rest = {n: CW @ self.arm.data.bones[full].matrix_local for n, full in self.map.items()}
        # neutral legs on the clip's zero (Latch's rule): turn each leg bone's rest to the neutral direction, the turn carried down the leg
        for s in ('Left', 'Right'):
            carry = Matrix.Identity(3)
            for b in ('UpLeg', 'Leg', 'Foot', 'ToeBase'):
                n = s + b; M = self.rest[n]; R = carry @ M.to_3x3(); hd = M.translation
                bone = self.arm.data.bones[self.map[n]]; tl = (CW @ bone.tail_local) - (CW @ bone.head_local); cur = (carry @ tl).normalized()
                if b in ('UpLeg', 'Leg'): want = Vector((0, 0, -1))
                elif b == 'Foot': pitch = math.atan2(-cur.z, math.hypot(cur.x, cur.y)); want = Vector((0, -math.cos(pitch), -math.sin(pitch)))
                else: want = Vector((0, -1, 0))
                q = cur.rotation_difference(want).to_matrix(); R = q @ R; carry = q @ carry
                self.rest[n] = Matrix.Translation(hd) @ R.to_4x4()

    def world(self, n):
        return self.arm.matrix_world @ self.arm.pose.bones[self.map[n]].matrix

    def remove(self):
        a = self.arm.animation_data.action; bpy.data.objects.remove(self.arm, do_unlink=True)


def retarget(fig, clip):
    arm = fig.arm; FW = arm.matrix_world; FWi = FW.inverted(); fb = arm.data.bones
    def depth(b): return 0 if b.parent is None else 1 + depth(b.parent)
    order = [b.name for b in sorted(fb, key=depth) if b.name in clip.map]
    fr = {n: FW @ fb[n].matrix_local for n in order}; fa = {n: fb[n].matrix_local for n in order}
    zero = json.loads(arm['fit_zero']) if 'fit_zero' in arm else None        # the Humanoid zero when the rest is the source pose (e2e)
    fz = {n: (FW @ Matrix(zero[n])) if zero and n in zero else fr[n] for n in order}
    ratio = fr['Hips'].translation.z / max(clip.rest['Hips'].translation.z, 1e-6)
    pose = {}
    for n in order:
        cp = clip.world(n); D3 = cp.to_3x3() @ clip.rest[n].to_3x3().inverted()
        rot_a = FWi.to_3x3() @ (D3 @ fz[n].to_3x3()); par = fb[n].parent
        if par is None or par.name not in pose:
            dw = cp.translation - clip.rest[n].translation; head_a = FWi @ (fr[n].translation + dw * ratio)
            M = Matrix.Translation(head_a) @ rot_a.to_4x4(); basis = fa[n].inverted() @ M
        else:
            rel = fa[par.name].inverted() @ fa[n]; head_a = (pose[par.name] @ rel).translation
            M = Matrix.Translation(head_a) @ rot_a.to_4x4(); basis = rel.inverted() @ pose[par.name].inverted() @ M
        pose[n] = M; pb = arm.pose.bones[n]; loc, q, _ = basis.decompose(); pb.location = loc; pb.rotation_quaternion = q


def layer(fig):
    """the arm-out / leg-out layer, driven by the figure's weight slider (fit.json layers)."""
    k = max(0.0, fig.weight - 0.5); Ly = L.D['layers']
    if k <= 0: return
    bpy.context.view_layer.update(); arm = fig.arm
    hips = arm.matrix_world @ arm.pose.bones['Hips'].matrix
    fwd = (hips.to_3x3() @ Vector((0, 0, 1))).normalized()   # the Hips bone's z is its forward in this rig? use the hips facing: -Y of the hips' frame
    lr = (arm.matrix_world @ arm.pose.bones['LeftUpLeg'].head) - (arm.matrix_world @ arm.pose.bones['RightUpLeg'].head); lr.z = 0
    fwd = lr.cross(Vector((0, 0, 1))).normalized()          # forward = (left - right) x up (the figures face -Y)
    for b, deg in (('Arm', Ly['armOutDegPerWeight']), ('UpLeg', Ly['legOutDegPerWeight'])):
        for s, sg in (('Left', 1), ('Right', -1)):
            pb = arm.pose.bones[s + b]; hd = pb.head.copy()
            R = Matrix.Rotation(math.radians(deg * k) * sg, 4, (arm.matrix_world.inverted().to_3x3() @ fwd))
            pb.matrix = Matrix.Translation(hd) @ R @ Matrix.Translation(-hd) @ pb.matrix; bpy.context.view_layer.update()


class CapeSim:
    """the cape's shared chain: each link lags its parent's carried direction and is pulled down; it may not swing in front of the back plane."""
    def __init__(self, fig):
        self.fig = fig; self.prev = {}

    def step(self):
        arm = self.fig.arm; Ly = L.D['garments']['cape']; bpy.context.view_layer.update()
        for n in self.fig.cape:
            pb = arm.pose.bones[n]; hd = pb.head.copy(); cur = (pb.tail - pb.head).normalized()
            want = cur
            if n in self.prev: want = (cur * (1 - Ly['lag']) + self.prev[n] * Ly['lag'])
            want = (want + Vector((0, 0, -1)) * Ly['gravity']).normalized()
            sp = arm.pose.bones['Spine2'].matrix.to_3x3(); back = -(sp @ Vector((0, 0, 1))).normalized()   # behind the chest
            lr = arm.pose.bones['LeftShoulder'].head - arm.pose.bones['RightShoulder'].head
            back = lr.cross(Vector((0, 0, 1))).normalized() * -1 if lr.length > 0 else Vector((0, 1, 0))
            if want.dot(back) < 0.05: want = (want - back * (want.dot(back) - 0.05)).normalized()
            q = cur.rotation_difference(want).to_matrix().to_4x4()
            pb.matrix = Matrix.Translation(hd) @ q @ Matrix.Translation(-hd) @ pb.matrix; bpy.context.view_layer.update()
            self.prev[n] = (pb.tail - pb.head).normalized()


# ------------------------------------------------------------------ metrics
def tri_data(o):
    T = L.tris_of(o); E = np.unique(np.sort(np.concatenate([T[:, [0, 1]], T[:, [1, 2]], T[:, [2, 0]]]), 1), axis=0)
    return T, E


def dom_bone(o, T):
    names = [g.name for g in o.vertex_groups]; W = L.weights_of(o, names) if names else np.zeros((len(o.data.vertices), 1))
    tw = W[T].sum(1); return np.array(names)[np.argmax(tw, 1)] if names else np.array(['Hips'] * len(T)), names, W


def areas(V, T): return np.linalg.norm(np.cross(V[T[:, 1]] - V[T[:, 0]], V[T[:, 2]] - V[T[:, 0]]), axis=1) / 2


def tnormals(V, T):
    n = np.cross(V[T[:, 1]] - V[T[:, 0]], V[T[:, 2]] - V[T[:, 0]]); return n / np.maximum(np.linalg.norm(n, axis=1, keepdims=True), 1e-12)


def overlap_pairs(Va, Ta, Vb, Tb, same=False):
    if len(Ta) == 0 or len(Tb) == 0: return 0
    A = BVHTree.FromPolygons([Vector(v) for v in Va], [list(t) for t in Ta], all_triangles=True)
    B = A if same and Va is Vb else BVHTree.FromPolygons([Vector(v) for v in Vb], [list(t) for t in Tb], all_triangles=True)
    pairs = A.overlap(B); n = 0
    for i, j in pairs:
        if same and (i == j or set(Ta[i]) & set(Tb[j])): continue
        n += 1
    return n // (2 if same else 1)


class Meter:
    """per-figure rest data and per-frame measures."""
    def __init__(self, fig):
        self.fig = fig; self.parts = {}
        bpy.context.view_layer.update()
        for role, objs in fig.meshes.items():
            for o in objs:
                if role == 'donor' and not SPEC.get('metrics', True): continue
                T, E = tri_data(o); V = co(o); dom, names, W = dom_bone(o, T)
                ix = {n: i for i, n in enumerate(names)}
                armw = np.zeros(len(V)); torw = np.zeros(len(V))
                for n in ('LeftArm', 'RightArm'):
                    if n in ix: armw = armw + W[:, ix[n]]
                for n in ('Spine1', 'Spine2', 'LeftShoulder', 'RightShoulder'):
                    if n in ix: torw = torw + W[:, ix[n]]
                pit = (armw > 0.1) & (armw < 0.9) & (torw > 0.1)
                self.parts[o.name] = {'pitE': pit[E].all(1), 'pitT': pit[T].all(1),'role': role, 'o': o, 'T': T, 'E': E, 'V0': V, 'L0': np.linalg.norm(V[E[:, 0]] - V[E[:, 1]], axis=1),
                                      'A0': areas(V, T), 'N0': tnormals(V, T), 'dom': dom, 'kind': o.get('fit_kind', role)}
        self.setup_cover(); self.setup_regions()

    def setup_regions(self):
        """fit.json measureRegions on the body layer: the skirt band, its leg-to-leg bridge edges, the ponytail."""
        R = L.D['measureRegions']; self.reg = None; bn = self.body_part()
        if not bn: return
        p = self.parts[bn]; V = p['V0']; E = p['E']; T = p['T']; arm = self.fig.arm; Mw = arm.matrix_world; Bb = arm.data.bones
        H = float(V[:, 2].max() - V[:, 2].min()); z0 = float(V[:, 2].min())
        hip = float(np.mean([(Mw @ Bb[s + 'UpLeg'].head_local).z for s in ('Left', 'Right')]))
        sk = (V[:, 2] > z0 + R['skirt']['zLoFrac'] * H) & (V[:, 2] < hip)
        o = p['o']; names = [g.name for g in o.vertex_groups]; W = L.weights_of(o, names); dv = np.array(names)[np.argmax(W, 1)]
        side = np.where(np.isin(dv, ['LeftUpLeg', 'LeftLeg', 'LeftFoot']), 1, np.where(np.isin(dv, ['RightUpLeg', 'RightLeg', 'RightFoot']), -1, 0))
        bridge = sk[E].all(1) & (side[E[:, 0]] * side[E[:, 1]] == -1)
        pony = np.zeros(len(V), bool)
        dn = next((n for n, q in self.parts.items() if q['role'] == 'donor'), None)
        if dn:
            D = self.parts[dn]; bvh = BVHTree.FromPolygons([Vector(v) for v in D['V0']], [list(t) for t in D['T']], all_triangles=True)
            dist = np.array([bvh.find_nearest(Vector(v))[3] or 0 for v in V])
            sp = Mw @ Bb['Spine2'].head_local; nz = (Mw @ Bb['Neck'].head_local).z; wz = (Mw @ Bb['Spine1'].head_local).z
            pr = R['ponytail']; pony = (dist > pr['looseM']) & (V[:, 1] > sp.y + pr['behindM']) & (V[:, 2] > wz) & (V[:, 2] < nz + pr['aboveNeckM'])
        self.reg = {'part': bn, 'skE': sk[E].all(1), 'skT': sk[T].all(1), 'bridge': bridge, 'ponyE': pony[E].all(1), 'ponyT': pony[T].all(1),
                    'counts': {'skirtVerts': int(sk.sum()), 'bridgeEdges': int(bridge.sum()), 'ponytailVerts': int(pony.sum())}}

    def regions(self, Vt):
        if not self.reg: return {}
        r = self.reg; p = self.parts[r['part']]; V = Vt[r['part']]
        Lr = np.linalg.norm(V[p['E'][:, 0]] - V[p['E'][:, 1]], axis=1) / np.maximum(p['L0'], 1e-9); ok = p['L0'] > 0.002
        Nn = tnormals(V, p['T']); out = {}
        def st(m, k):
            m = m & ok
            if m.any(): out[k + 'StretchP99'] = float(np.percentile(Lr[m], 99) - 1); out[k + 'StretchMax'] = float(Lr[m].max() - 1); out[k + 'CompressP99'] = float(1 - np.percentile(Lr[m], 1))
            else: out[k + 'StretchP99'] = out[k + 'StretchMax'] = out[k + 'CompressP99'] = 0.0
        st(r['skE'], 'skirt'); st(r['bridge'], 'skirtBridge'); st(r['ponyE'], 'pony')
        A = areas(V, p['T']) / np.maximum(p['A0'], 1e-12); big = p['A0'] > 1e-6
        out['skirtTriMin'] = float(A[r['skT'] & big].min()) if (r['skT'] & big).any() else 1.0
        out['ponyBodyPairs'] = overlap_pairs(V, p['T'][r['ponyT']], V, p['T'][~r['ponyT']]) if r['ponyT'].any() else 0
        return out

    def body_part(self):
        for k in ('suit', 'body'):
            for n, p in self.parts.items():
                if p['role'] == k: return n
        return None

    def setup_cover(self):
        """covered body vertices per covering surface (A: the donor under the source mesh; G: the suit under each garment); at rest each covered vertex
        is behind its cover's inner wall; a frame counts the ones that crossed it."""
        self.cover = []
        parts = self.parts
        if any(p['role'] == 'donor' for p in parts.values()):
            dn = next(n for n, p in parts.items() if p['role'] == 'donor'); bn = next(n for n, p in parts.items() if p['role'] == 'body')
            self.cover.append({'kind': 'body', 'under': dn, 'over': bn, 'closed': True, 'region': None})
        sn = next((n for n, p in parts.items() if p['role'] == 'suit'), None)
        if sn:
            for n, p in parts.items():
                if p['role'] == 'garment': self.cover.append({'kind': p['kind'], 'under': sn, 'over': n, 'closed': False})
        for c in self.cover:
            U = parts[c['under']]; O = parts[c['over']]
            bvh = BVHTree.FromPolygons([Vector(v) for v in O['V0']], [list(t) for t in O['T']], all_triangles=True)
            sel = []; sides = []
            for i, p in enumerate(U['V0']):
                loc, nrm, fi, d = bvh.find_nearest(Vector(p))
                if loc is None or d > 0.08: continue
                s = (Vector(p) - loc).dot(nrm)
                if c['closed']:
                    if s < -0.001: sel.append(i)              # inside the source mesh
                else:
                    sel.append(i); sides.append(1.0 if s >= 0 else -1.0)
            c['sel'] = np.array(sel, int)
            if not c['closed']:
                # the garment's inner wall = the faces whose normal faces the covered vertices at rest
                Vc = O['V0']; T = O['T']; N = O['N0']; cen = Vc[T].mean(1)
                ub = BVHTree.FromPolygons([Vector(v) for v in U['V0'][c['sel']]], [[i, i, i] for i in range(0)] or [[0, 0, 0]], all_triangles=True) if False else None
                body_c = U['V0'][c['sel']]
                inner = np.zeros(len(T), bool)
                if len(body_c):
                    kb = BVHTree.FromPolygons([Vector(v) for v in U['V0']], [list(t) for t in U['T']], all_triangles=True)
                    for fi in range(len(T)):
                        loc, nrm, _, d = kb.find_nearest(Vector(cen[fi]))
                        if loc is not None: inner[fi] = (loc - Vector(cen[fi])).dot(Vector(N[fi])) > 0
                c['inner'] = inner
                Ti = T[inner]; ok = np.ones(len(c['sel']), bool)
                if len(Ti):
                    ib = BVHTree.FromPolygons([Vector(v) for v in Vc], [list(t) for t in Ti], all_triangles=True)
                    for k2, i in enumerate(c['sel']):
                        loc, nrm, fi, d = ib.find_nearest(Vector(U['V0'][i]))
                        if loc is not None and d < 0.15 and (Vector(U['V0'][i]) - loc).dot(nrm) < -0.003: ok[k2] = False
                c['okRest'] = ok; c['crossedAtRest'] = int((~ok).sum())
            c['n'] = int(len(c['sel']))

    def frame(self, layer_on=False):
        out = {}
        Vt = {n: co(p['o']) for n, p in self.parts.items()}
        worst = []
        st_p99 = cp_p99 = st_max = cp_max = 0.0; flips = 0; amin = 9; amax = 0; pit_st = pit_cp = 0.0; pit_fl = 0; pit_n = 0
        for n, p in self.parts.items():
            if p['role'] in ('donor',): continue
            V = Vt[n]; Lr = np.linalg.norm(V[p['E'][:, 0]] - V[p['E'][:, 1]], axis=1) / np.maximum(p['L0'], 1e-9)
            ok = p['L0'] > 0.002
            if p['role'] == 'prop' or p['role'] == 'hatrigid': continue
            r = Lr[ok]
            if len(r):
                st_p99 = max(st_p99, float(np.percentile(r, 99) - 1)); cp_p99 = max(cp_p99, float(1 - np.percentile(r, 1)))
                st_max = max(st_max, float(r.max() - 1)); cp_max = max(cp_max, float(1 - r.min()))
            A = areas(V, p['T']) / np.maximum(p['A0'], 1e-12); big = p['A0'] > 1e-6
            Nn = tnormals(V, p['T'])
            # flip: the posed normal against the rest normal carried by the triangle's dominant bone
            R = {}
            for b in set(p['dom'].tolist()):
                if b in self.fig.arm.pose.bones:
                    pb = self.fig.arm.pose.bones[b]; M = (pb.matrix @ self.fig.arm.data.bones[b].matrix_local.inverted()).to_3x3(); R[b] = np.array(M)
            Nr = np.array([R.get(b, np.eye(3)) @ nn for b, nn in zip(p['dom'], p['N0'])])
            fl = ((Nn * Nr).sum(1) < -0.2) & big; flips += int(fl.sum())
            if p['role'] in ('body', 'suit'):
                pe = p['pitE'] & ok
                if pe.any():
                    pit_st = max(pit_st, float(np.percentile(Lr[pe], 99) - 1)); pit_cp = max(pit_cp, float(1 - np.percentile(Lr[pe], 1)))
                pit_fl += int((fl & p['pitT']).sum()); pit_n = max(pit_n, int(pe.sum()))
            if big.any():
                amin = min(amin, float(A[big].min())); amax = max(amax, float(A[big].max()))
                for fi in np.argsort(A[big])[:3]:
                    idx = np.nonzero(big)[0][fi]; worst.append((float(A[idx]), n, int(idx), str(p['dom'][idx])))
                for fi in np.argsort(-A[big])[:2]:
                    idx = np.nonzero(big)[0][fi]; worst.append((float(A[idx]), n, int(idx), str(p['dom'][idx])))
        out.update({'stretchP99': st_p99, 'compressP99': cp_p99, 'stretchMax': st_max, 'compressMax': cp_max, 'triAreaMin': amin, 'triAreaMax': amax, 'flipped': flips,
                    'armpitStretchP99': pit_st, 'armpitCompressP99': pit_cp, 'armpitFlipped': pit_fl, 'armpitEdges': pit_n, 'worst': sorted(worst, key=lambda w: min(w[0], 1 / max(w[0], 1e-9)))[:5]})
        # interpenetration
        pen = {}
        for c in self.cover:
            if c['n'] == 0: continue
            U = Vt[c['under']][c['sel']]; O = Vt[c['over']]; T = self.parts[c['over']]['T']
            if c['closed']:
                bvh = BVHTree.FromPolygons([Vector(v) for v in O], [list(t) for t in T], all_triangles=True); k = 0
                for pnt in U:
                    loc, nrm, fi, d = bvh.find_nearest(Vector(pnt))
                    if loc is not None and (Vector(pnt) - loc).dot(nrm) > 0.003: k += 1
            else:
                Ti = T[c['inner']]
                if len(Ti) == 0: continue
                bvh = BVHTree.FromPolygons([Vector(v) for v in O], [list(t) for t in Ti], all_triangles=True); k = 0
                for pnt in U[c['okRest']] if 'okRest' in c else U:
                    loc, nrm, fi, d = bvh.find_nearest(Vector(pnt))
                    # the inner wall's normal points at the body: a body vertex on its far side has crossed it
                    if loc is not None and d < 0.15 and (Vector(pnt) - loc).dot(nrm) < -0.003: k += 1
            pen[c['kind']] = round(100.0 * k / max(1, int(c['okRest'].sum()) if 'okRest' in c else c['n']), 2)
        out['penetrationPct'] = pen
        # hems, arm-torso, thighs, props
        bn = self.body_part()
        hem = 0; at = 0; th = 0; prop = 0
        for n, p in self.parts.items():
            if p['role'] in ('garment', 'body') and p.get('hem') is not None and len(p['hem']):
                V = Vt[n]; hem += overlap_hem(V, p['T'][p['hem']], p['T'])
        if bn:
            p = self.parts[bn]; V = Vt[bn]; dom = p['dom']
            arm_m = np.isin(dom, ['LeftArm', 'RightArm', 'LeftForeArm', 'RightForeArm']); tor = np.isin(dom, ['Spine', 'Spine1', 'Spine2', 'Hips'])
            at = overlap_pairs(V, p['T'][arm_m], V, p['T'][tor])
            th = overlap_pairs(V, p['T'][dom == 'LeftUpLeg'], V, p['T'][dom == 'RightUpLeg'])
            for n2, p2 in self.parts.items():
                if p2['role'] == 'prop': prop += overlap_pairs(Vt[n2], p2['T'], V, p['T'])
        out.update({'hemSelfPairs': hem, 'armTorsoPairs': at, 'thighPairs': th, 'propBodyPairs': prop})
        out.update(self.regions(Vt))
        return out


def hem_sets(meter):
    """hem triangles: a garment's lowest band (A: the loose skirt/coat region's lowest band), excluding pairs that share a vertex."""
    for n, p in meter.parts.items():
        p['hem'] = None
        if p['role'] == 'garment' and p['kind'] in ('skirt', 'coat', 'cape'):
            z = p['V0'][p['T']].mean(1)[:, 2]; p['hem'] = np.nonzero(z < z.min() + 0.06)[0]
        if p['role'] == 'body':
            lab_path = p['o'].get('fit_labels')
            if lab_path and os.path.exists(lab_path):
                lab = np.load(lab_path); tl = lab[p['T'][:, 0]]
                m = np.isin(tl, ['skirt', 'coat', 'cape'])
                if m.any():
                    z = p['V0'][p['T']].mean(1)[:, 2]; zmin = z[m].min(); p['hem'] = np.nonzero(m & (z < zmin + 0.06))[0]


def selfpairs_fix(meter):
    # pairs between hem tris and the same mesh share vertices with their neighbours: exclude by vertex sharing
    pass


def overlap_hem(V, Th, T):
    A = BVHTree.FromPolygons([Vector(v) for v in V], [list(t) for t in Th], all_triangles=True)
    B = BVHTree.FromPolygons([Vector(v) for v in V], [list(t) for t in T], all_triangles=True)
    n = 0
    for i, j in A.overlap(B):
        if set(Th[i]) & set(T[j]): continue
        n += 1
    return n


# ------------------------------------------------------------------ render
def setup_render():
    sc = bpy.context.scene; g = look.studio(sc, RD['close']['w'] * max(1, len(SPEC['figures'])) // (2 if len(SPEC['figures']) > 1 else 1), RD['close']['h'])
    cam = look.camera(sc); cam.data.sensor_fit = 'VERTICAL'; cam.data.angle_y = math.radians(RD['close']['fovDeg'])
    sc.render.resolution_x = int(RD['close']['w'] * (1 if len(SPEC['figures']) == 1 else 0.62 * len(SPEC['figures']))); sc.render.resolution_y = RD['close']['h']
    return sc, cam


def main():
    t0 = time.time()
    bpy.ops.wm.read_factory_settings(use_empty=True)
    figs = [Fig(f) for f in SPEC['figures']]
    sc, cam = setup_render()
    meters = [Meter(f) for f in figs] if SPEC.get('metrics', True) else []
    for m in meters: hem_sets(m)
    rest_rows = {f.label: dict(m.frame(), frame=0) for f, m in zip(figs, meters)}
    clips = SPEC.get('clips') or list(L.D['clips']['list'])
    nfr = SPEC.get('frames', L.D['clips']['frames'])
    res = {f.label: {} for f in figs}; res_layer = {f.label: {} for f in figs}
    span = max(f.arm.location.x for f in figs) - min(f.arm.location.x for f in figs)
    centre = sum((f.arm.location.x for f in figs), 0.0) / len(figs)
    dist = RD['close']['distM'] + span * 1.1
    vid = 0
    for ci, cname in enumerate(clips):
        clip = Clip(os.path.join(os.path.expanduser(L.D['clips']['lib']), L.D['clips']['list'][cname]))
        frames = sorted(set(int(round(x)) for x in np.linspace(clip.f0, clip.f1, nfr)))
        sims = [CapeSim(f) for f in figs]
        per = {f.label: [] for f in figs}; perl = {f.label: [] for f in figs}
        for fr in range(clip.f0, clip.f1 + 1):
            sc.frame_set(fr)
            for f, sim in zip(figs, sims):
                retarget(f, clip); bpy.context.view_layer.update()
                if f.layer: layer(f)
                if f.cape: sim.step()
            if fr not in frames: continue
            bpy.context.view_layer.update()
            for f, m in zip(figs, meters):
                r = m.frame(); r['frame'] = fr
                for n, p in m.parts.items():
                    if p.get('hem') is not None and len(p['hem']): pass
                per[f.label].append(r)
            if SPEC.get('close', True):
                k = frames.index(fr); a = math.radians(RD['close']['orbitFromDeg'] + RD['close']['orbitDeg'] * (ci + k / max(1, len(frames) - 1)) / max(1, len(clips)))
                eye = (centre + dist * math.sin(a), -dist * math.cos(a), RD['close']['heightM'])
                look.look_at(cam, eye, (centre, 0, 0.9)); cam.data.type = 'PERSP'; cam.data.sensor_fit = 'VERTICAL'; cam.data.angle_y = math.radians(RD['close']['fovDeg'])
                sc.render.resolution_x = int(RD['close']['w'] * (1 if len(figs) == 1 else 0.62 * len(figs)))
                sc.render.resolution_y = RD['close']['h']
                sc.render.filepath = os.path.join(FR_DIR, f'close_{vid:04d}.png'); bpy.ops.render.render(write_still=True); vid += 1
            if SPEC.get('play', True) and k_in(frames, fr, 4):
                P = RD['play']; look.play_cam(cam, (centre, 0, 0.85), P['yawDeg'], P['pitchDeg'], P['ppm'], P['h'])
                sc.render.resolution_x = int(P['w'] * max(1, len(figs)) / 1.5) if len(figs) > 1 else P['w']; sc.render.resolution_y = P['h']
                sc.render.filepath = os.path.join(FR_DIR, f'play_{cname}_{frames.index(fr):02d}.png'); bpy.ops.render.render(write_still=True)
                cam.data.type = 'PERSP'
        for f in figs: res[f.label][cname] = per[f.label]
        clip.remove()
        L.log(NAME, cname, 'frames', len(frames), round(time.time() - t0, 1))
    summary = {}
    for f in figs:
        S = {}
        for cname, rows in res[f.label].items():
            if not rows: continue
            agg = {}
            for k in ('stretchP99', 'compressP99', 'stretchMax', 'compressMax', 'triAreaMax', 'flipped', 'hemSelfPairs', 'armTorsoPairs', 'thighPairs', 'propBodyPairs', 'armpitStretchP99', 'armpitCompressP99', 'armpitFlipped',
                      'skirtStretchP99', 'skirtStretchMax', 'skirtCompressP99', 'skirtBridgeStretchP99', 'skirtBridgeStretchMax', 'ponyStretchP99', 'ponyStretchMax', 'ponyBodyPairs'):
                if k not in rows[0]: continue
                v = [r[k] for r in rows]; i = int(np.argmax(v)); agg[k] = {'max': round(float(v[i]), 3), 'frame': rows[i]['frame']}
            v = [r['triAreaMin'] for r in rows]; i = int(np.argmin(v)); agg['triAreaMin'] = {'min': round(float(v[i]), 3), 'frame': rows[i]['frame']}
            kinds = sorted(set(k for r in rows for k in r['penetrationPct']))
            agg['penetrationPct'] = {kd: {'max': max(r['penetrationPct'].get(kd, 0) for r in rows), 'mean': round(float(np.mean([r['penetrationPct'].get(kd, 0) for r in rows])), 2)} for kd in kinds}
            agg['worst'] = sorted([w + [r['frame']] if isinstance(w, list) else list(w) + [r['frame']] for r in rows for w in r['worst']], key=lambda w: min(w[0], 1 / max(w[0], 1e-9)))[:5]
            agg['flags'] = flags(agg)
            S[cname] = agg
        summary[f.label] = S
    for f in figs:
        if f.label in rest_rows:
            r = rest_rows[f.label]; summary[f.label]['rest'] = {k: r[k] for k in ('penetrationPct', 'hemSelfPairs', 'armTorsoPairs', 'thighPairs', 'propBodyPairs', 'flipped', 'ponyBodyPairs') if k in r}
            m = next(mm for ff, mm in zip(figs, meters) if ff is f)
            if m.reg: summary[f.label]['regions'] = m.reg['counts']
    json.dump({'name': NAME, 'figures': [x['label'] for x in SPEC['figures']], 'clips': clips, 'summary': summary,
               'frames': {f.label: res[f.label] for f in figs}, 'seconds': round(time.time() - t0, 1)}, open(os.path.join(OUT, f'{NAME}.metrics.json'), 'w'), indent=1, default=str)
    L.log('DONE', NAME, round(time.time() - t0, 1))


def k_in(frames, fr, n):
    idx = frames.index(fr); pick = set(int(round(x)) for x in np.linspace(0, len(frames) - 1, n)); return idx in pick


def flags(agg):
    f = []
    if agg['stretchP99']['max'] > TH['edgeStretchP99']: f.append(f"edge stretch p99 {agg['stretchP99']['max']:.2f} > {TH['edgeStretchP99']}")
    if agg['compressP99']['max'] > TH['edgeCompressP99']: f.append(f"edge compression p99 {agg['compressP99']['max']:.2f} > {TH['edgeCompressP99']}")
    if agg['stretchMax']['max'] > TH['edgeStretchMax']: f.append(f"an edge stretched {agg['stretchMax']['max']:.2f} > {TH['edgeStretchMax']}")
    if agg['compressMax']['max'] > TH['edgeCompressMax']: f.append(f"an edge compressed {agg['compressMax']['max']:.2f} > {TH['edgeCompressMax']}")
    if agg['triAreaMin']['min'] < TH['triAreaMin']: f.append(f"a triangle shrank to {agg['triAreaMin']['min']:.2f} of its area")
    if agg['triAreaMax']['max'] > TH['triAreaMax']: f.append(f"a triangle grew {agg['triAreaMax']['max']:.1f}x")
    if agg['flipped']['max'] > TH['triFlipped']: f.append(f"{agg['flipped']['max']:.0f} triangles flipped")
    for kd, v in agg['penetrationPct'].items():
        if v['max'] > TH['penetrationPct']: f.append(f"{kd}: {v['max']:.1f} % of covered body vertices poke through")
    if agg['hemSelfPairs']['max'] > TH['hemSelfPairs']: f.append(f"hem self-intersection {agg['hemSelfPairs']['max']:.0f} pairs")
    if agg['armTorsoPairs']['max'] > TH['armTorsoPairs']: f.append(f"arm-torso {agg['armTorsoPairs']['max']:.0f} pairs")
    if agg['thighPairs']['max'] > TH['thighPairs']: f.append(f"thigh-thigh {agg['thighPairs']['max']:.0f} pairs")
    if agg['propBodyPairs']['max'] > TH['propBodyPairs']: f.append(f"prop-body {agg['propBodyPairs']['max']:.0f} pairs")
    return f


main()
