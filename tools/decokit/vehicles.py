"""Vehicles (reference sheet row 2 + service robot): streamliner tram, 1950s box truck, Vespa-style scooter,
service robot. Length along Y, nose at -Y, wheels touching z = 0. Helpers live in vehicles_parts."""

import math

from mathutils import Vector

from geo import dome, fillet, ring, rrect
from kit import asset
from mats import DECAL, cell, pal, register
from vehicles_parts import cap_pt, carve, lamp, lerp_rows, loop, mesh, patch, plan, psweep, rot_to, slab, stations, vloft, wheel

register("DK_Cabin", "stone", "#86ACB3")  # dim interior seen through window glass

# ------------------------------------------------------------------------------------------------ tram
T_HW, T_YF, T_YR, T_E = 1.3, -4.2, 4.4, 2.0
T_PILLARS = (-1.7, -0.45, 0.8, 3.9)
T_DOORS = ((-4.02, -2.95), (2.05, 3.12))
T_WIN = (1.52, 2.76)  # window pocket z range
# z, half-width offset, front cap depth, rear cap depth, material of the band above this section
T_SECS = [(0.25, -0.12, 1.14, 0.90, "Chrome"), (0.29, -0.04, 1.3, 1.06, "Chrome"),
          (0.36, -0.005, 1.4, 1.13, "Enamel_Red"), (0.82, 0.0, 1.45, 1.15, "Enamel_Ivory"),
          (0.90, 0.0, 1.45, 1.15, "Enamel_Red"), (1.30, 0.0, 1.4, 1.13, "Enamel_Ivory"),
          (1.52, 0.0, 1.3, 1.08, "Chrome"), (1.52, -0.035, 1.265, 1.045, "Cabin"),
          (2.14, -0.035, 1.05, 0.93, "Cabin"), (2.76, -0.035, 0.74, 0.80, "Chrome"),
          (2.76, 0.0, 0.775, 0.835, "Enamel_Ivory"), (3.05, 0.0, 0.77, 0.83, "Enamel_Red")]
_RZ, _RX = 0.5, 0.72  # elliptical roof roll
T_SECS += [(3.05 + _RZ * math.sin(math.pi / 14 * k), -_RX * (1 - math.cos(math.pi / 14 * k)),
            0.77 - 0.85 * _RX * (1 - math.cos(math.pi / 14 * k)), 0.83 - 0.8 * _RX * (1 - math.cos(math.pi / 14 * k)),
            "Enamel_Red" if k < 4 else "Enamel_Ivory") for k in range(1, 8)]
_POCKET = [r for r in T_SECS if r[1] == -0.035]
_OUTER = [r for r in T_SECS[2:] if r[1] > -0.01]


def _t_df(z, rear=False):  # outer-skin cap depth at z (lamps, whiskers, boards on the nose / tail)
    return lerp_rows(_OUTER, z, 3 if rear else 2)


def _pocket_df(z, rear=False):  # window-pocket cap depth at z
    return lerp_rows(_POCKET, z, 3 if rear else 2)


def _tram_body(s):
    keys = [y for d in T_DOORS for y in d] + [p + d for p in T_PILLARS for d in (-0.06, 0.06)]
    sides = stations(T_YF, T_YR, keys, 0.4)

    def paint(i, j, c):
        band = T_SECS[i][4]
        if c.z > T_WIN[1] - 0.02 and (c.y < T_YF - 0.01 or c.y > T_YR + 0.01) and band.startswith("Enamel"):
            return pal.Enamel_Red  # red nose / tail domes above the screens
        return getattr(pal, band)

    secs = [(z, T_HW + dw, T_YF, T_YR, df, dr) for z, dw, df, dr, _ in T_SECS]
    vloft(s, secs, pal.Enamel_Red, sides, nc=24, e=T_E, paint=paint,
          caps=((0, pal.Iron), (-1, pal.Enamel_Ivory)))
    zs = [T_WIN[0] + 0.015 + (T_WIN[1] - T_WIN[0] - 0.03) * k / 4 for k in range(5)]
    glass = [(z, T_HW - 0.02, T_YF, T_YR, _pocket_df(z) + 0.015, _pocket_df(z, True) + 0.015) for z in zs]
    vloft(s, glass, pal.GlassDark, sides, nc=24, e=T_E, caps=())


def _side(sg, a):
    return sg * (T_HW + a)


def _tram_trim(s):
    coarse = stations(T_YF, T_YR, [], 1.5)
    for z in T_WIN:
        s.sweep(loop([(x, y, z) for x, y in plan(T_HW + 0.004, T_YF, T_YR, _pocket_df(z) + 0.035,
                                                     _pocket_df(z, True) + 0.035, coarse, 24, T_E)]),
                0.018, pal.Chrome, seg=10, caps=False)
    solid = sorted(list(T_DOORS) + [(p - 0.06, p + 0.06) for p in T_PILLARS])
    for sg in (-1, 1):
        s.sweep([(_side(sg, -0.014), T_YF - 0.05, 2.5), (_side(sg, -0.014), T_YR + 0.05, 2.5)], 0.012,
                pal.Chrome, seg=10)
        for y in T_PILLARS:
            xa, xb = sorted((_side(sg, -0.045), _side(sg, 0.004)))
            s.rbox((xa, y - 0.06, T_WIN[0] - 0.02), (xb, y + 0.06, T_WIN[1] + 0.02), 0.015, pal.Enamel_Ivory)
        for (_, ya), (yb, _) in zip(solid, solid[1:]):  # chrome frame around each side pane
            x = _side(sg, -0.012)
            fr = [(x, ya + 0.03, T_WIN[0] + 0.04), (x, yb - 0.03, T_WIN[0] + 0.04), (x, yb - 0.03, T_WIN[1] - 0.04),
                  (x, ya + 0.03, T_WIN[1] - 0.04)]
            s.sweep(loop(fillet(fr, 0.07, n=2)), 0.012, pal.Chrome, seg=10, caps=False)
    for rear in (False, True):  # windscreen / rear screen pillars following the raked pocket
        for x, r, m in ((-0.86, 0.045, pal.Enamel_Ivory), (0.86, 0.045, pal.Enamel_Ivory), (0.0, 0.022, pal.Chrome)):
            path = []
            for z in [T_WIN[0] - 0.02 + (T_WIN[1] - T_WIN[0] + 0.04) * k / 4 for k in range(5)]:
                p, n = cap_pt(T_HW - 0.035, T_YF, _pocket_df(z, rear), x, T_E, rear, T_YR)
                p = p + n * 0.026
                path.append((p.x, p.y, z))
            s.sweep(path, r, m, seg=10)
        pts = []  # bumper whisker round the nose / tail
        for x in [-T_HW + 0.03 + (2 * T_HW - 0.06) * k / 18 for k in range(19)]:
            p, n = cap_pt(T_HW + 0.015, T_YF, _t_df(0.46, rear) + 0.015, x, T_E, rear, T_YR)
            pts.append((p.x, p.y, 0.46))
        y0 = T_YF + 1.0 if not rear else T_YR - 1.0
        s.sweep([(-T_HW - 0.015, y0, 0.46)] + pts + [(T_HW + 0.015, y0, 0.46)], 0.032, pal.Chrome, seg=10)


def _tram_doors(s):
    for y0, y1 in T_DOORS:
        for sg in (-1, 1):
            def box(a, b, ya, yb, za, zb, r, m):
                xa, xb = sorted((_side(sg, a), _side(sg, b)))
                s.rbox((xa, ya, za), (xb, yb, zb), r, m)
            box(-0.012, 0.02, y0, y1, 0.36, T_WIN[1] + 0.02, 0.02, pal.Enamel_Red)
            ym = (y0 + y1) / 2
            for ya, yb in ((y0, ym), (ym, y1)):
                for za, zb in ((0.52, T_WIN[1] - 0.1),):
                    box(0.018, 0.022, ya + 0.08, yb - 0.08, za, zb, 0.0, pal.Cabin)
                    box(0.022, 0.026, ya + 0.08, yb - 0.08, za, zb, 0.0, pal.GlassDark)
                    x = _side(sg, 0.026)
                    fr = [(x, ya + 0.08, za), (x, yb - 0.08, za), (x, yb - 0.08, zb), (x, ya + 0.08, zb)]
                    s.sweep(loop(fillet(fr, 0.05, n=2)), 0.012, pal.Chrome, seg=10, caps=False)
            s.sweep([(_side(sg, 0.022), ym, 0.4), (_side(sg, 0.022), ym, T_WIN[1])], 0.012, pal.Chrome, seg=10)


def _tram_front(s):
    u0, v0, u1, v1 = 0.5 + 3 / 1024, 0.75 - 167 / 1024, 0.5 + 253 / 1024, 0.75 - 78 / 1024
    for rear in (False, True):
        for x, z, r in ((0.0, 0.64, 0.19), (-0.74, 0.7, 0.13), (0.74, 0.7, 0.13)):
            p, n = cap_pt(T_HW, T_YF, _t_df(z, rear), x, T_E, rear, T_YR)
            if rear:
                if x:
                    lamp(s, (p.x, p.y, z + 0.1), n, 0.075, lens=pal.Signal_Red)
            elif x == 0.0:
                lamp(s, (p.x, p.y - 0.03, z), n, r, housing=pal.Enamel_Red, hr=0.29, hd=0.08)
            else:
                lamp(s, (p.x, p.y, z), n, r, depth=0.05)
        sy = 1 if rear else -1
        yt = T_YR + _t_df(2.9, True) if rear else T_YF - _t_df(2.9)
        ya, yb = sorted((yt + sy * 0.04, yt - sy * 0.1))
        s.rbox((-0.42, ya, 2.785), (0.42, yb, 3.035), 0.025, pal.Rubber)
        q = yt + sy * 0.041
        quad = [(0.37 * sy, q, 2.8), (-0.37 * sy, q, 2.8), (-0.37 * sy, q, 3.02), (0.37 * sy, q, 3.02)]
        s.decal(quad, pal.Decal_Transit, ((u0, v0), (u1, v0), (u1, v1), (u0, v1)))


def _pantograph(s):
    y0, zr = -0.7, 3.55
    s.rbox((-0.55, y0 - 0.55, zr - 0.03), (0.55, y0 + 0.55, zr + 0.05), 0.02, pal.Iron)
    for x in (-0.42, 0.42):
        for y in (y0 - 0.42, y0 + 0.42):
            with s.at((x, y, zr + 0.05)):
                s.lathe([(0.05, 0), (0.05, 0.03), (0.035, 0.04), (0.045, 0.06), (0.03, 0.08), (0.03, 0.12),
                         (0, 0.12)], pal.Chrome, seg=12)
    base, knee, head = (y0 + 0.42, zr + 0.24), (y0 - 0.95, zr + 0.78), (y0 + 0.02, zr + 1.35)
    s.rbox((-0.34, y0 + 0.28, zr + 0.14), (0.34, y0 + 0.58, zr + 0.26), 0.03, pal.Iron)
    for x in (-0.26, 0.26):
        s.sweep([(x, base[0], base[1]), (x * 0.25, knee[0], knee[1])], 0.024, pal.Chrome, seg=10)
    s.sweep([(0, knee[0], knee[1]), (0, head[0], head[1])], 0.022, pal.Chrome, seg=10)
    s.sweep([(0, base[0] - 0.12, base[1]), (0, knee[0] + 0.3, knee[1] - 0.03)], 0.012, pal.Chrome, seg=10)
    with s.at((0, head[0], head[1])):
        bow = fillet([(-1.0, 0, -0.16), (-0.72, 0, 0), (0.72, 0, 0), (1.0, 0, -0.16)], 0.14, n=4, closed=False)
        for dy in (-0.08, 0.08):
            s.sweep([(x, y + dy, z) for x, y, z in bow], 0.02, pal.Chrome, seg=10)
        for x in (-0.35, 0.35):
            s.sweep([(x, -0.08, 0), (x, 0.08, 0)], 0.016, pal.Chrome, seg=10)
        s.sweep([(-0.35, 0, 0), (0, 0, -0.03), (0.35, 0, 0)], 0.016, pal.Chrome, seg=10)


def _bogies(s):
    for yb in (-3.3, 3.3):
        s.rbox((-0.72, yb - 1.25, 0.16), (0.72, yb + 1.25, 0.34), 0.05, pal.Iron)
        for dy in (-0.85, 0.85):
            for x in (-0.5, 0.5):
                with s.at((x, yb + dy, 0.33), (0, 90, 0)):
                    s.lathe([(0.3, -0.07), (0.33, -0.05), (0.33, 0.07), (0.1, 0.07), (0.1, -0.07), (0.3, -0.07)],
                            pal.Steel, seg=16, cap_bottom=False, cap_top=False)


@asset("Tram", "Vehicles", bevel=0.008)
def tram(s):
    _tram_body(s)
    _tram_trim(s)
    _tram_doors(s)
    _tram_front(s)
    _pantograph(s)
    _bogies(s)


# ------------------------------------------------------------------------------------------------ box truck
K_E, K_YR = 3.2, -1.1
K_WIN = (1.68, 2.22)
# cab: z, half-width, nose tip y, front cap depth, band material
K_CAB = [(0.40, 0.96, -3.0, 0.32, "Enamel_Ivory"), (0.46, 1.04, -3.07, 0.38, "Enamel_Ivory"),
         (0.78, 1.08, -3.11, 0.42, "Enamel_Ivory"), (0.98, 1.08, -3.11, 0.42, "Enamel_Ivory"),
         (1.06, 1.08, -3.11, 0.42, "Enamel_Mustard"), (1.14, 1.075, -3.1, 0.41, "Enamel_Mustard"),
         (1.22, 1.07, -3.09, 0.4, "Enamel_Mustard"), (1.32, 1.06, -3.07, 0.38, "Enamel_Mustard"), (1.46, 1.03, -2.93, 0.33, "Enamel_Mustard"),
         (1.56, 1.0, -2.76, 0.28, "Enamel_Mustard"), (1.64, 0.98, -2.64, 0.26, "Enamel_Mustard"),
         (1.68, 0.975, -2.62, 0.255, "Chrome"), (1.68, 0.945, -2.59, 0.225, "Cabin"),
         (2.22, 0.915, -2.25, 0.2, "Chrome"), (2.22, 0.945, -2.28, 0.23, "Enamel_Mustard"),
         (2.34, 0.93, -2.16, 0.22, "Enamel_Mustard"), (2.43, 0.88, -2.02, 0.2, "Enamel_Mustard"),
         (2.49, 0.8, -1.88, 0.17, "Enamel_Mustard"), (2.52, 0.7, -1.78, 0.13, "Enamel_Mustard")]
K_POD = [(0.40, 0.26, -2.92, -1.9, 0.26, 0.3), (0.46, 0.33, -2.92, -1.86, 0.32, 0.36), (0.85, 0.37, -2.9, -1.84, 0.36, 0.4),
         (1.1, 0.35, -2.86, -1.87, 0.34, 0.38), (1.22, 0.28, -2.8, -1.92, 0.28, 0.32),
         (1.29, 0.15, -2.72, -1.98, 0.16, 0.2)]  # pontoon fender plans about x = +-0.8
K_BOX = dict(hw=1.18, yf=-1.0, yr=2.95, df=0.36, dr=0.3, collar=-0.62)


K_PK = next(i for i, r in enumerate(K_CAB) if r[4] == "Cabin")  # window pocket rows K_PK, K_PK + 1


def _k_pocket(z):
    a, b = K_CAB[K_PK], K_CAB[K_PK + 1]
    t = (z - a[0]) / (b[0] - a[0])
    return [a[k] + (b[k] - a[k]) * t for k in range(4)]


def _truck_cab(s):
    sides = stations(0, 1, [], 0.2)

    def paint(i, j, c):
        z, hw, tip, df, band = K_CAB[i]
        zb = 1.02 + 0.22 * min(max((-1.95 - c.y) / 0.9, 0.0), 1.0)  # ivory fender line rises to the front
        if band.startswith("Enamel") and c.z < zb:
            return pal.Enamel_Mustard if (c.y < tip + df and abs(c.x) < 0.4) else pal.Enamel_Ivory
        return getattr(pal, band) if band != "Enamel_Ivory" else pal.Enamel_Mustard

    def body(t):
        secs = [(z, hw, tip + df, K_YR, df, 0.07 if i in (K_PK, K_PK + 1) else 0.1) for i, (z, hw, tip, df, _) in
                enumerate(K_CAB)]
        vloft(t, secs, pal.Enamel_Mustard, sides, nc=16, e=K_E, paint=paint,
              caps=((0, pal.Iron), (-1, pal.Enamel_Mustard)))
        for sg in (-1, 1):
            with t.at((sg * 0.8, 0, 0)):
                vloft(t, K_POD, pal.Enamel_Ivory, sides, nc=12, e=2.4, caps=((0, pal.Iron), (-1, None)))

    carve(s, body, _arch_cutters(-2.2))
    zs = [K_WIN[0] + 0.015 + (K_WIN[1] - K_WIN[0] - 0.03) * k / 3 for k in range(4)]
    glass = []
    for z in zs:
        _, hw, tip, df = _k_pocket(z)
        glass.append((z, hw + 0.015, tip + df, K_YR, df + 0.015, 0.085))
    vloft(s, glass, pal.GlassDark, sides, nc=16, e=K_E, caps=())
    for z in K_WIN:  # chrome window surround
        _, hw, tip, df = _k_pocket(z)
        s.sweep(_open_front(hw + 0.03, tip + df, df + 0.03, z), 0.014, pal.Chrome, seg=10)
    pil = []
    for z in [K_WIN[0] - 0.02 + (K_WIN[1] - K_WIN[0] + 0.04) * k / 3 for k in range(4)]:
        _, hw, tip, df = _k_pocket(z)
        pil.append((z, hw, tip, df))
    for sg in (-1, 1):  # A pillars, B pillar
        s.sweep([(sg * (hw + 0.02), tip + df + 0.06, z) for z, hw, tip, df in pil], 0.05, pal.Enamel_Mustard,
                seg=10)
        xa, xb = sorted((sg * 0.9, sg * 0.975))
        s.rbox((xa, -1.46, K_WIN[0] - 0.02), (xb, -1.38, K_WIN[1] + 0.02), 0.02, pal.Enamel_Mustard)
    path = []
    for z, hw, tip, df in pil:
        path.append((0, tip + 0.02, z))
    s.sweep(path, 0.035, pal.Enamel_Mustard, seg=10)


def _open_front(hw, yf, df, z, y0=-1.25):
    """Open polyline round the cab front at height z, from the left side at y0 to the right side at y0."""
    pts = [(-hw, y0, z)]
    for k in range(17):
        x = -hw * 0.995 + 2 * hw * 0.995 * k / 16
        p, _ = cap_pt(hw, yf, df, x, K_E)
        pts.append((p.x, p.y, z))
    return pts + [(hw, y0, z)]


def _arch_cutters(y, r=0.54, zc=0.46):
    def cut(t):
        for x0 in (-1.3, 0.62):
            with t.at((x0, y, zc), (0, 90, 0)):
                t.lathe([(r, 0), (r, 0.68)], pal.Rubber, seg=40)
    return cut


def _truck_box(s):
    b = K_BOX
    sides = stations(b["yf"], b["yr"], [b["collar"]], 0.3)
    ys = [b["yf"] + (b["yr"] - b["yf"]) * t for t in sides]
    jr = min(range(len(ys)), key=lambda k: abs(ys[k] - b["collar"]))
    R = 0.26
    rows = [(0.55, -0.04, "Enamel_Mustard"), (0.6, 0.0, "Enamel_Mustard"), (1.08, 0.0, "Chrome"),
            (1.08, 0.012, "Chrome"), (1.12, 0.012, "Chrome"), (1.12, 0.0, "Enamel_Ivory")]
    rows += [(2.84 + R * math.sin(math.pi / 12 * k), -R * (1 - math.cos(math.pi / 12 * k)), "Enamel_Ivory")
             for k in range(7)]

    def paint(i, j, c):
        band = rows[i][2]
        if band == "Enamel_Ivory" and (c.y < b["collar"] or c.y > b["yr"]):
            return pal.Enamel_Mustard
        return getattr(pal, band)

    n = 2 * len(sides) + 2 * 15
    jl = n - 15 - 1 - jr  # left-side index of the collar station
    front = list(range(jl, n)) + list(range(0, jr + 1))
    back = list(range(jr, jl + 1))

    def body(t):
        secs = [(z, b["hw"] + d, b["yf"], b["yr"], b["df"] + d, b["dr"] + d) for z, d, _ in rows]
        vloft(t, secs, pal.Enamel_Ivory, sides, nc=16, e=3.0, paint=paint,
              caps=((0, pal.Iron), (-1, pal.Enamel_Mustard, front), (-1, pal.Enamel_Ivory, back)))

    carve(s, body, _arch_cutters(1.95))
    for sg in (-1, 1):  # royal-blue swoosh, thin raised blade on each flank
        top = [(-0.35 + 3.27 * t, 1.28 + 0.14 * t + 1.24 * t * t + 0.12 * t) for t in [k / 16 for k in range(17)]]
        poly = [(-0.35, 1.28), (2.92, 1.42)] + top[::-1][:-1]
        x0 = sg * b["hw"]
        with s.at((0, 0, 0), (90, 0, 90)):
            s.extrude(poly, min(x0, x0 + sg * 0.006), max(x0, x0 + sg * 0.006), pal.Enamel_Royal)
        with s.at((sg * (b["hw"] + 0.004), -0.35, 0.8), rot_to((sg, 0, 0))):  # fuel filler on the rail
            s.lathe([(0.09, 0), (0.09, 0.012), (0.075, 0.025), (0, 0.03)], pal.Chrome, seg=20)


def _k_hw(z):
    return lerp_rows([r for r in K_CAB if r[4] != "Cabin"], z, 1)


def _truck_front(s):
    with s.at((0, -3.15, 0.5), (-10, 0, 0)):  # tall chrome grille between the fenders
        s.rbox((-0.3, -0.02, 0.02), (0.3, 0.3, 1.0), 0.05, pal.Rubber)
        for k in range(15):
            x = -0.266 + 0.038 * k
            w = 0.015 if k != 7 else 0.034
            s.rbox((x - w / 2, -0.055, 0.06), (x + w / 2, 0.0, 0.96), 0.006, pal.Chrome)
        fr = fillet([(-0.32, -0.045, 0.0), (0.32, -0.045, 0.0), (0.32, -0.045, 1.02), (-0.32, -0.045, 1.02)], 0.14, n=4)
        s.sweep(loop(fr), 0.042, pal.Chrome, seg=10, caps=False)
    for sg in (-1, 1):
        lamp(s, (sg * 0.8, -3.15, 0.98), (sg * 0.25, -1, 0.05), 0.13, housing=pal.Enamel_Ivory, hr=0.17, hd=0.03,
             depth=0.05)
        lamp(s, (sg * 0.44, -3.1, 0.6), (0, -1, 0), 0.042, depth=0.03, seg=16)
        s.rbox((sg * 0.95 - 0.06, 3.2, 0.66), (sg * 0.95 + 0.06, 3.27, 0.8), 0.015, pal.Signal_Red)
        s.sweep([(sg * 0.99, -2.42, 1.72), (sg * 1.18, -2.48, 1.86), (sg * 1.22, -2.5, 1.96)], 0.016, pal.Chrome,
                seg=10)
        with s.at((sg * 1.23, -2.5, 2.0), rot_to((0, 1, 0))):
            s.lathe([(0.085, -0.02), (0.085, 0.008), (0.07, 0.02), (0, 0.024)], pal.Chrome, seg=20)
            s.lathe([(0, -0.024), (0.08, -0.021)], pal.Glass, seg=20, cap_bottom=False)
        zs = [1.02 + 0.64 * k / 4 for k in range(5)]
        hx = [sg * (_k_hw(z) + 0.003) for z in zs]
        door = [(hx[-1], -1.98, zs[-1])] + [(x, -1.98, z) for x, z in zip(hx[::-1], zs[::-1])][1:] + \
            [(hx[0], -1.46, zs[0])] + [(x, -1.46, z) for x, z in zip(hx[1:], zs[1:])]
        s.sweep(door, 0.007, pal.Rubber, seg=6)
        x = sg * (_k_hw(1.5) + 0.01)
        s.rbox((x - 0.015, -1.66, 1.48), (x + 0.015, -1.54, 1.52), 0.008, pal.Chrome)
        x = sg * _k_hw(1.45)
        s.rbox((x - 0.01, -1.36, 1.38), (x + 0.012, -1.26, 1.5), 0.01, pal.Signal_Red)
        xa, xb = sorted((sg * 0.8, sg * 1.1))
        s.rbox((xa, -1.66, 0.34), (xb, -1.1, 0.4), 0.01, pal.Chrome)
    path = []
    for k in range(19):
        p, _ = cap_pt(1.1, -2.78, 0.47, -1.06 + 2.12 * k / 18, K_E)
        path.append((p.x, p.y, 0.44))
    path = [(-1.1, -2.62, 0.44)] + path + [(1.1, -2.62, 0.44)]
    psweep(s, path, rrect(0.17, 0.08, 0.035, n=3), pal.Chrome, side=(0, 0, 1))
    for y in (-2.2, 1.95):
        for sg in (-1, 1):
            wheel(s, (sg * 0.86, y, 0.46), 0.46, 0.28, rin=0.27, white=(0.29, 0.365), hub=0.56, rim=pal.Steel,
                  side=sg, seg=28)


@asset("BoxTruck", "Vehicles", bevel=0.006)
def box_truck(s):
    _truck_cab(s)
    _truck_box(s)
    _truck_front(s)


# ------------------------------------------------------------------------------------------------ scooter
S_FW, S_RW = (0, -0.62, 0.2), (0, 0.56, 0.2)
# leg shield plan sections: z, half-width, edge y, forward bulge
S_SHIELD = [(0.2, 0.2, -0.36, 0.05), (0.26, 0.235, -0.41, 0.07), (0.36, 0.26, -0.455, 0.085),
            (0.52, 0.265, -0.5, 0.09), (0.68, 0.24, -0.55, 0.085), (0.8, 0.19, -0.585, 0.07),
            (0.9, 0.12, -0.61, 0.055), (0.97, 0.075, -0.63, 0.04)]


def _crescent(z, W, ye, b, t, n=10):
    """Plan of a thin panel bulging to -Y (leg shield), CCW."""
    us = [-1 + 2 * i / n for i in range(n + 1)]
    return ([(W * u, ye - b * (1 - u * u), z) for u in us] +
            [(W * u * (1 - 0.5 * t / W), ye - b * (1 - u * u) + t, z) for u in reversed(us)])


def _scooter_front(s):
    mesh(s, [_crescent(*r, 0.035) for r in S_SHIELD], pal.Enamel_Powder)
    for sg in (-1, 1):
        s.sweep([(sg * W, ye + 0.017, z) for z, W, ye, b in S_SHIELD], 0.012, pal.Chrome, seg=10)
    with s.at((0, -0.585, 0.74), rot_to((0, -1, 0.1))):  # horn cast
        s.lathe([(0.05, -0.02), (0.05, 0.006), (0.04, 0.014), (0, 0.016)], pal.Chrome, seg=14)
    yc, zc, rf = S_FW[1], S_FW[2], 0.245
    path = [(0, yc - rf * math.cos(math.radians(a)), zc + rf * math.sin(math.radians(a))) for a in range(14, 164, 10)]
    bs = [math.radians(-90 + 180 * i / 10) for i in range(11)]  # U section wrapping the tyre flanks
    sec = [(0.1 * math.sin(b), 0.06 - 0.105 * math.cos(b)) for b in bs] + \
          [(0.088 * math.sin(b), 0.07 - 0.093 * math.cos(b)) for b in reversed(bs)]
    psweep(s, path, sec, pal.Enamel_Ivory, paint=lambda i, j, c: pal.Enamel_Red if 3 <= j <= 6 else None)
    wheel(s, S_FW, 0.2, 0.1, rin=0.115, white=(0.12, 0.168), hub=0.5, rim=pal.Steel, seg=22)
    s.sweep([(-0.08, yc, zc), (-0.08, yc - 0.03, zc + 0.16), (-0.06, yc + 0.01, zc + 0.3)], 0.022, pal.Chrome, seg=10)
    s.sweep([(0, -0.64, 0.93), (0, -0.66, 1.03)], 0.052, pal.Enamel_Powder, seg=16)
    pod = [(0.99, 0.075, -0.7, -0.6, 0.05, 0.03), (1.01, 0.105, -0.7, -0.6, 0.075, 0.05),
           (1.1, 0.105, -0.7, -0.6, 0.075, 0.05), (1.125, 0.08, -0.69, -0.61, 0.05, 0.03)]
    vloft(s, pod, pal.Enamel_Powder, [0, 1], nc=8, e=2.2, caps=((0, None), (-1, pal.Enamel_Ivory)))
    lamp(s, (0, -0.775, 1.06), (0, -1, -0.08), 0.075, housing=pal.Enamel_Powder, hr=0.1, hd=0.03, depth=0.03)
    for sg in (-1, 1):
        s.sweep([(sg * 0.09, -0.64, 1.08), (sg * 0.2, -0.625, 1.085), (sg * 0.31, -0.615, 1.09)], 0.014,
                pal.Chrome, seg=10)
        s.sweep([(sg * 0.31, -0.615, 1.09), (sg * 0.41, -0.61, 1.092)], 0.021, pal.Whitewall, seg=10)
        s.sweep([(sg * 0.12, -0.66, 1.09), (sg * 0.25, -0.675, 1.075), (sg * 0.33, -0.67, 1.07)], 0.007,
                pal.Chrome, seg=10, caps=False)
        s.sweep([(sg * 0.22, -0.625, 1.09), (sg * 0.25, -0.64, 1.16), (sg * 0.3, -0.65, 1.21)], 0.008, pal.Chrome,
                seg=10)
        with s.at((sg * 0.31, -0.645, 1.235), rot_to((0, 1, 0.1))):
            s.lathe([(0.048, -0.015), (0.048, 0.006), (0.04, 0.012), (0, 0.014)], pal.Chrome, seg=12)
            s.lathe([(0, -0.016), (0.044, -0.014)], pal.Glass, seg=12, cap_bottom=False)


S_COWL = [(0.28, 0.05, 0.22, 0.7, 0.1, 0.12), (0.32, 0.1, 0.2, 0.72, 0.14, 0.16), (0.37, 0.12, 0.19, 0.73, 0.15, 0.17),
          (0.42, 0.13, 0.18, 0.74, 0.16, 0.18), (0.49, 0.135, 0.19, 0.735, 0.16, 0.19), (0.56, 0.135, 0.2, 0.72, 0.16, 0.2), (0.68, 0.12, 0.24, 0.66, 0.14, 0.2), (0.76, 0.08, 0.3, 0.58, 0.1, 0.16),
          (0.79, 0.03, 0.34, 0.52, 0.05, 0.08)]  # side cowl plans (z, hw, yf, yr, df, dr) about x = +-0.19


def _cowl_x(y, z, e=2.3):
    """Lateral half-width of a side cowl at (y, z), from the S_COWL plan sections."""
    for a, b in zip(S_COWL, S_COWL[1:]):
        if a[0] <= z <= b[0]:
            t = (z - a[0]) / (b[0] - a[0])
            hw, yf, yr, df, dr = [a[k] + (b[k] - a[k]) * t for k in range(1, 6)]
            break
    else:
        return 0.0
    u = (y - yr) / dr if y > yr else (yf - y) / df if y < yf else 0.0
    return hw * max(0.0, 1 - min(u, 1.0) ** e) ** (1 / e)


def _cowls(s):
    sides = stations(0, 1, [], 0.2)
    for sg in (-1, 1):
        with s.at((sg * 0.19, 0, 0)):
            vloft(s, S_COWL, pal.Enamel_Powder, sides, nc=10, e=2.3)
        rows, top = [], []  # red speed flash: single-sided skin 4 mm proud of the cowl's lower rear flank
        for i in range(9):
            y = 0.26 + 0.56 * i / 8
            z0, z1 = 0.325 + 0.015 * (i / 8) ** 2, 0.345 + 0.25 * (i / 8) ** 1.3
            zs = [z0 + (z1 - z0) * k / 4 for k in range(5)]
            out = [(sg * (0.19 + _cowl_x(y, z) + 0.004), y, z) for z in zs]
            rows.append(out[::-1] if sg > 0 else out)  # quad normals face outward
            top.append((sg * (0.19 + _cowl_x(y, z1) + 0.007), y, z1))
        mesh(s, rows, pal.Enamel_Red, closed=False)
        s.sweep(top, 0.008, pal.Chrome, seg=10)


def _scooter_rear(s):
    s.rbox((-0.2, -0.44, 0.19), (0.2, 0.08, 0.25), 0.02, pal.Enamel_Powder)
    for x in (-0.1, 0.0, 0.1):
        s.rbox((x - 0.012, -0.39, 0.245), (x + 0.012, 0.04, 0.262), 0.005, pal.Whitewall)
    tun = fillet([(0.02, 0.24), (0.36, 0.24), (0.38, 0.8), (0.16, 0.8), (0.1, 0.62), (0.05, 0.42), (0.02, 0.3)],
                 0.04, n=3)
    slab(s, tun, 0.13, 0.05, pal.Enamel_Ivory)
    body = [(0.32, 0.10, 0.38, 0.74, 0.06, 0.1), (0.40, 0.14, 0.36, 0.76, 0.08, 0.13),
            (0.66, 0.16, 0.34, 0.76, 0.1, 0.15), (0.78, 0.15, 0.34, 0.74, 0.1, 0.14),
            (0.80, 0.12, 0.36, 0.72, 0.08, 0.12)]
    vloft(s, body, pal.Enamel_Powder, [0, 0.33, 0.66, 1], nc=10, e=2.4)
    _cowls(s)
    seat = fillet([(0.02, 0.79), (0.76, 0.79), (0.78, 0.87), (0.72, 0.935), (0.45, 0.92), (0.3, 0.92), (0.08, 0.93),
                   (0.01, 0.88)], 0.04, n=3)
    slab(s, seat, 0.155, 0.065, pal.Enamel_Red)
    lamp(s, (0, 0.9, 0.66), (0, 1, 0.25), 0.035, lens=pal.Signal_Red, depth=0.025, seg=16)
    wheel(s, S_RW, 0.2, 0.1, rin=0.115, white=(0.12, 0.168), hub=0.5, rim=pal.Steel, seg=22)


@asset("Scooter", "Vehicles", bevel=0.003)
def scooter(s):
    _scooter_front(s)
    _scooter_rear(s)


# ------------------------------------------------------------------------------------------------ service robot
def _r_rad(z):
    return 0.3 if z <= 0.5 else 0.3 * math.sqrt(max(0.0, 1 - ((z - 0.5) / 0.36) ** 2))


@asset("ServiceRobot", "Vehicles", bevel=0.003)
def service_robot(s):
    prof = [(0.0, 0.14), (0.2, 0.14), (0.27, 0.17), (0.295, 0.22), (0.3, 0.28)] + dome(0.3, 0.5, 0.36, 10)
    s.lathe(prof, pal.Enamel_Ivory, seg=36)
    s.lathe(ring(0.297, 0.312, 0.43, 0.52), pal.Enamel_Royal, seg=36, cap_bottom=False, cap_top=False)
    patch(s, lambda z: 0.312, -125, -50, [0.458, 0.466, 0.484, 0.492], 0.006, pal.Chrome, n=6)
    patch(s, _r_rad, 156, 206, [0.535 + 0.19 * k / 5 for k in range(6)], 0.012, pal.Enamel_Royal, n=6)
    th, z = math.radians(-98), 0.64
    r = _r_rad(z)
    n = (math.cos(th) * r / 0.09, math.sin(th) * r / 0.09, (z - 0.5) / 0.1296)
    with s.at((r * math.cos(th), r * math.sin(th), z), rot_to(n)):
        s.lathe([(0.145, -0.07), (0.145, 0.0), (0.137, 0.03), (0.12, 0.044), (0.104, 0.036), (0.104, -0.02)],
                pal.Enamel_Royal, seg=24, cap_bottom=False, cap_top=False)
        s.lathe([(0.104, 0.02), (0.104, 0.032), (0.096, 0.038), (0.088, 0.032), (0.088, 0.0)], pal.Chrome, seg=24,
                cap_bottom=False, cap_top=False)
        s.lathe([(0.089, 0.004), (0.066, 0.014)], pal.Iron, seg=24, cap_bottom=False, cap_top=False)
        s.lathe([(0.067, 0.012), (0.05, 0.03), (0.025, 0.04), (0, 0.043)], pal.RobotEye, seg=24, cap_bottom=False)
    with s.at((-0.07, 0.05, 0.84)):
        s.lathe([(0.042, -0.02), (0.042, 0.012), (0.026, 0.03), (0.015, 0.042), (0, 0.042)], pal.Chrome, seg=16)
        s.sweep([(0, 0, 0.03), (0, 0, 0.2)], 0.011, pal.Chrome, seg=10)
        s.lathe([(0.036 * math.sin(math.pi * k / 8), 0.232 - 0.036 * math.cos(math.pi * k / 8)) for k in range(9)],
                pal.Chrome, seg=16)
    lamp(s, (0, 0.298, 0.32), (0, 1, 0), 0.03, lens=pal.Signal_Red, depth=0.02, seg=16)
    for x in (-0.31, 0.31):
        for y in (-0.2, 0.2):
            wheel(s, (x, y, 0.165), 0.165, 0.12, rin=0.095, hub=0.6, seg=24, dome=0.07, side=1 if x > 0 else -1)
