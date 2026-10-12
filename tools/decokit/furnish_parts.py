"""Furnish family helpers (furnish*.py): stroke lettering, emblems, tulip pedestals, cola bottles, striped parasol,
and pot foliage built on plants.py's cards (one plants.Cards per Shape: its build hook owns every card normal).

Frames as booths.py: FRONT puts local XY on world XZ with local +Z toward -Y (a panel facing the viewer)."""

import math

from mathutils import Vector

from geo import arc, dome, rrect
from mats import pal, register
from plants import card, lobes_mound

FRONT, BACK = (90, 0, 0), (-90, 0, 0)
SIDE_R, SIDE_L = (90, 0, 90), (90, 0, -90)  # local +Z -> world +X / -X
Z = Vector((0, 0, 1))
register("DK_Enamel_Coral", "enamel", "#E07560")  # same entry as skyline.py (register is first-wins)
register("DK_Cola", "enamel", "#3A1B12")  # filled bottle: glossy dark glass read, opaque
register("DK_LampSoft", "emit", "#FFE4B8", 1.3)  # lit machine fronts, compartments, price tabs
register("DK_Fabric_Coral", "fabric", "#E07560")
register("DK_Fabric_Ivory", "fabric", "#EFE8D6")
register("DK_Pastry", "stone", "#D39A55")
register("DK_Water", "glass", "#6FB7C4")  # as plaza.py


# ----------------------------------------------------------------- stroke lettering

def ell(cx, cy, rx, ry, a0, a1, n):
    return [(cx + rx * math.cos(math.radians(a0 + (a1 - a0) * i / n)),
             cy + ry * math.sin(math.radians(a0 + (a1 - a0) * i / n))) for i in range(n + 1)]


def _six():
    return [ell(0.3, 0.5, 0.3, 0.5, 72, 185, 4) + ell(0.3, 0.3, 0.3, 0.3, 180, 540, 10)]


# glyph: (advance, strokes) in cap-height units; Deco geometric capitals, low crossbars, narrow numerals
GLYPH = {
    "A": (0.6, [[(0, 0), (0.3, 1), (0.6, 0)], [(0.14, 0.34), (0.46, 0.34)]]),
    "C": (0.56, [ell(0.3, 0.5, 0.3, 0.5, 42, 318, 10)]),
    "O": (0.6, [ell(0.3, 0.5, 0.3, 0.5, 0, 360, 14)]),
    "L": (0.5, [[(0, 1), (0, 0), (0.5, 0)]]),
    "S": (0.58, [ell(0.29, 0.75, 0.27, 0.25, 25, 270, 6) + ell(0.29, 0.25, 0.29, 0.25, 90, -205, 7)[1:]]),
    "N": (0.6, [[(0, 0), (0, 1), (0.6, 0), (0.6, 1)]]),
    "K": (0.56, [[(0, 0), (0, 1)], [(0.56, 1), (0.02, 0.38)], [(0.2, 0.58), (0.58, 0)]]),
    "P": (0.56, [[(0, 0), (0, 1), (0.28, 1)] + ell(0.28, 0.74, 0.28, 0.26, 90, -90, 6)[1:] + [(0, 0.48)]]),
    "T": (0.6, [[(0, 1), (0.6, 1)], [(0.3, 1), (0.3, 0)]]),
    "U": (0.6, [[(0, 1)] + ell(0.3, 0.3, 0.3, 0.3, 180, 360, 7) + [(0.6, 1)]]),
    "M": (0.7, [[(0, 0), (0, 1), (0.35, 0.36), (0.7, 1), (0.7, 0)]]),
    "1": (0.3, [[(0.02, 0.8), (0.24, 1), (0.24, 0)]]),
    "2": (0.56, [ell(0.28, 0.72, 0.28, 0.28, 155, -38, 6) + [(0, 0), (0.58, 0)]]),
    "3": (0.56, [ell(0.28, 0.76, 0.26, 0.24, 155, -90, 6) + ell(0.28, 0.26, 0.29, 0.26, 90, -155, 7)[1:]]),
    "6": (0.6, _six()),
    "9": (0.6, [[(0.6 - x, 1 - y) for x, y in _six()[0]]]),
}


def _split(pts, deg=55):
    """Break a stroke at sharp corners so each tube keeps its full section (sweep averages corner tangents)."""
    out, cur = [], [pts[0]]
    for i in range(1, len(pts) - 1):
        a, p, b = Vector(pts[i - 1]), Vector(pts[i]), Vector(pts[i + 1])
        cur.append(pts[i])
        if (p - a).angle(b - p, 0) > math.radians(deg):
            out.append(cur)
            cur = [pts[i]]
    out.append(cur + [pts[-1]])
    return out


def text_width(text, h, track=0.2):
    return h * (sum(GLYPH[c][0] for c in text) + track * (len(text) - 1))


def stroke(s, pts, t, z0, z1, mat):
    """Flat-topped stroke of width t along a local-XY polyline: top strip and loose walls, so the kit bevel skips it."""
    P = [Vector(p) for p in pts]
    m, L, R = len(P), [], []
    for i, p in enumerate(P):
        d = (P[min(i + 1, m - 1)] - P[max(i - 1, 0)]).normalized()
        nv = Vector((-d.y, d.x)) * (t / 2)
        L.append(p + nv)
        R.append(p - nv)
    s._add([(v.x, v.y, z1) for v in R + L], [[i, i + 1, m + i + 1, m + i] for i in range(m - 1)], mat, False)
    walls(s, R + L[::-1], z0, z1, mat)


def walls(s, poly, z0, z1, mat):
    """Loose side faces of a CCW local-XY outline from z0 to z1 (no shared edges: no bevel strips)."""
    for a, b in zip(poly, poly[1:] + poly[:1]):
        s.face([(a[0], a[1], z0), (b[0], b[1], z0), (b[0], b[1], z1), (a[0], a[1], z1)], mat)


def flat(s, poly, z0, z1, mat, fan=False):
    """Crisp raised plate: loose walls + top (fan=True triangulates a star-shaped top from its centroid)."""
    walls(s, poly, z0, z1, mat)
    n = len(poly)
    if fan:
        c = (sum(x for x, _ in poly) / n, sum(y for _, y in poly) / n, z1)
        s._add([c] + [(x, y, z1) for x, y in poly], [[0, i + 1, (i + 1) % n + 1] for i in range(n)], mat, False)
    else:
        s.face([(x, y, z1) for x, y in poly], mat)


def letters(s, text, h, mat, x=0.0, y=0.0, lift=0.0, wt=0.15, depth=None, track=0.2, step=1):
    """Deco capitals centred at (x, y) on local XY, strokes wt * h wide standing `depth` proud of `lift` along +Z
    (use under FRONT). Strokes split at sharp corners overlap by half a width so joints stay square; step=2 halves
    the curve resolution for small type (clock numerals)."""
    t = h * wt
    d = depth or t * 0.5
    cx = x - text_width(text, h, track) / 2
    for c in text:
        w, strokes = GLYPH[c]
        for st in strokes:
            parts = _split(st[:-1:step] + st[-1:] if step > 1 and len(st) > 10 else st)
            for i, part in enumerate(parts):
                P = [Vector((cx + px * h, y + (py - 0.5) * h)) for px, py in part]
                if i > 0:
                    P[0] = P[0] - (P[1] - P[0]).normalized() * t / 2
                if i < len(parts) - 1:
                    P[-1] = P[-1] + (P[-1] - P[-2]).normalized() * t / 2
                stroke(s, [tuple(p) for p in P], t, lift, lift + d, mat)
        cx += (w + track) * h


def frame(s, w, h, r, d, lip, rim, wall=None, floor=None, n=3, cx=0.0, cy=0.0):
    """Light raised bezel on local XY standing d proud along +Z: outer wall + flat top ring in `rim`, inner reveal
    (`wall`) down to the surface, optional flat recess floor. Only the outer top edge takes the kit bevel."""
    def R(i):
        return rrect(w - 2 * i, h - 2 * i, max(r - i, 0.003), n, cx, cy)
    s.loft([(R(0), -0.005), (R(0), d), (R(lip), d)], rim, cap_bottom=False, cap_top=False)
    s.loft([(R(lip), d), (R(lip), 0.002)], wall or rim, cap_bottom=False, cap_top=False)
    if floor is not None:
        s.face([(x, y, 0.002) for x, y in R(lip)], floor)


def strip(s, x0, y0, x1, y1, mat, z=0.0015):
    """Loose quad on local XY facing +Z: seams, slots, price tabs, pinstripes."""
    s.face([(x0, y0, z), (x1, y0, z), (x1, y1, z), (x0, y1, z)], mat)


def vent(s, w, h, n, slat, back=None):
    """Louvred vent on local XY facing +Z: dark back and n rain-shedding slats (loose faces)."""
    strip(s, -w / 2, -h / 2, w / 2, h / 2, back or pal.Enamel_Charcoal, 0.001)
    p = h / n
    for k in range(n):
        y = -h / 2 + p * (k + 0.5)
        s.face([(w / 2, y + 0.45 * p, 0.002), (-w / 2, y + 0.45 * p, 0.002), (-w / 2, y - 0.2 * p, 0.6 * p),
                (w / 2, y - 0.2 * p, 0.6 * p)], slat)


def lamp(s, r, mat, seg=10):
    """Indicator lamp on local +Z: flat chrome washer and an emissive lens dome."""
    s.lathe([(r * 1.5, 0.001), (r, 0.001)], pal.Chrome, seg=seg, cap_bottom=False, cap_top=False)
    s.lathe(dome(r, 0.0, r * 0.6, 3), mat, seg=seg, cap_bottom=False)


# ----------------------------------------------------------------- emblems

def star(n, r0, r1, a0=90.0):
    """CCW star polygon, n points, inner / outer radius."""
    return [((r1 if k % 2 == 0 else r0) * math.cos(math.radians(a0 + 180 * k / n)),
             (r1 if k % 2 == 0 else r0) * math.sin(math.radians(a0 + 180 * k / n))) for k in range(2 * n)]


def starburst(s, r, mat, n=8, t=0.012, k=0.4, hub=None):
    """Flat Googie starburst on local XY standing t proud along +Z, optional domed hub."""
    flat(s, star(n, r * k, r), 0.0, t, mat, fan=True)
    if hub is not None:
        s.lathe(dome(r * 0.28, t, r * 0.16, 3), hub, seg=10, cap_bottom=False)


def atom(s, r, ring_mat, core_mat, n=16, t=None, d=None):
    """Flat atom emblem on local XY (under FRONT): three elliptic orbit strokes and a domed nucleus, along +Z."""
    t, d = t or r * 0.09, d or r * 0.05
    for k, a in enumerate((0, 60, 120)):
        ca, sa = math.cos(math.radians(a)), math.sin(math.radians(a))
        pts = [(r * math.cos(2 * math.pi * i / n), 0.36 * r * math.sin(2 * math.pi * i / n)) for i in range(n + 1)]
        stroke(s, [(x * ca - y * sa, x * sa + y * ca) for x, y in pts], t, 0.0, d * (1 + 0.3 * k), ring_mat)
    s.lathe(dome(r * 0.2, d, r * 0.16, 3), core_mat, seg=10, cap_bottom=False)


def boomerang(t=0.05, span=0.34, rise=0.1, n=10):
    """Googie boomerang outline (local XY, CCW): a swept chevron with rounded tips."""
    r = (span * span / 4 + rise * rise) / (2 * rise)
    a = math.degrees(math.asin(span / 2 / r))
    mid = arc(0, rise - r, r, 90 + a, 90 - a, n)
    L, R = [], []
    for i, (x, y) in enumerate(mid):
        w = t * (0.35 + 0.65 * math.sin(math.pi * i / n))
        d = Vector((x, y - (rise - r))).normalized()
        L.append((x + d.x * w / 2, y + d.y * w / 2))
        R.append((x - d.x * w / 2, y - d.y * w / 2))
    return L + R[::-1]


# ----------------------------------------------------------------- turned parts

def tulip(h, rb, rs, rt, foot=0.4, n=5):
    """Open (r, z) tulip-pedestal profile for lathe (no caps: it stands on the ground and meets a top at h):
    flat foot rb curving up into a stem rs, flaring to rt under the top."""
    hf = h * foot
    prof = [(rb, 0.0), (rb, 0.01)]
    prof += [(rs + (rb * 0.96 - rs) * (1 - t) ** 2.4, 0.02 + (hf - 0.02) * t) for t in (k / n for k in range(n + 1))]
    return prof + [(rs, h - 0.14)] + [(rs + (rt - rs) * u ** 2, h - 0.14 + 0.13 * u) for u in (0.4, 0.7, 1.0)]


BOTTLE = [(0.029, 0), (0.03, 0.07), (0.031, 0.1), (0.02, 0.145), (0.011, 0.175), (0.0115, 0.19), (0.0125, 0.2)]


def bottle(s, mat=None, cap=None, seg=10, k=1.0):
    """Contour soda bottle standing on the local origin (open base), 0.2 m at k = 1: opaque cola read, the crown
    cap a loose disc (reads from above, costs 8 tris)."""
    s.lathe([(r * k, z * k) for r, z in BOTTLE], mat or pal.Cola, seg=seg, cap_bottom=False, cap_top=False)
    s.face([(math.cos(2 * math.pi * i / seg) * 0.0125 * k, math.sin(2 * math.pi * i / seg) * 0.0125 * k, 0.2 * k)
            for i in range(seg)], cap or pal.Enamel_Red)


def knob(s, r, mat, seg=10):
    """Domed button / knob standing on local +Z (open base)."""
    s.lathe([(r, 0), (r * 1.05, r * 0.35)] + dome(r * 0.95, r * 0.5, r * 0.55, 3)[1:], mat, seg=seg, cap_bottom=False)


# ----------------------------------------------------------------- striped parasol

def parasol(s, R, za, ze, mats, n=8, drop=0.14, nr=4, na=3):
    """Scalloped canopy, panels alternating `mats` (plain fabric colours), valance hem, double-sided."""
    for i in range(n):
        th0 = 2 * math.pi * i / n
        verts = []
        for jr in range(nr + 1):
            t = jr / nr
            for ja in range(na + 1):
                w = ja / na
                ph = th0 + 2 * math.pi / n * w
                sc = math.sin(math.pi * w)
                rr = max(0.02, R * t * (1 - 0.06 * sc))
                verts.append((rr * math.cos(ph), rr * math.sin(ph), za - (za - ze) * t ** 1.25 - 0.04 * sc * t))
        for ja in range(na + 1):
            x, y, z = verts[nr * (na + 1) + ja]
            verts.append((x * 1.004, y * 1.004, z - drop + 0.05 * (1 - math.sin(math.pi * ja / na))))
        faces = [[jr * (na + 1) + ja, jr * (na + 1) + ja + 1, (jr + 1) * (na + 1) + ja + 1, (jr + 1) * (na + 1) + ja]
                 for jr in range(nr + 1) for ja in range(na)]
        m = mats[i % len(mats)]
        s._add(verts, [f[::-1] for f in faces], m, True)  # top skin faces up / out
        s._add([(x, y, z - 0.01) for x, y, z in verts], faces, m, True)


# ----------------------------------------------------------------- pot foliage (plants.py cards)

def mound(cd, rng, c, r, mat, size, per=18, fill=True, up=0.55):
    """Clipped ball / cushion of cards around c (radius r): outer shell plus an inner fill layer."""
    kw = dict(crown_c=c, crown_r=r * 1.2, tilt=(-20, 20), bend=0.3, zmin=-99, up_bias=up)
    lobes_mound(cd, rng, [(c, r)], mat, size, per=per, shell=(0.72, 0.98), **kw)
    if fill:
        lobes_mound(cd, rng, [(c, r)], mat, size * 1.2, per=max(4, per // 3), shell=(0.25, 0.5), **kw)


def bed(cd, rng, x0, x1, y0, y1, z, flowers=True, nx=None, leaf=None, bloom=None, h=0.12):
    """Rectangular planting over soil at z: leaf lobes filling the bed, flowering clumps crowning it."""
    L = x1 - x0
    nx = nx or max(3, round(L / 0.2))
    ys = [y0 + (y1 - y0) * f for f in ((0.25, 0.75) if y1 - y0 < 0.4 else (0.2, 0.5, 0.8))]
    xc, yc = (x0 + x1) / 2, (y0 + y1) / 2
    kw = dict(crown_c=(xc, yc, z - 0.05), crown_r=max(0.3, (y1 - y0)), axis=L / 2 - 0.1, w=0.3, bend=0.2,
              tilt=(-20, 20))

    def row(ys_, zr, rr):
        return [((x0 + 0.08 + (L - 0.16) * i / (nx - 1) + rng.uniform(-0.03, 0.03), y + rng.uniform(-0.02, 0.02),
                  z + rng.uniform(*zr)), rng.uniform(*rr)) for i in range(nx) for y in ys_]
    lobes_mound(cd, rng, row(ys, (h * 0.4, h * 0.8), (0.1, 0.13)), leaf or pal.Leaf_Shrub, 0.19, per=6, zmin=z,
                up_bias=0.2, shell=(0.4, 1.0), **kw)
    if flowers:
        lobes_mound(cd, rng, row(ys[::2] if len(ys) > 2 else ys, (h, h * 1.3), (0.08, 0.1)), bloom or pal.Flower_Coral,
                    0.19, per=6, zmin=z + 0.05, up_bias=0.7, shell=(0.5, 1.0), **kw)


def trail(cd, rng, pts, mat, size=0.2, drop=0.25):
    """Sprays draping over a rim: per (rim point, outward dir) a chain of cards cut from the lower half of the
    cluster texture, so the straight cut edge tucks inside the rim and the round edge hangs outside."""
    n = max(1, round(drop / (size * 0.7)))
    for p, out in pts:
        p, out = Vector(p), Vector((out[0], out[1], 0)).normalized()
        for k in range(n):
            c = p + out * (0.03 + 0.035 * k) - Z * (size * (0.3 + 0.62 * k))
            nn = (out * 0.8 + Z * (0.45 - 0.25 * k) + Vector((rng.uniform(-.15, .15), rng.uniform(-.15, .15), 0)))
            card(cd, rng, c, nn.normalized(), size * rng.uniform(0.9, 1.1), mat,
                 lambda q, o=out: (o * 0.75 + Z * 0.45).normalized(), tilt=(-8, 8), roll=(-12, 12),
                 uv=(0.08, 0.03, 0.92, 0.52), bend=0.2)
