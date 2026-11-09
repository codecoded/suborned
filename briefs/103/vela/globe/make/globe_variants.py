"""Brief 103 (Vela, 2026-11-09): scratch variants of the Records Office globe (RecordsEntrance, tools/decokit/civic_records.py). Renders only.

Nothing here changes the adopted piece, its export or its kit row. build(variant) swaps civic_records.globe for one kit.build("RecordsEntrance")
in this Blender session and puts it back; every other part of the entrance (window, blades, marquee, lanterns, doors, steps, piers) is the
generator's own. Run inside Blender with tools/decokit on sys.path (render.py does that).

  today  the adopted globe: GR 1.2 m relief globe, chrome seas and satin-steel continents, 23.5 deg tilt, meridian ring, centre 7.38 m up and
         0.9 m out of the face, on a chrome stand with a stone plinth on the marquee roof
  a      the painting's: one smooth ball in the window's upper arch on the painting's three slim columns from the marquee roof (A_C, A_R); its finish is an argument
  b      no globe: the window, blades, marquee and lanterns as they are
  c      a matte enamel armillary in the same place as A: three hoops and a tilted band round a small ball, on a slim teal stem
  a-nickel-finials  A in satin nickel with the piers' and the marquee's chrome finials in the same satin (a question for Sky, not part of A)

The window fixes the room (civic_records: glass half width PR 1.65, arch spring PS 7.6, so the arch top is at 9.25; the brass grid stands at
y -0.14..-0.07; marquee top ZG1 5.0, 1.8 m out). A and C keep their backs 0.15 m off the grid, their tops 0.4-0.5 m under the arch top and
their feet 2.2 m over the marquee, so at the play camera they sit inside the arch, clear of the marquee and the door."""

import math

import civic_records as cr
import mats
from geo import fillet, rrect
from mats import pal

A_C, A_R = (0.0, -0.9, 8.0), 0.75          # A: ball centre (x along the face, y out of it, z up) and radius: 1.5 m across in a 3.3 m window
C_C, C_R = (0.0, -0.95, 8.0), 0.8          # C: armillary centre and outer hoop radius
# Round 1 had A at r 0.85, 1.0 m out, 7.8 up (C at 0.9): at the play heading (60 deg off the face) anything 1 m out slides 0.87 m sideways on
# screen, so both lay over the window's left jamb. Smaller, nearer the glass and 0.2 m higher, they stay inside the arch at yaw 210 and 300.
TILT = 23.5                                # the polar axis leans in the plane of the face, as today's globe

# scratch rows (a real change would register them in civic_common.py)
SUNSHINE = mats.register("DK_Enamel_Sunshine103", "enamel", "#F8E26C")   # GDD s15 palette: "the lantern and records areas take sunshine #f8e26c"
FINISH = {   # A's ball and stem, one row per finish shown on the board (mats KIND: brass = metallic 0.6, smoothness 0.6, lit by the sun)
    "chrome": "DK_Chrome",                                                   # today's mirror chrome (metallic 1, smoothness 0.95, no sun specular in game)
    "nickel": mats.register("DK_Nickel_Satin103", "brass", "#C9C4BA"),     # satin nickel: the kit's satin-brass recipe on a warm silver
    "gold": mats.register("DK_Gold_Civic103", "brass", mats.GOLD_DK),       # DecoKit gold, satin: gold may be a large surface on a landmark (s15)
    "enamel": mats.register("DK_Enamel_Globe103", "enamel", "#1CB9C6"),     # turquoise enamel ball (the s15 tower accent): no metal at all
}


def stem(s, c, r, zroof, mat, base_mat, cup=0.25):
    """Slim stand from the marquee roof to a shallow cup under a sphere of radius r at c: base disc 0.6 m across, stem 9 cm, cup rim on the sphere."""
    zr = c[2] - math.sqrt(r * r - cup * cup)              # where the cup's rim meets the sphere
    z0 = zr - 0.12
    with s.at((c[0], c[1], zroof - 0.03)):
        s.lathe([(0, 0), (0.30, 0), (0.30, 0.04), (0.20, 0.08), (0.08, 0.12), (0.045, 0.16), (0, 0.16)], base_mat, seg=32)
    s.sweep([(c[0], c[1], zroof + 0.12), (c[0], c[1], z0 + 0.01)], 0.045, mat, seg=12)
    with s.at((c[0], c[1], z0)):
        s.lathe([(0, 0), (0.045, 0), (0.07, 0.03), (0.15, 0.08), (cup, 0.115), (cup, 0.125), (0, 0.125)], mat, seg=32)


def columns(s, c, r, zroof, mat, cup=0.22):
    """The painting's stand (the 26 Sep records-office painting): three columns from the canopy, the middle one up to a flared cradle under the ball,
    the two beside it stopping short with domed tops: the crown's three blades said again in metal. A low rounded plinth on the marquee roof."""
    x, y = c[0], c[1]
    zr = c[2] - math.sqrt(r * r - cup * cup)              # where the cradle's rim meets the ball
    with s.at((x, y, zroof - 0.03)):
        s.extrude(rrect(0.9, 0.42, 0.2), 0.0, 0.1, mat)
    z0 = zroof + 0.07
    s.sweep([(x, y, z0), (x, y, zr - 0.16)], 0.075, mat, seg=16)
    with s.at((x, y, zr - 0.17)):
        s.lathe([(0, 0), (0.075, 0), (0.09, 0.05), (0.15, 0.12), (cup, 0.17), (cup, 0.18), (0, 0.18)], mat, seg=32)
    zt = zr - 0.7
    for sx in (-1, 1):
        xx = x + sx * 0.235
        s.sweep([(xx, y, z0), (xx, y, zt)], 0.065, mat, seg=14)
        with s.at((xx, y, zt)):
            s.lathe([(0, 0), (0.065, 0), (0.06, 0.03), (0.045, 0.055), (0.025, 0.07), (0, 0.075)], mat, seg=14)


def ball_a(finish):
    def globe(s, c, r, zroof=cr.ZC1, **_):
        m = mats.get(FINISH[finish])
        cr.sphere(s, A_C, A_R, m, seg=56, n=28)
        columns(s, A_C, A_R, zroof, m)
    return globe


def hoop(s, R, w, t, mat, seg=72):
    """A band of radius R (outer), axial width w, radial thickness t, rounded section, about local Z."""
    r0 = R - t
    sec = fillet([(r0, -w / 2), (R, -w / 2), (R, w / 2), (r0, w / 2)], min(w, t) * 0.45, n=3)
    s.lathe(sec + [sec[0]], mat, seg=seg, cap_bottom=False, cap_top=False)


def armillary_c(s, c, r, zroof=cr.ZC1, **_):
    """Matte enamel armillary: sunshine meridians and equator, a coral ecliptic band, a turquoise core, ivory axis and knobs; deep teal stem."""
    R, sun, coral, core, ivory, teal = C_R, mats.get(SUNSHINE), pal.Accent_Coral, pal.Accent_Turquoise, pal.Enamel_Ivory, pal.Enamel_Teal
    with s.at(C_C, (0, TILT, 0)):
        with s.at((0, 0, 0), (90, 0, 0)):
            hoop(s, R, 0.12, 0.07, sun)                    # meridian in the plane of the face: the circle you read
        with s.at((0, 0, 0), (0, 90, 0)):
            hoop(s, R - 0.02, 0.10, 0.06, sun)             # meridian across the face
        hoop(s, R - 0.04, 0.10, 0.06, sun)                 # equator
        with s.at((0, 0, 0), (TILT, 0, 0)):
            hoop(s, R - 0.10, 0.20, 0.05, coral)           # ecliptic: the one wide band of colour
        s.sweep([(0, 0, -R - 0.16), (0, 0, R + 0.16)], 0.03, ivory, seg=10)
        for z in (-R - 0.16, R + 0.16):
            cr.sphere(s, (0, 0, z), 0.075, ivory, seg=16, n=8)
        cr.sphere(s, (0, 0, 0), 0.24, core, seg=32, n=16)
    # stem to the meridian's lowest point (directly under the centre: the meridian is a circle in the face plane)
    zb = C_C[2] - R
    with s.at((C_C[0], C_C[1], zroof - 0.03)):
        s.lathe([(0, 0), (0.30, 0), (0.30, 0.04), (0.20, 0.08), (0.08, 0.12), (0.045, 0.16), (0, 0.16)], teal, seg=32)
    s.sweep([(C_C[0], C_C[1], zroof + 0.12), (C_C[0], C_C[1], zb + 0.02)], 0.045, teal, seg=12)


def no_globe(s, c, r, **_):
    return None


VARIANTS = {"today": None, "b": no_globe, "c": armillary_c}
VARIANTS.update({f"a-{k}": ball_a(k) for k in FINISH})
VARIANTS["a-nickel-finials"] = VARIANTS["a-nickel"]


class _FinialPal:
    """civic_records' pal while pier() and marquee() run for "a-nickel-finials": their chrome (the pier finials, the marquee's corner finials) is satin nickel."""

    def __getattr__(self, key):
        return mats.get(FINISH["nickel"]) if key == "Chrome" else getattr(pal, key)


def _finials(fn):
    def wrapped(*a, **k):
        keep = cr.pal
        cr.pal = _FinialPal()
        try:
            return fn(*a, **k)
        finally:
            cr.pal = keep
    return wrapped


def build(variant):
    """Build RecordsEntrance with the variant's globe (kit.build, as the exporter builds it); the module's globe is put back afterwards."""
    import kit
    keep = cr.globe, cr.pier, cr.marquee
    fn = VARIANTS[variant]
    if fn is not None:
        cr.globe = fn
    if variant.endswith("-finials"):
        cr.pier, cr.marquee = _finials(cr.pier), _finials(cr.marquee)
    try:
        obj = kit.build("RecordsEntrance")
    finally:
        cr.globe, cr.pier, cr.marquee = keep
    obj.name = f"entrance-{variant}"
    return obj
