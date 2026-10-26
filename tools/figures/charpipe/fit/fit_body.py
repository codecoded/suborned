"""Stage 1 (brief 069 fit test): fit the MPFB base inside a generated mesh: sliders first, then joint positions (the pose).
  out/scratch/forma/char-pipeline/blender.sh -P tools/figures/charpipe/fit/fit_body.py -- <input id> [...]
For each input: import the glb, normalise (lib.normalise: up, facing, feet at 0, a nominal 1.75 m), build its signed distance grid, then for each sex
minimise (fit.json 'fit'): outsideW * mean(max(sd,0)^2) + gapW * mean(clip(-sd - gapFreeM, 0, gapCapM)^2) over the proxy's vertices (fingers left out), by a
coordinate pattern search over: global scale and offset, spine lean, weight, muscle, proportions, height, each arm's drop, swing and elbow, each leg's
spread, swing and knee. The sex with the lower loss wins. Writes fit-test/fit/<id>.fit.json and <id>_src.blend (the normalised source)."""
import os, sys, json, math, time, importlib.util
import numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
L = importlib.util.module_from_spec(s := importlib.util.spec_from_file_location('fitlib', os.path.join(HERE, 'lib.py'))); s.loader.exec_module(L)
import bpy

FD = L.D['fit']; OUT = L.ensure(os.path.join(L.SCR, 'fit'))
FRONT = np.array([0.0, -1.0, 0.0])


class Body:
    """the linear MPFB model of one sex (basis.py)."""
    def __init__(self, sex):
        z = np.load(os.path.join(L.SCR, 'basis', f'basis_{sex}.npz'))
        self.z = z; self.sex = sex; self.names = [str(x) for x in z['names']]; self.par = [int(x) for x in z['par']]
        self.macros = L.D['basis']['macros']
        self.W = z['proxy_W']; self.tris = z['proxy_tris']
        fing = [i for i, n in enumerate(self.names) if 'Hand' in n and n[-1].isdigit()]
        self.use = self.W[:, fing].sum(1) < 0.3

    def shape(self, mac, key):
        V = self.z[f'ref_{key}'].copy()
        for m in self.macros:
            x = mac.get(m, 0.5)
            if x >= 0.5: V += (x - 0.5) / 0.5 * (self.z[f'{m}_1_{key}' if False else f'{m}_1_{key}'] - self.z[f'ref_{key}'])
            else: V += (0.5 - x) / 0.5 * (self.z[f'{m}_0_{key}'] - self.z[f'ref_{key}'])
        return V

    def rig(self, mac):
        return L.Rig(self.names, self.shape(mac, 'H'), self.shape(mac, 'T'), self.par)


PARAMS = [  # name, init (None = measured), step, lo, hi
    ('s', None, 0.02, 0.6, 1.6), ('tx', 0.0, 0.01, -0.1, 0.1), ('ty', 0.0, 0.01, -0.15, 0.15), ('tz', 0.0, 0.01, -0.15, 0.15),
    ('lean', 0.0, 0.03, -0.3, 0.3),
    ('weight', 0.5, 0.1, *FD['ranges']['weight']), ('muscle', 0.5, 0.1, *FD['ranges']['muscle']), ('proportions', 0.5, 0.1, *FD['ranges']['proportions']),
    ('height', 0.5, 0.1, *FD['ranges']['height']),
    ('aL', None, 0.08, -0.2, 1.5), ('fL', 0.0, 0.06, -0.6, 0.6), ('eL', 0.05, 0.08, 0.0, 1.2),
    ('aR', None, 0.08, -0.2, 1.5), ('fR', 0.0, 0.06, -0.6, 0.6), ('eR', 0.05, 0.08, 0.0, 1.2),
    ('oL', 0.03, 0.03, -0.15, 0.4), ('gL', 0.0, 0.03, -0.3, 0.3), ('kL', 0.0, 0.04, 0.0, 0.5),
    ('oR', 0.03, 0.03, -0.15, 0.4), ('gR', 0.0, 0.03, -0.3, 0.3), ('kR', 0.0, 0.04, 0.0, 0.5),
]
PI = {p[0]: i for i, p in enumerate(PARAMS)}


def perp(v, ref):
    r = ref - v * np.dot(ref, v); n = np.linalg.norm(r); return r / n if n > 1e-9 else r


def aims_of(x, rig):
    """the pose as world directions (the same dict drives lib.pose_blender for the real rig)."""
    g = lambda k: x[PI[k]]
    aims = {}
    for side, sx in (('Left', 1.0), ('Right', -1.0)):
        k = side[0]
        a, f, e = g('a' + k), g('f' + k), g('e' + k)
        d = np.array([sx * math.cos(a) * math.cos(f), -math.cos(a) * math.sin(f), -math.sin(a)]); d /= np.linalg.norm(d)
        d2 = d * math.cos(e) + perp(d, FRONT) * math.sin(e)
        aims[side + 'Arm'] = d; aims[side + 'ForeArm'] = d2
        o, gg, kk = g('o' + k), g('g' + k), g('k' + k)
        t = np.array([sx * math.sin(o), -math.sin(gg) * math.cos(o), -math.cos(o) * math.cos(gg)]); t /= np.linalg.norm(t)
        t2 = t * math.cos(kk) + perp(t, -FRONT) * math.sin(kk)
        aims[side + 'UpLeg'] = t; aims[side + 'Leg'] = t2
        for b in ('Foot', 'ToeBase'):
            i = rig.n.index(side + b); v = rig.T[i] - rig.H[i]; aims[side + b] = v / np.linalg.norm(v)
    return aims, {'Spine': L.rotx(g('lean'))}


def posed(body, x, Vkey='proxy', W=None):
    mac = {m: x[PI[m]] for m in body.macros}
    rig = body.rig(mac); V = body.shape(mac, Vkey)
    aims, pre = aims_of(x, rig); R, Hp = rig.pose(aims, pre)
    Vp = L.lbs(V, body.W if W is None else W, R, rig.H, Hp)
    return Vp * x[PI['s']] + np.array([x[PI['tx']], x[PI['ty']], x[PI['tz']]]), rig, aims, pre


def loss(body, sdf, x, parts=False):
    Vp, *_ = posed(body, x)
    d = sdf(Vp[body.use])
    out = np.maximum(d, 0); gap = np.clip(-d - FD['gapFreeM'], 0, FD['gapCapM'])
    reg = 0.002 * sum((x[PI[m]] - 0.5) ** 2 for m in body.macros) + 0.001 * (x[PI['eL']] ** 2 + x[PI['eR']] ** 2 + x[PI['kL']] ** 2 + x[PI['kR']] ** 2)
    v = FD['outsideW'] * float(np.mean(out ** 2)) + FD['gapW'] * float(np.mean(gap ** 2)) + reg
    if parts:
        return v, {'outsideMeanMm': round(1000 * float(out.mean()), 2), 'outsideP95Mm': round(1000 * float(np.percentile(out, 95)), 1),
                   'pctOutside5mm': round(100 * float((d > 0.005).mean()), 2), 'gapMedianMm': round(1000 * float(np.median(-d[d < 0])), 1) if (d < 0).any() else None}
    return v


def init(body, Vm):
    x = np.array([p[1] if p[1] is not None else 0.0 for p in PARAMS], float)
    V0 = body.shape({}, 'proxy'); hb = np.ptp(V0[:, 2]); hm = np.ptp(Vm[:, 2])
    x[PI['s']] = hm / hb * 0.97
    rig = body.rig({})
    for side, sx in (('Left', 1.0), ('Right', -1.0)):
        sh = rig.H[rig.n.index(side + 'Arm')] * x[PI['s']]
        cand = Vm[(Vm[:, 0] * sx > 0) & (Vm[:, 2] > 0.35 * hm) & (Vm[:, 2] < sh[2])]
        hand = cand[np.argmax(cand[:, 0] * sx)]
        x[PI['a' + side[0]]] = math.atan2(sh[2] - hand[2], abs(hand[0] - sh[0]))
    return x


def search(body, sdf, x):
    steps = np.array([p[2] for p in PARAMS]); lo = np.array([p[3] for p in PARAMS]); hi = np.array([p[4] for p in PARAMS])
    best = loss(body, sdf, x); evals = 1
    for rnd in range(FD['iterations']):
        improved = True; sweeps = 0
        while improved and sweeps < 40:
            improved = False; sweeps += 1
            for i in range(len(PARAMS)):
                for sgn in (1, -1):
                    y = x.copy(); y[i] = min(hi[i], max(lo[i], y[i] + sgn * steps[i]))
                    if y[i] == x[i]: continue
                    v = loss(body, sdf, y); evals += 1
                    if v < best - 1e-12: best, x, improved = v, y, True; break
        steps *= 0.5
    return x, best, evals


def main():
    ids = sys.argv[sys.argv.index('--') + 1:]
    rows = {r['id']: r for r in L.D['inputs']}
    for fid in ids:
        r = rows[fid]; t0 = time.time()
        L.reset() if False else bpy.ops.wm.read_factory_settings(use_empty=True)
        o = L.import_glb(L.store(r['src'])); o.name = f'{fid}_src'
        norm = L.normalise(o)
        Vm = np.array([list(v.co) for v in o.data.vertices]); Tm = L.tris_of(o)
        t_imp = time.time() - t0
        sdf = L.SDF(Vm, Tm, FD['voxelM']); t_sdf = time.time() - t0 - t_imp
        res = {}
        for sex in ('m', 'f'):
            body = Body(sex); x = init(body, Vm)
            x, v, ev = search(body, sdf, x)
            v, parts = loss(body, sdf, x, parts=True)
            res[sex] = (v, x, parts, ev)
            L.log(fid, sex, 'loss', round(v * 1e4, 3), parts, 'evals', ev)
        tighter = min(res, key=lambda k: res[k][0]); sex = r['sex']; v, x, parts, ev = res[sex]
        body = Body(sex); Vp, rig, aims, pre = posed(body, x)
        out = {'id': fid, 'src': r['src'], 'normalise': norm, 'sex': sex, 'loss': {k: round(res[k][0] * 1e4, 4) for k in res}, 'tighterSex': tighter,
               'params': {p[0]: round(float(x[i]), 5) for i, p in enumerate(PARAMS)}, 'macros': {m: round(float(x[PI[m]]), 4) for m in body.macros},
               'aims': {k: [round(float(c), 6) for c in v] for k, v in aims.items()}, 'pre': {k: v.tolist() for k, v in pre.items()},
               'quality': parts, 'srcTris': int(len(Tm)), 'seconds': {'import': round(t_imp, 2), 'sdf': round(t_sdf, 2), 'search': round(time.time() - t0 - t_imp - t_sdf, 2)}}
        json.dump(out, open(os.path.join(OUT, f'{fid}.fit.json'), 'w'), indent=1)
        # the posed proxy as a check mesh next to the source
        L.mesh_obj(f'{fid}_fitproxy', Vp, body.tris)
        bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, f'{fid}_src.blend'), compress=True)
        L.log('DONE', fid, sex, out['macros'], parts, out['seconds'])


main()
