"""Brief 077 "the domes go" (Vela): the dome-family review page and cost numbers.

python3 -I tools/decokit/review077_domes_page.py [render_dir]   (default out/scratch/vela/077/d, written by review077_domes.py)
  -> briefs/077/domes/index.html (self-contained) and briefs/077/domes/metrics.json
Measured: triangles and welded vertices per LOD of every candidate (domes_pieces.json, pieces.json), today's planting triangles per piece
by material (census.json, from the kit FBX), placements (world export). Computed: zone totals. Estimated: ms (Redline's k <= 0.7 ms per
million view + caster triangles, casters x2 cascades; 1.5-3 us a draw), draws, SetPass. Nothing here is a Unity or Air measurement."""
import base64
import hashlib
import io
import json
import math
import sys
from pathlib import Path

from PIL import Image

REPO = Path(__file__).resolve().parents[2]
R = Path(sys.argv[1]) if len(sys.argv) > 1 else REPO / "out/scratch/vela/077/d"
DST = REPO / "briefs/077/domes"
DST.mkdir(parents=True, exist_ok=True)
SRC = REPO / "out/decokit/troughs077"
DM = json.loads((SRC / "domes077.manifest.json").read_text())
DP = json.loads((SRC / "domes_pieces.json").read_text())["pieces"]
TP = json.loads((SRC / "pieces.json").read_text())["pieces"]
CEN = json.loads((SRC / "census.json").read_text())
TM = json.loads((REPO / "briefs/077/troughs/metrics.json").read_text())
W = json.loads((REPO / "out/world/harbour-district.world.json").read_text())


def sha(p):
    return hashlib.sha256(Path(p).read_bytes()).hexdigest()


PIECES = {**{k: v for k, v in TP.items()}, **DP}
placed = {}
for e in W["civic"]:
    k = (e.get("kit") or {}).get("id")
    if k and not e.get("hidden"):
        placed[k] = placed.get(k, 0) + 1


def lv(pid, level):
    return PIECES[pid]["lods"][level]["tris"]


def top_level(pid):
    return max(PIECES[pid]["lods"].values(), key=lambda x: x["tris"])["tris"]


FAMILY = [
    ("PlanterRun_4 (café square kerbs)", [(f"PlanterRun_4_{v}", f"PlanterRun_4_{v}_TB") for v in "ABC"]),
    ("PlanterRun_2 / Corner / End", [(f"PlanterRun_{k}_{v}", f"PlanterRun_{k}_{v}_TB") for k in ("2", "Corner", "End") for v in "ABC"]),
    ("BedTuft (per unit)", [(f"BedTuft_{v}", f"BedTuft_{v}_TB") for v in "ABC"]),
    ("PlanterArc (fountain islands)", [("PlanterArc", "PlanterArc_TB")]),
    ("Globe lamps (new, with the islands)", [(None, "GlobeLamp_TB")]),
    ("CafeTerrace planting", [("CafeTerrace", "CafeTerrace_Planting_TB")]),
]
BEDS = [f"CivicBed_{b}_TB" for b in DM["civicBeds"]]

rows, zone = [], {"now": 0, "unit": {"gameplay": 0, "top": 0}, "bed": {"gameplay": 0, "top": 0}}
per_piece = {}
for fam, pairs in FAMILY:
    for old, new in pairs:
        n = placed.get(old, 0) if old else placed.get("PlanterArc", 0)
        now_t = CEN[old]["planting_tris"] if old else 0
        p = PIECES[new]
        g, t = lv(new, "gameplay"), top_level(new)
        per_piece[new] = dict(replaces=old, placements=n, now_planting_tris=now_t, now_stone_tris=CEN[old]["stone_tris"] if old else None,
                              lods={k: dict(lod=x["lod"], tris=x["tris"], vertices_welded=x["vertices_welded"], mesh_sha256=x["mesh_sha256"]) for k, x in p["lods"].items()},
                              materials=p["materials"], fbx=p["fbx"], composition=p.get("composition"))
        zone["now"] += n * now_t
        for key in ("unit", "bed"):
            if new.startswith("BedTuft") and key == "bed":
                continue
            zone[key]["gameplay"] += n * g
            zone[key]["top"] += n * t
bed_now_hedge = sum(1 for e in W["civic"] if e.get("surface") == "bed-hedge")
for b in BEDS:
    p = PIECES[b]
    per_piece[b] = dict(replaces="the BedTuft stations and bed-hedge fill of " + b[9:-3], placements=1,
                        lods={k: dict(lod=x["lod"], tris=x["tris"], vertices_welded=x["vertices_welded"], mesh_sha256=x["mesh_sha256"]) for k, x in p["lods"].items()},
                        materials=p["materials"], fbx=p["fbx"], placement=DM["civicBeds"][b[9:-3]], bake_match=DM["bakeLod"][b[9:-3]])
    zone["bed"]["gameplay"] += lv(b, "gameplay")
    zone["bed"]["top"] += top_level(b)


def est(now, new, soil_share=0.002):
    dv = new - now
    return round(0.7 * (dv + 2 * dv * (1 - soil_share)) / 1e6, 3)


zone_rows = {
    "now (domes, cushions, tufts)": dict(tris=zone["now"]),
    "proposed, BedTuft per unit: gameplay LOD (testbed's choice at play zoom)": dict(tris=zone["unit"]["gameplay"], gpu_ms_upper=est(zone["now"], zone["unit"]["gameplay"])),
    "proposed, BedTuft per unit: top LOD everywhere": dict(tris=zone["unit"]["top"], gpu_ms_upper=est(zone["now"], zone["unit"]["top"])),
    "proposed, bed level (testbed's civic beds): gameplay LOD": dict(tris=zone["bed"]["gameplay"], gpu_ms_upper=est(zone["now"], zone["bed"]["gameplay"])),
    "proposed, bed level: top LOD everywhere": dict(tris=zone["bed"]["top"], gpu_ms_upper=est(zone["now"], zone["bed"]["top"])),
}
tuft_n = sum(placed.get(f"BedTuft_{v}", 0) for v in "ABC")
draws = dict(
    note="draws are estimates; in the swap scene some civic planting is baked into shared batches (Latch 075), so today's draw count per piece is not known here",
    per_unit_tufts=f"{tuft_n} instances x 2 submeshes (leaf, petal; C has no blooms: 1) = up to +{2 * tuft_n} forward draws without instancing, 6 with the GPU Resident Drawer; shadow the same again per cascade",
    bed_level=f"5 pieces x 3 submeshes = 15 forward draws, 10 shadow per cascade; one-off meshes, so no instancing needed",
    render_thread_ms=dict(per_unit=f"+{round(2 * tuft_n * 3 * 1.5e-3, 2)} to +{round(2 * tuft_n * 3 * 3e-3, 2)} (forward + 2 shadow cascades, no instancing)", bed_level="+0.07 to +0.14 at most for 45 draws"),
)
bedtuft = dict(
    flag="Not 92 x 12k. The ~12k a bed is the testbed's per BED (its civic flowerBedRun beds), and we have 5 tufted beds: 45,239 triangles at the gameplay LOD for all five. Per unit, a testbed clump in the 0.62 m disc is 92-144 triangles at gameplay (638-680 at the top LOD), so 92 units are 11,678 at gameplay.",
    rings_match="Our five tufted bed-kerb rings are the testbed's own civic beds (civic-planting-layout flowerBedRun outlines), each moved rigidly: max vertex error < 1 mm (domes077.manifest.json civicBeds). So the bed-level composition is literally the testbed's planting of those beds, reproduced by its code to the triangle (gameplay counts equal the bake's, bakeLod).",
    per_unit_reads="Close up the per-unit clumps read as a planted bed, but on today's flat green bed-hedge fill, lower and with fewer flowers than the testbed's beds. At play zoom they read as a loose row of clumps along the ring. The same 3 variants repeat 30 times each, with random yaw. Acceptable as a stopgap, not the testbed's look.",
    bed_level_reads="The testbed's beds: a dense planted mass with a canopy row and tall coral/amber blooms, dark soil at the rim, no hedge slab. It matches the testbed column.",
    proposal_for_splice="Generator rule: a civic bed ring (bed-kerb) is planted by one bed-level composition (cafe-garden-composition with the bed's flowerBedRun planting points: centre canopy row every 0.38 m, border rows every 0.30 m at (w/2 - wall) x 0.62-0.8, rounded-end points), not by BedTuft stations. The bed-hedge fill is dropped and soil sits 3 mm proud of the kerb top. Our five beds are the testbed's, so their compositions are those bakes (CivicBed_<bed>_TB). Any new ring gets a flowerBedRun-style point set from its outline. Cost: 45,239 triangles at gameplay for the five (vs 8,305 tufts + hedge today), 15 forward draws instead of up to 184.",
    recommend="Bed level. It's the testbed's own design, it reads continuous, and it trades about +33k triangles (<= +0.07 ms GPU) for about 170 fewer draws than per-unit tufts would add. Per-unit BedTuft_<v>_TB is built and rendered as asked.",
)
shader = dict(
    owner="Latch (shader code), Glint (look in the grade)",
    found="DK Lit Cutaway's passes include URP's stock LitForwardPass / GBuffer / DepthNormals: no vertex colour and no back-face normal flip (unity/Assets/Suborned/Shaders/DecoKit/DK_LitCutaway.shader lines 198, 345, 449).",
    changes=[
        "Vertex colour: a shader_feature_local _DK_VERTEX_COLOR. Pass COLOR through Varyings and multiply albedo by color.rgb in the forward, GBuffer and Meta passes. The values are linear multipliers (three's), exported linear (FBX colors_type LINEAR); check Unity imports them unconverted. They carry the four leaf tints, root-to-tip occlusion and dark stems.",
        "Back faces: leaf, petal and stem materials are Cull Off (testbed DoubleSide). In the forward, GBuffer and DepthNormals fragments, flip the world normal and the tangent frame for back faces (FRONT_FACE_SEMANTIC + IS_FRONT_VFACE), or the undersides light as if facing away. The shadow caster stays Cull Off.",
        "Roughness: the testbed reads roughness = 0.49 (leaf) or 0.64 (petal) x ORM green. URP Lit wants smoothness in the metallic map's alpha, so derive a mask map at build time (A = 1 - k x ORM.g, same size, deterministic) or add a roughness-map keyword. Either is a format derivation, not a texture cut.",
        "Normal scale 0.16 (_BumpScale), base colour #afbea1 on leaves, white on petals, soil maps on DK_Soil_TB (normal 0.4).",
    ],
    setpass="One new keyword set (vertex colour + normal + mask + cull off) for leaf and petal: forward SetPass +1, shadow +1 (cull off). The lamps' chrome and opal can map onto the kit's existing DK_Chrome / DK_Lamp_Deco (0 new). Not measured.",
    start_now="Can start in parallel: the candidate FBXs and the material spec are in out/decokit/troughs077/Models and briefs/077/troughs/metrics.json.",
)
metrics = dict(
    schema="suborned.metrics077.domes/1", brief="077 the domes go", author="Vela (Opus 5.5)", date="2026-10-30",
    status="candidates, review only: nothing catalogued, copied into Unity or placed",
    measured_vs_estimated=TM["measured_vs_estimated"],
    placements_in_zone={k: placed.get(k, 0) for k in ["PlanterRun_4_A", "PlanterRun_4_B", "PlanterRun_4_C", "PlanterRun_2_A", "PlanterRun_Corner_A", "PlanterRun_End_A", "BedTuft_A", "BedTuft_B", "BedTuft_C", "PlanterArc", "CafeTerrace"]},
    pieces=per_piece, census_now=CEN, zone_total=zone_rows, draws=draws, bedtuft=bedtuft, leaf_shader=shader,
    textures=dict(new_MiB=0.0, note="every piece shares the troughs' atlas v2 + soil set (9.0 MiB with mips, BC7/BC5, counted once in briefs/077/troughs/metrics.json); the lamps are untextured"),
    alpha="none anywhere: opaque curved meshes (testbed opaque: true)", shadows="leaves, stems, petals and lamps cast; soil does not",
    lights=dict(globe_lamps="4 point lights, #fff1d8, range 5.8 m, decay 2, no shadows, at 2.31 m (testbed intensity 0 by day): night only, a per-pixel cost in their 5.8 m radius (Glint)"),
    not_ported=["records_tower.py and cafe_upper.py embed planting() too (the Records Office crown trough and the café roof trough): still domes, not in this brief's list",
                "the testbed's two island palms (fountain-garden-layout palms on islands 0 and 1): our PlanterArc has none; a placement decision",
                "OvalPlanter (civic-monument) has no dome planting (soil only)"],
    provenance=dict(sources=DM["sources"], generator={**DM["generator"], **{f"tools/decokit/{f}": sha(REPO / "tools/decokit" / f) for f in ("domes077.py", "domes077_paths.py", "review077_domes.py", "review077_domes_page.py", "troughs077_lib.py")}},
                    bins={k: v["sha256"] for k, v in DM["files"].items()}),
)
(DST / "metrics.json").write_text(json.dumps(metrics, indent=1) + "\n")


# ------------------------------------------------------------------ page
def jpg(img, q=82):
    b = io.BytesIO()
    img.convert("RGB").save(b, "JPEG", quality=q, optimize=True)
    return "data:image/jpeg;base64," + base64.b64encode(b.getvalue()).decode()


def fig(name, cap):
    p = R / name
    if not p.exists():
        return f"<figure><div class='miss'>{name}: not rendered</div><figcaption>{cap}</figcaption></figure>"
    return f"<figure><img src='{jpg(Image.open(p))}'><figcaption>{cap}</figcaption></figure>"


def project(c, yaw, half, pt, W_=1920, H_=1080):
    yw, pc = math.radians(yaw), math.radians(38)
    f = (math.sin(yw) * math.cos(pc), math.cos(yw) * math.cos(pc), -math.sin(pc))
    r = (math.cos(yw), -math.sin(yw), 0.0)
    u = (r[1] * f[2] - r[2] * f[1], r[2] * f[0] - r[0] * f[2], r[0] * f[1] - r[1] * f[0])
    d = [a - b for a, b in zip(pt, c)]
    s = H_ / (2 * half)
    return W_ / 2 + s * sum(a * b for a, b in zip(d, r)), H_ / 2 - s * sum(a * b for a, b in zip(d, u))


def crop(name, c, yaw, pt, cap, w=420, h=230):
    p = R / name
    if not p.exists():
        return ""
    im = Image.open(p)
    x, y = project(c, yaw, 14, pt)
    x0, y0 = int(max(0, min(im.width - w, x - w / 2))), int(max(0, min(im.height - h, y - h / 2)))
    return f"<figure class='crop'><img src='{jpg(im.crop((x0, y0, x0 + w, y0 + h)).resize((w * 2, h * 2), Image.NEAREST))}'><figcaption>{cap} · play-zoom pixels at 2x</figcaption></figure>"


def row3(cells):
    return "<div class='row'>" + "".join(cells) + "</div>"


def tr(cells, head=False):
    t = "th" if head else "td"
    return "<tr>" + "".join(f"<{t}>{c}</{t}>" for c in cells) + "</tr>"


cost = [tr(["piece (replaces)", "in zone", "now planting tris", "LOD tris (top ... gameplay)", "welded verts (top / gameplay)", "materials", "textures", "alpha", "shadows"], True)]
for pid, x in per_piece.items():
    lods = sorted(x["lods"].items(), key=lambda kv: kv[1]["lod"])
    cost.append(tr([f"<b>{pid}</b><br><small>{x['replaces'] or 'new'}</small>", x["placements"], x.get("now_planting_tris", "-"),
                    " / ".join(f"{k} {v['tris']}" for k, v in lods), f"{lods[0][1]['vertices_welded']} / {lods[-1][1]['vertices_welded']}",
                    ", ".join(x["materials"]), "shared atlas + soil (0 new)" if "Lamp" not in pid else "none", "opaque", "cast (soil no)"]))
ztab = [tr(["Handover zone (civic block), every dome-family placement", "triangles", "GPU upper vs now (ms)"], True)] + \
       [tr([k, v["tris"], f"+{v['gpu_ms_upper']}" if "gpu_ms_upper" in v else "-"]) for k, v in zone_rows.items()]

SQ, BD = (-159.0, -230.0, 0.18), (-157.0, -296.0, 0.18)
css = """body{font:14px/1.45 -apple-system,Helvetica,Arial,sans-serif;margin:24px;background:#f5f2ea;color:#17302f}h1{margin:0 0 4px}h2{margin-top:34px;border-bottom:1px solid #99784d}
.row{display:flex;gap:10px;flex-wrap:wrap}figure{margin:0;flex:1 1 23%;min-width:300px}figure img{width:100%;display:block;border:1px solid #cbc6c3}
figcaption{font-size:12px;color:#40605d;margin:3px 0 10px}table{border-collapse:collapse;margin:8px 0 14px;font-size:12.5px}td,th{border:1px solid #cbc6c3;padding:3px 7px;text-align:left;vertical-align:top}
th{background:#e7e1d3}.box{background:#fff;border-left:4px solid #99784d;padding:10px 14px;max-width:1150px;margin:10px 0}.miss{height:200px;background:#ddd;display:flex;align-items:center;justify-content:center}ul{max-width:1150px}"""
T, N, P, PB = "testbed", "now", "proposed", "proposed-bed"
html = f"""<!doctype html><html><head><meta charset='utf-8'><title>077 · the domes go: testbed | now | proposed</title><style>{css}</style></head><body>
<h1>077 · the domes go: the whole dome planting family, testbed | now | proposed</h1>
<p>Vela, 2026-10-30. Sky: today's planting "shouldn't exist in our kit at all at that design - they're low poly placeholder looking". Every dome-family piece below gets the testbed's planting, composed by the testbed's own code (café composition, fountain garden, globe lamps; atlas v2, its leaf and flower geometry, its authored LODs) on our existing stonework, at the same piece transform. Candidates only. Day, Blender light (not the game's grade). The café troughs alone, with night and the LOD0 comparison, are on <a href='../troughs/index.html'>troughs/index.html</a>.</p>
<div class='box'><b>Verdict.</b> Every family reads as the testbed: glossy curled leaves, strap fans, cupped coral and gold blooms, dark soil. The run family (2 / 4 / Corner / End) joins into one continuous bed. The fountain islands are the testbed's islands, which share our PlanterArc outline (r 5.36 to 7.06, 27.5°). The terrace troughs follow the café curve. For the civic beds, <b>bed-level planting (the testbed's own beds) is the one that reads as the testbed</b>. Per-unit tufts read as a row of clumps on today's flat hedge slab. The look caveat from the troughs holds throughout: darker and less candy-bright than the domes at play zoom. Sky judges that from a Unity capture once the leaf shader below is in.</div>
<div class='box'><b>Leaf shader (Latch / Glint, can start now).</b> {shader['found']}<ul>{''.join(f'<li>{c}</li>' for c in shader['changes'])}</ul>{shader['setpass']}</div>
<h2>Zone total at play zoom (running)</h2><table>{''.join(ztab)}</table><div class='box'><b>Headroom.</b> At the testbed's own gameplay LOD the whole zone costs at most +0.06 (per unit) or +0.13 ms (bed level). Drawing every piece at its top LOD instead (no LODGroup, as the 075 trial drew) is +0.38 / +0.60 ms. That second figure is the whole of the 0.6 ms Redline has in hand, so the full-detail-everywhere route doesn't fit. The testbed's own authored LODs are what the testbed drew at this zoom (no decimation), and keeping them is Sky's call under 028. I recommend them.</div>
<p>The upper bound counts the whole zone in view, with casters in two cascades. A play view sees one part of it (the square view or the beds view). Draws: {draws['per_unit_tufts']}; bed level {draws['bed_level']}. Render thread for per-unit tufts: {draws['render_thread_ms']['per_unit']} ms, which is the bigger cost of the per-unit route. Textures: 0 new (the troughs' 9.0 MiB set is shared). Alpha: none.</p>
<div class='box'><b>BedTuft, flagged early.</b> {bedtuft['flag']} {bedtuft['rings_match']}<br><br><b>Per unit:</b> {bedtuft['per_unit_reads']}<br><b>Bed level:</b> {bedtuft['bed_level_reads']}<br><b>Rule for Splice:</b> {bedtuft['proposal_for_splice']}<br><b>Recommendation:</b> {bedtuft['recommend']}</div>

<h2>Café square at play zoom: troughs, terrace, fountain islands and lamps</h2>
{row3([fig(f'{T}-square-y030.png', 'testbed: its café beds (its baked meshes)'), fig(f'{N}-square-y030.png', 'now · yaw 30'), fig(f'{P}-square-y030.png', 'proposed · yaw 30')])}
{row3([fig(f'{T}-square-y210.png', 'testbed · yaw 210'), fig(f'{N}-square-y210.png', 'now · yaw 210'), fig(f'{P}-square-y210.png', 'proposed · yaw 210')])}
{row3([crop(f'{N}-square-y030.png', SQ, 30, (-155.7, -232.2, 0.8), 'now: NE fountain island'), crop(f'{P}-square-y030.png', SQ, 30, (-155.7, -232.2, 0.8), 'proposed: NE island + lamp'),
       crop(f'{N}-square-y030.png', SQ, 30, (-150.0, -226.0, 0.8), 'now: terrace east run'), crop(f'{P}-square-y030.png', SQ, 30, (-150.0, -226.0, 0.8), 'proposed: terrace east run')])}

<h2>Fountain islands (PlanterArc) and globe lamps</h2>
{row3([fig(f'{T}-fountain-y030.png', "testbed: its fountain garden by its own code (stone, soil, planting, 4 instances) and its 4 globe lamps; no AtomFountain"), fig(f'{N}-close-arc-y030.png', 'now · close · NE island'), fig(f'{P}-close-arc-y030.png', 'proposed · close (fountain LOD close)')])}
{row3([fig(f'{T}-close-arc-y210.png', 'testbed · close · yaw 210'), fig(f'{N}-close-arc-y210.png', 'now · close · yaw 210'), fig(f'{P}-close-arc-y210.png', 'proposed · close · yaw 210')])}

<h2>CafeTerrace planting</h2>
{row3([fig(f'{T}-close-terrace-y030.png', 'testbed: its café strip (the recipe used on the terrace runs)'), fig(f'{N}-close-terrace-y030.png', 'now · close · yaw 30'), fig(f'{P}-close-terrace-y030.png', 'proposed · close · yaw 30')])}
<p>At yaw 210 the café itself stands between the camera and its terrace, in both now and proposed, so the close view there shows the building. That view is in the render folder, not repeated here.</p>

<h2>Civic beds: BedTuft per unit vs bed level</h2>
{row3([fig(f'{T}-beds-y030.png', 'testbed: its baked civic beds'), fig(f'{N}-beds-y030.png', 'now · yaw 30'), fig(f'{P}-beds-y030.png', 'proposed: BedTuft_&lt;v&gt;_TB per unit'), fig(f'{PB}-beds-y030.png', 'proposed: bed level (CivicBed_*_TB)')])}
{row3([fig(f'{T}-beds-y210.png', 'testbed · yaw 210'), fig(f'{N}-beds-y210.png', 'now · yaw 210'), fig(f'{P}-beds-y210.png', 'per unit · yaw 210'), fig(f'{PB}-beds-y210.png', 'bed level · yaw 210')])}
{row3([fig(f'{T}-close-bed-y030.png', 'testbed · close · dropoff-west'), fig(f'{N}-close-bed-y030.png', 'now · close'), fig(f'{P}-close-bed-y030.png', 'per unit · close'), fig(f'{PB}-close-bed-y030.png', 'bed level · close')])}
{row3([crop(f'{N}-beds-y030.png', BD, 30, (-141.1, -294.9, 0.9), 'now: office-west bed'), crop(f'{P}-beds-y030.png', BD, 30, (-141.1, -294.9, 0.9), 'per unit'), crop(f'{PB}-beds-y030.png', BD, 30, (-141.1, -294.9, 0.9), 'bed level')])}

<h2>The run family: PlanterRun_End_A + 2_B + Corner_C + 4_A + End_B joined (studio; none of 2 / Corner / End is placed in the world today)</h2>
{row3([fig(f'{T}-close-terrace-y210.png', 'testbed: its café strip (reference)'), fig('runs-now-runs-close-y030.png', 'now · close · yaw 30'), fig('runs-proposed-runs-close-y030.png', 'proposed · close · yaw 30')])}
{row3([fig('runs-now-runs-play-y210.png', 'now · play zoom · yaw 210'), fig('runs-proposed-runs-play-y210.png', 'proposed · play zoom · yaw 210'), fig('runs-proposed-runs-close-y210.png', 'proposed · close · yaw 210')])}
<p>The planting meets across the joints: the leaf mass runs through. The soil stops 5.5 cm short of each open end (the testbed's inward wall offset is applied all round), so a thin limestone bar shows at each joint between leaves. The fix is a generator one: inset the soil only along the side walls.</p>

<h2>Cost per piece</h2><table>{''.join(cost)}</table>
<p>LODs are the testbed's own: café-recipe pieces are LOD0 inspection, LOD1 close, LOD2 near, LOD3 gameplay (Unity relative heights 0.667 / 0.296 / 0.167, for a 4 m piece; scale by piece size). PlanterArc_TB is LOD0 close, LOD1 near, LOD2 gameplay (the fountain's 0.55 m leaf rule: 44 / 24 px). Play zoom draws gameplay. The CivicBed counts equal the testbed's bake triangle for triangle at the gameplay and near levels.</p>

<h2>Open issues</h2><ul>
<li>The leaf shader changes above gate any Unity look judgement.</li>
<li>Heights: café-recipe stems and blooms reach about 1.0 to 1.1 m above their soil, as on the troughs. The civic beds' canopy blooms go higher: 1.76 to 1.89 m above the bed base, 1.24 to 1.36 m above soil. All render-only with no collider. Keel and Vantage confirm sight ignores render meshes.</li>
<li>Soil at the run joints (above). The bed-level route drops the world's bed-hedge surface: that's a world generator change (Splice).</li>
<li>The lamps stand in the islands' soil where the testbed stands them. They're new placements with 4 night point lights (Glint), so Vantage and Datum sign off the placement.</li>
<li>Not in this list but still domes: records_tower.py (Records Office crown trough) and cafe_upper.py (café roof trough) embed planting(). They're the same recipe, so the next pass can reuse this.</li>
<li>The "same piece id" merge: candidates are planting-only meshes beside the stonework. Folding them into the generator so DK_PlanterRun_4_A etc. are rebuilt with testbed planting is my next generator step once Sky picks, then Splice catalogues and rejects the dome versions.</li>
<li>The generator imports the testbed's code from outside the repo (sha256 pinned). If adopted, Splice vendors it.</li>
</ul>
<h2>Provenance</h2><table>{''.join(tr([k, f'<code>{v}</code>']) for k, v in {**metrics['provenance']['sources'], **metrics['provenance']['generator'], **{p + ' FBX': x['fbx']['sha256'] for p, x in per_piece.items()}}.items())}</table>
</body></html>"""
(DST / "index.html").write_text(html)
print("wrote", DST / "index.html", round((DST / "index.html").stat().st_size / 2**20, 1), "MiB")
print(json.dumps(zone_rows, indent=1))
