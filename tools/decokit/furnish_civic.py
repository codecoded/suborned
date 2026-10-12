"""Furnish: civic and waterfront street pieces (registered via furnish.py): street clock, drinking fountain,
pneumatic tube-mail post, coin telescope, lifebuoy stand. Pivot = footprint centre at z = 0, front -Y."""

import math

from geo import dome, fillet, rrect
from kit import asset
from mats import pal, register
from furnish_parts import (FRONT, SIDE_L, SIDE_R, flat, frame, knob, lamp, letters, starburst, strip)

register("DK_Dial", "emit", "#FFF1D6", 0.8)  # softly lit clock dial
register("DK_Rope", "fabric", "#D8C7A0")


def band(s, r0, r1, z0, z1, mat, seg=16):
    s.lathe([(r0, z0), (r1, (z0 + z1) / 2), (r0, z1)], mat, seg=seg, cap_bottom=False, cap_top=False)


def ring_pts(r, n, a0=0.0):
    return [(r * math.cos(math.radians(a0 + 360 * k / n)), r * math.sin(math.radians(a0 + 360 * k / n))) for k in range(n)]


# ----------------------------------------------------------------- street clock

def dial(s, R):
    """One clock face on local XY (+Z out): lit dial, Deco numerals at 12 / 3 / 6 / 9, gold batons, spade hands."""
    s.face([(x, y, 0.0) for x, y in ring_pts(R, 28)], pal.Dial)
    for t, (x, y) in (("12", (0, 0.7)), ("3", (0.72, 0)), ("6", (0, -0.7)), ("9", (-0.72, 0))):
        letters(s, t, R * 0.26, pal.Enamel_Charcoal, x=x * R, y=y * R, lift=0.0, wt=0.17, depth=0.003, track=0.1,
                step=2)
    for h in range(12):
        if h % 3:
            a = math.radians(90 - 30 * h)
            c, sn, w = math.cos(a), math.sin(a), 0.008
            p = [(r * c - k * w * sn, r * sn + k * w * c) for r, k in ((R * 0.74, -1), (R * 0.9, -1), (R * 0.9, 1), (R * 0.74, 1))]
            s.face([(x, y, 0.002) for x, y in p], pal.Gold)
    for a, L, w in ((125, 0.5, 0.009), (48, 0.8, 0.006)):  # hands at 10:10: spade hour, slim minute
        c, sn = math.cos(math.radians(a)), math.sin(math.radians(a))
        spade = [(-0.12, 0), (0.0, -w), (0.72, -w * 0.6), (0.84, -w * 1.8), (1.0, 0), (0.84, w * 1.8), (0.72, w * 0.6),
                 (0.0, w)]
        flat(s, [(u * L * R * c - v * sn, u * L * R * sn + v * c) for u, v in spade], 0.004, 0.008, pal.Bakelite)
    s.lathe(dome(0.018, 0.008, 0.012, 2), pal.Gold, seg=10, cap_bottom=False)


@asset("StreetClock", "Furnish")
def street_clock(s):
    """4.25 m pole clock: stepped octagonal Deco base, teal column with gold collars, double-faced drum (lit ivory
    dials, Deco numerals, gold batons, spade hands) in chrome bezels, coral side fins, chrome crown, gold starburst."""
    with s.at((0, 0, 0), (0, 0, 22.5)):  # octagon: a flat faces the front
        s.lathe([(0.34, 0.0), (0.34, 0.07), (0.28, 0.09), (0.28, 0.3), (0.22, 0.33), (0.2, 0.56), (0.12, 0.64)],
                pal.Enamel_Ivory, seg=8, cap_bottom=False, cap_top=False)
    s.lathe([(0.08, 0.62), (0.075, 1.5), (0.068, 3.08), (0.1, 3.14), (0.13, 3.21)], pal.Enamel_Teal, seg=16,
            cap_bottom=False, cap_top=False)
    for z in (0.66, 3.02):
        band(s, 0.075, 0.09, z - 0.025, z + 0.025, pal.Gold)
    Zc, R = 3.62, 0.41
    with s.at((0, 0, Zc), FRONT):  # drum: local z = -world y
        s.lathe([(R - 0.03, -0.11), (R - 0.009, -0.095), (R, -0.055), (R, 0.055), (R - 0.009, 0.095), (R - 0.03, 0.11)],
                pal.Enamel_Teal, seg=28, cap_bottom=False, cap_top=False)
        for rot in ((0, 0, 0), (0, 180, 0)):
            with s.at((0, 0, 0), rot):  # bezel: loose band + face ring (crisp, no bevel strips)
                s.lathe([(R - 0.018, 0.1), (R - 0.018, 0.122)], pal.Chrome, seg=28, cap_bottom=False, cap_top=False)
                s.lathe([(R - 0.018, 0.122), (R - 0.06, 0.13)], pal.Chrome, seg=28, cap_bottom=False, cap_top=False)
                with s.at((0, 0, 0.118)):
                    dial(s, R - 0.058)
        for sx in (-1, 1):  # Googie side fins
            fin = [(R - 0.02, -0.11), (R + 0.12, -0.23), (R + 0.145, -0.19), (R + 0.07, 0.04), (R - 0.02, 0.13)]
            flat(s, [(sx * x, y) for x, y in (fin if sx > 0 else fin[::-1])], -0.012, 0.012, pal.Enamel_Coral)
            s.face([(sx * x, y, -0.012) for x, y in (fin[::-1] if sx > 0 else fin)], pal.Enamel_Coral)
    s.rbox((-0.1, -0.13, Zc + R - 0.04), (0.1, 0.13, Zc + R + 0.02), 0.025, pal.Chrome)  # crown saddle
    s.lathe([(0.1, Zc + R + 0.02), (0.098, Zc + R + 0.05), (0.08, Zc + R + 0.07), (0.07, Zc + R + 0.11),
             (0.05, Zc + R + 0.14), (0.035, Zc + R + 0.19), (0.014, Zc + R + 0.3)], pal.Enamel_Teal, seg=16,
            cap_bottom=False, cap_top=False)  # crown
    band(s, 0.07, 0.078, Zc + R + 0.09, Zc + R + 0.12, pal.Gold)
    with s.at((0, 0, Zc + R + 0.3), FRONT):
        starburst(s, 0.08, pal.Gold, n=8, t=0.012, k=0.35)
    s.lathe([(0.012, Zc + R + 0.38)] + dome(0.018, Zc + R + 0.39, 0.03, 3), pal.Gold, seg=10, cap_bottom=False)


# ----------------------------------------------------------------- drinking fountain

@asset("DrinkingFountain", "Furnish")
def drinking_fountain(s):
    """0.92 m pedestal bubbler: seafoam column with ivory fluting on a chrome foot, chrome basin with a starburst
    drain, hooded bubbler and water arc, push button on the rim, and a ground-level dog bowl on the right."""
    s.lathe([(0.27, 0.0), (0.27, 0.03), (0.23, 0.05), (0.2, 0.08)], pal.Enamel_Teal, seg=20, cap_bottom=False,
            cap_top=False)
    s.lathe([(0.272, 0.012), (0.272, 0.022)], pal.Chrome, seg=20, cap_bottom=False, cap_top=False)
    col = [(0.2, 0.075), (0.16, 0.12), (0.135, 0.2), (0.125, 0.4), (0.125, 0.66), (0.14, 0.72), (0.18, 0.77),
           (0.22, 0.8)]
    s.lathe(col, pal.Enamel_Seafoam, seg=20, cap_bottom=False, cap_top=False)
    for k in range(10):
        a = 2 * math.pi * (k + 0.5) / 10
        c, sn = math.cos(a), math.sin(a)
        s.sweep([(0.13 * c, 0.13 * sn, 0.2), (0.126 * c, 0.126 * sn, 0.4), (0.127 * c, 0.127 * sn, 0.65)], 0.011,
                pal.Enamel_Ivory, seg=10, caps=False)
    s.lathe([(0.21, 0.79), (0.26, 0.82), (0.288, 0.858)], pal.Enamel_Ivory, seg=28, cap_bottom=False, cap_top=False)
    s.lathe([(0.288, 0.858), (0.292, 0.872), (0.28, 0.884), (0.266, 0.876)], pal.Chrome, seg=28, cap_bottom=False,
            cap_top=False)  # rim
    s.lathe([(0.266, 0.876), (0.235, 0.852), (0.14, 0.837), (0.0, 0.832)], pal.Enamel_Ivory, seg=28,
            cap_bottom=False)
    with s.at((0.05, 0.05, 0.834)):
        starburst(s, 0.05, pal.Steel, n=8, t=0.003, k=0.35)
    with s.at((0, -0.12, 0.834)):  # bubbler + guard hood
        s.lathe([(0.026, 0.0), (0.024, 0.02), (0.016, 0.05), (0.01, 0.07), (0.011, 0.078)], pal.Chrome, seg=12,
                cap_bottom=False, cap_top=False)
        s.face([(x, y, 0.078) for x, y in ring_pts(0.011, 10)], pal.Iron)
        s.sweep(fillet([(-0.035, 0.02, 0.0), (-0.035, 0.02, 0.1), (0.035, 0.02, 0.1), (0.035, 0.02, 0.0)], 0.03, n=4,
                       closed=False), 0.006, pal.Chrome, seg=10, caps=False)
        jet = fillet([(0, 0, 0.08), (0, 0.02, 0.12), (0, 0.06, 0.14), (0, 0.1, 0.12), (0, 0.12, 0.08)], 0.03, n=3,
                     closed=False)
        s.sweep(jet, [0.006 - 0.003 * i / (len(jet) - 1) for i in range(len(jet))], pal.Water, seg=10)
    with s.at((0, -0.272, 0.84), (72, 0, 0)):  # push button on the basin flank
        flat(s, rrect(0.07, 0.05, 0.015, 2), 0.0, 0.01, pal.Chrome)
        with s.at((0, 0, 0.01)):
            knob(s, 0.016, pal.Enamel_Red)
    with s.at((0.36, 0.0, 0.0)):  # dog bowl and its spigot
        s.lathe([(0.11, 0.0), (0.12, 0.04), (0.112, 0.055), (0.1, 0.05), (0.085, 0.02), (0.0, 0.015)], pal.Chrome, seg=16,
                cap_bottom=False)
        s.face([(x, y, 0.035) for x, y in ring_pts(0.093, 16)], pal.Water)
    s.sweep(fillet([(0.13, 0.0, 0.3), (0.3, 0.0, 0.3), (0.34, 0.0, 0.2)], 0.05, n=4, closed=False), 0.012, pal.Chrome,
            seg=10, caps=False)


# ----------------------------------------------------------------- pneumatic post

def capsule(s, L=0.2, r=0.045, seg=12):
    """Brass tube-mail capsule along local +Z, red end caps (centred on the origin)."""
    s.lathe([(r, -L / 2 + 0.03), (r, L / 2 - 0.03)], pal.Gold, seg=seg, cap_bottom=False, cap_top=False)
    for sgn in (-1, 1):
        with s.at((0, 0, sgn * (L / 2 - 0.03)), (0 if sgn > 0 else 180, 0, 0)):
            s.lathe([(r * 1.04, 0.0), (r * 1.04, 0.012)] + dome(r * 0.98, 0.014, 0.022, 3), pal.Enamel_Red, seg=seg,
                    cap_bottom=False)


@asset("PneumaticPost", "Furnish")
def pneumatic_post(s):
    """2.34 m tube-mail post, footprint 0.6 x 1.0 m (return pipe dives into the pavement at y = +0.45): red
    streamline pillar on a chrome foot, POST plaque, lit capsule hatch, destination dial, send lever, status lamps,
    a glass chute with a capsule in flight up to a chrome junction head with an amber beacon."""
    s.lathe([(0.3, 0.0), (0.3, 0.03), (0.26, 0.05), (0.24, 0.1)], pal.Chrome, seg=24, cap_bottom=False, cap_top=False)
    s.lathe([(0.236, 0.095), (0.24, 0.2), (0.235, 0.9), (0.225, 1.3), (0.21, 1.36), (0.175, 1.41), (0.12, 1.44)],
            pal.Enamel_Red, seg=24, cap_bottom=False, cap_top=False)
    s.lathe([(0.231, 1.12), (0.229, 1.2)], pal.Enamel_Mustard, seg=24, cap_bottom=False, cap_top=False)
    s.lathe([(0.238, 0.2), (0.244, 0.215), (0.238, 0.23)], pal.Chrome, seg=24, cap_bottom=False, cap_top=False)
    with s.at((0, 0, 1.28), FRONT):  # plaque
        flat(s, rrect(0.3, 0.11, 0.03, 3), 0.15, 0.238, pal.Enamel_Ivory)
        letters(s, "POST", 0.07, pal.Enamel_Red, lift=0.238, wt=0.17, depth=0.004)
    with s.at((0, 0, 0.92), FRONT):  # capsule hatch on a mounting plate
        flat(s, rrect(0.3, 0.34, 0.06, 3), 0.17, 0.245, pal.Enamel_Ivory)
        with s.at((0, 0.01, 0.245)):
            frame(s, 0.22, 0.2, 0.08, 0.035, 0.024, pal.Chrome, pal.Iron, pal.LampSoft)
            with s.at((0, 0, 0.018), (0, 90, 0)):
                capsule(s, 0.16, 0.035, seg=10)
            s.face([(x, y, 0.03) for x, y in rrect(0.17, 0.15, 0.055, 3)], pal.GlassDark)
            s.sweep(fillet([(0.12, -0.02, 0.0), (0.16, -0.02, 0.03), (0.16, 0.07, 0.04)], 0.03, n=3, closed=False),
                    0.008, pal.Chrome, seg=10, caps=False)  # send lever
            with s.at((0.16, 0.075, 0.04), (-90, 0, 0)):
                knob(s, 0.016, pal.Enamel_Red)
            for x, m in ((-0.05, pal.Signal_Green), (0.05, pal.Signal_Amber)):
                with s.at((x, 0.135, 0.0)):
                    lamp(s, 0.009, m)
    with s.at((0, 0, 0.62), FRONT):  # destination dial
        flat(s, rrect(0.2, 0.2, 0.1, 4), 0.2, 0.238, pal.Chrome)
        with s.at((0, 0, 0.238)):
            for k in range(8):
                a = math.radians(90 - 45 * k)
                c, sn = 0.07 * math.cos(a), 0.07 * math.sin(a)
                strip(s, c - 0.007, sn - 0.007, c + 0.007, sn + 0.007, pal.Enamel_Charcoal, 0.001)
            knob(s, 0.03, pal.Enamel_Ivory)
            strip(s, -0.004, 0.02, 0.004, 0.05, pal.Enamel_Red, 0.02)
    s.lathe([(0.13, 1.43), (0.13, 1.47), (0.1, 1.49), (0.09, 1.5)], pal.Chrome, seg=20, cap_bottom=False, cap_top=False)
    s.lathe([(0.075, 1.49), (0.075, 2.06)], pal.Glass, seg=20, cap_bottom=False, cap_top=False)  # chute
    for z in (1.66, 1.9):
        band(s, 0.076, 0.086, z - 0.012, z + 0.012, pal.Chrome, seg=20)
    with s.at((0, 0, 1.76)):
        capsule(s, 0.2, 0.05)
    s.lathe([(0.09, 2.04), (0.13, 2.08), (0.15, 2.14), (0.15, 2.2), (0.12, 2.25), (0.07, 2.28), (0.04, 2.29)],
            pal.Chrome, seg=20, cap_bottom=False, cap_top=False)  # junction head
    s.lathe([(0.042, 2.285)] + dome(0.045, 2.29, 0.05, 3), pal.Signal_Amber, seg=12, cap_bottom=False)
    band(s, 0.15, 0.158, 2.155, 2.185, pal.Enamel_Red, seg=20)
    s.sweep(fillet([(0.0, 0.1, 2.17), (0.0, 0.45, 2.17), (0.0, 0.45, 0.0)], 0.12, n=5, closed=False), 0.045,
            pal.Chrome, seg=12, caps=False)  # return pipe into the pavement
    with s.at((0, 0.45, 0)):
        s.lathe([(0.1, 0.0), (0.1, 0.02), (0.06, 0.04)], pal.Chrome, seg=12, cap_bottom=False, cap_top=False)


# ----------------------------------------------------------------- coin telescope

@asset("CoinTelescope", "Furnish")
def coin_telescope(s):
    """1.42 m esplanade viewer: teal pedestal on a chrome foot, chrome yoke, mustard binocular head tilted up 8 deg
    (twin lens barrels to -Y, visor and rubber eyecups to +Y), coin box on the right, grip handles, coral starburst."""
    s.lathe([(0.26, 0.0), (0.26, 0.03), (0.22, 0.05), (0.19, 0.07)], pal.Chrome, seg=20, cap_bottom=False, cap_top=False)
    s.lathe([(0.19, 0.065), (0.14, 0.14), (0.1, 0.3), (0.085, 0.6), (0.08, 0.92), (0.1, 0.99), (0.13, 1.03)],
            pal.Enamel_Teal, seg=20, cap_bottom=False, cap_top=False)
    band(s, 0.086, 0.1, 0.6, 0.64, pal.Gold)
    s.lathe([(0.13, 1.02), (0.15, 1.04), (0.15, 1.07), (0.12, 1.085), (0.0, 1.09)], pal.Chrome, seg=20, cap_bottom=False)
    s.lathe([(0.045, 1.08), (0.04, 1.1), (0.04, 1.14)], pal.Chrome, seg=12, cap_bottom=False, cap_top=False)  # neck
    s.sweep(fillet([(-0.222, 0, 1.3), (-0.222, 0, 1.14), (0.222, 0, 1.14), (0.222, 0, 1.3)], 0.09, n=4, closed=False),
            0.02, pal.Chrome, seg=10, caps=False)  # yoke
    for sx in (-1, 1):
        with s.at((sx * 0.2, 0, 1.28), SIDE_R if sx > 0 else SIDE_L):
            s.lathe([(0.045, 0.0), (0.045, 0.03)] + dome(0.042, 0.032, 0.015, 2), pal.Chrome, seg=12, cap_bottom=False)
    with s.at((0, 0, 1.28), (8, 0, 0)):
        s.rbox((-0.18, -0.17, -0.1), (0.18, 0.15, 0.1), 0.07, pal.Enamel_Mustard)
        s.rbox((-0.03, -0.12, 0.08), (0.03, 0.12, 0.13), 0.025, pal.Enamel_Teal)  # dorsal fin
        for x in (-0.08, 0.08):
            with s.at((x, -0.16, 0), FRONT):
                s.lathe([(0.062, 0.0), (0.066, 0.05), (0.07, 0.085), (0.064, 0.095), (0.052, 0.09)], pal.Chrome, seg=16,
                        cap_bottom=False, cap_top=False)
                s.face([(px, py, 0.08) for px, py in ring_pts(0.053, 16)], pal.GlassDark)
            with s.at((x, 0.15, 0.02), (-90, 0, 0)):
                s.lathe([(0.03, 0.0), (0.034, 0.04), (0.03, 0.05), (0.02, 0.045)], pal.Rubber, seg=12, cap_bottom=False,
                        cap_top=False)
        s.rbox((-0.17, 0.12, 0.05), (0.17, 0.2, 0.1), 0.02, pal.Enamel_Teal)  # visor
        s.rbox((0.06, -0.06, 0.07), (0.16, 0.08, 0.15), 0.02, pal.Chrome)  # coin box
        with s.at((0.11, -0.06, 0.11), FRONT):
            strip(s, -0.03, 0.005, 0.03, 0.013, pal.Iron, 0.001)
            with s.at((-0.03, -0.02, 0.0)):
                lamp(s, 0.007, pal.Signal_Green)
        with s.at((-0.18, 0.0, 0.0), SIDE_L):
            starburst(s, 0.07, pal.Enamel_Coral, n=8, t=0.006, hub=pal.Chrome)
        for sx in (-1, 1):  # grips
            s.sweep([(sx * 0.18, 0.08, -0.04), (sx * 0.3, 0.12, -0.06)], 0.012, pal.Chrome, seg=10, caps=False)
            with s.at((sx * 0.3, 0.12, -0.06), SIDE_R if sx > 0 else SIDE_L):
                s.lathe([(0.02, 0.0), (0.022, 0.06)] + dome(0.02, 0.07, 0.015, 2), pal.Enamel_Red, seg=10,
                        cap_bottom=False)


# ----------------------------------------------------------------- lifebuoy stand

@asset("LifebuoyStand", "Furnish")
def lifebuoy_stand(s):
    """1.64 m waterfront lifebuoy post: teal enamel post with ivory cap on a chrome plate, red / ivory lifebuoy
    (0.76 m) with a scalloped grab line on a chrome saddle hook, starburst plaque."""
    flat(s, rrect(0.36, 0.28, 0.05, 3), 0.0, 0.03, pal.Chrome)
    s.rbox((-0.07, -0.06, 0.02), (0.07, 0.06, 1.52), 0.03, pal.Enamel_Teal)
    s.rbox((-0.1, -0.085, 1.5), (0.1, 0.085, 1.58), 0.035, pal.Enamel_Ivory)
    s.lathe([(0.03, 1.58), (0.022, 1.6)] + dome(0.03, 1.61, 0.03, 3), pal.Chrome, seg=12, cap_bottom=False)
    with s.at((0, -0.06, 1.38), FRONT):
        flat(s, rrect(0.12, 0.12, 0.03, 3), 0.0, 0.008, pal.Enamel_Ivory)
        with s.at((0, 0, 0.008)):
            starburst(s, 0.045, pal.Enamel_Red, n=8, t=0.005)
    s.sweep(fillet([(0, -0.05, 1.21), (0, -0.235, 1.21), (0, -0.245, 1.27)], 0.03, n=3, closed=False), 0.014,
            pal.Chrome, seg=10, caps=False)  # saddle hook
    Rb, rt, yc = 0.3, 0.08, -0.135
    zc = 1.21 + 0.014 - (Rb - rt)
    for k in range(4):
        a0 = 45 + 90 * k
        path = [(Rb * math.cos(math.radians(a0 + 15 * i)), yc, zc + Rb * math.sin(math.radians(a0 + 15 * i)))
                for i in range(7)]
        s.sweep(path, rt, pal.Enamel_Red if k % 2 else pal.Enamel_Ivory, seg=10, caps=False)
        clamp = math.radians(a0)
        c, sn = math.cos(clamp), math.sin(clamp)
        with s.at((Rb * c, yc, zc + Rb * sn), (0, -a0, 0)):
            s.lathe([(rt + 0.004, -0.012), (rt + 0.008, 0.0), (rt + 0.004, 0.012)], pal.Rope, seg=10, cap_bottom=False,
                    cap_top=False)
        rope = [((Rb + rt + 0.004 + 0.04 * math.sin(math.pi * i / 6)) * math.cos(math.radians(a0 + 15 * i)), yc,
                 zc + (Rb + rt + 0.004 + 0.04 * math.sin(math.pi * i / 6)) * math.sin(math.radians(a0 + 15 * i)))
                for i in range(7)]
        s.sweep(rope, 0.008, pal.Rope, seg=10, caps=False)
