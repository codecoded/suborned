"""Crossing pieces (brief 009, A): the materials they add and the flat-geometry helpers. Sky, 14 Oct: no zebra, no paint, no landing slab; the crossing is light
set flush in the road (seafoam bars or dots in brass frames), a floating chrome orb at each kerb end showing a walking or standing figure over a thin beam and a
glowing disc where people wait, brass dome studs along the kerb edge, seafoam inlay arcs where the crossing lands. Brass is a frame line only (SF-21).

Colour roles (all from the palette; dE2000 to the signal colours is measured by proposal009.py):
  seafoam glow   DK_Cross_Light #72B5A7 (the kit seafoam enamel, emissive): bars, dots, the halo's ring, the orb's emitter lens
  walk           DK_Cross_Walk #9BCFC2 (the civic pale seafoam, e_seafoam_pale), hotter than the bars: it is the message
  stop           DK_Cross_Stop #E8402F, a signal red. Sky's exception (14 Oct): small emissive display pictograms only, never a surface (mats.SIGNAL_EXEMPT)
  light volumes  DK_Cross_Beam #9BCFC2 (alpha 0.28) and DK_Cross_Halo #72B5A7 (alpha 0.40): kind `glow`, translucent; Latch draws them additive, double-sided
  frame          DK_Brass_Civic #99784D: bezels, bar and dot frames, hairlines, the dome stud (small metal only)
  body           DK_Chrome, display plate DK_Enamel_Charcoal #2B2F33, arc inlay DK_Inlay_Seafoam (as the civic paving)

Frames: metres, Z up, pivot at the ground. Flat pieces are 4 mm (light, inlay) to 6 mm (brass) proud of the surface they are set in, with no sides and no
underside: nothing here goes below z = 0, and the faces point up. Pieces with a front (the orb) face -Y."""

import math

import mats
from mats import register

register("DK_Cross_Light", "emit", mats.LIB["DK_Enamel_Seafoam"][1], 2.5)
register("DK_Cross_Walk", "emit", "#9BCFC2", 2.4)
register("DK_Cross_Stop", "emit", "#E8402F", 2.4)
register("DK_Cross_Beam", "glow", "#9BCFC2", 2.0, 0.28)
register("DK_Cross_Halo", "glow", mats.LIB["DK_Enamel_Seafoam"][1], 2.0, 0.40)
register("DK_Brass_Civic", "brass", "#99784D")                                 # as civic_common.py (setdefault: the same row)
register("DK_Inlay_Seafoam", "ceramic", mats.LIB["DK_Enamel_Seafoam"][1])      # as civic_paving.py

INLAY, LIGHT, BRASS = 0.004, 0.004, 0.006   # heights above the surface: inlay tile, light lens, brass frame


def flat(s, poly, z, mat):
    """One convex CCW polygon face-up at height z."""
    s.face([(x, y, z) for x, y in poly], mat)


def strip(s, a, b, z, mat, closed=False):
    """Face-up quads between two equal-length paths walked the same way, `a` the outer one (CCW about the interior): a ring or an arc band."""
    n = len(a)
    for i in range(n if closed else n - 1):
        j = (i + 1) % n
        s.face([(*a[i], z), (*a[j], z), (*b[j], z), (*b[i], z)], mat)
