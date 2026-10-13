"""RecordsEntrance (brief 008 B1): the Records Office public front as in the 26 Sep paintings (records-office, gameplay-plan,
inez-cutaway, mission-block): a tall arched window with a chrome globe on a stand over a seafoam marquee with two lanterns,
brass double doors under it, three chrome-and-gold blades rising from the arch head, cream flank piers with chrome ball
finials and comet banners, and a flight of five treads with chrome rails and cheek walls.

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

from geo import arc, fillet
from kit import asset
from mats import pal
from plaza import canopy_plan
from plaza_parts import land, lift, uvface
from structures import arch, arch_band, ccw, sec, soft_extrude, tube, xz
from civic_common import hair

FRONT, SIDE_R, SIDE_L = (90, 0, 0), (90, 0, 90), (90, 0, -90)
ZL, NR, RISE, GO, LAND = 0.75, 6, 0.125, 0.36, 1.5   # landing height, risers, rise, going, landing depth
SW = 2.3                                              # half width of the flight
PR, SU, PS = 1.65, 2.2, 7.6                           # window half width, surround outer half width, arch spring
ZC0, ZC1 = 4.05, 4.62                                 # marquee soffit and fascia top
EDGE = 0.1375                                         # the fascia sections reach 0.11 and the gold bead 0.14 past the plan line: the plan is drawn that far inside the envelope
PX, PW, PD0, PD1 = 3.5, 0.48, -1.03, 0.23             # public pier: centre, plinth half width, plinth front and back (y)
GR = 1.2                                              # globe radius (centre 7.0 m up, 0.9 m out of the face)


def sphere(s, c, r, mat, seg=12, n=6):
    prof = [(r * math.sin(math.pi * k / n), -r * math.cos(math.pi * k / n)) for k in range(n + 1)]
    with s.at(c):
        s.lathe(prof, mat, seg=seg)


def banner(s, x0, x1, y, zt, length, tip=0.5, kr=0.055):
    """plaza_parts.banner at a quarter of its triangles: comet fabric slab, comet art both faces, gold rod, small knobs."""
    xm, zb, zv = (x0 + x1) / 2, zt - length, zt - length + tip
    zs = zt - (x1 - x0) * 1.5 * 0.7 * 1.3
    with s.at((0, y, 0), (90, 0, 0)):
        s.extrude([(x0, zv), (xm, zb), (x1, zv), (x1, zt), (x0, zt)], -0.012, 0.012, pal.Fabric_Banner)

    def v(z):
        return 0.3 + 0.7 * (z - zs) / (zt - zs) if z >= zs else 0.04 + 0.26 * (z - zb) / (zs - zb)
    for side, yy in ((1, y - 0.016), (-1, y + 0.016)):
        def u(x):
            return (x - x0) / (x1 - x0) if side > 0 else (x1 - x) / (x1 - x0)
        for poly in ([(x0, zs), (x1, zs), (x1, zt), (x0, zt)], [(x0, zv), (xm, zb), (x1, zv), (x1, zs), (x0, zs)]):
            pts = poly if side > 0 else poly[::-1]
            uvface(s, [(x, yy, z) for x, z in pts], pal.Banner_Comet, [(u(x), v(z)) for x, z in pts])
    s.sweep([(x0 - 0.08, y, zt + 0.05), (x1 + 0.08, y, zt + 0.05)], 0.028, pal.Accent_Gold, seg=5, caps=False)
    for x in (x0 - 0.1, x1 + 0.1):
        sphere(s, (x, y, zt + 0.05), kr, pal.Accent_Gold, seg=8, n=4)


def globe(s, c, r, nlon=40, nlat=20, lon0=-15):
    """Relief globe at a third of plaza_parts.globe's triangles: chrome seas, satin-steel continents, no graticule sweeps
    (an equator and a meridian ring instead), tilted 23.5 deg on a chrome stand with a base disc and two struts."""
    h = 0.03

    def pt(lon, lat, rr):
        cl = math.cos(math.radians(lat))
        lo = math.radians(lon)
        return rr * cl * math.sin(lo), -rr * cl * math.cos(lo), rr * math.sin(math.radians(lat))

    def rad(lon, lat):
        return r + h * min(1.0, max(0.0, (land(lon + lon0, lat) + 0.08) / 0.16))
    verts, faces, cells = [pt(0, -90, r)], [], []
    for j in range(1, nlat):
        lat = -90 + 180 * j / nlat
        verts += [pt(360 * i / nlon, lat, rad(360 * i / nlon, lat)) for i in range(nlon)]
    verts.append(pt(0, 90, r))
    top = len(verts) - 1
    for j in range(nlat):
        for i in range(nlon):
            i1 = (i + 1) % nlon
            a, b = 1 + (j - 1) * nlon, 1 + j * nlon
            faces.append([0, b + i1, b + i] if j == 0 else [a + i, a + i1, top] if j == nlat - 1 else [a + i, a + i1, b + i1, b + i])
            cells.append(land(360 * (i + 0.5) / nlon + lon0, -90 + 180 * (j + 0.5) / nlat) > 0)
    with s.at(c, (0, 23.5, 0)):
        bv = s._add(verts, faces, pal.Chrome, True)
        steel = s._mi(pal.Steel)
        lookup = {frozenset(bv[k] for k in fc): cl for fc, cl in zip(faces, cells)}
        for f in {f for v in bv for f in v.link_faces}:
            if lookup.get(frozenset(f.verts)):
                f.material_index = steel
        for z0, z1 in ((r - 0.02, r + 0.16), (-r - 0.16, -r + 0.02)):
            s.lathe([(0, z0), (0.05, z0), (0.05, z1), (0, z1)], pal.Chrome, seg=8)
    with s.at(c, (90, 0, 0)):  # meridian ring in the plane of the face
        s.lathe([(r + 0.14, -0.06), (r + 0.23, -0.06), (r + 0.23, 0.06), (r + 0.14, 0.06), (r + 0.14, -0.06)], pal.Chrome, seg=48, cap_bottom=False, cap_top=False)
    # stand: base disc on the marquee roof, post to the globe, two struts
    zb = ZC1 + 0.28
    s.rbox((-0.95, c[1] - 1.15, ZC1 - 0.03), (0.95, c[1] + 0.4, zb), 0.06, pal.Limestone, seg=1)
    with s.at((c[0], c[1], zb)):
        s.lathe([(0, 0), (0.7, 0), (0.7, 0.06), (0.55, 0.13), (0.28, 0.2), (0.14, 0.28), (0, 0.3)], pal.Chrome, seg=24)
    s.sweep([(c[0], c[1], zb + 0.2), (c[0], c[1], c[2] - r - 0.15)], 0.09, pal.Chrome, seg=10)
    for ang, sx in ((236, -1), (304, 1)):
        end = (c[0] + (r + 0.15) * math.cos(math.radians(ang)), c[1], c[2] + (r + 0.15) * math.sin(math.radians(ang)))
        s.sweep([(c[0] + 0.45 * sx, c[1], zb + 0.08), end], 0.05, pal.Chrome, seg=6)


def window(s, grand):
    """Tall arched window on the face above the marquee: teal and seafoam surround, dark glass, gold grid."""
    with s.at((0, 0, 0), FRONT):
        soft_extrude(s, arch_band(PR, PR + 0.32, PS, ZC0, n=14), 0.0, 0.16, pal.Enamel_Teal, 0.05)
        soft_extrude(s, arch_band(PR + 0.32, SU, PS, ZC0, n=14), 0.0, 0.07, pal.Enamel_Seafoam, 0.03)
    head = arch(2 * PR, PS, ZC0, n=14)
    s.face([(x, -0.03, z) for x, z in head], pal.Enamel_Charcoal)
    s.face([(x, -0.07, z) for x, z in head], pal.GlassDark)
    s.sweep(xz(arc(0, PS, PR - 0.04, 0, 180, 14), -0.1), 0.035, pal.Accent_Gold, seg=4)
    for x in (-0.6, 0.6):
        s.box((x - 0.04, -0.14, ZC1 + 0.2), (x + 0.04, -0.07, PS + math.sqrt(PR * PR - x * x) - 0.05), pal.Accent_Gold)
    for z in (ZC1 + 0.2, PS):
        s.box((-PR, -0.14, z - 0.035), (PR, -0.07, z + 0.035), pal.Accent_Gold)


def blades(s):
    """Three steel blades with gold spines rising from the arch head (the crown fins of the painting)."""
    for x, w, z0, z1 in ((0.0, 0.26, PS + PR + 0.3, 13.6), (-0.8, 0.18, PS + PR + 0.15, 12.2), (0.8, 0.18, PS + PR + 0.15, 12.2)):
        h = z1 - z0
        lens = [(0, z0), (w, z0 + 0.25 * h), (w * 0.75, z0 + 0.75 * h), (0, z1), (-w * 0.75, z0 + 0.75 * h), (-w, z0 + 0.25 * h)]
        with s.at((x, 0, 0), FRONT):
            s.extrude(lens, 0.04, 0.2, pal.Steel)  # brushed steel: a mirror chrome slab vanishes against the cream tower face and the sky
        s.box((x - 0.012, -0.205, z0 + 0.3 * h), (x + 0.012, -0.195, z1 - 0.1), pal.Accent_Gold)


def doors(s, wide, zl=ZL):
    """Brass double doors with side lights and a transom: dark glass in gold frames, chrome pulls, kick plates. zl = sill height."""
    ZL = zl  # noqa: N806 (local: the public front stands on its landing at 0.75, the official door is flush at 0)
    y, z1, hd = -0.12, ZC0, 2.5
    zt = ZL + hd
    s.face([(-wide, y, ZL), (wide, y, ZL), (wide, y, z1), (-wide, y, z1)], pal.GlassDark)
    s.face([(-wide, y + 0.09, ZL), (wide, y + 0.09, ZL), (wide, y + 0.09, z1), (-wide, y + 0.09, z1)], pal.Enamel_Charcoal)
    for x in (-wide, -1.1, 0.0, 1.1, wide):
        s.box((x - 0.045, y - 0.06, ZL), (x + 0.045, y + 0.03, z1), pal.Accent_Gold)
    for z in (ZL, zt - 0.04, z1 - 0.06):
        s.box((-wide, y - 0.06, z), (wide, y + 0.03, z + 0.07), pal.Accent_Gold)
    s.box((-1.05, y - 0.07, ZL + 0.02), (1.05, y + 0.03, ZL + 0.3), pal.Accent_Gold)  # kick plates
    for x in (-0.16, 0.16):
        s.sweep([(x, y - 0.14, ZL + 1.0), (x, y - 0.14, ZL + 1.55)], 0.02, pal.Chrome, seg=5)
        for z in (ZL + 1.0, ZL + 1.55):
            s.sweep([(x, y - 0.03, z), (x, y - 0.14, z)], 0.014, pal.Chrome, seg=4, caps=False)


def marquee(s, hw, d):
    """hw, d = the envelope (half width along the face, projection): nothing of the canopy reaches past it."""
    path = canopy_plan(hw - EDGE, d - EDGE, 3.0, 16)
    s.extrude(path, ZC0, ZC0 + 0.1, pal.Enamel_Ivory)
    zf = ZC1 - 0.18
    tube(s, lift(path), sec(-0.07, 0.3, ZC0 + 0.02, zf, 0.0), pal.Enamel_Teal)
    tube(s, lift(path), sec(-0.11, 0.36, zf, ZC1, 0.0), pal.Enamel_Mint)
    s.extrude(path, zf - 0.07, ZC1 - 0.03, pal.Enamel_Seafoam)
    hair(s, path, -0.1, ZC0 + 0.05, 0.035, pal.Accent_Gold)
    hair(s, path, -0.12, zf, 0.035, pal.Accent_Gold)
    for x in (-hw * 0.5, hw * 0.5):
        with s.at((x, -d * 0.62, ZC0)):
            bulb = [(0.17 * math.cos(t), -0.03 - 0.12 * math.sin(t)) for t in (math.pi / 2 * k / 4 for k in range(1, 5))]
            s.lathe([(0.19, 0.0), (0.19, -0.03)] + bulb, pal.Lamp_Deco, seg=12)
    for x in (-hw + 0.2, hw - 0.2):  # chrome ball finials on the marquee corners
        with s.at((x, -0.6, ZC1 - 0.03)):
            s.lathe([(0, 0), (0.1, 0), (0.1, 0.05), (0.045, 0.1), (0.04, 0.55), (0.07, 0.6), (0, 0.62)], pal.Chrome, seg=10)
        sphere(s, (x, -0.6, ZC1 + 0.84), 0.2, pal.Chrome, seg=14, n=7)


def stairs(s):
    """Landing box, five treads, cheek walls with sloped coping, two chrome handrails on posts (gooseneck at both ends)."""
    s.box((-SW, -LAND, 0.0), (SW, 0.0, ZL), pal.Limestone)
    for k in range(1, NR):
        s.box((-SW, -(LAND + k * GO), 0.0), (SW, -(LAND + (k - 1) * GO), ZL - k * RISE), pal.Limestone)
    L1 = LAND + (NR - 1) * GO
    for sx in (-1, 1):
        x0, x1 = sorted((sx * SW, sx * (SW + 0.26)))
        poly = ccw([(0, 0), (-L1 - 0.1, 0), (-L1 - 0.1, 0.5), (-LAND, ZL + 0.5), (0, ZL + 0.5)])
        with s.at((x0, 0, 0), SIDE_R):
            s.extrude(poly, 0.0, x1 - x0, pal.Limestone)
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
    """Cream flank pier inside its plinth footprint (x +-0.48, y -1.03..0.23): seafoam band, teal cap, chrome ball on a stand."""
    s.rbox((x - 0.41, -0.96, 0.0), (x + 0.41, 0.16, h), 0.04, pal.Limestone, seg=1)
    s.box((x - PW, PD0, 0.0), (x + PW, PD1, 0.35), pal.Limestone)
    s.box((x - 0.45, PD0 + 0.03, h - 0.5), (x + 0.45, PD1 - 0.03, h - 0.28), pal.Enamel_Seafoam)
    s.box((x - PW, PD0, h), (x + PW, PD1, h + 0.28), pal.Enamel_Teal)
    hair(s, [(x - 0.45, PD0 + 0.015), (x + 0.45, PD0 + 0.015)], 0.0, h + 0.03, 0.03, pal.Accent_Gold)
    if ball:
        with s.at((x, -0.4, h + 0.28)):
            s.lathe([(0, 0), (0.3, 0), (0.3, 0.05), (0.15, 0.13), (0.1, 0.22), (0, 0.22)], pal.Chrome, seg=14)
        sphere(s, (x, -0.4, h + 0.28 + 0.22 + 0.42), 0.42, pal.Chrome, seg=18, n=9)


def pilaster(s, x, h):
    """Official front: a 0.3 m pilaster on the tower face (x +-0.35, y -0.30..+0.20): plinth, shaft, seafoam band, teal cap."""
    s.box((x - 0.35, -0.30, 0.0), (x + 0.35, 0.2, 0.3), pal.Limestone)
    s.box((x - 0.31, -0.26, 0.3), (x + 0.31, 0.2, h), pal.Limestone)
    s.box((x - 0.33, -0.29, h - 0.5), (x + 0.33, 0.2, h - 0.28), pal.Enamel_Seafoam)
    s.box((x - 0.35, -0.30, h), (x + 0.35, 0.2, h + 0.25), pal.Enamel_Teal)
    hair(s, [(x - 0.33, -0.285), (x + 0.33, -0.285)], 0.0, h + 0.03, 0.03, pal.Accent_Gold)


def build(s, grand):
    if grand:
        marquee(s, 3.15, 2.3)
        doors(s, PR)
        stairs(s)
        for sx in (-1, 1):
            pier(s, sx * PX, 7.2, True)
            x0, x1 = sorted((sx * (PX - 0.27), sx * (PX + 0.27)))
            banner(s, x0, x1, -1.0, 7.2 - 0.7, 7.2 - 1.4)  # on the shaft, inside the plinth footprint
        window(s, True)
        blades(s)
        globe(s, (0.0, -0.9, 7.0), GR)
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
