"""Alleys spans (registered via alleys.py): pieces that cross or run along a 6 m alley.
  AlleyBridge    enclosed streamline footbridge 11.5-14.1 m up (floor 11.8 = frontage floor 3), porthole rows, chrome
                 ribs and speed lines, riding a twin Warren-truss gantry that carries a pipe gallery; ends at y = -+3.
  CableSpan_6    messenger + feed cable swagged at 8 m between wall rosettes, four atomic lanterns (emissive).
  PipeBundle_6   6 m of wall pipe run: five pipes on channel brackets, half flanges at both ends (a joint makes one
                 bolted flange pair), mid flanges, a red valve wheel and a gauge.
Bridge and cable pivot at the alley centre on the walk, spanning Y (walls at y = -+3); the bundle pivots on the wall
face at the walk, x along the wall, pipes at z 3.55-4.62."""

import math

from alleys_parts import ALONG_X, FRONT, BACK, HW, SKIN, sag
from geo import circle, dome, ring
from kit import asset
from mats import pal
from mega_parts import strut
from structures import crescent, superell, tube

ZF, HA, HB = 11.8, 1.25, 1.3  # bridge floor, tube half-width, half-height
ZC = ZF - 0.3 + HB
BRIDGE = superell(HA, HB, 2.8, 28)


def surface_x(z):
    """Tube half-width at height z (superellipse side)."""
    return HA * max(0.0, 1 - abs((z - ZC) / HB) ** 2.8) ** (1 / 2.8)


def span_y(y0, y1, z=ZC):
    return [(0.0, y0, z), (0.0, y1, z)]


def loop3(sec_pts, y, k=1.0):
    pts = [(-a * k, y, ZC + b * k) for a, b in sec_pts]
    return pts + pts[:1]


@asset("AlleyBridge", "Alleys", bevel=0.0)
def alley_bridge(s):
    tube(s, span_y(-3.25, 3.25), BRIDGE, pal.Enamel_Ivory)
    tube(s, span_y(-2.9, 2.9), crescent(BRIDGE, 15, 28, 1.012, 0.99), pal.Enamel_Teal)
    for sy in (-1, 1):  # wall collars: fat ivory ring, chrome bead on its alley edge
        tube(s, span_y(sy * 2.82, sy * 3.2), superell(HA * 1.1, HB * 1.08, 2.8, 28), pal.Enamel_Ivory)
        s.sweep(loop3(BRIDGE, sy * 2.82, 1.085), 0.04, pal.Chrome, seg=6, caps=False)
    for y in (-2.25, -0.75, 0.75, 2.25):
        s.sweep(loop3(BRIDGE, y, 1.012), 0.034, pal.Chrome, seg=6, caps=False)
    for sx in (-1, 1):
        for z in (ZC + 0.62, ZC + 0.72, ZC + 0.82):  # speed lines above the portholes
            x = sx * (surface_x(z) + 0.012)
            s.sweep([(x, -2.7, z), (x, 2.7, z)], 0.014, pal.Chrome, seg=6)
        for y in (-1.5, 0.0, 1.5):
            with s.at((sx * HA, y, ZC + 0.08), (0, sx * 90, 0)):
                s.lathe([(0.38, -0.04), (0.38, 0.02), (0.36, 0.05), (0.31, 0.055), (0.285, 0.03), (0.285, -0.02)],
                        pal.Chrome, seg=16, cap_bottom=False, cap_top=False)
                s.lathe([(0.29, 0.012), (0, 0.012)], pal.Window_Tower, seg=16, cap_bottom=False)
    tube(s, span_y(-2.6, 2.6, ZC + HB - 0.02), [(a * 0.16, b * 0.1 + 0.05) for a, b in circle(1.0, 10)],
         pal.Enamel_Teal)  # dorsal spine
    with s.at((0.0, 1.9, ZC + HB)):  # roof mast: whip, dipole bars, red beacon
        s.lathe([(0.1, -0.05), (0.1, 0.06), (0.03, 0.1), (0.012, 1.6), (0.0, 1.62)], pal.Chrome, seg=8)
        for zz, hw in ((0.9, 0.26), (1.25, 0.17)):
            strut(s, (-hw, 0, zz), (hw, 0, zz), 0.016, pal.Chrome)
        with s.at((0, 0, 1.62)):
            s.lathe([(0.04 * math.sin(math.pi * k / 4), -0.04 * math.cos(math.pi * k / 4)) for k in range(5)],
                    pal.Signal_Red, seg=6)
    gantry(s)


def gantry(s):
    """Twin Warren trusses under the belly (teal), cross beams at each panel point, pipe gallery and downlights."""
    zt, zb, xs, n = ZF - 0.32, ZF - 1.05, 0.9, 8
    ys = [-3.2 + 6.4 * i / n for i in range(n + 1)]
    for sx in (-1, 1):
        x = sx * xs
        for z in (zt, zb):
            strut(s, (x, -3.2, z), (x, 3.2, z), 0.09, pal.Enamel_Teal)
        for i, y in enumerate(ys):
            strut(s, (x, y, zb), (x, y, zt), 0.06, pal.Enamel_Teal)
            if i < n:
                a, b = ((y, zb), (ys[i + 1], zt)) if i % 2 else ((y, zt), (ys[i + 1], zb))
                strut(s, (x, a[0], a[1]), (x, b[0], b[1]), 0.05, pal.Enamel_Teal)
    for y in ys[1:-1]:
        strut(s, (-xs, y, zb), (xs, y, zb), 0.06, pal.Steel)
        strut(s, (-xs, y, zt), (xs, y, zt), 0.05, pal.Steel)
    for x, z, r, m in ((-0.4, zb + 0.2, 0.15, pal.Enamel_Coral), (0.3, zb + 0.16, 0.1, pal.Chrome),
                       (0.62, zb + 0.12, 0.05, pal.Enamel_Mustard)):
        with s.at((x, -3.25, z), BACK):
            s.lathe([(r, 0.0), (r, 6.5)], m, seg=12, cap_bottom=False, cap_top=False)
            for yy in (2.95, 3.25, 3.55):  # centre flange pair + a mid band
                s.lathe(ring(r - 0.01, r + 0.035, yy - 0.03, yy + 0.03), pal.Chrome, seg=12, cap_bottom=False,
                        cap_top=False)
    for y in (-1.6, 1.6):
        with s.at((0.0, y, zb - 0.04), (180, 0, 0)):
            s.lathe([(0.14, -0.03), (0.14, 0.02), (0.11, 0.04), (0.0, 0.04)], pal.Chrome, seg=12)
            s.lathe(dome(0.1, 0.04, 0.05, 3), pal.Lamp, seg=12)


# ----------------------------------------------------------------- cable span

def lantern(s, cap):
    """Atomic lantern hanging from its origin: enamel cap, warm globe, chrome Saturn ring and finial."""
    s.sweep([(0, 0, 0.0), (0, 0, -0.2)], 0.006, pal.Steel, seg=4)
    s.lathe([(0.0, -0.33), (0.12, -0.33), (0.11, -0.3), (0.06, -0.24), (0.015, -0.2), (0, -0.19)], cap, seg=12)
    zc = -0.44
    s.lathe([(0.12 * math.sin(math.pi * k / 6), zc - 0.11 * math.cos(math.pi * k / 6)) for k in range(7)], pal.Lamp,
            seg=12)
    s.lathe(ring(0.15, 0.2, zc - 0.008, zc + 0.008), pal.Chrome, seg=16, cap_bottom=False, cap_top=False)
    for k in range(3):
        a = 2 * math.pi * k / 3
        strut(s, (0.1 * math.cos(a), 0.1 * math.sin(a), zc), (0.155 * math.cos(a), 0.155 * math.sin(a), zc), 0.01,
              pal.Chrome)
    s.lathe([(0.0, zc - 0.25), (0.03, zc - 0.14), (0.035, zc - 0.11), (0.0, zc - 0.1)], pal.Chrome, seg=8)


@asset("CableSpan_6", "Alleys", bevel=0.0)
def cable_span(s):
    Z, SAG, Y = 8.0, 0.5, 3.0 + SKIN
    for sy in (-1, 1):
        with s.at((0, sy * Y, Z), BACK if sy < 0 else FRONT):
            s.lathe([(0.1, 0.0), (0.1, 0.02), (0.07, 0.045), (0.03, 0.055), (0.0, 0.055)], pal.Chrome, seg=16)
            s.lathe([(0.018, 0.05), (0.018, 0.16)], pal.Steel, seg=8, cap_bottom=False)
            s.lathe([(0.0, 0.16), (0.03, 0.17), (0.035, 0.26), (0.03, 0.3), (0.0, 0.31)], pal.Enamel_Mustard, seg=10)
    main = sag((0.03, -Y + 0.3, Z), (0.03, Y - 0.3, Z), SAG, 20)
    s.sweep(main, 0.013, pal.Steel, seg=5)
    s.sweep(sag((-0.03, -Y + 0.3, Z - 0.02), (-0.03, Y - 0.3, Z - 0.02), SAG + 0.06, 20), 0.009, pal.Rubber, seg=5)
    caps = (pal.Enamel_Teal, pal.Enamel_Coral, pal.Enamel_Mustard, pal.Enamel_Teal)
    for y, cap in zip((-2.1, -0.7, 0.7, 2.1), caps):
        u = (y + Y - 0.3) / (2 * Y - 0.6)
        with s.at((0.0, y, Z - 4 * SAG * u * (1 - u))):
            lantern(s, cap)


# ----------------------------------------------------------------- pipe bundle

PIPES = ((0.11, 4.38, -0.32, "Enamel_Teal"), (0.075, 4.06, -0.27, "Enamel_Mustard"), (0.06, 3.8, -0.23, "Chrome"),
         (0.05, 3.58, -0.2, "Enamel_Coral"), (0.028, 4.62, -0.14, "Rubber"))


def flange(s, r, x0, x1, bolts=4):
    with s.at((x0, 0, 0), ALONG_X):
        s.lathe([(r - 0.004, 0.0), (r + 0.055, 0.0), (r + 0.055, x1 - x0), (r - 0.004, x1 - x0)], pal.Chrome,
                seg=12, cap_bottom=False, cap_top=False)
    for k in range(bolts):
        a = 2 * math.pi * (k + 0.5) / bolts
        c = ((r + 0.035) * math.cos(a), (r + 0.035) * math.sin(a))
        strut(s, (x0 - 0.012, c[0], c[1]), (x1 + 0.012, c[0], c[1]), 0.02, pal.Steel, caps=True)


@asset("PipeBundle_6", "Alleys", bevel=0.0)
def pipe_bundle(s):
    for r, z, y, m in PIPES:
        with s.at((0, y, z)):
            with s.at((-HW, 0, 0), ALONG_X):
                s.lathe([(r, 0.0), (r, 2 * HW)], getattr(pal, m), seg=14 if r > 0.04 else 6, cap_bottom=False,
                        cap_top=False)
            if r > 0.04:
                flange(s, r, -HW, -HW + 0.03)
                flange(s, r, HW - 0.03, HW)
                flange(s, r, -0.035, 0.035)
    for xb in (-1.5, 1.5):  # channel posts, arms, U-clamps
        s.rbox((xb - 0.06, SKIN - 0.07, 3.35), (xb + 0.06, SKIN + 0.01, 4.8), 0.015, pal.Enamel_Charcoal, seg=1)
        for r, z, y, m in PIPES:
            s.box((xb - 0.035, y, z - r - 0.05), (xb + 0.035, SKIN - 0.06, z - r - 0.01), pal.Steel)
            R = r + 0.015
            s.sweep([(xb, y + R * math.cos(math.pi * k / 8), z + R * math.sin(math.pi * k / 8)) for k in range(9)],
                    0.012, pal.Chrome, seg=5)
    r, z, y = PIPES[0][:3]  # gate valve on the teal main: body, bonnet, red hand wheel
    with s.at((0.8, y, z)):
        with s.at((-0.16, 0, 0), ALONG_X):
            s.lathe([(r + 0.02, 0.0), (r + 0.05, 0.05), (r + 0.06, 0.16), (r + 0.05, 0.27), (r + 0.02, 0.32)],
                    pal.Chrome, seg=16, cap_bottom=False, cap_top=False)
        s.lathe([(0.07, r), (0.06, r + 0.16), (0.075, r + 0.18), (0.075, r + 0.22), (0.02, r + 0.24)], pal.Chrome,
                seg=12, cap_bottom=False)
        s.lathe([(0.012, r + 0.2), (0.012, r + 0.36)], pal.Steel, seg=6)
        zw = r + 0.34
        s.sweep([(0.19 * math.cos(t), 0.19 * math.sin(t), zw) for t in (2 * math.pi * i / 20 for i in range(21))],
                0.018, pal.Enamel_Red, seg=6, caps=False)
        for k in range(4):
            a = math.pi / 4 + math.pi / 2 * k
            s.sweep([(0.02 * math.cos(a), 0.02 * math.sin(a), zw), (0.18 * math.cos(a), 0.18 * math.sin(a), zw)],
                    0.011, pal.Enamel_Red, seg=5)
        s.lathe(dome(0.035, zw - 0.02, 0.05, 3), pal.Chrome, seg=10)
    r, z, y = PIPES[2][:3]  # pressure gauge on the chrome line
    with s.at((-0.9, y, z)):
        s.lathe([(0.012, r - 0.01), (0.012, r + 0.1)], pal.Chrome, seg=6)
        with s.at((0, -0.02, r + 0.17), FRONT):
            s.lathe([(0.075, -0.03), (0.075, 0.02), (0.065, 0.03), (0.0, 0.03)], pal.Chrome, seg=16)
            s.lathe([(0.058, 0.032), (0.0, 0.032)], pal.Enamel_Ivory, seg=16, cap_bottom=False)
            s.box((-0.004, 0.0, 0.034), (0.004, 0.045, 0.037), pal.Enamel_Red)
