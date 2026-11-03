"""Deco Kit shop fillers (brief 072, second piece pass, Vela; Splice's finding, Sky's "a)": shopfront runs keep shopfronts). The every-face pass left 248 street
tenancies with their own procedural shopfront boxes in front of calm side bays: tails of street faces the 6 m street bays cannot tile, corners with no corner piece,
whole street faces whose 14 m bay body does not fit the plot, single-volume sheds. These pieces are the street fronts' shopfront as narrow wall modules, so the
canopy, fascia and plinth of a shopfront run carry on where a street bay stops.

  ShopFill_3 / _4    3 m wall module (KIT frame, coping 16.3 / 19.7: the street fronts' own): the street bays' ground storey (granite plinth, half piers, glazed
                     shop with brass frame and mullion, interior floor and back wall, teal fascia 4.0-5.0 with two brass beads, ivory canopy 1.6 m out at 3.85-4.0
                     with its chrome nose: skyline.py ground_trims), then per storey one window under the side bays' sill band and eyebrow (sides.py), parapet
                     band and coping. Width scale 0.5-1.7 (1.5-5.1 m): any run of 1.5 m or more tiles with whole modules, and the 3 m module sits on the
                     tenancies' 3 m width snap.
  ShopStack_Base     the same shopfront as the PLAN frame's ground band (5.65 m, under SideBayStack_Mid / _Top) for walls over 4 storeys and the sheds' stacks:
                     plinth 0.45, the glass and the canopy at the tall street bays' heights (their KIT ground at sy 1.04 -- the tall bays' H(n) / coping is 1.039
                     to 1.047 for 5 to 10 storeys -- canopy 4.02-4.17, 1.65 m out), the fascia from there to the band top (4.17-5.65) with brass beads at 4.235
                     and 5.155 (the tall bays' two) and 5.59 (SideBayStack_Base's top bead): its foot meets the street bays' fascia, its top the stack's.

Frame (as SideBay, sides.py): pivot = the module's centre on the building line at its foot; Blender face -Y (kit front +Z), x across; the building line is y 0;
the canopy reaches 1.6 / 1.65 m out (-Y), the shop's back wall 2.0 m in (+Y). Nothing stands over the coping. Materials: the side bay's set with the
fronts' shopfront glass (DK_GlassDark, as the street bays, L0 only) and DK_Lobby for the back wall; the upper windows are the side bay's opaque DK_Window_Film.
L1 / L2 hand-built: the shop glass becomes an opaque dark quad (DK_Stone_Dark: the dark shop at distance, no see-through), bands and canopy as boxes. L2 is the
module's shadow copy (ShadowProxies), so it keeps the shop open to its back wall (the dark quad 2.0 m in, the piers and head that deep, the floor's top) and its
upper windows open in the slab (sides.py _slab, brief 072-G): the canopy and reveals shade the shop and the pockets, as L0's own shadow would."""

from kit import asset
from mats import pal
from plaza_parts import bead, glazing, lift
from sides import BACKY, KIT, PLAN, SKIN, mod_mid, mod_top, wall
from structures import sec, tube

HW = 1.5                    # the module's half width (3 m)
PW = 0.4                    # half pier at each joint (the street bays': a 0.8 m pier forms on every joint, with a street bay too)
GLASS_Y, DEEP = 0.3, 2.0    # shop glass plane, back wall face (into the building)

# the ground storey per frame: canopy foot / top, canopy reach, fascia foot / top, brass bead heights, transom height
SHOP = {
    "KIT": dict(c0=3.85, c1=4.0, out=1.6, f0=4.0, f1=5.0, beads=(4.06, 4.94), transom=2.75),
    "PLAN": dict(c0=4.016, c1=4.172, out=1.65, f0=4.172, f1=5.65, beads=(4.235, 5.155, 5.59), transom=2.87),
}


def shop_ground(S, hw, lod):
    """The shopfront storey from the pavement to the fascia top, frame values S (SHOP[...])."""
    g0, g1 = -hw + PW, hw - PW
    f0, f1, c0, c1, out = S["f0"], S["f1"], S["c0"], S["c1"], S["out"]
    line = [(-hw, 0.0), (hw, 0.0)]

    def draw(s):
        # piers to the back wall (the shop's side walls), the head behind the fascia; L1 closes the shop at the glass, so only the slab's depth; L2 (the
        # module's shadow copy, brief 072-G) keeps the shop's depth, so the sun reaches L0's shop floor and back wall under the canopy as L0's own shadow lets it
        back = DEEP + 0.15 if lod != 1 else BACKY
        for x0, x1 in ((-hw, g0), (g1, hw)):
            s.box((x0, SKIN, 0.45), (x1, back, f0), pal.Limestone_Tex)
        s.box((-hw, SKIN + 0.02, f0), (hw, back, f1), pal.Limestone_Tex)
        if lod == 0:
            tube(s, lift(line), sec(-0.06, 0.3, 0.0, 0.45, 0.02), pal.Stone_Dark)
            glazing(s, g0, g1, GLASS_Y, 0.45, f0, nv=2, dz=S["transom"] - 0.45)
            s.box((g0, GLASS_Y, 0.3), (g1, DEEP, 0.45), pal.Stone_Dark)              # shop floor
            s.box((g0, DEEP, 0.45), (g1, DEEP + 0.15, f0), pal.Lobby)                # back wall
            tube(s, lift(line), sec(-0.1, 0.3, f0, f1, 0.03), pal.Enamel_Teal)       # fascia
            for z in S["beads"]:
                bead(s, line, -0.1, z, 0.025, pal.Gold)
            tube(s, lift(line), sec(-out, 0.0, c0, c1, 0.06), pal.Enamel_Ivory)      # canopy
            bead(s, line, -out, (c0 + c1) / 2, 0.03, pal.Chrome)
            return
        s.box((-hw, -0.06, 0.0), (hw, 0.3, 0.45), pal.Stone_Dark)
        yb = GLASS_Y if lod == 1 else DEEP   # L1: the dark shop on the glass; L2: the back wall at L0's, over the shop floor
        if lod == 2:
            s.face([(g0, GLASS_Y, 0.45), (g1, GLASS_Y, 0.45), (g1, DEEP, 0.45), (g0, DEEP, 0.45)], pal.Stone_Dark)  # the shop floor's top (faces up)
        s.face([(g0, yb, 0.45), (g1, yb, 0.45), (g1, yb, f0), (g0, yb, f0)], pal.Stone_Dark)  # the dark shop behind the glass, opaque; a material of L0's set (kit.test: a LOD asks only for its piece's materials)
        if lod == 1:
            s.box((-0.035, GLASS_Y - 0.07, 0.45), (0.035, GLASS_Y, f0), pal.Gold)
        s.box((-hw, -0.1, f0), (hw, 0.0, f1), pal.Enamel_Teal)
        s.box((-hw, -out, c0), (hw, 0.0, c1), pal.Enamel_Ivory)
    return draw


def shop_fill(n, lod=0):
    """ShopFill_n: the shop ground, then n storeys with one window each and the KIT parapet (sides.py wall modules, kind "one")."""
    upper = wall(KIT, HW, "one", [(mod_mid, KIT["g"] + k * KIT["fh"]) for k in range(n)] + [(mod_top, KIT["g"] + n * KIT["fh"])], lod)

    def build(s):
        shop_ground(SHOP["KIT"], HW, lod)(s)
        upper(s)
    return build


def shop_base(lod=0):
    def build(s):
        shop_ground(SHOP["PLAN"], HW, lod)(s)
    return build


assert abs(PLAN["g"] - SHOP["PLAN"]["f1"]) < 1e-9 and abs(KIT["g"] - SHOP["KIT"]["f1"]) < 1e-9  # the fascia top is the frame's ground top

for _n in (3, 4):
    asset(f"ShopFill_{_n}", "Sides", bevel=0.0, near=shop_fill(_n, 1), far=shop_fill(_n, 2))(shop_fill(_n, 0))
asset("ShopStack_Base", "Sides", bevel=0.0, near=shop_base(1), far=shop_base(2))(shop_base(0))
