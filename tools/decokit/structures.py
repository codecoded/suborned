"""Deco Kit structures (reference sheet rows 1-2): transit pavilion, curved streamline kiosk, tram canopy,
fluted column, bus shelter. Civic language of the plaza reference: ivory enamel masses, teal / red fins,
chrome hoops and trims wherever two parts meet, seafoam glass over a dark teal interior."""

import math

import bmesh
from mathutils import Vector

from geo import arc, dome, fillet, ring, rrect
from kit import asset
from mats import pal, register
from signals import puck
from vehicles_parts import lamp, mesh

register("DK_Timber", "stone", "#B48C5E")  # as alleys_props.py (register is first-wins)

FRONT = (90, 0, 0)  # local XY -> world XZ (elevation drawing), extrusion -> world -Y


# ----------------------------------------------------------------- local helpers

def tube(s, path, sec, mat, up=(0, 0, 1), closed=False, scale=None, caps=True):
    """Sweep a CCW 2D section along a 3D polyline. Section a-axis = up x tangent (inward on a CCW plan
    loop when up = Z), b-axis = up; `up` stays fixed (planar paths); `scale` = per-point (sa, sb)."""
    P = [Vector(p) for p in path]
    n, m, U = len(P), len(sec), Vector(up)
    verts = []
    for i, p in enumerate(P):
        a, b = (P[i - 1], P[(i + 1) % n]) if closed else (P[max(i - 1, 0)], P[min(i + 1, n - 1)])
        t = (b - a).normalized()
        side = U.cross(t).normalized()
        vert = t.cross(side)
        sa, sb = scale[i] if scale else (1.0, 1.0)
        verts += [tuple(p + side * (u * sa) + vert * (v * sb)) for u, v in sec]
    faces = [[i * m + k, i * m + (k + 1) % m, (i + 1) % n * m + (k + 1) % m, (i + 1) % n * m + k]
             for i in range(n if closed else n - 1) for k in range(m)]
    if caps and not closed:
        faces += [list(range(m))[::-1], list(range((n - 1) * m, n * m))]
    return s._add(verts, faces, mat, True)


def sec(a0, a1, b0, b1, r=0.0, n=4):
    """Rectangular section (a0..a1, b0..b1), optionally with rounded corners."""
    q = [(a0, b0), (a1, b0), (a1, b1), (a0, b1)]
    return fillet(q, r, n) if r else q


def superell(ha, hb, p=3.0, n=24, ca=0.0, cb=0.0):
    """Superellipse (rounded-box) section, CCW."""
    out = []
    for k in range(n):
        t = 2 * math.pi * k / n
        c, sn = math.cos(t), math.sin(t)
        out.append((ca + ha * math.copysign(abs(c) ** (2 / p), c), cb + hb * math.copysign(abs(sn) ** (2 / p), sn)))
    return out


def ccw(poly):
    area = sum(x0 * y1 - x1 * y0 for (x0, y0), (x1, y1) in zip(poly, poly[1:] + poly[:1]))
    return poly if area > 0 else poly[::-1]


def xz(pts, y=0.0):
    """Elevation (x, z) points -> 3D at depth y."""
    return [(x, y, z) for x, z in pts]


def loop(pts):
    return list(pts) + [pts[0]]


def along(path, step, margin=0.0):
    """Evenly spaced (point, tangent) stations along a 2D polyline."""
    P = [Vector(p) for p in path]
    L = [0.0]
    for a, b in zip(P, P[1:]):
        L.append(L[-1] + (b - a).length)
    k = max(1, round((L[-1] - 2 * margin) / step))
    out = []
    for j in range(k + 1):
        d = margin + (L[-1] - 2 * margin) * j / k
        i = next(i for i in range(len(L) - 1) if L[i + 1] >= d - 1e-9)
        f = (d - L[i]) / max(L[i + 1] - L[i], 1e-9)
        out.append((P[i].lerp(P[i + 1], f), (P[i + 1] - P[i]).normalized()))
    return out


def arch(w, spring, z0=0.0, n=20):
    """Round-headed opening outline, CCW, feet at z0."""
    r = w / 2
    return [(r, z0)] + arc(0, spring, r, 0, 180, n) + [(-r, z0)]


def arch_band(ri, ro, spring, z0=0.0, n=20):
    """U-shaped band between two concentric round-headed arches, CCW."""
    return ([(ro, z0)] + arc(0, spring, ro, 0, 180, n) + [(-ro, z0), (-ri, z0)] + arc(0, spring, ri, 180, 0, n)
            + [(ri, z0)])


def soft_extrude(s, poly, z0, z1, mat, r, seg=3):
    """extrude() whose z1 cap edges are rounded (bmesh bevel, radius r): the soft enamel nosing."""
    before = set(s.bm.faces)
    bv = s.extrude(poly, z0, z1, mat, smooth_sides=True)
    top = set(bv[len(poly):])
    edges = list({e for v in top for e in v.link_edges if e.other_vert(v) in top})
    bmesh.ops.bevel(s.bm, geom=edges, offset=r, segments=seg, profile=0.5, affect="EDGES", clamp_overlap=True)
    mi = s._mi(mat)
    for f in set(s.bm.faces) - before:
        f.material_index, f.smooth = mi, True


def crescent(sec, i0, i1, k_out, k_in):
    """Closed partial shell over section points i0..i1 (scaled out, then back scaled in): trim caps."""
    part = sec[i0:i1]
    return ccw([(a * k_out, b * k_out) for a, b in part] + [(a * k_in, b * k_in) for a, b in part][::-1])


def box2(s, a, b, mat, r=0.0):
    """Box from two corners in any order (rounded when r > 0)."""
    mn = tuple(min(p, q) for p, q in zip(a, b))
    mx = tuple(max(p, q) for p, q in zip(a, b))
    return s.rbox(mn, mx, r, mat) if r else s.box(mn, mx, mat)


# ----------------------------------------------------------------- DecoPavilion

PW = 3.7  # pavilion half-width (side-wing faces)


def pav_window(s, sx, y):
    """Tall side-wing window on the x = sx * PW face: teal interior, glass, chrome frame."""
    x = sx * PW
    box2(s, (x - 0.02 * sx, y - 0.25, 0.85), (x + 0.02 * sx, y + 0.25, 2.85), pal.Enamel_Teal)
    box2(s, (x + 0.035 * sx, y - 0.25, 0.85), (x + 0.045 * sx, y + 0.25, 2.85), pal.Glass)
    fr = rrect(0.52, 2.02, 0.12, n=5, cx=y, cy=1.85)
    s.sweep([(x + 0.04 * sx, u, v) for u, v in loop(fr)], 0.03, pal.Chrome, seg=8, caps=False)


def pav_pylon(s, YF):
    """Round-topped ivory pylon: red fin in a chrome hoop, chrome bullnose trims and finial, teal drum at its foot."""
    with s.at((0, YF - 0.35, 0), FRONT):
        soft_extrude(s, arch(0.86, 4.1, 0.4, n=12), -1.35, 0, pal.Enamel_Ivory, 0.1)
        soft_extrude(s, arch(0.3, 3.95, 1.0, n=8), 0.0, 0.2, pal.Enamel_Red, 0.06)
    s.sweep(xz(arch(0.44, 3.95, 1.0, n=10), YF - 0.45), 0.04, pal.Chrome, seg=10)
    for y in (YF - 0.38, YF + 1.0):  # chrome bullnose over the round top, front and back
        s.sweep(xz(arch(0.86, 4.1, 3.9, n=14), y), 0.055, pal.Chrome, seg=10)
    with s.at((0, YF + 0.32, 0)):
        s.lathe([(0.07, 4.45), (0.07, 4.66), (0.1, 4.7)] + dome(0.1, 4.7, 0.1, n=6)[1:], pal.Chrome, seg=16)
    s.extrude(rrect(1.2, 1.7, 0.52, n=8, cy=YF + 0.3), 0.03, 0.88, pal.Enamel_Teal, smooth_sides=True)
    s.extrude(rrect(1.26, 1.76, 0.55, n=8, cy=YF + 0.3), 0.88, 0.95, pal.Chrome, smooth_sides=True)
    s.extrude(rrect(1.16, 1.66, 0.5, n=8, cy=YF + 0.3), 0.0, 0.03, pal.Chrome, smooth_sides=True)


@asset("DecoPavilion", "Structures", bevel=0.014)
def deco_pavilion(s):
    YF, YB, RI, RO, SP = -2.5, -1.2, 1.35, 2.0, 2.6  # screen front/back, arch inner/outer half-width, spring
    # rear body = the glazed side wings, on a teal plinth band with a chrome bead, seafoam coping
    D, CY = 5.0, 0.9  # body depth and centre: y from -1.6 to 3.4
    s.extrude(rrect(2 * PW, D, 0.6, n=8, cy=CY), 0.0, 3.55, pal.Enamel_Ivory, smooth_sides=True)
    s.extrude(rrect(2 * PW + 0.14, D + 0.14, 0.67, n=8, cy=CY), 0.03, 0.46, pal.Enamel_Teal, smooth_sides=True)
    s.extrude(rrect(2 * PW + 0.18, D + 0.18, 0.69, n=8, cy=CY), 0.46, 0.52, pal.Chrome, smooth_sides=True)
    s.extrude(rrect(2 * PW + 0.1, D + 0.1, 0.62, n=8, cy=CY), 0.0, 0.03, pal.Chrome, smooth_sides=True)
    tube(s, [(x, y, 3.55) for x, y in rrect(2 * PW, D, 0.6, n=8, cy=CY)], sec(-0.07, 0.35, -0.02, 0.16, 0.06),
         pal.Enamel_Seafoam, closed=True)
    for sx in (-1, 1):
        for k in range(4):
            pav_window(s, sx, CY + 0.85 * (k - 1.5))
    # stepped masses behind the screen: the roofline falls in soft steps toward the wings
    for hw, y1, z1, cap in ((2.95, 2.8, 4.2, 1), (2.95, 1.4, 4.85, 1), (1.95, 0.2, 6.1, 0), (1.2, -0.5, 6.9, 0)):
        s.rbox((-hw, YB - 0.2, 3.6), (hw, y1, z1), 0.42, pal.Enamel_Ivory)
        if cap:
            tube(s, [(x, y, z1 - 0.1) for x, y in rrect(2 * hw - 0.62, y1 - YB - 0.4, 0.25, n=4, cy=(y1 + YB) / 2)],
                 sec(-0.05, 0.22, 0.0, 0.14, 0.05), pal.Enamel_Seafoam, closed=True)
    # facade screen: slim crown, stepped rounded shoulders, pierced by the round-headed opening
    top = fillet([(2.95, 0), (2.95, 5.2), (1.95, 5.2), (1.95, 6.45), (1.2, 6.45), (1.2, 7.05), (0.62, 7.05),
                  (0.62, 7.4), (-0.62, 7.4), (-0.62, 7.05), (-1.2, 7.05), (-1.2, 6.45), (-1.95, 6.45), (-1.95, 5.2),
                  (-2.95, 5.2), (-2.95, 0)], 0.28, n=6, closed=False)
    screen = top + [(-RO, 0)] + arc(0, SP, RO, 180, 0, 24) + [(RO, 0)]
    with s.at((0, YF, 0), FRONT):
        soft_extrude(s, screen, -(YB - YF), 0, pal.Enamel_Ivory, 0.12)
        soft_extrude(s, fillet([(1.2, 4.75), (1.2, 7.05), (-1.2, 7.05), (-1.2, 4.75)], 0.22, n=5), -0.1, 0.14,
                     pal.Enamel_Ivory, 0.08)
        soft_extrude(s, fillet([(0.62, 4.8), (0.62, 7.4), (-0.62, 7.4), (-0.62, 4.8)], 0.26, n=6), -0.1, 0.3,
                     pal.Enamel_Ivory, 0.08)
    # teal arch surround lining the reveal, chrome beads on both edges
    with s.at((0, YF - 0.14, 0), FRONT):
        soft_extrude(s, arch_band(RI, RO, SP, n=24), -(YB - YF + 0.14), 0, pal.Enamel_Teal, 0.07)
    for r in (RI + 0.01, RO - 0.01):
        s.sweep(xz(arch(2 * r, SP, 0.5, n=24), YF - 0.14), 0.045, pal.Chrome, seg=10)
    # glazed entrance: transom arch, mullions, double doors, dark teal vestibule close behind
    YG, TR = -1.8, 2.4
    with s.at((0, YG, 0), FRONT):
        s.extrude(arch(2 * RI, SP, 0.18), -0.01, 0.01, pal.Glass)
        s.extrude(arch(2 * RI, SP, 0.0), -0.09, -0.05, pal.Enamel_Teal)
        s.rbox((-RI, TR - 0.06, -0.05), (RI, TR + 0.06, 0.05), 0.02, pal.Chrome)
        for x in (-0.78, 0.78):
            s.rbox((x - 0.045, 0.18, -0.05), (x + 0.045, SP + math.sqrt(RI * RI - x * x), 0.05), 0.02, pal.Chrome)
        s.rbox((-0.03, TR, -0.04), (0.03, SP + 0.78, 0.04), 0.012, pal.Chrome)
        for x in (-1.07, 1.07):
            s.rbox((x - 0.02, 1.3, -0.03), (x + 0.02, TR, 0.03), 0.008, pal.Chrome)
        for sx in (-1, 1):
            leaf = rrect(0.72, TR - 0.26, 0.05, n=3, cx=sx * 0.39, cy=(TR + 0.22) / 2)
            s.sweep([(x, z, 0.03) for x, z in loop(leaf)], 0.032, pal.Chrome, seg=8, caps=False)
            s.rbox((sx * 0.39 - 0.32, 0.24, -0.02), (sx * 0.39 + 0.32, 0.5, 0.045), 0.015, pal.Chrome)  # kick plate
            s.sweep([(sx * 0.1, 0.95, 0.11), (sx * 0.1, 1.7, 0.11)], 0.024, pal.Chrome, seg=8)  # push bar
            for z in (0.97, 1.68):
                s.sweep([(sx * 0.1, z, 0.11), (sx * 0.1, z, 0.02)], 0.015, pal.Chrome, seg=6)
    s.sweep(xz(arc(0, SP, 0.78, 0, 180, 14), YG - 0.04), 0.035, pal.Chrome, seg=8)
    s.sweep(xz(arch(2 * RI, SP, 0.18, n=24), YG - 0.04), 0.05, pal.Chrome, seg=10)
    # front step and teal plinth under the screen
    s.extrude(rrect(4.3, 1.0, 0.28, n=6, cy=YF - 0.5), 0.0, 0.18, pal.Limestone_Tex, smooth_sides=True)
    for sx in (-1, 1):
        box2(s, (sx * RO, YF - 0.12, 0.0), (sx * 2.9, YB, 0.46), pal.Enamel_Teal, 0.03)
        box2(s, (sx * RO, YF - 0.14, 0.46), (sx * 2.9, YB, 0.52), pal.Chrome, 0.02)
    # crown: red fin inside a chrome hoop, teal portholes on the shoulders
    with s.at((0, YF - 0.3, 0), FRONT):
        soft_extrude(s, arch(0.34, 6.9, 4.95, n=8), 0.0, 0.2, pal.Enamel_Red, 0.06)
    s.sweep(xz(arch(0.6, 7.02, 4.95, n=12), YF - 0.38), 0.045, pal.Chrome, seg=10)
    for sx in (-1, 1):
        with s.at((sx * 0.91, YF - 0.14, 6.62), FRONT):
            s.lathe([(0, 0), (0.13, 0), (0.13, 0.03), (0.1, 0.06), (0, 0.07)], pal.Chrome, seg=20)
            s.lathe([(0, 0.05), (0.095, 0.05), (0.085, 0.08), (0, 0.095)], pal.Enamel_Seafoam, seg=20)
        with s.at((sx * (PW - 0.44), 0, 0)):
            pav_pylon(s, YF)


# ----------------------------------------------------------------- CurvedKiosk

KX0, KY0, KR, KYB, KXT = -2.8, -2.0, 2.0, 2.0, 1.4  # left face x, front face y, corner radius, back y, tower x


def kpath(d=0.0, n=18):
    """Kiosk facade plan (back-left -> quarter-round corner -> front, into the tower), offset outward by d."""
    return [(KX0 - d, KYB)] + arc(KX0 + KR, KY0 + KR, KR + d, 180, 270, n) + [(KXT, KY0 - d)]


@asset("CurvedKiosk", "Structures", bevel=0.012)
def curved_kiosk(s):
    with s.at((-0.08, 0.18, 0)):  # centre the footprint on the pivot
        line = [(x, y, 0.0) for x, y in kpath()]
        s.extrude(kpath(-0.42) + [(KXT, KYB)], 0.0, 4.8, pal.Enamel_Ivory)
        layers = ((sec(-0.08, 0.5, 0.0, 0.3, 0.05), pal.Chrome),           # plinth
                  (sec(-0.16, 0.5, 0.3, 0.88, 0.14), pal.Enamel_Ivory),    # rounded sill band
                  (sec(0.06, 0.075, 0.88, 3.1), pal.Glass),                # wraparound glazing
                  (sec(0.3, 0.43, 0.88, 3.1), pal.Enamel_Teal),            # interior behind the glass
                  (sec(-0.04, 0.12, 0.85, 0.91, 0.02), pal.Chrome),        # sill rail
                  (sec(-0.04, 0.12, 3.07, 3.13, 0.02), pal.Chrome),        # head rail
                  (sec(-0.06, 0.5, 3.13, 3.2, 0.02), pal.Enamel_Ivory),    # band recess lip
                  (sec(0.07, 0.5, 3.16, 3.9), pal.Steel),                  # speed-band recess
                  (sec(-0.06, 0.5, 3.88, 4.15, 0.03), pal.Enamel_Ivory),   # fascia
                  (sec(-0.09, 0.3, 4.13, 4.43, 0.03), pal.Enamel_Royal),   # royal-blue fascia band
                  (sec(-0.09, 0.85, 4.43, 5.05, 0.25), pal.Enamel_Ivory),  # rounded parapet
                  (sec(1.1, 1.5, 4.7, 5.5, 0.14), pal.Enamel_Ivory),       # set-back upper parapet
                  (sec(1.04, 1.56, 5.46, 5.64, 0.08), pal.Enamel_Royal))   # its blue cap
        for sc, mat in layers:
            tube(s, line, sc, mat)
        for k in range(5):  # chrome speed bands
            s.sweep([(x, y, 3.27 + 0.135 * k) for x, y in kpath(-0.02)], 0.043, pal.Chrome, seg=10)
        for p, t in along(kpath(), 1.0, margin=0.5):  # chrome mullions square to the curve
            with s.at((p.x, p.y, 0), (0, 0, math.degrees(math.atan2(t.y, t.x)))):
                s.rbox((-0.035, -0.04, 0.88), (0.035, 0.11, 3.1), 0.014, pal.Chrome)
        # corner tower: blue-capped block, chrome strip, mustard fin, pier with a powder-blue fin
        s.rbox((KXT - 0.1, KY0 - 0.26, 0.0), (3.12, KYB, 0.3), 0.05, pal.Chrome)
        s.rbox((KXT - 0.05, KY0 - 0.16, 0.25), (2.1, KYB, 6.1), 0.1, pal.Enamel_Ivory)
        s.rbox((KXT - 0.1, KY0 - 0.22, 6.02), (2.15, KYB, 6.5), 0.1, pal.Enamel_Royal)
        s.rbox((KXT - 0.1, KY0 - 0.22, 4.9), (KXT + 0.02, KYB, 6.1), 0.03, pal.Enamel_Royal)
        s.rbox((2.0, KY0 - 0.28, 0.3), (2.12, KY0 - 0.1, 6.02), 0.03, pal.Chrome)
        s.rbox((2.1, KY0 - 0.32, 0.3), (2.75, KYB - 0.1, 6.15), 0.14, pal.Enamel_Mustard)
        s.rbox((2.73, KY0 - 0.26, 0.25), (3.1, KYB, 5.65), 0.1, pal.Enamel_Ivory)
        s.rbox((2.8, KY0 - 0.36, 1.4), (3.03, KY0 - 0.2, 4.9), 0.05, pal.Enamel_Powder)


# ----------------------------------------------------------------- TramCanopy

def foot(s, x, z1, r):
    """Chrome foot with a teal collar between chrome rings (canopy legs)."""
    with s.at((x, 0, 0)):
        s.lathe([(0, 0), (r + 0.09, 0), (r + 0.09, 0.05), (r + 0.06, 0.08), (0, 0.08)], pal.Chrome, seg=32)
        s.lathe(ring(r - 0.02, r, 0.08, z1 - 0.06), pal.Enamel_Teal, seg=32, cap_bottom=False, cap_top=False)
        for z in (0.1, z1 - 0.06):
            s.lathe(ring(r - 0.03, r + 0.03, z - 0.035, z + 0.035), pal.Chrome, seg=32, cap_bottom=False, cap_top=False)


@asset("TramCanopy", "Structures", bevel=0.01)
def tram_canopy(s):
    with s.at((-0.13, 0, 0)):  # centre the footprint on the pivot
        Z = 3.0  # roof frame centre line
        plan = (arc(2.0, -0.55, 0.5, 270, 360, 6)[:-1] + arc(2.0, 0.55, 0.5, 0, 90, 6)[:-1]
                + arc(-1.95, 0, 1.05, 90, 270, 22)[:-1])
        tube(s, [(x, y, Z) for x, y in plan], superell(0.16, 0.12, p=2.0, n=20), pal.Chrome, closed=True)
        glass = arc(2.0, -0.55, 0.4, 270, 360, 4) + arc(2.0, 0.55, 0.4, 0, 90, 4) + arc(-1.95, 0, 0.95, 90, 270, 18)
        s.loft([([(x * 0.86, y * 0.7) for x, y in glass], Z - 0.1), (glass, Z - 0.03), (glass, Z - 0.01)], pal.Glass)
        s.sweep([(x, y, Z - 0.03) for x, y in loop(glass)], 0.035, pal.Chrome, seg=8, caps=False)
        shell = crescent(superell(0.172, 0.132, p=2.6, n=32), 5, 16, 1.0, 0.86)
        tube(s, [(2.3, 1.1, Z)] + [(x, y, Z) for x, y in arc(-1.95, 0, 1.05, 90, 160, 10)], shell, pal.Enamel_Red)
        # right member: the roof end turns down into a chrome blade leg (red along its back/top)
        bend = [(1.4, 0, Z), (1.8, 0, Z), (2.2, 0, Z)] + xz(arc(2.2, Z - 0.8, 0.8, 80, 0, 8))
        bend += [(3.0, 0, z) for z in (1.8, 1.4, 1.0, 0.7)]
        k = len(bend)
        sc = []
        for p in bend:
            f = min(1.0, max(0.0, (p[0] - 1.9) / 1.1)) if p[2] > Z - 0.85 else 1.0
            g = 0.0 if p[2] > 2.0 else min(1.0, (2.0 - p[2]) / 1.3)
            sc.append((0.16 + 0.14 * f - 0.02 * g, 1.2 - 0.72 * f - 0.1 * g))
        blade = superell(1.0, 1.0, p=2.8, n=32)
        tube(s, bend, blade, pal.Chrome, up=(0, 1, 0), scale=sc)
        tube(s, bend[:k - 3], crescent(blade, 10, 19, 1.04, 0.96), pal.Enamel_Red, up=(0, 1, 0), scale=sc[:k - 3])
        foot(s, 3.0, 0.72, 0.34)
        # left leg: collared post flaring into a Y that grips both rails
        with s.at((-1.35, 0, 0)):
            foot(s, 0.0, 0.5, 0.25)
            s.lathe([(0.16, 0.48), (0.16, 1.0)], pal.Chrome, seg=24, cap_bottom=False, cap_top=False)
            s.lathe(ring(0.2, 0.26, 1.0, 1.42), pal.Enamel_Teal, seg=28, cap_bottom=False, cap_top=False)
            for z in (1.0, 1.42):
                s.lathe(ring(0.18, 0.28, z - 0.035, z + 0.035), pal.Chrome, seg=28, cap_bottom=False, cap_top=False)
            s.lathe([(0.16, 1.4), (0.16, 1.85), (0.2, 2.2), (0.3, 2.5), (0.42, 2.72), (0.46, 2.8), (0.0, 2.84)],
                    pal.Chrome, seg=28)
        for sy in (-1, 1):
            arm = fillet([(-1.35, 0, 1.8), (-1.45, 0, 2.3), (-1.95, sy * 0.66, 2.84), (-2.5, sy * 0.89, Z)], 0.34, n=8,
                         closed=False)
            s.sweep(arm, [0.2 - 0.08 * i / (len(arm) - 1) for i in range(len(arm))], pal.Chrome, seg=16)


# ----------------------------------------------------------------- FlutedColumn

@asset("FlutedColumn", "Structures", bevel=0.006)
def fluted_column(s):
    R, N, Z0, Z1, DP = 0.46, 12, 0.84, 3.3, 0.045
    s.lathe([(0, 0), (0.64, 0), (0.64, 0.12), (0.61, 0.16), (0.58, 0.17), (0.58, 0.29), (0.55, 0.32), (0.53, 0.33),
             (0.53, 0.47), (0.5, 0.52), (0.49, 0.62), (0.47, 0.66), (0.47, 0.72), (0, 0.72)], pal.Chrome, seg=48)
    s.lathe([(0.47, 0.7), (0.47, 0.74), (R, 0.76), (R, Z0), (0, Z0)], pal.Enamel_Ivory, seg=48, cap_bottom=False)
    s.lathe([(0, Z1), (R, Z1), (R, 3.44), (0, 3.44)], pal.Enamel_Ivory, seg=48)
    af = 0.48 * math.pi / N  # flute half-angle
    h = R * math.sin(af)
    rc = (h * h + DP * DP) / (2 * DP)
    b0 = math.asin(h / rc)
    star, flutes = [], []
    for i in range(N):
        ph = 2 * math.pi * i / N
        u, v = Vector((math.cos(ph), math.sin(ph))), Vector((-math.sin(ph), math.cos(ph)))
        c = u * (R * math.cos(af) - DP + rc)

        def groove(rr, bb, c=c, u=u, v=v):
            return [tuple(c + rr * (-math.cos(b) * u + math.sin(b) * v)) for b in bb]
        star += groove(rc, [b0 * (k / 4 - 1) for k in range(9)])
        star += [(R * math.cos(ph + af + (2 * math.pi / N - 2 * af) * k / 3),
                  R * math.sin(ph + af + (2 * math.pi / N - 2 * af) * k / 3)) for k in range(1, 3)]
        flutes.append(groove)
    s.extrude(star, Z0 - 0.01, Z1 + 0.01, pal.Enamel_Ivory, smooth_sides=True, top=False, bottom=False)
    for groove in flutes:  # teal lining in each channel, tapered to rounded ends
        secs = []
        for z, w in ((Z0 + 0.03, 0.25), (Z0 + 0.06, 0.7), (Z0 + 0.11, 0.92), (Z1 - 0.11, 0.92), (Z1 - 0.06, 0.7),
                     (Z1 - 0.03, 0.25)):
            bb = [b0 * w * (k / 3 - 1) for k in range(7)]
            secs.append((ccw(groove(rc - 0.001, bb) + groove(rc - 0.007, bb)[::-1]), z))
        s.loft(secs, pal.Enamel_Teal)
    s.lathe(ring(R - 0.01, R + 0.025, 3.43, 3.47), pal.Chrome, seg=48, cap_bottom=False, cap_top=False)
    s.lathe(loop(superell(0.075, 0.125, p=2.4, n=20, ca=0.485, cb=3.6)), pal.Enamel_Red, seg=48, cap_bottom=False,
            cap_top=False)
    s.lathe([(0.0, 3.62), (0.4, 3.62), (0.4, 3.73), (0.39, 3.78), (0.36, 3.82), (0.3, 3.84), (0.0, 3.85)],
            pal.Chrome, seg=40)


# ----------------------------------------------------------------- BusShelter (props sheet, shelters #3)

SH_X, SH_Y, SH_Z, SH_A, SH_B, SH_T = 2.3, 1.3, 2.72, 0.16, 0.26, 0.07  # roof plan half-sizes, saddle, thickness


def zs(x, y):
    """Saddle roof top: rises to the two ends, dips over the open front and the glazed back."""
    return SH_Z + SH_A * x * x - SH_B * y * y


def saddle(s, mat):
    """Hyperbolic-paraboloid shell over a superelliptic plan: top, soffit and edge band as one closed skin."""
    ring = superell(SH_X, SH_Y, p=2.6, n=64)
    K, n = 10, len(ring)
    top = [(0.0, 0.0, zs(0, 0))]
    for k in range(1, K + 1):
        t = k / K
        top += [(t * x, t * y, zs(t * x, t * y)) for x, y in ring]
    bot = [(x, y, z - SH_T) for x, y, z in top]
    verts, m = top + bot, len(top)

    def rv(k, j):  # ring k (1..K), point j
        return 1 + (k - 1) * n + j % n
    faces = [[0, rv(1, j), rv(1, j + 1)] for j in range(n)]
    faces += [[rv(k, j), rv(k + 1, j), rv(k + 1, j + 1), rv(k, j + 1)] for k in range(1, K) for j in range(n)]
    faces += [[m + i for i in f[::-1]] for f in list(faces)]
    faces += [[rv(K, j), m + rv(K, j), m + rv(K, j + 1), rv(K, j + 1)] for j in range(n)]
    s._add(verts, faces, mat, True)
    edge = [(x, y, zs(x, y) - SH_T / 2) for x, y in ring]
    s.sweep(edge + edge[:1], 0.048, pal.Chrome, seg=12)


def trumpet(s, cx, cy, w0, d0, w1, d1, shift, mat, rows=14):
    """Column flaring into the soffit: rounded-rect rings, the top ring pressed onto the curved roof."""
    out = []
    for i in range(rows + 1):
        t = i / rows
        e = t ** 3.2  # slender to two thirds of the height, then the flare
        w, d = w0 + (w1 - w0) * e, d0 + (d1 - d0) * e
        ox = cx - shift * e
        pts = rrect(w, d, min(w, d) * 0.32, 5)
        z_top = lambda x, y: zs(x, y) - SH_T / 2  # noqa: E731
        z = 2.6 * t
        out.append([(ox + x, cy + y, min(z, z_top(ox + x, cy + y)) if i < rows else z_top(ox + x, cy + y))
                    for x, y in pts])
    mesh(s, out, mat, caps=((0, None),))
    return out


@asset("BusShelter", "Structures", bevel=0.01)
def bus_shelter(s):
    """Hyperbolic saddle roof sweeping up at both ends, chrome-rimmed; one end grows out of a flared cream
    column holding the map case, the other rides a chrome V-strut; a glazed back and a timber bench."""
    saddle(s, pal.Enamel_Ivory)
    col = trumpet(s, -1.35, 0.36, 0.34, 0.4, 1.0, 0.72, 0.2, pal.Enamel_Ivory)
    s.sweep([max(r, key=lambda v: v[0] - abs(v[1] - 0.36)) for r in col[:-2]], 0.012, pal.Chrome, seg=8)  # inner rib
    with s.at((-1.19, 0.36, 0.95), (90, 0, 90)):  # map case on the column's inner face (+X)
        s.rbox((-0.12, -0.3, -0.01), (0.12, 0.3, 0.03), 0.02, pal.Chrome)
        s.decal([(-0.1, -0.27, 0.031), (0.1, -0.27, 0.031), (0.1, 0.27, 0.031), (-0.1, 0.27, 0.031)],
                pal.Lightbox_Travel)
    foot, tops = (1.1, -0.72, 0.0), ((0.15, -0.95), (2.0, -0.55))  # the V spreads across the open front
    for x, y in tops:  # the V: two tapered chrome struts from one foot
        z = zs(x, y) - SH_T + 0.01
        s.sweep([foot, (foot[0] + (x - foot[0]) * 0.5, foot[1] + (y - foot[1]) * 0.5, z * 0.5), (x, y, z)],
                [0.05, 0.058, 0.075], pal.Chrome, seg=16)
        lamp(s, (x + 0.05 * (1 if x > foot[0] else -1), y, z - 0.03), (0, 0, -1), 0.045, depth=0.025, seg=16)
    with s.at(foot):
        s.lathe(puck(0.1, 0.0, 0.05, 0.02), pal.Chrome, seg=24)
    lamp(s, (-0.95, 0.1, zs(-0.95, 0.1) - SH_T - 0.01), (0, 0, -1), 0.045, depth=0.025, seg=16)
    # glazed back wall (lower than the soffit, as drawn), chrome rails and mullions
    XL, XR, YB, Z0, Z1 = -1.16, 1.15, 0.64, 0.26, 1.95
    for z0, z1 in ((Z0 - 0.06, Z0), (Z1, Z1 + 0.06)):
        box2(s, (XL, YB - 0.03, z0), (XR, YB + 0.03, z1), pal.Chrome, 0.012)
    box2(s, (XL, YB - 0.008, Z0), (XR, YB + 0.008, Z1), pal.Glass)
    for x in (XR, (XL + XR) / 2):
        box2(s, (x - 0.03, YB - 0.035, Z0 - 0.2), (x + 0.03, YB + 0.035, Z1 + 0.06), pal.Chrome, 0.01)
    # timber bench on sculpted seafoam supports
    for x in (-0.95, 0.0, 0.9):
        with s.at((x, 0, 0), (90, 0, 90)):  # SIDE: local (y, z) -> world (y, z), extruded along X
            leg = fillet([(0.14, 0.0), (0.56, 0.0), (0.56, 0.9), (0.49, 0.9), (0.47, 0.45), (0.1, 0.45), (0.1, 0.37),
                          (0.44, 0.35), (0.46, 0.07), (0.14, 0.07)], 0.03, n=3)
            s.extrude(leg, -0.035, 0.035, pal.Enamel_Seafoam, smooth_sides=True)
    for k in range(4):  # seat slats
        y = 0.12 + 0.093 * k
        s.rbox((-1.06, y, 0.45), (1.02, y + 0.08, 0.485), 0.012, pal.Timber)
    for k in range(3):  # back slats against the uprights
        z = 0.58 + 0.11 * k
        s.rbox((-1.06, 0.435, z), (1.02, 0.47, z + 0.08), 0.012, pal.Timber)
