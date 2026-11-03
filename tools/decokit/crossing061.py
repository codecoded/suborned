"""Crossing signals, three directions (brief 061, Vela, 2026-10-24). DESIGN-STAGE CANDIDATES: this family is NOT in kit.FAMILIES, so `bun run decokit` never builds it and nothing is adopted;
tools/decokit/road_board.py imports it for the brief's mock-ups (`--x061 A|B|C --state walk|clear|stop`).

Sky, 2026-10-24: the heads must be real objects that face the people who use them (along the walk, toward each kerb, never the camera), change colour by state, and run a clock.
Frame (as the SignalEye): metres, Z up, pivot = the ground point under the head at the kerb end. Display faces on local -Y and +Y. Local -Y points ACROSS THE ROAD (the world export
places SignalEye at yaw walk - pi/2 on the near landing and walk + pi/2 on the far one, so local -Y is the walk direction from that kerb): -Y looks over the road at the people waiting on
the far kerb, +Y looks over the pavement at the people waiting here. Both always show the same aspect.

States (emissive colours; the cycle is runtime look, Latch drives it from the mission clock):
  walk   DK_X61_Walk  seafoam-mint #55E6B0   the figure walking
  clear  DK_X61_Clear amber         #FFB624   the walk is ending: countdown (numerals, ring or dial) running down to stop
  stop   DK_X61_Stop  signal red    #F2432E   the standing figure (red is allowed on small emissive displays)

Directions:
  A  the floating eye   a chrome lens (a coin on edge, faces along the walk) floating 2.55 m over the kerb; a lit rim ring all round carries the state, so the state reads edge-on from any
                        yaw; figure on each face, two-digit countdown numerals in clear; a beam down to a light disc in the paving that is the clock ring (drains in clear)
  B  the kerb pylon     a finned chrome and ivory pylon 3.0 m tall, a tall oval display with the figure over a round 1950s station-clock dial whose sector drains in clear; edge lights
                        up the head's two sides carry the state edge-on
  C  the kerb lantern   a low chrome capsule 1.0 m high with a ring of 24 lit segments round its crown (the clock: all lit in walk, draining in clear, dark in stop), a small
                        pictogram blade; the big readout is a pictogram disc inlaid in the paving where people wait, with the same ring; no tall head at all
"""

import math

import crossing_eye as ce
from geo import circle, fillet, rrect
from kit import asset
from mats import pal, register

register("DK_X61_Walk", "emit", "#55E6B0", 4.5)
register("DK_X61_Clear", "emit", "#FFB624", 4.5)
register("DK_X61_Stop", "emit", "#F2432E", 4.0)
register("DK_X61_BeamWalk", "glow", "#55E6B0", 2.0, 0.26)
register("DK_X61_BeamClear", "glow", "#FFB624", 2.0, 0.26)
register("DK_X61_BeamStop", "glow", "#F2432E", 2.0, 0.26)
register("DK_X61_HaloWalk", "glow", "#55E6B0", 2.0, 0.40)
register("DK_X61_HaloClear", "glow", "#FFB624", 2.0, 0.40)
register("DK_X61_HaloStop", "glow", "#F2432E", 2.0, 0.40)

STATES = ("walk", "clear", "stop", "arrive")      # arrive: the last 3 s of stop, the change back to walk (direction A only in the mock-ups): red, the wait counted down, the ring filling
LIT = {"walk": "X61_Walk", "clear": "X61_Clear", "stop": "X61_Stop", "arrive": "X61_Stop"}
BEAM = {"walk": "X61_BeamWalk", "clear": "X61_BeamClear", "stop": "X61_BeamStop", "arrive": "X61_BeamStop"}
HALO = {"walk": "X61_HaloWalk", "clear": "X61_HaloClear", "stop": "X61_HaloStop", "arrive": "X61_HaloStop"}
CLEAR_FRAC = 0.58   # the share of the clearing time left in the mock-up frames (7 of 12 s)
CLEAR_DIGITS = "07"


def lit(st):
    return getattr(pal, LIT[st])


# ----------------------------------------------------------------------------- flat drawing in a face's local XY (z = the plane, faces point +Z)
def flat(s, poly, z, mat):
    s.face([(x, y, z) for x, y in poly], mat)


def figure(s, kind, z, mat, height, cx=0.0, cy=0.0):
    """SignalEye's pictogram (crossing_eye.figure) at `height` m, its centre at (cx, cy) of the face plane."""
    k = height / 0.35
    old = ce.FIG
    ce.FIG = k
    try:
        with s.at((cx, cy - 0.01 * k, 0)):   # the figure's box is centred at about y = +0.01
            ce.figure(s, kind, z, mat, True)
    finally:
        ce.FIG = old


SEG = {"0": "abcdef", "1": "bc", "2": "abdeg", "3": "abcdg", "4": "bcfg", "5": "acdfg", "6": "acdefg", "7": "abc", "8": "abcdefg", "9": "abcdfg"}


def _hseg(cx, cy, L, t):
    return [(cx - L / 2, cy), (cx - L / 2 + t / 2, cy - t / 2), (cx + L / 2 - t / 2, cy - t / 2), (cx + L / 2, cy), (cx + L / 2 - t / 2, cy + t / 2), (cx - L / 2 + t / 2, cy + t / 2)]


def _vseg(cx, cy, L, t):
    return [(cx, cy - L / 2), (cx + t / 2, cy - L / 2 + t / 2), (cx + t / 2, cy + L / 2 - t / 2), (cx, cy + L / 2), (cx - t / 2, cy + L / 2 - t / 2), (cx - t / 2, cy - L / 2 + t / 2)]


def digit(s, ch, z, mat, h, cx, cy):
    """One seven-segment digit h tall, 0.58 h wide, centred at (cx, cy); hexagonal segments (CCW), a hairline gap between them."""
    w, t = 0.58 * h, 0.17 * h
    Lh, Lv = w - t * 0.95, h / 2 - t * 0.95
    pos = {"a": (0, h / 2 - t / 2, "h"), "g": (0, 0, "h"), "d": (0, -h / 2 + t / 2, "h"),
           "f": (-w / 2 + t / 2, h / 4, "v"), "b": (w / 2 - t / 2, h / 4, "v"), "e": (-w / 2 + t / 2, -h / 4, "v"), "c": (w / 2 - t / 2, -h / 4, "v")}
    for sg in SEG[ch]:
        x, y, o = pos[sg]
        flat(s, _hseg(cx + x, cy + y, Lh, t * 0.92) if o == "h" else _vseg(cx + x, cy + y, Lv, t * 0.92), z, mat)


def digits(s, text, z, mat, h, cx, cy):
    w = 0.58 * h
    step = w + 0.28 * h
    x0 = cx - step * (len(text) - 1) / 2
    for i, ch in enumerate(text):
        digit(s, ch, z, mat, h, x0 + i * step, cy)


def arc_poly(r0, r1, a0, a1, n=4):
    pts0 = [(r0 * math.cos(math.radians(a0 + (a1 - a0) * i / n)), r0 * math.sin(math.radians(a0 + (a1 - a0) * i / n))) for i in range(n + 1)]
    pts1 = [(r1 * math.cos(math.radians(a0 + (a1 - a0) * i / n)), r1 * math.sin(math.radians(a0 + (a1 - a0) * i / n))) for i in range(n + 1)]
    return pts0 + pts1[::-1]


def seg_ring(s, r0, r1, z, n, nlit, mat_on, mat_off, start=90.0):
    """A ring of n wedge segments in the face plane, the first `nlit` lit (clockwise from the top: the clock hand's way), the rest dark."""
    gap = 1.2
    for k in range(n):
        a1 = start - 360.0 * k / n
        a0 = start - 360.0 * (k + 1) / n
        poly = arc_poly(r0, r1, a0 + gap / 2, a1 - gap / 2, 3)
        flat(s, poly, z, mat_on if k < nlit else mat_off)


# ----------------------------------------------------------------------------- A: the floating eye
def eyeA(s, st):
    ZC, RB, HT, PR = 2.55, 0.44, 0.15, 0.31          # hover height, lens radius, half thickness at the faces, plate radius
    on = lit(st)
    char, chrome = pal.Enamel_Charcoal, pal.Chrome
    with s.at((0, 0, ZC), (90, 0, 0)):               # local Z = the display axis (-Y out of face A)
        s.lathe([(PR + 0.0, -HT), (0.37, -0.14), (0.42, -0.08), (RB, 0.0), (0.42, 0.08), (0.37, 0.14), (PR, HT)], chrome, seg=36, cap_bottom=False, cap_top=False)
        s.lathe([(RB - 0.012, -0.05), (RB + 0.022, -0.032), (RB + 0.022, 0.032), (RB - 0.012, 0.05)], on, seg=36, cap_bottom=False, cap_top=False)   # the rim light, all the way round
    for rot in ((90, 0, 0), (90, 0, 180)):
        with s.at((0, 0, ZC), rot):
            s.lathe([(PR, HT), (0, HT)], char, seg=28, cap_bottom=False)
            if st in ("clear", "arrive"):
                figure(s, "walk" if st == "clear" else "stop", HT + 0.004, on, 0.20, 0.0, 0.17)
                digits(s, CLEAR_DIGITS if st == "clear" else "03", HT + 0.004, on, 0.20, 0.0, -0.10)
            else:
                figure(s, "walk" if st == "walk" else "stop", HT + 0.004, on, 0.46, 0.0, 0.0)
    with s.at((0, 0, ZC)):                            # the nozzle under the lens
        s.lathe([(0.05, -(RB + 0.01)), (0.07, -(RB - 0.04)), (0.12, -(RB - 0.14))], chrome, seg=14, cap_bottom=False, cap_top=False)
        s.lathe([(0.07, RB - 0.02), (0.04, RB + 0.035), (0, RB + 0.1)], chrome, seg=10, cap_bottom=False)
    zb, zt = 0.012, ZC - (RB + 0.02)
    with s.part("Beam") as b:
        b.sheet(lambda u, v: ((0.17 - 0.125 * v) * math.cos(2 * math.pi * u), (0.17 - 0.125 * v) * math.sin(2 * math.pi * u), zb + (zt - zb) * v), 10, 1, getattr(pal, BEAM[st]))
    with s.part("Halo") as h:                         # the clock in the paving: 24 segments, all lit in walk, draining in clear, red full in stop
        n = 24
        nlit = {"walk": n, "clear": round(n * CLEAR_FRAC), "stop": n, "arrive": 3}[st]
        seg_ring(h, 0.47, 0.60, 0.006, n, nlit, on, pal.Enamel_Charcoal)
        flat(h, circle(0.47, 28), 0.005, getattr(pal, HALO[st]))


# ----------------------------------------------------------------------------- B: the kerb pylon
def pylonB(s, st):
    on = lit(st)
    chrome, ivory, char = pal.Chrome, pal.Enamel_Ivory, pal.Enamel_Charcoal
    s.lathe([(0.27, 0.0), (0.27, 0.05), (0.21, 0.10), (0.11, 0.17), (0.075, 0.32), (0.07, 2.0)], ivory, seg=28, cap_bottom=False, cap_top=False)
    for sx in (-1, 1):                                 # the two side fins, a rocket's: ivory enamel, 24 mm thick
        with s.at((0, 0, 0), (90, 0, 0)):
            fin = fillet([(sx * 0.07, 0.15), (sx * 0.30, 0.10), (sx * 0.31, 0.14), (sx * 0.075, 1.05)][::sx], 0.05, 4)
            s.extrude(fin, -0.012, 0.012, ivory)
    s.rbox((-0.27, -0.19, 1.95), (0.27, 0.19, 3.0), 0.09, chrome)
    for sy, rot in ((-1, (90, 0, 0)), (1, (90, 0, 180))):
        with s.at((0, 0, 2.48), rot):
            zf = 0.19 + 0.004
            flat(s, rrect(0.40, 0.90, 0.17, 6), zf, char)
            if st == "clear":
                figure(s, "walk", zf + 0.004, on, 0.30, 0.0, 0.22)
            else:
                figure(s, "walk" if st == "walk" else "stop", zf + 0.004, on, 0.34, 0.0, 0.22)
            # the station clock: a dial with 24 sectors; the lit ones are the time left
            n = 24
            nlit = {"walk": n, "clear": round(n * CLEAR_FRAC), "stop": 0}[st]
            flat(s, circle(0.178, 24, 0.0, -0.20), zf + 0.003, pal.Chrome)       # the dial's chrome bezel
            flat(s, circle(0.168, 24, 0.0, -0.20), zf + 0.004, char)
            with s.at((0, -0.20, 0)):
                seg_ring(s, 0.05, 0.158, zf + 0.007, n, nlit, on, pal.Enamel_Teal)    # lit = time left; spent sectors fall back to the deep teal enamel
                flat(s, circle(0.03, 12), zf + 0.010, pal.Chrome)
    for sx in (-1, 1):                                  # edge lights up the head's two sides: the state reads edge-on from any yaw
        s.rbox((sx * 0.262 - 0.012, -0.10, 2.05), (sx * 0.262 + 0.012, 0.10, 2.90), 0.01, on)
    s.rbox((-0.31, -0.25, 3.0), (0.31, 0.25, 3.07), 0.04, chrome)   # the visor's roof
    s.lathe([(0.0, 3.07), (0.05, 3.08), (0.04, 3.14), (0, 3.18)], chrome, seg=10)


# ----------------------------------------------------------------------------- C: the kerb lantern and the pictogram disc in the paving
def lanternC(s, st):
    on = lit(st)
    chrome, ivory, char = pal.Chrome, pal.Enamel_Ivory, pal.Enamel_Charcoal
    s.lathe([(0.24, 0.0), (0.24, 0.05), (0.19, 0.12), (0.16, 0.34)], ivory, seg=28, cap_bottom=False, cap_top=False)
    s.lathe([(0.16, 0.34), (0.18, 0.55), (0.2, 0.75), (0.19, 0.84), (0.14, 0.95), (0.06, 1.01), (0, 1.03)], chrome, seg=28, cap_bottom=False, cap_top=False)
    n = 24
    nlit = {"walk": n, "clear": round(n * CLEAR_FRAC), "stop": 0}[st]
    ra, zr0, zr1 = 0.202, 0.78, 0.83
    for k in range(n):                                  # the crown ring: segments round the capsule, clockwise from the front
        a0 = math.radians(-90 + 360.0 * k / n + 0.6)
        a1 = math.radians(-90 + 360.0 * (k + 1) / n - 0.6)
        P = lambda a, z: (ra * math.cos(a), ra * math.sin(a), z)  # noqa: E731
        s.face([P(a0, zr0), P(a1, zr0), P(a1, zr1), P(a0, zr1)], on if k < nlit else char)
    s.rbox((-0.13, -0.20, 0.42), (0.13, 0.20, 0.68), 0.06, chrome)
    for rot in ((90, 0, 0), (90, 0, 180)):
        with s.at((0, 0, 0.55), rot):
            flat(s, circle(0.105, 20), 0.2 + 0.004, char)
            figure(s, "walk" if st != "stop" else "stop", 0.2 + 0.008, on, 0.14, 0.0, 0.0)
    with s.part("Disc") as d:                            # the readout in the paving, upright for the person waiting here: its up is local -Y (across the road)
        with d.at((0, 0.45, 0), (0, 0, 180)):            # centred 0.45 m behind the lantern; local +Y of this frame points across the road
            flat(d, circle(0.78, 40), 0.004, char)
            figure(d, "walk" if st != "stop" else "stop", 0.008, on, 0.62, 0.0, 0.0)
            seg_ring(d, 0.60, 0.72, 0.008, 24, {"walk": 24, "clear": round(24 * CLEAR_FRAC), "stop": 24}[st], on, char)


for _d, _fn in (("A", eyeA), ("B", pylonB), ("C", lanternC)):
    for _st in STATES:
        def _make(fn=_fn, st=_st):
            def build(s):
                fn(s, st)
            return build
        asset(f"X61{_d}_{_st}", "Signals", bevel=0.0)(_make())
