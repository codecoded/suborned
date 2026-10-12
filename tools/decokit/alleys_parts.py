"""Alleys family helpers (alleys.py, alleys_span.py, alleys_props.py): the service-wall slab with real openings and
panel seams, and its fixtures (porthole and service windows, louvre banks, rounded AC units, riser duct and duct runs,
drainpipe, door with canopy, catwalk), the caged lamp, antenna cluster, chrome rails and catenary cables.

Wall frame (skyline.py turned into the alley): face -Y, x across the 6 m bay, z up from the walk surface. The render
face sits at y = SKIN, 2 cm proud of the pivot line, so a plain return face left on that line never z-fights.
Segment counts are set for the alley camera (30-60 m): walls stay under 8k tris with dense service detail."""

import math

from mathutils import Vector

from booths import bezel
from geo import dome, fillet, stadium
from mats import pal, register
from mega_parts import strut
from structures import sec, soft_extrude, tube
from vehicles_parts import carve

register("DK_Seam", "stone", "#8E8472")          # panel-seam groove floor: a shade under the limestone
register("DK_Blind_Coral", "fabric", "#E58A6E")  # half-drawn blinds: the lived-in windows
register("DK_Blind_Cream", "fabric", "#F0E2C2")

FRONT = (90, 0, 0)    # local XY -> world XZ, local +Z (lathe axis / extrusion) -> world -Y
BACK = (-90, 0, 0)    # local +Z -> world +Y
SIDE = (90, 0, 90)    # local X -> world Y, local Y -> world Z, extrusion -> world X
ALONG_X = (0, 90, 0)  # local +Z -> world +X
HW, SKIN, BACKY, REVEAL = 3.0, -0.02, 0.35, 0.2  # half bay, face plane, slab back, window pocket depth


def straight(y=0.0, z=0.0, x0=-HW, x1=HW):
    return [(x0, y, z), (x1, y, z)]


def band(s, z0, z1, a0, mat, r=0.02):
    """Full-width course standing a0 proud of the face (tube section: a = inward +Y, b = z; chamfered edges)."""
    tube(s, straight(), sec(SKIN - a0, 0.05, z0, z1, r, n=1), mat)


def sag(p0, p1, dz, n=12):
    """Parabolic catenary from p0 to p1 hanging dz at mid-span."""
    a, b = Vector(p0), Vector(p1)
    return [tuple(a.lerp(b, i / n) - Vector((0, 0, 4 * dz * (i / n) * (1 - i / n)))) for i in range(n + 1)]


def round_poly(pts, radii, n=3):
    """Closed 2D polygon with a fillet radius per corner (0 keeps the corner sharp)."""
    out = []
    for i, r in enumerate(radii):
        tri = [pts[i - 1], pts[i], pts[(i + 1) % len(pts)]]
        out += fillet(tri, r, n, closed=False)[1:-1] if r > 0 else [pts[i]]
    return [(p[0], p[1]) for p in out]


# ----------------------------------------------------------------- slab

def slab(s, z1, holes, seams, body):
    """Wall slab x -HW..HW, y SKIN..BACKY, z 0..z1 minus openings and seam grooves (one boolean). holes: ('r', x0, z0,
    x1, z1, reveal) pockets and ('c', x, z, r, reveal) portholes, REVEAL + 0.02 deep; seams: ('h', z, x0, x1) /
    ('v', x, z0, z1) 4 cm grooves 2 cm deep (an edge seam at x = -+HW is half a groove: the joint reads as a seam)."""
    def whole(b):
        b.box((-HW, SKIN, 0.0), (HW, BACKY, z1), body)

    def cut(c):
        for h in holes:
            if h[0] == "r":
                _, x0, za, x1, zb, m = h
                c.box((x0, SKIN - 0.1, za), (x1, SKIN + REVEAL + 0.02, zb), m)
            else:
                _, x, z, r, m = h
                with c.at((x, SKIN - 0.1, z), BACK):
                    c.lathe([(r, 0.0), (r, REVEAL + 0.12)], m, seg=12)
        for kind, v, a, b in seams:
            if kind == "h":
                c.box((max(a, -HW - 0.1), SKIN - 0.1, v - 0.02), (min(b, HW + 0.1), SKIN + 0.02, v + 0.02), pal.Seam)
            else:
                c.box((v - 0.02, SKIN - 0.1, a), (v + 0.02, SKIN + 0.02, b), pal.Seam)
    carve(s, whole, cut)


# ----------------------------------------------------------------- openings

def pane(s, x0, z0, x1, z1, mat=None):
    y = SKIN + REVEAL
    s.face([(x0, y, z0), (x1, y, z0), (x1, y, z1), (x0, y, z1)], mat or pal.Window_Tower)


def window(s, x, z0, w=0.9, h=1.25, blind=None, hood=False):
    """Service window in its ivory-lined pocket: steel head, sill and transom bars, chrome sill, optional blind and
    ivory eyebrow."""
    x0, x1, z1 = x - w / 2, x + w / 2, z0 + h
    y, f, zt = SKIN + REVEAL, 0.045, z0 + h * 0.64
    pane(s, x0, z0, x1, z1)
    if blind:
        s.box((x0 + f, y - 0.022, z1 - h * 0.45), (x1 - f, y - 0.01, z1 - f), getattr(pal, blind))
    for a, b in (((x0, z0), (x1, z0 + f)), ((x0, z1 - f), (x1, z1)), ((x0, zt - 0.02), (x1, zt + 0.02))):
        s.box((a[0], y - 0.06, a[1]), (b[0], y - 0.004, b[1]), pal.Steel)
    s.box((x0 - 0.06, SKIN - 0.08, z0 - 0.07), (x1 + 0.06, SKIN + 0.06, z0 + 0.005), pal.Chrome)
    if hood:
        s.rbox((x0 - 0.12, SKIN - 0.32, z1 + 0.05), (x1 + 0.12, SKIN + 0.02, z1 + 0.15), 0.05, pal.Enamel_Ivory, seg=1)


def porthole(s, x, z, r=0.42, dogs=False, gasket=False, seg=12):
    """Round service light: proud chrome rim, chrome-sleeved pocket (the slab hole), glass; teal gasket, clamp dogs."""
    with s.at((x, SKIN, z), FRONT):
        s.lathe([(r + 0.09, -0.01), (r + 0.08, 0.05), (r - 0.012, 0.045), (r - 0.012, -0.03)],
                pal.Chrome, seg=seg, cap_bottom=False, cap_top=False)
        if gasket:
            s.lathe([(r + 0.002, -REVEAL + 0.05), (r - 0.05, -REVEAL + 0.012), (r - 0.05, -REVEAL + 0.004)],
                    pal.Enamel_Teal, seg=seg, cap_bottom=False, cap_top=False)
        s.lathe([(r, -REVEAL), (0, -REVEAL)], pal.Window_Tower, seg=seg, cap_bottom=False)
        for k in range(4 if dogs else 0):
            a = math.radians(45 + 90 * k)
            with s.at(((r + 0.09) * math.cos(a), (r + 0.09) * math.sin(a), 0.0), (0, 0, 45 + 90 * k)):
                s.box((-0.03, -0.035, 0.0), (0.05, 0.035, 0.05), pal.Steel)


def door(s, x, w=1.1, h=2.25, z0=0.15, leaf=None, bar=True, light=True):
    """Service door in an ivory-lined pocket (the caller cuts ('r', x -+ (w/2 + 0.05), z0, z0 + h + 0.1)): enamel leaf
    with a vision porthole, chrome speed lines, kick plate, push bar; chrome architrave; a granite step at the walk."""
    x0, x1, yl = x - w / 2, x + w / 2, SKIN + 0.12
    s.box((x0, yl - 0.05, z0), (x1, yl, z0 + h), leaf or pal.Enamel_Teal)
    s.box((x0 + 0.06, yl - 0.058, z0 + 0.06), (x1 - 0.06, yl - 0.05, z0 + 0.34), pal.Chrome)  # kick plate
    for k in range(3 if bar else 2):
        z = z0 + 1.06 + 0.07 * k
        s.box((x0 + 0.08, yl - 0.062, z), (x1 - 0.08, yl - 0.05, z + 0.024), pal.Chrome)
    if light:
        with s.at((x, yl - 0.05, z0 + 1.62), FRONT):
            s.lathe([(0.2, 0.0), (0.2, 0.02), (0.17, 0.035), (0.15, 0.0)], pal.Chrome, seg=12, cap_bottom=False,
                    cap_top=False)
            s.lathe([(0.15, 0.004), (0, 0.004)], pal.Window_Tower, seg=12, cap_bottom=False)
    if bar:
        s.sweep([(x0 + 0.14, yl - 0.05, z0 + 0.93), (x0 + 0.16, yl - 0.13, z0 + 0.93), (x1 - 0.16, yl - 0.13, z0 + 0.93),
                 (x1 - 0.14, yl - 0.05, z0 + 0.93)], 0.02, pal.Chrome, seg=6)
    a, top = w / 2 + 0.05, z0 + h + 0.1
    for xa, xb, za, zb in ((x - a - 0.07, x - a, z0, top + 0.07), (x + a, x + a + 0.07, z0, top + 0.07),
                           (x - a - 0.07, x + a + 0.07, top, top + 0.07)):
        s.box((xa, SKIN - 0.03, za), (xb, SKIN + 0.01, zb), pal.Chrome)
    if 0.05 < z0 < 0.5:  # ground doors step up from the walk
        s.rbox((x - a - 0.2, SKIN - 0.42, 0.0), (x + a + 0.2, SKIN + 0.02, z0), 0.02, pal.Stone_Dark, seg=1)


def canopy(s, x, z, w=1.9, d=0.85, t=0.13):
    """Streamline door canopy: rounded-front ivory slab, teal edge stripe, chrome nose bead, two chrome tie rods."""
    def plan(e):
        R, xa, xb, yf = 0.36 + e, x - w / 2 - e, x + w / 2 + e, SKIN - d - e
        return ([(xa, SKIN + 0.02)] + [(xa + R + R * math.cos(math.radians(a)), yf + R + R * math.sin(math.radians(a)))
                                        for a in range(180, 271, 30)]
                + [(xb - R + R * math.cos(math.radians(a)), yf + R + R * math.sin(math.radians(a)))
                   for a in range(270, 361, 30)] + [(xb, SKIN + 0.02)])
    soft_extrude(s, plan(0.0), z, z + t, pal.Enamel_Ivory, 0.04, seg=2)
    s.extrude(plan(0.008), z + 0.02, z + 0.06, pal.Enamel_Teal, smooth_sides=True, top=False, bottom=False)
    s.sweep([(px, py, z + 0.09) for px, py in plan(0.012)[1:-1]], 0.014, pal.Chrome, seg=5)
    for sx in (-1, 1):
        xr = x + sx * (w / 2 - 0.22)
        s.sweep([(xr, SKIN - d + 0.18, z + t), (xr, SKIN, z + 0.68)], 0.016, pal.Chrome, seg=5)  # clears PipeBundle


# ----------------------------------------------------------------- fixtures

def blade(s, x0, x1, y, z, hd, tilt, mat=None):
    """Louvre blade: one quad canted `tilt` degrees, facing the alley and the sky (single-sided: cheap, reads solid)."""
    c, sn = hd * math.cos(math.radians(tilt)), hd * math.sin(math.radians(tilt))
    s.face([(x0, y - c, z - sn), (x1, y - c, z - sn), (x1, y + c, z + sn), (x0, y + c, z + sn)],
           mat or pal.Enamel_Ivory)


def vent_bank(s, x, z0, w, h, n=None):
    """Louvre bank in a charcoal pocket (the caller cuts it): canted ivory blades, a mullion every 0.7 m, chrome frame."""
    n = n or max(3, round(h / 0.14))
    x0, x1, y = x - w / 2, x + w / 2, SKIN + REVEAL
    pane(s, x0, z0, x1, z0 + h, pal.Enamel_Charcoal)
    for k in range(n):
        blade(s, x0, x1, SKIN + 0.1, z0 + h * (k + 0.5) / n, 0.07, 40)
    m = max(1, round(w / 0.7))
    for j in range(1, m):
        xm = x0 + w * j / m
        s.box((xm - 0.018, SKIN - 0.01, z0), (xm + 0.018, y, z0 + h), pal.Steel)
    t = 0.05
    for xa, xb, za, zb in ((x0 - t, x0, z0 - t, z0 + h + t), (x1, x1 + t, z0 - t, z0 + h + t),
                           (x0, x1, z0 - t, z0), (x0, x1, z0 + h, z0 + h + t)):
        s.box((xa, SKIN - 0.035, za), (xb, SKIN + 0.01, zb), pal.Chrome)


def ac_unit(s, x, ztop, w=0.72, d=0.46, h=0.5):
    """Rounded wall AC box: ivory shell, teal band, chrome fan grille, on two chrome brackets, a drip pipe."""
    y0, y1, z0 = SKIN - d, SKIN - 0.02, ztop - h
    s.rbox((x - w / 2, y0, z0), (x + w / 2, y1, ztop), 0.08, pal.Enamel_Ivory, seg=1)
    s.rbox((x - w / 2 - 0.006, y0 - 0.006, ztop - 0.14), (x + w / 2 + 0.006, y1, ztop - 0.08), 0.06, pal.Enamel_Teal,
           seg=1)
    with s.at((x - 0.06, y0, z0 + h / 2 - 0.04), FRONT):
        s.lathe([(0.19, -0.01), (0.19, 0.02), (0.15, 0.006)], pal.Chrome, seg=10, cap_bottom=False, cap_top=False)
        s.lathe([(0.15, 0.004), (0, 0.004)], pal.Enamel_Charcoal, seg=10, cap_bottom=False)
        s.lathe(dome(0.03, 0.004, 0.025, 1), pal.Chrome, seg=6)
        for k in range(3):
            with s.at((0, 0, 0.008), (0, 0, 120 * k + 15)):
                s.box((0.02, -0.012, 0.0), (0.145, 0.012, 0.006), pal.Chrome)
    for sx in (-1, 1):
        xb = x + sx * (w / 2 - 0.1)
        s.box((xb - 0.02, y0 + 0.03, z0 - 0.04), (xb + 0.02, SKIN + 0.01, z0), pal.Chrome)
        strut(s, (xb, SKIN, z0 - 0.42), (xb, y0 + 0.07, z0 - 0.03), 0.03, pal.Chrome)
    s.sweep([(x + 0.22, y0 + 0.12, z0), (x + 0.22, SKIN - 0.03, z0 - 0.4)], 0.011, pal.Rubber, seg=4)


def grille(s, x, z, w=0.5, h=0.42):
    """Small louvred wall vent: raised ivory bezel over a real charcoal recess, canted slats (booths.bezel style)."""
    with s.at((x, SKIN, z), FRONT):
        bezel(s, w, h, 0.03, 0.035, 0.03, pal.Enamel_Ivory, pal.Enamel_Charcoal, pal.Enamel_Charcoal, n=1)
        for k in range(5):
            with s.at((0, -h / 2 + 0.07 + (h - 0.14) * k / 4, 0.015), (-90, 0, 0)):
                blade(s, -w / 2 + 0.04, w / 2 - 0.04, 0.0, 0.0, 0.024, 50)


def riser(s, x, z0, z1, bands, w=0.62, d=0.44, mat=None):
    """Rounded vertical service duct hugging the wall: enamel stadium section, chrome bands, granite foot."""
    yc = SKIN - d / 2 + 0.03
    s.extrude([(px + x, py + yc) for px, py in stadium(w, d, n=4)], z0, z1, mat or pal.Enamel_Seafoam,
              smooth_sides=True, bottom=False)
    for z in bands:
        s.extrude([(px + x, py + yc) for px, py in stadium(w + 0.04, d + 0.04, n=3)], z - 0.05, z + 0.05, pal.Chrome,
                  smooth_sides=True, top=False, bottom=False)
    s.rbox((x - w / 2 - 0.05, yc - d / 2 - 0.05, 0.0), (x + w / 2 + 0.05, SKIN + 0.02, z0), 0.02, pal.Stone_Dark, seg=1)


def duct_run(s, z, w=0.36, d=0.44, clamps=(-1.5, 1.5), mat=None):
    """Horizontal rounded duct across the whole bay (meets its neighbours), chrome saddle bands."""
    yc = SKIN - w / 2 - 0.02
    tube(s, straight(yc, z), list(stadium(w, d, n=3)), mat or pal.Enamel_Seafoam)
    for xc in clamps:
        tube(s, straight(yc, z, xc - 0.05, xc + 0.05), list(stadium(w + 0.04, d + 0.04, n=3)), pal.Chrome, caps=False)


def drainpipe(s, x, z1, collars, r=0.075, seg=6):
    """Teal rainwater pipe: hopper head under the parapet, chrome collars on wall brackets, a shoe at the foot."""
    y = SKIN - 0.04 - r
    with s.at((x, y, 0)):
        s.lathe([(r, 0.55), (r, z1)], pal.Enamel_Teal, seg=seg, cap_bottom=False, cap_top=False)
        for z in collars:
            s.lathe([(r, z - 0.045), (r + 0.018, z - 0.03), (r + 0.018, z + 0.03), (r, z + 0.045)], pal.Chrome,
                    seg=seg, cap_bottom=False, cap_top=False)
    for z in collars:
        s.box((x - 0.02, y, z - 0.03), (x + 0.02, SKIN + 0.01, z + 0.03), pal.Chrome)
    s.sweep(fillet([(x, y, 0.6), (x, y, 0.22), (x, y - 0.24, 0.1)], 0.14, 3, closed=False), r, pal.Enamel_Teal,
            seg=seg)
    hop = [(stadium(0.2, 0.18, n=3), z1 - 0.02), (stadium(0.2, 0.18, n=3), z1 + 0.08),
           (stadium(0.38, 0.3, n=3), z1 + 0.36), (stadium(0.4, 0.32, n=3), z1 + 0.46)]
    with s.at((x, SKIN - 0.15, 0)):
        s.loft(hop, pal.Enamel_Teal)
    s.box((x - 0.19, SKIN - 0.33, z1 + 0.44), (x + 0.19, SKIN + 0.02, z1 + 0.5), pal.Chrome)


def conduit(s, z, r, mat, y=None):
    y = SKIN - 0.03 - r if y is None else y
    s.sweep(straight(y, z), r, mat, seg=6)


def insulator(s, x, z, y=SKIN):
    """Porcelain spool on a chrome bracket: cable hook."""
    s.box((x - 0.02, y - 0.16, z - 0.02), (x + 0.02, y + 0.01, z + 0.02), pal.Chrome)
    with s.at((x, y - 0.16, z - 0.02), (180, 0, 0)):
        s.lathe([(0.025, 0.0), (0.045, 0.025), (0.03, 0.05), (0.04, 0.075), (0.0, 0.09)], pal.Enamel_Ivory, seg=4)


def cable_run(s, z, sag_dz=0.18):
    """Cable swagged between porcelain hooks at x = -+1.5: the sag's low point falls on every joint, so runs join."""
    pts = []
    for i in range(13):
        x = -HW + 0.5 * i
        u = ((x + 1.5) % 3.0) / 3.0
        pts.append((x, SKIN - 0.17, z - 4 * sag_dz * u * (1 - u)))
    s.sweep(pts, 0.014, pal.Rubber, seg=4)
    for x in (-1.5, 1.5):
        insulator(s, x, z + 0.02)


def catwalk(s, z, d=0.72):
    """Maintenance catwalk along the bay (meets its neighbours): steel grating deck, teal toe board, cantilever
    struts, chrome top and knee rails on posts every 1.5 m."""
    y = SKIN - d
    s.box((-HW, y, z - 0.07), (HW, SKIN, z), pal.Steel)
    s.box((-HW, y - 0.02, z - 0.1), (HW, y, z + 0.1), pal.Enamel_Teal)
    for x in (-2.25, -0.75, 0.75, 2.25):
        strut(s, (x, SKIN, z - 0.75), (x, y + 0.1, z - 0.08), 0.05, pal.Enamel_Teal)
        strut(s, (x, y + 0.03, z + 0.1), (x, y + 0.03, z + 1.02), 0.035, pal.Chrome)
    for hh, r in ((1.0, 0.024), (0.52, 0.017)):
        s.sweep(straight(y + 0.03, z + hh), r, pal.Chrome, seg=6)


def antennas(s, x, y, z):
    """Antenna cluster on a duct head: a whip mast with dipole bars and a red beacon, a short whip, a small dish."""
    with s.at((x - 0.12, y, 0)):
        s.lathe([(0.035, z), (0.012, z + 2.6), (0.0, z + 2.62)], pal.Chrome, seg=6)
        for zz, hw in ((z + 1.6, 0.34), (z + 2.1, 0.22)):
            strut(s, (-hw, 0, zz), (hw, 0, zz), 0.018, pal.Chrome)
        with s.at((0, 0, z + 2.62)):
            s.lathe([(0.04 * math.sin(math.pi * k / 4), -0.04 * math.cos(math.pi * k / 4)) for k in range(5)],
                    pal.Signal_Red, seg=6)
    with s.at((x + 0.16, y, 0)):
        s.lathe([(0.025, z), (0.01, z + 1.7), (0.0, z + 1.72)], pal.Chrome, seg=6)
    strut(s, (x + 0.02, y, z), (x + 0.02, y, z + 0.5), 0.04, pal.Steel)
    with s.at((x + 0.02, y - 0.04, z + 0.55), (-65, 0, 20)):
        dish = [(0.24 * k / 4, 0.06 * (k / 4) ** 2) for k in range(5)]
        s.lathe(dish, pal.Enamel_Ivory, seg=8, cap_bottom=False, cap_top=False)
        s.lathe([(a, b + 0.012) for a, b in dish][::-1], pal.Enamel_Ivory, seg=8, cap_bottom=False, cap_top=False)
        s.sweep([(0, 0, 0.0), (0, 0, 0.22)], 0.008, pal.Chrome, seg=4)


def caged_lamp(s, body=None, seg=16):
    """Caged jar lamp on a gooseneck wall bracket. Frame origin = the wall face at the backplate centre; out along -Y."""
    B = body or pal.Enamel_Teal
    s.rbox((-0.075, -0.035, -0.14), (0.075, 0.005, 0.14), 0.03, B, seg=1)
    for zz in (-0.095, 0.095):
        s.box((-0.012, -0.043, zz - 0.012), (0.012, -0.03, zz + 0.012), pal.Chrome)
    s.sweep(fillet([(0, -0.03, 0.03), (0, -0.26, 0.03), (0, -0.3, -0.05)], 0.07, 3, closed=False), 0.018,
            pal.Chrome, seg=6)
    with s.at((0, -0.3, -0.05)):
        s.lathe([(0.03, 0.02), (0.03, -0.01), (0.026, -0.02)], pal.Chrome, seg=seg, cap_bottom=False)
        s.lathe([(0.02, -0.03), (0.105, -0.085), (0.115, -0.08), (0.028, -0.012)], B, seg=seg, cap_bottom=False,
                cap_top=False)
        s.lathe([(0, -0.3), (0.05, -0.285), (0.066, -0.2), (0.058, -0.08), (0.04, -0.06)], pal.Lamp,
                seg=seg, cap_top=True)
        for k in range(4):
            a = 2 * math.pi * (k + 0.5) / 4
            c, sn = math.cos(a), math.sin(a)
            wire = [(0.1 * c, 0.1 * sn, -0.085), (0.085 * c, 0.085 * sn, -0.16), (0.08 * c, 0.08 * sn, -0.26),
                    (0.03 * c, 0.03 * sn, -0.315)]
            s.sweep(wire, 0.006, pal.Chrome, seg=3)
        for z, r in ((-0.2, 0.084),):
            s.sweep([(r * math.cos(t), r * math.sin(t), z) for t in (2 * math.pi * i / seg for i in range(seg + 1))],
                    0.006, pal.Chrome, seg=3, caps=False)
        s.lathe([(0.035, -0.32), (0.035, -0.305), (0, -0.3)], pal.Chrome, seg=8, cap_bottom=True)


def rails(s, path, z, heights=(1.0, 0.52), r=0.024, step=1.3, seg=5):
    """Chrome tube railing along an open plan path: horizontal rails at each height, square posts at the ends and
    every `step` along the way."""
    P = fillet([(x, y, 0.0) for x, y in path], 0.18, 2, closed=False)
    for i, hh in enumerate(heights):
        s.sweep([(x, y, z + hh) for x, y, _ in P], r if i == 0 else r * 0.7, pal.Chrome, seg=seg if i == 0 else 4)
    pts = [Vector((x, y, 0)) for x, y in path]
    stations = [pts[0]]
    for a, b in zip(pts, pts[1:]):
        k = max(1, round((b - a).length / step))
        stations += [a.lerp(b, j / k) for j in range(1, k + 1)]
    for p in stations:
        strut(s, (p.x, p.y, z), (p.x, p.y, z + heights[0]), 0.035, pal.Chrome)
