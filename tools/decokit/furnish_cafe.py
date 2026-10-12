"""Furnish: café and terrace pieces (registered via furnish.py). Tulip table / chair / set, chrome-wire bistro set
with a striped parasol, tiling terrace screen, harbour deckchair, round tree bench.
Seat height 0.45 m, table height 0.74 m (bistro 0.72 m); chairs face -Y with the seat centre over the pivot."""

import math
import random

from geo import circle, dome, fillet, rrect
from kit import asset
from mats import pal
from plants import Cards, lobes_mound
from plaza_parts import uvmesh
from furnish_parts import FRONT, SIDE_R, boomerang, flat, parasol, tulip


# ----------------------------------------------------------------- tulip family

def tulip_table(s, R=0.4, H=0.74, top=None):
    s.lathe(tulip(H - 0.03, 0.27, 0.042, 0.14, n=3), pal.Enamel_Ivory, seg=20, cap_bottom=False, cap_top=False)
    s.lathe([(0.13, H - 0.032), (R - 0.012, H - 0.026)], pal.Enamel_Ivory, seg=24, cap_bottom=False, cap_top=False)
    s.lathe([(R - 0.012, H - 0.026), (R + 0.002, H - 0.016), (R + 0.002, H - 0.006), (R - 0.008, H)], pal.Chrome,
            seg=24, cap_bottom=False, cap_top=False)
    s.face([(x, y, H) for x, y in circle(R - 0.008, 24)], top or pal.Terrazzo_Ivory)


def skin(s, G, t, mat, rim=None):
    """Thin shell from a rows x cols grid of outer points G (rows up, cols CCW about Z): outer skin, inner skin offset
    t toward the axis, and a loose rim (crisp edge, no bevel strip)."""
    def inset(p):
        r = math.hypot(p[0], p[1])
        return (p[0] * (r - t) / r, p[1] * (r - t) / r, p[2])
    rows, cols = len(G), len(G[0])
    I = [[inset(p) for p in row] for row in G]
    ix = lambda i, j: i * cols + j  # noqa: E731
    quads = [[ix(i, j), ix(i, j + 1), ix(i + 1, j + 1), ix(i + 1, j)] for i in range(rows - 1) for j in range(cols - 1)]
    s._add([p for row in G for p in row], quads, mat, True)
    s._add([p for row in I for p in row], [q[::-1] for q in quads], mat, True)
    edge = [G[0][j] for j in range(cols)] + [G[i][-1] for i in range(1, rows)] + \
        [G[-1][j] for j in range(cols - 2, -1, -1)] + [G[i][0] for i in range(rows - 2, 0, -1)]
    inner = [inset(p) for p in edge]
    k = len(edge)
    s._add(edge + inner, [[(a + 1) % k, a, k + a, k + (a + 1) % k] for a in range(k)], rim or mat, True)


def tulip_chair(s, cushion=None):
    """Saarinen-style shell chair: tulip foot, seat bowl, back shell wrapping into arms, loose cushion."""
    s.lathe(tulip(0.41, 0.25, 0.034, 0.1, n=3), pal.Enamel_Ivory, seg=16, cap_bottom=False, cap_top=False)
    s.lathe([(0.1, 0.4), (0.22, 0.414), (0.262, 0.462), (0.24, 0.462), (0, 0.438)], pal.Enamel_Ivory, seg=16,
            cap_bottom=False)
    rows, cols = 5, 9
    G = []
    for i in range(rows):
        v = i / (rows - 1)
        z, r, half = 0.46 + 0.37 * v, 0.256 + 0.06 * v ** 1.6, math.radians(150 - 108 * v ** 0.85)
        G.append([(r * math.cos(math.pi / 2 + half * (2 * j / (cols - 1) - 1)),
                   r * math.sin(math.pi / 2 + half * (2 * j / (cols - 1) - 1)), z - 0.02 * (1 - v) * abs(2 * j / (cols - 1) - 1))
                  for j in range(cols)][::-1])
    skin(s, G, 0.014, pal.Enamel_Ivory)
    s.lathe([(0.198 + 0.016 * math.cos(math.radians(a)), 0.458 + 0.016 * math.sin(math.radians(a)))
             for a in (-45, -15, 15, 45, 75)] + [(0.12, 0.476), (0, 0.477)], cushion or pal.Enamel_Coral, seg=16,
            cap_bottom=False)


@asset("CafeTable_Tulip", "Furnish")
def cafe_table_tulip(s):
    """0.8 m round terrazzo top in a chrome edge band on an ivory tulip pedestal, 0.74 m."""
    tulip_table(s)


@asset("CafeChair_Tulip", "Furnish")
def cafe_chair_tulip(s):
    """Shell chair on a tulip foot: seat 0.45-0.48 m (coral cushion), back to 0.83 m, faces -Y."""
    tulip_chair(s)


@asset("CafeSet_Tulip", "Furnish")
def cafe_set_tulip(s):
    """Tulip table with three shell chairs at 0.72 m (cushions coral / seafoam / mustard) and a bud vase."""
    tulip_table(s)
    for k, c in enumerate((pal.Enamel_Coral, pal.Enamel_Seafoam, pal.Enamel_Mustard)):
        a = 90 + 120 * k + 30
        with s.at((0.72 * math.cos(math.radians(a)), 0.72 * math.sin(math.radians(a)), 0), (0, 0, a - 90)):
            tulip_chair(s, c)
    with s.at((0.08, -0.05, 0.74)):  # bud vase: seafoam enamel, three coral tulip buds
        s.lathe([(0.03, 0.0), (0.038, 0.03), (0.024, 0.09), (0.014, 0.13), (0.02, 0.15)], pal.Enamel_Seafoam, seg=12,
                cap_bottom=False, cap_top=False)
        for dx, dy, h in ((0.0, 0.0, 0.3), (0.035, 0.01, 0.26), (-0.03, 0.02, 0.24)):
            s.face([(0.0, -0.002, 0.14), (0.0, 0.002, 0.14), (dx, dy + 0.002, h), (dx, dy - 0.002, h)], pal.Petiole)
            s.face([(-0.002, 0.0, 0.14), (dx - 0.002, dy, h), (dx + 0.002, dy, h), (0.002, 0.0, 0.14)], pal.Petiole)
            with s.at((dx, dy, h - 0.01)):
                s.lathe([(0.008, 0.0), (0.018, 0.022), (0.013, 0.045), (0, 0.058)], pal.Enamel_Coral, seg=10,
                        cap_bottom=False)


# ----------------------------------------------------------------- bistro set

def wire_chair(s):
    """Bent chrome-wire bistro chair: front U, back legs rising into a hoop, coral seat and boomerang backrest."""
    w = 0.009
    s.sweep(fillet([(-0.19, -0.21, 0.0), (-0.165, -0.165, 0.435), (0.165, -0.165, 0.435), (0.19, -0.21, 0.0)], 0.06,
                   n=2, closed=False), w, pal.Chrome, seg=10, caps=False)
    s.sweep(fillet([(-0.2, 0.22, 0.0), (-0.165, 0.165, 0.435), (-0.185, 0.23, 0.84), (0.185, 0.23, 0.84),
                    (0.165, 0.165, 0.435), (0.2, 0.22, 0.0)], 0.09, n=2, closed=False), w, pal.Chrome, seg=10,
            caps=False)
    for sx in (-1, 1):  # side stretchers
        s.sweep([(sx * 0.178, -0.19, 0.2), (sx * 0.183, 0.195, 0.2)], 0.006, pal.Chrome, seg=10, caps=False)
    s.loft([(rrect(0.38, 0.37, 0.14, 3), 0.43), (rrect(0.4, 0.39, 0.15, 3), 0.445), (rrect(0.38, 0.37, 0.14, 3), 0.46)],
           pal.Chrome, cap_bottom=False, cap_top=False)
    s.loft([(rrect(0.37, 0.36, 0.135, 3), 0.455), (rrect(0.35, 0.34, 0.125, 3), 0.472)], pal.Enamel_Coral,
           cap_bottom=False)
    with s.at((0, 0.215, 0.7), (100, 0, 0)):
        flat(s, boomerang(0.1, 0.36, 0.05), -0.009, 0.009, pal.Enamel_Coral)


@asset("BistroSet", "Furnish", bevel=0.004)
def bistro_set(s):
    """0.6 m round enamel table (0.72 m) on a chrome wire tripod, two wire chairs, coral / ivory striped parasol
    (R 1.1 m, 2.35 m) with a boomerang finial."""
    H = 0.72
    s.lathe([(0.03, H - 0.03), (0.29, H - 0.027)], pal.Enamel_Ivory, seg=28, cap_bottom=False, cap_top=False)
    s.lathe([(0.29, H - 0.027), (0.302, H - 0.019), (0.302, H - 0.008), (0.29, H - 0.001)], pal.Chrome, seg=28,
            cap_bottom=False, cap_top=False)
    s.face([(x, y, H) for x, y in circle(0.292, 28)], pal.Enamel_Coral)
    for k in range(3):  # tripod: splayed wire legs up into a chrome collar
        a = math.radians(90 + 120 * k)
        c, sn = math.cos(a), math.sin(a)
        s.sweep(fillet([(0.26 * c, 0.26 * sn, 0.0), (0.05 * c, 0.05 * sn, 0.22), (0.03 * c, 0.03 * sn, H - 0.03)],
                       0.12, n=4, closed=False), 0.01, pal.Chrome, seg=10, caps=False)
    s.lathe([(0.045, 0.2), (0.05, 0.22), (0.045, 0.24)], pal.Chrome, seg=12, cap_bottom=False, cap_top=False)
    s.lathe([(0.016, 0.0), (0.016, 2.32)], pal.Chrome, seg=10, cap_bottom=False, cap_top=False)  # pole
    s.lathe([(0.016, 1.62), (0.035, 1.64), (0.035, 1.7), (0.016, 1.72)], pal.Chrome, seg=10, cap_bottom=False,
            cap_top=False)  # runner
    for i in range(4):
        a = 2 * math.pi * (i + 0.5) / 4
        s.sweep([(0.03 * math.cos(a), 0.03 * math.sin(a), 1.69), (0.55 * math.cos(a), 0.55 * math.sin(a), 2.08)],
                0.006, pal.Chrome, seg=10, caps=False)
    parasol(s, 1.1, 2.3, 1.95, (pal.Fabric_Coral, pal.Fabric_Ivory), drop=0.13, nr=3)
    s.lathe([(0.03, 2.28), (0.045, 2.3), (0.03, 2.34), (0.018, 2.36)], pal.Chrome, seg=12, cap_bottom=False,
            cap_top=False)
    with s.at((0, 0, 2.36), FRONT):
        flat(s, boomerang(0.045, 0.3, 0.09), -0.008, 0.008, pal.Enamel_Mustard)
    for a in (-35, 145):
        with s.at((0.62 * math.cos(math.radians(a)), 0.62 * math.sin(math.radians(a)), 0), (0, 0, a - 90)):
            wire_chair(s)


# ----------------------------------------------------------------- terrace screen

@asset("TerraceScreen", "Furnish")
def terrace_screen(s):
    """2.0 m café-terrace barrier, tiles along X (rails run the full 2 m and meet the neighbour's): teal enamel
    trough (0.42 deep, 0.55 high) with ivory rim and chrome speed lines, clipped box hedge, chrome rail at 1.05 m."""
    W, D = 1.96, 0.42

    def rr(e):
        return rrect(W - 2 * e, D - 2 * e, 0.07 - e * 0.6, 4)
    s.loft([(rr(0.03), 0.0), (rr(0.0), 0.03), (rr(0.0), 0.48)], pal.Enamel_Teal, cap_bottom=False, cap_top=False)
    s.loft([(rr(0.0), 0.478), (rr(-0.012), 0.5), (rr(-0.012), 0.53), (rr(0.004), 0.55), (rr(0.035), 0.55),
            (rr(0.05), 0.53), (rr(0.05), 0.48)], pal.Enamel_Ivory, cap_bottom=False, cap_top=False)
    s.face([(x, y, 0.49) for x, y in rr(0.05)], pal.Soil)
    s.loft([(rr(0.015), 0.0), (rr(0.0), 0.012), (rr(0.0), 0.04), (rr(0.012), 0.05)], pal.Chrome, cap_bottom=False,
           cap_top=False)  # kick band
    for sy in (-1, 1):
        for z, L in ((0.16, 1.7), (0.22, 1.5), (0.28, 1.3)):
            s.sweep([(-L / 2, sy * (D / 2 + 0.003), z), (L / 2, sy * (D / 2 + 0.003), z)], 0.008, pal.Chrome, seg=10,
                    caps=False)
    for x in (-0.72, 0.72):  # rail posts
        with s.at((x, 0, 0.53)):
            s.lathe([(0.045, 0.0), (0.045, 0.02), (0.026, 0.04), (0.02, 0.1), (0.02, 0.5)], pal.Chrome, seg=12,
                    cap_bottom=False, cap_top=False)
            s.lathe([(0.02, 0.5)] + [(0.034 * math.sin(math.pi * k / 6), 0.52 - 0.034 * math.cos(math.pi * k / 6))
                                     for k in range(1, 7)], pal.Chrome, seg=12, cap_bottom=False)
    s.sweep([(-1.0, 0, 1.05), (1.0, 0, 1.05)], 0.022, pal.Chrome, seg=12, caps=False)
    s.sweep([(-1.0, 0, 0.82), (1.0, 0, 0.82)], 0.008, pal.Chrome, seg=10, caps=False)
    rng = random.Random(5101)
    cd = Cards(s)
    lobes = [((-0.8 + 1.6 * i / 7 + rng.uniform(-0.03, 0.03), rng.uniform(-0.03, 0.03), 0.66 + rng.uniform(-0.02, 0.02)),
              rng.uniform(0.19, 0.22)) for i in range(8)]
    kw = dict(crown_c=(0, 0, 0.5), crown_r=0.4, axis=0.75, w=0.35, bend=0.25, tilt=(-15, 15), zmin=0.45)
    lobes_mound(cd, rng, lobes, pal.Leaf_Shrub, 0.26, per=11, shell=(0.72, 0.98), up_bias=0.4, **kw)
    lobes_mound(cd, rng, lobes, pal.Leaf_Shrub, 0.3, per=4, shell=(0.2, 0.45), up_bias=0.4, **kw)
    with s.at((0, 0, 0.5), scale=(0.95, 0.19, 0.2)):  # dark core: gaps read as shade
        s.lathe([(0.9, -0.1)] + dome(1.0, 0.0, 1.0, n=4), pal.Leaf_CoreShrub, seg=12)


# ----------------------------------------------------------------- deckchair

def sling(s, P0, P1, w, sag, n=8, stripes=6):
    """Striped canvas sling between two bars (P0 top, P1 front), sagging `sag`, double-sided, awning UVs."""
    x0 = -w / 2
    for side in (1, -1):
        verts, uvs = [], []
        for i in range(n + 1):
            t = i / n
            y = P0[1] + (P1[1] - P0[1]) * t
            z = P0[2] + (P1[2] - P0[2]) * t - sag * math.sin(math.pi * t) - (0.006 if side < 0 else 0)
            for j in range(stripes * 2 + 1):
                u = j / (stripes * 2)
                verts.append((x0 + w * u, y, z))
                uvs.append((u * stripes * 0.25 * 1.2, t * 1.2))
        m = stripes * 2 + 1
        faces = [[i * m + j, i * m + j + 1, (i + 1) * m + j + 1, (i + 1) * m + j] for i in range(n) for j in range(m - 1)]
        if side < 0:
            faces = [f[::-1] for f in faces]
        uvmesh(s, verts, faces, uvs, pal.Awning_Teal)


@asset("Deckchair", "Furnish")
def deckchair(s):
    """Harbour-esplanade deckchair, 0.66 x 1.05 x 0.97 m: chrome tube frames, teal / cream striped canvas sling,
    seafoam armrests, faces -Y."""
    top, front = (0, 0.36, 0.93), (0, -0.4, 0.42)
    for sx in (-1, 1):
        s.sweep([(sx * 0.27, -0.5, 0.0), (sx * 0.27, 0.4, 0.97)], 0.016, pal.Chrome, seg=10, caps=False)  # back frame
        s.sweep([(sx * 0.31, 0.46, 0.0), (sx * 0.31, -0.44, 0.44)], 0.016, pal.Chrome, seg=10, caps=False)  # leg frame
        with s.at((sx * 0.29, -0.21, 0.323), SIDE_R):
            s.lathe([(0.026, -0.03), (0.026, 0.03)] + dome(0.026, 0.03, 0.012, 2), pal.Chrome, seg=12,
                    cap_bottom=False)  # pivot boss
        s.sweep([(sx * 0.31, -0.42, 0.44), (sx * 0.31, -0.42, 0.56), (sx * 0.29, 0.02, 0.6)], 0.012, pal.Chrome, seg=10,
                caps=False)  # arm strut
        s.rbox((sx * 0.31 - 0.035, -0.46, 0.585), (sx * 0.31 + 0.035, 0.05, 0.615), 0.014, pal.Enamel_Seafoam)
    for y, z, hw in ((0.37, 0.94, 0.27), (-0.43, 0.43, 0.31), (-0.49, 0.015, 0.27), (0.45, 0.015, 0.31)):
        s.sweep([(-hw, y, z), (hw, y, z)], 0.014, pal.Chrome, seg=10, caps=False)
    sling(s, (0, top[1], top[2] - 0.01), (0, front[1] + 0.02, front[2] + 0.01), 0.52, 0.2)


# ----------------------------------------------------------------- round bench

@asset("RoundBench", "Furnish", bevel=0.006)
def round_bench(s):
    """Ring bench around a 2.2 m tree pit (no tree): outer R 1.6 m, seat 0.45 m facing out, inner backrest to
    0.84 m, eight teal Googie fins on chrome shoes. Free-standing; centre it on a ground tree pit."""
    S, n = 48, 8
    seat = [(1.6, 0.4)] + [(1.585 + 0.03 * math.cos(math.radians(a)), 0.425 + 0.03 * math.sin(math.radians(a)))
                           for a in (-30, 0, 30, 60, 90)] + [(1.2, 0.455), (1.18, 0.452)]
    s.lathe(seat, pal.Enamel_Seafoam, seg=S, cap_bottom=False, cap_top=False)
    s.lathe([(1.18, 0.452), (1.15, 0.45), (1.15, 0.4)], pal.Enamel_Seafoam, seg=S, cap_bottom=False, cap_top=False)
    s.lathe([(1.617, 0.402), (1.617, 0.388)], pal.Chrome, seg=S, cap_bottom=False, cap_top=False)
    s.lathe([(1.2, 0.45)] + [(1.1675 + 0.0325 * math.cos(math.radians(a)), 0.8 + 0.0325 * math.sin(math.radians(a)))
                             for a in range(0, 181, 30)] + [(1.135, 0.45)], pal.Enamel_Ivory, seg=S, cap_bottom=False,
            cap_top=False)
    s.lathe([(1.203, 0.62), (1.203, 0.64)], pal.Gold, seg=S, cap_bottom=False, cap_top=False)
    for k in range(16):  # segment seams on the seat and backrest
        a = math.radians(360 * k / 16)
        c, sn, w = math.cos(a), math.sin(a), 0.003
        s.face([(1.21 * c + w * sn, 1.21 * sn - w * c, 0.4565), (1.58 * c + w * sn, 1.58 * sn - w * c, 0.4565),
                (1.58 * c - w * sn, 1.58 * sn + w * c, 0.4565), (1.21 * c - w * sn, 1.21 * sn + w * c, 0.4565)],
               pal.Enamel_Teal)
        s.face([(1.2015 * c - w * sn, 1.2015 * sn + w * c, 0.47), (1.2015 * c + w * sn, 1.2015 * sn - w * c, 0.47),
                (1.2015 * c + w * sn, 1.2015 * sn - w * c, 0.79), (1.2015 * c - w * sn, 1.2015 * sn + w * c, 0.79)],
               pal.Enamel_Seafoam)
    fin = [(1.14, 0.0), (1.52, 0.0), (1.56, 0.02), (1.5, 0.08), (1.42, 0.39), (1.14, 0.39)]
    for k in range(n):
        a = 360 * (k + 0.5) / n
        with s.at((0, 0, 0), (0, 0, a)):
            with s.at((0, 0, 0), (90, 0, 0)):
                flat(s, fin, -0.025, 0.025, pal.Enamel_Teal)
                s.face([(x, y, -0.025) for x, y in fin][::-1], pal.Enamel_Teal)
            flat(s, rrect(0.46, 0.08, 0.02, 2, 1.35, 0.0), 0.0, 0.025, pal.Chrome)
