"""Deco Kit skyline family (Concepts/Streets/skyline/canyon.png): streamline mid-rise frontage and Art Deco
towers that carry the street up to the skyline.

Frontage follows the hall convention (plaza.py): face -Y, pivot = footprint front-centre at z = 0 (placed at the walk
surface), facade plane y = 0, depth +Y; 6 m bays tile along X and their ribbons run on across bay joints.
  StreamBay_6 / _9   6 x 14 m: black-granite plinth, shopfront, teal fascia, ivory canopy (1.6 m out, 3.85 m up),
                     then 6 / 9 floors (3.4 m) of ribbon windows under ivory eyebrows, parapet.
  StreamBay_9W       the same in white render.
  DecoBay_10         6 x 14 m: the shopfront under 10 floors of limestone piers and bronze spandrels, stepped crown.
  StreamEnd_6 / _9 / _9W  a run's last bay: quarter-round R 3 on -X, ribbons wrapping the 14 m side (mirror X for +X).
  StreamCorner_9     HallCornerWing's 8 x 8 m cell and plan (quarter-round R 6 about (2, 6)): shopfront, ribbons and
                     eyebrows wrap the round; an ivory fin rises over the corner. Mirror X for the right-hand corner.
Towers pivot at their footprint centre (kit convention), front -Y, no bevel (seen from 60 m+), opaque window skin:
  Tower_Setback      30 m square, piers, setbacks at 48 / 84 / 112 m, crown, globe and spire to 172 m (landmark).
  Tower_Stepped      24 m square, setbacks at 42 / 70 m, pier crown to 98 m.
  Tower_Saucer       34 x 14 m stadium slab to 86 m on a podium, teal fin, saucer crown and mast to 126 m.
"""

import math

from geo import arc, rrect, stadium
from kit import asset
from mats import pal, register
from plaza import corner_path
from plaza_parts import bead, curved_glazing, glazing, lift, offset, ribbon
from structures import along, sec, tube

register("DK_Render_White", "stone", "#F6F2EA")    # lime render, whiter than the limestone
register("DK_Window_Tower", "enamel", "#34434B")   # opaque curtain skin: fresnel sky, dark interior
register("DK_Enamel_Coral", "enamel", "#E07560")

G, FH, SP, D = 5.0, 3.4, 1.05, 14.0  # ground floor top, floor height, spandrel height, bay depth
HW = 3.0
GLASS_Y, BACK_Y = 0.3, 1.4           # ribbon glass plane, interior back wall


def _straight(x0, x1):
    return [(x0, 0.0), (x1, 0.0)]


def top_of(n):
    return G + n * FH


# ----------------------------------------------------------------- shared elevation

def ground_trims(s, path, band, closed=False):
    """Plinth, fascia with gold beads, ivory canopy with a chrome nose, all following the plan path."""
    P = lift(path)
    tube(s, P, sec(-0.06, 0.3, 0.0, 0.45, 0.02), pal.Stone_Dark, closed=closed)
    tube(s, P, sec(-0.1, 0.3, 4.0, G, 0.03), band, closed=closed)
    for z in (4.06, G - 0.06):
        bead(s, path, -0.1, z, 0.025, pal.Gold, closed)
    tube(s, P, sec(-1.6, 0.0, 3.85, 4.0, 0.06), pal.Enamel_Ivory, closed=closed)
    bead(s, path, -1.6, 3.925, 0.03, pal.Chrome, closed)


def floor_trims(s, path, n, band, closed=False):
    """Per floor: a sill band on the spandrel and an ivory eyebrow over the ribbon; parapet band and coping."""
    P = lift(path)
    for k in range(n):
        zs, zt = G + k * FH + SP, G + (k + 1) * FH
        tube(s, P, sec(-0.05, 0.02, zs - 0.22, zs - 0.06, 0.02), band, closed=closed)
        tube(s, P, sec(-0.45, 0.05, zt - 0.02, zt + 0.1, 0.05), pal.Enamel_Ivory, closed=closed)
    zt = top_of(n)
    tube(s, P, sec(-0.07, 0.02, zt + 0.35, zt + 0.6, 0.03), band, closed=closed)
    tube(s, P, sec(-0.14, 0.4, zt + 0.95, zt + 1.1, 0.06), pal.Enamel_Ivory, closed=closed)


def trims(s, path, n, band, closed=False):
    ground_trims(s, path, pal.Enamel_Teal, closed)
    floor_trims(s, path, n, band, closed)


def mullions(s, path, z0, z1, y, step=1.5, mat=None):
    for p, t in along(path, step, margin=step / 2):
        with s.at((p.x, p.y, 0), (0, 0, math.degrees(math.atan2(t.y, t.x)))):
            s.box((-0.03, y - 0.08, z0), (0.03, y + 0.02, z1), mat or pal.Chrome)


# ----------------------------------------------------------------- straight bays

def shopfront(s, body):
    """Ground floor of a straight bay: half piers (a full 0.8 m pier forms on every joint), glazed shop, interior."""
    for x0, x1 in ((-HW, -HW + 0.4), (HW - 0.4, HW)):
        s.box((x0, 0.0, 0.45), (x1, D, 4.0), body)
    glazing(s, -HW + 0.4, HW - 0.4, GLASS_Y, 0.45, 4.0, nv=3, dz=2.75)
    s.box((-HW + 0.4, GLASS_Y, 0.3), (HW - 0.4, 3.2, 0.45), pal.Terrazzo_Ivory)  # shop floor
    s.box((-HW + 0.4, 3.2, 0.45), (HW - 0.4, 3.4, 4.0), pal.Lobby)               # shop back wall
    s.box((-HW, 0.0, 4.0), (HW, D, G), body)                                      # floor slab behind the fascia
    s.box((-HW + 0.4, 3.4, 0.45), (HW - 0.4, D, 4.0), body)  # between the piers: no coplanar side faces


def ribbon_floors(s, n, body):
    """n floors of full-width ribbon windows: spandrel slab, glass, dark interior wall, 6 cm end caps."""
    for k in range(n):
        zb = G + k * FH
        zs, zt = zb + SP, zb + FH
        s.box((-HW, 0.0, zb), (HW, D, zs), body)
        s.face([(-HW, GLASS_Y, zs), (HW, GLASS_Y, zs), (HW, GLASS_Y, zt), (-HW, GLASS_Y, zt)], pal.GlassDark)
        s.box((-HW, BACK_Y, zs), (HW, BACK_Y + 0.1, zt), pal.Enamel_Charcoal)
        s.box((-HW, BACK_Y + 0.1, zs), (HW, D, zt), body)
        for x0, x1 in ((-HW, -HW + 0.06), (HW - 0.06, HW)):
            s.box((x0, 0.0, zs), (x1, BACK_Y, zt), body)
        mullions(s, _straight(-HW, HW), zs, zt, GLASS_Y)
    s.box((-HW, 0.0, top_of(n)), (HW, D, top_of(n) + 0.95), body)  # parapet + roof deck


def stream_bay(n, body):
    """body: palette name, resolved at build time (a material fetched at import dies with the icon studio's reset)."""
    def build(s):
        body_mat = getattr(pal, body)
        shopfront(s, body_mat)
        ribbon_floors(s, n, body_mat)
        trims(s, _straight(-HW, HW), n, pal.Enamel_Seafoam)
    return build


asset("StreamBay_6", "Skyline", bevel=0.0)(stream_bay(6, "Limestone_Tex"))
asset("StreamBay_9", "Skyline", bevel=0.0)(stream_bay(9, "Limestone_Tex"))
asset("StreamBay_9W", "Skyline", bevel=0.0)(stream_bay(9, "Render_White"))


@asset("DecoBay_10", "Skyline", bevel=0.0)
def deco_bay(s):
    """Vertical Deco bay: limestone piers (half piers at the joints, a proud centre pier) over the shopfront, bronze
    spandrels between two window columns per floor; piers climb past the parapet as a stepped crown."""
    n, body = 10, pal.Limestone_Tex
    zt = top_of(n)
    shopfront(s, body)
    ground_trims(s, _straight(-HW, HW), pal.Enamel_Teal)
    s.box((-HW, 0.0, zt), (HW, D, zt + 0.95), body)
    s.box((-HW, 1.3, G), (HW, D, zt), body)
    s.box((-HW, 1.2, G), (HW, 1.3, zt), pal.Enamel_Charcoal)
    for x0, x1, y0, z1 in ((-HW, -HW + 0.3, -0.25, zt + 2.2), (HW - 0.3, HW, -0.25, zt + 2.2),
                           (-0.3, 0.3, -0.4, zt + 3.4)):
        s.box((x0, y0, G), (x1, 1.2, z1), body)
    s.box((-0.2, -0.3, zt + 3.4), (0.2, 1.0, zt + 4.6), body)  # centre pier's stepped cap
    for xa, xb in ((-HW + 0.3, -0.3), (0.3, HW - 0.3)):
        for k in range(n):
            zb = G + k * FH
            s.box((xa, 0.3, zb), (xb, 1.2, zb + SP), pal.Bronze)
            s.face([(xa, 0.35, zb + SP), (xb, 0.35, zb + SP), (xb, 0.35, zb + FH), (xa, 0.35, zb + FH)], pal.GlassDark)
        xm = (xa + xb) / 2
        s.box((xm - 0.03, 0.27, G), (xm + 0.03, 0.37, zt), pal.Chrome)
    s.box((-HW, -0.12, zt + 0.3), (HW, 0.0, zt + 0.6), pal.Enamel_Teal)


# ----------------------------------------------------------------- wrap-around blocks (corner, run ends)

def wrap_cap(s, p, t, zs, zf, body, start):
    """6 cm cap closing a ribbon's cavity at a path end (p, travel t), 0 .. BACK_Y inward."""
    with s.at((p.x, p.y, 0), (0, 0, math.degrees(math.atan2(t.y, t.x)))):
        s.box((0.0, 0.0, zs) if start else (-0.06, 0.0, zs), (0.06, BACK_Y, zf) if start else (0.0, BACK_Y, zf), body)


def flank_walls(s, path, back, z0, z1, body, t=0.3):
    """Brief 048 (Vela): the two closing edges of a wrap block's plan (path end -> back, back -> path start) as thin walls of the body over the shop storey. The lobby
    prism ran out to those edges, so the bay-joint flank of a run end or corner showed the dark brown shop interior from the street. Inward is the left of travel
    (the building is on the left of the path); the slab stands 1 cm proud of the plan edge so it does not z-fight the lobby's own flank."""
    for a, b in ((path[-1], back), (back, path[0])):
        dx, dy = b[0] - a[0], b[1] - a[1]
        L = math.hypot(dx, dy)
        if L < 0.05:
            continue
        nx, ny = -dy / L, dx / L
        o = -0.01
        s.extrude([(a[0] + nx * o, a[1] + ny * o), (b[0] + nx * o, b[1] + ny * o), (b[0] + nx * t, b[1] + ny * t), (a[0] + nx * t, a[1] + ny * t)], z0, z1, body)


def wrap_block(s, path, back, n, body):
    """Block whose facade follows a plan path (building on the left of travel) closed by `back`: solid piers on the
    first / last segments at ground, shop glazing round the rest, ribbons, eyebrows and trims wrapping the whole run."""
    plan = path + [back]
    ends = along(path, 1e3)  # (start, tangent), (end, tangent)
    for seg in (path[:2], path[-2:]):
        tube(s, lift(seg), sec(0, 0.8, 0.45, 4.0), body)
    curved_glazing(s, path[1:-1], 0.45, 4.0, a=GLASS_Y, step=1.5, dz=2.75, interior=False)
    s.extrude(offset(path, 2.6) + [back], 0.45, 4.0, pal.Lobby)
    s.extrude(offset(path, GLASS_Y) + [back], 0.3, 0.45, pal.Terrazzo_Ivory)
    flank_walls(s, path, back, 0.45, 4.0, body)
    s.extrude(plan, 4.0, G, body)
    for k in range(n):
        zb = G + k * FH
        zs, zf = zb + SP, zb + FH
        s.extrude(plan, zb, zs, body)
        ribbon(s, offset(path, GLASS_Y), zs, zf, pal.GlassDark)
        # brief 048 (Vela): the core behind the ribbon is solid body, with the charcoal as a skin in front of it (as DecoCorner / DecoEnd). It was one charcoal
        # prism, and its side wall on the bay-joint flank (path end -> back) was the building's wall there: flat black slabs from the street, no glass.
        s.extrude(offset(path, BACK_Y + 0.02) + [back], zs, zf, body)
        ribbon(s, offset(path, BACK_Y), zs, zf, pal.Enamel_Charcoal)
        wrap_cap(s, *ends[0], zs, zf, body, True)
        wrap_cap(s, *ends[-1], zs, zf, body, False)
        mullions(s, path, zs, zf, GLASS_Y)
    s.extrude(plan, top_of(n), top_of(n) + 0.95, body)
    trims(s, path, n, pal.Enamel_Seafoam)


@asset("StreamCorner_9", "Skyline", bevel=0.0)
def stream_corner(s):
    n = 9
    wrap_block(s, corner_path(n=24), (4.0, 8.0), n, pal.Limestone_Tex)
    zt = top_of(n)
    # corner fin on the arc's midpoint, facing out along the diagonal
    with s.at((2.0 - 6.0 * math.sqrt(0.5), 6.0 - 6.0 * math.sqrt(0.5), 0.0), (0, 0, -45)):
        s.rbox((-0.25, -1.3, G), (0.25, 0.6, zt + 8.0), 0.2, pal.Enamel_Ivory)
        for k in range(3):
            z = zt + 4.2 + 1.1 * k
            s.box((-0.27, -1.32, z), (0.27, 0.4, z + 0.35), pal.Enamel_Teal)


END_R = 3.0


def end_path(n=14):
    """Run end: the -X side wall (from the back) -> quarter round R 3 -> the front, to the bay joint at +3."""
    return [(-HW, D)] + arc(-HW + END_R, END_R, END_R, 180, 270, n) + [(HW, 0.0)]


def stream_end(n, body):
    """6 m bay that ends its run: a quarter-round front corner on -X and ribbons wrapping the full 14 m side, so a
    taller building steps down to a lower neighbour the streamline way instead of showing a blank party wall."""
    def build(s):
        wrap_block(s, end_path(), (HW, D), n, getattr(pal, body))
    return build


asset("StreamEnd_6", "Skyline", bevel=0.0)(stream_end(6, "Limestone_Tex"))
asset("StreamEnd_9", "Skyline", bevel=0.0)(stream_end(9, "Limestone_Tex"))
asset("StreamEnd_9W", "Skyline", bevel=0.0)(stream_end(9, "Render_White"))


# ----------------------------------------------------------------- towers

def pier_tier(s, h, z0, z1, step=1.5, floor=3.6, corner=1.3):
    """Square tier of half-width h: opaque window skin, bronze floor strips, limestone piers and corner piers, a
    parapet ring on top (the setback terrace edge)."""
    s.box((-h + 0.4, -h + 0.4, z0), (h - 0.4, h - 0.4, z1), pal.Window_Tower)
    for rot in (0, 90, 180, 270):
        with s.at((0, 0, 0), (0, 0, rot)):
            z = z0 + floor
            while z < z1 - 1.0:
                s.box((-h + corner, -h + 0.36, z - 0.9), (h - corner, -h + 0.42, z), pal.Bronze)
                z += floor
            k = max(1, round((2 * h - 2 * corner) / step))
            for i in range(1, k):
                x = -h + corner + i * (2 * h - 2 * corner) / k
                s.box((x - 0.22, -h, z0), (x + 0.22, -h + 0.45, z1), pal.Limestone_Tex)
            s.box((-h, -h, z0), (-h + corner, -h + corner, z1), pal.Limestone_Tex)
    s.box((-h, -h, z1), (h, h, z1 + 1.2), pal.Limestone_Tex)  # parapet: one box, so no coplanar overlaps


def spire(s, z0, height, r0):
    s.lathe([(r0, z0), (r0 * 0.7, z0 + height * 0.2), (r0 * 0.35, z0 + height * 0.6), (0.1, z0 + height * 0.97),
             (0.0, z0 + height)], pal.Chrome, seg=16)
    for f in (0.12, 0.3, 0.48):
        z, r = z0 + height * f, r0 * (1.0 - 0.7 * f) + 0.35
        s.lathe([(r - 0.3, z), (r, z + 0.15), (r, z + 0.45), (r - 0.3, z + 0.6)], pal.Gold, seg=24, cap_bottom=False,
                cap_top=False)


@asset("Tower_Setback", "Skyline", bevel=0.0)
def tower_setback(s):
    for h, z0, z1 in ((15.0, 0.0, 48.0), (11.5, 48.0, 84.0), (8.5, 84.0, 112.0), (5.5, 112.0, 128.0)):
        pier_tier(s, h, z0, z1)
    for h, z0, z1 in ((4.2, 129.2, 133.0), (3.0, 133.0, 136.5), (2.0, 136.5, 139.0)):
        s.box((-h, -h, z0), (h, h, z1), pal.Limestone_Tex)
        s.box((-h - 0.05, -h - 0.05, z1 - 0.5), (h + 0.05, h + 0.05, z1 - 0.2), pal.Gold)
    s.lathe([(0.0, 139.0)] + [(2.1 * math.sin(math.pi * k / 12), 141.1 - 2.1 * math.cos(math.pi * k / 12))
                              for k in range(1, 13)], pal.Chrome, seg=24)
    spire(s, 143.0, 29.0, 0.9)


@asset("Tower_Stepped", "Skyline", bevel=0.0)
def tower_stepped(s):
    for h, z0, z1 in ((12.0, 0.0, 42.0), (9.0, 42.0, 70.0), (6.0, 70.0, 90.0)):
        pier_tier(s, h, z0, z1)
    for rot in (0, 90, 180, 270):
        with s.at((0, 0, 0), (0, 0, rot)):
            for x, z1 in ((-2.4, 95.0), (0.0, 98.0), (2.4, 95.0)):
                s.box((x - 0.5, -6.2, 91.2), (x + 0.5, -4.8, z1), pal.Limestone_Tex)
    s.box((-4.8, -4.8, 91.2), (4.8, 4.8, 93.5), pal.Enamel_Teal)


@asset("Tower_Saucer", "Skyline", bevel=0.0)
def tower_saucer(s):
    s.extrude(rrect(42.0, 22.0, 4.0), 0.0, 8.0, pal.Limestone_Tex)
    s.extrude(rrect(42.4, 22.4, 4.2), 6.2, 7.0, pal.Enamel_Teal)
    z = 8.0
    while z < 86.0 - 1.0:
        s.extrude(stadium(34.0, 14.0), z, z + 1.2, pal.Render_White, top=False, bottom=False)
        s.extrude(stadium(33.4, 13.4), z + 1.2, z + 3.4, pal.Window_Tower, top=False, bottom=False)
        z += 3.4
    s.extrude(stadium(34.0, 14.0), z, 87.5, pal.Render_White)
    s.box((-0.5, -7.7, 8.0), (0.5, -6.4, 94.0), pal.Enamel_Coral)  # front fin
    s.lathe([(3.0, 87.5), (3.0, 97.0), (2.4, 97.0)], pal.Render_White, seg=24, cap_bottom=False)
    s.lathe([(2.4, 97.0), (13.5, 99.0), (15.0, 100.2), (15.0, 100.8), (13.0, 102.4), (6.0, 104.0), (0.0, 104.4)],
            pal.Enamel_Ivory, seg=48)
    s.lathe([(15.02, 100.1), (15.02, 100.9)], pal.Enamel_Teal, seg=48, cap_bottom=False, cap_top=False)
    spire(s, 104.4, 21.6, 0.6)
