"""Ground kit (reference sheet row 1 right, row 2 left, row 5 middle; plaza.png): 2 x 2 m paving modules, kerb,
manhole cover, drain grate.

Grid: module pivot = centre at z = 0; walk surface z = 0.15 (kerb height), road / tram surface z = 0. Every slab is
its own piece stopping J short of each cell line, so joints are 2 J = 4 mm everywhere, including across module
edges: modules tile edge to edge with nothing past +-1 m. Slabs sit on a mortar bed; a grout sheet 4 mm below the
surface fills the joints so they read as dark lines.
Terrazzo bands: inner / outer radius BAND about a module corner (four Tile_TerrazzoArc make a circle); the straight
Tile_Terrazzo band sits at the same offset, so it continues the arc tangentially across any module edge.
Kerb line = grid line: Tile_CornerCurb's arc and Tile_Tactile's steel nosing drop at the module edge; Kerb_Straight's
pivot is that line (stone on the walk side, gutter on the road side, 4 mm proud so it can overlay a paving module).
"""

import math

from mathutils import Vector
from mathutils.geometry import tessellate_polygon

import mats
from geo import arc, circle, rrect, stadium
from kit import asset
from mats import pal, register

# satin metal: flat castings / grates read as brushed silver instead of mirror-black (a flat mirror only shows
# the horizon). setdefault so a later mats.KIND entry of the same name wins.
mats.KIND.setdefault("satin", (0.5, 1.0, 0.0, 0.0, 1.0))
register("DK_Steel_Satin", "satin", "#B9BBBE")
register("DK_Iron_Cast", "satin", "#8C8F93")
register("DK_Concrete", "stone", "#B5AEA1")

J = 0.002  # half joint
TOP, BED = 0.15, 0.03  # walk surface, mortar bed top
BAND = (1.25, 1.6)  # seafoam band radii about the arc module's corner
BRASS = 0.006  # half width of the gold divider strips
SIDE = (90, 0, 90)  # local X -> world Y, local Y -> world Z, extrusion -> world X


# ----------------------------------------------------------------- 2D helpers

def rect(x0, y0, x1, y1):
    return [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]


def _dedupe(poly, eps=1e-6):
    out = []
    for p in poly:
        if not out or (Vector(p) - Vector(out[-1])).length > eps:
            out.append(tuple(p))
    if len(out) > 2 and (Vector(out[0]) - Vector(out[-1])).length <= eps:
        out.pop()
    return out


def _area(poly):
    return 0.5 * sum(x0 * y1 - x1 * y0 for (x0, y0), (x1, y1) in zip(poly, poly[1:] + poly[:1]))


def inset(poly, g):
    """Pull a CCW polygon in by g (a distance, or fn(p, q) -> distance per edge), mitred corners."""
    P = [Vector(p) for p in _dedupe(poly)]
    n = len(P)
    lines = []
    for i in range(n):
        p, q = P[i], P[(i + 1) % n]
        d = (q - p).normalized()
        lines.append((p + Vector((-d.y, d.x)) * (g(p, q) if callable(g) else g), d))
    out = []
    for i in range(n):
        (a, da), (b, db) = lines[i - 1], lines[i]
        cr = da.x * db.y - da.y * db.x
        if abs(cr) < 1e-9:
            out.append(tuple(b))
        else:
            ab = b - a
            out.append(tuple(a + da * ((ab.x * db.y - ab.y * db.x) / cr)))
    return out


def clip_circle(poly, c, r, keep_in=True, step=3.0):
    """Clip a CCW polygon to the inside (or outside) of a circle; cut spans are replaced by arcs."""
    C = Vector(c)
    P = [Vector(p) for p in poly]
    kept = lambda p: ((p - C).length < r) == keep_in
    k = next((i for i, p in enumerate(P) if kept(p)), None)
    if k is None:
        return []
    P = P[k:] + P[:k]
    ang = lambda p: math.atan2(p.y - C.y, p.x - C.x)
    out, state, ea, n = [P[0]], True, 0.0, len(P)
    for i in range(n):
        p, q = P[i], P[(i + 1) % n]
        d, f = q - p, p - C
        a, b, cc = d.dot(d), 2 * f.dot(d), f.dot(f) - r * r
        disc = b * b - 4 * a * cc
        ts = []
        if disc > 0:
            sq = math.sqrt(disc)
            ts = sorted(t for t in ((-b - sq) / (2 * a), (-b + sq) / (2 * a)) if 1e-9 < t < 1 - 1e-9)
        for t in ts:
            x = p + d * t
            if not state:  # re-entry: follow the circle (short way) from the exit point
                da = (ang(x) - ea + math.pi) % (2 * math.pi) - math.pi
                m = max(1, math.ceil(abs(math.degrees(da)) / step))
                out += [C + r * Vector((math.cos(ea + da * j / m), math.sin(ea + da * j / m))) for j in range(1, m)]
            else:
                ea = ang(x)
            out.append(x)
            state = not state
        if state and i < n - 1:
            out.append(q)
    return [tuple(v) for v in out]


def sector(c, r0, r1, a0, a1, step=3.0):
    """CCW annular sector about c (r0 = 0 gives a pie wedge), angles in degrees."""
    n = max(2, math.ceil(abs(a1 - a0) / step))
    return arc(c[0], c[1], r1, a0, a1, n) + ([tuple(c)] if r0 < 1e-6 else arc(c[0], c[1], r0, a1, a0, n))


def grid_pts(x0, y0, x1, y1, p=0.09, m=0.045):
    nx, ny = int((x1 - x0 - 2 * m) / p) + 1, int((y1 - y0 - 2 * m) / p) + 1
    ox, oy = (x0 + x1 - (nx - 1) * p) / 2, (y0 + y1 - (ny - 1) * p) / 2
    return [(ox + i * p, oy + j * p) for i in range(nx) for j in range(ny)]


# ----------------------------------------------------------------- solid helpers

def slab(s, poly, mat, z0=BED, z1=TOP, g=J):
    """One paving piece: polygon pulled in by the half joint; bottom hidden on the bed."""
    s.extrude(inset(poly, g), z0, z1, mat, bottom=False)


def bed(s, poly=None, z0=0.0, z1=BED):
    s.extrude(poly or rect(-1, -1, 1, 1), z0, z1, pal.Stone_Dark)


def grout(s, poly=None, top=TOP):
    """Dark grout sheet 4 mm under the surface: only the joints see it, so the grid reads as dark lines."""
    s.extrude(poly or rect(-1, -1, 1, 1), top - 0.01, top - 0.004, pal.Stone_Dark, bottom=False)


def plate(s, outer, holes, z0, z1, mat, bottom=False):
    """Flat plate with real holes (tessellated caps + walls on every loop)."""
    loops = [_dedupe(outer)] + [_dedupe(h) for h in holes]
    loops = [L if (_area(L) > 0) == (i == 0) else L[::-1] for i, L in enumerate(loops)]
    pts = [p for L in loops for p in L]
    n = len(pts)
    verts = [(x, y, z1) for x, y in pts] + [(x, y, z0) for x, y in pts]
    faces = []
    for tri in tessellate_polygon([[Vector((x, y, 0)) for x, y in L] for L in loops]):
        tri = list(tri) if _area([pts[i] for i in tri]) > 0 else list(tri)[::-1]
        faces.append(tri)
        if bottom:
            faces.append([n + i for i in tri[::-1]])
    o = 0
    for L in loops:
        m = len(L)
        faces += [[n + o + i, n + o + (i + 1) % m, o + (i + 1) % m, o + i] for i in range(m)]
        o += m
    s._add(verts, faces, mat, False)


def revolve(s, prof, c, a0, a1, mat, g=J, step=3.0):
    """Closed CCW (r, z) profile swept about a vertical axis at c from a0 to a1 (deg, a1 > a0), capped.
    Ends are pulled in by g as parallel cuts, so an end lying on a module edge stays on it."""
    n, m = max(2, math.ceil((a1 - a0) / step)), len(prof)
    verts = []
    for k in range(n + 1):
        for r, z in prof:
            a = math.radians(a0 + (a1 - a0) * k / n)
            a += math.asin(g / r) if k == 0 else -math.asin(g / r) if k == n else 0.0
            verts.append((c[0] + r * math.cos(a), c[1] + r * math.sin(a), z))
    faces = [[k * m + j, (k + 1) * m + j, (k + 1) * m + (j + 1) % m, k * m + (j + 1) % m]
             for k in range(n) for j in range(m)]
    faces += [list(range(m)), [n * m + j for j in range(m)][::-1]]
    s._add(verts, faces, mat, True)


STUD = [(1.0, -0.002), (0.975, 0.47), (0.88, 0.73), (0.69, 0.9), (0.38, 0.99), (0.0, 1.0)]  # (r, z) / (R, H)


def blister(s, x, y, z, mat, r=0.021, h=0.015, seg=10):
    """Tactile stud (90 tris): near-vertical flank, rounded crown (every ring turn < 38 deg, so no modifier bevel);
    base sunk 2 mm into the slab. The flank is what catches light and throws the stud's shadow."""
    with s.at((x, y, z)):
        s.lathe([(r * a, b if b < 0 else h * b) for a, b in STUD], mat, seg=seg, cap_bottom=False)


def crown(w0, w1, z_edge, z_mid, n=6):
    """Parabolic crowned top from w0 to w1 (rail heads catch a highlight instead of mirroring the horizon)."""
    return [(w0 + (w1 - w0) * k / n, z_edge + (z_mid - z_edge) * (1 - (2 * k / n - 1) ** 2)) for k in range(n + 1)]


def kerb_prof(r0, r1, z1, rn=0.05):
    """(r, z) CCW kerb section: flat back at r0, bullnose on the outer top edge, face at r1 down to z = 0."""
    return [(r0, 0.0), (r1, 0.0)] + arc(r1 - rn, z1 - rn, rn, 0, 90, 6) + [(r0, z1)]


# ----------------------------------------------------------------- assets

@asset("Tile_TramRail", "Ground", bevel=0.012)
def tile_tram_rail(s):
    """Road-level module (top z = 0, body to -0.15): one track along X, rail-head centrelines at y = +-0.5 (the tram's
    wheel treads), grooved rails flush in grey track slabs, ivory outer slabs, mint + red edge strips on both sides."""
    zb, zt, gf = -0.12, 0.0, 0.4625  # gf = gauge face (inner edge of the 75 mm head)
    bed(s, z0=-0.15, z1=zb)
    for sg in (1, -1):
        y0, y1 = sorted((sg * 0.8, sg * 1.0))
        s.rbox((-1 + J, y0 + J, zb), (-0.6 - J, y1 - J, zt), 0.022, pal.Tile_Mint)
        s.rbox((-0.6 + J, y0 + J, zb), (1 - J, y1 - J, zt), 0.022, pal.Tile_Red)
        y0, y1 = sorted((sg * (gf + 0.095), sg * 0.8))
        for x0 in (-1, 0):
            slab(s, rect(x0, y0, x0 + 1, y1), pal.Tile_Ivory, zb, zt)

        def part(pts, mat, sg=sg):  # (w, z) section; w = distance inward (toward y = 0) from the gauge face
            poly = [(sg * (gf - w), z) for w, z in pts]
            with s.at((0, 0, 0), SIDE):
                s.extrude(poly if _area(poly) > 0 else poly[::-1], -1, 1, mat, smooth_sides=True)
        part(rect(-0.095, -0.15, 0.085, -0.04), pal.Steel_Satin)  # web + foot block
        part(rect(-0.095, -0.04, -0.075, zt), pal.Steel_Satin)  # outer keeper flat
        part([(-0.075, -0.04), (0.0, -0.04)] + crown(0.0, -0.075, -0.005, zt), pal.Steel)  # crowned head
        part(rect(0.0, -0.04, 0.035, -0.037), pal.Iron)  # flangeway groove floor
        part([(0.035, -0.04), (0.065, -0.04)] + crown(0.065, 0.035, -0.004, -0.001, 4), pal.Steel)  # guard lip
        part(rect(0.065, -0.04, 0.085, zt), pal.Steel_Satin)  # inner keeper flat
    yi = gf - 0.085
    for y0, y1 in ((gf + 0.095, 1), (-yi, yi), (-1, -gf - 0.095)):
        grout(s, rect(-1, y0, 1, y1), zt)
    for (y0, y1), cuts in (((0.0, yi), (-1, -0.5, 0.5, 1)), ((-yi, 0.0), (-1, 0, 1))):  # running bond
        for x0, x1 in zip(cuts, cuts[1:]):
            slab(s, rect(x0, y0, x1, y1), pal.Tile_Grey, zb, zt)


@asset("Tile_Tactile", "Ground", bevel=0.012)
def tile_tactile(s):
    """Sidewalk module at a crossing: ivory slabs (back), powder-blue panel, red blister strip on a satin-steel tray
    with a polished-steel nosing along the -Y (kerb) edge."""
    bed(s)
    grout(s)
    for r in ((-1, 0.25, -0.55, 1), (-1, -0.35, -0.55, 0.25), (-0.55, 0.25, 0, 0.625), (-0.55, 0.625, 0, 1),
              (0, 0.25, 1, 0.625), (0, 0.625, 1, 1)):
        slab(s, rect(*r), pal.Tile_Ivory)
    slab(s, rect(-0.55, -0.35, 1, 0.25), pal.Tile_Blue)
    s.box((-1, -0.988, BED), (1, -0.35 - J, 0.12), pal.Steel_Satin)
    s.box((-1, -1, 0), (1, -0.988, TOP), pal.Steel)
    for x0, x1 in ((-1, -0.55), (-0.55, 1)):
        slab(s, rect(x0, -0.988, x1, -0.35), pal.Tile_Red, z0=0.12)
        for x, y in grid_pts(x0, -0.988, x1, -0.35):
            blister(s, x, y, TOP, pal.Tile_Red)


@asset("Tile_CornerCurb", "Ground", bevel=0.012)
def tile_corner_curb(s):
    """Quarter-round corner: R = 2 m about the inner corner (-1, +1); kerb ring (0.3 m, bullnose) drops to road
    level at the arc, which is tangent to the grid lines y = -1 and x = +1 where straight kerbs continue."""
    c = (-1.0, 1.0)
    bed(s, sector(c, 0, 1.7, 270, 360))
    grout(s, sector(c, 0, 1.96, 270, 360))  # runs under the kerb ring too (stops inside the bullnose)
    for (a0, a1), mat in zip(((270, 300), (300, 330), (330, 360)), (pal.Tile_Grey, pal.Limestone_Tex,
                                                                    pal.Limestone_Tex)):
        revolve(s, kerb_prof(1.7 + J, 2.0, TOP), c, a0, a1, mat)
    for r0, r1, a0, a1, mat in ((0, 1, 270, 315, pal.Tile_Ivory), (0, 1, 315, 360, pal.Tile_Grey),
                                (1, 1.7, 270, 280, pal.Tile_Ivory), (1, 1.7, 280, 306, pal.Tile_Red),
                                (1, 1.7, 306, 360, pal.Tile_Blue)):
        slab(s, sector(c, r0, r1, a0, a1), mat)
    for i in range(7):  # blister rows on constant radii across the red sector
        r = 1.08 + 0.09 * i
        n = int((r * math.radians(26) - 0.09) / 0.09) + 1
        for k in range(n):
            a = math.radians(293) + (k - (n - 1) / 2) * 0.09 / r
            blister(s, c[0] + r * math.cos(a), c[1] + r * math.sin(a), TOP, pal.Tile_Red)


@asset("Tile_Plain", "Ground", bevel=0.012)
def tile_plain(s):
    """Sidewalk module: four 1 x 1 m glazed ivory slabs."""
    bed(s)
    grout(s)
    for x0 in (-1, 0):
        for y0 in (-1, 0):
            slab(s, rect(x0, y0, x0 + 1, y0 + 1), pal.Tile_Ivory)


def _brass_edge(on_line):
    return lambda p, q: BRASS if on_line(p) and on_line(q) else J


@asset("Tile_Terrazzo", "Ground", bevel=0.005)
def tile_terrazzo(s):
    """Plaza module: 1 m ivory terrazzo slabs, seafoam band along X at y = -0.6..-0.25 (the arc's offset) framed
    by flush gold divider strips."""
    bed(s)
    grout(s)
    yb = (1 - BAND[1], 1 - BAND[0])
    g = _brass_edge(lambda p: any(abs(p.y - v) < 1e-6 for v in yb))
    for y0, y1, mat in ((-1, yb[0], pal.Terrazzo_Ivory), (yb[0], yb[1], pal.Terrazzo_Seafoam),
                        (yb[1], 0, pal.Terrazzo_Ivory), (0, 1, pal.Terrazzo_Ivory)):
        for x0 in (-1, 0):
            slab(s, rect(x0, y0, x0 + 1, y1), mat, g=g)
    for y in yb:
        s.box((-1, y - BRASS, 0.1), (1, y + BRASS, TOP + 0.0005), pal.Gold)


@asset("Tile_TerrazzoArc", "Ground", bevel=0.005)
def tile_terrazzo_arc(s):
    """Plaza module: quarter-circle seafoam band (r = 1.25..1.6) about the +X+Y corner, gold strips on both
    edges, ivory terrazzo cut on the same 1 m grid as Tile_Terrazzo."""
    c = Vector((1.0, 1.0))
    bed(s)
    grout(s)
    g = _brass_edge(lambda p: any(abs((Vector(p) - c).length - r) < 1e-4 for r in BAND))
    for x0 in (-1, 0):
        for y0 in (-1, 0):
            cell = rect(x0, y0, x0 + 1, y0 + 1)
            parts = ((clip_circle(cell, c, BAND[0]), pal.Terrazzo_Ivory),
                     (clip_circle(clip_circle(cell, c, BAND[1]), c, BAND[0], False), pal.Terrazzo_Seafoam),
                     (clip_circle(cell, c, BAND[1], False), pal.Terrazzo_Ivory))
            for poly, mat in parts:
                if len(poly) >= 3 and _area(poly) > 1e-4:
                    slab(s, poly, mat, g=g)
    for r in BAND:
        revolve(s, [(r - BRASS, 0.1), (r + BRASS, 0.1), (r + BRASS, TOP + 0.0005), (r - BRASS, TOP + 0.0005)],
                c, 180, 270, pal.Gold, g=0.0)


@asset("Kerb_Straight", "Ground", bevel=0.008)
def kerb_straight(s):
    """2 m run of two 1 m limestone kerb stones (bullnose, 0.3 m deep, walk side +Y) and a dished concrete gutter
    (road side -Y). Pivot = kerb line at road level; stone 4 mm proud so it can overlay a paving module's edge."""
    yf, zt, rn = -0.004, TOP + 0.004, 0.05
    stone = [(yf, 0.0), (0.3, 0.0), (0.3, zt)] + arc(yf + rn, zt - rn, rn, 90, 180, 6)
    gutter = [(-0.3, 0.0), (yf, 0.0), (yf, 0.003), (-0.07, 0.0015), (-0.3, 0.005)]
    with s.at((0, 0, 0), SIDE):
        for x0 in (-1, 0):
            s.extrude(stone, x0 + J, x0 + 1 - J, pal.Limestone_Tex)
            s.extrude(gutter, x0 + J, x0 + 1 - J, pal.Concrete)
    s.box((-J, 0.02, 0.0), (J, 0.3, zt - 0.004), pal.Stone_Dark)  # grouted joint between the two stones


@asset("ManholeCover", "Ground", bevel=0.0)
def manhole_cover(s):
    """0.8 m cover: chunky polished rim and a raised concentric + staggered radial rib web (rounded sections, so no
    bevel modifier) on a satin field, seated in a square cast-iron frame; top z = 0.02."""
    plate(s, rrect(0.88, 0.88, 0.025), [circle(0.403, 72)], 0.0, 0.014, pal.Iron_Cast)
    s.lathe([(0.35, 0.0), (0.402, 0.0), (0.402, 0.009), (0.397, 0.0155), (0.388, 0.0192), (0.376, 0.02),
             (0.364, 0.0192), (0.356, 0.017), (0.35, 0.0155), (0.35, 0.0)], pal.Steel, seg=72, cap_bottom=False,
            cap_top=False)
    s.lathe([(0.351, 0.0), (0.351, 0.0135), (0, 0.0135)], pal.Steel_Satin, seg=72, cap_bottom=False)
    s.lathe([(0.04, 0.012), (0.04, 0.016), (0.03, 0.0193), (0.012, 0.02), (0, 0.02)], pal.Steel, seg=32,
            cap_bottom=False)
    radii, w = (0.068, 0.128, 0.186, 0.242, 0.298, 0.351), 0.01
    for r in radii[:-1]:
        s.lathe([(r + w / 2, 0.012), (r + w / 2, 0.0172), (r + w / 4, 0.0195), (r - w / 4, 0.0195),
                 (r - w / 2, 0.0172), (r - w / 2, 0.012)], pal.Steel, seg=32 if r < 0.15 else 56,
                cap_bottom=False, cap_top=False)
    rib = [(-w * 0.4, 0.012), (w * 0.4, 0.012), (w * 0.4, 0.0172), (w * 0.2, 0.0194), (-w * 0.2, 0.0194),
           (-w * 0.4, 0.0172)]
    for i, count in enumerate((8, 12, 16, 22, 28)):
        ra, rb = radii[i] + w / 2 - 0.002, radii[i + 1] - w / 2 + 0.002
        for k in range(count):
            with s.at((0, 0, 0), (0, 0, (k + 0.5 * (i % 2)) * 360 / count)), s.at((0, 0, 0), SIDE):
                s.extrude(rib, ra, rb, pal.Steel, smooth_sides=True, top=False, bottom=False)


@asset("DrainGrate", "Ground", bevel=0.003)
def drain_grate(s):
    """1.0 x 0.6 m steel grate with eight open stadium slots over a dark catch pan, in a polished flange frame
    with four corner bolts; top z = 0.02."""
    plate(s, rrect(1.0, 0.6, 0.035), [rrect(0.884, 0.484, 0.016)], 0.0, 0.016, pal.Steel)
    slots = [[(x + 0.1 * i - 0.35, y) for x, y in stadium(0.05, 0.36, n=8)] for i in range(8)]
    plate(s, rrect(0.88, 0.48, 0.014), slots, 0.004, 0.018, pal.Steel_Satin)
    plate(s, rrect(0.88, 0.48, 0.014), [rrect(0.83, 0.43, 0.01)], 0.017, 0.02, pal.Steel)  # raised grate border
    s.extrude(rrect(0.884, 0.484, 0.016), 0.0, 0.002, pal.Iron, bottom=False)  # catch pan: slots read dark
    hexa = [(0.0042 * math.cos(math.radians(60 * k)), 0.0042 * math.sin(math.radians(60 * k))) for k in range(6)]
    for x in (-0.471, 0.471):
        for y in (-0.271, 0.271):
            with s.at((x, y, 0.0)):
                s.lathe([(0.012, 0.015), (0.012, 0.0165), (0.008, 0.0192), (0, 0.0195)], pal.Chrome, seg=16,
                        cap_bottom=False)
                s.extrude(hexa, 0.0188, 0.02, pal.Iron, bottom=False)


# ----------------------------------------------------------------- large inlays (plaza.png sweeping bands)

INLAY_SPAN, INLAY_W, INLAY_UP = 15.0, 0.7, 0.003  # sector degrees, band width, height proud of the walk surface


def _inlay_arc(radius):
    def build(s):
        """Overlay piece for plaza-scale arcs: one 15 deg sector of seafoam terrazzo band (0.7 m wide, gold edges),
        3 mm proud of the paving like Kerb_Straight. Pivot = ring centre (not the footprint), sector centred on +X:
        yaw in 15 deg steps about one pivot lays any sweep, joints landing 4 mm apart."""
        r0, r1, a = radius - INLAY_W / 2, radius + INLAY_W / 2, INLAY_SPAN / 2
        z0, z1 = TOP - 0.001, TOP + INLAY_UP
        slab(s, sector((0, 0), r0, r1, -a, a, 1.0), pal.Terrazzo_Seafoam, z0=z0, z1=z1)
        for r in (r0 + BRASS + J, r1 - BRASS - J):
            revolve(s, [(r - BRASS, z0), (r + BRASS, z0), (r + BRASS, z1 + 0.0005), (r - BRASS, z1 + 0.0005)],
                    (0, 0), -a, a, pal.Gold, step=1.0)
    return build


for _r in (6, 10, 16):  # no bevel: on a 3 mm lip it only doubles the tris
    asset(f"InlayArc_R{_r}", "Ground", bevel=0.0)(_inlay_arc(_r))
