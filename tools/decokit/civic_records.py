"""RecordsEntrance (brief 008 B1): the Records Office public front as in the 26 Sep paintings (records-office, gameplay-plan,
inez-cutaway, mission-block): a tall arched window with the painting's ball in its arch on three slim columns, over a seafoam marquee with two lanterns,
brass double doors under it, three chrome-and-gold blades rising from the arch head, cream flank piers with ball
finials and comet banners, and a flight of five treads with chrome rails and cheek walls.
Brief 103 (Sky, 2026-11-10, from briefs/103/vela/globe/board.png): the ball (was a 2.4 m chrome relief globe on a stand) and the four ball finials (the piers' two and the
marquee's two corner finials, so the official front's too) are satin nickel, DK_Nickel_Satin (civic_common.py).

Frame: pivot = the centre of the tower face at the pavement (z = 0), front -Y (block -x, north), the tower on +Y. Built for
tower-public (6 x 6 m, face at block x 52.5, centred y -30): the door is ON the face (Vantage moves the entrance point from
x 54.65 to the face, the landing z = 0.75), the piers stand beside the tower, plinths x +-3.02..3.98 (7.96 m across in all, the box is exactly that wide: caps, banner
rods and knobs are inside the plinth footprint), y -1.03..+0.23 about the face. Steps are real geometry: 6 risers of 0.125 m (0.75 m), going 0.36 m,
landing 1.5 m deep (x 51..52.5 in the block), flight 3.3 m out (foot x 49.2), cheeks to 3.4 m (x 49.1), marquee 2.3 m out.
RecordsEntrance_Official: the lean FLUSH version for tower-official (brief 008 B2): no landing, steps, cheeks or rails; the door
sill is at the pavement (z 0); the canopy fills the block's office-boarding-pavilion envelope (3.0 m out of the face, the tower's
6.0 m width, soffit 4.05, top 4.62, carried on the face, no posts); the piers are 0.3 m pilasters on the face.
Extents are checked on the built mesh by verify008.py (block frame numbers in out/decokit-008/measured.json)."""

import math

from geo import arc, fillet, rrect
from kit import asset
from mats import pal
from plaza import canopy_plan
from plaza_parts import lift, uvface
from structures import arch, arch_band, ccw, sec, soft_extrude, tube, xz
from civic_common import hair

FRONT, SIDE_R, SIDE_L = (90, 0, 0), (90, 0, 90), (90, 0, -90)
ZL, NR, RISE, GO, LAND = 0.75, 6, 0.125, 0.36, 1.5   # landing height, risers, rise, going, landing depth
SW = 2.3                                              # half width of the flight
PR, SU, PS = 1.65, 2.2, 7.6                           # window half width, surround outer half width, arch spring
ZC0, ZC1 = 4.05, 4.62                                 # marquee soffit and fascia top (the official canopy: the office-boarding-pavilion envelope)
ZG0, ZG1, DG = 4.5, 5.0, 1.8                          # the public marquee (brief 023): soffit, fascia top, projection. Was 4.05, 4.62, 2.3: at the play camera (38 deg, yaw 30) the old
                                                      # marquee hid the top half of a figure on the landing; now a figure 0.75 m off the face is whole under it, and the lift and the
                                                      # shorter reach cost the arch window 0.45 m of straight side and the doors a taller transom, nothing else
EDGE = 0.1375                                         # the fascia sections reach 0.11 and the gold bead 0.14 past the plan line: the plan is drawn that far inside the envelope
PX, PW, PD0, PD1 = 3.5, 0.48, -1.03, 0.23             # public pier: centre, plinth half width, plinth front and back (y)
GC, GR = (0.0, -0.9, 8.0), 0.75                       # brief 103: the ball's centre and radius, 1.5 m across. The centre is 0.4 m over the arch spring PS (arch top 9.25), the foot 2.25 m
                                                      # over the marquee top ZG1, and the back 0.15 m off the face, 1 cm clear of the window's brass grid


def sphere(s, c, r, mat, seg=12, n=6):
    prof = [(r * math.sin(math.pi * k / n), -r * math.cos(math.pi * k / n)) for k in range(n + 1)]
    with s.at(c):
        s.lathe(prof, mat, seg=seg)


def banner(s, x0, x1, y, zt, length, tip=0.5, kr=0.055):
    """plaza_parts.banner at a quarter of its triangles: comet fabric slab, comet art both faces, gold rod, small knobs."""
    xm, zb, zv = (x0 + x1) / 2, zt - length, zt - length + tip
    zs = zt - (x1 - x0) * 1.5 * 0.7 * 1.3
    with s.at((0, y, 0), (90, 0, 0)):
        s.extrude([(x0, zv), (xm, zb), (x1, zv), (x1, zt), (x0, zt)], -0.012, 0.012, pal.Cloth_Seafoam)

    def v(z):
        return 0.3 + 0.7 * (z - zs) / (zt - zs) if z >= zs else 0.04 + 0.26 * (z - zb) / (zs - zb)
    for side, yy in ((1, y - 0.016), (-1, y + 0.016)):
        def u(x):
            return (x - x0) / (x1 - x0) if side > 0 else (x1 - x) / (x1 - x0)
        for poly in ([(x0, zs), (x1, zs), (x1, zt), (x0, zt)], [(x0, zv), (xm, zb), (x1, zv), (x1, zs), (x0, zs)]):
            pts = poly if side > 0 else poly[::-1]
            uvface(s, [(x, yy, z) for x, z in pts], pal.Banner_Comet_Seafoam, [(u(x), v(z)) for x, z in pts])
    s.sweep([(x0 - 0.08, y, zt + 0.05), (x1 + 0.08, y, zt + 0.05)], 0.028, pal.Brass_Civic, seg=5, caps=False)
    for x in (x0 - 0.1, x1 + 0.1):
        sphere(s, (x, y, zt + 0.05), kr, pal.Brass_Civic, seg=8, n=4)


def globe(s, c, r, zroof, mat, cup=0.22):
    """Brief 103 (Sky, 2026-11-10: option A): the 26 Sep records-office painting's ball, smooth, at c in the window's arch, on the painting's three slim columns
    from a low rounded plinth on the marquee roof (zroof). The middle column ends in a flared cradle under the ball. The two beside it stop 0.7 m short with
    domed tops: the crown's three blades said again in metal. One material for all of it (satin nickel). It was a 2.4 m relief globe in mirror chrome
    on a stand, hanging over the marquee and the door guard; in game light the chrome mirrored one probe and read as pale-blue glass."""
    sphere(s, c, r, mat, seg=56, n=28)
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


def window(s, grand, z0=ZC0, z1=ZC1):
    """Tall arched window on the face above the marquee: seafoam and mint surround with an 8 cm deep-teal reveal, dark glass, brass grid."""
    with s.at((0, 0, 0), FRONT):
        soft_extrude(s, arch_band(PR, PR + 0.08, PS, z0, n=14), 0.0, 0.18, pal.Enamel_Teal, 0.02)       # deep teal: an 8 cm reveal line, no more
        soft_extrude(s, arch_band(PR + 0.08, PR + 0.32, PS, z0, n=14), 0.0, 0.16, pal.Enamel_Seafoam, 0.05)
        soft_extrude(s, arch_band(PR + 0.32, SU, PS, z0, n=14), 0.0, 0.07, pal.Enamel_Mint, 0.03)
    head = arch(2 * PR, PS, z0, n=14)
    s.face([(x, -0.03, z) for x, z in head], pal.Enamel_Charcoal)
    s.face([(x, -0.07, z) for x, z in head], pal.GlassDark)
    s.sweep(xz(arc(0, PS, PR - 0.04, 0, 180, 14), -0.1), 0.035, pal.Brass_Civic, seg=4)
    for x in (-0.6, 0.6):
        s.box((x - 0.04, -0.14, z1 + 0.2), (x + 0.04, -0.07, PS + math.sqrt(PR * PR - x * x) - 0.05), pal.Brass_Civic)
    for z in (z1 + 0.2, PS):
        s.box((-PR, -0.14, z - 0.035), (PR, -0.07, z + 0.035), pal.Brass_Civic)


def blades(s):
    """Three steel blades with gold spines rising from the arch head (the crown fins of the painting)."""
    for x, w, z0, z1 in ((0.0, 0.26, PS + PR + 0.3, 13.6), (-0.8, 0.18, PS + PR + 0.15, 12.2), (0.8, 0.18, PS + PR + 0.15, 12.2)):
        h = z1 - z0
        lens = [(0, z0), (w, z0 + 0.25 * h), (w * 0.75, z0 + 0.75 * h), (0, z1), (-w * 0.75, z0 + 0.75 * h), (-w, z0 + 0.25 * h)]
        with s.at((x, 0, 0), FRONT):
            s.extrude(lens, 0.04, 0.2, pal.Steel)  # brushed steel: a mirror chrome slab vanishes against the cream tower face and the sky
        s.box((x - 0.012, -0.205, z0 + 0.3 * h), (x + 0.012, -0.195, z1 - 0.1), pal.Brass_Civic)


def doors(s, wide, zl=ZL, zt0=ZC0):
    """Brass double doors with side lights and a transom: dark glass in gold frames, chrome pulls, kick plates. zl = sill height."""
    ZL = zl  # noqa: N806 (local: the public front stands on its landing at 0.75, the official door is flush at 0)
    y, z1, hd = -0.12, zt0, 2.5
    zt = ZL + hd
    s.face([(-wide, y, ZL), (wide, y, ZL), (wide, y, z1), (-wide, y, z1)], pal.GlassDark)
    s.face([(-wide, y + 0.09, ZL), (wide, y + 0.09, ZL), (wide, y + 0.09, z1), (-wide, y + 0.09, z1)], pal.Enamel_Charcoal)
    for x in (-wide, -1.1, 0.0, 1.1, wide):
        s.box((x - 0.045, y - 0.06, ZL), (x + 0.045, y + 0.03, z1), pal.Brass_Civic)
    for z in (ZL, zt - 0.04, z1 - 0.06):
        s.box((-wide, y - 0.06, z), (wide, y + 0.03, z + 0.07), pal.Brass_Civic)
    s.box((-1.05, y - 0.07, ZL + 0.02), (1.05, y + 0.03, ZL + 0.3), pal.Brass_Civic)  # kick plates
    for x in (-0.16, 0.16):
        s.sweep([(x, y - 0.14, ZL + 1.0), (x, y - 0.14, ZL + 1.55)], 0.02, pal.Chrome, seg=5)
        for z in (ZL + 1.0, ZL + 1.55):
            s.sweep([(x, y - 0.03, z), (x, y - 0.14, z)], 0.014, pal.Chrome, seg=4, caps=False)


def marquee(s, hw, d, z0=ZC0, z1=ZC1):
    """hw, d = the envelope (half width along the face, projection): nothing of the canopy reaches past it."""
    path = canopy_plan(hw - EDGE, d - EDGE, 3.0, 16)
    s.extrude(path, z0, z0 + 0.1, pal.Enamel_Ivory)
    zf = z1 - 0.18
    tube(s, lift(path), sec(-0.07, 0.3, z0 + 0.02, zf, 0.0), pal.Enamel_Seafoam)  # fascia band: seafoam (deep teal read as a dark marquee in Unity)
    tube(s, lift(path), sec(-0.11, 0.36, zf, z1, 0.0), pal.Enamel_Mint)
    s.extrude(path, zf - 0.07, z1 - 0.03, pal.Enamel_Seafoam)
    hair(s, path, -0.1, z0 + 0.05, 0.035, pal.Brass_Civic)
    hair(s, path, -0.12, zf, 0.035, pal.Brass_Civic)
    for x in (-hw * 0.5, hw * 0.5):
        with s.at((x, -d * 0.62, z0)):
            bulb = [(0.17 * math.cos(t), -0.03 - 0.12 * math.sin(t)) for t in (math.pi / 2 * k / 4 for k in range(1, 5))]
            s.lathe([(0.19, 0.0), (0.19, -0.03)] + bulb, pal.Lamp_Deco, seg=12)
    for x in (-hw + 0.2, hw - 0.2):  # ball finials on the marquee corners, satin nickel as the ball (brief 103; both fronts)
        with s.at((x, -0.6, z1 - 0.03)):
            s.lathe([(0, 0), (0.1, 0), (0.1, 0.05), (0.045, 0.1), (0.04, 0.55), (0.07, 0.6), (0, 0.62)], pal.Nickel_Satin, seg=10)
        sphere(s, (x, -0.6, z1 + 0.84), 0.2, pal.Nickel_Satin, seg=14, n=7)


def stair_block(s, mat):
    """Brief 055 (K-1, collide): the landing, the five treads and the two cheek walls as ONE closed solid. They were six boxes and two extrusions that touched face to face, so every
    shared face was an inner wall and the stair read from outside as thin treads with open risers: a ray, a collider or a voxeliser could not tell it was solid.
    The footprint is cut on the lines of the landing, the treads and the cheeks; each cell is a column from z 0 to its top; a face is made only where a column meets air or a lower
    column (top, riser, cheek wall, foot, back, underside), every vertex shared. The same shape, material and numbers as before."""
    L1 = LAND + (NR - 1) * GO
    run = L1 + 0.1 - LAND
    xs = [-(SW + 0.26), -SW, SW, SW + 0.26]
    ys = [0.0, -LAND] + [-(LAND + k * GO) for k in range(1, NR)] + [-(L1 + 0.1)]    # from the tower face out (y falls)

    def cheek(y):                                                                   # the coping: level over the landing, then falling to 0.5 at the foot
        u = -y
        return ZL + 0.5 if u <= LAND + 1e-9 else ZL + 0.5 - (u - LAND) * (ZL / run)

    def tread(ym):
        u = -ym
        if u < LAND:
            return ZL                                                               # the landing
        k = int((u - LAND) / GO) + 1
        return ZL - k * RISE if k < NR else None                                    # past the last tread: air between the cheeks

    cell = {}
    for ix in range(3):
        for iy in range(len(ys) - 1):
            ya, yb = ys[iy], ys[iy + 1]
            if ix == 1:
                h = tread((ya + yb) / 2)
                cell[ix, iy] = None if h is None else (h, h)
            else:
                cell[ix, iy] = (cheek(ya), cheek(yb))
    verts, index, faces = [], {}, []

    def vid(p):
        k = tuple(round(c, 5) for c in p)
        if k not in index:
            index[k] = len(verts)
            verts.append(k)
        return index[k]

    def face(pts, out):                                                             # wound so that the normal points out; coincident corners (a triangle wall) are dropped
        pts = [p for i, p in enumerate(pts) if tuple(round(c, 5) for c in p) != tuple(round(c, 5) for c in pts[i - 1])]
        n = [0.0, 0.0, 0.0]
        for i, p in enumerate(pts):
            q = pts[(i + 1) % len(pts)]
            n[0] += (p[1] - q[1]) * (p[2] + q[2]); n[1] += (p[2] - q[2]) * (p[0] + q[0]); n[2] += (p[0] - q[0]) * (p[1] + q[1])
        faces.append([vid(p) for p in (pts[::-1] if sum(a * b for a, b in zip(n, out)) < 0 else pts)])
    for (ix, iy), c in sorted(cell.items()):
        if c is None:
            continue
        x0, x1, ya, yb = xs[ix], xs[ix + 1], ys[iy], ys[iy + 1]
        ta, tb = c
        face([(x0, ya, ta), (x1, ya, ta), (x1, yb, tb), (x0, yb, tb)], (0, 0, 1))
        face([(x0, ya, 0.0), (x1, ya, 0.0), (x1, yb, 0.0), (x0, yb, 0.0)], (0, 0, -1))
        side = lambda n: cell.get(n)  # noqa: E731
        for nb, ends, out in ((side((ix - 1, iy)), (x0, x0), (-1, 0, 0)), (side((ix + 1, iy)), (x1, x1), (1, 0, 0))):
            na, nbb = (0.0, 0.0) if nb is None else nb  # the neighbour's tops at ya and yb
            if (ta - na > 1e-9 or tb - nbb > 1e-9) and ta >= na - 1e-9 and tb >= nbb - 1e-9:
                face([(ends[0], ya, ta), (ends[1], yb, tb), (ends[1], yb, nbb), (ends[0], ya, na)], out)
        for nb, y, top_here, top_there, out in ((side((ix, iy - 1)), ya, ta, None, (0, 1, 0)), (side((ix, iy + 1)), yb, tb, None, (0, -1, 0))):
            there = 0.0 if nb is None else (nb[1] if out[1] > 0 else nb[0])         # the neighbour's top on the shared line
            if top_here - there > 1e-9:
                face([(x0, y, top_here), (x1, y, top_here), (x1, y, there), (x0, y, there)], out)
    s._add(verts, faces, mat, False)


def stairs(s):
    """Landing, five treads and cheek walls (one closed solid, see stair_block), two chrome handrails on posts (gooseneck at both ends)."""
    stair_block(s, pal.Limestone)
    L1 = LAND + (NR - 1) * GO
    # the solids the flight is made of, declared from the same numbers (brief 055): landing and treads to stand on, the cheek walls at the height of their coping where each span starts
    s.solid((-SW, -LAND, 0.0), (SW, 0.0, ZL), stand=True)
    for k in range(1, NR):
        s.solid((-SW, -(LAND + k * GO), 0.0), (SW, -(LAND + (k - 1) * GO), ZL - k * RISE), stand=True)
    spans = [(0.0, LAND)] + [(LAND + (k - 1) * GO, LAND + k * GO) for k in range(1, NR)] + [(L1, L1 + 0.1)]
    for sx in (-1, 1):
        x0, x1 = sorted((sx * SW, sx * (SW + 0.26)))
        for u0, u1 in spans:
            s.solid((x0, -u1, 0.0), (x1, -u0, ZL + 0.5 if u0 < LAND else ZL + 0.5 - (u0 - LAND) * (ZL / (L1 + 0.1 - LAND))))
    slope = RISE / GO
    zl = lambda u: ZL - (u - LAND) * slope  # noqa: E731 (tread nosing line, u = distance out of the face)
    posts = [(LAND - 0.25, ZL, ZL + 0.9)] + [(LAND + (k - 0.5) * GO, ZL - k * RISE, zl(LAND + (k - 0.5) * GO) + 0.9) for k in (2, 5)]
    for sx in (-1, 1):
        x = sx * 1.75
        rail = fillet([(x, -(LAND - 0.25), ZL + 0.9), (x, -LAND, ZL + 0.9), (x, -L1, zl(L1) + 0.9), (x, -(L1 + 0.07), zl(L1) + 0.9), (x, -(L1 + 0.07), 0.0)], 0.2, n=3, closed=False)
        s.sweep(rail, 0.03, pal.Chrome, seg=6)
        for u, z0, z1 in posts:
            s.sweep([(x, -u, z0), (x, -u, z1)], 0.02, pal.Chrome, seg=5)
            with s.at((x, -u, z0)):
                s.lathe([(0, 0), (0.07, 0), (0.07, 0.02), (0.04, 0.05), (0, 0.06)], pal.Chrome, seg=8)


def pier(s, x, h, ball):
    """Cream flank pier inside its plinth footprint (x +-0.48, y -1.03..0.23): mint band, seafoam cap, a satin-nickel ball on a stand (brief 103)."""
    s.rbox((x - 0.41, -0.96, 0.0), (x + 0.41, 0.16, h), 0.04, pal.Limestone, seg=1)
    s.box((x - PW, PD0, 0.0), (x + PW, PD1, 0.35), pal.Limestone)
    s.solid((x - PW, PD0, 0.0), (x + PW, PD1, h + 0.28))  # brief 055: the plinth footprint, to the cap
    s.box((x - 0.45, PD0 + 0.03, h - 0.5), (x + 0.45, PD1 - 0.03, h - 0.28), pal.Enamel_Mint)
    s.box((x - PW, PD0, h), (x + PW, PD1, h + 0.28), pal.Enamel_Seafoam)
    hair(s, [(x - 0.45, PD0 + 0.015), (x + 0.45, PD0 + 0.015)], 0.0, h + 0.03, 0.03, pal.Brass_Civic)
    if ball:
        with s.at((x, -0.4, h + 0.28)):
            s.lathe([(0, 0), (0.3, 0), (0.3, 0.05), (0.15, 0.13), (0.1, 0.22), (0, 0.22)], pal.Nickel_Satin, seg=14)
        sphere(s, (x, -0.4, h + 0.28 + 0.22 + 0.42), 0.42, pal.Nickel_Satin, seg=18, n=9)


def pilaster(s, x, h):
    """Official front: a 0.3 m pilaster on the tower face (x +-0.35, y -0.30..+0.20): plinth, shaft, mint band, seafoam cap."""
    s.box((x - 0.35, -0.30, 0.0), (x + 0.35, 0.2, 0.3), pal.Limestone)
    s.box((x - 0.31, -0.26, 0.3), (x + 0.31, 0.2, h), pal.Limestone)
    s.solid((x - 0.35, -0.30, 0.0), (x + 0.35, 0.2, h + 0.25))  # brief 055: plinth to cap
    s.box((x - 0.33, -0.29, h - 0.5), (x + 0.33, 0.2, h - 0.28), pal.Enamel_Mint)
    s.box((x - 0.35, -0.30, h), (x + 0.35, 0.2, h + 0.25), pal.Enamel_Seafoam)
    hair(s, [(x - 0.33, -0.285), (x + 0.33, -0.285)], 0.0, h + 0.03, 0.03, pal.Brass_Civic)


def build(s, grand):
    if grand:
        marquee(s, 3.15, DG, ZG0, ZG1)
        doors(s, PR, zt0=ZG0)
        stairs(s)
        for sx in (-1, 1):
            pier(s, sx * PX, 7.2, True)
            x0, x1 = sorted((sx * (PX - 0.27), sx * (PX + 0.27)))
            banner(s, x0, x1, -1.0, 7.2 - 0.7, 7.2 - 1.4)  # on the shaft, inside the plinth footprint
        window(s, True, ZG0, ZG1)
        blades(s)
        globe(s, GC, GR, ZG1, pal.Nickel_Satin)  # brief 103: the ball in the window's arch, its columns on the marquee roof
        return
    marquee(s, 3.0, 3.0)          # = office-boarding-pavilion (x 75.5..78.5, y -33..-27, soffit 4.05, top 4.62), carried on the face
    doors(s, 1.45, 0.0)           # flush: sill at the pavement
    for sx in (-1, 1):
        pilaster(s, sx * 2.35, 5.4)
        x0, x1 = sorted((sx * 2.1, sx * 2.6))
        banner(s, x0, x1, -0.255, 3.8, 2.8, kr=0.04)  # on the pilaster shaft: rod and knobs stay inside 0.3 m of the face


@asset("RecordsEntrance", "Plaza", bevel=0.0)
def records_entrance(s):
    build(s, True)


@asset("RecordsEntrance_Official", "Plaza", bevel=0.0)
def records_entrance_official(s):
    build(s, False)
