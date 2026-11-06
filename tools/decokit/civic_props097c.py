"""Brief 097 full pass (Vela, 2026-11-06): the zone audit's weakest buildable street pieces (briefs/097/vela/audit/audit.md), rebuilt to the look bar at their
store pieces' sizes (siting and collide footprints unchanged), palette materials only, AO baked (vertex_ao.PIECES). Concepts: paid/097-props/codex/ pass 2.

Hydrant_Play         Street  0.43 x 0.42 x 0.79 m (Hydrant): a streamline boulevard-coral body on a limestone foot, brass hairline, chrome dome cap, collar and outlet caps.
BikeRack_Play        Street  1.38 x 0.94 x 0.85 m (BikeRack): a limestone plinth with a teal enamel rail and brass hairline, four chrome hoops on stone pads.
Bench_Play           Street  1.88 x 0.585 x 0.86 m (Bench): two ivory seat shells with teal backs on chrome frames between honed limestone end plinths.
UtilityCabinet_Play  Booths  1.29 x 0.635 x 1.28 m (UtilityCabinet): a rounded teal cabinet on a limestone plinth and brass hairline, two ivory louvred doors,
                             chrome dial and handles, a limestone bullnose cap.
PhoneBooth_Single    Booths  the one-hood booth (restore026's booth_body, single): the finish PhoneBooth_Play has (boulevard-coral drum, chrome rims, brass ring, stone plinth).
Pivot = footprint centre at the foot, front -Y."""

import math

import mats
from civic_props097 import rband, rloft, ring_prof
from geo import dome
from kit import asset
from mats import pal, register
from signals import FRONT, puck, slab

register("DK_Brass_Civic", "brass", mats.BRASS)
# Sky 2026-11-06 ("why you reduce the colors ... converging on blue"): the old town's colourway enamels, each piece in its old colour family (no new material where a colourway .mat exists)
register("DK_Enamel_Teal__boulevard", "enamel", "#C8553D")   # boulevard coral
register("DK_Enamel_Teal__southside", "enamel", "#D9962B")   # southside marigold
register("DK_Enamel_Seafoam__gardens", "enamel", "#8FA88A")  # gardens sage
register("DK_Enamel_Cobalt", "enamel", "#2F84E8")             # the old town's cobalt (first-wins: the kit row stands)
register("DK_Enamel_Sunshine", "enamel", "#FDEB8C")

register("DK_Enamel_Teal__boulevard", "enamel", "#C8553D")   # the boulevard coral (civic_props097b.py)


# ----------------------------------------------------------------------------- hydrant
@asset("Hydrant_Play", "Street", bevel=0.0)
def hydrant_play(s):
    s.lathe([(0.0, 0.0), (0.2, 0.0), (0.2, 0.025), (0.19, 0.04), (0.0, 0.042)], pal.Limestone, seg=28)                          # stone foot
    s.lathe(ring_prof(0.15, 0.158, 0.04, 0.05), pal.Brass_Civic, seg=24, cap_bottom=False, cap_top=False)                     # brass hairline
    body = [(0.155, 0.04), (0.16, 0.07), (0.13, 0.12), (0.112, 0.2), (0.108, 0.5), (0.118, 0.56), (0.13, 0.6)]
    s.lathe(body, pal.Enamel_Teal__boulevard, seg=24, cap_bottom=False, cap_top=False)                                        # the coral body
    s.lathe(ring_prof(0.125, 0.142, 0.585, 0.635), pal.Chrome, seg=24, cap_bottom=False, cap_top=False)                       # the bolt collar
    s.lathe([(0.13, 0.6), (0.13, 0.64)] + dome(0.13, 0.64, 0.11, n=5)[1:], pal.Chrome, seg=24, cap_bottom=False)                  # the chrome dome cap
    s.lathe(puck(0.05, 0.72, 0.79, 0.02, n=2), pal.Chrome, seg=16)                                                           # chrome cap nut
    for x in (-1, 1):                                                                                                       # side outlets
        with s.at((x * 0.11, 0, 0.42), (0, x * 90, 0)):
            s.lathe([(0.045, -0.02), (0.045, 0.06), (0.05, 0.065)], pal.Enamel_Teal__boulevard, seg=14, cap_bottom=False, cap_top=False)
            s.lathe(puck(0.055, 0.06, 0.1, 0.012, n=2), pal.Chrome, seg=14)
    with s.at((0, -0.105, 0.33), FRONT):                                                                                   # the front pumper outlet
        s.lathe([(0.06, -0.02), (0.06, 0.04), (0.068, 0.05)], pal.Enamel_Teal__boulevard, seg=16, cap_bottom=False, cap_top=False)
        s.lathe(puck(0.075, 0.045, 0.1, 0.015, n=2), pal.Chrome, seg=16)
    s.solid((-0.2, -0.2, 0), (0.2, 0.2, 0.79))


# ----------------------------------------------------------------------------- bike rack
@asset("BikeRack_Play", "Street", bevel=0.0)
def bike_rack_play(s):
    rloft(s, [(1.38, 0.62, 0.3, 0.0), (1.38, 0.62, 0.3, 0.07), (1.35, 0.59, 0.28, 0.09), (1.3, 0.54, 0.26, 0.1)], pal.Limestone, n=5)   # the honed plinth
    rband(s, 1.36, 0.6, 0.29, 0.03, 0.042, pal.Brass_Civic, n=5, lip=0.004)                                                      # brass hairline
    rloft(s, [(1.24, 0.14, 0.07, 0.1), (1.24, 0.14, 0.07, 0.15), (1.2, 0.1, 0.05, 0.17)], pal.Enamel_Cobalt, n=4)                 # the teal rail
    for x in (-0.48, -0.16, 0.16, 0.48):                                                                                    # four chrome hoops over the rail
        arch = [(x, -0.24 * math.cos(a), 0.6 + 0.24 * math.sin(a)) for a in (math.pi * k / 10 for k in range(1, 10))]
        s.sweep([(x, -0.24, 0.1), (x, -0.24, 0.6)] + arch + [(x, 0.24, 0.6), (x, 0.24, 0.1)], 0.022, pal.Chrome, seg=8)
        for y in (-0.24, 0.24):
            with s.at((x, y, 0.1)):
                s.lathe(puck(0.04, 0.0, 0.02, 0.008, n=1), pal.Chrome, seg=10)                                               # foot collars
    s.solid((-0.69, -0.31, 0), (0.69, 0.31, 0.17))


# ----------------------------------------------------------------------------- bench
@asset("Bench_Play", "Street", bevel=0.0)
def bench_play(s):
    with s.at((0, -0.07, 0)):   # the store Bench's footprint (y -0.34..0.25: the back stands over the plinths' rear)
        for x in (-0.88, 0.88):                                                                                                 # honed limestone end plinths
            with s.at((x, 0.02, 0)):
                rloft(s, [(0.12, 0.52, 0.05, 0.0), (0.12, 0.52, 0.05, 0.4), (0.1, 0.5, 0.04, 0.42)], pal.Limestone, n=3)
                rband(s, 0.12, 0.52, 0.05, 0.04, 0.052, pal.Brass_Civic, n=3, lip=0.003)
        s.sweep([(-0.82, 0.0, 0.38), (0.82, 0.0, 0.38)], 0.022, pal.Chrome, seg=8)                                              # chrome frame rail
        s.sweep([(0.0, -0.15, 0.022), (0.0, -0.05, 0.36), (0.0, 0.05, 0.36), (0.0, 0.15, 0.022)], 0.022, pal.Chrome, seg=8)         # centre leg
        for x in (-0.41, 0.41):
            s.rbox((x - 0.4, -0.26, 0.4), (x + 0.4, 0.18, 0.46), 0.025, pal.Enamel_Teal__boulevard, seg=2)                                # seat shell
            s.rbox((x - 0.4, -0.27, 0.43), (x + 0.4, -0.2, 0.47), 0.02, pal.Enamel_Teal__boulevard, seg=2)                                # its rolled front lip
            with s.at((x, 0.2, 0.46), (-14, 0, 0)):
                s.rbox((-0.4, -0.03, 0.0), (0.4, 0.03, 0.385), 0.025, pal.Enamel_Teal__boulevard, seg=2)                                     # teal back
        s.sweep([(-0.86, 0.12, 0.42), (-0.85, 0.27, 0.8), (-0.8, 0.29, 0.845), (0.8, 0.29, 0.845), (0.85, 0.27, 0.8), (0.86, 0.12, 0.42)], 0.016, pal.Chrome, seg=8)  # the back frame
        for x in (-0.86, 0.86):
            s.sweep([(x, -0.22, 0.42), (x, -0.21, 0.6), (x, -0.15, 0.63), (x, 0.18, 0.63)], 0.014, pal.Chrome, seg=6)        # arm loops
    s.solid((-0.94, -0.34, 0), (0.94, 0.2, 0.46))


# ----------------------------------------------------------------------------- utility cabinet
UW, UD, UH = 1.29, 0.635, 1.28


@asset("UtilityCabinet_Play", "Booths", bevel=0.0)
def utility_cabinet_play(s):
    rloft(s, [(UW, UD, 0.12, 0.0), (UW, UD, 0.12, 0.06), (UW - 0.02, UD - 0.02, 0.11, 0.075), (UW - 0.05, UD - 0.05, 0.1, 0.08)], pal.Limestone, n=4)
    bw, bd = UW - 0.07, UD - 0.08
    rband(s, bw, bd, 0.14, 0.08, 0.092, pal.Brass_Civic, n=4, lip=0.004)
    rloft(s, [(bw, bd, 0.14, 0.092), (bw, bd, 0.14, 1.15)], pal.Enamel_Powder, n=4, cap_top=False)
    rloft(s, [(bw - 0.01, bd - 0.01, 0.14, 1.145), (UW, UD, 0.17, 1.17), (UW, UD, 0.17, 1.24), (UW - 0.04, UD - 0.04, 0.15, 1.27),
              (UW - 0.14, UD - 0.14, 0.1, UH)], pal.Limestone, n=4)                                                       # the bullnose cap
    with s.at((0, -bd / 2, 0), FRONT):
        for x in (-0.28, 0.28):
            with s.at((x, 0.62, 0.0)):
                slab(s, 0.5, 0.92, 0.06, 0.0, 0.018, pal.Enamel_Ivory, e=0.006, n=3, m=1)                                    # ivory door
                for k in range(5):                                                                                          # louvres
                    s.box((-0.17, 0.12 + 0.05 * k, 0.018), (0.17, 0.135 + 0.05 * k, 0.03), pal.Enamel_Mustard)  # mustard: civic-look.json draws the civic cabinets' mustard as seafoam
                hx = -0.2 if x > 0 else 0.2                                                                                  # the handle at the doors' meeting edge
                s.sweep([(hx, -0.08, 0.025), (hx, 0.08, 0.025)], 0.01, pal.Chrome, seg=5)
        s.box((-0.025, 0.12, 0.0), (0.025, 1.08, 0.022), pal.Enamel_Teal__southside)                                       # the marigold strip between the doors (the old orange)
        with s.at((0, 0.9, 0.0)):
            s.lathe(puck(0.06, 0.0, 0.02, 0.007, n=1), pal.Chrome, seg=16)                                                     # the dial
            s.lathe(puck(0.045, 0.014, 0.024, 0.003, n=1), pal.Enamel_Ivory, seg=16)
    s.solid((-UW / 2, -UD / 2, 0), (UW / 2, UD / 2, UH))


# ----------------------------------------------------------------------------- the one-hood booth's finish
@asset("PhoneBooth_Single", "Booths", bevel=0.0)   # loaded after restore026 (kit.FAMILIES): same builder, same id, the PhoneBooth_Play finish
def phone_booth_single(s):
    from booths import booth_body
    booth_body(s, single=True)
    old = mats.get("DK_Enamel_Coral")
    if old in s.mats:
        s.mats[s.mats.index(old)] = pal.Enamel_Teal__boulevard
    s.lathe([(0.0, 0.0), (0.612, 0.0), (0.612, 0.07), (0.606, 0.088), (0.596, 0.097), (0.0, 0.1)], pal.Limestone, seg=48)
    s.lathe(ring_prof(0.578, 0.588, 0.1, 0.114), pal.Brass_Civic, seg=48, cap_bottom=False, cap_top=False)
    s.lathe(ring_prof(0.578, 0.6, 0.118, 0.172), pal.Chrome, seg=48, cap_bottom=False, cap_top=False)
    s.lathe(ring_prof(0.578, 0.598, 0.885, 0.94), pal.Chrome, seg=48, cap_bottom=False, cap_top=False)


# ----------------------------------------------------------------------------- the civic sphere bollard (same id, same profile and height)
@asset("BollardSphere", "Street", bevel=0.0, lean=0.75)   # loaded after civic_street (kit.FAMILIES): the bar's ivory post, not an all-chrome one
def bollard_sphere(s):
    """The civic bollard's own profile (civic_street.py), finished to the bar: a honed limestone foot with a brass hairline, an ivory enamel post, a seafoam
    band, a chrome collar and the chrome sphere top."""
    from civic_street import sphere
    s.lathe([(0, 0), (0.21, 0), (0.21, 0.03), (0.15, 0.055), (0.0, 0.056)], pal.Limestone, seg=20)                               # stone foot
    s.lathe(ring_prof(0.148, 0.156, 0.05, 0.062), pal.Brass_Civic, seg=20, cap_bottom=False, cap_top=False)                    # brass hairline
    s.lathe([(0.15, 0.055), (0.105, 0.085), (0.105, 0.72)], pal.Enamel_Ivory, seg=20, cap_bottom=False, cap_top=False)        # ivory post
    s.lathe([(0.105, 0.72), (0.125, 0.735), (0.125, 0.775), (0.09, 0.8)], pal.Chrome, seg=20, cap_bottom=False, cap_top=False) # chrome collar
    s.lathe(ring_prof(0.104, 0.114, 0.42, 0.58), pal.Enamel_Teal__boulevard, seg=20, cap_bottom=False, cap_top=False)                 # seafoam band
    sphere(s, (0, 0, 0.945), 0.145, pal.Chrome, seg=20, n=8)
