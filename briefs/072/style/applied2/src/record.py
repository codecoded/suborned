#!/usr/bin/env python3
"""Brief 072, second push (Vela): writes rules.json `applied2` (values, counts, file hashes) and appends the push-2 section to briefs/072/style/applied.html
(self-contained: before / applied / colourway-proposal triptychs at the 072-B cameras plus Lantern Lane, the swatches, the spread). Idempotent: the section
between the push-2 markers is replaced. Run after apply2.py, the pipeline, spread.py (before and after) and the renders.
Run: python3 -I briefs/072/style/applied2/src/record.py"""
import base64, hashlib, html, io, json, sys
from pathlib import Path
from PIL import Image

HERE = Path(__file__).resolve().parent
D = HERE.parent
REPO = HERE.parents[4]
sys.path.insert(0, str(HERE))
import apply2  # noqa: E402

RULES = D.parent / "rules.json"
PAGE = D.parent / "applied.html"
SB, SA = json.loads((D / "spread-before.json").read_text()), json.loads((D / "spread-after.json").read_text())
e = html.escape
FILES = ["contracts/style/suborned.style.json", "unity/Assets/Suborned/Editor/World/facade.json", "unity/Assets/Suborned/Editor/World/look.json",
         "unity/Assets/Suborned/Editor/World/looks/harbour.json", "contracts/world/civic-look.json", "tools/world/gen/colour.ts"]
WEIGHTS = {"accent_teal": 13, "accent_coral": 12, "accent_red": 9, "accent_royal": 9, "accent_tangerine": 9, "accent_turquoise": 7, "accent_cerulean": 7,
           "accent_cobalt": 6, "accent_flamingo": 6, "accent_plum": 6, "accent_mint": 5, "accent_jade": 5, "accent_peach": 3, "accent_lime": 3}
ACC = {"accent_teal": "#36b0a8", "accent_coral": "#f28a70", "accent_red": "#c42a1f", "accent_royal": "#2c56a6", "accent_turquoise": "#1cb9c6", "accent_cobalt": "#2f84e8",
       "accent_mint": "#7fd0b8", "accent_flamingo": "#e8609a", "accent_peach": "#f8b898", **{k: v.lower() for k, v in apply2.LOOK_HEX.items()}}
KEYS = ("buildings", "neighbour_pairs", "wall_family", "distinct", "distinct_dE8", "distinct_enamel_dE8", "neighbours_same", "neighbour_wall_dE_summary",
        "export_accent", "export_accent2", "export_accent_neighbours_same", "export_accent_pairs", "spec")


def record():
    r = json.loads(RULES.read_text())
    r["applied2"] = {
        "date": "2026-10-28", "by": "Vela (072 second push)", "sky": "yeah, I more colors needed",
        "files_sha256": {f: hashlib.sha256((REPO / f).read_bytes()).hexdigest() for f in FILES},
        "facade_json": {"swatches_changed": apply2.SWATCHES, "swatches_added": apply2.NEW, "families_added": apply2.FAMILIES_ADD,
                        "areas": {a: {"families": f, "enamel_added": en} for a, (f, en) in apply2.AREAS.items()}, "civic": "unchanged: seafoam + deep teal"},
        "look_harbour_and_contract_roles": apply2.LOOK_HEX,
        "world_accent_weights_colour_ts": WEIGHTS,
        "fin_roles_add": ["accent_tangerine", "accent_plum", "accent_cerulean", "accent_jade"], "flag_roles_add": ["accent_tangerine", "accent_cerulean", "accent_lime", "accent_plum"],
        "contract": {"version": "0.23.0", "palette_world.source.sha256": json.loads((REPO / FILES[0]).read_text())["palette_world"]["source"]["sha256"]},
        "kit_mats_py": "unchanged (no kit colour moved; decokit --mats-only reproduced decokit_materials.json byte-equal in content)",
        "civic_look": "unchanged (lookcolours072.py --check: up to date)", "road": "unchanged: powder-blue enamel",
        "spread": {"before": {k: SB[k] for k in KEYS}, "after": {k: SA[k] for k in KEYS},
                   "how": "briefs/072/style/applied2/src/spread.py: the builder's FacadeKit.Fill + ProcGrammar.Assign replayed (same dice, same neighbour scoring); export accents read as they are"},
        "finding": ("The every-face kit modules (17,827 in the export, all `colourway: base`) draw every building's walls in the same DK_Limestone_Tex, teal fascia and seafoam bands; "
                    "Unity never remaps a module's colourway. So the wall families reach the screen only on roofs, the leftover procedural walls and shopfront stretches; "
                    "the walls Sky sees stay cream until module colourways are wired (export colourway per building, kit material variants, KitColumns remap). "
                    "The colourway renders (applied2/*__colourway.png) are that proposal, not the game."),
    }
    RULES.write_text(json.dumps(r, indent=1, ensure_ascii=False) + "\n")


def jpg(path, w=1500):
    im = Image.open(path).convert("RGB"); im.thumbnail((w, w)); b = io.BytesIO(); im.save(b, "JPEG", quality=82)
    return "data:image/jpeg;base64," + base64.b64encode(b.getvalue()).decode()


def chip(h, label=""): return f'<span class="chip"><i style="background:{h}"></i>{e(label or h)}</span>'


VIEWS = [("lantern-high-300", "Lantern Lane (lantern-west/4..14), ortho 70 m, yaw 300: the district street where the families show"),
         ("lantern-play-300", "Lantern Lane at play zoom, yaw 300"),
         ("rocket-high-300", "Rocket Lane row from further out (ortho 60 m)"),
         ("rocket-play-300", "Rocket Lane at play zoom, yaw 300"),
         ("district-play-30", "District street at play zoom, yaw 30"),
         ("office-play-210", "Records Office at play zoom, yaw 210 (civic: seafoam and deep teal kept)"),
         ("office-street", "Records Office from the crossing, eye height"),
         ("cafe-play-30", "Café and fountain at play zoom, yaw 30 (civic block unchanged)"),
         ("cafe-close", "Café terrace close")]


def section():
    fig = "".join(f'<figure><div class="tri"><div><b>before (072-G, committed)</b><img src="{jpg(D / f"{v}__before.png")}"></div>'
                  f'<div><b>applied (what Unity draws: modules stay one colourway)</b><img src="{jpg(D / f"{v}__after.png")}"></div>'
                  f'<div><b>proposal: module colourways (not built)</b><img src="{jpg(D / f"{v}__colourway.png")}"></div></div><figcaption>{e(c)}</figcaption></figure>' for v, c in VIEWS)
    fam = "".join(f'<tr><td>{n}</td><td>{chip(apply2.NEW[k[0]])}{chip(apply2.NEW[k[1]], "plinth")}{chip(apply2.NEW[k[2]], "roof")}</td></tr>' for n, k in apply2.FAMILIES_ADD.items())
    chg = "".join(f'<tr><td>{k}</td><td>{chip(v)}</td></tr>' for k, v in apply2.SWATCHES.items())
    bar = "".join(f'<span style="flex:{n};background:{ACC[k]}" title="{k}">{n}</span>' for k, n in WEIGHTS.items())
    def row(label, k, sub=None):
        b, a = (SB[k][sub], SA[k][sub]) if sub else (SB[k], SA[k]); return f"<tr><td>{e(label)}</td><td>{b}</td><td>{a}</td></tr>"
    tab = "".join([row("wall colours drawn", "distinct", "wall"), row("wall colours ≥ 8 dE2000 apart", "distinct_dE8", "wall"), row("plinth colours", "distinct", "plinth"),
                   row("roof colours", "distinct", "roof"), row("enamel A/B colours (≥ 8 dE apart)", "distinct_enamel_dE8"),
                   f"<tr><td>world accent roles (export skins)</td><td>{len(SB['export_accent'])}</td><td>{len(SA['export_accent'])}</td></tr>",
                   f"<tr><td>area enamel swatches</td><td>{SB['spec']['area_enamel']}</td><td>{SA['spec']['area_enamel']}</td></tr>",
                   f"<tr><td>neighbours: same wall / plinth / roof (of {SA['neighbour_pairs']} pairs)</td><td>{SB['neighbours_same']['wall']} / {SB['neighbours_same']['plinth']} / {SB['neighbours_same']['roof']}</td><td>{SA['neighbours_same']['wall']} / {SA['neighbours_same']['plinth']} / {SA['neighbours_same']['roof']}</td></tr>",
                   f"<tr><td>neighbour walls dE2000 min / median</td><td>{SB['neighbour_wall_dE_summary']['min']} / {SB['neighbour_wall_dE_summary']['median']}</td><td>{SA['neighbour_wall_dE_summary']['min']} / {SA['neighbour_wall_dE_summary']['median']}</td></tr>",
                   f"<tr><td>export accent: same as a neighbour</td><td>{SB['export_accent_neighbours_same']}</td><td>{SA['export_accent_neighbours_same']}</td></tr>"])
    fams = lambda s: ", ".join(f"{k} {n}" for k, n in s["wall_family"].items())  # noqa: E731
    return f"""<!-- push2:start -->
<style>.tri{{display:grid;grid-template-columns:1fr 1fr 1fr;gap:8px}}.tri img{{width:100%;display:block;border-radius:4px}}.tri b{{display:block;font-size:12.5px;margin:2px 0}}.warn{{background:#fff;border-left:4px solid #e2661f;padding:10px 14px;max-width:1100px}}</style>
<h1 style="margin-top:60px">Second push: more colours (072, 28 Oct)</h1>
<p>Sky, 28 Oct, on the applied palette: <i>"yeah, I more colors needed"</i>. Everything 072-G added stays; the range widens. Civic block keeps seafoam and deep teal; the road stays powder blue; signals own no colour.</p>
<div class="warn"><b>What the renders show.</b> Same Blender scene and light as 072-B and 072-G, now with each building's colours replayed from the builder's own picks (spread.py), and the procedural mass set behind the kit modules' slab as Unity does (072-G's scene drew it at the building line, so its colour showed through the modules' window pockets and flattered the "after"). Left: 072-G as committed. Middle: this push as Unity will draw it. <b>The every-face kit modules (17,827, all <code>colourway: base</code>) paint every building's walls the same cream stone with teal and seafoam bands, so the new wall families reach the screen only on roofs, leftover procedural walls and shop stretches.</b> Right: a proposal, not built: the modules take their building's colourway (stone tinted to the wall, teal → enamel A, seafoam → enamel B, granite plinth → the family plinth). That is what makes neighbours read different at play zoom; it needs the export, the kit and the builder (see the open issues).</div>
{fig}
<h2>Spread over the district (320 buildings, 189 street neighbour pairs)</h2>
<table><tr><td></td><td>before</td><td>after</td></tr>{tab}</table>
<p>Wall families drawn, before: {e(fams(SB))}. After: {e(fams(SA))}.</p>
<div class="cols"><div><h2>New families (facade.json)</h2><table>{fam}</table><h2>Changed swatches</h2><table>{chg}</table></div>
<div><h2>World accent weights (tools/world/gen/colour.ts)</h2><div class="bar">{bar}</div>
<p>New roles (look.json, looks/harbour.json, style contract v0.23.0): {"".join(chip(v, k) for k, v in apply2.LOOK_HEX.items())}</p>
<p>New enamels in the area sets: e_tangerine, e_plum, e_cerulean, e_jade, e_lime (and flamingo in lantern). Fins take tangerine, plum, cerulean and jade; flags tangerine, cerulean, lime and plum.</p></div></div>
<p>Values: <code>briefs/072/style/rules.json</code> → <code>applied2</code>. Vela, 2026-10-28.</p>
<!-- push2:end -->"""


def page():
    t = PAGE.read_text()
    if "<!-- push2:start -->" in t:
        t = t[:t.index("<!-- push2:start -->")] + t[t.index("<!-- push2:end -->") + len("<!-- push2:end -->"):]
    t = t.replace("</html>", section() + "</html>", 1) if "</html>" in t else t + section()
    PAGE.write_text(t)


if __name__ == "__main__":
    record()
    page()
    print("rules.json applied2 and applied.html push-2 section written")
