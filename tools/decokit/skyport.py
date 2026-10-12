"""Deco Kit skyport family: the harbour city's aircar ports (1950s civic limestone and ivory enamel dressed as docked
spacecraft: plated discs, lift tubes, gantries, pipework, vent banks, antenna crowns). Front -Y, no bevel.
  Skyport_Tower  harbour skyport; z = 0 is the water surface. Caisson drum (R 15) with fenders, deck at the pier level
                 (+1.2); the pier (20 m wide) arrives from -X onto the apron edge at x = -22. 14 m fluted stem, three
                 cantilevered landing discs branching like a tree (PADS: deck tops 38 / 62 / 84 m), glazed gate lounges,
                 lift tubes, service gantry, control cupola, beacon mast to 110.5 m.
  SkyPad_Roof    R 7 rooftop aircar pad on four splayed 6 m legs, deck 7.2 m, ship stair down on +X; pivot = the
                 leg-footprint centre on the roof deck.
skyport_civic.py adds CircusMonument, SkyRailStation and SkyRail_Pylon_Tall (imported at the end, so the
family registers them in priority order)."""

import math

from geo import circle, rrect
from kit import asset
from landmarks import fluted_shaft
from mats import pal
from mega_parts import FRONT, lens, strut, truss
from skyport_parts import (ball, band, bead, dish, face, flight, lift_tube, louvres, lounge, needle, pad, pipes, pol,
                           rail, word)
from structures import sec, superell, tube

RS, ZF, RC = 7.0, 1.2, 15.0  # stem radius, caisson deck (= pier deck), caisson radius
APRON_X, APRON_W = -22.0, 20.0
# numeral, disc radius, deck top, azimuth (deg), centre offset from the stem axis, soffit depth, lounge depth, lift car z
PADS = (("1", 16.0, 38.0, -60.0, 20.0, 4.0, 3.6, 12.0), ("2", 13.0, 62.0, 160.0, 17.0, 3.4, 3.2, 40.0),
        ("3", 10.0, 84.0, 20.0, 14.0, 2.8, 2.8, 70.0))
GANTRY_A, PIPE_A, LIFT_R = 88.0, 232.0, 9.2
FIN_A = (54.0, 124.0, 265.0, 340.0)
BELTS = (45.0, 70.0)  # machinery belt centres  # buttress fins, clear of the tubes, pipes, gantry and entrance


def pad_centre(i):
    """World (x, y, deck top) of PADS[i]: where the lead parks aircars."""
    _, R, zt, az, D, *_ = PADS[i]
    return (*pol(D, az), zt)


# ----------------------------------------------------------------- Skyport_Tower

def caisson(s):
    """Concrete caisson with a tidal band, rubber fenders, iron rubbing band and bollards; limestone deck; the pier apron
    on -X (concave to the drum) with chrome rails, lamp standards and the SKYPORT gateway."""
    s.lathe([(RC + 0.8, -4.0), (RC, -1.2), (RC, ZF - 0.35), (RC - 0.35, ZF)], pal.Concrete, seg=64, cap_bottom=False,
            cap_top=False)
    band(s, RC + 0.01, -1.2, 0.3, pal.Iron, 64)  # tidal band
    s.lathe([(RC - 0.35, ZF), (0.0, ZF)], pal.Limestone_Tex, seg=64, cap_bottom=False, cap_top=False)
    s.lathe([(RC + 0.02, ZF - 0.7), (RC + 0.25, ZF - 0.6), (RC + 0.25, ZF - 0.4), (RC + 0.02, ZF - 0.3)], pal.Iron,
            seg=64, cap_bottom=False, cap_top=False)
    a_ap = math.degrees(math.asin(APRON_W / 2 / RC))
    for k in range(28):
        a = 360 * (k + 0.5) / 28
        if abs(a - 180) < a_ap + 4:
            continue
        with s.at(pol(RC + 0.35, a, 0.0)):
            s.lathe([(0.32, -1.4), (0.32, ZF - 0.5)], pal.Rubber, seg=8, cap_bottom=False)
        if k % 2:
            with s.at(pol(RC - 1.0, a, ZF)):
                s.lathe([(0.22, 0.0), (0.2, 0.55), (0.34, 0.66), (0.0, 0.8)], pal.Iron, seg=8, cap_bottom=False)
    arc_pts = [pol(RC, 180 + a_ap - 2 * a_ap * k / 12) for k in range(13)]
    plan = [(APRON_X, -APRON_W / 2)] + arc_pts + [(APRON_X, APRON_W / 2)]
    s.extrude(plan, -3.0, ZF - 0.3, pal.Concrete)
    s.extrude(plan, ZF - 0.3, ZF, pal.Limestone_Tex, bottom=False)
    x1 = arc_pts[0][0]
    for y in (-APRON_W / 2 + 0.3, APRON_W / 2 - 0.3):
        s.sweep([(APRON_X, y, ZF + 1.05), (x1, y, ZF + 1.05)], 0.05, pal.Chrome, seg=6)
        for k in range(6):
            x = APRON_X + (x1 - APRON_X) * k / 5
            s.box((x - 0.04, y - 0.04, ZF), (x + 0.04, y + 0.04, ZF + 1.05), pal.Chrome)
    # gateway: two teal pylons carrying a lit SKYPORT board over the pier landing
    xg = APRON_X + 1.5
    for y in (-6.0, 6.0):
        s.rbox((xg - 0.6, y - 0.6, ZF), (xg + 0.6, y + 0.6, ZF + 8.0), 0.25, pal.Enamel_Teal)
        with s.at((xg, y, ZF + 8.0)):
            s.lathe([(0.75, 0.0), (0.75, 0.2), (0.3, 0.45), (0.3, 1.6), (0.0, 1.7)], pal.Chrome, seg=16, cap_bottom=False)
        ball(s, (xg, y, ZF + 9.9), 0.3, pal.Lamp, seg=12)
    s.rbox((xg - 0.35, -6.0, ZF + 5.6), (xg + 0.35, 6.0, ZF + 7.4), 0.12, pal.Enamel_Ivory)
    with s.at((xg - 0.36, 0.0, ZF + 6.5), (90, 0, -90)):
        word(s, "SKYPORT", 1.05, 0.16, 0.0, 0.05, pal.Lamp)


def portal(s):
    """Entrance on the pier side (-X): glazed doors in the foot, streamline canopy with a chrome nose, teal pylons."""
    x0 = -10.35
    s.box((x0 - 0.2, -3.2, ZF), (x0 + 0.6, 3.2, ZF + 4.2), pal.Window_Tower)
    for y in (-3.3, -1.1, 1.1, 3.3):
        s.box((x0 - 0.3, y - 0.08, ZF), (x0 + 0.6, y + 0.08, ZF + 4.2), pal.Chrome)
    with s.at((-12.2, 0.0, 0.0)):
        s.extrude(rrect(5.2, 11.0, 2.4, n=6), ZF + 4.5, ZF + 5.1, pal.Enamel_Ivory)
        s.extrude(rrect(5.35, 11.15, 2.45, n=6), ZF + 4.62, ZF + 4.98, pal.Chrome, top=False, bottom=False)
    for y in (-5.0, 5.0):
        s.rbox((-11.4, y - 0.45, ZF), (-10.0, y + 0.45, 11.0), 0.2, pal.Enamel_Teal)
        s.box((-11.45, y - 0.12, ZF + 5.5), (-11.3, y + 0.12, 10.4), pal.Enamel_Coral)


def stem(s, base=None):
    """Flared limestone foot with four gilded buttress fins, 14 m fluted shaft with ivory hoops and a plated machinery
    belt, vent banks, service pipe risers and the three lift tubes on their spines.
    base = None: as built. base = z: a regen order's frame top; the stem starts there (no foot, fins or caisson) and
    everything that stood on the ground starts on the frame."""
    zb0, zr = (ZF, 16.0) if base is None else (base, base + 0.4)
    foot = [(10.5, ZF), (10.5, ZF + 2.6), (9.9, 5.0), (8.8, 8.5), (7.7, 12.5), (RS, 16.0)]
    if base is None:
        s.lathe(foot, pal.Limestone_Tex, seg=48, cap_bottom=False, cap_top=False)
        band(s, 10.52, ZF + 1.0, ZF + 1.8, pal.Enamel_Teal, 48)
        bead(s, 10.5, ZF + 2.0, pal.Gold, 48, 0.05, 0.05)
    for a in FIN_A if base is None else ():
        edge = [(14.3, ZF + 1.6), (13.6, ZF + 3.0)] + [(r + 1.6, z) for r, z in foot[2:]] + [(RS + 0.9, 24.0),
                                                                                              (RS + 0.4, 32.0)]
        with s.at((0, 0, 0), (0, 0, a)), s.at((0, 0, 0), FRONT):
            s.extrude([(8.0, ZF), (14.3, ZF)] + edge + [(RS - 0.3, 32.0)], -0.8, 0.8, pal.Limestone_Tex)
            for e0, e1 in ((0.8, 0.84), (-0.84, -0.8)):
                s.extrude([(12.4, ZF + 0.8), (12.9, ZF + 0.8), (12.2, ZF + 2.6), (10.2, 7.4), (9.0, 12.5), (8.4, 12.5),
                           (9.6, 7.4)], e0, e1, pal.Enamel_Teal)
        tube(s, [pol(r + 0.02, a, z) for r, z in edge], circle(0.13, 6), pal.Gold)
    fluted_shaft(s, RS, zr - 0.4, 92.0, ribs=20)
    for z in (zr - 0.4,):  # ivory hoop over the foot
        s.lathe([(RS, z - 0.4), (RS + 0.75, z - 0.15), (RS + 0.75, z + 0.25), (RS, z + 0.5)], pal.Enamel_Ivory, seg=40,
                cap_bottom=False, cap_top=False)
        bead(s, RS + 0.75, z + 0.05, pal.Chrome, 40, 0.05, 0.04)
    for zb in BELTS:  # plated machinery belts: vent ring between chrome beads, ribbed
        s.lathe([(RS, zb - 2.6), (8.4, zb - 2.0), (8.4, zb + 2.0), (RS, zb + 2.6)], pal.Hull_Ivory, seg=40,
                cap_bottom=False, cap_top=False)
        band(s, 8.42, zb - 1.0, zb + 1.0, pal.Iron, 40)
        for z in (zb - 1.2, zb + 1.2):
            bead(s, 8.4, z, pal.Chrome, 40, 0.06, 0.06)
        for k in range(20):
            with s.at(pol(8.46, 9 + 18 * k, zb - 1.0), face(9 + 18 * k)):
                s.box((-0.1, -0.08, 0.0), (0.1, 0.1, 2.0), pal.Steel_Ivory)
    for a in (99, 135, 243, 279, 315):  # vent banks between the flutes
        with s.at(pol(RS + 0.02, a, 17.5 + (zb0 - ZF if base is not None else 0.0)), face(a)):
            louvres(s, 1.2, 3.2, n=5)
    for a in (99, 135, 189, 243, 279):
        with s.at(pol(RS + 0.02, a, 86.0), face(a)):
            louvres(s, 1.2, 2.4, n=4)
    pipes(s, RS + 0.85, PIPE_A, zb0, 90.0, n=3)
    pipes(s, RS + 0.7, PIPE_A - 14, 12.0 if base is None else zb0 + 1.0, 90.0, n=2, pr=0.18, pitch=0.5, clamp=8.0)
    for num, R, zt, az, D, depth, ld, car in PADS:
        lift_tube(s, *pol(LIFT_R, az), zb0, zt + 3.0, car=max(car, zb0 + 2.0), back=az + 180)
        with s.at(pol(RS + 0.2, az, 0.0), face(az)):  # spine tying the tube to the flutes
            s.box((-0.35, -0.8, 12.0 if base is None else zb0), (0.35, 0.3, zt), pal.Steel_Ivory)


def branch(s, num, R, zt, az, D, depth, ld, seg, zmin=None):
    """One landing disc on its cantilever: a plated web bracket with portholes and two raking hull struts from a stem
    collar to the pendant hub pod, chrome pipes along the struts, the gate lounge where the disc meets the stem."""
    zb = zt - depth
    rr, rl = R - 0.25, RS + ld + 0.5
    phi = math.degrees(math.acos((D * D + rr * rr - rl * rl) / (2 * D * rr)))
    span = math.degrees(math.acos((RS * RS + D * D - R * R) / (2 * RS * D))) + 3.0
    with s.at((*pol(D, az), 0.0)):
        pad(s, R, zt, num, face_to=az + 180, gap=(az + 180 - phi, az + 180 + phi), seg=seg, depth=depth,
            lights=max(16, round(R * 1.6)), approach=round(R / 2.6))
        s.lathe([(3.0, zb + 0.01), (2.2, zb - 1.4), (0.9, zb - 2.6), (0.0, zb - 2.9)], pal.Hull_Ivory, seg=24,
                cap_bottom=False)  # hub pod
        bead(s, 2.6, zb - 0.7, pal.Gold, 24, 0.06, 0.06)
        ball(s, (0.0, 0.0, zb - 2.9), 0.45, pal.Lamp, seg=12)
    z0 = zb - 0.85 * (D - RS)
    if zmin is not None:  # a regen order's frame: nothing hangs below it
        z0 = max(z0, zmin + 1.2)
    for zz in (z0, zt - 0.8):
        s.lathe([(RS, zz - 1.2), (RS + 0.6, zz - 0.9), (RS + 0.6, zz + 0.9), (RS, zz + 1.2)], pal.Steel_Ivory, seg=40,
                cap_bottom=False, cap_top=False)
    # web bracket in the radial plane (x = distance from the stem axis, y = z), its top buried in the disc
    rim = min(1.4, 0.35 * depth)
    s0, s1 = (D - 3.5, zb + 0.35), (D - 0.72 * R, zt - 0.6 - rim)
    top = s1[1] + (s1[1] - s0[1]) / (s0[0] - s1[0]) * (s1[0] - RS + 0.3) + 0.3
    web = [(RS - 0.3, z0 - 1.0), (D - 1.6, zb - 2.3), (D - 2.9, zb + 0.4), (RS - 0.3, min(top, zt - 0.4))]
    with s.at((0, 0, 0), (0, 0, az)), s.at((0, 0, 0), FRONT):
        s.extrude(web, -0.6, 0.6, pal.Hull_Ivory)
        for k in range(4):  # portholes on both faces, along the web's mid-line
            f = 0.15 + 0.2 * k
            lo = [web[0][i] + (web[1][i] - web[0][i]) * f for i in (0, 1)]
            hi = [web[3][i] + (web[2][i] - web[3][i]) * f for i in (0, 1)]
            cx, cy = (lo[0] + hi[0]) / 2, (lo[1] + hi[1]) / 2
            for e0, e1 in ((0.6, 0.66), (-0.66, -0.6)):
                s.extrude(circle(0.42, 8, cx, cy), e0, e1, pal.Window_Tower)
    u, v = pol(1.0, az), pol(1.0, az + 90)
    for sd in (-1, 1):
        a = (u[0] * (RS + 0.3) + v[0] * 2.8 * sd, u[1] * (RS + 0.3) + v[1] * 2.8 * sd, z0)
        b = (u[0] * (D - 1.8) + v[0] * 1.4 * sd, u[1] * (D - 1.8) + v[1] * 1.4 * sd, zb - 0.9)
        tube(s, [a, b], sec(-0.5, 0.5, -0.7, 0.7, 0.3), pal.Hull_Ivory, scale=[(1.25, 1.25), (0.85, 0.85)])
        for p, k in ((a, 1.4), (b, 0.95)):
            q = tuple(pp + (bb - aa) * (0.04 if p is a else -0.06) for pp, aa, bb in zip(p, a, b))
            tube(s, [p, q], sec(-0.5 * k - 0.06, 0.5 * k + 0.06, -0.7 * k - 0.06, 0.7 * k + 0.06), pal.Gold)
        off = (v[0] * 0.95 * sd, v[1] * 0.95 * sd, -0.55)
        s.sweep([tuple(x + o for x, o in zip(a, off)), tuple(x + o for x, o in zip(b, off))], 0.16, pal.Chrome, seg=8)
    lounge(s, RS, az, span, ld, zt)


def gantry(s, z0=ZF):
    """Service gantry on the back (+Y): lattice tower to the gallery collar, catwalks to the stem, a mustard lift cage
    and red obstruction lamps."""
    c = pol(RS + 4.4, GANTRY_A)
    truss(s, (*c, z0), (*c, 90.5), 1.5, 1.5, 22, pal.Steel_Ivory, chord=0.4, lace=0.2)
    for z in range(14 if z0 == ZF else int(z0) + 2, 88, 12):
        p = pol(RS + 2.9, GANTRY_A)
        with s.at((*p, float(z)), face(GANTRY_A)):
            s.box((-0.7, -0.1, -0.12), (0.7, 2.2, 0.0), pal.Steel_Ivory)
            for x in (-0.7, 0.7):
                s.sweep([(x, -0.1, 1.0), (x, 2.2, 1.0)], 0.035, pal.Chrome, seg=6)
    s.box((c[0] - 1.0, c[1] - 1.0, 44.0), (c[0] + 1.0, c[1] + 1.0, 46.6), pal.Enamel_Mustard)
    for z in (24.0, 48.0, 72.0, 90.9):
        for dx, dy in ((-1.5, -1.5), (1.5, 1.5)):
            ball(s, (c[0] + dx, c[1] + dy, z), 0.22, pal.Signal_Red, seg=8, n=4)


def crown(s, tip=None):
    """Observation gallery collar, sloped-glass control cupola with chrome mullions, plated roof, antenna cluster,
    lattice mast and needle with red beacons. tip = a regen order's top: the needle is cut to it (kit 110.5)."""
    lens(s, 89.6, RS, 11.2, 2.4, pal.Hull_Ivory, seg=48)
    band(s, 11.23, 89.6 + 0.3, 89.6 + 0.55, pal.Lamp, 48)  # landing-light row under the gallery glazing
    rail(s, 10.4, 91.75)
    s.lathe([(RS + 0.6, 92.0), (7.9, 92.3)], pal.Hull_Ivory, seg=40, cap_bottom=False, cap_top=False)
    s.lathe([(7.9, 92.3), (9.0, 96.6)], pal.Window_Tower, seg=40, cap_bottom=False, cap_top=False)
    for k in range(20):
        a = 360 * k / 20
        strut(s, pol(7.96, a, 92.3), pol(9.06, a, 96.6), 0.12, pal.Chrome)
    s.lathe([(9.0, 96.6), (9.5, 96.8), (9.5, 97.3), (8.2, 98.1), (5.0, 99.4), (2.4, 99.9)], pal.Hull_Ivory, seg=40,
            cap_bottom=False, cap_top=False)
    band(s, 9.52, 96.9, 97.2, pal.Enamel_Coral, 40)
    s.lathe([(2.4, 99.9), (2.4, 101.2), (1.8, 101.6), (0.0, 101.6)], pal.Steel_Ivory, seg=24, cap_bottom=False)
    truss(s, (0, 0, 101.6), (0, 0, 107.6), 1.1, 0.35, 4, pal.Steel_Ivory, chord=0.24, lace=0.11)
    for z, w in ((103.1, 2.6), (105.4, 1.8)):  # yard arms with beacons
        strut(s, (-w, 0, z), (w, 0, z), 0.14, pal.Steel_Ivory)
        for x in (-w, w):
            ball(s, (x, 0, z + 0.2), 0.2, pal.Signal_Red, seg=8, n=4)
    needle(s, (0, 0, 107.6), 3.2 if tip is None else tip - 107.6, 0.2)
    if tip is None:
        ball(s, (0, 0, 110.5), 0.3, pal.Signal_Red, seg=10, n=5)
    ball(s, (0, 0, 107.7), 0.35 if tip is None else min(0.35, tip - 107.7), pal.Signal_Red, seg=10, n=5)
    for a in (40, 160, 280):
        dish(s, pol(6.2, a, 100.4), 1.5, tilt=55, az=a)
    for a in (100, 220, 340):
        needle(s, pol(6.8, a, 98.5), 6.0, 0.08)
        ball(s, pol(6.8, a, 104.5), 0.12, pal.Signal_Red, seg=6, n=3)
    ball(s, pol(4.0, 0, 100.9), 1.1, pal.Hull_Ivory, seg=16, n=8)  # radome


@asset("Skyport_Tower", "Skyport", bevel=0.0)
def skyport_tower(s):
    caisson(s)
    portal(s)
    stem(s)
    for (num, R, zt, az, D, depth, ld, car), seg in zip(PADS, (64, 56, 48)):
        branch(s, num, R, zt, az, D, depth, ld, seg)
    gantry(s)
    crown(s)


# ----------------------------------------------------------------- SkyPad_Roof

PAD_R, PAD_Z = 7.0, 7.2  # disc radius, deck top above the roof deck


@asset("SkyPad_Roof", "Skyport", bevel=0.0)
def skypad_roof(s):
    zb = PAD_Z - 1.8
    pad(s, PAD_R, PAD_Z, None, gap=(-8.0, 8.0), seg=48, hub=3.2, depth=1.8, lights=16, ribs=8)
    heads = []
    for k in range(4):
        a = 45 + 90 * k
        foot, head = pol(6.4, a, 0.3), pol(3.0, a, zb + 0.1)
        tube(s, [foot, head], superell(0.3, 0.26, p=2.6, n=12), pal.Enamel_Ivory, scale=[(1.2, 1.2), (0.85, 0.85)])
        for f, mat in ((0.0, pal.Chrome), (0.55, pal.Gold)):
            p = [foot[i] + (head[i] - foot[i]) * f for i in range(3)]
            q = [foot[i] + (head[i] - foot[i]) * (f + 0.06) for i in range(3)]
            tube(s, [p, q], superell(0.3 * (1.2 - 0.35 * f) + 0.05, 0.26 * (1.2 - 0.35 * f) + 0.05, p=2.6, n=12), mat)
        with s.at(foot[:2] + (0.0,), face(a)):
            s.box((-0.7, -0.7, 0.0), (0.7, 0.7, 0.3), pal.Limestone_Tex)
            s.box((-0.45, -0.45, 0.3), (0.45, 0.45, 0.36), pal.Chrome)
        heads.append([foot[i] + (head[i] - foot[i]) * 0.45 for i in range(3)])
    for k in range(4):
        strut(s, heads[k], heads[(k + 1) % 4], 0.14, pal.Steel_Ivory)
    with s.at((0, 0, 0), (0, 0, 90)):  # ship stair rising toward the rim on +X (local -Y = world +X)
        flight(s, -0.65, 0.65, -PAD_R - 4.3, -PAD_R + 0.1, 0.0, PAD_Z, 24)
    s.box((PAD_R + 3.9, -0.8, 0.0), (PAD_R + 4.7, 0.8, 0.12), pal.Steel_Ivory)
    # windsock mast and floodlights at the gate
    m = pol(PAD_R - 0.8, 22, PAD_Z)
    needle(s, m, 3.6, 0.07, pal.Steel_Ivory)
    ball(s, (m[0], m[1], m[2] + 3.65), 0.12, pal.Signal_Red, seg=8, n=4)
    with s.at((m[0], m[1], m[2] + 3.3), (0, 90, 30)):
        for k, mat in enumerate((pal.Enamel_Coral, pal.Enamel_Ivory, pal.Enamel_Coral)):
            r0, r1 = 0.32 - 0.07 * k, 0.32 - 0.07 * (k + 1)
            s.lathe([(r0, 0.5 * k), (r1, 0.5 * k + 0.5)], mat, seg=10, cap_bottom=False, cap_top=False)
    for a in (135, 225):
        p = pol(3.3, a, zb - 0.2)
        ball(s, p, 0.22, pal.Lamp, seg=10, n=5)


import skyport_civic  # noqa: F401,E402 - registers CircusMonument, SkyRailStation, SkyRail_Pylon_Tall after these
