# Redline 074-D: draws under the GPU Resident Drawer, today (every kit facade `base`) against the export's real colourways (074 export).
# Same model as briefs/072/redline/colourway_cost.py (phase 3 §5): one instanced draw per distinct (mesh variant [+LOD], submesh material)
# visible in a view; ortho play views draw L0; ground views pick LOD by distance (45/120/330 m). Differences from phase 3: the colourway is
# read from the export (not a stand-in hash), Vela's mapping recolours 5 base slots (kit.json colourways.slots: wall DK_Limestone_Tex and
# DK_Render_White, band DK_Enamel_Seafoam, fascia DK_Enamel_Teal, plinth DK_Stone_Dark), and the 924 non-module street pieces are counted
# too (Latch swaps them). Shadow proxies keep the base set, so shadow draws are unchanged and not counted. All numbers COMPUTED.
# Run: python3 -I briefs/074/redline/colourway_draws.py <world.json> <kit.json> <out.json> [slot,slot,...  (what-if: recolour only these)]
import json, math, sys
from collections import defaultdict, Counter
import numpy as np
world_p, kit_p, out_p = sys.argv[1:4]
W = json.load(open(world_p)); K = json.load(open(kit_p))
kit = {a['id']: a for a in K['assets']}
CW = K['colourways']; RECOL = set(CW['slots']); KEEP = set(CW.get('keep', []))
if len(sys.argv) > 4: RECOL = set(sys.argv[4].split(','))  # what-if: recolour only these base slots
D2R = math.pi / 180
mods = []
for f in W['facades']:
    k = f['kit']; a = kit[k['id']]; s = k['scale']; y = k['yaw']
    if 'normal' in f: n = np.array(f['normal'], float)
    else:  # street piece: perpendicular of its edge a->b (the AABB is symmetric, so the sign does not matter)
        e = np.array(f['b'], float) - np.array(f['a'], float); e /= np.linalg.norm(e); n = np.array([-e[1], e[0]])
    t = np.array([n[1], -n[0]])
    module = 'family' in f
    col = (f['column']['id'] if f.get('column') else f['id']) if module else f['id']
    mods.append(dict(piece=k['id'], b=f['building'], cw=f.get('colourway') or 'base', c=np.array(k['at'], float), n=n, t=t,
                     w=a['size'][0]*s[0], d=a['size'][2]*s[2], z0=f['base'], z1=f['base'] + a['size'][1]*s[1], col=col, yaw=y,
                     st=round(s[1], 3), module=module))
bycol = defaultdict(list)
for m in mods: bycol[m['col']].append(m)
roots = []
for cid, ms in bycol.items():
    if len(ms) > 1 and all(abs(x['yaw'] - ms[0]['yaw']) < 1e-4 and x['cw'] == ms[0]['cw'] for x in ms): roots.append(sorted(ms, key=lambda x: x['z0']))
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
    var = tuple((m['piece'], m['st']) for m in r)
    mats = sorted({x for m in r for x in kit[m['piece']]['materials']})
    R.append(dict(lo=lo, hi=hi, var=var, mats=mats, b=r[0]['b'], cw=r[0]['cw'], module=r[0]['module']))
def ortho(F, yaw, zoom, aspect=16/9, pitch=38):
    y = yaw*D2R; p = pitch*D2R; right = np.array([math.cos(y), 0, -math.sin(y)]); up = np.array([math.sin(p)*math.sin(y), math.cos(p), math.sin(p)*math.cos(y)])
    hw, hh = zoom*aspect, zoom; F = np.array(F, float)
    return [(right, -right@F + hw), (-right, right@F + hw), (up, -up@F + hh), (-up, up@F + hh)]
def inside(lo, hi, P):
    for nn, dd in P:
        if nn @ np.where(nn >= 0, hi, lo) + dd < 0: return False
    return True
def count(vis, colour):
    out = {}
    for part in ('module', 'street', 'all'):
        draws = set(); mats = set(); cws = set()
        for r in vis:
            if part == 'module' and not r['module']: continue
            if part == 'street' and r['module']: continue
            c = r['cw'] if colour and r['cw'] not in KEEP else 'base'; cws.add(c)
            for m in r['mats']:
                k = (m, c) if m in RECOL and c != 'base' else (m, 'base')
                draws.add((r['var'], k)); mats.add(k)
        out[part] = dict(draws=len(draws), materials=len(mats), colourways=len(cws))
    return out
def row(name, vis):
    b, a = count(vis, False), count(vis, True)
    return dict(view=name, roots=len(vis), buildings=len({r['b'] for r in vis}), before=b, after=a,
                delta={p: a[p]['draws'] - b[p]['draws'] for p in a})
FOCI = {'office-front': [-160, 0, -281], 'cell-open': [-170.5, 0, -227.5], 'cafe-square': [-159, 0, -221.5], 'heaviest': [-60, 0, -200],
        'parade': [212, 0, 0], 'crown-circus': [0, 0, 0], 'north-edge': [-220, 0, 280], 'harbour-quay': [321, 9.43, -506.5],
        'wide-lamplight': [-215, 0, -120]}
play, wide = [], []
for name, F in FOCI.items():
    for yaw in (30, 120, 210, 300):
        play.append(row(f'{name}@{yaw}/14', [r for r in R if inside(r['lo'], r['hi'], ortho(F, yaw, 14))]))
        vis = [r for r in R if inside(r['lo'], r['hi'], ortho(F, yaw, 18.5))]
        if vis: wide.append(row(f'{name}@{yaw}/18.5', vis))
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
ground = [row('tp-street', tpview([0, -20], 180)), row('tp-worst', tpview([-10, -25], 90))]
dist = row('district', R)
res = dict(inputs=dict(world=world_p, kit=kit_p), recolour_slots=sorted(RECOL), keep=sorted(KEEP), roots=len(R),
           roots_by_kind=Counter('module' if r['module'] else 'street' for r in R), colourways=Counter(r['cw'] for r in R),
           district=dist, ground=ground, play14=play, wide185=wide)
json.dump(res, open(out_p, 'w'), indent=1)
def fmt(r): return f"{r['view']:24s} roots {r['roots']:4d} bld {r['buildings']:3d} | module {r['before']['module']['draws']:4d}->{r['after']['module']['draws']:4d} ({r['delta']['module']:+d}) | street {r['before']['street']['draws']:4d}->{r['after']['street']['draws']:4d} ({r['delta']['street']:+d}) | all {r['before']['all']['draws']:4d}->{r['after']['all']['draws']:4d} ({r['delta']['all']:+d}) | mats {r['before']['all']['materials']}->{r['after']['all']['materials']} cw {r['after']['all']['colourways']}"
print('roots', len(R), dict(res['roots_by_kind']))
print(fmt(dist))
for r in ground: print(fmt(r))
print('-- play 14'); [print(fmt(r)) for r in play]
print('-- wide 18.5'); [print(fmt(r)) for r in wide]
d14 = [r['delta']['all'] for r in play]; d18 = [r['delta']['all'] for r in wide]
print('play14 delta all: min', min(d14), 'max', max(d14), '| wide18.5 max', max(d18))
