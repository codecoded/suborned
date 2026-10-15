import base64, html, io, json, shutil
from pathlib import Path
from PIL import Image
from lib import *
from de import row, srgb2lab, hex2rgb, de2000
import p5

rows = {r["id"]: r for r in read_ledger()}
v1 = [json.loads(l) for l in (SCR / "ledger-v1.jsonl").read_text().splitlines() if l.strip()]
for r in v1: r["file"] = "_iteration/v1/" + r["file"]
A = json.load(open(DEL / "analysis5.json")); T = json.load(open(DEL / "takes5.json")); A1 = json.load(open(DEL / "_iteration/v1/analysis5.json"))
dd = lambda a, b: de2000(srgb2lab(hex2rgb(a)), srgb2lab(hex2rgb(b)))
rel = lambda p: str(Path(p).relative_to(DEL))
def thumb(path, w):
    im = Image.open(path).convert("RGB"); im.thumbnail((w, w * 2)); b = io.BytesIO(); im.save(b, "JPEG", quality=80, optimize=True)
    return "data:image/jpeg;base64," + base64.b64encode(b.getvalue()).decode()
def fig(path, w, cap=""): return f'<figure><a href="{html.escape(rel(path))}"><img loading="lazy" src="{thumb(path, w)}"></a><figcaption>{cap}</figcaption></figure>'
NAMES = {"a": "a. Restrained", "b": "b. Monochrome", "c": "c. Flight deck"}
ROLES = ("scout", "fixer", "tech", "enforcer")

TARGETS = {
 "a": {"graphite base #3a3836": "#3a3836", "pine-teal (Scout) #2f6e68": "#2f6e68", "deep wine (Fixer) #6b3342": "#6b3342", "pale ice-blue grey (Tech) #b4c4d4": "#b4c4d4", "warm taupe-sand (Enforcer) #8c7a68": "#8c7a68", "gunmetal hardware #6c6663": "#6c6663"},
 "b": {"bone #e4e1da": "#e4e1da", "charcoal #2b2d30": "#2b2d30", "gunmetal hardware #6c6663": "#6c6663"},
 "c": {"ink-navy fabric #1c2233": "#1c2233", "brushed steel (Scout) #9aa1a8": "#9aa1a8", "warm pewter (Fixer) #8d8a86": "#8d8a86", "mirror chrome (Tech) #d4d7dc": "#d4d7dc", "smoked titanium (Enforcer) #4a4540": "#4a4540"},
}
P = [f"""<!doctype html><meta charset=utf-8><title>Cell round 5: space age, serious (brief 012)</title><style>
body{{font:14px/1.45 system-ui,sans-serif;margin:0;background:#f1f1ef;color:#222}} header,section.s{{padding:16px 28px}} header{{background:#22262b;color:#eee}}
h1{{margin:0 0 4px}} h2{{border-bottom:2px solid #22262b;padding-bottom:4px;margin-top:44px}} h3{{margin:24px 0 6px}} .note{{max-width:1000px;color:#444;margin:2px 0 10px}}
.grid,.wide{{display:flex;flex-wrap:wrap;gap:12px}} figure{{margin:0;background:#fff;padding:6px;border-radius:6px;box-shadow:0 1px 3px #0002;max-width:300px}} .wide figure{{max-width:760px}} .xw figure{{max-width:1500px}}
figure img{{display:block;max-width:100%;height:auto}} figcaption{{font-size:12px;margin-top:4px}} .b{{font-size:10px;padding:1px 5px;border-radius:8px;color:#fff;background:#4a5560}}
table{{border-collapse:collapse;font-size:12px;margin:8px 0}} td,th{{border:1px solid #bbb;padding:3px 8px;text-align:left;vertical-align:top}} .num td{{text-align:right}} .num td:first-child{{text-align:left}}
.px{{image-rendering:pixelated}} .sw{{display:inline-block;width:14px;height:14px;border:1px solid #888;vertical-align:middle}} .pick{{background:#fff;border-left:6px solid #22262b;padding:8px 16px;max-width:1000px}} .bad{{color:#a02020}} .ok{{color:#1d6b3a}}
.strips{{display:flex;gap:18px;align-items:flex-start;flex-wrap:wrap;background:#fff;padding:10px;border-radius:6px}} .strips img{{image-rendering:pixelated}}
</style><header><h1>Cell round 5: space age, serious</h1><div>Brief 012 round 5 · Forma · 2026-10-16 · Codex image tool only, no paid spend. Verdict on round 4 ("nah, too fruity"): the candy accents on glossy white are out. Same four people, same cut. Full files by click; model, prompt, refs, sha256 in PROVENANCE.json.</div></header><section class=s>"""]
P.append("""<h2>Pick</h2><div class=pick><b>c. Flight deck</b> (ink fabric, brushed metal) as the base, because it keeps the most of the space age (sculpted metal collars, shoulder plates, ring collars, a chrome bell panel) with no colour at all, and its metal carries the 50 px read: the Tech's chrome skirt and the Fixer's pewter leg stripes show at play size, the Scout by the pack and collar plates, the Enforcer by size.<br>
Runner-up <b>b. Monochrome</b> reads best of the three at 50 px (light and dark are placed differently on each person), but its bone melts into the city's cream (dE 6.0 from the pavement #f9efdc), and white-and-black is also how the guide dresses security.<br>
<b>a. Restrained</b> is the most dangerous-looking and the most different from security, but at 50 px only the Tech's pale blue and the Enforcer's taupe show; the Scout's teal and the Fixer's wine are dim.<br>
<b>Not decided here, for Sky:</b> whether c's chrome-on-black is too close to security's white, black and chrome; a cheap fix is a round 6 that puts one of a's muted blocks on the Scout and the Enforcer into c.</div>""")
P.append("<h2>1. What changed against round 4</h2><p class=note>Kept: the space-age cut (moulded collar-hood and pack, ring collar and waist circle, quilted tunic with a rigid bell panel and porthole visor, moulded shoulder-yoke cuirass), the four faces and bodies, the signature items, the porthole lamp (now a faint pale aqua, small). Taken out: candy and pastel accents, high-gloss vinyl and patent, white boots, the bright and optimistic wording, the expressive poses. Three small departures from the round 4 outfits, on purpose, all of them toy or costume tells: the Scout's bare legs are covered by matte tights under the shorts; the white go-go and moulded boots are dark matte boots with ring buckles; the Tech's bell is a dark or metal moulded panel rather than a pale flared skirt. Each prompt says the space-age character is in the form (ring collars, porthole discs, moulded panels), not in the colour, and carries no date.</p>")
P.append("<h2>2. Lineups</h2><div class=wide>")
for d in "abc":
    r = rows[f"R5-lineup-{d}"]; P.append(fig(DEL / r["file"], 760, f'<span class=b>Codex</span> <b>{NAMES[d]}</b><br>{html.escape(p5.DIR[d]["hook"])}'))
P.append("</div>")
cmpf = DEL / "lineups/comparison-R4L4-vs-R5.jpg"
M = Image.new("RGB", (1536, 4 * 1024 // 1), "white"); ims = [ROOT / "briefs/012/cell/round4/lineups/R4-lineup-L4.png"] + [DEL / f"lineups/R5-lineup-{d}.png" for d in "abc"]
M = Image.new("RGB", (1200, 4 * 800), "white")
for i, p in enumerate(ims): M.paste(Image.open(p).convert("RGB").resize((1200, 800)), (0, i * 800))
M.save(cmpf, quality=88)
P.append("<h3>Next to the one that was rejected</h3><div class=wide>" + fig(cmpf, 900, "Top: round 4 L4 (rejected, 'too fruity'). Then round 5 a, b, c.") + "</div>")

P.append("<h2>3. Play-size strips (50 px)</h2><p class=note>The four figures at play scale: tallest figure 56 px after the 38 degree foreshortening, cut out onto three grounds (neutral, the city's pavement cream #f9efdc, the asphalt #a5a2a8) and as black silhouettes; native size, then 4x (nearest-neighbour). Rows in each column: neutral, cream pavement, asphalt, silhouette.</p>")
P.append('<div class=strips>' + "".join(f'<div><b>{NAMES[d]}</b><br><img class=px src="strips/strip-{d}.png"> native<br><img class=px src="strips/strip-{d}-x4.png" style="width:470px"></div>' for d in "abc") + "</div>")
P.append('<h3>All three at 3x, three grounds</h3><div class=strips><img src="strips/overview-50px-x3.png"></div><p class=note>Columns: a, b, c. Reading, honestly: <b>b</b> reads as four different people at once, because the light and dark are placed differently on each (light jacket and pack over dark legs; dark tunic over white trousers; dark, light, dark; pale shoulders on a dark body) and it keeps its silhouette on every ground, though the bone parts fade on cream. <b>c</b> is next: the chrome and pewter give each person a different bright shape, the Scout and Enforcer lean on silhouette. <b>a</b> is the quietest: the dusty colours are small blocks and soften to grey at 50 px, except the Tech\'s ice blue. All three are dark teams, so on the pale city they stand out; none of them reads as a toy line.</p>')
P.append("<h2>4. Colour</h2><p class=note>Gate: dE2000 20 or more from cone red #e03a2f, guard amber #f0a020, rival violet #8a4fd0. First the colours aimed at, then what was painted. Every desaturated mid-tone blue lands near the rivals' violet in dE2000, which is why the round 5 first pass (slate blue, blued steel) failed and was redrawn.</p>")
for d in "abc":
    P.append(f"<h3>{NAMES[d]}: target colours</h3><table class=num><tr><th>colour</th><th>cones</th><th>guard</th><th>rivals</th></tr>")
    for n, h in TARGETS[d].items():
        r = row(h); P.append(f"<tr><td><span class=sw style='background:{h}'></span> {n}</td><td>{r['cones']}</td><td>{r['guard']}</td><td>{r['rivals']}</td></tr>")
    P.append("</table>")
P.append("<h3>As painted: lineups</h3><p class=note>Per figure, the painted colour (held by 0.5 percent or more of the figure, skin removed) nearest each signal and its dE, then the share of the figure's pixels within dE 20 of that signal (the thin edge pixels, shadows and skin edges). Round 4 L1 and L4 measured the same way for reference.</p>")
P.append("<table class=num><tr><th>lineup</th><th>figure</th><th>cones: nearest painted (dE)</th><th>guard</th><th>rivals</th><th>share under 20 (cones / guard / rivals)</th></tr>")
for k in ("a", "b", "c"):
    for role in ROLES:
        s = A[k]["figures"][role]["signals"]; cell = lambda x: f"<span class=sw style='background:{x['nearest_painted']}'></span> {x['nearest_painted']} ({x['dE']})"
        P.append(f"<tr><td>{k}</td><td>{role}</td><td>{cell(s['cones'])}</td><td>{cell(s['guard'])}</td><td>{cell(s['rivals'])}</td><td>{s['cones']['share_within_20']*100:.1f}% / {s['guard']['share_within_20']*100:.1f}% / {s['rivals']['share_within_20']*100:.1f}%</td></tr>")
    v = A[k]; P.append(f"<tr style='font-weight:600'><td>{k}</td><td>lowest</td><td>{v['min_dE_to_signal']['cones']}</td><td>{v['min_dE_to_signal']['guard']}</td><td>{v['min_dE_to_signal']['rivals']}</td><td></td></tr>")
for k in ("R4-L1", "R4-L4"):
    v = A[k]; P.append(f"<tr class=bad><td>{k}</td><td>lowest</td><td>{v['min_dE_to_signal']['cones']}</td><td>{v['min_dE_to_signal']['guard']}</td><td>{v['min_dE_to_signal']['rivals']}</td><td>worst figure: {v['worst_share_within_20']['cones']*100:.1f}% within 20 of cones</td></tr>")
P.append("</table><h3>As painted: the twelve takes</h3><table class=num><tr><th>take</th><th>cones (dE)</th><th>guard</th><th>rivals</th><th>share under 20 (cones / guard / rivals)</th></tr>")
for k, s in T.items():
    cell = lambda x: f"<span class=sw style='background:{x['nearest_painted']}'></span> {x['dE']}"
    P.append(f"<tr><td>{k}</td><td>{cell(s['cones'])}</td><td>{cell(s['guard'])}</td><td>{cell(s['rivals'])}</td><td>{s['cones']['share_within_20']*100:.1f}% / {s['guard']['share_within_20']*100:.1f}% / {s['rivals']['share_within_20']*100:.1f}%</td></tr>")
P.append("</table><p class=note>Everything painted at 0.5 percent of a figure or more is 23 or more from all three signals (lowest 23.3, the Fixer's pewter next to guard amber in c); the thin remainder (at most 0.9 percent of a figure) is skin edge and shadow. Close calls to watch in the engine: the Enforcer's taupe in a (25 from the cones and from amber) and the bone in b (24 from amber). Also against the city: bone #e4e1da is 6.0 from the pavement #f9efdc and 6.9 from a slightly darker cream: the monochrome set's pale parts disappear on cream, only the charcoal carries.</p>")
P.append("<h2>5. Four different people at 50 px</h2><p class=note>Silhouette: profile distance (how different the width-over-height outlines are; the closest pair matters) and true-scale overlap (lower is more distinct). Value: mean lightness (L) of the upper and the lower half of each figure, which is what survives at 50 px. Colour separation is the dE between two figures' dominant colour clusters on the 50 px cut-outs; it is low for all three because every team is dark, and lower for a than for round 4 L4 (14.7, the toy read). Judge by the strips above, not by this number.</p>")
P.append("<table class=num><tr><th>lineup</th><th>closest pair</th><th>min profile</th><th>mean profile</th><th>mean IoU</th><th>colour sep 50 px min / mean</th><th>L up / low: scout</th><th>fixer</th><th>tech</th><th>enforcer</th></tr>")
for k in ("a", "b", "c", "R4-L1", "R4-L4"):
    v = A[k]; P.append(f"<tr><td>{k}</td><td>{v['closest_pair']}</td><td>{v['min_profile_dist']}</td><td>{v['mean_profile_dist']}</td><td>{v['mean_iou']}</td><td>{v['min_colour_pair']} / {v['mean_colour_pair']}</td>" + "".join(f"<td>{v['figures'][r]['L_upper']:.0f} / {v['figures'][r]['L_lower']:.0f}</td>" for r in ROLES) + "</tr>")
P.append("</table><p class=note>Figures: 0 Scout, 1 Fixer, 2 Tech, 3 Enforcer. The silhouettes are the same shapes as round 4 (closest pairs 0.032 to 0.042 against 0.031 to 0.041 there); the new work is in value and metal, not outline. Black silhouette sheets are in silhouettes/.</p>")
P.append('<div class=strips>' + "".join(f'<div><b>{NAMES[d]}</b><br><img src="silhouettes/{d}-black.png" style="width:470px"></div>' for d in "abc") + "</div>")
P.append("<h2>6. The twelve takes</h2><p class=note>One take per person per direction, from the head and body references of round 4 (face and body shape only); full prompts in PROVENANCE.json.</p>")
for d in "abc":
    P.append(f"<h3>{NAMES[d]}</h3><div class=grid>" + "".join(fig(DEL / rows[f'R5-{d}-{r}']['file'], 280, f"<b>{r}</b><br><i>{html.escape(rows[f'R5-{d}-{r}']['identity'])}</i>") for r in ROLES) + "</div>")
P.append("<h2>7. Animation notes (estimates from the drawings; no rig built)</h2><p class=note>The cut is round 4's, so the bone count is round 4's: the Scout's pack and collar-hood (+2), the Fixer's ponytail, cable and case (+8), the Tech's visor discs (2 hinges) and a rigid bell panel needing 2 to 4 hem bones or a split front and back panel (+4 to +6), the Enforcer's rifle socket and cuirass (+1); about +16 over the 65-bone humanoid. Differences by direction: matte fabric needs no vinyl or patent shader and no cloth sim (all rigid shells skinned to the body). In c the metal collar, shoulder plates, chest plate and gauntlets are rigid parts on the body bones (no extra bones); the Tech's chrome bell is the one flared piece and stays a moulded panel. The lamps stay emissive materials, driven by the 'acting' state.</p>")
P.append(f"<h2>8. Iteration log</h2><p class=note>First pass of all twelve takes and three lineups (kept in _iteration/v1/). Measured on the painted lineups it failed the gate in two places: a's slate-blue Tech was 17.5 from the rivals' violet and a's teal Scout 17.5 (4 to 9 percent of those figures under 20), and c's blued-steel Tech was 19.2 (3 percent). The slate-blue target itself was 16.8 from violet (I had not checked the targets before painting), and the teal target (24.2) painted greyer than aimed; mid-tone desaturated blues and teals sit near violet in dE2000. Redrawn with a pale ice-blue Tech and a greener pine-teal Scout in a, mirror chrome for the Tech in c. At the same time the prompts were changed to say that the space-age character is in the form, not the colour, and c's metal was made bigger and placed differently on each person for the 50 px read. v1 colour numbers: a lowest rivals {A1['a']['min_dE_to_signal']['rivals']}, c lowest rivals {A1['c']['min_dE_to_signal']['rivals']}.</p><div class=wide>" + "".join(fig(DEL / f"_iteration/v1/lineups/R5-lineup-{d}.png", 420, f"v1 {NAMES[d]}") for d in "abc") + "</div>")
P.append("<h2>9. Open issues</h2><ul><li>Pick between c (space age), b (50 px) and a (most different from security): Sky.</li><li>Security's palette is white, black, gold and chrome in the guide (§15); c and b both overlap it. Glyph or Aster to confirm what the guard sprites will wear before the cell's palette is fixed.</li><li>The guide's player-cell line in the style contract still says 'black, cream and gold with a violet accent'; none of round 5 follows it (violet is the rivals' signal, cream and gold are the city's). It needs a GDD edit once Sky has chosen; I have not touched it.</li><li>The porthole lamps are the only colour in b and c besides skin; at 50 px each is a dot of pale aqua and only reads with bloom.</li><li>HUD portraits untouched, as asked.</li></ul></section>")
(DEL / "index.html").write_text("".join(P))

def prow(r):
    d = {k: r.get(k) for k in ("id", "set", "model", "model_id", "prompt", "seed", "cost", "file", "size", "sha256", "sha256_png", "hook", "at", "secs", "refnotes")}
    d["refs"] = [("refs/" + Path(x).name if "cell-r5/refs" in str(x) else rel(x) if str(x).startswith(str(DEL)) else str(x)) for x in r.get("refs", [])]
    for k in ("direction", "role", "outfit", "identity", "takes"): 
        if k in r: d[k] = r[k]
    d["note"] = "Codex image tool: prompt verbatim, unseeded, no per-image cost."; return d
prov = dict(brief="012 round 5", owner="Forma", date="2026-10-16", spent_usd=0, model="Codex image tool only (ChatGPT plan)", verdict_on_round4="nah, too fruity (Sky via Aster)",
            pick="c. Flight deck; runner-up b", analysis=A, takes_check=T, images=[prow(r) for r in sorted(rows.values(), key=lambda r: r["id"])], iteration_v1=[prow(r) for r in sorted(v1, key=lambda r: r["id"])])
(DEL / "PROVENANCE.json").write_text(json.dumps(prov, indent=1))
for f in Path(__file__).parent.glob("*.py"): pass
print("ok", len(rows), "images,", len(v1), "v1")
