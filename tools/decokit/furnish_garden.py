"""Furnish: planters, pots, hanging basket, window box (registered via furnish.py).

Foliage is plants.py cards (one Cards per Shape). Hard containers keep the kit bevel (bent card normals survive it);
the two palms-in-pots use bevel 0 as plants.py does, so their thin petioles stay cheap 5-sided tubes."""

import math
import random

from mathutils import Vector

from furnish_parts import FRONT, SIDE_R, Z, bed, boomerang, flat, knob, mound, starburst, trail
from geo import circle, dome, rrect
from kit import asset
from mats import pal
from plants import Cards, fan_leaf, feather_frond, rings_sweep


def disc(s, r, z, mat, n=24):
    s.face([(x, y, z) for x, y in circle(r, n)], mat)


def rim_face(s, poly, z, mat):
    s.face([(x, y, z) for x, y in poly], mat)


def drapes(cd, rng, p, out, i):
    """One spray over a rim: odd i = short coral bloom, even i = leaf trail of random length."""
    if i % 2:
        trail(cd, rng, [(p, out)], pal.Flower_Coral, rng.uniform(0.15, 0.18), 0.1)
    else:
        trail(cd, rng, [(p, out)], pal.Leaf_Shrub, rng.uniform(0.16, 0.2), rng.choice((0.1, 0.1, 0.26)))


# ----------------------------------------------------------------- urn

@asset("Planter_Urn", "Furnish")
def planter_urn(s):
    """Stepped terrazzo plinth, teal-and-ivory Deco goblet urn with gold beads and chrome scroll handles, clipped
    boxwood ball: 0.66 x 0.54 x 1.3 m."""
    rng = random.Random(5101)
    cd = Cards(s)
    s.rbox((-0.27, -0.27, 0), (0.27, 0.27, 0.07), 0.02, pal.Terrazzo_Ivory)
    s.rbox((-0.215, -0.215, 0.07), (0.215, 0.215, 0.12), 0.015, pal.Enamel_Teal)
    S = 24
    s.lathe([(0.2, 0.12), (0.2, 0.135), (0.17, 0.152), (0.12, 0.185), (0.09, 0.235), (0.085, 0.29)],
            pal.Enamel_Ivory, seg=S, cap_bottom=False, cap_top=False)
    s.lathe([(0.085, 0.29), (0.104, 0.298), (0.108, 0.31), (0.104, 0.322), (0.085, 0.33)], pal.Gold, seg=S,
            cap_bottom=False, cap_top=False)
    s.lathe([(0.085, 0.33), (0.13, 0.358), (0.195, 0.398), (0.245, 0.445), (0.272, 0.5)], pal.Enamel_Teal,
            seg=S, cap_bottom=False, cap_top=False)
    s.lathe([(0.272, 0.5), (0.284, 0.51), (0.287, 0.522), (0.284, 0.534), (0.276, 0.542)], pal.Gold, seg=S,
            cap_bottom=False, cap_top=False)
    s.lathe([(0.276, 0.542), (0.282, 0.6), (0.296, 0.648), (0.316, 0.68), (0.33, 0.7), (0.328, 0.714),
             (0.312, 0.718), (0.298, 0.708), (0.292, 0.69)], pal.Enamel_Ivory, seg=S, cap_bottom=False, cap_top=False)
    disc(s, 0.293, 0.69, pal.Soil)
    for sx in (1, -1):  # chrome scroll handles: out from the rim, down, back into the bead
        s.sweep([(sx * 0.3, 0, 0.66), (sx * 0.37, 0, 0.665), (sx * 0.405, 0, 0.625), (sx * 0.39, 0, 0.565),
                 (sx * 0.34, 0, 0.535), (sx * 0.29, 0, 0.525)], 0.014, pal.Chrome, seg=10, caps=False)
        with s.at((sx * 0.405, 0, 0.6)):
            s.lathe(dome(0.022, 0, 0.022, 3), pal.Gold, seg=10, cap_bottom=False)
    s.lathe([(0.03, 0.69), (0.026, 0.76)], pal.Bark_Fibre, seg=8, cap_bottom=False, cap_top=False)
    c = Vector((0, 0, 0.97))
    with s.at(tuple(c)):
        s.lathe([(0.0, -0.22)] + dome(0.22, 0.0, -0.22, 3)[::-1][1:] + dome(0.22, 0.0, 0.22, 3)[1:], pal.Leaf_CoreShrub,
                seg=12)
    mound(cd, rng, c, 0.27, pal.Leaf_Shrub, 0.2, per=64, up=0.15)


# ----------------------------------------------------------------- trough

@asset("Planter_Trough", "Furnish")
def planter_trough(s):
    """2 m flared seafoam trough on a chrome kick plinth, ivory rim, coral speed whiskers front and back, a
    loaf of shrub with coral flowering clumps: 2.04 x 0.54 x 0.5 m (planting to ~0.8)."""
    rng = random.Random(5202)
    cd = Cards(s)
    s.rbox((-0.94, -0.19, 0), (0.94, 0.19, 0.07), 0.03, pal.Chrome)
    s.loft([(rrect(1.9, 0.4, 0.08, 4), 0.07), (rrect(2.0, 0.5, 0.1, 4), 0.46)], pal.Enamel_Seafoam,
           cap_bottom=False, cap_top=False)
    s.loft([(rrect(2.0, 0.5, 0.1, 4), 0.46), (rrect(2.04, 0.54, 0.12, 4), 0.475), (rrect(2.04, 0.54, 0.12, 4), 0.5),
            (rrect(1.94, 0.44, 0.07, 4), 0.505), (rrect(1.92, 0.42, 0.06, 4), 0.47)], pal.Enamel_Ivory,
           cap_bottom=False, cap_top=False)
    rim_face(s, rrect(1.92, 0.42, 0.06, 4), 0.47, pal.Soil)

    def y(z):  # flared side at height z, 2 mm proud
        return 0.2 + 0.05 * (z - 0.07) / 0.39 + 0.002
    for sy in (-1, 1):
        for L, z in ((0.62, 0.34), (0.48, 0.29), (0.34, 0.24)):
            for sx in (-1, 1):
                x0, x1 = (-0.84, -0.84 + L) if sx < 0 else (0.84 - L, 0.84)
                q = [(x0, sy * y(z), z), (x1, sy * y(z), z), (x1, sy * y(z + 0.022), z + 0.022),
                     (x0, sy * y(z + 0.022), z + 0.022)]
                s.face(q if sy < 0 else q[::-1], pal.Enamel_Coral)
        with s.at((0, sy * y(0.3), 0.3), (-sy * 97.3, 0, 0)):  # square to the flared face
            starburst(s, 0.075, pal.Gold, n=8, t=0.008, k=0.35)
    with s.at((0, 0, 0.47), scale=(0.93, 0.19, 0.13)):
        s.lathe([(1.0, 0.0)] + dome(1.0, 0.1, 0.9, n=4), pal.Leaf_CoreShrub, seg=16, cap_bottom=False)
    bed(cd, rng, -0.92, 0.92, -0.19, 0.19, 0.47, h=0.13)


# ----------------------------------------------------------------- potted palm / fern

@asset("PottedPalm_Small", "Furnish", bevel=0.0)
def potted_palm_small(s):
    """Atomic-age mustard cone pot on three splayed chrome hairpin legs with gold ball feet, a short fibrous fan
    palm: ~1.3 x 1.3 x 1.5 m."""
    rng = random.Random(5303)
    cd = Cards(s)
    S = 24
    for k in range(3):
        a = math.radians(-90 + 120 * k)
        o = Vector((math.cos(a), math.sin(a), 0))
        s.sweep([o * 0.1 + Z * 0.4, o * 0.2 + Z * 0.19, o * 0.285 + Z * 0.03], 0.011, pal.Chrome, seg=10, caps=False)
        with s.at(tuple(o * 0.29)):
            s.lathe([(0.0, 0.0), (0.02, 0.006)] + dome(0.026, 0.024, 0.024, 3)[1:], pal.Gold, seg=10)
    s.lathe([(0, 0.25), (0.06, 0.258), (0.095, 0.28), (0.11, 0.305), (0.205, 0.555), (0.236, 0.64)],
            pal.Enamel_Mustard, seg=S, cap_bottom=False, cap_top=False)
    r = lambda z: 0.11 + (z - 0.305) / 0.25 * 0.095  # noqa: E731  cone radius
    s.lathe([(r(0.43), 0.43), (r(0.435) + 0.006, 0.438), (r(0.47) + 0.006, 0.472), (r(0.477), 0.48)],
            pal.Enamel_Coral, seg=S, cap_bottom=False, cap_top=False)
    s.lathe([(0.236, 0.64), (0.248, 0.65), (0.252, 0.665), (0.244, 0.678), (0.228, 0.676), (0.222, 0.662)],
            pal.Enamel_Ivory, seg=S, cap_bottom=False, cap_top=False)
    disc(s, 0.224, 0.655, pal.Soil)
    H = 0.3
    with s.at((0, 0, 0.64)):
        rings_sweep(s, H, lambda z: 0.065 + 0.03 * math.exp(-z / 0.08), pal.Bark_Fibre, lambda z: (0.0, 0.0, z),
                    seg=10, band=0.07, depth=0.1)
        s.lathe(dome(0.075, H - 0.03, 0.1, n=3), pal.Bark_Fibre, seg=10, cap_bottom=False)
    top = Vector((0, 0, 0.64 + H))
    cc, cr = top + Z * 0.25, 0.7
    n = 16
    for i in range(n):
        yaw = math.radians(i * 137.5 + rng.uniform(-10, 10))
        el = 84 - 66 * (i / (n - 1)) ** 1.1 + rng.uniform(-5, 5)
        L = rng.uniform(0.28, 0.42) * (0.85 if el < 35 else 1.0)
        o = Vector((math.cos(yaw), math.sin(yaw), 0))
        p = top + Z * 0.05 + o * 0.03
        pts = [p]
        for k in range(1, 4):
            e = math.radians(el + 6 - 20 * k / 3)
            p = p + (o * math.cos(e) + Z * math.sin(e)) * L / 3
            pts.append(p)
        s.sweep(pts, [0.01, 0.009, 0.008, 0.006], pal.Petiole, seg=5)
        d = ((pts[-1] - pts[-2]).normalized() - Z * 0.15).normalized()
        fan_leaf(cd, rng, pts[-1] - d * 0.02, d, rng.uniform(0.44, 0.54), cc, cr, pal.Leaf_PalmFan,
                 droop=rng.uniform(0.18, 0.28))


@asset("PottedFern", "Furnish", bevel=0.0)
def potted_fern(s):
    """Seafoam bowl jardiniere on an ivory tulip pedestal, chrome rim, arching fronds spilling to the floor line:
    ~1.4 x 1.4 x 1.1 m."""
    rng = random.Random(5404)
    cd = Cards(s)
    S = 24
    h = 0.2  # pedestal lift: the bowl clears the frond skirt
    s.lathe([(0.2, 0.0), (0.2, 0.012), (0.17, 0.03), (0.1, 0.075), (0.056, 0.14), (0.042, 0.22 + h),
             (0.05, 0.28 + h), (0.1, 0.31 + h)], pal.Enamel_Ivory, seg=S, cap_bottom=False, cap_top=False)
    with s.at((0, 0, h)):
        s.lathe([(0.1, 0.31), (0.17, 0.33), (0.23, 0.37), (0.262, 0.42), (0.27, 0.47)], pal.Enamel_Seafoam, seg=S,
                cap_bottom=False, cap_top=False)
        s.lathe([(0.27, 0.47), (0.282, 0.478), (0.285, 0.492), (0.276, 0.503), (0.262, 0.5), (0.256, 0.488)],
                pal.Chrome, seg=S, cap_bottom=False, cap_top=False)
        s.lathe([(0.25, 0.4), (0.265, 0.405), (0.268, 0.42), (0.262, 0.425)], pal.Gold, seg=S, cap_bottom=False,
                cap_top=False)
        disc(s, 0.258, 0.485, pal.Soil)
        s.lathe([(0.12, 0.47), (0.08, 0.55), (0, 0.57)], pal.Leaf_CoreShrub, seg=10, cap_bottom=False)
    cc, cr = (0, 0, 0.62 + h), 0.7
    n = 22
    for i in range(n):
        yaw = i * 137.5 + rng.uniform(-12, 12)
        f = i / (n - 1)
        o = Vector((math.cos(math.radians(yaw)), math.sin(math.radians(yaw)), 0))
        feather_frond(cd, Vector((0, 0, 0.5 + h)) + o * 0.04, yaw, 80 - 42 * f + rng.uniform(-6, 6),
                      85 + 35 * f + rng.uniform(-10, 10), rng.uniform(0.62, 0.78) * (0.8 if f < 0.2 else 1.0),
                      28 + 10 * f, rng.uniform(-25, 25), cc, cr, pal.Leaf_PalmFeather)


# ----------------------------------------------------------------- hanging basket

@asset("HangingBasket", "Furnish")
def hanging_basket(s):
    """Wall bracket (teal wall rose, chrome arm and brace, coral boomerang web, gold finial) holding a teal bowl of
    shrub and coral flowers trailing over the rim at ~2.6 m. Pivot on the wall face at ground: the wall is y = 0,
    the bracket projects -Y 0.55 m."""
    rng = random.Random(5505)
    cd = Cards(s)
    s.rbox((-0.065, -0.03, 2.8), (0.065, 0.0, 3.16), 0.018, pal.Enamel_Teal)
    with s.at((0, -0.03, 2.98), FRONT):
        starburst(s, 0.05, pal.Gold, n=8, t=0.006, k=0.4)
    Y = -0.5
    s.sweep([(0, -0.02, 3.08), (0, Y + 0.02, 3.08), (0, Y - 0.03, 3.1)], 0.014, pal.Chrome, seg=10, caps=False)
    s.sweep([(0, -0.02, 2.86), (0, -0.28, 3.07)], 0.011, pal.Chrome, seg=10, caps=False)
    with s.at((0, Y - 0.04, 3.1)):
        s.lathe([(0.0, -0.022)] + dome(0.024, 0.0, 0.024, 3)[:-1] + [(0.0, 0.024)], pal.Gold, seg=10)
    web = [(-0.02, 3.075), (-0.25, 3.075), (-0.02, 2.885)]  # coral web filling the bracket triangle (x = 0 plane)
    with s.at((-0.004, 0, 0), SIDE_R):
        flat(s, web, 0.0, 0.008, pal.Enamel_Coral)
        s.face([(y, z, 0.0) for y, z in web[::-1]], pal.Enamel_Coral)
    with s.at((0, -0.3, 3.088), SIDE_R):  # mustard boomerang fin riding the arm
        flat(s, boomerang(0.035, 0.2, 0.05), -0.005, 0.005, pal.Enamel_Mustard)
        s.face([(x, y, -0.005) for x, y in boomerang(0.035, 0.2, 0.05)[::-1]], pal.Enamel_Mustard)
    zr, R = 2.68, 0.23
    for k in range(3):  # hanger rods from the hook to the rim
        a = math.radians(90 + 120 * k)
        s.sweep([(0, Y, 3.06), (R * 0.92 * math.cos(a), Y + R * 0.92 * math.sin(a), zr + 0.01)], 0.004, pal.Chrome,
                seg=10, caps=False)
    with s.at((0, Y, 0)):
        s.lathe([(0.0, 2.43), (0.05, 2.44), (0.12, 2.47), (0.18, 2.52), (0.215, 2.58), (0.228, 2.64), (0.23, zr)],
                pal.Enamel_Teal, seg=20, cap_top=False)
        s.lathe([(0.23, zr), (0.24, zr + 0.008), (0.238, zr + 0.022), (0.222, zr + 0.024), (0.216, zr + 0.012)],
                pal.Enamel_Ivory, seg=20, cap_bottom=False, cap_top=False)
        s.lathe([(0.187, 2.54), (0.198, 2.55), (0.196, 2.57), (0.21, 2.565)], pal.Gold, seg=20, cap_bottom=False,
                cap_top=False)
        with s.at((0, 0, 2.39)):
            knob(s, 0.028, pal.Gold)
        disc(s, 0.218, zr + 0.005, pal.Leaf_CoreShrub, n=16)
    c = Vector((0, Y, zr + 0.06))
    mound(cd, rng, c, 0.19, pal.Leaf_Shrub, 0.14, per=34, up=0.3)
    mound(cd, rng, c + Z * 0.07, 0.14, pal.Flower_Coral, 0.13, per=20, fill=False, up=0.8)
    for i in range(9):  # drapes at irregular lengths: the teal bowl shows between them
        a = math.radians(i * 40 + rng.uniform(-10, 10))
        o = Vector((math.cos(a), math.sin(a), 0))
        drapes(cd, rng, c + o * 0.2 - Z * 0.04, o, i)


# ----------------------------------------------------------------- window box

@asset("WindowBox", "Furnish")
def window_box(s):
    """1.4 m ivory window box with a teal rim, chrome shoes, teal speed lines and a gold starburst; shrub and
    coral flowers with sprays over the front lip. Sits on a sill at z = 0, back toward +Y: 1.44 x 0.28 x 0.24 m."""
    rng = random.Random(5606)
    cd = Cards(s)
    for sx in (-1, 1):
        s.rbox((sx * 0.55 - 0.06, -0.11, 0), (sx * 0.55 + 0.06, 0.11, 0.016), 0.006, pal.Chrome)
    s.loft([(rrect(1.36, 0.2, 0.03, 3), 0.016), (rrect(1.4, 0.24, 0.04, 3), 0.2)], pal.Enamel_Ivory,
           cap_bottom=False, cap_top=False)
    s.loft([(rrect(1.4, 0.24, 0.04, 3), 0.2), (rrect(1.44, 0.28, 0.05, 3), 0.21), (rrect(1.44, 0.28, 0.05, 3), 0.232),
            (rrect(1.36, 0.2, 0.025, 3), 0.236), (rrect(1.34, 0.18, 0.02, 3), 0.21)], pal.Enamel_Teal,
           cap_bottom=False, cap_top=False)
    s.face([(x, y, 0.016) for x, y in rrect(1.36, 0.2, 0.03, 3)][::-1], pal.Enamel_Ivory)
    rim_face(s, rrect(1.34, 0.18, 0.02, 3), 0.21, pal.Soil)

    def y(z):
        return -(0.1 + 0.02 * (z - 0.016) / 0.184) - 0.0015
    for sx in (-1, 1):
        for k, L in enumerate((0.44, 0.34, 0.24)):
            z = 0.13 - k * 0.035
            x0, x1 = (-0.64, -0.64 + L) if sx < 0 else (0.64 - L, 0.64)
            s.face([(x0, y(z), z), (x1, y(z), z), (x1, y(z + 0.014), z + 0.014), (x0, y(z + 0.014), z + 0.014)],
                   pal.Enamel_Teal)
    with s.at((0, y(0.11), 0.11), (96.2, 0, 0)):
        starburst(s, 0.065, pal.Gold, n=8, t=0.006, k=0.35, hub=pal.Enamel_Coral)
    bed(cd, rng, -0.66, 0.66, -0.085, 0.085, 0.21, nx=7, h=0.1)
    for i, x in enumerate((-0.6, -0.41, -0.18, 0.02, 0.27, 0.47, 0.62)):
        drapes(cd, rng, (x + rng.uniform(-0.03, 0.03), -0.13, 0.24), (0, -1), i)
    for sx in (-1, 1):
        drapes(cd, rng, (sx * 0.7, 0, 0.24), (sx, 0), 0)
