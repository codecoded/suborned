"""Shared robot parts (robots.py). The robots are characters: faces are glowing dot-matrix displays that read at kerb
distance, dress is bow ties, badges and pennants. Then the mechanism layer that makes them look engineered and cared
for: jointed arms with three-fingered hands, bolted shoulders, rivets, panel seams, ribbed hoses, lamps, hover
saucers, trays of things.
Frames: rot_to(n) puts local +Z along n; for faces looking roughly -Y, local X stays world X and local Y is up."""

import math

from mathutils import Vector

from geo import dome, fillet, thicken
from mats import RobotPal, register

pal = RobotPal()  # enamels as their detailed robot variants (mats.py)
from signals import ball, band, puck
from vehicles_parts import lamp, mesh, rot_to, slab

register("DK_Glow_Seafoam", "emit", "#3FC9A0", 0.7)  # as booths.py (register is first-wins)
register("DK_Glow_Amber", "emit", "#F28A1E", 1.1)  # as booths.py
register("DK_Bakelite", "enamel", "#17181B")  # as booths.py
register("DK_Enamel_Coral", "enamel", "#E07560")  # as skyline.py
register("DK_Fabric_Ivory", "fabric", "#EFE8D6")  # as furnish_parts.py
register("DK_Timber", "stone", "#B48C5E")  # as alleys_props.py
register("DK_Stem", "enamel", "#4F7F3A")  # flower stems, greens
register("DK_Skin", "ceramic", "#E2A988")  # the baby
register("DK_Leather_Black", "ceramic", "#1E1E20")  # as mobility.py: straps, seats


# ----------------------------------------------------------------- surfaces

def profile_rad(prof):
    """r(z) of a lathe profile [(r, z)...] whose z rises, by linear interpolation (clamped at the ends)."""
    def rad(z):
        for (r0, z0), (r1, z1) in zip(prof, prof[1:]):
            if z0 <= z <= z1 and z1 > z0:
                return r0 + (r1 - r0) * (z - z0) / (z1 - z0)
        return prof[0][0] if z < prof[0][1] else prof[-1][0]
    return rad


def surf(rad, z, a, off=0.0):
    """(point `off` proud of the body of revolution r = rad(z) at height z, angle a deg (-90 = front), normal)."""
    t = math.radians(a)
    n = Vector((math.cos(t), math.sin(t), -(rad(z + 0.005) - rad(z - 0.005)) / 0.01)).normalized()
    return Vector((rad(z) * math.cos(t), rad(z) * math.sin(t), z)) + n * off, n


def panel(s, rad, zs, span, th, mat, n=24):
    """Thin raised panel conformal to a body of revolution whose angular span(z) -> (a0, a1) deg varies with height
    (cut garments, lapels); th proud of the body, 1 cm buried."""
    rows = []
    for z in zs:
        a0, a1 = span(z)
        r = rad(z)
        arc_ = [math.radians(a0 + (a1 - a0) * i / n) for i in range(n + 1)]
        rows.append([((r + th) * math.cos(u), (r + th) * math.sin(u), z) for u in arc_]
                    + [((r - 0.01) * math.cos(u), (r - 0.01) * math.sin(u), z) for u in reversed(arc_)])
    mesh(s, rows, mat)


def stud(s, p, n, r, mat):
    with s.at(tuple(p), rot_to(n)):
        s.lathe(dome(r, -0.004, r * 0.6, 3), mat, seg=8)


def rivets(s, rad, z, count, a0=0.0, a1=360.0, r=0.0055, mat=None):
    """A row of rivet heads round a body of revolution."""
    span = count if abs(a1 - a0) >= 360 else max(count - 1, 1)
    for k in range(count):
        p, n = surf(rad, z, a0 + (a1 - a0) * k / span)
        stud(s, p, n, r, mat or pal.Chrome)


def seam(s, r, z, w=0.004):
    """A dark panel line round a body of radius r at height z."""
    band(s, r - 0.004, r + 0.0015, z - w / 2, z + w / 2, pal.Iron, seg=32)


def vseam(s, rad, z0, z1, a, n=4):
    """A vertical panel line up a body of revolution at angle a."""
    s.sweep([tuple(surf(rad, z0 + (z1 - z0) * k / n, a)[0]) for k in range(n + 1)], 0.0028, pal.Iron, seg=4)


def lights(s, rad, z, count, a0, a1, lens=None, r=0.012):
    """A row of small round lamps on a body of radius rad(z), between angles a0..a1 (deg, 0 = +X, -90 = front)."""
    for k in range(count):
        p, n = surf(rad, z, a0 + (a1 - a0) * k / max(count - 1, 1))
        lamp(s, tuple(p), tuple(n), r, lens=lens or pal.Signal_Amber, depth=0.01, seg=8)


def vents(s, x0, x1, y, z0, n, h=0.012, gap=0.03):
    for k in range(n):
        z = z0 + gap * k
        s.box((x0, y - 0.004, z), (x1, y + 0.006, z + h), pal.Iron)


# ----------------------------------------------------------------- faces
# A face is a display, not a cartoon: dark glass in a chrome bezel with dot-matrix glyphs glowing on it, like a
# 1970s LED readout. The glyphs animate, so the engine draws them (DK_RobotFace.shader, RobotFace.cs, glyphs in
# RobotGlyphs.txt): the model carries only the glass and a UV'd screen sheet over it.

register("DK_Face_Amber", "face", "#F28A1E", 1.6)  # the LED colours of DK_Glow_Amber / DK_Glow_Seafoam
register("DK_Face_Teal", "face", "#3FC9A0", 1.4)


def display(s, rad, zc, w, h, ac=-90.0, cr=0.02, th=0.005, bezel=True):
    """Dark glass display conformal to a body of revolution r = rad(z): w (along the surface) x h metres, centred
    at angle ac and height zc, corners rounded cr, th proud, in a thin chrome bezel. Returns at(x, z) -> the point
    on its face x metres across (+X side positive) at height z, for glyphs."""
    def hw(z):
        dz = abs(z - zc) - (h / 2 - cr)
        return w / 2 - cr + math.sqrt(max(cr * cr - dz * dz, 0.0)) if dz > 0 else w / 2

    def ang(x, z):
        return ac + math.degrees(x / rad(z))

    panel(s, rad, [zc - h / 2 + h * k / 16 for k in range(17)], lambda z: (ang(-hw(z), z), ang(hw(z), z)), th,
          pal.Bakelite, n=20)
    if bezel:
        pts = []
        for cx, cz, a0 in ((w / 2 - cr, zc + h / 2 - cr, 0), (cr - w / 2, zc + h / 2 - cr, 90),
                           (cr - w / 2, zc - h / 2 + cr, 180), (w / 2 - cr, zc - h / 2 + cr, 270)):
            pts += [(cx + cr * math.cos(math.radians(a0 + 15 * k)), cz + cr * math.sin(math.radians(a0 + 15 * k)))
                    for k in range(7)]
        loop = [tuple(surf(rad, z, ang(x, z), th)[0]) for x, z in pts]
        s.sweep(loop + loop[:1], 0.0045, pal.Chrome, seg=6)
    return lambda x, z: surf(rad, z, ang(x, z), th)[0]


def screen(s, at, w, h, zc, mat, nu=12, nv=6, lift=0.0012):
    """The face layer of a display: a sheet w x h centred at (0, zc) over at(x, z), lifted off the glass, UVs 0..1
    (u across to +X, v up) for the engine's dot-matrix face. Built on its own part so the face can be driven alone."""
    def p(u, v):
        x, z, e = (u - 0.5) * w, zc + (v - 0.5) * h, 1e-4
        c = Vector(at(x, z))
        n = (Vector(at(x + e, z)) - Vector(at(x - e, z))).cross(Vector(at(x, z + e)) - Vector(at(x, z - e)))
        return c + n.normalized() * lift  # d/dx x d/dz points out of a face read left to right, bottom to top
    s.sheet(p, nu, nv, mat)


def bowtie(s, p, n, w, mat):
    """Bow tie of width w at p facing n: two flared wings and a knot."""
    h = w * 0.46
    with s.at(tuple(p), rot_to(n)):
        for sg in (-1, 1):
            wing = [(sg * w * 0.08, -h * 0.25), (sg * w * 0.5, -h * 0.5), (sg * w * 0.5, h * 0.5), (sg * w * 0.08, h * 0.25)]
            s.extrude(fillet(wing if sg > 0 else wing[::-1], w * 0.05, n=3), -w * 0.05, w * 0.05, mat, smooth_sides=True)
        s.rbox((-w * 0.1, -h * 0.3, -w * 0.07), (w * 0.1, h * 0.3, w * 0.08), w * 0.04, mat)


def star(r, k=5, inner=0.45):
    """CCW k-point star outline."""
    return [((r if i % 2 == 0 else r * inner) * math.cos(math.pi / 2 + math.pi * i / k),
             (r if i % 2 == 0 else r * inner) * math.sin(math.pi / 2 + math.pi * i / k)) for i in range(2 * k)]


def heart(r, n=28):
    """CCW heart outline, about 2r wide, point down."""
    pts = []
    for k in range(n):
        t = -2 * math.pi * k / n
        x = 16 * math.sin(t) ** 3
        y = 13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t)
        pts.append((x * r / 16, (y + 2) * r / 16))
    return pts


def emblem(s, p, n, outline, mat, t=0.006):
    with s.at(tuple(p), rot_to(n)):
        s.extrude(outline, -0.002, t, mat)


def badge(s, p, n, r, face=None):
    """Gold badge at p facing n with a raised enamel star."""
    with s.at(tuple(p), rot_to(n)):
        s.lathe(puck(r, -0.01, 0.006, 0.003), pal.Gold, seg=16)
        s.extrude(star(r * 0.72), 0.005, 0.009, face or pal.Enamel_Coral)


def pennant(s, p, h, mat, lean=(0.0, 0.0)):
    """Whip aerial with a triangular pennant streaming back (+Y) near its top."""
    top = (p[0] + lean[0], p[1] + lean[1], p[2] + h)
    s.sweep([tuple(p), top], 0.005, pal.Chrome, seg=6)
    with s.at(top):
        ball(s, 0.012, pal.Chrome, seg=8, n=5)
    with s.at((p[0] + lean[0] * 0.9, p[1] + lean[1] * 0.9, p[2] + h * 0.82), (90, 0, 90)):
        s.extrude([(0, -0.07), (0.17, -0.035), (0, 0)], -0.002, 0.002, mat)


# ----------------------------------------------------------------- mechanism

def joint(s, p, r):
    with s.at(tuple(p)):
        ball(s, r, pal.Chrome, seg=10, n=6)


def shoulder(s, p, side, r):
    """Bolted chrome shoulder disc at p on the body's `side` (+1 = +X), a ring of bolt heads round its hub."""
    with s.at(tuple(p), (0, side * 90, 0)):
        s.lathe(puck(r, -0.03, 0.03, r * 0.25), pal.Chrome, seg=16)
        s.lathe(puck(r * 0.45, 0.02, 0.045, r * 0.12), pal.Steel, seg=12)
        for k in range(6):
            a = math.pi * k / 3
            with s.at((r * 0.72 * math.cos(a), r * 0.72 * math.sin(a), 0.03)):
                s.lathe(dome(r * 0.1, -0.003, r * 0.08, 2), pal.Iron, seg=6)


def hand(s, r, curl=0.7):
    """Three-fingered hand in the local frame (+Z out of the wrist, palm toward -Y): chrome cuff, steel palm,
    jointed chrome fingers with rubber tips curled by `curl` (0 = open, 1 = fist), an opposed thumb."""
    s.lathe([(0, -0.01), (r * 1.3, -0.01), (r * 1.5, 0.02), (r * 1.3, 0.04), (0, 0.04)], pal.Chrome, seg=10)
    L = r * 1.5
    s.rbox((-r * 1.15, -r * 0.55, 0.035), (r * 1.15, r * 0.55, 0.035 + L), r * 0.45, pal.Steel)
    c = curl * math.pi / 2

    def finger(base, a, th1, th2, ln, rr):
        p1 = base + ln * Vector((math.sin(a), -math.sin(th1), math.cos(th1))).normalized()
        p2 = p1 + ln * 0.8 * Vector((math.sin(a), -math.sin(th2), math.cos(th2))).normalized()
        s.sweep([tuple(base), tuple(p1)], rr, pal.Chrome, seg=6)
        s.sweep([tuple(p1), tuple(p2)], rr * 0.9, pal.Chrome, seg=6)
        with s.at(tuple(p1)):
            ball(s, rr * 1.2, pal.Chrome, seg=6, n=4)  # knuckle
        with s.at(tuple(p2)):
            ball(s, rr * 1.05, pal.Rubber, seg=6, n=4)

    for k in (-1, 0, 1):
        finger(Vector((k * r * 0.78, 0, 0.03 + L)), k * (0.12 + 0.25 * (1 - curl)), 0.7 * c, 1.7 * c, r * 1.3, r * 0.32)
    finger(Vector((0, -r * 0.5, 0.035 + L * 0.35)), 0.0, math.radians(60), math.radians(60) - 1.2 * c, r * 1.1,
           r * 0.34)


def arm(s, pts, r=0.028, curl=0.7, roll=0.0):
    """Chrome arm through pts: tapered links, a rubber bellows boot on each, ball joints, a hand rolled `roll` deg
    about the forearm. Returns (wrist, forearm direction)."""
    for i, (a, b) in enumerate(zip(pts, pts[1:])):
        s.sweep([a, b], [r * 1.12, r * 0.95] if i == 0 else [r, r * 0.88], pal.Chrome, seg=8)
        m = (Vector(a) + Vector(b)) / 2
        with s.at(tuple(m), rot_to(Vector(b) - Vector(a))):
            for k in range(3):
                band(s, r * 1.05, r * 1.35, -0.03 + 0.024 * k, -0.018 + 0.024 * k, pal.Rubber, seg=8)
    for p in pts[:-1]:
        joint(s, p, r * 1.45)
    tip, d = Vector(pts[-1]), (Vector(pts[-1]) - Vector(pts[-2])).normalized()
    with s.at(tuple(tip), rot_to(d)), s.at((0, 0, 0), (0, 0, roll)):
        hand(s, r, curl)
    return tip, d


def hose(s, pts, r, mat=None, n=20):
    """Ribbed flexible hose along a smoothed path."""
    P = [Vector(p) for p in fillet(pts, r * 3, n=4, closed=False)]
    L = [0.0]
    for a, b in zip(P, P[1:]):
        L.append(L[-1] + (b - a).length)
    out, j = [], 0
    for k in range(n + 1):
        t = L[-1] * k / n
        while j < len(P) - 2 and L[j + 1] < t:
            j += 1
        u = (t - L[j]) / max(L[j + 1] - L[j], 1e-9)
        out.append(tuple(P[j].lerp(P[j + 1], min(u, 1.0))))
    s.sweep(out, [r * (1.15 if k % 2 else 0.9) for k in range(n + 1)], mat or pal.Rubber, seg=8)


def antenna(s, p, h, lean=(0, 0), tip=None):
    x, y, z = p
    s.sweep([(x, y, z), (x + lean[0], y + lean[1], z + h)], 0.006, pal.Chrome, seg=6)
    with s.at((x + lean[0], y + lean[1], z + h)):
        ball(s, 0.016, tip or pal.Signal_Red, seg=8, n=6)


def hover_disc(s, r, z0, z1, lamps=0):
    """Saucer base with a chrome rim (and a ring of amber running lights) and a teal glow on its underside."""
    rim, body = pal.Chrome, pal.Enamel_Ivory  # never as defaults: materials resolved at import die with the scene
    h = z1 - z0
    s.lathe([(0, z0), (r * 0.8, z0)], pal.Glow_Seafoam, seg=32, cap_top=False)
    s.lathe([(r * 0.8, z0), (r * 0.95, z0 + h * 0.12), (r, z0 + h * 0.3), (r * 0.99, z0 + h * 0.45),
             (r * 0.97, z0 + h * 0.55)], rim, seg=32, cap_bottom=False, cap_top=False)
    s.lathe([(r * 0.97, z0 + h * 0.55), (r * 0.8, z0 + h * 0.75), (r * 0.62, z0 + h * 0.92), (r * 0.5, z1), (0, z1)],
            body, seg=32, cap_bottom=False)
    top = profile_rad([(r * 0.97, z0 + h * 0.55), (r * 0.8, z0 + h * 0.75), (r * 0.62, z0 + h * 0.92)])
    if lamps:
        lights(s, top, z0 + h * 0.66, lamps, 0, 360 * (1 - 1 / lamps), r=0.011)


# ----------------------------------------------------------------- things they carry

def tray(s, c, r=0.19):
    """Chrome serving tray centred at c; returns the height of its top."""
    with s.at(tuple(c)):
        s.lathe(puck(r, 0.0, 0.016, 0.006), pal.Chrome, seg=24)
        s.lathe([(r - 0.004, 0.012), (r + 0.006, 0.03)], pal.Chrome, seg=24, cap_bottom=False, cap_top=False)
    return c[2] + 0.016


def cup(s, p):
    with s.at(tuple(p)):
        s.lathe([(0, 0), (0.06, 0.0), (0.07, 0.012), (0, 0.012)], pal.Enamel_Ivory, seg=14)
        s.lathe([(0, 0.012), (0.03, 0.012), (0.042, 0.06), (0.036, 0.062), (0, 0.02)], pal.Enamel_Ivory, seg=14)
        s.sweep(fillet([(0.036, 0, 0.05), (0.06, 0, 0.048), (0.058, 0, 0.026), (0.034, 0, 0.024)], 0.008, n=3,
                       closed=False), 0.004, pal.Enamel_Ivory, seg=6)


def glass(s, p):
    with s.at(tuple(p)):
        s.lathe([(0, 0), (0.03, 0.0), (0.024, 0.01), (0.006, 0.02), (0.006, 0.07), (0.03, 0.09), (0.034, 0.14),
                 (0.03, 0.14), (0.026, 0.095), (0, 0.078)], pal.Glass, seg=12)


def cloche(s, p):
    with s.at(tuple(p)):
        s.lathe(puck(0.11, 0.0, 0.008, 0.003), pal.Chrome, seg=16)
        s.lathe(dome(0.1, 0.006, 0.085, 8), pal.Chrome, seg=16)
        s.lathe([(0, 0.088), (0.012, 0.088), (0.012, 0.1), (0, 0.108)], pal.Gold, seg=8)


def teapot(s, p, r=0.048):
    """A round teapot in cream enamel with a gold knob."""
    with s.at(tuple(p)):
        s.lathe([(0, 0), (r * 0.7, 0), (r * 0.95, r * 0.35), (r, r * 0.7), (r * 0.9, r * 1.05), (r * 0.62, r * 1.28),
                 (r * 0.4, r * 1.32)], pal.Enamel_Cream, seg=16, cap_top=False)
        s.lathe([(r * 0.4, r * 1.32), (r * 0.44, r * 1.36), (r * 0.3, r * 1.5), (0, r * 1.54)], pal.Enamel_Coral, seg=14,
                cap_bottom=False)
        with s.at((0, 0, r * 1.6)):
            ball(s, r * 0.12, pal.Gold, seg=8, n=5)
        s.sweep([(r * 0.8, 0, r * 0.45), (r * 1.3, 0, r * 0.8), (r * 1.62, 0, r * 1.3)], [r * 0.2, r * 0.13, r * 0.09],
                pal.Enamel_Cream, seg=8)
        s.sweep(fillet([(-r * 0.82, 0, r * 1.08), (-r * 1.45, 0, r * 1.0), (-r * 1.4, 0, r * 0.45),
                        (-r * 0.9, 0, r * 0.35)], r * 0.2, n=3, closed=False), r * 0.08, pal.Enamel_Coral, seg=6)


def bud_vase(s, p, h=0.13):
    """A glass bud vase with one coral flower."""
    with s.at(tuple(p)):
        s.lathe([(0, 0), (0.018, 0), (0.022, 0.02), (0.012, 0.05), (0.008, 0.07), (0.012, 0.08), (0, 0.078)],
                pal.Glass, seg=10)
        s.sweep([(0, 0, 0.05), (0.004, 0, 0.1), (0.01, 0, h)], 0.003, pal.Stem, seg=5)
        with s.at((0.006, 0, 0.09), (0, 50, 0), (1, 0.45, 0.2)):
            ball(s, 0.02, pal.Stem, seg=6, n=4)  # leaf
        with s.at((0.01, 0, h)):
            ball(s, 0.009, pal.Enamel_Mustard, seg=8, n=5)
            for k in range(6):
                a = math.pi * k / 3
                with s.at((0.014 * math.cos(a), 0.014 * math.sin(a), 0.0), (0, 0, math.degrees(a)), (1, 0.6, 0.35)):
                    ball(s, 0.013, pal.Enamel_Coral, seg=6, n=4)


def towel(s, c, d, w=0.13, drop=(0.14, 0.1), t=0.008):
    """A cloth folded over a forearm through c heading d, hanging `drop` down each side."""
    with s.at(tuple(c), (0, 0, math.degrees(math.atan2(d[1], d[0])))):
        path = [(-0.046, -drop[0]), (-0.047, -0.02), (-0.032, 0.028), (0.0, 0.042), (0.032, 0.028), (0.047, -0.02),
                (0.05, -drop[1])]
        slab(s, thicken(fillet(path, 0.015, n=3, closed=False), t), w / 2, t * 0.45, pal.Fabric_Ivory)
