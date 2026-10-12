"""Deco Kit high-rise family (Concepts/Blocks/needle_trio.png, mixed_skyline.png): the towers that rise off a
block's podium deck and give the city back its grandeur. Pivot = base centre on the deck (z 0), front -Y, no bevel.
  Tower_Atom       14.5 m square, rounded, fluted; setbacks at 40 and 76 m (planted); a stepped drum, a fluted chrome
                   pedestal and a chrome nucleus in three tilted orbits, 118 m.
  Tower_Starburst  17 x 16 m, the tallest: four tiers stepping back toward the rear, so the first front terrace takes
                   a pool; a gold ziggurat crown throwing a starburst of spikes and a chrome needle, 184 m.
  Tower_Lantern    16 m square, deep rounded corners, coral banding; an observation gallery, a teal glazed lantern
                   with chrome mullions, a teal dome with gold ribs and a gold finial, 138 m.
  Tower_Needle     (saucer_spires) Googie spire: a flared foot, an 11 m fluted round shaft ringed every 16 m, three
                   coral fins, a saucer restaurant with a glazed band at 118 m, a smaller saucer, a chrome needle; 176 m.
  Tower_Terrace    (hanging_terraces) 24 x 22 m ziggurat of four planted terraces, greenery spilling over each
                   parapet, a slender shaft above with a gold-domed belvedere; 112 m.
  StreamBay_4 / StreamEnd_4 / StreamCorner_4, PortholeBay_4, BalconyBay_4   the podium: the frontage at 4 floors over
                   the shop, flat roofed for the landscaped deck DecoKitHighrise lays over the block.
Each tier: a limestone body on a rounded plan, continuous teal glazed channels between proud piers (a slim dark
line across each floor). Each setback is a terrace: teal and coral bands, a parapet, terrazzo, planters along the
edge, a pool with loungers where the step is deep enough.
"""

import math

from facades import balcony_bay, porthole_bay, stream_corner
from geo import circle, dome, ring, rrect
from kit import asset
from landmarks import fluted_shaft
from mats import pal, register
from mega_parts import lens
from plaza_parts import lift
from roofdeck import ball, lounger, parasol, planter, pool
from skyline import spire, stream_bay, stream_end
from structures import ccw, sec, tube

register("DK_Window_Teal", "enamel", "#2E6D70")  # the towers' glazed channels: teal, deeper than the faience

FH = 3.6  # tower floor height


# ----------------------------------------------------------------- tiers and terraces

def tier(s, plan, z0, z1, pitch=2.2, cw=1.0):
    """plan = (w, d, r, cy): the fluted shaft from z0 to z1."""
    w, d, r, cy = plan
    body = pal.Limestone_Tex
    with s.at((0.0, cy, 0.0)):
        s.extrude(rrect(w, d, r), z0, z1, body)
        for rot, half, off in ((0, w / 2 - r, d / 2), (90, d / 2 - r, w / 2), (180, w / 2 - r, d / 2),
                               (270, d / 2 - r, w / 2)):
            k = max(1, round(2 * half / pitch))
            step = 2 * half / k
            with s.at((0, 0, 0), (0, 0, rot)):
                for i in range(k + 1):  # a pier on every joint; the end ones where the corner round begins
                    x = -half + i * step
                    s.box((x - 0.22, -off - 0.3, z0), (x + 0.22, -off + 0.01, z1), body)
                for i in range(k):
                    x = -half + (i + 0.5) * step
                    s.box((x - cw / 2, -off - 0.05, z0 + 0.6), (x + cw / 2, -off + 0.01, z1 - 0.6), pal.Window_Teal)
                    z = z0 + FH
                    while z < z1 - 1.0:  # a slim dark floor line: the channel still reads as one teal stripe
                        s.box((x - cw / 2, -off - 0.07, z - 0.12), (x + cw / 2, -off - 0.04, z), pal.Window_Tower)
                        z += FH


def band(s, plan, z0, z1, mat, out=0.35):
    w, d, r, cy = plan
    with s.at((0.0, cy, 0.0)):
        s.extrude(rrect(w + 2 * out, d + 2 * out, r + out), z0, z1, mat)


def terrace(s, lo, hi, z):
    """The setback on top of tier lo, round tier hi (None: the roof under a crown)."""
    w, d, r, cy = lo
    band(s, lo, z - 1.7, z - 1.2, pal.Enamel_Teal)
    band(s, lo, z - 1.0, z - 0.3, pal.Enamel_Coral)
    with s.at((0.0, cy, 0.0)):
        tube(s, lift(rrect(w + 0.7, d + 0.7, r + 0.35)), sec(0.0, 0.45, z, z + 1.1), pal.Limestone_Tex, closed=True)
        s.extrude(rrect(w, d, r), z, z + 0.06, pal.Terrazzo_Ivory)
    if hi is None:
        return
    hw, hd, _, hcy = hi
    zt = z + 0.06
    gaps = {  # clear depth between the parapet and the tier above, per side
        "front": (hcy - hd / 2) - (cy - d / 2) - 0.45, "back": (cy + d / 2) - (hcy + hd / 2) - 0.45,
        "side": (w - hw) / 2 - 0.45,
    }
    if gaps["front"] >= 4.4:  # a pool on the deep front step, loungers either end
        L, W = min(hw - 3.0, 10.0), gaps["front"] - 2.6
        y = cy - d / 2 + 0.45 + 1.0 + W / 2
        pool(s, L, W, 0.0, y, zt)
        for side in (-1, 1):
            lounger(s, side * (L / 2 + 1.3), y - 0.4, zt, 90 * side)
        parasol(s, -(L / 2 + 1.3), y + 1.4, zt)
    elif gaps["front"] >= 1.4:
        edge(s, w, cy - d / 2 + 0.45 + 0.6, hw, zt, 0)
    if gaps["back"] >= 1.4:
        edge(s, w, cy + d / 2 - 0.45 - 0.6, hw, zt, 0)
    if gaps["side"] >= 1.4:
        for sx in (-1, 1):
            edge(s, d, sx * (w / 2 - 0.45 - 0.6), hd, zt, 90, cy)


def edge(s, span, at, clear, z, rot, cy=0.0):
    """Planters along one terrace edge (span long, `at` in from the centre line), leaving the corners' rounds."""
    n = max(1, int((span - 6.0) // 4.0))
    for i in range(n):
        u = -(n - 1) * 2.0 + i * 4.0
        if rot == 0:
            planter(s, u, at, z, l=3.0, w=1.0)
        else:
            planter(s, at, cy + u, z, l=3.0, w=1.0, yaw=90)


def base(s, plan, canopy=1.0):
    """Lobby plinth on the deck: dark stone base course, a 1 m ivory canopy with a chrome nose at the lobby head
    (canopy = how far it stands out; a regen order's footprint trims it)."""
    w, d, r, cy = plan
    with s.at((0.0, cy, 0.0)):
        P = lift(rrect(w + 0.6, d + 0.6, r + 0.3))
        tube(s, P, sec(-0.05, 0.3, 0.0, 0.45, 0.02), pal.Stone_Dark, closed=True)
        tube(s, P, sec(-canopy, 0.0, 4.2, 4.4, 0.05), pal.Enamel_Ivory, closed=True)
        tube(s, lift(rrect(w + 2 * canopy + 0.6, d + 2 * canopy + 0.6, r + canopy + 0.3)), sec(0.0, 0.05, 4.24, 4.36), pal.Chrome, closed=True)


def stack(s, tiers, canopy=1.0):
    """tiers = [(plan, z0, z1)] bottom up: the base, each tier and the terrace on it."""
    base(s, tiers[0][0], canopy)
    for i, (plan, z0, z1) in enumerate(tiers):
        tier(s, plan, z0, z1)
        terrace(s, plan, tiers[i + 1][0] if i + 1 < len(tiers) else None, z1)


# ----------------------------------------------------------------- crowns

def orbit(s, c, a, b, tilt, yaw, mat, k=1.0):
    """A closed elliptical orbit (semi-axes a, b) about c, in a vertical plane tilted `tilt` about Y, turned `yaw`."""
    with s.at(c, (0, tilt, yaw)):
        pts = [(a * math.cos(t), 0.0, b * math.sin(t)) for t in [2 * math.pi * k / 48 for k in range(49)]]
        s.sweep(pts, 0.28 * k, mat, seg=10, caps=False)
        ball(s, (a, 0.0, 0.0), 0.75 * k, mat, seg=12)


def atom_crown(s, z, k=1.0):
    """k scales every length of the crown (a regen order's crown height); k = 1 is the kit's 26.9 m."""
    for r0, r1, h in ((4.2, 3.9, 2.6), (3.4, 3.0, 2.4), (2.6, 2.2, 2.2)):  # stepped drum, gold lips
        r0, r1, h = r0 * k, r1 * k, h * k
        s.lathe([(r0, z), (r1, z + h), (0.0, z + h)], pal.Limestone_Tex, seg=32, cap_bottom=False)
        s.lathe(ring(r1 + 0.02, r1 + 0.1, z + h - 0.5 * k, z + h - 0.2 * k), pal.Gold, seg=32, cap_bottom=False, cap_top=False)
        z += h
    s.lathe([(1.9 * k, z), (1.2 * k, z + 3.0 * k), (0.5 * k, z + 7.0 * k), (0.8 * k, z + 7.6 * k), (0.0, z + 7.9 * k)],
            pal.Chrome, seg=24)
    for i in range(8):  # pedestal flutes
        a = math.radians(45 * i)
        s.sweep([(1.95 * k * math.cos(a), 1.95 * k * math.sin(a), z), (0.55 * k * math.cos(a), 0.55 * k * math.sin(a), z + 7.0 * k)],
                0.1 * k, pal.Gold, seg=6)
    c = (0.0, 0.0, z + 11.5 * k)
    ball(s, c, 3.0 * k, pal.Chrome, seg=32, n=12)
    for tilt, yaw in ((0, 0), (60, 60), (-60, -60)):
        orbit(s, c, 8.6 * k, 3.3 * k, tilt, yaw, pal.Chrome, k)


def starburst_crown(s, z, w, d):
    for f, h in ((0.92, 3.0), (0.72, 3.0), (0.52, 2.6), (0.34, 2.4)):  # gold ziggurat, ivory reveals
        s.extrude(rrect(w * f, d * f, 0.6), z, z + h, pal.Gold)
        s.extrude(rrect(w * f + 0.24, d * f + 0.24, 0.7), z + h - 0.4, z + h - 0.15, pal.Enamel_Ivory)
        z += h
    c = z + 1.6
    ball(s, (0.0, 0.0, c), 1.8, pal.Gold, seg=24)
    for tilt, n, L in ((22, 8, 12.0), (46, 12, 9.5), (70, 16, 7.0), (94, 16, 4.8)):  # the starburst
        for i in range(n):
            with s.at((0.0, 0.0, c), (tilt, 0, 360.0 * i / n + tilt)):
                s.lathe([(0.5, 0.0), (0.28, L * 0.55), (0.0, L)], pal.Gold, seg=8)
    spire(s, c + 1.2, 26.0, 0.9)


def lantern_crown(s, z, finial=16.0):
    """finial = the needle on the dome (16 m from the dome base; a regen order's tip cuts it)."""
    lens(s, z, 3.0, 9.4, 1.6, pal.Enamel_Ivory)  # observation gallery
    s.lathe(ring(9.1, 9.18, z + 1.6, z + 2.7), pal.Chrome, seg=48, cap_bottom=False, cap_top=False)
    z += 1.6
    oct_ = circle(5.3, n=8)
    s.extrude(oct_, z, z + 8.0, pal.Window_Teal)
    for x, y in oct_:  # chrome mullions on the corners, a gold rim over the glass
        with s.at((x, y, 0.0), (0, 0, math.degrees(math.atan2(y, x)))):
            s.box((-0.1, -0.22, z), (0.28, 0.22, z + 8.0), pal.Chrome)
    s.extrude(circle(5.7, n=8), z + 8.0, z + 8.7, pal.Gold)
    z += 8.7
    s.lathe(dome(5.4, z, 6.2, n=10), pal.Enamel_Teal, seg=32)
    for k in range(8):
        a = math.radians(22.5 + 45 * k)
        s.sweep([(rr * 1.01 * math.cos(a), rr * 1.01 * math.sin(a), zz) for rr, zz in dome(5.4, z, 6.2, n=8)],
                0.12, pal.Gold, seg=6)
    ball(s, (0.0, 0.0, z + 6.8), 0.9, pal.Gold, seg=16)
    s.lathe([(0.35, z + 7.5), (0.0, z + finial)], pal.Gold, seg=12)


# ----------------------------------------------------------------- towers

@asset("Tower_Atom", "Highrise", bevel=0.0)
def tower_atom(s):
    stack(s, [((14.5, 14.5, 3.4, 0.0), 0.0, 40.0), ((10.5, 10.5, 2.8, 0.0), 40.0, 76.0),
              ((8.6, 8.6, 2.4, 0.0), 76.0, 92.0)])
    atom_crown(s, 93.1)


@asset("Tower_Starburst", "Highrise", bevel=0.0)
def tower_starburst(s):
    top = (8.6, 6.8, 1.5, 3.4)
    stack(s, [((17.0, 16.0, 2.0, 0.0), 0.0, 54.0), ((13.5, 10.5, 2.0, 2.6), 54.0, 100.0),
              ((11.0, 8.5, 1.8, 3.2), 100.0, 132.0), (top, 132.0, 146.0)])
    with s.at((0.0, top[3], 0.0)):
        starburst_crown(s, 147.1, top[0], top[1])


@asset("Tower_Lantern", "Highrise", bevel=0.0)
def tower_lantern(s):
    t1, t2, t3 = (16.0, 16.0, 4.2, 0.0), (12.0, 12.0, 3.6, 0.0), (10.8, 10.8, 3.4, 0.0)
    stack(s, [(t1, 0.0, 46.0), (t2, 46.0, 92.0), (t3, 92.0, 112.0)])
    for plan, z in ((t1, 23.0), (t2, 69.0)):  # coral banding mid-tier
        band(s, plan, z, z + 0.7, pal.Enamel_Coral, out=0.33)
    lantern_crown(s, 113.1)


@asset("Tower_Needle", "Highrise", bevel=0.0)
def tower_needle(s):
    R, top = 5.5, 118.0
    s.lathe([(8.2, 0.0), (8.2, 1.2), (7.4, 3.0), (6.2, 9.0), (R, 14.0)], pal.Limestone_Tex, seg=40,
            cap_bottom=False, cap_top=False)
    s.lathe(ring(8.22, 8.3, 1.5, 2.3), pal.Enamel_Teal, seg=40, cap_bottom=False, cap_top=False)
    s.lathe(ring(8.9, 9.0, 4.2, 4.4), pal.Chrome, seg=40, cap_bottom=False, cap_top=False)
    s.lathe([(8.9, 4.2), (8.9, 4.4), (0.0, 4.4)], pal.Enamel_Ivory, seg=40, cap_bottom=False)  # lobby canopy
    fluted_shaft(s, R, 14.0, top, ribs=18)
    for z in range(30, int(top) - 6, 16):
        lens(s, float(z), R + 0.3, R + 1.3, 0.6, pal.Enamel_Ivory, glaze=False, seg=40)
    for k in range(3):  # coral fins, tapering from the foot to 64 m
        with s.at((0.0, 0.0, 0.0), (90, 0, 90 + 120 * k)):
            s.extrude(ccw([(R, 0.0), (R + 4.2, 0.0), (R + 3.0, 20.0), (R + 0.8, 64.0), (R, 64.0)]), -0.35, 0.35,
                      pal.Enamel_Coral)
    lens(s, top, R + 0.2, 17.0, 4.6, pal.Hull_Ivory)  # the saucer restaurant
    s.lathe([(11.0, top + 4.6), (11.0, top + 6.2), (0.0, top + 6.2)], pal.Window_Tower, seg=40, cap_bottom=False)
    lens(s, top + 6.2, 0.5, 12.5, 1.4, pal.Enamel_Teal, glaze=False, seg=40)
    lens(s, top + 12.0, R - 1.0, 9.0, 2.6, pal.Hull_Ivory, seg=40)  # the upper observation saucer
    s.lathe([(R - 1.0, top + 7.6), (R - 1.0, top + 12.0)], pal.Limestone_Tex, seg=24, cap_bottom=False, cap_top=False)
    spire(s, top + 14.6, 43.0, 2.2)


def hanging(s, plan, z, step=2.4):
    """Greenery spilling over a terrace parapet: foliage masses hung round the plan's rim just under z."""
    w, d, r, cy = plan
    P = rrect(w + 1.1, d + 1.1, r + 0.55, n=4, cy=cy)
    ring_ = P + P[:1]
    for (xa, ya), (xb, yb) in zip(ring_, ring_[1:]):
        n = max(1, round(math.hypot(xb - xa, yb - ya) / step))
        for k in range(n):
            t = (k + 0.5) / n
            ball(s, (xa + (xb - xa) * t, ya + (yb - ya) * t, z - 0.9 - 0.5 * (k % 2)), 0.8 + 0.15 * (k % 3),
                 pal.Foliage, seg=8, n=4, squash=1.5)


@asset("Tower_Terrace", "Highrise", bevel=0.0)
def tower_terrace(s):
    t = [(24.0, 22.0, 3.0, 0.0), (19.5, 17.5, 2.6, 0.0), (15.0, 13.0, 2.2, 0.0), (11.0, 9.5, 2.0, 0.0),
         (8.0, 8.0, 2.0, 0.0)]
    zs = [0.0, 14.0, 28.0, 42.0, 56.0, 96.0]
    stack(s, [(t[i], zs[i], zs[i + 1]) for i in range(5)])
    for i in range(4):
        hanging(s, t[i], zs[i + 1])
    z = zs[5] + 1.1  # belvedere: ivory columns, gold dome, finial
    s.lathe([(4.6, z), (4.6, z + 0.4), (0.0, z + 0.4)], pal.Limestone_Tex, seg=24, cap_bottom=False)
    for k in range(8):
        a = math.radians(22.5 + 45 * k)
        with s.at((3.9 * math.cos(a), 3.9 * math.sin(a), z + 0.4)):
            s.lathe([(0.18, 0.0), (0.14, 0.4), (0.14, 4.2), (0.22, 4.4), (0.0, 4.4)], pal.Enamel_Ivory, seg=8)
    s.lathe(ring(3.7, 4.5, z + 4.8, z + 5.4), pal.Enamel_Ivory, seg=24, cap_bottom=False, cap_top=False)
    s.lathe(dome(4.5, z + 5.4, 3.4, n=8), pal.Gold, seg=24)
    ball(s, (0.0, 0.0, z + 9.0), 0.45, pal.Gold)
    s.lathe([(0.15, z + 9.3), (0.0, z + 15.0)], pal.Gold, seg=8)


# ----------------------------------------------------------------- podium frontage (4 floors over the shop)

asset("StreamBay_4", "Facades", bevel=0.0)(stream_bay(4, "Limestone_Tex"))
asset("StreamEnd_4", "Facades", bevel=0.0)(stream_end(4, "Limestone_Tex"))
asset("StreamCorner_4", "Facades", bevel=0.0)(stream_corner(4, roof=False))
asset("PortholeBay_4", "Facades", bevel=0.0)(porthole_bay(4, roof=False))
asset("BalconyBay_4", "Facades", bevel=0.0)(balcony_bay(4, roof=False))
