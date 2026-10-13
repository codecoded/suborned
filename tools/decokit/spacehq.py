"""SpaceHQ, the flagship (brief 007, Sky 2026-10-13: "crowned with an atom or starburst over a needle mast, not a rocket").
Concepts: AtomicAge/exposition_palace (atom crown over stepped cream massing), power_spire (fluted tiers, gold ribs,
flanking streamline piers, halo gallery), grand_hotel (finial on a mast), monorail_terminal (the station hall).

  tower(s, zb, fin_r)   above the base (drum roof or podium) at zb: four fluted cream limestone tiers with gold ribs and
                        teal glazing slots, stepped setbacks with turquoise speed-band pairs, eight streamline buttress
                        fins from the base out at fin_r climbing to the third tier, two chrome-and-gold galleries, the
                        crown drum, a chrome-and-gold atom (sphere and three orbits) on a needle mast with a rose
                        beacon; tip 252.
  drum_dress(...)       exterior pilasters and ribbon windows on the drum's bearing halves.
  hall(...)             the station hall interior: terrazzo inlays, a ribbed and coffered ceiling with recessed lamps,
                        pilasters with torcheres, a painted mural band, lamp standards.
  portal_wall(...)      the flat portal frontispiece at each end of the hall, recessed into the drum, with the
                        12 x 7.5 arched opening, a chrome reveal with a coral inlay, stepped archivolts both faces.
Kit frame: metres, Z up, pivot = drum centre at street z 0; the sky rail line runs along local Y.
"""

import math

from geo import circle
from mats import accent, pal, register
from mega_parts import lens
from skyport_parts import ball

register("DK_Enamel_Coral17", "enamel", accent("coral"))
register("DK_Enamel_Sunshine", "enamel", accent("sunshine"))
register("DK_Enamel_Turquoise", "enamel", accent("turquoise"))
register("DK_Enamel_Flamingo", "enamel", accent("flamingo"))
register("DK_Window_Teal", "enamel", "#2E6D70")
register("DK_Lamp_Deco", "emit", "#FFF1C4", 5.0)   # warm lamp glass, dE2000 24.6 from guard (DK_Lamp #FFE2A6 is 19.3)
register("DK_Enamel_Mint", "enamel", "#A8D3BE")

TIP = 252.0
TIERS = ((29.0, 78.0, 40), (22.5, 126.0, 32), (16.5, 166.0, 26), (11.5, 190.0, 18))  # (radius, top z, piers)
def _fin_profile(zb, steps, top_r, foot_r):
    """Stepped streamline buttress edge (r, z): from the base flare, vertical runs (r, z_from, z_to) joined by quarter
    rounds where each tier sets back, ending in a round head on the tower face at top_r."""
    pts = []
    r0, za, _ = steps[0]
    for k in range(7):                                    # base flare: a concave quarter from the foot out to r0 + 5.5
        f = k / 6
        pts.append((r0 + (foot_r - r0) * (1 - math.sin(f * math.pi / 2)), zb + 9.0 * (1 - math.cos(f * math.pi / 2))))
    for i, (r, z0, z1) in enumerate(steps):
        if i:
            rp, zp = pts[-1]
            for k in range(1, 7):                         # convex quarter round from the run above the last into this one
                f = k / 6
                pts.append((r + (rp - r) * math.cos(f * math.pi / 2), zp + (z0 - zp) * math.sin(f * math.pi / 2)))
        pts.append((r, z1))
    rp, zp = pts[-1]
    for k in range(1, 7):                                 # the head rounds onto the tower face
        f = k / 6
        pts.append((top_r + (rp - top_r) * math.cos(f * math.pi / 2), zp + 5.0 * math.sin(f * math.pi / 2)))
    return pts


def _rot(s, deg):
    return s.at((0, 0, 0), (0, 0, deg))


def fluted_tier(s, r, z0, z1, n):
    """Cream limestone piers (68 % of the girth) round a teal glazed core; a slim gold rib on every other pier."""
    s.lathe([(r - 0.7, z0), (r - 0.7, z1)], pal.Window_Teal, seg=n * 2, cap_bottom=False, cap_top=False)
    pw = 2 * math.pi * r / n * 0.68
    for k in range(n):
        with _rot(s, 360.0 * k / n):
            s.box((r - 1.1, -pw / 2, z0), (r, pw / 2, z1), pal.Limestone_Tex)
            if k % 2 == 0:
                s.box((r - 0.02, -0.2, z0 + 1.2), (r + 0.22, 0.2, z1 - 3.6), pal.Gold)


def setback(s, r, z, r_next):
    """The terrace on top of a tier: turquoise speed-band pair, gold bead, limestone coping, terrazzo terrace."""
    for za, zb in ((z - 3.4, z - 2.8), (z - 2.3, z - 1.7)):
        s.lathe([(r + 0.18, za), (r + 0.18, zb)], pal.Enamel_Turquoise, seg=96, cap_bottom=False, cap_top=False)
    s.lathe([(r + 0.25, z - 1.35), (r + 0.4, z - 1.2), (r + 0.25, z - 1.05)], pal.Gold, seg=96, cap_bottom=False, cap_top=False)
    s.lathe([(r + 0.35, z - 0.9), (r + 0.35, z), (r_next, z)], pal.Limestone_Tex, seg=96, cap_bottom=True, cap_top=False)
    s.lathe([(r + 0.2, z + 0.02), (r_next + 0.05, z + 0.02)], pal.Terrazzo_Ivory, seg=96, cap_bottom=False, cap_top=False)
    s.lathe([(r + 0.35, z), (r + 0.35, z + 1.1), (r + 0.05, z + 1.1), (r + 0.05, z + 0.02)], pal.Limestone_Tex, seg=96,
            cap_bottom=False, cap_top=False)   # parapet


def fins(s, zb, fin_r):
    """Four full-height stepped buttresses (deco ziggurat piers, streamline quarter rounds at each setback) on the
    diagonals, and four short ones on the first tier between them: limestone blades 3.2 m thick with a gold edge bead
    and a turquoise inlay on both faces. Their outline is the tower's broad shoulders."""
    t1, t2, t3 = TIERS[0][1], TIERS[1][1], TIERS[2][1]
    big = _fin_profile(zb, [(fin_r - 2.5, zb + 9.0, t1 - 7.0), (TIERS[1][0] + 8.0, t1 + 3.0, t2 - 8.0), (TIERS[2][0] + 5.0, t2 + 2.0, t3 - 10.0)], TIERS[2][0] - 1.0, fin_r)
    small = _fin_profile(zb, [(fin_r - 5.0, zb + 9.0, t1 - 14.0)], TIERS[0][0] - 1.0, fin_r - 2.0)
    for i in range(8):
        prof = big if i % 2 == 0 else small
        w = 2.5 if i % 2 == 0 else 1.6
        poly = [(8.0, zb)] + prof + [(8.0, prof[-1][1])]
        inlay = [(r - 1.7, z) for r, z in prof[3:-3]]
        band = inlay + [(r - 0.8, z) for r, z in inlay[::-1]]
        with _rot(s, 45.0 + 45.0 * i), s.at((0, 0, 0), (90, 0, 0)):     # local XY -> world XZ, extrude along -Y
            s.extrude(poly, -w, w, pal.Limestone_Tex)
            s.extrude(band, w, w + 0.06, pal.Enamel_Turquoise)
            s.extrude(band, -w - 0.06, -w, pal.Enamel_Turquoise)
            s.sweep([(r + 0.05, z, 0.0) for r, z in prof[2:]], 0.34, pal.Gold, seg=8)
        with _rot(s, 45.0 + 45.0 * i):
            ball(s, (prof[-1][0] + 0.6, 0.0, prof[-1][1] + 1.2), 1.3 if i % 2 == 0 else 1.0, pal.Gold, seg=16, n=8)


def atom(s, c, R=9.0, a=27.0, b=12.5):
    """Chrome sphere, three gold orbits (ellipses in planes tilted 68 deg, turned 0/60/120), chrome electrons."""
    ball(s, c, R, pal.Chrome, seg=40, n=20)
    s.lathe([(R + 0.05, c[2] - 0.5), (R + 0.35, c[2]), (R + 0.05, c[2] + 0.5)], pal.Gold, seg=48, cap_bottom=False, cap_top=False)
    for j, yaw in enumerate((0.0, 60.0, 120.0)):
        with s.at(c, (0, 0, yaw)), s.at((0, 0, 0), (68, 0, 0)):
            pts = [(a * math.cos(t), b * math.sin(t), 0.0) for t in (2 * math.pi * k / 64 for k in range(65))]
            s.sweep(pts, 1.05, pal.Gold, seg=10, caps=False)
            t = 2 * math.pi * (0.15 + 0.3 * j)
            ball(s, (a * math.cos(t), b * math.sin(t), 0.0), 2.4, pal.Chrome, seg=16, n=8)


def crown(s, z):
    """From the top tier's roof z (190) to the tip 252: stepped crown drum, chrome pedestal, atom, needle mast, beacon."""
    for r0, r1, h in ((9.4, 9.0, 3.0), (7.6, 7.2, 2.8), (5.8, 5.4, 2.6)):
        s.lathe([(r0, z), (r1, z + h), (0.0, z + h)], pal.Limestone_Tex, seg=48, cap_bottom=False)
        s.lathe([(r1 + 0.02, z + h - 0.7), (r1 + 0.2, z + h - 0.45), (r1 + 0.02, z + h - 0.2)], pal.Gold, seg=48, cap_bottom=False, cap_top=False)
        z += h
    zp = z
    s.lathe([(4.2, zp), (2.6, zp + 4.0), (1.7, zp + 7.6), (1.5, zp + 9.0)], pal.Chrome, seg=32, cap_bottom=False, cap_top=False)
    for i in range(12):  # gold pedestal flutes
        a = math.radians(30 * i)
        s.sweep([(4.25 * math.cos(a), 4.25 * math.sin(a), zp), (1.75 * math.cos(a), 1.75 * math.sin(a), zp + 7.6)], 0.16, pal.Gold, seg=6)
    c = (0.0, 0.0, 223.0)
    atom(s, c)
    # needle mast through the sphere: chrome, gold collars, rose beacon (obstruction lamp, never signal red)
    s.lathe([(1.5, zp + 9.0), (1.3, 214.5), (1.0, 232.0), (0.62, 241.0), (0.32, 248.0), (0.1, TIP - 0.4), (0.0, TIP)], pal.Chrome, seg=16, cap_bottom=False)
    for zc, r in ((232.0, 1.0), (237.0, 0.85), (241.0, 0.62)):
        s.lathe([(r, zc - 0.35), (r + 0.45, zc), (r, zc + 0.35)], pal.Gold, seg=16, cap_bottom=False, cap_top=False)
    for k in range(4):  # starburst spikes at the 237 collar: a gold finial star
        with s.at((0, 0, 237.0), (0, 0, 45 * k)):
            for tilt in (90, -90):
                with s.at((0, 0, 0), (tilt, 0, 0)):
                    s.lathe([(0.3, 0.0), (0.12, 2.6), (0.0, 4.2)], pal.Gold, seg=6)
    ball(s, (0.0, 0.0, 245.2), 0.9, pal.Beacon_Rose, seg=16, n=8)


def tower(s, zb, fin_r=38.5):
    """Everything above the base zb (34 on the drum, 5 on the kit podium) to the tip 252."""
    z0 = zb
    for i, (r, z1, n) in enumerate(TIERS):
        fluted_tier(s, r, z0, z1, n)
        if 1 <= i <= 2:  # a mid-tier turquoise band pair on the long tiers
            zm = (z0 + z1) / 2
            for za, zz in ((zm - 0.6, zm), (zm + 0.5, zm + 1.1)):
                s.lathe([(r + 0.18, za), (r + 0.18, zz)], pal.Enamel_Turquoise, seg=96, cap_bottom=False, cap_top=False)
        r_next = TIERS[i + 1][0] if i + 1 < len(TIERS) else 0.0
        setback(s, r, z1, r_next)
        z0 = z1
    # the base collar where the first tier meets the base: gold ring and a limestone plinth step
    s.lathe([(TIERS[0][0] + 1.6, zb), (TIERS[0][0] + 1.6, zb + 2.2), (TIERS[0][0], zb + 2.2)], pal.Limestone_Tex, seg=96, cap_bottom=False, cap_top=False)
    s.lathe([(TIERS[0][0] + 1.62, zb + 1.4), (TIERS[0][0] + 1.62, zb + 1.8)], pal.Gold, seg=96, cap_bottom=False, cap_top=False)
    fins(s, zb, fin_r)
    # gallery 1: the observation deck on the second setback, chrome with gold rims, pierced by the fins
    lens(s, 119.4, TIERS[1][0] - 0.5, 30.0, 6.0, pal.Chrome, bead=pal.Gold, seg=96)
    s.lathe([(30.05, 122.0), (30.05, 123.0)], pal.Enamel_Turquoise, seg=96, cap_bottom=False, cap_top=False)
    # gallery 2: the halo under the crown (power_spire), chrome disc and a gold ring on struts
    lens(s, 184.0, TIERS[3][0] - 0.5, 23.0, 4.2, pal.Chrome, bead=pal.Gold, seg=96)
    s.lathe([(25.6, 193.4), (26.4, 193.8), (25.6, 194.2), (24.8, 193.8)], pal.Gold, seg=96, cap_bottom=False, cap_top=False)
    for k in range(12):
        a = math.radians(30 * k + 15)
        s.sweep([(21.5 * math.cos(a), 21.5 * math.sin(a), 188.2), (25.6 * math.cos(a), 25.6 * math.sin(a), 193.8)], 0.18, pal.Chrome, seg=6)
    crown(s, TIERS[3][1])


# ----------------------------------------------------------------- drum (SpaceHQ_Portal)

def _arc_sector(s, r0, r1, a0, a1, z0, z1, mat, n=None):
    n = n or max(2, int(abs(a1 - a0) / 3))
    o = [(r1 * math.cos(math.radians(a0 + (a1 - a0) * k / n)), r1 * math.sin(math.radians(a0 + (a1 - a0) * k / n))) for k in range(n + 1)]
    i = [(r0 * math.cos(math.radians(a1 - (a1 - a0) * k / n)), r0 * math.sin(math.radians(a1 - (a1 - a0) * k / n))) for k in range(n + 1)]
    s.extrude(o + i, z0, z1, mat, smooth_sides=True)


def drum_dress(s, Rw, Rp, xs, z_top):
    """Pilasters (Rw -> Rp) every 6 deg on both bearing halves (|x| >= xs), ribbon windows between them at the street,
    deck and concourse storeys, a gold capital line. Rw = the wall, Rp = the pilaster face (<= the drum envelope)."""
    a = math.degrees(math.acos(xs / Rp))
    for base in (0.0, 180.0):
        angs = [base - a + 0.8 + k * 6.0 for k in range(int((2 * a - 1.6) // 6.0) + 1)]
        for t in angs:
            with _rot(s, t):
                s.box((Rw - 0.05, -0.7, 0.0), (Rp, 0.7, z_top - 1.0), pal.Limestone_Tex)
                s.box((Rp - 0.02, -0.9, z_top - 1.6), (Rp, 0.9, z_top - 1.0), pal.Gold)
        for t0, t1 in zip(angs, angs[1:]):
            for za, zz in ((1.6, 7.6), (12.2, 20.6), (25.6, 29.2)):
                _arc_sector(s, Rw, Rw + 0.06, t0 + 1.25, t1 - 1.25, za, zz, pal.Window_Teal, n=2)
                _arc_sector(s, Rw, Rw + 0.12, t0 + 1.25, t1 - 1.25, zz, zz + 0.25, pal.Chrome, n=2)  # sill


def portal_wall(s, sy, R, yo, half=12.0, z0=24.0, z1=31.0, rr=6.0, spring=23.0, t=0.8):
    """The flat frontispiece at one end (sy = +1 / -1) of the hall: x -half..half, its outer face at |y| = yo, t thick,
    z0..z1, with the arched opening (clear half width rr, head on the spring) lined in chrome with a coral inlay, and
    stepped archivolts (gold, limestone, coral) on both faces. Built in a frame where local x = world x, local y = world z,
    local z = depth into the hall from the outer face."""
    rl = rr + 0.25                                                       # the lining's outer radius = the wall's cut
    n = 16
    def pt(r, k):
        a = math.pi * k / n
        return (r * math.cos(a), spring + r * math.sin(a))
    rot = (90, 0, 0) if sy > 0 else (90, 0, 180)
    with s.at((0.0, sy * yo, 0.0), rot):                                 # local z 0 -> outer face, t -> hall face
        zc = math.sqrt(rl * rl - (z0 - spring) ** 2)                     # the cut's half width at the wall foot
        for sx in (-1, 1):
            xa, xb = sorted((sx * zc, sx * half))
            s.extrude([(xa, z0), (xb, z0), (xb, z1), (xa, z1)], 0.0, t, pal.Limestone_Tex)  # piers (to the arch foot)
        k0 = next(k for k in range(n + 1) if pt(rl, k)[1] >= z0)
        for k in range(n):                                               # spandrel strips over the arc
            (x0, y0), (x1, y1) = pt(rl, k), pt(rl, k + 1)
            if max(y0, y1) < z0:
                continue
            y0, y1 = max(y0, z0), max(y1, z0)
            q = [(x1, y1), (x0, y0), (x0, z1), (x1, z1)]
            s.extrude(q, 0.0, t, pal.Limestone_Tex)
        for k in range(n):                                               # reveal: chrome / coral inlay / chrome
            (a0, b0), (a1, b1) = pt(rr, k), pt(rr, k + 1)
            (c0, d0), (c1, d1) = pt(rl, k), pt(rl, k + 1)
            q = [(a0, b0), (c0, d0), (c1, d1), (a1, b1)]
            for za, zz, m in ((-0.05, 0.3, pal.Chrome), (0.3, 0.5, pal.Enamel_Coral17), (0.5, t + 0.05, pal.Chrome)):
                s.extrude(q, za, zz, m)
            for face, dz in ((-0.05, -1), (t + 0.05, 1)):                # stepped archivolts, proud of each face
                for ri, ro, m, pr in ((rl, rl + 0.6, pal.Gold, 0.12), (rl + 0.6, rl + 1.3, pal.Limestone_Tex, 0.3), (rl + 1.3, rl + 1.6, pal.Enamel_Coral17, 0.18)):
                    (e0, f0), (e1, f1) = pt(ri, k), pt(ri, k + 1)
                    (g0, h0), (g1, h1) = pt(ro, k), pt(ro, k + 1)
                    if min(f0, f1, h0, h1) < z0 - 3.0:
                        continue
                    lo, hi = sorted((face, face + dz * pr))
                    s.extrude([(e0, f0), (g0, h0), (g1, h1), (e1, f1)], lo, hi, m)
        for sx in (-1, 1):                                               # jamb linings below the spring
            xa, xb = sorted((sx * rr, sx * rl))
            s.extrude([(xa, spring - 1.5), (xb, spring - 1.5), (xb, spring), (xa, spring)], -0.05, t + 0.05, pal.Chrome)


def _lamp_standard(s, x, y, z):
    """Deco lamp standard 4.6 m: limestone foot, fluted chrome post with gold bands, a globe with a gold saturn ring."""
    with s.at((x, y, z)):
        s.lathe([(0.45, 0.0), (0.45, 0.35), (0.3, 0.5), (0.0, 0.5)], pal.Limestone_Tex, seg=12)
        s.lathe([(0.12, 0.5), (0.09, 4.0)], pal.Chrome, seg=10, cap_bottom=False)
        for zz in (0.9, 2.2, 3.6):
            s.lathe([(0.1, zz - 0.06), (0.16, zz), (0.1, zz + 0.06)], pal.Gold, seg=10, cap_bottom=False, cap_top=False)
        ball(s, (0.0, 0.0, 4.25), 0.32, pal.Lamp_Deco, seg=14, n=7)
        s.lathe([(0.36, 4.2), (0.55, 4.25), (0.36, 4.3)], pal.Gold, seg=16)


def _torchere(s, sd, y, z, xw):
    """Wall uplighter on a pilaster: gold bracket, fluted bowl, glowing lamp glass at the rim."""
    with s.at((sd * (xw - 0.45), y, z)):
        s.box((min(0.0, sd * 0.47), -0.08, -0.5), (max(0.0, sd * 0.47), 0.08, -0.34), pal.Gold)
        s.box((-0.08, -0.08, -0.5), (0.08, 0.08, -0.05), pal.Gold)
        s.lathe([(0.08, -0.6), (0.38, -0.05), (0.42, 0.0)], pal.Gold, seg=12, cap_bottom=True, cap_top=False)
        s.lathe([(0.4, -0.02), (0.0, -0.02)], pal.Lamp_Deco, seg=12, cap_bottom=False, cap_top=False)


def _mural(s, sd, yc, w, z0, z1, xw, motif):
    """One mural panel on the hall wall (face at |x| = xw), procedural: a turquoise sky, and a sunburst, a ringed planet
    or a rocket's coral trail with its sunshine star, by motif. Built in the wall plane (local x = along y, local y = z)."""
    rot = (90, 0, 90) if sd < 0 else (90, 0, -90)
    loc = (sd * xw, yc, 0.0)
    with s.at(loc, rot):
        # local x along the wall, local y = world z, local z = toward the hall on both walls
        def lay(poly, d0, d1, m):
            s.extrude(poly, d0, d1, m)
        lay([(-w / 2, z0), (w / 2, z0), (w / 2, z1), (-w / 2, z1)], -0.0, 0.05, pal.Enamel_Turquoise)
        zm, h = (z0 + z1) / 2, z1 - z0
        if motif == 0:   # sunburst: sunshine disc and gold rays
            lay(circle(h * 0.22, 16, 0.0, zm), 0.05, 0.08, pal.Enamel_Sunshine)
            for k in range(12):
                a = 2 * math.pi * k / 12
                ux, uy = math.cos(a), math.sin(a)
                p = [(ux * h * 0.26 - uy * 0.06, zm + uy * h * 0.26 + ux * 0.06), (ux * h * 0.45, zm + uy * h * 0.45),
                     (ux * h * 0.26 + uy * 0.06, zm + uy * h * 0.26 - ux * 0.06)]
                lay(p if (p[1][0] - p[0][0]) * (p[2][1] - p[0][1]) - (p[1][1] - p[0][1]) * (p[2][0] - p[0][0]) > 0 else p[::-1], 0.05, 0.07, pal.Gold)
        elif motif == 1:  # ringed planet: flamingo globe, gold ring
            lay(circle(h * 0.24, 16, -w * 0.12, zm), 0.05, 0.08, pal.Enamel_Flamingo)
            ring = [(-w * 0.12 + h * 0.5 * math.cos(2 * math.pi * k / 20), zm + h * 0.1 * math.sin(2 * math.pi * k / 20)) for k in range(20)]
            lay(ring, 0.08, 0.1, pal.Gold)
            lay(circle(h * 0.06, 8, w * 0.3, zm + h * 0.25), 0.05, 0.08, pal.Enamel_Sunshine)
        else:             # rocket trail: a coral chevron climbing to a sunshine star
            for k in range(3):
                x0 = -w * 0.38 + k * w * 0.18
                lay([(x0, z0 + h * 0.2 + k * h * 0.12), (x0 + w * 0.16, z0 + h * 0.28 + k * h * 0.12),
                     (x0 + w * 0.16, z0 + h * 0.4 + k * h * 0.12), (x0, z0 + h * 0.32 + k * h * 0.12)], 0.05, 0.08, pal.Enamel_Coral17)
            cx, cy = w * 0.3, z0 + h * 0.7
            star = []
            for k in range(10):
                rr = h * (0.22 if k % 2 == 0 else 0.08)
                a = math.pi / 2 + 2 * math.pi * k / 10
                star.append((cx + rr * math.cos(a), cy + rr * math.sin(a)))
            for k in range(10):  # convex fan triangles from the star centre
                a, b = star[k], star[(k + 1) % 10]
                lay([(cx, cy), a, b] if (a[0] - cx) * (b[1] - cy) - (a[1] - cy) * (b[0] - cx) > 0 else [(cx, cy), b, a], 0.05, 0.08, pal.Enamel_Sunshine)
        lay([(-w / 2, z1), (w / 2, z1), (w / 2, z1 + 0.18), (-w / 2, z1 + 0.18)], -0.0, 0.12, pal.Gold)     # gold frame
        lay([(-w / 2, z0 - 0.18), (w / 2, z0 - 0.18), (w / 2, z0), (-w / 2, z0)], -0.0, 0.12, pal.Gold)


def hall(s, R, xs, xt, ya, z_floor=24.0, soffit=30.0, z_roof=31.0, y_end=37.0):
    """The station hall between the bearing halves (|x| < xs), floor z_floor, ribs down to the soffit, coffer backs at
    z_roof. Clear: nothing inside |x| < 16 between the floor and the soffit except the platforms (plan portal.station)."""
    # terrazzo inlays: a coral line at the clear edge, gold-and-turquoise rosettes every 9 m on the departure floor
    for sd in (1, -1):
        for xa in (16.0, 16.4):
            s.box((min(sd * xa, sd * (xa + 0.14)), -ya + 2.0, z_floor), (max(sd * xa, sd * (xa + 0.14)), ya - 2.0, z_floor + 0.012), pal.Enamel_Coral17)
        for k in range(-3, 4):
            cx, cy = sd * 12.0, 9.0 * k
            s.extrude(circle(1.2, 24, cx, cy), z_floor, z_floor + 0.012, pal.Enamel_Turquoise)
            for j in range(8):
                a = 2 * math.pi * j / 8
                ux, uy = math.cos(a), math.sin(a)
                tri = [(cx + ux * 1.2 - uy * 0.35, cy + uy * 1.2 + ux * 0.35), (cx + ux * 1.2 + uy * 0.35, cy + uy * 1.2 - ux * 0.35),
                       (cx + ux * 2.6, cy + uy * 2.6)]
                s.extrude(tri, z_floor, z_floor + 0.014, pal.Gold)
            if k < 3:
                _lamp_standard(s, sd * 18.0, cy + 4.5, z_floor)
    # wall dressing on the slot faces |x| = xs: dado, pilasters with gold capitals and torcheres, mural band, cornice
    for sd in (1, -1):
        xw = xs
        s.box((min(sd * xw, sd * (xw - 0.06)), -ya, z_floor), (max(sd * xw, sd * (xw - 0.06)), ya, z_floor + 1.1), pal.Enamel_Teal)
        s.box((min(sd * xw, sd * (xw - 0.1)), -ya, z_floor + 1.1), (max(sd * xw, sd * (xw - 0.1)), ya, z_floor + 1.25), pal.Gold)
        s.box((min(sd * xw, sd * (xw - 0.25)), -ya, soffit - 0.45), (max(sd * xw, sd * (xw - 0.25)), ya, soffit - 0.2), pal.Gold)
        ys = [6.0 * k for k in range(-5, 6)]
        for y in ys:
            s.box((min(sd * xw, sd * (xw - 0.55)), y - 0.5, z_floor), (max(sd * xw, sd * (xw - 0.55)), y + 0.5, soffit - 0.45), pal.Limestone_Tex)
            for dx in (-0.25, 0.0, 0.25):  # flutes
                s.box((min(sd * (xw - 0.55), sd * (xw - 0.6)), y + dx - 0.06, z_floor + 1.3), (max(sd * (xw - 0.55), sd * (xw - 0.6)), y + dx + 0.06, soffit - 1.2), pal.Chrome)
            s.box((min(sd * xw, sd * (xw - 0.75)), y - 0.65, soffit - 1.2), (max(sd * xw, sd * (xw - 0.75)), y + 0.65, soffit - 0.45), pal.Gold)
            _torchere(s, sd, y, z_floor + 3.0, xw - 0.55)
        for i, (y0, y1) in enumerate(zip(ys, ys[1:])):
            _mural(s, sd, (y0 + y1) / 2, (y1 - y0) - 1.3, z_floor + 3.3, soffit - 0.7, xw, (i + (0 if sd > 0 else 1)) % 3)
    # ribbed, coffered ceiling: ribs z soffit..z_roof, mint coffer backs, a recessed lamp in a gold ring per coffer
    xr = [-xs, -12.3, -xt, xt, 12.3, xs]
    yr = [4.5 * k for k in range(-7, 8)]
    for x in xr:
        yl = math.sqrt(max(0.0, (R - 1.0) ** 2 - x * x))
        s.box((x - 0.35, -yl, soffit), (x + 0.35, yl, z_roof), pal.Limestone_Tex)
        s.box((x - 0.4, -yl, soffit - 0.02), (x + 0.4, yl, soffit), pal.Gold)
    for y in yr:
        s.box((-xs, y - 0.3, soffit + 0.1), (xs, y + 0.3, z_roof), pal.Limestone_Tex)
        s.box((-xs, y - 0.33, soffit + 0.08), (xs, y + 0.33, soffit + 0.1), pal.Gold)
    for (x0, x1) in zip(xr, xr[1:]):
        for (y0, y1) in zip(yr, yr[1:]):
            s.box((x0 + 0.35, y0 + 0.3, z_roof - 0.03), (x1 - 0.35, y1 - 0.3, z_roof - 0.01), pal.Enamel_Mint)
            cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
            with s.at((cx, cy, 0.0)):
                s.lathe([(0.0, z_roof - 0.22), (0.62, z_roof - 0.22), (0.62, z_roof - 0.04)], pal.Lamp_Deco, seg=16, cap_bottom=False, cap_top=False)
                s.lathe([(0.9, z_roof - 0.04), (0.9, z_roof - 0.12), (0.62, z_roof - 0.24)], pal.Gold, seg=16, cap_bottom=False, cap_top=False)
