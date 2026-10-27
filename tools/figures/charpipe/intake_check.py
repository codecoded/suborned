"""Intake stage, step 3 (brief 069 v2): the pose check. A gate on a generated sheet, not a dial: it measures the still exactly as it measures the base's
own mannequin sheet (intake_pose.py) and fails loudly when a number is out of intake.json's tolerance. Deterministic, numpy only.
  python3 -I tools/figures/charpipe/intake_check.py <sheet.png> <mannequin_<sex>.png> <out prefix>
Writes <out>.json (every number, the base's, the verdict) and <out>.png (the still with the base's silhouette edge, scaled to each view, in red, and the
measured arm lines and feet marked).
What it measures, per view (each view is its third of the sheet; the figure = pixels unlike the ground, the ground = the sheet's border colour):
  height    top of the head to the soles, in px; the three views must agree (same scale)
  arms      front view: rows where each arm stands apart from the body; a straight line through each arm's centre; its angle below the horizontal
  legs      front view: rows between the ankles and the crotch where the legs stand apart
  feet      front view: the two feet in the lowest 2.5 % of the height: their spacing / height, their widths; side view: the foot length / height;
            a toe turned out by y widens the foot seen from the front: width = w0 cos y + L sin y, solved for y with the base's w0 and the still's L."""
import json, math, sys
import numpy as np
from PIL import Image, ImageDraw

HERE = __file__.rsplit('/', 1)[0]
_CHK = json.load(open(HERE + '/intake.json'))['check']; TOL = _CHK['tolerances']; LONG = _CHK.get('longSkirt')


def mask_of(a):
    h, w, _ = a.shape
    border = np.concatenate([a[:6].reshape(-1, 3), a[-6:].reshape(-1, 3), a[:, :6].reshape(-1, 3), a[:, -6:].reshape(-1, 3)])
    bg = np.median(border, 0); flat = float(np.abs(border - bg).max(1).mean())
    d = np.abs(a.astype(float) - bg).max(2)
    m = d > max(8.0, 4 * flat + 6)            # a flat ground lets the threshold sit low; a noisy one raises it
    # close tiny holes in each row (stitching, specular): a 1-2 px gap in a run is not a gap
    return m, bg, flat


def runs(row, minlen=3, merge=2):
    out = []; x = np.flatnonzero(row)
    if not len(x): return out
    start = prev = x[0]
    for v in x[1:]:
        if v - prev > merge + 1:
            if prev - start + 1 >= minlen: out.append((start, prev))
            start = v
        prev = v
    if prev - start + 1 >= minlen: out.append((start, prev))
    return out


def view_metrics(m, kind):
    rows = np.flatnonzero(m.any(1)); cols = np.flatnonzero(m.any(0))
    if not len(rows): return None
    top, bot = rows[0], rows[-1]; H = bot - top
    cx = (cols[0] + cols[-1]) / 2
    r = {'top': int(top), 'bottom': int(bot), 'heightPx': int(H), 'left': int(cols[0]), 'right': int(cols[-1]), 'touchesEdge': bool(cols[0] <= 1 or cols[-1] >= m.shape[1] - 2)}
    yb = lambda f: int(round(bot - f * H))                                                    # row at a fraction of the height above the soles
    if kind == 'front':
        # the body's own column: the run holding the centre at mid-thigh height
        # each side's arm = the whole span of everything apart from the body in that row (spread fingers split a hand into several runs);
        # the angle is fitted from the armpit down to a quarter short of the lowest such row, so the hand doesn't bend the line
        mid_rows = range(yb(0.80), yb(0.45))
        Ls, Rs, gap = [], [], 0
        for y in mid_rows:
            rs = runs(m[y])
            if len(rs) < 3: continue
            # the body = every run touching the central band (a shading line can split the torso); the arms = runs wholly outside it
            body = [s for s in rs if s[1] >= cx - 0.12 * H and s[0] <= cx + 0.12 * H]
            if not body: continue
            b0, b1 = min(s[0] for s in body), max(s[1] for s in body)
            lo = [s for s in rs if s[1] < b0]; hi = [s for s in rs if s[0] > b1]
            if lo and hi:
                gap += 1; Ls.append((y, (min(s[0] for s in lo) + max(s[1] for s in lo)) / 2)); Rs.append((y, (min(s[0] for s in hi) + max(s[1] for s in hi)) / 2))
        def trim(pts):
            if len(pts) < 4: return pts
            y0, y1 = pts[0][0], pts[-1][0]; cut = y0 + 0.7 * (y1 - y0); return [p for p in pts if p[0] <= cut]
        L, R = trim(Ls), trim(Rs)
        band = range(yb(0.70), yb(0.50))
        r['armGapRows'] = round(sum(1 for y, _ in Ls if band.start <= y < band.stop) / max(1, len(band)), 3)
        def angle(pts):
            if len(pts) < 8: return None
            y = np.array([p[0] for p in pts], float); x = np.array([p[1] for p in pts], float)
            a, b = np.polyfit(y, x, 1); return round(90 - math.degrees(math.atan(abs(a))), 1), (float(a), float(b), int(y.min()), int(y.max()))
        al, ar = angle(L), angle(R)
        r['armDownFromHorizontal'] = {'screenLeft': al[0] if al else None, 'screenRight': ar[0] if ar else None}
        r['_armLines'] = [x[1] for x in (al, ar) if x]
        lg = 0; band = range(yb(0.40), yb(0.08))
        for y in band:
            rs = [s for s in runs(m[y]) if abs((s[0] + s[1]) / 2 - cx) < 0.2 * H]
            if len(rs) >= 2: lg += 1
        r['legGapRows'] = round(lg / max(1, len(band)), 3)
        def two(ys):
            got = []
            for y in ys:
                rs = [s for s in runs(m[y]) if abs((s[0] + s[1]) / 2 - cx) < 0.25 * H]
                lo = [s for s in rs if (s[0] + s[1]) / 2 < cx]; hi = [s for s in rs if (s[0] + s[1]) / 2 > cx]    # a shading line can split a leg: each side is one leg
                if lo and hi and max(s[1] for s in lo) < min(s[0] for s in hi):
                    a = (min(s[0] for s in lo), max(s[1] for s in lo)); b = (min(s[0] for s in hi), max(s[1] for s in hi))
                    got.append([(a[0] + a[1]) / 2, (b[0] + b[1]) / 2, a[1] - a[0] + 1, b[1] - b[0] + 1])
            return np.median(np.array(got), 0) if got else None
        feet = two(range(yb(0.02), bot + 1)); shin = two(range(yb(0.10), yb(0.07))); knee = two(range(yb(0.25), yb(0.23)))
        if feet is not None:
            r['feetSpacingH'] = round(float(feet[1] - feet[0]) / H, 4); r['footWidthH'] = round(float(np.mean(feet[2:])) / H, 4)
            r['_feet'] = [[int(feet[0]), int(feet[1])], int(yb(0.012))]
        else:
            r['feetSpacingH'] = None; r['footWidthH'] = None
        r['splayFrom'] = 'knee'
        if knee is None and shin is not None and LONG:
            # long-skirt rule (intake.json check.longSkirt): the knee is under the hem; take the top of the legs' visible stretch instead
            y_sh = yb(0.085); y = y_sh
            while y > top and two(range(y - 1, y)) is not None: y -= 1
            y_top = y + int(round(LONG['marginH'] * H)); span = (y_sh - y_top) / H
            r['visibleLegSpanH'] = round(float(span), 4)
            if span >= LONG['minSpanH']:
                knee = two(range(y_top, y_top + 2)); r['splayFrom'] = 'visible lower legs'; r['_kneeY'] = y_top; r['_splaySpanH'] = span
            else: r['splayFrom'] = 'legs hidden by the skirt'
        if feet is not None and shin is not None:
            # splay: the legs lean out if the feet sit wider than the shins (over 7.5 % of the height); yaw: a toe turned out pushes the foot's blob outward of its shin line
            # splay from the leg's own line between knee-low (24 %) and shin (8.5 %) heights: above any boot's flare at the sole
            dz = r.get('_splaySpanH', 0.155) * H                               # the vertical run the splay is measured over (knee row 24 % -> shin 8.5 %, or the visible stretch)
            r['legSplayDeg'] = round(math.degrees(math.atan(((shin[1] - shin[0]) - (knee[1] - knee[0])) / 2 / dz)), 1) if knee is not None else None
            r['footOutwardH'] = [round(float(shin[0] - feet[0]) / H, 4), round(float(feet[1] - shin[1]) / H, 4)]
            r['_shin'] = [[int(shin[0]), int(shin[1])], int(yb(0.085))]
        else:
            r['legSplayDeg'] = None; r['footOutwardH'] = None
    elif kind == 'side':
        fb = range(yb(0.02), bot + 1); ls = []
        for y in fb:
            rs = runs(m[y])
            if rs: ls.append(max(s[1] - s[0] + 1 for s in rs))
        r['footLengthH'] = round(float(np.median(ls)) / H, 4) if ls else None
    return r


def sheet(path):
    a = np.asarray(Image.open(path).convert('RGB')); m, bg, flat = mask_of(a)
    w = a.shape[1]; thirds = [(0, w // 3), (w // 3, 2 * w // 3), (2 * w // 3, w)]
    views = {}
    for (x0, x1), kind in zip(thirds, ('front', 'side', 'back')):
        v = view_metrics(m[:, x0:x1], kind)
        if v: v['x0'] = x0
        views[kind] = v
    return {'size': list(a.shape[1::-1]), 'background': [int(x) for x in bg], 'backgroundFlatness': round(flat, 1), 'views': views}, a, m


def yaw(wf, w0, L):
    if None in (wf, w0, L) or L <= w0: return None
    best = min(range(0, 61), key=lambda d: abs(w0 * math.cos(math.radians(d)) + L * math.sin(math.radians(d)) - wf))
    return float(best)


def verdict(s, b):
    v, bv = s['views'], b['views']; fails = []; num = {}
    found = sum(1 for k in ('front', 'side', 'back') if v.get(k)); num['viewsFound'] = found
    if found < TOL['viewsFound']: fails.append(f'{found} of 3 views found')
    hs = [v[k]['heightPx'] for k in ('front', 'side', 'back') if v.get(k)]
    num['viewHeightSpreadPct'] = round(100 * (max(hs) - min(hs)) / np.mean(hs), 1) if hs else None
    if num['viewHeightSpreadPct'] is None or num['viewHeightSpreadPct'] > TOL['viewHeightSpreadPct']: fails.append(f'views not at one scale ({num["viewHeightSpreadPct"]} % apart)')
    if any(v.get(k) and v[k]['touchesEdge'] for k in v): fails.append('a figure runs past its third of the sheet')
    f, bf = v.get('front') or {}, bv['front']
    for side in ('screenLeft', 'screenRight'):
        a = (f.get('armDownFromHorizontal') or {}).get(side); ref = bf['armDownFromHorizontal'][side]; num[f'arm_{side}'] = a
        if a is None: fails.append(f'{side} arm not apart from the body')
        elif abs(a - ref) > TOL['armAngleDeg']: fails.append(f'{side} arm at {a} deg below horizontal (base {ref}, tolerance {TOL["armAngleDeg"]})')
    num['armGapRows'] = f.get('armGapRows')
    if (f.get('armGapRows') or 0) < TOL['armGapRowsMin']: fails.append(f'arms touch the body (apart on {f.get("armGapRows")} of the waist rows)')
    num['legGapRows'] = f.get('legGapRows')
    if (f.get('legGapRows') or 0) < TOL['legGapRowsMin']: fails.append(f'legs not apart (gap on {f.get("legGapRows")} of the shin and knee rows; base {bf.get("legGapRows")})')
    num['feetSpacingH'] = f.get('feetSpacingH'); num['feetSpacingH_base'] = bf.get('feetSpacingH'); num['legSplayDeg'] = f.get('legSplayDeg')
    if f.get('feetSpacingH') is None: fails.append('two feet not found at the soles')
    elif abs(f['feetSpacingH'] - bf['feetSpacingH']) > TOL['feetSpacingHLoose']: fails.append(f'feet {f["feetSpacingH"]} H apart (base {bf["feetSpacingH"]}, loose tolerance {TOL["feetSpacingHLoose"]})')
    num['splayFrom'] = f.get('splayFrom'); num['visibleLegSpanH'] = f.get('visibleLegSpanH')
    if f.get('legSplayDeg') is None: fails.append('legs hidden by the skirt: no visible stretch to measure the splay on' if f.get('splayFrom') == 'legs hidden by the skirt' else 'shins not found apart')
    elif abs(f['legSplayDeg']) > TOL['legSplayDeg']: fails.append(f'legs lean {"out" if f["legSplayDeg"] > 0 else "in"} {abs(f["legSplayDeg"])} deg (straight is 0; base {bf.get("legSplayDeg")}, tolerance {TOL["legSplayDeg"]})')
    L = (v.get('side') or {}).get('footLengthH') or (bv.get('side') or {}).get('footLengthH'); num['footLengthH'] = L
    ys = []
    for o, ob in zip(f.get('footOutwardH') or [], bf.get('footOutwardH') or [0, 0]):
        ys.append(round(math.degrees(math.asin(max(-1, min(1, 2 * o / L)))), 1) if L else None)       # against straight ahead (0), not the bare-footed base
    num['footYawDegEst'] = ys
    if not ys or None in ys: fails.append('foot yaw not measurable')
    elif max(ys) > TOL['footYawDeg']: fails.append(f'feet turned out about {max(ys):.0f} deg (tolerance {TOL["footYawDeg"]})')
    num['backgroundFlatness'] = s['backgroundFlatness']
    if s['backgroundFlatness'] > TOL['backgroundFlatness']: fails.append(f'ground not flat ({s["backgroundFlatness"]})')
    return {'pass': not fails, 'fails': fails, 'numbers': num}


def overlay(a, s, bm, b, out):
    im = Image.fromarray(a).convert('RGB'); d = ImageDraw.Draw(im)
    # the base's silhouette edge, scaled so its height and soles match each view's
    edge = bm & ~(np.roll(bm, 1, 0) & np.roll(bm, -1, 0) & np.roll(bm, 1, 1) & np.roll(bm, -1, 1))
    w = bm.shape[1]
    for i, k in enumerate(('front', 'side', 'back')):
        v, bv = s['views'].get(k), b['views'].get(k)
        if not v or not bv: continue
        sc = v['heightPx'] / bv['heightPx']; bx0 = i * (w // 3); bcx = bx0 + (bv['left'] + bv['right']) / 2
        vcx = v['x0'] + (v['left'] + v['right']) / 2 if k != 'front' else v['x0'] + (v['left'] + v['right']) / 2
        ys, xs = np.nonzero(edge[:, bx0:bx0 + w // 3]); xs = xs + bx0
        for y, x in zip(ys[::2], xs[::2]):
            X = vcx + (x - bcx) * sc; Y = v['bottom'] - (bv['bottom'] - y) * sc
            d.point((X, Y), fill=(230, 40, 40))
    f = s['views'].get('front')
    if f:
        for (aa, bb, y0, y1) in f.get('_armLines', []):
            d.line([(f['x0'] + aa * y0 + bb, y0), (f['x0'] + aa * y1 + bb, y1)], fill=(40, 200, 255), width=3)
        if f.get('_feet'):
            (c1, c2), y = f['_feet']; d.line([(f['x0'] + c1, y), (f['x0'] + c2, y)], fill=(40, 200, 255), width=3)
        if f.get('_kneeY'):
            d.line([(f['x0'] + f['left'], f['_kneeY']), (f['x0'] + f['right'], f['_kneeY'])], fill=(255, 120, 40), width=2)     # the long-skirt rule's upper row
        if f.get('_shin') and f.get('_feet'):
            (s1, s2), ys_ = f['_shin']; (c1, c2), yf = f['_feet']
            d.line([(f['x0'] + s1, ys_), (f['x0'] + c1, yf)], fill=(255, 210, 40), width=2); d.line([(f['x0'] + s2, ys_), (f['x0'] + c2, yf)], fill=(255, 210, 40), width=2)
    im.save(out)


def main():
    still, base, out = sys.argv[1:4]
    s, a, _ = sheet(still); b, _, bm = sheet(base)
    v = verdict(s, b)
    clean = lambda d: {k: ({kk: vv for kk, vv in x.items() if not kk.startswith('_')} if isinstance(x, dict) else x) for k, x in d.items()}
    json.dump({'still': still, 'base': base, 'verdict': v, 'still_views': clean(s['views']), 'base_views': clean(b['views']), 'tolerances': TOL}, open(out + '.json', 'w'), indent=1)
    overlay(a, s, bm, b, out + '.png')
    print(('PASS' if v['pass'] else 'FAIL'), still.rsplit('/', 1)[-1], json.dumps(v['numbers']), '|', '; '.join(v['fails']))


if __name__ == '__main__':
    main()
