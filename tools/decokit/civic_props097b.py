"""Brief 097 pass 2 (Vela, 2026-11-06): play-zoom rebuilds of the Handover zone's weakest store pieces, to the look bar (paid/097-look-bar/codex/office-y210-bar.png,
square-y030-bar.png): palette enamels, a honed stone cap or base, brass hairlines, rounded streamline forms. Each keeps its store piece's size exactly (the
capstone programme places lockers and vending machines at the size their piece states, the bin slot fits to it), so siting is unchanged; the world takes them
by tag (tools/kit/tags.ts) or by the programme's piece id (capstone-rules.ts), and the store pieces keep a retired tag.

Lockers_Play             Street  2.49 x 0.688 x 1.9 m (Lockers): a teal enamel locker bank with rounded ends on a limestone plinth and brass hairline, twelve
                                 doors with chrome pulls, an ivory control column with a chrome dial and a lamp, a honed limestone bullnose cap.
RecyclingBins_Play       Street  1.06 x 0.42 x 0.89 m (RecyclingBins): twin streamline drums (teal and seafoam) on one limestone plinth, ivory domed lids with
                                 chrome mouth rings, chrome bands and a brass hairline at the foot.
VendingMachine_Cola_Play Street  0.953 x 0.832 x 1.978 m: an ivory cabinet with a round-shouldered top, a coral band and lower panel, a dark goods window in a chrome
VendingMachine_Snack_Play        bezel, a chrome coin plate and dial, a dispensing hatch; the snack twin (1.003 x 0.85 x 1.86 m) in teal with an ivory band. No lettering.
Pivot = footprint centre at the foot, front -Y. Palette materials only."""

import math

import mats
from civic_props097 import rband, rloft, ring_prof
from geo import arc, circle, dome, fillet, rrect
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



# ----------------------------------------------------------------------------- lockers
LW, LD, LH = 2.49, 0.688, 1.9


@asset("Lockers_Play", "Street", bevel=0.0)
def lockers_play(s):
    rloft(s, [(LW, LD, 0.14, 0.0), (LW, LD, 0.14, 0.075), (LW - 0.02, LD - 0.02, 0.13, 0.095), (LW - 0.06, LD - 0.06, 0.12, 0.1)], pal.Limestone, n=4)  # plinth
    bw, bd, br = LW - 0.08, LD - 0.1, 0.2
    rband(s, bw, bd, br, 0.1, 0.114, pal.Brass_Civic, n=4, lip=0.005)                                     # brass hairline at the foot
    rloft(s, [(bw, bd, br, 0.114), (bw, bd, br, 1.76)], pal.Enamel_Cobalt, n=4, cap_top=False)               # the teal bank, rounded ends
    rloft(s, [(bw - 0.01, bd - 0.01, br, 1.755), (LW, LD, 0.24, 1.78), (LW, LD, 0.24, 1.86), (LW - 0.04, LD - 0.04, 0.22, 1.89),
              (LW - 0.16, LD - 0.16, 0.16, LH)], pal.Limestone, n=4)                                      # the honed limestone bullnose cap
    yf = -bd / 2
    cols = [-0.8, -0.42, 0.42, 0.8]                                                                       # door columns either side of the control column (on the flat front)
    with s.at((0, yf, 0), FRONT):
        for x in cols:
            for k, (z0, z1) in enumerate(((0.2, 0.68), (0.72, 1.2), (1.24, 1.7))):
                with s.at((x, (z0 + z1) / 2, 0.0)):
                    slab(s, 0.34, z1 - z0, 0.05, 0.0, 0.016, pal.Enamel_Teal__boulevard if x == 0.42 else pal.Enamel_Cobalt, e=0.006, n=2, m=1)
                    s.sweep([(0.11, -0.06, 0.02), (0.12, -0.03, 0.03), (0.12, 0.03, 0.03), (0.11, 0.06, 0.02)], 0.008, pal.Chrome, seg=5)   # pull
        with s.at((0.0, 0.95, 0.0)):                                                                      # the ivory control column
            slab(s, 0.3, 1.52, 0.1, 0.0, 0.02, pal.Enamel_Ivory, e=0.008, n=4, m=2)
            with s.at((0, 0.42, 0.02)):
                s.lathe(puck(0.07, 0.0, 0.018, 0.006, n=1), pal.Chrome, seg=16)                            # the dial
                s.lathe(puck(0.052, 0.012, 0.022, 0.003, n=1), pal.Enamel_Charcoal, seg=16)
            with s.at((0, 0.66, 0.02)):
                s.lathe([(0.026, 0.0)] + dome(0.026, 0.004, 0.02, n=3)[1:], pal.Lamp, seg=10, cap_bottom=False)  # the lamp
            for y in (0.12, -0.08, -0.28):                                                                # chrome key plates
                with s.at((0, y, 0.02)):
                    slab(s, 0.12, 0.06, 0.02, 0.0, 0.008, pal.Chrome, e=0.003, n=2, m=1)
    s.solid((-LW / 2, -LD / 2, 0), (LW / 2, LD / 2, LH))


# ----------------------------------------------------------------------------- recycling bins
BR, BX = 0.19, 0.27


def _bin(s, mat):
    s.lathe([(BR - 0.01, 0.1), (BR, 0.11), (BR, 0.66), (BR - 0.006, 0.68)], mat, seg=20, cap_bottom=False, cap_top=False)               # the drum
    for z in (0.16, 0.6):
        s.lathe(ring_prof(BR, BR + 0.008, z, z + 0.018), pal.Chrome, seg=20, cap_bottom=False, cap_top=False)                         # chrome bands
    s.lathe([(BR - 0.006, 0.68), (BR + 0.006, 0.69)] + dome(BR + 0.006, 0.69, 0.19, n=5)[1:], pal.Enamel_Ivory, seg=20, cap_bottom=False)  # domed lid
    with s.at((0, -0.135, 0.79), (62, 0, 0)):                                                              # the mouth, tilted up the dome's front
        s.lathe(ring_prof(0.07, 0.085, -0.01, 0.025), pal.Chrome, seg=14, cap_bottom=False, cap_top=False)
        s.lathe([(0.07, 0.01), (0.0, 0.0)], pal.Enamel_Charcoal, seg=14, cap_bottom=False, cap_top=False)
    s.lathe(puck(0.022, 0.875, 0.89, 0.006, n=1), pal.Chrome, seg=8)                                      # the knob


@asset("RecyclingBins_Play", "Street", bevel=0.0)
def recycling_bins_play(s):
    rloft(s, [(1.06, 0.42, 0.2, 0.0), (1.06, 0.42, 0.2, 0.06), (1.02, 0.4, 0.19, 0.08), (0.98, 0.38, 0.18, 0.085)], pal.Limestone, n=5)   # plinth
    for x, mat in ((-BX, pal.Enamel_Teal), (BX, pal.Enamel_Teal__southside)):
        with s.at((x, 0, 0)):
            s.lathe(ring_prof(BR - 0.012, BR - 0.006, 0.085, 0.1), pal.Brass_Civic, seg=20, cap_bottom=False, cap_top=False)            # brass hairline
            _bin(s, mat)
    s.sweep([(-BX + BR, 0, 0.42), (BX - BR, 0, 0.42)], 0.016, pal.Chrome, seg=6)                           # the chrome tie between them
    s.solid((-0.53, -0.21, 0), (0.53, 0.21, 0.89))


# ----------------------------------------------------------------------------- vending machines
def _outline(w, h, r, n=6):
    """Front elevation, CCW: square feet, round shoulders of radius r."""
    return [(-w / 2, 0.0), (w / 2, 0.0)] + arc(w / 2 - r, h - r, r, 0, 90, n) + arc(-w / 2 + r, h - r, r, 90, 180, n)


def _vending(s, W, D, H, body, band, low):
    """W x D x H is the store piece's size: the body is cut so the hairline, panels and front hardware stay inside it."""
    w, d, h, c, z0 = W - 0.012, D - 0.03, H - 0.105, 0.009, 0.09   # the crown lamp stands 0.105 m over the body
    with s.at((0, c, 0)):
        _vbody(s, w, d, h, z0, body, band, low)
    s.solid((-W / 2, -D / 2, 0), (W / 2, D / 2, H))


def _vbody(s, w, d, h, z0, body, band, low):
    s.extrude(rrect(w - 0.08, d - 0.1, 0.05, 3), 0.0, z0, pal.Enamel_Charcoal)                             # recessed kick plinth
    with s.at((0, 0, z0), FRONT):   # local x across, y up, z = -Y: the body as an extrusion of its front elevation through the depth
        ol = [p[:2] for p in fillet([(x, y) for x, y in _outline(w, h - z0, 0.26)], 0.03, n=2)]
        e, hh = 0.035, h - z0
        sec = lambda t: [(x * (w - 2 * e * t) / w, y * (hh - e * t) / hh) for x, y in ol]  # noqa: E731  inset by e*t: the rounded front and back rims
        rims = [(1 - math.sin(math.pi / 2 * k / 3), k) for k in range(4)]
        s.loft([(sec(t), -d / 2 + e * (1 - math.cos(math.pi / 2 * k / 3))) for t, k in rims] + [(sec(t), d / 2 - e * (1 - math.cos(math.pi / 2 * k / 3))) for t, k in rims[::-1]],
               body, smooth=True)
        s.extrude([(-w / 2 - 0.006, 0.0), (w / 2 + 0.006, 0.0), (w / 2 + 0.006, 0.014), (-w / 2 - 0.006, 0.014)], -d / 2 - 0.006, d / 2 + 0.006, pal.Brass_Civic)   # brass hairline at the foot
        s.box((-w / 2 - 0.004, 0.014, -d / 2 - 0.004), (w / 2 + 0.004, 0.52, d / 2 + 0.004), low)          # the lower panel
        s.box((-w / 2 - 0.006, h - z0 - 0.52, -d / 2 - 0.006), (w / 2 + 0.006, h - z0 - 0.4, d / 2 + 0.006), band)   # the band round the shoulders
        ww, wh = w * 0.6, h * 0.42
        with s.at((-0.08, h * 0.56, d / 2)):                                                              # the goods window: chrome bezel, shelves of goods
            slab(s, ww, wh, 0.06, 0.0, 0.02, pal.Chrome, e=0.006, n=3, m=1)
            slab(s, ww - 0.04, wh - 0.04, 0.045, 0.014, 0.022, pal.Enamel_Charcoal, e=0.003, n=3, m=1)
            s.box((-ww / 2 + 0.04, wh / 2 - 0.05, 0.022), (ww / 2 - 0.04, wh / 2 - 0.035, 0.03), pal.Lamp)                     # the light over the shelves
            goods = (pal.Enamel_Coral, pal.Enamel_Mustard, pal.Enamel_Seafoam, pal.Enamel_Ivory)
            for r in range(3):
                y = -wh / 2 + 0.06 + r * (wh - 0.14) / 3
                s.sweep([(-ww / 2 + 0.03, y, 0.03), (ww / 2 - 0.03, y, 0.03)], 0.006, pal.Chrome, seg=4, caps=False)          # shelf lip
                for k in range(4):
                    x = -ww / 2 + 0.06 + k * (ww - 0.12) / 3
                    s.box((x - 0.035, y + 0.008, 0.022), (x + 0.035, y + 0.09, 0.034), goods[(k + r) % 4])                  # the goods
        with s.at((w / 2 - 0.13, h * 0.56, d / 2)):                                                       # the coin plate and dial
            slab(s, 0.13, 0.5, 0.04, 0.0, 0.014, pal.Chrome, e=0.004, n=2, m=1)
            with s.at((0, 0.1, 0.014)):
                s.lathe(puck(0.04, 0.0, 0.014, 0.005, n=1), pal.Enamel_Ivory, seg=12)
            with s.at((0, -0.12, 0.014)):
                s.lathe(puck(0.025, 0.0, 0.012, 0.004, n=1), pal.Lamp, seg=10)
        with s.at((0, 0.3, d / 2)):                                                                       # the dispensing hatch
            slab(s, w * 0.5, 0.14, 0.04, 0.0, 0.016, pal.Chrome, e=0.005, n=2, m=1)
            slab(s, w * 0.44, 0.09, 0.03, 0.012, 0.02, pal.Enamel_Charcoal, e=0.003, n=2, m=1)
        s.sweep([(0, h - z0 + 0.004, -d / 2 + 0.06), (0, h - z0 + 0.004, d / 2 - 0.06)], 0.014, pal.Chrome, seg=6)   # the chrome crown ridge
        with s.at((0, h - z0 - 0.02, 0.0), (-90, 0, 0)):                                                  # the crown lamp: a chrome cup and an opal ball
            s.lathe(puck(0.07, 0.0, 0.035, 0.012, n=2), pal.Chrome, seg=12)
            s.lathe([(0.0, 0.03), (0.05, 0.045)] + [(0.05 * math.cos(a), 0.075 + 0.05 * math.sin(a)) for a in (0.6, 1.1, 1.571)], pal.Lamp, seg=12, cap_bottom=False)


@asset("VendingMachine_Cola_Play", "Street", bevel=0.0)
def vending_cola_play(s):
    _vending(s, 0.9532, 0.832, 1.978, pal.Enamel_Ivory, pal.Enamel_Vermilion, pal.Enamel_Vermilion)


@asset("VendingMachine_Snack_Play", "Street", bevel=0.0)
def vending_snack_play(s):
    _vending(s, 1.0031, 0.85, 1.86, pal.Enamel_Teal__southside, pal.Enamel_Ivory, pal.Enamel_Sunshine)


# ----------------------------------------------------------------------------- pass 3: the twin phone booth's finish, the potted topiary's pot
# (the boulevard coral is registered above): the kit's colourway enamel (colourways.py BOULEVARD_FASCIA), no new colour


@asset("PhoneBooth_Play", "Booths", bevel=0.0)
def phone_booth_play(s):
    """The twin-pod booth Sky asked back (booths.booth_body, the store PhoneBooth's builder, same size and hoods), finished to the bar: the drum off the
    saturated coral onto the boulevard coral, thicker chrome rims at the drum's foot and shoulder, a brass hairline foot ring on a honed limestone plinth."""
    from booths import booth_body
    booth_body(s)
    old = mats.get("DK_Enamel_Coral")
    if old in s.mats:
        s.mats[s.mats.index(old)] = pal.Enamel_Teal__boulevard
    s.lathe([(0.0, 0.0), (0.612, 0.0), (0.612, 0.07), (0.606, 0.088), (0.596, 0.097), (0.0, 0.1)], pal.Limestone, seg=48)   # honed limestone plinth (over the kick ring)
    s.lathe(ring_prof(0.578, 0.588, 0.1, 0.114), pal.Brass_Civic, seg=48, cap_bottom=False, cap_top=False)                  # the brass hairline foot ring
    s.lathe(ring_prof(0.578, 0.6, 0.118, 0.172), pal.Chrome, seg=48, cap_bottom=False, cap_top=False)                          # thicker chrome rims
    s.lathe(ring_prof(0.578, 0.598, 0.885, 0.94), pal.Chrome, seg=48, cap_bottom=False, cap_top=False)


@asset("PottedTopiary_Play", "Plants", bevel=0.0)   # loaded after restore026 (kit.FAMILIES), so this pot replaces the store copy's; the topiary is unchanged
def potted_topiary_play(s):
    """A cream enamel pot with a bellied streamline profile, a teal band, a chrome rolled rim and a brass hairline over a honed limestone foot; the store
    PottedTopiary's topiary exactly (same seed, same z0, h, r, size), its soil disc where it was."""
    import random
    from plants import Cards, topiary
    rng = random.Random(4707)
    cd = Cards(s)
    s.lathe([(0.0, 0.0), (0.3, 0.0), (0.31, 0.012), (0.31, 0.04), (0.296, 0.052), (0.0, 0.052)], pal.Limestone, seg=40)                 # stone foot
    s.lathe(ring_prof(0.286, 0.294, 0.052, 0.064), pal.Brass_Civic, seg=40, cap_bottom=False, cap_top=False)                          # brass hairline
    body = [(0.284, 0.06), (0.3, 0.12), (0.325, 0.25), (0.336, 0.38), (0.33, 0.5), (0.322, 0.53)]
    s.lathe(body, pal.Enamel_Ivory, seg=40, cap_bottom=False, cap_top=False)                                                          # the cream (ivory) pot: DK_Enamel_Cream is not in the civic look palette (CD-4)
    s.lathe([(0.3, 0.13), (0.314, 0.15), (0.33, 0.25), (0.336, 0.29), (0.329, 0.29)], pal.Enamel_Teal, seg=40, cap_bottom=False, cap_top=False)  # teal band
    s.lathe([(0.32, 0.525), (0.338, 0.535), (0.348, 0.555), (0.344, 0.575), (0.33, 0.582), (0.312, 0.575), (0.305, 0.56)], pal.Chrome, seg=40,
            cap_bottom=False, cap_top=False)                                                                                         # chrome rolled rim
    s.extrude(circle(0.306, 40), 0.54, 0.575, pal.Soil)
    topiary(cd, rng, z0=0.56, h=1.38, r=0.37, size=0.33)
