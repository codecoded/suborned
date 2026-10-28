"""Deco Kit streamline stack modules (brief 075-E, Vela; Sky 2026-10-28: "we've still lost all the lovely curved buildings"): the old streamline bay, run end and
wrap-around corner (skyline.py StreamBay_n / StreamEnd_n, facades.py StreamCorner_n, the old DecoKit generator's own code) cut into per-storey bands, so a wall
taller than the whole-wall pieces' storey codes stacks into one continuous streamline building: Base + (n - 1) Mid + Top = H(n) at scale 1.

PLAN frame (the plan's standards.buildings, as sides.py SideBayStack): Base 5.65 (granite plinth 0-0.45, the glazed shop storey under its 1.6 m ivory canopy
4.5-4.65, the teal fascia 4.65-5.65 with two brass beads), Mid 3.6 (spandrel 0-1.1 with the seafoam sill band 0.88-1.04, the ribbon window 1.1-3.6, the ivory
eyebrow 0.45 proud at 3.48-3.6, inside the band, so a module is exactly its band tall), Top 3.95 (the attic ribbon: spandrel 0-0.75, ribbon 0.75-2.85, eyebrow
2.83-2.95, the seafoam parapet band 3.2-3.45, the ivory coping 3.8-3.95). Every band is the same in all three families, so floor lines, eyebrows, sills,
fascia, canopy and coping run on unbroken from a bay round a corner or an end: the corner and end wraps share the whole-wall pieces' plan paths and radii.

  StreamBayStack_Base/_Mid/_Top     6 x 14 m bay (StreamBay_n's plan): half piers at the joints (a full 0.8 m pier forms on every joint), ribbons run across.
  StreamEndStack_Base/_Mid/_Top     a run's last bay (StreamEnd_n's plan): quarter round R 3 on -X, the ribbons wrapping the 14 m side (mirror X for +X).
  StreamCornerStack_Base/_Mid/_Top  the 8 x 8 m corner cell (StreamCorner_n's plan): the quarter round R 6 about (2, 6) on a square (90 deg) outside corner, shop
                                    glazing, ribbons, eyebrows and canopy wrapping it; the ivory fin on the round's midpoint climbs the Mid bands and tops out on the
                                    Top 6.0 m over the eyebrow line with three teal bands and a mast, as on the whole-wall piece.

Frames: as the whole-wall pieces (skyline.py): face -Y, pivot = the footprint's front centre on the facade plane (y = 0, the building line) at the band's foot,
depth +Y, no bevel; the same footprint and pivot as StreamBay_n / StreamEnd_n / StreamCorner_n, so a column of stack modules stands where the whole-wall piece would.
Windows: the opaque window film (sides.py DK_Window_Film, Redline's 072 rule for the every-face pieces: no transparent film over thousands of tall walls; the
occlusion cut fades it with the wall) in front of a solid body; GLASS names the material if Glint or Redline want the fronts' DK_GlassDark back.
L0 is full detail (GDD §12). L1 and L2 are hand-built: L1 the same layout with square-section trims, fewer segments on the rounds, no beads or mast; L2 the bands and
the window recess alone (body at the face, film 0.3 behind it, eyebrows, canopy and coping kept), so it is also the shadow copy with its windows open (072-G)."""

import math

from geo import rrect
from kit import asset
from mats import pal
from facades_parts import mast
from plaza import corner_path
from plaza_parts import bead, lift, offset, ribbon
from skyline import D, GLASS_Y, HW, END_R, end_path, wrap_cap
from structures import along, sec, tube
import sides  # noqa: F401  (registers DK_Window_Film)

G, FAS, CAN = 5.65, 4.65, 4.5      # Base: band top, fascia foot, canopy foot (canopy 0.15 thick, fascia 1.0, as the whole-wall pieces' 3.85 / 4.0 / 5.0)
FH, SP = 3.6, 1.1                  # Mid: storey, spandrel (the ribbon's sill)
ZT, SPT = 2.85, 0.75               # Top: eyebrow line, attic spandrel
BAND = {"Base": G, "Mid": FH, "Top": ZT + 1.1}   # 5.65, 3.6, 3.95
GLASS = "Window_Film"
SEG = {"corner": (16, 8, 6), "end": (14, 6, 4)}  # segments on the round per lod (L0 = the whole-wall pieces')
FIN_C = (2.0 - 6.0 * math.sqrt(0.5), 6.0 - 6.0 * math.sqrt(0.5))


def _t(s, P, a0, a1, b0, b1, r, mat, lod):
    tube(s, lift(P), sec(a0, a1, b0, b1, r if lod == 0 else 0.0), mat)


# ----------------------------------------------------------------- trims per band (skyline.ground_trims / floor_trims at the PLAN heights)

def trims_base(s, P, lod):
    _t(s, P, -0.06, 0.3, 0.0, 0.45, 0.02, pal.Stone_Dark, lod)
    _t(s, P, -0.1, 0.3, FAS, G, 0.03, pal.Enamel_Teal, lod)
    _t(s, P, -1.6, 0.0, CAN, CAN + 0.15, 0.06, pal.Enamel_Ivory, lod)
    if lod == 0:
        for z in (FAS + 0.06, G - 0.06):
            bead(s, P, -0.1, z, 0.025, pal.Gold)
        bead(s, P, -1.6, CAN + 0.075, 0.03, pal.Chrome)


def trims_mid(s, P, lod):
    if lod < 2:
        _t(s, P, -0.05, 0.02, SP - 0.22, SP - 0.06, 0.02, pal.Enamel_Seafoam, lod)
    _t(s, P, -0.45, 0.05, FH - 0.12, FH, 0.05, pal.Enamel_Ivory, lod)


def trims_top(s, P, lod):
    if lod < 2:
        _t(s, P, -0.05, 0.02, SPT - 0.22, SPT - 0.06, 0.02, pal.Enamel_Seafoam, lod)
        _t(s, P, -0.07, 0.02, ZT + 0.35, ZT + 0.6, 0.03, pal.Enamel_Seafoam, lod)
    _t(s, P, -0.45, 0.05, ZT - 0.02, ZT + 0.1, 0.05, pal.Enamel_Ivory, lod)
    _t(s, P, -0.14, 0.4, ZT + 0.95, ZT + 1.1, 0.06, pal.Enamel_Ivory, lod)


def mullions(s, path, z0, z1, step=1.5):
    """skyline.mullions: chrome glazing bars square to the path, in front of the film."""
    for p, t in along(path, step, margin=step / 2):
        with s.at((p.x, p.y, 0), (0, 0, math.degrees(math.atan2(t.y, t.x)))):
            s.box((-0.03, GLASS_Y - 0.08, z0), (0.03, GLASS_Y + 0.02, z1), pal.Chrome)


def shop_glazing(s, path, lod):
    """The shop storey's glazing along a path (skyline.glazing / plaza_parts.curved_glazing): film, brass frame beads and bars, a chrome transom at 3.2."""
    film = getattr(pal, GLASS)
    ribbon(s, offset(path, GLASS_Y), 0.45, FAS, film)
    if lod == 2:
        return
    for p, t in along(path, 1.5, margin=0.75):
        with s.at((p.x, p.y, 0), (0, 0, math.degrees(math.atan2(t.y, t.x)))):
            s.box((-0.035, GLASS_Y - 0.08, 0.45), (0.035, GLASS_Y + 0.03, FAS), pal.Gold)
    if lod == 0:
        for z in (0.49, FAS - 0.04):
            bead(s, path, GLASS_Y - 0.03, z, 0.035, pal.Gold)
        bead(s, path, GLASS_Y - 0.03, 3.2, 0.022, pal.Chrome)


# ----------------------------------------------------------------- the straight bay (skyline.shopfront / ribbon_floors)

def bay(band, lod):
    film = getattr(pal, GLASS)
    ST = [(-HW, 0.0), (HW, 0.0)]

    def build(s):
        body = pal.Limestone_Tex
        if band == "Base":
            for x0, x1 in ((-HW, -HW + 0.4), (HW - 0.4, HW)):
                s.box((x0, 0.0, 0.45), (x1, D, FAS), body)
            x0, x1 = -HW + 0.4, HW - 0.4
            s.face([(x0, GLASS_Y, 0.45), (x1, GLASS_Y, 0.45), (x1, GLASS_Y, FAS), (x0, GLASS_Y, FAS)], film)
            if lod < 2:  # skyline.glazing: brass frame, two bars (nv 3), a chrome transom
                f = 0.09
                for a, b in (((x0, 0.45), (x1, 0.45 + f)), ((x0, FAS - f), (x1, FAS)), ((x0, 0.45), (x0 + f, FAS)), ((x1 - f, 0.45), (x1, FAS))):
                    s.box((a[0], GLASS_Y - 0.05, a[1]), (b[0], GLASS_Y + 0.02, b[1]), pal.Gold)
                for k in (1, 2):
                    x = x0 + (x1 - x0) * k / 3
                    s.box((x - 0.035, GLASS_Y - 0.07, 0.45), (x + 0.035, GLASS_Y + 0.01, FAS), pal.Gold)
                s.box((x0, GLASS_Y - 0.045, 3.175), (x1, GLASS_Y + 0.01, 3.225), pal.Chrome)
            s.box((x0, GLASS_Y + 0.02, 0.0), (x1, D, FAS), body)
            s.box((-HW, 0.0, FAS), (HW, D, G), body)
            trims_base(s, ST, lod)
            return
        z0, z1 = (SP, FH) if band == "Mid" else (SPT, ZT)
        s.box((-HW, 0.0, 0.0), (HW, D, z0), body)
        s.face([(-HW, GLASS_Y, z0), (HW, GLASS_Y, z0), (HW, GLASS_Y, z1), (-HW, GLASS_Y, z1)], film)
        s.box((-HW, GLASS_Y + 0.02, z0), (HW, D, z1), body)
        for x0, x1 in ((-HW, -HW + 0.06), (HW - 0.06, HW)):  # the 6 cm end caps: a joint's ribbons meet on a thin stone line
            s.box((x0, 0.0, z0), (x1, GLASS_Y + 0.02, z1), body)
        if lod < 2:
            mullions(s, ST, z0, z1)
        if band == "Mid":
            trims_mid(s, ST, lod)
        else:
            s.box((-HW, 0.0, ZT), (HW, D, ZT + 0.95), body)  # parapet and roof deck
            trims_top(s, ST, lod)
    return build


# ----------------------------------------------------------------- the wraps (skyline.wrap_block per band)

def wrap(kind, band, lod):
    film = getattr(pal, GLASS)

    def build(s):
        body = pal.Limestone_Tex
        n = SEG[kind][lod]
        P, back = (corner_path(n=n), (4.0, 8.0)) if kind == "corner" else (end_path(n), (HW, D))
        plan = P + [back]
        inner = offset(P, GLASS_Y + 0.02) + [back]
        ends = along(P, 1e3)
        if band == "Base":
            for seg in (P[:2], P[-2:]):  # solid piers on the straight lead-ins, the glazing round the rest
                tube(s, lift(seg), sec(0, 0.8, 0.45, FAS), body)
            shop_glazing(s, P[1:-1], lod)
            s.extrude(inner, 0.0, FAS, body)
            s.extrude(plan, FAS, G, body)
            trims_base(s, P, lod)
            return
        z0, z1 = (SP, FH) if band == "Mid" else (SPT, ZT)
        s.extrude(plan, 0.0, z0, body)
        ribbon(s, offset(P, GLASS_Y), z0, z1, film)
        s.extrude(inner, z0, z1, body)
        wrap_cap(s, *ends[0], z0, z1, body, True)
        wrap_cap(s, *ends[-1], z0, z1, body, False)
        if lod < 2:
            mullions(s, P, z0, z1)
        if band == "Mid":
            trims_mid(s, P, lod)
        else:
            s.extrude(plan, ZT, ZT + 0.95, body)
            trims_top(s, P, lod)
        if kind == "corner":
            fin(s, band, lod)
    return build


def fin(s, band, lod):
    """facades.stream_corner's ivory fin on the round's midpoint (0.5 x 1.9 m, 1.3 m proud, along the diagonal): straight through the Mid bands, rounded off on
    the Top 6.0 m over the eyebrow line with three teal bands and the chrome mast (L0)."""
    plan = rrect(0.5, 1.9, 0.2 if lod == 0 else 0.0, 3, 0.0, -0.35)
    with s.at((*FIN_C, 0.0), (0, 0, -45)):
        if band == "Mid":
            s.extrude(plan, 0.0, FH, pal.Enamel_Ivory)
            return
        zt = ZT
        if lod == 0:
            s.extrude(plan, 0.0, zt + 5.6, pal.Enamel_Ivory)
            s.rbox((-0.25, -1.3, zt + 5.2), (0.25, 0.6, zt + 6.0), 0.2, pal.Enamel_Ivory)
        else:
            s.extrude(plan, 0.0, zt + 6.0, pal.Enamel_Ivory)
        if lod < 2:
            for k in range(3):
                z = zt + 2.6 + 1.0 * k
                s.box((-0.27, -1.32, z), (0.27, 0.4, z + 0.32), pal.Enamel_Teal)
        if lod == 0:
            mast(s, 0.0, -0.4, zt + 6.0, 3.2)


for _band in ("Base", "Mid", "Top"):
    asset(f"StreamBayStack_{_band}", "Facades", bevel=0.0, near=bay(_band, 1), far=bay(_band, 2))(bay(_band, 0))
    for _kind, _fam in (("end", "StreamEndStack"), ("corner", "StreamCornerStack")):
        asset(f"{_fam}_{_band}", "Facades", bevel=0.0, near=wrap(_kind, _band, 1), far=wrap(_kind, _band, 2))(wrap(_kind, _band, 0))

assert abs(BAND["Base"] + BAND["Top"] - 9.6) < 1e-9 and END_R == 3.0  # H(n) = 9.6 + 3.6 (n - 1): the plan's storey arithmetic
