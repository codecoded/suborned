#!/usr/bin/env python3
"""Brief 072-G (Vela): briefs/072/style/applied.html, self-contained: before/after pairs (072-B scene, same cameras and light) and the applied palette as swatches.
Run: python3 -I briefs/072/style/applied/src/html.py"""
import base64, html, io, json
from pathlib import Path
from PIL import Image

HERE = Path(__file__).resolve().parent
D = HERE.parent
REPO = HERE.parents[4]
R = json.loads((D.parent / "rules.json").read_text())["applied"]
BEFORE = json.loads((REPO / "out/scratch/vela-applied/before/facade.json").read_text())
e = html.escape

def jpg(path, w=1800):
    im = Image.open(path).convert("RGB"); im.thumbnail((w, w)); b = io.BytesIO(); im.save(b, "JPEG", quality=84)
    return "data:image/jpeg;base64," + base64.b64encode(b.getvalue()).decode()

def chip(h, label=""):
    return f'<span class="chip"><i style="background:{h}"></i>{e(label or h)}</span>'

VIEWS = [("office-street", "Records Office from the crossing, eye height: the deep teal storey band on the wing and both towers, brass-gold hairlines, bluer glass"),
         ("office-play-210", "Records Office at play zoom, yaw 210"),
         ("rocket-high-300", "Rocket Lane row from further out (ortho 60 m, not play zoom): parapet accent lines now enamel red and royal, warmer walls"),
         ("district-play-30", "District street at play zoom, yaw 30: warmer limestone walls, the accent coping coral-red"),
         ("cafe-play-30", "Café and fountain at play zoom, yaw 30 (the civic block keeps the paintings' seafoam; brass-gold frames)"),
         ("cafe-close", "Café terrace close (eye height)")]

def pair(v, cap):
    a, b = D / f"{v}__before.png", D / f"{v}__after.png"
    return f'<figure><div class="pair"><div><b>today (committed)</b><img src="{jpg(a)}"></div><div><b>applied</b><img src="{jpg(b)}"></div></div><figcaption>{e(cap)}</figcaption></figure>'

sw = R["facade_json"]["swatches"]
rows = "".join(f'<tr><td>{e(k)}</td><td>{chip(BEFORE["swatches"].get(k, "#ffffff"), BEFORE["swatches"].get(k, "new"))}</td><td>{chip(v)}</td></tr>' for k, v in sw.items())
look = "".join(f'<tr><td>harbour.{e(k)}</td><td>{chip(v)}</td></tr>' for k, v in R["look_harbour_and_contract_roles"].items())
kit = "".join(f'<tr><td>{e(k)}</td><td>{e(json.dumps(v) if not isinstance(v, str) else v)}</td></tr>' for k, v in R["kit_mats_py"].items())
areas = "".join(f'<tr><td>{e(a)}</td><td>{" ".join(chip(sw.get(x) or BEFORE["swatches"].get(x, "#fff"), x) for x in v["enamel"])}</td><td>{e(", ".join(f"{k} {n}" for k, n in v["families"].items()))}</td></tr>' for a, v in R["facade_json"]["areas_added"].items())
wts = R["world_accent_weights_colour_ts"]
acc = {"accent_teal": "#36b0a8", "accent_coral": "#f28a70", "accent_red": "#c42a1f", "accent_royal": "#2c56a6", "accent_turquoise": "#1cb9c6", "accent_cobalt": "#2f84e8", "accent_mint": "#7fd0b8", "accent_flamingo": "#e8609a", "accent_peach": "#f8b898"}
bar = "".join(f'<span style="flex:{wts[k]};background:{h}" title="{k} {wts[k]}">{wts[k]}</span>' for k, h in acc.items())
was = {"accent_teal": 35, "accent_coral": 20, "accent_turquoise": 15, "accent_mint": 10, "accent_flamingo": 10, "accent_peach": 10}
bar0 = "".join(f'<span style="flex:{n};background:{acc[k] if k != "accent_coral" else "#fc9c84"}">{n}</span>' for k, n in was.items())

page = f"""<!doctype html><html lang="en"><meta charset="utf-8"><title>072-G applied palette · Vela</title>
<style>body{{font:15px/1.45 -apple-system,Helvetica,sans-serif;margin:24px auto;max-width:1840px;padding:0 20px;color:#17302f;background:#faf7f0}}h1{{margin:0 0 4px}}h2{{margin-top:34px}}
.pair{{display:grid;grid-template-columns:1fr 1fr;gap:10px}}.pair img{{width:100%;display:block;border-radius:4px}}.pair b{{display:block;font-size:13px;margin:2px 0}}figure{{margin:22px 0}}figcaption{{color:#4b5f5d;margin-top:6px}}
table{{border-collapse:collapse;font-size:13px}}td{{padding:3px 10px;border-bottom:1px solid #e5dfd2;vertical-align:top}}.chip{{display:inline-flex;align-items:center;gap:5px;margin:1px 6px 1px 0;white-space:nowrap}}.chip i{{width:22px;height:16px;border-radius:3px;border:1px solid #0002;display:inline-block}}
.bar{{display:flex;height:26px;border-radius:4px;overflow:hidden;color:#fff;font-size:12px;max-width:900px;margin:4px 0}}.bar span{{display:flex;align-items:center;justify-content:center;text-shadow:0 1px 1px #0006}}.cols{{display:grid;grid-template-columns:1fr 1fr;gap:30px}}.note{{background:#fff;border-left:4px solid #2c56a6;padding:10px 14px;max-width:1100px}}</style>
<h1>Palette push, applied (072-G)</h1>
<p>Sky, 27 Oct: <i>"yep, more, more reds / blues allowed as well"</i> · deep teal band on the Records Office: <i>"yep"</i> · road: <i>"keep powdblue"</i>. Grade is Glint's (not in these renders).</p>
<div class="note"><b>How to read these.</b> Same Blender scene as the 072-B proposal (Eevee, one sun at the game's angle, the DecoKit city HDRI; not the game's light or grade). Left: today's committed palette, kit materials, world export and RecordsTower. Right: the applied files after <code>bun run decokit</code>, <code>kit</code>, <code>world</code>. The scene draws procedural walls as flat swatches (one per part), so the push in the procedural layer (blush and powder walls, red and blue plinths, enamel bands, trade colours) shows less here than it will in Unity; Latch's offscreen capture (072-G item 5) is the real check.</div>
{''.join(pair(v, c) for v, c in VIEWS)}
<h2>World accents (export, tools/world/gen/colour.ts)</h2><p>Building accent weights (speed bands, piers, spandrels, porthole panels, banners). Today, then applied: reds (coral, red, flamingo) 36, blues (royal, cobalt, turquoise) 30, teal 24. Fins also take red and royal; flags red and royal. In the export: 36 buildings red, 39 royal, 30 cobalt of 310.</p>
<div class="bar">{bar0}</div><div class="bar">{bar}</div>
<div class="cols"><div><h2>facade.json swatches</h2><table><tr><td></td><td>today</td><td>applied</td></tr>{rows}</table>
<p>Deviation from the proposal: <code>sand_p</code> is oxblood, not brass-gold (a 0.5 m plinth band is a large brass surface; Sky asked for more reds).</p></div>
<div><h2>Area enamel sets (applied) and added families</h2><table>{areas}<tr><td>civic</td><td>{chip("#72b5a7","e_seafoam")}{chip("#1f6c67","e_teal_deep")}</td><td>was e_seafoam + e_seafoam_pale</td></tr></table>
<h2>Look roles (look.json, looks/harbour.json, style contract)</h2><table>{look}</table>
<h2>Kit (tools/decokit/mats.py, records_tower.py)</h2><table>{kit}</table></div></div>
<h2>Written rules</h2><p><b>Contract palette_world.rule:</b> {e(R["contract"]["palette_world.rule"])}</p>
<p><b>Civic hairline:</b> {e(R["civic_look_roles"]["hairline"])}<br><b>Civic deep:</b> {e(R["civic_look_roles"]["deep"])}</p>
<p>Final values: <code>briefs/072/style/rules.json</code> → <code>applied</code>. Vela, 2026-10-27.</p></html>"""
(D.parent / "applied.html").write_text(page)
print("wrote", D.parent / "applied.html", round(len(page) / 1e6, 2), "MB")
