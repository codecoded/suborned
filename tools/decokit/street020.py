"""Street pieces cut to the plan's slots (brief 020 follow-ups, Splice's adoption notes). The adopted and candidate pieces were drawn at their own sizes; the world's slots are the plan's:
  FanPalm_Play        FanPalm with about a quarter of the triangles (a leaf is a 2 x 3 card, 20 leaves, a 4-band trunk): 4.2k to about 1k. Same crown size and pivot.
  Planter_Urn_Play    Planter_Urn with a 10-sided urn and 14 foliage cards instead of 64: 3.6k to about 1k. Same size and pivot.
  PneumaticPost_Play  the tube-mail post cut to the plan's mailbox, 0.6 x 0.6 x 1.3 m (the post is 2.34 m with its chrome stack): the red pillar with a rocket-nose cap, plaque,
                      capsule hatch and an amber beacon on top; no stack, no return pipe.
  PhoneBooth_Single   one Googie capsule, 0.91 x 0.91 x 2.5 m (the plan's K6 kiosk; the adopted PhoneBooth is a 2.58 m twin): coral drum, glazed cabin in chrome mullions with a
                      seafoam hood and a lit lamp inside, ivory domed cap, chrome starburst finial.
  Newsstand           3.0 x 1.5 x 2.8 m (the plan's newsstand; CurvedKiosk is 6.1 x 4.4 x 6.5 and was scaled to 0.43): a stadium-plan kiosk in the CurvedKiosk's ivory, royal blue,
                      mustard and chrome: glazed on three sides, a counter with a paper rack, speed bands, a mustard fin to 2.8 m.
Pivot = footprint centre on the ground, front -Y (as the kit's Furnish)."""

import math
import random

from mathutils import Vector

from booths import starburst as booth_starburst
from furnish_civic import band, post_body
from furnish_garden import disc
from furnish_parts import FRONT, Z, flat, frame, knob, lamp, letters, mound, strip
from geo import dome, fillet, rrect, stadium
from kit import asset
from mats import pal
from plants import FAN_HUB, FAN_STEM, Cards, rings_sweep, rot
from structures import ccw, sec, tube


# ----------------------------------------------------------------- FanPalm_Play

def _fan_leaf(cd, rng, base, d, D, crown_c, crown_r, mat, droop, cup=0.12):
    """plants.fan_leaf on a 3 x 4 vertex grid (6 quads) instead of 5 x 5 (16): the same card, the same droop at the corners."""
    t = d.normalized()
    b = t.cross(Z) if abs(t.z) < 0.98 else Vector((0, -1, 0))
    b.normalize()
    n0 = b.cross(t)
    r = rng.uniform(-25, 25)
    n0, b = rot(n0, t, r), rot(b, t, r)
    cc = Vector(crown_c)
    us, vs = (0.0, 0.5, 1.0), (0.0, FAN_HUB, 0.62, 1.0)
    P, UV, N = [], [], []
    for v in vs:
        rp, rn = [], []
        for u in us:
            x, y = (u - 0.5) * D, (v - FAN_STEM) * D
            rho = math.hypot(x, y - (FAN_HUB - FAN_STEM) * D) / D
            q = base + b * x + t * y + n0 * (cup * abs(x) - droop * rho * rho * D)
            rp.append(q)
            rn.append((n0 * 0.6 + (q - cc) / crown_r * 0.4 + Z * 0.25).normalized())
        P.append(rp)
        N.append(rn)
        UV.append([(u, v) for u in us])
    cd.grid(P, UV, N, mat)


@asset("FanPalm_Play", "Plants", bevel=0.0)
def fan_palm_play(s):
    rng = random.Random(4202)
    cd = Cards(s)
    H = 1.0
    lean = lambda z: (0.06 * (z / H) ** 2, 0.0, z)  # noqa: E731
    rad = lambda z: 0.13 + 0.05 * math.exp(-z / 0.2) - 0.02 * z / H  # noqa: E731
    rings_sweep(s, H, rad, pal.Bark_Fibre, lean, seg=8, band=0.25, depth=0.09)
    top = Vector(lean(H))
    with s.at(top):
        s.lathe(dome(0.16, -0.05, 0.2, n=2), pal.Bark_Fibre, seg=8)
    cc, cr = top + Z * 0.35, 1.2
    n = 24
    for i in range(n):
        yaw = math.radians(i * 137.5 + rng.uniform(-10, 10))
        el = 88 - 76 * (i / (n - 1)) ** 1.1 + rng.uniform(-6, 6)
        L = rng.uniform(0.5, 0.7) * (0.8 if el < 30 else 1.0)
        o = Vector((math.cos(yaw), math.sin(yaw), 0))
        p0 = top + Z * 0.08 + o * 0.05
        pts, p = [p0], p0
        for k in range(1, 3):
            e = math.radians(el + 8 - 22 * k / 2)
            p = p + (o * math.cos(e) + Z * math.sin(e)) * L / 2
            pts.append(p)
        s.sweep(pts, [0.014, 0.011, 0.008], pal.Petiole, seg=4)
        d = (pts[-1] - pts[-2]).normalized()
        d = (d - Z * 0.15).normalized()
        _fan_leaf(cd, rng, pts[-1] - d * 0.03, d, rng.uniform(0.74, 0.9) * 1.1, cc, cr, pal.Leaf_PalmFan, rng.uniform(0.18, 0.3))


# ----------------------------------------------------------------- Planter_Urn_Play

@asset("Planter_Urn_Play", "Furnish")
def planter_urn_play(s):
    rng = random.Random(5101)
    cd = Cards(s)
    s.box((-0.27, -0.27, 0), (0.27, 0.27, 0.07), pal.Terrazzo_Ivory)
    s.box((-0.215, -0.215, 0.07), (0.215, 0.215, 0.12), pal.Enamel_Teal)
    S = 10
    s.lathe([(0.2, 0.12), (0.17, 0.152), (0.09, 0.235), (0.085, 0.29)], pal.Enamel_Ivory, seg=S, cap_bottom=False, cap_top=False)
    s.lathe([(0.085, 0.29), (0.108, 0.31), (0.085, 0.33)], pal.Gold, seg=S, cap_bottom=False, cap_top=False)
    s.lathe([(0.085, 0.33), (0.195, 0.398), (0.272, 0.5)], pal.Enamel_Teal, seg=S, cap_bottom=False, cap_top=False)
    s.lathe([(0.276, 0.542), (0.296, 0.648), (0.33, 0.7), (0.292, 0.69)], pal.Enamel_Ivory, seg=S, cap_bottom=False, cap_top=False)
    disc(s, 0.293, 0.69, pal.Soil, n=S)
    for sx in (1, -1):  # chrome handles
        s.sweep([(sx * 0.3, 0, 0.66), (sx * 0.405, 0, 0.625), (sx * 0.29, 0, 0.525)], 0.014, pal.Chrome, seg=4, caps=False)
    c = Vector((0, 0, 0.97))
    with s.at(tuple(c)):
        s.lathe([(0.0, -0.22), (0.19, -0.1), (0.22, 0.0), (0.19, 0.1), (0.0, 0.22)], pal.Leaf_CoreShrub, seg=8)
    mound(cd, rng, c, 0.27, pal.Leaf_Shrub, 0.2, per=14, up=0.15)


# ----------------------------------------------------------------- PneumaticPost_Play (the mailbox)

@asset("PneumaticPost_Play", "Furnish")
def pneumatic_post_play(s):
    """The street mailbox: the original tube-mail post (red pillar, plaque, capsule hatch, dial, chute with a capsule, junction head, amber beacon, return pipe) in the
    old bright red, in a 0.6 x 0.6 footprint (the return pipe tucked against the pillar), 2.34 m tall. 20 Oct (Sky, brief 026): the 1.3 m rocket-nose cut lost the character."""
    post_body(s, red=pal.Enamel_RedBright, py=0.255, pr=0.035, pb=0.04)


# ----------------------------------------------------------------- PhoneBooth_Single

@asset("PhoneBooth_Single", "Booths", bevel=0.0)
def phone_booth_single(s):
    """One Googie capsule 0.91 x 0.91 x 2.5 m: coral drum to 0.95, glass cabin 0.95 to 2.18 in eight chrome mullions with a seafoam hood behind (open toward -Y) and a lit
    lamp, ivory cap with a seafoam band, a chrome starburst finial. Front (the opening) -Y."""
    R = 0.455
    s.lathe([(0, 0), (R - 0.02, 0), (R - 0.02, 0.06), (R - 0.05, 0.07)], pal.Rubber, seg=24, cap_top=False)
    s.lathe([(R - 0.05, 0.06), (R - 0.045, 0.2), (R, 0.28), (R, 0.9), (R - 0.01, 0.95)], pal.Enamel_Coral, seg=24, cap_bottom=False, cap_top=False)
    s.lathe([(R - 0.01, 0.95), (R - 0.06, 0.95)], pal.Chrome, seg=24, cap_bottom=False, cap_top=False)
    s.lathe([(R - 0.03, 0.95), (R - 0.03, 2.18)], pal.Glass, seg=24, cap_bottom=False, cap_top=False)
    for k in range(8):
        a = math.radians(45 * k + 22.5)
        if abs(math.sin(a) + 1) < 0.2 and abs(math.cos(a)) < 0.7:  # the opening faces -Y: no mullion across it
            continue
        s.sweep([((R - 0.02) * math.cos(a), (R - 0.02) * math.sin(a), 0.95), ((R - 0.02) * math.cos(a), (R - 0.02) * math.sin(a), 2.18)], 0.02, pal.Chrome, seg=4)
    # hood behind the glass: a seafoam half drum (open toward -Y), a shelf and a lit lamp
    outer = [(0.36 * math.cos(math.radians(a)), 0.36 * math.sin(math.radians(a))) for a in range(15, 166, 15)]
    inner = [(0.31 * math.cos(math.radians(a)), 0.31 * math.sin(math.radians(a))) for a in range(165, 14, -15)]
    s.extrude(outer + inner, 1.0, 2.12, pal.Enamel_Seafoam)
    s.box((-0.2, 0.05, 1.28), (0.2, 0.3, 1.3), pal.Enamel_Teal)
    with s.at((0, 0.3, 1.72)):
        s.lathe([(0.0, 0.0), (0.05, 0.0), (0.05, 0.04), (0.0, 0.05)], pal.LampSoft, seg=8)
    s.lathe([(R - 0.03, 2.18), (R + 0.01, 2.18), (R + 0.01, 2.24), (R - 0.01, 2.24)], pal.Chrome, seg=24, cap_bottom=False, cap_top=False)
    s.lathe([(R + 0.01, 2.24), (R + 0.015, 2.3), (R - 0.04, 2.34)], pal.Enamel_Seafoam, seg=24, cap_bottom=False, cap_top=False)
    s.lathe([(R - 0.04, 2.34), (0.3, 2.4), (0.15, 2.45), (0.0, 2.47)], pal.Enamel_Ivory, seg=24, cap_bottom=False, cap_top=False)
    with s.at((0, 0, 2.47)):
        booth_starburst(s, 0.025, 8, 0.05, pal.Chrome)


# ----------------------------------------------------------------- Newsstand

@asset("Newsstand", "Structures", bevel=0.0)
def newsstand(s):
    """3.0 x 1.5 x 2.8 m: stadium-plan kiosk, chrome plinth, ivory sill band, glass on three sides over a teal interior, an ivory fascia with a royal-blue band and chrome speed
    bands, a mustard fin to 2.8 m at the right end, a counter ledge and a paper rack on the front (-Y). The back wall is closed (the vendor's door)."""
    W, D = 3.0, 1.5
    plan = ccw(stadium(W, D, 4))

    def inset(d):
        return [(x * (W / 2 - d) / (W / 2) if abs(x) > 0 else x, y * (D / 2 - d) / (D / 2)) for x, y in plan]
    line = [(x, y, 0.0) for x, y in plan]
    s.extrude(inset(0.1), 0.0, 2.4, pal.Enamel_Ivory)
    for secn, mat in ((sec(-0.04, 0.2, 0.0, 0.18), pal.Chrome), (sec(-0.1, 0.12, 0.18, 0.85, 0.05), pal.Enamel_Ivory), (sec(0.02, 0.04, 0.85, 2.12), pal.Glass),
                      (sec(0.05, 0.14, 0.85, 2.12), pal.Enamel_Teal), (sec(-0.04, 0.1, 2.12, 2.4), pal.Enamel_Ivory), (sec(-0.07, 0.08, 2.4, 2.6), pal.Enamel_Royal),
                      (sec(-0.1, 0.1, 2.6, 2.7, 0.03), pal.Enamel_Ivory)):
        tube(s, line + line[:1], secn, mat, closed=True)
    for k in range(2):  # speed bands on the fascia
        s.sweep([(x * 1.0, y * 1.0, 2.27 + 0.07 * k) for x, y in plan + plan[:1]], 0.012, pal.Chrome, seg=4, caps=False)
    n = len(plan)
    for i in range(0, n, 2):  # mullions
        x, y = plan[i]
        if y > 0.3:
            continue
        s.sweep([(x, y, 0.85), (x, y, 2.12)], 0.012, pal.Chrome, seg=4, caps=False)
    s.box((-W / 2 + 0.5, D / 2 - 0.02, 0.0), (-W / 2 + 1.5, D / 2 + 0.06, 2.1), pal.Enamel_Mustard)  # vendor's door, back
    s.box((W / 2 - 0.34, -D / 2 - 0.1, 0.18), (W / 2 - 0.12, D / 2 - 0.05, 2.8), pal.Enamel_Mustard)  # fin
    s.box((W / 2 - 0.36, -D / 2 - 0.12, 0.18), (W / 2 - 0.34, D / 2 - 0.05, 2.78), pal.Chrome)
    # counter ledge and the paper rack on the front
    s.box((-1.2, -D / 2 - 0.3, 0.88), (1.2, -D / 2 - 0.04, 0.94), pal.Chrome)
    rng = random.Random(7020)
    covers = (pal.Enamel_Coral, pal.Enamel_Mint, pal.Enamel_Royal, pal.Enamel_Ivory, pal.Enamel_Mustard, pal.Enamel_Seafoam)
    for i in range(9):  # magazines on a slanted rack
        x = -1.1 + 0.275 * i
        with s.at((x, -D / 2 - 0.2, 0.94), (-20, 0, 0)):
            s.box((-0.1, -0.02, 0.0), (0.1, 0.0, 0.3), covers[rng.randrange(len(covers))])
    s.box((-1.2, -D / 2 - 0.32, 0.5), (1.2, -D / 2 - 0.3, 0.88), pal.Enamel_Royal)  # rack skirt
