"""Regen orders: one parameterised piece per order in the plan (tools/decokit/orders.ts writes <out>/orders.json from
contracts/world/harbour-district.plan.json; export_kit.py passes it as --orders). Each builder is a variant of its source
asset (skyline.py / highrise.py / skyport.py) driven only by what the order states: tiers (width, z0, z1) on the storey
grid, the tip, the footprint, the frame. z = 0 is the podium deck for towers (grid.deck = its plan height, the tip adds it).
No order: nothing registers, the kit is unchanged.

Footprint is the outer envelope (piers, bands, parapets, canopy): plates are inset by the measured excess so a tier of
width W is W wide as built. `measure` reports the built footprint below and above the first setback and the tip; the
fit table (tools/decokit/fit.ts, tools/kit) checks them against the site.
"""

import json
import math
import re
import sys

from geo import Shape, rrect, stadium
from kit import asset
from mats import pal

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
    return o["regen"]["tiers"][0][2] + 1.4  # past the first terrace: parapet 1.1, planters 1.3 stand inside the tier below


def measure(name, obj):
    """Built footprint below / above the first setback (or the frame), the tip, in the piece's frame (pivot = footprint centre)."""
    o, vs = ORDERS[name], _verts(obj)
    sp = _split(o)
    zmax = max(v[2] for v in vs)
    roof = None if o["kind"] == "megastructure" else o["regen"]["tiers"][-1][2] + 1.2  # body roof incl. parapet: the crown stands above
    return dict(split=sp, roof=roof, lower=extents(vs, -1e9, sp + EPS), upper=extents(vs, sp + EPS, roof + EPS if roof else 1e9),
                crown=extents(vs, roof + EPS, 1e9) if roof else None, tip=round(zmax, 3), tipPlan=round(zmax + (GRID["deck"] if o["kind"] == "tower" else 0.0), 3))


def _excess(o, fn):
    """Probe build with plates at the order's widths; per tier how far piers, bands and parapets stand proud of the plate."""
    s = Shape("DK_probe")
    fn(s, o, [0.0] * len(o["regen"]["tiers"]), 1.0)
    obj = s.build(None, bevel=0.0)
    vs, out = _verts(obj), []
    for i, (w, z0, z1) in enumerate(_tiers(o)):
        lo = z0 + (5.0 if i == 0 else 0.0)  # the base canopy is trimmed separately
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
    g = GRID
    (wl, d, top), t = o["regen"]["body"], _tiers(o)
    fl = g["storey"]
    for w, z0, z1 in t:
        z = z0
        while z < z1 - EPS:
            s.extrude(stadium(w, d), z, z + 1.2, pal.Render_White, top=False, bottom=False)
            s.extrude(stadium(w - 0.6, d - 0.6), z + 1.2, z + 3.2, pal.Window_Tower, top=False, bottom=False)
            s.extrude(stadium(w, d), z + 3.2, z + fl, pal.Render_White, top=(z + fl >= z1 - EPS), bottom=False)
            z += fl
    s.box((-0.5, -(d / 2 + 0.7), 0.0), (0.5, -(d / 2 - 0.6), top + 6.5), pal.Enamel_Coral)  # front fin
    R = t[-1][0] / 2 + g["halfBay"] - 0.02               # the saucer overhangs the upper tier by the half bay (teal ring 0.02 proud)
    total = _num(o["regen"]["crown"], r"total ≤ (\d+(?:\.\d+)?)", o["id"])[0]
    stem_h = 1.6
    sk = R / 15.02
    prof = [(2.4, 0.0), (13.5, 2.0), (15.0, 3.2), (15.0, 3.8), (13.0, 5.4), (6.0, 7.0), (0.0, 7.4)]
    zs = top + stem_h
    assert stem_h + prof[-1][1] <= total + EPS, "saucer taller than the order's total"
    s.lathe([(3.0, top), (3.0, zs), (2.4, zs)], pal.Render_White, seg=24, cap_bottom=False)
    s.lathe([(r * sk if r > 3 else r, zs + z) for r, z in prof], pal.Enamel_Ivory, seg=48)
    s.lathe([(15.02 * sk, zs + 3.1), (15.02 * sk, zs + 3.9)], pal.Enamel_Teal, seg=48, cap_bottom=False, cap_top=False)
    from skyline import spire
    z0 = zs + prof[-1][1]
    spire(s, z0, _tip_top(o) - z0, 0.6)                  # cut to the tip


# ----------------------------------------------------------------- Tower_Atom / Tower_Lantern

def atom(s, o, ex, canopy):
    from highrise import atom_crown, stack
    t = _tiers(o)
    tiers = [((w - 2 * e, w - 2 * e, 0.25 * w, 0.0), z0, z1) for (w, z0, z1), e in zip(t, ex)]
    stack(s, tiers, canopy)
    top = t[-1][2]
    avail = _tip_top(o) - top - 1.1
    atom_crown(s, top + 1.1, k=0.99 * avail / 26.9)


def lantern(s, o, ex, canopy):
    from highrise import band, lantern_crown, stack
    t = _tiers(o)
    tiers = [((w - 2 * e, w - 2 * e, 0.28 * w, 0.0), z0, z1) for (w, z0, z1), e in zip(t, ex)]
    stack(s, tiers, canopy)
    for plan, z0, z1 in tiers[:-1]:                      # coral banding mid-tier
        z = round((z0 + z1) / 2 / GRID["storey"]) * GRID["storey"]
        band(s, plan, z, z + 0.7, pal.Enamel_Coral, out=0.33)
    top = t[-1][2]
    finial = _tip_top(o) - (top + 1.1) - 10.4           # dome base sits 10.4 m over the crown base (gallery 1.6, lantern 8, rim 0.7, dome 0)
    lantern_crown(s, top + 1.1, finial=finial)


def _inset_tower(fn):
    def build(s, o):
        ex = _excess(o, fn)
        canopy = max(0.05, ex[0] - 0.3)
        fn(s, o, ex, canopy)
    return build


TOWERS = {"Tower_Stepped": ("Skyline", stepped, False), "Tower_Saucer": ("Skyline", saucer, False),
          "Tower_Atom": ("Highrise", atom, True), "Tower_Lantern": ("Highrise", lantern, True)}


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


# ----------------------------------------------------------------- registry

def _register():
    for name, o in ORDERS.items():
        if o["kind"] == "tower":
            cat, fn, inset = TOWERS[o["from"]]
            build = _inset_tower(fn) if inset else (lambda s, o, fn=fn: fn(s, o, [0.0] * len(o["regen"]["tiers"]), 1.0))
        else:
            cat, build = "Skyport", portal
        asset(name, cat, bevel=0.0)(lambda s, o=o, build=build: build(s, o))


_load()
_register()
