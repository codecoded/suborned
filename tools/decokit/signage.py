"""Deco Kit signage: Googie / Streamline advertising for the space-age city, engineered like 1970s-80s airbrushed SF
hardware (lattice legs, gantries, caged ladders, cable trays, landing beacons). Art: signage_art.py atlas.
  Billboard_Rooftop_A/B/C   12 x 4 m poster on lattice legs, catwalk, goosenecks (roof deck or plaza; pivot = legs)
  Billboard_Pylon_A/B       14 m enamel mast, double-sided lit 5 x 2.5 m sign box, atom / starburst finial (plaza)
  Billboard_Wall_A/B/C      6 x 3 m framed poster, 3 goosenecks (facade: pivot at the wall face, bottom centre)
  BladeSign_Cafe/Hotel/Cinema  6 m neon blade on wall brackets (facade: pivot at the wall face, bottom centre)
  Marquee_Cinema            8 m canopy, chaser bulbs, letter boards, fin (facade: pivot at the wall foot, soffit 4 m)
  AdColumn / SandwichBoard  poster column and A-frame board (ground)
  NeonStar_Wall             2.5 m faceted starburst with neon (facade: pivot at the wall face, bottom centre)
"""

import math

import glyphs
import mats
from geo import circle, dome, fillet, rrect
from kit import asset
from mats import pal
from mega_parts import girder, strut
from signage_art import ATLAS
from signage_parts import (BACK, FRONT, SIDE_L, SIDE_R, ad, art, atom, beacon, board, bulb, bulbs, framed, gooseneck,
                           ladder, neon_word, poster, rail, row, scallops, starburst, tray)
from signals import ball, band, bezel, puck, slab
from structures import along, ccw, sec, superell, tube

for _n, _k, _h, *_e in (("DK_Billboard_Ads", "art", "#FFFFFF"), ("DK_Billboard_Ads_Lit", "emit", "#FFFFFF", 1.0),
                        ("DK_Neon_Red", "emit", "#FF4A3A", 5.0), ("DK_Neon_Coral", "emit", "#F58468", 2.4),
                        ("DK_Neon_Cream", "emit", "#FFE6C4", 1.8), ("DK_Neon_Teal", "emit", "#3CF2D2", 4.0),  # brief 022: Neon_Cream is the cafe sign's own lit pair, warm, soft, no pink bloom
                        ("DK_Neon_Gold", "emit", "#FFC94A", 4.0), ("DK_Bulb_Dim", "emit", "#F0B860", 0.5),
                        ("DK_Enamel_Coral", "enamel", "#E07560"),  # = skyline.py's entry
                        ("DK_Steel_Ivory", "enamel", "#E4DFD2")):  # = landmarks.py's painted lattice steel
    mats.register(_n, _k, _h, *_e)
mats.TEXTURES["DK_Billboard_Ads"] = mats.TEXTURES["DK_Billboard_Ads_Lit"] = (ATLAS, 1.0)


# ----------------------------------------------------------------- rooftop billboard

LEGS = (-5.4, -2.7, 0.0, 2.7, 5.4)


def rooftop(s, cell, accent):
    """12 x 4 m face (art plane y = -1.62, centre z = 5.3) on five lattice bents: posts, raking kickers to rear
    feet at y = +1.2, sills, knee braces, X-bracing and a top service gantry; catwalk, caged ladder, cable tray."""
    st, YA, ZC, zd = pal.Steel_Ivory, -1.62, 5.3, 2.3
    for x in LEGS:
        strut(s, (x, -1.2, 0), (x, -1.2, 7.75), 0.2, st)
        strut(s, (x, 1.2, 0.05), (x, -1.15, 6.4), 0.16, st)
        strut(s, (x, -1.2, 0.35), (x, 1.05, 0.35), 0.12, st)
        strut(s, (x, -1.2, 3.0), (x, 0.1, 3.0), 0.1, st)
        strut(s, (x, -1.2, 0.35), (x, 0.1, 3.0), 0.08, st)
        for y in (-1.2, 1.2):
            s.box((x - 0.24, y - 0.24, 0), (x + 0.24, y + 0.24, 0.05), pal.Steel)
    for x0, x1 in zip(LEGS, LEGS[1:]):
        for (za, zb) in ((0.35, 3.0), (3.2, 5.4), (5.4, 7.6)):
            strut(s, (x0, -1.05, za), (x1, -1.05, zb), 0.07, st)
            strut(s, (x1, -1.05, za), (x0, -1.05, zb), 0.07, st)
    for z in (3.2, 5.4):
        strut(s, (-5.5, -1.05, z), (5.5, -1.05, z), 0.1, st)
    girder(s, (-6.0, -0.75, 7.75), (6.0, -0.75, 7.75), 0.6, 0.5, 8, st, chord=0.09, lace=0.05)
    with s.at((0, YA, ZC), FRONT):
        slab(s, 12.7, 4.7, 0.18, -0.32, -0.005, pal.Enamel_Cream, e=0.03, n=4, m=2)
        wf, hf = framed(s, 12.0, 4.0, ad(cell), pal.Billboard_Ads, lip=0.18, depth=0.14)
        ya = -hf / 2 - 0.28
        slab(s, wf, 0.56, 0.08, -0.3, 0.08, accent, e=0.02, n=3, m=1, cy=ya)
        for k in range(3):
            s.box((-wf * 0.36, ya - 0.137 + 0.12 * k, 0.07), (wf * 0.36, ya - 0.103 + 0.12 * k, 0.095), pal.Chrome)
        W, top = (2.3, 2.3, 3.2, 2.3, 2.3), hf / 2 - 0.04
        s.extrude(scallops(W, (0.55, 0.55, 0.9, 0.55, 0.55), top, 0.14), -0.2, 0.08, pal.Enamel_Ivory)
        s.extrude(scallops(W, (0.69, 0.69, 1.04, 0.69, 0.69), top, 0.14), -0.28, -0.2, accent)
    s.box((-6.3, -2.55, zd - 0.06), (6.3, -1.66, zd), pal.Steel)  # catwalk deck, toe board, bearers, braces
    s.box((-6.3, -2.58, zd - 0.1), (6.3, -2.55, zd + 0.14), accent)
    for x in LEGS:
        strut(s, (x, -1.2, zd - 0.1), (x, -2.52, zd - 0.1), 0.1, st)
        strut(s, (x, -1.2, zd - 1.2), (x, -2.4, zd - 0.12), 0.08, st)
    rail(s, [(-6.25, -1.72, zd), (-6.25, -2.5, zd), (6.25, -2.5, zd)], h=1.0, post=1.6)
    with s.at((6.62, -2.1, 0), (0, 0, 90)):
        ladder(s, 0.03, zd, ext=1.0)
    tray(s, [(0.0, -1.08, 0.4), (0.0, -1.08, 7.7)], (0, 1, 0))
    tray(s, [(-4.8, -0.75, 7.8), (4.8, -0.75, 7.8)], (0, 0, 1))
    s.rbox((-0.36, -1.1, 0.05), (0.36, -0.7, 0.95), 0.04, accent)  # switch cabinet at the tray foot
    s.rbox((-0.08, -0.72, 0.4), (0.08, -0.66, 0.62), 0.02, pal.Chrome)
    for x in (-3.9, -1.6, 1.6, 3.9):
        gooseneck(s, (x, -1.37, 7.46), 0.75, 1.55, 0.35, aim=32, shade=accent)
    beacon(s, (0, -1.52, 8.36 - 0.03))
    for x in (-6.0, 6.0):
        strut(s, (x, -0.75, 8.25), (x, -0.75, 9.0), 0.08, st)
        beacon(s, (x, -0.75, 9.0))


@asset("Billboard_Rooftop_A", "Signage", bevel=0.0)
def billboard_rooftop_a(s):
    rooftop(s, "mars", pal.Enamel_Teal)


@asset("Billboard_Rooftop_B", "Signage", bevel=0.0)
def billboard_rooftop_b(s):
    rooftop(s, "fizz", pal.Enamel_Red)


@asset("Billboard_Rooftop_C", "Signage", bevel=0.0)
def billboard_rooftop_c(s):
    rooftop(s, "skyrail", pal.Enamel_Mustard)


# ----------------------------------------------------------------- pylon

def pylon(s, front, back, top):
    """Plinth, tapered enamel mast with four rocket fins and neon strakes, a double-sided lit sign box (z 7-9.5)
    over a railed service platform reached by a caged ladder, cable tray, beacons, atom or starburst finial."""
    def mast(z):
        f = (z - 0.75) / 10.65
        return 0.55 - 0.29 * f, 0.34 - 0.16 * f
    s.loft([(rrect(2.7, 1.9, 0.32, 5), 0.0), (rrect(2.7, 1.9, 0.32, 5), 0.3), (rrect(2.3, 1.5, 0.26, 5), 0.36),
            (rrect(2.3, 1.5, 0.26, 5), 0.7), (rrect(1.9, 1.2, 0.2, 5), 0.76)], pal.Limestone_Tex)
    s.loft([(rrect(2.74, 1.94, 0.33, 5), 0.24), (rrect(2.74, 1.94, 0.33, 5), 0.31)], pal.Chrome,
           cap_bottom=False, cap_top=False)
    s.loft([(superell(*mast(z), p=2.6, n=24), z) for z in (0.7, 11.4)] + [(superell(0.2, 0.14, p=2.6, n=24), 11.5)],
           pal.Enamel_Ivory)
    band(s, 0.26, 0.34, 11.35, 11.5, pal.Chrome, seg=24)
    s.lathe([(0.2, 11.48), (0.24, 11.6), (0.12, 11.78), (0.06, 11.85), (0, 11.86)], pal.Chrome, seg=16,
            cap_bottom=False)
    for side in (-1, 1):  # neon strakes on the mast's broad faces
        for z0, z1 in ((1.2, 6.9), (9.6, 11.2)):
            s.sweep([(0, side * (mast(z0)[1] + 0.03), z0), (0, side * (mast(z1)[1] + 0.03), z1)], 0.03, pal.Neon_Teal,
                    seg=6)
    blade = [(0.3, 4.4), (0.3, 0.76), (0.82, 0.76), (1.12, 1.25), (1.12, 1.7), (0.48, 3.9)]
    tip = [(0.95, 0.97), (1.12, 1.25), (1.12, 1.7), (0.98, 1.95)]
    for az in (45, 135, 225, 315):
        with s.at((0, 0, 0), (0, 0, az)), s.at((0, 0, 0), FRONT):
            s.extrude(blade, -0.07, 0.07, pal.Enamel_Red)
            s.extrude(tip, -0.085, 0.085, pal.Chrome)
    for z in (4.5, 6.95, 9.55):
        band(s, mast(z)[0] - 0.02, mast(z)[0] + 0.05, z - 0.06, z + 0.06, pal.Chrome, seg=24)
    with s.at((0, 0, 8.25), FRONT):
        slab(s, 5.0, 2.5, 0.4, -0.4, 0.4, pal.Enamel_Ivory, e=0.05)
    for y, rot, cell in ((-0.4, FRONT, front), (0.4, BACK, back)):
        with s.at((0, y, 8.25), rot):
            bezel(s, 5.0, 2.5, 0.4, 0.08, -0.02, 0.05, pal.Chrome, n=5)
            for cy in (0.95, -0.95):
                slab(s, 4.7, 0.38, 0.12, -0.02, 0.03, pal.Enamel_Mustard, e=0.01, n=3, m=1, cy=cy)
            with s.at((0, 0, 0.03)):
                bulbs(s, row(-2.1, 2.1, 0.95, 0.3) + row(-2.1, 2.1, -0.95, 0.3), r=0.055)
            framed(s, 4.4, 4.4 / 3, ad(cell), pal.Billboard_Ads_Lit, trim=pal.Enamel_Teal, lip=0.06, depth=0.07, r=0.04)
    st, zp = pal.Steel_Ivory, 6.1
    s.box((-2.6, -0.85, zp - 0.07), (2.6, 0.85, zp), pal.Steel)
    for y in (-0.85, 0.85):
        s.box((-2.6, y - 0.02, zp - 0.12), (2.6, y + 0.02, zp + 0.1), pal.Enamel_Coral)
        rail(s, [(-2.55, y, zp), (2.55, y, zp)], h=0.85, post=1.7)
        for x in (-2.3, 2.3):
            strut(s, (0, y * 0.35, 4.9), (x, y * 0.95, zp - 0.08), 0.1, st)
        strut(s, (-2.5, y * 0.9, zp - 0.12), (2.5, y * 0.9, zp - 0.12), 0.1, st)
    with s.at((0.64, 0, 0.76), (0, 0, 90)):
        ladder(s, 0.0, zp - 0.76, ext=0.9)
    tray(s, [(-0.62, 0, 0.8), (-0.62, 0, zp - 0.1)], (-1, 0, 0))
    s.rbox((-0.95, -0.3, 0.76), (-0.6, 0.3, 1.5), 0.04, pal.Enamel_Teal)
    for x in (-2.3, 2.3):
        beacon(s, (x, 0, 9.5))
    s.lathe([(0.06, 11.8), (0.03, 13.85), (0, 13.9)], pal.Chrome, seg=8)
    beacon(s, (0, 0, 13.88), r=0.09)
    if top == "atom":
        atom(s, (0, 0, 12.75), 0.95)
    else:
        for rot in (FRONT, SIDE_R):
            with s.at((0, 0, 12.75), rot):
                starburst(s, 12, 0.18, 1.15, (pal.Enamel_Mustard, pal.Chrome), alt=0.55, ridge=0.07, tip=0.06)
        ball(s, 0.24, pal.Enamel_Red, seg=16, n=8, z=12.75)


@asset("Billboard_Pylon_A", "Signage", bevel=0.0)
def billboard_pylon_a(s):
    pylon(s, "planetarium", "robot", "atom")


@asset("Billboard_Pylon_B", "Signage", bevel=0.0)
def billboard_pylon_b(s):
    pylon(s, "cruise", "aircar", "star")


# ----------------------------------------------------------------- wall billboard

def wall_board(s, cell, accent):
    """6 x 3 m: cream backboard flush on the wall (y = 0), art 5.6 x 1.87 at y = -0.3, apron, scalloped crest, three
    goosenecks and a conduit down to a junction box."""
    zc = 0.44 + 1.113
    with s.at((0, -0.3, zc), FRONT):
        slab(s, 6.0, 2.33, 0.12, -0.3, -0.005, pal.Enamel_Cream, e=0.02, n=4, m=2)
        wf, hf = framed(s, 5.6, 5.6 / 3, ad(cell), pal.Billboard_Ads, lip=0.15, depth=0.12, r=0.08)
        ya = -hf / 2 - 0.21
        slab(s, wf, 0.42, 0.06, -0.3, 0.06, accent, e=0.015, n=3, m=1, cy=ya)
        for k in range(2):
            s.box((-wf * 0.35, ya - 0.065 + 0.1 * k, 0.05), (wf * 0.35, ya - 0.035 + 0.1 * k, 0.072), pal.Chrome)
        W, top = (1.9, 2.1, 1.9), hf / 2 - 0.04
        s.extrude(scallops(W, (0.22, 0.34, 0.22), top, 0.1), -0.2, 0.06, pal.Enamel_Ivory)
        s.extrude(scallops(W, (0.3, 0.42, 0.3), top, 0.1), -0.28, -0.2, accent)
    for x in (-2.0, 0.0, 2.0):
        gooseneck(s, (x, -0.13, 2.6), 0.62, 1.0, 0.3, aim=32, shade=accent, R=0.2)
    s.sweep(fillet([(-2.0, -0.08, 2.6), (-3.08, -0.08, 2.6), (-3.08, -0.08, 0.55)], 0.12, n=4, closed=False), 0.03,
            pal.Steel, seg=6)
    s.rbox((-3.24, -0.16, 0.12), (-2.92, 0.0, 0.56), 0.03, pal.Enamel_Charcoal)


@asset("Billboard_Wall_A", "Signage", bevel=0.0)
def billboard_wall_a(s):
    wall_board(s, "cinema", pal.Enamel_Red)


@asset("Billboard_Wall_B", "Signage", bevel=0.0)
def billboard_wall_b(s):
    wall_board(s, "robot", pal.Enamel_Teal)


@asset("Billboard_Wall_C", "Signage", bevel=0.0)
def billboard_wall_c(s):
    wall_board(s, "aircar", pal.Enamel_Mustard)


# ----------------------------------------------------------------- blade sign

def blade(s, word, body, letters, neon, border, crest=True):
    """Streamline blade 1.0 m out x 0.35 m thick x 6.2 m on three chrome arms, stacked channel letters with neon on
    both faces, a neon border, chrome edge bead and a starburst on the outer top corner."""
    Y0, Y1, T = -0.28, -1.28, 0.175
    out = fillet([(Y0, 0.4), (Y1, 0.0), (Y1, 5.6), (Y0, 6.2)], 0.22, n=4)
    with s.at((0, 0, 0), SIDE_R):
        s.extrude(ccw(out), -T, T, body)
    s.sweep([(0, y, z) for y, z in out + out[:1]], 0.045, pal.Chrome, seg=6, caps=False)
    rim = fillet([(Y0 - 0.1, 0.56), (Y1 + 0.1, 0.22), (Y1 + 0.1, 5.46), (Y0 - 0.1, 5.96)], 0.14, n=3)
    for sx, rot in ((1, SIDE_R), (-1, SIDE_L)):
        s.sweep([(sx * (T + 0.02), y, z) for y, z in rim + rim[:1]], 0.022, border, seg=5, caps=False)
        with s.at((sx * T, (Y0 + Y1) / 2, 2.95), rot):
            neon_word(s, word, min(4.5 / glyphs.layout(word, stack=True)[1][1], 0.95), letters, neon, stack=True)
    for z in (0.75, 3.1, 5.45):
        s.rbox((-0.05, Y0 - 0.02, z - 0.05), (0.05, -0.02, z + 0.05), 0.02, pal.Chrome)
        s.rbox((-0.17, -0.04, z - 0.17), (0.17, 0.0, z + 0.17), 0.02, pal.Chrome)
    strut(s, (0, -0.03, 6.5), (0, Y0 - 0.02, 5.47), 0.06, pal.Chrome)
    s.rbox((-0.1, -0.04, 6.4), (0.1, 0.0, 6.62), 0.02, pal.Chrome)
    with s.at((0, Y1 + 0.12, 5.72), SIDE_R):
        if crest:
            starburst(s, 8, 0.07, 0.5, (pal.Gold, pal.Chrome), alt=0.55, ridge=0.04, tip=0.04)
        ball(s, 0.075 if crest else 0.09, pal.Chrome, seg=12, n=6)


@asset("BladeSign_Cafe", "Signage", bevel=0.0)
def blade_sign_cafe(s):
    """Brief 022 (Glyph: read as a HUD waypoint at distance): cream blade, coral channel letters with soft coral neon, warm cream
    neon border, chrome bead and a plain chrome ball finial (no starburst, no magenta). Static: nothing animates on a kit sign."""
    blade(s, "CAFE", pal.Enamel_Cream, pal.Enamel_Coral, pal.Neon_Coral, pal.Neon_Cream, crest=False)


@asset("BladeSign_Hotel", "Signage", bevel=0.0)
def blade_sign_hotel(s):
    blade(s, "HOTEL", pal.Enamel_Red, pal.Enamel_Ivory, pal.Neon_Teal, pal.Neon_Teal)


@asset("BladeSign_Cinema", "Signage", bevel=0.0)
def blade_sign_cinema(s):
    blade(s, "CINEMA", pal.Enamel_Ivory, pal.Enamel_Red, pal.Neon_Gold, pal.Neon_Red)


# ----------------------------------------------------------------- cinema marquee

@asset("Marquee_Cinema", "Signage", bevel=0.0)
def marquee_cinema(s):
    """8 x 2.6 m streamline canopy (soffit at z = 4.0, clearing the 3.85 m shop canopy line): red fascia with backlit
    letter boards front and sides, chaser-bulb rows, soffit downlights, chrome beads, tie rods to the wall and a
    swept ivory fin with neon and a starburst."""
    Z0, hw, D = 4.0, 4.0, 2.6
    path = fillet([(-hw, 0.0), (-hw, -D), (hw, -D), (hw, 0.0)], 0.9, n=8, closed=False)
    s.extrude(ccw(path), Z0, Z0 + 0.22, pal.Enamel_Ivory)
    tube(s, [(x, y, 0) for x, y in path], sec(-0.07, 0.07, Z0 + 0.2, Z0 + 1.75), pal.Enamel_Red)
    s.extrude(ccw(path), Z0 + 1.72, Z0 + 1.88, pal.Enamel_Ivory)
    for z in (Z0 + 0.2, Z0 + 1.88):
        tube(s, [(x, y, 0) for x, y in path], circle(0.035, 6, -0.08, z), pal.Chrome)
    crown = fillet([(-hw + 0.45, 0.0), (-hw + 0.45, -D + 0.45), (hw - 0.45, -D + 0.45), (hw - 0.45, 0.0)], 0.55, n=6,
                   closed=False)
    s.extrude(ccw(crown), Z0 + 1.86, Z0 + 2.08, pal.Enamel_Teal)
    tube(s, [(x, y, 0) for x, y in crown], circle(0.03, 6, -0.02, Z0 + 2.08), pal.Chrome)
    zb = Z0 + 0.975
    with s.at((0, -D - 0.07, zb), FRONT):
        art(s, 6.2, 6.2 * 112 / 684, board("front"), pal.Billboard_Ads_Lit)
        bezel(s, 6.3, 6.2 * 112 / 684 + 0.1, 0.04, 0.06, -0.01, 0.04, pal.Chrome, n=2)
    for sx, rot in ((1, SIDE_R), (-1, SIDE_L)):
        with s.at((sx * (hw + 0.07), -0.86, zb), rot):
            art(s, 1.6, 0.8, board("side"), pal.Billboard_Ads_Lit)
            bezel(s, 1.7, 0.9, 0.04, 0.06, -0.01, 0.04, pal.Chrome, n=2)
    for z in (Z0 + 0.34, Z0 + 1.61):
        for i, (p, t) in enumerate(along(path, 0.3, margin=0.12)):
            n = (t.y, -t.x)
            with s.at((p.x + n[0] * 0.07, p.y + n[1] * 0.07, z), (90, 0, math.degrees(math.atan2(n[0], -n[1])))):
                bulb(s, 0.05, i % 3 == 0)
    for x in (-3.0, -2.0, -1.0, 0.0, 1.0, 2.0, 3.0):
        for y in (-0.7, -1.9):
            with s.at((x, y, Z0)):
                s.lathe([(0, -0.02), (0.11, -0.012), (0.11, 0.0)], pal.Lamp, seg=8, cap_top=False)
    for x in (-3.3, 3.3):  # tie rods with turnbuckles to wall plates
        strut(s, (x, -2.3, Z0 + 1.88), (x, -0.05, Z0 + 4.2), 0.05, pal.Chrome)
        s.rbox((x - 0.18, -0.05, Z0 + 4.0), (x + 0.18, 0.0, Z0 + 4.45), 0.02, pal.Chrome)
        with s.at((x, -1.18, Z0 + 3.03)):
            s.rbox((-0.04, -0.12, -0.12), (0.04, 0.12, 0.12), 0.02, pal.Enamel_Charcoal)
    fin = fillet([(-2.35, Z0 + 1.88), (-0.3, Z0 + 1.88), (-0.5, Z0 + 3.3), (-0.8, Z0 + 5.35), (-1.1, Z0 + 5.25),
                  (-1.65, Z0 + 3.3)], 0.2, n=4)
    inlay = fillet([(-1.75, Z0 + 2.1), (-0.65, Z0 + 2.1), (-0.72, Z0 + 3.3), (-0.9, Z0 + 4.6), (-1.35, Z0 + 3.3)],
                   0.15, n=3)
    with s.at((0, 0, 0), SIDE_R):
        s.extrude(ccw(fin), -0.09, 0.09, pal.Enamel_Ivory)
        s.extrude(ccw(inlay), -0.11, 0.11, pal.Enamel_Teal)
    edge = [(-2.2, Z0 + 2.0), (-1.62, Z0 + 3.3), (-1.08, Z0 + 5.1)]
    for sx in (-1, 1):
        s.sweep([(sx * 0.12, y, z) for y, z in edge], 0.028, pal.Neon_Red, seg=6)
    with s.at((0, -0.95, Z0 + 5.5), SIDE_R):
        starburst(s, 12, 0.12, 0.85, (pal.Gold, pal.Chrome), alt=0.55, ridge=0.05, tip=0.05)
        ball(s, 0.12, pal.Enamel_Red, seg=14, n=7)


# ----------------------------------------------------------------- ground props

def flutes(r, d, n=20, k=6):
    """Fluted drum section (CCW): n concave flutes of depth d between sharp arrises."""
    rad = [r - d * abs(math.sin(math.pi * i / k)) ** 0.7 for i in range(n * k)]
    a = [2 * math.pi * i / (n * k) for i in range(n * k)]
    return [(q * math.cos(t), q * math.sin(t)) for q, t in zip(rad, a)]


@asset("AdColumn", "Signage", bevel=0.0)
def ad_column(s):
    """Morris column, 3.3 m: chrome foot, teal plinth drum, fluted ivory drum, four wrapped 2:3 posters between
    chrome battens and rings, fluted frieze, teal cornice, red dome, gold-ball finial."""
    s.lathe(puck(0.8, 0, 0.08, 0.025), pal.Chrome, seg=40)
    s.lathe([(0.74, 0.07), (0.74, 0.36), (0.7, 0.42), (0.64, 0.44), (0, 0.44)], pal.Enamel_Teal, seg=40,
            cap_bottom=False)
    band(s, 0.735, 0.76, 0.3, 0.34, pal.Chrome, seg=40)
    s.loft([(flutes(0.63, 0.025), 0.43), (flutes(0.63, 0.025), 0.7)], pal.Enamel_Ivory, cap_bottom=False)
    s.lathe([(0.6, 0.68), (0.6, 2.2)], pal.Enamel_Ivory, seg=40, cap_bottom=False, cap_top=False)
    R, z0, z1 = 0.606, 0.73, 0.73 + 0.5 * math.pi * 0.606 * 1.5
    for j, k in enumerate(("expo", "saturn", "rocketrace", "vesper")):
        (u0, v0), (u1, _), (_, v1), _ = poster(k)
        a0, m = math.radians(-135 + 90 * j), 6
        for i in range(m):
            aa, ab = a0 + math.pi / 2 * i / m, a0 + math.pi / 2 * (i + 1) / m
            ua, ub = u0 + (u1 - u0) * i / m, u0 + (u1 - u0) * (i + 1) / m
            s.decal([(R * math.cos(aa), R * math.sin(aa), z0), (R * math.cos(ab), R * math.sin(ab), z0),
                     (R * math.cos(ab), R * math.sin(ab), z1), (R * math.cos(aa), R * math.sin(aa), z1)],
                    pal.Billboard_Ads, ((ua, v0), (ub, v0), (ub, v1), (ua, v1)), lift=0.0)
        a = a0
        strut(s, (0.618 * math.cos(a), 0.618 * math.sin(a), z0 - 0.02), (0.618 * math.cos(a), 0.618 * math.sin(a),
                                                                         z1 + 0.02), 0.03, pal.Chrome)
    for z in (z0 - 0.025, z1 + 0.025):
        band(s, 0.6, 0.628, z - 0.02, z + 0.02, pal.Chrome, seg=40)
    s.loft([(flutes(0.63, 0.02), z1 + 0.04), (flutes(0.63, 0.02), 2.44)], pal.Enamel_Ivory, cap_bottom=False)
    s.lathe([(0.6, 2.43), (0.7, 2.46), (0.78, 2.5), (0.8, 2.55), (0.78, 2.6), (0.7, 2.62)], pal.Enamel_Teal, seg=40,
            cap_bottom=True, cap_top=False)
    band(s, 0.795, 0.815, 2.52, 2.58, pal.Chrome, seg=40)
    s.lathe([(0.7, 2.6)] + dome(0.7, 2.62, 0.4, n=8)[1:], pal.Enamel_Red, seg=40, cap_bottom=False)
    band(s, 0.69, 0.715, 2.61, 2.66, pal.Gold, seg=40)
    s.lathe([(0.1, 2.98), (0.1, 3.02), (0.05, 3.06), (0.04, 3.12)], pal.Chrome, seg=16, cap_bottom=False)
    ball(s, 0.075, pal.Gold, seg=16, n=8, z=3.18)
    s.lathe([(0.025, 3.23), (0.004, 3.34)], pal.Chrome, seg=8, cap_bottom=False)


@asset("SandwichBoard", "Signage", bevel=0.0)
def sandwich_board(s):
    """A-frame pavement board, 1.05 m: red enamel panels in chrome frames, a poster each side, ivory header, mustard
    kick panel, chrome hinge, stays and carry handle, rubber feet."""
    th, L = math.degrees(math.atan2(0.3, 1.05)), math.hypot(0.3, 1.05)
    for side, rot, k in ((-1, (90 - th, 0, 0), "expo"), (1, (90 - th, 0, 180), "rocketrace")):
        with s.at((0, side * 0.15, 0.537), rot):
            slab(s, 0.62, L, 0.05, -0.035, 0.0, pal.Enamel_Red, e=0.008, n=3, m=1)
            bezel(s, 0.62, L, 0.05, 0.03, -0.01, 0.012, pal.Chrome, n=3)
            art(s, 0.5, 0.75, poster(k), pal.Billboard_Ads, cy=0.05)
            bezel(s, 0.52, 0.77, 0.01, 0.018, -0.005, 0.008, pal.Chrome, n=1, cy=0.05)
            slab(s, 0.5, 0.085, 0.03, -0.005, 0.012, pal.Enamel_Ivory, e=0.004, n=2, m=1, cy=L / 2 - 0.085)
            slab(s, 0.5, 0.13, 0.03, -0.005, 0.012, pal.Enamel_Mustard, e=0.004, n=2, m=1, cy=-L / 2 + 0.11)
            for x in (-0.27, 0.27):
                s.rbox((x - 0.035, -L / 2 + 0.004, -0.04), (x + 0.035, -L / 2 + 0.05, 0.005), 0.01, pal.Rubber)
    s.sweep([(-0.31, 0, 1.057), (0.31, 0, 1.057)], 0.022, pal.Chrome, seg=10)
    s.sweep(fillet([(-0.12, 0, 1.06), (-0.12, 0, 1.16), (0.12, 0, 1.16), (0.12, 0, 1.06)], 0.05, n=4, closed=False),
            0.014, pal.Chrome, seg=8)
    for x in (-0.25, 0.25):
        s.sweep([(x, -0.17, 0.4), (x, 0.17, 0.4)], 0.008, pal.Chrome, seg=6)


@asset("NeonStar_Wall", "Signage", bevel=0.0)
def neon_star_wall(s):
    """Googie starburst, 2.5 m: 16 faceted coral / cream spikes with gold tips and cream neon on the long ones, a
    chrome-rimmed ivory hub with a coral neon ring and coral boss (brief 022: was mustard/teal/red), on two chrome stand-offs (wall at y = 0)."""
    zc, yf = 1.32, -0.3
    for z in (zc - 0.3, zc + 0.3):
        s.rbox((-0.05, yf + 0.05, z - 0.05), (0.05, -0.02, z + 0.05), 0.015, pal.Chrome)
        s.rbox((-0.14, -0.03, z - 0.14), (0.14, 0.0, z + 0.14), 0.015, pal.Chrome)
    with s.at((0, yf, zc), FRONT):
        s.lathe(puck(0.34, -0.08, 0.08, 0.035), pal.Enamel_Ivory, seg=32)
        s.lathe([(0.33, 0.03), (0.37, 0.03), (0.38, 0.06), (0.35, 0.1), (0.33, 0.09), (0.33, 0.03)], pal.Chrome,
                seg=32, cap_bottom=False, cap_top=False)
        s.sweep([(0.25 * math.cos(a), 0.25 * math.sin(a), 0.1) for a in (2 * math.pi * k / 32 for k in range(33))],
                0.022, pal.Neon_Coral, seg=6, caps=False)
        s.lathe([(0, 0.08)] + dome(0.14, 0.08, 0.08, n=5), pal.Enamel_Coral, seg=20)
        ball(s, 0.05, pal.Chrome, seg=12, n=6, z=0.19)
        starburst(s, 16, 0.3, 1.25, (pal.Enamel_Coral, pal.Enamel_Cream), alt=0.58, ridge=0.07, tip=0.055)
        for k in range(0, 16, 2):
            a = math.pi / 2 + 2 * math.pi * k / 16
            s.sweep([(r * math.cos(a), r * math.sin(a), 0.07 * (1.25 - r) / 0.89 + 0.02) for r in (0.52, 1.0)], 0.018,
                    pal.Neon_Cream, seg=5)
