"""The testbed café's table sets, parasol and room, ported (brief 026, Vela; Sky 2026-10-20: "our testbed cafe parasols were better and the interior of the cafe").

Geometry is the testbed's own (art/cafe3d/parts.json, baked from the store's glb files by port_cafe3d.py; provenance and sha256 in art/cafe3d/PROVENANCE.json), replayed here with its UVs and
its materials. Nothing was redrawn. Textures: tex_cafe3d.py (the furniture atlas, to the ground set) and art/cafe/cafe_surface_tb_v1.png (the counter front, lit art).

  CafeTable_Testbed     the round café table (758 tris): satin top, brass pedestal, espresso cup and bud vase as one mesh; 1.25 m across, 1.04 m high with the vase. Pivot = foot centre.
  CafeChair_Testbed     the bistro chair (520): splayed legs, cushioned bow back, cloth with a sheen map in the testbed (dropped here: URP Lit has no sheen). Pivot = chair centre; faces +X before turning? as exported.
  Parasol_Testbed       the canopy and mast (3008 tris): a scalloped eight-panel canvas parasol 2.56 m across, 2.8 m high, canvas with a woven normal map. Pivot = the mast foot (the table centre).
  CafeLamp_Testbed_<Hue> Amber, Teal, Peach, Coral, Lilac: the atom-age table lamp, the testbed's brass body (600) with the diffuser lathed from its own profile at 16 sides (cafe-table-lamps.js), each diffuser
                        its own colour (#ffc066 #67d6ce #ffd099 #ffa891 #b79be4) in DK_Lamp_Table_<Hue>, night emissive 2.2 (0 by day). 760 tris each; Pivot = foot centre on the tabletop.
  CafeSet_Testbed       one table set as the testbed laid the second one out (cafe-multiview-world.js layouts[1]): table turned -0.48 rad, four chairs at 0.40, 1.82, 3.54 and 4.95 rad about 1 m out, the lamp
                        is placed apart (the clear corner of the top: (0.116, -0.320, 0.797) from the pivot), in the table's hue. Without the parasol (it is placed apart, as Parasol_Stripe is). Pivot = table centre.
  CafeInterior_Testbed  CafeInterior's own shell (floor, ceiling and cove, side walls with windows, back wall: C-0 unchanged) with the testbed's room contents in place of the generator's: counter
                        run with walnut cabinet and painted front, register and espresso gear (bakelite), back shelves, two facing petrol-velvet booths, a three-table lounge on the west with plants in pots,
                        eight opal pendants with chrome rods (rods extended to our 4.6 m ceiling), the amber cove and teal mood emitters; and two testbed table sets by the front glass. One rigid offset moves the
                        room from the testbed's frame (floor 0.99, back wall y 5.61) to ours (floor 0.04, back wall y 10.9): dy +5.29, dz -0.95, dx 0. Companion of CafeBay_v2 like CafeInterior (same pivot).
Emissive values are the testbed's (pendant opal 2.0, cove 1.5, mood 1.06, luminaires 3.0, lamp diffuser 2.2 on the tabletop lamp): Latch's night scalar takes them to 0 by day.
"""

import json
import math
from pathlib import Path

import cafe_grand as cg
import lamps026  # noqa: F401  (DK_Lamp_Table_*)
import mats
from kit import asset
from mats import TEXTURES, pal, register
from plaza_parts import lift, offset
from structures import ccw, sec, tube

DATA = json.loads((Path(__file__).parent / "art/cafe3d/parts.json").read_text())
FURN, ROOM, TBM = DATA["furniture"], DATA["room"], DATA["materials"]

# materials: the atlas is textured (ground set, explicit UVs); the rest are the glb's own base colours (the testbed's linear colours as sRGB hex)
register("DK_Cafe_TB_Atlas", "paving", "#FFFFFF")
TEXTURES.setdefault("DK_Cafe_TB_Atlas", ("ground_cafetb_v1.png", 1.0))
mats.GROUND.setdefault("DK_Cafe_TB_Atlas", dict(albedo="ground_cafetb_v1.png", normal="ground_cafetb_v1_normal.png", mask="ground_cafetb_v1_mask.png", tile=(1.0, 1.0), smoothness=1.0))
register("DK_Cafe_TB_Velvet", "ceramic", TBM["Cafe petrol velvet booths"]["hex"])
register("DK_Cafe_TB_Walnut", "ceramic", TBM["Cafe walnut cabinetry"]["hex"])
register("DK_Cafe_TB_Bakelite", "enamel", TBM["Cafe register black bakelite"]["hex"])
register("DK_Cafe_TB_Petrol", "enamel", TBM["Cafe petrol enamel"]["hex"])
register("DK_Cafe_TB_Foliage", "ceramic", TBM["Cafe interior glossy foliage"]["hex"])
register("DK_Cafe_Mood", "emit", "#56C9B8", 1.8)                       # Glint's teal mood point colour; the testbed's emitter is #30B595 at 1.06
register("DK_Cafe_TB_Counter", "emit", "#FFFFFF", 0.7)
TEXTURES.setdefault("DK_Cafe_TB_Counter", ("cafe_surface_tb_v1.png", 1.0))
ROOMMAT = {"Cafe shared live PBR.001": "Cafe_TB_Atlas", "Cafe petrol velvet booths": "Cafe_TB_Velvet", "Cafe walnut cabinetry": "Cafe_TB_Walnut", "Cafe register black bakelite": "Cafe_TB_Bakelite",
           "Cafe warm opal luminaires": "Cafe_Opal", "Cafe warm pendant emitter": "Cafe_Opal", "Cafe interior glossy foliage": "Cafe_TB_Foliage", "Cafe amber cove emitter": "Cafe_Cove",
           "Cafe teal mood emitter": "Cafe_Mood", "Cafe polished chrome": "Chrome", "Cafe petrol enamel": "Cafe_TB_Petrol", "Cafe pilot counter enamel": "Cafe_TB_Counter"}


def mesh(s, d, mat, smooth=True):
    """Replay a baked part (vertices, uv, triangles) with its UVs under the current frame."""
    before = set(s.bm.faces)
    bv = s._add([tuple(v) for v in d["v"]], d["t"], mat, smooth)
    uv = dict(zip(bv, (tuple(u) for u in d["uv"])))
    for f in set(s.bm.faces) - before:
        f[s._uvkeep] = 1
        s._pending_uv.append((f, [uv[lp.vert] for lp in f.loops]))


def atlas():
    return pal.Cafe_TB_Atlas


@asset("CafeTable_Testbed", "Plaza", bevel=0.0)
def table_tb(s):
    mesh(s, FURN["table"], atlas())


@asset("CafeChair_Testbed", "Plaza", bevel=0.0)
def chair_tb(s):
    mesh(s, FURN["chair"], atlas())


@asset("Parasol_Testbed", "Plaza", bevel=0.0)
def parasol_tb(s):
    mesh(s, FURN["parasol"], atlas())


HUES = ("Amber", "Teal", "Peach", "Coral", "Lilac")  # the testbed's five (cafe-table-lamps.js colours): #ffc066 #67d6ce #ffd099 #ffa891 #b79be4


def lamp(s, at=(0.0, 0.0, 0.0), hue="Amber"):
    with s.at(at):
        mesh(s, FURN["lamp"], atlas())
        prof = [(0, .144), (.048, .148), (.079, .174), (.083, .215), (.067, .263), (.026, .287), (0, .288)]  # cafe-table-lamps.js
        s.lathe(prof, getattr(pal, f"Lamp_Table_{hue}"), seg=16, cap_bottom=False, cap_top=False)


for _h in HUES:
    asset(f"CafeLamp_Testbed_{_h}", "Plaza", bevel=0.0)(lambda s, h=_h: lamp(s, hue=h))


TOP = 0.797  # the tabletop the lamp stands on (cafe-multiview-world.js)


def table_set(s, at=(0.0, 0.0, 0.0), heading=-0.48, lamp_xy=(0.25, -0.23), seats=((0.40, 0.96), (1.82, 0.99), (3.54, 1.02), (4.95, 0.91)), lamps=True, hue="Amber"):
    """layouts[1] of cafe-multiview-world.js: a table turned `heading`, chairs at (angle, distance) with their backs out, the lamp at lamp_xy in the table's frame."""
    ax, ay, az = at
    with s.at((ax, ay, az), (0, 0, math.degrees(heading))):
        mesh(s, FURN["table"], atlas())
    for a, d in seats:
        with s.at((ax + math.cos(a) * d, ay + math.sin(a) * d, az), (0, 0, math.degrees(a - math.pi / 2))):
            mesh(s, FURN["chair"], atlas())
    if lamps:
        c, sn = math.cos(heading), math.sin(heading)
        lamp(s, (ax + lamp_xy[0] * c - lamp_xy[1] * sn, ay + lamp_xy[0] * sn + lamp_xy[1] * c, az + TOP), hue)


@asset("CafeSet_Testbed", "Plaza", bevel=0.0)
def set_tb(s):
    table_set(s, lamps=False)  # the lamp is placed apart, in the table's hue: CafeLamp_Testbed_<hue> at (0.116, -0.320, 0.797) from the pivot


# ----------------------------------------------------------------------------- the room
DY, DZ = cg.Y_BACK_IN - 5.61, cg.Z_FLOOR - 0.99


def _rod_x_y():
    """The pendant centres (the emitter's eight parts), for the rods that reach our ceiling."""
    emit = next(p for p in ROOM if p["name"] == "warm pendant emitter")
    n = len(emit["t"]) // 8
    out = []
    for k in range(8):
        pts = [emit["v"][i] for t in emit["t"][k * n:(k + 1) * n] for i in t]
        out.append((sum(p[0] for p in pts) / len(pts), sum(p[1] for p in pts) / len(pts), max(p[2] for p in pts)))
    return out


@asset("CafeInterior_Testbed", "Plaza", bevel=0.0)
def interior_tb(s):
    """CafeInterior's shell, the testbed's contents."""
    P = cg.front_path(6, 4)
    poly = ccw([(x, y) for x, y in P] + [(cg.X_SIDE - 0.2, cg.Y_BACK_IN), (-cg.X_SIDE + 0.2, cg.Y_BACK_IN)])
    s.extrude(poly, 0.0, cg.Z_FLOOR, pal.Cafe_Floor)
    s.extrude(poly, cg.Z_CEIL, cg.Z_CEIL + 0.06, pal.Cafe_Plaster)
    tube(s, lift(offset(P, 0.45), cg.Z_CEIL - 0.05), sec(-0.04, 0.04, 0.0, 0.05), pal.Cafe_Cove)
    tube(s, lift(offset(P, 0.45), cg.Z_CEIL - 0.07), sec(-0.07, 0.07, 0.0, 0.02), pal.Brass_Civic)
    cg.side_wall(s, -1)
    cg.side_wall(s, 1)
    cg.back_wall(s)
    with s.at((0, DY, DZ)):
        for part in ROOM:
            mesh(s, part, getattr(pal, ROOMMAT["Cafe " + part["name"]]))
        for x, y, ztop in _rod_x_y():  # the testbed's rods stop at its 2.7 m ceiling: carry them up to ours
            s.sweep([(x, y, ztop), (x, y, cg.Z_CEIL - DZ)], 0.011, pal.Chrome, seg=4, caps=False)
    for x, hue in ((-5.4, "Teal"), (5.4, "Peach")):  # a front row by the glass, the testbed's own sets, each lamp its own hue
        table_set(s, (x, 2.9, cg.Z_FLOOR), heading=-0.48 if x < 0 else 0.18, seats=((0.4, 0.96), (3.54, 1.02)) if x < 0 else ((1.9, 1.0), (4.9, 0.95)), hue=hue)
