"""The sedan, finished (brief 070, Vela; Glyph on the briefing's getaway still: "the car model has no detail and looks like a stand-in"). The adopted sedans (cars020) are a smooth tub, a glass
cabin with a painted roof pill and blade fins; their bumper, grille and headlamps are modelled inside the nose (the tub's nose is at y -2.66, the bumper at -2.36), so a close still sees a
blank slab. This is the same car brought to Sedan_Rival's finish, keeping what makes it the cell's car (the slab body and pill hardtop, coral with a seafoam roof):

  the nose     a wrap-round chrome bumper on the nose's own outline with two over-riders; an oval chrome grille mouth with vertical bars over a dark intake; round headlamps in body-colour
               pods under chrome eyebrows; parking lamps; a chrome comet on the hood and a chrome hood spear
  the body     wheel arches cut in; a seafoam two-tone flash down each flank, edged in chrome; door lines, handles and a chrome rocker strip; the paint is DK_CarEnamel_* (the base enamel
               with car_tex.py's flake, film and orange-peel detail pair: the "body texture")
  the cabin    chrome window base and drip rail, seafoam A and C posts and a chrome B post: a hardtop on posts, not a glass blob under a lid
  the tail     rounded fins with chrome edges and bullet tail lamps, tail lamps in the panel, a chrome tail strip and a wrap-round rear bumper
  wheels       24 sides (the old 14), whitewalls, chrome caps

Same chassis as cars020 (TUB heights and half-widths, SIDES, WHEELS_Y, the 0.92 scale), so the footprint, the pivot and the wheel stations are unchanged and nothing in the sim or the world moves;
everything added stays inside the old bounds (x +-0.986, y -2.66..2.48, z <= 1.40 in model units). Length along Y, nose at -Y, pivot = footprint centre on the ground. Full detail (nolod).
Only the getaway is catalogued (Sedan_Getaway_v2, candidate); sedan_v2() takes any livery, so Vale's car can follow (Sedan_Official_v2 is render-only until asked)."""

import math

from geo import fillet
from kit import REGISTRY, asset
from mats import car_enamel, pal
from vehicles_parts import carve, lamp, lerp_rows, mesh, plan, rot_to, wheel
from cars020 import SIDES, TUB, WHEELS_Y

E = 2.6  # the tub's and the cabin's superellipse caps (cars020 vloft's e)
CAB = [(0.98, 0.80, -0.50, 1.46, 0.55, 0.60), (1.18, 0.75, -0.30, 1.34, 0.55, 0.60), (1.34, 0.66, -0.05, 1.12, 0.52, 0.56), (1.40, 0.63, 0.05, 1.04, 0.50, 0.54)]  # cars020's cabin
SIDES_V2 = [0, 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1]  # the same straight sides, more stations: the arches cut clean and the flank shades smooth


def end_y(rows, z, x, end):
    """The outline's y at lateral x and height z: end -1 the nose, +1 the tail (rows: (z, hw, yf, yr, df, dr) sections; superellipse caps)."""
    hw, yf, yr, df, dr = (lerp_rows(rows, z, i) for i in (1, 2, 3, 4, 5))
    u = min(abs(x) / hw, 0.999)
    c = (1 - u ** E) ** (1 / E)
    return yf - df * c if end < 0 else yr + dr * c


def half_w(row, y):
    """A section's half-width at y (its superellipse caps), or None past its tips."""
    z, hw, yf, yr, df, dr = row
    if y < yf - df or y > yr + dr:
        return None
    if y > yr:
        u = (y - yr) / dr
    elif y < yf:
        u = (yf - y) / df
    else:
        return hw
    return hw * max(1 - u ** E, 0.0) ** (1 / E)


def side_x(z):
    """The flank's half-width at height z (the straight sides)."""
    return lerp_rows(TUB, z, 1)


def _nose_pt(x, z, off=0.0):
    return (x, end_y(TUB, z, x, -1) - off, z)


def _tail_pt(x, z, off=0.0):
    return (x, end_y(TUB, z, x, +1) + off, z)


def _wrap(s, z, end, r, off, mat, span=0.92, n=26):
    pts = []
    for k in range(n + 1):
        x = side_x(z) * span * math.sin(math.radians(-90 + 180 * k / n))
        pts.append(_nose_pt(x, z, off) if end < 0 else _tail_pt(x, z, off))
    s.sweep(pts, r, mat, seg=10)


def _nose(s, body):
    _wrap(s, 0.36, -1, 0.055, 0.035, pal.Chrome)          # the bumper, on the nose's own outline
    _wrap(s, 0.29, -1, 0.025, 0.01, pal.Chrome, span=0.8)  # its lower lip
    for sg in (-1, 1):                                       # over-riders: chrome teardrops standing on the bumper
        x = sg * 0.36
        y = end_y(TUB, 0.36, x, -1) - 0.03
        s.sweep([(x, y + 0.03, 0.29), (x, y - 0.012, 0.37), (x, y - 0.002, 0.47), (x, y + 0.04, 0.54)], [0.026, 0.035, 0.028, 0.014], pal.Chrome, seg=10)
    # the grille mouth: an oval (x half 0.46, z 0.45..0.65) laid on the nose, a dark intake, vertical chrome bars, a chrome frame
    cx, cz, ax, az = 0.0, 0.55, 0.46, 0.10

    def zr(x):  # the oval's half-height at x
        return az * max(1 - (abs(x) / ax) ** 2.4, 0.0) ** (1 / 2.4)
    xs = [ax * math.sin(math.radians(-90 + 180 * k / 24)) for k in range(25)]
    rows = [[_nose_pt(x, cz + zr(x) * t, -0.012) for x in xs] for t in (-1.0, -0.5, 0.0, 0.5, 1.0)]
    mesh(s, rows, pal.Enamel_Charcoal, closed=False, caps=None)
    for k in range(11):
        x = -0.40 + 0.08 * k
        h = zr(x)
        if h > 0.02:
            s.sweep([_nose_pt(x, cz + h * t, -0.002) for t in (-0.92, -0.3, 0.3, 0.92)], 0.011, pal.Chrome, seg=6)
    s.sweep([_nose_pt(x, cz, 0.002) for x in xs[2:-2]], 0.012, pal.Chrome, seg=6)  # the cross bar
    ring = [_nose_pt(ax * math.cos(a), cz + zr(ax * math.cos(a)) * (1 if math.sin(a) >= 0 else -1), -0.011) for a in (2 * math.pi * (k + 0.5) / 40 for k in range(40))]  # the frame stands 14 mm proud: the whole nose stays inside the old outline
    s.sweep(ring + ring[:1], 0.022, pal.Chrome, seg=8, caps=False)
    for sg in (-1, 1):  # headlamps in body-colour pods, a chrome eyebrow over each; parking lamps below
        x, z = sg * 0.66, 0.70
        p = _nose_pt(x, z, -0.045)
        n = (sg * 0.32, -1, 0)
        lamp(s, p, n, 0.095, depth=0.05, housing=body, hr=0.14, hd=0.05, seg=20)
        brow = []
        for k in range(9):
            a = math.radians(200 - 140 * k / 8) if sg < 0 else math.radians(-20 + 140 * k / 8)
            xx = x + sg * 0.0 + 0.15 * math.cos(a)
            zz = z + 0.02 + 0.12 * math.sin(a)
            brow.append(_nose_pt(xx, zz, 0.05))
        s.sweep(brow, 0.016, pal.Chrome, seg=6)
        lamp(s, _nose_pt(sg * 0.66, 0.47, -0.005), (sg * 0.3, -1, 0), 0.045, depth=0.03, seg=12)


def _hood(s):
    zt = TUB[-1][0]
    s.sweep([(0, -2.38, zt - 0.012), (0, -2.1, zt + 0.004), (0, -1.15, zt + 0.006)], [0.012, 0.016, 0.012], pal.Chrome, seg=6)  # the spear
    zc = zt + 0.032
    with s.at((0, -1.95, zc), rot_to((0, -1, 0.04))):  # the comet: a low chrome jet riding the spear, its tail flaring back into two swept vanes
        s.lathe([(0.0, -0.16), (0.018, -0.12), (0.03, -0.02), (0.026, 0.07), (0.012, 0.12), (0.0, 0.14)], pal.Chrome, seg=14)
    for sg in (-1, 1):
        s.sweep([(sg * 0.012, -1.88, zc), (sg * 0.06, -1.80, zc - 0.012), (sg * 0.075, -1.76, zc - 0.024)], [0.012, 0.009, 0.006], pal.Chrome, seg=5)


def _flank(s, roof):
    """The two-tone flash: a seafoam spear from the tail to a point behind the front wheel, on the flank, edged in chrome; door lines, handles, a chrome rocker strip."""
    ys = [-1.05 + (2.02 + 1.05) * k / 24 for k in range(25)]

    def band(y):  # the flash's low and high z at y: full at the tail, to a point at the front
        t = (y - ys[0]) / (ys[-1] - ys[0])
        mid, half = 0.815 - 0.015 * t, 0.008 + 0.05 * t ** 0.8  # above the rear arch (its top is at 0.75)
        return mid - half, mid + half
    for sg in (-1, 1):
        rows = []
        for y in ys:
            lo, hi = band(y)
            col = [(sg * (side_x(z) + 0.004), y, z) for z in (lo + (hi - lo) * k / 4 for k in range(5))]
            rows.append(col if sg > 0 else col[::-1])
        mesh(s, rows, roof, closed=False, caps=None)
        zt = sum(band(ys[0])) / 2
        tip = (sg * (side_x(zt) + 0.007), ys[0] - 0.08, zt)  # the spear's point, where its two chrome edges meet
        for k in (0, 1):
            s.sweep([tip] + [(sg * (side_x(band(y)[k]) + 0.008), y, band(y)[k]) for y in ys], 0.01, pal.Chrome, seg=5, caps=False)
        for y, z0 in ((-0.70, 0.44), (0.42, 0.44), (1.48, 0.78)):  # door lines: charcoal hairlines from the sill (the rear one from over the arch) to the beltline
            s.sweep([(sg * (side_x(z) + 0.003), y, z) for z in (z0 + (0.95 - z0) * k / 4 for k in range(5))], 0.006, pal.Enamel_Charcoal, seg=4)
        for y in (0.22, 1.30):
            x = sg * (side_x(0.90) + 0.008)
            s.rbox((x - 0.013, y - 0.085, 0.885), (x + 0.013, y + 0.085, 0.915), 0.009, pal.Chrome, seg=1)
        x = sg * (side_x(0.42) + 0.006)
        s.sweep([(x, -0.92, 0.42), (x, 0.0, 0.42), (x, 0.93, 0.42)], 0.012, pal.Chrome, seg=5)  # the rocker strip, between the arches
        x = sg * (side_x(0.95) + 0.004)
        s.sweep([(x, -2.0, 0.95), (x, 0.0, 0.955), (x, 1.95, 0.95)], 0.009, pal.Chrome, seg=4, caps=False)  # the beltline


def _cabin(s, roof):
    mesh(s, [[(x, y, r[0]) for x, y in plan(r[1], r[2], r[3], r[4], r[5], SIDES_V2, 16, E)] for r in CAB], pal.GlassDark,
         paint=lambda i, j, c: roof if i == len(CAB) - 2 else None, caps=((0, None), (-1, roof)))
    for k, dz, rr in ((0, 0.012, 0.02), (2, 0.004, 0.016)):  # the window base and the drip rail
        r = CAB[k]
        pts = [(x, y, r[0] + dz) for x, y in plan(r[1] + 0.012, r[2], r[3], r[4], r[5], SIDES_V2, 16, E)]
        s.sweep(pts + pts[:1], rr, pal.Chrome, seg=6, caps=False)
    for (yb, yt), mat, rr in (((-0.86, -0.36), roof, 0.035), ((0.45, 0.45), pal.Chrome, 0.016), ((1.62, 0.96), roof, 0.045)):  # A, B and C posts
        for sg in (-1, 1):
            path = []
            for i, r in enumerate(CAB[:3]):
                y = yb + (yt - yb) * i / 2
                w = half_w(r, y)
                if w is None:
                    break
                path.append((sg * (w + 0.004), y, r[0] + (0.01 if i == 0 else -0.004 if i == 2 else 0)))
            if len(path) == 3:
                s.sweep(path, rr, mat, seg=6)


def _fin_outline():
    pts = [(1.05, 0.97)]
    for k in range(1, 8):
        t = k / 7
        pts.append((1.05 + (2.28 - 1.05) * t, 0.97 + (1.30 - 0.97) * t ** 2.4))
    return pts + [(2.36, 1.30), (2.38, 1.12), (2.30, 0.97)]


def _tail(s, body):
    poly = fillet(_fin_outline(), 0.05, n=4)
    for sg in (-1, 1):
        x = sg * 0.80
        with s.at((x, 0, 0), (90, 0, 90)):
            s.extrude(poly, -0.035, 0.035, body)
        s.sweep([(x, y, z + 0.012) for y, z in _fin_outline()[2:-2]] + [(x, 2.36, 1.31)], 0.022, pal.Chrome, seg=8)  # the fin's chrome crest
        with s.at((x, 2.36, 1.17), rot_to((0, 1, 0))):  # the bullet tail lamp: a chrome sleeve, a red lens
            s.lathe([(0.0, -0.10), (0.045, -0.08), (0.05, 0.0), (0.05, 0.02)], pal.Chrome, seg=14, cap_bottom=False)
            s.lathe([(0.05, 0.02), (0.04, 0.05), (0.0, 0.065)], pal.Signal_Red, seg=14, cap_bottom=False)
        lamp(s, _tail_pt(sg * 0.64, 0.72, -0.01), (sg * 0.3, 1, 0), 0.065, lens=pal.Signal_Red, depth=0.035, seg=14)
    _wrap(s, 0.36, +1, 0.055, 0.03, pal.Chrome)
    _wrap(s, 0.60, +1, 0.012, 0.006, pal.Chrome, span=0.8)  # the tail strip
    s.sweep([_tail_pt(x, 0.83, 0.008) for x in (-0.10, 0.0, 0.10)], 0.013, pal.Chrome, seg=6)  # the boot handle


def _arches(t):
    for sg in (-1, 1):
        for y in WHEELS_Y:
            with t.at((0.58 if sg > 0 else -1.10, y, 0.34), (0, 90, 0)):
                t.lathe([(0.41, 0), (0.41, 0.52)], pal.Rubber, seg=32)


def _sedan_v2(s, body, roof):
    carve(s, lambda t: mesh(t, [[(x, y, r[0]) for x, y in plan(r[1], r[2], r[3], r[4], r[5], SIDES_V2, 16, E)] for r in TUB], body), _arches)
    _cabin(s, roof)
    _nose(s, body)
    _hood(s)
    _flank(s, roof)
    _tail(s, body)
    for sg in (-1, 1):
        for y in WHEELS_Y:
            wheel(s, (sg * 0.84, y, 0.34), 0.34, 0.2, white=(0.21, 0.29), hub=0.5, rim=pal.Steel, seg=24, side=sg)


def sedan_v2(s, body, roof):
    """The finished sedan in a livery: body and roof enamels (base LIB names; their DK_CarEnamel_ variants are drawn)."""
    with s.at((0, 0, 0), (0, 0, 0), scale=0.92):  # cars020's 5.1 m model and 0.92
        _sedan_v2(s, car_enamel(body), car_enamel(roof))


@asset("Sedan_Getaway_v2", "Vehicles", bevel=0.0)
def sedan_getaway_v2(s):
    """The cell's getaway car, finished (brief 070): coral, seafoam roof and flash, chrome."""
    sedan_v2(s, "DK_Accent_Coral", "DK_Enamel_Seafoam")


REGISTRY["Sedan_Getaway_v2"]["nolod"] = True  # full detail (Sky): no decimated levels, as Sedan_Rival
