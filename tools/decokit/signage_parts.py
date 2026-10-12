"""Signage hardware (signage.py): atlas art panels in enamel mouldings, scalloped crests, lattice legs, catwalks,
caged ladders, cable trays, landing beacons, gooseneck lamps, chaser bulbs, neon channel letters, atom and starburst
finials. Kit frame: metres, Z up, front -Y. Panel helpers draw in local XY facing +Z (place with s.at(.., FRONT))."""

import math

from mathutils import Vector

import glyphs
from geo import dome, fillet, thicken
from mats import pal
from mega_parts import strut
from signage_art import AD, BOARD, POSTER, S
from signals import ball, bezel, puck
from structures import ccw, sec, tube

FRONT = (90, 0, 0)     # local XY -> world XZ, local +Z -> world -Y
BACK = (90, 0, 180)    # local +Z -> world +Y, local X -> world -X (reads the right way round from behind)
SIDE_R = (90, 0, 90)   # local +Z -> world +X, local X -> world +Y
SIDE_L = (90, 0, -90)  # local +Z -> world -X, local X -> world -Y


# ----------------------------------------------------------------- atlas UVs + art

def uv(rect, rot=False, inset=1.5):
    """Atlas pixel rect (x, y, w, h; y down) -> decal UVs (BL, BR, TR, TL); rot: content drawn 90 deg CCW."""
    x, y, w, h = rect
    u0, u1, v0, v1 = (x + inset) / S, (x + w - inset) / S, 1 - (y + h - inset) / S, 1 - (y + inset) / S
    q = ((u0, v0), (u1, v0), (u1, v1), (u0, v1))
    return q[1:] + q[:1] if rot else q


def ad(k):
    return uv(AD[k])


def poster(k):
    return uv(POSTER[k])


def board(k):
    return uv(BOARD[k][0], rot=True)


def art(s, w, h, uvq, mat, cx=0.0, cy=0.0):
    """Art quad w x h centred at (cx, cy) in local XY, facing +Z."""
    x0, x1, y0, y1 = cx - w / 2, cx + w / 2, cy - h / 2, cy + h / 2
    s.decal([(x0, y0, 0), (x1, y0, 0), (x1, y1, 0), (x0, y1, 0)], mat, uvq)


def framed(s, w, h, uvq, mat, trim=None, lip=0.2, depth=0.16, r=0.1, cy=0.0):
    """Art in a chrome bead and a raised enamel moulding `lip` wide standing `depth` proud: a real recess."""
    art(s, w, h, uvq, mat, cy=cy)
    bezel(s, w + 0.1, h + 0.1, 0.03, 0.07, -0.02, depth * 0.4, pal.Chrome, n=2, cy=cy)
    W, H = w + 0.06 + 2 * lip, h + 0.06 + 2 * lip
    bezel(s, W, H, r, lip, -0.04, depth, trim or pal.Enamel_Ivory, n=4, cy=cy)
    return W, H


def scallops(widths, heights, y0, base, n=8):
    """Crest outline in local XY (CCW): semi-elliptical scallops side by side on y = y0, centred on x = 0, over a
    straight band `base` deep."""
    x, top = -sum(widths) / 2, []
    for w, h in zip(widths, heights):
        pts = [(x + w / 2 * (1 - math.cos(math.pi * k / n)), y0 + h * math.sin(math.pi * k / n))
               for k in range(n + 1)]
        top += pts[1:] if top else pts
        x += w
    return ccw([(top[0][0], y0 - base), (top[-1][0], y0 - base)] + top[::-1])


# ----------------------------------------------------------------- engineered hardware

def ladder(s, z0, z1, w=0.46, cage=2.2, ext=0.9, mat=None):
    """Access ladder in the local XZ plane, climber on the -Y side: stiles running `ext` past the landing, rungs at
    0.3 m, a hoop cage bulging to -Y from `cage` m up. Place with s.at(foot, (0, 0, yaw))."""
    mat = mat or pal.Chrome
    for x in (-w / 2, w / 2):
        strut(s, (x, 0, z0), (x, 0, z1 + ext), 0.05, mat)
    n = max(1, round((z1 - z0) / 0.3))
    for k in range(1, n + 1):
        z = z0 + (z1 - z0) * k / n
        strut(s, (-w / 2, 0, z), (w / 2, 0, z), 0.03, mat)
    if cage is None or z1 - z0 < cage + 0.5:
        return
    hoop = [(w / 2 * math.cos(math.pi * k / 8), -0.72 * math.sin(math.pi * k / 8)) for k in range(9)]
    hz = [z0 + cage + 0.8 * k for k in range(int((z1 + ext - z0 - cage) / 0.8) + 1)]
    for z in hz:
        s.sweep([(x, y, z) for x, y in hoop], 0.018, mat, seg=5)
    for k in (2, 4, 6):
        x, y = hoop[k]
        strut(s, (x, y, hz[0]), (x, y, hz[-1]), 0.025, mat)


def tray(s, path, up, w=0.16, mat=None):
    """Cable tray: a flat steel channel along a polyline (`up` = its face normal) carrying a dark cable bundle."""
    tube(s, path, sec(-w / 2, w / 2, 0.0, 0.025), mat or pal.Steel, up=up)
    tube(s, path, sec(-w / 2, -w / 2 + 0.018, 0.0, 0.07), mat or pal.Steel, up=up, caps=False)
    s.sweep([tuple(Vector(p) + Vector(up) * 0.05) for p in path], 0.032, pal.Iron, seg=6)


def beacon(s, c, r=0.1):
    """Red landing-light beacon on a chrome puck."""
    with s.at(c):
        s.lathe(puck(r * 1.35, 0, r * 0.45, r * 0.15, 2), pal.Chrome, seg=12)
        s.lathe(dome(r, r * 0.4, r * 1.3, n=4), pal.Signal_Red, seg=12)


def rail(s, pts, h=1.0, r=0.028, post=1.6, mat=None):
    """Chrome guard rail along a floor polyline (3D points): posts, top and knee rails."""
    mat = mat or pal.Chrome
    P = [Vector(p) for p in pts]
    for a, b in zip(P, P[1:]):
        n = max(1, round((b - a).length / post))
        for k in range(n + (b is P[-1])):
            p = a.lerp(b, k / n)
            s.sweep([tuple(p), tuple(p + Vector((0, 0, h)))], r, mat, seg=6)
    for z in (h, h * 0.5):
        s.sweep([tuple(p + Vector((0, 0, z))) for p in P], r * (1 if z == h else 0.7), mat, seg=6)


def gooseneck(s, base, rise, reach, drop, aim=30.0, shade=None, R=0.26):
    """Chrome gooseneck from base: up `rise`, arching `reach` toward -Y and down `drop`; an enamel reflector whose
    lamp face points down, tilted `aim` degrees back toward +Y (the sign)."""
    x, y, z = base
    head = (x, y - reach, z + rise - drop)
    path = fillet([(x, y, z), (x, y, z + rise), (x, y - reach, z + rise), head], min(rise, reach) * 0.55, n=6,
                  closed=False)
    s.sweep(path, [0.04 - 0.015 * i / (len(path) - 1) for i in range(len(path))], pal.Chrome, seg=8)
    with s.at(head, (aim, 0, 0)):
        s.lathe([(R, -0.24), (R * 0.97, -0.19), (R * 0.72, -0.08), (R * 0.3, -0.015), (0, 0)], shade or pal.Enamel_Teal,
                seg=14, cap_bottom=False)
        s.lathe([(0, -0.19), (R * 0.9, -0.23), (R * 0.95, -0.24)], pal.Lamp, seg=14, cap_bottom=False, cap_top=False)
        s.lathe([(R * 0.95, -0.25), (R * 1.04, -0.245), (R * 1.04, -0.225), (R * 0.95, -0.22), (R * 0.95, -0.25)],
                pal.Chrome, seg=14, cap_bottom=False, cap_top=False)


def bulb(s, r=0.05, dim=False, seg=6):
    """Chaser bulb: a low-poly dome facing local +Z (dim = the unlit step of the chase)."""
    s.lathe(dome(r, 0, r * 1.1, n=2), pal.Bulb_Dim if dim else pal.Lamp, seg=seg, cap_bottom=False)


def bulbs(s, pts, r=0.05, every=3):
    """Chaser bulbs at local (x, y) facing +Z; every `every`-th one dim (a frozen chase)."""
    for i, (x, y) in enumerate(pts):
        with s.at((x, y, 0)):
            bulb(s, r, i % every == 0)


def row(x0, x1, y, step):
    n = max(1, round((x1 - x0) / step))
    return [(x0 + (x1 - x0) * k / n, y) for k in range(n + 1)]


# ----------------------------------------------------------------- neon lettering + finials

def neon_word(s, word, cap, body, neon, stack=False, depth=0.07, t=0.17, r=0.024):
    """Raised enamel channel letters (stroke t * cap, `depth` deep) centred on local XY at z = 0 facing +Z, each stroke
    traced by an emissive neon tube; stack sets the word vertically, top letter first."""
    placed, (ew, eh) = glyphs.layout(word, stack=stack)
    ox, oy = -ew / 2, (eh - 2) / 2
    for strokes, (gx, gy) in placed:
        for st in strokes:
            pts = [((gx + x + ox) * cap, (gy + y + oy) * cap) for x, y in glyphs.rounded(st, 0.16, n=3)]
            s.extrude(ccw(thicken(pts, t * cap)), -0.01, depth, body, bottom=False)
            s.sweep([(x, y, depth + r * 0.8) for x, y in pts], r, neon, seg=6)


def _tri(s, a, b, c, mat, up):
    n = (Vector(b) - Vector(a)).cross(Vector(c) - Vector(a))
    s.face([a, b, c] if n.z * up >= 0 else [a, c, b], mat)


def starburst(s, n, r_in, r_out, mats, alt=0.6, ridge=0.06, tip=0.05, both=True, a0=90.0):
    """Faceted Googie star in local XY facing +Z (and -Z when both): n ridged kite spikes alternating long / short
    (mats = (long, short)), a gold bead on each long tip."""
    hw = math.pi / n * 0.8
    for k in range(n):
        a = math.radians(a0) + 2 * math.pi * k / n
        L = r_out if k % 2 == 0 else r_out * alt
        c, sn, rs = math.cos(a), math.sin(a), r_in + 0.26 * (L - r_in)
        ws = max(r_in * math.sin(hw) * 1.25, 0.045 * L)  # shoulder half-width: a diamond ray, not a needle
        T, Rc = (L * c, L * sn, 0), (rs * c, rs * sn, 0)
        B1, B2 = ((r_in * math.cos(a + d), r_in * math.sin(a + d), 0) for d in (-hw, hw))
        S1, S2 = ((Rc[0] - q * ws * sn, Rc[1] + q * ws * c, 0) for q in (-1, 1))
        for up in ((1, -1) if both else (1,)):
            R = (rs * c, rs * sn, up * ridge)
            for tri in ((B1, S1), (S1, T), (T, S2), (S2, B2)):
                _tri(s, *tri, R, mats[k % 2], up)
        if k % 2 == 0 and tip:
            with s.at(T):
                ball(s, tip, pal.Gold, seg=8, n=4)


def atom(s, c, R, core=None, glow=None, ring=None):
    """Atom finial: three chrome orbits tilted 60 deg at 120 deg spacing with glowing electrons round a nucleus."""
    with s.at(c):
        ball(s, R * 0.24, core or pal.Enamel_Red, seg=16, n=8)
        for k in range(3):
            with s.at((0, 0, 0), (0, 0, 120 * k)), s.at((0, 0, 0), (0, 60, 0)):
                pts = [(R * math.cos(a), R * 0.36 * math.sin(a), 0) for a in (2 * math.pi * i / 24 for i in range(25))]
                s.sweep(pts, R * 0.05, ring or pal.Chrome, seg=5, caps=False)
                with s.at(pts[(8 * k + 3) % 24]):
                    ball(s, R * 0.075, glow or pal.Neon_Teal, seg=10, n=5)
