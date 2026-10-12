"""Deco Kit roof-deck family (Concepts/Blocks/needle_trio.png, mixed_skyline.png): the landscaped podium
decks DecoKitHighrise lays over a block are furnished with these; seen from the 2.5D camera a roof is half of every
building, so none is left bare. Pivot = base centre on the deck, front -Y, no bevel.
  Roof_Pool           14 x 7 m raised pool: limestone coping, teal-tiled rim, water; loungers along both sides,
                      two coral parasols, a chrome ladder.
  Roof_Conservatory   12 x 8 m glasshouse: limestone plinth, chrome-ribbed glass barrel vault, a glazed dome over
                      the crossing, shrub masses inside.
  Roof_Bandstand      8 m octagonal pavilion: stepped plinth, slender ivory columns, a gold dome and finial.
  Roof_Cabana         3 m cabana: ivory posts, coral pyramid canopy with a teal valance, two cushions.
  Roof_LatticeMast    vermilion lattice radio tower, 32 m: gallery, ring antennas, a red beacon.
  Roof_Planter        6 x 1.4 m limestone planter: shrub masses and coral flowers.
  Roof_LapPool        16 x 4.4 m lap pool for a 14 m deep roof strip (DecoKitRoofline): loungers along its +Y side
                      between coral and ivory parasols; 17 x 9 m in all, the pivot on the pool's centre.
  Roof_Tennis         26 x 12.6 m rooftop court: seafoam surround, a teal court with ivory lines, chrome posts and a
                      net, end fences of chrome posts and rails.
The helpers (pool, lounger, parasol, planter) also dress the towers' setback terraces (highrise.py).
"""

import math

from geo import dome, ring, rrect
from kit import asset
from lowrise import arc_pts
from mats import pal, register
from mega_parts import truss
from plaza_parts import lift
from structures import sec, tube

register("DK_Pool_Water", "enamel", "#3CC3C4")  # a rooftop pool seen from above: bright, glossy turquoise
register("DK_Foliage", "stone", "#5B8449")      # planter shrubs as rounded masses, legible at 2.5D distance


def ball(s, c, r, mat, seg=12, n=6, squash=1.0):
    with s.at(c, scale=(1.0, 1.0, squash)):
        s.lathe([(r * math.sin(math.pi * k / n), -r * math.cos(math.pi * k / n)) for k in range(n + 1)], mat, seg=seg)


def pool(s, L, W, cx=0.0, cy=0.0, z=0.0):
    """Raised basin L x W: limestone coping ring (0.42 m), teal-tiled inner rim, water 12 cm below the coping."""
    with s.at((cx, cy, z)):
        tube(s, lift(rrect(L + 0.8, W + 0.8, 0.8)), sec(0.0, 0.4, 0.0, 0.42, 0.05), pal.Limestone_Tex, closed=True)
        tube(s, lift(rrect(L, W, 0.4)), sec(0.0, 0.14, 0.0, 0.36), pal.Tile_Teal, closed=True)
        s.extrude(rrect(L - 0.2, W - 0.2, 0.3), 0.0, 0.3, pal.Pool_Water)


def lounger(s, x, y, z=0.0, yaw=0.0):
    """Sun lounger 0.7 x 1.9 m, head toward +Y: ivory frame, coral cushion and raised back."""
    with s.at((x, y, z), (0, 0, yaw)):
        s.box((-0.35, -0.95, 0.0), (0.35, 0.95, 0.3), pal.Enamel_Ivory)
        s.box((-0.31, -0.9, 0.3), (0.31, 0.45, 0.38), pal.Enamel_Coral)
        with s.at((0.0, 0.45, 0.32), (-35, 0, 0)):
            s.box((-0.31, 0.0, 0.0), (0.31, 0.7, 0.08), pal.Enamel_Coral)


def parasol(s, x, y, z=0.0, r=1.3, mat=None):
    with s.at((x, y, z)):
        s.lathe([(0.04, 0.0), (0.04, 2.2), (0.0, 2.2)], pal.Chrome, seg=8, cap_bottom=False)
        s.lathe([(r, 2.05), (r * 0.5, 2.4), (0.0, 2.55)], mat or pal.Enamel_Coral, seg=16)
        s.lathe([(r + 0.02, 1.9), (r + 0.02, 2.06)], pal.Enamel_Ivory, seg=16, cap_bottom=False, cap_top=False)


def planter(s, x, y, z=0.0, l=6.0, w=1.4, yaw=0.0):
    """Limestone bed l x w, 0.6 m, soil top, shrub masses along it and coral flower heads between."""
    with s.at((x, y, z), (0, 0, yaw)):
        s.box((-l / 2, -w / 2, 0.0), (l / 2, w / 2, 0.6), pal.Limestone_Tex)
        s.box((-l / 2 + 0.1, -w / 2 + 0.1, 0.6), (l / 2 - 0.1, w / 2 - 0.1, 0.62), pal.Soil)
        k = max(1, round(l / 1.3))
        for i in range(k):
            px = -l / 2 + (i + 0.5) * l / k
            ball(s, (px, 0.0, 0.85), 0.42 + 0.08 * (i % 2), pal.Foliage, squash=0.8)
            if i + 1 < k:
                ball(s, (px + l / k / 2, (0.25 if i % 2 else -0.25), 0.8), 0.16, pal.Enamel_Coral, seg=8, n=4)


@asset("Roof_Pool", "Rooftop", bevel=0.0)
def roof_pool(s):
    pool(s, 14.0, 7.0)
    for side in (-1, 1):
        for x in (-5.0, -2.5, 0.0, 2.5, 5.0):
            lounger(s, x, side * 5.2, yaw=0 if side > 0 else 180)
    parasol(s, -3.75, 5.9)
    parasol(s, 3.75, -5.9, mat=pal.Enamel_Ivory)
    for y in (-1.1, -0.5):  # ladder rails over the coping at the deep end
        s.sweep([(6.5, y, 0.1), (6.5, y, 1.0), (7.0, y, 1.1), (7.25, y, 0.42)], 0.03, pal.Chrome, seg=6)


@asset("Roof_Conservatory", "Rooftop", bevel=0.0)
def roof_conservatory(s):
    L, r = 12.0, 3.4
    s.extrude(rrect(L + 0.6, 2 * r + 1.4, 0.3), 0.0, 0.7, pal.Limestone_Tex)
    s.extrude(rrect(L + 0.8, 2 * r + 1.6, 0.35), 0.55, 0.7, pal.Enamel_Teal)
    with s.at((-L / 2, 0.0, 0.7), (0, 90, 0)):  # the vault: half-disc section along X
        s.extrude(arc_pts(r, 12), 0.0, L, pal.Glass)
        k = 8
        for i in range(k + 1):
            u = L * i / k
            s.extrude(arc_pts(r + 0.04, 12), max(u - 0.05, 0.0), min(u + 0.05, L), pal.Chrome)
    for x in (-L / 2 - 0.02, L / 2 - 0.08):  # glazed gable ends
        with s.at((x, 0.0, 0.7), (0, 90, 0)):
            s.extrude(arc_pts(r, 12), 0.0, 0.1, pal.Glass)
    z0 = 0.7 + r - 0.3
    s.lathe(dome(2.2, z0, 2.5, n=8), pal.Glass, seg=24)  # the crossing dome, four chrome ribs
    for i in range(4):
        a = math.radians(45 + 90 * i)
        s.sweep([(2.25 * math.cos(a) * rr, 2.25 * math.sin(a) * rr, z) for rr, z in
                 [(x / 2.2, z) for x, z in dome(2.2, z0 + 0.03, 2.5, n=6)]], 0.05, pal.Chrome, seg=6)
    ball(s, (0.0, 0.0, 0.7 + r + 2.3), 0.3, pal.Gold)
    for x, y, rr in ((-4.0, -1.2, 1.0), (-1.5, 1.4, 1.3), (1.8, -1.0, 1.1), (4.2, 1.1, 0.9), (0.2, 0.2, 0.8)):
        ball(s, (x, y, 0.7 + rr * 0.8), rr, pal.Foliage, squash=0.9)


@asset("Roof_Bandstand", "Rooftop", bevel=0.0)
def roof_bandstand(s):
    with s.at((0, 0, 0), (0, 0, 22.5)):  # octagon flats square to the axes
        s.lathe([(4.2, 0.0), (4.2, 0.3), (3.9, 0.3), (3.9, 0.6), (0.0, 0.6)], pal.Limestone_Tex, seg=8)
        s.lathe(ring(3.25, 3.9, 3.8, 4.3), pal.Enamel_Ivory, seg=8, cap_bottom=False, cap_top=False)
        s.lathe(dome(3.8, 4.3, 2.6, n=8), pal.Gold, seg=8)
        s.lathe(ring(3.3, 3.34, 1.2, 1.3), pal.Chrome, seg=8, cap_bottom=False, cap_top=False)
    for k in range(8):
        a = math.radians(22.5 + 45 * k)  # on the octagon's corners
        with s.at((3.6 * math.cos(a), 3.6 * math.sin(a), 0.6)):
            s.lathe([(0.14, 0.0), (0.1, 0.4), (0.1, 3.2), (0.16, 3.3), (0.0, 3.3)], pal.Enamel_Ivory, seg=8)
    ball(s, (0, 0, 7.1), 0.35, pal.Gold)
    s.lathe([(0.12, 7.3), (0.0, 8.6)], pal.Gold, seg=8)


@asset("Roof_Cabana", "Rooftop", bevel=0.0)
def roof_cabana(s):
    for x in (-1.35, 1.35):
        for y in (-1.35, 1.35):
            s.box((x - 0.06, y - 0.06, 0.0), (x + 0.06, y + 0.06, 2.3), pal.Enamel_Ivory)
    with s.at((0, 0, 0), (0, 0, 45)):
        s.lathe([(2.2, 2.3), (0.0, 3.3)], pal.Enamel_Coral, seg=4)
        s.lathe(ring(2.12, 2.2, 1.95, 2.3), pal.Enamel_Teal, seg=4, cap_bottom=False, cap_top=False)
    s.box((-1.2, -0.9, 0.0), (1.2, 0.9, 0.35), pal.Enamel_Ivory)
    for x in (-0.6, 0.6):
        s.box((x - 0.5, -0.8, 0.35), (x + 0.5, 0.8, 0.5), pal.Enamel_Coral if x < 0 else pal.Enamel_Seafoam)


@asset("Roof_LatticeMast", "Rooftop", bevel=0.0)
def roof_lattice_mast(s):
    for x in (-2.6, 2.6):
        for y in (-2.6, 2.6):
            s.box((x - 0.5, y - 0.5, 0.0), (x + 0.5, y + 0.5, 0.6), pal.Limestone_Tex)
    truss(s, (0, 0, 0.6), (0, 0, 22.0), 2.6, 0.9, 8, pal.Enamel_Red, chord=0.32, lace=0.14)
    s.lathe([(2.1, 15.8), (2.1, 16.2), (0.0, 16.2)], pal.Steel, seg=8)  # gallery
    s.lathe(ring(2.0, 2.06, 16.2, 17.2), pal.Chrome, seg=16, cap_bottom=False, cap_top=False)
    truss(s, (0, 0, 22.0), (0, 0, 29.0), 0.9, 0.4, 4, pal.Enamel_Red, chord=0.2, lace=0.1)
    for z, r in ((24.0, 1.5), (26.0, 1.2), (28.0, 0.9)):
        s.lathe(ring(r, r + 0.08, z, z + 0.1), pal.Chrome, seg=16, cap_bottom=False, cap_top=False)
    s.lathe([(0.08, 29.0), (0.05, 31.6), (0.0, 31.6)], pal.Chrome, seg=6, cap_bottom=False)
    ball(s, (0, 0, 31.9), 0.35, pal.Signal_Red, seg=10)


@asset("Roof_Planter", "Rooftop", bevel=0.0)
def roof_planter(s):
    planter(s, 0.0, 0.0)


@asset("Roof_LapPool", "Rooftop", bevel=0.0)
def roof_lap_pool(s):
    pool(s, 16.0, 4.4)
    for k in range(6):
        lounger(s, -6.25 + 2.5 * k, 3.95, yaw=0)
    for k, x in enumerate((-7.5, 0.0, 7.5)):
        parasol(s, x, 4.1, r=1.2, mat=pal.Enamel_Coral if k % 2 == 0 else pal.Enamel_Ivory)


@asset("Roof_Tennis", "Rooftop", bevel=0.0)
def roof_tennis(s):
    L, W, cl, cw = 26.0, 12.6, 23.77, 10.97
    s.box((-L / 2, -W / 2, 0.0), (L / 2, W / 2, 0.06), pal.Enamel_Seafoam)
    s.box((-cl / 2, -cw / 2, 0.06), (cl / 2, cw / 2, 0.08), pal.Enamel_Teal)
    t, sw = 0.05, 8.23 / 2
    for x0, y0, x1, y1 in ((-cl / 2, -cw / 2, cl / 2, -cw / 2 + t), (-cl / 2, cw / 2 - t, cl / 2, cw / 2),
                           (-cl / 2, -sw, cl / 2, -sw + t), (-cl / 2, sw - t, cl / 2, sw),
                           (-cl / 2, -cw / 2, -cl / 2 + t, cw / 2), (cl / 2 - t, -cw / 2, cl / 2, cw / 2),
                           (-6.4 - t / 2, -sw, -6.4 + t / 2, sw), (6.4 - t / 2, -sw, 6.4 + t / 2, sw),
                           (-6.4, -t / 2, 6.4, t / 2)):
        s.box((x0, y0, 0.08), (x1, y1, 0.085), pal.Enamel_Ivory)
    for y in (-cw / 2 - 0.5, cw / 2 + 0.5):  # net posts, net, tape
        with s.at((0.0, y, 0.0)):
            s.lathe([(0.05, 0.08), (0.05, 1.07), (0.0, 1.07)], pal.Chrome, seg=8, cap_bottom=False)
    s.box((-0.02, -cw / 2 - 0.5, 0.2), (0.02, cw / 2 + 0.5, 0.9), pal.Enamel_Charcoal)
    s.box((-0.03, -cw / 2 - 0.5, 0.9), (0.03, cw / 2 + 0.5, 0.97), pal.Enamel_Ivory)
    for x in (-L / 2 + 0.1, L / 2 - 0.1):  # end fences: posts every 2.1 m, top and mid rails
        for k in range(7):
            y = -W / 2 + 0.1 + (W - 0.2) * k / 6
            with s.at((x, y, 0.0)):
                s.lathe([(0.05, 0.06), (0.05, 3.0), (0.0, 3.0)], pal.Chrome, seg=6, cap_bottom=False)
        for z in (1.5, 2.95):
            s.box((x - 0.04, -W / 2 + 0.1, z), (x + 0.04, W / 2 - 0.1, z + 0.06), pal.Chrome)
