"""Street furniture (reference sheet rows 3-5): bollard, streetlamp, hydrant, bench, barrier, recycling
bins, bike rack, oval planter, faceted pot. Proportions are the reference's chunky, toy-like read."""

from geo import circle, dome, fillet, ring, rrect, stadium, thicken
from kit import asset
from mats import DECAL, cell, pal

SIDE = (90, 0, 90)  # extrude frame: local X -> world Y, local Y -> world Z, extrusion -> world X
FRONT = (90, 0, 0)  # local XY -> world XZ (a panel facing -Y), extrusion -> world -Y


@asset("Bollard", "Street")
def bollard(s):
    s.lathe([(0, 0), (0.27, 0), (0.27, 0.025), (0.25, 0.045), (0.16, 0.055), (0, 0.055)], pal.Chrome, seg=48)
    s.lathe(ring(0.17, 0.215, 0.04, 0.058), pal.Enamel_Red, seg=48, cap_bottom=False, cap_top=False)
    s.lathe([(0.14, 0.05), (0.14, 0.085), (0.125, 0.1), (0, 0.1)], pal.Chrome, seg=40)
    s.lathe([(0.118, 0.09), (0.118, 0.72)] + dome(0.118, 0.72, 0.1)[1:], pal.Chrome, seg=40)
    s.lathe(ring(0.116, 0.123, 0.36, 0.372), pal.Steel, seg=40, cap_bottom=False, cap_top=False)
    s.lathe(ring(0.116, 0.125, 0.63, 0.69), pal.Enamel_Red, seg=40, cap_bottom=False, cap_top=False)


@asset("Streetlamp", "Street")
def streetlamp(s):
    s.lathe([(0, 0), (0.24, 0), (0.24, 0.04), (0.2, 0.08), (0.14, 0.1), (0, 0.1)], pal.Chrome, seg=40)
    s.lathe([(0.095, 0.09), (0.095, 1.18), (0.085, 1.22), (0, 1.22)], pal.Enamel_Red, seg=32)
    s.lathe(ring(0.08, 0.108, 1.18, 1.28), pal.Chrome, seg=32, cap_bottom=False, cap_top=False)
    head = (1.25, 0.0, 3.72)
    arm = fillet([(0, 0, 1.2), (0, 0, 3.86), (head[0], 0, 3.86)], 0.85, n=18, closed=False)
    s.sweep(arm, [0.052 - 0.016 * i / (len(arm) - 1) for i in range(len(arm))], pal.Chrome, seg=16)
    with s.at(head):
        shell = [(0.56, -0.07), (0.5, -0.01), (0.37, 0.06), (0.18, 0.12), (0, 0.14),
                 (0, 0.1), (0.17, 0.085), (0.35, 0.03), (0.48, -0.03), (0.53, -0.07), (0.56, -0.07)]
        s.lathe(shell, pal.Enamel_Teal, seg=56, cap_bottom=False, cap_top=False)
        s.lathe(ring(0.53, 0.58, -0.1, -0.055), pal.Chrome, seg=56, cap_bottom=False, cap_top=False)
        s.lathe([(0, -0.1), (0.28, -0.085), (0.46, -0.05), (0.46, -0.03)], pal.Lamp, seg=48)
        s.lathe([(0.045, 0.12)] + dome(0.045, 0.16, 0.05), pal.Chrome, seg=16)


@asset("Hydrant", "Street")
def hydrant(s):
    s.lathe([(0, 0), (0.2, 0), (0.2, 0.03), (0.17, 0.05), (0, 0.05)], pal.Chrome, seg=40)
    body = [(0.15, 0.05), (0.15, 0.085), (0.125, 0.11), (0.125, 0.52), (0.14, 0.535), (0.14, 0.585),
            (0.128, 0.6)] + dome(0.128, 0.6, 0.11, n=8)[1:]
    s.lathe(body, pal.Enamel_Red, seg=40)
    s.lathe(ring(0.138, 0.152, 0.535, 0.585), pal.Chrome, seg=40, cap_bottom=False, cap_top=False)
    s.lathe([(0.055, 0.68), (0.055, 0.735), (0.038, 0.755), (0, 0.76)], pal.Chrome, seg=24)
    s.extrude([(0.02 * c, 0.02 * d) for c, d in ((1, -.58), (1, .58), (0, 1.15), (-1, .58), (-1, -.58), (0, -1.15))],
              0.76, 0.79, pal.Chrome)
    for side, rot in ((1, (0, 90, 0)), (-1, (0, -90, 0))):
        with s.at((0.1 * side, 0, 0.4), rot):
            s.lathe([(0.052, 0), (0.052, 0.07)], pal.Enamel_Red, seg=24, cap_bottom=False)
            s.lathe([(0.064, 0.07), (0.064, 0.105), (0.05, 0.115), (0, 0.115)], pal.Chrome, seg=24)
        chain = [(0.19 * side, -0.03, 0.37), (0.17 * side, -0.07, 0.3), (0.13 * side, -0.08, 0.3),
                 (0.11 * side, -0.07, 0.34)]
        s.sweep(fillet(chain, 0.03, n=4, closed=False), 0.006, pal.Steel, seg=6)
    with s.at((0, -0.1, 0.34), (90, 0, 0)):
        s.lathe([(0.068, 0), (0.068, 0.07)], pal.Enamel_Red, seg=28, cap_bottom=False)
        s.lathe([(0.08, 0.07), (0.08, 0.11), (0.062, 0.122), (0, 0.122)], pal.Chrome, seg=28)


@asset("Bench", "Street", bevel=0.012)
def bench(s):
    for x in (-0.84, 0.84):
        s.rbox((x - 0.09, -0.3, 0.03), (x + 0.09, 0.22, 0.42), 0.05, pal.Enamel_Mint)
        s.rbox((x - 0.1, -0.32, 0.0), (x + 0.1, 0.24, 0.04), 0.012, pal.Chrome)
    s.rbox((-0.92, -0.13, 0.3), (0.92, 0.09, 0.42), 0.04, pal.Enamel_Mint)
    seat = thicken(fillet([(-0.34, 0.47), (0.12, 0.44), (0.22, 0.86)], 0.16, n=12, closed=False), 0.06)
    for x in (-0.43, 0.43):
        with s.at((x, 0, 0), SIDE):
            s.extrude(fillet(seat, 0.02, n=2), -0.37, 0.37, pal.Enamel_Red, smooth_sides=True)


@asset("Barrier", "Street")
def barrier(s):
    for x in (-0.88, 0.88):
        with s.at((x, 0, 0)):
            s.lathe([(0, 0), (0.12, 0), (0.12, 0.03), (0.1, 0.05), (0, 0.05)], pal.Chrome, seg=32)
            s.lathe([(0.075, 0.05), (0.075, 0.96)], pal.Enamel_Red, seg=32, cap_bottom=False, cap_top=False)
            s.lathe(ring(0.07, 0.085, 0.05, 0.1), pal.Chrome, seg=32, cap_bottom=False, cap_top=False)
            s.lathe([(0.085, 0.95), (0.085, 0.99)] + dome(0.08, 0.99, 0.08)[1:], pal.Chrome, seg=32)
            for z in (0.58, 0.86):
                s.rbox((-0.11 * (x / abs(x)) - 0.03, -0.025, z - 0.025), (-0.11 * (x / abs(x)) + 0.03, 0.025, z + 0.025),
                       0.008, pal.Chrome)
    frame = fillet(rrect(1.58, 0.36, 0.0), 0.12, n=8)
    with s.at((0, 0, 0.72), FRONT):
        s.sweep([(x, y, 0) for x, y in frame] + [(frame[0][0], frame[0][1], 0)], 0.018, pal.Chrome, seg=10,
                caps=False)
        s.extrude(rrect(1.5, 0.29, 0.1, n=8), -0.018, 0.018, pal.Enamel_Teal)


@asset("RecyclingBins", "Street", bevel=0.006)
def recycling_bins(s):
    for x, col, icon in ((-0.28, pal.Enamel_Teal, DECAL["recycle_teal"]),
                         (0.28, pal.Enamel_Mustard, DECAL["recycle_mustard"])):
        with s.at((x, 0, 0)):
            foot = stadium(0.5, 0.42, n=10)
            s.extrude(foot, 0.0, 0.06, pal.Chrome)
            body = stadium(0.46, 0.38, n=10)
            s.loft([(body, 0.06), (body, 0.74)], col, cap_bottom=False, cap_top=False)
            s.loft([(stadium(0.49, 0.41, n=10), 0.74), (stadium(0.49, 0.41, n=10), 0.78),
                    (stadium(0.45, 0.37, n=10), 0.84), (stadium(0.36, 0.28, n=10), 0.875),
                    (stadium(0.2, 0.14, n=10), 0.89)], pal.Chrome)
            with s.at((0, -0.172, 0.805), (62, 0, 0)):
                s.extrude(stadium(0.26, 0.06, n=8), -0.008, 0.01, pal.Iron)
            s.decal([(-0.12, -0.192, 0.3), (0.12, -0.192, 0.3), (0.12, -0.192, 0.54), (-0.12, -0.192, 0.54)],
                    pal.Decal_Transit, cell(icon))


@asset("BikeRack", "Street")
def bike_rack(s):
    for i in range(3):
        with s.at((0.62 * (i - 1), 0.12 * (i - 1), 0)):
            hoop = fillet([(0, -0.28, 0.02), (0, -0.28, 0.82), (0, 0.28, 0.82), (0, 0.28, 0.02)], 0.26, n=12,
                          closed=False)
            s.sweep(hoop, 0.03, pal.Chrome, seg=14)
            for y in (-0.28, 0.28):
                with s.at((0, y, 0)):
                    s.lathe([(0, 0), (0.07, 0), (0.07, 0.02), (0.05, 0.03), (0, 0.03)], pal.Chrome, seg=24)
                    s.lathe(ring(0.028, 0.048, 0.02, 0.09), pal.Enamel_Royal, seg=24, cap_bottom=False, cap_top=False)


@asset("OvalPlanter", "Street", bevel=0.008)
def oval_planter(s):
    W, D = 1.7, 0.72
    prof = [(0.07, 0.0), (0.02, 0.05), (0.0, 0.14), (0.0, 0.46), (0.015, 0.53), (0.045, 0.55), (0.085, 0.53),
            (0.085, 0.47)]
    s.loft([(stadium(W - 2 * i, D - 2 * i, n=14), z) for i, z in prof], pal.Enamel_Ivory, cap_top=True)
    s.loft([(stadium(W + 0.012, D + 0.012, n=14), 0.1), (stadium(W + 0.012, D + 0.012, n=14), 0.22)],
           pal.Enamel_Teal, cap_bottom=False, cap_top=False)
    s.loft([(stadium(W - 0.13, D - 0.13, n=14), 0.02), (stadium(W - 0.1, D - 0.1, n=14), 0.0)], pal.Chrome,
           cap_bottom=False, cap_top=False)
    s.extrude(stadium(W - 0.17, D - 0.17, n=14), 0.47, 0.5, pal.Soil)


@asset("FacetedPot", "Street")
def faceted_pot(s):
    b, t, h = 0.19, 0.27, 0.5  # half-widths at base and rim
    lo = [(-b, -b, 0), (b, -b, 0), (b, b, 0), (-b, b, 0)]
    hi = [(-t, -t, h), (t, -t, h), (t, t, h), (-t, t, h)]
    s.face(lo[::-1], pal.Enamel_Ivory)
    s.face([lo[0], lo[1], hi[0]], pal.Enamel_Red)  # front split on the diagonal, as in the reference
    s.face([lo[1], hi[1], hi[0]], pal.Enamel_Mustard)
    s.face([lo[1], lo[2], hi[2], hi[1]], pal.Enamel_Ivory)
    s.face([lo[2], lo[3], hi[3], hi[2]], pal.Enamel_Mustard)
    s.face([lo[3], lo[0], hi[0], hi[3]], pal.Enamel_Red)
    rim = [(x, y) for x, y, _ in hi]
    s.extrude(rim, h, h + 0.03, pal.Enamel_Ivory, bottom=False, top=False)
    inner = [(x * 0.88, y * 0.88) for x, y in rim]
    for i in range(4):
        a, c = rim[i], rim[(i + 1) % 4]
        a2, c2 = inner[i], inner[(i + 1) % 4]
        s.face([(*a, h + 0.03), (*c, h + 0.03), (*c2, h + 0.03), (*a2, h + 0.03)], pal.Enamel_Ivory)
    s.extrude(inner, h - 0.06, h + 0.03, pal.Soil, bottom=False, top=True)
