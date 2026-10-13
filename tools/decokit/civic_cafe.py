"""CafeBay (brief 008 B1): the corner café's ground storey as in the 26 Sep paintings (gameplay-plan, inez-cutaway): a curved
dark-teal glazed bay in thin brass frames, a banded canopy wrapping the curve (seafoam fascia, gold line, cream soffit),
brass doors, cream piers, a planted terrace wall in front, and a blade sign standing above the canopy.

Frame: pivot = the centre of the café's flat front on the ring line (block x -10.7, y -29), z = 0 pavement top; front -Y
(block +x, south); the building is on +Y. Fitted to civic-block.json's café: an 11.4 m flat between two R 5.3 arcs (22 m
wide, rule C-0 unchanged). The piece is dressing: it covers the ground storey of the front (z 0 to 4.7) plus the canopy
(2.45 m out) and the terrace wall (6.4 m out, brief 008 B2; its gate is 5.2 m between the run ends); the upper storeys and the
side walls are the generator's."""

import math

from furnish_parts import GLYPH, letters
from geo import fillet
from kit import asset
from mats import pal
from plaza_parts import lift, offset, ribbon
from structures import along, ccw, sec, tube
from civic_common import HF, band, front_path, hair, planting, split_runs, trough

GLYPH.setdefault("F", (0.5, [[(0, 0), (0, 1), (0.5, 1)], [(0, 0.55), (0.38, 0.55)]]))
GLYPH.setdefault("E", (0.5, [[(0.5, 1), (0, 1), (0, 0), (0.5, 0)], [(0, 0.52), (0.38, 0.52)]]))

Z_G0, Z_G1 = 0.5, 4.0          # glazing sill and head
Z_SOF, Z_TOP = 4.22, 4.68      # canopy soffit, fascia top
DOOR_X, DOOR_H = 1.3, 2.55     # half opening between the jamb piers, door leaf height
GLASS_A = -0.14                # glass film 14 cm proud of the ring line (the generator's wall stays behind it)
CANOPY = 2.45                  # canopy projection past the ring (Vantage: dressing, L3)
WALL_A = -6.4                  # planter wall line (terrace edge), outward: 0.5 m beyond the B1 line, off the Oct 9 waitress loop (Vantage, B2)
GATE = 2.602                   # half gate: the runs end here, so the gate is 5.204 m between the end faces: at least Vantage's 5.2 m cafe-gate-spine (the tube end faces are skewed 1 mm by the arc's averaged tangent)


def door_leaf(s, x0, x1, y):
    w = x1 - x0
    s.face([(x0 + 0.05, y, 0.3), (x1 - 0.05, y, 0.3), (x1 - 0.05, y, DOOR_H - 0.05), (x0 + 0.05, y, DOOR_H - 0.05)], pal.GlassDark)
    for xa, xb in ((x0, x0 + 0.06), (x1 - 0.06, x1)):
        s.box((xa, y - 0.05, 0.0), (xb, y + 0.03, DOOR_H), pal.Accent_Gold)
    for za, zb in ((DOOR_H - 0.06, DOOR_H), (0.28, 0.34)):
        s.box((x0, y - 0.05, za), (x1, y + 0.03, zb), pal.Accent_Gold)
    s.box((x0 + 0.06, y - 0.055, 0.0), (x1 - 0.06, y + 0.03, 0.28), pal.Accent_Gold)  # kick plate
    hx = x1 - 0.15 if x0 < 0 else x0 + 0.15
    s.sweep([(hx, y - 0.1, 0.95), (hx, y - 0.1, 1.5)], 0.02, pal.Chrome, seg=5)
    for z in (0.95, 1.5):
        s.sweep([(hx, y - 0.03, z), (hx, y - 0.1, z)], 0.015, pal.Chrome, seg=4, caps=False)


def stacked_sign(s, word, h_letter, x, ymid, z0, body, ink, edge):
    """Vertical blade in the YZ plane at x, mounted to the wall (y 0) and reaching out to y = -1.15: a swept enamel
    fin with the word stacked on both faces and a chrome edge bead."""
    Y0, Y1, T = -0.05, -1.15, 0.1
    n = len(word)
    top = z0 + n * h_letter * 1.25 + 0.5
    out = fillet([(Y0, z0 + 0.3), (Y1, z0), (Y1, top - 0.5), (Y0, top)], 0.18, n=3)
    with s.at((x, 0, 0), (90, 0, 90)):
        s.extrude(ccw(out), -T, T, body)
    s.sweep([(x, y, z) for y, z in out + out[:1]], 0.03, edge, seg=4, caps=False)
    zc = z0 + 0.45 + h_letter * 0.5
    for sx, rot in ((1, (90, 0, 90)), (-1, (90, 0, -90))):
        for i, ch in enumerate(word):
            with s.at((x + sx * T, ymid, top - 0.62 - i * h_letter * 1.25 - h_letter * 0.5), rot):
                letters(s, ch, h_letter, ink, x=0.0, y=0.0, lift=0.0, wt=0.17, depth=0.03, step=2)


@asset("CafeBay", "Plaza", bevel=0.0)
def cafe_bay(s):
    P = front_path(6, 4)
    out_canopy = offset(P, -CANOPY)
    # base: deep teal faience band with a limestone coping, broken at the doors
    for run in split_runs(P, lambda x, y: abs(x) > DOOR_X - 0.05):
        band(s, run, -0.3, 0.15, 0.0, 0.45, pal.Enamel_Teal)
        band(s, run, -0.34, 0.15, 0.4, 0.46, pal.Limestone)
    # glazing: dark glass film over a dark back sheet, thin brass mullions every 1.45 m, brass sill / transom / head
    ribbon(s, offset(P, GLASS_A), Z_G0, Z_G1, pal.GlassDark)
    ribbon(s, offset(P, GLASS_A + 0.04), Z_G0, Z_G1, pal.Enamel_Charcoal)
    for p, t in along(P, 1.45):
        with s.at((p.x, p.y, 0), (0, 0, math.degrees(math.atan2(t.y, t.x)))):
            s.box((-0.035, GLASS_A - 0.09, Z_G0), (0.035, GLASS_A + 0.02, Z_G1), pal.Accent_Gold)
    for z in (2.55, Z_G1 - 0.03):
        hair(s, P, GLASS_A - 0.06, z, 0.05, pal.Accent_Gold)
    for run in split_runs(P, lambda x, y: abs(x) > DOOR_X - 0.05):
        hair(s, run, GLASS_A - 0.06, Z_G0 + 0.03, 0.05, pal.Accent_Gold)
    # cream piers: where the flat meets each arc and at the door jambs
    for x in (-HF, HF, -DOOR_X - 0.3, DOOR_X + 0.3):
        s.rbox((x - 0.28, -0.4, 0.0), (x + 0.28, 0.16, Z_G1 + 0.2), 0.04, pal.Limestone)
        s.box((x - 0.04, -0.43, 0.46), (x + 0.04, -0.4, Z_G1), pal.Accent_Gold)
    # doors: two brass-framed leaves in the opening, flush entrance (no steps: the walk height is unchanged)
    s.box((-DOOR_X, GLASS_A - 0.1, DOOR_H), (DOOR_X, GLASS_A + 0.04, Z_G1), pal.Enamel_Charcoal)  # transom back
    door_leaf(s, -DOOR_X, 0.0, GLASS_A - 0.02)
    door_leaf(s, 0.0, DOOR_X, GLASS_A - 0.02)
    # canopy: cream slab, seafoam fascia with gold lines, soffit downlights
    poly = out_canopy + P[::-1]
    s.extrude(poly, Z_SOF, Z_SOF + 0.14, pal.Enamel_Ivory)
    tube(s, lift(out_canopy), sec(-0.06, 0.1, Z_SOF - 0.02, Z_TOP), pal.Enamel_Seafoam)
    hair(s, out_canopy, -0.08, Z_SOF + 0.03, 0.035, pal.Accent_Gold)
    hair(s, out_canopy, -0.08, Z_TOP - 0.06, 0.035, pal.Accent_Gold)
    for p, _t in along(offset(P, -1.3), 2.4):
        with s.at((p.x, p.y, Z_SOF)):
            s.lathe([(0.0, -0.025), (0.11, -0.012), (0.11, 0.0)], pal.Lamp_Deco, seg=8, cap_top=False)
    # sign above the canopy: a blade on the wall at x = 3.6 standing on the canopy slab (clear of it, not through it), 1.15 m out
    s.rbox((3.6 - 0.2, -1.2, Z_SOF + 0.14), (3.6 + 0.2, 0.0, Z_SOF + 0.26), 0.02, pal.Chrome, seg=1)
    stacked_sign(s, "CAFE", 0.52, 3.6, -0.6, Z_SOF + 0.3, pal.Enamel_Teal, pal.Enamel_Ivory, pal.Chrome)  # Enamel_Cream is dE 20.5 from the guard colour: Ivory has 27
    # terrace wall: low limestone troughs planted with coral and cream (variant A), broken at the door axis and the arc ends
    PW_ = front_path(6, 4, flat_x=(-HF, -GATE, 0.0, GATE, HF))  # the same outline with vertices at the gate edges and one in the gate (x 0 fails the keep test: that is the break)
    for k, run in enumerate(split_runs(offset(PW_, WALL_A), lambda x, y: abs(x) >= GATE - 1e-6 and y < 3.0)):
        trough(s, run, z_top=0.62, w=0.6)
        planting(s, run, "AB"[k % 2], step=0.8, blooms=1, ends=0.32)  # foliage starts 0.32 m in: nothing leans past a run end into the gate
