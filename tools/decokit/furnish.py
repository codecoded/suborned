"""Furnish family: coin-op machines and civic street pieces. Café, terrace and waterfront pieces live in
furnish_cafe.py, planters in furnish_garden.py (imported at the bottom so `kit.load` registers all three).

Pivot = footprint centre at z = 0, front -Y. Wall-mounted pieces say so in their docstring. Detail budget: loose
faces (furnish_parts.flat / strip / letters / vent) carry the engineered detail without taking the kit's edge
bevel, so a machine reads intricate at 3-4k tris."""

import math

from booths import sub
from geo import circle, dome, fillet, rrect
from kit import asset
from mats import DECAL, pal
from furnish_parts import (FRONT, SIDE_L, SIDE_R, bottle, flat, frame, knob, lamp, letters, starburst, strip,
                           vent)


def rr(w, d, r, n=4, cx=0.0, cy=0.0):
    return rrect(w, d, r, n, cx, cy)


def fstrip(s, x0, z0, x1, z1, y, mat):
    """Loose quad on the plane y facing -Y (front seams, slots)."""
    with s.at((0, y, 0), FRONT):
        strip(s, x0, z0, x1, z1, mat)


# ----------------------------------------------------------------- cola machine

@asset("VendingMachine_Cola", "Furnish")
def vending_cola(s):
    """0.9 x 0.74 x 1.99 m free-standing: red lower cabinet, ivory upper, streamline shoulder into a glass dome of
    bottles; lit COLA sign, selection strip + coin mech, hooded delivery tray, rear condenser vents."""
    W, D, R, Y = 0.9, 0.74, 0.12, -0.37

    def sec(e=0.0, z=0.0):
        return rr(W + 2 * e, D + 2 * e, R + e), z
    s.loft([sec(-0.02, 0.0), sec(0.0, 0.02), sec(0.0, 0.05), sec(-0.015, 0.066)], pal.Chrome, cap_bottom=False,
           cap_top=False)  # plinth
    s.loft([sec(-0.012, 0.06), sec(0, 0.085), sec(0, 0.96)], pal.Enamel_Red, cap_bottom=False, cap_top=False)
    s.loft([sec(0, 0.958), sec(0.008, 0.966), sec(0.012, 0.982), sec(0.008, 0.998), sec(0, 1.006)], pal.Chrome,
           cap_bottom=False, cap_top=False)  # belt
    s.loft([sec(0, 1.004), sec(0, 1.42), (rr(0.87, 0.71, 0.14), 1.48), (rr(0.78, 0.67, 0.2), 1.53),
            (rr(0.68, 0.64, 0.28), 1.56), (rr(0.62, 0.62, 0.31), 1.575)], pal.Enamel_Ivory, cap_bottom=False,
           cap_top=False)
    with s.at((0, Y, 1.2), FRONT):  # illuminated sign
        frame(s, 0.6, 0.34, 0.07, 0.03, 0.028, pal.Chrome, pal.Enamel_Red, pal.LampSoft)
        letters(s, "COLA", 0.16, pal.Enamel_Red, lift=0.002, wt=0.2, track=0.24)
        for y in (-0.118, 0.118):
            strip(s, -0.23, y - 0.006, 0.23, y + 0.006, pal.Enamel_Red, 0.003)
    with s.at((0, Y, 0.8), FRONT):  # selection strip, coin mech
        frame(s, 0.64, 0.2, 0.05, 0.03, 0.022, pal.Chrome, pal.Iron, pal.Enamel_Ivory)
        for x in (-0.24, -0.14, -0.04):
            with s.at((x, -0.028, 0.002)):
                knob(s, 0.026, pal.Enamel_Red)
            strip(s, x - 0.036, 0.03, x + 0.036, 0.062, pal.LampSoft, 0.004)
        flat(s, rr(0.2, 0.15, 0.02, 3, 0.19), 0.002, 0.016, pal.Chrome)
        strip(s, 0.235, -0.005, 0.249, 0.05, pal.Iron, 0.0175)  # coin slot
        strip(s, 0.215, -0.058, 0.265, -0.028, pal.Iron, 0.0175)  # return cup
        for y, m in ((0.045, pal.Signal_Green), (0.012, pal.Signal_Red)):
            with s.at((0.13, y, 0.016)):
                lamp(s, 0.008, m)
        with s.at((0.13, -0.035, 0.016)):
            knob(s, 0.019, pal.Enamel_Ivory)  # coin return
    with s.at((0, Y, 0.36), FRONT):  # hooded delivery tray, a bottle waiting inside
        frame(s, 0.48, 0.22, 0.06, 0.07, 0.03, pal.Enamel_Coral, pal.Enamel_Charcoal, pal.Enamel_Charcoal)
        flat(s, rr(0.38, 0.035, 0.012, 2, 0, -0.082), 0.002, 0.08, pal.Chrome)
        with s.at((-0.07, -0.04, 0.03), (0, 90, 0)):
            bottle(s, k=0.85)
    with s.at((0.255, Y, 0.6), FRONT):  # bottle opener + cap catcher
        flat(s, rr(0.06, 0.08, 0.012, 2), 0.0, 0.012, pal.Chrome)
        strip(s, -0.016, 0.004, 0.016, 0.018, pal.Iron, 0.0135)
        flat(s, rr(0.09, 0.06, 0.012, 2, 0, -0.085), 0.0, 0.03, pal.Chrome)
        strip(s, -0.035, -0.07, 0.035, -0.062, pal.Iron, 0.0315)
    for x in (-0.31, 0.31):  # door seams
        fstrip(s, x - 0.002, 0.12, x + 0.002, 0.93, Y, pal.Enamel_Charcoal)
        fstrip(s, x - 0.002, 1.03, x + 0.002, 1.4, Y, pal.Enamel_Charcoal)
    for sx in (-1, 1):  # speed lines, starbursts, condenser vents
        rot = SIDE_R if sx > 0 else SIDE_L
        for z, L in ((0.62, 0.42), (0.68, 0.3)):
            s.sweep([(sx * 0.451, -0.24, z), (sx * 0.451, -0.24 + L, z)], 0.007, pal.Chrome, seg=10, caps=False)
        with s.at((sx * 0.451, 0.0, 1.22), rot):
            starburst(s, 0.11, pal.Enamel_Coral, t=0.008, hub=pal.Chrome)
        with s.at((sx * 0.451, 0.13, 0.26), rot):
            vent(s, 0.2, 0.22, 7, pal.Enamel_Red)
    s.lathe([(0.3, 1.57), (0.322, 1.58), (0.326, 1.6), (0.305, 1.622)], pal.Chrome, seg=20, cap_bottom=False,
            cap_top=False)  # dome collar
    s.face([(x, y, 1.622) for x, y in circle(0.305, 24)], pal.Enamel_Coral)  # turntable
    for k in range(6):
        a = 2 * math.pi * (k + 0.5) / 6
        with s.at((0.15 * math.cos(a), 0.15 * math.sin(a), 1.622)):
            bottle(s)
    s.lathe(dome(0.3, 1.618, 0.27, 6), pal.Glass, seg=24, cap_bottom=False)
    s.lathe([(0.04, 1.878), (0.03, 1.9), (0.013, 1.925), (0.01, 1.94)], pal.Chrome, seg=10, cap_bottom=False,
            cap_top=False)  # finial
    s.lathe([(0.012, 1.938), (0.02, 1.95)] + dome(0.02, 1.958, 0.02, 3), pal.Enamel_Red, seg=10, cap_bottom=False)


# ----------------------------------------------------------------- snack machine

PACKS = ("Enamel_Red", "Enamel_Mustard", "Enamel_Seafoam", "Enamel_Coral", "Enamel_Powder", "Enamel_Cream")


def pack(s, x, y, z, w, h, d, mat):
    """Open product box: front, top and left faces only."""
    x0, x1, y1, z1 = x - w / 2, x + w / 2, y + d, z + h
    s.face([(x0, y, z), (x1, y, z), (x1, y, z1), (x0, y, z1)], mat)
    s.face([(x0, y, z1), (x1, y, z1), (x1, y1, z1), (x0, y1, z1)], mat)
    s.face([(x0, y1, z), (x0, y, z), (x0, y, z1), (x0, y1, z1)], mat)


def coil(s, x, y, z, r=0.046, turns=1.25, pitch=0.07, n=12, w=0.011):
    """Product spiral seen from the front: a flat chrome ribbon helix about +Y, facing -Y."""
    verts = []
    for k in range(n + 1):
        a = 2 * math.pi * turns * k / n
        u = (math.sin(a), math.cos(a))
        yy = y + pitch * turns * k / n
        verts += [(x + (r - w / 2) * u[0], yy, z + (r - w / 2) * u[1]), (x + (r + w / 2) * u[0], yy, z + (r + w / 2) * u[1])]
    s._add(verts, [[2 * k, 2 * k + 2, 2 * k + 3, 2 * k + 1] for k in range(n)], pal.Chrome, True)


@asset("VendingMachine_Snack", "Furnish")
def vending_snack(s):
    """0.96 x 0.8 x 1.86 m free-standing: teal cabinet, glazed bay of 16 product spirals, ivory control column
    (display, keypad, coin mech, status lamps), lit SNACKS header, pickup bin with a hinged flap."""
    T, Y, D, W, H0, H1 = pal.Enamel_Teal, -0.4, 0.4, 0.96, 0.07, 1.86
    X0, X1, Z0, Z1 = -0.42, 0.16, 0.62, 1.54
    s.loft([(rr(0.92, 0.76, 0.06), 0.0), (rr(0.95, 0.79, 0.075), 0.02), (rr(0.95, 0.79, 0.075), 0.05),
            (rr(0.93, 0.77, 0.065), 0.075)], pal.Chrome, cap_bottom=False, cap_top=False)
    HC, HH = (H0 + H1) / 2, H1 - H0
    bx, bz = (X0 + X1) / 2, (Z0 + Z1) / 2
    with s.at((0, 0, HC), FRONT):  # carcass loft back -> front, turning into the bay: local z = -world y
        def body(e):
            return rr(W - 2 * e, HH - 2 * e, 0.08 - e)
        bay = rr(X1 - X0, Z1 - Z0, 0.03, 4, bx, bz - HC)
        s.loft([(body(0), -D), (body(0), -Y - 0.03), (body(0.006), -Y - 0.012), (body(0.018), -Y - 0.002),
                (body(0.03), -Y), (bay, -Y)], T, cap_top=False)
        s.loft([(bay, -Y), (bay, -0.28)], pal.Enamel_Charcoal, cap_bottom=False)
    s.face([(X0, Y + 0.08, Z1 - 0.012), (X0, 0.26, Z1 - 0.012), (X1, 0.26, Z1 - 0.012), (X1, Y + 0.08, Z1 - 0.012)],
           pal.LampSoft)  # lamp under the bay roof
    with s.at((bx, Y, bz), FRONT):
        frame(s, X1 - X0 + 0.04, Z1 - Z0 + 0.04, 0.045, 0.028, 0.03, pal.Chrome)
    for g in ([(X0, Y + 0.012, Z0), (X1, Y + 0.012, Z0), (X1, Y + 0.012, Z1), (X0, Y + 0.012, Z1)],):
        s.face(g, pal.GlassDark)
        s.face(g[::-1], pal.GlassDark)
    cw = X1 - X0
    for i in range(4):
        zs = Z0 + 0.02 + i * (Z1 - Z0 - 0.04) / 4
        s.face([(X0, Y + 0.05, zs), (X1, Y + 0.05, zs), (X1, 0.28, zs), (X0, 0.28, zs)], pal.Steel)  # shelf
        fstrip(s, X0, zs - 0.026, X1, zs + 0.006, Y + 0.05, pal.Enamel_Mustard)  # price rail
        for j in range(4):
            x = X0 + cw * (j + 0.5) / 4
            fstrip(s, x - 0.022, zs - 0.02, x + 0.022, zs, Y + 0.049, pal.Enamel_Ivory)  # price tab
            m = getattr(pal, PACKS[(i * 3 + j * 2) % len(PACKS)])
            h = (0.15, 0.12, 0.17, 0.1)[(i + j) % 4]
            for k, y in enumerate((Y + 0.1, Y + 0.17)):
                pack(s, x, y, zs, 0.1, h * (1 - 0.06 * k), 0.035, m)
            coil(s, x, Y + 0.07, zs + 0.046)
    with s.at((0, Y, 1.7), FRONT):  # lit header
        frame(s, 0.8, 0.2, 0.06, 0.028, 0.024, pal.Chrome, pal.Enamel_Mustard, pal.LampSoft)
        letters(s, "SNACKS", 0.115, pal.Enamel_Red, lift=0.002, wt=0.16)
    for z in (1.585, 1.815):
        fstrip(s, -0.4, z - 0.002, 0.4, z + 0.002, Y, pal.Enamel_Charcoal)  # header panel seams
    P, F = 0.325, Y - 0.016
    with s.at((P, Y, 1.09), FRONT):
        flat(s, rr(0.24, 0.9, 0.03, 3), 0.002, 0.016, pal.Enamel_Ivory)
    with s.at((P, F, 1.44), FRONT):
        frame(s, 0.19, 0.09, 0.02, 0.022, 0.016, pal.Chrome, pal.Iron, pal.Screen)
    with s.at((P, F, 1.35), FRONT):
        for x, m in ((-0.05, pal.Signal_Green), (0.0, pal.Signal_Amber), (0.05, pal.Signal_Red)):
            with s.at((x, 0, 0)):
                lamp(s, 0.009, m)
    with s.at((P, F, 1.2), FRONT):
        frame(s, 0.17, 0.19, 0.02, 0.018, 0.014, pal.Chrome, pal.Steel, pal.Steel)
        s.decal([(-0.07, -0.08, 0.004), (0.07, -0.08, 0.004), (0.07, 0.08, 0.004), (-0.07, 0.08, 0.004)],
                pal.Decal_Transit, sub(DECAL["keypad"], 31, 30, 225, 240))
    with s.at((P, F, 0.99), FRONT):  # coin plate, slot, return
        flat(s, rr(0.14, 0.15, 0.02, 3), 0.0, 0.014, pal.Chrome)
        strip(s, -0.006, 0.0, 0.006, 0.05, pal.Iron, 0.0155)
        with s.at((0, -0.04, 0.014)):
            knob(s, 0.019, pal.Enamel_Red)
    with s.at((P, F, 0.8), FRONT):
        frame(s, 0.12, 0.08, 0.02, 0.03, 0.014, pal.Chrome, pal.Iron, pal.Iron)
    fstrip(s, P + 0.1, Z0 + 0.06, P + 0.114, Z1 - 0.06, F - 0.0005, pal.Enamel_Mustard)  # pinstripe
    with s.at((bx, Y, 0.34), FRONT):  # pickup bin: recess, hinged flap, push lip
        frame(s, 0.5, 0.24, 0.05, 0.04, 0.03, pal.Chrome, pal.Enamel_Charcoal, pal.Enamel_Charcoal)
        with s.at((0, 0.08, 0.03), (16, 0, 0)):
            flat(s, rr(0.4, 0.15, 0.012, 2, 0, -0.075), -0.004, 0.004, pal.Steel)
        s.sweep(fillet([(-0.12, -0.07, 0.01), (-0.12, -0.07, 0.035), (0.12, -0.07, 0.035), (0.12, -0.07, 0.01)],
                       0.015, n=3, closed=False), 0.008, pal.Chrome, seg=10, caps=False)
    for z in (0.14, 0.56):
        fstrip(s, -0.44, z - 0.002, 0.44, z + 0.002, Y, pal.Enamel_Charcoal)
    for sx in (-1, 1):
        rot = SIDE_R if sx > 0 else SIDE_L
        for z, L in ((0.36, 0.46), (0.42, 0.32)):
            s.sweep([(sx * 0.481, -0.3, z), (sx * 0.481, -0.3 + L, z)], 0.007, pal.Chrome, seg=10, caps=False)
        with s.at((sx * 0.481, 0.2, 0.3), rot):
            vent(s, 0.22, 0.24, 7, T)


# ----------------------------------------------------------------- automat wall

def plate(s, X, Zs, y, mat):
    """Plate on the plane y facing -Y over the X x Zs edge lists, with a hole in every odd x odd cell."""
    n = len(X)
    s._add([(x, y, z) for z in Zs for x in X],
           [[j * n + i, j * n + i + 1, (j + 1) * n + i + 1, (j + 1) * n + i]
            for j in range(len(Zs) - 1) for i in range(n - 1) if not (i % 2 and j % 2)], mat, False)


def cell(s, x0, x1, z0, z1, ya, yb, wall, back=None, top=None):
    """Inward-facing tunnel from ya to yb (floor, ceiling, cheeks), optional back at yb and ceiling material."""
    s.face([(x0, ya, z0), (x1, ya, z0), (x1, yb, z0), (x0, yb, z0)], wall)
    s.face([(x0, yb, z1), (x1, yb, z1), (x1, ya, z1), (x0, ya, z1)], top or wall)
    s.face([(x0, ya, z0), (x0, yb, z0), (x0, yb, z1), (x0, ya, z1)], wall)
    s.face([(x1, yb, z0), (x1, ya, z0), (x1, ya, z1), (x1, yb, z1)], wall)
    if back is not None:
        s.face([(x0, yb, z0), (x1, yb, z0), (x1, yb, z1), (x0, yb, z1)], back)


def edges(lo, hi, k, bar):
    """Cell edge list [lo, a0, b0, a1, b1, ..., hi] for k cells separated by bars."""
    step = (hi - lo - bar) / k
    out = [lo]
    for i in range(k):
        out += [lo + bar + i * step, lo + i * step + step]
    return out + [hi]


def food(s, kind, x, y, z):
    """Automat plate: pie slice, layer cake or a cup of coffee (loose-faced wedges, no bevel strips)."""
    s.face([(x + px, y + py, z + 0.004) for px, py in circle(0.07, 10)], pal.Enamel_Ivory)
    if kind == 2:
        with s.at((x, y, z + 0.004)):
            s.lathe([(0.03, 0), (0.035, 0.065)], pal.Enamel_Ivory, seg=10, cap_bottom=False, cap_top=False)
            s.face([(px, py, 0.055) for px, py in circle(0.032, 10)], pal.Cola)
        return
    r, a = 0.075, 50
    poly = [(0, r / 2)] + [(r * math.cos(math.radians(t)), r / 2 + r * math.sin(math.radians(t)))
                           for t in (270 - a / 2 + a * i / 3 for i in range(4))]
    layers = (((0.012, pal.Pastry), (0.026, pal.Enamel_Red), (0.01, pal.Pastry)) if kind == 0 else
              ((0.02, pal.Enamel_Cream), (0.01, pal.Enamel_Coral), (0.02, pal.Enamel_Cream), (0.006, pal.Enamel_Ivory)))
    with s.at((x, y, z + 0.005)):
        zz = 0.0
        for dz, m in layers:
            flat(s, poly, zz, zz + dz, m)
            zz += dz


@asset("Automat", "Furnish")
def automat(s):
    """Wall-mounted 1.86 x 0.72 x 2.46 m (1.8 m unit; back face y = +0.25 flush to the wall, ledge to y = -0.47):
    5 x 4 chrome-framed glass compartments (lit ones stocked, a green lamp each), coin-change panel, terrazzo
    counter ledge with chrome nosing, fluted teal base, lettered header with a stepped crest."""
    Y, D = -0.25, 0.25
    s.rbox((-0.89, Y + 0.01, 0.0), (0.89, D - 0.01, 0.1), 0.02, pal.Chrome)
    s.rbox((-0.9, Y, 0.08), (0.9, D, 0.92), 0.03, pal.Enamel_Teal)  # base cabinet
    for k in range(11):  # fluting
        x = -0.75 + 1.5 * k / 10
        s.sweep([(x, Y - 0.004, 0.16), (x, Y - 0.004, 0.84)], 0.016, pal.Enamel_Seafoam, seg=10, caps=False)
    for z in (0.13, 0.87):
        fstrip(s, -0.8, z - 0.003, 0.8, z + 0.003, Y - 0.001, pal.Gold)
    s.rbox((-0.93, -0.47, 0.9), (0.93, Y + 0.04, 0.955), 0.018, pal.Terrazzo_Ivory)  # counter ledge
    s.sweep([(-0.93, -0.47, 0.927), (0.93, -0.47, 0.927)], 0.03, pal.Chrome, seg=10, caps=False)
    for x in (-0.7, 0.0, 0.7):
        with s.at((x, 0, 0), SIDE_R):
            flat(s, [(Y, 0.9), (-0.43, 0.9), (Y, 0.72)][::-1], -0.012, 0.012, pal.Chrome)  # ledge brackets
    GX0, GX1, GZ0, GZ1 = -0.84, 0.5, 1.0, 1.98
    X, Zs = edges(GX0, GX1, 5, 0.04), edges(GZ0, GZ1, 4, 0.04)
    s.box((-0.9, Y + 0.225, 0.95), (0.9, D, 2.02), pal.Enamel_Ivory)  # carcass behind the compartments
    plate(s, X, Zs, Y - 0.01, pal.Chrome)
    lit = {(0, 1), (1, 3), (2, 0), (2, 2), (3, 1), (3, 4), (0, 3), (1, 0)}
    for j in range(4):
        for i in range(5):
            x0, x1, z0, z1 = X[2 * i + 1], X[2 * i + 2], Zs[2 * j + 1], Zs[2 * j + 2]
            on = (j, i) in lit
            cell(s, x0, x1, z0, z1, Y - 0.01, Y + 0.015, pal.Chrome)
            if on:
                cell(s, x0, x1, z0, z1, Y + 0.015, Y + 0.22, pal.Enamel_Cream, pal.Enamel_Cream, pal.LampSoft)
            else:
                cell(s, x0, x1, z0, z1, Y + 0.015, Y + 0.22, pal.Enamel_Charcoal, pal.Enamel_Charcoal)
            s.face([(x0, Y + 0.012, z0), (x1, Y + 0.012, z0), (x1, Y + 0.012, z1), (x0, Y + 0.012, z1)], pal.GlassDark)
            fstrip(s, x1 - 0.03, (z0 + z1) / 2 - 0.035, x1 - 0.018, (z0 + z1) / 2 + 0.035, Y + 0.011, pal.Chrome)  # pull
            fstrip(s, x0 + 0.03, z0 - 0.029, x0 + 0.075, z0 - 0.013, Y - 0.011, pal.Iron)  # coin slot
            if on:
                fstrip(s, x1 - 0.024, z0 - 0.028, x1 - 0.012, z0 - 0.014, Y - 0.011, pal.Signal_Green)
                food(s, (i + 2 * j) % 3, (x0 + x1) / 2, Y + 0.12, z0)
    CX = 0.7  # coin-change panel
    with s.at((CX, Y - 0.01, 1.49), FRONT):
        flat(s, rr(0.3, 0.98, 0.03, 3), 0.0, 0.02, pal.Enamel_Seafoam)
    F = Y - 0.03
    with s.at((CX, F, 1.8), FRONT):
        starburst(s, 0.1, pal.Gold, n=12, t=0.008, hub=pal.Enamel_Coral)
    with s.at((CX, F, 1.55), FRONT):
        frame(s, 0.2, 0.13, 0.03, 0.025, 0.02, pal.Chrome, pal.Iron, pal.LampSoft)
        for k in range(3):  # coin stacks behind the window
            flat(s, rr(0.036, 0.05, 0.004, 1, -0.05 + 0.05 * k, -0.01 + 0.008 * k), 0.002, 0.012,
                 pal.Enamel_Coral if k == 1 else pal.Gold)
    with s.at((CX, F, 1.33), FRONT):
        flat(s, rr(0.14, 0.16, 0.02, 3), 0.0, 0.016, pal.Chrome)
        strip(s, -0.006, -0.01, 0.006, 0.05, pal.Iron, 0.0175)
        with s.at((0, -0.045, 0.016)):
            knob(s, 0.02, pal.Enamel_Red)
        for x, m in ((-0.045, pal.Signal_Green), (0.045, pal.Signal_Amber)):
            with s.at((x, 0.05, 0.016)):
                lamp(s, 0.008, m)
    with s.at((CX, F, 1.1), FRONT):
        frame(s, 0.18, 0.1, 0.03, 0.05, 0.02, pal.Chrome, pal.Enamel_Charcoal, pal.Enamel_Charcoal)
    with s.at((CX, F, 1.7), FRONT):
        vent(s, 0.18, 0.08, 4, pal.Enamel_Seafoam)
    s.rbox((-0.92, Y - 0.06, 2.0), (0.92, D, 2.24), 0.05, pal.Enamel_Teal)  # header
    for z in (2.006, 2.234):
        s.sweep([(-0.88, Y - 0.062, z), (0.88, Y - 0.062, z)], 0.01, pal.Gold, seg=10, caps=False)
    with s.at((0, Y - 0.06, 2.12), FRONT):
        letters(s, "AUTOMAT", 0.12, pal.Gold, wt=0.15)
    crest = [(-0.31, 0), (0.31, 0), (0.31, 0.08), (0.2, 0.08), (0.2, 0.16), (0.1, 0.16), (0.1, 0.24), (-0.1, 0.24),
             (-0.1, 0.16), (-0.2, 0.16), (-0.2, 0.08), (-0.31, 0.08)]
    with s.at((0, Y - 0.02, 2.2), FRONT):  # stepped crest: teal slab, ivory face, gold starburst
        flat(s, crest, -0.2, 0.0, pal.Enamel_Teal)
        with s.at((0, 0.012, 0), scale=(0.84, 0.8, 1)):
            flat(s, crest, 0.0, 0.012, pal.Enamel_Ivory)
        with s.at((0, 0.12, 0.012)):
            starburst(s, 0.075, pal.Gold, n=12, t=0.008, hub=pal.Enamel_Coral)
    for x in (-0.9, 0.9):  # end pilasters
        s.rbox((x - 0.03, Y - 0.035, 0.94), (x + 0.03, D, 2.02), 0.02, pal.Enamel_Seafoam)
        s.sweep([(x, Y - 0.037, 0.98), (x, Y - 0.037, 1.98)], 0.008, pal.Gold, seg=10, caps=False)


import furnish_cafe  # noqa: E402,F401  (registers the café / terrace pieces)
import furnish_civic  # noqa: E402,F401  (registers the civic / waterfront pieces)
import furnish_garden  # noqa: E402,F401  (registers the planters)
