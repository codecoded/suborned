"""Deco Kit rooftop accents: the retro-future roofline over the streamline frontage (studies: spaceage/sky_bridges,
mega/machine_crowns, mega/sphere_station). Pivot = base centre on the roof deck, front -Y, no bevel.
  Roof_Radome   drum and panelled radome, 7 m.         Roof_Mast   ringed lattice mast with a red beacon, 21 m.
  Roof_Fin      ivory fin with chrome rings, 11 m.     Roof_Dish   radar dish on a pedestal, tilted to the sky, 7 m.
  Roof_Tank     panelled water sphere on four legs, 9 m.  Roof_Drum  glazed observation cupola and flagpole, 8 m.
"""

import math

from kit import asset
from mats import pal
from mega_parts import lens, ring_girder, strut, truss


def ball(s, c, r, mat, seg=24, n=12):
    with s.at(c):
        s.lathe([(r * math.sin(math.pi * k / n), -r * math.cos(math.pi * k / n)) for k in range(n + 1)], mat, seg=seg)


def plinth(s, r, h):
    s.lathe([(r, 0.0), (r, h), (0.0, h)], pal.Limestone_Tex, seg=24, cap_bottom=False)
    s.lathe([(r + 0.03, h - 0.45), (r + 0.03, h - 0.2)], pal.Enamel_Teal, seg=24, cap_bottom=False, cap_top=False)


@asset("Roof_Radome", "Rooftop", bevel=0.0)
def roof_radome(s):
    plinth(s, 2.4, 1.6)
    ball(s, (0, 0, 4.2), 2.8, pal.Hull_Ivory, seg=32)
    s.lathe([(2.95, 3.9), (3.15, 4.2), (2.95, 4.5)], pal.Chrome, seg=32, cap_bottom=False, cap_top=False)


@asset("Roof_Mast", "Rooftop", bevel=0.0)
def roof_mast(s):
    plinth(s, 1.8, 1.0)
    truss(s, (0, 0, 1.0), (0, 0, 17.0), 0.9, 0.3, 8, pal.Steel_Ivory, chord=0.14, lace=0.07)
    for z, r in ((6.0, 1.7), (10.5, 1.3), (14.5, 0.9)):
        lens(s, z, 0.2, r, 0.35, pal.Steel_Ivory, glaze=False, seg=24)
    s.lathe([(0.0, 17.0), (0.12, 17.0), (0.03, 20.6), (0.0, 20.6)], pal.Chrome, seg=8)
    ball(s, (0, 0, 17.3), 0.28, pal.Signal_Red, seg=12, n=6)


@asset("Roof_Fin", "Rooftop", bevel=0.0)
def roof_fin(s):
    s.box((-0.9, -1.8, 0.0), (0.9, 1.8, 0.8), pal.Limestone_Tex)
    s.rbox((-0.28, -1.6, 0.8), (0.28, 1.6, 11.0), 0.25, pal.Enamel_Ivory)
    for k, z in enumerate((6.4, 7.6, 8.8)):
        s.box((-0.31, -1.63, z), (0.31, 1.3 - 0.4 * k, z + 0.3), pal.Chrome)
    s.box((-0.3, -1.62, 1.6), (0.3, -1.2, 5.6), pal.Enamel_Coral)


@asset("Roof_Dish", "Rooftop", bevel=0.0)
def roof_dish(s):
    plinth(s, 1.3, 1.2)
    s.lathe([(0.45, 1.2), (0.4, 3.4), (0.0, 3.4)], pal.Steel_Ivory, seg=16, cap_bottom=False)
    with s.at((0, 0, 3.8), (-35, 0, 0)):
        prof = [(3.2 * k / 8, 0.9 * (k / 8) ** 2) for k in range(9)]
        s.lathe(prof, pal.Hull_Ivory, seg=40, cap_bottom=False, cap_top=False)
        s.lathe([(r, z + 0.08) for r, z in prof][::-1], pal.Hull_Ivory, seg=40, cap_bottom=False, cap_top=False)
        s.lathe([(3.2, 0.9), (3.32, 0.98), (3.2, 1.06)], pal.Chrome, seg=40, cap_bottom=False, cap_top=False)
        for a in (0, 120, 240):  # feed-horn legs
            x, y = 2.6 * math.cos(math.radians(a)), 2.6 * math.sin(math.radians(a))
            strut(s, (x, y, 0.62), (0.0, 0.0, 2.4), 0.07, pal.Steel_Ivory)
        ball(s, (0, 0, 2.5), 0.22, pal.Chrome, seg=12, n=6)
    strut(s, (0, 0, 3.4), (0, 0, 4.0), 0.5, pal.Steel_Ivory)


@asset("Roof_Tank", "Rooftop", bevel=0.0)
def roof_tank(s):
    for k in range(4):
        a = math.radians(45 + 90 * k)
        foot, head = (2.4 * math.cos(a), 2.4 * math.sin(a), 0.0), (1.7 * math.cos(a), 1.7 * math.sin(a), 5.2)
        truss(s, foot, head, 0.22, 0.16, 3, pal.Steel_Ivory, chord=0.1, lace=0.05)
    ring_girder(s, 2.05, 2.6, 8, 0.12, pal.Steel_Ivory)
    ball(s, (0, 0, 6.6), 2.4, pal.Hull_Ivory, seg=32)
    s.lathe([(2.42, 6.4), (2.42, 6.8)], pal.Enamel_Teal, seg=32, cap_bottom=False, cap_top=False)
    s.lathe([(0.0, 9.0), (0.08, 9.0), (0.02, 10.4), (0.0, 10.4)], pal.Chrome, seg=6)


@asset("Roof_Drum", "Rooftop", bevel=0.0)
def roof_drum(s):
    plinth(s, 3.2, 0.9)
    s.lathe([(2.9, 0.9), (2.9, 3.4)], pal.GlassDark, seg=32, cap_bottom=False, cap_top=False)
    s.lathe([(2.4, 0.9), (2.4, 3.4), (0.0, 3.4)], pal.Enamel_Charcoal, seg=24, cap_bottom=False)
    for k in range(12):
        with s.at((0, 0, 0), (0, 0, 30 * k)):
            s.box((2.82, -0.04, 0.9), (2.96, 0.04, 3.4), pal.Chrome)
    lens(s, 3.4, 0.5, 3.7, 0.5, pal.Enamel_Ivory, glaze=False, seg=32)
    s.lathe([(1.6, 3.9), (1.6, 4.4), (1.2, 4.6), (0.0, 4.6)], pal.Enamel_Ivory, seg=24, cap_bottom=False)
    s.lathe([(0.0, 4.6), (0.07, 4.6), (0.03, 8.4), (0.0, 8.4)], pal.Chrome, seg=6)
