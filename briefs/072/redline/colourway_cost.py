# Redline 072-D phase 3 addendum: what per-building colourways on the every-face modules cost, from the export's own modules.
# Draw model (GPU Resident Drawer): one instanced draw per distinct (mesh variant, submesh material) visible in a view. Recolouring
# slots (wall DK_Limestone_Tex, band DK_Enamel_Seafoam, fascia DK_Enamel_Teal) splits only those slots' draws by colourway; the other
# slots (window film, chrome, ivory, gold...) stay one draw across colourways. Ortho play views draw L0 only.
# Colourway per building = its wall family (facade.json areas weights, building -> area by its most common frontage area), optionally
# capped to K per area (the K most-weighted families; the rest fold onto them). Family pick is a stand-in hash (FacadeKit's Dice is C#):
# the distribution matches the weights, not the exact building.
# Run: python3 -I briefs/072/redline/colourway_cost.py <world.json> <kit.json> <facade.json> <out.json>   (read-only; all numbers COMPUTED)
import json, math, sys, hashlib
from collections import Counter, defaultdict
import numpy as np
world_p, kit_p, fac_p, out_p = sys.argv[1:5]
W = json.load(open(world_p)); K = json.load(open(kit_p)); FAC = json.load(open(fac_p))
kit = {a['id']: a for a in K['assets']}
RECOL = {'DK_Limestone_Tex': 'wall', 'DK_Enamel_Seafoam': 'band', 'DK_Enamel_Teal': 'fascia'}
D2R = math.pi / 180
areas = FAC['areas']
area_of = defaultdict(Counter)
for fr in W['frontages']:
    if fr.get('area'): area_of[fr['host']][fr['area']] += 1
def area(b):
    if b in area_of: return area_of[b].most_common(1)[0][0]
    for a in areas:
        if a in b: return a
    return 'harbour'
def h01(s): return int(hashlib.sha256(s.encode()).hexdigest()[:8], 16) / 2**32
def family(b, cap=None):
    a = area(b); fams = sorted(areas[a]['families'].items(), key=lambda kv: (-kv[1], kv[0]))
    tot = sum(v for _, v in fams); r = h01(b) * tot
    for i, (n, v) in enumerate(fams):
        if r < v: break
        r -= v
    if cap and i >= cap: i = int(h01(b + '#fold') * cap)
    return a + ':' + fams[i][0] if cap else fams[i][0]
mods = []
for f in W['facades']:
    if 'family' not in f: continue
    k = f['kit']; a = kit[k['id']]; s = k['scale']; n = np.array(f['normal'], float); t = np.array([n[1], -n[0]])
    mods.append(dict(piece=k['id'], b=f['building'], c=np.array(k['at'], float), n=n, t=t, w=a['size'][0]*s[0], d=a['size'][2]*s[2],
                     z0=f['base'], z1=f['base'] + a['size'][1]*s[1], col=f['column']['id'] if f.get('column') else f['id'], yaw=k['yaw'], st=round(s[1], 3)))
bycol = defaultdict(list)
for m in mods: bycol[m['col']].append(m)
roots = []
for cid, ms in bycol.items():
    if len(ms) > 1 and all(abs(x['yaw'] - ms[0]['yaw']) < 1e-4 for x in ms): roots.append(sorted(ms, key=lambda x: x['z0']))
    else: roots.extend([[x] for x in ms])
def aabb(r):
    pts = []
    for m in r:
        for sx in (-1, 1):
            for sd in (-1, 1):
                p = m['c'] + m['t']*sx*m['w']/2 + m['n']*sd*m['d']/2; pts.append((p[0], m['z0'], p[1])); pts.append((p[0], m['z1'], p[1]))
    P = np.array(pts); return P.min(0), P.max(0)
R = []
for r in roots:
    lo, hi = aabb(r)
    var = tuple((m['piece'], m['st']) for m in r) if len(r) > 1 else ((r[0]['piece'], r[0]['st']),)
    mats = sorted({x for m in r for x in kit[m['piece']]['materials']})
    R.append(dict(lo=lo, hi=hi, var=var, mats=mats, b=r[0]['b']))
def ortho(F, yaw, zoom, aspect=16/9, pitch=38):
    y = yaw*D2R; p = pitch*D2R; right = np.array([math.cos(y), 0, -math.sin(y)]); up = np.array([math.sin(p)*math.sin(y), math.cos(p), math.sin(p)*math.cos(y)])
    hw, hh = zoom*aspect, zoom; F = np.array(F, float)
    return [(right, -right@F + hw), (-right, right@F + hw), (up, -up@F + hh), (-up, up@F + hh)]
def inside(lo, hi, P):
    for nn, dd in P:
        if nn @ np.where(nn >= 0, hi, lo) + dd < 0: return False
    return True
FOCI = {'office-front': [-160, 0, -281], 'cell-open': [-170.5, 0, -227.5], 'cafe-square': [-159, 0, -221.5], 'heaviest': [-60, 0, -200],
        'parade': [212, 0, 0], 'crown-circus': [0, 0, 0], 'north-edge': [-220, 0, 280], 'harbour-quay': [321, 9.43, -506.5]}
SCEN = {'base': lambda b: 'base', 'wall-family': lambda b: family(b), 'cap4-area': lambda b: family(b, 4), 'cap3-area': lambda b: family(b, 3),
        'cap2-area': lambda b: family(b, 2), 'per-building': lambda b: b}
def count(vis, cw):
    draws = set(); mats = set(); cws = set()
    for r in vis:
        c = cw(r['b']); cws.add(c)
        for m in r['mats']:
            k = (m, c) if m in RECOL and c != 'base' else (m, 'base')
            draws.add((r['var'], k)); mats.add(k)
    return len(draws), len(mats), len(cws)
rows = []
for name, F in FOCI.items():
    for yaw in (30, 120, 210, 300):
        vis = [r for r in R if inside(r['lo'], r['hi'], ortho(F, yaw, 14))]
        row = dict(view=f'{name}@{yaw}', roots=len(vis), buildings=len({r['b'] for r in vis}), variants=len({r['var'] for r in vis}))
        for s, cw in SCEN.items(): row[s] = dict(zip(('draws', 'materials', 'colourways'), count(vis, cw)))
        rows.append(row)
dist = {}
for s, cw in SCEN.items():
    d, m, c = count(R, cw); dist[s] = dict(draws=d, materials=m, colourways=c)
per_area = defaultdict(set)
for r in R: per_area[area(r['b'])].add(family(r['b']))
per_piece_area = Counter()
for s in ('wall-family', 'cap4-area'):
    pa = defaultdict(set)
    for r in R:
        for p, _ in r['var']: pa[(p, area(r['b']))].add(SCEN[s](r['b']))
    per_piece_area[s] = max(len(v) for v in pa.values())
res = dict(inputs=dict(world=world_p, kit=kit_p, facade=fac_p), roots=len(R), recolour_slots=RECOL, district=dist,
           families_per_area={a: len(v) for a, v in per_area.items()}, max_colourways_per_piece_area=dict(per_piece_area), play14=rows)
json.dump(res, open(out_p, 'w'), indent=1)
print(json.dumps({k: res[k] for k in ('roots', 'district', 'families_per_area', 'max_colourways_per_piece_area')}, indent=1))
hdr = ['view', 'roots', 'bld', 'var'] + [f'{s}' for s in SCEN]
print(' | '.join(hdr))
for r in rows: print(r['view'], r['roots'], r['buildings'], r['variants'], ' | '.join(f"{r[s]['draws']}d/{r[s]['materials']}m/{r[s]['colourways']}c" for s in SCEN))
# ground (perspective) rows and the wide play zoom 18.5, as every_face_v4.py (far 350 m; LOD by distance only changes the mesh, so a
# root's LOD level enters the draw key)
def persp(cam, target, fov=55, aspect=16/9, far=350.0):
    f = target - cam; f /= np.linalg.norm(f); r = np.cross(np.array([0, 1, 0.0]), f); r /= np.linalg.norm(r); u = np.cross(f, r)
    th = math.tan(fov/2*D2R); tw = th*aspect
    def pl(nn): nn = nn/np.linalg.norm(nn); return (nn, -nn@cam)
    return [pl(f*th - u), pl(f*th + u), pl(f*tw - r), pl(f*tw + r), pl(f), (-f, f@cam + far)]
def tpview(walk, heading):
    P = np.array([-174.3 + walk[0], 0.18, -228.4 + walk[1]]); h = heading*D2R; fw = np.array([math.sin(h), 0, math.cos(h)])
    cam = P - fw*5.5 + np.array([0, 1.3 + 0.32*5.5, 0]); tgt = P + fw*8 + np.array([0, 1.35, 0])
    out = []
    for r in R:
        if not inside(r['lo'], r['hi'], persp(cam, tgt)): continue
        d = np.linalg.norm((r['lo'] + r['hi'])/2 - cam); L = 0 if d < 45 else 1 if d < 120 else 2 if d < 330 else 3
        if L < 3: out.append(dict(r, var=r['var'] + (('L', L),)))
    return out
extra = []
for name, vis in (('tp-street', tpview([0, -20], 180)), ('tp-worst', tpview([-10, -25], 90))):
    row = dict(view=name, roots=len(vis), buildings=len({r['b'] for r in vis}), variants=len({r['var'] for r in vis}))
    for s, cw in SCEN.items(): row[s] = dict(zip(('draws', 'materials', 'colourways'), count(vis, cw)))
    extra.append(row)
for name, F in FOCI.items():
    for yaw in (30, 120, 210, 300):
        vis = [r for r in R if inside(r['lo'], r['hi'], ortho(F, yaw, 18.5))]
        if not vis: continue
        row = dict(view=f'{name}@{yaw}/18.5', roots=len(vis), buildings=len({r['b'] for r in vis}), variants=len({r['var'] for r in vis}))
        for s, cw in SCEN.items(): row[s] = dict(zip(('draws', 'materials', 'colourways'), count(vis, cw)))
        extra.append(row)
res['ground_and_wide'] = extra; json.dump(res, open(out_p, 'w'), indent=1)
for r in extra: print(r['view'], r['roots'], r['buildings'], r['variants'], ' | '.join(f"{r[s]['draws']}d/{r[s]['materials']}m/{r[s]['colourways']}c" for s in SCEN))
