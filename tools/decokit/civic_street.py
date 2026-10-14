"""Civic block street pieces (brief 008, B1): sphere bollard, striped parasol, play-zoom table set / sphere lamp / cypress,
modular planter runs (2 m, 4 m, corner, end x 3 plantings), square-bay shelter v2. LOD0 is the budget: the play camera
is orthographic and never switches LOD (Redline's lod-play-zoom.html), so every triangle here is drawn.

Budgets (parts list, 14 Oct): bollard <= 600, parasol <= 850, table set ~1,000, sphere lamp <= 2,500, cypress <= 1,500,
planter run <= 800, shelter <= 2,500."""

import math
import random
from contextlib import contextmanager

import spacehq  # noqa: F401  (registers DK_Enamel_Coral17 / Flamingo / Turquoise)
import plants
from geo import arc, ring, rrect
from furnish_cafe import tulip_chair, tulip_table
from furnish_parts import parasol
from kit import asset
from mats import pal
from plants import Cards, Z, frame, lerp, rot
from civic_common import VARIANTS, bloom, dome_lobe, hair, planting, trough
from structures import sec, superell, tube
from plaza_parts import lift


def sphere(s, c, r, mat, seg=20, n=6):
    prof = [(r * math.sin(math.pi * k / n), -r * math.cos(math.pi * k / n)) for k in range(n + 1)]
    with s.at(c):
        s.lathe(prof, mat, seg=seg)


# ----------------------------------------------------------------- BollardSphere

@asset("BollardSphere", "Street", bevel=0.0, lean=0.75)
def bollard_sphere(s):
    """Chrome post 1.05 m with a seafoam enamel band and a sphere top (mission-block, records-office paintings)."""
    s.lathe([(0, 0), (0.21, 0), (0.21, 0.03), (0.15, 0.055), (0.105, 0.085), (0.105, 0.72), (0.125, 0.735), (0.125, 0.775), (0.09, 0.8)],
            pal.Chrome, seg=20, cap_top=False)
    s.lathe(ring(0.104, 0.112, 0.42, 0.56), pal.Enamel_Seafoam, seg=20, cap_bottom=False, cap_top=False)
    sphere(s, (0, 0, 0.945), 0.145, pal.Chrome, seg=20, n=8)


# ----------------------------------------------------------------- Parasol_Stripe

@asset("Parasol_Stripe", "Furnish", bevel=0.0)
def parasol_stripe(s):
    """Seafoam and cream eight-panel parasol, 2.6 m across, 2.07 m clear, 2.6 m to the finial; the pole stands from the
    ground through the table top (pivot = pole). Place over CafeSet_Tulip_Play with the pole at the table centre."""
    R_, za, ze, drop = 1.3, 2.52, 2.2, 0.13
    s.lathe([(0.022, 0.0), (0.022, 2.54)], pal.Chrome, seg=8, cap_bottom=False)
    s.lathe([(0.022, 1.55), (0.045, 1.58), (0.045, 1.66), (0.022, 1.69)], pal.Chrome, seg=8, cap_bottom=False, cap_top=False)
    parasol(s, R_, za, ze, (pal.Cloth_Seafoam, pal.Cloth_Cream), n=8, drop=drop, nr=3, na=4)
    for i in range(8):
        a = 2 * math.pi * (i + 0.5) / 8
        s.sweep([(0.03 * math.cos(a), 0.03 * math.sin(a), za - 0.08), (R_ * 0.9 * math.cos(a), R_ * 0.9 * math.sin(a), ze + 0.04)],
                0.007, pal.Chrome, seg=4, caps=False)
    sphere(s, (0, 0, za + 0.07), 0.05, pal.Chrome, seg=10, n=4)
    s.lathe([(0.02, za + 0.09), (0.012, za + 0.14), (0.0, za + 0.15)], pal.Chrome, seg=6, cap_bottom=False)


# ----------------------------------------------------------------- CafeSet_Tulip_Play

@asset("CafeSet_Tulip_Play", "Furnish", bevel=0.0, lean=0.45, cap=1000)
def cafe_set_tulip_play(s):
    """CafeSet_Tulip at its play-zoom budget (about 1,000 tris, same footprint and heights): limestone-top tulip table and
    three shell chairs, cushions in the civic enamels (seafoam, deep teal, contract coral). Pivot = table centre."""
    tulip_table(s, top=pal.Limestone)  # the adopted terrazzo top is 24% under dE 20 from the guard colour per texel; at play zoom a limestone top reads the same
    for k, c in enumerate((pal.Enamel_Seafoam, pal.Enamel_Teal, pal.Enamel_Coral17)):
        a = 90 + 120 * k + 30
        with s.at((0.72 * math.cos(math.radians(a)), 0.72 * math.sin(math.radians(a)), 0), (0, 0, a - 90)):
            tulip_chair(s, c)


# ----------------------------------------------------------------- PlazaSphereLamp_Play

def pennant(s, x0, x1, zt, length, mat, y=0.0):
    """Hanging pennant in the XZ plane, V-cut foot, 2.4 cm thick."""
    xm, zv = (x0 + x1) / 2, zt - length + (x1 - x0) * 0.5
    with s.at((0, y, 0), (90, 0, 0)):
        s.extrude([(x0, zv), (xm, zt - length), (x1, zv), (x1, zt), (x0, zt)], -0.012, 0.012, mat)


@asset("PlazaSphereLamp_Play", "Signals", bevel=0.0)
def plaza_sphere_lamp_play(s):
    """PlazaSphereLamp at its play-zoom budget: same 4.6 m standard (teal sleeve, chrome pole, 0.45 m sphere, two arms
    at 3.8 m and 2.42 m) carrying a seafoam and a contract-coral pennant. Pivot = pole (arms reach +X 0.7 m)."""
    sq = lambda w, r=0.03: rrect(w, w, r, 2)  # noqa: E731
    s.loft([(sq(0.5, 0.05), 0), (sq(0.52, 0.06), 0.012), (sq(0.52, 0.06), 0.045), (sq(0.48, 0.05), 0.065)], pal.Chrome)
    s.loft([(sq(0.42), 0.05), (sq(0.42), 0.1), (sq(0.34), 0.78)], pal.Enamel_Teal, cap_bottom=False, cap_top=False)
    s.loft([(sq(0.34), 0.76), (sq(0.38), 0.77), (sq(0.38), 0.8), (sq(0.34), 0.83), (sq(0.2, 0.05), 0.87)], pal.Chrome)
    s.lathe([(0.11, 0.86), (0.09, 0.93), (0.065, 0.99), (0.065, 3.95)], pal.Chrome, seg=14, cap_bottom=False, cap_top=False)
    s.lathe(ring(0.066, 0.082, 1.8, 1.83), pal.Chrome, seg=14, cap_bottom=False, cap_top=False)
    s.lathe([(0.062, 3.93), (0.085, 3.95), (0.085, 4.07), (0.05, 4.12), (0.045, 4.14), (0, 4.14)], pal.Chrome, seg=14, cap_bottom=False)
    s.lathe(ring(0.05, 0.1, 4.1, 4.125), pal.Lamp_Deco, seg=14, cap_bottom=False, cap_top=False)
    sphere(s, (0, 0, 4.36), 0.225, pal.Chrome, seg=22, n=10)
    s.lathe([(0.012, 4.57), (0.01, 4.6), (0, 4.62)], pal.Chrome, seg=6, cap_bottom=False)
    for z, x1, r in ((3.8, 0.66, 0.016), (2.42, 0.62, 0.013)):
        s.sweep([(0.07, 0, z), (x1, 0, z)], r, pal.Chrome, seg=6)
        sphere(s, (x1 + r, 0, z), r * 1.9, pal.Chrome, seg=8, n=4)
    pennant(s, 0.12, 0.62, 3.78, 1.3, pal.Cloth_Seafoam)
    pennant(s, 0.12, 0.54, 2.4, 0.8, pal.Cloth_CoralSoft)


# ----------------------------------------------------------------- Cypress_Play

def card2(cd, rng, c, n, size, mat, nf, tilt=(-25, 25), roll=(-20, 20), uv=(0.0, 0.0, 1.0, 1.0), bend=0.22, up=Z, jit=0.3):
    """plants.card with one quad (2 tris) instead of a 3 x 3 grid (8 tris): same placement and bent normals."""
    j = type(c)((rng.gauss(0, 1), rng.gauss(0, 1), rng.gauss(0, 1))).normalized() * jit
    n, t, b = frame(n, up)
    a = rng.uniform(*tilt)
    n, t = rot(n, b, a), rot(t, b, a)
    r = rng.uniform(*roll)
    t, b = rot(t, n, r), rot(b, n, r)
    u0, v0, u1, v1 = uv
    if rng.random() < 0.5:
        u0, u1 = u1, u0
    h = size / 2
    P = [[c + b * (x * h) + t * (y * h) - n * (bend * 0.5 * h * (x * x + y * y)) for x in (-1, 1)] for y in (-1, 1)]
    UV = [[(lerp(u0, u1, (x + 1) / 2), lerp(v0, v1, (y + 1) / 2)) for x in (-1, 1)] for y in (-1, 1)]
    cd.grid(P, UV, [[(nf(p) + j).normalized() for p in row] for row in P], mat)


@contextmanager
def single_quad_cards():
    old = plants.card
    plants.card = card2
    try:
        yield
    finally:
        plants.card = old


@asset("Cypress_Play", "Plants", bevel=0.0)
def cypress_play(s):
    """Cypress at its play-zoom budget: the same 6.3 m profile and card layout as Cypress, one quad per foliage card instead of a 3 x 3 grid (2 tris instead of 8)."""
    rng = random.Random(4303)
    cd = Cards(s)
    s.lathe([(0.07, 0), (0.06, 0.4), (0, 0.45)], pal.Bark_Fibre, seg=8)
    prof = [(0.12, 0.22), (0.5, 0.45), (1.2, 0.56), (2.0, 0.58), (2.9, 0.52), (3.8, 0.41), (4.7, 0.28), (5.4, 0.16), (5.9, 0.07), (6.25, 0.0)]
    olive, cool = plants.tinted("DK_Leaf_CypressOlive"), pal.Leaf_Cypress
    with single_quad_cards():
        plants.conifer(cd, rng, prof, 0.72, ((1.0, 1.0, (olive,)), (0.93, 0.75, (olive,)), (0.8, 1.2, (cool, olive))), lump=0.09)


# ----------------------------------------------------------------- PlanterRun

def _centre(path, w=0.62):
    xs, ys = [p[0] for p in path], [p[1] for p in path]
    return (min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2


def _run(kind, variant):
    def build(s):
        if kind == "2":
            path = [(-1.0, 0.0), (1.0, 0.0)]
        elif kind == "4":
            path = [(-2.0, 0.0), (0.0, 0.0), (2.0, 0.0)]
        elif kind == "Corner":
            path = [(-1.0, -1.0), (0.0, -1.0)] + arc(0, 0, 1.0, 270, 360, 6)[1:] + [(1.0, 1.0)]
        else:  # End: a straight run closed by a half-round end
            path = [(-1.0, 0.0), (0.0, 0.0), (0.3, 0.0)]
        cx, cy = _centre(path)
        with s.at((-cx, -cy, 0)):
            trough(s, path)
            planting(s, path, variant, **{"2": dict(step=0.5), "4": dict(step=0.52, blooms=2), "Corner": dict(step=0.62, blooms=2), "End": dict(step=0.5)}[kind])
            if kind == "End":
                ex = path[-1][0]
                with s.at((ex, 0, 0)):
                    s.lathe([(0.3, 0), (0.3, 0.6)], pal.Limestone, seg=14, cap_bottom=False, cap_top=False)
                    s.lathe([(0.34, 0.6), (0.34, 0.655), (0.17, 0.655), (0.17, 0.6)], pal.Limestone, seg=14, cap_bottom=False, cap_top=False)
                    s.lathe([(0.34, 0), (0.34, 0.1)], pal.Limestone, seg=14, cap_bottom=False, cap_top=False)
                    dome_lobe(s, (0, 0, 0.7), 0.3, 0.3, pal.Leaf_Civic if variant == "A" else pal.Leaf_CivicDark)
                    bloom(s, (0.02, 0, 0.98), 0.1, getattr(pal, "Bloom_Coral" if variant == "A" else "Bloom_Flamingo" if variant == "B" else "Bloom_Cream"))
    return build


for _k in ("2", "4", "Corner", "End"):
    for _v in "ABC":
        asset(f"PlanterRun_{_k}_{_v}", "Plaza", bevel=0.0)(_run(_k, _v))


# ----------------------------------------------------------------- BoardingPavilion_Square_v2

@asset("BoardingPavilion_Square_v2", "Plaza", bevel=0.0)
def boarding_square_v2(s):
    """civic-block square-boarding-pavilion at its locked footprint (8.4 x 2.14, roof top 2.587), long along X: a lens-shaped
    cream roof with a seafoam fascia, a gold bead and a lamp strip, on two chrome posts (the block's two PavilionPier_Square
    piers are the other two supports, at local y -1.07..0.05, x +-3.22 +- 0.56; the chrome posts stand at y +0.68). Pivot = footprint centre."""
    L, D, H = 8.4, 2.14, 2.587
    FH = 0.36  # fascia height: the painting's roof is a thick shell with a broad seafoam edge
    pts = superell(L / 2 - 0.05, D / 2 - 0.05, 2.6, 28)  # trim and bead add 5 cm: the box is 8.4 x 2.14
    scaled = lambda k: [(x * k, y * (1 - (1 - k) * 0.45)) for x, y in pts]  # noqa: E731
    s.loft([(scaled(1.0), H - FH), (scaled(1.0), H - 0.14), (scaled(0.95), H - 0.07), (scaled(0.82), H - 0.02), (scaled(0.64), H)], pal.Enamel_Ivory)
    tube(s, lift(pts), sec(-0.03, 0.0, H - FH - 0.01, H - 0.1), pal.Enamel_Seafoam, closed=True)
    hair(s, pts, -0.03, H - FH - 0.01, 0.035, pal.Brass_Civic, closed=True)
    hair(s, pts, -0.03, H - 0.1, 0.03, pal.Brass_Civic, closed=True)
    s.box((-3.0, -0.12, H - FH - 0.012), (3.0, 0.12, H - FH), pal.Lamp_Deco)
    for x in (-3.22, 3.22):
        with s.at((x, 0.68, 0)):
            s.lathe([(0, 0), (0.17, 0), (0.17, 0.035), (0.085, 0.07), (0.055, 0.12), (0.055, H - FH)], pal.Chrome, seg=10, cap_top=True)


# ----------------------------------------------------------------- PottedTopiary_Play

@asset("PottedTopiary_Play", "Plaza", bevel=0.0)
def potted_topiary_play(s):
    """Cone topiary in a cream pot, 1.9 m (gp-entrance, mission-block: pairs at the doors): a limestone pot 0.67 m across, three
    clipped tiers of civic green with a tip. Smooth lathes in flat civic leaf colours, no textures. Pivot = pot centre."""
    s.lathe([(0, 0), (0.19, 0), (0.21, 0.04), (0.28, 0.14), (0.30, 0.52), (0.335, 0.55), (0.335, 0.62), (0.285, 0.62), (0.285, 0.57)],
            pal.Limestone, seg=16, cap_top=False)
    for k, (zb, h, R_) in enumerate(((0.56, 0.62, 0.36), (0.98, 0.55, 0.27), (1.37, 0.5, 0.18))):
        prof = [(0.0, zb), (0.72 * R_, zb), (R_, zb + 0.16 * h), (0.94 * R_, zb + 0.5 * h), (0.55 * R_, zb + 0.88 * h), (0.0, zb + h + 0.08 * (k == 2) * 2)]
        s.lathe(prof, pal.Leaf_CivicDark if k % 2 == 0 else pal.Leaf_Civic, seg=12)


# ----------------------------------------------------------------- BedTuft (civic planting beds, 14 Oct)

def _tuft(variant):
    """One clipped-hedge cushion with a few blooms, 0.62 m across: the unit the civic beds are planted with, instanced along any
    ring (spacing 0.55 m: neighbours overlap into a scalloped hedge). Pivot = the base centre, which stands on the bed fill. A, B, C
    are the planter runs' three plantings (coral + cream, flamingo + peach, cream + peach), so beds and troughs match."""
    def build(s):
        v = VARIANTS[variant]
        rng = random.Random(v["seed"] * 7 + 3)
        leaf = getattr(pal, v["leaf"][0])
        dome_lobe(s, (0, 0, 0), 0.31, v["h"] * 1.15, leaf, seg=10, n=3)
        for k in range({"A": 3, "B": 3, "C": 2}[variant]):
            a = rng.uniform(0, 2 * math.pi) + k * 2.1
            r = rng.uniform(0.05, 0.17)
            bloom(s, (r * math.cos(a), r * math.sin(a), v["h"] * rng.uniform(0.88, 1.0)), rng.uniform(0.10, 0.13), getattr(pal, v["bloom"][k % 2]), seg=5)
    return build


for _v in "ABC":
    asset(f"BedTuft_{_v}", "Plaza", bevel=0.0)(_tuft(_v))
