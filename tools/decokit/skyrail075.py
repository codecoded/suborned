"""The sky track to the old reel's profile (brief 075 district roll-out, step 1, Vela; designs.md row C9, parity.html "Sky track").

The old town's sky rail (old Editor/DecoKit/DecoKitSkyRail.cs) was one swept streamline beam, ivory with a proud teal stripe each side, on the kit pylons
(SkyRail_Pylon_Foot or _Tall, a stretched SkyRail_Pylon_Shaft, SkyRail_Pylon_Head: skycraft.py, skyport_civic.py) every 30 m. Ours drew a flat coral box on
square posts. This module adds the two pieces the old kit lacked for our plan:

  SkyRail_Beam_4m        the old beam section as a straight tiling piece, 4 m long (one texture repeat of the old sweep's u = s / 4). Section (x across, z up, top
                         at z = 0 = the rail path, as the old sweep and the cars' origin): 1.2 wide x 1.6 deep, every corner filleted r 0.22 (4 points a corner, the old
                         Section()); a teal stripe 0.20 tall (z -1.05 .. -0.85) standing 0.02-0.035 proud on both flanks. Body DK_Hull_Ivory (the kit's painted
                         hull plating, 8 m metric: the "textured beam"; the old was the flat DK_Enamel_Ivory), stripe DK_Enamel_Teal. UVs are metric (u = metres
                         along, v = metres round the section from the bottom-left fillet), so a swept beam and this tile take the same material with no seam.
                         Length along Y (kit front -Y = Unity +Z, the direction of travel), pivot at the top centre of the tile's middle.
  SkyRail_Pylon_HeadTwin the twin-beam cross-head for S1's double trunk (plan standards.skyrail.beamCentres 6): the old head's flared cradle (same bottom section,
                         so the stretched Shaft meets it) rising into a smooth rounded cross-arm 8.0 m across, 1.4 m along the track; a bearing pad under each
                         beam at x +-3 (DK_Steel_Ivory, as the old head's cap: the vermilion livery's lattice slot), a teal stripe along both faces. Top at z = 0 =
                         the beams' underside, 2.2 m deep like the old head. The single-beam S1 loop keeps the old SkyRail_Pylon_Head.
Deterministic, no randomness. Curvy and smooth (Sky 2026-10-24): every end rounded.
"""

import math

from kit import asset
from mats import pal
from structures import superell, tube

BEAM_W, BEAM_H, BEAM_R = 0.6, 1.6, 0.22          # half width, depth, fillet (old DecoKitSkyRail: BeamW .6, BeamH 1.6, Section(.22))
STRIPE = (-1.05, -0.85)                          # the old stripe band (z), proud of the flank
TILE = 4.0                                       # one repeat of the old sweep's u = s / 4
TWIN = 3.0                                       # beam centre offset (plan beamCentres 6 / 2)


def beam_section(w=BEAM_W, h=BEAM_H, r=BEAM_R, per=4):
    """The old Section(): closed CCW outline (x side, z up, top at 0), filleted corners, `per` points a corner (0..90 deg in 30 deg steps)."""
    corners = [(-w, -h, 180.0), (w, -h, 270.0), (w, 0.0, 0.0), (-w, 0.0, 90.0)]
    out = []
    for x, y, a0 in corners:
        cx, cy = x - math.copysign(r, x), y + (r if y < 0 else -r)
        for k in range(per):
            a = math.radians(a0 + 90.0 / (per - 1) * k)
            out.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    return out


def stripe_section(side, w=BEAM_W, z0=STRIPE[0], z1=STRIPE[1]):
    """A slim proud band on one flank (side +-1): 0.04 thick (from 0.005 inside the flank to 0.035 out), rounded top and bottom edges; CCW in (x, z)."""
    xi, xo, r = w - 0.005, w + 0.035, 0.012
    pts = [(xi, z0), (xo - r, z0), (xo, z0 + r), (xo, z1 - r), (xo - r, z1), (xi, z1)]
    pts = [(side * x, z) for x, z in pts]
    return pts if side > 0 else pts[::-1]


def _perimeter(sec):
    d = [0.0]
    for (x0, z0), (x1, z1) in zip(sec, sec[1:] + sec[:1]):
        d.append(d[-1] + math.hypot(x1 - x0, z1 - z0))
    return d


def _swept(s, sec, mat, y0, y1, nu=1):
    """A closed section swept straight along Y from y0 to y1 as one sheet with metric UVs (u = metres along from y0, v = metres round the section)."""
    d = _perimeter(sec)
    L, m = d[-1], len(sec)

    def at(v):
        t = v * L
        for k in range(m):
            if d[k + 1] >= t - 1e-9:
                f = 0.0 if d[k + 1] == d[k] else (t - d[k]) / (d[k + 1] - d[k])
                (xa, za), (xb, zb) = sec[k], sec[(k + 1) % m]
                return xa + (xb - xa) * f, za + (zb - za) * f
        return sec[0]
    # one grid column per section vertex (v at each vertex's arc length) so the outline is exact; sheet() spaces v evenly, so remap through `vs`
    vs = [d[k] / L for k in range(m)] + [1.0]
    n0 = len(s._pending_uv)
    s.sheet(lambda u, v: (at(vs[round(v * m)])[0], y0 + (y1 - y0) * u, at(vs[round(v * m)])[1]), nu, m, mat, smooth=True)
    for i in range(n0, len(s._pending_uv)):  # metric UVs: the kit's textured materials tile by the metre (DK_Hull_Ivory 8 m)
        f, cell = s._pending_uv[i]
        s._pending_uv[i] = (f, [(u * (y1 - y0), vs[round(v * m)] * L) for u, v in cell])


def _beam(s, x=0.0, y0=-TILE / 2, y1=TILE / 2):
    _swept(s, [(px + x, pz) for px, pz in beam_section()], pal.Hull_Ivory, y0, y1)
    for side in (-1, 1):
        _swept(s, [(px + x, pz) for px, pz in stripe_section(side)], pal.Enamel_Teal, y0, y1)


@asset("SkyRail_Beam_4m", "Sky", bevel=0.0)
def sky_beam(s):
    """One 4 m straight of the old beam (1.2 x 1.6, fillet 0.22, teal stripe both sides), top at z 0; tiles end to end along Y."""
    _beam(s)


@asset("SkyRail_Pylon_HeadTwin", "Sky", bevel=0.0)
def pylon_head_twin(s):
    """Twin-beam cross-head, top at z = 0 (the beams' underside), 2.2 m deep: the old head's cradle flaring across into a rounded cross-arm, pads at x +-3."""
    # the cradle: the old head's bottom section (superell 0.6 x 0.45, up X: 1.2 along the track, 0.9 across), flaring across into the arm
    zs = [-2.2 + 1.5 * k / 10 for k in range(11)]
    tube(s, [(0, 0, z) for z in zs], superell(0.6, 0.45, p=3, n=24), pal.Enamel_Ivory, up=(1, 0, 0),
         scale=[(1 + 0.12 * (k / 10) ** 2, 1 + 3.4 * (k / 10) ** 2.2) for k in range(11)], caps=True)
    # the cross-arm: a rounded bar along X, flat top at z -0.12 under the pads, deepest (1.5) over the cradle, 0.9 at the shoulders, rounded ends past |x| 3.4
    W, L, L0 = 0.7, 4.0, 3.4

    def arm(u, v):
        x = -L + 2 * L * u
        e = abs(x)
        f = 1.0 if e <= L0 else math.sqrt(max(0.0, 1 - ((e - L0) / (L - L0)) ** 2))
        h = 0.45 + 0.3 * (1 - min(1.0, e / L0) ** 2)   # half depth: 0.75 at the centre, 0.45 at the shoulders
        zc = -0.12 - h
        t = 2 * math.pi * v
        c, sn = math.cos(t), math.sin(t)
        y = W * f * math.copysign(abs(c) ** (2 / 3.2), c)
        z = zc + h * max(f, 0.35) * math.copysign(abs(sn) ** (2 / 3.2), sn)
        return (x, y, z if f > 0 else zc)
    s.sheet(arm, 48, 28, pal.Enamel_Ivory, smooth=True)
    for side in (-1, 1):  # teal stripe along both faces of the arm, rounded ends
        s.rbox((-3.3, side * (W + 0.005) - 0.02, -0.62), (3.3, side * (W + 0.005) + 0.02, -0.46), 0.015, pal.Enamel_Teal)
    for x in (-TWIN, TWIN):  # bearing pads under each beam (the old head's 0.12 cap, one per beam)
        s.rbox((x - 0.7, -0.66, -0.14), (x + 0.7, 0.66, 0.0), 0.03, pal.Steel_Ivory)
