# Redline 072-D phase 2: the every-face census and per-view counts from the export's OWN modules (export v4: facades with a `family`),
# priced with the catalogue's real triangles (contracts/world/kit.json: tris / lods[].tris, Vela's 072-A table as adopted).
# Same view machinery as every_face.py (phase 1), so the model can be checked against Latch's batchmode counts.
# Run: python3 -I briefs/072/redline/every_face_v4.py <world.json> <kit.json> <out.json>     (read-only on inputs; all numbers COMPUTED)
import json, math, sys
from collections import Counter, defaultdict
import numpy as np

world_p, kit_p, out_p = sys.argv[1:4]
W = json.load(open(world_p)); K = json.load(open(kit_p))
kit = {a['id']: a for a in K['assets']}
D2R = math.pi / 180

def tri(pid):
    a = kit[pid]; l = [a['tris']] + [x['tris'] for x in a.get('lods', [])]
    while len(l) < 3: l.append(l[-1])
    return l[:3]

mods = []
for f in W['facades']:
    if 'family' not in f: continue
    k = f['kit']; a = kit[k['id']]; s = k['scale']
    n = np.array(f['normal'], float); t = np.array([n[1], -n[0]])
    w = a['size'][0] * s[0]; d = a['size'][2] * s[2]; h = a['size'][1] * s[1]
    c = np.array(k['at'], float)
    mods.append(dict(piece=k['id'], fam=f['family'], c=c, n=n, t=t, w=w, d=d, z0=f['base'], z1=f['base'] + h,
                     col=f['column']['id'] if f.get('column') else f['id'], cls=f['lod'].get('class'), yaw=k['yaw']))
# module roots as KitColumns places them: a column of 2+ modules with one yaw = one baked root; else each module its own root
bycol = defaultdict(list)
for m in mods: bycol[m['col']].append(m)
roots = []
for cid, ms in bycol.items():
    if len(ms) > 1 and all(abs(x['yaw'] - ms[0]['yaw']) < 1e-4 for x in ms): roots.append(ms)
    else: roots.extend([[x] for x in ms])

def aabb_mod(m):
    pts = [m['c'] + m['t'] * sx * m['w'] / 2 + m['n'] * sd * m['d'] / 2 for sx in (-1, 1) for sd in (-1, 1)]
    xs = [p[0] for p in pts]; ys = [p[1] for p in pts]
    return np.array([min(xs), m['z0'], min(ys)]), np.array([max(xs), m['z1'], max(ys)])
def aabb_root(r):
    bs = [aabb_mod(m) for m in r]
    return np.min([b[0] for b in bs], axis=0), np.max([b[1] for b in bs], axis=0)

R = []
for r in roots:
    lo, hi = aabb_root(r)
    t3 = np.sum([tri(m['piece']) for m in r], axis=0)
    R.append(dict(mods=r, lo=lo, hi=hi, tris=t3, size=float(np.max(hi - lo)), n=r[0]['n'], cls='column' if len(r) > 1 else r[0]['cls']))

def plane_test(lo, hi, planes):
    for nn, dd in planes:
        p = np.where(nn >= 0, hi, lo)
        if np.dot(nn, p) + dd < 0: return False
    return True
def ortho_planes(F, yaw, zoom, aspect=16 / 9, pitch=38):
    y = yaw * D2R; p = pitch * D2R
    right = np.array([math.cos(y), 0, -math.sin(y)]); up = np.array([math.sin(p) * math.sin(y), math.cos(p), math.sin(p) * math.cos(y)])
    hw, hh = zoom * aspect, zoom; F = np.array(F, float)
    return [(right, -np.dot(right, F) + hw), (-right, np.dot(right, F) + hw), (up, -np.dot(up, F) + hh), (-up, np.dot(up, F) + hh)]
def fwd_of(yaw, pitch):
    y = yaw * D2R; p = pitch * D2R
    return np.array([math.cos(p) * math.sin(y), -math.sin(p), math.cos(p) * math.cos(y)])
SUN_TO = -fwd_of(330, 38)
def spheres(F, yaw, zoom, standoff=160.0, aspect=16 / 9):
    f = fwd_of(yaw, 38); F = np.array(F, float); cam = F - f * standoff; hw, hh = zoom * aspect, zoom; out = []
    for d0, d1 in ((0.0, standoff - 60), (standoff - 60, standoff + 40)):
        out.append((cam + f * (d0 + d1) / 2, math.sqrt(hw * hw + hh * hh + ((d1 - d0) / 2) ** 2)))
    return out
def spheres_fixed(F, yaw, r=57.504, standoff=160.0):
    """Latch's measured cascades: both culling spheres radius 57.5 m (offscreen.json); centres on the view axis at the two slices' middles."""
    f = fwd_of(yaw, 38); F = np.array(F, float); cam = F - f * standoff
    return [(cam + f * (standoff - 60) / 2, r), (cam + f * (standoff - 10), r)]
def is_caster(lo, hi, sph):
    c = (lo + hi) / 2; rho = np.linalg.norm(hi - lo) / 2
    for cen, rad in sph:
        v = c - cen; tt = np.dot(v, SUN_TO)
        if tt < -rad - rho: continue
        if np.linalg.norm(v - SUN_TO * tt) <= rad + rho: return True
    return False

FOCI = {'office-front': [-160, 0, -281], 'cell-open': [-170.5, 0, -227.5], 'home': [-159, 0, -232.5], 'cafe-square': [-159, 0, -221.5],
        'heaviest': [-60, 0, -200], 'parade': [212, 0, 0], 'crown-circus': [0, 0, 0], 'north-edge': [-220, 0, 280], 'harbour-quay': [321, 9.43, -506.5]}
views = []
for name, F in FOCI.items():
    for yaw in (30, 120, 210, 300):
        for zoom in (14, 18.5):
            P = ortho_planes(F, yaw, zoom); sph = spheres(F, yaw, zoom); sphf = spheres_fixed(F, yaw)
            vis = [r for r in R if plane_test(r['lo'], r['hi'], P)]
            cast = [r for r in R if is_caster(r['lo'], r['hi'], sph)]
            castf = [r for r in R if is_caster(r['lo'], r['hi'], sphf)]
            pieces = sorted({m['piece'] for r in vis for m in r['mods']})
            views.append(dict(view=f'{name}@{yaw}', zoom=zoom, roots=len(vis), modules=sum(len(r['mods']) for r in vis),
                              tris_l0=int(sum(r['tris'][0] for r in vis)), pieces=len(pieces),
                              casters=len(cast), caster_tris_l2=int(sum(r['tris'][2] for r in cast)), caster_tris_l0=int(sum(r['tris'][0] for r in cast)),
                              casters_r57=len(castf), caster_tris_l2_r57=int(sum(r['tris'][2] for r in castf))))

# ground (perspective) rows, as phase 1, with the classes' distances [45, 120, 330]
def persp_planes(cam, target, fov=55, aspect=16 / 9, far=350.0):
    f = target - cam; f /= np.linalg.norm(f); r = np.cross(np.array([0, 1, 0.0]), f); r /= np.linalg.norm(r); u = np.cross(f, r)
    th = math.tan(fov / 2 * D2R); tw = th * aspect
    def pl(nn): nn = nn / np.linalg.norm(nn); return (nn, -np.dot(nn, cam))
    return [pl(f * th - u), pl(f * th + u), pl(f * tw - r), pl(f * tw + r), pl(f), (-f, np.dot(f, cam) + far)]
def tp(name, walk, heading):
    P = np.array([-174.3 + walk[0], 0.18, -228.4 + walk[1]]); h = heading * D2R; fw = np.array([math.sin(h), 0, math.cos(h)])
    cam = P - fw * 5.5 + np.array([0, 1.3 + 0.32 * 5.5, 0]); tgt = P + fw * 8 + np.array([0, 1.35, 0])
    planes = persp_planes(cam, tgt); vis = [r for r in R if plane_test(r['lo'], r['hi'], planes)]
    lv = [0, 0, 0, 0]; tris = 0; far = 0
    for r in vis:
        d = np.linalg.norm((r['lo'] + r['hi']) / 2 - cam)
        L = 0 if d < 45 else 1 if d < 120 else 2 if d < 330 else 3   # by distance (KitLods.ScreenAt(size, D) makes the switch at D for its own size)
        lv[L] += 1; tris += 0 if L == 3 else r['tris'][L]; far += d >= 150
    return dict(view=name, roots_in_frustum=len(vis), by_level=dict(L0=lv[0], L1=lv[1], L2=lv[2], culled=lv[3]), tris=int(tris), roots_far150=int(far))

res = dict(inputs=dict(world=world_p, kit=kit_p), modules=len(mods), roots=len(R), baked_columns=sum(1 for r in roots if len(r) > 1),
           singles=sum(1 for r in roots if len(r) == 1), census=dict(Counter(m['piece'] for m in mods)),
           district_tris=dict(zip(('L0', 'L1', 'L2'), [int(x) for x in np.sum([r['tris'] for r in R], axis=0)])),
           cost_table={p: dict(L0=tri(p)[0], L1=tri(p)[1], L2=tri(p)[2], mats=len(kit[p]['materials'])) for p in sorted({m['piece'] for m in mods})},
           play=views, ground=[tp('tp-street', [0, -20], 180), tp('tp-worst', [-10, -25], 90)])
json.dump(res, open(out_p, 'w'), indent=1)
print(json.dumps({k: res[k] for k in ('modules', 'roots', 'baked_columns', 'singles', 'district_tris', 'ground')}, indent=1))
for v in views:
    if v['view'].split('@')[0] in ('office-front', 'cell-open') and v['zoom'] == 14: print(v)
