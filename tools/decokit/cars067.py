"""The rivals' sedan (brief 067, Vela; Sky on 0.2.122: "people wearing the wrong look confuse him"): a variant of the adopted sedan (cars020: the same wheels and wheel stations, side stations, section
heights and half-widths, scale, footprint and pivot, so it parks in the same bays and the sim's car footprint is unchanged) that reads as the rival crew's car and neither as Vale's (deep teal, ivory
roof) nor the cell's (coral, seafoam roof) at play zoom, by silhouette and by livery.

  Silhouette  a torpedo plan (a long pointed nose, a tapering tail) in place of the slab's square ends; a smoked bubble canopy in place of the painted roof pill: taller, a teardrop in plan,
              lacquer-black over a chrome rim and pillars; swept tail fins with rounded chrome edges; a wrap-round chrome bumper with bullets and a slatted grille mouth; wheel arches cut in.
  Livery      deep plum enamel (the crew's colour: their figures wear violet and navy), ink-black lacquer for the roof, intake and sills, a pair of pearl stripes down the hood, chrome.
              Never cream, ivory, teal, coral or seafoam; never gold or brass; nothing emissive beyond the sedan's own lamps (the rivals' violet signal marks draw over it, unlit).
Concept: paid/067-rival-car/codex/rival-sedan-options.png (Codex image tool, $0; prompt in its ledger). Length along Y, nose at -Y, pivot = footprint centre on the ground, modelled at 5.1 m
and scaled 0.92 as the other two. Full detail: no lean, no cap, no LODs (nolod)."""

import math

from geo import fillet
from kit import REGISTRY, asset
from mats import pal, register
from vehicles_parts import carve, lamp, lerp_rows, loop, mesh, rot_to, wheel
from cars020 import SIDES, TUB, WHEELS_Y

register("DK_Enamel_Plum", "enamel", "#482A5A")  # the crew's colour: the concept sheets' violet_set (#6b4f8f) taken down to their jackets' value (violet quilting over navy), a plum that stays dark in the sun
register("DK_Enamel_Ink", "enamel", "#1B1723")   # violet-black lacquer: roof, intake, sills
register("DK_Enamel_Pearl", "enamel", "#D9D3E3")  # cool pearl: the stripes (cooler and paler than the official car's ivory roof)

Z0 = 0.98  # the deck: the canopy stands on it
# the body: the sedan's own tub sections (cars020.TUB: the heights and half-widths), re-planned as a torpedo: per section the front / rear end of the straight sides and the front / rear cap depth
# (a long ellipse for a nose, a shorter rear cap); z, half-width, then those four
_PLAN = [(-1.55, 1.95, 0.85, 0.42), (-1.62, 2.02, 0.90, 0.40), (-1.65, 2.04, 0.90, 0.40), (-1.60, 1.98, 0.92, 0.42), (-1.45, 1.86, 0.95, 0.46)]
TUB_R = [(z, hw, *p) for (z, hw, *_), p in zip(TUB, _PLAN)]
EF_TUB, ER_TUB = 2.0, 2.6  # the nose is an ellipse, the tail a squarer cap
E_F, E_R = 2.3, 1.75       # the canopy's plan: a round front cap and a more pointed rear cap (a teardrop)


def _plan(hw, yf, yr, df, dr, sides, nc=14, ef=2.6, er=2.6):
    """vehicles_parts.plan with a separate superellipse exponent for each cap (CCW, as plan())."""
    se = lambda t, e: (math.copysign(abs(math.cos(t)) ** (2 / e), math.cos(t)), math.copysign(abs(math.sin(t)) ** (2 / e), math.sin(t)))  # noqa: E731
    ys = [yf + (yr - yf) * t for t in sides]
    pts = [(hw, y) for y in ys]
    pts += [(hw * cx, yr + dr * sy) for cx, sy in (se(math.pi * k / nc, er) for k in range(1, nc))]
    pts += [(-hw, y) for y in reversed(ys)]
    pts += [(hw * cx, yf + df * sy) for cx, sy in (se(math.pi * (1 + k / nc), ef) for k in range(1, nc))]
    return pts


def _edge(rows, z, x, ef, er, end):
    """The body's outline y at lateral x and height z: end = -1 the nose, +1 the tail (the tip at x = 0, back to the straight sides at |x| = hw)."""
    hw, yf, yr, df, dr = (lerp_rows(rows, z, i) for i in (1, 2, 3, 4, 5))
    u = min(abs(x) / hw, 0.999)
    return yf - df * (1 - u ** ef) ** (1 / ef) if end < 0 else yr + dr * (1 - u ** er) ** (1 / er)


def _canopy_rows(n=9, top_deg=80.0):
    """The bubble: plan sections up an ellipse in side view (a short steep front, a long raked rear: a fastback), every section a teardrop that shrinks toward the roof."""
    H, hw0, tf0, tr0, af, ar = 0.56, 0.78, -1.30, 2.12, -0.15, 0.50  # height, half-width, base tips front / rear, the apex's front / rear y
    rows = []
    for k in range(n + 1):
        t = math.radians(top_deg * k / n)
        c = math.cos(t)
        tf, tr = af + (tf0 - af) * c, ar + (tr0 - ar) * c
        L = tr - tf
        df, dr = 0.34 * L, 0.60 * L
        rows.append((Z0 + H * math.sin(t), hw0 * c ** 0.8, tf + df, tr - dr, df, dr))
    return rows


def _half_width(row, y):
    """The canopy plan's half-width of a row at y (the superellipse caps of _plan), or None outside it."""
    z, hw, yf, yr, df, dr = row
    if y < yf - df or y > yr + dr:
        return None
    if y > yr:
        u, e = (y - yr) / dr, E_R
    elif y < yf:
        u, e = (yf - y) / df, E_F
    else:
        return hw
    return hw * (1 - u ** e) ** (1 / e)


def _canopy(s, rows, z_roof):
    """The bubble: one ink-lacquer loft (a clear coat reads as smoked glass without a glass film, which would tint it teal), chrome rim, pillars and roof rim."""
    mesh(s, [[(x, y, r[0]) for x, y in _plan(r[1], r[2], r[3], r[4], r[5], SIDES, 14, E_F, E_R)] for r in rows], pal.Enamel_Ink, caps=((0, None), (-1, pal.Enamel_Ink)))
    base = rows[0]  # a chrome rim round the base, and three pillars up the dome (they read as windows under the roof rim)
    pts = [(x, y, Z0 + 0.012) for x, y in _plan(base[1] + 0.012, base[2], base[3], base[4], base[5], SIDES, 14, E_F, E_R)]
    s.sweep(loop(pts), 0.02, pal.Chrome, seg=6, caps=False)
    for yp in (-0.72, 0.30, 1.28):
        for sg in (-1, 1):
            path = []
            for r in rows:
                if r[0] > z_roof + 0.02:
                    break
                w = _half_width(r, yp)
                if w is None:
                    break
                path.append((sg * (w + 0.008), yp, r[0]))
            if len(path) > 2:
                s.sweep(path, 0.014, pal.Chrome, seg=5)
    k = next(i for i, r in enumerate(rows) if r[0] > z_roof)  # the roof's rim: the first section above the window line
    r = rows[k]
    pts = [(x, y, r[0] + 0.004) for x, y in _plan(r[1] + 0.006, r[2], r[3], r[4], r[5], SIDES, 14, E_F, E_R)]
    s.sweep(loop(pts), 0.016, pal.Chrome, seg=6, caps=False)


def _fin_outline(lead_y=0.55, crest=(2.10, 1.56), tip=(2.30, 1.62), z=0.97):
    pts = [(lead_y, z)]
    for k in range(1, 9):
        t = k / 8
        pts.append((lead_y + (crest[0] - lead_y) * t, z + (crest[1] - z) * t ** 2.2))
    pts += [tip, (tip[0] + 0.01, 1.36), (2.30, z)]
    return pts


def _fins(s):
    poly = fillet(_fin_outline(), 0.07, n=4)
    for sg in (-1, 1):
        x = sg * 0.78
        with s.at((x, 0, 0), (90, 0, 90)):
            s.extrude(poly, -0.05, 0.05, pal.Enamel_Plum)
        s.sweep([(x, y, z) for y, z in _fin_outline()[1:-2]] + [(x, 2.30, 1.59)], 0.05, pal.Chrome, seg=8)  # the chrome edge rides the leading edge and the tip
        lamp(s, (x, 2.31, 1.27), (0, 1, 0), 0.05, lens=pal.Signal_Red, depth=0.03, seg=10)


def _wrap(s, z, end, r, off, mat, span=0.9, n=22):
    """A tube round the nose (end -1) or the tail (+1) at height z, `off` outside the body's outline, from -span to +span of the half-width."""
    hw = lerp_rows(TUB_R, z, 1)
    pts = []
    for k in range(n + 1):
        x = hw * span * math.sin(math.radians(-90 + 180 * k / n))
        y = _edge(TUB_R, z, x, EF_TUB, ER_TUB, end) + (off if end > 0 else -off)
        pts.append((x, y, z))
    s.sweep(pts, r, mat, seg=8)


def _nose(s):
    _wrap(s, 0.40, -1, 0.06, 0.09, pal.Chrome)  # the bumper, wrapping the nose
    for sg in (-1, 1):  # bumper bullets
        x = sg * 0.46
        with s.at((x, _edge(TUB_R, 0.40, x, EF_TUB, ER_TUB, -1) - 0.12, 0.40), rot_to((0, -1, 0))):
            s.lathe([(0.0, -0.05), (0.07, -0.05), (0.082, 0.0), (0.068, 0.07), (0.04, 0.13), (0.0, 0.16)], pal.Chrome, seg=14)
    xs = [-0.5 + 1.0 * i / 14 for i in range(15)]
    for z in (0.55, 0.605, 0.66, 0.715):  # a slatted mouth: chrome bars with the dark intake between
        s.sweep([(x, _edge(TUB_R, z, x, EF_TUB, ER_TUB, -1) - 0.02, z) for x in xs], 0.017, pal.Chrome, seg=6)
    for z in (0.5775, 0.6325, 0.6875):
        s.sweep([(x, _edge(TUB_R, z, x, EF_TUB, ER_TUB, -1) - 0.008, z) for x in xs], 0.026, pal.Enamel_Ink, seg=6)
    for sg in (-1, 1):
        x = sg * 0.72
        lamp(s, (x, _edge(TUB_R, 0.74, x, EF_TUB, ER_TUB, -1), 0.74), (sg * 0.35, -1, 0), 0.09, depth=0.05, seg=10)


def _doors(s):
    """Four doors: ink hairlines down the flank under the canopy's pillars (above the arches, below the beltline) and a chrome handle on each."""
    for sg in (-1, 1):
        for y in (-0.72, 0.30, 0.95):
            s.sweep([(sg * (lerp_rows(TUB_R, z, 1) + 0.003), y, z) for z in (0.44, 0.60, 0.72, 0.80)], 0.006, pal.Enamel_Ink, seg=4)
        for y in (-0.10, 0.62):
            x = sg * (lerp_rows(TUB_R, 0.75, 1) + 0.008)
            s.rbox((x - 0.012, y - 0.08, 0.735), (x + 0.012, y + 0.08, 0.765), 0.008, pal.Chrome, seg=1)


def _stripes(s):
    for a, b in ((0.07, 0.19), (-0.19, -0.07)):
        s.rbox((a, -2.30, 0.978), (b, -1.12, 0.99), 0.004, pal.Enamel_Pearl, seg=1)


def _arches(t):
    """Wheel wells: a cylinder along X at each wheel, a hand's width wider than the tyre; the cut faces take the cutter's dark rubber."""
    for sg in (-1, 1):
        for y in WHEELS_Y:
            with t.at((0.56 if sg > 0 else -1.08, y, 0.34), (0, 90, 0)):
                t.lathe([(0.40, 0), (0.40, 0.52)], pal.Rubber, seg=32)


def _rival(s):
    def tub(t):  # the body in plum, ink sills; the arches are cut out of it
        mesh(t, [[(x, y, r[0]) for x, y in _plan(r[1], r[2], r[3], r[4], r[5], SIDES, 16, EF_TUB, ER_TUB)] for r in TUB_R], pal.Enamel_Plum,
             paint=lambda i, j, c: pal.Enamel_Ink if c.z < 0.4 else None)
    carve(s, tub, _arches)
    _canopy(s, _canopy_rows(), z_roof=Z0 + 0.56 * math.sin(math.radians(50)))
    _fins(s)
    for sg in (-1, 1):  # the beltline, above the arches, on the flank
        z = 0.815
        hw = lerp_rows(TUB_R, z, 1) + 0.006
        s.sweep([(sg * hw, -1.45, z), (sg * hw, 0.2, z), (sg * hw, 1.8, z + 0.02)], 0.012, pal.Chrome, seg=4, caps=False)
    _doors(s)
    _wrap(s, 0.40, +1, 0.06, 0.012, pal.Chrome)  # the rear bumper, wrapping the tail
    _nose(s)
    _stripes(s)
    for sg in (-1, 1):
        for y in WHEELS_Y:
            wheel(s, (sg * 0.84, y, 0.34), 0.34, 0.2, white=(0.21, 0.29), hub=0.5, rim=pal.Steel, seg=24, side=sg)  # smoother than the others' 14


@asset("Sedan_Rival", "Vehicles", bevel=0.0)
def sedan_rival(s):
    """The rival crew's car (brief 067): plum torpedo body, ink-lacquer bubble roof, pearl stripes, swept fins."""
    with s.at((0, 0, 0), (0, 0, 0), scale=0.92):  # the same 5.1 m model and the same 0.92 as cars020.sedan
        _rival(s)


REGISTRY["Sedan_Rival"]["nolod"] = True  # full detail, no decimated levels (export_kit reads it; precedent: WashingtoniaPalm_Street, AtomFountain_Testbed)
