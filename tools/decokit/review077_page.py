"""Brief 077 steps 2-3 (Vela): the café trough review page and the cost numbers.

python3 -I tools/decokit/review077_page.py [render_dir]   (default out/scratch/vela/077/r, written by review077_troughs.py)
  -> briefs/077/troughs/index.html (self-contained: every image inlined as JPEG) and briefs/077/troughs/metrics.json

Measured: triangles, welded vertices, submeshes, bounds, fit (troughs077.py pieces.json, exact), the current domes' triangles (kit.json),
texture dimensions (PNG headers). Computed: texture memory at the stated import format, the café-square totals, the Unity LOD transition
heights. Estimated: frame time (Redline's 072 k <= 0.7 ms per million drawn triangles incl. shadows; 1.5-3 us per draw on the render
thread; 2.1-2.5 ms per Mpx warm pixel work), SetPass and draws in Unity. Nothing here is a Unity or Air measurement."""
import base64
import hashlib
import io
import json
import math
import struct
import sys
from pathlib import Path

from PIL import Image

REPO = Path(__file__).resolve().parents[2]
R = Path(sys.argv[1]) if len(sys.argv) > 1 else REPO / "out/scratch/vela/077/r"
DST = REPO / "briefs/077/troughs"
DST.mkdir(parents=True, exist_ok=True)
SRC = REPO / "out/decokit/troughs077"
ART = Path("~/Projects/the-suborned-store/testbed-art/cafe-plants").expanduser()
MAN = json.loads((SRC / "troughs077.manifest.json").read_text())
PCS = json.loads((SRC / "pieces.json").read_text())["pieces"]
KIT = {a["id"]: a for a in json.loads((REPO / "contracts/world/kit.json").read_text())["assets"]}
W = json.loads((REPO / "out/world/harbour-district.world.json").read_text())


def sha(p):
    return hashlib.sha256(Path(p).read_bytes()).hexdigest()


# ------------------------------------------------------------------ placements (the world export)
runs = [e for e in W["civic"] if (e.get("kit") or {}).get("id", "").startswith("PlanterRun_4_") and not e.get("hidden")]
cafe_ids = {"civic/run-west-cafe/0", "civic/run-west-cafe/1", "civic/run-east-cafe/0", "civic/run-east-cafe/1", "civic/run-east-cafe/2", "civic/run-east-cafe/3"}
count = lambda ids: {v: sum(1 for e in runs if e["id"] in ids and e["kit"]["id"].endswith(v)) for v in "ABC"}
SQUARE = count({e["id"] for e in runs})
CAFE = count(cafe_ids)

# ------------------------------------------------------------------ per piece
LEVELS = ["gameplay", "near", "close", "inspection"]
now = {}
for v in "ABC":
    a = KIT[f"PlanterRun_4_{v}"]
    now[v] = dict(piece=a["id"], status=a["status"], tris_L0=a["tris"], trough_tris=60, planting_tris_L0=a["tris"] - 60, tris_L1=a["lods"][0]["tris"],
                  l1_screen=a["lods"][0]["screen"], materials=a["materials"], submeshes=len(a["materials"]), top=a["size"][1], textures=0,
                  alpha="none (opaque flat colour)", note="PlanterRun_4 planting = slots 1-4; the 60-triangle trough (slot 0) stays in both")


def png_size(p):
    b = Path(p).read_bytes()[:24]
    return struct.unpack(">II", b[16:24])


def mips(w, h, bpp):
    tot, lv = 0, 0
    while True:
        tot += max(1, w) * max(1, h) * bpp
        lv += 1
        if w <= 1 and h <= 1:
            break
        w, h = max(1, w // 2), max(1, h // 2)
    return tot, lv


TEX = []
for n, role, fmt in (("botanical-atlas-v2", "albedo (sRGB)", "BC7"), ("botanical-normal-v2", "normal", "BC5"), ("botanical-orm-v2", "ORM (G = roughness, as three)", "BC7"),
                     ("soil-base", "soil albedo (sRGB)", "BC7"), ("soil-normal", "soil normal", "BC5"), ("soil-orm", "soil ORM", "BC7")):
    p = ART / f"{n}.png"
    w, h = png_size(p)
    npot = (w & (w - 1)) or (h & (h - 1))
    iw, ih = (1 << round(math.log2(w)), 1 << round(math.log2(h))) if npot else (w, h)
    bc, lv = mips(iw, ih, 1.0)
    raw, _ = mips(w, h, 4.0)
    TEX.append(dict(file=f"testbed-art/cafe-plants/{n}.png", sha256=sha(p), role=role, source_px=[w, h], npot=bool(npot),
                    import_px=[iw, ih], import_note="Non-Power of 2 = ToNearest (an upscale; BC needs multiple-of-4 sides and 887 is not)" if npot else "as is",
                    format=fmt, mips=lv, MiB_compressed=round(bc / 2**20, 2), MiB_rgba32_uncompressed_at_source=round(raw / 2**20, 2)))
tex_total = round(sum(t["MiB_compressed"] for t in TEX), 2)
tex_raw = round(sum(t["MiB_rgba32_uncompressed_at_source"] for t in TEX), 2)

prop = {}
for v in "ABC":
    p = PCS[f"PlanterRun_4_{v}_TB"]
    prop[v] = dict(piece=f"PlanterRun_4_{v}_TB", status="candidate", fbx=p["fbx"], lods={l: dict(lod=p["lods"][l]["lod"], tris=p["lods"][l]["tris"], vertices_welded=p["lods"][l]["vertices_welded"],
                                                                                                       mesh_sha256=p["lods"][l]["mesh_sha256"], top=p["lods"][l]["top"]) for l in LEVELS},
                   materials=p["materials"], submeshes=3, fit=p["fit"], composition=p["composition"],
                   alpha="none: opaque, the curved meshes make the silhouettes (testbed 'opaque: true'); no alpha test, no cards",
                   shadow="foliage (with stems) and flowers cast; soil does not (testbed castShadow kind != soil)")

# Unity LODGroup heights that reproduce the testbed's own choice at 1080 rows: relative height = size * px_per_m / 1080.
SIZE = 4.0
lod_rel = {"LOD0 inspection": round(SIZE * 180 / 1080, 4), "LOD1 close": round(SIZE * 80 / 1080, 4), "LOD2 near": round(SIZE * 45 / 1080, 4), "LOD3 gameplay": 0.01}
play_rel = round(SIZE / 28, 4)


def total(counts, f):
    return sum(counts[v] * f(v) for v in "ABC")


def square(counts, label):
    n = sum(counts.values())
    now_view = total(counts, lambda v: now[v]["planting_tris_L0"])
    g = total(counts, lambda v: prop[v]["lods"]["gameplay"]["tris"])
    l0 = total(counts, lambda v: prop[v]["lods"]["inspection"]["tris"])
    near = total(counts, lambda v: prop[v]["lods"]["near"]["tris"])
    soil = 2 * n
    out = dict(label=label, troughs=n, by_piece=counts,
               now_planting_tris=now_view,
               proposed_tris={"gameplay (testbed's choice at play zoom, 1080 rows)": g, "near (testbed's choice at 1864 rows, the Air's native)": near, "LOD0 inspection (no LODGroup)": l0})
    est = {}
    for k, t in out["proposed_tris"].items():
        dv = t - now_view
        dc = (t - soil) - now_view  # casters: soil does not cast; the domes all cast
        lo, hi = 0.0, 0.7 * (dv + 2 * dc) / 1e6
        est[k] = dict(delta_view_tris=dv, delta_caster_tris_per_cascade=dc, gpu_ms_upper=round(hi, 3), cascades_assumed="1-2 (PC asset 1; Redline 075 cites two)")
    out["geometry_estimate"] = est
    out["draws"] = dict(now="the domes are slots 1-4 of the PlanterRun mesh; in the swap scene they are baked into the shared civic batches (Latch 075 trial), so removing them saves no draw",
                        proposed_forward=f"+{3 * 3} (GPU Resident Drawer instancing: 3 pieces x 3 submeshes) to +{3 * n} (no instancing: {n} instances x 3)",
                        proposed_shadow=f"+{2 * 3} to +{2 * n} per cascade (soil casts nothing)",
                        render_thread_ms=f"+{round(9 * 1.5e-3, 3)} to +{round((3 * n + 2 * n * 2) * 3e-3, 3)} (1.5-3 us a draw)")
    return out


sq = square(SQUARE, "café square: all 12 PlanterRun_4 troughs on its two street kerbs (the piece swap reaches every placement of the piece)")
cf = square(CAFE, "café frontage: run-west-cafe/0-1 and run-east-cafe/0-3")

# pixels: the troughs' projected area at play zoom, a generous box per trough (4.0 x 0.68 top + 1.7 m of planting seen at 38 deg)
ppm = 1080 / 28
area_m2 = 4.0 * (0.68 * math.sin(math.radians(38)) + 1.7 * math.cos(math.radians(38)))
px = round(12 * area_m2 * ppm * ppm)
pixels = dict(play_px_per_m=round(ppm, 2), per_trough_m2_on_screen=round(area_m2, 2), square_px_upper=px, square_Mpx=round(px / 1e6, 3),
              estimate_ms=f"+0.01 to +0.04 (the planting replaces dome pixels that are already shaded; the delta is the textured, normal-mapped leaf shader over flat colour, "
                          f"about a quarter of the 2.1-2.5 ms/Mpx average on {round(px / 1e6, 3)} Mpx at most)",
              overdraw="opaque only: Apple's hidden-surface removal and early depth reject hidden leaves; no alpha-test overdraw (the old-town trial's leaves are alpha-clipped, these are not)")

metrics = dict(
    schema="suborned.metrics077.troughs/1", brief="077 steps 1-3", author="Vela (Opus 5.5)", date="2026-10-30",
    status="candidate pieces, review only: not catalogued, not copied into Unity, no scene touched",
    measured_vs_estimated=dict(
        measured=["triangles and welded vertices per LOD (pieces.json, exact; welded = identical position, normal, uv, colour collapsed as Unity's importer welds)",
                  "submeshes, fit and bounds (troughs077.py)", "current domes' triangles and L1 (contracts/world/kit.json)", "texture source sizes (PNG headers) and sha256"],
        computed=["texture memory at the stated import format with full mip chains", "café-square totals from the world export's placements", "Unity LOD relative heights"],
        estimated=["frame time (Redline's models: k <= 0.7 ms per M drawn triangles incl. shadows, 1.5-3 us a draw, 2.1-2.5 ms/Mpx)", "SetPass and draw deltas in Unity"],
        not_measured=["any Unity or Air frame time, SetPass or draw count", "the shader variant the vertex colour needs", "Unity import (tangents, weld, vertex colour) of these FBXs"]),
    now=now, proposed=prop,
    lod_plan=dict(
        source="the testbed's own authored levels (garden-runtime.js), not a decimation: each level is regenerated from the same leaves by the testbed's code (fewer rows per leaf, simpler blossoms)",
        fbx_naming="DK_PlanterRun_4_<v>_TB_LOD0..LOD3 = inspection, close, near, gameplay (Unity's importer builds the LODGroup from the names)",
        testbed_rule="projected 0.4 m leaf span: gameplay < 18 px, near < 32, close < 72, inspection above (hysteresis 16/28/60)",
        unity_relative_heights=lod_rel, play_zoom_relative_height=play_rel,
        at_play_zoom="gameplay (LOD3) at 1080 rows, as the testbed drew it; Unity's LOD is resolution independent, so on the Air's native 1864 rows the testbed would have drawn near and Unity still draws LOD3",
        full_detail_option="drawing LOD0 everywhere (no LODGroup, as Latch's 075 trial did) costs <= 0.12 ms by Redline's k; both are affordable, so which one ships is Sky's call (brief 028: no compression without Sky)"),
    textures=dict(maps=TEX, total_MiB_compressed=tex_total, total_MiB_uncompressed_rgba32=tex_raw,
                  shared="one atlas set for leaves and petals, one soil set: shared by all three pieces and every future testbed planting port (fountain islands, civic beds, forest)",
                  now="0 MiB: the domes are flat colour",
                  proposal="BC4 from the ORM green (the only channel three reads) would save 1.33 MiB; a format choice, not a downscale; Sky's call"),
    materials=dict(
        pieces=["DK_Soil_TB (soil maps, cull back, no shadow cast)", "DK_Leaf_TB (atlas, normal 0.16, roughness 0.49 x ORM.g, base colour #afbea1, cull off)", "DK_Petal_TB (atlas, normal 0.16, roughness 0.64 x ORM.g, white, cull off)"],
        setpass_estimate="+1 to +2 forward (one new keyword set for leaf/petal: normal + mask + vertex colour + cull off; soil may match an existing DK set), +1 shadow (double-sided caster). Not measured.",
        needs=["vertex colour in DK Lit Cutaway (the testbed's per-leaf tint and root occlusion live there; without it the leaves read flat and lighter)",
               "back-face normal flip for cull-off leaves (three's DoubleSide flips; check DK Lit Cutaway does)"]),
    squares=dict(square=sq, cafe=cf), pixels=pixels,
    height=dict(now_top_local={v: now[v]["top"] for v in "ABC"}, proposed_top_local={v: prop[v]["lods"]["inspection"]["top"] for v in "ABC"},
                leaf_mass_p90_local="1.08-1.21 m (inside today's 1.06-1.12 envelope give or take 0.1 m)",
                above="fans, stems and flower heads rise to 1.61-1.77 m above the trough base (world 1.79-1.95 m at base 0.18), as the testbed's café beds do (1.54-1.64 m above their bed base)",
                consequence="render-only, no collider; but it is 0.55-0.65 m above the envelope Vela and Latch held in 075 for sight and the HOLD: Keel and Vantage say whether the sight raster reads render meshes before anything is placed"),
    threats_to_60fps="None from geometry or pixels on these numbers: the worst case (LOD0 on all 12 troughs, two cascades, no instancing) is about +0.12 ms GPU and +0.25 ms render thread (the render thread runs beside the GPU, so the frame pays the larger, not the sum) against Redline's 0.6 ms in hand at the opening (12.4 of 13.0). The real risks are a new shader variant (SetPass +1 to +2) and the +9 MiB of textures, both small; Redline measures in Unity.",
    provenance=dict(manifest="out/decokit/troughs077/troughs77.manifest.json".replace("troughs77", "troughs077"), sources=MAN["sources"], three=MAN["three"], art=MAN["art"],
                    generator={**MAN["generator"], **{f"tools/decokit/{f}": sha(REPO / "tools/decokit" / f) for f in ("troughs077.py", "troughs077_lib.py", "review077_troughs.py", "review077_page.py")}},
                    bins={k: v["sha256"] for k, v in MAN["files"].items()}),
)
(DST / "metrics.json").write_text(json.dumps(metrics, indent=1) + "\n")

# ------------------------------------------------------------------ page
def jpg(img, q=84):
    b = io.BytesIO()
    img.convert("RGB").save(b, "JPEG", quality=q, optimize=True)
    return "data:image/jpeg;base64," + base64.b64encode(b.getvalue()).decode()


def load(name):
    p = R / name
    return Image.open(p) if p.exists() else None


def project(c, yaw, half, pt, W_=1920, H_=1080):
    yw, pc = math.radians(yaw), math.radians(38)
    f = (math.sin(yw) * math.cos(pc), math.cos(yw) * math.cos(pc), -math.sin(pc))
    r = (math.cos(yw), -math.sin(yw), 0.0)
    u = (r[1] * f[2] - r[2] * f[1], r[2] * f[0] - r[0] * f[2], r[0] * f[1] - r[1] * f[0])
    d = (pt[0] - c[0], pt[1] - c[1], pt[2] - c[2])
    s = H_ / (2 * half)
    return W_ / 2 + s * sum(a * b for a, b in zip(d, r)), H_ / 2 - s * sum(a * b for a, b in zip(d, u))


def crop2x(img, cx, cy, w=360, h=200):
    x0, y0 = int(max(0, min(img.width - w, cx - w / 2))), int(max(0, min(img.height - h, cy - h / 2)))
    return img.crop((x0, y0, x0 + w, y0 + h)).resize((w * 2, h * 2), Image.NEAREST)


CEN = {"testbed": (0.0, -3.0, 0.0), "now": (-159.0, -224.0, 0.18), "proposed": (-159.0, -224.0, 0.18)}
SPOTS = {"testbed": [("bed-east", (5.7, -5.2, 0.6)), ("bed-side", (9.0, -1.5, 0.6))],
         "now": [("run-west-cafe", (-179.5, -220.5, 1.0)), ("run-east-cafe", (-138.5, -224.5, 1.0))]}
SPOTS["proposed"] = SPOTS["now"]
COLS = [("testbed", "Testbed: its own baked café beds (bed-west, bed-east, bed-side), its meshes and maps"), ("now", "Now: the current domes (075 restyle)"),
        ("proposed", "Proposed: PlanterRun_4_A/B/C_TB in the same troughs")]


def grid(views, light, crops=False, suffix=""):
    h = []
    for vname in views:
        h.append(f"<h3>{vname} · {light}</h3><div class='row'>")
        for sc_, lab in COLS:
            if suffix and sc_ == "now":
                continue
            im = load(f"{sc_}-{light}-{vname}{suffix}.png")
            if im is None:
                h.append(f"<figure><div class='miss'>not rendered</div><figcaption>{lab}</figcaption></figure>")
                continue
            lv = json.loads((R / f"{sc_}-{light}{suffix}.json").read_text())["views"][vname]["level"]
            h.append(f"<figure><img src='{jpg(im)}'><figcaption>{lab} · planting level <b>{lv}</b></figcaption></figure>")
        h.append("</div>")
        if crops:
            h.append("<div class='row'>")
            for sc_, lab in COLS:
                if suffix and sc_ == "now":
                    continue
                im = load(f"{sc_}-{light}-{vname}{suffix}.png")
                if im is None:
                    h.append("<figure></figure>")
                    continue
                yaw = 30 if "y030" in vname else 210
                for nm, pt in SPOTS[sc_]:
                    x, y = project(CEN[sc_], yaw, 14, pt)
                    if 0 <= x < 1920 and 0 <= y < 1080:
                        h.append(f"<figure class='crop'><img src='{jpg(crop2x(im, x, y))}'><figcaption>{sc_} · {nm} · play-zoom pixels at 2x (nearest)</figcaption></figure>")
                        break
            h.append("</div>")
    return "\n".join(h)


def ref(path, cap):
    p = REPO / path
    if not p.exists():
        return ""
    im = Image.open(p)
    if im.width > 1920:
        im = im.resize((1920, round(im.height * 1920 / im.width)))
    return f"<figure><img src='{jpg(im)}'><figcaption>{cap}<br><code>{path}</code></figcaption></figure>"


def row(cells, head=False):
    t = "th" if head else "td"
    return "<tr>" + "".join(f"<{t}>{c}</{t}>" for c in cells) + "</tr>"


tbl = [row(["piece", "level", "tris", "welded verts", "submeshes / materials", "alpha", "shadow", "top (m, local)"], True)]
for v in "ABC":
    n = now[v]
    tbl.append(row([f"now <b>{n['piece']}</b> planting", "L0 (drawn at play zoom; L1 at 0.008)", f"{n['planting_tris_L0']} (+60 trough)", "-", f"4 / {', '.join(n['materials'][1:])}", "opaque", "casts", n["top"]]))
for v in "ABC":
    p = prop[v]
    for l in reversed(LEVELS):
        x = p["lods"][l]
        tbl.append(row([f"proposed <b>{p['piece']}</b>" if l == "inspection" else "", f"LOD{x['lod']} {l}", x["tris"], x["vertices_welded"], "3 / DK_Soil_TB, DK_Leaf_TB, DK_Petal_TB" if l == "inspection" else "",
                        "opaque, no alpha test" if l == "inspection" else "", "leaf + petal cast, soil no" if l == "inspection" else "", x["top"]]))
sq_rows = [row(["", "troughs", "now planting tris", "proposed: gameplay", "near", "LOD0 everywhere", "GPU upper (ms), gameplay / near / LOD0"], True)]
for s in (cf, sq):
    t = list(s["proposed_tris"].values())
    e = [x["gpu_ms_upper"] for x in s["geometry_estimate"].values()]
    sq_rows.append(row([s["label"], s["troughs"], s["now_planting_tris"], t[0], t[1], t[2], " / ".join(f"+{x}" for x in e)]))
tex_rows = [row(["map", "source px", "import", "format", "mips", "MiB"], True)] + [row([t["file"].split("/")[-1], "x".join(map(str, t["source_px"])), "x".join(map(str, t["import_px"])), t["format"], t["mips"], t["MiB_compressed"]]) for t in TEX]
tex_rows.append(row(["<b>total</b>", "", "", "", "", f"<b>{tex_total}</b> (uncompressed RGBA32 at source size: {tex_raw})"]))

css = """body{font:14px/1.45 -apple-system,Helvetica,Arial,sans-serif;margin:24px;background:#f5f2ea;color:#17302f}h1{margin:0 0 4px}h2{margin-top:34px;border-bottom:1px solid #99784d}
.row{display:flex;gap:10px;flex-wrap:wrap}figure{margin:0;flex:1 1 30%;min-width:320px}figure img{width:100%;display:block;border:1px solid #cbc6c3}figure.crop{flex:1 1 30%}
figcaption{font-size:12px;color:#40605d;margin:3px 0 10px}table{border-collapse:collapse;margin:8px 0 14px;font-size:12.5px}td,th{border:1px solid #cbc6c3;padding:3px 7px;text-align:left;vertical-align:top}
th{background:#e7e1d3}.verdict{background:#fff;border-left:4px solid #99784d;padding:10px 14px;max-width:1100px}.miss{height:200px;background:#ddd;display:flex;align-items:center;justify-content:center}
code{font-size:11.5px}ul{max-width:1150px}"""

html = f"""<!doctype html><html><head><meta charset='utf-8'><title>077 · café troughs: testbed | now | proposed</title><style>{css}</style></head><body>
<h1>077 · café troughs: testbed | now | proposed</h1>
<p>Vela, 2026-10-30. The testbed's café trough planting, composed by the testbed's own code (<code>createCafeGardenComposition</code>, <code>flowerLookLeafGeometry</code>, lush-tropical profile, the coral-and-amber palette it drew, atlas v2 + normal + ORM, soil maps), fitted to the PlanterRun_4 coping (4.0 x 0.68 m) and sat on today's restyled limestone trough. Candidate pieces only: nothing catalogued, copied into Unity or placed in a scene.</p>
<div class='verdict'><b>Look verdict.</b> At close range the proposed troughs are the testbed's café beds: curled, veined, glossy leaves in four tints, rolled strap fans, cupped coral and gold blooms on stems, dark soil between. That's a clear step up from the domes, and unlike the old-town trial it's a dense green mass, not a dotted line. At play zoom (about 15 px per leaf) they read as a dark, glossy, textured hedge with sparse warm specks. The detail survives, but at a glance the colour reads <i>darker and less flowery</i> than the domes' bright green with pink and coral caps, so the café edge loses some of its candy brightness. They're as faithful to the testbed as I can make them: the same darkness is in the testbed's own frame (reference below). At night they go near-black away from the lamps (a Blender approximation, not Glint's grade). The flower stems rise 0.55 to 0.65 m above today's planting envelope; the leaf mass doesn't. <b>Recommendation:</b> worth a default-off Unity trial in the café troughs, because Blender isn't the game's grade. Sky judges colour and darkness from the Unity capture. The shader needs vertex colour or the leaves lose their tint and occlusion.</div>
<h2>Play zoom (orthographic 14, pitch 38): day</h2>{grid(['play-y030', 'play-y210'], 'day', crops=True)}
<h2>Play zoom: night (Blender approximation: moon, dark teal sky, a warm point light at each kit lamp; the testbed scene has no lamps of ours, so two stand-ins at the café kerb spacing)</h2>{grid(['play-y030', 'play-y210'], 'night', crops=True)}
<h2>Close (orthographic half-height 3, pitch 38): day</h2>{grid(['close-y030', 'close-y210'], 'day')}
<h2>Close: night</h2>{grid(['close-y030', 'close-y210'], 'night')}
<h2>Play zoom with LOD0 (inspection) forced: the full-detail option</h2><p>The testbed would draw its gameplay level at this zoom (above). The same view with the top level instead, testbed and proposed (the domes have no other level at this zoom, so "now" isn't repeated):</p>{grid(['play-y030', 'play-y210'], 'day', crops=True, suffix='-inspection')}
<h2>References (not rendered by me)</h2><div class='row'>
{ref('briefs/026/glint/cafe/testbed-cafe-day.png', "The testbed's own frame (three.js, its light and grade), brief 026 capture: the café beds on the terrace edge, dark glossy green with coral and amber")}
{ref('briefs/075/recovery/oldtown-foliage-unity/baseline/today-day-cafe-square-y210.png', "Now in Unity (Latch 075 baseline, the game's grade): the café's kerb troughs, right, as domes")}
{ref('briefs/075/recovery/oldtown-foliage-unity/baseline/today-night-cafe-square-y210.png', "Now in Unity at night (Latch 075 baseline)")}
</div>
<p><b>How the "testbed" column is made.</b> It renders the testbed's own data: the baked garden <code>plaza-garden*.bin.gz</code> (its gameplay, near, close and inspection levels), decoded as <code>garden-cache.js</code> does, for its three café beds (seeds 3246089332, 3392746718, 2456283560). It uses the testbed's maps and its pearl trough shell, shaded as the testbed's three.js materials. Its bed outline and layout are the testbed café's own (strips with tree bays, terrace dropped from 0.9 m to the ground), not our troughs. Light and renderer are ours (Eevee, the same sun and sky as the other two columns), not three.js. The testbed's own frame is in the references.</p>
<h2>Cost (step 3)</h2>
<p><b>Measured</b>: triangles, welded vertices, submeshes, bounds and fit (exact, from the generated meshes); the domes' triangles from kit.json; texture sizes. <b>Computed</b>: texture memory, square totals, LOD heights. <b>Estimated</b>: milliseconds, SetPass and draws (Redline's models). Nothing here is a Unity or Air measurement.</p>
<table>{''.join(tbl)}</table>
<table>{''.join(sq_rows)}</table>
<p>GPU upper bound = 0.7 ms per million (view + caster) triangles, casters x2 for two cascades (Redline 072). Draws: +9 (Resident Drawer instancing, 3 pieces x 3 submeshes) to +36 forward, +6 to +24 shadow per cascade. The domes are baked into shared civic batches in the swap scene, so removing them saves no draw. Render thread: {sq['draws']['render_thread_ms']} ms (all 12 troughs, two cascades, no instancing at the top end). Pixels: at most {pixels['square_Mpx']} Mpx of trough on screen, replacing dome pixels already shaded; opaque, so no alpha overdraw: +0.01 to +0.04 ms.</p>
<table>{''.join(tex_rows)}</table>
<p>LOD plan: the testbed's own four levels as Unity LODs (LOD0 inspection, LOD1 close, LOD2 near, LOD3 gameplay). The relative heights that reproduce its choice at 1080 rows are {', '.join(f'{k} {x}' for k, x in lod_rel.items())}. Play zoom is {play_rel}, so LOD3 draws. Drawing LOD0 everywhere costs at most +0.12 ms. Both fit, so the choice is Sky's (028).</p>
<p><b>Threat to 60 fps:</b> {metrics['threats_to_60fps']}</p>
<h2>Open issues</h2><ul>
<li><b>Vertex colour.</b> DK Lit Cutaway reads no vertex colour. The testbed's four leaf tints and its root-to-tip occlusion are vertex colours, written linear in the FBX. A keyword for them is a shader change (Latch / Glint) and probably SetPass +1.</li>
<li><b>Double-sided leaves.</b> The leaves are cull-off. Three flips back-face normals; check DK Lit Cutaway does, or the leaf undersides go dark.</li>
<li><b>Height.</b> Stems and blooms reach 1.61 to 1.77 m above the trough base (world up to 1.95 m), against today's envelope of 1.06 to 1.12 m. The leaf mass (p90 1.08 to 1.21 m) is about inside it. The pieces are render-only with no collider; Keel and Vantage confirm the sight raster ignores render meshes before any placement.</li>
<li><b>Atlas size.</b> The atlas is 1774 x 887 (NPOT, 887 is not a multiple of 4). Importing with ToNearest upscales it to 2048 x 1024 for BC7/BC5. That's an upscale, not a cut, but it does resample.</li>
<li><b>Sources outside the repo.</b> The generator imports the testbed's code from <code>~/Projects/the-suborned/testbed/src</code> (read only, sha256 pinned in the manifest). If adopted, Splice vendors those modules into the repo.</li>
<li><b>FBX determinism.</b> FBX bytes carry Blender's timestamp and object ids, as every kit FBX does. Mesh content is pinned by <code>mesh_sha256</code> per LOD, which is stable from run to run.</li>
<li><b>Not yet ported.</b> The café terrace's own curved planter (CafeTerrace embedded planting / PlanterArc) is still domes. It's the next fit: the testbed's <code>flowerBedRun</code> takes arcs natively.</li>
<li><b>Light.</b> Blender light and night are approximations. Colour and darkness are judged from Unity.</li>
</ul>
<h2>Provenance</h2><table>{''.join(row([k, f"<code>{x}</code>"]) for k, x in {**metrics['provenance']['sources'], **metrics['provenance']['art'], **metrics['provenance']['generator'], **metrics['provenance']['bins'], **{p['piece'] + ' FBX': p['fbx']['sha256'] for p in prop.values()}}.items())}</table>
</body></html>"""
(DST / "index.html").write_text(html)
print("wrote", DST / "index.html", round((DST / "index.html").stat().st_size / 2**20, 1), "MiB;", DST / "metrics.json")
