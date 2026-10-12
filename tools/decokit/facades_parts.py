"""Facade family helpers (used by facades.py): mitred plan offsets and run frames, the split ground trims, Deco window
columns, portholes, starbursts, raised sign letters and the engineered roof plant (vent banks, ducts, antenna masts).

Plan paths keep the building on the left of travel (structures.tube's frame): offset a > 0 is inward, a < 0 out.
`run(s, p0, p1)` puts local x along a straight facade run with the facade plane at y = 0 and +y inward, which is the
kit's front convention, so the -Y facing elevation helpers here and in plaza_parts drop onto any run or return."""

import math
from contextlib import contextmanager

from mathutils import Vector

from geo import fillet
from mats import pal
from plaza_parts import bead, lift
from skyline import FH, G, SP, floor_trims
from structures import ccw, sec, tube

FRONT = (90, 0, 0)  # local XY -> world XZ, extrusion -> world -Y


# ----------------------------------------------------------------- plan geometry

def moff(path, a):
    """Exact mitred offset of a straight-run plan polyline (left of travel for a > 0)."""
    P = [Vector(p) for p in path]
    N = [Vector((-t.y, t.x)) for t in ((b - q).normalized() for q, b in zip(P, P[1:]))]
    out = []
    for i, p in enumerate(P):
        m = N[0] if i == 0 else N[-1] if i == len(P) - 1 else (N[i - 1] + N[i]) / (1.0 + N[i - 1].dot(N[i]))
        out.append(tuple(p + m * a))
    return out


def band(path, a0, a1):
    """Plan polygon between two offsets of a path: piers, pylons and parapet strips wrapping a corner."""
    return ccw(moff(path, a0) + moff(path, a1)[::-1])


def soft(path, r=0.05):
    """Tight fillet on a plan path's corners so swept trims turn them round instead of pinching."""
    return fillet(path, r, n=6, closed=False)


@contextmanager
def run(s, p0, p1):
    """Local frame on the facade run p0 -> p1 (yields its length)."""
    d = Vector(p1) - Vector(p0)
    with s.at((p0[0], p0[1], 0), (0, 0, math.degrees(math.atan2(d.y, d.x)))):
        yield d.length


# ----------------------------------------------------------------- ground floor (skyline.ground_trims, split)

def plinth(s, path):
    tube(s, lift(path), sec(-0.06, 0.3, 0.0, 0.45, 0.02), pal.Stone_Dark)


def canopy(s, path):
    tube(s, lift(path), sec(-1.6, 0.0, 3.85, 4.0, 0.06), pal.Enamel_Ivory)
    bead(s, path, -1.6, 3.925, 0.03, pal.Chrome)


def fascia(s, path, mat):
    tube(s, lift(path), sec(-0.1, 0.3, 4.0, G, 0.03), mat)
    for z in (4.06, G - 0.06):
        bead(s, path, -0.1, z, 0.025, pal.Gold)


def parapet_trims(s, path, n, band_mat, closed=False):
    """skyline.floor_trims' parapet band and coping only, at top_of(n)."""
    with s.at((0, 0, n * FH)):
        floor_trims(s, path, 0, band_mat, closed)


def lamps(s, pts, z):
    """Recessed downlight domes in a canopy soffit."""
    for x, y in pts:
        with s.at((x, y, z)):
            s.lathe([(0.16, 0.0), (0.16, -0.02), (0.12, -0.07), (0.0, -0.09)], pal.Lamp, seg=12)


# ----------------------------------------------------------------- Deco elevation (run frame)

def deco_column(s, x0, x1, ks, reeds=3, bare=()):
    """Window column over floors ks: bronze spandrel with gold reeds and a limestone sill, glass film at 0.35 over the
    dark interior, one chrome mullion up the middle (DecoBay_10's language). Floors in `bare` get glass only."""
    for k in ks:
        zb = G + k * FH
        zs = zb + SP
        if k not in bare:
            s.box((x0, 0.3, zb), (x1, 1.2, zs), pal.Bronze)
            s.box((x0, 0.16, zs - 0.08), (x1, 0.4, zs), pal.Limestone_Tex)
            for i in range(reeds):
                x = x0 + (x1 - x0) * (i + 1) / (reeds + 1)
                s.box((x - 0.035, 0.26, zb + 0.16), (x + 0.035, 0.3, zs - 0.22), pal.Gold)
        s.face([(x0, 0.35, zs), (x1, 0.35, zs), (x1, 0.35, zb + FH), (x0, 0.35, zb + FH)], pal.GlassDark)
    xm = (x0 + x1) / 2
    s.box((xm - 0.03, 0.27, G + ks[0] * FH + SP), (xm + 0.03, 0.37, G + (ks[-1] + 1) * FH), pal.Chrome)


def trim(path, t):
    """Path with both ends moved inward along it by t (t < 0 extends)."""
    P = [Vector(p) for p in path]
    P[0] += (P[1] - P[0]).normalized() * t
    P[-1] += (P[-2] - P[-1]).normalized() * t
    return [tuple(p) for p in P]


def stepped_pier(s, path, z0, tiers, mat):
    """Pier wrapping a plan corner, with setbacks: tiers = [(a_out, a_in, end_trim, z_top)]; a gold band proud of every
    face under each step. Keep a_in under leg / tan(half turn) or the band folds."""
    for a0, a1, t, z1 in tiers:
        s.extrude(band(trim(path, t), a0, a1), z0, z1, mat)
        s.extrude(band(trim(path, t - 0.02), a0 - 0.02, a1 + 0.02), z1 - 0.3, z1 - 0.12, pal.Gold)
        z0 = z1


def sunburst(s, cx, y, zb, r, n=9, mat=None):
    """Half sunburst rising from a lintel's soffit line (facing -Y): gold half disc and n tapered rays."""
    mat = mat or pal.Gold
    with s.at((cx, y, zb), FRONT):
        s.extrude([(r * 0.28 * math.cos(math.pi * k / 12), r * 0.28 * math.sin(math.pi * k / 12)) for k in range(13)],
                  0.0, 0.06, mat)
        for k in range(n):
            a = math.pi * (k + 0.5) / n
            L = r * (1.0 if k % 2 == 0 else 0.72)
            c, sn, w = math.cos(a), math.sin(a), 0.07
            s.extrude(ccw([(r * 0.33 * c - w * sn, r * 0.33 * sn + w * c), (L * c, L * sn),
                           (r * 0.33 * c + w * sn, r * 0.33 * sn - w * c)]), 0.0, 0.04, mat)


# ----------------------------------------------------------------- portholes, starbursts, letters

def porthole(s, cx, cz, r, h, body, n=24, depth=0.4):
    """Round window in a square skin cell (half-size h) on the y = 0 plane: skin ring with the hole, reveal to depth,
    glass, dark interior disc, proud chrome rim. n must be a multiple of 8 so the cell keeps its corners."""
    ring = []
    for k in range(n):
        t = 2 * math.pi * k / n
        c, sn = math.cos(t), math.sin(t)
        m = max(abs(c), abs(sn))
        ring.append((cx + h * c / m, 0.0, cz + h * sn / m))
    hole = [(cx + r * math.cos(2 * math.pi * k / n), 0.0, cz + r * math.sin(2 * math.pi * k / n)) for k in range(n)]
    s._add(ring + hole, [[k, (k + 1) % n, n + (k + 1) % n, n + k] for k in range(n)], body, False)
    with s.at((cx, 0.0, cz), FRONT):
        s.lathe([(r, 0.0), (r, -depth)], pal.Enamel_Ivory, seg=n, cap_bottom=False, cap_top=False)
        s.lathe([(r + 0.14, 0.0), (r + 0.13, 0.05), (r + 0.07, 0.09), (r + 0.01, 0.06), (r - 0.01, -0.05)], pal.Chrome,
                seg=n, cap_bottom=False, cap_top=False)
    for y, mat in ((depth - 0.1, pal.GlassDark), (depth, pal.Enamel_Charcoal)):
        s.face([(x, y, z) for x, _, z in hole], mat)


def starburst(s, c, r, n=12, depth=0.05, mat=None):
    """Flat Googie starburst facing -Y at c: alternating long / short diamond rays round a chrome boss."""
    mat = mat or pal.Gold
    with s.at(c, FRONT):
        for k in range(n):
            a = 2 * math.pi * k / n + math.pi / n
            L, w = (r, r * 0.09) if k % 2 == 0 else (r * 0.6, r * 0.07)
            ca, sa = math.cos(a), math.sin(a)
            pts = [(0.12 * r * ca, 0.12 * r * sa), (0.4 * L * ca - w * sa, 0.4 * L * sa + w * ca), (L * ca, L * sa),
                   (0.4 * L * ca + w * sa, 0.4 * L * sa - w * ca)]
            s.extrude(ccw(pts), 0.0, depth, mat)
        s.lathe([(0.16 * r, 0.0), (0.16 * r, depth + 0.02), (0.08 * r, depth + 0.08), (0.0, depth + 0.1)], pal.Chrome,
                seg=12)


def sputnik(s, c, r, mat=None):
    """Chrome ball with radiating spikes (Googie sputnik)."""
    mat = mat or pal.Chrome
    with s.at(c):
        s.lathe([(r * 0.3 * math.sin(math.pi * k / 6), -r * 0.3 * math.cos(math.pi * k / 6)) for k in range(7)],
                mat, seg=12)
        for i in range(14):
            z = 1 - 2 * (i + 0.5) / 14
            a = i * 2.39996
            d = Vector((math.sqrt(1 - z * z) * math.cos(a), math.sqrt(1 - z * z) * math.sin(a), z))
            rot = d.to_track_quat("Z", "Y").to_euler()
            with s.at((0, 0, 0), [math.degrees(v) for v in rot]):
                s.lathe([(0.03 * r, 0.2 * r), (0.0, r)], pal.Gold, seg=4, cap_top=False)


GLYPH = {  # 3 x 5 stroke grid (x right, z up): the sign letters
    "H": ((0, 0, 1, 5), (2, 0, 3, 5), (1, 2, 2, 3)),
    "O": ((0, 0, 1, 5), (2, 0, 3, 5), (1, 0, 2, 1), (1, 4, 2, 5)),
    "T": ((0, 4, 3, 5), (1, 0, 2, 4)),
    "E": ((0, 0, 1, 5), (1, 0, 3, 1), (1, 2, 2.4, 3), (1, 4, 3, 5)),
    "L": ((0, 0, 1, 5), (1, 0, 3, 1)),
}


def letters(s, word, cx, z_top, h, mat, gap=0.35, depth=0.08):
    """Upright letters stacked downward from z_top, centred on x = cx, raised toward -Y from y = 0."""
    u = h / 5
    for i, ch in enumerate(word):
        z0 = z_top - (i + 1) * h - i * gap
        for a, b, c, d in GLYPH[ch]:
            s.box((cx + (a - 1.5) * u, -depth, z0 + b * u), (cx + (c - 1.5) * u, 0.0, z0 + d * u), mat)


# ----------------------------------------------------------------- engineered roof plant

def mast(s, x, y, z, h, beacon=True):
    """Chrome antenna whip on a steel collar, two cross-arm dipoles, red beacon on the tip."""
    with s.at((x, y, z)):
        s.lathe([(0.09, 0.0), (0.09, 0.25), (0.04, 0.3), (0.025, h), (0.0, h)], pal.Chrome, seg=6)
        for f, w in ((0.55, 0.45), (0.78, 0.3)):
            s.box((-w, -0.02, h * f), (w, 0.02, h * f + 0.04), pal.Steel)
        if beacon:
            s.lathe([(0.0, h - 0.02), (0.08, h), (0.08, h + 0.1), (0.0, h + 0.16)], pal.Signal_Red, seg=8)


def vent_bank(s, x, y, z, w=2.4, d=1.2, h=1.1, rot=0):
    """Louvred plant housing: steel-ivory box, louvre slats on both long faces, lid, fan cowl."""
    with s.at((x, y, z), (0, 0, rot)):
        s.box((-w / 2, -d / 2, 0.0), (w / 2, d / 2, h), pal.Steel_Ivory)
        s.box((-w / 2 - 0.05, -d / 2 - 0.05, h), (w / 2 + 0.05, d / 2 + 0.05, h + 0.08), pal.Steel)
        zz = 0.18
        while zz < h - 0.15:
            for y0 in (-d / 2 - 0.04, d / 2):
                s.box((-w / 2 + 0.12, y0, zz), (w / 2 - 0.12, y0 + 0.04, zz + 0.05), pal.Iron)
            zz += 0.12
        with s.at((w / 4, 0, h + 0.08)):
            s.lathe([(0.4, 0.0), (0.4, 0.28), (0.34, 0.33), (0.0, 0.33)], pal.Steel, seg=12, cap_bottom=False)


def roof_plant(s, x, y, z, rot=0, masts=(3.5, 2.2)):
    """Services cluster on a roof deck at z: vent bank, a round duct looping down into the deck, antenna masts."""
    vent_bank(s, x, y, z, rot=rot)
    with s.at((x, y, z), (0, 0, rot)):
        s.sweep(fillet([(-1.2, 0.0, 0.6), (-1.9, 0.0, 0.6), (-1.9, 0.0, -0.05)], 0.3, n=3, closed=False), 0.2,
                pal.Steel, seg=8)
        for i, h in enumerate(masts):
            mast(s, 0.9 - 1.3 * i, 0.9 + 0.4 * i, 0.0, h, beacon=i == 0)


def penthouse(s, x0, x1, y0, y1, z, h=2.4):
    """Lift / stair housing on a roof deck: rendered box, steel lid with a drip overhang, teal band, charcoal door and
    louvre on its -Y face."""
    s.box((x0, y0, z), (x1, y1, z + h), pal.Render_White)
    s.box((x0 - 0.12, y0 - 0.12, z + h), (x1 + 0.12, y1 + 0.12, z + h + 0.14), pal.Steel)
    s.box((x0 - 0.02, y0 - 0.02, z + h - 0.42), (x1 + 0.02, y1 + 0.02, z + h - 0.3), pal.Enamel_Teal)
    xm = (x0 + x1) / 2
    s.box((xm - 0.45, y0 - 0.04, z), (xm + 0.45, y0, z + 2.0), pal.Enamel_Charcoal)
    s.box((xm - 0.5, y0 - 0.08, z + 2.0), (xm + 0.5, y0, z + 2.08), pal.Chrome)
    for i in range(4):
        s.box((x1 - 0.6, y0 - 0.03, z + 0.5 + 0.14 * i), (x1 - 0.2, y0, z + 0.56 + 0.14 * i), pal.Iron)


def water_tank(s, x, y, z, r=0.75, h=1.5):
    """Riveted steel water tank on four legs with cross ties, domed lid, access ladder on its -Y side."""
    zl = z + 1.0
    with s.at((x, y, zl)):
        s.lathe([(0.0, 0.0), (r, 0.0), (r, h), (r * 0.7, h + 0.25), (0.0, h + 0.35)], pal.Steel_Ivory, seg=16)
        for zz in (0.3, h - 0.3):
            s.lathe([(r + 0.02, zz), (r + 0.02, zz + 0.06)], pal.Iron, seg=16, cap_bottom=False, cap_top=False)
    k = r * 0.62
    for dx, dy in ((k, k), (-k, k), (k, -k), (-k, -k)):
        s.box((x + dx - 0.05, y + dy - 0.05, z), (x + dx + 0.05, y + dy + 0.05, zl), pal.Iron)
    s.box((x - k, y - k - 0.02, z + 0.45), (x + k, y - k + 0.02, z + 0.5), pal.Iron)
    s.box((x - k, y + k - 0.02, z + 0.45), (x + k, y + k + 0.02, z + 0.5), pal.Iron)
    for dx in (-0.18, 0.18):
        s.box((x + dx - 0.02, y - r - 0.12, z), (x + dx + 0.02, y - r - 0.08, zl + h), pal.Steel)


def roof_deck(s, x0, x1, y0, y1, z, flip=False):
    """Roof deck services for a bay (x0..x1 wide, y0..y1 deep, deck at z): coping upstand, raised panel seams, a
    penthouse and a water tank at the back (sides swapped by `flip`), red obstruction lamps on the front corners."""
    for a, b in (((x0, y0), (x1, y0 + 0.15)), ((x0, y1 - 0.15), (x1, y1)),
                 ((x0, y0), (x0 + 0.08, y1)), ((x1 - 0.08, y0), (x1, y1))):
        s.box((a[0], a[1], z), (b[0], b[1], z + 0.3), pal.Limestone_Tex)
    for a, b in (((x0, y0 - 0.03), (x1, y0 + 0.18)), ((x0, y1 - 0.18), (x1, y1))):  # inside the cell
        s.box((a[0], a[1], z + 0.3), (b[0], b[1], z + 0.36), pal.Chrome)
    y = y0 + 1.6
    while y < y1 - 1.0:
        s.box((x0 + 0.08, y - 0.03, z), (x1 - 0.08, y + 0.03, z + 0.02), pal.Steel)
        y += 1.8
    sg = -1 if flip else 1
    xp = (x0 + 0.3, x0 + 2.3) if not flip else (x1 - 2.3, x1 - 0.3)
    penthouse(s, xp[0], xp[1], y1 - 3.4, y1 - 0.9, z)
    water_tank(s, (x1 + x0) / 2 + sg * 1.55, y1 - 2.1, z)
    for x in (x0 + 0.2, x1 - 0.2):
        with s.at((x, y0 + 0.08, z + 0.36)):
            s.lathe([(0.0, 0.0), (0.07, 0.0), (0.07, 0.1), (0.0, 0.17)], pal.Signal_Red, seg=8)


def zigzag_plate(s, pts, y0, y1, t, mat):
    """Folded plate: an (x, z) zig-zag, t thick below it with vertical ends, extruded from y0 (front) back to y1."""
    with s.at((0, 0, 0), FRONT):
        s.extrude(ccw(list(pts) + [(x, z - t) for x, z in pts[::-1]]), -y1, -y0, mat)


def rail_ring(s, c, r, z, rr, mat, n=32):
    """Closed round rail (sweep) of radius r about plan point c at height z."""
    s.sweep([(c[0] + r * math.cos(2 * math.pi * k / n), c[1] + r * math.sin(2 * math.pi * k / n), z)
             for k in range(n + 1)], rr, mat, seg=6, caps=False)
