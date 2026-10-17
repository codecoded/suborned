"""The harbour liner berth (brief 007 round 2, Sky: "LinerDock reads as a lattice rocket gantry, not a liner berth").
Rockets stay at the horizon pads; the berth is a harbour liner terminal (Horizon/pano/pano_harbour, AtomicAge
monorail_terminal and grand_hotel for the deco language).

  LinerDock@liner-berth  the deco liner terminal on the caisson (plan landmarks.liner-berth: disc r 22, top 104 on a
                         1.2 m caisson deck, so the piece fits inside r 21.8 and rises <= 102.8 above the deck):
                         a two-storey streamline terminal hall, a fluted harbour tower with an observation gallery, a
                         glazed lantern and a needle mast with a gold starburst and a rose beacon; on the water side two
                         lattice boarding gantries with enclosed streamline gangways at the liner's promenade-deck level
                         (10 m above the water) and the upper deck (17 m), bollards and capstans on the caisson edge.
                         Pivot = caisson centre on its deck (z 0). The water side is local -X (plan +x, east, once the
                         FBX axes are baked: kit x = -Blender x).
  OceanLiner             concept B "atomic" (brief 007 round 3, Sky; briefs/007/liner-B-atomic-*.jpg), 96 m: a flared white
                         hull with a raked, flared stem, a coral boot-top, a gold sheer line, gold speed lines and an
                         atom crest on the bow, portholes; a glazed observation dome on chrome ribs at the bow (the
                         signature from the 38 deg camera); four rounded stepped decks with ribbon windows, wraparound
                         promenades, chrome rails and teak decks; lifeboats in davits; twin swept-back coral tail-fin
                         funnels with teal bands; a chrome radar mast topped by an atom (SpaceHQ's crown in small).
                         Pivot = waterline amidships (z 0 = water), bow to -Y. Boarding doors at the berth's gangways:
                         y +-9, floors 8.8 / 15.8 m on walls at half-beam 6.1 / 4.8 (LinerDock's hoods).
"""

import math

from geo import circle, rrect
from kit import asset
from mats import accent, pal, register
from mega_parts import strut, truss
from skyport_parts import ball
from structures import sec, tube

register("DK_Hull_Navy", "enamel", "#24406A")        # dE2000 23.8 from rivals
register("DK_Hull_Ivory", "enamel", "#F4EFE3")
register("DK_Steel_Ivory", "enamel", "#E4DFD2")
register("DK_Window_Teal", "enamel", "#2E6D70")
register("DK_Window_Tower", "enamel", "#34434B")
register("DK_Enamel_Turquoise", "enamel", accent("turquoise"))
register("DK_Enamel_Flamingo", "enamel", accent("flamingo"))
register("DK_Enamel_Sunshine", "enamel", accent("sunshine"))
register("DK_Enamel_Coral17", "enamel", accent("coral"))
register("DK_Lamp_Deco", "emit", "#FFF1C4", 5.0)


# ----------------------------------------------------------------- the terminal

def _streamline_block(s, w, d, z0, z1, mat, bands=(), glass=(), r=None):
    """Rounded-end block w (X) x d (Y): ribbon glass bands (z pairs) and turquoise speed lines, both slightly proud."""
    r = r if r is not None else min(w, d) / 2 - 0.05
    s.extrude(rrect(w, d, r, n=8), z0, z1, mat, smooth_sides=True)
    for za, zb in glass:
        s.extrude(rrect(w + 0.08, d + 0.08, r + 0.04, n=8), za, zb, pal.Window_Teal, smooth_sides=True, top=False, bottom=False)
        s.extrude(rrect(w + 0.16, d + 0.16, r + 0.08, n=8), zb, zb + 0.18, pal.Chrome, smooth_sides=True, top=False, bottom=False)
    for za, zb in bands:
        s.extrude(rrect(w + 0.14, d + 0.14, r + 0.07, n=8), za, zb, pal.Enamel_Turquoise, smooth_sides=True, top=False, bottom=False)


def _gangway(s, a, b, h=2.6, w=2.6):
    """Enclosed streamline gangway tube from a to b (3D points): ivory shell, window band, turquoise keel stripe."""
    tube(s, [a, b], sec(-w / 2, w / 2, -h / 2, h / 2, 1.0), pal.Hull_Ivory)
    tube(s, [a, b], sec(-w / 2 - 0.03, w / 2 + 0.03, 0.0, 0.7, 0.3), pal.Window_Teal, caps=False)
    tube(s, [a, b], sec(-0.5, 0.5, -h / 2 - 0.03, -h / 2 + 0.2), pal.Enamel_Turquoise, caps=False)


def _harbour_tower(s, cx, cy, z0):
    """Fluted harbour tower: limestone piers, teal slots, gold ribs, a gallery, a glazed lantern, dome, mast, starburst."""
    from spacehq import fluted_tier, setback
    with s.at((cx, cy, 0.0)):
        fluted_tier(s, 5.8, z0, 46.0, 16)
        setback(s, 5.8, 46.0, 4.4)
        fluted_tier(s, 4.4, 46.0, 62.0, 12)
        setback(s, 4.4, 62.0, 0.0)
        from mega_parts import lens
        lens(s, 62.4, 3.0, 9.0, 2.2, pal.Chrome, bead=pal.Gold, seg=48)       # observation gallery
        oct_ = circle(3.6, n=8)
        s.extrude(oct_, 64.6, 71.0, pal.Window_Teal)                           # lantern
        for x, y in oct_:
            s.box((x - 0.12, y - 0.12, 64.6), (x + 0.12, y + 0.12, 71.0), pal.Chrome)
        s.lathe([(4.1, 71.0), (4.1, 71.6), (3.2, 72.4)] + [(3.2 * math.cos(math.pi / 2 * k / 6), 72.4 + 3.2 * math.sin(math.pi / 2 * k / 6)) for k in range(1, 7)],
                pal.Hull_Ivory, seg=32)
        s.lathe([(4.15, 71.1), (4.15, 71.5)], pal.Gold, seg=32, cap_bottom=False, cap_top=False)
        s.lathe([(0.6, 75.4), (0.42, 86.0), (0.22, 96.0), (0.08, 101.6), (0.0, 102.4)], pal.Chrome, seg=10, cap_bottom=False)
        for zc, r in ((80.0, 0.5), (88.0, 0.36)):
            s.lathe([(r, zc - 0.3), (r + 0.35, zc), (r, zc + 0.3)], pal.Gold, seg=10, cap_bottom=False, cap_top=False)
        with s.at((0, 0, 88.0)):                                               # Grand Hotel starburst finial
            ball(s, (0, 0, 0), 0.9, pal.Gold, seg=16, n=8)
            for tilt, n, L in ((90, 8, 4.2), (45, 6, 3.0), (135, 6, 3.0)):
                for i in range(n):
                    with s.at((0, 0, 0), (tilt, 0, 360.0 * i / n)):
                        s.lathe([(0.22, 0.0), (0.1, L * 0.55), (0.0, L)], pal.Gold, seg=6)
        ball(s, (0.0, 0.0, 99.0), 0.45, pal.Beacon_Rose, seg=12, n=6)


@asset("LinerDock@liner-berth", "Landmarks", bevel=0.0)
def liner_terminal(s):
    # terminal hall: two storeys on the land side, streamline rounded ends, ribbon windows, a roof terrace
    hx = 5.0                                                              # hall centre x (land side)
    with s.at((hx, 0.0, 0.0)):
        _streamline_block(s, 18.0, 30.0, 0.0, 10.4, pal.Limestone_Tex, bands=((4.6, 5.0), (9.6, 10.0)), glass=((1.4, 4.2), (6.0, 9.2)))
        # coping and terrace: corner radius 0.05 under the half width, as _streamline_block does. At r = w / 2 the two
        # arcs of each end meet in a zero-length side (repeated vertices), which Unity's importer drops as
        # self-intersecting polygons (brief 007 round 3; L1 and L2 inherited them through the decimation).
        s.extrude(rrect(18.6, 30.6, 9.25, n=8), 10.4, 11.0, pal.Limestone_Tex, smooth_sides=True)         # coping
        s.extrude(rrect(17.4, 29.4, 8.65, n=8), 11.0, 11.03, pal.Terrazzo_Ivory)
        # the land-side entrance: a coral-inlaid arched portal and a gold canopy fin over it
        with s.at((9.0, 0.0, 0.0), (90, 0, 90)):                           # local XY = world (y, z), extrude along +x
            arch = [(3.0 * math.cos(math.pi * k / 12), 4.6 + 3.0 * math.sin(math.pi * k / 12)) for k in range(13)]
            s.extrude([(3.0, 0.0)] + arch + [(-3.0, 0.0)], 0.0, 0.06, pal.Window_Teal)
            ring = [(3.4 * math.cos(math.pi * k / 12), 4.6 + 3.4 * math.sin(math.pi * k / 12)) for k in range(13)]
            for k in range(12):
                q = [arch[k], ring[k], ring[k + 1], arch[k + 1]]
                s.extrude(q, 0.0, 0.14, pal.Enamel_Coral17)
            s.extrude([(-0.35, 8.2), (0.35, 8.2), (0.35, 10.8), (-0.35, 10.8)], 0.0, 0.5, pal.Gold)
        for y in (-11.0, -6.0, 6.0, 11.0):                                   # roof flagpoles with pennants
            with s.at((0.0, y, 0.0)):
                s.lathe([(0.08, 11.0), (0.05, 18.0)], pal.Chrome, seg=6)
                m = (pal.Enamel_Flamingo, pal.Enamel_Turquoise, pal.Enamel_Sunshine, pal.Enamel_Turquoise)[int(abs(y)) % 4]
                s.box((0.0, -0.02, 16.4), (1.8, 0.02, 17.6), m)
    _harbour_tower(s, hx + 2.0, 0.0, 11.0)
    # boarding gantries on the water side (local -X): lattice towers, gangways at the promenade deck (+8.8 = 10 m over the
    # water) and the upper deck (+15.8), gangway heads cantilevered to the caisson edge with hooded bellows
    for sy in (-1, 1):
        gx, gy = -15.0, sy * 9.0
        truss(s, (gx, gy, 0.0), (gx, gy, 24.0), 1.5, 1.5, 8, pal.Steel_Ivory, chord=0.36, lace=0.18)
        s.box((gx - 1.8, gy - 1.8, 24.0), (gx + 1.8, gy + 1.8, 24.4), pal.Steel_Ivory)
        s.box((gx - 1.9, gy - 1.9, 24.4), (gx + 1.9, gy + 1.9, 24.7), pal.Enamel_Turquoise)
        with s.at((gx, gy, 0.0)):
            s.lathe([(0.07, 24.7), (0.04, 29.0)], pal.Chrome, seg=6)
            ball(s, (0.0, 0.0, 29.1), 0.22, pal.Lamp_Deco, seg=10, n=5)
        # heads reach the liner's walls as sited by Datum (OceanLiner centre plan x 402.6 = 30.6 m east of the caisson centre):
        # deck 1 side (half width 6.1) at 10 m over the water, deck 3 side (4.8) at 17 m; the hood's face sits on the wall
        for zg, x_end in ((8.8, -(30.6 - 6.1)), (15.8, -(30.6 - 4.8))):
            _gangway(s, (hx - 9.2, gy, zg), (gx + 1.5, gy, zg))
            _gangway(s, (gx - 1.5, gy, zg), (x_end + 0.9, gy, zg), h=2.4, w=2.4)
            s.box((x_end, gy - 1.5, zg - 1.5), (x_end + 0.9, gy + 1.5, zg + 1.5), pal.Window_Tower)  # bellows hood
            s.box((x_end + 0.05, gy - 1.6, zg + 1.4), (x_end + 1.1, gy + 1.6, zg + 1.65), pal.Enamel_Coral17)
            strut(s, (gx, gy, zg + 6.0), (x_end + 1.4, gy, zg + 1.3), 0.14, pal.Steel)  # stay
    # caisson edge furniture on the water side: bollards and capstans
    for k in range(-4, 5):
        a = math.radians(180 + 13 * k)
        x, y = 20.6 * math.cos(a), 20.6 * math.sin(a)
        with s.at((x, y, 0.0)):
            if k % 2:
                s.lathe([(0.35, 0.0), (0.3, 0.6), (0.42, 0.7), (0.42, 0.85), (0.0, 0.85)], pal.Iron, seg=10)
            else:
                s.lathe([(0.6, 0.0), (0.45, 0.3), (0.45, 0.8), (0.55, 0.9), (0.0, 0.95)], pal.Steel_Ivory, seg=12)
    # lamp standards round the forecourt
    from spacehq import _lamp_standard
    for k in range(6):
        a = math.radians(30 + 60 * k)
        _lamp_standard(s, 19.0 * math.cos(a), 19.0 * math.sin(a), 0.0)


# ----------------------------------------------------------------- the ship (concept B, "atomic")
#
# Brief 007 round 3 (Sky: the liner is concept B, briefs/007/liner-B-atomic-*.jpg). Blender axes: bow -Y, beam X, z 0 = the
# waterline, pivot amidships. The berth is on +X (plan -x: kit x = -Blender x), but the ship is symmetric. Interface with
# LinerDock@liner-berth (above, unchanged): its gangways reach y = +-9 with floors 8.8 / 15.8 m over the water (centres 10 / 17)
# and their hoods' faces on walls at half-beam 6.1 and 4.8, so tier A (the promenade deck, floor 8.8) stands at 6.1 and tier C
# (floor 15.8) at 4.8, with boarding doors there and gaps in the bulwark and rails. Plan envelope (signatures.ocean-liner):
# beam <= 16.4 (half 8.2), length <= 96, top ~34.

# unlit dE2000 from cones / guard / rivals, as measured when they were chosen (brief 007; no gate holds them to it since 2026-10-16). The accents are
# the shared look.json roles (mats.py): DK_Accent_Coral #FC9C84 21.7 (boot-top, fins, lifeboat hoods), DK_Accent_Teal #36B0A8 43.3
# (fin bands, bridge wings), DK_Accent_Gold #D0C848 20.9 (sheer line, speed lines, bow atom, mast orbits).
register("DK_Liner_White", "enamel", "#F7F4EC")                  # 30.3 (guard): the hull and superstructure
register("DK_Liner_Teak", "stone", "#9E7E62")                    # 22.0 (guard): decks (a warmer #B48C5E teak is 15.3)
register("DK_Liner_Dome", "enamel", "#8CC3C8")                   # 36.2: the observation dome's glazing, opaque gloss so it reads
register("DK_Liner_Pool", "enamel", accent("turquoise"))         # sun-deck pool


ZK, ZS = -2.0, 11.4          # keel; top of the stem
SHEER = 8.8                  # main (promenade) deck amidships = the lower gangway's floor
BOOT = 1.8                   # top of the coral boot-top
BW = 1.0                     # bulwark height
GANG_Y = (-9.0, 9.0)         # gangway stations (both sides; the berth uses +X)


def _zn(z):
    return min(1.0, max(0.0, (z - ZK) / (ZS - ZK)))


def _hx(u, z):
    """Half beam at station u (0 bow .. 1 stern) and height z: fine entry flaring out above the water, cruiser stern."""
    zn = _zn(z)
    run, p = 0.36 - 0.08 * zn, 0.8 - 0.38 * zn
    bow = min(1.0, u / run) ** p
    st = math.sqrt(max(0.0, 1.0 - max(0.0, (u - 0.86) / 0.14) ** 2))
    return (8.12 - 1.7 * math.exp(-(z - ZK) / 1.5)) * min(bow, st)


def _hy(u, z):
    """Station u at height z: a raked, flared stem (-44.2 at the keel, -47.9 at the top), an overhanging counter stern."""
    zn = _zn(z)
    yb, ys = -44.2 - 3.7 * zn ** 1.4, 44.5 + 3.3 * zn ** 0.7
    return yb + (ys - yb) * u


def _ztop(u):
    """Sheer: the main deck, rising to the bow and a little to the stern."""
    return SHEER + 2.6 * max(0.0, 1 - u / 0.3) ** 2 + 0.6 * max(0.0, (u - 0.85) / 0.15) ** 2


def _u_at(y):
    return (y - _hy(0, SHEER)) / (_hy(1, SHEER) - _hy(0, SHEER))


def _U(t):
    return (1 - math.cos(math.pi * t)) / 2   # stations crowd the bow and stern


def _hull_sheet(s, sx, u0, u1, zlo, zhi, mat, nu, nv, lift=0.0, inner=False, dx=0.0):
    """A strip of the hull surface on side sx between u0..u1 and heights zlo(u)..zhi(u), lifted `lift` off it
    (or set `dx` inboard of the sheer line: bulwarks). Faces outward (inward if inner)."""
    flip = (sx < 0) != inner

    def fn(a, v):
        u = u0 + (u1 - u0) * _U(1 - a if flip else a)
        za, zb = zlo(u), zhi(u)
        z = za + (zb - za) * v
        x = _hx(u, min(z, _ztop(u))) + lift - dx
        return (sx * max(x, 0.0), _hy(u, min(z, ZS)), z)
    s.sheet(fn, nu, nv, mat)


def _gaps(u0, u1, ys, half):
    """[u0, u1] split around stations ys (each +-half m)."""
    cuts = sorted((_u_at(y - half), _u_at(y + half)) for y in ys)
    out, a = [], u0
    for c0, c1 in cuts:
        if c0 > a:
            out.append((a, c0))
        a = max(a, c1)
    if a < u1:
        out.append((a, u1))
    return out


def _hull(s):
    n = 40
    top = lambda u: _ztop(u)                                            # noqa: E731
    for sx in (1, -1):
        _hull_sheet(s, sx, 0.0, 1.0, lambda u: ZK, lambda u: BOOT, pal.Accent_Coral, n, 3)
        _hull_sheet(s, sx, 0.0, 1.0, lambda u: BOOT, top, pal.Liner_White, n, 4)
        # gold sheer line below the deck, and the bow's three gold speed lines
        _hull_sheet(s, sx, 0.012, 0.985, lambda u: _ztop(u) - 0.9, lambda u: _ztop(u) - 0.62, pal.Accent_Gold, n, 1, lift=0.035)
        for k in range(3):
            zc = 7.2 - 0.62 * k
            _hull_sheet(s, sx, 0.085, 0.2 - 0.025 * k, lambda u, zc=zc: zc, lambda u, zc=zc: zc + 0.16, pal.Accent_Gold, 6, 1, lift=0.035)
        # bulwark: outer and inner faces and a coping, open at the gangway stations
        for a, b in _gaps(0.0, 1.0, GANG_Y, 1.5):
            m = max(2, int(n * (b - a)))
            hi = lambda u: _ztop(u) + BW                                # noqa: E731
            _hull_sheet(s, sx, a, b, lambda u: _ztop(u) - 0.15, hi, pal.Liner_White, m, 1)
            _hull_sheet(s, sx, a, b, top, hi, pal.Liner_White, m, 1, inner=True, dx=0.18)
            flip = sx < 0

            def cope(t, v, a=a, b=b, flip=flip, sx=sx):
                u = a + (b - a) * _U(1 - t if flip else t)
                z = _ztop(u) + BW
                return (sx * max(_hx(u, _ztop(u)) - 0.18 * v, 0.0), _hy(u, ZS if z > ZS else z), z)
            s.sheet(cope, m, 1, pal.Liner_White)
    # flat bottom and the teak main deck
    s.sheet(lambda a, v: ((2 * v - 1) * _hx(_U(a), ZK), _hy(_U(a), ZK), ZK), n, 1, pal.Accent_Coral)
    s.sheet(lambda a, v: ((1 - 2 * v) * max(_hx(_U(a), _ztop(_U(a))) - 0.18, 0.0), _hy(_U(a), min(_ztop(_U(a)), ZS)), _ztop(_U(a))),
            n, 2, pal.Liner_Teak)


def _frame(u, z, sx):
    """Point on the hull and its frame: T along the side (aft on starboard), V up, N outward = T x V."""
    from mathutils import Vector
    P = lambda uu, zz: Vector((sx * _hx(uu, zz), _hy(uu, zz), zz))      # noqa: E731
    T = (P(u + 1e-3, z) - P(u - 1e-3, z)).normalized() * sx
    V = (P(u, z + 1e-3) - P(u, z - 1e-3)).normalized()
    N = T.cross(V).normalized()
    V = N.cross(T).normalized()
    return P(u, z), T, V, N


def _on_hull(s, u, z, sx, pts2d, lift, mat):
    P, T, V, N = _frame(u, z, sx)
    s.face([tuple(P + a * T + b * V + lift * N) for a, b in pts2d], mat)


def _portholes(s):
    for zp in (4.7, 6.5):
        for k in range(36):
            y = -26.0 + 2.05 * k
            if zp > 6 and y < -22:
                continue
            u = _u_at(y)
            for sx in (1, -1):
                _on_hull(s, u, zp, sx, circle(0.27, 8), 0.03, pal.Window_Teal)


def _hull_windows(s):
    """A row of square saloon windows high on the hull, between the gold line and the portholes."""
    sq = [(-0.3, -0.36), (0.3, -0.36), (0.3, 0.36), (-0.3, 0.36)]
    for k in range(40):
        y = -18.0 + 1.6 * k
        u = _u_at(y)
        z = _ztop(u) - 1.36
        for sx in (1, -1):
            _on_hull(s, u, z, sx, sq, 0.03, pal.Window_Teal)


def _bow_atom(s):
    """The bow's gold atom (the line's crest, quay concept): nucleus and three orbits on the flare, ahead of the speed lines."""
    for sx in (1, -1):
        u, z = 0.066, 6.6
        _on_hull(s, u, z, sx, circle(0.32, 8), 0.12, pal.Accent_Gold)
        for j, rot in enumerate((0.0, 60.0, 120.0)):
            c, sn = math.cos(math.radians(rot)), math.sin(math.radians(rot))
            ring = []
            for k in range(17):
                t = 2 * math.pi * k / 16
                ring.append((1.25 * math.cos(t), 0.5 * math.sin(t)))
            for k in range(16):
                q = []
                for (ex, ey), f in ((ring[k], 1.0), (ring[k + 1], 1.0), (ring[k + 1], 0.8), (ring[k], 0.8)):
                    ax, by = ex * f, ey * f
                    q.append((ax * c - by * sn, ax * sn + by * c))
                _on_hull(s, u, z, sx, q, 0.1 + 0.005 * j, pal.Accent_Gold)


def _rail(s, poly, z, gaps=(), h=0.95, mat=None):
    """White ship's rail along a closed outline (points), open where any point falls in a gap box (x0, x1, y0, y1)."""
    from geo import thicken
    inside = [any(x0 <= x <= x1 and y0 <= y <= y1 for x0, x1, y0, y1 in gaps) for x, y in poly]
    if not any(inside):
        runs = [poly + poly[:1]]
    else:
        k0 = inside.index(True)
        order = poly[k0:] + poly[:k0]
        flags = inside[k0:] + inside[:k0]
        runs, cur = [], []
        for p, f in zip(order + order[:1], flags + flags[:1]):
            if f:
                if len(cur) > 1:
                    runs.append(cur)
                cur = []
            else:
                cur.append(p)
        if len(cur) > 1:
            runs.append(cur)
    for r in runs:
        s.extrude(thicken(r, 0.06), z, z + h, mat or pal.Liner_White, top=False, bottom=False)


def _tier(s, hw, ya, yb, z0, z1, slab, win, rail_gaps=(), slab_t=0.3, teak=False):
    """One rounded superstructure tier: white wall with a proud ribbon window band and a chrome sill, a white deck slab
    overhanging to `slab` with a teak top, a white rail on its edge."""
    yc, d = (ya + yb) / 2, yb - ya
    with s.at((0.0, yc, 0.0)):
        s.extrude(rrect(2 * hw, d, hw - 0.05, n=8), z0, z1, pal.Liner_White, smooth_sides=True, bottom=False, top=False)
        wa, wb = win
        s.extrude(rrect(2 * hw + 0.08, d + 0.08, hw - 0.01, n=8), wa, wb, pal.Window_Teal, smooth_sides=True, top=False, bottom=False)
        s.extrude(rrect(2 * hw + 0.14, d + 0.14, hw + 0.02, n=8), wa - 0.14, wa, pal.Chrome, smooth_sides=True, top=False, bottom=False)
        so = rrect(2 * slab, d + 2 * (slab - hw), slab - 0.05, n=8)
        s.extrude(so, z1, z1 + slab_t, pal.Liner_White, smooth_sides=True, top=False)
        s.face([(x, y, z1 + slab_t) for x, y in so], pal.Liner_Teak if teak else pal.Liner_White)
        inner = rrect(2 * slab - 0.2, d + 2 * (slab - hw) - 0.2, slab - 0.15, n=8)
        _rail(s, inner, z1 + slab_t, [(x0, x1, y0 - yc, y1 - yc) for x0, x1, y0, y1 in rail_gaps])


def _door(s, x, y, z0, z1):
    """Boarding door on a wall at half-beam x (both sides): chrome frame, dark leaf, coral hood."""
    for sx in (1, -1):
        a, b = (x - 0.02, x + 0.08) if sx > 0 else (-x - 0.08, -x + 0.02)
        s.box((a, y - 1.1, z0), (b, y + 1.1, z1), pal.Chrome)
        a2, b2 = (x + 0.08, x + 0.1) if sx > 0 else (-x - 0.1, -x - 0.08)
        s.box((a2, y - 0.9, z0 + 0.1), (b2, y + 0.9, z1 - 0.2), pal.Window_Tower)
        a3, b3 = (x, x + 0.5) if sx > 0 else (-x - 0.5, -x)
        s.box((a3, y - 1.3, z1), (b3, y + 1.3, z1 + 0.15), pal.Accent_Coral)


def _superstructure(s):
    gA = [(sx * 5.5 - 1.0, sx * 5.5 + 3.0, y - 1.6, y + 1.6) for y in GANG_Y for sx in (1,)] + \
         [(-5.5 - 3.0, -5.5 + 1.0, y - 1.6, y + 1.6) for y in GANG_Y]
    # A promenade deck (floor 8.8, wall 6.1 = the lower gangway's hood), B, C (wall 4.8 = the upper hood), D bridge deck
    _tier(s, 6.1, -24.0, 30.0, 8.5, 12.0, 6.45, (9.9, 11.1), teak=True)
    _tier(s, 5.5, -21.0, 24.0, 12.3, 15.5, 6.2, (13.4, 14.6), rail_gaps=gA)
    _tier(s, 4.8, -18.0, 18.0, 15.8, 19.0, 5.3, (16.9, 18.1))
    _tier(s, 4.0, -16.0, -2.5, 19.3, 22.3, 4.4, (20.2, 21.6))
    for y in GANG_Y:
        _door(s, 6.1, y, 8.8, 11.3)
        _door(s, 4.8, y, 15.8, 18.3)
    # bridge wings, sun-deck pool aft on A's roof
    s.box((-6.2, -15.0, 21.15), (6.2, -13.0, 21.55), pal.Liner_White)
    s.box((-6.25, -15.05, 20.9), (6.25, -12.95, 21.15), pal.Accent_Teal)
    with s.at((0.0, 26.9, 0.0)):
        s.extrude(rrect(4.2, 2.6, 1.0, n=4), 12.3, 12.38, pal.Liner_White)
        s.face([(x, y, 12.385) for x, y in rrect(3.7, 2.1, 0.8, n=4)], pal.Liner_Pool)


def _dome(s, yc=-30.0):
    """The observation lounge: a drum with a ribbon window and gold collar under a glazed dome on chrome ribs."""
    R, z0, z1, H = 5.6, 8.9, 11.6, 4.7
    from geo import dome
    with s.at((0.0, yc, 0.0)):
        s.lathe([(R, z0), (R, z1), (R - 0.25, z1)], pal.Liner_White, seg=40, cap_bottom=False, cap_top=False)
        s.lathe([(R + 0.05, 9.7), (R + 0.05, 11.0)], pal.Window_Teal, seg=40, cap_bottom=False, cap_top=False)
        s.lathe([(R + 0.06, z1 - 0.32), (R + 0.16, z1 - 0.2), (R + 0.06, z1 - 0.06)], pal.Accent_Gold, seg=40, cap_bottom=False, cap_top=False)
        Rg = R - 0.25
        s.lathe(dome(Rg, z1, H, n=8), pal.Liner_Dome, seg=40, cap_bottom=False)
        for k in range(12):                                             # meridian ribs
            a = 2 * math.pi * k / 12
            pts = [((Rg + 0.04) * math.cos(math.pi / 2 * i / 7) * math.cos(a), (Rg + 0.04) * math.cos(math.pi / 2 * i / 7) * math.sin(a),
                    z1 + (H + 0.04) * math.sin(math.pi / 2 * i / 7)) for i in range(7)]
            s.sweep(pts, 0.08, pal.Chrome, seg=4, caps=False)
        for f in (0.42, 0.78):                                          # two latitude hoops
            ang = math.asin(f)
            r, z = (Rg + 0.05) * math.cos(ang), z1 + (H + 0.05) * f
            s.lathe([(r, z - 0.07), (r + 0.06, z), (r, z + 0.07)], pal.Chrome, seg=32, cap_bottom=False, cap_top=False)
        s.lathe([(0.55, z1 + H - 0.05), (0.45, z1 + H + 0.35), (0.08, z1 + H + 1.3), (0.0, z1 + H + 1.4)], pal.Accent_Gold, seg=10, cap_bottom=False)


def _fin_funnel(s, y0, zb=19.3, zt=31.0, chord=9.4, w=3.9, m=9, far=False):
    """Swept tail-fin funnel: airfoil sections raked aft, coral with a teal band edged white near the top."""
    def foil(z):
        f = (z - zb) / (zt - zb)
        c, ww = chord * (1 - 0.56 * f), w * (1 - 0.4 * f)
        le = y0 + (z - zb) * math.tan(math.radians(40))                  # swept straight back
        ss = [(1 - math.cos(math.pi * k / m)) / 2 for k in range(m + 1)]
        h = lambda q: ww / 2 * math.sqrt(q) * (1 - q) / 0.385 + 0.02     # noqa: E731
        star = [(h(q), le + c * q) for q in ss]
        port = [(-h(q), le + c * q) for q in ss[1:-1][::-1]]
        return star + port
    bands = ((zb, 22.5, pal.Accent_Coral), (22.5, 26.6, pal.Accent_Coral), (26.6, 27.0, pal.Liner_White), (27.0, 28.7, pal.Accent_Teal),
             (28.7, 29.1, pal.Liner_White), (29.1, zt, pal.Accent_Coral))
    if far:
        bands = ((zb, 27.0, pal.Accent_Coral), (27.0, 28.7, pal.Accent_Teal), (28.7, zt, pal.Accent_Coral))
    for i, (za, zc, mat) in enumerate(bands):
        s.loft([(foil(za), za), (foil(zc), zc)], mat, cap_bottom=False, cap_top=(i == len(bands) - 1))


def _atom_mast(s, y=-9.0, zb=22.6):
    """Chrome radar mast topped by an atom (SpaceHQ's crown in small: chrome nucleus, gold orbits), tip 34.0."""
    with s.at((0.0, y, 0.0)):
        s.lathe([(0.42, zb), (0.3, 27.0), (0.2, 30.4)], pal.Chrome, seg=10, cap_bottom=False, cap_top=False)
        s.box((-2.6, -0.12, 26.6), (2.6, 0.12, 26.85), pal.Chrome)                 # radar yard
        s.box((-1.6, -0.5, 28.0), (1.6, 0.5, 28.12), pal.Chrome)                  # radar scanner bar
        s.lathe([(0.8, 26.85), (0.9, 27.0), (0.0, 27.0)], pal.Accent_Teal, seg=12, cap_bottom=False)
        c = (0.0, 0.0, 31.9)
        ball(s, c, 0.72, pal.Chrome, seg=16, n=8)
        s.lathe([(0.75, c[2] - 0.08), (0.84, c[2]), (0.75, c[2] + 0.08)], pal.Accent_Gold, seg=16, cap_bottom=False, cap_top=False)
        for j, yaw in enumerate((0.0, 60.0, 120.0)):
            with s.at(c, (0, 0, yaw)), s.at((0, 0, 0), (68, 0, 0)):
                a, b = 2.3, 1.05
                pts = [(a * math.cos(t), b * math.sin(t), 0.0) for t in (2 * math.pi * k / 24 for k in range(25))]
                s.sweep(pts, 0.075, pal.Accent_Gold, seg=5, caps=False)
                t = 2 * math.pi * (0.15 + 0.3 * j)
                ball(s, (a * math.cos(t), b * math.sin(t), 0.0), 0.17, pal.Chrome, seg=8, n=4)
        s.lathe([(0.12, 30.4), (0.06, 33.4), (0.0, 34.0)], pal.Chrome, seg=6, cap_bottom=False)
        ball(s, (0.0, 0.0, 33.3), 0.13, pal.Beacon_Rose, seg=8, n=4)


def _lifeboats(s):
    """White boats with coral hoods hung in chrome davits off tier A's roof, clear of the upper gangways (y +-9)."""
    for sx in (1, -1):
        for y in (-15.6, -4.0, 0.6, 14.6, 19.2):
            x = sx * 7.25
            s.rbox((x - 0.72, y - 2.4, 12.9), (x + 0.72, y + 2.4, 13.75), 0.32, pal.Liner_White, seg=1)
            s.rbox((x - 0.6, y - 1.9, 13.75), (x + 0.6, y + 1.9, 14.2), 0.2, pal.Accent_Coral, seg=1)
            for dy in (-1.7, 1.7):
                s.sweep([(sx * 6.1, y + dy, 12.3), (sx * 6.25, y + dy, 14.5), (sx * 6.7, y + dy, 14.95), (x, y + dy, 14.85)],
                        0.07, pal.Chrome, seg=4, caps=False)


def _staffs(s):
    """Jackstaff at the stem, ensign staff at the stern with a flamingo pennant."""
    for y, z, h in ((_hy(0.02, ZS) + 0.9, _ztop(0.02) + BW, 3.2), (_hy(0.985, 9.6) - 1.2, _ztop(0.985) + BW, 4.2)):
        with s.at((0.0, y, 0.0)):
            s.lathe([(0.07, z), (0.04, z + h)], pal.Chrome, seg=6, cap_bottom=False)
    yst = _hy(0.985, 9.6) - 1.2
    s.box((-0.02, yst, _ztop(0.985) + BW + 2.8), (0.02, yst + 1.6, _ztop(0.985) + BW + 4.0), pal.Enamel_Flamingo)


def _collapse_pinched(s):
    """Port and starboard sheets meet at x = 0 on the stem and stern: a quad there repeats a point. Rebuild each as the
    triangle it is (or drop it), so no face carries a repeated vertex into the FBX."""
    key = lambda v: (round(v.co.x, 5), round(v.co.y, 5), round(v.co.z, 5))   # noqa: E731
    bad = [f for f in s.bm.faces if len({key(v) for v in f.verts}) < len(f.verts)]
    for f in bad:
        seen, vs = set(), []
        for v in f.verts:
            if key(v) not in seen:
                seen.add(key(v))
                vs.append(v)
        mi, sm = f.material_index, f.smooth
        s.bm.faces.remove(f)
        if len(vs) >= 3:
            try:
                nf = s.bm.faces.new(vs)
            except ValueError:
                continue
            nf.material_index, nf.smooth = mi, sm
    s._pending_uv = [(f, uv) for f, uv in s._pending_uv if f.is_valid]


def _far(s):
    """The hand-built L2 (~0.7k tris; kit.asset far=): the 6% collapse flattened the fins and the dome. The same hull at 14
    stations, the four tiers with one window band each, the dome, both fins with their teal band, the mast and its atom,
    a coral line for the lifeboats."""
    n = 14
    for sx in (1, -1):
        _hull_sheet(s, sx, 0.0, 1.0, lambda u: ZK, lambda u: BOOT, pal.Accent_Coral, n, 1)
        _hull_sheet(s, sx, 0.0, 1.0, lambda u: BOOT, lambda u: _ztop(u) + BW, pal.Liner_White, n, 2)
        _hull_sheet(s, sx, 0.012, 0.985, lambda u: _ztop(u) - 0.9, lambda u: _ztop(u) - 0.5, pal.Accent_Gold, n, 1, lift=0.04)
    s.sheet(lambda a, v: ((1 - 2 * v) * _hx(_U(a), _ztop(_U(a))), _hy(_U(a), min(_ztop(_U(a)), ZS)), _ztop(_U(a)) + 0.3), n, 1, pal.Liner_Teak)
    for hw, ya, yb, z0, z1, wa, wb in ((6.1, -24.0, 30.0, 8.5, 12.3, 9.9, 11.1), (5.5, -21.0, 24.0, 12.3, 15.8, 13.4, 14.6),
                                       (4.8, -18.0, 18.0, 15.8, 19.3, 16.9, 18.1), (4.0, -16.0, -2.5, 19.3, 22.6, 20.2, 21.6)):
        with s.at((0.0, (ya + yb) / 2, 0.0)):
            o = rrect(2 * hw, yb - ya, hw - 0.05, n=3)
            s.extrude(o, z0, z1, pal.Liner_White, bottom=False)
            s.extrude(rrect(2 * hw + 0.1, yb - ya + 0.1, hw, n=3), wa, wb, pal.Window_Teal, top=False, bottom=False)
    for sx in (1, -1):
        s.box((min(sx * 6.5, sx * 7.9), -17.0, 12.9), (max(sx * 6.5, sx * 7.9), 21.0, 14.0), pal.Accent_Coral)
    from geo import dome
    with s.at((0.0, -30.0, 0.0)):
        s.lathe([(5.6, 8.9), (5.6, 11.6)], pal.Liner_White, seg=16, cap_bottom=False, cap_top=False)
        s.lathe(dome(5.35, 11.6, 4.7, n=4), pal.Liner_Dome, seg=16, cap_bottom=False)
    _fin_funnel(s, -1.0, m=4, far=True)
    _fin_funnel(s, 8.6, m=4, far=True)
    with s.at((0.0, -9.0, 0.0)):
        s.lathe([(0.4, 22.6), (0.15, 33.6), (0.0, 34.0)], pal.Chrome, seg=4, cap_bottom=False)
        ball(s, (0.0, 0.0, 31.9), 0.8, pal.Chrome, seg=8, n=4)
        for yaw in (0.0, 60.0, 120.0):
            with s.at((0, 0, 31.9), (0, 0, yaw)), s.at((0, 0, 0), (68, 0, 0)):
                s.sweep([(2.3 * math.cos(t), 1.05 * math.sin(t), 0.0) for t in (2 * math.pi * k / 10 for k in range(11))], 0.12, pal.Accent_Gold, seg=3, caps=False)
    _collapse_pinched(s)


@asset("OceanLiner", "Landmarks", bevel=0.0, far=_far)
def ocean_liner(s):
    _hull(s)
    _portholes(s)
    _hull_windows(s)
    _bow_atom(s)
    _dome(s)
    _superstructure(s)
    _lifeboats(s)
    _fin_funnel(s, -1.0)
    _fin_funnel(s, 8.6)
    _atom_mast(s)
    _staffs(s)
    _collapse_pinched(s)
