"""Brief 075 golden view (Sky: "still nothing like old in terms of color, style, richness"): the tram stop's two pieces the kit lacked. Today the world draws both as
procedural stand-ins at every stop and station (kiosk-tickets 7, timetable-column 20). Pivot = footprint centre at z = 0, front -Y (Unity +Z), materials from the kit's own
rows only (no new material, no new shader variant): ivory enamel, the coral transit accent (DK_Accent_Coral, the plan's one item allowed coral_transit), chrome, the
kiosk glass, the lit dial and the amber ticket screen. Chrome, never brass, at street level.

  TicketKiosk      the plan's kiosk-tickets box 2.0 x 1.5 x 2.7 m (catalogue.kiosk-tickets; trade tram-kiosk / skyrail-kiosk): a streamline capsule booth, coral skirt,
                   chrome sill, wraparound glazing on the front half, ticket window with a speaking grille and an amber fare screen, coral fascia band, a transit disc
                   (the tram's plan pictogram on coral) over the window, overhanging rounded roof, chrome sphere finial.
  TimetableColumn  the plan's timetable column 0.4 x 0.4 x 2.6 m with a 0.8 m coral dial at 2.0 (a clock face marking the next arrival, no text): the dial is double-sided
                   across X, so the piece is 0.86 m along X and 0.4 deep; the next-arrival hand on each face is the part `Hand` (it turns about the dial axis, local Y
                   through (0, 0, 2.0)), live 3D for Pulse's timetable."""

import math

from geo import circle, dome, rrect, stadium
from kit import asset
from mats import pal
from structures import sec, tube
from furnish_parts import FRONT, flat
import booths  # noqa: F401  (registers DK_Screen_Amber)
import furnish_civic  # noqa: F401  (registers DK_Dial)

# ----------------------------------------------------------------- ticket kiosk
KW, KD = 1.86, 1.36          # body capsule (the roof overhangs to the plan's 2.0 x 1.5)
Z_SK, Z_SILL, Z_HEAD, Z_FAS, Z_ROOF = 0.9, 1.0, 2.02, 2.3, 2.42


def _st(w, d, n=12):
    """geo.stadium without the zero-length straights a capsule's rrect leaves (duplicate points)."""
    out = []
    for p in stadium(w, d, n):
        if not out or math.dist(p, out[-1]) > 1e-6:
            out.append(p)
    return out[:-1] if math.dist(out[0], out[-1]) <= 1e-6 else out


def _ring(w, d, z, n=12):
    return [(x, y, z) for x, y in _st(w, d, n)]


def _front(w, d, z, n=12):
    """The front half (y <= 0) of a stadium loop, left to right, as an open 3D path."""
    pts = [(x, y) for x, y in _st(w, d, n) if y <= 1e-6]
    pts.sort(key=lambda p: p[0])
    return [(x, y, z) for x, y in pts]


def _back(w, d, z, n=12):
    pts = [(x, y) for x, y in _st(w, d, n) if y >= -1e-6]
    pts.sort(key=lambda p: -p[0])
    return [(x, y, z) for x, y in pts]


@asset("TicketKiosk", "Structures", bevel=0.006)
def ticket_kiosk(s):
    # plinth and coral skirt
    s.extrude(_st(KW + 0.06, KD + 0.06), 0.0, 0.1, pal.Chrome)
    s.extrude(_st(KW, KD), 0.1, Z_SK, pal.Accent_Coral)
    tube(s, _ring(KW, KD, 0.0), sec(-0.02, 0.04, 0.16, 0.2, 0.01), pal.Chrome, closed=True)  # kick rail
    # sill band (rounded, 6 cm proud) and the chrome sill rail
    tube(s, _ring(KW, KD, 0.0), sec(-0.07, 0.1, Z_SK, Z_SILL, 0.035), pal.Enamel_Ivory, closed=True)
    # glazed front half: interior (teal) behind a glass wrap; solid ivory back half
    s.extrude(_st(KW - 0.24, KD - 0.24), Z_SILL, Z_HEAD, pal.Enamel_Teal)
    tube(s, _front(KW - 0.02, KD - 0.02, 0.0), sec(0.0, 0.02, Z_SILL, Z_HEAD), pal.Glass)
    tube(s, _back(KW, KD, 0.0), sec(0.0, 0.12, Z_SILL, Z_HEAD), pal.Enamel_Ivory)
    for z in (Z_SILL + 0.02, Z_HEAD - 0.02):
        s.sweep(_front(KW + 0.01, KD + 0.01, z), 0.022, pal.Chrome, seg=8)
    for p in _front(KW + 0.005, KD + 0.005, 0.0)[1:-1:3]:  # chrome mullions round the wrap
        s.sweep([(p[0], p[1], Z_SILL + 0.02), (p[0], p[1], Z_HEAD - 0.02)], 0.018, pal.Chrome, seg=6)
    # ticket window: tray, speaking grille, amber fare screen
    yf = -KD / 2
    s.rbox((-0.26, yf - 0.07, Z_SILL - 0.01), (0.26, yf + 0.02, Z_SILL + 0.03), 0.012, pal.Chrome)
    with s.at((0.0, yf - 0.012, 1.42), FRONT):
        s.lathe([(0.0, 0.0), (0.085, 0.0), (0.095, 0.01), (0.08, 0.018), (0.0, 0.02)], pal.Chrome, seg=20)
        for k in range(3):
            s.lathe([(0.02 + 0.022 * k, 0.021), (0.03 + 0.022 * k, 0.021)], pal.Steel, seg=20, cap_bottom=False, cap_top=False)
    s.rbox((-0.2, yf - 0.03, 1.66), (0.2, yf + 0.01, 1.84), 0.02, pal.Chrome)
    s.rbox((-0.17, yf - 0.04, 1.69), (0.17, yf - 0.02, 1.81), 0.01, pal.Screen_Amber)
    # fascia with its coral band, then the overhanging roof and its chrome edge
    s.extrude(_st(KW, KD), Z_HEAD, Z_FAS, pal.Enamel_Ivory)
    tube(s, _ring(KW, KD, 0.0), sec(-0.03, 0.05, 2.1, 2.22, 0.02), pal.Accent_Coral, closed=True)
    tube(s, _ring(KW, KD, 0.0), sec(-0.07, 0.3, Z_FAS, Z_ROOF, 0.05), pal.Enamel_Ivory, closed=True)
    s.extrude(_st(KW - 0.5, KD - 0.5), Z_FAS, Z_ROOF - 0.01, pal.Enamel_Ivory)
    s.sweep(_ring(KW + 0.11, KD + 0.11, Z_ROOF - 0.06) + [_ring(KW + 0.11, KD + 0.11, Z_ROOF - 0.06)[0]], 0.016, pal.Chrome, seg=8, caps=False)
    s.lathe(dome(0.42, Z_ROOF - 0.02, 0.06, 4), pal.Enamel_Ivory, seg=24, cap_bottom=False)  # roof crown
    # transit disc over the window: chrome ring, coral field, the tram's plan pictogram in ivory
    with s.at((0.0, yf - 0.02, 2.16), FRONT):
        s.lathe([(0.0, -0.03), (0.2, -0.03), (0.215, -0.01), (0.215, 0.02), (0.2, 0.03), (0.0, 0.03)], pal.Chrome, seg=28)
        s.extrude(circle(0.18, 28), 0.03, 0.036, pal.Accent_Coral)
        flat(s, rrect(0.2, 0.1, 0.05, 4, 0.0, 0.01), 0.036, 0.042, pal.Enamel_Ivory)
        for x in (-0.05, 0.05):
            flat(s, rrect(0.06, 0.035, 0.012, 2, x, 0.02), 0.042, 0.046, pal.Accent_Coral)
        flat(s, rrect(0.16, 0.016, 0.006, 2, 0.0, -0.065), 0.036, 0.042, pal.Enamel_Ivory)
    # chrome sphere finial (the civic block's chrome spheres)
    z = Z_ROOF + 0.04
    s.lathe([(0.05, z - 0.01), (0.045, z + 0.02), (0.018, z + 0.05), (0.014, z + 0.13)], pal.Chrome, seg=12, cap_bottom=False)
    s.lathe([(0.0, z + 0.12)] + [(0.06 * math.sin(math.pi * k / 10), z + 0.18 - 0.06 * math.cos(math.pi * k / 10)) for k in range(1, 10)] + [(0.0, z + 0.24)],
            pal.Chrome, seg=16)


# ----------------------------------------------------------------- timetable column
ZC, RD = 2.0, 0.4  # dial centre and radius (the plan's 0.8 m dial at 2.0)


def _dial_face(s, sgn):
    """One face on local XY (+Z out) of the dial frame: lit ivory dial, 12 chrome studs, a coral quarter band round the rim (the run's hour)."""
    s.face([(x, y, 0.0) for x, y in circle(RD - 0.05, 32)], pal.Dial)
    for h in range(12):
        a = math.radians(90 - 30 * h)
        r0, r1, w = (RD - 0.14, RD - 0.08, 0.016) if h % 3 else (RD - 0.17, RD - 0.08, 0.024)
        c, sn = math.cos(a), math.sin(a)
        p = [(r * c - k * w * sn, r * sn + k * w * c) for r, k in ((r0, -1), (r1, -1), (r1, 1), (r0, 1))]
        flat(s, p, 0.0, 0.006, pal.Chrome)
    s.lathe(dome(0.035, 0.0, 0.02, 3), pal.Chrome, seg=12, cap_bottom=False)


@asset("TimetableColumn", "Furnish", bevel=0.005)
def timetable_column(s):
    # chrome foot, ivory shaft with coral collars
    s.lathe([(0.0, 0.0), (0.2, 0.0), (0.2, 0.04), (0.17, 0.07), (0.12, 0.1), (0.0, 0.1)], pal.Chrome, seg=24)
    s.lathe([(0.1, 0.1), (0.075, 0.3), (0.06, 1.0), (0.055, ZC - RD + 0.02)], pal.Enamel_Ivory, seg=20, cap_bottom=False, cap_top=False)
    for z0, z1 in ((0.32, 0.4), (1.1, 1.16)):
        s.lathe([(0.0, z0), (0.08, z0), (0.085, (z0 + z1) / 2), (0.08, z1), (0.0, z1)], pal.Accent_Coral, seg=20)
    # the dial drum across X (faces -Y and +Y): coral rim between chrome bezels
    with s.at((0, 0, ZC), FRONT):
        s.lathe([(RD - 0.02, -0.06), (RD, -0.045), (RD, 0.045), (RD - 0.02, 0.06)], pal.Accent_Coral, seg=40, cap_bottom=False, cap_top=False)
        for rot in ((0, 0, 0), (0, 180, 0)):
            with s.at((0, 0, 0), rot):
                s.lathe([(RD - 0.02, 0.06), (RD - 0.005, 0.07), (RD - 0.05, 0.08)], pal.Chrome, seg=40, cap_bottom=False, cap_top=False)
                s.lathe([(RD - 0.05, 0.065), (RD - 0.05, 0.08)], pal.Chrome, seg=40, cap_bottom=False, cap_top=False)
                with s.at((0, 0, 0.066)):
                    _dial_face(s, 1)
    # the next-arrival hand on both faces: the part the engine turns about local Y through (0, 0, ZC)
    with s.part("Hand") as hp:
        for sg in (-1, 1):
            with hp.at((0, sg * 0.09, ZC), FRONT if sg < 0 else (90, 0, 180)):
                flat(hp, [(-0.012, -0.05), (0.012, -0.05), (0.02, RD - 0.16), (0.0, RD - 0.1), (-0.02, RD - 0.16)], 0.0, 0.008, pal.Accent_Coral)
    # crown: a coral cap and chrome sphere over the dial
    z = ZC + RD + 0.02
    s.lathe([(0.0, z - 0.06), (0.05, z - 0.06), (0.04, z), (0.02, z + 0.06)], pal.Chrome, seg=16, cap_top=False)
    s.lathe([(0.0, z + 0.04)] + [(0.07 * math.sin(math.pi * k / 10), z + 0.11 - 0.07 * math.cos(math.pi * k / 10)) for k in range(1, 10)] + [(0.0, z + 0.18)],
            pal.Accent_Coral, seg=16)


# ----------------------------------------------------------------- overhead wires (brief 075 golden: the tram street's span wires, plan standards.streetscape.sections.tramStreet.wires)
# Five cheap pieces, two materials shared by all (DK_Iron the wire, DK_Chrome the fittings; the rosette disc and the pole skirt DK_Enamel_Teal). They cross the play
# camera's view, so the wire is a 6-sided strand at the plan's sizes (contact 12 mm, span 20 mm), no caps (the ends butt): Glint judges them at play zoom.
#   TramWire_Contact  the contact wire, 1.0 m along X, Ø 0.012, centreline on the pivot (x -0.5..0.5, z 0): stretched along X only to a run's length (scale [L, 1, 1]).
#   TramWire_Span     the span wire, 1.0 m along X, Ø 0.020, the same frame: stretched along X to the span (anchor eye to anchor eye).
#   TramWire_Hanger   the drop from the span (7.0) to the contact wire (5.8): pivot on the contact wire centreline, top 1.2 on the span wire centreline; an ivory
#                     bobbin insulator and a chrome ear on the wire.
#   TramWire_Rosette  the wall rosette: a 0.6 m teal deco disc with a chrome boss; pivot on the facade at the span height (the disc's centre), the boss out along
#                     -Y (the facade's normal, Unity +Z); the span wire's end (the eye) at (0, -0.17, 0).
#   TramWire_Pole     the slim side pole: chrome, Ø 0.3 shaft on a teal fluted skirt, 8.5 m to the ball top; the span eye is a collar all round at 7.0 (yaw free).
WIRE_C, WIRE_S, HANG = 0.006, 0.010, 1.2


def _strand(s, a, b, r, up):
    tube(s, [a, b], [(r * math.cos(t), r * math.sin(t)) for t in (2 * math.pi * k / 6 for k in range(6))], pal.Iron, up=up, caps=False)


@asset("TramWire_Contact", "Structures", bevel=0.0)
def tram_wire_contact(s):
    _strand(s, (-0.5, 0.0, 0.0), (0.5, 0.0, 0.0), WIRE_C, (0, 0, 1))


@asset("TramWire_Span", "Structures", bevel=0.0)
def tram_wire_span(s):
    _strand(s, (-0.5, 0.0, 0.0), (0.5, 0.0, 0.0), WIRE_S, (0, 0, 1))


@asset("TramWire_Hanger", "Structures", bevel=0.0)
def tram_wire_hanger(s):
    _strand(s, (0.0, 0.0, 0.012), (0.0, 0.0, 0.95), 0.005, (1, 0, 0))
    _strand(s, (0.0, 0.0, 1.1), (0.0, 0.0, HANG), 0.005, (1, 0, 0))
    s.lathe([(0.0, 0.95), (0.03, 0.97), (0.03, 1.08), (0.0, 1.1)], pal.Enamel_Ivory, seg=6)   # bobbin insulator
    s.box((-0.04, -0.012, -0.012), (0.04, 0.012, 0.016), pal.Chrome)                          # ear clamp on the contact wire


@asset("TramWire_Rosette", "Structures", bevel=0.0)
def tram_wire_rosette(s):
    with s.at((0, 0, 0), FRONT):  # lathe axis -> world -Y, out of the wall
        s.lathe([(0.30, 0.0), (0.30, 0.03), (0.26, 0.055), (0.0, 0.055)], pal.Enamel_Teal, seg=12, cap_bottom=False)
        s.lathe([(0.10, 0.055), (0.10, 0.10), (0.07, 0.15), (0.0, 0.17)], pal.Chrome, seg=12, cap_bottom=False)


@asset("TramWire_Pole", "Structures", bevel=0.0)
def tram_wire_pole(s):
    s.lathe([(0.0, 0.0), (0.26, 0.0), (0.26, 0.1), (0.21, 0.3), (0.17, 1.0)], pal.Enamel_Teal, seg=8, cap_top=False)
    s.lathe([(0.17, 1.0), (0.15, 1.04), (0.10, 8.24)], pal.Chrome, seg=8, cap_bottom=False, cap_top=False)
    s.lathe([(0.10, 6.9), (0.16, 6.96), (0.16, 7.04), (0.10, 7.1)], pal.Chrome, seg=8, cap_bottom=False, cap_top=False)   # span eye collar
    s.lathe([(0.0, 8.22)] + [(0.14 * math.sin(math.pi * k / 6), 8.36 - 0.14 * math.cos(math.pi * k / 6)) for k in range(1, 6)] + [(0.0, 8.5)], pal.Chrome, seg=8)
