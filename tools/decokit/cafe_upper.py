"""The café's body above its ground storey (brief 066, Vela): the streamline tiers of the 26 Sep paintings (inez-cutaway, gameplay-plan) in place of the
generator's plain limestone mass, so the corner café reads at play zoom as the paintings draw it under the height cap.

  CafeUpper_3   the café at 3 storeys (H(3) = 16.8 m to the roof line): two upper storeys over CafeBay_v2, the crown terrace and its solarium.
  CafeUpper_4   the café at 4 storeys (H(4) = 20.4 m, plan 0.4.0): three upper storeys, the same crown.

Sky has not yet picked 3 or 4 storeys for the café; both pieces are built and the world takes the one whose roof line is the block's café height, so the
storey count stays the block's. What each storey carries, all the way round (the café is seen from every quarter: at yaw 210 the camera looks at its back):
  a cream limestone spandrel with a seafoam enamel speed band and a brass hairline (brass is a line only), a ribbon of window film between thin chrome mullions
  with two reflection streaks a pane, and an ivory eyebrow with a chrome nose that follows the curve (the paintings' rounded bands). The back corners are
  rounded R 2 inside the shell's square ones; the back's centre is a stair bay, a tall strip of glazing in chrome bars between limestone pilasters with a
  seafoam fin up its middle.
The crown (the plan's 3.95 m above the eaves): a parapet with the seafoam band and an ivory coping, a planted trough behind it the length of the front (the
civic planting, variant A), two chrome spheres on short pylons where the arcs meet the flat, and a glazed solarium set back 3.4 m from the front, its corners
rounded R 3 (curvy, never a mitre), standing to the roof line. Nothing passes the roof line: the play streets' cap holds.

Frame: CafeBay_v2's (pivot = centre of the café's flat front on the ring line, front -Y, the building on +Y, z 0 = the pavement top); the piece starts at
z 4.7 where the ground storey's belt ends. The outline is the Oct 9 shell's (civic-block.json, rule C-0): 11.4 m flat between two R 5.3 arcs, sides at
x +-11, back at y 11.3. Floor lines are the district's (plinth 0.45 + ground 5.2 + 3.6 a storey), so the café's storeys line up with its neighbours'."""

import math

from mathutils import Vector

import cafe_grand as cg
from civic_common import HF, R, band, front_path, hair, planting, trough
from facades import fin_plan
from kit import asset
from mats import pal
from plaza_parts import lift, offset, ribbon
from structures import along, ccw, sec, tube

Z0 = 4.7                         # top of CafeBay_v2's belt: the piece starts here
PLINTH, GROUND, STOREY, CROWN = 0.45, 5.2, 3.6, 3.95   # standards.buildings (plan 0.4.0)
X_SIDE, Y_BACK = 11.0, 11.3
SILL, HEAD = 0.9, 2.75           # ribbon glass above each floor
RECESS = 0.28                    # glass set back from the stone face
EYEBROW = 0.55                   # eyebrow projection
SETBACK = 3.4                    # the solarium's front, back from the café front
SOL_R = 3.0                      # its corner radius
SOL_X = 8.6                      # its half width


def floors(n):
    """Floor levels of the upper storeys 2..n, the eaves and the roof line, for the café at n storeys."""
    fl = [PLINTH + GROUND + STOREY * (k - 2) for k in range(2, n + 1)]
    eaves = PLINTH + GROUND + STOREY * (n - 1)
    return fl, eaves, eaves + CROWN


RB = 2.0                         # the back corners of the upper body, rounded inside the shell's square ones (curvy, never a mitre)
STAIR = 2.0                      # half width of the stair bay at the back centre
STAIR_GLASS = 0.01               # brief 109: the stair glazing stands 1 cm proud of the stone line (its face lay on the spandrels' and the stair stone's, 0 mm:
                                 # the film, transparent and depth-tested, z-fought the stone and flickered once the night lit it); inside the chrome bars (0.05)


def _back_arc(sx):
    """The rounded back corner on side sx (-1 west, +1 east), in travel order (CCW: east goes side -> back, west back -> side)."""
    from geo import arc
    if sx > 0:
        return arc(X_SIDE - RB, Y_BACK - RB, RB, 0, 90, 6)
    return arc(-X_SIDE + RB, Y_BACK - RB, RB, 90, 180, 6)


def outline():
    """The upper body's closed outline, counter-clockwise (the building on the left of travel): the west back corner, the west side down, the front,
    the east side up, the east back corner; the back closes it."""
    return _back_arc(-1) + front_path(10, 6) + _back_arc(1)


def wrap_path():
    """The ribbons' path: from the stair bay west along the back, round the west corner, down the side, the front, up the east side, round, back to the stair bay."""
    return [(-STAIR, Y_BACK)] + outline() + [(STAIR, Y_BACK)]


def solarium_outline():
    """The crown's set-back solarium: a rectangle x +-SOL_X, y SETBACK..Y_BACK - 0.5 with its two front corners rounded R SOL_R, CCW."""
    from geo import arc
    y0, y1 = SETBACK, Y_BACK - 0.5
    left = arc(-SOL_X + SOL_R, y0 + SOL_R, SOL_R, 180, 270, 8)
    right = arc(SOL_X - SOL_R, y0 + SOL_R, SOL_R, 270, 360, 8)
    return ccw([(-SOL_X, y1)] + left + right + [(SOL_X, y1)])


def streak_pair(s, path, d0, d1, z0, z1, a):
    """Two diagonal reflection streaks on the pane from arc length d0 to d1 of `path`, laid `a` m outward of it (as CafeBay_v2's glass throws the street)."""
    at, _ = cg._sampler(path)
    w = d1 - d0
    for f, wd in ((0.18, 0.18), (0.48, 0.07)):
        da = d0 + w * f
        slant = min(0.55, w * 0.35)
        pts = []
        for d, z in ((da, z0), (da + wd, z0), (da + wd + slant, z1), (da + slant, z1)):
            p, t = at(d)
            q = Vector((p.x, p.y)) + cg._out(t) * a
            pts.append((q.x, q.y, z))
        o = cg._out(at((d0 + d1) / 2)[1])
        cg._toward(s, pts, pal.Glass_Sheen, (o.x, o.y, 0))


def ribbon_storey(s, W, zf):
    """One storey's ribbon along the wrap: window film recessed RECESS behind the stone line, chrome mullions every ~1.45 m, streaks, brass sill line."""
    z0, z1 = zf + SILL, zf + HEAD
    G = offset(W, RECESS)
    ribbon(s, G, z0, z1, pal.GlassDark)
    at, tot = cg._sampler(G)
    n = max(2, round(tot / 1.45))
    st = [tot * i / n for i in range(n + 1)]
    for d in st[1:-1]:
        p, t = at(d)
        with s.at((p.x, p.y, 0), (0, 0, math.degrees(math.atan2(t.y, t.x)))):
            s.box((-0.035, -0.06, z0), (0.035, 0.02, z1), pal.Chrome)
    for d0, d1 in zip(st, st[1:]):
        streak_pair(s, G, d0 + 0.06, d1 - 0.06, z0 + 0.1, z1 - 0.1, 0.004)
    # reveals: the stone returns into the recess at the ribbon's two ends
    for i, k in ((0, 1), (-1, -2)):
        a, b = Vector(W[i]), Vector(W[k])
        t = (b - a).normalized() if i == 0 else (a - b).normalized()
        q0, q1 = a, a + Vector((-t.y, t.x)) * RECESS
        cg._toward(s, [(q0.x, q0.y, z0), (q1.x, q1.y, z0), (q1.x, q1.y, z1), (q0.x, q0.y, z1)], pal.Limestone, (t.x, t.y, 0) if i == 0 else (-t.x, -t.y, 0))


def crown(s, eaves, top):
    """Parapet, the planted trough behind the front, the two chrome spheres, the terrace deck and the set-back solarium to the roof line."""
    O = outline()
    P = front_path(10, 6)
    zp = eaves + 1.1
    tube(s, lift(O), sec(0.0, 0.32, eaves, zp), pal.Limestone, closed=True)                  # parapet wall
    tube(s, lift(O), sec(-0.06, 0.38, zp, zp + 0.08), pal.Enamel_Ivory, closed=True)            # coping
    band(s, O, -0.05, 0.0, eaves + 0.35, eaves + 0.65, pal.Enamel_Seafoam, closed=True)        # speed band on the parapet
    hair(s, O, -0.07, eaves + 0.72, 0.035, pal.Brass_Civic, closed=True)
    # the terrace deck inside the parapet (pale, the plaza's terrazzo) and the planted trough along the front
    inner = offset(O, 0.32, closed=True)
    s.extrude(ccw(inner), eaves - 0.25, eaves + 0.05, pal.Terrazzo_Ivory, bottom=False)
    T = offset(front_path(10, 6), 0.9)
    with s.at((0, 0, eaves + 0.05)):
        trough(s, T, z_top=0.55, w=0.8)
        planting(s, T, "A", z0=0.55, step=0.55, w=0.7, rows=2)
    # chrome spheres on short limestone pylons where the arcs meet the flat
    for x in (-HF, HF):
        with s.at((x, 0.2, zp + 0.08)):
            s.extrude(ccw([(-0.32, -0.32), (0.32, -0.32), (0.32, 0.32), (-0.32, 0.32)]), 0.0, 0.7, pal.Limestone)
            s.extrude(ccw([(-0.36, -0.36), (0.36, -0.36), (0.36, 0.36), (-0.36, 0.36)]), 0.7, 0.78, pal.Enamel_Seafoam)
            s.lathe([(0.0, 0.78), (0.12, 0.8), (0.12, 0.86)], pal.Chrome, seg=16, cap_bottom=False)
            s.lathe([(math.sin(math.pi * i / 16) * 0.42, 0.86 + 0.42 - math.cos(math.pi * i / 16) * 0.42) for i in range(17)], pal.Chrome, seg=24, cap_bottom=False, cap_top=False)
    # the solarium: stone base, a ribbon of film between chrome mullions, a seafoam band and an ivory roof slab with a chrome edge, to the roof line
    S = solarium_outline()
    zs0, zs1 = eaves + 0.05, top
    g0, g1 = zs0 + 0.45, zs1 - 0.7
    tube(s, lift(S), sec(0.0, 0.3, zs0, g0), pal.Limestone, closed=True)
    ribbon(s, offset(S, 0.15, closed=True), g0, g1, pal.GlassDark, closed=True)
    stations = along(S, 1.6, margin=0.4)
    for p, t in stations:
        with s.at((p.x, p.y, 0), (0, 0, math.degrees(math.atan2(t.y, t.x)))):
            s.box((-0.035, -0.04, g0), (0.035, 0.18, g1), pal.Chrome)
    Sg = offset(S, 0.15, closed=True)
    _, tot = cg._sampler(Sg)
    k = max(2, round(tot / 1.6))
    for i in range(k):
        streak_pair(s, Sg, tot * i / k + 0.08, tot * (i + 1) / k - 0.08, g0 + 0.1, g1 - 0.1, 0.004)
    tube(s, lift(S), sec(0.0, 0.3, g1, zs1 - 0.25), pal.Limestone, closed=True)
    band(s, S, -0.04, 0.0, g1 + 0.08, g1 + 0.3, pal.Enamel_Seafoam, closed=True)
    s.extrude(ccw(offset(S, -0.25, closed=True)), zs1 - 0.25, zs1, pal.Enamel_Ivory)
    tube(s, lift(offset(S, -0.25, closed=True)), sec(-0.03, 0.03, zs1 - 0.03, zs1 + 0.01), pal.Chrome, closed=True)  # brief 109: 1 cm over the slab (its top lay on the slab's: a flickering line along the roof)
    s.extrude(ccw(offset(S, 0.3, closed=True)), g0 - 0.02, g0, pal.Enamel_Charcoal, bottom=False)   # the dark room behind the film


def cafe_upper(s, n):
    fl, eaves, top = floors(n)
    O, W = outline(), wrap_path()
    # the core: stone to the eaves behind everything (the glass film lies on it, recessed)
    s.extrude(ccw(offset(O, RECESS + 0.02, closed=True)), Z0, eaves, pal.Limestone)
    # per storey: the spandrel (stone, flush with the outline) from under the floor to the sill, the ribbon, the head band, the eyebrow
    lows = [Z0] + [zf + HEAD for zf in fl[:-1]]
    for i, zf in enumerate(fl):
        z_lo = lows[i]
        tube(s, lift(O), sec(0.0, RECESS + 0.04, z_lo, zf + SILL), pal.Limestone, closed=True)   # spandrel, all round
        band(s, O, -0.04, 0.0, zf + 0.2, zf + 0.5, pal.Enamel_Seafoam, closed=True)               # speed band, all round
        hair(s, W, -0.06, zf + 0.58, 0.035, pal.Brass_Civic)                                      # its brass line, along the wrap
        ribbon_storey(s, W, zf)
        # the stair bay at the back centre: stone over the ribbon's height (its glass-block strip and fin are drawn once, below)
        tube(s, lift([(STAIR, Y_BACK), (-STAIR, Y_BACK)]), sec(0.0, RECESS + 0.04, zf + SILL, zf + HEAD), pal.Limestone)
        # head: stone from the ribbon head to the next floor's spandrel (the last storey: to the eaves), the eyebrow over the ribbon
        z_hi = eaves if i == len(fl) - 1 else zf + HEAD
        if z_hi > zf + HEAD:
            tube(s, lift(O), sec(0.0, RECESS + 0.04, zf + HEAD, z_hi), pal.Limestone, closed=True)
        tube(s, lift(W), sec(-EYEBROW, 0.02, zf + HEAD + 0.02, zf + HEAD + 0.16, 0.03), pal.Enamel_Ivory)
        hair(s, W, -EYEBROW - 0.01, zf + HEAD + 0.09, 0.05, pal.Chrome)
    stair(s, eaves)
    crown(s, eaves, top)


def stair(s, eaves):
    """The back's stair bay (the face the camera sees at yaw 210): a tall strip of glazing in chrome bars between limestone pilasters, a seafoam fin up its middle
    with chrome rings over the parapet; nothing passes the roof line."""
    yb = Y_BACK
    for x0, x1 in ((-STAIR, -1.25), (1.25, STAIR)):
        s.rbox((x0, yb - 0.1, Z0), (x1, yb + 0.22, eaves + 1.1), 0.06, pal.Limestone)
    s.box((-1.25, yb + STAIR_GLASS, Z0 + 0.6), (1.25, yb + STAIR_GLASS + 0.01, eaves - 0.4), pal.GlassDark)
    z = Z0 + 0.6
    while z < eaves - 0.4:
        s.box((-1.25, yb, z), (1.25, yb + 0.05, z + 0.04), pal.Chrome)
        z += 0.45
    for x in (-0.62, 0.62):
        s.box((x - 0.02, yb, Z0 + 0.6), (x + 0.02, yb + 0.05, eaves - 0.4), pal.Chrome)
    with s.at((0.0, yb, 0.0), (0, 0, 180)):
        s.extrude(fin_plan(-0.22, 0.22, -0.95, 0.05), Z0 + 1.2, eaves + 2.6, pal.Enamel_Seafoam)
        for zr in (eaves + 1.5, eaves + 1.9, eaves + 2.3):
            s.extrude(fin_plan(-0.24, 0.24, -0.97, 0.05), zr, zr + 0.14, pal.Chrome)


for _n in (3, 4):
    asset(f"CafeUpper_{_n}", "Plaza", bevel=0.0)(lambda s, n=_n: cafe_upper(s, n))
