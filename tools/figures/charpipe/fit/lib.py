"""Brief 069 fit test: shared Blender-side helpers (MPFB bodies, the numpy rig model, signed distance, weights, clips, renders).
Loaded by the stage scripts with importlib; never run on its own. Reuses tools/figures/charpipe/build.py (read only) for the rig prune, aim and FBX export."""
import bpy, bmesh, addon_utils, json, os, sys, math, time, importlib.util
import numpy as np
from mathutils import Vector, Matrix, Quaternion
from mathutils.bvhtree import BVHTree

HERE = os.path.dirname(os.path.abspath(__file__))
CP = os.path.dirname(HERE)
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(CP)))
SCR = os.path.join(ROOT, 'out/scratch/forma/char-pipeline/fit-test')
D = json.load(open(os.path.join(HERE, 'fit.json')))
SET = os.environ.get('FIT_SET', 'fittest')          # 'e2e' / 'fixer': the same stages on an end-to-end set (<set>.json), written to its out folder
if SET in ('e2e', 'fixer', 'enforcer'):                          # end-to-end sets: e2e.json, fixer.json (the same stages, their own people and out folder)
    E2E = json.load(open(os.path.join(HERE, f'{SET}.json')))
    SCR = os.path.join(ROOT, os.environ.get('FIT_OUT', E2E['out']))
    D['inputs'] = [dict(r, src=os.path.join(ROOT, r['src'])) for r in E2E['people']]
MPFB = os.environ.get('MPFBDATA', '')
P = 'mixamorig:'


def _load(name, path):
    sp = importlib.util.spec_from_file_location(name, path); m = importlib.util.module_from_spec(sp); sp.loader.exec_module(m); return m


bp = _load('cp_build', os.path.join(CP, 'build.py'))      # read only: prune, aim, apply_rest, export_fbx, ctx
KEEP = bp.KEEP


def store(p): return p if os.path.isabs(p) else os.path.join(os.path.expanduser(D['store']), p)


def ensure(p): os.makedirs(p, exist_ok=True); return p


def log(*a): print('FIT', *a, flush=True)


class Timer:
    def __init__(self): self.t = {}; self.t0 = time.time()

    def lap(self, k):
        now = time.time(); self.t[k] = round(self.t.get(k, 0) + now - self.t0, 2); self.t0 = now


# ------------------------------------------------------------------ MPFB bodies
def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    addon_utils.enable('bl_ext.user_default.mpfb', default_set=True, persistent=False)


def human(macros, sex, keep_basemesh=True, proxy=True):
    """An MPFB human at `macros` in MPFB's own rest: the 52-bone rig (prefix stripped), the basemesh (shape keys kept, helpers masked) and the LOD0 proxy."""
    from bl_ext.user_default.mpfb.services.humanservice import HumanService
    from bl_ext.user_default.mpfb.services.targetservice import TargetService
    m = TargetService.get_default_macro_info_dict(); m.update(D['basis']['ref']); m.update(macros); m['gender'] = 1.0 if sex == 'm' else 0.0
    b = HumanService.create_human(scale=0.1, macro_detail_dict=m)
    arm = HumanService.add_builtin_rig(b, 'mixamo_unity')
    px = None
    if proxy:
        px = HumanService.add_mhclo_asset(f"{MPFB}/proxymeshes/{D['basis']['proxy'][sex]}/{D['basis']['proxy'][sex]}.proxy", b, asset_type='Proxymeshes', subdiv_levels=0)
        bp.bake_world(px, arm)
    meshes = [x for x in (b, px) if x is not None]
    bp.prune(arm, meshes, KEEP)
    if not keep_basemesh: bpy.data.objects.remove(b, do_unlink=True); b = None
    return arm, b, px


def eval_co(o, armature=True):
    """World vertex positions of o as evaluated (shape keys; the armature modifier on or off; the helper mask always off)."""
    saved = []
    for md in o.modifiers:
        if md.type == 'MASK' or (md.type == 'ARMATURE' and not armature): saved.append((md, md.show_viewport)); md.show_viewport = False
    dg = bpy.context.evaluated_depsgraph_get(); ev = o.evaluated_get(dg); me = ev.to_mesh()
    co = np.empty(len(me.vertices) * 3, np.float64); me.vertices.foreach_get('co', co); co = co.reshape(-1, 3)
    ev.to_mesh_clear()
    for md, s in saved: md.show_viewport = s
    M = np.array(o.matrix_world); return co @ M[:3, :3].T + M[:3, 3]


def faces_of(o):
    return [tuple(p.vertices) for p in o.data.polygons]


def tris_of(o):
    o.data.calc_loop_triangles(); t = np.empty(len(o.data.loop_triangles) * 3, np.int64); o.data.loop_triangles.foreach_get('vertices', t); return t.reshape(-1, 3)


def weights_of(o, names):
    """dense (n verts x len(names)) weights, normalised."""
    idx = {g.index: names.index(g.name) for g in o.vertex_groups if g.name in names}
    W = np.zeros((len(o.data.vertices), len(names)))
    for v in o.data.vertices:
        for g in v.groups:
            j = idx.get(g.group)
            if j is not None: W[v.index, j] += g.weight
    s = W.sum(1, keepdims=True); s[s == 0] = 1; return W / s


def set_weights(o, W, names, maxinf=4):
    for g in list(o.vertex_groups): o.vertex_groups.remove(g)
    gs = [o.vertex_groups.new(name=n) for n in names]
    W = limit(W, maxinf)
    for j, g in enumerate(gs):
        nz = np.nonzero(W[:, j] > 1e-4)[0]
        for i in nz: g.add([int(i)], float(W[i, j]), 'REPLACE')


def limit(W, k):
    W = W.copy()
    if W.shape[1] > k:
        cut = np.argsort(-W, axis=1)[:, k:]; np.put_along_axis(W, cut, 0.0, axis=1)
    W[W < 1e-3] = 0; s = W.sum(1, keepdims=True); s[s == 0] = 1; return W / s


def bones_of(arm, names):
    Mw = arm.matrix_world; B = arm.data.bones
    H = np.array([list(Mw @ B[n].head_local) for n in names]); T = np.array([list(Mw @ B[n].tail_local) for n in names])
    par = [names.index(B[n].parent.name) if B[n].parent else -1 for n in names]
    return H, T, par


# ------------------------------------------------------------------ the numpy rig: world rotations by aim, as build.aim does in Blender
def sa(a, b):
    """shortest-arc rotation matrix taking unit a to unit b."""
    a = a / np.linalg.norm(a); b = b / np.linalg.norm(b); v = np.cross(a, b); c = float(np.dot(a, b))
    if c < -0.999999:
        ax = np.cross(a, [1, 0, 0]);
        if np.linalg.norm(ax) < 1e-6: ax = np.cross(a, [0, 1, 0])
        ax /= np.linalg.norm(ax); return 2 * np.outer(ax, ax) - np.eye(3)
    K = np.array([[0, -v[2], v[1]], [v[2], 0, -v[0]], [-v[1], v[0], 0]]); return np.eye(3) + K + K @ K / (1 + c)


def rotx(a): c, s = math.cos(a), math.sin(a); return np.array([[1, 0, 0], [0, c, -s], [0, s, c]])


def roty(a): c, s = math.cos(a), math.sin(a); return np.array([[c, 0, s], [0, 1, 0], [-s, 0, c]])


def rotaxis(ax, a):
    ax = np.asarray(ax, float); ax = ax / np.linalg.norm(ax); K = np.array([[0, -ax[2], ax[1]], [ax[2], 0, -ax[0]], [-ax[1], ax[0], 0]])
    return np.eye(3) + math.sin(a) * K + (1 - math.cos(a)) * K @ K


class Rig:
    def __init__(self, names, H, T, par):
        self.n = names; self.H = H; self.T = T; self.par = par; self.ix = {k: i for i, k in enumerate(names)}
        depth = []
        for i in range(len(names)):
            d, p = 0, par[i]
            while p >= 0: d += 1; p = par[p]
            depth.append(d)
        self.order = sorted(range(len(names)), key=lambda i: (depth[i], names[i]))

    def pose(self, aims, pre=None):
        """aims: {bone: world direction} (shortest arc from the parent-carried rest direction, as build.aim); pre: {bone: world rotation applied
        before the aim, carried to the children} (spine lean). Bones with neither inherit their parent's rotation. Returns R (n,3,3), posed heads (n,3)."""
        n = len(self.n); R = np.zeros((n, 3, 3)); Hp = np.zeros((n, 3))
        for i in self.order:
            p = self.par[i]; Rp = R[p] if p >= 0 else np.eye(3)
            Hp[i] = self.H[i] if p < 0 else Hp[p] + Rp @ (self.H[i] - self.H[p])
            Ri = Rp
            if pre and self.n[i] in pre: Ri = pre[self.n[i]] @ Ri
            if self.n[i] in aims:
                cur = Ri @ (self.T[i] - self.H[i]); Ri = sa(cur, np.asarray(aims[self.n[i]], float)) @ Ri
            R[i] = Ri
        return R, Hp


def lbs(V, W, R, H, Hp):
    """linear blend skinning: sum_b w_b (R_b (v - h_b) + h'_b)."""
    out = np.zeros_like(V)
    for b in np.nonzero(W.sum(0) > 0)[0]:
        w = W[:, b:b + 1]; out += w * ((V - H[b]) @ R[b].T + Hp[b])
    return out


def lbs_inv(Vp, W, R, H, Hp):
    """inverse LBS per vertex: v = (sum w R)^-1 (v' - sum w (h' - R h))."""
    A = np.einsum('vb,bij->vij', W, R); c = np.einsum('vb,bi->vi', W, Hp - np.einsum('bij,bj->bi', R, H))
    return np.linalg.solve(A, (Vp - c)[..., None])[..., 0]


# ------------------------------------------------------------------ Blender rig posing that matches Rig.pose exactly
def pose_blender(arm, aims, pre=None):
    bp.ctx(arm, 'POSE'); Pb = arm.pose.bones
    for pb in Pb: pb.rotation_mode = 'QUATERNION'; pb.rotation_quaternion = (1, 0, 0, 0); pb.location = (0, 0, 0)
    bpy.context.view_layer.update()
    def depth(b): return 0 if b.parent is None else 1 + depth(b.parent)
    Mi = arm.matrix_world.inverted()
    for pb in sorted(Pb, key=lambda b: (depth(b), b.name)):
        if pre and pb.name in pre:
            head = pb.head.copy(); Rw = Matrix([list(r) for r in pre[pb.name]]).to_4x4()
            Ra = Mi.to_3x3().to_4x4() @ Rw @ arm.matrix_world.to_3x3().to_4x4()
            pb.matrix = Matrix.Translation(head) @ Ra @ Matrix.Translation(-head) @ pb.matrix; bpy.context.view_layer.update()
        if pb.name in aims:
            want = (Mi.to_3x3() @ Vector(aims[pb.name])).normalized(); bp.aim(pb, want)
    bp.ctx(arm, 'OBJECT')


def apply_rest(arm, meshes):
    bp.apply_rest(arm, meshes)


# ------------------------------------------------------------------ signed distance grid of a closed-ish mesh
class SDF:
    """Unsigned distance by BVH nearest on a voxel grid, sign by majority ray parity along x, y and z. Trilinear lookup in numpy."""
    def __init__(self, V, F, h, pad=0.06):
        self.bvh = BVHTree.FromPolygons([Vector(v) for v in V], [list(f) for f in F], all_triangles=False, epsilon=0.0)
        lo = V.min(0) - pad; hi = V.max(0) + pad; self.lo = lo; self.h = h
        self.shape = tuple(int(math.ceil((hi[k] - lo[k]) / h)) + 1 for k in range(3))
        nx, ny, nz = self.shape
        gx = lo[0] + np.arange(nx) * h; gy = lo[1] + np.arange(ny) * h; gz = lo[2] + np.arange(nz) * h
        votes = np.zeros(self.shape, np.int8)
        for axis in range(3):
            votes += self._parity(axis, gx, gy, gz)
        inside = votes >= 2
        dist = np.empty(self.shape)
        fn = self.bvh.find_nearest
        for i in range(nx):
            for j in range(ny):
                for k in range(nz):
                    r = fn(Vector((gx[i], gy[j], gz[k])))
                    dist[i, j, k] = r[3] if r[0] is not None else 1.0
        self.d = np.where(inside, -dist, dist)

    def _parity(self, axis, gx, gy, gz):
        g = [gx, gy, gz]; out = np.zeros(self.shape, np.int8)
        a, b = [k for k in range(3) if k != axis]
        d = Vector([1.0 if k == axis else 0.0 for k in range(3)])
        start = g[axis][0] - 0.01
        for ia, va in enumerate(g[a]):
            for ib, vb in enumerate(g[b]):
                o = [0.0, 0.0, 0.0]; o[a] = va; o[b] = vb; o[axis] = start; o = Vector(o)
                hits = []; cur = o.copy(); guard = 0
                while guard < 64:
                    loc, nrm, idx, dd = self.bvh.ray_cast(cur, d)
                    if loc is None: break
                    hits.append(loc[axis]); cur = loc + d * 1e-5; guard += 1
                if len(hits) < 2: continue
                cells = g[axis]; cnt = np.searchsorted(np.array(hits), cells)     # crossings before each cell
                ins = (cnt % 2 == 1)
                sl = [0, 0, 0]; sl[a] = ia; sl[b] = ib; sl[axis] = slice(None)
                out[tuple(sl)] = ins.astype(np.int8)
        return out

    def __call__(self, X):
        u = (X - self.lo) / self.h; i0 = np.floor(u).astype(int); f = u - i0
        for k in range(3):
            bad = (i0[:, k] < 0) | (i0[:, k] >= self.shape[k] - 1)
            i0[:, k] = np.clip(i0[:, k], 0, self.shape[k] - 2); f[:, k] = np.where(bad, np.clip(f[:, k], 0, 1), f[:, k])
        out = np.zeros(len(X))
        for dx in (0, 1):
            for dy in (0, 1):
                for dz in (0, 1):
                    w = (f[:, 0] if dx else 1 - f[:, 0]) * (f[:, 1] if dy else 1 - f[:, 1]) * (f[:, 2] if dz else 1 - f[:, 2])
                    out += w * self.d[i0[:, 0] + dx, i0[:, 1] + dy, i0[:, 2] + dz]
        far = np.any((u < 0) | (u > np.array(self.shape) - 1), axis=1)
        out[far] = np.maximum(out[far], 0.05)
        return out


# ------------------------------------------------------------------ mesh Laplacian (cotangent, clamped) and CG
def laplacian(V, T):
    """returns (rows, cols, vals) of L (positive semi-definite: L = D - W) and the lumped mass."""
    n = len(V); I = []; J = []; Wv = []
    a, b, c = V[T[:, 0]], V[T[:, 1]], V[T[:, 2]]
    def cot(p, q, r):
        u = q - p; v = r - p; cr = np.linalg.norm(np.cross(u, v), axis=1); return np.einsum('ij,ij->i', u, v) / np.maximum(cr, 1e-12)
    ca = cot(a, b, c); cb = cot(b, c, a); cc = cot(c, a, b)
    for (i, j, w) in ((T[:, 1], T[:, 2], ca), (T[:, 2], T[:, 0], cb), (T[:, 0], T[:, 1], cc)):
        w = np.clip(0.5 * w, 1e-4, 50.0); I += [i, j]; J += [j, i]; Wv += [w, w]
    I = np.concatenate(I); J = np.concatenate(J); Wv = np.concatenate(Wv)
    diag = np.bincount(I, Wv, n)
    area = np.linalg.norm(np.cross(b - a, c - a), axis=1) / 2; M = np.bincount(T.ravel(), np.repeat(area / 3, 3), n)
    return I, J, Wv, diag, M


def lap_mul(I, J, Wv, diag, X):
    out = diag[:, None] * X
    np.subtract.at(out, I, Wv[:, None] * X[J]) if False else None
    acc = np.zeros_like(X)
    for k in range(X.shape[1]): acc[:, k] = np.bincount(I, Wv * X[J, k], len(X))
    return out - acc


def inpaint(V, T, known, Wk, iters=600):
    """minimise the Dirichlet energy of the weights with the known rows fixed (Abdrashitov et al. 2023, eq. 3): solve L_uu X_u = -L_uk X_k by CG."""
    n = len(V); I, J, Wv, diag, M = laplacian(V, T)
    unk = np.nonzero(~known)[0]
    if len(unk) == 0: return Wk
    X = Wk.copy(); X[unk] = 0.0
    def Lmul(Y):
        full = np.zeros((n, Y.shape[1])); full[unk] = Y; return lap_mul(I, J, Wv, diag, full)[unk]
    full = X.copy(); full[unk] = 0; rhs = -lap_mul(I, J, Wv, diag, full)[unk]
    Dg = diag[unk][:, None] + 1e-9
    x = np.full((len(unk), X.shape[1]), 0.0); r = rhs - Lmul(x); z = r / Dg; p = z.copy(); rz = (r * z).sum(0)
    for it in range(iters):
        Ap = Lmul(p); alpha = rz / np.maximum((p * Ap).sum(0), 1e-30); x += alpha * p; r -= alpha * Ap
        if np.abs(r).max() < 1e-7: break
        z = r / Dg; rz2 = (r * z).sum(0); p = z + (rz2 / np.maximum(rz, 1e-30)) * p; rz = rz2
    X[unk] = np.clip(x, 0, 1); s = X.sum(1, keepdims=True); s[s == 0] = 1
    return X / s


def smooth_rows(T, X, rows_mask, iters):
    n = len(X); E = np.concatenate([T[:, [0, 1]], T[:, [1, 2]], T[:, [2, 0]]]); E = np.concatenate([E, E[:, ::-1]])
    deg = np.bincount(E[:, 0], minlength=n).astype(float); deg[deg == 0] = 1
    for _ in range(iters):
        acc = np.zeros_like(X)
        for k in range(X.shape[1]): acc[:, k] = np.bincount(E[:, 0], X[E[:, 1], k], n)
        avg = acc / deg[:, None]; X = np.where(rows_mask[:, None], avg, X)
    s = X.sum(1, keepdims=True); s[s == 0] = 1; return X / s


def closest_on(Vsrc, Tsrc, P):
    """closest point on the source triangles for each P: (face, barycentric, point, distance, face normal) via BVH."""
    bvh = BVHTree.FromPolygons([Vector(v) for v in Vsrc], [list(t) for t in Tsrc], all_triangles=True)
    F = np.zeros(len(P), int); B = np.zeros((len(P), 3)); Q = np.zeros((len(P), 3)); Dd = np.zeros(len(P)); N = np.zeros((len(P), 3))
    for i, p in enumerate(P):
        loc, nrm, idx, d = bvh.find_nearest(Vector(p))
        F[i] = idx; Q[i] = loc; Dd[i] = d; N[i] = nrm
    a, b, c = Vsrc[Tsrc[F, 0]], Vsrc[Tsrc[F, 1]], Vsrc[Tsrc[F, 2]]
    v0 = b - a; v1 = c - a; v2 = Q - a
    d00 = (v0 * v0).sum(1); d01 = (v0 * v1).sum(1); d11 = (v1 * v1).sum(1); d20 = (v2 * v0).sum(1); d21 = (v2 * v1).sum(1)
    den = np.maximum(d00 * d11 - d01 * d01, 1e-18); v = (d11 * d20 - d01 * d21) / den; w = (d00 * d21 - d01 * d20) / den; u = 1 - v - w
    B = np.clip(np.stack([u, v, w], 1), 0, 1); B /= B.sum(1, keepdims=True)
    return F, B, Q, Dd, N


def vnormals(V, T):
    fn = np.cross(V[T[:, 1]] - V[T[:, 0]], V[T[:, 2]] - V[T[:, 0]])
    N = np.zeros_like(V)
    for k in range(3): np.add.at(N, T[:, k], fn)
    return N / np.maximum(np.linalg.norm(N, axis=1, keepdims=True), 1e-12)


# ------------------------------------------------------------------ mesh objects from numpy
def mesh_obj(name, V, F, coll=None):
    me = bpy.data.meshes.new(name); me.from_pydata([tuple(v) for v in V], [], [tuple(int(i) for i in f) for f in F]); me.update()
    o = bpy.data.objects.new(name, me); (coll or bpy.context.scene.collection).objects.link(o); return o


def set_co(o, V):
    o.data.vertices.foreach_set('co', np.asarray(V, np.float64).ravel()); o.data.update()


# ------------------------------------------------------------------ input normalisation
def import_glb(path):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=path)
    new = [o for o in bpy.data.objects if o not in before]
    ms = [o for o in new if o.type == 'MESH']
    for o in new:
        if o.type != 'MESH': continue
    bpy.ops.object.select_all(action='DESELECT')
    for o in ms: o.select_set(True)
    bpy.context.view_layer.objects.active = ms[0]
    if len(ms) > 1: bpy.ops.object.join()
    o = bpy.context.view_layer.objects.active
    o.parent = None
    o.data.transform(o.matrix_world); o.matrix_world = Matrix.Identity(4)
    # glTF brings custom split normals; once the mesh is re-posed they no longer turn with it and shade as light and dark slivers (found in the
    # brief 069 end-to-end test): clear them and shade smooth from the geometry
    if not (SET in ('e2e', 'fixer', 'enforcer') and E2E.get('lod0', {}).get('mode') == 'source'): clean_normals(o)
    for x in new:
        if x is not o and x.name in bpy.data.objects: bpy.data.objects.remove(x, do_unlink=True)
    return o


def normalise(o):
    """Up = the longest bbox axis (its sign: the feet are the end with the narrowest slab), front-back = the shorter of the others, front = the way
    the feet point. Returns the 4x4 applied (to Blender's Z up, front -Y, feet at z = 0, centred in x and y on the feet)."""
    V = np.array([list(v.co) for v in o.data.vertices]); N = D['normalise']
    ext = V.max(0) - V.min(0); up = int(np.argmax(ext)); rest = [k for k in range(3) if k != up]
    fb = rest[int(np.argmin(ext[rest]))]; lr = [k for k in rest if k != fb][0]
    h = ext[up]; lo = V[:, up].min(); hi = V[:, up].max()
    # sign of up: the head end is narrower in the left-right extent than the arm-span band? use the slabs: the 5 % at each end; the feet end is two blobs wide, the head end one
    def width(sel): return np.ptp(V[sel][:, lr]) if sel.any() else 0
    s_lo = width(V[:, up] < lo + 0.05 * h); s_hi = width(V[:, up] > hi - 0.05 * h)
    upsign = 1.0 if s_lo > s_hi else -1.0
    z = (V[:, up] - (lo if upsign > 0 else hi)) * upsign
    foot = z < N['footSlabFrac'] * h; shin = (z > N['shinSlabFrac'][0] * h) & (z < N['shinSlabFrac'][1] * h)
    fwdsign = 1.0 if V[foot][:, fb].mean() > V[shin][:, fb].mean() else -1.0
    # new axes: X = left-right (sign set so the frame is right handed), -Y = front, Z = up
    ez = np.zeros(3); ez[up] = upsign; ey = np.zeros(3); ey[fb] = -fwdsign; ex = np.cross(ey, ez)
    Rm = np.stack([ex, ey, ez])           # rows: new axes in old coords
    W = (V - V.mean(0)) @ Rm.T
    W[:, 2] -= W[:, 2].min()
    f = W[:, 2] < N['footSlabFrac'] * h
    W[:, 0] -= W[f][:, 0].mean(); W[:, 1] -= W[f][:, 1].mean() * 0 + np.median(W[W[:, 2] < 0.5 * h][:, 1])
    # scale to a nominal 1.75 m for the fit (the final scale comes after the fit)
    k = 1.75 / h; W *= k
    set_co(o, W)
    return {'up': up, 'fb': fb, 'upsign': upsign, 'fwdsign': fwdsign, 'scale': k, 'heightRaw': float(h)}


# ------------------------------------------------------------------ texture helpers
def base_color_image(o):
    for m in o.data.materials:
        if not m or not m.use_nodes: continue
        for n in m.node_tree.nodes:
            if n.type == 'BSDF_PRINCIPLED':
                l = n.inputs['Base Color'].links
                if l and l[0].from_node.type == 'TEX_IMAGE': return l[0].from_node.image
    return None


def simple_material(name, image=None, colour=(0.8, 0.8, 0.8, 1), rough=0.8):
    m = bpy.data.materials.new(name); m.use_nodes = True; nt = m.node_tree; b = nt.nodes['Principled BSDF']
    b.inputs['Roughness'].default_value = rough
    if image is not None:
        t = nt.nodes.new('ShaderNodeTexImage'); t.image = image; nt.links.new(t.outputs['Color'], b.inputs['Base Color'])
    else: b.inputs['Base Color'].default_value = colour
    return m


def flat_material(o, image):
    """one plain material with the source's base colour only (normal and ORM dropped, as the game's figures are matte)."""
    m = simple_material(o.name + '_mat', image)
    o.data.materials.clear(); o.data.materials.append(m); return m


def decimate_to(o, tris):
    n = len(tris_of(o))
    if n <= tris: return n
    md = o.modifiers.new('dec', 'DECIMATE'); md.ratio = tris / n; md.use_collapse_triangulate = True
    with bpy.context.temp_override(object=o, active_object=o):
        bpy.ops.object.modifier_move_to_index(modifier=md.name, index=0); bpy.ops.object.modifier_apply(modifier=md.name)
    return len(tris_of(o))


# ------------------------------------------------------------------ landmarks, the loose-part classifier, the standard rest, attachment points
def landmarks(arm, body_co):
    W = arm.matrix_world; B = arm.data.bones; hz = lambda n: (W @ B[n].head_local).z
    hip = (hz('LeftUpLeg') + hz('RightUpLeg')) / 2; crown = float(body_co[:, 2].max()); head = hz('Head')
    return {'knee': (hz('LeftLeg') + hz('RightLeg')) / 2, 'hip': hip, 'crotch': hip - 0.04 * (crown / 1.75), 'waist': hz('Spine1'), 'chest': hz('Spine2'),
            'neck': hz('Neck'), 'head': head, 'crown': crown, 'brow': head + D['garments']['kinds']['hat']['browFrac'] * (crown - head)}


def grow(mask, T, rings):
    E = np.concatenate([T[:, [0, 1]], T[:, [1, 2]], T[:, [2, 0]]])
    for _ in range(rings):
        m2 = mask.copy(); m2[E[mask[E[:, 0]], 1]] = True; m2[E[mask[E[:, 1]], 0]] = True; mask = m2
    return mask


def classify(Vt, Tt, dist, nearest_bone, lm, wears):
    """the garment kind of each target vertex ('' = tight/suit), by fit.json garments.kinds; only the kinds the person wears."""
    G = D['garments']; loose = dist > G['looseM']; z = Vt[:, 2]; nb = np.asarray(nearest_bone)
    kinds = {}
    def isin(names): return np.isin(nb, names)
    head = isin(['Head', 'Neck'])
    if 'hat' in wears: kinds['hat'] = loose & (z > lm['brow']) & head
    top = isin(['Spine1', 'Spine2', 'LeftShoulder', 'RightShoulder', 'LeftArm', 'RightArm', 'Neck'])
    if 'cape' in wears: kinds['cape'] = loose & (z > lm['waist']) & top & ~kinds.get('hat', np.zeros(len(z), bool))
    low = isin(['Hips', 'Spine', 'LeftUpLeg', 'RightUpLeg', 'LeftLeg', 'RightLeg'])
    if 'skirt' in wears: kinds['skirt'] = loose & (z < lm['waist']) & (z > lm['knee'] - 0.1) & low
    if 'coat' in wears:
        kinds['coat'] = loose & (z < lm['chest']) & (z > lm['knee'] - 0.1) & (low | isin(['Spine1', 'Spine2', 'LeftArm', 'RightArm', 'LeftForeArm', 'RightForeArm']))
    used = np.zeros(len(z), bool)
    for k in kinds.values(): used |= k
    kinds['hair'] = loose & head & ~used & (z > lm['neck'] - 0.02)
    lab = np.array([''] * len(z), dtype=object)
    for k in ('hair', 'coat', 'skirt', 'cape', 'hat'):                       # later kinds win where two overlap
        if k in kinds: lab[grow(kinds[k], Tt, 0)] = k
    # drop specks: a kind needs at least 1 % of the mesh, and grow each by growRings into the tight part
    out = np.array([''] * len(z), dtype=object)
    for k in ('hair', 'coat', 'skirt', 'cape', 'hat'):
        m = lab == k
        if m.sum() < 0.01 * len(z): continue
        out[grow(m, Tt, G['growRings'])] = k
    return out


def loose_rules(W, names, Vt, lab, lm, arm):
    """overwrite the weights of loose vertices by fit.json transfer.loose (A); returns the count per rule."""
    R = D['transfer']['loose']; ix = {n: i for i, n in enumerate(names)}; Mw = arm.matrix_world; Bn = arm.data.bones
    if SET in ('e2e', 'fixer', 'enforcer') and 'looseRules' in E2E: R = {k: v for k, v in R.items() if k in E2E['looseRules']}
    knee = {s: np.array(Mw @ Bn[s + 'Leg'].head_local) for s in ('Left', 'Right')}
    hipj = {s: np.array(Mw @ Bn[s + 'UpLeg'].head_local) for s in ('Left', 'Right')}
    counts = {}
    for k, rule in R.items():
        if k == 'note': continue
        m = lab == k
        if rule.get('below') == 'crotch': m &= Vt[:, 2] < lm['crotch']
        if not m.any(): continue
        rows = np.nonzero(m)[0]; W[rows] = 0
        if rule['rule'] == 'rigid': W[rows, ix[rule['bones'][0]]] = 1
        elif rule['rule'] == 'side':
            cx = Vt[rows, 0]; half = max(np.abs(cx).max(), 1e-3); t = np.clip(np.abs(cx) / half, 0, 1)
            cs = rule['centreShare']; W[rows, ix['Spine2']] = cs + (1 - cs) * (1 - t)
            W[rows, ix['LeftShoulder']] = np.where(cx > 0, (1 - cs) * t, 0); W[rows, ix['RightShoulder']] = np.where(cx <= 0, (1 - cs) * t, 0)
        elif rule['rule'] == 'lateralBlend':
            # the hem follows both thighs: the share of each thigh by lateral position between the two thigh axes at that height; the hips' share fades in toward the crotch
            p = Vt[rows]
            def axis_x(s):
                a, b = hipj[s], knee[s]; t = np.clip((p[:, 2] - a[2]) / (b[2] - a[2]), 0, 1); return a[0] + t * (b[0] - a[0])
            xl, xr = axis_x('Left'), axis_x('Right'); u = np.clip((p[:, 0] - xr) / np.maximum(xl - xr, 1e-3), 0, 1)
            u = u * u * (3 - 2 * u)
            hs = rule['hipShare'] * np.clip((p[:, 2] - lm['knee']) / max(lm['crotch'] - lm['knee'], 1e-3), 0, 1)
            W[rows, ix['Hips']] = hs; W[rows, ix['LeftUpLeg']] = (1 - hs) * u; W[rows, ix['RightUpLeg']] = (1 - hs) * (1 - u)
        counts[k] = int(len(rows))
    return W, counts


def stance_deg(arm):
    """fit.json rest.stance (brief 069, Sky: the Enforcer's feet land in line): the legs' sideways angle in the Humanoid zero. 'keepOutward': the
    figure's own designed stance, the mean of its two legs' outward angle at the fitted rest (hip joint to ankle, seen from the front), never
    inward (an inward rest is knees held together by a skirt or a fit artefact: straight down, as before) and at most maxDeg. Unity reads
    the zero as legs straight down, so a wide stance designed into the mesh is kept, and narrowed by nothing, in every clip."""
    rule = D['rest'].get('stance', {}); Mw = arm.matrix_world; B = arm.data.bones
    if rule.get('rule') != 'keepOutward': return 0.0
    a = []
    for s, sx in (('Left', 1), ('Right', -1)):
        up = Mw @ B[s + 'UpLeg'].head_local; an = Mw @ B[s + 'Foot'].head_local
        a.append(math.degrees(math.atan2((an.x - up.x) * sx, up.z - an.z)))
    return round(min(max(0.0, sum(a) / 2), rule['maxDeg']), 3)


def standard_aims(arm):
    """the standard rest: the reference T-pose directions for the upper body, Latch's neutral legs (straight down, feet forward at their own pitch, toes level)."""
    ref = json.load(open(os.path.join(ROOT, D['rest']['reference'])))['directions']
    aims = {k: list(v) for k, v in ref.items() if k in arm.pose.bones and 'Leg' not in k}
    Mw = arm.matrix_world; spread = stance_deg(arm)
    for s, sx in (('Left', 1), ('Right', -1)):
        t = math.radians(spread); aims[s + 'UpLeg'] = [sx * math.sin(t), 0, -math.cos(t)]; aims[s + 'Leg'] = list(aims[s + 'UpLeg'])
        pb = arm.pose.bones[s + 'Foot']; v = (Mw.to_3x3() @ (pb.tail - pb.head)); pitch = math.atan2(-v.z, math.hypot(v.x, v.y))
        aims[s + 'Foot'] = [0, -math.cos(pitch), -math.sin(pitch)]; aims[s + 'ToeBase'] = [0, -1, 0]
    return aims


def rest_leg_report(arm):
    Mw = arm.matrix_world; B = arm.data.bones; out = {}
    for s in ('Left', 'Right'):
        up = Mw @ B[s + 'UpLeg'].head_local; kn = Mw @ B[s + 'Leg'].head_local; an = Mw @ B[s + 'Foot'].head_local; to = Mw @ B[s + 'ToeBase'].head_local
        d = kn - up; f = to - an
        out[s] = {'abductionDeg': round(math.degrees(math.atan2(d.x * (1 if s == 'Left' else -1), -d.z)), 2) + 0.0, 'ankleOutM': round(abs(an.x - up.x), 4),
                  'footYawDeg': round(math.degrees(math.atan2(f.x, -f.y)), 2), 'footPitchDeg': round(math.degrees(math.atan2(-f.z, math.hypot(f.x, f.y))), 2)}
    return out


def add_attach(arm, body_co):
    """fit.json attach.points as non-deforming bones placed by rule from the rest skeleton and body; returns their rest heads."""
    bp.ctx(arm, 'EDIT'); eb = arm.data.edit_bones; Mi = arm.matrix_world.inverted(); Mw = arm.matrix_world
    def wh(n): return Mw @ eb[n].head
    out = {}
    def mk(name, parent, head, tail, work=None):
        # work: the prop's working line there (a barrel). The bone's roll puts its Z on it, so in Unity the bone's +Y is the prop's up (the bone's
        # direction) and its +Z the working line: every held prop sits at localRotation identity (charpipe/props/props.json convention, attach.frame)
        b = eb.new(name); b.head = Mi @ head; b.tail = Mi @ tail; b.parent = eb[parent]; b.use_deform = False; out[name] = list(head)
        if work is not None:
            up = (tail - head).normalized(); wk = (work - up * work.dot(up)).normalized(); b.align_roll(Mi.to_3x3() @ wk)
    for s, sx in (('Right', -1), ('Left', 1)):
        h = wh(s + 'Hand'); m1 = wh(s + 'HandMiddle1'); i1 = wh(s + 'HandIndex1'); p1 = wh(s + 'HandPinky1')
        palm = h + (m1 - h) * 0.5; nrm = (i1 - h).cross(p1 - h).normalized() * (1 if s == 'Left' else -1)
        w = (i1 - p1).length; c = palm - nrm * 0.25 * w
        fore = (h - wh(s + 'ForeArm')).normalized(); thumb = (i1 - p1); thumb = (thumb - fore * thumb.dot(fore)).normalized()
        if f'Attach_{s}Hand' in D['attach']['points']: mk(f'Attach_{s}Hand', s + 'Hand', c, c + thumb * 0.05, work=fore)   # up: the thumb side; work: along the forearm
    hips = wh('Hips'); hz = (wh('LeftUpLeg').z + wh('RightUpLeg').z) / 2
    sl = body_co[np.abs(body_co[:, 2] - hz) < 0.02]; sl = sl[np.abs(sl[:, 0]) < 0.3]
    wx = float(np.abs(sl[:, 0]).max()) if len(sl) else 0.18
    lr = wh('LeftUpLeg') - wh('RightUpLeg'); lr.z = 0; fwd = lr.cross(Vector((0, 0, 1))).normalized()   # the figure's facing (the figures face -Y)
    hp = Vector((-(wx + 0.01), hips.y, hz))
    mk('Attach_HipRight', 'Hips', hp, hp + fwd * 0.05, work=Vector((0, 0, -1)))   # holster: up = forward, working line = down (barrel down, slide forward)
    s2 = wh('Spine2'); band = body_co[np.abs(body_co[:, 2] - s2.z) < 0.02]; band = band[np.abs(band[:, 0]) < 0.12]
    by = float(band[:, 1].max()) if len(band) else s2.y + 0.12
    mk('Attach_Back', 'Spine2', Vector((0, by + 0.01, s2.z)), Vector((0, by + 0.06, s2.z)))
    sh = wh('LeftShoulder'); ar = wh('LeftArm'); top = (sh + ar) / 2
    nz = wh('Neck').z                                     # the shoulder's top lies below the neck base (above it, the search finds the head)
    near = body_co[(np.linalg.norm(body_co[:, [0, 1]] - np.array([top.x, top.y]), axis=1) < 0.04) & (body_co[:, 2] < nz + 0.02)]
    tz = float(near[:, 2].max()) if len(near) else top.z + 0.06
    mk('Attach_ShoulderLeft', 'LeftShoulder', Vector((top.x, top.y, tz)), Vector((top.x, top.y, tz + 0.05)))
    bp.ctx(arm, 'OBJECT')
    return out


def fitted_human(fit, keep_basemesh=True):
    """the real MPFB body at the fit's sex and macros, scaled and moved as fitted, posed into the source's pose (the fit's aims). Returns arm, basemesh, proxy,
    and the donor (posed basemesh body: V, tris, W) with the bone names."""
    arm, b, px = human(fit['macros'], fit['sex'])
    p = fit['params']; arm.matrix_world = Matrix.Translation((p['tx'], p['ty'], p['tz'])) @ Matrix.Scale(p['s'], 4)
    bpy.ops.object.select_all(action='DESELECT')
    for o in (arm, b, px): o.select_set(True)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    pose_blender(arm, {k: v for k, v in fit['aims'].items()}, {k: np.array(v) for k, v in fit['pre'].items()})
    names = list(KEEP)
    full = eval_co(b, armature=True); body = np.array([any(g.group == b.vertex_groups['body'].index and g.weight > 0.5 for g in v.groups) for v in b.data.vertices])
    T = tris_of(b); keep = body[T].all(1); T = T[keep]; remap = -np.ones(len(full), int); remap[np.nonzero(body)[0]] = np.arange(body.sum())
    donor = {'V': full[body], 'T': remap[T], 'W': weights_of(b, names)[body], 'names': names}
    if not keep_basemesh: bpy.data.objects.remove(b, do_unlink=True); b = None
    return arm, b, px, donor


def transfer(donor, Vt, Tt, height):
    """robust weight transfer (Abdrashitov et al. 2023): closest-point match where confident, Dirichlet inpainting for the rest. Returns W, stats, distance, nearest bone."""
    X = D['transfer']; Vd, Td, Wd = donor['V'], donor['T'], donor['W']
    F, Bc, Q, dist, Nd = closest_on(Vd, Td, Vt)
    Wm = np.einsum('vk,vkb->vb', Bc, Wd[Td[F]])
    Nt = vnormals(Vt, Tt); cosang = np.abs((Nt * Nd).sum(1))
    ok = (dist < X['maxDistFrac'] * height) & (cosang > math.cos(math.radians(X['maxNormalDeg'])))
    W = inpaint(Vt, Tt, ok, Wm * ok[:, None], X['cgIters'])
    W = smooth_rows(Tt, W, ~ok, X['smoothIters'])
    empty = W.sum(1) < 0.5; W[empty] = Wm[empty]
    if X.get('smoothAllIters', 0): W = smooth_rows(Tt, W, np.ones(len(W), bool), X['smoothAllIters'])
    if X.get('spatialMergeM', 0): W = spatial_merge(Vt, W, X['spatialMergeM'], X.get('spatialMergePasses', 2))
    nb = np.array(donor['names'])[np.argmax(Wm, 1)]
    return W, {'matched': int(ok.sum()), 'inpainted': int((~ok).sum()), 'pctInpainted': round(100 * float((~ok).mean()), 1), 'unanchored': int(empty.sum())}, dist, nb


def separate_tris_mask(o, keep_faces):
    """split the faces in keep_faces (bool per polygon) into a new object; returns it."""
    bpy.ops.object.select_all(action='DESELECT'); bp.ctx(o, 'EDIT'); bpy.ops.mesh.select_all(action='DESELECT'); bp.ctx(o, 'OBJECT')
    for p in o.data.polygons: p.select = bool(keep_faces[p.index])
    bp.ctx(o, 'EDIT'); before = set(bpy.data.objects); bpy.ops.mesh.separate(type='SELECTED'); bp.ctx(o, 'OBJECT')
    new = [x for x in bpy.data.objects if x not in before]; return new[0] if new else None


def tag(o, role, lod=0):
    o['fit_role'] = role; o['fit_lod'] = lod


# ------------------------------------------------------------------ texture transfer by closest point (replaces a cage bake: no missed rays)
def _uv_tris(o):
    me = o.data; me.calc_loop_triangles(); lt = me.loop_triangles
    li = np.empty(len(lt) * 3, np.int64); lt.foreach_get('loops', li); vi = np.empty(len(lt) * 3, np.int64); lt.foreach_get('vertices', vi)
    uv = np.empty(len(me.loops) * 2); me.uv_layers.active.data.foreach_get('uv', uv); uv = uv.reshape(-1, 2)
    return vi.reshape(-1, 3), uv[li].reshape(-1, 3, 2)


def image_array(img):
    w, h = img.size; a = np.empty(w * h * 4, np.float32); img.pixels.foreach_get(a); return a.reshape(h, w, 4)


def texture_transfer(target, source, src_img, px, margin=4):
    """For each texel of target's UV layout: its 3D point, the closest point on source, the source's UV there, the source image's colour (bilinear).
    Returns a Blender image (px x px)."""
    Tv, Tuv = _uv_tris(target); Vt = np.array([list(v.co) for v in target.data.vertices])
    Sv, Suv = _uv_tris(source); Vs = np.array([list(v.co) for v in source.data.vertices])
    bvh = BVHTree.FromPolygons([Vector(v) for v in Vs], [list(t) for t in Sv], all_triangles=True)
    S = image_array(src_img); sh, sw = S.shape[:2]
    out = np.zeros((px, px, 4), np.float32); filled = np.zeros((px, px), bool)
    for t in range(len(Tv)):
        uv = Tuv[t] * px; lo = np.floor(uv.min(0)).astype(int); hi = np.ceil(uv.max(0)).astype(int)
        lo = np.clip(lo, 0, px - 1); hi = np.clip(hi, 0, px - 1)
        xs, ys = np.meshgrid(np.arange(lo[0], hi[0] + 1) + 0.5, np.arange(lo[1], hi[1] + 1) + 0.5); P = np.stack([xs.ravel(), ys.ravel()], 1)
        a, b, c = uv; v0 = b - a; v1 = c - a; den = v0[0] * v1[1] - v1[0] * v0[1]
        if abs(den) < 1e-12: continue
        d = P - a; l1 = (d[:, 0] * v1[1] - v1[0] * d[:, 1]) / den; l2 = (v0[0] * d[:, 1] - d[:, 0] * v0[1]) / den; l0 = 1 - l1 - l2
        ins = (l0 >= -1e-3) & (l1 >= -1e-3) & (l2 >= -1e-3)
        if not ins.any(): continue
        X = l0[ins, None] * Vt[Tv[t, 0]] + l1[ins, None] * Vt[Tv[t, 1]] + l2[ins, None] * Vt[Tv[t, 2]]
        pix = np.floor(P[ins]).astype(int)
        for k in range(len(X)):
            loc, nrm, fi, dist = bvh.find_nearest(Vector(X[k]))
            if loc is None: continue
            A, B, C = Vs[Sv[fi]]; q = np.array(loc)
            e0 = B - A; e1 = C - A; e2 = q - A; d00 = e0 @ e0; d01 = e0 @ e1; d11 = e1 @ e1; d20 = e2 @ e0; d21 = e2 @ e1; dn = d00 * d11 - d01 * d01
            if dn < 1e-18: m1 = m2 = 1 / 3
            else: m1 = (d11 * d20 - d01 * d21) / dn; m2 = (d00 * d21 - d01 * d20) / dn
            m0 = 1 - m1 - m2; suv = m0 * Suv[fi, 0] + m1 * Suv[fi, 1] + m2 * Suv[fi, 2]
            sx = (suv[0] % 1.0) * sw - 0.5; sy = (suv[1] % 1.0) * sh - 0.5; x0 = int(np.floor(sx)); y0 = int(np.floor(sy)); fx = sx - x0; fy = sy - y0
            x0c, x1c = np.clip([x0, x0 + 1], 0, sw - 1); y0c, y1c = np.clip([y0, y0 + 1], 0, sh - 1)
            col = (S[y0c, x0c] * (1 - fx) * (1 - fy) + S[y0c, x1c] * fx * (1 - fy) + S[y1c, x0c] * (1 - fx) * fy + S[y1c, x1c] * fx * fy)
            out[pix[k, 1], pix[k, 0]] = col; filled[pix[k, 1], pix[k, 0]] = True
    for _ in range(margin):                                  # grow the islands into the gutters
        grown = out.copy(); g2 = filled.copy()
        for dy, dx in ((0, 1), (0, -1), (1, 0), (-1, 0)):
            sh_ = np.roll(np.roll(out, dy, 0), dx, 1); sf = np.roll(np.roll(filled, dy, 0), dx, 1); m = sf & ~g2
            grown[m] = sh_[m]; g2 |= m
        out, filled = grown, g2
    out[..., 3] = 1.0
    img = bpy.data.images.new(target.name + '_tex', px, px, alpha=False); img.pixels.foreach_set(out.ravel()); img.update()
    return img


def drop_crumbs(o, pct):
    """delete connected parts under pct % of the vertices; returns how many vertices went."""
    bm = bmesh.new(); bm.from_mesh(o.data); bm.verts.ensure_lookup_table(); seen = set(); comps = []
    for v in bm.verts:
        if v.index in seen: continue
        st = [v]; c = []
        while st:
            x = st.pop()
            if x.index in seen: continue
            seen.add(x.index); c.append(x); st.extend(e.other_vert(x) for e in x.link_edges)
        comps.append(c)
    lim = pct / 100.0 * len(bm.verts); gone = [v for c in comps if len(c) < lim for v in c]
    bmesh.ops.delete(bm, geom=gone, context='VERTS'); bm.to_mesh(o.data); bm.free(); o.data.update()
    return len(gone), len(comps)


def props(arm, att, suit, skip=()):
    """a cross-body bag (strap from the left shoulder over the chest to a bag at the right hip) and a tray in the left hand; weights from the nearest suit point.
    skip: props this person doesn't get (a role with a visible holster keeps the right hip free: no bag; charpipe/props/props.json roles)."""
    A = D['attach']['props']; out = []
    Vs = eval_co(suit); Ts = tris_of(suit); names = [g.name for g in suit.vertex_groups]; Ws = weights_of(suit, names)
    sb = BVHTree.FromPolygons([Vector(v) for v in Vs], [list(t) for t in Ts], all_triangles=True)
    bag = None; hip = np.array(att['Attach_HipRight']); sz = A['bag']['size'][2]
    if 'bag' not in skip:
        # bag
        hip = np.array(att['Attach_HipRight']); sx, sy, sz = A['bag']['size']
        bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1.0)
        for v in bm.verts: v.co = Vector((v.co.x * sy, v.co.y * sx, v.co.z * sz))
        bmesh.ops.bevel(bm, geom=list(bm.edges), offset=0.015, segments=2, affect='EDGES')
        me = bpy.data.meshes.new('bag'); bm.to_mesh(me); bm.free(); bag = bpy.data.objects.new('prop_bag', me); bpy.context.scene.collection.objects.link(bag)
        bag.location = Vector(hip) + Vector((-sy / 2 - 0.02, 0, -0.05)); bp.ctx(bag); bpy.ops.object.transform_apply(location=True)
        # strap: points on the line shoulder -> bag top, each pushed out of the body by 1 cm along the front
        sh = np.array(att['Attach_ShoulderLeft']); end = hip + np.array([0, 0, sz / 2 - 0.05])
        pts = []; Mw = arm.matrix_world; ctr = Mw @ arm.data.bones['Spine2'].head_local
        for k in range(13):
            # the strap point sits outside the body's OUTERMOST surface along the line from the spine axis (generated meshes have inner shells,
            # whose nearest normals point inward), plus strapOffsetM
            q = sh + (end - sh) * k / 12; o = Vector((ctr.x, ctr.y, q[2]))
            d = (Vector((q[0] - o.x, 0, 0)) * 2.0 + Vector((0, -1, 0))).normalized()     # a cross-body strap crosses the chest's front, tilted to its side
            far = None; cur = o
            for _ in range(64):
                loc, nr, fi, dd = sb.ray_cast(cur, d, 1.0)
                if loc is None or (far is not None and (loc - far).length > 0.05): break      # a gap: the next hit is another part (an arm), not this surface
                far = loc; cur = loc + d * 1e-4
            q2 = np.array(far + d * A['bag']['strapOffsetM']) if far is not None else q
            if k == 0: q2 = np.maximum(q2, sh) if False else q2
            pts.append(q2)
        cu = bpy.data.curves.new('strap', 'CURVE'); cu.dimensions = '3D'; sp = cu.splines.new('POLY'); sp.points.add(len(pts) - 1)
        for i, q in enumerate(pts): sp.points[i].co = (*q, 1)
        cu.bevel_depth = 0.008; cu.bevel_resolution = 1
        so = bpy.data.objects.new('prop_strap', cu); bpy.context.scene.collection.objects.link(so); bp.ctx(so); bpy.ops.object.convert(target='MESH'); strap = bpy.context.view_layer.objects.active
        bpy.ops.object.select_all(action='DESELECT'); bag.select_set(True); strap.select_set(True); bpy.context.view_layer.objects.active = bag; bpy.ops.object.join()
    # tray: a disc at the left palm, flat across the palm
    ar = arm.data.bones; Mw = arm.matrix_world
    h = Mw @ ar['LeftHand'].head_local; m1 = Mw @ ar['LeftHandMiddle1'].head_local; i1 = Mw @ ar['LeftHandIndex1'].head_local; p1 = Mw @ ar['LeftHandPinky1'].head_local
    nrm = (i1 - h).cross(p1 - h).normalized(); tx, ty, tz = A['tray']['size']
    bm = bmesh.new(); bmesh.ops.create_cone(bm, cap_ends=True, segments=24, radius1=tx / 2, radius2=tx / 2, depth=tz)
    me = bpy.data.meshes.new('tray'); bm.to_mesh(me); bm.free(); tray = bpy.data.objects.new('prop_tray', me); bpy.context.scene.collection.objects.link(tray)
    # the tray clears the hand: its centre sits past the hand's own depth on the palm side (measured from the body's hand vertices), not a fixed gap
    hand_ix = [i for i, n in enumerate(names) if n.startswith('LeftHand')]; ap = np.array(att['Attach_LeftHand'])
    hv = Vs[(Ws[:, hand_ix].sum(1) > 0.5) & (np.linalg.norm(Vs - ap, axis=1) < 0.15)] if hand_ix else Vs[:0]
    depth = float(max(0.0, ((hv - ap) @ -np.array(nrm)).max())) if len(hv) else 0.01
    R = Vector((0, 0, 1)).rotation_difference(nrm).to_matrix().to_4x4(); c = Vector(att['Attach_LeftHand']) - nrm * (depth + tz / 2 + A['tray'].get('clearM', 0.004))
    tray.matrix_world = Matrix.Translation(c) @ R; bp.ctx(tray); bpy.ops.object.transform_apply(location=True, rotation=True)
    m = simple_material('prop_chrome', None, (0.78, 0.78, 0.8, 1), 0.35)
    for o, rule in [(o_, r_) for o_, r_ in ((bag, 'nearest'), (tray, 'LeftHand')) if o_ is not None]:
        o.data.materials.clear(); o.data.materials.append(m if o is tray else simple_material('prop_bag', None, (0.12, 0.35, 0.42, 1), 0.6))
        V = np.array([list(v.co) for v in o.data.vertices])
        if rule == 'nearest':
            W = np.zeros((len(V), len(names)))
            for i, q in enumerate(V):
                loc, nr, fi, d = sb.find_nearest(Vector(q)); W[i] = Ws[Ts[fi]].mean(0)
            # the bag body itself rides the hips only (a rigid box), the strap blends
            body = V[:, 2] < hip[2] + sz / 2 - 0.04; W[body] = 0; W[body, names.index('Hips')] = 1
            set_weights(o, W, names, 4)
        else:
            W = np.zeros((len(V), 1)); W[:, 0] = 1; set_weights(o, W, [rule], 1)
        o.parent = arm; md = o.modifiers.new('Armature', 'ARMATURE'); md.object = arm; tag(o, 'prop'); out.append(o)
    return out




def hair_under_hat(hat, objs, centre, brow):
    """any vertex of the head pieces above the brow that lies at or beyond the hat's inner wall (seen from the head's centre) is pulled inside it."""
    Vh = np.array([list(v.co) for v in hat.data.vertices]); Th = tris_of(hat)
    bvh = BVHTree.FromPolygons([Vector(v) for v in Vh], [list(t) for t in Th], all_triangles=True)
    pad = D['garments']['hat']['hairPushM']; moved = 0; c = Vector(centre)
    for o in objs:
        V = np.array([list(v.co) for v in o.data.vertices]); ch = False
        for i in np.nonzero(V[:, 2] > brow)[0]:
            v = Vector(V[i]); d = v - c; r = d.length
            if r < 1e-6: continue
            loc, nrm, fi, dist = bvh.ray_cast(c, d.normalized(), r + 0.05)
            if loc is not None and dist < r + pad:
                V[i] = list(c + d.normalized() * max(dist - pad, 0.0)); moved += 1; ch = True
        if ch: set_co(o, V)
    return moved





def spatial_merge(V, W, r, passes):
    """weights averaged over every vertex within r in space (not along the surface): layers of an image-to-3D mesh that lie on top of each other
    without sharing edges (cloth over an inner layer) then deform as one and do not z-fight once posed."""
    from mathutils.kdtree import KDTree
    kd = KDTree(len(V))
    for i, v in enumerate(V): kd.insert(Vector(v), i)
    kd.balance()
    nb = [[j for (_, j, _) in kd.find_range(Vector(v), r)] for v in V]
    for _ in range(passes):
        W = np.array([W[n].mean(0) for n in nb])
    s = W.sum(1, keepdims=True); s[s == 0] = 1; return W / s



def clean_normals(o):
    """glTF brings custom split normals; once the mesh is re-posed they no longer turn with it: clear them, shade smooth from the geometry."""
    if o.data.has_custom_normals:
        with bpy.context.temp_override(object=o, active_object=o): bpy.ops.mesh.customdata_custom_splitnormals_clear()
    o.data.shade_smooth()


def export_fbx(path, objs, active):
    """build.py's pinned FBX export (fixed header time and object ids), but without the tangent space: MikkTSpace on these decimated image-to-3D
    meshes gives different binormals run to run (found by the brief 069 end-to-end determinism check; every other array was identical). Unity
    recomputes tangents on import (the model meta: tangents = Calculate Mikktspace), so nothing is lost."""
    bp.pin_fbx()
    # the header's ApplicationNativeFile is the open .blend's full path: written as its bare name, so the same build gives the same bytes in any folder
    from io_scene_fbx import export_fbx_bin as eb
    pinned = eb.fbx_header_elements
    def header(root, scene_data, time=None):
        pinned(root, scene_data, time)
        def walk(e):
            if e.id == b'P' and e.props and e.props[0][4:].endswith(b'ApplicationNativeFile'):   # 'Original|ApplicationNativeFile'; strings carry a 4-byte length prefix
                nm = os.path.basename(bpy.data.filepath).encode(); e.props[-1] = len(nm).to_bytes(4, 'little') + nm
            for c in e.elems: walk(c)
        walk(root)
    eb.fbx_header_elements = header
    # texture file names: the exporter writes <output folder>/<image name> (absolute); written as the bare name, so they point beside the FBX
    enc = eb.encode_bin; write0 = enc.write
    def write(fn, root, version):
        def walk(e):
            for k, (t, v) in enumerate(zip(e.props_type, e.props)):
                if t == ord('S') and v[4:5] == b'/':                    # any absolute path (texture FileName, Filename, Path, Url): its bare name
                    nm = os.path.basename(v[4:].decode('utf8', 'replace')).encode(); e.props[k] = len(nm).to_bytes(4, 'little') + nm
            for c in e.elems: walk(c)
        walk(root); return write0(fn, root, version)
    enc.write = write
    try:
        bpy.ops.object.select_all(action='DESELECT')
        for x in objs: x.select_set(True)
        bpy.context.view_layer.objects.active = active
        os.makedirs(os.path.dirname(path), exist_ok=True)
        bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={'ARMATURE', 'MESH'}, use_mesh_modifiers=False,
                                 add_leaf_bones=False, bake_anim=False, axis_forward='-Z', axis_up='Y', apply_unit_scale=True,
                                 apply_scale_options='FBX_SCALE_ALL', bake_space_transform=False, primary_bone_axis='Y', secondary_bone_axis='X',
                                 armature_nodetype='NULL', mesh_smooth_type='FACE', use_tspace=False, path_mode='STRIP', embed_textures=False)
    finally:
        eb.fbx_header_elements = pinned; enc.write = write0



def work(*p):
    """working files (.blend): not deliverables, never compared byte for byte (see e2e.json deliverables)."""
    return os.path.join(ensure(os.path.join(SCR, 'work')), *p)


def src_blend(fid):
    w = os.path.join(SCR, 'work', f'{fid}_src.blend'); return w if os.path.exists(w) else os.path.join(SCR, 'fit', f'{fid}_src.blend')


def rest_mode():
    return E2E.get('rest', {}).get('mode', 'tpose') if SET in ('e2e', 'fixer', 'enforcer') else D['rest'].get('mode', 'tpose')



def armpit_merge(V, W, names):
    """e2e.json armpit: spatial weight averaging inside the armpit band only."""
    if not (SET in ('e2e', 'fixer', 'enforcer') and 'armpit' in E2E): return W, 0
    A = E2E['armpit']; ix = {n: i for i, n in enumerate(names)}
    armw = W[:, ix['LeftArm']] + W[:, ix['RightArm']]; torw = sum(W[:, ix[n]] for n in ('Spine1', 'Spine2', 'LeftShoulder', 'RightShoulder'))
    band = (armw > A['band'][0]) & (armw < A['band'][1]) & (torw > A['torsoMin'])
    from mathutils.kdtree import KDTree
    kd = KDTree(len(V))
    for i, v in enumerate(V): kd.insert(Vector(v), i)
    kd.balance(); W2 = W.copy()
    for i in np.nonzero(band)[0]:
        nb = [j for (_, j, _) in kd.find_range(Vector(V[i]), A['mergeM'])]; W2[i] = W[nb].mean(0)
    s = W2.sum(1, keepdims=True); s[s == 0] = 1; return W2 / s, int(band.sum())



def band_merge(V, W, lm):
    """fit.json transfer.bandMerge: a long garment's band (a skirt bridging the legs) gets spatially averaged weights, so its folds move as one."""
    R = D['transfer'].get('bandMerge')
    if not R: return W, {}
    z0 = float(V[:, 2].min()); H = float(V[:, 2].max()) - z0
    lo_z = lm['knee'] - R['kneeDownFrac'] * H; hi_z = lm['knee'] + R['kneeUpFrac'] * H
    levels = np.linspace(lo_z, hi_z, R['levelN']); dz = (hi_z - lo_z) / (2 * R['levelN'])
    bridged = sum(1 for z in levels if ((np.abs(V[:, 2] - z) < dz) & (np.abs(V[:, 0]) < R['centreM'])).any())
    frac = bridged / R['levelN']; info = {'bridgedFrac': round(frac, 3), 'applied': frac >= R['minBridged']}
    if not info['applied']: return W, info
    band = (V[:, 2] > z0 + R['zLoFrac'] * H) & (V[:, 2] < lm['hip'])
    from mathutils.kdtree import KDTree
    kd = KDTree(len(V))
    for i, v in enumerate(V): kd.insert(Vector(v), i)
    kd.balance(); idx = np.nonzero(band)[0]
    for _ in range(R['passes']):
        W2 = W.copy()
        for i in idx:
            nb = [j for (_, j, _) in kd.find_range(Vector(V[i]), R['mergeM'])]; W2[i] = W[nb].mean(0)
        W = W2
    s = W.sum(1, keepdims=True); s[s == 0] = 1; info['merged'] = int(len(idx)); return W / s, info


# ------------------------------------------------------------------ garment rigs (fit.json garmentRigs): build by rule, simulate, bake
def _hat(u, n):
    """weights of n bones along a chain for parameter u in [0, 1] (hat functions centred on the bones' middles, clamped at the ends)."""
    c = (np.arange(n) + 0.5) / n; w = np.clip(1 - np.abs(u[:, None] - c[None, :]) * n, 0, 1)
    w[u <= c[0], 0] = 1; w[u >= c[-1], -1] = 1; s = w.sum(1, keepdims=True); s[s == 0] = 1; return w / s


def _smooth(x): x = np.clip(x, 0, 1); return x * x * (3 - 2 * x)


def spatial_avg(V, W, idx, r):
    from mathutils.kdtree import KDTree
    kd = KDTree(len(V))
    for i, v in enumerate(V): kd.insert(Vector(v), i)
    kd.balance(); W2 = W.copy()
    for i in idx:
        nb = [j for (_, j, _) in kd.find_range(Vector(V[i]), r)]; W2[i] = W[nb].mean(0)
    s = W2.sum(1, keepdims=True); s[s == 0] = 1; return W2 / s


def garment_rigs(arm, body, donor_obj, skirt_on):
    """fit.json garmentRigs on a figure at rest: adds the chain bones to arm and their weights to body (and returns what it built)."""
    G = D['garmentRigs']; Mw = arm.matrix_world; Bb = arm.data.bones
    V = np.array([list(v.co) for v in body.data.vertices]); names = [g.name for g in body.vertex_groups]; W = weights_of(body, names)
    Vd = eval_co(donor_obj); Td = tris_of(donor_obj)
    bvh = BVHTree.FromPolygons([Vector(v) for v in Vd], [list(t) for t in Td], all_triangles=True)
    dist = np.array([bvh.find_nearest(Vector(v))[3] or 0.0 for v in V])
    z0 = float(V[:, 2].min()); H = float(V[:, 2].max()) - z0
    hz = lambda n: (Mw @ Bb[n].head_local).z
    hipZ = (hz('LeftUpLeg') + hz('RightUpLeg')) / 2; knee = (hz('LeftLeg') + hz('RightLeg')) / 2
    built = []; chains = {}; new_cols = []
    if skirt_on and G['ring'].get('enabled', True):
        R = G['ring']; hp = Mw @ Bb['Hips'].head_local; c = np.array([hp.x, hp.y])
        # the hem: from the knee down, the lowest height where the surface still crosses the centre line (the skirt joins the legs)
        hem = knee
        for z in np.arange(knee, z0, -0.005):
            if ((np.abs(V[:, 2] - z) < 0.004) & (np.abs(V[:, 0] - c[0]) < D['transfer']['bandMerge']['centreM'])).any(): hem = z
            else: break
        band = (V[:, 2] > hem - 0.01) & (V[:, 2] < hipZ)
        ang = np.arctan2(V[:, 0] - c[0], -(V[:, 1] - c[1]))            # 0 = front (-Y), + toward the figure's left
        n, nb = R['chains'], R['bones']; th = np.radians(R['angleOffsetDeg']) + 2 * np.pi * np.arange(n) / n
        bp.ctx(arm, 'EDIT'); eb = arm.data.edit_bones; Mi = Mw.inverted()
        for k in range(n):
            d = np.array([math.sin(th[k]), -math.cos(th[k])]); dang = np.abs(((ang - th[k] + np.pi) % (2 * np.pi)) - np.pi)
            pts = []
            for j in range(nb + 1):
                z = hipZ + (hem - hipZ) * j / nb; sel = band & (dang < np.pi / n) & (np.abs(V[:, 2] - z) < 0.02)
                rad = float(((V[sel][:, :2] - c) @ d).max()) if sel.any() else 0.1
                pts.append(Vector((c[0] + d[0] * rad * R['insetFrac'], c[1] + d[1] * rad * R['insetFrac'], z)))
            prev = eb[R['parent']]; names_k = []
            for j in range(nb):
                b = eb.new(f'Skirt_{k}_{j + 1}'); b.head = Mi @ pts[j]; b.tail = Mi @ pts[j + 1]; b.parent = prev; b.use_connect = j > 0; b.use_deform = True
                prev = b; names_k.append(b.name)
            chains[f'skirt_{k}'] = {'bones': names_k, 'parent': R['parent'], 'kind': 'ring'}
        bp.ctx(arm, 'OBJECT')
        # weights: angle between the two nearest chains, height along the chain, blended in at the top, kept off skin that hugs the body
        idx = np.nonzero(band)[0]; u = np.clip((hipZ - V[idx, 2]) / max(hipZ - hem, 1e-3), 0, 1)
        a = ((ang[idx] - th[0]) % (2 * np.pi)) / (2 * np.pi / n); k0 = np.floor(a).astype(int) % n; k1 = (k0 + 1) % n; fa = a - np.floor(a)
        hat = _hat(u, nb); cols = [f'Skirt_{k}_{j + 1}' for k in range(n) for j in range(nb)]
        Wc = np.zeros((len(idx), len(cols)))
        for j in range(nb):
            Wc[np.arange(len(idx)), k0 * nb + j] += (1 - fa) * hat[:, j]; Wc[np.arange(len(idx)), k1 * nb + j] += fa * hat[:, j]
        keep = _smooth((dist[idx] - R['tightM']) / (R['looseM'] - R['tightM']))
        if R.get('tightBelowKneeOnly'): keep = np.where(V[idx, 2] < knee, keep, 1.0)
        share = _smooth(u / R['blendTopFrac']) * keep
        new_cols += cols; W = np.hstack([W, np.zeros((len(V), len(cols)))]); base = W.shape[1] - len(cols)
        W[idx, :base] *= (1 - share)[:, None]; W[idx, base:] = Wc * share[:, None]
        W = spatial_avg(V, W, idx, R['mergeM'])
        built.append({'rig': 'ring', 'chains': n, 'bones': nb, 'hemZ': round(float(hem - z0), 4), 'hipZ': round(float(hipZ - z0), 4), 'verts': int(len(idx)), 'meanShare': round(float(share.mean()), 3)})
    # strand (ponytail)
    S = G['strand']; R2 = D['measureRegions']['ponytail']; sp = Mw @ Bb['Spine2'].head_local; nz = hz('Neck'); wz = hz('Spine1')
    pony = (dist > R2['looseM']) & (V[:, 1] > sp.y + R2['behindM']) & (V[:, 2] > wz) & (V[:, 2] < nz + R2['aboveNeckM'])
    if S.get('enabled', True) and pony.sum() >= S['minVerts']:
        P = V[pony]; top, bot = float(P[:, 2].max()), float(P[:, 2].min()); nb = S['bones']; pts = []
        for j in range(nb + 1):
            z = top + (bot - top) * j / nb; sel = np.abs(P[:, 2] - z) < max(0.02, (top - bot) / (2 * nb)); q = P[sel].mean(0) if sel.any() else P.mean(0)
            pts.append(Vector((float(q[0]), float(q[1]), z)))
        bp.ctx(arm, 'EDIT'); eb = arm.data.edit_bones; Mi = Mw.inverted(); prev = eb[S['parent']]; nm = []
        for j in range(nb):
            b = eb.new(f'Pony_{j + 1}'); b.head = Mi @ pts[j]; b.tail = Mi @ pts[j + 1]; b.parent = prev; b.use_connect = j > 0; b.use_deform = True; prev = b; nm.append(b.name)
        bp.ctx(arm, 'OBJECT'); chains['ponytail'] = {'bones': nm, 'parent': S['parent'], 'kind': 'strand'}
        idx = np.nonzero(pony)[0]; u = np.clip((top - V[idx, 2]) / max(top - bot, 1e-3), 0, 1); hat = _hat(u, nb)
        share = _smooth(u / S['blendTopFrac']) * _smooth((dist[idx] - S['tightM']) / (S['looseM'] - S['tightM']))
        W = np.hstack([W, np.zeros((len(V), nb))]); base = W.shape[1] - nb; new_cols += nm
        W[idx, :base] *= (1 - share)[:, None]; W[idx, base:] = hat * share[:, None]
        W = spatial_avg(V, W, idx, S['mergeM'])
        built.append({'rig': 'strand', 'bones': nb, 'verts': int(len(idx)), 'meanShare': round(float(share.mean()), 3)})
    if built:
        set_weights(body, W, names + new_cols, D['transfer']['maxInfluences'])
        arm['fit_chains'] = json.dumps(chains, sort_keys=True)
    return built


class ChainSim:
    """the deterministic chain solve (fit.json garmentRigs): Verlet joints, root pinned to the parent bone, spring to the rigid pose, lengths, capsules.
    Works in the armature's space; step() sets the chain bones' pose for the current frame and returns their basis quaternions."""
    def __init__(self, arm, dt):
        self.arm = arm; self.dt = dt; self.ch = json.loads(arm['fit_chains']); self.state = {}
        B = arm.data.bones
        self.rest = {}
        for key, c in self.ch.items():
            par = B[c['parent']]; P = par.matrix_local.inverted()
            joints = [B[c['bones'][0]].head_local] + [B[b].tail_local for b in c['bones']]
            self.rest[key] = ([P @ j for j in joints], [(B[b].tail_local - B[b].head_local).length for b in c['bones']])
        # collider radii: the body's half-thickness around each listed bone, measured once at rest from the body mesh's vertices weighted to it
        self.rad = {}; self.clamp = {}

    @staticmethod
    def _seg_dist(p, a, b):
        ab = b - a; t = max(0.0, min(1.0, (p - a).dot(ab) / max(ab.length_squared, 1e-12))); return (p - (a + ab * t)).length

    def set_radii(self, body, names_kind):
        V = eval_co(body); names = [g.name for g in body.vertex_groups]; Wb = weights_of(body, names); Mw = self.arm.matrix_world
        for kind, bones in names_kind.items():
            for b in bones:
                if b in self.rad or b not in names: continue
                sel = Wb[:, names.index(b)] > 0.6
                if not sel.any(): self.rad[b] = 0.06; continue
                bn = self.arm.data.bones[b]; a = np.array(Mw @ bn.head_local); t = np.array(Mw @ bn.tail_local); ax = (t - a) / np.linalg.norm(t - a)
                rel = V[sel] - a; perp = rel - np.outer(rel @ ax, ax); self.rad[b] = float(np.median(np.linalg.norm(perp, axis=1)))

    def step(self):
        G = D['garmentRigs']; arm = self.arm; bpy.context.view_layer.update(); Pb = arm.pose.bones; out = {}
        for key, c in self.ch.items():
            R = G['ring'] if c['kind'] == 'ring' else G['strand']
            par = Pb[c['parent']].matrix; jr, lens = self.rest[key]; target = [par @ j for j in jr]
            if key not in self.state: self.state[key] = ([t.copy() for t in target], [t.copy() for t in target])
            p, pp = self.state[key]; g = Vector((0, 0, -R['gravity'] * self.dt * self.dt))
            caps = [(Pb[b].head, Pb[b].tail, self.rad.get(b, 0.06) + R['marginM']) for b in R['colliders']]
            if R.get('colliderClampToRest'):
                if key not in self.clamp:                          # each joint's own rest distance from each capsule caps that capsule's radius for it
                    self.clamp[key] = [[min(r, self._seg_dist(t, a, b) * 0.98) for (a, b, r) in caps] for t in target]
                cl = self.clamp[key]
            else: cl = None
            q = [target[0].copy()]
            for j in range(1, len(p)):
                v = (p[j] - pp[j]) * (1 - R['damping']); x = p[j] + v + g; x = x + (target[j] - x) * R['stiffness']; q.append(x)
            for _ in range(R['iterations']):
                for j in range(1, len(q)):
                    d = q[j] - q[j - 1]; L_ = d.length or 1e-9; q[j] = q[j - 1] + d * (lens[j - 1] / L_)
                    for ci, (a, b, r) in enumerate(caps):
                        if cl is not None: r = cl[j][ci]
                        ab = b - a; t = max(0.0, min(1.0, (q[j] - a).dot(ab) / max(ab.length_squared, 1e-12))); cpt = a + ab * t; e = q[j] - cpt
                        if e.length < r: q[j] = cpt + (e.normalized() if e.length > 1e-9 else Vector((0, -1, 0))) * r
            self.state[key] = (q, p)
            for j, bn in enumerate(c['bones']):
                pb = Pb[bn]; bpy.context.view_layer.update(); bp.aim(pb, (q[j + 1] - q[j]).normalized())
            bpy.context.view_layer.update()
            for bn in c['bones']: out[bn] = tuple(round(x, 7) for x in Pb[bn].rotation_quaternion)
        return out



def hand_rule(V, T, W, names, arm):
    """fit.json transfer.hands: keep finger weights only where the mesh's fingers are articulated; else fold them into Hand (per hand)."""
    R = D['transfer'].get('hands')
    if not R: return W, {}
    bvh = BVHTree.FromPolygons([Vector(v) for v in V], [list(t) for t in T], all_triangles=True); Pb = arm.pose.bones; Mw = arm.matrix_world; out = {}
    def near(p): r = bvh.find_nearest(Vector(p)); return r[3] if r[0] is not None else 1.0
    for s_ in ('Left', 'Right'):
        fing = ['Index', 'Middle', 'Ring', 'Pinky']; align = []
        for f in fing + ['Thumb']:
            for i in (1, 2, 3):
                pb = Pb[f'{s_}Hand{f}{i}']; a = Mw @ pb.head; b = Mw @ pb.tail
                for t in np.linspace(0, 1, R['samples']): align.append(near(a + (b - a) * float(t)))
        gaps = [near((Mw @ Pb[f'{s_}Hand{a}2'].head + Mw @ Pb[f'{s_}Hand{b}2'].head) / 2) > R['gapM'] for a, b in zip(fing, fing[1:])]
        ok = max(align) < R['alignM'] and all(gaps)
        out[s_] = {'maxBoneToMeshM': round(float(max(align)), 4), 'gaps': sum(gaps), 'articulated': bool(ok)}
        if not ok:
            hi = names.index(f'{s_}Hand'); cols = [i for i, n in enumerate(names) if n.startswith(f'{s_}Hand') and n != f'{s_}Hand']
            W = W.copy(); W[:, hi] += W[:, cols].sum(1); W[:, cols] = 0
    return W, out


def layer_merge(V, W):
    R = D['transfer'].get('layerMerge')
    if not R: return W, 0
    return spatial_avg(V, W, np.arange(len(V)), R['mergeM']), int(len(V))


def reduce_lod(full, tris, name, arm, lod=0):
    """fit.json reduction: a copy of the full generated mesh reduced to tris by quadric collapse, the face and hands protected by a vertex-group
    weight, then the full mesh's normals carried over (data transfer, nearest face interpolated). Vertex groups follow the collapse."""
    R = D['reduction']; c = full.copy(); c.data = full.data.copy(); bpy.context.scene.collection.objects.link(c); c.name = name
    for m in list(c.modifiers): c.modifiers.remove(m)
    n = len(tris_of(c))
    if tris is not None and n > tris:
        if R.get('weldSeams'):
            # glTF splits vertices along UV seams: the mesh is open there and collapse pulls the two sides apart (cracks). Welded, the surface is
            # continuous and the seams live on as per-corner UVs.
            bm = bmesh.new(); bm.from_mesh(c.data); bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=R['weldSeams']); bm.to_mesh(c.data); bm.free(); c.data.update()
        names = [g.name for g in c.vertex_groups]; W = weights_of(c, names); V = np.array([list(v.co) for v in c.data.vertices])
        dom = np.array(names)[np.argmax(W, 1)] if names else np.array(['Hips'] * len(V))
        prot = np.zeros(len(V))
        if lod not in R.get('protectLods', [0, 1, 2]): pass
        elif 'hands' in R['protect']: prot[np.array(['Hand' in d for d in dom])] = 1.0
        if lod in R.get('protectLods', [0, 1, 2]) and 'face' in R['protect'] and 'Head' in arm.data.bones:
            hd = arm.matrix_world @ arm.data.bones['Head'].head_local
            face = np.isin(dom, ['Head', 'Neck'])
            if R.get('faceFrontOnly', False): face &= V[:, 1] < hd.y
            prot[face] = 1.0                                           # the whole head: face and hair are separate shells, and an unprotected hair shell opens dark cracks at the hairline
        if lod in R.get('protectLods', [0, 1, 2]) and 'boundaries' in R['protect']:
            # after the seam weld, the open edges left are real holes, such as the rim a cut-out cap leaves in the head (the cap hides it):
            # moving that rim opens slivers of the inside below the brim
            bm = bmesh.new(); bm.from_mesh(c.data)
            for e in bm.edges:
                if e.is_boundary:
                    for v in e.verts: prot[v.index] = 1.0
            bm.free()
        g = c.vertex_groups.new(name='_protect'); idx = np.nonzero(prot > 0)[0]
        if len(idx): g.add([int(i) for i in idx], 1.0, 'REPLACE')
        # protected faces resist collapse, so one pass stops short of the budget: the ratio is cut by the overshoot and the reduction redone from the
        # full mesh, up to `tries` times (deterministic: the same inputs give the same ratios)
        base = c.data.copy(); ratio = tris / n
        for _ in range(R.get('tries', 6)):
            c.data = base.copy()
            md = c.modifiers.new('dec', 'DECIMATE'); md.ratio = ratio; md.use_collapse_triangulate = True
            md.vertex_group = '_protect'; md.invert_vertex_group = True; md.vertex_group_factor = R['protectFactor']
            with bpy.context.temp_override(object=c, active_object=c): bpy.ops.object.modifier_apply(modifier=md.name)
            got = len(tris_of(c))
            if got <= tris * 1.02: break
            ratio *= tris / got
        bpy.data.meshes.remove(base)
        c.vertex_groups.remove(c.vertex_groups['_protect'])
        dt = c.modifiers.new('nrm', 'DATA_TRANSFER'); dt.object = full; dt.use_loop_data = True; dt.data_types_loops = {'CUSTOM_NORMAL'}
        dt.loop_mapping = R.get('normalMapping', 'POLYINTERP_LNORPROJ')     # along each corner's own normal: the nearest face can be a shell's inner side
        with bpy.context.temp_override(object=c, active_object=c): bpy.ops.object.modifier_apply(modifier=dt.name)
    md = c.modifiers.new('Armature', 'ARMATURE'); md.object = arm; c.parent = arm
    return c, len(tris_of(c))


def budget(row):
    return D['budgets'][row.get('kind', 'crowd')]


def role_props():
    """the held-prop roles (charpipe/props/props.json roles): weapon and holster per role"""
    return json.load(open(os.path.join(CP, 'props', 'props.json')))['roles']


def role_skips(row):
    """the test props a person doesn't get, by role: a role whose holster is visible keeps Attach_HipRight free (no cross-body bag there)"""
    r = role_props().get(row.get('role', ''), {})
    return ('bag',) if r.get('holster') == 'visible' else ()


FINGERS = ('Thumb', 'Index', 'Middle', 'Ring', 'Pinky')


def _comps(idx, adj):
    """connected components of the vertex subset idx (bool mask) under the edge adjacency adj (list of neighbour arrays)"""
    seen = np.zeros(len(idx), bool); out = []
    for s in np.nonzero(idx)[0]:
        if seen[s]: continue
        st = [s]; seen[s] = True; c = []
        while st:
            v = st.pop(); c.append(v)
            for u in adj[v]:
                if idx[u] and not seen[u]: seen[u] = True; st.append(u)
        out.append(np.array(c))
    return out


def finger_fit(src, arm):
    """fit.json transfer.fingers (brief 069, Sky: "sure"): the finger bones placed in the generated mesh's own fingers and weighted from them, so a
    52-bone figure's hand can close. In the rest (the generated pose), per hand:
      the hand's vertices (those its rigid Hand weight holds, beyond the wrist) are swept from the fingertips back along the hand's long axis; the
      fingers are the separate pieces of mesh beyond the deepest cut that still leaves the most pieces (the webs between the fingers). Fewer than
      minFingers pieces: the fingers are fused, the hand stays rigid (as before).
      each piece's own long axis (its principal direction) is the finger's line; its tip is the farthest point, its web the nearest; the knuckle
      (Finger1's head) lies back along the line from the web by webFrac of the proximal bone (the web sits that far along it), and the three
      bones split the knuckle-to-tip length in the base finger's own proportions. Each bone's roll puts its Z on the palm's normal, so a finger
      curls about its X.
      weights: every hand vertex within the finger's radius (radiusMul x its median distance from the line) and past its knuckle goes to that
      finger, split along the line between Hand, 1, 2, 3 with a linear blend blendFrac of a bone's length wide at each joint; the rest stay Hand.
    Returns stats per hand."""
    R = D['transfer'].get('fingers')
    if not R: return {}
    names = [g.name for g in src.vertex_groups]; W = weights_of(src, names)
    V = np.array([list(v.co) for v in src.data.vertices]) @ np.array(src.matrix_world)[:3, :3].T + np.array(src.matrix_world)[:3, 3]
    adj = [[] for _ in range(len(V))]
    for e in src.data.edges: a, b = e.vertices; adj[a].append(b); adj[b].append(a)
    # generated meshes are many separate shells: vertices of different shells closer than bridgeM count as joined (a gap between fingers is wider)
    from mathutils.kdtree import KDTree
    kd = KDTree(len(V))
    for i, p in enumerate(V): kd.insert(Vector(p), i)
    kd.balance()
    for i, p in enumerate(V):
        for _, j, _ in kd.find_range(Vector(p), R['bridgeM']):
            if j != i: adj[i].append(j)
    adj = [np.array(sorted(set(x)), int) for x in adj]
    bp.ctx(arm, 'EDIT'); eb = arm.data.edit_bones; Mw = arm.matrix_world; Mi = Mw.inverted(); out = {}
    def wh(n): return np.array(Mw @ eb[n].head)
    def wt(n): return np.array(Mw @ eb[n].tail)
    for s in ('Left', 'Right'):
        hand = f'{s}Hand'; hi = names.index(hand); h = wh(hand); fa = wh(f'{s}ForeArm')
        famask = np.array([n == hand or (n.startswith(hand) and n[len(hand):].rstrip('0123456789') in FINGERS) for n in names])
        dom = np.argmax(W, 1); mine = famask[dom]
        a0 = (h - fa) / np.linalg.norm(h - fa)
        sv = (V - h) @ a0; mine &= sv > 0.0
        if mine.sum() < R['minVerts']: out[s] = {'fitted': False, 'why': 'too few hand vertices'}; continue
        # the hand's own long axis: principal direction of its vertices, pointed away from the forearm
        Q = V[mine] - V[mine].mean(0); ax = np.linalg.svd(Q, full_matrices=False)[2][0]; ax = ax if ax @ a0 > 0 else -ax
        sv = (V - h) @ ax; smax = sv[mine].max(); minsz = max(R['minPieceVerts'], int(R['minPieceFrac'] * mine.sum()))
        best = None
        for t in np.arange(smax - R['stepM'], 0.0, -R['stepM']):
            pieces = [c for c in _comps(mine & (sv > t), adj) if len(c) >= minsz and sv[c].max() > smax - R['tipBandM']]   # pieces that reach the fingertips' band
            if best is None or len(pieces) >= len(best[1]): best = (t, pieces)
            if best is not None and len(pieces) < len(best[1]) and len(best[1]) >= R['minFingers']: break
        t, pieces = best
        if len(pieces) < R['minFingers']:
            out[s] = {'fitted': False, 'pieces': len(pieces), 'why': 'the fingers are not separated: the hand stays rigid'}; continue
        # which finger is which: across the palm from the thumb side; the thumb, when it is a piece, is the one reaching least far down the hand
        base = {f: (wh(f'{s}Hand{f}1'), wt(f'{s}Hand{f}3')) for f in FINGERS}
        across = base['Index'][0] - base['Pinky'][0]; across -= ax * (across @ ax); across /= np.linalg.norm(across)
        info = [(c, V[c].mean(0)) for c in pieces]
        thumb = None
        if len(info) >= 5:
            k = int(np.argmin([float((V[c] @ ax).max()) for c, _ in info])); thumb = info.pop(k)
        info = sorted(info, key=lambda x: -float(x[1] @ across))[:4]
        fit = dict(zip(('Index', 'Middle', 'Ring', 'Pinky'), info))
        if thumb is None:
            # the thumb parts from the palm nearer the wrist than the fingers do: below the fingers' cut, cut on down until the hand is in two
            # pieces, the palm and the thumb, the deepest cut before they join; the thumb is the one on the index side of the palm
            taken = np.zeros(len(V), bool)
            for c in pieces: taken[c] = True
            tb = None
            for t2 in np.arange(t - R['stepM'], 0.0, -R['stepM']):
                pc = [c for c in _comps(mine & (sv > t2) & ~taken, adj) if len(c) >= minsz]
                if len(pc) >= 2: tb = (t2, pc)
                elif tb is not None: break
            if tb is not None:
                pc = sorted(tb[1], key=len)
                cand = [c for c in pc[:-1] if float(V[c].mean(0) @ across) > float(V[pc[-1]].mean(0) @ across)]   # not the palm (the biggest); on the index side
                if cand: thumb = (cand[-1], V[cand[-1]].mean(0))
        if thumb is not None: fit['Thumb'] = thumb
        nrm = np.cross(base['Index'][0] - h, base['Pinky'][0] - h); nrm /= np.linalg.norm(nrm); nrm = nrm if s == 'Left' else -nrm   # the palm side
        lines = {}
        hand_len = float(np.linalg.norm(V[fit['Middle'][0]] - h, axis=1).max())                    # wrist to the middle fingertip, straight (along the hand's own axis it read 2 cm short: the fingers curve)
        Lmid0 = sum(np.linalg.norm(wt(f'{s}HandMiddle{i}') - wh(f'{s}HandMiddle{i}')) for i in (1, 2, 3))
        for f, (c, cen) in fit.items():
            X = V[c]; d = np.linalg.svd(X - X.mean(0), full_matrices=False)[2][0]
            tipdir = ax if f != 'Thumb' else (X.mean(0) - h) / np.linalg.norm(X.mean(0) - h)
            d = d if d @ tipdir > 0 else -d; u = (X - X.mean(0)) @ d
            tip = X.mean(0) + d * u.max(); web = X.mean(0) + d * u.min()
            bl = [np.linalg.norm(wt(f'{s}Hand{f}{i}') - wh(f'{s}Hand{f}{i}')) for i in (1, 2, 3)]; L0 = sum(bl)
            if f != 'Thumb' and R.get('knuckle'):
                # the true knuckle line (Sky via Aster: a hand that bends at the web is a wrong rig): each finger's knuckle-to-tip length from
                # the hand's own length (wrist to the middle fingertip along the hand) by the anatomical ratio, the other fingers in the base
                # skeleton's proportion to the middle; the knuckle lies that far back from the tip along the finger's line
                Lm = R['knuckle']['middleOfHandLength'] * hand_len * L0 / Lmid0
            else:
                wf = R['webFrac'][f if f == 'Thumb' else 'finger']
                Lm = np.linalg.norm(tip - web) / max(0.2, 1 - wf * bl[0] / L0)
            knuckle = tip - d * Lm; heads = [knuckle, knuckle + d * Lm * bl[0] / L0, knuckle + d * Lm * (bl[0] + bl[1]) / L0]
            for i in (1, 2, 3):
                e = eb[f'{s}Hand{f}{i}']; e.head = Vector(Mi @ Vector(heads[i - 1])); e.tail = Vector(Mi @ Vector(heads[i] if i < 3 else tip))
                e.align_roll(Mi.to_3x3() @ Vector(nrm))
            rad = float(np.median(np.linalg.norm((X - knuckle) - np.outer((X - knuckle) @ d, d), axis=1)))
            lines[f] = (knuckle, d, Lm, rad, [b / L0 * Lm for b in bl])
        # weights: hand vertices to the nearest fitted finger within its radius, blended along it; the rest Hand
        Wn = W.copy(); cols = {f: [names.index(f'{s}Hand{f}{i}') for i in (1, 2, 3)] for f in FINGERS}
        cand = np.nonzero(famask[dom] & (((V - h) @ ax) > -0.02))[0]; assigned = 0
        for v in cand:
            best_f, best_r = None, 1e9
            for f, (kn, d, Lm, rad, seg) in lines.items():
                u = (V[v] - kn) @ d
                if u < -R['blendFrac'] * seg[0] or u > Lm + 0.01: continue
                r = np.linalg.norm((V[v] - kn) - u * d)
                if r < R['radiusMul'] * rad and r / rad < best_r: best_f, best_r = f, r / rad
            if best_f is None: continue
            kn, d, Lm, rad, seg = lines[best_f]; u = (V[v] - kn) @ d
            joints = [0.0, seg[0], seg[0] + seg[1]]; bones = [hi] + cols[best_f]; w = np.zeros(len(names))
            k = 0
            while k < 3 and u > joints[k]: k += 1          # the bone u lies in: 0 = still Hand, k = Finger k
            k = max(0, k); wv = {bones[k]: 1.0}
            for j in range(3):                               # blend across the joint at joints[j] between bones[j] and bones[j+1]
                half = R['blendFrac'] * seg[j] / 2
                if abs(u - joints[j]) < half:
                    t_ = (u - joints[j] + half) / (2 * half); wv = {bones[j]: 1 - t_, bones[j + 1]: t_}
            for b, x in wv.items(): w[b] = x
            Wn[v] = w; assigned += 1
        W = Wn
        out[s] = {'fitted': True, 'handLengthM': round(hand_len, 4), 'pieces': len(pieces), 'cutM': round(float(t), 4), 'thumb': thumb is not None, 'verts': assigned,
                  'fingers': {f: {'lengthM': round(float(v[2]), 4), 'radiusM': round(float(v[3]), 4)} for f, v in lines.items()}}
        if thumb is None:   # the thumb not a separate piece: its bones keep no weight (it rides the hand)
            W[:, cols['Thumb']] = 0
    bp.ctx(arm, 'OBJECT')
    set_weights(src, W, names, D['transfer']['maxInfluences'])
    return out


def relax_rigid_hands(src, arm, crowd):
    """fit.json transfer.fingers.rigidRelax (brief 069, Sky: "hands pose looks unnatural"): a rigid hand (the crowd's 22-bone skeleton, or a
    52-bone hand whose fingers are fused, so finger_fit left every vertex on Hand) keeps the generated pose's hand in every clip: straight and
    spread. So its fingers are bent into a gentle relaxed curl in the mesh itself, in the rest (the fitted pose), before the LODs are made:
      the hand: the vertices Hand leads, past the wrist; its line: their principal direction away from the forearm; its length: wrist to the
      farthest of them; the palm side: the hand mesh's own plane (its thinnest principal axis), signed by the fitted base fingers' (Index1, Pinky1), as finger_fit;
      the knuckle line: knuckleFrac of the length from the wrist (finger_fit's knuckle rule: the fingers are middleOfHandLength of the hand);
      past it, every vertex bends about the line across the palm at the knuckles as a uniform arc of curlDeg over the finger length (toward the
      palm), and its distance across the hand from the hand's line shrinks by togetherFrac at the tips (spread to together);
      the custom normals turn with their vertices. Returns stats per hand."""
    R = D['transfer'].get('fingers', {}).get('rigidRelax')
    if not R: return {}
    names = [g.name for g in src.vertex_groups]; W = weights_of(src, names); dom = np.argmax(W, 1)
    Mw = np.array(src.matrix_world); V = np.array([list(v.co) for v in src.data.vertices]) @ Mw[:3, :3].T + Mw[:3, 3]
    me = src.data; custom = me.has_custom_normals
    if custom:
        LN = np.empty(len(me.loops) * 3); me.corner_normals.foreach_get('vector', LN); LN = LN.reshape(-1, 3) @ Mw[:3, :3].T
        LV = np.empty(len(me.loops), np.int64); me.loops.foreach_get('vertex_index', LV)
    B = arm.data.bones; Am = arm.matrix_world; j = lambda n: np.array(Am @ B[n].head_local)
    out = {}; Vn = V.copy(); rot = {}
    for s in ('Left', 'Right'):
        fing = [i for i, n in enumerate(names) if n.startswith(f'{s}Hand') and n != f'{s}Hand']
        hi = names.index(f'{s}Hand') if f'{s}Hand' in names else None
        if hi is None: continue
        if not crowd and fing and W[:, fing].sum() > 1e-6: out[s] = {'rigid': False}; continue
        h = j(f'{s}Hand'); fa = j(f'{s}ForeArm'); a0 = (h - fa) / np.linalg.norm(h - fa)
        mine = (dom == hi) & (((V - h) @ a0) > 0)
        if mine.sum() < 50: out[s] = {'rigid': True, 'relaxed': False, 'why': 'too few hand vertices'}; continue
        Q = V[mine] - V[mine].mean(0); ax = np.linalg.svd(Q, full_matrices=False)[2][0]; ax = ax if ax @ a0 > 0 else -ax
        nrm = np.cross(j(f'{s}HandIndex1') - h, j(f'{s}HandPinky1') - h); nrm /= np.linalg.norm(nrm); nrm = nrm if s == 'Left' else -nrm   # the palm side
        n3 = np.linalg.svd(Q, full_matrices=False)[2][2]; nrm = n3 if n3 @ nrm > 0 else -n3   # the hand mesh's own plane (its thinnest axis), on the palm side
        nrm = nrm - ax * (nrm @ ax); nrm /= np.linalg.norm(nrm); acr = np.cross(ax, nrm)
        sv = (V - h) @ ax; L_ = float(sv[mine].max()); k0 = R['knuckleFrac'] * L_; Lf = L_ - k0
        th = math.radians(R['curlDeg']); Rr = Lf / th
        idx = np.nonzero(mine & (sv > k0))[0]
        kn = h + ax * k0; c = V[mine].mean(0); c_acr = float((c - h) @ acr)
        for i in idx:
            d = V[i] - kn; u = float(d @ ax); w = float(d @ nrm); x = float(d @ acr)
            phi = u / Rr; r = Rr - w
            # the arc: centre on the palm side at Rr; distance along it u, off it -w
            pu = r * math.sin(phi); pw = Rr - r * math.cos(phi)
            t = min(1.0, u / Lf); x2 = c_acr + (x + float((kn - h) @ acr) - c_acr) * (1 - R['togetherFrac'] * t) - float((kn - h) @ acr)
            Vn[i] = kn + ax * pu + nrm * pw + acr * x2
            rot[i] = (ax, nrm, acr, phi)
        out[s] = {'rigid': True, 'relaxed': True, 'verts': int(len(idx)), 'handLengthM': round(L_, 4), 'curlDeg': R['curlDeg']}
    if rot:
        set_co(src, (Vn - Mw[:3, 3]) @ np.linalg.inv(Mw[:3, :3]).T)
        if custom:
            for li, vi in enumerate(LV):
                if vi in rot:
                    ax, nrm, acr, phi = rot[vi]; n = LN[li]; a, b = n @ ax, n @ nrm
                    LN[li] = ax * (a * math.cos(phi) - b * math.sin(phi)) + nrm * (a * math.sin(phi) + b * math.cos(phi)) + acr * (n @ acr)   # the arc's own turn at that vertex
            LNo = LN @ np.linalg.inv(Mw[:3, :3]).T; LNo /= np.linalg.norm(LNo, axis=1, keepdims=True)
            me.normals_split_custom_set([tuple(x) for x in LNo])
    return out


def coat_blend(V, W, names, lab, lm):
    """fit.json transfer.coatBlend: an open coat (its panels part at the front, so the band merge does not fire) is a standard game coat: the
    vertices labelled coat below the hip joints take hipShare of their weight from the hips (blended in from the hip line down over fadeM), the
    rest stays with the legs: the panels follow the legs at half the swing instead of tearing with each thigh."""
    R = D['transfer'].get('coatBlend')
    if not R or not R.get('enabled', True): return W, {'applied': False, 'parked': True}
    m = np.isin(lab, R['labels']) & (V[:, 2] < lm['hip'])
    if not m.any(): return W, {'applied': False}
    hi = names.index('Hips'); legs = [i for i, n in enumerate(names) if any(k in n for k in ('UpLeg', 'Leg', 'Foot', 'ToeBase'))]
    t = np.clip((lm['hip'] - V[m, 2]) / R['fadeM'], 0, 1)[:, None] * R['hipShare']
    Wm = W[m].copy(); legw = Wm[:, legs].sum(1, keepdims=True)
    Wm[:, legs] *= (1 - t); Wm[:, hi:hi + 1] += legw * t
    W = W.copy(); W[m] = Wm; s = W.sum(1, keepdims=True); s[s == 0] = 1
    return W / s, {'applied': True, 'verts': int(m.sum())}
