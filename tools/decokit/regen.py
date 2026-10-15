"""Regen orders: one parameterised piece per order in the plan (tools/decokit/orders.ts writes <out>/orders.json from
contracts/world/harbour-district.plan.json; export_kit.py passes it as --orders). Each builder is a variant of its source
asset (skyline.py / highrise.py / skyport.py) driven only by what the order states: tiers (width, z0, z1) on the storey
grid, the tip, the footprint, the frame. z = 0 is the podium deck for towers (grid.deck = its plan height, the tip adds it).
No order: nothing registers, the kit is unchanged.

Footprint is the outer envelope (piers, bands, parapets, canopy): plates are inset by the measured excess so a tier of
width W is W wide as built (`tiers` in the measure reads each tier's own width; fit.ts fails one narrower than the plan by more than 0.1 m). `measure` reports the built footprint below and above the first setback and the tip; the
fit table (tools/decokit/fit.ts, tools/kit) checks them against the site.
"""

import json
import math
import re
import sys

from geo import Shape, rrect, stadium
from kit import asset
from mats import accent, pal, register

register("DK_Render_Cream", "stone", "#F1E4C8")          # cream render (round 3 Tower_Saucer): dE2000 24.4 from guard
register("DK_Window_Teal", "enamel", "#2E6D70")           # as highrise.py (register is first-wins)
register("DK_Enamel_Turquoise", "enamel", accent("turquoise"))
register("DK_Enamel_Coral17", "enamel", accent("coral"))


def _stad(w, d, n=12):
    """stadium() without its repeated points (the two arcs of each end meet at one point): a clean n-gon cap."""
    out = []
    for p in stadium(w, d, n):
        if not out or abs(p[0] - out[-1][0]) + abs(p[1] - out[-1][1]) > 1e-6:
            out.append(p)
    if abs(out[0][0] - out[-1][0]) + abs(out[0][1] - out[-1][1]) <= 1e-6:
        out.pop()
    return out

RMIN = 0.1   # a corner round stays this far under half the narrower side (see _round)


def _round(w, d, r):
    """Corner radius of a plate w x d, clamped under half its narrower side. The inset (`_excess`) can leave a plate narrower than twice the kit's
    radius (crescent-tower tier 3: 5.3 m plate, r 3.0; crescent-terrace tiers 4 and 5: 2.84 and 3.84 m, r 2.0). Geo.rrect already takes r down to
    half, but then the arcs of neighbouring corners meet in one point: repeated points, zero-length edges, and the terrace's coping and roof caps
    (extrudes of that outline) come out with degenerate faces. Held RMIN under half, every edge of the outline has length."""
    return max(0.0, min(r, min(w, d) / 2 - RMIN))


ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
ORDERS = {}
GRID = {}
EPS = 1e-6


def _load():
    if "--orders" not in ARGS:
        return
    doc = json.loads(open(ARGS[ARGS.index("--orders") + 1]).read())
    GRID.update(doc["grid"])
    for o in doc["pieces"]:
        ORDERS[o["id"]] = o


def _grid(z, what):
    k = z / GRID["storey"]
    if abs(k - round(k)) > 1e-6:
        raise ValueError(f"{what}: {z} m is off the {GRID['storey']} m storey grid")


def _num(text, pattern, what):
    m = re.search(pattern, text)
    if not m:
        raise ValueError(f"{what}: order text has no match for {pattern!r}")
    return [float(g) for g in m.groups()]


def _tiers(o):
    t = [tuple(x) for x in o["regen"]["tiers"]]
    for w, z0, z1 in t:
        _grid(z0, o["id"] + " tier z0")
        _grid(z1, o["id"] + " tier z1")
    return t


def _tip_top(o):
    """Tip height above the pivot (deck): the order's tip is a plan height."""
    return o["regen"]["tip"] - GRID["deck"]


# ----------------------------------------------------------------- measurement

def _verts(obj):
    return [tuple(v.co) for v in obj.data.vertices]


def extents(vs, lo, hi):
    """[xmin, xmax, ymin, ymax] of the vertices with lo < z <= hi (None when empty)."""
    p = [v for v in vs if lo < v[2] <= hi]
    return [round(min(v[0] for v in p), 3), round(max(v[0] for v in p), 3),
            round(min(v[1] for v in p), 3), round(max(v[1] for v in p), 3)] if p else None


def _split(o):
    if o["kind"] == "megastructure":
        return _num(o["regen"]["add"], r"frame (\d+(?:\.\d+)?) → (\d+(?:\.\d+)?)", o["id"])[1]
    if o["kind"] == "signature":
        return o["regen"]["drum"]["z"][1]
    if "tiers" not in o["regen"]:  # shaft towers (Needle, DiscTower): the shaft below the saucer is the footprint
        return o["regen"]["body"][2] + 0.5
    return o["regen"]["tiers"][0][2] + 1.4  # past the first terrace: parapet 1.1, planters 1.3 stand inside the tier below


def tier_widths(o, vs):
    """Each tier's built envelope [x, y] in metres, read in the tier's own top band (z1 - 1.5, z1 + 0.02]: there the teal and coral bands (0.35 out), the piers' tops and the
    hanging greenery are the widest things the tier has, and nothing beside it is wider (the tier above starts at z1 and is narrower; the terrace furniture of the tier below
    stands at most 1.31 m over its own top, 12 m or more under this band). None for a tower with no tiers (shaft towers, the portals)."""
    if o["kind"] != "tower" or "tiers" not in o["regen"]:
        return None
    out = []
    for _, _, z1 in _tiers(o):
        p = [v for v in vs if z1 - 1.5 < v[2] <= z1 + 0.02]
        out.append([round(max(v[0] for v in p) - min(v[0] for v in p), 3), round(max(v[1] for v in p) - min(v[1] for v in p), 3)])
    return out


def measure(name, obj):
    """Built footprint below / above the first setback (or the frame), the tip, in the piece's frame (pivot = footprint centre); per-tier widths for a tiered tower."""
    o, vs = ORDERS[name], _verts(obj)
    sp = _split(o)
    zmax = max(v[2] for v in vs)
    roof = None if (o["kind"] != "tower" or "tiers" not in o["regen"]) else o["regen"]["tiers"][-1][2] + 1.2  # body roof incl. parapet: the crown stands above
    return dict(split=sp, roof=roof, lower=extents(vs, -1e9, sp + EPS), upper=extents(vs, sp + EPS, roof + EPS if roof else 1e9),
                crown=extents(vs, roof + EPS, 1e9) if roof else None, tip=round(zmax, 3), tipPlan=round(zmax + (GRID["deck"] if o["kind"] == "tower" else 0.0), 3),
                tiers=tier_widths(o, vs))


def _excess(o, fn):
    """Probe build with plates at the order's widths; per tier how far piers, bands and parapets stand proud of the plate."""
    s = Shape("DK_probe")
    fn(s, o, [0.0] * len(o["regen"]["tiers"]), 1.0)
    obj = s.build(None, bevel=0.0)
    vs, out = _verts(obj), []
    for i, (w, z0, z1) in enumerate(_tiers(o)):
        # Tier 0: above the base canopy (trimmed separately). The others: above the terrace of the tier below, whose parapet (top 1.1) and planters (1.31) stand outside
        # this tier's plate: read from z0 they gave every upper tier the lower tier's parapet as its own excess, and built 4 to 7 m narrower than the plan (15 Oct, brief 009).
        lo = z0 + (5.0 if i == 0 else 1.4)
        p = [v for v in vs if lo < v[2] <= z1 + 1.15]
        out.append(round(max(max(abs(v[0]), abs(v[1])) for v in p) - w / 2, 3))
    import bpy
    me = obj.data
    bpy.data.objects.remove(obj)
    bpy.data.meshes.remove(me)
    return out


# ----------------------------------------------------------------- Tower_Stepped

def stepped(s, o, ex, canopy):
    from skyline import pier_tier
    t, top = _tiers(o), o["regen"]["tiers"][-1][2]
    for w, z0, z1 in t:
        pier_tier(s, w / 2, z0, z1)
    h, ch = t[-1][0] / 2, _tip_top(o) - top           # crown height = the tip's, what the order states
    inner, outer = h * 0.8, h + 0.2
    for rot in (0, 90, 180, 270):
        with s.at((0, 0, 0), (0, 0, rot)):
            for x, f in ((-0.4 * h, 0.625), (0.0, 1.0), (0.4 * h, 0.625)):
                s.box((x - 0.5, -outer, top + 1.2), (x + 0.5, -(h - 1.2), top + ch * f), pal.Limestone_Tex)
    s.box((-inner, -inner, top + 1.2), (inner, inner, top + 1.2 + 2.3), pal.Enamel_Teal)


# ----------------------------------------------------------------- Tower_Saucer

def saucer(s, o, ex, canopy):
    """Brief 007 round 3 (Sky: "black-and-white bands read as modern"): a cream deco slab, not a striped curtain wall.
    Per tier: a cream render core with teal glass set back in short window strips (1.6 of the 3.6 storey) between full-height
    cream pilasters every ~3 m on the long faces and every 30 deg round the ends (AtomicAge power_spire, starlight_hotel), a
    teal faience spandrel band every third storey, a gold band and a cream coping at every tier top, teal faience panels on
    the round ends' centre lines, and the coral front fin with a gold edge. Outer envelope = the order's stadium (w x d)."""
    g = GRID
    (wl, d, top), t = o["regen"]["body"], _tiers(o)
    fl = g["storey"]
    for ti, (w, z0, z1) in enumerate(t):
        cw, cd = w - 0.9, d - 0.9                          # core (window wall) set 0.45 inside the pilaster face
        s.extrude(_stad(cw, cd), z0, z1 - 0.9, pal.Render_Cream, top=False, bottom=(ti == 0))
        z, k = z0, 0
        while z < z1 - EPS:
            zt = min(z + fl, z1)
            if k % 3 == 2:                                 # teal faience speed band every third storey, wrapping the pilasters
                s.extrude(_stad(w, d), z + 0.05, z + 1.0, pal.Enamel_Turquoise, top=False, bottom=False)
            s.extrude(_stad(cw + 0.06, cd + 0.06), z + 1.1, z + 2.7, pal.Window_Teal, top=False, bottom=False)
            z, k = zt, k + 1
        # pilasters: long faces every ~3 m (straight run of the stadium), ends every 30 deg
        run = w - d
        n = max(2, int(round(run / 3.0)))
        for i in range(n + 1):
            x = -run / 2 + run * i / n
            for sy in (-1, 1):
                y0, y1 = sorted((sy * (cd / 2 - 0.1), sy * (d / 2 - 0.05)))
                s.box((x - 0.35, y0, z0), (x + 0.35, y1, z1 - 0.9), pal.Render_Cream)
        R = d / 2
        for sx in (-1, 1):
            for a in (-60, -30, 0, 30, 60):
                with s.at((sx * run / 2, 0.0, 0.0), (0, 0, a + (0 if sx > 0 else 180))):
                    s.box((cd / 2 - 0.1, -0.35, z0), (R - 0.05, 0.35, z1 - 0.9), pal.Render_Cream)
            with s.at((sx * run / 2, 0.0, 0.0), (0, 0, 0 if sx > 0 else 180)):   # end panel: teal faience between the piers
                s.box((cd / 2 + 0.02, -1.3, z0 + 1.0), (cd / 2 + 0.2, 1.3, z1 - 1.0), pal.Enamel_Turquoise)
        s.extrude(_stad(w, d), z1 - 0.9, z1 - 0.5, pal.Gold, top=False, bottom=False)          # gold band
        s.extrude(_stad(w, d), z1 - 0.5, z1, pal.Render_Cream, top=True, bottom=False)       # coping
    s.box((-0.5, -(d / 2 + 0.7), 0.0), (0.5, -(d / 2 - 0.6), top + 6.5), pal.Enamel_Coral17)  # front fin
    for sx in (-1, 1):                                                                         # its gold edge beads
        s.box((sx * 0.5 - 0.06, -(d / 2 + 0.7), 0.0), (sx * 0.5 + 0.06, -(d / 2 - 0.6), top + 6.5), pal.Gold)
    R = t[-1][0] / 2 + g["halfBay"] - 0.02               # the saucer overhangs the upper tier by the half bay (teal ring 0.02 proud)
    total = _num(o["regen"]["crown"], r"total ≤ (\d+(?:\.\d+)?)", o["id"])[0]
    stem_h = 1.6
    sk = R / 15.02
    prof = [(2.4, 0.0), (13.5, 2.0), (15.0, 3.2), (15.0, 3.8), (13.0, 5.4), (6.0, 7.0), (0.0, 7.4)]
    zs = top + stem_h
    assert stem_h + prof[-1][1] <= total + EPS, "saucer taller than the order's total"
    s.lathe([(3.0, top), (3.0, zs), (2.4, zs)], pal.Render_Cream, seg=24, cap_bottom=False)
    s.lathe([(r * sk if r > 3 else r, zs + z) for r, z in prof], pal.Enamel_Ivory, seg=48)
    s.lathe([(15.02 * sk, zs + 3.1), (15.02 * sk, zs + 3.9)], pal.Enamel_Turquoise, seg=48, cap_bottom=False, cap_top=False)
    s.lathe([(13.6 * sk, zs + 2.05), (14.4 * sk, zs + 2.55)], pal.Gold, seg=48, cap_bottom=False, cap_top=False)  # gold soffit bead
    from skyline import spire
    z0 = zs + prof[-1][1]
    spire(s, z0, _tip_top(o) - z0, 0.6)                  # cut to the tip


# ----------------------------------------------------------------- Tower_Atom / Tower_Lantern

def atom(s, o, ex, canopy):
    from highrise import atom_crown, stack
    t = _tiers(o)
    tiers = [((w - 2 * e, w - 2 * e, _round(w - 2 * e, w - 2 * e, 0.25 * w), 0.0), z0, z1) for (w, z0, z1), e in zip(t, ex)]
    stack(s, tiers, canopy)
    top = t[-1][2]
    avail = _tip_top(o) - top - 1.1
    atom_crown(s, top + 1.1, k=0.99 * avail / 26.9)


def lantern(s, o, ex, canopy):
    from highrise import band, lantern_crown, stack
    t = _tiers(o)
    tiers = [((w - 2 * e, w - 2 * e, _round(w - 2 * e, w - 2 * e, 0.28 * w), 0.0), z0, z1) for (w, z0, z1), e in zip(t, ex)]
    stack(s, tiers, canopy)
    for plan, z0, z1 in tiers[:-1]:                      # coral banding mid-tier
        z = round((z0 + z1) / 2 / GRID["storey"]) * GRID["storey"]
        band(s, plan, z, z + 0.7, pal.Enamel_Coral, out=0.33)
    top = t[-1][2]
    finial = _tip_top(o) - (top + 1.1) - 10.4           # dome base sits 10.4 m over the crown base (gallery 1.6, lantern 8, rim 0.7, dome 0)
    lantern_crown(s, top + 1.1, finial=finial)


def starburst_tower(s, o, ex, canopy):
    from highrise import starburst
    zs = [0.0] + [z1 for _, _, z1 in _tiers(o)]
    starburst(s, zs)


def terrace_tower(s, o, ex, canopy):
    from highrise import TERRACE_PLANS, terrace_tower as build
    t = _tiers(o)
    zs = [0.0] + [z1 for _, _, z1 in t]
    plans = [(w - 2 * e, d - 2 * e, _round(w - 2 * e, d - 2 * e, rr), cy) for (w, _, _), (_, d, rr, cy), e in zip(t, TERRACE_PLANS, ex)]
    build(s, zs, plans, canopy)


def needle_tower(s, o, ex, canopy):
    from highrise import needle
    needle(s, o["regen"]["body"][2])


def disc_tower(s, o, ex, canopy):
    from landmarks import disc_tower_at
    disc_tower_at(s, o["regen"]["body"][2])


def _fit_excess(o, fn, ex, rounds=6):
    """The probe cannot see everything: a tier's excess moves with its plate (the hanging greenery's balls sit at stations along the plate's rim, so their sizes and reach change
    as it shrinks: the terrace tower's upper tiers came out 0.3 to 0.5 m short). So build the tower as it will be built, read each upper tier's width in its own band (tier_widths,
    what fit.ts checks), and move that tier's excess by half its difference from the plan until it is the plan's width to 5 mm. Tier 0 keeps the probe's value: it is bounded by the
    site over its whole height (the fit table's `lower`), not by a band."""
    import bpy
    ex, t = list(ex), _tiers(o)
    for _ in range(rounds):
        s = Shape("DK_probe")
        fn(s, o, ex, max(0.05, ex[0] - 0.3))
        obj = s.build(None, bevel=0.0)
        env = tier_widths(o, _verts(obj))
        me = obj.data
        bpy.data.objects.remove(obj)
        bpy.data.meshes.remove(me)
        gap = [(env[i][0] - w) / 2 for i, (w, _, _) in enumerate(t)]
        if all(abs(g) <= 0.005 for g in gap[1:]):
            break
        for i in range(1, len(ex)):
            ex[i] += gap[i]
    return ex


def _inset_tower(fn):
    def build(s, o):
        ex = _fit_excess(o, fn, _excess(o, fn))
        canopy = max(0.05, ex[0] - 0.3)
        fn(s, o, ex, canopy)
    return build


TOWERS = {"Tower_Stepped": ("Skyline", stepped, False), "Tower_Saucer": ("Skyline", saucer, False),
          "Tower_Atom": ("Highrise", atom, True), "Tower_Lantern": ("Highrise", lantern, True),
          "Tower_Starburst": ("Highrise", starburst_tower, False), "Tower_Terrace": ("Highrise", terrace_tower, True),
          "Tower_Needle": ("Highrise", needle_tower, False), "DiscTower": ("Landmarks", disc_tower, False)}


# ----------------------------------------------------------------- Skyport_Tower_Portal

def portal(s, o):
    import skyport as sk
    r, lim = o["regen"], o["limits"]
    assert r["scale"] == 1.0 and r["yaw"] == 0, "portal variant is built at x1.0, yaw 0"
    ax, ay = r["at"]
    f0, f1 = _num(r["add"], r"frame (\d+(?:\.\d+)?) → (\d+(?:\.\d+)?)", o["id"])
    legs = [[a - ax, b - ax, c - ay, d - ay] for a, b, c, d in lim["legs"]]
    lw = legs[0][1] - legs[0][0]
    X0, X1 = min(l[0] for l in legs), max(l[1] for l in legs)
    Y0, Y1 = min(l[2] for l in legs), max(l[3] for l in legs)
    e = 0.06                                              # dressing stays inside the leg rect
    for x0, x1, y0, y1 in legs:                           # four legs: the stem's buttress fins, limestone with teal inlays and gold edges
        cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
        h = lw / 2 - e
        s.box((cx - h, cy - h, 0.0), (cx + h, cy + h, f0), pal.Limestone_Tex)
        for sx, sy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            a, b = (cx + sx * h, cy + sy * h), (cx + sx * lw / 2, cy + sy * lw / 2)
            lo, hi = (min(a[0], b[0]), min(a[1], b[1])), (max(a[0], b[0]), max(a[1], b[1]))
            w = 1.0
            if sx:
                s.box((lo[0], cy - w, 2.0), (hi[0], cy + w, f0 - 2.0), pal.Enamel_Teal)
            else:
                s.box((cx - w, lo[1], 2.0), (cx + w, hi[1], f0 - 2.0), pal.Enamel_Teal)
        for sx in (-1, 1):
            for sy in (-1, 1):
                s.sweep([(cx + sx * (h - 0.13), cy + sy * (h - 0.13), 0.4), (cx + sx * (h - 0.13), cy + sy * (h - 0.13), f0)],
                        0.12, pal.Gold, seg=6)
    for x0, x1, y0, y1 in ((X0, X1, Y0, Y0 + lw), (X0, X1, Y1 - lw, Y1), (X0, X0 + lw, Y0 + lw, Y1 - lw), (X1 - lw, X1, Y0 + lw, Y1 - lw)):
        s.box((x0 + e, y0 + e, f0), (x1 - e, y1 - e, f1), pal.Limestone_Tex)    # frame: leg to leg on four sides
    s.box((-3.0, Y0 + lw, f0), (3.0, Y1 - lw, f1), pal.Limestone_Tex)           # cross-girder carrying the stem
    s.lathe([(0.0, f1 - 1.6), (11.4, f1 - 1.6), (11.4, f1 - 0.02), (0.0, f1 - 0.02)], pal.Limestone_Tex, seg=48)  # the stem's platform
    sk.stem(s, base=f1)
    for (num, R, zt, az, D, depth, ld, car), seg in zip(sk.PADS, (64, 56, 48)):
        sk.branch(s, num, R, zt, az, D, depth, ld, seg, zmin=f1)
    sk.gantry(s, z0=f1)
    sk.crown(s, tip=r["tip"])


# ----------------------------------------------------------------- SpaceHQ_Portal

def _sector(s, r0, r1, a0, a1, z0, z1, mat, n=None):
    """Annular sector prism (angles from +X, degrees)."""
    n = n or max(2, int(abs(a1 - a0) / 5))
    outer = [(r1 * math.cos(math.radians(a0 + (a1 - a0) * k / n)), r1 * math.sin(math.radians(a0 + (a1 - a0) * k / n))) for k in range(n + 1)]
    inner = [(r0 * math.cos(math.radians(a1 - (a1 - a0) * k / n)), r0 * math.sin(math.radians(a1 - (a1 - a0) * k / n))) for k in range(n + 1)]
    s.extrude(outer + inner, z0, z1, mat, smooth_sides=True)


def _segment(sd, R, xcut, n=14):
    """The part of the circle R beyond |x| = xcut on side sd (CCW)."""
    a = math.degrees(math.acos(xcut / R))
    arc = [(sd * R * math.cos(math.radians(-a + 2 * a * k / n)), R * math.sin(math.radians(-a + 2 * a * k / n))) for k in range(n + 1)]
    return arc if sd > 0 else arc[::-1]


def _floor(x0, x1, R):
    xs = [x0 + (x1 - x0) * k / 6 for k in range(7)]
    top = [(x, math.sqrt(R * R - x * x)) for x in xs]
    return top + [(x, -y) for x, y in top[::-1]]


def space_hq_portal(s, o):
    """SpaceHQ on the terminal drum (plan signatures[0].regen): r 40, z 0..34. The line runs along local Y (the stair
    side = +Y once yawed). Bearing halves |x| 20..40: limestone wall r 39.3 with pilasters to 39.9 every 6 deg, ribbon
    windows at the street, deck and concourse storeys, the gold band at the deck line 9.43 and the teal speed band at the
    concourse 24 wrapping them. Between the halves the Parade passes through at street level; the concourse floor (top 24)
    covers |x| 4.6..20, the beam trench |x| < 4.6 stays open. The station hall (spacehq.hall): ribbed coffered ceiling,
    rib soffit 30 (plan), coffer backs 31; pilasters, torcheres, mural band, lamp standards, terrazzo inlays. Each end:
    the curved drum wall for |x| 12..20 and a flat portal frontispiece over |x| < 12, recessed to the chord, with the
    12 x 7.5 arched opening (z 21.5..29.0, head r 6 on the spring 23; spacehq.portal_wall). Drum roof 31..34.
    Above 34: spacehq.tower (Sky 2026-10-13: an atom over a needle mast, not a rocket); tip 252."""
    import spacehq as hq
    register("DK_Enamel_Coral17", "enamel", accent("coral"))
    register("DK_Enamel_Sunshine", "enamel", accent("sunshine"))
    dr = o["regen"]["drum"]
    R, (z0, z1), (rz0, rz1) = 39.9, dr["z"], dr["roof"]
    RW, ZR = 39.3, rz0 + 1.0                             # bearing-half wall (pilasters to R); coffer backs / roof soffit 31
    XS, XT, XP = 20.0, 4.6, 12.0                          # slot half width (legs), beam trench half width, portal half width
    L = pal.Limestone_Tex
    ya = math.sqrt(R * R - XS * XS)
    for sd in (1, -1):
        s.extrude(_segment(sd, RW, XS), z0, ZR, L, smooth_sides=True)                      # bearing half
        a = math.degrees(math.acos(XS / RW))
        for za, zb, mat in ((9.2, 9.7, pal.Gold), (23.7, 24.9, pal.Enamel_Turquoise)):       # bands wrap the pilasters
            _sector(s, RW, R + 0.1, (-a if sd > 0 else 180 - a), (a if sd > 0 else 180 + a), za, zb, mat)
        xf = sd * XS
        s.box((min(xf, xf - sd * 0.1), -ya, 9.2), (max(xf, xf - sd * 0.1), ya, 9.7), pal.Gold)  # gold line along the slot face
        fl = _floor(XT, XS, R) if sd > 0 else [(-x, y) for x, y in _floor(XT, XS, R)][::-1]
        s.extrude(fl, 23.6, 24.0, pal.Terrazzo_Ivory)                                        # concourse floor = platform decks
        xi = sd * XT
        s.box((min(xi, xi + sd * 0.1), -26.0, 22.4), (max(xi, xi + sd * 0.1), 26.0, 24.0), pal.Enamel_Ivory)   # platform fascia on the trench
        s.box((min(xi, xi + sd * 0.45), -25.0, 24.0), (max(xi, xi + sd * 0.45), 25.0, 24.012), pal.Enamel_Sunshine)  # edge line, 50 m
        for y in range(-24, 25, 6):
            s.box((min(xi, xi + sd * 0.1) - 0.02, y - 0.1, 23.1), (max(xi, xi + sd * 0.1) + 0.02, y + 0.1, 23.6), pal.Chrome)
    hq.drum_dress(s, RW, R, XS, ZR)
    s.lathe([(0.0, ZR), (R, ZR), (R, rz1), (0.0, rz1)], L, seg=96)                            # drum roof, cornice over the pilasters
    s.lathe([(R, rz1 - 0.9), (R + 0.1, rz1 - 0.9), (R + 0.1, rz1 - 0.3), (R, rz1 - 0.3)], pal.Gold, seg=96, cap_bottom=False, cap_top=False)
    s.lathe([(R - 0.6, rz1), (R - 0.6, rz1 + 1.1), (R - 1.0, rz1 + 1.1), (R - 1.0, rz1)], L, seg=96, cap_bottom=False, cap_top=False)  # roof parapet
    s.lathe([(R - 1.0, rz1 + 0.02), (0.0, rz1 + 0.02)], pal.Terrazzo_Ivory, seg=96, cap_bottom=False, cap_top=False)            # roof terrace
    # hall end walls: curved drum wall for |x| 12..20, the flat portal frontispiece over |x| < 12
    th0, th1 = math.degrees(math.acos(XS / R)), math.degrees(math.acos(XP / R))
    for base in (0.0, 180.0):
        _sector(s, R - 0.8, R, base + th0, base + th1, 24.0, ZR, L)
        _sector(s, R - 0.8, R, base + 180 - th1, base + 180 - th0, 24.0, ZR, L)
    yo = math.sqrt(R * R - XP * XP)
    for sy in (1, -1):
        hq.portal_wall(s, sy, R, yo, half=XP, z0=24.0, z1=ZR)
    hq.hall(s, R, XS, XT, ya, z_floor=24.0, soffit=rz0, z_roof=ZR)
    hq.tower(s, rz1)


# ----------------------------------------------------------------- registry

def _register():
    for name, o in ORDERS.items():
        if o["kind"] == "tower":
            cat, fn, inset = TOWERS[o["from"]]
            build = _inset_tower(fn) if inset else (lambda s, o, fn=fn: fn(s, o, [0.0] * len(o["regen"].get("tiers", [])), 1.0))
        elif o["kind"] == "signature":
            cat, build = "Landmarks", space_hq_portal
        else:
            cat, build = "Skyport", portal
        asset(name, cat, bevel=0.0)(lambda s, o=o, build=build: build(s, o))


_load()
_register()
