"""Writes briefs/072/style/proposal.html (self-contained). Run: python3 -I html.py"""
import base64, json, re, html
R = "/Users/sky/Projects/suborned/"; H = R + "out/scratch/vela-style/html/"
rules = json.load(open(R + "briefs/072/style/rules.json"))
def img(n, alt): return f'<img alt="{alt}" src="data:image/jpeg;base64,{base64.b64encode(open(H + n, "rb").read()).decode()}">'
def chips(s):
    s = html.escape(str(s))
    return re.sub(r"#([0-9A-Fa-f]{6})\b", lambda m: f'<span class="chip" style="background:#{m.group(1)}"></span>#{m.group(1)}', s)
def short(p): return html.escape(p.replace("unity/Assets/Suborned/Editor/World/", "World/").split(" (")[0].split(";")[0])
rows = "".join(f'<tr class="{r["kind"]}"><td>{r["id"]}</td><td><b>{html.escape(r["key"])}</b><br><span class="f">{short(r["file"])}</span></td><td>{chips(r["today"])}</td><td>{chips(r["decokit"])}</td><td>{chips(r["proposed"])}<br><span class="f">{html.escape(r["basis"])}</span></td><td>{html.escape(r["effect"])}</td><td>{r["choice"]}</td><td>{"yes" if r["rendered"] else "-"}</td></tr>' for r in rules["rows"])
doc = f"""<!doctype html><html lang="en"><head><meta charset="utf-8"><title>072-B · Loosening the style kit toward DecoKit (proposal)</title>
<style>
body{{font:15px/1.5 -apple-system,Helvetica,Arial,sans-serif;color:#17302f;background:#f7f4ec;margin:0;padding:24px 32px;max-width:1700px}}
h1{{font-size:26px;margin:0 0 4px}} h2{{font-size:20px;margin:30px 0 8px;border-bottom:2px solid #d2e6e2;padding-bottom:4px}} h3{{font-size:16px;margin:18px 0 6px}}
.sub{{color:#4b6663}} img{{width:100%;max-width:1608px;display:block;margin:6px 0 2px;border-radius:4px}} .cap{{color:#4b6663;font-size:13px;margin-bottom:14px}}
.box{{background:#fff;border:1px solid #d2e6e2;border-radius:6px;padding:12px 16px;margin:10px 0}} .rec{{color:#0c5b55;font-weight:600}}
table{{border-collapse:collapse;font-size:12.5px;background:#fff;width:100%}} td,th{{border:1px solid #d9e3e0;padding:4px 6px;vertical-align:top;text-align:left}} th{{background:#17302f;color:#f7f4ec;position:sticky;top:0}}
tr.rule td{{background:#fbfaf5}} tr.handoff td,tr.done td,tr.info td{{color:#62706e;background:#f3f3f1}} tr.choice td{{background:#fff7e8}}
.chip{{display:inline-block;width:11px;height:11px;border:1px solid #0003;border-radius:2px;margin:0 3px -1px 0}} .f{{color:#6a7d7a;font-size:11.5px}}
ol li,ul li{{margin:3px 0}}
</style></head><body>
<h1>Loosening the style kit toward the original DecoKit</h1>
<div class="sub">Brief 072-B · Vela · 2026-10-27 · a proposal: nothing in the style contract, GDD, facade.json, civic-look.json or the DecoKit generator has been changed.</div>

<div class="box"><b>Short version.</b> The DecoKit kit's own colours already match the original: 109 of its 113 materials are unchanged. What's holding the look back is three other things. First, the written rules: dark brass allowed only as hairlines, no blue in the glass, near-white wall colours, and four colours still retuned for the old signal-colour gate that was dropped on 16 Oct. Second, the flat procedural walls, which are 79 % of what you see (brief 072-A is replacing them). Third, a paler road and grade. This proposal loosens the rules and values back toward DecoKit: brass-gold trim, blue-teal glass, warmer walls, DecoKit's mustard, coral and royal blue back in the accent pool, and DecoKit's finishes. On its own that changes 1–6 % of each frame. The road is the biggest single lever, so it's shown as a separate choice. The grade is a smaller one, shown as choice 4 (Glint's to set).</div>

<h2>What the old city had that today's frames don't</h2>
{img("ref-office.jpg", "old DecoKit vs today")}
<div class="cap">Measured on the brief 026 captures at the same stops, mean saturation is 0.32–0.62 in the old DecoKit frames, against 0.14–0.22 at the 026 baseline and 0.25–0.27 in today's game shots. The old frames are also darker: the 10th-percentile value is 0.19–0.53, against 0.34–0.56. Most of that comes from the walls (textured kit everywhere in the old city), the dark asphalt road and the grade (old street grade: saturation 25, exposure −0.8; today: 18 and +0.15). The palette rules matter less.</div>
{img("ref-street.jpg", "old DecoKit street vs baseline street")}
<h3>The look being chased: the old DecoKit city at play zoom</h3>{img("old-decokit-play.jpg", "old DecoKit city at play zoom")}
<div class="cap">Brief 026 captures of the old DecoKit city (briefs/026/old-decokit/), play zoom 14 at 38°: the cafe at yaw 30 and a crossing at yaw 210. No new renders. Note the dark asphalt, the textured stone on every wall, the warm gold sun and the gold trim. The zebra crossing is the old city's; it stays out (§15). Mean saturation is 0.32 in both, against 0.15–0.20 in the Loosened renders below.</div>

<h2>Before and after (same geometry, same light, materials only)</h2>
<div class="cap">These are real renders, but not the game's renderer. Blender Eevee builds the street from the world export with the game's own kit FBXs at their export positions. The flat procedural layer (building walls and windows from the skins, shopfront boxes, roof parts) is drawn as stand-ins in the facade.json colours, with my own hash picking each building's family. The light is one sun at the game's angle (pitch 38, yaw 330) plus the DecoKit city HDRI; there's no game grade. Each scene is built once and rendered as "Today", then recoloured in place from the proposal and rendered again as "Loosened". The views are the brief 026 baseline cameras: play zoom 14 at 38°, the café at yaw 30 and the Records Office front at yaw 210; one district street at yaw 30; two close shots.</div>
<h3>Café and square, play zoom, yaw 30</h3>{img("cafe-play-30.jpg", "cafe play 30")}
<div class="cap">The civic block changes least, on purpose: its colours come from the paintings, and its kit enamels are already DecoKit's. You can see the brass-gold frames and the bluer glass.</div>
<h3>Records Office front, play zoom, yaw 210</h3>{img("office-play-210.jpg", "office play 210")}
<div class="cap">Choice 2 is applied here: a deep teal storey band like the old DecoKit office, plus brass-gold hairlines, blue-teal glass and a warmer ivory.</div>
<h3>A district street, play zoom, yaw 30</h3>{img("district-play-30.jpg", "district play 30")}
<div class="cap">Warmer walls and roofs, smoother finishes, and the DecoKit accents on the hosts whose roll lands on them.</div>
<h3>Close: the café front</h3>{img("cafe-close.jpg", "cafe close")}{img("cafe-crop.jpg", "cafe crop")}
<div class="cap">The street-level brass goes from dark bronze (#99784D, a value the old signal gate forced) to brass-gold (#C29A55, between today and DecoKit's #D6AE62).</div>
<h3>Close: the Records Office from the kerb</h3>{img("office-street.jpg", "office street")}

<h2>Choice 3 shown: the road</h2>
{img("office-play-210-road.jpg", "road office")}{img("district-play-30-road.jpg", "road district")}{img("office-street-road.jpg", "road street")}
<div class="cap">Same scenes with the road swapped from the powder-blue enamel (§15) to the existing DK_Ground_RoadAsphalt, which is close to DecoKit's asphalt. It changes 23–33 % of the pixels in these three frames, more than all the palette rows together (1–6 %).</div>

<h2>Choice 4 shown: the grade (an approximation)</h2>
<div class="box"><b>This is an approximation of a grade Glint would set in Unity, not the game's renderer.</b> The same Loosened render goes through a numpy pass twice. Left: today's grade (light.json: saturation 18, contrast 6, exposure +0.15). Right: the old DecoKit street grade (DK_StreetGrade: saturation 25, contrast 10). The pass follows URP's order: exposure in linear, contrast in LogC, saturation around luma. But it runs after Blender's tone map instead of before ACES, so it overstates the highlights a little. The old grade's exposure was −0.8, but on a sun of 3 against today's 1.8. Matched on the sun, that's about −0.06, so the right frame is 0.21 stops darker. Applied literally, −0.8 would be about a stop darker than today and doesn't compare like for like.</div>
<h3>Café and square, yaw 30</h3>{img("cafe-play-30-grade.jpg", "cafe grade")}
<h3>Records Office front, yaw 210</h3>{img("office-play-210-grade.jpg", "office grade")}
<h3>District street, yaw 30</h3>{img("district-play-30-grade.jpg", "district grade")}
<div class="cap">The grade raises mean saturation from 0.18–0.19 to 0.20–0.21 (district: 0.15 to 0.16) and darkens slightly. That's a real step, but a small one: the old city's 0.32 comes mostly from its textured walls, asphalt and warm sun (072-A/C, choice 3, and Glint's light).</div>
<h3>Road and grade together (choices 3 + 4)</h3>{img("office-play-210-road-grade.jpg", "office road and grade")}{img("district-play-30-road-grade.jpg", "district road and grade")}
<div class="cap">Loosened palette plus DK_Ground_RoadAsphalt, under each grade. This pair is the closest these renders get to the old frames: the 10th-percentile value falls to 0.33–0.36, inside the old city's 0.19–0.53.</div>

<h2>What Sky needs to decide</h2>
<ol>
<li><b>Choice 1: the palette loosening</b> (the rows marked 1 below). Brass-gold instead of dark brass, allowed as frames and bands up to about 15 cm and on at most 12 % of an elevation (large gold panels still out). Blue-teal glass with sky reflections (curtain walls still out). Walls and bodies a step warmer and deeper. DecoKit's mustard, deep coral and royal blue back in the district accent pool. DecoKit's finishes (matte 0.22, glossy 0.72). The leftovers from the dropped signal gate removed (oxblood red, flamingo signal red, rose-brass, the "no red-orange banners" clause). Values sit between today and DecoKit; peach goes slightly past DecoKit's chroma, per your "loosened the desaturated hue a little". <span class="rec">Recommendation: yes, as rendered.</span></li>
<li><b>Choice 2: the civic block.</b> Either keep the paintings' pale seafoam, or bring back DecoKit's deep teal band on the Records Office and allow deep-teal bands up to 0.6 m on the civic kit (rows C1–C2). Brass-gold comes with choice 1 either way. <span class="rec">Recommendation: take the deep teal band; it brings back the old office's contrast and the paintings still show teal edge lines.</span></li>
<li><b>Choice 3: the road.</b> Keep your powder-blue enamel road (brief 028), or go back to DecoKit-style asphalt using the DK_Ground_RoadAsphalt material that already exists. This is a §15 rule, so it's yours to call. <span class="rec">Recommendation: try asphalt as an A/B in the next build and decide in the game. In these renders it's the biggest single step toward the old city.</span></li>
<li><b>Choice 4: the grade</b> (owner Glint; rows L1–L2). Keep today's grade (18, 6, +0.15) or move to DecoKit's street grade (saturation 25, contrast 10, exposure matched to today's sun). <span class="rec">Recommendation: yes, with Glint setting it in Unity and checking that the signals still read. It's a small step on its own and matters most with asphalt.</span></li>
</ol>
<div class="box">Not in this proposal: the flat walls are brief 072-A/C. The planets already use the old DecoKit maps (row P1). Once you OK it, the edit is mechanical: rules.json lists file, key, today's value and the new value for every row.</div>

<h2>Every rule and value that differs</h2>
<div class="cap">Columns: today → DecoKit → proposed (with where the value sits). "Choice" is the decision a row belongs to. "Rendered" means the row is applied in the Loosened renders; text rules can't be rendered. The same table, machine-readable: briefs/072/style/rules.json.</div>
<table><tr><th>id</th><th>rule or key · file</th><th>today</th><th>DecoKit</th><th>proposed</th><th>what it does to the picture</th><th>choice</th><th>rendered</th></tr>{rows}</table>
<p class="f">Sources (sha256 in rules.json): contracts/style/suborned.style.json v0.21.0 · GDD v0.30.2 §15 · World/facade.json · World/look.json and looks/harbour.json · contracts/world/civic-look.json · tools/decokit/mats.py · World/light.json · the old DecoKit generator mats.py and decokit_materials.json, ~/Projects/the-suborned. Renders and scripts: out/scratch/vela-style/ (build.py, overrides.json, shots/final). Grade pass: src/grade.py, numbers in out/scratch/vela-style/grade/grade.report.json. Old grade: the-suborned DK_StreetGrade.asset and DecoKitStreetDemo.cs (sun 3).</p>
</body></html>"""
open(R + "briefs/072/style/proposal.html", "w").write(doc)
print(len(doc) / 1e6, "MB")
