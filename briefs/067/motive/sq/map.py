# Motive brief 067 square probe: a top-down map of the getaway car's square (north up = block -x; east right = block +y), from a site file:
# the walk grid (blocked cells grey), the café and buildings, the cover points (green dot + a tick toward what shields them), the fight posts,
# the car with its 8 m hold ring and the exit lane, the keep-clear zones (civic-block dressing), lamps, bollards, the removed planter runs
# (dashed), extra candidate objects, and optional traces (trace.jsonl from the scratch engine's `trace` command: one colour per body).
# usage: python3 -I map.py <site.json> <block.json> <out.png> [--trace f.jsonl[:label]] [--extra extra.json] [--box x0 x1 y0 y1] [--scale s] [--title t]
import json, sys, math
from PIL import Image, ImageDraw, ImageFont
a = sys.argv[1:]; site = json.load(open(a[0])); block = json.load(open(a[1])); out = a[2]
opt = {}; i = 3; traces = []
while i < len(a):
    if a[i] == '--trace': traces.append(a[i + 1]); i += 2
    elif a[i] == '--only': opt['only'] = a[i + 1].split(','); i += 2
    elif a[i] == '--box': opt['box'] = [float(v) for v in a[i + 1:i + 5]]; i += 5
    else: opt[a[i][2:]] = a[i + 1]; i += 2
X0, X1, Y0, Y1 = opt.get('box', [-22, 42, -68, -14]); S = float(opt.get('scale', 14))
extra = json.load(open(opt['extra'])) if 'extra' in opt else {}
W, H = int((Y1 - Y0) * S), int((X1 - X0) * S) + 40
im = Image.new('RGB', (W, H), (250, 248, 240)); d = ImageDraw.Draw(im, 'RGBA')
F = lambda n: ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial.ttf', n)
font, small, big = F(12), F(10), F(15)
def P(x, y): return ((y - Y0) * S, (x - X0) * S + 40)
g = site['grid']; c = g['cell'] / 100; gx0 = g['x0'] / 100; gy0 = g['y0'] / 100
for i2, row in enumerate(g['rows']):
    x = gx0 + i2 * c
    if x + c < X0 or x > X1: continue
    for j, ch in enumerate(row):
        y = gy0 + j * c
        if y + c < Y0 or y > Y1: continue
        if ch != '.':
            p0 = P(x, y); p1 = P(x + c, y + c); d.rectangle([p0[0], p0[1], p1[0], p1[1]], fill=(205, 200, 190))
# keep-clear zones
for z in block.get('dressing', {}).get('keepClear', []):
    if 'polygon' in z:
        pts = [P(*p) for p in z['polygon']]; d.polygon(pts, outline=(120, 120, 220, 160), fill=(120, 120, 220, 22))
        q = P(*z['polygon'][0]); d.text((q[0] + 2, q[1] + 2), z['id'], fill=(80, 80, 180), font=small)
    elif 'ring' in z:
        cx, cy = z['ring']['centre']
        for r in (z['ring']['r0'], z['ring']['r1']):
            p0 = P(cx - r, cy - r); p1 = P(cx + r, cy + r); d.ellipse([p0[0], p0[1], p1[0], p1[1]], outline=(120, 120, 220, 140))
for o in site['occluders']:
    pts = [P(p[0] / 100, p[1] / 100) for p in o['poly']]
    if all(not (X0 - 5 <= p[0] / 100 <= X1 + 5 and Y0 - 5 <= p[1] / 100 <= Y1 + 5) for p in o['poly']): continue
    d.polygon(pts, outline=(60, 60, 80), fill=(100, 100, 120, 110))
for b in site.get('cover', {}).get('blockers', []):
    d.polygon([P(p[0] / 100, p[1] / 100) for p in b['poly']], outline=(0, 90, 0), fill=(40, 120, 40, 120))
# exit lane and hold ring
gw = site['getaway']; car = site['places'][gw['car']]['at']; cx, cy = car[0] / 100, car[1] / 100
lane = gw['exit']; hw = lane['w'] / 2
for k in range(1, len(lane['polyline'])):
    (ax, ay), (bx, by) = [(q[0] / 100, q[1] / 100) for q in lane['polyline'][k - 1:k + 1]]
    L = math.hypot(bx - ax, by - ay); nx_, ny_ = -(by - ay) / L * hw, (bx - ax) / L * hw
    d.polygon([P(ax + nx_, ay + ny_), P(bx + nx_, by + ny_), P(bx - nx_, by - ny_), P(ax - nx_, ay - ny_)], outline=(200, 120, 0), fill=(240, 160, 40, 40))
r = 8; p0 = P(cx - r, cy - r); p1 = P(cx + r, cy + r); d.ellipse([p0[0], p0[1], p1[0], p1[1]], outline=(220, 90, 0), width=2)
q = P(cx, cy); d.rectangle([q[0] - 0.95 * S, q[1] - 2.3 * S, q[0] + 0.95 * S, q[1] + 2.3 * S], outline=(0, 0, 0), fill=(255, 200, 80, 200)); d.text((q[0] + 14, q[1] - 6), 'getaway car', fill=(0, 0, 0), font=font)
# removed planter runs (dashed outline)
for run in block.get('dressing', {}).get('planterRuns', {}).get('removed', []):
    for m in run['modules']:
        mx, my = m['at']; yaw = math.radians(run['yawDeg']); ux, uy = math.cos(yaw), math.sin(yaw)
        hl, hw2 = 2.15, 0.34
        corners = [(mx + ux * sx * hl - uy * sy * hw2, my + uy * sx * hl + ux * sy * hw2) for sx, sy in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
        pts = [P(*p) for p in corners]
        for k2 in range(4):
            d.line([pts[k2], pts[(k2 + 1) % 4]], fill=(160, 60, 60), width=1)
    q = P(*run['modules'][0]['at']); d.text((q[0] + 6, q[1] + 4), run['id'] + ' (removed)', fill=(160, 60, 60), font=small)
for lp in block.get('dressing', {}).get('lamps', {}).get('at', []):
    q = P(*lp['at']); d.ellipse([q[0] - 3, q[1] - 3, q[0] + 3, q[1] + 3], fill=(240, 200, 0), outline=(120, 100, 0))
for bo in block.get('dressing', {}).get('bollards', {}).get('at', []):
    q = P(*bo['at']); d.ellipse([q[0] - 2, q[1] - 2, q[0] + 2, q[1] + 2], fill=(90, 90, 90))
# cover points: dot + tick toward the object (faces), length by arc
for cp in site.get('cover', {}).get('points', []):
    x, y = cp['at'][0] / 100, cp['at'][1] / 100
    if not (X0 <= x <= X1 and Y0 <= y <= Y1): continue
    q = P(x, y); fx, fy = cp['faces'][0] / 1000, cp['faces'][1] / 1000
    col = (0, 140, 0) if cp['q'] >= 600 else (120, 170, 0)
    d.ellipse([q[0] - 3, q[1] - 3, q[0] + 3, q[1] + 3], fill=col); e = P(x + fx * 0.9, y + fy * 0.9); d.line([q, e], fill=col, width=2)
    if cp['kind'] != 'car': d.text((q[0] + 3, q[1] + 2), cp['id'][3:], fill=(0, 100, 0), font=small)
# posts
pl = site['places']
def post(pid, col, label=None):
    if pid not in pl: return
    x, y = (pl[pid].get('stand') or pl[pid]['at']); x /= 100; y /= 100
    q = P(x, y); d.rectangle([q[0] - 5, q[1] - 5, q[0] + 5, q[1] + 5], outline=col, width=2)
    d.text((q[0] + 7, q[1] - 8), (label or pid) + f' {math.hypot(x - cx, y - cy):.1f} m', fill=col, font=font)
for k in site['fight']['ambush']: post(k, (150, 0, 170))
for k in site['fight']['police']: post(k, (0, 60, 200))
post(site['fight']['guard'], (200, 120, 0))
post('rival-drop', (150, 0, 170))
for e in extra.get('objects', []):
    pts = [P(*p) for p in e['poly']]; d.polygon(pts, outline=(200, 0, 0), fill=(255, 80, 80, 120)); q = P(*e['poly'][0]); d.text((q[0] + 3, q[1] - 14), e['id'], fill=(180, 0, 0), font=font)
for e in extra.get('points', []):
    q = P(*e['at']); d.ellipse([q[0] - 5, q[1] - 5, q[0] + 5, q[1] + 5], outline=tuple(e.get('col', (200, 0, 120))), width=2); d.text((q[0] + 6, q[1] + 2), e['id'], fill=tuple(e.get('col', (200, 0, 120))), font=font)
# traces
COL = {'scout': (0, 150, 220), 'fixer': (0, 170, 90), 'tech': (230, 140, 0), 'enforcer': (200, 0, 0), 'vale': (0, 0, 0)}
RC = [(150, 0, 170), (120, 60, 200), (190, 40, 140), (90, 20, 120), (210, 90, 210), (60, 0, 90)]
for tr in traces:
    f, _, lab = tr.partition(':'); rows = [json.loads(l) for l in open(f) if l.startswith('{"t"')]
    paths = {}
    for rw in rows:
        for k2, v in rw['ops'].items():
            if not v['car'] and not v['in']: paths.setdefault(k2, []).append((rw['t'], v['at'], v['down']))
        if rw['vale']: paths.setdefault('vale', []).append((rw['t'], rw['vale'], False))
        for k2, v in rw['h'].items():
            if v['at']: paths.setdefault(k2, []).append((rw['t'], v['at'], v['down']))
    ri = 0
    for k2, pts in paths.items():
        if 'only' in opt and k2 not in opt['only']: continue
        col = COL.get(k2) or RC[(ri := ri + 1) % len(RC)]
        xy = [P(p[1][0] / 100, p[1][1] / 100) for p in pts if X0 <= p[1][0] / 100 <= X1 and Y0 <= p[1][1] / 100 <= Y1]
        if len(xy) > 1: d.line(xy, fill=col + (200,), width=3 if lab else 2)
        if xy:
            end = pts[-1]; q = P(end[1][0] / 100, end[1][1] / 100)
            if X0 <= end[1][0] / 100 <= X1 and Y0 <= end[1][1] / 100 <= Y1:
                d.text((q[0] + 4, q[1] - 12), k2 + (' (down)' if end[2] else ''), fill=col, font=small)
for x in range(int(math.ceil(X0 / 5) * 5), int(X1) + 1, 5):
    p0 = P(x, Y0); d.line([p0, P(x, Y1)], fill=(0, 0, 0, 22)); d.text((2, p0[1] + 1), f'x {x}', fill=(0, 0, 0, 140), font=small)
for y in range(int(math.ceil(Y0 / 5) * 5), int(Y1) + 1, 5):
    p0 = P(X0, y); d.line([p0, P(X1, y)], fill=(0, 0, 0, 22)); d.text((p0[0] + 1, 42), f'y {y}', fill=(0, 0, 0, 140), font=small)
d.text((8, 8), opt.get('title', 'the square at the getaway car (block frame, metres; north up)'), fill=(0, 0, 0), font=big)
im.save(out); print(out, W, H)
