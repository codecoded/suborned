"""Signals & surveillance (reference sheet row 3 + the plaza's chrome-ball posts): traffic light, CCTV pole,
dome camera, info lightbox totem, plaza sphere lamp. Chunky toy reads, chrome trim wherever parts meet."""

import math

from geo import arc, dome, fillet, ring, rrect
from kit import asset
import mats
from vehicles_parts import mesh
from mats import pal

FRONT = (90, 0, 0)     # local XY -> world XZ, local +Z -> world -Y (panel / lathe axis facing the viewer)
BACK = (90, 0, 180)    # local +Z -> world +Y
SIDE_R = (90, 0, 90)   # local X -> world Y, local Y -> world Z, local +Z -> world +X
SIDE_L = (90, 0, -90)  # local +Z -> world -X
# low-glow lens rims keep the aspect colours saturated; the DK_Signal core is the hot spot
LENS = {c: mats.register(f"DK_SignalLens_{c}", "emit", hx, 0.3) for c, hx in
        (("Red", "#CC1E12"), ("Amber", "#F2B20E"), ("Green", "#0E9E48"))}
mats.register("DK_Enamel_Vermilion", "enamel", "#CF4413")  # signal hoods: hotter than the kit's coral


# ----------------------------------------------------------------- local helpers

def band(s, r0, r1, z0, z1, mat, seg=32):
    """Open lathe ring: trim band / collar."""
    s.lathe(ring(r0, r1, z0, z1), mat, seg=seg, cap_bottom=False, cap_top=False)


def ball(s, r, mat, seg=32, n=12, z=0.0):
    s.lathe([(r * math.sin(math.pi * k / n), z - r * math.cos(math.pi * k / n)) for k in range(n + 1)], mat, seg=seg)


def soft(profile, r, n=3):
    """Round the interior corners of an open (r, z) lathe profile (soft chrome highlights, no hard bevel)."""
    return fillet(profile, r, n, closed=False)


def puck(r, z0, z1, e, n=3):
    """(r, z) lathe profile of a disc whose rim is rounded by e."""
    return [(0, z0)] + arc(r - e, z0 + e, e, -90, 0, n) + arc(r - e, z1 - e, e, 0, 90, n) + [(0, z1)]


def _rr(w, h, r, d, n, cy=0.0):
    return rrect(w - 2 * d, h - 2 * d, max(r - d, 0.002), n, 0.0, cy)


def slab(s, w, h, r, t0, t1, mat, e=0.01, n=6, m=3, cy=0.0):
    """Rounded-rect plate (local XY outline w x h, corner r) from t0 to t1 on local Z, both rims filleted by e."""
    secs = [(_rr(w, h, r, e * (1 - math.sin(a)), n, cy), t0 + e * (1 - math.cos(a)))
            for a in (math.pi / 2 * k / m for k in range(m + 1))]
    secs += [(_rr(w, h, r, e * (1 - math.cos(a)), n, cy), t1 - e + e * math.sin(a))
             for a in (math.pi / 2 * k / m for k in range(m + 1))]
    s.loft(secs, mat)


def bezel(s, w, h, r, lip, t0, t1, mat, n=6, cy=0.0):
    """Raised rounded-rect frame `lip` wide (outline w x h, corner r) from t0 to t1 on local Z, soft top edges."""
    e = min(lip / 3, (t1 - t0) / 2)
    o = lambda d: _rr(w, h, r, d, n, cy)  # noqa: E731
    s.loft([(o(0), t0), (o(0), t1 - e), (o(e), t1), (o(lip - e), t1), (o(lip), t1 - e), (o(lip), t0)], mat,
           cap_bottom=False, cap_top=False)


def bolts(s, count, rad, z, r=0.016, h=0.012, a0=0.0):
    """Ring of domed bolt heads (open bottoms sit on the surface below)."""
    for k in range(count):
        a = math.radians(a0 + 360 * k / count)
        with s.at((rad * math.cos(a), rad * math.sin(a), z)):
            s.lathe(dome(r, 0, h, n=3), pal.Chrome, seg=8, cap_bottom=False)


BANNER_V = (1.0, 0.975, 0.895, 0.35, 0.025, 0.0)  # banner_comet rows: border | plain | comet | plain | border


def banner(s, x0, x1, z1, h, mat, cols=5, amp=0.008):
    """Double-sided hanging banner in the XZ plane (front -Y), top edge at z1. The comet motif keeps the
    texture's 2:3 aspect; only its two plain-teal rows stretch to the requested length. Gentle cloth wave."""
    V, w = BANNER_V, x1 - x0
    nat = [(V[i] - V[i + 1]) * 1.5 * w for i in range(5)]
    g = (h - nat[0] - nat[2] - nat[4]) / (nat[1] + nat[3])
    z = [z1]
    for i, hh in enumerate(nat):
        z.append(z[-1] - hh * (g if i in (1, 3) else 1))
    for i in range(5):
        for c in range(cols):
            u0, u1 = c / cols, (c + 1) / cols
            xa, xb = x0 + w * u0, x0 + w * u1
            ya, yb = amp * math.sin(2 * math.pi * u0), amp * math.sin(2 * math.pi * u1)
            s.decal([(xa, ya, z[i + 1]), (xb, yb, z[i + 1]), (xb, yb, z[i]), (xa, ya, z[i])], mat,
                    ((u0, V[i + 1]), (u1, V[i + 1]), (u1, V[i]), (u0, V[i])))
            s.decal([(xb, yb, z[i + 1]), (xa, ya, z[i + 1]), (xa, ya, z[i]), (xb, yb, z[i])], mat,
                    ((1 - u1, V[i + 1]), (1 - u0, V[i + 1]), (1 - u0, V[i]), (1 - u1, V[i])))


# ----------------------------------------------------------------- assets

def aspect(s, r, col, cowl, depth):
    """Round signal aspect in a lamp frame (+Z out): saturated lens rim over a hot core, chrome bezel, and the
    deep open-bottomed hood of the 1950s signal, swept back to a narrower lip."""
    dm = dome(r, 0.004, r * 0.38, n=6)
    s.lathe([(0, 0.004)] + dm[:5], mats.get(LENS[col]), seg=28, cap_top=False)
    s.lathe(dm[4:], mats.get("DK_Signal_" + col), seg=28)
    s.lathe(soft([(r * 0.96, -0.03), (r * 1.2, -0.03), (r * 1.24, 0.012), (r * 1.14, 0.03), (r * 0.96, 0.018)], 0.006),
            pal.Chrome, seg=28, cap_bottom=False, cap_top=False)
    rows, us = [], [math.radians(-22 + 224 * k / 18) for k in range(19)]  # open at the bottom
    for t in (i / 6 for i in range(7)):
        rc = r * (1.28 + 0.14 * t)  # a slight flare to the lip
        reach = [-0.02 + (depth + 0.02) * t * (0.42 + 0.58 * max(0.0, math.sin(u))) for u in us]  # longest on top
        rows.append([(rc * math.cos(u), rc * math.sin(u), z) for u, z in zip(us, reach)] +
                    [((rc - 0.012) * math.cos(u), (rc - 0.012) * math.sin(u), z) for u, z in zip(us[::-1], reach[::-1])])
    mesh(s, rows, cowl)


@asset("TrafficLight", "Signals")
def traffic_light(s):
    """Space-age signal (props sheet, signals #1): a cream enamel pole tapering from a teal bell foot and swelling
    into the lamp head, three hooded aspects to the traffic, a pedestrian pair on the flank, a chrome acorn."""
    s.lathe(soft([(0.3, 0.0), (0.3, 0.04), (0.27, 0.07), (0.22, 0.14), (0.18, 0.26), (0.165, 0.36)], 0.025),
            pal.Enamel_Seafoam, seg=40, cap_top=False)
    band(s, 0.302, 0.307, 0.0, 0.035, pal.Rubber, seg=40)
    pole = [(0.165, 0.36), (0.145, 0.9), (0.14, 1.5), (0.15, 1.85), (0.185, 2.05), (0.195, 2.2), (0.195, 3.08),
            (0.18, 3.26), (0.12, 3.36), (0.06, 3.4), (0, 3.41)]
    s.lathe(soft(pole, 0.05, 4), pal.Enamel_Ivory, seg=36, cap_bottom=False)
    a = math.radians(-125)  # chrome rib down the front-left flank
    s.sweep([((r + 0.004) * math.cos(a), (r + 0.004) * math.sin(a), z) for r, z in pole[:8]], 0.011, pal.Chrome, seg=8)
    for zc, col in ((2.97, "Red"), (2.6, "Amber"), (2.23, "Green")):
        with s.at((0, -0.18, zc), FRONT):
            s.lathe([(0.185, -0.06), (0.185, 0.0)], pal.Enamel_Ivory, seg=32, cap_bottom=False)  # boss off the pole
            aspect(s, 0.148, col, pal.Enamel_Vermilion, 0.27)
    for zc, col, cowl in ((2.96, "Red", pal.Enamel_Vermilion), (2.68, "Green", pal.Enamel_Vermilion),
                          (2.4, "Amber", pal.Chrome)):
        with s.at((-0.18, 0, zc), SIDE_L):
            s.lathe([(0.105, -0.06), (0.105, 0.0)], pal.Enamel_Ivory, seg=28, cap_bottom=False)
            aspect(s, 0.085, col, cowl, 0.17)
    with s.at((0, 0, 3.38)):  # chrome acorn finial
        s.lathe(soft([(0.075, 0.0), (0.075, 0.025), (0.045, 0.05), (0.045, 0.08)], 0.01), pal.Chrome, seg=28,
                cap_bottom=False)
        s.lathe([(0.045, 0.08), (0.075, 0.12), (0.085, 0.18), (0.072, 0.25), (0.045, 0.31), (0.015, 0.35), (0, 0.36)],
                pal.Chrome, seg=28, cap_bottom=False)


@asset("CCTVPole", "Signals")
def cctv_pole(s):
    s.lathe(soft([(0.335, 0), (0.34, 0.04), (0.32, 0.06), (0.26, 0.075), (0.25, 0.09), (0.25, 0.21), (0.235, 0.235),
                  (0.2, 0.25), (0, 0.25)], 0.012), pal.Chrome, seg=32, cap_bottom=False)
    bolts(s, 6, 0.29, 0.064, r=0.022, h=0.015, a0=30)
    band(s, 0.176, 0.198, 0.24, 0.29, pal.Chrome, seg=32)
    s.lathe([(0.175, 0.28), (0.175, 0.3), (0.158, 1.3), (0, 1.3)], pal.Enamel_Teal, seg=32, cap_bottom=False)
    band(s, 0.154, 0.17, 1.28, 1.305, pal.Chrome, seg=32)
    s.lathe([(0.155, 1.3), (0.18, 1.303)] + arc(0.174, 1.322, 0.018, -75, 0, 2) + arc(0.174, 1.418, 0.018, 0, 75, 2)
            + [(0.155, 1.437), (0.1, 1.44), (0, 1.44)], pal.Enamel_Red, seg=32, cap_bottom=False)
    band(s, 0.075, 0.12, 1.435, 1.46, pal.Chrome, seg=32)
    s.lathe([(0.078, 1.45), (0.07, 2.4), (0, 2.4)], pal.Chrome, seg=24, cap_bottom=False)
    band(s, 0.068, 0.088, 2.37, 2.4, pal.Chrome, seg=28)
    s.lathe([(0.08, 2.39), (0.088, 2.4), (0.088, 2.5), (0.078, 2.515), (0.05, 2.52), (0, 2.52)], pal.Enamel_Royal,
            seg=28, cap_bottom=False)
    ball(s, 0.06, pal.Chrome, seg=20, n=8, z=2.55)
    s.sweep([(0, 0, 2.55), (0, 0.03, 2.6), (0, 0.068, 2.655)], 0.036, pal.Chrome, seg=14)
    with s.at((0, -0.1, 2.9), (12, 0, 0), 1.12):  # housing tilted 12 deg nose-down, pole under its rear third
        s.rbox((-0.09, -0.05, -0.26), (0.09, 0.22, -0.17), 0.025, pal.Chrome)
        s.rbox((-0.19, -0.48, -0.19), (0.19, 0.44, 0.19), 0.14, pal.Enamel_Ivory, seg=4)
        s.rbox((-0.04, -0.44, 0.165), (0.04, 0.1, 0.205), 0.018, pal.Enamel_Teal)
        with s.at((0, 0.44, 0), (-90, 0, 0)):  # rear cable gland
            s.lathe(puck(0.055, -0.01, 0.03, 0.012), pal.Chrome, seg=20)
        with s.at((0, -0.4, 0), FRONT):  # flared teal lens hood
            sec = lambda w, r: rrect(w, w, r, 5)  # noqa: E731
            s.loft([(sec(0.38, 0.14), 0.0), (sec(0.42, 0.16), 0.075), (sec(0.42, 0.16), 0.165),
                    (sec(0.396, 0.148), 0.192), (sec(0.33, 0.12), 0.192), (sec(0.31, 0.113), 0.15)],
                   pal.Enamel_Teal, cap_bottom=False)
            with s.at((0, 0, 0.15)):
                s.lathe([(0.12, 0.0), (0, 0.0)], pal.Iron, seg=28, cap_bottom=False)  # dark lens well
                s.lathe([(0.116, 0.0), (0.138, 0.0), (0.14, 0.016), (0.128, 0.028), (0.116, 0.022),
                         (0.116, 0.0)], pal.Chrome, seg=32, cap_bottom=False, cap_top=False)
                s.lathe([(0.04, 0.006), (0.058, 0.006), (0.058, 0.022), (0.04, 0.026), (0.04, 0.006)], pal.Chrome,
                        seg=24, cap_bottom=False, cap_top=False)
                s.lathe([(0, 0.006)] + dome(0.042, 0.006, 0.024, n=4), pal.Enamel_Charcoal, seg=24)
                s.lathe([(0, 0.004)] + dome(0.118, 0.004, 0.045, n=6), pal.GlassDark, seg=32, cap_bottom=False)


@asset("DomeCamera", "Signals")
def dome_camera(s):
    with s.at((0, 0, 0.22), FRONT):  # wall plate: back face on y = 0, bottom at z = 0
        slab(s, 0.15, 0.44, 0.07, 0, 0.028, pal.Enamel_Teal, e=0.01)
        slab(s, 0.122, 0.412, 0.056, 0.02, 0.04, pal.Enamel_Red, e=0.008)
        bezel(s, 0.148, 0.438, 0.069, 0.016, 0.02, 0.046, pal.Chrome)
        for y in (0.168, -0.168):
            with s.at((0, y, 0.039)):
                s.lathe([(0, 0), (0.011, 0)] + dome(0.011, 0, 0.008, n=4)[1:], pal.Chrome, seg=16)
        s.rbox((-0.036, -0.085, 0.03), (0.036, 0.085, 0.068), 0.018, pal.Chrome)
    s.sweep([(0, -0.06, 0.22), (0, -0.135, 0.22)], 0.024, pal.Chrome, seg=16)
    s.sweep([(0, -0.135, 0.22), (0, -0.245, 0.285)], 0.021, pal.Chrome, seg=16)
    with s.at((0, -0.135, 0.22), (0, 90, 0)):
        s.lathe(puck(0.034, -0.035, 0.035, 0.012), pal.Chrome, seg=20)
    with s.at((0, -0.4, 0.03), (0, 0, 0), 0.9):  # crown + blue band + skirt, smoked glass dome with a dark liner and lens ball
        s.lathe(soft([(0.15, 0.205), (0.162, 0.215), (0.19, 0.25), (0.203, 0.298), (0.207, 0.345)], 0.01)
                + [(0.207 * math.cos(a) ** 0.75, 0.345 + 0.11 * math.sin(a) ** 0.85)  # squarish helmet crown
                   for a in (math.pi / 20 * k for k in range(1, 11))], pal.Enamel_Ivory, seg=48)
        s.lathe([(0.204, 0.292), (0.219, 0.294), (0.225, 0.302), (0.225, 0.338), (0.219, 0.346), (0.204, 0.348),
                 (0.204, 0.292)], pal.Enamel_Royal, seg=48, cap_bottom=False, cap_top=False)
        s.lathe([(0.14, 0.195), (0.154, 0.195), (0.159, 0.205), (0.154, 0.216), (0.14, 0.216), (0.14, 0.195)],
                pal.Chrome, seg=40, cap_bottom=False, cap_top=False)
        s.lathe([(0.15 * math.sin(a), 0.207 - 0.15 * math.cos(a)) for a in (math.pi / 20 * k for k in range(11))],
                pal.GlassDark, seg=40, cap_top=False)
        with s.at((0, 0.035, 0.207)):
            s.lathe([(0.13 * math.sin(x), -0.13 * math.cos(x)) for x in (math.pi / 12 * k for k in range(7))],
                    pal.Enamel_Charcoal, seg=28, cap_top=False)
        with s.at((0, -0.05, 0.165), (125, 0, 0)):  # lens ball aimed forward and down
            ball(s, 0.06, pal.Iron, seg=28, n=12)
            s.lathe([(0.03, 0.044), (0.05, 0.036), (0.054, 0.052), (0.04, 0.068), (0.03, 0.044)], pal.Chrome,
                    seg=28, cap_bottom=False, cap_top=False)
            s.lathe([(0, 0.052)] + dome(0.032, 0.052, 0.018, n=5), pal.GlassDark, seg=28)
            s.lathe([(0, 0.05), (0.016, 0.05)] + dome(0.016, 0.05, 0.013, n=4)[1:], pal.Iron, seg=16)


@asset("InfoLightbox", "Signals", bevel=0.006)
def info_lightbox(s):
    W, D, R, Z0, Z1, RT = 0.74, 0.34, 0.06, 0.4, 2.0, 0.24
    plan = lambda w, d, r=R: rrect(w, d, r, 6)  # noqa: E731
    s.loft([(plan(0.9, 0.46, 0.15), 0), (plan(0.92, 0.48, 0.16), 0.015), (plan(0.92, 0.48, 0.16), 0.05),
            (plan(0.88, 0.44, 0.14), 0.075)], pal.Chrome)  # chrome foot
    s.loft([(plan(0.84, 0.42, 0.13), 0.07), (plan(0.84, 0.42, 0.13), 0.235), (plan(0.8, 0.38, 0.11), 0.265)],
           pal.Enamel_Teal, cap_bottom=False)
    s.loft([(plan(0.792, 0.392, 0.1), 0.25), (plan(0.792, 0.392, 0.1), 0.27)], pal.Chrome,
           cap_bottom=False, cap_top=False)
    s.loft([(plan(0.78, 0.38, 0.09), 0.26), (plan(0.78, 0.38, 0.09), 0.39), (plan(0.76, 0.36, 0.08), 0.415)],
           pal.Enamel_Teal, cap_bottom=False)
    secs = [(plan(W, D), Z0 - 0.02), (plan(W, D), Z1 - RT)]  # chrome shell, top rounded in both views
    for k in range(1, 9):
        a = math.pi / 2 * k / 8
        secs.append((plan(W - 2 * RT * (1 - math.cos(a)), D - 0.16 * (1 - math.cos(a))), Z1 - RT + RT * math.sin(a)))
    s.loft(secs, pal.Chrome)
    zc, pw, ph = 1.12, 0.56, 1.2  # glowing travel poster, framed by a raised chrome bezel
    s.decal([(-pw / 2, -D / 2, zc - ph / 2), (pw / 2, -D / 2, zc - ph / 2), (pw / 2, -D / 2, zc + ph / 2),
             (-pw / 2, -D / 2, zc + ph / 2)], pal.Lightbox_Travel, uv=((0, 0), (1, 0), (1, 1), (0, 1)))
    with s.at((0, -D / 2, zc), FRONT):
        bezel(s, pw + 0.07, ph + 0.07, 0.07, 0.04, -0.006, 0.014, pal.Chrome)
    with s.at((0, D / 2, 1.12), BACK):
        slab(s, W - 0.12, 1.26, 0.12, -0.004, 0.01, pal.Enamel_Ivory, e=0.005)
    for x, rot in ((W / 2, SIDE_R), (-W / 2, SIDE_L)):
        with s.at((x, 0, 1.11), rot):
            slab(s, 0.21, 1.24, 0.09, -0.004, 0.01, pal.Enamel_Teal, e=0.004)
            slab(s, 0.044, 0.2, 0.02, 0.006, 0.016, pal.Chrome, e=0.004, cy=0.4)
            with s.at((0, -0.48, 0.006)):
                s.lathe(puck(0.022, 0, 0.01, 0.004, 2), pal.Chrome, seg=16)


@asset("PlazaSphereLamp", "Signals")
def plaza_sphere_lamp(s):
    sq = lambda w, r=0.03: rrect(w, w, r, 3)  # noqa: E731
    s.loft([(sq(0.5, 0.05), 0), (sq(0.52, 0.06), 0.012), (sq(0.52, 0.06), 0.045), (sq(0.48, 0.05), 0.065)],
           pal.Chrome)
    s.loft([(sq(0.42), 0.05), (sq(0.42), 0.1), (sq(0.34), 0.78)], pal.Enamel_Teal, cap_bottom=False, cap_top=False)
    s.loft([(sq(0.43), 0.09), (sq(0.43), 0.125)], pal.Chrome, cap_bottom=False, cap_top=False)
    s.loft([(sq(0.34), 0.76), (sq(0.38), 0.77), (sq(0.38), 0.8), (sq(0.34), 0.83), (sq(0.22, 0.05), 0.86),
            (sq(0.2, 0.05), 0.87)], pal.Chrome)
    s.lathe([(0.11, 0.86), (0.11, 0.88), (0.09, 0.91), (0.08, 0.97), (0.075, 0.99), (0.065, 3.95), (0, 3.95)],
            pal.Chrome, seg=32, cap_bottom=False)
    band(s, 0.07, 0.08, 1.8, 1.83, pal.Chrome)
    s.lathe([(0.062, 3.93), (0.085, 3.95), (0.085, 4.0), (0.078, 4.01), (0.078, 4.03), (0.085, 4.04), (0.085, 4.07),
             (0.06, 4.09), (0.045, 4.095), (0.045, 4.14), (0, 4.14)], pal.Chrome, seg=32, cap_bottom=False)
    s.lathe(ring(0.045, 0.1, 4.1, 4.125), pal.Lamp, seg=32, cap_bottom=False, cap_top=False)
    ball(s, 0.225, pal.Chrome, seg=48, n=20, z=4.36)
    s.lathe([(0.012, 4.57), (0.012, 4.6)], pal.Chrome, seg=12, cap_bottom=False, cap_top=False)
    ball(s, 0.022, pal.Chrome, seg=16, n=8, z=4.61)
    for z, x1, r in ((3.8, 0.66, 0.016), (2.42, 0.66, 0.013)):
        band(s, 0.066, 0.08, z - 0.03, z + 0.03, pal.Chrome)
        s.sweep([(0.07, 0, z), (x1, 0, z)], r, pal.Chrome, seg=12)
        with s.at((x1 + r, 0, 0)):
            ball(s, r * 1.9, pal.Chrome, seg=16, n=8, z=z)
    banner(s, 0.13, 0.63, 3.8, 1.38, pal.Banner_Comet)
