"""Ground cars for the play scene (brief 020 C): the kit had no car (the official and getaway cars are 1.8 x 1.4 x 4.6 m boxes in HostViews.cs; the kit's vehicles are a tram, a box
truck, scooters and hovering AirCars). A 1950s-shaped sedan: low lofted body with a rounded plan, a glass cabin with a painted roof, rear fins, chrome bumpers, grille and
flank line, four whitewall wheels. Length along Y, nose at -Y, about 4.7 m long, 1.8 m wide, 1.3 m high; pivot = footprint centre on the ground, so it drops in for the box.
Sedan_Official is deep teal with an ivory roof (Vale's car), Sedan_Getaway coral with a seafoam roof (the cell's). About 3k triangles each: a car at play zoom is 55 px high."""

import math

from geo import Shape  # noqa: F401
from kit import asset
from mats import pal
from vehicles_parts import lamp, vloft, wheel

L2 = 2.3  # half length


def sedan(s, body, roof):
    with s.at((0, 0, 0), (0, 0, 0), scale=0.92):  # modelled at 5.1 m; 0.92 makes it the box's 4.6 m (plus lamps and fins)
        _sedan(s, body, roof)


def _sedan(s, body, roof):
    sides = [0, 0.3, 0.7, 1]
    tub = [(0.28, 0.80, -2.12, 2.02, 0.36, 0.30), (0.42, 0.90, -2.2, 2.08, 0.42, 0.36), (0.74, 0.92, -2.22, 2.10, 0.44, 0.38), (0.90, 0.90, -2.15, 2.04, 0.46, 0.40),
           (0.98, 0.84, -1.95, 1.90, 0.50, 0.44)]
    vloft(s, tub, body, sides, nc=12, e=2.6)
    cab = [(0.98, 0.80, -0.50, 1.46, 0.55, 0.60), (1.18, 0.75, -0.30, 1.34, 0.55, 0.60), (1.34, 0.66, -0.05, 1.12, 0.52, 0.56), (1.40, 0.63, 0.05, 1.04, 0.50, 0.54)]
    vloft(s, cab, pal.GlassDark, sides, nc=12, e=2.6, paint=lambda i, j, c: roof if i == len(cab) - 2 else None, caps=((0, None), (-1, roof)))
    for sg in (-1, 1):  # rear fins and the chrome flank line
        with s.at((sg * 0.84, 0, 0), (90, 0, 90)):
            s.extrude([(1.55, 0.96), (2.22, 1.0), (2.18, 1.28)] if sg < 0 else [(1.55, 0.96), (2.22, 1.0), (2.18, 1.28)], -0.03, 0.03, body)
        s.sweep([(sg * 0.925, -1.9, 0.62), (sg * 0.935, 0.2, 0.62), (sg * 0.925, 1.95, 0.7)], 0.012, pal.Chrome, seg=4, caps=False)
    s.rbox((-0.82, -2.36, 0.32), (0.82, -2.16, 0.5), 0.05, pal.Chrome, seg=2)   # front bumper
    s.rbox((-0.82, 2.14, 0.32), (0.82, 2.34, 0.5), 0.05, pal.Chrome, seg=2)     # rear bumper
    s.box((-0.4, -2.3, 0.56), (0.4, -2.2, 0.76), pal.Chrome)                    # grille
    for k in range(5):
        s.box((-0.36 + 0.18 * k, -2.31, 0.58), (-0.33 + 0.18 * k, -2.2, 0.74), pal.Enamel_Charcoal)
    for sg in (-1, 1):
        lamp(s, (sg * 0.62, -2.2, 0.72), (0, -1, 0), 0.1, depth=0.05, seg=10)
        lamp(s, (sg * 0.66, 2.12, 0.8), (0, 1, 0.1), 0.06, lens=pal.Signal_Red, depth=0.03, seg=8)
    for sg in (-1, 1):
        for y in (-1.38, 1.4):
            wheel(s, (sg * 0.84, y, 0.34), 0.34, 0.2, white=(0.21, 0.29), hub=0.5, rim=pal.Steel, seg=14, side=sg)


@asset("Sedan_Official", "Vehicles", bevel=0.0)
def sedan_official(s):
    """Vale's official car: deep teal, ivory roof."""
    sedan(s, pal.Enamel_Teal, pal.Enamel_Ivory)


@asset("Sedan_Getaway", "Vehicles", bevel=0.0)
def sedan_getaway(s):
    """The cell's getaway car: coral, seafoam roof."""
    sedan(s, pal.Accent_Coral, pal.Enamel_Seafoam)
