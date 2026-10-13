#!/usr/bin/env python3
"""Brief 008 B1: the paving band design for the civic block, as data Splice can emit from (out/decokit-008/paving-bands.json) and a
plan drawing (paving-bands.svg). Block frame (contracts/world/civic-block.json): x south, y east, z up, metres; angles run from +x
toward +y (the same turn as a piece's yaw about +Z). Every number below is read off civic-block.json or the parts list; nothing is
placed on the plan. Pure Python. Run: python3 tools/decokit/bands008.py"""

import json
import math
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
OUT = REPO / "out/decokit-008"
BLOCK = json.loads((REPO / "contracts/world/civic-block.json").read_text())
F = (6.5, -30.0)  # atom fountain centre
O = {o["id"]: o["polygon"] for o in BLOCK["objects"]}
S = {o["id"]: o["polygon"] for o in BLOCK["structures"]}
r2 = lambda v: round(v, 3)  # noqa: E731

pieces = []  # {"id", "piece", "at":[x,y], "yawDeg", "scaleX"?, "role"}


def line(bid, piece, x0, y0, x1, y1, width_name, role):
    """Straight band from (x0,y0) to (x1,y1) in 2 m modules; the last module is scaled along its length to land on the end."""
    L = math.hypot(x1 - x0, y1 - y0)
    yaw = math.degrees(math.atan2(y1 - y0, x1 - x0))
    ux, uy = (x1 - x0) / L, (y1 - y0) / L
    n, rest, k = int(L // 2), L - 2 * int(L // 2), 0
    for i in range(n):
        c = 1.0 + 2 * i
        pieces.append(dict(id=f"{bid}.{k}", band=bid, piece=piece, at=[r2(x0 + ux * c), r2(y0 + uy * c)], yawDeg=r2(yaw), role=role))
        k += 1
    if rest > 0.05:
        c = 2 * n + rest / 2
        pieces.append(dict(id=f"{bid}.{k}", band=bid, piece=piece, at=[r2(x0 + ux * c), r2(y0 + uy * c)], yawDeg=r2(yaw), scaleX=r2(rest / 2), role=role))
    return dict(id=bid, kind="line", piece=piece, width=width_name, from_=[x0, y0], to=[x1, y1], length=r2(L), yawDeg=r2(yaw), role=role)


def ring(bid, piece, c, radius, width, role):
    for k in range(4):
        pieces.append(dict(id=f"{bid}.{k}", band=bid, piece=piece, at=[r2(c[0]), r2(c[1])], yawDeg=90 * k, role=role))
    return dict(id=bid, kind="ring", piece=piece, centre=[r2(c[0]), r2(c[1])], radius=radius, width=width, role=role)


bands = []
# 1. the café spine: 1.2 m, y -29.5 (half-way between the door axis y -29 and the fountain axis y -30; both stay inside its width),
#    from the door sill (x -10.2) to the outer fountain ring's edge (x -2.7)
R_OUT, R_IN = 8.6, 4.3
x_ring = F[0] - (R_OUT + 0.6)
bands.append(line("spine-north", "InlayBand_12_2", -10.2, -29.5, x_ring, -29.5, "1.2", "café door to the fountain"))
# 2. inside the outer ring the spine narrows to 0.6 and runs to the inner ring (x 1.6): the painting's tapering band
x_in0, x_in1 = F[0] - (R_OUT - 0.6), F[0] - (R_IN + 0.3)
bands.append(line("spine-inner", "InlayBand_06_2", x_in0, -29.5, x_in1, -29.5, "0.6", "narrowing toward the pool"))
# 3. the rings round the fountain (pool coping R 3.3 to 3.6; planter ring R 5.36 to 7.06 at the diagonals)
bands.append(ring("ring-inner", "InlayArc_R4_W06", F, R_IN, 0.6, "between the coping (3.6) and the planters (5.36): 0.4 m of slab inside, 0.76 m outside"))
bands.append(ring("ring-outer", "InlayArc_R9_W12", F, R_OUT, 1.2, "0.94 m of slab outside the planter ring (7.06); clear of the café terrace wall (x -4.8) and the crew line"))
# 4. the shelter spoke: 1.2 m on the fountain's west axis (x 6.5) from the outer ring to 0.7 m short of the square shelter (roof edge y -44.86);
#    x 6.5 falls between the two PavilionPier_Square piers (x 5.84 to 11.16 open)
y_w0, y_w1 = F[1] - (R_OUT + 0.6), -44.86 + 0.7
bands.append(line("spoke-west", "InlayBand_12_2", F[0], y_w0, F[0], y_w1, "1.2", "fountain ring to the square shelter"))
# 5. the Records forecourt bar: 1.2 m parallel to the tower face (x 52.5), 0.7 m before the stair foot (x 49.2), across the 8.2 m
#    corridor between the civic-entrance beds (y -34.5 to -26.3)
bands.append(line("forecourt-bar", "InlayBand_12_2", 47.9, -34.4, 47.9, -26.4, "1.2", "apron line before the entrance stairs"))
# 6. lamp rings: one full ring (4 x InlayArc_R2_W06) per PlazaSphereLamp_Play that Vantage places on the fountain plaza;
#    suggested lamps on the diagonals at 12.4 m from the fountain: 0.9 m of slab between the lamp ring and the outer ring
lamps = [(F[0] + 12.4 * math.cos(math.radians(a)), F[1] + 12.4 * math.sin(math.radians(a))) for a in (45, 135, 225, 315)]
for i, l in enumerate(lamps):
    bands.append(ring(f"lamp-ring-{i}", "InlayArc_R2_W06", l, 2.0, 0.6, "suggested lamp position (Vantage's to move)"))

cnt = {}
for p in pieces:
    cnt[p["piece"]] = cnt.get(p["piece"], 0) + 1
rules = {
    "schema": "suborned.paving-bands/1", "brief": "008 B1", "date": "2026-10-14",
    "frame": "block frame of contracts/world/civic-block.json: x south, y east, metres; angles from +x toward +y = the piece's yaw about +Z; arcs are quarter turns sweeping local +X to +Y, pivot at the arc centre",
    "drawn_from": "gameplay-plan, mission-block and inez-cutaway paintings (26 Sep): large pale slabs, seafoam bands 0.6 to 1.2 m wide, arcs round the lamps and the fountain",
    "slabs": {"material": "DK_Pave_Civic", "tile_m": 4.0, "use": "plot surfaces of the fountain square, the café terrace and the office apron; world-space UVs"},
    "inlay": {"material": "DK_Inlay_Seafoam", "proud_mm": 4, "widths_m": [0.6, 1.2], "rules": [
        "bands never cross a Handover point's footprint or the kerb of a bay; they may pass under furniture",
        "straight bands are 2 m modules; the last module of a run is scaled along its length (scaleX), never across",
        "arc rings are four quarter pieces at yaw 0, 90, 180, 270 about the centre",
        "a ring leaves 0.4 m or more of slab to the coping, 0.9 m to other bands, 0.5 m to planters and walls",
        "no band in the getaway bay or the crew line (x -2 to 16, y -52 to -44)",
        "colour is the enamel seafoam #72B5A7 (the 4 mm ceramic strip); gold is never used in paving"]},
    "anchors": {"fountain_centre": list(F), "pool_coping_R": [3.3, 3.6], "planter_ring_R": [5.36, 7.06], "cafe_door": [-10.3, -29.0], "cafe_ring_line_x": -10.7,
                "cafe_terrace_wall_x": -4.8, "tower_public_face_x": 52.5, "stair_foot_x": 49.2, "entrance_bed_gap_y": [-34.5, -26.3],
                "square_shelter": {"x": [4.3, 12.7], "y": [-47.0, -44.86]}, "handover_points_kept_clear": {"plaza_east_west": [[6.5, -25.5], [6.5, -34.5]], "steps": [-3.6, -29.0], "crew_line": [[1.2, -49.7], [4.8, -49.7]]}},
    "bands": bands,
    "pieces": pieces,
    "counts": cnt,
    "not_designed": ["terrace edge band (the café terrace is plain slab; its wall already draws the curve)", "the link between the two plazas (x 14 to 47: the Orbit Walk crossing H6, block x 31 to 44, is not Vela's)", "the monument plaza (x 24 to 26)", "lamp positions (Vantage places them; rings follow)"],
}
for b in rules["bands"]:
    if "from_" in b:
        b["from"] = b.pop("from_")
(OUT / "paving-bands.json").write_text(json.dumps(rules, indent=1) + "\n")
print("paving-bands.json:", len(pieces), "pieces", cnt)


# ---------------------------------------------------------------------------------------------------------- SVG
def svg(view, scale, title):
    x0, x1, y0, y1 = view  # block x range, block y range
    W, H = (x1 - x0) * scale, (y1 - y0) * scale
    px = lambda x: (x - x0) * scale  # noqa: E731
    py = lambda y: (y1 - y) * scale  # noqa: E731  (east up)
    o = [f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {W:.0f} {H:.0f}" role="img" aria-label="{title}">',
         f'<rect width="{W:.0f}" height="{H:.0f}" fill="#efe8d6"/>']

    def poly(p, fill, stroke="none", op=1.0, sw=0):
        o.append(f'<polygon points="{" ".join(f"{px(a):.1f},{py(b):.1f}" for a, b in p)}" fill="{fill}" stroke="{stroke}" stroke-width="{sw}" opacity="{op}"/>')

    def circ(c, r, fill="none", stroke="none", sw=0, dash=""):
        o.append(f'<circle cx="{px(c[0]):.1f}" cy="{py(c[1]):.1f}" r="{r * scale:.1f}" fill="{fill}" stroke="{stroke}" stroke-width="{sw}" {dash}/>')

    # grid every 4 m
    gx = int(x0 // 4 * 4)
    for x in range(gx, int(x1) + 1, 4):
        if x >= x0:
            o.append(f'<line x1="{px(x):.1f}" y1="0" x2="{px(x):.1f}" y2="{H:.0f}" stroke="#d9d0bc" stroke-width="0.6"/>')
    gy = int(y0 // 4 * 4)
    for y in range(gy, int(y1) + 1, 4):
        if y >= y0:
            o.append(f'<line x1="0" y1="{py(y):.1f}" x2="{W:.0f}" y2="{py(y):.1f}" stroke="#d9d0bc" stroke-width="0.6"/>')
    # obstacles
    for b_ in BLOCK["buildings"]:
        v = b_["polygon"]
        poly(v, "#c9bfa8", "#8a806c", 1, 0.8)
    for k, v in O.items():
        if k.startswith(("fountain-garden", "civic-entrance", "cafe-palm")):
            poly(v, "#9bbf8e", "#5f8a56", 0.9, 0.6)
        elif k.startswith("cafe-table"):
            poly(v, "#e8dcc2", "#9a8f77", 1, 0.5)
    circ(F, 3.6, "#b8d8da", "#8aa8ac", 1)
    circ(F, 3.3, "#6fb7c4", "none", 0)
    for k, v in S.items():
        if "square" in k:
            poly(v, "#d9d0bc" if "boarding" in k else "#b0a690", "#6d6552", 0.9, 0.8)
    for b in BLOCK["bays"]:
        poly(b["polygon"], "#cfcabd", "#8a8575", 0.55, 0.8)
    # tower
    poly([(52.5, -33), (58.5, -33), (58.5, -27), (52.5, -27)], "#e3d7bf", "#8a806c", 1, 1)
    poly([(49.2, -32.3), (52.5, -32.3), (52.5, -27.7), (49.2, -27.7)], "#d9ceb6", "#8a806c", 1, 0.8)
    # bands
    for p in pieces:
        if p["piece"].startswith("InlayBand"):
            w = 1.2 if "12" in p["piece"] else 0.6
            L = 2.0 * p.get("scaleX", 1.0)
            c, a = p["at"], math.radians(p["yawDeg"])
            ux, uy, vx, vy = math.cos(a), math.sin(a), -math.sin(a), math.cos(a)
            poly([(c[0] + s_ * L / 2 * ux + t_ * w / 2 * vx, c[1] + s_ * L / 2 * uy + t_ * w / 2 * vy) for s_, t_ in ((-1, -1), (1, -1), (1, 1), (-1, 1))], "#72b5a7", "#1f6c67", 1, 0.5)
    for b in bands:
        if b["kind"] == "ring":
            circ(b["centre"], b["radius"], "none", "#72b5a7", b["width"] * scale)
            circ(b["centre"], b["radius"] + b["width"] / 2, "none", "#1f6c67", 0.5)
            circ(b["centre"], b["radius"] - b["width"] / 2, "none", "#1f6c67", 0.5)
    for i, l in enumerate(lamps):
        if x0 <= l[0] <= x1 and y0 <= l[1] <= y1:
            circ(l, 0.28, "#8b8f94", "#3a3d40", 1)
    # handover points
    for k, (hx, hy) in {"E": (6.5, -25.5), "W": (6.5, -34.5), "steps": (-3.6, -29.0)}.items():
        if x0 <= hx <= x1 and y0 <= hy <= y1:
            circ((hx, hy), 0.35, "#fff", "#c0392b", 1.4)
    # scale bar + labels
    o.append(f'<g font-family="system-ui,sans-serif" font-size="{max(9, scale * 0.55):.0f}" fill="#3a3a3a">')
    o.append(f'<text x="6" y="{H - 6:.0f}">{title}</text></g>')
    o.append(f'<line x1="{W - 6 - 4 * scale:.0f}" y1="{H - 10:.0f}" x2="{W - 6:.0f}" y2="{H - 10:.0f}" stroke="#333" stroke-width="2"/><text x="{W - 6 - 4 * scale:.0f}" y="{H - 14:.0f}" font-family="system-ui,sans-serif" font-size="10" fill="#333">4 m</text>')
    o.append("</svg>")
    return "\n".join(o)


(OUT / "paving-bands.plaza.svg").write_text(svg((-13, 19, -52, -16), 17, "Fountain square: block x (south) to the right, y (east) up"))
(OUT / "paving-bands.forecourt.svg").write_text(svg((44, 62, -42, -18), 28, "Records forecourt"))
print("svg written")
