"""Alley props (registered via alleys.py): the service side's clutter, streamline and well kept. Pivot = footprint
centre on the ground, front -Y (BackDoorLamp: the wall face at its backplate centre, lamp out along -Y).
  Dumpster      tapered teal tub, mustard lids on chrome hinges (one ajar), lifting trunnions, swivel castors
  TrashCans     three ribbed enamel cans with domed chrome lids, one lid knocked askew
  CrateStack    riveted steel-framed cargo crates (stencilled) and a hooped coral barrel
  SteamVent     cast floor grate over a dark pit beside a low cowled vent stack fed by a flanged elbow
  BackDoorLamp  caged jar lamp on a gooseneck wall bracket
  Pallet        timber deck pallet, two lots of boards
  MilkCrates    three open slatted crates, one carrying milk bottles
  BikeLean      balloon-tyre cruiser bicycle leant against a wall behind it
"""

import math

from alleys_parts import ALONG_X, caged_lamp
from geo import dome, fillet, ring, rrect
from kit import asset
from mats import DECAL, cell, pal, register
from mega_parts import strut
from structures import tube

register("DK_Timber", "stone", "#B48C5E")
register("DK_Timber_B", "stone", "#A07A50")  # second lot of boards


def caster(s, x, y, z):
    """Swivel caster whose plate sits at z: chrome plate, stem, fork, rubber wheel on a chrome hub."""
    with s.at((x, y, 0)):
        s.lathe([(0, z - 0.02), (0.055, z - 0.02), (0.055, z), (0, z)], pal.Chrome, seg=10)
        for sy in (-1, 1):
            s.box((-0.03, sy * 0.03 - 0.005, 0.06), (0.04, sy * 0.03 + 0.005, z - 0.02), pal.Chrome)
        with s.at((0.015, -0.025, 0.065), (-90, 0, 0)):
            s.lathe([(0.03, 0.0), (0.055, 0.0), (0.065, 0.012), (0.065, 0.038), (0.055, 0.05), (0.03, 0.05)],
                    pal.Rubber, seg=10, cap_bottom=False, cap_top=False)
            s.lathe([(0.0, -0.004), (0.032, -0.004), (0.032, 0.054), (0.0, 0.054)], pal.Chrome, seg=8)


# ----------------------------------------------------------------- dumpster

@asset("Dumpster", "Alleys", bevel=0.0)
def dumpster(s):
    W, D, H = 1.8, 1.05, 1.12

    def rr(w, d, z, r=0.2):
        return rrect(w, d, r, n=4), z
    s.loft([rr(W - 0.16, D - 0.14, 0.24), rr(W - 0.04, D - 0.04, 0.9), rr(W, D, H), rr(W - 0.06, D - 0.06, H),
            rr(W - 0.22, D - 0.2, 0.32)], pal.Enamel_Teal)
    rim = rrect(W + 0.02, D + 0.02, 0.21, n=4)
    s.sweep([(x, y, H) for x, y in rim + rim[:1]], 0.028, pal.Chrome, seg=6, caps=False)
    s.loft([rr(W - 0.1, D - 0.08, 0.2), rr(W - 0.1, D - 0.08, 0.3)], pal.Chrome, cap_bottom=False, cap_top=False)
    for k in range(3):  # speed lines
        z = 0.97 + 0.045 * k
        s.sweep([(-0.72, -D / 2 + 0.005, z), (0.72, -D / 2 + 0.005, z)], 0.011, pal.Chrome, seg=6)
    s.decal([(-0.15, -0.482, 0.45), (0.15, -0.482, 0.45), (0.15, -0.502, 0.75), (-0.15, -0.502, 0.75)],
            pal.Decal_Transit, cell(DECAL["city_crest"]))
    for sx, tilt in ((-1, 0), (1, -16)):  # lids hinged at the back edge; the right one ajar
        with s.at((0, D / 2 + 0.02, H + 0.02), (tilt, 0, 0)):
            cx = sx * W / 4
            s.loft([(rrect(W / 2 - 0.03, D + 0.06, 0.12, n=4, cx=cx, cy=-D / 2 - 0.02), 0.0),
                    (rrect(W / 2 - 0.03, D + 0.06, 0.12, n=4, cx=cx, cy=-D / 2 - 0.02), 0.04),
                    (rrect(W / 2 - 0.1, D - 0.04, 0.1, n=4, cx=cx, cy=-D / 2 - 0.02), 0.09)], pal.Enamel_Mustard)
            s.sweep(fillet([(cx - 0.18, -D - 0.05, 0.03), (cx - 0.18, -D - 0.1, 0.03), (cx + 0.18, -D - 0.1, 0.03),
                            (cx + 0.18, -D - 0.05, 0.03)], 0.03, 3, closed=False), 0.013, pal.Chrome, seg=6)
            for hx in (cx - 0.25, cx + 0.25):
                with s.at((hx - 0.07, 0.0, 0.0), (0, 90, 0)):
                    s.lathe([(0.025, 0.0), (0.025, 0.14)], pal.Chrome, seg=8)
    for sx in (-1, 1):  # lifting trunnions + end push bars
        with s.at((sx * (W / 2 - 0.02), 0, 0.84), (0, sx * 90, 0)):
            s.lathe([(0.08, 0.0), (0.08, 0.05), (0.06, 0.06), (0.06, 0.12), (0.075, 0.13), (0.075, 0.16),
                     (0.0, 0.17)], pal.Chrome, seg=12)
        bar = fillet([(sx * 0.9, -0.3, 0.6), (sx * 0.99, -0.3, 0.6), (sx * 0.99, 0.3, 0.6), (sx * 0.9, 0.3, 0.6)],
                     0.05, 3, closed=False)
        s.sweep(bar, 0.018, pal.Chrome, seg=6)
    for x in (-0.68, 0.68):
        for y in (-0.33, 0.33):
            caster(s, x, y, 0.24)


# ----------------------------------------------------------------- trash cans

def can(s, body, lid=(0.0, 0.0, 0.0, 0.0)):
    """Ribbed enamel can; lid = (dx, dy, tilt deg, dz) knocks the domed chrome lid askew."""
    R, H = 0.25, 0.74
    wall = [(0.0, 0.0), (R - 0.03, 0.0), (R - 0.02, 0.03)]
    for z in (0.2, 0.48):
        wall += [(R - 0.02, z - 0.03), (R + 0.004, z - 0.012), (R + 0.004, z + 0.012), (R - 0.02, z + 0.03)]
    wall += [(R - 0.02, H - 0.04), (R, H - 0.02), (R, H), (R - 0.02, H), (R - 0.03, 0.08), (0.0, 0.08)]
    s.lathe(wall, body, seg=16)
    dx, dy, tilt, dz = lid
    with s.at((dx, dy, H + dz), (tilt, 0, 0)):
        s.lathe([(0.0, -0.01), (R + 0.02, -0.01), (R + 0.03, 0.01), (R + 0.02, 0.035)]
                + dome(R - 0.01, 0.035, 0.09, 4)[1:], pal.Chrome, seg=16)
        s.sweep(fillet([(-0.07, 0, 0.11), (-0.07, 0, 0.16), (0.07, 0, 0.16), (0.07, 0, 0.11)], 0.03, 3,
                       closed=False), 0.012, pal.Chrome, seg=6)
    for sx in (-1, 1):
        s.sweep(fillet([(sx * R, -0.07, 0.6), (sx * (R + 0.05), -0.07, 0.6), (sx * (R + 0.05), 0.07, 0.6),
                        (sx * R, 0.07, 0.6)], 0.02, 2, closed=False), 0.011, pal.Chrome, seg=5)


@asset("TrashCans", "Alleys", bevel=0.0)
def trash_cans(s):
    for (x, y, rz), body, lid in (((-0.3, -0.14, 10), pal.Enamel_Teal, (0, 0, 0, 0)),
                                   ((0.3, -0.17, -25), pal.Enamel_Coral, (0.05, -0.04, 11, 0.03)),
                                   ((0.02, 0.33, 40), pal.Enamel_Mustard, (0, 0, 0, 0))):
        with s.at((x, y, 0), (0, 0, rz)):
            can(s, body, lid)


# ----------------------------------------------------------------- crates

def span(a, b):
    """Box corners from two opposite points in any order."""
    return tuple(map(min, a, b)), tuple(map(max, a, b))


def crate(s, w, d, h, body, label=None):
    """Riveted cargo crate: rounded enamel box, steel corner angles and rim bands, chrome corner caps, a rivet row
    along each front band and a stencil decal."""
    s.rbox((-w / 2, -d / 2, 0.0), (w / 2, d / 2, h), 0.02, body, seg=1)
    e = 0.05
    for sx in (-1, 1):
        for sy in (-1, 1):
            s.box(*span((sx * (w / 2 + 0.004), sy * (d / 2 + 0.004), 0.0), (sx * (w / 2 - e), sy * (d / 2 - e), h)),
                  pal.Steel)
            for z in (-0.002, h - 0.058):
                s.box(*span((sx * (w / 2 + 0.008), sy * (d / 2 + 0.008), z),
                            (sx * (w / 2 - 0.07), sy * (d / 2 - 0.07), z + 0.06)), pal.Chrome)
    for z0 in (0.0, h - 0.07):
        for sy in (-1, 1):
            s.box(*span((-w / 2 + e, sy * (d / 2 + 0.006), z0), (w / 2 - e, sy * (d / 2 - 0.01), z0 + 0.07)), pal.Steel)
        for sx in (-1, 1):
            s.box(*span((sx * (w / 2 + 0.006), -d / 2 + e, z0), (sx * (w / 2 - 0.01), d / 2 - e, z0 + 0.07)), pal.Steel)
        k = 5
        for i in range(k):
            x = -w / 2 + e + 0.06 + (w - 2 * e - 0.12) * i / (k - 1)
            with s.at((x, -d / 2 - 0.006, z0 + 0.035), (90, 0, 0)):
                s.lathe(dome(0.012, 0.0, 0.01, 2), pal.Chrome, seg=6)
    if label is not None:
        y = -d / 2 - 0.002
        a = min(w - 0.3, h - 0.26) / 2
        s.decal([(-a, y, h / 2 - a), (a, y, h / 2 - a), (a, y, h / 2 + a), (-a, y, h / 2 + a)], pal.Decal_Transit,
                cell(DECAL[label]))


def barrel(s, body):
    s.lathe([(0.0, 0.0), (0.26, 0.0), (0.28, 0.02), (0.3, 0.1), (0.31, 0.45), (0.3, 0.8), (0.28, 0.88),
             (0.265, 0.9), (0.24, 0.9), (0.24, 0.885), (0.0, 0.885)], body, seg=24)
    for z in (0.2, 0.68):
        s.lathe(ring(0.302, 0.318, z - 0.025, z + 0.025), pal.Chrome, seg=24, cap_bottom=False, cap_top=False)
    s.lathe(ring(0.255, 0.275, 0.885, 0.915), pal.Chrome, seg=24, cap_bottom=False, cap_top=False)
    with s.at((0.12, 0.05, 0.885)):
        s.lathe([(0.0, 0.0), (0.04, 0.0), (0.04, 0.03), (0.0, 0.03)], pal.Steel, seg=8)


@asset("CrateStack", "Alleys", bevel=0.0)
def crate_stack(s):
    with s.at((-0.75, 0.05, 0), (0, 0, 3)):
        crate(s, 1.0, 0.8, 0.7, pal.Enamel_Teal, "vesper_transit")
        with s.at((0.05, 0.03, 0.7), (0, 0, -8)):
            crate(s, 0.8, 0.64, 0.55, pal.Enamel_Mustard, "caution")
    with s.at((0.25, -0.08, 0), (0, 0, 11)):
        crate(s, 0.85, 0.72, 0.62, pal.Enamel_Ivory, "harbour_line_7")
    with s.at((1.0, 0.2, 0)):
        barrel(s, pal.Enamel_Coral)


# ----------------------------------------------------------------- steam vent

@asset("SteamVent", "Alleys", bevel=0.0)
def steam_vent(s):
    with s.at((-0.35, 0, 0)):
        s.box((-0.47, -0.47, -0.02), (0.47, 0.47, 0.004), pal.Enamel_Charcoal)
        for a in ((-0.5, -0.5, 0.5, -0.44), (-0.5, 0.44, 0.5, 0.5), (-0.5, -0.44, -0.44, 0.44), (0.44, -0.44, 0.5, 0.44)):
            s.box((a[0], a[1], 0.0), (a[2], a[3], 0.04), pal.Iron)
        for k in range(9):
            x = -0.4 + 0.1 * k
            s.box((x - 0.014, -0.44, 0.0), (x + 0.014, 0.44, 0.034), pal.Iron)
        for y in (-0.15, 0.15):
            s.box((-0.44, y - 0.012, 0.0), (0.44, y + 0.012, 0.03), pal.Iron)
    with s.at((0.5, 0.12, 0)):
        s.rbox((-0.28, -0.28, 0.0), (0.28, 0.28, 0.14), 0.03, pal.Stone_Dark, seg=1)
        s.lathe([(0.17, 0.14), (0.17, 0.78)], pal.Enamel_Teal, seg=20, cap_bottom=False, cap_top=False)
        for z in (0.2, 0.5):
            s.lathe(ring(0.168, 0.19, z - 0.03, z + 0.03), pal.Chrome, seg=20, cap_bottom=False, cap_top=False)
        s.lathe([(0.15, 0.76), (0.19, 0.78), (0.19, 0.84), (0.32, 0.93), (0.34, 0.98), (0.24, 1.04), (0.0, 1.06)],
                pal.Chrome, seg=20)
        for k in range(8):
            with s.at((0, 0, 0.66), (0, 0, 45 * k)):
                s.box((0.155, -0.018, -0.05), (0.176, 0.018, 0.05), pal.Enamel_Charcoal)
        elbow = fillet([(-0.55, -0.1, -0.05), (-0.55, -0.1, 0.4), (-0.17, -0.1, 0.4)], 0.2, 6, closed=False)
        s.sweep(elbow, 0.075, pal.Enamel_Mustard, seg=12)
        with s.at((-0.55, -0.1, 0.0)):
            s.lathe([(0.1, -0.02), (0.1, 0.05), (0.08, 0.065)], pal.Chrome, seg=12, cap_top=False)
        with s.at((-0.24, -0.1, 0.4), ALONG_X):
            s.lathe([(0.1, 0.0), (0.1, 0.07)], pal.Chrome, seg=12)


# ----------------------------------------------------------------- small props

@asset("BackDoorLamp", "Alleys", bevel=0.0)
def back_door_lamp(s):
    caged_lamp(s)


@asset("Pallet", "Alleys", bevel=0.004)
def pallet(s):
    L, W = 1.2, 0.8
    for i, (y, w) in enumerate(((-0.3275, 0.145), (-0.165, 0.1), (0.0, 0.145), (0.165, 0.1), (0.3275, 0.145))):
        s.box((-L / 2, y - w / 2, 0.122), (L / 2, y + w / 2, 0.144), pal.Timber if i % 2 == 0 else pal.Timber_B)
    for x in (-0.5275, 0.0, 0.5275):
        s.box((x - 0.0725, -W / 2, 0.1), (x + 0.0725, W / 2, 0.122), pal.Timber_B)
        for y in (-0.3275, 0.0, 0.3275):
            s.box((x - 0.0725, y - 0.0725, 0.022), (x + 0.0725, y + 0.0725, 0.1), pal.Timber)
    for y in (-0.3275, 0.0, 0.3275):
        s.box((-L / 2, y - 0.0725, 0.0), (L / 2, y + 0.0725, 0.022), pal.Timber_B)


def milk_crate(s, body, bottles=()):
    W, H, t = 0.34, 0.28, 0.02
    s.box((-W / 2 + 0.01, -W / 2 + 0.01, 0.0), (W / 2 - 0.01, W / 2 - 0.01, t), body)
    for sx in (-1, 1):
        for sy in (-1, 1):
            s.rbox((sx * W / 2 - 0.02 - 0.02 * sx, sy * W / 2 - 0.02 - 0.02 * sy, 0.0),
                   (sx * W / 2 + 0.02 - 0.02 * sx, sy * W / 2 + 0.02 - 0.02 * sy, H), 0.01, body, seg=1)
    for rot in (0, 90, 180, 270):
        with s.at((0, 0, 0), (0, 0, rot)):
            for z0, z1 in ((H - 0.035, H), (0.12, 0.15)):
                s.box((-W / 2 + 0.03, -W / 2, z0), (W / 2 - 0.03, -W / 2 + 0.018, z1), body)
            for x in (-0.075, 0.0, 0.075):
                s.box((x - 0.01, -W / 2 + 0.002, t), (x + 0.01, -W / 2 + 0.016, H - 0.035), body)
    for x, y in bottles:
        with s.at((x, y, t)):
            s.lathe([(0.0, 0.0), (0.036, 0.0), (0.038, 0.02), (0.038, 0.13), (0.03, 0.17), (0.019, 0.2),
                     (0.019, 0.225), (0.0, 0.227)], pal.Enamel_Ivory, seg=10)
            s.lathe([(0.021, 0.205), (0.021, 0.23), (0.0, 0.232)], pal.Gold, seg=10, cap_bottom=False)


@asset("MilkCrates", "Alleys", bevel=0.0)
def milk_crates(s):
    with s.at((-0.2, 0.02, 0), (0, 0, 4)):
        milk_crate(s, pal.Enamel_Teal)
        with s.at((0.01, 0.0, 0.28), (0, 0, 7)):
            milk_crate(s, pal.Enamel_Coral)
    with s.at((0.2, -0.04, 0), (0, 0, -9)):
        milk_crate(s, pal.Enamel_Mustard, [(-0.09, -0.09), (0.0, -0.09), (0.09, -0.09), (-0.09, 0.0), (0.09, 0.0),
                                            (0.0, 0.09)])


def bike_wheel(s, x, r=0.33):
    """Balloon-tyre wheel in the XZ plane at (x, r): rubber tyre, chrome rim, spokes, hub."""
    with s.at((x, 0, r), (90, 0, 0)):
        s.lathe([(r - 0.05, -0.02), (r - 0.03, -0.028), (r - 0.008, -0.022), (r, 0.0), (r - 0.008, 0.022),
                 (r - 0.03, 0.028), (r - 0.05, 0.02)], pal.Rubber, seg=20, cap_bottom=False, cap_top=False)
        s.lathe([(r - 0.05, 0.016), (r - 0.07, 0.01), (r - 0.07, -0.01), (r - 0.05, -0.016)], pal.Chrome, seg=20,
                cap_bottom=False, cap_top=False)
        s.lathe([(0.0, -0.05), (0.025, -0.05), (0.025, 0.05), (0.0, 0.05)], pal.Chrome, seg=8)
        for k in range(12):
            a = 2 * math.pi * k / 12
            strut(s, (0.02 * math.cos(a), 0.02 * math.sin(a), 0.04 * (-1) ** k),
                  ((r - 0.07) * math.cos(a), (r - 0.07) * math.sin(a), 0.0), 0.006, pal.Steel)


def fender(s, x, r, a0, a1, mat):
    """Enamel mudguard: a flat-topped arc over the wheel at (x, r), chrome tip."""
    pts = [(x + (r + 0.05) * math.cos(math.radians(a)), 0.0, r + (r + 0.05) * math.sin(math.radians(a)))
           for a in range(a0, a1 + 1, 15)]
    tube(s, pts, [(-0.01, -0.055), (0.012, -0.045), (0.012, 0.045), (-0.01, 0.055)], mat, up=(0, 1, 0))


def bicycle(s):
    """1950s balloon-tyre cruiser, upright, length along X (front +X): teal frame with a streamline tank, ivory
    fenders, chrome bars, sprung saddle, bullet headlamp, rear rack."""
    R, xf, xr = 0.33, 0.56, -0.56
    for x in (xf, xr):
        bike_wheel(s, x, R)
    fender(s, xf, R, 10, 150, pal.Enamel_Ivory)
    fender(s, xr, R, 30, 170, pal.Enamel_Ivory)
    bb, st, ht0, ht1 = (0.0, 0.0, 0.3), (-0.12, 0.0, 0.84), (0.42, 0.0, 0.66), (0.46, 0.0, 0.9)
    for a, b, r in ((bb, st, 0.02), (bb, ht0, 0.022), (st, ht1, 0.019), ((xr, 0, R), bb, 0.013),
                    ((xr, 0, R), st, 0.012), (ht0, ht1, 0.028)):
        for dy in ((-0.045, 0.045) if r < 0.015 else (0.0,)):
            s.sweep([(a[0], dy, a[2]), (b[0], dy, b[2])], r, pal.Enamel_Teal, seg=6)
    s.sweep([(ht0[0], 0, ht0[2]), (xf, 0, R)], 0.016, pal.Chrome, seg=6)  # fork
    tank = [(0.08, 0.5), (0.2, 0.68), (0.4, 0.8), (0.42, 0.7), (0.3, 0.56), (0.12, 0.44)]
    with s.at((0, 0, 0), (90, 0, 0)):
        s.extrude(tank, -0.04, 0.04, pal.Enamel_Teal, smooth_sides=True)
    s.sweep([(0.1, -0.042, 0.52), (0.38, -0.042, 0.72)], 0.008, pal.Chrome, seg=4)
    s.sweep([(0.1, 0.042, 0.52), (0.38, 0.042, 0.72)], 0.008, pal.Chrome, seg=4)
    s.sweep(fillet([(ht1[0], 0, ht1[2]), (ht1[0] + 0.02, 0, 1.0), (0.36, -0.3, 1.04), (0.3, -0.32, 1.02)], 0.05, 3,
                   closed=False), 0.013, pal.Chrome, seg=6)
    s.sweep(fillet([(ht1[0] + 0.02, 0, 1.0), (0.36, 0.3, 1.04), (0.3, 0.32, 1.02)], 0.05, 3, closed=False), 0.013,
            pal.Chrome, seg=6)
    for sy in (-1, 1):
        s.sweep([(0.33, sy * 0.28, 1.025), (0.27, sy * 0.35, 1.015)], 0.018, pal.Rubber, seg=6)
    s.sweep([st, (-0.15, 0, 0.93)], 0.012, pal.Chrome, seg=6)
    s.rbox((-0.3, -0.09, 0.93), (-0.04, 0.09, 0.99), 0.03, pal.Enamel_Charcoal, seg=2)  # saddle
    for sy in (-1, 1):
        s.sweep([(-0.26, sy * 0.05, 0.93), (-0.25, sy * 0.05, 0.88), (-0.15, sy * 0.02, 0.9)], 0.008, pal.Chrome, seg=4)
    with s.at((0.5, 0, 0.86), (0, 90, 0)):  # bullet headlamp
        s.lathe([(0.0, -0.08), (0.03, -0.075), (0.05, -0.03), (0.05, 0.02), (0.045, 0.03)], pal.Chrome, seg=10)
        s.lathe([(0.045, 0.028), (0.03, 0.04), (0.0, 0.045)], pal.Lamp, seg=10, cap_bottom=False)
    for x in (-0.72, -0.35):  # rear rack
        s.sweep([(x, -0.07, 0.72), (x + (0.1 if x > -0.5 else 0), 0.07, 0.72)], 0.008, pal.Chrome, seg=4)
    for sy in (-1, 1):
        s.sweep([(-0.74, sy * 0.07, 0.72), (-0.33, sy * 0.07, 0.72), (-0.12, sy * 0.05, 0.84)], 0.009, pal.Chrome,
                seg=4)
        s.sweep([(-0.72, sy * 0.07, 0.72), (xr, sy * 0.05, R)], 0.008, pal.Chrome, seg=4)
    with s.at((0, 0.06, 0.3), (-90, 0, 0)):  # chainring, cranks, pedals
        s.lathe([(0.0, 0.0), (0.1, 0.0), (0.1, 0.008), (0.0, 0.008)], pal.Chrome, seg=14)
    for sy, sz in ((1, -1), (-1, 1)):
        p = (0.0 + 0.04 * sz, sy * 0.1, 0.3 + 0.16 * sz)
        s.sweep([(0.0, sy * 0.08, 0.3), p], 0.012, pal.Chrome, seg=4)
        s.box((p[0] - 0.05, p[1] - 0.04 + 0.04 * sy, p[2] - 0.012), (p[0] + 0.05, p[1] + 0.04 + 0.04 * sy, p[2] + 0.012),
              pal.Rubber)
    s.sweep([(0.0, 0.07, 0.3), (xr, 0.07, R)], 0.006, pal.Iron, seg=4)  # chain line


@asset("BikeLean", "Alleys", bevel=0.0)
def bike_lean(s):
    """The cruiser leant against a wall behind it (+Y): tyres on the ground, bars resting on the wall at y = +0.33."""
    with s.at((0, -0.24, 0), (-14, 0, 0)):
        bicycle(s)
