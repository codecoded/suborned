# Roster column repaint (brief 046 round 6). Sky: "the more I look at that character plate, the more rivets seem to appear and few oddities like
# the blue side borders not really connected to anything."
#
# Provenance: NOT a new generation (cost 0). The kit's roster_column@2x (briefs/011/ui/kit/roster/roster_column@2x.png, cut by out/concepts/ui-011-kit/build1.py
# from the Codex sheet col4_cab) stays the source of every ring, bezel, mat, cap, tab and body pixel. This script (a) removes the rivets by inpainting the
# body / bezel, (b) clears the seafoam side stripes and redraws them as an inlay from the sprite's OWN stripe pixel profile, (c) fixes the key-colour spill
# (lime fringe round the portrait windows, magenta fringe on the silhouette). Geometry is untouched: same size, same ring centres, same clean cut rows
# (223, 412, 601, 789), so the lab's stretch recipe and every ring-centre number still hold.
#
# Variants (ids are the lab's roster.column token):
#   inlay     : the side stripes run the full height and tuck under both caps (clipped by the rim, so they end where the frame ends); 4 corner rivets stay
#   bare      : no side stripes; 4 corner rivets
#   rivetless : as inlay, no rivets at all
# (A third idea, the inlay turning the corner under each cap as an L, was drawn and dropped: the foot's L sits across the foot cut row 789 of the stretch recipe, so the
#  repeated block smears it into a solid slab. The top-only L would be lopsided.)
# Rivets removed in all: 8 on the bezels, 4 in the two gaps between portraits (the 3rd gap never had any: the inconsistency Sky saw).
#
# Run: python3 -I testbed/hud-style/prep/roster_column.py   (needs opencv + numpy)
import cv2, numpy as np, json, hashlib, os, sys
ROOT = '/Users/sky/Projects/suborned/'
SRC = ROOT + 'briefs/011/ui/kit/roster/roster_column@2x.png'
OUT = ROOT + 'testbed/hud-style/sprites/'
os.makedirs(OUT, exist_ok=True)
W, H = 218, 833
CUTS = [223, 412, 601, 789]

def load(p):
    a = cv2.imread(p, cv2.IMREAD_UNCHANGED)
    return a[:, :, [2, 1, 0, 3]].copy()
def save(a, p):
    cv2.imwrite(p, a[:, :, [2, 1, 0, 3]])
def luma(a): return (0.299 * a[..., 0] + 0.587 * a[..., 1] + 0.114 * a[..., 2])

src = load(SRC)
rgb0, alpha = src[:, :, :3].copy(), src[:, :, 3].copy()

# ---- 1. the sprite's own stripe profile (row 300, between rings 2 and 3, nothing else there)
PROF_L = rgb0[300, 6:16].astype(np.float32)        # x 6..15 : outer pale line, highlight, 6 px seafoam, pale line, cream inner edge
# each side uses its own pixels (index k = distance from the outer edge)
STRIPE_X = {'L': list(range(6, 16)), 'R': list(range(211, 201, -1))}   # the sprite's own right stripe is 1 px offset (lit differently); the inlay is drawn symmetric: the right side mirrors the left profile   # index k = distance from the outer edge
PROF = {'L': PROF_L, 'R': PROF_L.copy()}

# ---- 2. interior mask: the body between the rim and the caps (dark teal, or stripe), connected to the column centre
r, g, b = [rgb0[..., i].astype(int) for i in range(3)]
dark = (luma(rgb0) < 100) & (alpha > 250)
stripe_px = (g > r + 60) & (alpha > 250)
interior0 = (dark | stripe_px).astype(np.uint8)
n, lab = cv2.connectedComponents(interior0, connectivity=4)
interior = lab == lab[222, 100]    # the gap between rings 1 and 2

# first / last interior row per column near the top and bottom (the body face under the lip's dark shadow line)
def first_row(x, y0, y1, step):
    ys = range(y0, y1, step)
    for y in ys:
        if interior[y, x] and luma(rgb0[y, x]) > 28: return y
    return None

# ---- 3. body fill used where a stripe or a rivet goes: per-row colour from the clean body strip x 16..18 (kit body), smoothed along y
def body_fill():
    # the plain face colour per row: median of the body-like pixels of the row (dark teal, not stripe, not ring, not rivet); rows that cross a ring (few free pixels) take the nearest rows that do not
    L = luma(rgb0)
    ok = interior & (L > 25) & (L < 95) & (g > r + 12) & ~stripe_px
    ok[:, :17] = False; ok[:, 201:] = False
    f = np.zeros((H, 3), np.float32); cnt = ok.sum(axis=1)
    for y in range(H):
        if cnt[y] >= 12: f[y] = np.median(rgb0[y][ok[y]], axis=0)
    good = np.where(cnt >= 12)[0]
    for k in range(3): f[:, k] = np.interp(np.arange(H), good, f[good, k])
    return cv2.GaussianBlur(f.reshape(H, 1, 3), (1, 0), sigmaX=0.1, sigmaY=4).reshape(H, 3)
FILL = body_fill()

def clear_stripes(rgb):
    out = rgb.copy()
    for xs in (range(6, 16), range(199, 212)):
        for y in range(39, 790):
            for x in xs:
                if y >= 49 and y <= 780 and alpha[y, x] > 0: out[y, x] = FILL[y]
                elif interior[y, x] or stripe_px[y, x]: out[y, x] = FILL[y]
    return out

# ---- 4. rivets: positions found by template matching (corner type and bezel type), listed here so the count is auditable
def find(template_box, thr):
    x0, y0, x1, y1 = template_box
    t = rgb0[y0:y1, x0:x1].astype(np.float32)
    res = cv2.matchTemplate(rgb0.astype(np.float32), t, cv2.TM_CCOEFF_NORMED)
    ys, xs = np.where(res > thr)
    pts = []
    for y, x in sorted(zip(ys, xs), key=lambda p: -res[p]):
        c = (x + (x1 - x0) // 2, y + (y1 - y0) // 2)
        if all(abs(c[0] - p[0]) > 8 or abs(c[1] - p[1]) > 8 for p in pts): pts.append(c)
    return sorted(pts, key=lambda p: (p[1], p[0]))
CORNER = find((17, 43, 34, 60), 0.55)       # 8: ring-1 top, ring-2 top, ring-3 top, foot
BEZEL_L = find((18, 123, 35, 140), 0.88)    # left bezel rivets (4)
BEZEL_R = [(217 - x, y) for x, y in BEZEL_L]  # mirrored: the right ones are the same rivets (their lighting differs, so mirrored positions are checked below)
OUTER = [p for p in CORNER if p[1] < 60 or p[1] > 700]           # the four outer corners
GAPS = [p for p in CORNER if 60 <= p[1] <= 700]                  # the two gap pairs
assert len(CORNER) == 8 and len(BEZEL_L) == 4 and len(OUTER) == 4 and len(GAPS) == 4, (len(CORNER), len(BEZEL_L), len(OUTER), len(GAPS))

def remove_corner(rgb, pts):
    m = np.zeros((H, W), np.uint8)
    for x, y in pts: cv2.circle(m, (x, y + 1), 13, 255, -1)
    # fill with the plain face colour per row (the body is a smooth vertical gradient), feathered into the surround
    out = rgb.astype(np.float32)
    soft = cv2.GaussianBlur(m, (0, 0), 3.0).astype(np.float32)[..., None] / 255.0
    # face colour at this row, from clean columns just inside the rim and between the rivet and the ring: use the Telea fill as the base, then blend to the row colour
    inp = cv2.inpaint(rgb, m, 7, cv2.INPAINT_TELEA).astype(np.float32)
    return (out * (1 - soft) + inp * soft).clip(0, 255).astype(np.uint8)

def remove_bezel(rgb, pts):
    out = rgb.astype(np.float32)
    for x, y in pts:
        y0, y1 = y - 11, y + 12        # rows above and below the rivet and its shadow
        for xx in range(x - 9, x + 10):
            a, bb = out[y0, xx], out[y1, xx]
            for yy in range(y0 + 1, y1):
                t = (yy - y0) / (y1 - y0)
                out[yy, xx] = a * (1 - t) + bb * t
    return out.clip(0, 255).astype(np.uint8)

# ---- 5. key-colour spill: lime fringe at the window edge, magenta on the silhouette
def defringe(rgb, al):
    out = rgb.copy(); r, g, b = [rgb[..., i].astype(int) for i in range(3)]
    # lime: green dominant and (nearly) transparent neighbours = the window edge. Take the cream mat colour from the nearest clean pixel inside the mat.
    green = (g > r + 18) & (g > b + 18) & (alpha > 0) & (luma(rgb) < 200)
    # only the thin line along the window edge: within 4 px of a transparent pixel, not the seafoam stripes (g>r+60 and bright) nor the cap tab
    transp = (alpha < 10).astype(np.uint8)
    near = cv2.dilate(transp, np.ones((9, 9), np.uint8)) > 0
    # transparent pixels are ring windows (interior) or outside the column: the window pixels are those not connected to the column border
    n2, lab2 = cv2.connectedComponents(transp, connectivity=4)
    outside = lab2 == lab2[0, 0]
    win = (transp > 0) & ~outside
    nearwin = cv2.dilate(win.astype(np.uint8), np.ones((9, 9), np.uint8)) > 0
    fringe = green & nearwin
    m = fringe.astype(np.uint8) * 255
    m = cv2.dilate(m, np.ones((3, 3), np.uint8))
    m[(alpha < 10)] = 0
    # inpaint from the cream mat only: temporarily paint the window with mat colour so the fill never pulls lime or the portrait key
    tmp = rgb.copy(); tmp[win] = (231, 217, 183)
    fixed = cv2.inpaint(tmp, m, 3, cv2.INPAINT_TELEA)
    out[m > 0] = fixed[m > 0]
    # magenta / purple spill on the outside silhouette: pull toward neutral
    pu = (r > g + 14) & (b > g + 14) & (alpha > 0)
    grey = ((r + g + b) / 3).astype(np.uint8)
    for k in range(3): out[..., k][pu] = (out[..., k][pu].astype(int) * 0.25 + grey[pu] * 0.75).astype(np.uint8)
    return out, int(fringe.sum()), int(pu.sum())

# ---- 6. the inlay (stripes) drawn from the sprite's own profile
def inlay_vertical(rgb, y_top, y_bot):
    out = rgb.copy()
    for side in ('L', 'R'):
        xs = STRIPE_X[side]
        for y in range(y_top, y_bot + 1):
            for k, x in enumerate(xs):
                if (49 <= y <= 780 and alpha[y, x] > 0) or interior[y, x] or stripe_px[y, x]:
                    out[y, x] = PROF[side][k].astype(np.uint8)
    return out

def build(variant):
    rgb = rgb0.copy()
    rgb, nf, npu = defringe(rgb, alpha)
    rgb = remove_bezel(rgb, BEZEL_L + BEZEL_R)
    rgb = clear_stripes(rgb)                     # first, so the rivet fill never pulls stripe colour
    rem = GAPS + ([] if variant in ('inlay', 'bare') else OUTER)   # the CORNER list already holds left and right rivets
    rgb = remove_corner(rgb, rem)
    if variant == 'inlay': rgb = inlay_vertical(rgb, 39, 789)
    elif variant == 'rivetless': rgb = inlay_vertical(rgb, 39, 789)
    out = np.dstack([rgb, alpha])
    return out, nf, npu

if __name__ == '__main__' and '--count' not in sys.argv:
    log = {'source': SRC.replace(ROOT, ''), 'source_sha256': hashlib.sha256(open(SRC, 'rb').read()).hexdigest(), 'tool': 'testbed/hud-style/prep/roster_column.py (opencv inpaint + procedural inlay from the sprite\'s own stripe profile); no image generation, cost 0',
           'rivets_found': {'corner': [list(map(int, p)) for p in CORNER], 'bezel': [list(map(int, p)) for p in BEZEL_L + BEZEL_R]}, 'variants': {}}
    for v, rv in (('inlay', 4), ('bare', 4), ('rivetless', 0)):
        out, nf, npu = build(v)
        p = OUT + f'roster_column_{v}@2x.png'; save(out, p)
        half = cv2.resize(out, (109, 416), interpolation=cv2.INTER_AREA); save(half, OUT + f'roster_column_{v}@1x.png')
        log['variants'][v] = {'rivets': rv, 'lime_fringe_px_fixed': nf, 'magenta_px_fixed': npu, 'sha256_2x': hashlib.sha256(open(p, 'rb').read()).hexdigest()}
        print(v, rv, 'rivets', p, nf, npu)
    log['rivets_before'] = {'corner': 8, 'bezel': 8, 'total': 16, 'by_gap': 'rings 1-2: 2, rings 2-3: 2, rings 3-4: 0, foot 2, top 2'}
    json.dump(log, open(OUT + 'PROVENANCE.json', 'w'), indent=1)

# ---- rivet counter (also run by check.ts): python3 -I prep/roster_column.py --count <png> [--half]   -> {"corner": n, "bezel": n, "total": n}
# Template matching with the four rivet crops of the kit sprite (corner L/R, bezel L/R); works on the sprite, its stretched copies and its @1x (--half scales the templates by 0.5).
def count_rivets(path, half=False):
    im = cv2.imread(path, cv2.IMREAD_UNCHANGED)[:, :, [2, 1, 0, 3]]
    img = im[:, :, :3].astype(np.float32)
    T = {'corner': [rgb0[43:60, 17:34], rgb0[43:60, 184:201]], 'bezel': [rgb0[123:140, 18:35], rgb0[123:140, 183:200]]}
    found = {'corner': [], 'bezel': []}
    for kind, tpls in T.items():
        for t in tpls:
            t = t.astype(np.float32)
            if half: t = cv2.resize(t, None, fx=0.5, fy=0.5, interpolation=cv2.INTER_AREA)
            res = cv2.matchTemplate(img, t, cv2.TM_CCOEFF_NORMED)
            ys, xs = np.where(res > (0.7 if not half else 0.75))
            for y, x in sorted(zip(ys, xs), key=lambda p: -res[p]):
                c = (x + t.shape[1] // 2, y + t.shape[0] // 2); r = 8 if not half else 4
                if all(abs(c[0] - p[0]) > r or abs(c[1] - p[1]) > r for p in found[kind]): found[kind].append(c)
    return {'corner': len(found['corner']), 'bezel': len(found['bezel']), 'total': len(found['corner']) + len(found['bezel']), 'at': sorted([list(map(int, p)) for k in found for p in found[k]], key=lambda p: (p[1], p[0]))}
if __name__ == '__main__' and len(sys.argv) > 2 and sys.argv[1] == '--count':
    print(json.dumps(count_rivets(sys.argv[2], '--half' in sys.argv)))
