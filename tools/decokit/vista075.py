"""Brief 075 golden view, the vista (Vela): the boulevard axis ends as the old reel's did, on the gold parabolic arch and a gold banded disc tower.

  ParabolicArch@crown-avenue  the Crown Avenue gateway (plan landmarks[crown-vista].gateway at (0, 53), Datum), vista 2: the vista 1 piece x 1.4 in
                              height only. Outer span 34.0 (foot to foot), foot centres +-15.5 on X, 28.0 clear between the plinths, plinths 3.0 across
                              x 6.0 along (unchanged), crest (rib centreline) 95.2, the atom's sphere top 105.3 (its rings ~105.85, under 106). The leg
                              at 8 m up stands at x +-14.83. The same design as the pier arch and the old DecoKit arch (limestone rib with teal face
                              inlays and gold edge beads, a chrome-and-gold atom at the crest): the rib is 2.5 m across at the foot, thickened to 5.0 deep
                              with the rise. Plinths rounded, limestone with a teal enamel band and chrome hairlines (street level: no gold). Hand-built
                              L1 (fewer sweep points, 4-sided beads) and L2 (no beads or hairlines). Crown piece (crown.py): pal.Gold is DK_Crown_Gold.
  DiscTower@crown-vista       the Crown Vista tower (plan landmarks[crown-vista], top 250, r 30), vista 2: the old reel's gold banded tower at scale 1,
                              tip 250.0, everything inside r 29.1 in plan (centred: pivot = footprint centre). A limestone podium drum (r 22, teal band),
                              eight gold buttress fins flaring into a gold fluted shaft (r 12, 32 fine ivory ribs) ringed with ivory bands every 12 m;
                              the lower ivory disc at z 150 (r 29, 10 thick), an observation drum, a narrower fluted gold shaft (r 9) to the upper disc at z 225
                              (r 23, 8 thick), a glazed drum, a stepped gold crown and a chrome needle to 250. No crane (the old gold tower had none).
                              Hand-built L1 (no ribs: the gold core) and L2 (coarse lathes, no glazing). Its gold is a DK_Crown_Gold slot (crown.py),
                              which the livery key `gold` (DK_Crown_Gold__gold, #F2C14E, the old SpaceHQ gold) paints as the old one.

Pivot = footprint centre at ground (z = 0), front -Y; kit +X = plan +x for the arch (feet on X)."""

import math

from geo import rrect
from kit import asset
from landmarks import fluted_shaft
from mats import pal
from mega_parts import lens, strut
from structures import sec, tube

# ----------------------------------------------------------------- ParabolicArch@crown-avenue (vista 2: the vista 1 piece x 1.4 in height only)
CA_A, CA_H = 15.5, 95.2      # foot centres +-15.5, crest (rib centreline) 95.2 (vista 1: 68)
CA_HW = 1.25                 # rib half width in the arch plane at the foot (kit 1.6): rib and beads stand inside the 3 m plinth
CA_D = 2.5                   # rib half depth (vista 1: 2.2; thickened with the rise, the plinths stay 6.0)
CA_PX, CA_PY, CA_PZ = 3.0, 6.0, 3.2   # plinth across (x), along (y), height
CA_ATOM_R = 2.6              # the atom's sphere radius (as the kit arch)
CA_ATOM_Z = 105.3 - CA_ATOM_R          # sphere top 105.3 (vista 1: 75.2 x 1.4); rings ~105.85, under the marks tier's 106


def _arch(s, lod):
    n, rr, rn = ((56, 0.6, 4), (28, 0.6, 2), (16, 0.0, 0))[lod]
    P = [(CA_A * (2 * k / n - 1), 0.0, CA_H * (1 - (2 * k / n - 1) ** 2)) for k in range(n + 1)]
    taper = [(1.0 - 0.45 * (1 - abs(2 * k / n - 1)),) * 2 for k in range(n + 1)]
    tube(s, P, sec(-CA_HW, CA_HW, -CA_D, CA_D, rr, max(rn, 1)) if rr else sec(-CA_HW, CA_HW, -CA_D, CA_D), pal.Limestone_Tex, up=(0, 1, 0), scale=taper)
    inlay = CA_HW * 0.55 / 1.6
    for side in (-1, 1):
        tube(s, P, sec(-inlay, inlay, side * CA_D - 0.03, side * CA_D + 0.03)[:: side], pal.Enamel_Teal, up=(0, 1, 0), scale=taper)
        if lod < 2:  # the gold edge beads (L2: a hairline is sub-pixel at that range)
            bs = 6 if lod == 0 else 4
            for a in (-CA_HW - 0.02, CA_HW + 0.02):
                tube(s, P, [(a + 0.08 * math.cos(t), side * (CA_D - 0.2) + 0.08 * math.sin(t))
                            for t in (2 * math.pi * j / bs for j in range(bs))], pal.Gold, up=(0, 1, 0), scale=taper)
    pn = (6, 3, 2)[lod]
    for x in (-CA_A, CA_A):  # plinths: 3.0 x 6.0 overall (the band and hairlines are the outermost), rounded corners
        with s.at((x, 0.0, 0.0)):
            s.extrude(rrect(CA_PX - 0.08, CA_PY - 0.08, 0.6, n=pn), 0.0, CA_PZ - 0.2, pal.Limestone_Tex, smooth_sides=True)
            s.extrude(rrect(CA_PX, CA_PY, 0.64, n=pn), CA_PZ - 0.2, CA_PZ, pal.Limestone_Tex, smooth_sides=True)   # coping
            s.extrude(rrect(CA_PX - 0.03, CA_PY - 0.03, 0.62, n=pn), 0.84, 2.36, pal.Enamel_Teal, smooth_sides=True)  # enamel band
            if lod < 2:
                for z in (0.8, 2.36):                                                                               # chrome hairlines
                    s.extrude(rrect(CA_PX, CA_PY, 0.64, n=pn), z, z + 0.04, pal.Chrome, smooth_sides=True)
    sseg, rseg, rs = ((24, 48, 8), (16, 32, 6), (12, 24, 4))[lod]
    with s.at((0.0, 0.0, CA_ATOM_Z)):  # the atom, as the kit arch
        s.lathe([(CA_ATOM_R * math.sin(math.pi * k / (sseg // 2)), -CA_ATOM_R * math.cos(math.pi * k / (sseg // 2))) for k in range(sseg // 2 + 1)], pal.Chrome, seg=sseg)
        for tilt in (0, 60, 120):
            with s.at((0, 0, 0), (70, 0, tilt)):
                ring = [(6.5 * math.cos(2 * math.pi * j / rseg), 3.0 * math.sin(2 * math.pi * j / rseg), 0.0) for j in range(rseg)]
                tube(s, ring, [(0.18 * math.cos(t), 0.18 * math.sin(t)) for t in (2 * math.pi * j / rs for j in range(rs))], pal.Gold, closed=True)
    strut(s, (0.0, 0.0, CA_H - 1.0), (0.0, 0.0, CA_ATOM_Z - CA_ATOM_R + 0.5), 0.8, pal.Chrome)  # the stem from the crest into the sphere


@asset("ParabolicArch@crown-avenue", "Landmarks", bevel=0.0, near=lambda s: _arch(s, 1), far=lambda s: _arch(s, 2))
def crown_avenue_arch(s):
    _arch(s, 0)


# ----------------------------------------------------------------- DiscTower@crown-vista (vista 2: the old reel's gold banded tower, 250 m)
CV_TOP = 250.0               # the mast tip (plan landmarks[crown-vista].top), at scale 1
CV_R = 12.0                  # the gold shaft: about 24 across
CV_D1, CV_D1R, CV_D1T = 150.0, 29.0, 10.0    # lower disc: floor z, radius, thickness (shows inside the arch)
CV_R2 = 9.0                  # the upper shaft between the discs
CV_D2, CV_D2R, CV_D2T = 225.0, 23.0, 8.0    # upper disc (over the arch top and the gate pair)
CV_FIN_R, CV_FIN_TOP = 21.0, 46.0          # eight gold buttress fins flare to r 21 on the podium; plan max = the lower disc's 29 (inside 30)


def _fin(s, a, n):
    """A gold buttress fin in the radial plane at angle a (deg): from the podium top at r CV_FIN_R, a concave sweep up into the shaft at CV_FIN_TOP."""
    z0 = 4.0
    curve = [(CV_R + (CV_FIN_R - CV_R) * (1 - t) ** 2, z0 + 1.5 + (CV_FIN_TOP - z0 - 1.5) * t) for t in (k / n for k in range(n + 1))]
    poly = [(CV_R - 0.3, z0), (CV_FIN_R, z0)] + curve + [(CV_R - 0.3, CV_FIN_TOP)]
    with s.at((0, 0, 0), (0, 0, a)), s.at((0, 0, 0), (90, 0, 0)):   # local x = radial, local y = up, local z = across the fin
        s.extrude(poly, -0.6, 0.6, pal.Gold)


def _bands(s, r, z0, z1, seg):
    for z in range(int(z0), int(z1), 12):
        lens(s, float(z), r + 0.3, r + 1.4, 0.7, pal.Enamel_Ivory, glaze=False, seg=seg)


def _tower(s, lod):
    hi = lod == 0
    seg, dseg, bseg = ((96, 64, 64), (48, 48, 32), (24, 24, 16))[lod]
    # podium: a round limestone drum with a rounded lip and a teal band (street level: stone, enamel; no gold)
    s.lathe([(22.0, 0.0), (22.0, 3.0), (21.4, 3.7), (20.6, 4.0), (0.0, 4.0)], pal.Limestone_Tex, seg=dseg, cap_bottom=False)
    s.lathe([(22.03, 1.0), (22.03, 2.2)], pal.Enamel_Teal, seg=dseg, cap_bottom=False, cap_top=False)
    for k in range(8):
        _fin(s, 22.5 + 45 * k, (10, 6, 4)[lod])
    # lower gold shaft, fluted: gold core, fine ivory ribs (L1 / L2 keep the gold core and the bands, the ribs drop)
    if hi:
        fluted_shaft(s, CV_R, 4.0, CV_D1, ribs=32, rib=pal.Enamel_Ivory, core=pal.Gold, half=0.2)
    else:
        s.lathe([(CV_R, 4.0), (CV_R, CV_D1)], pal.Gold, seg=seg, cap_bottom=False, cap_top=False)
    _bands(s, CV_R, 16, CV_D1 - 4, bseg)
    s.lathe([(CV_R + 0.4, CV_D1 - 2.0), (CV_R + 2.2, CV_D1 + 0.2)], pal.Enamel_Ivory, seg=dseg, cap_bottom=False, cap_top=False)  # collar into the disc
    lens(s, CV_D1, CV_R - 0.2, CV_D1R, CV_D1T, pal.Hull_Ivory, glaze=lod < 2, seg=dseg)
    s.lathe([(CV_R - 0.2, CV_D1 + CV_D1T), (0.0, CV_D1 + CV_D1T)], pal.Hull_Ivory, seg=dseg, cap_bottom=False, cap_top=False)   # the disc's deck
    s.lathe([(10.0, CV_D1 + CV_D1T), (10.0, CV_D1 + CV_D1T + 4.0)], pal.Window_Tower, seg=dseg, cap_bottom=False, cap_top=False)  # observation drum
    lens(s, CV_D1 + CV_D1T + 4.0, 0.5, 11.0, 1.2, pal.Enamel_Ivory, glaze=False, seg=dseg)
    z2 = CV_D1 + CV_D1T + 5.2
    # upper gold shaft
    if hi:
        fluted_shaft(s, CV_R2, z2, CV_D2, ribs=24, rib=pal.Enamel_Ivory, core=pal.Gold, half=0.2)
    else:
        s.lathe([(CV_R2, z2), (CV_R2, CV_D2)], pal.Gold, seg=seg, cap_bottom=False, cap_top=False)
    _bands(s, CV_R2, z2 + 10, CV_D2 - 6, bseg)
    s.lathe([(CV_R2 + 0.4, CV_D2 - 1.6), (CV_R2 + 1.8, CV_D2 + 0.2)], pal.Enamel_Ivory, seg=dseg, cap_bottom=False, cap_top=False)
    lens(s, CV_D2, CV_R2 - 0.2, CV_D2R, CV_D2T, pal.Hull_Ivory, glaze=lod < 2, seg=dseg)
    zd = CV_D2 + CV_D2T
    s.lathe([(CV_R2 - 0.2, zd), (0.0, zd)], pal.Hull_Ivory, seg=dseg, cap_bottom=False, cap_top=False)
    s.lathe([(7.0, zd), (7.0, zd + 3.5)], pal.Window_Tower, seg=dseg, cap_bottom=False, cap_top=False)
    # the gold crown: stepped rounded drums to a needle (no crane: the old gold tower had none)
    zc = zd + 3.5
    s.lathe([(7.4, zc), (7.4, zc + 1.6), (6.0, zc + 2.0), (5.2, zc + 2.0), (5.2, zc + 3.6), (4.0, zc + 4.0), (3.4, zc + 4.0),
             (3.4, zc + 5.6), (2.4, zc + 6.0), (1.6, zc + 6.0), (0.9, zc + 7.0), (0.0, zc + 7.0)], pal.Gold, seg=dseg // 2 if lod else dseg, cap_bottom=False)
    for z, r in ((zc + 1.7, 7.45), (zc + 3.7, 5.25)) if lod < 2 else ():
        s.lathe([(r, z - 0.2), (r + 0.25, z), (r, z + 0.2)], pal.Enamel_Ivory, seg=dseg // 2, cap_bottom=False, cap_top=False)
    s.lathe([(0.0, zc + 6.5), (0.55, zc + 6.5), (0.12, CV_TOP - 0.6), (0.0, CV_TOP)], pal.Chrome, seg=(12, 8, 6)[lod])   # the needle: tip 250.0


@asset("DiscTower@crown-vista", "Landmarks", bevel=0.0, near=lambda s: _tower(s, 1), far=lambda s: _tower(s, 2))
def crown_vista_tower(s):
    _tower(s, 0)
