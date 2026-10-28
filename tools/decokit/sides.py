"""Deco Kit sides family (brief 072 A, Vela; Sky 2026-10-27: "every" face is kit, side walls "a)"): the calm frontage-style side bay a building
shows wherever the public sees a wall that is not a shopfront, its narrow pilaster filler, the same as per-storey stack modules for the tall city,
and the rounded corner piers that turn the bands round a building's outside and inside corners. Service backs take the AlleyWall family (alleys.py).

Two storey frames:
  KIT   the street fronts' (skyline.py G 5.0, FH 3.4, SP 1.05; facades.py PortholeBay_3L / _4L, lowrise.py StreamBay_3): the whole-wall _3 / _4 pieces,
        so a front's floor lines, ground band and parapet run on round the corner at the same heights; the world scales a building's pieces together
        (sy = H(n) / coping, 16.3 -> 16.8 and 19.7 -> 20.4 in the play streets).
  PLAN  the plan's standards.buildings (plinth 0.45 + ground 5.2 = 5.65, upper storey 3.6, roof crown 3.95): the stack modules, so Base + (n - 1) Mid + Top
        is H(n) exactly at scale 1 (tall walls beyond the play streets; Splice's storey bands).
Bands in both: the granite plinth (0-0.45), the teal fascia (the ground storey's top 1.0 m) with two brass beads, per storey the seafoam sill band and the
ivory eyebrow 0.45 proud over the window head, the seafoam parapet band and the ivory coping.

  SideBay_3 / _4            6 m module, ground + 3 / 4 storeys (KIT, coping 16.3 / 19.7): a rusticated ground storey with two tall windows, per storey two
                            windows with a chrome porthole between, a teal joint strip at each 6 m joint.
  SideBayStack_Base/_Mid/_Top   the same bay per storey band (PLAN): Base 5.65 (plinth, ground storey, fascia), Mid 3.6, Top 3.95 (an attic: two square
                            lights and a porthole, the parapet band, a brass line, the coping).
  SidePilaster_3 / _4, SidePilaster_Base/_Mid/_Top   1.5 m filler with the same bands and no windows: a raised limestone pilaster with a teal flute, for the
                            rest of a run no whole number of 6 m modules fills (tags scale 0.33-1.7: 0.5-2.55 m).
  CornerPier_Out_3 / _4     rounded outside corner (KIT): the plinth, rustication, fascia, eyebrows, parapet band and coping turn a round of R 1.0 standing
                            0.35 proud of both faces; a teal inlay climbs the round between the eyebrows; three seafoam speed lines at the ground.
  CornerPier_Out_Base/_Mid/_Top, CornerPier_In_Base/_Mid/_Top   the piers per storey band (PLAN).
  CornerPier_In_3 / _4      inside corner (KIT): a cove R 0.9 between the two faces, every band following it (no sharp re-entrant mitre).

Frames (Blender: face -Y, Z up; the kit's Unity front +Z is Blender -Y; pivot at z 0 = the module's foot; no bevel; nothing passes the coping):
  SideBay*, SidePilaster*: as AlleyWall: pivot = the module's centre on the building line (the wall's outer plane), x across (tile along x), the render face
    2 cm proud (y = -0.02), the slab 0.35 deep behind it (+Y, into the building), trims up to 0.45 proud (-Y).
  CornerPier_*: pivot = the building's vertex on the building line, front (-Y) = the outward normal of face A, the face ENDING at the corner on a CCW ring
    (the incoming edge). Out: face A runs from the vertex along -X, face B along +Y (outward +X); the building is the quadrant x < 0, y > 0. In: face A
    runs along -X, face B along -Y (outward -X); the open air is the quadrant x < 0, y < 0. Each pier takes LEG = 1.0 m of both faces from the vertex
    (the modules start there); its footprint box along each face is exactly the leg, so a clip of the box against the face lines measures it.

L0 is full detail (GDD §12); L1 and L2 are hand-built (kit.asset near= / far=): the same layout with flat windows and box bands, then the bands and window
quads alone, so the windows survive the far levels (a collapse decimate eats them)."""

import math

from geo import arc
from holes import holed_slab
from kit import asset
from mats import pal, register
from plaza_parts import bead, lift, offset
from structures import ccw, sec, tube
from vehicles_parts import carve

# Brief 072 (Redline's look and cost item, briefs/072/redline/plan.md): the side pieces' windows are opaque. The fronts' DK_GlassDark is a transparent film (DK_GlassSky,
# alpha 0.72, queue 3000): the occlusion cut never fades it (FadeTags skips transparent queues), so panes floated in the cut, and it drew a blended layer over 8-15 %
# of the screen (est. 0.09-0.31 ms). DK_Window_Film is that film made opaque: the colour the film shows at the play camera (DK_GlassSky: the glaze #4A8588 lerped
# 0.41 toward its street mirror #A78F78, at alpha 0.72 over the charcoal back = about #5F726E unlit), taken a step darker as a lit enamel (sunlit walls read
# brighter), clear coat for the sky glint. Through the Complex Lit path it takes the cutaway shader, so it fades with the wall. [S]: Glint judges it in game.
# 29 Oct (brief 075 old palette, Sky: "back to old town"): the old DK_GlassDark colour #35585A (was #4F6B6B, the 072 film estimate): the old reel's windows read as dark glass
# ribbons (sampled #2A3229-#333328 at the circus), and the old glass is #35585A.
register("DK_Window_Film", "enamel", "#35585A")

SKIN, BACKY = -0.02, 0.35              # render face, slab back (alleys_parts)
REVEAL = 0.2                           # window pocket depth
WX, WW = (-1.45, 1.45), 1.5            # window centres and width
WXS = {"bay": WX, "one": (0.0,)}       # window centres by kind: the side bay's pair, the shop filler's one (shopfill.py)
PR = 0.3                               # the porthole between the windows
COURSE, GROOVE = 0.7, 0.04             # ground rustication: course height and groove
LEG, PROUD, RO, RI = 1.0, 0.35, 1.0, 0.9   # corner piers: face taken from the vertex, outside proud, outside round, inside cove
PIL = 0.25                             # the pilaster's half width

# storey frames: ground top, storey, sill (above the floor), ground windows (sill, head, transom), upper window (sill, head), top band height and its kind
# brow: the eyebrow's foot and top about the storey's top (KIT straddles it as floor_trims does; PLAN keeps it inside the band, so a stack module is exactly its band tall)
KIT = dict(g=5.0, fh=3.4, sp=1.05, gw=(1.0, 3.55, 2.75), w=(1.05, 3.0), top=1.1, attic=False, brow=(-0.02, 0.1))
PLAN = dict(g=5.65, fh=3.6, sp=1.1, gw=(1.1, 4.15, 3.3), w=(1.1, 3.2), top=3.95, attic=True, brow=(-0.12, 0.0))


def tops(F):
    """The top band's levels: parapet band (z0, z1), coping foot, coping top (the module's height)."""
    return ((0.35, 0.6), 0.95, 1.1) if not F["attic"] else ((2.85, 3.1), 3.8, 3.95)


# ----------------------------------------------------------------- shared drawing

def _window(s, x, z0, z1, lod, w=WW, transom=None):
    """Window in its stone pocket (lod 0): the opaque window film (DK_Window_Film), chrome frame, centre mullion and a transom; lod 1: a chrome frame quad with
    the film inset; lod 2: nothing here, the holed slab draws the film at the back of its pocket (_slab)."""
    x0, x1 = x - w / 2, x + w / 2
    if lod == 2:
        return  # the film closes the slab's pocket (_slab: L2 is holed so the shadow copy lets the sun into L0's pockets, brief 072-G)
    if lod:
        y = SKIN - 0.01
        s.face([(x0, y, z0), (x1, y, z0), (x1, y, z1), (x0, y, z1)], pal.Chrome)
        s.face([(x0 + 0.06, y - 0.005, z0 + 0.06), (x1 - 0.06, y - 0.005, z0 + 0.06), (x1 - 0.06, y - 0.005, z1 - 0.06), (x0 + 0.06, y - 0.005, z1 - 0.06)], pal.Window_Film)
        return
    yg = SKIN + REVEAL - 0.04
    s.face([(x0, yg, z0), (x1, yg, z0), (x1, yg, z1), (x0, yg, z1)], pal.Window_Film)
    f, yf = 0.05, yg - 0.05
    zt = transom if transom is not None else z0 + (z1 - z0) * 0.68
    bars = [((x0, z0), (x1, z0 + f)), ((x0, z1 - f), (x1, z1)), ((x0, z0), (x0 + f, z1)), ((x1 - f, z0), (x1, z1)), ((x - 0.025, z0), (x + 0.025, z1))]
    if transom is not False:
        bars.append(((x0, zt - 0.02), (x1, zt + 0.02)))
    for a, b in bars:
        s.box((a[0], yf, a[1]), (b[0], yg, b[1]), pal.Chrome)


def _porthole(s, x, z, lod):
    """Chrome-rimmed porthole: rim proud of the face, the opaque window film in the pocket (lod 1: the film disc on the face)."""
    n = 16 if lod == 0 else 8
    disc = [(x + PR * math.cos(2 * math.pi * k / n), z + PR * math.sin(2 * math.pi * k / n)) for k in range(n)]
    if lod:
        s.face([(px, SKIN - 0.01, pz) for px, pz in disc], pal.Window_Film)
        return
    with s.at((x, SKIN, z), (90, 0, 0)):
        s.lathe([(PR + 0.09, 0.0), (PR + 0.085, 0.045), (PR + 0.03, 0.06), (PR - 0.01, 0.035), (PR - 0.01, -0.03)], pal.Chrome, seg=n, cap_bottom=False, cap_top=False)
    s.face([(px, SKIN + REVEAL - 0.05, pz) for px, pz in disc], pal.Window_Film)


def _band(s, hw, z0, z1, a0, mat, r=0.02, lod=0, a1=0.05):
    """Full-width course from a0 (negative = proud) to a1 behind the face plane y = 0 (the fronts' trims are measured from it)."""
    if lod:
        s.box((-hw, a0, z0), (hw, min(a1, 0.0), z1), mat)
    else:
        tube(s, lift([(-hw, 0.0), (hw, 0.0)]), sec(a0, a1, z0, z1, r, n=1), mat)


def _pilaster(s, z0, z1, lod, flute=None):
    """The filler's raised pilaster (0.5 m, 0.12 proud) with its teal flute."""
    s.box((-PIL, SKIN - 0.12, z0), (PIL, SKIN + 0.01, z1), pal.Limestone_Tex)
    if flute and lod < 2:
        s.box((-0.06, SKIN - 0.13, z0 + flute[0]), (0.06, SKIN - 0.115, z1 - flute[1]), pal.Enamel_Teal)


# ----------------------------------------------------------------- wall modules: (holes, seams, draw, stone height) for a module at z0

def mod_base(F, hw, kind, z0, lod):
    """Ground storey 0..g: granite plinth, rusticated stone (two tall windows on a bay), the teal fascia over the top 1.0 m with its brass beads."""
    g, (gw0, gw1, gwt) = F["g"], F["gw"]
    bay = kind == "bay"
    holes = [("r", x - WW / 2, z0 + gw0, x + WW / 2, z0 + gw1) for x in WX] if bay else []
    seams = [("h", z0 + 0.45 + COURSE * k) for k in range(1, 8) if 0.45 + COURSE * k < g - 1.0 - 0.1]

    def draw(s):
        _band(s, hw, z0, z0 + 0.45, -0.06, pal.Stone_Dark, lod=lod, a1=0.3)
        if bay:
            for x in WX:
                _window(s, x, z0 + gw0, z0 + gw1, lod, transom=z0 + gwt)
        else:
            _pilaster(s, z0 + 0.45, z0 + g - 1.0, lod)
        _band(s, hw, z0 + g - 1.0, z0 + g, -0.1, pal.Enamel_Teal, r=0.03, lod=lod, a1=0.3)
        if lod == 0:
            for z in (g - 0.94, g - 0.06):
                bead(s, [(-hw, 0.0), (hw, 0.0)], -0.1, z0 + z, 0.025, pal.Gold)
    return holes, seams, draw, g


def mod_mid(F, hw, kind, z0, lod):
    """One upper storey: two windows and the porthole between (a bay) or the pilaster (a filler), the seafoam sill band, the ivory eyebrow over the head,
    the teal joint strips (a bay)."""
    fh, sp, (w0, w1) = F["fh"], F["sp"], F["w"]
    bay, xs = kind in ("bay", "one"), WXS.get(kind, ())
    holes = [("r", x - WW / 2, z0 + w0, x + WW / 2, z0 + w1) for x in xs] + ([("c", 0.0, z0 + (w0 + w1) / 2, PR)] if kind == "bay" else [])

    def draw(s):
        if bay:
            for x in xs:
                _window(s, x, z0 + w0, z0 + w1, lod)
            if lod < 2 and kind == "bay":
                _porthole(s, 0.0, z0 + (w0 + w1) / 2, lod)
        else:
            _pilaster(s, z0, z0 + fh - 0.02, lod, flute=(0.3, 0.3))
        if lod < 2:
            _band(s, hw, z0 + sp - 0.22, z0 + sp - 0.06, -0.05, pal.Enamel_Seafoam, lod=lod, a1=0.02)
        _band(s, hw, z0 + fh + F["brow"][0], z0 + fh + F["brow"][1], -0.45, pal.Enamel_Ivory, r=0.05, lod=lod)
        if kind == "bay":
            for xa, xb in ((-hw, -hw + 0.12), (hw - 0.12, hw)):
                s.box((xa, SKIN - 0.04, z0), (xb, SKIN + 0.01 if lod < 2 else SKIN, z0 + fh - 0.02), pal.Enamel_Teal)
    return holes, [], draw, fh


def mod_top(F, hw, kind, z0, lod):
    """The top band: KIT a 1.1 m parapet (floor_trims' band and coping); PLAN the 3.95 m roof crown as an attic (two square lights and a porthole on a bay),
    the parapet band, a brass line and the coping."""
    (b0, b1), c0, c1 = tops(F)
    bay = kind in ("bay", "one")  # "one" (the shop fillers, shopfill.py): KIT only, the plain parapet
    attic = F["attic"]
    holes = []
    if attic and bay:
        holes = [("r", x - 0.45, z0 + 0.95, x + 0.45, z0 + 1.85) for x in WX] + [("c", 0.0, z0 + 1.4, PR)]

    def draw(s):
        if attic:
            if bay:
                for x in WX:
                    _window(s, x, z0 + 0.95, z0 + 1.85, lod, w=0.9, transom=False)
                if lod < 2:
                    _porthole(s, 0.0, z0 + 1.4, lod)
            else:
                _pilaster(s, z0, z0 + b0, lod, flute=(0.3, 0.3))
            if lod == 0:
                tube(s, lift([(-hw, 0.0), (hw, 0.0)]), [(-0.09, z0 + 3.2), (-0.05, z0 + 3.2), (-0.05, z0 + 3.24), (-0.09, z0 + 3.24)], pal.Gold)
        elif not bay:
            _pilaster(s, z0, z0 + b0, lod)
        _band(s, hw, z0 + b0, z0 + b1, -0.07, pal.Enamel_Seafoam, r=0.03, lod=lod, a1=0.02)
        if lod:
            s.box((-hw, -0.14, z0 + c0), (hw, BACKY, z0 + c1), pal.Enamel_Ivory)
        else:
            tube(s, lift([(-hw, 0.0), (hw, 0.0)]), sec(-0.14, 0.4, z0 + c0, z0 + c1, 0.06), pal.Enamel_Ivory)
    return holes, [], draw, c0


def _slab(s, hw, z0, z1, holes, seams, lod):
    """The wall from z0 to z1 minus window pockets, portholes and rustication grooves (one exact boolean, cut faces in the stone); lod 1: a plain box;
    lod 2: a box open at the window pockets, closed at the pocket depth by the window film (holes.py; brief 072-G, Sky's "ok": the L2 is the module's
    shadow copy, and a solid slab put every sunlit pocket in shadow). Portholes stay closed: 0.6 m, their own proud rim shades them in L0 too."""
    body = pal.Limestone_Tex
    rects = [h for h in holes if h[0] == "r"]
    if lod == 2 and rects:
        holed_slab(s, -hw, hw, z0, z1, SKIN, BACKY, [(x0, za, x1, zb, pal.Window_Film, None) for _, x0, za, x1, zb in rects], body, SKIN + REVEAL)
        return
    if lod or not (holes or seams):
        s.box((-hw, SKIN, z0), (hw, BACKY, z1), body)
        return

    def whole(b):
        b.box((-hw, SKIN, z0), (hw, BACKY, z1), body)

    def cut(c):
        for h in holes:
            if h[0] == "r":
                _, x0, za, x1, zb = h
                c.box((x0, SKIN - 0.1, za), (x1, SKIN + REVEAL, zb), body)
            else:
                _, x, z, r = h
                with c.at((x, SKIN - 0.1, z), (-90, 0, 0)):
                    c.lathe([(r, 0.0), (r, REVEAL + 0.1)], body, seg=16)
        for _, z in seams:
            c.box((-hw - 0.1, SKIN - 0.1, z - GROOVE / 2), (hw + 0.1, SKIN + 0.025, z + GROOVE / 2), body)
    carve(s, whole, cut)


def wall(F, hw, kind, mods, lod=0):
    """A wall piece from modules [(fn, z0)], bottom to top: one slab over their whole stone height, then each module's draws."""
    def build(s):
        parts = [fn(F, hw, kind, z0, lod) for fn, z0 in mods]
        _slab(s, hw, mods[0][1], mods[-1][1] + parts[-1][3], [h for p in parts for h in p[0]], [q for p in parts for q in p[1]], lod)
        for p in parts:
            p[2](s)
    return build


def whole_mods(F, n):
    """Ground + n storeys + the top band, in frame F."""
    return [(mod_base, 0.0)] + [(mod_mid, F["g"] + k * F["fh"]) for k in range(n)] + [(mod_top, F["g"] + n * F["fh"])]


def _wall(name, F, hw, kind, mods):
    asset(name, "Sides", bevel=0.0, near=wall(F, hw, kind, mods, 1), far=wall(F, hw, kind, mods, 2))(wall(F, hw, kind, mods, 0))


for _name, _hw, _kind in (("SideBay", 3.0, "bay"), ("SidePilaster", 0.75, "pil")):
    _stack = "SideBayStack" if _kind == "bay" else "SidePilaster"
    for _n in (3, 4):
        _wall(f"{_name}_{_n}", KIT, _hw, _kind, whole_mods(KIT, _n))
    for _tag, _fn in (("Base", mod_base), ("Mid", mod_mid), ("Top", mod_top)):
        _wall(f"{_stack}_{_tag}", PLAN, _hw, _kind, [(_fn, 0.0)])


# ----------------------------------------------------------------- corner piers

def out_path(n=10):
    """The outside pier's face line, building on the left: along face A (y = -PROUD) from the leg end, round R about (PROUD - RO, RO - PROUD), up face B (x = PROUD)."""
    c = (PROUD - RO, RO - PROUD)
    return [(-LEG, -PROUD)] + arc(c[0], c[1], RO, 270, 360, n) + [(PROUD, LEG)]


def out_plan(n=10, path=None):
    """The pier's body: the face line closed through the building (0.35 behind each face, as the side bays' slab)."""
    return ccw((path or out_path(n)) + [(-BACKY, LEG), (-BACKY, BACKY), (-LEG, BACKY)])


def in_path(n=8):
    """The inside cove's face line, building on the left: along face A (y = SKIN) toward the vertex, the cove R about (SKIN - RI, SKIN - RI), down face B (x = SKIN)."""
    c = (SKIN - RI, SKIN - RI)
    return [(-LEG, SKIN)] + arc(c[0], c[1], RI, 90, 0, n) + [(SKIN, -LEG)]


def in_plan(n=8, path=None):
    return ccw((path or in_path(n)) + [(BACKY, -LEG), (BACKY, BACKY), (-LEG, BACKY)])


def _wrap(s, path, a0, a1, z0, z1, mat, r=0.0, lod=0):
    tube(s, lift(path), sec(a0, a1, z0, z1, r if lod == 0 else 0.0, n=1), mat)


def _line(s, path, a0, a1, z0, z1, mat):
    tube(s, lift(path), [(a0, z0), (a1, z0), (a1, z1), (a0, z1)], mat)


ARC = {"out": (7, 4, 2), "in": (6, 3, 2)}  # round segments per lod


def _pp(kind, lod):
    n = ARC[kind][lod]
    return (out_path(n), out_plan(n)) if kind == "out" else (in_path(n), in_plan(n))


def pier_base(F, kind, z0, lod):
    P, plan = _pp(kind, lod)
    g = F["g"]
    zf = g - 1.0

    def draw(s):
        _wrap(s, P, -0.06, 0.3, z0, z0 + 0.45, pal.Stone_Dark, 0.0, lod)
        if lod == 0:  # rustication: the stone recessed by the groove depth, its courses at the face line
            sunk = offset(P, GROOVE)
            s.extrude(out_plan(path=sunk) if kind == "out" else in_plan(path=sunk), z0 + 0.45, z0 + zf, pal.Limestone_Tex)
            zc = 0.45
            while zc < zf - 0.05:
                zn = min(zc + COURSE, zf)
                _wrap(s, P, 0.0, 0.05, z0 + zc + GROOVE / 2, z0 + zn - (GROOVE / 2 if zn < zf else 0.0), pal.Limestone_Tex)
                zc = zn
            if kind == "out":  # three seafoam speed lines round the corner, above head height
                for z in (zf - 1.38, zf - 1.16, zf - 0.94):
                    _wrap(s, P[1:-1], -0.03, 0.02, z0 + z, z0 + z + 0.07, pal.Enamel_Seafoam)
        else:
            s.extrude(plan, z0 + 0.45, z0 + zf, pal.Limestone_Tex)
        s.extrude(plan, z0 + zf, z0 + g, pal.Limestone_Tex)
        _wrap(s, P, -0.1, 0.3, z0 + zf, z0 + g, pal.Enamel_Teal, 0.03, lod)
        if lod == 0:
            for z in (zf + 0.06, g - 0.06):
                _line(s, P, -0.125, -0.1, z0 + z - 0.022, z0 + z + 0.022, pal.Gold)
    return draw


def pier_mid(F, kind, z0, lod):
    P, plan = _pp(kind, lod)
    fh, sp = F["fh"], F["sp"]

    def draw(s):
        s.extrude(plan, z0, z0 + fh, pal.Limestone_Tex)
        if kind == "in":  # the sill band turns the cove with the faces'
            if lod < 2:
                _wrap(s, P, -0.05, 0.02, z0 + sp - 0.22, z0 + sp - 0.06, pal.Enamel_Seafoam, 0.02, lod)
            _wrap(s, P, -0.45, 0.05, z0 + fh + F["brow"][0], z0 + fh + F["brow"][1], pal.Enamel_Ivory, 0.05, lod)
        else:  # the eyebrow at the pier's face, 0.1 proud: its outer edge is the faces' eyebrow line (0.45 off the building line), so the eyebrows run on round the corner
            _wrap(s, P, -0.1, 0.05, z0 + fh + F["brow"][0], z0 + fh + F["brow"][1], pal.Enamel_Ivory, 0.05, lod)
            if lod < 2:  # teal inlay up the round, between the sill and the eyebrow
                c, mid = (PROUD - RO, RO - PROUD), 315.0
                strip = arc(c[0], c[1], RO, mid - 11, mid + 11, 4 if lod == 0 else 2)
                _wrap(s, strip, -0.025, 0.02, z0 + 0.25, z0 + fh - 0.08, pal.Enamel_Teal)
    return draw


def pier_top(F, kind, z0, lod):
    P, plan = _pp(kind, lod)
    (b0, b1), c0, c1 = tops(F)

    def draw(s):
        s.extrude(plan, z0, z0 + c0, pal.Limestone_Tex)
        if F["attic"] and kind == "out" and lod < 2:  # the attic: the teal inlay continues up the round
            c, mid = (PROUD - RO, RO - PROUD), 315.0
            _wrap(s, arc(c[0], c[1], RO, mid - 11, mid + 11, 4 if lod == 0 else 2), -0.025, 0.02, z0 + 0.25, z0 + b0 - 0.25, pal.Enamel_Teal)
        if lod < 2:
            _wrap(s, P, -0.07, 0.02, z0 + b0, z0 + b1, pal.Enamel_Seafoam, 0.03, lod)
        _wrap(s, P, -0.14, 0.4, z0 + c0, z0 + c1, pal.Enamel_Ivory, 0.06, lod)
        if lod == 0 and (kind == "out" or F["attic"]):  # a brass hairline under the coping, round the pier
            zb = c0 - 0.07 if not F["attic"] else 3.2
            _line(s, P[1:-1] if kind == "out" else P, -0.11, -0.07, z0 + zb, z0 + zb + 0.04, pal.Gold)
    return draw


def pier_piece(F, kind, mods, lod=0):
    def build(s):
        for fn, z0 in mods:
            fn(F, kind, z0, lod)(s)
    return build


def _pier(name, F, kind, mods):
    asset(name, "Sides", bevel=0.0, near=pier_piece(F, kind, mods, 1), far=pier_piece(F, kind, mods, 2))(pier_piece(F, kind, mods, 0))


for _k, _tag in (("out", "Out"), ("in", "In")):
    for _n in (3, 4):
        _pier(f"CornerPier_{_tag}_{_n}", KIT, _k, [(pier_base, 0.0)] + [(pier_mid, KIT["g"] + j * KIT["fh"]) for j in range(_n)] + [(pier_top, KIT["g"] + _n * KIT["fh"])])
    for _t, _fn in (("Base", pier_base), ("Mid", pier_mid), ("Top", pier_top)):
        _pier(f"CornerPier_{_tag}_{_t}", PLAN, _k, [(_fn, 0.0)])
