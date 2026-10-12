"""Deco Kit sky traffic (studies: Concepts/Horizon/street_ab, sky_ab): the sky-rail streamliner and its
pylon parts, aircars, a saucer and a flying rocket liner. Unity moves them (Scripts/Sky); the beam is swept there
along the route (DecoKitSkyRail.cs). Kit frame: metres, Z up, front -Y (Unity +Z = direction of travel).

  SkyRail_Car_Head   18 m nose car; origin = beam top centre (skirts hang over a 1.2 x 1.6 m beam)
  SkyRail_Car_Mid    18 m middle car
  SkyRail_Pylon_Foot limestone plinth;  SkyRail_Pylon_Shaft 1 m unit (Unity stretches it);  SkyRail_Pylon_Head
                     flared cradle, pivot at its top = the beam's underside
  AirCar_Coral / AirCar_Teal   6.4 m finned teardrop with a bubble canopy and a glowing exhaust
  AirCar_Saucer      9 m saucer, glazed dome, lamp ring
  RocketShip         34 m liner in flight, flame below the base (pivot = base centre)
"""

import math

import mats
from kit import asset
from mats import pal
from mega_parts import FRONT, rocket
from structures import superell, tube

mats.register("DK_Flame", "emit", "#FFD9A0", 25.0)  # engine glow: blooms
CAR_L, CAR_W, CAR_H = 18.0, 1.6, 3.2  # length, half-width, height above the beam top
SIDE = (0, 0, 90)                      # with FRONT: local (x, y, z) -> world (z, x, y); polygons in the YZ plane


def car(s, nose):
    """Streamliner car along Y (front -Y): superellipse body, a nose ogive when `nose`, window band, coral stripe,
    chrome skirt line; skirts straddle the beam (z < 0)."""
    y0, y1, n = -CAR_L / 2, CAR_L / 2, 24
    ys = [y0 + (y1 - y0) * k / n for k in range(n + 1)]
    L = 5.0  # nose length

    def sc(y):
        u = (y0 + L - y) / L if nose else 0.0
        return max(0.08, math.sqrt(max(0.0, 1 - u * u))) if u > 0 else 1.0
    path = [(0.0, y, 0.0) for y in ys]
    scale = [(sc(y), sc(y)) for y in ys]
    body = superell(CAR_W, CAR_H / 2, p=3.2, n=28, cb=CAR_H / 2 + 0.1)  # z 0.1 .. 3.3
    tube(s, path, body, pal.Enamel_Ivory, scale=scale, caps=True)
    straight = [(0.0, y, 0.0) for y in ys if sc(y) > 0.97]
    band = superell(CAR_W + 0.03, 0.42, p=6, n=28, cb=2.0)
    tube(s, straight, band, pal.Window_Tower, caps=False)
    stripe = superell(CAR_W + 0.02, 0.1, p=8, n=28, cb=1.1)
    tube(s, straight, stripe, pal.Enamel_Coral, caps=False)
    for x in (-1.0, 1.0):  # skirts over the beam
        s.box((x * 1.12 - 0.12, y0 + (L if nose else 0.4), -0.9), (x * 1.12 + 0.12, y1 - 0.4, 0.3), pal.Enamel_Ivory)
        s.box((x * 1.12 - 0.14, y0 + (L if nose else 0.4), -0.95), (x * 1.12 + 0.14, y1 - 0.4, -0.85), pal.Chrome)
    s.box((-0.3, y1 - 0.2, 0.4), (0.3, y1 + 0.3, 2.6), pal.Enamel_Charcoal)  # gangway bellows


@asset("SkyRail_Car_Head", "Sky", bevel=0.0)
def rail_head(s):
    car(s, nose=True)
    with s.at((0, -CAR_L / 2 + 0.15, CAR_H / 2 + 0.1), (90, 0, 0)):  # headlamp at the nose tip
        s.lathe([(0.0, 0.0), (0.22, 0.0), (0.22, 0.1), (0.0, 0.1)], pal.Lamp, seg=16)


@asset("SkyRail_Car_Mid", "Sky", bevel=0.0)
def rail_mid(s):
    car(s, nose=False)


@asset("SkyRail_Pylon_Foot", "Sky", bevel=0.0)
def pylon_foot(s):
    s.rbox((-1.4, -1.4, 0.0), (1.4, 1.4, 1.2), 0.15, pal.Limestone_Tex)
    s.box((-1.43, -1.43, 0.8), (1.43, 1.43, 1.0), pal.Enamel_Teal)


@asset("SkyRail_Pylon_Shaft", "Sky", bevel=0.0)
def pylon_shaft(s):
    tube(s, [(0, 0, 0.0), (0, 0, 1.0)], superell(0.75, 0.55, p=3, n=20), pal.Enamel_Ivory, up=(1, 0, 0),
         scale=[(1.0, 1.0), (0.78, 0.78)], caps=False)


@asset("SkyRail_Pylon_Head", "Sky", bevel=0.0)
def pylon_head(s):
    """Flared cradle, top at z = 0 (the beam's underside), 2.2 m deep."""
    zs = [-2.2 + 2.2 * k / 8 for k in range(9)]
    tube(s, [(0, 0, z) for z in zs], superell(0.6, 0.45, p=3, n=20), pal.Enamel_Ivory, up=(1, 0, 0),
         scale=[(1 + 1.4 * (k / 8) ** 2, 1 + 1.2 * (k / 8) ** 2) for k in range(9)], caps=True)
    s.box((-1.5, -0.7, -0.12), (1.5, 0.7, 0.0), pal.Steel_Ivory)


def aircar(s, paint):
    """Finned teardrop along Y (front -Y): tapered superellipse body, bubble canopy, twin fins, chrome spear."""
    ys = [-3.0 + 6.4 * k / 20 for k in range(21)]

    def sc(y):
        if y < -1.6:
            u = (-1.6 - y) / 1.4
            return max(0.1, math.sqrt(max(0.0, 1 - u * u)))
        return max(0.15, 1 - 0.85 * max(0.0, (y - 0.6) / 2.8) ** 1.3)
    tube(s, [(0.0, y, 0.0) for y in ys], superell(1.05, 0.6, p=2.6, n=24), paint,
         scale=[(sc(y), sc(y)) for y in ys], caps=True)
    with s.at((0, -0.9, 0.35), (0, 0, 0), (0.75, 1.5, 0.62)):
        s.lathe([(math.sin(math.pi / 2 * k / 8), math.cos(math.pi / 2 * k / 8)) for k in range(9)][::-1],
                pal.GlassDark, seg=24, cap_bottom=False)
    for x in (-0.55, 0.55):
        with s.at((0, 0, 0), SIDE), s.at((0, 0, 0), FRONT):
            s.extrude([(1.2, 0.3), (3.3, 0.35), (3.5, 1.55), (2.9, 1.5)], x - 0.05, x + 0.05, paint)
    spear = [y for y in ys if -2.2 <= y <= 1.9]
    tube(s, [(0.0, y, 0.0) for y in spear], superell(1.05, 0.05, p=8, n=24, cb=0.05), pal.Chrome,
         scale=[(sc(y) + 0.02, 1.0) for y in spear], caps=False)
    with s.at((0, 3.3, 0), (-90, 0, 0)):  # exhaust glow, pointing aft
        s.lathe([(0.0, 1.4), (0.22, 0.4), (0.24, 0.0)], pal.Flame, seg=12, cap_bottom=False)


@asset("AirCar_Coral", "Sky", bevel=0.0)
def aircar_coral(s):
    aircar(s, pal.Enamel_Coral)


@asset("AirCar_Teal", "Sky", bevel=0.0)
def aircar_teal(s):
    aircar(s, pal.Enamel_Teal)


@asset("AirCar_Saucer", "Sky", bevel=0.0)
def aircar_saucer(s):
    from mega_parts import lens
    lens(s, -0.7, 0.4, 4.5, 1.4, pal.Hull_Silver, glaze=True, seg=48)
    s.lathe([(1.7 * math.sin(math.pi / 2 * k / 8), 0.7 + 1.3 * math.cos(math.pi / 2 * k / 8)) for k in range(9)][::-1],
            pal.GlassDark, seg=32, cap_bottom=False)
    s.lathe([(3.2, -0.5), (3.4, -0.62), (3.6, -0.5)], pal.Lamp, seg=48, cap_bottom=False, cap_top=False)
    s.lathe([(0.0, -0.75), (1.2, -0.75), (0.9, -1.1), (0.0, -1.1)], pal.Flame, seg=24)  # lift glow


@asset("RocketShip", "Sky", bevel=0.0)
def rocket_ship(s):
    rocket(s, 0.0, 34.0, 2.1, fins=3, body=pal.Hull_Silver, accent=pal.Enamel_Coral, seg=32)
    s.lathe([(0.0, -13.0), (0.55, -8.0), (0.85, -3.0), (0.8, 0.4), (0.0, 0.4)], pal.Flame, seg=24)
