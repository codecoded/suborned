#!/usr/bin/env python3
"""Self-contained report for the civic override table + brass over gold (Vela, 14 Oct 2026). No Blender.
Reads out/decokit-008/civic-overrides.json and out/decokit-brass/brass-family.json, embeds the before/after renders,
writes out/review/008-brass-overrides.html. Run: python3 tools/decokit/report008b.py"""

import base64
import html
import io
import json
from pathlib import Path

from PIL import Image

REPO = Path(__file__).resolve().parents[2]
OV = json.loads((REPO / "out/decokit-008/civic-overrides.json").read_text())
BR = json.loads((REPO / "out/decokit-brass/brass-family.json").read_text())
R = REPO / "out/review/008-renders-brass"
e = html.escape
sw = lambda h: f'<span class="sw" style="background:{h}"></span>{e(h)}'  # noqa: E731
mn = lambda d: min(d.values())  # noqa: E731


def img(path, w=420):
    im = Image.open(path).convert("RGB")
    im.thumbnail((w, w * 2))
    b = io.BytesIO()
    im.save(b, "JPEG", quality=82)
    return f'<img src="data:image/jpeg;base64,{base64.b64encode(b.getvalue()).decode()}" alt="{e(path.name)}">'


def table(head, rows):
    return "<table><tr>" + "".join(f"<th>{h}</th>" for h in head) + "</tr>" + "".join("<tr>" + "".join(f"<td>{c}</td>" for c in r) + "</tr>" for r in rows) + "</table>"


def main():
    s = []
    s.append("<h1>Civic overrides and brass over gold</h1><p>Vela, 14 Oct 2026. Proposal only: <code>contracts/</code> and <code>unity/</code> are not touched. Splice applies it after the light pass.</p>")
    # ---- civic table
    rows = []
    for piece, d in OV["by_piece"].items():
        for slot, v in d["slots"].items():
            tg = "<br>".join(f"{e(t['material'])} {sw(t['unlit'])} (dE {t['min']})" for t in v["targets"])
            rows.append([e(piece) + f"<br><small>{', '.join(e(m.split('/')[-1]) for m in d['masses'])}</small>", e(slot), f"{sw(v['now'])} (dE {mn(v['now_dE'])})", tg, e(v["painting"]), e(v["why"])])
    s.append("<h2>1. Civic override table (civic masses, reachable)</h2>" + table(["piece / masses", "slot", "now", "target (unlit, min dE2000 to a signal colour)", "painting", "why"], rows))
    s.append("<p>The gold slots (PottedPalm_Small, RobotButler, AtomFountain) are not in this table: they follow the brass family below. "
             "The contract's four <code>PlanterArc DK_Accent_Gold -&gt; DK_Brass_Civic</code> rows are now redundant (the material itself is brass) and harmless.</p>")
    s.append("<h3>Before / after (brass in both, override in the second)</h3><div class='row'>" + "".join(f"<figure>{img(R / f'{n}.play.{t}.png', 380)}<figcaption>{n} {t}</figcaption></figure>" for n in ("FountainGarden",) for t in ("before", "over")) + "</div>")
    s.append("<div class='row'>" + "".join(f"<figure>{img(R / f'{n}.thumb.{t}.png', 250)}<figcaption>{n} {t}</figcaption></figure>" for n in ("PottedPalm_Small", "UtilityCabinet", "RobotButler") for t in ("before", "over")) + "</div>")
    s.append("<h3>Not a change</h3><p>" + e(OV["not_a_change"]["CafeSet_Tulip_Play"]) + "</p>")
    # ---- not reachable
    nr = OV["not_reachable"]
    rows = [[e(k), e(v["now"]), "<br>".join(e(i) for i in v["ids"])] for k, v in nr["kinds"].items()]
    s.append("<h2>2. Not civic masses (cannot be reached by civic-look.json)</h2><p>" + e(nr["why"]) + "</p>" + table(["kind", "draws now", "civic items"], rows))
    s.append("<ol>" + "".join(f"<li><b>step {x['step']}</b>: {e(x['what'])}<br><small>reaches: {e(x['reaches'])}</small></li>" for x in nr["smallest_first"]) + "</ol>")
    # ---- landing
    ld = OV["landing"]
    rows = [[e(o["option"]), sw(o["hex"]), e(str(o["surface_key"])), f"{o['dE']['cones']} / {o['dE']['guard']} / {o['dE']['rivals']}", str(o["vs_civic_paving_E4DACA"]), e(o["note"])] for o in ld["options"]]
    s.append("<h2>3. Landing patch: a question for Sky</h2><p>" + e(ld["question_for_sky"]) + "</p>" + table(["option", "colour", "existing key", "dE cones / guard / rivals", "dE from civic paving", "note"], rows)
             + "<p><b>Recommendation.</b> " + e(ld["recommendation"]) + "</p><p><b>Civic block meanwhile.</b> " + e(ld["civic_block_meanwhile"]) + "</p>")
    # ---- brass
    rows = [[e(r["material"]), e(r["role"]), sw(r["old"]) if r["old"] else "-", sw(r["new"]) if r["new"] else "-", str(mn(r["dE_old"])) if r["dE_old"] else "-", str(mn(r["dE_new"])) if r["dE_new"] else "-"] for r in BR["family"]]
    s.append("<h2>4. Brass family (district, street level and facades)</h2>" + table(["material", "role", "old", "new", "dE old", "dE new"], rows)
             + "<p>Names kept (a rename would change 61 .mat guids for no gain). DK_Far_Gold and DK_Neon_Gold untouched. DK_Crown_Gold and DK_Crown_Gold17 are new: the old values, for the skyline.</p>")
    cp = BR["crown_pieces"]
    s.append(f"<h3>Crown pieces ({len(cp)}): keep their gold, FBX + LODs rebuilt, tris and size identical</h3><p>Test: seen mainly against the sky means crown gold; seen from the street at eye level means brass.</p>"
             + table(["piece", "category", "height m"], [[e(p["id"]), e(p["category"]), str(p["height_m"])] for p in cp]))
    bp = BR["brass_pieces"]
    s.append(f"<h3>Brass pieces ({len(bp)}): .mat hash only</h3>" + table(["piece", "category", "height m", "materials"], [[e(p["id"]), e(p["category"]), str(p["height_m"]), e(", ".join(p["materials"]))] for p in bp])
             + "<p>Judgement calls (tall, but seen at eye level, so brass): " + ", ".join(e(k) for k in BR["judgement_calls"]) + ". Move any to <code>crown.CROWN_LANDMARKS</code> to keep its gold.</p>")
    # ---- facade
    fd = OV["facade_district"]
    rows = [[e(k), sw(v["old"]), sw(v["new"]), e(v["kind"]), str(v["min"]), e(", ".join(v["used_at"])), e(v["why"])] for k, v in fd.items()]
    s.append("<h2>5. District facade list (Latch's data)</h2>" + table(["key", "old", "new", "kind", "min dE", "used at", "why"], rows))
    rows = [[e(k), sw(v["old"]), sw(v["new"]), str(mn(v["dE_new"])), e(v["why"])] for k, v in OV["look_and_contract_roles_still_gold"].items()]
    s.append("<h3>Look and contract roles still gold (Aster / Latch)</h3>" + table(["role", "old", "new", "dE", "why"], rows))
    # ---- splice
    cp_ = OV["changed_pieces"]
    s.append("<h2>6. For Splice</h2><ul>"
             f"<li>re-adopt FBX + LODs + .mat: {len(cp_['re_adopt_fbx_lods_and_mat'])} crown pieces</li>"
             f"<li>re-adopt .mat only: {len(cp_['re_adopt_mat_only'])} brass pieces</li>"
             f"<li>new materials: {e(', '.join(cp_['new_materials']))}</li><li>legacy copy: {e(', '.join(cp_['legacy_copy']))}</li>"
             f"<li>palette rows: {e(', '.join(r['material'] + ' ' + r['unlit'] for r in OV['palette_change']))} (civic palette_add: {e(', '.join(r['material'] for r in OV['palette_add']))})</li>"
             f"<li>{e(cp_['no_change'])}</li></ul><p>Full lists with file paths and sha256: <code>out/decokit-008/civic-overrides.json</code> (<code>changed_pieces</code>) and <code>out/decokit-brass/brass-family.json</code>.</p>")
    css = "body{font:14px/1.45 system-ui;max-width:1180px;margin:2em auto;color:#222;padding:0 1em}table{border-collapse:collapse;margin:.6em 0;width:100%}td,th{border:1px solid #ccc;padding:3px 6px;vertical-align:top;text-align:left}th{background:#eee}.sw{display:inline-block;width:14px;height:14px;border:1px solid #888;margin-right:4px;vertical-align:-2px}.row{display:flex;flex-wrap:wrap;gap:8px}figure{margin:0}figcaption{font-size:12px}code{background:#f2f2f2;padding:0 3px}small{color:#555}"
    out = REPO / "out/review/008-brass-overrides.html"
    out.write_text(f"<!doctype html><meta charset=utf-8><title>Civic overrides and brass</title><style>{css}</style>" + "\n".join(s))
    print(out, out.stat().st_size // 1024, "KB")


if __name__ == "__main__":
    main()
