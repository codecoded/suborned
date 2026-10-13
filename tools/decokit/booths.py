"""Booths & kiosks (reference sheet rows 4-5 + the espresso cart): phone booth, ticket machine, parcel
lockers, utility cabinet, coffee cart. Chunky rounded enamel volumes, chrome wherever parts meet, raised
bezels with real recesses for screens, vents and trays, and decal faces from the transit sheet."""

import math

from mathutils import Vector

from geo import arc, circle, dome, fillet, rrect, thicken
from kit import asset
from mats import DECAL, cell, pal, register
from signals import puck, soft
from vehicles_parts import lamp, plathe, rot_to

SIDE = (90, 0, 90)  # local X -> world Y, local Y -> world Z, extrusion -> world X
FRONT = (90, 0, 0)  # local XY -> world XZ (a panel facing -Y), extrusion / lathe axis -> world -Y
BACK = (-90, 0, 0)  # lathe axis -> world +Y
register("DK_Bakelite", "enamel", "#17181B")
register("DK_Enamel_Coral", "enamel", "#E07560")  # as skyline.py (register is first-wins)
register("DK_LampSoft", "emit", "#FFE4B8", 1.3)  # as furnish_parts.py
# low glows keep their colour under the tone curve (as the signal lenses); a small hot core carries the light
register("DK_Screen_Amber", "emit", "#B8581A", 0.7)  # warm videophone / ticket screens
register("DK_Glow_Seafoam", "emit", "#3FC9A0", 0.7)  # lit glass crowns
register("DK_Glow_Amber", "emit", "#F28A1E", 1.1)
PLATES = (((22, 88), (95, 162), (169, 235)), ((32, 120), (128, 217)))  # number-plate pixel runs, cells 4/5


# ----------------------------------------------------------------- local helpers

def sub(i, x0, y0, x1, y1, px=256):
    """UV quad (BL, BR, TR, TL) of a pixel sub-rect (cell-local, y down) of decal cell i."""
    (u0, _), _, (u1, v1), _ = cell(i)
    k = (u1 - u0) / px
    return ((u0 + x0 * k, v1 - y1 * k), (u0 + x1 * k, v1 - y1 * k), (u0 + x1 * k, v1 - y0 * k),
            (u0 + x0 * k, v1 - y0 * k))


def plate_uv(n):
    """Locker number plate 1..12 cut from the lockers_1_6 / lockers_7_12 cells."""
    i = (n - 1) % 6
    (cx0, cx1), (ry0, ry1) = PLATES[0][i % 3], PLATES[1][i // 3]
    return sub(DECAL["lockers_1_6"] if n <= 6 else DECAL["lockers_7_12"], cx0, ry0, cx1, ry1)


def fdecal(s, x, y, z, w, h, uv):
    """Decal quad on the plane y facing -Y, centred at (x, z)."""
    s.decal([(x - w / 2, y, z - h / 2), (x + w / 2, y, z - h / 2), (x + w / 2, y, z + h / 2),
             (x - w / 2, y, z + h / 2)], pal.Decal_Transit, uv)


def ccw(poly):
    a = sum(x0 * y1 - x1 * y0 for (x0, y0), (x1, y1) in zip(poly, poly[1:] + poly[:1]))
    return poly if a > 0 else poly[::-1]


def bead(r0, dr, z0, z1, n=6):
    """Open (r, z) half-round collar profile standing dr proud of radius r0 (lathe, caps off)."""
    return [(r0 + dr * math.cos(a), (z0 + z1) / 2 + (z1 - z0) / 2 * math.sin(a))
            for a in (math.pi * (k / n - 0.5) for k in range(n + 1))]


def clip_x(poly, x0, x1):
    """Clip a convex CCW polygon to x0 <= x <= x1 (Sutherland-Hodgman)."""
    def clip(pts, x, keep):
        out = []
        for a, b in zip(pts[-1:] + pts[:-1], pts):
            ia, ib = keep(a[0]), keep(b[0])
            if ia != ib:
                t = (x - a[0]) / (b[0] - a[0])
                out.append((x, a[1] + (b[1] - a[1]) * t))
            if ib:
                out.append(b)
        return out
    return clip(clip(poly, x0, lambda v: v >= x0), x1, lambda v: v <= x1)


def bezel(s, w, h, r, d, lip, rim, wall, floor=None, n=5):
    """Raised rounded-rect bezel in local XY standing d proud along +Z, with a real recess (inset lip)
    whose walls step back down to z = 0.003; `floor` caps the recess (screen, vent back, tray)."""
    def R(i):
        return rrect(w - 2 * i, h - 2 * i, max(r - i, 0.004), n)
    s.loft([(R(0), -0.01), (R(0), d - 0.006), (R(0.006), d), (R(lip), d)], rim, cap_bottom=False, cap_top=False)
    s.loft([(R(lip), d), (R(lip + 0.003), 0.003)], wall, cap_bottom=False, cap_top=False)
    if floor is not None:
        s.extrude(R(lip + 0.003), -0.005, 0.003, floor, bottom=False)


def resample(pts, n):
    P = [Vector(p) for p in pts]
    L = [0.0]
    for a, b in zip(P, P[1:]):
        L.append(L[-1] + (b - a).length)
    out, j = [], 0
    for i in range(n + 1):
        t = L[-1] * i / n
        while j < len(L) - 2 and L[j + 1] < t:
            j += 1
        out.append(P[j].lerp(P[j + 1], (t - L[j]) / max(L[j + 1] - L[j], 1e-9)))
    return out


def coil(path, rc, turns, n=220):
    """Helical cord wound around a base polyline (phone cords)."""
    P = resample(path, n)
    out = []
    for i, p in enumerate(P):
        t = (P[min(i + 1, n)] - P[max(i - 1, 0)]).normalized()
        nx = t.cross(Vector((1, 0, 0)))
        nx = (nx if nx.length > 1e-3 else t.cross(Vector((0, 1, 0)))).normalized()
        a = 2 * math.pi * turns * i / n
        out.append(tuple(p + rc * (math.cos(a) * nx + math.sin(a) * t.cross(nx))))
    return out


def wheel(s, r, w, spokes=8, hub=None, seg=36):
    """Wheel about local Z (outer face +Z): rubber tyre, chrome rim, spokes, domed hub."""
    tyre = [(r * 0.7, -w / 2), (r * 0.86, -w / 2), (r * 0.96, -w * 0.42), (r, -w * 0.18), (r, w * 0.18),
            (r * 0.96, w * 0.42), (r * 0.86, w / 2), (r * 0.7, w / 2), (r * 0.7, -w / 2)]
    s.lathe(tyre, pal.Rubber, seg=seg, cap_bottom=False, cap_top=False)
    if spokes:  # caster wheels (spokes=0) are just tyre + hub
        s.lathe(bead(r * 0.6, r * 0.11, -w * 0.3, w * 0.34), pal.Chrome, seg=seg, cap_bottom=False, cap_top=False)
        s.lathe([(0, -w * 0.3), (r * 0.62, -w * 0.3), (r * 0.62, -w * 0.2), (0, -w * 0.2)], pal.Iron, seg=seg)
    for k in range(spokes):
        a = 2 * math.pi * k / spokes
        s.sweep([(r * 0.16 * math.cos(a), r * 0.16 * math.sin(a), w * 0.15),
                 (r * 0.63 * math.cos(a), r * 0.63 * math.sin(a), w * 0.05)], r * 0.028, pal.Chrome, seg=10)
    s.lathe([(0, -w * 0.2), (r * 0.24, -w * 0.2), (r * 0.26, w * 0.12)] + dome(r * 0.24, w * 0.14, w * 0.28, 4)[1:],
            hub or pal.Chrome, seg=max(seg * 2 // 3, 12))


# ----------------------------------------------------------------- videophone booth (props sheet, booths #5)

def hood(s, R, a, t, shell, lining):
    """Acoustic hood about local +Z: a sphere (outer radius R, wall t) cut open at +Z to an opening of radius a,
    shell outside, lining inside, a chrome roll over the rim. Local X stays horizontal, local Y is up."""
    th, Ri, n = math.asin(a / R), R - t, 18
    zr = R * math.cos(th)
    ai = math.sqrt(Ri * Ri - zr * zr)
    outer = [(R * math.sin(p), -R * math.cos(p)) for p in ((math.pi - th) * k / n for k in range(n + 1))]
    thi = math.acos(-zr / Ri)  # inner rim's angle from the back pole
    inner = [(Ri * math.sin(p), -Ri * math.cos(p)) for p in (thi * (n - k) / n for k in range(n + 1))]
    plathe(s, outer + inner, [shell] * len(outer) + [lining] * len(inner), seg=36)
    c, rho = (a + ai) / 2, (a - ai) / 2 + 0.01
    s.lathe([(c + rho, zr - 0.02)] + [(c + rho * math.cos(b), zr + rho * math.sin(b))
                                       for b in (math.pi * k / 8 for k in range(9))] + [(c - rho, zr - 0.02)],
            pal.Chrome, seg=36, cap_bottom=False, cap_top=False)
    # chrome band round the horizontal great circle, broken by the opening
    Rb, p0 = R + 0.002, math.asin(min((zr - 0.03) / (R + 0.002), 1.0))
    band = [(Rb * math.cos(p), 0, Rb * math.sin(p)) for p in
            (math.pi - p0 + (math.pi + 2 * p0) * k / 40 for k in range(41))]
    s.sweep(band, 0.01, pal.Chrome, seg=8)
    return zr, Ri


def videophone(s, w=0.28, h=0.4):
    """Wall videophone in the booths' panel frame (face -Y, up +Z, origin on the wall): screen, keypad,
    handset on its hook with a coiled cord."""
    s.rbox((-w / 2, -0.05, -h / 2), (w / 2, 0.01, h / 2), 0.03, pal.Enamel_Teal)
    with s.at((0.02, -0.05, 0.08), FRONT):
        bezel(s, 0.17, 0.14, 0.02, 0.018, 0.012, pal.Chrome, pal.Iron, pal.Screen_Amber)
    fdecal(s, 0.03, -0.051, -0.09, 0.09, 0.1, sub(DECAL["keypad"], 31, 30, 225, 240))
    X, F = -0.1, -0.05
    s.rbox((X - 0.03, F - 0.03, 0.11), (X + 0.03, F + 0.01, 0.135), 0.006, pal.Chrome)  # cradle hook
    s.sweep(fillet([(X, F - 0.035, -0.12), (X, F - 0.05, -0.05), (X, F - 0.05, 0.04), (X, F - 0.035, 0.11)],
                   0.03, n=4, closed=False), 0.013, pal.Bakelite, seg=10)
    for z in (-0.12, 0.11):
        with s.at((X, F - 0.047, z), BACK):
            s.lathe([(0, 0), (0.022, 0), (0.028, 0.011), (0.028, 0.025), (0.02, 0.036), (0, 0.036)],
                    pal.Bakelite, seg=14)
    cord = fillet([(X, F - 0.03, -0.14), (X + 0.01, F - 0.04, -0.19), (X + 0.08, F - 0.03, -0.2),
                   (X + 0.1, F + 0.005, -0.16)], 0.04, n=6, closed=False)
    s.sweep(coil(cord, 0.006, 8, n=90), 0.0035, pal.Bakelite, seg=8, caps=False)


def starburst(s, r, spikes, length, mat):
    """Atomic-age star: a ball with tapered spikes on a golden-angle spiral."""
    s.lathe([(r * math.sin(math.pi * k / 10), -r * math.cos(math.pi * k / 10)) for k in range(11)], mat, seg=20)
    for k in range(spikes):
        z = 1 - 2 * (k + 0.5) / spikes
        a = k * math.pi * (3 - math.sqrt(5))
        d = Vector((math.sqrt(1 - z * z) * math.cos(a), math.sqrt(1 - z * z) * math.sin(a), z))
        s.sweep([tuple(d * r * 0.7), tuple(d * (r + length * 0.6)), tuple(d * (r + length))],
                [r * 0.22, r * 0.09, 0.0015], mat, seg=8)


@asset("PhoneBooth", "Booths", bevel=0.0, lean=0.45, cap=1500)
def phone_booth(s):
    """Twin acoustic hoods, teal and cream, back to back on a coral drum; each opens toward the walk at head
    height with a videophone on its back wall, a ledge, a lamp; a chrome starburst rides the join."""
    R, A, ZC, X, YAW, WALL = 0.65, 0.47, 1.45, 0.63, 15.0, -0.25
    s.lathe([(0, 0), (0.6, 0), (0.6, 0.07), (0.58, 0.08)], pal.Rubber, seg=48, cap_top=False)  # kick ring
    s.lathe([(0.58, 0.07), (0.58, 0.96), (0.565, 1.0), (0.53, 1.02)], pal.Enamel_Coral, seg=48,
            cap_bottom=False, cap_top=False)
    s.lathe(dome(0.53, 1.015, 0.05, 6), pal.Chrome, seg=48, cap_bottom=False)  # under the hood ledges
    with s.at((0, 0, ZC), (0, 90, 0)):  # waist: a fillet of revolution tangent to both shells, chrome seam ring
        rw = 0.45  # waist radius; the fillet lies outside both spheres, so it never reaches into a hood
        rho = (R * R - X * X - rw * rw) / (2 * (rw - R))  # tangency: X^2 + (rw + rho)^2 = (R + rho)^2
        rc, xt = rw + rho, X * rho / (R + rho)
        prof = [(rc - math.sqrt(rho * rho - x * x), x) for x in (-xt + 2 * xt * k / 16 for k in range(17))]
        plathe(s, prof, [pal.Enamel_Seafoam] * 8 + [pal.Enamel_Ivory] * 8, seg=40)  # open band: its edges lie on the shells
        s.lathe([(rw - 0.02, -0.018), (rw + 0.012, -0.012), (rw + 0.016, 0.0), (rw + 0.012, 0.012),
                 (rw - 0.02, 0.018)], pal.Chrome, seg=40, cap_bottom=False, cap_top=False)
    for sx, shell in ((-1, pal.Enamel_Seafoam), (1, pal.Enamel_Ivory)):
        with s.at((sx * X, 0, ZC), (90, 0, sx * YAW)):  # local +Z out of the opening, Y up, X along the street
            zr, Ri = hood(s, R, A, 0.04, shell, pal.Enamel_Cream)
            s.extrude(circle(math.sqrt(Ri * Ri - WALL * WALL) + 0.005, 40), WALL - 0.02, WALL, pal.Enamel_Cream)
            with s.at((0, 0.02, WALL), (-90, 0, 0)):  # alcove wall, panel frame
                videophone(s, 0.3, 0.44)
            y = -0.3  # ledge across the lower hood, lined teal
            rho = math.sqrt(Ri * Ri - y * y) - 0.012
            with s.at((0, y, 0), (-90, 0, 0)):
                ledge = [(rho * math.cos(b), min(max(rho * math.sin(b), -zr + 0.02), -WALL)) for b in
                         (2 * math.pi * k / 32 for k in range(32))]
                s.extrude(ledge, -0.03, 0.0, pal.Enamel_Teal, smooth_sides=True)
            d = Vector((sx * 0.9, 0.2, 0.05)).normalized()  # wall lamp on the outboard cheek
            with s.at(tuple(d * (Ri - 0.004)), rot_to(-d)):
                s.lathe([(0.03, 0), (0.03, 0.02), (0, 0.022)], pal.Chrome, seg=12)
                s.lathe([(0, 0.02), (0.022, 0.02), (0.022, 0.07), (0, 0.078)], pal.LampSoft, seg=12)
    s.sweep([(0, 0, ZC + 0.1), (0, 0, ZC + 0.62)], [0.04, 0.02], pal.Chrome, seg=12)
    with s.at((0, 0, ZC + 0.62)):
        s.lathe(puck(0.05, -0.02, 0.02, 0.012), pal.Chrome, seg=16)
    with s.at((0, 0, ZC + 0.72)):
        starburst(s, 0.055, 16, 0.15, pal.Chrome)


# ----------------------------------------------------------------- ticket machine (props sheet, kiosks #5)

@asset("TicketMachine", "Booths", bevel=0.005)
def ticket_machine(s):
    """Jukebox-form ticket machine: a glowing arched crown in chrome hoops over a cream front with a porthole
    screen, slot column and ticket tray; seafoam flanks, a coral kick panel with an arrow, black rubber skirt."""
    W, D, ZB = 0.95, 0.64, 1.25
    YF, hw = -D / 2, W / 2
    s.rbox((-hw - 0.012, YF - 0.012, 0.0), (hw + 0.012, -YF + 0.012, 0.07), 0.02, pal.Chrome)
    s.rbox((-hw + 0.004, YF + 0.004, 0.06), (hw - 0.004, -YF - 0.004, 0.37), 0.02, pal.Rubber)
    s.rbox((-hw, YF, 0.36), (hw, -YF, ZB), 0.035, pal.Enamel_Seafoam)
    s.rbox((-hw + 0.055, YF - 0.012, 0.25), (hw - 0.055, YF + 0.03, ZB - 0.015), 0.012, pal.Enamel_Cream)
    for x in (-hw + 0.03, hw - 0.03):  # chrome corner columns, front and back
        s.rbox((x - 0.03, YF - 0.022, 0.06), (x + 0.03, YF + 0.05, ZB + 0.01), 0.022, pal.Chrome)
        s.rbox((x - 0.03, -YF - 0.05, 0.06), (x + 0.03, -YF + 0.012, ZB + 0.01), 0.022, pal.Chrome)
    s.rbox((-hw + 0.075, YF - 0.022, 0.1), (hw - 0.075, YF + 0.02, 0.47), 0.015, pal.Chrome)  # kick panel frame
    s.rbox((-hw + 0.09, YF - 0.03, 0.115), (hw - 0.09, YF + 0.01, 0.455), 0.012, pal.Enamel_Coral)
    arrow = [(-0.14, -0.018), (0.05, -0.018), (0.05, -0.06), (0.14, 0.0), (0.05, 0.06), (0.05, 0.018), (-0.14, 0.018)]
    with s.at((0, YF - 0.03, 0.3), FRONT):
        s.extrude(arrow, -0.004, 0.006, pal.Enamel_Cream)
    with s.at((-0.1, YF - 0.012, 0.98), FRONT):  # porthole screen
        s.lathe(soft([(0.12, -0.01), (0.12, 0.012), (0.135, 0.03), (0.155, 0.032), (0.17, 0.018), (0.17, -0.01)],
                     0.006), pal.Chrome, seg=40, cap_bottom=False, cap_top=False)
        s.lathe([(0.121, 0.0), (0.09, 0.012), (0.05, 0.018), (0, 0.02)], pal.Screen_Amber, seg=40)
    with s.at((0.24, YF - 0.012, 0.94), FRONT):  # slot column
        bezel(s, 0.1, 0.36, 0.02, 0.022, 0.014, pal.Chrome, pal.Iron, pal.Iron)
        for z in (0.1, 0.02, -0.06):
            s.rbox((-0.03, z - 0.012, 0.0), (0.03, z + 0.012, 0.012), 0.004, pal.Chrome)
    with s.at((-0.1, YF - 0.012, 0.66), FRONT):  # ticket tray
        bezel(s, 0.28, 0.12, 0.025, 0.035, 0.018, pal.Chrome, pal.Iron, pal.Iron)
        s.box((-0.1, -0.01, 0.0), (0.1, 0.006, 0.004), pal.Rubber)
    for x in (-0.27, 0.07):  # indicator lamps flanking the porthole
        lamp(s, (x, YF - 0.012, 0.82), (0, -1, 0), 0.01, lens=pal.Signal_Red, depth=0.008, seg=10)
    s.rbox((-hw - 0.02, YF - 0.035, ZB - 0.01), (hw + 0.02, -YF + 0.02, ZB + 0.04), 0.02, pal.Chrome)  # cornice
    r = hw - 0.025
    with s.at((0, -YF - 0.03, ZB + 0.04), FRONT):  # crown: seafoam glass vault, amber glowing face
        s.extrude(arc(0, 0, r, 0, 180, 28), 0.0, D - 0.06, pal.Glow_Seafoam, smooth_sides=True)
        s.extrude(arc(0, 0, r - 0.06, 0, 180, 24), D - 0.07, D - 0.055, pal.Glow_Amber)
    for y in (YF + 0.03, YF + 0.2, -YF - 0.03):  # chrome hoops over the crown
        s.sweep([(0.0 + (r + 0.012) * math.cos(a), y, ZB + 0.04 + (r + 0.012) * math.sin(a))
                 for a in (math.pi * k / 28 for k in range(29))], 0.022, pal.Chrome, seg=10)
    s.rbox((hw - 0.004, 0.02, 1.02), (hw + 0.008, 0.16, 1.12), 0.01, pal.Gold)  # flank badge


# ----------------------------------------------------------------- parcel lockers

@asset("Lockers", "Booths", bevel=0.004)
def lockers(s):
    Z0, Z1, g = 0.12, 1.74, 0.009
    s.rbox((-1.2, -0.305, 0.0), (1.2, 0.29, Z0 + 0.01), 0.03, pal.Enamel_Powder)  # plinth
    for x in (-1.2, 1.14):
        s.rbox((x, -0.3, 0.1), (x + 0.06, 0.3, Z1 + 0.01), 0.025, pal.Enamel_Powder)  # end panels
    s.box((-1.14, -0.27, Z0), (1.14, 0.27, Z1), pal.Enamel_Charcoal)  # carcass: reads dark in the seams
    s.rbox((-1.15, 0.26, Z0 - 0.01), (1.15, 0.295, Z1 + 0.01), 0.012, pal.Enamel_Powder)  # finished back
    s.rbox((-1.245, -0.34, Z1 - 0.01), (1.245, 0.315, 1.9), 0.065, pal.Enamel_Ivory)  # top slab
    cols = ((0.45, pal.Enamel_Ivory, (1, 1, 1, 1), 1), (0.45, pal.Enamel_Red, (1, 1, 1, 1), 5),
            (0.48, None, None, 0), (0.45, pal.Enamel_Ivory, (1.2, 0.55, 0.55, 0.55, 1.6), 0),
            (0.45, pal.Enamel_Ivory, (1, 1, 1, 1), 9))
    x = -1.14
    for w, mat, rows, num in cols:
        if mat is None:
            control_column(s, x + w / 2, w, Z0, Z1)
            x += w
            continue
        z, tot = Z1, sum(rows)
        for k, rw in enumerate(rows):
            h = (Z1 - Z0) * rw / tot
            x0, x1, z0, z1 = x + g / 2, x + w - g / 2, z - h + g / 2, z - g / 2
            s.rbox((x0, -0.3, z0), (x1, -0.27, z1), 0.012, mat)
            hz = z1 - 0.06 if h > 0.3 else (z0 + z1) / 2
            s.rbox((x1 - 0.1, -0.314, hz - 0.017), (x1 - 0.03, -0.296, hz + 0.017), 0.007, pal.Chrome)  # latch
            s.box((x1 - 0.052, -0.3165, hz - 0.006), (x1 - 0.044, -0.312, hz + 0.006), pal.Iron)
            if num:
                fdecal(s, x0 + 0.065, -0.3, z1 - 0.07, 0.066, 0.088, plate_uv(num + k))
            z -= h
        x += w


def control_column(s, cx, w, z0, z1):
    s.rbox((cx - w / 2 + 0.004, -0.318, z0 + 0.004), (cx + w / 2 - 0.004, -0.27, z1 - 0.004), 0.02,
           pal.Enamel_Royal)
    with s.at((cx, -0.318, 1.17), FRONT):  # screen hood
        bezel(s, 0.34, 0.38, 0.04, 0.055, 0.032, pal.Chrome, pal.Steel, pal.Screen)
    fdecal(s, cx, -0.318, 0.88, 0.15, 0.162, sub(DECAL["keypad"], 31, 30, 225, 240))
    s.rbox((cx - 0.1, -0.33, 0.7), (cx + 0.1, -0.31, 0.75), 0.01, pal.Chrome)  # receipt slot
    s.rbox((cx - 0.085, -0.335, 0.715), (cx + 0.085, -0.325, 0.735), 0.004, pal.Enamel_Red)
    s.box((cx - 0.07, -0.338, 0.721), (cx + 0.07, -0.33, 0.729), pal.Iron)
    s.rbox((cx - 0.07, -0.325, 0.62), (cx + 0.07, -0.31, 0.635), 0.005, pal.Chrome)  # label rule


# ----------------------------------------------------------------- utility cabinet

@asset("UtilityCabinet", "Booths", bevel=0.005)
def utility_cabinet(s):
    s.rbox((-0.61, -0.287, 0.0), (0.61, 0.287, 0.085), 0.022, pal.Chrome)  # plinth
    s.rbox((-0.605, -0.28, 0.07), (0.605, 0.28, 1.15), 0.055, pal.Enamel_Powder)
    Y = -0.293
    for sx in (-1, 1):
        x0, x1 = sorted((sx * 0.08, sx * 0.575))
        s.rbox((x0, Y, 0.11), (x1, -0.275, 1.12), 0.012, pal.Enamel_Powder)  # door
        with s.at(((x0 + x1) / 2, Y, 0.83), FRONT):  # louvred vent: real slats in a recess
            bezel(s, 0.3, 0.38, 0.02, 0.03, 0.02, pal.Enamel_Powder, pal.Enamel_Charcoal, pal.Enamel_Charcoal)
            for k in range(10):
                with s.at((0, -0.1485 + 0.033 * k, 0.015), (-50, 0, 0)):
                    s.box((-0.127, -0.017, -0.0025), (0.127, 0.017, 0.0025), pal.Enamel_Powder)
    s.rbox((-0.075, -0.302, 0.09), (0.075, -0.275, 1.14), 0.016, pal.Enamel_Mustard)  # centre strip
    s.rbox((-0.025, -0.31, 0.52), (0.025, -0.296, 0.67), 0.008, pal.Chrome)  # lock escutcheon
    s.rbox((-0.013, -0.33, 0.545), (0.013, -0.306, 0.64), 0.009, pal.Chrome)  # lever handle
    s.box((-0.004, -0.312, 0.652), (0.004, -0.306, 0.66), pal.Iron)  # key slot
    fdecal(s, -0.33, Y, 0.38, 0.25, 0.141, sub(DECAL["city_utility"], 14, 49, 242, 178))
    s.rbox((-0.645, -0.318, 1.13), (0.645, 0.305, 1.28), 0.06, pal.Enamel_Ivory)  # lid


# ----------------------------------------------------------------- coffee cart

def loaf(u, D, H, hs=0.06, n=16, k=1.0):
    """Canopy cross-section (local x = world Y, local y = height), shrunk by u toward the rounded ends."""
    top = [(D / 2 * u * math.cos(math.pi * i / n), (hs + (H * k - hs) * math.sin(math.pi * i / n) ** 0.75) * u ** 0.5)
           for i in range(n + 1)]
    return [(D / 2 * u, 0.0)] + top + [(-D / 2 * u, 0.0)]


def canopy(s, L, D, H, z):
    e = D * 0.5
    ends = [L / 2 - e + e * math.cos(0.06 + (math.pi / 2 - 0.06) * k / 8) for k in range(9)]
    bands = ((-L / 2 - 1, -0.86, "Enamel_Ivory"), (-0.86, -0.5, "Enamel_Red"), (-0.5, 0.54, "Enamel_Ivory"),
             (0.54, 0.57, "Chrome"), (0.575, L / 2 + 1, "Enamel_Ivory"))
    xs = sorted({round(v, 4) for v in [-x for x in ends] + ends + [-0.3, 0.0, 0.3, 0.42, 0.48, 0.64, 0.7] +
                 [b for a, b, _ in bands[:-1]]})

    def dip(x):  # the roof reads as two pillows meeting at the chrome rib
        return 1 - 0.07 * math.exp(-((x - 0.555) / 0.1) ** 2)

    def u(x):
        t = max(0.0, (abs(x) - (L / 2 - e)) / e)
        return math.sqrt(max(1 - t * t, 0.004))
    with s.at((0, 0, z), SIDE):
        for i, (a, b, m) in enumerate(bands):
            rib = m == "Chrome"
            sec = [(loaf(u(x) * (1.02 if rib else 1.0), D, H, k=dip(x)), x) for x in xs if a <= x <= b]
            s.loft(sec, getattr(pal, m), cap_bottom=i == 0 or rib, cap_top=i == len(bands) - 1 or rib)
    rx = sorted(set([-x for x in ends] + ends))
    rim = [(x, -D / 2 * u(x) - 0.004, z) for x in rx] + [(x, D / 2 * u(x) + 0.004, z) for x in rx[::-1]]
    s.sweep(rim + rim[:1], 0.022, pal.Chrome, seg=10, caps=False)


def espresso(s):
    C = pal.Chrome
    s.rbox((-0.27, -0.2, 0.0), (0.27, 0.2, 0.05), 0.015, C)  # base
    s.rbox((-0.26, -0.03, 0.04), (0.26, 0.19, 0.3), 0.025, C)  # tower behind the brew bay
    s.rbox((-0.285, -0.2, 0.28), (0.285, 0.2, 0.47), 0.03, C)  # head housing
    s.rbox((-0.24, -0.235, 0.045), (0.24, -0.03, 0.07), 0.008, pal.Steel)  # drip tray
    for k in range(6):
        s.box((-0.21, -0.215 + 0.03 * k, 0.066), (0.21, -0.205 + 0.03 * k, 0.072), pal.Iron)
    s.rbox((-0.25, -0.212, 0.315), (0.25, -0.19, 0.44), 0.01, pal.Iron)  # control fascia
    for x in (-0.17, 0.17):  # pressure gauges
        with s.at((x, -0.21, 0.38), FRONT):
            s.lathe([(0, 0), (0.04, 0), (0.044, 0.012), (0.036, 0.018), (0, 0.018)], C, seg=16)
            s.lathe([(0.032, 0.0185), (0, 0.0185)], pal.Enamel_Ivory, seg=16, cap_bottom=False)
    for x in (-0.06, 0.0, 0.06):  # buttons
        with s.at((x, -0.21, 0.38), FRONT):
            s.lathe([(0, 0), (0.016, 0), (0.016, 0.014)] + dome(0.016, 0.014, 0.008, 4)[1:], pal.Enamel_Red, seg=12)
    for x in (-0.12, 0.12):  # group heads, portafilters, cups
        with s.at((x, -0.11, 0)):
            s.lathe([(0, 0.2), (0.042, 0.2), (0.05, 0.215), (0.05, 0.28), (0, 0.28)], C, seg=16)
            s.lathe([(0, 0.17), (0.04, 0.17), (0.044, 0.2), (0, 0.2)], pal.Steel, seg=16)
            s.sweep([(0, -0.03, 0.185), (0, -0.12, 0.17), (0, -0.2, 0.16)], [0.012, 0.014, 0.015], pal.Bakelite, seg=10)
            s.lathe([(0, 0.07), (0.026, 0.07), (0.032, 0.125), (0.028, 0.125), (0.022, 0.077), (0, 0.077)],
                    pal.Enamel_Ivory, seg=16)
    rail = fillet([(-0.24, -0.16, 0.52), (0.24, -0.16, 0.52), (0.24, 0.16, 0.52), (-0.24, 0.16, 0.52)], 0.04, n=3)
    s.sweep(rail + rail[:1], 0.008, C, seg=10, caps=False)
    for x in (-0.24, 0.24):
        for y in (-0.16, 0.16):
            s.sweep([(x, y, 0.46), (x, y, 0.52)], 0.007, C, seg=10)
    for x, y in ((-0.12, 0.02), (0.02, -0.05), (0.14, 0.04)):  # cups warming on top
        with s.at((x, y, 0.47)):
            s.lathe([(0, 0), (0.026, 0), (0.032, 0.055), (0.028, 0.055), (0.022, 0.007), (0, 0.007)],
                    pal.Enamel_Ivory, seg=12)
    s.sweep(fillet([(0.285, -0.08, 0.37), (0.34, -0.1, 0.36), (0.35, -0.16, 0.14)], 0.05, n=5, closed=False),
            0.007, C, seg=10)  # steam wand
    with s.at((0.3, -0.05, 0.42), (0, 90, 0)):
        s.lathe([(0, 0), (0.022, 0), (0.022, 0.03)] + dome(0.022, 0.03, 0.012, 4)[1:], pal.Bakelite, seg=12)


@asset("CoffeeCart", "Booths", bevel=0.005)
def coffee_cart(s):
    foot = rrect(1.9, 0.8, 0.2, n=8)
    for x0, x1, m in ((-1.0, -0.38, pal.Enamel_Teal), (-0.38, 0.02, pal.Enamel_Red), (0.02, 1.0, pal.Enamel_Mustard)):
        s.extrude(clip_x(foot, x0, x1), 0.33, 0.98, m)
    s.loft([(rrect(1.88, 0.78, 0.19, 8), 0.28), (rrect(1.935, 0.835, 0.215, 8), 0.3),
            (rrect(1.935, 0.835, 0.215, 8), 0.355), (rrect(1.91, 0.81, 0.205, 8), 0.37)], pal.Chrome)  # skirt
    s.loft([(rrect(2.0 + 2 * e, 0.9 + 2 * e, 0.22 + e, 8), z) for e, z in
            ((-0.07, 0.96), (0.0, 0.975), (0.018, 0.992), (0.024, 1.02), (0.018, 1.05), (0.0, 1.065))], pal.Chrome,
           cap_top=False)
    s.extrude(rrect(2.0, 0.9, 0.22, 8), 1.0, 1.068, pal.Steel)  # brushed counter top inside the chrome nosing
    for x in (-0.8, 0.8):  # canopy posts: chrome shafts, red sleeves under the roof
        for y in (-0.34, 0.34):
            with s.at((x, y, 1.07)):
                s.lathe([(0, 0), (0.062, 0), (0.062, 0.022), (0.048, 0.05), (0.04, 0.065), (0.04, 0.92)], pal.Chrome,
                        seg=16)
                s.lathe([(0.047, 0.5), (0.047, 0.9)], pal.Enamel_Red, seg=16, cap_bottom=False, cap_top=False)
                s.lathe(bead(0.045, 0.012, 0.48, 0.52), pal.Chrome, seg=16, cap_bottom=False, cap_top=False)
    canopy(s, 2.2, 1.05, 0.46, 1.95)
    with s.at((0.05, 0.06, 1.068), scale=1.12):
        espresso(s)
    with s.at((0.72, 0.16, 1.07)):  # grinder
        s.lathe([(0, 0), (0.08, 0), (0.08, 0.02), (0.065, 0.04), (0.065, 0.24), (0.05, 0.27), (0.03, 0.28)],
                pal.Chrome, seg=24)
        s.lathe([(0.03, 0.28), (0.09, 0.42), (0.09, 0.44), (0, 0.44)], pal.GlassDark, seg=24, cap_bottom=False)
        s.lathe([(0.092, 0.44), (0.092, 0.46)] + dome(0.085, 0.46, 0.03, 4)[1:], pal.Enamel_Red, seg=24)
    for x in (0.42, 0.53):  # syrup canisters
        with s.at((x, -0.22, 1.07)):
            s.lathe([(0, 0), (0.042, 0), (0.042, 0.15)], pal.Enamel_Ivory, seg=18)
            s.lathe([(0.046, 0.14), (0.046, 0.17)] + dome(0.046, 0.17, 0.02, 4)[1:], pal.Enamel_Red, seg=18)
    with s.at((-0.42, -0.2, 1.07)):  # milk pitcher
        s.lathe([(0, 0), (0.042, 0), (0.046, 0.05), (0.036, 0.11), (0.033, 0.11), (0.042, 0.05), (0.037, 0.005),
                 (0, 0.005)], pal.Chrome, seg=20)
    for y, rot in ((-0.47, FRONT), (0.47, BACK)):  # spoked wheels with mustard fenders
        with s.at((0.52, y, 0.32), rot):
            wheel(s, 0.32, 0.11, seg=28)
        with s.at((0, 0, 0), FRONT):
            fz = (0.395, 0.54) if y < 0 else (-0.54, -0.395)
            s.extrude(ccw(thicken(arc(0.52, 0.32, 0.37, 10, 170, 18), 0.05)), *fz, pal.Enamel_Mustard,
                      smooth_sides=True)
    for y in (-0.25, 0.25):  # swivel casters
        with s.at((-0.74, y, 0)):
            s.lathe([(0, 0.26), (0.06, 0.26), (0.06, 0.285), (0, 0.285)], pal.Chrome, seg=16)
            s.lathe([(0.014, 0.2), (0.014, 0.27)], pal.Chrome, seg=10)
            for sy in (-1, 1):
                s.box((-0.03, sy * 0.032 - 0.005, 0.07), (0.05, sy * 0.032 + 0.005, 0.22), pal.Chrome)
            s.box((-0.03, -0.037, 0.19), (0.05, 0.037, 0.22), pal.Chrome)
            with s.at((0.02, 0, 0.075), FRONT):
                wheel(s, 0.075, 0.045, spokes=0, seg=16)
    s.sweep(fillet([(-0.93, -0.26, 0.9), (-1.1, -0.26, 0.9), (-1.1, 0.26, 0.9), (-0.93, 0.26, 0.9)], 0.07, n=6,
                   closed=False), 0.018, pal.Chrome, seg=10)  # push handle
