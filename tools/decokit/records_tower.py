"""RecordsTower (brief 072 A, Vela; Sky 2026-10-27 "yes"): the Records Office as one landmark kit piece, the one tall building of the play streets, in place of
the procedural "grand" mass. Drawn from the 26 Sep paintings (records-office, mission-block): a cream limestone wing with bull-nosed corners, horizontal seafoam
bands that wrap the rounds (the storey band at 5.0..5.9 m deep teal, DecoKit's old civic office band: brief 072-G, Sky 2026-10-27 "yep"), a giant order of limestone pilasters with seafoam flutes over two-storey glazing, a planted roof terrace with a glass rotunda,
and two pylon towers with seafoam fins round a glazed stair slot, a stepped crown and a chrome sphere at 38 m.

Footprint, entrances and heights are civic-block.json's `civic-office` (rule C-0; this piece never changes the block): the wing polygon (x 55.5..72.5,
y -40.5..-19.5, its four chamfers rounded INSIDE the polygon, never outside it), wings 15.5 m to the coping, giant order 12 m (0.45..12.45), the two 6 x 6 m
towers (public x 52.5..58.5, official x 69.5..75.5, y -33..-27) to the crown at 38.0. The entrances are the block's dressing pieces, left their faces:
  office-entrance (public, RecordsEntrance at block (64, -40.5), yawDeg 0): the wing's west face is a flat stone wall from x 59.9 to 68.1 up to 12.75 m;
  official-entrance (RecordsEntrance_Official at (75.5, -30), yawDeg 90): tower official's south face is flat stone from 0 to 5.9 m;
  office-service-door (block (62.5, -18.9), east face y -19.5): drawn here, a teal service door under a streamline canopy, as the alley walls'.
At ground level nothing stands more than 0.3 m off the block polygon (the giant order's granite feet; the plinth 0.08) but the service door's 0.15 m step,
0.44 m into the service yard (the door point at 0.6 m stays clear); above 2.6 m the door canopy (0.87), above 4.8 m the bands, cornice and fins stand up to 0.35 m off it.

Frame (a civic dressing piece's): pivot at block (64, -30), z 0 = the pavement top; kit X = block x - 64 (block south), kit Y = block y + 30 (block east),
front -Y = the block's west (the public face, onto Rocket Lane). Place it as civic-block.json `dressing.pieces` are placed: at [64, -30], yawDeg 0.
L1 and L2 are hand-built (kit.asset near= / far=): no planting, mullions or beads at L1; L2 the masses, bands, glass and crowns."""

import math

from alleys import remap
from alleys_parts import canopy, door
from civic_common import hair, planting, split_runs, trough
from geo import rrect
from kit import asset
from mats import pal
from plaza_parts import ball, lift, offset, ribbon
from structures import along, ccw, sec, tube

OX, OY = 64.0, -30.0
# wing polygon (kit frame), CCW from the front's left end; per vertex the tangent lengths of its round (incoming edge, outgoing edge)
WING = [(-5.1, -10.5), (6.8, -10.5), (8.5, -8.5), (8.5, 10.5), (-5.1, 10.5), (-8.5, 5.5), (-8.5, -6.5)]
TAN = [(2.2, 1.0), (1.3, 1.3), (1.3, 1.3), (2.0, 2.0), (2.4, 3.0), (3.0, 2.0), (2.8, 3.0)]
TOWERS = {"public": (-8.5, 0.0), "official": (8.5, 0.0)}
TW, TR = 6.0, 0.6                                   # tower side, corner radius
WINGS, ORDER0, ORDER1, CROWN = 15.5, 0.45, 12.45, 38.0
GL0, GL1, GU0, GU1 = 1.0, 4.8, 6.4, 12.2           # ground and upper glazing
FLOOR2 = 0.45 + 5.2 + 3.6                           # the upper glazing's transom: the third floor line (plan standards)
RECESS, ROOM = 0.3, 0.7                             # glass behind the face line, the dark room behind the glass
DOOR_X = -1.5                                       # office-service-door (block x 62.5)
ENTRANCE = (-4.1, 4.1, 12.75)                       # the public entrance's flat wall: x range on the front, top


def wing_path(n=8):
    """The rounded wing outline (closed, CCW, building on the left): each vertex a quadratic round between its two tangent points."""
    out = []
    m = len(WING)
    for i in range(m):
        p, a, b = WING[i], WING[i - 1], WING[(i + 1) % m]
        ti, to = TAN[i]
        da = (p[0] - a[0], p[1] - a[1]); la = math.hypot(*da)
        db = (b[0] - p[0], b[1] - p[1]); lb = math.hypot(*db)
        A = (p[0] - da[0] / la * ti, p[1] - da[1] / la * ti)
        C = (p[0] + db[0] / lb * to, p[1] + db[1] / lb * to)
        for k in range(n + 1):
            s = k / n
            out.append(((1 - s) ** 2 * A[0] + 2 * (1 - s) * s * p[0] + s * s * C[0], (1 - s) ** 2 * A[1] + 2 * (1 - s) * s * p[1] + s * s * C[1]))
    return out


def in_tower(x, y, pad=0.0):
    return any(abs(x - cx) < TW / 2 + pad and abs(y - cy) < TW / 2 + pad for cx, cy in TOWERS.values())


def at_entrance(x, y):
    return y < -10.3 and ENTRANCE[0] - 0.05 < x < ENTRANCE[1] + 0.05


def at_door(x, y):
    return y > 10.3 and abs(x - DOOR_X) < 1.0


def runs(path, keep):
    """Open runs of a closed path where keep(x, y) holds (the closed loop rotated so a run never wraps the seam)."""
    m = len(path)
    k0 = next((i for i in range(m) if not keep(*path[i])), None)
    if k0 is None:
        return None
    loop = path[k0:] + path[:k0]
    return split_runs(loop, keep)


def closed_or_runs(s, path, keep, fn):
    """fn(sub_path, closed) over the parts of a closed path where keep holds."""
    r = runs(path, keep)
    if r is None:
        fn(path, True)
    else:
        for q in r:
            fn(q, False)


def straight_runs():
    """The straight stretches of the wing between its rounds (for the giant order), as (a, b) on the face line."""
    out = []
    m = len(WING)
    for i in range(m):
        p, q = WING[i], WING[(i + 1) % m]
        d = (q[0] - p[0], q[1] - p[1]); L = math.hypot(*d); u = (d[0] / L, d[1] / L)
        a, b = TAN[i][1], L - TAN[(i + 1) % m][0]
        if b - a > 1.0:
            out.append(((p[0] + u[0] * a, p[1] + u[1] * a), (p[0] + u[0] * b, p[1] + u[1] * b)))
    return out


def pilaster_stations():
    """Giant-order pilasters (centre, tangent) along the straight runs, clear of the towers, the public entrance and the service door, about 2.6 m apart."""
    out = []
    for a, b in straight_runs():
        L = math.dist(a, b); u = ((b[0] - a[0]) / L, (b[1] - a[1]) / L)
        segs, cur, st = [], None, 0.05
        k = int(L / st)
        for j in range(k + 1):
            d = L * j / k
            x, y = a[0] + u[0] * d, a[1] + u[1] * d
            ok = not in_tower(x, y, 0.75) and not at_entrance(x, y - 0.0) and not (y > 10.3 and abs(x - DOOR_X) < 1.75)
            if ok and cur is None:
                cur = d
            if not ok and cur is not None:
                segs.append((cur, d)); cur = None
        if cur is not None:
            segs.append((cur, L))
        for d0, d1 in segs:
            if d1 - d0 < 0.9:
                continue
            n = max(1, round((d1 - d0 - 0.7) / 2.6))
            for j in range(n + 1 if d1 - d0 > 1.6 else 1):
                d = d0 + 0.35 + (d1 - d0 - 0.7) * (j / n if n else 0.5) if d1 - d0 > 1.6 else (d0 + d1) / 2
                out.append(((a[0] + u[0] * d, a[1] + u[1] * d), u))
    return out


# ----------------------------------------------------------------- wing

def glazing_run(s, P, z0, z1, transom, lod, closed=False):
    """Glass film RECESS behind the face line over a dark room, chrome mullions about 1.3 m apart, a chrome transom (lod 0); lod 1: glass and room; lod 2: glass only."""
    ribbon(s, offset(P, RECESS, closed), z0, z1, pal.GlassDark, closed)
    if lod < 2:
        ribbon(s, offset(P, RECESS + ROOM, closed), z0, z1, pal.Enamel_Charcoal, closed)
    if lod:
        return
    for p, t in along(P + P[:1] if closed else P, 1.3, margin=0.0 if closed else 0.65):
        with s.at((p.x, p.y, 0), (0, 0, math.degrees(math.atan2(t.y, t.x)))):
            s.box((-0.03, RECESS - 0.06, z0), (0.03, RECESS + 0.01, z1), pal.Chrome)
    if transom:
        tube(s, lift(offset(P, RECESS - 0.03, closed)), sec(-0.03, 0.03, transom - 0.03, transom + 0.03), pal.Chrome, closed=closed)


def wing_far(s):
    """L2: the wing at its face line, the glazing laid on it, the bands, cornice and coping; the entrance wall and the door as blocks."""
    P = wing_path(1)
    s.extrude(ccw(P), 0.0, 14.2, pal.Limestone)
    keep_g = lambda x, y: not at_entrance(x, y) and not in_tower(x, y, 0.05)  # noqa: E731
    for z0, z1 in ((GL0, GL1), (GU0, GU1)):
        closed_or_runs(s, P, keep_g, lambda q, c, z0=z0, z1=z1: ribbon(s, offset(q, -0.02, c), z0, z1, pal.GlassDark, c))
    keep_e = lambda x, y: not at_entrance(x, y)  # noqa: E731
    tube(s, lift(P), sec(-0.08, 0.3, 0.0, ORDER0), pal.Stone_Dark, closed=True)
    closed_or_runs(s, P, keep_e, lambda q, c: tube(s, lift(q), sec(-0.06, 0.02, 5.0, 5.9), pal.Enamel_Teal, closed=c))
    closed_or_runs(s, P, keep_e, lambda q, c: tube(s, lift(q), sec(-0.35, 0.3, ORDER1, ORDER1 + 0.3), pal.Enamel_Ivory, closed=c))
    tube(s, lift(P), sec(0.0, 0.45, 14.2, WINGS - 0.15), pal.Limestone, closed=True)
    tube(s, lift(P), sec(-0.06, 0.02, 13.85, 14.3), pal.Enamel_Seafoam, closed=True)
    tube(s, lift(P), sec(-0.1, 0.5, WINGS - 0.15, WINGS), pal.Enamel_Ivory, closed=True)
    with s.at((DOOR_X, 10.5, 0.0), (0, 0, 180)):
        s.box((-0.55, -0.03, 0.15), (0.55, 0.0, 2.4), pal.Enamel_Teal)
        s.box((-0.95, -0.85, 2.62), (0.95, 0.0, 2.75), pal.Enamel_Ivory)


def wing(s, lod):
    if lod == 2:
        wing_far(s)
        return
    n = (8, 4, 3)[lod]
    P = wing_path(n)
    # the core behind the dark room, to the roof deck
    s.extrude(ccw(offset(P, RECESS + ROOM + 0.02, True)), 0.0, 14.0, pal.Limestone)
    s.extrude(ccw(offset(P, 0.44, True)), 14.0, 14.2, pal.Limestone, bottom=False)                   # the roof deck, closing the rooms under the parapet
    # solid courses at the face line: sill zone, the band zone between the glazings, the head and parapet (broken only where the public entrance's wall stands)
    keep_e = lambda x, y: not at_entrance(x, y)  # noqa: E731
    keep_ed = lambda x, y: not at_entrance(x, y) and not at_door(x, y)  # noqa: E731
    closed_or_runs(s, P, keep_ed, lambda q, c: tube(s, lift(q), sec(-0.08, 0.3, 0.0, ORDER0, 0.02 if lod == 0 else 0.0), pal.Stone_Dark, closed=c))
    closed_or_runs(s, P, keep_ed, lambda q, c: tube(s, lift(q), sec(0.0, RECESS + 0.05, ORDER0, GL0), pal.Limestone, closed=c))
    closed_or_runs(s, P, keep_e, lambda q, c: tube(s, lift(q), sec(0.0, RECESS + 0.05, GL1, GU0), pal.Limestone, closed=c))
    closed_or_runs(s, P, keep_e, lambda q, c: tube(s, lift(q), sec(0.0, RECESS + 0.05, GU1, ENTRANCE[2]), pal.Limestone, closed=c))
    tube(s, lift(P), sec(0.0, 0.45, ENTRANCE[2], WINGS - 0.15), pal.Limestone, closed=True)        # parapet, all round
    # the glazing (not behind the towers, the public entrance or the service door)
    keep_g = lambda x, y: not at_entrance(x, y) and not in_tower(x, y, 0.05)  # noqa: E731
    closed_or_runs(s, P, lambda x, y: keep_g(x, y) and not at_door(x, y), lambda q, c: glazing_run(s, q, GL0, GL1, 3.65, lod, c))
    closed_or_runs(s, P, keep_g, lambda q, c: glazing_run(s, q, GU0, GU1, FLOOR2, lod, c))
    # the deep teal storey band wrapping the rounds (072-G, Sky "yep": DecoKit's old civic office band, DK_Enamel_Teal), its brass hairlines; the ivory cornice over the giant order; the parapet's seafoam band; the coping
    closed_or_runs(s, P, keep_e, lambda q, c: tube(s, lift(q), sec(-0.06, 0.02, 5.0, 5.9, 0.02 if lod == 0 else 0.0), pal.Enamel_Teal, closed=c))
    if lod == 0:
        for z in (4.93, 5.97):
            closed_or_runs(s, P, keep_e, lambda q, c, z=z: hair(s, q, -0.075, z, 0.035, pal.Brass_Civic, closed=c))
    closed_or_runs(s, P, keep_e, lambda q, c: tube(s, lift(q), sec(-0.35, 0.3, ORDER1, ORDER1 + 0.3, 0.05 if lod == 0 else 0.0), pal.Enamel_Ivory, closed=c))
    tube(s, lift(P), sec(-0.06, 0.02, 13.85, 14.3, 0.02 if lod == 0 else 0.0), pal.Enamel_Seafoam, closed=True)
    if lod == 0:
        closed_or_runs(s, P, keep_e, lambda q, c: hair(s, q, -0.36, ORDER1 + 0.15, 0.05, pal.Chrome, closed=c))
        hair(s, P, -0.075, 14.42, 0.035, pal.Brass_Civic, closed=True)
    tube(s, lift(P), sec(-0.1, 0.5, WINGS - 0.15, WINGS, 0.04 if lod == 0 else 0.0), pal.Enamel_Ivory, closed=True)
    entrance_wall(s)
    service_door(s, lod)
    if lod < 2:
        giant_order(s, lod)


def entrance_wall(s):
    """The public entrance's wall: flat stone on the block face (y -40.5) from x 59.9 to 68.1 up to 12.75 m; RecordsEntrance stands on it (its piers let 0.23 m in)."""
    x0, x1, zt = ENTRANCE
    s.box((x0, -10.5, 0.0), (x1, -10.5 + RECESS + ROOM + 0.1, zt), pal.Limestone)
    s.box((x0, -10.58, 0.0), (x1, -10.5, 0.3), pal.Stone_Dark)


def service_door(s, lod):
    """office-service-door: on the east face (y 10.5 in the kit frame) at block x 62.5; the alleys' service door and canopy, turned to face +Y, in a stone surround."""
    xa, xb, y = DOOR_X - 1.0, DOOR_X + 1.0, 10.5
    for x0, x1, z0, z1 in ((xa, DOOR_X - 0.62, 0.0, GL1), (DOOR_X + 0.62, xb, 0.0, GL1), (DOOR_X - 0.62, DOOR_X + 0.62, 2.52, GL1)):
        s.box((x0, y - RECESS - 0.05, z0), (x1, y, z1), pal.Limestone)
    s.box((DOOR_X - 0.62, y - RECESS - 0.05, 0.0), (DOOR_X + 0.62, y - 0.14, 2.52), pal.Enamel_Charcoal)
    s.box((xa, y, 0.0), (xb, y + 0.08, ORDER0), pal.Stone_Dark)
    with s.at((DOOR_X, y, 0.0), (0, 0, 180)):
        if lod == 0:
            door(s, 0.0, light=True)
            canopy(s, 0.0, 2.62)
        else:
            s.box((-0.55, -0.12, 0.15), (0.55, -0.07, 2.4), pal.Enamel_Teal)
            s.box((-0.95, -0.85, 2.62), (0.95, 0.0, 2.75), pal.Enamel_Ivory)


def giant_order(s, lod):
    """Limestone pilasters 0.45..12.45 (12 m, the block's giant order) on the straight runs: granite foot, a seafoam flute up the face, an ivory capital block."""
    for (p, u) in pilaster_stations():
        with s.at((p[0], p[1], 0), (0, 0, math.degrees(math.atan2(u[1], u[0])))):
            s.box((-0.4, -0.3, 0.0), (0.4, 0.3, ORDER0), pal.Stone_Dark)
            if lod == 0:
                s.rbox((-0.35, -0.25, ORDER0), (0.35, 0.3, ORDER1), 0.06, pal.Limestone, seg=2)
                s.box((-0.07, -0.265, 1.2), (0.07, -0.2, 11.6), pal.Enamel_Seafoam)
                for z in (1.12, 11.68):
                    s.box((-0.1, -0.28, z - 0.02), (0.1, -0.24, z + 0.02), pal.Brass_Civic)
            else:
                s.box((-0.35, -0.25, ORDER0), (0.35, 0.3, ORDER1), pal.Limestone)
                s.box((-0.07, -0.26, 1.2), (0.07, -0.24, 11.6), pal.Enamel_Seafoam)


def roof(s, lod):
    """The roof terrace inside the parapet: a planted trough along it (clear of the towers), and the glass rotunda on its drum."""
    P = wing_path(8 if lod == 0 else 4)
    if lod == 0:
        T = offset(P, 1.05, True)
        for q in runs(T, lambda x, y: not in_tower(x, y, 0.45)) or [T]:
            if len(q) > 2:
                with s.at((0, 0, 14.2)):
                    trough(s, q, z_top=0.5, w=0.7)
                    planting(s, q, "A", z0=0.5, step=0.95, w=0.62, rows=1, blooms=2)
    elif lod == 1:
        T = offset(P, 1.05, True)
        for q in runs(T, lambda x, y: not in_tower(x, y, 0.45)) or [T]:
            if len(q) > 2:
                tube(s, lift(q, 14.2), sec(-0.35, 0.35, 0.0, 0.85), pal.Leaf_Civic)
    # the rotunda: limestone drum with a seafoam band, a clerestory ribbon, a glass dome on chrome ribs, a chrome finial
    c, r = (0.0, 1.0), 3.0
    seg = (32, 16, 12)[lod]
    with s.at((c[0], c[1], 14.2)):
        s.lathe([(r + 0.15, 0.0), (r + 0.15, 1.3), (0.0, 1.3)], pal.Limestone, seg=seg, cap_bottom=False)
        s.lathe([(r + 0.18, 0.75), (r + 0.18, 1.05)], pal.Enamel_Seafoam, seg=seg, cap_bottom=False, cap_top=False)
        s.lathe([(r, 1.3), (r, 2.2)], pal.GlassDark, seg=seg, cap_bottom=False, cap_top=False)
        if lod < 2:
            s.lathe([(r - 0.4, 1.3), (r - 0.4, 2.2)], pal.Enamel_Charcoal, seg=seg, cap_bottom=False, cap_top=False)
        s.lathe([(r + 0.2, 2.2), (r + 0.2, 2.42), (0.0, 2.42)], pal.Enamel_Ivory, seg=seg, cap_bottom=False)
        dome = [(r * math.cos(math.pi / 2 * k / 8), 2.42 + 2.4 * math.sin(math.pi / 2 * k / 8)) for k in range(9)]
        s.lathe(dome, pal.GlassDark, seg=seg, cap_bottom=False)
        if lod == 0:
            for k in range(12):
                with s.at((0, 0, 0), (0, 0, 30 * k)):
                    s.sweep([(px + 0.02, 0.0, pz + 0.01) for px, pz in dome[:-1]] + [(0.0, 0.0, 4.84)], 0.04, pal.Chrome, seg=5, caps=False)
            for k in range(16):
                with s.at((0, 0, 0), (0, 0, 22.5 * k)):
                    s.box((r - 0.03, -0.03, 1.3), (r + 0.03, 0.03, 2.2), pal.Chrome)
        s.lathe([(0.25, 4.8), (0.12, 5.0), (0.06, 5.5)], pal.Chrome, seg=10)
        ball(s, (0, 0, 5.7), 0.22, pal.Chrome, seg=12)


# ----------------------------------------------------------------- towers

def tower(s, key, lod):
    cx, cy = TOWERS[key]
    seg = (6, 3, 2)[lod]
    O = rrect(TW, TW, TR, seg, cx, cy)
    out_x = -1 if key == "public" else 1                 # the face away from the wing (block north for public, south for official)
    zt = 33.4
    s.extrude(O, 0.0, zt, pal.Limestone)
    tube(s, lift(O), sec(-0.08, 0.3, 0.0, ORDER0), pal.Stone_Dark, closed=True)
    tube(s, lift(O), sec(-0.06, 0.02, 5.0, 5.9), pal.Enamel_Teal, closed=True)                    # the wing's band runs round the tower
    tube(s, lift(O), sec(-0.35, 0.3, ORDER1, ORDER1 + 0.3), pal.Enamel_Ivory, closed=True)
    if lod == 0:
        for z in (4.93, 5.97):
            hair(s, O, -0.075, z, 0.035, pal.Brass_Civic, closed=True)
    # each face: the glazed stair slot between two seafoam fins (from the band on the outer face, above the wing's roof on the others)
    for nx, ny in ((out_x, 0), (-out_x, 0), (0, 1), (0, -1)):
        z0 = GU0 if (nx, ny) == (out_x, 0) else WINGS + 0.6
        fc = (cx + nx * TW / 2, cy + ny * TW / 2)
        yaw = math.degrees(math.atan2(nx, -ny))               # turns local -Y onto the face's outward normal
        with s.at((fc[0], fc[1], 0), (0, 0, yaw)):
            # local frame: face plane y = 0, outward -Y
            s.box((-0.9, -0.02, z0), (0.9, 0.0, zt - 0.8), pal.GlassDark)
            if lod < 2:
                s.box((-0.9, 0.0, z0), (0.9, 0.02, zt - 0.8), pal.Enamel_Charcoal)
            for xa in (-1.12, 0.9):
                s.box((xa, -0.24, z0 - 0.5), (xa + 0.22, 0.0, zt), pal.Enamel_Seafoam)
            if lod == 0:
                z = z0 + 0.45
                while z < zt - 1.0:
                    s.box((-0.9, -0.06, z - 0.025), (0.9, -0.02, z + 0.025), pal.Chrome)
                    z += 0.9
                s.box((-0.03, -0.07, z0), (0.03, -0.02, zt - 0.8), pal.Chrome)
    # crown: ivory band and brass line, two setbacks with a seafoam ring, the chrome sphere to 38.0
    tube(s, lift(O), sec(-0.15, 0.3, zt, zt + 1.0, 0.04 if lod == 0 else 0.0), pal.Enamel_Ivory, closed=True)
    if lod == 0:
        hair(s, O, -0.17, zt + 0.15, 0.04, pal.Brass_Civic, closed=True)
    s.extrude(O, zt + 1.0, zt + 1.05, pal.Limestone)
    O2 = rrect(4.8, 4.8, 0.5, seg, cx, cy)
    s.extrude(O2, zt + 1.0, zt + 2.2, pal.Limestone)
    tube(s, lift(O2), sec(-0.06, 0.02, zt + 1.45, zt + 1.75), pal.Enamel_Seafoam, closed=True)
    O3 = rrect(3.4, 3.4, 0.45, seg, cx, cy)
    s.extrude(O3, zt + 2.2, zt + 2.8, pal.Enamel_Ivory)
    rs = 1.0
    zc = CROWN - rs
    with s.at((cx, cy, 0)):
        s.lathe([(0.5, zt + 2.8), (0.22, zt + 3.0), (0.16, zc - rs + 0.05)], pal.Chrome, seg=(16, 10, 8)[lod], cap_bottom=False)
    ball(s, (cx, cy, zc), rs, pal.Chrome, seg=(24, 14, 10)[lod])


def volumes(s):
    """The solids a walker and a shot meet (geo.Shape.solid, brief 055; declared, not measured: the 0.25 m cells bun run collide measures a piece this size at
    overshoot its bounds and would round the block's wall out by a cell): the wing as 0.5 m strips of its rounded outline (each the strip's widest x range,
    a superset of the wall by at most a few centimetres), the two towers, the giant order's feet, and the service door's step (a step: stand)."""
    P = wing_path(8)
    m = len(P)
    y = -10.5
    while y < 10.5 - 1e-6:
        y1 = min(y + 0.5, 10.5)
        xs = [px for px, py in P if y - 1e-9 <= py <= y1 + 1e-9]
        for i in range(m):
            (ax, ay), (bx, by) = P[i], P[(i + 1) % m]
            for yy in (y, y1):
                if (ay - yy) * (by - yy) < 0:
                    xs.append(ax + (bx - ax) * (yy - ay) / (by - ay))
        if xs:
            s.solid((min(xs), y, 0.0), (max(xs), y1, WINGS))
        y = y1
    for cx, cy in TOWERS.values():
        s.solid((cx - TW / 2, cy - TW / 2, 0.0), (cx + TW / 2, cy + TW / 2, CROWN))
    for (p, u) in pilaster_stations():
        n = (u[1], -u[0])                                        # outward (right of travel)
        corners = [(p[0] + u[0] * a + n[0] * b, p[1] + u[1] * a + n[1] * b) for a in (-0.4, 0.4) for b in (0.0, 0.3)]
        s.solid((min(c[0] for c in corners), min(c[1] for c in corners), 0.0), (max(c[0] for c in corners), max(c[1] for c in corners), ORDER1))
    s.solid((DOOR_X - 0.85, 10.5, 0.0), (DOOR_X + 0.85, 10.94, 0.15), stand=True)


def records_tower(lod):
    def build(s):
        wing(s, lod)
        roof(s, lod)
        for k in TOWERS:
            tower(s, k, lod)
        if lod == 0:
            volumes(s)
        remap(s, {"DK_Window_Tower": "GlassDark"})  # the service door's vision light: the piece's own glass, one material fewer
    return build


asset("RecordsTower", "Landmarks", bevel=0.0, near=records_tower(1), far=records_tower(2))(records_tower(0))
