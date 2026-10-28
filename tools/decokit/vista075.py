"""Brief 075 golden view, the vista (Vela): the boulevard axis ends as the old reel's did, on the gold parabolic arch and a gold banded disc tower.

  ParabolicArch@crown-avenue  the Crown Avenue gateway (plan landmarks[crown-vista].gateway, Datum): ParabolicArch regenerated to the avenue's building lines.
                              Outer span 34.0 (foot to foot), foot centres +-15.5 on X, 28.0 clear between the plinths, plinths 3.0 across x 6.0 along, crest
                              (rib centreline) 68, the atom's sphere top 75.2 (its rings 75.8, as the pier's arch). The same design as the pier arch and the old
                              DecoKit arch (limestone rib with teal face inlays and gold edge beads, a chrome-and-gold atom at the crest): the rib is 2.5 m
                              across at the foot (the kit's 3.2 x 0.78, so rib and beads stand inside the 3 m plinth) and keeps its 4.4 m depth. Plinths are
                              rounded (curvy, never boxy), limestone with a teal enamel band and chrome hairlines (street level: no gold, no bronze panel;
                              chrome, not brass, because a crown piece carries one gold only: kit.test 'brass over gold'). Crown piece (crown.py): its pal.Gold is DK_Crown_Gold like the pier arch's.
  DiscTower@crown-vista       the Crown Vista tower (plan landmarks[crown-vista], DiscTower x 1.1): DiscTower at the kit's own size and silhouette with the old
                              reel's gold shaft: a gold core with 40 fine ivory ribs (0.4 m), so the shaft reads gold with cream lines, crossed by the kit's
                              ivory ring bands every 12 m (gold in the core, so the L1 / L2 collapse, which drops the ribs, stays gold); disc, drum, crane and mast as the kit. Its gold is a DK_Crown_Gold slot (crown.py), which the
                              livery key `gold` (DK_Crown_Gold__gold, #F2C14E, the old SpaceHQ gold) paints as the old one.

Pivot = footprint centre at ground (z = 0), front -Y; kit +X = plan +x for the arch (feet on X)."""

import math

from geo import rrect
from kit import asset
from landmarks import disc_tower_at
from mats import pal
from mega_parts import strut
from structures import sec, tube

# ----------------------------------------------------------------- ParabolicArch@crown-avenue
CA_A, CA_H = 15.5, 68.0      # foot centres +-15.5, crest (rib centreline) 68
CA_HW = 1.25                 # rib half width in the arch plane at the foot (kit 1.6)
CA_PX, CA_PY, CA_PZ = 3.0, 6.0, 3.2   # plinth across (x), along (y), height


@asset("ParabolicArch@crown-avenue", "Landmarks", bevel=0.0)
def crown_avenue_arch(s):
    n = 40
    P = [(CA_A * (2 * k / n - 1), 0.0, CA_H * (1 - (2 * k / n - 1) ** 2)) for k in range(n + 1)]
    taper = [(1.0 - 0.45 * (1 - abs(2 * k / n - 1)),) * 2 for k in range(n + 1)]
    tube(s, P, sec(-CA_HW, CA_HW, -2.2, 2.2, 0.55), pal.Limestone_Tex, up=(0, 1, 0), scale=taper)
    inlay = CA_HW * 0.55 / 1.6
    for side in (-1, 1):
        tube(s, P, sec(-inlay, inlay, side * 2.2 - 0.03, side * 2.2 + 0.03)[:: side], pal.Enamel_Teal, up=(0, 1, 0), scale=taper)
        for a in (-CA_HW - 0.02, CA_HW + 0.02):
            tube(s, P, [(a + 0.07 * math.cos(t), side * 2.0 + 0.07 * math.sin(t))
                        for t in (2 * math.pi * j / 6 for j in range(6))], pal.Gold, up=(0, 1, 0), scale=taper)
    for x in (-CA_A, CA_A):  # plinths: 3.0 x 6.0 overall (the band and hairlines are the outermost), rounded corners
        with s.at((x, 0.0, 0.0)):
            s.extrude(rrect(CA_PX - 0.08, CA_PY - 0.08, 0.6, n=6), 0.0, CA_PZ - 0.2, pal.Limestone_Tex, smooth_sides=True)
            s.extrude(rrect(CA_PX, CA_PY, 0.64, n=6), CA_PZ - 0.2, CA_PZ, pal.Limestone_Tex, smooth_sides=True)   # coping
            s.extrude(rrect(CA_PX - 0.03, CA_PY - 0.03, 0.62, n=6), 0.84, 2.36, pal.Enamel_Teal, smooth_sides=True)  # enamel band
            for z in (0.8, 2.36):                                                                                   # chrome hairlines
                s.extrude(rrect(CA_PX, CA_PY, 0.64, n=6), z, z + 0.04, pal.Chrome, smooth_sides=True)
    c = (0.0, 0.0, CA_H + 4.6)
    with s.at(c):  # the atom, as the kit arch
        s.lathe([(2.6 * math.sin(math.pi * k / 12), -2.6 * math.cos(math.pi * k / 12)) for k in range(13)], pal.Chrome, seg=24)
        for tilt in (0, 60, 120):
            with s.at((0, 0, 0), (70, 0, tilt)):
                ring = [(6.5 * math.cos(2 * math.pi * j / 48), 3.0 * math.sin(2 * math.pi * j / 48), 0.0) for j in range(48)]
                tube(s, ring, [(0.18 * math.cos(t), 0.18 * math.sin(t)) for t in (2 * math.pi * j / 8 for j in range(8))], pal.Gold, closed=True)
    strut(s, (0.0, 0.0, CA_H - 1.0), (0.0, 0.0, CA_H + 2.2), 0.6, pal.Chrome)


# ----------------------------------------------------------------- DiscTower@crown-vista

@asset("DiscTower@crown-vista", "Landmarks", bevel=0.0)
def crown_vista_tower(s):
    disc_tower_at(s, shaft={"ribs": 40, "rib": pal.Enamel_Ivory, "core": pal.Gold, "half": 0.2})
