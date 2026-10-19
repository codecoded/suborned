"""The 9 Oct "Caprice Grand Café" look, rebuilt as DecoKit pieces (brief 020 B, Vela). Two pieces share CafeBay's frame (pivot = centre of the café's flat front on
the ring line, block (-10.7, -29), yaw 90; front -Y = block +x; the building on +Y; z 0 = the pavement top):

  CafeBay_v2    the ground-storey front of CafeBay with clear glazing instead of the dark film over a charcoal sheet: clear panes in thin chrome mullions with brass
                transom and head lines, reflection streaks on the glass, glass doors, cream piers, a seafoam belt under the generator's upper mass (z 4.7), the coral and
                ivory striped awning with a scalloped valance in place of the flat canopy, the blade sign. Replaces CafeBay (same place, same pivot, same footprint).
  CafeInterior  what is behind it: the room's shell (limestone floor, plaster ceiling and cove, the back wall with the back-bar painting and the botanical mural, the
                two side walls with a window each, outside faces in cream stone with the seafoam base course and belt), a window row of tables and chairs, a second row,
                the counter with stools, two petrol-velvet booths, opal pendants on chrome rods, potted plants. Companion of CafeBay_v2 (same pivot and yaw): the
                ground-storey mass is drawn by the piece only, so until now the sides and back of the café were open air behind the front.

Exteriors only: this is set dressing seen through glass, never entered. The whole room is drawn "lit" (emit materials with a low strength, DK_Cafe_*), so it reads
through the glass without a light inside; Glint may swap them for real lights. Fits the Oct 9 shell (civic-block.json: 22 x 11.3 m, rule C-0 unchanged): the room is
x -11..11, y 0..11.3, floor 0.04, ceiling 4.6, under the generator's mass that starts at 4.7. CafeTerrace (the wall 6.4 m out) is unchanged."""

import math

from mathutils import Vector

import mats
from civic_cafe import DOOR_H, DOOR_X, GLASS_A, Z_G0, Z_G1, stacked_sign
from civic_common import HF, R, band, front_path, hair, split_runs
from kit import asset
from mats import pal, register
from plaza_parts import lift, offset, ribbon
from structures import along, ccw, sec, tube

# ----------------------------------------------------------------------------- materials
# The lit room: `emit` kind with a low strength, so the interior reads through glass with no light inside (URP emission, not shadowed by the canopy and the ceiling).
# Hexes are the 9 Oct café's own materials (Data/Civic/Materials/Cafe_*.mat), lifted a little where a dark room would lose them.
register("DK_Glass_Clear", "clear", "#B3D9DE")                  # Cafe_clear_glazing: base (0.70, 0.85, 0.87), alpha 0.12, smoothness 0.94
register("DK_Glass_Sheen", "sheen", "#F4FBFA")                  # reflection streaks
register("DK_Cafe_Plaster", "emit", "#E1CBB1", 0.55)            # warm ivory plaster
register("DK_Cafe_Velvet", "emit", "#2B6567", 0.6)              # petrol velvet booths (old #224C4F, lifted)
register("DK_Cafe_PetrolEnamel", "emit", "#2E7C80", 0.55)       # counter front (old #266B70)
register("DK_Cafe_Walnut", "emit", "#7A5640", 0.45)             # walnut cabinetry (old #584036)
register("DK_Cafe_Ivory", "emit", "#EFE8D6", 0.5)               # table tops, chair shells
register("DK_Cafe_Coral", "emit", "#FC9C84", 0.55)              # chair cushions, stool seats
register("DK_Cafe_Seafoam", "emit", "#72B5A7", 0.5)             # chair shells (second colour)
register("DK_Cafe_Opal", "emit", "#FFE6AA", 5.0)                # pendant globes (Cafe_warm_pendant_emitter)
register("DK_Cafe_Cove", "emit", "#FFB866", 3.2)                # amber cove light (Cafe_amber_cove_emitter)
register("DK_Cafe_Bakelite", "emit", "#2A2B2E", 0.25)           # the register
register("DK_Cafe_Leaf", "emit", "#4E8A52", 0.45)               # potted plants
register("DK_Cafe_Floor", "emit", "#FFFFFF", 0.55)              # ivory / seafoam limestone checker, 1.2 m tile of 0.6 m cells (cafe_floor_v1.png)
register("DK_Cafe_BackBar", "emit", "#FFFFFF", 0.85)            # the back-bar painting (cafe_back_bar_v1.png)
register("DK_Cafe_Mural", "emit", "#FFFFFF", 0.7)               # the botanical mural (cafe_mural_v1.png)
mats.TEXTURES.setdefault("DK_Cafe_Floor", ("cafe_floor_v1.png", 1.2))
mats.TEXTURES.setdefault("DK_Cafe_BackBar", ("cafe_back_bar_v1.png", 1.0))
mats.TEXTURES.setdefault("DK_Cafe_Mural", ("cafe_mural_v1.png", 1.0))

# ----------------------------------------------------------------------------- frame
Z_FLOOR, Z_CEIL, Z_UPPER = 0.04, 4.6, 4.7  # floor top, ceiling underside, where the generator's mass starts
Y_BACK_IN, Y_BACK = 10.9, 11.3             # back wall: inner face, outer face (the Oct 9 depth)
X_SIDE_IN, X_SIDE = 10.6, 11.0             # side walls: inner face, outer face (the arcs end tangent to them at y 5.3)
Y_ARC_END = R                              # the front arcs meet the side walls here
BELT0 = 4.40                               # the seafoam belt: 4.40 to 4.70
CANOPY_V2 = 1.7                            # awning projection (the 008 canopy reached 2.45 m and hid the room from the play camera)
Z_AW0, Z_AW1, DROP = 4.36, 4.02, 0.24      # awning: top at the wall, at the edge, valance drop
WIN_Y0, WIN_Y1, WIN_Z0, WIN_Z1 = 6.5, 10.1, 0.9, 3.5  # side window openings (both sides)


def _sampler(path):
    P = [Vector((x, y, 0)) for x, y in path]
    L = [0.0]
    for a, b in zip(P, P[1:]):
        L.append(L[-1] + (b - a).length)

    def at(d):
        d = min(max(d, 0.0), L[-1])
        i = min(next((i for i in range(len(L) - 1) if L[i + 1] >= d - 1e-9), len(L) - 2), len(L) - 2)
        f = (d - L[i]) / max(L[i + 1] - L[i], 1e-9)
        return P[i].lerp(P[i + 1], f), (P[i + 1] - P[i]).normalized()
    return at, L[-1]


def _resample(path, step):
    at, tot = _sampler(path)
    k = max(1, math.ceil(tot / step))
    return [(at(tot * i / k)[0].x, at(tot * i / k)[0].y) for i in range(k + 1)]


def _out(t):
    """Outward (right of travel) unit normal of the front path at tangent t."""
    return Vector((t.y, -t.x))


def _toward(s, pts, mat, hint):
    """One polygon whose normal points along `hint`."""
    P = [Vector(p) for p in pts]
    n = Vector((0, 0, 0))
    for a, b in zip(P, P[1:] + P[:1]):
        n += Vector(((a.y - b.y) * (a.z + b.z), (a.z - b.z) * (a.x + b.x), (a.x - b.x) * (a.y + b.y)))
    s.face([tuple(p) for p in (P if n.dot(Vector(hint)) >= 0 else P[::-1])], mat)


def _facing(dx, dy):
    """Yaw (deg) that turns a chair's front (-Y) to the direction (dx, dy)."""
    return math.degrees(math.atan2(dx, -dy))


# ----------------------------------------------------------------------------- CafeBay_v2

def door_leaf(s, x0, x1, y):
    """A glass leaf in a thin brass frame with a chrome push bar (the room behind it reads through)."""
    s.face([(x0 + 0.05, y, 0.3), (x1 - 0.05, y, 0.3), (x1 - 0.05, y, DOOR_H - 0.05), (x0 + 0.05, y, DOOR_H - 0.05)], pal.Glass_Clear)
    for xa, xb in ((x0, x0 + 0.06), (x1 - 0.06, x1)):
        s.box((xa, y - 0.05, 0.0), (xb, y + 0.03, DOOR_H), pal.Brass_Civic)
    for za, zb in ((DOOR_H - 0.06, DOOR_H), (0.28, 0.34)):
        s.box((x0, y - 0.05, za), (x1, y + 0.03, zb), pal.Brass_Civic)
    s.box((x0 + 0.06, y - 0.055, 0.0), (x1 - 0.06, y + 0.03, 0.28), pal.Brass_Civic)  # kick plate
    hx = x1 - 0.15 if x0 < 0 else x0 + 0.15
    s.sweep([(hx, y - 0.1, 0.95), (hx, y - 0.1, 1.5)], 0.02, pal.Chrome, seg=5)
    for z in (0.95, 1.5):
        s.sweep([(hx, y - 0.03, z), (hx, y - 0.1, z)], 0.015, pal.Chrome, seg=4, caps=False)
    # the leaf's reflection: one streak
    ox = 0.3 if x0 < 0 else 0.25
    _toward(s, [(x0 + ox, y - 0.004, 0.45), (x0 + ox + 0.09, y - 0.004, 0.45), (x0 + ox + 0.39, y - 0.004, DOOR_H - 0.12), (x0 + ox + 0.3, y - 0.004, DOOR_H - 0.12)], pal.Glass_Sheen, (0, -1, 0))


def streaks(s, P, d0, d1, z0, z1):
    """Two reflection streaks across the pane that runs from arc length d0 to d1 along the path (diagonal, as a window throws the street)."""
    at, _ = _sampler(P)
    w = d1 - d0
    for a, wd in ((0.2, 0.16), (0.46, 0.06)):
        da = d0 + w * a
        slant = min(0.5, w * 0.35)
        pts = []
        for d, z in ((da, z0), (da + wd, z0), (da + wd + slant, z1), (da + slant, z1)):
            p, t = at(d)
            q = Vector((p.x, p.y)) + _out(t) * (-GLASS_A + 0.004)
            pts.append((q.x, q.y, z))
        o = _out(at((d0 + d1) / 2)[1])
        _toward(s, pts, pal.Glass_Sheen, (o.x, o.y, 0))


def awning(s, P):
    """Coral and ivory stripes (0.46 m wide) sloping from the belt to a chrome edge rail, a scalloped valance, the same cloth under."""
    pts = _resample(P, 0.46)
    inner, outer = offset(pts, -0.05), offset(pts, -CANOPY_V2)
    for i in range(len(pts) - 1):
        mat = pal.Cloth_CoralSoft if i % 2 == 0 else pal.Cloth_Cream
        a0, a1, b0, b1 = inner[i], inner[i + 1], outer[i], outer[i + 1]
        top = [(*a0, Z_AW0), (*a1, Z_AW0), (*b1, Z_AW1), (*b0, Z_AW1)]
        _toward(s, top, mat, (0, 0, 1))
        _toward(s, [(x, y, z - 0.025) for x, y, z in top], mat, (0, 0, -1))
        hem = []
        for k in range(5):  # scallop, bottom edge from b1 back to b0
            f = k / 4
            hem.append((b1[0] + (b0[0] - b1[0]) * f, b1[1] + (b0[1] - b1[1]) * f, Z_AW1 - DROP + 0.075 * (1 - math.sin(math.pi * f))))
        outn = Vector(((b1[1] - b0[1]), -(b1[0] - b0[0]), 0))
        _toward(s, [(*b0, Z_AW1), (*b1, Z_AW1)] + hem, mat, outn)
        _toward(s, [(*b0, Z_AW1 - 0.02), (*b1, Z_AW1 - 0.02)] + [(x, y, z - 0.02) for x, y, z in hem], mat, -outn)
    s.sweep([(x, y, Z_AW1 + 0.012) for x, y in outer], 0.017, pal.Enamel_Seafoam, seg=5, caps=False)  # edge rail (seafoam: chrome mirrored the dark sky as a black line)
    for p, t in along(P, 2.6):  # gallows arms: wall to edge rail, a chrome strut under the cloth
        n = _out(t)
        a = p + n * 0.05
        b = p + n * (CANOPY_V2 - 0.03)
        s.sweep([(a.x, a.y, Z_AW0 - 0.3), (b.x, b.y, Z_AW1 - 0.03)], 0.013, pal.Chrome, seg=4)


@asset("CafeBay_v2", "Plaza", bevel=0.0)
def cafe_bay_v2(s):
    """The café's front with clear glass (brief 020): replaces CafeBay. Same pivot, footprint and frame; the generator's upper mass starts at z 4.7."""
    P = front_path(6, 4)
    for run in split_runs(P, lambda x, y: abs(x) > DOOR_X - 0.05):  # base course: seafoam faience, limestone coping, broken at the doors
        band(s, run, -0.3, 0.15, 0.0, 0.45, pal.Enamel_Seafoam)
        band(s, run, -0.34, 0.15, 0.4, 0.46, pal.Limestone)
    # clear glazing, the room behind it; brass hairlines at sill, transom and head only
    ribbon(s, offset(P, GLASS_A), Z_G0, Z_G1, pal.Glass_Clear)
    at, tot = _sampler(P)
    n = max(2, round(tot / 1.45))
    stations = [tot * i / n for i in range(n + 1)]
    for i, d in enumerate(stations):
        p, t = at(d)
        if abs(p.x) < DOOR_X + 0.35 and p.y < 0.5:
            continue
        with s.at((p.x, p.y, 0), (0, 0, math.degrees(math.atan2(t.y, t.x)))):
            s.box((-0.03, GLASS_A - 0.07, Z_G0), (0.03, GLASS_A + 0.02, Z_G1), pal.Chrome)
    for d0, d1 in zip(stations, stations[1:]):
        pm = at((d0 + d1) / 2)[0]
        if abs(pm.x) < DOOR_X + 0.3 and pm.y < 0.5:
            continue
        streaks(s, P, d0, d1, Z_G0 + 0.12, 2.55 - 0.06)
        streaks(s, P, d0 + (d1 - d0) * 0.15, d1, 2.55 + 0.06, Z_G1 - 0.1)
    for z in (2.55, Z_G1 - 0.03):
        hair(s, P, GLASS_A - 0.06, z, 0.05, pal.Brass_Civic)
    for run in split_runs(P, lambda x, y: abs(x) > DOOR_X - 0.05):
        hair(s, run, GLASS_A - 0.06, Z_G0 + 0.03, 0.05, pal.Brass_Civic)
    # cream piers at the flat's ends, the door jambs and where the arcs meet the side walls
    for x in (-HF, HF, -DOOR_X - 0.3, DOOR_X + 0.3):
        s.rbox((x - 0.28, -0.4, 0.0), (x + 0.28, 0.16, Z_G1 + 0.2), 0.04, pal.Limestone)
        s.box((x - 0.04, -0.43, 0.46), (x + 0.04, -0.4, Z_G1), pal.Brass_Civic)
    for sx in (-1, 1):
        s.rbox((sx * (HF + R) - 0.3 + 0.1, Y_ARC_END - 0.5, 0.0), (sx * (HF + R) + 0.3 + 0.1, Y_ARC_END + 0.1, Z_G1 + 0.2), 0.04, pal.Limestone)
    # header and belt: limestone head from the glass to the belt, then the seafoam belt course under the upper storeys (z 4.40 to 4.70) with brass lines
    band(s, P, -0.2, 0.15, Z_G1, BELT0, pal.Limestone)
    band(s, P, -0.26, 0.1, BELT0, Z_UPPER, pal.Enamel_Seafoam)
    hair(s, P, -0.28, BELT0 + 0.02, 0.035, pal.Brass_Civic)
    hair(s, P, -0.28, Z_UPPER - 0.05, 0.035, pal.Brass_Civic)
    # doors: two glass leaves, flush entrance (no steps)
    s.box((-DOOR_X, GLASS_A - 0.05, DOOR_H), (DOOR_X, GLASS_A + 0.02, DOOR_H + 0.05), pal.Brass_Civic)  # transom line: the lite above the leaves is the glazing
    door_leaf(s, -DOOR_X, 0.0, GLASS_A - 0.02)
    door_leaf(s, 0.0, DOOR_X, GLASS_A - 0.02)
    awning(s, P)
    # blade sign (as CafeBay): on the wall at x 3.6, its foot on the belt
    s.rbox((3.6 - 0.2, -1.2, BELT0), (3.6 + 0.2, 0.0, BELT0 + 0.12), 0.02, pal.Chrome, seg=1)
    stacked_sign(s, "CAFE", 0.52, 3.6, -0.6, BELT0 + 0.16, pal.Enamel_Teal, pal.Enamel_Ivory, pal.Chrome)


# ----------------------------------------------------------------------------- CafeInterior

def chair(s, shell, cushion):
    """Bistro chair, seat at 0.47 m, back to 0.84 m; the seat centre over the origin, front toward -Y (about 110 tris: it is seen through glass at 40 px)."""
    s.lathe([(0.0, 0.44), (0.19, 0.44), (0.2, 0.49), (0.0, 0.495)], cushion, seg=8, cap_bottom=False)
    for sx, sy in ((-1, -1), (1, -1), (-1, 1), (1, 1)):
        s.sweep([(sx * 0.14, sy * 0.13, 0.44), (sx * 0.19, sy * 0.19, 0.0)], 0.011, pal.Chrome, seg=4, caps=False)
    for sx in (-1, 1):
        s.sweep([(sx * 0.15, 0.17, 0.46), (sx * 0.16, 0.2, 0.84)], 0.011, pal.Chrome, seg=4, caps=False)
    s.box((-0.17, 0.185, 0.6), (0.17, 0.215, 0.84), shell)


def table(s, r=0.36, top=None):
    """Round café table, 0.74 m: walnut top, a chrome column and foot (about 90 tris)."""
    ring = [(r * math.cos(2 * math.pi * k / 10), r * math.sin(2 * math.pi * k / 10)) for k in range(10)]
    s.extrude(ring, 0.705, 0.74, top or pal.Cafe_Walnut)
    s.sweep([(0, 0, 0.03), (0, 0, 0.705)], 0.025, pal.Chrome, seg=4, caps=False)
    s.extrude([(0.2 * math.cos(2 * math.pi * k / 8), 0.2 * math.sin(2 * math.pi * k / 8)) for k in range(8)], 0.0, 0.03, pal.Chrome)


def pendant(s, x, y, z=3.35):
    """Opal globe on a chrome rod to the ceiling (about 60 tris)."""
    s.sweep([(x, y, z + 0.17), (x, y, Z_CEIL)], 0.011, pal.Chrome, seg=4, caps=False)
    with s.at((x, y, 0)):
        s.lathe([(0.0, z - 0.17), (0.12, z - 0.12), (0.17, z), (0.12, z + 0.12), (0.0, z + 0.17)], pal.Cafe_Opal, seg=9, cap_bottom=False, cap_top=False)


def bench(s, length, back_mat, seat_mat):
    """Booth bench, long axis Y, seat 0.46 m, back to 1.3 m, front toward +X, seat depth 0.58 (petrol velvet, with seams), about 100 tris."""
    s.box((0.0, -length / 2, 0.0), (0.58, length / 2, 0.34), pal.Cafe_Walnut)
    s.box((0.04, -length / 2 + 0.03, 0.34), (0.58, length / 2 - 0.03, 0.5), seat_mat)
    s.box((0.0, -length / 2, 0.5), (0.16, length / 2, 1.3), back_mat)
    for k in range(1, 4):
        yy = -length / 2 + length * k / 4
        s.box((0.158, yy - 0.008, 0.56), (0.166, yy + 0.008, 1.24), pal.Cafe_Walnut)
    s.box((0.0, -length / 2, 1.3), (0.17, length / 2, 1.33), pal.Brass_Civic)  # brass cap line


def plant(s, x, y, h=0.9):
    """Potted plant: seafoam pot, three leaf cushions (about 100 tris)."""
    with s.at((x, y, 0)):
        s.lathe([(0.0, 0.0), (0.2, 0.0), (0.26, 0.4), (0.0, 0.4)], pal.Cafe_Seafoam, seg=8, cap_bottom=False)
        for dx, dy, r, hh in ((0, 0, 0.3, h), (0.12, 0.08, 0.22, h * 0.8), (-0.1, 0.1, 0.2, h * 0.7)):
            with s.at((dx, dy, 0.38)):
                s.lathe([(0.0, 0.0), (r, 0.0), (r * 0.7, hh * 0.6), (0.0, hh)], pal.Cafe_Leaf, seg=7, cap_bottom=False)


def side_wall(s, sx):
    """One side wall (sx = -1 west / +1 east): cream stone outside with the seafoam base course and belt, plaster inside, a window of clear glass in chrome mullions."""
    xi = sx * X_SIDE_IN
    lo, hi = (min(xi, sx * X_SIDE), max(xi, sx * X_SIDE))
    for y0, y1, z0, z1 in ((Y_ARC_END, WIN_Y0, 0.0, Z_UPPER), (WIN_Y1, Y_BACK, 0.0, Z_UPPER), (WIN_Y0, WIN_Y1, 0.0, WIN_Z0), (WIN_Y0, WIN_Y1, WIN_Z1, Z_UPPER)):
        s.box((lo, y0, z0), (hi, y1, z1), pal.Limestone)
    # outside: seafoam base course with a coping line, belt, brass lines
    o = sx * X_SIDE
    a, b = (o, o + sx * 0.06) if sx > 0 else (o - 0.06, o)
    s.box((a, Y_ARC_END, 0.0), (b, Y_BACK, 0.45), pal.Enamel_Seafoam)
    s.box((min(a, o + sx * 0.08), Y_ARC_END, 0.4), (max(b, o + sx * 0.08), Y_BACK, 0.46), pal.Limestone)
    a2, b2 = (o, o + sx * 0.26) if sx > 0 else (o - 0.26, o)
    s.box((a2, Y_ARC_END, BELT0), (b2, Y_BACK + 0.26, Z_UPPER), pal.Enamel_Seafoam)
    # inside: plaster, a petrol wainscot to 1.0 with a brass rail
    for y0, y1, z0, z1 in ((Y_ARC_END, WIN_Y0, Z_FLOOR, Z_CEIL), (WIN_Y1, Y_BACK_IN, Z_FLOOR, Z_CEIL), (WIN_Y0, WIN_Y1, Z_FLOOR, WIN_Z0), (WIN_Y0, WIN_Y1, WIN_Z1, Z_CEIL)):
        s.box((xi - 0.02 if sx > 0 else xi, y0, z0), (xi if sx > 0 else xi + 0.02, y1, z1), pal.Cafe_Plaster)
    j0, j1 = (xi - 0.045, xi - 0.02) if sx > 0 else (xi + 0.02, xi + 0.045)
    s.box((j0, Y_ARC_END, Z_FLOOR), (j1, Y_BACK_IN, 1.0), pal.Cafe_PetrolEnamel)
    s.box((j0 - 0.01 if sx > 0 else j0, Y_ARC_END, 1.0), (j1 if sx > 0 else j1 + 0.01, Y_BACK_IN, 1.03), pal.Brass_Civic)
    # window: clear glass in a thin chrome frame, streaks
    gx = sx * (X_SIDE + 0.02)
    ys = [WIN_Y0 + (WIN_Y1 - WIN_Y0) * i / 3 for i in range(4)]
    hint = (sx, 0, 0)
    _toward(s, [(gx, WIN_Y0, WIN_Z0), (gx, WIN_Y1, WIN_Z0), (gx, WIN_Y1, WIN_Z1), (gx, WIN_Y0, WIN_Z1)], pal.Glass_Clear, hint)
    for yy in ys:
        s.box((gx - 0.03 if sx > 0 else gx - 0.03, yy - 0.03, WIN_Z0), (gx + 0.03, yy + 0.03, WIN_Z1), pal.Chrome)
    for z in (WIN_Z0, WIN_Z1 - 0.03):
        s.box((gx - 0.04, WIN_Y0, z), (gx + 0.04, WIN_Y1, z + 0.03), pal.Brass_Civic)
    for k in range(3):
        y0 = ys[k] + 0.2
        _toward(s, [(gx + sx * 0.004, y0, WIN_Z0 + 0.1), (gx + sx * 0.004, y0 + 0.14, WIN_Z0 + 0.1), (gx + sx * 0.004, y0 + 0.14 + 0.5, WIN_Z1 - 0.1), (gx + sx * 0.004, y0 + 0.5, WIN_Z1 - 0.1)], pal.Glass_Sheen, hint)


def back_wall(s):
    """The back wall: cream stone outside with the seafoam base and the kitchen door (block (-22.4, -26): x 3), inside the back-bar painting and the botanical murals."""
    s.box((-X_SIDE, Y_BACK_IN, 0.0), (X_SIDE, Y_BACK, Z_UPPER), pal.Limestone)
    s.box((-X_SIDE, Y_BACK, 0.0), (X_SIDE, Y_BACK + 0.06, 0.45), pal.Enamel_Seafoam)
    s.box((-X_SIDE, Y_BACK, 0.4), (X_SIDE, Y_BACK + 0.08, 0.46), pal.Limestone)
    s.box((-X_SIDE, Y_BACK, BELT0), (X_SIDE, Y_BACK + 0.26, Z_UPPER), pal.Enamel_Seafoam)
    s.box((-X_SIDE, Y_BACK + 0.26, BELT0 + 0.02), (X_SIDE, Y_BACK + 0.29, BELT0 + 0.055), pal.Brass_Civic)
    # kitchen door: a seafoam leaf in a limestone surround, a chrome bar, a small canopy light
    s.box((2.35, Y_BACK, 0.0), (3.65, Y_BACK + 0.1, 2.4), pal.Limestone)
    s.box((2.5, Y_BACK + 0.1, 0.0), (3.5, Y_BACK + 0.14, 2.25), pal.Enamel_Seafoam)
    s.sweep([(3.3, Y_BACK + 0.2, 0.9), (3.3, Y_BACK + 0.2, 1.4)], 0.015, pal.Chrome, seg=4)
    s.box((2.35, Y_BACK + 0.1, 2.4), (3.65, Y_BACK + 0.45, 2.46), pal.Limestone)
    # inside: art on flat quads, never on the curved glass
    Y = Y_BACK_IN - 0.003
    top = 4.2
    for x0, x1, m in ((-X_SIDE_IN, -3.7, pal.Cafe_Mural), (-3.7, 3.7, pal.Cafe_BackBar), (3.7, X_SIDE_IN, pal.Cafe_Mural)):
        s.decal([(x0, Y, Z_FLOOR), (x1, Y, Z_FLOOR), (x1, Y, top), (x0, Y, top)], m)
    s.box((-X_SIDE_IN, Y_BACK_IN - 0.02, top), (X_SIDE_IN, Y_BACK_IN, Z_CEIL), pal.Cafe_Plaster)
    s.box((-X_SIDE_IN, Y_BACK_IN - 0.05, top - 0.015), (X_SIDE_IN, Y_BACK_IN - 0.01, top + 0.015), pal.Brass_Civic)


def counter(s, cx=0.0, cy=8.35):
    """The counter: a straight petrol-enamel run with a fluted front, limestone top in a chrome edge, a brass footrail, a register, stools."""
    L, D = 6.4, 0.75
    x0, x1, y0, y1 = cx - L / 2, cx + L / 2, cy - D / 2, cy + D / 2
    s.box((x0, y0 + 0.04, Z_FLOOR), (x1, y1, 1.0), pal.Cafe_Walnut)
    s.box((x0, y0, Z_FLOOR + 0.1), (x1, y0 + 0.04, 0.98), pal.Cafe_PetrolEnamel)
    k = int(L / 0.32)
    for i in range(k + 1):  # fluting: thin vertical ribs on the front
        xx = x0 + L * i / k
        s.box((xx - 0.03, y0 - 0.02, Z_FLOOR + 0.12), (xx + 0.03, y0, 0.96), pal.Cafe_PetrolEnamel)
    s.box((x0 - 0.05, y0 - 0.06, 1.0), (x1 + 0.05, y1 + 0.04, 1.05), pal.Limestone)
    s.box((x0 - 0.05, y0 - 0.075, 1.0), (x1 + 0.05, y0 - 0.06, 1.055), pal.Chrome)
    s.sweep([(x0 + 0.1, y0 - 0.2, 0.18), (x1 - 0.1, y0 - 0.2, 0.18)], 0.016, pal.Brass_Civic, seg=4)
    for xx in (x0 + 0.3, cx, x1 - 0.3):
        s.sweep([(xx, y0 - 0.2, 0.0), (xx, y0 - 0.2, 0.18)], 0.012, pal.Brass_Civic, seg=4, caps=False)
    s.box((cx - 1.9, cy - 0.1, 1.05), (cx - 1.5, cy + 0.15, 1.3), pal.Cafe_Bakelite)  # register
    s.box((cx - 1.85, cy - 0.06, 1.3), (cx - 1.55, cy + 0.1, 1.36), pal.Chrome)
    s.box((cx + 0.9, cy - 0.15, 1.05), (cx + 1.7, cy + 0.2, 1.42), pal.Chrome)     # the machine
    s.box((cx + 0.95, cy - 0.12, 1.42), (cx + 1.65, cy + 0.16, 1.5), pal.Cafe_Ivory)
    for k2 in range(6):  # stools: chrome post, coral seat
        sx_ = cx - 2.5 + k2 * 1.0
        sy_ = y0 - 0.55
        s.sweep([(sx_, sy_, 0.02), (sx_, sy_, 0.64)], 0.02, pal.Chrome, seg=4, caps=False)
        with s.at((sx_, sy_, 0)):
            s.lathe([(0.0, 0.0), (0.17, 0.0), (0.17, 0.014), (0.0, 0.02)], pal.Chrome, seg=8, cap_bottom=False)
            s.lathe([(0.0, 0.62), (0.18, 0.62), (0.19, 0.7), (0.0, 0.7)], pal.Cafe_Coral, seg=8, cap_bottom=False)


def booth(s, cx, cy):
    """A pair of facing booth benches with a table between (petrol velvet), long axis Y; benches 2.0 m long, 1.5 m apart face to face."""
    with s.at((cx - 0.75 - 0.58, cy, Z_FLOOR)):
        bench(s, 2.0, pal.Cafe_Velvet, pal.Cafe_Velvet)
    with s.at((cx + 0.75 + 0.58, cy, Z_FLOOR), (0, 0, 180)):
        bench(s, 2.0, pal.Cafe_Velvet, pal.Cafe_Velvet)
    with s.at((cx, cy, Z_FLOOR)):
        s.box((-0.4, -0.55, 0.7), (0.4, 0.55, 0.74), pal.Cafe_Ivory)
        s.sweep([(0, 0, 0.0), (0, 0, 0.7)], 0.03, pal.Chrome, seg=4, caps=False)
        s.box((-0.2, -0.2, 0.0), (0.2, 0.2, 0.02), pal.Chrome)


@asset("CafeInterior", "Plaza", bevel=0.0)
def cafe_interior(s):
    """The lit room behind CafeBay_v2's glass and the walls around it (brief 020): companion of CafeBay_v2, same pivot and yaw."""
    P = front_path(6, 4)
    # floor: the ring line to the walls; the limestone checker (DK_Cafe_Floor), 4 cm over the pavement so the door stays flush
    poly = ccw([(x, y) for x, y in P] + [(X_SIDE - 0.2, Y_BACK_IN), (-X_SIDE + 0.2, Y_BACK_IN)])
    s.extrude(poly, 0.0, Z_FLOOR, pal.Cafe_Floor)
    # ceiling and the amber cove along the glass
    s.extrude(poly, Z_CEIL, Z_CEIL + 0.06, pal.Cafe_Plaster)
    tube(s, lift(offset(P, 0.45), Z_CEIL - 0.05), sec(-0.04, 0.04, 0.0, 0.05), pal.Cafe_Cove)
    tube(s, lift(offset(P, 0.45), Z_CEIL - 0.07), sec(-0.07, 0.07, 0.0, 0.02), pal.Brass_Civic)
    side_wall(s, -1)
    side_wall(s, 1)
    back_wall(s)
    # window row: tables 1.3 m inside the glass, two chairs each, clear of the door aisle (x within 2.0)
    shells = (pal.Cafe_Ivory, pal.Cafe_Seafoam, pal.Cafe_Ivory)
    cushions = (pal.Cafe_Coral, pal.Cafe_Coral, pal.Cafe_Seafoam)
    row = [(p, t) for p, t in along(offset(P, 1.35), 2.7, margin=0.5) if not (abs(p.x) < 2.2 and p.y < 2.5)]
    pend = []
    for i, (p, t) in enumerate(row):
        nrm = _out(t)
        with s.at((p.x, p.y, Z_FLOOR)):
            table(s)
        for sgn, dx in ((1, 0.62), (-1, 0.62)):
            q = p + nrm * (sgn * dx)
            with s.at((q.x, q.y, Z_FLOOR), (0, 0, _facing(-nrm.x * sgn, -nrm.y * sgn))):
                chair(s, shells[(i + (sgn > 0)) % 3], cushions[(i + (sgn > 0)) % 3])
        if i % 2 == 0:
            pend.append((p.x, p.y))
    # second row and two four-tops, further in
    for i, (tx, ty) in enumerate(((-7.3, 4.9), (-3.6, 4.6), (3.6, 4.6), (7.3, 4.9))):
        with s.at((tx, ty, Z_FLOOR)):
            table(s)
        for sgn in (1, -1):
            with s.at((tx, ty + sgn * 0.62, Z_FLOOR), (0, 0, _facing(0, -sgn))):
                chair(s, shells[(i + sgn) % 3], cushions[(i + sgn) % 3])
        if i in (1, 2):
            pend.append((tx, ty))
    # counter, two booths
    counter(s)
    for xx in (-3.0, 0.0, 3.0):
        pend.append((xx, 8.35))
    booth(s, -7.6, 8.6)
    booth(s, 7.6, 8.6)
    pend += [(-7.6, 8.6), (7.6, 8.6)]
    for x, y in pend:
        pendant(s, x, y)
    for x, y in ((-2.0, 6.6), (2.0, 6.6), (-10.0, 5.9), (10.0, 5.9)):
        plant(s, x, y)
