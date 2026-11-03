# Redline 075: kit facade roots, triangles (by LOD) and base draws in view, for two exports (before/after T-K1), same frusta as
# briefs/074/redline/colourway_draws.py (ortho play zoom 14 at 38 deg; tp-street / tp-worst perspective, LOD by distance 45/120/330 m, culled past 330).
# Ortho play views draw L0. Triangles = kit.json tris per piece per LOD; columns are baked per root, so a root's tris = the sum of its modules'.
# All COMPUTED. Run: python3 -I briefs/075/redline/tk1_tris.py <before.world.json> <after.world.json> <kit.json> <out.json>
import json, math, sys
from collections import defaultdict
import numpy as np
bp, ap, kp, op = sys.argv[1:5]
K = json.load(open(kp)); kit = {a['id']: a for a in K['assets']}
D2R = math.pi / 180
def tris(pid):
    a = kit[pid]; t = [a['tris']] + [l['tris'] for l in a.get('lods', [])]
    while len(t) < 3: t.append(t[-1])
    return t[:3]
def roots(W):
    by = defaultdict(list)
    for f in W['facades']:
        k = f['kit']; a = kit[k['id']]; s = k['scale']
        if 'normal' in f: n = np.array(f['normal'], float)
        else:
            e = np.array(f['b'], float) - np.array(f['a'], float); e /= np.linalg.norm(e); n = np.array([-e[1], e[0]])
        t = np.array([n[1], -n[0]]); c = np.array(k['at'], float)
        w, d = a['size'][0]*s[0], a['size'][2]*s[2]; z0 = f['base']; z1 = z0 + a['size'][1]*s[1]
        col = (f['column']['id'] if f.get('column') else f['id']) if 'family' in f else f['id']
        pts = []
        for sx in (-1, 1):
            for sd in (-1, 1):
                p = c + t*sx*w/2 + n*sd*d/2; pts += [(p[0], z0, p[1]), (p[0], z1, p[1])]
        by[col].append((np.array(pts), tris(k['id']), (k['id'], round(s[1], 3))))
    R = []
    for cid, ms in by.items():
        P = np.vstack([m[0] for m in ms]); T = np.sum([m[1] for m in ms], axis=0)
        R.append(dict(lo=P.min(0), hi=P.max(0), t=T, var=tuple(m[2] for m in ms)))
    return R
def ortho(F, yaw, zoom, aspect=16/9, pitch=38):
    y = yaw*D2R; p = pitch*D2R; right = np.array([math.cos(y), 0, -math.sin(y)]); up = np.array([math.sin(p)*math.sin(y), math.cos(p), math.sin(p)*math.cos(y)])
    hw, hh = zoom*aspect, zoom; F = np.array(F, float)
    return [(right, -right@F + hw), (-right, right@F + hw), (up, -up@F + hh), (-up, up@F + hh)]
def inside(lo, hi, P):
    return all(nn @ np.where(nn >= 0, hi, lo) + dd >= 0 for nn, dd in P)
def persp(cam, target, fov=55, aspect=16/9, far=350.0):
    f = target - cam; f /= np.linalg.norm(f); r = np.cross(np.array([0, 1, 0.0]), f); r /= np.linalg.norm(r); u = np.cross(f, r)
    th = math.tan(fov/2*D2R); tw = th*aspect
    def pl(nn): nn = nn/np.linalg.norm(nn); return (nn, -nn@cam)
    return [pl(f*th - u), pl(f*th + u), pl(f*tw - r), pl(f*tw + r), pl(f), (-f, f@cam + far)]
FOCI = {'office-front': [-160, 0, -281], 'cell-open': [-170.5, 0, -227.5], 'cafe-square': [-159, 0, -221.5], 'heaviest': [-60, 0, -200],
        'parade': [212, 0, 0], 'crown-circus': [0, 0, 0], 'north-edge': [-220, 0, 280], 'harbour-quay': [321, 9.43, -506.5], 'wide-lamplight': [-215, 0, -120]}
def views(R):
    out = {}
    for name, F in FOCI.items():
        for yaw in (30, 120, 210, 300):
            v = [r for r in R if inside(r['lo'], r['hi'], ortho(F, yaw, 14))]
            out[f'{name}@{yaw}/14'] = dict(roots=len(v), tris=int(sum(r['t'][0] for r in v)), draws=len({(r['var'], 'L0') for r in v}))
    for name, walk, heading in (('tp-street', [0, -20], 180), ('tp-worst', [-10, -25], 90)):
        P = np.array([-174.3 + walk[0], 0.18, -228.4 + walk[1]]); h = heading*D2R; fw = np.array([math.sin(h), 0, math.cos(h)])
        cam = P - fw*5.5 + np.array([0, 1.3 + 0.32*5.5, 0]); tgt = P + fw*8 + np.array([0, 1.35, 0]); pl = persp(cam, tgt)
        n = 0; t = 0; dr = set()
        for r in R:
            if not inside(r['lo'], r['hi'], pl): continue
            d = np.linalg.norm((r['lo'] + r['hi'])/2 - cam); L = 0 if d < 45 else 1 if d < 120 else 2 if d < 330 else 3
            if L < 3: n += 1; t += int(r['t'][L]); dr.add((r['var'], L))
        out[name] = dict(roots=n, tris=t, draws=len(dr))
    return out
B, A = roots(json.load(open(bp))), roots(json.load(open(ap)))
vb, va = views(B), views(A)
res = dict(before=bp, after=ap, district=dict(roots=[len(B), len(A)], L0=[int(sum(r['t'][0] for r in B)), int(sum(r['t'][0] for r in A))],
           L1=[int(sum(r['t'][1] for r in B)), int(sum(r['t'][1] for r in A))], L2=[int(sum(r['t'][2] for r in B)), int(sum(r['t'][2] for r in A))]),
           views={k: dict(before=vb[k], after=va[k]) for k in vb})
json.dump(res, open(op, 'w'), indent=1)
print('district', res['district'])
for k in vb:
    if vb[k] != va[k]: print(f'{k:24s}', vb[k], '->', va[k])
print('unchanged views:', sum(vb[k] == va[k] for k in vb), 'of', len(vb))
